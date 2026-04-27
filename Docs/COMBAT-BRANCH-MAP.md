# CombatSystem Branch Map (Phase 0)

**Date:** 2026-04-26
**Branch:** `audit/combat-deep-sweep`
**Source:** `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` (677 LOC)

This document enumerates every decision branch in `CombatSystem.cs` and notes whether existing tests exercise it. Companion to `Docs/COMBAT-TEST-BACKLOG.md` (the prioritized work-list this map produces).

---

## Test files that touch CombatSystem (10 files)

Coverage is broader than the 37 tests in `CombatSystemTests.cs` alone. The full surface is exercised across:

| File | Tests | What it covers |
|---|---:|---|
| `CombatSystemTests.cs` | 37 | DiceRoller (7), StatUtils (6), MessageLog (2), MeleeWeaponPart/ArmorPart defaults (2), GetDV (3), GetAV (2), RollPenetrations (2), MeleeAttack (8), HandleDeath (5) |
| `AdversarialColdEyeTests.cs` | 8 | The original cross-cut adversarial probe — `ApplyDamage` edges (zero, negative, null target, exceeds-HP, no-Hitpoints-stat, fires-events, null-source, already-dead). Found the 2 bugs that motivated this audit |
| `WitnessedEffectTests.cs` | 2 (Combat-side) | `HandleDeath_BroadcastsWitness_ToNearbyPassiveNpcs` + `HandleDeath_DoesNotShakeActiveCombatants` |
| `M1AdversarialTests.cs` | (cross-refs) | Touches CombatSystem via WitnessedEffect death tests |
| `BodyPartSystemTests.cs` | (cross-refs) | Anatomy generation, not Combat × Body integration |
| `CorpsePartTests.cs` | (cross-refs) | M5 — corpse spawned via Died event chain |
| `TriggerOnStepPartTests.cs` | (cross-refs) | M6 — mover-dies-mid-sweep break loop |
| `InventorySystemTests.cs` | (cross-refs) | Inventory integration |
| `DisposeOfCorpseGoalTests.cs` | (cross-refs) | M5 goal-side |
| `ScenarioContextExtensionsTests.cs` | (cross-refs) | Test harness |

**Total Combat-touching tests: ~50-55** (37 in primary file + ~15-18 cross-refs).

---

## Public API (8 entry points)

1. `PerformMeleeAttack(attacker, defender, zone, rng)` → bool
2. `GetDV(entity)` → int
3. `GetAV(entity)` → int
4. `RollPenetrations(pv, av, rng)` → int
5. `ApplyDamage(target, amount, source, zone)` → void
6. `HandleDeath(target, killer, zone)` → void
7. `SelectHitLocation(body, rng)` → BodyPart
8. `GetPartAV(entity, hitPart)` → int

Plus ~8 private helpers (`PerformBodyPartAwareAttack`, `PerformLegacyAttack`, `PerformSingleAttack`, `GatherMeleeWeapons`, `GetEffectiveArmor`, `BroadcastDeathWitnessed`, `DropInventoryOnDeath`, `CheckCombatDismemberment`).

---

## Branch coverage by method

Legend: ✅ covered (≥1 test asserts this branch) · ⚠️ partial (covered in passing but not asserted) · ❌ uncovered

### `PerformMeleeAttack` (lines 33-53) — 4 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B1 | `attacker == null \|\| defender == null` early return | ❌ | No test. Defensive guard. |
| B2 | `BeforeMeleeAttack` event handler returns false | ✅ | `MeleeAttack_BeforeMeleeAttack_CanCancel` |
| B3 | `body != null` → BodyPartAware path | ⚠️ | Implicit via `MeleeAttack_DealsDamage`, but no test specifically asserts the BodyPartAware route was taken |
| B4 | `body == null` → Legacy path | ✅ | `MeleeAttack_DealsDamage` uses `CreateCreature` which has no Body |

### `PerformBodyPartAwareAttack` (lines 59-86) — 5 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B5 | `weapons.Count == 0` → punch attack | ❌ | **Zero tests.** The "no weapons" path is unverified. |
| B6 | per-weapon attack loop | ❌ | **Zero tests.** Multi-weapon attack ordering/fairness unverified. |
| B7 | `defender.HP <= 0` mid-loop → break | ❌ | **Zero tests.** Mid-loop early termination unverified — could wrong-fail tests if a 2-weapon attacker kills with weapon 1 and weapon 2 also tries. |
| B8 | weapon name resolution chain (Entity → BaseDamage → "fist") | ❌ | Cosmetic but visible in MessageLog |
| B9 | hand name fallback to "hand" | ❌ | Cosmetic |

### `PerformLegacyAttack` (lines 91-107) — 4 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B10 | `attackerInventory != null` → inventory check | ✅ | Implicit via tests |
| B11 | `equippedWeapon != null` → use equipped | ⚠️ | Not directly asserted |
| B12 | `weapon == null` (after inventory) → fall back to attacker.GetPart | ✅ | `MeleeAttack_DealsDamage` (attacker has MeleeWeaponPart, no inventory) |
| B13 | weapon stays null → null weapon used in PerformSingleAttack | ⚠️ | Indirect |

### `PerformSingleAttack` (lines 112-203) — 19 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B14 | `weapon?.BaseDamage` ?? "1d2" default | ⚠️ | Indirect |
| B15-18 | `weapon?.HitBonus / PenBonus / MaxStrengthBonus / Stat` defaults | ⚠️ | Indirect via tests with set values |
| B19 | `!isPrimary` → off-hand penalty | ❌ | **Zero tests.** OFF_HAND_HIT_PENALTY is dead-coverage. |
| B20 | `attackSourceDesc != null` → format with srcTag | ❌ | Cosmetic |
| B21 | `naturalTwenty = hitRoll == 20` | ❌ | **Zero tests.** Critical-hit path bypasses DV. |
| B22 | `!naturalTwenty && totalHit < dv` → miss | ✅ | `MeleeAttack_CanMiss` |
| B23 | `defenderCell != null` → emit miss particle | ⚠️ | Asserted by AsciiFx integration, not unit-tested |
| B24 | `defenderBody != null` → SelectHitLocation | ❌ | **Zero direct tests.** |
| B25 | `hitPart != null` → format partDesc | ⚠️ | Indirect |
| B26 | `maxStrBonus >= 0 && strMod > maxStrBonus` → cap strMod | ❌ | **Zero tests.** Strength-cap logic unverified. |
| B27 | `hitPart != null` → use GetPartAV; else GetAV | ❌ | **Zero tests.** Per-part vs global AV branching unverified. |
| B28 | `penetrations == 0` → "fails to penetrate" return | ❌ | **Zero tests.** Failed-penetration message path unverified. |
| B29 | damage roll loop (per penetration) | ⚠️ | Implicit |
| B30 | `totalDamage <= 0` → "deals no damage" return | ❌ | **Zero tests.** Possible if damage dice rolls 0+0+... |
| B31 | ApplyDamage call | ✅ | Many tests |
| B32 | `hitCell != null` → emit floating damage | ❌ | Cosmetic |
| B33 | `hpAfter > 0` AND `hitPart != null` → CheckCombatDismemberment | ❌ | **Zero direct tests.** Dismemberment integration unverified. |

### `GatherMeleeWeapons` (lines 210-267) — 8 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B34 | per-part loop | ❌ | **Zero direct tests.** |
| B35 | `part.Type != "Hand"` → continue | ❌ | Skip-non-hand path unverified |
| B36 | `part._Equipped != null && part.FirstSlotForEquipped` | ❌ | **Zero tests.** First-slot-only logic unverified. |
| B37 | equipped has MeleeWeaponPart → add | ❌ | |
| B38 | `part._DefaultBehavior != null && part.FirstSlotForDefaultBehavior` | ❌ | **Zero tests.** Natural weapons (claws, fists) untested. |
| B39 | default has MeleeWeaponPart → add | ❌ | |
| B40 | post-loop sort: primary first | ❌ | **Zero tests.** Primary-ordering unverified. |
| B41 | `result.Count > 0 && !result[0].IsPrimary` → mark first as primary | ❌ | Edge case: weapons exist but none marked primary |

### `GetDV` (lines 273-299) — 3 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B42 | `body != null` → ForeachEquippedObject sum DV | ❌ | **Zero tests.** Body-aware DV path unverified. |
| B43 | `armor.DV != 0` → add to bestDV (note: variable named `bestDV` but logic is sum, not max) | ❌ | |
| B44 | else (no body) → GetEffectiveArmor → add DV | ✅ | `GetDV_*` tests |

### `GetAV` (lines 305-328) — 4 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B45 | `body != null` → ForeachEquippedObject sum AV | ❌ | **Zero tests.** Body-aware AV path unverified. |
| B46 | `armor != null` → add to totalAV | ❌ | |
| B47 | also natural armor → add naturalArmor.AV | ❌ | |
| B48 | else (no body) → GetEffectiveArmor → return AV ?? 0 | ✅ | `GetAV_*` tests |

### `GetEffectiveArmor` (lines 333-343) — 3 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B49 | `inventory != null` → check equipped | ⚠️ | Indirect |
| B50 | `equippedArmor != null` → return its part | ⚠️ | Indirect |
| B51 | fall through → entity.GetPart<ArmorPart> | ✅ | Common path |

### `RollPenetrations` (lines 349-377) — 4 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B52 | per-roll loop | ✅ | `Penetrations_*` tests |
| B53 | `roll > av` → penetration++, streak++ | ✅ | |
| B54a | `streak == rollsInSet` → reset, decrement PV by 2, restart loop | ❌ | **Zero tests.** The "all 3 succeeded → roll again with PV-2" rule is the most subtle bit and is **untested**. Possibly subtly broken — uses `i = -1` for restart which is unusual C#. |
| B54b | `currentPV + 8 <= av` → break (early termination) | ❌ | Untested |

### `ApplyDamage` (lines 383-435) — 7 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B55 | `target == null \|\| amount <= 0` → return | ✅ | `ApplyDamage_AmountZero_IsNoOp`, `_NegativeAmount_DoesNotHeal`, `_NullTarget_DoesNotThrow` |
| B56 | `hpStat == null \|\| hpStat.BaseValue <= 0` → return (post-65df19c fix) | ✅ | `ApplyDamage_NoHitpointsStat_DoesNotCrashOrKill`, `_OnAlreadyDeadTarget_DoesNotReFireDeath` |
| B57 | TakeDamage event fired | ✅ | `ApplyDamage_FiresTakeDamageEvent_OnTarget` |
| B58 | `hpStat.BaseValue -= amount` | ✅ | All damage tests |
| B59 | HP alias: `hpAlias != null && !ReferenceEquals(hpAlias, hpStat)` → also decrement | ❌ | **Zero tests.** Unusual alias-stat path. Almost certainly dead code, but dead-code is its own bug class. |
| B60 | `source != null` → fire DamageDealt event | ✅ | `ApplyDamage_FiresDamageDealtEvent_OnNonNullSource`, `_NullSource_NoDamageDealtFiredAnywhere` |
| B61 | `hpStat.BaseValue <= 0` → HandleDeath | ✅ | `ApplyDamage_AmountExceedsCurrentHP_TriggersDeath`, `MeleeAttack_KillsTarget` |

### `HandleDeath` (lines 442-488) — 9 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B62 | `killer?.GetDisplayName() ?? "something"` | ⚠️ | Indirect via DeathMessage test |
| B63 | `killer.HasTag("Player")` → AwardKillXP | ❌ | **Zero direct tests.** XP-on-kill path unverified. |
| B64 | `zone != null` → drop equipment branch | ✅ | `HandleDeath_NoZone_NoCrash` |
| B65 | `body != null` → DropAllEquipment | ❌ | **Zero direct tests.** Body-aware drop is the modern path. |
| B66 | `inventory != null` → DropInventoryOnDeath | ✅ | `HandleDeath_DropsCarriedItems` etc. |
| B67 | DeathSplatterFx.Emit (always — even with zone null?) | ❌ | **Verify**: does `DeathSplatterFx.Emit` defend against null zone? |
| B68 | Died event fired | ✅ | Implicit via M5/M6 corpse tests, not direct |
| B69 | `zone != null` → BroadcastDeathWitnessed | ✅ | `WitnessedEffectTests.HandleDeath_BroadcastsWitness*` |
| B70 | `zone != null` → RemoveEntity | ✅ | `MeleeAttack_KillsTarget` asserts removed |

### `BroadcastDeathWitnessed` (lines 499-533) — 11 branches

Mostly covered by `WitnessedEffectTests.cs`:

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B71 | `zone == null \|\| deceased == null` early return | ✅ | Implicit via NoZone test |
| B72 | `deathCell == null` early return | ❌ | Edge case (deceased not in zone) |
| B73 | per-witness loop | ✅ | |
| B74 | `witness == deceased` → continue | ✅ | Self-skip |
| B75 | `witness == killer` → continue | ✅ | Killer-skip; null killer is fine |
| B76 | `!witness.HasTag("Creature")` → continue | ✅ | |
| B77 | `brain == null \|\| !brain.Passive` → continue | ✅ | `HandleDeath_DoesNotShakeActiveCombatants` |
| B78 | `wCell == null` → continue | ❌ | Edge: witness-not-in-zone |
| B79 | `dist > radius` → continue | ✅ | Indirect |
| B80 | `!HasLineOfSight` → continue | ✅ | M1Adversarial covers this |
| B81 | ApplyEffect WitnessedEffect | ✅ | |

### `DropInventoryOnDeath` (lines 539-568) — 7 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B82 | `pos.x < 0 \|\| pos.y < 0` → return | ❌ | Edge: target not in zone |
| B83 | per-equipped-item loop | ⚠️ | Indirect |
| B84 | `physics != null` → null-out Equipped + InInventory | ❌ | Edge: item without PhysicsPart |
| B85 | AddEntity to zone | ✅ | |
| B86 | clear EquippedItems dict | ⚠️ | |
| B87 | per-carried-item loop | ✅ | |
| B88 | RemoveObject + AddEntity | ✅ | |

### `SelectHitLocation` (lines 587-614) — 8 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B89 | per-part total-weight loop | ❌ | **Zero direct tests.** |
| B90 | `parts[i].Abstract` → continue | ❌ | |
| B91 | `parts[i].TargetWeight <= 0` → continue | ❌ | |
| B92 | `totalWeight <= 0` → return null | ❌ | **Zero tests.** Edge: no targetable parts. |
| B93 | per-part selection loop | ❌ | |
| B94 | (same exclusions as B90/B91 in selection loop) | ❌ | |
| B95 | `roll < cumulative` → return part | ❌ | |
| B96 | fall-through → return null | ❌ | Edge: shouldn't happen if B92 doesn't fire |

### `GetPartAV` (lines 620-637) — 3 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B97 | `hitPart._Equipped != null` → check armor | ❌ | **Zero direct tests.** |
| B98 | `armor != null` → add av | ❌ | |
| B99 | natural armor → add av | ❌ | |

### `CheckCombatDismemberment` (lines 643-665) — 5 branches

| # | Branch | Coverage | Notes |
|---|---|---|---|
| B100 | `!hitPart.IsSeverable()` → return | ❌ | **Zero tests.** |
| B101 | `hitPart.Mortal` → 2x threshold | ❌ | |
| B102 | `damageRatio < threshold` → return | ❌ | |
| B103 | chance calculation: `BASE + (int)(excess * 50)`, capped at 50 | ❌ | |
| B104 | `roll < chance` → Dismember | ❌ | |

---

## Summary statistics

| | |
|---|---:|
| Total decision branches | ~104 |
| Branches with explicit assertion (✅) | ~30 (29%) |
| Branches with partial/indirect coverage (⚠️) | ~14 (13%) |
| Branches with NO coverage (❌) | ~60 (58%) |
| Public methods with zero direct tests | 3 (`SelectHitLocation`, `GetPartAV`, the body-aware path of `PerformMeleeAttack`) |
| Private helpers with zero direct tests | 4 (`GatherMeleeWeapons`, `CheckCombatDismemberment`, plus the `PerformBodyPartAwareAttack` and `PerformSingleAttack` body-aware path) |

**Headline finding**: about **58% of CombatSystem.cs's branches have zero direct test assertions**. The legacy attack path (no Body) is well-covered. The body-part-aware path — which is the runtime default for any creature with anatomy — is largely uncovered.

---

## High-complexity zones (recon's highest-priority callouts)

These are the zones I'd flag as most likely to harbor bugs:

1. **`PerformBodyPartAwareAttack` + `GatherMeleeWeapons`** — multi-weapon ordering, primary/off-hand, default-behavior fallback. The runtime default; zero direct tests; subtle logic around `FirstSlotForEquipped` / `FirstSlotForDefaultBehavior` / `IsPrimary` resolution.

2. **`PerformSingleAttack` body-aware branches** — B19 (off-hand penalty), B21 (natural-twenty), B26 (strength cap), B27 (per-part vs global AV), B30 (totalDamage <= 0), B33 (dismemberment integration). These are runtime-default paths with no direct tests.

3. **`RollPenetrations` streak rule** (B54a/b) — the "all 3 succeed, drop PV by 2, restart" logic is the most subtle math in the file and is fully untested. The use of `i = -1` to restart the for-loop is unusual C# and could harbor an off-by-one.

4. **`SelectHitLocation`** — pure function, weighted-random selection, zero direct tests. Deterministic given seeded RNG; trivial to write spec tests for.

5. **`HandleDeath` ordering** (B65 body-drop vs B66 inventory-drop vs B67 splatter vs B68 Died event vs B69 broadcast vs B70 RemoveEntity) — the documented order is enforced by code, but no test asserts the order. A regression here would be silent until a Died handler tries to read state that was already torn down.

6. **`CheckCombatDismemberment`** — chance-calculation logic (B103) is integer arithmetic with float intermediates; potentially off-by-one.

7. **`HP alias`** in `ApplyDamage` (B59) — the `target.GetStat("HP")` second-decrement is unusual; if it's dead code, it's noise; if it's load-bearing, removing the original Hitpoints stat would silently leak.

8. **`DeathSplatterFx.Emit` with null zone** (B67) — fires unconditionally before the `zone != null` checks for drop-equipment. Need to verify Emit is null-safe.

---

## Recommendation for Phase 1+

Given that **58% of branches lack direct coverage** (vs ~5-10% expected for a mature audited surface), the test-writing phases have a deep well to draw from. The backlog (`Docs/COMBAT-TEST-BACKLOG.md`) prioritizes the gaps.

**Audit budget probably stays at the planned 2-3 days.** The branch map confirms substantial coverage gaps but does not reveal any architectural surprise that would force a re-plan. Phase 1's 25-30 tests can comfortably target the highest-priority gaps; Phase 2 picks up integration boundaries; Phase 3 picks up adversarial edges.
