# Sella’s field dressing stops an active bleed

One dressing can treat ordinary poison and bleeding together. Self-use
and party care now share an active-condition check. A condition that has
already expired cannot offer Apply or spend the dressing. Gas poison is
outside this treatment’s scope.

## Ordinary Sodden playtest

The seed-65 traveler left Sella’s shelter at 42 HP with dressing
`23323` and spare sumpsieve pad `23316`. In Sodden `16.8.0`, a
sari-snake bite and the first bleed tick took the traveler to 35 HP.
The bite left active bleeding and both items in the pack.

Applying the dressing while exposed stopped the original bleed.
Another snake hit during the same action and started a new one.
The traveler ended the turn at 29 HP with the dressing spent. An
untreated outing also confirmed that the action disappears when
the bleeding clears, with both items still present.

Reloading the bite and retreating to the world map left active
bleeding at 33 HP. Both branches below used that exact saved state,
the same position and one action.

| Action | At tick 7050 | Pack |
| --- | --- | --- |
| Wait | 31 HP and active bleeding | Dressing and spare pad |
| Apply the field dressing | 33 HP and bleeding cleared | Spare pad |

The dressing prevented the next two points of bleed damage. The
original wound’s lost HP remained. Fresh Continue kept 33 HP, no
bleeding, the spent dressing and the same spare pad.

[The bite](01-sari-snake-bleed.png) · [Apply](02-apply-field-dressing.png) ·
[After treatment](03-after-treatment.png) · [Observations and save pins](observations.json)

## Verification

Both expiry cases failed before the fix and passed afterward. Unity
passed 299 of 300 focused checks with zero compiler errors or skips.
The one failure names the two missing Peat Works models,
`AbandonedFieldWorkRig` and `PeatWorksStillCase`. Unchanged main
reproduced the same failure in a separate three-case run.

The live comparison used ordinary self-care with naturally acquired
bleeding. Companion, ordinary poison and gas interactions have
automated coverage. Independent review checked both live branches,
the exact retained pad and archived saves. All 876 normal Quick
files and 87 prior tracked edits matched their earlier hashes. The
Editor exited Play Mode.
