# Multi-cell ridge native acceptance

Status, 2026-09-10: final 56° native gameplay/cleanup acceptance passed in run
`da0e5e856474438e96c7b6436b7cbd42`, with 26 live gates and 315 independent artifact
checks passing. MC26 passed all 416 net additional tests; its only 31 failures
match the exact pre-existing baseline. The four picking helper tests are GREEN.
Historical 81° evidence is separately labelled below. Visual quality and feel
remain bounded by the separate look-pass. CoO-original.
Parent: [pilot plan](MULTI-CELL-PILOT.md). This document scopes the real Editor
acceptance that follows spatial/runtime/render EditMode gates.

## Verification sweep

| Existing surface | Verified contract and consequence |
| --- | --- |
| `NativeSaveIsolation.Begin/Restore/Finish` | Disposable token-owned save root survives domain reload and remains held through Play shutdown. Restore original preferences and root only after shutdown; never touch the user's saves. |
| `SpawnRing3DNativeAuditBatch` | Native Editor, actual 1920×1080 GameView, normal SampleScene/bootstrap, boot-menu N through a dedicated Input System keyboard; exact scene/view/seed cleanup receipts. Reuse these ownership patterns. |
| `SpawnRing3DNativeAudit` | Handles nested coroutine exceptions/disposal, records exact input/fixture distinctions, retains native AI/health/FOV, binds real transition results through InputHandler. |
| `SpawnRing3DNativeAudit.Profile` | ProfilerRecorder names are discovered with units/availability rather than assumed. Required engine/input/renderer/GC counters, raw per-frame data, and phase timing exclude capture/setup overhead. |
| Spawn and destination | Morrowfast `(3,6)` ordinary entrance `(40,23)`; pilot `(3,7)` open north/south at x40. The former ring audit's permanent fen-water precondition is deliberately obsolete for fresh pilot content. |
| Physical owner contract | The ridge at `(6,2)` has an anchor hole, starts its occupied edge at `(8,2)`, and owns ten cells. The pipe has three cells; the MawToad has four. Selection, damage, occupancy and visuals must refer to the same owner. |
| Native actions vs fixture setup | Keyboard boot/movement/F5/F6 and real push/drag/destruction APIs are distinct evidence. Any deterministic relocation or damage stimulus is named explicitly and never presented as keyboard-driven combat. |

## Workload

1. Enter the normal game with N using an isolated private save root and the
   fixed existing audit world seed. Walk from Morrowfast into the chunk to
   the south with ordinary S input. Pin player/zone/turn/presenter binding.
2. Capture the real 1080p GameView in ordinary FOV. Assert the 149 authored
   owner contract, exact known footprints/holes, and native 3D ownership.
3. Exercise physical edge selection and a nonlethal/destructive stimulus on
   separately owned ridge pieces. Pin one durability change, one removal,
   no neighboring owner damage, every covered cell cleared, and no hole hit.
4. Check the actual large creature's footprint-aware path search, forced
   relocation with one registered owner, blocked far-edge return, and exact
   restoration of its native body/home/brain. Do not create a chase: the
   MawToad's canon says it does not chase. Check the pipe's successful/blocked
   displacement and actual hauling. Position any temporary fixture explicitly
   and restore/remove it in a finally block.
5. Revisit the same chunk through the real north/south transition mechanism;
   damage, missing owner and displaced pipe must remain unchanged.
6. Use ordinary F5 at the player input boundary; perform post-checkpoint
   counter-mutations; use F6. Compare immutable owner positions, durability,
   absence, native parts and tile writing; reject stale owner/player aliases.
7. Collect 80 seconds of real 1080p native rendering in four 20-second
   phases: idle and paced ordinary walking before mutations, then idle and
   walking after the gameplay/save workload. Preserve AI, health, turns and
   FOV. Record raw frame counters with availability/units, wall duration,
   percentiles/max, input outcomes and exact workload identity.
8. Capture post-destruction and restored-save views; restore input settings,
   display preferences, GameView, seed, scenes, inherited save root and last
   game preference. Require a run-specific cleanup receipt and report.

## Gates and evidence

TDD pins receipt validation against missing/failed checks, wrong run/root,
wrong resolution, stale captures, missing/incomplete profiling and failed
cleanup. The native workload is an explicit menu/command-line audit only;
it never registers an automatic runtime hook outside its owned invocation.
Reports and screenshots use `Docs/Verification/MultiCellPilot/MCN-<runid>-*`.
Every report states which steps used ordinary input and which used named API
stimuli or fixture positioning. No recreated world substitutes for the live
bootstrap world, and no old result substitutes for the current run.

Can verify: exact state, canonical identity, physics cells, hit counts,
persistence, current-zone/presenter binding, captured pixels and measured
counters. Cannot establish by these assertions alone: artistic closeness,
pleasantness, complete game balance, broad-world performance, or subjective
feel. Visually inspect the actual captures separately before calling the
pilot complete.

## Review and implementation log

The following entries describe their original gate state. References to native
execution being pending belong to that point in the log; see the explicit
historical result and current camera follow-up below.

- Read both ordinary ring acceptance and its Editor teardown/isolation paths.
- Corrected inherited fen assumptions in this plan: fresh ridge ground is dry;
  preserved writing in legacy saves remains deliberately untouched.
- MC09:33 receipt metadata tests failed because the explicit native harness
  did not exist. Implemented only after this assertion RED.
- MC10:33/33 receipt metadata cases passed, zero C# errors. Native gameplay,
  three GameView captures and profiling still await a real Editor run.
- Peer cold-eye found output failure could bypass settings cleanup because
  Finish ran before OnDestroy's finally. Actual invalid-output-path regression
  and two live-toad evidence countercases failed in MC11, as hypothesized.
  The input clone remained installed after output failure; missing live-toad
  evidence was accepted. Fixed after RED: arm cleanup before mutation; put
  all OnDestroy finalization inside unconditional restoration; always request
  batch teardown even if report/abort writing throws; require and perform
  native forced-body movement, far-cell blockage and exact restoration.
  MC12: all36 native metadata/cleanup cases GREEN, zero C# errors.
  Live movement itself still requires the native run; no headless receipt
  claims that Play exercise has already happened.
- Native files: `Assets/Scripts/Scenarios/Custom/MultiCellPilotNativeAudit.cs`,
  its `.Profile.cs`, and `Assets/Editor/Scenarios/MultiCellPilotNativeAuditBatch.cs`.
  Entry point: `CavesOfOoo.Editor.MultiCellPilotNativeAuditBatch.RunFromCommandLine`.
  Menu: `Caves Of Ooo/Scenarios/World/Multi-Cell Ridge Acceptance`. Profiling is
  mandatory for this audit; no optional command-line profile flag is needed.
- Peer review found private-save ownership, real input, named API stimuli,
  stable snapshot identity, same-run captures and batch teardown consistent.
  Raw-profile verification checks path/size/SHA and metric metadata; it does
  not independently recompute all percentiles from CSV. Raw data is retained
  for independent analysis and no performance result is claimed yet.

## Final native preflight (v8 art)

Read-only verification during MC13 full regression confirmed source/resource
layout byte identity (`0010e4187170e04da5b6f83938c2bb8a70a14fe958cf20f57e3c394734866955`),
149 owners,421 solid cells, clear north/south x40 entries, the ten-cell ridge
with an empty anchor hole, the four-cell MawToad and three-cell pipe. Native
Player Strength18 can haul Weight60. The ordinary input boundary really calls
EndTurnAndProcess after a successful south transition. The native toad has
multiple reachable destinations at least two steps away; pipe+hauler lanes
are selected from actual five-cell clearance. Fixed destinations are never
assumed clear. Remaining preconditions depend on live input/FOV/AI/rendering.

Review strengthening installed after the MC13 freeze: retain actual model
handles before destruction and current-version F5/F6, then require them gone
or inactive afterward. The nearby destruction target and player must have
visible models first; all other saved owners may have inactive models under
ordinary FOV. RefreshCurrent constructs these before applying visibility. API rejection alone is insufficient because the API
already refuses any owner missing canonical membership. This does not add
old-save migration work.

MC13 full regression exposed the existing creature-icon roster pin at55 after
the native MawToad icon made56. The pin and explanatory comment were changed
only after that confirmed RED. All115 owned targeting/runtime/native tests
were already green in MC12; the new model-handle assertions still await actual
Play execution, and no native acceptance result is claimed here.

Post-MC14 read-only slip review found the first candidate toad landing at
(12,2) overlaps native tar. Immediate LiquidPool projection was anchor-only,
but the existing full-body TileStateSource renewal makes this ground oily
after the profile advances turns. Exact-position movement must therefore
select a current dry body and dry path, retaining all ordinary hazard code.
The same review found immediate pool projection and lifetime cleanup did not
cover the whole footprint. MC15 captured14 REDs and2 controls among16 dedicated pool lifecycle/native
selector cases. The minimal fix was installed only afterward: immediate
projection and departure cleanup follow every committed physical cell, and
exact-position toad/pipe stimuli choose current dry body cells. The native
profile and all hazard code remain active. MC16 passed all16 dedicated cases
and all390 added tests, with zero C# errors; the full10,907-test run retained
only the31 exact pre-existing failures. Actual native execution is pending.

## Historical 81° native acceptance result

Run `f8a9acbea29d442e9e75c6f04cc71aa6` used the historical 81° downward camera.
It completed through ordinary native Play:
26 required gameplay/presentation/save/profile gates passed, no unexpected
errors, and cleanup exited0 with the isolated save directory removed and
scene/view/input/preferences restored. All three captures are actual
1920×1080 GameView PNGs. Four profile phases recorded25,171 frames across
80.004740583 seconds, with25 successful ordinary keyboard movements in each
walk phase. Idle phases preserved the player position and turn count.

Independent retained-artifact validation passed315 checks. It parsed every
CSV row, recomputed all available count/mean/max/p95/p99 summaries, checked
phase/tick/position evidence, verified PNG headers, and compared saved/loaded
owner and tile ledgers byte for byte. The current save has148 owners after
one intended ridge destruction. This closes the earlier CSV-metadata-only
evidence bound for this run; the in-engine validator itself is unchanged.
Raw compressed CSV SHA256:
`13ea6636327f1764505d0319e8e476b2032878c60b23777afd9eec4c1451efc1`.

Artifacts live under `Docs/Verification/MultiCellPilot/MCN-f8a9acbea29d442e9e75c6f04cc71aa6-*`:
`native.json`, `cleanup.json`, `independent-evidence.json`, the three captures,
full profile metadata/raw rows, and saved/loaded owner/tile ledgers. Reproduce
the independent checks with `verify-native-evidence.py` in that directory,
passing the native report and optional `--output` JSON destination.

The observed gameplay and cleanup gates passed. Artistic closeness, any
visual defects visible in the captures, and subjective feel remain the
separate human/agent look-pass; these metrics do not establish a production
performance budget or complete whole-game correctness.

Measured performance detail: this Apple M5/Metal Editor run was uncapped
(targetFrameRate=-1, vSyncCount=0). Engine-frame means across the four phases
were2.99–3.33ms and p99s3.85–6.65ms. Outliers were real:635.76ms during pristine
walking and465.53ms during restored idle; restored idle also recorded a
60.8MB peak GC Allocated In Frame. The observer includes Editor/UI work and
completed-counter lag, so these observations neither identify a cause nor
justify a claim of uniformly smooth production play. The retained CSV makes
those stalls inspectable instead of hiding them behind averages.

Independent pool/dry-selector cold-eye review also completed with no blocker:
committed old single/indexed bodies are unprojected before registration
changes, same-liquid shared owners retain their projection, both swap
participants are excluded from departing-source retention, other writing
survives, and candidate dry-ground reads follow actual feet while excluding
holes. The review changed no files and launched no Unity session.

## Camera-relative native picking stimulus (MC23 follow-up)

Status: MC23 confirmed all four new native stimulus tests RED because the private
selector was absent. The minimum helper and audit adaptation then passed all four
tests in MC24 and MC25, with no C# compiler errors in the coordinated gates.
The production camera now targets 56° downward. Final native run
`da0e5e856474438e96c7b6436b7cbd42` passed this gate and all other required live
gates; see its independently checked result below.

The old native visual assertion cast at a flat cell centre and required its
physical mesh contact to retain that cell. A tilted camera projects raised
geometry across neighbouring flat cells, so this was an invalid stimulus
assumption. The independent `physical_edge_selection_and_hole` gate still checks
owner occupancy at (8,2) and the empty hole at (6,2) exactly as before.

The visual gate now chooses an already-visible, nonanchor boundary cell. It
samples a real imported mesh surface there, projects it through the actual world
and source cameras, and independently confirms the nearest collider contact
still belongs to that owner and physical cell. Only then does the audit call
`TryPickWorld` once and compare its canonical owner and physical coordinates to
the independent measurement. The selector never calls the picker to search for
an outcome that passes. It changes no world placement, FOV, fog texture, camera,
or model. It uses no fixed camera-angle constant, so the same measurement follows
future approved camera tilt changes.

Counter-checks use the same real imported ridge: an unoccupied hole, a hidden
physical cell while another cell keeps the view active, and disabled mesh
colliders cannot supply a valid audit contact. A separate positive test confirms
that the chosen point hits the imported mesh, differs from its flat ground
coordinates, and returns the measured physical cell through the real picker.

Files: `Assets/Scripts/Scenarios/Custom/MultiCellPilotNativeAudit.cs`;
`Assets/Tests/EditMode/Presentation/Rendering/MultiCellPilotNativePickingStimulusTests.cs`.
This is a test-harness correction, not a change to player picking semantics.

Independent cold-eye review by the spatial audit agent found no blocker in the
bounded selector. Its nearest-collider filter is conservative: an occluding
hidden or unrelated surface may make a sample unusable, but cannot manufacture a
passing target. Allocations occur only in this explicit native audit stimulus,
not in ordinary player picking. MC24/MC25 completed the four helper GREEN cases;
the final 56° native run subsequently passed the live acceptance gate.

## Final 56° run readiness

Read-only review of `/tmp/coo-run-pilot-native.py` and the retained
`Docs/Verification/MultiCellPilot/verify-native-evidence.py` found no concrete
blocker. The launcher refuses an existing Unity process, restarts and settles
MCP before launching the ordinary Editor, snapshots protected source files,
checks compiler errors before selecting exactly one newly created native report,
and independently validates artifacts only after Unity has exited and cleanup
has completed. The verifier ties report, cleanup, profile, raw CSV and save/load
ledgers to the run identity, recomputes available frame summaries, checks ordinary
walk/idle controls, and requires the three 1920×1080 PNGs. Its checks do not judge
camera feel or certify image quality; the final captures require a separate
look-pass. This review launched no process and changed no Assets/source/tests.


MC26 final full EditMode gate: 10,933 tests, 10,902 passed, zero skipped, zero C#
compiler errors. The 31 failures match the exact pre-existing baseline; all 416
net additional tests passed. The four camera-relative native picking helper
cases remain GREEN. The final 56° native Editor run followed this gate and
completed successfully, as recorded below.


## Final 56° native acceptance result

Run `da0e5e856474438e96c7b6436b7cbd42` completed all 26 required live gates with
zero failures, zero unexpected errors and no fatal exception. Cleanup exited 0,
removed the owned private save root, and restored seed, inherited save root,
last-game preference, scenes, GameView, input settings and display preferences.
The launcher exited 0, reported zero C# compiler errors and no changed protected
files, and produced exactly one new native report. All three captures are actual
1920×1080 GameView PNGs from this run.

The picking stimulus now proves the intended tilted contact: its imported mesh
point was (8.50,1.04,22.50), projected to legacy cursor (8.50,23.20). That cursor's
flat cell is (8,1), while the real picker returned the independently measured
physical occupied cell (8,2) and its canonical ridge owner. The separate empty
anchor-hole occupancy assertion also passed. This replaces the old overhead
assumption with an observed current-camera result.

Independent retained-artifact verification passed 315 checks, with zero failures,
including every raw CSV row, all available per-phase metric summaries, phase
identity, ordinary walking/idle controls, PNG headers and byte-identical saved
and loaded owner/tile ledgers. The save contains 148 unique owners after the
intended one-owner destruction. The verifier was run again independently after
the launcher's successful check; both accepted the same retained files.

The four phases recorded 28,601 frames across 80.005094708 seconds. Both walking
phases completed all 25 ordinary keyboard steps with no rejected targets or
unmoved inputs. Idle phases kept player position and tick unchanged. Player HP
remained 40 in every phase. Each phase supplied 13 available metrics whose
count, mean, maximum, p95 and p99 were recomputed from raw rows.

| Phase | Frames | Seconds | Ordinary steps | Mean frame ms | p99 frame ms | Maximum frame ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Pristine idle | 7,637 | 20.002 | 0 | 2.62 | 3.59 | 20.74 |
| Pristine walking | 6,899 | 20.001 | 25 | 2.90 | 3.85 | 616.37 |
| Restored idle | 6,722 | 20.002 | 0 | 2.98 | 4.19 | 397.60 |
| Restored walking | 7,343 | 20.001 | 25 | 2.72 | 3.68 | 23.61 |

This Apple M5/Metal Editor run was uncapped (targetFrameRate=-1, vSyncCount=0).
The long frame stalls are real observations, and restored idle recorded a
61,291,848-byte peak in GC Allocated In Frame. Editor/UI/observer work and delayed
completed-counter samples are included; these observations neither isolate a
cause nor establish a production frame-time budget or uniformly smooth play.
They do not justify attributing a performance improvement to camera tilt.
Gameplay, identity, persistence and cleanup assertions do not certify artistic
closeness, the entire world, subjective feel or absence of unobserved defects.

Artifacts: `Docs/Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-*`.
The launch receipt is in `native-launch-149a82a3f3754ad9adf6ed5e687164e6/`.
Raw compressed CSV SHA256:
`6d02df8392f32cc215f1bb18c91c4dc4c0595da8d501014860ed108bf3f1a3da`.
Reproduce with the retained `verify-native-evidence.py` and this run's native
report. The final review changed only documentation and launched no Unity process.
