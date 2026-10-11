# System depth: second ten-iteration pass

Status: all ten iterations implemented, reviewed and verified in native Unity
and actual Play. Requested Steam Deck package pending.
Baseline `428cb4be9` on main.
This is a new slate, not a recount of the ten iterations in
`SYSTEM-DEPTH-TEN-ITERATIONS-2026-10-10.md`.

## Goal and audit method

Finish connections between ordinary player-facing systems: recruited companions,
equipment decisions, material improvisation and combat timing. Prefer completing
an existing gameplay loop to adding another large framework. Every iteration
must have an observed failing test before production changes, counterchecks,
review and a scoped commit containing its living documentation. Finish with
independent review, native Unity tests and a controlled actual Play session.

The audit read current source paths alongside the prior living plan, FOLLOWERS,
ALPHA-READINESS, REGIONAL-GUIDANCE, the item utility audit/catalog and material,
equipment and combat documents. Historical statuses are leads, not proof of an
unfinished feature. This is a prioritized system audit, not a claim to have
re-read every Markdown file or exhaustively tested every system.

## Verification sweep and corrections

| Premise | Current evidence | Decision |
|---|---|---|
| Companions need gifts and healing invented | `CompanionCareActions` already shares remedies and meals. `CivilianEquipmentGiftPart` already equips spare gear, but its `Willing` gate excludes recruits. | Reuse existing inventory/equipment rules; do not duplicate care. |
| Companions need stay/follow and terrain routing | Prior pass shipped these, despite old FOLLOWERS phase tables. | Exclude from new iteration count. |
| All worn AV is globally protective | `EquipmentComparisonService.AddContributions` totals AV; `CombatSystem.GetPartAV` uses the struck physical slot. Back has zero target weight; Handwear has weight 5. | Correct comparison wording/coverage, preserve combat balance and valid hand protection. |
| A cudgel enemy can already shove | `CombatTacticsPart` lacks Slam; ordinary `SpreadDitchMate` carries a Cudgel but has no skills. | Connect this one existing kit to audited Slam behavior. |
| Direct player spell hits rally followers | `CompanionCombat` handles committed basic melee and party-as-victim damage, not direct spell hits. | Extend at direct spell/strike boundaries, not anonymous or delayed world damage. |
| Pith can wipe off body coatings | `MaterialFieldActions` only wicks ground coatings; `LiquidCoveredEffect` already models body amount and stat consequences. | Add finite, paid body wicking; reuse effect lifecycle. |
| Gold pickup is consistent | Ground `PickupCommand` credits 5 drams per coin; container take puts coins in the pack, explicitly pinned by an old adversarial test. | Deliberate policy unification: ordinary container acquisition follows ground purse semantics atomically; update that exact historical pin. |
| Self-use consumables always cost time | InventoryUI has an explicit FieldMeal Eat turn path; generic food/tonics rebuild the open inventory. Historical Spread expedition docs explicitly call eating free. | Deliberate timing-policy change, not a regression claim: make successful self-consumption pay one turn, with exact success contracts. |
| Schematics can be studied in normal play | Study is advertised without BitLocker; execution rejects it. User explicitly keeps BitLocker dev-only. | Explain unavailability and suppress an unusable action; preserve sale/ownership and authorized developer behavior. |
| Shrines lack all functionality | `SanctuaryPart` already accepts a paid blessing; adjacent passive healing remains a historical polish wish. Campfire/inn recovery is already implemented. | Defer new shrine healing; current priorities have stronger missing connections. |

## Ten implementation iterations

| # | Behavior and player payoff | Boundaries / acceptance |
|---|---|---|
| 1 | Inspect a current companion's pack and explicitly give/retrieve carried supplies. | Exact living adjacent recruit, visible normal menu, capacity/ownership/stack rules, no stealing equipped or bound gear, stale selection and rollback. |
| 2 | Deliberately equip and replace companion gear, leaving displaced gear in their pack. | Existing EquipPlanner/EquipCommand, real anatomy and comparison, explicit selection, no automatic best-gear guesses, save persistence. |
| 3 | Ask a blocking companion to step aside. | Safe ordinary neighboring movement, preserve Stay, no forced swap, refuse hazards/root/veto/enclosure, charge only a successful action. |
| 4 | Use PrismreedPith to wick supported sticky/flammable liquid off yourself or a willing adjacent ally. | One ordinary harvest per use, bounded amount, exact current effect, no free cure of poison/fire/wetness, current source/target identity, normal paid item action. |
| 5 | Coins taken from containers reach the purse like coins picked up from ground. | Same 5-drams unit value, no duplicate credit, capacity/overflow/callback/claims and provenance considered; failed acquisition has no credit. |
| 6 | Successful self-consumption gives the world its turn. | Enumerate supported food/tonic commands; use existing pending-turn UI bridge; unsuccessful/cancelled/inspection commands remain free; keyboard and native controller checks. |
| 7 | Equipment comparison explains where armor protects. | Physical-slot AV and displaced coverage, untargetable-slot explanation, preserve global DV/speed/resistance comparisons; no rule changes. |
| 8 | The existing ditch mate can use its cudgel to shove. | Existing ordinary encounter source, exact-target friendly-safe preview, cooldown/weapon/action cost, brace versus unbraced countercheck, truthful examine warning. |
| 9 | Schematic inspection and actions honestly describe access. | Ordinary characters cannot execute Study; no BitLocker unlock; developer study, known/missing recipe and consumption paths remain valid. |
| 10 | Companions join a player's damaging direct spell or weapon skill. | Actual positive damage to a surviving non-party target; current local witnesses, Stay/busy/peace gates; first eligible victim retains target, no free attack or distant/delayed alerts. |

Iterations 1–3 live in `SYSTEM-DEPTH-II-COMPANIONS.md`; 4–6 in
`SYSTEM-DEPTH-II-MATERIALS.md`; 7–9 in `SYSTEM-DEPTH-II-EQUIPMENT.md`.
Iteration 10 and final integration are recorded here. Parallel work uses separate
test runners and disjoint source ownership; only the coordinator uses Unity.

## Design constraints

- CoO extensions using existing systems; no claim of exact Qud parity. User's
  dev-only tinkering decision remains in force. No copied Qud enemies or assets.
- No broad world generation, liquid-mixture save rewrite, new class system,
  remote companion simulation or speculative optimization.
- Preserve preexisting workspace changes and saves. Edit Objects.json
  surgically and prove the parsed object delta; add unique .meta files.
- Read-only menu construction may enumerate local inventory. Do not add
  per-frame zone scans or redraw loops; retain existing event/dirty boundaries.
- Tests with runner stubs establish rule behavior only. Native UI/input tests,
  actual Play evidence, and hardware/build limits must be reported separately.

## Iteration 10: direct combat assistance

Implemented; native tests, independent review and actual Play pass. The existing
local party helper owns eligibility and target selection. `PerformSingleAttack`
and `SkillCombatHelpers.DealGuaranteedHitDamage` signal actual HP loss. Direct
spells collect unique damaged creatures only inside the existing active `Cast`
scope and signal on successful commit. A failed/throwing resolver cannot issue
an offensive order; ordinary `ApplyDamage` and an out-of-cast spell helper do not
issue one. First eligible witnessed survivor in resolver order wins per recruit;
an existing fight is retained. No free attack or movement occurs.

Observed RED: 9/9 cases fail at missing assistance, including actual Ember Spit
command/cooldown and positive Lunge damage. Initial Lunge fixture used unsupported
constant dice `1`; correcting it to `1d1` was necessary to reach the real missing
assistance assertion. A constant-maximum RNG was also discarded because the
penetration algorithm can keep rolling; seeded ordinary randomness is used.
GREEN: 78/78 core cases (31 new direct-combat cases plus 47 existing party cases),
zero failures in the isolated runner. Raw XML is under
`Verification/SystemDepthII/iteration10-{red,green}.xml`.

Self-review: 🟡 included the separate guaranteed-hit weapon path after source
inspection, so Slam/GroundPound are not left behind. 🔵 direct spell recipients
are revalidated at commit, including death, removal, dismissal and Stay changes;
repeated hits retain one goal. ⚪ no exact Qud parity or physical Deck claim.
Initial native Unity run `866b889a3949440f9283936f0cb810b8` passed all 31 new
direct-combat cases (the combined run also intentionally captured six companion
UI RED assertions). Per-cast allocation is lazy and only for a player with a nonempty party; no
per-frame work or zone scan was added. Save tests use the existing goal graph.
Changed files: CompanionCombat, CombatSystem, SkillCombatHelpers,
SpellDamageHelpers and two new test fixtures with unique .meta files.

Independent review checked source attribution, cast completion, surviving/current
targets, Stay/busy gates and guaranteed-hit coverage. No significant actionable
finding. The 22 adversarial cases exercise failed and throwing resolvers, delayed
damage, death/removal/dismissal during resolution, party/non-creature targets,
repeat hits, immunity, busy followers and save restoration; green counterchecks
are evidence for those hypotheses, not proof against every possible interaction.

## Remaining larger gaps

Full heterogeneous liquid mixtures, broader companion command schedules and
remote pursuit, a normal-play schematic economy, and passive sanctuary healing
remain deliberately outside this slate. Their eventual priority should be
judged against visible gameplay benefit rather than age of the TODO.

## Final verification

**Final native Unity EditMode: 1,606/1,606 passed, zero failures or skips** in
86 selected fixtures, job `e80e1a3999bb4459883d4c3902c5c0fe`. The selected list is
`Verification/SystemDepthII/native-final-fixtures.json`, with raw results in
`native-final-integration.xml`. This includes the previous slate's 45 fixtures,
new contracts and affected inventory/equipment/AI/movement/UI regressions. It is
a selected integration suite, not the entire project suite. Fresh compilation
had no errors. No production edits followed the final suite.

The earlier combined gate passed 1,584/1,584 (`native-first-integration.xml`).
Review then fixed step-aside rollback admission; its native recheck passed84/84
(`native-step-review.xml`). The tool monitor reported an initialization timeout
for that short job, but Unity's fresh, complete XML independently records all84
passes and the new21-case step fixture. No timeout was counted as a success on
its own. These overlapping suites are not added together.

**Actual Play: 14/14 checks pass, zero unexpected errors**, run
`78c52afbb3da4f72940713ffabe0a4fd`, eight screenshots. All eight were visually
reviewed at original resolution across two reviewers. The pack/equipped reader
is visible and readable; container coins, pith/tonic outcomes, cleared path and
Slam knockback/companion attack appear in the actual interface.

The first Play run `b3d7562477b34730ae6b9a3f28f62c4b` retained12/14 passes and
two failures. Its real Look-mode reader failure led to two observed native RED
cases and a narrow dispatch fix; the27-case reader regression then passed.
The Slam check initially stopped before a newly inserted enemy received its
first turn. The route now sends up to three ordinary waits, with exact energy,
HP/position/goal observations. The final run required one wait; no authored AI
chance, damage, health, or cooldown was altered to force the outcome.

Independent review fixed prepared equipment clone validation, stale comparison
publication, hidden-owner access, spent-coin alias/quantity checks, schematic ID
mutation, physical footprint hazards and rollback ownership. Each concrete
finding has its RED/counter evidence in the per-iteration logs. Parsed blueprint
comparison against `428cb4be9` confirms only `SpreadDitchMate` changed. New C#
metadata, whitespace checks and scoped Git changes were checked separately.

The reusable native route is **Caves Of Ooo → Scenarios → World → System Depth II
Connections Audit** (`ReferenceGladeNativeBatch.LaunchSystemDepthII`). It creates an
isolated seed64 new game and arranges an adjacent recruited villager, ordinary
factory supplies, a coin chest, an oil coating, an injury and a ditch mate.
Keyboard and synthetic Gamepad input use the actual menus; assertions measure
inventory identity, actual equipment slots, effects, purse, cooldowns, goals and
action cost. One action is scheduler energy spent, not necessarily one tick.
The route retains normal scheduler behavior and declares its seeded enemy RNG.
Its report and screenshots go under `Verification/SystemDepthII/Native/<runId>`.

For ordinary play: recruit a willing creature using the existing recruitment
skill, stand adjacent, look at it and confirm to open its actions. Inspect its
pack, give a complete carried stack, choose an equipment slot/replacement,
retrieve unequipped supplies or ask it to step aside. Comparison and inspection
are free; successful management mutations spend one action. Stay remains an
explicit limit on combat assistance. An oil/pitch/honey-coated character can use
carried Prismreed pith to wick a bounded amount; food and tonics now let the world
act after successful self-consumption. Ditch mates retain their normal Spread
encounter sources, with an examine warning about their new cudgel shove.

Native EditMode, arranged Play actions, source/content availability, and hardware
performance are separate evidence. At the user's follow-up request, a new Linux
Steam Deck package will be built after the final fixes pass, with its extracted
folder named `CavesOfOoo-current`. The package does not itself establish physical
Deck controls/performance, natural discovery rates, or exhaustive whole-project
verification.
