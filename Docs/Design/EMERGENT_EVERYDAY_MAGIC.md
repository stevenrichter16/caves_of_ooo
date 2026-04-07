# Emergent Gameplay Through Environmental Properties and Everyday Magic

## Design Philosophy

Emergence in Caves of Ooo comes from one architectural rule: **systems don't know about each other.** Temperature doesn't have special cases for oil. Oil doesn't have special cases for fire. When oil meets fire, the generic temperature system checks combustibility, finds a high value, and ignites it. No combo was authored. The result was emergent.

Every new system built this way multiplies the possibility space of every existing system. The more systems that interact through shared generic properties rather than special-cased interactions, the more emergence the game produces.

This principle extends to magic: **everyday spells don't have "effects." They modify environmental properties on cells.** The game's simulation systems react to those property changes. The spells are mundane because their modifications seem trivial. The emergence comes from other systems reading those property changes and producing consequences the spell designer never anticipated.

---

## The Shared Property Layer

Every cell in the zone has environmental properties that simulation systems read from. Everyday spells (and other game actions) write to these properties. The properties are the bridge between systems -- the shared language that enables emergence without special-casing.

### Cell Environmental Properties

| Property | What It Represents | Systems That Read It |
|----------|-------------------|---------------------|
| **Organic Matter** | Plant life, debris, biological material | Temperature (combustibility), creature AI (food source), decomposition over time |
| **Moisture** | Dampness, humidity on surfaces | Electrical conductivity, fire resistance, liquid spread rate, plant growth |
| **Scent** | Airborne chemical presence | Creature AI (attraction/repulsion by type), tracking, detection radius |
| **Airflow** | Wind/breeze intensity and direction | Gas dispersal direction/speed, fire spread direction, scent propagation distance, sound carry |
| **Sound Level** | Ambient noise at a cell | Creature AI (alert radius), stealth modifiers, masking other sounds |
| **Light Modifier** | Brightness delta from ambient | FOV/visibility, creature behavior (photophobic/photophilic creatures), plant growth rate |
| **Structural Integrity** | How solid terrain/objects are | Whether walls crack, floors collapse, objects break under stress |
| **Ground Cover** | What's physically on the floor | Movement speed, slip chance, concealment, combustion fuel |

### Design Rules for the Property Layer

1. **Properties are generic.** They don't reference specific spells, items, or creatures. "Organic matter" doesn't know it came from a flower spell -- it's just a float value.
2. **Properties propagate.** Scent spreads to adjacent cells over turns. Moisture seeps. Temperature conducts. Airflow carries other properties in its direction.
3. **Properties decay.** Without active maintenance, organic matter decomposes, moisture evaporates, scent fades, sound dissipates. Nothing persists forever without a source.
4. **Systems read properties, not spell IDs.** The temperature system checks the combustibility derived from organic matter + moisture. It never asks "was this created by Field of Flowers?"

---

## Everyday Spells as Property Modifiers

Each everyday spell is defined entirely by which properties it modifies, by how much, and over what area. The spell itself has no knowledge of what consequences those modifications will produce.

### How Spells Map to Properties

**Field of Flowers**
- +high organic matter
- +moderate moisture
- +moderate scent (pollen)
- +light ground cover
- +minor light modifier (colorful)

What the player sees: a pretty field of flowers. What the simulation sees: combustible, moist, fragrant, concealing ground cover. The spell didn't "do" anything tactical.

**Shake Leaves from Trees**
- +moderate organic matter (dry leaves, low moisture)
- -concealment on tree cells (canopy thinned)
- +ground cover (loose debris)
- +minor sound (rustling, brief)

Dry leaves are MORE combustible than flowers (organic matter but no moisture). Ground cover provides concealment for small creatures. Thinned canopy reveals anything hiding in trees. Rustling briefly increases awareness radius of nearby creatures.

**Gentle Breeze**
- +airflow in a chosen direction
- No direct property changes to cells

The subtlest spell. Breeze doesn't change any cell's properties directly. It changes how properties MOVE between cells. Gas clouds drift downwind. Fire spreads faster downwind. Scent carries further downwind. Sound carries further downwind. Feels useless. One of the most powerful shaping tools in the game.

**Warm Hearth**
- +temperature in a small radius

Just heat. But heat thaws frozen liquids, dries out moisture (reducing fire resistance of organic matter over time), and creates a temperature gradient that creature AI might respond to (cold-blooded creatures seek warmth).

**Clean Surface**
- Removes liquids from cells
- Removes ground cover
- Removes organic matter
- Zeroes out scent

The "janitor spell." Seems pointless. But it removes the oil slick before someone lights it. Removes the water puddle before someone electrifies it. Removes the scent trail before the predators follow it. A hard counter to environmental setups -- yours or the enemy's.

**Muffle**
- -sound level in an area (dampening zone)
- Minor -airflow (sound and air are related)

Stealth enabler. But also: you can't hear enemies in the muffled zone either. And reduced airflow means gas clouds linger instead of dispersing. You've created a silent gas trap.

**Create Spring**
- +liquid (water) to a cell
- +moisture to surrounding cells
- +ambient sound (trickling)

A decorative fountain. Also: a conductivity path for electricity. A source of liquid for the liquid physics system to spread. A sound source that might mask footsteps OR attract thirsty creatures.

**Dry Wind**
- +airflow
- -moisture in area

Dries things out. Wet organic matter becomes dry organic matter. Dry organic matter is extremely combustible. Puddles evaporate. Damp clay walls might crack when dried rapidly (reduced structural integrity).

**Brighten / Dim**
- +/- light modifier in area

Brighten reveals hidden things and makes photophobic creatures flee or become agitated. Dim provides concealment and makes photophilic creatures avoid the area. Light affects plant growth rates -- a Brightened field of flowers might grow and spread over many turns, increasing organic matter.

---

## Emergence Chains: How "Useless" Spells Cascade

These examples require NO special-case authoring. Every consequence follows from generic system rules reading generic properties.

### The Fire Corridor

1. Cast **Field of Flowers** in a corridor. Cells gain organic matter, moisture, scent, ground cover.
2. Cast **Dry Wind** through the corridor. Moisture drops to zero. Organic matter remains but is now dry.
3. Dry organic matter has maximum combustibility in the temperature system.
4. An enemy pyromancer walks down the corridor and casts a fire spell. Temperature system checks combustibility, finds dry organic matter, ignites it.
5. Fire spreads through connected dry organic matter (the flower field). Airflow from Dry Wind is still active -- fire spreads FASTER in the airflow direction.
6. The corridor is now a fire tunnel.

The player used a flower spell and a wind spell to create a fire trap.

### The Electric Puddle

1. Cast **Create Spring** near a doorway. Water pools in the cell.
2. Cast **Gentle Breeze** toward the door (carries the trickling sound further, masking approach).
3. Enemy approaches through the door. Steps in water.
4. Any electricity source (spell, trap, environmental) chains through the water's conductivity.
5. The breeze carries the spring sound further down the hallway, potentially luring more enemies toward the water.

### The Ecological Cascade

1. Cast **Field of Flowers** near a settlement. Scent increases.
2. Scent attracts pollinator creatures (bees, butterflies -- harmless).
3. Pollinator creatures attract insectivore creatures (something dangerous).
4. Dangerous creatures near the settlement trigger the settlement's defenders.
5. The player caused a fight between wild creatures and the settlement without lifting a sword. Faction consequences depend on whether the settlement blames you.

### The Silent Gas Trap

1. Cast **Muffle** on a room. Sound drops, airflow drops.
2. Deploy a gas source (poison cantrip, gas grenade, natural vent).
3. Normally, gas disperses over a few turns via airflow. But Muffle reduced airflow -- the gas lingers.
4. Enemies enter the room. They can't hear the gas hissing (muffled sound). The gas doesn't disperse (no airflow).
5. By the time they realize what's happening, they've been breathing poison for several turns.

### The Structural Collapse

1. Cast **Dry Wind** on damp cave walls (high moisture + moderate structural integrity).
2. Rapid drying causes clay/mud walls to crack (-structural integrity from moisture loss).
3. Cast **Shake Leaves from Trees** or any vibration/impact effect near the weakened wall.
4. Wall collapses, creating a new passage or crushing creatures underneath.
5. The player broke through a wall using a drying spell and a leaf-shaking spell.

---

## Why This Feels Like Frieren's Everyday Magic

The Frieren philosophy: "someone spent 80 years perfecting a spell that makes a field of flowers, and everyone laughed, and then it saved the world."

In this system, **the same spell is useless or game-changing depending on the player's understanding of the simulation.** A new player casts Field of Flowers and thinks "that's cute." A player who understands combustibility, scent propagation, and creature AI casts Field of Flowers and thinks "I'm setting up a kill corridor."

The spells don't change. The player's simulation literacy changes. Mastery is expressed through mundane tools, not flashy ones. The mage who spent 3 turns preparing the environment with "useless" spells wins more decisively than the one who spent 3 turns casting fireballs.

### What Makes Everyday Spells Distinct from Combat Spells

1. **No direct damage, healing, or status effects on creatures.** They modify environment only.
2. **Their property modifications are indirect.** Flowers don't hurt anyone. They add organic matter.
3. **Their usefulness depends entirely on what other systems are present.** Flowers are useless in a void. Flowers in a world with combustion physics, creature AI, and scent propagation are a multi-tool.
4. **They feel useless in isolation.** Their power is combinatorial. This is by design.

---

## Discoverability

The biggest risk with emergent systems is that players never discover the interactions exist. Solutions:

### In-Game Discovery Mechanisms

- **Accidents.** The player casts Field of Flowers for fun, a torch-carrying enemy walks through, and the flowers ignite. The player didn't plan it -- but now they know organic matter burns.
- **NPC hints.** Dialogue from knowledgeable NPCs: "Be careful casting that flower spell near open flames, traveler." Faction-specific tips based on their expertise.
- **Look/Inspect system.** Examining a cell shows its properties: "Ground: flowering plants (organic matter: high, moisture: moderate, combustibility: moderate)." Players who use Look mode learn the property language.
- **Cause-and-effect log.** When chain reactions occur, the message log traces the cascade: "The dry flowers ignite. The fire spreads along the airflow. The snapjaw is engulfed in flames." This teaches the player what happened and why.

### What NOT to Do

- **Don't add a "combo list" or recipe book.** The moment players can look up "flowers + dry wind + fire = corridor trap," it stops being emergent and becomes a checklist.
- **Don't add special-case interactions.** If flowers + fire doesn't work through the generic combustibility system, don't add a special "flowers burn" rule. Fix the combustibility system instead.
- **Don't make everyday spells secretly powerful.** They should be genuinely weak in isolation. Their power must come from combination with other systems, not from hidden multipliers.

---

## Implementation Architecture

### Property Storage

Each `Cell` gains an `EnvironmentProperties` struct/component:

```
Cell.OrganicMatter    (float, 0-1)
Cell.Moisture         (float, 0-1)
Cell.Scent            (float, 0-1, + ScentType enum)
Cell.Airflow          (Vector2, direction + magnitude)
Cell.SoundLevel       (float, 0-1)
Cell.LightModifier    (float, -1 to +1)
Cell.StructuralIntegrity  (float, 0-1)
Cell.GroundCover      (float, 0-1, + CoverType enum)
```

### System Integration Points

Existing systems need to READ these properties (not write special cases):

| System | Property It Reads | What It Does With It |
|--------|------------------|---------------------|
| Temperature | OrganicMatter, Moisture | Computes combustibility: `organic * (1 - moisture)`. High combustibility = ignites at lower temperatures. |
| Liquid Physics | Moisture, Airflow | Moisture affects spread rate. Airflow direction biases liquid flow. |
| Creature AI | Scent, SoundLevel, LightModifier | Modifies detection radius, attraction/repulsion behaviors. |
| Gas System | Airflow | Gas clouds move in airflow direction. Low airflow = gas lingers. |
| Fire Spread | OrganicMatter, Moisture, Airflow | Fire jumps to adjacent cells with high combustibility. Airflow accelerates spread in its direction. |
| Stealth | SoundLevel, LightModifier, GroundCover | Player visibility/audibility derived from cell properties. |
| Structural | StructuralIntegrity, Moisture | Walls/floors with low integrity can collapse from impacts. Rapid moisture change reduces integrity. |

### Everyday Spell Definition Format

Each spell is a data definition, not code:

```
SpellID: "FieldOfFlowers"
TargetType: Area (radius 3)
Duration: 200 turns (then organic matter decays)
PropertyModifications:
  - OrganicMatter: +0.7
  - Moisture: +0.4
  - Scent: +0.5 (type: Pollen)
  - GroundCover: +0.3 (type: Vegetation)
  - LightModifier: +0.1
```

No behavior code. No special cases. Just property deltas. The simulation does the rest.

### Property Propagation (Per-Turn Update)

Each turn, a lightweight pass updates property propagation:

- **Scent** spreads to adjacent cells, attenuated by distance. Airflow carries it further in one direction.
- **Moisture** seeps to adjacent cells slowly. Evaporates faster in high temperature / low humidity.
- **Sound** decays rapidly (1-2 turn radius unless sustained by a source).
- **Organic matter** decays slowly over many turns (decomposition). Grows slowly if Light + Moisture are high (plant growth).
- **Airflow** is generally static once set (breeze spell lasts N turns, then fades).

This propagation step is the engine of emergence -- it's what makes a flower spell in one cell eventually affect cells 5 tiles away through scent drift.

---

## Open Questions

1. **Property UI.** How much of the property layer is visible to the player? Full transparency (show all values) rewards system mastery but is information-dense. Selective transparency (show properties only when relevant, e.g., "this area smells of pollen") is more approachable but might hide important information.

2. **NPC spell use.** Can NPCs cast everyday spells? An NPC alchemist who casts Clean Surface on their shop floor, or a druid who maintains a Field of Flowers around their grove, would make the world feel alive. Enemy mages using everyday spells tactically (Muffle before ambush, Dry Wind before fire attack) would teach the player by example.

3. **Spell interactions with the grimoire system.** Everyday spells are the canonical cantrips -- memorized permanently, weak in isolation, always available. They can't be sigiled (cantrips can't). But a Shaping grimoire with Sigil of Expansion casting a bound terrain spell over a field of flowers would amplify the property modifications. The everyday spell sets up the environment; the bound spell exploits it.

4. **Property persistence across zone transitions.** Do environmental modifications persist when the player leaves and returns to a zone? Persistent modifications enable long-term environmental strategy (pre-shape a zone before a boss fight). Non-persistent keeps zones fresh but undermines the "patient mage" fantasy.

5. **How many everyday spells is enough?** Each spell should modify a UNIQUE combination of properties. If two spells modify the same properties in the same direction, one is redundant. The current set (Field of Flowers, Shake Leaves, Gentle Breeze, Warm Hearth, Clean Surface, Muffle, Create Spring, Dry Wind, Brighten/Dim) covers 9-10 spells across the property space. More can be added as new properties or property combinations are identified during playtesting.

6. **Can non-spell sources modify properties?** Rain increases moisture. Campfires increase temperature and light. Creature movement increases sound. Rotting corpses increase organic matter and scent. If the property layer is generic enough, EVERYTHING in the world feeds into it, not just spells. This dramatically increases emergence but also complexity.
