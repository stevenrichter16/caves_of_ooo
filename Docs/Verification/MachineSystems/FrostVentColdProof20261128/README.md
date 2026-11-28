# Frost vent Examine explains freezing underfoot

A radiating cold source now explains how water freezing underfoot can
hold someone in place and how cold-proofed clothing can shorten the
freeze. The existing terrain source supplies the clue to the ordinary
Examine readers. It follows the source in an older save and needs no
tool in the reader’s pack.

## Ordinary play

The saved seed 65 traveler started on the third floor at 27/42 HP.
Two flights of stairs and an ordinary walk reached a first-floor crate.
The player took frost lichen 64944 from crate 64933 and left its gold.
One ordinary pour emptied the brine flask. The pack recipe turned that
lichen into two measures of cold-proofing in the same flask, keeping the
sole water in the other flask.

The direct return approach ended in a cave bear attack. A fresh branch
from the completed recipe took the southern detour around the walls and
arrived beside the same natural vents at 27 HP. The experiment started
at (62,13), standing on vent 72052, at tick 9820. The ordinary Examine
originally said the vent made nearby standing water freeze.

| Branch | Ground after one water | Remaining thaw shown | HP |
| --- | --- | --- | --- |
| Same vent, plain cloak | Ice, three turns | About eight turns | 27/42 |
| Same vent, cold-proofed cloak | Ice, three turns | About five turns | 27/42 |
| Nearby dry limestone shoulder at (63,14) | Water | Traveler remained unfrozen | 27/42 |

Both vent trials ended at tick 9850. One wait in the plain branch
matched the turn spent applying one coat to worn cloak 2705. The coat
gave +35 cold resistance and kept 98 worn turns at the measured point.
The one-water flask was empty in each branch. The dry shoulder was
outside the vent’s reach and was tested separately at tick 9840.
The thaw numbers are the game’s remaining estimates under these
conditions. The trial measured the freeze state before thawing.

The revised reader showed the clue on the same source. A repeated
coated trial reproduced the freeze and saved it. Fresh ordinary Continue
preserved the exact cloak, both flasks, ice, wetness, freeze estimate
and health.

[Revised vent Examine](vent-examine.png) ·
[Plain cloak](plain-freeze.png) ·
[Coated cloak](coated-freeze.png)

## Native checks

The current-state line uses the existing source part, positive radiating
cold and current world placement. The same part still describes nearby
usable heat to eligible still users. The first native run exposed a test
fixture missing its physical part. The corrected old-source run had
eight passes and two expected missing-reader failures. The final run
passed 325 cases with one explicit save-contract generator skipped.
The case set includes ordinary readers, active and inactive terrain,
liquid transfer, freezing, clothing proofing and natural heat.

The Unity Editor playtest ran on Apple M5. No performance benchmark or
new standalone package was made for this reader change.

[Source, saves, exact matched state and regression counts](observations.json)
