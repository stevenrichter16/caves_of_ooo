# Sheltered winnowing in Morrowfast

November 26, 2026, America/Chicago.

A carried sieve and directed-air tool can keep both fractions of a gathered
knotflax bundle on a dry interior tile. One bundle and one action yield one
plantable seed and one unit of chaff. Both tools stay in the pack. The simple
sieve option yields just the seed. The fixed public frame still keeps chaff
on fair, dry days without requiring either carried tool.

The existing material receipt owns the payment and products. It binds the
exact tools and the work cell. A wet floor, changed tool, or invalid chaff
template causes refusal without spending the bundle. The crop and fuel items
are the existing canonical products.

## GameBootstrap playtest

1. Loaded the retained seed 65 Duelist at the western Morrowfast allotment.
   Advanced the private clock from 520 to 14380 to reach the ripe stand.
   Ordinary harvest took bundle 7202 at 14390. The test supplied only hand
   sieve 7203 and hand bellows 7204. Acquisition of both tools in this run
   remains open.
2. Traveled to Morrowfast and opened that bundle's inventory actions outdoors
   at 38,14. The only active processing choice was the simple sieve. The
   player kept the bundle, walked around intervening obstacles, opened The
   Dry Hem's door and entered the dry interior at 32,12.
   [Outdoors](01-outdoor-sieve.png) · [Under cover](02-dry-shelter-choice.png)
3. Selected the bellows action at 14580. At 14590 the bundle was spent, seed
   9280 and chaff 9281 were in the pack, and both tools kept their original
   IDs. A fresh Continue restored those four items and the same room and tick.
   [Reloaded pack](03-reloaded-products.png)
4. An initial return approached the allotment along a straight line and a
   wine-leaf sundew took five HP. That branch was left unsaved. From the
   fresh winnowing checkpoint, the player walked around the snare, harvested
   the hearthbulb on bed 62,11 and planted exact seed 9280. Fresh Continue at
   14900 kept knotflax crop 9285 on that bed, chaff 9281 and both tools in the
   pack, and 40 HP. The crop reader showed seed stage and dry soil.
   [Reloaded crop](04-reloaded-crop.png)

The resulting chaff stayed in the pack. Its existing fuel behavior was
verified separately on the public-frame route. This playtest used the Flat
Editor on Apple Silicon. The shelter comparison happened after the shower.
Tool balance, natural discovery across layouts and packaged play remain open.
[Earlier chaff fuel playtest](../FairWeatherChaff20261125/README.md)

## Native checks and preservation

The first compiling run had six expected failures for the missing choice and
one passing simple-sieve control. The final run passed 302 checks, with zero
failures, skips or compiler errors. It includes both existing methods, exact
tool replacement, a newly wet or uncovered work cell, a compatible distinct
pair when one tool has both capabilities, unsafe template refusal, and the
adjacent rain, material-process, field-tool and camp-hearth fixtures.

Independent source and gameplay reviews verified the code, menus, raw save
graphs and all eight checkpoint hashes. All 876 normal quick-save files and
87 pre-existing modified tracked files matched their baseline hashes. The
Editor returned to Edit Mode, the isolated save root was cleared and the
temporary input device was removed.

[Recorded observations](observations.json) · [Native and save receipts](receipt.json)
