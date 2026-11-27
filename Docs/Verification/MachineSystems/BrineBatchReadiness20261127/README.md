# Public brine distillation in Sill

The still names the first missing requirement for each brine batch.
It checks the carried feed, the capacity of a separate empty flask, and
the heat at the still. Keeping crust also needs clear, dry ground.
A one-unit receiver can still show the crust choice and its blocker.
The highlighted action and the corresponding Examine guide share the
same reason. The two recipes use their existing costs and yields.

## Ordinary purchases and finite brine

I continued the retained seed-65 Duelist at Sill with 31 drams and 40 HP.
Through Yevven's native deal screen, I bought one lit torch from his
two-torch stack and his separate empty flask for 26 drams in total.
The original flask held twelve water. I poured that water into a puddle
in Sill first, leaving both flasks available. No items or currency were
supplied. The market was unchanged by this work.

I crossed the world map to a newly entered surface zone at (15,8).
A native walk reached the dry edge of the brine at (33,19). The fill at
(32,20) moved twelve units from the exact 90-unit natural pool into the
bought flask. The other pools still held 90. A sari-snake missed during
the approach and fill. I returned to Sill with 40 HP and five drams.

## The same heat, two outputs

At Sill's public still (37,11), both recipes said they needed a fire
within two cells with five fuel. Opening and closing the menu and Examine
took no time. The torch in the pack could not heat the still. I dropped
it on (37,12) and stood on clear ground at (38,11). Both batches quoted
the same purchased torch as the exact fuel source.

- At 2230 the crust batch spent four brine and five torch fuel. The
  ordinary turn also burned 0.3 fuel. Eight brine remained. One clean
  water went into the original flask and one pound of loose crust
  appeared at the player's feet.
- From the same pre-batch save, the water-only choice left the same
  eight brine and 44.4 torch fuel. It gave two water in the original
  flask and produced no loose crust.

In the first branch I picked up the exact crust, poured the clean water
into a separate puddle, and turned the recovered crust into one dose of
shock-proofing away from the still. The original worn buckler held
+35 shock resistance for 100 turns of wear. A fresh Continue kept the
same buckler and resistance, eight brine, empty original flask, and
the separate water puddles. Health stayed at 40. No shock hit was used
to test the coat.

[Paid trade](01-purchase-quote.png) ·
[Missing heat](02-still-needs-heat.png) ·
[The batch choices](03-public-still-torch-quote.png) ·
[Crust at the work cell](04-kept-crust.png) ·
[Worn buckler choice](05-shock-proofing-application.png)

## Verification and limits

The prior implementation passed 28 native controls and failed five
specific batch-readiness expectations. The connected run passed 218
cases after the correction. The first live inspection also caught a
missing visual reason under a highlighted disabled action. The final
implementation passes the same refusal text to that highlight.

This verifies ordinary acquisition of the torch and receiver, natural
brine and public distillation in one generated world. The purchased
camp-still route, wider discovery and all twelve complete machines
remain open. The separate scraped-bank and supplied camp branches retain
their prior evidence.

[Recorded states, tests and save pins](observations.json)

Independent source and gameplay reviews matched both paid branches and
all 20 playtest files. The old screenshots showed an empty highlight
area. The final two-line reason was checked in the Editor after
retrieving the same torch. All 876 normal save files and 87 earlier
tracked edits matched their original hashes. The private save root
and temporary keyboard were cleared.

## Mac package

The universal Mac package for `d1b16bb89` built with zero errors and
five warnings. An Apple M5 launch reached GameBootstrap. Both the
current and previous apps matched their recorded 186 files. All 876
normal Quick files matched again and startup preferences were restored.
The Mac check covers startup. The actions above were exercised in
the Editor.
[Startup receipt](mac-startup-verification.json)
