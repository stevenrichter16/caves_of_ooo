# Hand bellows in Brine Camp

The pack, world action and use guide now explain three reasons a
visible adjacent cloud cannot be pushed. It may be too thick, fixed
in place, or have no clear visible path through and beyond it.
Selecting the refusal spends no action. A blocked or unseen destination
gets the same path explanation.

## Dense marsh gas

The ordinary seed 65 Brine Camp checkpoint started at 33 HP with an
equipped lit torch. The isolated test supplied one canonical set of
hand bellows `23320`. Three east steps brought the traveler beside
the existing mire at `(44,12)`. The normal torch action lit that
mire and the next wait produced its own marsh gas, density 60.

At tick 7120 the adjacent menu said it was too thick for the bellows.
Selecting the row left the clock, HP, heat, tool and exact gas
unchanged. Fresh Continue restored that same cloud and its creator.
In the final runtime both the pack's Examine reader and
the adjacent world menu showed the same refusal. Reading took no time.

## A thinner cloud

The traveler doused the first mire. The cloud later dispersed and
nearby heat and fire remained active. The traveler approached a
separate cloud from another mire at `(46,12)`. At tick 7340 the
menu offered the push at density 14. The normal selection moved
the exact cloud east one cell to `(47,12)`. Continue kept the
cloud's identity, density, creator and new position, the same
bellows and the traveler at 23 HP. This verified displacement,
with traversal of the former gas cell still open.

## Verification

The first native run passed 46 controls and reproduced five missing
refusal or thinning cases. A separate case reproduced an exposed
stale reference to a hidden cloud. The exact cell guard fixed it.
All 130 focused cases passed in the final run. The nearby menu still
uses the original move for a thin cloud, and the blocking checks
preserve the same gas identity and finite density.

The tests of stable and obstructed sources use fixtures. Independent
review checked the changes, native results and recorded source and
refusal. The test ran in the Editor. The latest verified Mac build
remains the frost trap package. All 876 checked normal saves and
87 pre-existing tracked edits matched.

[Dense refusal](01-dense-refusal.png) · [Examine](02-examine.png) ·
[Thin cloud option](03-thin-option.png) · [After the push](04-after-push.png) ·
[Observations and save hashes](observations.json)
