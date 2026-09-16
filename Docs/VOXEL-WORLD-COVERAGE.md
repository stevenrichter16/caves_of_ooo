# Native voxel world coverage and discovery

Status: source inventory and isolated saved-world access audit verified on
2026-09-16. All 28 new audit/help tests pass in VA03. Its seven legacy
minimal-blueprint fixture failures reproduce unchanged when run alone in
VA06. VA07 full suite: 14544 passes, 32 unchanged baseline failures, no new failures or C# errors.
This document does not claim an interactive visit to every location.

The completed compositions are installed in normal world generation, not only
in screenshot scenarios. The main discovery problem found in this audit is that
help describes `< >` only as stairs, although the same bindings open the world
map and enter its selected ground chunk. The current scene starts in a forest
chunk immediately west of Morrowfast.

## What the inventory counts

- **Four original native voxel chunks:** a presentation conversion over existing
  owners and authored layouts. These are not four new procedural towns.
- **Twenty-one composition systems:** six ordinary surface-biome systems,
  eleven named surface settlement systems, and four named three-level stacks.
  A system is not a single chunk: the biome systems cover many eligible map
  cells, while each stack covers a mouth, descent and floor.
- **Twenty area voxel libraries plus the original `VoxelWorld/Library` mesh
  catalogue:** all corresponding Resources assets exist. Grovelands uses the
  original mesh catalogue rather than a twenty-first dedicated area library.
  Existence alone is not proof that every runtime recipe resolves; per-area
  tests and native render receipts establish their separately documented gates.

The ordinary-biome and original-four sets overlap. Do not add the counts below
as though they describe disjoint content. Native actors, collision, inventory,
destruction and service Parts remain gameplay authority; a visual voxel is not
an independently mineable simulation cell.

## Original four-chunk integration

All addresses in this document use `Overworld.X.Y.Z`; omitted `Z` means zero.
World X increases east and world Y increases south.

| Native chunk | Address | Installed content | Reach from current fresh spawn |
| --- | --- | --- | --- |
| Western Grovelands field | `2.6.0` | Original voxel presentation; now also ordinary Grovelands composition | The spawn itself |
| Morrowfast | `3.6.0` | Bespoke native Morrowfast village and voxel presentation | One chunk east |
| Southern Stump root spur | `3.7.0` | Native multi-cell pilot and its voxel owners | One east, one south |
| Southeastern grove | `4.7.0` | Original voxel presentation; now also ordinary Grovelands composition | Two east, one south |

The reusable Blender oasis demo and the four exported `candidateOnly:true`
layouts remain authoring outputs. The game borrows compatible meshes and
recipes; it does not replace Morrowfast's canonical owners with the complete
demo town. See [original integration](VOXEL-WORLD-INTEGRATION.md) and
[toolkit](VOXEL-TOWN-GENERATOR.md).

## Six ordinary biome systems

The counts were independently recomputed from the current 20×20 authored biome
table, named places, sinkhole mouths and each plan's explicit exclusions. They
are **static eligible address counts**, not a claim that every eligible address
uses composition in every world seed. A runtime lair, merchant camp or other
POI wins its specialized pipeline before the ordinary-biome branch. None of
these systems replaces a named settlement merely because it shares a biome.

| System | Static eligible surface cells | Important exclusions | Main source |
| --- | ---: | --- | --- |
| Grovelands | 37 | Named places, sinkhole mouths, Woven Doll `1.6` | [GrovelandsCompositionPlan](../Assets/Scripts/Gameplay/World/Generation/GrovelandsCompositionPlan.cs) |
| Spread | 132 | Named places and sinkhole mouths | [SpreadCompositionPlan](../Assets/Scripts/Gameplay/World/Generation/SpreadCompositionPlan.cs) |
| Sodden | 60 | Named places and sinkhole mouths | [SoddenCompositionPlan](../Assets/Scripts/Gameplay/World/Generation/SoddenCompositionPlan.cs) |
| Beating | 100 | Named places, Tenth Fire `2.19`, abandoned counters `19.18` and `19.19` | [BeatingCompositionPlan](../Assets/Scripts/Gameplay/World/Generation/BeatingCompositionPlan.cs) |
| Stump | 18 | Stillleaf mouth, Root `3.3`, Felling-Site `3.5`, multi-cell spur `3.7` | [StumpCompositionPlan](../Assets/Scripts/Gameplay/World/Generation/StumpCompositionPlan.cs) |
| Overwrit | 22 | Explicit finite whitelist; Unsaying `2.11` remains excluded | [OverwritCompositionPlan](../Assets/Scripts/Gameplay/World/Generation/OverwritCompositionPlan.cs) |

For orientation, the Stump lies northwest, Grovelands around it, Sodden
northeast, Spread through the central band, Beating across the south and
Overwrit along the western scrape. The exact cell is authoritative, not a
distance-from-spawn rule. The world seed varies local layouts and opportunistic
POIs; it does not move the authored named towns.

## Eleven named surface settlement compositions

Every row has a matching authored `Village` POI, an exact address/profile gate
in `CreateVillagePipeline`, a base builder, a late profile builder and arrival
reservations. Native population, trading and applicable settlement services
run after composition. None requires an Editor scenario to exist.

| Place | Address | Current biome | Required profile | Composition identity |
| --- | --- | --- | --- | --- |
| Cinderhold | `6.6.0` | Grovelands | `PruningPost` | Concord workyard, real forge/anvil and pruning contract |
| Sumphold | `15.6.0` | Spread | `Boatyard` | Wet boatyard and native settlement services; no invented boat-driving mechanic |
| Drowned Ledger | `17.5.0` | Sodden | `ExcavationCamp` | Three native witnesses, excavation bays and courier origin |
| Marrowstye | `12.12.0` | Spread | `Intake` | Intake aisles, native coffers and courier destination |
| Wellmeet | `8.16.0` | Beating | `TentCamp` | Tent-Right gathering and repairable native services |
| First Tent | `5.17.0` | Beating | `TentCampFirst` | Open oath court, five cloth poles, hosts and salt service |
| Last Counter | `18.18.0` | Beating | `ConcordPost` | Sparse retreat post, notice and useful native stores |
| Gantry | `7.8.0` | Spread | `CrossroadsExchange` | Crossroads exchange and public services |
| Tine | `13.7.0` | Spread | `LakesideVillage` | Village arranged around a native lake and shoreline activity |
| Quillhold | `14.9.0` | Spread | `PrimaryArchive` | Public stacks, copy hall, refectory and native Scribe copying |
| Tally | `10.14.0` | Spread | `CentralExchange` | Modular exchange, service counters and goods circulation |

These are native public districts. A familiar lore name does not imply that
every future god encounter, faction quest or world-scale institution has been
implemented. Their individual composition documents record those limits.

## Four named vertical compositions

The manager checks these sinkhole identities **before** the generic underground
branch, so their lower levels are actual native destinations. A mouth leads
through native stairs and connector builders to the descent and floor. Surface
map descent enters depth zero; use the mouth's stairs to reach lower levels.

| Area | Exact supported addresses | Authority and native floor | Reach from `2.6.0` to mouth |
| --- | --- | --- | --- |
| Ginmere | `2.7.0`, `2.7.1`, `2.7.2` | Named `Ginmere`, `DrownedSima` archetype | One south |
| Deepest Cathedral | `5.4.0`, `5.4.1`, `5.4.2` | Named Cathedral, `ChoirCathedral`; native nave, elders and tendril | Three east, two north |
| Stillleaf | `2.4.0`, `2.4.1`, `2.4.2` | `SealedLibrary` profile/archetype and native sealed archive | Two north |
| Olderdeep | `4.6.0`, `4.6.1`, `4.6.2` | `FoundingVillage` profile, `StrandedSettlement`; retained Rooted/plume stamp | Two east |

Depth three and beyond retain existing underground generation. **Reachable
area does not mean every room is unlocked:** Stillleaf's exterior, descent and
sealed-door approach are reachable, but its archive key/readable contents
remain part of a deferred quest arc. Composition does not remove that seal or
claim the interior can already be unlocked through normal progression.

Undiscovered sinkhole mouths intentionally appear as their ordinary biome on
the world map. Village `!` markers do not have this hiding rule. Enter a mouth's
chunk and return to the world map to reveal its named marker.

## What remains outside this conversion inventory

| Existing native place/content | Address | Current state |
| --- | --- | --- |
| Sill | `10.10.0` | Native large starting-town pipeline and earlier 3D work; not one of the 21 compositions or four voxel-catalogue scopes |
| Posy | `5.9.0` | Generic native village; no new composed voxel district |
| Salt-Vault | `15.15.0` | Generic native village; no new composed voxel district |
| Slip | `16.11.0` | Generic native village; no new composed voxel district |
| Quiet's Door | `16.1.0` | Generic native village; no new composed voxel district |
| Lampwell | `12.3.0–2` | Existing stranded-settlement sinkhole pipeline; not a new composition |
| Spivenor | `16.4.0–2` | Existing stranded-settlement sinkhole pipeline; not a new composition |
| Unsaying | `2.11.0` | Excluded ordinary Overwrit legacy pipeline; no authored town composition |
| Root, Felling-Site, Woven Doll, Tenth Fire, abandoned counters | `3.3`, `3.5`, `1.6`, `2.19`, `19.18`, `19.19` | Existing specialized/native content is preserved; do not count it as newly converted ordinary biome content |

“Outside this inventory” does not mean absent from the game. Nor does existing
3D art automatically establish the new voxel-composition treatment. The native
Felling scene and Morrowfast bespoke scene are examples of earlier work whose
ownership must not be replaced by a generic biome generator.

## How to find the content in the actual game

The build scene is `Assets/Scenes/Main/SampleScene.unity`. Its serialized
`FreshGameZoneID` is **`Overworld.2.6.0`**. The `GameBootstrap` field default
still names Morrowfast, and the historical `WorldMap.StartingZoneID` constant
still identifies Sill for village systems; neither overrides that scene field.
Continuing a save uses its saved active zone instead of the fresh-game choice.

1. From a surface chunk, press **Shift+comma (`<`)** to open the world map when
   no applicable upstairs path takes precedence.
2. Move around the map using the normal movement keys. **`!` marks a village**;
   its native display/examine name identifies the destination.
3. Press **Shift+period (`>`)** to enter the currently selected map chunk.
4. Alternatively, walk over a chunk boundary. East increments world X, south
   increments world Y; the transition system generates the neighboring chunk
   and searches its arrival edge for a valid position.

The nearest clear town demonstration is **Morrowfast, one chunk east of the
fresh spawn**. Cinderhold is four chunks east. This is not a requirement to
walk every intervening 80×25 ground cell: world-map traversal already exists.

For the actual saved position audited below, the nearest town is instead
**Sumphold, one chunk west from `16.6.0` to `15.6.0`**. The fresh-start location
does not move a continued character.

The pre-correction help text says only “take stairs up / down.” Its boot text
also directs the player to villagers without explaining how to find those
villages. `NavigationDiscoveryTests` specifies the missing surface/map,
destination, marker and physical-key guidance for both help surfaces, while
retaining null-output safety. Existing onboarding tests keep the boot summary
to four lines and preserve its F1/quest guidance.

## Actual saved-world access audit

[Final VA05 native accessibility receipt](Verification/VoxelAccessibility/VA05-all-destinations/native-accessibility.json)
inspected an isolated copy of the current save; the original save was left
untouched. Its data was independently read back for this document:

| Observation | Verified result |
| --- | --- |
| Saved world seed | `141343545` |
| Saved active chunk | `Overworld.16.6.0` |
| Cached graphs before the audit | 25: 24 surface ground chunks and the world map |
| Cached named villages before the audit | **Zero** |
| Authored named villages present in the saved map | **17**, with the expected current profiles |
| Saved-map town generation compared with a fresh world using the same seed | **17/17 identical blueprint censuses**, no generation errors |
| Native world-map descents into the named villages | **17/17 successful** |
| Additional ground-edge transfers | `16.6.0 → 15.6.0` west to Sumphold and `2.6.0 → 3.6.0` east to Morrowfast, both successful |

The saved-cache explanation is **disproved for the reported missing towns**:
none was cached yet, and all seventeen generated identically through the
saved-map and fresh-map managers. The general persistence caveat below remains
true for older visited content, but is not the cause established by this save.

The final expanded census inspects 37 saved/fresh destinations and successfully
transfers into all 29 surface destinations through the map, plus the two edge
transfers: **31/31**. Separate tests physically walk through the world map to all
eleven recreated settlements, walk across the two source chunk edges, and
walk down/up both stair connections in all four recreated stacks.

The batch observations are native programmatic generation/transfer checks, not
manual keyboard journeys. They establish the actual destination graph and
successful transfer, without proving every road, encounter or building
interior is traversable in every seed.

The final receipt confirms nine blueprint identities still lack applicable
voxel recipes: BerryBush, Magpie, Bandfrog, Greatdew, Reedfrog, BrittleHound,
GlassScorpion, Urn and Wellmeet’s MarketStall. The pilot’s apparent missing
models in VA01 were a measurement error: passing its required catalogue in
VA04/VA05 eliminates all of them. See the [conversion backlog](VOXEL-CONVERSION-BACKLOG.md). Successful destination entry and existing library assets must
not be rewritten as “every owner everywhere already has a voxel model.”

## Saves, presentation and scope boundaries

- Loading rehydrates matching named village profiles from the current authored
  table; a pre-quartet saved map does not automatically strand unvisited Gantry
  or Quillhold in a null-profile pipeline.
- **Previously generated saved chunks keep their owners/layouts.**
  `ZoneManager.GetZone` returns a cached graph before invoking generation. An
  old visited generic town does not spontaneously rebuild into the new plan.
  This is persistence, not a missing dispatch; this audit adds no migration.
  VA01 found no cached towns in this user's save, so this caveat does not
  explain the reported absence of towns there.
- Managed zone presentation checks the current map's real POI/profile. A
  synthetic preview at a supported address is not proof that a different
  runtime POI at that address is eligible.
- `Village3DSettings.Enabled` gates the presentation and is persisted. The
  default is enabled, but a saved display preference can disable it without
  deleting native town entities.
- All 20 area library assets and the original catalogue were found at their
  declared Resources paths. An independent asset audit resolved all 660 area entries and 406 original
  conversion bindings to unique on-disk GUID dependencies. Persisted display
  preferences are enabled; this does not establish the active Editor’s current
  compiled assembly or live frame. VA05 separately inspected an isolated copy of the player's saved graph, not the
  active Editor's in-memory state.

## Verification evidence and boundaries

| Claim | Source |
| --- | --- |
| Fixed 20×20 geography, named places protected before random POIs | [WorldGenerator](../Assets/Scripts/Gameplay/World/Generation/WorldGenerator.cs), [WorldMapAuthoring](../Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs) |
| All 21 systems dispatch in normal generation | [OverworldZoneManager](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs): `GetPipelineForZone`, `CreateVillagePipeline`, named stack pipelines |
| Finite presentation scope and real map authority | [VoxelWorldPresentation](../Assets/Scripts/Presentation/Rendering/VoxelWorldPresentation.cs), [AreaCompositionScope](../Assets/Scripts/Gameplay/World/Generation/AreaCompositionScope.cs) |
| Native surface/underground entry routes | [WorldMapTraversal](../Assets/Scripts/Gameplay/World/Map/WorldMapTraversal.cs), [ZoneTransitionSystem](../Assets/Scripts/Gameplay/World/ZoneTransitionSystem.cs), [InputHandler](../Assets/Scripts/Presentation/Input/InputHandler.cs) |
| Village markers and intentionally hidden sinkholes | [WorldMapZoneBuilder](../Assets/Scripts/Gameplay/World/Generation/Builders/WorldMapZoneBuilder.cs) |
| Profile repair does not regenerate saved zone graphs | [SaveSystem](../Assets/Scripts/Gameplay/Save/SaveSystem.cs), [WorldMap](../Assets/Scripts/Gameplay/World/Map/WorldMap.cs), [ZoneManager](../Assets/Scripts/Gameplay/World/Map/ZoneManager.cs) |
| Stillleaf's intentional sealed-room limit | [SealedLibraryBuilder](../Assets/Scripts/Gameplay/World/Generation/Builders/SealedLibraryBuilder.cs) |

Can verify here: exact authored scope, matching runtime dispatch, checked-in
asset existence, actual input paths, the help omission and VA01's saved-world
censuses/transfers. Cannot verify: the active Editor's in-memory graph/refresh
state, every procedural seed's traversability, active rendered appearance,
performance or feel. Broader parent-run coverage gates and their receipts
should be appended before claiming comprehensive playable-world acceptance.

This is CoO-original integration. It makes no claim of Qud implementation
parity. No town was moved and the original save/Unity session was not modified by
the audit. The only production change in this phase is clearer controls and
boot guidance; generation, presentation and travel mechanics remain as audited.

## Implementation log

- 2026-09-16: inventoried all 21 composition plans and compared authored place
  profiles, sinkhole identities, manager dispatch and voxel scope. No missing
  converted-town dispatch or mismatched authored profile found. Counted 20
  installed area libraries plus the original mesh catalogue.
- 2026-09-16: verified the actual `2.6.0` scene spawn, nearby Morrowfast and
  native world-map entry controls. Found the navigation instruction omission
  and authored `NavigationDiscoveryTests` before any help-text correction.
- 2026-09-16: independently read VA01's actual saved-world receipt: 17 named
  villages generated, their blueprint censuses matched the fresh same-seed
  controls, and 17 map descents plus two edge transfers succeeded. No town
  existed in the original 25 cached graphs. The source-level stale-cache
  hypothesis therefore does not explain this user's missing-town report.
- 2026-09-16: VA02 actual RED: 28 total, 20 passing, eight failing, zero
  compiler errors. All failures are the intended navigation-help omissions;
  null-sink controls pass. The later VA03/VA07 runs verify the correction and final fixture cleanup.

- 2026-09-16: VA03 confirms all 28 new cases green. Seven pre-existing
  WorldMapTraversal tests fail when selected alone or with this subset because
  their minimal factory omits container blueprints; VA06 reproduces all seven
  without running any new tests. No log suppression or production workaround.
- 2026-09-16: VA04 corrects the audit's missing pilot catalogue; VA05 extends
  native map transfers from 17 towns to all 29 selected surface destinations.
  All 31 map/edge transfers succeed; all 37 saved/fresh graphs generate.
- 2026-09-16: independent review added exact loot-registry restoration to the
  test fixture and confirmed the help bindings. Broader legacy Shift+comma
  pickup-before-ascent behavior is recorded as navigation debt, not changed here.
- Interactive [saved-world guide](Verification/VoxelAccessibility/world-map.html)
  shows all 400 coordinates, 17 towns, six sinkhole mouths, the 24 cached ground
  chunks and the saved player location. Browser inspection verified rendering
  and selecting Sumphold. It is a static save snapshot, not a live tracker.

- Final VA07: all 28 new tests pass; 14544 passing overall, 32 identical baseline failures, zero compiler errors. See [closeout](Verification/VoxelAccessibility/VA08-closeout/README.md).
