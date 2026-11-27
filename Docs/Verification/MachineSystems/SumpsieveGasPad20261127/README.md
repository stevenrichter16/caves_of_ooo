# The spare sumpsieve pad clears Brine Camp marsh gas

The seed-65 traveler carried one pad grown with water from the
recovered camp still. At Brine Camp the player used the original
lit torch on the existing mire at (44,12). The burning mire
released a cloud of marsh gas. A normal step into the cloud
applied ten turns of gas poison at two damage per turn. Heat,
burning and exposure all contributed to the damage on the way
out. The traveler reached the world map with 11 of 42 HP, nine
gas turns and one turn of burning.

| Test | After one action at tick 7170 | Original pad |
| --- | --- | --- |
| Apply from the live injury | 11 HP, gas cleared, charred | Spent |
| Wait after fresh Continue of the saved injury | 9 HP, eight gas turns, charred | Retained |

Both actions advanced the same amount of game time and left the
same lit torch equipped with the same fuel. Burning expired on
both branches. Fresh Continue from the treated save kept 11 HP,
the cleared poison, the exact pack and equipment, the consumed pad,
and the separate charred effect.

Fresh Continue of the untreated save kept the gas type, damage,
duration, location and pad. It lost the effect’s source ID for
the mire. Source review found that the effect uses a second
field named `Owner` for the gas creator and the save filter
excludes fields with that name. This remains open.

[Gas exposure](01-marsh-gas-exposure.png) · [Apply](02-apply-sumpsieve-pad.png) ·
[After treatment](03-poison-cleared.png) · [Observed state and save hashes](observations.json)

## Verification

The native GameBootstrap playtest used ordinary movement, torch
interaction and the inventory Apply action at runtime `4d90fb4af`.
An independent review checked the action receipts, screenshots
and three saved states. The playtest used isolated copies. All
876 checked normal save files and 87 pre-existing tracked edits
matched their earlier hashes. The Editor exited Play Mode and
its ordinary input settings and save location were restored.
