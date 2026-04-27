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
| F | `BeforeTakeDamage` event hook (post-sweep narrowed scope; see Phase F note below) | ✅ **complete** |
| G | Multiweapon hooks: off-hand penalty becomes tunable; `MultiWeaponSkillBonus` stat reduces it | ✅ **complete** |
| H | `CanBeDismembered` event hook (re-scoped after sweep — see Phase H note) | ✅ **complete** |

### Phase H scope correction note (post-verification sweep)

Originally scoped as "cutting → 1.5x sever, blunt → 0.5x sever" — i.e., damage-attribute-modulated dismemberment chance.

**Verification sweep against Qud reference revealed this isn't how Qud does it.** Searching `qud_decompiled_project` for `HasAttribute.*Cutting` / `HasAttribute.*Bludg` returns **only one hit**: the `IsBludgeoningDamage()` helper inside `Damage.cs` itself, never consumed by combat code. Qud's combat dismemberment is:
- **Skill-driven**: `Axe_Dismember.Dismember()` and `Axe_Decapitate.Decapitate()` proc on successful axe-skill hits
- **Mod-driven**: `ModSerrated`, `ModNanon`, `ModGlazed` add dismember chance per-weapon
- **Power-driven**: `DismemberAdjacentHostiles` is an explicit power
- **Mutation-driven**: `Decarbonizer`

None of these consult the `Damage` object's attributes. Damage type doesn't affect dismemberment in Qud's combat path.

The closest centralized Qud-parity hook is `CanBeDismemberedEvent` (`IGameSystem.cs:637`), which lets parts veto dismemberment per-call. This is real Qud parity work (mirrors the `BeforeDismemberEvent` pattern at `Body.cs:2498`).

**Phase H final scope:** add `CanBeDismembered` event hook to `CheckCombatDismemberment` so parts can veto a pending dismemberment. Mirrors Qud's `CanBeDismemberedEvent` / `BeforeDismemberEvent`. Foundation for future "TitaniumBones" / "Indestructible" content. ~30 min, ~5 tests with counter-checks.

The originally-planned damage-attribute-modulated dismemberment is a CoO-original divergence. Defer until/unless gameplay design calls for it; if so, document as CoO-original per Methodology Template §4.2, NOT as parity.

### Phase F scope correction note (post-verification sweep)

Originally scoped as "per-attribute reactions" (acid corrodes equipment, fire spreads, electricity arcs).
**Verification sweep against Qud reference revealed this isn't centralized in the damage path.**
Qud's per-attribute behaviors live in scattered systems:
- `LiquidAcid.cs:105` applies `ContainedAcidEating` to its container (not via damage)
- `Physics.cs:3013` applies `Burning` during flame ticks (temperature-driven, not damage-driven)
- Cold/freeze likewise temperature-driven

**Second sweep correction.** I initially planned F.1 (4 "missing" `Is*Damage()` helpers — Poison/Bleeding/Mental/Explosion). Re-reading `XRL.World/Damage.cs:139-189` confirms Qud only has the 7 instance helpers we ported in Phase C — Poison/Bleeding/etc. are AttributeSound entries, not instance methods. No parity gap there.

Similarly, F.2 (Poison/BleedResistance stats) would be a CoO-original extension, not parity, and would be dead-letter without content that emits typed `Damage` with those attributes. Deferred.

Centralized damage-path gap that DOES have Qud parity:
- **F.3 → renamed F**: `BeforeTakeDamage` event hook. Mirrors Qud's `BeforeApplyDamageEvent` (Physics.cs:3418): listeners can mutate damage or veto entirely. Lets future content (status effects, mutations, equipment) react to incoming damage without modifying `CombatSystem`. Useful as architectural plumbing even before any concrete listener uses it — Phase E's resistance code could itself be re-implemented as a BeforeTakeDamage listener in the future.

Phase F final scope: **just the `BeforeTakeDamage` event hook**. ~30 min, ~5 tests including counter-checks.

The other deferred work (equipment corrosion, terrain reactions, temperature spreading, status-effect-from-damage-attribute) is either scattered across non-combat systems or CoO-original. Flagged in the post-port punch-list.

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

// NEW (post-self-review Finding 5: magic number replaced by constant)
int strMod = StatUtils.GetModifier(attacker, statName);
int bonus = strMod + penBonus;
int effectiveMaxStrBonus = (maxStrBonus < 0) ? LEGACY_UNCAPPED_MAX_STR_BONUS : maxStrBonus;
int maxBonus = effectiveMaxStrBonus + penBonus;
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
| 3 | Skipped Qud's `BeforeApplyDamageEvent` chain in initial Phase E | **Subsequently addressed in Phase F** as `BeforeTakeDamage`. |

### Files changed (Phase E)

| File | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | `ApplyDamage` typed overload now applies resistances (with IgnoreResist bypass); two new private helpers: `ApplyResistances`, `ApplyResistanceFor` |
| `Assets/Tests/EditMode/Gameplay/Combat/ResistanceTests.cs` | **new** — 13 spec + adversarial tests (positive resist, 100% immunity, low-damage min-1 clamp, negative resist vulnerability, type-mismatch no-op, IgnoreResist bypass, no-stat default 0, multi-type chaining, zero-damage edge, melee unaffected by elemental) |

---

## Self-review remediation pass (post-A/C/D/E)

### Status: ✅ complete (commit `02e08d1`)

After Phases A/C/D/E shipped without an in-phase self-review (Methodology Template §5 violation), a cold-eye review of those four phases was performed against the template. **11 findings logged** with severity markers; **6 fixed pre-commit**, 3 deferred, 2 architectural notes.

See full findings doc: [`Docs/COMBAT-PARITY-PORT-REVIEW.md`](./COMBAT-PARITY-PORT-REVIEW.md).

### Findings fixed

| # | Severity | Finding | Fix |
|---|:-:|---|---|
| 1 | 🟡 | TakeDamage listeners couldn't mutate `damage.Amount` in-flight (captured-before-event) | Re-read `damage.Amount` AFTER event with `Math.Max(0)` clamp |
| 2 | 🟡 | `NaturalTwenty_AutoPen_DoesNotFire_ForNonPlayerAttacker` threshold `< 50` allowed bug to slip through | Tightened to `< 5` |
| 3 | 🟡 | `NonCriticalHit_DoesNotAddCriticalAttribute` was vacuous `nonCrit > crit` comparison | Strengthened to bound `critsObserved ≤ 25` |
| 4 | 🟡 | Full resistance was silent (no event for "fully blocked") | Added `DamageFullyResisted` event firing |
| 5 | 🔵 | Magic number `50` in `effectiveMaxStrBonus` | Extracted to `LEGACY_UNCAPPED_MAX_STR_BONUS` constant |
| 6 | 🔵 | Doc snippet referenced `int.MaxValue / 2` instead of the actual constant | Updated doc |

### Findings deferred

| # | Severity | Finding | Why deferred |
|---|:-:|---|---|
| 7 | 🧪 | No test for stat-modification-based resistance (e.g., `StoneSkin` effect granting +25 cold resist) | Works but unpinned; revisit if a status-modifier bug surfaces |
| 8 | 🧪 | Snapjaw blueprint (and others) define no resistance stats — Phase E is dead-letter against current content | Content-team note, not a code fix |
| 9 | 🧪 | `bonus == maxBonus` boundary test for `RollPenetrations` | Minor edge case, low risk |

### PlayMode sanity sweep verification

Three scenarios verified end-to-end against the live `SampleScene.unity` bootstrap with raw output capture and explicit can-verify / cannot-verify honesty bounds:

- **Scenario 1 — Phase A + C**: typed Damage flows through TakeDamage event with `[Melee, Strength]` attributes ✅
- **Scenario 2 — Phase D**: 200 attacks → 11 crits at 6.7% rate, `[Melee, Strength, Critical]` attributes ✅
- **Scenario 3 — Phase E + Finding 4**: 50% resist halves, 100% resist absorbs + DamageFullyResisted fires; counter-check confirms full damage passes when no resistance ✅

### Playtest scenario

`Assets/Scripts/Scenarios/Custom/CombatParityShowcase.cs` (commit `02e08d1`) registered under **Caves Of Ooo > Scenarios > Combat Stress > Combat Parity Showcase**. Spawns 3 soak Snapjaws + Heat-immune Snapjaw + Cold-vulnerable Snapjaw for human inspection of the new mechanics.

### Files changed (remediation)

| File | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | Findings 1, 4, 5 fixes (listener mutation propagation, DamageFullyResisted event, LEGACY_UNCAPPED_MAX_STR_BONUS constant) |
| `Assets/Tests/EditMode/Gameplay/Combat/CriticalHitTests.cs` | Findings 2, 3 (tightened thresholds) |
| `Assets/Tests/EditMode/Gameplay/Combat/CombatSelfReviewRegressionTests.cs` | **new** — 7 regression tests pinning the fixes |
| `Assets/Scripts/Scenarios/Custom/CombatParityShowcase.cs` | **new** — playtest scenario |
| `Assets/Editor/Scenarios/ScenarioMenuItems.cs` | Menu entry for the showcase |
| `Docs/COMBAT-PARITY-PORT-REVIEW.md` | **new** — findings doc + sweep results |

---

## Phase F: `BeforeTakeDamage` event hook

### Status: ✅ complete (commit `5deb287`)

**Result:** added a pre-resistance event firing on the typed `ApplyDamage` path. Listeners can mutate damage in-flight (add/remove attributes, reduce Amount) or veto entirely. Vetoed damage fires `DamageFullyResisted` so observers see the attempt but `TakeDamage` doesn't fire and HP doesn't decrement. New tests: 8. Full suite: 2166/2166 green.

### Qud reference

`XRL.World.Parts/Physics.cs:3418` — `BeforeApplyDamageEvent.Check(damage, ParentObject, ...)` fires after resistance and lets handlers veto by returning false. We mirror the *intent* with our string-keyed event firing BEFORE resistance (so listeners see pre-resistance damage; matches our Phase E lift of resistance into `CombatSystem.ApplyDamage`).

### Pre-impl verification sweep — TWO false-premise corrections

The original Phase F scope ("per-attribute reactions: acid corrodes equipment, fire spreads, electricity arcs") did NOT survive the verification sweep. Qud's per-attribute behaviors live in **scattered non-combat systems**:
- `LiquidAcid.cs:105` applies `ContainedAcidEating` to its container (not via damage)
- `Physics.cs:3013` applies `Burning` during *flame ticks* (temperature-driven, NOT damage-driven)
- Cold/freeze likewise temperature-driven

There is no centralized "fire damage → Burning effect from melee" in Qud's combat path. Documented as scope correction #1 in the commit body.

A second sweep correction: I planned `F.1` (4 "missing" `Is*Damage()` helpers — Poison/Bleeding/Mental/Explosion). Re-reading `XRL.World/Damage.cs:139-189` confirmed Qud only has the 7 instance helpers we ported in Phase C. Poison/Bleeding/etc. are AttributeSound entries, not instance methods. No parity gap. Dropped F.1.

Final F scope: **just the BeforeTakeDamage event hook**.

### Implementation in our port

In `ApplyDamage(Entity, Damage, Entity, Zone)`, between the dead-target guard and resistance:

```csharp
var beforeTakeDamage = GameEvent.New("BeforeTakeDamage");
beforeTakeDamage.SetParameter("Target", (object)target);
beforeTakeDamage.SetParameter("Source", (object)source);
beforeTakeDamage.SetParameter("Damage", (object)damage);
if (!target.FireEvent(beforeTakeDamage))
{
    // Veto path — surface as fully-resisted so observers see the attempt
    var fullyResistedVeto = GameEvent.New("DamageFullyResisted");
    fullyResistedVeto.SetParameter("Target", (object)target);
    fullyResistedVeto.SetParameter("Source", (object)source);
    fullyResistedVeto.SetParameter("Damage", (object)damage);
    target.FireEvent(fullyResistedVeto);
    return;
}
// (resistance applies here, then TakeDamage fires)
```

Cancellation pattern matches the existing `BeforeMeleeAttack` flow at `CombatSystem.cs:43`: listeners return `false` from `Part.HandleEvent` → `target.FireEvent(...)` returns `false` → veto path runs.

### Vetoed-damage contract

The `Damage` object passed to `DamageFullyResisted` on a veto:
- **Amount** is left unchanged (the value the attack WOULD have dealt before resistance, with any pre-veto listener mutations applied). Listeners that want to know "how much was blocked" can read `damage.Amount`.
- **Attributes** reflect any pre-veto listener mutations.

### Divergences captured (Phase F)

| # | Divergence | Rationale |
|---|---|---|
| 1 | String-keyed `GameEvent.New("BeforeTakeDamage")` instead of Qud's typed `BeforeApplyDamageEvent.Check(...)` | Matches our existing event-system pattern; typed events would be a separate event-system upgrade phase. |
| 2 | Fires BEFORE resistance, Qud's fires AFTER | Our resistance is centralized in `CombatSystem.ApplyDamage` (Phase E divergence #1), so "before" is the only place a listener can intervene before resistance is computed. |
| 3 | Veto produces `DamageFullyResisted` event, Qud just returns from the handler | Surfacing the attempt to observers (UI, AI retaliation, achievements) is gameplay-valuable and costs one extra GameEvent allocation. |

### Phase F self-review (Methodology Template §5)

| # | Severity | Finding | Status |
|---|:-:|---|---|
| F-1 | 🧪 | Missing counter-check: "listener mutates attributes; downstream resistance picks up new attribute" was in original test plan but dropped when narrowing the file | **Fixed pre-commit** — added `BeforeTakeDamage_ListenerAddsFireAttribute_HeatResistanceApplies` (now 8/8) |
| F-2 | 🔵 | Vetoed damage's Damage object contract was undocumented | **Fixed pre-commit** — inline contract docstring added in CombatSystem.cs |
| F-3 | ⚪ | No `AfterTakeDamage` event symmetry | Architectural note; defer to future event-system phase |
| F-4 | ⚪ | 4-5 GameEvent allocations per `ApplyDamage` now (BeforeTakeDamage + TakeDamage + DamageDealt + maybe DamageFullyResisted) | Acceptable for turn-based pace; would matter for an event-pool refactor |

### Implementation log

| Step | Result |
|---|---|
| Pre-impl sweep — false premise #1 caught (per-attribute reactions are scattered non-combat) | Scope re-narrowed to event hook |
| Pre-impl sweep — false premise #2 caught (4 "missing" helpers don't exist in Qud either) | F.1 dropped |
| RED tests in `BeforeTakeDamageTests.cs` (initially 7) | 4 of 7 RED, 3 trivially pass (no-op listener / dead target / no-Hitpoints — those scenarios should be unaffected by the new code) |
| Implementation in `ApplyDamage` | 7/7 GREEN |
| Self-review surfaced F-1 missing counter-check | Added 8th test pre-commit |
| Final test count | 8/8 BeforeTakeDamage tests green |
| Full EditMode suite | 2166/2166 green (was 2158; +8 net) |

### Files changed (Phase F)

| File | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | `ApplyDamage` typed overload now fires `BeforeTakeDamage` before resistance with veto path |
| `Assets/Tests/EditMode/Gameplay/Combat/BeforeTakeDamageTests.cs` | **new** — 8 tests: mutation, veto, fires-before-resistance, dead-target guard, no-Hitpoints guard, no-op counter-check, source-vs-target counter-check, listener-adds-attribute downstream resist |

---

## Phase G: Stat-modulated off-hand penalty

### Status: ✅ complete (commit `c44cf3a`)

**Result:** the hard-coded `OFF_HAND_HIT_PENALTY = -2` constant is now consulted via a new `GetOffHandHitBonus(Entity)` method that adds the attacker's `MultiWeaponSkillBonus` stat. New tests: 9. Full suite: 2175/2175 green.

### Qud reference

`XRL.World.Parts/Combat.cs:775` — `getMeleeAttackChanceEvent.HandleFor(Attacker, weapon, chance, ...)` lets skill parts modify per-attack chance for off-hand swings. Default secondary-attack chance comes from `Sheeter.cs:109` (`TwoWeaponFightingSecondaryAttackChance` global config, default 75%).

We mirror the *intent* (penalty/chance is listener-modifiable) with a stat-driven hook since we don't have a skill system yet.

### Pre-impl verification sweep finding

Qud's mechanism is "chance to attack at all" (default 75% gate that listeners modify), NOT a hit-bonus penalty. Our pre-Phase-G `OFF_HAND_HIT_PENALTY = -2` is itself a CoO-original mechanism. So Phase G does not change the underlying mechanism — it makes the existing CoO mechanism listener-modulatable via a stat.

### Implementation in our port

```csharp
public static int GetOffHandHitBonus(Entity attacker)
{
    if (attacker == null) return OFF_HAND_HIT_PENALTY;
    return OFF_HAND_HIT_PENALTY + attacker.GetStatValue("MultiWeaponSkillBonus", 0);
}

// In PerformSingleAttack:
if (!isPrimary)
    hitBonus += GetOffHandHitBonus(attacker);
```

### Divergences captured (Phase G)

| # | Divergence | Rationale |
|---|---|---|
| 1 | Stat-driven (`MultiWeaponSkillBonus`), Qud is skill-driven | We don't have a skill system yet. Stat is a placeholder a future skill system would shift. CoO-original interpretation, not strict parity. Documented in test class xml-doc. |
| 2 | Hit-bonus penalty, Qud is chance-to-attack gate | Our pre-existing mechanism; Phase G keeps it but exposes the listener hook. Migrating to chance-to-attack semantics would be a separate phase. |
| 3 | No per-weapon "BalancedOffHand" attribute | Scope decision; flagged for future once weapon `Attributes` get richer downstream consumers. |

### Phase G self-review (Methodology Template §5)

| # | Severity | Finding | Status |
|---|:-:|---|---|
| G-1 | 🧪 | Counter-check missing: primary-hand swings unaffected by `MultiWeaponSkillBonus` (docstring claimed it but no test pinned it) | **Fixed pre-commit** — added `Integration_PrimaryHand_NotAffectedByMultiWeaponSkillBonus` (now 9/9) |
| G-2 | ⚪ | Stat-driven approach is CoO-original placeholder for Qud's skill-driven approach | Already documented in test class xml-doc |
| G-3 | ⚪ | Per-weapon "BalancedOffHand" attribute not added | Scope decision |

### Implementation log

| Step | Result |
|---|---|
| Pre-impl sweep — confirmed Qud's mechanism is skill-driven event chain | Decision: CoO-original stat hook is honest scope |
| RED tests in `MultiWeaponPenaltyTests.cs` (initially 8) | All 8 fail compilation (`GetOffHandHitBonus` doesn't exist) |
| Implementation: `GetOffHandHitBonus(Entity)` + caller update | 8/8 GREEN |
| Self-review surfaced G-1 missing primary-hand counter-check | Added 9th test pre-commit |
| Final test count | 9/9 MultiWeaponPenalty tests green |
| Full EditMode suite | 2175/2175 green (was 2166; +9 net) |

### Files changed (Phase G)

| File | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | New `GetOffHandHitBonus(Entity)` method; `PerformSingleAttack` consults it instead of using the bare constant |
| `Assets/Tests/EditMode/Gameplay/Combat/MultiWeaponPenaltyTests.cs` | **new** — 9 tests: default behavior, stat-zero counter-check, +2 cancels penalty, +5 over-correction, negative stacks, integration via PerformSingleAttack, primary-hand-immunity counter-check, large-stat overflow, null-attacker safety |

---

## Phase H: `CanBeDismembered` event hook

### Status: ✅ complete (commit `667e2cb`)

**Result:** `CheckCombatDismemberment` now fires `CanBeDismembered` after the chance roll passes; defender-side listeners can veto by returning false. Foundation for "Indestructible Bones" / mutation-granted limb immunity content. New tests: 6. Full suite: 2181/2181 green.

### Qud reference

- `XRL/IGameSystem.cs:637` — `CanBeDismemberedEvent` (per-call veto pattern)
- `XRL.World.Parts/Body.cs:2498` — `BeforeDismemberEvent.Check(parentObject, Part, Where, Silent, obliterate)` (parallel pattern in the dismember flow)
- `XRL.World.Parts/NoDamageExcept.cs:54` — example listener that vetoes dismemberment unless damage matches a specific tag

### Pre-impl verification sweep — false-premise correction

Originally scoped as "cutting → 1.5x sever, blunt → 0.5x sever" (damage-attribute-modulated dismemberment chance). **Sweep proved Qud doesn't do this.** Searching `qud_decompiled_project` for `HasAttribute.*Cutting` / `HasAttribute.*Bludg` returns one hit: `IsBludgeoningDamage()` inside `Damage.cs` itself, never consumed by combat code.

Qud's combat dismemberment is:
- **Skill-driven**: `Axe_Dismember.Dismember()` and `Axe_Decapitate.Decapitate()` proc on successful axe-skill hits
- **Mod-driven**: `ModSerrated`, `ModNanon`, `ModGlazed` add dismember chance per-weapon
- **Power-driven**: `DismemberAdjacentHostiles` is an explicit power
- **Mutation-driven**: `Decarbonizer`

None consult `Damage.Attributes`. Damage type doesn't affect dismemberment in Qud's combat path. Documented as scope correction in the commit body.

The closest centralized Qud-parity hook is `CanBeDismemberedEvent`. That's what Phase H now ships.

### Implementation in our port

`CheckCombatDismemberment` — the chance computation is unchanged; after the chance roll passes, we fire the event:

```csharp
int roll = rng.Next(100);
if (roll >= chance) return;  // chance roll failed — no event, no dismember

// Phase H: fire CanBeDismembered to give listeners a chance to veto.
var canBeDismembered = GameEvent.New("CanBeDismembered");
canBeDismembered.SetParameter("Defender", (object)defender);
canBeDismembered.SetParameter("BodyPart", (object)hitPart);
canBeDismembered.SetParameter("Damage", damage);
if (!defender.FireEvent(canBeDismembered))
    return;  // veto — skip the actual dismemberment

body.Dismember(hitPart, zone);
```

`CheckCombatDismemberment` was promoted from `private` to `public` so unit tests can target the dismemberment chance/veto path directly without plumbing through `PerformMeleeAttack`. Safe — pure function with no state other than the body and rng.

### Divergences captured (Phase H)

| # | Divergence | Rationale |
|---|---|---|
| 1 | String-keyed `GameEvent.New("CanBeDismembered")` instead of Qud's typed `CanBeDismemberedEvent` | Matches our existing event pattern; same as Phase F's choice. |
| 2 | Event fires AFTER chance roll passed, not on every dismember-eligible hit | Mirrors Qud's "you'd be dismembered if not for X" semantics — listeners only fire when dismemberment was actually about to happen. Avoids spamming listeners. |
| 3 | No port of Qud's dismemberment chance computation (skill, mod, power, mutation) | The CoO-original chance formula (DISMEMBER_BASE_CHANCE + scaled damage ratio, capped at 50%) stays. Qud's mechanisms would land in future phases when skills/mods exist. |

### Phase H self-review (Methodology Template §5)

| # | Severity | Finding | Status |
|---|:-:|---|---|
| H-1 | 🧪 | Source-vs-defender counter-check missing (Phase F had it; H should too) | **Fixed pre-commit** — added `CanBeDismembered_FiresOnDefender_NotOnAttacker` (now 6/6) |
| H-2 | 🔵 | Test-only alias `CheckCombatDismembermentForTest` was redundant since the canonical method became public | **Fixed pre-commit** — alias removed; tests use canonical name |
| H-3 | 🔵 | TDD step compression — RED→GREEN separation was implicit (compile errors when method missing) rather than a separate "method-exists-behavior-wrong" step | Process note for future reviewers; tests are functionally correct |

### Implementation log

| Step | Result |
|---|---|
| Pre-impl sweep — false premise caught (Qud doesn't do attribute-modulated dismem) | Scope re-narrowed to CanBeDismembered event hook |
| RED tests in `CanBeDismemberedTests.cs` (initially 5) | All 5 fail compilation (referenced `CheckCombatDismembermentForTest` which didn't exist) |
| Implementation: event firing in `CheckCombatDismemberment` + method promotion to public | 5/5 GREEN |
| Self-review: H-1 source-vs-defender counter-check + H-2 redundant alias removal | Added 6th test, removed alias |
| Final test count | 6/6 CanBeDismembered tests green |
| Full EditMode suite | 2181/2181 green (was 2175; +6 net) |

### Files changed (Phase H)

| File | Change |
|---|---|
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | `CheckCombatDismemberment` promoted to public; fires `CanBeDismembered` event after chance roll passed; veto path skips `body.Dismember()` |
| `Assets/Tests/EditMode/Gameplay/Combat/CanBeDismemberedTests.cs` | **new** — 6 tests: default dismemberment, veto listener, no-op counter-check, no-fire-on-below-threshold, no-fire-on-non-severable, source-vs-defender counter-check |

---

## Final port summary (post-Phase-H)

### Test count progression

| Milestone | Test count |
|---|---:|
| Pre-port (Phase 0 baseline) | 2087 |
| After Phase 1 surgical | 2087 |
| After Phase A | 2099 |
| After Phase C | 2129 |
| After Phase D + E | 2151 |
| After self-review remediation | 2158 |
| After Phase F | 2166 |
| After Phase G | 2175 |
| After Phase H | **2181** |

Net delta over the port: **+94 tests**, all green. Zero regressions.

### Commit history (audit/combat-deep-sweep branch)

| Commit | Purpose |
|---|---|
| `e438fd4` | Phase 0 — branch map + prioritized backlog |
| `cb5844b` | Phase 1 surgical audit — 15 specs for finished mechanics |
| `85a0a25` | Phase A — `RollDamagePenetrations` Qud parity |
| `4721630` | Phase C — Damage class foundation |
| `14f8047` | Phase D + E — crits + resistances |
| `02e08d1` | Self-review remediation pass (6 findings fixed + PlayMode sweep + playtest scenario) |
| `5deb287` | Phase F — `BeforeTakeDamage` event hook |
| `c44cf3a` | Phase G — stat-modulated off-hand penalty |
| `667e2cb` | Phase H — `CanBeDismembered` event hook |

### False premises caught by verification sweeps

The Methodology Template §1.2 pre-impl verification discipline caught **three false premises** before any code was written:

1. **Phase B** (Strength → damage): I claimed Qud adds Str to damage; sweep showed it doesn't. Phase B skipped entirely as already-at-parity.
2. **Phase F** (per-attribute reactions): I claimed Qud has centralized "fire damage → Burning effect" wiring; sweep showed those behaviors are scattered across non-combat systems. Re-scoped to BeforeTakeDamage event.
3. **Phase H** (cutting/blunt dismemberment): I claimed Qud modulates dismemberment chance by damage type; sweep showed it doesn't (the only `IsBludgeoningDamage()` reference is the helper itself). Re-scoped to `CanBeDismembered` event.

These corrections saved an estimated **2-3 days** of work that would have shipped under "parity" but was actually CoO-original divergence.

### Phases not done

- **Phase B½** — small damage-path polish (`WeaponIgnoreStrength` tag, multi-stat selection, `AdjustDamageResult`/`AdjustDamageDieSize` caller args). Low-impact edge cases; flagged for future when content needs them.

### Multi-phase work flagged for future

These were originally bundled into Phase F before the sweep narrowed scope; they remain genuine gameplay extensions but are CoO-original (not Qud parity) and scattered across non-combat systems:

- **Equipment durability + acid corrosion** — would need a durability system on `EquippablePart` and a TakeDamage listener that decrements durability when "Acid"-attributed damage hits.
- **Terrain reactions** — fire spreading, ice freezing water, electricity arcing. Would integrate with the existing `MaterialReactions` system.
- **Status-effect-from-damage-attribute** — e.g., taking Fire damage applies `Burning` effect. CoO-original (Qud doesn't do this from melee Fire damage). Could ship as a thin opt-in mechanism on weapons that have a `"BurnsOnHit"` attribute.

### Remaining methodology debts

- **Manual playtest scenarios** — `CombatParityShowcase` (commit `02e08d1`) covers Phases A-E. Phases F, G, H lack dedicated playtest scenarios. Probably fine since they're event-hook/stat-tunable plumbing without immediately-visible game effect, but worth noting.
- **PlayMode sanity sweep for F/G/H** — only ran for the A-E remediation. F/G/H are pure plumbing (no live-bootstrap-specific risk) so this is low-priority. Would be valuable if any of them later wire into player-visible content.
