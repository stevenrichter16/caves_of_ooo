# Tier 1 — CryoLance + IceWight pair

**Status:** in progress
**Branch:** `feat/cryolance-icewight`
**Plan ref:** `Docs/CONTENT-ROADMAP.md` Tier 1 → Elemental weapons + Resistant creatures

## Goal

Ship two paired Tier-1 content items: a **Piercing/Ice longblade-piercer hybrid weapon** and a **Cold-immune, Fire-vulnerable creature**. Same template as the four prior elemental-weapon ships (FlamingSword/Glowmaw, IceSword/SnapjawHunter, ThunderHammer/StoneGolem+BrassHusk, AcidicDagger/CaveSlime+Scorpion).

## User-visible invariant

"CryoLance damage on a Cold-resistant creature is reduced; on the new IceWight (CR=100) it deals **zero damage**. Fire damage on an IceWight is **amplified 1.5×** by its negative HeatResistance — the inverse of what FlamingSword does on a Glowmaw. CryoLance is the first **Piercing-class elemental** weapon (the existing 4 are Cutting × Fire/Ice/Bludgeoning, AcidicDagger is Piercing/Acid)."

## Scope

| In | Out |
|---|---|
| CryoLance blueprint (`Piercing Ice LongBlades`, 1d6+2, Frozen on-hit) | EmberSpear (separate ship) |
| IceWight blueprint (CR=100, HR=-50, mid-tier creature) | CharredHusk variant (separate ship) |
| Per-weapon attribute test (in `WeaponAttributesContentTests`) | Anything affecting StatusEffectsPart, OnHitClassEffects |
| End-to-end damage-routing test (in new `CryoLanceContentTests`) | Crit-rate / weapon-balance tuning |
| Resistance-presence test (in `ResistanceStatsContentTests`) | New AI behavior for IceWight |
| **First "full immunity" pin** — CryoLance on IceWight = 0 damage | LightSourcePart on IceWight (defer) |
| **First "negative-HR creature" pin** — Fire on IceWight = 1.5× damage | New showcase menu entry (extend ElementalSwordsShowcase if cheap) |

## Verification sweep — premises confirmed

| Premise | Status | Source |
|---|---|---|
| `MeleeWeapon` blueprint shape (BaseDamage / PenBonus / Attributes / OnHitEffectsRaw) | ✅ | IceSword `Objects.json:2022-2052` |
| `Inherits: MeleeWeapon` works for Piercing weapons too | ✅ | Spear `Objects.json:1311-1335` (1d6+1, `Attributes: "Piercing"`) |
| `OnHitEffectsRaw` format `"Frozen,30,,3,1.0"` is the live wire format | ✅ | IceSword `OnHitEffectsRaw` line + `OnHitEffectsRaw,...,Magnitude` parsed by `OnHitEffectSpec.Parse` |
| `Stats` block supports `ColdResistance` with `Min: -100, Max: 200` | ✅ | BrassHusk `Objects.json:3054` (`ElectricResistance: -50`), Snapjaw `Objects.json:171` (`ColdResistance: 25`) |
| `ApplyResistances` formula `damage *= (100 - resist) / 100` floors at 0 | ✅ → at `resist = 100`: `damage *= 0` ⇒ 0 damage. At `resist = -50`: `damage *= 1.5` |
| `Damage.IsColdDamage()` returns true if attributes contain `Ice`/`Cold`/`Freeze` | ✅ | `Damage.cs:140-150` (existing test pins this for IceSword) |
| `WeaponAttributesContentTests` has a per-weapon row pattern (e.g., `IceSword_HasCuttingIceLongBladesAttribute`) | ✅ | `WeaponAttributesContentTests.cs:138+` |
| `ResistanceStatsContentTests` has per-creature presence-test pattern | ✅ | `ResistanceStatsContentTests.cs:47-65` |
| `IceSwordContentTests` is the e2e damage-routing template | ✅ | full pipeline test at line 50 |

**No false premises detected.** No `IceWight` blueprint exists today; no `CryoLance` blueprint exists today. Path is clear.

## Sub-milestones (smallest blast radius first)

### M1 — CryoLance (one commit)

**RED tests** (in new `Assets/Tests/EditMode/Gameplay/Combat/CryoLanceContentTests.cs`):
1. `CryoLance_BlueprintExists_AndIsMeleeWeapon` — sanity
2. `CryoLance_HasPiercingIceLongBladesAttribute` — exact string
3. `CryoLance_AttributesContain_Ice` — resilience to reorder
4. `CryoLance_AttributesContain_Piercing` — physical class
5. `CryoLance_DoesNotHaveCutting_OrBludgeoning` — counter-check
6. `CryoLance_AttributesViaCombatPath_TriggerColdResistance` — full pipeline (mirror IceSword test)
7. `NonIceDamage_OnColdResistantTarget_NotReduced` — counter-check (no Ice attribute = full damage)

**Add to `WeaponAttributesContentTests.cs`:**
- One row matching the existing pattern (test that CryoLance Attributes string is exactly `"Piercing Ice LongBlades"`).

**Blueprint** in `Objects.json` (insert after IceSword):
```json
{
  "Name": "CryoLance",
  "Inherits": "MeleeWeapon",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "cryolance" },
      { "Key": "RenderString", "Value": "/" },
      { "Key": "ColorString", "Value": "&C" },
      { "Key": "RenderLayer", "Value": "5" }
    ]},
    { "Name": "Physics", "Params": [{ "Key": "Takeable", "Value": "true" }, { "Key": "Weight", "Value": "5" }] },
    { "Name": "MeleeWeapon", "Params": [
      { "Key": "BaseDamage", "Value": "1d6+2" },
      { "Key": "PenBonus", "Value": "3" },
      { "Key": "Attributes", "Value": "Piercing Ice LongBlades" },
      { "Key": "OnHitEffectsRaw", "Value": "Frozen,30,,3,1.0" }
    ]},
    { "Name": "Commerce", "Params": [{ "Key": "Value", "Value": "45" }] },
    { "Name": "Material", "Params": [
      { "Key": "MaterialID", "Value": "Steel" },
      { "Key": "Combustibility", "Value": "0" },
      { "Key": "Conductivity", "Value": "0.8" },
      { "Key": "Brittleness", "Value": "0.2" },
      { "Key": "MaterialTagsRaw", "Value": "Metal,Conductor,Ice" }
    ]},
    { "Name": "Thermal", "Params": [
      { "Key": "FlameTemperature", "Value": "1500" },
      { "Key": "HeatCapacity", "Value": "0.5" },
      { "Key": "BrittleTemperature", "Value": "-150" }
    ]}
  ],
  "Stats": [
    { "Name": "Hitpoints", "Value": 9, "Min": 0, "Max": 9 }
  ],
  "Tags": [
    { "Key": "Tier", "Value": "2" }
  ]
}
```

Damage value pick: `1d6+2` (matches roadmap). PenBonus 3 (high penetration vs IceSword's 2 — the lance is the premium piercer). Slightly lighter (Weight 5 vs IceSword's 6) since lances are slimmer than swords. Tier 2 commerce-value 45 (between Spear's 18 and Greatsword's 60).

### M2 — IceWight (one commit)

**RED tests** (additions to `ResistanceStatsContentTests.cs`):
1. `IceWight_HasColdResistance100` — full Cold immunity (the new pin: first creature with 100% elemental resistance)
2. `IceWight_HasHeatResistance_NegativeFifty` — Fire vulnerability
3. `IceWight_BlueprintExists_AndIsCreature` — sanity (verify Inherits works)

**Add to `CryoLanceContentTests.cs`:**
4. `CryoLance_OnIceWight_DealsZeroDamage` — **the canonical 100%-immunity pin.** First end-to-end test of the resistance ≥ 100 = total negation path.
5. `IceWight_TakesExtraDamage_FromFireDamage` — Fire damage on negative-HR creature deals 1.5× (mirrors BrassHusk/ThunderHammer test).

**Blueprint** in `Objects.json` (insert in the creature section, near other elementally-themed creatures):
```json
{
  "Name": "IceWight",
  "Inherits": "Creature",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "ice wight" },
      { "Key": "RenderString", "Value": "W" },
      { "Key": "ColorString", "Value": "&C" }
    ]},
    { "Name": "Armor", "Params": [{ "Key": "AV", "Value": "1" }, { "Key": "DV", "Value": "2" }] },
    { "Name": "Material", "Params": [
      { "Key": "MaterialID", "Value": "Ice" },
      { "Key": "Combustibility", "Value": "0" },
      { "Key": "MaterialTagsRaw", "Value": "Cold,Ice,Crystalline" }
    ]},
    { "Name": "Thermal", "Params": [
      { "Key": "FlameTemperature", "Value": "200" },
      { "Key": "HeatCapacity", "Value": "1.5" },
      { "Key": "FreezeTemperature", "Value": "-20" },
      { "Key": "BrittleTemperature", "Value": "-200" },
      { "Key": "AmbientTemperature", "Value": "0" }
    ]}
  ],
  "Stats": [
    { "Name": "Hitpoints", "Value": 25, "Min": 0, "Max": 25 },
    { "Name": "Strength", "Value": 14 },
    { "Name": "Agility", "Value": 16 },
    { "Name": "Toughness", "Value": 14 },
    { "Name": "Speed", "Value": 90 },
    { "Name": "XPValue", "Value": 25 },
    { "Name": "ColdResistance", "Value": 100, "Min": -100, "Max": 200 },
    { "Name": "HeatResistance", "Value": -50, "Min": -100, "Max": 200 }
  ],
  "Tags": [
    { "Key": "Faction", "Value": "Wights" },
    { "Key": "Tier", "Value": "2" }
  ]
}
```

### M3 — Showcase (one commit)

**Decision:** Extend `ElementalSwordsShowcase` to include CryoLance + IceWight if the existing scenario layout supports a fourth row cleanly. If not, create a new `CryoLanceShowcase` scenario.

Read `ElementalSwordsShowcase.cs` first; small extension preferred over new menu entry.

Add smoke test to `ScenarioCustomSmokeTests`.

## Test plan summary

- M1: 7 new tests in `CryoLanceContentTests` + 1 row in `WeaponAttributesContentTests`
- M2: +3 tests in `ResistanceStatsContentTests` + 2 more end-to-end in `CryoLanceContentTests`
- M3: 1 smoke test

Total ≈ 13 new tests + 1 row addition.

## Performance section

Per CLAUDE.md Performance triggers: **none apply.**

- ❌ No render hook (existing weapon/creature render path)
- ❌ No hot-path allocations (one-shot blueprint construction)
- ❌ No new cache (uses existing PhasePart caches)
- ❌ No new MonoBehaviour
- ❌ No per-frame listener

Does not require a Performance section.

## Pre-flagged self-review

- 🔵 IceWight has no AI behavior beyond the default Brain wandering. No `Faction: Wights` faction config exists today (need to verify in M2). If absent, defer faction setup to a separate ship — for M2 use `Faction: Snapjaws` or a test-only tag.
- 🔵 IceWight's HP=25 / DV=2 is mid-tier; might be too hard for early-game player. Tunable in playtest.
- 🔵 No corpse/loot tables for IceWight. Inherits `Creature` defaults (drops `CreatureCorpse`). Could later add a thematic IceShard drop — out of scope.
- 🔵 `OnHitEffectsRaw` Frozen on CryoLance mirrors IceSword's; on IceWight the on-hit Frozen is irrelevant (IceWight is already cold-immune, Frozen.OnApply on a cold-immune creature should no-op). Worth verifying — but defer; not in scope for this ship.
- ⚪ CryoLance and IceWight share the `Material: Ice` themed tag — visually consistent. Future ice-themed content can hook on this.
- ⚪ Damage values picked to match the elemental-weapon tier: 1d6+2 base × PenBonus 3 ≈ same average as IceSword's 1d8/PenBonus 2. Lance trades a smaller die for higher penetration.

## Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ✅ | this commit |
| Verification sweep | ✅ | IceSword/Spear/Snapjaw/BrassHusk templates confirmed |
| Branch cut | ✅ | feat/cryolance-icewight from main 8dcb682 |
| M1: CryoLance blueprint + tests | ✅ | 7 tests in CryoLanceContentTests + 1 row in WeaponAttributesContentTests |
| M2: IceWight blueprint + tests | ✅ | 6 tests in ResistanceStatsContentTests + 2 e2e in CryoLanceContentTests |
| M3: Showcase + smoke | ✅ | CryoLanceShowcase.cs (3-creature layout: IceWight, SnapjawHunter, Glowmaw) + smoke test + menu entry priority 113 |
| Self-review | ✅ | 🔵 IceWight uses unknown Faction:Wights — FactionManager.GetFactionFeeling returns 0/neutral for unknown factions; safe but means no auto-aggro. 🔵 IceWight balance (HP 25/DV 2/AV 1) tunable in playtest. ⚪ No corpse/loot table beyond Creature default. |
| Roadmap update | ✅ | flipped CryoLance + IceWight 💡 → ✅; added Recently Shipped entry |
| Merged to main | ⏳ | (pending commit) |
