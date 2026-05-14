# Observability — Status & Stopping Point

**As of:** 2026-05-13 (after the mechanics-coverage push).

This is the always-on entry point for the diag/observability surface.
For deep how-to, see `AI-OBSERVABILITY.md`. This doc tracks **what
currently emits**, **what still doesn't**, and **the most recent
stopping point** so a future contributor (or future Claude session)
can pick up without re-reading every commit body.

---

## What's done

### Combat damage pipeline — fully decomposed (E.5+ ships, May 2026)

Every modifier site in `CombatSystem.PerformSingleAttack` and
`CombatSystem.ApplyDamage` emits an inspectable diag record. The
six kinds, in the order they fire per attack:

| Kind | When | Key payload fields |
|---|---|---|
| `damage/HitRoll` | Every attempt (hit OR miss) | `weapon`, `hitRoll` (d20), `agilityMod`, `weaponHitBonus`, `skillHitBonus`, `totalHit`, `dv`, `naturalTwenty`, `landed` |
| `damage/Penetration` | Hit lands (before AutoPen) | `weapon`, `av`, `weaponPenBonus`, `strMod`, `skillPenBonus`, `critPenBonus`, `totalBonus`, `maxBonus`, `penetrations`, `autoPenForced` |
| `damage/DamageRoll` | Hit penetrates | `weapon`, `damageDice`, `penetrationsRolled`, `baseDamageTotal`, `naturalTwenty`, `attributes` |
| `damage/PreDamageMutation` | `BeforeTakeDamage` listener mutates amount OR vetoes | `amountBefore`, `amountAfter`, `delta`, `vetoed` |
| `damage/ResistanceApplied` | Per-resistance reduction (one per resistance fired) | `resistanceStat`, `resistancePercent`, `amountBefore`, `amountAfter`, `delta` |
| `damage/DamageDealt` | Final amount lands (existing, pre-ship) | `amount`, `hpAfter`, `lethal`, `attributes` |

All emissions gated by `Diag.IsChannelEnabled("damage")` — zero
allocation when the damage channel is off.

### Per-attack correlation via `CauseTraceId` (4065aae)

`PerformSingleAttack` wraps its body in
`using var _attackCause = Diag.WithCause(attackId)` so every record
emitted during one attack (including those from nested
`ApplyDamage` / `ApplyResistanceFor` calls) shares a single
8-char `CauseTraceId`. `DiagQuery.Filter.CauseTraceId` returns
the deterministic per-attack record set — no timestamp-window
heuristics required.

**Canonical query pattern:**

```csharp
// Step 1 — find an attack's id from any of its records
var pen = DiagQuery.Apply(new DiagQuery.Filter {
    Category = "damage", Kind = "Penetration", Limit = 5
}).Records[0];
string attackId = pen.CauseTraceId;

// Step 2 — pull all 4-6 records for that attack
var attackRecords = DiagQuery.Apply(new DiagQuery.Filter {
    Category = "damage", CauseTraceId = attackId, Limit = 20
}).Records;
```

### Enhancement diag (E.1–E.4)

| Category | Kinds | Fired by |
|---|---|---|
| `enhancement` | `Applied`, `ApplyFailed`, `Removed`, `BonusApplied`, `BonusRemoved`, `Triggered` | `ItemEnhancing.Apply/Remove`, concrete enhancement `OnEquipped`/`OnUnequipped`/`OnAttackerHit` hooks |
| `mineral-trade` | `Traded`, `Rejected` | `MineralTradeService.TryTrade` |

### Movement diag (new, post-mechanics-coverage)

| Category | Kinds | Fired by |
|---|---|---|
| `movement` | `Attempt`, `Blocked`, `Completed` | `MovementSystem.TryMove`, `TryMoveTo`, `TryMoveEx` |

Every entry point emits `Attempt` then either `Blocked` (with
`reason` ∈ {OutOfBounds, NoCurrentCell, NoTargetCell,
BlockedByEntity, VetoedByEvent} + `blockerId` when known) or
`Completed` (with `isPlayer` flag for full-zone-dirty marking
debug). `TryMove` delegates to `TryMoveTo` — only one set of
records per call; `entryPoint` field in `Attempt` distinguishes
which overload was invoked.

### Trade diag (extended, post-mechanics-coverage)

| Category | Kinds | Fired by |
|---|---|---|
| `trade` | `Bought`, `Sold`, **`Rejected`** *(NEW)* | `TradeSystem.BuyFromTrader`, `SellToTrader` |

Pre-fix: 8 reject paths in Buy + Sell were silent. Post-fix
every reject path emits `trade/Rejected` with `direction` (Buy
or Sell) + `reason` ∈ {NullArg, TraderHasNoInventory,
BuyerHasNoInventory, TraderUnable:{is dead|is on fire|...},
NoTrade, InsufficientDrams, BeforeTradeVetoed,
TraderRemoveObjectFailed, BuyerAddObjectFailed:Weight, etc.}.

### Leveling diag (NEW, post-mechanics-coverage)

| Category | Kinds | Fired by |
|---|---|---|
| `leveling` | `Awarded`, `Rejected`, `LeveledUp` | `LevelingSystem.AwardKillXP`, `CheckLevelUp` |

- `Awarded`: xpGained + xpBefore/xpAfter + currentLevel + xpToNext
- `Rejected`: reason ∈ {NullArg, VictimHasNoXPValue, KillerHasNoExperienceStat}
- `LeveledUp`: one per level transition (multi-level overflow
  emits one record each) with prevLevel/newLevel, xpThresholdCrossed,
  xpRemaining, hpMaxBefore/After, healedToFull, gainedMP, gainedSP

### Inventory diag (NEW, post-mechanics-coverage)

| Category | Kinds | Fired by |
|---|---|---|
| `inventory` | `Pickup`, `Drop`, `DropPartial`, `Equip`, `Unequip`, `AutoEquip`, `Rejected` | `InventorySystem` facade |

Every mutation goes through the facade. Success → corresponding
kind. Failure → `Rejected` with `operation` (which facade method),
`errorCode` (Validation/Execution/Exception), `errorMessage` (the
same string the UI surfaces), and `validationCode` (the
`InventoryValidationErrorCode` enum value: NotOwned, NotTakeable,
InvalidActor, InsufficientStrength, …).

### Tinkering diag (extended, post-mechanics-coverage)

| Category | Kinds | Fired by |
|---|---|---|
| `enhancement` | `Applied`, `ApplyFailed`, `Removed`, ... + **`Crafted`/`CraftRejected`**, **`ApplyModSucceeded`/`ApplyModRejected`**, **`Disassembled`/`DisassembleRejected`** *(NEW)* | `TinkeringService.TryCraft/TryApplyModification/TryDisassemble` |

Pre-fix: 20+ silent reject paths in the three Try* methods.
Post-fix every call emits exactly one record with `reason`
matching the UI message (e.g. "Crafter is missing.", "Not enough
bits.", "You must own the target item.").

### Other categories on by default

`event`, `effect`, `damage`, `turn`, `furniture`, `trade`,
`quest`, `skill`, `enhancement`, `mineral-trade`, `movement`,
`inventory`, `leveling` — see `Diag.DefaultOnCategories`
(`Diag.cs:119`).

---

## What's NOT emitting yet (known gaps)

These are the obvious next-frontier targets. Each is a single-day
ship that fills out a specific observability blind spot.

### Non-melee damage paths

- **Effect-tick damage** (`BleedingEffect`, `BurningEffect`, etc. —
  the per-turn damage these apply doesn't go through
  `PerformSingleAttack`, so it gets `DamageDealt` but NOT the
  upstream `HitRoll`/`Penetration`/`DamageRoll` records). Pattern:
  bleed ticks emit `attributes: []` on `DamageDealt`, which is the
  current way to filter them out.
- **Trap damage** (`SpikeTrapPart`, `PressurePlatePart`, …) — also
  bypasses the attack pipeline. Some traps already emit
  `furniture/Triggered`; the damage they deal goes through
  `ApplyDamage` only.
- **Environmental damage** (acid pools, lava, fire-on-cell). Same
  shape — no upstream attack record, just `DamageDealt`.

A small unified emission — `damage/DirectApply` with
`{source, reason, amount}` — would distinguish these from
attack-pathway damage without forcing them through a fake
`PerformSingleAttack` synthetic.

### Skill-system gaps

- **Cooldown progression** — `SkillsPart` emits `CommandRouted`/
  `CommandRejected` but doesn't emit a `CooldownAdvanced` record
  per turn. Debugging "why is my skill still on cooldown?" requires
  inspecting `SkillsPart` state via `execute_code`.
- **Power-cost deduction** — when a skill spends mutation-power
  points or stat charges, no diag record fires. Useful for
  "why did my power deplete unexpectedly?" debugging.

### AI decision records

- **GoalHandler decisions** — `BrainPart` selects goals each turn
  but doesn't emit a `ai/GoalSelected` record. Knowing "why is the
  NPC standing still?" requires log-grep and brain-state probing.
- **Pathfinding rejections** — when an AI can't reach a target,
  no diag fires. Surface as `ai/PathFailed` with `from`, `to`,
  `reason` (no path / blocked / out of range).

### Inventory + equipment events

- **EquipBonusUtility.ApplyEquipBonuses** runs on equip/unequip
  but no diag fires. A record-on-stat-mutation would help debug
  "why did my Strength jump 4 points?"
- ~~**Item drops** — `InventoryPart.RemoveObject` doesn't emit a
  drop record. Currently observable via MessageLog only.~~
  **CLOSED post-mechanics-coverage:** facade `Drop` emits
  `inventory/Drop`; `inventory/Rejected` emits on every reject.

---

## Test coverage

| Fixture | Tests | What it pins |
|---|---|---|
| `PenetrationDiagTests.cs` | 13 | Each of the 6 damage diag kinds + 3 correlation tests (single-attack share id, two-attacks have distinct ids, Filter.CauseTraceId returns only matching) |
| `DiagTests.cs` | n | Core ring buffer + filter mechanics |
| Skill-system diag tests | scattered | `CommandRouted`/`CommandRejected` emissions |
| Enhancement diag tests | scattered | Within each `Enhancement*Tests.cs` |
| **`CombatObservabilityTests.cs`** *(NEW)* | 7 | End-to-end pipeline dumps; full / miss / resist / multi-resist / veto / crit / two-attack isolation |
| **`MovementObservabilityTests.cs`** *(NEW)* | 9 | Attempt/Blocked/Completed contract; reasons OOB/Solid/Veto/NoCurrentCell; no double-emission on TryMove → TryMoveTo |
| **`TradeObservabilityTests.cs`** *(NEW)* | 9 | Bought/Sold success + Rejected on InsufficientDrams/NoTrade/TraderUnable/NullArg |
| **`LevelingObservabilityTests.cs`** *(NEW)* | 8 | Awarded/Rejected/LeveledUp; multi-level overflow emits one LeveledUp per transition |
| **`InventoryObservabilityTests.cs`** *(NEW)* | 8 | Pickup/Drop/Rejected; null-args across all 5 facade methods; **surface gap: PickupCommand has no adjacency check** |
| **`TinkeringObservabilityTests.cs`** *(NEW)* | 8 | Crafted/Disassembled success + Rejected paths with reason matching UI |
| **`EffectsObservabilityTests.cs`** *(NEW)* | 8 | OnApply/OnRemove; stacking no-double-emission; force-apply flag; **surface observation: FrozenEffect auto-removes BurningEffect** |

---

## Stopping point: 2026-05-13 (mechanics-coverage push)

The systematic observability-driven mechanics-coverage push closed
seven major systems:

| System | Coverage | Tests |
|---|---|---|
| Combat damage pipeline | 6 kinds + CauseTraceId correlation (4065aae) | 7 new + 13 prior |
| Movement | NEW `movement/Attempt|Blocked|Completed` | 9 |
| Trade | EXTENDED with `trade/Rejected` (8 reject paths) | 9 |
| Leveling | NEW `leveling/Awarded|Rejected|LeveledUp` | 8 |
| Inventory facade | NEW `inventory/<op>` + `Rejected` | 8 |
| Tinkering Try* | EXTENDED with `<op>Rejected` (20+ paths) | 8 |
| Status effects | Existed; now pinned by observability tests | 8 |
| **Total new** | | **57 tests** |

### Surface gaps found during test development

1. **`PickupCommand.Validate` has no adjacency check.** Pickup
   from any zone position succeeds. The UI gates this via
   `GetTakeableItemsAtFeet` but a skill/code path could call
   directly. Pinned as a regression-check in
   `InventoryObservabilityTests.SurfaceGap_PickupHasNoAdjacencyCheck_EmitsPickup`.
2. **`FrozenEffect.OnApply` auto-removes `BurningEffect`** via
   `target.RemoveEffect<BurningEffect>()`. The diag stream
   surfaces this as a 3-record set per Frozen-onto-Burning apply.
   Pinned in
   `EffectsObservabilityTests.FrozenOnBurning_AppliesAndAutoRemovesBurning_DocumentedSideEffect`.
3. **`TinkeringService` had 20+ silent reject paths** — every
   `return false` did not emit a diag record. Closed.

### Empirical regression sweep result

441/441 tests pass across all 13 related fixture groups
(observability + trade + leveling + inventory + tinkering +
diag + the new fixtures).

**Pick up here:** the next high-value addition is the **non-melee
damage `DirectApply` record**, which would let queries distinguish
bleed/burn/trap damage from attack damage without the
`attributes:[]` heuristic. Cost: ~20 LOC + 3 tests.

After that, the **AI-decision records** are the biggest remaining
observability blind spot — "why did the NPC stand still?" is
currently un-debuggable without log-grep.

Also queued: **EquipBonusUtility.ApplyEquipBonuses** stat-mutation
diag (`equipment/StatBonusApplied`), so a future "why did my
Strength jump 4 points?" debug starts with a query, not a grep.
