# Overwrit concept critique and native parts map

2026-10-27. Current pass: actual playable before → built-in imagegen iteration
→ native-parts specification. **No native atlas or runtime integration yet.**
Exact prompts, input/output hashes and reference credit are in `../references.json`.

The target was actual `overwrit-before-06-bench-approach.png`: player75,18 and
bench76,18 on ordinary4,12. Actual packaged Grovelands09 supplied the approved
sprite style only. The [NPS grass photo](https://www.nps.gov/whsa/learn/nature/plant-survival-strategies.htm)
supplied a narrow shoot/ground contrast reference; it is not an instruction to
add desert species, dunes, flowering stalks or extra vegetation. Production
will use original native pixels, not cropped reference imagery.

V1 makes the pale regional material and low shoots legible, but repeats too
much mottled floor noise and enlarges the bench. Reject that floor. V2 removes
most texture and better expresses the quiet inward field, but is still a
concept: generated gradients, resampled player/UI, altered placements and
bench scale are not acceptable production output. Neither concept is a
gameplay screenshot, native-resolution sheet or completion evidence.

Independent review also caught uneven, flowering-looking tips in v2. The
actual NewGrowth description says every tip reaches the same height. Native
variants must share that clipped top line and vary lower leaves/stems only;
reject flower/seed-bead shapes from the generated image.

Use v2 for the quiet material hierarchy and the shoots' compact silhouette.
Native construction must flatten the gradients, restore all real entity
positions, retain the approved player and UI unchanged, and fit each new
foreground into its single16×24 cell. Do not downscale a scene crop as a kit.

| Native part | Actual owner | Proposed cells | Required distinctions |
| --- | --- | --- | --- |
| Pale scraped ground | OverwritGround |0–3| Opaque shared base; rare low-contrast scuffs, no per-cell border. Cultivated/Plantable source floors retain soil/crop rendering. |
| Low clipped young shoots | OverwritNewGrowth |4–5| Transparent, two compact clustered silhouettes with equal tip height. Not crops, harvest plants or mature grass. |
| Plain inward-facing wooden bench | OverwritPilgrimBench |6| Transparent seat/back/feet; no service icon. Exact observed owner is Wood, solid, Structure12/12. Partial damage keeps body; gone/changed states follow truthful fallback. |
| Short blank stone marker | OverwritWaymarker |7| Transparent, legible against ground, no text/arrow/service cue. Registered but not naturally observed; native fixture coverage cannot substitute for actual observation. |

Proposed atlas128×24; eight16×24 cells,16PPU, center pivot, Point/Clamp,
uncompressed, no mipmaps. Palette roles: muted pale warm-grey ground, two
nearby scuff values, dark taupe contact/outline, restrained sage shoots and
ivory tips, desaturated brown wood and cool-grey stone. Freeze actual colors
against approved player contrast and day/night captures before export.

All5 authored pilgrimage scopes currently face inward west; verify the small
bench's inward read in actual4,12. Do not import the concept's large horizontal
bench literally. Preserve editable source and record the final frame map.

Whole-scene acceptance remains open. The Blank's8 actual shoots and rim
clusters are deliberate source topology, but that does not excuse an unfinished
appearance. Judge normal-zoom after views against the real before captures,
including travel from GreenRim through Blank and DryRim. Keep the region quiet
without repeated high-contrast glyphs or arbitrary new clutter. The actual
viewport is59 columns, so no full80×25 coverage is implied.

Next: implement the bounded tests in
`Docs/Verification/Live16x24/overwrit-native-preflight.md`, save RED, author the
editable native kit, integrate through the regional catalog, run GREEN, then
repeat earned checkpoints and ordinary movement/reader/save/Continue actions.
The Overwrit is still missing native art; Stump and remaining world obligations
remain active. Nuanced animation is deferred.
