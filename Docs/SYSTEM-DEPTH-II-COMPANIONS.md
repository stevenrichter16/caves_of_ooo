# System depth II: companion field management

Status: iteration 1 core implemented and reviewed; iterations 2–3 underway. Native integration pending. Baseline `428cb4be9`.

## Goal and scope

Complete three ordinary recruited-companion loops: inspect and transfer carried supplies, choose real equipment slots and replacements, and ask a blocking ally to move safely. Use the existing nearby world-action menu and its keyboard/controller navigation. Mutations cost one player action only after the shared inventory transaction commits; pack/comparison reading stays free. Saved stay/follow orders remain unchanged. These are CoO extensions, not a claim of Qud parity.

Content readiness: 🟢 existing recruits, inventories, anatomy, gear and movement; 🟢 scrolling action menu and paged announcement reader; 🟡 native input/Play evidence pending; ⚪ no new NPCs, blueprints, copied reference code, automatic best-gear choices or remote management.

## Inspected source sweep and corrections

| Lead / risk | Verified source | Design decision |
|---|---|---|
| Gifts already exist | `SecondExplorationServices.CivilianEquipmentGiftPart` equips only free slots; `SecondExplorationActions.Willing` explicitly rejects a party leader. | Keep civilian semantics; new commands require exact recruitment effect, leader, roster, living adjacency and real owner parts. |
| Companion care / orders missing | `CompanionCareActions`, `CompanionOrders`, `RecruitedEffect.StayHere` already implement care and persistent orders. | Do not duplicate care or orders. |
| Inventory UI can just point at a follower | `InventoryUI` executes commands as its PlayerEntity; `InventoryScreenData` alone is owner-generic. | Use nearby world actions and a read-only paged announcement, never impersonate an NPC in player inventory UI. |
| Transfers can blindly reuse AddObject | `InventoryPart.AddObject` merges matching stacks; `AddRetrievedObject` retains exact source and performs a long capacity precheck. `InventoryTransferSnapshot` restores only touched ownership/count entries. | Transfer the selected complete stack intact; preserve separate resident stacks, no cloning/splitting or hidden equipment removal. Label whole-stack choices. |
| General world actions pay a turn | InputHandler's generic event path is free; `PerformInventoryActionCommand` provides Before/After callback and commit boundaries. | Add a narrow companion classifier in the existing paid dispatch. Inspect is explicitly free. |
| Comparison is global armor | Iteration 7 owner is correcting `EquipmentComparisonService` while preserving its API. | Call it with the actual follower and an actually owned item; show all compatible physical-slot previews, then explicit slot choices. |
| Friendly bump can safely swap | `MovementSystem.TrySwap` forces one landing; civilian courtesy uses voluntary TryMove. | Separate step-aside command chooses a conservative safe neighbor and respects voluntary veto/root rules. No forced swap. |

## Milestones and verification

1. **Pack**: pure offered choices plus complete pack/equipment/weight reader; exact complete-stack give/retrieve; forbid equipped, bound, rented and special items; claim participants, restore transfer on outer failure, revalidate recruitment/parts/cells/source at commit. Core RED then production, counterchecks and adverse callbacks. Native menu fixture prepared for root before UI integration.
2. **Gear**: existing EquipPlanner/EquipCommand/UnequipCommand in the same outer receipt, exact current anatomy/slot; deliberate replacement leaves displaced gear in follower pack; comparison free. Test incompatible/cursed/veto/stale/rollback and save graph. No changes to root's combat or HUD owner's comparison service.
3. **Step aside**: safe dry/empty neighbor, voluntary movement, no root/drag/footprint/hazard/enclosure; preserve stay and recruitment, one successful paid action. Test veto/refusal/rollback and unchanged leader position.

Each milestone receives its own scoped commit and log here, with RED/GREEN counts, in-phase review and explicit pending native evidence. The isolated .NET runner proves core contracts only. Root owns all Unity imports/tests/Play. Native fixture setup may arrange recruits; that is not proof of organic recruitment or Deck hardware performance.

## Iteration 1 log

Implemented ordinary nearby `CompanionPack`, `CompanionGive|...`, and `CompanionTake|...` choices through the real Brain event route. The complete pack/equipment/weight text uses the existing paged announcement surface. Transfers move the selected whole stack intact, reject bound/rented/equipped/ambiguous sources, and join the outer inventory transaction. The input branch charges a turn only for successful mutations; inspection and refusals are free. Existing stay/follow rows retain their higher menu priority.

Observed standalone RED: 16/16 failed on absent choices. GREEN: 29/29 after 13 adverse/counter cases, including capacity in both directions, exact resident stack retention, stale quantities, dead/dismissed/distant recipients, inventory/brain replacement, reentrancy, independent callback changes and outer rollback. Raw receipts: `Verification/SystemDepthII/iteration01-pack-{red,green}.xml`.

Observed native UI RED: job `866b889a3949440f9283936f0cb810b8`, raw `Verification/SystemDepthII/native-companion-ui-red.xml`: 37 selected cases = 31 independent direct-combat GREEN plus six companion UI cases failing only on missing real menu choices. The initial six include future gear/step fixtures; iteration 1 retains the three pack cases and later iterations restore their cases. Native GREEN/Play remain pending; standalone results do not validate Unity input or reader rendering.

Self-review: 🔵 uses existing scoped receipts and preserves separate stack identities; 🔵 menu reads do not invoke action-block callbacks; 🔵 complete reader avoids truncating late inventory entries. 🟡 callback identity/capacity/recruitment revalidation and participant claims are covered before commit. Independent save-agent review found no actionable ownership/rollback defect. Q1–Q4 review: give/retrieve symmetric; consistent free versus paid classifier; refusal counters paired; complete-stack retention is explicit in menu/doc (no automatic merging or partial-stack picker).

Files: new `CompanionManagementActions.cs`, `CompanionPackTests.cs`, `CompanionManagementInputTests.cs` and metas; narrow `BrainPart.cs` and `InputHandler.cs` dispatch; this log and focused receipts. No InventoryUI, save schema, blueprint or combat changes.

## Iteration 2 log

Pending.

## Iteration 3 log

Pending.
