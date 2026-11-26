# Item icons in trade and storage choices

The inventory atlas covers 357 portable item types and 35 additional storage
owners. A neutral frame serves an unrecognized type. The witness-book,
carved name-token and severed limb now have dedicated art. The Sporeblade,
curation key and absentmint leaf have silhouettes appropriate to their
physical forms. All other prior frames and their mappings were preserved.
Closed ordinary, locked and mimic chests share the same art.

Both trade panels use the same type icons as the pack. The storage
destination icon appears in the `Put in` row for the exact listed owner.
The available choices, prices and weights keep their existing rules.
**Pause → Graphics → Item icons** defaults on. A player may independently
label an item with a color mark or colored text. The earlier
[label playtest](../InventoryRowIcons20261126/README.md) covers migrating older
icon labels to the same colors.

## Actual GameBootstrap play

- At tick 4870, the player spoke to Ostrel Reedfall and opened his shop. The
  seven visible stock rows and 38 visible player rows showed their icons.
  The deal view showed those same 45. Turning item icons off in Graphics
  and reopening the shop cleared the icons at tick 4870. The pack entries
  and prices matched. The saved playthrough used previously supplied gear
  and raised stats as documented in the [merchant playtest](../MerchantConsignments20261123/README.md).
- A separate private Sill Continue kept the off setting. The player turned
  it on through Graphics, dropped the carried camp box and opened the
  gladroot stack's actions. `Put in camp box` showed the destination icon.
  The player deposited all four seeds. At tick 160 the exact stack and its
  green `spring seed` text belonged to the exact box on the ground.
- F5 and fresh Continue preserved box, stack, quantity and label. The
  pickup key lifted the loaded box in one action. At tick 170 the ordinary
  Expand contents action showed the stack indented beneath the box, with
  its seed icon and colored label. The original unset icon preference was
  restored after the last checkpoint.

## Verification

Three native tests reproduced the missing coverage. A second run exposed
three incorrect silhouettes and passed two shared-chest controls. The
final connected run passed all 378 cases. The generator reproduced the
current atlas and index byte for byte. Independent review matched all four
screens and all twelve checkpoint files. The editor stopped with the
original save root, and all 861 selected normal files and 86 selected
foreign workspace files matched the prior hashes. A new Mac package and
the full regression suite remain open. Available space was 148 MiB after
the Editor playtest.

[Trade on](coo-item-icons-final-trade-on.png) · [Trade off](coo-item-icons-final-trade-off.png) ·
[Deposit choice](coo-item-icons-final-deposit.png) · [Expanded pack](coo-item-icons-final-expanded.png) ·
[Observations](observations.json)
