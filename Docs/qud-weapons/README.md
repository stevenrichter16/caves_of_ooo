# Caves of Qud weapon schema + examples

A JSON Schema for Qud weapons (`qud-weapon.schema.json`) plus five real
examples (`examples.json`), derived **only** from the decompiled source at
`/Users/steven/qud-decompiled-project`. Every field and value below is traced
to a `file:line`. Nothing is invented.

## ⚠️ What the decompile does and does NOT contain (honesty bound)

- ✅ **It has the weapon Part code:** `XRL.World.Parts/MeleeWeapon.cs` and
  `XRL.World.Parts/MissileWeapon.cs`. These define the **field structure**
  (names, C# types, default values) of a Qud weapon — that's the schema.
- ✅ **It has weapon configs hardcoded in C#:** Part defaults, and natural
  weapons that mutations/effects build and configure in code (horns, beak,
  fungal-infected limbs, stingers, …). Those are the examples.
- ❌ **It has NO `ObjectBlueprints` XML / Data folder** (verified: `find … -iname
  "*.xml"` returns nothing). Qud's *named* weapons — Dagger, Long Sword, Battle
  Axe, etc. — and their per-blueprint `Render` (glyph/color), `Physics` (weight)
  and `Commerce` (value) live in that XML, which is **not in this decompile**.
  So those weapons' concrete stats are **not represented here, and were not
  fabricated.** The examples are the real weapon definitions that *do* live in
  the decompiled code.

## Schema provenance (`qud-weapon.schema.json`)

A weapon is a GameObject whose combat behavior is one of two Parts. The schema
discriminates on `weaponType` and embeds the matching Part's fields.

**Melee — `XRL.World.Parts/MeleeWeapon.cs`** (fields + defaults from its
`Reset()`, lines 23-57; `BONUS_CAP_UNLIMITED = 999` at line 21):

| Field | Type | Default | Line |
|---|---|---|---|
| `BaseDamage` | string | `"5"` | 29 |
| `Skill` | string | `"Cudgel"` | 33 |
| `Stat` | string | `"Strength"` | 35 |
| `Slot` | string | `"Hand"` | 37 |
| `PenBonus` | int | `0` | 25 |
| `HitBonus` | int | `0` | 27 |
| `MaxStrengthBonus` | int | `0` (999 = uncapped) | 23, 21 |
| `Ego` | int | `0` | 31 |
| `Attributes` | string? | `null` | 39 |

**Missile — `XRL.World.Parts/MissileWeapon.cs`** (field initializers, lines
25-59):

| Field | Type | Default | Line |
|---|---|---|---|
| `Skill` | string | `"Rifle"` | 59 |
| `Modifier` | string | `"Agility"` | 57 |
| `MaxRange` | int | `999` | 37 |
| `VariableMaxRange` | string? | `null` | 39 |
| `RangeIncrement` | int | `3` | 55 |
| `EnergyCost` | int | `1000` | 53 |
| `ShotsPerAction` | int | `1` | 27 |
| `AmmoPerAction` | int | `1` | 29 |
| `ShotsPerAnimation` | int | `1` | 31 |
| `AnimationDelay` | int | `10` | 25 |
| `AimVarianceBonus` | int | `0` | 33 |
| `WeaponAccuracy` | int | `0` | 35 |
| `AmmoChar` | string | `"ù"` | 41 |
| `NoWildfire` | bool | `false` | 43 |
| `ShowShotsPerAction` | bool | `true` | 45 |
| `FiresManually` | bool | `true` | 47 |
| `ProjectilePenetrationStat` | string? | `null` | 49 |
| `SlotType` | string | `"Missile Weapon"` | 51 |

## The 5 examples (`examples.json`) — per-example provenance

All five **validate against the schema** (Draft 2020-12, `jsonschema`).

1. **`(MeleeWeapon part defaults)`** — every field is the exact default from
   `MeleeWeapon.cs:23-57`. The baseline a weapon GameObject gets before its
   blueprint overrides anything.
2. **`horns (mutation, level 1)`** — `MultiHorns.cs:516-529` builds object
   `"Horns Single"` and sets `Skill="Cudgel"` (525), `MaxStrengthBonus=10`
   (526), `BaseDamage="2d3"` (529), `HitBonus=Max(Level-4,0)` ⇒ `0` at L1 (519).
   BaseDamage scales by level in the same block: 2d3→2d4→2d5→2d6→2d7→2d8 (L10+,
   line 574), HitBonus reaches 6 at L10. Fields not set by the code
   (`Stat`/`Slot`/`PenBonus`/`Ego`/`Attributes`) are shown as the Part defaults
   — the base `"Horns Single"` blueprint (XML, not in the decompile) may set
   `Slot` to a head slot.
3. **`beak (mutation)`** — `Beak.cs:64-71` builds `"Beak"` and sets
   `Skill="ShortBlades"` (68) and `BaseDamage="1"` (69). `Slot` is set to the
   attaching body part at runtime (`Slot = Part.Type`, line 70); shown here as
   the Part default. Other fields are Part defaults.
4. **`fungal-infected hand (FungalSporeInfection, non-wax variant)`** —
   `FungalSporeInfection.cs:231,243-246` (the `bodyPart.Type == "Hand"` branch,
   so `Slot="Hand"` is correct) sets `BaseDamage="1d4"`, `Skill="Cudgel"`,
   `PenBonus=0`, `MaxStrengthBonus=999` (= `BONUS_CAP_UNLIMITED`, i.e. uncapped
   Strength bonus). The wax-infection branch (236-239) instead uses
   `BaseDamage="2d3"`.
5. **`(MissileWeapon part defaults)`** — every field is the exact initializer
   default from `MissileWeapon.cs:25-59`. The baseline ranged-weapon config.

## Notes
- `BaseDamage` is a **string** in Qud — a die expression (`"2d3"`) or a flat
  integer (`"5"`, `"1"`). That's why the schema types it as `string`.
- `Skill` values seen in the decompile: `Cudgel`, `ShortBlades`, `LongBlades`
  (the stinger is described as "a long blade" in `Stinger.cs`), plus `Rifle`
  for missiles. The full skill list lives in XML/skill data not decompiled here.
- The stinger (`Stinger.cs:303-318`) is a real weapon too, but its
  `BaseDamage`/`PenBonus` come from venom-type- and level-specific
  `StingerProperties` (not constants), so it isn't a clean fixed-value example.

## Regenerate / validate
```
python3 -m pip install jsonschema
python3 -c "import json,jsonschema; s=json.load(open('qud-weapon.schema.json')); \
[jsonschema.Draft202012Validator(s).validate(e) for e in json.load(open('examples.json'))]; \
print('all valid')"
```
