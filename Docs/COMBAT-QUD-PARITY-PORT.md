# Combat: Qud Parity Port

> Living document tracking the systematic port of `CavesOfOoo.Core.CombatSystem` to **behavioral parity** with Caves of Qud's combat math.

## Strategy

**Behavioral parity, not structural parity.** Match the math, decision logic, and observable side effects of Qud's combat surface; keep our simpler event plumbing (string-keyed `GameEvent`) where it doesn't compromise correctness.

Where Qud's event chain genuinely matters for the math (e.g., listeners that mutate penetration bonuses inline via `ref` parameters), introduce **typed event classes** that extend `GameEvent` — gives compile-time safety and `e.PenetrationBonus += 2`-style mutation without porting the full pool/`Send`/`Process` infrastructure.

**Divergences are explicit decisions, not accidental drift.** Anywhere we deviate from Qud's behavior, log it in this document under the relevant phase's "Divergences" section so future-us can find it.

## Reference

- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Combat.cs` (1758 LOC, the main combat orchestrator)
- `/Users/steven/qud-decompiled-project/XRL.Rules/Stat.cs` (penetration math, dice rolls, stats)
- Other XRL classes pulled in as needed (`Damage`, `BodyPart`, `MeleeWeapon`, etc.)

## TDD discipline

Every phase follows strict RED → GREEN → adversarial:

1. **Spec tests (RED).** Write tests for the Qud-parity behavior *before* changing implementation. They should fail because the current implementation doesn't match.
2. **Implementation (GREEN).** Port the algorithm. Run tests; they should pass on first run if the port is correct, on second pass after a tweak otherwise.
3. **Adversarial pass.** Add mutation tests (flip a constant, reverse a comparison) and edge cases (extreme inputs, boundary conditions). These catch ports that are "close but not exact."

Test execution: `mcp__unity__run_tests` filtering on the relevant test class.

## Phase summary

| Phase | Scope | Status |
|---|---|---|
| A | `RollDamagePenetrations` (3-roll set, exploding 1d10−2, per-set pens, set-decay, MaxBonus cap) | ✅ **complete** |
| B | Strength bonus to damage (post-pen) | ⛔ **skipped — false premise** |
| B½ | Damage path polish (`WeaponIgnoreStrength` tag, multi-stat selection, `AdjustDamage*` args, pen event hooks) | ⏸ deferred (low-impact edge cases) |
| C | Damage class foundation — `Damage(int amount, List<string> attributes)` mirroring Qud's `XRL.World.Damage` | ✅ **complete** |
| D | Critical hit damage scaling (nat-20 → AutoPen flag + crit pen bonus + "Critical" attribute) | ✅ **complete** |
| E | Resistance stats (`ColdResistance`, `HeatResistance`, `AcidResistance`, `ElectricResistance`) — applied in `ApplyDamage` based on damage attributes | ✅ **complete** |
| F | Per-attribute reactions (acid corrodes equipment, fire spreads, electricity arcs, etc.) | ⏸ pending |
| G | Off-hand penalty + multiweapon fighting (skill-aware scaling) | ⏸ pending |
| H | Dismemberment with damage-attribute interaction (cutting → sever, blunt → fracture) | ⏸ pending |

### Phase B correction note

Originally I claimed Qud adds Strength mod to damage. **This was wrong.** Re-reading
`Combat.cs` lines 1276-1304 carefully, the only `damage.Amount +=` in the damage path
is the per-penetration BaseDamage roll. Qud's "Strength affects damage" is purely
indirect — high Str → more penetrations → more damage dice rolled. No additive Str
to damage exists in Qud, and our code already mirrors this behavior. Phase B as
originally scoped has no work to do.

The smaller Phase B-flavored gaps (WeaponIgnoreStrength tag, multi-stat selection,
AdjustDamage* args, pen event hooks) are deferred as Phase B½. They're real but
low-impact edge cases that don't block the Damage class foundation.

---

## Phase A: `RollDamagePenetrations` parity

### Status: ✅ complete

**Result:** algorithm replaced behaviorally one-for-one with Qud. New tests (7 parity + 7 adversarial) all green; full suite 2099/2099 green; zero regressions.

### Qud reference

`XRL.Rules/Stat.cs:160-203`:

```csharp
public static int RollDamagePenetrations(int TargetInclusive, int Bonus, int MaxBonus)
{
    int num = 0;          // total penetration count
    int num2 = 3;          // sentinel: enter loop the first time
    while (num2 == 3)
    {
        num2 = 0;
        for (int i = 0; i < 3; i++)
        {
            int num3 = Random(1, 10) - 2;       // 1d10 − 2 (range −1 to 8)
            int num4 = 0;
            while (num3 == 8)                    // explode on 8 (raw 10)
            {
                num4 += 8;
                num3 = Random(1, 10) - 2;
            }
            num4 += num3;
            int num5 = num4 + Math.Min(Bonus, MaxBonus);
            if (num5 > TargetInclusive)
                num2++;
        }
        if (num2 >= 1)
            num++;          // ANY success in set → +1 total pen
        Bonus -= 2;          // bonus decays unconditionally each set
    }
    return num;
}
```

### Current implementation (pre-port)

`Assets/Scripts/Gameplay/Combat/CombatSystem.cs:349-377`:

```csharp
public static int RollPenetrations(int pv, int av, Random rng)
{
    int penetrations = 0;
    int currentPV = pv;
    int streak = 0;
    int rollsInSet = 3;

    for (int i = 0; i < rollsInSet; i++)
    {
        int roll = DiceRoller.Roll(8, rng) + currentPV;     // 1d8 + PV
        if (roll > av)
        {
            penetrations++;                                  // +1 pen PER successful roll
            streak++;

            if (streak == rollsInSet)
            {
                currentPV -= 2;                              // decay only on streak
                streak = 0;
                rollsInSet = 3;
                i = -1;                                      // restart loop
                if (currentPV + 8 <= av)
                    break;
            }
        }
    }

    return penetrations;
}
```

### Gap summary

| # | Aspect | Yours | Qud | Impact |
|---|---|---|---|---|
| 1 | Die size | 1d8 | 1d10 − 2 | Different distribution; same effective max in non-explosion case |
| 2 | Exploding dice | none | explodes on raw 10 (post-mod 8) | Major: enables low-PV penetration via streaks of 10s |
| 3 | Pen counting | per successful roll | per set with ≥1 success | Qud is more conservative; max +1 pen per 3 rolls |
| 4 | Bonus decay | only on streak | every set unconditionally | Qud decays faster → fewer total pens |
| 5 | MaxBonus cap | not present | `Math.Min(Bonus, MaxBonus)` | Yours has no per-call cap; `MaxStrengthBonus` only caps strMod externally |
| 6 | Loop continuation | restart-on-streak via `i = -1` | `while (num2 == 3)` outer loop | Same intent, cleaner Qud structure |

### Implementation plan

1. **Replace `RollPenetrations` with Qud-parity algorithm** (same method name to keep the public API stable; new signature adds `maxBonus`).
2. **Add overload for backward compatibility** during transition? → No, breaking change is fine; only `PerformSingleAttack` calls it. Update caller.
3. **Update `PerformSingleAttack`** to thread `maxBonus` through (computed from `MaxStrengthBonus + PenBonus`).

New signature:
```csharp
public static int RollPenetrations(int targetInclusive, int bonus, int maxBonus, Random rng)
```

Uses our `DiceRoller` for the `1d10` rolls (to keep RNG threading consistent across the codebase).

### Test plan

**RED tests** — new file `RollPenetrationsParityTests.cs`. Each asserts a Qud-specific behavior that the current code violates:

| # | Test | What it proves |
|---|---|---|
| 1 | `ExplodingDice_AllowsPenetration_AtImpossibleBaseRange` | A target the OLD algo can never hit gets penetrated by chained 10s under Qud |
| 2 | `PerSetPenCounting_TwoOfThreeSucceed_OnlyOnePen` | Qud awards ≤ 1 pen per set; old algo would award 2+ |
| 3 | `BonusDecay_EveryUnconditionalSet_NotJustStreaks` | After 1 successful-but-not-clean-sweep set, next set sees Bonus−2 |
| 4 | `MaxBonusCap_LimitsBonusContribution` | High Bonus + low MaxBonus → effective bonus = MaxBonus |
| 5 | `Deterministic_SameSeedSameResult` | Replay safety preserved |
| 6 | `LoopTerminates_EvenWithUnboundedExplosions` | Sanity: no infinite loops under any input |

**GREEN expectation:** Tests 1-4 fail before port, pass after. 5-6 pass either way (sanity).

**Adversarial pass:** mutation tests + boundary inputs (see "Adversarial" section after GREEN).

### Caller updates

In `PerformSingleAttack` (`CombatSystem.cs:158-164`):

```csharp
// OLD
int strMod = StatUtils.GetModifier(attacker, statName);
if (maxStrBonus >= 0 && strMod > maxStrBonus)
    strMod = maxStrBonus;
int pv = strMod + penBonus;
int av = hitPart != null ? GetPartAV(defender, hitPart) : GetAV(defender);
int penetrations = RollPenetrations(pv, av, rng);

// NEW
int strMod = StatUtils.GetModifier(attacker, statName);
int bonus = strMod + penBonus;
int maxBonus = ((maxStrBonus < 0) ? int.MaxValue / 2 : maxStrBonus) + penBonus;
int av = hitPart != null ? GetPartAV(defender, hitPart) : GetAV(defender);
int penetrations = RollPenetrations(av, bonus, maxBonus, rng);
```

Note: the strMod cap moves from caller into `RollPenetrations` (matching Qud). The `int.MaxValue / 2` guard handles the legacy "uncapped" sentinel (`-1`).

### Stale Phase 1 spec tests to reconcile

Five tests in `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemSpecTests.cs` were locked in against the old algorithm. After the port:

| Existing test | Status after port |
|---|---|
| `RollPenetrations_NegativePV_HighAV_ReturnsZero` | Still ~true (with explosions, exceedingly rare nonzero) — tighten gap to make safely deterministic |
| `RollPenetrations_HighPV_ZeroAV_StreakTriggers_ReturnsMoreThan3` | Still true (Qud's set-streak also produces >3) |
| `RollPenetrations_HighPV_ZeroAV_StreakDoesNotLoopForever` | Still true (Qud's loop also bounded by Bonus decay) |
| `RollPenetrations_BalancedPVAV_NoStreak_ReturnsZeroToThree` | **STALE** — Qud allows >3 via per-set streak. Delete. |
| `RollPenetrations_Deterministic_SameSeedSameResult` | Still true |

### Divergences captured (Phase A)

| # | Divergence | Rationale |
|---|---|---|
| 1 | Legacy `MeleeWeaponPart.MaxStrengthBonus = -1` sentinel maps to `effectiveMaxStrBonus = 50` at the call site | Qud's MaxStrengthBonus is always a positive value per weapon (no sentinel). Mapping `-1` to `50` preserves the legacy "uncapped" semantic without risking `int.MaxValue` overflow during `bonus -= 2` decay loop. |
| 2 | RNG threading — explicit `Random rng` parameter | Qud uses a global `Stat.Rnd`; we keep our explicit RNG pattern for testability/replay safety. Behavior identical for any single seeded run. |
| 3 | No "Debug" logging branch | Qud's `Options.DebugDamagePenetrations` flag is omitted; combat log already shows penetration outcomes via the message log. |

### Implementation log

| Step | Result |
|---|---|
| RED tests in `RollPenetrationsParityTests.cs` | 9 compile errors (no 4-arg overload) — confirmed RED state |
| Algorithm port (`CombatSystem.cs:349-396`) | Replaced 30-line method with Qud-parity 50-line version (with explosion loop + outer streak loop) |
| Caller update (`PerformSingleAttack`) | Threaded `bonus`, `maxBonus`, `targetInclusive` through new signature; legacy `-1` sentinel handled |
| Stale spec test reconciliation | Removed 2 tests from `CombatSystemSpecTests.cs` (`NegativePV_HighAV_ReturnsZero`, `BalancedPVAV_NoStreak_ReturnsZeroToThree`); updated 3 tests' signatures; updated 2 tests in `CombatSystemTests.cs` |
| GREEN verification | 7/7 parity tests pass, 57/57 combat tests pass |
| Adversarial pass (`RollPenetrationsAdversarialTests.cs`, 7 tests) | Mutation-resistant tests for: strict-greater, Min-cap (monotone-on-cap), per-set vs per-roll, outer-loop guard, unconditional decay, exploding dice present, integration smoke. All 7 pass |
| Full EditMode suite | 2099/2099 green (was 2087 pre-port; +12 net: +7 parity, +7 adversarial, −2 stale) |

### Files changed (Phase A)

| File | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | `RollPenetrations` algorithm replaced with Qud parity; signature changed to `(int targetInclusive, int bonus, int maxBonus, Random rng)`. Caller in `PerformSingleAttack` updated. |
| `Assets/Tests/EditMode/Gameplay/Combat/RollPenetrationsParityTests.cs` | **new** — 7 tests pinning Qud-parity behavior (exploding dice, per-set counting, decay, MaxBonus cap, determinism, termination) |
| `Assets/Tests/EditMode/Gameplay/Combat/RollPenetrationsAdversarialTests.cs` | **new** — 7 mutation-resistance tests |
| `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemSpecTests.cs` | Removed 2 stale tests; updated 3 to new signature |
| `Assets/Tests/EditMode/Gameplay/Combat/CombatSystemTests.cs` | Updated 2 legacy tests to new signature (renamed for clarity: PV→Bonus, AV→Target) |

---

## Phase C: `Damage` class foundation

### Status: ✅ complete

**Result:** typed `Damage` class created (mirror of `XRL.World.Damage`); `PerformSingleAttack` and `ApplyDamage` refactored to use it; backward-compat int overload preserved for 20+ legacy callers; `MeleeWeaponPart.Attributes` field added. New tests: 21 unit + 9 integration. Full suite: 2129/2129 green.

### Qud reference

`XRL.World/Damage.cs` (327 LOC):
- `Amount` (int, with `Math.Max(0, value)` clamp on set)
- `Attributes` (`List<string>`) — flexible tag set, **not** an enum
- Methods: `AddAttribute`, `HasAttribute`, `HasAnyAttribute(List<string>)`, `AddAttributes(string)` (space-separated)
- Type-check helpers: `IsColdDamage()`, `IsHeatDamage()`, `IsElectricDamage()`, `IsBludgeoningDamage()`, `IsAcidDamage()`, `IsLightDamage()`, `IsDisintegrationDamage()` — each checks for any of several alias attributes (e.g., Cold OR Ice OR Freeze)

### Design decision: tags over enum

Qud uses a flexible **tag set** rather than a single `DamageType` enum. A single piece of damage can have multiple attributes simultaneously:
- A flaming sword: `["Melee", "Cutting", "Fire", "LongBlades", "Strength"]`
- A poisoned arrow: `["Missile", "Piercing", "Poison", "Agility"]`
- A psionic icebolt: `["Ranged", "Cold", "Mental", "Willpower"]`

This gives us:
- **Composable interactions** — fire-resistance applies to anything with "Fire" attribute, regardless of source weapon
- **Stat tracking** — `damage.HasAttribute("Strength")` lets achievements/stats hook into damage source
- **Cleaner event hooks** — listeners filter by attribute presence rather than enum match

We mirror Qud's design directly.

### What Phase C does NOT do

- ❌ Resistance stats (`ColdResistance`, etc.) → **Phase E**
- ❌ Per-attribute reactions (acid corrodes equipment, fire spreads, electricity arcs) → **Phase F**
- ❌ Damage-type-specific dismemberment → **Phase H**
- ❌ Critical-hit attribute (`damage.AddAttribute("Critical")`) → **Phase D**

Phase C is just the foundation — the Damage class plumbing — so subsequent phases have something typed to flow through.

### Implementation plan

1. **New file:** `Assets/Scripts/Gameplay/Combat/Damage.cs`
   - Mirrors `XRL.World/Damage.cs` field-for-field (Amount, Attributes, Has*, Is*Damage, AddAttribute, AddAttributes)
   - Skip the static AttributeSounds dictionary (Unity-side concern, not parity-essential)
   - Skip `[Serializable]` decorators; use our own SaveSystem path

2. **`MeleeWeaponPart`** gains `Attributes` (string, space-separated, mirroring Qud)
   - Default: empty
   - Blueprint-loadable: `{ "Name": "MeleeWeapon", "Params": [{ "Key": "Attributes", "Value": "Cutting LongBlades" }] }`

3. **`PerformSingleAttack`** builds typed Damage:
   - `Damage damage = new Damage(0)` at top
   - `damage.AddAttribute("Melee")`
   - `damage.AddAttribute(weapon.Stat ?? "Strength")` — the stat used for pen
   - `damage.AddAttributes(weapon.Attributes)` — weapon-defined attributes
   - After damage roll: `damage.Amount = totalDamage`
   - Pass `damage` to `ApplyDamage`

4. **`ApplyDamage` overloads:**
   ```csharp
   // New primary — typed
   public static void ApplyDamage(Entity target, Damage damage, Entity source, Zone zone)

   // Legacy backward-compat wrapper
   public static void ApplyDamage(Entity target, int amount, Entity source, Zone zone)
       => ApplyDamage(target, new Damage(amount), source, zone);
   ```
   20+ existing callers continue to work via the int wrapper.

5. **TakeDamage / DamageDealt events** gain a `"Damage"` parameter (carrying the Damage object). Existing `"Amount"` parameter stays for backward-compat.

### Test plan

**RED tests (`DamageTests.cs`):**
- Constructor sets Amount; negative input clamps to 0
- AddAttribute appends; HasAttribute returns true; non-added returns false
- AddAttributes parses space-separated string
- HasAnyAttribute returns true on intersection, false on disjoint
- IsColdDamage matches "Cold", "Ice", "Freeze"; not "Heat"
- IsHeatDamage matches "Fire", "Heat"; not "Cold"
- (Same shape for Electric, Bludgeoning, Acid, Light, Disintegration)

**Integration tests:**
- `PerformMeleeAttack` causes a typed Damage to flow through ApplyDamage with correct attributes
- Weapon's `Attributes` field propagates into the Damage
- Default attributes always include "Melee" and the weapon's Stat name

**Adversarial:**
- Damage with empty attributes list — IsColdDamage etc. return false
- Damage.Amount setter clamps negative to 0 (mutation: flip Math.Max → Math.Min)
- AddAttribute with null/empty — does it append? Qud appends; we mirror
- Backward-compat int overload preserves existing behavior bit-for-bit

### Divergences captured (Phase C)

| # | Divergence | Rationale |
|---|---|---|
| 1 | Skipped `[ModSensitiveCacheInit]` static constructor for `AttributeSounds` | Sound-effects mapping is a Unity-side concern (we have `AsciiFxBus` instead). Will revisit in Phase F if/when per-attribute SFX is desired. |
| 2 | Skipped `[Serializable]` decorators (kept `[Serializable]` on `Damage` only as a marker) | Our SaveSystem uses its own serialization path via `ISaveSerializable` rather than `BinaryFormatter`. |
| 3 | `AddAttribute` does NOT dedupe (matches Qud) | Some Qud code paths count duplicate tags. Documented in tests; can revisit if confusing in practice. |

### Implementation log

| Step | Result |
|---|---|
| RED tests in `DamageTests.cs` (21 tests) | Compile errors confirmed RED (type `Damage` not found) |
| `Damage` class implemented | `Assets/Scripts/Gameplay/Combat/Damage.cs` (~150 LOC); 21/21 tests green on first run |
| `MeleeWeaponPart.Attributes` field added | Auto-handled by `EntityFactory.ApplyParameters` reflection — no factory changes needed |
| `PerformSingleAttack` refactored | Builds `Damage` with `["Melee", weapon.Stat, ...weapon.Attributes]`, sets `damage.Amount` from rolled total, passes typed `Damage` to `ApplyDamage` |
| `ApplyDamage` overload split | New typed primary; legacy int wraps `new Damage(amount)` and forwards. Both `TakeDamage` and `DamageDealt` events now carry both legacy `"Amount"` int and new `"Damage"` object parameters |
| Integration tests in `CombatDamageIntegrationTests.cs` (9 tests) | All green: typed flow, weapon attribute propagation, legacy `Amount` preserved, int overload still works, null/zero/negative damage no-ops, no aliasing across attacks |
| Full EditMode suite | 2129/2129 green (was 2099 pre-Phase-C; net +30: 21 unit + 9 integration) |

### Files changed (Phase C)

| File | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/Damage.cs` | **new** — Qud-parity Damage class with attributes list, type-check helpers |
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | `PerformSingleAttack` builds typed Damage; `ApplyDamage` split into typed + int overloads; events carry both `Amount` and `Damage` parameters |
| `Assets/Scripts/Gameplay/Items/MeleeWeaponPart.cs` | New `Attributes` field (space-separated string of damage attributes) |
| `Assets/Tests/EditMode/Gameplay/Combat/DamageTests.cs` | **new** — 21 unit tests for the Damage class |
| `Assets/Tests/EditMode/Gameplay/Combat/CombatDamageIntegrationTests.cs` | **new** — 9 integration + adversarial tests for the typed-Damage flow |

---

## Phase D: Critical hit damage scaling

### Status: ✅ complete

### Qud reference

`XRL.World.Parts/Combat.cs:1106-1140, 1218-1228, 1247-1275`:
- nat-20 (or skill-modified threshold) sets the critical flag
- On critical: `+1` PenBonus, `+1` PenCapBonus, `flag5` (AutoPen) set
- AutoPen forces pens=1 if rolls fail AND attacker is the player (`flag5 && Attacker.IsPlayer()`)
- On successful critical pen: `damage.AddAttribute("Critical")`

### Implementation in our port

In `PerformSingleAttack`:

```csharp
int critPenBonus = naturalTwenty ? 1 : 0;
int critMaxBonus = naturalTwenty ? 1 : 0;
bool autoPen = naturalTwenty;

int penetrations = RollPenetrations(av, bonus + critPenBonus, maxBonus + critMaxBonus, rng);

// AutoPen — only fires for the player (mirrors Qud's IsPlayer guard)
if (penetrations == 0 && autoPen && attacker.HasTag("Player"))
    penetrations = 1;

// ...damage build...
if (naturalTwenty)
    damage.AddAttribute("Critical");
```

### Divergences captured (Phase D)

| # | Divergence | Rationale |
|---|---|---|
| 1 | Skipped `Skills.GetGenericSkill` weapon-critical-modifier chain | We don't have skills yet. Phase D restores the missing nat-20 mechanic without a skill system. Revisit when skills land. |
| 2 | Skipped `WeaponCriticalModifier` / `AttackerCriticalModifier` event chain | These are listener-mutation hooks that need typed events to be ergonomic. Defer to a future event-system upgrade phase. |
| 3 | No `CriticalHit` event firing | Listeners interested in crits can detect via `damage.HasAttribute("Critical")` on the existing `TakeDamage` / `DamageDealt` events. |

### Files changed (Phase D)

| File | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | `PerformSingleAttack`: added crit pen-bonus, AutoPen guard, "Critical" attribute |
| `Assets/Tests/EditMode/Gameplay/Combat/CriticalHitTests.cs` | **new** — 5 spec tests (AutoPen for player, AutoPen blocked for non-player, Critical attribute, non-crit doesn't add, crit pen bonus smoke) |
| `Assets/Tests/EditMode/Gameplay/Combat/CriticalHitAdversarialTests.cs` | **new** — 4 mutation-resistance tests (AutoPen produces exactly 1, doesn't cap successful pens, no Critical when pen fails, pen bonus is exactly 1) |

---

## Phase E: Resistance stats

### Status: ✅ complete

### Qud reference

`XRL.World.Parts/Physics.cs:3351-3417` — four resistance loops in `HandleEvent` for `TakeDamage`:
- `AcidResistance` reduces `IsAcidDamage()` damage
- `HeatResistance` reduces `IsHeatDamage()` damage
- `ColdResistance` reduces `IsColdDamage()` damage
- `ElectricResistance` reduces `IsElectricDamage()` damage

Formula (per type):
- Positive resist: `damage.Amount = damage.Amount * (100 − resist) / 100`, with min-1 unless ≥100% resist
- Negative resist: `damage.Amount += damage.Amount * (-resist / 100)` (vulnerability)
- `IgnoreResist` attribute on damage bypasses all resistance

### Implementation in our port

In `ApplyDamage(Entity, Damage, Entity, Zone)`, immediately after the dead-target guard:

```csharp
if (!damage.HasAttribute("IgnoreResist"))
    ApplyResistances(target, damage);

if (damage.Amount <= 0) return;  // resistance fully absorbed
```

`ApplyResistances` dispatches each elemental check via `damage.IsXDamage()` helpers from Phase C, calling `ApplyResistanceFor(target, damage, "XResistance")` on matches. Order is fixed (Acid → Heat → Cold → Electric) to match Qud's source.

### Divergences captured (Phase E)

| # | Divergence | Rationale |
|---|---|---|
| 1 | Resistance is applied in `CombatSystem.ApplyDamage`, not in a target-side `Part.HandleEvent("TakeDamage")` | Qud's `Physics.cs` runs resistance in a Part handler; our port lifts it to `CombatSystem` for centralization. Both produce the same result. Listeners that want *pre*-resistance damage would need to hook upstream of ApplyDamage; defer if needed. |
| 2 | Order matches Qud (Acid → Heat → Cold → Electric) | Documented in case future work cares (e.g., a damage tagged Cold AND Fire chains both, with cold first). |
| 3 | Skipped Qud's `BeforeApplyDamageEvent` chain | Not needed for resistance correctness; would land in a future event-upgrade phase. |

### Files changed (Phase E)

| File | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | `ApplyDamage` typed overload now applies resistances (with IgnoreResist bypass); two new private helpers: `ApplyResistances`, `ApplyResistanceFor` |
| `Assets/Tests/EditMode/Gameplay/Combat/ResistanceTests.cs` | **new** — 13 spec + adversarial tests (positive resist, 100% immunity, low-damage min-1 clamp, negative resist vulnerability, type-mismatch no-op, IgnoreResist bypass, no-stat default 0, multi-type chaining, zero-damage edge, melee unaffected by elemental) |
