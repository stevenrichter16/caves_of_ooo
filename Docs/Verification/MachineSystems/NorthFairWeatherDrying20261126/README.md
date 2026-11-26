# Fair-weather drying at North Works

Two newly generated North Works shelves can air out an exact carried item
in fair weather. Stand alongside one on a dry, exposed cell. One action
removes up to 25 points on the shared moisture scale. The quote shows the
selected item and its resulting moisture. Ordinary shelf storage, hearth
drying, loose-gear evaporation and Drying Breeze remain available.

## Actual GameBootstrap play

- A fresh seed 64 Duelist traveled north one world-map step from the
  Reference Glade and entered North Works at tick 50 with 40 HP.
  Normal movement reached the west side of shelf 6926 at `25,16`
  at tick 250. The existing patchbearer encounter brought HP to 27.
- The visual audit supplied exact striker 6961 at 50% moisture with its
  normal 20 strikes and unlit torch 6962 at the player's feet. Choosing
  the ordinary torch-lighting action refused at the same tick.
- The shelf's C menu quoted 50% to 25% moisture. The free F1 reader
  explained the dry approach and action cost. One action brought the
  same striker to 25% with all 20 strikes at tick 260. The patchbearer
  hit the player during this action and left 15 HP after bleeding.
- Ordinary torch lighting then used exactly one strike. It lit torch
  6962 and kept the same tick and 15 HP. Fresh Continue preserved
  both capable shelves, the torch flame and fuel, and the original
  striker at 25% with 19 strikes.
- The separate older North Works save continued at tick 70 and kept
  both original shelf identities and positions. Those cached shelves
  retained ordinary storage. New sites attach the capability explicitly.

## Verification

The prior generator passed the ordinary shelf control and failed the
six expected missing-action tests. The final native run passed 163
cases with no failures, skips, inconclusive cases or compiler errors.
The connected run covers the affected weather, shelf generation,
striker and shared environmental cases. The shelf checks include
rain, roof and wet-floor refusal, stale offers, independent moisture
changes and late effect reassignment. Independent source review
confirmed that rollback leaves reassigned effects alone. Recorded-play
review matched all twelve archive files and both captures.

The test proves a bounded fair-weather option for carried gear.
Automatic drying batches, new weather penalties and packaged
qualification remain open. Two new checkpoints and the older save
provide the native receipts.

[Quoted moisture](01-quoted-moisture.png) · [Lit torch](02-lit-torch.png) ·
[Native observations](observations.json)
