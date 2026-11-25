# A delivery deadline and overdue notice at the wayside kitchen

November 25 UTC, 2026. Local candidate following the first NPC shipment.
Independent final review and publication remain open.

The supplier's booking records an expected time for the exact order.
The quote allows ten world ticks per walking step, pickup and handover,
followed by forty ticks of grace. The shelf and marker display the expected
day and time. When the area is active, the assigned cook can notice the late
shipment if he is near the shelf. His first observation creates one notice.
The same physical handover closes the notice even if the clay is still boxed.

## Editor play

Ordinary seed 64, game `e20820bd3ab048c892516952957e1e56`. The player travelled
from Gleaners' to the kitchen using the normal map and walked in from the
south. No supplies, money or positions were granted.

| State | Observation |
| --- | --- |
| 150 carrying | The porter held box `6896` and two measures in stack `6897`. The shelf reader showed day 1, Dawn + 170 ticks and forty ticks of grace. Reading kept tick 150. |
| 160 to 240 away | Ordinary map travel passed the grace period. The porter stayed at 25,17 in the cached kitchen, with the same parcel and escrow. The case remained unopened. |
| 250 returned | During the local return action at clock 240, Orven marked the shipment overdue. The shelf reader showed the notice while the porter still held the crate. |
| 270 received | During the worker's action at clock 260, the same parcel arrived at shelf `6895`. Arrival and notice closure both recorded 260. The cook had yet to unpack it. The shelf reader showed the actual receipt time. |

Separate F5 and fresh Continue comparisons at 250 and 270 matched the selected
order, custody, purses, clock and notice fields. An earlier v18 carrying save
from the previous shipment implementation loaded with the same cargo and
no imposed deadline. The new verification did not repeat its delivery.

- [Original agreement](01-agreement.png)
- [Open overdue notice](02-notice.png)
- [Late handover](03-receipt.png)

## Verification and limits

The compiling RED passed nine inherited cases and failed all five new
deadline cases. The first targeted pass was 45 of 45. Tests cover the due time,
inclusive grace boundary, repeated observation, a delivered box awaiting
unpacking, death without a reported cause, inactive-area pause and native
saved clock and case state. The actual late branch used a living porter.
Personal or delegated search, evidence collection and packaged gameplay
remain open.

The expanded final pass was 451 of 451 with zero C# errors, skips or
inconclusive cases. Thirty source and metadata bodies matched the test
clone. All 12 new checkpoint files were verified. The four private Editor
sessions closed and their temporary roots were removed. The selected 86
foreign and 861 normal files match their recorded checksums.
