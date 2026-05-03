# Tier 2 Closeout — plan

> Closes the open 💡 entries in Tier 2 of `Docs/CONTENT-ROADMAP.md`.
> Six items planned in full; **the three combat-shaped items are
> implemented this ship** (light-emitting equipment, PressurePlate,
> TripWire). The three combat-adjacent / non-combat items
> (Lockpicking, Consumable keys, Hunger) are planned in detail but
> deferred to their own dedicated ships per the user's "Tier 2 combat
> content" scope on this branch.

---

## Open items inventory (full Tier 2)

| # | Item | Class | Plan section | Implement now? |
|---|---|---|---|---|
| 1 | LightSourcePart propagation through equipment | Combat | T2.2 | ✅ yes |
| 2 | PressurePlate (rearmable trap) | Combat | T2.3 | ✅ yes |
| 3 | TripWire (multi-cell trigger) | Combat | T2.4 | ✅ yes |
| 4 | Lockpicking skill + lockpick item | Dungeon polish | T2.D-LOCKPICK | ⚪ deferred |
| 5 | Consumable single-use keys | Lock & Key v2 polish | T2.D-KEY-CONSUMABLE | ⚪ deferred |
| 6 | Hunger / Food (HungerStat) | Survival | T2.D-HUNGER | ⚪ deferred |

The 3-item combat ship is one cohesive bundle: each item is content
that pairs with a small substrate addition. Total implementation
work: ~5 commits (3 sub-milestones + plan + closeout).

---

## Verification sweep (combat items)

| Premise | Status | Source |
|---|---|---|
| `LightSourcePart` exists with `Radius`, `LightColor`, `Intensity` fields | ✅ confirmed | `Entities/LightSourcePart.cs:1-31` |
| `LightMap.Compute` iterates `zone.GetReadOnlyEntities()` and pulls `entity.GetPart<LightSourcePart>()` only — **does NOT walk equipped items** | ⚠️ **the gap to fix** | `World/LightMap.cs:53-63` |
| `InventoryPart.GetAllEquipped()` returns `List<Entity>` of all equipped items (no duplicates for multi-slot) | ✅ confirmed | `Inventory/InventoryPart.cs:258-268` |
| `LightMap.Compute` short-circuits via `EntityVersion` cache — adding equipment iteration doesn't break the cache invariant since equipment changes go through `inv.AddObject`/`RemoveObject` which mutate `Objects`, but **equipping/unequipping does not mutate `Objects` directly**. Need to invalidate light cache on equip/unequip too. | ⚠️ **methodology debt** | `World/LightMap.cs:36-38`, `Inventory/InventoryPart.cs:42` |
| `InventoryPart` exposes 4 mutation entry points: `Equip(item, slot)`, `Unequip(slot)`, `EquipToBodyPart(item, bp)`, `UnequipFromBodyPart(bp)` (lines 197/215/110/157). Bump must hit all four for correctness. | ✅ confirmed | `Inventory/InventoryPart.cs:110-220` |
| `TriggerOnStepPart` exists with `TriggerFaction`, `ConsumeOnTrigger`, abstract `OnTrigger(actor, zone)` | ✅ confirmed | `Entities/TriggerOnStepPart.cs:25-83` |
| Existing trap subclasses (`SpikeTrapTriggerPart`, `FireTrapTriggerPart`, `BearTrapTriggerPart`) follow the same shape | ✅ confirmed | `Entities/TriggerOnStepPart.cs:164-242` |
| `EntityEnteredCell` fires only on cell-CHANGE moves (an actor stepping onto the cell), not every turn an actor stands on it. So `ConsumeOnTrigger=false` alone gives a perfectly fine "fires once per step-onto" pressure plate without needing cooldown logic. | ✅ confirmed (corrects an earlier draft of the plan) | `Turns/MovementSystem.cs:FireCellEnteredEvents` |
| `TurnManager.TickCount` is a public read-only int property on a non-static `TurnManager` instance — there's no static `TurnManager.CurrentTurn`. The earlier draft's `NextArmedTurn = TurnManager.CurrentTurn + ...` would NOT compile. | ❌ **false premise — corrected** | `Turns/TurnManager.cs:80` |
| `Cell.Objects` is the `List<Entity>` of cell occupants. There's no `Cell.GetEntitiesAtCell()` (a name I invented in the earlier draft). | ❌ **false premise — corrected** | `World/Map/Cell.cs:49` |
| Multi-cell entities — there's no abstraction for "this entity occupies cells (x,y), (x+1,y), (x+2,y)"; all entities live at one cell | ⚠️ **architecture constraint** | `World/Map/Zone.cs:212-249` |
| Zone perf rules (CLAUDE.md §non-negotiables) — LightMap is per-render, ZoneRenderHooks is the dirty mechanism | ✅ confirmed | `Docs/PERF-FOUNDATION.md` |

**Three false premises corrected (two added during critical-review pass), one architecture constraint.**

1. **Light propagation through equipment requires LightMap to also iterate
   equipped items per zone entity.** Roadmap implied "just add LightSourcePart
   to FlamingSword's blueprint" — that won't work because LightMap doesn't
   look in inventory. Planned into T2.2 with explicit perf gate.

2. **TripWire = multi-cell, but our entity model is single-cell.** Solving
   this cleanly requires either (a) a multi-cell entity abstraction (large
   blast radius, deferred to a future system ship), or (b) modeling the
   tripwire as N separate entity-segments that share a `WireGroupId` and
   coordinate via direct zone scan. Planned into T2.4 with option (b).

3. **PressurePlate cooldown was over-engineered in earlier draft.** Critical
   review caught: `TurnManager.CurrentTurn` doesn't exist (only `TickCount`
   on a non-static instance), and `EntityEnteredCell` already fires only on
   cell-change moves — so `ConsumeOnTrigger=false` alone IS the right
   v1 design. Player stepping ON-OFF-ON-OFF intentionally takes repeated
   damage; that's correct PressurePlate semantics, not a bug. Cooldown logic
   removed from v1 plan; can return as a follow-on if playtest reveals the
   need (e.g. for puzzle-state plates that should debounce). Catch saved
   ~30 min of plumbing + a brittle TurnManager dependency.

4. **`Cell.Objects` is the list-of-occupants — no `GetEntitiesAtCell()` method
   exists.** Earlier draft's TripWire detonation code wouldn't have compiled.
   Corrected pre-implementation.

---

## Sub-milestones (smallest blast radius first)

### T2.1 — Plan + branch (this commit)

- `Docs/TIER2-CLOSEOUT.md` (this file)
- Branch `feat/tier2-combat-closeout` cut from `main` at `e4aa5e2`

### T2.2 — Light-emitting equipment (one commit)

**Goal:** Equipping a weapon with `LightSourcePart` makes the wielder
project that light at their cell. Held FlamingSword glows red, held
IceSword glows cyan, held Lantern (non-weapon, equipped to a
"Floating Nearby" slot) glows warm yellow.

**Code change:** `LightMap.Compute` extension. After the existing
zone-entity LightSourcePart iteration, add a second pass: for each
zone entity that has an `InventoryPart`, walk
`inv.GetAllEquipped()` and add light at the wielder's cell for every
equipped item that has a `LightSourcePart`.

```csharp
foreach (var entity in zone.GetReadOnlyEntities())
{
    // Pass 1 (existing) — entity itself is a light source.
    var ownLight = entity.GetPart<LightSourcePart>();
    if (ownLight != null)
    {
        var cell = zone.GetEntityCell(entity);
        if (cell != null)
            AddLight(zone, cell.X, cell.Y, ownLight.Radius,
                     ownLight.Intensity, QudColorParser.Parse(ownLight.LightColor));
    }

    // Pass 2 (new) — equipped items project light at the wielder.
    var inv = entity.GetPart<InventoryPart>();
    if (inv != null)
    {
        var wielderCell = zone.GetEntityCell(entity);
        if (wielderCell != null)
        {
            foreach (var item in inv.GetAllEquipped())
            {
                if (item == null) continue;
                var itemLight = item.GetPart<LightSourcePart>();
                if (itemLight == null) continue;
                AddLight(zone, wielderCell.X, wielderCell.Y,
                         itemLight.Radius, itemLight.Intensity,
                         QudColorParser.Parse(itemLight.LightColor));
            }
        }
    }
}
```

**Cache invalidation:** `LightMap` short-circuits on
`EntityVersion`. Equipping/unequipping mutates `EquippedItems` but
NOT `_entityCells` (so EntityVersion doesn't bump). For light to
update on equip/unequip, the EntityVersion cache needs to bump on
those operations OR we need a separate "EquipmentVersion" tracker.

Cleanest: bump `Zone.EntityVersion` whenever an item is equipped or
unequipped. The verification sweep identified **4 mutation entry points**
in `InventoryPart`:

| Method | Line | Path |
|---|---|---|
| `Equip(item, slot)` | 197 | Legacy string-slot path |
| `Unequip(slot)` | 215 | Legacy string-slot path |
| `EquipToBodyPart(item, bp)` | 110 | Body-part-aware path |
| `UnequipFromBodyPart(bp)` | 157 | Body-part-aware path |

Plus `EquipToBodyParts(item, parts)` (line 131) which is the multi-slot
variant — this internally calls `EquipToBodyPart` once per slot, so a
single bump in the inner method covers it.

Strategy: extract a private helper `MarkEquipmentChanged()` on
`InventoryPart` that walks up to find the parent entity's current zone
and calls a new public `Zone.MarkEquipmentChanged()` (which just
increments EntityVersion). Call from all 4 mutation sites. Documented in
T2.2 commit body and in inline comments at each call site.

**Content edits to Objects.json:**
- Add `LightSource` part to `FlamingSword` blueprint (color `&R`, radius 4, intensity 0.8)
- Add `LightSource` part to `IceSword` blueprint (color `&C`, radius 4, intensity 0.7)
- Add `LightSource` part to `ThunderHammer` blueprint (color `&Y`, radius 3, intensity 0.6)

**RED → GREEN tests** in `Tests/.../World/LightMapEquipmentLightingTests.cs`:
1. `EquippedFlamingSword_AddsLightAtWielderCell` — positive: wielder with FlamingSword equipped → LightMap.GetBrightness at wielder's cell > ambient.
2. `UnequippedFlamingSword_DoesNotAddLight` — counter-check: same FlamingSword in inventory but NOT equipped → LightMap.GetBrightness = ambient.
3. `EquippedFlamingSword_TintMatchesLightColor` — pin the red color carries through (avoids "we got brightness but wrong tint" bug).
4. `EquipNewLightSource_BumpsEntityVersion` — pin the cache-invalidation contract: equipping a LightSource must bump EntityVersion so next LightMap.Compute call recomputes.
5. `WielderWithoutInventory_DoesNotCrash` — adversarial null-safety.
6. `EquippedItemWithoutLightSourcePart_NoOp` — counter-check: equipped sword without `LightSource` part doesn't add light.

### T2.3 — PressurePlate (one commit)

**Goal:** A floor trap that fires every time someone steps onto it
(unlike SpikeTrap which is one-shot — SpikeTrap consumes itself
post-detonation; PressurePlate persists). Critical-review correction:
no cooldown logic in v1 — `EntityEnteredCell` already fires on
cell-CHANGE only, so a stationary actor doesn't re-trigger. A player
who deliberately steps ON-OFF-ON-OFF takes repeated damage; that's
correct PressurePlate semantics, not a bug to debounce.

**New file:** `Entities/PressurePlateTriggerPart.cs` extending
`TriggerOnStepPart`:

```csharp
public class PressurePlateTriggerPart : TriggerOnStepPart
{
    /// <summary>Damage dealt on each trigger. Default 8 (lighter than
    /// one-shot traps since it fires repeatedly).</summary>
    public int Damage = 8;

    /// <summary>Damage attribute attached to the strike (e.g. "Piercing"
    /// for a spiked plate, "Bludgeoning" for a crushing plate).</summary>
    public string DamageAttribute = "Bludgeoning";

    public PressurePlateTriggerPart()
    {
        // Don't consume on trigger — the plate persists for repeated stepping.
        // EntityEnteredCell fires on cell-CHANGE only, so this can't loop on
        // a stationary actor; only re-stepping triggers a re-fire.
        ConsumeOnTrigger = false;
    }

    protected override void OnTrigger(Entity actor, Zone zone)
    {
        var dmg = new Damage(Damage);
        if (!string.IsNullOrEmpty(DamageAttribute))
            dmg.AddAttribute(DamageAttribute);
        CombatSystem.ApplyDamage(actor, dmg, ParentEntity, zone);
        MessageLog.Add($"{actor.GetDisplayName()} treads on the " +
                       $"{ParentEntity.GetDisplayName()}.");
    }
}
```

**New blueprint** `PressurePlate` in `Objects.json` (Tier 1, value 0,
non-takeable, render `^`).

**RED → GREEN tests** in `Tests/.../Entities/PressurePlateTests.cs`:
1. `Step_FirstTime_DealsDamage` — positive: actor steps on plate → damage applied.
2. `StepOff_AndBackOn_FiresAgain` — pin the rearmable contract: A→B→A re-triggers.
3. `Step_DoesNotConsumePlate` — counter-check vs SpikeTrap: plate persists in zone after firing.
4. `Step_AppliesConfiguredDamageAttribute` — pin the Bludgeoning/Piercing attribute pass-through.
5. `FactionMate_DoesNotTrigger` — existing TriggerFaction filter still works (counter-check the inherited filter).

### T2.4 — TripWire (one commit)

**Goal:** A line trap. Player steps on any of N tripwire segments → a
single coordinated detonation deals damage to ALL segments' cells in
one event. After detonation, all segments consume themselves (one-shot).

**Architecture (option B from sweep):** Each segment is a
`TripWireTriggerPart` entity at one cell, with a shared `WireGroupId`
string. When one segment fires, it broadcasts to all entities in the
zone with a matching `WireGroupId` so they all damage their own cell
+ self-consume.

```csharp
public class TripWireTriggerPart : TriggerOnStepPart
{
    /// <summary>Damage dealt at this segment's cell on detonation.
    /// Default 10. Each segment of a multi-segment wire deals
    /// independently — a 3-segment wire striking 3 cells.</summary>
    public int Damage = 10;

    /// <summary>Group ID linking segments. All segments with the same
    /// group ID fire together when any one is tripped.</summary>
    public string WireGroupId = "";

    public TripWireTriggerPart() { ConsumeOnTrigger = false; /* manual cleanup */ }

    protected override void OnTrigger(Entity actor, Zone zone)
    {
        // Find all sibling segments and ourselves; each damages whoever
        // is at its cell + self-consumes. Snapshot via GetAllEntities to
        // be safe against the in-flight RemoveEntity calls.
        var allSegments = new System.Collections.Generic.List<Entity>();
        foreach (var e in zone.GetAllEntities())
        {
            var seg = e.GetPart<TripWireTriggerPart>();
            if (seg != null && seg.WireGroupId == WireGroupId)
                allSegments.Add(e);
        }
        foreach (var seg in allSegments)
        {
            var segCell = zone.GetEntityCell(seg);
            if (segCell == null) continue;
            // Damage whoever's at this segment's cell (could be the
            // tripper for the segment they actually stepped on; could
            // be empty for the others — fire damage so a "trip the
            // wire from cover" exploit doesn't help).
            // cell.Objects is the canonical occupant list (Cell.cs:49).
            // Snapshot via ToArray since RemoveEntity may mutate it
            // mid-iteration if a Death handler removes corpses.
            foreach (var occ in segCell.Objects.ToArray())
            {
                if (occ == seg) continue;
                CombatSystem.ApplyDamage(occ, new Damage(Damage),
                    ParentEntity, zone);
            }
            zone.RemoveEntity(seg);
        }
        MessageLog.Add($"The tripwire snaps taut!");
    }
}
```

**RED → GREEN tests** in `Tests/.../Entities/TripWireTests.cs`:
1. `Trip_Single_FiresAndConsumesAllSegments` — 3 segments same group → trip one → all 3 removed from zone.
2. `Trip_Single_DamagesActorAtTrippedCell` — positive damage assertion.
3. `Trip_OtherActorsAtOtherSegments_AlsoDamaged` — pin the multi-cell coverage (place a snapjaw on a non-tripped segment cell, trip the wire, assert snapjaw took damage too — proves it's a LINE not just an AOE around the tripped cell).
4. `Trip_DifferentGroupIds_NotAffected` — counter-check: a 4th segment with a different group ID is NOT removed when the first group fires.
5. `Trip_FactionMate_DoesNotTrigger` — the existing TriggerFaction filter still works.

### T2.5 — Cold-eye review + roadmap update + merge + push

Per CLAUDE.md §3.4. Update roadmap entries:
- Tier 2 § Light-emitting weapons: 💡 → ✅
- Tier 2 § Trap furniture: PressurePlate 💡 → ✅, TripWire 💡 → ✅
- Recently shipped: prepend Tier 2 closeout entry

Cold-eye Q1-Q4:
- Q1: PressurePlate↔TripWire are both `TriggerOnStepPart` subclasses; the only asymmetry is the multi-cell coordination in TripWire. Documented.
- Q2: damage routing all uses `Damage` typed objects (no int-overload bypass), matches the SpikeTrap precedent.
- Q3: every positive damage assertion has a counter-check (no-trip / different group / disarmed plate).
- Q4: Plan ↔ shipped commits.

---

## Deferred sub-milestones (planned, not implemented this ship)

These three are out of scope per the user's "Tier 2 combat content"
request. They're documented in full so the next ship can pick them
up without re-planning.

### T2.D-LOCKPICK — Lockpicking skill + lockpick item (deferred)

**Verification sweep finding:** No `SkillSystem` or `SkillCheck` helper
exists in the codebase. A minimal v1 doesn't need a full skill system
— a single static helper `StatUtils.RollSkillCheck(actor, statName,
dc, rng)` (1d20 + StatModifier(actor, statName) ≥ DC) is enough for
lockpicking.

**Plan:**
- Add `LockPart.Difficulty = 0` field (default = no lockpick option;
  >0 enables lockpicking with that DC).
- New `LockpickPart` item with optional `BonusToCheck` (skilled lockpicks
  vs makeshift). `StackerPart` for a stack of pick attempts.
- `LockpickPart.HandleEvent` for `GetInventoryActions` adds
  "Pick lock" entry visible only when targeting a `LockPart`.
- Roll: `1d20 + Agility-modifier + LockpickPart.BonusToCheck` ≥
  `LockPart.Difficulty`. On success: `LockPart.IsLocked = false`,
  consume one lockpick. On fail: consume one lockpick (skill-vs-difficulty
  saw) — half-failure can be a 🟡 finding to defer ("crit-fail breaks
  pick" not v1).
- Tests: positive pick on easy lock, fail on hard lock, lockpick consumed
  on both, counter-check no-key + no-lockpick remains locked.

**Estimate:** 1 commit, ~120 lines of code + 8 tests. ~30 min focused.

### T2.D-KEY-CONSUMABLE — Single-use keys (deferred)

**Plan:**
- Add `KeyPart.Consumable = false` field (default reusable, matches
  existing master-key model).
- In `LockPart.HandleEvent` `AttemptUnlock` branch, after `keyUsed`
  resolves and `succeeded == true`, if `keyUsed.GetPart<KeyPart>().Consumable`,
  remove it from the actor's inventory.
- Optionally: add a `BronzeKey` blueprint (Consumable=true) as the
  consumable variant.
- Tests: consumable key consumed on success, reusable key NOT consumed,
  consumable key NOT consumed on fail (lock stays locked + key stays).

**Estimate:** 1 commit, ~30 lines + 4 tests. ~15 min.

### T2.D-HUNGER — Hunger / Food (deferred)

**Plan:**
- Add `HungerStat` to player blueprint and creature blueprints that
  should care (humanoids — yes; constructs/undead — no via blueprint
  selection).
- New `HungerSystem` (or just a static `OnTurnEnd` hook) that
  decrements the stat by 1 per N turns (configurable; defaults to
  1 per 50 turns).
- Threshold-based modifier application (Sated +5%, Hungry -5%,
  Starving -20% Strength + DOT). Cleanest: a `HungerEffect` that
  mirrors the existing Effect system.
- `FoodPart.Eat` extension: also restore HungerStat by `FoodPart.Nutrition`
  (new int field).
- Tests: stat decrements per turn, food restores nutrition,
  threshold transitions apply/remove correct effect, save/load
  round-trip.

**Estimate:** 1 commit, ~200 lines + 12 tests. ~1.5 hr focused.

---

## Critical files

### New files

| Path | Purpose |
|---|---|
| `Docs/TIER2-CLOSEOUT.md` | This plan |
| `Scripts/Gameplay/Entities/PressurePlateTriggerPart.cs` | T2.3 |
| `Scripts/Gameplay/Entities/TripWireTriggerPart.cs` | T2.4 |
| `Tests/EditMode/Gameplay/World/LightMapEquipmentLightingTests.cs` | T2.2 (~6 tests) |
| `Tests/EditMode/Gameplay/Entities/PressurePlateTests.cs` | T2.3 (~5 tests) |
| `Tests/EditMode/Gameplay/Entities/TripWireTests.cs` | T2.4 (~5 tests) |

### Modified files

| Path | Change |
|---|---|
| `Scripts/Gameplay/World/LightMap.cs` | T2.2 — add Pass 2 (equipped-item walk) |
| `Scripts/Gameplay/Inventory/InventoryPart.cs` | T2.2 — bump `Zone.EntityVersion` on Equip / Unequip mutations |
| `Resources/Content/Blueprints/Objects.json` | T2.2 + T2.3 — add LightSource to 3 weapons; add PressurePlate blueprint |
| `Docs/CONTENT-ROADMAP.md` | T2.5 — flips + Recently Shipped row |

---

## Reusable utilities (don't reinvent)

| Utility | Path |
|---|---|
| `LightMap.AddLight` (existing private) | `World/LightMap.cs:66-101` |
| `InventoryPart.GetAllEquipped()` | `Inventory/InventoryPart.cs:258` |
| `TriggerOnStepPart` base class | `Entities/TriggerOnStepPart.cs:25` |
| `Cell.GetEntitiesAtCell()` for tripwire occupant scan | `World/Map/Cell.cs` (existing) |
| `Damage` typed object + `AddAttribute("...")` | `Effects/Damage.cs` |
| `Diag.Record` (reuse existing channels — no new channel) | `Shared/Utilities/Diag.cs` |

---

## Diag observability — reuse existing channels

No new diag channel. `damage/DamageDealt` is already emitted by
`CombatSystem.ApplyDamage` for every plate / wire detonation. The
combat showcase scenarios already query `damage` channel for
verification.

For T2.2 (light) — there's no per-frame diag for lighting and
shouldn't be (perf-critical path). Verification is via direct
`LightMap.GetBrightness(x, y)` calls in tests.

---

## Self-review pre-flagged 🟡 findings

These are designed-in tradeoffs; fix or defer with a note per CLAUDE.md §5.

- **🟡 T2.2 perf — light cache invalidation cost.** Bumping
  EntityVersion on every equip/unequip means LightMap recomputes
  fully on every equip even though only ONE wielder changed. v1
  acceptable since equip/unequip is rare (player turn, intentional
  action). If a future "auto-equip on pickup" feature lands, revisit
  with a finer-grained `EquipmentVersion` field on Zone.
- **🟡 T2.2 multi-equipped lights stack additively.** Two flaming
  swords dual-wielded = double light intensity at same cell. That's
  not necessarily wrong but is a content authoring tradeoff.
  Documented in commit body; defer to playtest.
- **🟡 T2.3 deliberately ships without cooldown.** Critical-review
  pass dropped this — `EntityEnteredCell` fires on cell-CHANGE only,
  so a stationary actor doesn't re-trigger; deliberate ON-OFF-ON
  stepping is correct semantics. If playtest later wants debouncing
  (e.g. for puzzle-state plates that should fire-once-per-actor),
  add a `_actorsAlreadyOnPlate` HashSet field — clear on TurnEnd.
  Don't reach for TurnManager coupling.
- **🟡 T2.4 multi-cell entity is a workaround.** TripWire's "N
  segments share a WireGroupId" pattern works but doesn't scale to
  arbitrary multi-cell shapes (a 3×3 trap mat). Future
  multi-cell-entity work would replace this. Out of scope.
- **🔵 T2.4 detonation event is intra-zone scan O(zone size).**
  Each segment's `OnTrigger` does a full zone-walk to find
  group-mates. For a 9-segment max wire in a 80×25 zone, this is
  fine. Documented.
- **⚪ T2.4 no faction-aware "trip the wire on the snapjaw chasing
  you" tactic surfaced in tests.** The TriggerFaction filter
  (existing in base) handles "I laid this and it ignores me" but
  the multi-segment broadcast doesn't filter faction-mates of OTHER
  segments. Acceptable v1 — a wire that fires on anyone walking on
  any segment is closer to actual tripwire physics.

---

## Verification (post-implementation)

Three layers:

1. **Per-fixture RED → GREEN cycles** during T2.2-T2.4:
   - T2.2: 6 tests
   - T2.3: 5 tests
   - T2.4: 5 tests
   - **Total**: 16 new tests

2. **Targeted regression sweep** after T2.4:
   ```
   run_tests EditMode group_names=[
     "LightMapEquipmentLightingTests",
     "PressurePlateTests", "TripWireTests",
     "TriggerOnStepPartTests", "TrapFurnitureTests",
     "FlamingSwordContentTests", "IceSwordContentTests",
     "ThunderHammerContentTests",
     "ScenarioCustomSmokeTests"
   ]
   ```
   Expected: 100% GREEN.

3. **Manual playtest** via existing scenarios:
   - Equip FlamingSword → walk into a dark zone → confirm red glow
   - Drop a PressurePlate (use `execute_code` or scenario) → walk
     across it twice → confirm 2nd hit disarmed by cooldown
   - 3-segment TripWire → step on segment 2 → confirm all 3 segments
     consumed + segment 1's cell occupant takes damage

---

## Implementation sequence (paced for Unity MCP)

Per the user's directive: "do not overload the mcp" — space MCP
calls out to ≥20s gaps and minimize requests.

```
1. Plan to disk + commit T2.1 (this commit)        [no MCP]
2. T2.2 LightMap edits + content + tests
   → 1 refresh + 1 test run                         [2 MCP calls]
3. T2.3 PressurePlateTriggerPart + blueprint + tests
   → 1 refresh + 1 test run                         [2 MCP calls]
4. T2.4 TripWireTriggerPart + tests
   → 1 refresh + 1 test run                         [2 MCP calls]
5. Targeted regression sweep
   → 1 test run                                     [1 MCP call]
6. Cold-eye review + roadmap update + commit T2.5
   → no MCP                                         [0]
7. Merge + push                                     [no MCP]
```

**Total MCP calls: 7** — same budget as Tier 1 closeout.

Expected total: ~250 lines new code + ~200 lines new tests + ~30
lines blueprint JSON + this plan (~400 lines). ~2 hr focused.

---

## What gets observable to the player after this ship

| Today | After T2 closeout |
|---|---|
| FlamingSword has a red render color but no glow | Equipping it lights a 4-tile red radius around the wielder |
| IceSword same | + 4-tile cyan glow |
| ThunderHammer same | + 3-tile yellow glow |
| Trap furniture: 3 single-use traps (Spike/Fire/Bear) | + PressurePlate (rearmable) + TripWire (multi-segment line) |
| Roadmap §"Light-emitting weapons / equipment": 1 ✅ | All ✅ |
| Roadmap §"Trap furniture" PressurePlate + TripWire: 2 💡 | Both ✅ |
| Tier 2 has 6 outstanding 💡/📋 items (across light + traps + locks + hunger) | **Tier 2 has 3 outstanding 💡 items** (lockpicking + consumable keys + hunger; deferred to their own ships per user scope) |
