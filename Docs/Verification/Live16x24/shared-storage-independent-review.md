# Shared storage — independent source review

Reviewed 2026-10-27. **Bounded source, native and five-owner actual body/reader verification: PASS.** The injected-constructor regression is corrected, and the final root-run receipts contain 606 distinct Passed identities with no failures or skips. Independent viewing of the five actual genuine storage bodies and readers found no blocking geometry or identity mismatch. Crate transfer and fresh Continue have functional evidence; the retained Pause/clean-world image pair does not prove pixel equality. Ordinary Mimic, successful-key actual interaction, packages and whole-area polish remain unaccepted. The initial findings and their pre-GREEN state below are retained as history, followed by the final native and actual checkpoints.

## Scope and independence

The reviewer did not author this storage runtime or its43-case fixture, and did not operate Unity/Git or execute tests. This pass read the new companion, four Greybox integration paths, established painter/reader authority, actual source blueprints, four immutable image inputs, existing anchor regressions and retained root-run RED receipts. Source review is independent of the runtime/test authors. No simulation, reader information policy, art geometry or pixels were changed by this review.

Frozen body-only scope is five genuine owners—Chest, LockedChest, Crate, StrongBox, OreCache—and a separate live MimicChest branch. Chest, LockedChest and live Mimic share one cached Chest body in both dormant and awake states. There are no lock/open/full/empty/damage/awake frames or overlays. A closed body denotes the storage object, not permission to access it. Existing actions and menus remain authoritative for locks and contents.

## Finding and narrow correction

**Initial finding:** new authoritative shared-storage refusal superseded older opening Chest/Crate art, but `SharedStorageArtFor` initially constructed a companion only when `_loadOptionalResources` was true. All established injected Greybox constructors leave that false. Thus a valid explicitly supplied anchors/cellar group stopped providing its Chest/Crate native body even though the input existed. The new focused fixture's default/explicit-companion positives did not cover that compatibility boundary.

Root ran the existing39-case anchor suite before correction. `shared-storage-injected-anchors-red.json` independently parses to **34 Passed and5 Failed**, retaining all39 results. Failures are the Chest frame6 positive, positive-before-foreign-Container test, anchor-only injection test, owner replacement test and borrowed-resource/peer-disposal test. This is behavioral evidence for the reported constructor defect, not a newly invented test expectation.

Saved correction retains borrowed `_storageAnchorsAtlas` and `_storageCellarAtlas` in the terminal thirteen-input Greybox constructor. Lazy loading now uses the ordinary default companion only when optional resource loading is enabled; otherwise, if either borrowed group is present, it constructs the companion from those exact references with null StrongBox/OreCache inputs. If both are absent, it stays unavailable. An already assigned `_sharedStorageArt`, including an explicitly injected instance with unavailable groups, is never replaced. No Resources lookup or older-art bypass is introduced for the injected path.

The final source read confirms that this addresses the identified failure while preserving authoritative refusal and group independence. Its native recovery was pending at the initial source-review boundary and is verified in the final native checkpoint below.

## Runtime and integration findings

- Exact recognition is limited to the six named IDs. Required Render/Physics/Container/Examinable references are unique and owned; visibility, canonical glyph, custom visual ID/variant/glyph variants, custody, takeability and one-cell footprint constraints remain guarded. The selected modern world owner still comes from the established current graph/placement/disclosure selector. There is no biome or room-layout admission expansion.
- Genuine containers remain separate from creatures. Wood bodies preserve their source14/1,18/2 and10/0 structure contracts and accept surviving positive structural HP rather than only pristine health. Gone/invalid structure, wrong material and repurposed wreckage refuse. Chest/LockedChest require their authored Thermal part; Crate does not invent one. StrongBox/OreCache do not acquire nonexistent Material/Thermal/Destructible/Harvestable semantics. Required and optional supported Lock/StatusEffects references reject duplicate or foreign ownership. Custom terrain, liquid, crop, harvest, service, door, stair, repair and trigger meanings are conservatively refused.
- Neither combined lock flags, contents, fullness nor post-unlock `Physics.Solid=false` selects a different body. The catalog never opens, unlocks, transfers, destroys or stocks anything. It grants no ground, ambient backing, hazard suppression or collision authority.
- Exact live Mimic requires owned Body/Brain/AIAmbush/Container and a positive owned Hitpoints stat. It allows its real inherited combat/material/status parts and surviving damage, while rejecting generic creature aliases. There is no goal scan, dormant latch inference, damage-baseline initialization, wake action or contents read. Dead/removed bodies remain subject to existing owner and portrait retirement.
- Known-storage success/refusal is authoritative in legacy `TryResolve`, legacy semantic `TryGetSubjectSprite`, modern `TryResolvePresentation` and shared `TryGetPresentationSubjectSprite`. Invalid or unavailable storage cannot fall back into the older weaker local Chest/Crate art. The low-level subject helpers remain semantic lookup, not new reader authorization; the existing document/current-presentation wrapper supplies that authority. The old procedural path's view flags retain their earlier contract; the actual modern painter is the native-cue path.
- Each optional input validates its own dimensions and creates only one cached Sprite/Tile pair. Shared Chest roles borrow the same pair. Disposal is idempotent, destroys only those owned views, and does not destroy input textures or peer views. There is no per-query asset slicing, allocation framework, world scan, RNG or gameplay event dispatch. This is source inspection, not a measured zero-allocation guarantee.

## Native color and information boundaries

Modern successful storage opts into the existing `PreserveRenderColorCue` path. `ZoneRenderer.FlatPixelPaint.ResolveRenderColor` fires one pooled Render event for the selected visible owner, parses the authored and resulting colors, and uses white when they are equal. Therefore authored Chest `&w` and Mimic `&Y` do not create different neutral native body tints. A real event override retains its raw native cue, while the separate legacy lit Tint remains available for glyph fallback. Environment neighbor reclaim validates zone/cell/owner/Render identity and reuses that receipt without another Render event. Catalogs and portraits do not consume flashes or advance effects.

**Body/color parity is not complete information disguise.** Existing WorldCursorRenderer can make visible hostile creatures red. The action menu already uses HealthReadout; ExamineDocumentBuilder already exposes Creature/Flesh versus Object/Wood facts. Those policies are neither hidden nor newly invented here. CombatIntentRenderer remains tied to actual CommittedMelee state; stock Mimic has no new intent component added. The scope fixes the additional native-body/glyph tell while preserving preexisting interaction information. Actual ordinary Mimic observation is still missing.

## Immutable source views and remaining visual gate

| Input | Borrowed view | SHA-256 |
| --- | --- | --- |
| opening-anchors192×24 | frame6, Rect96,0,16,24 | `aacb70020e54fe9322d507c1f1fadbcf9004bb4f7f8715ad4cb7105cea8ff98f` |
| opening-cellar144×24 | frame1, Rect16,0,16,24 | `94a9dd85bee742f03543138e877b39adec2e7058b9c732911025ef976ec7cf77` |
| cont_strongbox16×16 | full Rect0,0,16,16 | `eeafc5515ded0031254e45963acd5ecbeca586aaf8b15d2051803d7a81b539e0` |
| cont_ore_cache16×16 | full Rect0,0,16,16 | `673011057ee2b01346b8dd0339b5a816243e3e9ca5fa1f8dcd098011c4f902a8` |

All four hashes match the independent preimplementation inspection. They have binary alpha. The new cached sprites use centered16PPU FullRect views; they do not use the legacy StrongBox14×9/OreCache12×8 cropped zero-pivot importer sub-sprites or stretch squares to rectangular cells. Original contacts/source images and actual Chest/Crate/StrongBox before views were inspected; native after comparison is not yet established. Square occupied pixels sit six native rows higher than the lower-aligned wooden sources under this centered policy, which remains an explicit real-world alignment/readability trial item, not a speculative redraw requirement.

## Evidence and limits

Initial root-run `shared-storage-native-red.json` contains **43 distinct Failed results,0 Passed,0 skipped**, matching its summary. The separate anchor compatibility RED contains39 retained results,34 Passed/5 Failed. These executions must not be added together as final acceptance, and this reviewer ran neither.

| Reviewed file | SHA-256 |
| --- | --- |
| FlatPixelSharedStorageCatalog.cs | `2c3913eb439625c36f5df97d423f54f0daa396b2bc01b96b50750911799ef0dd` |
| FlatPixelGreyboxCatalog.cs after constructor fix | `49ffcbb269b032de5b5d51635e0c7656e4e7adf0f2f0d71af6c72b0533d3a072` |
| Initial43-case native RED JSON | `8ebf1d5a80be6fc70fedefb5532eab0182a36a929d3ae676c6bacb3c80aad603` |
| Injected-anchor RED JSON | `181e6f2bcc3143220cc1741c91e36c96697e2351a8742589e4c2da981bcf9b41` |

The finite fixture exercises representative current ownership/disclosure/custody, actual access/transfer/unlock, structure destruction, real Mimic wake/death and one-event color behavior. Its existence is not proof those tests pass; final focused and relevant regression GREEN remains pending, including the compatibility correction. Actual storage after-play, ordinary Mimic evidence, package delivery and world/reader/underlay comparison also remain pending. No lock-cue readability, awake silhouette, complete disguise policy, full-chunk composition, all-area polish, Linux/Deck execution or performance acceptance is asserted.

## Final native checkpoint —606 distinct passes

Independently parsed both complete final JSON result lists, checked every state, counted unique full test names and checked cross-receipt overlap. Root executed the tests; the reviewer did not rerun them.

- `shared-storage-focused-green.json`: **136/136 Passed**,5.5558454 seconds. Exact fixture counts are43 shared-storage cases,39 existing opening-anchor cases and54 existing cellar-art cases.
- `shared-storage-related-green.json`: **470/470 Passed**,37.5016037 seconds.
- Combined: **606 distinct Passed identities**, zero overlap, failures, skipped or inconclusive result states. Neither the initial43-case RED nor the anchor regression RED is added to this final total.

The initial anchor finding is therefore closed by both saved source and the existing39-case native regression. All54 cellar-art cases also pass with the retained borrowed-cellar path. The retained zero-case attempt and stale XML callback are not used as successful native evidence; the complete current JSON results are the authoritative files reviewed here.

Two test-only corrections were independently compared against preserved source:

1. Before genuine RED, the inaccessible internal `owner.SpatialZone` assertion was replaced by public `zone.GetEntityCell(owner)`/ParentZone checks. The first-compile-attempt and preimplementation snapshots preserve this exact difference.
2. After RED, the Paint fixture detaches its borrowed world catalog from the temporary EnvironmentSpriteRenderer before destroying that renderer. Production EnvironmentSpriteRenderer normally owns/disposes its field; the test intentionally reuses its catalog across regions/save checks. The before-lifetime snapshot differs from the final fixture only in this teardown block. All43 test bodies/assertions remain unchanged; this is not a relaxed body, access or disclosure expectation.

Final hashes were independently rechecked against `shared-storage-native-summary.json`:

| Evidence | SHA-256 |
| --- | --- |
| Focused136 GREEN JSON | `7737a02cd34757f552d254498adbd12356fca7b150910cc2dd4313c52fedd815` |
| Related470 GREEN JSON | `8dd74c87631747667295444ca8e1dd92e2abe0560954c6cb578e37d4c3063ec9` |
| Native summary | `7297e1c3abe2c760e31eecefba297413ac1131c2678d425b24a896cca63391c8` |
| Final FlatPixelSharedStorageTests.cs | `f4c4ab5ecf881e4ac45a1232dadaec9540eb7f4dad7aacbb184f5a400e5ca499` |
| Preserved preimplementation/before-lifetime fixture | `cc1567cd7601b3a8a1150bd2ff21928e4a46c0f26817c47fe2e4ce0cd2d3960e` |
| Preserved first compile attempt | `ad9dd53ed3d07a6716f51018960bd33c134175cda07c8d7b095693f01cbd1bc0` |

The companion and Greybox hashes remain exactly `2c3913eb439625c36f5df97d423f54f0daa396b2bc01b96b50750911799ef0dd` and `49ffcbb269b032de5b5d51635e0c7656e4e7adf0f2f0d71af6c72b0533d3a072`; no post-review runtime change is hidden in the result. This closes the native pending statements above. It does **not** close actual ordinary controls/after-view comparison, unobserved Mimic encounter, lock/awake visual refinement, packages, whole-chunk composition or platform/performance acceptance.


## Actual Editor body/reader checkpoint — five genuine families

Independently opened all 16 `shared-storage-after-01` through `-16` PNGs and the before body/reader views for all five genuine families, at their supplied 1280×800 size. The reviewer operated no game controls. Root performed the ordinary controls; interaction/state conclusions below combine visible UI with the retained `shared-storage-after-play.json` receipt. Its SHA-256 at review is `8484b6886dbd7256d1d6f683600d3b631f75dea8bac25a28b7983aac1cd4f72f`. All 16 individual image hashes and the earned104 metadata/save hashes match that receipt. Companion, Greybox, final fixture and all four borrowed PNG hashes remain the exact values recorded above; there is no intervening art or runtime revision.

| Family and actual owner | Viewed before → after | Bounded result |
| --- | --- | --- |
| Crate67305 at45,1, Stump SummitScrub; player45,2 HP29 | before07/09 → after01/02, plus03–06 | The former `0` becomes a compact upright boarded box with a matching Wood10/10 portrait. Edges and internal boards remain crisp and fit one cell. Open shows three actual items; after taking EmberFruit67306, the contents UI shows only HealingTonic67307 and StunGasGrenade67308. Root's fresh Continue restores player position/HP, both remaining IDs and the transferred fruit's player custody. |
| StrongBox72917 at70,2, sandstone depth1; player70,3 HP23 | before11/13 → after07/08/09 | The former `=` becomes a low iron body with matching full-texture reader. The compact lid/plate silhouette is distinct from nearby rubble, though small and warm under cave grading. The actual Unlock attempt is refused without a matching key; the receipt retains locked/solid truth. There was no successful unlock or transfer. |
| Chest7483 at43,11, Sill; player43,12 HP40 | before04 and surrounding court views → after10/11/12 | The former `=` becomes the approved wooden Chest with matching Wood14/14 portrait. It is visibly different from the taller crate. Actual Open still lists the three grimoires; no item was taken. The rendered Water-Keeper's Grimoire label and the receipt's PurifyWaterGrimoire blueprint describe the same first item, not a stock change. |
| OreCache86894 at34,16, limestone depth4; player44,16 HP33 | before15/16/17 → after13/14 | The former symbol becomes a low mineral pile with pale ore flecks, and the authorized reader clearly shows the same body. This was remote Look/Examine at the unchanged earned88 viewpoint; there was no loot access, transfer or newly demonstrated safe route. |
| LockedChest39340 at61,3, Beating; player62,5 HP7 | before21 → after15/16 | The same approved Chest body replaces the former `=`, with matching Wood18/18 reader. Existing name/description retain lock information. Root continued the preserved poisoned/confused combat interruption and only inspected remotely; HP7 was not a new after-pass damage event or evidence of safe access. No unlocking, looting or healing was exercised. |

The square StrongBox/OreCache inputs remain square, with no visible stretching, clipping or new black tile backing in these views. Their higher occupied-pixel placement relative to the lower-aligned wooden bodies is visible but does not disconnect either from its selected cell or produce a concrete registration defect. This checkpoint accepts the existing immutable geometry for bounded body coverage. It does not establish equal apparent scale or ideal contrast in every region. Reader portraits make the material distinctions considerably clearer than the small world bodies.

### Restart comparison is intentionally not accepted

After05 visibly contains the Pause menu; after06 is the clean world after fresh Continue. Independent RGB comparison of the recorded rectangle `(0,40,940,640)` reproduces **561,159 differing pixels out of564,000**. This is not an equivalent-view comparison, and no screenshot equality is claimed. The receipt explicitly marks it rejected. The functional restart evidence is narrower: the same player45,2/HP29, EmberFruit67306 in player custody, and the same two remaining crate item IDs. The saved fixture is `Fixtures/shared-storage-crate-after104`, turn9100. Root's archived-session/Finish receipts report stopped Play, null isolated root and clean scene; this review reads those receipts rather than independently querying Unity.

### Whole-view composition and remaining acceptance debt

These before/after views demonstrate useful coverage breadth without changing placement or filling space with invented props. The Stump viewport still has a very broad quiet center, sparse furniture/vegetation and subtle solid-dome separation; a native crate does not make that composition finished. The old earned Sill graph still displays repetitive grass and black-backed water glyph rows. The sandstone/limestone views retain broad quiet passages and repeated rubble, and the OreCache is subtle enough to resemble a small rock at normal zoom until selected. The Beating room retains mixed glyph actors and danger cues. These are remaining coverage/composition/contrast issues, not evidence that hidden or unexplored regions are bare.

No blocking body/reader regression was found in this finite actual comparison. **Accepted:** the five disclosed genuine storage bodies, matching authorized portraits, the shown Open/refusal behaviors, and receipt-backed Crate transfer/fresh Continue. **Not accepted by this review:** ordinary Mimic encounter or awake/disguise behavior, successful-key actual unlocking, actual OreCache/LockedChest access, lock/open/empty/awake visual differentiation, continuous travel among the four regions, complete chunks or all-area polish, packaged execution, Linux/Deck execution or performance. The 606 native passes remain separate evidence for the tested semantic/cue/ownership boundaries; they do not replace these missing actual cases.


## Actual local travel and observational profile supplement

Independently opened after17 `shared-storage-after-17-surface-return.png` and after18 `shared-storage-after-18-strongbox-travel-return.png`. The updated after-play receipt SHA-256 is `9a593748084fb3229bbb7bbde82da939728923d934d83c5c821ba628c5eb036e`; this supersedes the earlier 16-capture receipt hash for the added supplement, without changing that earlier review boundary. Both new capture hashes and both earned105 save/metadata hashes match. The recorded ordinary route is cave0.9.1 player70,3 → existing upstairs56,7 → surface0.9.0 → downstairs back to cave0.9.1 →70,3, with HP23 throughout. The returned view shows the same compact native StrongBox above the player in the same sandstone passage. The retained earned `Fixtures/shared-storage-travel105` is turn11290, HP23, cave0.9.1. This closes a bounded local vertical-travel continuity check; it does not establish a continuous journey through the four earlier storage regions or another successful-key/access interaction.

The surface image adds a clear composition limitation: the disclosed scraped-ground viewport contains a very broad plain field with scattered small shoots and a few remaining glyph-backed owners. It is visually sparse. That observation concerns the actual visible surface, not fog/undisclosed cells, and does not authorize decorative filler or promote its area polish. The return cave view has no new body-registration or material discontinuity visible. Root's supplemental Finish receipt again reports stopped Play, null isolated root and clean scene; no Unity query was performed by this reviewer.

Independently parsed all four raw profile CSVs and recomputed their counts, means, maxima and the receipt's percentile convention: sorted index `floor(0.95 × (count−1))`. All values match. Each channel retains 45,131 samples, including its one zero sample; capacity131,072 was not reached and the receipt says unwrapped. UTC endpoints independently subtract to117.230491 seconds. Profile receipt SHA-256: `971211b45121ae9fd88a992843d86ce1588d98feb2328d9dc4d97ef1000aeb09`.

| Recorded channel | Mean | p95 | Maximum |
| --- | ---: | ---: | ---: |
| ZoneRenderer.LateUpdate | 0.132917 ms | 0.193000 ms | 93.922250 ms |
| Input.Update | 0.033549 ms | 0.028625 ms | 441.775917 ms |
| Main Thread | 2.592895 ms | 5.465125 ms | 1798.074834 ms |
| GC Allocated In Frame | 50,320.066 bytes | 18,509 bytes | 440,784,590 bytes |

These are observations during Editor movement, stair transitions, idle periods and tool instrumentation. Large recorded stalls/allocation maxima are retained, not discarded. There is no matched pre-change comparison or isolation of the storage adapter, so these data neither demonstrate an adapter regression nor certify sustained FPS, a zero-allocation hot path, standalone performance or physical Deck behavior. The finite 606 native results and source remain unchanged; this supplement adds actual local travel/profile evidence only.
