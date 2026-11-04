const { test } = require('node:test');
const assert = require('node:assert/strict');
const { webcrypto } = require('node:crypto');
const { gzipSync } = require('node:zlib');
const { IDBFactory, IDBObjectStore } = require('fake-indexeddb');
const BrowserSaveStore = require('../Client/wwwroot/web-save-store.js');

const bytes = n => new Uint8Array([31, 139, 8, n]);
const info = n => JSON.stringify({ GameID: 'adversarial', SaveVersion: 8, Turn: n });
function fixture() {
  const indexedDB = new IDBFactory();
  const make = () => new BrowserSaveStore({ indexedDB, crypto: webcrypto });
  return { indexedDB, make, store: make() };
}
async function getRecord(store) {
  const db = await store.open();
  return new Promise((resolve, reject) => {
    const tx = db.transaction('checkpoints', 'readonly');
    const request = tx.objectStore('checkpoints').get(['adversarial', 'Quick']);
    tx.oncomplete = () => resolve(request.result);
    tx.onabort = () => reject(tx.error);
  });
}

test('failed first transaction leaves no discoverable campaign and retry remains possible', async () => {
  const { store, make } = fixture(); await store.hydrate();
  const original = IDBObjectStore.prototype.put;
  IDBObjectStore.prototype.put = function (...args) {
    const request = original.apply(this, args);
    request.addEventListener('success', () => this.transaction.abort());
    return request;
  };
  try { await assert.rejects(store.commit('adversarial', 'Quick', bytes(1), info(1))); }
  finally { IDBObjectStore.prototype.put = original; }
  assert.deepEqual(await make().hydrate(), []);
  await store.commit('adversarial', 'Quick', bytes(2), info(2));
  assert.equal((await make().hydrate())[0].metadata, info(2));
});

test('a concurrent caller cannot interleave another slot during an active commit', async () => {
  const { store } = fixture(); await store.hydrate();
  const first = store.commit('adversarial', 'Quick', bytes(1), info(1));
  await assert.rejects(store.commit('adversarial', 'Primary', bytes(2), info(2)), /still saving/i);
  await assert.rejects(store.hydrate(), /still saving/i);
  await first;
  await store.commit('adversarial', 'Primary', bytes(3), info(3));
  assert.equal((await store.hydrate()).length, 2);
});

test('the payload returned by commit cannot mutate the retained backup generation', async () => {
  const { store } = fixture(); await store.hydrate();
  const committed = await store.commit('adversarial', 'Quick', bytes(1), info(1));
  new Uint8Array(committed.payload)[3] = 255;
  await store.commit('adversarial', 'Quick', bytes(2), info(2));
  assert.deepEqual(new Uint8Array((await getRecord(store)).previous.payload), bytes(1));
});

test('hydration consumers cannot mutate the next checkpoint backup generation', async () => {
  const { store, make } = fixture(); await store.hydrate();
  await store.commit('adversarial', 'Quick', bytes(1), info(1));
  const fresh = make(); const [loaded] = await fresh.hydrate();
  new Uint8Array(loaded.payload)[3] = 255;
  await fresh.commit('adversarial', 'Quick', bytes(2), info(2));
  assert.deepEqual(new Uint8Array((await getRecord(fresh)).previous.payload), bytes(1));
});

test('a conflicting session cannot bypass its lock by calling hydrate again', async () => {
  const { store, make } = fixture(); await store.hydrate();
  await store.commit('adversarial', 'Quick', bytes(1), info(1));
  const stale = make(); await stale.hydrate();
  await store.commit('adversarial', 'Quick', bytes(2), info(2));
  await assert.rejects(stale.commit('adversarial', 'Quick', bytes(3), info(3)), /another tab/i);
  await assert.rejects(stale.hydrate(), /another tab/i);
  assert.equal(stale.ready, false);
  await assert.rejects(stale.commit('adversarial', 'Quick', bytes(4), info(4)));
  assert.equal((await getRecord(store)).current.metadata, info(2));
});

test('a database upgrade invalidates this store instead of reopening it as writable', async () => {
  const { store, indexedDB } = fixture(); await store.hydrate();
  const db = await store.open();
  await new Promise((resolve, reject) => {
    const request = indexedDB.open(db.name, db.version + 1);
    request.onsuccess = () => { request.result.close(); resolve(); };
    request.onerror = () => reject(request.error);
  });
  assert.equal(store.ready, false);
  await assert.rejects(store.commit('adversarial', 'Quick', bytes(1), info(1)));
});

// Real gzip is essential here: the ordinary persistence tests deliberately use
// opaque gzip-shaped bytes, while importing an untrusted backup must decode it.
const BUILD_ID = '8c4573ae-web-fixture';
function realPayload(turn, magic = 0x304F4F43, version = 8) {
  const raw = Buffer.alloc(12);
  raw.writeUInt32LE(magic, 0); raw.writeInt32LE(version, 4); raw.writeInt32LE(turn, 8);
  return new Uint8Array(gzipSync(raw));
}
function backupRecord(turn, gameId = 'adversarial', slot = 'Quick') {
  return { gameId, slot, payloadBase64: Buffer.from(realPayload(turn)).toString('base64'),
    metadata: JSON.stringify({ GameID: gameId, SaveVersion: 8, Turn: turn }) };
}
function bundle(...checkpoints) {
  return { format: 'caves-of-ooo-backup', version: 1, buildId: BUILD_ID, checkpoints };
}

test('backup roundtrip keeps real compressed bytes and retains the destination checkpoint as previous', async () => {
  const source = fixture(), destination = fixture();
  await source.store.hydrate(); await destination.store.hydrate();
  await source.store.commit('adversarial', 'Quick', realPayload(20), info(20));
  const exported = await source.store.exportBackup(BUILD_ID);
  assert.equal(exported.format, 'caves-of-ooo-backup');
  assert.equal(exported.version, 1); assert.equal(exported.buildId, BUILD_ID);
  assert.deepEqual(exported.checkpoints, [backupRecord(20)]);
  await destination.store.commit('adversarial', 'Quick', realPayload(10), info(10));
  await destination.store.importBackup(JSON.parse(JSON.stringify(exported)), BUILD_ID);
  const record = await getRecord(destination.store);
  assert.deepEqual(new Uint8Array(record.current.payload), realPayload(20));
  assert.equal(record.current.metadata, info(20));
  assert.deepEqual(new Uint8Array(record.previous.payload), realPayload(10));
  assert.equal(record.previous.metadata, info(10));
  assert.equal((await destination.make().hydrate())[0].metadata, info(20));
});

test('a late invalid backup record prevents every checkpoint write', async () => {
  const { store, make } = fixture(); await store.hydrate();
  await store.commit('adversarial', 'Quick', realPayload(1), info(1));
  const before = await getRecord(store);
  const invalid = backupRecord(3, 'second-campaign');
  invalid.payloadBase64 = Buffer.from(realPayload(3, 0x11111111)).toString('base64');
  await assert.rejects(store.importBackup(bundle(backupRecord(2), invalid), BUILD_ID));
  assert.deepEqual(await getRecord(store), before);
  const loaded = await make().hydrate();
  assert.equal(loaded.length, 1); assert.equal(loaded[0].metadata, info(1));
});

test('backup validation rejects build, path, metadata, duplicate-key, and encoding ambiguities without mutation', async () => {
  const { store } = fixture(); await store.hydrate();
  await store.commit('adversarial', 'Quick', realPayload(1), info(1));
  const before = await getRecord(store);
  const invalidBundles = [];
  const wrongBuild = bundle(backupRecord(2)); wrongBuild.buildId = 'another-build'; invalidBundles.push(wrongBuild);
  const wrongFormat = bundle(backupRecord(2)); wrongFormat.format = 'another-game'; invalidBundles.push(wrongFormat);
  const wrongVersion = bundle(backupRecord(2)); wrongVersion.version = 2; invalidBundles.push(wrongVersion);
  invalidBundles.push(bundle(backupRecord(2, '../outside')));
  invalidBundles.push(bundle(backupRecord(2, 'adversarial', '../Quick')));
  invalidBundles.push(bundle(backupRecord(2), backupRecord(3)));
  const wrongIdentity = backupRecord(2); wrongIdentity.metadata = JSON.stringify({ GameID: 'other', SaveVersion: 8 });
  invalidBundles.push(bundle(wrongIdentity));
  const wrongMetadataVersion = backupRecord(2); wrongMetadataVersion.metadata = JSON.stringify({ GameID: 'adversarial', SaveVersion: 7 });
  invalidBundles.push(bundle(wrongMetadataVersion));
  const badBase64 = backupRecord(2); badBase64.payloadBase64 = 'not base64 %%%'; invalidBundles.push(bundle(badBase64));
  for (const invalid of invalidBundles) {
    await assert.rejects(store.importBackup(invalid, BUILD_ID));
    assert.deepEqual(await getRecord(store), before, JSON.stringify(invalid).slice(0, 160));
  }
});

test('backup import verifies gzip integrity and the decoded game header', async () => {
  const { store, make } = fixture(); await store.hydrate();
  const valid = realPayload(2);
  const badCrc = valid.slice(); badCrc[badCrc.length - 8] ^= 0xff;
  const cases = [valid.slice(0, 12), badCrc, realPayload(2, 0x11111111), realPayload(2, 0x304F4F43, 7), new Uint8Array(gzipSync(Buffer.alloc(3)))];
  for (const payload of cases) {
    const record = backupRecord(2); record.payloadBase64 = Buffer.from(payload).toString('base64');
    await assert.rejects(store.importBackup(bundle(record), BUILD_ID));
    assert.deepEqual(await make().hydrate(), []);
  }
});

test('backup import rejects a small gzip bomb exceeding the total decompressed budget', async () => {
  const { store, make } = fixture(); await store.hydrate();
  let raw = Buffer.alloc(128 * 1024 * 1024 + 1);
  raw.writeUInt32LE(0x304F4F43, 0); raw.writeInt32LE(8, 4);
  const record = backupRecord(2);
  record.payloadBase64 = gzipSync(raw).toString('base64'); raw = null;
  await assert.rejects(store.importBackup(bundle(record), BUILD_ID));
  assert.deepEqual(await make().hydrate(), []);
});

test('backup decompression budget applies across records before any import writes', async () => {
  const { store, make } = fixture(); await store.hydrate();
  let raw = Buffer.alloc(65 * 1024 * 1024);
  raw.writeUInt32LE(0x304F4F43, 0); raw.writeInt32LE(8, 4);
  const encoded = gzipSync(raw).toString('base64'); raw = null;
  const first = backupRecord(1), second = backupRecord(2, 'second-campaign');
  first.payloadBase64 = second.payloadBase64 = encoded;
  await assert.rejects(store.importBackup(bundle(first, second), BUILD_ID));
  assert.deepEqual(await make().hydrate(), []);
});

test('export is read-only and does not refresh a stale gameplay session revision', async () => {
  const { store, make } = fixture(); await store.hydrate();
  await store.commit('adversarial', 'Quick', realPayload(1), info(1));
  const otherTab = make(); await otherTab.hydrate();
  await otherTab.commit('adversarial', 'Quick', realPayload(2), info(2));
  const before = await getRecord(otherTab);
  const exported = await store.exportBackup(BUILD_ID);
  assert.equal(exported.checkpoints.length, 1);
  assert.deepEqual(await getRecord(otherTab), before);
  await assert.rejects(store.commit('adversarial', 'Quick', realPayload(3), info(3)), /another tab/i);
  assert.deepEqual(await getRecord(otherTab), before);
});

test('a fresh unhydrated instance can export persisted checkpoints without starting gameplay', async () => {
  const { store, make } = fixture(); await store.hydrate();
  await store.commit('adversarial', 'Quick', realPayload(1), info(1));
  const exporter = make();
  const exported = await exporter.exportBackup(BUILD_ID);
  assert.equal(exported.checkpoints.length, 1);
  assert.equal(exported.checkpoints[0].metadata, info(1));
  assert.equal(exporter.ready, false);
});

test('raw recovery export preserves unrecoverable bytes without changing storage or enabling gameplay', async () => {
  const { store, make } = fixture(); await store.hydrate();
  await store.commit('adversarial', 'Quick', realPayload(1), info(1));
  const corrupt = await getRecord(store);
  corrupt.current.payload = bytes(255).buffer;
  const db = await store.open();
  await new Promise((resolve, reject) => {
    const tx = db.transaction('checkpoints', 'readwrite');
    tx.objectStore('checkpoints').put(corrupt);
    tx.oncomplete = resolve; tx.onabort = () => reject(tx.error);
  });
  const reader = make(); await assert.rejects(reader.hydrate(), /recover|corrupt/i);
  const before = await getRecord(store);
  const recovery = await reader.exportRecovery(BUILD_ID);
  assert.equal(recovery.format, 'caves-of-ooo-recovery');
  assert.equal(recovery.version, 1); assert.equal(recovery.buildId, BUILD_ID);
  assert.equal(recovery.records.length, 1);
  assert.equal(recovery.records[0].current.payloadBase64, Buffer.from(bytes(255)).toString('base64'));
  assert.equal(recovery.records[0].current.metadata, info(1));
  assert.equal(recovery.records[0].current.payloadHash, before.current.payloadHash);
  assert.equal(reader.ready, false);
  assert.deepEqual(await getRecord(store), before);
  await assert.rejects(reader.exportBackup(BUILD_ID), /recover|corrupt/i);
  const destination = fixture(); await destination.store.hydrate();
  await assert.rejects(destination.store.importBackup(recovery, BUILD_ID));
  assert.deepEqual(await destination.make().hydrate(), []);
});
