# COO Mutation System Implementation Plan (Derived from Qud Source)

## Scope
This plan is derived from decompiled Qud source (not from a local `MUTATIONS.md` file) and defines how to implement Qud-style mutations in Caves of Ooo with dependency order, parity targets, and concrete file-level work.

## Source Basis (Qud Decompiled)
Primary source files used:
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/BaseMutation.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Mutations.cs`
- `/Users/steven/qud-decompiled-project/XRL/MutationFactory.cs`
- `/Users/steven/qud-decompiled-project/XRL/MutationEntry.cs`
- `/Users/steven/qud-decompiled-project/XRL/MutationCategory.cs`
- `/Users/steven/qud-decompiled-project/XRL.UI/StatusScreen.cs`
- `/Users/steven/qud-decompiled-project/Qud.API/MutationsAPI.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/UnstableGenome.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/IrritableGenome.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/Chimera.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/Esper.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Body.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/MutationOnEquip.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/MutationPointsOnEat.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/RandomMutations.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Effects/MutationInfection.cs`
- `/Users/steven/qud-decompiled-project/XRL.World/GetMutationTermEvent.cs`
- `/Users/steven/qud-decompiled-project/XRL.World/SyncMutationLevelsEvent.cs`
- `/Users/steven/qud-decompiled-project/XRL.World/GetRandomBuyMutationCountEvent.cs`
- `/Users/steven/qud-decompiled-project/XRL.World/GetRandomBuyChimericBodyPartRollsEvent.cs`
- `/Users/steven/qud-decompiled-project/XRL.World/GameObject.cs` (MP semantics + mutation sync helpers)
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Leveler.cs` (rapid advancement hooks)

## 1. Mutation System Determination (What Qud Actually Does)

### 1.1 Data/Registry Layer
Qud uses XML-backed mutation metadata via `MutationFactory`:
- Categories (`MutationCategory`): name, display name, stat, category-level modifier property.
- Entries (`MutationEntry`): class, display name, cost, max level, defect flag, exclusions, variant metadata, category.
- Runtime pool queries are entry-driven, not class-driven.

Key behavior:
- Mutation category and entry metadata drive level scaling stat, max level, exclusion logic, mutation pool eligibility, and UI text.
- Mutation variants are first-class (`Variant`, selection UI, variant validation).

### 1.2 Runtime Layer
Qud runtime has two major components:
- `BaseMutation` (per-mutation part instance)
- `Mutations` (container/controller part)

`BaseMutation` responsibilities:
- Stores `BaseLevel`, computes effective `Level`.
- Lifecycle hooks: `Mutate`, `AfterMutate`, `Unmutate`, `AfterUnmutate`, `ChangeLevel`.
- Cap logic (`GetMutationCapForLevel(level) = level/2 + 1`, with `CapOverride`).
- `CanIncreaseLevel`: `CanLevel && BaseLevel < MaxLevel && Level < Cap`.
- Rapid levels via `RapidLevel_*` int properties.
- Variant support and display naming.

`Mutations` responsibilities:
- Owns `MutationList` and temporary/permanent `MutationMods` (`MutationModifierTracker`).
- Adds/removes mutations, levels mutations, handles sync and glimmer updates.
- Maintains pool logic (`GetMutatePool`) with exclusions/defect constraints.
- Applies/removes external mutation modifiers (equipment, cooking, tonic, etc.).

### 1.3 Effective Level Formula
`BaseMutation.CalcLevel()` is additive and ordered:
1. `BaseLevel`
2. Associated stat modifier (`MutationEntry.Stat` or category stat)
3. Legacy per-class/per-property int props
4. `AllMutationLevelModifier`
5. Category modifier (`<CategoryName>MutationLevelModifier`)
6. Adrenal bonus for physical (except AdrenalControl2)
7. Rapid levels (`RapidLevel_<MutationName>`)
8. `Mutations.MutationMods` bonuses by source
9. Clamp minimum to 1
10. Apply cap (`GetMutationCap`) with temporary bonuses reduced first
11. Add force bonuses (`CategoryForceProperty + ForceProperty`)

### 1.4 MP Economy and Spend Semantics
From `GameObject`:
- Gain MP: `MP.BaseValue += amount` (`GainMP`)
- Spend MP: `MP.Penalty += amount` (`UseMP`)

Gameplay implications:
- Rank-up in status screen spends 1 MP.
- Buying a new mutation costs 4 MP by default.
- MP is stat-based and participates in standard stat math.

### 1.5 Acquisition Flows
1. Direct add (`Mutations.AddMutation`): instantiate class, compatibility checks, lifecycle calls.
2. Buy new mutation (`MutationsAPI.BuyRandomMutation` + `StatusScreen.BuyRandomMutation`):
- Build mutate pool.
- Shuffle deterministically for run seed.
- Target choice count defaults to 3, overridable by `GetRandomBuyMutationCountEvent`.
- If pool is larger than target count, prefer entries with `Cost >= 2`.
- Chimera can inject extra limb outcomes per option via `GetRandomBuyChimericBodyPartRollsEvent`.
3. Random mutation (`MutationsAPI.RandomlyMutate`) used by systems like infection and irritable genome.
4. On-eat MP (`MutationPointsOnEat`) and infection (`MutationInfectionOnEat`).

### 1.6 Morphotypes / Constraints
- Esper: only mental mutation acquisition paths are valid.
- Chimera: only physical mutation acquisition paths are valid.
- Both are implemented as special mutation parts with `CanLevel=false` and extra behavior restrictions in selection APIs.

### 1.7 Special Mutation Behaviors (System-Level Dependencies)
- `IrritableGenome`: tracks MP spent (except BuyNew context), auto-randomly spends later gained MP.
- `UnstableGenome`: on level-up chance to manifest latent mutation; consumes its own base levels.
- Ranked duplicates (`IRankedMutation`) adjust rank instead of adding duplicate mutation parts.

### 1.8 Body/Equipment Integration
Full parity requires body-system integration:
- `BaseDefaultEquipmentMutation`: registers body slots, regenerates/decorates mutation equipment.
- `Body.UpdateBodyParts`: unmutates body/equipment-affecting mutations, rebuilds anatomy, remutates, re-equips.
- `MutationOnEquip`: applies mutation modifiers while equipment is active.

This is a hard dependency for true parity of limb/equipment mutations.

## 2. Gap Analysis vs Current Caves of Ooo
Current COO has:
- `BaseMutation`, `MutationsPart`, `ActivatedAbilitiesPart`, and a few concrete mutations.

Current missing parity-critical systems:
- Mutation metadata registry (entry/category/cost/exclusions/max-level/variants).
- Full effective-level pipeline and source-tracked mutation modifiers.
- Mutation sync event pipeline (`SyncMutationLevels` + recompute trigger points).
- Buy-random-mutation selection algorithm with weighted options and morphotype constraints.
- MP spend context logic needed for irritable-genome behavior.
- Ranked duplicate behavior (`IRankedMutation` equivalent).
- Body/anatomy mutation integration layer.
- Save/load strategy for mutation list + modifiers + rapid levels.

## 3. Implementation Architecture for COO

### 3.1 New Core Types
Add in `Assets/Scripts/Core/Mutations/`:
- `MutationDefinition.cs`
- `MutationCategoryDefinition.cs`
- `MutationRegistry.cs`
- `MutationModifierTracker.cs`
- `MutationSourceType.cs`
- `IRankedMutation.cs`

### 3.2 Extend Existing Core Types
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/BaseMutation.cs`
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/MutationsPart.cs`
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/Entity.cs`
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/GameEvent.cs`
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/Stat.cs`

### 3.3 New Events/Requests (COO equivalents)
Add event IDs/payload conventions for:
- `SyncMutationLevels`
- `GetMutationTerm`
- `GetRandomBuyMutationCount`
- `GetRandomBuyChimericBodyPartRolls`
- `GainedMP`
- `UsedMP` (with `Context`)

### 3.4 Data Source
Add mutation data file (JSON) under:
- `/Users/steven/caves-of-ooo/Assets/Resources/Blueprints/Mutations.json`

It must contain category and mutation-entry metadata, including:
- `Class`, `DisplayName`, `Cost`, `MaxLevel`, `Defect`, `Exclusions`, `Category`, `Stat`, `Variant` metadata.

## 4. Phased Implementation Plan

### Phase 0: Registry + Metadata
Deliverables:
- Mutation registry bootstrapped at game startup.
- Query APIs: by class, by category, full mutate pool source.

Exit criteria:
- Unit tests validate load and lookups for categories/entries/exclusions.

### Phase 1: BaseMutation Parity Core
Deliverables:
- `BaseMutation` gains:
- max-level lookup from registry
- cap logic (`level/2 + 1`, optional override)
- rapid level support
- full `CalcLevel` pipeline (initial subset + TODO gates for body-dependent modifiers)
- variant selection hooks (headless-safe)

Exit criteria:
- Deterministic tests for level math, cap behavior, and rapid levels.

### Phase 2: MutationsPart Parity Core
Deliverables:
- `MutationList` + `MutationMods` tracker list.
- `AddMutation`, `RemoveMutation`, `LevelMutation`, `AddMutationMod`, `RemoveMutationMod` parity semantics.
- Duplicate ranked-mutation behavior.
- `GetMutatePool` with exclusions/defect filters.

Exit criteria:
- Tests for add/remove/level/modifier interactions and pool filtering.

### Phase 3: Sync + Recompute Pipeline
Deliverables:
- `SyncMutationLevels` event implementation.
- Recompute on relevant stat/property changes.
- Mutation level transitions trigger `Mutate/Unmutate/ChangeLevel` correctly.

Exit criteria:
- Tests where stat changes dynamically shift effective level and lifecycle hooks fire exactly once.

### Phase 4: MP Economy + Mutation Spending
Deliverables:
- MP stat semantics parity (`BaseValue` gain, `Penalty` spend).
- `UseMP(context)` and `GainMP` events.
- Rank-up API (`SpendMPToIncreaseMutation`) using `CanIncreaseLevel`.

Exit criteria:
- Tests for MP gain/spend arithmetic and context-sensitive spend tracking.

### Phase 5: Buy-New-Mutation Flow
Deliverables:
- COO equivalent of buy-random flow:
- selection count event override
- cost>=2 weighted preference when trimming pool
- morphotype constraints and compatibility checks
- optional chimera extra-limb annotation hook

Exit criteria:
- Deterministic tests (seeded RNG) for selection count, weighting, and filtering.

### Phase 6: Special Mutations Framework
Deliverables:
- Implement framework support for:
- IrritableGenome-style delayed random MP spend
- UnstableGenome-style level-up trigger mutation grant
- Morphotype parts (Esper/Chimera) and constraint enforcement

Exit criteria:
- Tests for each behavior and interaction with MP contexts.

### Phase 7: Body/Equipment Dependency Track
Deliverables:
- Add body/anatomy integration seam to mutation system:
- mutation declares `AffectsBodyParts`/`GeneratesEquipment`
- mutation remutate cycle hook when body changes
- mutation equipment cleanup utilities

Notes:
- This phase can ship as interfaces + stubs if full body system is not yet implemented.
- Full parity for `MultipleArms`, `Wings`, `Stinger`, etc. requires body-part graph and default-equipment pipeline.

Exit criteria:
- If body system exists: integration tests covering rebuild/remutate/re-equip.
- If not: explicit TODO gates and no-op-safe behavior.

### Phase 8: Content Migration
Deliverables:
- Port starting mutation content from hardcoded strings to registry entries.
- Move existing COO mutations (`FlamingHandsMutation`, `TelepathyMutation`, `RegenerationMutation`) onto entry-driven metadata.

Exit criteria:
- Player startup mutations still work and read from registry-backed definitions.

### Phase 9: Persistence + Regression Suite
Deliverables:
- Persist mutation list, base levels, variants, rapid levels, mutation modifiers.
- Regression tests for save/load integrity.

Exit criteria:
- Round-trip save/load preserves effective levels and mutation behavior.

## 5. Dependency Order (Must Follow)
1. Registry/metadata
2. BaseMutation level math/cap/variant plumbing
3. MutationsPart container + modifier tracking
4. Sync pipeline
5. MP economy + spend contexts
6. Buy/random acquisition flows
7. Special mutations (Irritable/Unstable/morphotypes)
8. Body/equipment integration
9. Persistence

## 6. Definition of Done
Mutation system is considered parity-ready when:
- Effective mutation level matches Qud-style composition and cap rules.
- MP behavior matches Qud semantics and supports context-aware spending.
- New mutation purchasing follows pool/filter/selection rules.
- Morphotype restrictions are enforced consistently.
- Sync/recompute path is stable under stat and modifier changes.
- Body-affecting mutation hooks are implemented or explicitly gated behind body-system completion.

## 7. Immediate Build Slice (Recommended)
Implement first in this order for fastest progress without blocking on body graph:
1. Phases 0-4 (core mutation economy + progression)
2. Phase 5 (buy flow)
3. Phase 6 (special mutation logic)
4. Phase 7+ (body/equipment full parity)

This gives a functional mutation progression system compatible with XP/leveling work, while preserving a clear path to full anatomical mutation parity.
