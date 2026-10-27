# Biome grading continuity: independent evidence review

2026-10-27. **Bounded pass for the reported Overwrit → Grovelands → Overwrit
travel and fresh-Continue color defect.** The corrected Overwrit scene uses a
consistent neutral, muted palette. No new significant visual defect was found
in these five captures. Whole-region fullness and remaining sprite coverage
are not accepted by this grading review.

The reviewer directly opened all five `overwrit-grading-01..05` PNGs at their
original 1280×800 resolution, read `biome-grading-actual-replay.json`, decoded
and compared the indicated RGB world rectangle, and parsed the saved native
RED/GREEN XML files. The reviewer also previously read the runtime correction
and nine-case lifecycle fixture. No Unity, Git, gameplay, source-art, save or
build operation was performed. This review document is the only write.

## Actual visual observations

| Capture | What is visible | Bounded result |
| --- | --- | --- |
| `overwrit-grading-01-continue.png` | Player 75,18, HP 38/40, the approached native bench, low clipped growth and a neutral grey field. The previously reported green cast is absent. | Corrected Continue view is visually coherent. |
| `overwrit-grading-02-bench-reader.png` | The same native wood portrait, inward-facing bench description and Structure 11/12 on the corrected field. | Damaged bench identity remains consistent; grading did not replace or recolor its source pixels. |
| `overwrit-grading-03-grovelands-travel.png` | Player 40,12, HP 38/40, dark green ground, teal water and existing fungal/root material identities. | The neighboring biome retains a distinct visible grading treatment. This is not a new full Grovelands coverage review. |
| `overwrit-grading-04-return.png` | Player 75,18 returns to the same bench-side scene with the neutral Overwrit grading. | Travel back agrees visually with the corrected Continue scene. |
| `overwrit-grading-05-fresh-return-continue.png` | The saved return scene appears unchanged after the root's reported fresh Launch/Continue. | Its decoded world RGB pixels match 04 exactly. Fresh-session operation is attributed to the root's replay record, not inferred solely from identical images. |

The crop glyphs still have conspicuous black-backed cells. Earlier
`overwrit-after-independent-review.md` remains historical layout and source
identity evidence; its tan-floor observations precede this grading correction
and are not the final accepted color treatment. The corrected grey palette
does not resolve the almost empty centered Pilgrimage approach, naturally
unobserved Waymarker, uncovered crops/stairs or whole-region fullness.

## Independently recomputed comparison

Compared `overwrit-grading-04-return.png` and
`overwrit-grading-05-fresh-return-continue.png`, interpreting the recorded
bounds `[0,40,940,640]` as left/top/right/bottom, with right and bottom excluded.
The resulting 940×600 rectangle contains **564,000 RGB pixels**, or 1,692,000
channels. Direct byte comparison found **zero changed channels**. The RGB
SHA256 is:

`8b43758ba102c04ee3b8f35dc368bf294936ae8a5fb3ceccdf180165e264ece7`

This exactly matches the replay record. The two complete PNG files are also
byte-identical; they must not be counted as two independently different
render states. Identical bytes establish the saved visual-state match, while
the fresh-session sequence and graph diagnostics come from the parent's
separately recorded operation.

The replay diagnostics report the displayed zone and patcher zone agreeing,
the manager belonging to the displayed graph, separate runtime/shared profiles
and fade 1. Overwrit is recorded as filter (.9,.9,.9), contrast −10,
saturation −40; Grovelands as (.88,.92,.85), contrast 8, saturation 6. These match
the authored palette constants read during the source review. The shared
filter remains (1,.97,.91) in all four diagnostic checkpoints. These are
reported runtime observations; this reviewer did not query Unity anew.

## Native evidence and artifact binding

The saved XML files independently parse as:

- `biome-grading-native-red.xml`: 9 unique cases, 8 failed and 1 passed; all nine
  belong to `BiomeColorPatcherTravelTests`. SHA256
  `ac22c7b43a800dd68dd7fb26dce31da53cfa8671d6544e1a19fab6d244ea668d`.
- `biome-grading-related-green.xml`: 533 unique cases, all Passed, including
  all nine lifecycle cases; zero Failed, Skipped or Inconclusive. SHA256
  `9eb3a0655171d9b276a5cc6a090f5e9ebd2fd8c8ce04d9573dabad1ad0c1c2a7`.

The earlier RED MCP job had no final result payload; the retained RED XML now
provides the exact 9/8/1 result. The combined GREEN MCP initialization timeout
is recorded in the replay JSON; the actual final XML is the basis for the
533-case result here. This reviewer parsed those files but did not run tests.

| Inspected actual PNG | SHA256 |
| --- | --- |
| `overwrit-grading-01-continue.png` | `75377d592643cc45d3fcd9646ffab4aa19fd7ae4bf59d37d0d899dcb4e1330d5` |
| `overwrit-grading-02-bench-reader.png` | `3c4877f779c6cdb0c1e70570cfa545f7ab7d7f6cf801fb75514f834eeb4227e4` |
| `overwrit-grading-03-grovelands-travel.png` | `f09e33474ef0c4b2d1d85ebbe3289063276371a54b7e36f3ec8051197536e2b9` |
| `overwrit-grading-04-return.png` | `3aaa7a78780d5cb9a4ce914f2f446cb53b18340fec0b49f40951cea5d1b6ec44` |
| `overwrit-grading-05-fresh-return-continue.png` | `3aaa7a78780d5cb9a4ce914f2f446cb53b18340fec0b49f40951cea5d1b6ec44` |

Replay JSON SHA256 at review:
`40ee7f29fabc1c6bd915d865aa2ba74768517c26a8ef894f4911519b20ccf896`.

This is an Editor evidence/source review, not a packaged-player review,
source-to-binary attestation, Linux execution, physical Steam Deck test,
performance certification or visual acceptance of every biome and lighting
condition. No palette constants or source pixels were changed for this review.
