# Live 16×24 — native north vegetation

Status: native vegetation integrated and ordinary Play checked, 2026-10-23. Continues the real north-route greybox and reviewed imagegen extraction in [north route](LIVE-16X24-NORTH-ROUTE.md). Four new frames replace ordinary Tree/Hedge/BerryBush/DryBrush markers; ordinary Bush/Reeds reuse the accepted base frames. North actors, works, crops, reserve cords and products remain separate unfinished groups. Normal release activation is still off.

## Production prompt and source sweep

Reconstruct the approved north concept silhouettes as four original native 16×24 frames: Tree, Hedge, BerryBush and DryBrush. Reuse the accepted low Bush and tall/short Reeds pixels for their ordinary owners. Make obstacles and passable growth legible in the real route without moving owners, altering collision or making resources appear inexhaustible. Reuse the actual north screenshots and reviewed imagegen concepts; do not rerun a census or import the generated extraction. Author editable integer-cell Blender geometry, verify the exact native render, import one cached atlas, then repeat ordinary north travel and inspect actual gameplay.

| Source checked before implementation | Correction / consequence |
| --- | --- |
| `Objects.json`: six vegetation blueprints | Tree and Hedge block movement, but neither blocks player FOV. Tree's Solid tag affects AI sight; Hedge lacks that tag. Presentation must not add tags or change opacity. |
| `FlatPixelOpeningArtCatalog.FrameOf` | Bush and Reeds currently require glade-specific VisualIDs. Add exact ordinary empty-VisualID routes while preserving explicit glade and foreign-reskin behavior. Do not assign fake glade roles. |
| `HarvestablePart` | BerryBush is a finite WildBerries source; successful harvest removes the entire owner. No spent/regrowing bush frame. Positive valid yield and owned unspent harvest are required. |
| `FlatPixelGreyboxCatalog` | Current graph admission, owner selection, visibility and remembered terrain remain upstream. Tree/Hedge are not Terrain/Wall and are currently absent from memory; don't change that policy here. |
| Existing atlas injection chain | A seventh optional borrowed atlas must remain independent of six earlier groups. Old injection overloads omit it without implicitly consulting Resources. Dispose generated tiles/sprites, never borrowed textures. |
| North concepts and parts critique | Generated foliage is overly detailed and not a native atlas. Rebuild four compact silhouettes using flat palette clusters. The dense hedge module should join in rows and columns while actual gaps stay empty. |

## Bounded implementation and verification

1. Write RED tests for new exact identities, reused ordinary Bush/Reeds, semantic guards, finite berry removal, optional atlas/cache lifetime, exact-subject portraits, world fog and current-owner context. Preserve old explicit glade mappings and test foreign VisualID rejection.
2. Author four frames in order Tree/Hedge/BerryBush/DryBrush, a 64×24 atlas, editable source-cell data and Blender source. Inspect direct-rendered native/enlarged contacts including repeated horizontal/vertical/corner hedges. No new animation claim.
3. After native RED, implement cached routing and import Point/Clamp/no mip/uncompressed. Actual resource loading must fail before import and pass afterward. Run affected prior art/portrait/scope consumers and cold review.
4. Repeat the real seed-64 north route with ordinary inputs. Capture the same entry/worksite views, inspect vegetation and collision/selection, and compare against saved greybox. Record clean isolation restoration and honest rendering observations.

## Performance and ownership

Use the current shared Tilemap/material path and four cached sprites/tiles, with no new GameObjects, Update loop, zone scan, adjacency scan, allocation or RNG in per-cell selection. Reuse base frames rather than duplicate Bush/Reeds pixels. Measure before/after editor rendering over ordinary gameplay where available; report valid sample bounds and do not equate editor timings with Steam Deck performance. Cold cache construction is distinct from steady-state rendering. Existing gameplay diagnostics remain authoritative; this read-only visual selector does not add noisy per-cell log events.

## Current pass and honesty bounds

Greybox, actual captures, scene reinterpretations and isolated-parts extraction were completed in `67629188e`. Their prompts and independent critiques remain under `ArtSource/Live16x24/concepts/north-*`. This continuation completed native production, import, semantic selection, regression checks and ordinary north travel. The source-native verifier, not a resampled concept, establishes the production pixels.

Initial baseline Play attempt was interrupted by a script import/domain reload while tests were being authored: the editor remained in Play with a null current zone and a stale drawn frame. Ordinary inputs therefore did not exercise gameplay, and that attempt is excluded from baseline evidence. Finish restored the isolated session. Serialize later Play runs after scripts settle; never substitute the stale frame for a game capture.

## Production and implementation

[Editable source packet](../ArtSource/Live16x24/north-vegetation/README.md) contains named integer-cell Blender geometry, source-cell JSON, native64×24 atlas, direct8× contact and read-only verifier. Tree has an exposed trunk; the hedge joins on both axes; the low berry bush has restrained fruit accents; dry brush is sparse warm stems. Four distinct cells use binary alpha and3–6 colors. Zero source/native/contact pixel mismatches; approved character and prior atlas bytes remain unchanged. PNG SHA256: `f6d1ede626402f9b7963b4aba7434044f10a925376e4da74c73e4dfb94e68777`.

`FlatPixelOpeningArtCatalog` adds a seventh independently optional borrowed resource, caches its four sprites/tiles and releases only those generated objects. Exact blueprint, owned Physics, Vegetation tag, empty VisualID and non-creature guards precede selection. Berry additionally requires an owned unspent valid WildBerries harvest. Ordinary Bush/Reeds use base5 and3/4; authored glade mappings remain intact. `FlatPixelGreyboxCatalog` forwards the resource while retaining graph admission, current owner and fog authority. Earlier constructor injections deliberately omit the resource, preserving deterministic fallback. No simulation, population, collision, FOV, yield or permission changed.

[Import receipt](Verification/Live16x24/north-vegetation-import.json):64×24, Point, Clamp, no mipmaps, uncompressed, sRGB, not readable. Runtime cached sprites use16PPU. These are static frames; no new animation or damaged/burning-state art is claimed.

## Native Unity test evidence

| Gate | Actual result |
| --- | --- |
| [RED before production](Verification/Live16x24/north-vegetation-red.xml) |49 expected failures: missing seventh-group API/resource plus actual ordinary Bush/Reeds routing failures. |
| [Routing before resource import](Verification/Live16x24/north-vegetation-resource-red.xml) |48 passed, one expected missing actual resource failure. |
| [Native GREEN](Verification/Live16x24/north-vegetation-green.xml) |204/204: vegetation, opening catalog, cellar art, exact examine portraits and north scope. |
| [Consumer regression](Verification/Live16x24/north-vegetation-consumers.xml) |108/108: greybox catalog, ground states, actor motion and fullscreen ownership. |

The two GREEN XML files contain **312 distinct cases, zero failures or skips**:49 new and263 prior. They are real Unity EditMode results, not the external runner. Counter-checks cover foreign blueprint/VisualID/part ownership, missing tags, creatures, altered solidity/takeability, invalid/spent berry sources, missing/malformed independent atlases, tile/sprite identity and borrowed-resource lifetime. Positive visible owner selection pairs with hidden, removed, replaced and memory cases. Actual finite harvest removes the source, grants1–3 berries and rejects a repeat; FOV tests distinguish Tree/Hedge from a real wall.

## Actual ordinary Play and captures

Fresh isolated seed64, real SampleScene/GameBootstrap,1280×800. All travel, selection, examination, harvesting and Calm used ordinary CUA keyboard input. Diagnostics and Unity ScreenCapture were observational; no teleports, spawns, fog reveal, added supplies or health changes. Each PNG has a sibling receipt with independently verified PNG dimensions/hash; editor dock dimensions remain separately recorded. Owner receipts include hidden owners and are not a rendered-visibility claim.

| Flow | Observation |
| --- | --- |
| Comparable entry: [before](Verification/Live16x24/north-vegetation-before-entry.png), [native](Verification/Live16x24/north-vegetation-native-entry.png) | Both runs used N,13 Up and reached north connector `(40,24)`,40HP. Native tree clusters, continuous hedges and low passable growth replace rough markers at the same entry. This is the valid positional before/after comparison. |
| Worksite approach |21 Right,6 Up met live combat in both runs. Before ended `(60,18)`,20HP after viper interception; native ended `(56,18)`,20HP after patchbearer interception. These are not matched worksite positions or a controlled performance benchmark. Skill6,Enter,Down cast Calm on the actual patchbearer. |
| [Hedge collision](Verification/Live16x24/north-vegetation-hedge-collision.png) | After advancing to `(56,10)`, Left struck hedgerow without moving. Up through the real gap and the subsequent west/north/east route reached `(60,2)`; no visual gap was painted across a solid owner. |
| [Berry examine](Verification/Live16x24/north-vegetation-berry-examine.png) | L,Up,Enter,X selected actual BerryBush `(60,1)`. Popup showed the same native low fruit silhouette and real harvest description/material/6-of-6 structure. |
| [Berry harvest](Verification/Live16x24/north-vegetation-berry-harvest.png) | Escape,Enter,H harvested one WildBerries. Before/after receipts prove the bush owner disappeared and inventory gained exactly one berry. The world sprite disappeared immediately. |
| [Nella plot](Verification/Live16x24/north-vegetation-native-plot.png) | Escape,3 Up crossed into `11.8.0` at `(60,24)`;40 Left,14 Up reached `(20,10)`,20HP beside Nella. Native trees and reused reeds render in the connected destination. Nella, other actors, works, crop/claim art and products remain explicit unfinished coverage; this image is not full plot acceptance. |

[Final cleanup](Verification/Live16x24/north-vegetation-native-cleanup.json) confirms Play, pending, compile, isolation and view-pending flags all false with SampleScene restored. The earlier invalid baseline has a separate finish receipt and is excluded.

## Rendering observations and limits

| Recorder | Before: final16,384 samples only | Native:73,154 samples, no wrap |
| --- | --- | --- |
| ZoneRenderer mean / p99 / max ms |0.064466 /0.133709 /0.460125 |0.077379 /0.133459 /28.004833 |
| Main Thread mean / p99 / max ms |1.302666 /2.184167 /803.587125 |1.321655 /4.908375 /813.366500 |
| Draw calls mean / p99 / max |35.384 /36 /36 |35.042 /37 /45 |
| Batches mean / p99 / max |34.384 /35 /35 |34.772 /36 /45 |

Raw [before](Verification/Live16x24/north-vegetation-before-profile.json) and [native](Verification/Live16x24/north-vegetation-after-profile.json) receipts include bounds. Before elapsed85.637s/63,514 frames, but its16,384-capacity recorder wrapped: only the retained tail is summarized. Native elapsed97.011s with131,072 capacity. The windows include editor/tool idle time and different combat/positions. They establish recorded costs only, **not a speedup, no-regression proof or controlled comparison**. The28ms renderer and roughly0.8s main-thread maxima are unattributed; do not assign them to art without an isolated reproduction. Physical Steam Deck performance remains unmeasured. New art uses one shared atlas and four cached frames, with no new per-cell scan or GameObjects.

## In-phase self-review and continuation

- 🔵 Source correction resolved: Tree/Hedge movement solidity is not player-FOV opacity. The catalog reads existing semantics and adds no Wall/Solid tags; native wall counter-checks pass.
- 🔵 Scope divergence: this is the first bounded native north packet, not the full route. Ordinary Bush/Reeds reuse accepted art instead of adding redundant cells. The earlier greybox/imagegen/extraction is reused because geometry remains unchanged.
- 🔵 Independent source/art/runtime review found no actionable P1/P2. Native resource and ordinary gameplay gates completed after that review. Exact source-native checks and all312 native cases pass.
- 🧪 Performance comparison is limited by baseline buffer wrap, unequal live combat and editor instrumentation. Preserve raw receipts; controlled player/Deck profiling remains an acceptance gate.
- ⚪ Existing focus sidebar briefly retains the just-harvested bush's text until look mode is exited or selection changes, while the world sprite and owner are already gone. This is a UI refresh follow-up; it did not grant a duplicate harvest or block movement. No fix is claimed here.
- ⚪ No seed65 live journey, burning/damage visuals, physical controller test or new release build in this packet. DryBrush has exact import/owner tests and native source review but was not separately targeted in this seed64 journey.

**Can verify (script-observable):** the stated native tests, actual imports, owner removal/inventory result, positions/HP, admitted renderer, recorded profile samples and isolation restoration. **Observed visually:** actual native entry/plot, hedge boundary, berry portrait and disappearance at1280×800. **Cannot verify from these checks:** broader aesthetic completion, animation feel, every generated seed, controlled performance, physical Deck behavior or normal release activation.

Next: use the saved north concepts/parts inventory for native actors and the field alembic/forage/shelves; then bind approved Nella and implement crop/product states plus persistent validated reserve cords. Keep the east/kitchen packet, full connected journey, water/growth/save/load, reactions and release/Deck gates visible in the live brief. Do not repeat the completed vegetation packet or present fallback actors as finished art.

Files: two runtime catalogs; new49-case `FlatPixelNorthVegetationTests.cs` and GUID meta; new native resource/meta; editable source, builder/importer and verifier; this record and cross-linked continuation records; seven actual Game screenshots, read-only gameplay/profile receipts and native test XML/JSON. This is original CoO art, not copied Qud source or sprites.
