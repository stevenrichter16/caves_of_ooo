# Slip’s east road lamps can light a torch

Slip’s oil delivery also makes the two original fixtures a place to
light an accessible, dry torch with fuel. The lamp’s menu identifies
which torch will light. One action transfers the flame. The torch then
burns its own fuel and the town’s lamp keeps burning. The same kindling
receipt handles the torch’s flame state and rechecks the exact fixture
and torch at commit. The reachable wick is declared on the Slip lamp
blueprint. Existing installed lamps can read the declaration without
changing their identity, light state or saved oil condition.

## Ordinary play

The prior seed 65 save began on Slip’s east road after the oil delivery
at tick 2890. I walked to a fallen merchant’s dropped goods, picked up a
stack of two torches and separated one through normal inventory controls.
I returned to `(48,11)`, set the exact torch `12905` down and extinguished
it. The old lamp menu lacked the new action. The replay of that same
save had a dark torch with 28.4 fuel, both road lamps lit, 41 HP and 13
drams.

The lamp at `(49,10)`, `12919`, offered to light the torch at `(48,11)`.
The exact torch lit at tick 3330, after one action. Its fuel dropped from
28.4 to 28.1 in the normal turn. Health, drams, the other torch and both
town lamps matched before and after. Ambient light held at 0.4. The
south sample at `(48,14)` rose from 0.6 to 0.943. The distant control at
`(40,17)` stayed at 0.4.

Fresh Continue restored the exact lit torch, lamp IDs and measured light
at tick 3330. I picked up that torch, equipped it, and walked four steps
south to `(48,15)`. At tick 3380 the equipped torch still had 26.9 fuel.
The player was beyond both road lamps’ radius and stood in full light at
ambient 0.4. Health stayed at 41 HP during the save and carry trials.

## Verification

Six cases reproduced the missing action on the previous runtime. The
final focused suite passed all 313, including equipped and loose
variants, nearby fire or flint controls, and a condition change during
commit. The matched ordinary replay uses a loose torch, which was
found and handled in play. Independent review checked the source,
exact custody and menu, and the fixed-position light readings.
The playtest used the Unity Editor on Apple M5. The 880 pinned normal
save files, 87 pre-existing modified workspace paths and both preferences
matched their prior bytes after the playtest.

[Previous lamp menu](01-old-paid-lamp.png) · [New wick action](02-wick-option.png) · [Lit torch and lamps](03-torch-lit.png) · [Carry the flame](04-carried-beyond-the-lamps.png) · [Measured results](observations.json)
