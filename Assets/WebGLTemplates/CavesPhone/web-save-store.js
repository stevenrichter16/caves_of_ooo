/* Atomic browser checkpoints. Independent of Unity's IDBFS auto-sync. */
(function (root) {
  'use strict';
  const DATABASE = 'caves-of-ooo-checkpoints';
  const STORE = 'checkpoints';
  const SCHEMA = 1;
  const MAX_BYTES = 128 * 1024 * 1024;
  const safeId = value => typeof value === 'string' && /^[A-Za-z0-9_-]{1,80}$/.test(value);
  const keyOf = (gameId, slot) => JSON.stringify([gameId, slot]);

  class BrowserSaveStore {
    constructor(options = {}) {
      this.idb = options.indexedDB || root.indexedDB;
      this.crypto = options.crypto || root.crypto;
      this.ready = false;
      this.revisions = new Map();
      this.verified = new Map();
      this.conflicts = new Set();
      this.busy = false;
    }

    open() {
      if (this.opening) return this.opening;
      this.opening = new Promise((resolve, reject) => {
        if (!this.idb || !this.crypto?.subtle) {
          reject(new Error('Browser storage requires HTTPS and IndexedDB.')); return;
        }
        const request = this.idb.open(DATABASE, SCHEMA);
        let settled = false;
        request.onupgradeneeded = () => request.result.createObjectStore(STORE, { keyPath: ['gameId', 'slot'] });
        request.onblocked = () => { settled = true; reject(new Error('Close other game tabs, then reload to upgrade storage.')); };
        request.onerror = () => { settled = true; reject(request.error || new Error('Cannot open browser storage.')); };
        request.onsuccess = () => {
          const db = request.result;
          if (settled) { db.close(); return; }
          db.onversionchange = () => {
            this.ready = false; db.close();
            this.onUnavailable?.("Browser storage changed in another tab. Reload this page.");
          };
          resolve(db);
        };
      });
      return this.opening;
    }

    async digest(bytes) {
      const hash = new Uint8Array(await this.crypto.subtle.digest('SHA-256', bytes));
      return Array.from(hash, b => b.toString(16).padStart(2, '0')).join('');
    }

    validateInput(gameId, slot, payload, metadata) {
      if (!safeId(gameId) || !safeId(slot)) throw new Error('Invalid checkpoint identifier.');
      if (!(payload instanceof Uint8Array) || payload.length < 3 || payload.length > MAX_BYTES
          || payload[0] !== 31 || payload[1] !== 139 || payload[2] !== 8)
        throw new Error('Invalid or oversized compressed checkpoint.');
      if (typeof metadata !== 'string' || metadata.length > 1024 * 1024) throw new Error('Invalid checkpoint metadata.');
      const info = JSON.parse(metadata);
      if (!info || info.GameID !== gameId || info.SaveVersion !== 8)
        throw new Error('Checkpoint belongs to another game or save format.');
    }

    async validSnapshot(gameId, slot, snapshot) {
      try {
        if (!snapshot) return false;
        const bytes = new Uint8Array(snapshot.payload);
        this.validateInput(gameId, slot, bytes, snapshot.metadata);
        return snapshot.payloadHash === await this.digest(bytes)
          && snapshot.metadataHash === await this.digest(new TextEncoder().encode(snapshot.metadata));
      } catch (_) { return false; }
    }

    async hydrate() {
      if (this.busy) throw new Error('A checkpoint is still saving.');
      this.ready = false;
      const db = await this.open();
      const records = await new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, 'readonly');
        const request = tx.objectStore(STORE).getAll();
        tx.oncomplete = () => resolve(request.result);
        tx.onabort = () => reject(tx.error || new Error('Cannot read checkpoints.'));
        tx.onerror = () => {}; // onabort is the final failure signal.
      });
      const loaded = [], revisions = new Map(), verified = new Map();
      for (const record of records) {
        if (record.schema !== SCHEMA || !safeId(record.gameId) || !safeId(record.slot)
            || typeof record.revision !== 'string') throw new Error('Unsupported or corrupt checkpoint record. Export storage before resetting.');
        let snapshot = record.current, recovered = false;
        if (!await this.validSnapshot(record.gameId, record.slot, snapshot)) {
          snapshot = record.previous; recovered = true;
          if (!await this.validSnapshot(record.gameId, record.slot, snapshot))
            throw new Error('Cannot recover checkpoint ' + record.gameId + '/' + record.slot + '. Your stored data has been retained.');
        }
        const key = keyOf(record.gameId, record.slot);
        revisions.set(key, record.revision); verified.set(key, snapshot);
        loaded.push({ gameId: record.gameId, slot: record.slot, payload: snapshot.payload.slice(0),
          metadata: snapshot.metadata, recovered });
      }
      this.revisions = revisions; this.verified = verified;
      // A conflicting live session must reload gameplay, not just refresh revisions.
      if (this.conflicts.size) throw new Error('Another tab saved this game. Reload this page to continue safely.');
      this.ready = true;
      return loaded;
    }

    async records() {
      const db = await this.open();
      return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, 'readonly');
        const request = tx.objectStore(STORE).getAll();
        tx.oncomplete = () => resolve(request.result);
        tx.onabort = () => reject(tx.error || new Error('Cannot read browser storage.'));
      });
    }

    static base64(payload) {
      const bytes = new Uint8Array(payload), chunks = [];
      for (let i = 0; i < bytes.length; i += 32768)
        chunks.push(String.fromCharCode(...bytes.subarray(i, i + 32768)));
      return btoa(chunks.join(''));
    }

    async exportBackup(buildId) {
      // Export must never refresh the revision owned by a running game.
      const reader = new BrowserSaveStore({ indexedDB: this.idb, crypto: this.crypto });
      const db = await reader.open();
      try {
        const loaded = await reader.hydrate();
        return { format: 'caves-of-ooo-backup', version: 1, buildId,
          checkpoints: loaded.map(record => ({ gameId: record.gameId, slot: record.slot,
            payloadBase64: BrowserSaveStore.base64(record.payload), metadata: record.metadata })) };
      } finally { db.close(); }
    }

    async exportRecovery(buildId) {
      const records = await this.records();
      const encode = snapshot => snapshot ? { ...snapshot, payload: undefined,
        payloadBase64: BrowserSaveStore.base64(snapshot.payload) } : null;
      return { format: 'caves-of-ooo-recovery', version: 1, buildId,
        records: records.map(record => ({ ...record, current: encode(record.current), previous: encode(record.previous) })) };
    }

    async importBackup(bundle, expectedBuildId) {
      if (!this.ready || this.busy || this.conflicts.size) throw new Error('Browser storage is not ready for import.');
      if (!bundle || bundle.format !== 'caves-of-ooo-backup' || bundle.version !== 1
          || !expectedBuildId || bundle.buildId !== expectedBuildId)
        throw new Error('Backup format or game build differs. Use the same build that exported this backup.');
      if (!Array.isArray(bundle.checkpoints) || bundle.checkpoints.length > 1024)
        throw new Error('Invalid backup checkpoint list.');
      this.busy = true;
      try {
        const prepared = [], seen = new Set();
        let compressedTotal = 0, expandedTotal = 0;
        for (const record of bundle.checkpoints) {
          const encoded = record.payloadBase64;
          if (typeof encoded !== 'string' || encoded.length > MAX_BYTES * 1.34
              || encoded.length % 4 !== 0 || /[^A-Za-z0-9+/=]/.test(encoded)
              || (encoded.includes('=') && !/^[A-Za-z0-9+/]*={1,2}$/.test(encoded)))
            throw new Error('Invalid base64 checkpoint.');
          const raw = atob(encoded), bytes = Uint8Array.from(raw, c => c.charCodeAt(0));
          compressedTotal += bytes.length;
          if (compressedTotal > MAX_BYTES) throw new Error('Backup exceeds the compressed size limit.');
          this.validateInput(record.gameId, record.slot, bytes, record.metadata);
          const key = keyOf(record.gameId, record.slot);
          if (seen.has(key)) throw new Error('Duplicate checkpoint in backup.');
          seen.add(key);
          // Read through EOF so gzip CRC/truncation failures cannot enter storage.
          const stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream('gzip'));
          const reader = stream.getReader(), header = new Uint8Array(8);
          let headerBytes = 0;
          try {
            for (;;) {
              const part = await reader.read(); if (part.done) break;
              expandedTotal += part.value.length;
              if (expandedTotal > MAX_BYTES) { await reader.cancel(); throw new Error('Backup exceeds the decompressed size limit.'); }
              const count = Math.min(8 - headerBytes, part.value.length);
              header.set(part.value.subarray(0, count), headerBytes); headerBytes += count;
            }
          } finally { reader.releaseLock(); }
          const data = new DataView(header.buffer);
          if (headerBytes !== 8 || data.getUint32(0, true) !== 0x304f4f43 || data.getInt32(4, true) !== 8)
            throw new Error('Checkpoint has an incompatible game header.');
          prepared.push({ key, gameId: record.gameId, slot: record.slot, revision: this.crypto.randomUUID(),
            current: { payload: bytes.buffer, metadata: record.metadata,
              payloadHash: await this.digest(bytes), metadataHash: await this.digest(new TextEncoder().encode(record.metadata)) } });
        }
        // Every input has been validated before this single atomic import transaction.
        const db = await this.open();
        await new Promise((resolve, reject) => {
          let tx;
          try { tx = db.transaction(STORE, 'readwrite', { durability: 'strict' }); }
          catch (_) { tx = db.transaction(STORE, 'readwrite'); }
          let failure = null;
          tx.oncomplete = resolve;
          tx.onabort = () => reject(failure || tx.error || new Error('Import transaction aborted.'));
          tx.onerror = () => {};
          const store = tx.objectStore(STORE);
          for (const item of prepared) {
            const request = store.get([item.gameId, item.slot]);
            request.onsuccess = () => {
              if ((request.result?.revision || null) !== (this.revisions.get(item.key) || null)) {
                this.conflicts.add(item.key); failure = new Error('Another tab saved this game. Reload before importing.'); tx.abort(); return;
              }
              try { store.put({ schema: SCHEMA, gameId: item.gameId, slot: item.slot, revision: item.revision,
                current: item.current, previous: this.verified.get(item.key) || null }); }
              catch (error) { failure = error; tx.abort(); }
            };
          }
        });
        for (const item of prepared) { this.revisions.set(item.key, item.revision); this.verified.set(item.key, item.current); }
      } finally { this.busy = false; }
    }

    async commit(gameId, slot, payload, metadata) {
      if (!this.ready) throw new Error('Browser storage has not hydrated.');
      const key = keyOf(gameId, slot);
      if (this.conflicts.has(key)) throw new Error('Another tab saved this game. Reload before saving again.');
      if (this.busy) throw new Error('A checkpoint is still saving.');
      this.validateInput(gameId, slot, payload, metadata);
      const owned = payload.slice(); // Before the first await: callers may reuse WASM memory.
      this.busy = true;
      try {
        const current = { payload: owned.buffer, metadata,
          payloadHash: await this.digest(owned), metadataHash: await this.digest(new TextEncoder().encode(metadata)) };
        const db = await this.open();
        const revision = this.crypto.randomUUID();
        const expected = this.revisions.get(key) || null;
        await new Promise((resolve, reject) => {
          let tx;
          try { tx = db.transaction(STORE, 'readwrite', { durability: 'strict' }); }
          catch (_) { tx = db.transaction(STORE, 'readwrite'); }
          let failure = null;
          tx.oncomplete = resolve;
          tx.onabort = () => reject(failure || tx.error || new Error('Checkpoint transaction aborted.'));
          tx.onerror = () => {};
          const store = tx.objectStore(STORE);
          const request = store.get([gameId, slot]);
          request.onsuccess = () => {
            if ((request.result?.revision || null) !== expected) {
              this.conflicts.add(key);
              failure = new Error('Another tab saved this game. Reload before saving again.');
              tx.abort(); return;
            }
            try {
              store.put({ schema: SCHEMA, gameId, slot, revision, current,
                previous: this.verified.get(key) || null });
            } catch (error) { failure = error; tx.abort(); }
          };
        });
        // A put success is insufficient: only the entire transaction acknowledges durability.
        this.revisions.set(key, revision); this.verified.set(key, current);
        return { gameId, slot, payload: current.payload.slice(0), metadata };
      } finally { this.busy = false; }
    }
  }
  root.CooBrowserSaveStore = BrowserSaveStore;
  if (typeof module !== 'undefined') module.exports = BrowserSaveStore;
})(typeof window !== 'undefined' ? window : globalThis);
