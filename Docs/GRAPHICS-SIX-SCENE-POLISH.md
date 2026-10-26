# Six connected scenes: consistency and polish

2026-10-26. **Connected CopperPipe correction accepted in native tests and
ordinary Editor gameplay.** The six-scene review started from source c3d866f0c;
the scoped source change is now implemented. Source `6b1032459` is delivered in verified Mac and Linux packages; actual Mac
restart/travel checks passed. The whole opening is not yet accepted.

Scope: glade, cellar, north works, Nella's plot, east connector and Orven's kitchen.
Use the existing real geometry, actors and controls. Preserve accepted native art
unless a named defect warrants changes; no new mechanic expansion. Root owns
Unity. Editor was idle, scene clean, tests/imports inactive and save isolation
unowned before beginning. New immutable protection snapshot is
`/tmp/coo-six-scene-polish-protection/before.json`; normal saves/preferences,
preexisting 34 tracked changes and current Mac/Linux builds are protected.

## Original pass plan

1. Start a fresh isolated normal New Game at seed 64 through SampleScene and
   ordinary build selection. No presentation override, teleport, reveals or grants.
2. Capture actual 1280x800 views at default and ordinary 2× zoom, walk connected
   routes and review each scene's paths, scale, material palette, focal hierarchy,
   actor motion, fog and selection. Reviewers separately inspect recent evidence.
3. Prioritize the three largest demonstrated inconsistencies. Reuse valid
   greybox/accepted art; use actual capture plus imagegen for new visual design,
   record part/state ownership, author native assets, test runtime changes RED
   then GREEN with counterchecks and independent review.
4. Replay affected real paths/actions and compare disclosed owners and framing.
   Preserve test checkpoints before ending isolation. Review exact owned files,
   update coverage/continuation and push to main; storage inspection only.

Whole-opening completion requires scene and motion acceptance, not merely six
visits. Native tests, Editor input and packaged/device results stay distinct.
No Linux or physical Deck acceptance is implied by this pass.

## Named defect, corrected premises and source change

The same CopperPipe family appeared as literal `=` marks in North, repeated
isolated elbows in East, and connected native copper at Nella. The difference
was presentation scope, not different pipe mechanics. The reference remains
the user's Qud-like, flat native 16×24 style: readable material identities and
clear cells. No external asset reuse, engine parity or visual-quality parity
with Caves of Qud is claimed.

| Premise | Verified correction and preserved boundary |
| --- | --- |
| North needs a newly drawn pipe | The approved `nella-corner` atlas already has all 16 cardinal pipe masks. Reuse its editable source, palette and pixels. |
| East's elbows are a topology calculation failure | `FlatPixelEastKitchenCatalog` deliberately used static frame 15 for every CopperPipe. The connected catalog previously admitted only exact 11.8. |
| Pipes carry irrigation water | CopperPipe is non-solid, nonportable Terrain with Copper material and conductivity 100. It conducts charge; this change invents no fluid flow, irrigation, heat or active-charge art. |
| Reusing the catalog should admit all its art everywhere | Only CopperPipe gains exact North 11.9 and East 12.10 admission. Peat stays Nella-local in this adapter; East's existing peat adapter and North's prior peat presentation remain unchanged. Kitchen and other addresses do not gain pipe topology. |
| A failed optional query can overwrite the shared output | East may already have its approved static elbow. The connected query now assigns a temporary tile only on success, so absent/malformed connected pixels do not erase that fallback. |
| Same seed means an identical before/after scene | Two fresh seed 64 games followed different routes and combat histories. Runtime IDs, ticks, HP and time of day differ. These captures are qualitative owner/framing comparisons, not pixel-identical replays. |

Production changes are confined to `FlatPixelNellaCornerCatalog`'s shared
role-specific admission and `FlatPixelGreyboxCatalog`'s routing/output handling.
Existing current-graph, unclaimed Spread, exact owner, material, authored
override, custody and footprint checks still apply. No generator, save schema,
actor, collision, inventory or hazard-state code changed. No new art was needed:
the earlier actual-capture/imagegen/extraction/native-source loop is preserved
in [Nella's corner record](GRAPHICS-G3-NELLA-CORNER.md) and
`ArtSource/Live16x24/nella-corner/`.

| Real owner/state | Presentation mapping |
| --- | --- |
| Eligible selected CopperPipe in exact 11.8,11.9,12.10 | `Sprites/Live16x24/nella-corner`, frames 0–15; cardinal mask N=1, E=2, S=4, W=8. Frame0 retains the approved isolated elbow. |
| Disclosed selected eligible cardinal neighbor | Contributes its direction. Diagonals, hidden/unexplored, covered, stale, foreign or malformed neighbors do not create joins. |
| Actor/item covering a pipe | The selected foreground owner wins. An externally authorized current lower-subject Examine can still show that exact pipe's current connected portrait. |
| PeatBog at Nella | Existing frame 16; neutral peat identity, not a volume or wetness promise. East peat continues through its separate existing adapter. |
| Missing/malformed connected atlas | North retains its glyph; East retains its prior static elbow and portrait. Existing remembered-cell policy stays unchanged. |

## Native verification and independent review

The [RED receipt](Verification/Live16x24/six-scene-pipes-red.json),
23:05:18–23:05:24 UTC, records **47 completed cases: 37 passed, 10 failed**.
The ten failures were the expected new North/East all-mask, world/reader,
admission-positive and submitted-repaint cases. They were behavioral failures,
not compile errors.

The [GREEN receipt](Verification/Live16x24/six-scene-pipes-green.json),
23:06:17–23:06:25 UTC, records **105/105 passed, zero skipped**:

| Fixture | Cases | Relevant proof |
| --- | ---: | --- |
| FlatPixelNellaCornerTests | 47 | Existing Nella contract plus North/East masks, selected ownership, exact reader, graph/biome/POI and foreign-world refusal, peat scope, missing resources and incremental connections. |
| FlatPixelEastKitchenTests | 26 | Existing service/owner roles, static fallback, memory, resource injection and disposal remain valid. |
| FlatPixelEastFixtureTests | 32 | Existing East peat, walls and gates retain their own presentation and authority. |

Two source reviews, including independent `inventory_stat_review`, found no
material regression or fixture false premise. Existing static East expectations
were retained: those tests inject their own catalog with optional loading
disabled. Submitted-Tilemap cadence tests explicitly mark dirty cells; they
prove local repaint without a full pass or idle repaint, not ordinary-input/FOV
acceptance by themselves. Actual input evidence follows. No FPS claim is made.

## Actual six-scene evidence and comparison limits

All `six-scene-01` through `six-scene-40` PNGs are real 1280×800 Unity Game views
with adjacent observational JSON receipts and a [40-capture index](Verification/Live16x24/six-scene-captures.json) that corrects early filenames. Root used ordinary New Game/build
selection, movement, zoom and interactions; no teleport, reveal or grant was
used. The receipts distinguish display dimensions from the captured Game view.

| Replay and evidence | Actual coverage |
| --- | --- |
| Before fix, 01–15 | Fresh glade at tick 10, ordinary cellar entry at tick 270, cellar traversal and North arrival. [North 12](Verification/Live16x24/six-scene-12-north-pipes-before-2x.png) shows literal pipe marks at player 32,20 / tick 1150 / HP26. Capture 13 is Look only, 14 is the action menu, 15 is actual Examine. |
| After fix, 16–23 | A second fresh seed 64 game reaches North 32,23 / tick 230 / HP40. Capture 17 was blocked by the patchbearer and is not cover acceptance; 18–19 retain the real combat. [20](Verification/Live16x24/six-scene-20-north-pipe-actor-cover.png) puts the player on 30,20 at tick 360 / HP25; [21](Verification/Live16x24/six-scene-21-north-pipe-reader-after.png) examines from 31,20 at tick 370; [22](Verification/Live16x24/six-scene-22-north-pipes-uncovered.png) shows the uncovered connected run. |
| After fix, 24–28 | Connected walk to Nella, 1× frontage, 2× plot, real dialogue/conversation, then [glade return 28](Verification/Live16x24/six-scene-28-glade-connected-return.png) at tick 1670 / HP25. This seed has no Nella CopperPipe owners; Nella topology preservation is covered by its existing actual record and retained tests. |
| After fix, 29–34 | East entry followed by [player cover 30](Verification/Live16x24/six-scene-30-east-pipe-covered.png) at 29,5 / tick 2400 / HP25; [uncover 31](Verification/Live16x24/six-scene-31-east-pipe-uncovered.png) at 30,5 / tick 2410 / HP18; actual encounter/tonic and [reader 34](Verification/Live16x24/six-scene-34-east-pipe-reader.png) at tick 2470 / HP28. Other occupants continue to cover their own pipe cells. |
| After fix, 35–40 | Connected kitchen arrival, service approach and court view, then the separately identified brine below. Capture 37 selected grass; 38 records the afflicted/sitting response, not successful cook service. These visits do not establish kitchen service success or every motion state. |

The post-fix North run uses owners 4886/4887/4888 at 30,18/19/20; East uses
9354/9353/9352/9351 at 29,3/4/5/6. Before-fix North has the same local coordinates
but different runtime IDs 6915/6916/6917. North before is Dark / tick 1150; the
uncovered after view is Height / tick 370. East's uncovered view is Dawn / tick 2410.
Different time, position, combat, fog and occupants prevent an identical-pixel
comparison. The bounded visual improvement is the continuous material run with
truthful caps and preserved occupant priority.

The first isolated Play session was stopped before its checkpoint was archived;
NativeSaveIsolation disposed that disposable test checkpoint and restored the
normal user save set. It cannot be used for an exact replay. The [preservation receipt](Verification/Live16x24/six-scene-preservation.json) confirms normal Editor/standalone saves, full preference stores, 34 preexisting changed files and previous packages preserved; four owned audit/session preference keys were restored. No storage deletion was performed. The second game's
earned checkpoints were safely archived before cleanup:

- [North fixture](Verification/Live16x24/Fixtures/six-scene-north-pipe64/Quick.json):
  turn 370, HP25/40; save SHA256
  `ff46ccb5220dd0889f31e31975da5fcf1015dc37e67888ea5a2e185eecd061b6`.
- [Kitchen fixture](Verification/Live16x24/Fixtures/six-scene-kitchen64/Quick.json):
  turn 2890, HP28/40; save SHA256
  `3702854d045da9337df667de36be518c583641b6b7d0f2a084d52bc699958a18`.

The later [brine fixture](Verification/Live16x24/Fixtures/six-scene-kitchen-brine64/Quick.json) is also archived at turn 3030, HP28/40. All three belong to game 7149edb751014f1c8ef55c71dd7d5548. Archiving establishes a
continuation point, not new restart-persistence acceptance for this patch.

## Next bounded scene candidate and delivery gate

[Capture 40](Verification/Live16x24/six-scene-40-kitchen-brine-before.png) and the
[brine state receipt](Verification/Live16x24/six-scene-kitchen-brine-state.json)
identify actual BrinePool 11527 at 36,14 and11528 at 36,13. Each retains 90 units of
`brine`, with live brine coating, approximately 20.26 temperature and zero current
heat/cold/charge. Authored Brine material has conductivity 95; the recorded
freezing threshold is −8. These are salt-water owners, distinct from fresh-water
ground. Do not silently admit them as ordinary fresh water or depict an active
charge/flow. This is a named follow-up for source/state audit and a bounded
visual design loop, not a completed correction.

Root next packages the tested pipe source, verifies ordinary launch/Continue and
captures actual delivered gameplay before marking delivery complete. Existing
builds remain protected until replacements are verified. This record accepts
the pipe consistency slice and documents six-site observation; broader scene
hierarchy, animation, remaining incidental identities and regional coverage
remain open. No updated package, Linux execution, physical Deck, measured frame
rate or whole-opening release acceptance is claimed here.


## Delivered pipe correction — 2026-10-26

Source `6b1032459e5e045815af14047aa79710c6b7522e` was pushed and verified on
origin/main before packaging. The historical source-checkpoint paragraph above
records what was pending then; this section closes that bounded delivery gate.

- Mac job `build-abc61e488e`: succeeded, zero errors/four warnings,
  `Builds/Verification/2026-10-26-connected-pipes-6b1032459-current/CavesOfOoo.app`.
- Linux accepted retry `build-8065cf1e41`: succeeded, zero errors/zero warnings.
  The first attempt returned succeeded with two import-worker transport errors
  and four warnings; rejected for delivery and retained separately, not deleted.
- Current Linux archive: `Builds/SteamDeck/2026-10-26-connected-pipes-6b1032459-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`,
  SHA256 `ef2b3e94d3b906838687fb0e73aac1c333b0e52a90a1a00a48563ee4446f8aaf`.
  Independent review checked180 regular files,10 directories,179 checksums,
  sizes/modes/safe paths and19 source support/license files. Binaries are local
  ignored deliverables; source, receipts, checkpoints and screenshots are in Git.

[Eight original Mac captures and action record](Verification/Live16x24/packaged-connected-pipes-mac-play-summary.json)
show ordinary boot/Continue, the unchanged earned North checkpoint, exact pipe
Examine, player covering/uncovering, F5, full process quit/relaunch/Continue and
ordinary North→glade→North travel. Images are1280×856 CUA screenshots including
56px of Mac title chrome, with no repaint or cropping. The final earned save is
North31,20 at turn490, HP25/40 and50 drams. Its [checkpoint](Verification/Live16x24/Fixtures/packaged-connected-pipes-return64/Quick.json)
retains save SHA256 `f466413c2f5a88ddf09f219145c34825b04b0d6fe1b992e3300d6fd220cda2a7`.
The separate binary-prefix observation confirms the zone/cell; HP/turn come
from Quick metadata and actual display, not that limited parser.

[Independent package review](Verification/Live16x24/packaged-connected-pipes-independent-review.json)
found no material defect. Saved/restarted pipe/player/pool/worksite/tree image
regions are pixel-identical; return world interior also matches. Both Player
logs are retained: no exception/fatal/error lines in the scoped scan, with the
known shutdown thread-finalization and ComputeBuffer disposal notices still
present. This does not claim a warning-free runtime.

[Guarded restoration](Verification/Live16x24/packaged-connected-pipes-project-restored.json)
compared48,827 baseline tracked objects and restored only three reviewed build
serialization files. All34 preexisting tracked edits, normal Editor saves,
complete logical preference maps and previous packages were preserved. The
standalone's original save-directory absence was restored by archiving the test
save tree, not deleting it. Editor idle; standalone closed. Storage inspection
showed about415GiB available; no storage/cache cleanup performed.

Can verify: native105-case suite, actual Mac appearance/actions, restart/travel,
archive integrity and preservation. Cannot verify: Linux execution, physical
Deck controls/performance, all scene states, whole-opening or regional release.
Next is the named kitchen brine identity through capture→imagegen→editable
native source→integration, followed by actor-motion/incidental-identity review.
