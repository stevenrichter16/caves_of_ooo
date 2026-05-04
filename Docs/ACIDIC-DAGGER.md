# AcidicDagger — Acid-Carrier Piercing Weapon

> Tier-1 Quick Win. Fourth elemental weapon, completing the
> Fire/Ice/Lightning/Acid quartet. Branch: `feat/acidic-dagger`.

## Goal

Ship an `AcidicDagger` — a fast Piercing/Acid melee weapon. Adds
**AcidResistance** as the fourth elemental resistance surface to
in-game content, with the same resistance/vulnerability/control
trifecta the ThunderHammer scenario established for Lightning.

## User-visible invariant

"An AcidicDagger's swings produce damage tagged `Piercing Acid`.
CaveSlime's AcidResistance=+50 halves it (slime is chemically inert);
Scorpion's AcidResistance=−50 amplifies it 1.5× (acid eats through
chitin); Snapjaw (no AR) takes full damage. Three points on the Phase
E curve, visible per-hit via the AcidDemo probe."

## Phase mapping

| Phase | Surface | Used here |
|---|---|---|
| Phase C | `Damage.Attributes`, `MeleeWeaponPart.Attributes` reflective load | Carries "Acid" through to Damage |
| Phase E | `AcidResistance` stat, `ApplyResistanceFor` | Three creatures expose the curve |

Same shared infrastructure as Heat/Cold/Lightning. No new code paths.

## Verification sweep — corrections table

| Premise | Status | Source |
|---|---|---|
| `Damage.IsAcidDamage()` returns true for "Acid" attribute | ✅ confirmed | `Damage.cs:137-139` |
| `ApplyDamage` calls `ApplyResistances` which routes Acid → AcidResistance | ✅ confirmed | `CombatSystem.cs:646` |
| Negative-resistance formula (vulnerability) is shared with Heat/Cold/Lightning | ✅ confirmed | `CombatSystem.cs:644` (proven by ThunderHammer ship) |
| CaveSlime exists; current AR=0; tags `Organic,Wet`; HP 18, Tier 1 | ✅ confirmed | `Objects.json:2410-2439` |
| Scorpion exists; current AR=0; tags `Organic,Chitinous`; HP 10, Tier 1 | ✅ confirmed | `Objects.json:2522-2552` |
| `acid_plus_organic.json` material reaction is **cell-level** (Acidic surface state vs Organic tag) — doesn't double-dip with creature AR | ✅ confirmed | `Assets/Resources/Content/Data/MaterialReactions/acid_plus_organic.json` |
| Hit-log fix `2bbe31b` already applies to Acid (shared `ApplyResistanceFor`) | ✅ trivially follows | combat code path |

**No false premises detected.** Two thematic targets, ready to wire up.

## Design choices

### AcidicDagger blueprint

| Field | Value | Rationale |
|---|---|---|
| Name | `AcidicDagger` | Symmetric with FlamingSword/IceSword/ThunderHammer |
| Inherits | `MeleeWeapon` | Same parent |
| Attributes | `"Piercing Acid"` | Piercing physical class + Acid elemental. (Daggers don't have a sub-class like LongBlades or Cudgel — Dagger's existing Attributes is just "Piercing".) |
| BaseDamage | `1d4+1` | Slight upgrade over base Dagger's 1d4 — "fast but elemental" niche |
| PenBonus | `1` | Same as Dagger |
| MaxStrengthBonus | `3` | Same as Dagger — fast small weapon caps strength bonus |
| Color | `&G` (bright green) | Acid theme; visually distinct from other elemental weapons |
| RenderString | `/` | Dagger glyph (matches existing Dagger) |
| Weight | `4` | Same as Dagger — light |
| Material | Steel + tags `Metal,Acid` | Acid-themed materials |
| Commerce.Value | `30` | Lighter weapon, slightly cheaper than 40g elemental swords |
| Tier tag | `2` | Same as other elemental weapons |
| Hitpoints | `5` | Same as Dagger (light/fragile) |

### Resistance additions

| Creature | Current AR | New AR | Thematic justification |
|---|---:|---:|---|
| CaveSlime | 0 | **+50** | Slime/ooze is chemically inert, mostly water → acid washes off |
| Scorpion | 0 | **−50** | Chitinous exoskeleton dissolves in acid (Brittleness 0.3 already declared — chitin breaks down chemically) |

Content additions only — no schema changes.

## Sub-milestones (smallest blast radius first)

### A.1 — Plan + branch (this commit)

- Plan to disk (this file)
- Branch `feat/acidic-dagger` cut from `main` at `a952dcf`

### A.2 — RED + GREEN: AcidicDagger blueprint + tests + resistances (one commit)

- Add 3 blueprint-shape tests in `WeaponAttributesContentTests.cs`:
  - `AcidicDagger_HasPiercingAcidAttribute` (positive, exact-string)
  - `AcidicDagger_AttributesContain_Acid` (Acid substring pinned)
  - `AcidicDagger_DoesNotHaveCutting_OrBludgeoning` (counter-check)
- Add new file `AcidicDaggerContentTests.cs` with 5 integration tests:
  - `AcidicDagger_AttributesViaCombatPath_TriggerAcidResistance` — synthetic damage halved on AR=+50 target
  - `NonAcidDamage_OnAcidResistantTarget_NotReduced` — counter-check
  - `AcidicDagger_OnAcidVulnerableTarget_AmplifiesDamage` — AR=−50 amplifies 1.5×
  - `AcidicDagger_OnCaveSlime_TakesLessDamageThan_Control` — real blueprint
  - `AcidicDagger_OnScorpion_TakesMoreDamageThan_Control` — real vulnerability
- Add AcidicDagger blueprint to `Objects.json`
- Add AcidResistance to CaveSlime (+50) and Scorpion (−50)
- Run RED → GREEN

### A.3 — AcidicDaggerShowcase scenario (separate commit)

- New file `AcidicDaggerShowcase.cs` mirroring `ThunderHammerShowcase`:
  - Player gets AcidicDagger equipped, base Dagger (control non-Acid weapon) in inventory
  - Three targets:
    - **E**: CaveSlime (AR=+50) — halved
    - **NE**: Scorpion (AR=−50) — amplified 1.5×
    - **SE**: Snapjaw (AR=0) — full damage (control)
- New `AcidicDaggerDemoProbePart` that logs:
  - target name
  - pre-resistance amount
  - ACID / non-acid flag
  - live AcidResistance value (including negative)
  - full attribute list
- Menu entry under Combat Stress at priority 109
- Smoke test in `ScenarioCustomSmokeTests.cs`

### A.4 — Self-review + commit + merge + push + roadmap update

- Severity-marked findings
- Update `Docs/CONTENT-ROADMAP.md`: flip AcidicDagger + CaveSlime + Scorpion to ✅
- Two commits → merge `feat/acidic-dagger` to main → push

## Implementation log

(populated as work progresses)

## Self-review findings

(populated post-impl)
