const { test } = require('node:test');
const assert = require('node:assert/strict');
const { webcrypto } = require('node:crypto');
const { IDBFactory, IDBObjectStore } = require('fake-indexeddb');
const BrowserSaveStore = require('../Client/wwwroot/web-save-store.js');

function fixture() {
  const indexedDB = new IDBFactory();
  const make = () => new BrowserSaveStore({ indexedDB, crypto: webcrypto });
  return { make, store: make() };
}
const payload = n => new Uint8Array([31, 139, 8, n]);
const metadata = n => JSON.stringify({ GameID: 'game', SaveVersion: 8, Turn: n });
async function record(store) {
  const db = await store.open();
  return new Promise((resolve, reject) => {
    const tx = db.transaction('checkpoints', 'readonly');
    const req = tx.objectStore('checkpoints').get(['game', 'Quick']);
    tx.oncomplete = () => resolve(req.result);
    tx.onabort = () => reject(tx.error);
  });
}
async function replace(store, value) {
  const db = await store.open();
  return new Promise((resolve, reject) => {
    const tx = db.transaction('checkpoints', 'readwrite');
    tx.objectStore('checkpoints').put(value);
    tx.oncomplete = resolve; tx.onabort = () => reject(tx.error);
  });
}

test('opening is distinct from hydration; commits require successful hydration', async () => {
  const { store } = fixture();
  await store.open();
  await assert.rejects(store.commit('game', 'Quick', payload(1), metadata(1)), /hydrat/i);
  assert.deepEqual(await store.hydrate(), []);
  await store.commit('game', 'Quick', payload(1), metadata(1));
  assert.equal((await record(store)).current.metadata, metadata(1));
});

test('a fresh browser discovers the complete payload/metadata pair and previous generation', async () => {
  const { store, make } = fixture();
  await store.hydrate();
  await store.commit('game', 'Quick', payload(1), metadata(1));
  await store.commit('game', 'Quick', payload(2), metadata(2));
  const r = await record(store);
  assert.equal(r.previous.metadata, metadata(1));
  const [loaded] = await make().hydrate();
  assert.deepEqual(new Uint8Array(loaded.payload), payload(2));
  assert.equal(loaded.metadata, metadata(2));
});

test('transaction abort after a successful put does not acknowledge or replace the checkpoint', async () => {
  const { store, make } = fixture();
  await store.hydrate();
  await store.commit('game', 'Quick', payload(1), metadata(1));
  const original = IDBObjectStore.prototype.put;
  IDBObjectStore.prototype.put = function (...args) {
    const request = original.apply(this, args);
    request.addEventListener('success', () => this.transaction.abort());
    return request;
  };
  try { await assert.rejects(store.commit('game', 'Quick', payload(2), metadata(2))); }
  finally { IDBObjectStore.prototype.put = original; }
  assert.equal((await make().hydrate())[0].metadata, metadata(1));
  await store.commit('game', 'Quick', payload(3), metadata(3));
  assert.equal((await record(store)).current.metadata, metadata(3));
});

test('two tabs cannot silently overwrite one another; conflict stays latched', async () => {
  const { store, make } = fixture();
  await store.hydrate();
  await store.commit('game', 'Quick', payload(1), metadata(1));
  const second = make(); await second.hydrate();
  await store.commit('game', 'Quick', payload(2), metadata(2));
  await assert.rejects(second.commit('game', 'Quick', payload(3), metadata(3)), /another tab/i);
  await assert.rejects(second.commit('game', 'Quick', payload(4), metadata(4)), /another tab/i);
  assert.equal((await record(store)).current.metadata, metadata(2));
});

test('corrupt newest generation recovers previous and retains it on the next commit', async () => {
  const { store, make } = fixture(); await store.hydrate();
  await store.commit('game', 'Quick', payload(1), metadata(1));
  await store.commit('game', 'Quick', payload(2), metadata(2));
  const r = await record(store); r.current.metadata = metadata(99); await replace(store, r);
  const recovered = make();
  const [loaded] = await recovered.hydrate();
  assert.equal(loaded.recovered, true);
  assert.equal(loaded.metadata, metadata(1));
  await recovered.commit('game', 'Quick', payload(3), metadata(3));
  assert.equal((await record(recovered)).previous.metadata, metadata(1));
});

test('unrecoverable or unsafe records fail hydration instead of becoming a fresh game', async () => {
  const { store, make } = fixture(); await store.hydrate();
  await store.commit('game', 'Quick', payload(1), metadata(1));
  const r = await record(store); r.current.payload = payload(99); await replace(store, r);
  await assert.rejects(make().hydrate(), /recover|corrupt/i);
  await assert.rejects(store.commit('../outside', 'Quick', payload(1), metadata(1)), /identifier/i);
});

test('caller mutation after submission cannot alter the stored bytes', async () => {
  const { store } = fixture(); await store.hydrate();
  const bytes = payload(1);
  const pending = store.commit('game', 'Quick', bytes, metadata(1));
  bytes[3] = 99;
  await pending;
  assert.deepEqual(new Uint8Array((await record(store)).current.payload), payload(1));
});
