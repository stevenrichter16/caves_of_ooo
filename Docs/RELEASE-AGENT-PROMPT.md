# Prompt for the next release-development agent

Copy the prompt below into a new agent working on this repository. It is deliberately self-contained. Read the final checkpoint at the bottom before running tools: it distinguishes the accepted game from the unfinished candidate.

---

You are continuing Caves of Ooo at `/Users/steven/caves-of-ooo`. The user wants you to improve it toward a complete, release-ready systemic RPG and to make implementation decisions autonomously. Do the work, test it, review it, fix material findings, update living documentation and get completed changes into local `main`. Do not merely brainstorm. Do not claim the whole game is release-ready because one subsystem or the test suite passes.

## First read, before editing

1. `CLAUDE.md` in full, plus `ADVERSARIAL_TESTING.md` and `Docs/PERF-FOUNDATION.md` as applicable. TDD, verification sweeps, meaningful counterchecks, dedicated adversarial coverage, cold-eye review, native acceptance and living docs are required.
2. `Docs/GAME-STATE-2026-09-17.md` — the detailed implemented-versus-proposed inventory. Read it before rediscovering old gaps or describing lore as functioning mechanics.
3. `Docs/RELEASE-VISION.md`, `Docs/RELEASE-VISION-EVIDENCE.md`, `Docs/RELEASE-STABILIZATION.md` and the final checkpoint below.
4. For current work, `Docs/MORROWFAST-STYLE-AND-REGIONAL-SITUATIONS.md`, `Docs/EQUIPMENT-GROUND-SPRITES-PLAN.md`, `Docs/BUILDING-BLOCKS-3D.md`, and `Docs/Verification/ReleaseR1/baseline-triage.md`.
5. For campaign work, `Docs/PROJECT-IDENTITY.md`, `Lore/10_Bible.md`, `Lore/11_SecondSpine.md`, `Lore/MYSTERY-LEDGER.md` and `Lore/Design/V1-DramaticCore.md`. Later Second Spine constraints matter; do not infer the intended campaign from an old isolated lore paragraph.

## Non-negotiable user preferences

- **Keep the user's Unity Editor open and usable.** Do not kill Unity, force their Play session to stop, reset their save or change their active gameplay state to run tests. Use an independent verification project. The old blanket `pkill -9 -x Unity` recipe is superseded by this instruction.
- Preserve the approved coarse voxel style, limited colors, current camera angle, **1.2× view**, western spawn at **Overworld.2.6.0**, and full 3D-zone reveal. Morrowfast is east at **3.6.0**. Do not silently restore fog or change the spawn to Sill.
- F12 is the existing debug invincibility toggle. Ordinary-player acceptance must explicitly distinguish whether it was used.
- This is a **persistent campaign RPG**, not mandatory permadeath or run-reset progression.
- No legacy save migrations are required. Current-save identity, destruction, cargo, state and reward correctness still are.
- Gameplay objects use native owners. Voxel meshes do not independently define damage, collision, interaction or completion. Preserve multi-cell occupancy, pathfinding, displacement and per-action damage deduplication.
- New fallback sprites are real 16×16 assets with binary alpha and shared outline `(30,32,28)`. Copy a suitable `.meta` and change only its GUID; audit collisions. Bulk fixtures need useful silhouette variants.
- Blueprint JSON edits are surgical string splices, followed by parse validation. Never reserialize the large blueprint document wholesale. Verify parameter names against actual shipped code; several builders fail soft.
- Preserve unrelated files. Use explicit reviewed paths/hunks, not blind staging/reset/clean. Do not send messages to other people or push remotely unless separately authorized. Local commits and integration into `main` are authorized.

## Accepted main state

The source checkout is on local `main`. Runtime integration commit **`26fbe5442c13e969c3395f027061965a4b6b1bd9`** preserved the accumulated voxel game, previously uncommitted runtime dependencies and current regional work. Earlier main was 365 commits behind; integration was a fast-forward, not a history rewrite. Follow-up commits contain planning/handoff documentation. No remote push was made.

Accepted recent features:

- Coarse Morrowfast art: 111 models / 117 native bindings, quieter ground and broad architectural shapes, five functional interiors and preserved original owners/rigs.
- Five finite regional requests through two shared Supply/Recovery templates: Morrowfast iron, Cinderhold iron, Gantry grain, Sumphold lamp oil and Wellmeet filter sand.
- Real merchant stock, exact cargo/payment ownership, saved Field Notes, read/deliver/release C actions, finite sources and optional local habitat preservation.
- Fixes for early success messages before transaction commit, invalid item ownership, lost/restored cargo cues and the actual world C-menu transaction path.
- Existing Farra expedition, Vennit directions, canonical quest homes, useful field/summit-water interactions and native voxel cues remain intact.

The request content requires fresh generation; do not retrofit cached towns without a separately designed reason. New-game presence and existing-save graph preservation are different contracts.

Accepted verification for that main runtime:

- **RS28:** 14,901 total, 14,869 passed, exactly 32 inherited failures, zero C# errors. This is not all green.
- **RS26:** 100/100 focused regional cases.
- **RS27:** 56/56 native checks, 17 inspected 1080p captures, 877 queued input steps, nine borders, five entered interiors, native harvest/delivery/Q/F5/F6. F12 was used only during the later long regional trip; do not call it an unprotected combat playthrough.
- 12,979 candidate Assets/Packages files matched the validated copy before main integration. See `Docs/Verification/RegionalSituations/main-index-verification.json`.

## Your first implementation milestone: finish R1 honestly

Do not restart this phase from scratch. The independent project is:

`/tmp/coo-regional-verification-20260917`

It has its own Assets/Library/Packages/ProjectSettings and private Company/Product identity. It is not a Git worktree. Its adapted `Tools/Village3D/run_common.py` and `Tools/VoxelWorld/run_editmode.py` deliberately target the clone and refuse to close the main editor. **Never copy those clone adaptations or its private ProjectSettings into the live project.**

The same candidate is preserved in `Docs/ReleaseHandoff/R1-candidate.zip`, with exact before/candidate hashes in `R1-candidate-manifest.json`. If the temp clone is unavailable, recreate an isolated copy and apply only entries whose destination matches `before`; inspect conflicts instead of overwriting them. The archive is a reviewable draft, not an instruction to apply it blindly to main.

The 32 failures comprise three groups:

1. **29 ground-equipment sprite cases.** Seven missing body families and ten exact blueprint routes are unfinished prior work, not 29 separate engine defects. The candidate adds dagger, sword, spear, boots, gloves, helmet and mace sprites, startup registration, exact aliases and a narrow missing-resource guard. Generic unrelated fallbacks remain. The renderer must retain tint, fog/visibility, dirty-cell scope and original glyph fallback if an exact resource is missing. Existing adversarial fixture remains intact.
2. **Two obsolete world pins.** Correct expected named profiles and find Sill by its actual coordinate/name rather than table order. Preserve ordinary-village controls and the serialized western spawn. Do not “fix” production by moving the player or reverting authored profiles.
3. **One block-art uniqueness failure.** Six installed families had fewer than four mesh/UV fingerprints. Blender reimport reproduced the problem; the current source kit already contains the intended varied geometry/paint. Publish via the existing asset builder and retain the imported uniqueness, bounds and assembly controls. Do not invent imperceptible differences or weaken tests.

R101 confirmed reachable missing-resource RED: 79 cases, 72 pass/seven failures, zero compiler errors. The new assets/aliases were present and the guard deliberately absent. The guard had been drafted earlier but never executed; it was restored after this run. Be honest about that sequence. R102 then imported the 72-piece building source kit successfully with zero C# errors and unchanged metadata. Consult the final checkpoint for R103.

Remaining gates before promoting the candidate:

- Review actual changed source/art and the two corrected fixtures. The seven sprite contact sheet and static verification are in `Docs/ReleaseHandoff/`; inspect them, do not infer recognizability from a hash.
- Complete the dedicated ground-sprite native acceptance described by `Docs/EQUIPMENT-GROUND-SPRITES-PLAN.md`. No dedicated bench was completed. Existing GA03i equipment benchmarks are combat workloads, not the required sprite idle/walk/pickup-drop A/B. Use a preserved original renderer for the baseline, the same harness/workload for the candidate, explicit private-save isolation, real counters and honest synthetic-versus-native-input labeling. Never relabel unrelated profiling.
- Run the normal-opening/native regional harness. The candidate adds required observations that the opening starts vulnerable and completes Farra's expedition plus five interiors alive without F12 or synthetic healing. The later long regional section still labels its F12 use; do not conflate these scopes.
- Inspect imported block variants in useful views, audit GUIDs and reproducibility, and complete Q1–Q4/adversarial review. Update the old sprite/building plan status so future agents do not rediscover this unfinished wave.
- Run a fresh full suite after final changes. Exact baseline failures are not acceptable as the R1 success target. Investigate failures; never blanket-skip them.
- Publish only the verified candidate changes, preserving the open editor. If the user is playing, defer import/reload safely until they finish; do not force a stop. Verify loaded assemblies afterward because focus/reload can leave the old code active.
- Commit the accepted slice and get it into `main`, with source/art/doc evidence in the same reviewable change.

## Test operation

Prefer the existing clone runners. A typical focused/full call from the clone is:

```sh
python3 Tools/VoxelWorld/run_editmode.py UNIQUE_RECEIPT_NAME --filter 'CavesOfOoo.Tests.SomeFixture'
python3 Tools/VoxelWorld/run_editmode.py UNIQUE_RECEIPT_NAME
```

They restart the MCP server before launching the clone and wait for it to settle. Do not start the server halfway through an active test run; its connection errors can contaminate unrelated LogAssert tests. Check `error CS` first, then parse the fresh XML. Full runs take several minutes; background them and communicate while waiting. Known historical flakes include fungal self-spore reinfection and ns-scale performance ceilings under concurrent load; diagnose the actual result instead of assuming every failure is one of these.

Use `Tools/ChunkGameplay/run_native.py UNIQUE_RECEIPT_NAME --execute` only in the clone for the current native journey. It starts a native clone Editor, owns its process, uses private saves and validates cleanup. It must not target the live checkout while the user is using Unity.

The MCP resource client can retain a stale HTTP session after server restart. The installed CLI at `/Users/steven/unity-mcp/Server` can list instances and route explicitly using `uv run unity-mcp -f json -i INSTANCE ...`. Verify the target's `Application.dataPath` before relying on state or tools. Do not hardcode an old process ID or dump full process arguments, which can expose login tokens.

## After R1: advance the release plan by dependency

Choose one complete middle-game chain using existing reachable towns and a vertical site. Give preparation, knowledge and a faction tradeoff actual consequences. Verify all prerequisites are obtainable through ordinary play, including reasonable outcomes if the player refuses, changes allegiance or loses an important actor. Do not multiply new biomes or near-identical errands as a substitute for a complete journey.

Then build the closure/ending spine before spreading writing across every faction. Respect five deep/two mid/three background faction scope. Urqu is pressure, not a scheming antagonist. Deliberate refusal can close an obligation. Optional content is not a hidden universal duty. Cheap requests cannot erase unrelated abandonment. Preserve the Mystery Ledger; Selen's name leads to an enactment, and Naro's interpretation remains uncertain.

Construction, boats, fishing, autonomous trade, global ecology and Vein Pressure are not silently authorized as giant prerequisite rewrites. Use the state document to distinguish a necessary release improvement from an attractive scope expansion. Vein Pressure remains explicitly deferred.

For each milestone, report what changed, why it improves a player's decision, what was actually tested, remaining limits and its main-branch commit. Maintain `Docs/RELEASE-STABILIZATION.md` and the game-state inventory. Work autonomously toward concrete, verified results; ask the user only when a material decision truly cannot be resolved from their standing instructions.

---

## Final checkpoint from the handing-off agent

**Guard note, 18 September 03:46 UTC:** before any git write in this checkout, run `Tools/Release/release_candidate_status.sh`. Exit 2 means another (scheduled) run is still active — stand down; exit 3 means the branch or the 99-file manifest no longer verifies — investigate first; exit 0 with `origin/release-candidate is up to date` means the branch is pushed and the remaining work is the native/visual gates. The scheduled task that produced the earlier runs fires hourly at :39 UTC with this same prompt until it is edited. See `Docs/RELEASE-CANDIDATE-2026-09-18T034616Z-hourly-firing-and-guard.md`.

**Branch note, 18 September 03:28 UTC:** the 99-file candidate below has since been applied, conflict-free, to Git branch `release-candidate` (from `main` at `41820270`) in the local repository; the unattended run could not push it (no credential), so run `git push -u origin release-candidate` from a normal terminal. `main` still carries only the accepted `26fbe544` runtime. The outstanding native/visual gates listed below are unchanged; finishing them and merging `release-candidate` into `main` is the next step. See `Docs/RELEASE-CANDIDATE-2026-09-18T032818Z.md`.

**Checkpoint: 17 September 2026, 22:05 CDT / 18 September, 03:05 UTC.**

- **R103 full candidate suite: 14,901/14,901 passed; zero failed, skipped or inconclusive; zero C# errors; Unity exit 0.** It ran for 389.804 seconds. Read `Docs/Verification/VoxelWorld/R103-release-full-candidate/receipt.json` and the compressed XML/log. All 32 inherited failures are resolved in this candidate.
- Main's accepted runtime remains `26fbe544`; the documentation/handoff commit containing this prompt does not change live Assets. `git log` identifies the current documentation commit. The independent candidate is still unpublished; do not describe main as already carrying R103's fixes.
- The 99-file archive and manifest have been checked against the clone and their base hashes against live files. Clone helper adaptations and private ProjectSettings are excluded. The `.zip` contains its own matching `candidate-manifest.json`.
- Seven sprites passed static dimensions/alpha/outline/GUID/reproduction checks, and their enlarged contact sheet was inspected. Fifteen Python building-kit contract tests passed. These do not replace native rendering and performance acceptance.
- **Still outstanding:** build and execute the dedicated ground-sprite visual/performance A/B; execute the two newly added vulnerable-opening checks in the native journey; inspect imported block variants in useful views; finish cold-eye/adversarial review and any resulting fixes. R103 compiled the opening checks but did not run that native scenario.
- Keep Unity open. Complete those gates in isolation, rerun relevant checks after any changes, then publish the exact accepted files and commit them into main. Do not spend the next turn repeating the existing inventory or expanding to unrelated mechanics before closing R1.

No dedicated ground-sprite native benchmark should be assumed to exist merely because it was requested of an agent. No further process from R103 is running. This is a recoverable development checkpoint, not R1 release acceptance.

## Continuation override — R1 accepted 18 September 2026

Read [RELEASE-R1-ACCEPTANCE](RELEASE-R1-ACCEPTANCE.md) before acting on earlier pending checkpoints. R1 runtime/art/native acceptance is complete and promoted to local main. Full R108 had one known timing flake; isolated fixture retry passed, reported separately. The next bounded implementation is [RELEASE-REGIONAL-OUTCOMES](RELEASE-REGIONAL-OUTCOMES.md). Keep the live Unity Editor open; never republish stale draft files over subsequent changes.
