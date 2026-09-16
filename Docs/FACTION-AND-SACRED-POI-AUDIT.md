# Faction settlements and non-settlement points of interest

Status: complete; native census and independent review verified, 2026-09-16. Scope: current working-tree production
world, separating authored sites, native generated landmarks, settlement chunks,
and lore-only destinations. No production gameplay changes are planned.

The census counts unique `Overworld.x.y.z` chunks. A site may span several chunks;
a chunk may contain several landmarks. These are separate counts. A biome's
association with a god does not by itself make every chunk a religious POI.
An explicit shrine, faction outpost, historic/mystery scene, or enacted conceptual
landmark qualifies. Generic terrain, creature encounters and resources do not.

The fixed-town audit found 17 surface villages plus three inhabited catacomb
floors. The expanded audit excludes these settlements and reports their
entrance/descent chunks separately. Small wilderness outposts are landmarks,
not an extra named town. Ordinary merchant-camp settlements are excluded.

## Method and verification

- Read map authorship, live pipeline routing, stamp catalogs and actual resident
  factions. Compare lore claims with generation rather than treating names as
  evidence of implemented institutions.
- Independently review fixed sites, biome concepts, and existing native receipts.
- Generate all 400 surface addresses and the six named sinkholes' below-ground
  levels with the previously copied save's seed, 141343545, in the isolated
  validation project. Record actual blueprint counts and placement diagnostics.
- Preserve the user's open editor and save. Audit tooling operates on detached
  worlds and never calls save.
- Audit result is a generation census, not a new movement/visual verification.
  Previous accessibility evidence remains in `Docs/VOXEL-WORLD-ACCESSIBILITY.md`.

Qud reference: not applicable; this inventories Caves of Ooo's own content.
TDD: not applicable to a source/data audit; no gameplay behavior changes.

## Corrections identified during the sweep

| Initial counting risk | Correct interpretation |
| --- | --- |
| Count map icons as all POIs | Many local faction/sacred landmarks have no map marker |
| Count a shrine, archive and outpost in one zone as three chunks | Deduplicate by full zone ID |
| Count sinkhole mouth, descent and village floor as three villages | One settlement site, three physical chunks |
| Call every faction-tagged settlement exclusively faction-owned | Primary faction and mixed residents are distinct |
| Treat a stamp's chance as an exact percentage of chunks | Tier gates, cap, catalog order, reservations and failed anchors affect realized placement |
| Treat all lore destinations as playable | Trace the production generator and native content |

## Implementation log

- Started read-only settlement census and independent review.
- Expanded scope at user request to non-settlement faction, sacred and conceptual POIs.
- Native census finished: 412/412 chunks generated, zero compiler errors, zero runtime/generation errors, zero dropped diagnostic records.

## Result: 124 qualifying chunks, plus three optional ecological chunks

Fresh native generation with the copied save's seed **141343545** contains
**124 distinct faction, sacred, mystery or enacted-cultural POI chunks**:
**120 surface chunks and four below ground**. This is 30% of the 400 surface
addresses, plus the Cathedral and Stillleaf's four below-ground levels.

The count includes **108 chunks with discrete authored sites, local landmark
stamps or a Bloom-front**, plus **16 additional chunks** whose Choir grove,
tendril fen or composting ground is itself a cultural destination. These
formations qualify through actual institutions/interactions: posted Choir law
and a seep; resident conversational/trading tendrils; deliberately arranged
remains and harvestable compost caches. Merely growing fungi does not qualify.

**Ginmere contributes three further chunks if ecological destinations count:**
**127 total** under that broader definition. Its drowned sima has a strong
identity but no specific faction, god or spirit affiliation. It is separated
rather than assigning an invented religious meaning.

This is a census of freshly generated chunks, **not an assertion that the
player has visited them or that every cached chunk still has these objects**.
Destruction, older generation and saved world state can change existing chunks.
No player save was loaded by this census. The seed came from the prior copied
save audit; the live save was not read or written for this pass.

### Fixed destinations

| Site | Zone addresses (`Overworld.` prefix) | Chunks | Association and current scope |
| --- | --- | ---: | --- |
| Deepest Cathedral | `5.4.0`, `5.4.1`, `5.4.2` | 3 | Rot Choir; nave, elders, tendrils and Choir node. Wedded's original body/audience is deferred. |
| Stillleaf | `2.4.0`, `2.4.1`, `2.4.2` | 3 | Recension history and forgotten pre-Felling archive. Sealed room exists; quest key/readable contents are deferred. |
| Felling-Site | `3.5.0` | 1 | The six Firsts and empty seventh; localized confusion/slippage exists. |
| Woven doll grove | `1.6.0` | 1 | Unexplained preservation within Choir country. |
| Tenth fire | `2.19.0` | 1 | An untended mystery; deliberately not assigned to a god or doctrine. |
| Abandoned Counter A | `19.18.0` | 1 | Saccharine Concord's withdrawn frontier. |
| Abandoned Counter B | `19.19.0` | 1 | Second withdrawn frontier position. |
| **Primary fixed total** | **Seven sites** | **11** | Includes four approach/descent chunks around the two central sanctums. |
| Ginmere, optional | `2.7.0`, `2.7.1`, `2.7.2` | 3 | Drowned-sima ecology; no specific faction/god claim. |

Counting only the central destination, rather than its approach levels, gives
seven fixed thematic destination chunks (eight with Ginmere). The table's
11/14 counts are physical chunks, not 11/14 independent temples.

Both abandoned counter addresses retain their authored route on ordinary fresh
maps because they lie within the existing Last Counter's POI exclusion radius.
Their stamps still need valid anchors; `Chance=100` means an attempt, not an
exhaustive all-seeds realization guarantee. Both actually placed in this census.

### Recurring local landmarks and cultural formations

Counts below are **chunks containing the feature**, after excluding villages,
merchant-camp chunks and all three catacomb settlement stacks. Rows overlap.

| Feature | Observed chunks | Affiliation / idea |
| --- | ---: | --- |
| Tent-Right wilderness camps | 30 | Guest-right, shelter, oath and hospitality |
| Folk river shrines | 30 | Local reverence and unnamed river belief |
| Concord waystations | 17 | Saccharine Concord's exchange and delivery network |
| Festival grounds | 16 | Communal celebration and temporary conjured flowers |
| Choir groves with seep and law | 13 | Rot Choir's grown public spaces; includes the fixed doll grove |
| Rot Choir congregation shrines | 9 | Five named Choir keepers gathered at a fire |
| Tendril fens | 7 | Resident Choir tendrils with conversation/trade |
| Composting grounds | 5 | Ordered remains, reclamation and harvestable caches |
| Snapjaw warband camps | 3 | Faction warband outposts |
| Rune Cult dig sites | 1 | Cult gathering and rune-laying encounter |
| Driving Bloom front | 1 | The driving/unfinished-work condition, growth and affected hosts |
| Recension ambient archives | 0 | Catalog exists, no successful occurrence in this surface census |
| Glassblown obelisks | 0 | Legacy Desert catalog; no fresh-map Desert route |
| Pale Curation galleries | 0 in this scope | Depth-six-plus generic-cave content; not sampled here |
| Overwrit pilgrimage furniture | 0 | Five eligible rim addresses; no bench/marker realized for this seed |

Small Tent-Right/warband camps and waystations are included as wilderness
outposts. They are not additional named villages. The three formal
`MerchantCamp` chunks are treated as temporary settlements and excluded as
whole chunks—even when an additional faction stamp appears inside them.

The **99 unique stamp/Bloom-front chunks** overlap the **11 fixed-site chunks**
in two places: Counter A also has a Tent-Right camp; Counter B has a Concord
waystation. Thus `99 + 11 - 2 = 108`. The 25 qualifying Choir formation chunks
share nine addresses with that set, adding 16: `108 + 25 - 9 = 124`.
One excluded merchant camp also has a Tent-Right stamp, and another has a
composting formation; counting their objects without the chunk-level exclusion
would overstate the answer.

The smaller preliminary count of 111 used discrete sites/stamps and included
Ginmere. Final review separated ecological Ginmere (-3) and added the 16
qualifying cultural-formation chunks: `111 - 3 + 16 = 124`.

### Breakdown by affiliation or theme

These counts deduplicate within each row. A chunk can belong to more than one
row; the final union is 124, not the sum of the table.

| Affiliation / theme | Chunks |
| --- | ---: |
| Saccharine Concord | 18 |
| Rot Choir | 29 |
| Tent-Right | 30 |
| Recension | 3 |
| Rune Cult | 1 |
| Snapjaws | 3 |
| Folk river reverence | 30 |
| Festival grounds | 16 |
| Driving Bloom | 1 |
| Felling / Urqu | 1 |
| Tenth Fire | 1 |

### What was deliberately not counted

- All 17 surface Village POIs; Olderdeep, Lampwell and Spivenor's whole stacks;
  and all three map-marked merchant camps. The three catacomb sites have six
  transit chunks at depths zero/one and three inhabited floors at depth two.
- Ordinary biome character: fungal walls, tree stands, peat faces, flower
  meadows, salt, bare Overwrit ground, generic ruins and petrified Stump terrain.
  A landscape's lore association alone is insufficient. A named shrine, social
  institution, mystery scene or enacted cultural destination is required.
- Ordinary hermit huts, generic tombs/vaults, resource caches, lairs, monsters,
  portable faction items and population-table appearances. A faction creature
  walking through a chunk is not a dedicated place.
- Settlement-contained institutions such as First Tent's monument, Drowned
  Ledger's excavation, Cinderhold's pruning post and Olderdeep's Rooted chamber.
- The Root's reserved coordinates as a completed First Root encounter; the
  Unsaying as a completed Hush town. The actual Rune Cult stamp that happened
  to appear at the Unsaying's legacy address is counted independently.

The ordinary Grovelands field of fruiting walls is not counted; the Bloom-front
is counted where it actually realized its special growth/hosts/workshop.
Pilgrimage benches/markers would count if present; their five eligible addresses
are not five realized POIs. Comparable Flower Meadow terrain remains distinct
from a stamped festival gathering with its sign.

## Gaps and uneven distribution

1. **Recurring faction landmarks concentrate in the Beating and Grovelands.**
   Tent-Right and Concord outposts are common; the Choir additionally has
   social/ecological institutions. These counts describe realized locations,
   not their encounter quality or visibility from the gameplay camera.
2. **Glassblown's landmark is effectively disconnected from the fresh map.**
   `GlassblownObelisk` belongs to `StampCatalog.Desert`; all 400 authored cells
   use the six Felling biomes, and Beating's catalog omits the obelisk.
   The old asset/catalog existing does not create a current-world destination.
3. **Rune Cult and ambient Recension archives have very limited surface reach.**
   They are inherited from the Ruins catalog. Ordinary composed Overwrit zones
   intentionally omit that catalog; the Unsaying reservation retains it. This
   seed produced one Rune Cult site there and zero ambient Recension archives.
   Stillleaf remains a separate fixed Recension-related location.
4. **Pale Curation's non-settlement gallery is a deep-cave feature.**
   Its minimum underground tier is three (`depth / 3 + 1`), so eligibility starts
   at depth six. This is a 25% attempt before caps and fitting; zero in the
   audited shallow scope is not zero across all possible caves.
5. **No general network of named spirit sanctums was found.**
   The named sanctum routing selects Cathedral/Stillleaf. Bloom and the Felling
   seventh provide special conceptual/Urqu content, but do not imply a shipped
   network of separate god/spirits' audiences. The Wedded body, First Root
   ending room and authored Unsaying remain incomplete.
6. **Map labels conceal most of this content.**
   Only three primary fixed thematic locations have formal map POIs (Cathedral,
   Stillleaf and Felling); Ginmere adds a fourth in the ecological category.
   Local faction outposts, shrines and mystery scenes are normally unmarked.

This is an audit, not a request to implement these gaps. No gameplay or world
placement changes were made.

## Previous request: settlement census

| Primary surface assignment / native underground community | Surface | Underground | Settlements |
| --- | ---: | ---: | --- |
| Saccharine Concord | 3 | 0 | Cinderhold, Tally, Last Counter |
| Recension (`Palimpsest`) | 2 | 0 | Quillhold, Drowned Ledger |
| Pale Curation | 2 | 0 | Marrowstye, Salt-Vault |
| Tent-Right | 2 | 0 | Wellmeet, First Tent |
| Bower-Folk | 1 | 0 | Posy |
| Stillcord | 1 | 0 | Morrowfast |
| Catacomb Folk / Rooted's people | 1 | 3 | Quiet's Door; Olderdeep, Lampwell, Spivenor |
| Mixed/generic `Villagers` | 5 | 0 | Sill, Gantry, Tine, Sumphold, Slip |
| **Total** | **17** | **3** | **20** |

This gives 12 specifically faction-assigned surface settlements and five
mixed/generic ones. The underground communities add three faction-associated
settlements, giving 15 specifically associated settlements across seven
factions. Sinkhole POIs have a null map faction: their classification comes
from native Catacomb Folk residents and institutions, not an invented POI tag.

Lore's Lampwell–Concord alliance and Spivenor–Choir integration do not change
those actual resident tags. The Deepest Cathedral is a Choir stronghold but
not a village. Glassblown, Imminent Archive and Rune Cult have zero fixed named
settlements in this map. Faction assignment is primary affiliation, not an
assertion of exclusive residency; lore explicitly favors mixed presences.
Thresk is not in the current authored settlement list.

## Evidence, limits and review

- [Full native census](Verification/FactionPOI/FP01-native-census/native-census.json):
  400 surface chunks plus 12 named sinkhole below-ground chunks; 412 successful
  native graphs, no error results, no dropped diagnostic records.
- [Classifier and counterchecks](Verification/FactionPOI/classify.py) and
  [deduplicated counts/address lists](Verification/FactionPOI/summary.json).
  Every counted stamp has its required living native blueprint owners verified;
  cultural formations need both the generation record and actual signature.
- [Execution receipt](Verification/FactionPOI/FP01-native-census/receipt.json):
  Unity exit zero, zero C# errors, zero captured runtime errors.
- [Source freeze](Verification/FactionPOI/FP01-native-census/source-freeze.json):
  1,161 script/content files matched the isolated project; no synchronization
  was necessary. [Reproduction harness](Verification/FactionPOI/FP01-native-census/FactionPoiCensus.cs.txt)
  lives outside the production Assets tree in this repository.

Can verify: current source routes, fixed-site count, actual content in these
fresh native graphs, classification and unique-address arithmetic.
Cannot verify from this census: live saved-world mutations, which landmarks the
player has noticed, visual readability, completed narrative arcs, or all-seed
placement guarantees. It is not a new traversal or full test-suite run.

Generic underground generation continues to deeper levels without a finite
content-depth boundary (`GetZoneBelow` increments depth). Therefore **124 is
not a finite total for all possible underground chunks**. It is the complete
surface plus named-shallow-sites census under the stated classification.

Review performed: independent fixed-site, formation and evidence-source audits;
source cross-checks; native owner verification; exclusion counterchecks for
Village, MerchantCamp and settlement sinkholes; duplicate-zone and overlap
checks. No gameplay code changed, so no feature RED/GREEN or adversarial Unity
suite is claimed. The prior suite results remain unchanged evidence.

Files changed in this audit: this document and `Docs/Verification/FactionPOI/`
audit artifacts only. Production code, graphics, camera and save remain untouched.

## Source references

- [Authored settlements](../Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs),
  [world POI placement](../Assets/Scripts/Gameplay/World/Generation/WorldGenerator.cs),
  [sinkhole sites](../Assets/Scripts/Gameplay/World/Map/SinkholeSites.cs).
- [Actual generation routes](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs),
  [landmark catalogs and fitting](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs).
- [Grovelands institutions](../Assets/Scripts/Gameplay/World/Generation/Builders/GrovelandsFormationBuilder.cs),
  [Bloom-front](../Assets/Scripts/Gameplay/World/Generation/Builders/BloomFrontBuilder.cs),
  [Overwrit furniture](../Assets/Scripts/Gameplay/World/Generation/OverwritCompositionPlan.cs).
- [Cathedral boundary](../Assets/Scripts/Gameplay/World/Generation/Builders/ChoirCathedralBuilder.cs),
  [sealed archive boundary](../Assets/Scripts/Gameplay/World/Generation/Builders/SealedLibraryBuilder.cs),
  [seventh position](../Assets/Scripts/Gameplay/World/SeventhPositionPart.cs).
- [Faction registry](../Assets/Resources/Content/Data/Factions.json),
  [lore geography](../Lore/History/02_Geography.md),
  [mystery constraints](../Lore/MYSTERY-LEDGER.md).

- Final independent review reconstructed the exact 124-address union from raw
  rows, verified native signatures and the census hash, and accepted the scope
  qualifications. No material counting or unsupported-claim findings remained.
- Removed the temporary audit entry point from the isolated Unity project;
  retained the reproduction source, raw census, source freeze and classifier.
