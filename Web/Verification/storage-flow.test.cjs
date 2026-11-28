// Executes the production UI coordinator with DOM/engine/storage adapters.
// This is a JS contract test, not a real-browser or WebAssembly acceptance test.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { gzipSync } = require('node:zlib');
const { webcrypto } = require('node:crypto');

function deferred() {
  let resolve, reject;
  const promise = new Promise((yes, no) => { resolve = yes; reject = no; });
  return { promise, resolve, reject };
}
function element() {
  return {
    hidden: true, disabled: false, textContent: '', value: '', style: {}, dataset: {},
    firstChild: { textContent: '' }, classList: { add() {}, remove() {}, toggle() {} },
    append() {}, replaceChildren() {}, after() {}, click() {}, setAttribute() {},
    addEventListener() {}, getBoundingClientRect: () => ({ left: 0, top: 0 }),
  };
}
function checkpoint(turn = 100) {
  const raw = Buffer.from('simulation-checkpoint-' + turn);
  return { gameId: 'expedition', slot: 'Quick', payload: new Uint8Array(gzipSync(raw)),
    metadata: JSON.stringify({ GameID: 'expedition', SaveVersion: 8, SavedAt: '2026-11-04T00:00:00Z', Tick: turn }) };
}
function store(records = []) {
  return { closes: 0, commits: [], records,
    async open() { return { close: () => { this.closes++; } }; },
    async hydrate() { return this.records; },
    async commit(...args) { this.commits.push(args); },
  };
}
function harness(initialStore, candidates = []) {
  const elements = new Map(), documentHandlers = {}, windowHandlers = {}, downloads = [];
  const preferences = new Map();
  const document = {
    hidden: false, body: element(),
    getElementById(id) { if (!elements.has(id)) elements.set(id, element()); return elements.get(id); },
    createElement: () => element(), querySelector: () => element(), querySelectorAll: () => [],
    addEventListener: (name, handler) => { documentHandlers[name] = handler; },
  };
  const stores = [initialStore, ...candidates];
  const context = {
    document, navigator: {}, crypto: webcrypto, console, Blob, Response, CompressionStream, DecompressionStream,
    TextEncoder, TextDecoder, Uint8Array, Uint32Array, ArrayBuffer, URL: {
      createObjectURL: blob => { downloads.push(blob); return 'blob:test'; }, revokeObjectURL() {},
    }, btoa, atob, setTimeout: () => 1, clearTimeout() {},
    localStorage: { setItem: (key, value) => preferences.set(key, value), getItem: key => preferences.get(key) ?? null },
    window: { addEventListener: (name, handler) => { windowHandlers[name] = handler; }, scrollTo() {} },
    WorldMapView: class { draw() {} set() {} center() {} zoom() {} pan() {} },
    imagesReady: Promise.resolve(), drawOpening() {},
    CooBrowserSaveStore: function () { const next = stores.shift(); if (!next) throw new Error('Unexpected storage reader'); return next; },
    Blazor: { start: async () => {} }, DotNet: { invokeMethodAsync: async () => { throw new Error('Engine adapter not set'); } },
  };
  // Keep production coordinator functions intact. Prevent only its automatic
  // startup side effect, then expose closure state to the test adapter.
  const source = fs.readFileSync(path.join(__dirname, '../Client/wwwroot/app.js'), 'utf8')
    .replace(/^import .*from '\.\/map\.js';\s*$/m, '')
    .replace(/^openStorage\(\);\s*$/m, '');
  vm.runInNewContext(source + `
    globalThis.flow = {
      act, saveCheckpoint, continueGame, exportBackup,
      set(initial, snapshot) { state = initial; checkpoints = snapshot; storageReady = true; dirty = 0; lastSaveTick = initial.tick; },
      configure(method) { invoke = method; runtime = async () => {}; playState = next => { state = typeof next === 'string' ? JSON.parse(next) : next; }; render = () => {}; },
      clearSession() { state = null; },
      setDirty(value) { dirty = value; },
      inspect() { return { state, store, checkpoints, storageReady, dirty, busy, saving }; }
    };`, context, { filename: 'app.js' });
  return { flow: context.flow, document, documentHandlers, windowHandlers, elements, downloads, preferences };
}
function state(tick = 100) {
  return { gameId: 'expedition', tick, revision: tick, zone: '1.1.1.0', name: 'Wayfarer', build: 'classic', dead: false, faulted: false, error: '' };
}
const settle = async () => { await new Promise(resolve => setImmediate(resolve)); };

test('failed Restore retains the live store revision owner and closes only the candidate reader', async () => {
  const previousRecords = [checkpoint(100)], original = store(previousRecords), candidate = store([checkpoint(200)]);
  const h = harness(original, [candidate]); const before = state(); h.flow.set(before, previousRecords);
  h.flow.configure(async method => { if (method === 'Restore') throw new Error('Invalid expedition graph'); throw new Error('Unexpected ' + method); });
  await h.flow.continueGame('expedition');
  assert.equal(h.flow.inspect().state, before);
  assert.equal(h.flow.inspect().store, original, 'Failed load must not refresh the expected IndexedDB revision of old gameplay.');
  assert.equal(h.flow.inspect().checkpoints, previousRecords);
  assert.equal(original.closes, 0); assert.equal(candidate.closes, 1);
  assert.equal(h.flow.inspect().busy, false);
});

test('successful Restore publishes the candidate store and closes the previous connection', async () => {
  const original = store([checkpoint(100)]), candidate = store([checkpoint(200)]);
  const h = harness(original, [candidate]); h.flow.set(state(), original.records);
  h.flow.configure(async method => { assert.equal(method, 'Restore'); return state(200); });
  await h.flow.continueGame('expedition');
  assert.equal(h.flow.inspect().state.tick, 200);
  assert.equal(h.flow.inspect().store, candidate);
  assert.equal(original.closes, 1); assert.equal(candidate.closes, 0);
  assert.equal(h.flow.inspect().dirty, 0);
});

test('pagehide during a pending action requests a checkpoint after the action settles', async () => {
  const original = store(), dispatch = deferred(); const h = harness(original); h.flow.set(state(), []);
  h.flow.configure(async method => {
    if (method === 'Dispatch') return dispatch.promise;
    if (method === 'Checkpoint') return new Uint8Array([1, 2, 3, 4]);
    throw new Error('Unexpected ' + method);
  });
  const acting = h.flow.act('step', { x: 1, y: 0 });
  assert.equal(h.flow.inspect().busy, true);
  h.document.hidden = true; h.windowHandlers.pagehide();
  dispatch.resolve(state(110)); await acting; await settle();
  if (h.flow.inspect().saving) await h.flow.inspect().saving;
  assert.equal(original.commits.length, 1, 'A background request cannot disappear merely because an action owned busy at that instant.');
  assert.equal(JSON.parse(original.commits[0][3]).Tick, 110);
  assert.equal(h.flow.inspect().dirty, 0);
});

test('a failed save retains dirty state and the last acknowledged checkpoint', async () => {
  const oldRecords = [checkpoint(90)], original = store(oldRecords); original.commit = async () => { throw new Error('Quota exceeded'); };
  const h = harness(original); h.flow.set(state(), oldRecords); h.flow.setDirty(3);
  h.flow.configure(async method => { assert.equal(method, 'Checkpoint'); return new Uint8Array([1, 2, 3, 4]); });
  await assert.rejects(h.flow.saveCheckpoint(), /Quota exceeded/);
  assert.equal(h.flow.inspect().dirty, 3); assert.equal(h.flow.inspect().saving, null);
  assert.equal(h.flow.inspect().checkpoints, oldRecords);
  assert.match(h.elements.get('save-state').textContent, /failed/i);
  assert.doesNotMatch(h.elements.get('save-state').textContent, /Saved on this device/);
});

test('export after storage failure captures current gameplay directly without committing', async () => {
  const original = store(); original.commit = async () => { throw new Error('Quota exceeded'); };
  const h = harness(original); h.flow.set(state(), []); h.flow.setDirty(3);
  h.flow.configure(async method => { assert.equal(method, 'Checkpoint'); return new Uint8Array([1, 2, 3, 4]); });
  await assert.rejects(h.flow.saveCheckpoint(), /Quota exceeded/);
  await h.flow.exportBackup();
  assert.equal(h.downloads.length, 1);
  const backup = JSON.parse(await h.downloads[0].text());
  assert.equal(backup.format, 'caves-of-ooo-backup');
  assert.equal(backup.checkpoints[0].gameId, 'expedition');
  assert.equal(JSON.parse(backup.checkpoints[0].metadata).Tick, 100);
  assert.equal(h.flow.inspect().dirty, 3, 'Export does not mean IndexedDB acknowledged a checkpoint.');
});

test('import cannot replace durable records when a later full simulation graph is rejected', async () => {
  const oldRecords = [checkpoint(90)], original = store(oldRecords); let imports = 0, restores = 0;
  original.importBackup = async () => { imports++; };
  const h = harness(original); h.flow.set(state(), oldRecords); h.flow.clearSession();
  h.flow.configure(async method => {
    assert.equal(method, 'Restore');
    if (++restores === 2) throw new Error('Incomplete expedition graph');
    return JSON.stringify({ gameId: 'expedition' });
  });
  const good = checkpoint(100);
  const bundle = { format: 'caves-of-ooo-backup', version: 1, buildId: 'standalone-web-1', checkpoints: [
    { gameId: 'expedition', slot: 'Quick', payloadBase64: Buffer.from(good.payload).toString('base64'), metadata: good.metadata },
    { gameId: 'second', slot: 'Quick', payloadBase64: Buffer.from(good.payload).toString('base64'), metadata: good.metadata },
  ] };
  const target = { files: [{ size: 1000, text: async () => JSON.stringify(bundle) }], value: 'selected-file' };
  await h.elements.get('import-file').onchange({ target });
  assert.equal(restores, 2); assert.equal(imports, 0);
  assert.equal(h.flow.inspect().checkpoints, oldRecords);
  assert.equal(h.flow.inspect().state, null);
  assert.equal(h.flow.inspect().busy, false);
  assert.match(h.elements.get('startup-status').textContent, /Incomplete expedition graph/);
  assert.equal(target.value, '');
});

for (const explicit of [false, true]) {
  test(explicit ? 'explicit Continue identity overrides the last-game preference' : 'Continue restores the last played campaign despite another checkpoint having a future timestamp', async () => {
    const active = checkpoint(100), future = checkpoint(200);
    active.gameId = 'active'; future.gameId = 'future-old';
    active.metadata = JSON.stringify({ GameID: 'active', SaveVersion: 8, SavedAt: '2026-11-04T00:00:00Z' });
    future.metadata = JSON.stringify({ GameID: 'future-old', SaveVersion: 8, SavedAt: '2099-11-04T00:00:00Z' });
    const original = store(), candidate = store([future, active]), h = harness(original, [candidate]);
    h.flow.set(state(), []); h.flow.clearSession(); h.preferences.set('ooo.last-game', 'active');
    let restoredPayload;
    h.flow.configure(async (method, raw) => {
      assert.equal(method, 'Restore'); restoredPayload = new TextDecoder().decode(raw);
      const tick = Number(restoredPayload.split('-').pop());
      return { ...state(tick), gameId: tick === 100 ? 'active' : 'future-old' };
    });
    await h.flow.continueGame(explicit ? 'future-old' : undefined);
    assert.equal(restoredPayload, explicit ? 'simulation-checkpoint-200' : 'simulation-checkpoint-100');
    assert.equal(h.flow.inspect().state.gameId, explicit ? 'future-old' : 'active');
  });
}
