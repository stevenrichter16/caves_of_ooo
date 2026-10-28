# Environment, shading and composition — packaged delivery

Runtime source `7f73b6a784e3c8016c057ff77177e130128e13d7` was frozen and verified on origin/main before building. This delivers the reviewed Sodden, Stump, Grovelands, Beating, Sill and Overwrit environmental passes, Sill continuity/contact/wall shading and fresh SummitScrub shelter composition. No new runtime or artwork change was made in this delivery pass. Prior `43a151ddb` builds and archive remain intact.

## Build and archive evidence

Both builds use ordinary `Assets/Scenes/Main/SampleScene.unity` / GameBootstrap, with development disabled. Mac job `build-b1481dd08c` succeeded in 95.813426 seconds; Linux job `build-d4089926df` in 85.427828 seconds. Each reported 0 errors and 4 warnings. The full source/save/build snapshot finished at 21:16:52.910533 UTC, before either accepted build began.

- Mac: `Builds/Verification/2026-10-28-environment-batch-7f73b6a78-current/CavesOfOoo.app`.
- Linux: `Builds/SteamDeck/2026-10-28-environment-batch-7f73b6a78-current/CavesOfOoo-current`.
- Archive: sibling `CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`, 128,841,112 bytes; SHA256 `135131319d80e56bb887021672e3433dda335b3e01f0525cfa70aea3ca2d648a`.

The bounded package helper directly reuses the pinned archive helpers. All 190 regular files, 13 directories, modes, checksums and gzip footer verify. The raw Linux `CavesOfOoo` remains unchanged; an exclusive byte-identical 0755 `CavesOfOoo.x86_64` copy satisfies the established launcher. Existing controller guidance, audio credits and licenses are included. Linux and physical Steam Deck were not executed.

Retained native checkpoints are separate: SummitScrub 213/213 passed; wall shading 224 passed and one existing unreliable allocation-counter skip. They are not added into an invented all-source test total. This delivery reran no native tests because it changed no runtime/art. Earlier material and contact-pilot evidence remains in each linked feature record. Build provenance is bounded by the frozen source, snapshot and job/output receipts, not a reproducible-binary claim.

## Actual standalone Mac play

Nine unretouched 1280×856 window captures include the 56-pixel title bar above 1280×800 gameplay. Normal keyboard actions only; no owner creation, teleport, health edit or hidden-state disclosure.

| Process | Earned input | Observed actions and result |
| --- | --- | --- |
| 36952 | wall-shadow-return187 | Continue Sill at39,11, HP40; Up3/Left3 to36,8; c/Up/x stone-wall reader (Stone38/40); Return/Right; ordinary worldmap travel to glade11.10 and back; F5 turn1450, HP40 |
| 37214 | first process's real save | Fresh Continue Sill at39,11, HP40; capture05; normal quit |
| 37350 | summit-scrub-return188 | Continue43,8, HP40; c/Up/x Bush reader (Terrain/Plant6/6); Return/Up/Down; real sari-snake attack reduced HP40→33; F5 turn1650 |
| 37610 | third process's real save | Fresh Continue43,8, HP33; capture09; normal quit |

The Summit Up/Down sequence returned to43,8; there is no intermediate packaged screenshot on43,7. The earlier Editor traversal evidence remains the direct on-cell observation. Do not claim a new full-chunk traversal or all three pockets observed in these central packaged views.

Sill04→05 differs by 1,121/564,000 world pixels in rectangle(0,96)-(940,696), localized within crop bounds(224,224)-(513,336); cause remains unproven. Summit08→09 differs by 0/564,000 world pixels. UI logs are outside that comparison. Immutable earned Quick pairs are `Fixtures/environment-sill-return189` and `Fixtures/environment-summit-return190`, with exact hashes/provenance recorded.

Input/tool correction: observing the app after Command-Q caused CUA to launch a second process automatically. The requested manual relaunch correctly refused while PID37214 existed. That actual process became the recorded Sill restart; its log was copied from the real `~/Library/Logs/DefaultCompany/caves-of-ooo/Player.log`. Later quits deliberately used shell process inspection without reopening the app. A mistyped source SHA in the new Linux receipt was corrected against frozen HEAD before package preflight; it never entered the accepted archive. No build or source revision was changed by either correction.

The four actual logs retain respectively 7/6/6/6 premature-thread-finalization notices and one ComputeBuffer-disposal notice each. No exception or `Error` substring appeared, but this is not a clean-log or sustained-performance claim. Build warnings are retained as reported; no physical-device/FPS claim is made.

## Review, preservation and next work

Independent inspection of all nine actual captures found no new actionable P1/P2 within the bounded views. Attached wall-foot shade, material hierarchy, travel and readers remain readable. The player contact shade is too subtle here to re-prove its isolated geometry; reuse the earlier matched pilot evidence. Broad empty ground, flat dome caps, repeated scrub stems, simple river edges and mixed glyph props remain open. This package does not advance any full-area polish scope or claim every included region was played standalone.

Protection snapshot `/private/tmp/coo-environment-batch-build-protection-7f73b6a78` covered 54,137 tracked objects, 49 unrelated modified paths, normal saves/preferences and prior packages. Explicit reviewed restoration backed up and restored ten whitespace-only sprite metadata rewrites and four generated settings. At 21:26:00 UTC all tracked snapshot objects, normal Editor state, normal standalone preferences and prior packages matched. The originally absent standalone save root is absent again; both owned branches are archived outside the repo. Zero cache/drive deletion; 387,832,082,432 bytes free at restoration. The idle Editor subsequently reapplied the same ten whitespace-only metadata changes. It was closed with normal Command-Q; at21:30:50 UTC those exact files were backed up/restored again and only the49 protected unrelated paths remained unstaged. See the post-exit preservation receipt; Unity is closed for the next session.

Next candidate: an ordinary Spread road/hedge approach outside the six opening graphs. The ledger records only opening-anchor integration there, whereas the six regional material passes now have bounded evidence. Start with an earned normal-travel baseline and real Grass/RoadStone/parcel-edge owner audit; no nonopening Spread defect, checkpoint or acceptance is invented. Use that comparison to choose one coherent environmental pass, with restrained shading and periodic meaningful population still considered. Crops and nuanced animation remain deferred.

Last actual hourly report remains 20:44:12.183742 UTC; next due21:44:12 UTC. This package milestone does not reset it. Exact delivery evidence publication is the next step; source7f73b6a78 is already remote.
