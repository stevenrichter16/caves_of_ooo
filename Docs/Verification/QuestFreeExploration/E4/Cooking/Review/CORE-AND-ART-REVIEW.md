# Cooking core and prepared-grain binding: bounded cold-eye review

Private review, 2026-09-28. No Unity calls, tests, shared changes or Git operations by this reviewer. Exact inspected inputs are in review-inputs.json. The renderer candidate is reviewed privately; import and current native pixels remain root-owned gates.

## Findings and resolution

No concrete significant blocker found in the scoped changed core or prepared-grain binding/importer. Current narrative originally said Emberwheat still lacked Cookable and the core was merely proposed. Root is correcting that Q4 drift in the environmental/cooking living docs, while retaining raw pre-implementation evidence. The copy-only importer error noun was corrected from field gate to prepared grain before the final nine-path manifest (d8ca11cbba20eb550674c2fa72a26cbb3f10e4812928b4a7bcb27a7348bbcca6); exact shared outputs match that manifest. Root added the dedicated cooking ledger and corrected the current parent narrative; historical raw audit remains retained.

## Q1 — symmetry and transaction order

CookingService.CurrentFood validates actual carried inventory and exact recipe/stack/physics identities before creating outputs and repeats the checks afterward. StationProof captures the selected exact owner/anchor/Campfire/Physics/Fuel/Thermal and finite flag; a different nearby owner or same-valued replacement cannot inherit the selection. Legacy and finite sources share current ownership/reach validation. Finite adds only positive finite fuel and finite temperature >= the named150 constant. No fuel debit, Burning requirement or legacy heat rule was added.

The existing InventoryTransferSnapshot/claim/rollback/AfterCommit sequence is retained. The source tests preserve independent callback changes on refusal. Existing97 neighbors cover capacity/merge/outer rollback and observer exceptions, so source validity is not confused with committing a new transaction design.

The art recipe is transient/unbatched like other takeable food. SpawnRing3DPresenter.RefreshCurrent uses visible-only current cell FOV for transient views, removes stale recipes/views on pickup or owner removal, and inherits the existing current-source and surface lifecycle. The new extension does not create a second rendering lifecycle or manipulate camera/selection.

## Q2 — data and naming consistency

One raw blueprint gains Cookable; the output is independent FoodItem ToastedEmberwheat, not raw inheritance. Quantity1:1 and weight1 are preserved, healing2d4->3d4 and commerce10->12 are explicit content changes, no raw row yield or stock addition. Old saved instances are not silently rehydrated with a recipe.

The exact model ID spread-toasted-emberwheat, resource SpreadCooking3D/Library, one-entry class SpreadCooking3DLibrary, source blueprint and namespace agree across library, prefab/metadata and five narrow hooks. It requires the existing native refusal unmodeled-native-blueprint, preserving earlier source refusals. Raw Emberwheat/CookedMeat/RoastedMushroom paths are untouched. Exact imported catalog and FBX hashes match the reviewed source; approved material is borrowed without mutation.

Importer uses its own preview scene with finally cleanup, scoped output paths and SaveAssetIfDirty. Eight original pieces are flattened with transformed positions, inverse-transpose normals and winding correction; the final inert one-material mesh/prefab validates160 triangles, palette-centred UV, finite normals/vertices and actual culling bounds. It does not rebuild any portable library or replace borrowed source assets. Failed import may leave reported owned partial output; this review makes no all-or-nothing filesystem publication claim.

## Q3 — evidence and counterchecks

Parsed native-cooking-core-green-art-red.json directly:152 total,148 pass,4 fail,0 skip. New core39 and existing97 all passed. The4 art failures are precisely missing optional library/model in persistent source, actual Cook/drop, moved ground stack and pickup/drop/full-save tests. The12 art controls passed. This is136 native core+neighbors GREEN with meaningful art RED, not152 GREEN or full activation.

Core checks pair default legacy/cold/thermalless, finite threshold/fuel/malformed numbers, changed/unchanged callbacks, moved adjacent source, replaced input parts, full saved replacement and old zero-field wire. The old wire is built in the executing runtime, not a claim of cross-runtime historical-byte compatibility. Actual66/67 material passes establish local heat behavior, not elapsed world/player actions.

Art16 uses actual CookingService via inventory action, two-unit output, actual Drop/Pickup and replacement save graphs. Hidden Render, removed/carried/foreign physics, custom visual, unknown blueprint and foreign graph/map counterchecks are meaningful. Transient=true plus existing TakeableMemoryHidesItsViewAndColliderThenRecoversWithoutMembershipMutation covers the inherited FOV route; these16 do not newly prove all occlusion/pixel configurations. Original raw/prepared model identity controls keep old art separate.

## Q4 — claim boundaries and remaining gates

The dedicated cooking living doc accurately separates integrated core from unregistered residual-coal station. Its verification table is historical pre-implementation sweep data. Current text should label that timing clearly and mark native core RED/GREEN complete; source audit statements using 'currently accepts any' remain historical, not present behavior.

This review does not accept the station, source allocation/version, AllowRest opt-out, ember/light/readout, native approach timing, ordinary discovery or second variants. Those are later separate milestones. Cooking-ready is not physically cold/hot. No code change was requested for the frozen39/core package. Root subsequently reports native art/related126 GREEN; that later run was not executed by this reviewer and its full receipt was not part of this read. Actual-camera and local keyboard cooking acceptance remain root gates. These later results do not enlarge this bounded source review.
