# GA03a — camera-safe effect cancellation

Status: APPLIED AND VERIFIED; full9097/9097GREEN. Baseline0b7311fc,
9066GREEN. CoO-original Unity-lifetime repair; no new spell content/art or gameplay.

## Repair and integration boundary

The Awake-bound camera accent used CLR null-conditional access to a cached Unity
Camera. A destroyed native Camera retains a non-null managed wrapper, so GetComponent
threw. Cancellation calls the zero-shake accent before cancelling playback and clearing
renderers/queues; the exception interrupted all remaining cleanup. Explicit Unity
null checks on the Camera and CameraFollow now skip destroyed objects while preserving
normal live-camera Shake(0,0). An accented visible cast also remains Playing instead
of becoming a caught-and-cancelled presentation failure when its camera disappears.

**The runtime guard is applied in the current working tree.** The surrounding FX
coordinator/binding is protected preexisting animation work absent from HEAD. Staging
the modified renderer against HEAD would adopt that unrelated work. The commit therefore
retains `GA03a-camera-guard.patch` plus before/after SHA256 in the state JSON, with tests,
native tools and evidence; it deliberately does not stage ZoneRenderer.cs. Reverse
application check proves the exact patch is already applied. This is not a claim that
HEAD alone includes the pending animation integration; the tested current workspace
contains the fix and needs no user action to use it.

## Verification

Every headless run restarts/settles MCP before Unity and checks compiler errors before
reading fresh XML. Actual destroyed references assert Unity-null and managed-non-null.

| Archive | Total/pass/fail | UTC and duration |
|---|---|---|
|GA03a-red.xml.gz|9/3/6|2026-09-06 07:25:17Z – 2026-09-06 07:25:17Z; 0.2661904s|
|GA03a-minimum-green.xml.gz|26/26/0|2026-09-06 07:27:00Z – 2026-09-06 07:27:01Z; 0.3829127s|
|GA03a-adversarial-green.xml.gz|46/46/0|2026-09-06 07:28:46Z – 2026-09-06 07:28:47Z; 0.5544415s|
|GA03a-full-known-flake.xml.gz|9095/9094/1|2026-09-06 07:35:28Z – 2026-09-06 07:37:45Z; 136.9948307s|
|GA03a-full-known-flake-repeat.xml.gz|9095/9094/1|2026-09-06 07:40:38Z – 2026-09-06 07:42:53Z; 134.3114057s|
|GA03a-fungal-precondition-controls.xml.gz|2/2/0|2026-09-06 07:47:07Z – 2026-09-06 07:47:07Z; 0.1780229s|
|GA03a-stabilized-focused.xml.gz|96/96/0|2026-09-06 07:48:23Z – 2026-09-06 07:48:23Z; 0.5604035s|
|GA03a-full.xml.gz|9097/9097/0|2026-09-06 07:53:39Z – 2026-09-06 07:55:56Z; 136.9263425s|

All tabulated0compilererrors. Nine initial cases: live/destroyed-component/destroyed-
GameObject × direct cancel/OnDisable body/OnDestroy body. Six destroyed-camera cases
failed before the guard; three live controls passed. EditMode lifecycle bodies are
explicitly invoked, not represented as automatic Unity callbacks.

Dedicated20cases cover ordinary null, missing Follow, inactive/disabled cameras,
zone/hide/Off/sprite transitions, hard timeout, idle/queued-only work, repeat cancel/
dispose, real owned materials vs unrelated material, own vs replacement hooks, and
positive intensity cast with live/destroyed cameras. Real active particles/playback
and both nonempty queues are asserted before cancellation, then cleared afterward.
Live controls seed positive shake before requiring both intensity/duration zero.

## Cold-eye and isolation

Independent review found no further runtime defect in the narrow guard. Two test
isolation findings were fixed: preserve all5EntityVisualHooks overwritten by enabled
actor rendering in Awake; inspect the3actual Awake-owned materials rather than
replace/orphan them. Fixture teardown neutralizes the callback only after assertions
so RED cleanup does not contaminate neighboring tests; it restores settings/render
and entity-visual hooks and destroys only captured fixture roots/cameras.

Hypotheses include destruction between creation/cancel, queued-only shutdown, repeated
cleanup with fresh work, hidden/Off masking active work, accented Play silently
catching a failure, owned cleanup skipped after failed Dispose, and accidental clearing
of another renderer's hooks/resources. Counterchecks cover live/present/inactive
participants, unrelated resources, and actual state counts rather than just no throw.
No Qud-parity reference is claimed; the source contract is Unity object lifetime and
the game's existing coordinator cancellation sequence.

## Native engine-lifetime evidence

Run20732597b43b427db55ca1f93d06f49a: **44/44PASS**,.121291375s,
exit0,0compilererrors,0MissingReferenceException lines and0caught presentation-failure
warnings in the complete process log. The short duration reflects a focused lifecycle
matrix with unrelated frame rendering disabled; it is not a performance benchmark.

The standalone empty-scene launcher creates no game/session/save binding. Actual Unity
Awake caches the test camera. Real deferred Destroy creates the stale wrapper; actual
enabled transitions dispatch OnDisable; real component destruction dispatches
OnDestroy. Assertions occur before cleanup substitutes any callback. Nine lifetime
combinations seed active playback/particles plus both pending buses; two accented-cast
controls prove live shake and destroyed-camera continued playback. The driver waits
through native destruction before reporting, and raw process log is inspected after exit.

Can verify: real Unity lifetime callbacks, destroyed-wrapper behavior, cancelled active
work, drained queues, live shake reset, cleared owned hooks and no matching native
exceptions/warnings. Cannot verify: gameplay/keyboard/mouse delivery, rendered pixels,
visual quality, subjective smoothness or a performance speedup. No75second frame
claim applies to these two constant-time event callback guards. Settings/static hooks
are restored and disposable scene objects are cleaned up.

2424unique Asset GUIDs,0collisions. Runtime changes are one exact hunk in the protected
working file; tests/native tools are new owned files. No preexisting spell/art file is
adopted into the evidence commit. Full suite9097/9097GREEN;31new cases relative to9066baseline (29camera+2fungal test controls).

## Verification correction: previously flaky self-exposure test

Two full runs failed only the recorded fungal self-spore test. GasSystem disperses
unstable clouds before per-turn exposure. A30-density cloud can leave the host before
the self-immunity check; absence of its diagnostic is then correct. Two deterministic
spread/no-spread controls passed before correcting the original test. The original
now fixes its exposure precondition, requires the same infection instance and exact
clock20, releases its event and restores the same global RNG object in finally.

No gas or infection runtime changed, no test was skipped, and no lucky seed was chosen.
Independent source review confirmed both branches. Focused96/96 and full9097/9097
passed with0compilererrors after stabilization. This is a test-reliability improvement,
not a newly fixed infection mechanic. Both earlier full failure archives are retained.

Self-review: 🟡 destroyed-camera interrupted cleanup fixed in the applied guard;
🟡 fixture global hooks/material ownership corrected; 🟡 random exposure precondition
stabilized with explicit countercheck. ⚪ surrounding animation integration remains
protected and unstaged; exact applied patch retained. 🧪 visual/physical-input/feel
claims remain outside these lifetime checks. Next wave: GA03b new-game save isolation.
