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

### DirectApply diag (NEW, second wave)

| Category | Kind | Fired by |
|---|---|---|
| `damage` | `DirectApply` | `CombatSystem.ApplyDamage` when no upstream `WithCause` scope exists (i.e., NOT inside `PerformSingleAttack`) |

Bleed ticks, burn ticks, trap damage, environmental hazards,
and direct mutation/spell hits now emit `damage/DirectApply`
**before** the existing damage flow, opening a fresh
`Diag.WithCause` scope so downstream `PreDamageMutation` /
`ResistanceApplied` / `DamageDealt` records share an 8-char
trace id. Payload: `amount`, `attributes`, `hasSource`.

Replaces the `attributes:[]` heuristic — a clean filter for
"all non-melee damage" is now `category=damage kind=DirectApply`.
For "all melee" use `category=damage kind=HitRoll` and chase the
`CauseTraceId`.

### AI diag (NEW, second wave)

| Category | Kinds | Fired by |
|---|---|---|
| `ai` | `GoalPushed`, `GoalPopped`, `GoalSelected`, `TurnSkipped`, `PathFailed` | `BrainPart` + `FindPath.Search` |

- `GoalPushed`: every PushGoal call with `goal` (type name), `details`, `stackDepth`
- `GoalPopped`: every RemoveGoal call with `goal`, `details`, `stackDepthAfter`
- `GoalSelected`: top-of-stack at start of each AI turn with `goal`, `details`, `age`, `stackDepth`, `hasTarget`
- `TurnSkipped`: early-return paths (`reason` ∈ {NoZone, NotInZone, InConversation}) with `goalStackDepth` + `topGoal`

The "why is the NPC standing still?" question now answers via
`diag_query category=ai actor=<npc>`. Player frames are NOT
emitted (would flood the buffer — see HandleTakeTurn:Player tag
early-return).

`PathFailed` (added with FindPath instrumentation) fires on every
failed A* search with reason ∈ {NullZone, OutOfBounds, NoPath,
Exhausted} + from/to coords + expanded-node-count + maxNodes.
Successful pathfinds are silent.

### Skill cooldown diag (NEW, fifth wave)

| Category | Kinds | Fired by |
|---|---|---|
| `skill` | `CooldownAdvanced`, `CooldownReady` | `ActivatedAbilitiesPart.TickCooldowns` |

Every per-turn cooldown tick that decrements an ability's
`CooldownRemaining > 0` emits one record. `CooldownAdvanced`
fires while remaining > 0; `CooldownReady` fires on the
transition to 0 (the user-visible "your skill is usable again"
moment). Idle abilities (CR=0) are silent — no flood.

Payload: `ability` (display), `class`, `command`, `before`,
`after`, `maxCooldown`.

Closes the "why is my skill still on cooldown?" gap — debug
starts with `diag_query category=skill kind=CooldownAdvanced
actor=<id>` not `execute_code` state probing.

### Equipment diag (NEW, fourth wave)

| Category | Kinds | Fired by |
|---|---|---|
| `equipment` | `StatBonusApplied`, `StatBonusRemoved`, `SpeedPenaltyApplied`, `SpeedPenaltyRemoved` | `EquipBonusUtility.ApplyEquipBonuses` |

When `InventorySystem.Equip` / `UnequipItem` runs, each parsed
stat:amount pair in the item's `EquippablePart.EquipBonuses`
emits one record with `statName`, `delta`, `bonusBefore`,
`bonusAfter`, `item`, `itemBlueprint`. Symmetric on unequip.
Armor with non-zero `SpeedPenalty` emits a separate
`SpeedPenaltyApplied`/`Removed` record. The "why did my Strength
jump 4 points?" debug now answers via `diag_query
category=equipment kind=StatBonusApplied`.

### Other categories on by default

`event`, `effect`, `damage`, `turn`, `furniture`, `trade`,
`quest`, `skill`, `enhancement`, `mineral-trade`, `movement`,
`inventory`, `leveling`, `ai`, `equipment` — see
`Diag.DefaultOnCategories` (`Diag.cs:119`).

---

## What's NOT emitting yet (known gaps)

These are the obvious next-frontier targets. Each is a single-day
ship that fills out a specific observability blind spot.

### Non-melee damage paths

~~Effect-tick / trap / environmental damage emitted only `DamageDealt`
with `attributes:[]`.~~ **CLOSED in the second wave** — every
`ApplyDamage` call that's NOT inside `PerformSingleAttack` now
emits `damage/DirectApply` at the top of the method, opening a
shared cause scope. Bleed/burn/trap/environmental damage are
queryable by `category=damage kind=DirectApply`.

### Skill-system gaps

- **Cooldown progression** — `SkillsPart` emits `CommandRouted`/
  `CommandRejected` but doesn't emit a `CooldownAdvanced` record
  per turn. Debugging "why is my skill still on cooldown?" requires
  inspecting `SkillsPart` state via `execute_code`.
- **Power-cost deduction** — when a skill spends mutation-power
  points or stat charges, no diag record fires. Useful for
  "why did my power deplete unexpectedly?" debugging.

### AI decision records

- ~~**GoalHandler decisions** — `BrainPart` selects goals each turn
  but doesn't emit a `ai/GoalSelected` record.~~ **CLOSED in the
  second wave** — BrainPart now emits `ai/GoalSelected`,
  `ai/GoalPushed`, `ai/GoalPopped`, `ai/TurnSkipped`.
- **Pathfinding rejections** — when an AI can't reach a target,
  no diag fires. Surface as `ai/PathFailed` with `from`, `to`,
  `reason` (no path / blocked / out of range). Still open.

### Inventory + equipment events

- ~~**EquipBonusUtility.ApplyEquipBonuses** runs on equip/unequip
  but no diag fires.~~ **CLOSED wave 4:** emits
  `equipment/StatBonusApplied|Removed` and
  `equipment/SpeedPenaltyApplied|Removed` per stat-bonus change.
- ~~**Item drops** — `InventoryPart.RemoveObject` doesn't emit a
  drop record. Currently observable via MessageLog only.~~
  **CLOSED wave 1:** facade `Drop` emits
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
| **`DirectApplyDamageObservabilityTests.cs`** *(NEW, second wave)* | 8 | DirectApply emission + shared CauseTraceId with downstream records; bleed-tick as the non-melee signal; scope-leak counter-check |
| **`AIObservabilityTests.cs`** *(NEW, waves 2+3)* | 12 | 8 GoalSelected/Pushed/Popped/TurnSkipped + 4 PathFailed (NullZone/OutOfBounds/NoPath + success-silent counter-check) |
| **`EquipmentObservabilityTests.cs`** *(NEW, fourth wave)* | 7 | StatBonusApplied + Removed; multi-bonus per item; SpeedPenalty for armor; stat-not-present counter-check; malformed-entry counter-check; symmetric round trip |
| **`SkillCooldownObservabilityTests.cs`** *(NEW, fifth wave)* | 6 | CooldownAdvanced / Ready emissions; idle-tick silence; full 3→2→1→0 ladder; multi-ability isolation; payload propagation |

---

## Stopping point: 2026-05-14 (mechanics-coverage push, second wave)

The systematic observability-driven mechanics-coverage push closed
nine major systems across two waves (2026-05-13 → 2026-05-14):

| System | Coverage | Tests |
|---|---|---|
| Combat damage pipeline | 6 kinds + CauseTraceId correlation (4065aae) | 7 new + 13 prior |
| Movement | NEW `movement/Attempt|Blocked|Completed` | 9 |
| Trade | EXTENDED with `trade/Rejected` (8 reject paths) | 9 |
| Leveling | NEW `leveling/Awarded|Rejected|LeveledUp` | 8 |
| Inventory facade | NEW `inventory/<op>` + `Rejected` | 8 |
| Tinkering Try* | EXTENDED with `<op>Rejected` (20+ paths) | 8 |
| Status effects | Existed; now pinned by observability tests | 8 |
| **Non-melee damage** *(2nd wave)* | NEW `damage/DirectApply` (auto-opens cause scope) | 8 |
| **AI decisions** *(2nd wave)* | NEW `ai/GoalSelected|Pushed|Popped|TurnSkipped` | 8 |
| **Pathfinding** *(3rd wave)* | NEW `ai/PathFailed` on FindPath.Search failures | 4 |
| **Equipment bonuses** *(4th wave)* | NEW `equipment/StatBonus*` + `SpeedPenalty*` | 7 |
| **Skill cooldowns** *(5th wave)* | NEW `skill/CooldownAdvanced` + `CooldownReady` | 6 |
| **Total new** | | **90 tests** |

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

**513/513** tests pass across all 18 related fixture groups.
- Wave 1 (Combat / Movement / Commerce / Leveling / Inventory /
  Tinkering / Effects): 57 new + 384 prior = 441/441.
- Wave 2 (DirectApply + AI): 16 new + scope verification = 458/458.
- Wave 3 (PathFailed): 4 new = 462/462.
- Wave 4 (Equipment): 7 new + 502 prior = 513/513.
- Wave 5 (SkillCooldown): 6 new + SkillsPart + Wsp83 = **207/207**
  on a focused-fixture sweep.

Zero regressions across all five waves.

**Pick up here:** remaining gaps from this doc:
- ~~**Skill cooldown progression** (`skill/CooldownAdvanced` per
  turn) — currently requires inspecting SkillsPart state via
  `execute_code`.~~ **CLOSED wave 5.**
- **Power-cost deduction** — when a skill spends MP/charges, no
  record fires.
