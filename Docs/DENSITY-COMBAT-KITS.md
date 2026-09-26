# Content density: enemy combat kits

Status: core kits, actual warlord gear and focused verification pass. Native focused combat tests passed in root integration. The scheduler scenario is verified privately; integrated ordinary-stat play and the broad differential remain root acceptance work.

## Goal and scope

Give ten existing hostile blueprints usable tactical abilities, with ordinary melee or movement between casts. Make social combatants alert nearby willing faction mates and vary escape thresholds. Preserve passive wildlife, GlassScorpion neutrality, plain Shambler's disease-free attack, and existing saved creatures. This is CoO-authored content using shipped powers, not a claim of Qud numerical parity.

Reference: `Docs/QUD-DENSITY-GAP-ANALYSIS.md` Phase 2 and `Docs/DENSITY-COMPLETION-PLAN.md` C2. Source checks below precede implementation.

## Verification sweep and corrections

| Planned assumption | Verified behavior / resulting decision |
|---|---|
| Any part named after a skill activates it | `SkillsPart.AddSkill` owns registration; create an opt-in `CombatTactics` part that grants a narrow supported list on `ObjectCreated`, after factory anatomy setup. |
| AI can use adjacent powers | `KillGoal` only tries the ranged helper when nonadjacent; add a tactical pass before melee. Preserve ordinary attack/movement when no valid cast is selected. |
| Pointing toward an enemy hits it | Directional skills travel eight straight rays; off-ray targets must not cause wasted casts. Preview each supported power's actual target walk. |
| Melee skills target the selected enemy | Shank chooses the first adjacent creature. Only use it when its actual target is the intended enemy; do not change established player skill targeting. |
| Natural weapons satisfy weapon skills | `FindEquippedWeaponOfClass` excludes natural defaults. Warlord needs an actual carried/equipped cleaver; gear changes belong to the loot tranche. Scavenger may carry dagger or short sword, so learns both class-compatible powers. |
| Every faction should assist | `Beasts` groups unrelated wildlife. Assistance is opt-in on both caller and receiver; social combatants only, same nonempty faction, local sight/radius, no relay cascade or party betrayal. |
| IceWight's kit will be encountered | It is absent from population tables. Add one weighted option at underground tier 2+, preserving pick-one pack bounds. |
| New Brain fields save automatically | Brain has an explicit serializer. Reuse its existing serialized `FleeThreshold`; new configuration lives on the ordinary reflection-serialized tactics part. |
| Spell cooldown is a scheduler delay | `ActivatedAbilitiesPart` ticks cooldown at `EndTurn`; successful commands register cooldown, refusals do not. One successful ability replaces one normal AI action. |

## Authored roster

| Creature | Supported skills | Ability chance | Flee below | Assist |
|---|---|---:|---:|---|
| MarlbackGleaner | Shank, Lunge (actual weapon decides) | 45% | 40% | yes |
| MarlbackTunnelguard | Shank | 45% | 35% | yes |
| DesertBandit | Lunge | 45% | 35% | yes |
| AmbushBandit | Lunge | 45% | 25% | yes |
| MarlbackWallkeeper | Lunge | 55% | 15% | yes |
| MarlbackBreacher | Berserk | 55% | 5% | yes |
| CaveSlime | Acid Spray | 60% | 0% | no |
| IceWight | Ice Lance | 60% | 0% | no |
| CharredHusk | Ember Spit | 60% | 0% | no |
| RuneCultist | Ice Lance | 60% | 40% | yes |

Assist radius is six cells. Existing spell damage, resistance, elemental aftermath, FX and cooldowns remain authoritative. Frost is compatible with the cultist's existing frost rune; no new origin/lore claims are added.

## Implementation and diagnostics contract

The selector accepts a live hostile in the same zone, a supplied seeded RNG, an owned registered supported skill, usable cooldown and actual class-compatible equipped gear. Shank's actual first adjacent target, Lunge's `LineTargeting` first impact and projectile powers' `SkillLine` first target must equal the intended enemy. Berserk requires a nearby enemy and no existing Berserk. Eligible choices receive equal weight within the authored chance gate. An attempted but refused command falls back to normal AI behavior; no second power fires in the same action.

Diagnostics under `ai`: `TacticGranted` / `TacticGrantRejected`, `TacticRejected` (reason, command, cooldown), `TacticUsed`, `AssistAlert`, `AssistRejected`. Emissions are channel-gated. Missing tactics parts do not create per-turn noise.

## Verification plan

1. RED factory-grant/action tests before production; RED content tests before blueprint changes.
2. Focused GREEN through real skills, turn events, equipment and target walks; positive/counter pairs for geometry, chance, cooldown and assistance.
3. Cold-eye Q1–Q4 and dedicated adversarial tests: parsing, idempotence, mutation boundaries, malformed state, geometry, RNG, party/neutral boundaries, persistence and diagnostics.
4. Root integrates native EditMode and a normal-stat fight (melee, caster, blocked projectile, local assist, retreat) through gameplay input. Native live checks must report ordinary HP and elapsed turns; no balance claim based on invincible demonstrations.

## Verification evidence and bounds

Receipts live in `Docs/Verification/DensityCompletion/Combat`. The standalone runner can prove core rules, blueprint wiring and save behavior, but cannot prove frame/input/FX timing, feel or sustained difficulty. A one-fight native check cannot establish balance across seeds. Existing saves preserve existing creature parts and abilities; no migration is added.

## Implementation log

- Factory/content RED: 17 cases, 12 failures and five unaffected controls. Missing ten kits, the equipped warlord axe and the IceWight depth source were individually exposed before edits.
- API RED: after correcting an unrelated test API typo, compile failed only because `CombatTacticsPart` did not exist. The receipt preserves that corrected RED.
- Initial core GREEN: all 17 behavior cases passed; content failures identified eight missing concrete Brain overrides and the still-pending visible warlord cleaver. The shared-blueprint owner corrected the eight overrides separately.
- First dedicated adversarial sweep: 58 cases. Two failures were fixture isolation defects: fresh factories reused entity IDs, so queries included historical diagnostics. Tests now use unique actor IDs without clearing the user's diagnostic buffer. No production fix was needed for these failures.
- Early focused result: 91/92 passed while the Warlord loadout was pending. That dependency is now integrated and passes. Save tests establish real token-graph part identity and cooldown preservation, then restore an actor to a zone and cast after cooldown expiration.

## In-phase self-review

- Q1, symmetry: skill ownership uses the same `SkillsPart.AddSkill` and registered-command dispatcher as player skills; cooldown ticking remains `EndTurn`. Local assistance deliberately suppresses relay calls while direct hostility retains its existing particle/target behavior.
- Q2, consistency: grants and action decisions emit `ai` diagnostics with actor/target in the standard envelope, command/reason/cooldown in payload. This channel is off by default in the current registry; tests explicitly enable and restore it. No new default-on high-frequency logging is introduced.
- Q3, counters: eight directional rays, off-ray/out-of-range, same-cell/foreign/removed/dead target controls, occupied lines/scenery/walls, chance endpoints, actual equipped class, cooldown/fallback, same-faction/passive/party/radius/occlusion/no-relay, save and old-save controls are exercised.
- Q4, documentation: the tactical grant supports six concrete skills, not all 31 spells or all melee powers. Shank can fall back to ordinary melee when an ally occupies its built-in first-adjacent target position. Both caller and receiver must opt into assistance. Existing saves do not retroactively gain parts.
- 🟡 Closed: pre-existing command collision could leave a granted skill without a registered ability. A failing counter-check demonstrated it; the grant now rejects before attaching anything, leaving the original command unchanged.
- 🟡 Closed: the new Brain alert call dereferenced a detached Brain's missing owner. RED preserved the previous tolerant API contract; the lookup is now null-safe.
- 🟡 Closed: a tactics part attached to an Item could grant NPC skills. RED proved it; the grant now requires a non-player Creature.
- 🔵 Closed: grant diagnostics used the class name in the command field, unlike execution diagnostics. A failing schema check now pins the actual command; `skillClass` is a separate field.
- 🧪 Remaining acceptance: native normal-stat combat and integrated suites, plus sustained multi-seed balance. Rule tests and a single fight do not certify difficulty or responsiveness under all encounter combinations.


### Final focused check before gear integration

250 cases: 249 pass, one known dependency failure (`WarlordActuallyWieldsAnAxeForBerserk`) until the root-owned loadout edit lands. New coverage is 98 cases: 17 factory/content, 17 core actions and 64 dedicated adversarial tests. Existing 152 cases cover combat pathfinding, goal stacks, factions, following, ambushes and population/depth generation. The dedicated sweep found and closed the three robustness gaps and one diagnostic inconsistency above. Additional adversarial cases were added after the initial 58-case sweep; the initial two failed diagnostic assertions were fixture errors, not production defects.

No new source is committed or pushed by the specialist. Parent integration owns the surgical blueprint changes, final ordinary-stat play acceptance, the unfiltered native suite and the full standalone baseline differential.

### Files owned by this milestone

- NEW `Assets/Scripts/Gameplay/AI/CombatTacticsPart.cs` and metadata.
- MOD `Assets/Scripts/Gameplay/AI/BrainPart.cs`, `Goals/BoredGoal.cs`, `Goals/KillGoal.cs`.
- MOD `Assets/Scripts/Data/Tables/PopulationTable.cs`: one IceWight depth-group option.
- NEW `Assets/Tests/EditMode/Gameplay/AI/DensityCombatContentTests.cs`, `DensityCombatTacticsTests.cs`, `DensityCombatAdversarialTests.cs` and metadata.
- Root-owned `Objects.json`: ten explicit kits and flee thresholds; separate loadout work supplies actual compatible weapons.
- This document and `Docs/Verification/DensityCompletion/Combat` evidence.

### Remaining honesty bounds

Preview examines at most eight rays of the supported power's range (shipped maximum six cells). This bounds work per eligible power; no whole-world query or path generation occurs. No timing benchmark or whole-world performance claim is made. Assistance is a once-on-acquisition local entity scan and does not broadcast recursively.

The shipped forms deliberately omit area spells, item throwing and unknown future skills until their exact affected-target policy has an AI preview. This is a bounded authoring decision, not a claim those powers are broken. The ordinary fallback still acts each turn. The old `TryUseRangedAbility` path remains for actors without this opt-in part, preserving unrelated authored/scenario behavior.

### Ordinary-stat scheduler scenario and post-gear verification

`DensityCombatSchedulerBench` uses detached arenas and real Player, IceWight,
MarlbackTunnelguard and MarlbackGleaner factory entities. It keeps authored HP maxima,
Strength, Agility, Speed, gear, ability chance and cooldown configuration. A fresh
GlassScorpion supplies the unaffected assistance control. No invincibility or
boosted player statistics are used.

Seven finite cases cover an Ice Lance cast, a blocked projectile line, a cooling
ability falling back to movement, an equipped hunter's Shank, a hunter injured
through normal damage retreating at five of 25 HP, local assistance with neutral
wildlife unchanged, and a full-health pursuit control. The fixtures explicitly
stage personal hostility, seeded AI RNG, ready energy (NPC 1100, player 1000),
one cooldown and one injury; they do not claim to prove natural encounter
acquisition. Assistance checks the alert transition, not the receiver's later turn.

The real `TurnManager.ProcessUntilPlayerTurn` dispatches each action and EndTurn.
The audit checks one NPC action, one EndTurn and exact energy accounting. Its
first run found a scenario-assumption error: Ice Lance legitimately freezes the
ordinary 40-HP player, spends one blocked player turn and advances ten clock ticks
before the next input yield. The corrected audit accounts for those ticks and
records them, while still requiring exactly one NPC action. This was not a combat
production defect. Hunter and spell damage remain stochastic; Shank acceptance
uses its actual registered cooldown, not a forced successful hit.

The synchronous scope restores the active scheduler/world, loadout factory/RNG,
message text/announcements/serials/callback/tick provider, pending ASCII and spell
FX references/order, and the cosmetic spell serial. It does not advance the live
zone or player. Four scenario tests cover seven subcases, repeatability, missing
content refusal, and preservation of preexisting FX and callbacks. The private
scope uses reflection only for debug restoration seams without public push-back.

Post-gear focused receipt: **253/253 pass** (98 new kit cases, 152 existing AI/world
cases and the initial three scenario tests). The additional isolation test then
passes with the other three scenario tests, **4/4**. Scenario correction and that
extra test were temporarily verified from `/tmp` during root's Assets freeze.
Final published-source verification passed 315/315 (254 combat/AI/scheduler,
16 independent everyday, 45 Sari). Root subsequently reports native coverage
including all four scheduler cases. Native player-visible fight feel remains a
separate gate from the scheduler's numerical assertions. Durable receipts
are `post-gear.xml.gz`, `scheduler-red.log.gz`, `scheduler-fixture-correction.xml.gz`
and `scheduler-isolation.xml.gz` in the combat verification directory.

The scenario is a numerical scheduler audit. It does not render its detached
fight, exercise keyboard targeting, establish FX timing or prove encounter
balance. Root's native launch/input checks and longer play balance remain separate.

C13 identity migration changes the four family kit names above while preserving their authored combat profiles. Earlier raw receipts retain the retired names as historical evidence. See `ORIGINAL-ENEMY-REPLACEMENT-AUDIT.md` for identity/save/art verification.
