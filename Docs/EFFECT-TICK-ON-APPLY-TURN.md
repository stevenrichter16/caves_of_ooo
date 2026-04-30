# Fix — Effects ticking on the same turn they're applied

**Status:** in progress
**Branch:** `fix/effect-tick-on-apply-turn`
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

1. Turn N: `TurnManager` fires `BeginTakeAction` on player.
2. Player issues `MovementSystem.TryMove`. The move succeeds; the
   destination cell's `EntityEnteredCell` event fires synchronously.
3. `BearTrapTriggerPart.OnTrigger` calls
   `actor.ApplyEffect(new StunnedEffect(1), ...)` — the new effect is
   appended to player's `StatusEffectsPart._effects` list **during
   the player's own action**.
4. Player's action completes. `TurnManager` fires `EndTurn` on player.
5. `StatusEffectsPart.HandleEndTurn` iterates `_effects` and ticks
   `OnTurnEnd` on **all** effects, including the just-added one.
   `StunnedEffect.Duration` decrements 1 → 0.
6. Cleanup loop removes effects with `Duration == 0`. The just-added
   `StunnedEffect` is removed before the player's next turn.

Net: the effect was applied and disposed of within turn N. The player
never gets stunned.

The mid-turn application path is unique to **self-applied** effects
during the owner's own action. For melee on-hit (e.g., `Mace` swing
applies `Stunned` to enemy) or AOE spells (e.g., `Conflagration`
applies `Burning` to enemies), the target is **not** the actor whose
turn just ended — its `HandleEndTurn` runs on its own next turn, well
after the apply event, so there's no same-turn evaporation.

## Verification sweep

| Premise | Status | Source |
|---|---|---|
| `EntityEnteredCell` fires synchronously inside `MovementSystem.FireCellEnteredEvents` | ✅ | `MovementSystem.cs:102` |
| `TurnManager.EndTurn` fires `EndTurn` event on the actor that just acted | ✅ | `TurnManager.cs:247` |
| `StatusEffectsPart.HandleEndTurn` iterates and ticks all effects unconditionally | ✅ | `StatusEffectsPart.cs:355-369` |
| `Effect.OnTurnEnd` decrements `Duration` if > 0 | ✅ | `Effect.cs:154-158` |
| `StunnedEffect(1)` constructed from `BearTrap` has `Duration = 1` | ✅ | `StunnedEffect.cs:12-15` + `TriggerOnStepPart.cs:239` |
| `BleedingEffect.OnTurnEnd` does the save-check (can early-out the effect) | ✅ | `BleedingEffect.cs:46-66` |
| `BurningEffect.OnApply` sets `Duration = ceil(intensity * 3)` when no FuelPart | ✅ | `BurningEffect.cs:42-44` |
| `ApplyEffectInternal` sets `effect.Owner = ParentEntity` before `effect.Apply` | ✅ | `StatusEffectsPart.cs:73-77` |

## Fix

Add a one-bit `JustApplied` flag to the `Effect` base class. Skip the
first `OnTurnEnd` for any effect that was applied during the owner's
**currently-active** turn. Gate "currently-active" on a new
`_isOwnerActing` field in `StatusEffectsPart` that flips on
`BeginTakeAction` and back off after `HandleEndTurn` — so:

- **Trap (self-apply mid-action):** `_isOwnerActing == true` →
  `JustApplied = true` → first `OnTurnEnd` skipped. Effect survives
  the apply turn. ✅
- **On-hit melee (other-apply during attacker's turn):**
  `_isOwnerActing == false` on the defender → `JustApplied = false`
  → tick normally on defender's later turns. **Existing behavior
  preserved.** ✅
- **Spell on enemy (other-apply during caster's turn):** same as
  on-hit. **Existing behavior preserved.** ✅
- **Self-applied tonic on own turn:** `_isOwnerActing == true` →
  `JustApplied = true` → +1 turn of effect duration vs. pre-fix.
  This is the correct semantic — drinking a 5-turn tonic gets you
  5 full turns of buff, not 4.

### Why a flag, not a turn-counter

A counter ("AppliedAtOwnerTurnCount == OwnerTurnCount → skip") would
also fail to distinguish "applied during my turn" from "applied while
I was idle" — both have the same counter value. The `_isOwnerActing`
gate is a true semantic distinguisher: it's only `true` between the
owner's `BeginTakeAction` and `EndTurn` boundaries.

## Sub-milestones

### M1 — RED tests

`Assets/Tests/EditMode/Gameplay/Effects/EffectTickOnApplyTurnTests.cs`:

1. `EffectAppliedDuringOwnAction_SkipsFirstOnTurnEnd` — set up entity,
   manually fire `BeginTakeAction`, `ApplyEffect(StunnedEffect(1))`,
   fire `EndTurn`. Verify Stunned is still present, `Duration == 1`.
2. `EffectAppliedWhileOwnerNotActing_TicksOnNextEndTurn` — apply
   `StunnedEffect(2)` without firing `BeginTakeAction`, then fire
   `EndTurn`. Verify Duration tick 2 → 1 (no skip).
3. `BearTrap_StepOnce_StunnedSurvivesToNextTurn` — integration:
   spawn player + bear trap, run a TryMove, run an EndTurn,
   verify player is Stunned and Duration=1. Run another EndTurn,
   verify Stunned cleared.
4. `BleedingFromTrap_SurvivesAtLeastOneFullTurn` — apply Bleeding
   mid-action; first EndTurn does NOT call `BleedingEffect.OnTurnEnd`
   (no save check). Effect still present.
5. `BurningFromFireTrap_FullDurationPreserved` — FireTrap applies
   Burning(intensity=1.5) → Duration set to 5. After apply turn's
   EndTurn, Duration still 5 (not 4). After the NEXT EndTurn,
   Duration = 4.
6. `EffectAppliedDuringOwnAction_FollowingEndTurn_TicksNormally` —
   ensure the flag is single-shot (cleared after first skip).
7. `OnHitFromAttacker_DoesNotSkipDefenderTick` — counter-check:
   defender's effect (applied while attacker is acting, not defender)
   does NOT get JustApplied=true. Defender's next EndTurn ticks normally.

### M2 — GREEN impl

- `Effect.cs`: add `public bool JustApplied;` (default false).
- `StatusEffectsPart.cs`:
  - Add `private bool _isOwnerActing;`.
  - In `HandleEvent`: set `_isOwnerActing = true` when
    `BeginTakeAction`/`TakeTurn` arrives.
  - In `HandleEndTurn`: skip `OnTurnEnd` for effects with
    `JustApplied == true` (clear flag). Then `_isOwnerActing = false`.
  - In `ApplyEffectInternal`: after `effect.Owner = ParentEntity`,
    set `effect.JustApplied = _isOwnerActing`.

### M3 — Combat regression sweep

Run effect/combat/scenario fixtures:
- `EffectDamageAttributeTests`, `BleedingEffect*`, `StunnedEffect*`,
  `BurningEffect*`, `OnHitClassEffectsTests`, `OnHitWeaponEffectsTests`,
  `CombatSystemSpecTests`, `ScenarioCustomSmokeTests`.

Watch for: any test that relied on "effect Duration ticks
immediately on apply turn" needs to be re-pinned. Most tests apply
effects without firing BeginTakeAction first (so JustApplied=false,
behavior unchanged). The few that DO go through BeginTakeAction
might need a one-line update if they assert on Duration after the
apply turn's first EndTurn.

## Files changed

| Path | Change |
|---|---|
| `Assets/Scripts/Gameplay/Effects/Effect.cs` | +1 field: `JustApplied` |
| `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs` | +1 field, +3 lines (set/clear in HandleEvent), +5 lines (skip in HandleEndTurn) |
| `Assets/Tests/EditMode/Gameplay/Effects/EffectTickOnApplyTurnTests.cs` | NEW — 7 tests |

## Self-review pre-flagged 🟡 findings

- **🟡 Save/load**: `JustApplied` is transient, not serialized. If a
  game saves between apply and EndTurn (very small window in practice),
  reload would reset the flag to false and the effect would tick on
  load. Acceptable for a small balance edge case; revisit if reports
  surface.
- **🟡 Self-tonic +1 turn**: Self-applied tonics now last one turn
  longer because the apply turn no longer eats a tick. This is the
  correct semantic — author intent is "5-turn tonic = 5 turns" — but
  any tuning that compensated for the old bug will need rebalancing.
  Currently no tonic durations are tightly tuned, so this is fine.
- **🔵 Effect added during another effect's `OnTurnEnd`**: The existing
  reverse-iteration loop in `HandleEndTurn` already doesn't visit
  newly-added effects in the same call (they're appended past the
  loop's starting index). The flag is a no-op for this case but
  doesn't break anything.
