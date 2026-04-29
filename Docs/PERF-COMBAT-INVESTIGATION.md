# Combat Performance — Deep-Dive Investigation Plan

> Status: Phase 1 (codebase audit) complete. Phase 2 (live profiling)
> next, then phased fix implementation in priority order.

## Symptom

> "performance is absolutely terrible — it's been like that for a little
> while" — and after the initial perf fixes (`b2b2289`: 98% diagnostic
> revert + per-weapon parser cache), the slowdown still kicks in
> specifically when fighting NPCs.

Observable: framerate drops in populated combat. Doesn't trigger when
exploring or idling. Stronger in showcase scenarios (4–5 padded NPCs)
than vanilla zones. Suggests per-NPC-per-turn overhead, not a one-time
load cost.

## What we already shipped (b2b2289)

| Fix | Effect |
|---|---|
| Diagnostic 98% chance → 15%/25% | Removed log spam from stun-locked NPCs (large) |
| Per-weapon `OnHitEffectsRaw` parser cache | Eliminated per-hit `List+string[]` alloc (medium) |

These addressed the most obvious hot spots but didn't touch the
structural problems found in the Phase-1 audit below.

---

## Phase 1 — Audit results (3 parallel Explore agents)

Each finding cites `file:line`. Triaged by per-turn cost in populated
combat.

### 🔴 Tier A — likely dominant contributors

#### A1 · `GameEvent` pool leak (massive)

**File:** `Assets/Scripts/Gameplay/Events/GameEvent.cs:18, 40-49, 51-79`

- Pool is a `Stack<GameEvent>` of capacity 32, max 64. `Rent(string id)`
  pulls from pool when available; otherwise allocates fresh.
- `Release()` returns to pool — but **only ~3 call sites** invoke it
  (`Entity.FireEventAndRelease`, `Entity.SetStatValue`, `Entity.UseMP`).
- All other event fires use `FireEvent()` → never released → leaked.
- ~20+ leaking sites enumerated by Agent 1, including:
  - `CombatSystem.ApplyDamage`: 6 events per damage application
    (`BeforeTakeDamage`, `DamageFullyResisted`, `TakeDamage`, `DamageDealt`,
    `Died`, …)
  - `CombatSystem.PerformMeleeAttack`: `BeforeMeleeAttack`, `CanBeDismembered`
  - `StatusEffectsPart.CheckBeforeApply` / `SendApplied` / `SendRemoved`:
    multiple per effect lifecycle
- After the pool fills (3-4 turns of populated combat), every event
  allocates fresh → constant GC pressure.

**Multiplier in our gameplay**: each `ApplyDamage` alone leaks ~5
events. With 10 NPCs each landing/taking 1-2 hits per turn = ~100
leaked event allocations per turn before counting effect ticks.

#### A2 · `ZoneRenderer` does full-screen redraw on every change

**File:** `Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs:405-413, 494-498, 520-561, 553-557, 634`

- Renderer is dirty-flag-driven (good in principle): `MarkDirty(source)`
  sets `_dirty = true`.
- But **the dirty granularity is "anything in the zone"** — there's no
  per-cell or per-region dirty tracking.
- When `_dirty == true`, `RenderZone()` does:
  - `ClearAllTiles()` on main + background tilemaps (lines 526–532)
  - 80×25 = **2000 `SetTile()` calls** every redraw (line 553-557, 634)
- Every entity move, every damage event, every floating-number emission
  triggers `MarkDirty()`. In populated combat, that's ~10-30 redraws
  per turn → 20,000-60,000 SetTile calls per turn.

**Why it's slow**: `Tilemap.SetTile` is a Unity engine call that
internally invalidates the chunk + dirties the rendering data. Doing
2000 of them is ~ms-scale per redraw; 30 redraws/turn = potentially
30+ms of overhead per turn frame.

#### A3 · AI target scan is O(N · radius) per NPC per turn

**Files:**
- `Assets/Scripts/Gameplay/AI/AIHelpers.cs:105-147` (`FindNearestHostile`)
- `Assets/Scripts/Gameplay/AI/Goals/BoredGoal.cs:67`
- `Assets/Scripts/Gameplay/AI/Goals/KillGoal.cs:38-67`
- `Assets/Scripts/Gameplay/AI/AIHelpers.cs:287-327` (`TryApproachWithPathfinding`)

- Every NPC's `BoredGoal.TakeAction` calls `FindNearestHostile(self,
  zone, radius=8)`.
- `FindNearestHostile` loops every Creature-tagged entity in the zone,
  doing a Bresenham line-of-sight + faction check per candidate.
- **Per-turn complexity**: `O(M · N · radius)` where M = bored NPCs,
  N = creatures, radius ≈ 8-10 cells walked.
- For the showcase scenarios (5 padded NPCs + player), this is small.
  For a vanilla populated zone (15-30 entities), it scales fast.
- Every blocked `KillGoal` recomputes A* from scratch (no path cache).

**No spatial partition, no enemy-cache, no precomputed visibility** —
every NPC re-derives its entire awareness state every turn.

### 🟡 Tier B — significant contributors

#### B1 · `AsciiFxBus.EmitFloatingNumber` allocates per digit

**File:** `Assets/Scripts/Gameplay/Effects/AsciiFxBus.cs:265-316`

- For "100 damage" the bus enqueues **3 separate `AsciiFxRequest`
  objects** (one per digit, line 308-315).
- 20 NPCs taking damage in a turn = 60 allocations just for damage
  numbers.

#### B2 · `Damage.HasAttribute` linear scans (cumulative)

**File:** `Assets/Scripts/Gameplay/Combat/Damage.cs:55-59, 112-152`

- `HasAttribute(name)` is `Attributes.Contains(name)` — O(K) string-equality scan.
- `IsHeatDamage` does 2 calls; `IsColdDamage` 3; `IsElectricDamage` 4;
  `IsAcidDamage` 1; `IsBludgeoningDamage` 2.
- `ApplyResistances` (CombatSystem.cs:646-649) calls all four
  `IsXDamage` helpers in sequence.
- Per `ApplyDamage` on a 5-attribute Damage: 1 + 2 + 3 + 4 + (others) =
  ~50 string comparisons.
- Not catastrophic alone but compounds with leaked allocations.

#### B3 · `CombatSystem.GatherMeleeWeapons` allocates `List` per attack

**File:** `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:212`

- `var weapons = new List<MeleeWeaponPart>();` per attack.
- Per turn × per NPC swing → constant allocation churn.

#### B4 · `SidebarStateBuilder` allocates per frame

**File:** `Assets/Scripts/Presentation/UI/SidebarStateBuilder.cs:129-153`

- `MessageLog.GetRecentEntries(30)` allocates a fresh `List<Entry>` per
  call.
- Plus `var entries = new List<SidebarLogEntry>()` at line 132.
- Called per frame (presumably) → 60 allocs/sec just for sidebar
  refresh.

### 🔵 Tier C — minor / pre-existing

| File:line | Issue |
|---|---|
| `Damage.cs:91-104` | `AddAttributes` does `string.Split(' ')` per hit — pre-existing, low-impact |
| `GameBootstrap.cs:121` | `Debug.Log` per `MessageLog.Add` — Editor-only, pre-existing |
| `BrainPart.cs:267` | `new BoredGoal()` push on first turn — one-shot |

---

## Phase 2 — Validation profile (next)

Before committing fixes, confirm the Tier-A hypotheses with live data.

### Profile setup

1. Load `On-Hit Effects Showcase` (4 padded snapjaws).
2. Engage all 4 hostile.
3. Use Unity Profiler or `mcp__unity__manage_profiler` to capture:
   - GC alloc per turn
   - Top-K allocation call sites
   - Frame time during combat vs. idle
4. Walk 10 turns of combat, snapshot results.

### Decision matrix

If the profile shows:

| Observation | Confirms hypothesis | Fix priority |
|---|---|---|
| Top alloc = `GameEvent` ctor | A1 (pool leak) | 🔴 critical |
| `Tilemap.SetTile` shows up high | A2 (renderer redraw) | 🔴 critical |
| AI takes >5ms per NPC turn | A3 (O(N²) target scan) | 🔴 critical |
| `AsciiFxRequest` ctor in top-20 | B1 | 🟡 then |
| `String.Equals` / `List.Contains<string>` high | B2 | 🟡 then |

If profile shows something **not on the list above**, that's the new
top suspect — investigate before committing to the fix order below.

---

## Phase 3 — Fix plan (priority order)

Each fix is its own commit per CLAUDE.md §1.4. RED test where the
behavior is testable; otherwise document as a manual-verification ship.
Each fix has explicit "stop condition" — the perf metric that
should improve.

### Fix 1 · `GameEvent` pool: audit + plug all leaks (🔴 A1)

**Branch:** `perf/event-pool-plug-leaks`

**Approach:** Audit every `GameEvent.New(...)` / `Rent(...)` call site.
For each, ensure the event is released after firing. Two patterns:

- For "fire and forget" events (most), use `FireEventAndRelease(e)`.
- For events whose parameters are read after the fire (rare), keep
  `FireEvent(e)` but explicitly call `e.Release()` after the read.

**Concrete steps:**
1. Grep `GameEvent.New|GameEvent.Rent` across `Assets/Scripts/Gameplay`.
   Expect ~30 sites.
2. For each:
   - If the next line is `FireEvent(e)`, replace with `FireEventAndRelease(e)`.
   - If parameters are read after fire, append `e.Release();`.
3. Add a debug-build assertion: pool overflow logs a warning so
   regressions are visible.
4. Run combat-adjacent regression tests (the 154-test sweep).
5. Profile + verify alloc count drops dramatically.

**Stop condition**: After 10 turns of populated combat, GC alloc per
turn is < 1 KB (currently expected to be 10-50× higher).

**Expected impact**: ~80% reduction in per-turn allocation. Single
biggest win.

### Fix 2 · `ZoneRenderer`: per-cell dirty tracking (🔴 A2)

**Branch:** `perf/zone-renderer-dirty-cells`

**Approach:** Replace the global `_dirty` boolean with a `HashSet<Vec2Int>`
of dirty cells. Only `SetTile` for cells in that set; clear after redraw.

**Concrete steps:**
1. Add `HashSet<Cell>` `_dirtyCells` field; default-allocated once,
   `Clear()` after redraw (no per-frame alloc).
2. `MarkDirty(cell)` overload that adds the cell, plus a
   `MarkDirty(string source)` that marks the whole zone if needed
   (rare — zone changes, not individual entity changes).
3. `RenderZone()`: if `_dirtyCells` populated, iterate only those.
   Else fall back to full redraw if `_fullDirty` is set.
4. Update all `MarkDirty` call sites to pass cell coords:
   - `MovementSystem.TryMove` → mark old + new cell dirty
   - `CombatSystem.ApplyDamage` → mark target cell dirty (for floating
     numbers / damage flash)
   - Entity death → mark the cell where the entity was
5. Test by walking a snapjaw through a corridor: profile should show
   `SetTile` call count drop from ~2000 to ~2.

**Stop condition**: `Tilemap.SetTile` calls per turn reduce from
~20,000-60,000 to under 100 in typical combat.

**Expected impact**: ~30-50% framerate improvement during combat.

### Fix 3 · AI target cache + spatial query (🔴 A3)

**Branch:** `perf/ai-target-cache`

**Approach:** Cache the result of `FindNearestHostile` per-NPC,
invalidated when:
- The cached target moves out of sight
- The cached target dies
- The NPC's own goal stack changes

**Concrete steps:**
1. Add `BrainPart._lastKnownHostile` (entity ref) +
   `_lastKnownHostileTick` (turn count of last refresh).
2. `BoredGoal.FindNearestHostile`:
   - If cache is recent (< K turns old), validate via cheap
     `IsAlive() + LOS`. If still valid, reuse.
   - Else re-scan and refresh.
3. Optionally: `Zone.GetEntitiesWithinRadius(center, radius)` using a
   spatial hash (defer if cache alone fixes it).
4. **Risk**: A target that moves a single tile but stays in LOS still
   hits the LOS check. That's fine — LOS is cheap. The win is skipping
   the full N-creature scan.

**Stop condition**: AI per-turn time drops from O(M·N·LOS) to
O(M·LOS_check) when targets are stable (which is most of combat).

**Expected impact**: ~40-60% AI cost reduction in populated zones.

### Fix 4 · `AsciiFxBus.EmitFloatingNumber`: pool requests + emit as one (🟡 B1)

**Branch:** `perf/asciifx-pool-and-batch`

**Approach:**
- Pool `AsciiFxRequest` objects.
- For damage numbers, emit ONE request that carries the whole string,
  not one per digit. Renderer can lay out the digits at draw time.

**Stop condition**: `AsciiFxRequest` allocations per turn drop to ~0.

**Expected impact**: small but uniform — better for low-end machines.

### Fix 5 · `Damage.HasAttribute`: bitmask or pre-hashed set (🟡 B2)

**Branch:** `perf/damage-attribute-bitmask`

**Approach:** Convert known attribute names to a `[Flags] enum`
bitmask. `Damage.AttributeFlags` is a single int OR'd from contributing
flags. `IsHeatDamage()` becomes a single bit check.

**Trade-off**: loses ability to add arbitrary string attributes
dynamically. Mitigate by keeping the string list for unknown
attributes, bitmask for known ones.

**Stop condition**: `Damage.HasAttribute`/`IsXDamage` cumulative time
drops from ~50 string comparisons per ApplyDamage to ~5 bit ops.

**Expected impact**: small per-call but compounds with high call volume.

### Fix 6 · `CombatSystem.GatherMeleeWeapons`: scratch list (🟡 B3)

**Branch:** `perf/gather-weapons-scratch-list`

**Approach:** A static `List<MeleeWeaponPart> _scratchWeapons`
cleared+reused per call. Combat is single-threaded, so a static is safe.

**Stop condition**: Per-attack alloc drops by 1 List per attack.

**Expected impact**: small; fixes a specific allocation site.

### Fix 7 · `SidebarStateBuilder`: cache last-rendered or batch-update (🟡 B4)

**Branch:** `perf/sidebar-cache-or-throttle`

**Approach:**
- Track `MessageLog.NextSerialValue` from the last build.
- If unchanged since last frame, return cached output (no rebuild).

**Stop condition**: Sidebar build only runs when log changes (~once
per turn) instead of every frame.

**Expected impact**: ~60 allocs/sec saved when nothing's logging.

---

## Phase 4 — Verification

After each fix lands, re-profile the 10-turn populated-combat scenario
and capture:

| Metric | Before (Phase 2 baseline) | After fix | Target |
|---|---|---|---|
| Total GC alloc per turn | TBD | TBD | < 5 KB |
| Frame time during combat | TBD | TBD | within 2× idle |
| `SetTile` calls per turn | TBD | TBD | < 100 |
| AI time per NPC turn | TBD | TBD | < 1ms |

If a fix lands and the metrics don't improve as expected, **stop and
re-investigate** — don't pile fixes on top.

---

## Honesty bounds (per CLAUDE.md §6.3)

What this audit can claim:
- Code paths confirmed via file:line citations.
- Patterns ("O(N²)", "allocates per call") inferred from source shape.

What this audit *cannot* claim without Phase-2 profiling:
- Which specific bottleneck is producing the user-visible stutter.
- The actual cost of any single hypothesis in milliseconds.
- Whether fixing #1 alone restores acceptable performance (could be
  several issues compounding).

The ranked priority above is best-guess based on per-turn cost shape;
Phase 2 may reorder it.

---

## What's *not* in this plan

- **Frame-rate target tuning** (e.g. forcing 60fps caps): out of scope
  until structural fixes land.
- **Scenarios with dozens of entities**: showcase-scale (4-5 NPCs)
  should work first; large-zone perf is a follow-up.
- **Render upgrades**: switch to ECS/DOTS, rewrite with sprites:
  premature; the dirty-cells fix alone should be enough.
- **Save-system perf**: confirmed by audit not to be a hot-path issue
  in current state.

---

## Implementation order (executive summary)

1. **Profile first** (Phase 2) — confirm Tier-A ranking before fixing
2. **Plug `GameEvent` pool leaks** — most likely the single biggest win
3. **Implement per-cell renderer dirty tracking** — large frame-time win
4. **Cache AI target lookups** — fixes O(N²) AI scaling
5. **Re-profile** — confirm Tier-B fixes are still needed
6. **Apply Tier-B fixes if perf still off-target**

Stop after each step if the metric improves enough to call it shipped.
Don't overfit fixes to imagined problems.
