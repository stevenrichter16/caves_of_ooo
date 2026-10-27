# Ordinary shallow caves — materials, obstacles and return routes

2026-10-27. **The corrected backdrop and six-owner cave integration now have
bounded ordinary Editor evidence: matched entry, readers, surface return,
real wall destruction/wreckage, two cave columns, depth2 round trip and fresh
Continue.** The unchanged native total is 395 distinct passes: 56 focused +
339 rerun related cases, zero failures/skips/inconclusive. The final earned
fixture85 is depth2 at turn11910 with HP33; a fresh isolated Continue restores
that state. Root's final protection check passed for both normal save locations
and all44 unrelated tracked baseline paths. Isolated Play is finished and the
scene is clean; current-source packaging remains pending. This does not accept whole-cave
polish or all underground coverage.

The first actual AFTER exposed a release-blocking presentation
defect: black backgrounds behind empty cells and transparent rubble read as
pits. The earlier 383-case checkpoint did not accept that view. The separate
backdrop plan is retained in §10 and its strict RED→GREEN evidence in §11.
The up-stair reader is verified in actual AFTER03; §12 records the corrected
replay and its exact limits.
See §9 for the retained RED, one corrected fixture premise,
source checks and explicit test-first timing deviation. Whole-cave polish is
not accepted.

Latest actual hourly report:17:27:22UTC; next report:18:27:22UTC. These are
communication checkpoints, not additional gameplay or package evidence.

**Historical BEFORE checkpoint:** source preflight complete; the first ordinary Editor BEFORE route
reached depth1 and ended in a fatal encounter. Depth2 and return were unverified.
These captures are isolated **Unity Editor Play**, using runtime source
`4dbdc9be9` and delivery commit `83b455e4140ad2fd7f7391402b2d33dad7ac56dc`, which
root verified on origin/main. The Editor loaded the earned
`packaged-grovelands-actors82` fixture; this is not a standalone cave-package run.
The living entry83 save remains preserved. A first imagegen scene study is
running at this checkpoint, with no output yet reviewed. Final art decisions,
native source production, genuine native RED, runtime implementation and AFTER
acceptance remain pending.

The finite scope is six exact owners in ordinary, non-roomed sandstone caves
at depths 1–2: **SandstoneFloor, SandstoneWall, Rubble, Stalagmite, StairsDown and
StairsUp**. Cover their existing functions together, without adding population,
changing cave generation, or introducing optional animation. This is original
Caves of Ooo art; no copied Qud assets or new Qud-parity claim is proposed.

The user prioritizes missing graphics across all biomes and areas, followed by
a separate serious polish phase. Whole-chunk readability and fullness remain
requirements: six mapped owners or a passing test count cannot accept a cave
band. The [next-gap audit](Verification/Live16x24/post-grovelands-next-gap-plan.md)
motivates this slice; the current
[world coverage ledger](../ArtSource/Live16x24/world-sprite-coverage.json)
still records ordinary underground material/scene/fullness acceptance as open.

## 1. Verification sweep and corrected premises

Current references below were read before proposing production. Blueprint
inheritance was resolved from `Objects.json`; defaults were checked against
`PhysicsPart` and factory construction. The first actual depth1 census confirms
the literal parts on the observed owners. No production or test code was run
by this preflight.

| Premise | Verified source and consequence |
| --- | --- |
| `Cave` is another authored surface country. | [WorldMapAuthoring.cs:51](../Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs) uses six surface biome letters. This work enters an actual stair from an existing surface graph, rather than fabricating a Cave surface zone. |
| Every depths 1/2 sandstone graph is ordinary. | [OverworldZoneManager.cs:95](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs) dispatches pilot/lair/sinkhole/Root/cellar cases before the ordinary depth branch. Matching depth or material does not grant native cave admission. |
| Shallow sandstone can be roomed. | [SolidEarthBuilder.cs:42](../Assets/Scripts/Gameplay/World/Generation/Builders/SolidEarthBuilder.cs) chooses sandstone for depths 1–2. [UndergroundLayoutPlan.cs:35](../Assets/Scripts/Gameplay/World/Generation/UndergroundLayoutPlan.cs) returns a natural `shallow` layout below depth 3. The25% roomed choice applies only at depth 3+ in eligible ordinary Spread/Sodden/Beating columns. |
| The selector itself proves current rendering authority. | `UndergroundLayoutPlan.Select` is a cold-generation choice, not a current-graph or saved-lair check. Do not call it per cell. [WorldLocationContext.cs:13](../Assets/Scripts/Gameplay/World/WorldLocationContext.cs) provides current attached authority; [LairStacks.cs:203](../Assets/Scripts/Gameplay/World/Generation/LairStacks.cs) is a read-only saved-column exclusion. It requires the canonical **surface** ID, not the depth ID. |
| All six stone-looking owners have Stone material or health. | Only SandstoneWall owns Material/Thermal/Destructible. The other five have no authored MaterialPart, ThermalPart, DestructiblePart or Hitpoints stat. [EntityFactory.cs:205](../Assets/Scripts/Data/Factories/EntityFactory.cs) constructs actual merged stats/parts; it does not infer material from a display name. The census's `hp:-1` is a missing-stat sentinel, including walls, not a dead owner or negative structural HP. |
| Empty cave cells already contain a floor. | [StrataBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs) rolls 85% sandstone floor, 10% rubble and 5% neither on carved cells. Later connectivity/stair/landmark placement can change this. Never invent an invisible sandstone owner to fill a screenshot. |
| CaveBat/CaveSlime are the shallow ordinary encounter. | [PopulationTable.cs:1102](../Assets/Scripts/Data/Tables/PopulationTable.cs) uses a weighted MarlbackScrabbler/Gleaner/Tunnelguard group, Stalagmite 2–6 and optional Glowmaw at tier1. Other tables contain the bats/slimes. This phase changes none of them. |
| Every surface has a usable entrance. | [CaveEntranceBuilder.cs:27](../Assets/Scripts/Gameplay/World/Generation/Builders/CaveEntranceBuilder.cs) has a 50% attempt roll, bounded placement and an optional placement filter. A historic coordinate is a route candidate until the current physical owner is confirmed. |
| A stair stores an arbitrary destination, or autowalk guarantees disclosure. | `StairsDownPart`/`StairsUpPart` are marker-only parts. [ZoneTransitionSystem.cs:371](../Assets/Scripts/Gameplay/World/ZoneTransitionSystem.cs) chooses the same world x/y column at z±1 and finds a usable counterpart. Connections support endpoint generation. [StairTravel.cs:21](../Assets/Scripts/Gameplay/World/StairTravel.cs) searches reachable physical stairs without an explicit explored/visible filter; its hostile interruption is separate. The previous audit's word “disclosed” overstated that helper. Preserve current mechanics; new render queries must still enforce disclosure. |
| No POI now means the cached graph was never authored. | Map POIs are mutable. Null current POI plus a material match cannot recover past sinkhole/Root provenance. Keep explicit registered authored-column exclusions and exact saved-lair checks; do not claim provenance that the source does not store. |

## 2. Exact owner and state contract

All six inherit owned Render, Physics and Examinable. `PhysicsPart` defaults to
non-takeable, non-solid, no inventory/equipment custody; only the two solid
owners override solidity. These are world objects, not portable item identities.
None has creature Hitpoints, Body or Brain. Structural health belongs to the
wall's **DestructiblePart**.

| Exact blueprint / definition | Render and physical meaning | Material, health and essential outcome |
| --- | --- | --- |
| [SandstoneFloor:8761](../Assets/Resources/Content/Blueprints/Objects.json) | `.`; `GlyphVariants` is `...,.'` followed by a backtick; `&W`, layer 0, Terrain, passable. | No Material/Thermal/Destructible or stats. Granular worn stone, not necessarily constructed paving. Native ground may replace only a real valid floor. |
| [SandstoneWall:7895](../Assets/Resources/Content/Blueprints/Objects.json) | `#`, `&W`, background`^w`, layer 0; Wall+Solid tags and solid Physics. | Stone; tags Stone,Mineral; combustibility 0; Thermal heat capacity 3 and flame temperature 1200. Destructible HP30 / MaxHP30, hardness 2, wreckage`Rubble`. Partial damage, destruction and exposed neighbors must remain truthful. No HP-stat test. |
| [Rubble:3489](../Assets/Resources/Content/Blueprints/Objects.json) | `,`, `&y`, layer 0, Terrain, passable. | No Material/Thermal/health/harvest part. Naturally placed loose stone and wall wreckage share the same blueprint. It must not look like a solid ledge, ore or collectible. |
| [Stalagmite:3523](../Assets/Resources/Content/Blueprints/Objects.json) | `^`, `&y`, layer 1; Solid tag and solid Physics, no Wall or Terrain tag. | No Material/Thermal/health/harvest part. A compact blocking stone formation; do not add breakability, mining yield or a new footprint. |
| [StairsDown:8989](../Assets/Resources/Content/Blueprints/Objects.json) | `>`, `&W`, layer 0, Terrain+StairsDown, passable; owned StairsDownPart. | No Material/Thermal/health. Ordinary destination is the same world x/y at depth+1, subject to actual generation/counterpart/arrival checks. No invented trapdoor, lock or service. |
| [StairsUp:9032](../Assets/Resources/Content/Blueprints/Objects.json) | `<`, `&W`, layer 0, Terrain+StairsUp, passable; owned StairsUpPart. | Same absence of inferred material/health. Destination is the same world x/y at depth−1. A removed counterpart must not become an apparent valid one-way exit. |

`DestructionSystem.Damage` subtracts hardness with at least one applied damage;
successful destruction removes the wall, places real Rubble through the current
factory and marks the cell dirty ([DestructionSystem.cs:90](../Assets/Scripts/Gameplay/World/DestructionSystem.cs)).
The adapter must observe that lifecycle, not generate wreckage or simulate a
damage step. Choose a partial-damage art policy before RED: either the same
truthful surviving rock silhouette with the actual structural reader, or an
explicit fallback/state variant. Do not silently require pristine 30 HP and then
claim every damage state is native.

## 3. Ordinary area and travel authority

Proposed admission is a conservative **current ordinary column** contract:

1. Require a canonical `Overworld.x.y.z`, in-bounds x/y and z exactly 1 or 2.
   Require attached `OverworldZoneManager`, intact map dimensions and exact
   `CachedZones[id]` reference equality. The fallback catalog permits unattached
   synthetic graphs; that is not sufficient geographic authority for this kit.
2. Reject current POIs and `LairStacks.HasSavedColumn(manager, surfaceId)`.
   Read that check without initializing a ledger. Reject source-registered
   special columns, including sinkholes/Root, the pilot 3,7 and the authored
   wilderness reservations. Compare their surface IDs; do not compare a z1 ID
   directly against a z0 reservation. The glade 11,10/cellar remains its own kit.
3. Preserve unsupported/specially owned cached graphs. Do not rerun generation,
   add a provenance ledger, change POIs, adopt a lair, or create a world graph to
   make a render query succeed. Exotic edited/legacy provenance cannot be
   reconstructed from six blueprint names; remain explicit about that limit.
4. Native art is for current visible, explored, selected owners in those graphs.
   Remembered/hidden/unexplored cells retain conservative existing fallback.
   Scope is the two underground depths: surface entry art and depth3 limestone
   are boundaries, not automatic scenery admission.

Use the actual existing reservations rather than a hand-maintained guessed
list: [SinkholeSites.IsMouth:53](../Assets/Scripts/Gameplay/World/Map/SinkholeSites.cs)
covers all six authored mouths, including Stillleaf 2,4; RootSiteBuilder's
constants identify 3,3 in [RootSite.cs:18](../Assets/Scripts/Gameplay/World/Generation/Builders/RootSite.cs).
`MultiCellPilotRuntime.ZoneID` is 3,7, and
`OverworldZoneManager.AuthoredWildernessZoneIDs` supplies the three reserved
surface scenes. `WorldGenerator` stamps the mouths and Root from those sources;
the older geography comment listing only five sinkholes is not the registry.

The ordinary pipeline is SolidEarth → Strata → Connectivity → StairsUp/Down →
StairConnector → landmarks/hazards/population/containers
([OverworldZoneManager.cs:580](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs)).
Connectivity and stair connection can carve real passages; population arrives
later. A generated connected layout is not a promise that the current route is
unoccupied or safe.

`StairsUpBuilder` reads registered connections from above; `StairsDownBuilder`
respects surviving counterpart stairs and pending links; neither should be
replayed on a cached graph for art. `StairConnectorBuilder` connects the physical
stairs, adding floor only where its cleared cell is empty. The vertical travel
system validates actual return stairs and usable arrival before transfer. Use
ordinary `>`/`<` controls and existing interruption behavior. Record a failure
honestly if a hazard, actor or missing endpoint prevents the intended route.

## 4. Historical immutable art candidates — final selection in §9

| Candidate | Exact source geometry | Decision still required |
| --- | --- | --- |
| Beating floor | `Sprites/Live16x24/beating-materials`,544×24 atlas, frames12–15, each16×24. | Trial the quiet color and subtle joins against the cave capture. Its existing catalog admits eligible Beating surface addresses only. Borrow pixels without borrowing that region policy. |
| Beating wall | Same atlas, frames16–17. | **Named mismatch:** the inspected contact has built masonry joints. A natural cave needs continuous geological mass and readable exposed edges, not an invented brick ruin. Do not approve this reuse by material-name matching. Original cave rock art is likely needed after imagegen/parts critique; preserve the Beating pixels. |
| Rubble | `Sprites/Environment/rubble`,16×16; alternative opening-base frame6 from the 160×24 atlas. | Existing native base routing intentionally requires `reference-glade-gravel`. Choose a truthful loose-stone silhouette; do not widen that VisualID rule. Transparent scatter must not imply an underlying floor where none exists. |
| Stalagmite | `Sprites/Environment/stalagmite`,16×16. | Existing small pale point is a viable immutable trial candidate, not accepted collision readability. Avoid enlarging its footprint or inventing resource cues. |
| Down stair | `Sprites/Live16x24/opening-anchors`,192×24 atlas, frame4. | Recessed stair opening; verify readability and distinction from the up stair in actual native paint (lighting correction below). |
| Up stair | `Sprites/Live16x24/opening-cellar`,144×24 atlas, frame0. | Borrow the exact cell, not the cellar address contract. Verify the ordinary return route and reader. |

Decode-only inspection confirmed these sizes, binary alpha, Point/Clamp,
16 PPU, no mipmaps and no texture compression. Textures are not CPU-readable;
cached `Sprite.Create` subrects do not require pixel reads. The two square
sources can remain centered 16 PPU FullRect sprites inside the 16×24 grid, with
implicit 4px vertical space rather than stretched/padded PNG copies. Current
resource mappings are documented in
[FlatPixelOpeningArtCatalog.cs:497](../Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs)
and [FlatPixelBeatingMaterialsCatalog.cs:54](../Assets/Scripts/Presentation/Rendering/FlatPixelBeatingMaterialsCatalog.cs).

Do not fix an atlas frame count until actual capture → built-in imagegen →
parts/state critique selects reuse versus new art. New native pixels need
editable exact-cell source, direct native/contact renders, immutable-input
checks and source RED/GREEN. No raster resizing/tracing or change to approved
input art; no optional animation packet. Preserve the actual lighting pipeline:
depth 1 ambient 0.28 and depth 2 ambient 0.22 remain simulation/fallback settings
([OverworldZoneManager.cs:1332](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs),
[BiomeColorPatcher.cs:174](../Assets/Scripts/Presentation/Rendering/BiomeColorPatcher.cs)).
**Native-paint correction before production:**
[ZoneRenderer.FlatPixelPaint.cs:43](../Assets/Scripts/Presentation/Rendering/ZoneRenderer.FlatPixelPaint.cs)
paints `NativeForeground` and `LocalGround` in their authored colors, bypassing
`ApplyFlatLighting`. Do not assume the unchanged native floor or new wall will
be multiplied by depth ambient 0.28/0.22 or warmed/darkened by Cave grading.
Keep this existing paint behavior; judge actual native wall/floor contrast in
the AFTER captures. Visibility and fallback lighting remain separate concerns.

## 5. Historical proposed integration hooks — implemented checkpoint in §9

One finite companion, provisionally `FlatPixelShallowCavesCatalog`, should use
the existing regional pattern: `IsSupportedZone`, a real-floor query, a selected
foreground query, exact-subject reader lookup and disposal. Final resource
constructor/frame contract follows the art decision, not this preflight.

- In [FlatPixelGreyboxCatalog.TryResolvePresentation:791](../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs),
  retain fallback selection first; obtain native ground only from a valid real
  floor and use white tint for authored RGB. Preserve covering actors/items and
  literal selected stairs/rubble. Same-layer Terrain stairs are overlays, not
  permission to ignore all competing Terrain. In an ownerless cell, preserve
  the actual fallback background.
- Use exact owned unique parts, one registered cell, canonical glyphs/layer,
  no takeable/custody/reskin/foreign-service ambiguity. Reject unknown active
  material/thermal/state meanings rather than whitening over them. A floor
  claim must not hide a real liquid, IceSheet, coating, cultivated patch, heat
  mark or competing material. Foreground stone should not require its whole
  cell to have no unrelated floor state.
- Shared `TryGetPresentationSubjectSprite` may consult the companion only after
  current graph/selected-owner validation. The actual document renderer remains
  the authorization boundary ([ExaminePortraitRenderer.cs:50](../Assets/Scripts/Presentation/Rendering/ExaminePortraitRenderer.cs)).
  No extra Render event, damage tick, travel, neighbor simulation or reader
  side effect belongs in a lookup.
- Reuse current dirty-cell/neighbor repaint and cached Sprite/Tile ownership.
  If actual wall readability requires edge variants, join only disclosed,
  selected, validated exact-family neighbors. No unseen joins, world-wide scans
  or per-query `Split`, generation-selector calls or new sprites.
- Independently optional resources must fail locally; explicit injected null
  does not load defaults. Dispose only owned sprite/tile pairs. Keep old local
  opening admissions and all current actor/item catalogs unchanged.

## 6. Actual BEFORE checkpoint — baseline only

Root loaded earned
[packaged fixture82](Verification/Live16x24/Fixtures/packaged-grovelands-actors82/Quick.json)
in isolated ordinary **Unity Editor Play**, then travelled from Grove 4,7
through the world map to GreenRim 0,9. Current surface StairsDown 53825 was
observed at 56,7. Ordinary Shift+Period autowalk descended to **Overworld.0.9.1**,
player 56,7 at turn 10730 with HP23 unchanged. The saved
[entry83 header](Verification/Live16x24/Fixtures/shallow-caves-entry83/Quick.json)
records that living checkpoint. No teleport or new gameplay owner was introduced.
The input fixture came from the earlier Grovelands package; the cave controls
and PNGs in this section were produced in Editor, not a new standalone build.

The first two actual captures are
[surface](Verification/Live16x24/shallow-caves-before-01-surface.png) and
[depth1 entry](Verification/Live16x24/shallow-caves-before-02-depth1-entry.png).
This document's author viewed both: the surface is extremely sparse, while the
cave currently presents a dense dot/comma field and hash-mark walls in dark
grading. That is a BEFORE observation, not a selected new palette or AFTER
readability acceptance. Large dark areas include undisclosed space; do not
decorate them based on a screenshot alone.

The actual [depth1 census](Verification/Live16x24/shallow-caves-before-depth1-census.json)
records these counts at that view:

| Owner | Whole graph | Currently visible |
| --- | ---: | ---: |
| SandstoneFloor | 1,253 | 339 |
| SandstoneWall | 521 | 63 |
| Rubble | 142 | 41 |
| Stalagmite | 2 | 1, at 53,3 |
| StairsUp 72907 | 1 | 1, at 56,7 |
| StairsDown 72908 | 1 | 0, at 40,12 |

Other actual owners remain outside this six-owner art scope: one hidden
MarlbackTunnelguard, one visible OreCache, one visible StrongBox, one hidden
Crate, three hidden SteamVents and one hidden IceSheet. A whole-graph diagnostic
count is not player disclosure. The native floor must not hide those owners or
their states; they remain coverage debt even if this foundation succeeds.

### Interrupted depth1 continuation

After preserving entry83, a second `>` began ordinary stair autowalk but stopped
at 49,14 with “You stop — you are not alone.” Manual movement then encountered
the solid OreCache at 48,17 and sandstone wall at 48,14. Root observed two bumps
against that wall reducing its structural HP30→25. This was partial damage,
not destruction or a native damaged-art test.

The manual route crossed 49,18  → 47,18  → 47,20  → 44,20  → 36,20; the downstairs at 40,12
then became visible. The retained route views are 03–08, including the
[blocking edge](Verification/Live16x24/shallow-caves-before-04-blocking-edge.png)
and [lower chamber](Verification/Live16x24/shallow-caves-before-08-lower-chamber.png).
Visibility of the destination is not successful arrival or descent.

The final batched Right3/Up8/Right1 attempt encountered previously hidden
MarlbackTunnelguard 72913. The retained
[death log](Verification/Live16x24/shallow-caves-before-death.json) records a
22-damage **Shank** at 11110, leaving 1 HP, followed by the player's missed sword
attack and a 1-damage spear hit at 11120 that killed the isolated character. The
attack is named Shank in the actual log; an earlier verbal description of
“shield bash” was inaccurate. The final
[death-interruption capture 09](Verification/Live16x24/shallow-caves-before-09-death-interruption.png)
was originally named `down-stair` and has been renamed to describe its actual
result. It must not count as down-stair arrival, depth2 or return evidence.

**Depth2, successful use of the depth1 down stair, and return were not achieved.**
The preserved entry83 fixture remains the living turn 10730/HP23 checkpoint;
the fatal continuation does not replace that baseline. This is an interrupted
BEFORE route, not new graphical or gameplay acceptance. No combat difficulty,
AI, travel or item state was edited to complete the attempted route.

Source review recommends trying unchanged art for five owner families and
authoring natural rock for the wall; that is a reuse recommendation, not art
approval. Root has started the scene imagegen study, but its output and the
final part/frame contract remain unreviewed at this checkpoint. Historical
backup route 16,13 has a retained seed 65 surface stair at 45,9, but has not been
revisited here; see the [route audit](Verification/Live16x24/post-grovelands-next-gap-plan.md).

## 7. Historical native test plan and remaining actual acceptance gates

Write a finite new fixture after the art/resource contract is fixed. Root must
run genuine native RED before production. Pair each positive with its nearest
countercase, using existing real factory, current manager, submitted painter,
reader and world-state helpers instead of a new test framework:

1. Both ordinary depths and exact six owners; counter depth0/3, wrong material,
   authored/POI/saved-lair column, noncanonical ID, detached/replaced graph and
   changed current authority. Keep regional floor admission separate from
   portable actors.
2. Real layer 0 floor+stairs, floor+rubble and floor+covering actor; counter
   ownerless/competing floor and real hazardous/cultivated state. Assert actual
   submitted ground/foreground and authorized reader identity, not a catalog
   lookup alone. Preserve state values before/after every render query.
3. Real wall damage/destruction→Rubble, collider/passability truth, stale owner
   removal and incremental repaint; counter invalid/foreign Destructible and
   vetoed destruction. Do not use Hitpoints to decide wall survival.
4. Existing normal stair transition and return keep exact actual destination,
   counterpart and cached owners; missing/removed endpoint is a countercase.
   Art must not create or repair connections. Native tests do not replace the
   ordinary keyboard route or certify a safe encounter.
5. Missing/malformed resources independently retain truthful fallbacks;
   source pixels/rect/PPU/lifetime remain exact. Hidden/memory/foreign/reskinned/
   multicell/custody owners do not acquire art or reveal neighbors. Preserve all
   borrowed globals/hooks/factories in teardown.

After GREEN and cold review, replay matched actual depth1 and depth2 views,
collision, stair approach/return, reader and changed-wall outcome when reached
normally, then save and fresh Continue. Review whole visible rooms and narrow
passages, not only close-ups. Explicit debts are natural rock versus masonry,
wall/floor/stalagmite separation, quiet ground without repetitive glyph-like
carpeting, honest darkness/empty cells, return-route visibility, and remaining
containers/fauna/hazards. Do not add arbitrary rubble or props to inflate
fullness. Preserve gameplay route geometry unless a separately recorded actual
composition defect justifies a bounded change and its own RED.

A completed material slice still leaves deeper bands, roomed caves, authored
stacks, other owners and later serious area polish open. Current-source Mac
ordinary smoke/restart and Linux archive verification follow reviewed source;
neither native tests nor Editor profiler samples prove Linux execution,
physical Deck behavior or a performance improvement.

## 8. Historical preflight self-review and continuation

- 🟡 Corrected inferred materials/health, shallow encounter names, cold selector
  authority, surface-keyed lair lookup and overstated stair-disclosure/destination
  assumptions before production. Peer source review confirmed the lair and
  mutable-POI authority cautions; no runtime policy is implemented yet.
- 🧪 Baseline is partial and its continuation ended in death. The imagegen
  scene study is running; output review, new art, source/native RED/GREEN,
  runtime, matched AFTER, depth2/return/restart and package acceptance remain
  pending. These BEFORE captures are Editor evidence, not standalone cave
  execution.
- ⚪ Sparse GreenRim composition, cave actors/containers/hazards and all later
  bands stay visible in the backlog. Nuanced animation is deferred.

Files changed by this preflight: **Docs/GRAPHICS-SHALLOW-CAVES.md only**.
No code, assets, tests, Unity or Git state was changed by its author. Next:
finish root's ordinary BEFORE route and exact owner/state reads, choose reuse
versus original cave materials against those captures, then follow the retained
capture → imagegen → native source → RED → minimal integration → GREEN → actual
comparison pipeline. This document must be updated with implementation and
evidence in the same source commit when that work occurs.

## 9. Implemented source and native checkpoint — 17:09UTC

### Final scope and source

`FlatPixelShallowCavesCatalog` now supplies the six exact owners through the
shared Greybox painter and authorized subject reader. Canonical depth1/2
addresses and immutable reserved-column facts are prepared once; attached
current graph, map shape, mutable POI and surface-keyed saved-lair authority
are checked on each query. No generation selector is rerun, and the catalog
does not infer unsaved historical provenance from material names.

Native ground requires a real, current, valid `SandstoneFloor`. Stairs/rubble
and covering actors preserve that floor, while actual hazards, cultivation
and competing material veto the plain-ground claim. Foreground art follows
the selected visible owner and exact owned parts/state. Partial structural
wall damage retains its rock silhouette; real destruction supplies the actual
Rubble owner. Remembered/hidden/unsupported owners retain fallback. Resource
groups fail independently; the catalog owns cached Sprite/Tile views and
never destroys borrowed textures. No new Render event, simulation action or
per-cell sprite construction was added.

The implemented ground path is:

```csharp
var caves = !fallback.Remembered ? ShallowCavesArtFor(zone) : null;
bool caveGround = caves != null
    && caves.TryResolveGround(zone, x, y, out caveGroundTile, out caveFloor);
if (caveGround)
{
    groundTile = caveGroundTile;
    groundColor = Color.white;
    ground = FlatPixelGreyboxGround.StoneFloor;
}
```

The exact source is in
[FlatPixelGreyboxCatalog.cs:908](../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs);
the same companion serves selected foreground and reader identity. The native
RGB/direct-paint correction in §4 remains applicable.

| Family | Implemented art and essential state |
| --- | --- |
| SandstoneWall | Two original opaque 16×24 cells in a 32×24 atlas. Shared base edges, sparse interior fractures, no masks/masonry/extrusion. Full and surviving partial structural HP use the same mass. |
| SandstoneFloor | Unchanged Beating frames 12–15, cached 16×24 views. Static coordinate variation favors frame 0 on 75% of positions. |
| Rubble / Stalagmite | Unchanged Environment 16×16 sources, centered 16PPU FullRect views; no resized or padded image copies. Their passable/solid distinction remains simulation-owned. |
| StairsDown / StairsUp | Unchanged opening-anchors4 / opening-cellar0. Real physical markers, current selection and actual travel/return rules remain authoritative. |

Wall variants use the same static hash with a 75%/25% split. The new source and
root-import PNG match SHA256
`4bb598677352f0ccd8b5be8a67f31e79d6aeab0704aa96db9c826b0d4b8dc576`.
The [frozen source handoff](../ArtSource/Live16x24/shallow-caves/frozen-handoff.json)
binds the editable Blender source, named cells, manifest and direct 1×/8×
contacts. Source RED preceded the first render. Source GREEN checks 768 exact
pixels, 768 reopened saved integer faces, 11 named clusters, actual material
colors, nine rejected corruptions and a positive metadata-whitespace control.
Two fresh Blender renders produced byte-identical atlas, contacts and source
cells; only nonpixel timing metadata is normalized. Twenty exact input files
and five explicitly normalized metadata pins remain protected. Root accepted
the source contacts for an actual trial; this is not cave gameplay acceptance.

### Native evidence and methodology correction

| Retained run | Actual result and interpretation |
| --- | --- |
| [Native RED](Verification/Live16x24/shallow-caves-native-red.xml),16:59:32–16:59:46UTC | 44 executed,44 failed,0 passed/skipped/inconclusive, before the new companion/resource were integrated. [Preflight](Verification/Live16x24/shallow-caves-red-preflight.json) records their absence and the existing Greybox hash. An earlier zero-discovery attempt and bridge startup timeout are not counted as RED or a pass. |
| [First GREEN attempt](Verification/Live16x24/shallow-caves-native-first-green-attempt.xml),17:02:59–17:03:05UTC | 43 passed,1 failed. `Native(Add(Floor,X-1,Y))` incorrectly checked the helper's default cellX,Y instead of the newly placed floor atX−1,Y. The [original fixture](Verification/Live16x24/shallow-caves-red-fixture.cs.txt) is retained. |
| [Final focused GREEN](Verification/Live16x24/shallow-caves-green.xml),17:06:24–17:06:31UTC | 44 passed,0 failed/skipped/inconclusive after correcting that one call to `Native(Add(Floor,X-1,Y),X-1,Y)`. Runtime code did not change for this correction. XML SHA256`689c34d25e579efbcd37c808eb5b572709d157b7c6dc9d4d57d3a8f0e5d163b5`. |
| [Related native GREEN](Verification/Live16x24/shallow-caves-related-green.xml),17:08:38–17:09:04UTC | 339 passed,0 failed/skipped/inconclusive across ten related fixtures. Raw XML completed despite another bridge startup-metadata timeout; the timeout itself is not a passing result. XML SHA256`ab22203e619817883811640e63e97555ec14d25a542c2877699e4e19d2af4666`. |

The focused cases exercise real factory owners, submitted painter/authorized
reader, both generated shallow depths, current graph and reserved-column
refusals, disclosure, custody/footprint/material guards, independent ground
and hazards, real structural damage/destruction, actual vertical transition
round trip and missing return endpoint, optional inputs and disposal. These
are native tests, not an ordinary keyboard traversal or performance result.
The two final XMLs contain disjoint fullname sets: 44 + 339 = **383 distinct
native passes**. This is the bounded selected regression suite, not an all-game
test run. Counts and the zero-overlap claim were checked directly from the XML.

**Test-first timing deviation:** a production scratch draft was prepared
outside Assets before the RED run. It was applied to Assets only after all 44
genuine native failures and confirmation that the runner was idle. This
establishes a real before-integration RED boundary, but does **not** satisfy
the stricter claim that production code was first authored after RED. The
earlier draft timing is retained and acknowledged; no reenacted failure is
presented as the original RED. Future production drafts must wait for RED.

### In-phase review, files and remaining gates

- 🔵 The preliminary reuse decisions resolved to five unchanged families and
  two new natural-wall cells. Beating masonry was rejected for this owner;
  no old art, geometry, population, combat or stair mechanics were changed.
- 🔵 The fixture-coordinate mistake is fixed and both runs are retained.
  Independent runtime/art review by the test author found no significant
  defect; that author's test review is self-review, not an independent test
  audit. Source-contact review is separate from actual-play acceptance.
- 🧪 Matched AFTER views, depth2/return/restart and packaged Mac/Linux
  verification remain pending. Actual cave wall/floor contrast,
  repetition and connected-mass readability still require ordinary play.
- 🧪 The BEFORE route's fatal Tunnelguard encounter remains honest evidence
  of an interrupted route. No grants, difficulty adjustment or teleports
  were added to manufacture its completion. Other fauna, containers, hazards,
  deeper bands, authored caves and later area polish remain open.

Files in this implementation slice:

- New `Assets/Scripts/Presentation/Rendering/FlatPixelShallowCavesCatalog.cs`
  and meta; modified `FlatPixelGreyboxCatalog.cs`: finite scope, shared native
  ground/body/reader integration and owned-resource disposal.
- New `Assets/Tests/EditMode/FlatPixelShallowCavesTests.cs` and meta: 44 focused
  cases, with the original RED fixture and raw XML retained under verification.
- New `Tools/Live16x24/build_shallow_caves.py`,
  `ArtSource/Live16x24/shallow-caves/` production source/evidence, and
  `Assets/Resources/Sprites/Live16x24/shallow-caves.png` plus root-owned meta:
  original two-cell wall source and import. Root-owned concepts/prompts are
  retained separately from production pixels.
- This phase document and the named `Docs/Verification/Live16x24/shallow-caves-*`
  evidence: source corrections, actual BEFORE, timing disclosure and native
  results. Source push and a new cave package remain pending.

## 10. First actual AFTER defect — retained ambient backdrop plan

Root replayed the earned cave in isolated Editor Play after the first native
GREEN. [AFTER01 entry](Verification/Live16x24/shallow-caves-after-01-entry.png)
shows native walls, floor and rubble, but black squares interrupt otherwise
continuous passages. Transparent rubble exposes the same black backing.
The document's author viewed this capture: these dark squares read like pits,
although these cells have no corresponding pit owner or newly blocking rule.
This is a significant scene defect, not an accepted material pass.

[AFTER02](Verification/Live16x24/shallow-caves-after-02-up-stair-menu.png)
retains a filename suggesting an up-stair menu, but it was a focus miss and
shows neither that menu nor a completed action. It does not count as reader or
travel evidence. [AFTER03 examine](Verification/Live16x24/shallow-caves-after-03-up-stair-examine.png)
does show the actual stairs-leading-up reader and its native portrait. That
establishes the reader, not successful ascent, return or broader cave acceptance.

Root stopped Play and preserved the first-trial isolation as
`e4200c135a14440b89f97f2576fa935d-cave-first-trial-preserved`. The initial
images and 383-case evidence remain intact. The original frozen wall packet
does not need a pixel change for this background defect.

### Source correction and bounded invariant

The earlier requirement to avoid inventing a sandstone floor is correct but
insufficient as a presentation policy. `StrataBuilder` legitimately leaves
some carved cells without a floor; a lone transparent Rubble also need not
have a floor underneath. Their existing black backing becomes misleading
beside opaque native floor. **An ambient cell backdrop is not a floor owner,
floor sprite, terrain kind, interaction or collision claim.** This distinction
permits a neutral background while retaining every real-floor countercheck.

The pre-implementation contract below was saved before the new RED runs;
its implementation/results are recorded in §11. The correction is separate:

- Add companion query `TryResolveCellBackdrop` for an eligible, currently
  disclosed, neutral cave cell. Reuse the existing cached white
  `FlatPixelFallbackCatalog.BackgroundTile`, tinted RGB128,106,76, matching
  the approved floor base. This is flat presentation color, not a new sprite
  or procedural texture. Do not add an owner or return one from the query.
- Add default-false `FlatPixelGreyboxCell.AmbientBackdrop` solely to let the
  submitted background retain that intended RGB rather than pass through
  `ApplyFlatLighting`. The existing `LocalGround` flag remains **false** for
  empty/Rubble-only cells; the real-floor query still fails there. Do not set
  `StoneFloor`, invent a reader subject, mark a native ground identity, alter
  movement or suppress tile-state/liquid marks because of this flag.
- Keep the same canonical ordinary depth1/2, current graph, map/POI/reserved/
  saved-lair and cell disclosure gates as the companion. Remembered, hidden,
  unexplored or unauthorized cells keep the current fallback. No neighbor
  lookup or inferred unseen topology is required.
- Require neutral actual tile state. Inspect only currently registered,
  visible physical occupants with owned placement/parts and no inventory or
  equipment custody. Such an occupant must veto the backdrop if it supplies
  a `SandstoneFloor` (including failed/missing floor resources), unknown Terrain,
  a liquid/gas/source/trigger/cultivation meaning, an authored background,
  or a malformed Rubble identity. Do not hide a failed floor admission behind
  a coincidentally matching background color.
- Hidden, foreign/stale and custodied entities cannot add or revoke the
  backdrop. An authoritative visible Rubble may permit the neutral background
  only when its actual ordinary identity is valid. Ordinary covering actors
  or items do not themselves create terrain. Preserve the selected foreground,
  authorized reader, dirty repaint and all actual state exactly.
- Cache/reuse the existing tile and return presentation data only. Do not
  allocate textures/sprites in cell queries, run simulation or fire additional
  Render events. No global material tint or lighting policy change is proposed.

This narrowly supersedes the historical §5 instruction to retain the existing
background in every ownerless cell. It does **not** supersede the real-owner
floor rule, safe fallback for nonneutral states or disclosure boundaries.

### Retained test-first gate and actual acceptance

At this planning checkpoint the test author was adding focused RED controls.
The explicit gate was: no runtime implementation
or production scratch draft for this correction may be written before root
confirms genuine native RED. The existing initial timing deviation is not
repeated or hidden.

The new positive/counter pairs must check the submitted background tint and
cached tile, separate default-false flag, unchanged owner/foreground/reader,
and `LocalGround == false` for ownerless/Rubble-only cells. They must also cover
a real floor with a missing resource, altered Rubble, unknown Terrain,
actual hazards/sources/cultivation/authored background, and hidden/foreign/
custody variants that cannot influence visible presentation. Current-graph and
disclosure refusal must survive warmed caches. A real floor's existing native
identity and the previously tested no-invented-floor assertion remain intact.

After RED, minimal implementation, GREEN and review, root should compare the
same entry and passages in ordinary Play. Acceptance requires the accidental
black-pit pattern to disappear without whitening hazards, erasing real
backgrounds or revealing fog contents. Retain an explicit view of rubble,
empty neutral cells and the up-stair reader; continue the wider depth/return
route only through ordinary controls. This plan is not completed runtime,
visual acceptance, a performance measurement or a new package. The completed
runtime/native step is recorded below; actual visual acceptance is separate.

## 11. Backdrop implementation and current native results — 17:30UTC

Root authored production only after the strengthened native RED completed.
This correction follows the strict test-first boundary: there was no earlier
production scratch draft. It does not retrospectively change the initial
six-owner timing deviation in §9.

`TryResolveCellBackdrop` now returns the existing borrowed white background
tile and RGB128,106,76 for a neutral disclosed eligible cave cell. It does not
return or create a floor owner. Greybox records default-false `AmbientBackdrop`
separately from `LocalGround`; real-floor admission and all foreground/reader
identity remain independent. Both actual background paint paths now honor
the flag: `ZoneRenderer.RenderFlatCellCore` and the environment's background
claim pass. Non-native backgrounds without either flag keep their existing
lighting attenuation. State marks and liquid suppression still use their
existing semantic flags, not `AmbientBackdrop`.

Only four runtime files changed for this correction:

- `FlatPixelShallowCavesCatalog.cs`: finite backdrop query and current
  disclosed owner/state/resource-failure refusals.
- `FlatPixelGreyboxCatalog.cs`: separate presentation flag and composition.
- `ZoneRenderer.FlatPixelPaint.cs`: main background paint respects the flag.
- `EnvironmentSpriteRenderer.cs`: environment background repaint uses the
  same flag, preventing a later pass from dimming it again.

The frozen atlas, palette, geometry, import and all other art are unchanged.
No new texture, resource, gameplay owner, world generation or simulation
behavior was added.

| Retained native run | Actual result and interpretation |
| --- | --- |
| [Backdrop RED](Verification/Live16x24/shallow-caves-backdrop-red.xml),17:20:21–17:20:36UTC | 12 new failures,44 prior passes,0 skipped/inconclusive, with production still unchanged. Original test source retained in `shallow-caves-backdrop-red-fixture.cs.txt`. |
| [Strengthened RED](Verification/Live16x24/shallow-caves-backdrop-strengthened-red.xml),17:22:59–17:23:14UTC | The same12 failures and44 passes, with stronger real-attenuation/painter controls. Production was authored only after this run. Strengthened source retained in `shallow-caves-backdrop-strengthened-red-fixture.cs.txt`. |
| [First GREEN attempt](Verification/Live16x24/shallow-caves-backdrop-green-attempt.xml),17:25:15–17:25:23UTC | 55 passed,1 fixture failure,0 skipped/inconclusive. The optional-resource test injected a texture but its helper incorrectly expected the Resources-loaded Beating texture. The raw run remains retained. This was not a56-pass result. |
| [Final focused GREEN](Verification/Live16x24/shallow-caves-backdrop-green.xml),17:27:05–17:27:19UTC | 56 passed,0 failed/skipped/inconclusive. The fixture now checks the actual injected `inputs[1]` texture and its shared world/reader rect/tint. No runtime fix was needed. SHA256`189ebe9792c41cc8e7d765a2c2e6d84b604aa611a83d6affefc341d1c5cdd5cb`. |
| [Current related rerun](Verification/Live16x24/shallow-caves-backdrop-related-green.xml),17:29:38–17:30:03UTC | 339 passed,0 failed/skipped/inconclusive on the corrected runtime. SHA256`ad2353650ad520a64d6df46e16b86a5a654448b353c54fba216dc14d8d31c345`. |

The two current GREEN files contain disjoint fullname sets, giving **395
distinct native passes**, not395 plus the superseded383. The added tests
exercise both real attenuation paths, an unchanged dimmed non-native control,
incremental cover/removal/repaint without a forced full pass or camera change,
and the source-planned owner/state/disclosure/resource countercases. Passing
those checks does not establish a hardware performance gain.

Two independent cold reviews found no significant blocker in this correction.
The fixture expectation error is corrected with its failed result retained;
it is not described as a production regression. The source author did not
rerender or alter the frozen wall packet during this correction.

- 🔵 Strict new RED preceded production; initial and strengthened source/run
  evidence remain preserved, alongside the55/1 fixture correction.
- 🧪 Actual matched AFTER replay, whole-view contrast/repetition, depth2/return/
  restart and current-source package gates still need root evidence. The first
  flawed view is retained; native GREEN alone does not accept the backdrop fix.
- ⚪ Remaining cave actors, containers, hazards, deeper bands and separate
  serious area polish remain open. No broad coverage claim is promoted here.

## 12. Corrected ordinary Editor replay — bounded visual and gameplay evidence

Root replayed the earned route through normal keyboard controls with the
same frozen wall source and corrected runtime. The author of this phase
record viewed actual AFTER04,07,12,15,16,19,25,26,27,28,29 and read the profile,
both new save headers and the final
[29-capture replay receipt](Verification/Live16x24/shallow-caves-after-play.json).
Specific structural HP/entity IDs below are root's
script-observed state, not values inferred from sprite pixels. A separate
reviewer owns the final image-review report.

| Actual checkpoint | Verified meaning and limit |
| --- | --- |
| [04 corrected entry](Verification/Live16x24/shallow-caves-after-04-backdrop-entry.png) | Matched initial cave view no longer has black squares behind neutral empty cells and rubble. Rock walls remain darker than the traversable base; rubble silhouettes remain visible. This closes the named first-trial backdrop defect in the observed view. |
| [07 stalagmite reader](Verification/Live16x24/shallow-caves-after-07-stalagmite-reader.png) | Actual native stalagmite portrait and solid owner at53,3. Together with actual up-stair readers03/06, this verifies those exact subject paths; it is not a claim for every cave object. |
| [08 surface return](Verification/Live16x24/shallow-caves-after-08-surface-return.png) and [09 reentry](Verification/Live16x24/shallow-caves-after-09-reentry.png) | Normal ascent to Overworld.0.9.0 and descent back to0.9.1, using the actual physical endpoints. No generated replacement endpoint or teleport. |
| [11 wall damage](Verification/Live16x24/shallow-caves-after-11-wall-damage.png) and [12 wreckage](Verification/Live16x24/shallow-caves-after-12-wall-wreckage.png) | Ordinary attacks on wall48,13 gave observed structural HP28→16→5, then real destruction and Rubble72942. The player subsequently entered48,13, verifying the newly passable cell. No synthetic HP or cell edit. |
| [15 fresh Continue](Verification/Live16x24/shallow-caves-after-15-fresh-continue.png) and [16 actual rubble examine](Verification/Live16x24/shallow-caves-after-16-rubble-examine.png) | Fresh isolated Continue from the real turn10980 autosave preserves the wall's removal and native rubble. Actual examine identifies rubble with its native portrait and terrain meaning. This is persisted wreckage, not a staged prop. |
| [18 Wind Barrens entrance](Verification/Live16x24/shallow-caves-after-18-wind-barrens-entrance.png) and [19 second column](Verification/Live16x24/shallow-caves-after-19-second-column.png) | Normal world-map travel to surface16,13, then actual descent to its depth1 cave. Native material/backdrop policy works beyond the first0,9 column in this observed route. |
| [21 supplies](Verification/Live16x24/shallow-caves-after-21-inventory-supplies.png), [23 tonic menu](Verification/Live16x24/shallow-caves-after-23-tonic-menu.png) and [24 route combat](Verification/Live16x24/shallow-caves-after-24-route-combat.png) | Root used an existing tonic through the ordinary inventory menu, healing23→40HP. Further ordinary wall breaking and a Scrabbler fight left33HP and killed the Scrabbler. Supplies were consumed through their real actions, not granted to complete the test. |
| [25 depth2](Verification/Live16x24/shallow-caves-after-25-depth2.png) and [26 return](Verification/Live16x24/shallow-caves-after-26-depth2-return.png) | Ordinary `>` reached Overworld.16.13.2; ordinary `<` returned to depth1's actual down-stair. Both captures show HP33. The different visible/remembered regions are current disclosure, not removed cave geometry. |
| [27 fresh depth2 Continue](Verification/Live16x24/shallow-caves-after-27-depth2-fresh-continue.png) | Fresh isolated normal Continue from retained fixture85 restores Overworld.16.13.2, player40,12, HP33. Native floor, wall, rubble and stair context persist. No pixel-equality or all-cache parity claim is made. |
| [28 down-stair menu](Verification/Live16x24/shallow-caves-after-28-down-stair-menu.png) and [29 down-stair examine](Verification/Live16x24/shallow-caves-after-29-down-stair-examine.png) | Normal look/menu/examine identifies the disclosed down stair at39,12 with its native shared portrait. This verifies the reader; it does not claim descent to depth3. |

Two attempts are explicitly excluded from the reader/save evidence:
[13 rejected Skills input](Verification/Live16x24/shallow-caves-after-13-rejected-skills-input.png)
and [14 Pause input](Verification/Live16x24/shallow-caves-after-14-pause-input.png).
Neither is a successful reader or the save provenance. Earlier AFTER02 also
remains the documented focus miss. These files are retained rather than
silently relabeled as successful actions.

### Earned saves and protection boundary

Both retained save headers identify the actual earned GameID
`50e670c30b6d48feb82756df64bd1cbc`:

- [Wreckage84](Verification/Live16x24/Fixtures/shallow-caves-wreckage84/Quick.json):
  actual autosave at turn10980, Overworld.0.9.1, HP23/40,
  timestamp2026-10-27T17:37:31.9055400Z. Its save stream is retained alongside
  the header; actual fresh Continue and the rubble reader establish the observed
  persisted destruction, independently of the rejected UI attempts.
- [Depth2-85](Verification/Live16x24/Fixtures/shallow-caves-depth2-85/Quick.json):
  final earned F5 checkpoint at turn11910, Overworld.16.13.2, HP33/40,
  timestamp2026-10-27T17:49:53.6404320Z. The final replay receipt clarifies the
  order: this F5 was taken at depth2 after25, **before** the successful ascent
  shown in26. AFTER27 starts a fresh isolation and continues that earlier
  saved depth2 state; no intervening second descent is asserted.

The original living entry83, fatal BEFORE continuation and flawed first trial
remain preserved. The initial Tunnelguard death is not rewritten as a
successful route. The later depth2 journey used the second ordinary column
and existing equipment/supplies. Root's final protection check now reports
both normal Editor/standalone save locations unchanged and all44 unrelated
tracked baseline paths preserved. The five isolated roots listed in the replay
receipt were archived; isolated Play is finished, its root is null and the
scene is clean. These are root's protection results, not an inference from the
existence of fixtures or this author's independent inspection of normal saves.

The receipt also retains two rejected extraction attempts: the initial84
copy selected a stale launch marker and was rejected by its metadata, with
that copy preserved separately; the accepted84 comes from the actual GameID
autosave. The initial85 copy expected the wrong compressed suffix; its actual
`Quick.sav.gz` was subsequently copied byte-for-byte. No save payload was
edited. An unverified rapid return-input batch also does not count as a
transition; the repeated single normal `<` established the ascent in26.

### Profile — measured Editor context, no speed claim

[The profile](Verification/Live16x24/shallow-caves-profile.json) spans
17:34:27.608069–17:35:57.894642UTC,90.286573 seconds, with18,378 samples per
recorder. It includes actual Editor cave reentry, interrupted stair autowalk,
ordinary wall attacks, idle time and MCP/CUA instrumentation. Recorders were
stopped/disposed before later travel.

| Recorder | Mean | P95 | Maximum |
| --- | ---: | ---: | ---: |
| ZoneRenderer.LateUpdate | 0.121363ms | 0.197458ms | 24.002292ms |
| Input.Update | 0.017353ms | 0.030250ms | 14.026334ms |
| Main Thread | 4.905822ms | 13.729792ms | 1354.185833ms |
| GC Allocated In Frame | 64,519.31 bytes | 19,135 bytes | 139,923,785 bytes |

Timing values above convert the raw nanosecond fields to milliseconds; they
are not FPS estimates. There is no matched BEFORE timing run, standalone
measurement, Linux execution or physical Steam Deck evidence. The instrumentation
and large recorded spikes prevent treating these samples as a sustained
shipping-game performance result or an improvement claim.

### Bounded outcome and remaining work

The observed integration now connects native material/readers to real wall
damage, wreckage passability, surface/depth travel and restart. The corrected
entry removes the named accidental-pit pattern without adding a floor owner.
The whole views retain readable routes and quiet negative space; visible
walls use the original planar mass rather than masonry. These are bounded
observations from two columns, not a census or an all-seed acceptance claim.

Remembered cells still use the existing dark glyph fallback, clearly visible
around the return route. Fauna, containers, vents, other hazards, deeper bands,
roomed caves and authored stacks remain separate coverage or polish work;
this slice does not repaint those owners or certify their actions. Some
large chambers remain simple and repetitive, so the later serious composition
and area-polish pass remains open. Do not add arbitrary filler to inflate
apparent density or count dark fog as empty generated terrain.

Native evidence remains **395 distinct passes**, unchanged by this replay.
Root's final protection check is complete; source publication and fresh
Mac/Linux package work remain pending. The final visual review is separate, and no
Steam Deck/performance or completed-underground claim is promoted.
