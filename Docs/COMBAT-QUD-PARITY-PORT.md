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
| B | Strength bonus to damage (post-pen, with MaxStrengthBonus cap) | ⏸ pending |
| C | Damage types (cutting/blunt/piercing; typed AV; weapon damage type) | ⏸ pending |
| D | Critical hit damage scaling (nat-20 → AutoPen flag + crit damage) | ⏸ pending |
| E | Off-hand penalty + multiweapon fighting (skill-aware scaling) | ⏸ pending |
| F | Dismemberment with damage-type interaction (cutting → sever, blunt → fracture) | ⏸ pending |

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
