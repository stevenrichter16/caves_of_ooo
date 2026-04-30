# BUG: Bear-trap bleeding "ends same turn" despite four sequential fixes

**Status:** Open. Deferred — coming back to this later.
**Severity:** UX-confusing but not gameplay-blocking. Stun + initial damage land correctly; bleeding's per-turn damage is what's not behaving as the player expects.
**First reported:** 2026-04-30, during `feat/trap-furniture` playtest.
**Last touched:** 2026-04-30, commit `3e45701` (`fix/damage-ticks-through-stun` merged).

## Symptom (in user's words)

> "the bleeding finishes during the same turn it gets applied. see the logs"

After stepping onto a `BearTrap`, the player perceives the `BleedingEffect` as ending immediately on the apply turn. Stun + initial 15 piercing damage land correctly; the bleeding-specific feedback is what fails to materialize.

## Repro

1. Launch `Caves Of Ooo / Scenarios / Combat Stress / Trap Furniture Showcase`.
2. Walk east through the corridor; step onto the bear trap (third trap, glyph `^&y`).
3. Observe the message log and any sidebar / HUD status indicators.

## What the log actually shows (post-`3e45701`)

```
ÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ        ← turn N start
you springs the bear trap! Iron jaws clamp shut.
you is stunned!
you is bleeding!
ÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ        ← turn N end
you is stunned and cannot act!
you is no longer stunned.
ÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ        ← turn N+1 end
```

Critical observations from `~/Library/Logs/Unity/Editor.log`:

- Exactly **one** `bleeding` entry in the entire log: `"you is bleeding!"` (the `OnApply` message).
- **No** `"you stops bleeding."` message anywhere — this is the canonical `OnRemove` message and the only path to remove `BleedingEffect` is `RemoveEffectAt` which fires `OnRemove` synchronously.
- **No** `"you takes X bleed damage."` message anywhere either.

So per the log, `BleedingEffect` is **APPLIED but never removed and never tick-damages**. By every `MessageLog.Add(...)` callsite, bleeding is still alive in the player's `_effects` list — but the user is convinced it has ended.

## Fixes shipped so far (each correct in its narrow scope, none of which fully resolved the user's perception)

| # | Branch | What it fixed |
|---|---|---|
| 1 | `fix/effect-tick-on-apply-turn` (8185e2b) | Added `Effect.JustApplied` flag + `_isOwnerActing` per-part field. `HandleEndTurn` skips first tick of `JustApplied` effects. Fixed StunnedEffect(1) evaporating same turn from BearTrap. |
| 2 | `fix/effect-tick-by-current-actor` (91b8008) | Replaced per-part `_isOwnerActing` with `TurnManager.Active.CurrentActor` query. Closes a lazy-create timing hole where `StatusEffectsPart` doesn't exist when `BeginTakeAction` first fires (Creature blueprint doesn't include the part). |
| 3 | `fix/burning-thermal-extinguish` (a934351) | `BurningEffect.OnApply` heats entity to `FlameTemperature + 50f` so `ThermalPart.HandleEndTurn` doesn't immediately extinguish it via the "if you're not at flame temp, you can't be burning" invariant. **Confirmed working** by playtest (see "you takes X fire damage" lines in the log post-fix). |
| 4 | `fix/damage-ticks-through-stun` (44e0a64) | Reordered `HandleBeginTakeAction` so `OnTurnStart` (damage ticks) fires before the `AllowAction` block check. Damage-tick effects (poison/burn/bleed) now tick even when the entity is stunned/frozen. |

After all four fixes: Stun + Burn + initial damage all behave correctly. Bleed remains the outlier per the user's report.

## What we know is correct

- `StatusEffectsPart.ApplyEffectInternal` sets `effect.JustApplied = true` for trap-applied effects (verified via the production-path integration test `TurnManagerFlow_StunFromTrap_PersistsAcrossPlayerTurnEnd`).
- `HandleEndTurn` skips `OnTurnEnd` for `JustApplied=true` effects on the apply turn (verified by 11+ unit tests).
- `BleedingEffect` has no `OnTurnStart`-removal path — the only self-removal is via `OnTurnEnd`'s save check (`SaveTarget=14` from BearTrap, with Toughness=18 → +1 mod, ~40% pass rate per turn).
- No code outside `StatusEffectsPart.RemoveEffectAt` mutates `_effects` other than `RestoreEffectsForLoad`.
- `MessageLog.Add` always fires `OnMessage` → Debug.Log, so any `OnApply`/`OnRemove` log line MUST appear in `Editor.log`.

## Suspected real causes (untested hypotheses)

The actual log shows bleeding is alive but the user reports it ending. One of these must be true:

### H1 — User is reading sidebar/HUD that's stale

The `SidebarStateBuilder.BuildStatusText` cache is keyed on `(count, name, duration)`. `BleedingEffect.Duration` is `DURATION_INDEFINITE = -1` and never changes between turns. If the cache hash is computed wrongly somewhere, "bleeding" could disappear from the sidebar even though it's still in `_effects`. **Worth checking by reading the sidebar status line directly via `execute_code` or screenshot the in-game HUD post-trap-step and verify whether "bleeding" is shown.**

### H2 — Stale Unity build / hot-reload edge case

The fixes were merged to `main` and pushed, but the user's Unity Editor may have been mid-domain-reload during the playtest, running an older compiled DLL. Verify the player saw the fix's behavior by inspecting `Editor.log` for diagnostic lines from the running DLL, or force-reload via `refresh_unity {mode: force}` and check the .NET DLL build timestamp matches the source mtime.

### H3 — A second `BleedingEffect`-like instance that I missed

Maybe BearTrap applies bleeding TWICE somehow (once via `BearTrapTriggerPart.OnTrigger`, once via... ?). The first tick goes through stack via `OnStack`, the second... no, the stack path is fine. But worth double-checking by adding `Debug.Log` in `BleedingEffect.OnApply` and counting calls per trap step.

### H4 — `EffectRemoved` event listener that suppresses log

Some part is listening for `EffectRemoved` and silently consuming the message. Less likely but possible — search for `"EffectRemoved"` listeners that might intercept Bleeding.

### H5 — The player is misinterpreting the log

The log clearly shows bleeding is alive. The user might be:
- Looking at a screenshot of an OLD log from before the fixes
- Looking at the sidebar (which might have stale state — see H1)
- Conflating "no longer stunned" as "no longer bleeding"
- Stopping their playthrough one turn too early to see the bleed damage tick that would have fired on turn N+2

## Diagnostic plan for next session

1. **Read the in-game sidebar status line directly via `execute_code`:**
   ```csharp
   var sb = SidebarStateBuilder.Build(player, zone, ...);
   Debug.Log($"Sidebar status: {sb.StatusText}");
   Debug.Log($"_effects: {string.Join(", ", effectsPart.GetAllEffects().Select(ef => ef.GetType().Name))}");
   ```
   Run this RIGHT after the bear-trap step. Compare what the player sees to what's actually in `_effects`.

2. **Add temporary diagnostic logs to:**
   - `BleedingEffect.OnApply` — log "[BleedDiag] APPLY" with stack trace
   - `BleedingEffect.OnRemove` — log "[BleedDiag] REMOVE" with stack trace + `LastRemovalCause`
   - `BleedingEffect.OnTurnEnd` — log "[BleedDiag] OnTurnEnd: rolling save vs {SaveTarget}, mod={mod}, roll={roll}, pass={pass}"
   - `BleedingEffect.OnTurnStart` — log "[BleedDiag] OnTurnStart: dealing {damage} damage"

   Have the user step on the bear trap and share the **complete** `[BleedDiag]` line sequence.

3. **Force-rebuild and verify build timestamp:** before the next playtest, run `refresh_unity {mode: force}` and verify the `CavesOfOoo.dll` build timestamp matches the latest commit. If the editor was deferred, the player may have been running stale code.

4. **Take a screenshot of the in-game UI right after the bear-trap step.** Specifically the sidebar and message panel. Compare the displayed effects against `_effects` ground truth.

5. **Review the user's ENTIRE recent screen log** — not just the filtered `[Combat]` lines — to see if any non-`[Combat]` log line is removing the effect (e.g., a `[Material]` reaction or `[Aura]` hook firing on bleed).

## Affected files (current state on `main`)

- `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs` — `HandleBeginTakeAction` reordered; `JustApplied` skip in `HandleEndTurn`; `TurnManager.Active.CurrentActor` check in `ApplyEffectInternal`.
- `Assets/Scripts/Gameplay/Effects/Concrete/BleedingEffect.cs` — unchanged from main as of `3e45701` (no diagnostic logs left).
- `Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs` — `OnApply` raises `ThermalPart.Temperature` to `FlameTemperature + 50f`.
- `Assets/Scripts/Gameplay/Effects/Effect.cs` — `JustApplied` field added.
- `Assets/Scripts/Gameplay/Turns/TurnManager.cs` — `Active` static + ctor that registers it.

## Reference logs

Last user-shared log on `main` post-`3e45701` (truncated to combat lines):

```
[Combat] you springs the bear trap! Iron jaws clamp shut.
[Combat] you is stunned!
[Combat] you is bleeding!
[Combat] ÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ
[Combat] you is stunned and cannot act!
[Combat] you is no longer stunned.
[Combat] ÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ
```

Note: The `you is no longer burning.` issue from earlier reports IS fixed — fire-damage ticks now visible in the log on the fire-trap turn series. Bear-trap bleeding is the only outstanding piece.

## Why we're parking it

The user has chosen to defer rather than continue iterating without runtime feedback. The Unity MCP tools are not currently registered in the active Claude Code session (server is running on PID 26173 but tool registration requires a session reload), so further diagnosis is best done with live `read_console` / `execute_code` access, which means picking this up in a fresh session.
