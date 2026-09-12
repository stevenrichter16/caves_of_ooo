# Gameplay camera zoom

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
