# North Works shows when shallow lichen is ready

The shallow seasonal patch now shows a low crust while dormant and
full fronds when one pinch is ready. Flat mode changes from a short
cyan ground mark to the existing lichen sprite. Previous 3D mode
shows a smaller crust, then the full existing model. Fog conceals
live growth and the deeper ordinary patch retains its own appearance.
The same saved season and source control the harvest and its cue.

The seed 64 playtest continued an earned walk to (40, 10), facing
shallow patch 6957 at (39, 9). Tick 70 offered no harvest. A controlled
clock advance to 28870 kept the player, terrain, time of day and source
fixed and showed ripe fronds. Actual **gather one frost lichen** removed
the patch and yielded one pinch, 6958, at tick 28880. A fresh Continue
kept that exact pinch, 40 HP and the ordinary deep patch 6918. This
test kept the previously verified freezing action as separate evidence.
[Earlier field-use trial](../NorthSeasonalLichen20261126/README.md)

The separate 3D comparison observed the same owner's scale change
from 0.55 by 0.30 by 0.55 to the full 1 by 1 by 1. It kept the ready
patch and deep patch in a separate save. Independent review checked
the four captures, both exact save histories and the source binding.
The earlier Examine remains in the saved log in the captures, while the
map shows the source without an open reader or menu.

- [Flat dormant](01-dormant.png) · [Flat ready](02-ready.png)
- [Previous 3D dormant](03-previous-dormant.png) · [Previous 3D ready](04-previous-ready.png)
- [Rendered states and exact save hashes](observations.json)

The baseline native run reproduced the false ripe appearance, with
the fog control passing. The final filter passed 137 cases with one
opt-in skip. Two older cord action tests now use the verified seed 2
allotment. Their earlier optional seed 64 stand was absent in the
combined filter, before either action ran. The other checks still
exercise the original seed 64 allotment.

The comparison covers one North Works layout in the Editor. The
current Mac package predates the change.
