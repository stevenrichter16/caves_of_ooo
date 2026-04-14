Here’s a clean mechanic framework you can plug into your game:

Core Mechanic: Universal Grab / Throw System
Everyone (player, enemies, allies) can:
Detect nearby throwable objects (logs, rocks, crates, weapons, pots, corpses, etc.)
Attempt pickup based on Strength vs Object Weight
Carry (movement/attack penalties based on load)
Throw with force derived from Strength + object properties
1) Give Every Object a Weight + Tags
Add baseline data to all world objects:
weight (required)
size (small/medium/large)
material (wood, metal, stone, flesh, etc.)
state tags (burning, frozen, electrified, poisoned, brittle, wet, explosive)
This lets one system handle everything without hand-scripting each object.
2) Strength-Gated Pickup + Throw
Use your Strength-equivalent stat (call it Might here):
Pickup allowed if Might >= pickup_threshold(weight)
Throw allowed if Might >= throw_threshold(weight)
Heavier objects can be lifted but maybe not thrown (or thrown very weakly)
Example thresholds
pickup_threshold = weight * 0.8
throw_threshold = weight * 1.0
So an object at weight 20 requires:
16 Might to lift
20 Might to throw properly
3) Throw Force Scales with Strength (Your Key Goal)
You wanted lighter objects to be throwable with more force — perfect.
Use something like:

throw_force = base_force * (Might / weight)^0.7

Results:

Higher Might = harder/faster throws
Lower weight = stronger throws
Heavy objects still possible, but slower/shorter
You can also convert force into:
projectile speed
arc distance
impact stagger/knockback
collision damage
4) Elemental Payload System (Burning Log Example)
Thrown objects carry their current status effects:
Burning log deals impact + fire DoT + ignition chance
Frozen chunk applies slow/freeze buildup
Electrified spear chains lightning on hit
Poison barrel splashes AoE toxin on break
This gives natural cooldown filler:
“Fire grimoire on cooldown? Grab burning debris.”
5) AI Uses It Too (Critical for Fun)
Enemies/allies evaluate throw opportunities with a utility score:
utility = hit_chance * expected_damage * status_value - time_cost - risk

Behavior examples:

Fire-resistant enemies throw burning items less often
Ice enemies avoid standing near campfires
Allies throw disabling objects first (nets, oil jars)
6) Combat Balance Rules
To avoid chaos or exploits:
Wind-up time scales with weight
Stamina cost for pickup/carry/throw
Carry speed penalty for heavy objects
Limited throw angle indoors
Friendly fire optional by difficulty
Object durability (some break on impact)
7) UX / Feedback
Make it readable and satisfying:
Outline throwable objects in range
Show “Too heavy” reason text
Arc preview for held object
Distinct hit reactions by weight (light flinch vs heavy knockdown)
Elemental VFX transfer (embers trailing from burning log)
8) Progression Hooks
Turn this into a full build path:
Strength perks:
quick-lift
power throw
heavy-object specialist
ricochet throw
Magic synergy:
imbue held object with current spell school
Companion command:
“Fetch and throw nearest burning object at ice targets”
9) Simple Formula Starter Pack
If you want quick implementable values:
can_pickup = Might >= weight * 0.75
can_throw = Might >= weight * 1.0
speed_penalty = clamp(weight / (Might+1), 0, 0.35)
throw_velocity = 8 + 12 * (Might / (weight + 5))
impact_damage = weight * throw_velocity * 0.12
stagger = impact_damage * target_stagger_mod
element_damage = object_status_power * status_multiplier(target_type)
For your example:
Burning log vs ice enemy gets a status multiplier bonus (e.g. x1.5 fire effectiveness).