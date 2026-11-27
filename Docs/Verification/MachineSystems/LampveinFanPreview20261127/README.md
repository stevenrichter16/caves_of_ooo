# Lampvein fan previews what happens to the selected cloud

Hold the fan in a hand to see the same action in the equipment menu
and the adjacent interaction menu. A cloud with density 1–5 can be
cleared in one action. A denser cloud shows how much remains immediately
after the fan removes five. The cue names the selected cloud. The
nearby interaction menu notes that gas can change as the turn advances.

The fan targets one visible, nearby transient cloud. It can thin a
cloud until it is within the bellows’ capacity. The bellows needs
a clear path to move it.

## Matched Brine Camp comparison

The isolated seed 65 trial used the earlier ordinary Brine Camp save
at tick 7120 with 33 HP. That traveler had generated the original
marsh gas by lighting mire in play. The test supplied one canonical
Lampvein fan `23321`, and the traveler equipped it through the pack.
The original torch remained equipped. The older trial had also
supplied the carried bellows.

The matched branches started with the same 60-density marsh gas
from mire `11384` on the east tile. One plain wait left 58 density.
The fan preview said 55 would remain immediately after the action
and the action reported 55. The cloud then dispersed to 53 during
that turn. Both branches ended at tick 7130 with the traveler at 33 HP.
Fresh Continue restored the cloud at 53 and the same equipped fan.

When replayed with the final menu, the fan again reported 55. The mire
released another cloud during that turn. It merged with the same
visible cloud, which reached density 113. The screenshot shows the
turn-change note next to the preview and the new emission in the log.
Continue also restored that replay's final density.

## Clearing the selected cloud

A separate test set the original saved cloud's density from 53 to 5
on a private copy. The cloud stayed at the same tile with the same
source. The native menu offered **Clear this cloud**. One action at
tick 7130 removed that exact cloud and reported that it was fanned
away. At tick 7140 another mire had produced its own cloud. Nearby
fire dealt three damage to the traveler during the clear action.
Continue kept the new cloud, the exact equipped fan and the
traveler's 30 HP. The cleared cloud stayed gone.

## Native checks

Both initial preview cases reproduced the generic old label. Focused
checks cover the 5/6 boundary, both action menus, the feedback and one
action cost, the exact cloud, a separate veil, fixed and hidden gas,
and stale selection or outer transaction failure. The fan-and-bellows
test thins the same cloud from 45 to 40 and then moves it one cell.
All 171 cases passed with the final menu.

Independent source and gameplay reviews checked the selected cloud,
result log and 24 pinned save files. All 876 checked normal Quick
files and 87 unrelated tracked files matched. The Mac Editor stopped
and restored the checked input and save settings. The latest verified
standalone build is `2c4931acc`.

[Thinning preview](01-fan-thin-preview.png) ·
[After thinning](02-fan-thin-response.png) ·
[Clearing preview](03-fan-clear-preview.png) ·
[After clearing](04-fan-clear-response.png) ·
[Recorded state](observations.json)
