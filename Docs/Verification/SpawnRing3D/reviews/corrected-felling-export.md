# Corrected Felling export: independent final topology check

Read-only completion, 2026-09-10. The eight frozen candidate FBXs under `/tmp/codex-spawn-ring-art/hero-correction/models` match the eight adopted `ArtSource/SpawnRing3D/models` files byte for byte. The adopted and candidate catalogs also match, SHA256 `bb7a1f3f60083b19ae2e346d2b22c13a525f505d723d68152d9fa287938d3efb`. Comparing the prior independent 55-model hash ledger identifies exactly the eight declared changed Felling files.

All **218** adopted FBXs were independently decoded again. Every raw triangle-equivalent count equals the corrected catalog, and there are **zero zero-area triangle fans**. The scan record is `corrected-triangle-scan.json`, including all model hashes and individual candidate results.

All 218 models were also screened again for the preceding n-gon projection risk. **Zero caps are now flagged by either the first-three-vertex or largest-area-triangle projection screen.** The seven prior large root masses have no remaining eight-gon bark caps. The eighth model, southeast-middle-ridge, retains its two ordinary eight-gons from the separate stone fallback recipe; these are clean controls, and that model's triangle count remains 262. Existing shelf-fungus twelve-gons remain; they also pass the narrow screen. Six cyan models have only edge-on projection crossings, unchanged and not classified as defects.

| Corrected hero | Raw triangles |
|---|---:|
| stump-main | 32936 |
| west-root-buttress | 5654 |
| east-root-buttress | 7098 |
| far-east-root-buttress | 1668 |
| southwest-root-arch | 996 |
| southwest-foreground-rock | 476 |
| southeast-foreground-root | 1052 |
| southeast-middle-ridge | 262 |

Independent raw-file results agree with the separate real Unity receipt read from `/tmp/codex-ring-import-CSd9hDfB/import.json`: `passed-import-validation`, 218 model rows, same catalog SHA. Its raw `unity.log` contains **zero self-intersection/discard warnings**. I did not launch or control that Unity process. Root owns the full scene/hash/GUID and repeat-import acceptance.

This closes the specific zero-area and ambiguous-cap defects; it does not claim general manifoldness, absence of overlapping surfaces, exact native-mask footprint verification, GPU correctness, performance or visual acceptance. The source now documents exact polygon-vs-native-cell clipping, but this scan did not independently recompute its entire placement mask. No repository changes were made.
