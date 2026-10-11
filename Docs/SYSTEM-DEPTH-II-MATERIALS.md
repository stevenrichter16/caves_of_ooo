# System depth II: materials and consumption (iterations 4–6)

Status: iterations 4–6 implemented; standalone GREEN. Body cleanup native
4/4 GREEN; native currency and new consumption GREEN pending. Baseline `428cb4be9`.
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
🔵 Native job `95265fe24ccb4cfbb232cc1c4d5c0819` passed all four actual-controller
cleanup cases: partial/full cleanup and stale/callback refusal. The raw mixed
receipt is `Verification/SystemDepthII/native-consumption-red-and-integration.xml`;
its other cases include expected consumption RED and unrelated companion failures.
Independent source review completed with no actionable finding.
🧪 Physical appearance and ordinary wilderness traversal remain unverified. The source test harvests a real authored
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

Executed RED: `Verification/SystemDepthII/iteration05-red.xml` ran 89 cases,
73 passed and 16 failed at absent purse credit, unspent coin source or missing
currency-capacity refusal. This includes the deliberately changed historical
`Adversarial_ContainerGoldRemainsAnInventoryTradeGood` pin, renamed to state the
new unified acquisition policy. It is a policy replacement, not a newly discovered
violation of that old test. The authored BanditCacheT2 roll produced eleven coins
and reached the real container command before failing credit. The first implementation then passed 89/89. Cold review generated five ownership
alias and five Taken-mutation hypotheses; `iteration05-ownership-red.xml` and
`iteration05-callback-red.xml` each show 5/5 failing before the added guard.
The guard now requires exact initial coin ownership and exact spent source/Part
identity through precommit. `iteration05-final-green.xml` passes 99/99 (39 new,
60 existing acquisition adversarial). The dedicated new adversarial fixture has
33 cases; ordinary/source/save fixture has six.

🔵 Self-review: long arithmetic precedes credit conversion; source quantity is
spent before Taken; currency remains unavailable until commit. Initial aliases,
readdition, replaced Parts, blueprint/quantity changes and final purse overflow
refuse. Transaction rollback restores the source/quantity it changed and keeps
independent committed work. Arbitrary callback mutation of detached Parts is not
a general rollback promise. Source inspection by the HUD agent found no further
actionable issue; final guard review by the renderer agent also found no actionable issue.
🧪 Four native loot-popup controller cases (success, relock, overflow, Taken throw)
are authored but have not run. This does not claim a new ordinary coin spawn:
BanditCacheT2 already appears in LandmarkBuilder's bandit cache template.

Changed files: `Inventory/Commands/Acquisition/TakeFromContainerCommand.cs`;
new `ContainerCurrencyTests.cs`, `ContainerCurrencyAdversarialTests.cs`,
`Presentation/Input/ContainerCurrencyInputTests.cs` and metadata;
updated explicit legacy policy pin in `GameAuditAcquisitionAdversarialTests.cs`;
this doc and iteration05 receipts.

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
Native behavioral RED ran before production in job
`95265fe24ccb4cfbb232cc1c4d5c0819`: Starapple, ToastedEmberwheat, applied HealingTonic
and drink-label HealingTonic all consumed exactly one unit before failing the
missing turn handoff. The existing FieldMeal control passed. The full mixed
receipt is retained without claiming all of it passed. Core tests then produced
executed missing-type compile RED (`iteration06-core-red.txt`) before the new
proof implementation. `iteration06-first-green.xml` passes 37/37: ten behavior
cases and 27 dedicated adversarial cases. A broader 115-case run
(`iteration06-regression-attempt.xml`) passed 111 and exposed four old schematic
policy pins conflicting with iteration 9: unsuccessful study had counted as
handled, and nonconsuming ground study had been allowed. The schematic owner
is updating those pins; this receipt is not presented as all GREEN. All 37 new
consumption cases still passed.

Implementation: capture `SelfConsumptionTurnProof` immediately before a selected
action. Only typed Food/FieldMeal Eat and Tonic ApplyTonic are supported; command
success plus exactly one missing unit from the same valid source is required.
UI uses its existing pending-turn bridge; payload mechanics and InputHandler are
unchanged. Unsupported commands, unchanged handled actions, refunds, ambiguous
ownership and changed Part identities remain free. Proof is ephemeral/read-only,
not a durable action token, and callers must not retain it across actions.

🔵 Self-review: all current StatusTonic/CureTonic/Brew payloads compose TonicPart;
its Drink flag changes the label, never the ApplyTonic command. FieldMeal remains
paid, now with actual source-consumption evidence. No broader rule that every
handled inventory action costs time was introduced.
🧪 The expanded native fixture has 19 cases: seven successful/control cases
(including keyboard Enter and a registered TakeTurn observer), twelve cancelled,
stale, empty, veto, outer failure and handled-without-consumption counters.
Execution remains pending. Independent renderer-agent and HUD-agent reviews of the source proof
and UI predicate found no actionable loophole.

Changed files: new `Gameplay/Items/SelfConsumptionTurnProof.cs` and metadata;
narrow `Presentation/UI/InventoryUI.cs` predicate; new `SelfConsumptionTurnTests.cs`,
`SelfConsumptionTurnAdversarialTests.cs`, `Presentation/Input/PaidSelfConsumptionInputTests.cs`
and metadata; this doc and iteration06/native RED receipts.

## Performance

These are on-demand menu/command operations, following `PERF-FOUNDATION.md`'s
requirement to avoid new frame/turn scans. Body menu discovery scans the current
zone only on query and uses a weak transient effect-identity table; it does not
cache world values. Cleanup dirties only relevant occupied cells. Coin checks
scan the selected container and carrier; the consumption proof captures a few
references and a count only for supported selected commands. No new Update,
per-turn work, renderer pass or persistent cache is added. No throughput gain is
claimed; native play profiling remains the coordinator's gate.

## Verification and scope

Standalone tests run in this agent's isolated `/tmp/coo-system-depth-runner`.
They cannot prove Unity input, rendering or real-time cadence. Native tests and
Play are coordinator gates; no physical Steam Deck observation is claimed.
This doc and the exact owned tests/code/receipts accompany each scoped commit.
No edits to other agents' sources, the master plan, or unrelated workspace dirt.
