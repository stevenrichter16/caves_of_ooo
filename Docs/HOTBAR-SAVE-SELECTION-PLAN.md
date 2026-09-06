# Hotbar save selection — A07 / GA03c

Status: COMPLETE from017b1050,9168→9211GREEN (+43). CoO-original
input/save integration repair; W6 and FLOW1–6 are complete. New-game save isolation
finishes before this wave. No save-schema change, new content, art or spell rule.

## Player contract

Saving and loading restores the selected occupied hotbar slot. Selection is validated
against the loaded actor: invalid/unoccupied indices fall to the first occupied slot;
no abilities means -1. An occupied ability on cooldown remains selected. Restore
updates input and rendered state immediately without casting, targeting or spending
energy/turns. A bootstrap with no input handler captures -1.

## Source verification / corrections before implementation

| Source | Verified contract / correction |
|---|---|
| GameBootstrap CaptureGameSessionState829/837 | Hardcodes selectedHotbarSlot0. Read validated selection from co-located InputHandler. |
| InputHandler EnsureHotbarSelectionValid4008 | Occupancy in0–9, first occupied fallback, else-1; cooldown is not validity. Reuse this contract. |
| InputHandler SyncHotbarState4023 | Restore assigns, validates and synchronizes renderer; no activation dispatch. |
| GameBootstrap ApplyLoadedGame841/990 | Restore only after presentation wiring replaces InputHandler.PlayerEntity with loaded actor. |
| InputHandler TryHandleHotbarInput1999 | Brackets select without casting. Number/click/Enter/manager activation also cast and cannot serve as selection-only native controls. |
| ActivatedAbilitiesPart AssignAbilityToSlot148 | AddAbility auto-binds first empty; explicitly assign distinct commands to sparse slots for ordering fixtures. |
| ActivatedAbilitiesPart MigrateLegacyAssignments240 | Entirely unbound nonempty lists migrate/rebind at load. Empty-loaded-hotbar controls need no abilities. |
| SaveSystem GameSessionState Save/Load | v7 already writes/reads SelectedHotbarSlot integer. No decoder clamp or format bump; old0 behaves as occupied0 or current fallback. |

Proposed API: input capture method validates/returns current selection; restore method
assigns requested index, validates and synchronizes. Bootstrap captures through its
co-located InputHandler (absence-1) and applies only after replacement presentation
wiring. Final names/source anchors rechecked before first production edit.

## TDD / verification plan

RED nonzero bootstrap capture, serialized load→immediate input/render restoration,
invalid/empty fallback, cooldown occupancy, no action cost. Decisive ordering fixture:
old actor slots{0}, loaded actor{5,9}, saved9. Validating against old actor first would
fall to0 then5; a single desired loaded slot could hide the ordering error.

Dedicated24-case matrix: valid0/5/9; invalid-1/-2/3/10/min/max; null actor/missing
part/empty list; cooldown; both actor replacement directions; removed ability;
same-slot different ability; all-unbound migration; absent input; pre-first-Update
capture; missing renderer; repeated restore; zero activation/targeting/energy/cooldown
mutation; v7 capture→load→capture. Classify already-correct controls honestly.

Native brackets select9, F5, change selection, F6; assert input/renderer restoration
and unchanged action costs. Include cooldown and empty cases using GA03b isolation.
No mouse/pixel/subjective-feel or speedup claim. Capture/restore event changes do not
add per-frame work beyond existing selection validation.

Fixture isolation: Bootstrap/Input on one owned GameObject; save/restore factories,
narrative references, clocks, save callbacks, pending effects and TurnManager.Active
for full ApplyLoadedGame. Preserve preexisting protected InputHandler/Bootstrap
changes and stage only this wave’s attributable hunks. Independent cold-eye, fixes,
native verification, full suite and living docs precede commit.

## Fixture/source sweep before RED

Input and renderer selection fields both initialize to-1; SetHotbarState only stores
selection/pending ability. Use one inactive owned GameObject for Bootstrap/Input/
ZoneRenderer to inspect state without Awake-created grids/cameras. Full ApplyLoadedGame
still exercises replacement ordering and real SetZone; temporarily untag existing
MainCameras and restore exact tags so no borrowed camera is changed.

Mark FarmingAccessGrant.GrantedProperty on fixture players to avoid unrelated starter
kit generation. Use actual loaded zone/turn aliases and waiting-for-input state.
Snapshot every global factory/reference written by ApplyLoadedGame, both resettable
clocks, MessageLog.TickProvider, conversation state/choice contents; null borrowed Speaker to avoid mutating its brain
flag, settlement/render hooks. Compose existing save fixture for serialized-state,
prefs/runtime/FX/log/reputation/turn restoration. All-unbound ability migration is a
separate compatibility control; do not mistake it for a truly empty hotbar.

Final public names: CaptureHotbarSelection() validates current selection and returns
it; RestoreHotbarSelection(int) validates against current PlayerEntity and updates
renderer state. Bootstrap restore follows WirePresentationForLoadedGame. Existing
pending ability, targeting and ability assignments are outside this selection repair.

Initial13RED:2pass/11fail,08:41:39UTC,.3366005s,0CS. Existing v7 serialization
and occupied slot0 capture pass; nonzero/empty capture and immediate loaded selection
fail, with explicit missing restore API assertions. Minimum implementation adds
validated capture/restore methods and wires bootstrap capture plus restoration only
after loaded presentation/player replacement. No decoder or ability change.

Minimum57/57GREEN,08:43:01UTC,.676201s,0CS. Independent fixture review found
wrong test-only Player tag (IsPlayer→Player) and a useful sparse-capture countercheck
({5,9}, selected9); both corrected before dedicated24-case adversarial run.

First adversarial81:80pass/1test-reflection failure at08:50:39–40UTC,.8511089s,
0CS. Corrected the renderer probe name to _pendingHotbarAbility (input uses
_pendingAbility). This was a fixture mistake, not a gameplay defect. Added six
player-flow cases after independent cold-eye review: selected/sibling unbinding,
unselected removal, overflow ability and occupied/empty initial checkpoint.

Native driver authoring first compilation missed the Rendering namespace for
InputHandler:6error CS log lines (2unique locations); no test result consumed.
Added the verified namespace, retained compiler log, then restarted verification.

Expanded87/87GREEN,08:53:53–54UTC,.9114803s,0CS. Dedicated30 includes six
post-review hypotheses; all pass after the renderer-field fixture correction.
Native first18/19: all gameplay selection/save/load checks pass; an incorrect fixture
assertion expected one Quick file but isolation deliberately owns marker+fresh saves.
Retained report/log; corrected to both exact owned paths, distinct IDs, count2, and
fresh ID absent from normal Saves. Runtime unchanged.

Intermediate native19/19PASS; retained before strengthening immediate per-bracket
cost/targeting checks (a later load could otherwise erase an accidental cost).
Independent final review has no remaining runtime finding; native final rerun follows.

Final native22/22PASS,runabe24cd7aa7546b592304ca771421898,3.7328543s
(shutdown3.7517576s),0CS. Exact owned marker/fresh paths verified; owned root
removed after exit. Per-bracket tick/energy/cooldown/targeting checks precede loads.
Full suite pending. No physical-input, rendered-pixel or subjective-feel claim.

## Close-out

Full9211/9211GREEN,08:58:16–09:00:34UTC,137.0933501s,0CS. Native22PASS;
2434unique Asset GUIDs/0collisions. Independent cold-eye and hypothesis gate complete;
no remaining🟡+. All test-only authoring/verification corrections and honesty bounds
are retained in GA03c-REPORT.md. Attributable InputHandler/Bootstrap edits only.
Files: InputHandler.cs; GameBootstrap.cs; GameAuditHotbarSelectionTests.cs;
GameAuditHotbarSelectionAdversarialTests.cs; GameAuditHotbarSaveBenchPlayer.cs;
GameAuditHotbarSaveBenchBatch.cs; their new metadata and accompanying docs/evidence.
Next: A08 decode isolation, followed by the remaining whole-game repairs.
