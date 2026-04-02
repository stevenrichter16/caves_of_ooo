# Repairs And Restoration

## Purpose

This document defines a concrete hybrid repair framework for `caves-of-ooo`.

The design goal is not to simulate real-world plumbing or turn every broken object into a generic crafting prompt. The goal is to make repair into a meaningful world-facing activity with these properties:

- places can degrade, stabilize, recover, or improve
- the player can choose how deeply to engage with repair
- short-term interventions and long-term restorations feel different
- local materials, magic, manuals, and social stewardship all matter
- repaired places become worth revisiting because they remember what happened

This is the concrete system spec that should sit underneath well repair, heat-stone repair, irrigation repair, and later restoration content.

---

## Design Thesis

Repairs in this game should use a **hybrid layered model**.

A meaningful site is made of two interlocked systems:

1. a **physical infrastructure layer**
2. a **magical inscription / calibration layer**

Most sites should also have a third layer:

3. a **social stewardship layer**

That means:

- stone can crack
- channels can clog
- ropes can rot
- but runes can also drift
- maintenance inscriptions can decay
- and locals can forget how to keep a system healthy after the player leaves

This keeps the system grounded while preserving the setting-specific role of Inkbound-style inscription, manuals, and knowledge transmission.

---

## Player Commitment Model

Repairs are intentionally **optional**.

The player should be able to:

- ignore broken infrastructure entirely
- apply a quick stopgap and move on
- solve a site properly if they care
- invest deeply in a place and leave it better than before

This matters because the repair system is more compelling if it is a chosen relationship with the world rather than mandatory checklist labor.

The intended player profiles are:

### 1. Passerby
- sees broken things
- may help once if convenient
- does not manage settlements deeply

### 2. Field Mage / Tinkerer
- uses quick spells and materials to stabilize important sites
- solves selected local problems
- returns to favorite places when it pays off

### 3. Steward / Restorer
- adopts specific settlements or districts
- completes durable repairs
- teaches caretakers
- intentionally builds a network of improved places

The system should support all three without forcing everyone into the third.

---

## Repair Loop

Every major repairable site should support this loop:

1. **Notice**
   - a broken site is visible in the world
   - NPCs and inspect text communicate symptoms

2. **Diagnose**
   - player learns what is actually wrong
   - this may require inspection, an NPC conversation, a manual, or a spell

3. **Choose a method**
   - expedient fix
   - durable repair
   - improvement / stewardship extension

4. **Execute the repair**
   - consume materials, knowledge, spell use, social favor, or time

5. **Persist the outcome**
   - site state changes permanently or until relapse
   - settlement reactions update

6. **Revisit**
   - the place reflects what the player did or neglected

---

## Core System Objects

The current settlement scaffolding should grow into these conceptual objects.

### SettlementState
World-level persistent state for a settlement or settlement-like site cluster.

Core fields:
- `SettlementId`
- `SettlementName`
- `LastAdvancedTurn`
- `Conditions`
- `Dictionary<string, RepairSiteState> Sites`
- `Prosperity`
- `Trust`
- `Stability`

### RepairSiteDefinition
Static authored data for one repairable site.

Core fields:
- `SiteId`
- `SettlementId`
- `DisplayName`
- `SiteType`
- `LocationHint`
- `ProblemType`
- `Tags`
- `BaselineAxes`
- `SiteVisualStates`
- `AvailableMethods`
- `FailureHooks`
- `ImprovementHooks`

### RepairSiteState
Persistent mutable state for one site.

Core fields:
- `SiteId`
- `SiteType`
- `ProblemType`
- `Stage`
- `OutcomeTier`
- `LastTouchedTurn`
- `ResolvedAtTurn`
- `RelapseAtTurn`
- `Axes`
- `KnownDiagnosisFlags`
- `AppliedMethodHistory`
- `AssignedCaretakerId`
- `PendingConsequences`

### RepairMethodDefinition
Static authored data for one way to intervene.

Core fields:
- `MethodId`
- `DisplayName`
- `MethodClass`
- `Requirements`
- `Costs`
- `AxisChanges`
- `StagePreconditions`
- `NarrativeText`
- `ImmediateEffects`
- `RelapseRules`
- `FollowUpUnlocks`

---

## Site Axes

Every meaningful repair site uses the same four primary axes.

These should be stored as small bounded integers, ideally `0-3`.

### 1. Structural Integrity
Physical soundness of the site.

Scale:
- `0` ruined
- `1` unstable
- `2` functional
- `3` reinforced / improved

Represents:
- masonry
- ropes
- housings
- gate frames
- channels
- brackets
- pipework
- anchor stones

Questions this axis answers:
- can the site physically operate
- is it safe
- is collapse or leakage likely

### 2. Contamination / Instability
How polluted, corrupted, overheated, clogged, blighted, or otherwise compromised the site is.

Scale:
- `0` severe problem
- `1` lingering problem
- `2` clean / stable
- `3` exceptionally clean / buffered

Represents:
- fungal seepage in a well
- soot buildup in a heat stone system
- silt / root intrusion in irrigation
- curse residue
- runaway magical interference

Questions this axis answers:
- does the site produce harmful outputs
- is it volatile or brittle
- is the player merely masking symptoms

### 3. Inscription Fidelity
How accurate and complete the magical logic layer is.

Scale:
- `0` broken / unreadable / drifted
- `1` patched or partial
- `2` correct and stable
- `3` elegantly reworked / optimized

Represents:
- rune rings
- timing sigils
- calibration inscriptions
- maintenance notations
- water-drawing instructions
- heat-distribution logic

Questions this axis answers:
- does the magic still know what it is meant to do
- is the current behavior a degraded patch
- can the site self-regulate correctly

### 4. Stewardship
How well the site will hold up after the player leaves.

Scale:
- `0` neglected
- `1` fragile local practice
- `2` maintained routinely
- `3` institutionalized / resilient

Represents:
- whether locals know how to care for it
- whether a caretaker exists
- whether maintenance notes are available
- whether the site has social ownership and ritual continuity

Questions this axis answers:
- will the site relapse
- does the settlement actually know how to live with the fix
- does the player’s work persist culturally, not just mechanically

---

## Derived Outcome Model

The game should not treat sites as single booleans.

### Site Stage
A readable authored label, used for dialogue, visuals, and inspection text.

Typical stages:
- `Broken`
- `Contained`
- `Functional`
- `Improved`
- `Relapsed`
- `Botched`

### Outcome Tier
A coarse reward/quality band.

Suggested tiers:
- `None`
- `Temporary`
- `Stable`
- `Improved`
- `Brittle`
- `Botched`

### Derived Outcome Rules
Recommended baseline rules:

| Condition | Result |
|---|---|
| Integrity `0` or Inscription `0` | Site cannot be fully functional |
| Integrity `>=2` and Instability `>=2` and Inscription `>=2` | Site can reach `Functional` |
| Integrity `>=2` and Instability `>=2` and Inscription `>=2` and Stewardship `>=2` | Site can reach `Stable` outcome |
| Integrity `3` and Instability `3` and Inscription `>=2` and Stewardship `>=2` | Site can reach `Improved` outcome |
| Any quick-fix method with relapse timer and Stewardship `<=1` | `Temporary` outcome |
| Integrity `1` with otherwise functional axes | `Brittle` outcome |
| Instability `0` after a forceful shortcut | `Botched` or hazardous partial state |

This gives us a reusable logic skeleton without forcing every site into the same authored text.

---

## Method Classes

Each site should draw from these recurring method classes.

### 1. Expedient Spell
Fast containment. Usually raises `Instability`, sometimes `Inscription`, but rarely `Integrity` or `Stewardship`.

Use for:
- temporary water purification
- emergency heat relighting
- clearing blockages just long enough to operate

Pros:
- immediate relief
- low setup

Cons:
- relapse likely
- rarely improves the place

### 2. Material Reconstruction
Rebuilds the physical layer.

Use for:
- replacing cracked ring stones
- resealing channels
- swapping heat braces
- rebuilding sluice gates

Pros:
- tangible
- durable

Cons:
- requires field materials or trade
- may still need magical retuning afterward

### 3. Manual-Guided Calibration
Uses a manual or grimoire to repair the logic layer correctly.

Use for:
- restoring filtration ring patterns
- retiming heat flow sequences
- recalibrating irrigation logic

Pros:
- unlocks proper long-term results
- strong lore fit

Cons:
- requires discovery and interpretation

### 4. Inkbound Reinscription
Uses specialized inks or inscription media to restore the magical logic layer.

Use for:
- tracing missing runes
- rewriting damaged maintenance clauses
- renewing the system’s “memory” of intended behavior

Pros:
- distinctive setting flavor
- elegant and magical

Cons:
- should not replace physical repair
- requires correct diagnosis to avoid cargo-cult repairs

### 5. Stewardship / Training
Turns a site from merely repaired into socially maintained.

Use for:
- teaching a keeper
- leaving maintenance notes
- assigning a caretaker
- funding or formalizing a local routine

Pros:
- strongest persistence value
- best revisitation payoff

Cons:
- not always available immediately
- often requires trust or correct local relationships

---

## Materials Model

Do not use fully realistic raw-part simulation.

Do not use fully abstract `repair points` either.

Use **typed, site-flavored materials**.

### Material Categories

#### Structural Materials
Used to raise `Integrity`.

Examples:
- `SealingClay`
- `DressedStone`
- `TimberBrace`
- `CopperBracket`
- `KilnBrick`

#### Purification / Environmental Media
Used to raise `Instability`.

Examples:
- `SilverSand`
- `FilterReeds`
- `CharcoalBundle`
- `AntifungalSalts`
- `CoolingAsh`

#### Inscription Media
Used to raise `Inscription`.

Examples:
- `MaintainerInk`
- `SilverSuspensionInk`
- `HeatLacquer`
- `CanalSealPigment`
- `MemoryBlack`

#### Stewardship Enablers
Used to raise `Stewardship` indirectly.

Examples:
- `WellManual`
- `CaretakerLedger`
- `SeasonalRotationChart`
- `KilnTimingNotebook`

### Why this mix
- physical layer keeps repairs grounded
- inscription layer keeps Inkbound lore relevant
- knowledge layer stops materials from being sufficient by themselves

---

## Knowledge Model

### What manuals do
Manuals should not be single-use keys.

They should do one or more of these:
- reveal diagnosis
- unlock a precise method
- reduce wasted materials
- improve final outcome quality
- enable teaching a caretaker

### What spells do
Spells are best used for:
- diagnosis
- containment
- activation
- calibration
- cleanup

Spells should usually not perform the entire permanent repair on their own unless the site is trivial.

### What Ink does
Ink should be the medium for:
- retuning
- rewriting
- re-inscribing
- formalizing maintenance routines

Ink should not be able to replace missing masonry, ropes, or structural braces.

---

## Consequence Model

Every meaningful repair site should feed back into at least three domains:

1. **local description / visuals**
2. **NPC dialogue and attitudes**
3. **economic or comfort output**

Optional fourth and fifth domains:

4. local services or unlocks
5. future problems and follow-up story

This is what makes the system about places rather than isolated object interactions.

---

## Outcome Tables

## Generic Site Outcome Table

| Integrity | Instability | Inscription | Stewardship | Default Outcome |
|---|---:|---:|---:|---|
| 0-1 | any | any | any | Broken or brittle |
| 2 | 0-1 | any | any | Unsafe / contaminated partial function |
| 2 | 2 | 0-1 | any | Miscalibrated function |
| 2 | 2 | 2 | 0-1 | Functional but relapse-prone |
| 2 | 2 | 2 | 2 | Stable restoration |
| 3 | 3 | 2 | 2 | Strong stable restoration |
| 3 | 3 | 3 | 2-3 | Improved settlement asset |
| any | any | any | any after reckless shortcut | Botched or side-effect state |

## Method-Class Outcome Table

| Method Class | Typical Axis Effects | Typical Outcome |
|---|---|---|
| Expedient Spell | `Instability +1/+2`, sometimes `Inscription +1` | Temporary containment |
| Material Reconstruction | `Integrity +1/+2` | Necessary foundation, not sufficient alone |
| Manual-Guided Calibration | `Inscription +1/+2` | Stable restoration when paired correctly |
| Inkbound Reinscription | `Inscription +1/+2`, sometimes `Stewardship +1` if notes left behind | Improved precision / optimization |
| Stewardship / Training | `Stewardship +1/+2` | Prevents relapse, unlocks improved outcome |

---

## Authored Site Example: MainWell

### Narrative Role
The settlement’s primary water source. This is the clearest first example because it affects health, trade, crops, and village trust.

### Site Identity
- `SiteId`: `MainWell`
- `SiteType`: `Well`
- `ProblemType`: `FouledWater`
- `DisplayName`: `main well`
- `LocationHint`: village center / square

### Initial Diagnosis
The well has two overlapping issues:
- a cracked filtration ring stone
- fungal seepage entering from uphill runoff

### Initial Axis State
| Axis | Value | Reason |
|---|---:|---|
| Integrity | 1 | ring structure is cracked but not collapsed |
| Instability | 0 | water is fouled |
| Inscription | 1 | filtration logic still partly exists but is drifting |
| Stewardship | 1 | locals know habits, not proper maintenance |

### Symptoms
- bitter water
- villagers complain
- reduced food/water quality in trade stock
- possible light sickness flavor text

### Available Methods

#### A. Purify Water Rite
- Class: Expedient Spell
- Requirements:
  - `KnowsPurifyWater`
- Costs:
  - mana or action cost only
- Axis Changes:
  - `Instability 0 -> 2`
- Does Not Change:
  - `Integrity`
  - `Stewardship`
- Relapse:
  - yes, because physical source remains
- Outcome:
  - `Contained`
  - `Temporary`

Reasoning:
- lets a helpful passerby solve the immediate crisis
- should not permanently fix upstream seepage or cracked stone

#### B. Replace Filter Segment
- Class: Material Reconstruction
- Requirements:
  - `SilverSand`
  - `SealingClay`
- Costs:
  - consume both
- Axis Changes:
  - `Integrity 1 -> 2`
  - `Instability 0 -> 1`
- Outcome:
  - better, but not fully stable without retuning

Reasoning:
- material work should matter
- this alone should not be the best outcome because the well is also magically tuned

#### C. Manual-Guided Reattunement
- Class: Manual-Guided Calibration
- Requirements:
  - `WellMaintenanceManual`
  - optionally `SilverSuspensionInk` or `SilverSand`
- Axis Changes:
  - `Inscription 1 -> 2`
  - if using inscription media well, `Instability +1`
- Outcome:
  - when paired with structural repair, yields stable function

Reasoning:
- the manual matters because it explains the intended filtration pattern
- this is where Inkbound-like knowledge earns its keep

#### D. Teach The Well-Keeper
- Class: Stewardship / Training
- Requirements:
  - site already at stable function
  - local trust with keeper
- Axis Changes:
  - `Stewardship 1 -> 3`
- Outcome:
  - improved persistence
  - unlocks `Improved` tier if other axes are healthy

Reasoning:
- a place is not really repaired if nobody there knows how to keep it repaired

### MainWell Outcome Table

| Integrity | Instability | Inscription | Stewardship | Stage | World Result |
|---|---:|---:|---:|---|---|
| 1 | 0 | 1 | 1 | Fouled | bitter water, poor stock, complaints |
| 1 | 2 | 1 | 1 | Freshened | drinkable for now, relapse later |
| 2 | 1 | 1 | 1 | Patched | less dangerous but still wrong |
| 2 | 2 | 2 | 1 | Repaired | stable well, but relapse possible over long neglect |
| 2 | 2 | 2 | 3 | Maintained | durable village asset |
| 3 | 3 | 2-3 | 3 | Improved Well | cleaner output, stronger trust, better trade |

### Return Effects
- temporary: villagers thank you, then later say the bitterness returned
- repaired: water remains good, food/water trade returns to normal
- maintained/improved: villagers speak of monthly maintenance, health improves, trade gets a small bonus

---

## Authored Site Example: BakeryHeatStone

### Narrative Role
A district-scale warmth and oven-distribution site that affects bread, comfort, and the town’s sense of normalcy.

### Site Identity
- `SiteId`: `BakeryHeatStone`
- `SiteType`: `HeatStone`
- `ProblemType`: `HeatDrift`
- `DisplayName`: `bakery heat stone`
- `LocationHint`: bakery district or communal oven room

### Initial Diagnosis
The stone still glows, but heat distribution is erratic because:
- soot has accumulated in the heat channels
- the timing sigils are misaligned
- one retaining bracket is failing

### Initial Axis State
| Axis | Value | Reason |
|---|---:|---|
| Integrity | 1 | bracket and housing are failing |
| Instability | 1 | heat output spikes and gutters |
| Inscription | 0 | timing sequence has drifted badly |
| Stewardship | 0 | bakers know workarounds, not maintenance |

### Symptoms
- ovens go cold at the wrong times
- bread quality worsens
- bakery hours shrink
- residents complain about cold mornings

### Available Methods

#### A. Rekindle Burst
- Class: Expedient Spell
- Requirements:
  - fire or heat spell
- Axis Changes:
  - `Instability 1 -> 2` for a short window
- Does Not Change:
  - `Integrity`
  - `Inscription`
  - `Stewardship`
- Outcome:
  - emergency warmth only

Reasoning:
- good for weathering a crisis, bad as a permanent answer

#### B. Replace Retaining Bracket And Clear Channels
- Class: Material Reconstruction
- Requirements:
  - `CopperBracket`
  - `CoolingAsh`
  - `BrushBundle` or equivalent utility material
- Axis Changes:
  - `Integrity 1 -> 2`
  - `Instability 1 -> 2`
- Outcome:
  - safer, but still mistimed without calibration

#### C. Heat Ledger Recalibration
- Class: Manual-Guided Calibration
- Requirements:
  - `BakeryHeatLedger`
  - `HeatLacquer`
- Axis Changes:
  - `Inscription 0 -> 2`
- Outcome:
  - when paired with bracket/channel repair, restores consistent heat

Reasoning:
- the system is not just a hot rock; it is scheduled infrastructure
- the ledger preserves intended timing and load distribution

#### D. Train The Head Baker
- Class: Stewardship / Training
- Requirements:
  - stable system already restored
  - good standing with bakery district
- Axis Changes:
  - `Stewardship 0 -> 2`
- Follow-up:
  - unlocks periodic good bread stock or hospitality bonus

### BakeryHeatStone Outcome Table

| Integrity | Instability | Inscription | Stewardship | Stage | World Result |
|---|---:|---:|---:|---|---|
| 1 | 1 | 0 | 0 | Failing Heat Stone | cold mornings, poor bread |
| 1 | 2 | 0 | 0 | Rekindled | warmth returns briefly, still erratic |
| 2 | 2 | 0 | 0 | Safe But Mis-Timed | less dangerous, ovens still unreliable |
| 2 | 2 | 2 | 0 | Operational | ovens work, but only while memory holds |
| 2 | 2 | 2 | 2 | Stable Heat Network | bakery district restored |
| 3 | 3 | 3 | 2-3 | Refined Heat Array | superior food output, comfort, prestige |

### Return Effects
- temporary: warm district now, cold again later
- stable: normal bread and district comfort
- improved: special baked goods, favorable district mood, stronger winter resilience

---

## Authored Site Example: WatervineIrrigationNode

### Narrative Role
The settlement’s food-growth infrastructure. This site should connect field productivity, visible crop health, and future scarcity/prosperity.

### Site Identity
- `SiteId`: `WatervineIrrigationNode`
- `SiteType`: `IrrigationNode`
- `ProblemType`: `FlowDisruption`
- `DisplayName`: `watervine irrigation node`
- `LocationHint`: field edge or canal junction

### Initial Diagnosis
The node is underperforming because:
- root intrusion and silt are slowing flow
- canal seals have cracked
- flow-balancing marks have faded
- farmers are compensating manually and poorly

### Initial Axis State
| Axis | Value | Reason |
|---|---:|---|
| Integrity | 1 | canal seals are weakened |
| Instability | 1 | flow is uneven and partially blocked |
| Inscription | 1 | balancing marks are incomplete |
| Stewardship | 1 | farmers know workarounds, not system care |

### Symptoms
- patchy watervine growth
- some beds overwatered, others dry
- reduced produce availability
- subtle anxiety in farmer dialogue

### Available Methods

#### A. Growth Surge / Flow Push
- Class: Expedient Spell
- Requirements:
  - growth or water-moving spell
- Axis Changes:
  - `Instability 1 -> 2` temporarily
- Risks:
  - may later worsen silt or overgrowth if abused
- Outcome:
  - short harvest rescue, not stable repair

#### B. Dredge And Reseal Canal
- Class: Material Reconstruction
- Requirements:
  - `FilterReeds`
  - `SealingClay`
  - `BindingTwine`
- Axis Changes:
  - `Integrity 1 -> 2`
  - `Instability 1 -> 2`
- Outcome:
  - flow physically restored, but still uneven without proper marks

#### C. Reinscribe Flow Marks
- Class: Inkbound Reinscription or Manual-Guided Calibration
- Requirements:
  - `CanalRotationChart` or `IrrigationPrimer`
  - `CanalSealPigment`
- Axis Changes:
  - `Inscription 1 -> 2 or 3`
- Outcome:
  - fields receive correct distribution

Reasoning:
- this site is where inscription can shine without feeling detached from the physical world

#### D. Teach Seasonal Rotation
- Class: Stewardship / Training
- Requirements:
  - stable node already restored
  - relationship with farmers
- Axis Changes:
  - `Stewardship 1 -> 3`
- Outcome:
  - improved resilience against later drought or clogging

### WatervineIrrigationNode Outcome Table

| Integrity | Instability | Inscription | Stewardship | Stage | World Result |
|---|---:|---:|---:|---|---|
| 1 | 1 | 1 | 1 | Failing Irrigation | patchy crops, lower produce |
| 1 | 2 | 1 | 1 | Forced Throughput | short-term field rescue |
| 2 | 2 | 1 | 1 | Reopened Channel | better flow, still uneven |
| 2 | 2 | 2 | 1 | Rebalanced Irrigation | productive but somewhat fragile |
| 2 | 2 | 2 | 3 | Maintained Irrigation | healthy fields, durable output |
| 3 | 3 | 3 | 3 | Flourishing Watervine Network | surplus produce, strong settlement morale |

### Return Effects
- temporary: harvest saved once, later problems recur
- stable: normal field output returns
- improved: produce surplus, better trade stock, visible greener fields

---

## Repair Optionality: Broken Worlds You Can Choose To Heal

This is the right direction.

A lot of games fill their worlds with broken objects purely as environmental storytelling. The player sees decay but rarely gets to answer it. If this game lets the player actually repair that world, it gains a distinctive form of agency.

The key is to avoid turning that agency into obligation.

### The Intended Experience
The player should be able to walk through a damaged settlement and think:
- I could leave this alone
- I could stabilize one thing because people need it
- I could come back later and do it properly
- I could choose to become the sort of person who leaves places better than I found them

That is much stronger than:
- every broken prop is a checklist icon
- the main story requires total restoration coverage
- the player is punished for not becoming a janitor

### Why Optionality Matters

#### 1. It preserves role-playing range
Some players want to be custodians. Some want to be itinerant mages. Some want to solve only the places they care about.

#### 2. It creates emotional authorship
If the player fully restores a town, that should feel like their decision, not compliance.

#### 3. It makes brokenness meaningful
Decay has more weight when the world does not automatically demand that the player erase it.

#### 4. It creates long-term goals without quest-log coercion
A player can gradually build a personal network of restored places over a run.

### Practical Rules For Optional Repair Content

1. Never require 100% repair completion for mainline progression.
2. Let expedient fixes deliver enough value that casual engagement feels worthwhile.
3. Reserve the best long-term outcomes for players who invest deeply.
4. Make repaired places visibly and socially different on return.
5. Avoid cluttering every map with repairables; curate a smaller number of meaningful sites.
6. Keep some broken things permanently decorative so the world does not become a universal maintenance spreadsheet.

### Good World Composition Rule
For any given settlement:
- a few background broken props should remain non-interactive
- a smaller number of meaningful repair sites should exist
- only one or two should usually be in crisis at once

This keeps the world readable.

### Long-Term Identity Payoff
If this system works, the player’s memory of the world changes from:
- “that town with the well quest”

to:
- “that was the village where I fixed the water, ignored the irrigation, and came back in winter to find the bakery still running because I trained the baker properly”

That is the right target.

---

## World Composition: What Should Actually Be Repairable

One of the biggest risks in a repair-heavy game is turning every damaged prop into a low-grade obligation.

The correct target is not:
- every broken object is a multi-step repair system

The correct target is:
- the world feels broadly repairable
- but only a curated subset of broken things are meaningful repair sites

This keeps the fantasy of repair without making the player feel like civilization's universal janitor.

### Four Categories Of Brokenness

#### 1. Decorative Brokenness
Broken objects that are not mechanically repairable.

Examples:
- rubble piles
- shattered carts
- collapsed fences beyond salvage
- cracked urns
- old nonfunctional signage

Purpose:
- environmental history
- tone
- visual scale of ruin
- proof that not every scar in the world is asking for player labor

Rule:
- decorative brokenness should be common
- it should never look like a missed quest marker

#### 2. Light Repairables
Quick fixes with small local payoff.

Examples:
- relighting a lamp
- unsticking a door
- bracing a short walkway
- fixing a hand pump
- retying a pulley line

Purpose:
- lets repair-minded players express care without deep commitment
- provides low-friction utility

Rule:
- one step
- light material or spell cost
- local convenience reward
- little or no persistent site-state complexity

#### 3. Meaningful Infrastructure Sites
These are the core authored repair objects.

Examples:
- `MainWell`
- `BakeryHeatStone`
- `WatervineIrrigationNode`
- gate wards
- district bells
- communal kilns
- archive stones

Purpose:
- carry the real repair system
- justify diagnosis, layered methods, and persistent settlement consequences

Rule:
- only a few per settlement
- clearly readable as important
- strong revisit payoff

#### 4. Major Restoration Projects
Rare, long-horizon, optional projects.

Examples:
- reopening an aqueduct chain
- restoring a bathhouse
- repairing an archive hall
- reactivating a ruined observatory
- rebuilding a district heating network

Purpose:
- support late-game stewardship identity
- transform places in a memorable way

Rule:
- rare
- multi-stage
- never required for basic progression

### Recommended Density

For a normal settlement:
- many decorative broken props
- a handful of light repairables
- `1-3` meaningful infrastructure sites
- at most `1` major restoration project

This is the cleanest way to keep the world feeling repairable without overwhelming the player.

---

## Repair Restores Capacity, Not Just Function

This is the most important moral rule in the whole system.

Repair should be understood as restoring **capacity**.

Sometimes that capacity is obviously good:
- access to clean water
- stable irrigation
- safe communal heat
- road lighting

But sometimes capacity can serve power, coercion, extraction, or violence:
- a prison heat network
- a toll gate winch
- a surveillance bell
- a censor's copying press
- an extraction lift
- a militia signal relay

This matters because it creates a legitimate reason not to engage with every repair prompt.

If repair were always good, declining would feel negligent.
If repair sometimes empowers harmful people or institutions, declining becomes a valid moral choice.

That directly helps with the anti-guilt problem.

The player can think:
- I left that broken on purpose
- I stabilized it temporarily but would not fully restore it
- I took the money because I needed it
- I only repair systems that actually help the settlement

That is much healthier than:
- I saw a broken object and failed the game by not fixing it

---

## Moral Taxonomy Of Repairs

Not every repair job should have the same ethical shape.

### 1. Civic Repairs
Repairs that primarily help ordinary people.

Examples:
- wells
- irrigation
- communal ovens
- clinic wards
- road lamps

Typical rewards:
- trust
- hospitality
- long-term local prosperity
- safer, healthier settlements

These should form the bulk of early repair content.

### 2. Oppressive Repairs
Repairs that clearly empower a bad actor or coercive system.

Examples:
- prison restraint systems
- extortion toll gates
- surveillance bells
- slave-driving quarry lifts
- punitive ward anchors

Typical rewards:
- immediate money
- quick repair experience
- access to rare materials
- faction favor with the patron

Typical downsides:
- local resentment
- stronger oppressive institutions
- later consequences in dialogue, access, or regional feeling

These are valuable because they make repair a roleplaying choice instead of a universal good deed.

### 3. Contested Repairs
Repairs that help some people while also strengthening an institution with mixed or troubling effects.

Examples:
- a landlord-owned grain mill
- a prison heating system that improves prisoner survival but also strengthens the jail
- a watchtower that improves road safety but expands militarized oversight
- an archive press that preserves records but can also help official censorship

These are intellectually strong design material, but they are also more complicated to communicate and balance.

**Current product preference:** do not make contested repairs a major early focus.

Reason:
- they are more nuanced and harder to surface clearly
- they risk muddying the player's understanding of the system too early
- right now the preference is for clearer civic vs oppressive choices

Contested repairs should remain a future design space, not the immediate default.

---

## Immediate Reward Versus Long-Term Benefit

If oppressive repairs are going to exist, the reward structure has to be deliberate.

### Recommended Pattern

Bad patrons should often offer:
- more cash
- faster payout
- better immediate repair XP
- rare components
- simpler transactional dialogue

Civic repairs should often offer:
- slower or smaller direct payment
- trust
- lower prices
- recurring hospitality
- better regional trade
- safer roads
- stronger long-term settlement conditions

This creates a real tradeoff:
- dirty work pays now
- civic work pays later

That is a good structure.

### Important Safeguard

Oppressive repair work must not be the obviously optimal path.

If bad jobs provide:
- the most money
- the most XP
- the best parts
- and no meaningful downside

then the system stops being a moral choice and becomes pure optimization bait.

The player should be allowed to take dirty jobs, but not quietly forced into them by balance.

---

## How Much The Player Should Know Before Choosing

For moral repair choices to work, the game usually needs to communicate:
- who owns the site
- who benefits if it works again
- who suffers if it works again
- whether the site is public, private, coercive, or abandoned

Otherwise the player is not making a choice. They are stepping on hidden narrative traps.

Good communication methods:
- inspect text
- NPC testimony
- faction ownership tags
- local gossip
- visible guards, chains, locks, ledgers, banners, or tax markers
- disagreement between NPCs

Even for simple civic/oppressive content, this communication needs to be strong.

---

## Partial Repairs, Deliberate Limits, And Sabotaged Repairs

Once the system matures, repairs should not be limited to:
- fully repair
- do nothing

Interesting intermediate states include:
- temporary stabilization
- partial restoration
- restoration without optimization
- repair with a built-in cap or weakness
- repair that helps civilians but not the patron fully
- deliberate under-repair or sabotage

Examples:
- restore the prison heat stone enough to keep people alive, but not efficient enough to expand prison capacity
- repair a toll mechanism's safety parts without restoring its revenue counter
- relight watch lamps but leave long-range signal relays broken

This is a strong future direction because it adds morally expressive play without requiring combat.

However, this should be treated as a **later expansion**.

For near-term implementation, the simpler hierarchy is enough:
- ignore
- temporary fix
- stable civic repair
- improved civic repair
- oppressive repair for immediate gain

---

## Anti-Guilt Design Rules

If the repair system is going to fill the world with possible interventions, it needs explicit anti-guilt rules.

### Rule 1
Not every broken thing should be actionable.

### Rule 2
Not every actionable thing should be urgent.

### Rule 3
Not every repair should be morally good.

### Rule 4
The player should be able to walk away from a repair without feeling they have failed the game.

### Rule 5
Main progression should not require broad restoration coverage.

### Rule 6
The game should support a character who fixes only the places they care about.

These rules are important because the emotional fantasy is:
- I chose to care here

not:
- the game assigned me permanent maintenance debt

---

## Recommended Near-Term Scope

For the immediate implementation roadmap, the cleanest shape is:

### Focus On
- civic repairs
- a small number of meaningful infrastructure sites
- clear temporary vs stable vs improved outcomes
- visible settlement change on revisit
- optional engagement

### Include Selectively
- a few clearly oppressive repair jobs with strong immediate payout

### Avoid For Now
- contested repairs as a major content category
- dense networks of morally ambiguous faction infrastructure
- too many partial or sabotage variants

This keeps the system readable while preserving space for richer moral complexity later.

---

## Implementation Guidance

### What to build first
1. MainWell under the full hybrid model
2. BakeryHeatStone using the same axes and method classes
3. WatervineIrrigationNode as the agricultural variant

### What not to build first
- giant universal use-item-on-object system
- dozens of low-value repairables
- highly granular real-world material simulation
- a universal ink-only repair mechanic

### System success criteria
The repair system is succeeding when:
- players can distinguish temporary, stable, and improved outcomes
- repaired sites change dialogue, trade, and environmental feel
- different sites feel distinct even though they share the same underlying axes
- the player can ignore repairs without breaking the game
- players who choose to care about places feel rewarded for doing so
