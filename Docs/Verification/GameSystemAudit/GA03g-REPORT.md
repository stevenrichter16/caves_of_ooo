# GA03g / A10 — starting-equipment API lifecycle verification

Status: COMPLETE. Loadout Equip/Pick now uses the normal equipment command,
including generic contributions, actor hooks and enhancement activation. Refused
equipment remains carried; existing gear is not displaced. Current Resources
content contains no Loadout producer, so this is an authoring/API repair, not a
claim that sixteen existing enemies now spawn with equipment.

## Executed gates

| Gate | Result | UTC interval | Seconds |
|---|---|---|---|
| red | 4/9 PASS; 5 FAIL | 2026-09-06 11:03:57Z – 2026-09-06 11:03:57Z | 0.5428978 |
| minimum | 316/316 PASS; 0 FAIL | 2026-09-06 11:05:17Z – 2026-09-06 11:05:18Z | 0.998989 |
| adversarial-red | 38/43 PASS; 5 FAIL | 2026-09-06 11:10:34Z – 2026-09-06 11:10:36Z | 1.2875198 |
| corrected-adversarial-red | 38/43 PASS; 5 FAIL | 2026-09-06 11:12:04Z – 2026-09-06 11:12:06Z | 1.1697948 |
| after-hook-red | 39/46 PASS; 7 FAIL | 2026-09-06 11:14:51Z – 2026-09-06 11:14:52Z | 1.3151118 |
| final-red | 40/48 PASS; 8 FAIL | 2026-09-06 11:16:37Z – 2026-09-06 11:16:38Z | 1.3822002 |
| focused | 406/406 PASS; 0 FAIL | 2026-09-06 11:18:11Z – 2026-09-06 11:18:14Z | 2.6363779 |
| fixture-pin | 3/3 PASS; 0 FAIL | 2026-09-06 11:20:24Z – 2026-09-06 11:20:24Z | 0.3194674 |
| full | 9417/9417 PASS; 0 FAIL | 2026-09-06 11:28:42Z – 2026-09-06 11:30:59Z | 137.4359663 |

Every gate had0C# errors.48new tests:9initial plus39dedicated;9369→9417 is a
test increase, not a bug count. The initial9 reproduced5 failures with4controls.
Dedicated/review RED confirmed occupied/detached cached destinations, legitimate
other-free-hand replanning and late Glow activation after forced removal. Four
new result-receipt branches were also RED before being implemented.

One occupied-slot fixture initially created identical carried boots that merged,
so it did not establish the intended independent equip. The corrected LeatherBoots
fixture captures successful independent equip outside the executor's caught hook;
its RED remains meaningful. Final source review similarly strengthened detached
Feet setup outside the caught callback, then all3 affected rows passed. No failed
fixture premise is presented as a production defect.

## Implementation and verified boundaries

Loadout retains AddObject outside its equipment transaction, and explicitly keeps
no-Body actors carried. AutoEquip's existing ownership/singleton/no-displacement
rules apply. Equip now rebuilds its body plan after BeforeEquip, allowing another
valid free hand while refusing an occupied or detached sole destination. The unused
prebuilt-plan field/parameter plumbing was removed. AfterEquip may legitimately
complete forced removal: the command preserves that independent result and skips
late enhancement activation instead of rolling back an already reversed bonus.

The event/LoadoutEquipResult receipt identifies exact actor/item/blueprint and
current ownership, with equipped, auto_equip_refused, missing_body or
removed_during_equip. Carry-only produces no equip-attempt receipt. Chance/count,
Pick, recursion and save fields remain intact. Replayed ObjectCreated still grants
again; normal load does not replay it. Authored multi-unit stacks now remain carried
under ordinary AutoEquip policy, a documented difference from the raw bypass.

Actual IronshodBoots and actual DuelistCut/GlowQuartz producers prove penalties,
Agility and light activation plus normal/forced inverse. Explicit Lacquered,
signed bonuses and multi-slot payloads are labeled fixture configuration. Missing
stats/dependencies, failed independent grants, merged incoming items, occupied
hands, anatomy Props and complete saved aliases have positive controls. Proposed
extra parser/recursion/Engraved rows were not added merely to pad the dedicated
count; their existing contracts were not changed. The39 executed cases in the
adversarial class and raw XML define this wave's coverage.

Independent source review is GA03g-INDEPENDENT-REVIEW.md. Qud's AutoEquip and
BodyPart.Equip route through CommandEquipObject; Inventory emits equipped events
after storage. CoO aligns its lifecycle architecture while retaining local
no-displacement, singleton and no-Body-carry rules. It is not complete Qud parity.
The historical16-humanoid report in LOOT-OVERHAUL is explicitly corrected against
current Resources source. No new creature gear or loot economy is authored.

## Native engine evidence

Run `5ba190687fa048b289d7b2a93b616e6d`:24PASS/0FAIL/0unexpected, 2.054208seconds;
shutdown 2.1225924seconds. Runtime saving unregistered, owned root held
through teardown then removed. Final JSON and raw log retained.


Five explicit private-factory actors cover Equip, Carry, veto, no Body and initially
occupied Feet. Real item blueprint/mod producers run during ObjectCreated. Exact
unique IDs avoid private-factory/bootstrap collisions. The isolated API scenario
keeps the boot modal active, uses actual compressed saves and live bootstrap load
callbacks, and verifies normal unequip/re-equip, repeated complete round trips,
forced cleanup, saved empty equipment, exact aliases/configuration and no regrant.
The scenario has24 substantive checks; probe counters/config are explicit fixtures.

Can verify: supported factory/equipment/save API composition and live graph/clock
restoration. Cannot verify: keyboard routing, ordinary-world Loadout reach, pixels,
loot balance, subjective feel or a performance speedup. No new frame/turn listener
or allocation claim requires a75second workload. GUID2453valid/unique,0collisions.

Arbitrary observer exceptions after independent mutation and partial enhancement
rollback remain A41 debt. Later manual-displacement callback mutations, corrupt raw
aliases, old unapplied raw bonuses, quantity/parser redesign and death caller's
refused AddEntity fallback remain separately queued. No save format was changed.

## Attribution

Only LoadoutPart.cs, AutoEquipCommand.cs and EquipCommand.cs change production;
all3 were clean at this wave's baseline. Two new test classes, three scenario/
launcher classes with metadata, living docs and GA03g verification accompany them.
Existing protected spell/art modifications remain separate. No content JSON,
sprite, push, merge or user-save operation is included.
