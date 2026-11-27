# Loose materials show when they finish drying

A dropped wet item now repaints its own visible cell when its wet effect
ends. Stationary waiting shows its existing dry color immediately.
The change uses the same effect removal and world rendering as other
loose items. Hidden, carried and equipped items do not repaint a world
cell, and an old graph or replaced cell cannot redirect the refresh.

The native Editor comparison supplied one 25% wet clay-and-grit packet
to a retained camp fixture with its original equipment and 343/400 HP.
The player dropped packet 2731 through the inventory and stepped aside.
At tick 2420 it was still wet and blue. Ordinary stationary waits dried
it by 2600. The prior version kept painting blue until a control redraw.
From the same saved start, the corrected version changed to red without
movement, a menu or a forced redraw. Fresh Continue retained the exact
dry packet and its red appearance at the same tile.

[Wet packet](01-wet.png) · [Dry packet after ordinary waits](02-dry-fixed.png)

The final focused native run passed all 126 cases. The corrected RED
failed at the expected missing cell refresh with nine controls passing.
An earlier RED had an incorrect evaporation timing assertion, corrected
before the intended failure was established. The final controls cover
the transition, partial drying, fog, custody, scenery and a stale cell.

All twelve archived save files are pinned. A fresh comparison matched
860 scoped normal files, with Unity batch TestResults.xml excluded.
The isolated root and input device were released after the fresh read.
Independent review checked the two captures, exact packet identity,
the linked backups, the native receipts and the same 860 file hashes.
[Observations](observations.json) · [Save hashes](pins.json) ·
[Normal file comparison](normal-comparison.json)

This verifies a supplied packet on one Flat-mode layout in the Editor.
The earlier earned wet-packet checkpoint in the temporary folder was
absent after external cleanup. Acquisition and screening retain their
earlier separate evidence. The current Mac package predates this fix.
