# Live 16×24 — north works and creatures

Status: **bounded packet integrated, tested and reviewed in ordinary Play**, 2026-10-23. Continues vegetation `d936500db`. Nine native frames cover the actual field-alembic worksite and three creature identities, including the patchbearer's healing-slot state. Nella, crops, reserve cords, products and incidental hazards remain separate unfinished work. Normal release activation stays off.

## Production prompt

Use the accepted real north journey, `north-greybox-{entry,works,still-examine,plot}` and later native-vegetation captures, reviewed `north-works-concept.png` and `north-parts-reference.png`, their exact prompts and the parts critique. Reconstruct nine original native cells in an independent 144×24 atlas: still, empty-looking shelf, stoneburr, frost lichen, mendleaf, magpie, viper, and patchbearer with/without its actual healing bottle. Use editable integer-cell source geometry and direct native/contact rendering. Do not import, downscale or trace a generated raster. Preserve approved sources, real owners/layout and current game mechanics. Complete native RED→GREEN before ordinary gameplay/capture/review and exact-path commit.

## Pre-implementation verification sweep

| Source / question | Correction and implementation consequence |
| --- | --- |
| `Objects.json`: AlchemyStill | Non-solid `PhysicalObject` with owned `AlchemyStillPart`. It is a receiving vessel and condenser, not an always-burning furnace. Preserve brew actions and don't draw output inventory. |
| Same: AlchemyShelf | Solid wood container with six-item capacity; no explicit Furniture/Wall tag. The observed two shelves are empty. Neutral compartments can remain unchanged if contents change; don't promise stock or add sight opacity. |
| Stoneburr/FrostLichen/Mendleaf and `HarvestablePart` | Each is non-solid vegetation yielding its exact seed/lichen/sprig. Successful finite harvest removes the owner. Reject spent/wrong-yield/foreign/invalid parts rather than inventing a standing spent state. |
| Magpie/Viper blueprints and inherited creature graph | Real passive hoarder and hostile snake, respectively. Distinct bird/low S-body silhouettes; don't bake stolen equipment, aggression, poison cloud or action readiness into art. Mutable health/faction/control are gameplay, not asset identity. |
| `MarlbackPatchbearer`, `FieldMedicinePart.FindCarriedMedicine` | Its carried HealingTonic and separate Antidote are independent. Bottle-present/absent describes only the healing slot and is independent of health threshold or Calm. Use the pure existing local-inventory query, not a second counter. |
| `FieldMedicinePart.TryUseMedicine`, `InventoryPart` | Native self-treatment already dirties the actor cell. General inventory add/remove does not necessarily do so; verify stationary transfer/replenishment before claiming state acceptance. Use a bounded visible-owner tracker populated by the existing cell pass, revalidate its resolved frame before the renderer dirty decision and dirty only changed old/current cells. Clear on graph/mode changes; no gameplay inventory edits or per-frame world scan. RED tests must assert submitted tile updates without manually marking dirty. |
| `FlatPixelActorMotionCatalog` | Exact motion allowlist is Player/PetDog/Scrabbler/Gleaner. These three new identities remain static Tilemap bodies and must not be suppressed by an unrelated motion asset. New poses are a separate explicit gap. |
| Current catalogs | Add an eighth optional borrowed atlas without loading resources from older injected constructors. Current graph, foreground owner, fog, subject authorization and disposal remain upstream contracts. |

This is original CoO art informed by the user's requested flat pixel style. No decompiled Qud implementation or asset is copied. Reuse the existing greybox and concept stages because placement is unchanged; a new census or concept-only milestone would not advance this packet.

## Bounded plan and readiness

1. 🟢 Existing geometry/captures and reviewed concepts are ready. Freeze the nine frame indices above; preserve vegetation and masonry.
2. 🟢 Runtime RED preceded production; exact identities/owned semantics, foreign reskins, resources, cache lifetime, finite harvest, world/portrait owners and medicine transitions are tested. Stationary transfer exposed a real stale submitted tile.
3. 🟢 Nine cells, direct-render contact, exact source pixels, binary alpha, quiet palette and prior-source hashes verified. Independent review prompted a lower, broader Mendleaf before import.
4. 🟢 Selectors and bounded state invalidation implemented; real missing-resource RED followed by import and 755/755 selected native GREEN cases.
5. 🟢 Ordinary seed64 north walk, combat/Calm, inventory healing, worksite and matching examine captures, finite Stoneburr harvest and two-tonic batch brewing completed. No forced state for pictures. Actor self-treatment remains a native-test result, not a witnessed ordinary-Play action.
6. 🟢 Significant findings fixed, independent review complete and live handoffs updated. Commit/push is the final delivery operation; the next packet starts only after this checkpoint is on main.

## Performance and honesty bounds

Reuse the shared static Tilemap/material and cached sprites. No new per-cell zone/adjacency search or actor GameObjects. Patchbearer query may inspect only its own inventory; state invalidation must target the carrier, not all world entities. Record real editor cost with a sufficiently large recorder and disclose idle/tool time. Previous vegetation timings had unequal routes and a wrapped baseline and cannot establish a speedup. Physical Deck/player performance, all-seed composition, new directional animation and full route/release acceptance remain separate gates.

Current pass: native integration and bounded ordinary-Play acceptance complete. Exact test, capture, performance, review and next-action records follow. This is neither full north-route coverage nor the overall seven-milestone release gate.


## Production continuation

Source verifier was written and failed before authoring. The first nine-cell source pass passed exact pixels but independent review found Mendleaf too narrow/tall to read as soft leaves. The second pass lowers it to12 pixels and broadens side leaves around inset pale veins; the other eight frames remain unchanged. Root and independent visual review accepted the direct8× contact. The frozen144×24 PNG hash is `fb318bdbf5e49c4a0efd36639415e92fc27a548559fc5574de5deee58bdaeab4`; decoded RGBA hash `d58e3964a4d269bf017f75358ac7c49768c0acb786a8ff579f0b625ddaad9366`. Only eight pixels at the patchbearer's cradle differ between its two states; body/hand anatomy stays identical. All frames retain binary alpha,3–6 colors and exact source/contact pixels; prior approved images remain unchanged. [Editable source and verifier](../ArtSource/Live16x24/north-works/README.md).

Native Unity **64 expected RED failures** at10:57:33–36 precede production: missing eighth optional catalog/world API and actual resource. Receipt [north-works-red.xml](Verification/Live16x24/north-works-red.xml). The next intermediate gate supplies selectors without the quiet-state watcher so actual already-submitted tile staleness can be observed independently of the missing API. Art import remains gated until these source/routing checks.


Selectors-only native result at10:59:50–53: **56 passed,8 failed of64**. One failure is the intentionally missing resource. Seven assert actual stale submitted tiles: inventory transfer/refill, four foreign ownership variants, cover/uncover and hidden-cell retirement. Native AI self-treatment already passes through its existing dirty hook. This is a genuine state-cadence RED independent of the missing atlas API: [north-works-state-red.xml](Verification/Live16x24/north-works-state-red.xml).

Watcher correction: retain a previously observed covered patchbearer with a suppressed signature, checking only its cell/top owner; do not query its medicine while covered. This lets uncovering recover after raw owner-list changes that do not emit normal dirty notifications. Evict hidden/off-graph owners, and clear on mode/zone/fullscreen lifecycle. Reconciliation targets the bound renderer's old/current cells through a cached delegate rather than a potentially foreign global render callback.


Native override/watcher gate at11:07:50–53: **60 passed,9 failed of69**. All seven stale-tile cases now pass, as do five added lifecycle/allocation counterchecks (replacement cell, stale relocation, managed graph replacement, warmed no-allocation/no-discovery, foreign renderer). Eight deliberate explicit-reskin failures precede rejecting nonempty `VisualVariant` and `GlyphVariants`; the ninth is the missing resource. These extra five are post-initial-RED characterization checks, not independently RED behavior claims. [Receipt](Verification/Live16x24/north-works-reskin-red.xml).

After the two-field guard, native pre-import gate at11:09:18–21 is **68 passed,1 expected missing-resource failure**. [Receipt](Verification/Live16x24/north-works-resource-red.xml). Root then imported the frozen144×24 resource through `Tools/Live16x24/import_north_works.cs`: Point, Clamp, no mipmaps, Uncompressed, sRGB and no platform override. `north-works-import.json` records actual Unity import. Independent final four-file/test review reports no remaining P1/P2; default world and portrait paths both load the same optional atlas.


Final selected native Unity regression: **755/755 passed, zero skipped**, XML test interval11:12:53–11:15:40, including all69 new works cases and26 fixture groups: all `FlatPixel*` fixtures, environment rendering, fullscreen canvas ownership and four FieldMedicine fixture groups. [Authoritative XML](Verification/Live16x24/north-works-regression-green.xml). The initial collector expired at180seconds while Unity was still running; a read-only XML collector recovered the completed result. Intermediate bridge pings disconnected during synchronous world-generation tests; no test was restarted or counted twice. This is a selected native regression, not a full-project run.

Import clarification: the existing `SpriteImportPostprocessor` enforces Sprite/16PPU/2048max settings and Unity may retain generated sub-sprite rectangles. The runtime intentionally borrows the Texture2D and creates the full16×24 cached frames itself; actual tests assert those exact rects. Native size, Point/Clamp/no-mip/uncompressed/no-platform-override remain intact. No source pixel or asset ownership change is hidden by the importer.


## Ordinary gameplay and visible acceptance

The existing isolated launcher used real SampleScene/GameBootstrap, seed 64, and a fixed 1280×800 Game view. N began a fresh game. All movement, selection, combat response, inventory use, harvest and brewing used ordinary keyboard input through CUA. The accompanying diagnostics only observed state; they did not teleport, reveal fog, change health or insert supplies. Before/after geometry remains the accepted greybox route.

| Capture / action | Observed result |
| --- | --- |
| [Native north entry](Verification/Live16x24/north-works-native-entry.png) | 13 Up from glade `(40,12)` crossed to connector `(40,24)`, 40 HP. Existing native vegetation and masonry remained intact. |
| [Native worksite](Verification/Live16x24/north-works-native-worksite.png) | 21 Right and 6 Up reached `(61,18)`. The real patchbearer inflicted 36 damage during approach; skill 6, Enter, Down cast Calm. The capture records 4 HP and the peaceful actor. This is not a safe-route claim. Distinct still, empty shelves, forage and patchbearer replace rough markers. |
| Inventory and healing | I/Tab/select/Enter/B drank one actual starter HealingTonic, healing 19 to 23 HP and reducing the stack from two to one. Returning from inventory and the pause menu restored the world. No health was injected. |
| [Stoneburr examine](Verification/Live16x24/north-works-stoneburr-examine.png) and [harvest](Verification/Live16x24/north-works-stoneburr-harvest.png) | Look/action/examine selected the real source at `(60,18)`. Its native portrait matched the hooked plant. The ordinary Harvest action yielded `StoneburrSeed ×2` and removed the source. Returning from Examine goes to Look; a mistaken H there moved the cursor and was corrected through the actual action menu. |
| [Still examine](Verification/Live16x24/north-works-still-examine.png) | The real still at `(62,17)` showed its native condenser/receiver portrait and existing field-brewing description. The vessel is apparatus, not painted available stock. |
| [Batch brewing](Verification/Live16x24/north-works-batch-brew.png) | The still menu's C set the two StoneburrSeed aside; A brewed the full batch. The log names two stoneskin tonics, and the observed inventory contains `BrewedTonic ×2`, with no seeds remaining. This packet added no recipe or new tonic blueprint. |
| [Patchbearer examine](Verification/Live16x24/north-works-patchbearer-examine.png) | Ordinary Look/actions opened the matching native actor portrait with carried-healing explanation. The actor remained at 20 HP with a HealingTonic. Its bottle removal/refill is proven by native integration tests, **not** observed self-treatment in this playthrough. |

Every PNG was opened and inspected. Headers independently confirm 1280×800, with hashes in [capture audit](Verification/Live16x24/north-works-capture-audit.json). Sibling JSON records current owners and inventory. Those diagnostic owner lists include hidden entities and must not be mistaken for rendered disclosure. `editorReportedScreenSize` is retained as dock metadata only. The actual worksite and portrait were shared inline with the user.

[Cleanup](Verification/Live16x24/north-works-cleanup.json) confirms stopped Play, no pending launcher/view/compile state, inactive save isolation, and restored `Assets/Scenes/Main/SampleScene.unity`. No scene was saved over. Console UI showed zero errors and two warnings; the MCP reader classifies the two Metal memoryless-depth notices as Error and a plain `[Combat] Autosaved.` log as Exception. Its [raw receipt](Verification/Live16x24/north-works-console.json) is retained rather than claiming an empty console or a gameplay exception from that classification. No packet-specific C# exception was observed.

## Measured renderer cost

[Profiler receipt](Verification/Live16x24/north-works-profile.json): 69.436 seconds, 47,866 samples, capacity 131,072, valid and unwrapped. This is the ordinary walk/combat/Calm plus idle/tool time and inventory opening, not an isolated model benchmark.

| Counter | Mean | p99 | Maximum |
| --- | ---: | ---: | ---: |
| ZoneRenderer.LateUpdate | 0.077108 ms | 0.172708 ms | 30.997417 ms |
| Main Thread | 1.443270 ms | 5.696167 ms | 1197.081958 ms |
| Draw calls | 32.16 | 38 | 46 |
| Batches | 31.74 | 37 | 46 |
| Triangles | 6989.79 | 9224 | 9228 |

Recorders were disposed. Previous vegetation measurements have unequal routes and a wrapped baseline; no controlled speedup or no-regression claim follows from these numbers. Native tests separately assert zero allocation after warmup in 100 quiet reconciliation ticks and no new discovery scan. The tracker only revisits previously painted patchbearers, checks visibility/graph before medicine, and uses cached local dirty callbacks. Physical Deck behavior remains untested.

## In-phase and cold-eye review

- 🟡 **Fixed, observed RED:** selectors alone left already-painted healing-slot art stale on quiet inventory transfer/refill and ownership/cover changes. The renderer now reconciles only previously observed exact owners; invalid/hidden entries retire, covered entries retain a suppressed signature without reading medicine, and only changed cells become dirty.
- 🟡 **Fixed, observed RED:** explicit VisualVariant/GlyphVariants could be overwritten. Works routing now refuses those reskins, with eight counter-cases.
- 🟡 **Fixed, art review:** first Mendleaf looked like a tall forked stalk. A stricter source check failed before lowering and broadening it; other eight frames were preserved.
- 🔵 **Reviewed:** both default catalog loaders bind the eighth resource; older injections omit it deliberately. World and authorized exact-subject portraits share frames. Borrowed textures survive disposal; owned Sprite/Tile caches are destroyed. Current graph, cell ownership, visibility, foreign art, fullscreen/mode replacement and stale references have counter-checks. Final independent code and art reviews found no remaining P1/P2.
- 🔵 **Scope clarification:** the five added lifecycle/allocation cases initially passed and are characterization tests, not five independently discovered bugs. Intermediate RED stages are recorded separately; 755 is one selected final run, not a sum of overlapping receipts.
- ⚪ **Deliberate divergence:** no new layout, combat, loot, permissions, recipe, harvest semantics or medicine behavior. New creatures remain static Tilemap identities; new directional/reaction poses are not claimed. Existing native glade motion remains intact.
- 🧪 **Deferred coverage:** ordinary Play did not harvest FrostLichen/Mendleaf or witness patchbearer self-treatment. Native tests cover all three finite sources and self-treatment/transfer/refill. Loose products retain their current fallback until the product packet. Nella/crop/cord growth and revisit, east/kitchen, full route release, physical controller/Deck and all-seed composition are unfinished.

**Can verify:** actual imported native frame bindings, source pixels and preservation, tested ownership/state transitions, observed 1280×800 appearance, ordinary travel/combat/Calm/menu return/harvest/brewing, and bounded editor measurements. **Cannot verify:** full regional coverage, new creature motion, every field-use outcome, hardware Steam Deck graphics/feel/performance, or packaged activation. Flat mode remains opt-in.

Files: four modified rendering files (`FlatPixelOpeningArtCatalog`, `FlatPixelGreyboxCatalog`, `EnvironmentSpriteRenderer`, `ZoneRenderer`); new `FlatPixelNorthWorksTests.cs` and meta; native resource PNG/meta; editable `ArtSource/Live16x24/north-works` source/manifest/contact/verifier; `Tools/Live16x24/build_north_works.py` and importer; this record, source README, route/parts/connected-site/live-art handoffs; exact `Verification/Live16x24/north-works-*` receipts. Existing atlas importer whitespace was verified equivalent to HEAD and restored; approved asset bytes are preserved.

## Verified next-packet handoff (not implemented here)

Independent source audit while the native regression ran confirms the next bounded work:

- Bind exact `SpreadSeedKeeper` from a byte-identical Resources copy of approved `Assets/Art/SeedKeeper3/seed-keeper-3.png`. The existing study references that PNG; `SeedKeeperFlatLibrary` is older mesh/palette art. Keep original importer, full24-pixel height and row23 unchanged; create a centered16PPU runtime tile. No claim-part requirement for actor identity.
- Public `CandyCarrotCrop`/`EmberwheatCrop` have stages0–1 then automatic produce release/removal. Reserve `MarlrootCrop`/`PitchpodCrop` have stages0–2 then hand harvest yielding produce/seed. Reserve crops have Crop but no Vegetation tag: do not reuse `OrdinaryVegetation`. Reuse actual CropSystem/CropPart/CropYieldService dirty hooks and test submitted tiles/off-zone return.
- Wet ground currently follows foreground owner; a covering actor/item can hide the crop's soil cue. Resolve validated visible crop soil independently, without scanning the world.
- Persistent cords require validated `ReserveBedKey`, claim soil references and `Bound(zone)`. Reuse Zone's LocalGatheringClaimPart index/membership version; do not invoke whole-zone `ResolveReserveBed`. Mutable claim fields need revalidation beyond membership epochs. `Bound` currently allocates through LINQ: measure and narrowly correct only if needed by the renderer.
- Cords need a distinct transparent ground-decoration channel below actors/crops, preserving hazards, fog, full/incremental clearing, fullscreen and restoration. Tray art remains neutral regardless of contents. Public/copied/replaced soil, moved/removed tray/keeper, foreign claims, hidden cells, permission changes, harvest/replant/save-load, real growth and menu return are required counters.
- Minimum next source group: six growth frames, neutral tray and cord, deliberately reusing accepted planted marks; approved keeper remains a separate unchanged texture. Direct outputs and the additional crops bought from Nella remain further coverage. Recheck source before implementation.
