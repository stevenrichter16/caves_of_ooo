# Incidental equipment visual and source review

Reviewed 2026-10-25 from saved actual 1280×800 captures and JSON receipts.
This reviewer authored the native pixel source, but did not implement the
runtime companion, operate Unity or run the native test suite. The following is
a separate review of the supplied gameplay presentation, not independent art
authorship.

## Hatchet evidence

Reviewed `incidental-items-kitchen-hatchet-reader-after`,
`incidental-items-hatchet-inventory`, `incidental-items-hatchet-equipped`,
`incidental-items-hatchet-dropped` and `incidental-items-hatchet-reloaded`, with
their receipts and the earlier kitchen reader/ground baselines.

The hatchet is visually acceptable in these views. No art adjustment is
recommended. Its short diagonal haft and compact single-edge head read as a
small tool, without an oversized battleaxe head or decorative effects. The
ground sprite is small but visible against grass. The Examine header and pack
preview show the same silhouette with no clipping or intrusion into text.
Equipping preserves the exact item preview rather than introducing a different
item or condition illustration.

The receipts track `Hatchet` owner 13663 throughout this kitchen graph. The
inventory view records it carried; the equipped receipt records it in a hand
alongside the buckler, with the long sword returned to inventory. The drop and
reload receipts both place that same owner at 24,5, while the player is at 24,7.
Zone, player position, HP 42, tick 6030, inventory, equipped items and the complete
hatchet record match across drop/reload. The world sprite remains visible after
reload. The earlier reader capture identifies the same ground owner and shows
its ordinary hatchet description and mechanics with the new image.

## Leather armor evidence

Reviewed `incidental-items-north-armor-reader-after`,
`incidental-items-armor-equipped`, `incidental-items-armor-carried`,
`incidental-items-armor-dropped` and `incidental-items-armor-reloaded`, with
their matching receipts.

The armor is visually acceptable in these views. No art adjustment is
recommended. The reader and pack previews show an empty sleeveless leather
vest: the open neck, shoulder straps, joined panels and small fasteners remain
readable. Its muted color and plain silhouette do not imply damage, repair or
an extra wearer. The same item image fits the reader header and both equipped
and carried previews without clipping or obscuring the descriptions. On the
ground it is necessarily smaller, but the vest remains visible beside the
player and distinct from the grass in both the dropped and reloaded captures.

The receipts track `LeatherArmor` owner 6954. The reader identifies it at 16,17
in `Overworld.11.9.0`. The equipped capture shows it in the Body slot; the
carried capture shows the same owner in inventory and Body empty. Their player
position 16,17, HP 32 and tick 2360 match. The visible AV/DV values change from
4/7 while worn to 1/8 while carried, consistent with the displayed +3 AV and
-1 DV contribution. The gameplay log records pickup, equip, unequip and drop;
the capture sequence does not independently establish which keys were pressed.

Both drop and reload receipts place the same armor owner at 16,18 and the
player at 17,18. Zone, player position, HP 32, tick 2380, normal UI state,
inventory, equipped items and the complete armor record match. The new ground
image remains visible after reload. This accepts the supplied reader,
equipment, inventory and clear-ground presentation of this armor instance.

## Source and runtime review bounds

Both imported equipment PNGs were checked against the source export hashes:
exact 16×24 dimensions, source palette and binary alpha matched. Import metadata
specifies Point/Clamp, centered pivot, 16 PPU, no mipmaps and no compression.
The inspected companion keeps resource failures independent, preserves injected
constructor behavior, uses neutral art under mutable condition/combat/name
values, and disposes cached sprites/tiles without destroying borrowed textures.
World selection and authorized reader subjects remain distinct paths.

Correction to the earlier cold-review hypothesis: a late `Entity.AddPart` call
for a zero-offset footprint is **not** a stale-registration reproduction.
`Entity.AddPart` refreshes footprint registration. That example is withdrawn.
The separate proposed counter directly changes `CellsRaw` from a registered
two-cell footprint to one cell. The saved footprint RED summary records the
expected native-reader substitution failures for both armor and hatchet. The
current `SingleCell` guard checks `IsFootprintCurrent` for placed owners while
allowing unplaced or carried equipment. The saved final native result is
147/147 passed, including both stale-footprint cases and both valid-single-cell
cases. The tests also retain the valid late-`AddPart` counter and canonical
registration repair. This closes that specific native-art authority finding;
the implementation/test agents ran the suite, not this reviewer.

The source and export manifest now describe bounded integration status. The
manifest explicitly remains authoring evidence and points to the graphics
document for runtime and gameplay verification.

These captures accept the two supplied item identities in the reviewed states.
They do not establish every equipment identity, every damaged state, controller
comfort, physical Deck performance or packaged delivery.
