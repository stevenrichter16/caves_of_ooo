# Live 16×24 — gleaners’ cellar

Status: implemented and accepted for this bounded cellar packet, 2026-10-23. The opening motion, items and portraits are committed in `4f4fb0167`. Nine native frames, the actual ordinary-input down/up/revisit/save/load journey and the corrected examine palette are verified below. This extends the opt-in presentation to the existing cellar only; it does not enable the normal release or the four remaining connected surface zones. The required pipeline and overall acceptance remain in [the live art direction](LIVE-16X24-ART-DIRECTION.md).

## Design prompt and decisions

Use the real generated cellar below the glade, preserving its two routes, guard, finite supply crate, haulable beam, timber pallet and two cultivated sootroot beds. First fix exact-zone presentation admission and floor classification under native RED/GREEN tests. Walk into the room with ordinary controls, capture the greybox, reinterpret the actual layout with imagegen, extract functional parts, and author a compact native atlas from editable integer-cell sources. Integrate only validated current owners and states. Compare the same ordinary journey after import. Do not create demonstration stock, move enemies, reveal fog or change collision to make the image convenient.

The cellar should feel like a small abandoned working store: cool quiet floor, solid dark masonry, modest pale stair treads, warm dry wooden supplies, and hardy sootroot. Maintain the existing empty walking lanes. Material contrast and silhouette should identify an object without icons painted over every action.

## Preimplementation source corrections

| Source finding | Implementation consequence |
| --- | --- |
| `FlatPixelGreyboxCatalog` admits only the real glade. `SpreadPresentationScope` admits substantially more than the proposed route and can admit committed lairs. | Accept only the exact current managed cellar graph with a valid Spread map and no POI, preserving glade authority. Do not require a surviving staircase or stocked owner. Other connected sites remain outside this packet. |
| `Floor` currently falls through to a generic prop and surface-green ground. | Treat valid ordinary floor as ground, including beneath higher occupants and remembered terrain. Preserve visible water and prepared-soil priority; do not reveal live crop or coating state through memory. |
| The seed chooses north/south service passage and store placement. | Census seeds64 and65; capture actual locations rather than inventing a symmetric room. Seed64 is the ordinary-input art route. |
| Crates have no persistent open-lid state. | One neutral closed-plank silhouette; emptying preserves the owner and image. |
| Dismantling consumes the timber pallet and yields two actual timbers. | No fake depleted-pallet image, no loose supplies drawn before the action. Reuse accepted timber art for resulting items. |
| Sootroot has three stages and is removed by mature harvest; its growth timing can be modified. | Three state frames follow the current owned crop. Ground conveys wetness. Do not encode a fixed timer or invent a spent plant. |
| The pulp is applied to burning, and the seed requires cultivated ground. | Give both actual harvested items compact native identities. Keep application/planting behavior in gameplay. |

## Parts and runtime contract

One 144×24 atlas, nine 16×24 cells, binary transparency, square pixels, Point/Clamp, no mipmaps or compression. Static frames share centered tile pivots and16PPU. Preserve approved art bytes and old presentation. Existing beam, sign, Scrabbler motion, clay and timber remain reusable.

| Frame | Exact owner | Visual and state contract |
| --- | --- | --- |
| 0 | `StairsUp` | Pale rising treads, unlike the dark descending pit; owned upward-stair and physical parts. |
| 1 | `Crate` | Squat plank box with corner braces, unlike the latched chest or round barrel; actual owned container. Contents do not change the frame. |
| 2 | `GleanersTimberPallet` | Broad low slats on two runners, distinct from the loose timber bundle; valid haulable solid owner and unspent timber harvest. |
| 3 | `SootrootCrop`, stage0 | Small planted seed mark. |
| 4 | `SootrootCrop`, stage1 | Paired angular olive leaves, no exposed harvest bulb. |
| 5 | `SootrootCrop`, stage2 | Dark root shoulders, olive crown and a restrained pale seam; real mature-harvest pulp/seed contract. |
| 6 | `SootrootPulp` | Compact torn root portion with dark skin and pale interior; real takeable, valid stack, topical burn-cure contract. |
| 7 | `SootrootSeed` | One tapered kernel with a pale crease, no soil or packet; real takeable, valid stack, cultivated-sootroot seed contract. |
| 8 | `StoneWall` | Quiet broad solid masonry, with no foreign VisualID, a Wall tag and owned solid/non-takeable physics. It must not read as a narrow freestanding pillar. |

Use owned visible Render and explicit identity guards. A foreign VisualID retains fallback. Semantic sprite selection is independent of placement; existing world occupancy and examine authority remain responsible for their contexts. Use the dedicated solid-masonry frame only through an explicit ordinary StoneWall contract; no fake glade VisualID is assigned.

## Acceptance and iteration

1. Confirm actual scope/floor RED, implement, run native counterchecks and previous opening controls.
2. Census realized owners and contained goods for all six planned addresses in seeds64/65; only glade+cellar is admitted now. Record refusals honestly.
3. Ordinary fresh game → glade stairs → cellar landing → service route/beam → store; capture actual greyboxes before concept work.
4. Persist imagegen prompt, actual inputs/output and critique. Produce editable native geometry, source-cell manifest, native atlas and directly rendered contact sheet; inspect silhouettes at actual size.
5. Confirm native runtime/absent-resource RED; implement/import; verify exact resource bytes, optional group behavior, owner/state/cell/fog/removal counters, existing art and portrait controls.
6. Repeat ordinary cellar travel. Inspect upward stairs, crate, pallet and crops; exercise finite loot, harvest and dismantle where reachable. Return upstairs and revisit without regenerating supplies. Record any acceptance not completed.

No new per-frame scan, per-cell sprite allocation or independent simulation state. Captures establish visible appearance and ordinary keyboard flows; native tests establish stated invariants. Neither proves physical Steam Deck performance or controller feel.

## Continuation record

- Current pass: nine-frame integration accepted, native714/714 GREEN, portrait GPU9/9 and overlapping affected regression348/348 GREEN; detached census completed for both seeds. Ordinary seed64 return/revisit/save/load and actual palette comparison passed.
- Actual cellar greybox/capture: ordinary seed64 route accepted through the stair landing, northern service route and store, preserving40HP. Actual1280×800 captures and state receipts are `Docs/Verification/Live16x24/cellar-greybox-{entry,service,store}.{png,json}`.
- Imagegen inputs/prompt/output: saved in `ArtSource/Live16x24/concepts/cellar-prompt.md`, `cellar-paintover-v1.png`, `cellar-parts-v1.png` and `cellar-parts-v2.png`. Generated concepts are references, not imported gameplay art.
- Native source: `ArtSource/Live16x24/cellar/` contains the editable Blender source, native atlas, direct-rendered contact, source cells, manifest and read-only verification. Static review accepts the nine-frame identities and continuous masonry strip. Native import is complete; the first ordinary journey and portrait follow-up are recorded below.
- Review: independent code review found no actionable owner/state/lifetime defect. Ordinary native-art Play is accepted within the final journey below; physical Deck/performance acceptance remains unfinished.

## Native atlas integration contract

The sixth optional texture on `FlatPixelOpeningArtCatalog` and its world wrapper is `Sprites/Live16x24/opening-cellar`,144×24. Earlier injected overloads continue to pass a null sixth group rather than consult Resources. Each texture is borrowed; the catalog owns nine cached sprites and tiles and releases them on disposal. Default loading and exact resource acceptance are tested separately from synthetic texture cases.

The selector uses exact blueprint identity, current owned visible Render, and no foreign VisualID. Upward stairs require owned stair/physics; crates require their actual container; pallets require owned solid physics and handling plus a valid, unspent timber yield. The crop requires the authored mature-harvest pulp/seed targets and positive quantities, then follows stages0–2. It does not pin growth timing, moisture or composting. The two loose products require takeable physics and a valid stack when present: pulp must remain an applied burning cure, seed must still plant sootroot on cultivated ground. These conditions identify existing functionality; they do not promise that the current player has sufficient strength, reach, water or inventory capacity to perform an action.

World placement, current-cell ownership and fog remain the existing world selector’s responsibility. Exact-subject examine reuses the same semantic frames after its existing document authorization. Harvest/removal must reveal the actual ground/products; there is no retained spent crop or pallet image.

### Verification checkpoints

- Source sweep: verified actual Crop, Harvestable, Seed, CureTonic, Tonic, Handling, StairsUp and Container contracts against the audited real blueprints. Haul readiness is actor-dependent, so no fixed strength or `Carryable` value is interpreted as a readiness indicator.
- Detached census: seeds64/65 generated all six intended addresses with current exploration version17 in9.37seconds. World/settlement/save-root/equipment cleanup checks all returned true. Only glade+cellar presentation is admitted. Cellar crate custody contains fire clay(stack2), one seed-dependent grimoire/buckler, and Ditchkeeper’s Footwork; guard equipment is Cudgel/Hatchet. These are not placed art owners, and their future drops do not imply additional native art coverage.
- New fixture: `FlatPixelOpeningCellarArtTests` initially had51 cases for frames, part/identity counterchecks, crop/harvest state, loose-item semantics, world fog/removal/stale membership, exact-subject art, optional groups, cache/disposal, ordinary masonry and actual resource binding. Offline Roslyn syntax/API check passed. Native RED: the first44 failed on missing sixth-atlas/resource contracts (`opening-cellar-art-red.xml`,07:47:58Z); seven added ordinary-StoneWall cases then failed on their missing mapping in the51/51 RED receipt (`opening-cellar-masonry-art-red.xml`,07:51:47–48Z).
- Implementation: added the independent sixth atlas, current semantic selectors, cached frame ownership/disposal and world-wrapper forwarding. Earlier injected constructors omit the cellar texture. Independent cold review found no actionable P1/P2 owner/state/lifetime defect in that initial implementation; it did not accept the imported pixels. Subsequent native GREEN/resource import evidence is recorded below.
- Visual correction before import: inspection of `ArtSource/Live16x24/base/opening-base-contact.png` showed baseframe1 is a narrow stepped ruin pillar. Repeating it as ordinary cellar masonry would suggest walkable gaps. The packet therefore expands from8 to9 frames (128→144pixels wide), preserving first8 indices and adding dedicated solid `StoneWall` at8. Runtime tests now contain54 cases and require that dedicated frame; earlier injected overloads must not silently reuse the surface pillar. Fresh native RED confirmed the revised contract: `opening-cellar-solid-masonry-red.xml`,08:01:03Z,54 total/1pass/53fail (valid144atlas rejected by the old8-frame loader, wall still aliased the pillar, actual resource missing). Production now loads9 cellar frames and maps ordinary StoneWall to dedicated index8 under the same owned-part guards. Subsequent preimport and actual-resource receipts are recorded below.
- Review and actual native-art Play: see the first ordinary journey and required portrait follow-up below. The existing gameplay systems already own harvest, hauling, planting and loot; this packet adds presentation, not new stock or gameplay promises.

- Native preimport receipt: `Docs/Verification/Live16x24/opening-cellar-preimport.xml`,08:02:29–34Z,85 total/84pass/1expected failure/0skips. All53 synthetic cellar cases and31 scope/floor cases passed; the sole failure was the actual imported-cellar resource requirement. Root subsequently imported the accepted144×24 atlas and completed the regressions and Play evidence below; this earlier receipt alone does not establish post-import or Play acceptance.


## Actual import and first ordinary journey

The nine-frame native atlas was imported Point/Clamp, no mipmaps, no compression at144×24. Final source/runtime bytes match SHA256`94a9dd85bee742f03543138e877b39adec2e7058b9c732911025ef976ec7cf77`. Blender’s identical-pixel reproducibility rebuild changed embedded render-duration metadata; the source verifier records zero source/contact mismatches and the exact RGBA hash. No palette or sprite geometry changed during that rebuild.

Native `opening-cellar-green.xml` records **714/714 passed, zero skipped**,08:06:09–08:07:06Z. This includes85 new cellar scope/art cases and the629 previous opening, motion, state, projection and examine controls. The MCP bridge falsely timed out initialization and the local180-second watcher expired before native output; the fresh completed XML was retrieved independently. It is the acceptance evidence, not the bridge’s status. A launch requested during runner teardown was refused because the scene was not clean; a later read-only state check confirmed the restored clean SampleScene, and the isolated launch then succeeded.

Ordinary keyboard journey, seed64:

- Fresh N, player40,12. Walk around the existing blocked crossing to glade stairs28,19; descend normally. At cellar40,11 the actual upward stairs are visible beneath the previous player position. Continuous masonry bounds the current corridor; remembered geometry is dim. `cellar-native-entry.png/json`.
- Walk to46,5 through the north service route, then detour through46,2→50,2 around the real beam48,5. No wall or blocker was moved. `cellar-native-service.png/json`.
- Reach64,6 at40HP. The actual crate64,7, pallet65,8, ripe sootroot65,9 and dry seed-stage crop65,10 use native art. `cellar-native-store.png/json`.
- L/Down/Enter/X examines the real crate, retaining its finite-supply prose and native silhouette. Open and Tab take all3 contained entries (clay stack2, Shattered Rime and Ditchkeeper’s Footwork). Container remains, Contents0. `cellar-native-crate-examine.png/json` also exposes the portrait color defect below.
- At65,7 select pallet65,8 and H dismantle. It disappears immediately and two SalvagedTimber enter the pack. At65,8 select ripe65,9 and H harvest. The plant disappears; two pulp owners and one seed are left in that bed. `cellar-native-harvest.png/json` records crate0, absent pallet/ripe owner, intact dry stage0 crop and real outputs. Step into65,9, G/Tab collects the products.
- The attempted return key sequence encountered the moving guard in the service route. The player died; `cellar-native-return-death.png/json` records HP0. **This attempt did not complete return, revisit or save/reload acceptance.** Death was not undone through execution code, and the enemy was not moved or removed. Normal input retry is still required.
- The isolated session was finished and restored. `cellar-native-first-cleanup.json` confirms Play/pending/compile/isolation/view flags false and original SampleScene restored.

All six screenshots are actual1280×800 Game output, verified from PNG headers. Editor-execution Screen.width/height sometimes reported1576×1139; receipts label those as editor values instead of misreporting capture resolution. Console UI showed0 errors and2 existing Metal depth-surface warnings.

### In-phase self-review

- 🟡 Fixed before import: the existing stepped ruin pillar would imply gaps in solid cellar walls. Added one dedicated full-cell ninth masonry frame and confirmed revised native RED before runtime change.
- 🟡 Fixed before final acceptance: the native examine IMGUI path double-encoded the source palette in the Linear project. All4 crate colors matched a second sRGB encoding exactly. The narrow native portrait drawer/shader corrects this without changing source/imports or legacy/model drawing. GPU counterchecks and the actual corrected capture pass, as recorded below.
- 🔵 Independent code review found no remaining P1/P2 in cellar admission, ownership, state routing, optional resources or disposal. Independent world-image review accepted corridor/wall separation and the actual store silhouettes. Palette defect above is a separate real finding, not waived by714 green tests.
- 🧪 Remaining: full sootroot growth via watering, mouse/controller behavior, Deck hardware and normal packaged activation. Return/revisit and save/reload passed in the second ordinary run below.

## Final portrait correction and ordinary journey

`FlatPixelPortraitDrawer` borrows the authorized texture/UV and caches one disposable material. The resource shader decodes the source sRGB once, applies an explicit linear tint and lets the framebuffer encode once. `AnnouncementUI` selects it only for its recorded flat non-model portrait; previous model and legacy draw paths remain unchanged. There is no texture copying, readback, new world scan or global color-state change at runtime.

Native palette RED was9/9 expected failures at08:20:41Z. GPU GREEN was9/9 at08:23:40–41Z; the affected examine/cellar regression was **348/348, zero skipped**,08:25:50–08:26:37Z. The348 receipt overlaps the earlier714 scope; these are not1,062 unique tests. The native714 includes85 new cellar cases; the subsequent9 new GPU cases bring this batch to94 new cases.

The second isolated fresh seed64 game used ordinary keyboard input throughout:

1. Descend at the actual glade stairs and repeat the service route around the beam. At64,6, inspect crate64,7 with L/Down/Enter/X. `cellar-native-crate-examine-corrected.png/json` records the actual popup at40HP. Independent read-only analysis matches all1,386 opaque display pixels to the source crate at3×, with **zero channel or geometry mismatch**; all2,070 transparent frame pixels retain panel background. `cellar-native-portrait-palette-review.json` preserves both RED/GREEN image hashes and comparison. This closes the observed color defect in actual OnGUI, beyond the offscreen tests.
2. Open/take the crate's3 entries; dismantle the pallet; harvest and collect ripe sootroot. The moving guard reached the store and reduced health40→28 during these actions. Press6/Enter/KP7 to cast the existing Calm northwest; the log confirms the guard becomes peaceful. `cellar-native-supplies-final.png/json` records player65,9, crate0, absent pallet/ripe owner, intact dry stage0 crop, and real inventory gains: fire clay2, timber2, pulp2, seed1, Shattered Rime and Ditchkeeper's Footwork.
3. Walk south3 then west25 along the broad corridor; ascend normally. `cellar-native-return-up.png/json` records glade28,19 at28HP. Descend again: `cellar-native-revisit.png/json` records cellar40,12 with unchanged inventory and depleted owners. The store is outside visibility at the landing; its depletion is confirmed by the read-only owner receipt, not claimed visible in that screenshot.
4. PressF5 at40,12; the log confirms saved. Move up to40,11 (`cellar-native-saved-away.png/json`), then pressF6. `cellar-native-reloaded.png/json` confirms loaded, player restored to40,12,28HP, flat mode active, inventory preserved, crate still0 and pallet/ripe still absent. No supplies, health, enemies or fog were changed through execution code. This is an in-session quicksave/load test, not a cold application restart or legacy-save migration test.
5. Finish the isolated editor session. `cellar-native-final-cleanup.json` confirms Play, pending, compiling, save isolation and view-pending all false; original SampleScene restored. Console UI showed0 errors and2 existing Metal depth-surface warnings.

The six final screenshots are actual1280×800 Game output verified by PNG headers; editor Screen values are recorded separately. The failed first return and old portrait capture remain as honest evidence of iteration.

**Final self-review:** 🟡 corrected floor-as-prop classification, misleading masonry silhouette and double-gamma portrait before acceptance. 🔵 Independent source, native contact, world screenshot and actual portrait pixel reviews found no remaining P1/P2 within this packet. Root reviewed production diffs, state/removal/visibility counters and disposal. 🧪 Full crop growth, all dropped equipment art, physical Deck input/performance and release activation remain separate work. The four connected surface addresses are still unadmitted until their own art packets.

**Can verify:** native714 and overlapping348 results,9 GPU cases, source/resource identity, ordinary descent/service route/loot/dismantle/harvest/Calm/return/revisit/quicksave-load, correct actual portrait pixels and editor cleanup. **Cannot verify:** physical Deck behavior, fresh-process reload, every growth/weather state or whole-region/release readiness. This remains an opt-in development view of the glade and cellar only.

**Files:** modified `FlatPixelGreyboxCatalog.cs`, `FlatPixelOpeningArtCatalog.cs`, `ZoneRenderer.FlatPixel.cs`, `AnnouncementUI.cs`; new `FlatPixelPortraitDrawer.cs` and shader with metas; three new test fixtures/metas (`FlatPixelCellarScopeTests`, `FlatPixelOpeningCellarArtTests`, `FlatPixelPortraitColorTests`); cellar resource PNG/meta; editable source, generator/import snippet and detached census tool; concept, native test, census and actual Game evidence; this document plus connected-sites, examine and main art-direction updates.

**Scope divergence:** six-zone admission was narrowed to glade+cellar while the full12-graph census remains useful for later packets. Initial6/optional8 frames became9 after real solid-wall review. Added the bounded portrait color fix because actual inspection exposed it; neither palette assets nor gameplay changed. Next: north connector and seed plot, starting from their measured ordinary surroundings and an actual playable greybox, then the same capture/imagegen/native-source/integration workflow.
