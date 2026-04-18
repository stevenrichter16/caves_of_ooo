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

### Phase 1 — Core + minimal spawn (half day)

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

### Phase 2 — Richer positioning + modifications (half day)

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

### Phase 3 — Test harness integration (half day)

**Builds:**
- `ScenarioContext.FromTestHarness(...)` for EditMode/NUnit
- `ScenarioAssertions` — `.Assert.Entity(name).HasGoal<T>()`, `.HasNoGoal<T>()`,
  `.IsAtCell(...)`, `.HasHpBelow(fraction)`
- `ctx.AdvanceTurns(n)` via `TurnManager.Tick()`

**Ships:**
- A `RunAsTest()` extension that wraps a scenario in a PlayMode test harness
- Port 2 existing M1 tests to use the new library (demonstrates reuse)

**Outcome:** the library works for automated CI tests AND manual playtest
launch — no duplicated infrastructure.

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
