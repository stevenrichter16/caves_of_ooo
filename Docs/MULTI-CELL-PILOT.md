# South-spawn multi-cell pilot

Status: one-chunk pilot complete, including full regression and final 56° native acceptance, 2026-09-10. Parent design:
[feasibility audit](MULTI-CELL-ENTITY-FEASIBILITY.md). CoO-original, no Qud
parity claim. User authorized full implementation in one chunk and then chose
the attached pink-grey ridge image directly south of spawn.

## Scope and rollout

Target `Overworld.3.7.0`, south of Morrowfast (`Overworld.3.6.0`). Replace that
chunk's former TendrilFen layout with the image-led authored ridge pilot.
Other chunks keep their content. Shared spatial APIs default to existing
single-cell behavior; only pilot entities receive footprints. Current saves
must retain unrelated occupants and removed pilot owners across reloads. Old-save
migrations are no longer a requirement (explicit user direction, September 10).

Reference: user attachment `codex-clipboard-57b468dd-1ee9-4f8c-b36a-25dae7503ddb.png`.
Use its pink-grey grain, radiating ridges, small western hermit enclosure,
tar seeps, sparse rocks, pipes/vents and open paths. Tepuibone remains mineral
veins. Continuous ridge sections are individually destructible; large props
and the large Maw-Toad have one owner each. Source art stays editable in Blender.

## Verification corrections before production edits

| Finding | Decision |
| --- | --- |
| User's location supersedes the initially proposed northeast chunk | Author the south chunk, not `Overworld.4.5.0`. |
| Spawn is Morrowfast; south is currently a fen | Change only this explicit site and its map description; preserve neighbors. |
| Existing entity lists represent anchors | Add a separate physical occupancy query; never duplicate canonical membership. |
| Morrowfast footprint exceptions are specialized | Preserve town behavior during this pilot; generalize no town owners in this wave. Authored scenery conversion happens inside the pilot. |
| New content already has suitable 16x16 species/prop sprites and 3D model families | Reuse native identities and art; create refined pilot geometry without glyph-only objects. |
| Scene at start is clean, Editor stopped | Close normally for headless tests; source snapshot stored outside repo before edits. |
| Shared methods already have unrelated working-tree changes | Preserve exact baseline files; audit owned delta independently. Never stage/revert other work. |

## Milestones

1. Baseline and RED spatial contract: footprint validation, occupancy,
   transactions, old single-cell controls, save reconstruction.
2. Shared spatial query integration: collision, reach, projectile and AoE
   targeting, interaction, removal/dirtying; full stationary pilot tests.
3. Author the south ridge chunk and reusable 3D assets. Native entities own
   all destructible scenery; preserve traversable north/south entrances.
4. Movable prop and large creature: shape-aware pathfinding and displacement,
   contact and lifecycle, footprint-safe boundary transfer, rendering/picking.
5. Dedicated adversarial suite, cold-eye review, full regression, native
   deterministic playable scenario and visual/performance verification.

Each milestone uses RED -> GREEN -> counter-check -> adversarial -> review.
No partial content opt-in before its gameplay and persistence contracts pass.
The pilot uses fixed collision orientation; visual facing does not rotate the
body. Simultaneous cross-zone straddling and structural-collapse simulation
remain outside this one-chunk experiment.

## Required gates

Stationary object hit from any occupied edge; hole counter-check; one blast
hit per owner/pulse; separate strikes remain separate; atomic blocked move;
narrow passage rejection and valid alternate path; forced movement while
stunned; one death/loot/drop; full cleanup after destruction; save and revisit
retain damage/absence; ordinary worlds and Morrowfast remain unchanged.

## Performance and diagnostics

Per-zone derived occupancy, fast single-cell queries, no per-cell all-owner
scan. Invalidation touches old/new footprint and render bounds. Profile actual
pilot play and report max/percentiles; no desktop frame-rate promise. Emit
placement/move/refusal and cleanup/contact diagnostics and pin them in tests.
Follow [performance foundation](PERF-FOUNDATION.md) and
[adversarial playbook](../ADVERSARIAL_TESTING.md).

## Implementation log

- Pre-edit snapshot: `/tmp/coo-multicell-before-20260910`, including hashes.
- MC00 baseline: 10,486 / 10,517 pass, 31 pre-existing failures (29 equipment sprite, 2 old map expectations), zero C# errors. Archive `Verification/GameSystemAudit/MC00-baseline-20260910.xml.gz`.
- MC01 spatial RED confirmed missing footprint API compile errors before production. Archive `Verification/GameSystemAudit/MC01-spatial-compile-red.log.gz`. No stale XML counted.

## Reviews and evidence

The log below preserves each implementation gate, including failed attempts.
Current status is given above and in the final entries; earlier pending notes
describe their point in the implementation, not the latest result.

- MC02 missing integration types confirmed compile RED before targeting/transitions.
- MC03: 66/73 passed, zero C# errors. Seven planned movement cases failed RED.
- MC04: new contact-path API confirmed compile RED before implementation.
- MC05: 112/134 passed, zero C# errors. Core lifecycle, load-hook, callback publication and diagnostics hypotheses failed as expected; gas contact failures confirmed. Targeting 25/25 plus targeting adversarial 22/22 passed.
- MC06: chunk runtime/types confirmed compile RED before implementation.
- MC07: 158/203 passed, zero C# errors. Core spatial 16/16, core adversarial 30/30, movement 9/9, transitions 22/22, targeting 25/25 and targeting adversarial 22/22 passed. Eight renderer tests now failed actual assertions (one control passed) before render integration. Ability migration recorded 21 confirmed behavior failures, one invalid precondition and 13 passing controls. Chunk map/sprite requirements failed before those additions.
- Test-fixture corrections: BurningEffect.OnApply changes duration, so the 4-turn Pyroclasm precondition is set after application. TileState caps energy at 2; source coverage expects that cap. The new handling fixture needs explicit HP Max=100 because Stat's default Max clamps to 30. These are test setup corrections, not shipped gameplay fixes.

### Implemented contracts under verification

Canonical Cell.Objects still serializes one reference. Cell.Occupants / Zone.GetOccupants resolve physical body owners; translated GetOccupiedCells includes holes and negative offsets. The derived index validates saved bodies before rebuilding, repairs duplicate aliases deterministically, and is ready before load hooks. Dynamic body attachment/removal/change is transactional. Render callbacks publish after complete membership changes. Save refuses direct uncommitted CellsRaw mutation.

Movement checks the full body; A* can find any reachable contact edge. Gas exposure batches one strongest dose per family/pass for large bodies; separate families and passes stay independent. Targeting snapshots and deduplicates per hit, with native creature-versus-elemental filters preserved. Scene art remains separate owner meshes, and each movable owner has fixed collision orientation.

### Art readiness correction

The initial assumption that every selected blueprint already had a native sprite was wrong: MawToad, CopperPipe, TarSeep and SteamVent lacked their exact native sprite rows. Four 16×16 bodies are being supplied and pinned before close-out. No permission is needed: the user already authorized creating/refining all required art and making implementation decisions.

### Review findings fixed so far

🟡 Dynamic attachment/removal left stale bodies; now atomic, with rejection rollback.
🟡 Saved duplicate references left ghost owners; deterministic canonical repair.
🟡 Invalid saved geometry clipped silently; explicit failure before replacing usable indexes.
🟡 Load hooks observed anchor-only placeholders; rebuild before hooks.
🟡 Dirty callbacks saw partial body registration; publish after complete writes.
🟡 Cell view made single-cell indexed reads scan piles; restored direct-list fast path.
🟡 Large gas contact missed edges or multiplied doses; per-pass family aggregation.

Additional handling, ability, sprite, world-map and renderer changes are undergoing their next gate. No full-suite or native completion claim yet.

### Integrated verification, MC08–MC11

- MC08:212/224 pass, zero C# errors. Actual imported renderer9/9 passed. New cases exposed removed-victim callbacks, native inventory deletion/stacked hermit construction, remote tile contact and active-cache reconstruction; fixture corrections are recorded in the owning audit docs.
- MC09:245/317 pass, zero C# errors. All224 previously implemented cases passed. The added skill-selector sweep produced39 behavior failures and21 controls;33 native receipt assertions failed before its implementation.
- MC10:320/322 pass, zero C# errors. All60 selector and33 native metadata cases passed. Confirmed two new failures: Vault's outer body bypassed an intermediate archive barrier, and a gas behavior removed by an earlier dose caused a null-owner lookup. Both fixed after this RED.
- MC11:331/355 pass, zero C# errors. Confirmed24 assertions across landing events/removed-victim effects, portable models crossing ring/town boundaries, actual native cleanup after output failure, and required live large-creature evidence. Fixed after RED; next verification pending.
- Review correction: Charge may legitimately emit a momentum damage pulse in addition to its ordinary attack. Its passive callback control asserts damage presence; reacted/removed actors must still cause zero stale attack damage.

The current source includes one native owner per ridge section,3-cell copper pipe and2×2 MawToad, four exact16×16 sprites, full-body landing events, per-pass gas/tile/ability damage deduplication, and native ownership throughout 3D presentation. V8 final Blender candidate passes51 actual FBX roundtrips, triangle-interior/winding/degeneracy checks, and all1579 free cells remain connected. Current Unity art is still V5 until the coordinated V8 import.

User relaxed old-save migration scope explicitly. No additional migration effort is planned; current save/load and destruction persistence remain required gameplay correctness.

### V8 import and first full regression

- V8 imported51/51 models with zero C# errors and all borrowed assets unchanged. Receipt: `Verification/MultiCellPilot/unity-import-v8.json`; matching compressed Unity log retained. Global Assets GUID audit:3771 GUIDs,0 collisions.
- MC12:360/361 pass, zero C# errors. All355 prior pilot cases passed, including native cleanup, mobility and portable rendering. One new far-foot ice test failed; body-wide slip detection fixed afterward, preserving one roll per landing and excluding empty holes.
- MC13 first full:10801/10885 pass,84 failures, zero C# errors. All31 baseline failure names remain. Of53 new failures,42 reflect a recipe test helper's expanded method signature,3 are obsolete fen-location fixtures,1 is the new MawToad roster count,4 are deliberate new picking/slip/empty-anchor regressions, and3 require investigation (save, forge rejection, Shank rejection message). This is a failed regression receipt, not completion.
- Cold-eye checks added actual non-solid creature/prop controls for slip and physical-barrier/empty-anchor controls for Tumble. Root fixed both after MC13 assertion RED. Native receipt additionally checks actual prior view objects disappear after destruction/load, not merely that membership-based lookup stops returning them.

### Camera steering from the user

Before the final full/native run, the user requested a10% tilt away from full overhead to show characters better. Target:81° downward pitch, with ground-grid/HUD/cursor alignment retained. Shared3D camera/picking implementation and focused RED tests are being added; final native captures must use this new view. The existing2D camera remains borrowed and unchanged.

### Camera and pool lifecycle gates

- MC14: camera6 tests,1 control PASS/5 assertion RED, zero C# errors. Implemented the requested81° shared3D tilt afterward, with compensated ground registration and actual physical hit-cell picking.
- MC15:10861/10907 pass,46 failures, zero C# errors. All prior53 regression findings are resolved. Six camera tests and all28 new art/render/camera cases pass. Remaining failures:31 exact baseline failures,14 new pool-projection/diagnostic-selection REDs, and1 remaining old overhead camera-pose constant.
- Native preflight found that per-turn terrain seeding already wet all12 tar body cells, while immediate placement/removal still touched anchors only. This could leave ghost liquid after destruction, and a deterministic forced-toad fixture incorrectly chose a now-slippery destination. Added16 scoped pool tests (14RED/2controls) before fixing the projection lifecycle and dry-body native selector.
- Pool fixes cover immediate whole-body projection, anchor/interior holes, movement/re-add, destruction/removal, shape changes/attachment/detachment, swaps and transitions. Shared same-liquid sources retain their own cells. Other liquid/residue/energy/cloud layers are preserved. Current save behavior is exercised; no additional old-save migration work.
- Peer/root cold-eye reviewed committed old-body handling, preflight-before-writing, overlapping source retention, swap exclusion and physical-vs-flat camera picking. No unresolved blocker identified in those bounded passes. Final full/native receipts remain pending.

### Final headless regression

MC16:10876/10907 pass,31 failures, zero C# errors. Compared exact failure names against MC00: the same31 pre-existing failures remain (29 equipment-sprite adversarial expectations and2 old place/start-map expectations). All390 tests added in this pilot/camera work pass; no additional failures or skipped tests. Receipt: `Verification/GameSystemAudit/MC16-final-full-regression.xml.gz` and its compressed log. Native81° Play audit is now running against V8 with6113 source/scene/settings hashes captured for preservation checks.

### Native mechanics pass and camera visual follow-up

Native run `f8a9acbea29d442e9e75c6f04cc71aa6` completed26 gameplay checks,0 workload errors and all private-save/scene/view cleanup gates. Independent parsing passes315 checks over screenshots, raw frame CSV, exact owner/tile ledgers and cleanup. All6113 protected files match the pre-run snapshot. Four20-second profiling phases retain25,171 raw frames; Main Thread p95 is3.46–3.97ms but the largest retained frame is640.29ms. This validates the observed mechanics and typical measured Editor workload, not uninterrupted60fps or final visual quality.

The actual81° screenshots exposed north-row player-head clipping and visible dark facets/stippling inside rock geometry. MC17 records3 camera-headroom tests: disabled-view control passes, real imported north-player geometry fails at viewportY1.00678, and south framing retains the old12.5 cap. The bounded framing correction is13 half-height plus sufficient edge headroom at closer zoom and Look focus. Art/lighting diagnosis uses actual imported meshes and controlled native GPU captures before selecting a visual fix. These findings keep visual acceptance open; no legacy-save migration work is added.


### Final requested 56° camera and full regression

The user requested another 15° and then another 10° of tilt after the initial
81° view. The shared native town/ring camera now looks down at 56° (34° away
from vertical). Ground projection remains registered to the native grid and
HUD. Ring framing reserves 1.5 cells of headroom (half-height cap 14); native
town minimum half-height is 13.75. Imported player vertices at both borders,
cardinal facing, closer zoom and independent Look targets are covered. This
base-mesh evidence does not guarantee every future animation or equipment pose.

MC22/23 reproduced a related real selection defect: after rejecting a hidden
physical mesh hit, a presenter could still choose the same owner via a visible
flat cell. Both presenters now prevent that fallback while preserving real
no-mesh corners, other visible owners and native sprite priority. The native
acceptance harness chooses an actual imported mesh contact independently of
the picker before checking its result. Four harness controls passed in MC24/25.
A gas fixture now separates stable contact from deterministic merging; all 14
contact controls pass without changing production gas behavior.

MC24 confirmed 19 desired-angle/framing RED assertions before the 56° change.
MC25 passed all net 415 added cases but exposed one obsolete test assumption:
flat point (40,25) can now project onto raised in-bounds north-edge geometry.
The invalid-input control now uses a genuinely distant point (40,125); a new
positive test checks that north-edge geometry is selectable only while visible,
with null/-1 outputs after hiding it. This is an expectation correction, not a
new production change.

MC26 full regression: **10,902 / 10,933 pass**, zero C# errors and zero skipped
tests. The exact 31 MC00 failure names remain; no new failures. There are 416
net additional passing cases over baseline (one older test was renamed).
Receipt: `Verification/GameSystemAudit/MC26-final-camera56-full-green.xml.gz`,
with compressed log. Independent cold-eye review found no outstanding camera,
headroom or picker blocker. The final native 56° run is the remaining gate.

Controlled actual-model GPU probes, including the real SampleScene, passed
72 independently parsed evidence checks. No controlled new rendering defect
was demonstrated, so no speculative lighting, shader or art change was made.
Historical dark-facet observations remain bounded by scene illumination and
faceted art. These probes are documented in the art review and do not substitute
for the final 56° in-game screenshots.


### Final native 56° acceptance and close-out

Run `da0e5e856474438e96c7b6436b7cbd42` completed **26/26 gameplay checks**
and **315/315 independently parsed artifact checks**. The native wrapper
exited 0, with zero C# errors, zero workload/cleanup unexpected errors and no
changed protected source, scene, content or settings files. Private saves,
scene selection, GameView, input/display preferences and inherited save-root
settings were restored. The unrelated Unity-generated QualitySettings
serialization was subsequently restored to the original pre-work bytes.

Actual screenshots were inspected by root and the independent art reviewer.
At the north entrance the hood is fully visible with a small margin above it;
the center and side-facing player silhouettes are clear. The map stays separate
from the sidebar and hotbar. The black unobserved cells are native field of view.
No new angle, framing or HUD blocker was found. This accepts the observed base
poses and views; it does not certify every equipped animation or subjective feel.

Four 20-second phases retained **28,601 raw frames** over **80.0051 seconds**,
with 25 successful ordinary moves in each walking phase. Main Thread p95 was
3.11–3.64 ms; retained maximum was **620.07 ms**, with another 396.91 ms restored
idle frame. These uncapped Apple M5 Metal Editor observations do not establish
stable 60 fps. All frame rows and outliers remain in the receipt.

Evidence:
- [Native report](Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-native.json)
- [Independent artifact verification](Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-independent-evidence.json)
- [Cleanup](Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-cleanup.json)
- [Launcher/source-preservation result](Verification/MultiCellPilot/native-launch-149a82a3f3754ad9adf6ed5e687164e6/launch-result.json)
- [North entrance](Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-south-entry.png)
- [Destruction](Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-destructible-ridge.png)
- [Restored player and pipe](Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-restored-save.png)

The one-chunk rollout now includes shared occupancy, destructible authored
scenery, a movable three-cell pipe and a two-by-two creature, with pathfinding,
displacement, duplicate-damage and current-save gates complete. Fixed physical
orientation, no simultaneous cross-zone straddling and the game's existing
faceted lighting remain explicit limits. Old-save migrations, global scenery
conversion and the older W6/game-wide audit backlog are outside this close-out.
