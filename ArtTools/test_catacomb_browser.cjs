"use strict";
const { test } = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const core = require("../ArtSource/CatacombVillage/scene.js");
const cells = Array.from({ length: 25 }, (_, i) => [i % 5, Math.floor(i / 5)]);
const component = (id, cell, mutable = true) => ({
  id,
  name: id,
  kind: "prop",
  mutable,
  defaultVisible: true,
  bounds: [cell[0] * 32, cell[1] * 32, 32, 32],
  depth: cell[1] * 32,
  foot: [cell[0] * 32 + 16, cell[1] * 32 + 31],
  sprite: { path: id + ".png" },
  contact: null,
  mask: null,
  footprintCells: [cell],
  blocksMovement: true,
  notes: [],
});
const fixture = () => ({
  schemaVersion: 1,
  id: "test",
  title: "Test",
  canvas: { width: 160, height: 160, pixelsPerCell: 32 },
  base: { path: "base.png" },
  baseline: { path: "baseline.png" },
  underlay: { path: "underlay.png" },
  inferenceMask: { path: "inference.png" },
  navigation: { width: 5, height: 5, walkableCells: cells, spawn: [0, 0] },
  components: [
    component("crate", [1, 0]),
    component("rock", [0, 1]),
    component("wall", [4, 4], false),
  ],
});
const init = (f = fixture()) => core.createState(core.validateScene(f));
const action = (s, a, f = fixture()) => core.reduceState(f, s, a);
test("valid manifest accepts explicit dimensions and navigation", () =>
  assert.equal(core.validateScene(fixture()).id, "test"));
for (const [label, mutate] of [
  ["duplicate ids", (f) => f.components.push({ ...f.components[0] })],
  ["outside footprint", (f) => (f.components[0].footprintCells = [[5, 0]])],
  [
    "out-of-canvas rectangle",
    (f) => (f.components[0].bounds = [159, 0, 32, 32]),
  ],
  ["unsafe asset URL", (f) => (f.base.path = "https://example.com/a.png")],
  ["path traversal", (f) => (f.base.path = "../a.png")],
  ["fractional cell", (f) => (f.navigation.spawn = [0.1, 0])],
  ["blocked spawn", (f) => (f.navigation.spawn = [1, 0])],
  ["wrong dimensions", (f) => (f.navigation.width = 4)],
  ["invalid mutable type", (f) => (f.components[0].mutable = "true")],
])
  test("rejects " + label, () => {
    const f = fixture();
    mutate(f);
    assert.throws(() => core.validateScene(f));
  });
test("initial state independently owns hidden sets and player coordinates", () => {
  const a = init(),
    b = init();
  a.hidden.add("crate");
  a.player[0] = 3;
  assert.equal(b.hidden.size, 0);
  assert.deepEqual(b.player, [0, 0]);
});
test("blocked orthogonals forbid diagonal corner cutting", () =>
  assert.equal(core.findPath(fixture(), init(), [0, 0], [1, 1]), null));
test("removal opens only that prop occupancy", () => {
  const s = action(init(), { type: "REMOVE", ids: ["crate"] });
  assert.equal(core.isWalkable(fixture(), s, [1, 0]), true);
  assert.equal(core.isWalkable(fixture(), s, [0, 1]), false);
  assert.equal(core.isWalkable(fixture(), s, [4, 4]), false);
});
test("opening one orthogonal does not grant a corner diagonal", () => {
  const s = action(init(), { type: "REMOVE", ids: ["crate"] });
  assert.equal(
    core.neighbors(fixture(), s, [0, 0]).some((c) => c[0] === 1 && c[1] === 1),
    false,
  );
  assert.deepEqual(core.findPath(fixture(), s, [0, 0], [1, 1]), [
    [1, 0],
    [1, 1],
  ]);
});
test("opening both orthogonals allows direct diagonal", () => {
  const s = action(init(), { type: "REMOVE", ids: ["crate", "rock"] });
  assert.deepEqual(core.findPath(fixture(), s, [0, 0], [1, 1]), [[1, 1]]);
});
test("route cannot enter fixed obstacle or outside navigation", () => {
  const s = action(init(), { type: "REMOVE_ALL" });
  assert.equal(core.findPath(fixture(), s, [0, 0], [4, 4]), null);
  assert.equal(core.findPath(fixture(), s, [0, 0], [-1, 0]), null);
  assert.deepEqual(core.findPath(fixture(), s, [0, 0], [0, 0]), []);
});
test("unknown and fixed removals fail atomically", () => {
  const s = init();
  assert.throws(() => action(s, { type: "REMOVE", ids: ["crate", "missing"] }));
  assert.throws(() => action(s, { type: "REMOVE", ids: ["crate", "wall"] }));
  assert.equal(s.hidden.size, 0);
});
test("unknown or fixed restoration also rejected", () => {
  const s = action(init(), { type: "REMOVE_ALL" });
  assert.throws(() => action(s, { type: "RESTORE", ids: ["unknown"] }));
  assert.throws(() => action(s, { type: "RESTORE", ids: ["wall"] }));
});
test("repeated removal/restoration is idempotent", () => {
  let s = action(init(), { type: "REMOVE", ids: ["crate", "crate"] });
  s = action(s, { type: "REMOVE", ids: ["crate"] });
  assert.deepEqual([...s.hidden], ["crate"]);
  s = action(s, { type: "RESTORE", ids: ["crate"] });
  s = action(s, { type: "RESTORE", ids: ["crate"] });
  assert.equal(s.hidden.size, 0);
});
test("restore rejects player occupancy without changing another hidden object", () => {
  let s = action(init(), { type: "REMOVE_ALL" });
  s = action(s, { type: "MOVE", delta: [1, 0] });
  assert.throws(() => action(s, { type: "RESTORE_ALL" }), /occupied/i);
  assert.equal(s.hidden.has("crate"), true);
  assert.equal(s.hidden.has("rock"), true);
  s = action(s, { type: "MOVE", delta: [0, 1] });
  assert.equal(action(s, { type: "RESTORE_ALL" }).hidden.size, 0);
});
test("nonblocking prop can restore while player shares cell", () => {
  const f = fixture();
  f.components[0].blocksMovement = false;
  let s = action(init(f), { type: "REMOVE", ids: ["crate"] }, f);
  s = action(s, { type: "MOVE", delta: [1, 0] }, f);
  assert.equal(
    action(s, { type: "RESTORE", ids: ["crate"] }, f).hidden.size,
    0,
  );
});
test("hidden overlapping props retain occupancy until every blocker removed", () => {
  const f = fixture();
  f.components.push(component("second-crate", [1, 0]));
  const s = action(init(f), { type: "REMOVE", ids: ["crate"] }, f);
  assert.equal(core.isWalkable(f, s, [1, 0]), false);
});
test("walk step cannot bypass collision and rejects nonadjacent vectors", () => {
  assert.throws(
    () => action(init(), { type: "MOVE", delta: [1, 0] }),
    /blocked/i,
  );
  assert.throws(() => action(init(), { type: "MOVE", delta: [2, 0] }));
  assert.throws(() => action(init(), { type: "MOVE", delta: [0, 0] }));
});
test("inspect mode forbids movement and clears queued route", () => {
  let s = action(init(), { type: "REMOVE_ALL" });
  s = action(s, { type: "ROUTE", target: [3, 3] });
  assert.ok(s.route.length);
  s = action(s, { type: "MODE", mode: "inspect" });
  assert.equal(s.route.length, 0);
  assert.throws(() => action(s, { type: "MOVE", delta: [1, 0] }), /walk/i);
});
test("baseline / underlay comparison forbids walking until reconstructed restored", () => {
  const s = action(init(), { type: "VIEW", view: "baseline" });
  assert.throws(() => action(s, { type: "MOVE", delta: [1, 0] }), /walk/i);
  assert.equal(action(s, { type: "MODE", mode: "walk" }).view, "reconstructed");
});
test("changing prop occupancy clears previously computed route", () => {
  let s = action(init(), { type: "REMOVE_ALL" });
  s = action(s, { type: "ROUTE", target: [3, 3] });
  assert.ok(s.route.length);
  s = action(s, { type: "RESTORE", ids: ["crate"] });
  assert.equal(s.route.length, 0);
});
test("route step revalidates occupancy even for stale route", () => {
  const s = init();
  s.route = [[1, 0]];
  assert.throws(() => action(s, { type: "TICK" }), /blocked/i);
  assert.deepEqual(s.player, [0, 0]);
});
test("review selections cannot modify immutable navigation or input state", () => {
  const s = init();
  const n = action(s, { type: "SELECT", id: "crate" });
  assert.equal(n.selectedId, "crate");
  assert.equal(s.selectedId, null);
  assert.equal(core.isWalkable(fixture(), n, [1, 0]), false);
  assert.throws(() => action(s, { type: "SELECT", id: "missing" }));
});
test("solo/exploded imply inspect and cannot leave invisible player moving", () => {
  const s = action(init(), { type: "SOLO", id: "crate" });
  assert.equal(s.mode, "inspect");
  assert.throws(() => action(s, { type: "MOVE", delta: [1, 0] }));
  const n = action(init(), { type: "OPTION", key: "exploded", value: true });
  assert.equal(n.mode, "inspect");
});
test("alpha selection skips transparent pixels and prefers frontmost visible sprite", () => {
  const f = fixture();
  f.components[1].bounds = f.components[0].bounds;
  f.components[1].depth = 99;
  const alpha = new Map([
    ["crate", new Uint8Array(1024).fill(255)],
    ["rock", new Uint8Array(1024)],
  ]);
  const s = init(f);
  assert.deepEqual(
    core.hitTest(f, s, [33, 1], alpha).map((c) => c.id),
    ["crate"],
  );
  alpha.get("rock")[33] = 255;
  assert.deepEqual(
    core.hitTest(f, s, [33, 1], alpha).map((c) => c.id),
    ["rock", "crate"],
  );
  s.hidden.add("rock");
  assert.deepEqual(
    core.hitTest(f, s, [33, 1], alpha).map((c) => c.id),
    ["crate"],
  );
});
test("pixel comparison detects alpha and RGB drift", () => {
  assert.deepEqual(
    core.comparePixels(
      new Uint8Array([1, 2, 3, 255]),
      new Uint8Array([1, 2, 3, 255]),
    ),
    { changedPixels: 0, maxChannelDifference: 0 },
  );
  assert.equal(
    core.comparePixels(
      new Uint8Array([1, 2, 3, 0]),
      new Uint8Array([1, 2, 3, 255]),
    ).changedPixels,
    1,
  );
  assert.throws(() => core.comparePixels(new Uint8Array(4), new Uint8Array(8)));
});
test("short taps produce one move and native repeat adds no queued move", () => {
  const input = core.createInput();
  core.pressInput(input, "right");
  core.pressInput(input, "right");
  core.releaseInput(input, "right");
  assert.deepEqual(core.takeInput(input), [1, 0]);
  assert.equal(core.takeInput(input), null);
});
test("two taps remain two moves and focus-loss clears both held and queued", () => {
  const input = core.createInput();
  for (let n = 0; n < 2; n++) {
    core.pressInput(input, "down");
    core.releaseInput(input, "down");
  }
  assert.deepEqual(core.takeInput(input), [0, 1]);
  assert.deepEqual(core.takeInput(input), [0, 1]);
  core.pressInput(input, "left");
  core.clearInput(input);
  assert.equal(core.takeInput(input), null);
});
test("opposing held keys cancel rather than choose an arbitrary axis", () => {
  const input = core.createInput();
  core.pressInput(input, "left");
  core.pressInput(input, "right");
  assert.equal(core.takeInput(input), null);
});
const built = path.resolve(
  __dirname,
  "../ArtSource/CatacombVillage/build/scene.json",
);
if (fs.existsSync(built))
  test("actual scene validates and named waypoints are reachable", () => {
    const f = core.validateScene(JSON.parse(fs.readFileSync(built)));
    const s = core.createState(f);
    const targets = f.navigation.destinations || f.navigation.waypoints || [];
    assert.ok(targets.length > 0, "Actual scene must declare destinations");
    for (const waypoint of targets) {
      const target = waypoint.cell || waypoint;
      assert.notEqual(
        core.findPath(f, s, s.player, target),
        null,
        `waypoint ${waypoint.id || target} reachable`,
      );
    }
    assert.ok(f.components.some((c) => c.mutable && c.blocksMovement));
  });

test("underlay hit testing excludes removed props but still selects fixed architecture", () => {
  const f = fixture(),
    s = action(init(f), { type: "VIEW", view: "underlay" }, f),
    alpha = new Map(
      f.components.map((c) => [c.id, new Uint8Array(1024).fill(255)]),
    );
  assert.deepEqual(core.hitTest(f, s, [33, 1], alpha), []);
  assert.equal(core.hitTest(f, s, [129, 129], alpha)[0].id, "wall");
});
test("baseline picking uses intact default visibility independent of local removals", () => {
  const f = fixture(),
    s = action(
      action(init(f), { type: "REMOVE_ALL" }, f),
      { type: "VIEW", view: "baseline" },
      f,
    ),
    alpha = new Map(
      f.components.map((c) => [c.id, new Uint8Array(1024).fill(255)]),
    );
  assert.equal(core.hitTest(f, s, [33, 1], alpha)[0].id, "crate");
});
test("encoded traversal cannot escape the scene asset folder", () => {
  const f = fixture();
  f.base.path = "%2e%2e/other.png";
  assert.throws(() => core.validateScene(f));
});
test("missing/nonfinite player scale cannot silently hide the traveler", () => {
  for (const scale of [undefined, NaN, Infinity, 0, -1]) {
    const f = fixture();
    f.player = {
      path: "player.png",
      frameWidth: 32,
      frameHeight: 48,
      columns: 1,
      frameCount: 1,
      pivot: [0.5, 1],
      scale,
    };
    assert.throws(() => core.validateScene(f));
  }
});
test("cell masks cannot silently contain duplicates", () => {
  const f = fixture();
  f.navigation.walkableCells = [...cells, [0, 0]];
  assert.throws(() => core.validateScene(f));
});
test("state changes never mutate navigation or component footprints", () => {
  const f = fixture(),
    before = JSON.stringify(f);
  let s = init(f);
  s = action(s, { type: "REMOVE_ALL" }, f);
  s = action(s, { type: "MOVE", delta: [1, 1] }, f);
  s = action(s, { type: "RESTORE", ids: ["crate"] }, f);
  assert.equal(JSON.stringify(f), before);
});
test("failed route preserves previous valid queued route and player", () => {
  const f = fixture();
  let s = action(init(f), { type: "REMOVE_ALL" }, f);
  s = action(s, { type: "ROUTE", target: [3, 3] }, f);
  const before = JSON.stringify({ player: s.player, route: s.route });
  assert.throws(() => action(s, { type: "ROUTE", target: [4, 4] }, f));
  assert.equal(JSON.stringify({ player: s.player, route: s.route }), before);
});
test("removal order yields identical visible sets and occupancy", () => {
  const f = fixture();
  const a = action(
    action(init(f), { type: "REMOVE", ids: ["crate"] }, f),
    { type: "REMOVE", ids: ["rock"] },
    f,
  );
  const b = action(
    action(init(f), { type: "REMOVE", ids: ["rock"] }, f),
    { type: "REMOVE", ids: ["crate"] },
    f,
  );
  assert.deepEqual([...a.hidden].sort(), [...b.hidden].sort());
  assert.deepEqual(core.occupancy(f, a), core.occupancy(f, b));
});
test("reset clears removals, overlays, route and restores original spawn", () => {
  const f = fixture();
  let s = action(init(f), { type: "REMOVE_ALL" }, f);
  s = action(s, { type: "MOVE", delta: [1, 1] }, f);
  s = action(s, { type: "OPTION", key: "inferred", value: true }, f);
  s = action(s, { type: "RESET" }, f);
  assert.deepEqual(s.player, [0, 0]);
  assert.equal(s.hidden.size, 0);
  assert.equal(s.inferred, false);
  assert.equal(s.mode, "walk");
});
test("a path is deterministic over repeated calls and never crosses invisible base void", () => {
  const f = fixture();
  f.navigation.walkableCells = cells.filter((c) => c[0] !== 2 || c[1] === 3);
  const s = action(init(f), { type: "REMOVE_ALL" }, f),
    route = core.findPath(f, s, [0, 0], [4, 0]);
  assert.ok(route);
  assert.deepEqual(route, core.findPath(f, s, [0, 0], [4, 0]));
  assert.ok(route.some((p) => same(p, [2, 3])));
  function same(a, b) {
    return a[0] === b[0] && a[1] === b[1];
  }
  for (const p of route)
    assert.ok(f.navigation.walkableCells.some((c) => same(c, p)));
});

test("equal-depth sprite order is bytewise and does not vary with browser locale", () => {
  const f = fixture();
  f.components = [component("Z", [1, 0]), component("a", [1, 0])];
  const alpha = new Map(
    f.components.map((c) => [c.id, new Uint8Array(1024).fill(255)]),
  );
  assert.deepEqual(
    core.hitTest(f, init(f), [33, 1], alpha).map((c) => c.id),
    ["a", "Z"],
  );
});

if (fs.existsSync(built)) {
  const actualScene = () =>
    core.validateScene(JSON.parse(fs.readFileSync(built)));
  test("actual larder jar removal opens its blocked cell, occupied restoration rejects, and leaving permits restoration", () => {
    const f = actualScene(),
      jar = f.components.find((c) => c.id === "larder-jar-large");
    assert.ok(jar && jar.mutable && jar.blocksMovement);
    const target = jar.footprintCells[0];
    let s = core.createState(f);
    assert.equal(core.isWalkable(f, s, target), false);
    assert.equal(core.findPath(f, s, s.player, target), null);
    s = core.reduceState(f, s, { type: "REMOVE", ids: [jar.id] });
    assert.equal(core.isWalkable(f, s, target), true);
    s = core.reduceState(f, s, { type: "ROUTE", target });
    assert.ok(s.route.length);
    while (s.route.length) s = core.reduceState(f, s, { type: "TICK" });
    assert.deepEqual(s.player, target);
    assert.throws(
      () => core.reduceState(f, s, { type: "RESTORE", ids: [jar.id] }),
      /occupied/i,
    );
    assert.equal(s.hidden.has(jar.id), true);
    const escape = core
      .neighbors(f, s, s.player)
      .find(
        (c) => !jar.footprintCells.some((p) => p[0] === c[0] && p[1] === c[1]),
      );
    assert.ok(escape);
    s = core.reduceState(f, s, {
      type: "MOVE",
      delta: [escape[0] - s.player[0], escape[1] - s.player[1]],
    });
    s = core.reduceState(f, s, { type: "RESTORE", ids: [jar.id] });
    assert.equal(core.isWalkable(f, s, target), false);
  });
  test("actual actor pivot locates the feet near the bottom of its exported pose", () => {
    const f = actualScene();
    assert.ok(
      f.player.pivot[1] > 0.9,
      "Browser player pivot must use top-left normalized Y, not Unity bottom-left Y.",
    );
  });
  test("actual all-prop removal preserves every fixed layer and all destination routes", () => {
    const f = actualScene(),
      s = core.reduceState(f, core.createState(f), { type: "REMOVE_ALL" });
    assert.equal(s.hidden.size, f.components.filter((c) => c.mutable).length);
    for (const c of f.components) assert.equal(s.hidden.has(c.id), c.mutable);
    for (const destination of f.navigation.destinations)
      assert.notEqual(
        core.findPath(f, s, s.player, destination.cell),
        null,
        destination.id,
      );
    const restored = core.reduceState(f, s, { type: "RESTORE_ALL" });
    assert.equal(restored.hidden.size, 0);
  });
}

test("navigation checkbox focus does not swallow movement keys, while search typing does", () => {
  assert.equal(
    core.isTypingTarget({
      tagName: "INPUT",
      type: "checkbox",
      isContentEditable: false,
    }),
    false,
  );
  assert.equal(
    core.isTypingTarget({
      tagName: "INPUT",
      type: "search",
      isContentEditable: false,
    }),
    true,
  );
  assert.equal(
    core.isTypingTarget({
      tagName: "TEXTAREA",
      type: "textarea",
      isContentEditable: false,
    }),
    true,
  );
  assert.equal(
    core.isTypingTarget({ tagName: "SPAN", isContentEditable: true }),
    true,
  );
  assert.equal(
    core.isTypingTarget({ tagName: "CANVAS", isContentEditable: false }),
    false,
  );
});

test("step interpolation preserves endpoints and uses intermediate foot positions", () => {
  const tween = { from: [3, 5], to: [4, 6], start: 100, duration: 120 };
  assert.deepEqual(core.interpolateStep(tween, [4, 6], 100), [3, 5]);
  assert.deepEqual(core.interpolateStep(tween, [4, 6], 160), [3.5, 5.5]);
  assert.deepEqual(core.interpolateStep(tween, [4, 6], 220), [4, 6]);
  assert.deepEqual(core.interpolateStep(tween, [4, 6], 999), [4, 6]);
});
test("reset/teleport or reduced motion cannot retain a stale interpolation", () => {
  const tween = { from: [3, 5], to: [4, 6], start: 100, duration: 120 };
  assert.deepEqual(core.interpolateStep(tween, [20, 20], 150), [20, 20]);
  assert.deepEqual(core.interpolateStep(null, [4, 6], 150), [4, 6]);
  assert.deepEqual(core.interpolateStep(tween, [4, 6], 150, true), [4, 6]);
  assert.deepEqual(core.interpolateStep(tween, [4, 6], 50), [3, 5]);
});

test("actual exploded renderer draws its inventory without requiring a movement clock", () => {
  const vm = require("node:vm");
  const source = fs.readFileSync(
    path.resolve(__dirname, "../ArtSource/CatacombVillage/scene.js"),
    "utf8",
  );
  const begin = source.indexOf("  function drawExploded() {");
  const end = source.indexOf("  function locate()", begin);
  assert.ok(begin >= 0 && end > begin);
  const f = fixture(),
    state = core.createState(f);
  state.exploded = true;
  state.hidden.add("crate");
  state.selectedId = "rock";
  const runtime = {
    data: f,
    state,
    tween: null,
    sprites: new Map(
      f.components.map((c) => [
        c.id,
        { id: c.id, naturalWidth: c.bounds[2], naturalHeight: c.bounds[3] },
      ]),
    ),
    exploded: [],
  };
  const canvas = {},
    draws = [];
  const ctx = {
    clearRect() {},
    fillRect() {},
    fillText() {},
    drawImage(image, ...args) {
      draws.push({ id: image.id, alpha: this.globalAlpha, args });
    },
  };
  vm.runInNewContext(source.slice(begin, end) + "\ndrawExploded();", {
    runtime,
    canvas,
    ctx,
    interpolateStep: core.interpolateStep,
    reducedMotion: { matches: false },
    sorted: (items) =>
      [...items].sort(
        (a, b) => a.depth - b.depth || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0),
      ),
  });
  assert.equal(canvas.width, 1536);
  assert.equal(canvas.height, 200);
  assert.equal(draws.length, f.components.length);
  assert.equal(draws.find((d) => d.id === "crate").alpha, 0.4);
  assert.equal(draws.find((d) => d.id === "rock").alpha, 1);
  assert.equal(runtime.exploded.length, f.components.length);
});

test("actual normal renderer handles movement, inspection and comparisons with finite draw coordinates", () => {
  const vm = require("node:vm");
  const source = fs.readFileSync(
    path.resolve(__dirname, "../ArtSource/CatacombVillage/scene.js"),
    "utf8",
  );
  const begin = source.indexOf("  function drawPlayer("),
    end = source.indexOf("  function locate()", begin);
  const f = fixture();
  f.player = { frameWidth: 26, frameHeight: 59, pivot: [0.5, 1], scale: 1 };
  for (const view of [
    "walking",
    "masks-backing-grid",
    "baseline",
    "underlay",
    "isolated",
  ]) {
    const state = core.createState(f);
    state.player = [2, 2];
    if (view === "masks-backing-grid") {
      state.masks = true;
      state.inferred = true;
      state.grid = true;
      state.route = [[3, 2]];
    }
    if (["baseline", "underlay"].includes(view)) {
      state.view = view;
      state.mode = "inspect";
    }
    if (view === "isolated") {
      state.mode = "inspect";
      state.soloId = "crate";
      state.masks = true;
      state.inferred = true;
    }
    const runtime = {
      data: f,
      state,
      tween: { from: [1, 2], to: [2, 2], start: 100, duration: 110 },
      images: new Map(
        ["base", "baseline", "underlay", "player"].map((id) => [id, { id }]),
      ),
      sprites: new Map(f.components.map((c) => [c.id, { id: c.id }])),
      masks: new Map(f.components.map((c) => [c.id, { id: c.id + "-mask" }])),
      inference: { id: "inference" },
    };
    const canvas = { width: 160, height: 160 },
      draws = [];
    const ctx = {
      save() {},
      restore() {},
      stroke() {},
      strokeRect() {},
      beginPath() {},
      moveTo() {},
      lineTo() {},
      setLineDash() {},
      ellipse() {},
      fillRect() {},
      clearRect() {},
      drawImage(image, ...args) {
        assert.ok(
          args.every(Number.isFinite),
          `${view}: image coordinates must be finite`,
        );
        draws.push({ id: image.id, args });
      },
    };
    vm.runInNewContext(source.slice(begin, end) + "\ndraw(155);", {
      runtime,
      canvas,
      ctx,
      interpolateStep: core.interpolateStep,
      occupancy: core.occupancy,
      key: (c) => c.join(","),
      reducedMotion: { matches: false },
      performance: { now: () => 155 },
      sorted: (items) =>
        [...items].sort(
          (a, b) =>
            a.depth - b.depth || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0),
        ),
      drawLayer: (target, item) =>
        target.drawImage(
          runtime.sprites.get(item.id),
          item.bounds[0],
          item.bounds[1],
        ),
    });
    assert.ok(draws.length > 0, view);
    const actor = draws.find((d) => d.id === "player");
    assert.equal(
      Boolean(actor),
      ["walking", "masks-backing-grid"].includes(view),
    );
    if (actor)
      assert.equal(
        actor.args[4],
        51,
        "Interpolated actor X is anchored halfway through its step.",
      );
  }
});
