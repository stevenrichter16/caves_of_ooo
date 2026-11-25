# Fair-weather winnowing saves fire fuel

November 25, 2026 UTC. Roadmap item 34 has one qualified process. The
public seed frame still winnows one gathered bundle into one plantable seed
in one action. A clean outdoor frame on dry ground in a fair regional
window retains the bundle’s one-pound chaff fraction. The preview shows both
products. Rain, a wet cell, or unavailable bonus stock leaves the ordinary
seed work and its discarded chaff. No rainwater is spent.

The shared material receipt uses the previewed outputs at payment. A change
in the bonus condition invalidates the old action before spending the bundle.
The retained chaff has no trade value and is a plain combustible reagent.
Ordinary finite fires accept one for up to five fuel. Its state is protected
when stacking, and no saved Part fields were added.

## Actual Editor play

Both branches continued the earned seed 65 bundle save. Earlier gathering
used a controlled starting clock and one supplied empty flask. No new
bundle, seed, chaff, weather, or player stats were supplied in these branches.

| Tick | Observed result |
| --- | --- |
| 14240–14250 | In rain, the world menu quoted one seed. Native winnowing spent bundle `7200` at frame `7197`, produced one seed and left both rain readings at zero spent. |
| 14400–14410 | In a separate branch, 16 ordinary waits brought the same bundle into fair weather. The menu quoted one seed and one dry chaff. Winnowing produced seed `7201` and chaff `7202`, with the same one-action cost and no rain spending. |
| 14410 | Fresh Continue matched both exact products, the carried load, actor location, weather and frame state. |
| 14490–14510 | After normal world-map travel to `Overworld.4.5.0`, a supplied canonical camp hearth `7203` was set down on open ground. The common fire menu spent the exact chaff for five fuel. The hearth remained dark at 25°C and the seed remained in the pack. |
| 14510 | Fresh Continue matched the five-fuel unlit hearth, exact installed position, seed, spent chaff and selected actor and world fields. |

The first attempted hearth walk in `Overworld.2.5.0` ended in a sky-sari
attack at 14490. The retry resumed the unchanged earned chaff checkpoint.
The hearth was supplied to exercise ordinary installation and fuel use.
Natural discovery frequency and eventual fire startup remain open.

- [Rain menu](01-rain.png)
- [Fair menu](02-fair.png)
- [The same chaff at the camp hearth](03-fire.png)

## Targeted native checks

The first compiling test run passed 10 of 12. The dry-output and stale-quote
cases reproduced the missing behavior. The first implementation passed 11
of 12. The older post-season test then explicitly selected a rainy window so
it continued to verify the seed-only outcome. The next run passed 255 cases.
The final expanded run passed 417 executable cases and skipped the explicit
saved-contract generator. It had zero failures, inconclusive cases or C# errors.

Tests cover rain onset before selection and during a process callback, fair
weather over a wet cell, exact item and cell menu parity, malformed optional
content, altered stack refusal, mixed legacy work, saved chaff identity, and
the ordinary fire action. Native test time for the final run was
95.13 seconds. The inherited full-project regression remains open.

Independent source and recorded-play reviews found no remaining high-priority
issues. They rechecked 12 external save bodies, eight native run hashes, seven
source pins, both fresh comparisons and all three screenshots. The hearth
menu clips the end of its existing hint. Its cost, gain and ignition state are
legible. All 86 tracked foreign and 861 selected normal files matched.
