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


### Native representative combat follow-up plan (2026-09-27)

The [authoritative source sweep and bounded native plan](Verification/DensityCompletion/Combat/NativeAcceptance/PLAN.md) is recorded before private implementation. Root approved three ordinary starter tactical policies: original-dagger melee, starting EmberSpit ranged damage, and starting Calm control. This does not mean three progression/class builds or establish a balance ranking. Normal new games already grant all six starting spells; no skill, equipment, wealth or enemy grants are required.

The accepted glade12-check, lair14-check and keeper19-check native receipts already prove ordinary melee/death/drop, real starting controls, earned equipment/XP and replacement-graph saves. They did not enable `ai`, so they do not prove a named C2 enemy tactic fired. The remaining bounded gate is direct exact-owner tactic dispatch, one actual scheduled action, cooldown fallback, a player ranged-damage encounter, and current-goal control/persistence. Existing `turn`/`turn-verbose` Begin/End records provide actor-level timing; no new gameplay diagnostic is planned.

The actual native Spread census has few of the table-listed kits. The finite source plan therefore verifies current generated depth1 Gleaner/Tunnelguard and depth3 IceWight candidates; generic underground has no CaveSlime option. It preserves authored chance, RNG, stats, real cooldown/blocked turns, all existing actors and previous failed/death receipts. One labelled player approach is disclosed per mode; combat, spells, consumption and checkpoint use native keys. Source absence, danger or an unmet witness may stop the finite run honestly. Private evidence helpers/tests and the driver/launcher are implemented and independently reviewed. The missing-helper API compile RED was followed by46/46 private GREEN cases, including two actual detached-scheduler stream controls; three separate correlation-guard mutants each fail exactly their intended negative case. Runtime, editor and focused test sources compile against current native assembly references with0 errors. This was the prepublication state; the final bounded native outcomes below supersede the pending status. No gameplay or balance change follows from this plan.


The first native representative melee replay (`51a93b574a6f4a79b5e0b9764403d2f9`) passes8/8 with actual Gleaner Lunge, same-ability cooldown fallback, original-dagger damage and ordinary17HP finish after one original tonic. The first ranged replay (`6b75e3af2c1c492d939413061fd1d626`) preserves4 successful setup/source checks and its honest failure: the actual heat-vulnerable IceWight took directly attributed EmberSpit/fire damage, then lacked current zone membership before enemy tactic evidence (stored HP12; death was not proved). A private harness-only ordering repair now observes the enemy's actual tactic/fallback before delivering ranged damage; it changes no target, RNG, skill, HP or gameplay rule. The policy is ranged damage, not a promised first strike. Control run `66826fd41f0b49fa83ac51503a98ebeb` now passes13/13 in7.766534seconds, zero errors, with exact restoration. The later retry and final bounded reconciliation below supersede this intermediate status.


### Native ranged policy split after actual ordinary freeze (2026-09-27)

The existing60 helper cases pass in actual Unity (`8243cbbb6ae644f99c22b3cf602ac10a`). Raw retry `a2b5a8386f65495a99ffe042f8084e55` exposed an audit accounting premise: one input can produce two completed player turns, while the automatic player turn has no Brain zone for cosmetic ambience. The exact retained stream produced57PASS/3RED then60/60 privateGREEN; the helper now proves paired turn/energy/ambient receipts and separate tactic→fallback enemy turns. No gameplay scheduler or Frozen change was made. `BlockedWindow/archive.json` retains raw rows, diffs, native-reference compile and matched test evidence.

The next actual retry `f4abf62a7c2542d38f956003cb8b8413` honestly reaches the real7HP stop after19 completed player turns/10 inputs,0 casts and0 tonics. Its stream proves actual IceLance and same-ability fallback, but the player remains frozen and cannot perform the later damage cast. This is useful ordinary danger evidence, not a request to nerf freeze, grant resistance, reroll outcomes or relax the safety floor. It is retained as failed acceptance.

Root explicitly split the incompatible measurements into named bounded policies. `LaunchRanged` now requires ordinary original EmberSpit damage (six checks); it may end its positive damage witness before any enemy tactic and makes no enemy-dispatch claim. `LaunchRangedThreat` requires actual IceLance and its ordinary cooldown fallback (seven checks), then immediately ends with the real player alive above10HP even if still frozen. It does not require or claim returning fire, thawing or post-freeze survival. Melee8 and control13 acceptance remain unchanged. These are three starter tactical policies plus a separate ranged-threat observation, not four builds or one combined survivor. This was the intermediate split plan; the final outcomes below record both native replays. Image acceptance is limited to any separately recorded actual viewing. All earlier failed runs remain immutable.


### Final bounded native evidence reconciliation (2026-09-27)

The separate ranged-threat run `7006e539e07f42a0a5bdaf67384d4156` passes7/7 in5.805840seconds with0 errors and exact restoration: actual IceLance and same-ability cooldown fallback,3 scheduler-completed player turns from2 inputs, ending alive at37HP. It deliberately ends before requiring freeze recovery or returning fire. The first-strike retry `558a7d2f35e348a6884f4bfba8b0a1c6` remains failed: ordinary IceLance froze the player before the four-cell cast, and the real safety stop was reached at2HP after23 completed player turns/12 inputs,2 moves and0 casts. No reroll, immunity or balance adjustment follows.

The original failed run `6b75e3af2c1c492d939413061fd1d626` nevertheless has a valid, narrower positive ranged-use witness. Its exact retained CommandEmberSpit window records player2701→IceWight52668 Heat4 at tick30, actual original skill dispatch and0→7 cooldown, one paid player End/Begin, and40HP player finish. Burning1 and environmental Heat7 leave stored targetHP12. The earlier statement that this proved a kill was incorrect: target zone membership is absent, but no lethal/Death receipt proves death. `ranged-positive-subwitness.json` records source SHA, exact rows, native-key context and bounds. The immutable run stays `complete=false`; it is not relabeled as the corrected six-check policy passing. Its native-surface-unavailable FX receipt also precludes a3D projectile claim in that foreign underground zone.

Taken together, accepted melee8, control13, separate ranged-threat7 and the explicitly extracted ranged-damage sub-witness establish bounded ordinary starter input/tactic/fallback/control reachability. They do not establish a completed ranged-first encounter, surviving freeze and returning fire, equal difficulty across policies, three progression builds, campaign balance or frequency. Native helper72 and later frozen-window60 are distinct selections, not an additive count of unique tests. Stop chance-driven replays here; the subsequent C12.1 continuous acquired-resource journey is a separate acceptance gate.


Source trace for the original target removal: `fire_plus_ice.json` requires Burning, the Ice material tag and temperature≥0, applies5 typed Heat and then swaps the owner to WaterPuddle. IceWight has that exact tag; `BurningEffect.OnTurnStart` invokes the resolver, whose swap removes the original without an HP death event. The observed5→7 resistance-adjusted Heat followed by missing membership/HP12 is consistent with this authored melt path; existing `MaterialReactionPhaseCRETests` pin the swap and non-Ice refusal. The native report did not retain the replacement water owner, so the precise replacement remains a source-supported inference, not a directly observed death/XP or water-identity claim. No separate disappearance defect is established by this evidence.


### Root precommit cold-eye review

Q1: all native modes preserve ordinary actor/skill/stat ownership, and isolated launch/teardown restores the captured editor state; bounded failures remain failures. Q2: current scheduler Begin/End and canonical damage records supply the evidence without new combat rules. Q3:60 native helper cases include missing/foreign/duplicate records, blocked-turn accounting and current saved control; three independent guard-removal mutants fail their paired controls. Q4: actual8/13/7 successes and the failed-run ranged sub-witness support only the declared reachability claim. Root viewed active melee, Calm and frozen-threat frames; generic underground remains2D, and no ranged projectile visual-quality claim follows.

🟡 The original death inference and incompatible freeze/return-fire condition are corrected in the final interpretation above. ⚪ Global balance, complete ranged-first survival, all ten kit demonstrations and progression builds are not established. Raw evidence patch/script whitespace is preserved verbatim; gameplay and native source diffs have no whitespace errors.
