"use strict";
const test = require("node:test"),
  assert = require("node:assert/strict");
const M = require("../ArtSource/Morrowfast/scene.js");
function fixture() {
  const c = (id, kind, x, extra = {}) => ({
    id,
    name: id,
    kind,
    description: id,
    actions: ["Inspect", "Hide"],
    roomId: null,
    foot: [x * 8 + 4, 20],
    bounds: [x * 8, 16, 8, 8],
    mutable: true,
    blocksMovement: false,
    footprintCells: [],
    layerIds: [id + "-layer"],
    ...extra,
  });
  const components = [
    c("roof", "roof", 3, { roomId: "house" }),
    c("door", "door", 3, {
      roomId: "house",
      blocksMovement: true,
      footprintCells: [[3, 2]],
    }),
    c("chest", "container", 4, {
      roomId: "house",
      visibleWhen: "room-open",
      blocksMovement: true,
      footprintCells: [[4, 2]],
    }),
    c("fern", "plant", 1, { blocksMovement: true, footprintCells: [[1, 2]] }),
    c("shell", "building-shell", 5, { mutable: false }),
  ];
  return {
    schemaVersion: 1,
    id: "morrowfast",
    title: "Morrowfast",
    canvas: { width: 64, height: 40 },
    base: { path: "build/base.png" },
    baseline: { path: "reference.png" },
    layers: components.map((c, i) => ({
      id: c.id + "-layer",
      ownerId: c.id,
      path: "build/" + c.id + ".png",
      bounds: c.bounds,
      z: i,
      role:
        c.kind === "container"
          ? "furniture"
          : c.kind === "building-shell"
            ? "shell"
            : c.kind === "plant"
              ? "sprite"
              : c.kind,
      roomId: c.roomId,
    })),
    components,
    rooms: [
      {
        id: "house",
        name: "House",
        polygon: [
          [16, 8],
          [48, 8],
          [48, 32],
          [16, 32],
        ],
        interiorPolygon: [
          [24, 16],
          [40, 16],
          [40, 24],
          [24, 24],
        ],
        entry: [28, 20],
        roofId: "roof",
        doorId: "door",
      },
    ],
    navigation: {
      cellSize: 8,
      width: 8,
      height: 5,
      spawn: [0, 2],
      northExit: [0, 0],
      walkableCells: Array.from({ length: 40 }, (_, i) => [
        i % 8,
        Math.floor(i / 8),
      ]),
    },
  };
}
test("valid source contract accepts complete independent ownership", () =>
  assert.equal(M.validateScene(fixture()).id, "morrowfast"));
test("roof lift reveals only its room and preserves furniture collision", () => {
  const d = fixture(),
    s = M.createState(d);
  assert.equal(M.ownerVisible(d, s, "chest"), false);
  assert.equal(M.isWalkable(d, s, [4, 2]), false);
  assert.equal(M.setHidden(d, s, "roof", true).ok, true);
  assert.equal(M.ownerVisible(d, s, "chest"), true);
  assert.equal(M.isWalkable(d, s, [4, 2]), false);
  assert.equal(M.ownerVisible(d, s, "fern"), true);
});
test("closed door blocks; opening permits traversal; closing refuses occupancy", () => {
  const d = fixture(),
    s = M.createState(d);
  assert.equal(M.isWalkable(d, s, [3, 2]), false);
  M.setHidden(d, s, "door", true);
  assert.equal(M.isWalkable(d, s, [3, 2]), true);
  s.player = [3, 2];
  assert.equal(M.setHidden(d, s, "door", false).ok, false);
  assert.equal(s.hidden.has("door"), true);
  s.player = [2, 2];
  assert.equal(M.setHidden(d, s, "door", false).ok, true);
});
test("a fixed shell cannot be hidden and refusal does not alter state", () => {
  const d = fixture(),
    s = M.createState(d);
  assert.equal(M.setHidden(d, s, "shell", true).ok, false);
  assert.equal(s.hidden.size, 0);
});
test("harvest removes only matching layer owner and matching collision", () => {
  const d = fixture(),
    s = M.createState(d);
  M.setHidden(d, s, "fern", true);
  assert.equal(
    M.layerVisible(
      d,
      s,
      d.layers.find((l) => l.ownerId === "fern"),
    ),
    false,
  );
  assert.equal(M.isWalkable(d, s, [1, 2]), true);
  assert.equal(M.isWalkable(d, s, [3, 2]), false);
});
test("restoration of a blocker on surveyor is rejected atomically", () => {
  const d = fixture(),
    s = M.createState(d);
  M.setHidden(d, s, "fern", true);
  s.player = [1, 2];
  const before = M.serializeState(d, s);
  assert.equal(M.setHidden(d, s, "fern", false).ok, false);
  assert.deepEqual(M.serializeState(d, s), before);
});
test("indoor hidden object never becomes visible simply by restoring the owner", () => {
  const d = fixture(),
    s = M.createState(d);
  s.hidden.add("chest");
  M.setHidden(d, s, "chest", false);
  assert.equal(M.ownerVisible(d, s, "chest"), false);
  M.setHidden(d, s, "roof", true);
  assert.equal(M.ownerVisible(d, s, "chest"), true);
});
test("Walking route respects blockers and cardinal adjacency", () => {
  const d = fixture(),
    s = M.createState(d),
    path = M.findPath(d, s, [0, 2], [6, 2]);
  assert.ok(path.length > 6);
  let prev = [0, 2];
  for (const p of path) {
    assert.equal(Math.abs(prev[0] - p[0]) + Math.abs(prev[1] - p[1]), 1);
    assert.equal(M.isWalkable(d, s, p), true);
    prev = p;
  }
  assert.equal(M.findPath(d, s, [0, 2], [3, 2]), null);
});
test("comparison views refuse movement and inspect does not walk", () => {
  const d = fixture(),
    s = M.createState(d);
  s.view = "original";
  assert.equal(M.moveOne(d, s, [0, 1]).ok, false);
  s.view = "reconstruction";
  s.mode = "inspect";
  assert.equal(M.moveOne(d, s, [0, 1]).ok, false);
  s.mode = "walk";
  assert.equal(M.moveOne(d, s, [0, 1]).ok, true);
  assert.equal(s.steps, 1);
});
test("state export and import preserve removed door, lifted roof and player", () => {
  const d = fixture(),
    s = M.createState(d);
  M.setHidden(d, s, "door", true);
  M.setHidden(d, s, "roof", true);
  s.player = [3, 2];
  s.steps = 8;
  const restored = M.loadState(d, JSON.stringify(M.serializeState(d, s)));
  assert.deepEqual([...restored.hidden].sort(), ["door", "roof"]);
  assert.deepEqual(restored.player, [3, 2]);
  assert.equal(restored.steps, 8);
});
test("invalid imported state cannot add IDs, hide fixed shell or stand inside wall", () => {
  const d = fixture(),
    s = M.createState(d),
    v = M.serializeState(d, s);
  for (const bad of [
    { ...v, hidden: ["unknown"] },
    { ...v, hidden: ["shell"] },
    { ...v, player: [3, 2] },
    { ...v, player: [8, 2] },
    { ...v, id: "another" },
    { ...v, steps: -1 },
  ])
    assert.throws(() => M.loadState(d, JSON.stringify(bad)));
});
test("manifest rejects unsafe URLs, orphan layers, duplicate owners, missing roof and bad footprint", () => {
  for (const mutate of [
    (d) => (d.layers[0].path = "../secret.png"),
    (d) => (d.layers[0].ownerId = "lost"),
    (d) => d.components.push({ ...d.components[0] }),
    (d) => (d.rooms[0].roofId = "lost"),
    (d) => (d.components[0].footprintCells = [[99, 99]]),
    (d) => (d.layers[0].z = NaN),
  ]) {
    const d = fixture();
    mutate(d);
    assert.throws(() => M.validateScene(d));
  }
});
test("hide outdoors leaves roofs and indoor furniture intact", () => {
  const d = fixture(),
    s = M.createState(d);
  M.hideOutdoor(d, s);
  assert.equal(s.hidden.has("fern"), true);
  assert.equal(s.hidden.has("roof"), false);
  assert.equal(s.hidden.has("chest"), false);
  assert.equal(s.hidden.has("door"), false);
});
test("show all interiors lifts roofs without changing door collision", () => {
  const d = fixture(),
    s = M.createState(d);
  M.openAllRooms(d, s);
  assert.equal(M.ownerVisible(d, s, "chest"), true);
  assert.equal(M.isWalkable(d, s, [3, 2]), false);
});

const fs = require("node:fs"),
  path = require("node:path");
const sceneRoot = path.resolve(__dirname, "../ArtSource/Morrowfast");
const builtPath = path.join(sceneRoot, "build/manifest.json");
const built = fs.existsSync(builtPath)
  ? JSON.parse(fs.readFileSync(builtPath, "utf8"))
  : null;
const builtOptions = {
  skip: !built && "Run the Morrowfast art producer first.",
};
function flood(d, s) {
  const k = (p) => p.join(","),
    allowed = new Set(d.navigation.walkableCells.map(k)),
    blocked = M.occupancy(d, s),
    seen = new Set([k(s.player)]),
    queue = [s.player];
  for (let at = 0; at < queue.length; at++)
    for (const [dx, dy] of [
      [0, -1],
      [1, 0],
      [0, 1],
      [-1, 0],
    ]) {
      const p = [queue[at][0] + dx, queue[at][1] + dy],
        id = k(p);
      if (allowed.has(id) && !blocked.has(id) && !seen.has(id)) {
        seen.add(id);
        queue.push(p);
      }
    }
  return seen;
}
function insidePolygon(p, polygon) {
  let inside = false;
  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const a = polygon[i],
      b = polygon[j];
    if (
      a[1] > p[1] !== b[1] > p[1] &&
      p[0] < ((b[0] - a[0]) * (p[1] - a[1])) / (b[1] - a[1]) + a[0]
    )
      inside = !inside;
  }
  return inside;
}
function ownerApproaches(d, o) {
  const points = o.footprintCells.length
      ? o.footprintCells
      : [o.foot.map((v) => Math.floor(v / d.navigation.cellSize))],
    out = new Set();
  for (const p of points) {
    if (!o.blocksMovement) out.add(p.join(","));
    for (const [dx, dy] of [
      [0, -1],
      [1, 0],
      [0, 1],
      [-1, 0],
    ])
      out.add([p[0] + dx, p[1] + dy].join(","));
  }
  return out;
}
test(
  "actual build validates all owner links and every PNG file dimension",
  builtOptions,
  () => {
    M.validateScene(built);
    for (const [relative, expected] of [
      [built.base.path, [built.canvas.width, built.canvas.height]],
      [built.baseline.path, [built.canvas.width, built.canvas.height]],
      ...built.layers.map((l) => [l.path, l.bounds.slice(2)]),
    ]) {
      const png = fs.readFileSync(path.join(sceneRoot, relative));
      assert.equal(png.toString("hex", 0, 8), "89504e470d0a1a0a", relative);
      assert.deepEqual(
        [png.readUInt32BE(16), png.readUInt32BE(20)],
        expected,
        relative,
      );
    }
  },
);
test(
  "actual closed houses block indoor walking while keeping the north road open",
  builtOptions,
  () => {
    const s = M.createState(built),
      reach = flood(built, s),
      cs = built.navigation.cellSize;
    assert.ok(reach.has(built.navigation.northExit.join(",")));
    for (const r of built.rooms) {
      assert.equal(
        [...reach].some((k) =>
          insidePolygon(
            k.split(",").map((v) => (Number(v) + 0.5) * cs),
            r.interiorPolygon,
          ),
        ),
        false,
        r.id + " leaks through closed walls or door",
      );
    }
  },
);
test(
  "actual opened houses and intact outdoor props allow an approach to every semantic owner",
  builtOptions,
  () => {
    const s = M.createState(built);
    M.openAllRooms(built, s);
    for (const r of built.rooms)
      assert.equal(M.setHidden(built, s, r.doorId, true).ok, true, r.id);
    const reach = flood(built, s),
      bad = [];
    for (const o of built.components)
      if (![...ownerApproaches(built, o)].some((k) => reach.has(k)))
        bad.push(o.id);
    for (const r of built.rooms)
      assert.ok(
        reach.has(
          r.entry
            .map((v) => Math.floor(v / built.navigation.cellSize))
            .join(","),
        ),
        r.id + " doorway",
      );
    assert.deepEqual(
      bad,
      [],
      "Every component needs a reachable adjacent ground cell, not just a remote inspector click.",
    );
  },
);
test("an open arch is liftable scenery, not an invented hinged door", () => {
  const d = fixture(),
    s = M.createState(d);
  d.components.find((c) => c.id === "fern").kind = "gate";
  assert.equal(M.primaryActionLabel(d, s, "fern"), "Lift component");
  assert.equal(M.primaryActionLabel(d, s, "door"), "Open door");
  assert.equal(M.primaryActionLabel(d, s, "roof"), "Lift roof");
});

function bridgeFixture() {
  const d = fixture();
  d.components = d.components.filter((o) => o.id === "fern");
  d.components[0] = {
    ...d.components[0],
    id: "bridge",
    name: "Creek bridge",
    kind: "bridge",
    foot: [28, 20],
    bounds: [24, 16, 16, 8],
    blocksMovement: false,
    footprintCells: [],
    layerIds: ["bridge-layer"],
  };
  d.layers = [
    {
      id: "bridge-layer",
      ownerId: "bridge",
      path: "build/bridge.png",
      bounds: [24, 16, 16, 8],
      z: 1,
      role: "sprite",
    },
  ];
  d.rooms = [];
  d.navigation.walkableCells = Array.from({ length: 8 }, (_, x) => [x, 2]);
  d.navigation.northExit = [7, 2];
  d.navigation.crossings = [
    {
      ownerId: "bridge",
      cells: [
        [3, 2],
        [4, 2],
      ],
    },
  ];
  return d;
}
test("lifting the bridge removes river support instead of opening water as ground", () => {
  const d = bridgeFixture();
  M.validateScene(d);
  const s = M.createState(d);
  assert.ok(M.findPath(d, s, [0, 2], [7, 2]));
  assert.equal(M.setHidden(d, s, "bridge", true).ok, true);
  assert.equal(M.isWalkable(d, s, [3, 2]), false);
  assert.equal(M.findPath(d, s, [0, 2], [7, 2]), null);
  assert.equal(M.setHidden(d, s, "bridge", false).ok, true);
  assert.ok(M.findPath(d, s, [0, 2], [7, 2]));
});
test("bridge removal refuses a surveyor standing on supported water", () => {
  const d = bridgeFixture(),
    s = M.createState(d);
  s.player = [3, 2];
  assert.equal(M.setHidden(d, s, "bridge", true).ok, false);
  assert.equal(s.hidden.has("bridge"), false);
  assert.equal(M.isWalkable(d, s, s.player), true);
});
test("bridge removal refuses a queued route across it without mutating that route", () => {
  const d = bridgeFixture(),
    s = M.createState(d);
  s.route = M.findPath(d, s, s.player, [7, 2]);
  const before = s.route.map((p) => [...p]);
  assert.equal(M.setHidden(d, s, "bridge", true).ok, false);
  assert.deepEqual(s.route, before);
  assert.equal(s.hidden.has("bridge"), false);
  s.route = [];
  assert.equal(M.setHidden(d, s, "bridge", true).ok, true);
});
test("hidden bridge save cannot restore a player onto unsupported water", () => {
  const d = bridgeFixture(),
    s = M.createState(d);
  s.hidden.add("bridge");
  assert.throws(() =>
    M.loadState(d, { ...M.serializeState(d, s), player: [3, 2] }),
  );
});
test("crossing manifest rejects unknown owners, unsupported cells and duplicate supports", () => {
  for (const alter of [
    (d) => (d.navigation.crossings[0].ownerId = "unknown"),
    (d) => (d.navigation.crossings[0].cells = [[3, 1]]),
    (d) => d.navigation.crossings[0].cells.push([3, 2]),
    (d) => (d.navigation.crossings[0].cells = [[99, 2]]),
  ]) {
    const d = bridgeFixture();
    alter(d);
    assert.throws(() => M.validateScene(d));
  }
});
test("bulk outdoor lift retains a bridge reserved by the walking route", () => {
  const d = bridgeFixture(),
    s = M.createState(d);
  s.route = M.findPath(d, s, s.player, [7, 2]);
  const outcome = M.hideOutdoor(d, s);
  assert.deepEqual(outcome.refused, ["bridge"]);
  assert.equal(s.hidden.has("bridge"), false);
  assert.equal(s.route.length, 7);
});
test(
  "actual exported bridge owns support cells that disappear from walkable water when lifted",
  builtOptions,
  () => {
    assert.ok(
      built.navigation.crossings?.length,
      "The creek bridge needs explicit exported support cells.",
    );
    const s = M.createState(built);
    for (const crossing of built.navigation.crossings) {
      const before = flood(built, s),
        reachable = crossing.cells.find((p) => before.has(p.join(",")));
      assert.ok(reachable, crossing.ownerId + " is not reachable while intact");
      assert.equal(M.setHidden(built, s, crossing.ownerId, true).ok, true);
      for (const p of crossing.cells)
        assert.equal(
          M.isWalkable(built, s, p),
          false,
          crossing.ownerId + " left unsupported water walkable",
        );
      assert.equal(M.findPath(built, s, s.player, reachable), null);
      assert.equal(M.setHidden(built, s, crossing.ownerId, false).ok, true);
      assert.ok(M.findPath(built, s, s.player, reachable));
    }
  },
);
