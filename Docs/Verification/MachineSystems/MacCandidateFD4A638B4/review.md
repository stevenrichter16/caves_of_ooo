# Independent bounded Mac artifact review

**Final verdict: zero P1/P2 findings or publication blockers within the stated
build/boot-only scope.** This review does not qualify packaged Inventory input,
movement, actions, controller use or save/reload, and does not accept a new
delivery, full redesign or complete machine.

## Source and package checks

- Independently streamed input.tar and matched all **32,003 source hashes**.
  Reconstructed Git blob/tree identities, including file modes, from that
  archive and compared the four root trees with committed object
  `fd4a638b457bf9f7f44401fe4ba1670705df514f`. Assets, Packages, ProjectSettings and
  ArtSource match. This used read-only object hashing, not Git mutations.
- Independently recalculated input-deltas.json: **83 changed, zero missing,
  three added**, with no changed runtime C# source. The 86-file exact overlay
  matches those changes/additions and reconstructs all **32,006 effective inputs**.
  Every effective file hash and the complete four-root file inventory match the
  dedicated build copy, with no extra or missing files.
- Independently matched all **592 external local MCP files** and **186 current
  app files**, including complete file inventories. Rehashed the prior74,477,f8
  and7c3 packages against their earlier pins: 186 files each, no changed, extra
  or missing files. Current and prior7c3 managed assembly hashes match
  assembly-identity.json and differ; this proves binary identity, not gameplay.
- Reviewed the guarded Editor-only build helper: dedicated project and batch
  checks, refusal of an existing output, SampleScene, StandaloneOSX,
  DetailedBuildReport and universal architecture. Development, IncludeTests and
  AutoRun flags are absent. The report records success in63.424851s,
  364087837bytes, zero errors and five disclosed existing warnings. Mach-O
  inspection independently confirms both x86_64 and arm64.
- Compressed durable pins/build log, preparation, report, verification, delta
  manifest/overlay, helper and preservation/prior-package receipts match their
  private export counterparts byte-for-byte. Durable boot image, player log,
  process/window and restoration receipts likewise match the private boot bundle.

## Boot and restoration checks

Viewed the actual boot.png: it shows the character chooser over the world and
HUD. Player logs identify Unity6000.3.4f1, seven registered creatures,2679 entities
at generation and2680 final including the player. Process receipt43735 names the
exact candidate executable; its independently recalculated hash matches. That
PID is no longer present. Window receipt70668 belongs to that PID and records
native input preflight false. No packaged gameplay or inventory operation is
inferred from the chooser image or the different managed assembly.

Reviewed the restoration helper at
`/private/tmp/coo-mac-fd4a638b4-boot-check.py`; it differs from the previously
reviewed7c3 helper only in the candidate tag. Its guards target only the owned
player, protect an originally absent normal Saves path, restore original
preferences, delete only new Screenmanager keys and require fresh logical
equality. Independently exported current preferences read-only and compared the
complete ten-key logical result with prefs-before.plist: **equal**. Independently
confirmed normal standalone Saves is absent and not a symlink. There is no
byte-exact plist or whole-directory restoration claim.

## Limits

The84 foreign tracked/861 selected normal-file preservation and main Editor
stopped/not-compiling/root-null/profiler-false/view-owned-false status are owner
receipts reviewed here, not independently repeated Unity operations or a fresh
rehash by this reviewer. Warm Library reuse and generated/editor input changes
are disclosed; the effective tree is not described as byte-exact committed input.
Read-only artifact inspection cannot establish that no external actor ever posted
input; the recorded owned procedure and preflight support the explicitly limited
boot-only account.

No Unity/app launch, native UI operation, test run, process change or Git mutation
was performed during this review. This review.md is the only file written.
Packaged controls/gameplay, full redesign, performance/FPS, hardware/controller,
Linux/Deck execution and all twelve complete-machine gates remain open or
deferred as recorded in README.md. Final package checksums may be generated after
this review; no checksum manifest was claimed verified before it existed.
