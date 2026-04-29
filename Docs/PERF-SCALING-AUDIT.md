# Scaling Audit — How the Foundation Holds at Higher Loads

> Companion to `PERF-FOUNDATION.md`. The foundation doc covers
> patterns to USE; this doc covers what BREAKS at scale and which
> APIs need rework before content can grow.

**Scope.** Current load: 80×25 zone, ~2500 entities, 13 creatures,
4 light sources, ~5 simultaneous FX. Target: 5,000–10,000 entities,
50–100 creatures, 20–50 light sources, 50+ simultaneous FX (multi-mage
combat, settlement-at-night, dense magic encounters), 20+ status
effects per entity.

**Method.** Code-trace audit of all load-bearing APIs (Explore agent
+ direct file reads to verify). Each finding cites file:line and
concrete algorithmic complexity in N=entities, M=creatures, L=lights,
E=effects per entity, C=objects per cell. Severity at TARGET load,
not current.

---

## Findings sorted by severity at target load

### 🔴 Will hurt at target load — fix before content grows

#### S1 — Zone has no tag index; `GetEntitiesWithTagNonAlloc` is O(N)

**File:** `Assets/Scripts/Gameplay/World/Map/Zone.cs:166–174`

```csharp
public void GetEntitiesWithTagNonAlloc(string tag, List<Entity> result)
{
    result.Clear();
    foreach (var entity in _entityCells.Keys)
    {
        if (entity.HasTag(tag))
            result.Add(entity);
    }
}
```

Every NPC's `BoredGoal.TakeAction` calls `FindNearestHostileCached`,
which on cache miss calls `FindNearestHostile`, which calls this.
At current load: O(2500) entries × O(1) HashSet `HasTag` ≈ negligible.

At 10k entities × 100 NPCs scanning per turn × 1 cache miss per
4 turns = **250 full-zone scans per turn × 10k entities = 2.5M dict
lookups per turn**. Currently ~10ms; at target load ~100ms+.

**Fix:** add per-tag index:

```csharp
private readonly Dictionary<string, HashSet<Entity>> _tagIndex
    = new Dictionary<string, HashSet<Entity>>();
```

Maintain on `AddEntity` / `RemoveEntity` / `Tag.Add` /
`Tag.Remove`. `GetEntitiesWithTagNonAlloc` becomes:

```csharp
result.Clear();
if (_tagIndex.TryGetValue(tag, out var set))
    foreach (var e in set) result.Add(e);
```

O(1) lookup, O(matches) iterate. Drops creature-tag scans from
O(N) to O(M) where M = creatures only.

**Priority:** P0 (ship before adding settlement-scale content).

---

#### S2 — `AsciiFxBus` pool capped at 256

**File:** `Assets/Scripts/Gameplay/Effects/AsciiFxBus.cs:96` (constant
`MaxPoolSize = 256`)

Magic-heavy combat scenes can flood the pool. A single 100-damage
hit emits ~3 floating-number requests; a beam emits ~1; a chain
arc emits 1 per hop. Five enemies attacking simultaneously can
queue 30+ requests in a frame. With pool ceiling 256, a sustained
50+ FX burst will overflow → fresh allocations until burst ends.

The pool prevents unbounded growth (good), but the ceiling drops
allocation savings to zero during peak combat.

**Fix:** bump `MaxPoolSize` to 1024. Memory cost is bounded
(`AsciiFxRequest` is small), and the pool only grows under real
load; idle play won't hit the ceiling.

**Priority:** P1 (1-line change, zero risk).

---

### 🟡 Worth watching — fix when content adds load

#### S3 — `Cell.GetTopVisibleObject` is O(C) reverse scan with `GetPart<RenderPart>` per object

**File:** `Assets/Scripts/Gameplay/World/Map/Cell.cs:113–122`

```csharp
public Entity GetTopVisibleObject()
{
    for (int i = Objects.Count - 1; i >= 0; i--)
    {
        var render = Objects[i].GetPart<RenderPart>();
        if (render != null && render.Visible)
            return Objects[i];
    }
    return null;
}
```

Called per visible cell on every full `RenderZone` (~1000–2000
calls per redraw). At current load, cells average 2–3 objects → fine.
At target load (dense settlement cells with traps/furniture/pickups
stacking 8+ objects per cell): O(8) per cell × 2000 cells = 16,000
`GetPart<RenderPart>` calls per redraw.

**Fix:** cache `_topVisibleCache` on Cell, invalidate on
`AddObject` / `RemoveObject` / `RenderPart.Visible` change.

Caveat: `RenderPart.Visible` mutation is currently undirected — any
listener can flip it. Need a `Cell.InvalidateTopCache()` accessor
plus discipline on the few places that toggle Visible.

**Priority:** P2 (only matters once dense-stack cells ship).

---

#### S4 — `StatusEffectsPart.ApplyEffect` does O(E) type scan for stacking

**File:** `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs:62–71`

```csharp
for (int i = 0; i < _effects.Count; i++)
{
    if (_effects[i].GetType() == effect.GetType())
    {
        _effects[i].OnStack(effect);
        return false;
    }
}
```

Currently fine — entities have <5 effects. At target load (boss
creature with 20+ stacking effects from sustained DoTs), O(E) per
`ApplyEffect` × 5 reapplies per turn = 100 GetType comparisons per
creature per turn.

**Fix:** maintain `Dictionary<Type, Effect> _effectsByType` alongside
`_effects`. Update on Apply/Remove. `ApplyEffect` becomes O(1) lookup.

**Priority:** P2 (matters at 50+ NPCs with high-effect-density combat).

---

#### S5 — `MaterialSimSystem.TickMaterialEntities` scans full grid per turn

**File:** `Assets/Scripts/Gameplay/Materials/MaterialSimSystem.cs`
(approximately 80×25 cell loop calling `Cell.Objects` lookup per cell)

At current load: ~400 material parts. Cost is dominated by the
`MaterialPart` collection step, which walks every cell. Each turn.

At target load (1000+ material props in lit settlement at night):
full-grid scan stays O(2000), but per-tick effect application
grows linearly with material-part count.

**Fix:** maintain `Zone._materialEntities` set, updated on entity
add/remove with `MaterialPart`. Eliminates the grid scan; iteration
becomes O(material parts).

**Priority:** P2 (current ~1ms cost; fine until material parts pass
~1500).

---

#### S6 — `LightMap.Compute` invalidates on `EntityVersion` (any entity change)

**File:** `Assets/Scripts/Gameplay/World/LightMap.cs:34–64`

Already cached via `_lastEntityVersion` check (line 36). **GOOD.**
However, `EntityVersion` increments on every `MoveEntity` —
including non-light-source movements (player, NPCs, items thrown,
projectiles).

Result: lightmap recomputes O(L · R²) every time anyone moves —
~50 ops/turn at current load (4 lights × radius 6) ≈ negligible.

At target load (20 lights × radius 8 + 100 NPCs moving): lightmap
recomputes ~100×/turn at 20 × 64 = 1280 ops/recompute = 128k ops/turn.
Likely 5–10ms/turn.

**Fix:** introduce `Zone.LightVersion` separate from `EntityVersion`,
incremented only when:
- A LightSourcePart entity is added / removed / moved
- A light source's intensity / radius / color changes

Then `LightMap.Compute` cache check uses `LightVersion` instead of
`EntityVersion`. Most NPC moves stop invalidating the lightmap.

**Priority:** P2 (visible when night-lit settlement with active
NPCs ships).

---

### 🔵 Fine at target load

#### Confirmed-OK paths

These were checked and are O(1) or sublinear at scale:

| API | Complexity | Why fine |
|---|---|---|
| `Zone.GetEntityCell(entity)` | O(1) | `_entityCells` dict by reference |
| `Zone.MoveEntity` | O(1) + O(C) cell shuffle | dict + bounded cell-list ops |
| `Entity.GetPart<T>()` | O(P), P~5–10 | small list scan, called on hot path |
| `Entity.HasTag(name)` | O(1) | HashSet lookup |
| `Entity.GetIntProperty/GetProperty` | O(1) | dict |
| `Entity.GetStat / GetStatValue` | O(1) | dict |
| `Entity.FireEvent` | O(P), P~5–10 | small list scan |
| `BrainPart` cache validation | O(LOS) ≈ O(8) | Bresenham bounded by LOS radius |
| `FieldOfView.Compute` clear | O(2000) | property writes only, ~0.02ms |
| `FieldOfView.Compute` shadowcast | O(R²) | radius bounded; not entity-count |
| `RenderCellCore` `GameEvent.New` | pool hit | pooled, 0 alloc steady state |
| `AsciiFxRequest` allocation | pool hit | pooled to 256 (raise to 1024 — see S2) |
| `SaveSystem` serialization | O(N) | linear, async-able |

---

## Pattern-by-pattern scaling assessment

The `PERF-FOUNDATION.md` patterns hold up like this under target
load:

### Pattern 1 — Scratch list

**Holds.** Single-threaded gameplay means scratch lists never face
re-entrance contention. Bigger lists at scale just resize once at
peak; subsequent calls reuse the larger backing array.

**Watch:** if any future feature introduces async work (e.g., A*
on a worker thread), audit every static scratch list for
thread-safety. Add `[ThreadStatic]` or convert to `ArrayPool<T>`.

### Pattern 2 — Object pool (AsciiFxRequest, GameEvent)

**Holds with cap raise.** GameEvent pool already proven under
populated combat. AsciiFxRequest pool needs the cap raised for
50+ FX scenes (see S2).

**Watch:** if pool-pressure metrics show the ceiling being hit,
consider pool-size telemetry — log warning when allocations
exceed pool cap consistently.

### Pattern 3 — Snapshot fingerprint (sidebar / panels)

**Holds.** Fingerprint cost scales linearly with content size.
Sidebar fingerprint stays ~30–50 hash mixes even at target load
(message log capped at 50 entries; vitals lines fixed at 4).

**Watch:** new UI panels (inventory, spellbook, faction screen)
each need their own fingerprint. Don't share one fingerprint
across panels — that turns any panel change into a full-UI
re-render.

### Pattern 4 — Per-cell dirty tracking

**Holds.** HashSet<int> grows with dirty cells. Worst case is
"every cell dirty" = 2000 entries — still cheap. Major-zone events
(player moved → FOV change) trigger `_fullDirty` which wins over
per-cell anyway.

**Watch:** if zones grow beyond 80×25 (e.g., to 160×50 = 8000
cells), the `_fullDirty` path's `RenderZone` cost grows linearly.
At 8000 cells × ~6 tilemap ops = 48000 ops per full redraw — still
manageable but worth profiling.

### Pattern 5 — Validate-on-use cache (BrainPart hostile)

**Holds with caveat.** Cache validation is O(LOS) per call. Cache
miss is O(M·LOS) where M = creatures (NOT N = entities, because
the underlying `FindNearestHostile` already filters by tag —
which itself becomes O(N) without S1's tag index).

**S1 dependency:** without the Zone tag index, the BrainPart
cache hides an O(N) scan on every miss. After S1 ships, miss path
becomes truly O(M·LOS).

---

## Findings table (priority-sorted)

| # | Severity | Where | Pattern | Priority | Status |
|---|---|---|---|---|---|
| S1 | 🔴 | Zone.GetEntitiesWithTagNonAlloc | tag index missing | P0 | ✅ shipped (this commit) |
| S2 | 🔴 | AsciiFxBus pool cap=256 | bump to 1024 | P1 | ✅ shipped (this commit) |
| S3 | 🟡 | Cell.GetTopVisibleObject O(C) | cache + invalidate | P2 | ⚪ deferred — monitor at dense cells |
| S4 | 🟡 | StatusEffectsPart.ApplyEffect O(E) | per-type index | P2 | ⚪ deferred — monitor at high-effect bosses |
| S5 | 🟡 | MaterialSimSystem grid scan | maintain `_materialEntities` set | P2 | ⚪ deferred — monitor at 1500+ material parts |
| S6 | 🟡 | LightMap invalidates on any entity move | separate `LightVersion` | P2 | ⚪ deferred — visible only at night-lit settlements with movement |

---

## Triggers for re-running this audit

Re-run when any of these conditions hit:

- Zone size grows past 80×25 (e.g., a "town hub" zone gets bigger)
- Concurrent creature count in a single zone exceeds 30
- Light source count in any zone exceeds 10
- Status-effect-density (effects per entity) exceeds 10 in normal
  combat
- A new MonoBehaviour with `Update` / `LateUpdate` ships
- A new event listener fires per-frame or per-turn

The patterns themselves don't need rework — they hold to ~10× current
load. The data structures behind them (e.g., Zone's lack of a tag
index) are the bottlenecks, and they're spelled out above.
