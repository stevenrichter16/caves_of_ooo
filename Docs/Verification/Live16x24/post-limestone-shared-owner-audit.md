# Shared underground owners after the limestone material pass

Source audit, 2026-10-27. **Recommendation: a finite shared storage family—Chest,
LockedChest, StrongBox, Crate and OreCache—after the limestone/shale material
work.** This closes identifiable interaction targets in caves, ruins, ordinary
wilderness and settlements using existing art where it remains truthful. It
does not add containers, loot, encounters or generic decoration. CaveBear is a
small subsequent actor candidate; ChoirTendril and MarlbackScrabbler already
have portable native paths and should not be counted as missing again.

This file is the only change made for this audit. No runtime, tests, assets,
generation or Unity state changed; no new gameplay or package acceptance is
claimed. The current limestone implementation remains root's separate native
and actual-play validation task. No audit work blocks that delivery.

## What the retained observations establish

The [depth3 census](deeper-strata-before-depth3-census.json) was taken at39,12,
HP33 in `Overworld.16.13.3`; the [depth4 census](deeper-strata-before-depth4-census.json)
at40,12,HP33 in `Overworld.16.13.4`. For the families below, only one of the two
depth3 MarlbackScrabblers was visible at those census moments. All other listed
owners were hidden. Counts are actual graph contents, **not observed native
sprites, a safe route, or permission to disclose hidden positions**. The later
depth4 chamber capture has a different visibility state.

| Owner | Depth3 / depth4 count | Current native admission and available source |
| --- | ---: | --- |
| CaveBear | 0 / 1 | Missing from the flat actor catalogs. Existing `Sprites/Environment/cave_bear.png` is16×16 and a direct centered16PPU reuse candidate; it is registered only in the legacy bestiary. |
| ChoirTendril | 1 / 1 | **Already portable native:** `FlatPixelGrovelandsActorsCatalog` accepts the exact living `F` owner with its real Body, Brain, Conversation, Trader and Inventory. Uses unchanged16×16 `Environment/choir_tendril`. Current graph, disclosure and authorized reader still belong to callers. |
| MarlbackScrabbler | 2 / 0 | **Already portable native:** `opening-cast` frame2, with existing `opening-motion` actor2 derivatives. Do not replace it with the older16×16 environment image or64×384 legacy sheet merely because those also exist. |
| GlowQuartzVein | 1 / 2 | `opening-anchors` frame10 exists, but the opening selector requires `VisualID=reference-glade-lit-wall` and unspent GlowQuartz harvest. Ordinary unskinned veins are not admitted; the wall-attached opening silhouette is not automatically suitable for a freestanding mineral node. No dedicated ordinary vein source was found in the inspected environment/native catalog paths. |
| OreCache | 1 / 2 | Missing from flat shared ownership. Existing16×16 `Environment/cont_ore_cache` is a compact mineral recess candidate, distinct from a harvestable vein. |
| FrostVent | 2 / 0 | Missing native identity; no dedicated FrostVent source was found in the inspected sprite paths/catalogs. Its `^` does not authorize Stalagmite reuse. Requires a cold vent identity and preserved existing cold/reaction overlay, not optional plume animation. |
| BrinePool | 1 / 0 | Existing full20–23 / depleted-but-wet24–27 cells in34-frame `beating-materials`, scoped to eligible ordinary **surface Beating**. Separate16×24 `kitchen-brine` applies only to the current kitchen at12,11,0 and positive stock. Neither admits underground pools. |
| TarSeep | 0 / 2 | Missing native identity. Existing16×16 `Environment/tar_seep` is a reference/reuse candidate, not an opaque16×24 ground field. A square source must not be stretched to fill the taller cell. |
| Chest | 0 / 1 | Approved16×24 `opening-anchors` frame6, currently reached through opening/local admission. Legacy16×16 `Environment/chest` also exists; prefer the accepted native body if actual comparison supports it. |
| LockedChest | 1 / 1 | No explicit shared native identity. The legacy chest resolver conflates chest-shaped blueprint names; that is not the new flat contract. It may share the wooden Chest body but needs a truthful lock-state cue. |
| StrongBox | 0 / 1 | Missing shared native identity;16×16 `Environment/cont_strongbox` is available. Its iron-box description is **not** proof of Material/Destructible parts. |
| Crate | 2 / 0 | Approved16×24 `opening-cellar` frame1 exists under local opening admission. Legacy16×16 `Environment/cont_crate` is also available. The accepted rectangular native cell is the first reuse candidate. |
| VineWall | 15 / 15 | Connected native `sill-envelope` frames0–15 exist in Sill and eligible ordinary surface Grovelands. Their selected/disclosed neighbor checks are reusable; their regional admission is not underground admission. Legacy16×16 `vine_wall` is not required as a replacement. |
| Wall | 0 / 13 | Exact generic Wall is not LimestoneWall. Opening boundary art requires one of three authored glade VisualIDs; the plain Wall adapter is East-only at12,10,0. A mine-gallery masonry identity needs its own deliberate shared admission, not a natural-rock alias. |
| Pillar / BrokenColumn | 0 / 0 | Source candidates only, absent in both actual censuses. Legacy16×16 `Environment/pillar` served both despite their different collision. Opening BrokenColumn uses `opening-base` frame6 only for `reference-glade-dark-ruin`. Separate standing/broken identities remain necessary outside that role. |

Source anchors: [legacy mappings and flat early return](../../../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:150),
[flat branch](../../../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:1155),
[portable choir predicate](../../../Assets/Scripts/Presentation/Rendering/FlatPixelGrovelandsActorsCatalog.cs:38),
[portable actor lookup](../../../Assets/Scripts/Presentation/Rendering/FlatPixelActorMotionCatalog.cs:54),
[opening crate](../../../Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs:497),
[anchors](../../../Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs:604),
[opening/local world admission](../../../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:1026),
[reader admission](../../../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:1134),
[Beating scope](../../../Assets/Scripts/Presentation/Rendering/FlatPixelBeatingMaterialsCatalog.cs:50),
[Grovelands vine scope](../../../Assets/Scripts/Presentation/Rendering/FlatPixelGrovelandsMaterialsCatalog.cs:55),
[plain East wall](../../../Assets/Scripts/Presentation/Rendering/FlatPixelEastFixtureCatalog.cs:42),
[authored boundary identities](../../../Assets/Scripts/Presentation/Rendering/FlatPixelBoundaryCatalog.cs:118).

I read the PNG dimensions and importer fields for the named existing sources:
Point filtering, no mipmaps,16PPU, sRGB and uncompressed defaults are present.
I also viewed the original ore-cache, strongbox, bear, opening-anchor and cellar
PNGs directly. The small cache, compact box and bear silhouettes are recognizable
source candidates; this is not an in-game contrast/scale approval. Square
sources can use `Sprite.Create(Rect(0,0,16,16), pivot(.5,.5),16PPU,FullRect)`
without stretching or new PNGs, as the accepted Choir path already does.
Existing source pixels, imports and motion remain unchanged.

## Mechanic corrections that constrain the next art

| Apparent visual assumption | Actual source contract and consequence |
| --- | --- |
| Opening a container creates a persistent open-lid state | [ContainerPart:15–26,117–150](../../../Assets/Scripts/Gameplay/Items/ContainerPart.cs:15) stores Contents and lock access, but no persistent `Opened` field. Open dispatches an event and reports contents. Do not manufacture lid-open or empty art from a menu click, or reveal unseen contents through a sprite. |
| Only `LockPart.IsLocked` determines access | `ContainerPart.IsLocked` is `Locked || LockPart.IsLocked`. A key can clear one without clearing a legacy lock. Future lock visuals must use this live combined authority, while validating owned parts. |
| A chest must always remain physically solid to retain its identity | The real [unlock menu](../../../Assets/Scripts/Gameplay/Items/LockPart.cs:126) and [bump unlock](../../../Assets/Scripts/Gameplay/Combat/PhysicsPart.cs:140) can set Physics.Solid=false. Do not make a successful unlock turn the chest back into a glyph. Preserve mechanics and test both genuine action paths; do not infer a new lid state from passability. |
| All storage can use the same structural/material guard | Chest is Wood, HP14/hardness1; LockedChest Wood,18/2; Crate Wood,10/default hardness0 and no authored Thermal. StrongBox and OreCache have **no authored Material or Destructible**. Requiring those parts would reject real factory owners; do not add them as part of an art pass. |
| Destroyed storage remains as an empty box | [DestructionSystem:148–175](../../../Assets/Scripts/Gameplay/World/DestructionSystem.cs:148) spills contents, removes the owner, optionally creates declared wreckage and dirties the cell. These three destructible container blueprints declare no wreckage. Verify the actual dropped contents and removal instead of inventing a broken-box entity. |
| OreCache is a harvestable mineral vein | It is a solid Container, MaxItems6. GlowQuartzVein is a solid `MineralVein` with Harvestable yielding1–2 GlowQuartz. [HarvestablePart:126–164](../../../Assets/Scripts/Gameplay/Items/HarvestablePart.cs:126) spends/removes the source transactionally, including a valid failed-yield roll. No persistent depleted vein body should be invented. |
| Empty brine/tar draw stock means safe dry ground | Both retain TileStateSource; [EffectiveCoating and Seed](../../../Assets/Scripts/Gameplay/World/Map/TileStateSourcePart.cs:50) renew actual brine/oil regardless of LiquidPool.Volume. Brine's raw configured `water` resolves to brine. Tar is LiquidId oil with Material Tar. Any depleted source cell must remain wet/oily; mixed coatings, heat, charge and freezing must preserve the existing overlay/fallback policy. |
| FrostVent is merely blue rock | It is passable Terrain, Rime Material, initial temperature−25, Cold1 and `RadiatesEnergy=true`. Seed writes cold to self and four cardinal neighbors, not diagonal coatings. Preserve that hazard identity, avoid claiming extinguished/depleted states that do not exist. |
| Every solid-looking room prop is a wall | Pillar is a solid stone shaft with structure50/hardness4 and Rubble wreckage. BrokenColumn is passable Terrain with no Material/Destructible. Generic Wall is40/3 Stone; VineWall is14/3 Plant and combustible. Neither can be inferred from the current limestone band. |
| Bear art should require quadruped anatomy or a specific weapon | CaveBear inherits the current default Body and has HP35/AV4/DV1, Brain and DamageFlash; no anatomy or attack redesign is authorized. A future sprite predicate should follow real owned live parts and retain ordinary status/damage cues. |

Exact authored blueprint anchors: [Chest](../../../Assets/Resources/Content/Blueprints/Objects.json:24850),
[LockedChest](../../../Assets/Resources/Content/Blueprints/Objects.json:25088),
[Crate](../../../Assets/Resources/Content/Blueprints/Objects.json:30304),
[StrongBox](../../../Assets/Resources/Content/Blueprints/Objects.json:30496),
[OreCache](../../../Assets/Resources/Content/Blueprints/Objects.json:30563),
[GlowQuartzVein](../../../Assets/Resources/Content/Blueprints/Objects.json:28913),
[BrinePool](../../../Assets/Resources/Content/Blueprints/Objects.json:31946),
[TarSeep](../../../Assets/Resources/Content/Blueprints/Objects.json:32107),
[FrostVent](../../../Assets/Resources/Content/Blueprints/Objects.json:32507),
[Pillar](../../../Assets/Resources/Content/Blueprints/Objects.json:22077),
[BrokenColumn](../../../Assets/Resources/Content/Blueprints/Objects.json:22179),
[Wall](../../../Assets/Resources/Content/Blueprints/Objects.json:3280),
[VineWall](../../../Assets/Resources/Content/Blueprints/Objects.json:8088),
[CaveBear](../../../Assets/Resources/Content/Blueprints/Objects.json:17270).

## Small next slice and its verification boundary

**Shared storage is the first recommendation, with five exact blueprint owners,
not a generic ContainerPart reskin.** Start with Chest/Crate/OreCache body reuse,
then include LockedChest/StrongBox only after ordinary unlock captures settle
their essential lock cue. This is one connected family; unrelated corpses,
MimicChest, merchant stock, service furniture, sacks, urns and site-specific
reskins do not inherit it. No blanket “contains Chest” rule.

Reuse `opening-anchors[6]` and `opening-cellar[1]` unchanged. Trial the existing
16×16 OreCache and StrongBox centered at16PPU. LockedChest may borrow the wooden
Chest body with a separately authored, restrained lock-plate derivative; a
second strongbox lock-state view may be necessary. Decide exact derivatives
only after actual reference captures and the required concept/parts workflow.
Do not automatically create full/empty/open/damaged animation grids. Surviving
structural damage can retain the same body; destruction is real removal.

Use the existing optional companion and shared Greybox world/authorized-reader
seams. Admit exact neutral, current, placed and disclosed owners across regions,
without granting any regional ground or scenery admission. Reject unsupported
custom VisualID/variant, foreign/duplicate required parts, custody, multicell
footprints, conflicting service/creature meaning and destroyed bodies. Preserve
accepted authored opening/site roles before broadening ordinary unskinned
owners; a generic refusal must not erase an accepted specialized role. If a
container has an unsupported extra lock/state, retain truthful fallback rather
than falsely advertise ordinary access.

The finite native RED set should cover real factory positives and paired owner,
resource, current-graph, disclosure, placement and authorized-reader negatives;
ordinary key unlock with and without the key; retained legacy lock after key
unlock; both locked and unlocked Physics outcomes; open/loot/store without
invented lid state; real destruction with content spill/removal; and incremental
paint after normal state changes. Include one cave and one surface graph to
prove portability without broadening background ownership. Missing optional
textures must fail independently; dispose only cached views. No extra Render
events, per-cell generation, full-world scans or new framework are warranted.

The cross-region value is source-grounded: the
[underground pool](../../../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs:116)
contains OreCache, Crate and StrongBox; Spread, desert, ruins and settlement pools
reuse these families, while Sodden includes Crate. Authored
[MineGallery and Ziggurat](../../../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs:201)
provide Chest/LockedChest alongside mineral/Choir landmarks. Grovelands surface
container ecology deliberately excludes manufactured boxes; this work must not
insert any there. Existing settlements also place Crate, so reuse can improve
those actual objects without declaring a whole town finished.

For the eventual ordinary route, resume the retained earned depth3/4 fixtures
and locate a disclosed container through actual travel; the censuses do not
supply a verified safe endpoint. Capture its world body, actual menu/reader,
unlock if applicable, real loot transfer and return/restart. Revisit an already
observed surface crate as a second scope check. Retain interruption/death
honestly; no grants, teleports, visibility changes or new props to make a test
scene convenient. Confirm at normal zoom that wood cargo, hinged storage,
iron security and a mineral recess are visibly different. Then assess the
whole surrounding chamber: legible targets do not by themselves cure empty
rooms, repeated floor texture or disconnected landmarks.

## Subsequent priorities, deliberately separate

1. **CaveBear body reuse:** one exact living actor using its existing16×16
   image and the already established one-Render-event hurt/status cue. Its
   [depth tier2+ source](../../../Assets/Scripts/Data/Tables/PopulationTable.cs:1141)
   makes this relevant beyond one limestone fixture. Preserve corpse conversion,
   real damage and movement; do not add anatomy or combat mechanics.
2. **Shared liquid/vent hazards:** BrinePool first can reuse the accepted
   Beating full/depleted cells with narrow owner/state guards; TarSeep and
   FrostVent need their own meaningful source studies. All three occur in the
   [underground hazard pool](../../../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs:141),
   while brine/tar also occur across surface pools. Wet-zero, frozen, charged,
   burning and mixed states make this a separate tested slice, not a quick
   atlas-registration shortcut.
3. **Landmark envelope and minerals:** current VineWall mask reuse is promising;
   generic mine masonry, ordinary GlowQuartzVein, standing Pillar and passable
   BrokenColumn need distinct owner/shape decisions. Pillar/BrokenColumn are
   [room-generation possibilities](../../../Assets/Scripts/Gameplay/World/Generation/Builders/RuinsBuilder.cs:99),
   not facts observed in these two graphs. No new connectivity or decoration is
   justified without an actual chamber comparison.

🧪 All recommendations remain source/asset candidates. No new family is
implemented, tested, visually accepted or packaged by this audit. Existing
coverage-ledger cave/region statuses and the separate all-area polish gate are
unchanged. The ledger's historical sampled fallback lists can contain owners
subsequently integrated—MawToad, ChoirTendril and MarlbackScrabbler must be
checked against current code rather than blindly counted again.
