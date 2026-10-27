# Ordinary shale caves — finite material coverage plan

**Status:467 distinct native passes and a bounded actual Editor checkpoint.**
Root completed17 captures, all six owner readers, real wall damage/destruction,
Rubble traversal/fresh Continue and ordinary5→4→5→6→5 travel. The separate
depth6 combat interruption is preserved. Independent source/art/Editor review
is complete; source publication and fresh packages remain pending; no whole-area polish or device
performance acceptance is claimed. Earlier preimplementation and pending-GREEN
sections below are historical; the latest checkpoint records current state.
The preceding limestone source
`7f49fb70054b7b29957dacd17c999ae4b521c9fd` is verified on main with bounded fresh
Mac play and Linux archive integrity; this separate shale phase does not
inherit that material or area acceptance. Root owns Unity, further ordinary
play and Git. The five shared living records now reflect this bounded checkpoint.

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

## Source and reference checkpoint — 2026-10-27 19:10 UTC

The preceding limestone delivery evidence is now verified on origin/main at
`cd5e9af1ac3b7d3d95a57c5d856beeb963dc4928`. Root confirmed Editor stopped,
no compilation/build, clean SampleScene and the same46 unrelated tracked paths.
Those paths remain outside this phase.

Root used the actual depth6 entry capture for the built-in imagegen scene
reinterpretation, then generated a separate six-part material study. Exact
prompts and untouched reference outputs are in
`ArtSource/Live16x24/shale-caves/`. The useful direction is cool-gray thin
layered shale with quiet walkable ground and a distinct dense solid body.
The scene study changes resolution/UI and wall geometry, loses lighting and
introduces repetitive gravel; those changes are rejected. The parts study is
reference only: its striped walls and antialiased pixels must not be copied or
downscaled into production.

The bounded native contract selected before tests is96×24: four original16×24
floor cells at0–3, two wall cells at4–5. Floor/ownerless neutral base is
RGB84,88,92; wall palette and cluster placement remain an art decision subject
to actual comparison. Existing Rubble/Stalagmite/stairs remain unchanged.
Independent source audit confirmed28/max28,hardness2 shale versus generic
Wall40/3 and limestone35/3. Clarification: LocalGround/AmbientBackdrop bypass
per-cell attenuation but still pass through final camera grading; source RGB
is not an actual scene-color guarantee. No gameplay or generation edit is
authorized by this material plan. Native test fixture is being authored before
production; no RED/GREEN or new playable acceptance is claimed yet.

Genuine native RED completed19:12:25UTC before any shale runtime source:
36/36 failed (35 missing companion, one actual depth5 stair still glyph).
`shale-caves-native-red.xml` and its summary retain the evidence. The earlier
zero-case request is retained as invalid, with no assumed cause; reflection
subsequently found the fixture and a forced refresh allowed36-case discovery.
Runtime implementation is now permitted. Root rejected the initial wall-source
draft because three isolated highlighted piles read as loose rubble rather than
solid compressed layers. The four quiet floors remain stable; only the two new
wall cells are being revised with denser subtle broken laminae. That draft is
preserved separately and has no gameplay acceptance.

## Runtime integration checkpoint — RED verified, GREEN pending

The raw [native RED](Verification/Live16x24/shale-caves-native-red.xml) contains
36 executed failures from19:12:22–19:12:25UTC:35 missing-adapter failures and one
depth5 stair retaining glyph presentation. Its SHA256 is
`799c09353912995a4a6ecfbe11abefb55ce07a6a6ea7a0a3c0aaca7995a733f1`.
The earlier zero-case attempt remains invalid evidence. The runtime author
independently parsed the retained36-case XML and confirmed that the new source
file did not exist before starting production. No shale production draft
preceded this RED boundary.

The new [FlatPixelShaleCavesCatalog](../Assets/Scripts/Presentation/Rendering/FlatPixelShaleCavesCatalog.cs:12)
borrows the selector and five independently optional textures. Its default
constructor loads the original96×24 shale atlas and unchanged rubble,
stalagmite and stair sources; explicit null injection does not reload assets.
Four floor frames0–3 and two wall frames4–5 retain the approved frame contract.
The wall-source revision remains pending: root rejected the first draft's
isolated highlighted piles as loose rubble, rather than a solid compressed
mass. The first draft is preserved; runtime integration does not accept its art.

Exact implemented wall and ambient excerpts:

```csharp
case "ShaleWall":
    return Render(render, "#", "&K", 0) && render.BackgroundColor == "^k"
        && SolidObject(owner) && owner.HasTag("Wall") && Stone(owner)
        && SurvivingWall(owner) && NeutralThermal(owner);
// SurvivingWall also requires owned, non-Gone, positive structural HP,
// MaxHP28, Hardness2 and Rubble wreckage.
color = new Color32(84, 88, 92, 255);
```

Depth5/6 admission uses precomputed canonical addresses, the attached current
manager and exact cached graph, correctly shaped current map/POI arrays,
current POI absence and the read-only saved-lair surface key. Reserved columns
remain excluded. This is current eligibility, not reconstructed generation
provenance. Exact owner/parts, selected/disclosed placement, state, custody and
footprint checks preserve fallback for generic Wall, adjacent strata, custom
roles and incompatible hazards.

The [Greybox integration](../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:945)
adds one optional companion field, lazy acquisition, true ground, separate
ambient backdrop, foreground and authorized subject lookups, and disposal.
Separate output variables prevent failed optional lookups from erasing accepted
art. Only the actual submitted ShaleFloor suppresses its redundant glyph;
owner-free backing never sets LocalGround or creates an owner. Existing native
paint flags already preserve direct color in both painters, so neither painter
was edited. Camera grading remains a separate actual-view check.

### Scope and performance self-review

| Planned invariant | Implemented boundary / divergence |
| --- | --- |
| Six exact ordinary shale owners | ShaleFloor, ShaleWall, Rubble, Stalagmite, StairsDown and StairsUp only; no broader material alias or regional opening admission. No scope divergence. |
| Existing cave behavior remains intact | The limestone source hash is unchanged. Shale matches its finite control flow after names, depths5/6, authored colors,28/2 structure and84/88/92 backdrop substitutions. No shared framework/refactor was introduced. |
| Pure presentation | No gameplay, generation, blueprint, serialized Part, parser, command, transaction or diagnostic-action change; no extra Render event or frame polling. |
| Finite resources and cheap misses | Immutable address facts and ten Sprite/Tile views are prepared once. Explicit missing groups remain missing; borrowed textures and selector are not destroyed. |
| Existing incremental repaint | The companion participates only in existing queries and dirty-cell submission. It introduces no new Update/LateUpdate loop, full-world scan or forced full redraw. |

🔵 The runtime author read [PERF-FOUNDATION.md](PERF-FOUNDATION.md) and checked
the hot paths: no authored per-query collection/string allocation, reflection,
LINQ, parsing, generation or event dispatch. Local occupants use the existing
struct enumerator; current authority and owner state are rechecked instead of
caching mutable eligibility. One-time resource/view creation is not claimed to
be free. Actual instrumentation and native submitted-paint counters remain
required; source symmetry is not a performance measurement.

🔵 The test author's36-case fixture received a separate read-only source review
before implementation, including generic Wall's matching color/structure spoof,
thermal boundaries, missing-resource/backdrop separation and real4↔5/5↔6 stair
counterparts. No concrete fixture blocker was found. Root then reviewed the
new runtime symmetry and Greybox seams without identifying a blocker. These
are bounded source reviews, not GREEN or actual gameplay evidence.

🧪 Native compile/GREEN, related regressions, independent runtime review,
revised-source acceptance, whole-view contrast/fullness, ordinary wall
destruction/readers/travel/restart and profile remain pending. The pure
presentation scope uses these relevant counterchecks, not unrelated command
or serialization scaffolding. No new source push, package, Linux execution,
physical Deck performance or area-polish claim is made at this checkpoint.

### Frozen files for the native candidate

| File | SHA256 at integration freeze |
| --- | --- |
| `Assets/Scripts/Presentation/Rendering/FlatPixelShaleCavesCatalog.cs` | `eedecdb31479994c5678b0b954dbb7802e85d042a27546bb4502ad5e5681959d` |
| Its new `.cs.meta` | `27a7fe1df249d5e9469d2efbac3cae5b06b1c028d40f9351ec2e53c8b9346c65` |
| `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs` | `efdf1001274e215b40f69d31eba5a799748efb106800e1433bea32bdd2188625` |
| Frozen test fixture `FlatPixelShaleCavesTests.cs`, authored separately | `3d36614b36e5c0088492d3c08e70affbeb474e1571a0fea909b72470a602a793` |
| Unchanged predecessor `FlatPixelLimestoneCavesCatalog.cs` | `03ea7981d2ba80b845fbf1d303856760634f44503d9fc869018f263f40463ee5` |

Runtime author changed only the new companion/meta and the existing Greybox
seams. This follow-up changes only this phase document. Art and native tests
remain separate owners; root owns Unity, actual play, source publication and
delivery. This remains original CoO presentation, with no Qud parity claim.

### Prepared delivery helpers — independent source review only

Read-only comparison of `/tmp/coo-package-shale-caves.py` and
`/tmp/coo-shale-caves-restore.py` against the reviewed limestone predecessors
found no concrete guard regression. Ten generic package functions are
AST-identical, including source/build/output validation, manifest hashes,
safe archive membership/modes and full gzip EOF verification. The new native
gate rejects the intentionally empty GREEN hashes and zero related count
before writes; final byte pins and disjoint counts still require root review.
No future passing test count is inferred from the36-case RED fixture.

The restorer changes only phase labels and its exact three owned shale
metadata paths. The four settings allowlist, reviewed changed-set equality,
preexisting dirty-file protection, metadata trailing-whitespace-only rule,
index/save/preference/prior-build checks and explicit `--apply` remain intact.
Neither helper was imported or executed by this reviewer; no package, build
or restoration occurred as part of this check. Reviewed helper hashes:
`c4d042116d3f48e44838a8d839a66a86e8752909063e3f0ae002aa3a63cbbeea`
(package), and
`a17c5ee8fcb40ce2624ce9e8a3ed627b43d4aab2ca717cead5465b9ac7e6dd77`
(restore). Final bound pins and actual delivery still need their separate gates.

## Actual Editor checkpoint and hourly report —2026-10-27 19:28 UTC

### Native evidence and current source

The final focused [GREEN XML](Verification/Live16x24/shale-caves-native-green.xml)
contains36 Passed cases,19:16:06–19:16:11UTC, SHA256
`2ebd61f7f19d5649183710da71754d88d4dce1c39e987e03ae5014ff9b658e09`.
The [related rerun](Verification/Live16x24/shale-caves-related-green.xml) contains
431 Passed,19:16:51–19:17:24UTC, SHA256
`73aa27761ff010c25c7932afa748a8bad73b8ef57c966a2cc0bc657082dc943f`.
The two sets are disjoint: **467 distinct Passed, zero failed/skipped/inconclusive**.
This record author independently parsed both exact raw receipts and identities;
no tests were rerun for documentation. The
[summary](Verification/Live16x24/shale-caves-native-summary.json) retains those bounds.

The prepared package helper now pins those exact two receipts and derives467
from their reviewed disjoint counts; current helper SHA256 is
`6ee8b3dc4378dfc7db619093e11e31b19dce79d3ab23fe63e8048ea1f5412b19`.
Only the bound hashes, related count and preparation wording changed after the
earlier cold read. Its values were independently checked without importing or
executing the helper. Runtime publication and new builds have not occurred at
this checkpoint. Limestone source7f49fb700 and delivery evidence
`cd5e9af1ac3b7d3d95a57c5d856beeb963dc4928` remain the verified prior delivery.

### Ordinary gameplay, including the interruption

Root performed the controls and inspected the Game captures. This document
author read the [actual receipt](Verification/Live16x24/shale-caves-after-play.json),
[wall history](Verification/Live16x24/shale-caves-wall-history.json), fixture
metadata and17 saved capture files; that is not an independent visual replay.

| Trial / evidence | Actual observed outcome and limit |
| --- | --- |
| Depth6 from unchanged earned93; captures01–03 | Native entry and actual wall40,9 reader. One ordinary wall strike changed wall91332 from28→26. CaveBear93657 at39,11 then dealt25 damage, player HP33→8. F5 retained [damaged94](Verification/Live16x24/Fixtures/shale-caves-damaged94/Quick.json),turn12470; the full isolated trial was archived. This branch is not a completed destruction or safe encounter. |
| Separate depth5 trial from earlier unchanged earned92;04–07 | The earlier damage/combat branch remains intact. Eight ordinary Up attacks on wall87914 at39,12 changed structural28→24→12→8→4→actual Rubble90323. These are retained sampled checkpoints, not an invented per-hit sequence. The cell was nonblocking before entry. The player walked into39,12 and F5 saved [wreckage95](Verification/Live16x24/Fixtures/shale-caves-wreckage95/Quick.json),turn12500,HP33. |
| New isolated Continue of95;08 | Same depth5, player39,12,HP33 and real Rubble90323. The receipt's block=true after arrival is player occupancy; it does not make Rubble solid. This is actual persistence, not a scripted replacement or health reset. |
| Six owner readers;02,09–12,15 | Actual ShaleWall, Rubble, Stalagmite, ShaleFloor, StairsUp and StairsDown readers. Native tests separately cover malformed/hidden/resource cases; six observed readers do not establish every owner in a whole column. |
| Ordinary vertical route;13–17 | From depth5, normal `<` returned to limestone4; `>` reentered5; Right to40,13 then `>` entered6; `<` returned5. F5 preserved [roundtrip96](Verification/Live16x24/Fixtures/shale-caves-roundtrip96/Quick.json),turn12730,HP33 at40,13. No grants, teleports or AI/visibility edits were used. |

Input imperfections are retained: a combined close/Look batch missed Look and
walked the player to39,18. A wrong39,24 target was canceled, then the actual
stalagmite39,19 and floor40,18 were read after recentering. Incidental Return
activated EnGarde through normal input. These are disclosed actions, not
retroactively credited as the intended keys.

Root reports all three isolated roots archived before Finish, with Playfalse,
rootnull and clean SampleScene, and normal saves/preferences preserved through
the existing isolated launcher. This record author did not rerun the protection
audit. Fixture metadata and save hashes were read for94/95/96 and recorded in
both coverage ledgers; no save content was changed.

### Actual profile and its limits

The [profile](Verification/Live16x24/shale-caves-profile.json) spans
19:18:19.650118–19:19:54.122395UTC, **94.472277 seconds**, with34,675 samples for
each marker and capacity not reached. It includes the depth6 entry, reader,
three-step approach, wall strike, bear hit, idle and MCP/CUA instrumentation.
Recorders were stopped/disposed before the later trial. Values below convert
the retained nanoseconds to milliseconds; no samples or maxima were discarded.

| Marker | Mean | p95 | Maximum |
| --- | ---: | ---: | ---: |
| ZoneRenderer.LateUpdate |0.120941ms |0.196291ms |28.576583ms |
| Input.Update |0.034484ms |0.028917ms |662.931833ms |
| Main Thread |2.720410ms |5.369458ms |1384.246458ms |
| GC Allocated In Frame |47,478.343bytes |18,818bytes |310,472,283bytes |

These data do not isolate the adapter's cost. There is no matched before run,
sustained package benchmark, physical Deck measurement or demonstrated speedup.
The large maxima remain visible; low averages cannot certify smooth gameplay.
Native counterchecks and source allocation review address a different claim.

### Review state, remaining work and report clock

🔵 The finite material/state/travel/persistence journey is now observed in actual
Editor Play, alongside467 native passes. The damaged bear branch is preserved
and the later healthy trial is explicitly identified as a separate earlier
earned start. No game mechanic or generation change was needed.

🧪 Independent source/art/actual-view review is now complete with no new
significant issue. Exact owned source push and fresh Mac/Linux delivery remain open. The17 views do not accept the complete
80×25 chunks, all columns or serious area polish. Slate, quartzite, obsidian,
shared storage and mimic disguise, remaining actors, hazards and room props
remain separate coverage work. No Linux execution or physical Deck claim.

Root **sent the hourly report at19:28:00UTC**: completed limestonecd5 delivery
and this bounded shale Editor checkpoint; review/push/packages in progress;
next verify fresh Mac/Linux delivery, then missing world families. The next
report is due20:28:00UTC. Both JSON ledgers preserve the prior18:27:43 report
once in history and advance the clock once. This document update is not an
additional notification. All55+5 polish states remain unchanged; coverage is
not polish and storage cleanup remains unauthorized.

Files changed for this continuation: this phase document, the three shared
graphics Markdown records and the two coverage JSON ledgers only. No runtime,
test, asset, Unity or Git operation was performed for this record update.


### Independent Editor review completed

The [retained review](Verification/Live16x24/shale-caves-independent-review.md)
independently inspected all17 actual captures and found no new significant
material or reader regression. Runtime/art/actual-view review is independent;
the fixture author explicitly identifies the test audit as self-review. The
bounded six-owner Editor integration is accepted within the recorded route,
with broader area composition and source/package gates still open.

The review's decoded07/08 restart comparison found8,729 changed pixels out of
639,200 in rectangle[0,0,940,680], maximum channel delta100, near the player/light
area[382,252,558,432]. No owner/layout mismatch was visually apparent and
recorded save state agrees, but the cause of those pixel changes was not
established. Do not claim exact restart image identity. The hourly record and
all55+5 polish states remain unchanged; no new tests or actual replay occurred
for this final record adjustment.

Protection clarification from root’s read-only comparison: normal Editor and
standalone Saves, all46 foreign hashes, standalone preferences and game
preference keys are unchanged against the prior limestone prebuild snapshot.
Only Editor telemetry keys `unity.player_session_count`, `unity.player_sessionid`,
`PT_Run` and `unity_connect.session_id` differ. Full Editor plist byte identity
is not claimed; the fresh prebuild snapshot will retain current telemetry, with
no session-ID restoration needed.
