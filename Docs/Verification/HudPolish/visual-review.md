# HUD original-pixel review

All images below were opened at their original resolution. These findings come from pixel inspection, separately from the automated interaction results.

| Capture size | Native run | Files inspected | Finding |
| --- | --- | --- | --- |
| 1280 × 800 | `d5a77bbff6224351a5814ca23d298b7f` | `hud-01-original-ten-cards.png`, `hud-02-actual-pending.png`, `hud-03-real-cooldown-and-grouped-log.png`, `hud-04-original-full-details-reader.png`, `hud-05-native-older-history.png`, `hud-06-actual-look-focus.png`, `hud-07-load-restores-hud.png` | No visual blocker. |
| 1280 × 720 | `9c3380f3f8784f48ae0b0a99bbc5088f` | `hud-02-actual-pending.png`, `hud-03-real-cooldown-and-grouped-log.png`, `hud-04-original-full-details-reader.png`, `hud-05-native-older-history.png`, `hud-06-actual-look-focus.png`, `hud-07-load-restores-hud.png` | No visual blocker. |
| 1920 × 1080 | `203cb58428fb47f9bbeb302ce38db763` | `hud-01-original-ten-cards.png`, `hud-02-actual-pending.png`, `hud-03-real-cooldown-and-grouped-log.png`, `hud-04-original-full-details-reader.png`, `hud-05-native-older-history.png`, `hud-06-actual-look-focus.png`, `hud-07-load-restores-hud.png` | No visual blocker. Thin background seams remain cosmetic. |

Files are under `Native/<run ID>/` beside this review.

At all three sizes, all ten card frames and six distinct icons/names are visible. Selection, pending/AIM and the real cooldown remain distinct; names do not collide with state text or borders. Stored turn fences appear as single quiet teal rules, and Character punctuation displays as a dash rather than `?`. The full Flaming Hands mechanics/range reader and older-history controls are visible and unclipped. Look changes focus to the actual adjacent Grass cell. Loading visibly restores the frames, icons, selected state, cooldown and log. The older full-details font is denser than the new HUD at 720p but remains readable in the capture.

The successful 1080p run visibly reaches older history, shows the full mechanics reader and adjacent-cell focus, and restores the HUD after loading. Its sidebar background has faint one-pixel vertical seams. They do not obscure content; this remains a cosmetic limitation, not a reason for another layout redesign. The earlier incomplete run `6c4aba2084ff4aee943a8f517bc34e00` had only pending/cooldown/details inspected; it is retained as failure evidence and does not substitute for this completed route. The subsequent diagnostic `9471d5cb83714f598feed3205213c561` established the stationary-pointer controller bug before its fix.

## Final-source confirmation

After the stationary-pointer and thoughts-mode idle-cache corrections, the coordinator reported all 3,223 final-source tests passing. Each fresh native route below completed 19/19 checks with zero failures and zero unexpected errors. I independently read their reports and opened the two named PNGs at original resolution; the test counts are separate from the visual findings.

| Final capture size | Native run | Original files inspected | Finding |
| --- | --- | --- | --- |
| 1280 × 800 | `435f30fba2d14461aedc8ff47c052176` | `hud-03-real-cooldown-and-grouped-log.png`, `hud-07-load-restores-hud.png` | No visual blocker. |
| 1280 × 720 | `2f137a3b306a41098bff85b830f45b6b` | `hud-03-real-cooldown-and-grouped-log.png`, `hud-07-load-restores-hud.png` | No visual blocker. |
| 1920 × 1080 | `c461f3c50f9a4816b1acb7f6daa1962e` | `hud-03-real-cooldown-and-grouped-log.png`, `hud-07-load-restores-hud.png` | No visual blocker; faint sidebar background seams remain cosmetic. |

The final six images preserve all ten framed slots, six distinct icons and two-row names, the selected Ember Spit state, the separate red Ground Surge cooldown, readable vitals/focus and the grouped log. Loading leaves these elements visible and adds the actual “Game loaded.” entry. The homogeneous turn fences appear as single quiet rules rather than wrapped question marks. Character punctuation remains a dash. No new clipping or state/name overlap is visible in these captures. The 1080p seams do not obscure the text.

The broader pending, details-reader, older-history and Look findings above are from the explicitly listed earlier images; those images were not relabeled as final-source captures. The fresh final routes report those interaction checks passing, but this final visual confirmation inspected only files 03 and 07 as requested.

Limits: these are finite desktop-native captures, not a physical Steam Deck test or a claim about handheld comfort, every possible text length, long-session behavior or GPU/FPS improvement. Pixel inspection does not prove idle allocation or repaint performance; those remain separate measured gates.
