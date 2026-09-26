# Reference glade art — C14

Status: third refinement published after source/native RED: 40 terrain/prop models
and eight scoped actor paint meshes. Native import passed; GREEN and visual comparison
are pending. The first and second native images are explicitly rejected below.
All Assets are frozen for parent verification.
Main world/acceptance plan: `Docs/DENSITY-REFERENCE-GLADE.md` (parent-owned).
Reference: user-supplied September26 image; original Ooo interpretation.

## Preimplementation sweep

| Premise | Verified correction | Implementation consequence |
|---|---|---|
| Existing Spread models reproduce the image | Existing20-model kit covers warm hedge, barley, flowers, reeds, stubble | Separate28-model, seven-family kit with compact dark teal/cream/grass/gray/emerald palette; do not recolor shared atlas |
| Registering a material is enough | `SpawnRing3DGroundPatches.GetModel` rejects all except ring palette/water and groups into only those two meshes | Add third glade material batch, only for the active authored zone; preserve existing terrain batching |
| A new camera is needed | Current56° ortho has compensated ground registration and native raised-object picking | Retain it for first native comparison; no cosmetic camera changes until evidence requires them |
|11.10 is an authored settlement | BiomeRows/TierRows give Spread/Tier1; Places/SinkholeSites contain no11.10 | Parent owns procedural POI reservation and runtime authority; presentation gates on ReferenceGladePlan.IsActive(Zone) |
| Green geometry constitutes a light | Shader only samples the actual native light/fog texture | Parent attaches actual enabled LightSourcePart(&G) to lit owners; art does not invent gameplay illumination |
| Tiny cubes require many objects | Existing builders combine cube vertices into one mesh per prefab and changed ground patch | Offline meshes, no runtime cube GameObjects or per-frame geometry generation |

## Scoped asset contract

Four deterministic variants of each family, under the `reference-glade-` prefix:

| Family / VisualID suffix | Native blueprint | Intended silhouette |
|---|---|---|
| ground | Grass | Quiet dark teal square ground, a few low scattered stones |
| pale-reeds | Reeds | Seven irregular upright branching pale stalks, about0.65high |
| green-grass | Bush | Connected saturated green five-stem clusters |
| dark-ruin | Wall or BrokenColumn | Fine irregular charcoal/teal broken spires |
| low-wall | Wall | Gray squared masonry with staggered top stones, about0.55high |
| lit-wall | Wall or GlowQuartzVein | Dark wall with green mineral seam and pale green tips |
| gravel | Rubble | Broad low dotted field of raised contrasting stone cubes |
| chest (plain native alias) | Chest | Low golden plank chest with bands and closure |
| barrel (plain native alias) | WoodenBarrel | Compact wooden staves with bands and bung |
| mushroom-ring (plain native alias) | MushroomRing | Five small gold/pale fungi with stems and stepped caps |

Terrain must match both its exact blueprint and saved VisualID. The exact
plain containers and mushroom ring use aliases only when VisualID is empty.
Only the active glade may use these recipes. Hidden, removed, foreign-zone,
wrong-blueprint and wrong-VisualID owners must refuse the glade profile while
ordinary fallback stays available. Source library stores exact measured mesh
bounds/triangles and persistent prefab identities. C13's original creatures,
original humanoid rigs and real equipment are reused with scoped actor scaling.

New library/material/mesh/prefab assets will be additive. The material uses the
existing fog/light shader with a private compact palette. Renderer registration
occurs once at Bind; batching adds at most one glade material mesh per dirty patch.
The already-voxel registry prevents accidental rebaking or false missing-mesh
reports. No shared catalog/model/texture is recolored.

## Gates

Write and run failing native/art-source tests before implementation. Verify exact
40 unique terrain/prop IDs plus eight scoped actor meshes; variant bounds/triangles, material/UV/palette agreement, ownership,
hidden/removed/reskin/foreign-zone counters, allocation-safe cached identity, and
third-material batch admission/rejection. Record source/read-only hash evidence
for unrelated content. Native adoption and screenshot comparison remain parent
owned; inspect actual gameplay and world-only comparison frames before accepting
palette, plant scale, wall height, short shadows or composition. Tests cannot
prove the supplied image was matched.

## First-pass implementation log (historical 28-model kit)

`ReferenceGladeArtTests` first ran in Unity with24/24 expected assertion failures
because no kit existed (parent receipt `ReferenceGlade/native-art-red.xml.gz`).
Five offline source tests also failed on the missing source recipe; after writing
the deterministic generator they pass. Source evidence is in
`ReferenceGlade/Art/source-{red,green}.log`.

`ArtSource/ReferenceGlade3D/build_kit.py` generates28 combined-cuboid recipes in
`kit.json`. The source tests check exact IDs, real variant differences, positive
volume, native-cell bounds, low height hierarchy and16 distinct palette colors.
A native editor importer validates the full source before asset writes, builds a
private sixteen-swatch palette/material and28 measured meshes/prefabs, then
validates and publishes the library. It uses the established offline voxel kit
workflow; Blender is unnecessary for these deliberately cuboid assets.

The seven families now resolve only for exact native blueprint/VisualID matches
inside `ReferenceGladePlan.IsActive`. Dark ruins allow Wall or BrokenColumn;
lit walls allow Wall or GlowQuartzVein. This preserves actual solid ruins and
avoids manufacturing dozens of harvestable quartz rewards. Segment orientation
reads the parent's saved `ReferenceGladeQuarterTurns` property and normalizes it
to0–3 without modifying the entity. The renderer never adds LightSource parts.

The library supplies cached model IDs and actual immutable asset references.
Presenter Bind registers the private material once. The existing patch builder
now has one additional material batch, preserving ring palette/water and pilot
floor groups. The glade mesh follows the same staged replacement, failure cleanup
and Dispose paths as those groups. The already-voxel registry recognizes all28
meshes; removed native ground suppresses cosmetic fallback ground in this zone.
Unchanged fingerprints retain patch meshes on repeated FOV/camera updates.

An additional16-case native adversarial fixture covers foreign-zone lookalikes,
cached identity/range refusal, a changed managed world biome, and a real floor
removal. The latter first proves a triangle covers the chosen cell, removes native
owners, then checks the rendered geometry has a hole; it also checks stable
revision on a second unchanged refresh. This avoids claiming a hole merely
because the entity list is empty.

## First-pass sweep corrections / gates

- Parent authority intentionally supports bare standalone graphs at the exact
  address; managed graphs require their own current Spread/no-POI map. An early
  private test assumption that every bare graph must be refused was discarded
  before execution. The actual negative test changes an attached manager's biome.
- The reference's charcoal ruins need genuine blockers. Wall is permitted for
  dark-ruin/lit-wall styles; BrokenColumn remains a passable remnant. Only two
  explicit quartz endpoints are harvestable in the parent world plan.
- Native import, all40 art cases, existing batch regressions, screenshot palette
  comparison, actor scale, lighting/shadows and actual input acceptance are still
  pending. No visual match or performance acceptance is claimed by source tests.

## Files owned by this art slice

- New `ArtSource/ReferenceGlade3D/{build_kit.py,kit.json,test_kit.py}`.
- New `ReferenceGladeVoxelLibrary.cs` and its metadata.
- New `ReferenceGladeVoxelKitBuilder.cs` and its metadata.
- New `ReferenceGladeArtTests.cs`, `ReferenceGladeArtAdversarialTests.cs`, metadata.
- Modified ring library/catalog auxiliary lookup, native recipes, presenter Bind,
  voxel registration and ground patch material batching.
- Additive `Assets/Resources/ReferenceGlade3D` generated only by the explicit
  parent-run native import. Existing world/source assets remain independently owned.

## Native import / source review update

The native explicit import succeeded:28 models,533 source cuboids and6,396
triangles. The report source hash exactly matches the preserved recipe contract.
An optional Blender review gallery of these exact cuboids was generated and
inspected (`blender-kit-gallery.png`); the source retains its editable
`kit-review.blend`. It shows the intended teal ground/ruins, pale stalk clusters,
bright small green tufts, gray low masonry, green inset lights and scattered
gravel. This studio view uses different illumination from Unity and therefore
does not close the actual scene visual comparison gate.

### Native focused tests and offline visual review

The parent's integrated native259-case run passed all24 `ReferenceGladeArtTests`
and all16 `ReferenceGladeArtAdversarialTests`; raw authority is
`ReferenceGlade/native-integrated259.xml.gz`. Overall that run was258/259 because
of an unrelated traveller assertion, so it is not described as an all-green
integration sweep. The two initial native launch attempts stopped at their
new-game precondition and produced no valid glade composition screenshot. They
do not constitute visual acceptance.

A second offline comparison against the supplied reference confirms the intended
relative scale: low bright tufts, taller pale branching clusters, low gray
masonry and dark teal ground. Potential visual refinement remains: six broad
dark-ruin pillars may read chunkier than the reference's fractured spires, and
the12-brick gray wall may need finer surface detail. These are hypotheses from
the gallery, not a reason to alter gameplay geometry or global rendering. The
first valid native scene image should determine whether localized model detail
is necessary before acceptance.

### First native image rejected; scoped refinement plan

The first real glade frame, `Native/ecfc6b5315a1480c8907082293de972c/03-world-only-full-reveal.png`,
is **not visually accepted**. It proves native models render, but the ground is
flat/uniform green, pale stems are overbright, grass separates into tiny dots,
gray walls are thin strips, shadow depth is weak, the player is oversized, and
Warden/Villager still use fallback sprites. The full-reveal image is explicitly a
composition check, not ordinary player FOV.

Verified causes/seams before refinement: the presenter unconditionally borrows
the ring exposure2.2; the palette material has ambient0.7. The owned surface sun
has shadow strength0.55 and normal bias0.15, which is large relative to our0.07
stems. Its35-unit altitude puts the far side of the view near the existing
50-unit shadow distance. The material shader already supports actual URP main
light shadows; no new shader or global pipeline setting is needed. The current
55/-35 sunlight points shadows toward the upper-left of the native frame, while
the supplied reference calls for lower-left. Actual native images must verify
these hypotheses after the scoped settings change.

Plan: retain seven native families with fine fractured ruins, connected taller
green tufts, low mottled ground panels/readable specks, thicker staggered masonry
and larger raised gravel. Add four chest and four barrel variants, for36 models
and a24-color local palette. Borrow original ring humanoid rigs for two exact
local NPC identities; reduce glade actor instances (including children/gear and
picking bounds) without changing any native body, global model, statistics or
collision. All source renderer/visibility/ownership gates remain authoritative.

Apply lower ambient/exposure, lower shadow bias, stronger lower-left shadows and
a closer owned world camera only when the actual glade authority is active.
Preserve the existing surface constructor and defaults for other biomes. The
closer camera retains the same ground projection and parallel picking ray; the
ordinary camera is borrowed and untouched. Respect low-detail shadows-off.

Six refinement source tests ran RED against the real first-pass recipe; the
12-case native profile fixture ran in the parent's RED window: 11 failures
and one ordinary-lighting control pass.
Counterchecks pin ordinary-zone lighting/scale, original materials, glyph
refusal, hidden/removed owners, native equipment ownership and real picking.
The art-detail tests are reproducibility/intent checks, not substitutes for
visual acceptance of depth, shape and lighting.

### Independent authority review / added RED gate

🟡 A changed managed world map could invalidate the glade after Bind while
leaving its cached surface, local material family and smaller actors alive.
The recipe guard alone is insufficient: the presenter's same-zone Bind fast
path, Refresh and ordinary frame must also compare the authority used to build
that surface. Three additional native cases now flip the actual attached map
Spread→Sodden→Spread through those three entry points. Their RED run precedes
the repair; both transitions must rebuild, preserve native owners and restore
ordinary-zone scale/lighting when inactive. Reviewer: combat_density, read-only.

The first unfiltered native sweep also exposed census/private-fixture drift:
32 current skins,396 prefabs,410 source-mesh bindings (measured from the adopted
catalog) and278 generic meshes now include four new original creature rigs.
Only pins/test names change; every source→fresh-bake geometry, skeleton, bounds,
UV/water and whole-object palette check remains. The adversarial adapter fixture
uses its private constructor's explicit `false` glade flag for its ordinary
zone; no production compatibility overload or weakened validation is added.
This does not certify those checks until their native rerun reaches the bodies
previously blocked by setup pins.

### Refinement publication after RED

The three authority-transition cases reproduced the stale surface in the real
editor before repair (`Art/native-authority-red.xml.gz`). The presenter now
records the glade authority used at Bind, refuses to display a stale profile,
and rebuilds the whole owned surface through same-zone Bind, Refresh or the
ordinary frame when that authority changes. Both the gaining and losing paths
retain the actual native graph. Failed bind attempts retain their recorded
profile so they do not retry a missing resource every frame.

Published the refined 36 models / 2,047 cuboids / 24,564 triangles and local
24-color palette, scoped small actor instances and detailed real chest/barrel
recipes. The same 11 source tests pass from the shared source directory after
publication. The final editable Blender gallery was inspected: finer random
stone shades replace the earlier regular checkerboard, grass forms connected
clusters, and the eight wooden props have visible planks, bands and closures.
This is still an offline studio view; it does not establish native shadow or
scene fidelity. The native importer receives a new refinement receipt path so
the first-pass evidence remains intact. Exact published files and hashes are in
`Art/refinement-published-files.json`.

The native full-sweep failures also identified a real missed original-enemy
adapter: the Wellmeet warren now spawns `DirtGnome`, while the renderer retained
the old disguised enemy identity. The repair requires the exact current
blueprint, glyph `g`, and `warren_gnomes_routed` kill fact with amount one.
Existing wrong-glyph/wrong-fact counterchecks remain unchanged. Census and
private-constructor fixture changes described above are now published for a
native rerun; they are not a claim that the formerly blocked mesh/palette
assertions have passed.

🧪 Remaining gates: native import of all 36 models, all 15 profile cases,
full mesh/palette and adjacent rendering regressions, and a real scene visual
comparison plus performance observation. Parent owns those editor windows.

Independent post-publication review (combat_density, read-only) found the authority
repair covers the same-zone Bind, Refresh and frame seams and suppresses a stale
profile immediately; no further concrete P0–P2 issue was identified. This review
did not execute Unity and does not replace the pending native GREEN gate.

The parent native refinement import succeeded with no compile errors. Its durable
receipt (`Art/native-kit-refinement-import.json`) agrees with the published source
SHA-256 `59c6410516a10a7063622b83b06addd32eaf1e1a8e9390b4c8b06c270bcfe22a`,
36 models, 2,047 cuboids, 24,564 triangles and all 24 palette entries. The exact
owned-new manifest has been refreshed after the eight prop meshes/prefabs and
their metadata were generated. Native assertions and image review are pending.

### Second native image rejected; third-refinement sweep

The integrated native run passed all 580 affected cases
(`Integration/native-refinement-and-integration-green.xml.gz`). The 60-second
native observation in `Native/49bc88b781e54ba080da26d1408cd0e8` passed 12/12
checks with zero errors, 9,518 frames over 60.163 seconds, mean 6.321 ms and
p95 7.617 ms. Those are editor observations, not a released-device guarantee.
The actual world-only image is nevertheless **visually rejected**: actor fronts
are nearly black, pale plants have bright thin tips/dark stems, green plants read
as low mats, nine small floor panels create a grid, and masonry reads as a ladder.

Verified corrections before the third refinement:

| Observation | Source cause | Bounded correction |
|---|---|---|
| Two large brown shapes resemble crude actors | They are real MushroomRing owners at (34,19) and (50,21), using the old ring-mushroom-ring terrain model | Add four small detailed mushroom-ring models, exact glade-only owner alias; preserve harvest/membership |
| Pale twigs / green mats | 0.085-wide reed stems include dark swatch6; five heavily branched grass stems make a wide crown | Wider pale stalks/branches; three taller open green sprouts |
| Checkerboard floor | Each cell has nine independently shaded square surface panels | One quiet full-cell surface with raised specks; no alternating top panels |
| Front faces black | VillageLitColor multiplies SampleSH(normal) by the 0.30 ambient setting; scene ambient is dim | Supply a constant soft ambient SH probe only on owned glade renderers, using the existing diagnostic's CustomProvided/property-block API; no shader or global ambient changes |
| Gray ladder | Narrow wall cross-section and repeated broad top rows | Broader dimensional masonry with finer irregular top stones |

Five new source contracts failed against the imported second-pass recipe before
any third-pass geometry edits (`Art/source-native-feedback-red.log`). The changed
visual direction supersedes the earlier numeric ambient/point-filter and
nine-panel-ground assumptions; those tests must describe the new verified
intent, not block a correction to an image already rejected by inspection.
Native owner/fog/picking/ordinary-zone counterchecks remain requirements.

The third ambient/owner fixture ran in Unity: four expected failures and one
foreign-zone control pass (`Art/native-third-refinement-red.xml.gz`). Private
source revision now passes all 16 checks; one repeated grass variant was caught
by the existing variant-distinctness check and corrected before publication.
The latest offline gallery shows wider pale branches, three upright green
sprouts, broad masonry and five small mushrooms per real ring owner. Again, this
is source review, not the final native lighting result.

The parent also requested readable green/pale player, pale local people and
rust/ochre original enemies. Ambient fill cannot change teal hue into green.
The approved plan therefore adds exactly eight persistent UV-only mesh variants
inside ReferenceGlade3D for ring-player, ring-sien, ring-nam and the five Marlback
rigs. The importer copies the actual adopted voxel mesh channels; only UVs
change into the local palette's now-unused former floor swatches. Head/hand bone
weights give the player a pale head/gloves. The actual prefab rig, clips, sockets,
equipment and mesh geometry remain borrowed. Ordinary-zone and global mesh
paint stay unchanged; no runtime vertex copies or global atlas edits. Eight
native RED cases precede that addition and retain complete geometry/bone/UV,
real-picking, ownership, hiding and ordinary-zone counterchecks.

### Third publication / review

Eight actor-paint cases failed in native Unity exactly on the absent scoped mesh
assets (`Art/native-actor-paint-red.xml.gz`). That receipt contains 76 cases overall:
eight expected art failures, one separate C12 parser failure and 67 passes; it is
not described as an all-green run. After all RED evidence, the third candidate
was published. Shared source checks pass 16/16; the source contains 40 measured
terrain/prop models, 2,467 cuboids and 29,604 triangles, plus eight actor meshes
copied only during native import. The exact changed source files/hashes are in
`Art/third-published-files.json`; source contract and final offline gallery are
`Art/source-third-contract.json` and `Art/blender-kit-third-gallery.png`.

The parent independently reviewed the importer, actor cache, owned ambient
property blocks and presenter seams without finding a blocking issue. A separate
read-only review (combat_density) also found the ambient lifecycle coherent:
probe array built once before models, other renderer/indexed properties kept,
dynamic gear prepared through the same route, and authority reversal creating
fresh owned renderers. The glade-only actor copies are registered as already
voxel geometry, avoiding another bake or false missing-mesh diagnostics.

No global roster count or whole-world two-color test was relaxed for these local
assets. The actor variants preserve the existing two-color rule themselves;
geometry, bindposes, bone weights, submeshes, real picking, hidden owners, gear and
ordinary/global paint are checked by their eight native cases. Publication alone
does not establish their GREEN result or visual acceptance.

The third native import succeeded (`Art/native-kit-third-import.json`), matching
source SHA-256 `c6261225b872b67dc5b41f5abcddf0bc66f8b193d1345614b58fbb9294edb543`
and explicitly listing all eight scoped actor assets. The owned-new manifest now
contains 403 exact paths after generated mushroom/paint assets and metadata.
Independent read-only actor-paint review also found no concrete P0–P2 issue in
source preflight, channel preservation, exact source matching or preparation
order. Native assertions and screenshot inspection remain pending.

The first third-pass native selection reached 239 cases: 238 passed and one
scale pin failed. `ArtScaleMatchesLowReferenceHierarchy` still capped grass at
0.38 from the rejected hedge-mat design; the imported upright sprouts measure
0.462500006. The new source contract already required 0.40–0.50, so the native
pin now uses the same tighter lower/upper bounds. No production or mesh changed.
All other size controls remain. The parent-reported RED was saved before this
narrow repair in `Art/native-grass-height-pin-red.json`; parent archives the raw
NUnit authority. Overall native GREEN and the actual image are still pending.


### Third native GREEN; fourth visual pass remains open

The actual NUnit receipt `Art/native-third-refinement-green.json` and its paired
XML establish **239/239 GREEN**, including all eight actor-paint, five ambient,
fifteen profile and twenty-four native art cases. The finite live route at
`Native/20936d94bbe147f48c03d444dbae2486/report.json` passes **12/12**, with zero
callback errors and 9,842 frames over 60.208 seconds: mean 6.117 ms, p95 7.010 ms.
These are editor observations. They establish the declared native contracts and
route, not released-device performance or visual acceptance.

The third world-only and arrival images were independently inspected against the
user's reference. Readable green/pale player, pale people, rust/ochre Marlbacks,
small native mushrooms and quieter ground are improvements. **The image still
needs refinement**: pale reeds form horizontal stool-like silhouettes, grass is
thin, floor specks lack raised square volume, and humanoids project at roughly
27 pixels high versus roughly 46 in the reference. The ground's most frequent
RGB is already close (11,53,45 versus 11,53,47); its large uniform region still
lacks the reference's soft patches. Passing tests do not close this visual gate.

Pre-implementation sweep and approved bounded fourth scope:

| Verified source / observation | Planned correction | Preserved countercheck |
|---|---|---|
| Seven reed stems have 0.145-wide horizontal branches and broad top caps | Tapered uneven vertical shoots, upward offshoots and narrow tips; no connecting canopy | Pale palette, real-cell footprint, four distinct variants, existing native owner/fog rules |
| Grass central stems are 0.072 wide and side blades are about 0.05 wide | Chunkier rising blades and shaded bases, still below 0.50 cell high | No flat hedge mat, real footprint and height hierarchy |
| Surface pebbles are only 0.06 wide | Irregular 0.09–0.11 raised square pebbles with visible sides; quiet single ground surface retained | No checkerboard top panels; no global shader or atlas change |
| Actual adopted rigs are uniformly limited to 0.9 height / 0.68 width for glade humanoids | Measure projected owned-rig bounds and target approximately 1.35 height with proportional width, retaining the same rig/gear/picking root | Ordinary zone full scale, FOV, removed owners, dynamic gear, lost/regained authority |
| Ground hue is close but broad contact variation remains absent | Defer additional ground treatment until the above actual native image is inspected | Do not claim that larger pebbles solve broad soft ground shading |

Fourth source and native sizing contracts are prepared in a private staging
folder while the parent runs the full native foundation suite. No fourth Assets,
source kit, material or model is published yet. All-six original-enemy live
appearance/animation/gear acceptance also remains pending as a separately labelled
staged demonstration; this route did not establish every original creature role.


The unfiltered third-foundation native sweep ran 16,996 cases: 16,986 passed and
10 failed, all the same `WarrenQuestMobRequiresItsRealDeathFact` setup in four
regional rendering fixtures (`Integration/native-full-third-before-quest-pins.*`).
A broad original-enemy rename had made these setups create a MarlbackScrabbler
with glyph `g`; the real independent quest blueprint is now DirtGnome. Only the
four fixture bodies were corrected after that RED. Their amount-two and unrelated
fact rejections are preserved, with additional changed-glyph and disguised-Marlback
rejections and restored-valid identity proof. No production rendering changed.
The full native rerun remains the foundation gate; fourth art stays private.
