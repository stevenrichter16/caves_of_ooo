# Ember Spit sound design

Status: three variants created and technically verified, 2026-09-11. Original synthesized CoO sound; no sampled recordings, external model, or Qud audio used.

## Direction

A small charcoal seed ignites, flicks forward with a continuous warm rushing hiss, then lands with a restrained papery tick and short sizzle. Rounded transients and a filtered high end keep repeated casts comfortable. The projectile carries the energy; contact must not become an explosion or Flaming Hands-sized roar. Three deterministic variations retain the same identity.

## Timing and delivery

The current Blender manifest gives release at .220 seconds and three-cell contact at .295 seconds. A .800-second composite illustrates that example. Separate mono charge, flight and impact clips start at their own event time; a future integration must use actual release/contact events, suppress impact on misses, and manage travel duration rather than blindly replay a fixed three-cell composite. This task creates sound assets only; no audio is wired into Unity yet.

Deliver48kHz24-bit PCM WAVs, three variants of each stem and composite, plus a listening reel. Preserve relative stem gains with one shared pack gain. Keep sample and4x oversampled peaks below -1dBFS, negligible DC, quiet start/end boundaries and a quieter impact than flight. Float generation masters are also retained. No hard limiting to disguise clipping.

## Tools and verification

Python, NumPy, SciPy and FFmpeg are already installed. No additional download or paid service is necessary. `create.py` synthesizes and exports the assets; `verify.py` checks decoded delivery files, format, timing, signal boundaries, variant diversity and relative impact energy. The verifier ran before generation to record missing-asset RED; it now passes against generated files, with silence/clipping/loud-impact mutation controls.

Verification can establish technical signal properties and authored synchronization. It cannot establish a human listening judgment, speaker-specific comfort, or gameplay audio integration. The final inline reel is provided for listening.

## Implementation log

- Read project methodology and actual Ember manifest:100fps, contact29.5, release after .12-second cast plus .10-second charge. Installed synthesis/conversion tools are sufficient. Existing Blender/Unity animation work remains separate.
- `verification/01-before-generation-red.json` records real missing-file failure before the synthesis source was authored.
- Generated three seed variations with continuous filtered turbulence, a short descending warm body, fine charcoal grains and separate restrained impacts. A single shared gain preserves stem balance; PCM24 delivery retains float source masters.
- Technical review found the first impact roughly20dB below flight RMS: too little emphasis for a distinct contact cue. Raised only its authored layer gain by3, retaining the stricter impact-versus-flight energy bound. Earlier check receipts are retained as historical measurements; the current manifest hashes and `verification/06-refined-delivery-checks.json` describe final files.
- Final checks pass for all12 mono delivery clips and the4.3-second normal-speed listening reel. Maximum4x oversampled peak is approximately -4dBFS. The reel exactly preserves composite gain and timing. Silence, clipping and an oversized impact are each rejected by their intended gate.
- Cold-eye review: separated event-relative stems from the fixed three-cell preview, checked actual24-bit headers and decoded recomposition, retained nonidentical variants, and verified fades/DC/headroom. No Unity scenes, game scripts or pre-existing assets were changed. Human listening assessment remains open; technical analysis is not represented as an auditory review.

## Files and reproduction

- `preview/ember_spit_three_variations.wav`: all three casts at normal speed with quiet gaps.
- `wav/`:12 Unity-ready mono48kHz24-bit WAVs: charge, flight, impact and three-cell composite for each variant.
- `float-masters/`: corresponding floating-point masters for further editing.
- `create.py`, `verify.py`, `manifest.json`, `verification/`: deterministic authoring, delivery gates, hashes and history.

Run `OPENBLAS_NUM_THREADS=1 python3 create.py`, then `OPENBLAS_NUM_THREADS=1 python3 verify.py` from this directory. Integration should use the event-relative stems; the composite is an audition/reference clip.
