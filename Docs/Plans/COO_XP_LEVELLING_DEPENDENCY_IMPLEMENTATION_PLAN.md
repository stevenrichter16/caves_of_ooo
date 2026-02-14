# COO XP/Levelling + Dependency Implementation Plan

## Goal
Implement the **same XP/levelling behavior** as Qud (based on `XP_LEVELLING.md` and decompiled sources), and do it in dependency order so each prerequisite system exists before dependent features are added.

Primary source anchors:
- `/Users/steven/caves-of-ooo/XP_LEVELLING.md`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Experience.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts/Leveler.cs`
- `/Users/steven/qud-decompiled-project/XRL.World/GameObject.cs`
- `/Users/steven/qud-decompiled-project/XRL.World.Parts.Mutation/BaseMutation.cs`

---

## Part 1: XP/Levelling Parity Plan (System Target)

### 1. Parity Contract
1. XP threshold formula: `XP(L) = floor(L^3 * 15) + 100`.
2. Event flow parity: `AwardingXP -> AwardXP -> AwardedXP`.
3. Kill XP source and consumption parity:
   - Award from victim `XPValue`.
   - Respect `NoXP` on victim.
   - Consume/remove `XPValue` after award attempt.
4. Tier scaling parity:
   - attacker tier delta >2 => 0%
   - delta >1 => 10%
   - delta >0 => 50%
   - else 100%
5. Level-up reward parity:
   - HP gain each level
   - SP gain each level
   - MP gain for mutants only
   - AP on levels divisible by 3 and not 6
   - +1 all attributes on levels divisible by 6
   - rapid advancement at 5/15/25... for mutant non-esper.
6. MP semantics parity:
   - gain: `BaseValue += amount`
   - spend: `Penalty += amount`.
7. Mutation cap parity: `level / 2 + 1`.

### 2. Direct Implementation Targets (Caves of Ooo)
1. Add progression stats to creature/player blueprints.
2. Add XP award API and XP context event model.
3. Add `ExperiencePart` to process and scale XP.
4. Add `LevelerPart` to handle threshold crossing and rewards.
5. Wire kill XP into combat death handling.
6. Add MP utilities and mutation cap gating.
7. Add no-XP hooks and global XP multiplier.
8. Add tests for formulas, event flow, edge cases, and reward schedule.

---

## Part 2: Required Dependency Systems (What Must Exist First)

This section lists all systems required to implement XP/levelling correctly, in strict dependency order.

### Dependency A: Stat/Data Substrate
Required because XP/levelling is stat-driven and everything writes into stats.

Must exist:
1. Stable stats on actors:
   - `Level`, `XP`, `XPValue`, `SP`, `MP`, `AP`
   - core attributes used by rewards/modifiers: `Strength`, `Agility`, `Toughness`, `Intelligence`, `Willpower`, `Ego`.
2. Correct stat storage semantics:
   - earned totals go to `BaseValue`
   - spent pools go to `Penalty`.
3. Tag/property availability for gating:
   - `NoXP`, `NoXPGain`, `Mutant`, `Esper`, `Player`.
4. Blueprint-level support for defining all above.

Current status in repo:
- Partially present (`Stat`, blueprint stats pipeline are solid), but progression stats are not fully standardized across all creature definitions.

### Dependency B: XP Event Context + Cancellation Contract
Required because parity depends on pre-check veto and post-award level check.

Must exist:
1. Structured XP context object (fields analogous to `IXPEvent`):
   - `Actor`, `Kill`, `InfluencedBy`, `PassedUpFrom`, `PassedDownFrom`, `ZoneID`, `Amount`, `AmountBefore`, `Tier`, `Minimum`, `Maximum`, `Deed`, `TierScaling`.
2. Pre-award check event with cancel semantics.
3. Main award event.
4. Post-award event used by level-up system.

Current status in repo:
- Generic `GameEvent` exists, but no dedicated XP event contract yet.

### Dependency C: Kill Attribution + Death Order
Required because kill XP is emitted from death flow.

Must exist:
1. Reliable killer attribution in combat death path.
2. One award point in death sequence (single call).
3. Correct ordering:
   - death resolved
   - XP awarded to killer from victim
   - victim removed.

Current status in repo:
- Death path exists in `CombatSystem.HandleDeath`, but no XP award call yet.

### Dependency D: Deterministic Level-up Dice Engine
Required for HP/SP/MP reward parity and reproducible tests.

Must exist:
1. Parser for level-up strings:
   - flat (`"50"`)
   - range (`"1-4"`)
   - dice (`"1d4+1"`, `"2d3-1"`).
2. Deterministic RNG injection for tests.

Current status in repo:
- `DiceRoller` exists for combat dice, but level-up grammar compatibility should be isolated and explicitly tested.

### Dependency E: Experience Processor (Tier, Clamp, Multiplier)
Required before level-up can be trusted.

Must exist:
1. Tier scaling.
2. Clamp to min/max.
3. Global multiplier (`XPMul`) application.
4. `NoXPGain` handling.
5. Emits post-award event with final amount.

Current status in repo:
- Not implemented.

### Dependency F: Leveler Core
Required to convert XP into level and rewards.

Must exist:
1. `GetXPForLevel(level)` exact formula.
2. Multi-threshold while-loop for large awards.
3. Reward distribution:
   - HP/SP/MP/AP/all-attr/rapid advancement.
4. Level-up hooks for downstream systems.

Current status in repo:
- Not implemented.

### Dependency G: Mutation Progression Hooks
Required for MP and rapid advancement parity.

Must exist:
1. `GainMP` / `UseMP(context)`.
2. Mutation cap formula + `CanIncreaseLevel` gating.
3. Mutation rank-up API from MP spending.
4. Rapid advancement path (at least core mechanical version).

Current status in repo:
- Mutation system exists but lacks cap/rapid parity logic.

### Dependency H: Skill Economy Subsystem (for full parity)
Required if level-up SP rewards are expected to be spendable with parity behavior.

Must exist:
1. Skill entry data model (class, cost, requirements, exclusion, initiatory).
2. Skill acquisition + SP spend path.
3. Requirement evaluator.
4. Cost-0 inclusion behavior.
5. Optional NPC auto-spend logic.

Current status in repo:
- Not implemented (this is a major dependency for complete parity).

### Dependency I: Party XP Propagation (optional for initial ship, required for full parity)
Must exist:
1. Party leader graph / player-led flags.
2. Nearby companion query support.
3. Loop-prevention via `PassedUpFrom/PassedDownFrom`.

Current status in repo:
- Brain/faction AI exists, but formal party leadership model does not.

### Dependency J: External XP Source Systems (optional for core, required for full parity)
Must exist if parity includes all XP sources:
1. Quest system XP emitters.
2. Exploration/map-note XP emitters.
3. Sifrah/tinkering/social minigame XP emitters.
4. Wander WXU replacement system.

Current status in repo:
- Not implemented yet.

### Dependency K: Retroactive Stat Adjustments
Required for exact leveler parity.

Must exist:
1. Toughness change retroactive HP adjustment.
2. Intelligence base change retroactive SP adjustment + peak-int tracking.

Current status in repo:
- Not implemented.

### Dependency L: Persistence & Migration
Required to avoid corrupted progression after save/load.

Must exist:
1. Save/load persistence for new stats and properties (`PeakIntelligence`, rapid levels if implemented).
2. Migration defaults for old actors missing progression stats.

Current status in repo:
- Current project is test-first and runtime-driven; persistence requirements should still be defined before integration.

---

## Part 3: Dependency-First Implementation Roadmap

## Phase 0: Baseline Contract + Test Harness
Deliverables:
1. Add `ProgressionParityTests` skeleton with formula and fixture coverage.
2. Add deterministic RNG strategy for progression tests.

Files:
- `/Users/steven/caves-of-ooo/Assets/Tests/EditMode/ProgressionSystemTests.cs` (new)

Exit criteria:
1. Empty scaffolding tests compile.
2. Shared test builders for creatures with progression stats exist.

## Phase 1: Data Substrate
Deliverables:
1. Standardize progression stats in blueprints (`Level/XP/XPValue/SP/MP/AP`).
2. Ensure player and NPC creatures initialize valid defaults.

Files:
- `/Users/steven/caves-of-ooo/Assets/Resources/Blueprints/Objects.json`

Exit criteria:
1. Factory-created player and creature both carry required progression stats.
2. Existing tests continue passing.

## Phase 2: XP Context + API
Deliverables:
1. Introduce an internal `XPContext` model.
2. Implement `AwardXP(...)` and `AwardXPTo(...)` entry points.
3. Add event IDs and plumbing for pre/main/post XP pipeline.

Files:
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/ProgressionSystem.cs` (new)
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/GameEvent.cs` (event IDs/constants if desired)

Exit criteria:
1. Unit tests prove context creation, parameter pass-through, and cancellation behavior.

## Phase 3: ExperiencePart
Deliverables:
1. `ExperiencePart` handling:
   - NoXPGain block
   - tier scaling
   - clamp
   - multiplier
   - write XP to `BaseValue`
   - emit post-award.
2. Add `NoXPGainPart` parity behavior.

Files:
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/ExperiencePart.cs` (new)
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/NoXPGainPart.cs` (new)
- `/Users/steven/caves-of-ooo/Assets/Scripts/Rendering/GameBootstrap.cs` (if global multiplier state holder is bootstrapped)

Exit criteria:
1. Tier scaling test matrix passes.
2. NoXPGain veto tests pass.
3. Min/max clamp tests pass.

## Phase 4: Combat Integration
Deliverables:
1. Call `AwardXPTo(killer)` from death flow exactly once.
2. Consume victim `XPValue` after award attempt.

Files:
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/CombatSystem.cs`

Exit criteria:
1. Kill gives XP to killer.
2. Re-killing same corpse path cannot duplicate XP (consumption verified).

## Phase 5: Leveler Core
Deliverables:
1. `LevelerPart` with `GetXPForLevel` parity.
2. Multi-level jump loop.
3. Base rewards HP/SP/MP/AP/all-attr.
4. Rapid advancement gating hook (can be placeholder callback if mutation parity not fully done yet).

Files:
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/LevelerPart.cs` (new)
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/LevelUpRoller.cs` (new)

Exit criteria:
1. Threshold table tests pass.
2. Single huge XP award can level multiple times.
3. Reward schedule test for levels 2..30 passes.

## Phase 6: Mutation Dependency Completion
Deliverables:
1. `GainMP` / `UseMP(context)` helper API.
2. Mutation cap formula and `CanIncreaseLevel` enforcement.
3. Level-up rapid advancement applies 3 ranks when eligible.

Files:
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/Entity.cs` or `ProgressionSystem.cs`
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/BaseMutation.cs`
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/MutationsPart.cs`

Exit criteria:
1. MP spend/gain semantics match contract.
2. Mutation cannot exceed cap at low level.
3. Rapid advancement eligibility tests pass.

## Phase 7: Retroactive Adjustments
Deliverables:
1. Toughness retroactive HP adjustment.
2. Intelligence retroactive SP adjustment with peak-int property tracking.

Files:
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/LevelerPart.cs`
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/Entity.cs`

Exit criteria:
1. Stat-change regression tests pass for both retroactive rules.

## Phase 8: Skill Economy Dependency (Full parity gate)
Deliverables:
1. Skill entry model + data loader.
2. SP purchasing + requirements + exclusion.
3. Optional zero-cost included powers.

Files:
- `/Users/steven/caves-of-ooo/Assets/Scripts/Core/SkillsPart.cs` (new)
- `/Users/steven/caves-of-ooo/Assets/Scripts/Data/SkillData*` (new)
- `/Users/steven/caves-of-ooo/Assets/Resources/Skills/*.json` (new)

Exit criteria:
1. SP from level-up is spendable in-system.
2. Requirement and exclusion tests pass.

## Phase 9: Full-Parity Optional Extensions
Deliverables:
1. Party XP propagation.
2. Quest/exploration/sifrah XP sources.
3. Wander WXU mode.

Files:
- New systems as those domains are implemented.

Exit criteria:
1. Non-kill XP and party propagation tests pass.

---

## Part 4: Recommended Delivery Strategy

### Milestone A (Playable Core)
Ship after Phase 6:
1. Kill XP
2. Level-up progression
3. HP/SP/MP/AP reward cycle
4. Mutation cap + rapid advancement basics

### Milestone B (Progression Complete)
Ship after Phase 8:
1. Skill spending loop enabled
2. Full player progression economy

### Milestone C (Qud-Complete Progression)
Ship after Phase 9:
1. Party share + wander replacement + all major alternate XP sources

---

## Part 5: Risk and Mitigation

1. Risk: Progression stats absent on some blueprints.
   - Mitigation: Add factory-side validation + failing tests for required stats on all `Creature` descendants.
2. Risk: XP awarded multiple times for single death.
   - Mitigation: Consume `XPValue` on first award attempt and assert in tests.
3. Risk: Mutation progression diverges from level cap rules.
   - Mitigation: Centralize cap formula in one method and reference everywhere.
4. Risk: Future skill system redesign invalidates SP assumptions.
   - Mitigation: Keep SP accrual independent from skill UI/backend; expose stable spend API.

---

## Part 6: Definition of Done

For “XP/Levelling implemented with required dependencies”:
1. Phases 0 through 7 complete.
2. All new progression tests pass.
3. Existing test suite remains green.
4. Player can kill, gain XP, level up, receive rewards, spend MP, and be cap-limited on mutation rank.

For “exact same full system parity”:
1. Phases 0 through 9 complete, including skill and alternate XP-source dependencies.
