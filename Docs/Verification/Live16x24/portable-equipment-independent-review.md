# Portable equipment independent source review

Review time: 2026-10-27T04:14:15.649170+00:00. Reviewer: portable_equipment_final_review.

## Scope and result

Reviewed the four-identity travel extension: exact LongSword, LeatherArmor,
Hatchet and Cudgel retain accepted native art in current nonlocal graphs and
authorized subject views. No significant unresolved defect found in the current
bounded implementation after the footprint correction below. This is a source
review, not gameplay or packaged acceptance.

## Finding and verified correction

**P2, fixed in the new nonlocal path:** the first
`FlatPixelOpeningArtCatalog.TryResolvePortableLongSword` implementation reused
`LongSwordOwner` without the single-cell/current-registration guard used by the
other three equipment identities. A registered two-cell sword therefore repeated
one-cell art, and raw footprint mutation could retain native portrait art even
while the world rejected its stale spatial registration.

Root applied two reviewer-authored tests. The retained
`Docs/Verification/Live16x24/portable-equipment-footprint-red.xml` was read directly:
52 cases,50 passed,2 failed. Failures demonstrate exactly the multi-cell native
texture and stale-index native portrait problems, not fixture setup failure.

The current helper at `FlatPixelOpeningArtCatalog.cs:250–258` now checks owned
footprint, current registration when placed, and one zero-offset cell. It allows
legitimate carried/unplaced single-cell items without requiring a spatial zone.
Malformed geometry and foreign footprint ownership refuse native identity.
This matches incidental-equipment guard semantics. The correction is confined
to the new portable helper; existing local opening selection is unchanged.

## Other checks

- World selection still starts with the current-graph, legitimate occupant and
  visibility policy. Region admission is not expanded.
- The nonlocal incidental equipment path cannot have its successful tile
  clobbered by a later local adapter: those adapters remain null outside the
  admitted graphs. Local flow and mutually exclusive exact identity roles are
  preserved.
- The LongSword helper resolves only its own optional frame. It does not invoke
  the generic opening selector, so a Terrain/VisualID spoof cannot acquire a
  regional prop. Missing/malformed textures return false and retain glyph art.
- Subject selection remains downstream of `ExamineDocumentBuilder` custody and
  `ExaminePortraitRenderer` presentation-context validation. No new disclosure
  authority or world placement is introduced by the item art selector.
- Existing fog, moved-owner and stale-graph guards remain in the revised tests;
  only their intended current-other-region sprite expectations changed.
- The former service-counter failure was a reused stale world cell; the current
  test explicitly marks the replacement dirty and checks actual fallback
  owner/rect/pixels before comparing its reader. It does not weaken the regional
  exclusion assertion.
- New footprint tests cover valid single-cell positive controls, registered
  multi-cell world/reader fallback, carried geometry and ownership, actual stale
  registration, no read-time repair, and restoration by canonical registration.

## Pending evidence and limits

Root reports the combined222-case native job64b20035ad574287ada883ca2a1e3a89
running; reviewer did not execute or poll Unity and does not certify its result.
Root owns ordinary gameplay capture, inventory/equip/travel/save observations,
package validation and final Git preservation. No whole-world art, new biome kit
or chunk-fullness completion follows from this four-equipment change. Source-only
review cannot certify visual feel, actual screenshot correspondence or hardware
performance. Existing local LongSword footprint policy was deliberately not
broadened by this narrow correction; no claim that all local malformed-owner
cases are fixed.

## Reviewed source hashes

- `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs` SHA256 `b3b51ce8b54ebba03fa00ec4e6450a35954a331020565333a916f3698ba084dd`
- `Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs` SHA256 `18d3f179d9341bc1458b534eaf93dd6d434cb7bee60b582e8c6bc48b2705eca5`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGlobalPresentationTests.cs` SHA256 `8fbbf2a33f20777a929d14e52984e7c0eaefebdd3536c00cabbdcd0e126fd1a4`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelIncidentalEquipmentTests.cs` SHA256 `1cff1b70c1971953b4045ca09a09646c0248a953bba3173002f5358caea1ee79`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelLongSwordAdversarialTests.cs` SHA256 `bd1e481d8e4148f42f4c34ee460f4e9911ccccabe58d79dd74a5802891da9862`

Reviewer operations: read source/XML; wrote only requested scratch test patch and this scratch review. No Unity, Git or project-file edits.
