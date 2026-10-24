# Riposte action-feedback audit and narrow next-slice plan

2026-10-24. Read-only code/test audit. No Assets edits, Unity actions, tests, git changes or implementation performed. This is a recommendation for the next slice.

## Finding and severity

P2 presentation gap: `LongBlades_Riposte.TryCounter` (`Assets/Scripts/Gameplay/Skills/LongBlades_Riposte.cs:39`) directly issues `CombatSystem.PerformSingleAttack`, so an actual counter has combat audio, damage/defense and the post-result “Riposte” word, but no Attack visual hook or facing update. The only production melee hook is the outer ordinary/committed action wrapper (`CombatSystem.cs:130-133`). `AnimatedEntityRenderer.cs:451-461` consumes that hook to request the flat Attack pose and reads `RenderPart.VisualFacing`; the two legacy 3D presenters consume it too. This is not a missing combat resolution or missing sound.

`PerformSingleAttack` only returns Cancelled at its null attacker/defender/zone/RNG guard (`CombatSystem.cs:231-233`). Natural miss, full parry, armor stop, damage veto/resistance and ordinary hit remain attempted strikes. A hit-only or positive-damage-only animation would therefore be wrong.

## Complete direct-call inventory

Search covered `Assets/Scripts`, separating comments/test references from actual calls.

| Direct gameplay caller | Current issue boundary / grouping |
| --- | --- |
| `LongBlades_Riposte.cs:39` | Selected held blade after full parry, eligibility/proc/revalidation, allowance spent and reserved; explicit ReactiveCounter child with follow-ups suppressed. |
| `ShortBlades_Rejoinder.cs:65` | Defender’s selected Piercing weapon after incoming miss; per-instance recursion guard; explicit ReactiveCounter child. |
| `Cudgel_Backswing.cs:45` | Same attacker/weapon after miss and proc; per-instance recursion guard; explicit ExtraAttack child. |
| `Axe_Whirlwind.cs:103` | Active action; multiple adjacent targets in a loop, revalidating each. |
| `ShortBlades_Flurry.cs:104` | Active action; up to three same-target strikes, revalidating continuation. |
| `Cudgel_Conk.cs:78` | Active action; one strike and contact-dependent rider. |
| `Cudgel_ChargingStrike.cs:130` | Active action; movement, strike, then eligible damage rider. |
| `Axe_HookAndDrag.cs:93` | Active action; one strike and eligible hooked rider. |
| `ShortBlades_Backstab.cs:138` | Active action; strike plus eligible flank damage/procs. |
| `ShortBlades_Shank.cs:127` | Active action; strike while scoped penetration bonus is active. |
| `LongBlades_Lunge.cs:144` | Active action; movement then selected-weapon strike. |
| `LongBlades_FollowThrough.cs:18` | Active action; strike then optional move into killed target’s old cell. |

None of these twelve skill call sites separately emits Attack or calls `FaceToward`. The nine active ability implementations create ActiveAbility provenance scopes, but those scopes do not emit a visual event. Rejoinder/Backswing are therefore adjacent known gaps, not automatically fixed by the proposed Riposte slice. Keep them in the follow-up ledger rather than silently widening this patch.

Internal direct calls at `CombatSystem.cs:160,198,221` are already covered by the outer wrapper’s single event: natural/unarmed fallback, each selected hand, and legacy weapon. `FiftySecondCombatBench.cs:77,78,95,137,149,150` is controlled scenario instrumentation, not an ordinary runtime entry point. Existing tests make many direct calls deliberately and must not all acquire unconditional extra presentation behavior.

## Recommended smallest boundary

Add an explicit opt-in to `PerformSingleAttack` (for example a final optional `bool emitAttackVisual = false`) and pass it by name **only from Riposte** initially. In the method, place the opt-in after the null cancellation guard and after entering the supplied strike provenance scope, before audio begin, accuracy RNG, defense, damage and child hooks. It should call the existing facing calculation and emit exactly one Attack notification. Keep the ordinary/committed wrapper’s existing event untouched and leave its internal calls at the default false.

Reasons for that location:

1. It reports a validated issued strike, including misses/parries/armor stops; failed proc, invalidated eligibility and null cancellation never emit.
2. Facing is set while both live contact cells still exist. A post-result event can no longer derive direction if the counter killed/removed its opponent.
3. Attack starts before retaliation/Hurt/death, preserving the existing renderer’s bounded Attack→Hurt ordering. Emitting only after resolution risks overwriting a Hurt request or silently losing the attack because a participant died.
4. The callback sees the counter’s actual `MeleeStrikeScope.Current`, not its parent incoming strike. Existing reserved/spent gates and suppressed reactive ancestry remain in force during callbacks.
5. Default false preserves one ordinary action event even when primary, offhand and natural fallback all strike. No “already animated” global flag, actor cache, action-ID set or broad combat rewrite is needed.

Do **not** reroute Riposte through `PerformMeleeAttack`: it would change selected-weapon semantics, attack veto/oath/companion behavior, hand gathering, provenance and possibly swing count. Do not infer visual policy from display tag `(Riposte)`, and do not unconditionally emit inside every single strike. Do not add audio to the visual hook: `WorldAudioCombat.Begin` already emits the actual counter swing, and Defense already emits the preceding parry.

## Observer failure and reentrancy

`EntityVisualHooks.EmitAttack` (`EntityVisualHooks.cs:94-97`) currently invokes its multicast callback without exception containment. Adding that unprotected call before the counter strike would create a new failure mode: a bad renderer spends the allowance and aborts combat. The new opt-in boundary must contain optional visual-observer exceptions, following the small existing `WorldAudioCombat.Emit` pattern (`WorldAudioCombat.cs:78-79`), so simulation resolution/RNG does not change because a presenter throws. This containment can be local to the new opt-in path; hardening every hook is outside this slice. A single protected multicast invocation need not allocate `GetInvocationList()` arrays. It also does not promise later subscribers run after an earlier subscriber throws; do not claim such stronger isolation without implementing/testing it.

Keep `budget.Spent=true` and `budget.Reserved=true` in force before notification, with the existing `finally` release. A callback that tries the normal `TryCounter` gate must not issue a nested counter or consume another RNG roll. Existing provenance scope disposal must still restore the parent on exception/return. Presentation subscribers are not gameplay authorities: do not claim arbitrary callbacks that directly mutate entities or invoke unrestricted combat are sandboxed. If a mutation test exposes a new post-notification validation gap, address only that concrete committed-counter case; do not silently change all legacy combat callers’ permissive contracts.

## RED-first tests and counterchecks

Use `FiftySecondCombatFixture` plus deterministic `ComposableRiposteTests.Rolls` / `ComposableRiposteAdversarialTests.MutableRolls`. Save/restore `EntityVisualHooks.AttackCallback` and all other globals in teardown. Prefer actual full-parry→TryCounter→strike flow over calling the hook directly.

1. **RED: issued Riposte animation/facing.** Ordinary attack causes one event for the original attacker and one for the countering defender, in order. At the counter callback, assert actual actor/target/zone, expected facing, spent+reserved allowance and ReactiveCounter provenance. Pin HP, hit/percent draw counts and existing parry/swing audio order to the no-new-RNG behavior.
2. **Miss, full parry and armor-stop counter cases.** Each issued counter emits one Attack even with zero damage. Failed counter proc emits none. No source, spent allowance and reserved allowance emit none and retain their existing draw/allowance semantics.
3. **Post-proc invalidation matrix.** Reuse disarm, move out of contact, stun, remove, dead, removed skill and destroyed selected blade cases; each cancels before issue with zero counter event and no additional attack RNG. Do not substitute another held blade.
4. **Lethal ordering.** Counter event occurs while original attacker still occupies its live cell, before counter damage/death; afterward the target is removed as before. Counterer hurt/killed by retaliation must not have a late Attack event that resurrects or restarts its visual. Verify existing damage/event-word ownership still emits only once.
5. **Callback throws.** Deliberately throw from new counter visual callback: strike still resolves, same HP/RNG/provenance/audio results, spent remains true, reserved clears and ambient scope restores. Null renderer/callback remains harmless.
6. **Callback reentry.** During counter callback call `TryCounter` through its real eligibility gate. Exactly one counter total; no extra proc/hit RNG, nested animation or lingering reservation. Opposing full parry must still not counter-counter; existing follow-up suppression for Rejoinder/Backswing stays intact.
7. **Ordinary nonduplication.** Single ordinary hit/miss = one original Attack. Multi-hand/offhand-always case with two actual strike rolls still = one original action event. BeforeMeleeAttack veto = zero. `PerformCommittedMeleeAttack` stays one event/one primary strike. Direct non-opted scenario/single-strike calls retain current behavior.
8. **Native renderer gate.** Actual player/default accepted actor counter requests the existing Attack frame and returns to Idle; face left/right accurately. Current hidden/fog/foreign-zone/replaced owner must not gain a visible view or replay a deferred event after reveal. No new actor animation sheet or aliasing of static Cindercaller art is authorized by this fix.

Existing relevant evidence suites: `ComposableRiposteTests`, `ComposableRiposteAdversarialTests`, `ComposableComparisonAdversarialTests`, `FloatingCombatTextTests.RiposteReportsAnIssuedCounterEvenWhenItMissesButNotAFailedChance`, `WorldAudioDefenseTests.ParryPrecedesRiposteEvenWhenCounterKillsOriginalAttacker`, and `FlatPixelActorMotionTests` (ordinary miss/veto, Attack→Hurt, duplicate coalescing, hidden hooks, foreign zone, lifetime). They cover much of combat semantics but not the missing counter Attack callback; that new assertion must demonstrably fail first.

After native GREEN/regressions, capture one ordinary keyboard full-parry/riposte in actual Play with action receipts and bounded frame sequence. Test-only hook emissions or a static posed body are not action acceptance. Document the still-unaddressed active/Rejoinder/Backswing visual gaps explicitly, with no claim that all melee skills are fixed.
