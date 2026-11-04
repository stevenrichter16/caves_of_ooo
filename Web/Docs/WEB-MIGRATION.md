# Standalone web migration

The browser edition runs the existing C# simulation, independently of Unity. Continue moving the native player-facing workflows into the phone interface. The user explicitly permits breaking earlier web-save compatibility; do not spend this phase on historical save conversion. New checkpoints must still save and restore correctly.

## Current release: world travel and cartography

Status: implemented and verified in release `1c2547bf70c6e172`.

Goal: leave the opening area using the native world map, identify the region and disclosed landmarks, descend into a selected area, and return with the same character and persistent world state. Preserve the published portrait/landscape controls and single-action touch behavior.

Scope:

1. Expose actual world-map entry/descent through a travel command and native stair fallback. Existing transition, turn and checkpoint paths remain authoritative.
2. Export explicit map mode, logical world coordinates, biome, local depth, disclosed map-cell labels and genuine visitation. Surface/cave names use authored places and biome context.
3. Render the embedded 20×20 map using approved native regional atlas backgrounds and disclosed symbols. Keep input coordinates in native zone space. Frame the map independently from local exploration, preserving local zoom when returning.
4. Make World map and Enter area reachable from the phone controls, desktop menu and keyboard. Reading maps/landmarks remains free; moving/travelling advances native time.
5. Verify real core contracts, actual WASM touch gameplay, map offset targeting, travel checkpoints and landscape regressions; publish to the existing private Site and update the existing GitHub branch.

## Source verification before implementation

| Reference | Verified behavior / correction |
| --- | --- |
| `WorldMapTraversal.TryWorldMapVertical` | Surface ascent and world-map descent; underground ascent requires stairs. Direct `Ascend` is more permissive, so use the guarded entry point. |
| Native `InputHandler.TryUseStairs` | Try ordinary stairs first, then world-map fallback; transition and end the player turn once. No hostile guard applies to this fallback. |
| `WorldMapTravelCostPart` | Successful map movement adds 10 clock ticks through `AfterMove`, in addition to the ordinary turn. The bridge must not add them again. |
| `WorldMap` | Logical 20×20 grid starts at native zone offset **(30,3)**, not (30,2). |
| `WorldMapZoneBuilder` | Live map-cell render names/glyphs hide undiscovered sinkholes. Cached zones are not proof of visitation. |
| `FlatPixelWorldMapCatalog` | Reuses six existing 16×24 atlases, darkened per biome, with native foreground symbols. Copy original assets, do not redraw them. |
| Existing `BrowserGame.EnterZone` | Already applies the native arrival hooks and actor registration; reuse it. Existing JS saves automatically on zone transitions. |

Readiness: simulation and authored map/art are present; browser routing, projection and presentation are missing. This is a port of existing Caves of Ooo behavior, with an HTML/canvas presentation; it makes no Qud parity claim.

## Performance and observability

Only project the 400 meaningful map cells while in world-map mode; avoid serializing its surrounding 1,600 border cells. Resolve the lower map-cell owner while scanning occupants, so the player sprite cannot hide its destination label. Keep images cached and redraw on snapshots, resize, zoom and pan; do not add an animation loop. Native worldmap traversal/movement emits its existing diagnostic records. Bridge refusals return a visible error and do not advance time.

## Verification and review record

- Native tests: original 27 pass; ten new contracts first failed RED against the old bridge, then all 37 pass. The first GREEN attempt exposed border-wall attacks consuming a turn. A logical map-bounds guard fixes all four borders. Rejections, hidden sinkhole names/color, read-only inspection/journey and fresh map restoration pass.
- Browser tests: final 49 real-WASM travel checks pass in Chromium/WebKit at both phone orientations, including all 400 cell-center hit tests and 568×320 landscape. An additional overlapping WebKit run confirms short-window aim/cancel preserves free time and cell access. Existing 50 browser regressions, 63 landscape checks and 30 storage/coordinator checks pass. Receipts are in `Verification/evidence/world-*`.
- Independent bridge review: no remaining actionable travel/disclosure/persistence defects found. Native stair auto-walk and Helmwood passage handling remain broader migration gaps.
- Unity editor/player validation is intentionally excluded: the user requested a standalone website game.

### In-phase review and divergences

- 🟡 Fixed: the native world-map zone is surrounded by ordinary Wall objects. The browser now rejects moves beyond the logical 20×20 map before the generic structure-attack path. This is a deliberate boundary correction, not a new travel rule or extra time charge.
- 🟡 Fixed from actual browser screenshot review: the portrait zoom toolbar could cover northeastern cells despite the whole map fitting geometrically. Map framing now reserves the toolbar's actual occupied band. A full 400-cell hit-test assertion was added after this visual finding; it was not a pre-implementation RED test.
- 🔵 Presentation extension: fitted overview, separate local/map zoom, HTML place guide and visited corner marks. Native authored textures, disclosed symbols, traversal and discovery semantics remain authoritative.
- 🧪 Physical iPhone performance/lifecycle and every regional gameplay interaction remain outside emulated browser evidence. No historical web-save conversion was added or required.

The five new image files are unmodified original blobs from `Assets/Resources/Sprites/Live16x24/` at source revision `e5a782a2e8d91354798b917e3aceed36b4c5f42f`; the existing Sill atlas supplies the sixth region. Their combined additional PNG payload is under 15 KB. Local regional terrain coverage is still separate work.

## Remaining migration gaps, source-audited

- Mode-specific ability targeting/preview, especially off-axis direction-line selection and Vault neutral-target confirmation; throw ground/body choice.
- Explicit equipment body-slot selection, displacement review and comparison.
- Ability details and ten-slot reassignment (all current abilities already activate by ID).
- Hauling feedback: the core can grab/release, but propagation can be mistaken for failure; show current load and speed penalty.
- Crafting-station `CraftToggle`/`CraftNoop` rows need browser routing; batch forge/brew presentation is incomplete.
- Status/hazard/lighting projection and regional local-terrain art coverage. Do not paint every region as Sill or invent hazard-free ground.
- Original campfire/harvested-resource and some cellar asset lookup states need corrections.

Existing single crafting, throwing, ordinary equipment and skills are already in the web edition. No native firearm system was found; inventing one is outside migration scope.
