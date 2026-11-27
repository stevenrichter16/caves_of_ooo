# Partial flask fills preserve both sides of the transfer

An ordinary pool fill rechecks the exact grounded source, the carried
flask, the actor's position, and both liquid quantities before committing.
If the situation changes during the action, it retracts matching
transferred liquid from the original flask and refunds only what it can
recover to the original pool. An independent move, part replacement,
injury, or use of liquid remains in effect. Contained reservoirs and
wells retain their existing paths. A successful last draw continues to
retire poured puddles and preserve authored pool bodies.

## Ordinary repeat trip

I continued the seed-65 Duelist after the earlier Sill batch and buckler
coating. The original purchased torch was still on the ground by the
public still. I picked it up at 2320 and took the world map to the same
surface zone at (15,8). Both saved flasks were still in the pack.

Sari-snakes interrupted the approach. At 28 HP I drank one carried
healing tonic. It restored 12 HP and the bleeding stopped. Another
encounter left 33 HP. I reached the dry approach at (33,19) with the
buckler still worn. The neighboring pool's Examine page described
brine. Both the partial and empty flask were offered in its C menu.
Closing the menu did not advance time or change either quantity.

At 2570 I chose the original bought flask's 8/12 row. Four measures
entered that flask and the exact pool at (32,20) fell from 78 to 74.
The other three pools stayed at 90 each. The separate empty flask and
the exact recovered torch stayed in the pack. One normal turn reduced
the worn coat from 78 to 77 turns. A fresh Continue kept the same items,
quantities, 33 HP, five drams and +35 shock resistance. The C menu still
offered the empty flask and stopped offering the full one.

[Dry approach](01-dry-approach.png) ·
[Partial flask choice](02-choose-partial-flask.png) ·
[Completed refill](03-four-measure-top-up.png) ·
[Full flask after Continue](04-full-flask-no-repeat.png)

## Verification

The old runtime failed 12 of 39 focused cases. It accepted a late
source or flask change and overwrote independent changes on rollback.
The final 461 targeted cases passed. The additional conservation case
checks that an independently spent measure cannot also be refunded.
The normal game test covers a real refill, cancellation, and reload.
The timing-dependent interference was induced in native tests.

The retained game used ordinary items and game time. Both starting
and completed checkpoint bodies were pinned. The main Editor left
Play Mode and cleared the private keyboard and save root. All 876
normal Quick files and 87 earlier tracked edits matched.

[Recorded states and test receipts](observations.json)
