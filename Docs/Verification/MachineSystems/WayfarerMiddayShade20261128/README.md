# A wayfaring merchant uses an awning

November 28, 2026, America/Chicago. Unity 6000.3.4f1 on Apple M5.

The comparison used the same earned seed 65 Continue at tick 2930 in
Overworld.19.14.0. The original encounter had a wayfaring merchant at
(44,16). Each isolated copy received one authored CampAwning. The save
supplied the terrain, merchant, original stock and clock. A separate
Wellmeet visit confirmed that the usual village placement restriction
makes the wilderness the appropriate place for this trial.

| Branch | Observed outcome |
| --- | --- |
| Original rules, 2950 | One ordinary diagonal move and Set Down placed awning 42025 at (42,16). The player at (41,15) gained shade. The merchant remained uncovered at (44,16). |
| New routine, 2950 | The same actions left the same kit standing and the same player under it. The exact merchant stepped west to (43,16) into its shade. Both actors kept 38 and 20 HP respectively. |
| Examine and trade, 2960 | One ordinary east step put the player at (42,15), also under the awning. Examine reported that the merchant was resting in shade. Chat opened trade with the original healing tonic, two dried meat and carriage contract. The original 50 and 500 drams stayed in their respective purses. |
| Continue and Take Up, 2960 to 2970 | A fresh isolated Continue kept the two positions and the exact standing awning. Take Up moved awning 42025 back to the pack on one action. Both people lost its cover and the same merchant walked east to his original spot. |

The merchant checks reachable shade during Height while idle and bareheaded.
It takes one checked step, considering shelter within two steps and four
cells of its ordinary post. Roofs and installed shade use the existing
glare geometry. The step checks current footing and action state again
after movement callbacks. Conversation and trade use the merchant’s
current position. Homeward travel resumes through the existing routine
when shade goes away or the band ends. That routine retains its original
path rules. The trial used clear ground for the return.

For older saves, the active Beating encounter’s matching player and merchant
receipts attach the routine to the exact actor. Stock and position carry
through the existing save. This adds an idle choice without assigning sun
debt or consuming water. Tent-Right travel and hospitality inform the place
context. The ordinary encounter supplies the actor’s existing affiliations.

Ten focused cases failed on the earlier runtime at the expected missing
movement assertion. The final focused run passed 141 cases. Independent
source and gameplay reviews checked action order, ownership, all three
stock identities and the exact move and reload. The first live attempt was
discarded after Unity reloaded scripts. The completed replay started again
from the pinned source. All 880 ordinary Quick files and 87 unrelated
tracked paths matched the initial hashes. This trial covers one merchant
type in Unity. Broader enrollment and runtime profiling remain open.

- [Original merchant stays beyond the awning](01-previous-traveller-outside-shade.png)
- [Examine explains the merchant’s move](02-wayfarer-in-shade.png)
- [Trade under the awning](03-trade-under-awning.png)
- [The merchant’s original position after Take Up](04-back-at-the-original-post.png)
- [Native observations and exact archive receipts](observations.json)

## Mac package

The universal runtime at
`/Users/steven/coo-verification/builds/mac-af2414486-20261128/CavesOfOoo.app`
includes the Gantry consignment and the wayfarer routine. The existing
build workspace pinned 32,296 source inputs. The build completed in
23.55 seconds with zero errors and four warnings. The package has six
arm64 and x86_64 binaries. Gameplay was checked on Apple M5.

Native Continue opened the saved branch under the awning at 2960. The
player chose Take Up in the world menu and saved at 2970. An independent
read of the private save matched the Editor branch's actor positions,
health, stock, purses and exact packed kit. A fresh standalone process
continued the packed branch with the player at (42,15) and 38 HP, and
the merchant back at (44,16). The first attempt captured only the boot
prompt. The retained image is from the subsequent completed Continue.

Both processes used a private save folder. The packed result and
archived input were verified. Seven retained apps, both normal
preferences and 862 files in the two ordinary Saves folders matched
their initial bytes. Physical Intel play remains unverified.

[Fresh Mac Continue after Take Up](05-mac-continued-packed-awning.png) ·
[Build and preservation receipt](mac-observations.json)
