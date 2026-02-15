# Floor & Strata System Analysis

## Caves of Qud Source Code Analysis

### Zone Addressing — The Z Coordinate

Qud identifies every zone in the game with a string ID: `World.ParasangX.ParasangY.ZoneX.ZoneY.ZoneZ`

- **ParasangX/Y** — world map cell (80x25 overworld grid)
- **ZoneX/Y** — sub-zone within a parasang (3x3 grid, so 0-2)
- **ZoneZ** — the vertical layer (0-49, 50 total layers)

**Layer 10 is the surface.** Layers 0-9 are sky/upper levels, layers 11-49 are underground strata.

Source: `ZoneID.cs`, `Definitions.cs` (`Layers = 50`)

### Zone Blueprint System

Each world cell type (terrain) defines a `CellBlueprint` which contains a 3D array:
```
ZoneBlueprint[3, 3, 50] LevelBlueprint
```

This maps every `[ZoneX, ZoneY, ZoneZ]` to a `ZoneBlueprint` containing the builder pipeline for that specific zone. Zone blueprints are loaded from XML `Worlds.xml` with range specs:

```xml
<zone Level="10" x="0-2" y="0-2">        <!-- surface -->
  <builder Class="SurfaceCave" />
  <builder Class="StairsDown" />
  <population Table="CaveTier1" />
</zone>
<zone Level="11-49" x="0-2" y="0-2">     <!-- underground strata -->
  <builder Class="SolidEarth" />
  <builder Class="Strata" />
  <population Table="UndergroundCreatures" />
</zone>
```

Source: `WorldFactory.cs` lines 462-491, `CellBlueprint.cs`, `ZoneBlueprint.cs`

### The Strata Builder (`Strata.cs`)

The Strata builder is the heart of underground cave generation. Key mechanics:

**1. Wall Material by Depth**
- Weighted material selection varies by Z level (depth)
- Shallow (Z ≤ 15): Sandstone, Limestone, Shale, Marl, Halite, Gypsum — common sedimentary rocks
- Mid (Z 15-25): Oolite, Slate, Coral Rag — transitional materials
- Deep (Z 25-35): Serpentinite, Quartzite, Black Shale — metamorphic/igneous
- Very deep (Z 35+): Black Marble — rare prestige material
- Simplex noise adds per-zone variation to material weights, so adjacent zones can have different dominant rock types

**2. Cave Layout Algorithms**
Different rock types produce different cave shapes:
- **Cellular Automata** (`getCaveLayout`) — default for most rocks. Perlin noise + cellular grid
- **Box Filter** (`getBoxFilterCave`) — for Oolite/Marl. Random walk + smoothing passes
- **Random Walk** (`getRandomWalkCaveLayout`) — for Limestone/Coral Rag. Drunkard's walk carving
- **Pillars** (`getPillarsLayout`) — for Black Marble. Cellular noise with distance-based voronoi
- **Porous** (`getPorousLayout`) — for Halite/Gypsum. Simplex fractal billow noise
- **Windy** (`getWindyLayout`) — for Serpentinite/Sandstone. Billow noise with directional bias
- **Blocky** (`getBlockyLayout`) — for Quartzite. Cellular Manhattan distance

**3. Dual-Material Blending**
Each zone uses TWO wall materials:
- Primary material (highest weight) defines the dominant layout algorithm
- Secondary material blends in via simplex noise threshold
- The blend ratio depends on relative weights + noise, creating natural geological boundaries

**4. Liquid Pools by Depth**
- Shallow: Water pools are common (weight 1000)
- Mid (Z 25+): Oil pools, asphalt, lava start appearing
- Deep (Z 35+): Lava pools dominate (weight 1000), asphalt common
- Slime, acid, and other exotic liquids have flat low weights at all depths

**5. Floor Tile Painting**
After terrain generation, floor tiles are painted with colors derived from the wall materials:
- Primary/secondary material foreground and background colors
- Random dot patterns (`.`) for floor rendering
- 5% chance of grey floor tiles for variety

### 3D Tunnel Maze

Both `Cave.cs` and `Strata.cs` share a static `Maze3D TunnelMaze`:
```csharp
TunnelMaze = RecursiveBacktrackerMaze3D.Generate(seed, 240, 75, 30, ...)
```
- 240×75×30 cells covering the entire world across 30 underground layers
- Uses recursive backtracker algorithm for guaranteed connectivity
- Each maze cell has 6 directional flags: N/S/E/W/Up/Down
- The maze ensures all underground areas are reachable via tunnels
- Maze connections determine where stairs up/down are placed

Source: `Cave.cs` line 46, `Strata.cs` line 56

### Stairs and Vertical Connections

**StairsDown Builder** (`ZoneBuilders/StairsDown.cs`):
- Places a StairsDown object in a reachable, empty cell
- Registers a zone connection: `Z.CacheZoneConnection("d", x, y, "StairsUp", "StairsUp")`
- The "d" direction means "this point connects downward"
- The StairsUp object will be placed at the same x,y in the zone below

**StairsUp Builder** (`ZoneBuilders/StairsUp.cs`):
- Places StairsUp matching existing zone connections from the layer above
- Registers: `Z.CacheZoneConnection("u", x, y, "StairsDown", "StairsDown")`

**StairsDown Part** (`Parts/StairsDown.cs`):
- Handles `ClimbDown` event → calls `entity.Move("D")`
- Can be `PullDown` (pits/shafts that pull entities downward automatically)
- Supports multi-level falls with fall damage
- On EnteredCell, registers `"d"` zone connection for the zone manager

**StairsUp Part** (`Parts/StairsUp.cs`):
- Handles `ClimbUp` event → calls `entity.Move("U")`
- On EnteredCell, registers `"u"` zone connection

**StairConnector Builder**:
- Pathfinds between StairsUp and StairsDown in the same zone
- Clears walls along the path using a "Drillbot" pathfinder
- Ensures stairs are always reachable from each other

### Zone Tier System

Zone tier determines creature difficulty and loot quality:

```csharp
// ZoneManager.GetZoneTier()
if (ZPos > 15) {
    tier = Math.Abs(ZPos - 16) / 5 + 2;  // Underground: every 5 levels = +1 tier
}
// Clamped to 1-8
```

| Depth (Z) | Tier | Content |
|-----------|------|---------|
| 10 (surface) | 1 (from terrain) | Surface creatures |
| 11-15 | 1-2 | Shallow caves |
| 16-20 | 2 | Moderate underground |
| 21-25 | 3 | Mid caves |
| 26-30 | 4 | Deep caves |
| 31-35 | 5 | Very deep |
| 36-40 | 6 | Extreme depth |
| 41-45 | 7 | Near-bottom |
| 46-49 | 8 | Maximum depth |

Population builders pass `"zonetier"` to the population system, which selects tier-appropriate creatures.

Source: `ZoneManager.cs` lines 3121-3150

### Cave Builder (Shallow Underground)

`Cave.cs` handles zones closer to the surface (typically levels 11-15):
- Uses Perlin noise (1200×375 resolution) + cellular automata
- Shared TunnelMaze for connectivity
- Special: at Z > 49, adds lava pools
- 0.2% chance of spawning a CaveCity (rare encounter)
- Simpler than Strata — single wall type, no material blending

---

## Current Caves of Ooo Architecture

### Zone ID Format
Currently: `"Overworld.X.Y"` — flat, no Z coordinate.

### World Map
10×10 grid of BiomeType cells (Cave, Desert, Jungle, Ruins). Each cell = one zone.

### Zone Generation
- `ZoneManager.GenerateZone()` → `GetPipelineForZone()` → builder pipeline
- `OverworldZoneManager` routes by biome type
- 4 terrain builders: CaveBuilder, DesertBuilder, JungleBuilder, RuinsBuilder
- Support builders: BorderBuilder, ConnectivityBuilder, PopulationBuilder, TradeStockBuilder
- All builders implement `IZoneBuilder` with Priority ordering

### What's Missing
- No Z coordinate in zone IDs
- No vertical connections (stairs up/down)
- No depth-based generation variation
- No tier system for scaling difficulty
- No inter-zone connection registry
- No "solid earth" base layer concept
- No stair placement or pathfinding between stairs

---

## Proposed Implementation

### Phase 1: Zone ID Extension + Z Coordinate

**Extend zone ID format** from `"Overworld.X.Y"` to `"Overworld.X.Y.Z"`:
- Z=0 is the surface (current game)
- Z=1+ are underground levels
- Keep it simple: no sub-zone grid (Qud's 3x3 within a parasang is overkill for us)

**Changes:**
- `WorldMap.cs` — Update `ToZoneID`, `FromZoneID`, `GetAdjacentZoneID` to include Z
- Add `GetZoneAbove(zoneID)` and `GetZoneBelow(zoneID)` helpers
- `ZoneTransitionSystem.cs` — Add Up/Down to `TransitionDirection`

### Phase 2: Zone Connection Registry

Add a connection tracking system that links zones vertically:

```csharp
public class ZoneConnection
{
    public string SourceZoneID;
    public int SourceX, SourceY;
    public string TargetZoneID;
    public int TargetX, TargetY;
    public string Type; // "StairsDown", "StairsUp", "Pit"
}
```

Store connections on `ZoneManager`:
```csharp
Dictionary<string, List<ZoneConnection>> _connections; // keyed by zoneID
```

When generating the zone below, the system checks for existing connections to determine where StairsUp should appear.

### Phase 3: Underground Terrain Generation

**SolidEarthBuilder** (Priority 500):
- Fills all cells with wall objects (the "rock" base)
- Uses wall material determined by depth

**StrataBuilder** (Priority 1000):
- Core underground cave carver
- Simplified from Qud: 3 layout algorithms instead of 7
  - **Cellular Automata** — default (most depths)
  - **Random Walk** — for mid depths
  - **Noise Pillars** — for deep levels
- Single wall material per zone (no dual-material blending initially)
- Wall material determined by depth tier

**Wall Materials by Depth:**

| Depth | Material | Cave Style |
|-------|----------|------------|
| 1-3 | Sandstone | Cellular |
| 4-6 | Limestone | Random Walk |
| 7-9 | Shale | Cellular |
| 10-12 | Slate | Cellular |
| 13-15 | Quartzite | Noise Pillars |
| 16+ | Obsidian | Noise Pillars |

### Phase 4: Stair Placement

**StairsDownBuilder** (Priority 3000):
- Finds a reachable, empty cell
- Places a StairsDown entity (new blueprint)
- Registers a ZoneConnection from this zone to z+1

**StairsUpBuilder** (Priority 3000):
- Reads existing connections from the zone above
- Places StairsUp at the matching coordinates
- If no connection exists (underground-only entry), picks a random position

**StairConnectorBuilder** (Priority 3500):
- If both StairsUp and StairsDown exist in a zone, pathfinds between them
- Clears walls along the path to ensure connectivity
- Simple A* or BFS pathfinding through the wall grid

### Phase 5: Stair Entities and Player Interaction

**StairsDown Blueprint:**
```json
{
  "Name": "StairsDown",
  "DisplayName": "stairs leading down",
  "Render": { "Glyph": ">", "Color": "white" },
  "Parts": [{ "Name": "StairsDown" }]
}
```

**StairsDown Part:**
- On player interaction (new key: `>` or designated key), transition to z+1
- Creates the zone below on first descent (lazy generation)
- Registers the connection if not already registered

**StairsUp Part:**
- On player interaction (`<` or designated key), transition to z-1
- Surface StairsUp returns player to the overworld zone

### Phase 6: Zone Tier Scaling

Add tier calculation based on depth:
```csharp
public int GetZoneTier(string zoneID) {
    int z = ParseZ(zoneID);
    if (z == 0) return 1; // surface
    return Math.Min(z / 3 + 1, 8); // every 3 levels = +1 tier
}
```

Use tier in population tables:
- `PopulationTable.GetForTier(int tier)` — returns appropriate creature mix
- Tier 1: Snapjaws (existing)
- Tier 2: Tougher snapjaws, new enemies
- Tier 3+: Deeper creatures (future content)

### Phase 7: Surface Cave Entrances

Each surface zone can have 0-2 cave entrances:
- **CaveEntranceBuilder** (Priority 2500, surface only)
- Places a StairsDown glyph (`>`) on the surface
- The entrance leads to `Overworld.X.Y.1`

---

## Prerequisites & Dependencies

### Already Implemented (Ready to Use)
- [x] **Entity-Part system** — StairsDown/StairsUp parts can extend Part
- [x] **Zone generation pipeline** — IZoneBuilder interface with priority ordering
- [x] **Population system** — PopulationTable + PopulationBuilder
- [x] **Zone caching** — ZoneManager caches generated zones by ID
- [x] **Zone transition system** — Handles N/S/E/W transitions
- [x] **Blueprint system** — EntityFactory + JSON blueprints
- [x] **Cell system** — Zone has 80×25 Cell grid with entity lists
- [x] **Rendering** — ZoneRenderer renders entity glyphs on tilemap
- [x] **Input handling** — InputHandler processes player commands

### Needs Implementation (Prerequisites)

#### 1. Zone ID Z-Coordinate Support (Required First)
**Files:** `WorldMap.cs`, `ZoneManager.cs`, `ZoneTransitionSystem.cs`, `OverworldZoneManager.cs`

The zone ID format must be extended to include Z before anything else. Currently `"Overworld.X.Y"` → needs `"Overworld.X.Y.Z"`. This touches:
- Zone ID parsing/creation in WorldMap
- Zone ID generation in GameBootstrap (starting zone becomes `"Overworld.5.5.0"`)
- Adjacent zone lookups in ZoneTransitionSystem
- Pipeline routing in OverworldZoneManager

#### 2. Zone Connection Registry (Required Before Stairs)
**Files:** New `ZoneConnectionRegistry.cs` or added to `ZoneManager.cs`

A system to track connections between zones (stair pairs). Without this, generating the zone below wouldn't know where to place the matching StairsUp.

#### 3. Vertical Transition in InputHandler (Required Before Stairs)
**Files:** `InputHandler.cs`

The input handler needs to support `<` and `>` keys (or equivalent) for ascending/descending stairs. Currently only processes movement, inventory, trade, and conversation.

#### 4. Wall Entity Blueprints (Required Before Strata)
**Files:** `Objects.json`

New wall blueprints for underground materials: Sandstone, Limestone, Shale, Slate, Quartzite, Obsidian. Currently only generic walls exist.

#### 5. Pathfinding for Stair Connectivity (Nice-to-Have)
**Files:** New pathfinding utility or simple BFS

A simple pathfinding system to clear a path between StairsUp and StairsDown within a zone. Can be a basic BFS through the cell grid initially.

### Not Required (Can Be Added Later)
- Noise-based cave generation (current cellular automata works fine initially)
- Dual-material blending (start with single material per zone)
- Liquid pools (future content)
- 3D tunnel maze (overkill for initial implementation)
- Fall damage from pits
- Multiple stair types (regular, shaft, pit)

---

## Implementation Order

```
1. Zone ID Z-Coordinate       ← Foundation, must be first
2. Zone Connection Registry    ← Needed for stair pairing
3. Wall Material Blueprints    ← Data for underground rendering
4. SolidEarthBuilder          ← Base layer for underground
5. StrataBuilder              ← Cave carving
6. StairsDown/Up Builders     ← Stair placement during generation
7. StairConnectorBuilder      ← Path between stairs
8. Stair Blueprints + Parts   ← Entities the player interacts with
9. InputHandler Stair Keys    ← Player can use stairs
10. CaveEntranceBuilder       ← Surface connects to underground
11. Zone Tier Scaling          ← Difficulty progression
12. Tier-scaled Populations    ← Deeper = harder enemies
```

Steps 1-2 are infrastructure. Steps 3-7 are generation. Steps 8-10 are gameplay. Steps 11-12 are content scaling.

**Estimated scope:** Each step is a focused change to 1-3 files. The entire system can be built incrementally with each step testable independently.
