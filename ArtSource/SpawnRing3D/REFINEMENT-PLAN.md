# Spawn ring: source refinement after the first native Unity pilot

Status: implementing under /tmp; production Assets and adopted art are read-only. Baseline catalog SHA256 bb7a1f3f60083b19ae2e346d2b22c13a525f505d723d68152d9fa287938d3efb.

This is Caves of Ooo original presentation work; no Qud parity claim. The native simulation and existing rendering contract remain authoritative. Actual Unity screenshot evidence: Docs/Verification/SpawnRing3D/R3D-bf474b44d8a244bc86f0d7f81754b75f-zone-Overworld.*.png, all eight personally inspected. Re-read exact NW, west, Felling ImageGen references and final generator ground(), stone_cluster(), felling_hero(), atlas and export helpers before editing.

## Verification corrections

| Premise | Verified correction and consequence |
| --- | --- |
| Black cells indicate absent terrain | Native FOV/occlusion is enabled. Black unobserved cells are excluded from missing-art claims. |
| Blender overhead establishes gameplay quality | Actual Unity shows repeated tiny ground pieces as dark speckles. Compare candidate under unchanged Blender light/camera, then require another actual Unity pass. |
| More microgeometry improves richness | Ground's nine raised leaves/two pebbles/flowers repeat on each cell. Remove these and the centered round wash; paint subtle low-frequency stone/loam/moss instead. |
| Four ledge IDs ensure silhouette variety | All four use the same rounded eight-corner recipe and pale fill. Author four different worn outlines at existing cell bounds. |
| Felling import fix can be simplified | Keep exact native-solid polygon clipping, uniform outline shrinking and explicit triangulation. These fixed two proved Unity import failures. |
| A better image permits a larger budget | Parent explicitly prohibits triangle increases; compare every model and all eight native scenes. |

## Three bounded milestones

1. Quiet ground. All twelve floor/grass/tepui model IDs remain; ground becomes its existing flat unit base with four painted variants and no raised per-cell speckle silhouettes. Common edge colors avoid border outlines. Tepui colors move toward warm pink-gray fossil stone; forest stays olive loam.
2. Four worn ledges. Preserve each native cell/pivot, replace identical outlines with distinct asymmetric low slabs and restrained varied grain/moss. Do not alter other stone families.
3. Flowing Felling bark. Fewer, longer existing root-surface ridges, blended into continuous root mass. Native union, eight hero IDs and exact owner placements remain. No new scenery objects or interactive features.

## Gates

Before edits: a Blender asset probe must fail against baseline for raised ground, insufficient ledge aspect variation, and excessive bark-strip density. Those are measurable proxy goals; they do not assert aesthetics. Controls retain positive thickness and unchanged model/owner/zone counts. After edits: exact native scene manifests, per-model/per-scene triangles <= baseline, unrelated model metadata/FBX bytes retained, UV/material/bounds roundtrip and source topology checks. Independent review before adoption. Use unchanged camera/light for before/after crops. Actual Unity visual gate belongs to root and remains pending until reimport.

## Initial self-review

🟡 Ground painted variation can itself become tile camouflage: keep low contrast, shared edge values and existing quarter-turns, personally inspect repeat patterns before export.
🟡 Root strips may cross concave native boundaries: retain exact clipping of polygon interiors, not just vertex checks.
🧪 Shader lighting, native fog and final gameplay feel are not established by offline renders; no claim of final visual acceptance.

## Log

- Read original methodology and generator; personally inspected eight native shots and exact reference images. No production or baseline mutation.
- Baseline probe: three intended RED assertions, three passing controls. One initial probe bug treated all catalog ground-kind models as full thickness terrain, including dry puddle beds; corrected the scope to the twelve explicitly targeted floor/grass/tepui models before implementation. A metadata vector dict/list typo was also corrected before valid RED.
- v1 geometry: all six probe assertions GREEN; 150,908 → 126,898 unique triangles, 843 → 284 root detail objects. All eight placement lists and native manifest fields unchanged apart from measured triangle totals. Review rejected the first paint as too blurry and its cracks as long scratches.
- v2 paint: broken stone-joint fields and low-contrast opaque loam/moss flecks, replacing raised geometry. One previously unused axis_x atlas swatch is repurposed to root_bark; the ring exporter never instantiates axis-marker models. Root ridges receive directional UVs so grain follows their length. Palette dimensions, material slots and every other non-ground swatch remain fixed; verify actual pixel comparison before handoff.
- Final bounded shape pass rounded the root ridge ends with 12 perimeter points while retaining explicit triangles and the exact native-solid clipping. The additional points stay below every baseline model budget: final unique kit 132,578 vs 150,908; 284 ridge objects vs 843 prior cards. Frozen catalog SHA 1b77e93c6ab78b60a1ab01c2ded6a79758de6ba0d483426674c32f1cb3a5fe59.
- Actual 218 FBX roundtrip PASS, zero errors; source topology zero zero-area triangles. Source-delta audit proves exactly 21 geometry/UV/color models changed and 197 unchanged; only ground swatches64..75 and unused axis slot56 changed. All 197 retained FBXs are byte-identical to baseline.
- All six final asset-probe checks PASS. Every saved scene's 218 source models match the final library; all native instance transforms/owners match its manifest. Every manifest field is identical to baseline except lower measured triangle totals. All eight final wide renders and three crops were personally inspected.

## Final in-phase review and honesty bounds

🔵 Resolved: repeated ground cast-shadow speckling removed, tepui terrain has distinct warm stone paint, ledge outline/value variation increased, root growth ridges use fewer rounded forms. This is source-art review, not a claim of final Unity appearance.
🔵 Resolved: valid apex topology and exact native footprint clipping preserved; explicit curved-root triangles remain, with no zero-area source triangles or roundtrip count mismatch.
🧪 Remaining visual debt: faint painted ground repetition is visible in wide open areas; native boulder clusters still repeat and root-to-moss transitions remain simplified. Water detail and reference richness remain separate from this accepted bounded pass. No promise of exact ImageGen fidelity.
🧪 Parent owns the second actual Unity import/native/FOV/visual/performance gate. All candidate rendering used the same Blender camera/lighting; black native unobserved cells were never counted as absent art.

Files changed for runtime adoption: build_ring.py, catalog.json, palette PNG, 21 exact FBXs listed in ADOPT-RUNTIME-PATHS.txt. Companion handoff: rebuilt ring_kit.blend, eight scene.blends and manifests/overheads, three crops, asset guide and verification receipts. No project/Assets/Unity mutation by this subtask.
