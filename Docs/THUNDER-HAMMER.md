# ThunderHammer — Lightning-Carrier Bludgeoning Weapon

> Tier-1 Quick Win, third elemental weapon after FlamingSword (Fire)
> and IceSword (Ice). Branch: `feat/thunder-hammer`. Status: in progress.

## Goal

Ship a `ThunderHammer` melee weapon whose attacks carry the `Lightning`
attribute. Mirrors the FlamingSword/IceSword pattern on the Electric
side of the elemental plane. Adds **vulnerability** as a new visible
mechanic: BrassHusk (Conductivity 0.95) takes **1.5×** damage from
Lightning, while StoneGolem (non-conductive Stone) takes **half**.

This is the first content ship to expose **three** Phase E points
on the resistance curve in a single scenario: positive (halved),
negative (amplified), and zero (control).

## User-visible invariant

"A ThunderHammer's swings produce damage tagged `Bludgeoning Lightning
Cudgel`. StoneGolem's ElectricResistance=50 halves it; BrassHusk's
ElectricResistance=-50 amplifies it by 50%; Snapjaw (no ER) takes
full damage. The same weapon produces three visibly different per-hit
outcomes depending on the target's stat — and the player can read
which lever fires by checking the [ThunderDemo] log line."

## Phase mapping

| Phase | Surface | Used here |
|---|---|---|
| Phase C | `Damage.Attributes`, `MeleeWeaponPart.Attributes` reflective load | Carries "Lightning" through to Damage |
| Phase E | `ElectricResistance` stat, `ApplyResistanceFor` | Three creatures show resistance/vulnerability/control |

No new infrastructure. Pure content + tests + scenario. Mirrors the
proven FlamingSword/IceSword pattern.

## Verification sweep — corrections table

| Premise | Status | Source |
|---|---|---|
| `Damage.IsElectricDamage()` returns true for "Electric", "Shock", "Lightning", "Electricity" | ✅ confirmed | `Damage.cs:124-128` |
| `ApplyDamage` calls `ApplyResistances` which routes Electric → ElectricResistance | ✅ confirmed | `CombatSystem.cs:649` |
| Resistance formula handles **negative** values: `damage += damage * (-resist / 100)` | ✅ confirmed | `CombatSystem.cs:644` (existing ResistanceTests prove it works) |
| BrassHusk blueprint exists at line 2851; current ER=0; Conductivity=0.95 | ✅ confirmed | `Objects.json:2851-2886` |
| StoneGolem blueprint exists at line 2706; current ER=0; Material=Stone | ✅ confirmed | `Objects.json:2706-2737` |
| Snapjaw has ColdResistance only (no ER); clean control for Lightning | ✅ confirmed | (verified earlier session) |
| `lightning_plus_conductor.json` material reaction is **cell-level** (electrifies cells via PropagateAlongTag); doesn't double-dip with creature ElectricResistance | ✅ confirmed | `Assets/Resources/Content/Data/MaterialReactions/lightning_plus_conductor.json` |
| Hit-log fix `2bbe31b` already applies to Electric (shared `ApplyResistanceFor`) | ✅ confirmed | trivially follows from the fix |

**No false premises detected.** Three thematic targets, all live in the
codebase, ready to wire up.

## Design choices

### ThunderHammer blueprint

| Field | Value | Rationale |
|---|---|---|
| Name | `ThunderHammer` | Symmetric with FlamingSword / IceSword |
| Inherits | `MeleeWeapon` | Same parent as the other weapons |
| Attributes | `"Bludgeoning Lightning Cudgel"` | Bludgeoning physical class + Lightning elemental + Cudgel sub-class. "Lightning" matches weapon name and `IsElectricDamage()` covers it. |
| BaseDamage | `1d8+1` | Heavier than the elemental swords' 1d8 — Bludgeoning weapons hit a bit harder. |
| PenBonus | `3` | Bludgeoning weapons get higher pen than slashers. Mirrors Mace (PenBonus 3). |
| Color | `&Y` (bright yellow) | Lightning theme; visually distinct from FlamingSword `&R` and IceSword `&C` |
| RenderString | `!` | Mace/hammer glyph (matches existing Mace and Warhammer) |
| Weight | `12` | Heavier than swords (6) — two-hander feel |
| Material | Steel + tags `Metal,Conductor,Lightning` | Mirrors elemental sword pattern; high conductivity is on-theme |
| Commerce.Value | `50` | Slightly pricier than 40g elemental swords (heavier hitter) |
| Tier tag | `2` | Same tier as the elemental swords |
| Hitpoints | `10` | Slightly tougher than swords (Bludgeoning weapons) |

### Resistance additions

| Creature | Current ER | New ER | Thematic justification |
|---|---:|---:|---|
| StoneGolem | 0 | **50** | Stone is non-conductive; lightning sheds off |
| BrassHusk | 0 | **−50** | Brass conducts at 0.95; lightning surges through, amplifying internal damage |

These are content additions to existing creatures — no schema changes.

## Sub-milestones (smallest blast radius first)

### T.1 — Plan + branch (this commit)

- Plan to disk (this file)
- Branch `feat/thunder-hammer` cut from `main` at `183fbd1`

### T.2 — RED + GREEN: ThunderHammer blueprint + tests (one commit)

- Add 3 blueprint-shape tests in `WeaponAttributesContentTests.cs`:
  - `ThunderHammer_HasBludgeoningLightningCudgelAttribute` (positive, exact-string)
  - `ThunderHammer_AttributesContain_Lightning` (Lightning substring pinned)
  - `ThunderHammer_DoesNotHaveCutting_OrPiercing` (counter-check)
- Add new file `ThunderHammerContentTests.cs` with 3 integration tests:
  - `ThunderHammer_AttributesViaCombatPath_TriggerElectricResistance` — synthetic damage halved on ER=50 target
  - `NonLightningDamage_OnElectricResistantTarget_NotReduced` — counter-check (no Lightning attribute → no reduction)
  - `ThunderHammer_OnElectricVulnerableTarget_AmplifiesDamage` — **new test pattern**: ER=−50 target takes 1.5× damage. Adds coverage for the vulnerability path.
- Add ElectricResistance to StoneGolem (50) and BrassHusk (−50)
- Add ThunderHammer blueprint to `Objects.json`
- Run RED → GREEN

### T.3 — ThunderHammerShowcase scenario (separate commit)

- New file `ThunderHammerShowcase.cs` mirroring `FlamingSwordShowcase`:
  - Player gets ThunderHammer equipped, Cudgel (control non-Lightning weapon) in inventory
  - Three targets:
    - **E**: StoneGolem (ER=50) — ThunderHammer halved
    - **NE**: BrassHusk (ER=−50) — ThunderHammer amplified by 50%
    - **SE**: Snapjaw (ER=0) — full damage (control)
- New `ThunderHammerDemoProbePart` that logs:
  - target name
  - pre-resistance amount
  - LIGHTNING / non-electric flag
  - live ElectricResistance value (including negative)
  - full attribute list
- Menu entry under Combat Stress at priority 108 (next after the elemental showcase)
- Smoke test in `ScenarioCustomSmokeTests.cs`

### T.4 — Self-review + commit + merge + push + roadmap update

- Severity-marked findings
- Update `Docs/CONTENT-ROADMAP.md`: flip ThunderHammer to ✅
- Two commits (T.2 then T.3) → merge `feat/thunder-hammer` to main → push

## Implementation log

(populated as work progresses)

## Self-review findings

(populated post-impl)
