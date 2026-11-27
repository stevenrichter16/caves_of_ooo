# Fair weather at the Morrowfast seed frames

November 26, 2026, America/Chicago.

A clean, fixed public seed frame now shows a pale gold mesh when the
ground is dry and the local weather is fair. Examine says why it
matters. Winnowing a knotflax bundle there can save the chaff for a
fire. In rain, the frame still shows the shared catchment. A wet cell
in fair weather drops the new mark and retains ordinary seed work.

The map, Examine, and processing quote share the frame's site
condition. The quote still validates the selected bundle and output
separately. The existing clock and cell updates repaint the cue.
Ordinary winnowing uses one bundle and spends one action regardless
of weather. The hand sieve remains an alternative away from the
frame.

## Actual GameBootstrap play

- Started a fresh isolated seed 65 Duelist, generation v31, at
  Gleaners. Traveled normally to western Morrowfast and walked 23
  steps from arrival to the allotment at 59,8. The tick 520 checkpoint
  contains the intact stand at 60,7 and public frames 7198 and 7199.
- Advanced the private game clock from 520 to 14380 to reach the
  existing ripe season near a shower boundary. The player used the
  ordinary gather action to take one seed-head bundle from exact
  stand 7200 at 14390. Both frames showed their blue rain cue.
  The work menu quoted one plantable seed and discarded chaff.
  The player closed it without spending the bundle.
  [Rain in the allotment](01-rain-cloths.png)
- One ordinary wait reached tick 14400 without moving. Dawn began,
  and both frames changed to the pale mesh. Examine said that the dry mesh
  saves chaff. The new quote showed one seed and one dry chaff.
  [Fair view](02-fair-gold-mesh.png) · [Examine](03-dry-mesh-reader.png)
- Winnowed the one earned bundle at frame 7198. At 14410 the pack
  held seed 7203 and chaff 7204. Fresh Continue kept both products,
  mark positions and 40 HP. The shared rain catchment remained
  unspent. No items, weather, or health were supplied in this playtest.
  [Reloaded pack](04-seed-and-chaff-reloaded.png)

## Planting the earned seed

A separate Continue from 14410 used the exact new seed. Four steps
reached the cultivated beds. A normal harvest cleared hearthbulb 7191
from 62,11, leaving its produce and seed on the ground. The native
planting menu offered the carried knotflax seed beside the other
seed types. Planting spent seed 7203 on that bed. At 14470 fresh Continue
kept knotflax crop 7208 on the same tilled cell, chaff 7204 in the pack,
and 40 HP. Examine identified the crop as a dry seed awaiting water.
This branch kept the chaff for a future fire.
[Planted crop](05-planted-knotflax.png) ·
[Earlier use of this chaff type as fuel](../FairWeatherChaff20261125/README.md)

## Verification

The compiling RED run reproduced the missing fair mark and passed
the rain priority control. The final run passed all 181 checks across
nine fixtures, with zero compiler errors or skips. Tests check the
exact native quote, both frame owners, wetting and clearing a single
cell within one fair window, the older plain frame, fog and shared
rain allowance. Independent source and recorded-play reviews agreed.

The playback is one Flat Editor layout on Apple Silicon. The initial
clock advance was controlled. Long-term balance, other presentation
modes and broader acquisition remain open. All 876 normal quick saves
and 87 pre-existing modified tracked files matched. The isolated
session closed and the earlier seed and build choice settings resumed.

[Frame observations](observations-live.json) ·
[Planting observations](planting-observations.json) ·
[Tests, saves and image hashes](receipt.json)

## Current Mac package

The universal app at
`/Users/steven/coo-verification/builds/mac-e7f5dc2d9-20261126/CavesOfOoo.app`
contains runtime `e7f5dc2d9dad1b3196a99308dd571f0cf15c0d75`,
including the Stump pond and Morrowfast frame cues. The build matched
32,252 committed inputs, completed in 33.05 seconds with zero errors
and one warning, and preserved the previous app. Both packages
matched their 186 file hashes. All six native binaries include
Apple Silicon and Intel slices.

The new package completed startup on Apple M5 with 2,707 entities.
Both normal save roots matched across all 876 quick-save files. The
nine preferences changed during startup were logically restored.
The test player was stopped. Packaged interaction play and testing on
physical Intel remain open.
[Mac startup receipt](mac-startup-verification.json)
