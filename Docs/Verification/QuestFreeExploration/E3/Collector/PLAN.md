# F6 exact collector role — private source sweep and bounded contract

Status: private implementation, 381/381 matched standalone checks and native-reference runtime/test compilation pass; native execution/generation integration still pending. Test-first role only; no shared Assets, data, manager, composer, art or Unity changes. CoO-original behavior, no parity claim. Root owns generation integration and native runs.

## Verified corrections before production

| Premise | Actual source | Bounded decision |
|---|---|---|
| Hoarder deposits goods | AIHoarderPart.cs:64–96 pushes GoFetchGoal(returnHome); GoFetchGoal.cs:154–171 returns but never deposits. It scans all owners. | Preserve both and Magpie. New role holds one exact target/home; zero per-turn zone scans. |
| A scrap budget exists | Data/Tables/PopulationTable.cs:360–383 and PopulationBuilder.cs:91–96 produce receipt-owned Hatchet/Cudgel/LeatherBoots, plus ambient Magpie/PetDog. ContainerBuilder.cs:39–49 captures real rolled cache owners. | Use actual whole rolled tool/boots + existing exact cache unchanged; replace at most one actual ambient owner with new Tatterjay. No minted goods, unboxing, loadout, added container, or guarantee of successful placement. Composer claims distinct current receipts; role alone grants no generation authority. |
| Ordinary pickup keeps salvage carried | PickupCommand.cs:218–219 auto-equips. InventoryPart.cs:78–85 already supports capacity-checked identity-preserving carried addition. | Local private pickup command uses existing transaction/snapshot and AddRetrievedObject, avoiding merge/autoequip without changing player/general pickup. No dependency on dog-fetch goal. |
| Deposit needs a new transfer engine | PutInContainerCommand.cs:65–125 owns claim, source/destination snapshots, capacity/merge and rollback. | Reuse actual command behind exact current role/home/reach admission. No general inventory refactor. |
| Threat priority needs BoredGoal changes | BoredGoal.cs:81–116 acquires threats before AIBored dispatch; higher goals never dispatch this role. | New AIBehaviorPart handles AIBored, not a new goal or global hook. Passive current owner only; party control stays above collection. |
| New saved lookup service required | Normal Part saves retain public Entity/scalar/enum fields; existing Spread roles and current save fixtures prove replacement refs. | Saved exact Home/Target, zone and anchors, quantity, Phase, bounded attempts. No new schema, ID scan, static cache or constructor side effects. |

## API / phases

`SpreadCollectorPart.Configure(Zone zone, Entity home, Entity target)` accepts a live current passive non-player owner, exact current unlocked simple home, exact positive whole ground Hatchet/Cudgel/LeatherBoots; current ownership/quest/unique/currency/essential annotations and footprint refuse. Requires short local source/home distances. One assignment only. Source eligibility uses this closed actual loose-receipt list instead of globally retagging all matching items; generation must still prove exact receipt membership.

Saved `Home`, `Target`, `ZoneID`, `HomeX/Y`, `TargetX/Y`, `Quantity`, `Phase`, `Actions` and `Configured`. `CurrentCarriedItem` is a read-only exact current positive non-equipped owner/backlink property for scoped art. Phase is Seeking, Carrying, Deposited or Stopped. No restart/reconstruction after a removed source or home. Capacity refusal retains carried goods; attempts are finite. No extra food/stock is created, and death uses ordinary carried-item drops.

Each AIBored invocation performs at most one real movement/pickup/deposit action; Brain/TurnManager owns its payment. It never pushes a child goal and never pays extra energy. No-target/stopped waits consume the ordinary turn. Pickup uses before hooks with current reference recheck and publishes completion hooks only after the owned transfer commits. Deposit uses native command/snapshots; failed operation retains exact carried ownership. Post-commit observers can legitimately move/remove the owner; the read-only visual query refuses stale carry but still shows the same actually carried positive owner after item metadata/count changes. Duty remains pinned to original blueprint/count/admission.

## RED/controls and bounds

Paired actual scheduler+source tests: exact pickup/noauto-equip then exact deposit, legacy no-role control, source/refusal matrices, home capacity/merge, two-collector competition, before hook mutations/veto/exception, post-message failure rollback, current graph/body/backlink, save while seeking/carrying/deposited/stopped, death releases actual target once. No manufactured success report for absent generated source. Later root native must witness generated pickup/home deposit and renderer art; these are unproved by the core fixture.

Performance: direct saved refs and local pathfinding only, no RNG/search list/per-turn full-zone scan. Bounded 48 movement actions; snapshots/commands allocate only on the finite pickup/deposit, following existing transactions. Existing chosen source/home remain within twelve cells on configure. All callbacks recheck authorization without repairing foreign graphs.
