# Scenario Scripting — Authoring Guide

A scenario is a one-shot C# script that stages the game into a specific
engineered situation — spawning monsters, placing furniture, loading up the
player, etc. — so you can click a menu item and immediately play-test that
situation without walking around the world hunting for the right conditions.

This README is the practical authoring guide. For the library's design
rationale and long-range roadmap, see `Docs/SCENARIO_SCRIPTING.md`.

---

## Your first scenario

1. **Create a new `.cs` file** under `Assets/Scripts/Scenarios/Custom/`:

   ```csharp
   namespace CavesOfOoo.Scenarios.Custom
   {
       [Scenario(
           name: "My Scenario",
           category: "Combat Stress",
           description: "What this scenario stages.")]
       public class MyScenario : IScenario
       {
           public void Apply(ScenarioContext ctx)
           {
               ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
               ctx.Log("My Scenario applied.");
           }
       }
   }
   ```

2. **Register it** in `Assets/Editor/Scenarios/ScenarioMenuItems.cs`:

   ```csharp
   [MenuItem("Caves Of Ooo/Scenarios/Combat Stress/My Scenario")]
   private static void Launch_MyScenario() => ScenarioRunner.Launch<MyScenario>();
   ```

3. **Click it.** Unity compiles, enters Play mode, and your scenario runs
   inside `GameBootstrap.OnAfterBootstrap`. The `Re-run Last Scenario` item
   (`Cmd+Shift+R`) re-launches whatever you last ran.

---

## Common patterns

### Positioning

| Call | Behavior |
|------|----------|
| `.At(x, y)` | Absolute zone cell. Best for "place chest at known lab coords." |
| `.AtPlayerOffset(dx, dy)` | Player-relative. Works regardless of player spawn cell. |
| `.NearPlayer(min, max)` | Random passable cell in a Chebyshev band around the player. |
| `.AdjacentToPlayer()` | Alias for `NearPlayer(1, 1)`. |
| `.InRing(radius, i, n)` | Position `i` of `n` around a ring. Great for symmetric ambushes. |
| `.OnFirstPassableCell(pred)` | Scans cells row-major, takes first match. For pinning to terrain features. |

### Modifying spawned entities (EntityBuilder)

```csharp
ctx.Spawn("Snapjaw")
   .WithStatMax("Hitpoints", 100).WithStat("Hitpoints", 100)
   .WithStatMax("Strength", 30).WithStat("Strength", 28)
   .WithEquipment("LongSword")
   .WithInventory("HealingTonic", "HealingTonic")
   .WithStartingCell(x, y)   // pin AIGuard / BoredGoal return-point
   .AsPersonalEnemyOf(ctx.PlayerEntity)
   .Hostile()                 // or .Passive()
   .WithGoal(new KillGoal(ctx.PlayerEntity))
   .AtPlayerOffset(5, 0);
```

Modifiers chain in any order; the spawn happens at the positioning terminal
(`.At`, `.AtPlayerOffset`, `.NearPlayer`, etc.). Stats are clamped by their
`Max`, so raise `Max` first if you're setting a big value.

### Setting up the player (PlayerBuilder)

```csharp
ctx.Player
   .Teleport(50, 20)
   .SetHpFraction(0.5f)                  // or .SetHp(100), .SetHpMax()
   .SetStatMax("Strength", 100)
   .SetStat("Strength", 80)
   .AddMutation("FireBoltMutation", 5)   // level defaults to 3
   .GiveItem("HealingTonic", 5)
   .Equip("ShortSword")
   .ClearInventory()                     // carried items only
   .SetFactionReputation("Villagers", 100)
   .ModifyFactionReputation("Snapjaws", -50);
```

Every method is fluent (returns `this`) and applies immediately. Use
`ctx.PlayerEntity` when you need the raw Entity (e.g., for
`AsPersonalEnemyOf(ctx.PlayerEntity)`).

### Placing non-creature objects & clearing the world (ZoneBuilder)

```csharp
ctx.World.PlaceObject("Chest").At(20, 10);
ctx.World.PlaceObject("HealingTonic").AtPlayerOffset(2, 0);
ctx.World.ClearCell(25, 12);               // skip terrain + player
ctx.World.RemoveEntitiesWithTag("Creature"); // baseline-empty zone, player preserved
```

`PlaceObject` never registers the result for turns — use `ctx.Spawn` for
creatures. Both `ClearCell` and `RemoveEntitiesWithTag` unconditionally
preserve `ctx.PlayerEntity` and anything tagged `Wall`, `Floor`, or `Terrain`.

`RemoveEntitiesWithTag` matches tag **keys only**, not values. Snapjaw's
blueprint has `{ Key: "Faction", Value: "Snapjaws" }` — so passing `"Faction"`
removes every faction-bearing entity, but `"Snapjaws"` matches nothing. For
per-faction removal, add a unique key (e.g. `entity.SetTag("EnemyTeam")`) at
spawn time and remove by that key.

---

## Limitations

- **No hot-reload.** Editing a scenario requires a Unity recompile
  (automatic when you save), followed by re-clicking the menu item.
- **No cross-zone scenarios.** Phase 2 stages into the current zone only.
  Multi-zone setups await Gap A in the roadmap.
- **No cell-level effects.** Can't `PlaceOilSlick(x, y)` yet — deferred to
  Phase 5 when `ZoneBuilder.ApplyEffectToCell` ships.
- **No parameterized scenarios.** Every scenario is a fixed setup — no
  `[ScenarioParam] int SnapjawCount = 5` yet. Phase 5 target.
- **Restart required.** Scenarios apply on `GameBootstrap.OnAfterBootstrap`
  — can't be applied mid-session. Exit play, click, re-enter.

---

## Troubleshooting

- **Scenario doesn't appear in the menu** → Check `ScenarioMenuItems.cs` has
  the `[MenuItem]` stub for your scenario, and the path string is correct.
  Unity's `[MenuItem]` is compile-time; the `[Scenario]` attribute is
  discoverable metadata, not a menu registration.
- **Spawn silently dropped** → Check the console for passable-cell warnings.
  The builder fail-softs on out-of-bounds, non-passable cells, and unknown
  blueprints.
- **HP silently clamped to 30** → Stat `Max` defaults to 30 for most stats.
  Call `.WithStatMax("Hitpoints", 100)` before `.WithStat("Hitpoints", 100)`.
- **Mutation not granted** → `AddMutation` takes the class name (e.g.
  `"FireBoltMutation"`), not the blueprint name. The lookup is via
  `Type.Name` reflection.
- **Mutation level is lower than I asked for** → The player-level cap
  (`Level/2 + 1`) applies to `BaseMutation.Level`. `BaseLevel` holds your
  requested level; `Level` is the in-game effective level after the cap.

---

## Full API reference

### `ScenarioContext`

| Member | Description |
|--------|-------------|
| `Zone` | The live `Zone`. For raw position queries. |
| `Factory` | The live `EntityFactory`. Rarely needed directly. |
| `PlayerEntity` | The raw player `Entity` — for position lookups and `AsPersonalEnemyOf`. |
| `Turns` | The live `TurnManager`. Rarely needed directly. |
| `Rng` | Deterministic `System.Random`, seeded per-scenario-run. |
| `Spawn(blueprint)` | Fluent `EntityBuilder` — begin a spawn chain. |
| `Player` | Fluent `PlayerBuilder` — modify the player. |
| `World` | Fluent `ZoneBuilder` — place objects, clear cells, remove entities. |
| `Log(msg)` | Write to console + in-game `MessageLog` with a `[Scenario]` prefix. |

### `EntityBuilder` (returned by `ctx.Spawn`)

Modifiers (all chainable, applied at the positioning terminal):

| Method | Description |
|--------|-------------|
| `WithStat(name, value)` | Set a stat's `BaseValue` (clamped to `[Min, Max]`). |
| `WithStatMax(name, max)` | Raise a stat's `Max` ceiling (apply before `WithStat`). |
| `WithHp(fraction)` | Set HP to fraction of Max. |
| `WithHpAbsolute(hp)` | Set HP to absolute value. |
| `Passive(true)` / `Hostile()` | Set `BrainPart.Passive` flag. |
| `WithStartingCell(x, y)` | Override `BrainPart.StartingCellX/Y` (for AIGuard return-post). |
| `WithInventory(...items)` | Spawn items, add to inventory (not equipped). |
| `WithEquipment(item)` | Spawn item, add to inventory, call `InventorySystem.Equip`. |
| `WithGoal(goal)` | Push a goal onto the entity's brain stack. |
| `AsPersonalEnemyOf(target)` | Call `brain.SetPersonallyHostile(target)`. |
| `NotRegisteredForTurns()` | Skip `TurnManager.AddEntity` on this entity. |

Positioning terminals (return `Entity`):

| Method | Description |
|--------|-------------|
| `At(x, y)` | Absolute cell. |
| `AtPlayerOffset(dx, dy)` | Relative to `ctx.PlayerEntity`. |
| `NearPlayer(min, max)` | Random in Chebyshev band. |
| `AdjacentToPlayer()` | `NearPlayer(1, 1)`. |
| `InRing(radius, i, n)` | Position `i` of `n` around a ring. |
| `OnFirstPassableCell(pred)` | Row-major cell scan + predicate. |

### `PlayerBuilder` (accessed as `ctx.Player`)

All methods return `PlayerBuilder` for chaining.

| Method | Description |
|--------|-------------|
| `Teleport(x, y)` | Move player (bypasses `BeforeMove`/`AfterMove` events). |
| `SetHp(hp)` | Set HP absolute. |
| `SetHpFraction(frac)` | Set HP as fraction of Max. |
| `SetHpMax()` | Fully heal. |
| `SetStat(name, value)` | Set a stat's `BaseValue`. |
| `SetStatMax(name, max)` | Raise a stat's `Max` ceiling. |
| `AddMutation(class, level=3)` | Grant a mutation by `Type.Name`. |
| `GiveItem(blueprint, count=1)` | Add item(s) to carried inventory. |
| `Equip(blueprint)` | Give + equip (shortcut). |
| `ClearInventory()` | Remove all carried items (not equipped ones). |
| `SetFactionReputation(faction, value)` | Absolute rep (clamped to `[-200, 200]`). |
| `ModifyFactionReputation(faction, delta)` | Silent delta. |

### `ZoneBuilder` (accessed as `ctx.World`)

| Method | Description |
|--------|-------------|
| `PlaceObject(blueprint)` | Returns `ObjectPlacer`: `.At(x, y)` / `.AtPlayerOffset(dx, dy)`. |
| `ClearCell(x, y)` | Remove all non-terrain, non-player entities from a cell. |
| `RemoveEntitiesWithTag(tag)` | Zone-wide removal by tag. Player preserved. |

---

## Where things live

```
Assets/Scripts/Scenarios/
├── Core/
│   ├── IScenario.cs              Scenario contract (single Apply method)
│   ├── ScenarioAttribute.cs      [Scenario(name, category, description)]
│   ├── ScenarioContext.cs        Entry-point for all builders
│   ├── ScenarioRunner.cs         Dispatch on GameBootstrap.OnAfterBootstrap
│   └── PositionResolver.cs       Cell-search algorithms used by builders
├── Builders/
│   ├── EntityBuilder.cs          ctx.Spawn(...) — spawn chain
│   ├── PlayerBuilder.cs          ctx.Player — player mods
│   └── ZoneBuilder.cs            ctx.World — world mods
└── Custom/
    ├── FiveSnapjawAmbush.cs
    ├── SnapjawRingAmbush.cs
    ├── StoutSnapjaw.cs
    ├── WoundedWarden.cs
    ├── MimicSurprise.cs
    ├── EmptyStartingZone.cs
    └── CalmTestSetup.cs

Assets/Editor/Scenarios/
└── ScenarioMenuItems.cs          [MenuItem] stubs for every scenario

Assets/Tests/EditMode/Gameplay/Scenarios/
├── PositionResolverTests.cs
├── EntityBuilderModifierTests.cs
├── PlayerBuilderTests.cs
└── ZoneBuilderTests.cs
```

---

## Further reading

- `Docs/SCENARIO_SCRIPTING.md` — full design rationale, phase breakdown, and
  future roadmap
- `Docs/QUD-PARITY.md` — goal-stack roadmap that scenarios were built to support
