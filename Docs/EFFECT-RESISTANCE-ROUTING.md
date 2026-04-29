# Effect-Side Damage Routing — Fire & Acid Resistance Bypass

**Status:** shipped
**Branch:** `fix/effect-damage-attributes`
**Plan ref:** Tonic-content review follow-up to
`fix/electrified-effect-damage`. While reading `ElectrifiedEffect`
for that ship, two related bugs surfaced in `BurningEffect` and
`AcidicEffect`.

## Goal

`BurningEffect.OnTurnStart` deals `Fire`-attributed damage so
`HeatResistance` reduces / amplifies it. Same for `AcidicEffect` /
`Acid` / `AcidResistance`. Brings the per-turn DoT path into parity
with the on-hit weapon swing path, which already routes correctly.

## User-visible invariant

"A creature with HeatResistance = 100 (fire-immune, like a Glowmaw
variant) takes zero damage from BurningEffect, regardless of how it
was applied (FireTonic, FlamingSword on-hit, lit oil, etc.). Same
shape for AcidResistance and AcidicEffect."

## Bug evidence

`BurningEffect.cs:95`:
```csharp
int damage = RollDamage();
if (damage > 0)
{
    CombatSystem.ApplyDamage(target, damage, IgnitionSource, zone);
    //                              ^^^^^^ int overload
}
```

`CombatSystem.cs:662`:
```csharp
public static void ApplyDamage(Entity target, int amount, Entity source, Zone zone)
{
    ApplyDamage(target, new Damage(amount), source, zone);
    //                       ^^^^^^^^^^^^^^^^^ no attributes
}
```

`CombatSystem.cs:681-686`:
```csharp
private static void ApplyResistances(Entity target, Damage damage)
{
    if (damage.IsAcidDamage())     ApplyResistanceFor(target, damage, "AcidResistance");
    if (damage.IsHeatDamage())     ApplyResistanceFor(target, damage, "HeatResistance");
    if (damage.IsColdDamage())     ApplyResistanceFor(target, damage, "ColdResistance");
    if (damage.IsElectricDamage()) ApplyResistanceFor(target, damage, "ElectricResistance");
}
```

A `Damage` with no attributes returns false for all `IsXDamage()`
checks, so `ApplyResistances` is effectively a no-op and the full
amount lands. Same for `AcidicEffect.cs:43`.

This is asymmetric with the weapon path. `FlamingSword`'s on-hit
deals damage with `Fire LongBlades Cutting` attributes through
`PerformSingleAttack` → `ApplyDamage(typed Damage)`. So a
fire-immune creature takes zero from a sword swing but full damage
from the resulting BurningEffect tick. Inconsistent.

## Verification sweep (complete — no false premises)

| Premise | Status | Source |
|---|---|---|
| `Damage.AddAttribute("Fire")` sets `DamageAttributeFlags.Heat` | ✅ | `Damage.cs:74-99` |
| `Damage.AddAttribute("Acid")` sets `DamageAttributeFlags.Acid` | ✅ | `Damage.cs:74-99` |
| `IsHeatDamage()` is the predicate `ApplyResistances` checks | ✅ | `Damage.cs:118-121`, `CombatSystem.cs:684` |
| `IsAcidDamage()` ditto | ✅ | `Damage.cs:137-140`, `CombatSystem.cs:683` |
| Existing `ResistanceTests` exercises the typed-Damage path directly (not via Effects) | ✅ confirms why the bug wasn't caught | `ResistanceTests.cs:51-67` |
| `PoisonedEffect` / `BleedingEffect` use int overload too — but no `PoisonResistance` / `BleedingResistance` exists, so no bypass | ✅ benign | `Damage.cs:118-152` (only Cold/Heat/Electric/Acid/Light/Disintegrate predicates) |

**No corrections needed.** Two ~3-line edits.

## Test plan (8 tests)

1. **HeatResistance=100 nullifies BurningEffect damage** — RED before fix.
2. **HeatResistance=50 halves BurningEffect damage** — counter-check.
3. **HeatResistance=-50 amplifies BurningEffect damage** — vulnerability.
4. **No HeatResistance = full BurningEffect damage** — counter-check.
5. **AcidResistance=100 nullifies AcidicEffect damage** — RED before fix.
6. **AcidResistance=-50 amplifies AcidicEffect damage on organic** — vulnerability.
7. **No AcidResistance = full AcidicEffect damage on organic** — counter-check.
8. **Counter-check: ColdResistance does NOT block BurningEffect** — wrong-attribute regression guard.

## Performance section

Per CLAUDE.md Performance triggers — none apply. Each fix is a
3-line swap to use the typed-Damage overload. Same allocation
profile (one `Damage` per tick, same as before via the int overload's
internal wrapping).

## Pre-flagged self-review

- 🔵 `PoisonedEffect` and `BleedingEffect` could be migrated to the
  typed-Damage overload for consistency, even though no resistance
  type exists for them today. Defer until either a poison/bleed
  resistance ships OR the int overload itself gets deprecated.
- 🔵 The int-overload of `ApplyDamage` is itself a footgun — every
  caller that wants resistance-aware damage must remember to use
  the typed overload. Could remove the int overload entirely after
  migrating all call sites; tracked as a 🟡 finding for a future
  cleanup pass.
- ⚪ Damage formula unchanged on both effects. This is a correctness
  fix, not a balance change.

## Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ✅ | this commit |
| RED tests | ✅ | 5/7 RED as expected — 2 vacuous-pass tests are counter-checks (wrong-attribute-doesn't-block + no-resistance-deals-full) |
| GREEN impl | ✅ | 3-line edits in each effect; same allocation profile (one Damage per tick, was wrapped internally before, now explicit) |
| Confirm GREEN | ✅ | 7/7 EffectDamageAttributeTests + 202/202 broader sweep (all elemental weapon content + tonics + on-hit + scenarios + ResistanceTests) |
| Self-review | ✅ | All 🔴/🟡 clear; 🔵 PoisonedEffect/BleedingEffect migration deferred; 🔵 int-overload deprecation as separate cleanup |
| Merged to main | ✅ | this commit |
