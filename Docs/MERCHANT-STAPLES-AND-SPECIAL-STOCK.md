# Merchant staples and changing special stock

User direction, November 23, 2026. Prioritize this as the next merchant and
lasting-project increment in the active hourly systems program. This is the
requested design direction; the new stock layers and unlocks are not shipped.

Merchants should have a dependable base selection of specific goods and a
separate, periodically changing selection of interesting specials. Returning to
a familiar merchant should offer both reliability and the possibility of finding
something new. Opening a trade route or building a useful machine or tool for
the merchant can expand the special selection. Such projects gain a lasting,
visible benefit through the merchant's actual business.

## Dependable staples

Give each participating merchant an authored core catalogue appropriate to their
occupation, community, biome and economy. The staple identities stay dependable
across special-stock rotations. Exact quantities, replenishment and the meaning
of availability after a purchase require an explicit policy; a dependable
catalogue does not imply an unlimited supply created on each menu opening.
Preserve convenient access to ordinary supplies and existing trade/barter rules.

## Changing specials

Maintain a bounded special consignment that changes periodically in game time,
with limited quantities and a saved identity for each actual item. A special can
be an unusual material, regional tool, useful component, distinctive piece of
equipment or supported modification combination. Interesting does not require
ever-increasing combat power. Different merchants should have different themes
and pools, with enough variation to reward revisiting them.

The user explicitly wants potentially unique items. Distinguish an unusual
generated item from a genuinely one-off authored object. A one-off must retain
its provenance and cannot be recreated by restock, reload, merchant relocation
or another vendor's roll. Protect undisclosed item state and lore mysteries.

Separate special rotation from ordinary inventory custody. Bought items remain
the buyer's; player-sold goods, buyback stock, reserved items and goods held for
another service must not be deleted or regenerated when specials change. The
fate of unsold old specials needs a stated policy before implementation.

## Lasting projects expand opportunities

Use actual persistent route and equipment conditions to change stock eligibility:

- An opened route can admit supplies from the connected region or additional
  consignment types. Keep the merchant's existing staples available.
- A repaired forge or useful workbench can allow a smith to offer supported
  workmanship, components or refitted equipment.
- A still, preservation apparatus or suitable storage can enable additional
  preparations or goods the merchant can actually handle.
- Better transport or storage can increase a bounded consignment's variety or
  capacity where that capability genuinely supports the result.

These are examples to refine against shipped capabilities, not claims that those
projects or supply networks already exist. Use shared capabilities and world
conditions, not a separate scripted exception for each merchant and machine.
An upgrade broadens future eligible offers; it must not mint another lot whenever
the player toggles equipment, crosses a zone boundary or opens the shop.

Explain a meaningful unlock to the player: the relevant project is finished,
which kind of goods can now arrive or be made, and whether it affects the current
consignment or a later one. Preserve alternate merchants, direct crafting,
gathering and services. Special-stock improvements should be useful optional
projects, without making a particular machine mandatory for basic supplies.

## Current source footholds

`TraderPart` already authors a StockTable, starting purse and opening allocation.
`TraderRestockSystem` already stores LastRestockTurn and, on eligible zone entry,
tops up buying power and rolls the existing themed table when its shelf falls
below the low-water threshold. Its current interval guard is 300 turns; that is
not a selected cadence for new specials. The implementation currently uses one
themed shelf policy rather than the proposed independent staple/special layers.

`PlaceConsequences.OnZoneEntered` already tops up listed goods when an authored
place condition holds, with its own saved interval stamp. This is a possible
integration seam, not a verified trade-route graph or special-consignment system.
Concord carriage holds actual entrusted goods; merchant supply should not be
confused with transmitting information or the player's freight parcel custody.

Verify the shared authoritative time path, shelf accounting, generated item
state, valuation, save graph, sold-item custody and existing conditional stock
before selecting a minimal implementation. Reuse existing trade infrastructure.

## First bounded implementation and acceptance

Start with one existing merchant: retain explicit staples and add a small themed
special lot with saved quantities, identities and rotation state. Demonstrate a
second contrasting merchant profile or place before claiming broad reuse. Add
one supported machine/project condition that expands future eligibility, with a
real alternative to doing that project. Route-based eligibility follows a
verified route condition; no invented network is accepted merely by this note.

Use one authoritative game-time policy and stable persisted rotation decisions.
Menu reads, buy/sell, re-entry, travel and reload cannot reroll a current lot.
Evaluate long absences with bounded catch-up, not an unbounded backlog of free
consignments. No wall-clock stock economy, machine-toggle duplication, infinite
unique-item creation or purchase/resale profit loop. The refresh cadence, repeat
avoidance, unsold-item disposition, upgrade timing and old-save defaults remain
design choices to resolve explicitly rather than silently assume.

Obtain bounded independent design/source critique; write compiling native RED
and countercases; implement the smallest complete player benefit; run focused
trade/restock/ownership/valuation and save/reload checks. Actual GameBootstrap
shopping must show staple reliability, meaningful special variation, purchasing
the exact displayed item, free cancellation, saved purchases/current stock,
elapsed-time rotation and a visible lasting-project unlock. Tests and offline
roll samples do not replace that play. Update this note, the living dependency
map and continuation with accepted implementation and evidence.

Preserve all twelve machine statuses. This direction connects merchant variety
to lasting projects; it does not complete a machine or accept any hypothetical
route, production, NPC labor or economy simulation. Graphics remain cancelled,
capstan/cultures parked and Deck deferred.
