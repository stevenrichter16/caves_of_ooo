# Spare willow and a timed repair at the Drowned Ledger

The original survey line on a fresh Drowned Ledger layout has two spare
willow stakes beside the cord. Salvaging them uses the ordinary harvest
transfer. Excess spills at the line when the pack is full, and the
installation stays in place. Its temporary patch spends two willow hafts.
The existing lasting repair takes three oak hafts and one leather binding.
A willow patch leaves the lasting concern credit open.

Timed structure repairs now reconcile against the same saved clock while
the player is in the settlement as well as on entry. The new local check
runs before the player can act, on direct game-time advances, and on
Continue. Wells, ovens and lanterns use their existing rules.

## Actual play

The seed-65 traveler continued from Quillhold with 31 HP and three drams.
World-map travel reached the Drowned Ledger's dry commons. The unpatched
line at `(40,15)` offered two bills and a single salvage action. The
player gathered two ordinary willow stakes, then spent both on the
original line. No goods, money, clock or carrying capacity were supplied.

The accepted branch patched at tick 2300 and saved at 2310. Full Examine
showed the exact deadline's remaining 690 ticks, the empty spare stock and
the separate lasting bill. Continue kept the same patched line and spent
stock. A sari-snake did two damage while the player waited. The player
dealt with it through ordinary combat, returned to `(40,14)` and kept
that injury. At tick 2990 the line was still patched. One ordinary wait
reached tick 3000. The change happened on the same tile inside town, and
the message log reported that the stakes bent. Fresh Continue kept the
same out-of-line installation, spent supply, 29 HP and three drams.

One earlier capture shows the fresh offer before the first patch. The
first save key was consumed by the repair announcement, and its named
archive contained the earlier arrival autosave. The accepted branch
repeated those two ordinary actions from that arrival and checked the
actual patch and expiry saves separately.

## Verification

Three stock tests failed against the old source and the stable control
passed. A separate baseline test showed an older line stayed patched at
deadline while the player remained on site. The connected run exposed
five stale transition tests invoking an obsolete private signature.
Passing the existing autosave default let those tests exercise the intended
transition. The final filter passed 176 cases with zero failures. The only
skip was the opt-in saved-Part baseline generator.

Independent source and recorded-play review checked both archives.
All 876 normal quick files and 87 pre-existing tracked changes matched
their starting hashes. The Editor returned to Edit Mode with the private
save root cleared. The lasting oak repair was checked natively. The compact
menu can clip the final word of the countdown, which is visible in full
Examine. The full current regression remains open.

[Spare stakes and both bills](01-spare-stakes-and-repair-bills.png) ·
[The active patch in Examine](02-live-patch-examine.png) ·
[Ten ticks left](03-ten-ticks-before-failure.png) ·
[Expired on site](04-line-out-of-alignment.png) ·
[Recorded states](observations.json)
