# G3 east / kitchen reference selection

Selected 2026-10-24. Three primary museum/historic sources for shape and material study. No images downloaded. Approved Cook sprite remains unchanged. These are observational references, not textures or source images to copy into the game.

## 1. Small portable charcoal furnace / forge vocabulary

- Source: Smithsonian, National Museum of American History, **Charcoal Furnace**, circa 1900, record `nmah_1452871`.
- URL: https://www.si.edu/object/charcoal-furnace%3Anmah_1452871
- Source describes a cylindrical iron firebox, flared pedestal with draft holes, low domed pivoting cover, square side opening with an angled tool-rest shelf, wire bail handle, folded seam and riveted construction.
- Keep at 16×24: squat charcoal-gray cylinder; wider base; black square mouth; one projecting tool shelf; two separated vent pixels. One restrained orange inset can show an active fire. The projecting work shelf distinguishes a forge from a cooking pot.
- Accuracy: the original is a small furnace for heating soldering irons, not a documented full blacksmith forge. Adapt its compact construction language for our fictional small repair forge; do not mislabel the source artifact.
- Rights: Smithsonian search record explicitly says **Usage Conditions Apply** for media, while metadata is **CC0**. Image is reference-only. Current object page fetch returned an error, but the indexed Smithsonian record supplied the detailed description; visual inspection was not completed. Do not claim the photograph was inspected.

## 2. Rustic masonry oven

- Source: **Bake Oven**, Hopewell Furnace National Historic Site, U.S. National Park Service.
- Canonical page: https://home.nps.gov/places/000/bake-oven.htm
- Photo URL linked on that page: https://home.nps.gov/common/uploads/cropped_image/primary/B4912548-953C-781F-E137265D75E8170C.jpg?mode=crop&quality=90&width=1600
- Source photo caption/alt identifies a fire within a white stone structure; the article documents a double oven and retained-heat baking.
- Keep at 16×24: broad pale masonry body; large dark recessed mouth; stepped crown; two broken horizontal mortar lines; soot band above opening. Use a limited warm clay/gray palette. The heavy masonry mass must differ from the metal forge's narrow vented body.
- State cue: embers while heating, dark mouth when ready/idle; do not imply that a roaring flame is permanently required for baking. The historical oven cooks with retained heat after embers are removed.
- Rights: photograph credited **NPS** on the page. The page does not itself provide an explicit image license, so record as reference-only rather than asserting public-domain status.

## 3. Handled cast-iron cooking pan

- Source: Mont De Lancey collection, Victorian Collections, **Domestic object – Frying Pan**, identifier `3562`.
- URL: https://victoriancollections.net.au/items/678078110d49d20335c634a6
- Source describes a shallow cast-iron pan with a long tubular handle attached by three rivets and a hanging hole at the handle tip; dimensions 54×27 cm.
- Keep at 16×24: shallow oval bowl; one narrow long handle angled up; pale rim highlight only on one edge; single dark pixel at handle tip if readable. Omit rivet detailing at native size. A charcoal/gray mass, rather than a brown barrel shape, makes its material legible.
- Rights: source explicitly labels the media **Public domain**, with a request to acknowledge source, creator and title where known. Attribution: **Mont De Lancey / Victorian Collections, Frying Pan, object 3562; maker unknown**.
- Catalog discrepancy: prose calls it cast iron but material fields say “Tin, Metal.” Use it for silhouette; do not treat the material field as a reliable composition specification.

## Implementation priorities

1. Oven: large dark opening against pale masonry, recognizable before texture details.
2. Forge: narrow body plus side work shelf and vent pattern; do not reuse oven silhouette.
3. Pan: clear bowl-plus-handle silhouette, compatible with the approved Cook proportions.

The imagegen brief can describe those general construction features and use our own in-game screenshot as layout input. No source photograph needs to be embedded into the generated asset.
