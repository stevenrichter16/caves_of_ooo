# Genuine native keyboard combat in the reference glade

Private plan only. Root owns Unity and publication. Preserve every existing art/composition receipt, including the recent 12/12 run; do not relabel it as a player combat demonstration.

## Verified gap

ReferenceGladeNativePlayer's `combat_uses_live_creatures` currently passes if fewer than three live Marlbacks remain, any MarlbackCorpse exists, or any Marlback has less than maximum HP. It requires neither a keyboard attack nor a player-attributed hit. The actual glade also contains a Warden and PetDog, so autonomous NPC combat while the player walks to the chest, wall and vein can satisfy the condition. A missing initial creature could satisfy the count clause. The current result is creature attrition observed during keyboard traversal, not player combat acceptance.

The live plan places MarlbackScrabblers at (42,19) and (22,5), a Gleaner at (44,20), Warden at (35,15), Villager at (38,21), and PetDog at (33,23). The ordinary player starts at (40,12). Prior runs complete many movement turns before checking attrition, allowing the Warden/dog to fight first.

## Bounded correction

Use a separate isolated `ReferenceGladeCombatNativePlayer`/launcher (or a clearly distinct combat mode on the existing launcher), preserving the current visual route and captures. Run combat first, before chest/harvest/travel tasks. The same actual glade scene, seed and ordinary new-game actor must be used, with the actual starter dagger equipped through native inventory input. No spawned enemies, altered factions/HP/stats/schedules, forced animation hooks, source replacement, travel shortcuts, direct attacks, random-seed changes or repeat-until-success source selection.

1. Record all three original hostile IDs, species, exact cells, HP, Brain.CurrentZone, TurnManager registration and actual equipment. Require all three present and alive at the initial checkpoint, plus the existing Warden/dog identities. Require ordinary player 40 maximum HP, Strength/Agility/Toughness18, Ego16, Intelligence/Willpower10, no BitLocker/dev/invulnerability and no full reveal. These are controls, not a combat result.
2. Select the original northern MarlbackScrabbler authored at(22,5), before advancing further turns; record that exact owner and the distance from the ordinary start. Root approved this existing encounter to give the player a route before the Warden/dog engages the two southern actors. Never replace the selected owner. Replan a legal keyboard route to that exact owner after each key; record any naturally intervening creatures. If the chosen target dies to an NPC before player contact, retain that failure instead of silently replacing it or reseeding. Limit approach to40 movement attempts and the complete audit to180 seconds.
3. Once adjacent, press the real movement/attack direction key into the current enemy footprint. Keep real target/faction/range checks and existing input action costs. Wait for existing FX with a bounded loop, preserving death and all errors. Never call CombatSystem.PerformMeleeAttack, ApplyDamage or EmitAttack from the harness.
4. Observe actual `Diag` damage records, preserving the prior damage-channel setting. Add one scenario marker immediately before each key and collect records after that exact marker immediately after it. Each accepted player swing must have a new `HitRoll` with exact player ActorId and selected target TargetId; one accepted hit must also have positive `DamageDealt` with those IDs and the SAME nonempty CauseTraceId. Save full raw records, input key, cells, HP, tick/energy and messages. NPC-on-NPC or environmental damage cannot satisfy this correlation. Fail clearly if the marker no longer remains in the ring; never clear somebody else's diagnostic history.
5. Require actual scheduler response, evidenced by a fresh hostile→player HitRoll (an attempted retaliation is enough; do not force damage or misses). The required retaliation must come from the selected owner; other hostile attempts are recorded separately and cannot impersonate its response. A Brain component or scheduler registration alone is insufficient evidence of acting.
6. Continue bounded real attacks (at most40), with actual ordinary starter tonics allowed only through existing native inventory actions if needed; record their use and never grant replacements. Success requires player-attributed lethal DamageDealt correlated to that player's HitRoll, selected target `_DeathHandled`, disappearance of the living owner and an actual corpse/drop at its final cell. NPC kill-stealing is an honest failure, not credited to the player. No balance claim from this one encounter.
7. Verify native presenter ownership before the fight and sample its actual Animator/bone pose during keyboard attacks. Observe the player's actual Attack state/bone change in Update without playing or mutating clips. Require the dead target view to disappear after natural renderer refresh; living nearby actors remain rendered according to FOV. Captures should show pre-contact, a real attack, and outcome/HP. If attack capture timing misses a transient clip, fail the visual subcheck honestly; the raw combat records remain separate evidence.
8. Finish with ordinary live actor, no reveal, normal input, unchanged save-isolation/lifecycle contracts. The launcher reuses reviewed scene-restore retry, save root, seed, prefs, input settings and editor cleanup. Native first failure and full log remain archived. Add its two scene-restoration parameter cases before execution.

## Acceptance and negative controls

Prefer a small pure receipt-validation helper, exercised privately before integration: zero initial hostiles, Warden→Marlback damage, player→other target damage, stale records, same IDs with missing/different cause, miss-only hit roll, zero damage, NPC lethal after a player wound, dropped diagnostic window and manually changed HP all refuse. A synthetic correctly correlated receipt validates the parser only; it does not count as Play evidence. Runtime/editor reference compilation must pass before root runs actual native input.

Required native checks should explicitly distinguish source authority, keyboard approach, player attack dispatch, player damage, NPC response, player-attributed defeat, real presentation response and cleanup. Do not replace these with a raw total. The old check should be renamed `observed_live_hostile_attrition` if retained in future visual audits; historical reports remain untouched.

## Execution prompt

Implement only this native audit and its evidence parser/tests after root approves the bounded plan. First record failing parser/control cases, then implement the smallest read-only observer and genuine keyboard route. Preserve normal gameplay and all first failures. Compile privately against actual Unity references, request an independent source review, and wait for root's explicit shared publication window. No Unity calls by this agent.

## Private parser evidence

The26-case helper suite first recorded7 intended missing-feature REDs and19 negative controls GREEN, then26/26GREEN after implementing exact marker/actor/target/cause correlation and positive typed damage parsing. This proves only receipt validation. No native input, combat, pose observation or user-visible acceptance has run for this distinct combat audit.

## Verified implementation corrections

| Premise | Source correction | Implementation consequence |
| --- | --- | --- |
| A Scrabbler corpse always appears. | Actual MarlbackScrabbler CorpseChance is70, while its real authored loadout owns a weapon. | Require either a corpse whose SourceID/KillerID match this exact fight, or the exact pre-fight owned item ID now on the ground at the lethal cell. Never make a corpse roll succeed. |
| A transition toward Attack plus changed bones proves the attack frame. | Crossfade still contains the earlier pose during transition. | Require the actual current Attack state with positive normalized time and changed bound bones, correlated to a real player HitRoll in the same native key window. |
| There must be a separate launcher lifecycle. | The existing launcher can pass a distinct combat mode to the same isolated player; its scene/save restoration path is unchanged. | Add LaunchCombat and a mode flag only, preserving existing restoration tests rather than duplicating two identical cases. |

## Actual dagger-only death and bounded starter tactics

Actual native run `f86bde7dfd1f4462a6a6cab0311e1d22` retains its original report, raw combat records, captures and restoration receipt. Source preflight passed. Twelve real moves reached the exact northern Scrabbler; five swings gave player HP40→25→25→15→15→0 while the target ended4HP. Both actual starter tonics were unused. The run proves actual attack dispatch/damage/retaliation and a witnessed Attack bone frame, but neither a live completion nor a balanced encounter. The script's failure to use available kit is a concrete audit-policy gap, not evidence by itself of a balance defect.

Source sweep: HealingTonic is `4d6+4` (not4d4); native ApplyTonic spends exactly one carried unit before healing and emits event/TonicApplied with exact user/target/item/consumed fields. Rime Grip is an actual starting ability, range5, cooldown35, damage4 and freeze0.5; it can kill, so this bounded audit only uses it when the same adjacent target has more than4HP. This preserves the existing final melee-attribution gate without changing spell behavior. Calm is not used.

Root approved: capture the initial owned HealingTonic identities/units and initial Rime ability; at most two real inventory uses at HP≤two-thirds maximum, and at most one ready Rime after this target has actually retaliated. Recheck current identity, ownership, life, native FOV and targeting before keys. Tonic evidence requires exactly one fresh own TonicApplied plus total original-unit decrement by1; net HP alone cannot prove healing because NPCs may hit during the action. Rime requires real hotbar input/direction, cooldown cost and actual target Frozen or ordinary death. No cooldown reset, replacement tonic, direct ability call, stat changes, alternate seed/target, or retry until success. Bounded support actions are reported independently from movement/melee windows and never fabricate an attack or kill witness.

Before implementation, add paired read-only tonic receipt controls and preserve RED. Compilation/tests only prove code/receipt parsing; actual improved native route remains pending.

Source correction from the actual InventoryUI dispatch: ApplyTonic is currently an inventory-only action and does not set pendingEverydayTurn. The audit preserves and records actual tick/energy rather than asserting or manually charging a turn. Its real cost is one consumed starter unit. Rime uses the normal hotbar scheduler and cooldown. This is a scope limit, not a covert turn-cost repair.

Paired tonic evidence:43 total cases,2 intended positive RED with41 controls, then43/43 GREEN. Full actual Unity-reference runtime and fixture compile zero errors. Native strategy execution remains pending. On nested failure the runner now disposes iterator windows so their finally blocks retain support-action evidence; diagnostic event-channel ownership is restored alongside existing damage-channel ownership.
