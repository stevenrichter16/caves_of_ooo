# Release stabilization — R1

Status: started 17 September 2026, after all accumulated playable changes reached local `main` at `26fbe544`. The user authorized starting the release plan without intervention. Unity stays open; implementation and tests use the independent verification clone. No gameplay reset or debug-state change in the live editor.

## Goal

Resolve the 32 inherited RS28 failures by fixing actual missing content and correcting only demonstrably stale assumptions. Preserve meaningful assertions. Then verify the ordinary opening without debug invincibility; separate mechanical completion from feel/balance. This is release phase one, not a promise that the campaign is ready to ship.

## Verification sweep and existing RED evidence

RS28: 14,901 tests, 14,869 pass, 32 fail, zero compiler errors. Exact failures are already recorded before any R1 production edits.

| Failure family | Count | Verified premise / intended response |
|---|---:|---|
| Equipment ground sprites | 29 | Existing adversarial fixture expects seven distinct 16×16 sprites and exact ten-blueprint routes. PNGs/routes are missing; fulfill the art/mapping contract, retaining glyph fallback, tint, visibility, dirty-cell and claim controls. |
| Building-block variants | 1 | Six family/variant pairs still share geometry/paint. Compare authoring output to installed FBX, then repair the generator/import publication; do not hand-edit one baked mesh or weaken uniqueness. |
| Place profile table | 1 | Morrowfast is now an explicit authored profile. Replace the obsolete plain-village expectation with the actual authored-profile contract and neighboring controls. |
| Sill/start position | 1 | SampleScene explicitly starts at Overworld.2.6.0; bootstrap reads its configured field, not Places[0]. Sill's address/biome/tier contract stays; test those without imposing the obsolete table-order spawn premise. |

## Gates

1. Preserve source/art before editing. Work from the exact committed state and record corrections above before fixes.
2. Reuse the existing failing assertions as the RED baseline; add paired tests for any newly identified branch. Fix each group independently.
3. Keep pixel sprites binary-alpha, shared outline (30,32,28), full 16×16 frames and copied importer metadata with unique GUIDs. Check silhouettes and inspect the resulting art.
4. Rebuild block models from their generator and repeat-build/hash-check where relevant. Validate real imported meshes, not filenames or FBX metadata.
5. Run focused fixtures, independent Q1–Q4/countercheck review and a fresh full suite. No blanket exemptions.
6. Publish exact verified files, commit on main, keep the current Unity session intact. Record actual normal-player native acceptance and remaining limitations.

## Scope and performance

Art is produced offline; no extra per-frame scans or runtime cube generation. Existing renderer logic is changed only if tests establish a real routing defect. No new lore, campaign, world simulation or save migration in this first repair wave. The following roadmap remains `RELEASE-VISION.md`.

## Implementation log

- Main integration preserves all accumulated runtime dependencies; 12,979 Assets/Packages files match the validated clone. Main remains local; no remote push requested.
- R1 opened against exact RS28 red cases; source audits run before edits. The original full-suite failures remain visible until genuinely resolved.

### Handoff checkpoint — 17 September, 22:05 CDT

- Candidate work remains in `/tmp/coo-regional-verification-20260917`, independent of the user's open Editor. The reviewable 99-file draft is preserved in [ReleaseHandoff](ReleaseHandoff/README.md); it has not been installed into main Assets.
- R101: 79 cases, 72 passed/seven reachable missing-resource failures, zero C# errors. New sprites/preload/aliases were present, while the drafted guard was deliberately excluded so the fallback tests reached the real branch. Restoring the guard followed this run. Both corrected world fixtures passed.
- R102: regenerated 72-piece source kit imported through the existing asset builder; Unity exit 0, zero C# errors, no metadata changes. Fifteen offline contract tests passed.
- **R103: 14,901/14,901 full-suite cases passed, zero failed/skipped/inconclusive, zero C# errors, Unity exit 0.** All 32 inherited failures are resolved in the isolated candidate. [Receipt](Verification/VoxelWorld/R103-release-full-candidate/receipt.json); compressed XML/log are adjacent. This does not change the accepted main runtime's RS28 result.
- The candidate native journey adds observations requiring a vulnerable start and completion of the cloth expedition plus five interiors alive without F12 or synthetic healing. These compiled but have not run natively yet. Later regional F12 use remains explicitly separate.
- Seven ground sprites passed static/reproduction checks and contact-sheet inspection. Dedicated ground-sprite native idle/walk/pickup-drop A/B, the updated native opening, imported-block visual review and final cold-eye/adversarial acceptance remain outstanding. The existing combat-equipment bench is not the missing sprite bench.
- Current request is a comprehensive game-state document and next-agent prompt. Those are provided in [GAME-STATE-2026-09-17](GAME-STATE-2026-09-17.md) and [RELEASE-AGENT-PROMPT](RELEASE-AGENT-PROMPT.md), with exact candidate status preserved rather than declaring R1 complete.

### Release-candidate branch checkpoint — 18 September, 03:28 UTC

- Created Git branch `release-candidate` from `main` (`41820270`) and applied the preserved 99-file R1 candidate from `Docs/ReleaseHandoff/R1-candidate.zip`. Every base file matched its manifest `before` hash (83 modified, 16 new, 0 conflicts); every applied file matches its `candidate` hash. Nothing was staged by wildcard; the two UnityMCP log files and the ~4,300 untracked report/cache files remain uncommitted.
- Independent re-checks on this branch: seven sprite GUIDs unique across all Assets metas; sprites are 16×16 RGBA, binary alpha, ink outline present; `generate.py` reproduces pixel-identical sprites on a second host; 15/15 building-kit contract tests pass; `WorldMapAuthoring.PlaceAt` and `DebugInvincibility.IsEnabled` exist so the corrected fixtures and opening checks compile against this tree.
- Unity was not launched from this checkpoint (the user's Editor stays untouched). No fresh EditMode run, no native journey, no sprite A/B bench and no imported-block visual review were performed here; the R103 receipt remains the latest executed suite and it ran in the clone, not against this branch's checkout.
- `main` is unchanged. Promotion path: run the outstanding gates against `release-candidate`, then fast-forward or merge into `main`.

- **Countercheck, 18 September 03:38 UTC:** a third concurrent firing of the same scheduled task (at least three fired) independently verified the branch (99/99 candidate hashes at tip, commit composition exact, `main` untouched) and stood down rather than re-applying. Push still pending manual `git push -u origin release-candidate`. See [RELEASE-CANDIDATE-2026-09-18T033811Z-countercheck](RELEASE-CANDIDATE-2026-09-18T033811Z-countercheck.md).

- **Hourly-firing diagnosis and guard, 18 September 03:46 UTC:** a fourth unattended run found the trigger is a single hourly scheduled task (`39 * * * *`, prompt replaced 03:23 UTC) rather than a duplicate entry, so the same prompt re-fires every hour until edited. It observed a previous run still amending the tip after it began, waited for a ten-minute reflog quiet window, re-verified 99/99 candidate hashes, and added the read-only guard `Tools/Release/release_candidate_status.sh` (exit 2 = another run active; exit 3 = branch/manifest inconsistency). Push still pending manual `git push -u origin release-candidate`. See [RELEASE-CANDIDATE-2026-09-18T034616Z-hourly-firing-and-guard](RELEASE-CANDIDATE-2026-09-18T034616Z-hourly-firing-and-guard.md).

### R1 acceptance — 18 September 2026, local main promotion

R1 is accepted with the exact evidence and limits in [RELEASE-R1-ACCEPTANCE](RELEASE-R1-ACCEPTANCE.md). R104 ordinary vulnerable opening passes58/58, ending31HP. R106e/R107b ground sprite native comparison passes67/67 each with reviewed normal/dim art; all72 installed blocks match regenerated variants. R108 full:14,917/14,918 (one documented nanosecond ceiling flake), zero compiler errors; isolated whole-fixture retry plus harness controls20/20. Earlier R103 full candidate14,901/14,901. All32 original failures were repaired without weakening their assertions. Main promotion includes the99-file repair plus final verification infrastructure; later dated checkpoints supersede earlier pending statements. Next: [R2 regional outcomes](RELEASE-REGIONAL-OUTCOMES.md).

### R4 acceptance — 19 September 2026, material guidance, local main promotion

Fire clay, silver sand and ward oil now explain their real use from the ordinary inventory Examine action: what one measure repairs, which resident to speak to, the required guide by its in-game name and whether it is carried, and that the material is spent while the guide is kept. Fire clay also names Morrowfast's quiet-bell use and its free alternative. Every sentence was traced to the consumer that performs it; see [coverage review](Verification/MaterialGuidance/coverage-review.md). R124 RED 18 cases (7 intended failures); R127 focused plus a new 20-case adversarial file 64/64; **R129 native 81/81 checks over 1,010 native steps**, examining earned fire clay then spending it on the quiet bell with repeat-cost and F5/F6 controls; **R130 full 15,010/15,010**, zero compiler errors. Two defects were fixed before publication: the handed-off clone did not compile (a native harness call to a never-written method), and the native validator pinned exactly 23 captures. The 10-file candidate is recorded in its [manifest](Verification/MaterialGuidance/candidate-manifest.json). User Unity stayed open; execution used only the independent clone. The guard's manifest check now reports exit 3 on every run because three R1-manifest files were legitimately changed by later accepted slices (R2, fullscreen UI, R3); that is a stale reference, not an inconsistency. Next: see [RELEASE-MATERIAL-GUIDANCE-PLAN](RELEASE-MATERIAL-GUIDANCE-PLAN.md) §Publication.

### R5 acceptance — 19 September 2026, the Stillleaf Archive chain, local main promotion

The first complete middle-game chain now exists and is ordinarily obtainable: the Recension Searcher at Quillhold (the errand and the keeper's last words), the Pale Curation Indexer at the Salt-Vault (the file found by those words; Curation's request agreed, bargained or refused; the key released from a locked salt file), the sealed library under Stillleaf opened by its real key on the ordinary bump, and one enacted custody — delivered, filed or resealed — with standing following what was promised and the other party told the truth for free. Six sub-milestones, each RED→GREEN in the independent clone and committed to `main` separately (SA.1 `e05ddb63` … SA.5 `b2ec092f` and SA.6 (this promotion)); see [MIDGAME-STILLLEAF-ARCHIVE](MIDGAME-STILLLEAF-ARCHIVE.md) for every gate, divergence and self-review. The dedicated adversarial sweep (23 tests, stub-phase RED) found and fixed four gaps: a journal stuck when the errand was accepted after the work, a dangling loss, free theft of the salt file, and the keeper's slate not giving its words to a thief. **Full EditMode 15,109/15,109**, zero compiler errors; **native journey 102/102 checks over 1,414 native steps** with 33 captures inspected, covering the delivery outcome with agreed terms; filing, resealing, refusal, theft, loss and out-of-order arrival are EditMode-covered only. F12 was declared for the sinkhole descent alone. No ending spine, god contact or epilogue is implied; roadmap step 3 is met for one chain.
