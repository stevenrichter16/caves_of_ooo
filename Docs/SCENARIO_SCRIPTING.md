# C# Fluent Scenario Library — Detailed Plan

**Status:** Planning. Not yet implemented.

**Purpose:** Let the developer launch the game into specific engineered setups by
clicking a menu item — rather than walking around the world hoping for the right
conditions to line up for testing a mechanic.

## Goals (in priority order)

1. **Launch the game into a specific engineered setup.** Click a menu item → Play
   mode enters → player is placed → scenario script runs → world has been modified
   → you playtest manually. *This is the primary goal.*

2. **Author scenarios in ergonomic C#.** Reading a scenario should feel like reading
   a stage direction: "spawn 5 snapjaws around the player." Writing one should take
   under 5 minutes for common cases.

3. **Scenarios are discoverable and one-click runnable.** No CLI flags, no editing
   config files — a menu/window with every scenario grouped by category.

4. **Reuse for automated tests later (secondary).** The same helpers should work
   from NUnit PlayMode tests or MCP `execute_code` so investment compounds.

## Non-goals

- **Not a modding platform.** Scenarios are trusted C# authored by the developer.
- **Not hot-reloadable without recompile.** Edit → Unity recompile (~3s) → re-enter
  Play. That's fine for this use case.
- **Not a DSL.** No new language, no parser. Pure C# with a helpful fluent API.
- **Not procedural scenario generation.** Scenarios are authored; parametric
  variations (N snapjaws instead of 5) are simple C# loops inside `Apply`.

---

## Architecture at a glance

```
┌──────────────────────────────────────────────────────────────┐
│  Assets/Scripts/Scenarios/                                   │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ Core: IScenario, [Scenario] attribute, Context       │    │
│  └──────────────────────────────────────────────────────┘    │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ Builders: EntityBuilder, PlayerBuilder, ZoneBuilder  │    │
│  │ Resolvers: PositionResolver, CellFinder              │    │
│  └──────────────────────────────────────────────────────┘    │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ Runner: ScenarioRunner (singleton, pending+applied)  │    │
│  └──────────────────────────────────────────────────────┘    │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ Scenarios/Custom/ (user-authored)                    │    │
│  └──────────────────────────────────────────────────────┘    │
├──────────────────────────────────────────────────────────────┤
│  Assets/Editor/Scenarios/                                    │
│  - ScenarioMenu: auto-generated menu items                   │
│  - ScenarioWindow: dockable browser (later)                  │
└──────────────────────────────────────────────────────────────┘

Hook into runtime: GameBootstrap fires `OnScenarioApply` event
at end of Start(), after player + zone are ready. ScenarioRunner
is the sole subscriber.
```

---

## The API surface (concrete examples)

### A simple scenario

```csharp
// Assets/Scripts/Scenarios/Custom/FiveSnapjawAmbush.cs
using CavesOfOoo.Scenarios;

[Scenario("Five Snapjaw Ambush", category: "Combat Stress",
    description: "Player surrounded by 5 snapjaws in a ring.")]
public class FiveSnapjawAmbush : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        ctx.Spawn("Snapjaw").AtPlayerOffset(2, 0);
        ctx.Spawn("Snapjaw").AtPlayerOffset(-2, 0);
        ctx.Spawn("Snapjaw").AtPlayerOffset(0, 2);
        ctx.Spawn("Snapjaw").AtPlayerOffset(0, -2);
        ctx.Spawn("Snapjaw").AtPlayerOffset(2, 2);
    }
}
```

### A scenario with entity modifications

```csharp
[Scenario("Wounded Warden", category: "AI Behavior",
    description: "Warden at 20% HP out of player sight — watch her retreat.")]
public class WoundedWarden : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        ctx.Spawn("Warden")
           .AtPlayerOffset(8, 0)
           .WithHp(fraction: 0.20f)
           .WithStartingCell(atSpawnPosition: true);
    }
}
```

### A scenario with player setup

```csharp
[Scenario("Calm Test", category: "Mutations")]
public class CalmTestSetup : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        ctx.Player
            .AddMutation("CalmMutation", level: 3)
            .SetHp(max: true);

        // Line up 3 hostiles for target practice
        ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
        ctx.Spawn("Snapjaw").AtPlayerOffset(5, 0);
        ctx.Spawn("Snapjaw").AtPlayerOffset(7, 0);
    }
}
```

### A scenario with world + composition

```csharp
[Scenario("Mimic Surprise", category: "Content Demo")]
public class MimicSurprise : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        // A real chest (decoy)
        ctx.PlaceObject("Chest").AtPlayerOffset(3, 0);

        // A mimic disguised identically
        ctx.Spawn("MimicChest").AtPlayerOffset(5, 0);

        // Some gold in the real chest
        ctx.Spawn("GoldPile", count: 10).AtPlayerOffset(3, 0);
    }
}
```

### A scenario reused for automated testing

```csharp
// Same Apply works from a PlayMode test:
[UnityTest]
public IEnumerator WoundedWarden_RetreatsToStartingCell()
{
    var ctx = ScenarioContext.FromLiveGame();
    new WoundedWarden().Apply(ctx);

    yield return ctx.AdvanceTurns(10);

    ctx.Assert.Entity("Warden").HasGoal<RetreatGoal>();
    ctx.Assert.Entity("Warden").IsAtCell(playerOffset: (8, 0));
}
```

Same helpers, same fluent API, different runner.

---

## Fluent API reference (MVP scope)

### `ScenarioContext` — the root

Provides: `Zone`, `Factory`, `Player`, `TurnManager`, `Random`. Plus the three
sub-builders: `Player`, `Zone`, and factory methods `Spawn(...)`,
`PlaceObject(...)`, `Give(...)`.

Construction:
- `ScenarioContext.FromLiveGame()` — finds `GameBootstrap` via reflection, wires
  refs. Used by the runner and MCP callers.
- `ScenarioContext.FromTestHarness(Zone, EntityFactory, Entity player)` — for
  EditMode tests.

### `EntityBuilder` — returned by `ctx.Spawn("BlueprintName")`

**Positioning (pick one):**
- `.At(x, y)` — absolute cell
- `.AtPlayerOffset(dx, dy)` — relative to player
- `.NearPlayer(radius)` — random passable cell within Chebyshev radius
- `.AdjacentToPlayer()` — random passable adjacent cell
- `.InRing(radius, indexOf, outOf)` — nth of N points evenly spaced on a ring
  (for uniform multi-spawn)
- `.OnFirstPassableCell(predicate)` — first cell matching a predicate

If positioning fails (blocked cell), the builder **logs a warning and drops the
spawn** rather than crashing the scenario. Partial scenarios are better than no
scenarios.

**Entity modifications (chainable):**
- `.WithHp(fraction: 0.2f)` or `.WithHp(absolute: 10)`
- `.WithStat(name, value)` — any stat
- `.WithEquipment(blueprintName)` — spawn item, equip on body
- `.WithInventory(params string[] blueprintNames)` — add to inventory
- `.WithGoal(GoalHandler)` — push onto brain stack
- `.Passive(bool = true)` / `.Hostile(bool = true)` — flip Brain.Passive
- `.AsPersonalEnemyOf(entity)` — set PersonalEnemies relationship
- `.WithStartingCell(atSpawnPosition: true)` or `.WithStartingCell(x, y)`
- `.NotRegisteredForTurns()` — skip `TurnManager.AddEntity` (useful for inert
  test targets)
- `.Count(n)` — spawn N copies (each with identical config but different
  positions via InRing)

Returns: `Entity` on terminal methods for further manipulation (e.g.,
`var warden = ctx.Spawn("Warden").At(10, 10).Build();`).

### `PlayerBuilder` — accessed as `ctx.Player`

- `.Teleport(x, y)` / `.TeleportToZone(id)`
- `.SetHp(absolute: 50)` / `.SetHp(fraction: 0.5f)` / `.SetHp(max: true)`
- `.AddMutation(name, level: 1)` / `.RemoveMutation(name)`
- `.GiveItem(blueprintName, count: 1)`
- `.Equip(blueprintName)` — spawn + equip
- `.SetStat(name, value)`
- `.SetFactionFeeling(faction, value)` — quick reputation adjustment

### `ZoneBuilder` — accessed as `ctx.Zone`

- `.PlaceObject(blueprintName).At(x, y)` — furniture, items, non-creatures
- `.ApplyEffectToCell(Effect).At(x, y)` — fire, acid, etc. (Phase-D dependent)
- `.ClearCell(x, y)` — remove all non-terrain entities from a cell
- `.SetTimeOfDay(...)` — once Phase 12 Calendar lands

### Global utilities on `ScenarioContext`

- `ctx.Log(message)` — prints to Unity console + MessageLog with a `[Scenario]`
  prefix
- `ctx.AdvanceTurns(n)` — calls `TurnManager.Tick()` N times (for tests; not
  normally used in launch scenarios since player plays)
- `ctx.Rng` — deterministic RNG seeded per-scenario-run for reproducibility
- `ctx.Assert.*` — present only in `ScenarioContext.FromTestHarness` mode; null
  in launch mode

### Attribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ScenarioAttribute : Attribute
{
    public string Name;
    public string Category;   // menu grouping
    public string Description;
    public bool Hidden;        // skip from menu but discoverable via code
    public string[] Tags;      // for future filtering/search
}
```

---

## GameBootstrap integration

Minimal-invasive hook: one event, one line in `Start()`.

```csharp
// In GameBootstrap.cs, add a static event:
public static event System.Action<Zone, EntityFactory, Entity, TurnManager>
    OnAfterBootstrap;

// At the end of Start(), after RegisterCreaturesForTurns():
OnAfterBootstrap?.Invoke(_zone, _factory, _player, _turnManager);
```

`ScenarioRunner` subscribes to this event. When triggered:

```csharp
public static class ScenarioRunner
{
    public static Type PendingScenario;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        GameBootstrap.OnAfterBootstrap += ApplyPending;
    }

    static void ApplyPending(Zone zone, EntityFactory factory, Entity player, TurnManager tm)
    {
        if (PendingScenario == null) return;
        var instance = (IScenario)Activator.CreateInstance(PendingScenario);
        var ctx = new ScenarioContext(zone, factory, player, tm);
        try {
            instance.Apply(ctx);
            Debug.Log($"[Scenario] Applied: {PendingScenario.Name}");
        } catch (Exception e) {
            Debug.LogError($"[Scenario] {PendingScenario.Name} failed: {e.Message}");
        }
        PendingScenario = null; // single-use; re-set by menu click
    }
}
```

Editor menu item (or picker window — see open decisions below):
```csharp
[MenuItem("CavesOfOoo/Scenarios/Combat Stress/Five Snapjaw Ambush")]
static void Launch_FiveSnapjawAmbush()
{
    ScenarioRunner.PendingScenario = typeof(FiveSnapjawAmbush);
    EditorApplication.isPlaying = true; // enter play mode
}
```

These menu items are **generated automatically** via reflection — scan for
`[Scenario]` attribute, build a menu tree keyed by category.

---

## Directory layout

```
Assets/Scripts/Scenarios/
  Core/
    IScenario.cs                      ← interface: one Apply(ctx) method
    ScenarioAttribute.cs              ← [Scenario(name, category, ...)]
    ScenarioContext.cs                ← main context; exposes Spawn/Player/Zone
    PositionResolver.cs               ← .At, .AtPlayerOffset, .InRing, etc.
    CellFinder.cs                     ← predicates: passable, in room, adjacent
    ScenarioRunner.cs                 ← pending-scenario dispatcher
    ScenarioAssertions.cs             ← .Assert.* chain (test mode only)

  Builders/
    EntityBuilder.cs                  ← fluent creature spawning
    PlayerBuilder.cs                  ← fluent player modification
    ZoneBuilder.cs                    ← fluent world modification
    ItemBuilder.cs                    ← fluent item/object spawning

  Scenarios/
    README.md                         ← how to author a scenario
    Examples/
      FiveSnapjawAmbush.cs
      WoundedWarden.cs
      EmptyStartingZone.cs            ← "just the player, nothing else"
      FullyPopulatedVillage.cs
    Custom/
      (user scenarios land here)

Assets/Editor/Scenarios/
  ScenarioMenuBuilder.cs              ← auto-generates MenuItem entries
  ScenarioWindow.cs                   ← dockable browser (later)
```

Separating `Examples/` from `Custom/` lets you keep the example scenarios as
reference material while the user's own scenarios live in `Custom/` (and could
be gitignored or shared selectively).

---

## Implementation plan (ordered)

Each phase is independently usable — you get value after Phase 1, more after
Phase 2, and so on.

### Phase 1 — Core + minimal spawn ✅ SHIPPED (half day)

**Builds:**
- `IScenario` + `ScenarioAttribute`
- `ScenarioContext` with `Zone`/`Factory`/`Player`/`TurnManager` refs
- `ScenarioContext.FromLiveGame()` (reflection-based)
- `EntityBuilder` with `.At(x, y)`, `.AtPlayerOffset(dx, dy)` only
- `.WithHp(fraction)` modification
- `ScenarioRunner` singleton + `GameBootstrap.OnAfterBootstrap` event hook
- `Assets/Editor/Scenarios/ScenarioMenuBuilder.cs` — auto-menu

**Ships:**
- One example scenario: `FiveSnapjawAmbush`

**Outcome:** click `CavesOfOoo > Scenarios > Combat Stress > Five Snapjaw
Ambush` → Play mode enters → 5 snapjaws surround the player → you playtest.

### Phase 2 — Richer positioning + modifications ✅ SHIPPED (half day)

**Builds:**
- `.NearPlayer`, `.AdjacentToPlayer`, `.InRing`, `.OnFirstPassableCell`
- `EntityBuilder`: `.WithStat`, `.WithEquipment`, `.WithInventory`, `.WithGoal`,
  `.Passive`, `.AsPersonalEnemyOf`, `.WithStartingCell`, `.NotRegisteredForTurns`
- `PlayerBuilder`: `.Teleport`, `.SetHp`, `.AddMutation`, `.GiveItem`, `.Equip`,
  `.SetStat`
- `ZoneBuilder`: `.PlaceObject(...).At(...)`, `.ClearCell`

**Ships:**
- 3 more example scenarios: `WoundedWarden`, `CalmTestSetup`, `MimicSurprise`
- `README.md` authoring guide

**Outcome:** expressive enough to set up 90% of testing scenarios you'd want
for Phase 6 / 7 work.

### Phase 3 — Test harness integration ✅ SHIPPED

**Status:** Complete as of 2026-04-18 across commits `a463990` (3a),
`d82e674` (3b), `061bd39` (3c), `4404df7` (3d), `08c1f90` (3d follow-up),
plus review follow-ups.

**Shipped:**
- **3a — `ScenarioTestHarness`** (test-assembly only, zero runtime code):
  fixture-scope factory that encapsulates FactionManager init, blueprint load,
  stub/real-player construction, and `ScenarioContext` creation per test.
- **3b — `ctx.AdvanceTurns(n)`** extension (test-assembly only) + one runtime
  addition: `TurnManager.Entities` yield-based accessor. Simple tick semantics
  — fires `TakeTurn` once per registered entity per advance-step.
- **3c — `ctx.Verify()` fluent assertion DSL** (test-assembly only): root
  `ScenarioVerifier` with global assertions (`EntityCount`, `PlayerIsAlive`,
  `TurnCount`) + three sub-verifiers (`Entity(e)`, `Player()`, `Cell(x, y)`)
  with ~20 chained assertion methods. NUnit-native failures with readable
  messages.
- **3d — ported `AIBehaviorPartTests`** (13 tests) to the full stack. 385 → 310
  lines (-19%). Real blueprints where possible, special-case helpers kept for
  tests that genuinely can't use `ctx.Spawn`.

**What was deferred (originally planned but cut):**
- `RunAsTest()` decorator magic — punted; cleverness-to-payoff ratio not
  worth it. Tests just use the normal NUnit `[Test]` attribute with the
  harness in `[OneTimeSetUp]`.
- PlayMode test harness — different runner, different concerns. Can be its
  own phase if needed later.

**Acceptance criteria (met):**
- [x] All existing tests still pass (1445 / 1445)
- [x] `ScenarioTestHarness` in test assembly, zero runtime deps except
  `TurnManager.Entities` accessor (yield-based, no TurnEntry leak)
- [x] `ctx.Verify()` chain produces NUnit-native failure messages
- [x] 1 test file ported with measurable line reduction (AIBehaviorPartTests,
  385 → 310). Plan targeted 30-40%; actual 19% — honest report: the raw
  delta undersells the shift because per-test bodies halved (from ~10 to
  ~5 lines) while the deleted lines were mostly helpers.
- [x] README has a "Reusing scenarios as tests" section with full
  before/after walkthrough + verifier reference table

**Files shipped:**
```
Assets/Tests/EditMode/TestSupport/
├── ScenarioTestHarness.cs                  (3a)
├── ScenarioTestHarnessTests.cs             (3a self-tests)
├── ScenarioContextExtensions.cs            (3b AdvanceTurns)
├── ScenarioContextExtensionsTests.cs       (3b self-tests)
├── ScenarioVerifier.cs                     (3c root + .Verify() extension)
├── EntityVerifier.cs                       (3c)
├── PlayerVerifier.cs                       (3c)
├── CellVerifier.cs                         (3c)
└── VerifierTests.cs                        (3c self-tests, 48 tests)

Assets/Scripts/Gameplay/Turns/TurnManager.cs  (one new accessor: `Entities`)
```

### Phase 4 — Editor window (half day, optional)

**Builds:**
- `ScenarioWindow` — dockable Unity editor window
- Searchable list of all scenarios with category grouping
- Per-scenario "Launch" button + "Re-launch last" button
- Favorites (starred scenarios pinned to top)

**Outcome:** better UX than scrolling through menus when you have 30+ scenarios.

### Phase 5 — Polish + advanced features (as needed)

- `.ApplyEffectToCell` (once you want fire/acid tests)
- Deterministic seeding for reproducible scenarios
- "Scenario from snapshot" — save current world state as a starting scenario
- Parameterized scenarios with editor-visible inputs (sliders for "how many
  snapjaws?")

---

## Design decisions

1. **Scenario file location.** `Assets/Scripts/Scenarios/Custom/` — shipped with
   project, gitignored, or both? Author leans **shipped** so scenarios are
   durable test artifacts alongside unit tests. *Still to confirm.*

2. **Launch from Play mode already running.** Currently the design is: set
   pending scenario → enter Play mode. But what if Play is already running?
   Options:
   - (a) Scenario only runs on fresh Play mode entry — requires restart
   - (b) Allow mid-Play application — scenario applies to the current zone
     without restarting GameBootstrap
   - Preferred: **(b) for simple scenarios, (a) for complex.** Mid-play is
     usually fine for spawn-only scenarios; for scenarios that teleport player
     to a different zone, a restart is cleaner. *Still to confirm.*

3. **Auto-menu vs explicit menu items.** ✅ **Decided: Option A — one menu item
   per scenario.** See "Launch UX" section below.

4. **Scenario failure behavior.** If `Apply` throws partway through, what
   happens? Options:
   - Abort: remaining spawns don't happen, Play mode continues with partial
     setup
   - Transactional: roll back all spawns, enter Play mode as if nothing
     happened
   - Preferred: **abort + log clearly.** Rollback is complex (need to track
     every mutation) and rarely what you want during dev.

5. **Coordinate system for `.At(x, y)`.** Absolute zone cells? Relative to
   player? Both? Preferred: **both — `.At(x, y)` for absolute, `.AtPlayerOffset
   (dx, dy)` for relative, and document which is which.** Current zone is
   80x25 so absolute coords are bounded and memorable.

---

## Launch UX — Option A (decided)

**One menu item per scenario**, authored in a single centralized stubs file.
Click menu → Play mode enters → scenario applied → play manually.

### Authoring flow

A new scenario requires two edits:

1. Write the scenario class in `Assets/Scripts/Scenarios/Custom/`:

```csharp
[Scenario("Five Snapjaw Ambush", category: "Combat Stress")]
public class FiveSnapjawAmbush : IScenario
{
    public void Apply(ScenarioContext ctx) { /* ... */ }
}
```

2. Add one line to the central menu stubs file at
   `Assets/Editor/Scenarios/ScenarioMenuItems.cs`:

```csharp
[MenuItem("CavesOfOoo/Scenarios/Combat Stress/Five Snapjaw Ambush")]
static void Launch_FiveSnapjawAmbush()
    => ScenarioRunner.Launch<FiveSnapjawAmbush>();
```

Single Unity recompile — menu appears immediately.

### Why centralized stubs (not per-scenario stub files)

- **One file to audit** — open `ScenarioMenuItems.cs` to see every scenario that
  exists
- **No codegen** — all menu entries are hand-written, no generation pipeline to
  maintain
- **Fast cycle** — adding a scenario is a single-compile change (not the
  two-compile codegen pattern)
- **Refactor-safe** — IDE renames across the runtime class and stub
  automatically

### Permanent accelerator menu items

Three always-present entries at the top of the Scenarios menu:

```csharp
[MenuItem("CavesOfOoo/Scenarios/↻ Re-run Last Scenario %#r")]  // Cmd+Shift+R
[MenuItem("CavesOfOoo/Scenarios/⏹ Stop Play Mode %#.")]         // Cmd+Shift+.
[MenuItem("CavesOfOoo/Scenarios/─────────────", priority = 100)]  // separator
```

The `Re-run Last` shortcut makes the iteration loop trivial:
edit scenario → Unity recompiles → `Cmd+Shift+R` → Play enters with last
scenario applied. No menu navigation between iterations.

Individual scenarios can also take per-scenario shortcuts (`%#1` through `%#9`)
for the 5-10 most-frequent-to-launch ones.

### Scaling beyond ~30 scenarios

If the centralized stubs file becomes unwieldy at scale, a future
`Tools > Scenarios > Regenerate Menu` command can scan `[Scenario]` attributes
and codegen the stubs file. **Not built upfront** — wait until the friction is
real. The hand-written approach is fine up to at least 50 scenarios.

---

## Why this plan beats the alternatives

**vs. current MCP execute_code workflow:**
- Scenarios are version-controlled `.cs` files instead of ephemeral tool calls
- One-click launch instead of writing 40 lines of reflection + wiring every time
- Works even when MCP isn't connected

**vs. NUnit PlayMode tests alone:**
- Manual playtest mode — you actually PLAY the scenario, not just assert state
- Faster iteration (click-to-launch vs test-runner-setup)
- Same helpers so tests can reuse scenarios

**vs. full DSL port from sim:**
- ~2 days of work instead of ~2 weeks
- Zero runtime overhead (no VM)
- Type-safe, refactor-safe, IDE-navigable
- No new language to maintain

**vs. do nothing:**
- Unblocks rapid iteration on AI/combat/balance work for every future milestone
- Pays for itself in M2 alone (calm-mutation testing, witness-effect tuning
  both benefit hugely)

---

# Phase 2 Detailed Plan

**Status:** Designed, not yet implemented.

**Goal of Phase 2:** Take the library from "I can spawn a creature at (x,y)" to
"I can describe any engineered game situation in ~10-30 lines of C#."

After Phase 2, a scenario author can: spawn via 6 positioning primitives,
modify spawned entities with 8+ chainable methods, configure the player's
stats/mutations/inventory/reputation, and place/clear world objects.

## Sub-phase breakdown

Phase 2 splits into four commit-sized chunks. Each is independently useful
and leaves the library in a compiling, test-green state.

| Sub-phase | Scope | Effort | What unblocks |
|-----------|-------|:------:|---------------|
| **2a** | EntityBuilder: richer positioning | ~1 hr | `.NearPlayer`, `.AdjacentToPlayer`, `.InRing(N, of, total)`, `.OnFirstPassableCell(pred)` |
| **2b** | EntityBuilder: modification expansion | ~1.5 hr | `.WithStat`, `.WithEquipment`, `.WithInventory`, `.WithGoal`, `.Passive`, `.Hostile`, `.AsPersonalEnemyOf`, `.WithStartingCell` |
| **2c** | PlayerBuilder (new class) | ~1 hr | `.Teleport`, `.SetHp`, `.AddMutation`, `.GiveItem`, `.Equip`, `.SetStat`, `.SetFactionReputation` |
| **2d** | ZoneBuilder (new class) | ~1 hr | `.PlaceObject(bp).At(x,y)/.AtPlayerOffset(dx,dy)`, `.ClearCell`, `.RemoveEntitiesWithTag` |
| **2e** | Example scenarios + authoring README | ~1 hr | 3 new reference scenarios + developer docs |

Total Phase 2 effort: ~half a day including tests and commits.

---

## Sub-phase 2a — EntityBuilder richer positioning

**File:** `Assets/Scripts/Scenarios/Builders/EntityBuilder.cs`

### New positioning terminals

```csharp
/// <summary>
/// Spawn at a random passable cell within Chebyshev distance [minRadius, maxRadius]
/// from the player. All candidate cells are collected and one is selected via
/// ctx.Rng — deterministic given scenario seed.
/// Returns null if no passable cell exists in the range.
/// </summary>
public Entity NearPlayer(int minRadius = 1, int maxRadius = 8)

/// <summary>
/// Convenience: NearPlayer(1, 1). Picks one of the (up to) 8 adjacent cells
/// that's passable. Returns null if fully surrounded.
/// </summary>
public Entity AdjacentToPlayer()

/// <summary>
/// Deterministic ring-position spawn. Given <c>count = N</c> total spawns and
/// this being the <c>indexOf = i</c>-th, computes a grid cell approximating
/// (playerX + radius·cos(2πi/N), playerY + radius·sin(2πi/N)).
///
/// Use in a loop: for (int i = 0; i &lt; 8; i++) ctx.Spawn("Snapjaw").InRing(3, i, 8);
/// </summary>
public Entity InRing(int radius, int indexOf, int totalOfN)

/// <summary>
/// Scan the zone for the first passable cell matching the predicate, spawn there.
/// Scan order is row-major (x=0..79 for each y=0..24). Use for conditional
/// placement like "spawn in any room cell tagged FurnitureAdjacent".
/// </summary>
public Entity OnFirstPassableCell(Func<Cell, bool> predicate)
```

### Implementation notes

- All new terminals route through the existing `SpawnAt(x, y)` private method,
  so the BrainPart wiring and HP-fraction application logic stays centralized.
- `NearPlayer` should collect cells once, filter, then sample — not retry-sample.
  A fully-walled-in player gets a clean `return null + warning` rather than an
  infinite loop.
- `InRing` rounds to int; adjacent ring indices may land on the same cell for
  small radii. Document this — callers with fine-grained needs should use
  explicit `.AtPlayerOffset(dx, dy)` per spawn.

### Tests

- Unit test in `Assets/Tests/EditMode/Gameplay/Scenarios/PositionResolverTests.cs`:
  - `NearPlayer_ReturnsCellWithinRadius_WhenAvailable`
  - `NearPlayer_ReturnsNull_WhenFullyWalled`
  - `InRing_DistributesEvenly_AtLargeRadius` (asserts 8 spawns at radius 10 give
    8 distinct cells)
  - `InRing_ClustersExpected_AtSmallRadius` (documents the known-rounding behavior)
  - `OnFirstPassableCell_FindsFirstMatch_InRowMajorOrder`

---

## Sub-phase 2b — EntityBuilder modification expansion

**File:** same — `EntityBuilder.cs`.

### New chainable modifications

Each method returns `this` for continued chaining. All applied at well-defined
stages of the spawn pipeline (pre-placement, post-placement, post-brain-wire —
marked per method).

```csharp
/// <summary>Set a stat's BaseValue. If the stat doesn't exist, logs a warning
/// and skips. For stats with high target values (e.g., HP above 30), also call
/// <see cref="WithStatMax"/> — Stat.Max defaults to 30 and silently clamps.</summary>
/// <remarks>Applied post-spawn, before BrainPart wiring.</remarks>
public EntityBuilder WithStat(string statName, int value)

/// <summary>Raise a stat's Max ceiling so WithStat can set higher values.</summary>
public EntityBuilder WithStatMax(string statName, int max)

/// <summary>Spawn an item blueprint, add to inventory, and equip in the first
/// matching body slot. Entity must have InventoryPart or a Body with slots.
/// Logs warning + skips on missing inventory or equip failure.</summary>
/// <remarks>Applied post-placement.</remarks>
public EntityBuilder WithEquipment(string itemBlueprintName)

/// <summary>Spawn and add item blueprints to inventory (no equip). Stackable
/// items auto-merge via StackerPart.</summary>
public EntityBuilder WithInventory(params string[] itemBlueprintNames)

/// <summary>Push a goal onto the spawned entity's brain stack. Goal is
/// constructed by caller — library doesn't know game-specific goal types.</summary>
/// <remarks>Applied post-brain-wire. Throws if entity has no BrainPart.</remarks>
public EntityBuilder WithGoal(GoalHandler goal)

/// <summary>Set Brain.Passive. Default true; pass false to explicitly un-passivate
/// blueprints that were Passive by default.</summary>
public EntityBuilder Passive(bool enabled = true)

/// <summary>Alias for Passive(false) — explicit semantic.</summary>
public EntityBuilder Hostile()

/// <summary>Make this entity personally hostile toward target, bypassing faction.
/// One-way (source → target); in practice mutual because GetFeeling checks both.</summary>
public EntityBuilder AsPersonalEnemyOf(Entity target)

/// <summary>Override the auto-set starting cell (which defaults to the spawn
/// cell). Useful when you want the creature to "know home is elsewhere" from
/// the moment it spawns.</summary>
public EntityBuilder WithStartingCell(int x, int y)
```

### Pipeline ordering

```
CreateEntity(blueprint)
  ↓
Apply field-level mods: WithStat, WithStatMax, Passive, Hostile
  ↓
Wire BrainPart (CurrentZone, Rng, StartingCell default)
  ↓
Override: WithStartingCell (if set)
  ↓
zone.AddEntity(entity, x, y)
  ↓
Post-placement mods: WithHp, WithEquipment, WithInventory, WithGoal, AsPersonalEnemyOf
  ↓
TurnManager.AddEntity (if registered)
```

### Tests

- Expanded `EntityBuilderSmokeTests.cs` via MCP execute_code at commit time:
  one live scenario that chains 6+ modifications and asserts end state.

---

## Sub-phase 2c — PlayerBuilder

**File:** `Assets/Scripts/Scenarios/Builders/PlayerBuilder.cs` (new)

**Access:** `ctx.Player` — returns a shared `PlayerBuilder` instance wrapping
the context. Methods return `this` for chaining.

### API

```csharp
public sealed class PlayerBuilder
{
    private readonly ScenarioContext _ctx;
    internal PlayerBuilder(ScenarioContext ctx) { _ctx = ctx; }

    /// <summary>Move the player to (x, y) in the current zone. Uses zone.AddEntity
    /// semantics (bypasses BeforeMove/AfterMove events — a teleport, not a walk).</summary>
    public PlayerBuilder Teleport(int x, int y);

    /// <summary>Set player HP. Only ONE of absolute/fraction/max should be used.</summary>
    public PlayerBuilder SetHp(int? absolute = null, float? fraction = null, bool max = false);

    /// <summary>Set an arbitrary stat. See EntityBuilder.WithStat caveats.</summary>
    public PlayerBuilder SetStat(string statName, int value);

    /// <summary>Grant a mutation. Class name must match the MutationsPart
    /// reflection lookup (e.g., "FireBoltMutation", not "FireBolt").</summary>
    public PlayerBuilder AddMutation(string mutationClassName, int level = 1);

    /// <summary>Spawn a fresh item and add to inventory. Non-stackable: spawns
    /// `count` separate items. Stackable: relies on StackerPart auto-merge.</summary>
    public PlayerBuilder GiveItem(string itemBlueprintName, int count = 1);

    /// <summary>Shortcut: GiveItem + equip in first compatible slot.</summary>
    public PlayerBuilder Equip(string itemBlueprintName);

    /// <summary>Set player reputation with a named faction. Clamped to [-200, 200]
    /// by PlayerReputation. Silent (no in-game message log entry).</summary>
    public PlayerBuilder SetFactionReputation(string faction, int value);

    /// <summary>Adjust player reputation by delta. Silent.</summary>
    public PlayerBuilder ModifyFactionReputation(string faction, int delta);
}
```

### Implementation notes

- `Teleport` uses `zone.AddEntity(player, x, y)` — the confirmed idiomatic pattern
  for re-placing an already-placed entity. This is intentional: we want teleport
  semantics (no move-blockers), not "walk there" semantics.
- `SetHp` validates exactly one of its three modes is set; overloads would be
  cleaner but the parameterized form is explicit at the call site.
- `AddMutation` uses `MutationsPart.AddMutation(className, level)` — the same
  code path as blueprint-driven `StartingMutations` parsing.
- `GiveItem` for unknown blueprints logs a warning and no-ops (same pattern as
  EntityBuilder spawn failures).
- `Equip` is `GiveItem + InventorySystem.Equip` — one call does both.
- Faction reputation methods go through `PlayerReputation.Set` / `Modify` with
  `silent: true`, so the setup doesn't pollute MessageLog with noise.

### Also required

- Extend `ScenarioContext` with a `public PlayerBuilder Player { get; }` property
  (lazy-initialized). Small change to `ScenarioContext.cs`.

---

## Sub-phase 2d — ZoneBuilder

**File:** `Assets/Scripts/Scenarios/Builders/ZoneBuilder.cs` (new)

**Access:** `ctx.World` — not `ctx.Zone` (that name is taken by the raw Zone
reference). `World` reads more naturally in fluent contexts ("place a chest in
the world at...").

### API

```csharp
public sealed class ZoneBuilder
{
    private readonly ScenarioContext _ctx;
    internal ZoneBuilder(ScenarioContext ctx) { _ctx = ctx; }

    /// <summary>Begin a fluent placement chain for a non-creature object
    /// (furniture, item, decor). Returns an ObjectPlacer that needs a
    /// positioning terminal (.At or .AtPlayerOffset).</summary>
    public ObjectPlacer PlaceObject(string blueprintName);

    /// <summary>Remove all non-terrain entities from the cell. "Terrain" means
    /// entities with the "Wall" or "Floor" tag (or other terrain-ish tags to be
    /// confirmed). Useful for making a cell available to a subsequent spawn.</summary>
    public ZoneBuilder ClearCell(int x, int y);

    /// <summary>Remove every entity in the zone carrying the given tag. Common
    /// uses: RemoveEntitiesWithTag("Creature") to empty the zone of NPCs,
    /// RemoveEntitiesWithTag("Snapjaws") via the Faction tag.</summary>
    public ZoneBuilder RemoveEntitiesWithTag(string tagName);
}

public sealed class ObjectPlacer
{
    // Positioning terminals — same shape as EntityBuilder but the result is
    // always non-registered-for-turns.
    public Entity At(int x, int y);
    public Entity AtPlayerOffset(int dx, int dy);
}
```

### Implementation notes

- `PlaceObject` is essentially `Spawn(...).NotRegisteredForTurns()` but with a
  narrower return type. Internally it can share code with EntityBuilder — the
  "ObjectPlacer" wrapper just omits the creature-only modifiers.
- `ClearCell` iterates `cell.Objects` in reverse and calls `zone.RemoveEntity`
  on anything that lacks the terrain guard tags. Needs one-time verification
  during implementation — current Zone/Cell API may expose a cleaner helper.
- `RemoveEntitiesWithTag("Creature")` is very useful for test-setup scenarios
  that want to start from a clean world ("empty zone" baseline).

### Also required

- Extend `ScenarioContext` with `public ZoneBuilder World { get; }` lazy prop.

---

## Sub-phase 2e — Example scenarios + README

**New files under `Assets/Scripts/Scenarios/Custom/`:**

### 1. WoundedWarden.cs

```csharp
[Scenario(name: "Wounded Warden",
    category: "AI Behavior",
    description: "Warden at 20% HP out of sight — watch her retreat to guard post.")]
public class WoundedWarden : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        ctx.Spawn("Warden")
           .AtPlayerOffset(8, 0)
           .WithHp(fraction: 0.20f)
           .WithStartingCell(
               ctx.Zone.GetEntityPosition(ctx.Player).x + 8,
               ctx.Zone.GetEntityPosition(ctx.Player).y);

        ctx.Log("Warden spawned at 20% HP — observe RetreatGoal flow.");
    }
}
```

**Also needs a menu stub added to `ScenarioMenuItems.cs`:**
```csharp
[MenuItem("Caves Of Ooo/Scenarios/AI Behavior/Wounded Warden")]
private static void Launch_WoundedWarden() => ScenarioRunner.Launch<WoundedWarden>();
```

### 2. MimicSurprise.cs

```csharp
[Scenario(name: "Mimic Surprise",
    category: "Content Demo",
    description: "Real chest and a mimic next to each other. Attack or step on the wrong one.")]
public class MimicSurprise : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        // Real chest (decoy)
        ctx.World.PlaceObject("Chest").AtPlayerOffset(3, 0);

        // Mimic disguised identically — walk onto it wakes it
        ctx.Spawn("MimicChest").AtPlayerOffset(5, 0);

        // Gold pile as lure
        ctx.World.PlaceObject("GoldPile").AtPlayerOffset(3, 0);

        ctx.Log("Mimic Surprise applied. One of the chests is lying to you.");
    }
}
```

Menu stub: `Caves Of Ooo/Scenarios/Content Demo/Mimic Surprise`.

### 3. EmptyStartingZone.cs

```csharp
[Scenario(name: "Empty Starting Zone",
    category: "Baseline",
    description: "Strip all creatures from the current zone, leaving only the player.")]
public class EmptyStartingZone : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        ctx.World.RemoveEntitiesWithTag("Creature");
        // Re-add the player (we remove then re-add to guarantee the player's
        // position/registration isn't affected by the mass-remove).
        ctx.Zone.AddEntity(ctx.Player, ctx.Zone.GetEntityPosition(ctx.Player));
        ctx.Log("Zone emptied. Baseline for controlled tests.");
    }
}
```

Menu stub: `Caves Of Ooo/Scenarios/Baseline/Empty Starting Zone`.

### (Phase-2-ready, activates when M2 ships)

**CalmTestSetup.cs** — references `CalmMutation` which doesn't exist yet. Logs a
warning on launch until M2 lands, then works automatically.

```csharp
[Scenario(name: "Calm Test Setup",
    category: "Mutations",
    description: "Player with Calm mutation + 3 hostiles to target. Activates after M2.2.")]
public class CalmTestSetup : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        ctx.Player.AddMutation("CalmMutation", level: 3);

        ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
        ctx.Spawn("Snapjaw").AtPlayerOffset(5, 0);
        ctx.Spawn("Snapjaw").AtPlayerOffset(7, 0);

        ctx.Log("Calm Test ready — cast Calm on one snapjaw, fight the rest.");
    }
}
```

### README.md

**File:** `Assets/Scripts/Scenarios/README.md`

**Structure:**
- **What this is** — 2 paragraphs on the scenario library's purpose
- **Your first scenario** — walkthrough: create .cs, add menu stub, click, play
- **Common patterns**
  - Positioning: `.At` vs `.AtPlayerOffset` vs `.NearPlayer` vs `.InRing`
  - Modifying spawned entities: stats, equipment, inventory, goals
  - Setting up the player: mutations, reputation, teleport
  - Clearing the world: `RemoveEntitiesWithTag` for clean-slate tests
- **Limitations**
  - No hot-reload: edit requires Unity recompile
  - No cross-zone scenarios in Phase 2 (Gap A)
  - No cell-level effects in Phase 2 (Phase 5)
- **Troubleshooting**
  - Scenario doesn't appear in menu → check `ScenarioMenuItems.cs` has the stub
  - Spawn silently dropped → check console for passable-cell warning
  - HP silently clamped → stat.Max defaults to 30, use `.WithStatMax` to raise
  - Mutation not granted → class name must match Type.Name exactly
- **Reference table** — every builder method in one table with one-line description

---

## Phase 2 acceptance criteria ✅ MET

- [x] All existing tests still pass (1445 / 1445 after Phase 3)
- [x] 4 new `PositionResolverTests` pass (2a unit tests)
- [x] Each sub-phase compiles cleanly on commit (`mcp__unity__refresh_unity` + `read_console`)
- [x] MCP smoke tests for each new builder method produce the expected state
- [x] 3+ example scenarios exist and launch cleanly from the menu
- [x] `README.md` present and cross-referenced from `SCENARIO_SCRIPTING.md`
- [x] `CalmTestSetup` ships with a clear "activates after M2.2" note

## What Phase 2 explicitly does NOT include

Deferred to later phases to keep Phase 2 tight and shippable:

- **Automated test-harness reuse** — ✅ shipped in Phase 3 (`ScenarioTestHarness`,
  `ctx.AdvanceTurns(n)`, `ctx.Verify()` fluent DSL, ported `AIBehaviorPartTests`)
- **Cell-level effect placement** (Phase 5 — `ZoneBuilder.ApplyEffectToCell`)
- **Mid-play scenario application** (post-Phase 3 — apply to already-running
  Play session instead of requiring restart)
- **Parameterized scenarios** (Phase 5 — editor-visible inputs like
  `[ScenarioParam] int SnapjawCount = 5`)
- **Editor browser window** (Phase 4 — deferred indefinitely per Option A
  decision; 20-30 scenario scope doesn't warrant it)

## Known risks / decisions that may shift

1. **`RemoveEntitiesWithTag("Creature")`** may not cleanly remove NPCs that are
   mid-goal. Safe in blueprint-load ordering (before any turn has ticked) but
   testing needed. If it breaks, fallback: iterate entities and check
   `brain.CurrentZone = null` before removal.

2. **`MimicSurprise` uses `GoldPile`** — need to confirm that blueprint exists
   in `Objects.json`. If not, swap for any Item-type blueprint.

3. **Stat.Max = 30 default** could surprise scenario authors setting HP to 50+
   on spawned creatures. The `WithStatMax` method is the workaround but needs
   a prominent README callout.

4. **`InRing` rounding** clusters spawns at small radii. Acceptable limitation
   but README should guide authors toward `.NearPlayer` for random-ring or
   explicit `.AtPlayerOffset` chains for deterministic placement at r < 3.

## Implementation order recommendation

**Ship 2a and 2b first** (roughly 2.5 hours combined). These are the
most-used primitives and unblock the example scenarios. PlayerBuilder (2c)
and ZoneBuilder (2d) can each ship independently afterward — neither depends
on the other. 2e is last, consuming all prior work.

Recommended commits:
1. `feat(scenarios): Phase 2a — richer positioning primitives` (NearPlayer,
   AdjacentToPlayer, InRing, OnFirstPassableCell + 4 unit tests)
2. `feat(scenarios): Phase 2b — entity modification expansion` (WithStat,
   WithEquipment, WithInventory, WithGoal, Passive/Hostile, AsPersonalEnemyOf,
   WithStartingCell)
3. `feat(scenarios): Phase 2c — PlayerBuilder` (Teleport, SetHp, AddMutation,
   GiveItem, Equip, SetStat, SetFactionReputation)
4. `feat(scenarios): Phase 2d — ZoneBuilder` (PlaceObject, ClearCell,
   RemoveEntitiesWithTag)
5. `feat(scenarios): Phase 2e — example scenarios + authoring README`

After all five commits: push the branch and declare Phase 2 done.
