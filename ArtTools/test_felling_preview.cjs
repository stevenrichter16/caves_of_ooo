/* Run from any directory: node /path/to/caves-of-ooo/ArtTools/test_felling_preview.cjs */
'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const {test} = require('node:test');
const {pointInPolygon, buildGeometry, neighbors, findPath, computeVisible, createMovementInput, pressMovementInput, releaseMovementInput, takeMovementIntent, clearMovementInput} = require('../ArtSource/FellingSite/preview.js');

const square = [[0, 0], [3, 0], [3, 3], [0, 3]];
const fixture = {
  grid: {zoneWidth: 3, zoneHeight: 3, regionOrigin: [0, 0], regionSize: [3, 3], artPixelsPerCell: 1, imageGroundOrigin: [0, 0]},
  spawn: [0, 0],
  walkablePolygon: square,
  blockers: [
    {id: 'east', polygon: [[1, 0], [2, 0], [2, 1], [1, 1]], blocksSight: true},
    {id: 'south', polygon: [[0, 1], [1, 1], [1, 2], [0, 2]], blocksSight: true}
  ]
};
const blocked = buildGeometry(fixture);
const clear = buildGeometry({...fixture, blockers: []});

test('polygon vertex counts as inside', () => assert.equal(pointInPolygon([0, 0], square), true));
test('polygon edge counts as inside', () => assert.equal(pointInPolygon([3, 2], square), true));
test('point beyond polygon is outside', () => assert.equal(pointInPolygon([4, 2], square), false));
test('path cannot cut diagonally between two blocked neighbors', () => assert.equal(findPath(blocked, [0, 0], [1, 1]), null));
test('path cannot end on a blocked cell', () => assert.equal(findPath(blocked, [0, 0], [1, 0]), null));
test('path to the current cell is empty and valid', () => assert.deepEqual(findPath(blocked, [0, 0], [0, 0]), []));
test('clear diagonal path is available', () => assert.deepEqual(findPath(clear, [0, 0], [2, 2]), [[1, 1], [2, 2]]));
test('sight-blocking cell hides a cell beyond it', () => assert.equal(computeVisible(blocked, [0, 0]).has('2,0'), false));
test('sight-blocking target cell itself remains visible', () => assert.equal(computeVisible(blocked, [0, 0]).has('1,0'), true));

test('a quick key-down and key-up before the next frame produces exactly one step', () => {
  const input = createMovementInput();
  pressMovementInput(input, 'up');
  releaseMovementInput(input, 'up');
  assert.deepEqual(takeMovementIntent(input), [0, -1]);
  assert.equal(takeMovementIntent(input), null, 'The tap must not become a second step.');
});

test('held movement repeats while native key-repeat adds no extra queued steps', () => {
  const input = createMovementInput();
  pressMovementInput(input, 'up');
  assert.deepEqual(takeMovementIntent(input), [0, -1], 'Initial press supplies one intent.');
  pressMovementInput(input, 'up');
  assert.deepEqual(takeMovementIntent(input), [0, -1], 'Held key supplies the next timed step.');
  releaseMovementInput(input, 'up');
  assert.equal(takeMovementIntent(input), null, 'Native autorepeat must not leave an extra tap queued.');
});

test('two separate taps between frames remain two steps', () => {
  const input = createMovementInput();
  for (let tap = 0; tap < 2; tap++) {
    pressMovementInput(input, 'right');
    releaseMovementInput(input, 'right');
  }
  assert.deepEqual(takeMovementIntent(input), [1, 0]);
  assert.deepEqual(takeMovementIntent(input), [1, 0]);
  assert.equal(takeMovementIntent(input), null);
});

test('simultaneous keys coalesce into one diagonal without bypassing blocked corners', () => {
  const input = createMovementInput();
  pressMovementInput(input, 'down');
  pressMovementInput(input, 'right');
  releaseMovementInput(input, 'down');
  releaseMovementInput(input, 'right');
  const vector = takeMovementIntent(input);
  assert.deepEqual(vector, [1, 1]);
  assert.equal(takeMovementIntent(input), null, 'The diagonal must not also queue a cardinal step.');
  assert.equal(neighbors(blocked, [0, 0]).some(item => item.cell[0] === vector[0] && item.cell[1] === vector[1]), false, 'Input intents still use the same collision and no-corner-cutting rules.');
});

test('focus loss or route cancellation clears pending taps and held directions', () => {
  const input = createMovementInput();
  pressMovementInput(input, 'left');
  clearMovementInput(input);
  assert.equal(takeMovementIntent(input), null);
});

const sceneDirectory = path.resolve(__dirname, '../ArtSource/FellingSite');
const layout = JSON.parse(fs.readFileSync(path.join(sceneDirectory, 'layout.json'), 'utf8'));
const geometry = buildGeometry(layout);

test('real scene reaches the entrance, six sterile positions, and seventh absence', () => {
  assert.equal(layout.sterilePositions.length, 6, 'The scene has six sterile positions.');
  const targets = [{id: 'entrance', cell: layout.entrance}, ...layout.sterilePositions, layout.seventh];
  assert.equal(targets.length, 8);
  for (const target of targets) {
    const route = findPath(geometry, layout.spawn, target.cell);
    assert.notEqual(route, null, `${target.id} must be reachable from spawn.`);
    let previous = layout.spawn;
    for (const cell of route) {
      assert.equal(geometry.isWalkable(cell), true, `${target.id}: step ${cell} must be walkable.`);
      const dx = Math.abs(cell[0] - previous[0]);
      const dy = Math.abs(cell[1] - previous[1]);
      assert.ok(dx <= 1 && dy <= 1 && dx + dy > 0, `${target.id}: steps must be adjacent.`);
      if (dx && dy) {
        assert.equal(geometry.isWalkable([cell[0], previous[1]]), true, `${target.id}: diagonal's first orthogonal cell must be walkable.`);
        assert.equal(geometry.isWalkable([previous[0], cell[1]]), true, `${target.id}: diagonal's second orthogonal cell must be walkable.`);
      }
      previous = cell;
    }
    assert.deepEqual(previous, target.cell, `${target.id}: route must finish at the target.`);
  }
});

test('browser occupancy exactly matches every cell in the prepared Python export', context => {
  const filename = path.join(sceneDirectory, 'Prepared/occupancy.json');
  if (!fs.existsSync(filename)) {
    context.skip('Prepared/occupancy.json does not exist yet; generate it with ArtTools/felling_scene.py.');
    return;
  }
  const prepared = JSON.parse(fs.readFileSync(filename, 'utf8'));
  assert.deepEqual(prepared.regionOrigin, layout.grid.regionOrigin);
  assert.deepEqual(prepared.regionSize, layout.grid.regionSize);
  const [originX, originY] = layout.grid.regionOrigin;
  const [width, height] = layout.grid.regionSize;
  const browserRows = Array.from({length: height}, (_, y) => Array.from({length: width}, (_, x) => Number(geometry.isWalkable([originX + x, originY + y]))));
  assert.deepEqual(browserRows, prepared.rows, 'Browser and Python must agree cell for cell, not just on the count.');
});
