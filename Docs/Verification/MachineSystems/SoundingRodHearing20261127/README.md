# A sounding rod strike drew a Pale Stalker

The pack and underfoot menus now label the number as survey reach.
The action preview and Examine guide share a separate warning that
the strike can draw nearby creatures that hunt by sound. The reading
gives directions and rough distances for terrain, including a bore
through stone. It gives no direction of travel for the bore.

## Same cave, same beat

The existing sounding rod scenario supplied one rod `3457` and
started with 40 HP on depth one in seed 64. Ordinary movement and
stairs reached depth five. A naturally generated Pale Stalker
`18658` had pursued the traveler during the approach. Waiting quietly
let its search end. The comparison started from one saved tick 330
at `(20,11)` in `Overworld.10.11.5`. The stalker was three cells away
at `(23,10)`, wandering with no target. Both fresh Continues matched
the same positions, timing, prior noise and empty notebook.

| One action from tick 330 | At tick 340 |
|---|---|
| Quiet wait | The stalker moved to `(24,10)` and continued wandering with no target. The old noise stamp and empty notebook stayed. |
| Sound the ground | The rod recorded “Open ground lies northwest, close.” The strike stamped fresh noise. The same stalker moved to `(21,11)`, adjacent to the traveler, and started pursuit. |

The traveler stayed on the same tile with 40 HP in both branches.
The 14-pace survey description was visible before spending the action.
Fresh Continue kept the recorded lead, the exact rod, the noise
stamp and the stalker's pursuit. Reading the notebook from the pack
took no time and changed none of those observations.

## Verification

Four native cases reproduced the missing or ambiguous wording while
68 controls passed. The final run passed 171 cases. One interim
assertion was corrected to allow the underfoot menu's existing tool
suffix. Independent review checked the source, native results, exact
matched saves and recorded play. This comparison used the supplied
scenario rod and a natural stalker in the Editor. Ordinary purchase
was verified in earlier work. All 876 checked normal save files and
87 pre-existing tracked edits matched after the playtest.

[Examine](01-rod-f1.png) · [Quiet wait](02-quiet-wait.png) ·
[Survey action](03-survey-action.png) · [After the strike](04-rod-pursuit.png) ·
[Notebook](05-survey-notebook.png) · [Observations and save hashes](observations.json)
