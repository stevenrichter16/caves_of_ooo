# Caves of Qud: Stairs, Vertical Levels & Underground Structure

> Deep dive into how Qud implements stairways, multi-level dungeons, underground depth tiers, and the full vertical dimension of the world. All findings sourced from the decompiled source code at `qud-decompiled-project/`.

---

## Table of Contents

1. [Zone ID Format & the Z Coordinate](#1-zone-id-format--the-z-coordinate)
2. [The 3x3x50 Parasang Structure](#2-the-3x3x50-parasang-structure)
3. [Z-Level Semantics](#3-z-level-semantics)
4. [Vertical Navigation (Directions)](#4-vertical-navigation-directions)
5. [StairsUp Part](#5-stairsup-part)
6. [StairsDown Part](#6-stairsdown-part)
7. [Pull-Down Shafts & Falling](#7-pull-down-shafts--falling)
8. [Zone Connection System](#8-zone-connection-system)
9. [Zone Connection Caching (Build-Time)](#9-zone-connection-caching-build-time)
10. [Stair Placement Zone Builders](#10-stair-placement-zone-builders)
11. [Player Stair Usage Event Chain](#11-player-stair-usage-event-chain)
12. [Zone Transition Mechanics](#12-zone-transition-mechanics)
13. [Depth-to-Tier Mapping](#13-depth-to-tier-mapping)
14. [The Strata System (Underground Materials)](#14-the-strata-system-underground-materials)
15. [Tier Delta Weights (Creature Spawning)](#15-tier-delta-weights-creature-spawning)
16. [Biome 3D Distribution Underground](#16-biome-3d-distribution-underground)
17. [Lair Depth Structure](#17-lair-depth-structure)
18. [Multi-Level Dungeon Generation](#18-multi-level-dungeon-generation)
19. [SultanDungeon (Procedural Dungeons)](#19-sultandungeon-procedural-dungeons)
20. [Golgotha Chute System](#20-golgotha-chute-system)
21. [Redrock Depth Progression](#21-redrock-depth-progression)
22. [Gravity & Flying](#22-gravity--flying)
23. [NPC Stair Behavior](#23-npc-stair-behavior)
24. [Stair Rendering](#24-stair-rendering)
25. [Key Source Files](#25-key-source-files)

---

## 1. Zone ID Format & the Z Coordinate

**Source**: `XRL.World/ZoneID.cs`

Zone IDs follow a **6-part hierarchical format**:

```
World.ParasangX.ParasangY.ZoneX.ZoneY.ZoneZ
```

**Example**: `JoppaWorld.11.22.1.1.10` (surface zone at parasang 11,22, local position 1,1)

| Component | Type | Range | Description |
|-----------|------|-------|-------------|
| World | string | — | World name (e.g. "JoppaWorld") |
| ParasangX | int | 0-79 | X coordinate in the parasang grid |
| ParasangY | int | 0-24 | Y coordinate in the parasang grid |
| ZoneX | int | 0-2 | Local X within parasang |
| ZoneY | int | 0-2 | Local Y within parasang |
| ZoneZ | int | 0-49 | **Vertical depth level** |

**Parsing** (`ZoneID.Parse()`):
```csharp
// Extracts all 6 components by dot-delimiter scanning
ZoneID.Parse(zoneID, out World, out ParasangX, out ParasangY, out ZoneX, out ZoneY, out ZoneZ);
```

**Assembly** (`ZoneID.Assemble()`):
```csharp
// From Location2D (world coordinates)
public static string Assemble(string World, Location2D Location, int ZoneZ = 10)
{
    int parasangX = Location.X / 3;
    int parasangY = Location.Y / 3;
    int zoneX = Location.X % 3;
    int zoneY = Location.Y % 3;
    return $"{World}.{parasangX}.{parasangY}.{zoneX}.{zoneY}.{ZoneZ}";
}
```

**Optional Blueprint/Instance Specifiers**:
Zone IDs can include embedded blueprint specifiers:
```
WorldName@Blueprint@Instance.ParasangX.ParasangY.ZoneX.ZoneY.ZoneZ
```
Example: `JoppaWorld@Barathruum@PlayerSpawn.0.0.0.0.0`

**Zone ID Match Levels** (`ZoneID.Match()`):
- `-1`: Different worlds
- `0`: Same world only
- `1`: Same world and parasang (different zone/depth)
- `2`: Identical zone ID

---

## 2. The 3x3x50 Parasang Structure

**Source**: `XRL.World/Definitions.cs`, `XRL.World/CellBlueprint.cs`

Each parasang (world map tile) contains a **3D grid** of zones:

```csharp
// Definitions.cs
public static int Width = 3;     // zones per parasang X
public static int Height = 3;    // zones per parasang Y
public static int Layers = 50;   // total vertical levels (0-49)
```

```csharp
// CellBlueprint.cs — one per parasang
public class CellBlueprint
{
    public ZoneBlueprint[,,] LevelBlueprint = new ZoneBlueprint[3, 3, 50];
}
```

**Each parasang = 3 x 3 x 50 = 450 possible zones.**

Each individual zone is **80 cells wide x 25 cells tall**.

**Coordinate Resolution**:
```csharp
// Zone.cs
public int ResolvedX => wX * 3 + X;    // Global X = parasangX*3 + zoneX
public int ResolvedY => wY * 3 + Y;    // Global Y = parasangY*3 + zoneY
```

The full world (JoppaWorld) is 240x75 parasangs = 720x225 zones horizontally, each with 50 vertical layers.

---

## 3. Z-Level Semantics

**Source**: `XRL.World.ZoneBuilders/Strata.cs`, `XRL.World.WorldBuilders/JoppaWorldBuilder.cs`

| Z Range | Name | Description |
|---------|------|-------------|
| 0-9 | Upper void | Rarely used, special cases |
| **10** | **Surface** | The default overworld level. All terrain, villages, and map features generate here |
| 11-15 | Shallow underground | Typical lair depth, caves beneath surface |
| **15** | **Critical boundary** | Below this, depth-based tier scaling kicks in |
| 16-25 | Mid underground | Increasing difficulty, new materials appear |
| 26-35 | Deep underground | Lava pools, black shale, high-tier creatures |
| 36-49 | Abyssal depths | Maximum difficulty, lava-dominated |

**Surface Detection in Code**:
```csharp
string text2 = "surface";
if (list2[num2].z > 10) {
    text2 = "underground";
}
```

**The Z=10 Convention**: The surface is NOT z=0. It's z=10, leaving room for "above ground" levels (z=0-9) and 39 underground levels (z=11-49). This is a deliberate design choice that provides headroom for special vertical content.

---

## 4. Vertical Navigation (Directions)

**Source**: `XRL.Rules/Directions.cs`, `XRL.World/ZoneManager.cs`

Qud uses direction strings for all movement, including vertical:

| Direction | dX | dY | dZ | Description |
|-----------|----|----|-----|-------------|
| "N" | 0 | -1 | 0 | North |
| "S" | 0 | +1 | 0 | South |
| "E" | +1 | 0 | 0 | East |
| "W" | -1 | 0 | 0 | West |
| "NW","NE","SW","SE" | ±1 | ±1 | 0 | Diagonals |
| **"U"** | 0 | 0 | **-1** | **Up (ascend)** |
| **"D"** | 0 | 0 | **+1** | **Down (descend)** |

**Key insight**: "U" decreases Z (going toward surface), "D" increases Z (going deeper).

**ApplyDirectionGlobal** handles parasang boundary wrapping for horizontal movement:
```csharp
public static void ApplyDirectionGlobal(string dir, ref int x, ref int y, ref int z,
                                         ref int wx, ref int wy, int d = 1)
{
    ApplyDirection(dir, ref x, ref y, ref z, d);
    // X wrapping at parasang boundaries
    while (x < 0) { x += 3; wx--; }
    while (x > 2) { x -= 3; wx++; }
    // Y wrapping at parasang boundaries
    while (y < 0) { y += 3; wy--; }
    while (y > 2) { y -= 3; wy++; }
}
```

**Z-Level Wrapping** in `ZoneManager.GetZoneFromIDAndDirection()`:
```csharp
if (ZoneZ < 0) ZoneZ = Definitions.Layers - 1;  // Wrap to 49
if (ZoneZ >= Definitions.Layers) ZoneZ = 0;       // Wrap to 0
```

**Critical**: Vertical movement (U/D) does NOT change parasang or local zone XY coordinates — only Z changes. You stay at the same map position but change depth.

---

## 5. StairsUp Part

**Source**: `XRL.World.Parts/StairsUp.cs`

The `StairsUp` part is attached to stairway objects that let entities ascend.

**Properties**:
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Connected` | bool | true | Whether stairs connect to the zone above |
| `ConnectionObject` | string | "StairsDown" | Blueprint of the matching stair above |
| `Sound` | string | "sfx_interact_stairs_ascend" | Sound played on use |

**Events Handled**:

| Event | Behavior |
|-------|----------|
| `SubjectToGravityEvent` | Sets `SubjectToGravity = false` (objects on stairs don't fall) |
| `CanSmartUseEvent` | Allows smart-use only if actor is NOT already in the stair cell |
| `CommandSmartUseEvent` | Player: pushes `CmdMoveU` keyboard command. NPC: calls `Move("U")` |
| `EnteredCellEvent` | Registers zone connection; removes duplicate stairs; calls `ClearWalls()` |
| `IdleQueryEvent` | 1/2000 chance for NPC to climb stairs as idle action |
| `GetInventoryActionsEvent` | Adds "Ascend" action to menu |
| `ClimbUp` (custom) | Checks KeyObject requirement, calls `Move("U")`, plays sound |

**Zone Connection Registration** (in EnteredCellEvent):
```csharp
if (Connected)
    ParentZone.AddZoneConnection("u", X, Y, "StairsDown", ConnectionObject);
else
    ParentZone.AddZoneConnection("u", X, Y, "UpEnd", null);  // Dead-end
```

---

## 6. StairsDown Part

**Source**: `XRL.World.Parts/StairsDown.cs`

The `StairsDown` part handles downward stairways with additional complexity for pit/shaft mechanics.

**Properties**:
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Connected` | bool | true | Whether stairs connect to zone below |
| `ConnectionObject` | string | "StairsUp" | Blueprint of matching stair below |
| `PullDown` | bool | false | Whether objects are involuntarily pulled down (pit shafts) |
| `GenericFall` | bool | false | Use generic fall messages |
| `ConnectLanding` | bool | false | Connect landing zone even if not connected |
| `PullMessage` | string | null | Custom message when pulled down |
| `JumpPrompt` | string | "It looks like an awfully long fall..." | Confirmation prompt |
| `Sound` | string | "sfx_interact_stairs_descend" | Sound played on use |
| `Levels` | int | 1 | How many Z-levels to descend (multi-level stairs) |

**Events Handled**:

| Event | Behavior |
|-------|----------|
| `SubjectToGravityEvent` | Sets gravity to false |
| `GetNavigationWeightEvent` | If PullDown: weight = 99 (AI avoids) |
| `GetAdjacentNavigationWeightEvent` | If PullDown: weight = 4 (AI discouraged nearby) |
| `CheckAttackableEvent` | Returns false (stairs can't be attacked) |
| `InterruptAutowalkEvent` | If PullDown: interrupts autowalk |
| `EnteredCellEvent` | Zone connections; removes duplicates in Tzimtzlum (replaces with Space-Time Rift) |
| `ObjectEnteringCellEvent` | If PullDown: prompts player before entry |
| `ObjectEnteredCellEvent` | After entry: triggers `CheckPullDown()` |
| `GravitationEvent` | Calls `CheckPullDown()` for gravity-affected objects |
| `ZoneBuiltEvent` | On zone build: applies pull-down to existing objects |
| `ClimbDown` (custom) | Checks KeyObject, calls `Move("D")` for each level |

**Zone Connection Registration**:
```csharp
if (Connected)
    ParentZone.AddZoneConnection("d", X, Y, "StairsUp", ConnectionObject);
else if (ConnectLanding)
    ParentZone.AddZoneConnection("d", X, Y,
        PullDown ? "PullDownEnd" : "DownEnd", null);
```

**Connection Type Meanings**:
| Type | Description |
|------|-------------|
| `"StairsDown"` | Connected downward — links to StairsUp below |
| `"StairsUp"` | Connected upward — links to StairsDown above |
| `"DownEnd"` | Unconnected descent endpoint |
| `"UpEnd"` | Unconnected ascent endpoint |
| `"PullDownEnd"` | Unconnected forced-fall endpoint |

---

## 7. Pull-Down Shafts & Falling

**Source**: `XRL.World.Parts/StairsDown.cs` (CheckPullDown method, lines 314-460)

Pull-down stairs (pits, shafts) are a special variant where objects are **involuntarily pulled down**.

### Validation Checks
Before pulling an object down:
1. Not the stairs object itself
2. Has `CanFall` property
3. Not flying (wings/mechanical wings can skip pull-down)
4. `BeforePullDownEvent` not cancelled

### GetPullDownCell Algorithm
Finds the ultimate destination by traversing downward:
```
1. Get cell directly below (z+1) at same XY
2. If destination has a SuspendedPlatform → STOP here
3. If destination has another PullDown stair → RECURSE deeper
4. Return final destination cell + total Distance fallen
```

### Fall Damage Formula
```csharp
// Damage to creatures landed on:
1d4 crushing damage

// Damage to the falling object:
Roll(Distance + "d20+" + (100 + Distance * 25))
```

**Example**: Falling 3 levels = `3d20+175` damage.

### Post-Fall Effects
- `FellDownEvent` fired with distance information
- `Incommunicado` effect applied if separated from party
- Active zone updated if player fell
- Party leader following processed
- Achievement `DIE_BY_FALLING` triggered if player dies

### Related Events
| Event | Source | Description |
|-------|--------|-------------|
| `BeforePullDownEvent` | `XRL.World/BeforePullDownEvent.cs` | Fired before pull — allows cancellation or destination modification |
| `FellDownEvent` | `XRL.World/FellDownEvent.cs` | Fired after fall — includes Object, Cell, FromCell, Distance |
| `SubjectToGravityEvent` | `XRL.World/SubjectToGravityEvent.cs` | Checks if object is affected by gravity |
| `GravitationEvent` | `XRL.World/GravitationEvent.cs` | Triggered to apply gravity at a cell |

---

## 8. Zone Connection System

**Source**: `XRL.World/ZoneConnection.cs`, `XRL.World/ZoneManager.cs`

Zone connections are the data structure that links stairs between levels.

```csharp
public class ZoneConnection
{
    public string Type;     // "StairsUp", "StairsDown", "DownEnd", "UpEnd", "PullDownEnd"
    public int X;           // X position in zone (0-79)
    public int Y;           // Y position in zone (0-24)
    public string Object;   // Blueprint name to spawn (or null)
}
```

**Storage in ZoneManager**:
```csharp
private Dictionary<string, List<ZoneConnection>> ZoneConnections;
// Key = ZoneID, Value = list of connections in that zone
```

**Retrieval**:
```csharp
List<ZoneConnection> connections = zoneManager.GetZoneConnections(zoneID);
```

**Registration**:
```csharp
// Zone.AddZoneConnection delegates to ZoneManager
public void AddZoneConnection(string TargetDirection, int X, int Y,
                               string Type, string ConnectionObject)
{
    if (!Built)
        CacheZoneConnection(TargetDirection, X, Y, Type, ConnectionObject);
    else
        The.ZoneManager.AddZoneConnection(ZoneID, TargetDirection, X, Y, Type, ConnectionObject);
}
```

---

## 9. Zone Connection Caching (Build-Time)

**Source**: `XRL.World/CachedZoneConnection.cs`, `XRL.World/Zone.cs`

During zone generation, connections are **cached** before the zone is fully built:

```csharp
public class CachedZoneConnection : ZoneConnection
{
    public string TargetDirection;  // "u", "d", "-"
}
```

The `-` direction means a **horizontal connection point** (not vertical), used for general connectivity.

**Build-Time Flow**:
1. Zone builder calls `Z.CacheZoneConnection("d", x, y, "StairsUp", "StairsUp")`
2. Connection stored in zone's cached connections list
3. On zone completion, ZoneManager converts cached connections to persistent connections
4. Adjacent zones can query these connections during their own generation

This enables **bidirectional linking**: when zone A builds stairs down, it caches a connection. When zone B (below) generates, it reads zone A's connections to know where to place matching stairs up.

---

## 10. Stair Placement Zone Builders

**Source**: `XRL.World.ZoneBuilders/StairsUp.cs`, `XRL.World.ZoneBuilders/StairsDown.cs`

These are **zone builders** (not the same as the Parts) that place stair objects during zone generation.

### StairsUp Builder

**Configuration**:
- `Number`: How many stairs to place
- `x`, `y`: Position constraints (dice notation like "10-30")
- `Reachable`: Build reachability map from stair location

**Placement Algorithm** (`AddStairUp`):
1. Check existing zone connections for "StairsUp" type → use that position
2. Check if zone above is already built → skip if so (prevent duplicates)
3. Search for empty reachable cells avoiding:
   - `StairsDown`, `OpenShaft`, `LazyAir`, `LazyPit`, `Pit`, `StairBlocker`
4. Validate against coordinate constraints
5. Clear cell and place "StairsUp" blueprint
6. Cache connections:
   ```csharp
   Z.CacheZoneConnection("u", X, Y, "StairsDown", "StairsDown");  // vertical
   Z.CacheZoneConnection("-", X, Y, "Connection", null);            // horizontal
   ```
7. If `Reachable`, build reachability map from stair location

### StairsDown Builder

**Configuration**: Same as StairsUp, plus `EmptyOnly` (bool).

**Placement Algorithm** (`AddStairDown`):
1. Search existing zone connections for "StairsDown" type
2. Check if zone below is already built
3. If coordinates fully specified, check for existing connected stairs
4. Search for appropriate cells (empty, reachable, avoiding same blueprints)
5. Cache connections:
   ```csharp
   Z.CacheZoneConnection("d", X, Y, "StairsUp", "StairsUp");  // vertical
   Z.CacheZoneConnection("-", X, Y, "Connection", null);        // horizontal
   ```

### Stair Position Coordination Between Levels
The system ensures matching stair positions:
1. Level N's StairsDown builder places stairs at (x, y) and caches `"StairsUp"` connection for level N+1
2. Level N+1's StairsUp builder reads that cached connection and places StairsUp at the SAME (x, y)
3. Result: stairs are perfectly aligned between levels with no special alignment code

---

## 11. Player Stair Usage Event Chain

### Ascending (Player presses < / Shift+,)

```
1. Input: CmdMoveU key pressed
2. StairsUp.HandleEvent(CommandSmartUseEvent)
   → Keyboard.PushMouseEvent("Command:CmdMoveU")
3. Movement system: GameObject.Move("U")
4. GetCellFromDirection("U")
   → GetCellFromDirectionGlobal computes target zone
   → ZoneZ decremented by 1
   → ZoneManager.GetZone(newZoneID) — loads/generates target zone
5. Event chain:
   a. ProcessObjectLeavingCell (old cell)
   b. ProcessEnteringCell (new cell)
   c. ProcessObjectEnteringCell
   d. ProcessEnteringZone (if zone changed)
   e. ProcessLeaveCell
6. Object removed from old cell, added to new cell
7. AfterMoved event fired
8. ZoneManager.SetActiveZone(newZone)
9. Party leader following processed
10. Sound: "sfx_interact_stairs_ascend"
```

### Descending (Player presses > / Shift+.)

Same flow but:
- ZoneZ incremented by 1
- For multi-level stairs (`Levels > 1`), Move("D") called N times
- Sound: "sfx_interact_stairs_descend"

### Pull-Down (Involuntary)

```
1. Object enters cell with PullDown stairs
2. ObjectEnteredCellEvent → player prompted if long fall
3. StairsDown.CheckPullDown() called
4. Validation: CanFall, not flying, etc.
5. GetPullDownCell: traverse down through chained pits
6. BeforePullDownEvent (cancellable)
7. SystemMoveTo(destinationCell, forced: true)
8. Collision damage: 1d4 to creatures at destination
9. Fall damage: (Distance)d20 + (100 + Distance*25)
10. FellDownEvent sent
11. Incommunicado effect if party separated
12. Active zone updated
```

---

## 12. Zone Transition Mechanics

**Source**: `XRL.World/ZoneManager.cs`, `XRL.World/Zone.cs`

### GetZoneFromIDAndDirection
The core method for computing the target zone when moving vertically:

```csharp
public string GetZoneFromIDAndDirection(string ZoneID, string Direction)
{
    ZoneID.Parse(ZoneID, out World, out ParasangX, out ParasangY,
                 out ZoneX, out ZoneY, out ZoneZ);

    if (Direction == "u") ZoneZ--;
    if (Direction == "d") ZoneZ++;

    // Wrapping
    if (ZoneZ < 0) ZoneZ = Definitions.Layers - 1;   // 49
    if (ZoneZ >= Definitions.Layers) ZoneZ = 0;        // 0

    return ZoneID.Assemble(World, ParasangX, ParasangY, ZoneX, ZoneY, ZoneZ);
}
```

### GetCellFromDirectionGlobal
Called by the movement system to get the target cell across zone boundaries:

```csharp
public Cell GetCellFromDirectionGlobal(string Direction, bool bLocalOnly, bool bLiveZonesOnly)
{
    int x = X, y = Y;
    int z = ParentZone.GetZoneZ();

    Directions.ApplyDirection(Direction, ref x, ref y, ref z);

    if (x < 0 || y < 0 || x >= Width || y >= Height || z != ParentZone.GetZoneZ())
    {
        // Cross-zone movement — compute new zone, load it, return target cell
        Zone zone = zoneManager.GetZone(world, parasangX, parasangY, zoneX, zoneY, zoneZ);
        return zone.GetCell(x, y);
    }
    return ParentZone.GetCell(x, y);  // Same zone
}
```

### XY Preservation
**Critical design choice**: When moving U/D, the player's X and Y cell coordinates are preserved. You appear at the same (x, y) position in the zone above/below. This is why stair objects are placed at matching positions between levels.

### Zone Loading
When `ZoneManager.GetZone(zoneID)` is called for a zone that doesn't exist yet:
1. Zone is generated from its zone builders
2. Zone builders read cached connections from adjacent zones
3. Stairs are placed at matching positions
4. Zone is cached in `ZoneManager.CachedZones`
5. Subsequent visits return the cached zone

---

## 13. Depth-to-Tier Mapping

**Source**: `XRL.World/ZoneManager.cs` (GetZoneTier, line 3121)

### The Tier Formula

```csharp
public static int GetZoneTier(string world, int wXPos, int wYPos, int ZPos)
{
    // 1. Check terrain's RegionTier tag
    int tier = terrainObject.GetTag("RegionTier");

    // 2. If deep underground (Z > 15), override with depth formula
    if (ZPos > 15)
        tier = Math.Abs(ZPos - 16) / 5 + 2;

    // 3. Clamp to [1, 8]
    return Math.Max(1, Math.Min(8, tier));
}
```

### Depth-to-Tier Table

| Z Level | Depth Below Surface | Formula | Tier |
|---------|-------------------|---------|------|
| 10 | 0 (surface) | RegionTier from terrain | 1-8 (varies) |
| 11-15 | 1-5 | RegionTier from terrain | 1-8 (varies) |
| 16-20 | 6-10 | \|Z-16\|/5 + 2 = 0/5+2 | **2** |
| 21-25 | 11-15 | \|Z-16\|/5 + 2 = 1+2 | **3** |
| 26-30 | 16-20 | \|Z-16\|/5 + 2 = 2+2 | **4** |
| 31-35 | 21-25 | \|Z-16\|/5 + 2 = 3+2 | **5** |
| 36-40 | 26-30 | \|Z-16\|/5 + 2 = 4+2 | **6** |
| 41-45 | 31-35 | \|Z-16\|/5 + 2 = 5+2 | **7** |
| 46-49 | 36-39 | \|Z-16\|/5 + 2 = 6+2 | **8** (max) |

**Surface Tier** (Z <= 15): Determined by the terrain's `RegionTier` tag, set during world generation. Different regions of the overworld have different base tiers.

**Deep Underground** (Z > 15): Overrides terrain tier with a depth-based formula. Every 5 levels deeper adds 1 tier, starting at tier 2.

### Creature Tier from Level
**Source**: `XRL.World/GameObject.cs` (GetTier, line 7685)

```csharp
public int GetTier()
{
    string tag = GetTag("Tier");
    if (!tag.IsNullOrEmpty())
        return Convert.ToInt32(tag);    // Explicit Tier tag overrides
    return (Stat("Level") - 1) / 5 + 1; // Otherwise: Level-based
}
```

| Creature Level | Tier |
|---------------|------|
| 1-5 | 1 |
| 6-10 | 2 |
| 11-15 | 3 |
| 16-20 | 4 |
| 21-25 | 5 |
| 26-30 | 6 |
| 31-35 | 7 |
| 36+ | 8 |

---

## 14. The Strata System (Underground Materials)

**Source**: `XRL.World.ZoneBuilders/Strata.cs`

The Strata builder generates underground terrain with depth-dependent materials:

### Wall Materials by Depth

| Z Range | Common Walls | Uncommon | Rare |
|---------|-------------|----------|------|
| Z <= 5 | Default | Shale, Sandstone, Limestone | Marl, Halite, Gypsum |
| Z = 5-15 | Shale, Sandstone, Limestone | Marl, Halite, Gypsum | Oolite (at Z=15) |
| Z = 15-25 | Slate, Coral Rag | Serpentinite, Quartzite | — |
| Z = 25-35 | Black Shale (Z>25) | Black Marble (rare) | — |
| Z > 35 | Black Marble (common) | — | — |

### Liquid Pools by Depth

| Liquid | Appears | Weight Pattern |
|--------|---------|---------------|
| Pond (water) | All depths | Most common at Z < 35, rare at Z >= 35 |
| OilDeepPool | Z >= 25 | Moderate weight |
| AsphaltDeepPool | Z >= 25 | Moderate weight |
| LavaPool | Z >= 25 | Weight = Z * 10, increases dramatically with depth |

**At Z=35+**: Lava weight reaches 350+, making it the dominant liquid type. The deep underground becomes increasingly volcanic.

---

## 15. Tier Delta Weights (Creature Spawning)

**Source**: `XRL.World/GameObjectFactory.cs` (lines 147-186)

When spawning creatures, the factory uses **exponential tier delta weights** to bias selection:

```
Tier Delta = ZoneTier - CreatureTier
```

| Delta | Weight | Relative Chance |
|-------|--------|----------------|
| -7 | 10 | 1 in 10,000,000 |
| -6 | 100 | 1 in 1,000,000 |
| -5 | 1,000 | 1 in 100,000 |
| -4 | 10,000 | 1 in 10,000 |
| -3 | 100,000 | 1 in 1,000 |
| -2 | 1,000,000 | 1 in 100 |
| -1 | 10,000,000 | 1 in 10 |
| **0** | **100,000,000** | **Exact match (baseline)** |
| +1 | 10,000,000 | 1 in 10 |
| +2 | 1,000,000 | 1 in 100 |
| +3 | 100,000 | 1 in 1,000 |
| +4 | 10,000 | 1 in 10,000 |
| +5 | 1,000 | 1 in 100,000 |
| +6 | 100 | 1 in 1,000,000 |
| +7 | 10 | 1 in 10,000,000 |

**Weight Calculation**:
```
FinalWeight = TierDeltaWeight[delta] * RoleMultiplier * CustomWeightTag
```

**Role Multipliers**:
- Common: x4.0
- Rare: x0.01

This creates a **Gaussian bell curve** — creatures matching the zone's tier are overwhelmingly preferred, but occasional out-of-tier spawns provide variety. Each tier of difference reduces probability by 10x.

---

## 16. Biome 3D Distribution Underground

**Source**: `XRL.World.ZoneBuilders/FungalBiome.cs`, `XRL.World/BiomeManager.cs`

Biomes are not surface-only — they extend underground with **3D noise distribution**:

### Biome Types
- Fungal Biome (mushrooms, spores)
- Slimy Biome (slime bog)
- Tarry Biome (tar pits)
- Rusty Biome (rusted metal)
- Psychic Biome (mental effects)

### 3D Noise Distribution
```csharp
// FungalBiome uses LayeredNoise for 3D biome map (240x75x10)
for (int z = 10; z <= 29; z++) {
    int biomeValue = FungalBiome.BiomeLevels[x, y, z % 10];
    if (biomeValue >= 1) {
        // Generate biome features at this z-level
    }
}
```

Each 10-level "layer" (0-9, 10-19, 20-29) has independent noise. Biome intensity (0-3) determines mutation strength:

| Intensity | Effect |
|-----------|--------|
| 0 | No biome features |
| 1 | Sparse biome creatures (25% chance) |
| 2 | Moderate biome mutations (25-50% chance) |
| 3 | Heavy biome mutations (70% chance) |

### Zone Mutation
```csharp
BiomeManager.MutateZone(Zone Z) {
    foreach (var biome in topBiomes) {
        biome.MutateZone(Z);         // Spawn biome creatures
        biome.MutateGameObject(item); // Mutate existing objects
    }
}
```

---

## 17. Lair Depth Structure

**Source**: `XRL.World.WorldBuilders/JoppaWorldBuilder.cs`

Lairs (dungeons beneath overworld locations) have structured depth:

### Lair Depth Definition
```csharp
string tag = gameObject.GetTag("LairDepth", "2-4");  // Default 2-4 levels deep
int lairLevels = tag.RollCached();  // Roll dice notation
```

### Level Naming Convention
| Level Index | Tag | Name |
|-------------|-----|------|
| 0 (top) | `LairLevelNameSurface` | Surface entrance |
| 1..N-2 | `LairLevelNameSubsurface` | Underground levels |
| N-1 (bottom) | `LairLevelNameFinal` | Deepest level (boss/treasure) |

### Lair Zone ID Construction
```csharp
for (int i = 0; i < lairLevels; i++) {
    string zoneID = Zone.XYToID("JoppaWorld", x, y, 10 + i);
    // z=10 = surface entrance, z=11 = first underground, etc.
}
```

### World Builder Lair Count
JoppaWorldBuilder generates approximately **125 lairs** across the world. Special faction lairs (Nephilim) like Shug'ruith's Burrow, Qas Qon Lair, and Rermadon Lair can extend much deeper (z=10-40+).

---

## 18. Multi-Level Dungeon Generation

**Source**: `XRL.World.ZoneBuilders/` (various)

### General Pattern
All multi-level dungeons follow this pattern:

```csharp
// For each level of the dungeon:
if (Z.Z > surfaceZ)
    new StairsUp().BuildZone(Z);     // All levels have stairs up (except surface)
if (Z.Z < deepestZ)
    new StairsDown().BuildZone(Z);   // All levels have stairs down (except deepest)
```

### Connection Coordination
Zone builders on level N+1 read connections cached by level N:
```csharp
// StairsUp builder checks for existing connections
List<ZoneConnection> connections = Z.GetZoneConnections(targetZoneID);
foreach (var conn in connections) {
    if (conn.Type == "StairsUp") {
        // Place stairs at the same XY as the connection specifies
        PlaceStairsAt(conn.X, conn.Y);
    }
}
```

### ForceConnections Builder
**Source**: `XRL.World.ZoneBuilders/ForceConnections.cs`

Ensures all stairs and connection points are reachable:
1. Find all stairs and zone connection points
2. Use pathfinder with Simplex noise weighting
3. Carve paths through walls to ensure connectivity
4. **CaveLike mode**: Expands paths randomly for natural cave aesthetics

---

## 19. SultanDungeon (Procedural Dungeons)

**Source**: `XRL.World.ZoneBuilders/SultanDungeon.cs` (1380 lines)

The primary system for generating procedural multi-level dungeons.

### Generation Techniques
- **Wave Function Collapse (WFC)**: Uses `WaveCollapseFastModel` with template samples for layout diversity
- **Binary Space Partitioning (BSP)**: Recursively subdivides zones into segments
- **Influence Maps**: Creates regions for semantic object placement

### BSP Algorithm
```csharp
public void partition(Rect2D rect, ref int nSegments, List<ISultanDungeonSegment> segments)
{
    // Recursively split rectangles (horizontal or vertical)
    // Gaussian-random midpoints for variety
    // Minimum size: 8x8
    // Continue until all segments created or size limits reached
}
```

### Segmentation Types
| Type | Description |
|------|-------------|
| `"Zone"` | Full 80x25 map |
| `"Full"` | Interior only (2-78, 2-22) |
| `"BSP:N"` | Binary Space Partition with N iterations |
| `"Ring:N"` | Concentric ring patterns (2 or 3 rings) |
| `"Blocks:rolls,w,h"` | Random rectangular blocks |
| `"Circle:x,y,r"` | Circular segments |
| `"Tower:x,y,r,t"` | Hollow circular rings |

### Region Classification
Influence map regions are classified as:
| Region | Description |
|--------|-------------|
| `"vault"` | Relic/treasure chambers (dead-ends) |
| `"cult"` | Enemy encounter zones |
| `"abandoned"` | Sparse population |
| `"connection"` | Corridor regions |

### Tier-to-Period Mapping (Aesthetics)
| Zone Tier | Period | Wall Style |
|-----------|--------|-----------|
| 0-2 | 5 | Modern/recent |
| 3-4 | 4 | — |
| 5-6 | 3 | — |
| 7 | 2 | — |
| 8+ | 1 | Ancient |

Uses population: `"SultanDungeons_Wall_Default_Period" + period`

### Theme System (SultanDungeonArgs)
Dungeon themes map to quest/entity properties:
- "scholarship" -> Scholarship theme
- "might" -> Warrior theme
- "stars" -> Stars theme

Templates, walls, encounters, and furnishings are all selected based on theme.

---

## 20. Golgotha Chute System

**Source**: `XRL.World.ZoneBuilders/GolgothaChutes.cs`

Golgotha uses a unique **vertical conveyor shaft system** connecting Z-levels.

### Template Generation
```csharp
public static GolgothaTemplate GenerateGolgothaTemplate()
{
    MainBuilding = new Box(29, 1, 47, 6);
    Chutes = Tools.GenerateBoxes(
        MainBuilding.Grow(1),
        BoxGenerateOverlap.NeverOverlap,
        new Range(4, 4),       // 4 chutes
        new Range(10, 76),     // width: 10-76
        new Range(8, 20),      // height: 8-20
        new Range(200, 2000)   // area: 200-2000
    );
}
```

### Level Structure
- **Z == 15 (Top Level)**: Elevator hub, main building, elevator switch
- **Z > 10, Z != 15 (Chute Levels)**: Conveyor pads with directional flow

### Chute Hazards
| Chute | Trap Type | Interval |
|-------|----------|----------|
| 0 | WalltrapFire | 4-8 turns |
| 1 | WalltrapAcid | 12-16 turns |
| 2 | WalltrapShock | 4-8 turns |
| 3 | WalltrapCrabs | 4-16 turns |

### Cross-Level Coordination
Chutes store Y-coordinate anchors so tunnels align vertically:
```csharp
// Read Y-coordinate from zone above
string endY = ZoneManager.GetZoneProperty(
    Z.GetZoneIDFromDirection("U"), "Chute" + num + "EndY");
// Read Y-coordinate from zone below
string startY = ZoneManager.GetZoneProperty(
    Z.GetZoneIDFromDirection("D"), "Chute" + num + "StartY");
```

---

## 21. Redrock Depth Progression

**Source**: `XRL.World.ZoneBuilders/Redrock.cs`

Redrock demonstrates classic depth-based difficulty progression:

### Stair Placement
```csharp
if (Z.Z > 10) new StairsUp().BuildZone(Z);    // All levels have stairs up
if (Z.Z < 14) new StairsDown().BuildZone(Z);   // Levels 11-13 have stairs down
```

### Per-Level Configuration

| Z | Encounter Count | Special Features | Noise Seeds |
|---|----------------|------------------|-------------|
| 11 | 0-1 | Minimal caves | 1-3 per sector |
| 12 | 1-2 (75% boost) | Standard caves | 0-5 per sector |
| 13 | 2-3 | Fortress (Stockade/City/Fort) | 0-6 per sector |
| 14 | 3-4 | River system integration | 0-7 per sector |

Noise generation increases with depth — deeper levels are more complex and cave-dense.

---

## 22. Gravity & Flying

**Source**: `XRL.World/SubjectToGravityEvent.cs`

### Gravity Check
```csharp
public static bool Check(GameObject Object)
{
    bool flag = true;
    if (Object.HasPropertyOrTag("IgnoresGravity"))
        flag = false;

    SubjectToGravityEvent evt = FromPool();
    evt.SubjectToGravity = flag;
    Object.HandleEvent(evt);  // Parts can override
    return evt.SubjectToGravity;
}
```

### Gravity Interactions with Stairs
- Both StairsUp and StairsDown set `SubjectToGravity = false`
- Objects on stairs are stable and don't fall
- `SuspendedPlatform` objects create "islands" that interrupt pull-down cascades
- `Wings` and `MechanicalWings` parts check for StairsDown.PullDown and can avoid falling

### Special Properties
| Property/Tag | Effect |
|-------------|--------|
| `IgnoresGravity` | Never falls |
| `CanFall` | Required for pull-down to work |
| `SuspendedPlatform` | Stops downward cascade |
| `FallPreposition` | Custom text: "down X" |
| `FallUseDefiniteArticle` | "the shaft" vs "a shaft" |

---

## 23. NPC Stair Behavior

**Source**: `XRL.World.Parts/StairsUp.cs`, `XRL.World.Parts/StairsDown.cs` (IdleQueryEvent)

NPCs can use stairs as an idle action:

```csharp
// 1 in 2000 chance per tick
if (property == 1 && Stat.Random(1, 2000) == 1)
{
    // Create goal: move to stair object, then climb
    var goal = new DelegateGoal(delegate(GoalHandler handler) {
        handler.ParentBrain.ParentObject.Move("U", Forced: false);
    });
    E.Actor.RequirePart<Brain>().PushGoal(new MoveTo(ParentObject));
    E.Actor.RequirePart<Brain>().PushGoal(goal);
}
```

The `IdleStairs` property (int, set to 1) controls which stairs NPCs will use. Only stairs with this property attract idle NPC traffic.

For PullDown stairs, NPCs are actively discouraged:
- Navigation weight = 99 (nearly impassable for pathfinding)
- Adjacent navigation weight = 4 (discouraged even being near)

---

## 24. Stair Rendering

**Source**: `XRL.World.Parts/StairHighlight.cs`

Optional visual highlighting for stairs:

```csharp
public override bool FinalRender(RenderEvent E, bool bAlt)
{
    if (bEnabled && Options.HighlightStairs &&
        ParentObject.CurrentCell.IsExplored() &&
        !ParentObject.CurrentCell.HasObjectWithPropertyOrTag("SuspendedPlatform"))
    {
        E.CustomDraw = true;
        E.ColorString = Options.UseTiles ? "&y^M" : "&Y^M";  // Yellow/Magenta
        E.DetailColor = "Y";
    }
}
```

Stairs are displayed with **yellow on magenta** highlighting when:
- The option is enabled
- The cell has been explored
- No SuspendedPlatform is present

### Standard Stair Blueprints
| Blueprint | Glyph | Part | Description |
|-----------|-------|------|-------------|
| StairsUp | `<` | StairsUp | Ascending stairs |
| StairsDown | `>` | StairsDown | Descending stairs |
| OpenShaft | `>` | StairsDown (PullDown=true) | Open pit shaft |
| Pit | varies | StairsDown (PullDown=true) | Natural pit |

---

## 25. Key Source Files

### Core Parts
| File | Description |
|------|-------------|
| `XRL.World.Parts/StairsUp.cs` | Ascending stair part — all climb-up logic |
| `XRL.World.Parts/StairsDown.cs` | Descending stair part — climb-down + pull-down + fall damage |
| `XRL.World.Parts/StairHighlight.cs` | Visual stair highlighting |

### Zone System
| File | Description |
|------|-------------|
| `XRL.World/ZoneID.cs` | Zone ID parsing and assembly |
| `XRL.World/Zone.cs` | Zone class — coordinates, connections, cell grid |
| `XRL.World/ZoneManager.cs` | Zone loading, caching, tier calculation, connection management |
| `XRL.World/ZoneConnection.cs` | Connection data structure |
| `XRL.World/CachedZoneConnection.cs` | Build-time cached connections |
| `XRL.World/Definitions.cs` | Constants: Width=3, Height=3, Layers=50 |

### Zone Builders
| File | Description |
|------|-------------|
| `XRL.World.ZoneBuilders/StairsUp.cs` | Zone builder: places StairsUp objects |
| `XRL.World.ZoneBuilders/StairsDown.cs` | Zone builder: places StairsDown objects |
| `XRL.World.ZoneBuilders/ForceConnections.cs` | Ensures stair reachability by carving paths |
| `XRL.World.ZoneBuilders/Strata.cs` | Underground material generation by depth |
| `XRL.World.ZoneBuilders/SultanDungeon.cs` | Procedural dungeon generation (WFC + BSP) |
| `XRL.World.ZoneBuilders/GolgothaChutes.cs` | Vertical conveyor shaft system |
| `XRL.World.ZoneBuilders/Redrock.cs` | Depth-based progression example |

### Events
| File | Description |
|------|-------------|
| `XRL.World/SubjectToGravityEvent.cs` | Gravity check for objects |
| `XRL.World/GravitationEvent.cs` | Gravity application trigger |
| `XRL.World/BeforePullDownEvent.cs` | Pre-fall cancellation hook |
| `XRL.World/FellDownEvent.cs` | Post-fall notification with distance |

### Navigation
| File | Description |
|------|-------------|
| `XRL.Rules/Directions.cs` | Direction application including U/D for Z-axis |
| `XRL.World/GameObject.cs` | Move() method — core movement including zone transitions |

### World Structure
| File | Description |
|------|-------------|
| `XRL.World/CellBlueprint.cs` | 3x3x50 zone blueprint grid per parasang |
| `XRL.World.WorldBuilders/JoppaWorldBuilder.cs` | World generation, lair placement, terrain assignment |
| `XRL.World.ZoneBuilders/FungalBiome.cs` | 3D biome distribution example |
| `XRL.World/BiomeManager.cs` | Biome mutation of zones |
