# Observability — Status & Stopping Point

**As of:** 2026-05-13 (after `4065aae`).

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

### Liquid coating diag (LQ.4–LQ.7)

| Category | Kinds | Fired by |
|---|---|---|
| `liquid` | `Coated`, `CoatRejected` (reason: NullActor/NotACreature/RegistryUninitialized/NoLiquidId/UnknownLiquid/PoolEmpty/ZeroExposure), `StatModApplied`, `StatModRemoved`, `CoatExpired` | `LiquidPoolPart.HandleEvent` (transfer-on-contact gates); `LiquidCoveredEffect` Apply/Reverse stat mods + OnRemove lifecycle |

> **Note (LQ.5 design choice):** liquid damage *amplification*
> (water→Lightning, oil→Fire) is NOT a separate `liquid` record — it
> mutates `Damage.Amount` during `BeforeTakeDamage`, so it surfaces on
> the existing `damage/PreDamageMutation` record (`CombatSystem.cs:763`)
> with the before/after/delta. A dedicated `liquid/CoatModifiedDamage`
> (proposed in the plan) was deliberately not added — it would
> duplicate `PreDamageMutation`.

### Other categories on by default

`event`, `effect`, `damage`, `turn`, `furniture`, `trade`,
`quest`, `skill`, `enhancement`, `mineral-trade`, `worldmap`,
`liquid` — see `Diag.DefaultOnCategories` (`Diag.cs:119`).

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
- **Item drops** — `InventoryPart.RemoveObject` doesn't emit a
  drop record. Currently observable via MessageLog only.

---

## Test coverage

| Fixture | Tests | What it pins |
|---|---|---|
| `PenetrationDiagTests.cs` | 13 | Each of the 6 damage diag kinds + 3 correlation tests (single-attack share id, two-attacks have distinct ids, Filter.CauseTraceId returns only matching) |
| `DiagTests.cs` | n | Core ring buffer + filter mechanics |
| Skill-system diag tests | scattered | `CommandRouted`/`CommandRejected` emissions |
| Enhancement diag tests | scattered | Within each `Enhancement*Tests.cs` |

---

## Stopping point: 2026-05-13 (commit 4065aae)

The combat damage pipeline is **complete** end-to-end:
HitRoll → Penetration → DamageRoll → PreDamageMutation →
ResistanceApplied → DamageDealt, all deterministically
correlatable via `CauseTraceId`.

Empirically verified in the user's live combat session:
- 17 sharp longsword swings successfully decomposed
- Sharp mod's `+1 weaponPenBonus` confirmed in every penetration roll
- Cross-attack correlation via `f25d5a23` returned exactly 4 records
  for one swing — no bleed-tick interleave, no timestamp guessing

**Pick up here:** the next high-value addition is the non-melee
damage `DirectApply` record (above), which would let queries
distinguish bleed/burn/trap damage from attack damage without the
`attributes:[]` heuristic. Cost: ~20 LOC + 3 tests.

After that, the AI-decision records are the biggest remaining
observability blind spot — "why did the NPC stand still?" is
currently un-debuggable without log-grep.
