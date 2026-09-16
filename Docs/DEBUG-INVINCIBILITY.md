# Player invincibility debug toggle

Status: implemented and verified. DI07 passes230/230 focused checks; DI08 full suite has14516 passes and32 identical baseline failures, zero new failures or C# errors. All40 added tests pass. CoO-original debugging convenience, no Qud parity claim.

F12 toggles invincibility for the current living player during ordinary gameplay, with explicit ON/OFF messages and no turn cost. It is available without enabling the unrelated DevMode sandbox. Default OFF; transient per-entity state does not enter saves or carry to newly loaded/recreated players. Preserve HP, normal NPC damage and all unrelated controls. The existing modal/boot/death gates remain authoritative.

## Verification sweep

| Premise | Actual source correction |
|---|---|
| An invulnerability flag already exists | No existing implementation in Scripts or tests. |
| HP protection alone handles death | CombatSystem.ApplyDamage funnels all normal damage; HandleDeath is also called directly. Body.Dismember mutates anatomy before calling death, so all three need early guards. |
| Every HP decrease is damage | LeyTap and HeartFlame pay nonlethal HP costs directly. Keep their existing costs; this is damage/death immunity, not unlimited resources. |
| Debug keys are available normally | DevMode defaults false. User explicitly requests a usable shortcut; F12 is unbound and independent of destructive F7/F8/F9/P tools. |
| Existing late debug section is ideal | It follows held-wait and rate limits. Place F12 in Normal input after save/pause guards, before those gates, so a fresh press is reliable and costs no turn. |

References inspected: CombatSystem.ApplyDamage/HandleDeath, Body.Dismember, InputHandler.Update, InputHelper, SaveLoadInputController, ControlsReference, DevMode, direct HP writers, native input test fixtures, PERF-FOUNDATION and ADVERSARIAL_TESTING. Existing CQ18 full baseline: 14,508 tests, 14,476 pass, 32 known failures.

## Implementation and gates

Use weak, identity-based runtime state; reject null/NPC/dead/missing-HP actors. Damage/death/anatomy early guards precede mutation and side effects. Emit damage-blocked and toggle/rejection diagnostics; show ON/OFF in the native message log and F12 in help. Verify actual keyboard edges through InputHandler, hold/repress, competing wait/move, modal suppression, toggle-off restoration, non-player counterchecks, direct death, mortal/nonmortal anatomy and save replacement. Dedicated adversarial fixtures cover identity, invalid states, repeated toggles and side effects. Run actual RED, focused tests, independent cold-eye review, and full regression in the isolated project; preserve the original Unity session.

## Performance

One existing key poll during normal input; no new Update, scene scanning or redraw loop. Weak identity lookup is allocation-free on the ordinary damage path; allocate state only when explicitly enabled. No stat inflation/heal loop, timers or temporary saved Parts.

## Honesty bounds and review

Independent production review complete; no material production issue found. The save test now restores TurnManager.Active in finally after the reviewer identified a harness cleanup gap. Headless injected keyboard tests verify native dispatch and simulation outcomes; they do not establish physical keyboard/OS interception or live feel. Costs and status effects can still occur, but normal incoming damage and dismemberment cannot kill the protected player. No save migration work.


## Implementation log and review

- DI01/DI02: tests written before production. DI01 also exposed a fixture API typo (`Entity.Statistics`, not `Stats`); DI02 confirms missing DebugInvincibility API RED. DI03 caught the native parameterless `MessageLog.GetLast()` signature; fixed the test call, not production.
- Implemented weak, reference-identity runtime state, F12 input/help and early damage, death and dismemberment guards. No tags, permanent Parts or stat inflation. Both normal and environmental damage are covered. Save round-trip confirms a loaded player starts unprotected.
- DI04:39/40, with a useful negative-control failure: bare numeric dice strings cause no poison/bleed damage. Changed fixture dice to valid `1d1+2` and `1d1+1`; the off-state now proves actual damage before comparing immunity.
- DI05:230/230. Independent exact-delta/player-flow review found no production defect and one test-global cleanup issue. DI06 was deliberately stopped in the isolated project before completion; no incomplete XML was interpreted. Restored the prior TurnManager.Active in the save test finally block.
- DI07:230/230, including40 new cases,22 in the dedicated adversarial fixture. DI08 repeats the complete suite with identical baseline failure names/messages. DI09 freezes2,107 original/isolated inputs, verifies GUID uniqueness and records the scoped integration delta.
- 🟡 Resolved review finding: save-test global TurnManager cleanup.
- ⚪ Deliberate: F12 works without enabling the wider DevMode sandbox. Intentional nonlethal HP spell costs and debuffs remain; this debug option grants damage/death immunity rather than all-resource cheats.
- 🧪 Native keyboard injection in EditMode verifies real InputHandler dispatch, held/repressed keys, rate limits, turn cost and boot/look-modal suppression. Physical OS key interception and uninterrupted live-session hot reload were not exercised. Some Mac keyboards require Fn+F12.

### Implementation snippets and files

`DebugInvincibility.TryToggle(player, out bool enabled)` validates a living Player and adds/removes weak runtime state. `Blocks(target, operation, source)` gates `ApplyDamage`, `HandleDeath` and `Body.Dismember` before their side effects. `InputHandler.Update` consumes F12 in normal gameplay after save/pause gates and before held-wait/move cooldown; `ControlsReference` advertises it.

New: DebugInvincibility.cs, core/adversarial/real-keyboard tests and matching metadata. Modified: CombatSystem.cs, Body.cs, InputHandler.cs, ControlsReference.cs. Already-mixed shared files remain installed and unstaged; the exact delta is preserved in [implementation.patch](Verification/DebugInvincibility/DI09-closeout/implementation.patch). Initially clean files are committed normally. This scoped checkpoint preserves unrelated workspace changes and does not claim a standalone clean checkout.
