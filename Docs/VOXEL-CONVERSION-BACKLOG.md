# Remaining voxel conversion backlog

Status: read-only native-content and source inventory, 2026-09-16. No conversions
are implemented by this document. Counts describe the current authored map and
generation rules, not every possible saved or externally constructed zone.
The separate [coverage inventory](VOXEL-WORLD-COVERAGE.md) records what is already
installed and how to reach it.

The six authored surface biomes already have ordinary-chunk composition
systems. The remaining work falls into three different categories:

1. Existing native places or generation families outside the voxel scope.
2. Missing or incorrectly scoped owner models inside otherwise converted areas.
3. Lore locations/mechanics that have not been implemented and therefore cannot
   honestly be described as a simple art conversion.

## Counted scope

| Category | Established scope | What the count does not mean |
| --- | --- | --- |
| Authored surface map | 400 coordinate slots; 386 accepted by the current voxel address selector; **14 exceptions** below | A supported address does not prove all owners have models or that a seed-specific POI uses an ordinary biome composition |
| Converted lower levels | **8**: depths 1 and 2 under Ginmere, Cathedral, Stillleaf and Olderdeep | Their depth-3-and-lower descendants are not converted |
| Unconverted named lower levels | **4**: Lampwell and Spivenor, each depths 1 and 2 | Their two surface mouths are already included in the 14 surface exceptions |
| Other depth-1/2 slots | **788** coordinate slots route the generic underground family | These are possible native coordinates, not 788 prebuilt, individually authored or visited caves |
| All unconverted depth-1/2 slots | **792 of 800**: 788 generic plus four named legacy levels | This is a bounded two-layer count only |
| Depth 3 and deeper | All **400 world columns per depth** use generic underground generation; no authored maximum depth is imposed by `GetZoneBelow` | Do not publish a finite grand total of underground zones |
| Other non-ground zone | `WorldMap`, one navigational zone | This is the map interface, not an omitted explorable voxel biome |

The 386/400 surface figure comes from `VoxelWorldPresentation.IsSupported` and
the composition-plan eligibility sets. The lower-layer arithmetic follows
`OverworldZoneManager`: six authored sinkhole columns receive special handling
at depths 1 and 2; four of those columns have the new voxel treatment. Remaining
columns use `CreateUndergroundPipeline`. The six columns cease using their
special floor builders below depth two.

Sources: [presentation scope](../Assets/Scripts/Presentation/Rendering/VoxelWorldPresentation.cs)
(`IsSupported`, around line 169),
[map traversal arithmetic](../Assets/Scripts/Gameplay/World/Map/WorldMap.cs)
(`GetAdjacentZoneID`/`GetZoneBelow`, around lines 283–304),
[live dispatcher](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs)
(lines 70–86, 343–475).

## All fourteen authored surface exceptions

| Place / location | Exact surface address | Existing native content | Remaining conversion or authoring work |
| --- | --- | --- | --- |
| **Sill** | `Overworld.10.10.0` | Spread large village, five starting-town shop stamps, ordinary merchants, producers and repairable well/oven/lantern sites; earlier 3D assets | A coherent voxel town treatment that preserves those services and settlement records; not another empty town shell |
| **Posy** | `Overworld.5.9.0` | Spread generic village, BowerFolk faction, null profile | Distinct Bower-Folk architecture plus voxels. The canonical Resin-Cast Bower and aesthetic-reputation society are additional content, not existing owners to reskin |
| **Slip** | `Overworld.16.11.0` | Spread generic village, Villagers faction, null profile | Village composition and voxels. East-quarter Hush/name loss is not a shipped district mechanic |
| **Salt-Vault** | `Overworld.15.15.0` | Beating generic village, PaleCuration faction, null profile | Archive/intake architecture and voxels. The Salted's god-room, annual file review and mass-preservation complex need separate native design |
| **Quiet's Door** | `Overworld.16.1.0` | Sodden generic village, CatacombFolk faction, null profile | Threshold-village composition and voxels. No dedicated connection to a separately implemented Quiet settlement was found |
| **Lampwell mouth** | `Overworld.12.3.0` | Spread legacy wilderness plus native sinkhole mouth | Mouth/descent/floor treatment as one coherent stack; floor details below |
| **Spivenor mouth** | `Overworld.16.4.0` | Sodden legacy wilderness plus native sinkhole mouth | Mouth/descent/floor treatment as one coherent stack; floor details below |
| **Root of the World reservation** | `Overworld.3.3.0` | Stump legacy surface terrain/formation; tier-five reservation, no dedicated Root POI | Visual conversion of current terrain is possible, but the actual First Root room and ending rituals are unimplemented capstone work |
| **Felling-Site** | `Overworld.3.5.0` | Dedicated native Felling scene with existing smooth 3D presentation, seven positions and persistent component owners | Convert the existing authored scene's art, preserving ownership/removal and the six-versus-seventh composition; do not recreate it as random Stump scenery |
| **Unsaying reservation** | `Overworld.2.11.0` | Legacy Overwrit fallback: sparse DesertBuilder terrain plus native Ruins-derived circulation; no named town POI | Authored Unsaying settlement and voxel treatment. No existing bleed sidegraph can simply be switched on |
| **Woven Doll grove** | `Overworld.1.6.0` | Deliberately bare forced Grove, WovenDoll stamp and native fauna; no ordinary landmarks/containers/Bloom-front | Small restrained voxel scene. Preserve the isolation rather than add normal grove clutter |
| **Tenth Fire** | `Overworld.2.19.0` | Deliberately bare SaltPan, one authored fire, native fauna; no veins/ambient stamps/containers | Small restrained voxel scene retaining the untended-fire mystery |
| **Abandoned Counter I** | `Overworld.19.18.0` | Native Beating formation with a guaranteed abandoned-counter stamp of broken SandstoneWall and Bones, if its normal wilderness route is selected | Coarse ruined-outpost art and composition preserving its absence of service/trade |
| **Abandoned Counter II** | `Overworld.19.19.0` | Same native stamp family at the second address | Related but meaningfully varied ruin; do not clone a live Last Counter service hub |

The five generic villages are real, reachable villages already. Their lack of
a unique composition is not a missing POI or inaccessible-town bug. The current
authoring table's biome/tier/profile wins over older lore-table tags.

The two abandoned-counter addresses are excluded from ordinary Beating
composition, but are not in the manager's explicit authored-wilderness
reservation list (that list contains Tenth Fire and Woven Doll). Their special
stamp is added only by the normal Beating route. A future conversion must test
seed-specific POI precedence rather than claim both ruins are guaranteed in
every possible world merely from those constants.

Sources:
[all named places](../Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs)
(lines 211–227), [village routing](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs)
(around 790–975), [Felling runtime ownership](../Assets/Scripts/Gameplay/World/FellingSceneRuntime.cs),
[Felling presenter](../Assets/Scripts/Presentation/Rendering/FellingScenePresenter.cs),
[authored wilderness pipelines](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs)
(around 581–665), [abandoned-counter stamp](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs)
(`AbandonedCounter`, around 974).

## Lampwell and Spivenor: six native zones, four of them below ground

| Area | Entire conversion slice | Current native archetype | Useful existing owners and interactions |
| --- | --- | --- | --- |
| Lampwell | `12.3.0`, `12.3.1`, `12.3.2` | Mouth → descent → `StrandedSettlement` | HearthPatch, broken Drosera ring, NicheHome, plaque wall and six named plaques, BeetleJar, PebbleSundewThreshold, CatacombWarden and PlaqueTender |
| Spivenor | `16.4.0`, `16.4.1`, `16.4.2` | Mouth → descent → `StrandedSettlement` | The same shipped generic settlement family and native conversations; presently not a separate Choir-access simulation |

The hearth has native social consequences; the threshold is a walkable
announcement surface; plaques have actual read/examine content; the residents
have actual conversation owners. These provide a stronger conversion base
than inventing unrelated decorative shops. `StrandedSettlementBuilder`
explicitly says it is **not a shop**: it does not add a TraderPart or stock
table. A grand light-market economy must not be inferred from the lore name.

The contrasting canonical directions are sound, but require honest scope:

- **Lampwell:** light-work, public glow and cultivated illumination. Its
  lantern-beetle hatcheries, Concord light-trade economy and dedicated pruning
  contract are lore ambitions, not shipped hatchery/service implementations.
- **Spivenor:** inhabited substrate and Choir integration. Choir-reputation
  entry gates, the distinct Spore-Wedded village policy and Wall-Caught welcome
  are not implemented by the generic Warden conversation. Any new gate is a
  mechanics task with positive and refusal tests, not scenery.

No need to add Olderdeep's Rooted body or founding plume here: those belong to
the already converted founding site. Existing Olderdeep niche/plaque/jar art
may be reusable, but regional bindings and the two native settlements' actual
owner coverage still need checking.

Sources: [SinkholeSites](../Assets/Scripts/Gameplay/World/Map/SinkholeSites.cs)
(lines 40–49), [archetype selection](../Assets/Scripts/Gameplay/World/Map/SinkholeArchetypes.cs),
[native stranded settlement](../Assets/Scripts/Gameplay/World/Generation/Builders/StrandedSettlementBuilder.cs),
[actual resident dialogue](../Assets/Resources/Content/Conversations/Catacomb.json),
[canonical geography](../Lore/History/02_Geography.md) (Lampwell line 76,
Spivenor line 83), [Choir distinctions](../Lore/Factions/01_RotChoir.md)
(around lines 123–125 and 269–274).

## Generic underground: the largest existing-content gap

This is a live gameplay system, not a lore placeholder. Ordinary cave entrances
roll on eligible surface chunks; authored sinkholes supply guaranteed vertical
routes; underground edges preserve depth and lead to neighboring columns.
StairsDownBuilder continues to deeper levels. Conversion should therefore be
organized as reusable depth-band and encounter grammars rather than hundreds
of hand-authored coordinate exceptions.

| Existing material band | Depths | Current base family |
| --- | --- | --- |
| Sandstone | 1–2 | SandstoneWall / SandstoneFloor; open shallow caves |
| Limestone | 3–4 | LimestoneWall / LimestoneFloor; shallow cave parameters |
| Shale | 5–6 | ShaleWall / ShaleFloor; tighter middle caves |
| Slate | 7–8 | SlateWall / SlateFloor; middle cave parameters |
| Quartzite | 9–10 | QuartziteWall / QuartziteFloor; narrow deep tunnels |
| Obsidian | 11+ | ObsidianWall / ObsidianFloor; continuing deep family |

The common spine is solid earth → strata carving → connectivity → up/down
stairs → stair connectors → underground landmarks → hazards → depth population
→ containers. Native landmark families include:

- **MineGallery:** GlowQuartzVein and DeepSupplyT2 chest, minimum tier one.
- **CurationGallery:** actual PaleCurator, Campfire and supply chest, minimum
  tier three. This is an existing native service encounter worth preserving.
- **Reliquary:** VaultSentinel, locked SealedVaultT3 chest and IronKey, minimum
  tier four. The actual key/lock and destruction semantics own the encounter.

Population changes with depth as well as terrain. Snapjaw families, Glowmaw,
Stalagmite and later cave bears, rotlings, sentries and other deeper creatures
are not covered by merely giving the six wall types voxel meshes. Liquids,
hazards, dropped equipment, corpse forms and container contents also need
explicit rendering and lifecycle coverage.

The Root's underground coordinates (`3.3.1`, `3.3.2`, and deeper) currently
enter this generic family. They are **not** a hidden completed First Root
temple. The Rooted's real chamber is at Olderdeep `4.6.2`, already converted;
the two must not be confused.

Sources: [generic underground pipeline](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs)
(around 451–474), [materials](../Assets/Scripts/Gameplay/World/Generation/Builders/SolidEarthBuilder.cs)
(`GetMaterialsForDepth`), [strata](../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs),
[underground landmarks](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs)
(around 162–230), [depth populations](../Assets/Scripts/Data/Tables/PopulationTable.cs)
(`UndergroundTier`, around 995), [continued stairs](../Assets/Scripts/Gameplay/World/Generation/Builders/StairsDownBuilder.cs),
[Root reservation](FELLING-W6-PLAN.md) (scope exclusion near line 25).

## Seed-generated camps, lairs and reusable landmark content

The authored map is not the complete set of native places. `WorldGenerator`
also attempts **3–5 lairs and 2–3 merchant camps** per seed, with spacing and
attempt limits; those are requested counts, not unconditional guarantees.
Neither is placed in Overwrit or on the Stump. Their exact addresses change
with the seed and must not be enumerated as fixed towns.

- **Lairs** use `LairBuilder`, `LairPopulationBuilder`, connectivity and a
  lair-specific container pass. They are a distinct layout/encounter family
  still needing deliberate voxel coverage.
- **Merchant camps** deliberately select the underlying legacy biome pipeline
  and add a guaranteed native camp stamp before ambient landmarks. Being on an
  address accepted by the voxel selector does not mean they received the new
  ordinary-biome layout or that every camp owner is modeled.
- **Ambient shrines, camps, ruins, farms and small structures** are stamps
  within chunks, not separate world IDs. They need owner/model coverage in
  every region where their stamp can appear, including when a model exists
  only in another region's library.
- **House drama** is an overlay in a village, not an additional settlement or
  pocket map. Its actors/items can expand a town's realized model inventory.

`SettlementManager.GetOrCreateSettlement` creates persistent condition/service
records for an existing settlement ID and POI. It does not found new villages,
allocate additional world coordinates or create independent town instances.

Sources: [opportunistic POIs](../Assets/Scripts/Gameplay/World/Generation/WorldGenerator.cs)
(around 106–155), [camp/lair routing](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs)
(around 179–205 and 976–988), [stamp families](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs),
[settlement lifecycle](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs)
(`GetOrCreateSettlement`, around 37).

## Pocket worlds, legacy biomes and lore-only places

This audit traced production `GetZone`, zone construction, connection targets,
stair builders and the water-passage path. It found no additional shipped
arbitrary pocket-world or authored dungeon-ID family beyond the `Overworld`
grid and the singular `WorldMap` navigation zone.

The existing **Helmwood water passage** is an optional shortcut between a
Drowned Sima's already existing surface and floor (`z=0 ↔ z=2`). It does not
allocate a secret third zone. At the authored Ginmere site those two
destinations are already in the converted set.

The base `ZoneManager` can generate a generic cave for arbitrary IDs, and
scenarios/tests exercise detached zones. This API capability does not prove
those IDs are player destinations. Saved custom connections or future code
can extend the graph, so this is an audit of current production producers,
not a theorem about every possible string accepted by the manager.

**Legacy Cave/Desert/Jungle/Ruins enum values:** none is placed as a standalone
surface biome by the current six-biome authored map. Their generators and
population/stamp families remain live as implementation components, special
routes and old-save/synthetic-map support. The Unsaying's retained Ruins-derived
circulation is one real use. There is no fresh-map separate “Ruins continent”
that should be counted as an undiscovered additional area.

`POIType.RiverChunk` has a native full-zone river pipeline, but current
`WorldGenerator.PlacePOIs` does not author any such POI. Treat it as a retained
route/demo/old-map compatibility surface unless another producer is added;
ordinary rivers and wet town chunks remain gameplay content in their own
existing zones.

The following are **new-content work**, not merely unconverted existing rooms:

| Lore ambition | Current source boundary |
| --- | --- |
| Unsaying town, threshold bleeds, held bleeds, underreading and deep-bleed fragments | Explicitly deferred by the Overwrit composition. No procedural bleed or sidegraph was shipped |
| First Root room and ending rituals | Reserved W8 capstone; current Root coordinates remain terrain/generic underground |
| Posy's central Resin-Cast Bower | Not an existing native god owner in the surveyed blueprint pack |
| Salt-Vault's conscious Salted, annual file review and full preservation complex | Generic village exists; the signature god/institution needs implementation |
| Quiet's Door leading to a distinct isolated Quiet settlement | Generic village exists, but no separate Quiet connection/zone producer found |
| Slip's east-quarter Hush mechanics | Generic village exists; no implemented special district/unsaying system |
| Lampwell's hatcheries/light market and Spivenor's Choir entry policy | Generic stranded settlements exist; those unique systems remain lore work |

Existing mechanics with similar words are not substitutes: the combat
`BleedingEffect`, an `UrquBleedLevel` field and FlowerCharm's duration consumer
do not constitute the Overwrit's authored bleed geography. A named item such as
FirstRootGlaive does not constitute a First Root god-room.

Sources: [Helmwood destination pairing](../Assets/Scripts/Gameplay/World/HelmwoodPassages.cs),
[base fallback generation](../Assets/Scripts/Gameplay/World/Map/ZoneManager.cs)
(around 101–105), [retained biome routes](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs)
(around 476–519 and 993–1000), [Overwrit deferrals](OVERWRIT-COMPOSITION.md)
(around 117–120), [W7/W8 milestone boundaries](FELLING-IMPLEMENTATION-PLAN.md)
(around 284–285), [canonical geography](../Lore/History/02_Geography.md)
(Posy 66, Salt-Vault 74, Quiet's Door 84, Root 90, Slip/Unsaying 96–98).

## Model coverage inside already converted areas

Area scope and model completeness are separate checklists. The coordinated art
audit found all **20 area kits (660 entries)** and **406 original catalogue
bindings** installed, but also concrete owner recipe gaps. Existing 2D sprite
fallback can still display an owner; a missing voxel recipe is not proof that
the entity or its gameplay mechanics disappeared.

The final VA05 same-seed destination audit confirms **nine genuine blueprint
gaps**, after correcting the pilot-catalogue omission in the earlier VA01
probe:

| Region / address | Owners without an applicable explicit recipe in that probe |
| --- | --- |
| Sodden `16.6.0` | Bandfrog, Greatdew, Reedfrog |
| Beating `8.18.0` | BrittleHound, GlassScorpion, Urn |
| Spread `8.8.0` | BerryBush, Magpie |
| Wellmeet `8.16.0` | MarketStall; suitable art already exists in Cinderhold's kit but needs correct regional binding/registration |

The actual cached graphs contain **28 distinct observed recipe-gap identities**,
unchanged between VA01 and VA05: BerryBush, Magpie, BogSap, CreatureCorpse,
FrostLichen, Greatdew, RawMeat, Bandfrog, Reedfrog, Viper, GinFrog,
StoneburrSeed, BloomingFruitingBody, SmithAnvil, Warhammer, SeveredLimb,
GlimmerBrine, LeatherCap, Mushroom, PetDog, Signpost, SteelBladeComponent,
BlastcapSpore, Buckler, DriedMeat, RiverShrine, Farmer and HollowStump.

Those cached graphs expose reusable-art scope gaps: GinFrog,
SmithAnvil, Signpost, DriedMeat and Farmer have assets in other region families.
Prioritize correctly sharing those bindings over fabricating duplicate art.
Additional cached examples include corpses/severed limbs, food and ingredients,
equipment on the ground, environmental growths and small fixtures. This is a
per-owner audit that spans multiple systems; it is not another named-town
conversion count.

The pilot's reported model misses in VA01 were **audit false positives**: that
probe omitted the multi-cell pilot catalogue, whereas the actual presenter
supplies it. Ten reported pilot identities have 51 installed pilot models.
Do not turn that probe artifact into 149 new missing-asset tasks.

A broader static scan of **93 unique literal `PopulationTable` blueprint rows**
found **55 candidate missing explicit world-model aliases**: 27 creatures, 21
equipment/weapon types and seven other objects. This is a source-bounded
candidate list, not a complete pack-wide census or proof that all rows spawn
in the current world. Conditional/private rows are included, and held
equipment uses a separate adapter.

- Creature candidates: Bandfrog, BrassHusk, BrittleHound, CanopyStrangler,
  CaveBat, CharredHusk, DesertBandit, DuneLurker, GiantSpider, GlassScorpion,
  GlowMoth, Glowmaw, JungleApe, Magpie, ObsidianBrute, PaleStalker,
  PalimpsestEcho, PetDog, Reedfrog, RuinScavenger, SandWurm, Scorpion,
  SkeletalSentry, SporeShambler, StoneGolem, SunStriker, Viper.
- Object candidates: Beehive, BerryBush, Greatdew, HollowStump,
  MemoryBathPool, MirrorMucilagePool, WaterTonic.
- Dropped-equipment candidates: Battleaxe, Buckler, ChainMail, Claymore,
  Cloak, Cudgel, Dagger, Greatsword, Hatchet, IronBuckler, IronHelmet,
  LeatherArmor, LeatherBoots, LeatherCap, LeatherGloves, LongSword, Mace,
  ShortSword, Spear, WardedCloak, Warhammer.

The equipment adapter has generic head/hand model routes, not complete
torso/feet/glove/cloak/back coverage. A weapon visible in an actor's hand can
still lack a dropped-world owner recipe. Conversely, an unmodeled worn slot
does not remove its inventory item or statistics. Future conversion acceptance
should exercise both states and the drop/pickup/equip transitions.

There is also a separate **actor animation and attachment** backlog. Some new
coarse regional NPC models are intentionally unrigged and have no equipment
sockets. `SpawnRing3DPresenter.RefreshEquipment` synchronizes equipment only
when the view has an Animator. Those owners can carry real native loadouts
without displaying them. Reusing the existing held/head equipment adapter
alone will not add animation or visible gear to an unrigged model; this needs
an explicit rig/socket or static-attachment design, with the existing pose
contracts preserved.

Sources: [final VA05 native audit](Verification/VoxelAccessibility/VA05-all-destinations/native-accessibility.json),
[original VA01 receipt](Verification/VoxelAccessibility/VA01-save-audit/native-accessibility.json),
[population sources](../Assets/Scripts/Data/Tables/PopulationTable.cs),
[world recipes](../Assets/Scripts/Presentation/Rendering/SpawnRing3DRecipes.cs),
[equipped-item presentation](../Assets/Scripts/Presentation/Rendering/Village3DEquipmentViews.cs),
[actor equipment gate](../Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs)
(`RefreshEquipment`, around 236–242), and the preserved
[coordinated model inventory](Verification/VoxelAccessibility/remaining-model-inventory.json).
No model fixes were implemented by the accessibility phase. These remain
conversion work; that phase repaired navigation guidance and corrected the
audit's pilot-catalogue coverage. Future model fixes should close the relevant
rows with their own native receipts.

## Recommended implementation order

1. **Finish owner coverage in the places already converted.** Close actual
   recipe gaps, share existing art where appropriate, and test static,
   travelling, dropped, destroyed and equipped states. This prevents the world
   looking unfinished even before adding another area.
2. **Convert the shallow underground family first, then extend the six depth
   bands.** One bounded sandstone pilot can establish real stairs, edge
   arrivals, destructible walls, liquids, keyed loot and creatures before
   applying the reusable system to all eligible underground columns.
3. **Sill plus the four small authored story/ruin locations.** Sill supplies a
   service-rich village already fully playable. Woven Doll, Tenth Fire and the
   two abandoned counters are small, distinctive conversions whose emptiness
   and native identities must remain intact.
4. **Lampwell and Spivenor as two contrasting three-level stacks.** Start with
   existing native hearth/threshold/plaque mechanics and deliberate visual
   identities. Keep any new market or reputation mechanics separately scoped.
5. **Convert Felling's existing authored smooth 3D art.** Preserve its scene
   definition, object owners, removal persistence and seventh-position design;
   this is an art/lifecycle port rather than a random layout replacement.
6. **Posy, Salt-Vault, Slip and Quiet's Door public districts.** Each can first
   become a distinct native voxel settlement without pretending its deferred
   god, Hush or hidden-network content has been completed. Keep unique lore
   work explicit in its own plan.
7. **Root and Unsaying authored story content.** These need a native design
   milestone before final art conversion. Do not fill reserved narrative
   locations with generic decorative substitutes to make a coverage number
   reach 100%.

Across those waves, treat seed-generated lairs/camps and ambient stamp owners
as shared integration work, not a finite location checklist. Recheck scope,
all realized owners and actual player entry after each region, with a
negative-control region. Preserve the one-cell/native multi-cell ownership
rules and the user's current coarse shapes/two-swatch art direction.

## Review and honesty bounds

- 🟡 Corrected false premise: the First Root god-room was not hiding under
  `3.3`; only the already converted Olderdeep chamber has the native Rooted.
- 🟡 Corrected false premise: converting the four named stacks does not cover
  their deeper descendants or the generic underground world.
- 🟡 Corrected false premise: all supported surface coordinates do not imply
  all seed-specific camp/lair layouts or all individual models are converted.
- 🔵 Preserved distinction: missing regional alias, missing source geometry,
  intentionally smooth existing scene and unimplemented lore are different
  tasks and should not be combined into one “missing assets” total.
- ⚪ No save migration, gameplay source, art, new map profile, Unity session or
  conversion was changed by this backlog task.
- 🧪 This inventory covers current production zone producers and checked-in
  authored coordinates. It does not exhaust arbitrary future IDs, external
  custom saves, all seed outcomes or every blueprint in the content pack.

Implementation log: 2026-09-16 — read current map, manager routes, all remaining
surface exceptions, native sinkhole/strata/landmark/connection producers,
settlement lifecycle and relevant lore/phase deferrals. Coordinated the
remaining owner-model inventory with the art-review agent. Wrote this backlog
only; no conversion tests or production implementation were requested here.

Final accessibility-phase verification is recorded in
[VA07 full-suite receipt](Verification/VoxelAccessibility/VA07-full/receipt.json):
14,576 total, 14,544 passed, the same 32 baseline failures, zero compiler
errors; all 28 new accessibility cases pass. This verifies that phase's
changes, not completion of the conversion backlog listed here.
