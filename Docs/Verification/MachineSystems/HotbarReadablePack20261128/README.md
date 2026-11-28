# Larger inventory sections and skill hotbar

The large Inventory tab paints a heading before each item category and
repeats headings where sections continue into a new column or page.
Default item rows grew from 24 to 36 pixels and names from 20 to 26
pixels at normal scale. The icon boxes grew to 28 pixels. Name and
weight sorting work within each category. The list still selects and
opens the same item, including counted groups and nested storage.

The game hotbar uses ten cards aligned with the existing slot targets. Each skill shows its binding, icon and current
state. The summary and help line has no activation hit. Full-screen
inventory hides the strip.

Ordinary Continue loaded the earned Tally save at tick 5480 and 40 HP.
Opening Inventory revealed nine item rows and six categories without
advancing time. Back in gameplay, the 1 key used En Garde. The same
card showed its cooldown at tick 5490, with HP still 40. Independent
reviews covered item custody, pointer details and the hotbar help
strip. All 640 focused Edit Mode tests passed.

[Inventory](02-inventory-categories.png) · [Hotbar ready](01-hotbar-ready.png) · [En Garde cooldown](03-hotbar-in-use.png) · [Play and test observations](observations.json)

The universal Mac package reproduced ordinary Continue, the categorized
Inventory and En Garde's visible cooldown on Apple M5.
[Mac inventory](mac-inventory-categories.png) · [Mac cooldown](mac-hotbar-use.png) · [Mac package receipt](mac-package.json)
