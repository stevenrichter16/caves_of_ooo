# Felling-Site staged integration — historical handoff

**Superseded on 2026-09-06 by the [native Unity integration](../../../Docs/FELLING-UNITY-SCENE-INTEGRATION.md).** The game now has a dedicated definition, persistent simulation owners, presenter, shader and importer under `Assets`. `physical-layout.json` beside this document is the current physical authoring input. Do not install the historical `.cs.txt` importer or copy the offline core over the native implementation.

The remainder preserves the earlier offline handoff and its then-unrun verification matrix. Its proposed APIs and world-route observations are historical; use the current plan and `Docs/Verification/FellingScene` for actual implementation evidence.

The selected art/layout and overall delivery plan are in `../layout.json` and `../../../Docs/FELLING-SCENE-BUILD-PLAN.md` (repository-root path: `Docs/FELLING-SCENE-BUILD-PLAN.md`). Inspect the then-current renderer before applying this handoff: unrelated work was present and continued during preparation.

## Executable offline core

`Core/FellingSceneCore.cs` has no `UnityEngine` or editor dependencies and uses C# 7.3-compatible source. The console harness targets the locally installed .NET 10 SDK, not a claim about Unity's managed runtime. `Tests/NuGet.Config` clears remote feeds; there are no package references.

From repository root:

```sh
dotnet run --project ArtSource/FellingSite/Integration/Tests/FellingScene.Core.Tests.csproj
```

The initial compileable stub produced 17 failing tests and exit 1, recorded in `Validation/red.log`. The implementation produced 17 passing tests and exit 0 in `Validation/green.log`. Tests include all 1200 logical cell centers, half-open and nonfinite boundaries, exact art/world corners, blocked endpoints, cardinal/diagonal paths, both corner blockers, copied occupancy, sample-level fog, explicit overhang ownership, duplicate/invalid owners, and invalidation across shared footprints.

| API | Contract |
|---|---|
| `SceneCoordinates.TryImageToCell(point, out cell)` | Maps image samples in x[0,1536), y[224,1024) to zone x[16,64), y[0,25). Rejects upper overhang and outside samples. |
| `ImageToWorld` / `WorldToImage` | Finite affine transforms, including edge vertices outside the half-open sample domain. Full art bounds are world(16,0)..(64,32). |
| `CellCenterInImage` / `CellCenterInWorld` | Validates embedded region; world center follows the game's inverted Y convention. |
| `SceneOccupancy(walkableCells)` | Immutable occupancy snapshot; unlisted cells are blocked. Does not replace live game physics. |
| `TryFindPath(from,to,allowDiagonal,out path)` | Deterministic breadth-first path including endpoints. Diagonals require both adjacent cardinal cells walkable. All steps cost equally; live terrain/ability costs remain the game's responsibility. |
| `SceneVisibility.ResolveGround(pixel)` | Uses visibility of the single mapped cell. Unowned overhang is hidden. |
| `ResolveOwned(pixel, owner, footprintCell)` | Samples exactly one explicitly authored footprint cell, validated as part of that owner. Supports overhang. The renderer must first establish that this pixel belongs to this owner's mask. |
| `SceneryOwner(id,cells)` | Stable ID and a copied, sorted, deduplicated footprint. Empty/out-of-region owners rejected. |
| `OwnerInvalidationIndex.ForCell(cell)` | Returns every owner directly sharing that cell and the union of **all** their footprint cells. Refresh every visual patch for those IDs, including overhang. Ordinary ground at the changed cell is refreshed separately. |
| `ForOwner(id)` | Returns complete old coverage before an owner is removed/moved. Rebuild the immutable index afterward and invalidate new coverage as well. Unknown IDs throw rather than leave stale scenery silently. |

Ownership does not imply that an entire tree becomes visible when any footprint cell is visible. Every visual patch/sample needs one exact cell owner. A texture mask or subdivided mesh can implement this; no such GPU mask has been produced here. Core visibility returns only `Unexplored`, `Remembered`, `Visible`; the adapter applies current game tint/fog rules and suppresses dynamic entities/effects in remembered areas.

## Import installation

1. Copy the core into a suitable runtime assembly under `Assets/Scripts/Presentation/SceneArt/FellingSite/` when beginning Unity integration. Use alias `SceneCell = CavesOfOoo.FellingScene.Cell` where also importing `CavesOfOoo.Core`; both namespaces contain a `Cell` type.
2. Copy the staged `Unity/FellingSceneArtImporter.cs.txt` to `Assets/Editor/FellingSceneArtImporter.cs`. Compile before importing artwork.
3. Approved single-sprite art goes into **`Assets/Resources/SceneArt/FellingSite/Art/`**. This path avoids `SpriteImportPostprocessor.SpriteRoot`, which forces 16 PPU beneath `Assets/Resources/Sprites/` (`Assets/Editor/SpriteImportPostprocessor.cs:26`, `:34`). Preserve reference at 1536×1024 and 32 PPU. Do not copy generated outputs into the 16 PPU path.
4. Put ownership/data textures outside `Art/`; they need a separate linear, non-sRGB data-texture policy if implemented. The staged importer deliberately handles only color artwork and treats each PNG as a single sprite. Packed character sheets stay in their existing actor pipeline.
5. Staged settings are 32 PPU, point, no mips/compression, clamp, NPOT unchanged, full rectangle, bottom-left pivot, sRGB color, max 2048, and no generated physics shape. It clears four known platform overrides. Verify every target platform's actual imported settings and size in Unity.
6. Place a full-canvas bottom-left-pivot sprite at world(16,0,0). For a cropped source rectangle `(left,top,width,height)`, its bottom-left world position is `ImageToWorld(left,top+height)`. Do not change simulation scale or stretch the 1536×1024 plate to 80×25.

## Proposed runtime adapters — to implement and test

These are design contracts, not existing project APIs:

```text
FellingScenePresenter.Bind(Zone zone, FellingSceneDefinition definition,
                          Transform worldParent, ISceneVisibilitySource visibility)
FellingScenePresenter.ClaimsCell(int zoneX, int zoneY)
FellingScenePresenter.RefreshCells(IEnumerable<SceneCell> changedCells)
FellingScenePresenter.RefreshOwner(string stableOwnerId)
FellingScenePresenter.SetPresentationVisible(bool enabled)
FellingScenePresenter.Unbind()

FellingSceneDefinition: reference/plate ID and hash; 32 PPU transform;
  art bounds; source rectangles; footprint ownership; source-space coverage;
  per-patch footprint cell; foot/depth anchors; immutable/removable state;
  repair-underlay ID; effect masks; camera profile; stable landmark IDs.
```

Keep exported schema translation in one adapter. Validate schema version, artwork dimensions/hash, IDs, references, polygon bounds, source-rectangle bounds, and every footprint cell before allocating Unity objects. Fail closed with a visible diagnostic and keep glyph fallback available. Do not infer a missing mask or an ownership cell from the nearest visible ground cell.

### Zone/scenario and physics

Start with a manually selected showcase scenario rather than changing production world routing. Existing scenario entry shape is `Assets/Scripts/Scenarios/Custom/EmptyStartingZone.cs:15`; terrain assembly must use the actual `EntityFactory`/zone builder APIs after inspecting their current signatures. Do not place colliders only in Unity while simulation entities remain walkable.

The world coordinate(3,5) is reserved, biome Stump/tier 5 (`Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs:60`, `:95`, `:204`) and base-slope (`Assets/Scripts/Gameplay/World/Map/StumpBands.cs:44`). It currently routes to the generic Stump pipeline (`Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:130`, `:560`). A later explicit Felling-Site route must override only this destination, not all Stump zones, and respect then-current narrative/POI ownership.

Use existing blueprint names after validating inheritance: `TepuiStone` at `Assets/Resources/Content/Blueprints/Objects.json:30146`; `TepuiWall` at 30170; `GrainRidge` at 30194; `WaterPuddle` at 17867. `GrainRidge` is solid low terrain without Wall inheritance. `TepuiWall` inherits Wall and its 40 HP destruction (`Objects.json:994`), so the monumental trunk needs explicit immutability or a dedicated physical definition. `WaterPuddle` is a traversable liquid pool by default; the artwork cannot decide depth/drowning rules. `Grass` at 4528 carries Plantable; never use that unchanged for the six barren positions.

The six marks and seventh discontinuity are authored landmarks only. Their visibility, paths, and count can be tested; no new endgame interaction or ability is implied. Reserve scene cells against later procedural builders/population that could block approach or grow plants in the six.

### Rendering, fog and sorting

Current order is documented beside the active setup in `Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs` at the `Stable order` comment: −1 background, 0 ASCII, 1 fine water, 2 animated environment, 3 environment sprites, 4 tile state, 5 actor shadow, 6 actor body, 7 cursor, 8 FX, 20/21 popups. `AnimatedEntityRenderer.cs:14` pins 5/6. `EnvironmentSpriteRenderer.LoadMacroAtlas` hardcodes 16×16 slices and 16 PPU; bypass it for hero art.

- Begin authored ground at the environment order 3, keep residue/state overlays 4 and actors 5/6. Coordinate environment claims so old floor sprites, glyphs, and background boxes do not double-render. Inspect current `PostRender`, `ClaimCell`, `ReleaseAllClaims`, and dirty rendering paths before adding the claim gate.
- Front root patches require foot anchors and depth relative to actor feet; do not put every root above every actor. A possible path is to share body order 6 and explicit z-depth under the existing sort axis, retaining 5 for shadows and 7/8 for cursor/FX. Confirm actual Unity transparency sort configuration before committing to that path. Another valid path is explicit per-patch depth buckets; neither is implemented here.
- Background-only upper trunk patches must still honor ownership/fog. Store visibility state from `zone.GetCell(x,y).Explored/IsVisible`; `FieldOfView.cs:95` uses Wall-tag opacity (`Cell.cs:135`), while solidity/passability are separate (`Cell.cs:123`, `:210`). Retain live FOV; the current radius 999 does not remove occlusion.
- For removal, capture old owner's invalidation first, restore its approved repair underlay, remove every associated patch/effect, and refresh every affected cell plus new coverage. A baked object cannot truly disappear until its pixels have a coherent underlay.
- Use the plate's already shaded colors as initial visual baseline. Measure the actual selected material and ambient tint; do not add arbitrary lights until comparison demonstrates the need. Fog tint and darkness must stay semantically correct even if the art uses an unlit material.

### Camera and input

Exact reference camera: world center(40,16), orthographic half-height 16, game viewport aspect 1.5. Current `CameraFollow.TargetVisibleTileRows` is 34 (`CameraFollow.cs:27`); its clamp at 299–306 centers a taller view at zone Y 12.5, clipping the top 80 source pixels of this composition. A scene camera profile needs **art** bounds and a tested return to ordinary follow behavior.

Calculate framing from the actual HUD-excluded world viewport. Letterbox or crop deliberately at other aspects, and show the intended behavior in a comparison capture. `ZoneRenderer.ScreenToZoneCell` at 1596 uses WorldToCell and reversed Y; its live equivalent remains the authoritative picking path. Reject input outside the embedded region or in visual overhang even when the artwork is visible there. For the browser/offline mapping, use `TryImageToCell`, not clamped coordinates.

Actor feet are anchored at `(x+.5,24-zoneY)` (`AnimatedEntityRenderer.cs:497`), with 16 PPU pixel snapping (`:510`). Existing 16×24 actors therefore render 32×48 art pixels at the scene density, close to the selected traveler. Distinguish a cell's center from an actor's bottom/foot anchor.

### Lifecycle and fallback

Bind on matching zone entry only. Subscribe to existing cell/FOV/owner update hooks once; unbind before a new zone and on destruction. Restore claims, camera profile, and render toggles; release owned GameObjects/materials/meshes/textures; never destroy shared Resources assets. Hide on inventory/modal presentation paths as required by the current render lifecycle. On save/load or zone re-entry rebuild all transient rendering from current simulation state and stable layout IDs. Do not persist Unity object references or animation clocks.

Sprite-mode off must release scene claims and restore ordinary terrain/glyphs. Reduced motion stops visual water/mote/sway animation without changing cell state. A missing plate/invalid manifest must leave a playable fallback instead of black cells or stale giant scenery.

## Unity verification still required

| Test family | Required positive and negative cases |
|---|---|
| Import/EditMode | Full canvas remains 1536×1024 and 32 PPU; proper pivot/filter/NPOT/compression on actual targets; unrelated 16 PPU assets retain settings; data textures are not color sprites. |
| Generation/EditMode | Scene only at explicit route; all six approachable; no accidental seventh mark; barren cells never Plantable; outside terrain not clobbered; root solidity and opacity differ as authored. |
| Mapping/PlayMode | World-to-screen-to-cell round trip at corners/centers; HUD and upper overhang clicks rejected; same result at 1.5 and actual window aspect. |
| Fog/PlayMode | Unexplored samples hidden; remembered terrain dim; actors/FX hidden when remembered; one visible root footprint cell does not expose all its pixels; above-zone ownership obeys owner cells. |
| Depth/PlayMode | Actor passes behind and in front of every root; feet retain alignment; cursor/FX remain visible; state overlays do not disappear beneath ground. |
| Invalidation/PlayMode | Destroy/remove/move one multi-cell prop; refresh all old/new coverage and patches; unaffected props unchanged; underlay correct; event handlers do not multiply after re-entry. |
| Lifecycle/PlayMode | Leave/re-enter, save/load, sprite-toggle, reduced motion, pause/modals, missing asset fallback; camera restored on ordinary zone. |
| Visual/performance | Deterministic 1536×1024 game-only capture and HUD view; compare silhouette, six centers, small traveler scale, palette, blackwater path; measure drawcalls/memory/frame time against a real baseline. |

These tests require Unity and remain unrun. Passing the offline core proves coordinate/path/ownership behavior in that core, not the future adapters, imported sprites, shader masks, gameplay rendering, or performance.
