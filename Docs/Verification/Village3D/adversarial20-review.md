# Additional Village3D adversarial gate — 8 → 20 cases

Draft only, 2026-09-09. Read ADVERSARIAL_TESTING.md, all existing eight adversarial bodies, the 40 integration cases and the relevant actual presenter/library/manifest/native APIs. No Assets/Docs edits or Unity/test execution. `git apply --check` passed. This is not a GREEN claim and does not pre-classify potential failures as production bugs.

Apply `presenter-20case-red.patch`; original eight bodies are unchanged, twelve tests and helpers append to the same class. The only new using is UnityEngine.TestTools for the one expected fail-closed diagnostic. No NonParallelizable attribute. `Village3DPresenterAdversarialTests.cs` is the complete reviewable proposed file; generator is `/tmp/codex_v3d_adversarial20.py`.

## Added cases and taxonomy

| # | Case | Surface and counter-control |
|---|---|---|
| 9 | Invalidated manifest cannot reuse earlier valid definition | Malformed input + cached state. Clone actual library; valid cache → invalid replacement → explicit cache invalidation must reject → valid replacement recovers. Does not mutate resource asset. |
| 10 | Partial owner bind failure releases only owned resources | Atomicity/rollback. Replace one actual model binding in memory with an empty prefab: validation passes far enough to allocate scene resources, AddView fails; assert no owned world/camera/RT/hooks remain, native membership and borrowed target survive, then correct resources + zone transition recovers. Restores exact original resource Models and both cache references in finally. |
| 11 | Rapid display modes across loaded graph retain one hook | Duplicate subscribe/remove, graph swap and coexistence. Four mode/zone cycles after full graph load retain one callback per hook for both presenter and legacy renderer, correct loaded-owner selection, and no old owner claim. |
| 12 | Stale pre-load actor events cannot affect loaded owner | Entity identity vs persistent ID. Attack/damage/move/death on old entity with the same ID cannot rotate/move/hide the new view; current entity attack must rotate it. |
| 13 | Foreign graph with same ZoneID cannot affect view | Zone reference vs string ID. A real loaded second zone receives synthetic stale callbacks for the current actor; ignored, while current-zone control acts. |
| 14 | Real lethal damage hides only killed native guard | Multi-actor/cross-system death. Actual CombatSystem.ApplyDamage completes native death; view must hide before Refresh, other guard remains alive/rendered/pickable. Does not assume guard blueprints are equal. |
| 15 | Uploaded texture carries remembered ambient at correct row | Native light ↔ renderer data. Actual owned 80×25 texture reads dark remembered=.03, bright remembered cap=.2, alpha states; mirrored row unseen; visible transition control. This tests uploaded data, not GPU pixels. |
| 16 | FullReveal exit rehides without exploration | Temporary override/state leakage. Hidden actor/pick → full reveal succeeds → exit hides; every native exploration/visibility bit remains false. |
| 17 | Visible edge of large owner remains pickable | Boundary FOV/culling/selection composition. Find a real well hit outside anchor first; only that ground cell visible, anchor unseen; render/pick succeeds; remembered retains static submission but refuses picking; unseen hides. |
| 18 | Invalid real-world picks cannot retain prior hit | Boundary inputs/stale outputs. Real positive hit → NaN/infinity/out-of-world inputs/unknown lookup → all sentinel outputs clear → same positive hit still works. |
| 19 | Actual loot → clear → full save/load | Ownership acquisition + removal + reconstruction. Nonempty refusal, actual TakeFromContainer for all contents, clear empty owner, full graph save/load + mode cycle; exact carried blueprint quantities survive and container never restocks/reappears. |
| 20 | Native moved stool survives cutaway/load | Native furniture validation + visual offset + room gating + persistence. Known doorway choke rejected unchanged, permitted relocation succeeds, view displacement persists while leaving/re-entering room and after full graph load. |

The native loot test deliberately supplies sufficient inventory weight capacity so the renderer/save contract is exercised; it does not claim to test capacity refusal. Existing controls/integration already cover those separate concerns. Stale callback tests deliberately use the public presentation hook endpoints as delayed-event probes; they are not claims that a normal gameplay command emits malformed events.

## Six player-flow hypotheses

1. **Load during a visual action, then toggle display rapidly:** old graph callbacks might animate the replacement actor or duplicate subscriptions. Cases 11–13 pin identity and cleanup with active callbacks as controls.
2. **One guard dies while another is still interacting:** death might clear the shared model/material or wait for a later census. Case 14 checks immediate own-view death with a live other-owner control.
3. **Step into darkness after a fully revealed showcase:** old visible data might persist, be uploaded upside down or ignore dark-zone remembered brightness. Cases 15–16 check texture data and override reversibility.
4. **Inspect the visible edge of a large well while its anchor is unseen:** anchor-only culling might hide it; permissive picking could inspect remembered geometry or keep the last good owner after a bad coordinate. Cases 17–18 cover both sides.
5. **Empty a container, clear it, save/load and switch views:** visual reconstruction could restock or resurrect it, or drop transferred quantities. Case 19 checks the complete native flow.
6. **Move a stool, leave the room, reload, and return after art reimport:** native displacement could revert to manifest position, or partial art failure could leave an invisible selection world behind. Cases 9–10 and 20 pin those separate failure boundaries.

## Existing eight: classify separately from new findings

These are already reviewed/fixed or preservation controls. Do not count them again as bugs found by the additional twelve, and do not call eight tests eight distinct bugs.

| Existing test | Classification |
|---|---|
| LegacyRendererInitializedLaterDoesNotEraseVillageSubscriptions | Prior cold-eye source finding: assignment erased neighbors. Root changed subscription coexistence. |
| LegacyRendererDestroyedFirstRemovesItselfFromMulticastHooks | Prior cold-eye source finding: equality-to-entire-delegate teardown retained destroyed subscriber. Root changed unsubscribe symmetry. |
| PresenterDestructionRemovesOnlyItsOwnFiveHookSubscriptions | Preservation counter-control for the presenter side; not itself a separately discovered production bug. |
| HidingPresentationDiscardsQueuedActionBeforeShowingAgain | Prior interruption finding: queued action survived hiding. |
| ActualForcedMovementSnapsAndDiscardsQueuedAction | Related prior interruption finding on native forced movement; shared fix class with hiding. |
| StationaryCombatAndCastingFaceTheNativeResolvedDirection | Prior facing finding: stationary action omitted native facing. |
| OwnerWaterSlotKeepsWaterShaderAfterAllOwnerPreparation | Prior material remap finding: second PrepareModel treated water clone as non-water. |
| DistinctPresentersOwnDistinctFogMaterialsAndDisposalIsLocal | Isolation/lifetime preservation control. Does not prove two active same-layer worlds are GPU-isolated. |

Separate pre-existing integration finding: EditMode explicitly-bound presenter cleanup needed ExecuteAlways for the tested lifecycle. Not a new finding from these twelve.

## Limits and triage

Run all twenty only after the relevant controls/owner-integration GREEN gate per the methodology; investigate each failure as hard bug, latent bug, ambiguous contract, wrong fixture or redundant test. None was run here. Use fresh compiler/error/XML evidence.

This is an additional bounded gate, not an exhaustive 40–60-case taxonomy sweep. It supplements the original 40 integration cases, manifest malformed-content tests, control/low-detail tests and equipment tests. Not covered here: GPU shader/light/shadow correctness, two simultaneous presenters' shared-world-layer camera isolation, animation timing/smoothness, actual native keyboard lifecycle, or frame-rate acceptance. Parent's native captures and visual comparison remain required.

Qud classification: CoO-original rendering feature. Native combat, inventory, world and save APIs are reused as preservation contracts; no new Qud rules or parity claim is introduced by this renderer gate.
