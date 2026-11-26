# Taking a reserved component back at Cinderhold

The ordinary seed 2, generation 31 player returned from Morrowfast
with the previously earned iron spike and two measures of fire clay.
The native forge menu set the spike aside for crafting. In the pack,
the hearth nozzle and steam condenser stayed visible as unavailable.
Selecting the nozzle row explained how to use **Take back (crafting)**.
It spent no materials or game time. Five other recipes still reported
missing cord or binding.

F5 and fresh Continue at turn 3490 kept the mark, the exact spike, both
clay measures and 40 HP. Using **Take back (crafting)** in the item menu
removed the mark. Both recipes returned with their normal costs. The
same spike and clay were saved again. No new nozzle or condenser was made
in this trial.

[Reserved recipe and explanation](marked.png) ·
[Recipes available again](available.png) ·
[Observed states and test pins](observations.json) ·
[Isolated save pins](save-pins.json)

## Native checks

The original test failed because the unavailable row was missing.
The updated suite passed 290 cases. It includes both recipe successes,
stale quote rejection, a true shortage, other noncanonical item data and
a foreign marker. Two craft-availability UI cases also failed in the
unchanged-source comparison, with the same assertions. The full suite
has a separate pending gate. Independent source review found no
remaining blocking or high-priority issue. The editor stopped with its
save override cleared, and all 861 normal and 86 pre-existing workspace
file hashes matched.

## The earned nozzle still needs a hearth

A separate ordinary Continue brought the earned nozzle to Gantry, Posy,
Sill and Gleaners. Actual traders in the first two towns carried flint
strikers. The inspected stock in those four towns had no camp hearth.
Attempts to approach a Sill seller did not reach their trade panel, so
no live price was measured. The character bought nothing and the earned
nozzle save was preserved. A nearby owned hearth and natural target
remain open for the field-use trial.
