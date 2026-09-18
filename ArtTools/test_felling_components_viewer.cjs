'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const {pathToFileURL, fileURLToPath} = require('node:url');
const {test} = require('node:test');
const {validateManifest, resolveAssetUrl, createReviewState, reduceReviewState, getDrawOrder, hitTestComponents, clientToScene, compareSpriteToBaseline, intersectInferenceWithAlpha} = require('../ArtSource/FellingSite/Components/viewer.js');

const hash = 'a'.repeat(64);
function component(id, overrides = {}) {
  return {id, name: id, kind: 'rock', mutable: true, interaction: 'review', depth: 1, bounds: [1, 1, 2, 2], foot: [2, 3], pivot: [0.5, 1], footprintCells: [[0, 0]], sprite: {path: `${id}.png`, sha256: hash}, contact: null, repair: {maskPath: `${id}-repair.png`, surface: 'ground', inferred: true}, extraction: {method: 'source-mask', quality: 'draft', notes: []}, defaultVisible: true, contributesToComposite: true, ...overrides};
}
function manifest() {
  return {schemaVersion: 1, id: 'fixture', canvas: {width: 4, height: 4, pixelsPerCell: 32, imageGroundOrigin: [0, 0], regionOrigin: [0, 0]}, reference: {path: '../reference.png', sha256: hash}, baseline: {path: 'baseline.png', sha256: hash}, base: {path: 'base.png', sha256: hash, inferenceMaskPath: 'inference.png', retainsStatic: true}, report: {path: 'report.json'}, components: [component('b'), component('a'), component('static', {mutable: false, depth: 2}), component('study', {mutable: false, contributesToComposite: false, depth: 10})]};
}

test('valid synthetic manifest is accepted without changing its input', () => {
  const value = manifest(); const before = JSON.stringify(value);
  assert.equal(validateManifest(value), value);
  assert.equal(JSON.stringify(value), before);
});
test('invalid dimensions, duplicate ids, and out-of-canvas bounds are rejected', () => {
  const dimensions = manifest(); dimensions.canvas.width = 0;
  assert.throws(() => validateManifest(dimensions), /canvas/i);
  const duplicate = manifest(); duplicate.components[1].id = 'b';
  assert.throws(() => validateManifest(duplicate), /duplicate/i);
  const bounds = manifest(); bounds.components[0].bounds = [3, 3, 2, 2];
  assert.throws(() => validateManifest(bounds), /bounds/i);
});
test('missing contribution flag and invalid default visibility cannot silently change composition', () => {
  const value = manifest(); delete value.components[0].contributesToComposite;
  assert.throws(() => validateManifest(value), /contributesToComposite/);
  value.components[0].contributesToComposite = true; value.components[0].defaultVisible = 'yes';
  assert.throws(() => validateManifest(value), /defaultVisible/);
});
test('manifest paths resolve relative to the manifest URL, not the viewer page', () => {
  assert.equal(resolveAssetUrl('../reference.png', 'http://localhost:8765/Components/build/manifest.json'), 'http://localhost:8765/Components/reference.png');
  assert.throws(() => resolveAssetUrl('https://elsewhere.invalid/a.png', 'http://localhost/manifest.json'), /relative/i);
  assert.throws(() => resolveAssetUrl('javascript:alert(1)', 'http://localhost/manifest.json'), /relative/i);
});
test('composition uses contributing components in ascending depth and stable id order', () => {
  const data = manifest(); const state = createReviewState(data);
  assert.deepEqual(getDrawOrder(data, state).map(item => item.id), ['a', 'b', 'static']);
});
test('hide changes one layer and leaves the previous state intact', () => {
  const data = manifest(); const before = createReviewState(data);
  const after = reduceReviewState(data, before, {type: 'TOGGLE_VISIBILITY', id: 'a'});
  assert.equal(after.hidden.has('a'), true);
  assert.equal(before.hidden.has('a'), false);
  assert.deepEqual(getDrawOrder(data, after).map(item => item.id), ['b', 'static']);
  assert.equal(reduceReviewState(data, after, {type: 'TOGGLE_VISIBILITY', id: 'a'}).hidden.has('a'), false);
});
test('remove-all-mutable preserves static layers and retained study cutouts', () => {
  const data = manifest(); const after = reduceReviewState(data, createReviewState(data), {type: 'REMOVE_MUTABLE'});
  assert.deepEqual([...after.hidden].sort(), ['a', 'b']);
  assert.deepEqual(getDrawOrder(data, after).map(item => item.id), ['static']);
});
test('retained-in-base cutouts cannot be hidden but can be selected and inspected solo', () => {
  const data = manifest(); const state = createReviewState(data);
  assert.throws(() => reduceReviewState(data, state, {type: 'TOGGLE_VISIBILITY', id: 'study'}), /retained/i);
  const solo = reduceReviewState(data, state, {type: 'SOLO', id: 'study'});
  assert.equal(solo.soloId, 'study');
  assert.equal(solo.selectedId, 'study');
});
test('solo does not discard visibility changes, and restore-all restores authored defaults', () => {
  const data = manifest(); data.components[0].defaultVisible = false;
  let state = reduceReviewState(data, createReviewState(data), {type: 'TOGGLE_VISIBILITY', id: 'a'});
  state = reduceReviewState(data, state, {type: 'SOLO', id: 'static'});
  state = reduceReviewState(data, state, {type: 'SOLO', id: 'static'});
  assert.equal(state.hidden.has('a'), true);
  const restored = reduceReviewState(data, state, {type: 'RESTORE_ALL'});
  assert.deepEqual([...restored.hidden], ['b']);
  assert.equal(restored.soloId, null);
});
test('changing source comparison preserves hidden layers and exits solo', () => {
  const data = manifest(); let state = reduceReviewState(data, createReviewState(data), {type: 'TOGGLE_VISIBILITY', id: 'a'});
  state = reduceReviewState(data, state, {type: 'SOLO', id: 'static'});
  state = reduceReviewState(data, state, {type: 'SET_VIEW', view: 'reference'});
  assert.equal(state.hidden.has('a'), true);
  assert.equal(state.soloId, null);
  assert.equal(state.view, 'reference');
});
test('selecting another layer during solo keeps displayed and hit-tested selection consistent', () => {
  const data = manifest(); let state = reduceReviewState(data, createReviewState(data), {type: 'SOLO', id: 'a'});
  state = reduceReviewState(data, state, {type: 'SELECT', id: 'b'});
  assert.equal(state.selectedId, 'b');
  assert.equal(state.soloId, 'b');
});
test('exploded review exits native mode so illustrative scale is never labeled one-to-one', () => {
  const data = manifest(); let state = reduceReviewState(data, createReviewState(data), {type: 'SET_OPTION', key: 'actualSize', value: true});
  state = reduceReviewState(data, state, {type: 'SET_OPTION', key: 'exploded', value: true});
  assert.equal(state.exploded, true);
  assert.equal(state.actualSize, false);
});
test('invalid selection/action and invalid view are rejected', () => {
  const data = manifest(); const state = createReviewState(data);
  assert.throws(() => reduceReviewState(data, state, {type: 'SELECT', id: 'missing'}), /component/i);
  assert.throws(() => reduceReviewState(data, state, {type: 'SET_VIEW', view: 'gameplay'}), /view/i);
  assert.throws(() => reduceReviewState(data, state, {type: 'DELETE_FILES'}), /action/i);
});
test('actual sprite alpha selects the top layer and lets transparent holes reveal lower layers', () => {
  const data = manifest(); const state = createReviewState(data);
  const alpha = new Map([['a', new Uint8Array([255, 255, 255, 255])], ['b', new Uint8Array([255, 255, 255, 255])], ['static', new Uint8Array([0, 255, 0, 0])], ['study', new Uint8Array([255, 255, 255, 255])]]);
  assert.deepEqual(hitTestComponents(data, state, [1.2, 1.2], alpha).map(item => item.id), ['b', 'a']);
  assert.equal(hitTestComponents(data, state, [2.2, 1.2], alpha)[0].id, 'static');
  assert.deepEqual(hitTestComponents(data, state, [3, 2], alpha), [], 'Right edge is outside a local 2px sprite.');
});
test('hidden components cannot steal clicks in reconstruction', () => {
  const data = manifest(); const state = reduceReviewState(data, createReviewState(data), {type: 'TOGGLE_VISIBILITY', id: 'static'});
  const alpha = new Map([['a', new Uint8Array(4).fill(255)], ['b', new Uint8Array(4).fill(255)], ['static', new Uint8Array(4).fill(255)]]);
  assert.equal(hitTestComponents(data, state, [2, 2], alpha)[0].id, 'b');
});
test('pointer mapping is correct in fitted and scrolled native canvases', () => {
  assert.deepEqual(clientToScene([250, 150], {left: 100, top: 50, width: 768, height: 512}, [1536, 1024]), [300, 200]);
  assert.deepEqual(clientToScene([150, 100], {left: -250, top: -300, width: 1536, height: 1024}, [1536, 1024]), [400, 400]);
});
test('source fidelity compares opaque RGB samples at the authored offset and reports mismatches', () => {
  const baseline = new Uint8ClampedArray(4 * 4 * 4);
  baseline.set([20, 30, 40, 255], (1 * 4 + 1) * 4);
  baseline.set([50, 60, 70, 255], (1 * 4 + 2) * 4);
  const sprite = new Uint8ClampedArray([20, 30, 40, 255, 50, 61, 70, 255, 99, 99, 99, 0, 20, 30, 40, 128]);
  assert.deepEqual(compareSpriteToBaseline(sprite, baseline, [1, 1, 2, 2], 4), {opaquePixels: 2, matchingPixels: 1, differentPixels: 1, partialAlphaPixels: 1});
});
test('inference coverage follows local sprite support and does not color transparent diagnostic holes', () => {
  const global = new Uint8Array(16); global[5] = 255; global[6] = 255; global[9] = 0; global[10] = 255;
  const local = new Uint8Array([255, 0, 255, 128]);
  assert.deepEqual([...intersectInferenceWithAlpha(global, local, [1, 1, 2, 2], 4)], [255, 0, 0, 128]);
});

test('current exported build obeys the manifest and local PNG dimension contract when present', context => {
  const filename = path.resolve(__dirname, '../ArtSource/FellingSite/Components/build/manifest.json');
  if (!fs.existsSync(filename)) { context.skip('Actual build has not been exported yet; synthetic contract checks remain active.'); return; }
  const data = validateManifest(JSON.parse(fs.readFileSync(filename, 'utf8')));
  const manifestUrl = pathToFileURL(filename).href;
  const inspect = (assetPath, dimensions) => {
    const target = fileURLToPath(resolveAssetUrl(assetPath, manifestUrl));
    assert.equal(fs.existsSync(target), true, `Declared asset exists: ${assetPath}`);
    const bytes = fs.readFileSync(target);
    assert.deepEqual([...bytes.subarray(0, 8)], [137, 80, 78, 71, 13, 10, 26, 10], `Asset is a PNG: ${assetPath}`);
    assert.deepEqual([bytes.readUInt32BE(16), bytes.readUInt32BE(20)], dimensions, `PNG dimensions match authored bounds: ${assetPath}`);
  };
  const canvasSize = [data.canvas.width, data.canvas.height];
  for (const asset of [data.reference, data.baseline, data.base]) inspect(asset.path, canvasSize);
  inspect(data.base.inferenceMaskPath, canvasSize);
  for (const item of data.components) {
    inspect(item.sprite.path, item.bounds.slice(2));
    if (item.contact) inspect(item.contact.path, item.bounds.slice(2));
    inspect(item.repair.maskPath, item.bounds.slice(2));
  }
  const reportPath = fileURLToPath(resolveAssetUrl(data.report.path, manifestUrl));
  assert.equal(fs.existsSync(reportPath), true, 'Declared build report exists.');
  assert.equal(JSON.parse(fs.readFileSync(reportPath, 'utf8')).componentCount, data.components.length);
});
