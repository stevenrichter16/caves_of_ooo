# Screen clay at the worksite and leave recoverable grit

November 25, 2026 UTC. One three-pound packet of known spoil can be screened
in one action. The existing choice carries both grades. The optional choice
carries two pounds of fire clay and leaves one pound of coarse grit loose at
the player’s feet. Both keep the same hand sieve. Ordinary dropping after
screening is free in game time, so the option saves an inventory step and
keeps separately carried grit untouched.

A ground offer is tied to the exact visible clear tile. A live pool, active
coating or physical obstruction can invalidate it. The same initial packet,
tool, exact outputs and mass rules apply to either destination. On an
interruption the transaction reverses the units it can identify. Pre-existing
materials stay in their actual custody, including ordinary death spills.
If independently moved grit remains in the world, a limited reversal
reports that one packet was spent.

## Editor play

The guided seed 65 Morrowfast start supplied an adjacent forge and raw tool
ingredients. Its verified tick-10 save was reused. The player made the sieve
with the usual action, walked to the existing finite heap, and took its actual
packet. The trial used ordinary movement, inventory menus, well repair,
pickup and scattering.

| Tick | Observed result |
| --- | --- |
| 20 | The native forge action produced sieve `2714`. |
| 200 | Ordinary harvest exhausted heap `2490` and added exact packet `2715`. The pack weighed 19 lb. The neighboring direct clay bank remained. |
| 210 | The ground choice spent the packet and left one exact grit `2718` at `(57,12)`. Two fire-clay measures went into the 18 lb pack. The same sieve stayed. Fresh Continue matched the exact ground and pack state. |
| 240 | The fire clay reopened well `2477` and its ordinary draw-water action. The same grit was still loose at the worksite. |
| 270–280 | The player returned, picked up the exact grit and scattered it with the ordinary action. The ground gained 11 turns of footing after the action’s tick. The original grit had been spent, and the restored well remained usable. Fresh Continue matched this state. |
| 210 | A separate branch from the same tick-200 packet used the ordinary all-in-pack choice. Its pack stayed 19 lb and held both grades. The revised native menu showed both destination costs without truncation. |

[Native screening choices and carried weights](01-screening-options.png)

## Verification

The first compiling native run had one existing capacity pass and four
missing-choice failures. The 190-case implementation run passed. A subsequent
independent review found a fatal interruption that could move spill stock back
into a dead carrier’s pack. Both new native cases reproduced it. The correction
preserves actual custody and refunds only the spent units. The final set,
including the working destination-choice presentation, passed 508 executable
checks with one opt-in generator skip and zero failures or inconclusive cases.

The exact owned source, tests, native results, selected playback fields,
and earned four-file checkpoint hashes are recorded beside this receipt.
The test setup reused a guided start. Unguided heap discovery, packaged
gameplay and the full-project regression remain open.
