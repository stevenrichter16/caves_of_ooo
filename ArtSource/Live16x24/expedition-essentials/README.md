# Expedition essentials — immutable approved-art reuse

Source review, 2026-10-28. This packet records three existing bodies; it authors no new pixels, editable geometry, animation or import settings. Portable integration and actual after-image acceptance remain separate gates in [the phase plan](../../../Docs/GRAPHICS-EXPEDITION-ESSENTIALS.md).

## Exact bodies and parts

| Exact owner | Borrowed texture and complete Sprite rectangle | Readability and semantic limits |
| --- | --- | --- |
| HealingTonic | `Sprites/Live16x24/opening-items`, 192×24 atlas, frame7: `(112,0,16,24)` | Cream shoulders/body, brown stopper, worn rust paper band. Compact stoppered medicine, with no visible fill level, healing aura or reusable empty-bottle claim. |
| DriedMeat | Same atlas, frame8: `(128,0,16,24)` | Pale tie/knot and two unequal rust-brown strips with darker dry edges and separated tips. The two illustrated strips identify food; they do not encode the current stack quantity or guarantee bait behavior. |
| WateringGrimoire | `Sprites/Environment/item_book`, entire16×16 texture: `(0,0,16,16)` | Compact gray cover/page mass with a dark outline and pale page accents. Neutral book identity, with no rain, glow, open/read state or learned-skill cue. It does not depict a special water spell on its cover. |

Use centered pivots, square pixels and16PPU. Keep the book square: it occupies1×1 world units inside the existing1×1.5 cell. The tonic/meat use their full1×1.5 frame. Existing importer sub-sprites are cropped (tonic6×10, meat8×12, book12×9); borrowing those crops would change the required canvas/placement. Preserve all import bytes and construct complete-frame views from the borrowed textures. Existing Point/no-mipmap/uncompressed/Clamp settings were read only.

## Existing provenance and reuse critique

The tonic and meat retain the original [opening items editable Blender source](../items/opening-items.blend), [named pixel cells](../items/source-cells.json), [manifest](../items/manifest.json), [builder](../../../Tools/Live16x24/build_opening_items.py) and [contact sheet](../items/opening-items-contact.png). The builder names the bottle's stopper/neck/shoulders/base/paper band, and the meat's tie/knot/left-right strips/dry edges. This audit did not rebuild or reopen the Blender file; it checked the saved source cells against the approved atlas directly.

Their existing [loose-item concept](../concepts/loose-items-v1.png) and [prompt/critique](../concepts/loose-items-prompt.md) remain useful provenance. The native art already implements the capped vessel and tied strips as flat integer pixels, discarding the concept's irregular pixels and generated shading. Reuse the accepted native bodies; do not crop or resize that concept. No new concept or duplicate source archive is needed.

For the book, the approved PNG itself is the established source. Original authoring file, artist provenance and an editable model have **not** been established; this packet makes no reauthorship claim. The legacy `EnvironmentSpriteRenderer.ResolveItemBody` maps grimoires to `item_book`, but the new family must retain exact WateringGrimoire identity rather than copying that broad substring rule.

The compact bottle and tied meat are distinguishable in the inspected native contact. The book's restrained grayscale body is less role-specific and may enlarge differently in the square portrait canvas; actual world/reader scale and dark-ground contrast remain the next visual check. There is no source-art blocker requiring a new sprite before that trial.

## Read-only checks and preservation

`protected-inputs.json` is a plain repository-relative path→SHA256 map of13 relevant approved PNG/source/concept files. It deliberately contains no `.meta`, foreign-work, mutable runtime, save, or whole-repository pins. Root's separate preservation audit owns foreign changes and import/build state. Do not silently repin a mismatch.

- Approved runtime/source opening atlases are byte-identical: `1ead2618c2d82d2ca7cae8765ab09f690255998ad0bb5b36027d7835fc292394`.
- Full book PNG: `5a15f96a509ada25d3400f023ce63fb7072f4671d1091bd5e5a17474d260cbec`.
- Both384-pixel tonic/meat frames match all768 saved source cells exactly, including transparent pixels. Tonic has40 opaque pixels and6×10 bounds; meat44 and8×12. Both touch image row22 and leave row23 clear.
- Book has96 opaque pixels and12×9 bounds within16×16. All three alpha sets are exactly0/255 and each body uses four opaque colors.
- Full-frame top-down RGBA hashes: tonic `10251b6e1182dc68f94a313ad82deda5a36f54df85301bdfe005c2dd835c93f6`; meat `39696ca829ccd452b2a557c1c715b16f5da55b0188f1e6fd61ea4c335c44ee28`; book `e158c28f783cb040afb15a431b73a5c4368c9eac54bbf9fb71ee5406fcc83a32`.

These are read-only file/pixel checks, not a new native test run, Blender reproducibility run or in-game acceptance.

## Actual baseline and remaining gates

Viewed all five `Docs/Verification/Live16x24/expedition-essentials-before-01..05` captures and the inventory/ground receipts. The carried readers show the existing red meat glyph, green tonic glyph and blue book glyph. In OpenMire18.8.0 the real dropped owners are WateringGrimoire2696×1 at40,10; DriedMeat2702×2 at41,10; HealingTonic2701×2 at42,10, with player43,10/HP38. These are baseline observations, not proof that the new portable bodies have rendered.

Actual after-image identity/contrast, authorized carried and selected ground ownership, pickup/consumption/removal, skill-reading behavior and persistence remain root's integration gates. One static body does not imply use readiness or current quantity. The baseline still has broad sparse ground and repeated vegetation/mire patches; three small item bodies do not satisfy whole-chunk fullness or area-polish acceptance. No platform, package or performance claim is made here.
