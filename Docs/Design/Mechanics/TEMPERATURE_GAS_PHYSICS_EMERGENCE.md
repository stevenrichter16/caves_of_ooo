# Caves of Qud: Emergent Systems from Temperature, Gas, and Physics

> Synthesis of how Qud's `Physics.cs`, `Gas.cs`, and related gas behavior parts produce emergent gameplay.
> Based on analysis in `Docs/Design/Mechanics/COMBAT_AND_PHYSICS.md`, `Docs/Design/Mechanics/STATUS_EFFECTS.md`, and direct inspection of decompiled source in `qud-decompiled-project/`.

## Core Thesis

Qud's temperature and gas systems feel emergent because they are modeled as world simulation primitives rather than as one-off status effects. Temperature is a continuous property on objects. Gas is a density-bearing object that propagates through space. Physics then turns continuous values into discrete states such as aflame, frozen, vaporized, confused, or coated in plasma through thresholds and event hooks.

The result is that a single change in world state can cascade across multiple systems:

- local temperature changes become area heat spread
- area heat spread becomes ignition, extinguishing, or vaporization
- vaporization can become a gas cloud
- gas density affects damage, saving throws, perception, and AI navigation
- AI movement and player movement then change because the environment has changed

This is the core of the emergence: the game does not script a situation-by-situation answer. It lets a few generic simulation rules interact.

## The Simulation Primitives

### 1. Temperature is a continuous scalar on objects

Every object with `Physics` carries a temperature, ignition threshold, vapor threshold, freeze threshold, brittle threshold, and `SpecificHeat`.

This matters because the system is not binary. An object is not simply "burning" or "not burning." It can be warming, retaining heat, radiating heat, crossing thresholds, or overshooting thresholds by different amounts. That overshoot directly matters for burn damage.

Key consequences:

- `SpecificHeat` makes some objects thermally inertial and others thermally volatile.
- Heat and cold resistance alter how the same environmental input affects different actors.
- Ambient return means temperatures drift over time rather than snapping back.

### 2. Temperature changes use two different physical modes

Qud distinguishes between:

- **Radiant change**: asymptotic movement toward a source temperature
- **Direct change**: additive temperature change scaled by `SpecificHeat`

This is one of the biggest sources of emergence.

Radiant change is used for environmental spread, so hot or cold zones create gradual fields rather than instant state flips. Direct change is used for attacks and strong effects, so deliberate actions can force thresholds faster than the environment can.

That difference creates a real tactical distinction between:

- standing near danger
- stepping into danger
- being directly hit by danger

### 3. Heat spreads spatially

When an object's temperature differs enough from ambient, `UpdateTemperature()` radiates to all 8 adjacent cells. That means hot objects are not isolated hazards. They become local emitters that push thermal state into nearby cells and nearby objects.

This creates:

- fire propagation without bespoke fire-spread rules per object
- hot spots that punish clustering
- cold zones that gradually freeze an area instead of just one target

The environment starts behaving like a field, not a list of independent tiles.

### 4. Gas is a density field implemented as objects

A gas cloud is a real game object with:

- `Density`
- `Level`
- `Seeping`
- `Stable`
- `GasType`
- creator attribution

Each turn, gas can:

- lose density
- spread to nearby cells
- bias its spread using wind
- merge with same-type gas
- dissipate when thin enough

So gas is neither a static decal nor a fixed-duration aura. It is a moving, thinning, combining resource in the map.

### 5. Discrete states come from thresholds and hooks

Physics turns continuous temperature into important thresholds:

- `FlameTemperature` -> aflame
- `BrittleTemperature` -> frozen
- `VaporTemperature` -> vaporized

Gas turns density into effect strength:

- higher density means more damage, stronger status application, or higher AI avoidance
- lower density means dissipation and weaker tactical pressure

The event layer makes these thresholds extensible:

- `BeforeTemperatureChangeEvent`
- `AttackerBeforeTemperatureChangeEvent`
- `CanTemperatureReturnToAmbientEvent`
- `FrozeEvent`
- `ThawedEvent`
- `VaporizedEvent`
- gas behavior parts reacting on `TurnTick` and `ObjectEnteredCellEvent`

That hook structure is why special cases can be added without breaking the general model.

## Emergent Systems That Fall Out of These Rules

### 1. Fire becomes a self-propagating ecology

Fire is not just a damage-over-time effect. It is the visible result of:

- objects retaining heat differently because of `SpecificHeat`
- nearby cells receiving radiant heat
- ignition at `FlameTemperature`
- burn damage scaling by temperature overshoot in six tiers
- optional positive feedback from tags like `HotBurn`

This means a fire can intensify, spread, stall, or die out depending on material properties, resistances, proximity, and ambient context.

Emergent result:

- a hot object can become a local engine that heats neighbors
- neighbors may ignite at different times depending on their thermal properties
- the same room can behave very differently depending on what objects are in it

This produces "fire stories" rather than "fire effects."

### 2. Cold becomes spatial control, not just debuffing

Cold uses the same shared temperature system, but its strategic outcome is different. Cryo effects do not merely deal cold damage; they push objects toward freeze and brittle thresholds, which can lead to the `Frozen` state and all the mobility loss that comes with it.

Emergent result:

- cryogenic effects can reshape movement tempo by freezing actors in place
- cold zones punish repeated exposure rather than just a single bad roll
- an actor's resistances and thermal mass change whether a cloud is a nuisance or a disabling threat

Because thawing is event-driven too, cold creates temporal gameplay: what is frozen now may re-enter the fight later.

### 3. Vaporization links the temperature model to the gas model

One of the strongest bridges in the whole simulation is vaporization. When an object crosses `VaporTemperature`, it can die and spawn a `VaporObject`, often a gas.

That means a temperature event can create an air-control event.

Emergent result:

- heat can transform a local object problem into an area-denial problem
- matter changes phase and remains tactically relevant after the original object is gone
- kill attribution is preserved through creator tracking, so downstream gas hazards still belong to someone

This is a major reason the system feels coherent: fire, vapor, and gas are not separate minigames.

### 4. Gas clouds become dynamic territory

Gas propagation is density-based, wind-biased, and mergeable. That turns gas into temporary map geometry.

A cloud is effectively a region with:

- soft boundaries
- directionality from wind
- changing hazard intensity from density
- pathfinding implications

Emergent result:

- airspace itself becomes contested terrain
- the same corridor can be safe, risky, or impassable depending on gas thickness and wind
- gas placement matters more in narrow architecture, while open spaces create diffusion and drift stories

Because gas merges, separate sources can produce one stronger hazard field without a custom script that says they should.

### 5. AI sees the same hazard field the player sees

Gas parts publish navigation weights, and those weights depend on gas type, gas density, actor resistances, respiration, and smartness. Density changes flush navigation caches, so AI can react to a cloud becoming denser or thinner.

This is crucial for emergence.

If gases only harmed the player, they would be traps. Because they also alter pathfinding, they become tactical terrain.

Emergent result:

- clouds can herd or repel enemies
- resistances create asymmetric routes for different factions or builds
- the environment produces behaviors that look intentional even when they are just rule-consistent

Steam, cryo gas, confusion gas, and plasma each produce different route valuations, so the same map can host multiple overlapping hazard topologies.

### 6. Gas types are specialized without being separate systems

Each gas type uses the same propagation and density logic, but applies a different consequence:

- **Steam** converts density into heat damage against organic targets
- **Cryo gas** converts density into direct cooling plus cold damage
- **Confusion gas** turns inhalation performance and density into saving throws and confusion
- **Plasma gas** applies `CoatedInPlasma`, making future elemental harm much worse

This is exactly the kind of design that produces emergence: one transport layer, many payloads.

Emergent result:

- players learn a common grammar for clouds, then discover type-specific consequences
- combined hazards become richer without requiring new simulation code
- the same delivery rules can support lethality, control, sensory disruption, and debuffing

### 7. Resistance, physiology, and phase create actor-specific worlds

Not every actor experiences the same thermal or gaseous environment the same way.

- Heat resistance changes how quickly heat matters.
- Cold resistance changes how quickly freezing matters.
- Respiration matters for inhaled gases.
- Phase matching gates whether one object can thermally or gaseously affect another.
- Some effects or parts can block or modify temperature return to ambient.

Emergent result:

- one cloud may be irrelevant to one creature and catastrophic to another
- one actor can use a zone that another actor must avoid
- "the battlefield" is not the same battlefield for every participant

This creates asymmetric tactics from shared rules instead of scripted faction traits.

### 8. The event layer makes the simulation combinatorial

The event hooks around temperature change and state transitions are what allow the system to combine with the rest of Qud.

Objects and effects can:

- amplify or suppress incoming temperature changes
- prevent ambient re-equilibration
- react to freezing, thawing, or vaporization
- radiate heat through their own parts
- alter gas dispersal if they created the cloud

This means the core temperature/gas rules are not isolated. They are composable.

Emergent result:

- new mutations, items, or environmental objects can create surprising behavior without new top-level systems
- small local rules stack into novel outcomes
- designers can add content by plugging into the simulation rather than rewriting it

## Concrete Emergent Patterns

### Thermal Cascade

1. An object is heated directly past a meaningful threshold.
2. It remains above ambient and radiates to adjacent cells.
3. Nearby objects warm asymptotically and cross their own thresholds at different times.
4. Some ignite, some resist, some vaporize, some merely become dangerous to approach.

This is why fires and heat zones can feel organic rather than deterministic.

### Atmosphere as Battlefield

1. A gas source creates a cloud with meaningful density.
2. Wind and geometry alter where the gas goes.
3. Same-type clouds merge, increasing local pressure.
4. Density changes damage, save DC pressure, and AI avoidance.
5. The fight's navigable space changes in real time.

This is area denial emerging from propagation and density, not from a hard-coded "hazard tile."

### Thermal-to-Aerial Conversion

1. Heat pushes an object to vaporization.
2. Vaporization spawns a gas object.
3. That gas now disperses, merges, and affects navigation and creatures.

This creates a clean material chain:

`temperature change -> state transition -> new airborne hazard -> movement and status consequences`

### Build-Specific Hazard Interpretation

1. A cloud fills a hallway.
2. Each actor evaluates it through resistances, physiology, and AI smartness.
3. Different actors choose different routes or suffer different outcomes.

This is one of the strongest examples of Qud's emergence: the same simulated event generates different local truths for different participants.

## Why This Feels Different from a Typical RPG Status System

A typical status system says:

- fire means burning damage
- cold means slow or freeze
- gas means a debuff aura

Qud instead says:

- temperature is a number that moves through space and time
- gas is a material density that occupies cells and reacts to wind and geometry
- thresholds and events convert these continuous values into states

That inversion is the source of the emergence. States are outputs of simulation, not the primary unit of design.

## Design Takeaways

1. Continuous values create richer outcomes than binary tags.
2. Spatial propagation turns personal status into environmental gameplay.
3. Density is a strong generic carrier for both damage and area control.
4. Thresholds make simulation legible to players without simplifying the underlying model.
5. Event hooks are what let content scale without rewriting the core system.
6. AI integration is mandatory if environmental systems are meant to feel real.

## Source Notes

Primary references used for this synthesis:

- `Docs/Design/Mechanics/COMBAT_AND_PHYSICS.md`
- `Docs/Design/Mechanics/STATUS_EFFECTS.md`
- `qud-decompiled-project/XRL.World.Parts/Physics.cs`
- `qud-decompiled-project/XRL.World.Parts/Gas.cs`
- `qud-decompiled-project/XRL.World.Parts/GasSteam.cs`
- `qud-decompiled-project/XRL.World.Parts/GasCryo.cs`
- `qud-decompiled-project/XRL.World.Parts/GasConfusion.cs`
- `qud-decompiled-project/XRL.World.Parts/GasPlasma.cs`
- `qud-decompiled-project/XRL.World.Effects/Burning.cs`
