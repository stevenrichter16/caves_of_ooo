# F11 mechanics feasibility and smallest complete loop

Status: private proposal only, 2026-09-28. No F11 production code, shared source edits, Unity actions, Git mutations, or newly created art. Root approves implementation after reviewing this plan. The authoritative current-contract receipt is `current-contract-authored-factions.xml`: 20 passed, zero failed/skipped. These are current behavior probes, not RED/GREEN evidence for an implemented hunter.

## Recommendation

Implement one finite, exact hunter–grazer relationship in selected ordinary Spread zones. The original furrowstalker stalks one original reedback grazer, loses the trail when real cover defeats sight, may catch it through native melee, and may feed only on the exact native corpse from its own witnessed kill. Player interference, existing faction enemies, recruitment, Calm, blocked travel and the ordinary corpse roll remain authoritative. No guaranteed winner or reward.

Root's economic decision is binding: keep the grazer's authored 70% corpse roll and its existing independent Beast death loot. Add no grazer harvest yield and no synthetic meat. The corpse is already takeable. The player can interrupt the hunt, recover an ordinary death drop, or carry off the real body before feeding; the initial slice does not advertise harvesting that body. Update F11's roadmap wording about a harvestable corpse to explain this deliberate reduction in scope.

The first approval should cover the behavior prototype and its native RED tests, not simultaneous broad generation/art work. If bounded flight and pursuit cannot produce a believable escape promptly, record the limitation and switch the next milestone to F4 finite-water access, as the priority audit recommends.

## Corrected source assumptions

| Assumption | Current source and private evidence | Consequence |
|---|---|---|
| A healthy grazer can use FleeGoal | `FleeGoal.Finished` is true while healthy. Existing `SpreadGrazerPart` instead directly steps away before feeding. Paired HP10/HP2 probe passes. | Extend the scoped grazer role; do not introduce health damage to force flight. |
| Two Beast actors naturally fight | `FactionManager` treats the same faction as allied; current grazer only flees Player or actual hostile within3 and actual LOS. Paired Player/personal-enemy/allied-Beast probes pass. | Explicit reciprocal pair relationship, no global faction-table edit. |
| Existing kill pursuit respects cover | `KillGoal.TakeAction` reads and paths to the live target coordinate without LOS. Two hidden positions across an opaque Tree line produce different next steps. | Never push or call generic KillGoal for the quarry. Ordinary non-quarry combat keeps existing semantics. |
| Any hedge is sight cover | `Cell.IsSolid` honors Solid tag and closed doors/archive barriers, while `BlocksMovement` also honors Physics.Solid. Authored Hedge lacks Solid; Tree has it. Both block movement, only Tree breaks the tested LOS. | Use actual opaque Tree sources, or a deliberately authored opaque subtype with real tests. A hedge silhouette is not proof. |
| Existing flight will find a side gap | `TryStepAway` tries the directly-away step; diagonal motion has cardinal fallbacks, but blocked cardinal flight does not try open lateral exits. The blocked/open pair confirms this. | Add bounded local escape selection only for this configured hunt; retain ordinary grazer behavior outside it. |
| The grazer corpse is harvestable | `ReedbackGrazer` has CorpseChance70 and no HarvestBlueprint. `ReedbackGrazerCorpse` has no HarvestablePart, inherits Takeable=true and weight10. Actual HandleDeath70% boundary probes confirm this. | Do not promise harvest. A player can actually pick up the body. |
| Corpse creation represents all death loot | `CombatSystem.HandleDeath` drops inventory and rolls `LootDropSystem` before Died. BeastT1 separately includes RawMeat35% and ReagentCommon25%. | Feeding must consume only the body, not steal/remove other drop owners or manufacture another meat roll. |
| Matching corpse text proves provenance | Native CorpsePart writes SourceID/SourceBlueprint/KillerID/KillerBlueprint, but a scan can still select a preexisting or callback-created decoy. | Use a native creation receipt plus current owner and provenance validation. |
| A new role requires a save schema rewrite | SaveSystem already serializes ordinary public scalar/enum/Entity fields and restores exact Entity aliases; Brain goals also have reflective saves. | Keep state in a small Part; add round trips, not a broad save refactor. |
| NaturalWeapon will arm a quadruped | EntityFactory applies NaturalWeapon to Hands only. CombatSystem has the existing BodyNaturalAttack + on-owner MeleeWeapon fallback for no-hand bodies. | Author a real Quadruped body attack. Do not add fictitious hands or a useless natural-weapon property. |
| The armed-actor pin is still43 | Current hand-recipe roster is48; seven authored BodyNaturalAttack actors form a separate path. | Preserve the48 hand pin and add deliberate body-attack coverage, rather than weakening or mislabeling it. |
| All interaction effects can fire after source removal | EmitInteraction and presenter require both current physical owners. | A removed corpse cannot be used as the target of a post-consumption Interact effect. |

Current factory construction auto-registers concrete Part classes; a new sealed Part in the runtime assembly should use the existing name resolution. Verify its actual blueprint construction, rather than adding a redundant registry or assuming registration.

## Smallest behavior design

### Pair admission and saved state

Proposed `SpreadPredatorPart` on the new hunter, plus a narrow paired-threat extension of existing `SpreadGrazerPart`. The hunter owns one hunt; it does not rescan the zone to choose new prey. Configuration validates both exact Part backlinks, distinct living one-cell ground actors, same current zone, no party alignment/leader/members, no pet/service/work owner or conflicting territory/collector role, and eligible actual blueprints from the two source receipts. Admission writes both reciprocal references only after every check passes. A rejected configuration leaves both owners unchanged.

Do not reuse the grazer's existing `Configured`, `ZoneID`, `Food` or `ReservedRow` fields for hunting. Those fields belong to its finite-food reservation. Add separately named hunt fields. Existing forage configuration and all unpaired grazers retain their current behavior.

Proposed hunter state is explicit public fields: configured flag; home zone and home coordinate; exact prey Entity plus saved source ID; bounded phase enum; last-seen coordinate and valid flag; total pursuit actions remaining; hidden-search actions remaining; optional exact corpse Entity and corpse ID; feeding progress; terminal reason and Fed flag. Grazer stores its exact hunter reference, hunt zone, last observed threat coordinate and short flight-memory remainder. Public Entity references save through the normal alias graph. Once terminal, clear unnecessary live actor references while retaining historical IDs; never rearm after cache return or load.

Initial hard bounds for the prototype: actor leash12 from home,24 total active hunting actions,6 hidden search actions,3 remembered grazer-flight actions and radius4 local escape search. These are testable ceilings, not final balance claims. Paused Calm/conversation/work does not spend the active-action budget. Reacquisition may reset only the hidden-search allowance, never the total24. Exhaustion ends the hunt permanently. No remote-zone generation or offscreen simulation is introduced.

### Priority and one paid action

Put the hunter's idle dispatch in the existing BoredGoal role seam, with explicit threat precedence inside it. Higher active goals already prevent BoredGoal execution: tests must retain Calm/NoFightGoal, Wait/work, current KillGoal, follow and conversation behavior. Recruitment or loss of pair eligibility cancels only this hunt; it does not clear goals, target someone else, suppress ordinary hostility or release unrelated work.

Before a hunting or feeding action, validate the hunter/current zone and exact role. If a visible ordinary hostile/personal attacker other than the quarry exists, yield to the existing BoredGoal combat path without spending a hunting action. That path may push its ordinary KillGoal once. Low health also breaks off hunting; where a current threat exists, normal health-based retreat remains authoritative. NoFight remains an override even at low health, as it is today.

For quarry actions, execute either one MovementSystem move or one native PerformMeleeAttack directly, then return. Do not push a child after taking an action. Do not call SpreadActorContext.Combat for the quarry: that helper uses KillGoal and its hidden-coordinate tracking. State/receipt bookkeeping after a strike may occur in the same turn, but approaching, striking and feeding cannot all occur in one scheduler action.

### Visibility and finite pursuit

Acquire the paired quarry only through current range plus actual AIHelpers.HasLineOfSight. Current coordinates may be read to validate ground ownership and test visibility; when visibility fails, they must never update a route, facing, last-seen position or pursuit choice. Move only toward the saved last-seen coordinate while searching. No-visible-ever means no pursuit. At the last-seen point, allow bounded waiting/search steps that do not consult hidden coordinates; after the six-action search allowance or global24 is spent, record escape/abandonment and stop.

The critical adversarial comparison runs two copies with the same visible history and different hidden prey positions. Hunter movement, facing, phase and budgets must remain identical until one copy genuinely regains LOS. Save during lost sight, reconstruct all owners, and repeat this comparison. This catches accidental omniscience more strongly than checking one blocked ray.

Ordinary faction combat is deliberately outside this guarantee. The feature is a bounded hunt, not a global AI rewrite.

### Healthy escape

The paired hunter becomes an additional eligible proximity threat to its exact grazer without changing global factions. Reuse the existing threat-first ordering. Keep other nearby Player/real-hostile threats and party alignment authoritative; do not force the grazer to ignore a more immediate danger just to preserve the vignette.

For this configured hunt only, replace the weak blocked-cardinal escape with a bounded eight-way physical search. Examine at most radius4 (81cells), using actual BlocksMovement/MovementSystem semantics, interior boundaries and the current observed or remembered threat coordinate. Prefer a reachable LOS-breaking cell that maintains separation, with deterministic short-path tie breaking. If none exists, choose a reachable step that increases separation; if all candidates are blocked, wait rather than teleport, phase through cover or force a counterattack. Execute only the chosen first step and re-evaluate next action. A brief remembered coordinate allows the animal to continue away after sight loss without reading its hidden hunter position.

Do not bake an escape destination into the actor or move its position directly. The geometry must make the native movement useful. No guarantee that every maze or closed trap is escapable is needed. A covered variant must nevertheless prove one reachable successful escape; an open variant must allow actual catch/escape outcomes according to normal scheduler/combat randomness.

Equal speeds can preserve a constant gap forever on a straight empty lane. A finite budget still guarantees termination, but a believable catch may need authored speed/damage. Proposed tuning start for a new hunter: Speed110, HP12, Strength10, Agility14, sight8, BodyNaturalAttack1d4, no poison, forced bleed, extra equipment or granted skills; grazer remains Speed100/HP10. Run a bounded deterministic distribution before freezing stats. These are proposal values, not a claim of balance or authorization to force a kill.

### Native kill and finite feeding

Use the actual combat path and its normal hit/miss/armor/damage/death behavior. Keep the grazer's CorpseChance70. No callback or test-only setting may turn that into a guaranteed corpse in native observation.

Recommended narrow receipt seam for root approval: CorpsePart exposes a read-only, nonserialized reference to the owner it successfully added during its current native Died handling. Clear it before a new death attempt; assign only after actual zone.AddEntity succeeds. The hunter captures the exact quarry CorpsePart and prior receipt immediately before its one native strike, then reads the fresh receipt synchronously after that strike. Validate the same quarry/part, actual death, same current hunter and role, exact fresh corpse owner, current physical anchor, correct blueprint, source ID and killer ID. Revalidate after any callback. Do not scan for a matching corpse and do not accept a previous receipt or a replacement Part. Null receipt, chance failure, suppression, another killer, detached/ambiguous owner or callback invalidation ends the hunt without feeding.

The first slice binds only an actual kill caused inside the hunter's observed native strike. It need not claim delayed poison credit; the proposed weapon has no poison. If later designs require feeding after delayed external damage, add a separately tested creation notification rather than silently widening matching-ID scans. The bound corpse reference and ID live on the hunter's saved role before the turn returns, so mid-search and post-kill saves do not depend on the runtime receipt surviving reload.

Feeding should provide a brief visible opportunity to interfere: two paid, adjacent, exact-owner feeding-progress actions followed by consumption on the next paid action. On each progress action, validate normal threat/party priority and the same physical corpse, increment saved progress, then emit the existing Interaction hook while both owners remain current. This is an actual committed feeding-progress action, not a fake attack. On the final action remove only that corpse; commit Fed/ConsumedCorpseID and clear the corpse reference. Emit no post-removal Interaction against a detached owner. If removal fails, do not claim Fed; if the corpse is carried off, destroyed, moved beyond allowed reach, replaced or depleted, terminate that finite allowance without pursuing a player's inventory. No healing, new meat, new corpse, extra harvest table or nearby-item consumption.

An alternative is a persistent depleted corpse with an actual saved spent flag, but that expands corpse policy/model work. Prefer whole-owner consumption for the first slice. The graphics agent can use existing Idle/Walk/Attack/Hit/Interact routing with an original low body. Distinct new Stalk/Chase routing is optional polish after the loop works.

## Generation and art boundary

Generation agent owns exact-source admission, two-source atomic replacement, current family/version fixtures and census. It must replace the complete ordinary1–2 Viper/Marlback group with one hunter and one exact Magpie roll with one grazer. A missing actor blueprint or either ambiguous/missing receipt preserves all ordinary rolls. Other Magpies, PetDog and services remain. Never select a matching nearby animal by blueprint. Record group-count/placement-RNG/economic changes explicitly.

Use exact original opaque Tree owners for cover, preserving protected structures, exits, unrelated owners and actual eight-direction connectivity. Prove a local flight lane around real cover plus a player bypass outside initial hunter vision. Do not add a no-entry death trap or place the player inside the prey pair. Two genuine variants are covered escape versus open pursuit, not two rotations. Retain literal prior generation versions and the F9 shipped version. Fixed-seed native census uses1,64,1729 with declared canonical candidate denominator and all declines.

Art agent's `f11/art/FEASIBILITY.md` supplies the distinct low quadruped, five existing clips, native strike and exact-source remains plan. Examinable/action text should expose only current observable behavior (watching, tracking, searching, feeding); it must not disclose hidden prey coordinates or invent Harvest. No quest arrow is required. Real movement, cover, posture, ordinary inspection and the native body/drop owners supply the cues.

## Ordered implementation gates after approval

1. Write native failing tests for absent hunter admission/role, exact paired flight, bounded last-seen comparison, priority and one-action accounting. Preserve positive controls for unpaired grazer and ordinary non-quarry combat. Implement only the scoped role/Bored seam/grazer extension. No content/generation rollout until escape and hidden-position invariance pass.
2. Add actual factory blueprint/body attack, native corpse receipt and finite feeding tests. Preserve70%, test no corpse, other killer, callback moves/removals, stale Part, existing matching decoy, source pickup, pickup/drop/save and duplicate callbacks. Introduce only the narrow receipt API justified by those REDs. Compare ordinary corpse tests before/after.
3. Implement approved exact-source generation in its own private ownership lane. Paired counters: one versus two hostiles; complete versus partial stock; duplicate rows; missing hunter/grazer content; exact Magpie versus decoy; open/covered/blocked lanes; admission failure leaves source graph/RNG untouched. Measure census before claiming reachability.
4. Create original rig/model/remains and use the existing correct attack/interaction signals. Verify current-owner binding, save/reload reconstruction, visible/hidden transitions and all family biome tiles without force-refreshing after paid actions.
5. Run bounded native scenes from actual generated candidates, no actor scheduling suppression, player-health grant, guaranteed die roll, fabricated corpse, or direct forced outcome. Observe one genuine escape and one genuine kill/no-corpse-or-feed outcome when seeds permit; record all attempted candidates, time/input ceiling and any failure to see an outcome. Save mid-search and during feeding, then verify replacement references, budgets and spent state after actual cache exit/return. An environment transition must not regenerate the pair or replenish a meal.
6. Perform full required regression and comparative environment-baseline evaluation, update the living plan/ledger and counter-checks in the same eventual commit, and keep the ordinary20-chunk experience review explicitly open until actually done. Source/private runner evidence cannot prove native stochastic outcome, animation readability or play balance.

## Stop conditions and scope control

A failure to achieve healthy escape around one real cover packet is significant and blocks F11 advertisement. Infinite pursuit, duplicate feeding/loot, wrong corpse binding, party/service targeting or a second action in one scheduler turn is also significant. Camera-only pose polish, specialized gait vocabulary, global greedy-path weaknesses outside the pair and offscreen ecosystem behavior are lower priority; record and defer them rather than widening this milestone. Neither a synthetic test kill nor a forced native winner is a substitute for fixing the local loop.

Stage1 source correction: ApplyDamage retaliation is deliberately Player-sourced only (CombatSystem1091–1103; existing RetaliationHookTests pins NPC exclusion). Ordinary hunter bite does not install personal hostility. Test real NPC damage, separate explicit SetPersonallyHostile state, and real Player damage independently; preserve the global rule.
