# Icons in the inventory and storage

**Pause → Graphics → Item icons** controls the item art in the pack,
equipment, item choices, pickup and world storage lists. It defaults to on.
An expanded carried vessel shows the same icons on its indented child rows.
Labels remain separate color swatches or custom text.

The inventory-only atlas indexes 354 current portable item types, nine
world storage types and a fallback for an unknown type. It includes 237
distinct pixel images. Some variants share a silhouette. Hidden chest
types share the closed chest image to avoid exposing information before
opening. The original world atlas and index retained their previous
hashes. The editable source reproduces the new PNG and index byte for byte.

## Actual play

The private Sill checkpoint already contained marked gear, two satchels
and a camp box. The equipped dagger, cloak, grimoire, satchel and stored
meat converted from older icon labels to their original colors as
swatches. The text and quantity of the `field meds` stack remained exact.

Opening the pack with the default setting showed ten icon rows. Expanding
the loaded satchel showed twelve. Its two stored stacks kept their original
owners and quantities. The normal Graphics toggle hid every row icon.
The two indented rows, swatches, text and 38-pound pack weight stayed
visible. Both views and the toggle were observed at the same game tick.
Fresh Continue from the migrated save matched the same physical entries
and kept icons off. The ordinary label editor cycled from Color mark to
Text and back.

Independent review matched all four archive files. The original and
migrated uncompressed game saves differed at nine bytes only. Five former
icon modes became swatches and four nonzero icon choices cleared. The
original unset preference was restored after testing.

The three intended row failures were reproduced with the prior renderers:
the dense pack had no item art and the two storage menus drew their old
glyphs. The current build uses shared icon art for both kinds of row.
The connected native run passed 489 cases with no compiler errors,
failures or skips. It covers storage lists, the expanded child hit area,
graphics settings, save compatibility, legacy world icon behavior and
connected generation. The whole inherited suite and packaged play remain
open.

[Expanded pack, icons on](01-expanded.png) · [Graphics toggle](02-setting.png) ·
[Same pack, icons off](03-off.png) · [Label editor](04-label-editor.png) ·
[Observations](observations.json)
