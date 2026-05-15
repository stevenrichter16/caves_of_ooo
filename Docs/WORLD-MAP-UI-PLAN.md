# World Map UI — Plan

**Branch:** `feat/world-map-ui`
**Date:** 2026-05-14
**Status:** WM.1 → WM.7 complete (7 commits, 47 new tests).
202/202 in full regression sweep across all WM + Save + Diag fixtures.

## Implementation log

| Sub | Commit | Tests | Highlights |
|---|---|---|---|
| WM.1 | `d4b9c93` | — | Plan to disk, branch cut from main |
| WM.2 | `dbdccb2` | +10 | ZoneID `"WorldMap"`, builder, offset constants, coord translators |
| WM.3 | `a7961b7` | +11 | `WorldMapPart`, Ascend/Descend, LastZoneIDOnSurface restore |
| WM.4 | `5b569eb` | +7 | `TurnManager.AdvanceClock`, `WorldMapTravelCostPart` (10 ticks/step) |
| WM.5 | `8c2a027` | +8 | `Visited[20,20]` fog-of-war + save/load (FormatVersion 3→4) |
| WM.6 | `48caa21` | +7 | POI markers (`!` village, `&` lair, `$` merchant, `~` river) |
| WM.7 | (this commit) | +4 | `worldmap/Ascended|Descended|Stepped` diag emissions |
| **Total** | **7 commits** | **+47** | |

> **Genre framing:** CoO is an **RPG, not a roguelike**
> (`Docs/PROJECT-IDENTITY.md`). The world map is a persistent feature
> of the player's character — visited cells stay visited across the
> entire save file, not per-run.

---

## Goal — player-facing

When the player presses **`<`** (ascend) in a ground zone, they
**physically arrive on a world-map zone** — a 20×20 grid embedded in
the 80×25 tilemap, surrounded by impassable border cells. They can
walk cell-to-cell using normal movement (WASD / arrows). Each step
on the world map consumes game time (representing the long-distance
travel a single step represents).

Pressing **`>`** (descend) on a world-map cell drops the player into
the ground zone for that cell's `(worldX, worldY)`, placing them at
their **last saved location on the surface** (or the zone center on
first visit).

Each cell on the world map represents **one 80×25 ground zone** —
i.e. 2000 ground cells per world cell. (Qud's ratio is 3×3 zones per
parasang for ~18,000 cells; we keep the simpler 1:1 to match CoO's
existing `WorldMap` data model.)

The world map shows:
- A glyph per cell colored by `BiomeType`
- A POI marker for villages (`!`), lairs (`&`), merchants (`$`)
- A player marker (`@`) at the cell the player is currently in
- A **fog-of-war** dimming on cells the player hasn't visited

---

## Reference — what Qud does (research summary)

The full pre-impl survey is in the verification-sweep section below.
Highlights:

- **Qud's world map IS a Zone** (80×25), distinguished by ZoneID with
  no dots. `IsWorldMap()` checks `ZoneID.IndexOf('.') == -1`
  (`Zone.cs:6775-6782`). The player physically inhabits this zone like
  any other.
- **Ascend** via `CmdMoveU` (`XRLCore.cs:1329-1405`) saves
  `LastLocationOnSurface` then `DirectMoveTo` the parasang cell on the
  world-map zone.
- **Descend** via `CmdMoveD` (`XRLCore.cs:1415-1426`) calls
  `PullDown` (`GameObject.cs:3875-4045`) which returns the player to
  `LastLocationOnSurface` or the parasang's landing point.
- **Travel cost:** `TerrainTravel.cs:161-165` —
  `(300 / (Speed × (100 + travelSpeed%) / 100)) × Segments` per step,
  with `Game.TimeTicks++` every 10 segments.
- **Get-lost mechanic:** `TerrainTravel.cs:179-211` — random chance
  on each worldmap step.
- **Encounters on cell-entry:** `TerrainTravel.cs:77-141` — 10% base
  chance per `EncounterEntry` attached to the cell.
- **Parasang ratio:** 1 worldmap cell = 3×3 ground zones × 80×25 cells
  = 18,000 ground cells per parasang (`JoppaWorldBuilder.cs:249-253`).

**We diverge from Qud on:**
- Worldmap size: 20×20 (CoO's existing `WorldMap.cs:18-19`) rather than
  80×25. Embedded centered in the 80×25 tilemap with impassable border.
- Cell→ground ratio: **1:1** (one worldmap cell = one 80×25 ground
  zone), not Qud's 1:9. Keeps the existing `WorldGenerator` /
  `OverworldZoneManager` infrastructure intact.
- Get-lost and encounters: out of scope for v1. Documented as
  follow-on (WM.8+).

---

## Verification sweep (already complete)

| Premise | Status | Source |
|---|---|---|
| Qud's worldmap is a Zone with no dots in ZoneID; ascend uses `CmdMoveU` saving `LastLocationOnSurface`, descend uses `PullDown` restoring it | ✅ confirmed | `Zone.cs:6775-6782`, `XRLCore.cs:1329-1426`, `GameObject.cs:3875-4045` |
| Qud's worldmap zone is 80×25; each parasang has 3×3 ground zones at z=10 | ✅ confirmed | `ZoneManager.cs:3243, 3272`, `JoppaWorldBuilder.cs:249-253` |
| CoO already has a `WorldMap` class (20×20) with `BiomeType` + nullable `PointOfInterest`, fully save/loaded | ✅ confirmed | `WorldMap.cs:18-22`, `SaveSystem.cs:881-896` |
| CoO has NO world-map UI screen and NO `InputState.WorldMap` | ✅ confirmed | searches across `Presentation/UI/` returned 0 hits; `InputHandler.cs:103-122` enum |
| CoO's `Zone.Width=80, Height=25` are `const` — cannot create a 20×20 zone, must embed | ✅ confirmed | `Zone.cs:13-14` |
| CoO's worldmap data has no per-cell `Visited` flag at the world layer (only per-cell `Explored` inside each ground zone) | ✅ confirmed | `SaveSystem.cs:870-879`, `WorldMap.cs` field list |
| Zone-transition: `ZoneTransitionSystem.TransitionPlayer` handles cross-zone movement, drags party members; `TransitionPlayerVertical` for stairs | ✅ confirmed | `ZoneTransitionSystem.cs:87-161, 317-404` |
| `OverworldZoneManager` routes by ZoneID — easy to add a `"WorldMap"`-prefix route case | ✅ confirmed | `OverworldZoneManager.cs:26-74` |
| `MovementSystem.TryMove` works on any Zone — moving the player on the worldmap zone is just regular movement | ✅ confirmed | `MovementSystem.cs:35-105` |
| Existing `WorldMapTests.cs` has 66 tests pinning the `WorldMap` data model + zone-id helpers + biome generation — high-confidence baseline | ✅ confirmed | `Assets/Tests/EditMode/Gameplay/World/Map/WorldMapTests.cs:1-1479` |
| `Tepui` is NOT yet in `BiomeType` enum — adding 5th value is the W.2 plan, parallel to this work | ⚠️ noted | `WorldMap.cs:3-9` |

**No false premises detected.** One outstanding parallel-feature
awareness (Tepui) which is on a separate branch and doesn't gate this.

---

## Architecture decisions

### Worldmap-as-Zone (Qud-faithful)

The worldmap is a real `Zone` instance. The player physically
inhabits it. Movement, FOV, render, save/load — all reuse the
existing zone machinery. The single new concept is the **ZoneID
namespace**:

- Ground zones: `"Overworld.X.Y.Z"` (existing)
- World map zone: `"WorldMap"` (new — no dots, mirrors Qud)

### Embedded 20×20 layout in 80×25 tilemap

Since `Zone.Width=80, Height=25` are `const`, the worldmap zone is
80×25 like every other zone. We embed the **20×20 logical world map
inside it** with offset `(WORLDMAP_X_OFFSET=30, WORLDMAP_Y_OFFSET=3)`:

- Cells `[30..49] × [3..22]` = the playable 20×20 worldmap grid
- All other cells = impassable border (wall glyphs)
- Top edge (rows 0-2) = HUD strip showing "WORLD MAP — `<` to descend"
- Bottom edge (rows 23-24) = legend strip showing biome key

Player worldmap coords (0..19) translate as:
```
zoneX = worldX + 30
zoneY = worldY + 3
```

### Player marker

The player Entity moves on the worldmap zone like any other zone via
`MovementSystem.TryMove`. The renderer paints the player at their
current `(zoneX, zoneY)`. No special player-marker code needed.

### State preserved across ascend/descend

A new **`WorldMapPart`** on the player tracks:
- `LastZoneIDOnSurface` — string ZoneID the player was in
- `LastZoneX`, `LastZoneY` — cell within that zone

Set on ascend, read on descend. Persists via the standard save/load.

### Fog of war

A new `bool[20,20] Visited` field on `WorldMap`. Set to true when the
player ascends into a worldmap cell. Renderer dims unvisited cells.
Survives save/load via extension of `SaveSystem.SaveWorldMap` /
`LoadWorldMap`.

---

## Performance section

| Risk | Mitigation |
|---|---|
| **Plumbs `ZoneRenderHooks`** | The worldmap zone uses the same rendering pipeline as every other zone. The `MarkCellDirty` calls in `MovementSystem.DirtyForMove` will fire — already handled. No new dirty plumbing needed. |
| **Adds new MonoBehaviour with Update/LateUpdate** | No new MonoBehaviours. The worldmap zone is constructed once at boot via the same `OverworldZoneManager.GetZone(...)` path. |
| **Allocates collections in per-frame / per-turn paths** | Each ascend/descend is a discrete player action, not per-frame. No new collections in hot paths. |
| **Adds a new cache** | The worldmap zone is cached in `ZoneManager._zoneCache` the same way ground zones are. No new dictionary. |
| **Adds a new event listener that fires per-frame / per-turn** | Yes — a `MoveCompleted` listener on the worldmap player for travel-cost time-burn. Gated by `IsWorldMapZone()` check. Fires at most 1× per player turn (player can only move once per turn) → not a hot path. |

---

## Sub-milestones (smallest blast radius first)

### WM.1 — Plan + branch + verification (this commit)

- Plan to disk: `Docs/WORLD-MAP-UI-PLAN.md`
- Verification sweep documented above
- Branch `feat/world-map-ui` cut from `main`

### WM.2 — Worldmap-zone routing + impassable embed (one commit, ~8 tests)

**New:**
- `WorldMap.ToWorldMapZoneID()` static returning the constant `"WorldMap"`
- `WorldMap.IsWorldMapZoneID(id)` predicate
- `WorldMap.WORLDMAP_X_OFFSET = 30`, `WORLDMAP_Y_OFFSET = 3` constants
- `WorldMap.WorldCellToZoneCell(wx, wy)` / `ZoneCellToWorldCell(zx, zy)` helpers
- `WorldMapZoneBuilder.cs` — `IZoneBuilder` that:
  - Fills 80×25 with impassable wall cells (Solid tag + glyph)
  - Inside `[30..49] × [3..22]`, creates one passable cell per worldmap cell
  - Each cell gets a `WorldMapCellPart` carrying `WorldX`, `WorldY` + biome glyph render
- `OverworldZoneManager.CreateWorldMapPipeline()` wires it up
- `OverworldZoneManager.GetPipelineForZone` adds a case for `IsWorldMapZoneID`

**Tests:** `WorldMapZoneBuilderTests.cs`
- `ToWorldMapZoneID_ReturnsConstantString`
- `IsWorldMapZoneID_AcceptsConstant_RejectsOverworld`
- `WorldCellToZoneCell_RoundTrips` (counter-check via ZoneCellToWorldCell)
- `WorldMapZoneBuilder_FillsBordersImpassable`
- `WorldMapZoneBuilder_20x20Region_AllPassable`
- `WorldMapZoneBuilder_EachCell_HasWorldMapCellPart`
- `WorldMapZoneBuilder_BiomeGlyph_MatchesWorldMapBiome` (cave→`#`, desert→`.`, jungle→`%`, ruins→`o`)
- `OverworldZoneManager_RoutesWorldMapID_ToWorldMapPipeline`

### WM.3 — Ascend + Descend + WorldMapPart (one commit, ~10 tests)

**New:**
- `WorldMapPart.cs` — Part on the player tracking
  `LastZoneIDOnSurface`, `LastZoneX`, `LastZoneY`
- New input binding in `InputHandler` for `<` key
- New input binding in `InputHandler` for `>` key
- `WorldMapTraversal.Ascend(player, zoneManager)` — saves state,
  computes the worldmap zone coord from current zone's
  `WorldMap.FromZoneID(currentZoneID)`, transitions player via
  `ZoneTransitionSystem.TransitionPlayerVertical` (or a new
  `TransitionPlayerToWorldMap`)
- `WorldMapTraversal.Descend(player, zoneManager)` — reads player's
  cell on the worldmap zone, translates to `(worldX, worldY)`,
  resolves target `Overworld.X.Y.0`, transitions player to
  saved `LastZoneIDOnSurface` and `(LastZoneX, LastZoneY)`, or center
  if first descent

**Tests:** `WorldMapTraversalTests.cs`
- `Ascend_OnGroundZone_TransitionsToWorldMap`
- `Ascend_FromCenterZone_PlacesPlayerAt10_10` (verify offset math)
- `Ascend_SavesLastZoneIDOnSurface`
- `Descend_OnWorldMapCell_TransitionsToGroundZone`
- `Descend_AfterAscent_RestoresLastZoneIDAndCell` (round-trip)
- `Descend_FirstVisitToCell_PlacesPlayerAtZoneCenter`
- `Ascend_OnWorldMapZone_DoesNothing` (counter-check: can't ascend from worldmap)
- `Descend_OnNonWorldMapZone_DoesNothing` (counter-check: can't descend from ground)
- `Ascend_NullPlayer_NoCrash` (adversarial)
- `Descend_NoWorldMapPart_NoCrash` (adversarial)

### WM.4 — Travel time cost (one commit, ~6 tests)

**New:**
- `WorldMapTravelCostPart.cs` — Part on the player that subscribes to
  `MoveCompleted` and, when the active zone is the worldmap,
  advances `TurnManager` by `WORLDMAP_STEP_TURNS = 10`
- Constants in `WorldMapTraversal`: `WORLDMAP_STEP_TURNS = 10`,
  `WORLDMAP_HP_REGEN_PER_STEP = 5` (placeholder; revisit on playtest)

**Tests:** `WorldMapTravelCostTests.cs`
- `WorldMapMove_ConsumesTenTurns`
- `GroundZoneMove_DoesNotConsumeExtraTurns` (counter-check)
- `WorldMapMove_RegeneratesHP` (deferred if too speculative)
- `WorldMapMove_Diagonal_SameCost` (parity)
- `WorldMapMove_BlockedByWall_NoTurnCost` (counter-check)
- `WorldMapMove_AtFullHP_NoRegenOverflow` (boundary)

### WM.5 — Fog of war + Visited[,] save/load (one commit, ~8 tests)

**New:**
- `bool[20,20] Visited` field on `WorldMap`
- `WorldMap.MarkVisited(wx, wy)` method, called by `Ascend`
- Renderer: dim unvisited cells (color override or alpha)
- `SaveSystem.SaveWorldMap` extended to write Visited bitmap;
  `LoadWorldMap` to read it

**Tests:** `WorldMapVisitedTests.cs`
- `MarkVisited_FlipsFlag`
- `Visited_PersistsAcrossSaveLoad` (round-trip)
- `Ascend_MarksWorldCellVisited`
- `Visited_DefaultsFalse_OnFreshMap`
- `MarkVisited_OutOfBounds_NoOp` (adversarial)
- `Visited_LegacySaveWithoutField_LoadsAllFalse` (back-compat)
- `Renderer_UnvisitedCells_RenderDim` (presentation, may defer)
- `Visited_CenterCellAtStart_IsTrue` (Kyakukya starting cell)

### WM.6 — POI markers + biome legend (one commit, ~5 tests)

**New:**
- `WorldMapCellPart` renders POI marker on top of biome glyph if
  the world cell has a `PointOfInterest`:
  - Village → `!` in yellow
  - Lair → `&` in red
  - MerchantCamp → `$` in green
  - RiverChunk → `~` in blue
- Bottom-row legend in worldmap zone shows biome key

**Tests:** `WorldMapPOIRenderingTests.cs`
- `Village_RendersWithExclamation`
- `Lair_RendersWithAmpersand`
- `MerchantCamp_RendersWithDollar`
- `NoPOI_RendersBiomeGlyph` (counter-check)
- `LegendStrip_PresentInBottomRow`

### WM.7 — Observability + scenario + final sweep (one commit, ~5 tests)

**New diag emissions** under category `worldmap`:
- `Ascended` — player traveled to worldmap zone (from, to)
- `Descended` — player traveled to ground zone (from, to)
- `Stepped` — player moved on worldmap (fromCell, toCell, biome)

**Showcase scenario:** `WorldMapShowcase.cs` — places player at
center, marks all cells visited, opens at the worldmap zone so the
playtest can inspect the rendering.

**Final sweep:**
- Full regression: WorldMapTests + WorldMapZoneBuilderTests +
  WorldMapTraversalTests + WorldMapTravelCostTests +
  WorldMapVisitedTests + WorldMapPOIRenderingTests +
  SaveGraphRoundTripTests + InputHandlerTests
- Update `OBSERVABILITY-STATUS.md` with the new `worldmap` category
- Update this doc with the implementation log

### WM.8+ — DEFERRED (not in this push)

- **Get-lost mechanic** (Qud `TerrainTravel:179-211`) — random chance
  on travel that drops player to a random sub-zone with a Lost effect.
- **Random encounters on cell-entry** (Qud `TerrainTravel:77-141`) —
  10% chance per encounter entry attached to the cell.
- **Variable travel cost** by `BiomeType` (Hills vs Plains in Qud).
- **3×3 sub-zone-per-parasang** (Qud's actual ratio).
- **Fast travel** between two visited cells (cursor-pick + confirm).
- **World-map zoom levels** (e.g. tactical vs strategic view).

---

## Critical files

### New files
| Path | Purpose |
|---|---|
| `Docs/WORLD-MAP-UI-PLAN.md` | This plan |
| `Assets/Scripts/Gameplay/World/Map/WorldMapCellPart.cs` | Per-cell Part on worldmap zone entities |
| `Assets/Scripts/Gameplay/World/Generation/Builders/WorldMapZoneBuilder.cs` | Builds the 80×25 zone with embedded 20×20 worldmap |
| `Assets/Scripts/Gameplay/World/Map/WorldMapTraversal.cs` | Ascend / Descend helpers |
| `Assets/Scripts/Gameplay/World/Map/WorldMapPart.cs` | Part on player tracking LastZoneIDOnSurface |
| `Assets/Scripts/Gameplay/World/Map/WorldMapTravelCostPart.cs` | Time-burn on worldmap step |
| `Assets/Scripts/Scenarios/Custom/WorldMapShowcase.cs` | Manual playtest |
| 6 test fixtures matching the sub-milestones | RED→GREEN coverage |

### Modified files
| Path | Change |
|---|---|
| `Assets/Scripts/Gameplay/World/Map/WorldMap.cs` | + `Visited[20,20]`, +offset constants, +translation helpers |
| `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs` | + WorldMap routing case + `CreateWorldMapPipeline` |
| `Assets/Scripts/Presentation/Input/InputHandler.cs` | + `<` / `>` input bindings → WorldMapTraversal calls |
| `Assets/Scripts/Gameplay/Save/SaveSystem.cs` | + Visited[,] serialization in SaveWorldMap/LoadWorldMap |
| `Assets/Scripts/Shared/Utilities/Diag.cs` | + `worldmap` in DefaultOnCategories |

---

## Reusable utilities (don't reinvent)

| Utility | Path | Used for |
|---|---|---|
| `MovementSystem.TryMove` | `Turns/MovementSystem.cs` | Player movement on worldmap zone (just works) |
| `ZoneTransitionSystem.TransitionPlayerVertical` | `World/ZoneTransitionSystem.cs:317-404` | Pattern to mirror for ascend/descend |
| `ZoneManager.GetZone` | `World/Map/ZoneManager.cs:34-86` | Cache the worldmap zone |
| `OverworldZoneManager.GetPipelineForZone` | `World/Map/OverworldZoneManager.cs:26-74` | Add WorldMap routing case |
| `WorldMap.FromZoneID` | `World/Map/WorldMap.cs:90-120` | Parse `Overworld.X.Y.Z` → coords |
| `Entity.AddPart` + `Entity.GetPart<T>` | `Core/Entity.cs` | Attach WorldMapPart to player |
| `SaveSystem.SaveWorldMap` | `Save/SaveSystem.cs:881-913` | Extend with Visited[,] |
| `TurnManager.AdvanceTurn` (or equivalent) | `Turns/TurnManager.cs` | Burn turns on worldmap step |

---

## Self-review pre-flagged 🟡 findings

- **🟡 Travel cost units** — `WORLDMAP_STEP_TURNS = 10` is a rough guess.
  Real value depends on what "10 turns" feels like in CoO's existing turn
  cadence. May need playtest tuning. Defer parameterization to WM.7
  showcase.
- **🟡 HP regen during travel** — Qud regenerates HP on worldmap steps
  but the exact ratio isn't documented in our research. WM.4 starts with
  `WORLDMAP_HP_REGEN_PER_STEP = 5` placeholder; may flip to "no regen, just
  time burn" if it feels too easy.
- **🟡 Visited flag scope** — currently per-`WorldMap` (i.e. per-save).
  If we ever support multiple worlds per character, this won't compose.
  Not blocking; flag for revisit at multi-world time.
- **🟡 Worldmap-zone borders** — embedding 20×20 in 80×25 leaves big
  impassable strips. Aesthetically reasonable (room for HUD/legend), but
  if the worldmap grows past 20×20 the embed math changes. Document
  WORLDMAP_X_OFFSET / WORLDMAP_Y_OFFSET as the swap point.
- **🔵 Player marker glyph** — uses the player's existing
  `RenderPart.RenderString` on the worldmap zone. Should be `@` per
  CoO convention; verify in WM.2.
- **🔵 Save back-compat** — old saves don't have `Visited[,]` field.
  WM.5 must tolerate the missing field and default-init.
- **⚪ Tepui biome** — `BiomeType.Tepui` is parallel work (W.2). This
  branch does NOT add it. If both branches land, the worldmap renderer
  will need a Tepui glyph mapping. Track for follow-on merge.

---

## Verification (post-implementation)

### Three layers, in order:

1. **Per-fixture RED → GREEN cycles** during each sub-milestone:
   - WM.2: 8 tests
   - WM.3: 10 tests
   - WM.4: 6 tests
   - WM.5: 8 tests
   - WM.6: 5 tests
   - WM.7: 5 tests
   - **Total: ~42 new tests**

2. **Combined regression sweep** at end of WM.7:
   ```
   run_tests EditMode group_names=[
     "WorldMapTests", "WorldMapZoneBuilderTests",
     "WorldMapTraversalTests", "WorldMapTravelCostTests",
     "WorldMapVisitedTests", "WorldMapPOIRenderingTests",
     "SaveGraphRoundTripTests", "ZoneTransitionSystemTests",
     "OverworldZoneManagerTests", "InputHandlerTests"
   ]
   ```
   Expected: 200+ tests pass.

3. **Manual playtest** via showcase scenario (WM.7):
   - Click `Caves Of Ooo / Scenarios / World / World Map Showcase`
   - Press `<` to ascend → arrive on worldmap zone at center
   - Walk around (WASD) — visited cells light up
   - Step on a POI cell — see the marker glyph
   - Press `>` to descend → return to ground zone
   - Walk back to where you ascended from — should be exact cell

### Honesty bounds
- The travel-cost-feel is **visual / playtest-only** — can't pin
  "10 turns feels right" in a test.
- The fog-of-war rendering is **visual** — test pins data state
  (`Visited[x,y] == true`) but rendering color/dim is a Unity scene
  detail.

---

## Implementation sequence

```
1. Create branch feat/world-map-ui from main           ✓ done in WM.1
2. Write Docs/WORLD-MAP-UI-PLAN.md                     ← this commit
3. Commit WM.1
4. WM.2 — Worldmap-zone routing + builder + 8 tests
5. WM.3 — Ascend + Descend + WorldMapPart + 10 tests
6. WM.4 — Travel time cost + 6 tests
7. WM.5 — Visited[,] fog of war + 8 tests
8. WM.6 — POI markers + legend + 5 tests
9. WM.7 — Observability + scenario + final sweep
10. Combined regression: 200+ tests
11. Merge to main + push
```

Expected total: ~600 lines of new code + ~700 lines of tests +
~100 lines of doc. ~6-8 agent-hours of focused work.

---

## What gets observable to the player after this ship

| Action | Today | After WM |
|---|---|---|
| Press `<` on a ground zone | (no binding, nothing happens) | Transition to worldmap zone at current world coords |
| Press `>` on a worldmap cell | (no binding, nothing happens) | Transition back to ground zone at last surface location |
| Move on worldmap zone | (zone doesn't exist) | Walk cell-to-cell, 10 turns per step |
| Visit a worldmap cell | (no tracking) | Cell marked Visited, persists across save/load |
| See unvisited worldmap area | (no view) | Dimmed fog-of-war glyphs |
| See a POI on worldmap | (no view) | Marker glyph + name on examine |

After this ship, the player can mentally map their journey: "I came
from Kyakukya (10,10), walked to a desert village at (12,8), and now
I want to head south to the jungle ruins at (10,15)." The worldmap
makes the 20×20 grid a navigable game space, not just an
implementation detail of zone routing.
