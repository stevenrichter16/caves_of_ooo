# Felling Unity verification

Implementation and player instructions: [Felling layered scene in Unity](../../FELLING-UNITY-SCENE-INTEGRATION.md).

Final native run `3786680f8cee4b168ffa8e43f1454673` passed **120/120 checks**, with zero failures or fatal errors. It exercised 574 ordinary movement steps, 39 clearing actions, three creature examinations, two harvests, one pickup, re-entry/save/load and all seven lore positions. The separate preview leaves the original content intact.

The final intact preview (`c1c92487cfdc43e4bb5453477cbe9d2b`) passed 10/10 setup/access checks and was left playing in Unity. All 55 source owners, three supplemental objects and three animals were present, and the virtual test keyboard was released. See [preview screenshot](native-preview.png) and [preview report](native-preview.json). Its saves are disposable and isolated from the user's normal game.

## Latest evidence

| Evidence | What it establishes |
| --- | --- |
| [Populated Felling tests](populated-felling-unity-green.json) | 254/254 Unity cases passed for scene state, physical approaches, native population, dressing, persistence, imported art and rendering contracts. |
| [Focused existing regressions](focused-regression-green.json) | 151/151 save, render, camera, input and temporary-save-isolation cases passed. |
| [Final presentation tests](final-presentation-green.json) | 29/29 cases passed, including the final 3:2 framing and actor-tint corrections; this group overlaps earlier runs. |
| [Shared-cell menu regressions](pile-context-green.json) | 47/47 cases passed after correcting individual-owner examination from a pile picker. Initial pile summaries and ordinary singleton menus remain covered. |
| [Final camera and GPU tests](letterbox-clear-focused-green.json) | 53/53 cases passed, including actual GPU margin clearing at wide and square shapes, preserved HUD pixels, reuse and cleanup. This group overlaps the earlier presentation runs. |
| [Final live screenshot margins](letterbox-gpu-green.json) | Zero stale pixels in both measured margins on arrival, after clearing and after the new-object interactions. |
| [Export tests](export-green.log) | Nine deterministic producer checks passed. |
| [Imported ordering comparison](renderer-order-raster-check.json) | Original source composition has zero changed pixels after corrected contact/foreground ordering. This does not include game fog or new objects. |
| [Latest full native audit](native-audit.json) | Actual queued keyboard input through ordinary bootstrap, world travel, action menus and F5/F6. Check `failures` and `fatal` for the result; earlier failures remain preserved separately. |
| [Initial Game view](native-initial.png) | Actual Unity rendering of the intact scene, added wildlife and dressing, HUD and normal visibility. |
| [All original props cleared](native-all-removed.png) | Prepared backing after the 39 native clear actions. |
| [Fauna and harvest checkpoint](native-fauna-and-harvest.png) | Native interaction review after the added-object actions. |
| [Seventh position](native-seventh.png) | Final ordinary traversal to the seventh lore position. |

The native scenario uses a disposable save destination established before bootstrap. It restores prior save settings after Play stops. It does not invoke gameplay actions through MCP `execute_code`, teleport the player, or reveal fog. The live editor profiler measurements include traversal, menus, save/load, captures and audit observation; they are not a standalone build benchmark.

## Preserved failures and limits

`first-native-audit.json` retains a failed movement-key run; `second-native-audit.json` retains the audit path's incorrect collision query; `third-native-audit.json` retains the genuine selected-creature Examine failure on a shared cell. Their causes and corrections are described in the implementation plan. Red compile and regression artifacts are retained beside the passing results.

`broader-regression-initial.json` is a failed broader 279-case run. Recorded stacks point to missing container blueprints in unchanged minimal world-map fixtures. No clean-HEAD rerun was performed, and no full-repository green result is claimed.

Fixed trunk/root structures remain examinable but are not removable: complete hidden geometry is not authored. Normal unexplored ground remains hidden, and existing gameplay overlays still render. Exterior tepuibone drops use ordinary item presentation beyond the authored artwork.
