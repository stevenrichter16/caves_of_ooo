# Preserve player camp fixtures when moving

November24,2026. Correction accepted after final independent source/recorded-play
review0remainingP1/P2. This is a safety correction, not one of the
user's3-5targeted new capabilities. Armor recovery and box-stock work remain
proposals in [the worksite plan](../../../MACHINE-WORKSITE-RECLAMATION.md).
Current bounded [Mac6b5421a82](../MacCandidate6B5421A82/README.md) includes the correction; controlled packaged actions remain open.

## Cause and change

The retained1840save contains CampBox2150at41,10,14HP,25C,empty and solid. Later
2100/2150saves lack it. [Actual diagnostic trace](incident.json) kept the box
unchanged through a wait, two distillations, timber feeding and trap recovery/
re-laying/recovery. A west step to41,9 then south into the box destroyed it at1940:
existing InputHandler bump-to-break, not a processing pointer/custody defect.
The player's proofing2330and earlier proofing1840/2100/2150branches remain
separately preserved. This supplied proofing character has Strength70/maxHP400;
the diagnostic1940branch retains358HP, not the later343HP branch.

The9line guard inside the existing breakable-obstacle branch now freely refuses
a movement bump into an installed player-held fixture and explains C move,
take-up or deliberate break. Installation authority is the existing Player
marker plus Installed state, not the generic Owned tag. Explicit C Break and
ordinary generated/handed-over breakables retain their structural damage path.
No new Part, saved field, time policy, resource, damage, exposure or graphics.

## Native tests and actual play

Real InputHandler key REDc:10cases/5fail/5controls/0compiler errors. Four movement
directions and a legacy installed marker without Owned tag reproduced HP loss.
Controls retain generated/handed-over/loose/tag-only breaking and explicit break.
First two RED attempts had test-only GetActions/SpatialZone compiler errors and
are invalid. GREENa104/100pass/4failed new diagnostic assertions because the
movement channel was not enabled in the fixture; HP/turn checks already passed.
The test now explicitly enables that channel; existing fixture cleanup resets
diagnostics. Final GREENb104/104/0CS includes10new cases and94retained movement,
territory, door, destruction and held-fixture cases. Counts overlap earlier runs;
no full-suite acceptance follows. The original full regression remains failed.

[Actual play proof](play-proof.json): same supplied1840/41,9/358HP/50drams save.
Native south movement leaves tick1840, box14HP/25C/empty and actor41,9. Message
explains how to interact. C menu retains open/take-up/move/Break; choosing Open
on the empty box changes no time or box state. Native F5/all4files archived
before Finish. Full new Play/Continue1840 preserves the same selected box state;
another south bump is free and harmless. Fresh two-page field-note reader is
free at1840/1000energy/358HP. Fresh F5/all4files archived before further action.
Then the explicit native C Break countercase destroys the box,1840->1850;
its separate F5/all4files were archived before Finish. No claim the deliberately
destroyed countercase preserves the box or has a fresh1850reload. Use the
preserved fresh1840branch for later work, not the destructive countercase.

Two earlier after-play attempts lost their live bootstrap bindings around pending
imports and were excluded. Their preloaded1840four-file copies were unchanged,
verified/archived before exact-token Finish. Forced full import/compilation settle
and a clean scene preceded the successful retry. A first image write failed
because its output directory did not exist; directory created, real captures
then succeeded. No failed attempt is acceptance evidence. Main ended stopped,
rootnull,scene-clean,profiler-off,original7views/index0; scoped physical Unity
keyboard/mouse and cloned input settings restored after owned play.

Three actual screenshots, all visually inspected:

- [Blocked movement preserves the box](Screenshots/01-own-box-bump-preserved.png).
- [Deliberate break and other camp actions remain](Screenshots/02-deliberate-break-and-camp-actions.png).
- [Fresh box's free reader](Screenshots/03-fresh-box-reader.png).

Empty-box actual play and contents-preserving native cases are separate scopes.
No new acquisition, filled-box play, controller/FPS/Intel, whole-world equality
or complete-machine acceptance. All twelve machine dependency/status gates remain
open. Prior builds, foreign changes and normal saves/preferences remain protected.
