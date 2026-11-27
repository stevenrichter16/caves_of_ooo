# Current Mac package with environmental opportunity cues

The universal app at
`/Users/steven/coo-verification/builds/mac-0ab67c515-20261126/CavesOfOoo.app`
contains published runtime `0ab67c5157dcec790c9d5a1093ce5c80f19e998e`.
It includes the rain-clay seam, knotflax, North lichen, public catch-cloth
and early pitchpod cues, plus the completed loose-item drying repaint.

Unity reported success in 26.65 seconds, with zero errors and the same
five warning categories as the prior package. All 32,252 committed build
inputs matched the published Git tree before compilation. Unity changed
84 import or settings files inside the isolated clone. The runtime C#
sources stayed exact. Both new and previous apps passed 186 file hashes.
All six Mach-O binaries contain Apple Silicon and Intel architectures.

The new app completed GameBootstrap on Apple M5 with 2,697 entities.
This was a startup check without gameplay input. All 876 Quick files across the Editor and player save roots, including
archived save copies, matched afterward, and the nine changed runtime preferences
were logically restored. The app was then stopped. Packaged gameplay,
physical Intel execution and the whole-process regression remain open.

The first startup only checked 860 files under the Editor root. A second
startup freshly checked both roots. No fresh preservation claim is made
for player-root files during the initial limited check.

[Build and startup receipt](startup-verification.json)
