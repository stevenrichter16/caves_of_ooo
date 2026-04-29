# Elemental Tonics — AcidTonic, LightningTonic, FrostTonic, WaterTonic

**Status:** shipped
**Branch:** `feat/elemental-tonics`
**Plan ref:** `Docs/CONTENT-ROADMAP.md` Tier 1 → Status tonics

## Goal

Ship the four remaining elemental status tonics (Acid / Lightning /
Frost / Water) so the StatusTonicPart dispatcher's existing capacity
becomes player-exercisable content. Pure JSON + tests; no new code
paths required.

## User-visible invariant

"Drinking an `<Element>Tonic` applies the matching status effect to
the drinker. Other tonics never apply this effect." (4× elements)

## Scope

In:
- `Assets/Resources/Content/Blueprints/Objects.json`: 4 new blueprints
  inheriting `TonicItem`, mirroring `FireTonic`'s shape.
- `Assets/Tests/EditMode/Gameplay/Items/`: 4 new test classes mirroring
  `StoneskinTonicTests`.
- `Docs/CONTENT-ROADMAP.md`: flip 4 entries from 📋 to ✅ with hash.

Out (deferred to later content tiers):
- Throwable consumables (Tier-2: tonics shatter on impact).
- Faction-aware drop tables for the new tonics.
- Showcase scenario (`TonicTestBench`) — content-only ships first.

## Verification sweep (complete — no false premises)

| Premise | Status | Source |
|---|---|---|
| `StatusTonicPart.CreateEffect` already dispatches `acid|acidic|acidiceffect` → `new AcidicEffect(corrosion)` | ✅ | `StatusTonicPart.cs:61-65` |
| Same for `electric|electrified|...` → `new ElectrifiedEffect(charge)` | ✅ | `StatusTonicPart.cs:67-73` |
| Same for `ice|frost|frozen|...` → `new FrozenEffect(cold)` | ✅ | `StatusTonicPart.cs:75-80` |
| Same for `wet|water|...` → `new WetEffect(moisture)` | ✅ | `StatusTonicPart.cs:55-59` |
| Effect ctors take single magnitude float | ✅ | `AcidicEffect:15`, `ElectrifiedEffect:15`, `FrozenEffect:21`, `WetEffect:14` |
| Tonic blueprint shape (Render + Physics + Tonic + StatusTonic + Commerce) inheriting `TonicItem` | ✅ | `Objects.json:469-515` (PoisonTonic + FireTonic) |
| `FireTonic` uses only `EffectMagnitude`, no duration — same pattern works for elemental tonics whose effects use magnitude | ✅ | `Objects.json:494-515` |
| Test pattern: `ScenarioTestHarness` + `FireApplyTonic` helper, 6 tests per tonic | ✅ | `StoneskinTonicTests.cs:1-172` |
| Existing tonic dispatcher has `case "wet": case "water":` for the WaterTonic name → `WetEffect`. WaterTonic blueprint uses `EffectName=Water` | ✅ | `StatusTonicPart.cs:55-57` |

**No corrections required.** Implementation is 4× blueprint-paste + test-paste.

## Sub-milestones

Single commit, but four logically-independent unit-test classes so
review is per-tonic.

### M1 — AcidTonic
- Blueprint: `EffectName=Acid`, `EffectMagnitude=1`, green-tinted `!`,
  Commerce 22 (premium over Fire because Acid is rarer in the world).
- Test: `AcidTonicTests` (6 tests mirroring StoneskinTonicTests pattern).

### M2 — LightningTonic
- Blueprint: `EffectName=Lightning`, `EffectMagnitude=1`, white/yellow `!`,
  Commerce 22.
- Test: `LightningTonicTests`.

### M3 — FrostTonic
- Blueprint: `EffectName=Frost`, `EffectMagnitude=1`, cyan `!`,
  Commerce 22.
- Test: `FrostTonicTests`.

### M4 — WaterTonic
- Blueprint: `EffectName=Water`, `EffectMagnitude=1`, blue `!`,
  Commerce 12 (cheaper — water is mundane).
- Test: `WaterTonicTests`.

## Test plan (per tonic, 6 tests each = 24 total)

Mirror `StoneskinTonicTests`:

1. **Drinking applies the target Effect.** RED first, GREEN after blueprint.
2. **Counter-check: drinking does NOT apply unrelated effects.** Pair to #1.
3. **Magnitude flows from blueprint param.** EffectMagnitude=1 → Effect's magnitude field > 0.
4. **Counter-check: another tonic doesn't apply this effect.** E.g., HealingTonic doesn't apply Acidic.
5. **Null-safety: actor without StatusEffectsPart doesn't crash.**
6. **Effect class type is correct.** `HasEffect<AcidicEffect>` returns true.

## Performance section

Per `CLAUDE.md` Performance triggers, this feature does NOT need a perf
section:
- ❌ No render hook needed (status effects already plumbed)
- ❌ No hot-path allocations (one-shot drink action)
- ❌ No new cache
- ❌ No new MonoBehaviour with Update
- ❌ No per-frame / per-turn event listener

The 4 effect classes already exist and are exercised by other content.

## Implementation log

| Step | Status | Notes |
|---|---|---|
| Plan written | ✅ | this commit |
| RED tests | ✅ | 8 expected RED on AcidTonic + LightningTonic blueprint-missing path; counter-check tests for HealingTonic / FireTonic GREEN as expected (those blueprints exist) |
| GREEN blueprints | ✅ | 4 blueprints inserted after FireTonic in Objects.json |
| Confirm GREEN | ✅ | 30/30 tonic sweep + 390/390 broader regression sweep |
| Self-review | ✅ | All 🟡 / 🔴 clear; 🔵 boilerplate noted, deferred per pre-flag |
| Roadmap updated | ✅ | 4 entries flipped 📋 → ✅ |
| Merged to main | ✅ | this commit |

## Status: shipped

## Self-review (pre-flagged)

- 🔵 4× boilerplate test classes share 90% of code with StoneskinTonicTests.
  Could extract a shared `TonicTestBase<TEffect>` generic helper. Trade-off:
  generics-over-content increases cognitive load for a 4-tonic ship. Defer
  unless we add 5+ more tonics.
- ⚪ Commerce values picked from analogy (FireTonic=20, PoisonTonic=18).
  No economy balance pass; if a value matters, the merchant table will
  surface it.
- ⚪ `EffectMagnitude=1` is the default ctor value — explicit declaration
  is for readability, not behavior.
