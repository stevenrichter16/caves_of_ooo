# IceSword — Cold Counterpart to FlamingSword

> Tier-1 Quick Win, sibling content ship to `Docs/FLAMING-SWORD.md`.
> Branch: `feat/ice-sword`. Status: in progress.

## Goal

Ship an `IceSword` melee weapon whose attacks carry the `Ice` damage
attribute. Mirror of FlamingSword on the Cold side of the elemental
plane. Makes Phase E (`ColdResistance`) **player-visible** the same way
FlamingSword made HeatResistance visible: swing the IceSword at a
SnapjawHunter (ColdResistance=50) and watch the damage halve compared
to a control creature with no ColdResistance.

## User-visible invariant

"An IceSword's swings produce damage tagged `Cutting Ice LongBlades`.
SnapjawHunter's ColdResistance halves Ice-attributed damage. So the
IceSword that two-shots a Glowmaw will need ~twice as many hits on a
SnapjawHunter — symmetric to the FlamingSword/Glowmaw interaction."

## Phase mapping

| Phase | Surface | Used here |
|---|---|---|
| Phase C | `Damage.Attributes` list, `MeleeWeaponPart.Attributes` reflective load | Carries "Ice" through to Damage |
| Phase E | `ColdResistance` stat, `ApplyResistanceFor` in CombatSystem | SnapjawHunter halves damage |

No new infrastructure. Pure content + tests. The proven pattern from
FlamingSword applies one-for-one.

## Verification sweep — corrections table

| Premise | Status | Source |
|---|---|---|
| `Damage.IsColdDamage()` returns true for "Cold", "Ice", or "Freeze" attribute | ✅ confirmed | `Damage.cs:112-115` |
| `ApplyDamage` calls `ApplyResistances` which routes Cold → ColdResistance | ✅ confirmed | `CombatSystem.cs:620` |
| Snapjaw blueprint has `ColdResistance: 25` | ✅ confirmed | (grep on Objects.json) |
| SnapjawHunter blueprint has `ColdResistance: 50` | ✅ confirmed | (grep on Objects.json) |
| Glowmaw has NO ColdResistance — clean control for Ice damage | ✅ confirmed | Objects.json:2403 (only HeatResistance) |
| `MeleeWeaponPart.Attributes` reflective JSON load (re-verified per FlamingSword) | ✅ already proven | `MeleeWeaponPart.cs:47` |
| Bug-fix `fix(combat): hit log + floating FX + dismemberment use post-resistance damage` (commit `2bbe31b`) auto-applies to Cold because `ApplyResistanceFor` is shared between Heat and Cold | ✅ confirmed | `CombatSystem.cs:618-621` |

**No false premises detected.** All plumbing is shared with FlamingSword;
only the attribute string changes.

## Design choices

| Field | Value | Rationale |
|---|---|---|
| Name | `IceSword` | Symmetric naming with `FlamingSword` |
| Attributes | `"Cutting Ice LongBlades"` | Mirror FlamingSword shape; "Ice" matches weapon name (could also be "Cold" or "Freeze" — `IsColdDamage()` matches all three) |
| BaseDamage | `1d8` | Same as FlamingSword (paired tier) |
| PenBonus | `2` | Same as FlamingSword |
| Color | `&C` (light cyan) | Visual contrast with FlamingSword's `&R` (red) |
| Render glyph | `/` | Same shape as other swords |
| Weight | `6` | Same as FlamingSword |
| Material | Steel + tags `Metal,Conductor,Ice` | Mirror "Metal,Conductor,Fire" pattern; "Ice" tag for hypothetical material reactions |
| Commerce.Value | `40` | Same tier-2 pricing as FlamingSword |
| Tier tag | `2` | Same as FlamingSword |
| Hitpoints | `8` | Same as FlamingSword |

## Sub-milestones (smallest blast radius first)

### I.1 — Plan + branch (this commit)
- Plan to disk (this file)
- Branch `feat/ice-sword` cut from `main` at `2bbe31b`

### I.2 — RED + GREEN (one commit)
- Add 3 blueprint-shape tests in `WeaponAttributesContentTests.cs`:
  - `IceSword_HasCuttingIceLongBladesAttribute` (positive, exact-string)
  - `IceSword_AttributesContain_Ice` (Ice substring pinned)
  - `IceSword_DoesNotHavePiercing_OrBludgeoning` (counter-check)
- Add new file `IceSwordContentTests.cs` with 3 integration tests:
  - `IceSword_AttributesViaCombatPath_TriggerColdResistance` — synthetic damage
  - `NonIceDamage_OnColdResistantTarget_NotReduced` — counter-check
  - `IceSword_OnSnapjawHunter_TakesLessDamageThan_ControlTarget` — thematic pairing
- Run RED — expect blueprint-shape tests fail (`CreateEntity("IceSword")` returns null)
- Add IceSword blueprint to `Objects.json` (mirror FlamingSword shape)
- Run GREEN — all 6 pass

### I.3 — Self-review + commit
- Severity-marked findings
- Commit per §2.3 template
- Merge to main + push

### I.4 — Combined Elemental Showcase (separate branch, optional follow-up)
- New scenario `ElementalSwordShowcase` that includes both swords + targets
  for Heat and Cold + a control. Lets the player swap weapons and see all
  four resistance×attribute combinations side by side.
