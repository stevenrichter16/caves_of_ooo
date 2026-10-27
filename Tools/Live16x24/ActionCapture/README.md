# Bounded ordinary-Play action capture

These `.cs` files are **method-body snippets for the Unity MCP execution tool**,
not compiled game components or autonomous gameplay. Run them in an isolated
ordinary Play session with the real Game view set to1280×800. They observe
existing sprites and hooks; they never issue actions, create enemies, slow time,
teleport actors or render a second camera. The parent/operator supplies normal
keyboard/controller input.

## Sequence

1. Run `arm.cs` after the normal flat world has loaded. It binds the actual
   `ZoneRenderer._entityVisualRenderer`, not an unrelated renderer found first.
   It requires the flat world, input handler and real1280×800 target. A second
   simultaneous arm is rejected.
2. Optionally run `observe-events.cs` to correlate actual attack/damage hooks
   with pose receipts. Subscriptions are additive, limited to128 recorded events.
   Its unique event filename is inside the active capture's session folder,
   or the verification directory when armed independently.
3. Perform ordinary actions. The coroutine captures only when the visible
   actor sprite/flip or displayed readout placement signature changes, after `WaitForEndOfFrame`, using
   `ScreenCapture.CaptureScreenshotAsTexture()`. The PNG is the actual completed
   frame including UI. Pixel textures remain in memory until finish.
4. Run `finish.cs` to stop observation, encode the original textures, export
   frame PNG/JSON plus `index.json`, and destroy the held textures. Read the
   returned errors and `indexSaved`; an export failure is not acceptance.
   Repeating finish returns its first result instead of overwriting the index
   with cleared buffers. Existing capture files are preserved.
5. Run `finish-events.cs` to detach **only this observer's** attack/damage
   delegates and export its unique event log. Repeating it is harmless. Do this
   even if no action occurred or capture timed out.
6. **Finish/export and unsubscribe events before stopping Play.** Reaching the
   automatic frame/time bound does not itself export retained pixels or remove
   the separately armed event observer. A domain reload/Play stop may discard
   unexported observations; it is not the cleanup procedure.

The current helper also records visible grouped readout owner/value/row/lane
from the real bound FX renderer in the same frame. That observational metadata
does not enqueue effects or manufacture hits. Older before frames predate this
field and remain unchanged.

The frame observer is finite: at most24 selected frames or300 seconds, with
one sample per Unity frame. The event list has its own128-entry cap. These are
small diagnostic bounds, not a generic recording framework. Each arm resets its
own finished-result marker so later sessions remain independent. Files are
written under `Docs/Verification/Live16x24/G2Action-<UTC timestamp>/`; raw prior
captures are never edited or resampled.

## Evidence limits

Selective pose sampling is **not continuous video**. It omits unchanged poses,
may miss a short transition during a stalled frame, and does not prove every
state recovers solely because the last sprite looks idle. Read sprite name,
state enum, time, identity, HP and pending-reaction fields together. Correlate
ordinary action logs/events where needed; never infer who attacked solely from
an action-shaped sprite.

Synchronous screen readback and metadata collection add overhead. PNG encoding
is deferred until finish, but these captures still **cannot measure normal
frame pacing, latency, FPS or Steam Deck performance**. They establish visible
frames and recorded states, not uninstrumented motion feel or audio timing.

An earlier raw `endFrameRendering`/GPU-readback prototype produced upside-down
or inappropriate-phase images and was rejected. It is not this accepted EOF
pipeline. Do not fix such raw captures by flipping pixels and presenting them
as original gameplay evidence.

## Helper regression evidence

A scratch .NET harness executed the actual finish/observe method bodies with
minimal Unity object/hook stubs. Before these fixes it reproduced three issues:
second finish overwrote the saved index, finish-before-arm threw, and two event
sessions reused one file; the foreign-callback preservation control passed.
Afterward all four checks passed, including PNG/index preservation and repeated
cleanup. Receipts: `Docs/Verification/Live16x24/g2-action-helper-red.txt` and
`g2-action-helper-green.txt`.
This is helper/control-flow evidence, **not a native Unity or gameplay test**.
Actual EOF captures remain the parent-operated Unity acceptance gate.


## Preserved East caster baseline observer (2026-10-27)

`arm-east-caster.cs`, `observe-east-caster.cs` and
`finish-east-caster-events.cs` are the bounded method-body variants used for
E1 in `Docs/GRAPHICS-EAST-CASTER-MOTION-PLAN.md`. They preserve the original
capture workflow and add actual selected static caster rows plus short50ms
cast sampling buckets. Static rows are observational metadata, not motion
admission. The cast observer listens additively to the existing resolved hook;
it does not emit it. Hidden source coordinates/identity are not disclosed merely
because a target-side effect is visible.

Use the normal `finish.cs` for pixel export AND the matching
`finish-east-caster-events.cs` to detach attack, damage AND cast observers.
The old `finish-events.cs` removes only its original two observers and is not
sufficient for this variant. The E1 seven-session receipts establish that this
variant ran in actual Unity; the second/third/fourth/etc folders with no event
log entries are movement samples, not casting proof. The final
`G2Action-20261027-032836-072` contains the genuine cast. No normal performance
measurement or new animation acceptance is implied. The planned animation
integration is now deferred by the user's worldwide sprite-coverage priority.
