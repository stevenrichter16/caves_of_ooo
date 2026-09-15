# Overwrit voxel art kit

Status: complete and installed. Final targeted gate 396/396. Full suite 12,570 total / 12,538 passed / 32 unchanged baseline failures / zero C# errors. All 307 new feature cases pass; the suite also adds one surrounding equipment countercontrol.

The kit expresses deliberate absence with sixteen models in four families. Ground is one uninterrupted muted surface. New growth shares one height. The rare rim furnishings have useful silhouettes but no inscriptions, invented religious symbols, glow, or decorative rubble. Native placement keeps stone posts and benches at the rim; the art library never creates them in the blank interior.

| Family | Exact blueprint | Art rule |
|---|---|---|
| ground | OverwritGround | Full-cell plane, fixed exposed height/color. Four variants differ only in buried thickness; this is one visible appearance. |
| growth | OverwritNewGrowth | Three coarse gray-green tufts, common height and two subdued hues. Width/lean arrangement supplies four shapes, without flowers. |
| waymarker | OverwritWaymarker | Plain upright stone post on a modest base, two gray hues, no carved face. |
| bench | OverwritPilgrimBench | Low wooden seat, legs and backrest; local +Z is the open/facing side. Four proportions, at most two wood hues. |

Only ground has `kind=ground`; all fixtures are static `entity` meshes. Each prefab holds one combined mesh and the existing ring palette material, with no scripts or colliders. `OverwritVoxelLibrary` provides `Load`, `Validate`, `Find`, strict cached `ModelId(family,variant)`, exact `Family(blueprint)`, `VariantCount=4`, and resource path `OverwritVoxel3D/Library`. Unknown names are refused. No existing native floor, generic plant, PalimpsestEcho, ruin or undertext content is silently claimed.

## Verified corrections and boundaries

| Premise | Source and correction |
|---|---|
| Every biome needs many decorative formations | `Lore/Design/THE-OVERWRIT.md` requires near-blank, too-smooth terrain with sparse new growth and no old ruins. The native agent's Blank/Rim grammar preserves this identity. |
| Four variants require four visible floor patterns | Parent plan explicitly makes blank ground an appearance exception: four assets may vary buried thickness only. No shade checkerboard, cracks, rocks or raised edging. |
| A bench implies resting/underreading | Native contract provides solid, destructible, flammable scenery only; no Restable, dialogue or underreading verb. The seat does not implement one. |
| A waymarker can carry lore text | The exact native marker is a blank-faced rim post. Art introduces no writing or undertext disclosure. |
| The Recension scribe is an undertext ghost | The plan verifies PalimpsestEcho is a human field scribe. It is excluded from this library. |
| Asset geometry can own persistence | Native entities and current cell membership own visibility, orientation, pickup/removal and destruction. The generator produces reusable art only. |

References read: `CLAUDE.md`, `Docs/OVERWRIT-GINMERE-COMPOSITION-PLAN.md`, `Lore/Design/THE-OVERWRIT.md`, the native agent's four exact blueprint contracts, `Docs/PERF-FOUNDATION.md`, `StumpVoxelLibrary`, `StumpVoxelKitBuilder`, `StumpVoxelKitTests`, `SpawnRing3DCatalog.Model`, and sampled existing ring palette. Implemented swatches: ground35, growth6/48, marker12/13, bench11/15. All UVs sample constant palette points, never per-face texture noise.

## Gates, performance and self-review

RED specifications cover the complete sixteen-entry library, four distinct meshes per family, one-cell X/Z bounds, ≤240 vertices, at most two swatches, one renderer, exact metadata/material references and no gameplay components. Twelve corruption controls preserve original assets while proving validation failures. Further controls require a fixed blank surface, uniform growth height, an unmarked upright marker, bench backrest/open-side orientation and exact blueprint exclusions.

Authoring runs offline in `CavesOfOoo.Editor.OverwritVoxelKitBuilder.Run`, writing `Assets/Resources/OverwritVoxel3D`. Asset GUIDs are retained during rebuild and no scene is saved. There are no per-cube runtime objects. IDs are precomputed; lookup uses the existing finite validated-library pattern. Parent integration owns native batching, movement/removal reconciliation, exact eligible addresses, generated previews and full regression.

- 🔵 Art follows the native region's absence instead of importing the abandoned desert/ruins placeholder style.
- ⚪ Buried-only ground variation is deliberate; visible uniformity is the requirement.
- ⚪ No procedural bleeds, sounds, restored settlements, resting or underreading mechanics ship in this kit.
- 🧪 Numeric geometry and reference checks do not establish live feel or frame rate. Generated camera previews are still pending.

## Implementation log

- 2026-09-15: Read methodology and the parent plan; coordinated exact aliases and facing convention with the native agent. Authored tests and metadata before production. Awaiting parent RED.

- AR03: Parent confirmed missing-type RED for both new libraries (186 C# error lines including the independent native plan). Only then authored the strict library and offline combined-mesh builder. All six new script/test metadata files have distinct GUIDs; source geometry stays inside one cell and uses constant two-color-or-fewer palette samples. Parent owns build, native previews and Unity validation.

- AR09: The parent confirmed all four strengthened actual upward-face cases RED: side-tip0.22 versus the common0.32. The original test compared only each variant's tallest bound and missed this canon mismatch. Corrected only Growth's two shorter shoots: every upward tip is now0.32cells. The three coarse pieces, positions, widths and two swatches remain unchanged. Rebuild/GREEN remains pending.

- AR08 generated the first16 models with zero C# errors. AR09 passes29 of33 Overwrit art cases; the four expected exposed-tip regressions fail and led to the source-only height correction above. Inspected native Blank-64 and PilgrimageRim-64: appropriately empty compositions, with a noted warm/yellow ground tint for parent review. These captures precede the uniform-tip fix.

- AR12 rebuilt all80 models across both kits with zero C# errors. AR13 reports138 total,137 passed and exactly one expected color failure: source swatch4 chroma0.129411787 exceeds0.09. All upward-growth-tip and native ownership cases pass. Changed only Overwrit Ground's constant swatch4→35=(166,174,155), preserving geometry, height, material, variant count and shared palette files. The source-color test samples the real texture, so this fixes a measured yellow/sandy appearance rather than renaming the region. Final rebuild/verification remains with the parent.

- Final source/metadata cold-eye: checked every new family against exact native alias/height/ownership rules, connected limb/support geometry, one-cell bounds, two-color budget and strict library failure paths. No additional notable source finding remains after the growth/color corrections. Read-only audit of the AR12 isolated resource directory found16 meshes,16 prefabs and33 unique asset metadata entries, plus the unique folder metadata. Every prefab contains exactly one GameObject, Transform, MeshFilter and MeshRenderer; no extra component, collider, script or transform offset. All new resource/folder GUIDs occur once across isolated Assets. The original project resource export remains parent-owned. This audit precedes the final neutral-floor rebuild and does not claim final live visuals/performance.

## Final close-out

AR21 targeted:396/396. AR22 full:12,570 total,12,538 pass,32 failures whose
names and messages exactly match AR01; zero new failures and zero C# errors.
All307 new feature cases pass, plus one added surrounding equipment control.
Final AR18 native gallery contains21 views with zero missing meshes and zero
unmodeled visible owners. Main-project art is installed;405 source/art files
match the isolated copy and196 task metadata GUIDs have no collisions. Static
captures do not establish live input/HUD/lighting feel or sustained FPS. See
`OVERWRIT-GINMERE-COMPOSITION-PLAN.md` for the exact mixed-file commit boundary.
