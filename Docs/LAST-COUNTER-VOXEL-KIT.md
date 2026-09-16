# Last Counter voxel kit

Status: complete and installed for fresh native generation. TC17 passes280/280 targeted checks. TC20 full suite: 14,042 passed,32 unchanged baseline failures,0 C# errors and no new failures. All280 added cases pass. TC21 reproduces all166 final asset/metadata files byte for byte; TC22 confirms installation and no task GUID collisions. CoO-original composition and art; no Qud-parity or live-FPS claim.

## Purpose and art direction

The Last Counter is a staffed limit of the Concord's delivery guarantee. Its defining image is a low weathered supply court and a planed board with a current disclaimer above two older inscriptions. The envoy is a gold-clad trader with a ledger; the sign remains a separate, fixed native object. Quiet earthen ground and two broad stone strata keep architecture readable without tiled grooves or bright road coverage. Empty eastern ground belongs to the native layout and does not imply an invisible barrier.

This is CoO-original art using the user's coarse voxel direction. No Qud parity or new Concord delivery mechanic is claimed. Existing camera, native map tiers and the two abandoned predecessor counters remain outside this art task.

## Verification sweep / corrections

| Source read | Verified contract and art consequence |
|---|---|
| `CLAUDE.md`; parent composition plan | Actual RED before code, source/asset corruption checks and a distinct native-camera gate. Root owns shared rendering, Unity execution and final verification. |
| `Lore/Factions/04_SaccharineConcord.md:97`; `LandmarkBuilder.LastCounterPost` | The delivery guarantee has withdrawn twice. Native LastCounterSign and the nearby abandoned posts supply that history; no deity or supernatural border is introduced. |
| `Objects.json` / `LastCounterSign` | One solid examinable PhysicalObject, canonical `I`. A planed board with the current disclaimer and the same words older/fainter twice beneath. It has no Destructible Part or menu; art supplies three coarse bands, native Examine supplies the actual words. |
| `Objects.json` / `SaccharineEnvoy` | Actual Creature `@`, Trader EnvoyStock100drams, SaccharineEnvoy_1 conversation, AV2/DV2 and nonwandering Brain. This is a person, unlike Cinderhold's nontrader factor. No counter is attached to the mesh. |
| TC01 actual census | The post chest appears as native Chest. `chest:CampGoodsT1` is a stamp command, not the fictitious `ChestCampGoodsT1` blueprint; reuse existing Chest art. |
| `LastCounterCompositionPlan` native-agent contract | The new outdoor base is Floor, not Sand; StoneFloor remains sheltered interior. This area has no mapped road and adds no RoadStone. Corrected the proposed ground alias before production; positive Floor and negative Sand/RoadStone tests pin the distinction. |
| Existing Wellmeet, Cinderhold and shared kits | Reuse actual services, furnishing, late MarketStall, Campfire, portable goods and generic NPC roles. Do not create new prop families merely to duplicate existing coverage. |

## Families and APIs

All IDs are `lastcounter-<family>-<variant>` for variant0–3. Exact `Family` aliases are below. Invalid family/variant calls throw; unrelated or misspelled blueprint names return no alias. Architecture is selected only in the parent renderer's verified site scope.

| Family / native alias | Geometry | Palette slots | Boxes |
|---|---|---|---:|
| ground / Floor | Full-cell muted earth, continuous top0; only buried thickness varies. | 64 | 1 |
| wall / SandstoneWall | Two full-cell broad layers, height1.05, variable layer split but constant silhouette. | 56,79 | 2 |
| sign / LastCounterSign | One broad planed board on separate posts, three physically separated bands decreasing in width below the current line. | 12,35 | 6 |
| envoy / SaccharineEnvoy | Separate boots, neat ochre coat/cap, head/eyes, arms holding one dark ledger. Height1.51. | 49,12 | 10 |

Every model is one combined mesh with one renderer, maximum240 vertices and two palette UVs. X/Z bounds remain inside one native cell. No new material texture, collider, script, light, socket, animation clip or rig is attached. In particular, the sign's three strips are not textured lettering or an interactive UI; native content retains the only text and behavior.

## Reproducible implementation

`CavesOfOoo.Editor.LastCounterVoxelKitBuilder.Run()` builds `Assets/Resources/LastCounterVoxel3D` with shared ring material. It updates Mesh assets and prefabs in place, preserves existing GUIDs, validates exact mesh/prefab/metadata reference correspondence and never saves a scene. Runtime `LastCounterVoxelKitLibrary` exposes `Load`, `Find`, `Validate`, `Family` and `ModelId`; incomplete or malformed libraries fail clearly, including injected prefab lights.

The shared source audit `/tmp/audit_threshold_camps_art_source.py` writes `/tmp/threshold-camps-art-source-audit.json`: full40-model box/palette/bounds inventory, source hashes, six copied-meta uniqueness checks and166 expected generated paths. Old resource assets are not touched by authoring this new kit; root verifies isolated rebuild bytes separately.

## Verification and self-review

- TC05 actual RED: both absent classes produced72 C# missing-library lines before production. Root rejected stale-test-result interpretation.
- Anatomical tests preceded geometry: three distinct sign bands have real face gaps, one supporting board and separate posts; the envoy has separated legs and a held ledger without an attached desk; low walls form continuous full-cell masses; ground has one stable visible swatch.
- Source audit: all40 combined models within one-cell bounds, max240 vertices, max2 swatches, six copied source metadata GUIDs unique; no source-audit findings.
- 🟡 Corrected: LastCounter's new native Floor replaces proposed Sand alias; no invented road family or CampGoodsT1 chest blueprint.
- 🔵 Intentional art simplification: three coarse bands communicate repeated notices. Their exact wording and age nuance are only readable through native Examine, not through tiny fake font pixels.
- TC07 export completed:40 models and166 generated asset/metadata files,0 C# errors. TC08 imported-art gate passed79/79 cases, including exact references, anatomy, palette/bounds and corruption controls.
- TC13: independent native-camera review accepted all3 LastCounter seeds and the3 FirstTent controls; the existing sign now presents its three bands toward the camera. All16 models, prefabs and GUIDs remain byte-identical through the combined refinement.
- TC14: both imported-art fixtures passed81/81 cases after the palette refinement. TC16 independent final review accepted all8 native images and verified zero missing/unmodeled owners.
- 🧪 Pending: final full suite, unchanged-source rebuild and root's remaining player-flow gates.
- ⚪ Static source checks and later screenshots cannot establish live input feel, animation or sustained FPS.

## Files and implementation log

New source: `Assets/Scripts/Presentation/Rendering/LastCounterVoxelKitLibrary.cs`, `Assets/Editor/Scenarios/LastCounterVoxelKitBuilder.cs`, `Assets/Tests/EditMode/Presentation/Rendering/LastCounterVoxelKitTests.cs`, their three copied metadata files, and this document. Root alone exports and installs the generated directory after verification.

1. Read current faction lore, native stamp, TC01 three-seed owners, raw blueprints and existing reuse candidates.
2. Correct native ground alias; author art contracts and capture TC05 missing-type RED. Author anatomy tests before geometry.
3. Implement16 reusable source models and strict library. Source audit passes; imported-art and native-camera gates remain pending.

4. TC07/TC08: root exported40 combined models /166 files successfully with0 C# errors, then passed79/79 imported-art cases. This resolves the first-import pending statements above; TC09 integrated tests and TC10 native previews are the next independent gates.

## TC11 initial native-camera review

Independently inspected all three LastCounter images (seeds64,1729,729490642) and the three FirstTent controls in `Docs/Verification/VoxelWorld/TC11-initial-preview`. Native receipt reports zero missing meshes and zero unmodeled owners. The pale cutaway walls form quiet broad masses; the envoy and ordinary people remain visible against darker interior floors. No LastCounter kit palette or geometry refinement is warranted at this pass.

🟡 Renderer-facing finding: all three views show the sign's blank back. The sign geometry has three bands on local+Z, while the gameplay camera is positioned toward negativeZ and its current recipe has quarter-turn0. Root owns a dedicated actual-facing RED test and the proposed quarter-turn2 correction. Duplicating fictitious text on both sides or changing the native owner is unnecessary; the existing one-board art is correct.

The rooms' first layouts still look overly schematic and similarly furnished. Native agents own the proposed separate loading/storage activity, actual rest beds and restrained western shoulder colonies, preserving the empty eastern frontier. These do not require more model families. Full-chunk text is intentionally abstracted to three coarse lines; the native Examine description remains the source of exact words. Static views cannot establish live input feel, animation or sustained FPS.

## TC12 cross-kit refinement status

TC12 captured actual RED for the FirstTent palette and root-owned static sign-facing correction, before implementation. This kit's16 model sources are unchanged. The next combined40-model export will change only the FirstTent ground/path UVs; all LastCounter meshes, prefabs and GUIDs are expected to remain byte-identical. Root owns the sign recipe rotation and native layout refinements; refined native-camera review remains pending.

## TC13 refined native-camera acceptance

Independently viewed LastCounter64/1729/729490642 and the three FirstTent controls in `Docs/Verification/VoxelWorld/TC13-refined-preview`. The native loading crates and short western returns frame a useful supply apron, rest beds distinguish shelter from stores, and restrained brush/rubble colonies give the occupied side environmental context. The eastern empty ground remains a deliberate contrasting frontier. Low pale walls stay coarse and readable; changing their materials would add churn without resolving a remaining defect.

The static sign-facing finding is resolved by root's renderer change. All three LastCounter images show the existing three dark bands toward the camera; no sign model, duplicate inscription, collider or native mechanic was added. Exact words still belong to native Examine. No additional art refinement is needed at this pass.

Independent comparison of original TC07 manifest to the actual refined clone confirms166 current files:158 byte-identical, with only8 FirstTent ground/path mesh files changed. Every LastCounter mesh, prefab, library and metadata/GUID is unchanged. Audit receipt: `/tmp/threshold-camps-refined-byte-audit.json`. Combined export exit0 /0 C# errors; all six native views have zero missing/unmodeled owners. TC14 targeted result and final full suite remain pending; screenshots do not verify live input feel, animation or sustained FPS.

## TC16 final independent camera review

Inspected all eight actual PNGs and both receipts in `Docs/Verification/VoxelWorld/TC16-final-preview`: seeds64,1729,729490642,1 for each area. TC14 imported-art verification is81/81 passing. TC16 runs the native preview method against the final source, with exit0, an empty compile-error list, and zero missing/unmodeled owners in every row. It covers all three FirstTent formations (NorthwardWelcome, SouthwardWelcome, SaltRoadGathering) and all three LastCounter formations (SteppedPost, UpperSupplyCourt, LowerSupplyCourt).

The newly inspected LastCounter seed1 LowerSupplyCourt keeps the lower loading room's crates and envoy readable, with an open north frontage, separate western work apron and three-band sign still facing the camera. The corresponding FirstTent seed1 preserves the quiet ground, unpaved oath court and distinct rest/salt/guest roles despite changed owner positions. No substantive material, geometry or model-coverage defect was found in any of the eight views; no further art change is recommended.

The same honesty limits apply: single-cell actor details are small, and some camera-side low furniture is partially occluded by its actual wall. These are bounded static-readability limits, not evidence of missing owners. Imported anatomy/reference tests plus the native receipt provide separate measurable coverage. Live input feel, animation and sustained FPS are not verified by these images. Final full-suite and unchanged-source rebuild gates remain pending at this entry. Source remains unchanged since the accepted TC13 art refinement.


## Final integration and verification

The pending gates in the chronological entries above are closed. The first full run TC18 exposed eight older controls which still treated these now-converted sites as unconverted; assertions were retained and their scope fixtures corrected. TC19 verifies all288 selected new and corrected legacy cases before the final full run. TC17 passes280/280 cases:82 FirstTent native,59 LastCounter native,81 imported-art and58 owner-driven rendering checks. This includes96 native and24 rendering dedicated adversarial cases. TC20 adds no regressions to the verified baseline: all280 new cases pass; the existing32 failures retain identical test names and failure messages. Four old negative controls were readdressed to their still-unsupported depth1 counterparts, with none discarded.

[Final eight-view gallery](Verification/VoxelWorld/TC16-final-preview/index.html) shows four world seeds per area, collectively covering all three formations each. Every actual native graph has zero missing meshes and zero unmodeled visible owners. Independent visual and shared-source reviews are complete. The boundary-fire assertion was corrected using actual native walking and campfire-rest execution; it was not a terrain bug. The seeded FirstTent frontage collision and backward LastCounter sign were confirmed and fixed during development.

TC21 reproduces all40 models /166 asset and metadata files byte-identically, then TC22 installs those exact resources and audits GUID uniqueness. The source freeze verifies2033 source/content/assembly/meta inputs in the original and isolated project, unchanged throughout the final full run and rebuild. [Close-out evidence](Verification/VoxelWorld/TC22-closeout/README.md) records the complete case comparison, artifact hashes, integration delta and source audit.

Fresh native chunks at Overworld.5.17.0 and Overworld.18.18.0 receive these rules through the real world manager. Existing graph instances are preserved. Current spawn,1.2x camera and full-reveal preferences are unchanged. Original Unity was not restarted. Static previews establish composition and coverage; live input feel, animation and sustained FPS are not inferred. Existing mixed source files remain installed; only this phase's exact delta is recorded in the scoped implementation patch, preserving unrelated work.
