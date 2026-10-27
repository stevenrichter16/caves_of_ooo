# Ordinary shale caves — finite material coverage plan

**Status: source-corrected preimplementation plan and actual depth5/6 entry
baselines only.** No shale runtime, test fixture, native art or acceptance is
created by this document. The preceding limestone source
`7f49fb70054b7b29957dacd17c999ae4b521c9fd` is verified on main with bounded fresh
Mac play and Linux archive integrity; this separate shale phase does not
inherit that material or area acceptance. Root owns Unity, further ordinary
play and Git. The five shared living records remain frozen during this task.

First proposed scope is exact `ShaleFloor` and `ShaleWall` in conservatively
eligible current ordinary depths5–6, with unchanged Rubble, Stalagmite,
StairsDown and StairsUp as shared candidates. Use a distinct shale material
kit; do not recolor limestone by depth or admit generic walls. Broader native
coverage and the later serious biome-by-biome/area-by-area polish remain open.
No polish status advances from this plan.

## Actual baseline and its limits

The [packaged baseline receipt](Verification/Live16x24/shale-caves-before-play.json)
records ordinary controls in the current limestone Mac build. From depth4,
normal Left then `>` entered `Overworld.16.13.5` at39,13,HP33; F5 preserved
[fixture92](Verification/Live16x24/Fixtures/shale-caves-entry92/Quick.json),
turn12410. Normal `<` returned to limestone4. Actual captures
`shale-caves-before-01-packaged-entry.jpg` and
`shale-caves-before-02-limestone-return.jpg` retain this baseline only.

Root subsequently captured actual Editor depth5 entry
`shale-caves-before-03-editor-entry.png`, then normally traveled to depth6 at
40,13,HP33 in `shale-caves-before-04-depth6-entry.png`.
[Fixture93](Verification/Live16x24/Fixtures/shale-caves-depth6-93/Quick.json)
records `Overworld.16.13.6`,turn12430,HP33. The plan author opened both Editor
images: depth5 is a narrow disclosed dark floor/wall pocket; depth6 opens onto
branching disclosed passages with glyph materials, violet glyph marks, a
visible sword and bear glyph. The images do not establish danger-free routes,
interaction outcomes or an entire80×25 composition review. The old limestone
combat/destruction text in the log is inherited history, not a new shale test.

The two read-only entry censuses record the following total/current-visible
owner counts. Visibility is a snapshot at entry, not proof every counted object
was independently inspected or safely approached.

| Owner | Depth5 total / visible | Depth6 total / visible |
| --- | ---: | ---: |
| ShaleFloor | 1221 / 72 | 1149 / 406 |
| ShaleWall | 585 / 28 | 639 / 86 |
| Rubble | 134 / 11 | 142 / 51 |
| Stalagmite | 3 / 1 | 5 / 1 |
| StairsUp | 1 / 1 at39,13 | 1 / 1 at40,13 |
| StairsDown | 1 / 1 at40,13 | 1 / 1 at40,12 |

Sources: [depth5 census](Verification/Live16x24/shale-caves-before-depth5-census.json),
[depth6 census](Verification/Live16x24/shale-caves-before-depth6-census.json).
Depth5 also has13 generic Wall(2visible),4OreCache,Chest,MemoryBathPool,
2GlowQuartzVein,Glowmaw andCaveBear; those latter owners were hidden. Depth6
has15VineWall(3visible),4TarSeep(allvisible),LongSword andCaveBear(visible),
4OreCache,OilSlick,IronKey,PressurePlate,LockedChest andChoirTendril(hidden).
Neither census lists Pillar/BrokenColumn. These are separate real identities,
not shale decorations or permission to disclose hidden positions. A generic
wall or landmark family inside an ordinary column does not prove the saved
natural/roomed generation recipe.

The subsequent actual readers are retained in
`shale-caves-before-05-wall-reader.png` and
`shale-caves-before-06-floor-reader.png`: wall40,9 is Stone,solid,28/28;
floor41,13 is shale floor. Root reached depth6 through normal Right to40,13
then`>` and saved fixture93 atturn12430,HP33. No combat,hazard application or
wall damage occurred. The whole isolated root was archived as
`755d213cfff441148df7890c00b33a00-shale-before-preserved`; Finish verified
Playfalse,rootnull,scene clean. The updated baseline receipt retains this
Editor continuation.

Rubble/stair readers, structural collision and a wider normal-zoom route review
remain pending. The depth6 bear and oil sources make
short observed movement batches preferable to blindly replayed batches. Use
real earned state and normal controls; record combat/interruption without
health, inventory, visibility, AI or generation edits. A lost or rejected input
is not credited as a successful action.

## Preimplementation source verification and corrections

The production sources and referenced baseline files below were reopened for
this plan. This is CoO-original presentation, with no Qud parity or gameplay
change. No production snippet or test draft is authored before genuine RED.

| Potential false premise | Verified correction and consequence |
| --- | --- |
| Shale begins atdepth6, or all gray rock is limestone | [SolidEarthBuilder.cs:42](../Assets/Scripts/Gameplay/World/Generation/Builders/SolidEarthBuilder.cs:42) assigns ShaleWall/ShaleFloor at5–6, limestone3–4 and slate7–8. Keep exact blueprint and depth authority distinct; surface and deeper bands remain outside this kit. |
| ShaleWall has limestone's35HP/hardness3 | [Objects.json:8525](../Assets/Resources/Content/Blueprints/Objects.json:8525) defines structural28/max28,hardness2,Rubble wreckage,`#`,foreground`&K`,background`^k`. [Wall:3280](../Assets/Resources/Content/Blueprints/Objects.json:3280) supplies layer0,solid Physics,Stone/Mineral material and Thermal. Surviving1..28 is the same stone body; do not require a creature Hitpoints stat. |
| Generic Wall can share ShaleWall because glyph/colors match | The same Wall parent defaults to40/max40 andhardness3. It occurs in the actual depth5 census. Exact blueprint and structural semantics must refuse the alias even though its glyph,foreground andbackground match shale. VineWall is another separate owner. |
| ShaleFloor must carry Stone or Thermal parts | [ShaleFloor:8837](../Assets/Resources/Content/Blueprints/Objects.json:8837) inherits [Terrain:3438](../Assets/Resources/Content/Blueprints/Objects.json:3438),layer0,passable. It uses`.`,foreground`&K`,variants`...,.'` plus backtick, no authored background and no Material/Thermal/Destructible. Keep that real shape; add no mechanical parts for art. |
| Depth5/6 requires a new geometry or density change | [StrataBuilder.cs:34](../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs:34) already changes natural generation atdepth5 to seedChance60,passes3,noiseThreshold0.42. Its [floor placement:99](../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs:99) leaves real rubble-only and ownerless cells. Presentation must not fill them with invented floor owners or alter generation. |
| A live canonical ID proves original ordinary provenance | [WorldLocationContext.cs:5](../Assets/Scripts/Gameplay/World/WorldLocationContext.cs:5) is derived nonserialized authority. The [limestone guard:70](../Assets/Scripts/Presentation/Rendering/FlatPixelLimestoneCavesCatalog.cs:70) checks the attached manager,exact cached graph and correctly shaped current map/POI arrays. Reuse this conservative current eligibility, not a claim about an arbitrary edited/restored graph's original builder. |
| No current POI means no saved special/lair column | [LairStacks.cs:203](../Assets/Scripts/Gameplay/World/Generation/LairStacks.cs:203) supplies read-only `HasSavedColumn` using the surface key; `Inspect`/`TryPlan` may initialize state. Preserve that exclusion and the [precomputed reserved addresses:30](../Assets/Scripts/Presentation/Rendering/FlatPixelLimestoneCavesCatalog.cs:30):sinkhole mouths,Root,pilot and authored wilderness columns. Never generate/query mutable planning from a render cell. |
| Ordinary eligibility means all biomes generate rooms | [UndergroundLayoutPlan.cs:37](../Assets/Scripts/Gameplay/World/Generation/UndergroundLayoutPlan.cs:37) allows the stable25% roomed choice only for ordinary Spread/Sodden/Beating atdepth3+ with required content. Other biomes preserve their natural recipe. That cold builder gate is not an extra rendering-biome whitelist or saved provenance flag. |
| No Pillar in a census proves a natural cave | [RuinsBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/RuinsBuilder.cs:99) scatters Pillar/BrokenColumn probabilistically. Their absence and a single camera view do not certify the generator. Room-specific props remain separate even when the material floor/wall is covered. |
| The first shale floor also starts the next population tier | [PopulationTable.cs:1102](../Assets/Scripts/Data/Tables/PopulationTable.cs:1102) uses depth/3+1:depth5 is tier2,depth6 tier3. Burned/pale/skeletal additions and ChoirIronVein tier3 rules start at6, not5; authored comments are less precise than the conditions. Real encounters remain unchanged. |
| Material art covers containers,liquids and landmarks for free | [OverworldZoneManager.cs:580](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:580) adds landmarks,hazards,population and containers after geometry. The two actual censuses expose distinct Wall,VineWall,TarSeep and service/storage families. Retain truthful fallback or their existing portable native path; no generic material promotion. |
| Black gaps should receive a fabricated floor | [TryResolveCellBackdrop:124](../Assets/Scripts/Presentation/Rendering/FlatPixelLimestoneCavesCatalog.cs:124) is an owner-free canvas separate from real ground. A shale equivalent must retain disclosure,owner,state and resource-failure guards and use an approved shale base. It is never an examinable terrain owner. |

## Existing source art and finite native direction

The sixteen `Assets/Resources/Sprites/Environment/shale_m00.png` through`m15`
were decoded read-only: each is opaque16×16. Across4096pixels the palette is
RGB84,88,92 (`54585C`,2916pixels),92,96,100 (`5C6064`,743),74,78,84
(`4A4E54`,437). Representative importer fields are Point,no mipmaps,sRGB,
16PPU anduncompressed. Preserve every original PNG/importer.

[EnvironmentSpriteRenderer.cs:527](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:527)
describes these as a64×64 material field split into sixteen position-indexed
squares, with [ShaleFloor mapping:566](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:566)
and [macro loading:797](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:797).
They are not16adjacency masks or wall sprites. Centered16×16 ground would leave
vertical gaps in the16×24 grid; stretching breaks square pixels. The palette is
a verified reference, not an automatically accepted native canvas under the
current Cave grade. No new wall palette or frame count is fixed by this plan.

Use a small original native floor/wall family after actual capture critique
and imagegen scene/parts studies. Preserve shale's thin dark layered stone and
quiet overlapping flakes, distinct from pale limestone, warmer sandstone and
future slate. Solid/walkable separation must remain legible under actual Cave
grading. Avoid horizontal stripe grids, overbright seams, beveled3D tops and
pit-like black backgrounds. Do not brighten unknown/remembered cells to make a
concept prettier. Select a few useful quiet variants instead of a large mask
or animation packet unless a named actual defect requires more.

Existing Rubble/Stalagmite16×16 centered16PPU and native down/up stair cells
remain immutable reuse candidates, as in the [existing companion:49](../Assets/Scripts/Presentation/Rendering/FlatPixelLimestoneCavesCatalog.cs:49).
They require fresh contrast/scale review on shale and exact current-owner
mapping. Their successful limestone appearance does not accept a shale scene.

## Proposed integration boundary and counterchecks

Prefer one bounded companion through the existing Greybox ground,selected
foreground,authorized subject and disposal seams. Do not widen global
`IsAdmitted` or the sandstone/limestone companions. Cache only immutable
canonical address facts and Sprite/Tile views; recheck mutable current graph,
map/POI/lair and owner authority. Source-code implementation remains deferred
until root records genuine native RED.

Keep real ShaleFloor ground ownership distinct from selected body and ambient
canvas. A floor beneath an actor is still actual terrain; a successful ground
submission may suppress only its own redundant glyph. Subject portraits must
use the same currently selected disclosed owner and native frame. Reject
custom VisualID/variant,foreign/duplicate required parts,custody,multicell,
malformed structural/thermal state and active color overrides. Changed live
state retains truthful fallback; do not apply neutral art over mandatory cues.

An ambient canvas is a separate flag,color and existing cached background tile.
It may support truly neutral visible ownerless/rubble cells without changing
Owner,GroundKind,collision or reader identity. Do not let hidden,foreign or
carried occupants toggle backing. Refuse actual floor resource failure,
authored backgrounds,visible incompatible terrain/cultivation/liquid sources,
triggers and actual nonempty tile state. Preserve overlays and painter order;
do not add blanket hazard suppression. Existing [paint semantics:50](../Assets/Scripts/Presentation/Rendering/ZoneRenderer.FlatPixelPaint.cs:50)
and [environment semantics:1166](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:1166)
bypass light tint for native ground/ambient, so actual graded contrast needs
visual comparison rather than assumed source RGB parity.

Proposed finite native tests, **not authored or run here**:

- Exact current5/6 six-owner positives paired with4/7,surface,noncanonical,
  stale/detached/current-map mismatch,POI,legacy saved-lair and reserved-column
  refusals. Current eligibility must not initialize generation/ledger state.
- Real ShaleWall28/28 andsurviving1 preserve native identity; limestone35/3,
  genericWall40/3 andcustomwall roles refuse. Test actual destruction to real
  Rubble via [DestructionSystem.cs:171](../Assets/Scripts/Gameplay/World/DestructionSystem.cs:171),
  retaining unrelated tile state and dirty repaint. Never simulate wreckage by
  replacing the owner solely inside the assertion.
- Exact floor variants/parts with positive ownership below actor; same-layer
  selection and current subject parity. Hidden/remembered/removed,custodied,
  foreign/duplicate parts and one flipped-state counter must preserve truthful
  fallback without leaking current occupants through art or background.
- Native floor versus separate ownerless/rubble backdrop; optional missing or
  wrong-size material resource must not silently become ambient art. Shared
  resource groups fail independently and borrowed textures survive disposal.
- Actual submitted full/dirty frames,source removal and restored neighbors;
  no hidden hazard/mixture/cultivation/thermal/charge marks,extra render events,
  full-zone scan,camera reset or simulation mutation.
- Native ordinary stair transitions4↔5 and5↔6 plusmissing counterpart refusal
  where the existing real fixture supports it. Runtime tests do not replace
  actual ordinary controls/readers,wall damage/traversal and save/fresh Continue.

## Production, performance, review and delivery gates

Follow the [required production pipeline](LIVE-16X24-ART-DIRECTION.md:80):
actual playable baseline → actual capture/critique → imagegen scene study →
parts/essential-state mapping → original editable native pixels → real
integration after RED → normal play/profile/comparison and correction. Save
exact prompts,references,concept critique and owner/frame mapping. Reject
imagegen fog/layout/scale/UI alterations; do not shrink a scene into art.
Preserve all approved sprites,sources,metadata and previous presentation.

Profile actual normal gameplay, including correct maxima and recorder/idle/
instrumentation limits, after native integration. Use cached views and no
per-cell Split/LINQ,newcollections,worldcensus,eventdispatch or material parsing.
Reuse existing dirty reclaim; avoid a new renderer framework. Do not infer a
speedup from test counts or report Editor/tool latency as package/Deck FPS.

Before source publication, independent runtime/art/actual-view review must
address significant findings; exact same-commit living records retain raw
RED/GREEN,counters,profile,actual route,failures and recovery. Root pushes exact
owned files and verifies fresh Mac gameplay and Linux archive integrity as
separate gates. Preserve normal saves/preferences,earlier builds and unrelated
work. Storage remains inspection-only; this plan authorizes no cleanup.

Chunk fullness is an actual composition gate: coherent quiet material fields,
solid silhouettes,readable connecting routes,landmarks/services/hazards and
consistent reader identity across transitions. Inspect disclosed extents at
normal zoom; darkness is not a missing-prop count. No arbitrary decoration or
density change is included. Any named placement defect requires separate
source-corrected generation work and tests. Keep all55+5 existing polish states
unchanged; broad static coverage precedes the later serious all-area polish.

## Implementation log and in-phase self-review

- 2026-10-27: source-only plan saved after packaged5 entry/return and actual
  Editor5/6 entries,censuses,fixtures92/93 and literal wall/floor readers.
  Existing16floor textures decoded read-only; both Editor entry and reader
  images inspected. No runtime,test,art or
  generation draft created. Root's actual controls are attributed,not rerun by
  this author.
- 🧪 Pending: actual rubble/stair readers,blocking and broader view/route review;
  imagegen/parts/native source; genuine RED→GREEN,independent review,actual
  destruction/stairs/restart/profile,source push andfresh package gates.
- ⚪ Source-corrected contract:28/2 shale wall,unmodified floor parts,depth5tier2
  versus6tier3,current authority rather thanhistorical provenance,distinct
  genericWall/VineWall,hazard/ambient separation andsquare-source limitations.
- ⚪ Separate gaps: slate/quartzite/obsidian,containers,vents/liquids,room props,
  uncovered actors/special sites and serious whole-area polish. Shared-storage
  [triage](Verification/Live16x24/post-limestone-shared-owner-audit.md) remains
  source-only planning; already portable ChoirTendril/MarlbackScrabbler are not
  counted as missing new actors.

Files changed for this checkpoint: `Docs/GRAPHICS-SHALE-CAVES.md` only.
Last actual hourly report remains18:27:43UTC;next19:27:43UTC. This plan is not an
additional report and makes no acceptance claim.
