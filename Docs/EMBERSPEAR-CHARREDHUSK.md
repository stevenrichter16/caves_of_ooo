# Tier 1 — EmberSpear + CharredHusk pair

**Status:** in progress
**Branch:** `feat/emberspear-charredhusk`
**Plan ref:** `Docs/CONTENT-ROADMAP.md` Tier 1 → Elemental weapons + Resistant creatures
**Methodology ref:** CLAUDE.md major-feature workflow (full pass)

## Goal

Mirror the CryoLance + IceWight pair on the Heat axis. Ship a
**Piercing/Fire spear** and a **Heat-immune, Cold-vulnerable creature**.
Same template as the four prior elemental weapon ships
(FlamingSword/Glowmaw, IceSword/SnapjawHunter,
ThunderHammer/StoneGolem+BrassHusk, AcidicDagger/CaveSlime+Scorpion,
CryoLance/IceWight).

## User-visible invariant

"EmberSpear damage on a Heat-resistant creature is reduced; on the new
**CharredHusk (HR=100)** it deals **zero damage** — the second 100%-immune
creature in the game (mirrors IceWight's CR=100). **Cold damage on a
CharredHusk is amplified 1.5×** by its negative ColdResistance — the
inverse of the IceWight × Fire interaction. EmberSpear is the second
Piercing+elemental weapon (CryoLance was the first; AcidicDagger is the
existing third in the matrix)."

## Scope

| In | Out |
|---|---|
| EmberSpear blueprint (`Piercing Fire`, 1d6+1, Burning on-hit) | Sub-class `Spears` or `Polearm` (none registered yet — match Spear's no-sub-class pattern) |
| CharredHusk blueprint (HR=100, CR=-50, mid-tier creature) | New AI behavior (defaults to wandering Brain) |
| Per-weapon attribute test (in `WeaponAttributesContentTests`) | Anything affecting StatusEffectsPart, OnHitClassEffects |
| End-to-end damage-routing test (in new `EmberSpearContentTests`) | Crit-rate / weapon-balance tuning |
| Resistance-presence + behavior tests (in `ResistanceStatsContentTests`) | LightSourcePart on EmberSpear (defer) |
| **Second "full immunity" pin** — EmberSpear on CharredHusk = 0 damage | New showcase menu entry crowding (extend / sibling — decide in M3) |
| **Second "negative-CR creature" pin** — Cold on CharredHusk = 1.5× damage | Faction config for "Husks" or similar (default-neutral is fine) |

## Verification sweep — premises confirmed

| Premise | Status | Source |
|---|---|---|
| `MeleeWeapon` blueprint shape (BaseDamage / PenBonus / Attributes / OnHitEffectsRaw) | ✅ | IceSword `Objects.json:2022-2052`, CryoLance `Objects.json:2061+` |
| Plain Spear blueprint shape (Piercing weapon, no sub-class) | ✅ | Spear `Objects.json:1311-1335` declares `Attributes: "Piercing"` only |
| `Stats` block supports `HeatResistance: 100` and `ColdResistance: -50` with `Min: -100, Max: 200` | ✅ | IceWight (now-shipped) declares these inverted; same Min/Max ranges work |
| `ApplyResistances` formula at `resist=100` → 0% damage, at `resist=-50` → 1.5× | ✅ | Existing `IceWight_ColdDamage_FullyNegated` test pins this for the Cold axis; same formula on Heat axis |
| `Damage.IsHeatDamage()` returns true for `Fire`/`Heat` attributes | ✅ | `Damage.cs` heat predicate; existing `FlamingSword_AttributesContain_Fire` test pins this |
| `OnHitEffectsRaw: "Burning,30,,5,1.0"` is the live wire format | ✅ | FlamingSword `OnHitEffectsRaw` line is `Burning,...` |
| `WeaponAttributesContentTests` per-weapon test row pattern | ✅ | `AcidicDagger_HasPiercingAcidAttribute`, `CryoLance_HasPiercingIceLongBladesAttribute` are the templates |
| `ResistanceStatsContentTests` per-creature presence + behavior pattern | ✅ | IceWight tests (just shipped) are the canonical template |
| `CryoLanceContentTests` is the e2e routing template; mirror line-for-line | ✅ | full pipeline test pattern proven |

**No false premises detected.**

### ⚠️ Existence-collision check (IceWight lesson)

| Name | Match in `Objects.json` |
|---|---|
| `EmberSpear` | ❌ none |
| `CharredHusk` | ❌ none |
| `AshHusk`, `CinderHusk` (alt names that might collide) | ❌ none |
| `EmberVein*` | ✅ `EmberVeinGrimoire` exists at line 4466 — **Item**, not MeleeWeapon, no collision |

Per IceWight collision lesson: the plan's "no X exists" must be backed by an actual grep, NOT memory. This pass cleared.

## Sub-milestones (smallest blast radius first)

### M1 — EmberSpear (one commit)

**RED tests first** in new `Assets/Tests/EditMode/Gameplay/Combat/EmberSpearContentTests.cs`:

1. `EmberSpear_BlueprintExists_AndIsMeleeWeapon` — sanity
2. `EmberSpear_HasPiercingFireAttribute` — exact string `"Piercing Fire"`
3. `EmberSpear_AttributesContain_Fire` — resilience to reorder
4. `EmberSpear_AttributesContain_Piercing` — physical class
5. `EmberSpear_DoesNotHaveCutting_OrBludgeoning` — counter-check
6. `EmberSpear_AttributesViaCombatPath_TriggerHeatResistance` — full pipeline
7. `NonFireDamage_OnHeatResistantTarget_NotReduced` — counter-check (no Fire attr = full damage)

**Add row to `WeaponAttributesContentTests.cs`:**
- `EmberSpear_HasPiercingFireAttribute` matching exact-string convention.

**Blueprint** (after CryoLance, before AcidicDagger):
```json
{
  "Name": "EmberSpear",
  "Inherits": "MeleeWeapon",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "ember spear" },
      { "Key": "RenderString", "Value": "/" },
      { "Key": "ColorString", "Value": "&R" },
      { "Key": "RenderLayer", "Value": "5" }
    ]},
    { "Name": "Physics", "Params": [{ "Key": "Takeable", "Value": "true" }, { "Key": "Weight", "Value": "6" }] },
    { "Name": "MeleeWeapon", "Params": [
      { "Key": "BaseDamage", "Value": "1d6+1" },
      { "Key": "PenBonus", "Value": "2" },
      { "Key": "Attributes", "Value": "Piercing Fire" },
      { "Key": "OnHitEffectsRaw", "Value": "Burning,30,,5,1.0" }
    ]},
    { "Name": "Commerce", "Params": [{ "Key": "Value", "Value": "35" }] },
    { "Name": "Material", "Params": [
      { "Key": "MaterialID", "Value": "Steel" },
      { "Key": "Combustibility", "Value": "0" },
      { "Key": "Conductivity", "Value": "0.8" },
      { "Key": "Brittleness", "Value": "0.2" },
      { "Key": "MaterialTagsRaw", "Value": "Metal,Conductor,Fire" }
    ]},
    { "Name": "Thermal", "Params": [
      { "Key": "FlameTemperature", "Value": "1500" },
      { "Key": "HeatCapacity", "Value": "0.5" },
      { "Key": "BrittleTemperature", "Value": "-150" }
    ]}
  ],
  "Stats": [
    { "Name": "Hitpoints", "Value": 8, "Min": 0, "Max": 8 }
  ],
  "Tags": [
    { "Key": "Tier", "Value": "2" }
  ]
}
```

**Damage value rationale:**
- `BaseDamage 1d6+1` (matches plain Spear's 1d6+1 — no boost; we trade up via OnHit Burning)
- `PenBonus 2` (matches plain Spear)
- `Attributes "Piercing Fire"` (no sub-class — matches Spear's pattern; CryoLance had `LongBlades` because of the lance/longblade hybrid framing, but a spear is just a spear)
- `OnHitEffectsRaw: Burning,30,,5,1.0` mirrors FlamingSword exactly
- `Commerce Value 35` (between Spear's 18 and CryoLance's 45)
- Glyph `/` color `&R` (red, mirrors FlamingSword)
- `Tier: 2` (matches CryoLance/AcidicDagger)

### M2 — CharredHusk (one commit)

**RED tests first** in `ResistanceStatsContentTests.cs`:
1. `CharredHusk_BlueprintExists_AndIsCreature` — sanity
2. `CharredHusk_HasHeatResistance100` — full Fire immunity (second 100%-immune creature)
3. `CharredHusk_HasColdResistance_NegativeFifty` — Cold vulnerability
4. `CharredHusk_FireDamage_FullyNegated` — behavioral pin (HR=100 → 0 damage)
5. `CharredHusk_ColdDamage_AmplifiedByNegativeResistance` — behavioral pin (CR=-50 → 1.5× damage)
6. `CharredHusk_AcidDamage_FullDamage_NoMatchingResist` — counter-check (other elements unaffected)

**Add to `EmberSpearContentTests.cs`:**
7. `EmberSpear_OnCharredHusk_DealsZeroDamage` — the canonical second 100%-immunity pin
8. `EmberSpear_OnGlowmaw_TakesLessDamageThan_ControlTarget` — sanity / direction check on the existing HR=50 creature

**Blueprint** (insert near the existing IceWight or BrassHusk, themed-undead-construct neighborhood):
```json
{
  "Name": "CharredHusk",
  "Inherits": "Creature",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "charred husk" },
      { "Key": "RenderString", "Value": "H" },
      { "Key": "ColorString", "Value": "&r" }
    ]},
    { "Name": "Armor", "Params": [{ "Key": "AV", "Value": "3" }, { "Key": "DV", "Value": "1" }] },
    { "Name": "Material", "Params": [
      { "Key": "MaterialID", "Value": "CharredFlesh" },
      { "Key": "Combustibility", "Value": "0" },
      { "Key": "MaterialTagsRaw", "Value": "Charred,Undead,Brittle" }
    ]},
    { "Name": "Thermal", "Params": [
      { "Key": "Temperature", "Value": "120" },
      { "Key": "FlameTemperature", "Value": "9000" },
      { "Key": "FreezeTemperature", "Value": "-50" },
      { "Key": "BrittleTemperature", "Value": "-100" },
      { "Key": "HeatCapacity", "Value": "2.0" },
      { "Key": "AmbientDecayRate", "Value": "0.0" }
    ]}
  ],
  "Stats": [
    { "Name": "Hitpoints", "Value": 24, "Min": 0, "Max": 24 },
    { "Name": "Strength", "Value": 16 },
    { "Name": "Agility", "Value": 8 },
    { "Name": "Speed", "Value": 70 },
    { "Name": "XPValue", "Value": 35 },
    { "Name": "HeatResistance", "Value": 100, "Min": -100, "Max": 200 },
    { "Name": "ColdResistance", "Value": -50, "Min": -100, "Max": 200 }
  ],
  "Tags": [
    { "Key": "Tier", "Value": "2" },
    { "Key": "BloodColor", "Value": "&K" }
  ]
}
```

**Stats rationale:**
- HP 24 / Strength 16 / Agility 8 / Speed 70 / XPValue 35 — **identical to IceWight** (the ice/fire pair share the slow-undead profile by design — they're mirrors)
- AV 3 / DV 1 — matches IceWight's defensive profile
- HeatResistance 100 (immunity), ColdResistance -50 (vulnerability) — inverted from IceWight (CR=100, HR=-50)
- Material `CharredFlesh` (new id, no shared blueprint) — combustibility 0 (already burned), tags `Charred,Undead,Brittle` (mirrors IceWight's `Ice,Undead,Brittle`)
- Thermal `Temperature: 120` (warm corpse), `FlameTemp 9000` (won't re-ignite — already charred), `BrittleTemp -100` (gets brittle if frozen, in line with the cold-vulnerability theme), `HeatCapacity 2.0`, `AmbientDecayRate 0.0`
- BloodColor `&K` (charred ash-black; IceWight is `&C` cyan)
- No Faction tag (default-neutral; same as IceWight)

### M3 — Showcase + smoke (one commit)

**Decision:** Create new `EmberSpearShowcase` scenario as a sibling to `CryoLanceShowcase`. Same shape, swap weapons + targets:

```
            [Glowmaw NE: HR=50 — graded contrast]
                         ↗
[Player] →→ [CharredHusk E: HR=100, CR=-50 — extremes]
                         ↘
            [SnapjawHunter SE: CR=50 — Cold-resist contrast]
```

Player loadout: EmberSpear equipped, IceSword in inventory (the secondary
to demonstrate Cold-vulnerability flip).

**Add menu entry** at priority 114 (after CryoLanceShowcase=113).
**Add smoke test** to `ScenarioCustomSmokeTests.cs`.

## Test plan summary

| Suite | New tests | Add-on rows |
|---|---|---|
| `EmberSpearContentTests` (NEW) | 9 | — |
| `WeaponAttributesContentTests` | — | +1 row |
| `ResistanceStatsContentTests` | 6 | — |
| `ScenarioCustomSmokeTests` | 1 | — |

**Total: 16 new tests + 1 row.** Mirrors the CryoLance ship (15 + 1).

## Performance section

Per CLAUDE.md Performance triggers: **none apply.**

- ❌ No render hook (existing weapon/creature render path)
- ❌ No hot-path allocations (one-shot blueprint construction)
- ❌ No new cache
- ❌ No new MonoBehaviour
- ❌ No per-frame listener

Does not require a Performance section.

## Pre-flagged self-review

- 🔵 EmberSpear has no sub-class (`"Piercing Fire"`) while CryoLance has
  `"LongBlades"`. Asymmetry is **intentional**: a spear isn't a longblade.
  This adds a useful test variant — Piercing+Fire WITH and WITHOUT
  sub-class side by side.
- 🔵 CharredHusk's stat profile mirrors IceWight (HP 24, Str 16, Agi 8,
  Speed 70). Deliberate — they're a pair. Future tuning can diverge them.
- 🔵 No Faction tag means default-neutral. Showcase uses
  `.AsPersonalEnemyOf(player)` to override. Future ship can add a
  Husks/Wights faction config.
- 🔵 OnHitEffectsRaw `Burning,30,...` on EmberSpear is irrelevant when
  hitting CharredHusk (already heat-immune; Burning OnApply on a
  fully-heat-immune creature might no-op or might still apply the
  effect, depending on whether `Effect.CanApply` checks resistance).
  **Out of scope**; if it becomes a perceived issue, add an explicit
  `BurningEffect.CanApply` check that respects HR≥100.
- ⚪ EmberSpear and CharredHusk share `Material: Charred`/`Fire` tagging
  for visual consistency.
- ⚪ Damage values: 1d6+1 PenBonus 2 ≈ same DPR as the plain Spear, plus
  Fire elemental routing and 30% Burning on-hit. Less raw than CryoLance
  (1d6+2 PenBonus 3) by design — Fire's payoff is the on-hit DOT, not
  upfront damage.

## Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ✅ | this commit |
| Verification sweep | ✅ | EmberSpear/CharredHusk both confirmed absent; EmberVeinGrimoire is unrelated Item |
| Branch cut | ✅ | feat/emberspear-charredhusk from main dca9078 |
| M1: EmberSpear blueprint + tests | ✅ | 9 tests in EmberSpearContentTests + 1 row in WeaponAttributesContentTests; intermediate sweep 8/9 GREEN (1 RED waiting on M2) |
| M2: CharredHusk blueprint + tests | ✅ | 6 tests in ResistanceStatsContentTests; intermediate sweep 66/66 GREEN |
| M3: Showcase + menu + smoke | ✅ | EmberSpearShowcase.cs (3-creature layout) + menu priority 114 + smoke test |
| Live test sweep via /tmp/mcp-call.sh | ✅ | **210/210 GREEN** across 12 fixtures (0.378s) |
| Self-review | ✅ | 🔵 No sub-class on EmberSpear (matches Spear pattern; deliberate asymmetry vs CryoLance's LongBlades). 🔵 CharredHusk stat profile mirrors IceWight (HP 24 / Str 16 / Agi 8 / Speed 70). 🔵 No Faction tag (default-neutral). 🔵 OnHit Burning on a heat-immune CharredHusk irrelevant — out of scope. |
| Roadmap update | ✅ | flipped EmberSpear + CharredHusk 💡 → ✅; added Recently Shipped row |
| Merged to main | ⏳ | (pending commit) |
