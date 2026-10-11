# System depth: second ten-iteration pass

Status: audit complete; implementation beginning. Baseline `428cb4be9` on main.
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
| Gold pickup is consistent | Ground `PickupCommand` credits 5 drams per coin; `TakeFromContainerCommand` puts coins in the pack. | Unify ordinary container acquisition with ground purse semantics atomically. |
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

Pending detailed RED/implementation/review log. Existing local party helper is
the sole owner of eligibility and target selection. The direct spell helper and
single-strike damage path are the candidate signal boundaries; inspect actual
commit semantics before choosing exact placement. Ground/status/retort damage
must not accidentally become an offensive command.

## Remaining larger gaps

Full heterogeneous liquid mixtures, broader companion command schedules and
remote pursuit, a normal-play schematic economy, and passive sanctuary healing
remain deliberately outside this slate. Their eventual priority should be
judged against visible gameplay benefit rather than age of the TODO.

## Final verification

Pending. Do not interpret the plan's acceptance criteria as completed evidence.
