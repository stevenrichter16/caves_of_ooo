# Iteration 07 — Frostbind provocation

Status: standalone RED→GREEN, dedicated adversarial checks, independent review and final native Unity EditMode verification complete.

A paid outsider drenching/coating can provoke hostility, but before this iteration Frostbind rooted a neutral without any response. This change adds retaliation only when this cast meaningfully installs or extends restraint on a live nonparty creature. Preserve Calm and other spell rules. CoO design; no Qud-parity claim.

## Verified premises

Before implementation, `Cryomancy_Frostbind.ResolveSpell` returned true after `ApplyEffect` without observing its result. `RootedEffect.OnStack` extends duration; a successful extension is meaningful harm. `StatusEffectsPart` runs veto and lifecycle observers; their replacements/removals must not turn a refused/no-effect cast into provocation. The normal dispatcher owns one cooldown and action and must retain existing fired-cast payment semantics.

## Acceptance

Full skill command against neutral/hostile/party, install/extension, veto, missing/foreign target, lifecycle removal or replacement, save round-trip and retaliation through ordinary Brain action. Only actual meaningful restraint provokes; cancellation/refusal and party protection remain. No global status-effect hostility change or Oilmark expansion in this iteration.

## Verification / review / files

- Observed RED: `iteration07-red.xml`, 8 cases: 4 intended failures for missing neutral response/extension/save/ordinary AI retaliation; 4 existing counters pass. No production change preceded this run.
- Initial GREEN: `iteration07-initial-green.xml`, 8/8. Dedicated adversarial fixture contributes another 22 cases covering intrinsic effect boundaries, stale owners, death/removal, invalid selection, party changes and independent observer effects.
- Focused plus existing combat/skill regression: `iteration07-focused-green.xml`, 268/268. This includes all 30 new cases, original Frostbind, skill target reliability, directed melee, companion/follow and combat-tactics suites.
- Broader diagnostic run: `iteration07-broad-regression.xml`, 324 total, 318 passed, 6 failed in existing `MultiCellAbilityConsumerTests.MovementSkill_DispatchesEntryAtBodyEdgeAndPreservesMoveVetoBypass` (Vault/Disengage/Charge × one/body-sized actor). These assertions expect movement to bypass `BeforeMove`; failure occurs at the initial cast acceptance. `iteration07-legacy-movement-baseline.xml` reproduces all six with the original HEAD Frostbind source substituted only in the isolated runner. Shared Assets were never reverted. These were pre-existing relative to this iteration and were not reported as green. Subsequent native triage confirmed historical contract drift; the test-only repair and current native result are documented in [ReviewMovementContract.md](ReviewMovementContract.md). The original failed receipts remain unchanged.
- Runner: isolated tracked EditModeRunner under `/tmp/system-depth-combat-runner`, .NET SDK 10.0.105, net8 target with runtime roll-forward, one worker. This proves gameplay and the production token-graph save pipeline, not Unity input, rendering, serialization or feel.
- Independent navigation/order-agent review: no blocking issue. Receipt timing correctly separates observer work from the intrinsic cast; final exact-effect/liveness checks reject stale restraint, and existing committed-cast payment stays unchanged.
- Self-review: 🟡 neutral nonparty targets now react only to remaining added restraint; 🔵 nonpositive/infinite prior duration, veto/removal/replacement, party protection and invalid targets do not create hostility; 🧪 actual Play/input/feel evidence remains separate from the passed native EditMode suite.
- Files: modified `Cryomancy_Frostbind.cs`; new `FrostbindRetaliationTests.cs` and `FrostbindRetaliationAdversarialTests.cs` plus metadata; this log and the five receipts above. No shared generic status effect, AI, input or save-format change in this slice.

## Final behavior

A successful install or finite-duration extension records ordinary personal hostility against the caster when both remain live in the same world zone and outside the same party. The existing brain handles response and perception; the spell does not forge a melee event or grant an extra attack. Calm, Oilmark and generic status applications keep their existing rules. A valid committed cast still pays its existing cooldown even if an effect listener vetoes it; an invalid/empty selected target remains a free refusal. No scope divergence from the bounded iteration 07 plan.

## Final native verification

[native-final-integration.xml](native-final-integration.xml), job `941ad3a3628542c19f5fda2b0a9e49b4`: **810/810 passed, 0 failed, 0 skipped**, including all 30 FrostbindRetaliation core/adversarial cases. Current native movement companion tests also pass after the separately reviewed test-contract correction. No actual-input Frostbind encounter or campaign-balance claim follows from this EditMode run.
