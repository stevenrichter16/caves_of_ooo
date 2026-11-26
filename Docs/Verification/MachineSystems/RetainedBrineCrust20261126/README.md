# Keep crust from a brine batch

The existing hot still now offers a choice. Four carried brine and five
process fuel yield either two water in a separate empty flask or one
water and one pound of brine crust. The crust dries on the player's
clear, dry work cell and takes an ordinary pickup to carry away. Its
existing use and trade rules are the same as surface-scraped crust.

## Native play

The isolated seed 64 branch resumed the recorded expedition camp at
tick 1840. That earlier fixture supplied the camp and vessels and used
an adventurer with increased carry capacity and 400 maximum health.
It already held twelve brine drawn from an ordinary pool. No crust or
proofing liquid was supplied for this branch.

- From the camp bench at `41,9`, the ordinary C menu offered the
  two-water bill and explained that keeping crust needs clear dry ground.
  Closing the menu spent nothing. One ordinary step west reached
  `40,9` at tick 1850.
- The actual camp still at that position offered both outcomes. The
  player selected the one-water option for brine flask `2235` and
  empty flask `2236`. Tick 1860 had eight brine left, one water in the
  exact receiver, and loose crust `2728` underfoot. The fire fell from
  9.25 to 4 fuel, including a quarter unit of ordinary turn burn. The
  other flasks kept their contents.
- Fresh Continue restored the same loose crust at `40,9`. Ordinary
  pickup moved its exact identity into the pack at tick 1870.
- Two dry steps moved the player out of still reach. The portable
  press consumed exact crust `2728` at tick 1900 and put one unit of
  shock-proofing in separate flask `2237`. The player worked that dose
  into the original worn leather cap. It gave +35 shock resistance and
  100 worn turns, leaving the proofing flask empty and the distilled
  water in `2236`.
- Inventory navigation also equipped a kilnfelt apron before coating
  the cap. The apron grants heat resistance and added one equipment
  tick. The final tick 1911 snapshot and its fresh Continue agreed on
  the cap, +35 shock resistance, full wear duration, brine, water and
  empty proofing flask. Health stayed at 358 throughout.

## Verification

Against the old runtime, the five initial feature cases failed on the
missing choice and 21 controls passed. The final focused run passed
586 Unity EditMode cases with no skips, failures or compiler errors.
It covers the exact flasks, a one-unit receiver, save/reload, missing or
unqualified crust, stale wet or occupied destinations, and late
interruption that retracts the staged output while preserving outside
liquid, fuel and health changes.

Independent review examined the actual menu, ground output, coat and
four-file archive manifests. No shock hazard was applied in play.
The ordinary two-water and separate brine-bank routes remain available.
The fixed game fixture leaves ordinary discovery frequency, the full
regression and packaged Mac play open.

[Batch choice](01-choice.png) · [Loose crust at the work cell](02-ground.png) ·
[Garment proofing](03-coat.png) · [Observed states](observations.json)
