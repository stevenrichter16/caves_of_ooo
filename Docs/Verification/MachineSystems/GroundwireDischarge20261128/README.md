# Groundwire Screen shows where the charge goes

The highlighted action now explains the one-turn grounding cost,
the maximum charge the held screen can send to the selected tile,
and the risk of wet or metal ground carrying that charge onward.
The quote fits the ground and pack action menus. It describes
the transfer before ground capacity, reactions and ordinary decay.

## Ordinary play

The seed 65 Sill checkpoint at tick 2230 retained an earned loose
brine crust and one clean water in the original flask. Ordinary
pickup and brew converted exact crust 12616 to electrified tonic
12617. Travel reached the original Peat Works locker 13278. The
traveler took its exact 8 lb screen 15122, removed the buckler from
one hand and equipped the screen. Electrical
resistance rose from 0 to 50.

One clean water from flask 9873 made a puddle at (61,7). The traveler
stepped off it to (61,8) and drank the brew while still wet. This
deliberate exposure left 38 HP, two held charge and one actionable
turn at tick 2660. The first ordinary menu named only the action,
coordinates and screen. The matched fresh branch confirmed the
same charged state before the next input.

Grounding south and one ordinary wait each reached tick 2670 at
38 HP with the exact screen still in hand. Both ended the held
charge. The target tile (61,9) retained one charge after grounding
and zero after waiting. The separate puddle and the emptied vessel
remained in both branches. The charged ground reader and the same
physical save survived a further Continue with the revised hint.

[Quoted transfer](01-grounding-quote.png) ·
[Charged ground](02-charged-stone.png) ·
[Ground after fresh Continue](03-fresh-stone.png)

## Checks

Two direct preview cases failed on the original empty hint. The
uncharged control passed. Final native testing passed 203 cases and
skipped only the explicit save-contract regeneration helper. Tests
cover the one- and two-charge offers, shared ground and pack rows,
the capped first write, no time or inventory changes from reading,
and the existing stun, full-target and rollback guards. Independent
source and recorded gameplay reviews agreed.

This was native Flat Editor play on Apple M5. The controlled
exposure used ordinary items. The test did not use an enemy shock
or measure a charge chain through a wet or metal route. The current
standalone app predates the preview.
[Exact observations, save hashes and test receipts](observations.json)
