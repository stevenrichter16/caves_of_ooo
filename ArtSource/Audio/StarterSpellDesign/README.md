# Starter spell sound drafts — revision1 (rejected)

The user found these too similar. Current individually sourced material studies and layer auditions are in [revision2](revision2/README.md). This directory retains the original attempt for comparison.

Six auditionable designs following the approved Ember's layered depth. These are original synthesized sound assets outside Unity, ready for listening/design refinement. Ember and current gameplay are unchanged.

## Listen

The combined `preview/starter_spells_design_reel.wav` plays each first variation at its original speed/gain, with0.6s silence between spells. No spoken labels or music are mixed into the sound effects.

| Start | Sound | Individual preview |
| --- | --- | --- |
| 0.25s | Flaming Hands | [Listen](preview/flaming_hands_design.wav) |
| 2.10s | Jet Blast | [Listen](preview/jet_blast_design.wav) |
| 4.15s | Ground Surge | [Listen](preview/ground_surge_design.wav) |
| 6.30s | Rime Grip | [Listen](preview/rime_grip_design.wav) |
| 8.55s | Calm | [Listen](preview/calm_design.wav) |
| 10.95s | Conjure Rain (starter-book utility) | [Listen](preview/conjure_rain_design.wav) |

## Editable delivery

- `layers/`:72 mono48kHz24-bit full-timeline stems: four simultaneous material layers × three variations × six spells.
- `contact/`:18 corresponding event-relative finish clips. Their full-timeline versions already exist in `layers/`; use one representation at a time.
- `mix/`:18 complete successful-outcome audition mixes.
- `preview/`:six first-variation clips and the ordered combined reel.
- `float-masters/`:floating-point counterparts for editing without further quantization.
- `create.py`: deterministic original synthesis with per-spell shared mix gain. No downloaded/licensed samples.
- `manifest.json`: seeds, layer roles, reference contact timing, gains and230 audio-file hashes.
- `verify.py`, `verification/`: missing-delivery RED, final measurements and26 falsification controls.

Run `OPENBLAS_NUM_THREADS=1 python3 create.py` then `OPENBLAS_NUM_THREADS=1 python3 verify.py` in this directory. Existing Python/NumPy/SciPy were sufficient; no downloads were needed.

Design and mechanical constraints: [starter audio design](../../../Docs/STARTER-SPELL-AUDIO-DESIGN.md). These successful-outcome examples are not a complete runtime outcome pack. Calm/Rime success finishes and Rain crop patter must be conditionally dispatched when integrated. Natural audio tails must not extend turn waits.

## Verification and limits

All18 mixes pass PCM format, clipping/headroom, low/mid body, quiet boundaries, DC, exact layer recombination and matched contact-stem timing. All three variations differ. Each layer has measurable contribution; removing a layer, shifting contact, repeating an identical variant or supplying silence/clipping fails the relevant controls. The review reel preserves its source mixes at original gain, sample order and speed.

Worst4× oversampled peak is approximately−3.2dBFS. These numerical checks do not establish perceived realism, comfort or subjective balance. The user has not yet auditioned these drafts; no new in-game playback is claimed.
