# Allotment notice model fix

Status: implemented after current native RED; 79/79 affected art tests pass and matched controlled native pixels inspected, 2026-10-04. Root owns the editor and publication. This completes the bounded mapping correction; unaided discovery and all-camera legibility are not claimed.

## Outcome and scope

Draw the existing allotment notice in `Overworld.2.6.0` with the already imported original wooden sign model. Preserve its actual owner, location, collision, Examine text and regional guidance. This is a presentation completion for one existing native source, not a new sign, gameplay interaction, world-generation rule or global scenery permission.

Use the existing `ring-grove-sign` model through the catalog's `GroveSign` binding. Its original source is a wooden stem and board with abstract grain and one leaf; it contains no authored text or rune. The model is a static entity using the existing ring palette, with 156 source triangles. It is already present in the source catalog, imported prefab and `Assets/Resources/SpawnRing3D/Library.asset`. No mesh, material, prefab, catalog schema or importer change is planned. A native screenshot still must establish that this reused form reads clearly at the actual notice location.

## Source verification and corrections

| Earlier premise | Current source evidence and correction |
| --- | --- |
| The three failures concern an unspecified sign in a prior settlement. | `RepairCultivationSite.cs` declares the Grovelands field address `Overworld.2.6.0`, creates a real `Signpost` with role `RepairCultivation.Role=notice`, and gives it ID `repair-cultivation:allotment-notice` and display name `allotment notice`. Its Examine text describes the existing allotment mechanics. It is not a settlement-specific new source. |
| Signpost art is missing altogether. | `SpreadSceneryRecipes` already maps canonical Signpost owners to two imported `spread-scenery-signpost-*` models. `SpreadSceneryWorldRecipes` correctly requires actual current Spread scope, which this Grovelands field lacks. Widening that scope would claim unrelated scenery and styles. |
| A catalog or art rebuild is needed. | The native catalog already maps `GroveSign` to `ring-grove-sign`, and both its model record and imported library entry exist. A bounded visual alias can borrow that exact model while retaining the Signpost owner. |
| Reusing the Spread scenery model is automatically a one-line fix. | Its authored-mesh registration remains inside the Spread/glade path. Cultivation can already supply the borrowed palette in this field, but that does not register the scenery meshes. The base ring model already uses the receiving ring pipeline and avoids expanding those contracts. |
| The old failure can be called a fresh test result. | `Docs/Verification/EnemyFieldMedicine/native-signpost-baseline.xml` records the three actual failures for seeds 64, 1729 and 729490642, each reporting `Overworld.2.6.0 Signpost: unmodeled-native-blueprint`. `regression-comparison.json` records the same failures on baseline and the medicine feature. Current source still has the same missing path, but this read-only sweep did not rerun Unity. Root will record the current gate before production. |

Sources read: `CLAUDE.md`; `Docs/ENEMY-FIELD-MEDICINE-DESIGN.md`; the two verification files above; `RepairCultivationSite.cs`; `MorrowfastExpedition.cs`; `SpreadPresentationScope.cs`; `AreaCompositionScope.cs`; `SpawnRing3DRecipes.cs`; `SpawnRing3DCatalog.cs`; `SpawnRing3DLibrary.cs`; `SpawnRing3DPresenter.cs`; `SpawnRing3DGroundPatches.cs`; `VoxelWorldPresentation.cs`; `SpreadSceneryRecipes.cs`; `SpreadSceneryWorldRecipes.cs`; `Objects.json` Signpost; `ArtSource/SpawnRing3D/build_ring.py` GroveSign; both native catalog copies and imported library; `SpawnRing3DRecipeTests`, `SpawnRing3DIntegrationTests` and scenery neighbor tests.

## Exact owner and scope safeguards

The proposed alias applies only after the existing native resolver has established its normal zone, owner and visible Render prerequisites, and only when no existing blueprint binding has succeeded. It must not replace an earlier successful recipe or named refusal.

Require the actual managed, currently cached `Overworld.2.6.0` receiving graph, with its own Grovelands map address and no conflicting POI. Do not consult a different globally active world's map or generate a zone while rendering. Require the actual current ground member: `entity.SpatialZone`, its canonical cell's parent zone and cell owner membership must agree. A same-address stale graph, detached owner or foreign carried/equipped entity cannot borrow the model. No new duplicate-ID validation framework is introduced for this presentation-only alias.

Require the exact existing notice identity and role together with its real Signpost semantics; the role string alone grants no rendering authority. Reuse the existing `SpreadSceneryRecipes.TryModel` canonical Signpost admission instead of duplicating its Render, Physics, guidance, custom-appearance and ownership checks. The native resolver already rejects absent/hidden/nonmember owners and preserves earlier named refusals. Add only the bounded receiving-field and notice-identity conditions needed by this alias. No new general validator or malformed-owner repair is planned.

Return the same owner and current cell position with the existing static scenery batching and fog behavior. No new gameplay state, saved field, ID assignment, model cache, RNG call, collider, light or synthetic text is needed. Examine and regional direction text remain live gameplay output; the borrowed board's abstract grain does not replace them.

## Test-first implementation prompt

During root's next source window, add focused failing tests for the actual generated allotment notice and the bounded alias safeguards. Have root observe the current missing-model RED before adding the mapping. Then implement the smallest exact-owner branch in `SpawnRing3DRecipes.cs`, preserving the existing `GroveSign` binding and all earlier resolver behavior. Run affected native GREEN and the existing three failing seed cases, inspect the actual notice in the native renderer, record limits and update this document before publication. No renderer framework, global scope widening, new art or unrelated coverage cleanup.

Planned changes:

- `Assets/Scripts/Presentation/Rendering/SpawnRing3DRecipes.cs`: one narrowly guarded native notice alias and, if clearer, its private read-only predicate.
- `Assets/Tests/EditMode/Presentation/Rendering/SpawnRing3DRecipeTests.cs`: generated source/model and refusal controls; retain all existing cases.
- `Assets/Tests/EditMode/Presentation/Rendering/SpawnRing3DIntegrationTests.cs`: actual imported model submission, owner/fog/removal and saved replacement checks where the existing fixture supports them.
- This document and a bounded verification directory: exact RED/GREEN results, source correction and native visual evidence. A short existing native scenario extension is conditional on root's acceptance window; no new general runner is required.

Meaningful verification:

1. Root reruns the three `CurrentNativeGroundGraphResolvesWithoutMutation` cases for `Overworld.2.6.0` (64, 1729, 729490642), records the actual source notice and failure, then requires the unchanged cases to pass after the mapping. The full 43-case fixture is a compact neighbor regression.
2. Assert the actual generated notice receives `ring-grove-sign`, the same entity reference, current projected cell, static/batched policy, and a real catalog/prefab entry. Repeated resolution must preserve entity membership, IDs, parts, physical state, source text and tile state.
3. Pair with hidden, custom appearance, carried and foreign/detached owner controls, reusing the existing canonical Signpost tests for detailed shape validation. Preserve existing normal Spread Signpost selection and existing GroveSign selection. These tests stay bounded to the new branch.
4. Native presenter evidence must show the actual imported ring model submitted for the actual visible notice, with normal fog hiding and removal relinquishing its rendering contribution. Rebinding/restoring must follow the replacement owner rather than a prior reference. A resolver return alone is not visual completion.
5. Root inspects a native screenshot at the real allotment and, if running the short gameplay check, opens its actual Examine reader. State explicit limits: scripted source/model/ownership checks can prove those properties; they do not prove unaided discovery or all-camera visual quality.

## Review and execution record

Pre-implementation review: the gap is still present by current source inspection. The recommended path reuses existing art and the native ring material/voxel pipeline. The three historical failing cases are precisely identified, but no new native RED/GREEN has been run for this plan. There are no changes to Assets from this assessment.

Root approved this bounded actual-notice fix after the dispatch yard on 2026-10-03. Next gate: root's source window and current observed RED. Production, native integration, screenshot review and final self-review remain pending.

Test-only source window, 2026-10-04: added nine focused cases in the two existing recipe/integration fixtures. They cover one actual generated source, three existing canonical-admission counterchecks, foreign owner/address refusal, unchanged neighboring recipes, real batch contribution with fog/hide/remove, and visible/hidden full-session replacement saves. The original three generated-seed failures remain intact. Root requested proportionate reuse of existing guards; the plan and tests were reduced accordingly. No production edit or Unity operation was performed by this agent; current RED observation is pending.

Current RED, 2026-10-04: root ran the two complete art fixtures, 79 cases with 68 passing and 11 expected failures (eight new missing-alias failures plus the three unchanged generated-seed pins). The combined result, also containing the separately owned Mendleaf fixture, is `Docs/Verification/AllotmentNotice/red.xml`. Root authorized the production alias only after this result and after opening an Assets source window.

Implementation: `SpawnRing3DRecipes.ResolveNative` adds one 15-line branch after ordinary blueprint binding lookup. It checks the specific notice and receiving field, calls existing `SpreadSceneryRecipes.TryModel` for canonical Signpost semantics, and selects the existing `GroveSign` binding. No imported assets, gameplay source, persistence schema, global scope or new model were changed.

Controlled render: the existing submission/fog/removal test now frames the actual generated notice with the presenter's owned camera and unchanged native camera pitch. It saves matched 960×640 visible and Render-hidden PNGs under `Docs/Verification/AllotmentNotice/ControlledRender`, asserting a nonblack frame and a changed pixel contribution when this owner is hidden. This supplemental pixel assertion was added after the original missing-alias RED, not separately observed RED. It does not add a case or introduce a generic runner. The fixture reveals the zone and centres the camera for inspection, so these images are controlled rendering evidence, not ordinary discovery or a player journey.

In-phase self-review: 🔵 bounded alias reuses the existing static batching, native palette/voxel pipeline and canonical Signpost admission; no global Signpost binding or Spread scope widening. 🧪 the pixel comparison establishes an actual visible contribution, while human inspection must still assess the board's readability. ⚪ duplicate-ID/general malformed-world validation is outside this small presentation correction; normal current-owner and semantic checks remain in force. Source diff whitespace checks pass. No Unity operation was performed by this agent.

Native GREEN: root's combined run passed 108/108 selected cases. The exact art subset in `Docs/Verification/AllotmentNotice/green.xml` is **49/49 `SpawnRing3DRecipeTests` and 30/30 `SpawnRing3DIntegrationTests`**, including all three unchanged seed pins and all nine focused additions. The other 29 cases belong to the separately owned yard/native work. This is a focused regression result, not a claim about every repository test.

Visual inspection: root and the mapping agent independently viewed `ControlledRender/notice-visible.png` and `ControlledRender/notice-hidden-control.png`. The actual notice contributes a rectangular brown wooden board near the frame centre; it disappears in the matched hidden control. Nearby foliage obscures much of the lower stem, and the voxel output is coarse. This establishes that the existing imported art reaches the actual generated location and remains distinguishable in a centred comparison. It does **not** establish unaided notice recognition at ordinary play zoom, readable lettering (the asset has none), a native Examine interaction, or acceptance from every camera position. No new art polish or vegetation relocation is claimed.

### Final Q1–Q4 cold review

- **Q1, symmetry:** the new branch sits beside existing native visual aliases and only runs after a missing ordinary binding. It returns through the same model/position/static-batch path as `GroveSign`; hiding, restoring and removing the source use the existing inverse invalidation path, covered by actual mesh and pixel checks.
- **Q2, consistency:** model identity remains `ring-grove-sign`, while recipe ownership remains the original `Signpost`. The receiving graph comes from `WorldLocationContext.For(zone)` and its current cache. Existing Spread signposts and GroveSign recipes retain their prior models. No new saved state, model registry or gameplay role is introduced.
- **Q3, counter-checks:** generated positive coverage is paired with hidden, custom appearance, carried, detached/foreign owner and unrelated-zone refusals. Actual native submission is paired with fog, hide/remove, restore and full-session replacement-owner cases. The matched hidden pixel control passed. Detailed malformed Signpost shapes reuse the pre-existing canonical admission rather than a duplicate validator.
- **Q4, documentation:** corrected the earlier audit's vague prior-settlement premise to this actual allotment source. Historical baseline failures remain historical. The nine added cases, 79-case current result and controlled-render limits are distinguished. No Qud parity, new art, ordinary journey or global scope expansion is claimed.

No 🟡/🔴 finding remains in the bounded change. 🧪/⚪ deferred bounds are unaided/all-camera readability and general duplicate-ID validation, as above. Root performs the commit; the agent did not operate Unity or commit.
