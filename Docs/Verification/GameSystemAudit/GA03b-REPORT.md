# GA03b — new-game checkpoints and isolated save binding

Status: COMPLETE; full9168/9168GREEN, native61/61PASS, compatibility33/33PASS.
Baseline7c7d87a7,9097GREEN. CoO-original save/input repair; no format/content/art change.

## Player-visible result

New Game now chooses the freshly generated character’s save identity before capture
and attempts an initial Quick checkpoint. F6, pause Load and death-L address that
character immediately. If capture or writing fails, the fresh world starts with a
clear F5 retry message and retains its own load binding. Continue keeps its previous
save and same-frame priority; failed Continue stays on the menu.

RegisterRuntime receives an independently known fresh ID from bootstrap without
changing the previous Continue binding. BeginNewGame captures once, rejects a
mismatched identity, supplies blank identity from its chosen ID, and restores that
binding/releases its recursion guard in finally. No-prior-save bootstrap uses the
same operation. Shared captured-state writing retains ordinary SaveSlot behavior.

False means checkpoint failure, not failure to start the fresh world. Save and
metadata are separately atomic; metadata refusal may leave valid fresh data while
the previous preference remains. Next-process preference persistence is only claimed
after a successful checkpoint/F5. Ordinary F5 trusts its capture identity as before;
a permanently wrong future callback and A08 decode/apply atomicity are outside scope.

## Verification

Every run settles MCP before Unity and checks compiler errors before fresh XML.
All tabulated runs have0compilererrors.

| Archive | Total/pass/fail | UTC and duration |
|---|---|---|
|GA03b-red.xml.gz|18/2/16|2026-09-06 08:01:48Z – 2026-09-06 08:01:49Z; 0.2782619s|
|GA03b-minimum-green.xml.gz|85/85/0|2026-09-06 08:04:14Z – 2026-09-06 08:04:14Z; 0.7823219s|
|GA03b-adversarial-first.xml.gz|53/52/1|2026-09-06 08:09:17Z – 2026-09-06 08:09:17Z; 0.6014114s|
|GA03b-isolation-red.xml.gz|47/35/12|2026-09-06 08:11:09Z – 2026-09-06 08:11:09Z; 0.5869419s|
|GA03b-expanded-green.xml.gz|132/132/0|2026-09-06 08:14:12Z – 2026-09-06 08:14:13Z; 1.1044306s|
|GA03b-cold-eye-red.xml.gz|53/51/2|2026-09-06 08:17:08Z – 2026-09-06 08:17:09Z; 0.6032626s|
|GA03b-cold-eye-green.xml.gz|138/138/0|2026-09-06 08:18:38Z – 2026-09-06 08:18:39Z; 1.0939924s|
|GA03b-full.xml.gz|9168/9168/0|2026-09-06 08:31:11Z – 2026-09-06 08:33:27Z; 135.5073505s|

Initial18tests use real serialized old/fresh states and the production adapter;
2Continue controls passed and16failed (observed stale binding/missing checkpoint
and explicit missing-operation/root boundaries). Dedicated35 then reproduced a
recursive initial-capture bug; the operation guard fixed it. Four direct retry
counterchecks raise dedicated service coverage to39. They also cover real loaded
world aliases, sparse failure boundaries, Primary/metadata/discovery and exact
fixture restoration.71new tests total=18regression+39dedicated service+14helper.

The fresh fixture snapshots runtime callbacks/IDs/guard/root, preference presence
and value, message entries/announcements/counters, reputation, turn singleton and
pending effects. Old Quick/Primary data, metadata and backups are byte-compared.
Review fixed partial-constructor cleanup and an owned file blocker missed by
directory-only cleanup before minimum implementation; the RED blocker was removed.

## Native audit isolation and review fixes

An initial N checkpoint writes a new GUID directory, so previous marker-only
launchers could no longer isolate their writes. SaveRootOverride now covers every
implicit read/write/metadata/discovery path. The shared Editor helper owns a token-
derived temporary root and original preference snapshot independently of launcher
completion. It rejects concurrent/stale owners, reapplies the same root after static
reset, and keeps isolation until normal Play teardown completes. Preferences are
restored/flushed and only its owned root is deleted before requested exit.

All21existing launchers use this helper; modes/deadlines remain intact. The old4
now also unsubscribe on manual stop. Three pre-N save suppressions were removed.
Independent review found and RED-tested2helper defects: thrown manual-stop callbacks
skipped cleanup, and external quit exposed the real root before remaining teardown.
The former logs failure and completes cleanup; the latter unregisters saving and
retains the disposable destination for the remainder of process shutdown.

Helper14checks include preference/root variants, concurrent/stale owners, repeated
restore/finish, failed launch, sibling preservation, invalid marker, manual callback
failure and external quit. These are direct helper/callback tests; explicit restore
simulates static reset and does not claim an actual Editor domain reload.

## Native keyboard and completed-shutdown evidence

| Mode | Run | Checks | Audit / through shutdown elapsed |
|---|---|---|---|
|new|c3329cbd2b3241958b992823a44e2177|15/15PASS|3.9724785s / 3.9903329s|
|continue|14d15b5c478f40528b156670deb6eca6|13/13PASS|2.4010979999999997s / 2.4222085s|
|empty|58b9339f504146ee98eacd9a38ab012c|13/13PASS|2.9071024999999997s / 2.92792s|
|failure|e118666fd16b40c884acb00c93c49376|20/20PASS|4.3400438s / 4.3597263s|

All4exit0,0compilererrors; **61/61PASS**. Each raw report observes actual driver
OnDestroy: the disposable root remains selected and saving is unregistered during
normal completed shutdown. Process logs show cleanup completed successfully and
post-exit filesystem checks confirm each owned root was removed. Exact old/fresh
IDs are absent under the normal save root. Old save/backups remain byte-identical.

Seeding happens at the real OnAfterBootstrap seam, before bootstrap registers its
real callbacks. Old marker111 is serialized before the shared player becomes222.
Queued N/C/F5/F6/pause keyboard Load go through actual InputHandler. Marker mutation
before reload proves the loaded checkpoint replaces current state. Failure mode
explicitly injects a null capture, verifies N/F6 cannot select the old character,
then restores callbacks and recovers through F5. Empty mode starts with an actually
empty isolated root and no boot menu. Reflection observes bindings; fixture seeding
and the capture fault are separate from keyboard evidence.

Preliminary New/Continue reports are retained. Review corrected a tautological
directory assertion and failure-mode checkpoint wording. Intermediate unity-clock
reports retain valid state checks but invalid shutdown elapsed values: Unity’s
realtime resets at Play exit. Final reports use monotonic Stopwatch and assert
nonnegative audit duration and shutdown duration at least as large.

Can verify: native bootstrap branch wiring, queued keyboard dispatch, serialized
character/world identity, F5 recovery, old-file preservation, normal Play teardown
isolation and final owned-root cleanup. Cannot verify: physical keyboard/mouse
ingestion, rendered pixels, subjective feel, performance speedup, an actual Editor
domain reload, or actual manual-stop/externally forced-quit lifecycle ordering.
Death-L is covered by controller/service graph tests, not these native inputs.
No75second frame/performance claim applies to this startup/explicit-I/O repair.

## Commit boundary and next work

Only attributable SaveSystem and GameBootstrap patches are staged; both apply
independently against HEAD. The surrounding preexisting animation/save changes stay
unstaged. Other runtime/controller/tests/helpers are owned whole-file changes.
No blueprint JSON, sprite asset or save-version change. A31’s applied shared-renderer
guard remains preserved as recorded in GA03a.

Compatibility: Craft Receipt25/25PASS,7.88173625s, runc229329d5cac44a8b0846277899b090e;
Founding Village8/8PASS, run09f71f9f1e5c4978aff95f667d9d5e0a. Both0CS/exit0.
Compatibility raws are separate GA03b files; original earlier report bytes
were restored. No screenshot was produced in the headless compatibility run. The common launcher and older W6 launcher both complete with the
new initial save/isolation flow. Final source review0remaining🟡+.2430GUIDs/0collisions.

Final full9168/9168GREEN (+71),0compilererrors. Next prepared wave: A07hotbar selection
capture/restoration, followed by the remaining actor/world/material/editor audit.
