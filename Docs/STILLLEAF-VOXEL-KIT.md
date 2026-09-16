# Stillleaf voxel kit

Status: complete and installed. Stillleaf art: 52 model variants passes final targeted and
full-suite checks. SC14 has 537/537 focused passes; SC17 has 12,825 passes and
exactly the 32 unchanged baseline failures, with zero C# errors and no new
failures. All 287 new tests pass. Eighteen native camera previews have complete
model coverage; live input feel and sustained FPS remain outside this evidence.
The aggregate implementation log and review are in
`CATHEDRAL-STILLLEAF-COMPOSITION-PLAN.md`.

## Identity and native contract

Stillleaf's intact archive is deliberate architecture inside an eroded chamber.
Long pale tepuibone courses, cloud-banded cool marble and dull vertical iron ribs
have separate silhouettes and restrained color pairs. Dark floors and shallow
shelves with tied bundles leave the enclosure quiet. The sealed door is a flush
iron panel in pale stone; unlocking the real owner replaces its visual barrier
with a low threshold. There is no second door entity or artificial art collider.

| Family / indices | Exact native aliases | Geometry and palette slots |
|---|---|---|
| `ground`, 0–3 | `SandstoneFloor`, `StoneFloor` | Quiet full-cell warm stone, slot 64. |
| `floor`, 4–7 | `SealedLibraryFloor` | Quiet full-cell dark floor, slot 12. Both floor families have ground metadata and vary only in buried thickness. |
| `tepuibone`, 8–11 | `LibraryTepuiboneWall` | Three broad joining courses, pale 19 and warm 79. |
| `marble`, 12–15 | `LibraryMemoryMarbleWall` | Three broad joining strata, one cloud band, cool 125 and neutral 35. |
| `iron`, 16–19 | `LibraryChoirIronWall` | Full-cell foundation and cap with two continuous vertical ribs, dull 12/13; seven boxes. |
| `door`, 20–23 | `SealedLibraryDoor` | Pale frame, dark flush panel and small pale keyway surround, 19/12. Local passage axis Z. |
| `open-door`, 24–27 | Same current `SealedLibraryDoor` owner, selected by native state | Low threshold, max height .16; two boxes, 19/12. No new blueprint alias. |
| `shelf`, 28–31 | `SealedArchiveShelf` | Shallow pale stone frame with two dark tied bundles and broad pale bindings, 19/15; nine boxes. |
| `bear`, 32–35 | `CaveBear` | Broad brown quadruped, projecting lighter muzzle, short paired ears and four paws, 8/11; ten boxes. |
| `slime`, 36–39 | `CaveSlime` | Low green pseudopod cluster, 16/60; four boxes. |
| `spring`, 40–43 | `ConvalescencePool` | Continuous flat cyan native liquid surface, slot 123; one box with a constant exposed top. |
| `boots`, 44–47 | `IronshodBoots` | Two separated leather shafts with iron toes and collars, 10/13; six boxes. |
| `wall`, 48–51 | `TepuiWall` | Two full-cell wind-cut pink-stone strata, 56/79, constant top 2.0; exact Stillleaf scope only. |

All 52 forms remain inside one cell in X/Z and use at most two constant palette
swatches. Full-cell wall foundations and constant tops avoid small repeated caps
or grooves through the sealed enclosure. Existing Stump/Ginmere/ring models cover
native approach rocks, mouth rim, descent ledges, expedition supplies and other
owners; the archive kit does not claim their blueprints.

## Verification sweep before production

| Source | Verified consequence / correction |
|---|---|
| `CLAUDE.md` and the two-area plan | Write and run RED before production; perform independent review and native camera checks after implementation. |
| `Objects.json`: three `Library*Wall` owners | Solid, non-takeable, explicitly indestructible native barriers. Stone/stone/metal material identities remain real gameplay Parts. No art destruction system is introduced. |
| `Objects.json`: `SealedLibraryDoor`; `SealedLibraryBarrierPart` | One actual owner with Lock key ID `coo.sealed-library.stillleaf`. Barrier closed state follows that lock. Successful native unlock also removes Physics.Solid. No open-door blueprint exists. |
| Root renderer state contract | Open door must be visibly low (<.3 cells) at the gameplay camera. Retaining a tall lintel was rejected before production because it would obscure the newly walkable cell. |
| `SealedLibraryBuilder` | Native enclosure protects stairs, stamped floor and excluded arrival area. Art must never introduce a gap into closed barriers or manufacture the missing quest key. |
| `Objects.json`: `SealedArchiveShelf` | Fixed examinable shelf with tied bundles and clay seals; no Container or Readable Part. Art does not invent loot or reading interactions. |
| `Objects.json`: four census additions | CaveBear/CaveSlime are native Creatures; the spring owns LiquidPool(convalessence, volume 80), Water material and Thermal; IronshodBoots is takeable Feet armor with AV 2. No decorative substitute changes these mechanics. |
| Native agent's actual stack | Surface uses TepuiStone/TepuiWall. Descent uses SandstoneFloor/SandstoneWall; late archive uses StoneFloor/SealedLibraryFloor. Alias both ordinary stone floors, reuse existing surface art. |

## Reproducible build and API

Run `CavesOfOoo.Editor.StillleafVoxelKitBuilder.Run` in isolated Unity. The only
asset output directory is `Assets/Resources/StillleafVoxel3D`, with the library
loaded at `StillleafVoxel3D/Library`. It updates existing mesh/prefab assets in
place to preserve GUIDs and destroys all temporary scene objects. It never saves
the user's scene or depends on manually placed presentation geometry.

`StillleafVoxelLibrary.ModelId(family, variant)` strictly returns
`stillleaf-<family>-<0–3>` and throws for unknown families or invalid variants.
The hyphen in `open-door` is part of the validated family. `Family` claims only
exact native blueprint names: the closed door alias stays `door`, while the
scoped runtime recipe selects `open-door` from the same owner's actual state.

```csharp
string openVisual = StillleafVoxelLibrary.ModelId("open-door", variant);
// Select only after inspecting the existing owner's lock/barrier state.
// Do not replace the native entity or replay a generation plan to infer state.
```

The library validates all IDs, shared mesh/material references, prefab structure,
identity transforms, bounds, triangle counts, metadata kind/path and absence of
rigs/clips/sockets before publishing its index. Both `ground` and `floor` have
`kind = "ground"`; every fixture and door state has `kind = "entity"`.

## Gates and self-review

The fixture tests exact aliases and near-name counterexamples, four variants per
family, constant floor tops, distinct wall palettes and full foundations. Pure
mesh ray tests establish closed-panel blockage, open passage clearance and a low
retained threshold as a positive control. Shelf tests require actual separated
bundle geometry inside a shallow frame. Source-preserving corruption cases
reject broken metadata, identities and references.

- ⚪ CoO-original visual implementation, not a Qud source-parity claim.
- ⚪ Native sealed walls and shelves remain intentionally indestructible. No
  archive key, new interaction or save migration is part of this art kit.
- 🧪 Numerical geometry checks do not establish live feel, sustained frame rate,
  or readable color separation after lighting. Parent-owned camera previews and
  native state/owner-removal integration tests remain required.

## Implementation log

- 2026-09-15: Verified owner names, lock/barrier state, material identities and
  native floor stamps. Wrote art fixture and source-preserving corruption cases.
- SC03: Parent confirmed actual missing `StillleafVoxelLibrary` compile RED from
  both saved new art fixtures before production.
- Implemented 32 source-authored models and strict library. The open door is a
  low threshold following the agreed renderer contract.
- SC05: Parent reported all initial art tests passing. Actual manager-generated
  owner coverage then failed on CaveBear, CaveSlime, ConvalescencePool and
  IronshodBoots. Added explicit alias/shape tests before the four additive source
  families, using that real missing-owner RED as authorization. The initial 32
  model identities and their geometry stay unchanged.
- Source-only audit: 68 models across both kits, maximum 240 vertices / ten boxes,
  maximum two swatches, all one-cell horizontal bounds. Six copied source metas
  match their templates except GUID and have no GUID collisions. Re-export and
  final asset checks are pending parent execution.

Files: `StillleafVoxelLibrary.cs`, `StillleafVoxelKitBuilder.cs`,
`StillleafVoxelKitTests.cs`, their `.meta` files, and this document. Parent export
owns generated `Resources/StillleafVoxel3D` assets and their validation receipts.

### SC07 native camera refinement proposal

The actual `WindcutThreshold-64` view shows fine repeating top grooves from the
shared Stump wall across broad native cliff masses. A Stillleaf-only `wall`
family, exact `TepuiWall` alias, will append indices 48–51. Two full-cell strata
at a constant 2.0-cell top, with muted lower pink-stone 56 and lighter upper 79,
remove those small repeated caps. Course height varies internally across four
forms; exposed top color and height remain constant. Existing Stump art and
its other areas remain untouched. Four geometry cases and the new exact alias
are written before production; actual parent RED is pending.

- SC08 actual bear muzzle RED: the old head face landed at floating-point
  z=.420000015 and was included by the projecting-muzzle test. Retracted the head
  front to .39, preserving its broad .40 width and the .28-wide muzzle reaching
  .49. This gives the muzzle clear physical projection and lets the existing
  strict geometry assertion inspect it without relaxing a threshold.

- SC09: Parent captured actual missing-wall alias, four slab geometry cases and
  expanded asset count RED, plus the expected transitional malformed-content
  case, with zero C# errors. Implemented the appended `wall` family as two
  full-cell strata at constant height 2.0; shared Stump source remains unchanged.
- The malformed-library removal control now uses the actual copied entry count
  minus one, so adding future families cannot temporarily make it a no-op.
- Final source-only audit covers 72 combined models (20 Cathedral + 52 Stillleaf),
  maximum 240 vertices and two swatches, all one-cell horizontal bounds. Native
  rebuild, exported art validation and camera review await parent SC10.

### SC10 static camera review and SC11 verification

Inspected `WindcutThreshold-64.png`, `WindcutThreshold-1729.png` and
`WindcutThreshold-729490642.png` in `Verification/VoxelWorld/SC10-refined-preview`,
comparing their cliff surfaces with SC07.

| Observed feature | Result at the existing gameplay camera |
|---|---|
| Broad cliff tops | The repeated small caps and horizontal groove pattern are gone in all three seeds. Tops form uninterrupted pale pink masses around the native clearing. |
| Footprint readability | Muted side strata and shadows distinguish raised blocking edges from the quieter pink floor. The outer outline, entrances and central native lip remain readable. |
| Color restraint | The walls preserve one consistent top color and a darker side course; the native formation provides variation through its outline rather than a checkerboard of small caps. |
| Reused bushes | The new native grouped bushes add green activity around the mouth. Their closely packed, repeated cell silhouettes still read as rows at this zoom; this is a minor aesthetic observation about shared foliage, not a missing mesh or a regression in the quiet wall. No foliage production is changed in this review. |

The `build-receipt.json` reports Unity exit 0 with no C# errors. Across all 18
preview rows, missing meshes and unmodeled owners both total zero. Parent SC11
reports **527/527 targeted checks passing**, including the final wall, bear,
liquid-surface, portable-item and library asset contracts.

🔵 Shared foliage repetition is an optional future art refinement; the requested
cliff simplification is visibly achieved in all three reviewed seeds.

🧪 Can verify: quieter wall surfaces, readable raised edges and the supplied
static native model coverage. Cannot verify: live movement feel, combat timing
or sustained frame rate. The full regression run is still pending. This pass
edited only this art document and the companion Cathedral art document.

## Final verification

SC17 confirms the exact baseline failure names and messages are unchanged.
SC13 rebuilt all 294 combined artifact/metadata files byte for byte. The final
GUID audit covers 5,735 Unity metadata files and finds no task collision.
`Verification/VoxelWorld/SC16-closeout/regression.json` records the comparison;
`SC10-refined-preview/index.html` contains all eighteen actual native renders.
Earlier pending statements in the implementation log describe those earlier
runs; this final verification supersedes them. New layout rules apply on fresh
native generation; existing cached/saved graphs are not rewritten.
