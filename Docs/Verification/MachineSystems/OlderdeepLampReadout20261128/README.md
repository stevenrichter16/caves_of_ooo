# Olderdeep’s descent jar describes its current light

The descent jar’s Examine now distinguishes mounted, packed and
held custody. It tells the player that unhooking stows it and dims
the ledge, and that holding it in a hand carries the light. A
disabled source reads as dark. The displayed wording follows the
current physical item and its existing light source.

## Ordinary play

From the earned Olderdeep court checkpoint, three steps northwest
and two west brought the traveler beside jar 26018. The same jar
started mounted at (20,5). In the original run, unhooking it put it
in the pack while its description still said it could be unhooked.

On the matching replay, the revised world Examine explained the
effect before the transfer. The ledge cell’s light-map brightness
fell from 1.0 to about 0.586 at the same player position. The cave’s
ambient level was 0.28 and other light still reached the ledge.
The inventory reader then showed the jar packed and explained how
to use it.

Existing equipment controls put the same jar into a hand. One step
east carried it with the player. Fresh Continue kept the hand light
and the reader described it as held. The screenshot pair shows the
change in physical custody. The light-map observations measure the
change in light. The flat authored ledge art showed little visible
difference in the two same-position frames.

[Mounted Examine](mounted-description.png) ·
[Packed Examine](packed-description.png) ·
[Held Examine after Continue](held-description.png) ·
[Before unhooking](mounted-ledge.png) ·
[After unhooking](packed-ledge.png)

## Native checks

The exact recovery part supplies the wording to the ordinary world
and inventory Examine readers. The reader derives custody and light
state at inspection time. It removes the old appended guidance from
the display while preserving the saved text and remaining lore.
The recovery action and lighting system use their existing rules.
Other beetle jars keep their normal descriptions.

Two focused cases failed on the old source and the unrelated jar
control passed. The same tests exercise the existing mounted-to-pack
action, equip, the enabled light state and unchanged saved prose.
The final run passed 228 tests and skipped the explicit save-contract
regeneration helper. The live test ran in the Unity Editor on Apple M5.

[Light levels, saved custody and test results](observations.json)
