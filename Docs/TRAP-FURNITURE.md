# Trap Furniture — SpikeTrap, FireTrap, BearTrap

**Status:** shipped
**Branch:** `feat/trap-furniture`
**Plan ref:** `Docs/CONTENT-ROADMAP.md` Tier 2 → Trap furniture

## Goal

Ship three mechanical floor-trap entities that trigger on step:
SpikeTrap (piercing damage), FireTrap (fire damage + BurningEffect),
BearTrap (piercing damage + Stunned + Bleeding). All three reuse the
existing `TriggerOnStepPart` infrastructure already used by runes —
this is a content-only ship with three small new Part subclasses.

## User-visible invariant

"Stepping onto a trap fires once: the stepper takes damage and gets
the matching status effect, the trap is consumed (single-use), and
the player sees a 'X springs the Y!' log line. Trap layers (NPCs of
the matching faction) do not trigger their own traps."

## Why traps vs runes

Mechanically identical to runes — both extend `TriggerOnStepPart`.
Differences are flavor + glyph, not behavior:

| | Runes | Traps |
|---|---|---|
| Glyph | `*` | `^` |
| Theme | Magical sigils (rune cultists lay them) | Mechanical (dungeon-built) |
| Faction | Rune cultists ignore own runes | TriggerFaction unset by default → triggers on anyone |
| Future placement | Cast by rune-laying NPC | Pre-placed by dungeon generator |

## Verification sweep (complete — no false premises)

| Premise | Status | Source |
|---|---|---|
| `TriggerOnStepPart` is the abstract base — handles `EntityEnteredCell` event, faction filter, ConsumeOnTrigger removal | ✅ | `TriggerOnStepPart.cs:25-83` |
| `MovementSystem.FireCellEnteredEvents` fires `EntityEnteredCell` on non-mover occupants of destination cell | ✅ | `MovementSystem.cs:202-249` |
| Existing rune subclasses (`RuneFlameTriggerPart`, `RuneFrostTriggerPart`, `RunePoisonTriggerPart`) are the implementation pattern | ✅ | `TriggerOnStepPart.cs:86-147` |
| Rune blueprints inherit from `PhysicalObject`, use `Physics.Solid=false`, declare a Trigger part, tag as Rune | ✅ | `Objects.json` (RuneOfFlame, RuneOfFrost, RunePoison) |
| `CombatSystem.ApplyDamage(target, Damage, source, zone)` typed overload routes through resistances correctly | ✅ recently fixed | `CombatSystem.cs:524-651` |
| `Entity.ApplyEffect(effect, source, zone)` auto-creates `StatusEffectsPart` if missing | ✅ | `Entity.cs:291-293, 329-338` |

**No corrections needed.** Implementation is 3 small Part subclasses + 3 blueprints + tests + scenario.

## Sub-milestones

### M1 — SpikeTrap
- New `SpikeTrapPart : TriggerOnStepPart` — Piercing damage on step.
- Blueprint: glyph `^`, color `&w` (gray steel), Damage 12, ConsumeOnTrigger=true.
- Tests: damage on step, faction filter, post-trigger removal, counter-check (non-creature doesn't trigger).

### M2 — FireTrap
- New `FireTrapPart : TriggerOnStepPart` — Fire-attributed damage + BurningEffect.
- Damage routes through `HeatResistance` (typed-Damage overload — fire-immune Glowmaws barely care).
- Blueprint: glyph `^`, color `&R`, Damage 8, BurnIntensity 1.5, ConsumeOnTrigger=true.
- Tests: damage + BurningEffect, HeatResistance reduces damage.

### M3 — BearTrap
- New `BearTrapPart : TriggerOnStepPart` — Piercing damage + Stunned 1 turn + Bleeding.
- Blueprint: glyph `^`, color `&y`, Damage 15, StunDuration 1, BleedSaveTarget 14.
- Tests: applies all three (damage, Stun, Bleeding), single-use.

### M4 — Showcase scenario
- `TrapFurnitureShowcase`: a 5×3 corridor with one of each trap pre-placed.
  Player walks east, observes each trigger.
- Smoke test added.

## Test plan

1. SpikeTrap_OnStep_DealsDamage — basic.
2. SpikeTrap_OnStep_RemovesSelfFromZone — single-use.
3. SpikeTrap_NonCreatureSteps_DoesNotFire — counter-check (only Creature-tagged actors trigger). _Wait — actually the existing TriggerOnStepPart fires on any non-self actor; the runes don't gate on Creature tag. Pin current behavior or add a creature-only gate?_

   **Decision:** existing runes trigger on any actor (including thrown items). Match that for traps. The "only creatures step on traps" gating is naturally enforced by who actually moves (only NPCs and the player call `MovementSystem.TryMove`). Items don't move themselves. So a counter-check that "an item dropped onto a trap doesn't trigger it" is well-defined.

4. SpikeTrap_LayerFactionTriggers — TriggerFaction prevents own-faction triggers (mirrors rune tests).
5. FireTrap_OnStep_AppliesBurning.
6. FireTrap_OnStep_DamageRoutesThroughHeatResistance — counter-check (Glowmaw takes less than non-resistant target).
7. BearTrap_OnStep_AppliesStunned_AndBleeding — multi-effect.
8. BearTrap_StunDuration_BlocksOneTurn — adversarial: confirm `AllowAction` blocks.
9. SpikeTrap_TriggeredTwice_DoesNotDoubleFire — adversarial: removed-from-zone after first trigger means second mover doesn't re-trigger.
10. Trap-stepped-by-thrown-item — counter-check (items don't actually move via MovementSystem, so they don't fire EntityEnteredCell).

## Performance section

Per `CLAUDE.md` Performance triggers: none apply.
- ❌ No render hook (cell where trap dies is dirty-marked via existing entity-removal hooks)
- ❌ No hot-path allocations (one-shot trigger)
- ❌ No new cache
- ❌ No new MonoBehaviour
- ❌ No per-frame listener (event-driven, fires only on actor move)

Does not require a Performance section.

## Pre-flagged self-review

- 🔵 Reusable trap (rearm cooldown) deferred. First ship is single-use only,
  matching the existing rune pattern. If players want refillable traps
  in dungeons, add `RearmTurns: int` to `TriggerOnStepPart` — small
  follow-up.
- 🔵 Hidden / detect-search mechanic deferred. Traps are visible by
  default. A perception-based hidden trap system needs a Perception
  stat + reveal-on-detect hook; out of scope.
- 🔵 TripWire (multi-cell line trigger) deferred. Single-cell first.
- ⚪ Trap-stepped-by-NPC behavior is the same as player. Faction filter
  prevents friendly-fire if a TrapFaction tag is set on the trap.
- ⚪ Damage values picked by analogy: SpikeTrap=12 (mid), FireTrap=8 (lower
  + DOT), BearTrap=15 (highest, single-target hardest).

## Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ✅ | this commit |
| Verification sweep | ✅ | TriggerOnStepPart pattern confirmed |
| RED tests | ✅ | 9 tests covering 3 traps + faction filter + double-trigger guard + resistance routing |
| GREEN impl | ✅ | 3 Part subclasses (`SpikeTrapTriggerPart`, `FireTrapTriggerPart`, `BearTrapTriggerPart`) + 3 blueprints |
| Confirm GREEN | ✅ | 11/11 trap+rune sweep, 52/52 with scenario smokes, 252/252 broader sweep |
| Showcase scenario | ✅ | `TrapFurnitureShowcase` — corridor with all 3 traps; menu entry priority 112 |
| Self-review | ✅ | All 🔴/🟡 clear; 🔵 reusable trap, hidden mechanic, TripWire all deferred per pre-flag |
| Roadmap updated | ✅ | Trap furniture row flipped 💡 → ✅ |
| Merged to main | ✅ | this commit |
