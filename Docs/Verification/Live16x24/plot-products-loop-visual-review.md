# Independent plot-product loop visual/evidence review — 2026-10-24

Read-only review of all 12 `Docs/Verification/Live16x24/plot-products-loop-*.png` files and their paired observational JSON, plus launch/cleanup. No Unity, input, gameplay, project writes or tests were performed by this reviewer. The native source contact/atlas was used only as a comparison reference. This report precedes the requested post-fix captures.

## Finding

The loop now provides real bounded acceptance for **ground resin and clod Examine, harvested seed pickup/planting, rain-fed growth, clod preparation, and observed save/load persistence**. It does **not** yet accept all eight product portraits/uses or close the two actual UI defects:

1. **P2, already being fixed:** after harvest, the sidebar FOCUS retains the removed crop's name/contents while the exact ground item popup correctly examines resin or clod. Both `resin-ground` and `clod-ground` demonstrate it.
2. **P2, already being fixed:** ordinary carried seed Examine leaves the player in inventory/log prose without the structured reader/native portrait. `seed-carried` proves actual custody/selection, not a successful portrait.

No additional material native sprite/palette defect was found in the observed resin/clod portraits or the clear world frames. Broader scene completion and unobserved products remain pending.

## Capture-by-capture classification

All names below have prefix `plot-products-loop-` and matching `.png` / `.json` under `Docs/Verification/Live16x24`.

| Suffix | What the actual screenshot and paired state establish | Limit / misleading interpretation to avoid |
|---|---|---|
| `arrival` | Clear ordinary flat plot view, player `(20,10)`, tick960,40HP,50drams, Nella rightsUnknown. Native keeper, player, field/crop markers and surrounding vegetation are visible. | Not a rights purchase, product acquisition or product portrait. The JSON includes observations beyond the camera composition. |
| `learn-rain` | Actual announcement over inventory: “You learn Conjure Rain.” State is tick970,42drams and Granted rights. Starting seed stocks remain4gladroot/2Emberwheat. | The JSON does not enumerate abilities; the learning message is the visual witness. Payment is evidenced by the state transition, not a payment-dialog screenshot. |
| `resin-ground` | Exact EXAMINE title “PitchPod resin,” amber native portrait and actual use/prose. At18,6 the crop is gone; two resin owners and one seed remain on the ground; no resin/seed yet carried. Tick1030. | FOCUS incorrectly still says PitchPod. The popup is valid lower-subject Examine, not a pile summary. Do not call this a clean final UI acceptance until the stale focus is recaptured. |
| `seed-carried` | Inventory visibly selects pitchpod seed, shows resin×2, and lists Plant actions including prepared bed18,6. JSON has seed×1/resin×2 and no loose outputs at18,6. Tick1040. | **No Examine reader or portrait is present.** The filename accurately denotes custody only. This is the recorded generic inventory Examine gap, not successful seed-art acceptance. |
| `planted` | Log says “You plant pitchpod seed.” Paired JSON has no carried PitchpodSeed and a new PitchpodCrop at18,6,stage0,moisture0,role null; player17,8,tick1060. | Pause menu is open. This is valid transaction/state evidence but a poor clean planted-scene showcase; it does not show a seed portrait. Do not mistake the later new crop for an old harvested crop respawning. |
| `rain-first` | Clear world after cast; hotbar Conjure Rain CD4 and rain log. All six relevant crops have39moisture,stage0. Tick1070. | Captured after the action, not a multi-frame rain animation review. Moisture39 is consistent with a40-unit top-up and paid-turn processing. |
| `sprout` | Clear scene with visibly larger plants. Tick1330: both public gladroot crops, Marlroot and replanted Pitchpod are stage1/moisture13; Emberwheat remainsstage0/moisture13. | Does not prove every species advanced together, nor ripe Emberwheat. Avoid blanket “all crops sprouted.” |
| `ripe` | Actual world-action menu on Marlroot at17,6 with Harvest offered. Tick1580: Marlroot and replanted Pitchpod arestage2/moisture15; two gladroot cells now contain four loose CandyCarrot owners in total; Emberwheatstage1. Log contains two rain messages and public gladroot auto-yield. | **Harvest menu, not a mature-crop Examine popup.** This frame demonstrates availability before hand harvest, not completed harvest. Recasting rain is supported by log/state, but there is no separately titled second-cast capture. |
| `clod-ground` | Exact EXAMINE “marlroot clod,” native rooted red-clay portrait and Prepare→fire clay prose. Tick1590: original Marlroot removed, two clods+one MarlrootSeed at17,6. Player18,7 and all outputs still loose. | FOCUS still says marlroot: same cached-focus defect. The tiny world pile mostly sits behind cursor/popup; don't claim an unobstructed world close-up of every output. |
| `prepared` | Clear world and log of clod pickup×2, seed pickup, then “You prepare marlroot clod into useful supplies.” Tick1610; inventory now clod×1,FireClay×1,MarlrootSeed×1,resin×2. No loose clod/seed remains. | Does not display the refined FireClay portrait or inventory item sprite. Exact conversion comes from the observational receipt plus action log, not from a visible clay showcase. |
| `saved` | Clear post-action scene and “Game saved.” Tick1610,player18,7,40HP,42drams; prepared inventory and replacement crop retained. | No separately saved away-position frame in this prefix, so do not claim this pair alone proves travel away and return. |
| `reloaded` | Clear scene and “Game loaded.” Same saved scalar fields/inventory and matching observed world records. | Observational owner rows lack IDs; this is matching field/multiset evidence, not byte-identical save or owner-identity proof. |

## Visual native16×24 assessment

- All12 PNGs are actual **1280×800** files. Several JSONs report editor screen2321×1139, while two report1280×800; those diagnostic screen values must not be substituted for PNG dimensions.
- Clear world frames retain flat, upright square-pixel art and recognizable player/keeper/plant silhouettes. No accidental3D fallback, stretched crop body or visibly filtered portrait was found in this sample.
- Resin is a compact amber bead; clod is a red clay body with pale root strands. They read as different loose goods, without a fabricated vessel, fire or recipe outcome. Both fit the accepted muted palette and have readable contrast against the dark reader panel.
- **Exact source pixel check:** read-only array comparison against `ArtSource/Live16x24/plot-products/plot-products.png` found every opaque pixel of frame5(resin) at3× integer scale in `resin-ground`, source-frame origin screenshot`(687,145)`,333opaque screen pixels. Frame3(clod) matches at3×,origin`(687,221)`,477opaque screen pixels. These are exact RGB matches, supporting correct source palette/integer scaling and no double-gamma/tint issue in those two actual popups. The check is foreground-only, not a renderer benchmark or blanket verification of every art group.
- Public yellow gladroot goods are visible in the clear mature/post-harvest world frames at the two actual crop cells. The quantity4 is simulation evidence; stacked occupants do not imply four separate visible sprites. Their exact ground/carried Examine remains unrecorded here.
- Field soil remains large rectangular patches amid a very sparse green scene. This is existing authored composition and scope outside the opening-only ground packet, not evidence that the whole north scene is production-finished. These captures should not be presented as complete graphics release acceptance.

## Exact eight-product coverage after this loop

| Product | Newly supported ordinary evidence | Still missing from this prefix |
|---|---|---|
| CandyCarrot / gladroot | Actual public auto-yield;4 owners across17,8 and18,8; yellow root world marks in clear frames. | Exact ground reader, pickup/carried reader, any actual food-use loop. |
| CandyCarrotSeed | Real starting carried stock×4 visible in inventory. | New native portrait/world sprite acceptance and planting/use of this species. Stock text alone is not sprite verification. |
| EmberwheatSeed | Real starting carried stock×2 visible in inventory. | Exact native portrait/world sprite acceptance and actual planting of this species. Standing Emberwheat never becomes ripe in this loop. |
| MarlrootClod | Real finite harvest2, exact ground portrait, pickup, one clod consumed→oneFireClay. | Exact carried clod portrait and direct visual refined-clay readback; source/native tests remain broader than this live sample. |
| MarlrootSeed | Actual harvest output1 and subsequent inventory ownership1, preserved on reload. | Exact ground/carried portrait, planting. |
| PitchpodResin | Actual finite harvest2, exact ground native portrait, pickup/resin×2 preserved. Earlier separate `plot-products-resin-carried-examine` remains its carried-portrait witness. | No actual brew/smear use in this prefix. Final focus-corrected ground recapture pending. |
| PitchpodSeed | Actual harvest/pickup1, inventory selection, one seed consumed to create stage0 crop, thenstage1→stage2, and saved replacement crop. | Generic carried native reader currently fails; exact ground portrait is not isolated here. Post-fix carried reader needed. |
| InertSludge | None. | Actual acquisition, world/carried presentation and compost/brewing-fallback use remain unobserved. |

The eight-frame source/native65+285 test result remains its original implementation evidence; this review adds ordinary observations and does not rerun or recount those tests.

## Save/load and cleanup verification

Read-only comparison of `saved.json` and `reloaded.json` found exact equality of seed64,tick1610,42drams,zone11.8.0,flattrue,player18,7,40HP. Inventory rows match as multisets. **All197 observed owner records match as multisets**, including granted rights, no original Marlroot, the intentionally replanted ripe Pitchpod and its12moisture, two Emberwheat sprouts and remaining gladroot outputs. Order differences do not imply state differences. No ID/complete-save claim is made.

`plot-products-loop-launch.json` records an editor-only isolation launch from SampleScene, no stored current mode key, no forced presentation and retained legacy preference presence. It is a setup receipt, not an input capture.

`plot-products-loop-cleanup.json` at2026-10-24T14:25:11Z confirms playingfalse,isolationfalse,artPendingfalse,viewBorrowedfalse,saveRootnull and originalSampleScene. Later native testing is separate from that completed Play cleanup.

## Next acceptance, bounded

1. After native fixes, recapture exact carried PitchpodSeed reader with its real native seed portrait; dismiss and retain custody/count without a paid turn.
2. Harvest while Look remains on the cell, then exact ground product Examine; capture current FOCUS plus valid popup. Repeat or countercheck clod if practical. Do not reuse these old stale-focus frames as post-fix success.
3. Preserve remaining product gaps above explicitly; use reachable public root/seed and merchant sludge actions when continuing G3. No showcase inventories or planted state should be injected to manufacture coverage.

Review outcome: two known actionable UI defects already in TDD, no additional material observed art defect. Bounded plot loop succeeds; entire G2/G3, all products/biomes, physical Deck/mouse and full release journey are not accepted by these files.
