# Packaged Overwrit: independent Mac visual review

2026-10-27. **Bounded native macOS standalone pass.** No new significant visual
blocker was found in the recorded Continue, bench/shoot examination, local
movement, save and restart sample. The Overwrit field retains the corrected
neutral grading across the three comparable world views. This does not close
whole-region fullness, remaining sprites or Steam Deck acceptance.

The build receipt binds this Mac player to source
`00959edf0f7a154acf03fd8e521326099d652414` and
`Builds/Verification/2026-10-27-overwrit-materials-00959edf0-current/CavesOfOoo.app`.
Its recorded build succeeded with zero errors and four warnings. This reviewer
did not build or launch that application or independently inspect its binary;
the source binding is the retained build record's claim. Both saved player
logs independently name that exact app path and record a complete startup
and shutdown sequence.

## Directly inspected actual captures

All seven original CUA JPEGs are 1280×856, including the 56-pixel macOS titlebar
and 800-pixel game canvas. They were viewed at original resolution. No image
was repainted, resized or replaced for this review.

| Capture | Visible result |
| --- | --- |
| `packaged-overwrit-materials-00-startup.jpg` | Ordinary startup displays the existing opening scene and a save-detected Continue prompt. This is the temporary startup presentation, not the loaded Overwrit state. |
| `packaged-overwrit-materials-01-continue.jpg` | The earned character is at 75,18 with HP 38/40, native skill icons, the neutral Overwrit field and nearby native bench/shoots. No green startup-palette cast remains. |
| `packaged-overwrit-materials-02-bench-reader.jpg` | The native wood portrait accompanies “inward-facing bench,” Object, Wood and Structure 11/12. |
| `packaged-overwrit-materials-03-shoot-reader.jpg` | The character is one cell west at 74,18. The native clipped-shoot portrait accompanies “short new growth,” the same-height-tip description, Terrain, Plant and Structure 4/4. It does not imply a crop, harvest or service. |
| `packaged-overwrit-materials-04-return-saved.jpg` | The character is back at 75,18 with HP 38/40. The world remains neutral and the log visibly reports a save. |
| `packaged-overwrit-materials-05-restart-continue.jpg` | Following the root's recorded full quit, relaunch and Continue, the world, location, HP and grading remain consistent. |
| `packaged-overwrit-materials-06-restart-bench-reader.jpg` | The same native wood bench portrait and Structure 11/12 remain after the restart. |

The root's input sequence is recorded in
`packaged-overwrit-materials-mac-play-summary.json`: Continue the earned 6230
fixture, examine the bench, move left and examine a shoot, return right, F5,
CmdQ, relaunch, Continue and examine the bench again. The reviewer verified
that retained input/output `Quick.json` headers use the same game ID and
ordinary 4,12 zone, with turns 6230→6250 and HP 38/40. These header reads support
the saved-state record; they are not a full save-stream audit or proof from
screenshots alone of which keys were pressed. No injection or teleport is
reported by the root; this reviewer did not operate the game.

## Color continuity and evidence checks

The decoded RGB rectangle `[0,96,940,696]` in captures 01, 04 and 05 excludes the
titlebar and uses the same 940×600 world bounds as the Editor comparison.
All **564,000 pixels / 1,692,000 RGB channels match exactly** between these
three JPEG decodes. SHA256 for each rectangle:

`975b642dee150bc5ace48a1ed5201be7adb03355582db2b2731578b10599b868`

The complete JPEG files have different hashes because surrounding UI/log
content differs. This is a decoded captured-image comparison, not a lossless
native sprite or renderer-buffer assertion. It does establish that the
observed packaged world colors did not drift across this sample.

All seven capture byte counts and SHA256 values match the play-summary
manifest. Both recorded log byte counts/hashes also match. Each log contains
**five** “may have been prematurely finalized” notices and **one**
ComputeBuffer-disposal notice. Neither contains an error/exception line.
The draft summary initially said six finalizer notices per process; the
reviewer reported the exact five count to its owner for correction. The logs
must not be described as warning-free, clean or proof of leak-free behavior.

## Acceptance limits

The two Margincress cells still have conspicuous black-backed fallback
graphics. The broader crop/stair coverage, naturally unobserved Waymarker,
second bench/Binderroot area and centered Pilgrimage composition/fullness
questions from the Editor review remain open. This short Mac sample does not
revisit every Overwrit formation or test other biomes, all lighting, combat,
performance, all controls, Linux execution or a physical Steam Deck.

The reviewer authored the original Overwrit source art, but did not implement
the grading fix, author the runtime adapter or capture this Mac run. This is
an independent review of delivered-player evidence, not independent authorship
of the sprite artwork. The Linux archive and its manifests are outside this
visual review. No tests, Unity/Git actions, build mutations, save changes or
source-art edits were performed; this new review document is the only write.
