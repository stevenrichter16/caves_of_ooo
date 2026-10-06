# Second fifty improvements: combat 01–15

Status: **complete — implemented, reviewed, native regression and controlled Play accepted.** Final combined evidence is `Verification/FiftyImprovementsII/affected-regression-final.xml` (2,578 passed, zero failed, one existing explicit census skipped) and `Native/f73a91c469ca4d4fac171f4321e94149/report.json` under the same verification directory. Earlier stage receipts below remain an implementation history.

## Scope and decisions

These fifteen outcomes build a connected loop: change the floor, place enemies, choose whether to retain or spend a primer, then exploit a paid positional opening. Existing precise targeting, spell modifier/charge accounting, fair committed melee, finite field medicine, repair, timed tonics, poison attribution, and all previous fifty improvements remain the baseline. No global damage, AI intelligence, action economy, or movement rebalance is intended.

Every successful active spends its normal one action and its registered cooldown. Empty casts remain free only where specifically stated. Skill availability is ordinary skill-tree purchase; the four new powers cost one point each. New enemy supplies are real finite inventory owners. New public effect/part state must survive the existing replacement-graph serializer. No new input mode is needed.

## Executed design/review prompts

1. Read current skills, spell targeting, terrain state, AI goals, medicine and authored content; identify actual outcomes absent from source rather than copy stale audit defects.
2. Compare each proposal with shipped neighboring mechanics (Slam wall impact, Rejoinder, consuming rites, floor ice melting, precise selected targets, finite medicine) and prune duplicate claims.
3. Turn each remaining outcome into an ordinary command/AI action with an explicit cost, refusal, source and counterexample. Use existing APIs and reflection in RED fixtures so missing new classes fail assertions rather than block other streams' assembly.

## Source corrections recorded before production

| Premise checked | Actual source | Design consequence |
|---|---|---|
| Cone floor coverage matches damage | FlameJet:75, Backdraft:72 and JetBlast:158 collect a centre line; `SpellTargeting.GetCreaturesInCone`:258 widens by bands. | 01 shares the existing cone geometry; no new shape rules. |
| Drying can cut a conductive puddle route | `Hydromancy_DryingBreeze`:43–52 removes entity Wet only. `ZoneTileState.RemoveCoating`:210 already exists. | 02 removes transient water coatings only. Pools/river source volumes and permanent projections remain. |
| Glacial Wall already spans the corridor | `Cryomancy_GlacialWall`:80–125 walks forward three times. | 03 explicitly changes the existing power to a three-cell crosswise barrier centred two cells ahead. |
| Floor ice has no melt rule | TileReactions/Reactions.json `melt_ice` already produces six-turn water. `LifespanPart`:25–39 only deletes expired entities. | 04 applies only to expiring summoned IceWall entities; no duplicate floor-ice reaction. |
| Acid ground contact already exists | Current tile reactions handle oil/water/ice/conductors, not acid. Acidic is an entity effect. | 05 uses a small visible ephemeral film entity and normal cell-entry events, not a new global reaction grammar or gatherable liquid. |
| Acid has no payoff | Etch already rewards melee; Verdigris Bloom is an ink-consuming radius rite. | 06 is a targeted cooldown spell which spends one Acidic mark for immediate damage, trading away its continuing damage and Etch setup. |
| Swords have a defensive stance | LongBlades has Lacerate, Expertise and Lunge; ShortBlades already owns passive Rejoinder. | 07 is a paid one-hit guard, not another free riposte. |
| Slam lacks collision damage | `Cudgel_Slam` already damages the launched target on an obstruction. | 09 adds damage to the physical obstruction only; existing victim damage/stun stays. |
| Vault lacks a distinct movement role | Vault already crosses one obstacle; Tumble swaps bodies; Disengage uses open ground. | 10 rewards successful landing with brief accuracy, without changing legal movement. |
| All ranged foes should retreat | KillGoal closes after refused/cooling tactics; Cindercaller text deliberately promises closing with a cudgel. | 11–12 are opt-in. Keep Cindercaller's existing identity. |
| AIRetriever recovers disarmed weapons | AIRetriever reacts to thrown ItemLanded events for fetch behavior. Disarm drops equipment directly. | 13 remembers only the exact weapon actually disarmed from an opted-in actor. |
| FieldMedicine already chooses cures | It requires the exact authored HealingTonic and a nonempty healing payload. | 14 adds explicit cure blueprint opt-in; no generic potion intelligence. |
| A new weighted ambient entry is merely an alternative | PopulationTable.Roll emits ambient MinCount independently. | 15 belongs to existing `DepthEncounter`, count one, weight one at tier >=2; it replaces that selected encounter, not adds another pack. |

## Individual implementation designs

### 01 — Elemental cones write the whole reachable fan

Plan: extract the current band/occlusion traversal into `SpellTargeting.GetConeCells`; have the existing creature query and Flame Jet, Backdraft and Jet Blast ground writes use that exact set. Preserve current creature damage, push order, range and cooldown. Terrain at the flank becomes a real choice: ignite flank oil, or lay a broad wet approach for lightning/cold.

Acceptance/counter: cast each actual registered command at empty ground and assert a lateral fan cell receives heat/water; beyond-range and wall-shadow cells do not. Occupants remain deduplicated by physical owner. A pillar must shadow its rays without cancelling other rays. Risk: changing diagonal cone shape or treating terrain-only casts as free; pin current shape rather than invent a second geometry.

### 02 — Drying Breeze cuts transient water routes

Plan: retain its radius-one actor cleanse and cost (one action, cooldown3); remove only local water coatings whose lifetime is finite. Do not remove oil, ice, charge, source entities, liquid volume, or permanent water projection. A player may sever a temporary conducting route while the original water elsewhere remains useful.

Acceptance/counter: exact inside/outside radius, transient/permanent water, mixed oil/water, subsequent propagation and save round-trip. Empty breeze remains paid as before. Risk: evaporating world water resources; inspect coating lifetime/source before mutation.

### 03 — Glacial Wall makes a crosswise barricade

Plan: direction selects an anchor two cells ahead; place ice at anchor and one perpendicular cell on each side. Cost remains one action/cooldown40, lifetime8, HP18. A blocked approach to the anchor refuses; occupied or solid individual wall slots are skipped, never burying a body. All slots unavailable is free. No arbitrary targeting through cover.

Acceptance/counter: all eight facings; exact three cells; side opening for occupant; opaque approach blocks; no duplicate existing wall; no-room command remains unhandled/cooldown0. Risk: footprint contact and diagonal placement; use real zone placement checks and snapshot owners.

### 04 — Expiring ice leaves finite meltwater

Plan: optional Lifespan expiry-coating fields default empty; IceWall alone authors water for4 player turns. Normal natural expiry removes the wall then writes a shallow coating at its actual current cell. Early forced removal/destruction does not masquerade as natural expiry. This offers a second, later lightning/cold setup after the barrier has served its purpose, without conjuring inventory liquid.

Acceptance/counter: countdown8 actually opens cell and leaves water; non-opted lifespan objects leave none; removed wall cannot later write; save before expiry preserves remaining lifetime and only one aftermath. Risk: generic Lifespan behavior; default must remain identical.

### 05 — Corrosion: Caustic Trail controls a short lane

Plan: new `Corrosion_CausticTrail`, cost1, cooldown16, direction/range3. Place visible non-solid non-takeable `CausticFilm` on passable ray cells until a solid blocker. Each film lasts4 material ticks and applies Acidic0.35 on creation contact and subsequent ordinary entry; it neither deals instant damage nor creates collectible liquid. Existing film is refreshed rather than duplicated. All bodies, including allies/caster returning through it, use the same contact rule. A wholly blocked ray is free.

Acceptance/counter: true movement into film applies the existing status; no effect beyond wall/range, off-lane or after expiry; current occupants are coated once per physical owner; refreshing cannot multiply owners. Risk: lifetime/source/entry identity. Use a bounded `CausticFilmPart`, existing status matrix, Lifespan and MaterialReactionResolver.Factory conventions.

### 06 — Corrosion: Caustic Draw spends a primer

Plan: new `Corrosion_CausticDraw`, cost1, prerequisite Acid Spray, cooldown20, first-impact line range4. Requires an actual Acidic first target. Deal base `4 + floor(8 * clamped corrosion)` through common Acid spell damage while the primer still exists, then remove that exact Acidic effect. This is a burst-versus-persistent-pressure choice; it does not add armor shatter or free healing. It may affect supported scenery like existing acid spells. Missing mark/blocked line is free; resistance does not refund a valid cast.

Acceptance/counter: primed/dry otherwise-identical target, front dry blocker shielding primed target, resistance, one mark only, existing Spellcraft bonus/charge, physical footprint and selected first impact. Risk: accidentally removing a newly applied replacement mark during callbacks; snapshot exact effect identity.

### 07 — Long Blades: En Garde reserves one parry

Plan: new `LongBlades_EnGarde`, cost1, cooldown12, self action with an equipped LongBlades weapon. For2 owner turns, halve the next positive incoming `Melee` damage from an adjacent current attacker, rounding remaining damage up. Consumed once. Require the sword still equipped when the blow lands. It does not stop spell/poison/environmental damage, counterattack, or stack reductions on itself. This makes an enemy windup a choice between evasion and holding ground.

Acceptance/counter: typed melee first/second hit, ranged source, spell, zero damage, no sword, expired guard and replacement save. Risk: defensive recursion and later equipment changes; use `Effect.OnBeforeTakeDamage` with one saved spent flag and no callbacks that attack.

### 08 — Long Blades: Follow Through claims a defeated foe's cell

Plan: new `LongBlades_FollowThrough`, cost1, cooldown12, selected adjacent creature, normal single sword attack. If this attack actually kills/removes the selected victim, attempt one ordinary step into its former contact cell; a survivor means hold position. A blocked/Rooted landing does not refund the already attempted attack. No damage bonus, teleport or bypass of traps. Multi-cell victims use the exact selected physical contact, never remote anchor.

Acceptance/counter: lethal/nonlethal/missed strikes, bystander selection, blocker appearing on death, rooted attacker, trap at the entered cell, cooldown on attack with refused advance. Risk: death callbacks deleting unrelated victims; advance only on selected victim's real terminal state and only through normal movement.

### 09 — Slam damages its physical collision partner

Plan: preserve Slam's existing three-step push, victim impact damage, stun and cooldown50. On first obstruction only, apply one additional rolled base-weapon Bludgeoning impact to the actual collided creature or destructible solid owner. Never continue the push through a destroyed obstacle, damage immutable map bounds, or recursively launch secondary bodies. Friendly collateral is real and disclosed.

Acceptance/counter: creature/fragile solid/immutable solid/map edge/open push, exactly one physical owner, resistance and actual structural HP. Risk: duplicating weapon on-hit procs on secondary contact; secondary uses typed damage routing only, not a second melee swing.

### 10 — Vault creates a short offensive opening

Plan: preserve all current landing rules and cooldown15. Successful actual movement gives a saved `VaultPoiseEffect` for2 owner turns; the next actual adjacent melee hit roll gains +2 accuracy and spends it, even on a miss. A narrow `CombatSystem.PerformSingleAttack` seam consumes this once, so off-hand attacks, pure readouts, ranged Lunge and spells cannot multiply it. Refresh duration rather than stack magnitude. No bonus from failed, rooted or blocked attempts; actual landing trap damage still occurs. This rewards crossing a foe/obstacle before attacking without adding another mobility mode.

Acceptance/counter: actual hit-roll receipt before/after vault, timed expiry, refresh, refused move, replacement save and ordinary walking never creates the buff. Risk: accidentally awarding poise to a dead actor after lethal entry; require live current landed actor.

### 11 — Opted ranged enemies keep a firing distance

Plan: `CombatTactics.PreferredRange` default0 preserves all legacy behavior. Opted actor tries one ordinary backstep when too close after it cannot use a spell; at its desired distance with a valid shot it waits through cooldown instead of walking into melee. Far/out-of-ray actors still approach. Initial content opt-ins: Soursprayer and new Stormbinder at3; Cindercaller remains a closer. One action, no bonus cast or cooldown refresh, Rooted/blocked retreat falls back normally.

Acceptance/counter: cooling opted caster retreats/holds, unopted actor closes, ready spell still fires, blocked backstep cannot teleport, no double attack/move, save retains option. Risk: perpetual invulnerable kiting; bounded single step, normal Speed, no hazard omniscience.

### 12 — Opted ranged enemies step around an ally's blocked shot

Plan: `CombatTactics.RepositionForShot` defaultfalse. When no current eligible ray exists, inspect eight adjacent legal placements and choose the first which gives an audited first-impact ray to the already perceived hostile target. Only a real one-cell move replaces the action; it cannot move and fire together or shoot through the ally. No hidden-target search or full-map tactical pathfinder.

Acceptance/counter: ally-blocked/off-ray setup becomes a clear line after one paid action; wall/occupied all exits fails; ally health untouched; defaultfalse leaves old approach; no firing on same step; actual next command uses normal cooldown. Risk: different preview/execution geometry; reuse the supported-skill preview rather than approximate Bresenham.

### 13 — Disarmed trained enemies can recover the same weapon

Plan: optional `WeaponRecoveryPart` on Tunnelguard/new Stormbinder. Disarm records the exact successfully dropped weapon. KillGoal may spend one action stepping toward that ground owner within2 cells, one picking it up at feet, then one equipping the exact carried owner through InventorySystem. No fabrication, scanning random better gear, free pickup+equip, or recovery across zones. If stolen/destroyed/foreign-owned, forget it. Other actor behavior remains unchanged.

Acceptance/counter: disarm→real ground owner→paid pickup→paid equip; missing opt-in; theft; blocked step; natural weapon not recovered; replacement graph retains identity; death never creates duplicate gear. Risk: save ownership and incidental fetch behavior. Do not reuse AIRetriever, which has different throw/return semantics.

### 14 — A prepared field medic spends a real cure when needed

Plan: add optional semicolon `CureBlueprints`, defaultempty, to FieldMedicine. Allow only authored Antidote/BurnSalve with actual matching CureTonic payload, genuine current ailments and carried consumable ownership. Matching cure takes priority over healing, uses the existing Tonic.ApplyTo one-unit path and replaces one fight/retreat action. Patchbearer gains one actual Antidote (beside its existing one HealingTonic); it can be looted if unused. No automatic immunity or regenerated stock.

Acceptance/counter: full-health poisoned medic cures using one real item; unpoisoned medic keeps it; burning+salve optional control; healing still uses threshold; non-opted/ground/foreign bottle refused; callback reentry bounded by existing tonic consumption; save and death retain only unused stock. Risk: mistaking any cure for every ailment; match current cure payload explicitly.

### 15 — Ordinary Stormbinder teaches soak then shock

Plan: new original `MarlbackStormbinder`, using an existing compatible marlback silhouette alias. HP20, ordinary Speed100, modest cudgel, one carried SparkRoot; no free player-granted rewards. CombatTactics owns actual Quench and ArcBolt abilities, chance100, range preference3 and sidestep. Optional `PreferElementalSetup` selects Quench while target is dry, ArcBolt only when target is Wet; absent/cooling setup falls back to positioning/melee, never a hidden free primer. A clear ray, allies and existing spell cooldowns remain authoritative. Counterplay is breaking sight, drying, closing during cooldown, or diverting the wet target.

Ordinary source: one weight1/count1 alternative in `PopulationTable.UndergroundTier`'s existing `DepthEncounter` group at tier>=2. No independent bonus pack, new world-generation framework or guaranteed encounter. Root supplies Objects and visual alias; combat owns this single table insertion after coordination.

Acceptance/counter: real factory loadout/registered powers; dry target receives Wet on one action then actual Electric hit on the next, with exact command cooldowns; blocker/ally/dry-target electric refusal; actual table roll selects the new encounter and excludes competing encounter rows; outside depth gate absent; unused real loot and replacement save. Risk: new actor difficulty; cap to singleton and existing spell magnitudes, distinguish source reachability from a guaranteed seed location.

## Verification and ownership

New native RED fixtures will be `FiftySecondCombatTerrainTests`, `FiftySecondCombatSkillTests`, `FiftySecondCombatAITests`, plus a separate `FiftySecondCombatAdversarialTests`. Reflection gates absent new parts/skills; existing gameplay commands remain the exercised boundary. The runtime witness will be `FiftySecondCombatBench` in a detached controlled state, with `ExpectedCases`, `RunId`, `Cases`, `Failures`, `Audit` and `Observations`, never live campaign grants.

Production ownership: SpellTargeting; FlameJet/Backdraft/JetBlast; DryingBreeze; GlacialWall; Lifespan; new CausticFilm part and two Corrosion skills; new two LongBlades skills/effects; CudgelSlam; Vault/poise and one CombatSystem hit-roll seam; CombatTactics/KillGoal; new WeaponRecovery part and Disarm notification; FieldMedicine; one PopulationTable row; Corrosion/LongBlades/Acrobatics/Hydromancy/Cryomancy skill descriptions; surgical EffectDescriber/GrimoireTooltipData entries coordinated with other agents. Root owns Objects.json, visual aliases and all Unity/native execution. No shared InputHandler edit is planned.

Can verify: exact commands, costs/cooldowns, physical cells/owners, status strengths/duration, finite supplies, cold table admission, saved replacement ownership, and controlled Play runtime observations. Cannot establish from unit/source inspection: subjective fun, encounter difficulty over many playthroughs, or a guaranteed ordinary generated route to a random encounter. Native screenshots and route evidence are separate proof, never inferred from a benchmark.

## Receipts

- Native RED observed before production: `Docs/Verification/FiftyImprovementsII/combat-preparation-red.xml`, combat 73 cases, 14 pass / 59 fail. Missing behavior, classes and authored options produced assertion failures rather than an assembly failure.
- First production pass implements 01–15, adds four one-point skill powers, and keeps default AI behavior opt-in. Root applied three new object blueprints and four existing-object patches separately.
- Full runtime and entire current EditMode source compilation passed. This is compilation evidence, not a native test result. Source-only compile also includes the completed `FiftySecondCombatBench` and its wrapper.
- Current test surface: the original 73 cases plus one callback-displacement counter, five discovery/readout cases, one runtime-bench wrapper, an ordinary-pickup compatibility counter and a missed Follow Through counter (82 total). These additions follow the main observed RED; they do not claim a separate observed RED.
- Initial native GREEN pending root. Play runtime and visual review remain pending.

## First implementation review

- Symmetry/ownership: En Garde requires the sword both at activation and at impact, applies only to sourced adjacent melee damage and removes its exact saved guard instance. Vault poise is consumed at the actual hit roll, after earlier attack vetoes, rather than in a preview getter. Other damage types and remote melee retain it.
- Independent cold review and self-review both identified a Follow Through callback edge: a death/damage observer could displace the attacker before the extra step. The implementation now requires the saved contact still be exactly one step away; the new counter moves the actor away during the strike and pins a paid attack with no remote snap-back.
- Lifespan aftermath writes only after successful removal of the current physical wall. The added fields default empty, preserving other ephemeral objects. New ice creation counts only successful placements.
- Field medicine records remaining units of the medicine blueprint it actually consumed, keeping Antidote evidence separate from the retained healing bottle. Existing `FindCarriedMedicine` remains a healing-only query for encounter source guards.
- Legacy wall tests were migrated to crosswise coordinates, preserving actual occupied-slot and stone-slot counterexamples. They no longer pass vacuously by examining the old forward line.
- Readouts expose guard limits, one-use accuracy, wet-ground removal, acid tradeoffs, collision collateral and normal landing hazards. Shared content and rendering are root-owned.
- Exact owned paths are in `/tmp/fifty-ii-combat-owned.txt`; curated directly affected fixture names and paths are in `/tmp/fifty-ii-combat-regression.txt`. All new metadata GUIDs were audited as 32 hexadecimal characters.

## Runtime witness bounds

`FiftySecondCombatBench.ExpectedCases = 15` contains one named check for each outcome, using detached zones, actual authored supplies/enemies/weapons, real skill commands/AI actions, one owner-scheduler payment check and a replacement-session save. It grants fixture skills and fixed actor statistics, controls positions and uses deterministic rolls. It captures and restores live factories, scheduler/world references, message state, visual hooks and queued effects. It never equips, injures or grants supplies to the caller's live player. Its wrapper also asserts the caller's position, HP and active scheduler remain unchanged. Actual encounter discovery, player learning, visual legibility and subjective challenge are separate claims.

## Initial native integration and corrections

Root captured `Docs/Verification/FiftyImprovementsII/integration-second.xml`: combat 80 cases, 71 pass / 9 fail. The controlled benchmark executed all 15 checks, with 13 pass / 2 fail. This is not claimed as a successful runtime sweep.

- **Actual integration defect:** standard `PickupCommand` immediately auto-equips a recovered weapon. This violated the promised separate pickup and equip actions. Root approved a narrow overload retaining the original single-argument constructor and default behavior; only WeaponRecovery passes `autoEquip: false`. A counter pins ordinary pickup's unchanged auto-equip behavior. The existing native failure is the RED for this correction.
- **Fixture correction:** the synthetic Follow Through sword had penetration bonus 1 while its minimum-roll die contributes -1; AV0 therefore legitimately stopped every strike. The actual authored LongSword runtime check already passed kill, trap entry, cooldown and scheduler payment. The controlled synthetic sword now has sufficient penetration for its explicit lethal-strike premise; a separate miss counter preserves the paid-no-movement alternative.
- **Clock correction:** poison applies its existing one damage at TakeTurn start before the medic chooses treatment. Cure assertions now require that actual 7-to-6 (and benchmark 20-to-19) sequence, removal of the poison, the spent original antidote, and untouched healing stock. No gameplay clock or cure magnitude changed.
- **Assertion correction:** descriptive text comparison is case-insensitive for the word cooldown. No skill cost or description was weakened.
- **Intentional content pin:** the exact natural-weapon roster grows from 52 to 53 solely for `MarlbackStormbinder|MarlbackRake`; all inherited-hand materialization and dice checks remain. The new Sodden guard declares a MeleeWeapon part but no NaturalWeapon recipe, so does not enter this roster.

The corrected runtime and full EditMode sources compile. Assets are frozen for root's corrected native pass; final Play and presentation proof remain separate and pending.

## Corrected native GREEN

Root's `Docs/Verification/FiftyImprovementsII/integration-third.xml` passed all 82 combat cases, including all 15 named runtime-benchmark checks, and all 26 natural-weapon roster regression cases. Root also reported 34/34 visual fixtures. These are native Unity EditMode results; the benchmark's EditMode wrapper is not presented as a completed Play-mode or ordinary-input journey. The benchmark checks exact spell floor cells, wall expiry, finite film contact, real primer spending, one parry, a lethal paid step onto a trap, collision damage, one accuracy roll, separate enemy reposition/cast actions, separate weapon pickup/equip, one pre-action poison tick followed by actual cure consumption, and replaced Stormbinder ownership/cooldowns.

All fifteen implementation outcomes are now GREEN at this focused level. Broader adjacent regressions and standalone native Play remain root's next verification boundary. The two staged comment corrections only remove obsolete claims about Slam leaving obstacles undamaged and failed Vault spending a cooldown; they do not change gameplay after the successful run.

Replacement recovery graph closure: root approved the two additional counters, and `Docs/Verification/FiftyImprovementsII/integration-fourth.xml` passed the complete 16-case combat adversarial class. Both ground and already-carried disarmed weapons retain the exact replacement reference/ID, finish pickup and equip in separate steps as appropriate, and never recreate the old owner. Combat therefore has 84 distinct passing cases across these focused receipts, with the original 82-case pass plus the two new save cases. No production change was needed for the save tests.

## Cross-stream cold review (read-only)

The preparation owner received a concrete mineral-infusion rollback finding: removing every enhancement not present before the command would also erase an independent enhancement added by a later callback. The owner is responsible for its focused counter and correction.

Exploration review covered current service commands, transfers, physical objects, local passage/rope routes and the cold packet installer. Findings sent to root and owner: add the local action-block gate for Stunned/Paralyzed/Asleep actors; validate newly copied grimoire singleton quantity and identity against existing owners; pin deferred aid against mutation of its accepted medicine payload between validation and commit. The locksmith two-cell predicate issue was already identified by root. This review made no exploration or preparation edits. Existing two-inventory transfer receipts and post-commit patient lifetime/range/willingness checks had no additional concrete defect identified in this bounded pass.


### Native Play acceptance

Root ran `Verification/FiftyImprovementsII/Native/f73a91c469ca4d4fac171f4321e94149/report.json`: all 15 combat observations pass, the detached bench preserves the original player/campaign snapshot, and the completed enclosing native driver has zero unexpected errors. This is controlled direct gameplay/owner-turn evidence within real Play, not keyboard combat or a claim about encounter balance. No additional production changes followed this receipt.

Final combined acceptance: all focused cases from this stream passed together with the 116-fixture affected regression; no requested fixture was unmatched. The final native Play receipt and its controlled-fixture limitations are recorded above and in the master ledger. No production changes followed final Play acceptance.
