# Starter magic in Blender

Seven editable motion studies for Caves of Ooo: the six default skill spells,
followed by the separately learnable Conjure Rain utility. These are original
design assets and real Blender renders. This directory owns art and its native
mesh handoff. The accepted Readability baseline is preserved; the current
source adds the Ember-only revision below, whose native verification is still
in progress. The original village source rig remains unchanged.

## Ember projectile revision — source accepted, native verification pending

Ember Spit now has a conspicuous molten nose and long, tightly filled conical
tail. Sixteen short overlapping ribbon/core sections and 256 fine embers flow
along the actual route, then contract into contact. A small warm puff/splash
and 16 fading embers replace its large radial fire crown. The approved Flaming
Hands animation and the other five spells are unchanged, including all their
normalized mesh/pose/material data and six FBX byte hashes.

The current [Blender source](starter_spells.blend), [native mesh/track library](runtime/starter_spell_library.json)
and [seven FBX studies](exports/) are frozen by
[Ember Art19](../../Docs/Verification/StarterSpell3D/EmberRevision/Art/19-ember-import-freeze.json):
**373 pieces, 45,384 triangles, 33 materials** across all seven spells;
Ember uses **50 pieces and 6,404 triangles**. Source SHA256:
`7e13f1dce87c67f31e2c39bcbf7758cb16ae20e182618b6fb75bf929f50e6830`;
library SHA256:
`158f2432230854309c786569666e453c0ebb6cf2e32394e0dd3748eefc3d4164`.

[Eight actual-route Blender proofs](../../Docs/Verification/StarterSpell3D/EmberRevision/Art/10-first-route-proof/)
show travel/contact/settle on long and short cardinal/diagonal shots. They
reconstruct the saved export with the real route mapping; the offline camera
is recentered at unchanged 56° and 33.38px/cell. These are not Unity pixels.
The source direction is accepted; **new native footage is pending**. The R23
videos below show the superseded large-crown Ember and must not illustrate
this new projectile revision.

The [identity/cross-section gate](../../Docs/Verification/StarterSpell3D/EmberRevision/Art/11-cross-section-controls.json)
passes 42/42, including 150 actual triangle-plane seam intersections, finite
1/2/4-cell route cap bounds, contact visibility, contraction, real mutations
and exact other-six preservation. Export102/102, pack readability86/86,
runtime60/60, saved geometry41/41, reveal9/9, actual asset mutations15/15,
seven FBX readbacks and ten contract-report controls pass. Art19 binds each
receipt. The original `.blend1` is restored to its exact pre-revision bytes.

## Readability 01 — accepted baseline, Ember design superseded

The accepted baseline implemented the five approved ImageGen directions
with larger connected shapes, bright cores, local glow and dense mesh-particle
cohorts. Flaming Hands inherits the fire family; the separately learnable
Conjure Rain retains crop-only utility. All seven remain authored and exported.
Its native-game visual acceptance and delivery media are **complete**. The
carved native forms are more graphic than the fluid ImageGen mockups; the
accepted result preserves their large, bright, densely connected spell families
at the unchanged game camera and zoom.

The previous source is recorded in
`Docs/Verification/StarterSpell3D/Readability/Art/59-continuity-import-freeze.json`:
**384 runtime pieces, 47,672 triangles, 33 static materials**. Its preserved
[Blender source](../../Docs/Verification/StarterSpell3D/EmberRevision/E00-prechange/preserved-inputs/ArtSource/StarterSpell3D/starter_spells.blend) and
[native library](../../Docs/Verification/StarterSpell3D/EmberRevision/E00-prechange/preserved-inputs/ArtSource/StarterSpell3D/runtime/starter_spell_library.json)
are the pre-Ember baseline. Its source SHA256 is
`61957bbd37cb8100b6e672c5d7d2169077bb97748c1fdd6e16d9f5d1a2216b19`;
library SHA256 is
`971a07ef686af7207ce7d1903c8446061c5ca0200d29e6ebb8731c5551a3bf95`.

Opaque carved surfaces and explicit additive vertex-alpha glow use separate
material paths. All motion is keyed mesh/rig transforms; there is no camera
shake or animated shader parameter. Fine particles are 320–384 actual islands
per fire/water/surge/frost/calm family, combined into 16-island cohorts. Counts
describe geometry, while rendered pixels establish visible density.

The final refinement closes diagonal seams with internal Jet laps and shared
pivot motion, plus four low, backward-reaching Surge stitches. Existing exact
surface cells, phase/range metadata, nine actor bones, caster actions and native
camera remain unchanged. Removed cone owners leave a visible missing wedge;
truncated Surge paths keep a clear endpoint. The source gate compares actual
saved border/cap vertices using real cardinal and diagonal owner positions.

Readability inspection images are under
`Docs/Verification/StarterSpell3D/Readability/Art/`:

- `38-fire-mantle-stills/`: Rime and Calm contact/peak proofs retain their
  current per-spell content. Receipt51 records the earlier continuity revision
  equality; the new Ember gate separately preserves all other six spells.
  This folder's Ember crown and older Jet/Surge images are historical.
- `52-native-anchor-proof/`: current Jet and Surge at two held frames in
  cardinal/diagonal layouts, plus missing-side and short-path controls. These
  reconstruct the exact exported meshes and sampled poses with actual per-owner
  native spacing and saved Blender materials. The offline camera is recentered
  for inspection; its 56° pitch and 33.38px/cell scale remain unchanged. These
  are Blender geometry proofs, **not Unity captures**.

Verified baseline actual-game footage from R23, before the Ember-only revision:

- [Playback-compatible full GameView preview](../../Docs/Verification/StarterSpell3D/Readability/Final-native-media-e29cae1c81ba4cac8fa92f275c1b719b/readability-full-gameview-native-1x-COMPATIBILITY-chroma-compressed.mp4):
  1920×1080 at recorded 1× timing; standard H.264 with lossy color/chroma conversion.
- [Lossless RGB full GameView master](../../Docs/Verification/StarterSpell3D/Readability/Final-native-media-e29cae1c81ba4cac8fa92f275c1b719b/readability-full-gameview-native-1x.mp4):
  decoded RGB frames exactly match the original capture pixels.
- [Media notes and reproduction](../../Docs/Verification/StarterSpell3D/Readability/Final-native-media-e29cae1c81ba4cac8fa92f275c1b719b/README.md) and
  [frame/timestamp/hash verification](../../Docs/Verification/StarterSpell3D/Readability/Final-native-media-e29cae1c81ba4cac8fa92f275c1b719b/media-verification.json).

The captures sample about 10 frames per second and preserve actual frame holds;
this is not a full-frame-rate recording and does not invent between-frame motion.
Any title pauses are identified separately from gameplay duration. The original
PNGs and RGB master remain authoritative for color; playback compatibility is
not guaranteed in every app. The
`renders/` videos/contact sheets below are the **historical P1 revision** and
must not be presented as current Readability footage. New offline full-motion
renders were deliberately omitted: actual native timed footage is the relevant
motion acceptance, while editable keyframes, 111 sampled poses and seven FBX
readbacks preserve Blender authorability. No stale video is relabeled.

The baseline is retained in the parent R00 snapshot. First draft/candidate1 and
candidate2 sources are preserved under `revisions/`; chronological RED/GREEN,
actual-data mutation, source-readback and fixed-scale proof receipts remain
under the Art verification directory. The source actor file is unchanged.

## Polish 01 — preserved previous revision

The second art pass adds restrained brighter cores and local accents to all
seven spells: curved ember wakes, layered palms, thicker water foam, traveling
warm ground stitches, pale ice rims, violet return arcs and soft rain beads.
There are 145 transient meshes in the editable studio scenes. All original
phase times, target shapes and the 56° camera are preserved.

The original one-to-three-frame scale pops were replaced with bounded eased
reveals and longer follow-through. Caster hands settle gradually to neutral;
rain strokes fall once without a visible one-frame upward reset. Colors remain
opaque and static, and no camera shake or shader animation is required.

The durable baseline is in `revisions/v1/`. Its `.blend`, manifest, source
builder, contact sheet and representative animation previews are preserved.
Historical P1 receipts are in `Docs/Verification/StarterSpell3D/Polish01/author/`;
all original v1 receipts remain unchanged.

## Open and review

- `starter_spells.blend`: choose one of the seven numbered scenes. Each has
  its caster, target or crops, transient effect hierarchy, semantic timeline
  markers, camera, and studio lighting.
- `renders/starter_magic_contact_sheet.png`: six default spells. Ember Spit
  and Calm deliberately show **travel**, the other four show **contact**.
- `renders/starter_magic_loop.gif` / `.mp4`: all six move together, with the
  current phase labeled independently per spell.
- `renders/conjure_rain_design.png` and `conjure_rain_loop.gif` / `.mp4`:
  empty-handed crop watering. Learning from the carried Watering Grimoire or
  buying with skill points does not imply a held book requirement.
- Per-spell `*_motion.mp4` clips are slow review; `*_native_timing.mp4` clips
  play the sampled native clock. Preview videos are a design review, not a
  recording of the game running.

The scene uses **100 fps** to preserve the existing presentation timings:
0.12 s cast, 0.10 s charge, 0.025 s per recorded travel cell, 0.20 s impact,
0.18 s transient aftermath. Frame 110 ends the demonstration reading pause.
Semantic fractional contact frames are retained exactly in `manifest.json`
and the scene's `phaseFrameExactJson`; Blender timeline markers round to the
nearest integer. Native-timing videos sample every three native frames and
play at 100/3 fps. Review loops use those same frames at 8 fps, **4.17× slower**.
The pause/reset is neither a game turn nor a cooldown.

## Geometry and presentation contracts

- One metre per native cell; the caster root is fixed. The existing teal
  character mesh and nine-bone skeleton are copied from the unchanged village
  source. Calm's recipient is the existing olive character, not brainless
  scenery. Other targets are explicitly neutral studio practice stand-ins.
- Seven distinct gestures use the current arm, hand, head and spine bones. The
  existing mitten-like hands have no articulated finger skeleton; wrist and
  arm motion carry the open/close idea in these studies.
- The 56° downward orthographic camera includes the native ground projection
  correction: horizontal sensor fit and pixel aspect X = 1/sin(56°), Y = 1.
  **Display the resulting PNG and video at square pixels.** The correction is
  already part of the rendered image. Equal ground metres remain equal pixel
  spans; height projects northward by cot(56°).
- Solid effect materials are opaque; designated glow meshes use static vertex
  alpha gradients. Both have static colors/emission. Mesh transforms and
  the character skeleton carry animation; there is no animated shader,
  particle simulation, Geometry Nodes dependency, or gameplay collider.
- Each effect piece has an explicit keyed transform and semantic pivot. Each spell has a
  distinct `ActorRoot`, `EffectRoot`, and `AftermathRoot`. Transient geometry
  clears, including in the FBX readback. Nothing becomes loot or a hazard.
- Flaming Hands has one selected adjacent ground cell. Upper decorative
  geometry may overhang its owner without changing spell reach. Jet Blast exposes
  the four-cell stencil (1,0), (2,-1), (2,0), (2,1) with separate side folds,
  not a blanket floor puddle. Ground Surge occupies four actual adjacent
  cell centers. Rime's three clamps keep the recipient's head and shoulders
  clear. Rain's three example crops occupy exact offsets (1,-1), (2,1),
  (3,0), all within its radius-three square query.

## Export readiness and limits

`exports/*.fbx` are **Blender-roundtrip design assemblies**, with embedded
player texture, animated caster skeleton, and animated mesh hierarchies.
The exported study preserves its stage-relative actor offset. It is **not**
an accepted drop-in Unity prefab, production Animator controller, or runtime
VFX adapter. The package currently uses FBX -Z forward / Y up; the project's
production art pipeline uses a separately verified Z-forward/export rotation
convention. The companion Unity asset builder performs validated local-origin/axis and
clip extraction; those native checks remain separate from the Blender
roundtrip and raw FBX design assembly.

The native adapter additionally consumes `runtime/starter_spell_library.json`,
exported from the current saved scene by `export_runtime.py`. It contains
373 separate mesh pieces, 45,384 triangles, explicit static sRGB colors,
material emission/glow, optional vertex-alpha gradients and 111 sampled
transform poses per piece at 100 fps. See `runtime/README.md` for semantic
roles, pivots, conditions, coordinate conversion and explicit cell clipping.
It contains no actor, studio, target, planter or crop-leaf meshes. Caster animation
continues through the separate FBX handoff.

The current source has measured transform fidelity, geometry bounds and
mutation controls. Those source checks alone do not establish Unity front
faces, bind pose, visibility handling, outcome routing, performance or feel.
The historical native acceptance below covers the previous pack; the new
Ember still requires its own native acceptance. A successful Blender export
never substitutes for that evidence.
Never infer damage, status, targeting, RNG, displacement or occupancy from
these meshes. Any persistent Burning, ice, charge, ground water or Pacified
view must be state-driven separately. A transient clamp or loop is the
successful contact gesture, not the lifetime of a status.

Only the main success shape/motion is animated here. Miss, immunity, denied
status, lethal break-away, blocked/successful push, wet-fire suppression,
Rime's water-only fallback/dry refusal, Calm's peaceful/failure branch and
Rain's no-valid-crop branch are specified in the parent design and manifest
but not rendered as separate animation variants. Those are covered by separate native outcome/integration tests, not by these
success-only studio renders. There is no studio claim of full gameplay branch
coverage. The
native handoff now includes three low transient freeze-water plates derived
from the actual Rime chip mesh; they are a separate conditional reaction
template, not a separately rendered studio branch or persistent water state.

## Reproduce

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 3 --python ArtSource/StarterSpell3D/build_studies.py -- --export-only ember_spit
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 3 --python ArtSource/StarterSpell3D/export_runtime.py
python3 ArtSource/StarterSpell3D/validate_contract.py --adversarial
python3 ArtSource/StarterSpell3D/audit_readability.py --controls --output /tmp/starter-readability.json
python3 ArtSource/StarterSpell3D/audit_runtime_library.py /tmp/starter-runtime.json
python3 ArtSource/StarterSpell3D/audit_ember_identity.py --controls --output /tmp/starter-ember-identity.json
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 3 --python ArtSource/StarterSpell3D/render_ember_identity.py -- --output /tmp/starter-ember-proof
```

The selective builder regenerates the owned source and Ember FBX, preserving
the other six FBX files; all seven still receive actual readback checks. It does
not edit Assets or the original village actor. `ember_projectile_art.py` owns
the new isolated Ember geometry and sampled transform authoring. Rendering/proof scripts read the saved source without
saving it, and record source SHA-256. Use fresh output paths to preserve earlier
receipts and renders. Native acceptance runs and final media are coordinated
separately; the historical video compositor is not part of this handoff rebuild.
`manifest.json` contains per-spell pivots, names, timing, shown native offsets,
materials, shape pieces, selected still frames, and explicit readiness bounds.

## Verification and review

The historical Art59 freeze links export102/102, saved continuity/cap/owner170/170,
readability/actual controls76/76, runtime60/60, saved geometry41/41, reveal9/9,
actual Blender mutations15/15, all seven FBX readbacks and ten contract-report
controls. Native acceptance is a separate gate; current and historical receipts
are never substituted for one another.

The [R23 native report](../../Docs/Verification/StarterSpell3D/Integration/NativeAudit/SSN-e29cae1c81ba4cac8fa92f275c1b719b-native.json) passes all 94 native checks,
with 968 independent checks, 286 captured PNGs and 81.6928 seconds of measured
Full/Reduced profiling. [Final visual acceptance](../../Docs/Verification/StarterSpell3D/Readability/R23-final-visual-acceptance.json)
records the inspected full-view and directional frames. Jet and Surge remain
connected on diagonal casts; brief opaque wave/fire overlap can mask parts of
a recipient's body, and nearby geometry can occlude decoration. The capture
sampling and finite profile do not establish uninterrupted frame pacing or
long-session performance.

[R24 full regression comparison](../../Docs/Verification/StarterSpell3D/Readability/R24-final-full/baseline-comparison.json) records
11,307/11,338 passing: the same 31 baseline failure names/messages, no new
failures, no compiler errors or skips, and all 130 added tests passing.


Historical P1 receipts under `Docs/Verification/StarterSpell3D/Polish01/author/`
record actual sampled reveal **1/8 RED → 9/9 GREEN**, saved geometry **41/41**,
actual Blender asset mutations **15/15**, native export **83/83** and exported
mesh-data checks plus real geometry/pose/outcome mutations **57/57**. All seven
37-frame sequences and hero stills were rendered fresh from the same frozen P1
source. Encoded media checks pass **51/51**; all 342 raw and composed PNGs
have square-display metadata. `16-media-provenance.json` and
`17-delivery-inventory.json` bind the editable source, previews, FBX files and
native mesh library by SHA-256. A first
P1 build briefly hid Rain at its exact contact boundary; the source/export
gate caught it, and a one-frame eased lead-in restored contact visibility.
A later validator assumption incorrectly demanded projectile progress end
exactly at 1; source inspection confirmed intentional 0.08 m follow-through,
and the validator now checks that bounded original movement explicitly.

The following preserved receipts describe the original v1 pass, under
`Docs/Verification/StarterSpell3D/`:

- `01-asset-contract-red.json`: actual absence before authoring.
- `03-first-build-cleanup-red.json`: caught a draft Rain book prop clearing
  too late. The prop was removed because casting is empty-handed.
- `12-native-stencil-red.json`: 20/29 saved-file geometry assertions passed;
  eight half-cell Ground Surge offsets and the missing Jet side lips failed.
- `14-native-stencil-green.json`: initial corrected stencil geometry,
  **31/31** passed. Later contact-frame visual inspection found the separate
  Calm crossings and Ground Surge's secondary stone tongues still using a
  stale Blender transform during pivot recentering.
- `21-fragment-pivot-red.json` → `25-fragment-pivot-green.json`: actual saved
  geometry **31/37 RED → 37/37 GREEN**. Pivot placement now composes current
  transform properties directly. The four changed spells were re-rendered;
  all 37 evaluated preview samples in the other three spells are identical.
- `27-final-asset-contract-green.json`: all seven source/export contracts
  pass, including effect-specific imported motion, cleanup and fixed roots;
  ten deliberately damaged reports are rejected by the validator.
- `28-final-real-asset-adversarial.json`: **15/15** assertions on actual in-memory
  Blender asset mutations: removed caster/mesh actions, a lingering effect,
  injected root motion, renamed required hierarchy, deleted effect geometry,
  and transparent material. Paired unchanged controls pass. The damaged
  in-memory data is never saved; delivered `.blend` SHA stays identical.
- `23-png-metadata-red.json` → `29-png-metadata-normalized.json`: actual
  Cycles PNGs carried non-square physical-density metadata. The exporter now
  removes only each PNG's `pHYs` chunk, preserving every compressed `IDAT`
  pixel byte. Raw frames and hero images therefore display at square pixels.
- `36-final-media-verification.json`: **51/51** media checks inspect the
  encoded videos and GIFs, including real frame counts, duration, sample
  aspect ratio and actual per-spell frame variation.
  They do not substitute for native Unity acceptance.
- `34-final-media-provenance.json` and `35-final-delivery-inventory.json`
  bind the final previews, seven FBX assemblies and editable source by SHA-256.
  Four corrected frame sets were rendered from the final source; three
  retained sets have identical visible evaluated geometry at all 37 samples.
  This distinction is explicit in provenance rather than hidden behind the
  final `.blend` hash.
- `camera-calibration/`: independently supplied native-projection camera
  verification. This directory is owned by the parent review.

The root review additionally checks evaluated triangles against actual cell
stencils: **15/15** independent geometry assertions pass after the four
Ground Surge group offsets and two Jet side-cell coverage failures were
corrected. Independent review of all seven saved scene cameras passes
**35/35** assertions against the calibrated native projection.

Blender visual inspection found and corrected pale linear-color
swatches, an edge-on Calm loop, off-center fragment scaling, and cropped
plinth framing. Asset scripts can establish keys, transforms, geometry,
cleanup, source preservation and readback. They cannot establish native Unity
rendering, performance, controller compatibility, or combat feel.

The Reduced handoff retains `Rime__drifting_ice_chip_0` as essential cold-contact
feedback when Frozen is denied. The canonical data gate recorded this gap RED
before the one-flag fix and rejects removal of the remaining neutral chip.

The native GPU review then caught a separate Rain contact-readability issue:
its first active stroke at exact frame 22 occupied less than a pixel, despite
having a nonzero scale. Each crop cluster now reveals its first defining stroke
smoothly during charge, with the fall still beginning at frame 22. The unchanged
source-space contact gate moved **1/4 RED → 4/4 GREEN**; the actual native GPU
gate is owned by the integration review. Six other complete scene sequences are
identical at all 37 samples, including actor meshes, materials, cameras and lights.
Only Rain's motion frames needed rendering again; all hero stills were refreshed.
Receipts `31-rain-scene-comparison.json`, `38-rain-media-provenance.json` and
`39-rain-delivery-inventory.json` describe the historical P1 Rain freeze. Earlier
P1 receipts remain immutable, and `revisions/p1_before_rain_contact/` preserves
that source. `finalize_rain_contact.py`, `finalize_polish.py` and
`finalize_delivery.py` are historical provenance tools and are not part of the
current Readability rebuild. Use the Reproduce section and Art59 freeze above
for the current source and handoff.
