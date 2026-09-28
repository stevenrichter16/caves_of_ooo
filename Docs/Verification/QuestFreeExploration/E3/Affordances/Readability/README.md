# Nearby action cue: native pixel/readability closure

Status: width-only correction adopted; actual Unity 81/81 passed (cue60 + collector carry19 + restoration2) in `E3/Integration/native-cue-pixel-carry-green.json`. No general display-support or unprompted human-awareness claim is made. The final default driver retains free Normal screenshots and removes all temporary width/color/force-render mutations.

## Finding and bounded evidence

The initial actual route `ff08f22a64284b28ad146b22f1c244dc` completed 8 checks, zero failures, one paid Harvest. Real hint/menu/stubble were visible, but the white Look rectangle prevented isolating the quiet corner cue. Normal-state follow-up `90de5af9a28b4b30b282f8e7490ef349` completed 10 checks, zero failures, one paid Harvest; root and independent render reviewer could not distinguish the muted short corners reliably.

The actual same-camera diagnostic `a00c749fc3c84b26a95c78e652bd8907` completed 11 checks, zero failures, one paid Harvest and restored exact owned settings. Its recorded 1920×1080 frame has a 1461×918 map viewport and orthographic size16.8. The actual cue at(32,7) projected to a .95625-pixel stroke. Original and restored exact corner boxes contain 10+4 cue-gray pixels, forced-off0+0, and .08-width same-color23+10. Full ROI changed-pixel counts21/60 also include minor ambient variation and are not used as exclusive cue counts. The marker was drawn; this was a readability/coverage issue, not a missing layer or owner query.

The source registration matches the working cursor: same grid/tilemap/world layer, Sprites/Default and sorting7 over native composite3. The only production change is stroke width .035→.08 (about2.186 pixels at the measured viewport). Muted color/alpha, two short corners, source selection, camera, palette, gameplay and menu input remain unchanged. White was a diagnostic positive control only.

Two owned-preview GPU cases at that measured viewport/zoom and two fractional offsets failed actual native RED at11/10 lit lower-corner pixels against a minimum16; paired Clear produced0. The width correction then passed both cases within81. This is an isolated black-background pixel-area regression, not a terrain-contrast or all-resolution guarantee. The former arbitrary <=.05 UI cap was openly changed to a broad <=.1 restraint cap; the new rendered test owns the meaningful minimum. No tests were silently loosened to claim pixel visibility.

## Review and boundaries

Q1: current query/owner/visibility, menu hiding and spent-source removal retain the existing symmetric paths. Actual Normal hint then no hint after Harvest were observed; no stale Harvest alias is accepted.

Q2: only the existing owned LineRenderer width changes. No saved fields, resource material edits, camera changes, new icons or new action execution path. The reversible diagnostic restores width/color/force flags in finally and was removed from the default acceptance driver afterward.

Q3: actual evidence is retained at the native paths above and `E3/Integration/native-collector-green-cue-pixel-red.json` then `native-cue-pixel-carry-green.json`. Runtime and full test-reference compilation had0 errors. Root/standalone bounded reviews found no source-registration, diagnostic-cleanup or GPU-fixture blocker. Standalone also reread the final width-only/safe-view delta and found no new concrete blocker: the same cells/predicates/source limits remain, HP/source changes still fail, and all temporary cue mutations are removed.

Q4: root and render reviewer independently read the original and widened same-camera images. The wider muted strokes are distinguishable; perceived salience, general readability, other display sizes and ordinary player discovery are unmeasured. These runs disclose an original-player setup transfer to an actual generated row and one real paid Harvest, not ordinary acquisition/travel. No full-suite result is inferred from81 focused cases.


## Final production capture

`Native/3bd66ab8d6c64883a57a4ec964bd715d` completed10 checks, zero failures and one paid Harvest in3.3622152s using the final production width and clean observer. Root and render_completion independently viewed `00b-normal-adjacent-current-cue.png` and `04-normal-after-harvest-current-cue.png`: muted corner/hint are present for the actual ripe row; after the real Harvest the hint/corner are gone and stubble/harvest log are visible. No diagnostic rendering overrides occur in this run. This closes the tested scene's pixel gate without claiming broader display/readability support. Root's full native run was still active at this document checkpoint; no full-suite result is claimed.
