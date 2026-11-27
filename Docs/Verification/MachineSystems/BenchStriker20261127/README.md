# Servicing a flint striker at a camp bench

November 27, 2026, America/Chicago.

An accessible, ready cold-work station can restore up to 15 strikes to a
carried striker for one iron spike. Portable hand knapping still restores up
to 10 for the same input. The bench row appears only when it can give more.
The work uses the existing station and material checks. It leaves the
striker's dampness and nearby fire unchanged.

## Actual GameBootstrap play

1. Continued the retained private camp at 2590. Walked to the loose flint
   striker and picked it up normally at 2630. For this controlled run, set
   exact striker 2297 from 19 to zero strikes and supplied one spike 2732.
   The existing installed camp bench 2149 stayed at (41,9). Away from the
   bench, inventory offered the ordinary +10 option.
   [Portable knapping](01-portable-knapping.png)
2. Walked to (41,9). Inventory offered +15 at the bench and +10 by hand.
   Chose the bench. One action reached 2710, spent exact spike 2732 and
   restored 15 strikes to the same striker.
   [Both repair options](02-bench-service.png)
3. Set down the existing cold hearth 2147 east at (42,9). Fed one loose
   timber board at 2730 for 10 fuel. The hearth stayed cold until its normal
   Relight action drew one of the 15 strikes.
   [One-strike quote](03-relight-uses-one-strike.png)
4. At 2740, the striker had 14 strikes. The same hearth burned with 9.75
   fuel and a temperature of 441.5. Saved and chose Continue in a fresh
   play session. The same striker, bench, fuel, temperature and lit state
   persisted.
   [Reloaded fire reader](04-reloaded-hearth.png)

## Verification

Six first-run native failures reproduced the missing service. The final
247 targeted cases passed without compiler errors or skips. They cover
both repair choices, capped gain, packed and moved fixtures, replacement
inputs, and a late station failure. They also cover preserving an unrelated
label when a service is rolled back.

Independent source and gameplay reviews cleared the slice. All four raw
checkpoint hashes, 876 normal quick-save hashes and 87 pre-existing
modified tracked files matched. The Editor returned to Edit Mode with the
ordinary keyboard and save root. This is a Flat Editor route on Apple
Silicon. The portable quote was presented here, with payment exercised by
native tests. The worn tool and one spike were controlled setup.

[Recorded observations](observations.json) · [Native and save receipts](receipt.json)
