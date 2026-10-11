# Iteration 2: useful trader restock

Status: implemented; standalone and native EditMode GREEN; live review pending. CoO extension, no Qud parity claim.

A player can sell three unrelated objects to a trader and suppress all future
shelf refill while those objects remain. Count positive carried units belonging
to the current stock table, including nested references; preserve sold objects,
the >300 turn gate, purse floor and one authored table roll per eligible refill.

## Verification sweep / corrections

| Premise | Evidence | Decision |
|---|---|---|
| Cap every refill at three units | MorrowfastMerchantRestockTests requires full authored refill quantities (20+ units) | Preserve one complete roll; three is the low-stock threshold, not output count. |
| Count matching inventory rows | AddObject merges stackable units into one row | Count positive units, saturating at the threshold; stacks cannot trigger repeated growth. |
| Direct entries define all goods | LootTableRegistry resolves nested TableRef before Blueprint; modes differ | Traverse references with visited-table cycle guard and exclude entries that cannot emit. |
| Old stamp guards all differences | int subtraction can wrap for a large forward gap | Compute elapsed comparison as long. |

Bound: at most one authored table roll per interval, only with fewer than three
relevant units. Repeated same-turn visits cannot add stock; retaining the new
stock closes the refill gate. No reroll to force a rare item, no junk deletion,
no arbitrary new quantity cap, no new persistent fields or cache.

## Evidence / review

`iteration02-red.xml`: 38 cases, 7 failures, 31 passes. The failures reproduced
junk blocking, matching stacks overfilling, nested-table membership, save return
refill and elapsed-turn overflow. An earlier runner selection failed compilation
because an existing fixture's helper was omitted; that is not counted as RED.
`iteration02-green.xml`: 58/58 pass, including 31 new cases (23 dedicated
adversarial), 18 existing Morrowfast quantity/persistence cases and 9 existing
TraderStock cases. Added cycle and registry-replacement probes pin correct
fresh membership without triggering a cyclic roll.

Final native job `941ad3a3628542c19f5fda2b0a9e49b4`, recorded in
`native-final-integration.xml`, passed 810/810 with no skips. It includes all 31
new restock cases (8 behavior and 23 adversarial), all 18 existing Morrowfast
quantity/persistence cases and 7 TraderStockContent cases, each passing.

🔵 Self-review: read the roller and membership traversal side by side. TableRef
wins over Blueprint; weighted mode ignores Chance; zero-weight weighted entries,
zero-chance independent entries and zero output counts are excluded. Membership
is exact and ephemeral. Unit addition saturates before overflow. Existing full
roll quantities, purse behavior, and the public return value remain unchanged;
the latter's comment now admits purse top-ups and shelf attempts both count.
⚪ Bound is authored roll size, not three output items. No hostile-table parser
or loot-generation rewrite is claimed. Full capacity can still prevent refill.
🧪 Standalone and native EditMode checks prove rules, authored stock quantities
and binary graph roundtrip, not live zone-entry input or UI feel. Those remain
parent gates.

## Files changed

- `Assets/Scripts/Gameplay/Economy/TraderRestockSystem.cs`: relevant positive-unit
  membership, nested traversal, elapsed comparison and accurate comments.
- `Assets/Tests/EditMode/Gameplay/Economy/UsefulTraderRestockTests.cs` and
  `UsefulTraderRestockAdversarialTests.cs`, with metadata: source and boundary cases.
- This log and `iteration02-red.xml` / `iteration02-green.xml`.
