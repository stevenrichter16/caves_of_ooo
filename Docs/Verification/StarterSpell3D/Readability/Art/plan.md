# Readability art — executable authoring plan

Status: complete — Art59 source frozen, source gates GREEN, R23 native visuals accepted, final media verified and R24 regression matches the baseline. 2026-09-11. CoO-original art, no Qud parity claim. Parent scope is `Docs/SPELL-3D-READABILITY.md`; no game/camera/Assets changes are owned by this art package. The milestone notes below retain their historical findings and pending states; the final handoff section closes them.

## Reference and shape decisions

The five approved `ArtSource/StarterSpell3D/Concepts/Readability01/0*.png` images are the color/density/silhouette references. They are approximate image edits; their shifted recipients and HUD text are not placement specifications. Six starters remain; Flaming Hands inherits the Ember fire language and Conjure Rain retains crop-only utility coverage.

| Family | Concrete authored result |
| --- | --- |
| Ember | Charcoal seed with connected amber/coral molten wakes; broad upward ivory-gold flame crown and 352 clustered carved embers. Open central face window. |
| Hands | Same gold/coral folded flame language around the adjacent cell, with broader upper flame fingers and clustered sparks; original one-cell lower contact retained. |
| Jet | Four real cone-cell anchors; thick turquoise folded lips, milky ridges and densely layered bead clusters. Neighboring upper curls visually connect, lower contacts stay in their cells. |
| Surge | Connected low ivory/ochre angular roots on the actual path; elevated small forks and tightly packed mineral sparks. No sky bolt. |
| Rime | Existing blunt lower clamps retained; broad chalk/blue outer ice crown with indigo facets and dense compact shards. Clamps/crown require Frozen; a neutral cold-hit cluster remains on denied status. |
| Calm | Large open ivory/lilac carved arcs and overlapping leaf motes, with three deliberate gaps. Traveling form remains unconditional; relaxing recipient arcs require Pacified. |
| Rain | Existing empty-handed crop-only strokes remain; modest self-lit swatches follow the shared material handoff. No new affected cells or growth/Wet claim. |

Main primary crowns span roughly 3.5–4.5 cells, with dense secondary clusters extending the complete envelope toward 4–5.5 cells. Their centre stays open around the recipient. Each cluster contains 8–16 separate actual particles in a single animated mesh; connected ribbons carry the broad silhouette. Full/reduced runtime budgets remain bounded by the parent integration gates.

## Verified contract and corrections before authoring

| Read source / prior premise | Correction adopted |
| --- | --- |
| `build_studies.py`, `render_loops.py`, native GameView reference | Existing 7.4-cell detail camera is not gameplay-scale evidence. Add a 33.38-pixel-per-cell proof view and final actual-native full-frame review. Keep 56-degree projection and existing native camera untouched. |
| `export_runtime.py`, `audit_geometry.py`, `audit_runtime_library.py` | Previously every cone mesh was clipped and every per-cell vertex constrained to ±0.5. Keep that for `CellSurface`; explicitly separate upper `AirborneDecoration`, at most 2.75 cells radially from its real owner. No widened gameplay stencil. |
| `NativeSpellFxLibrary.cs`, native renderer/importer | Static color alone cannot make luminous cores or soft local glow. Agreed additive data: material `emission` float 0..1, `glow` bool; optional mesh `vertexColors` flat RGBA (white RGB with alpha falloff); piece `visualBounds` enum string. Old/default entries remain valid. |
| Current renderer TargetImpact timeline | Parent adds a gate at copied actual contact. Source study has a smooth private lead-in, contact is already readable, then peaks over 6–8 frames and holds about 15 frames. Target visuals never anticipate the authoritative contact in-game. |
| Existing material/readback and mutation gates | Preserve opaque Principled alpha for solid surfaces, no animated shader parameters. Glow is an explicit static vertex-alpha material path; do not retain an inaccurate whole-package `opaqueOnly` claim. Solid-alpha mutation still fails. |
| Existing 129 imported pieces and native limits | 320–384 particles do not imply 320–384 GameObjects: combine independent islands into cohorts and retain actual transform samples. Root verifies scheduling priority under the 384 Full / 96 Reduced view caps. |
| Previous Rain GPU failure and color regression | Nonzero scale is not proof of visible pixels; export checks include actual geometry at exact contact and fixed-scale raster proof. sRGB→linear happens once; native GPU color/alpha/fog remain independent gates. |

## Additive data and timing

`SpellGlow` is a Blender POINT color attribute. Export copies/interpolates alpha for actual triangle vertices; static white RGB avoids accidental tint multiplication. Materials expose `emission` and `glow` custom properties. `AirborneDecoration` permits visual overhang and skips diagonal fit only in the native adapter. Native roles/anchors/conditions, 111 frames at 100 fps, release frame 22, contact/clear frames, source mapping X→Unity Z/Y→Unity X/Z→Unity Y, nine actor bones and seven action names stay unchanged.

The strongest new surfaces hold roughly contact+6 to contact+21 and settle by the existing clear frame. Alpha does not animate; motion/reveal use real keyed transforms. State-driven lingering effects remain outside transient clips. Native TargetImpact gating at contact intentionally omits the studio's pre-contact easing samples.

## Implementation and gates

1. Run new actual exported-geometry RED: connected primary extent at gameplay pixel scale, actual particle island count/density, luminous materials and gradient halos, early contact and held peak, finite nonnegative motion, source palette fields, complete clear and bounded per-cell overhang. Capture old-data failures before production edits.
2. Add side-effect-free procedural art helpers; integrate into the seven-scene builder and additive exporter. Preserve original source actor SHA and stable rig/action names. Keep baseline source/receipts immutable in R00.
3. Run saved-source geometry/reveal, FBX roundtrip, exported TRS/normal/bounds/material and actual-data adversarial controls. Pair new positives with particle deletion, flattened alpha, removed emission, cell misclassification/spill, excessive radius and abrupt/lingering-motion mutations.
4. Render Ember/Jet/Rime stills plus fixed-scale proof for parent review before all motion frames. Iterate shapes and density from actual pixels. Native renderer/importer tests and real GameView review are separate parent-owned gates.
5. Freeze art hash, render complete seven-scene motion, normalize PNG density without resampling, produce new provenance and update source README. Cold-eye after GREEN: compare all families, conditional variants, docs, export and source symmetry.

## Honesty bounds / in-phase review

Python/Blender checks establish actual keyed source, geometry, colors, gradients, bounds, fixed root, readback and source preservation. They cannot establish native shader appearance, fog occlusion, actual frame cost or subjective comfort. Those require the parent's native captures. Initial review: 🟡 old blanket cell clipping conflicts with approved upper overhang (fixed by explicit classification); 🟡 old detail-only views overstate native readability (fixed by native-scale proof). Current source/export implementation is present; final visual acceptance remains independent.

## Implementation log / owned files

- Read CLAUDE.md and parent plan; checked all five approved images, builder/exporter, saved geometry/reveal/runtime/adversarial audits and native library/renderer. R01b baseline returned 11,177/11,208 with the same 31 known failures, no CS/skips. Parent released ArtSource freeze.
- NEW this plan. Planned NEW `audit_readability.py`, `readability_art.py`, proof renderer; MOD builder/exporter/audits/source manifest/README and regenerated owned art. Every new receipt goes under this Art directory; old verification receipts remain immutable.

### Import candidate implementation — 2026-09-11

- Actual baseline geometry/material gate recorded **14/47** (`01-readability-red.json`). The first new draft recorded **46/47**: eight widened ground-root end caps reached 0.5274 cells. `revisions/readability-draft01/` retains that source/library/helper. Terminal caps were narrowed without widening the owning cells.
- Added `readability_art.py`: real tapered carved ribbons, conditional frost/calm crowns, multi-island particle cohorts, shared-flow Jet patches and static five-band glow sleeves. Material/vertex-alpha/visual-bound metadata is exported from the saved source. FBX actor names, nine bones and phase timings remain stable.
- First pixel review (`11-draft-stills/`) exposed three defects despite positive geometry counts: thick cores hid many motes, studio tone mapping produced pastel fire, and repeated Jet arches read as separate barriers. Motes now sit on tight outer shells; carving is thinner; Blender uses a static light-floor approximation under Standard color management; Jet is one low forward fan with a curled far lip. Detail-only studio framing widened to 9 metres; game framing and the 33.38px/cell proof did not change.
- Root's direction review also identified planar crowns. Fire curls now cross different azimuth planes, frost rays radiate in XY, and Calm's three open sections form a warped ellipsoid. The first current source gate was **62/62**, saved geometry **41/41**, sampled reveal plus actual abrupt-curve control **9/9**, real Blender mutations **15/15**, actual exported geometry/pose/outcome controls **60/60**, and all seven FBX readbacks passed.
- The current import candidate is frozen by `23-import-candidate-freeze.json`: source `c3290625…6025c0af`, library `c00ef612…523ec4db`, 285 pieces / 24,628 triangles / 34 materials. Native import succeeded separately; current source footage has not been represented as a game recording. Existing P1 videos are explicitly labeled historical pending final renders.
- A later strengthened minor-axis direction gate rejects the old planar draft (`24-flat-draft-direction-countercase.json`), including Calm's 9.09px side view. Current Fire/Hands/Jet/Rime/Calm pass. Ground Surge's narrow path measures 12.46px in two directions against an 18px art criterion (`25-readability-eight-directions.json`, **58/59**). This is a retained open visual finding for actual native review, not silently called GREEN or suppressed. A possible bounded remedy is a small lateral gold branch per actual cell. The imported candidate remains unchanged while root reviews it.

### Current self-review / bounds

- 🟡 Fixed: source geometry cap spill; hidden inside-core particle placement; pastel proof colors; cropped close-up; repeated Jet arches; planar fire/frost/calm envelopes.
- 🟡 Open: whether Ground Surge's north/south path should gain lateral branching to meet the strengthened minor-axis target. Await native pixels before changing the frozen import candidate.
- 🔵 Data terminology corrected: `posedMotes` counts active mesh islands; only pixel review can establish their visibility. Tests never equate an island count with rendered density.
- 🧪 Native color, fog, depth, direction, frame cost and feel remain independent parent gates. Full seven-scene final motion renders are deliberately deferred until native art review stabilizes the source.


### Second native candidate — actual pixel feedback and further RED/GREEN

- R09 native visual feedback rejected the first imported art despite correct contracts: rigid fire prongs, a straight wave edge, tiny ground roots, spiky frost, faint glow and sparse visible motes. Candidate1 source, helper, JSON and seven FBX exports remain in `revisions/readability-candidate1/`.
- `26-native-feedback-red.json` captured the increased actual-data extent/glow requirements before candidate2 implementation. The new native bounds are an art-only radius of 2.75 cells for upper decoration; ground surfaces remain ±0.5. This explicitly replaces the first candidate's insufficient 2-cell decoration budget, without changing any native hit cell or occupancy.
- Candidate2 reshaped Fire into tapered C/S curls through several azimuths, Jet into a continuous curved rolled lip, Surge into connected gold roots with short three-dimensional forks, Rime into broad blunt petals and Calm into large open folded arcs. Static radial/crossed additive beds have transparent edges and centres. The Blender proof uses transparent-plus-emission addition with camera-ray-only energy; it does not light the studio world or darken the background.
- The actual source with primary follow-through deliberately disabled was built/exported and preserved in `revisions/readability-candidate2-no-followthrough/`. `31-main-hold-motion-red.json` is 60/65: precisely the five intended primary-motion failures. Restoring a small eased rise/roll/settle during the strong hold passes; ground contacts do not drift. The final actual exported-array countercase freezes those poses and is rejected.
- First candidate2 pixels (`34-candidate2-stills/`) improved scale/glow and the water/frost/calm forms, but Fire still looked like long needles and the fine shell remained too sparse. `35-visible-mantle-feedback-red.json` recorded 59/65, precisely six actual island-density failures, before the final mantle revision. Curled tapered fire replaces those needles; 320–384 particles per family are now grouped into 16-island cohorts on external shells. Counts remain an art-data fact, not an occlusion claim.
- The fresh five contact/peak proof sets are `38-fire-mantle-stills/`, rendered from source `2e8dda06…1254e4d` at the retained 33.38px/cell proof and a separate detail studio camera. Root accepted these for the next actual-native comparison, with recipient face visibility explicitly still to review in-game. Detail crops may trim broad decoration; the fixed-scale proof fits the envelope and remains the comparison view.
- Current gates: export 102/102 with 2.3842e-7 maximum TRS error; actual readability + 11 real-data controls 76/76; saved geometry 41/41; reveal and abrupt-motion mutation 9/9; asset mutations 15/15; runtime geometry/outcome controls 60/60; seven FBX readbacks and ten contract-report controls GREEN. Receipts 37–45 bind this source/library. The frozen handoff contains 380 meshes/pieces, 47,352 triangles, 33 materials; the original village actor SHA remains unchanged.

### Candidate2 cold-eye / open boundaries

- 🟡 Fixed from real pixels: long straight Fire needles, flat Jet far edge, narrow Surge silhouette, antler-like frost, weak local glow, underpopulated external mantle and stationary main masses during the hold.
- 🔵 Preserved: original 111 samples/100 fps, contact and clear frames, seven identities, nine actor bones, fixed roots, copied cell stencils, separate reaction/conditional success forms, no colliders, explicit white-RGB glow alpha and one source color conversion.
- 🧪 Next gate: actual native full-view and cardinal/diagonal casts, including nearby head visibility, physical-cell fog/geometry occlusion and Full/Reduced profile. No claim that studio proof proves native final quality.
- ⚪ Current source is frozen for native import after the five-still review. Full seven-scene final Blender loops and delivery provenance are deferred until source stabilization; historical P1 footage is not relabeled as new art.


### Diagonal continuity refinement — plan before implementation

R16 actual native screenshots exposed separate outlined Jet panels and four isolated Surge tufts on northeast casts, despite strong cardinal silhouettes. Runtime `PoseInWorld` correctly rotates airborne pieces at authored size around real cell centres: diagonal owners are sqrt(2) metres apart. The source's cardinal Jet edge laps and half-cell roots therefore do not bridge the extra .4142 metres. This is an art continuity defect, not a reason to change targeting or camera transforms.

Preserve candidate2 in `revisions/readability-candidate2/`. First sample actual saved-mesh seam vertices and held transforms for all eight real anchor directions, plus unchanged ground data/missing-owner controls. Then widen only internal Jet laps, taper their internal foam ends and give neighbouring pieces common follow-through about the same studio source pivot. Add one thin elevated backward-reaching Surge stitch per true owner, forward extent−1.12 to+.38m; existing exact CellSurface roots remain unchanged, and the last owner adds no forward range. Redistribute existing low particle cohorts rather than increasing density/count. Compare normal, truncated and missing-owner layouts: removal must leave a visible gap, though intentionally permitted thin airborne overhang is not zero. No other spell source content, materials, runtime schema, camera or budgets change. Fresh source/GREEN/adversarial gates and true per-owner cardinal/diagonal proof renders precede the next freeze.


### Final continuity implementation and cold-eye — 2026-09-11

- Actual candidate2 saved-source continuity RED was **71/127** (`47-native-anchor-continuity-red.json`), before modifying geometry. At peak, real diagonal Jet seam gaps were .306–.350m laterally and .087–.102m longitudinally; independent object rotation widened a held seam to .439m. Surge's retained diagonal roots left .735m between endpoints. The existing source bounds/overall width gates did not prove adjoining-cell continuity.
- Jet now broadens internal laps only, tapers internal milky ends and rolls adjacent defining pieces about one common source pivot. The four existing ConeCell owners and body mesh count remain unchanged. Omitted side owners still remove their authored pieces and leave a visible open wedge.
- Root's endpoint review corrected the proposed Surge range before implementation: final stitches run **backward−1.24 to forward+.26m**, not the earlier draft−1.12→+.38. Complete cap vertices lead by at most .263903m, behind the retained full diagonal root's approximately .35m forward extent. Four thin essential meshes add 320 triangles. Two existing low mote cohorts per owner follow the new connective path; no particles or material types are added. The backward stitch smoothly reveals early enough to join at contact, preserves the original clear frame and does not shift the phase/range/carrier metadata.
- Actual saved-source seam/cap/owner checks pass **170/170** (`51`): Jet's narrowest sampled diagonal lap is .158138m; adjacent Surge bridges overlap .085786m. A removed middle owner leaves 1.328427m clear diagonally. Removed Jet side-owner projected area loss is at least29.5657% cardinal and about67% diagonal; the reinserted-owner control eliminates that hole and is rejected. Actual separated-border and extended-forward-cap mutations are rejected. These are sampled source geometry facts, not a rendered-pixel guarantee.
- `52-native-anchor-proof/` renders current immutable exported geometry/poses with actual cell-centre spacing and diagonal Surface fit, using saved Blender materials. Root and independent audit viewed cardinal/diagonal held frames, missing-side Jet and one/two-cell Surge, and accepted them for import. Offline framing is centered for inspection at unchanged56°/33.38px per cell; these are not Unity GameViews. Export per-spell content for Ember, Hands, Rime, Calm and Rain, and every Jet/Surge CellSurface mesh/pose, compares exactly after replacing global material ordinals with the full material identity/data.
- Final canonical gates: export102/102 (max TRS error2.3842e-7), continuity170/170, readability/actual controls76/76, runtime geometry/outcome controls60/60, saved geometry41/41, reveal9/9, real asset adversarial15/15, seven FBX readbacks and ten contract-report controls GREEN. Freeze59 binds **384 meshes/pieces,47,672 triangles,33 materials**; source `61957bbd…2216b19`, library `971a07ef…a3bf95`. The original village source SHA remains `3a9e4139…397763`.
- 🟡 Fixed: the actual diagonal discontinuity and its held-rotation regression. No runtime projection, camera, hit cell, status, save, shader or view-budget change was needed for this refinement. Independent audit accepted source ownership/cap/budget boundaries; actual native import/GPU/gameplay remain parent-owned final gates.
- ⚪ Deliberate media-scope revision: do not render a redundant full seven-spell offline video set. The verified native timed preview is the decisive motion presentation; real Blender keyframes/111 exported poses/FBX readbacks establish authorability. Existing P1 videos stay explicitly historical. Current unchanged-family stills remain valid through normalized equality; revised Jet/Surge proof52 supplies their new images. Final native media links remain pending until root supplies verified output.
- 🔵 Canonical `.blend`, JSON, FBX and generator/export inputs are frozen. Remaining art documentation edits are independent of import inputs. Root will restore the pre-existing `.blend1` backup bytes from the preserved baseline after the last source build.


### Accepted native handoff — 2026-09-11

- Art59's 13 source/import inputs remain unchanged: 384 pieces, 47,672 triangles,
  33 materials and all seven spells. No final documentation or media work saves
  the Blender source or changes FBX/JSON/generator inputs. The parent restored
  the original `.blend1` bytes separately; the canonical source hash is unchanged.
- The [R23 native report](../../Integration/NativeAudit/SSN-e29cae1c81ba4cac8fa92f275c1b719b-native.json)
  passes 94 native checks, with 968 independent checks, 286 actual PNGs and
  81.6928 seconds of Full/Reduced profile. Root and independent art review
  accepted the five main concepts and north/south/northeast samples; the
  [final visual pin](../R23-final-visual-acceptance.json) identifies exact frames.
  R19/R20 capture skips and the subsequent separately tested native contact
  recovery remain recorded in the parent log; no art change was needed.
- [R24 full regression](../R24-final-full/baseline-comparison.json):
  11,307/11,338 pass, exactly the same 31 baseline failure names/messages,
  no new failures, no compiler errors/skips, and all 130 added tests pass.
- Final [playback-compatible full GameView preview](../Final-native-media-e29cae1c81ba4cac8fa92f275c1b719b/readability-full-gameview-native-1x-COMPATIBILITY-chroma-compressed.mp4)
  and [lossless RGB master](../Final-native-media-e29cae1c81ba4cac8fa92f275c1b719b/readability-full-gameview-native-1x.mp4)
  show the five concepts followed by Hands/Rain. The [media notes](../Final-native-media-e29cae1c81ba4cac8fa92f275c1b719b/README.md)
  and [verification](../Final-native-media-e29cae1c81ba4cac8fa92f275c1b719b/media-verification.json)
  bind actual pixels, recorded timestamps and outputs. Captures sample about
  10 Hz, preserving frame holds without interpolation; they do not show every
  gameplay frame. The compatible copy uses lossy color/chroma conversion;
  original PNGs and the RGB master remain the color authority.
- 🔵 Final visual limits: the native meshes deliberately read as carved, more
  graphic forms than the fluid ImageGen images. The fixed camera/zoom remains.
  Jet/fire briefly mask parts of target bodies; heads remain identifiable in
  the inspected peaks, while walls can naturally hide decoration. The short
  sampled capture/profile cannot establish long-session performance or every
  fog/terrain arrangement. Source keyframes and exact readbacks establish
  authorability; historical P1 videos remain labeled historical.
