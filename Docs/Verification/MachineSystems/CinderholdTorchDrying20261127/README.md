# A public fire shows how much it will dry a tool

A warm campfire’s drying choice shows the selected item’s current and
resulting moisture. One action still removes up to 25 percentage points.
The preview uses the same amount as the existing drying action.

## Ordinary field trial

The seed 65 traveler continued in Cinderhold at tick 7300 with one
previously bought torch and two earned skill points. The torch was lit
and had 42 of 50 fuel. The traveler spent one point on Hydromancy and
one on Conjure Water, set the torch down, extinguished it, stepped back
and cast onto its cell. The loose torch was visibly wet and measured 74%
moisture after the turn. Picking it up left the same torch at 72%.

Two natural sari-snakes obstructed the direct return. The successful
branch used the existing Flurry and Shank to clear the road and reached
the original public campfire `25611` with 20 of 42 HP and five drams.
At tick 7420, trying to light the same loose torch from that fire refused
without advancing time. The message said the torch was too wet to light.

Two ordinary waits from the same saved tick 7430 left the packed torch
at 72% and out at tick 7450. In a fresh branch from the same save, the
campfire first showed **72% to 47%**, then **47% to 22%**. Two actions
gave the displayed changes. The traveler set down that torch and lit it
from the public fire at the same tick. It retained its 42 fuel. Fresh
Continue kept the same lit torch on the road at 22% moisture, the same
public fire, 20 HP and five drams.

## Verification

The three new cases failed against the old wording. The final native
run passed all 158 checks for campfire actions, weather drying, torch
readers and kindling. Independent source and gameplay reviews checked
the common action, exact identities and all 32 archive files. The 880
pinned normal `Quick.*` files and 87 unrelated tracked paths matched
before and after. The Editor closed the private play session. The
current standalone Mac release is the earlier field-cooking package.

[Wet loose torch](drying-01-wet-loose-torch.png) ·
[Wet lighting refusal](drying-02-wet-refusal.png) ·
[First drying choice](drying-03-torch-preview.png) ·
[Second drying choice](drying-04-torch-preview.png) ·
[Same torch relit](drying-05-same-torch-relit.png) ·
[Native results and observed states](observations.json)
