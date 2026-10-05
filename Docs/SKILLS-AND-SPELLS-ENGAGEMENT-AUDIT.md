# Skills and spells: engagement audit

Status: source/content review completed 2026-10-04 against main `14ef3e531`. Recommendations only; no gameplay edits, new tests or Unity runs in this review. Three independent reading passes covered weapons/utility, elemental schools, and progression/Spellcraft/Rites. This is an original Caves of Ooo design assessment, not a Qud-parity claim.

Implementation follow-up: the approved reliability, targeting, reader, progression and bounded control changes are documented in [SKILLS-AND-SPELLS-IMPLEMENTATION.md](SKILLS-AND-SPELLS-IMPLEMENTATION.md). The findings below are the preserved pre-change audit, not a list of all remaining defects.

## Judgment

The game has enough ability breadth to support interesting builds. Its highest-value next work is making existing promises reliable, explaining their consequences at the point of use, and giving neglected specializations recurring decisions. Some apparent balance weaknesses are implementation defects; tuning around those defects would hide them and distort later balance.

The current JSON registry contains **13 trees and 96 powers: 68 active and 28 passive**, plus the 13 roots. All 109 rows cost one skill point. The count follows registered current classes, not old brainstorm lists or remaining mutation classes.

| Tree | Active powers | Passive powers | Current engagement assessment |
| --- | ---: | ---: | --- |
| Acrobatics | 3 | 1 | Useful movement and recovery; targeting and naming need attention. |
| Axe | 4 | 4 | Strong bodily/armor consequences; remove a dead purchase and clarify collateral damage. |
| Corrosion | 1 | 2 | Thin active repertoire; preserve attrition and acid-to-melee identity. |
| Cryomancy | 8 | 2 | Strong freeze/terrain foundation; Cold Snap lacks its promised tempo role. |
| Cudgel | 5 | 5 | Good displacement/control; passive stun accumulation needs encounter testing. |
| Galvanism | 6 | 2 | Strong wet/conduction interactions; inconsistent investment benefits and spell costs. |
| Hydromancy | 7 | 0 | Valuable setup and utility; preserve environmental use and standardize mastery application. |
| Long Blades | 1 | 2 | Too few recurring active decisions; start with Lunge cadence before adding a framework. |
| Persuasion | 2 | 0 | Companion management should be dependable and not an unnecessary second purchase. |
| Pyromancy | 12 | 3 | Plenty of shapes; fix damage/terrain contracts and distinguish overlapping jobs. |
| Rites | 11 | 0 | Valuable finite-resource finishers; improve truthful descriptions and live payoff information. |
| Short Blades | 4 | 6 | Strongest existing setup/payoff weapon package; preserve its distinct roles. |
| Spellcraft | 4 | 1 | Universal investment currently inconsistent; health tradeoff timing and Calm deserve priority. |

## 1. Repair the abilities players cannot reliably plan around

These are source-traced defects or mismatches, not newly reproduced Play-mode findings. Before implementation, write and observe failing command/scheduler-level tests; immediate direct-method unit calls are insufficient for timing, payment and target-selection contracts.

### F1 — Health-for-power buffs expire before a normal follow-up

Ley Tap drains 15% current HP and sets expiry to `TickCount + 3`. Heart Flame sacrifices 50% and sets expiry to `TickCount + 5`. A normal Speed100 actor needs ten ticks to recover the 1000 energy spent by its activation. Through ordinary paid input, the next spell therefore arrives after either window. The modifier hooks also consume pending bonuses/charges per target query, not through an explicit once-per-cast contract.

**Recommendation:** measure the window in the owner's actions, display remaining charges, and define precisely which successful casts consume a charge. Decide multi-target behavior before routing more area spells through these hooks. Preserve the risky HP tradeoff once the promised payoff actually survives to the next action. Cover normal/fast/slow actors, delayed FX, non-damaging casts, multi-target casts, expiry and save/load policy.

Sources: `Assets/Scripts/Gameplay/Skills/Spellcraft_LeyTap.cs:65–100`; `Pyromancy_HeartFlame.cs:77–120`; `Assets/Scripts/Gameplay/Turns/TurnManager.cs:9–18,194–208`; ordinary paid input at `Assets/Scripts/Presentation/Input/InputHandler.cs:3610–3677`.

### F2 — Spell investment improves only some spells

`SpellDamageHelpers.ApplySpellDamage` is the sole caller of the shared spell-damage modifier dispatcher. Among direct-damage actives in the five elemental schools, only Kindle, Ice Lance, Quench, Arc Bolt and Acid Spray use it. Sixteen others use raw combat/destruction damage, including Ember Spit, Flame Jet, Conflagration, Rime Nova, Thunderclap and Overload. Every consuming rite also bypasses this helper. The universal Spellcraft/Empower promise therefore does not hold across the player's spell list. Hydromancy's moisture bonus similarly reaches Jet Blast, Drench Lob and Undertow, but not Quench/Conjure Water.

**Recommendation:** define one direct-spell damage contract and apply it consistently, preserving elemental resistance and structural damage. Explicitly decide whether and how Rites receive universal bonuses. Keep environmental aftermath, weapon riders and retaliatory damage distinct where intended. Fix F1's per-cast ownership first; simply replacing every raw damage call would otherwise spend charges inconsistently across area targets. Reassess balance only after investment benefits are real.

Sources: `Assets/Scripts/Gameplay/Skills/SpellDamageHelpers.cs:39–69`; `ProjectileSpellSkillBase.cs:98–102`; `Pyromancy_EmberSpit.cs:92–99`; `ConsumingRiteSkillBase.cs:175`; `Hydromancy_Quench.cs:35`.

### F3 — Five selected-target abilities still choose another neighbor

Frostbind chooses the first adjacent creature; Pyroclasm searches for the first adjacent Burning owner. Tumble, Recruit and Dismiss also choose a neighboring creature independently of the supplied `TargetCell`. These are separate from the eight weapon skills corrected in the previous gallery milestone. Tumble additionally checks an `Ally` tag, while recruitment establishes a `PartyLeader` relationship; a recruited companion can receive the hostile-only confusion rider.

**Recommendation:** honor the selected physical cell, refuse unavailable targets without retargeting, and use actual relationship semantics. Preserve Pyroclasm's legitimate burning-object targets. Verify two eligible neighbors, a bystander, empty selection, movement before resolution, companions and multi-cell bodies through real commands.

Sources: `Cryomancy_Frostbind.cs:53`; `Pyromancy_Pyroclasm.cs:60`; `Acrobatics_Tumble.cs:83,126`; `Persuasion_Recruit.cs:128`; `Persuasion_Dismiss.cs:59`; existing helper `SkillCombatHelpers.cs:29`; relationship assignment `Assets/Scripts/Gameplay/AI/BrainPart.cs:178`. Skill paths in this paragraph are under `Assets/Scripts/Gameplay/Skills/`.

### F4 — Some refused actions have already changed the world

Charging Strike moves before discovering there is no creature, then returns false. Flame Jet and Backdraft apply fire to tiles before rejecting an empty creature-target list. The dispatcher and input path interpret false as a refusal with no cooldown or paid turn. A useful charge or terrain ignition can therefore happen without its intended cost.

**Recommendation:** a committed movement or meaningful terrain change must count as a real action. A refusal must leave state unchanged. Keep terrain-only casting usable; do not fix the exploit by removing environmental interaction. Test empty/dry terrain, an oil lane, blocked movement, partial travel, traps and interrupted trajectories.

Sources: `Cudgel_ChargingStrike.cs:93–118`; `Pyromancy_FlameJet.cs:75–85`; `Pyromancy_Backdraft.cs:72–82`; `SkillsPart.cs:302–319`; `Assets/Scripts/Presentation/Input/InputHandler.cs:3648–3677`.

### F5 — Avoid dead purchases and misleading tactical promises

- Decapitate has no authored prerequisite despite having no effect without Dismember. Add the already-supported dependency.
- Dismiss costs a separate point, has no Recruit dependency and has a lower Ego requirement. Make basic companion dismissal part of recruitment or a free dependent management unlock.
- Hobble's description says its victim is harder to hit; actual Hobbled reduces DV by three. It does not slow movement. Cold Snap applies this effect despite describing a tempo/slowdown role.
- Still Heart's purchase description promises unwakeable sleep; its actual sleep wakes on positive damage. Rendered Steam's tooltip says blindness/CD30; actual behavior is Confusion/CD35. Scalding Veil's tooltip says CD35 instead of CD30.
- Cleave promises an adjacent enemy, but its selection can include companions. Choose an explicit collateral policy and make the description agree.

**Recommendation:** repair dependencies and descriptions immediately. Give Cold Snap an intentional role: either a clearly described vulnerability setup or actual movement control that remains distinct from Rime Nova's freezing burst. Do not silently redefine shared Hobbled across every existing skill just to repair one spell's description.

Sources: `Assets/Resources/Content/Data/Skills/Axe.json:43–47`; `Persuasion.json:12`; `ShortBlades.json:39`; `Rites.json:63`; `Assets/Scripts/Gameplay/Skills/Axe_Decapitate.cs:24`; `Cryomancy_ColdSnap.cs:24–66`; `Assets/Scripts/Gameplay/Effects/Concrete/HobbledEffect.cs:26–42`; `AsleepByGasEffect.cs:47`; `Assets/Scripts/Gameplay/Magic/GrimoireTooltipData.cs:214–222`.

## 2. Let players understand and exploit what is already there

### Preserve the strongest combinations

- Water plus electricity already increases charge and conduction; Thunderclap also has its own wet-target damage bonus.
- Wet increases initial freeze depth; Frozen stops actions; Brittle Strike rewards a melee follow-up. Heat thawing the victim creates a meaningful competing choice.
- Oil/fire, water/charge, ice and actual pushes/pulls connect terrain to positioning and cell-entry consequences.
- Flurry produces real separate attacks; Shank rewards distinct negative effects through penetration. Dodge/Rejoinder, companion positioning/Backstab, Slam and Vault already add tactical variety.
- Rites consume real status marks and one finite ink charge. Spending Frozen or Wet changes the next available move; retain that tradeoff.

Relevant sources: `Effects/Concrete/ElectrifiedEffect.cs:41`, `FrozenEffect.cs:131`; `Skills/Cryomancy_BrittleStrike.cs:45`, `ShortBlades_Flurry.cs:97`, `ShortBlades_Shank.cs:114`, `SkillCombatHelpers.cs:334`; `Magic/ResonanceSystem.cs:176`; `Skills/ConsumingRiteSkillBase.cs:140`. These paths are under `Assets/Scripts/Gameplay/`.

### Make information available at the decision

The purchase screen reserves one line and truncates at 56 characters: **95 of 109 current descriptions exceed that limit**. Heart Flame's visible line ends after its HP cost, before the promised benefit. The ability manager's selected-row help provides binding/cooldown state, not the ability's actual mechanics. Grimoire panels do have richer static tooltips, and Look already exposes effects and ground state; the problem is uneven access and inaccurate details, not a complete absence of information.

`ResonanceSystem.Preview` already computes consumed/declined marks and payoff without mutation, but current presentation code does not call it. Its comment promising targeting previews is ahead of implementation. Normal directional activation commits when a direction is pressed.

**Recommendation:** add a readable details view before purchase and from the ability manager; show range/shape, cooldown, costs, conditions and friendly-fire policy from the actual definitions. Add an optional inspect/aim preview for the selected rite, respecting fog and current targeting rules. It should explain which known statuses would be spent and their consequences, not provide omniscient hints or mark every opportunity in the world. Any preview of damage must be read-only; never invoke the current charge-consuming damage modifiers just to render a number.

Sources: `Assets/Scripts/Presentation/UI/SkillsScreenUI.cs:50–52,331–341`; `Assets/Scripts/Presentation/Rendering/AbilityManagerStateBuilder.cs:130`; `Assets/Scripts/Gameplay/Magic/GrimoireTooltipData.cs`; `ResonanceSystem.cs:166–220`; `Assets/Scripts/Gameplay/Look/CellStatusReadout.cs:61`; `Assets/Scripts/Presentation/Input/InputHandler.cs:3547–3606`.

## 3. Sharpen identities and progression after reliability fixes

### First balance comparisons

These are hypotheses for encounters, not proven claims that one build dominates.

1. **Calm versus other control.** Calm has range6, CD20 and 50 target actions of stationary pacification, no ink or resistance check, and ordinary damage does not remove its NoFightGoal. Test whether it permits inexpensive suppression while attacking and displaces freeze, retreat or Rites. A likely healthier role is a ceasefire/escape/social window that can be broken by aggression; choose this only after checking encounter and companion consequences.
2. **Passive cudgel stun versus deliberate control.** Bludgeon has a 50% chance to add 3–4 unsaveable stun turns on a damaging hit; stun durations add, and Conk provides four guaranteed turns. Measure how often opponents can act. Prefer tuning passive accumulation before weakening the intentional Conk/Slam choices or every stun in the game.
3. **Long Blades as repeated spacing decisions.** It has only one active: Lunge, two-cell reach at CD25. First test a shorter useful cadence so sword reach becomes a recurring tactic. Add a defensive response/riposte only if that still leaves an empty decision role; another generic damage bonus will not solve it.
4. **Elemental overlap.** Flame Jet deals5 with Burning at CD35; Conflagration deals2d6 with similar initial Burning at CD15. Ember Vein pierces range7 for2d6 at CD12; Rail Spike pierces range6 for5 at CD45 with its own charge behavior. Geometry, resistance, collateral and terrain prevent simple numerical dominance claims. Give each a clear job and cost, then merge/rework entries whose job remains redundant.
5. **Corrosion and fire aftermath.** Corrosion has only Acid Spray as an active, but already has sustained Organic-target attrition and Etch's melee payoff. Develop that material/attrition identity if expansion is needed. Cinder/Charsplit first need an ordinary route to their advertised Charred-after-fire sequence: ordinary Burning expiry does not apply Charred, and automatic fuel-exhaustion Charred is not a normal creature path. Tonics can supply Charred, so do not call the status wholly unreachable.

Sources: `Skills/Spellcraft_Calm.cs:25`; `AI/Goals/NoFightGoal.cs:46`; `AI/BrainPart.cs:65`; `Skills/Cudgel_Bludgeon.cs:27`, `Cudgel_Conk.cs:25,76`; `Effects/Concrete/StunnedEffect.cs:75`; `Skills/LongBlades_Lunge.cs:46`; `Pyromancy_Conflagration.cs:20,60`; `Pyromancy_FlameJet.cs:33`; `Pyromancy_EmberVein.cs:22`; `Galvanism_RailSpike.cs:31`; `Effects/Concrete/BurningEffect.cs:57,111`; `AcidicEffect.cs:37`. Paths are under `Assets/Scripts/Gameplay/`.

### Progression should teach a specialization without preventing hybrids

Current purchases have real opportunity cost: one SP arrives per level and another root delays a power. However, all rows cost one and none authors `Requires` or `Exclusion`; parent-tree ownership does almost all gating. The six attribute thresholds are met by all current starters. A high-impact power costs the same as an introductory one.

**Recommendation:** start with genuine dependencies and a small foundation → signature → mastery sequence in the weakest trees. Preserve cross-school purchases and discovery-based shortcuts; do not introduce rigid classes or a large prerequisite web. Price or gate only choices whose power/complexity warrants it after the reliability pass. Make a found book or weapon component open a new approach, using existing geographic sources.

Availability corrections:

- Only Classic receives the universal six-spell starter kit. Named builds receive their authored packages. Do not remove six spells from every start based on an outdated premise.
- Books teach 28 distinct current skills without spending SP or requiring parent ownership. Rites need a carried inked book for their finite charge, even when learned with SP.
- Ten hotbar positions are shortcuts, not a limit on learned/usable abilities; the manager can cast unbound entries. Rites' status slots are not equipment slots.
- Rites currently resolve immediately. Hold/channel exists in design commentary, not as a working player command; it is a possible later feature, not an available tactical choice.
- The player cannot normally learn every row just by leveling. A separate progression defect clamps Experience to999999, below level42's1033915 requirement, stopping ordinary leveling at41 despite Level.Max50. Record and fix this boundary separately rather than using it to justify an inflated early-game skill budget.

Sources: `Assets/Scripts/Gameplay/Skills/SkillPurchaseEligibility.cs:36`; `Assets/Scripts/Gameplay/Stats/LevelingSystem.cs:49,69`; `Assets/Scripts/Gameplay/Stats/Stat.cs:27`; `Assets/Resources/Content/Blueprints/Objects.json:1423`; `Assets/Scripts/Gameplay/Items/GrimoirePart.cs:82`; `Assets/Scripts/Gameplay/Magic/GrimoireChargePart.cs:31`; `Assets/Scripts/Gameplay/Bootstrap/StartingBuildService.cs:41,71,118`; `Assets/Scripts/Presentation/UI/AbilityManagerUI.cs:213`.

## Recommended implementation order and acceptance

1. **Make chosen actions dependable.** F1–F4, Decapitate dependency, companion management. Observe RED through actual commands and scheduler before fixes. Verify selected targets, ordinary action costs, per-cast resource consumption and saved state; preserve empty-action refusal. Avoid unrelated architecture work.
2. **Make decisions legible.** Correct mismatched descriptions, expose full ability details and implement a bounded read-only selected-rite preview. Check the actual native interface and fog behavior. This makes existing depth accessible before adding more content.
3. **Make specializations recur in combat.** Compare Calm/cudgel control, sword spacing, Cold Snap and overlapping elemental shapes in matched encounters after modifier fixes. Use the same investment and gear against an isolated foe, mixed group, armored foe, elemental-resistant foe and terrain hazard. Include companions and survival/retreat outcomes. Record enemy opportunities to act, number of distinct useful decisions, resource use and turns spent waiting—not only damage.
4. **Add content where a role is still missing.** Favor the thin sword/corrosion experiences and natural Charred setup. Add or meaningfully revise encounters that reward those choices. Preserve some quiet wilderness and avoid one repeated compulsory combo.

Do not assign final cooldown/damage numbers from this audit alone. Do not add mana, a replacement skill framework, channeling or many new spells as prerequisites to fixing the current roster.

## Review corrections and bounds

- 🟡 Source-traced defects above are implementation candidates, not fresh test failures. The next implementation must preserve observed RED and actual native evidence.
- 🟡 Evasive Roll was checked for an apparent action-blocking contradiction. Command routing and an existing command-level test support cleansing stun; no "cannot use while stunned" defect was found. It is a stationary cleanse despite the movement-sounding name.
- 🔵 Existing status synergies, ink economy, named starter identities, eight corrected melee targets, and Look/ground readouts were verified before recommending additions. Historical documents were not treated as current availability lists.
- 🧪 No new live play, visual review, benchmark or full test sweep was run. Feel, dominant strategies, exact tuning values and novice comprehension remain playtest questions.
- ⚪ This review adds only this living audit document. It does not change skills, blueprints, saves or balance.

Q1–Q4 reading pass: compared equivalent active damage/refusal paths, selected-target helpers and resource ownership; checked raw data against descriptions; distinguished existing counterexamples from suspected gaps; corrected old assumptions about starter kits, hotbar slots, channeling and eventual acquisition. The recommendations deliberately separate source defects, design choices and balance hypotheses.
