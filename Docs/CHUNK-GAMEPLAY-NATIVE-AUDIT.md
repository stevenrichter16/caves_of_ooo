# Chunk gameplay native acceptance

Status: CGN06 native acceptance passed on 2026-09-16: 28/28 checks, zero C#
errors, zero unexpected errors, frozen sources and complete cleanup verified.
Includes eight real 1920×1080 captures and a 60-second ProfilerRecorder window.

The acceptance workload uses the real SampleScene with its serialized western
spawn, seed 64 through NativeAuditBootstrapSettings, and an isolated disposable
save root through NativeSaveIsolation. It queues keys into the normal InputSystem
and reads UI selections; it does not invoke the quest or transfer methods directly.
No entities are relocated, no fixture rows are created, and no camera/reveal
settings or native world behavior are changed.

## Workload

1. Use N at the ordinary boot menu. verify spawn (2,6) and the generated basket/parcel.
2. Walk to a mutually clear eastern seam and cross into Morrowfast. Approach the
   actual Farra. Capture her available cue; accept the errand through dialogue.
3. Capture her active cue and the actual Q journal containing the quest.
4. Walk west, open the real basket through its action menu, and select the actual
   Sack parcel in PickupUI's container mode, which invokes TakeFromContainerCommand.
5. Carry the cloth east, deliver through the visible dialogue choice, and check
   one 15-dram/fire-clay reward, the same parcel placed at supper, and completed cue
   removal. Verify no repeat delivery choice.
6. Approach the actual Vennit, request regional directions, remember a real
   destination, and read it through Q → Tab.
7. Use F5/F6 to save/load completion; verify the loaded owner graph, reward and
   remembered note.
8. Check unchanged camera/reveal settings and private save ownership. Stop Play,
   restore input/seed/preferences/scenes/GameView, remove only the owned save root,
   and validate the fresh native report and eight PNGs.
9. Record a sixty-second window during the journey with ProfilerRecorder;
   if the journey ends early, record the remaining native-town idle tail.
   Validate real counter units, live sampling and evidence rejection controls.

The route precomputes no artificial corridor. It follows actual native passability,
replans around live actors, and performs bounded real wait turns when no path is
currently available. A failure remains a failure; it is never repaired by warping
or changing the world. Hauling does not cross zone boundaries in current gameplay;
the acceptance route correctly carries the removable parcel.

## Running

`python3 Tools/ChunkGameplay/run_native.py <unique-receipt-name>` prints the command
without launching Unity. Root may add `--execute` only after the normal compilation
and regression gates. Entry:
`CavesOfOoo.Editor.ChunkGameplayNativeAuditBatch.RunFromCommandLine`.

Outputs live under `Docs/Verification/ChunkGameplayImplementation/CGN-<run-id>-*`;
the runner archives the unique native report, cleanup report and captures under
its new receipt directory. Old files cannot satisfy a new run.

## Honesty bounds

Can verify: ordinary bootstrap, native queued movement and three boundary keys,
menu/dialogue/loot/journal state, carried/returned parcel identity, one reward,
visible cue renderer state, real save/load, unchanged display controls, private
save ownership and cleanup, and real 1920×1080 PNG capture.

Cannot verify from counters alone: subjective cue readability, enjoyment, combat
balance, frame rate, every regional quest, all save failures, farm visual quality,
or summit-water feel. Crop harvesting and water contact have separate EditMode
integration gates. Screenshots require actual independent inspection before a
visual acceptance claim. Aggregate editor counters are recorded below; no player-build FPS or isolated
quest-cue cost is claimed.

## Native attempt log

- CGN01 compiled with zero C# errors and reached Farra through actual walking.
  The automation's arrow/Enter selection closed the conversation: legacy mouse
  hover can reset the cursor. The harness now uses the player's labelled letter
  shortcut after natural text reveal, with a post-selection node assertion.
  No production dialogue behavior changed to make the audit pass.
- CGN02 passed the entire expedition, actual guidance/note UI and F5/F6 checks.
  Its final camera invariant was a false premise: existing CameraFollow uses
  distinct town and wilderness fit sizes. The harness now compares each area
  against its own earlier frame and also verifies the actual zoom preferences.
- CGN03 stopped when a real spore-shambler hit stunned the player. The game
  correctly consumed the attempted turn without moving; the harness had assumed
  every planned key moved. It now permits a bounded retry only when Stunned was
  present, the real turn advanced, position stayed unchanged and the player
  remained alive. Other movement failures still fail; no health or effect changes.
- All three attempts kept sources frozen and restored scenes, GameView, input,
  seed, save scope and last-game preference; their private save roots were removed.
  No successful final acceptance is claimed from these incomplete attempts.
- Static CGN02 inspection confirmed clear quest directions and travel notes,
  active-marker removal at completion/reload, but found the available amber
  exclamation hard to distinguish from the town ground. A focused contrast fix
  follows that real camera evidence; full acceptance will be rerun afterward.

- CG16 reproduced both actual-palette contrast failures; the narrow atlas and
  outline correction passed all 356 focused cases in CG17.
- **CGN04 passed:** seed 64, 221 actual movement keypresses including three
  chunk crossings and one legitimately consumed stunned turn; 27/27 checks;
  no fixture teleportation, direct quest/inventory calls, health changes or
  suppressed effects. Available → active → completed/reloaded cue lifecycle,
  real parcel recovery, one reward, Vennit directions, Q/Tab and F5/F6 all pass.
  Field half-height 16.8 restores on returning west; town half-height 16.5
  restores on load; GameplayZoomMultiplier remains 1.2 and full reveal true.
  All sources stayed byte-identical during execution.
- Native shutdown kept the private root until saving was unregistered, removed
  that root afterward, and restored the inherited root, last-game preference,
  seed, input settings, scenes and GameView. Exit code 0; no cleanup errors.
- Root and independent review inspected the real captures. The available ! is
  now legible over Farra and both northern givers; only Farra changes to a
  diamond and disappears after completing/loading her quest. Quest directions
  and travel notes fit without clipping. The existing journal compositing shows
  a dim legacy map beneath its lower half; it does not obscure the text and was
  not changed by this feature. No all-biome readability, animation-feel, FPS or
  farm/stubble visual-playtest claim is made.

## Evidence

- [Validated run and source freeze](Verification/ChunkGameplayImplementation/CGN04-native-journey/receipt.json)
- [Native checks](Verification/ChunkGameplayImplementation/CGN-ab39881b5ba444b2a3d453222717ecdb-native.json)
- [Cleanup](Verification/ChunkGameplayImplementation/CGN-ab39881b5ba444b2a3d453222717ecdb-cleanup.json)
- [Available markers](Verification/ChunkGameplayImplementation/CGN-ab39881b5ba444b2a3d453222717ecdb-cue-available.png)
- [Quest journal](Verification/ChunkGameplayImplementation/CGN-ab39881b5ba444b2a3d453222717ecdb-journal.png)
- [Travel notes](Verification/ChunkGameplayImplementation/CGN-ab39881b5ba444b2a3d453222717ecdb-travel-notes.png)

## Required native performance window

CGN05 collected valid data but failed an audit-only units assertion: Unity
reports `TimeNanoseconds`, not `Nanoseconds`. CGN06 corrects that assertion and
latches any recorder becoming invalid. It passed all 28 native checks and five
evidence counterchecks: absent, short, unavailable, empty and wrong-unit data
are rejected, while the unchanged valid report is accepted. No production change
was needed after CG18.

CGN06 sampled **60.003 seconds**, with **16,156 frames** per counter.
The final **2.206 seconds** were an explicitly recorded native-town idle tail
after the journey. The rest includes actual walking, menus, quest state changes,
chunk entry, captures and saving/loading.

| Timing counter, sorted by maximum | Average | Maximum |
|---|---:|---:|
| Main Thread | 3.707 ms | 479.459 ms |
| COO.Input.Update | 0.152 ms | 232.851 ms |
| COO.ZoneRenderer.LateUpdate | 1.121 ms | 27.141 ms |

GC allocated per frame: average **69.23 KiB**, maximum
**78.66 MiB**. These are gross editor/workload figures, including
automation path planning, logging, screenshots and existing world/load systems.
The spikes are recorded, not attributed to quest markers or described as meeting
a frame budget. This is after-writing profiling, not a controlled before/after
optimization experiment or a claim of allocation-free gameplay.

[Final native checks and counters](Verification/ChunkGameplayImplementation/CGN-dd380380841847e7ba1bfc4188e5c6b3-native.json) · [Final cleanup and evidence counterchecks](Verification/ChunkGameplayImplementation/CGN-dd380380841847e7ba1bfc4188e5c6b3-cleanup.json).
