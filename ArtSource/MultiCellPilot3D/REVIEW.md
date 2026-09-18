# Multi-cell pilot art and presentation review

2026-09-10. CoO-original visual work; no claim of Qud implementation parity.
The reference is an art direction image. The native 80×25 zone and explicit
occupied-cell sets determine movement, targeting, visibility and destruction.

## Verification sweep corrections

| Earlier assumption | Evidence and correction |
| --- | --- |
| An object's vertices inside its footprint imply its complete geometry fits. | False for concave NE/SE shapes. Triangle interiors crossed unoccupied corners in eight ridge models. Exact per-cell-union triangle clipping now interpolates vertex attributes and preserves one exported owner mesh. |
| Surface winding of nested crag shoulders was correct. | Source edge-direction audit caught inconsistent rings in early art. Shared edges now have opposite orientations. |
| The existing MawToad blueprint already had native pixel art. | Blueprint and ring mesh existed; no creature PNG/mapping existed. Actual-model orthographic 16px icon now supplied with binary alpha and exact shared outline. CopperPipe, TarSeep and SteamVent received the same treatment. |
| Large scenery should use existing static batching. | Independent destruction and whole-owner picking require individual roots. All149 pilot owners use separate roots; only native ground is patched. |
| Static memory and actor visibility can use the anchor or bounds. | A hidden anchor can have visible occupied body cells; concave bounds can contain empty holes. Native occupied cells are authoritative. |
| A procedural repeating sinusoidal ground matched the reference. | Visual review rejected the repetitive waves. ImageGen supplied a dedicated flat mineral albedo with no props/shadows, consumed unchanged as a normal world-UV texture. |

## Art gates

The offline contract was written and run before model creation; original RED is
`reports/RED-contract.json`. The v8 bundle has51 model exports and149 placements.
All421 solid cells are excluded from the same flood that reaches all1579 free
cells. Source audit samples triangle interiors, validates winding and rejects
zero-area geometry. Actual Blender FBX reimport validates triangles, UVs,
material split, axis conversion, bounds, rigs and five animation clips.
All three v8 gates pass: `reports/layout-validation.json`,
`reports/source-interior-winding.json`, `reports/fbx-roundtrip.json`.
The art pipeline uses two CPU threads and creates no runtime Rigidbody.
V8 contains 163,838 unique model triangles and 331,778 placed-owner triangles,
plus 4,000 native per-cell ground triangles. The largest model is
PilotRidgeSE_2 at 10,145 triangles. These are asset counts, not a frame-rate claim.

V8 improves asymmetric crown planes, silhouette breakup, angular boulders,
ochre growth, palette warmth and organic ground grain. This remains a simpler
low-poly interpretation of the reference's highly sculpted rock microdetail.
The overhead and crop renders are Blender previews, not Unity gameplay proof.
Actual Unity v8 import passed all 51 models, retained borrowed asset hashes,
and recorded catalog SHA256 `4048fd26e2015d1ce29072c115ae7a0f235e02d836e6a13144a7f5ebfe3bb335`.
Receipt: `Docs/Verification/MultiCellPilot/unity-import-v8.json`.
Steam is a native mechanic; source preview does not fake a steam sprite into
an exported static object. Full live steam appearance is a separate native gate.

## Rendering TDD and review

Initial rendering gate MC07: 8 failures/1 pass (real missing library, owner
recipes, visibility, picking and world-UV behavior). After production and
actual v5 Unity import, MC08 and MC09: 9/9 rendering tests passed with zero
compiler errors. Receipt paths and full-suite counts are parent-owned in
`Docs/Verification/MultiCellPilot` and `Docs/MULTI-CELL-PILOT.md`.

The dedicated 12-case adversarial wave tests native owner transfers into an
adjacent ring chunk and already-bound town, owner/model mutation, body changes,
149 independent roots, native hidden-render state, per-cell fog with shared
materials, selection toggle behavior and material disposal. MC11 classified four portable transfer failures as confirmed bugs and the
other eight cases as passing regression controls, with zero compiler errors.
The shared resolver and both ring/town presenter lifecycles now preserve
portable owner art; MC12 passed all four portable cases and all eight controls. A further cold-eye hypothesis
tests native fallback item click priority on a portable body inside town.
MC13 full-suite actual RED confirmed this item-priority bug. The correction
checks native fallback priority on cells occupied by portable views and leaves
ordinary town selection unchanged; MC15 passed that test and its paired controls.

The same full run caught a test reflection adapter still invoking the old
three-argument recipe signature and three tests assuming fresh 3.7 was still
a fen. The adapter now supplies the pilot catalog; all old identity/unrelated-
zone assertions remain. Explicit real TendrilFen generation in supported 2.7
keeps the water tests meaningful, including temporary/permanent contrasts,
water with no pool entity, dirty patch isolation and actual saved erasure.

## Self-review

- 🟡 Fixed: concave triangle bridging, incorrect crag winding, absent native
  fallback sprites, anchor-only body visibility and picking returning anchors.
- 🟡 Fixed and MC12 GREEN after four actual RED cases: actors/objects
  retain their 3D body in adjacent ring chunks and an already-bound town.
- 🟡 MC13 confirmed, MC15 GREEN: native fallback item click priority
  when overlapping a portable body in the town presenter.
- 🔵 MC13 test-adapter/fixture repairs: reflection arity and three superseded
  fen generation premises; no gameplay contracts were weakened.
- 🟡 Fixed after native look-pass/actual RED: north-row player head clipped after
  tilt. Final56° uses ring1.5-cell headroom at normal/user zoom and Look, plus
  native-town minimum13.75. Actual imported cardinal envelopes now fit both borders.
- ⚪ Native look-pass difference: darker faceting/thin grain differs from Blender.
  Independent checks found intact imported palette UVs and geometry. Some authored
  grain fibres intersect stone; this is not proof of the native dark-pixel cause.
  Controlled GPU comparisons are recorded separately from the actual dim native
  light/FOV. No lighting or art change is justified solely by white-fog previews.
- ⚪ Documented: imported models retain fixed orientation, one unit per native
  cell and anchored body offsets. Animation is rig motion inside that body;
  no arbitrary root scale/rotation changes are allowed.
- ⚪ Documented: UV ground is mineral pigment only; all ridges, tar, props,
  mineral seams and creatures remain separate owned geometry. Ground UVs are
  `(worldX/80, worldZ/25)` on native per-cell quads, not one immutable backdrop.

## Reproduction

Use Blender's bundled Python; a separate system `bpy` install is unnecessary.

```
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/MultiCellPilot3D/build.py -- --render
python3 ArtSource/MultiCellPilot3D/validate.py
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/MultiCellPilot3D/audit_source.py
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/MultiCellPilot3D/validate_fbx.py
```

The art builder consumes the versioned ImageGen PNG unchanged. Its exact prompt,
source path and no-object/no-lighting intent are in `imagegen-ground-provenance.json`.
Runtime importer is `CavesOfOoo.Editor.MultiCellPilot3DAssetBuilder.BuildFromCommandLine`,
with `-multiCellPilot3dSource <bundle>` and `-multiCellPilot3dReport <receipt>`.
Run only in a parent-coordinated Unity window. Do not overwrite the source
bundle while Unity is importing it.

## Camera angle follow-up

User requested lowering the native camera10% from overhead. The shared surface
is now56° after subsequent15° and10° requests, with orthographic compensation that preserves every ground cell
and the borrowed2D camera/UI. Both presenters cast matching tilted rays and
validate physical contact/FOV separately from flat native sprite priority.
MC14:5 RED/1 control; MC15:6/6 GREEN, zero compiler errors.
See `Docs/NATIVE-3D-CAMERA-ANGLE.md`. Blender previews here remain strict overhead
art reviews; they are not screenshots of the newer native56° camera.

MC16 full regression:10,876/10,907 passed,31 exact pre-existing failures,
zero compiler errors; every390 added case passed. Native run
`f8a9acbea29d442e9e75c6f04cc71aa6` passed26 gameplay/workload gates plus cleanup;
independent evidence parser passed315 checks. The three real81° native PNGs under
`Docs/Verification/MultiCellPilot` were inspected. Native destruction/transfer/
save workload is exercised; final56° native visual acceptance remains pending.
Broad FOV black regions are expected native visibility.
The longer, simpler ridge crowns and sparse growth still diverge from the
reference's denser broken rock masses; previews do not prove reference parity.

## Standalone GPU diagnosis

The native81° look raised a concern about dark facets/thin grain. A controlled
actual-model probe used the native render surface and imported ridge/boulder,
with default renderer probes, explicitly supplied scene SH, shadows on/off,
unlit palette/white, and neutral SH positive controls at216/768 pixels high.
GPU01 (`visual-diagnostic-a121220db7e44176a16cbca2bc73fe6c`) captured20 frames
but its source scene was untitled, so it remains a provenance-limited control.
GPU02 (`visual-diagnostic-f5298fd4d26742f99693f3d3564a3cae`) explicitly loaded
`Assets/Scenes/Main/SampleScene.unity`, preserved/restored scene setup, and
captured20 frames without shader/compile errors or protected-asset changes.
Independent checks passed72 assertions; the20 images are byte-identical to
GPU01 and default/supplied-source SH pairs are identical. See that run's
`independent-review.md`, `independent-evidence.json` and reproduction script.

No controlled rendering defect was established, so no speculative lighting,
shader or mesh correction follows. The probe deliberately uses white native
fog/light while actual pilot daylight is approximately(.392,.372,.368) before
other lights, and the earlier native screenshot used81° versus this probe's66°.
These captures therefore cannot establish the cause of a brightness difference
with gameplay. Faceted crowns and thin authored grain remain stylistic/art
limitations relative to the reference. Final56° native readability is a separate
look-pass, not inferred from these standalone images.

## Final visibility/picking review

The stronger56° tilt request followed the66° full sweep. That sweep exposed a test assumption equating the flat cursor cell with a raised mesh contact. The test now obtains an actual imported-collider hit from the current camera ray and asserts its occupied, nonanchor cell. It then exposed a real same-owner fog bypass: a hidden physical mesh hit could be rejected and reselected through another visible flat cell of that owner. Ring and portable-town assertion RED preceded the fix. Both presenters now record any registered hit on the flat owner and permit that owner's native-cell fallback only when no such hit exists; valid other-owner hits remain possible. Real pipe no-mesh corners and hidden-flat controls preserve fallback functionality. MC24 passed these controls and the fixes before recording19 desired56° camera/framing failures. No new allocation, gameplay mutation or lighting/art change accompanies this fix.


MC25 full regression completed at the final 56° camera with zero C# errors:
10,932 tests, 10,900 passed and 32 failed. All 415 added cases passed. The
remaining excess over the exact 31-case pre-existing baseline is an old test
that calls legacy (40,25) an invalid input even though raised town geometry
can project there. Its test-only correction and explicit north-gutter visibility
countercheck precede MC26. The final read-only camera/picking review found no
new blocker; native 56° screenshots are still pending and no new art, shader,
or lighting change is claimed.


MC26 closes full regression at the final 56° camera: **10,933 total, 10,902
passed, 31 failed, zero C# errors, zero skips**. The 31 failures exactly match
the pre-existing baseline. All 416 net added tests passed (417 new names with
one rename). The corrected far-outside invalid input and new raised north-gutter
visible/hidden countercheck pass without production changes. Final native 56°
workload and image review remain pending.


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


## Closed acceptance

Final native run `da0e5e856474438e96c7b6436b7cbd42` passed all 26 gameplay/workload
gates with zero unexpected errors. The four 20-second native profile segments
completed; cleanup restored scenes, GameView, preferences, and private save
state and returned zero. The wrapper reported zero C# errors and no protected
file changes. Independent validation passed 315 evidence checks. Together with
MC26 (all 416 added tests passing; exactly 31 pre-existing failures) and the
three inspected final 56° images, this closes the bounded native art/rendering
integration and camera acceptance. No unverified lighting or mesh change was
made to achieve this result. Reference parity, broad-world performance, and
every possible animation/equipment envelope are not claimed.

Receipts: [native run](../../Docs/Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-native.json),
[cleanup](../../Docs/Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-cleanup.json),
[independent evidence](../../Docs/Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-independent-evidence.json).
