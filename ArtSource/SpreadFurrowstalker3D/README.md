# Original furrowstalker source pack

Private F11 source work under `Docs/QUEST-FREE-HUNT-IMPLEMENTATION.md`. No content definition, Unity asset, native binding, imported controller or gameplay source is published here. Names are the root-approved original Furrowstalker/FurrowstalkerCorpse and proposed model IDs `spread-furrowstalker` / `spread-furrowstalker-remains`.

The live form is a long low, narrow-waisted quadruped, forward shoulders, flattened head, separate lower jaw, close ears, short broad paws and an uneven tapered tail. Pale throat/cheek breaks and brown flanks use the existing approved 24-swatch palette. The broad reed-ridged grazer, marlback armor/hands and existing animal assets are not repainted or replaced. Source bind geometry is 480 triangles/11 bones; remains 192 triangles, no rig. Geometry deliberately overhangs its logical cell like other original creatures; no new gameplay footprint, collider or root motion is implied.

Five existing state names are authored. Idle breathes, Walk alternates native quadruped legs, Attack snaps jaw/head, Hit recoils, Interact lowers head/jaw. Attack and Hit last 6 frames at 24 fps, Interact 14, Walk 16 and Idle 48. Root remains fixed. These durations fit the existing short presentation windows without delaying simulation. This is not a new Stalk/Chase runtime state system.

Feeding presentation follows the approved proposed source contract (the mechanics agent confirms stage 1 behavior is private TDD and stage 2 feeding is not yet implemented): two real paid progress actions may emit existing Interaction while the exact body is current; the third consumption action emits none. The pack does not itself trigger feeding, guarantee a kill/corpse, grant food, add harvest yield or infer a target through cover. Its remains are appearance only; the real CorpsePart must supply the correct source identity and roll.

## Files and reproduction

- `build_source.py` writes only the explicitly supplied kit JSON; `kit.json` is its current output.
- `export_source.py` reads the accepted mesh/rig helper functions without modifying or executing their full scenes. It writes FBX, catalog, palette PNG, source observation report, editable blend and review renders to an explicit output directory.
- `test_source.py` validates owned identities, finite/bounded geometry, exact palette, connected bones, five named motions, no root motion and static remains. Mutated swatch, invalid buffer, foreign weight/bone, omitted clip and motionless/root-motion copies are rejected.
- `test_export.py` independently checks actual FBX file hashes plus observed Blender geometry, rigid weights, finite normals/UVs and evaluated motion, with foreign buffer/weight counterexamples. Source render presence is the final output case.

Example: run the source builder with `--output <private>/kit.json`; run Blender background/factory-startup with `--python <private>/export_source.py -- --output <private>/Export --render`. Both test files run with ordinary Python. No Unity is invoked.

## Evidence and limits

Executed before authoring: 10 source cases failed because the kit was absent; 8 export cases failed because the source/export artifacts were absent. These are shared missing-asset preconditions, not 18 independent defects. The validators then test actual buffers and matched corrupted copies once valid assets exist. `source-red.log` and `export-red.log` retain the original outputs. Final source 10 and export 9 cases pass. The ninth export case was added for the narrow tail review below; all three current previews and the editable source file are complete.

Actual Blender evaluated Interact lowers the muzzle world-up from .246 to .105 units. Attack opens muzzle/jaw centroid separation from .071 to .139, then returns. Every clip deforms actual source vertices and Root remains stationary. These are source-coordinate observations, not Unity played motion or field behavior. The same source renderer conventions must still pass native axis, exact mesh/palette, current-owner/corpse source, hidden/load, action and camera checks after the mechanics/source gate.

Top, oblique and five-pose previews were independently viewed. The original low body, pale shoulder/face, separate lower remains and lowered feeding muzzle read consistently at source-preview scale. Walk remains subtle in a single still; played gait needs a later native review. Native import and actual live stalking/strike/feeding/escape/readability remain pending. Existing F9/F10 regression results do not validate this new pack. The original source and two raw RED logs must accompany eventual source adoption; no missing-library native result may be claimed until the real child blueprint exists.

Bounded source peer review by `/root/hauling_mechanics_audit` found no concrete anatomy, five-state routing, fixed-root, private-output or separate-remains mismatch. This read and the source deformation observations do not establish native feeding, motion or pixel acceptance.

## Narrow tail review

Root's first oblique review identified an apparently separated terminal tail block. Actual evaluated geometry remained connected in all 25 sampled frames, with only .010–.0114 units of overlap on the separating axes. The finding was a thin off-centre attachment and source readability issue, not a proven animated detachment. The original source and previews remain under `History/initial-tail`; `tail-contact-before.json` records the measurements.

One added source attachment test failed on all 25 samples before the change: the tip's inboard face centre did not enter the parent tail segment. It now passes, with a translated-away tip counter refused. Only the live tail-tip and its pale underside moved/grew slightly inboard. Rig, root, five motions, other body pieces, remains and triangle counts are unchanged. Current oblique preview has a clear connection. This test establishes sampled source attachment, not all interpolated Unity poses or native pixels.

## Self-review and next gate

- Q1, source: exact two original identities, approved palette, 480/192 triangles and 11 live bones; no borrowed model changes.
- Q2, counter-checks: invalid buffers/swatches/bones, missing or motionless clips, root motion and offset tail all rejected by owned validators. The first missing-file REDs remain accurately labelled.
- Q3, execution: 10 source + 9 export checks pass after actual Blender export. Only source buffers/poses and FBX hashes are checked; Unity import has not run.
- Q4, player-facing limits: source previews reviewed; actual imported axis, body/corpse current-owner recipes, save/hidden lifecycle, played gait and native encounter readability are pending. No hunt, corpse roll or feeding gameplay has been claimed here.

The peer's source review found no required anatomical/state-routing change. Root approved the general source direction and requested the addressed tail overlap. Native model binding and import are separate test-first work; no shared adoption is implied by these private manifests.
