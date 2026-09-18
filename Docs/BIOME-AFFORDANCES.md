# Truthful field and summit-water affordances

Status: implemented and verified by native EditMode mechanics, sprite, model
and presenter tests in CG17/CG18. Farm/stubble and summit-water visual feel
remain explicitly outside the live expedition capture.

## Scope and player outcome

An ordinary Spread field contains a few visibly ripe emberwheat rows among cut
stubble. Harvesting a ripe row yields one existing edible emberwheat sheaf once,
then leaves that same native owner as spent stubble. The small deterministic
budget is three ripe rows in tended fields, two in returning scrub, and one
gleaning in an after-harvest field. Other formations receive none. No theft rule,
crop regeneration, unattended growth or regional agricultural economy is implied.

Tank-brocchinia offers the existing water-drawing action, which removes Parched.
Walking through the plant does not coat the traveller. Spray pools are actual
freshwater pools: entering one applies native water contact and the current pool
owner projects the ground's water coating. Neither action invents an inventory
water supply or a new thirst meter.

Qud reference: none. This is CoO-original content integration, not a parity port.

## Verification sweep and corrections

| Initial assumption | Verified source and resulting decision |
|---|---|
| CropRow is part of the farming system | It has only terrain/render/examine behavior. Existing CropPart grows watered planted crops into loose produce; adding it to ripe rows would create a competing automatic yield path. Use a separate one-shot FieldHarvestPart. |
| A barley item can be reused | There is no barley produce blueprint. Emberwheat already has Food, Commerce and native planting/growth counterparts. Name ripe rows truthfully as emberwheat. |
| Existing Harvestable can retain a spent row | HarvestablePart consumes its owner. Leave that broad contract alone; FieldHarvestPart keeps its native owner and saves Harvested. |
| All field cells should give food | The census has 3,277 CropRows. Select only a bounded 3/2/1 ripe budget without changing the planned footprint or filling access lanes. |
| Water requires a new system | WellPart already draws/drinks water and removes Parched. LiquidPoolPart already applies contact; Zone.ProjectPool/UnprojectPool owns coating lifetime. Reuse both. |
| Drinking plants should be pools | A bromeliad's cup does not make the cell a puddle. Add Well only to the plant; LiquidPool water/40 only to SprayPool. |
| New state needs registration or migration | EntityFactory discovers concrete Parts from the gameplay assembly; save code reflects public fields. Pin normal ready/spent round trips. Historical cached entities remain unchanged; migrations are explicitly out of scope. |
| A full pack can silently lose the reward | Place the sheaf on the actual row cell when inventory cannot accept it; test the native dropped-item recipe too. |

Sources: Objects.json CropRow/Emberwheat/TankBrocchinia/SprayPool; HarvestablePart;
CropPart/CropSystemPart; SeedPart; WellPart; LiquidPoolPart; Zone pool projection;
EntityFactory; SaveSystem; SpreadCompositionBuilder; SpreadVoxelLibrary;
SpawnRing3DRecipes; EnvironmentSpriteRenderer.

## Implementation contract

- New `RipeCropRow` inherits the existing row; `FieldHarvestPart` carries public
  `Harvested`, `YieldBlueprint = "Emberwheat"` and `YieldCount = 1` state. Its
  world-action entry disappears after harvest. Action execution validates the
  live owner, actor, reach, zone and yield dependencies before spending anything.
- The native owner, ID, cell and footprint survive harvest. Update its name,
  glyph, examination text and per-cell render dirtiness. Replay cannot create a
  second sheaf. Failed dependency/placement paths do not consume the row.
- Existing ripe barley silhouettes are reused for standing grain. Four low
  two-swatch stubble meshes append to the Spread kit, bringing it from 16 to 20
  entries; the prior 16 meshes must remain unchanged. Spent selection reads native
  state, never a regenerated layout. Existing 16×16 crop sprites provide the
  standing-grain versus bare/cut-soil fallback cue.
- Objects.json changes use surgical string splices and a subsequent JSON parse.
  New metadata copies a shipped template and changes only its GUID.
- No camera, scene placement, shared quest logic, or automatic regrowth changes.

## Performance

Ripe selection runs once during generation. Action state changes mark only the
affected cell; no new per-turn or per-frame clock, scan, material allocation or
mesh generation is needed. Presentation borrows authored meshes and derives the
family from the current owner. The actual presenter test must verify retained
ownership and a visible mesh change after harvesting.

## Verification gates

1. Initial actual RED before implementation (root alone runs Unity with the established headless
   runner). Check compiler errors before trusting test results.
2. World-action positive and spent controls; one reward; full-pack ground spill;
   sparse deterministic generation; no grain in other formations; ready/spent
   save round trips; truthful names/examination.
3. Real movement into SprayPool applies water while stone/tank controls do not;
   removal retains a second live pool's projection and clears the last owner's;
   tank drinking restores Parched stat penalties through the existing effect.
4. Four coarse stubble variants, two colors maximum, one-cell bounds, portable
   overflow grain mapping, actual presenter transition and unchanged nearby row.
5. Dedicated adversarial cases for replay, foreign/missing actors and owners,
   factory/content failures, partial failure, multiple instances and saved state.
6. Cold-eye review plus native camera inspection. Headless assertions establish
   mechanics; they do not establish aesthetic readability or player enjoyment.

## Implementation log

- Read CLAUDE.md and the existing harvest, farming, pool, well, save and rendering
  seams. Recorded the corrections above before production.
- Authored `BiomeAffordanceTests` and `BiomeAffordanceRenderingTests`, with copied
  metadata. CG03 captured the absent FieldHarvestPart compile RED before production.
- Implemented FieldHarvestPart, surgical blueprint changes, bounded deterministic
  native row substitution, native-state voxel and sprite resolution, and four
  appended stubble models. The existing crop growth and generic consuming harvest
  systems are unchanged. Objects.json parsed successfully after editing.
- CG05 confirmed all core affordance logic green and two expected actual imported
  art failures. CG08 built the four appended stubble variants offline: root reports every prior16 model asset byte-identical; only the Library
  and new model artifacts changed.
- Added the dedicated 27-case adversarial fixture and actual presenter transition,
  removal, distant-patch, 2D fallback and strict model-ID controls. CG09 passed all
  affordance and art/presenter cases, zero C# errors. Its seven remaining failures
  belong to new expedition/cue guards, not this affordance slice.
- All four new production/test metadata GUIDs were checked against Assets with no
  collisions. No Unity was launched and no generated assets were manually edited
  by this agent.


## In-phase self-review

- 🟡 The generic harvest consumes its owner and loses yield when dependencies are
  absent; copying that flow would destroy the persistent row. Resolved with a
  distinct Part, live reach/ownership checks, and staging every yield on the real
  cell before changing inventory or spending state. Replay and dependency controls
  passed in CG09.
- 🟡 Matching RipeCropRow by blueprint alone would keep its tall ripe silhouette
  after harvesting. Resolved by reading its saved Harvested flag in both render
  paths; the actual presenter test verifies the changed mesh, retained native
  owner, subsequent ordinary removal and unchanged distant patch.
- 🔵 Generic model ID lookup silently treated unknown families as reeds. The
  appended family now uses explicit validation; counter-tests reject wrong families
  and out-of-range variants.
- ⚪ The pre-existing WellPart uses “well water” in its un-parched flavor message.
  The action and cure are correct for the bromeliad; a wording-only global change
  was kept outside this narrow content slice.
- 🧪 Static/headless gates cannot judge the artistic readability of very low cut
  stalks at the user's zoom. The new native journey harness exercises the actual
  expedition and quest cues, not a staged farm scene; no farm visual playtest is
  claimed. The root's final acceptance report owns that limitation.

## Changed files and executable handoff

- New `Assets/Scripts/Gameplay/Farming/FieldHarvestPart.cs` and metadata.
- Surgical `Assets/Resources/Content/Blueprints/Objects.json` crop/tank/pool hunks.
- `SpreadCompositionBuilder.cs`, `SpreadVoxelLibrary.cs`, `SpreadVoxelKitBuilder.cs`,
  crop-only `SpawnRing3DRecipes.cs` / `EnvironmentSpriteRenderer.cs` hunks.
- New `BiomeAffordanceTests.cs`, `BiomeAffordanceAdversarialTests.cs`,
  `BiomeAffordanceRenderingTests.cs` and metadata; updated existing
  `SpreadVoxelKitTests.cs` count/low-stubble contract.
- Root-only offline art entry: `CavesOfOoo.Editor.SpreadVoxelKitBuilder.Run`.
- Focused tests: `BiomeAffordanceTests;BiomeAffordanceAdversarialTests;BiomeAffordanceRenderingTests;SpreadVoxelKitTests;SpreadCompositionTests`.
- Unique native player-flow acceptance: `ChunkGameplayNativeAudit.cs`,
  `ChunkGameplayNativeAuditBatch.cs`, `Tools/ChunkGameplay/run_native.py`.
  The runner's default is a dry run. It requires an explicit `--execute`, captures
  the actual GameView, checks compiler errors before interpreting results, and
  archives one unique run plus its save/scene/view/input cleanup receipt. See
  [native acceptance bounds](CHUNK-GAMEPLAY-NATIVE-AUDIT.md).


### CG13 full-suite regression corrections

The full suite recorded four affected legacy contracts before changes: old spray
art-only assertions rejected the now-intentional LiquidPool and projected water;
the strict field realization check rejected the bounded RipeCropRow substitution;
and the global terrain-art audit found RipeCropRow had no ground fallback mapping.
The correction preserves meaningful controls: spray water is owned by its actual
pool with no competing TileStateSource, bromeliad cups offer drinking while keeping
their ground dry, unrelated removed brine cannot erase live spray, and exactly one
ripe-or-cut owner realizes each planned field row with the 3/2/1 budget. Both row
blueprints now explicitly retain Grass ground coverage while their earlier
state-aware crop sprite selection stays distinct. CropRow was removed from the
old glyph-only debt list, and real sprite-resource checks cover both row states.
CG13 was the actual pre-fix RED. CG14 passes all four affected contracts, two
new terrain/sprite controls and the complete 354-case focused set, zero C# errors.

### Final full-suite comparison

CG14 passed all 354 targeted cases. CG15 completed with 14,726 passed and
32 failed, zero C# errors. Its exact 32 failure names match the pre-change
CG01 baseline; no new failures remain. See the central implementation report
for rendered acceptance and its limits.

Final acceptance: CG17 356/356 focused, CG18 14,728 passed with the same 32
pre-existing failures, zero C# errors. CGN06 passed all 28 live checks and
60-second aggregate editor profiling; see the central implementation/native
reports for exact scope, visual evidence and performance bounds.
