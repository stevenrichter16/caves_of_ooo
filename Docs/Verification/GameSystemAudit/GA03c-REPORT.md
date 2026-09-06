# GA03c / A07 — hotbar selection across save/load

Status: COMPLETE; full9211/9211GREEN (+43). Baseline
017b1050,9168GREEN. CoO-original integration repair; no Qud parity claim.

## Result

Bootstrap saves the input handler's validated selected slot and restores it after
binding the loaded actor. Input and renderer selection synchronize immediately.
Occupied cooldown abilities remain selected; missing/invalid slots fall to the
first occupied slot; no available ability means-1. Capture validates input selection;
immediate renderer synchronization belongs to restore. Assignment, targeting and
action rules are unchanged. Existing v7 field needs no migration or schema bump.

## Evidence

| Gate | Raw result |
|---|---|
| Initial regression RED |13total,2pass/11fail;08:41:39UTC,.3366005s;0CS |
| Minimum + neighbors |57/57GREEN;08:43:01UTC,.676201s;0CS |
| First dedicated + neighbors |81total,80pass/1fixture reflection failure;08:50:39–40UTC,.8511089s;0CS |
| Native driver authoring compile |6error CS lines,2unique missing InputHandler namespace locations; retained log; no XML consumed |
| Expanded + hypotheses |87/87GREEN;08:53:53–54UTC,.9114803s;0CS |
| First native |18/19PASS;fa2d0cac1040408980061747014c6ee8;3.9063829s;0CS; wrong marker+fresh file-count assertion |
| Intermediate native |19/19PASS; retained before immediate per-bracket cost strengthening |
| Final native |22/22PASS;abe24cd7aa7546b592304ca771421898;3.7328543s; shutdown3.7517576s;0CS |
| Full suite |9211/9211GREEN;08:58:16–09:00:34UTC,137.0933501s;0CS |

43new tests:13initial regression/control +30dedicated adversarial/player-flow cases.
The initial RED establishes two broken integration paths (capture and restoration),
not eleven separate bugs. The dedicated sweep found no additional production defect;
its first failure was a test-only private-field name. Existing serializer, migration,
empty-input and binding behavior are honestly retained as controls.

## Cold-eye and hypothesis classification

- 🟡 Test precondition: IsPlayer was not TurnManager's Player tag. Corrected before
  expanded tests; initial waiting=true cases still exercised the intended load path.
- 🟡 Countercheck: singleton occupied-slot captures could hide “always choose first.”
  Added decisive sparse{5,9}/selected9 capture with unchanged bindings.
- 🔵 Test reflection: renderer stores _pendingHotbarAbility while input stores
  _pendingAbility. Corrected; retained failed XML. No runtime change.
- 🟡 Native isolation assertion expected one Quick save; helper deliberately owns a
  marker plus fresh character. Corrected to exact two paths/distinct IDs and absence
  of the fresh ID under normal Saves. The owned root is absent after native exit.
- 🟡 Native cost checks after F6 could hide an earlier accidental cost. Added checks
  immediately after every bracket pulse, before loading; final22PASS.
- ⚪ No remaining runtime finding from independent source/native review. Taxonomy,
  capture/restore symmetry, data shapes, ordering, counterchecks and doc drift reviewed.

Player-flow hypotheses now pin selected/sibling unbinding, selected/unselected
removal, same-slot replacement, unbound overflow, legacy rebinding, immediate recapture,
initial occupied/empty checkpoints, optional player/part/renderer, invalid indices,
repeated restoration and non-dispatch with an active event-listener positive control.
All already pass after minimum implementation. Save-decode partial-state behavior is
A08, separately planned; this wave does not claim failed-load atomicity.

## Native scope and honesty bounds

Can verify: real bootstrap N, queued InputSystem bracket/F5/F6 through InputHandler,
actual replacement actor, sparse-slot selection, cooldown7, input/render state fields,
turn/energy and no pending targeting, empty checkpoint restoration, exact isolated
save locations and normal shutdown. Ability seeding/removal are fixture setup, not
player-action evidence. Native observes after keyboard settling; EditMode establishes
immediate restoration. Final JSON includes the teardown assertion appended on exit.

Cannot verify: physical keyboard delivery, rendered pixels, subjective feel or mouse
input. No ability activation, effect appearance or performance speedup claim. New
capture/restore work runs at sparse save/load events; no new frame/turn loop.

## Attribution and files

Only this wave's incremental InputHandler and GameBootstrap hunks are staged over
protected files. Before copies are retained under /tmp/codex-ga03c-before. Other
animation/art edits remain outside the commit. New files are the two test files,
owned live driver/launcher and metadata; plan/report/raw evidence/daily logs accompany
the implementation. All Assets metadata audit includes ignored paths:2434unique,
0collisions. No content, sprite or blueprint edit in this wave.
