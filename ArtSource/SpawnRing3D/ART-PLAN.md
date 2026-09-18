# Spawn-ring Blender kit — living art subplan

Status: final refined kit, eight editable scenes and all218 Unity imports are complete in ArtSource/SpawnRing3D. This document preserves the initial art plan/history; README.md and REFINEMENT-PLAN.md describe the current adopted bundle. Root native profiling and final acceptance are tracked in Docs/BLENDER-SPAWN-RING-3D-PLAN.md. CoO-original art; native seed729490642 captures guide art only.

## Verified corrections before implementation

| Premise | Evidence | Correction |
|---|---|---|
| Image references are literal maps | Viewed all8 ImageGen references against exact exported cells | They add incidental steps/tent-like cloth/tree density. Use their material/silhouette quality only; native entities and cells determine models/positions. |
| Water always has a pool entity | Native fen export276 water cells, only7 pool-water cells | Current tile-state water has independent cell-surface recipe; no seed rehydration. |
| All ground blueprints are actors | Coverage61 blueprints,16 observed actors; sundew is a non-Creature trap | Map all ground content, including native vegetation and props. No creature-only enumeration. |
| Reference sinkhole is an abyss | Native cleared walkable interior and real StairsDown | Low wet rock lip, open earthy interior; no invented falling/collision. |
| Scene pixel bounds are physical Felling footprints |55 owners plus independent cell solid/water mask | Root/trunk hero volume derives native solid-cell regions; mutable objects anchored individually. All7 bare position cells remain empty. |
| One humanoid body fits native fauna | Actual examine text/brief | Frogs/tortoise/snakes/eagle/rooted tendril receive distinct real meshes and articulation families. |
| Snapshot carried gear is equipped | All41 observed Inventories have empty EquippedItems | Baseline characters carry no fake sword/shield. Exact four humanoid sockets support real runtime equip positives. |
| Existing village script is import-safe | It executes scene generation at module scope | Extract AST-approved geometry/rig/export functions into a self-contained shared module; do not import and run village source. Existing village source/assets untouched. |
| Repeated families can have one mesh | User wallpaper rule, references | Four real variants for repeated ground, plants, rocks, masonry, compost and ore families; shared prototype meshes keep native instance count affordable. |
| GPU acceptance follows Blender previews | Root workflow | Export unit/axis/rig metadata, but root Unity screenshots/profile remain separate proof. |

## Milestones

1. Contract catalog and coverage assertions before mesh outputs: exact73 blueprint rows (61 observed,9 conditional,Player,StairsUp plus observed other-seed GlowQuartzVein),55 owner rows,218 planned model IDs. Hubble owns native parser/importer tests; root owns shared runtime tests.
2. Shared atlas and geometry factories; Stump/forest/props; canonical per-species rigs; Felling hero/component geometry. Four variants for bulk families. Actual FBX files with palette UVs; preserve tested export-onlyZπ and metres.
3. Eight editable native-snapshot scenes; strict overhead renders; self-compare each to its distinct ImageGen reference. Refine weak material/shape quality before final adoption. Captured layouts are previews, never runtime state.
4. Export audit and independent semantic rebuild; document counts/per-zone triangles, exceptions, prototype reuse and future fallback constraints. Root performs native import/render/perf/interaction/cleanup gates.

## Runtime catalog contract

`catalog.json` schema1,id spawn-ring-3d; model path,kind,rigged,rigFamily,primary materialFamily,triangles,boundsCenter/boundsSize {x,y,z} Unity basis,clips,sockets. Kinds ground/entity/actor/felling-component/tile-overlay. Rig families none/humanoid/frog/tortoise/serpent/avian/fungal/rooted. Clip names Idle/Walk/Interact/Attack/Hit; only humanoids have four exact Equipment.* sockets. Every rig animation is in-place, no movement simulation. Static models consolidate per palette/water group, original collections remain editable.

`blueprints` exact blueprint→model IDs and role; FellingSceneProp uses exact ComponentId resolver, with55 `fellingOwners` rows. Dynamic TileState water uses unit `ring-water-surface` at height.035; on-edge rounded details must stay inside wet footprint. Material names SpawnRingPalette/SpawnRingWater; new atlas2048×1024/128swatches, no village atlas overwrite. Equipment models reused from Village3D/Library only when actual native gear is equipped.

## Honesty/risks

References aim for rich rounded mossy miniature surfaces, dense leafy crowns and broad worn stone; low-detail blockouts are not acceptable final art. Root owns actual performance budget decisions after measured scenes. Full80×25 ground must fill every view with no black/unfilled edges. Trees may visually overhang slightly but native openings/actors must stay readable; runtime FOV/cutaway remains native. No final scene will invent houses, villagers, stairs, water, abyss, gear or loot.

## Log

- Read actual plan, coverage, blueprint art brief, native JSON, village source helpers and all8 reference images. Wrote214-model proposed contract and sent exact schema/mappings to root/Hubble before geometry.

- Native three-seed runtime gate found GlowQuartzVein in seed1729 NW. Added four canonical pale-cyan crystal-in-rock variants and exact blueprint mapping; catalog now218 models/73blueprints. Parent/Hubble own the corresponding RED→GREEN native tests.
- Actual artifact assertions first observed RED before any model export (`reports/contract-red.log`). All current previews are explicitly `--skip-export`; no preview directory is called a final FBX bundle.
- Preview1 exposed stale source object matrices from collections outside the evaluated scene. Corrected with linked source library/dependency update and explicit static `matrix_basis` assembly. Later suspected UV collapse was disproved by UV probe: UV spans were valid; the visible stripes were authored atlas repetition. Fine procedural paint was replaced, not misreported as a proven UV engine bug.
- Preview2–7 visual iterations were inspected at full80×25 and creature gallery scale. Corrected diagonal atlas fabric, then rejected high-contrast camouflage/raised circular moss patches. Source now has quiet painted earth, nearly flush irregular moss washes, distinct quarter-turned ground variants and materially different dense foliage crowns. Individual native anchors and all55 owner identities remain unchanged.
- Replaced uniform tube-like Felling ribs with a continuous smooth, radial-grained heightfield constrained to native solid cells, layered petrified bark geometry and patches of moss/fungus. This remains a stylized game mesh, not a claim to match ImageGen detail one-for-one.
- Actual gallery review corrected bead-chain snake silhouette with continuous bone-weighted skin and raised MawToad's mouth to remain visible at overhead camera. Five in-place clips, family-specific articulation and only humanoid equipment sockets remain unchanged.
- Root approved deterministic ground-only quarter-turns: uint h=x*0x9e3779b9 ^ y*0x85ebca6b; h^=h>>16; h*=0x7feb352d; h^=h>>15; degrees=(h&3)*90. Python masks32bits after multiplies and uses Blender local-Z=-Unity yaw. Felling ground variant0 inside x31..48,y6..18 remains free of foliage geometry.
- Mirrored Hubble's16 current-water masks in scene previews: cardinal dry N1/E2/S4/W8, radius.1 and8 segments per rounded quarter corner; round only when both adjacent cardinals are dry. Wet shared edges remain exact. No16 additional catalog IDs or water simulation changes.
- Review8 underway for all8 real native scenes; root requested a gameplay-scale crop before adoption. Actual GPU/native/performance results remain parent-owned and are not inferred from Blender triangles.

- First actual Unity import rejected GlowQuartzVein0 with210 imported triangles versus260 source triangles. Source inspection and independent raw FBX analysis both found five collapsed cone-apex rings, each contributing10 zero-area triangles. Corrected the shared cylinder helper to generate a true one-vertex apex with triangular sides and no degenerate cap (also handles a zero-radius bottom). This was a geometry repair, not an importer relaxation.
- Scanned all218 source models before the repair: exactly four quartz variants had50 zero-area triangles each, no other model. After rebuilding, all218 have zero zero-area triangles. Exact catalog change is only four triangle fields260→210; all bounds and214 other FBX bytes preserved. Corrected catalogSHA2086b05ccf5c800ae2a8276a6c96ea337e805a3b9824f18319b5eff0d80bc16a. Hubble independently scanned all218 corrected raw FBXs and found matching counts/zero degenerates.
- First-import renders predate the geometry-only crystal repair; seed729490642's visible eight-scene placements contain no GlowQuartzVein, so the rendered visible geometry is unchanged. Editable hidden model libraries are being rebuilt from corrected source before scene adoption.
- Whole-scene FBX warnings were traced to Blender5.2 exporter warning unconditionally on repeated mesh/material pairs. The shared objects all use identical slot0; individual218 model exports have no such warnings. Optional inspection FBXs remain separate from runtime correctness.
- No mesh LOD or ground-detail filtering is implemented by this art bundle. Runtime Low mode is parent-owned render-target/shadow behavior. Earlier wording suggesting detail filtering was a planned possibility, not a shipped claim.

- Second actual Unity import progressed162 models, then rejected8 nonplanar/self-intersecting8-gon bark caps in stump-main (48 triangulated faces discarded). Independent raw projection checks matched the exact8 caps and bounded other risks to the same root factory. Replaced per-vertex native-mask clamps with uniform convex-outline shrink; exact polygon clipping rejects any positive-area overlap with non-solid native cells. Authored each curved cap as a centre triangle fan and its side strips as triangles. This corrects shape and topology without relaxing Unity validation.
- Re-exported the entire8-model hero family; only7 catalog rows change counts, and southwest-foreground-rock's vertical bounds fall. Other210 runtime FBXs unchanged. Candidate catalogSHA bb7a1f3f60083b19ae2e346d2b22c13a525f505d723d68152d9fa287938d3efb. All218 rebuilt source meshes again have zero zero-area triangles; Hubble independently reviewing caps/raw FBXs before native reimport.

- Parent observed complete actual Unity importPASS218/73/55,zero compile errors andzero discarded/self-intersecting polygon warnings after both repairs. A second identical-source import preserved GUIDs. Those receipts are parent-owned; native integration/adversarial and final visual gates are separate.
- Final actual Blender roundtrip of all218 repaired runtime FBXs passed withzero errors: exacttriangles,UVs,material slots,static bounds(maxaxis delta0.0000035167),21 real skinned rigs,five nonempty clips and exact humanoid sockets. Kit150908 unique source triangles; full scene budgets range316770–1590922. Exact final totals are in reports/asset-budgets.json.
- All8 editable scene libraries rebuilt from corrected frozen source. Felling overhead/crop andoptional inspection FBX refreshed aftercaprepair; other full-zone renders are unchanged because those visible models did not change. A second independent self-contained rebuild is being compared semantically across all218 models and8native manifests; no binaryFBX determinism is claimed.

- Independent self-contained rebuildPASS: all218 normalized source model hashes,complete catalog,palettePNG bytes andall8 native manifest objects match a fresh source run. Geometry normalization includes topology/UVs/smoothing/transforms/materialslots/skinweights/restbones/socket transforms andnamed NLAstrip metadata. Container/FBX binary equality is deliberately not claimed. Exact report: reports/rebuild-verification.json.
