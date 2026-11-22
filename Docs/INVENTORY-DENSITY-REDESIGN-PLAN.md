# Inventory density redesign — proposed November 21, 2026

User direction: plan a redesign so many more items are visible at once.
This is a functional inventory proposal, not implemented behavior or a restart
of the cancelled graphics program. Preserve the existing visual language and art.

## What currently limits the pack

The live GameBootstrap camp scenario was opened with ordinary inventory input.
Its first page shows seven item entries and five category headings: twelve rows
out of twenty-five. The four camp kits are below the first page. A small pack
already needs scrolling to reach the tools for the current task.

The later actual workshop pack contains 71 item entries and six category
headings (77 rows); its first page shows ten items. Filled flasks and treated
garments can have identical visible names, forcing extra reader visits. Include
compact, disclosed state in rows where it distinguishes otherwise identical
entries: liquid identity/volume, charges and treatment markers. Search should
match those already disclosed facts too; never reveal hidden item properties.

The active renderer is `InventoryUI.LedgerView.cs`, not the old 80×45 tilemap
renderer. `LedgerListRows = 12` is fixed. A centered 1280×800 content rectangle
is scaled, so a larger monitor does not reveal additional rows. Equipment and
stats take 312 pixels of width; the pack has only 330 pixels of height; the
selected-item panel occupies most of the space below it. Quantity appears both
in stack names and in a separate column. These are layout costs, not a need to
make all text smaller.

## Recommended browsing view

Make Inventory a dedicated, full-height pack view. Keep Equipment as its own
comparison-focused tab. Retain Tinkering, Abilities, Crafting and Loadouts.

```
PACK   weight / capacity   drams   ink                modes / close
Find: ...       All | Weapons | Armor | Food | Materials | Tools ...
-----------------------------------------------------------------
ITEM                  QTY WT | ITEM                  QTY WT | SELECTED
...                         | ...                         | full name
...                         | ...                         | condition
...                         | ...                         | contents
...                         | ...                         | comparison
...                         | ...                         | useful facts
...                         | ...                         | F1 full details
-----------------------------------------------------------------
items 1–64 of 120    sort ...    navigation / actions / drop / details
```

Use two compact text columns for the pack, with a narrower details pane on the
right. These are rows with names and useful numbers, not icon-only tiles.
Both columns share one ordering and one selection. The current selected item's
full name, liquid contents, charges, proofing and equipment comparison remain
available in details; full Examine and action popups keep their existing routes.

Remove the persistent paperdoll/stats from Inventory. The Equipment tab retains
body-slot browsing, candidate comparison and relevant actor stats, including
extra limbs. Switching back restores the pack selection and scroll position.

Category filters occupy a compact top strip instead of consuming item rows.
The default All view remains sorted by category, then the existing stable item
order; show the selected item's category in details. Filtering and sorting are
free browsing actions. Keep `/` search and its current cancel/clear behavior.
Show stack quantity once, in QTY; keep meaningful name qualifiers and distinct
item identities. Do not merge merely similar garments, flasks or damaged items.
The weight summary must disclose both applicable limits when they differ: the
shipped pack's hard admission cap is150, while the current sidebar can show a
Strength-derived270. Explain the active limiting value without changing either
game rule as part of this layout work.
Move DMG/DEF to selected-item comparison rather than spending a wide column on
dashes for most materials and tools. Keep equipped and other important markers.

## Density targets, subject to native readability checks

Measure actual item entries, excluding headers and empty cells. Derive capacity
from available height and the chosen UI text scale, rather than a fixed row count.

| Window, default text scale | Proposed two-column item capacity | Current maximum |
| --- | ---: | ---: |
| 1280×720 | at least 36 | 12 rows including headings |
| 1280×800 | at least 44 | 12 rows including headings |
| 1920×1080 | at least 64 | 12 rows including headings |
| 2560×1440 | at least 88 | 12 rows including headings |

These are design targets, not measured new-screen results. Start with roughly
26-pixel rows and 20-pixel body glyphs at default scale; reserve room for controls
and status messages. The existing readable text size is the starting point.
Honor a larger text scale by reducing capacity. If columns become too narrow,
fall back to one full-height item column, with details available on demand.
Do not squeeze names into illegible columns to meet a count. At wide sizes use
the additional space for names before considering an optional third column.
Require space for at least 32 average-width name glyphs in each column, after
quantity, weight and markers; otherwise use one column. Remove only the view's
generated stack suffix when quantity is shown separately, preserving numeric
qualifiers in the item's actual name.

## Navigation and selection contract

- Use column-major order: a shared page fills the left column top to bottom,
  then the right. Both columns share one page/scroll offset. Up/down move within
  a column; left/right move to the neighboring item column
  at the same row. At a ragged end, choose the closest valid row. Browsing never
  wraps into a different tab or invokes an action accidentally.
- Page Up/Down advance a visible page. Home/End reach the beginning/end of the
  current filtered list. Tab keeps its current mode-switching meaning; category
  filters have explicit clicks/shortcuts that do not steal item actions.
- Enter opens the selected item's existing action menu; F1 opens full details;
  Escape returns to the same item. Pointer hit areas match painted rows, wheel
  scrolling follows the hovered list, and selection is visible without color alone.
- Preserve selection by item identity through sorting, filtering, reader return,
  resizing, stack changes and commands. If the item disappears, select the nearest
  remaining item. Do not preserve a stale row index that now names another item.
  A filter that excludes it selects the first matching item and remembers the
  previous selection; clearing the filter restores that item if it still exists.
  Explicitly selecting another item replaces that remembered selection.
- Retain contextual controller prompts and nested cancel behavior. This proposal
  does not reopen deferred Steam Deck hardware qualification.

## Small implementation sequence

1. Capture the current full workshop pack, an empty pack, long machine names,
   liquids, equipment comparison and a populated action menu at 800p and 1080p.
   Record item counts and actions needed to reach a camp kit/reagent.
2. Build the full-height two-column Inventory layout over the existing screen
   model and command paths. Centralize layout, capacity, hit testing and movement
   so painting and input cannot disagree. Leave Equipment's functional layout intact.
3. Move category headings into filters, remove duplicate quantity text, and add
   stable sorting only after selection retention works. Reuse existing details
   snapshots: hovering/painting must not repeatedly invoke mutable descriptions.
4. Validate native navigation and actual gameplay, then tune spacing and names.
   Do not rebuild custody, equipment, transactions, crafting or save data for this.

Likely files: `InventoryUI.LedgerView.cs`, `InventoryUI.LedgerInput.cs`,
`InventoryUI.cs`, `InventoryUI.Clarity.cs`; retain `InventoryScreenData` as the
model unless a small presentation-only item ordering adapter is needed.

Acceptance requires the density targets at readable scale, no clipped names
without accessible full names, all real items reachable exactly once, and stable
selection across 0/1/12/44/64/200 entries, resize, search and action completion.
Native input checks must include column edges, category filtering, long stacks,
extra body slots, F1 return and popup cancellation. Actual play must cover equip,
drop, throwing cancellation, liquid/camp actions and one paid action: quantities,
custody and turn costs stay governed by the existing commands. Obtain a bounded
independent source/UI review before acceptance. No redesign code is authorized
or claimed by this planning document alone.

Bounded independent plan review completed November 21: four requested navigation,
filter-return, name-width and quantity contracts are incorporated above. Native
readability, rasterized layout and implementation review remain future gates.
