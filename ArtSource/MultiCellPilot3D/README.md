# South-spawn Living Woodcut pilot art

Status: v8 native integration and final 56° camera accepted, 2026-09-10. Offline/source/FBX gates passed; MC26 passed all 416 added tests with exactly 31 pre-existing failures. The final native workload, cleanup, and three-image look review passed. CoO-original visual design,
no Qud parity claim. Runtime mechanics and Unity verification are parent-owned
in Docs/MULTI-CELL-PILOT.md. Generation writes outside Assets; the parent coordinates each Unity import.
The rendering cold-eye, adversarial checks, actual native acceptance, and remaining art/base-pose limits are recorded in REVIEW.md.

The user reference establishes warm rose fossil woodstone, transverse grain,
broken radiating ridges, dark tar, sparse ochre growth, a western enclosure,
and copper vents to the east. The blueprint and footprint layout is authoritative;
the reference is not a baked scene plane. Tepuibone is mineral seam, not bones.

## Verified contracts before implementation

| Assumption | Verified implementation decision |
| --- | --- |
| Blender available | /Applications/Blender.app reports 5.2.1 LTS. bpy is bundled and invoked in background mode. |
| Shared source axes | Existing mesh_kit.py uses X east, Y north, Z up and an export-only Z180 parent. Retained. |
| Existing SpawnRing3D palette is extensible | Native library validates its exact catalog; this pilot needs separate model/material bindings. It cannot append unadvertised models to the old library. |
| All reuse identities are in Objects.json | GrainRidge, Rock, TarSeep, CopperPipe, SteamVent, TepuiboneVein, MawToad, Wardline, CaveHermit, TepuiStone verified. |
| One-cell destructibility | Large art carries one explicit footprint per model; rock/lichen attached to the ridge are one object, removed together. Ground grain is paint. |
| Reference bone fence | Deliberately translated into flush tepuibone mineral inclusions. |

## Contract and gates

`layout.json`: {zoneId,width,height,placements:[{id,modelId,blueprint,x,y,
footprint:[{x,y}],cellsRaw,role,solid,opaque,destructible}]}.
One unit per cell. Anchors are cell centres. Local Blender (dx,-dy,z) becomes
Unity (dx,z,-dy). No per-placement rotations or scaling. Footprints have unique
integer cells. The x39..41 north/south corridor stays unblocked, and two east-west
crossways stay open. Models must fit their footprint in plan, including attached
rocks/vegetation. Four variants for repeatedly stamped families.

TDD artifact contract: validator is written and run before generating any model.
Following build: run asset/layout validation, source triangle/footprint inspection,
FBX reimport geometry/UV and orientation checks, render whole scene and crops,
then cold-eye review. Actual Unity import, native gameplay, fog and performance
remain separate gates; Blender appearance is never described as a native result.

## Delivered bundle

51 real FBX models, 149 independent placements, editable kit/scene Blender files,
per-cell world-UV mineral albedo, three actor rigs, and four native 16×16 icons.
The current source and output are v8. The frozen importer input is
`/tmp/coo-pilot-import-v8`; its SHA256 manifest records the exact reviewed
geometry/material bundle. Actual Unity v8 import passed all 51 models and preserved borrowed assets;
see `Docs/Verification/MultiCellPilot/unity-import-v8.json`. Full/native checks
are parent-owned and must be read from their actual receipts, not inferred
from these previews.
REVIEW.md records verified gates and honesty bounds.

Final acceptance: native run `da0e5e856474438e96c7b6436b7cbd42` passed 26 workload
gates, cleanup, and 315 independent evidence checks. The final north-row player
fits with visible headroom; player/pipe readability and map/HUD separation were
inspected in all three real 56° captures. See the [native receipt](../../Docs/Verification/MultiCellPilot/MCN-da0e5e856474438e96c7b6436b7cbd42-native.json)
and REVIEW.md for limitations. The Blender overhead previews remain art reviews,
not substitutes for those actual GameView images.


Surface polish follow-up: the accepted editable master and model FBXs now include the reviewed sculpt-normal pass. See `../ModelPolish3D/README.md` for the required fresh-build → polish → validate pipeline, original archive and preservation limits. `renders/polish-before.png` / `polish-after.png` are matched Blender galleries, not native screenshots. Per-model decisions are in `reports/polish.json`.
