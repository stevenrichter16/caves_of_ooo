# Equipment ground sprites

Seven authored16×16 pixel silhouettes supply the exact ten-blueprint contracts in `GameAuditEquipmentGroundSpriteAdversarialTests`. Dagger, sword, spear and mace have visibly different blade/shaft/head proportions; paired boots and gloves and an open-face helmet distinguish armor slots.

`python3 ArtSource/EquipmentGroundSprites/generate.py --preview /tmp/equipment.png` regenerates only these seven PNGs. Pillow draws directly on16×16 pixels with no antialiasing. A dark shared outline `(30,32,28)` surrounds two near-gray body shades, preserving the renderer's native glyph tint. Alpha is0/255. Original equipment images and unrelated sprite mappings are untouched.

New metadata copies `weapon_ground.png.meta`, changing only GUID; repeat generation retains GUIDs. Existing tests exercise actual Resources imports, full frames, exact routes, distinct masks/GUIDs, glyph fallback when a resource is missing, tint/visibility and dirty-cell ownership. Missing these assets/routes was recorded RED in RS28 before this R1 repair. The generator and static PNG checks do not substitute for Unity importer/render tests or live camera inspection.
