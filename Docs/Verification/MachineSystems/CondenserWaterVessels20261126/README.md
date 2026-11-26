# Carry a steam catch from Sodden to Gleaners

The ordinary seed 64 route continued a retained generation 25 world. The
player had made condenser 9208 at the real Cinderhold forge. At Gleaners
the player took loose waterskin 2124 and gathered the standing drawgourd.
That yielded shell 9210 and seed 9211. An existing healing tonic brought
the player to 38 HP. The route kept the 28-dram purse.

| Tick | Actual GameBootstrap result |
| --- | --- |
| 1740 | Fresh Continue kept the earned condenser, seed and both empty 3-drink vessels. |
| 1860 | Ordinary overland travel reached Sodden at `Overworld.15.5.0`. |
| 2100 | At `63,5`, the native menu of vent 11640 offered a one-measure catch into each vessel. Highlighting the row was free. Confirming a catch cost one action. |
| 2110 | The waterskin held 1/3, the shell remained empty, and the connected stock on vent 11639 held 3/4. |
| 2120 | The shell held 1/3, the waterskin still held 1/3, and the same stock held 2/4. F5 and fresh Continue kept both vessels, stock and 32 HP. |
| 2350 | After walking back to Gleaners, the player planted seed 9211 at `46,4`. The resulting crop 11658 had zero moisture and zero growth. |
| 2360 | The usual watering action spent the waterskin’s one drink. Growth reached one and moisture reached 39. Fresh Continue kept the crop, the empty waterskin and the shell’s remaining drink. |

A bandfrog dealt three damage during the approach and three more after
the first catch. The guided round trip used ordinary keyboard travel,
the game's menus and saved inventory. It measures one existing vent
group and one existing garden, in a retained world layout.

## Direct fill and the same steam supply

The first playtest exposed a second row labelled “fill the waterskin
from the steam vent.” In a disposable fork from tick 2120, that row
filled the waterskin from 1/3 to 3/3 in one action. The shared stock
stayed at 2/4. The vent's renewing wet coating was being treated as a
spring. The vapor emitter lacks a pool or well. That disposable fork
ended at tick 2130. Separate earned archives retain the catch and crop.

Direct filling from a coating now checks whether the source is vapor.
Actual pools and wells still use their physical water rules, and ordinary
liquid springs keep their existing fill action. The steam condenser,
scraper and settling tray compete for the same stock. That stock is saved
and recovers one catch per game day. No additional storage or timer is
needed.

With the correction loaded, the same tick-2120 save offered two condenser
choices and omitted the direct fill row. Cancelling kept the tick, vessels
and supply unchanged. Two more selections left both vessels at 2/3, spent
two separate actions and exhausted the same stock. The early menu allowed
two choices. The exhausted menu had no water action, even though both
vessels still had capacity. F5 and fresh Continue at 2140 kept the spent
stock, both exact vessels and 32 HP.

## Verification

The direct-fill tests first produced seven expected failures. Ordinary
spring fill and the existing fixture cases passed. The final isolated
Unity run passed 571 of 571 native cases, including the connected
inventory, water, crop and field tool regressions. The Editor compiled
the corrected runtime before the fixed-world replay. A separate full
regression uses the earlier pinned commit `ee0287659`. The latest Mac
package predates this change. Independent source and recorded-play
review checked the exact save bodies, test results and three captures.

The four native files at each checkpoint matched their archive receipts.
The tracked comparison of 861 normal save files and 86 pre-existing
foreign files had zero mismatches after the session. Save isolation and
the temporary test keyboard were cleared after native play.

- Both vessels compete for the same remaining catches. [Steam vent menu](shared-catches.png)
- The player selected the waterskin from two watering options. [Garden watering menu](crop-watering.png)
- The vent remains accessible after the catch supply is spent. [Exhausted vent](catch-exhausted.png)
- Exact actors, holders, quantities, menus and fresh-load results. [Native observations](observations.json)
- Source, test, archive and capture hashes. [Verification receipt](receipt.json)
