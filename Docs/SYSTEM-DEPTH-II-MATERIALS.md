# System depth II: materials and consumption (iterations 4–6)

Status: iteration 4 implemented with standalone GREEN; iterations 5–6 planned.
Native verification pending. Baseline `428cb4be9`.
CoO extensions and policy improvements; no exact Qud parity claim. The coordinator
owns Unity verification and the master `SYSTEM-DEPTH-TEN-ITERATIONS-II.md`.

## Pre-implementation sweep

| Premise | Verified source | Correction / bounded decision |
|---|---|---|
| Pith already cleans creatures | `MaterialFieldActions` only offers Wick for tile layers; `LiquidCoveredEffect` has finite Amount and normal removal/stat reversal | Add one-pith cleanup of at most 20 units of oil, pitch or honey, on self or an adjacent willing visible party member. Preserve Wet, poison, burning and every other coating kind. |
| Supply would need a new source | `BiomeCrops.json` Prismreed is a Stump crop yielding two pith; `BiomeCropPlan.SurfaceSites` allocates ordinary patches | Keep existing harvest, seeds, brewing and ground-wicking uses; add honest inspection guidance. No source grant or blueprint migration. |
| Menu target ID alone binds a coat | Effects have no durable identity, and a replacement may have equal fields | A weak per-effect transient token binds the exact original coat without saved fields or persistent object retention. Snapshot selected amount/type, source quantity, zone and positions. |
| Removal is a harmless field assignment | `LiquidCoveredEffect.OnRemove` reverses actual saved stat modifiers and can notify arbitrary observers | Validate before committing payment; subtract/remove only after commit, like physical MaterialField actions. Pre-commit failure spends nothing; post-commit observer errors cannot refund supplies. No generic effect/mixture rewrite. |
| Coins behave identically on acquisition | Ground Pickup credits 5 drams/unit with deferred currency; container take always AddObject | Give ordinary container acquisition the same credit, preserving exact ownership, Taken callbacks, overflow refusal and recorded provenance. |
| Eating was intended to cost time already | Historical Spread expedition README explicitly calls it free; InventoryUI only charges FieldMeal Eat and newer actions | Explicitly approved new timing policy: successful `Eat`/`ApplyTonic` self-consumption costs one action. Do not call old free eating a regression. Empty/stale/veto/refusal and nonconsuming reads remain free; arbitrary handled actions are not sufficient proof of consumption. |

## Iteration 4: body wicking

Plan: new `BodyCoatingCleanupActions` through existing CombatUtilityActions;
read-only local menu eligibility, exact carried supply and target checks, bounded
cleanup and original effect lifecycle. Tests must cover actual current liquid
penalties, partial/full subtraction, saved state and a real harvested source.
Counters include unrelated liquids/status, outsiders, hidden/moved/dead target,
source/coat replacement, quantity drift, callback veto/throw/reentry, no repeated
credit and harmless menus. Dedicated adversarial fixture requires 20+ meaningful
cases. Native menu confirmation must prove one paid action and no refusal cost.

Evidence: `Verification/SystemDepthII/iteration04-red.xml` contains the first
10 failures: nine absent-action failures and one fixture typo (`Harvest` rather
than the actual `HarvestCultivatedCrop` command). The typo was corrected before
production and `iteration04-confirmed-red.xml` records all 10 failing at the
missing action, including the real crop-yield path. `iteration04-first-green.xml`
passes 106/106: 10 behavior/source/save cases, 30 dedicated adversarial cases,
and 66 pre-existing material cases. A missing PartRoundTripHelper runner selection
was corrected before that run; the compile attempt is not counted as behavioral RED.

🔵 Self-review: selection includes source count, positions, exact effect token,
liquid and amount; payment callbacks and final outer callbacks are revalidated.
Removal uses existing saved modifier reversal, after commit. Removal observer
failure retains paid cost and emits the existing transaction diagnostic. No new
persistent fields, blueprint changes, source grants or generic liquid cleanup.
🧪 Native input/payment execution and physical appearance remain unverified;
independent source review completed with no actionable finding. A four-case native
controller fixture now covers partial/full cleanup and stale/callback refusal;
its execution is pending the coordinator. The source test harvests a real authored
ripe crop in a fixture, not a claim of ordinary wilderness travel.

Changed files: new `Gameplay/Items/BodyCoatingCleanupActions.cs` and metadata;
`CombatUtilityActions.cs` dispatch; `ItemTacticalUseDescription.cs` guidance;
new `Gameplay/Items/BodyCoatingCleanupTests.cs` and
`BodyCoatingCleanupAdversarialTests.cs` with metadata; native
`Presentation/Input/BodyCoatingCleanupInputTests.cs` and metadata; this log and named receipts.

## Iteration 5: container currency

Plan: reuse deferred-credit transaction semantics at TakeFromContainerCommand;
no automatic minting on ordinary inventory movement, purchase or identity-preserving
retrieval. Preserve unrelated contents and exact stack ownership on refusal.
Test authored source, stack/singleton, boundary overflow, locks, stale/double take,
Taken/outer failure, nested actions, claim/provenance and native loot-menu result.

RED/GREEN, review and final changed-file ledger pending.

## Iteration 6: paid self-consumption

Plan: narrowly classify successful actual `Eat` and `ApplyTonic` consumption
through the existing pending-turn bridge. Keep mechanics unchanged: eating at
full health may still consume food, failed effects may still spend a tonic under
their existing rules; a committed consumed unit is the cost criterion. Use a
read-only pre/post source proof or explicit committed action receipt rather than
assuming any `e.Handled` is success. Tests must send real input, account for one
ActionThreshold, and prove cancelled, refused and duplicate input remain free.

InventoryUI's success predicate is reserved with the companion agent; no other
InventoryUI changes are planned. Historical free-eating receipts stay untouched.
RED/GREEN, review and final changed-file ledger pending.

## Verification and scope

Standalone tests run in this agent's isolated `/tmp/coo-system-depth-runner`.
They cannot prove Unity input, rendering or real-time cadence. Native tests and
Play are coordinator gates; no physical Steam Deck observation is claimed.
This doc and the exact owned tests/code/receipts accompany each scoped commit.
No edits to other agents' sources, the master plan, or unrelated workspace dirt.
