# Existing visitor rigs in the Spread

Status: imported and integrated; **36/36 native assertions pass**. All13 persistent palette copies were imported with borrowed asset bytes unchanged. A39-frame native pose gallery is captured; root reviewed all39 poses, and this worker independently inspected15. Whole-biome visual acceptance and full integration remain separately evidenced.

This closes one part of the receiving-biome visitor gap: thirteen creatures already have real animated bodies but lack the accepted Spread palette. The exact roster is ChoirTendril, Mosshulk, Rotling, MawToad, SariSnake, Wardline, CascadeFather, GlasspaneFrog, YellowfootWayfarer, Shambler, GroveLanternMoth, SkySari and HelmwoodFrog. Their native `ring-*` model IDs remain unchanged. The other29 absent visitor forms have a separate implementation owner and are not covered here.

## Verification sweep and design

The native107-roster diagnostic and RED fixture establish current missing style, not ordinary occurrence/recruitment of all107 in Spread. Source geometry is the mesh actually adopted through `VoxelWorldMeshCatalog.Resolve` from each native prefab skin. Repainting raw FBX geometry would bypass the existing voxel conversion and could lose the current moth's repaired form. The importer instead prepares persistent UV-only copies of those exact current meshes and borrows the approved24-color glade material. Source prefabs, bones, bindposes, weights, clip references, sockets and vertex/index geometry remain native.

The original ring palette PNG is not necessarily CPU-readable through Unity's importer. A temporary owned decoded PNG snapshot supplies its pixels without changing the texture importer. Source RGB regions map deterministically to the nearest approved swatch using the same weighted color distance as the existing-environment adoption. All13 inputs and every output type are preflighted before any asset writes. The importer records source paths, hashes, counts and the mapped swatch histogram; hashes of borrowed asset dependencies and their meta files must remain equal afterward. Temporary decoded textures are destroyed on success and failure. Asset writes are not a filesystem transaction; interrupted/import failures must be reported and reviewed.

The independent library holds exact model ID, native SourcePrefab, adopted Source mesh and Painted mesh. Validation compares all geometry/topology/rig buffers and every nonprimary UV channel; primary UVs must index approved swatches. Source and instantiated bones/rootBone must belong to their own hierarchy. Application refuses borrowed prefab assets and matches exact owned skin, root-bone path and bone order before changing only borrowed mesh/material references, preserving renderer.localBounds.

Runtime integration is limited to an actual managed Spread bind. Presenter model creation supplies the exact native model ID after ordinary voxel and glade conversion. Equipment callbacks omit the ID and cannot acquire a visitor body paint. The explicit style catalog gets the exact painted mesh/material pair, and voxel registration recognizes those already-baked assets. Existing recipe/refusal/ambush/current-owner/visibility checks remain authoritative. No blueprint, faction, attack, body, skill or world-generation data changes belong in this slice.

## Tests and evidence

Actual native job `70cbf2e710f844f59ee46338123a35b4` produced13 missing-library RED and13 foreign native-model controls PASS: `Integration/native-calm37-fit6-green-headwear9-visitor13-red`. Each positive will require the real native current owner, unchanged model ID/glyph/color/position/parts, exact source-prefab→current-voxel relationship, UV-only geometry, five borrowed clips, bone order, renderer bounds, exact approved style and hide/removal cleanup. Foreign controls retain original meshes.

Ten adversarial additions cover nonprimary UV channels1–7, foreign bone/rootBone references and refusal to mutate a borrowed prefab. These are review-driven guards, not claims of separately reproduced native defects. Actual native job `fc8e984ae30c4402b2ace480e0715cb0` passes all26 paired source/foreign cases and all10 adversarial cases (36/36). The combined37-case receipt retains the one expected aggregate roster failure for the separate29 missing bodies: `Integration/native-visitor36-green-dynamic29-red`.

The importer receipt `Visitors/ExistingRigs/native-import.json` records13 source rows and14 written assets (13 meshes and the library), with `borrowedBytesUnchanged=true`; the editor scene stayed clean. The refreshed107-source diagnostic now records77 approved bodies,29 remaining missing bodies and one deliberately hidden owner. This is explicit factory source coverage, not evidence that all species naturally occur or can be recruited in the Spread. No standalone runner can establish this native asset/rendering behavior.

## Self-review and remaining gates

Q1/Q2: scoped UV mesh replacement follows existing glade actor paint timing and preserves native instance ownership; body/equipment callbacks remain distinct. Q3: actual foreign-owner controls precede feature publication; geometry/rig/malformed and visibility controls remain in the exact native gates. Q4: actual import, borrowed-byte invariance and36 native assertions are now established. The native gallery is captured and the15-image inspection below establishes visible framed bodies for the reviewed subset. Assertion success does not establish live animation transitions, complete pose quality or in-world readability.

The three renderer hook hunks were published against checked current preimages, preserving the separately serialized static-environment and new-body work. Root owns native refresh/import/test runs. Root subsequently reviewed the remaining24 poses; this worker does not claim that separate inspection as its own. The separate29-form pack now closes the107-source roster with106 approved visible bodies and one intentionally hidden definition. Native source coverage does not establish ordinary spawning or recruitment of every species; actual in-world/live-transition assertions remain bounded to the recorded routes and controls.


### Native gallery inspected

The C#6 capture helper executed successfully in the real editor and wrote39 original PNGs (13 species × Idle/Walk/Attack35%) under `Visitors/ExistingRigs/NativeGallery/2000b70854b94b33981fb43e64b9b326`. The actual adopted source geometry, palette, original rigs/clip assets, native GPU output, unchanged source dependency bytes and restored main scene were checked. Root directly viewed all39 poses in labelled contact sheets. The reviewed forms remain legible in the shared palette; some inherited voxel joint seams remain visible at close-up. This does not establish live transitions, ordinary occurrence, recruitment or gameplay animation quality. See the exact hashed `root-review.json`; the original PNGs are retained unchanged.

## Native pose gallery

Root executed the isolated native gallery at `Visitors/ExistingRigs/NativeGallery/2000b70854b94b33981fb43e64b9b326`. Its report records39 PNGs: the exact13 species in Idle, Walk and Attack at35% of their original imported clips. All26 non-Idle poses have a positive measured baked-vertex difference from the sampled Idle pose. Exact borrowed source dependency bytes remain unchanged and the original main scene/dirty state is restored. The script uses current native voxel adoption, the scoped persistent painted mesh, the original prefab bones/controller/avatar/clips and the approved Spread light/ambient profile; it frames the actual sampled world bounds. No gameplay actors, AI, inventory or world generation are involved.

Independent image inspection viewed all13 Idle captures plus `10-GroveLanternMoth-Walk.png` and `10-GroveLanternMoth-Attack.png` (15/39). All reviewed subjects are visible and fully inside the frame; no missing body or pose explosion is apparent. The original frog families retain similar squat silhouettes and the older source forms retain their coarse geometry. This palette-only slice does not claim a distinct-species geometry redesign. The other24 pose images were not visually inspected by this reviewer.

The preview deliberately supplies full gallery visibility and samples static poses. It cannot establish ordinary encounter/recruitment, scheduler movement, live animation blending, terrain occlusion, whole-biome appearance or gameplay balance. Native36 owner/foreign/rig tests and the separate receiving-zone transfer fixture cover different assertions; neither is replaced by these pictures.

## Shared core checkpoint

The final private standalone checkpoint (`Integration/standalone-spread-20260927.json`) retains the exact672-file baseline selection:10075 executed,9780 pass and the same295 environmental failures. Name-by-name comparison finds **zero newly failing and zero newly passing** against both the prior accepted postfoundation checkpoint and original `50ef23d2` baseline. The newer86 selected files execute1888 passing cases separately, including all37 Calm controls. All included gameplay/core/content inputs still matched shared sources after both runs. These results do not exercise visitor assets, rendering, native input or Unity-identical world hashes; native36 and the gallery provide the relevant evidence for this slice.


## Final native integration checkpoint

The complete unfiltered Unity EditMode run `fd718936b9304ca3b0441e84e92ad5d4` executed **19709 cases: 19709 passed, 0 failed and 0 skipped**, in 979.5685913 seconds. The authoritative receipt is `Docs/Verification/DensityCompletion/Integration/native-spread-full-second.json`. The earlier complete-sweep failures and test-first correction receipts remain preserved.

This full-suite result adds regression evidence to the scoped native source/owner/state controls, actual player routes and reviewed images above. It does not turn finite source censuses into every possible seed, isolated pose galleries into every animation transition, or the512-per-kind transient budget into unlimited output. The separate legacy2D liquid-shimmer defect and later content acceptance remain explicit followups.
