# A dry shortcut at the Peat Works

Fresh version 27 can place two 12-unit pools of shallow bog mire in an
existing break in the north shed wall. Each pool uses the ordinary finite
poured-liquid owner. From the north approach, clearing a gap must save at
least three actions after counting the transfer. The existing southern
entrance must retain the same dry walking distance. Geometry that fails
either check keeps its ordinary layout.

The one-pool proposal gave no practical saving in seed 64. Its neighboring
gap already offered an equally short dry path. The chosen pair lets a
traveler use the dry detour, carry away one pool with an empty flask,
move one pool to a clear outlet with the hand pump, or wade through with
ordinary mire exposure. The remaining pool and the relocated stock keep
their usual consequences.

## Editor play from the same north entry

The new seed 64 game bought its original empty flask from Sill’s
provisioner for 22 drams. It travelled through the northern stretch of
Sodden and crossed the local edge into the Works at tick 420. The three
branches started at the same saved cell `(40,0)` with 40 HP and 28 drams.
They reached adjacent cells on either side of the same field work rig at
`(56,5)`.

| Method | Actual actions | Visible outcome |
| --- | --- | --- |
| Dry detour | 23 steps, ending at 650 | Both pools and the empty flask persisted. The player took 18 HP from snake attacks and bleeding along the route. |
| Ordinary flask | 12 steps, one fill, four steps, ending at 590 | The flask took 12 units. The selected pool disappeared and the other still held all 12. The player reached the rig at 40 HP. |
| Hand pump | 12 steps, one stroke, four steps, ending at 590 | The selected pool disappeared and the clear outlet at `(53,2)` held its 12 units. The neighboring pool and empty flask remained. |

The flask was earned within this playthrough. A single canonical pump was
supplied only to its separate copy of the common save. A previous
playthrough verified ordinary pump acquisition through the forge.
The native rig reader still showed its actual cracked surface and
compatible repair bill. Wildlife can change the cost of either route, as
the detour encounter shows.
[Ordinary pump acquisition](../OrdinaryHandPumpAcquisition20261125/README.md)

Fresh Continue preserved both cleared branches. On the flask reload a
snake stood on the cleared cell while the terrain itself remained dry.
The pump reload kept the relocated pool and its coating at the outlet.

In a separate new seed 68 world, the Works had no optional pools. The
player took 15 ordinary steps from the southern approach and reached
the same rig at full health with their starting purse.

## Native coverage

The first compiling run had 33 passes and two expected missing-pool
failures. The subsequent connected run exposed weather checks whose
fixtures assumed the latest generation was always version 26. Literal
older-version cases now select their stated versions. The final suite
passed all 134 cases with zero compiler errors.

Generated seed 64 and 65 checks place different pairs in the realized
wall. Other cases exercise the exact flask and pump commands, stale
commands, saved pool removal and retained outlet state, old manifests,
and missing or altered pool definitions. Independent source and recorded
play review checked the native routes and the saved physical states.
The universal Mac package predates this change. Route discovery and
wider cross-seed play remain open.

[Dry detour](01-dry-detour.png) · [Native fill menu](02-native-fill.png) ·
[Rig reader](03-rig-reader.png) · [Native pump menu](04-native-pump.png)

[Native observations](observations.json) · [Save and test hashes](receipt.json)
