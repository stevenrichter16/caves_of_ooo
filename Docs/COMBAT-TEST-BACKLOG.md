# CombatSystem Test Backlog (Phase 0)

**Date:** 2026-04-26
**Branch:** `audit/combat-deep-sweep`
**Output of:** `Docs/COMBAT-BRANCH-MAP.md` (read that first)
**Drives:** Phases 1 (spec), 2 (cross-system), 3 (adversarial)

This backlog translates the branch map's coverage gaps into a prioritized, phase-tagged work-list. Each entry is one test candidate.

---

## Entry format

```
[#] (TARGET, GAP_SEVERITY 1-5, BUG_CLASS, PHASE) — TEST_SHAPE
```

- **TARGET** — the branch (B##) or touchpoint being probed
- **GAP_SEVERITY** — 1 = trivially defensible / 5 = high blast radius if bug
- **BUG_CLASS** — what kind of bug this would catch
- **PHASE** — P1 (spec) / P2 (cross-system) / P3 (adversarial) / P4 (property)
- **TEST_SHAPE** — one-line description of the test

---

## Phase 1 — Spec-first (canonical contract, ~25 entries)

The 25-30 tests Phase 1 will pull from. Predictions made cold. These probe documented behavior and well-defined contracts — NOT edges.

### `PerformBodyPartAwareAttack` — body-aware path baseline

```
[ 1] (B3, B5,    GAP=4, NO_WEAPONS_PUNCH,           P1) — Attacker with Body but no equipped weapons + no DefaultBehavior weapons → punch attack still fires; defender takes ≥0 damage; no NPE
[ 2] (B6,        GAP=4, MULTI_WEAPON_FIRES_BOTH,    P1) — Attacker with two equipped weapons (left + right hand, both primary-eligible) → both attacks fire in one PerformMeleeAttack
[ 3] (B7,        GAP=4, MIDLOOP_DEFENDER_DEAD_BREAK, P1) — Two-weapon attack where weapon-1 kills the defender → weapon-2 attack does NOT fire (no message, no AsciiFx)
[ 4] (B8,        GAP=2, WEAPON_NAME_RESOLUTION,     P1) — Weapon ParentEntity has DisplayName "saber" → MessageLog shows [hand: saber], not [hand: 1d6]
```

### `GatherMeleeWeapons` — selection logic

```
[ 5] (B35,       GAP=3, SKIP_NON_HAND_PARTS,        P1) — Body has Foot/Head with attached items; only Hand-typed parts contribute weapons
[ 6] (B36,       GAP=4, FIRST_SLOT_ONLY,            P1) — Hand has _Equipped weapon AND part.FirstSlotForEquipped=true → weapon used. If FirstSlotForEquipped=false (paired hand) → weapon NOT used
[ 7] (B38,       GAP=4, NATURAL_WEAPON_FALLBACK,    P1) — Hand has no _Equipped but has _DefaultBehavior with MeleeWeaponPart → natural weapon used
[ 8] (B40, B41,  GAP=3, PRIMARY_ORDERING,           P1) — Two weapons, neither marked Primary → result[0].IsPrimary becomes true after sort
[ 9] (B40,       GAP=3, OFFHAND_AFTER_PRIMARY,      P1) — Two weapons, one Primary + one not → Primary attacks first (index 0)
```

### `PerformSingleAttack` — body-aware branches

```
[10] (B19,       GAP=3, OFF_HAND_PENALTY_APPLIED,   P1) — Off-hand attack: hitBonus reduced by OFF_HAND_HIT_PENALTY (-2). Verify by setting attacker stats so off-hand attack misses but primary hits
[11] (B21,       GAP=4, NATURAL_TWENTY_BYPASSES_DV, P1) — hitRoll == 20 against an impossibly high DV (50) → still hits. Probes the !naturalTwenty short-circuit
[12] (B26,       GAP=3, MAX_STRENGTH_BONUS_CAP,     P1) — weapon.MaxStrengthBonus = 1, attacker has Str 20 (mod +2) → strMod capped at 1. Verify damage rolls reflect cap
[13] (B27,       GAP=4, PER_PART_AV_USED,           P1) — Defender with Body and equipped armor on hit-location → uses GetPartAV (sums armor.AV + naturalArmor.AV), not GetAV
[14] (B28,       GAP=3, PENETRATION_ZERO_MESSAGE,   P1) — High AV vs low PV → "fails to penetrate" message logged, no damage applied
[15] (B30,       GAP=3, DAMAGE_ROLLS_TO_ZERO,       P1) — Damage dice "1d1-1" with 1 penetration → totalDamage = 0 → "deals no damage" message, no death
```

### `SelectHitLocation` — weighted random

```
[16] (B89-B91,   GAP=4, ABSTRACT_PARTS_EXCLUDED,    P1) — Body with one Abstract part + one non-abstract → Abstract never selected over 1000 rolls
[17] (B89-B91,   GAP=4, ZERO_TARGET_WEIGHT_EXCLUDED, P1) — Body with TargetWeight=0 part → never selected
[18] (B92,       GAP=4, NO_VALID_TARGETS_RETURNS_NULL, P1) — All parts Abstract OR TargetWeight=0 → returns null
[19] (B95,       GAP=3, WEIGHTED_DISTRIBUTION,      P1) — Two parts (weight 2, weight 8) over 10000 rolls → distribution ≈ 20/80 ± 2%
```

### `GetPartAV` — per-part armor

```
[20] (B97-B99,   GAP=4, EQUIPPED_PLUS_NATURAL,      P1) — hitPart with equipped armor (AV=3) + entity has natural ArmorPart (AV=2) → returns 5
[21] (B97,       GAP=3, NO_EQUIPPED_NO_NATURAL,     P1) — hitPart with no _Equipped + entity has no ArmorPart → returns 0
```

### `RollPenetrations` — streak math

```
[22] (B54a,      GAP=5, STREAK_TRIGGERS_RESTART,    P1) — PV high enough to win 3-in-a-row, AV calibrated so PV-2 still beats AV most of time → over many trials, mean penetrations > 3 (proves streak resets and re-rolls fire)
[23] (B54b,      GAP=4, EARLY_TERMINATION,          P1) — PV high but AV calibrated so currentPV+8 ≤ AV after one decrement → early break, max ~3 penetrations
```

### `HandleDeath` — happy-path coverage

```
[24] (B63,       GAP=4, PLAYER_KILLER_AWARDS_XP,    P1) — Killer has "Player" tag → LevelingSystem.AwardKillXP called (verify XP stat increased on killer)
[25] (B65,       GAP=5, BODY_AWARE_EQUIPMENT_DROP,  P1) — Defender has Body with equipped armor → body.DropAllEquipment called; armor in zone after death (the runtime-default path; CURRENTLY ZERO TESTS)
```

---

## Phase 2 — Cross-system integration matrix (~30-40 entries)

Tier A (deep probe), Tier B (canonical probe), Tier C (smoke test only).

### Tier A: StatusEffects × Combat (4-5 tests)

```
[26] (TC×SE,     GAP=5, DOT_KILLS_VIA_TICK,          P2) — Apply PoisonedEffect → wait for OnTurnStart → defender HP→0 → HandleDeath fires with `source` properly attributed to poison source (or null if env)
[27] (TC×SE,     GAP=5, EFFECT_REMOVED_DURING_DEATH, P2) — Defender has SmolderingEffect; Died event handler attempts RemoveEffect → no NPE, no zombie effect
[28] (TC×SE,     GAP=4, ALLOW_ACTION_GATE_VS_DAMAGE, P2) — Frozen defender (AllowAction=false) — does ApplyDamage still apply? Should yes (damage isn't "action"); pin contract
[29] (TC×SE,     GAP=4, EFFECT_KILLED_KILLER,        P2) — Defender's SmolderingEffect's OnTurnStart applies smolder back to killer → killer dies on their turn → HandleDeath fires correctly
[30] (TC×SE,     GAP=4, AURA_ACTIVE_AFTER_HANDLEDEATH, P2) — Killed entity had IAuraProvider effect → does AsciiFxBus.AuraStop fire on the way out? Or does the aura linger?
```

### Tier A: Inventory × Combat (3-4 tests)

```
[31] (TC×Inv,    GAP=5, EQUIPPEDITEMS_VS_OBJECTS,    P2) — Defender's EquippedItems[slot] points to instance NOT in Inventory.Objects (corrupted state) → DropInventoryOnDeath behavior: drop both, drop one, or NPE?
[32] (TC×Inv,    GAP=4, INVENTORY_NULL_PHYSICS,      P2) — Equipped item has no PhysicsPart → loop continues without NPE
[33] (TC×Inv,    GAP=4, ZERO_INVENTORY_DROP,         P2) — Defender has empty inventory + no equipment → HandleDeath completes without NPE (already covered indirectly; pin explicitly)
[34] (TC×Inv,    GAP=3, DROP_ORDER_PRESERVED,        P2) — Multiple inventory items → all land in same cell. Order is implementation-defined but pin observed order as contract
```

### Tier A: Body × Combat (4-5 tests)

```
[35] (TC×Body,   GAP=5, DISMEMBER_REMOVES_PART,      P2) — Defender takes high-damage hit on Severable Hand → CheckCombatDismemberment fires → Hand removed from body tree (via Body.Dismember)
[36] (TC×Body,   GAP=4, DISMEMBER_DOES_NOT_KILL,     P2) — Severable Hand dismembered when defender survives the blow → defender HP > 0; both events (damage + dismemberment) fired correctly
[37] (TC×Body,   GAP=4, DISMEMBER_MORTAL_2X_THRESHOLD, P2) — Mortal part hit at 25% damage ratio → no dismemberment; at 50% → possible. Mortal threshold = 2x base
[38] (TC×Body,   GAP=4, NON_SEVERABLE_NO_DISMEMBER,  P2) — Hit on non-Severable part → CheckCombatDismemberment returns immediately, no roll
[39] (TC×Body,   GAP=3, BODY_DROPALLEQUIPMENT,       P2) — Body.DropAllEquipment integration: equipped items end up in zone at death cell, EquippedItems dict cleared on body parts
```

### Tier A: Save × Combat (2-3 tests)

```
[40] (TC×Save,   GAP=4, MID_DEATH_SAVE_LOAD,         P2) — Save right after BeforeMeleeAttack but before damage → Load → game state consistent (no half-fired event remnants)
[41] (TC×Save,   GAP=4, DEAD_ENTITY_NOT_IN_SAVE,     P2) — Defender killed → save → load → entity gone (already removed from zone, not in save graph)
[42] (TC×Save,   GAP=3, KILLER_REF_SURVIVES_LOAD,    P2) — BrainPart.Target = recently-killed entity, save before zone removal completes → load → reference is null or zombie?
```

### Tier A: CorpsePart × Combat (1-2 tests)

```
[43] (TC×Corpse, GAP=4, CORPSE_SPAWN_AT_DEATH_CELL,  P2) — Defender with CorpsePart dies → Died event fires → corpse appears at defender's old cell BEFORE zone.RemoveEntity
[44] (TC×Corpse, GAP=4, CORPSE_RESPECTS_DROP_ORDER,  P2) — Equipment drop happens BEFORE corpse spawn → both end up in same cell, order preserved
```

### Tier B: Faction × Combat (2 tests)

```
[45] (TC×Fac,    GAP=3, KILLER_NO_FACTION_NO_REP,    P2) — Killer with no Faction tag → kill happens, no faction-rep change attempted
[46] (TC×Fac,    GAP=3, PLAYER_KILLS_HOSTILE,        P2) — Player kills entity from hostile faction → reputation handling per FactionManager (pin observed behavior)
```

### Tier B: BrainPart × Combat (2 tests)

```
[47] (TC×Brain,  GAP=3, KILLER_TARGET_IS_DECEASED,   P2) — Killer's BrainPart.Target == defender → after kill, Target reference handling (cleared? stale?)
[48] (TC×Brain,  GAP=3, PERSONAL_ENEMIES_UPDATE,     P2) — Killer has defender in PersonalEnemies → after kill, defender removed from list
```

### Tier B: TurnManager × Combat (2 tests)

```
[49] (TC×Turn,   GAP=3, CURRENTACTOR_DIES_MIDTURN,   P2) — TurnManager.CurrentActor takes lethal DoT mid-turn → turn proceeds without NPE; entity removed from queue
[50] (TC×Turn,   GAP=2, QUEUED_ACTOR_REMOVED,        P2) — Entity in turn queue is killed by another → next turn, deceased not picked
```

### Tier B: Zone × Combat (1 test)

```
[51] (TC×Zone,   GAP=3, HANDLEDEATH_NO_CELL,         P2) — HandleDeath called on entity already removed from zone (zone.GetEntityCell returns null) → no NPE, no double-remove
```

### Tier C: smoke tests (5 tests)

```
[52] (TC×Msg,    GAP=2, NULL_DISPLAY_NAMES,          P2) — attacker.GetDisplayName() returns null/empty → MessageLog formats without NPE
[53] (TC×Fx,     GAP=2, SPLATTER_NULL_ZONE,          P2) — DeathSplatterFx.Emit with zone=null → no-op (probably already null-safe; verify)
[54] (TC×Lvl,    GAP=2, XP_FOR_ZERO_XP_TARGET,       P2) — Player kills entity with XP value 0 → no error, no negative XP awarded
[55] (TC×Wit,    GAP=2, BROADCAST_NO_PASSIVE_NEARBY, P2) — Death in zone with zero Passive NPCs → BroadcastDeathWitnessed completes silently
[56] (TC×Mut,    GAP=2, MUTATION_DIED_LISTENER,      P2) — Mutation that listens for Died event of self/others — pin behavior (probably no current mutation does this; smoke test if any do)
```

---

## Phase 3 — Adversarial cold-eye (~14 entries)

Predictions made BEFORE re-reading code.

### Numerical extremes

```
[57] (B58,       GAP=3, DAMAGE_INT_MAXVALUE,         P3) — ApplyDamage(target, int.MaxValue, ...) → HP underflows to negative? PRED: yes, then triggers HandleDeath via B61
[58] (B58,       GAP=3, DAMAGE_INT_MINVALUE,         P3) — ApplyDamage(target, int.MinValue, ...) → ApplyDamage's amount<=0 guard fires (B55) → no-op. PRED: silent no-op
[59] (B58,       GAP=4, DAMAGE_EXACTLY_HP,           P3) — ApplyDamage(target, target.HP, ...) → HP exactly 0 → B61 fires (`<= 0`). PRED: dies. Pin == vs <
[60] (B58,       GAP=3, REPEATED_DAMAGE_ZERO,        P3) — 100 calls of ApplyDamage(target, 0, ...) → events fire 0 times (B55 short-circuits). Pin event-fire suppression
```

### Reference graph + re-entrancy

```
[61] (B61,       GAP=4, SELF_KILL_MESSAGE,           P3) — ApplyDamage(target, 999, target, ...) → killer == target → death message format: "{name} is killed by {name}!" or self-name? PRED: just the name twice
[62] (B61,       GAP=5, REENTRANCY_DIED_HANDLER_DAMAGES_KILLER, P3) — Died event handler does ApplyDamage(killer, killer.HP). PRED: killer dies inside HandleDeath; double HandleDeath stack frame; M6-style break may not save us here
[63] (B61,       GAP=5, REENTRANCY_DIED_ADDS_LETHAL_EFFECT, P3) — Died handler applies lethal SmolderingEffect to killer → next tick, killer dies → double-Died chain bounded?
[64] (B70,       GAP=4, HANDLEDEATH_TWICE_GUARDED,   P3) — Direct call HandleDeath(target, killer, zone) twice → second call: hpStat already <= 0 (already handled by 65df19c) so should silently pass (since ApplyDamage gated, but HandleDeath itself is still callable). What happens? PRED: second call duplicates message, drops nothing (inventory already empty), fires Died again, removes-already-removed-entity. **Possibly bug.**
[65] (B65,       GAP=4, EQUIPMENT_DANGLING_INSTANCE, P3) — DropInventoryOnDeath's `EquippedItems` contains entity X; X is also in another zone's cell (corrupted state) → AddEntity at death cell creates duplicate? PRED: yes
```

### Concurrent / boundary

```
[66] (TC×M6,     GAP=4, TWO_RUNES_SAME_CELL,         P3) — Walking onto two co-located runes in one tick → HandleDeath called twice via the M6 Contains-break path. PRED: break loop fires, only one death. Regression-pin.
[67] (B22,       GAP=3, MISS_NULL_ZONE_NO_PARTICLE,  P3) — Miss with zone=null → no AsciiFxBus call (defenderCell would be null). PRED: silent
[68] (B33,       GAP=4, DISMEMBERED_PART_GETTING_HIT, P3) — Defender has dismembered Hand (still in body tree but dismembered). Hit on it → does SelectHitLocation pick it (TargetWeight implications)? PRED: depends on whether dismemberment sets Abstract or TargetWeight=0
```

### Math / dead-code

```
[69] (B59,       GAP=4, HP_ALIAS_STAT_DEAD_OR_LIVE,  P3) — Add a "HP" Stat to target that aliases "Hitpoints". ApplyDamage(target, X) → does HP also decrement? PRED: yes per the code, but is this dead-code or load-bearing? Worth pinning.
[70] (B103,      GAP=3, DISMEMBER_CHANCE_AT_BOUNDARY, P3) — Damage exactly at threshold (damageRatio == threshold) → does dismemberment ever fire? PRED: no (B102 uses `<`), so 25% on non-mortal is never dismembered. Pin off-by-one.
```

---

## Phase 4 — Property-based (5-7 entries)

Each runs 1000+ iterations with seeded RNG.

```
[P1] HP_IN_RANGE — After ANY sequence of (well-formed) ApplyDamage calls, target.HP ∈ [Min, Max]
[P2] NO_RESURRECTION — Once HandleDeath has fired for an entity, subsequent ApplyDamage cannot increase its HP
[P3] DAMAGE_COMMUTATIVE_NONLETHAL — For non-killing damages a, b: ApplyDamage(t,a);ApplyDamage(t,b) gives same final HP as ApplyDamage(t,b);ApplyDamage(t,a)
[P4] HIT_LOCATION_UNIFORMITY — Over 10000 SelectHitLocation calls, each part's selection rate ≈ TargetWeight / Σ(TargetWeights), within ±2%
[P5] PENETRATION_NEVER_NEGATIVE — RollPenetrations(any pv, any av, rng) always returns ≥ 0
[P6] PENETRATION_BOUNDED — RollPenetrations returns ≤ some bounded N (likely 5-6 given the streak rule). Pin the upper bound observed.
[P7] APPLYDAMAGE_ZERO_NO_EVENTS — ApplyDamage(t, 0, _, _) fires zero events of any kind (verifies B55 short-circuits BEFORE event creation)
```

---

## Phase 4½ — Mutation testing (manual, ~6-8 mutations)

Per the plan: edit production code surgically, run tests, see which still pass.

```
[M1] Flip B55 amount<=0 to amount<0 — does any test catch the off-by-one?
[M2] Flip B56 hpStat.BaseValue <= 0 to < 0 — does the regression test catch?
[M3] Reverse death-cascade order in HandleDeath: Died event BEFORE drops — does any integration test catch?
[M4] Make BroadcastDeathWitnessed a no-op — does any test catch?
[M5] Always return 0 from RollPenetrations — does combat-deals-damage test still pass? (probably not, since 0 pen → "fails to penetrate")
[M6] Always return parts[0] from SelectHitLocation — does any test catch?
[M7] Make CheckCombatDismemberment always trigger — does any test catch?
[M8] Make hpStat.BaseValue -= amount into hpStat.BaseValue -= amount + 1 — surely SOMETHING fails
```

---

## Prioritization summary

**Top 10 highest-priority entries** (combine GAP_SEVERITY × likelihood-of-bug × blast-radius):

| Rank | # | Target | Why |
|---:|---:|---|---|
| 1 | 25 | B65 BodyAwareEquipmentDrop | Runtime-default equipment drop path has zero direct test |
| 2 | 22 | B54a StreakTriggersRestart | Most subtle math in the file, fully untested |
| 3 | 62 | Reentrancy: Died damages killer | Double-stack-frame bug class — likely uncaught |
| 4 | 35 | Dismember removes part | Body × Combat integration — runtime path, untested |
| 5 | 13 | B27 PerPartAVUsed | Modern AV path, untested |
| 6 | 26 | DotKillsViaTick | StatusEffects × Combat — recent saga's bug class |
| 7 | 64 | HandleDeathTwiceGuarded | Direct double-call may bypass ApplyDamage's guard |
| 8 | 11 | NaturalTwentyBypassesDV | Critical-hit path completely untested |
| 9 | 1 | NoWeaponsPunch | Defender-with-Body, attacker-no-weapons → no test |
| 10 | 31 | EquippedItemsVsObjects | Item-dup bug class (Phase 1 of audit caught one related; this is a different angle) |

---

## Notes for Phase 1+ execution

- **Phase 1 will pull from entries 1-25** (canonical-contract, P1-tagged). Skip entry 24 (XP) only if LevelingSystem doesn't yet expose a testable XP stat.
- **Phase 2 will pull from entries 26-56** (cross-system, P2-tagged). Tier A (entries 26-44) absorbs ~60% of Phase 2's budget.
- **Phase 3 will pull from entries 57-70** (adversarial, P3-tagged). All have explicit predictions; classify each outcome.
- **Phase 4 properties** can run alongside Phase 1-3 if any test infrastructure is reused.
- **Phase 4½ mutations** run after Phase 3 to catch any coverage gaps the prior phases missed.

The 70 entries here are a working set. Phase 1-3 will pick ~60 of them for actual implementation; ~10 will remain as deferred items in the audit summary.
