# FlamingSword — First Elemental-Carrier Weapon

> Tier-1 Quick Win content ship. Per CLAUDE.md major-feature workflow.
> Branch: `feat/flaming-sword`. Status: in progress.

## Goal

Ship a `FlamingSword` melee weapon whose attacks carry the `Fire`
damage attribute. This makes Phase E (HeatResistance) **player-visible**:
swinging the FlamingSword at a Glowmaw (HeatResistance=50) deals roughly
half damage compared to a control target without the resistance.

## User-visible invariant

"A FlamingSword's swings produce damage tagged `Cutting Fire LongBlades`.
Glowmaw's HeatResistance halves Fire-attributed damage. So the same
weapon that two-shots a snapjaw will need ~twice as many hits on a
glowmaw — the player has to think about which weapon to bring."

## Phase mapping

| Phase | Surface | Used here |
|---|---|---|
| Phase C | `Damage.Attributes` list, `MeleeWeaponPart.Attributes` reflective load | Carries "Fire" through to Damage |
| Phase E | `HeatResistance` stat, `ApplyResistanceFor` in CombatSystem | Glowmaw halves the damage |

No new infrastructure. Pure content + tests.

## Scope

### In-scope
- `FlamingSword` blueprint in `Objects.json` (Inherits MeleeWeapon, adds
  Render, Physics, MeleeWeapon w/ Fire attribute, Commerce, Material, Thermal)
- Blueprint-shape tests (Attributes string is correct)
- Integration test: damage tagged Fire applied to Glowmaw blueprint
  is reduced by HeatResistance

### Out-of-scope
- Lighting/visuals (FlamingSword does not light tiles around it — that
  would need `LightSource` part wiring; deferred)
- Persistent flame status on hit (that would need a `BurningEffect`
  application path off the weapon — deferred)
- Loot table placement (FlamingSword exists in the registry but isn't
  spawned in any zone yet — deferred to a content-placement pass)
- Non-cumulative resistance interaction (the existing ResistanceTests
  cover the formula — we just verify the weapon participates)

## Verification sweep — corrections table

| Premise | Status | Source |
|---|---|---|
| `MeleeWeaponPart.Attributes` is a string field, reflection-loaded from JSON | ✅ confirmed | `MeleeWeaponPart.cs:47` |
| `Damage.IsHeatDamage()` returns true for "Fire" or "Heat" attribute | ✅ confirmed | `Damage.cs:118-120` |
| `ApplyDamage` calls `ApplyResistances` which routes Heat → HeatResistance | ✅ confirmed | `CombatSystem.cs:534, 616-619` |
| Glowmaw blueprint has `HeatResistance: 50` | ✅ confirmed | `Objects.json:2403` |
| `MeleeWeapon` parent blueprint inherits from `Item`, gives `Physics.Category="Melee Weapons"`, `Equippable.Slot="Hand"`, `Handling.GripType="OneHand"` | ✅ confirmed | `Objects.json:62-74` |
| Resistance formula: `damage *= (100 - resist) / 100` with min-1 clamp if resist<100 | ✅ confirmed | `CombatSystem.cs:633-639` |
| Existing `WeaponAttributesContentTests` shape uses `_harness.Factory.CreateEntity("Name").GetPart<MeleeWeaponPart>()` and asserts `weapon.Attributes` string match | ✅ confirmed | `WeaponAttributesContentTests.cs:51-86` |
| Sporeblade has NO `Attributes` declared — pre-existing latent gap | ⚠️ noted, not fixed (out of scope) | `Objects.json:1869` |

**No false premises detected.** All assumed plumbing exists and behaves
as expected.

## Sub-milestones (smallest blast radius first)

### F.1 — Plan + branch (this commit)
- Plan to disk (this file)
- Branch `feat/flaming-sword` cut from main

### F.2 — RED tests + GREEN blueprint (one commit)
- Write blueprint-shape tests in `WeaponAttributesContentTests.cs`:
  - `FlamingSword_HasCuttingFireLongBladesAttribute` (positive)
  - `FlamingSword_AttributesContain_Fire` (explicit Fire assertion — pins
    the elemental-carrier role)
  - `FlamingSword_DoesNotHavePiercing_OrBludgeoning` (counter-check)
- Run RED — confirms `CreateEntity("FlamingSword")` returns null (blueprint missing)
- Add FlamingSword blueprint to Objects.json
- Run GREEN — all three pass

### F.3 — Adversarial integration test (one commit)
- New test in `ResistanceTests.cs` (or a new file): verify the
  full chain — Damage tagged "Fire Cutting" applied to a real Glowmaw
  loaded from Objects.json is reduced by HeatResistance=50.
- Counter-check: same Damage minus the "Fire" attribute is NOT reduced.

### F.4 — Self-review + commit
- Severity-marked findings
- Commit per §2.3 template
- Merge to main + push

## Implementation log

### F.1 — plan + branch — DONE

- Plan written (this file)
- Branch `feat/flaming-sword` cut from `main` at `19a9b4d`

### F.2 — RED tests + GREEN blueprint — DONE (verification gated by Unity)

**RED tests written** (in `WeaponAttributesContentTests.cs`):
- `FlamingSword_HasCuttingFireLongBladesAttribute` — exact-string match
- `FlamingSword_AttributesContain_Fire` — explicit `Fire` substring
- `FlamingSword_DoesNotHavePiercing_OrBludgeoning` — physical-class counter-check

**RED state — verified by inspection only.** Unity MCP fell into the
deferred-domain-reload trap (`editor.is_focused: false` → repeated session
disconnects on every tool call). Per CLAUDE.md §7.2 the only fix is for
the user to focus the editor window. Inspection-level RED is robust here:
without the blueprint, `_harness.Factory.CreateEntity("FlamingSword")`
returns null, and the very first `Assert.IsNotNull(sword, "FlamingSword
blueprint must exist")` fails — there is no path through these tests that
passes with a missing blueprint.

**GREEN blueprint added** (`Objects.json:1893-1928`, between Sporeblade
and EchoKnife). Mirrors the ShortSword shape exactly; only differences:
- Color `&R` (red) instead of `&w` (white) — visual signal of fire affinity
- `BaseDamage 1d8` (vs 1d6) and `PenBonus 2` (vs 1) — Tier-2 stats
- `Attributes "Cutting Fire LongBlades"` — adds the `Fire` elemental
- `Material.MaterialTagsRaw "Metal,Conductor,Fire"` — adds `Fire` so the
  material reaction system will treat it as a flame source if anything
  in MaterialReactionResolver hooks on weapon material tags
- `Commerce.Value 40` (vs 15) — Tier-2 pricing, sits next to Sporeblade (35g)

**JSON validated** via `python3 -c "import json; json.load(...)"` — parses
clean, no syntax errors.

### F.3 — adversarial integration tests — DONE (verification gated by Unity)

**Tests written** (new file `FlamingSwordContentTests.cs`):
- `FlamingSword_AttributesViaCombatPath_TriggerHeatResistance` — synthesizes
  a `Damage(20)` exactly the way `CombatSystem.PerformSingleAttack`
  does (Melee + Stat + AddAttributes(weapon.Attributes)), applies to a
  fighter with HeatResistance=50, asserts HP delta is 10 (not 20).
- `NonFireDamage_OnHeatResistantTarget_NotReduced` — counter-check:
  identical-shape damage MINUS the `Fire` attribute → full 20 lands.
  Without this, the positive assertion above could pass for the wrong
  reason.
- `FlamingSword_OnGlowmaw_TakesLessDamageThan_ControlTarget` — thematic
  pairing: real Glowmaw blueprint (HeatResistance=50, AV=2) vs a control
  fighter. Asserts direction (less damage on Glowmaw), not exact magnitude
  — Glowmaw also has Armor, so exact numbers are noisy. The `Less` check
  is robust to that.

### F.4 — self-review + commit — IN PROGRESS

See self-review section below.

## In-phase self-review (Methodology Template §5)

### 🔵 Finding 1 — Unity test verification deferred to post-commit

**File:** all of this branch
**Severity:** 🔵 (blue — observation, not a bug)

Unity's MCP plugin is in the `editor.is_focused: false` deferred-reload
trap (CLAUDE.md §7.2). Force-refresh times out. The implementation
correctness is high-confidence by inspection (mirrors ShortSword shape,
Attributes string is `"Cutting Fire LongBlades"`, JSON parses clean,
test assertions cannot pass with the blueprint absent), but the
methodology's RED→GREEN cycle is verified by reasoning rather than
by running tests.

**Why it matters:** if a typo crept in (e.g., `"cutting Fire LongBlades"`
with lowercase c), the case-sensitive `Damage.HasAttribute("Fire")` check
would still trigger HeatResistance correctly, but the
`Assert.AreEqual("Cutting Fire LongBlades", weapon.Attributes)` would
fail. Inspection caught this risk and the literal string above is correct.

**Proposed fix:** when Unity recovers, run the full EditMode suite on
this commit. If anything fails, ship a follow-up commit per §3.9.5.
Pattern matches the `StoneskinTonic` ship from the previous session.

### ⚪ Finding 2 — FlamingSword has no LightSource part

**File:** `Objects.json:1892-1928`
**Severity:** ⚪ (deferred / scope-pruned)

A "flaming" sword that doesn't actually emit light feels off thematically.
Adding a `LightSource` part (radius=2, color=`&R`, intensity=0.5) would
make it player-visible in dim zones. However:
- `LightSource` on a held item raises questions about whether the light
  follows the wielder (probably yes, since LightSource is on the entity
  graph), but I haven't read that code path
- This is scope creep for a "Tier-1 Quick Win" content ship
- The plan called this out in § "Out-of-scope"

**Proposed fix:** future content commit `feat(items): FlamingSword
emits light when held` once the wielder-light propagation is verified.

### ⚪ Finding 3 — No `BurningEffect` application on hit

**File:** `Objects.json:1892-1928`
**Severity:** ⚪ (deferred / scope-pruned)

A real flaming sword should occasionally light its target on fire.
The `BurningEffect` exists. The path would be a `BeforeApplyDamage`-style
hook (Phase F) that, on Fire-attributed melee damage, rolls to apply
`BurningEffect`. This is a meaningful gameplay layer but:
- Requires a new `WeaponEffectPart` or similar abstraction
- Tier-1 Quick Win was scoped to "blueprint + tests"
- The plan called this out in § "Out-of-scope"

**Proposed fix:** future tier — see if `MeleeWeaponPart` should grow
an `OnHitEffects` list (probability + EffectName + Magnitude tuples).
Could pair with a similar `Cudgel` knockback or `Dagger` poison-tip.

### ✅ Pre-commit checklist

- [x] Diff introduces production behavior (the blueprint) AND a test diff
  in the same commit covering it (4 new tests).
- [x] Tests would have failed RED before the blueprint addition —
  verified by inspection (every test starts with `Assert.IsNotNull(sword)`
  on `CreateEntity("FlamingSword")` which returns null without the
  blueprint).
- [x] Counter-checks present:
  - `FlamingSword_DoesNotHavePiercing_OrBludgeoning` (vs the
    physical-class assertion)
  - `NonFireDamage_OnHeatResistantTarget_NotReduced` (vs the
    "Fire-tagged damage is halved" assertion)
- [x] No magic numbers — all values are blueprint params with self-explanatory keys.
- [x] No docstring claims unbacked by tests — the `Attributes` string is
  pinned by a literal-equality test.
- [x] No "Qud parity" overclaims — this is CoO-original content (no Qud
  weapon called FlamingSword).
- [x] Public API unchanged.
- [x] Verification sweep ran clean (8 premises, 0 false). Documented in
  the plan.

## Files changed

| State | Path | Purpose |
|---|---|---|
| NEW | `Docs/FLAMING-SWORD.md` | Plan + sweep + sub-milestone log + self-review |
| MOD | `Assets/Resources/Content/Blueprints/Objects.json` | +37 lines, new FlamingSword blueprint between Sporeblade and EchoKnife |
| MOD | `Assets/Tests/EditMode/Gameplay/Combat/WeaponAttributesContentTests.cs` | +44 lines, 3 new blueprint-shape tests |
| NEW | `Assets/Tests/EditMode/Gameplay/Combat/FlamingSwordContentTests.cs` | +180 lines, 3 integration tests + helpers |

## Tests

`+6 new tests`:
- 3 blueprint-shape (in `WeaponAttributesContentTests.cs`)
- 3 integration (new `FlamingSwordContentTests.cs`)

### Final test results — ALL GREEN ✅

After Unity recovered from the deferred-domain-reload trap, the full
6-test subset ran cleanly via Unity MCP test runner (job
`73c4406ec09745aa9162ba36a2d1154c`), 51ms total:

| # | Test | Result |
|---:|---|:-:|
| 1 | `FlamingSword_HasCuttingFireLongBladesAttribute` | ✅ PASS |
| 2 | `FlamingSword_AttributesContain_Fire` | ✅ PASS |
| 3 | `FlamingSword_DoesNotHavePiercing_OrBludgeoning` | ✅ PASS |
| 4 | `FlamingSword_AttributesViaCombatPath_TriggerHeatResistance` | ✅ PASS |
| 5 | `NonFireDamage_OnHeatResistantTarget_NotReduced` | ✅ PASS |
| 6 | `FlamingSword_OnGlowmaw_TakesLessDamageThan_ControlTarget` | ✅ PASS |

**6 passed, 0 failed, 0 skipped, resultState: Passed.**

The 🔵 Finding 1 (deferred verification) is closed. The
inspection-level confidence held: zero post-hoc bugs surfaced when
Unity finally ran the suite.

Full EditMode regression sweep follow-up: if any unrelated tests fail,
documented as a separate finding in the next session.

## Manual playtest scenario

Per Methodology Template §3.6, content with player-visible behavior gets
a `Scenarios/Custom/<Name>.cs` so a human can load and exercise the
feature in-game without needing to grind an actual playthrough into the
right conditions.

### Where to find it

**Menu:** `Caves Of Ooo / Scenarios / Combat Stress / FlamingSword Showcase (Phase C × E)`

After the menu click, Unity enters Play mode and the scenario applies on
`GameBootstrap.OnAfterBootstrap`. Press `Cmd+Shift+R` to re-launch it
without re-clicking the menu.

### What it stages

Three creatures, each with a `FlamingSwordDemoProbePart` that logs every
incoming hit's full attribute list + the target's HeatResistance:

| Position | Target | HeatResistance | Use this weapon | Demonstrates |
|---|---|---:|---|---|
| E (player+3, 0) | Glowmaw | 50 | FlamingSword | Fire damage **halved** |
| NE (player+3, -2) | Snapjaw | 0 | FlamingSword | Fire damage **NOT reduced** (no HR) |
| SE (player+3, +2) | Glowmaw | 50 | ShortSword (swap from inventory) | Non-Fire damage **NOT reduced** |

All three creatures have HP padded to 200 so a half-dozen swings each
land before any death. Player gets HP=200/200, Strength=24, FlamingSword
equipped, ShortSword + 5 HealingTonics in inventory.

### What to watch for in the message log

Each `[FlameDemo]` line carries:
- target name
- the damage amount BEFORE Phase E resistance fires
- a `FIRE` / `non-fire` flag
- the target's HeatResistance value (live read)
- the full attribute list

Expected sequence (paraphrased):

```
--- swing FlamingSword east at Glowmaw ---
[FlameDemo] glowmaw incoming: amount=8 FIRE HR=50 attrs=[Melee,Strength,Cutting,Fire,LongBlades]
(Glowmaw HP drops by ~4 — HeatResistance halved the hit)

--- swing FlamingSword northeast at Snapjaw ---
[FlameDemo] snapjaw incoming: amount=8 FIRE HR=0 attrs=[Melee,Strength,Cutting,Fire,LongBlades]
(Snapjaw HP drops by the full ~8 — no Heat resistance)

--- swap to ShortSword via inventory, swing southeast at the other Glowmaw ---
[FlameDemo] glowmaw incoming: amount=6 non-fire HR=50 attrs=[Melee,Strength,Cutting,LongBlades]
(Glowmaw HP drops by the full ~6 — no Fire attribute; HeatResistance never fires)
```

That trifecta proves the chain end-to-end:
- Fire attribute IS in the Damage object (per-hit log line shows it)
- HeatResistance IS the stat that gets read (probe shows the live value)
- The combination of both is what triggers reduction

### Files

| State | Path | Purpose |
|---|---|---|
| NEW | `Assets/Scripts/Scenarios/Custom/FlamingSwordShowcase.cs` | Scenario class + demo probe Part |
| MOD | `Assets/Editor/Scenarios/ScenarioMenuItems.cs` | `[MenuItem]` registration at priority 106 |


## Blueprint shape (proposed)

```json
{
  "Name": "FlamingSword",
  "Inherits": "MeleeWeapon",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "flaming sword" },
      { "Key": "RenderString", "Value": "/" },
      { "Key": "ColorString", "Value": "&R" },
      { "Key": "RenderLayer", "Value": "5" }
    ]},
    { "Name": "Physics", "Params": [{ "Key": "Takeable", "Value": "true" }, { "Key": "Weight", "Value": "6" }] },
    { "Name": "MeleeWeapon", "Params": [
      { "Key": "BaseDamage", "Value": "1d8" },
      { "Key": "PenBonus", "Value": "2" },
      { "Key": "Attributes", "Value": "Cutting Fire LongBlades" }
    ]},
    { "Name": "Commerce", "Params": [{ "Key": "Value", "Value": "40" }] },
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

Numbers chosen to slot between ShortSword (1d6 Tier-1, 15g) and Sporeblade
(1d8+1 Tier-2, 35g) — solid mid-tier weapon priced at 40g, identifiable by
red color (`&R`).
