# GPU02 independent bounded review

Status: evidence PASS, 72 checks. No controlled rendering defect demonstrated; no production lighting change recommended. This is a diagnostic observation, not final native gameplay or artistic acceptance.

Run `39521efc08834ea0bbe5d3282c2b9518` explicitly sampled `Assets/Scenes/Main/SampleScene.unity`. The report records Metal on Apple M5, Linear project color space, and sRGB render targets. All 20 PNG dimensions, hashes, decoded dark-pixel counts and decoded mean pixel values match the report. The source-scene and dirty-flag receipt is preserved, and `captures/scene-restore.json` records restoration of the original untitled editor scene setup. The launcher reports zero changed protected files. Raw log has no C# compiler/shader errors; one nonfatal startup licensing token message precedes the scoped probe, which records no unexpected logs.

Every GPU02 PNG is byte-identical to its GPU01 counterpart. Loading the actual SampleScene therefore corrected provenance without changing these controlled results. Original renderer probe bindings and explicit source SH also produce pixel-identical frames at each resolution.

## Subject-only measurements

Mask: subject pixels are white in the same geometry's unlit-white positive control (all RGB >=250), with one pixel removed from boundaries using a 3x3 minimum filter. The floor stays below the threshold. At 768 pixels high, 142,561 subject pixels remain. These are encoded sRGB-byte statistics, not linear luminance or whole-image brightness.

| Condition | Mean RGB (0–255) | 1st percentile RGB (0–255) | Subject pixels with mean RGB <64 |
|---|---:|---:|---:|
| Source SH, soft shadows | 140.75 | 27.33 | 10,531 |
| Source SH, shadows off | 143.76 | 27.33 | 8,993 |
| Neutral SH, shadows off | 181.15 | 104.67 | 0 |
| Neutral SH, soft shadows | 179.53 | 104.33 | 0 |
| Unlit source palette | 188.48 | 113.00 | 0 |

Source ambient-only illumination on white subjects is uniform mean RGB 73.67; the neutral SH positive control is 246.00. This verifies that the SH intervention reached the renderers. Removing shadows changes subject mean RGB by only 3.01, and leaves the darkest percentile unchanged; neutral SH without shadows raises mean RGB 37.39. The same direction holds at 216 pixels high, approximately 36 pixels per game cell.

The controlled images show intact imported ridge and boulder geometry with intentionally faceted directional shading. Dark facets remain with shadows disabled and lighten with the effective neutral ambient control. Shadow maps contribute smaller localized changes; this does not establish self-shadow acne as the cause of the darker native screenshots. No texture-corruption or missing-geometry defect was demonstrated by this bounded probe.

## Native-image comparison and honesty bounds

The earlier `MCN-f8a9acbea29d442e9e75c6f04cc71aa6-restored-save.png` is visibly darker, including small leaf edges, at roughly comparable pixel density. That is not a controlled defect comparison: it used the previous 81-degree camera, actual native illumination, visibility/memory, final 2D compositing, and gameplay presentation. GPU02 uses 66 degrees and a direct WorldCamera capture with white FogTexture illumination.

The native shader deliberately multiplies visible pixels by the gameplay FogTexture RGB. A pilot cell without an additional light source starts at ambient 0.40 and Stump tint (0.98,0.93,0.92), yielding approximately (0.392,0.372,0.368), versus (1,1,1) in this probe. This expected attenuation can deepen the observed shading, but it was not independently reproduced by the controlled frames, so it remains an explanation consistent with the source contract rather than proof of the entire native-image difference.

Only two actual imported subjects were exercised. The script does not certify the complete chunk, native fog transitions, animation, final compositing, or visual feel. Historical scene/file preservation relies on the recorded launcher/scene receipts; the independent script verifies their values and frame artifacts, not the past Unity process. Neutral SH is a strong positive control, not an approved setting. Preserve the lighting contract and finish the requested camera verification; do not wash out the scene to hide an unproven issue.

## Reproduce

Run `python3 verify-probe-evidence.py` from any directory with Pillow and numpy available. The script reads this run's captured files and writes only `independent-evidence.json`. It never launches Unity or modifies Assets. All original screenshots and diagnostic receipts are unchanged.
