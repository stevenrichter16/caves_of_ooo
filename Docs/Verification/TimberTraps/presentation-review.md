# Timber trap presentation review

Original Caves of Ooo cuboid geometry, using the existing reference-glade palette/material. No borrowed models, new shader, collider, physics owner or character rig.

## Source and authority

- Eight new jammed variants supplement the existing 56 scenery models. `art-source-diff.json` compares parsed baseline `fb4d5bcd7` with this source: every original model definition, the palette and schema are unchanged.
- The same exact supported trap owner selects its jammed mesh from `TrapJammingPart.Jammed`. Missing-Part legacy traps keep their original armed model. A malformed opt-in refuses an approved model instead of showing a misleading safe state.
- Normal one-shot activation still removes SpikeTrap, BearTrap and FireTrap owners/views; the ordinary unjammed PressurePlate stays armed. No fictitious spent debris is added.
- Existing player Interact animation is driven by the real transaction's post-commit hook. Presentation does not pay inventory, suppress a trigger, or create gameplay state.
- Dynamic Examine describes the current mechanism and nearby/focused hints use the ordinary action menu. The hint queries current, visible, in-reach owners without dispatching their events.

## Offline visual inspection

`trap-state-gallery.png` shows armed variants on the left and jammed variants on the right. Inspected the rendered image: broad tan timber clearly contrasts with the dark steel; spike teeth sit depressed, a board crosses bear jaws, the fire actuator is braced outside its vents, and a timber wedge lifts the pressure-plate edge. The four mechanisms remain visually distinct. The counterpart variants mirror small details without consuming game RNG.

Reproduce with the installed Blender executable, `--background --factory-startup --python ArtSource/SpreadScenery3D/preview_kit.py -- --traps-only --output Docs/Verification/TimberTraps`. The gallery uses two rendering threads and 24 samples. It is source-geometry inspection, not proof of Unity import, normal camera readability, fog, current owner submission or action timing.

## Native gates

`TrapJammingPresentationTests` was published before production for root's native RED. Its 25 cases cover armed/jammed identity pairs, current Examine, all eight actual imported meshes, actual resource command/gesture/mesh/safe crossing/replacement save, legacy firing/removal, malformed appearance refusals and pure current hints. Controlled fixture tests disclose their placement and visibility setup. Root records native results and ordinary journey/pixel acceptance separately in `TIMBER-TRAP-DESIGN.md`.

Importer menu: **Caves Of Ooo / Art / Import Timber Trap States**. The new receipt destination is `Docs/Verification/TimberTraps/native-art-import.json`; the historical DensityCompletion import receipt is preserved.

## Cold review: Q1–Q4

- **Q1, symmetry:** both variants retain the same owner and original trigger. Armed→jammed changes only mesh selection; a controlled flag reversal returns the original armed ID. Actual unjammed one-shot removal and repeatable pressure-plate behavior remain intact. Missing-Part legacy owners keep the armed path.
- **Q2, consistency:** all four exact blueprint/trigger pairs share `TrapJammingPart.IsSupported`, eight IDs use one naming convention, and the eighth command/hint row aligns at index 7. The pure hint calls the same eligibility query as the menu and cannot supply payment or execute events. The dynamic reader is appended at the same stage as structural-repair state, before location flavor.
- **Q3, counters:** each trap's real command is paired with an unmodified legacy trigger, same-owner armed/jammed variants are paired, saved replacement owners retain the mesh while the stale reference loses its approved view, and hidden/foreign/custom appearance cases cannot borrow safe art. The core adversarial suite additionally covers duplicate jamming/trigger Parts and wrong mechanisms; the recipe delegates to that exact guard. No new significant authority gap found in this pass.
- **Q4, drift finding (notable):** the generated trapper-store flavor said unconditionally that stepping on the teeth springs the trap, conflicting with the new jammed-state readout. Resolved after root-observed generated-source RED: conditional prose passed in the 641-case native GREEN sweep and appeared in the actual native reader screenshot.
- **Scope limitation:** the existing `SpreadPresentationScope` still governs 3D trap art (Spread surface, the glade cellar and admitted Spread lair floors). The action/readout works on supported newly instantiated traps elsewhere, but this change does not expand other biomes' renderer coverage. No claim of a new global trap-art system.

`new-art-files.json` lists the exact 32 newly imported mesh/prefab/meta paths for explicit staging, plus the existing library asset that changed. All 16 new asset GUIDs are well-formed and mutually distinct. The importer left the original model asset files unchanged.

## Ordinary native pixel acceptance

Root inspected the four screenshots from run `77d7e740bb62465eaaad6b32ce22b1c6`: armed reader, selected Jam action, jammed same owner and restored trap underfoot. The tan board is visible in the normal camera; the reader and menu clearly name the one-timber permanent cost. Actual mesh receipts separately confirm the original SpikeTrap owner switches to the jammed model. The player partly occludes the restored trap; this is not an all-angles or all-four-mechanism native camera proof. The source gallery covers the four mechanism shapes.
