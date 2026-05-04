# Performance Foundation — Strategies for New Features

> Compiled after the 2026-04-28 perf investigation that took combat
> from "incredibly laggy" to smooth. The fixes themselves are landed
> on `main`; this doc is the lessons-learned + design playbook for
> keeping the foundation stable as new content goes in.

---

## TL;DR — what to internalize before adding features

1. **Profile in real conditions, not unit tests.** A 5-call, JIT-warm
   isolated test hides the cost that compounds at scale. Use
   `Unity.Profiling.ProfilerRecorder` over real gameplay — the bug
   that took the foundation from 4fps to 600fps was invisible to
   isolated calls and only showed up under combat load.

2. **Cache misses must be cheap.** A cache where the miss-path does
   expensive work (rebuilds, regenerates, reparses) is a perf
   landmine. The foundation had **one** such cache
   (`CP437TilesetGenerator.GetTextTile`) and it was a 70,000× hot
   path before we fixed it.

3. **Tilemap operations dominate per-frame cost.** `Tilemap.SetTile`
   is ~50ns each, but 4000-op redraws are 200ms. Track dirty state
   at the right granularity. Skip work when nothing observable
   changed.

4. **Editor framerate ≠ build framerate.** `Application.runInBackground`
   defaults to false in Unity. With it off, the editor throttles to
   10fps on focus loss — looks like "the game is laggy" but is
   actually "the game is paused." Always set this true for
   development.

---

## How to profile correctly

### Live profiler hookup

Inside Unity (via `mcp__unity__execute_code`):

```csharp
var markers = new[] {
    "COO.ZoneRenderer.LateUpdate",
    "COO.UI.Sidebar.Render",
    // ... whichever markers you want
};
var opts = Unity.Profiling.ProfilerRecorderOptions.Default
         | Unity.Profiling.ProfilerRecorderOptions.SumAllSamplesInFrame;
var recorders = new List<Unity.Profiling.ProfilerRecorder>();
foreach (var m in markers) {
    recorders.Add(Unity.Profiling.ProfilerRecorder.StartNew(
        new Unity.Profiling.ProfilerCategory("Scripts"), m, 240, opts));
}
// Stash on AppDomain so multiple execute_code calls share state:
System.AppDomain.CurrentDomain.SetData("MY_RECORDERS", recorders);
```

Wait 60-90 seconds while the user plays, then read avg/max/p99
across the 240-sample window. Sort by **max** to catch spikes —
sorting by avg can hide infrequent-but-bad frames.

### Per-phase breadcrumbs

When a marker shows a spike but its sub-markers don't, instrument
inline with stopwatch + conditional log:

```csharp
double tA = 0, tB = 0;
long ts = Stopwatch.GetTimestamp();
DoPhaseA();
tA = ElapsedMilliseconds(ts, ts = Stopwatch.GetTimestamp());
DoPhaseB();
tB = ElapsedMilliseconds(ts, Stopwatch.GetTimestamp());

if (totalMs > spikeThresholdMs)
    Debug.Log($"[Perf] phase: A={tA:F2}ms B={tB:F2}ms");
```

Keep the instrumentation gated behind
`PerformanceDiagnostics.VerboseLoggingEnabled` so production
builds don't pay the cost. Remove inline timing once the bug is
diagnosed (don't accumulate diagnostic code).

### What "looks fine" actually looks like

| Reading | Healthy | Investigate |
|---|---|---|
| `ZoneRenderer.LateUpdate` avg | < 3ms | > 8ms |
| `UI.Sidebar.Render` avg | < 0.5ms (skipped most frames) | > 2ms |
| `Main Thread` avg | < 5ms | > 16ms (60fps budget) |
| `cpu_frame_time` from `manage_profiler get_frame_timing` | < 16ms | > 50ms (look for editor throttle) |
| `GC.Alloc/frame` | < 5KB | > 32KB or any frame > 100KB |

If `cpu_frame_time` is high but `cpu_main_thread_frame_time` is
near zero, you're hitting Unity's focus-loss throttle. Fix:
`Application.runInBackground = true` or check
`ProjectSettings/ProjectSettings.asset` line `runInBackground: 1`.

---

## Optimization patterns (use these for new features)

### Pattern 1 — Scratch list

For collections used inside Update / LateUpdate / per-turn loops:

```csharp
// At class scope
private static readonly List<Foo> _scratch = new List<Foo>(8);

// In hot path
public void Hot() {
    var list = _scratch;
    list.Clear();              // O(N) on bucket count, not capacity
    list.Add(...);
    DoWork(list);
}
```

**Caveats:**
- Single-threaded use only (gameplay is turn-serial in this project).
- If `DoWork` could re-enter `Hot()`, switch to `ArrayPool<Foo>`.
- The list reference must not be persisted by the consumer past the
  current frame; if it is (e.g. snapshot pattern), allocate per call.

**Real examples:**
- `MovementSystem._enteredCellScratch`
- `CombatSystem._gatherWeaponsScratch`
- `SidebarStateBuilder._dualLineScratch` (StringBuilder)
- `AsciiFxRenderer._aurasToRemoveScratch`

### Pattern 2 — Object pool

For objects allocated many times per turn / frame:

```csharp
private static readonly Stack<Foo> _pool = new Stack<Foo>(64);
private const int MaxPoolSize = 256;

public static Foo Rent() {
    if (_pool.Count > 0) {
        var f = _pool.Pop();
        f.Reset();              // wipe state
        return f;
    }
    return new Foo();
}

public static void Release(Foo f) {
    if (f == null) return;
    if (_pool.Count >= MaxPoolSize) return;  // bounded
    _pool.Push(f);
}
```

**Real examples:**
- `GameEvent.Rent` / `GameEvent.Release` — `FireEventAndRelease`
  helper makes the common case fail-safe.
- `AsciiFxBus.Rent` / `AsciiFxBus.Release`.

**Pool-leak failure mode:** if callers don't release, the pool
grows fresh allocations on miss while old objects are GC'd. Use
`FireEventAndRelease`-style wrapper helpers; reserve manual
release for paths that need post-fire param reads.

### Pattern 3 — Snapshot fingerprint

For renderers / UI panels that may not need to redraw every frame:

```csharp
public void Render(Snapshot s) {
    int fp = ComputeFingerprint(s);
    if (!_needsRedraw && fp == _lastFingerprint) return; // skip

    DoExpensiveDraw(s);
    _lastFingerprint = fp;
    _needsRedraw = false;
}

public void Invalidate() => _needsRedraw = true;

private static int ComputeFingerprint(Snapshot s) {
    unchecked {
        int hash = s.Count;
        for (int i = 0; i < s.Count; i++)
            hash = hash * 31 + (s[i].Text?.GetHashCode() ?? 0);
        return hash;
    }
}
```

**When to use:** any UI panel that re-renders on every frame but
whose content rarely changes. Sidebar (140× speedup), inventory,
hotbar overlay.

**When NOT to use:** content that genuinely changes every frame
(animations, time-based fades). Fingerprint cost approaches the
re-render cost.

### Pattern 4 — Per-cell / granular dirty tracking

For tilemap-backed renderers:

```csharp
private bool _fullDirty = true;
private readonly HashSet<int> _dirtyCells = new HashSet<int>();

public void MarkCellDirty(int x, int y) {
    if (_fullDirty) return;          // already covered
    _dirtyCells.Add(EncodeCellKey(x, y));
}

public void MarkDirty() => _fullDirty = true;

private void Update() {
    if (_fullDirty) {
        RenderAll();
        _fullDirty = false;
        _dirtyCells.Clear();
    } else if (_dirtyCells.Count > 0) {
        foreach (int key in _dirtyCells) RenderCell(...);
        _dirtyCells.Clear();
    }
}
```

**Real example:** `ZoneRenderer._dirtyCells` + `ZoneRenderHooks`
static callback bridge. Drops 2000-op full redraws to ~10-op
incremental redraws on NPC moves.

### Pattern 5 — Validate-on-use cache

For caches keyed by complex state (entity, position, radius):

```csharp
public Foo GetCached(Self self, Zone zone, int radius) {
    if (HasFreshCache && IsStillValid(_cached, self, zone, radius)) {
        TickCacheTtl();             // bound staleness
        return _cached;
    }
    InvalidateCache();
    var fresh = ExpensiveCompute(self, zone, radius);
    if (fresh != null) RefreshCache(fresh);
    return fresh;
}
```

**Real example:** `BrainPart` hostile-target cache via
`AIHelpers.FindNearestHostileCached`. Drops AI scan from
O(N·LOS) per turn to O(LOS) when targets are stable.

**Critical detail:** only cache **positive** results. Caching
"no result found" is a footgun — a new event between cache
fill and TTL expiry would be invisible to the AI for K turns.

---

## Anti-patterns to avoid

### Anti-pattern 1 — expensive cache miss

```csharp
public Tile GetTile(char c) {
    if (cache.TryGetValue(c, out var t)) return t;
    GenerateAtlas();                 // ~2ms — runs on EVERY miss
    if (cache.TryGetValue(c, out t)) return t;
    return cache['?'];               // fallback
}
```

The `GenerateAtlas()` call on miss-path is a perf landmine. If
the cache only contains a fixed range (e.g. CP437 0-255) and the
input domain is wider (e.g. arbitrary Unicode), every miss burns
the rebuild cost — and STILL misses afterward. Either:

a) Don't regenerate on miss; just return fallback.
b) Pre-populate the cache for the full input domain.

This was the **single biggest perf win** in this investigation
(70,000× speedup on `═` divider chars).

### Anti-pattern 2 — LINQ in hot paths

```csharp
// Allocates IEnumerable + ToList
var hostiles = entities.Where(e => e.IsHostile).ToList();

// vs. plain loop with scratch list
foreach (var e in entities) if (e.IsHostile) _scratch.Add(e);
```

LINQ's allocation cost (enumerator + materialized list) is
fine in setup code, deadly in per-frame code. Greppable rule:
**`.Where(`, `.Select(`, `.ToList(`, `.OrderBy(` should not
appear in any method called from `LateUpdate` / `Update` /
turn loops.**

### Anti-pattern 3 — string concat in loops

```csharp
// 5 intermediate string allocations per call
return left + " " + (val ?? "-") + " | " + right + " " + (val2 ?? "-");

// vs. cached StringBuilder
_sb.Clear();
_sb.Append(left).Append(' ').Append(val ?? "-")
   .Append(" | ")
   .Append(right).Append(' ').Append(val2 ?? "-");
return _sb.ToString();
```

Per-call allocation from string concat in a 4-vital × every-frame
sidebar path was 0.1-0.3ms; trivial individually but compounds.

### Anti-pattern 4 — reflection on the hot path

```csharp
// bad — getting field via reflection every call
var f = type.GetField("foo", BindingFlags.NonPublic | ...);
var val = f.GetValue(instance);
```

Cache `Type`, `FieldInfo`, `MethodInfo` references at static init
or use `Func<>`/`Action<>` delegates. Reflection in turn loops
costs ~10× the equivalent direct call.

### Anti-pattern 5 — `GameObject.Find` / `GetComponent` in Update

Standard Unity pitfall. Cache references in `Awake` / `Start`.
The audit found none of these in the gameplay layer; if you add
new MonoBehaviours, follow this rule.

---

## Editor + project settings checklist

Before you suspect a code issue:

- [ ] `ProjectSettings.asset` `runInBackground: 1` (or
      `Application.runInBackground = true` in code)
- [ ] `vSyncCount = 0` if you want to see real framerate (or `1`
      to lock at refresh rate for production)
- [ ] `targetFrameRate = -1` (uncapped) for dev measurement
- [ ] Editor not in deep-profile mode unless investigating
      (deep profile costs ~20× normal)

If `cpu_frame_time` from `get_frame_timing` is much higher than
`cpu_main_thread_frame_time`, the script side is fine — the
delta is some combination of GPU stall, render-thread wait, or
editor throttle.

---

## Per-feature checklist (use when designing new content)

For any new feature touching the per-frame or per-turn paths:

- [ ] **Render hook plan**: does this visibly change a cell? If
      yes, plumb `ZoneRenderHooks.MarkCellDirty(x, y, source)`
      from the gameplay path. If no, no render hook needed.
- [ ] **Allocation audit**: does any method called from `Update` /
      `LateUpdate` / turn loops `new List/Dict/array`? If yes,
      pattern-1 it.
- [ ] **String paths**: do format/concat strings appear in hot
      paths? Cache via fingerprint or use StringBuilder.
- [ ] **LINQ check**: zero LINQ in hot paths.
- [ ] **Cache invariants**: any `Dictionary<,>` lookup followed by
      "compute and insert"? Verify the compute path is cheap or
      gated by explicit invalidation.
- [ ] **Event count**: does this fire `GameEvent.New` per cell /
      per entity / per tick? Use `FireEventAndRelease` so the
      pool doesn't leak.
- [ ] **Cell visibility**: does this change `Cell.IsVisible` or
      lighting? FOV/lightmap recompute is gated on the
      `_fullDirty` path; if you set per-cell vis without
      triggering full-dirty, the visible state stays stale until
      next full redraw.

If two or more of these apply, write the feature plan in
`Docs/<feature>.md` first (per the major-feature workflow in
CLAUDE.md), and add a **Performance** section to the plan citing
which patterns from this doc you'll use.

---

## Audit findings table (current state of `main`)

| # | Severity | Where | Status |
|---|---|---|---|
| 1 | 🔴 | `AsciiFxRenderer.UpdateAuras` `new List<AuraKey>()` per frame | ✅ scratch list |
| 2 | 🔴 | `CP437TilesetGenerator.GetTextTile` regen on Unicode miss | ✅ removed regen |
| 3 | 🟡 | `SidebarStateBuilder.ComposeDualLine` 5-fragment concat | ✅ StringBuilder |
| 4 | 🟡 | `SidebarStateBuilder.BuildStatusText` `string.Join` per frame | ✅ content-hash cached |
| 5 | 🟡 | `LookQueryService.BuildSnapshot` multiple `new List<>()` | ⚪ deferred — only fires on cursor move; not a hot path |
| 6 | 🟡 | `ZoneRenderer.RenderDirtyCells` recomputes FOV+LightMap unnecessarily | ✅ skipped on per-cell path |
| 7 | 🟡 | `LightMap.Compute` full recompute every frame | ✅ now only in `RenderZone` (full dirty) path |
| 8 | 🔵 | `ZoneRenderer.RenderCellCore` fires `GameEvent.New("Render")` per cell | ⚪ deferred — pool already in place; ~0.5ms total |
| 9 | 🔵 | `LightMap.HasLineOfSight` quadratic per light | ⚪ deferred — current cost ~1-2ms in `RenderZone`, acceptable |
| 10 | 🔵 | `CombatSystem` log-string concat per attack | ⚪ deferred — per-turn, not per-frame |

**Steady-state frame budget after all fixes:**

```
COO.ZoneRenderer.LateUpdate   ≈ 1.6ms      (was 220ms)
  ├─ RenderSidebar             ≈ 0.08ms    (was 220ms)
  ├─ UpdateAmbientAnimations   ≈ 0.78ms
  ├─ RenderHotbar              ≈ 0.71ms
  └─ RenderZone (when dirty)   ≈ 7ms       (was 7ms — same)

Main Thread avg                ≈ 3-5ms     (300+fps potential)
GC.Alloc/frame                 ≈ 3KB
```

---

## Living follow-ups

If you make changes that affect any of these baselines, update the
**Steady-state frame budget** table above with the new numbers.
Treat regressions ≥ 2× as 🟡 self-review findings to address
pre-commit.

If you find a new perf landmine while implementing a feature:
1. Diagnose via `ProfilerRecorder`
2. Pick the matching pattern from this doc
3. Land the fix in a `perf/<short-name>` branch
4. Update this doc's findings table

The performance investigation that produced this doc is in
`Docs/PERF-COMBAT-INVESTIGATION.md` — it has the original audit's
hypotheses and which ones turned out to be true vs. red herrings.
