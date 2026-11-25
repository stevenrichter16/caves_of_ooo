# Compatible lining clay at the Peat Works rig

November 25, 2026 UTC. The existing Peat Works field rig accepts two
measures of fire clay, prepared lining clay or a mix. Its recipe still
requires the same masonry surface and quantity. The action, fault text and
structured Examine fact read the same bill. The original well and pan
Examine facts also reflect their supported alternative. Exact-only bills
such as the dressing bench retain their original requirement.

The change reuses the shared material matcher and saved repair result.
The clay item templates, station handling and associated costs are the
same. The rig needs an ordinary separate repair before it supplies forge
service. It uses the same reclamation rule and accounts for the binding.

## Actual Editor play

Seed 64 continued the earned wayside purchase at tick 190. It held two
prepared measures `6854` bought from Orven and zero fire clay. Ordinary
world-map travel reached the unvisited Peat Works at tick 310. The player
remained at 40 HP and reached the north shed through normal movement.

| Tick | Observed result |
| --- | --- |
| 570 | Ordinary locker access took the exact peat head `9348`, oak haft `9350` and leather binding `9351`. Its boots, buckler, cord, screen, hood and apron stayed inside. Ordinary pack crafting made mallet `9354` while no usable forge was near. |
| 620 | Beside the faulted rig `7420`, the mallet’s ordinary forge reclaim action refused. Carried clay, mallet, rig, actor and clock matched the pre-attempt state. |
| 620 | Native Examine showed the same mixed-grade repair bill as the ordinary world action. Reading cost no game time. |
| 630 | The normal repair spent both purchased measures and made that exact rig usable in place. The mallet stayed in the pack. |
| 640 | The same reclaim action spent the mallet and its binding and returned one head `9355` and one haft `9356`. The exact rig remained in service. |
| 640 | Fresh Editor Play and Continue retained the restored rig, exact recovered components, untouched locker remainder, player and clock. |

The two compatible grades were tested together in Unity. Actual play used
the earned prepared grade. Legacy direct fire-clay repair has its own prior
play receipt. The current test character and authored site used normal
movement, acquisition, preparation and station actions.

- [Same bill in the native Examine panel](01-rig-examine.png)
- [Ordinary reclamation from the mallet](02-rig-reclaim.png)
- [Earlier rig play](../FieldWorkRig/)

## Native and independent verification

The first compiling RED passed 65 of 67. Only prepared-only and mixed
repair failed. A later reader RED passed two of four and reproduced the
old fire-clay-only fact for the rig and well. The final connected suite
passed 275 of 276 with zero compiler errors. Its only failure is the
existing missing 3D model for `AbandonedFieldWorkRig` at Peat Works,
reproduced on the former content with the same message. Graphics remain
parked. The new cases cover normal action, both grades and a mixture,
wrong grade and spoil rejection, exact-only timber and the shared facts.

Independent review verified the three archives, exact serialized inputs and
outputs, Editor log, native failure messages and the two screenshots. The
86 selected foreign and 861 selected normal file bodies matched.

The inherited full-project regression remains open. The current Mac
package qualifies the preceding clay-tray and knotflax milestone. The
materials and Examine changes above have separate Editor evidence.
