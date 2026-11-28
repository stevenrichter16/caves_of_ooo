# Tally shows the terms of a weapon loan

The quartermaster’s rack shows the borrower’s Ink, the live rental
price and the refund due if the weapon comes back. A missing loaner
shows as out of stock. If the player is short of Ink, the price stays
visible alongside the shortfall. The return counter reads the payment
record from each loan, including a weapon the player wears.

## Ordinary play

The traveler reached Darret Colwick at Tally’s rental frontage in the
earned seed 224401440 save. At tick 5480, the original rack offered the
dagger, spear and longsword by name. The ordinary rent and return had
already proved the same physical dagger changed hands for 22 Ink and
came back for 11 Ink.

With the updated menu and the same earned save, the player saw 50 Ink
and three different choices.

| Loaner | Price | Refund | Initially |
| --- | ---: | ---: | --- |
| Dagger | 22 Ink | 11 Ink | Available |
| Spear | 40 Ink | 20 Ink | Available |
| Longsword | 86 Ink | 43 Ink | 36 Ink short |

The longsword attempt left the 50 Ink and all three weapons in place.
Renting the dagger moved that exact item into the pack and reduced
the wallet to 28 Ink. The rack now showed that dagger out of stock
and the spear 12 Ink short. A fresh Continue preserved the loan and
recomputed the same return quote.

The player returned the dagger for the stated 11 Ink. The same
weapon reappeared in the rack, the loan record cleared, and the wallet
rose to 39 Ink. A second fresh Continue preserved the result. The
spear showed a one-Ink shortfall, and the dagger carried its live quote
again. The conversation and inspection happened at tick 5480.

[Initial prices and wallet](initial-rack-quote.png) ·
[Rack after borrowing](rented-stock-row.png) ·
[Return quote after Continue](return-quote-row.png) ·
[Restocked rack after fresh Continue](fresh-restocked-row.png)

## Native checks

The displayed terms use the same rack selection, pricing and refund
rules as the existing rental actions. The live affordability check
includes the ordinary pack limit. The return quote walks the normal
return order when hard rack capacity limits the batch. A worn loan
or a partial return can still depend on the actual transfer result.

An offer follows its exact physical loan and recorded charge through
confirmation. A changed price or replacement weapon refreshes the
menu. Walking away keeps the loan choices out of reach through a
refresh. Older Tally racks are recognized from their existing
conversation and place.

Ten focused cases failed on the original source. The final run passed
226 tests and skipped the explicit save-contract regeneration helper.

The playtest used ordinary GameBootstrap Continue, chat and
confirmation in the Editor. The saved copies contain the original
currency and stock. The earlier Mac package was verified separately.
No performance benchmark was run for this change.

[Observed state, screenshots and regression totals](observations.json)
