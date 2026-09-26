# Original enemy art — C13

Status: original bodies, sprites and native subset import complete; focused native
checks green. All-six live in-scene visual/animation acceptance remains open.
This is original Ooo art, not a Qud-parity feature.

## Scope and verified corrections

| Premise | Verified source | Correction / decision |
|---|---|---|
| Renaming the enemies finishes replacement | `build_ring.py` explicitly authors `Scaled_canine_head`, `Long_snapjaw_muzzle` and canine ears; pixel sheets also author jaws/ears | Replace actual body geometry and sprite silhouettes, not only strings |
| There are five existing ring bodies | Current ring catalog has only Snapjaw and SnapjawWarlord among 218 models | Replace these two bodies and add three distinct role bodies; preserve 216 unrelated FBX files byte-for-byte |
| Internal model IDs can be renamed harmlessly | Seven spell families store paths under the two existing ring rig IDs | Parent approved retaining the two nonspawnable asset IDs as compatibility keys; fresh sourceBlueprint, display identities and geometry become original |
| Old capture files should be renamed | Native ring snapshots and historical art receipts are evidence of earlier runs | Preserve them. Live builder explicitly translates captured legacy blueprint identities to current authored bindings |
| GlowMoth already has a bespoke ring model | No current ring binding exists | Add a small original GroveLanternMoth body and sprite, bringing the catalog to 222 models. It gains no gameplay light |
| A fresh full rebuild may overwrite the accepted polish | Accepted master/FBXs have a sculpt-normal pass, and the importer walks the full bundle | Build into private output, use the existing polish pipeline, adopt only six scoped model bodies and measured metadata; prove all unrelated runtime/source FBXs unchanged |

## Authoring plan

Marlbacks are low, broad bank-burrowers. Short blunt faces sit below a layered
shale-and-mud back, with grasping forelimbs/digging rakes. No canine muzzle,
pointed ears, mane, tooth display or human hood/coat silhouette remains. Five
roles retain the actual native equipment sockets and five action clips:

- Scrabbler: compact bare plates, open digging forelimbs.
- Gleaner: staggered back-plate storage pockets and small tool sheaths.
- Tunnelguard: asymmetric braced shoulder plates suited to its real polearm.
- Wallkeeper: patched digging harness and a broad central back plate.
- Breacher: heavier forward plates and squared shoulder ridges; its actual
  equipped weapon remains gameplay-owned rather than baked duplicate gear.

GroveLanternMoth has a small dark thorax and broad pale wings with three soft
bands. Animation reuses the existing avian rig contract, with moth anatomy and
wing action. Palette shading suggests thin wings; the opaque shared material is
not claimed to be physically translucent. It has no invented glow power.

## Verification / acceptance plan

Record RED bindings and original-body requirements before implementation. Hash
all scoped and unrelated source/runtime FBXs before adoption. Assert six distinct
current bindings, original short/wide body geometry, exact role identities,
rig clips/sockets and honest actor-glyph refusal. New blueprint bindings must
retain native entity ownership; removed/hidden/reskinned actors stay rejected.

Build/reimport the actual FBXs, recompute bounds/triangles from meshes, inspect
matched Blender front/side/overhead previews, then verify actual imported Unity
models and native in-scene views for all five roles and the moth. Preserve
rejected renders and raw receipts. Blender views cannot prove Unity materials,
lighting, animation, FOV or feel. Native tool/test windows belong to the parent.

## Ownership

This work owns creature art generators, scoped adopted art, live ring contracts,
EntityVisuals.json, EnvironmentSpriteRenderer, SpawnRing3DRecipes,
SpawnRing3DCatalog, EntityVisualCatalog and rendering tests/fixtures. The combat
owner handles gameplay blueprint IDs, source populations, dialogue/quests,
factions, narrow old-save migration and non-rendering tests. No commits or pushes
are performed by this subagent.

## Implementation / evidence (September 26)

The six original bodies now exist in the source bundle and editable master. The
live builder removes the old canine recipe and authors short faces, overlapping
mineral plates, low splayed forelimbs and blunt rakes. Each Marlback retains the
native equipment skeleton/clip names with low body bone/socket positions. The
moth has independent wing bones and three pale wing bands. Five role pixel sheets
and static sprites, plus the moth, use the same original anatomy.

Two old `ring-snapjaw*` file/model keys remain solely to preserve existing spell
animation binding paths. Their sourceBlueprint and current gameplay bindings are
MarlbackScrabbler and MarlbackBreacher; these keys cannot spawn a retired enemy.
Historical native snapshots and scene reports remain unchanged. The builder's
local `CAPTURED_BLUEPRINT_ALIASES` translates only those immutable art replays.

The completed source catalog contains 222 models and 77 blueprint bindings. A
private six-model build used the existing sculpt-normal-v2 polish pass, then
actual FBX reimport: six of six passed triangle, UV, normal, axis/bounds, rig,
clip and socket checks. Marlbacks measure 1.183 units wide and 0.796–0.811 high;
the moth spans 0.715 units. The Blender gallery and enlarged pixel contact sheet
were inspected. They show original squat bodies, layered stone backs and distinct
role details; native lighting, picking and animation remain a separate gate.

Evidence lives in `Docs/Verification/DensityCompletion/OriginalEnemies/Art`:

- `source-red.log`: initial six test methods, sixteen failing subassertions.
- `sprite-and-source-red.log`, `importer-source-red.log`: additional source/API
  RED before the corresponding generator/importer edits.
- `native-art-red.xml.gz` and `.json`: parent-run native twelve of twelve RED.
- `blender-roundtrip.json`: actual six FBX roundtrips, all green.
- `blender-polish.json`, `blender-before.png`, `blender-after.png`,
  `sprite-contact.png`: bounded pipeline and visible anatomy evidence.
- `before-model-hashes.json`: all original source/runtime FBX bytes captured.
- `master-unrelated-semantics.json`: all 216 unrelated editable model collection
  geometry, normals, UVs, weights, transforms and skeleton signatures unchanged.
- `before-unrelated-runtime-art.json`: 1,724 unrelated runtime files protected
  before native adoption; excludes the two scoped old voxel file-GUID prefixes.
- `source-before-native-adoption.log`: eight source checks, seven pass; one
  expected failure because the adopted runtime catalog had not yet been imported.

The native importer now accepts an explicit model subset, retaining every other
prefab/controller and requiring identical unselected measured metadata and
palette bytes. The voxel baker accepts explicit native FBX paths and preserves
all other catalog bindings, including toolkit and Morrowfast coarse overlays.
Old scoped mesh rows are identified by persistent source file GUID, even when
reimport has invalidated an old mesh's local ID. No obsolete assets are deleted.
Six new refusal tests were added, followed by the auxiliary-family refusal
countercheck: exact selection is from declared ring models, not the resolver's
intentional auxiliary-library fallback. Its source reproduction is preserved in
`importer-preflight-review.txt`; native execution is still pending at this point.

## In-phase review

- 🟡 Fixed: the full importer would recreate all 218 prefabs/controllers. Added
  explicit subset publication and protected unrelated bytes before invocation.
- 🟡 Fixed: a ring-only import would leave stale voxel skins. Scoped voxel rebake
  removes only the selected file GUID prefixes and rebinds current skin matrices.
- 🟡 Fixed: `FindModel` is broader than declared ring metadata. Subset preflight
  now tests exact declared IDs; an auxiliary model cannot masquerade as a selected
  ring model. The countercheck was written before the small repair; native RED
  for that individual guard was not observed and is not claimed.
- 🔵 Retained internal model keys are compatibility asset names, documented above.
- 🧪 Native all-six live views, changed-pose animation, gear placement, current
  voxel coverage and full rendering regressions remain the parent's acceptance
  gates. Source/export success alone does not close them.

## Native adoption update

The parent imported exactly six ring models into the222-model library, then baked
exactly six current skin meshes. Focused native rendering/catalog/sprite checks
passed163/163; the complete final subset-refusal fixture passed7/7, including the
auxiliary-family counter. Raw native XML/JSON and import reports are stored in
the Art receipt folder. Eight source tests now pass after runtime adoption.

`after-model-hashes.json` confirms all432 unrelated source/runtime FBXs remained
byte-identical, while exactly the four old scoped files (two per root) changed.
`after-unrelated-runtime-art.json` confirms all1,724 protected runtime files are
byte-identical, including unrelated prefabs/controllers and voxel meshes. The
216 unrelated editable master collections also retained their semantic hashes.

These checks prove native resource validation, current sprite/model binding,
voxel skin coverage, ownership/refusal behavior and bounded publication. They
do not by themselves prove all six moving creatures and their equipment look
right under actual gameplay camera/light; that visual gate remains explicit.

### Unfiltered native regression corrections

The unfiltered native suite exposed a missed authored quest-body adapter: the
Wellmeet Warren now creates `DirtGnome` (exact current blueprint), while the
renderer still recognized a Marlback carrying the old gnome disguise. The
published repair requires `DirtGnome`, its exact `g` glyph and the actual
`warren_gnomes_routed` kill fact with amount1. The existing fact-removal refusal
and restoration controls remain; no general creature alias is added.

The sprite harness also retains a58-row pin. The actual deliberate roster is62:
Marlback Wallkeeper/Breacher replace the two old leader rows, while Scrabbler,
Gleaner, Tunnelguard and Grove Lantern-Moth add four explicit original rows.
Update only the count/explanation; the actual JungleApe sprite assertion and
all-species load/identity tests remain. Native RED comes from the parent's
unfiltered suite; both repairs were published after the import freeze.

The 580-case native integration rerun is completely GREEN
(`Integration/native-refinement-and-integration-green.xml.gz`): CanonicalVillageQuestAdversarial
11/11, FoundingCampRendering29/29, EnvironmentSpriteRendererHarness39/39,
VoxelWorldDensityArt8/8, VoxelWorldObjectPaletteArt6/6, and
VoxelWorldPresentationAdversarial22/22. These latter suites reached and passed
the complete geometry, skin, bounds, material/UV and palette assertions that were
previously blocked by stale setup counts. The repaired Warren body and roster
pins passed alongside their existing refusal controls. The separate all-six
live animation/appearance gate remains explicit.
