# Fish activity at the Stump

November 26, 2026, America/Chicago.

One small turquoise ring marks the nearest visible shore of an
active fishable basin. Thin supply shows a fainter dot. A spent
basin goes quiet until the shared supply starts to recover in game
time. Look on visible drawable water names the activity in words.
The marker leaves creature and hazard paint clear and provides
no stock count or mark through fog.

The projection uses the same structural component and saved stock
holder as the trap. Dry or frozen cells can still connect the
basin, but they cannot show activity themselves. Poured water,
finite films and undersized pools supply no cue. A stocked basin
uses its current game-time projection. An untouched basin stays
untouched. Visible shore selection walks each touched basin once
per relevant redraw or tick. Idle automatic sidebar focus leaves
that traversal to the map and deliberate Look.

## Actual play

- Continued the retained ordinary Stump character at 43,12 and
  tick 7360 with 38 HP and no trap. The nearest two basins each had
  one visible ring. Native Look at 43,11 read “water, fish rings.”
  Neither basin had a stock Part after the read. No turn elapsed.
  [Map](01-first-rings.png) · [Look](02-look-fish-rings.png)
- In the same private session, a controlled visual comparison gave
  the inspected basin a temporary zero-stock record. The ring
  disappeared and the Look text changed to “water, water quiet.”
  The other basin's ring remained. The player, camera, tick and HP
  stayed in place. That branch was discarded without saving it.
  [Quiet basin](03-quiet-pool.png)
- Continued separately from the earned tick 8450 branch of the
  same ordinary character, after the prior purchase, trip, placement
  and played waits. The visible ring belonged to the same pond
  58374. Native Take the fish yielded one perch 71583 and reduced
  the pond from seven to six, leaving trap 71547 installed. A
  sky-sari hit for 20 HP on the collection turn. Ordinary retreat
  to the world map left 18 HP. Eating the exact perch healed three
  HP. Fresh Continue at 8480 kept 21 HP, the spent meal, the exact
  installed trap and the six-of-seven stock.
  [Catch in the pack](04-perch-from-pond.png)

## Verification

Three compiling RED tests reproduced the missing reader. The adjacent
run passed 510 checks. Four dedicated checks passed, including one
visible mark per basin, selection from the other bank, fog suppression
and recovery from spent to thin. After the idle-sidebar adjustment,
all 114 exact final fish, Look and related readout checks passed.
Source and recorded gameplay were independently reviewed.

This is one bounded Flat Editor route on Apple Silicon. Ordinary
long-term balance, alternate presentation modes and packaged
gameplay remain open. The current Mac package predates this cue.
All 876 normal quick saves and 87 protected pre-existing tracked
files matched their before hashes. The private Editor session closed.

[Live observations](observations-live.json) · [Tests, saves and image hashes](receipt.json)
