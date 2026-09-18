# Morrowfast verification — 2026-09-06

The current exports pass **9 Python extraction tests and 25 Node preview tests**, with zero failures or skips. The separate exported-PNG audit reads the written files without regenerating them. It reconstructs all **1,572,864 pixels** of the approved source with **zero differences** and confirms no backing changes outside declared source ownership.

| Evidence | Result |
| --- | --- |
| `producer-final.log` | 9/9 extraction contracts: source RGB/alpha, disconnected parts, overlap rejection, source/support isolation, unknown IDs, wall protection, cistern cast-shadow handling and uniform world mapping |
| `preview-final.log` | 25/25 preview contracts, including actual asset dimensions/references, closed interiors, every-owner approaches, safe state restoration and dynamic bridge support |
| `export-audit.json` / `.log` |Independent PNG reconstruction, RGBA/alpha/bounds, source-layer pixel identity, owner links, masks and 66 removal inspections |
| `asset-hashes.json` |SHA256 of every generated PNG in `build/`, anchored to the approved source hash |
| `visual-review.md` |Independent review of repaired surfaces, extraction edges and representative removals |
| `build-final.log` |Producer counts and eight front/back occlusion repair records; no source reconstruction differences |

Earlier `*-red.log`, `build-initial.log`, `preview-polish.log` and related logs retain real failures that drove corrections. Their failure status is historical; the current results are the files named above. In particular, two interior approach failures were corrected by moving the stool and reserve cord inside their existing floors; bridge edge-support cells were reconciled with the actual walk grid. The cistern correction has its own RED→GREEN test so a removed fixture's dark shadow is not transferred onto inferred ground.

Browser checks used the actual localhost workbench: 71 owners and 145 layers loaded; a keeper roof revealed its room; the separate door opened; keyboard movement advanced the surveyor; all-roof reveal exposed furniture; Nemm's card displayed and his body removed independently; component search found Sella and her proposed food/tonic stock. State survived reload. Final browser state is reset to the intact scene. No browser console errors were observed during these checks.

These results verify source art and the authoring preview. No new Morrowfast Unity zone was installed or played during this pass. Single-pose inhabitants, native AI/trade/quest behavior and the 80×25 game collision map remain integration work. Small ground flecks, perimeter fences and outer rock masses remain fixed environmental texture.
