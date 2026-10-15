# Examine portrait coverage: bounded static audit

A static census of explicit identities in the current reusable recipe lists gives
the following counts. The census uses the closed `SpreadPortableRecipes.Concrete`
set and its declared portable delegates, the repair/cultivation switch, all three
blueprint columns of `BiomeCrops.json`, and the base SpawnRing catalog bindings.
It deduplicates blueprint names and compares them with `Objects.json`; it does
not count model variants, corpse-source families, repeated aliases or textures as
additional objects.

| Declared recipe group | Unique blueprint identities |
|---|---:|
| Base SpawnRing catalog bindings | 77 |
| Spread portable dispatch, including its portable delegates | 213 |
| Repair/cultivation world and portable identities | 18 |
| Biome crop rows: 35 crops, 35 seeds and 35 harvests | 105 |
| Union of those bounded groups | 411 |
| Union present in `Objects.json` | 407 |
| Carried-item dispatch alone, including repair supplies and crop produce/seeds | 291 |
| Carried-item identities present in `Objects.json` | 288 |

The four union identities outside `Objects.json` are `FellingSceneProp`,
`SeveredLimb`, `DetectiveNotebook` and `CrunchyLocket`; the latter three account
for the carried difference. These counts describe declared recipe identities,
not encounter availability, current visibility, successful asset loading or
guaranteed portraits. Other world recipe extensions are deliberately outside
this bounded census. Actual recipes also check ownership, state, appearance,
presentation context and imported assets.

The new carried route revalidates the document's current inventory/equipment
owner, obtains the viewer's current receiving-world context, and invokes shared
portable model selection without moving the item or adding a zone. Unsupported
recipes retain the current custom sprite where available, otherwise the current
CP437 glyph/color; unsupported Unicode uses the existing question-mark glyph.
Rejected ownership/visibility clears the old portrait instead of exposing it.
No new correctness or ownership defect was identified in this source review.

Remaining bounded coverage limitations: `SoddenFieldDressing` has a floor model
but no carried dispatch yet; `ToastedEmberwheat` has no portable mapping. Ordinary
carried goods currently receive models only in Spread/Sodden presentation
contexts, while repair/crop portable recipes support additional admitted zones.
Actor portraits clone the base model and do not include separately attached
equipment. These limitations should not be described as full current-world art
parity. The coordinating agent owns native validation of the expanded route.

## Source paths and extraction boundaries

- `Assets/Art3D/SpawnRing/Definitions/catalog.json`: unique `blueprints[].blueprint` (77).
- `Assets/Scripts/Presentation/Rendering/SpreadPortableRecipes.cs`: closed `Concrete` initializer (182), plus the three named special identities in `HandlesBlueprint`. The separate `CorpseFamilies` dictionary is excluded.
- Portable delegates: `Assets/Scripts/Presentation/Rendering/FiftySecondVisualAliases.cs` (`PortableModel`, 15 names); `EquipmentDiscoveryRecipes.cs` (`HeadForm` and two defensive items, 6 names); `ConnectedSpread3DLibrary.cs` (`IsPortable`, 2); `CurationYard3DLibrary.cs` (`IsPortable`, 5). These sum to 213 portable names before cross-group deduplication.
- `Assets/Scripts/Presentation/Rendering/RepairCultivationRecipes.cs`: `Handles` (18 names), `PortableModel` cases (9). FireClay overlaps the 213-name portable group.
- `Assets/Resources/Content/Data/Farming/BiomeCrops.json`: 35 `Species` rows; `CropBlueprint`, `SeedBlueprint`, `YieldBlueprint` columns (105 unique names), or only the last two for carried coverage (70).
- `Assets/Resources/Content/Blueprints/Objects.json`: the 712 `Objects[].Name` identities used for the authored intersection. This is not a census of dynamically constructed entities.
- Runtime scope/fallback review: `Assets/Scripts/Presentation/Rendering/ExaminePortraitRenderer.cs` (`GetPortrait`, `CurrentPresentationZone`, `PortableModel`, `Fallback`); current ownership contract: `Assets/Scripts/Gameplay/Entities/ExamineDocumentBuilder.cs` (`IsCurrentSubject`).

Counts were obtained by parsing the JSON and extracting string identities only
from the stated closed initializer/method boundaries, then unioning sets with
ordinal exact-name comparison. No Unity execution, asset import, encounter
simulation or production edits were performed for this static audit.
