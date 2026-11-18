# Spurgrass to serrated kit — source selection

2026-11-17. Bounded read-only source selection after accepted M3c (`c600701d0`). This records an existing source and consumer worth qualifying in ordinary play. It does not accept a new recipe, prove acquisition in the current save, or establish native execution. No Unity calls, runtime edits or Git mutations were performed for this selection.

## Proposed player choice

Consider **2 SpurgrassSpine + 1 KnotflaxCord → 1 SerratedEdgeComponent**. This recipe is **new design**, not shipped behavior. The existing spines remain recoverable throwing weapons; cord retains its existing binding, bandage, snare and repair uses. Processing would permanently consume both projectiles and the cord. A retained preview must disclose that loss, rather than imply that later weapon reclamation returns the original spines.

| Stock | Quantity | Total weight | Total base value |
|---|---:|---:|---:|
| SpurgrassSpine | 2 | 2 | 6 |
| KnotflaxCord | 1 | 1 | 3 |
| Proposed consumed inputs | — | **3** | **9** |
| Existing SerratedEdgeComponent output | 1 | **3** | **8** |

These are authored weight/value fields, not observed purchase or resale quotes. No mass gain or higher base resale value is proposed. Actual merchant prices, stock, custody and output admission remain qualification gates.

## Existing source, inheritance and consumer

- `Assets/Resources/Content/Blueprints/Objects.json`, `SpurgrassCrop` (line 52968 at inspection): manual mature harvest yields **2 SpurgrassSpine and 1 SpurgrassSeed**; 26 moist growth units per stage. `SpurgrassSpine` (line 53065) is a real `1d3`, `+1PV`, Strength-cap-2 piercing weapon, with `Handling` and ordinary recoverable throwing behavior. Each weighs 1 and has base value 3.
- The full inherited spine chain is **SpurgrassSpine → Item → PhysicalObject**. **Item supplies Stacker**, even though SpurgrassSpine does not declare it locally. The resolved spine has `Render`, `Physics`, `Examinable`, `Stacker`, `Handling`, `MeleeWeapon`, and `Commerce`; it has **no Material or Composition Part**. Inherited Stacker does not prove that the actual harvested owners merge or that the processing payload policy admits them.
- `Objects.json`, `SerratedEdgeComponent` (line 5486): exact description is “A set of close-set teeth and fastenings for dressing an edge. It is a finishing component, not a blade by itself.” Its full chain is **SerratedEdgeComponent → WeaponComponentItem → Item → PhysicalObject**. `WeaponComponentItem` (line 4655) supplies weight 3/value 8; Item supplies Stacker. The complete resolved kit has **no Material, Composition or metal tag**. Organic points held by lashing fit this material-unspecified type; this proposal does not turn plant matter into iron or infer recoverable stock from composition.
- The existing kit has `WeaponComponent.Slot=Binding`, `PenBonus=1`, and `OnHitEffectSpec=Bleeding,20,1d2,15,0`, with no HitBonus. `Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs` sums component penetration/hit at lines 378–379 and incorporates authored on-hit specifications at lines 404–408. Against existing GuardLashing, it trades **2 hit for 2 penetration and the authored bleeding effect**. It does not teach skills, guarantee bleeding against every target, or add a new enhancement framework.
- `Assets/Resources/Content/Data/Loot/LootTables.json` lists SerratedEdgeComponent in `WeaponsmithStock` (line 649, chance 40) and `ComponentAny` (line 1006). These establish possible existing finished alternatives, **not current merchant stock or an actual purchase quote**.

## Regional acquisition: not yet qualified

`Assets/Resources/Content/Data/Farming/BiomeCrops.json` (Spurgrass row at line 214) assigns the species to **Beating**. `Assets/Scripts/Gameplay/World/BiomeCropPlan.cs` allocates seed-ranked, eligible ordinary surface addresses; its read-only `SurfaceSites(manager)` result exposes `ZoneID`, `Biome`, `SpeciesIndex`, and `WorldSeed`. Spurgrass is Beating species index **4** in the current catalogue order.

The next permitted selection is a **bounded read of allocation metadata** for the existing character, filtered to Beating/Spurgrass, followed by ordinary travel and visible inspection at one candidate address. This is source-allocation guidance, not permission to scan hidden live owners, generate probe zones, teleport, delete caches, replace stock, grant inputs or merge branches.

`Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs` chooses reachable safe ground and, if placement succeeds, publishes two plants: one ripe and one seed/sprout. Exact local coordinates are placement-dependent. Allocation does not guarantee a surviving accessible crop in an already-cached save. No accepted ordinary Spurgrass acquisition fixture or verified Spurgrass address for **seed704860946** was established by this review. Existing biome-crop native census evidence covers sampled worlds, not this character's discovery or current stock.

Use the accepted M3c character for qualification. Establish an actual harvest and pickup, inspect the resulting owner/count/stack state, and compare the existing throw/recovery choice with keeping cord and obtaining a finished kit through ordinary trade if available. A long or unsuccessful search is not a reason to manufacture a convenient source.

## Work and admission gates before any implementation

1. Explicitly design the work: arranging rigid natural points and fastening them is not automatically authorized by a tool that only cuts flexible stock. Decide a justified existing capability or name the narrow new requirement; no implied metallurgy, heating or generic crafting permission.
2. Explicitly admit canonical `MeleeWeapon` and `Handling` payloads if the shared processing policy is to consume spines. Preserve refusal for unknown/modified state. Existing Stacker is neither a payload adapter nor authorization to add/retrofit Parts on saved owners.
3. Refuse equipped inputs, foreign custody and source/tool overlap. Verify actual merging and whether the proposed two-spine quantity resides in one eligible stack. If it does not, distinct-owner allocation is a separately reviewed contract, not a silent recipe convenience.
4. Retain exact ingredient owners/counts and tool/work-provider authority through callbacks; cancellation/refusal consumes nothing. Consuming a weapon-item cannot also leave it equipped or recoverable on the ground. Output admission and later refit retain existing rules.
5. Qualify the player benefit in actual play before locking quantities or writing RED tests. The proposed binding must be a meaningful alternative to GuardLashing and the raw thrown spines, not mandatory progression or another catalogue addition alone.

## Canonical fit and honest limit

`Lore/History/08_MaterialCulture.md` §VII, Tent-Right (line 156 onward), establishes portable woven work, layered wasteland dress, and the special protected meaning of guest cloth. It supports the regional lashing/workmanship context; it does **not** establish this recipe or authorize harvesting protected cloth. Spurgrass's specific biology and current utility come from the shipped crop catalogue and blueprint, not an invented historical claim.

Marlroot-to-FireClay and Sealbark-to-SalvagedTimber already ship and were excluded as new work. No new raw material source, ore stream, heat simulation, compulsory upkeep, capstan or culture system is proposed here. **Next gate is ordinary source and consumer qualification; no new recipe/runtime acceptance is claimed.**
