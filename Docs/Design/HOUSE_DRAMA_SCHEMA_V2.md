# House Drama Schema v2.0
## Generative Lore Template for Caves of Ooo

This schema defines the structure for generating House dramas — deep faction and family lore
with emergent player interactions, mechanical path costs, and accumulating world-state consequences.

Validated against two full instantiations: House Ordren and House Thresker.

---

## Root Structure

```
HouseDrama {
  craft             : CraftIdentity
  rootConflict      : RootConflict
  npcRoles          : Map<NpcId, NpcDef>
  witnessMap        : WitnessMap
  pressurePoints    : PressurePoint[4]    // exactly one per archetype
  memorialActs      : MemorialAct[]
  crossovers        : CrossoverEdge[]
  endStates         : EndState[5]         // exactly one per class
  corruptionGradient: CorruptionGradient
}
```

---

## Craft Identity

```
CraftIdentity {
  name                  : string
  distinctiveProperty   : string   // what separates this craft from a generic trade
  masterMarker          : string   // observable sign of full mastery
  minimumPractitioners  : int      // below this the craft fails within one cycle
  currentPractitioners  : {
    active   : int
    emerging : int
  }
  cycleDuration         : string
  failureCondition      : string   // DERIVED: active < minimum for > 1 cycle
                                   // the Extinct end-state must be computable from this
}
```

**Design note:** pressure points should emerge from craft constraints. Ask — what threatens
`minimumPractitioners`? what threatens `cycleDuration`? what threatens the `distinctiveProperty`?
— and let those answers generate the SuccessionHinge and Wound archetypes rather than inventing
them independently.

---

## Root Conflict

```
RootConflict {
  adversary           : FactionRef
  adversaryProject    : string    // what the adversary is actually doing
  adversaryAlignment  : AlignmentRef
  namedAntagonist     : NpcId     // must match a NpcDef with role: NamedAntagonist
  secretTruth         : string    // the fact the House does not collectively know
  disclosureStakes    : string    // what changes if secretTruth becomes public
}
```

---

## NPC Roles

```
NpcDef {
  role      : RoleType
  variant?  : string      // e.g. "strategic-compromised", "grief-fractured",
                          //      "idealized-origin", "compromised-origin"
  alive     : bool
  age?      : int

  interiority: {
    wants           : string   // active desire driving behavior
    fears           : string   // what they are protecting against
    selfDeception   : string   // the false frame they use to justify stasis
    changeCondition : string   // the specific external event that breaks their frame
                               // this field makes path costs derivable —
                               // if the Counsel path costs Time 3 + high Trust,
                               // it is because changeCondition is hard to engineer
  }

  // NamedAntagonist only:
  antagonistProfile?: {
    motivation          : string
    emotionalStyle      : string   // how they treat counterparties; shapes confrontation paths
    vulnerability       : string   // the internal contradiction or attachment point
    lineTheyWontCross   : string   // constraint that survives full commitment to their project
    changedBy: {
      condition           : string
      reachableInDrama    : bool   // false = this drama cannot produce sufficient evidence
                                   // false is valid and encouraged; unreachable changedBy
                                   // produces better antagonists than convertible ones
      evidenceTierNeeded  : string // what quality of proof the condition requires
    }
  }
}

RoleType = FoundationalDead | LostDead | DiminishedHead | RisingInheritor
         | NamedAntagonist | SilencedHelper
```

**FoundationalDead** admits two variants:
- `idealized-origin` — the standard the living head fails to match
- `compromised-origin` — the standard the living head is secretly continuing

Downstream dialogue generation must know which; declare the variant explicitly.

**RisingInheritor** may be bound to multiple NpcDefs with different ages and different
relationships to the drama (e.g. adult-investigator + child-custodian). Each gets their
own interiority block.

---

## Witness Map

```
WitnessMap {
  entries              : WitnessEntry[]
  dangerousDisclosures : DangerousDisclosure[]
}

WitnessEntry {
  subject      : NpcId | ExternalRef   // ExternalRef for communities or factions
  knows        : string[]
  suspects?    : string[]
  doesNotKnow? : string[]
}

DangerousDisclosure {
  condition   : string   // "if X is revealed to Y before Z resolves"
  consequence : string   // what happens; should be mechanically specific
}
```

**Design note:** crossover edges of type Stabilization and TruthPropagation should be
derivable from this map rather than hand-authored. If a Stabilization edge exists between
pressure points A and B, there should be a WitnessEntry change that explains why — the
DiminishedHead's knowledge state changed because of path resolution in A. Mark
`derivedFromWitnessMap: true` on those edges; it signals they don't need independent
maintenance.

**Validation:** every element named in `rootConflict.secretTruth` must appear in at least
one `WitnessEntry.knows`. Every `DangerousDisclosure.condition` must reference elements
present in WitnessEntry fields.

---

## Pressure Points

```
PressurePoint {
  id                  : string
  archetype           : ArchetypeType
  temporal            : TemporalMode
  dominantAlignment   : AlignmentRef | Variable

  activationState: {
    state     : ActivationState
    substate? : ActivationSubstate     // set at runtime; null at schema definition
    pathTaken?: string                 // set at runtime on resolution;
                                       // required for follow-on drama generation
    transitionRules: TransitionRule[]
  }

  urgencyEscalation?: {
    trigger  : string   // what event forces a state transition
    effect   : string   // what changes when the trigger fires
    timing   : Timing   // how long the player has before trigger fires
  }

  // HiddenEvidence archetype only:
  hiddenEvidence?: HiddenEvidenceDef

  paths: Path[]
}

ArchetypeType   = Wound | PreventedHelp | SuccessionHinge | HiddenEvidence
TemporalMode    = Past | Suspended | Imminent | Latent
ActivationState = dormant | active | resolved | failed

ActivationSubstate = string   // free-form; conventions:
                               //   "resolved:complete"  — fully resolved
                               //   "resolved:partial"   — partially resolved;
                               //                          follow-on dramas handle remainder
                               //   "failed:ignored"     — player chose not to engage
                               //   "failed:escalated"   — urgencyEscalation fired

Timing = immediate | short | medium | long

TransitionRule {
  from      : ActivationState
  to        : ActivationState
  condition : string
}
```

Every pressure point needs at least one TransitionRule for each reachable state transition.
Resolved pressure points must record `pathTaken`. This is the field that enables a second
drama to reference what happened in a first drama rather than treating the House as if
nothing occurred.

---

## Hidden Evidence

```
HiddenEvidenceDef {
  subtypes          : EvidenceSubtype[]   // composable; list all that apply
  custodian?        : NpcId
  maintenanceCycle? : string              // Living: how often tending is required
  lossCondition?    : string              // Living: what causes permanent loss
  gateCondition?    : string              // Gated: who controls access and how
  revealMethods     : RevealMethod[]
}

EvidenceSubtype = Passive | Active | Decaying | Living | Gated
```

Subtypes compose independently:
- `Passive`  — static; readable without conditions
- `Active`   — continues affecting the world while undiscovered
- `Decaying` — degrades over time; discovery window is limited
- `Living`   — requires maintenance; can be neglected into permanent loss; generates
               custody and protection mechanics unavailable to Passive
- `Gated`    — access controlled by a specific NpcId or condition; discovery requires
               permission, theft, or social engineering

Example: `[Living, Gated]` = custodian controls access AND evidence can be neglected or
lost. Both properties must be addressed in path design.

```
RevealMethod {
  alignment : AlignmentRef | None
  method    : string
}
```

---

## Paths

```
Path {
  id                   : string
  primaryAlignment     : AlignmentRef
  secondaryAlignment?  : AlignmentRef    // for paths that pull in two directions;
                                         // generates nuanced faction-rep consequences

  costs                : Cost[]
  emotionalCost?       : EmotionalCost   // required on any path approaching Corrupted;
                                         // used by runtime to compute corruption gradient

  corruptionContribution? : int          // 0–5; runtime sums these to find least-resistance
                                         // path to Corrupted end-state
  endStateContributions?  : Map<EndStateId, int>   // weighted per end-state
}

Cost {
  type      : CostType
  magnitude : int
  timing    : CostTiming   // when the player pays
}

EmotionalCost {
  description : string   // prose description of the emotional weight
  magnitude   : int      // 1–5; must be consistent with path's prose description
}

CostType   = SelfEdit | Ideology | Time | Trust | LineagePurity | Violence
CostTiming = front-loaded | back-loaded | ongoing
```

- `front-loaded` — player pays before receiving information or progress
- `back-loaded`  — player pays after acting; consequences arrive later
- `ongoing`      — cost recurs for the duration of the path resolution

---

## Memorial Acts

```
MemorialAct {
  subject      : NpcId                      // the dead NPC
  objects      : string[]                   // physical objects associated with them
  interactions : Map<AlignmentRef, string>  // what each alignment unlocks here
}
```

Every FoundationalDead and LostDead NpcDef requires at least one MemorialAct. Objects exist
in the world regardless; encoding them surfaces ambient interaction points that reward
exploration and provide lore through object-examination rather than dialogue. The interactions
map should produce different *kinds* of information per alignment — not just different amounts.

---

## Crossover Edges

```
CrossoverEdge {
  type    : CrossoverType
  from    : { pressurePointId: string; condition: string }
  to      : { pressurePointId: string; effect: string }
  derivedFromWitnessMap : bool   // true  = auto-derivable from knowledge propagation;
                                 //         no independent maintenance needed
                                 // false = hand-authored; review if witnessMap changes
}

CrossoverType = ActorDoubling | TruthPropagation | Stabilization | Amplification
              | Decay | AntagonistReveal | MutualExclusion
```

---

## End States

```
EndState {
  id               : EndStateId
  name             : string
  pathSignature    : string[]     // which path patterns produce this end-state
  tag              : string       // player-facing achievement tag
  rewards          : string[]     // unlocked access, relationships, or abilities
  externalResonance: {
    description : string
    scope       : ResonanceScope
  }
}

EndStateId    = Restored | TransformedA | TransformedB | Extinct | Corrupted
ResonanceScope = house-local | regional | basin-wide | cosmic
```

**Scope constraint:** no more than 2 paths per drama may produce `externalResonance` above
`house-local`. Exceeding this creates a combinatorial explosion of world-state flags that
follow-on dramas cannot manage. If a third path wants regional+ scope, demote one existing
path or compress two paths into one.

Extinct must be computable from `craft.failureCondition` without additional narrative
declaration.

---

## Corruption Gradient

```
CorruptionGradient {
  earlyWarningSignal : string   // what the player observes as they approach Corrupted;
                                // should read as success or comfort, not obvious failure
  pointOfNoReturn    : string   // the specific path or event that closes
                                // Restored and Transformed end-states
}
```

`leastResistancePath` is **not declared here**. It is computed at runtime by summing
`(Cost.magnitude + EmotionalCost.magnitude)` across all paths and finding the minimum-cost
sequence reaching Corrupted. If the computed path does not match the drama's intended
texture, fix the Cost and EmotionalCost values on individual paths — not this field. A
declared `leastResistancePath` that diverges from the cost math is a lie.

---

## Type Reference

```
AlignmentRef = Home | CosmicA | CosmicB | Variable
             // Home    = domestic, lineage, craft continuity
             // CosmicA = Palimpsest — annotation, overwrite, memory
             // CosmicB = Rot Choir  — substrate, decay, integration

FactionRef   = any faction name registered in Factions.json
NpcId        = string key used in npcRoles map
ExternalRef  = string label for a community or group ("seed-keeper network")
```

---

## Validation Rules

### 1. Archetype Completeness
Each of `{ Wound, PreventedHelp, SuccessionHinge, HiddenEvidence }` appears as exactly one
PressurePoint.

### 2. End-State Completeness
Each of `{ Restored, TransformedA, TransformedB, Extinct, Corrupted }` appears as exactly
one EndState.

### 3. Corrupted Reachability
The Corrupted end-state must be reachable through a path sequence whose summed
`corruptionContribution` crosses threshold without completing any Restored or Transformed
`pathSignature`.

### 4. Single-Alignment Playthrough
For each of `{ Home, CosmicA, CosmicB }`: a viable sequence exists through all four pressure
points using only that alignment's paths, producing a non-Corrupted end-state.

### 5. Zero Dead Ends
Each PressurePoint has at least one entry trigger accessible with zero or near-zero cost and
no prior path completion required.

### 6. Witness Map Coverage
Every element named in `rootConflict.secretTruth` appears in at least one
`WitnessEntry.knows`. Every `DangerousDisclosure.condition` references elements present in
WitnessEntry fields.

### 7. Craft Failure Derivability
The Extinct EndState's conditions must be derivable from `craft.failureCondition`. If the
Extinct narrative requires facts not in CraftIdentity, add them to CraftIdentity — not to
the EndState.

### 8. Antagonist Asymmetry
`antagonistProfile.changedBy.reachableInDrama = false` is a valid and encouraged value. At
least one `changedBy` field per drama should be unreachable in the drama's standard
possibility space. An antagonist with a theoretically-transformative but
practically-unreachable `changedBy` condition is better than one who converts on success.

### 9. Cost Honesty
The computed least-resistance path to Corrupted (runtime sum of `Cost.magnitude +
EmotionalCost.magnitude`) must match the drama's intended texture. If it doesn't, the costs
are wrong. Fix them.

### 10. Activation State Coverage
Each PressurePoint has at least one TransitionRule per reachable state transition. Resolved
PressurePoints must have `pathTaken` set. This enables follow-on drama generation.

### 11. Resonance Scope Constraint
No more than 2 paths per drama produce `externalResonance.scope` above `house-local`.

### 12. Memorial Act Coverage
Every NpcDef with role `FoundationalDead` or `LostDead` has at least one MemorialAct entry
with at least two alignment interactions.

### 13. Interiority Consistency
Each `NpcDef.interiority.changeCondition` must correspond to at least one path in the drama
that can produce it. If no path can produce the changeCondition, either add a path or revise
the condition. An NPC whose frame can never be broken is not a dramatic character — they are
an obstacle. Distinguish these intentionally.

### 14. Witness Map / Crossover Honesty
CrossoverEdges marked `derivedFromWitnessMap: true` must be explainable by a knowledge-state
change in `WitnessMap.entries`. If you cannot point to the WitnessEntry that produces the
edge, mark it `derivedFromWitnessMap: false` and maintain it manually.

---

## Schema Changelog

### v1.0 (implicit — Ordren/Thresker baseline)
- Root conflict, NPC roles, pressure points with archetypes/temporal modes
- 8 canonical crossover types, 5 end-state classes
- Path cost vocabulary: SelfEdit, Ideology, Time, Trust, LineagePurity, Violence

### v2.0
- **CraftIdentity** object replaces `houseCulture` string; pressure points now derivable
  from craft constraints; Extinct end-state computable from `failureCondition`
- **NPC Interiority Triad** (`wants / fears / selfDeception / changeCondition`) on every
  NpcDef; path costs now derivable from `changeCondition`
- **FoundationalDead variant flag** (`idealized-origin` vs `compromised-origin`)
- **AntagonistProfile** extended with `vulnerability`, `lineTheyWontCross`, `changedBy`
  with explicit `reachableInDrama` flag encouraging asymmetry
- **WitnessMap** with `WitnessEntry` and `DangerousDisclosure`; Stabilization/
  TruthPropagation crossovers now derivable rather than hand-authored;
  `derivedFromWitnessMap` flag on CrossoverEdge
- **EvidenceSubtype** composable enum (`Passive | Active | Decaying | Living | Gated`)
  replacing single subtype; `Living` generates maintenance/custody mechanics;
  `Gated` generates access/permission mechanics; subtypes combine independently
- **MemorialActs** array; required for all dead NPCs; exposes ambient lore through
  object-examination with per-alignment interaction variants
- **Path.secondaryAlignment** for paths with dual faction-rep consequences
- **Path.emotionalCost** (`description + magnitude 1–5`); enables corruption gradient
  computation
- **Path.corruptionContribution** (0–5); runtime sums these across path sequences
- **Cost.timing** (`front-loaded | back-loaded | ongoing`)
- **PressurePoint.activationState** with `{ state, substate, pathTaken, transitionRules }`;
  enables follow-on drama generation; `pathTaken` required on resolution
- **PressurePoint.urgencyEscalation** with trigger, effect, and timing
- **EndState.externalResonance** scoped by `ResonanceScope`; 2-path cap above house-local
- **CorruptionGradient** retains `earlyWarningSignal` and `pointOfNoReturn`; drops
  declarative `leastResistancePath` in favor of runtime computation from cost math
- Validation rules expanded from 8 to 14
