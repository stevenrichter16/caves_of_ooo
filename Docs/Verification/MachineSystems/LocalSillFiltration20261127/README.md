# Local and freight sand give Sill the same well repair

November 27, 2026, America/Chicago.

The existing keeper accepts one measure of silver sand with the well
manual. In seed 65, one unit gathered at Sill paid for the same lasting
well condition as the freight reward carried from Sumphold in seed 2.
Both branches used the existing dialogue and repair. This comparison
adds gameplay evidence for the alternate sources.

| Route | Source and payment | Result |
| --- | --- | --- |
| Local bank, seed 65 | Bought flask 9873 for 19 drams. Drained the finite settling pocket into it and gathered exact silver sand 10241. Dovrin spent that sand after the elder supplied manual 10242. | Existing well 9981 reached `StableRepair`, outcome `Stable`, through `ManualRepair` with no relapse time. |
| Sumphold freight, seed 2 | Carried earned consignment reward sand 15454. Perret spent it after the elder supplied manual 18497. | Existing well 18278 reached the same stable condition and method with no relapse time. |

[Freight route evidence](../SillEarnedSandFiltration20261126/README.md)

## Ordinary local play

1. Continued the seed 65 Duelist at 520 with 50 drams and no sand or
   flask. Traveled to Sill at 780 and bought the real provisioner's
   empty flask for 19. Held no consignment reward or supplied item.
2. From (42,17), filled that flask with all twelve units from the pocket
   and then gathered its single exposed measure at 980. Kept both the
   water and the sand. The native pocket stayed spent.
3. Used the cell picker to talk to the elder sharing his straw bed's
   tile. The ordinary dialogue yielded the maintenance manual at 1360.
   Dovrin took the exact sand at 1500. The original well immediately
   read as repaired, with the same follow-up maintenance conversation
   previously verified on the freight route.
   [Repair offer](01-local-sand-repair-offer.png)
4. Walked back to the original well and drank. Fresh Continue at 1850
   kept the stable repair, 31 drams, 40 HP, the same manual, and all
   twelve units in the flask. The well still offered Draw water.
   [Repaired well after Continue](02-local-repair-after-continue.png)

This guided Flat Editor comparison used no spawned equipment, modified
stats, clock adjustment or skipped travel. It verifies one local and one
freight branch. Both ordinary draws had the standard cool-water result.
The wider range of layouts and water effects remain open.

Independent review checked the two routes and the raw saves. All eight
private checkpoint hashes, 876 normal quick-save hashes and 87
pre-existing tracked modifications matched. The Editor returned to its
usual input and save root. Runtime `bac91380a` is unchanged by this
verification record.

[Actual observations](observations.json) · [Save receipt](receipt.json)
