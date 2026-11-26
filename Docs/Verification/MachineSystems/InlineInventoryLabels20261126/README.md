# Inline inventory contents and labels on ordinary items

Clicking a carried vessel opens its contents directly beneath it. Each
child row is indented and kept next to its own vessel. Two same-name
satchels can be expanded separately. The row action button opens the
vessel's other commands. Keyboard and controller item actions also offer
Expand or Collapse. A child row's actions are scoped to that vessel.

## Labels in actual play

The seed 64 GameBootstrap playtest continued the private Sill checkpoint
with two field satchels and one camp box. All label edits used ordinary
inventory input. The playtest applied the following choices.

| Item | Mode and color |
| --- | --- |
| Wielded dagger | Coral Tools icon |
| Worn buckler | Violet swatch |
| Worn cloak | Amber Clothing icon |
| Healing tonic stack | Cyan `field meds` text |
| Gladroot seed stack | Green `spring seed` text |
| Watering grimoire | Blue Important icon |
| Dried meat inside the first satchel | Amber Food icon |

The parent retained its cyan Materials icon. The separate empty satchel
retained an amber swatch, and the camp box retained green `seed kit`
text. The exact labeled tonic stack joined the meat in the first satchel.
The ordinary mouse click hid and then revealed both indented rows without
changing the 38-pound pack weight or advancing the game clock.
Cancelling a seed color edit kept the saved green. Fresh Continue kept
all seven new labels and the exact two stored stacks. The new inventory
opened with the vessel collapsed and the same mouse action revealed both
children.

## Verification

Both native mouse cases failed on the old implementation at 1280×800
and 1920×1080. In the completed feature all 313 focused cases passed,
including both pointer sizes, label identity for equipped and stored
items, preservation on splits, and merge compatibility by matching label.
The wider run passed 491 cases covering storage, inventory, trade, repair,
and material processing. One opt-in save-contract regeneration case was
skipped.

[Expanded labeled pack](01-expanded-labeled-pack.png) ·
[Editing a seed label](02-label-editor.png) · [Observations](observations.json)
