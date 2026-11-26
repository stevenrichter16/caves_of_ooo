# Vent scale from the Causeway becomes a wearable heat coat

The existing linked vent now explains what its pale mineral crust can
become. The reader adds the clue only on the marked Causeway surface
vent. It also works on a loaded version 29 vent with exhausted stock.
The save's authored description and the resource balance stay intact.

Flasks that contain proofing now list garment treatments immediately
after Examine in the ordinary pack menu. Worn gear is listed first.
Each garment keeps its own choice and both kinds of pour remain below.
Ordinary flasks use the usual liquid handling menu.

## Native play

The two private copies began at seed 64, version 29, tick 460 at
the Causeway with four scales harvested in the earlier native trial.
That trial supplied the scraper, condenser and flasks. A separate
[ordinary trade and crafting route](../OrdinaryCondenser20261125/README.md)
established how the condenser and first flask can be acquired.

- The earlier menu showed the cloak at choice 19 of 22. In the new
  menu it was choice 3, after Examine and the worn buckler.
- Three ordinary steps reached the dry shoulder. Examine showed the
  scale's use at tick 490. Vent `7635` stayed at zero of five catches
  with the same regeneration timestamp. The available dry route
  requires no coat or tool.
- Pressing spent one of four earned scales and filled exact flask
  `11160` with one heat-proofing. Coating the original worn cloak
  `2709` spent the liquid, left the flask empty for reuse, and advanced
  the clock by one action. Flask `11154` kept its earned unit of water.
- The coat gave +35 Heat Resistance for 100 worn turns. In the
  duration comparison the resistance dropped to zero off the body
  and three waits left its duration unchanged. Wearing it again
  restored +35. Three more worn turns left 97 turns on the current
  garment. Both heat-coat comparisons stayed at 40 HP.
- Fresh Continue at tick 540 restored the same worn cloak and its 97
  turns, the three remaining scales, empty receiving flask, original
  water and exhausted vent.

Fire-moss can supply the same temporary treatment through the same
press and garment actions. The separate water route spends that same
vent's stock on portable water. This trial measures coat behavior and
actual ownership. A damaging heat encounter remains a separate play
gate. No packaged performance measurement was taken.

## Contrasting cave opportunity

After reload the same scraper took one acid crust from the natural pool
at `(28,12)` below the Causeway, while the player stood on dry ground at
`(29,12)`. The linked pool group held two of three catches after scraping.
Both observed acid pools kept their 60-unit liquid volumes. At that point
health stayed at 40 and the player had no active acid effect. On the way
back the existing cave bear hit the player twice, leaving 4 HP at tick
690. The private trial stopped there. The earlier full-health save remains
available. A combined acid and brine brew remains open.

## A safer surface source and two uses for its crust

From the full-health tick 540 checkpoint, 19 dry moves reached the
surface brine bank. Two native scrapes at ticks 740 and 750 took one
crust each and left one of the original three catches. Both observed
pool owners kept 90 units of brine.

- **Portable brew.** Pack Crafting consumed one crust and produced one
  unsellable electrified tonic. It used no still, flask, fuel or drams.
- **Shock protection.** The other crust made one unit of shock-proofing
  in the previously emptied flask. The worn buckler gained +35 shock
  resistance for 100 worn turns. The worn cloak kept its own heat coat.
- **Ground use.** From dry `(17,17)`, the player threw the exact tonic at
  empty brine `(14,17)`. The tonic broke and the brine reacted to its
  charge. The two pools temporarily gained the electrified effect and
  neither lost liquid. No creature was struck. Two following waits
  produced no further brine charge reactions.

Fresh Continue at tick 800 kept the remaining catch, both pool volumes,
the exact worn shock and heat durations, and the original separate water.
The used tonic stayed absent. The run stayed at 40 HP and spent no drams.
This is an existing composition played with one supplied scraper. It adds
an ordinary surface alternative to the cave branch.

## Verification

The native RED run recorded eight expected failures and 27 passing
controls. The final connected run passed all 429 cases, including
unlinked or spent vents, both pour modes, a 30-garment popup, stale
choices and rollback. An earlier GREEN run found two tests that could
select any of several cave stairs. The final tests select the stair
at the existing connection's exact cell.

Independent review checked the existing vent markers in the saved
version 29 world, both readers, the exact garment and flask targets,
and the two native menu captures. The earlier cloak screenshot shows
the same result on the duration comparison branch. Independent review
also checked the two brine receipts and confirmed that the two affected
targets were pools and that no creature gained a new shock effect.

[Vent Examine](01-vent-examine.png) · [Coat menu](02-coat-menu.png) ·
[Cloak description](03-cloak-reader.png) · [Acid bank](04-acid-bank.png) ·
[Portable brine brew](05-portable-brine-brew.png) ·
[Ground throw](06-brine-ground-throw.png) · [Proofing receipts](observations.json) ·
[Brine receipts](brine-observations.json)
