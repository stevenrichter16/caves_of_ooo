# The Spread: worked country composition

Status: complete and installed, 2026-09-12. CoO-original; no Qud parity claim.

The Spread is the recovered river country between the stranger regions. Design
authority: Lore/README.md, Lore/10_Bible.md and the ordinary magic described in
Lore/History/09_Magic.md (temporary flowers cannot replace a lasting harvest).
Shipped geography and formation identities follow WorldMapAuthoring and
Docs/FELLING-WORLD-DESIGN.md's Spread illustration. No new dialogue or lore facts.

## Spatial grammar

Every chunk has an irregular field system, a shared headland and connected dry
approaches. A few trees stand at field corners and along shelter belts; open
ground separates productive clusters. Three husbandry conditions (tended,
harvested, returning scrub) vary density without a noisy palette.

- Hedgerow: three separate field rooms with broad gates and broken corners.
- Field strips: bounded barley plots with unplanted headlands and working lanes.
- Old road: a broad continuous route, sheltering roadside copses, quiet verges.
- Flower meadow: three asymmetric banks of ephemeral flowers around a gathering
  lawn, with existing FlowerCharmPart determining their lifespan.
- Fallow: interrupted field boundaries and brush advancing from one corner;
  the missing crop matters as much as what has returned.
- River meadow: a shallow, curved backwater inset from chunk edges, reeds only
  near its banks, and continuous dry crossing approaches.

## Verification sweep and implementation boundary

The old Spread combines JungleBuilder scatter with formation stamps. Ordinary
Spread chunks will instead use a pure plan and native realization builder.
POIs, river POIs, sinkhole mouths and other shared biome pipelines retain their
current builders. FormationSelector remains authoritative; only interior layout
depends on world seed. Renderer coverage is a finite authored Spread address set,
excluding named sites and sinkholes, like the Grovelands boundary.

Verified native content: Hedge has destructible/combustible Plant behavior;
CropRow and RoadStone are existing terrain (not new harvest mechanics);
FlowerField owns its FlowerCharm and petal-source behavior. Use a dedicated sixteen-model Spread voxel kit (four variants each of hedge,
barley, pink flower-charm and reed), retaining native entity identity. The existing
plain floor family supplies road ground. Do not claim CropRow is a harvestable planted crop.

## Gates

RED first: determinism, seed variation, shared boundary portals, all six signature
features, dry connected approaches, bounded crops and flora, preservation of
nonempty input and POI exclusion. Then native-generation and renderer checks,
adversarial tests, multi-seed actual voxel previews, review/fix, full regression
comparison against G12's 32 recorded failures. Record visual and measurement
limits explicitly. No save migration, camera adjustment or spawn relocation.

## Performance

Generation-only masks and bounded chunk loops; native entities remain the source
of truth. No runtime procedural replay, no per-frame geometry authoring. Coverage
lookup uses a cached finite set. Existing renderer batching and dirty patches
handle destruction. Composition emits a worldgen diagnostic with formation,
condition and counts, plus rejection reasons for invalid/nonempty inputs.


## Implementation and review log

- SP01 recorded missing-type RED. SP02 passed 29 generation/legacy checks.
- SP04 passed 574/575: the old renderer scope pin treated Spread 9.9 as unrelated.
  Its negative control now uses authored Beating 18.18; new Spread positive and
  native-owner/removal controls exercise the deliberate coverage expansion.
- SP05 rendered 18 native-voxel terrain examples. Art review rejected the initial
  borrowed dry-brush mapping for barley/reeds and the equal-width road branches.
  Added an offline repeatable voxel kit authoring tool and a single main road.
- Independent review found coating-only backwater was invisible to later landmark
  liquid guards. SP06 reproduced missing reservations, and a broader pocket sweep
  reproduced a disconnected Hedgerow pocket at seed 35. The pure plan now repairs
  disconnected open pockets with bounded headland routes before native realization.
  Water and approaches populate existing GenReservedCells, protecting them from
  reservation-aware landmarks, hazards and initial population placement. Actors can
  subsequently walk onto routes; this is not a permanent occupancy restriction.
- SP07: eighteen refined previews, zero missing voxel mappings. The dedicated kit
  uses two constant palette swatches per object, one combined mesh per prefab,
  three chunky stems per barley/reed cell and broad flower petals. No per-cube
  GameObjects, Blender import dependency, runtime mesh baking or new blueprint.
- The user resumed playing during development. Remaining verification runs in
  /tmp/coo-spread-validation, an APFS clone of the project; the original running
  game remains untouched by test launches. SP08 had 579/580 passing; its sole
  failure was an absent MCP WebSocket server error, not a gameplay assertion.
  Started the absent server, let it settle, and reran the targeted gate.

## Native mechanics and limits

Hedge damage/fire, FlowerCharm duration/petal effects, ground ownership and water
coatings retain their native implementations. CropRow remains existing decorative
barley terrain; no harvest/inventory/farming-system behavior is newly claimed.
The river-meadow backwater is local, inset water with dry crossings, not a promised
continuous world river. Authored river POIs keep their dedicated river pipeline.
Optional later stages may place content outside reserved cells; containers and
haulables keep their existing siting rules. Traversal is checked after the full
pipeline in addition to terrain-stage approach/pocket invariants.

New render resources are additive. Existing ring assets are borrowed unchanged.
SpreadVoxelLibrary supplies bounded metadata/prefab lookups for its sixteen static
models; VoxelWorldPresentation recognizes those exact prebuilt mesh references.
Unknown native objects retain the presenter's usual fallback. Terrain preview
images omit population and lightmap, so native-pipeline previews are a separate
check. Existing/generated saved chunks are not rebuilt.

- SP09: 585/585 targeted tests passed. Independent re-review cleared both findings.
- SP10 full isolated run: zero compiler errors, 11,912 cases. Its 27 failures
  beyond G12 comprised 19 MCP connection log failures and eight world-map
  transition tests whose minimal fixture omitted Spread terrain blueprints.
  Added the five required fixture identities (Hedge, CropRow, RoadStone,
  FlowerField, Reeds), preserving every transition assertion. Production still
  rejects missing required content instead of silently erasing a biome feature.
  The runner now keeps its MCP subprocess alive for the complete Unity run;
  separate background shell launches were being cleaned up before tests finished.

- SP12 confirmed the same eight incomplete-fixture failures once MCP stayed up.
  The isolated transition subset additionally exposed two older missing fixture
  identities (GoldCoin and WoodenBarrel). Added those fixture-only definitions.
  SP14: all 65 WorldMapTests pass; no assertions or production rejection rules
  were relaxed.
- SP15: eighteen native-pipeline previews (all six formations at three seeds),
  zero unmapped voxel meshes. These include real optional structures/hazards;
  inspection confirms the river/backwater remains clear of structural stamps and
  flower lawns retain their open gathering space. They are whole-chunk overview
  cameras at the gameplay pitch, not a recording of the user's current session.

## Reproduction and installed files

With a separate closed-editor project copy, run Unity batch executeMethod
`CavesOfOoo.Editor.SpreadVoxelKitBuilder.Run` to rebuild the sixteen generated
prefabs/meshes in Resources/SpreadVoxel3D. Rebuilding updates the same assets and
preserves GUIDs. The native ring material is shared; UVs sample exactly one
palette swatch per block. Asset validation covers coarse mesh budget, one-cell
horizontal extents, variant geometry, and one mesh renderer per prefab.

Set SPREAD_PREVIEW_OUT to an output directory and execute
`CavesOfOoo.Editor.SpreadCompositionPreviewBatch.Run` for terrain previews. Add
SPREAD_PREVIEW_NATIVE=1 for ordinary full-pipeline examples; BuildAndRun also
rebuilds the kit. Receipt keys inherited from the Grovelands preview harness map
columns→hedges, compost→barley, cache→flower-charms; these are object counts, not
new loot categories. The supplied overview images show all six formations.

New sources: SpreadCompositionPlan, SpreadCompositionBuilder, SpreadVoxelLibrary,
SpreadVoxelKitBuilder, SpreadCompositionPreviewBatch, and three test fixtures.
Existing integration edits: OverworldZoneManager; SpawnRing3D catalog, library,
recipes and presenter; VoxelWorldPresentation; one renderer-scope negative control.
WorldMapTests receives the missing minimal blueprint fixtures directly.
The pre-existing mixed native files remain installed; SP11/implementation.patch
records only this feature's delta instead of staging unrelated foundation work.
SP11/source-equivalence.json verifies the isolated copy matches installed feature
code/assets. GUID audit: 42 new metadata files, zero collisions.

Can verify: seeded composition, native object identity/removal, reserved water,
terrain pocket connectivity, sampled final-pipeline west/east traversal, complete
new voxel kit and rendered previews. Cannot infer extended balance, player
preference, all-address/all-direction traversal, or hitch-free performance from
these checks. Unsupported native actors/objects retain existing fallback art;
the dedicated kit adds four plant families, not all Spread creatures or buildings.
The user's spawn, 1.2x camera multiplier and full-reveal setting are unchanged.


## Final gate

SP16 full suite: **11,912 total / 11,880 passed / 32 failed**, zero C# compiler
errors. Exact failure names match G12's recorded baseline: **zero introduced
failures**. There are 49 new Spread composition/art test cases, with seeded loops
and native integration checks. SP09 targeted 585/585; SP14 transition/world-map
suite 65/65. Independent review and fix re-review complete, no outstanding
material findings. Source-equivalence audit compares 82 installed/isolated files,
zero differences. New GUID collision audit is clear.

No current player session was reset for final verification. Assets and source are
installed in the original project; new layouts apply on generation after Unity
loads the updated code. Cached chunks retain their native state. Full native
preview overview: Verification/VoxelWorld/SP15-native-preview/overview.png.
