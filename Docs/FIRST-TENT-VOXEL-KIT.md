# First Tent voxel kit

Status: complete and installed for fresh native generation. TC17 passes280/280 targeted checks. TC20 full suite: 14,042 passed,32 unchanged baseline failures,0 C# errors and no new failures. All280 added cases pass. TC21 reproduces all166 final asset/metadata files byte for byte; TC22 confirms installation and no task GUID collisions. CoO-original composition and art; no Qud-parity or live-FPS claim.

## Purpose and art direction

The First Tent is a monument to a mortal choice. A broad empty court, dark guest-cloths and low goat-hair shelter thresholds communicate hospitality without introducing a temple, divine figure, shrine fixture or sanctuary aura. The five poles and two human hosts remain separate native owners. This is CoO-original art, with the user's Qud-inspired coarse voxel direction; no source-level Qud parity is claimed.

The kit uses quiet sand, muted earth approaches and charcoal fabric. Only the one-cell hospitality cloth rises above two cells in height; its horizontal footprint stays inside its own cell. The existing gameplay camera and full-reveal setting are unchanged.

## Verified native contracts and corrections

| Source read | Verified behavior and resulting art decision |
|---|---|
| `CLAUDE.md`; parent composition plan | Test before production, actual RED, separate native/static verification and a source-level review. Root owns Unity execution, shared rendering and final gates. |
| `Lore/Factions/07_TentRight.md:118`; `LandmarkBuilder.FirstTentMonument` | Monument to a choice, explicitly no god. The original stamp creates four guest-cloth poles and a keeper; the ordinary camp adds one pole and one host. There is no `FirstTentMonument` blueprint to model. |
| `Objects.json` / `GuestClothPole` | A dark cloth on a straight pole. Nonsolid examinable PhysicalObject; no added Destructible, light or oath Part. Its description references hospitality, but native host conversation supplies the policy. |
| `Objects.json` / `TentWall` | Woven goat-hair Cloth, solid, destructible HP8, thermal and flammable. The low art is a gameplay cutaway of the existing wall; it does not change collision or provide a roof. |
| `Objects.json` / `TentRightHost` | Human Creature, canonical `@`, TentRightHost_1 conversation, LeatherBoots/LeatherCap and carried Dagger. A cap and open hand suggest the keeper's role; no attached altar or new item owner. |
| `Objects.json` / `SaltMaster`; Wellmeet kit | Native PaleSalt exchange and its existing four models are reused. No reason to duplicate the service or invent an extra ritual. |
| `FirstTentCompositionPlan` native-agent contract | Outdoor Sand, actual RoadStone approach and sheltered StoneFloor. The new kit owns Sand/RoadStone/TentWall identity; existing Wellmeet supplies StoneFloor and ordinary furnishing/services. |
| Existing Wellmeet corner topology | A corner joins local +X/+Z neighbors from the owner center. It needs half-cell arms, not a full-cell slab in both axes. The initial new geometry test's excessive total-depth requirement was corrected before generation; the actual open-quadrant ray countercontrol remains. |
| `LandmarkBuilder.LastCounterPost` / TC01 census | `chest:CampGoodsT1` creates an ordinary Chest with populated contents, not a `ChestCampGoodsT1` blueprint. Existing chest art remains authoritative. |

## Family contract

All IDs are `firsttent-<family>-<variant>` for variant 0–3. `Family` accepts exact blueprint strings only; `ModelId` rejects invalid families and variants. `corner` has no direct alias: the renderer chooses it only from the actual current TentWall neighborhood.

| Family / native alias | Geometry | Palette slots | Boxes |
|---|---|---|---:|
| ground / Sand | Full-cell continuous plane, top0. Buried thickness varies. | 64 | 1 |
| path / RoadStone | Full-cell earth/old-stone approach, same visible top across variants. | 106 | 1 |
| tent / TentWall | Low dark cloth panel across local X, coarse beam and offset pole. Height1.10. | 12,64 | 3 |
| corner / contextual TentWall | Two half-cell panels and beams meet at one post, opening left inside the room. Height1.10. | 12,64 | 5 |
| cloth / GuestClothPole | Straight 2.20-high pole, one broad unmarked dark hanging cloth, horizontal crossarm. | 12,32 | 3 |
| host / TentRightHost | Separate legs, dark coat/cap, warm face, forward open receiving hand. Height1.555. | 12,32 | 10 |

The maximum is 240 vertices, two swatches, one combined mesh and one renderer per owner. Geometry has no colliders, scripts, lights, rig, sockets or clips. Native owner membership, visual state, interaction, damage and removal remain authoritative. Actor-facing and tent rotation are supplied by root's existing presenter integration; the mesh adds no gameplay components.

## Reproducible implementation

`CavesOfOoo.Editor.FirstTentVoxelKitBuilder.Run()` builds `Assets/Resources/FirstTentVoxel3D`. It expands box geometry into a single shared-material mesh per variant, saves Mesh assets and prefabs in place, preserves existing GUIDs on rebuild, then validates exact metadata/reference consistency. It does not create a scene or alter manually authored objects. Runtime `FirstTentVoxelKitLibrary` exposes `Load`, `Find`, `Validate`, `Family`, and `ModelId` with the same strict contract as the completed neighboring kits.

Source audit helper: `/tmp/audit_threshold_camps_art_source.py`; report: `/tmp/threshold-camps-art-source-audit.json`. The report enumerates all 40 models, source SHA256 hashes and all166 expected generated paths. Source metadata are copies with only GUID changed; the audit checks uniqueness against all existing Assets metadata.

## Verification and in-phase review

- TC05: root captured 72 C# lines identifying the two missing kit classes, before either production class existed. No stale XML was used.
- Source anatomy assertions preceded geometry: real separate legs, real open room quadrant, broad elevated dark cloth with no filled base, and constant visible ground across variants. Imported references, budgets, exact aliases, strict IDs and 13 corruption controls accompany these.
- Source audit: 40 combined models across both kits, max240 vertices, max2 swatches, six unique copied source metas, no one-cell overhang or nonpositive dimensions. No existing asset or source was edited by this kit author.
- 🟡 Resolved before generation: corner span must match the renderer's proven half-cell join convention, rather than asserting an unrelated whole-cell depth.
- 🔵 Intentional approximation: cloth is a few rectangular masses; no dynamic wind, texture grain, font detail or simulated canopy. Native semantics remain intact.
- TC07 export completed:40 models and166 generated asset/metadata files,0 C# errors. TC08 imported-art gate passed79/79 cases, including exact references, anatomy, palette/bounds and corruption controls.
- TC13: refined40-model export succeeded with0 C# errors. Independent review of all six native views accepted the palette, composition and camera-facing sign; regeneration audit verified exactly8 mesh changes /158 byte-identical files.
- TC14: both imported-art fixtures passed81/81 cases after the palette refinement. TC16 independent final review accepted all8 native images and verified zero missing/unmodeled owners.
- 🧪 Pending: final full suite and unchanged-source rebuild gate. Imported assets and static native composition are verified separately.
- ⚪ Full-chunk views cannot establish live input feel, animation quality or sustained FPS.

## Files and implementation log

New source: `Assets/Scripts/Presentation/Rendering/FirstTentVoxelKitLibrary.cs`, `Assets/Editor/Scenarios/FirstTentVoxelKitBuilder.cs`, `Assets/Tests/EditMode/Presentation/Rendering/FirstTentVoxelKitTests.cs`, their three copied metadata files, and this document. Generated resource directory is installed only by root after the isolated export/audits.

1. TC01 native three-seed census and current blueprint/faction/profile survey established exact owners; no new blueprint was needed.
2. TC05 missing-type RED recorded, then production library and offline builder authored. Geometry tests were saved before geometry.
3. Source audit passed. Ready for first import, native preview and independent visual review.

4. TC07/TC08: root exported40 combined models /166 files successfully with0 C# errors, then passed79/79 imported-art cases. This resolves the first-import pending statements above; TC09 integrated tests and TC10 native previews are the next independent gates.

## TC11 initial native-camera review and refinement proposal

Independently inspected all three FirstTent images (seeds64,1729,729490642) in `Docs/Verification/VoxelWorld/TC11-initial-preview`, and the three LastCounter controls. Native receipt reports zero missing meshes and zero unmodeled owners. The one-cell cloth poles and hosts are visible; small hand/face details remain tiny at this full-chunk camera. Dark walls and interiors are readable, with real entrance gaps.

🟡 The sand and approach network dominate the First Tent image. Actual sampled palette32 has luminance0.602/chroma0.302, versus current path64 luminance0.451/chroma0.125. The large yellow field and roughly0.151 path contrast make the composition look schematic. Proposed minimal source correction: ground32→64 and path64→106, giving roughly0.051 visible path contrast and subdued chroma. No geometry, actor, cloth or other32 model changes are proposed.

Before changing production, the continuous-plane test now expects slots64/106; two new sampled-palette cases require chroma≤0.14, luminance≤0.50 and a nonzero0.03–0.08 route contrast, with unchanged dark tent fabric as countercontrol. Root will capture actual TC12 RED before authorizing those UV changes. Native agents separately refine functional shelter arrangements and keep the oath court unpaved; those are generation-rule changes, not manual scene edits or art ownership changes.

Static reviews do not establish live input feel, animation, sustained performance or pixel-legible faces from the full-chunk camera.

## TC12 palette refinement implementation

Root captured actual assertion RED:276 cases,259 passed,17 failed,0 C# errors. All three intended art palette cases failed against the imported old32/64 models; remaining failures belonged to concurrent native layout and sign-facing refinements. Production authorization followed that result.

Applied only the two source UV selectors: Ground now64 and Path now106. This affects eight FirstTent meshes after export; all geometry arithmetic and the other eight families across both kits remain byte-identical. `/tmp/threshold-camps-palette-source-preservation.json` records the two-method/two-line source change. The source audit was rerun and remains clean at40 models, maximum240 vertices, maximum2 colors, six unique copied source metas and166 expected output paths. All LastCounter art remains unchanged. Root will reexport40 models and verify the other32 models and existing GUIDs stay identical.

Refined imported-art/native-camera gates and final full regression remain pending; no green result is inferred from source arithmetic.

## TC13 refined review / asset-preservation gate

Independently viewed all six refined images in `Docs/Verification/VoxelWorld/TC13-refined-preview`: FirstTent and LastCounter at seeds64,1729,729490642. Export receipt records exit0 and0 C# errors; all six native views retain zero missing meshes / unmodeled owners. FirstTent no longer reads as a bright yellow field crossed by painted brown circuits. The darker ground and quiet approach remain distinguishable, the oath court stays natural and open, and cloth poles/hosts remain readable without being enlarged outside their native cells. Shelter roles now change arrangement across seeds. No further kit material or geometry correction is warranted.

Compared every original TC07 file from `/Users/steven/.cache/caves-of-ooo-validation/threshold-camps/first40-artifacts.json` to the actual refined isolated project. Of166 files,158 are byte-identical; the only differences are the eight `firsttent-ground-0..3.asset` and `firsttent-path-0..3.asset` mesh files. All prefabs, metadata/GUIDs, both libraries and the other32 models are unchanged. Independent receipt: `/tmp/threshold-camps-refined-byte-audit.json`.

Art acceptance is bounded to imported anatomy/materials, native static views and the verified asset changes. Small facial/hand details remain tiny at full-chunk scale. No assertion about live input feel, animations or sustained FPS is inferred. TC14 integrated targeted result and final full suite remain root-owned and pending at this entry.

## TC16 final independent camera review

Inspected all eight actual PNGs and both receipts in `Docs/Verification/VoxelWorld/TC16-final-preview`: seeds64,1729,729490642,1 for each area. TC14 imported-art verification is81/81 passing. TC16 runs the native preview method against the final source, with exit0, an empty compile-error list, and zero missing/unmodeled owners in every row. It covers all three FirstTent formations (NorthwardWelcome, SouthwardWelcome, SaltRoadGathering) and all three LastCounter formations (SteppedPost, UpperSupplyCourt, LowerSupplyCourt).

The newly inspected LastCounter seed1 LowerSupplyCourt keeps the lower loading room's crates and envoy readable, with an open north frontage, separate western work apron and three-band sign still facing the camera. The corresponding FirstTent seed1 preserves the quiet ground, unpaved oath court and distinct rest/salt/guest roles despite changed owner positions. No substantive material, geometry or model-coverage defect was found in any of the eight views; no further art change is recommended.

The same honesty limits apply: single-cell actor details are small, and some camera-side low furniture is partially occluded by its actual wall. These are bounded static-readability limits, not evidence of missing owners. Imported anatomy/reference tests plus the native receipt provide separate measurable coverage. Live input feel, animation and sustained FPS are not verified by these images. Final full-suite and unchanged-source rebuild gates remain pending at this entry. Source remains unchanged since the accepted TC13 art refinement.


## Final integration and verification

The pending gates in the chronological entries above are closed. The first full run TC18 exposed eight older controls which still treated these now-converted sites as unconverted; assertions were retained and their scope fixtures corrected. TC19 verifies all288 selected new and corrected legacy cases before the final full run. TC17 passes280/280 cases:82 FirstTent native,59 LastCounter native,81 imported-art and58 owner-driven rendering checks. This includes96 native and24 rendering dedicated adversarial cases. TC20 adds no regressions to the verified baseline: all280 new cases pass; the existing32 failures retain identical test names and failure messages. Four old negative controls were readdressed to their still-unsupported depth1 counterparts, with none discarded.

[Final eight-view gallery](Verification/VoxelWorld/TC16-final-preview/index.html) shows four world seeds per area, collectively covering all three formations each. Every actual native graph has zero missing meshes and zero unmodeled visible owners. Independent visual and shared-source reviews are complete. The boundary-fire assertion was corrected using actual native walking and campfire-rest execution; it was not a terrain bug. The seeded FirstTent frontage collision and backward LastCounter sign were confirmed and fixed during development.

TC21 reproduces all40 models /166 asset and metadata files byte-identically, then TC22 installs those exact resources and audits GUID uniqueness. The source freeze verifies2033 source/content/assembly/meta inputs in the original and isolated project, unchanged throughout the final full run and rebuild. [Close-out evidence](Verification/VoxelWorld/TC22-closeout/README.md) records the complete case comparison, artifact hashes, integration delta and source audit.

Fresh native chunks at Overworld.5.17.0 and Overworld.18.18.0 receive these rules through the real world manager. Existing graph instances are preserved. Current spawn,1.2x camera and full-reveal preferences are unchanged. Original Unity was not restarted. Static previews establish composition and coverage; live input feel, animation and sustained FPS are not inferred. Existing mixed source files remain installed; only this phase's exact delta is recorded in the scoped implementation patch, preserving unrelated work.
