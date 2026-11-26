# Carried storage and labels

The player can organize supplies into two visually distinct vessels even
when both have the same ordinary item name. A field satchel is light enough
to carry between outings. A camp box can be loaded at camp and packed up
with its contents. Both count against the player's carry limit.

## Carry and contents

- Use the existing `ContainerPart` for exact physical item custody and
  `ContainerLoadPart` for shell plus contents weight. A one-pound sack of
  eleven pounds of goods uses twelve pounds of the same pack capacity as
  the loose goods and the shell. Existing equipment still counts as usual.
- Use the existing camp box and give the field satchel its own portable
  blueprint. Existing sacks in ruins and towns serve as placed caches.
  Public installations, delivery parcels, fixed chests, and liquid flasks
  keep their own custody, weight, and stacking rules.
- The inventory's Open action shows an eligible carried vessel's exact
  contents. Stow and take move the same physical goods between the pack
  and the vessel. World opening still serves a vessel set down nearby.
  The first implementation handles loose goods and stacks. It keeps
  nested storage and loaded freight out of the reorganization menus to
  protect access and prevent self-insertion or compacting cargo graphs.
- Entry count is the vessel's existing storage capacity. Reorganization
  leaves total carried mass unchanged. Loading a vessel on the ground and
  taking it up is checked against the full pack with the load included.
  Corrupt or ambiguously owned load graphs fail closed. These actions use
  ordinary save and transaction ownership rules. Moving supplies between
  two parts of the pack gives no new gathering or quest credit.
- Empty portable vessels trade at the shell's price. Take contents out
  before trading them so each item gets its own eligibility and quote.

## Label data and controls

Keep optional cosmetic data on the physical item or stack. Use a fixed
mode, a named palette entry, and optional single-line text.
Old vessels start unmarked. Dropping, setting down, handing over, or loading
a game preserves the label on that exact vessel. Cosmetic data never
changes the ordinary display name, recipe identity, ownership, or price.

| Mode | What the inventory shows |
| --- | --- |
| Color mark | A filled pixel square in the chosen color |
| Text | A single short custom label in the chosen color |

Use a filled pixel swatch and a named palette with distinct bright tones
and readable ivory. Keep every color selectable for each swatch or text
label. The ordinary name stays visible beside the mark. Custom text
allows players to distinguish same-colored labels. Full details can name
the palette and the mark.

The selected item's **Label item** action opens a draft.
Choose a mode and color, type text if applicable, and confirm. Cancel
leaves the original label intact. Clear is an explicit choice. Names
accept one line of supported glyphs within a row-sized bound. Draft text
stays separate from command strings and color markup. Confirm checks the
same directly carried owner and the same original label captured when the
editor opened. Changes consume no game time or materials.

Verify exact allowance, refused excess, mass-neutral transfers, eligible
and excluded storage, every mark mode, clearing and cancelling, visible
colors, carried-to-world-to-carried identity, and fresh Continue. Exercise
the ordinary inventory buttons and collect a few actual in-game captures.

## Inline contents and labels throughout the pack

Clicking a carried vessel row expands its direct contents directly below
that vessel. Clicking again collapses the same exact vessel. Child rows
use left padding, stay with the parent when sorting or filtering, and show
their own item name, quantity, and weight. The vessel's loaded weight
already includes those items. Expanding leaves the pack weight and the
count of top-level stacks alone. Expansion is view state for the current inventory, with no
effect on game time or saved custody.

The parent row has a separate item-action button. Keyboard and controller
item actions offer Expand or Collapse. An accessible child can be labeled
where it is or moved into the pack with its own Take action. Eligible
healing tonics can also be used directly. Click and command selection
recheck the physical vessel and child.

Every ordinary item directly held or equipped by the player can carry
the same cosmetic data. Direct contents of an accessible carried vessel
can also be labeled. The same saved label Part supports all of them. A
label applies to the exact item or physical stack chosen. Separate one
first to mark just one unit. Splits retain the original mark on both
pieces and compatible stacks with equal labels can still merge. Matching
items with different marks keep separate rows and stacks, so transferring
them cannot silently erase a label.

## Item icons

Inventory and storage rows use a small pixel silhouette specific to the
item type. A satchel, a flask and a hand pump have different shapes.
The row still shows the ordinary name, quantity, weight and indentation
where applicable. A swatch or text label has its own color.

The same inventory-only atlas serves the pack, equipped items, carried
storage contents and the lists for opening or choosing world containers.
It also serves the physical item rows in Stow, Forge and Brew. Command
rows and ingredient requirements keep their ordinary text. The atlas
covers each portable item type and storage owner in the blueprint catalog.
It reuses existing art where possible and uses shared shapes for fixtures
with the same practical role. A closed chest uses the same icon regardless
of what opening it might reveal. New or dynamically generated types without
an atlas entry use a neutral icon. The menu only paints disclosed rows.

**Pause → Graphics → Item icons** turns the silhouettes on or off. This
is a player preference, enabled by default. An icon label made in an
earlier build becomes a simple swatch of the same color. Loading preserves
each physical entry and its quantity. Entries with equal swatches can
merge through ordinary inventory actions.

## Direct tonic use

An expanded slot offers the existing Drink or Apply verb for an
eligible simple healing tonic. Taking it out first remains available.
Both methods use one action and the ordinary tonic effect. Direct use
leaves a partial stack in the same slot with its label. The last unit
leaves the exact container and there is no temporary pack transfer.

Use the existing carried-storage checks and the ordinary inventory
action events. Bind the quote to the actor, physical vessel, exact
child, current quantity and eligible tonic payload. Check the same
custody after listeners run. On failure restore only the action's own
unit, if it still belongs to the original source. Apply the ordinary
payload only after payment commits. An open read or canceled chooser
spends no time. The existing labeled two-dose stack in Sill worked
in matched direct and unpack-and-drink branches.
[Native evidence](Verification/MachineSystems/StoredTonicUse20261126/README.md)
