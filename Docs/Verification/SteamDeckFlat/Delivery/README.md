# Deck-size UI and controller delivery

Published source `16afcc497` is built for Mac and Linux. Both builds completed with zero errors and four warnings. The archive is 162,168,409 bytes, SHA256 `ba9dfeee376856bc669e0e4cc4d942852eb2455e42b7b50a80a53924fb9c8ab8`. Independent review verified full gzip EOF, 320 members, 306 internal checksums, executable permissions and 170 source/evidence pins. The archive includes the source-time Deck readiness document; this delivery record supplies subsequent package status.

Current flat Editor walkthrough: 46 checks and 14 actual 1280×800 captures. Native tests: 198 input/framing/backend and 83 final UI/affordance checks, overlapping and nonadditive. Four footer RED failures and one shrinking-menu RED were corrected. Review-driven corrections cover interrupted boot-device cleanup, neutral-stick A wording, and old foreground clearing when abilities shrink. No remaining scoped source or visual P1/P2.

Mac smoke used the exact new executable with `-screen-width 1280 -screen-height 800 -screen-fullscreen 0`. Actual screenshots are 1280×856 including the 56-pixel title bar, leaving the required 1280×800 game area. Continue, readable Graphics/Abilities menus, map49,10 roundtrip, movement39,12HP24, zoom in/out, F5turn2860 and fresh Continue39,12HP24Height were observed. Saved/reloaded world rectangle `(0,56)-(940,736)` differs at950/639200 pixels, localized to relative `(879,192)-(929,249)`; cause remains unproven. No exact pixel-match claim. First launch overlapped a build, so this is not a benchmark.

Preservation completed2026-11-02T22:11:17.791892Z: all57,481 tracked snapshot objects,77 unrelated dirty paths, normal saves/preferences and prior4fa8 packages preserved. Four generated settings files restored with exact reviewed hashes; five introduced standalone preferences removed by the guarded restorer. Owned test saves archived. Editor and players closed. No cache/data cleanup.

Linux execution, physical Deck controls, Steam Input mapping, handheld comfort, sustained performance and suspend/resume remain unverified. Next graphics work is a fresh Sumphold15.6 scene comparison; broad area polish, population/shadows and R11 remain open.

Raw build receipts and player logs are preserved byte-for-byte. Their extra final blank lines and Unity connection-line whitespace are retained; the whitespace check is clean for the remaining owned files.
