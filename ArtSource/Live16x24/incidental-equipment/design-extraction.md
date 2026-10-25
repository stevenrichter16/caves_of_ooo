# Concept extraction and state mapping

Input: actual `incidental-items-kitchen-hatchet-reader-before.png` and
`incidental-items-north-armor-reader-before.png`, plus accepted `long-sword.png`.
Exact imagegen prompt is `concept-prompt.md`; output `concept-v1.png`.
Generated sheet is design reference only, not a screenshot or native export.

Retain: empty vest's neck and arm negative space, two restrained fasteners,
limited leather panels; diagonal short haft and one-sided wedge head.
Reject: generated shaded/uneven grid, oversized near-battleaxe blade, redesigned
comparison sword and any invented in-game scale claim. Existing approved sword
is byte-preserved. Native source authoring uses deliberate integer cell rows,
not a crop, traced downsample or generated pixels in runtime.

| Native part | Exact owner | Footprint / pivot | States |
| --- | --- | --- | --- |
| leather-armor | LeatherArmor + owned Armor + Equippable Body | One16×24 cell, center8,12 at16PPU | Neutral sound/damaged/repaired/renamed item; condition remains authoritative text/FX. |
| hatchet | Hatchet + owned MeleeWeapon + Equippable Hand | One16×24 cell, center8,12 at16PPU | Neutral loose/carried/equipped item; no fabricated temper/attack state. |

Both require actual Item/visible owned Render and portable owned Physics,
valid stack if present, no foreign role or explicit visual override. World
selection remains the current visible top owner; armor under coins is not painted
over them. Exact current world/carried reader may show the lower armor. Pickup,
drop, equip and stale-document rejection remain normal systems. New art stays in
current admitted opening graphs, with explicit fallback elsewhere. Two small
borrowed Resources textures produce cached sprites/tiles, no entity GameObjects.

Authoring review must inspect native size, enlarged contact, and actual world plus
reader output. Source/contact acceptance alone does not close gameplay acceptance.
