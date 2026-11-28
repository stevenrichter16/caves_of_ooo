# An overturned cart can cook one meal

Stand beside a burning object and choose the cooking row from the
object's **C** menu or from raw food in the pack. The row names
its fire and spends one action. The same recipe converts each raw
unit into one cooked unit. The fire continues to burn, take damage and
spread through the normal turn. Suitable objects need a visible live
flame and enough heat. A cold or exhausted prop cannot cook.

In the ordinary seed 65 Sill save, Yevven Reedfall sold the traveler
raw meat `9881` and one lit torch for 14 drams. The traveler took the
atlas one tile south and found overturned cart `12336` at `(62,2)`.
A viper and a sari-snake attacked on the approach, and the traveler
used two carried healing tonics beside the cart. The original cart
began with 12 structural HP and no fuel tank.

At `(61,2)` the ordinary touch-lit-torch action set the cart ablaze.
At tick 2510, it had 10 HP and two turns of flame. The pack preview and
the world preview named that exact cart. The world action advanced
one turn to 2520, spent the bought raw meat and produced cooked meat
`12403`. The cart still had one turn of flame and 6 HP. The nearby
snake hit and bleeding continued that turn. Fresh Continue kept the
exact meal, torch, position, cart, remaining flame and player HP.

Two waits from the separately restored lit checkpoint burned the cart
down at tick 2530 while leaving the original raw meat. Another fresh
branch doused the same cart with one measure of water. The cart went
cold and kept its 10 HP. The pack's Cook row then refused without
advancing time or taking the original raw meat.

## Verification

Five cases failed as expected on the old runtime, with 50 passing.
The final focused run passed all 328 cases. Campfire and hearth cooking
remain covered. Countercases include unseen scenery, an
expired or replaced flame, an invalid ignition threshold, a burning
creature, an empty fuel source, a stale authored menu and a changed
cooked stack at commit. Hedgerows and fueled wooden barrels can use
the same shared rule. The broader run's 25 older fire reader failures
matched unchanged source by case and message.

The actual trial used the Editor and isolated save copies. The earlier
standalone Mac build remains the packaged build.

[Burning cart](01-burning-overturned-cart.png) ·
[Pack choice](02-pack-cooking-preview.png) ·
[World choice](03-world-cooking-preview.png) ·
[Cooked meal](04-cooked-over-the-cart.png) ·
[After dousing](05-cooking-ends-with-flame.png) ·
[States, archives and test results](observations.json)
