# Reclaimed liquid oil can feed a finite hearth

November 26, 2026 UTC. Press C beside a visible finite hearth to add up to
three whole units of carried flask oil. Each unit adds one fuel to that
hearth. The menu quotes the amount and the flask keeps any unspent oil.
Cold coals still need a flame. Timber gives up to ten fuel and one full
mire flask yields three oil, below the five fuel needed for a heated batch.
Existing oil torch refills remain another use for the same supply.

## Actual play

The seed 64 branch used a [recorded camp save](../RetainedBrineCrust20261126/README.md)
at tick 1860. The expedition kit and increased stats were supplied earlier.
The oil had been settled in play. The player stood beside installed hearth
`2147` and still `2148` with 358 HP and four fuel. The still menu showed the
second batch unavailable. The selected feed reader and return to the menu
left the clock, contents and fuel unchanged.

| Branch | Outcome |
| --- | --- |
| Oil | At 1870 the same flask `2231` became empty and the hearth held 6.75 fuel, including a quarter unit of turn burn. Fresh Continue kept both. At 1880 the still used four brine from `2235` and five fuel, producing two water in `2237`. After turn burn, 1.5 fuel remained. Fresh Continue restored that exact result. |
| Timber | From the same 1860 start, one salvaged timber left 13.75 fuel at 1870. The same still paid for the same four-brine, two-water batch at 1880, leaving 8.5 fuel. Flask `2231` kept its three oil. Both hot branches kept the previous batch's water and loose crust. |
| Cold coals | A separate earned camp save at 2330 held zero fuel at 176.7°C. Oil feeding was unavailable while the leftover heat could restart it. Normal waits reduced the temperature to 134.8°C at 2490. At 2500 the flask was empty and the dark hearth held three fuel. Fresh Continue kept the dark hearth, exact emptied flask and three fuel. |

- [The oil and fuel bill shown by the selected action](01-oil-bill.png)
- [The second brine batch completed](02-second-batch.png)
- [Fuel in the hearth with the flame still out](03-cold-feed.png)

## Verification

The first native run compiled and exposed the missing choice in seven
cases with nine passing controls. An intermediate run exposed an inert
camp test fixture. The fixture now has the same installed state as the
live hearth. All 352 cases passed in the final focused run. Cases cover
exact flask choice, whole units and capacity, canonical empty state,
changed quotes, unsupported hearths, and a late interruption that
preserves unrelated damage, fuel and a replacement liquid.

Independent source review and archive checks verified the hot feed and
distillation. The original loosely placed crust and still were byte-exact
through both actions. Normal turns cooled the hearth, used equipped torch
fuel and advanced other occupants. This qualifies the guided camp trial.
Wider ordinary acquisition and packaged gameplay remain open.
[Checkpoint and test hashes](checkpoints.json)
