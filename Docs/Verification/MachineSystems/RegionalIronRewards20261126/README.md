# The Cinderhold iron job paid for a spike

The Cinderhold and Morrowfast iron supply requests now pay 24 drams and
one fire clay for one choir-iron ore. The previous price was eight drams
and one fire clay. A Cinderhold spike cost 22 drams in the tested shop.

| Turn | Ordinary GameBootstrap play |
| --- | --- |
| 2050 | Continued the previously earned Cinderhold sieve save in seed 2, world generation 31. The player began at 40 HP with six drams. |
| 2070 | Read Perret Rillpost's current terms and accepted the request. The reader showed 24 drams and one fire clay and warned that local mining costs Rot Choir standing. |
| 2560 | Entered the west chunk and harvested the existing marked choir-iron vein. It gave two ore in stack `17132` and worsened Rot Choir reputation. |
| 2840 | Returned to Perret. The request took one ore, put it into trade stock and paid 24 drams and the original fire clay reward `17140`. The purse rose from six to 30 drams. |
| 2840 | Bought one iron spike from Perret for 22 drams. The purse kept eight drams, the remaining ore and the fire clay. |
| Fresh 2840 | A new Continue kept the exact spike, ore, clay, purse, 40 HP and completed receipt. Perret's actual action menu still offered trade through conversation and offered no repeat delivery. |

The route was guided in one ordinary world. The two jobs share the same
native reward definition and transaction. Actual trade and mining were
verified at Cinderhold. Morrowfast was covered by the native tests.
Older unfinished saves receive 24 on delivery. Their previously saved
accepted note may still show eight until replaced by the completion
receipt or by release and reacceptance. Reading current terms shows 24.
Completed historical receipts and payments persist.

Two targeted native cases failed on the original offers and 11 controls
passed. The final connected run passed all 118 cases, including both
iron requests, other payment terms, full-session receipts, split stock,
currency refusal and rollback. The full regression remains open. The
latest startup-verified Mac package predates this reward change.

The two paid and purchased private save archives and the earlier
starting archive are pinned. After closure, 861 tracked normal files
and 86 tracked foreign files matched their earlier bytes.

[Observed state](observations.json) · [Archive and test pins](receipt.json) ·
[Request terms](request-terms.png) · [Paid receipt](paid-receipt.png) ·
[Spike purchase](spike-purchase.png)
