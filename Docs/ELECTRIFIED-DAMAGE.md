# ElectrifiedEffect — Per-Turn Lightning Damage Tick

**Status:** shipped
**Branch:** `fix/electrified-effect-damage`
**Plan ref:** Follow-up to `feat/throwable-consumables` audit. User-visible
asymmetry: `LightningTonic` produced no damage despite ThunderHammer's
Lightning attribute clearly being a damage type. This closes the gap.

## Goal

`ElectrifiedEffect.OnTurnStart` deals Lightning-attributed damage each
turn while active, routed through `ElectricResistance`. Brings
LightningTonic into parity with FireTonic / AcidTonic as a
damage-over-time payload. FrozenEffect remains the dedicated
hard-control effect — no DoT.

## User-visible invariant

"While a creature is electrified, it takes a small amount of
lightning damage each turn. Resistant creatures (StoneGolem,
ElectricResistance 50) take half. Vulnerable creatures (BrassHusk,
ER -50) take 1.5×."

## Verification sweep (complete — no false premises)

| Premise | Status | Source |
|---|---|---|
| `Effect.OnTurnStart(target, GameEvent context)` is the per-turn hook with Zone in context | ✅ | `StatusEffectsPart.cs:345-350`, mirrored by `BurningEffect.OnTurnStart:54-117` |
| `CombatSystem.ApplyDamage(target, Damage, source, zone)` is the typed-Damage entry point that routes `Lightning` attribute through `ElectricResistance` | ✅ | `CombatSystem.cs:510, 681-686` |
| `Damage.AddAttribute("Lightning")` sets the bitmask via `DamageAttributeFlags.Electric` | ✅ | `Damage.cs:74-99` |
| `BurningEffect` uses the int overload `ApplyDamage(target, int amount, source, zone)` — bypasses HeatResistance because no attributes propagate | ⚠️ separate bug, not in scope here | `BurningEffect.cs:95` |
| `FrozenEffect` deliberately deals no damage; serves as the hard-control peer | ✅ design-intentional | `FrozenEffect.cs` (no OnTurnStart override) |
| `ElectrifiedEffect.OnTurnEnd` already exists for chain propagation; `OnTurnStart` is currently NOT overridden | ✅ confirmed gap | `ElectrifiedEffect.cs:44` |

**No corrections needed.** Implementation is one method override + correct typed-Damage routing.

## Design

### Damage formula

`damage = 1 + floor(charge × 1.5)`

- `charge = 1.0` (LightningTonic default) → 2 damage/turn × 2 turns = 4 total
- `charge = 2.0` (wet target, OnApply doubled) → 4 damage/turn × 3 turns = 12 total
- `charge = 0` → 0 damage (no-op early return)

For comparison:
- BurningEffect intensity 1.0 → 1-2 damage/turn × 3 turns = 3-6 total
- AcidicEffect corrosion 1.0 → 5 damage/turn × ~5 turns = 25 total (organics only)

Lightning at charge 1.0 lands between Fire and Acid — not the strongest DoT
but reliable, and the wet-amplification doubles it which gives players
a discoverable combo.

### Resistance routing

Use the typed `Damage` overload of `ApplyDamage`:

```csharp
var dmg = new Damage(amount);
dmg.AddAttribute("Lightning");      // sets DamageAttributeFlags.Electric
CombatSystem.ApplyDamage(target, dmg, source: null, zone);
```

`CombatSystem.ApplyResistances` checks `IsElectricDamage()` and applies
`ElectricResistance`. This is the same path ThunderHammer's swing uses,
so the resistance creature roster (StoneGolem +50, BrassHusk -50)
already covers it.

### What does NOT change

- `OnApply` — still stuns + amplifies on wet (existing behavior).
- `OnTurnEnd` — still propagates chain to conductors (existing behavior).
- `OnRemove` — unchanged.
- `OnStack` — unchanged.
- `FrozenEffect` — stays no-DoT (this is design-intentional).
- The chain payload — chained-to entities still receive only the
  Electrified state, not damage. That's a larger feature; out of scope.

## Test plan (8 tests)

1. **Damage on first turn start** — RED. Apply ElectrifiedEffect, fire BeginTakeAction, HP drops by `1 + floor(charge × 1.5)`.
2. **Damage every turn while active** — duration 2 → expect damage on 2 distinct ticks.
3. **No damage when charge is 0** — counter-check (degenerate input doesn't crash).
4. **Damage routed through ElectricResistance** — StoneGolem (ER=50) takes half. Counter-check via plain Snapjaw (no ER) takes full.
5. **Lightning attribute set on damage instance** — counter-check that the routing path is correct: a target with `HeatResistance=100` should NOT block lightning damage (because it's a different attribute).
6. **Damage stops after duration ends** — apply, advance N turns past Duration, no more damage.
7. **Wet amplification doubles per-turn damage** — apply WetEffect first, then ElectrifiedEffect. Damage tick is computed against the doubled charge.
8. **Counter-check: FrozenEffect still doesn't damage** — regression guard, ensures we didn't accidentally generalize.

## Performance section

Per CLAUDE.md Performance triggers:
- ❌ No render hook (combat damage already plumbs renderer via Fix #2)
- ❌ No hot-path allocations (one Damage instance per tick — bounded by active electrified entities)
- ❌ No new cache
- ❌ No new MonoBehaviour
- ❌ No per-frame listener (per-turn, not per-frame)

Does not require a Performance section.

## Pre-flagged self-review

- 🔵 Damage formula `1 + floor(charge × 1.5)` is a starting point. If
  playtest shows it too weak / too strong, single-line balance.
- 🔵 The chain mechanic (`HandleTryChainElectricity`) still doesn't deal
  damage to chained creatures — only propagates the `Electrified` state.
  That's a separate feature (Tier-2 "lightning chain damage") deferred
  for now.
- 🔵 BurningEffect uses the int overload `ApplyDamage(target, int, ...)`
  which bypasses HeatResistance (a separate bug discovered while
  reading this file). NOT fixed in this commit — flag as a 🟡 finding
  for the upcoming tonic-content review.
- ⚪ Lightning damage source argument: passing `null`
  (no killer attribution). For a tonic the player threw, the player IS
  technically the source — but `IgnitionSource`-style attribution
  introduces complexity (effect needs to remember who applied it).
  BurningEffect does pass `IgnitionSource`; do the same here.

## Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ✅ | this commit |
| RED tests | ✅ | 6/8 RED as expected — the two passing were the FrozenEffect counter-check + zero-charge edge case |
| GREEN impl | ✅ | `OnTurnStart` override, ~10 LOC; routes Lightning damage through `ElectricResistance` |
| Confirm GREEN | ✅ | 8/8 ElectrifiedEffectDamageTests + 158/158 broader regression (ThunderHammerContent + LightningTonic regressions all pass — existing combat behavior preserved) |
| Self-review | ✅ | All 🔴/🟡 clear; 🔵 source attribution and BurningEffect-bypass both flagged for the upcoming tonic-content review |
| Merged to main | ✅ | this commit |
