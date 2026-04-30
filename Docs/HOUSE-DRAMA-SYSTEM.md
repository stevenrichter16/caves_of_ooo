# House Drama System — Implementation & Analysis

## Overview

The House Drama system is a data-driven procedural storytelling engine that models internal family conflicts as interactive moral narratives. Each drama consists of four pressure points (dramatic nodes) with multiple resolution paths, a witness knowledge graph, a corruption score, state propagation (crossover edges), and five possible narrative outcomes. Dramas are procedurally seeded into villages at world generation and resolved through player dialogue choices.

**Status:** Foundation complete (M1). Core features implemented, tested, and fixed. Save/load and urgency timers not yet implemented.

---

## Goals & Motivation

### Design Goals

1. **Make families non-interchangeable.** Rather than a generic "NPC family," each house has a *crisis* — a specific, authored conflict that the player can engage with or ignore.

2. **Create reactive narratives.** A drama progresses through player dialogue choices, not author-scripted cutscenes. The state machine evaluates choices and fires crossover effects.

3. **Model difficult moral trade-offs.** Each pressure point offers multiple resolution paths aligned with different values (Home, CosmicA, CosmicB). Resolutions have costs (emotional, ideological, violent) and contribute differently to five end states.

4. **Make consequences visible.** Players see what paths they take, what their choices cost, and what narrative outcome they've locked in.

5. **Support emerging storytelling.** The witness knowledge graph and crossover system allow complex cause/effect chains: revealing a secret to the wrong person at the wrong time can close off narrative branches.

### Inspiration: Caves of Qud's House System

The Caves of Qud House system (decompiled and studied) models aristocratic families with:
- Distinct crafts (weaving, metalwork, poison, etc.)
- Succession disputes and external pressure
- Dead members with memorial traditions
- Procedural family generation

The Caves of Ooo implementation reimagines this as a *conversational drama engine* where the player is a witness who can influence the family's trajectory through dialogue.

---

## Technical Architecture

### Data Layer: `HouseDramaData.cs` (~410 lines)

The data model is divided into logical sections:

```
HouseDramaData (top-level)
├── ID, Name
├── CraftIdentityData (what the family practices)
├── RootConflictData (the central antagonism)
├── NpcRoleData[] (6 character roles)
├── WitnessMapData (who knows what)
│   ├── WitnessEntryData[] (per-NPC knowledge)
│   └── DangerousDisclosureData[] (knowledge→consequence edges)
├── PressurePointData[] (exactly 4 pressure points)
│   ├── PressurePointState (dormant/active/resolved/failed)
│   ├── PathData[] (2-3 resolution options per point)
│   │   ├── CorruptionContribution (0-5)
│   │   ├── EmotionalCostMagnitude (difficulty, 1-5)
│   │   ├── CostData[] (Trust, Ideology, Time, Violence, etc.)
│   │   └── EndStateContributionData[] (path→end-state weights)
│   └── TransitionRuleData[] (state machine rules)
├── MemorialActData[] (interactions with dead NPCs)
├── CrossoverEdgeData[] (state propagation between points)
├── EndStateData[] (5 canonical outcomes: Restored/TransformedA/TransformedB/Extinct/Corrupted)
└── CorruptionGradientData (narrative signposts for authoring)
```

**Key design decision:** The data model is *grammar-like*. It defines the syntax of a family drama (archetypes, roles, knowledge states, path alignments) but not the semantics. Authors fill in the specifics (names, dialogue hooks, family history).

### Runtime Layer: `HouseDramaRuntime.cs` (~396 lines)

A static state machine with no MonoBehaviour dependency. Session-only (no persistence until save/load is built).

```csharp
HouseDramaRuntime (static)
├── _dramas: Dict<string, ActiveDrama>
├── RegisterDrama(HouseDramaData) — initialize a drama's runtime state
├── ActivateDrama(string dramaId) — transition dormant→active pressure points
├── IsDramaActive(string) / IsDramaRegistered(string)
├── AdvancePressurePoint(dramaId, pointId, newState, pathId)
│   └── Accumulates CorruptionContribution
│   └── Fires EvaluateCrossovers on state change
├── RevealWitnessFact(dramaId, npcId, factId)
├── AddCorruption(dramaId, amount)
├── IsPathClosed(dramaId, pointId, pathId)
├── ApplyCrossoverEffect(dramaId, effect)
│   └── Parses effect strings: "state:PointID:State" | "reveal:NpcID:FactID" | "corruption:Amount" | "close:PointID:PathID"
├── EvaluateEndState(dramaId) → returns most-specific matching end state
└── ComputeMinCorruptionPathCost(dramaId) → used for authoring validation
```

**ActiveDrama structure:**
```csharp
public class ActiveDrama
{
    public HouseDramaData Data;
    public bool IsActive;
    public Dictionary<string, PressurePointState> PressurePoints;
    public Dictionary<string, HashSet<string>> WitnessKnowledge;
    public Dictionary<string, HashSet<string>> ClosedPaths;
    public int CorruptionScore;
}
```

### Zone Generation: `HouseDramaZoneBuilder.cs` (~184 lines)

An `IZoneBuilder` (priority 4500) that:
1. Loads drama from `HouseDramaLoader`
2. Checks for idempotency (if already active, returns early)
3. Iterates NPC roles (skips dead roles)
4. Resolves blueprints (role → blueprint name via `RoleBlueprints` dict)
5. Places NPCs in interior cells (or fallback to open cells)
6. Attaches `HouseDramaPart` with (DramaID, NpcRole, NpcId)
7. Stamps `ConversationPart.ConversationID = "Drama_{DramaID}_{role}"`

```csharp
private static readonly Dictionary<string, string> RoleBlueprints = new Dictionary<string, string>
{
    { "DiminishedHead",  "Elder"    },
    { "RisingInheritor", "Villager" },
    { "NamedAntagonist", "Merchant" },
    { "SilencedHelper",  "Scribe"   },
};
```

**Idempotency guard (Fix 1):**
```csharp
if (!HouseDramaRuntime.IsDramaActive(_dramaId))
{
    HouseDramaRuntime.RegisterDrama(data);
    HouseDramaRuntime.ActivateDrama(_dramaId);
}
```

This prevents zone rebuilds from wiping drama state.

### World Integration: `OverworldZoneManager.cs` (lines 171–180)

Deterministically assigns one drama per village:

```csharp
var dramaIds = HouseDramaRuntime.GetAllDramaIds();
if (dramaIds.Count > 0)
{
    int zoneSeed = WorldSeed ^ zoneID.GetHashCode();  // Fix 2: stable seed
    int pick = (zoneSeed & int.MaxValue) % dramaIds.Count;
    pipeline.AddBuilder(new HouseDramaZoneBuilder(dramaIds[pick]));
}
```

**Fix 2:** Replaced non-deterministic `poi.GetHashCode()` with `WorldSeed ^ zoneID.GetHashCode()` — matching the exact formula `ZoneManager.GenerateZone` uses for its own RNG seeding. This ensures drama assignment is stable per world seed.

### Conversation Integration

**Actions (`ConversationActions.cs`, 10 drama hooks):**
- `AdvancePressurePoint`: "DramaID:PointID:NewState[:PathID]"
- `RevealWitnessFact`: "DramaID:NpcID:FactID"
- `AddCorruption`: "DramaID:Amount"
- `StartDrama`: "DramaID" (with idempotency guard — Fix 3)
- `TriggerCrossover`: "DramaID:effect_string"

**Predicates (`ConversationPredicates.cs`, 5 drama predicates + IfNot* inversions):**
- `IfDramaActive`: "DramaID"
- `IfPressurePointState`: "DramaID:PointID:ExpectedState"
- `IfPathTaken`: "DramaID:PointID:ExpectedPathID"
- `IfWitnessKnows`: "DramaID:NpcID:FactID"
- `IfCorruptionAtLeast`: "DramaID:Score"

**Entity Tags (`HouseDramaPart.cs`):**
On initialize, exposes drama identity as tags so dialogue nodes can use the existing `IfSpeakerHaveTag` predicate:
```csharp
ParentEntity.SetTag("DramaID", DramaID);
ParentEntity.SetTag("NpcRole", NpcRole);
ParentEntity.SetTag("DramaNpcId", NpcId);
```

### Bootstrap Flow

```
GameBootstrap.Initialize()
    ↓
HouseDramaLoader.LoadAll()
    ↓ (loads Resources/Content/Data/HouseDramas/*.json)
For each drama: HouseDramaRuntime.RegisterDrama(data)
    ↓ (seeds PressurePoints as dormant, WitnessMap, CorruptionScore=0)

(Later, when village zone generates):
OverworldZoneManager.CreateVillagePipeline()
    ↓ (picks drama by WorldSeed ^ zoneID.GetHashCode())
HouseDramaZoneBuilder.BuildZone()
    ↓ (idempotency guard, spawn NPCs, stamp ConversationID)
Zone ready with drama NPCs

(Player converses):
ConversationPredicates.Evaluate("IfDramaActive", ...)
    ↓ (checks HouseDramaRuntime state)
ConversationActions.Execute("AdvancePressurePoint", ...)
    ↓ (updates HouseDramaRuntime, fires crossovers, accumulates corruption)
```

---

## Bug Fixes (PR Review)

| Fix | Severity | Issue | Root Cause | Solution |
|---|---|---|---|---|
| 1 | HIGH | ZoneBuilder reset drama state on rebuild | `RegisterDrama` called unconditionally on every `BuildZone` | Idempotency guard: `if (!IsDramaActive(...)) { RegisterDrama; ActivateDrama; }` |
| 2 | HIGH | Non-deterministic drama assignment | `Math.Abs(poi.GetHashCode())` — identity-based hash | Use `WorldSeed ^ zoneID.GetHashCode()` (matches ZoneManager's seeding) |
| 3 | MEDIUM | `StartDrama` action re-registered live dramas | No guard for already-active dramas; calls `RegisterDrama` unconditionally | Early-return guard: `if (IsDramaActive(arg)) return;` |
| 4 | MEDIUM | `EvaluateEndState` returned first match instead of best match | Loop returned on first matching signature subset | Best-score loop: track largest matching PathSignature.Count |
| 5 | MEDIUM | Corruption accumulated emotional difficulty (wrong category) | `pathData.CorruptionContribution + pathData.EmotionalCostMagnitude` | Only `CorruptionContribution` accumulates; `EmotionalCostMagnitude` is authoring complexity metric |

---

## Test Coverage

**92 tests across 7 test classes** cover the drama system:

| File | Tests | Focus |
|---|---|---|
| `HouseDramaRuntimeTests.cs` | 32 | State machine: registration, activation, pressure point advancement, witness knowledge, corruption, crossovers, end-state evaluation |
| `ConversationPredicatesDramaTests.cs` | 15 | All 5 predicates + IfNot* inversions; edge cases (missing colons, unknown drama) |
| `HouseDramaValidatorTests.cs` | 11 | Schema validation: 5 rules (empty ID, PP with no paths, unknown role, dead NPC without memorial, invalid end state) |
| `HouseDramaZoneBuilderTests.cs` | 11 | Idempotency, dead-role filtering, bootstrap scenario, edge cases |
| `HouseDramaLoaderTests.cs` | 12 | JSON parsing, null/empty input, malformed JSON no-throw (try/catch), duplicate ID overwrites |
| `ConversationActionsDramaTests.cs` | 8 | StartDrama idempotency, bootstrap, graceful failures |
| `HouseDramaZoneBuilderNpcTests.cs` | 3 | NPC spawn with real blueprints, HouseDramaPart fields, ConversationID stamping |

---

## What Works Well

### 1. **Data Model is Grammar-Like**
The schema (archetypes, roles, path alignments, end states) defines the syntax of family drama without prescribing content. Authors can express diverse conflicts within the same framework.

### 2. **Seamless Integration with Existing Systems**
- Conversation actions/predicates dispatch pattern extended perfectly
- Zone generation pipeline (IZoneBuilder) integrates cleanly at priority 4500
- Entity tagging trick reuses existing `IfSpeakerHaveTag` for drama-role gating
- Static manager pattern consistent with FactionManager, PlayerReputation

### 3. **Crossover System is Elegant**
State transitions trigger effects on other pressure points. A single edge can reveal knowledge, add corruption, or close paths. Enables complex cause/effect chains without hardcoding.

### 4. **Witness Map Enables Emerging Storytelling**
Knowledge distribution across NPCs means revealing a fact to the *wrong person at the wrong time* can lock in bad outcomes. DangerousDisclosure rules formalize this.

### 5. **Five-Outcome Model Provides Clear Narrative Structure**
Restored (best), TransformedA/B (compromise), Extinct (failure), Corrupted (worst) map cleanly to player intent and alignment choices.

### 6. **No Hard-Coded Dialogue**
Drama progression is entirely data-driven. Authors author the data; conversations are authored separately and hooked by role + ID convention.

---

## What Is Incomplete / Concerning

### 1. **No Save/Load** ⚠️ CRITICAL
`HouseDramaRuntime` is explicitly "session-only" by design comment. All state (pressure point progress, witness knowledge, corruption, closed paths) is in-memory. Any reload wipes everything.

**Impact:** Players cannot reach authored end states across game sessions. For a roguelike, this may be acceptable; for campaign play, it's a hard blocker.

**Next steps:** Implement serialization for `ActiveDrama` as part of game save data.

---

### 2. **Dead NPCs Are Defined But Orphaned** ⚠️ HIGH
`FoundationalDead` and `LostDead` NPCs have full interiority (Wants, Fears, SelfDeception), MemorialActs with alignment-specific interactions, and witness map entries. The zone builder skips them entirely. No entity is ever created for dead NPCs.

**Gap:** The MemorialActs system (data structures defined, `Validate()` checks them) is never executed. How does a player interact with a dead NPC's memorial? The framework doesn't say.

**Next steps:** Design and implement memorial interaction system (objects, dialogue trees, or environmental storytelling).

---

### 3. **Urgency Timers Are Phantom Features** ⚠️ HIGH
Every `PressurePointData` has `UrgencyTrigger`, `UrgencyEffect`, and `UrgencyTiming`. Example:
- Trigger: "Petra attempts a double-salt knot alone and ruins remaining thread"
- Effect: "Closes Restored path; TransformedA becomes best achievable"
- Timing: "medium"

No runtime system processes urgency. A drama with `UrgencyTiming="short"` behaves identically to one with `UrgencyTiming="long"`. This feature would make dramas feel *alive* — consequences escalate if the player ignores them — but it's unimplemented.

**Next steps:** Add a turns-based urgency clock that fires UrgencyEffect on timeout.

---

### 4. **Conversation ID Validation Is Silent** ⚠️ MEDIUM
The zone builder stamps `ConversationPart.ConversationID = "Drama_{DramaID}_{role.Role}"`. If no matching conversation tree exists, the NPC gets the fallback "Hi." greeting. No warning is emitted; no validation checks this linkage.

**Impact:** Authors can ship mute NPCs without realizing it.

**Next steps:** Add conversation ID validation to `Validate()` or a separate authoring pass. Emit a warning if the drama's expected conversation trees don't exist.

---

### 5. **One Drama Per Village, No Uniqueness Constraint** ⚠️ MEDIUM
With N villages and K authored dramas, the same drama can appear in multiple villages (modular assignment can repeat). There's no geographic filtering, no biome-to-drama mapping, and no mechanism for a drama to span multiple zones.

**Next steps:** Consider adding optional drama flags like `BiomeFilter` or `Unique=true` to control assignment.

---

### 6. **NPC Placement Has Silent Failure Mode** ⚠️ MEDIUM
`PlaceNPCInInterior` tries interior cells (StoneFloor), then open cells. If none exist, it returns null and the NPC silently doesn't spawn. No warning is logged.

**Impact:** In a densely-built village, drama NPCs can vanish without indication.

**Next steps:** Emit a warning if an NPC fails to spawn; consider expanding the fallback cell search.

---

### 7. **`GetDrama()` Still Public & Mutable** ⚠️ MEDIUM
The public method returns an `ActiveDrama` reference (a mutable class). External code can call `GetDrama()`, then mutate the drama's state dict directly, bypassing all runtime method invariants.

**Status:** Fix 3 replaced the only external caller (`ConversationActions.StartDrama`) with `IsDramaRegistered()`. But `GetDrama()` itself is still public.

**Next steps:** Make `GetDrama()` internal. Consider making `ActiveDrama` and `PressurePointState` internal (they're implementation details).

---

### 8. **`ComputeMinCorruptionPathCost` Still Uses `EmotionalCostMagnitude`** ⚠️ LOW
Fix 5 correctly removed `EmotionalCostMagnitude` from runtime corruption accumulation (it's an authoring complexity metric, not a moral cost). But `ComputeMinCorruptionPathCost` still adds it. This method is used for validation ("if the computed path doesn't match intended texture, fix path costs"). The inconsistency will mislead authors.

**Next steps:** Update `ComputeMinCorruptionPathCost` to only use `CorruptionContribution`.

---

### 9. **No Authored Content** ⚠️ BLOCKS USAGE
The system is complete, tested, and fixed. But there are zero drama JSON files in production (only test data). The loader returns an empty list; drama seeding silently no-ops.

**Next steps:** Author at least one complete drama. HouseThresker (salt-weave family, succession dispute) is a reference implementation.

---

### 10. **`DerivedFromWitnessMap` on Crossover Edges Is Never Read** ⚠️ LOW
The field documents whether a crossover edge is derivable from the witness map (author-derived) or hand-authored. Code never reads it. It's purely a documentation hint.

**Status:** Not a bug, but indicates the feature is partial.

---

## Holistic Assessment

### Strengths

The House Drama system is a **well-architected foundation** for procedural family storytelling. The data model is thoughtful and expressive. The runtime state machine is clean and stateless (no MonoBehaviour dependency). Integration with the existing conversation and zone generation systems is seamless. The code quality is high, and the test suite is broad.

**The system makes five correctness guarantees:**
1. A drama's state cannot be wiped by zone rebuild (Fix 1)
2. A drama assignment is stable per world seed (Fix 2)
3. Activating an already-active drama is idempotent (Fix 3)
4. End state selection is deterministic (best-match, not first-match) (Fix 4)
5. Corruption only accumulates from moral alignment, not emotional difficulty (Fix 5)

All five guarantees are tested.

### Gaps

The main gap is between **schema richness and execution completeness**. The data model defines:
- **6 NPC role types** (FoundationalDead, LostDead, DiminishedHead, RisingInheritor, NamedAntagonist, SilencedHelper)
- **6 cost types** (SelfEdit, Ideology, Time, Trust, LineagePurity, Violence)
- **Urgency system** (triggers, effects, timing)
- **Memorial acts** (interactions with dead NPCs)
- **Witness knowledge propagation** (DangerousDisclosures)
- **Crossover edges** (state-triggered effects)

Of these, only **RisingInheritor, NamedAntagonist, SilencedHelper, and DiminishedHead** are spawned as live NPCs. **FoundationalDead and LostDead are data-only**. **Urgency timers never fire**. **MemorialActs are validated but never processed**. **DangerousDisclosures are data-only**.

About **40% of the defined features have no runtime processing**.

### Production Readiness

- ✅ State machine: robust, tested, fixed
- ✅ Zone integration: clean, idempotent
- ✅ Conversation hooks: comprehensive
- ❌ Save/load: not implemented (critical gap)
- ❌ Authored content: no production dramas
- ⚠️ Dead NPC interactions: undefined
- ⚠️ Urgency system: framework exists, no processor
- ⚠️ Memorial interactions: data structure exists, no processor

### Phases

This is a solid **M1 (foundation)** that will require additional passes:

- **M2 (urgency, memorial interactions):** Implement the missing runtime processors
- **M3 (save/load, campaign persistence):** Serialize drama state and wire it into game save data
- **M4 (content):** Author 5–10 diverse dramas across biomes

---

## Files

| File | Role | LOC | Status |
|---|---|---|---|
| `HouseDramaData.cs` | Data model + Validate() | 410 | ✅ Complete |
| `HouseDramaLoader.cs` | JSON loading + try/catch | 92 | ✅ Complete |
| `HouseDramaPart.cs` | Entity component + tagging | 39 | ✅ Complete |
| `HouseDramaRuntime.cs` | State machine | 396 | ✅ Complete |
| `HouseDramaZoneBuilder.cs` | NPC spawning | 184 | ✅ Complete |
| `ConversationActions.cs` (10 hooks) | Drama action dispatch | 399 | ✅ Complete |
| `ConversationPredicates.cs` (5 predicates) | Drama predicate dispatch | 235 | ✅ Complete |
| 7 test files | 92 tests, 847 lines | 847 | ✅ Complete |

---

## References

- **Caves of Qud inspiration:** House system from decompiled Qud (aristocratic families with crafts, succession disputes, procedural generation)
- **Data-driven design:** Conversation actions/predicates pattern (existing Caves of Ooo infrastructure)
- **State machine pattern:** HouseDramaRuntime follows the static manager pattern (FactionManager, PlayerReputation)
