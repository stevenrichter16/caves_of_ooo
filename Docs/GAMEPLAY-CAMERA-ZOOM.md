# Gameplay camera zoom

## Current setting: 20% wider than original

Latest user revision, 2026-09-12: Main SampleScene multiplier is **1.2**.
Read-only live verification confirmed configured/applied 1.2, source and voxel
camera sizes 16.8000011 (original 14), pitch 56 degrees, voxel presentation true
and full reveal true. Only the serialized setting changed. Self-review and live
numeric verification passed; no additional code or tests were needed. Unity is
playing. These checks verify camera configuration, not subjective play feel.

## Previous setting: 50% wider than original

User revision, 2026-09-12: Main SampleScene now uses multiplier **1.5** instead
of 2. Read-only live verification after scene reload confirmed configured and
applied values 1.5, source and voxel camera sizes 21 (original 14), pitch 56
degrees, voxel presentation true and full reveal true. Only the serialized
setting changed; the 52-test implementation verification below still applies.
Self-review: one setting, no code changes; live numeric verification passed.
Can verify camera span and rendering flags; player preference remains subjective.
Unity remains playing. The following section records the original implementation.

## Original implementation

Status: installed and live verified, 2026-09-12. CoO-original presentation setting;
no Qud parity claim. User requested twice the previous zoom distance.

Main SampleScene sets GameBootstrap.GameplayZoomMultiplier to 2. Bootstrap passes
it to CameraFollow during initial setup and save loading. CameraFollow applies
the multiplier after native scene fitting, once per frame, before bounds and
target framing. Other scenes and isolated fixtures default to 1. Popup/UI fits
keep their existing scale. The native voxel render camera copies this result.

Verification sweep correction: changing the serialized Camera orthographicSize
alone cannot persist because CameraFollow overwrites it each frame. Multiplying
before native scene fitting would likewise be overwritten. No camera pitch or
HUD layout setting was edited.

Self-review: cold-eye inspection covered both special scene and ordinary/ring
branches, initial/load wiring, per-frame reset (no cumulative zoom), bounds and
UI isolation. No notable findings. This reversible configuration adjustment uses
existing regression coverage and live measurement; no new RED test was authored.
No new state machine, parser, actor interaction or save migration was introduced.

Validation: Z01-double-zoom-camera passed 52/52 existing camera tests with zero
C# compiler errors. Read-only live measurement in Overworld.2.6.0 confirmed
configured/applied multipliers 2, source and voxel orthographic sizes 28 (previous
native fit 14), pitch 56 degrees, 3D enabled, full reveal true, voxel presentation
active and no presenter failure. Visually inspected the Unity Game view.

Can verify: doubled camera span, active voxel rendering, whole chunk visible,
unchanged pitch and HUD scale. Cannot infer user preference or extended play feel.
The camera now shows space beyond the loaded chunk, which appears black; this
change does not add simultaneous neighboring-chunk rendering. Unity remains in
Play mode for the user.

Files changed: CameraFollow.cs, GameBootstrap.cs, Main/SampleScene.unity. These
existing files have mixed prior edits, so the exact installed delta is recorded
in Verification/VoxelWorld/Z02-double-zoom-live/implementation.patch rather than
staging unrelated work. Live result is alongside it; test receipt is in Z01.
