# Reference glade art — C14

Status: the user explicitly approved the current native appearance and requested
whole-biome expansion on 26 September (C15). The seventh grass refinement is
imported and its four native geometry checks pass; the kit has 40 terrain/prop
models and eight scoped actor meshes. Contact shading is a separate pending
refinement with 10 native RED cases and six passing controls. The historical
iterations and their rejected images below remain evidence, not current status.
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


## Fourth refinement — vertical plants and readable native figures

The third foundation is committed (`6b3f8939`) after 16,996/16,996 native tests.
Its image remains rejected for visual fidelity, as recorded above. Fourth source
RED preceded geometry changes: five checks, four intended failures and one
unchanged-palette/prop control pass. Native actor RED then ran eleven cases:
ten sizing failures and the unchanged passive-moth control passed
(`Art/native-fourth-profile-red.*`). These are actual assertion failures, not
transport or compiler failures.

The fourth source changes exactly 16 of 40 model recipes: all four variants of
ground, pale reeds, green grass and gravel. The 24-color palette and other 24
models are parsed-object identical (`Art/source-fourth-diff.json`). Reeds are
seven irregular upright tapering shoots with thick bases and narrow tips; broad
horizontal canopies are removed. Grass has thicker rising blades and small shaded
bases. Ground/gravel stones have visible raised sides at .09–.11-cell widths.
The kit has 2,479 cuboids/29,748 triangles; all 21 source tests pass. The offline
Blender gallery is a review aid, not native visual acceptance.

Scoped scale uses actual adopted bounds and only changes the three exact
humanoid rigs (Player/Sien/Nam) and original Marlbacks within active glade
authority. Humanoids cap at 1.35 height/1.10 width; Marlbacks retain low/wide
silhouettes capped at .78 height/1.02 width. Other creatures retain the previous
caps, including the passive moth. The real owned rig root is scaled; animation,
gear children, body ownership, native movement and after-scale picking remain
on the existing path. Existing profile upper-bound pins are updated for this
intentional hierarchy; the new fixture also pins lower/projected bounds,
dynamic gear/fog, hidden/removal and authority reversal.

Fourth publication does not import generated native assets itself. Root should
invoke `CavesOfOoo.Editor.ReferenceGladeVoxelKitBuilder.Run` with the durable
`Art/native-kit-fourth-import.json` report path, then run the affected native
rendering fixtures and inspect actual arrival/world-only images. Improved
silhouette, projected actor readability, contact shadows, animation and overall
reference fidelity remain open gates until those observations. No frame-time
or visual-acceptance result is inferred from source tests or offline Blender.

The separate all-six staged ordinary-zone driver is also published for native
compilation. Its exact scope and state-restoration contract live in
`Docs/DENSITY-ORIGINAL-ENEMY-NATIVE-AUDIT.md`. It is not a natural encounter,
player-controlled combat or acquisition/balance test.


### Fourth native sizing correction

The first native selection executed148 cases:140 passed, two liquid-menu RED
cases belonged to the separately pending C3 work, and six art assertions failed
(`Art/native-fourth-before-sizing-repair.*`). All humanoid, gear/fog, authority
and unchanged moth controls passed. Ground height was0.100000009 against an
intended0.10 bound; the scale test now allows one microcell for float bounds
recomputation, retaining all substantive hierarchy limits.

All five adopted Marlback skin bounds were .852–.861 wide because the original
.78 height cap was the limiting dimension. Increasing only the height cap to
.90 preserves uniform proportions and yields a predicted .983–.994 width. The
required .95–1.03 width assertion remains unchanged; the height upper bound is
now .91, and width must still exceed height. This is an actual scale repair,
not a lowered readability requirement. Native verification and visual review
remain pending.


## Fifth refinement — broad floor variation and block-finger reeds

The fourth native rendering selection passes 140/140 after the measured
Marlback repair (`Art/native-fourth-refinement-green.*`). Its actual route
`Native/5145a7f823024f75b65b911c240d4f93/report.json` passes 12/12 with zero
callback errors; the 60.047-second editor sample contains 9,398 frames, mean
6.389 ms and p95 8.245 ms. These editor observations are not a player-build
benchmark. The arrival and world-only images were viewed against the reference:
figures and grass improved, but uniform floor luminance and repeated cone-shaped
reeds still fail the visual gate. Existing soft shadows alone did not produce
the reference's broad ground variation.

The pre-implementation sweep found a narrow owner-safe seam: the existing
palette fragment already knows world position, normal, transient ownership and
native fog; the active glade registers a separate borrowed material whose surface
clone can carry a default-zero parameter. The water fragment, shadow/depth paths,
scene layout, camera, native light/FOV, actor paint/scale and all ordinary material
settings remain on their existing paths.

Actual source RED was three cases: two silhouette failures and one unchanged
36-model/palette control. Actual native RED was nine cases: five intended
failures (three missing material/profile controls and two GPU uniform-output
assertions) with four real rendered/visibility controls passing. Both lit and
remembered positive probes produced nonblack baselines, so this was not a black
render or shader-import failure (`Art/native-fifth-ground-red.*`). The small
probe calls the actual production fragment and restores its shader globals and
render target in `finally`; paired PNGs and numeric receipts are under
`Art/GroundGpu/`. A sampled GPU plane proves shader behavior, not scene fidelity.

Implementation adds static, smooth world-position color variation at five- and
nine-cell scales. Only the owned glade palette enables strength .24, bounded to
at most +/-12% albedo variation; other source/cloned materials default to zero.
Only upward faces near ground (full below .10, fading out by .16 cell) vary.
Transient rigs/gear are excluded. Native fog clips before any color output, and
remembered cells use the same static albedo variation with their saved brightness.
This is ground color variation, not an ambient-occlusion or additional-shadow
claim. No time input or per-frame mesh/color copying is used.

Exactly four reed recipes change: seven unequal rectangular stalks with flat
block tops and only two or three asymmetric rising branchlets. Other 36 recipes
and all 24 palette entries are hash-identical. The old narrow-tip cone assertion
is deliberately replaced after the rejected image; dominant vertical mass,
independent narrow stalks and no horizontal canopy remain pinned. All 24 source
checks pass; the kit has 2,415 cuboids / 28,980 triangles, source SHA-256
`bc548562c4911fba668543188f57a6abd588b58aaeab6508b9fa99edcf5c3310`.

Self-review: independent read-only review found no concrete P0-P2 issue in shader
ownership, fog ordering, static noise or GPU-test isolation. Native GPU GREEN,
material state counterchecks, rebuilt persistent meshes, actual fifth scene
captures and visual acceptance are still pending. Parent owns native import and
all Unity runs. Exact publication is `Art/fifth-published-files.json`.

## Fifth native focused gate

Actual Unity native combined selection:422/422 passed, no skips,70.435s (Integration/native-c10-c11-fifth-focused). Includes all nine ground-profile/GPU cases. Initial Metal shader compilation caught reserved identifier point; renamed only that local parameter to noisePosition, then compilation and the graphics cases passed. Source test/compiler assumptions alone did not catch this. Fifth native import uses kit SHA bc548562c4911fba668543188f57a6abd588b58aaeab6508b9fa99edcf5c3310. Visual acceptance still requires the new Play capture.


Fifth native follow-up: Metal rejected `point` as a reserved HLSL parameter
name (`Art/native-fifth-shader-compile-red.json`). The parent renamed only that
parameter to `noisePosition`, then confirmed shader compilation and reported
422/422 affected native cases GREEN, including all nine GPU/profile cases. This
actual compile repair is included in the publication manifest hash; offline
source tests had not established shader-language compilation.

The actual fifth image at
`Native/6700565ba4e3401bbe95dbc1daa19a65/03-world-only-full-reveal.png` was viewed
against the reference. Rectangular reed fingers improve on cones, but the visual
gate is still open. The floor still reads flatter than the reference; the native
humanoids look like pale head blobs with little separated limb silhouette, and
grass has repeated fern-like branches rather than simple upright shoots. The
source explains the anatomy: an approximately .76-wide/.78-tall hood covers a
.78-wide coat; short .30 trousers sit mostly beneath the coat. Painting all
Head-bone vertices pale emphasizes the whole hood. The camera already uses an
oblique 56-degree compensated projection, so arbitrary exposure changes cannot
repair those proportions. A sixth scoped humanoid/grass design is proposed;
no sixth implementation or visual acceptance is claimed here.

## Fifth capture acceptance boundary

Actual fifth captures are retained under Native/6700565ba4e3401bbe95dbc1daa19a65.
An asynchronous script reload occurred during the performance section, clearing
the driver's nonserialized audit state. Root aborted this invalidated run;
the abort receipt is Native/de244c006ac3451e9e9531e3e620cab8. No fifth gameplay
or performance pass is claimed from it. Captures were inspected as visual
evidence only; exact scene/start, seed, save preference/override and input/background
settings were restored and recorded. The captured editor log corroborates the
reload. Future Play runs wait for completed full asset import and native tests.

The image improves reed silhouette but is still not accepted as a match.
Humanoid head/coat proportions obscure limbs, grass still reads as little ferns,
and broad ground variation is weak at scene scale. A sixth scoped body/grass
iteration is private; global camera/palette changes are not justified by this
evidence alone.

## Sixth silhouette refinement — native verification pending

The fifth actual image and source anatomy established two remaining silhouette
problems: a large pale hood dominates each humanoid, and repeated side branches
make the green tufts fern-like. This slice leaves camera/projection, scene layout,
lighting, material/fog fields and all other biome assets on their current paths.

Before production, three actual native morphology cases failed on head/body
height about .60 (required below .28 width / .26 height); ordinary-authority
recovery and real gear/visibility controls passed. The parent saved this RED in
`Integration/native-moth-door-steam-sixth-gates*`. Three source cases separately
reproduced two grass failures, with the other 36 recipes/palette passing unchanged.

Only the exact player/Sien/Nam scoped body meshes are rebuilt as compact cuboid
forms: small pale head, raised short torso, separated legs/boots and independent
hands at the real equipment sockets. The importer derives mesh bind space and
side orientation from the actual original prefab and preflights the exact nine
source bones before publishing actor assets. It retains source bindposes,
skeleton/animation/socket objects, palette and material ownership. A serialized
flag explicitly distinguishes these three geometry variants from the five
strict UV-only Marlback paints; those five retain their exact mesh controls.
The renderer preserves its source animation envelope, expanding only to include
the new bind-pose geometry. Persistent borrowed meshes replace runtime references
once per owned instance; no per-frame geometry copy or global mesh mutation.

Exactly four grass recipes now have seven unequal upright fingers with shaded
bases and short bright tips. Other 36 recipes and all 24 palette entries are
hash-identical. Source RED/GREEN, preservation hashes and offline compiler output
are in `Art/Sixth/`; all 27 source tests pass. Offline C# compilation has zero
errors against current Unity references (expected duplicate private/source type
warnings do not prove editor import). Independent read-only review found no concrete P0–P2 blocker in bindspace,
borrowed-source ownership or conservative animation bounds. Actual native import, morphology/rig/gear
GREEN and the next real visual comparison remain pending. No sixth visual or
performance acceptance is claimed.

## Sixth native buffer failure and bounded importer repair

The first actual native selection ran242 cases:236 passed, six failed on the
three humanoid forms and their three geometry controls. Native import had run
the new builder, not an old assembly: all three library entries declared authored
geometry, and the assets contained new submesh metadata (384vertices/576indices)
while their native vertex buffers still held the old2116vertices. Repeating the
import reproduced the mismatch. Evidence is `Integration/native-steam-sixth-first.*`
and `Art/Sixth/native-buffer-red.json`; no sixth image was accepted from these
invalid mixed assets.

The bounded repair writes the already-preflighted form arrays directly through
Mesh geometry APIs into the owned persistent asset, after Clear(false), then
sets rigid weights/exact source bindposes and recalculates normals/bounds. It
removes the temporary authored native mesh and the ineffective CopySerialized
step for those three forms. The five unchanged-geometry Marlback UV copies keep
their existing path. The importer now reports actual actor vertex counts.

Two added native controls exercise fresh and previously populated owned assets,
write/save/reimport each twice, check384vertices/576indices and stable GUIDs, and
verify the borrowed source arrays and bindposes. They delete only their uniquely
named temporary asset in finally. These two new controls were written after the
six actual behavior failures; they are not represented as separately executed RED
against a Fill method that did not yet exist. Offline compilation is error-free;
parent-owned native repair verification and actual visual acceptance are pending.

## Sixth visible-size normalization correction

After the direct buffer repair, actual native484-case selection had481 passes
and three failures: all new fresh/reimport controls and actor-paint cases passed,
but visible humanoid heights were .9742575 (player) and .9491963 (local people),
below the new1.05 lower bound (`Integration/native-sixth-c11-second-red.*`).
The presenter was normalizing from renderer.bounds, which correctly retains a
large imported animation/culling envelope but does not describe the compact new
body's visible proportions.

Only exact flagged three humanoid mesh references now use transformed mesh bounds
for initial scale and the pick collider. The actual SkinnedMeshRenderer.localBounds
is retained unchanged for conservative motion culling; source rigs/sockets and
all other actors' measurement paths stay unchanged. Existing profile checks now
measure actual visible vertices for those three forms, retaining1.20–1.36 height
and existing width/projection bands. An old scale<.8 implementation pin becomes
scale<1, since source culling padding no longer determines visual scale; exact
visible height/width, real gear ownership and ordinary-zone scale remain pinned.
The morphology cases additionally check that every old culling corner remains
inside the owned renderer envelope and that picking follows actual visible height.

Offline compile passed after including the presenter's existing internal helper
sources in the private compiler assembly. Native GREEN, actual animation and
screenshot acceptance remain parent-owned pending gates.


## Sixth native image, gameplay and restoration receipt

The actual run `4281297b663744e0bc6de61ec5d80201` completed all 12 existing
checks with zero reported errors. Its 60.245-second movement sample recorded
10,647 frames, mean5.658ms and p956.432ms in this editor session; this is not a
player-build benchmark. `Native/4281297b663744e0bc6de61ec5d80201/report.json`
records the route and stronger native checkpoint proof. Its `restoration.json`
shows exact pre/post scene, start scene, seed, save override, last-game pointer,
background and input-settings equality. The complete236,449-byte actual editor
log range is preserved with SHA256 in `unity-log-range.json` and
`unity-log-segment.log.gz`. The current combat checkpoint observes casualties
without proving player attribution; a stronger attribution check is being
prepared separately.

The sixth screenshot is retained as evidence, not final visual acceptance.
Humanoid limbs and head proportions read more clearly. Comparison with the
reference still finds an overly uniform floor, thin grass and sharply pixelated
pale reed caps with little soft contact shading. The next iteration will measure
actual material activation, shadows and the owned camera sampling path before
changing settings. No global pipeline or gameplay changes are implied.

The subsequent125-case selection (`Integration/native-sixth-poured-renderer-red.*`)
found six stale glade test assumptions: three still measured the old padded
projection band1.45–2.15, whereas actual visible projection is1.1781193cells; three
authority-recovery cases still required root scale<.8, whereas the new body
requires .82317102. Ordinary exact unit scale already passed. The private repair
keeps visible height1.20–1.36 and width<=1.11, checks the analytic projection
with the unchanged56-degree camera plus a visible1.10–1.30cell band, and pins
exact ordinary versus restored local mesh references. No production geometry or
render setting is changed to repair those measurement assumptions.

## Seventh measurement-first experiment

The sixth actual frame contains broad mottle variation, but only a few8-bit
levels. Source confirms the correct owned material reaches batched ground;
shadows are visible. Normal target resolution and pipeline render scale are1,
but the native camera/target have no antialiasing. The raw world capture bypasses
composite filtering. The reference has many more intermediate colors and softer
contact transitions; different content/compression makes a color census alone
insufficient to diagnose the cause.

The test-only `ReferenceGladeRenderMeasurementTests` now runs four actual camera
pairs: mottle0/current, shadows off/on,1x/2x resolved sampling, and medium/high
soft shadows. It writes matched PNGs plus material, camera, separate borrowed
and measurement target properties, pixel deltas and rough teal-floor statistics.
It restores only owned render state in finally and checks unchanged native
positions, version, tile state and ground build count. No shared pipeline,
material, camera geometry, humanoid form or seventh production setting changes.
These diagnostics do not themselves declare visual acceptance.

A private four-grass candidate follows actual source RED: three new cases had
two expected silhouette failures and one exact-preservation control pass. The
private revision widens the across-screen footprint from about.56 to.95cell and
reduces height from.438 to.42048, preserving seven fingers, depth and the other36
models/palette exactly. All30source cases pass. Four native asset-bound cases
are published for RED before any grass import; the source/meshes remain private.

Plan, source RED/GREEN, pixel-census limits and offline compile evidence are in
`Art/Seventh/`. Cold-eye caught misleading target metadata; the fixture now
records actual temporary-target values separately from borrowed production
values. Actual native measurements, grass RED and subsequent art decisions
remain parent-owned gates.

Measurement boundary: these pairs render the generated integration fixture with
its own fixed camera and full-reveal state. They diagnose the production path;
they do not replace the separately captured actual-game glade frame or its final
visual judgment.

### Seventh measured render comparison and grass source publication

Actual Unity selection `Integration/native-seventh-liquid-bed-door-material-red` passed all four diagnostic renders and the current corrected glade contracts. All four `ReferenceGladeSeventhGrassTests` failed against the old imported tufts, as intended. The paired images under `Art/SeventhMeasurements` were inspected directly: current mottle is active (2.04% normalized masked-floor delta); shadows are active (1.42%); Medium-to-High soft-shadow filtering changes only0.41% of masked-floor luminance on average. Rendering at2× and resolving to the same output visibly smooths stems and pale caps, but does not supply the missing soft contact footprints. These fixture renders are diagnostic; their720×512 composition is not the actual gameplay acceptance image.

The bare modal teal in the sixth actual image is already close to the reference (12,54,46 versus11,53,47). Flatness therefore needs local variation/depth rather than a blanket exposure reduction that would also darken readable actors. A local contact solution remains a separate design/test-first step; no seventh lighting, shader, global pipeline or camera change has been published.

Published the bounded grass source unit after its source2RED+1control and native4RED: exactly four green-grass variants widen to about.95 units and lower to.42048 units, retaining21 cuboids and seven separate fingers. Other36 models and all palette entries compare exactly with the prior source. All30 source tests pass. `Art/Seventh/grass-published-files.json` records the five source/doc hashes. Native `ReferenceGladeVoxelKitBuilder.Run()` adoption, the four GREENs and a real image remain pending. The three accepted humanoid forms and current sixth Blender-review snapshot are preserved.

### Native follow-up selection: 193/193 GREEN

Actual Unity job `4ccacd6728a04704aab763829b3c2f23` completed all 193 cases with zero failures or skips: 137 material/outlier/neighbor checks, 26 poured-liquid rendering checks, 4 seventh-grass geometry checks and 26 combat-witness checks. Raw XML is preserved at `Verification/DensityCompletion/Integration/native-seventh-liquid-material-combat-green.xml.gz`. This closes those focused unit gates; the separate Play routes and visual comparison are still required.

The actual seventh grass import completed (`Art/Seventh/native-build.json`). Native focused selection `Integration/native-seventh-liquid-material-combat-green` passed193/193, including the four imported grass contracts and all26 poured renderer cases. This closes the source/import tests, not visual acceptance: the full gameplay view still needs its next image.

The next private contact proposal is deliberately local. A glade-owned linear640×200 texture will derive bounded soft contact footprints from the real imported static meshes' ground-facing faces, with no actor/rig, native position or global pipeline change. Only current visible owners contribute; movement, removal and visibility changes reconcile with existing static recipes. The palette effect defaults to zero, affects live upward ground only, and is disabled in low-detail. Remembered/hidden/transient/raised geometry retains existing rendering. Disposal clears the owned binding before destroying only its texture. This is stylized contact shading, not SSAO or gameplay light. The source cache, contributors and raster writes have named hard limits.

Before implementation, a private16-case test fixture and dedicated actual-fragment GPU probe cover real reed contacts, broad-floor/elevated-face exclusions, movement/removal/hidden/foreign clearing, native/source preservation, low-detail/authority/disposal and live-versus-control GPU pixels. An additional diagnostic mode compares current.24 mottle with.65 under identical shadow-free camera conditions and records both values. Actual referenced-assembly compilation passes; native RED and production implementation remain pending. Private test hashes and detailed ownership plan are archived as `Art/Seventh/contact-private-tests.json` and `contact-plan.md`. The root paused editor automation because an ordinary user gameplay preview was active; no contact files have been published.

Contact publication advanced only its five test/probe/diagnostic files; manifest `Art/Seventh/contact-test-published-files.json` records them. The renderer integration and shader are still unchanged until native RED. In parallel, root authorized extracting the math into one reusable dependency-free helper. Against its explicitly empty new skeleton,21 CPU tests produced15 real behavior failures and6 controls; the implemented pure extraction/raster helper now passes21/21. Cases cover real downward winding, broad/raised/sloped exclusions, source preservation, rotation, bounded blur, edge clipping, overlap order, move/clear and atomic budget rejection. This establishes new-math TDD only; actual native geometry/visibility/texture lifecycle and image quality remain separate gates. Raw receipts and the initial skeleton are retained in `Art/Seventh/ContactCpu`.

A private math countercheck then exposed a real mirrored-yaw defect: the initial centred symmetric rectangle did not distinguish+90° from−90°. Four off-centre positive-Y rotation cases produced25cases/23pass/2RED; swapping the90°/270° transforms yields25/25GREEN. The paired yaw receipts are retained in`ContactCpu`. No Unity integration or shared source changed for this repair.


The user subsequently approved the current native look as the basis for an entire biome (C15), including an ordinary new-game start there. The seventh contact unit remains an unimplemented, executed-native-RED-pending refinement; prior image criticisms describe earlier comparison stages, not a claim that the user rejected the current scene. C15 source audit and scoped rendering plan are tracked separately in `DENSITY-SPREAD-BIOME-ART.md`. A cold-eye review of the private25-case pure contact helper found no concrete blocker after the yaw repair; native placed-mesh transforms, ownership, GPU output and actual scene quality remain separate gates.


Actual native contact baseline is now recorded in `Integration/native-contact-door-harvest-red`:16cases,10 intended failures and6 controls. Failures establish the absent native extractor, owned field/properties and zero live-GPU delta. The five render diagnostics passed. The stronger-mottle paired images were viewed directly: current.24→.65 changes normalized floor luminance by3.688% but offers no clear improvement to the user-approved look, so production remains.24.

The private contact implementation uses the already-tested pure helper, caches40 validated source meshes, shares the actual batching quarter-turn function, and reconciles only current visible static native owners. It allocates one640×200 linear owned texture; reused raster/upload arrays and explicit work caps bound updates. A stable signature avoids rerasterizing unchanged inputs. Owned material binding is cleared before texture/material disposal; no borrowed geometry/palette, actor rig, source placement, global render setting or gameplay light is changed. Actual-fragment shading is zero-default and live/upward/near-ground only. Low-detail or presentation loss sets strength to zero.

The full current runtime source compiles against native Unity references with zero errors;25 pure-math cases rerun GREEN. Four added native placed-geometry controls independently transform the real imported mesh with Unity's quarter-turn/translation and compare the field, bringing the fixture to20. Those four were added after the integration candidate and have not run RED; the earlier16 native cases supply the executed failing behavior evidence. Their compile passes, but native shader/ownership GREEN and actual matched screenshot remain pending. Exact candidate hashes and compile/math receipts are under `Art/Seventh/ContactCpu/integration-candidate.json`. Nothing in this paragraph claims the private candidate has been published or accepted.


Following root's explicit editor-idle window and two independent source reviews with no concrete blocker, the exact11 contact code/test/shader files are published. `Art/Seventh/contact-published-files.json` records current hashes. The CPU helper also processed all40 actual adopted mesh buffers decoded read-only from serialized assets: each fits the limits; four reed variants yield7 base rectangles and52–54 affected samples, including33–35 intermediate samples. This source-buffer probe does not substitute for native shader/placed-object acceptance. Native20-case contact fixture,25 pure controls under Unity, a matched real scene image and bounded frame profile are still pending; mottle remains.24 and C15 runtime has not been published.


Actual Unity contact selection is now179/179 GREEN, zero failures (`Integration/native-reference-contact-green`, jobc0a51df3b9d948e1b02c0999eed13e05). This includes all20 native contact/GPU/placed-geometry cases, all25 pure geometry controls and current glade regression/measurement fixtures. It closes the focused native implementation gate. Root's actual scene capture and finite performance run remain separate visual/runtime acceptance; this result alone does not declare the contact appearance accepted.


### Actual seventh contact scene and movement acceptance

Native run `02f3f2b73f8c4202822becfad85bd282` completed12/12, zero unexpected errors, with exact scene/preferences/input/save-root restoration and its full editor log range archived. The root directly inspected `03-world-only-full-reveal.png`: the dark teal ground, widened low green tufts, pale upright reeds, dark ruin mass, readable gray wall, green seam and approved small actors remain coherent; the new ground contacts do not obscure paths or wash out the palette. This frame is an explicitly full-reveal composition view. Actual walking, chest loot, wall blocking, finite harvest, save/load and zone exit/return use ordinary visibility and input.

The finite editor movement sample measured60.016seconds/15,511frames, mean3.869ms andp954.030ms. This supports a usable current editor scene, not a standalone-build benchmark, memory/allocation result, or causal speedup over the earlier6th run. Current glade visual/runtime acceptance is complete for this bounded contact unit. C15 entire-biome models, authority, default spawn and native coverage remain separate unfinished work.
