# G3 kitchen court and orchard identity

Status, 2026-10-26: bounded kitchen/orchard slice accepted in ordinary native Unity Play, with260/260 tests following41expectedRED failures. Includes real harvest, save/load, border return and earned pan repair. Packages and full opening/regional acceptance remain open.

## Visible defect and intended result

The actual packaged fresh-game journey, seed `200893250`, reaches Orven's kitchen with native trees, cook and service furniture but literal `6` glyphs for the orchard's beehives. The 15 existing court floor owners produce repeated small, bead-like marks at the current camera scale, competing with the small service objects. The prior source uses two or three stone forms per frame; the defect is their observed visual rhythm, not an assertion that the source contains many separate pebbles. Give the hives a clear ochre, ridged silhouette and simplify the court texture to one or two broad quiet stones per cell. Preserve the approved cook, trees, service objects, walls and all world geometry.

Actual baseline evidence:

- [Kitchen arrival](Verification/Live16x24/packaged-glade-east-18-kitchen-court.jpg): player `(24,10)`, HP `30/42`, 50 drams, actual macOS standalone journey.
- [Quit and Continue at the kitchen](Verification/Live16x24/packaged-glade-east-22-continued-kitchen.jpg): player `(23,9)`, same displayed HP and drams; Focus identifies the actual worn stepping stones.

These are gameplay captures from package source `dce87081d30286ffad870b7b300811f6d898d0e5`, not from this new patch. They do not verify new art. Existing quiet ground and orchard topology provide the playable layout baseline; this pass does not need a new generation layout.

## Verification sweep and corrected assumptions

| Question | Verified source and consequence |
| --- | --- |
| What owns a hive? | `Assets/Resources/Content/Blueprints/Objects.json`, `Beehive`, is a non-solid `PhysicalObject` with glyph `6`, RenderLayer 4 and `HarvestablePart` yielding 1–2 `Honeycomb`. PopulationBuilder records real BerryBush/Beehive forage receipts. Do not add decorative duplicate hives or change population budgets. |
| Does harvesting leave an empty hive? | `HarvestablePart.DoHarvest` sets `Harvested` in the inventory transaction, removes the whole world owner, then marks its cell dirty. There is no persistent depleted-hive entity, regrowth timer, bee population or hive anger state. One unspent identity frame is sufficient; successful harvest reveals the existing ground. |
| Are 3D hive variants lifecycle states? | `SpreadSceneryRecipes` recognizes the same exact owner and uses preserved `spread-scenery-beehive-0/-1` presentation variants. They are not full/empty gameplay states. Preserve the old presentation and models. |
| Is native hive art already available? | No existing flat catalog owns `Beehive`; the current gameplay path falls through to `FlatPixelFallbackCatalog`'s canonical glyph. The actual baseline shows that discontinuity. |
| Where is flat art admitted? | `FlatPixelGreyboxCatalog.IsAdmitted` covers the exact glade, cellar, two northern graphs, eastern shelter and kitchen, subject to existing current-graph/Spread/no-POI authority. Extending `EastKitchenArtFor` broadly would also widen unrelated roles. Use a separate narrow optional hive companion inside the already-admitted path. |
| Is a subject sprite an authorization API? | No. `TryGetPresentationSubjectSprite` supplies semantic art; the caller's current examine document controls disclosure/custody. Keep world visibility/current owner selection in `FlatPixelFallbackCatalog` and reader authorization in the existing reader. |
| Can the court be simplified by deleting floor placements? | Not in this pass. `SpreadExplorationResidents.CourtStones` participates in candidate geometry/Fits and contains 15 placements. Removing entries could select a different complete service site. Change the borrowed texture resource only; preserve every floor owner, role marker, substrate and position. |
| Does honeycomb need new gameplay? | No. Existing `Honeycomb` is food/cooking material and supports honey coating. Its product art is outside this minimal scene pass. Do not claim a new honey use or native product sprite. |

This is original Caves of Ooo art fitted to the approved flat 16×24 style, not a claim of direct Caves of Qud code or asset parity.

## Required pipeline record

| Gate | Evidence / next action |
| --- | --- |
| Playable layout and capture | Reuse the actual package journey above, its existing service approaches and real owners. No detached art scene. |
| Imagegen reinterpretation | `ArtSource/Live16x24/concepts/kitchen-orchard/scene-prompt.txt` → `scene-v1.png`. Root used the actual kitchen capture. |
| Parts extraction | `parts-prompt.txt` → `parts-v1.png` in the same directory. Accept the ridged ochre dome/dark entrance and one or two broad floor stones. Reject generated glow, soft alpha, excessive highlights, text changes and displaced pixels/owners. |
| Editable native production | Artist supplied `orchard-hive.png` (16×24) and `working-court-v2.png` (48×24, three cells), editable `ArtSource/Live16x24/kitchen-orchard/kitchen-orchard.blend`, exact `source-cells.json`, generator, manifest and source verification. Prior working-court pixels and approved sprites remain. Native contact sheet review accepts quiet broad shapes; this is source review, not gameplay acceptance. |
| Real integration | Saved after native RED: a small `FlatPixelOrchardCatalog` and existing world/exact-subject call sites; only `FlatPixelWorkingCourtCatalog.ResourcePath` changes for floor art. No generation, blueprint, harvest, collision or UI action change. |
| Native verification | New `FlatPixelKitchenOrchardTests`: RED 42 total, 41 failed, 1 passed, 0 skipped at 07:17:53–07:17:56 UTC. Final GREEN: 260/260 across eight fixtures, zero failures/skips, at 07:21:53 UTC. See `Verification/Live16x24/kitchen-orchard-native-{red,green}-summary.json` and matching XML. Full fixture list is below. |
| Ordinary gameplay acceptance | Load the earned kitchen checkpoint through normal Continue, inspect court from the same approach and use a real hive through normal controls. Capture before/after harvest and return/save-reload. Confirm actual owner receipts, no false depleted sprite, no service displacement and no new selection obstruction. Pending. |
| Performance | Shared cached Tile/Sprite resources on existing Tilemaps; no per-hive GameObjects, animator, polling or neighbor scan. Confirm resource reuse by tests; record any measured gameplay profiling separately. No numerical performance claim yet. |

## Owner/state and native asset contract

| Real owner / state | Native treatment | Preserved behavior and refusal |
| --- | --- | --- |
| `Beehive`, current visible unspent owner | `Sprites/Live16x24/orchard-hive`, one 16×24 frame, centered pivot `(8,12)`, PPU 16, Point/Clamp, no mipmaps, transparent exterior | Exact blueprint, owned Render/Physics/Harvestable; canonical glyph `6`, no explicit art override; non-solid/nonportable, not actor/item; valid Honeycomb yield range and positive legal chance. Neutral identity does not advertise exact stock, guaranteed yield, friendliness or bee activity. |
| Harvested/stale hive reference | No hive frame | Actual success removes owner. Spent, missing, foreign or malformed semantics cannot borrow the live identity. No permanent empty-hive frame or invented regrowth. |
| Hive under fog or stale world graph | Existing fallback policy | Layer-4 non-terrain source is not remembered as a current object. An atlas cannot disclose a hidden owner or bypass exact graph/cell/physics ownership. |
| Hive in any other area | Existing canonical fallback | No expansion of six-zone admission. The new image is not permission to silently convert other regional kits. |
| Exact current `StoneFloor` with both `court-floor` markers over eligible Grass in Kitchen | `Sprites/Live16x24/working-court-v2`, three 16×24 cells, same coordinate variant choice | Existing `FlatPixelWorkingCourtCatalog` checks, foreground ownership, court layout, 15 placements and plantable Grass stay unchanged. Hazards, soil/crops, explicit art, wrong markers, other regions and unseen cells continue to outrank/refuse this floor. |
| Prior court atlas | Retained `working-court` resource | Explicit injected old atlas still works; the old art remains recoverable. No source deletion. |

Implemented integration shape:

```csharp
// Inside the existing current-owner and admitted-zone presentation flow:
var orchard = local ? OrchardArtFor(owner) : null;
bool orchardNative = orchard != null && orchard.TryResolve(owner, out art);
// Include orchardNative in native foreground choice and exact-subject reuse.
// Working court behavior is unchanged apart from its resource identifier.
```

The adapter owns only cached Tile/Sprite objects and borrows its texture. Injected null or malformed textures stay unavailable; they must not silently load production replacements. Existing injected world constructors remain deterministic. Disposing twice must release owned wrappers while preserving borrowed pixels.

## Native test plan

`Assets/Tests/EditMode/Presentation/Rendering/FlatPixelKitchenOrchardTests.cs` is saved before production integration, with 42 cases:

| Invariant | Positive / counter-check |
| --- | --- |
| Actual world integration | Six current admitted graphs render the real hive as native RGB art and share its cached exact-subject sprite. These tests use the shipped `TryResolvePresentation` entry point rather than testing only an unused adapter. |
| Exact semantics | 28 cases first accept a canonical hive, then reject spent/wrong yield/invalid range/chance, foreign/missing parts, invisible/explicit art/glyph variants, portable/solid/actor/item identity, multi-cell/empty or foreign stack, and foreign blueprint. |
| Pure presentation | Repeated world/portrait queries preserve the exact owner list, cell, harvest flag and altered-but-valid yield numbers. Texture import settings are not mutated. |
| Real lifecycle | Existing harvest command yields 1–2 Honeycomb, removes the exact hive, reveals ground, rejects repeated harvest and cannot resolve a retained depleted reference. |
| Disclosure and graph authority | Hidden/unexplored cells, moved owner's stale list entry, replaced current graph and non-admitted area keep their prior behavior. |
| Resource lifecycle | Missing and malformed native pixels fall back to glyph `6`; disposal releases cached sprites/tiles without destroying source pixels. |
| Court source selection | Normal default rendering and exact-floor reader use `working-court-v2`; original atlas remains loadable and explicitly usable. Rendering leaves the owner list untouched. Existing broad court tests retain hazard and floor-eligibility counter-checks. |
| Old injected constructor | Null injected world resources do not implicitly pick up the new hive asset. |

Native RED confirmed the expected missing routing/companion and old floor resource selection. The existing injected fallback counter passed. Production changes were saved only after root reported that receipt and confirmed the completed concept/extraction gate. Native GREEN then passed all 260 cases from `FlatPixelKitchenOrchardTests` (42), `FlatPixelWorkingCourtTests`, `FlatPixelEastKitchenTests`, `FlatPixelNorthVegetationTests`, `FlatPixelFallbackCatalogTests`, `FlatPixelGreyboxCatalogTests`, `DensityHarvestSecurityTests` and `SpreadWorkingCourtTests`. These include original court placement and hazard guards, so the texture change does not rely only on new identity tests.

## Self-review and scope limits

- 🟡 Corrected a false lifecycle assumption before authoring: harvest destroys the hive owner. There will be no invented empty or animated bee state.
- 🟡 Preserved generation geometry: changing the 15 `CourtStones` specs could relocate the entire service. Only its texture will change.
- 🔵 The hive silhouette should identify a modest physical object, not a bright quest marker. Generated glow and soft edges are explicitly rejected.
- 🔵 Independent preflight review found no material test-contract issue. It corrected the description of the old court: the small bead-like pattern is the observed camera-scale appearance, not its literal source component count.
- 🔵 Independent final runtime source review found no significant defects. The new catalog is narrow, read-only and disposable; no generation, authority or transaction API changed. Native GREEN passed all 260 selected cases.
- 🧪 Actual rendered art, harvest replay, save-return and visual comparison are pending. Tests and concepts alone cannot close the gameplay acceptance gate.
- ⚪ Honeycomb product art, broader orchard rearrangement, walls and Nella service continuity are not part of this patch. Other agents' work must remain separate.
- Can verify here: audited mechanics, admission/call paths, saved source integration, independent review and recorded native RED/GREEN. Cannot yet verify: integrated appearance, animation/feel, measured performance, a new packaged run or physical Steam Deck controls/performance.

## Files and implementation log

Owned by this implementation agent: this plan; new `FlatPixelKitchenOrchardTests.cs` and its fresh metadata; new narrow `FlatPixelOrchardCatalog.cs` and metadata, `FlatPixelGreyboxCatalog` integration and the working-court resource identifier only. Artist/root own art, concept files, actual captures, native runs, packaging and remote delivery.

- 2026-10-26: read the required graphics pipeline and current package continuation; audited owner, harvest, renderer and court placement source. Reviewed actual kitchen arrival/Continue captures and extracted concept parts.
- 2026-10-26: saved 42-case fixture and metadata before runtime edits. Root confirmed native RED (42 total / 41 failed / 1 passed / 0 skipped); all failures matched the native ownership/routing or old texture defects.
- 2026-10-26: saved the minimal production candidate after RED; exact hive adapter uses shared cached wrappers and existing admission/visibility/reader paths. Court changes only its resource identifier.
- 2026-10-26: root recorded native GREEN 260/260 across eight fixtures, zero failures/skips. Independent source review found no significant defects. Root owns the pending ordinary replay and will append exact actual acceptance separately.

## Ordinary gameplay acceptance — 2026-10-26 07:33 UTC

**Bounded slice accepted.** Root loaded the unchanged earned seed `200893250`
checkpoint through normal Continue in an isolated save root, twice, before and
following integration. No actor/stock grants, teleportation, forced visibility
or gameplay mutations were used. Actual engine `CaptureScreenshot` output is
1280×800. The read-only observer and exact keyboard route are preserved in
`Verification/Live16x24/kitchen-orchard-observer.cs.txt` and
`kitchen-orchard-play-journal.json`.

| Actual action | Evidence and observed result |
| --- | --- |
| Court comparison | `kitchen-orchard-before-court.png` / `after-court.png`: player23,9, tick4290, HP30/42, 50drams and inventory match. Broad quiet stones replace the bead-like marks. All eight observed service/crop owner positions match; generation geometry is unchanged. |
| Walk to hive | Before/after-hive captures both show player11,15, tick4470. After receipt names hive13737 at10,15 and13738 at25,19. Both use original16×24 ochre identity art. NPC positions differ between replays; this is a matched player approach, not identical whole-world simulation. |
| Examine | `kitchen-orchard-hive-examine.png`: normal C→Left→x shows the matching native portrait and existing wax/honey description; time remains4470. |
| Finite harvest | Escape→C→Left→h removes13737; tick4480, Honeycomb13790 x2 in actual inventory, second hive13738 remains. `kitchen-orchard-harvested.png/json`. No invented depleted sprite. |
| Save/load | F5, moveRight, F6 restores11,15/tick4480; same Honeycomb13790 x2 and remaining hive13738, harvested13737 absent. `kitchen-orchard-harvest-reloaded.png/json`. |
| Exit/return | Left12 reaches western Overworld.11.11.0 at79,15/tick4600; Right12 returns kitchen11,15/tick4720 with the same depleted state and honey. Actual outside graph remains flat glyph fallback, visibly documenting incomplete regional art. `western-exit` / `returned` captures. |
| Service access | Return via13,9→22,9→22,6; C→Right exposes real pan13771 repair using two earned FireClay. `pan-access` at tick4920. The existing service remains reachable. |
| Paid repair/reload | r spends the two clay; pan13771 at23,6 becomes repaired by2711, cause2407688c84674ab793708de5c696ebc5. Tick4930, 30HP/50drams, Honeycomb x2 retained. F5/move/F6 preserves repair and stock. `pan-repaired` / `pan-reloaded`. Batch remainsIdle; no meal completion or cook introduction is claimed. |

The new earned replay fixture is
`Verification/Live16x24/Fixtures/kitchen-orchard-earned-repaired` (same gameID
`2e93a161b86a495483a3c2d81dab34e2`). Quick.sav.gz SHA256:
`bc5c4c606b1d080618ac39c488f3a2d3ea47ef47e852cfdee2294af7e621df25`.
It is a saved result of ordinary inputs, not an authored replacement world.
The original kitchen fixture is retained.

Independent reviews found no significant source, runtime or ordinary-scale
visual defect. The hive reads as a physical object without glow; the broad
stones support the gold service props and approved Cook. All65 protected art
and editable source hashes match. One neutral hive frame and three ground
variants were produced by the saved Blender integer-cell generator, not by
resizing the concepts. Original court/3D art remains available.

### Performance and bounds

`kitchen-orchard-profile.json` records single Editor observations at the same
23,9/tick4290 view before and after:38draw calls,38batches,64SetPass calls,
8022triangles,16064vertices in both. Shared Tile/Sprite wrappers and original
Tilemaps are used; no per-hive scene objects were added. This demonstrates no
draw-call growth in that sampled view; it is **not** a timing benchmark or
physical Deck performance acceptance. Editor allocation changed after tests
and domain activity; no attribution or memory-improvement claim is made.

Can verify: native routing/counter-checks260/260, clean original-pixel import,
actual world/reader appearance, finite harvest/removal, F5/F6 and graph return,
earned pan repair, the sampled render statistics. Cannot verify here: physical
Deck controls/performance, Linux execution, new package activation, arbitrary
seeds, full six-service completion or whole-opening/regional art completeness.
Dated current Mac/Linux packages remain frozen at `dce87081d`, before both the
underfoot fix and this artwork; they are not advertised as containing this patch.

### Preservation and continuation

Normal776 save/backup files and both foreign AllotmentNotice PNGs match their
pre-run hashes. Root restored stopped SampleScene, preferences/view and normal
save root.26 importer whitespace-only rewrites were backed up outside the repo
and restored; no storage was deleted (429GiB free). Receipts:
`kitchen-orchard-preservation.json`, `kitchen-orchard-editor-restored.json`.

Next graphics defect: the kitchen's long hedges repeat the same scalloped cap
on every segment. Plan disclosed-neighbor end/corner/interior native silhouettes
without changing owner placement, collision or approved service art. A separate
read-only Nella audit found intentional mortality plus a first-contact reliability
risk: resident placement excludes creatures within3cells, but hostile vipers can
acquire from10. The recorded death does not establish the exact initial distance;
reproduce that before any gameplay fix. Do not grant immunity or silently drop
Nella to solve it. These remain separate follow-up work, not claims for this patch.
