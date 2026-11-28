# World actions name the selected object

Selecting an object on an occupied tile opens that object's heading
and shows its own health if it has health. The tile summary and the
target picker still describe the cell. The ground status has its own
section. Generic choice popups continue to use their supplied headings.

## Actual Sill replay

I used Continue from the ordinary seed-65 save after the flask drained
the bank. The settling pocket and a soot gremlin shared the tile south
of the traveler. At tick 1520 with 32 HP, C south opened the gremlin's
menu showing 15/15 HP. I opened “everything here” and picked the
exact pocket. The same gravel action that formerly sat below “You see
a soot gremlin” now reads “You see the settling pocket.”

F1 showed the selected gravel action and the exposed pocket. Escape
returned to the same heading and highlighted action, and “everything
here” rebuilt the same picker. Selecting the gremlin showed its
own health and Soaked status. Closing the menu kept the original tick,
health, purse, flask, inventory and still unspent pocket. The before
screenshot comes from the preceding runtime at the same saved tile.
Its log groups one extra menu interaction.

## Verification

Old code passed 24 focused cases and failed three heading regressions.
The final build passed all 208 cases in the heading and adjacent
fixtures, including pile summaries and controller choice popups.
A separate reviewer checked the source, screenshots and play receipts.
The Editor ran on Apple M5. The 880 ordinary saves, 87 pre-existing
tracked edits and both preference files matched their original bytes.

[Before](01-before-selected-pocket.png) · [Selected pocket](02-selected-pocket.png) · [F1 details](03-pocket-details.png) · [Selected gremlin](04-selected-creature.png) · [Records](observations.json)
