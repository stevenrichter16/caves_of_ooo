# Sill's settling pocket

November 26, 2026, America/Chicago.

One shallow pocket can appear on Sill's north riverbank when the approach
and two work cells fit. A little wave marks the covered sand. Emptying the
pocket exposes a pale streak. Gathering its one measure of silver sand
leaves a scrape. In this location, an empty flask holds all twelve units
and a hand pump can move all twelve to a clear outlet. Both leave the same
exposed deposit through ordinary liquid handling. The river, merchants
and well repair are unchanged.

The deposit reads its actual cell. A partial draw, refill, or active coating
keeps an unspent deposit covered. Collection uses the existing fixed harvest
receipt and rechecks the selected site through commit. The spent deposit
retains its saved graph through explicit zone eviction. Generation omits
the site when terrain or authored pool content fails its checks.

## Actual GameBootstrap play

- Started from the retained seed 65 Duelist at western Morrowfast, tick 520.
  Ordinary travel to Sill reached 39,11 at 700. The provisioner sold flask
  9873 for 19 drams. The player walked around villagers to 42,17 and saw the
  wave on the bank directly south. [Covered pocket](01-covered-pocket.png)
- At 940 the side-facing menu offered a flask fill. The fill finished at
  950 with twelve water in the same flask and no remaining pool 10209. The
  mark changed before opening Examine or another action menu.
  [Exposed sand](02-exposed-sand.png)
- The native gather option yielded exact sand 10241 at 960 and left the
  scrape on deposit 10208. Ordinary trail marking spent that sand on water
  note 10242 at 42,17. The player stepped north, saved and resumed.
  [Spent scrape](03-spent-scrape.png)
- Fresh Continue at 980 kept the exact spent deposit and water note, flask
  9873 with twelve water, and 40 HP. The native reader describes the mark
  as a traveller's chosen note.
  [Reloaded note](04-reloaded-water-mark.png)

All movement, trade and work used ordinary native controls. This branch
used the flask, with no item grants or clock adjustment. The pump was
verified in native Edit Mode tests. Tool acquisition, player routing and
visual comparison were exercised in one Flat Editor world on Apple Silicon.

## Verification

Five detached Sill layouts admitted the first candidate. Seven initial
native tests reproduced the missing pocket. The final run passed all 167
checks without compiler errors or skips. The adjacencies cover the
old court and rain seam, both liquid routes, refill, a naturally stale
gather, a coating introduced after staging a gather, invalid authored
pool content, save and explicit unload. The late-coating fixture stages
the ordinary inventory action and interrupts before commitment. A first
fixture had tried vetoing the existing post-commit notification, which
cannot undo paid work. The corrected fixture tests the actual boundary.

Independent review compared both states in the raw save, all four file
hashes and the capture sequence. All 876 normal quick-save files and 87
pre-existing modified tracked files matched their baselines. The Editor
returned to Edit Mode, the private root cleared and its keyboard was removed.

[Recorded observations](observations.json) · [Native and save receipts](receipt.json)
