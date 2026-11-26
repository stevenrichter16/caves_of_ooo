# Complete inventory and storage icon coverage

The inventory-only atlas now has 389 blueprint mappings and one neutral
fallback. That includes 354 portable types and 35 additional storage
owners. Twenty-six owners gained their first inventory icon, including
wooden barrels, woven baskets, shelves, lockers and receiving trays.
Related variants share art. All 364 existing frames retain their pixels,
and the generator reproduces the current atlas and index byte for byte.
The ordinary, locked and mimic chest images match. The icon lookup uses
the known item type and never reads container contents.

The carried-storage Stow chooser and the physical inputs in Forge and
Brew use the shared row icons. Navigation, headings and empty groups
remain text rows. **Pause → Graphics → Item icons** applies throughout
these menus and defaults to on. The [original inventory release](../InventoryRowIcons20261126/README.md)
records the color-mark and text label editor and the migration of older
icon labels to same-color swatches.

## GameBootstrap play

- A private Sill Continue opened at tick 160 with the default setting.
  The usual Inventory and Stow commands showed four item icons and two
  navigation rows. Turning icons off through Graphics cleared all icons
  from the same chooser. The four choices and the 38-pound pack remained
  available at the same tick.
- With icons off, the player stowed four gladroot seeds from exact stack
  2707 into satchel 7341. The stack's `spring seed` label and color
  persisted. A native F5 and fresh Continue preserved its owner, pack
  weight, tick and the off preference.
- A separate earned Cinderhold checkpoint showed the iron spike, rough
  field haft and leather binding in Forge with their icons. Brew was
  initially empty. The visual audit then supplied frost lichen and
  mendleaf and opened Brew normally to verify both disclosed icons.
- The same isolated visual audit supplied a barrel and a basket on an
  otherwise empty player tile. The ordinary pickup key opened the
  container chooser with distinct icons. The real settings control
  hid both icons on reopening. This was a supplied menu fixture.

Independent review matched the twelve files in the three private archives
and the five captures. The default unset preference was restored and
the isolated editor was stopped. The existing manifests matched all
861 checked normal-save and preference files and 86 checked foreign
workspace files.

## Verification

The previous atlas and row renderers fail the four new coverage cases.
The connected native run passes 425 cases with zero failures, skips,
inconclusive cases or compiler errors. The first trial stopped during
compiler postprocessing because the disk was full. The final runs used
the existing warm verification clone after reclaiming compiler cache
space. The available space still prevented a new Mac package.

[Stow on](01-stow-on.png) · [Stow off](02-stow-off.png) ·
[Forge](03-forge.png) · [Brew](04-brew.png) ·
[World storage](05-world-storage.png) · [Observations](observations.json)
