# Spent vent and knotflax recovery

Both checks used earned seed 64 saves from normal play. Ordinary keyboard
actions went through `GameBootstrap` in the Editor. The two forks kept their
own Quick saves and the original routes remain available.

## Sodden steam vent

At tick 2140 the player opened the world action menu for steam vent 11640
at `64,4`. The condenser explained that the vent was out of catches, that
scrapers and trays share its supply, and that it restores one catch per
game day. Selecting the unavailable action showed the same explanation.
Both vessels stayed at 2/3. The connected stock on vent 11639 stayed at
0/4, with its last supply tick still 2100. Opening, selecting and leaving
the menu kept the player at `63,5` with 32 HP and no turns spent. F5 and a
fresh Continue kept the state and showed the same explanation again.

The [earlier route](../CondenserWaterVessels20261126/README.md) already used
the same condenser supply to water a crop. Native tests cover the daily
boundary, a separate ready vent, hidden-source exclusion and old action
quotes. The Editor playtest ended at tick 2140 on the spent source.
[Menu and explanation](spent-vent.png)

## Peat Works mallet

At tick 1040 the player unequipped the exact cord-bound mallet 9362 made in
the [earlier recovery and rebuilding route](../PeatFieldHead20261126/README.md).
With dagger 2698 still carried, they opened its ordinary pack menu. The
field cut showed the cost before confirmation. Cancelling kept the same
tick and resources. The next confirmation took one action, spent the
assembly and returned exactly one peat-packing mallet head 9363. No haft or
binding was returned. The same dagger, two prepared clay measures, one
knotflax cord and three timber stayed in the pack. The save and fresh
Continue kept that result at tick 1050, 40 HP and 39 drams.

Keeping the mallet preserves a working tool. Using a working forge can
also preserve the haft. The pack still advertises that forge choice when
a usable forge is absent. The execution already checks station readiness.
The presentation is a separate follow-up opportunity.
[Field cut and its cost](cord-recovery.png)

## Verification

The old runtime failed the three new cases for the missing actions. The
final connected Unity EditMode run passed 136 of 136 cases. Independent
review checked the source, exact XML, archived save hashes, recorded play
and both captures. Test outputs and exact saved checkpoints are listed below.
The Editor released its temporary input and save settings. The tracked
861 normal files and 86 pre-existing modified files matched their earlier
hashes. The current Mac package precedes these two changes.
[Native observations and receipts](observations.json)
