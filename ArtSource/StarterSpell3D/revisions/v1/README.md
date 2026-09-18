# Starter magic in Blender

Seven editable motion studies for Caves of Ooo: the six default skill spells,
followed by the separately learnable Conjure Rain utility. These are original
design assets and real Blender renders. No Unity runtime, gameplay, source rig,
save, or project scene is changed by this package.

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
- Six distinct gestures use the current arm, hand, head and spine bones. The
  existing mitten-like hands have no articulated finger skeleton; wrist and
  arm motion carry the open/close idea in these studies.
- The 56° downward orthographic camera includes the native ground projection
  correction: horizontal sensor fit and pixel aspect X = 1/sin(56°), Y = 1.
  **Display the resulting PNG and video at square pixels.** The correction is
  already part of the rendered image. Equal ground metres remain equal pixel
  spans; height projects northward by cot(56°).
- All effect materials are opaque and use static colors. Mesh transforms and
  the character skeleton carry animation; there is no animated shader,
  particle simulation, Geometry Nodes dependency, or gameplay collider.
- Each effect fragment scales about its own carved form. Each spell has a
  distinct `ActorRoot`, `EffectRoot`, and `AftermathRoot`. Transient geometry
  clears, including in the FBX readback. Nothing becomes loot or a hazard.
- Flaming Hands stays within one selected adjacent cell. Jet Blast exposes
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
convention. Actual Unity axis, bind-pose, socket, local-origin extraction and
clip import acceptance remain required before integration.

The handoff should separate cast pose clips from event-bound effects; convert
study coordinates to actor and resolved contact pivots; partition traveling
fragments by visible cells; and feed one existing WorldFxCoordinator queue.
Never infer damage, status, targeting, RNG, displacement or occupancy from
these meshes. Any persistent Burning, ice, charge, ground water or Pacified
view must be state-driven separately. A transient clamp or loop is the
successful contact gesture, not the lifetime of a status.

Only the main success shape/motion is animated here. Miss, immunity, denied
status, lethal break-away, blocked/successful push, wet-fire suppression,
Rime's water-only fallback/dry refusal, Calm's peaceful/failure branch and
Rain's no-valid-crop branch are specified in the parent design and manifest
but not rendered as separate animation variants. Those remain integration
and acceptance work; there is no claim of full gameplay branch coverage.

## Reproduce

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 4 --python ArtSource/StarterSpell3D/build_studies.py -- --render-stills
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 4 --python ArtSource/StarterSpell3D/render_loops.py
python3 ArtSource/StarterSpell3D/compose_previews.py --loops
python3 ArtSource/StarterSpell3D/verify_previews.py
python3 ArtSource/StarterSpell3D/normalize_png.py --check
python3 ArtSource/StarterSpell3D/validate_contract.py --adversarial
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 2 --python ArtSource/StarterSpell3D/audit_geometry.py
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 2 --python ArtSource/StarterSpell3D/audit_adversarial.py
```

The builder only writes this directory. The review compositor uses Pillow
and FFmpeg. Source assets remain unchanged and their SHA-256 is recorded.
`manifest.json` contains per-spell pivots, names, timing, shown native offsets,
materials, shape pieces, selected still frames, and explicit readiness bounds.

## Verification and review

Receipts are under `Docs/Verification/StarterSpell3D/`:

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
