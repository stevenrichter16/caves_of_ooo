# Starter magic — Blender animation design

Status: Blender design and Polish 01 complete, 2026-09-10. The subsequent
[game integration](SPELL-3D-INTEGRATION.md) passes final native rendering and
performance acceptance on 2026-09-11; whole-suite close-out is recorded there. CoO-original art; no Qud mechanical or
visual parity claim. These studies cover the actual six starting skill spells,
plus separately learnable Conjure Rain. The original design-only pass preserved
Unity code and donor assets; the integration log records subsequent code changes.

## Current revision

The canonical Blender source now contains the subsequent [larger, denser ImageGen-guided readability revision](SPELL-3D-READABILITY.md). That living log owns current native acceptance, validation and delivery links. The P1 videos and studies below show the earlier revision and remain design history.

## Deliverables

- **Earlier P1 Unity gameplay:** [native-timing video](Verification/StarterSpell3D/Integration/NativeMedia/SSN-0d7a74963b314b6f9c9270a00d13846d/starter-spells-unity-native-timing.mp4),
  [loop](Verification/StarterSpell3D/Integration/NativeMedia/SSN-0d7a74963b314b6f9c9270a00d13846d/starter-spells-unity-native-timing.gif), and
  [contact sheet](Verification/StarterSpell3D/Integration/NativeMedia/SSN-0d7a74963b314b6f9c9270a00d13846d/starter-spells-unity-contact-sheet-refined.png).
  These are captured command fixtures in the real game; the other previews below
  remain Blender design studies. [Capture provenance and limitations](Verification/StarterSpell3D/Integration/NativeMedia/SSN-0d7a74963b314b6f9c9270a00d13846d/README.md).
- [Editable seven-scene Blender source](../ArtSource/StarterSpell3D/starter_spells.blend)
  and [reproduction/export guide](../ArtSource/StarterSpell3D/README.md).
- [Six starting spells — animated review](../ArtSource/StarterSpell3D/renders/starter_magic_loop.gif)
  and [contact sheet](../ArtSource/StarterSpell3D/renders/starter_magic_contact_sheet.png).
- [Conjure Rain — animated utility review](../ArtSource/StarterSpell3D/renders/conjure_rain_loop.gif).
- Individual slow-review and native-timing MP4 clips are in
  ArtSource/StarterSpell3D/renders; design FBX assemblies are in exports.

## Refinement P1 — brighter payoff, gentler motion

User direction: make the studies a little more flashy and comfortable.
Interpretation: more satisfying local color and contact detail, softer motion
transitions, and a relaxed return to neutral. This is a moderate polish of all
seven Blender studies. Existing game mechanics, cast phase times, targeting
shapes and the 56° camera remain the reference. P1 owns the art; the user's
follow-up instruction to put it in the game is tracked in the integration plan.

Pre-edit sweep: reviewed the current builder, pulse/pose keys, renderer,
export/readback and media contracts. The previous scene is the visual baseline.

| Existing fact | Refinement consequence |
| --- | --- |
| Pulse reaches full scale within a few 100-fps frames | Spread the reveal over a readable eased envelope; test actual sampled motion before and after. |
| Shape colors are static, opaque mesh materials | Add restrained colored highlight layers and moving carved accents; no dependency on alpha animation or bloom. |
| Semantic contact/clear keys already track current phase times | Improve interpolation and secondary motion inside those times; do not lengthen the gameplay clock to make a slow review attractive. |
| Rig has mitten hands, fixed cell root and unchanged equipment sockets | Improve wrist/shoulder follow-through, keeping the lower body and source position stable. |
| Previous export and image-density failures have genuine regression pins | Retain those checks, including evaluated fragment placement and square-display PNG metadata. |

- Ember: a fuller warm core, curved flame wake and a compact outward ember
  finish; one coherent projectile remains readable.
- Flaming Hands: layered folded palms with a warmer inner edge and a small
  cushioned contact curl, contained within the adjacent cell.
- Jet Blast: a fuller water fold, rounded foam accents and a weighty splash
  settling into the existing four-cell fan.
- Ground Surge: staggered warm ground pulses and a small traveling highlight
  along the four-cell stitches; one smooth surge, not repeated white flashes.
- Rime Grip: thicker pale rim accents and a few softly drifting ice chips;
  closing clamps stay low and blunt.
- Calm: a fuller violet/ivory loop with gentle unfurling contact arcs beside
  the recipient; a relaxed finish without head-covering graphics.
- Rain: softer droplet rhythms, small bead ripples and gentle crop motion at
  the same three valid crop cells.

Verification: archive a baseline, record a failing sampled-motion requirement
before changing the builder, rerun the existing geometry/rig/cleanup/export
gates and independent camera/media checks, and visually compare real Blender
renders at both playback speeds. A dedicated actual-asset mutation check must
reject a reintroduced abrupt reveal or persistent fragment. Comfort remains a
visual-design judgement, not a medical or accessibility certification.

## Plan and verification sweep

1. Verify normal bootstrap, exact starter grants, mechanics, result hooks and
   existing art/rig conventions before designing shapes.
2. Author six distinct cast gestures and transient mesh studies using the
   existing teal player rig, with metre/cell scale and the current 56° camera.
3. Supply a seventh clearly labeled Conjure Rain study: starter access comes from reading the carried book, with a Hydromancy
   purchase also possible. It waters crops, not combat targets or the whole floor.
4. Render a visual sheet and animated previews from Blender; retain editable
   source, phase markers, a reproducible builder and Unity handoff constraints.
5. Independently review silhouettes, timing, native shape fidelity, cleanup,
   rig/mesh animation contracts, and the distinction between design and runtime.

| Verified source | Correction / consequence |
| --- | --- |
| StartingSpellKit.SpellClasses | Six starting actives, in order: Ember Spit, Flaming Hands, Jet Blast, Ground Surge, Rime Grip, Calm. No extra weapon/passive skill grants implied. |
| Farming starter kit and WateringGrimoire | Conjure Rain is carried as a learnable book, not initially learned/equipped. Its study is separate from the six default hotbar entries. |
| Pyromancy_FlamingHands | One selected adjacent cell; do not design a wide damaging cone from the old prose. Empty-space release still succeeds. |
| Hydromancy_JetBlast | Creature cone length 2; ground coating follows the centerline and actual final target positions, not an invented blanket cone puddle. |
| Galvanism_GroundSurge | Four-cell line, damage and attempted one-cell push. The 40% roll is for target Electrified; ground charge is a separate recorded write. |
| Cryomancy_RimeGrip | Nearest target within 5 cells; water-only empty-line fallback can freeze existing water. Dry empty refusal must not leave ice. |
| Spellcraft_Calm | Single projectile, range 6, no damage; Pacified does not mean recruited, healed, asleep, or controlled. |
| Existing SpellFxSequence and coordinator | Presentation follows resolved outcomes; meshes never perform targeting, rolls, damage, status application or occupancy. |
| Native 3D rig / presenters | Reuse character-teal skeleton and sockets. Existing native cast uses Interact for .22s; new Blender takes do not auto-integrate. |
| Current native camera | 56° down, not the older strict-overhead art study. One metre = one cell. |
| Native palette material path | Opaque, lit, fog-aware; emission/alpha shader animation is not currently portable. Use mesh transforms and bold opaque colors for the initial studies. |
| Phase-history magic prose | Gesture/material language informs art; current shipped combat spells remain mechanic authority. No new cosmology, faction claim or secret writing is added. |

## Art direction: small bindings, strong silhouettes

Magic looks worked by hand: pinch, fold, press, close, and release. Use broad
carved planes, shared dark ink (30,32,28), two or three tones per material,
restrained pale highlights and sparse physical threads. Preserve the hood,
face and hand silhouette. Avoid huge luminous spheres, ground-covering circles,
unreadable sparks and tall curtains that obscure one-cell actors.

The material language belongs to ordinary folk practice in this world. It does
not assign these spells to a god or faction, reveal the under-text, add a reagent
cost, or turn the everyday caster into a staff-dependent class.

| Ability | Gesture | Distinct mesh language / motion | Outcome constraints |
| --- | --- | --- | --- |
| Ember Spit | Pinch a coal near the mouth, brief shoulder recoil, flick forward | Asymmetric charcoal seed with three pointed ochre/ivory flame splinters; short broken soot tail; tight outward impact | Single nearest body, max 4; optional Burning and embers from recorded writes. Wet suppression produces a dull split and a small captured steam response only if that reaction exists. |
| Flaming Hands | Both palms open, then a short firm push | Two folded flame fans join into one low serrated sheet contained within the chosen adjacent cell; fingers remain readable | One-cell heat burst; no neighboring cone damage or persistent burning gloves. Valid empty cast still releases. |
| Jet Blast | Cup, draw back, unfold the palm | Solid river-blue folded ribbon with a broad ivory lip, three heavy droplets and a shallow broken splash crown | Cone length 2; move the target only to its actual resolved destination. Water remains only where captured writes say it does. |
| Ground Surge | Lower a palm and press toward the ground | Ochre-white angular stitches chase along a narrow four-cell strip, with a low hooked wave front; no sky strike | One owner response per resolved result; blocked pushes compress without translating. Persistent target charge is conditional; no made-up chain lightning. |
| Rime Grip | Spread fingers, then close one hand | Three blunt split frost clamps close around a target's lower body, with a thin cold thread; leave the head and upper silhouette exposed | Nearest target max 5. Clamp/status only if applied; death gets break-away pieces. Water-only branch uses flat plates on actual frozen cells, dry refusal leaves none. |
| Calm | Open hand, soften shoulders, slowly uncurl fingers | A traveling open loop of ivory/soft violet thread, then two angular crossings relax into an open arc near the target | Range 6 single target; no hearts, sleep, healing, ally conversion or area wave. Failed/already-peaceful result opens and fades without a new binding marker. |
| Conjure Rain — learnable utility | Count two beats with empty hands, then turn the palm down; an open book is optional learning art | A low sparse canopy of short rain strokes over valid crop cells; brief leaf dips and bead rings | Radius 3 square crop query, moisture top-up only. No combat Wet, floor puddles, new crop growth or permanent cloud. |

## Timing / deliverable boundaries

Use the existing presentation clock as the first design target: .12s cast,
.10s charge, .025s per recorded path cell when the family travels, .20s impact,
.18s aftermath. Rime Grip/Flaming Hands inscription families and Conjure Rain
field suppress travel. Preserve independent semantic markers for gather,
release, contact and clear; animation is retimed by the native coordinator
speed settings. A looping Blender study includes a reading pause and reset;
that loop/reset is not a turn, cooldown or gameplay delay.

All seven cast gestures must work with empty hands: no skill requires a book,
staff, reagent or equipped weapon. An optional book may appear in the learning
study only. Persistent Wet, Burning, Frozen, Electrified and Pacified indicators
follow current native state/goal and disappear when it clears; the .18s aftermath
is only a transient accent, not their lifetime or a cooldown animation.

Cast root stays at its native cell. Cosmetic debris is neither loot nor a
new environmental hazard. No Blender particle simulation, Geometry Nodes or
material keyframes are assumed to survive FBX. Lighting in a Blender preview
is studio presentation, not native Unity illumination or performance proof.

## Choreography and readable outcomes

The first six gestures share a stable lower body and change hands/shoulders,
so the player's cell and facing remain legible. Gesture names such as pinch
and close describe arm/wrist silhouettes on the existing nine-bone rig; these
studies do not add individual finger or facial bones. Accessories remain
attached to the original sockets. Hands do not vanish into an oversized effect; a cast does
not borrow movement or attack root motion. The silhouettes differ in grayscale:
pointed seed, short teeth, broad lip, angular stitch, closing clamp, open loop.

| Beat | Source gesture | Effect responsibility |
| --- | --- | --- |
| Gather, 0–.12s | Ember pinch; hands brace; water cup; ground palm lowers; frost fingers spread; calm shoulders soften | A small source accent, never an implied area hit. |
| Bind, .12–.22s | Hold one clear silhouette before release | Material gathers into the school-specific form. No target success marker yet. |
| Release / travel | Recoil/follow-through within the same cell | Use actual path and distance. Inscription effects appear at the recorded contact at release. |
| Contact, .20s | Source begins settling | One impact per resolved owner; move accents follow recorded initial/final cells. Shape and actual status communicate outcome. |
| Afterglow, .18s | Return to neutral | Loose fragments clear. Actual remaining tile/status state belongs to its independent native view. |

The delivered studies use 100 fps, retaining exact fractional contact frames
in the manifest and scene metadata. Integer Blender timeline markers are
rounded labels, not the animation clock. Videos sample every three native
frames: native-timing clips play at 100/3 fps, while review loops play the same
frames at 8 fps, 4.17× slower. The preview adds a neutral reading pause; this
is neither a game turn nor a cooldown. GIF centisecond timing rounds the slow
loop to approximately 4.62 seconds.

| Spell | Required successful reading | Counter-outcome specification |
| --- | --- | --- |
| Ember Spit | One small hot projectile and tight impact; captured embers | Wet/material suppression can still take damage without Burning. Death has no surviving flame loop. Empty refusal has no successful hit. |
| Flaming Hands | Both hands form one adjacent-cell burst | Empty cast still releases. Heat does not guarantee ignition. No false neighboring cone. |
| Jet Blast | Short four-cell fan, actual Wet, actual shove | Blocked rays have no wet fan; blocked shove compresses at contact without translation. Lethal hits have no living Wet loop. Empty cast writes only real ground water. |
| Ground Surge | Low line, distinct per-owner contact and actual push | Failed Electrified roll still has damage/contact. Blocked push stays in place. Ground charge is independent of target charge. Empty-line dispatch caveat remains unresolved in this design pass. |
| Rime Grip | Close around one lower-body silhouette, retain face visibility | Dry empty line leaves no frost. Water-only case makes plates on real frozen cells. Death ends living clamps; thermal thaw releases state-driven geometry. |
| Calm | One traveling loop followed by relaxed/open shape on actual pacification | Already-peaceful/brainless/miss has no new success marker. No sleep, hearts, allegiance change, injury or area wave. |
| Conjure Rain | Short drops over actual watered crops; leaf response | Empty cast finishes without invented crops. Moisture top-up is not growth, floor water or combat Wet. No held-book dependency. |

These counter-outcomes are authoring and adapter requirements. The first
rendered studies demonstrate the main visual treatment; a row being specified
here does not claim that every alternate animation has already been built.

## Blender and Unity handoff contract

- Author in metres with feet-root caster and separate effect origin, travel-tip,
  impact-contact and ground-contact pivots. The player occupies one cell;
  a visual overhang never adds occupancy or a hitbox.
- Preserve the existing nine-bone player skeleton and the four equipment
  sockets. Separate source-cast takes from travel/impact/afterglow assemblies.
- Use opaque flat-shaded meshes and transform/bone animation for portable
  motion. No mandatory bloom, high-frequency flashes, fluid simulation,
  Geometry Nodes, light animation or alpha shader keys. Studio lighting is
  explicitly a preview environment.
- Initial visual ceiling: fire/splash/wave contact stays low; frost clamps
  leave the upper body clear; flying heads stay roughly at hand/face height.
  These are art targets, not verified bounds for every animation/attachment.
- The current Unity camera stretches vertical projection to preserve ground
  registration. The Blender studies use a numerically verified equivalent:
  orthographic HORIZONTAL sensor fit, pixel_aspect_x = 1/sin(56°), y = 1,
  judged in square-pixel output PNGs. Sixteen numerical assertions across four
  aspect ratios confirm equal ground X/Y spans and height parallax h×cot56.
  Blender clamps pixel aspect below 1, so setting y=sin56 silently fails.
  This verifies projection geometry, not native fog, lighting or all animated
  viewport bounds; integration must still cover facings, borders and equipment.
- FBX roundtrip checks establish retained skeleton and animated mesh motion,
  cleanup and fixed roots in Blender. These exports remain design assemblies:
  they retain the studio actor offset and use -Z forward / Y up, while the
  production art pipeline uses Z forward with an export rotation. The subsequent integration verifies Unity
  axes, bind pose, socket/local-origin extraction, clip import and controllers
  for the separately extracted runtime package. These original studio FBXs
  are not drop-in Unity prefabs.

## Readiness and completed Unity handoff

- 🟢 Starter identities, mechanical shapes, existing teal rig and current camera verified.
- 🟢 Seven Blender designs, actual motion renders, geometry/rig/export readback and independent media checks complete.
- 🟢 The subsequent native 3D adapter, shader/pool integration and live acceptance are complete in [SPELL-3D-INTEGRATION.md](SPELL-3D-INTEGRATION.md).

The shipped adapter remains under WorldFxCoordinator's one queue consumer and
uses copied results, full/reduced/off settings, per-fragment visibility, actual
contact cells, interruption, zone/load/view changes and pooling cleanup. It never
replays damage or status logic. Multi-cell owner impacts deduplicate while actual
affected ground cells retain independent visuals. The integration fixes the
previous empty-Ground-Surge capture/refund inconsistency and canonical-anchor
spell capture errors, with explicit RED/GREEN and native outcome receipts.

The design FBX files above remain studio assemblies. Unity uses the separately
extracted semantic meshes, transformed animation data and compatible native-rig
clips described in the integration log; the studio assembly itself is not
instantiated in gameplay.

## Sources

- [StartingSpellKit](../Assets/Scripts/Gameplay/Skills/StartingSpellKit.cs), [normal bootstrap](../Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs) and [new-game items](../Assets/Scripts/Gameplay/Bootstrap/NewGameLoadout.cs).
- The six named skill implementations; [Conjure Rain](../Assets/Scripts/Gameplay/Skills/Hydromancy_ConjureRain.cs), [book Read behavior](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs), [Hydromancy skill data](../Assets/Resources/Content/Data/Skills/Hydromancy.json).
- Assets/Scripts/Gameplay/Effects/SpellFxSequence.cs; Docs/SPELL-FX-ART.md.
- ArtSource/Village3D/build_scene.py and village_master.blend; Docs/NATIVE-3D-CAMERA-ANGLE.md.
- Lore/10_Bible.md, Lore/11_SecondSpine.md and aligned Lore/History/09_Magic.md.
- Docs/LIVING-WOODCUT-ART-DIRECTION.md and Docs/VISUAL-IDENTITY-BIBLE.md.

## Implementation log and review

- Read CLAUDE.md and preserved pre-existing workspace. Read-only starter and
  animation-pipeline audits completed before asset authoring. No Unity run,
  production edit or gameplay test result is claimed for this design phase.

- Independent design cold-eye corrected two misleading implications: Rain
  requires neither a held book nor book-only learning, and persistent statuses
  must not inherit a transient clip lifetime. Ground-charge wording now clearly
  separates terrain charge from the target's conditional Electrified result.
  Both corrections are in the design before Blender finalization.

- Independent installed-Blender camera calibration passed 16 numerical
  assertions at landscape, square, portrait and wide output ratios. One-metre
  ground spans match within 4.77e-7 relative error; vertical parallax matches
  h×cot56 within 2e-6. The final studies adopt the tested projection settings.
  [Calibration source and receipt](Verification/StarterSpell3D/camera-calibration/coo-spell-camera-calibration.json)
  are retained. The small unrendered save_render probe showed square density,
  but did not predict Cycles' actual PNG metadata; see the later media review.

- First actual Blender stills prompted a value-contrast refinement and a
  camera-facing adjustment to Calm's loop. The utility cast is empty-handed;
  Calm's successful recipient uses the existing olive character rather than a
  literal brainless practice dummy. Other targets are labeled studio stand-ins.
- Manual grid review caught three draft authoring mismatches before final
  motion renders: rain beds beyond radius three, ground stitches offset half a
  cell toward the source, and Jet Blast's missing lateral fan pieces. These
  are design-asset corrections, not claimed gameplay bug fixes. A saved draft
  was sampled independently before the corrected source was accepted.

- Independent evaluated-mesh review of a frozen draft recorded 9 passing
  controls and 6 genuine geometry failures: four Ground Surge groups were
  half a cell behind, and both Jet Blast side cells contained zero projected
  effect area. The corrected frozen file passes the same **15/15** assertions.
  The rain edit had already landed in the frozen draft; its range checks are
  pinned-correct, not a claimed RED-to-GREEN result. Calm's hero is deliberately
  mid-travel; its actual contact frame already reached the recipient correctly.
- All seven saved scene cameras pass **35/35** separate numerical checks for
  the calibrated projection. First independently reviewed Blender SHA-256:
  `d43a03b36b1bbc4e74bd98ca57a9e1cee89179e19831b5b87389743aec4b6020`.
  This receipt precedes the later fragment-pivot correction below.

- Final motion review found Calm's traveling loop correct but its separate
  finishing crossings displaced toward scene origin. The shared fragment-pivot
  helper read a deferred Blender transform immediately after assigning it.
  The saved-file gate recorded 31/37 PASS before correction and 37/37 after
  explicit transform composition. Evaluated vertices at all 37 preview samples
  show changes in Ember Spit, Jet Blast, Ground Surge and Calm; their complete
  frame sets were regenerated. Flaming Hands, Rime Grip and Rain remain
  geometrically identical at those samples.
- Independent draft media review passes 109 checks and fails seven actual
  Cycles PNG metadata checks: anisotropic density metadata could make other
  viewers stretch the already corrected raster again. The composed videos and
  GIFs are square-pixel correct. Raw still/frame export strips only PNG pHYs
  metadata and verifies unchanged compressed IDAT pixel bytes, with no
  resampling. The earlier unrendered metadata probe is insufficient evidence
  for actual renderer output. Final media verification passes below.
- Final corrected source SHA-256:
  `ed172cd7fb058d6aa22045454fb799e596273797b224bf02ee13361a0bfb071b`.
  Unchanged independent geometry checks pass **15/15**, and the seven saved
  cameras pass **35/35** again. Earlier draft receipts remain archived; final
  source reports are in Verification/StarterSpell3D/independent-geometry.
- Independent final media review passes **124/124** assertions: seven raw
  37-frame sequences, seven hero stills, sixteen MP4s and two animated GIFs.
  All output has correct square-pixel display; slow videos last 4.625 seconds,
  native-timing videos 1.110 seconds, and GIFs 4.620 seconds. Encoded frames
  match rendered sources. Thirty-millisecond native sampling introduces
  0–20 ms contact-frame quantization; the editable source retains exact keys.
  [Final media receipt](Verification/StarterSpell3D/independent-media-review.json).
- Root visual review of final gather/contact/aftermath/clear frames for all
  seven studies confirms no residual origin marker, clear character heads and
  transient cleanup in the shown phases. The final mechanics/document cold-eye
  found no remaining mismatch within the design-study scope. This is not a
  claim of native combat feel or unrendered branch acceptance.
- Protected-source verification passes **3,750/3,750** pre-existing files:
  gameplay scripts, content, native art, main scenes and original village
  sources match their pre-design hashes. Baseline and final receipt are archived
  under Verification/StarterSpell3D. Existing unrelated work remains intact;
  no Unity tests, import or gameplay changes were performed in this design pass.
