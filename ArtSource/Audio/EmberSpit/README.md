# Ember Spit sound design

Status: approved revision3 integrated and verified in Unity, 2026-09-11. Original synthesized CoO audio. No recordings, external model or Qud audio used.

## Current direction

Three simultaneous sound layers make Ember feel fuller:

- **Wind:** a deep, broad blowing body with higher airy turbulence. No pitched oscillator or isolated bass drum note.
- **Burning wood:** dense fine fire crackle and irregular larger log fractures. Its crescendo follows the same swell as the wind, with enough level to remain a substantial part of the mix.
- **Contact:** a deep crush of wood and coals, with overlapping fractures over roughly 200 ms and fading cinders. Wind and fire continue underneath its onset.

The user's latest request deliberately supersedes the earlier quiet-impact direction. Deep contact weight is now wanted; narrow pitched thumps remain outside the design. Independent signal review verifies layered contribution, not human listening quality.

## Timing and delivery

Release remains .220 seconds and the three-cell example makes contact at .295 seconds, as in the current Blender manifest. Each composite is now 1.100 seconds because the sound has a longer natural decay; animation timing is not stretched.

Charge and flight are complementary event-relative stems, with a 40 ms handoff beginning at release. They reconstruct the continuous wind/fire body exactly. The impact stem starts at its own actual contact event. Three full-timeline wind/fire/crush layers are also exported for sound editing.

Unity now imports the three full-timeline wind layers, three fire layers and three event-relative impact clips unchanged. The presentation coordinator starts matched wind/fire together and plays impact once at the selected visual renderer's real contact time/cell. Misses have no crushing hit; natural tails do not block turns. F10 includes sound volume. The fixed three-cell composite remains an audition/reference, not the runtime event schedule. See [integration and native verification](../../../Docs/EMBER-AUDIO-INTEGRATION.md).

## Current files

- `preview/ember_spit_layered_v3.wav`: three current variations, normal speed and original gain; 5.5 seconds.
- `preview/ember_spit_v3_layer_breakdown.wav`: wind, fire, crushing, then the combined first variation at actual mix gains; 6.25 seconds.
- `wav/`: 12 current mono 48 kHz, 24-bit PCM WAVs: charge, flight, impact and composite for each variation.
- `layers/`: nine current mono 48 kHz, 24-bit PCM WAVs: wind, fire and contact-crushing layer for each variation.
- `float-masters/`: corresponding floating-point source masters.
- `create.py`, `verify.py`, `verify_layers.py`, `manifest.json`: reproducible synthesis, verification and hashes.

The manifest identifies the 23 current delivery WAVs. Earlier preview filenames and archives are historical; the new ZIP includes only current manifest-listed audio and associated masters, source and receipts.

## Reproduction and verification

Installed tools are Python, NumPy, SciPy and FFmpeg; no new download was needed. Run `OPENBLAS_NUM_THREADS=1 python3 create.py`, then `OPENBLAS_NUM_THREADS=1 python3 verify.py` from this directory. The verifier dispatches to the revision 3 layer checks.

A single shared gain preserves the balance between editable layers, event stems and previews. Final checks verify actual PCM format, 4x oversampled peak headroom, DC and click-free boundaries, low-frequency wind/crushing energy, high-frequency fire detail, simultaneous precontact rise, meaningful layer contribution, delayed contact onset and exact decoded recomposition. Negative controls remove each layer, replace the crescendo with a constant body, or introduce silence/clipping. Preview checks establish unchanged sample order, speed and gain.

Current result: `verification/17-final-layered-checks.json`. Maximum measured oversampled peak is approximately -3 dBFS. Numerical checks cannot establish perceived realism, listening comfort across speakers, or headphone/speaker hardware output. The separate integration audit verifies real Unity playback and contact handling.

## Revision history and review

- Revision 1 introduced three procedural variants and separate stems. The user found them too drum-like. Preserved in `revisions/01-percussive-draft.zip`.
- Revision 2 removed pitched tones and sustained an airy wind/fire body. The subsequent request asks for deeper and more distinct simultaneous layers plus a heavy crushing impact. Revision 2 is preserved in `revisions/02-wind-fire-draft.zip`.
- Revision 3 began with an actual missing-layer RED in `verification/12-layered-direction-red.json`. Independent review measured only about .3% low-band energy in the previous wind, confirming the depth gap.
- The first revision 3 candidate passed signal and layer contracts. A proposed reversed-crescendo counterexample was invalid: reversing a long fade can itself create a rise in the tested window. Its failed receipt is retained as14; the corrected counterexample repeats a constant-energy segment and is rejected by the same unchanged rise gate.
- Increased only the fire layer by 1.3 after review to bring crackling forward. Final files use the same shared pack gain and preserve exact event/layer reconstruction. Both preview forms are checked against decoded delivery samples.
- Cold-eye review checked gain preservation, overlap, actual decoded boundaries and contact placement, irregular multi-event crushing instead of a lone pitched strike, and package selection that excludes old previews or unrelated nested files.
- No Unity scene, gameplay script or pre-existing unrelated asset was changed. No claim of completed Unity audio integration or human auditory review is made.
