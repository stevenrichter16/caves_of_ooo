# Spell animation verification — 5 September 2026

Implemented presentation for all **49 registered castable magic skills**, all eleven
rites, four elemental retorts, and existing elemental melee triggers. The
[coverage map](COVERAGE.md) lists the 53 catalog definitions. The system presents
copied gameplay results through one coordinator, pooled sprite effects or ASCII
fallback, actor casting poses, and bounded turn-wait handles. Original spell
targeting, RNG, damage, costs, cooldowns and save data remain authoritative.

Seven original school atlases provide distinct shapes and motion. Seven humanoid
cast sheets add 112 directional frames; other actors retain their Attack fallback.
F10 opens Off / Reduced / Full, speed, flash and shake controls. See
[implementation and controls](../../SPELL-FX.md) and [art pipeline](../../SPELL-FX-ART.md).

## Verified checks

| Check | Result | Evidence |
|---|---|---|
| Relevant Unity EditMode regression selection | 995 passed, zero failed or skipped | [Native test XML](EditMode-results.xml) |
| Original generated spell assets | Two Python checks passed: deterministic regeneration, dimensions, transparency, registration and catalog coverage | `ArtTools/test_spell_fx_assets.py` |
| Every showcase case through real commands | 66 cases, zero unexpected accept/refuse outcomes; every playback ended with zero active sprites/atoms and no blocking wait | [Runtime sweep](runtime-sweep.csv) |
| Representative phase captures | 72 PNGs: twelve cases at six presentation times, all seven schools and all ten catalog families | [Manifest](capture-frames.csv), [timings](capture-summary.csv) |
| Normal runtime motion | One accepted Ember Vein cast; 34 captured frames over 1.202 seconds; first sampled completed wait at 0.733 seconds after casting | [Live frame log](live/frames.csv), [completion](live/complete.txt) |
| Full / Reduced / Off live profiling | Three actual Thunderclap casts; all modes finished without blocking | [Profiler frame log](profiler-frames.csv), [summary](profiler-summary.json) |

The regression selection covers spell/rite outcomes, resonance, elemental skills,
status application, tile reactions, destruction, inputs, thrown items, sprite and
ASCII effects, actor animation, and save round trips. It is not the entire project
test suite. New checks include copied geometry before death/movement, rejected
casts, capped healing, actual cleansing and fuel consumption, queue ownership,
timeout recovery, pooling, missing assets, mode changes, and changing FOV at both
impact centers and cell fragments. Gameplay and cosmetic RNG remain separate.

## Visual evidence

![Ember Vein recorded during normal Unity updates](ember-vein-live.gif)

Inspected sequential live frames for charge, beam release, impact and cleanup, and
the following phase sheets for school silhouettes, registration and readability:

- [Kindle, Ember Vein, Jet Blast and Drench Lob](captured-phases-1.png)
- [Thunderclap, Glacial Wall, Ward Gleam and fed Bloodletter](captured-phases-2.png)
- [Acid Spray, Ground Surge, Overload and zero-mark Bloodletter](captured-phases-3.png)
- [Live motion frames with timestamps](live-motion-contact.png)

The deterministic phase captures execute each gameplay command once, then advance
only presentation clocks in paused Play mode. Their camera is 512×256, at 16 pixels
per world cell. The separate live recording uses normal Unity Update/LateUpdate,
with no manual simulation or animation ticks. Its GIF preserves recorded time
intervals, rounded to GIF timing precision, with a short final hold. Frame captures
verify selected phases, not exhaustive visual approval of every frame of all 49 spells.

Glacial Wall immediately changes FOV through its existing gameplay result; the
darkened cells in its capture are intentional. Earlier inspection exposed missing
showcase floor tiles and incorrect texture shape import; both were corrected before
these final captures. Flash direction and impact clipping after FOV changes were
also corrected and regression-tested.

## Measured budgets

Live Unity ProfilerRecorder sampling used an already-warmed 192-sprite pool:

| Mode | Peak live spell sprites | Peak whole-frame draw calls | Final active / blocking |
|---|---:|---:|---|
| Baseline, no cast | 0 | 116 | 0 / false |
| Full | 192 | 307 | 0 / false |
| Reduced | 48 | 164 | 0 / false |
| Off | 0 | 116 | 0 / false |

The pool stayed at 192 allocated sprite objects throughout, without growth. Scheduled
atoms are capped at 1,536, pending sequences at 256, and new dynamic lights at zero.
Dense casts can drop decorative sprites at the limit; target impacts share this
budget. Existing state indicators and damage readouts remain independently available.

The 66-case fixed-step sweep measured a maximum coordinator tick of **0.419 ms** and
a maximum initial acceptance update of **0.426 ms** in this Editor session. Mean tick
time across four seconds per case was 0.00266 ms, including idle tails; it is not an
active-animation frame average. These Stopwatch measurements exclude gameplay,
actor updates, rendering, GPU work, PNG encoding and IO.

Whole-Editor-frame allocation medians were 32,319 bytes in every sampled mode.
Peak cast-phase values were 325,512 / 315,846 / 314,844 bytes for Full / Reduced / Off.
These include gameplay, UI, editor tooling and the sampler; they do **not** establish
FX-only allocations or zero GC. This Unity Mono build's per-thread allocation API
returned zero even for a calibrated 1 MiB allocation, so that API was not used as
evidence. No standalone-build or target-device GPU performance claim is made.

## Remaining limits

- Shared atlases and per-spell composition provide the coverage; every spell does
  not have a separate bespoke sheet. Non-humanoid poses use existing fallback art.
- The new Flash control governs sprite spell impacts. Older actor hurt and
  environmental animation retain their existing controls and behavior.
- Retort coalescing is per pending batch. A hard five-second wall timeout may shorten
  unusually long playback at the slowest setting.
- Existing ground-write-before-refusal behavior in Ground Surge, Flame Jet and
  Backdraft was preserved. Refusals emit no successful spell sequence.

Verification ran in Unity 6000.3.4f1 with URP 2D on the local Mac. The showcase was
stopped after capture. Capture and profiling invoked no save APIs and saved no scene
edits. The final test rerun completed and restored SampleScene in edit mode; deliberate
invalid-save and duplicate-command fixtures emit expected diagnostic messages.
Existing unrelated workspace changes were retained.
