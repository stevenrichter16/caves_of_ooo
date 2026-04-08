# Caves of Qud: Emergent Systems from AI Environmental Responses

> Synthesis of how Qud's `Brain.cs`, AI goal handlers, gas navigation weights, healing-location hooks, and faction feeling logic produce emergent behavior.
> Based on `Docs/Design/Mechanics/NPCs.md`, `FACTION_SYSTEM.md`, `Docs/Design/Mechanics/COMBAT_AND_PHYSICS.md`, `Docs/Design/Mechanics/STATUS_EFFECTS.md`, and direct inspection of decompiled source in `qud-decompiled-project/`.

## Core Thesis

Qud's AI environmental responses feel emergent because the game does not give most creatures a single hardcoded survival script. Instead, it combines:

- a perception layer
- a LIFO goal stack
- environment-derived scores such as navigation weight, liquid cooling, and healing-location value
- social feeling derived from factions plus personal opinions
- behavior parts that can interrupt normal action selection

That combination lets the same creature react differently based on what it can perceive, how intelligent it is, what the map currently contains, what hazards are in the air or on the ground, and who is socially involved.

The result is not "AI with one state machine." It is a set of overlapping response systems that produce believable survival and group behavior from local rules.

## The Response Primitives

### 1. Perception is broader than line of sight

`Brain.CheckPerceptionOf()` accepts three channels:

- visibility
- audibility
- smellability

That matters because Qud's environmental AI is not purely visual. Sound propagation and olfaction exist as zone-level spatial systems, so awareness can travel through the map differently from sight.

There is one important nuance: the default `FindProspectiveTarget()` path is still vision-led, because it begins from `FastCombatSquareVisibility()`. So the system is not "everything is equally sensed." It is better described as:

- target acquisition is primarily visibility-based
- the general perception layer still supports hearing and smell
- other AI checks and interactions can use those broader senses

This already creates more environmental variety than a pure LOS combat AI.

### 2. The goal stack makes reactions composable

`Brain` runs the top goal on a `CleanStack<GoalHandler>`, so behavior is naturally interruptible and layered.

That is why environmental responses can coexist instead of overwriting each other:

- `Bored` can push `ExtinguishSelf`
- `AISelfPreservation` can clear goals and push `Retreat`
- `Retreat` can temporarily push `Flee`
- movement goals can be inserted under or over existing goals

This matters for emergence because the AI does not need one monolithic "survival state." Environmental pressures can inject short-lived priorities into a shared execution model.

### 3. The world advertises affordances instead of hardcoded destinations

Several systems expose the environment as scored opportunities:

- gases publish navigation weights
- liquids expose cooling values
- cells expose healing-location values through `PollForHealingLocationEvent`
- factions and opinions expose social risk through `GetFeeling()`

The AI is therefore not reacting to special named tiles alone. It is reacting to values and hooks attached to the world.

That lets very different content participate in the same behavior:

- a healing liquid
- a bed
- a chair
- a safe empty tile
- a low-density gas corridor

All of them can be evaluated by generic logic without a bespoke planner per object type.

### 4. Social feeling is layered, not binary

`Brain.GetFeeling()` combines:

- weighted faction allegiance
- per-faction overrides
- personal opinions
- leader inheritance
- hostile/calm flags

`AllegianceSet` computes base feeling as a weighted average over faction memberships, while `OpinionMap` stores personal history like attack, theft, trespass, or ally-killing incidents.

This means the "environment" the AI responds to is not just physical. It is also social. A room can become dangerous because of gas, because of fire, or because someone nearby just stole from an ally.

## Emergent Systems Produced by These Rules

### 1. Environmental pressure becomes priority inversion

Because behavior parts can interrupt `AITakingAction`, environmental danger can immediately outrank whatever the creature was already doing.

Examples:

- `AISelfPreservation` clears the current stack and pushes `Retreat`
- `AISeekHealingPool` clears the stack and pushes `MoveTo` toward a healing location
- `Bored` pushes `ExtinguishSelf` as soon as the actor is on fire

This creates believable reprioritization without a central planner.

Emergent result:

- patrol behavior can abruptly become survival behavior
- pursuit can collapse into retreat
- idle creatures can suddenly exploit terrain features that were previously irrelevant

### 2. Fire response scales with intelligence, not just with status

`ExtinguishSelf` is one of the clearest examples of emergent environmental response.

A burning actor does not simply run a single extinguish action. The goal handler checks intelligence and then explores multiple options:

- if intelligent enough, look for a nearby wading-depth pool with strong cooling
- if smart enough, try to douse using carried liquid containers
- attempt generic firefighting actions
- if still needed, search farther for a pool and move toward it

It also ranks nearby pools by:

- liquid cooling value
- path distance

This means two creatures on fire in the same zone can behave differently based on intelligence, inventory, flight state, and nearby liquid composition.

Emergent result:

- water, slime, or other cooling liquids become implicit AI safety infrastructure
- smarter creatures appear more "resourceful" without a hand-authored intelligence script
- inventory and terrain combine into survival behavior

### 3. Survival behavior happens in tiers, not all at once

Qud splits wounded behavior into at least two meaningful stages:

- `AISeekHealingPool` triggers once HP penalty reaches its `Trigger` threshold
- `AISelfPreservation` triggers later and more aggressively by forcing `Retreat`

With the defaults in the decompiled code:

- `AISeekHealingPool.Trigger = 0.35f`
- `AISelfPreservation.Threshold = 35`

These do not describe the same moment in the fight. The healing-pool seeker responds once HP penalty reaches 35% of base HP, while `AISelfPreservation` pushes retreat at roughly 35% HP remaining. One behavior starts looking for recovery while the other treats the situation as critical.

Emergent result:

- some creatures disengage early to recover
- others fight longer and only break when near collapse
- the same creature can first route toward healing terrain and later fall back to broader retreat behavior

This creates a survival curve instead of a single panic switch.

### 4. Retreat is strategic; flee is tactical; panic is degraded strategy

Qud distinguishes between `Retreat` and `Flee`, and that distinction is where a lot of environmental subtlety appears.

`Retreat`:

- searches up to 30 cells out
- filters to empty, visible, reachable, low-danger cells
- scores cells by navigation weight, healing-location value, and distance from enemies
- prefers cells farther from enemies and better for recovery
- tries retreat abilities before pathing
- uses healing locations when it reaches them

`Flee`:

- is shorter-horizon
- moves away from a specific target
- normal mode still prefers lower-danger escape cells
- panic mode drops to a cruder rule: move directly away or pick a random adjacent escape option
- panic mode also stops trying retreat abilities first

This is a strong design move. Qud gives AI both:

- a deliberative escape mode
- a degraded emergency scramble

Emergent result:

- wounded creatures sometimes look tactically clever
- panicked creatures look messy and frightened
- pathfinding quality changes with state, which makes fear and injury visible in motion

### 5. Hazards become pathfinding fields, not just damage sources

Gas behaviors such as `GasConfusion` publish `GetNavigationWeightEvent` and `GetAdjacentNavigationWeightEvent` values. Those values scale with density and whether the actor is smart, unbreathing, or otherwise affectable.

That means gas is not just something that hurts when entered. It reshapes route selection before entry.

For confusion gas specifically:

- smart AI increases avoidance as density rises
- adjacent cells also become less attractive
- density changes flush navigation caches, so route choice updates as the cloud evolves

Emergent result:

- dynamic gas clouds create moving soft walls
- some actors route around them, others accept them, and others ignore them
- the same room changes from open space into constrained topology as gas thickens

This is one of the main ways environmental response turns into emergent positioning.

### 6. Healing terrain becomes a shared ecological resource

Healing is not tied to one hardcoded object type. Cells answer `PollForHealingLocationEvent`, and multiple systems can respond:

- liquids via `LiquidVolume`
- beds
- chairs
- potentially anything else with a healing-location implementation

`Retreat` uses healing-location value in cell scoring, and `AISeekHealingPool` looks for cells that advertise healing pools.

This means healing is environmental, not just inventory-based.

Emergent result:

- restorative objects and liquids become attractors in combat space
- wounded creatures converge on the same areas, creating secondary conflict zones
- a room's furniture and fluids can matter tactically even before the player interacts with them

This is a good example of Qud making level dressing mechanically active.

### 7. Social incidents propagate like environmental contagion

The faction and opinion layer turns social conflict into another form of local pressure field.

When a creature is attacked or killed:

- `AIHelpBroadcastEvent.Send()` propagates the incident in a radius
- nearby minds evaluate the victim, attacker, and cause
- allied or affiliated observers can add personal opinions such as `OpinionAttackAlly` or `OpinionKilledAlly`
- theft and trespass likewise create `OpinionThief` and `OpinionTrespass`
- if the new feeling state crosses hostility thresholds, observers can decide to kill in response

The important point is that this is not a simple faction toggle. It depends on:

- current feeling
- party membership
- shared faction
- allegiance level
- personal opinion history

Emergent result:

- one assault can snowball into a local multi-actor brawl
- murder, theft, and trespass can produce different escalation patterns
- social spaces behave differently depending on who saw what and how they already felt

This is "environmental response" in the social sense: the nearby social graph behaves like reactive terrain.

### 8. Weighted allegiances create ambiguous loyalties

Because `AllegianceSet` computes a weighted average, actors are not forced into a single categorical allegiance. A creature can partially belong to multiple factions, and its resulting base feeling is blended.

Emergent result:

- mixed-membership actors can evaluate the same target differently from pure members
- crowd reactions are less uniform
- allegiance changes can shift behavior without rewriting the AI

This is why Qud can support social weirdness such as temporary loyalties, conversions, summoned allies, or faction overlays without collapsing into brittle special cases.

### 9. Perception, hazard, and social memory can all stack at once

The strongest emergent behaviors come from overlap, not from any one subsystem alone.

A creature can:

- perceive a hostile through sight, sound, or smell checks
- decide that a gas cloud makes the shortest route unattractive
- already hold a personal grudge from earlier friendly fire or trespass
- be low enough on health to prioritize healing terrain
- be on fire and interrupt everything to extinguish itself

Those are not separate state machines. They are concurrent pressures routed through one goal stack.

Emergent result:

- identical species can behave differently in the same combat
- local context produces behaviors that look authored even when they are only rule-consistent
- environmental AI feels situational rather than generic

## Concrete Emergent Patterns

### 1. The Burning Scholar and the Burning Beast

Two creatures catch fire.

- the smarter one searches for nearby cooling pools, may pour a carried liquid on itself, lands if necessary, and then routes toward a better pool
- the duller one is more likely to rely on immediate firefighting attempts and less likely to convert the terrain into a survival plan

The difference is not a handcrafted species script. It is intelligence-gated access to the same extinguishing logic.

### 2. The Wounded Creature in a Toxic Corridor

A creature is hurt, a healing pool exists elsewhere in the zone, and confusion gas fills the shortest hallway.

- `AISeekHealingPool` or `Retreat` makes the creature want restorative terrain
- gas navigation weights make the direct path more expensive
- if a safe retreat point can be scored, the creature may route around the gas
- if things get worse, it may stop being strategic and fall back to short-horizon `Flee`

This produces behavior that looks like caution, desperation, and collapse in sequence.

### 3. The Theft That Becomes Terrain

The player steals from or trespasses near a mixed-faction group.

- observers process the event through allegiance and opinion
- some remain neutral
- some add negative personal opinions
- some cross the hostility line and attack
- once combat starts, more observers can respond to assault or ally-killing broadcasts

The room's social composition becomes as tactically important as its walls and liquids.

### 4. The Healing Nook

A zone contains a bed, a chair, and a healing liquid.

None of these need a custom "AI safe room" tag. They simply publish healing-location value. As combat unfolds:

- wounded AI may drift toward that nook
- retreating AI may score it as better than an ordinary empty tile
- other actors may follow or contest it

The map develops a dynamic center of gravity from generic restorative hooks.

## Design Takeaways

1. A goal stack is stronger than a giant AI state enum when many environmental responses need to interrupt one another.
2. Publishing world affordances as scores or events lets new content participate in AI behavior without planner rewrites.
3. Distinguishing strategic retreat from panic retreat makes fear and injury visible in movement.
4. Social memory can be treated like environmental state if it is local, additive, and broadcast-driven.
5. Weighted allegiances produce better crowd behavior than one-faction labels.
6. Hazard-aware navigation is one of the fastest ways to make AI feel like it inhabits the world instead of merely occupying it.

## Source Notes

Primary references used for this synthesis:

- `Docs/Design/Mechanics/NPCs.md`
- `FACTION_SYSTEM.md`
- `Docs/Design/Mechanics/COMBAT_AND_PHYSICS.md`
- `Docs/Design/Mechanics/STATUS_EFFECTS.md`
- `qud-decompiled-project/XRL.World.Parts/Brain.cs`
- `qud-decompiled-project/XRL.World.AI.GoalHandlers/ExtinguishSelf.cs`
- `qud-decompiled-project/XRL.World.AI.GoalHandlers/Flee.cs`
- `qud-decompiled-project/XRL.World.AI.GoalHandlers/Retreat.cs`
- `qud-decompiled-project/XRL.World.Parts/AISelfPreservation.cs`
- `qud-decompiled-project/XRL.World.Parts/AISeekHealingPool.cs`
- `qud-decompiled-project/XRL.World.Parts/GasConfusion.cs`
- `qud-decompiled-project/XRL.World.AI/AllegianceSet.cs`
- `qud-decompiled-project/XRL.World.AI/OpinionMap.cs`
