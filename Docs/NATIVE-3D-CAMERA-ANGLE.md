# Native3D camera angle

Status: closed at 56° after MC26 and the final native workload, cleanup, and image review, 2026-09-10. User requested successive 9°, 15° and 10° tilts from vertical,
so the final requested angle is56° down (34° from vertical). CoO-original,
no Qud parity claim. This changes the shared town/ring3D surface only.

## Verified design

The shared surface reads the borrowed XY camera without rewriting it. CameraFollow
supplies the border headroom described below; cursor, HUD, native cell coordinates
and current chunk stay registered. At height H and ground center(cx,cy), use
camera(cx,H,cy−H*cot56), rotation Euler(56,0,0). Rebuild its orthographic
projection each Sync, then multiply its vertical matrix coefficient m11 by
1/sin56. Every ground point still projects to the exact old XY pixel. A raised
point of height h shifts visually north by h*cot56 (≈0.674509h), exposing sides.
Never divide the previous compensated matrix again on repeated Sync.

Picking must follow the tilted projection. Its ray through legacy ground
point(gx,gy) is origin(gx,H,gy−H*cot56), direction(0,−sin56,cos56). This line
must match the actual compensated camera ViewportPointToRay. Native fallback
sprites stay on the registered ground grid and retain click priority on the
visible flat cell under the cursor. Mesh hits can land in a different physical
cell: use hit.point XZ for footprint/FOV/contact validation, not the flat input
cell. A hidden flat cell cannot reject a visible raised body part; a visible
far body cell cannot reveal an unseen physical hit. No-mesh fallback remains
at the ray's ground intersection, where existing native occupancy is checked.

## Gates and scope

Write/run RED before production. Existing fixtures cover town and ring resources.
Pin56° pose, whole-ground registration, viewport/zoom and repeated-Sync stability,
actual height parallax, raised-body contact, both FOV directions and native item
priority. A controlled taller registered collider exercises adjacent-cell parallax
without claiming new art or a live visual pass. Existing removal/material/identity
and borrowed-camera controls remain. Cold-eye review and native screenshot follow.
No new gameplay footprint, rotation, save migration or2D layout change is included.

Performance: constant camera matrix/ray arithmetic; no per-frame allocations or
all-owner cell scans. The shared surface leaves borrowed materials, cameras and UI
untouched; existing CameraFollow owns the explicit ready-scene framing changes.

MC14 had zero compiler errors. The five desired-angle/parallax/picking cases
failed before production; repeated camera/ground registration already passed.
At MC14 the shared surface compensated81° and both presenters validated physical
hit cells using the same tilted projection ray. Four older surface tests
retain every viewport/resource assertion and update only explicit overhead pose
expectations. Borrowed camera transforms, UI and native positions are untouched.

MC15 full suite: zero compiler errors; all six camera cases passed. Actual
camera/projection-ray equivalence, raised physical contact, both fog directions,
flat native fallback priority and repeated pan/zoom sync are verified. One
remaining older source-replacement test failed its explicit90° pose constant;
only that expectation was updated, preserving camera/root/target identity and
borrowed-resource assertions. Native visual/performance acceptance is pending.

## Native look-pass correction: ring border headroom

MC16 passed every new camera/render case. The actual native capture
`MCN-f8a9acbea29d442e9e75c6f04cc71aa6-south-entry.png` nevertheless exposes
the raised player head crossing the upper map viewport at native row0. The old
ring framing intentionally capped half-height at12.5 and rejected any margin;
ground alignment alone did not cover elevated art. Planned bounded correction:
cap ready-ring half-height at13 and center its fully visible25-row ground at12.5,
giving half a cell of top/bottom headroom. Preserve ordinary disabled/town
framing, horizontal clamps, native positions and UI viewport. New RED tests
project actual imported player vertices at rows0 and24, with a disabled-ring
counter-check. No production framing change precedes the RED receipt.

The native look-pass also found black stippling/triangular dark facets in fully
revealed rock geometry, absent in the Blender render. The shared shader has no
dither and already uses URP shadow bias/clamping. Subsequent palette/mesh checks and the controlled GPU02 diagnosis found no
proven rendering defect. See the art REVIEW.md for receipts and the distinction
between controlled white fog and actual native light; no speculative lighting
or mesh change followed.

## Intermediate requested angle:66 degrees

User subsequently asked for another15° of tilt, so the new desired pitch is66°
down (24° from vertical), retaining the same compensated projection and matching
picking-ray equations with66 substituted. Production remains81 until MC18 RED.
The imported ring-player bounds are1.73 units tall and.85 units deep before
placement. At66°, its height parallax is approximately.445229h; one cell of
vertical headroom is a conservative fit, verified against actual mesh vertices.
Plan ready-ring half-height cap13.5, with matching Y bounds halfHeight−1 and
26−halfHeight even at user zoom20. Ground-center12.5 stays fixed when all rows
fit. North/south and genuinely separate Look-target cases test actual player
geometry while a separate center owner supplies the ordinary tracked target.
Shared camera/picking tests now expect66 and the tall-body fallback test derives
the actual flat cell, which is two rows north at this greater tilt. The disabled
ring, borrowed camera/target/UI, native position and exact ground alignment
controls remain. GPU diagnosis is independent of this desired-angle RED gate.

MC17 reproduced the actual imported north-player clipping at81° (viewport Y1.00678253); the south envelope and disabled-render control separated geometric clipping from the old zoom-cap contract. MC18 recorded11 desired66/zoom/headroom failures with2 existing controls passing. The shared pitch is now66, ring cap13.5, with Y bounds halfHeight−1 and26−halfHeight. MC19 passed all13 camera/ring cases and55 older controls;8 new town cases failed before its production change (including actual north-body viewport Y1.00518179). The native town now retains center12.75/width-fit with minimum half-height13.25; flat-image Morrowfast is unchanged. Cardinal-facing tests use actual imported/skinned current vertices and the native facing hook; EditMode does not play the Attack animation.

MC20 passed every actual town body/cap/center assertion;8 new strict viewport-helper
comparisons failed. MC20b diagnostic established identical source/native clip-space
XY, with source letterbox height467.840027 pixels against integer native target468.
Unity's WorldToViewport helper differs by~0.00015 normalized units under this
fractional rectangle. Tests now require strict direct clip-space XY agreement
(1e-5) and separately bound the helper difference to less than half a native
pixel per axis; existing integer-rectangle registration tests retain their
original tolerance. No camera production change or unmeasured tolerance
relaxation follows this fixture correction. That fixture correction proceeded to the MC21 full sweep; the user then
requested a further tilt before a new native capture. The older ready-town zoom pin changes12.75→13.25; its source
center, aspect, disabled-mode and native-position assertions are preserved.

## Further requested tilt:56 degrees

User asked for another10° after66; the desired final angle is now56° down,
34° from vertical. Production remains66 until the new desired-angle RED run.
Independent actual FBX vertex measurements of ring-player and character-teal
(548 vertices each) put the worst cardinal north-row top at26.00187 for56°.
The planned ring margin1.5/cap14 and native-town minimum13.75 with center12.75
both fit to26.5, leaving approximately.498 cell above the base envelope.
Tests use the actual imported/skinned geometry; this is not an animated/equipped
body guarantee. The controlled cross-cell picking collider now tops out at2.5
units so its flat projection stays in the map at56°, retaining real contact/FOV
and native-sprite priority gates. Native ground/projection equations substitute56.

MC21's full run also exposed a stale test that equated a flat cursor row with
physical raised contact. Its actual imported collider ray now supplies the
expected occupied nonanchor cell. This revealed a real fog-selection bug:
after rejecting a hidden physical hit, both presenters could select the same
owner via its other visible flat cell. MC22/23 confirmed ring/town assertion RED.
Each presenter now remembers whether the flat owner had a registered hit and
permits same-owner flat fallback only on a genuine no-own-mesh ray. No allocation
is added; nearest valid other-owner hits and visible native sprite priority stay.
Real pipe corner controls preserve no-mesh selection and reject that same owner
when its flat cell is hidden. Actor bounds were not assumed to contain a gap.

MC24: zero compiler errors;32 controls passed and19 desired56° camera/framing cases failed. Both hidden-contact fixes and real no-mesh corner controls were GREEN before changing the camera. Production now uses56°, ring margin1.5/cap14 with matching smaller-zoom Y bounds, and native-town minimum13.75 centered12.75. Earlier explicit camera/zoom pins update to those values; all disabled-mode, width-fit, ownership, visibility, target and clip-space assertions remain. MC25 full regression and the final56° native look were the next gates.


## MC25 full regression and final review

MC25 completed with zero C# errors: 10,932 tests, 10,900 passed and 32 failed.
All 415 added tests passed, including the final 56° camera/headroom, physical
contact, hidden-hit fallback, native no-mesh corner, and disabled-mode controls.
The failures comprise the same 31 pre-existing baseline cases plus one obsolete
invalid-input fixture: a cursor at legacy (40,25) can now legitimately select
raised town geometry beyond the flat ground boundary. The fixture is being
corrected to use a point far outside the bounded scene, with a separate positive
and hidden-contact countercheck for raised geometry in the north gutter.
No production change is needed for that correction.

The final read-only source review found consistent 56° camera/ray signs,
projection reset before compensation, ring headroom at ordinary/Look/user zoom,
and native-town framing isolated from flat Morrowfast. Both pickers keep
physical FOV checks and same-owner rejected-hit protection while accepting
valid raised geometry beyond the flat ground rectangle. MC26 and final native
56° captures remain required before visual close-out. Imported cardinal envelope
tests establish the current mesh fit; they do not claim every animation or
equipment combination was sampled.


MC26 completed with zero C# errors and zero skips: **10,933 tests, 10,902
passed, 31 failed**. Those 31 failures exactly match the pre-existing baseline;
all 416 net added tests passed (417 new names, because one old test was renamed).
The stale invalid-point fixture now uses a far-outside coordinate while retaining
its NaN, infinity, other boundary, and stale-output controls. A separate native
town case proves raised visible geometry can be picked beyond the flat north
ground boundary and rejects the owner when its actual contact becomes hidden.
No production change accompanied this fixture correction. Final native 56°
workload/captures are running; visual acceptance remains pending.


The three actual final 56° captures from native run
`da0e5e856474438e96c7b6436b7cbd42` were independently inspected:
`south-entry`, `destructible-ridge`, and `restored-save` under
`Docs/Verification/MultiCellPilot`. The north-row player's hood is completely
inside the viewport with a small black margin above it; the earlier 81° crop
is absent. Central and side-facing players and the moved copper pipe are clear.
The map stays separate from the sidebar and hotbar. No new camera/framing/HUD
blocker was found in these stills. Native FOV black cells and the already
documented faceted/thin-grain art differences remain; the stills do not claim
reference parity or exhaustive animation coverage. Native workload/profile and cleanup receipts were subsequently received and
verified independently of this visual inspection.


## Final native acceptance

Run `da0e5e856474438e96c7b6436b7cbd42` completed all 26 native gameplay/workload
gates with zero failures or unexpected errors. Its four 20-second profile
segments retained actual native AI/FOV/health and ordinary paced walking; this
is bounded Editor evidence, not a production frame-rate guarantee. Cleanup
returned zero and restored scene, GameView, save-root, and user settings; the
wrapper reported no protected-file changes and zero C# errors. Independent
evidence validation passed all 315 checks. The final 56° visual review above
found no remaining camera/head-clipping/HUD blocker. This camera follow-up is
closed; art parity and exhaustive animation/equipment envelopes remain outside
that acceptance claim.

Receipts: [native run](Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-native.json),
[cleanup](Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-cleanup.json),
[independent evidence](Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-independent-evidence.json).
