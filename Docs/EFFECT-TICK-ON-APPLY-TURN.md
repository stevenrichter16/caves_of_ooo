# Fix — Effects ticking on the same turn they're applied

**Status:** shipped (second attempt — first attempt 8185e2b was incomplete; this doc describes the working fix)
**Branches:** `fix/effect-tick-on-apply-turn` (first attempt), `fix/effect-tick-by-current-actor` (correct fix)
**Reported by:** playtest of `feat/trap-furniture` showcase

## Bug

When the player steps onto a trap (`BearTrap`/`FireTrap`/`SpikeTrap`),
the trap's status payload (Stunned, Bleeding, Burning) appears in the
log AND immediately disappears on the **same turn**. The effective
duration is one turn shorter than the configured `Duration`. For
`StunnedEffect(1)` from `BearTrap`, that's **0 turns of effective stun**.

Same shape historically reported with self-applied tonics and any
mid-turn effect application by the acting entity.

## Root cause

Trace for a player stepping on a `BearTrap`:

1. Game start: `GameBootstrap` calls `TurnManager.ProcessUntilPlayerTurn()`.
2. Inside `ProcessUntilPlayerTurn`: `CurrentActor = player`, fires
   `BeginTakeAction` event, then sets `WaitingForInput = true` and
   returns.
3. Player presses W. `InputHandler.Update` calls
   `MovementSystem.TryMoveEx(player, ...)` directly — the move
   succeeds; `EntityEnteredCell` fires synchronously on the trap.
4. `BearTrapTriggerPart.OnTrigger` calls
   `actor.ApplyEffect(new StunnedEffect(1), ...)`.
5. `Entity.ApplyEffect` → `EnsureStatusEffectsPart` lazy-creates a
   fresh `StatusEffectsPart` (the production `Creature` blueprint
   doesn't include it; it's added on first use).
6. `StatusEffectsPart.ApplyEffectInternal` adds the effect.
7. `EndTurnAndProcess` fires `EndTurn` event on player →
   `HandleEndTurn` ticks `OnTurnEnd` on every effect including the
   just-added one. `StunnedEffect.Duration: 1 → 0`. Cleanup loop
   removes it.
8. Turn N+1: no stun, player walks freely.

## Why the first attempt didn't work

Commit `8185e2b` added a `_isOwnerActing` flag on `StatusEffectsPart`,
set true on `BeginTakeAction` and false after `HandleEndTurn`.
`ApplyEffectInternal` captured the flag into the new effect's
`JustApplied`, and `HandleEndTurn` skipped the first tick of any
`JustApplied` effect.

This **failed in production** because of the lazy-creation timing
in step 5 above:

- `BeginTakeAction` fires in step 2, **before** the player has any
  `StatusEffectsPart`. There's no listener to flip `_isOwnerActing`.
- `EnsureStatusEffectsPart` creates a fresh part in step 5 with
  `_isOwnerActing = false` (default).
- `ApplyEffectInternal` captures `false` into `JustApplied`.
- `HandleEndTurn` ticks the effect normally.

Tests passed because they manually added `StatusEffectsPart` to the
test entity *before* firing `BeginTakeAction`. That's not the
production shape — production uses lazy creation.

**Lesson** (CLAUDE.md §6.3 honesty bounds): tests that fire
synthetic events on isolated entities don't catch part-lifecycle
bugs. The fix needs runtime verification or production-shape
integration tests.

## Working fix

Skip the per-part flag entirely. Query `TurnManager.Active.CurrentActor`
directly at apply time:

```csharp
var tm = TurnManager.Active;
effect.JustApplied = tm != null && tm.CurrentActor == ParentEntity;
```

`TurnManager.CurrentActor` is the canonical "who's acting" source —
set in `ProcessUntilPlayerTurn` before `BeginTakeAction` fires
(line 201), cleared at the end of `EndTurn` (line 262). The window
in which `CurrentActor == player` is exactly the player's active
turn, regardless of whether the player has a `StatusEffectsPart`.

Lazy-created parts read this query correctly the moment they're
created — no warmup, no listener, no part-creation-order
dependency.

`HandleEndTurn` still skips effects with `JustApplied=true` and
clears the flag on first tick (single-shot).

### Why a static reference is acceptable here

`TurnManager.Active` is a static "well-known service" reference,
set in the `TurnManager` constructor. Production has exactly one
`TurnManager` per running game session; tests instantiate
freely (the most recent wins, mirroring the production shape).

The alternative — threading the `TurnManager` instance through
every `Entity.ApplyEffect` call site — would require a sweeping
API change with no real correctness benefit. Qud uses a similar
pattern (`The.Game.Player`, `XRLCore.Core`) for global services.

## Coverage matrix

| Path | `CurrentActor` | `ParentEntity` | `JustApplied` | Behavior |
|---|---|---|---|---|
| Trap step (player → self) | player | player | true | Effect survives apply turn ✅ |
| Self-applied tonic on own turn | player | player | true | +1 turn vs pre-fix (correct semantic) ✅ |
| On-hit melee (attacker → defender) | attacker | defender | false | Unchanged ✅ |
| AOE spell (caster → enemy) | caster | enemy | false | Unchanged ✅ |
| Standalone test or no TurnManager | — | — | false | Legacy synthetic-event path ✅ |
| Effect applied between turns | null | any | false | Same as no-TurnManager path ✅ |

## Verification

| Premise | Status | Source |
|---|---|---|
| `TurnManager.CurrentActor` is set before `BeginTakeAction` fires | ✅ | `TurnManager.cs:201, 211` |
| `TurnManager.CurrentActor` is cleared at end of `EndTurn` | ✅ | `TurnManager.cs:262` |
| `Entity.ApplyEffect` lazy-creates `StatusEffectsPart` if missing | ✅ | `Entity.cs:329-338` |
| `Creature` blueprint does NOT include `StatusEffects` part | ✅ | `Objects.json:111-148` |
| `Effect.OnTurnEnd` default decrements `Duration` if > 0 | ✅ | `Effect.cs:154-158` |

### Production-path integration tests

The new fixture has tests that drive the actual `TurnManager`
production path **without** manually adding `StatusEffectsPart`
(matching the live game's lazy-create shape):

- `TurnManagerFlow_StunFromTrap_PersistsAcrossPlayerTurnEnd` —
  asserts `JustApplied=true` on a freshly-lazy-created part
- `TurnManagerFlow_TrapStun_BlocksExactlyOnePlayerTurnAfter` —
  multi-turn integration
- `BleedingFromTrap_SurvivesAtLeastOneFullTurn`
- `BurningFromFireTrap_FullDurationPreserved`
- `OnHitOnDefender_DoesNotSkipDefenderTick` (counter-check)
- `EffectAppliedBetweenTurns_DoesNotSkip` (counter-check)

## Files changed

| Path | Change |
|---|---|
| `Assets/Scripts/Gameplay/Effects/Effect.cs` | +1 field: `JustApplied` (kept from first attempt) |
| `Assets/Scripts/Gameplay/Turns/TurnManager.cs` | +1 static property `Active`, +1 ctor that registers it |
| `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs` | Removed `_isOwnerActing` field + BeginTakeAction setter; query `TurnManager.Active.CurrentActor` directly in `ApplyEffectInternal`; HandleEndTurn skip-on-JustApplied unchanged |
| `Assets/Tests/EditMode/Gameplay/Effects/EffectTickOnApplyTurnTests.cs` | Rewritten to use real `TurnManager` (no manual `AddPart(new StatusEffectsPart())`) |

## Self-review pre-flagged 🟡 findings

- **🟡 `TurnManager.Active` static**: introduces a process-wide
  reference. Acceptable trade-off (Qud uses similar `The.Game`
  globals). If we ever need parallel game sessions, this pattern
  will need rework.
- **🟡 `JustApplied` is transient (not serialized)**: if a save lands
  between apply and EndTurn — extremely small window in practice —
  the apply turn is treated as "already consumed" on reload.
  Acceptable; revisit if reports surface.
- **🟡 Self-applied tonics now last +1 turn vs. pre-fix**: this is
  the correct semantic, not a regression. No tonic durations are
  tightly tuned to the old bug.
- **🔵 Effect added during another effect's `OnTurnEnd`**: the
  reverse-iteration loop in `HandleEndTurn` already doesn't visit
  newly-added effects in the same call (they're appended past the
  loop's starting index). Flag is a no-op for this path but
  doesn't break it.
- **⚪ `BeginTakeAction` listener for AllowAction-blocking is
  unchanged**: the part's old responsibilities (block-on-stun,
  fire OnTurnStart) are preserved. Only the `_isOwnerActing` flag
  was removed — the existing AllowAction gate works as before.
