# Composable world systems

Standing user direction, November16,2026 (America/Chicago). This is a central design philosophy for the whole game: machines, NPCs, objects, their components, behaviors and effects.

The user clarified the overarching purpose on November23: an eye-widening sense
of wonder as players discover how much of the world the equipment, mechanics
and items let them engage with. Favor discoveries that expand what a player can
reasonably attempt. Learning one capability should suggest useful applications
across travel, inhabited places, creatures, materials and lasting projects.
Content quantity is valuable when it produces distinct opportunities and
connections through shared rules, rather than repeated isolated interactions.

Let a discovery lead somewhere: an unusual merchant item can suggest a project;
a completed project can enable an activity; familiar equipment can acquire a
new practical use in that activity. Preserve experimentation, observable
consequences and multiple approaches. Descriptions, readable inventory categories
and previews should help players recognize these possibilities without spelling
out every combination or claiming unsupported behavior. The examples express a
design aim; each supported interaction still needs implementation and actual play.

Latest user refinement, November23: develop numerous connected mechanics with
medium-depth nuance. Finish meaningful correctness and actual-play gates for
each increment, then expand the useful surface instead of repeatedly deepening
one instance. Share screenshots at visible progress points.

November 26 user refinement: use Orven's response to a player's porter
sighting as a benchmark for depth. A player can tell him where they saw
the overdue porter alive. In the Wayside playtest he changed direction,
checked that spot, and resumed the search. The parcel stayed in transit.
Look for similar links between ordinary player actions and visible world
behavior wherever the mechanics support them. Verify the change during
play, including what persists when the player leaves or reloads.
[Wayside playtest](Verification/MachineSystems/WaysideLivingPlayerWitness20261126/README.md)

November24 user refinement: avoid predetermined town/road projects becoming
the same checklist on every playthrough. Let different builds and tools solve
similar practical problems through shared causal rules. Moving a useful load
may justify hiring, carrying smaller batches, hauling, supported processing at
source or an optional access improvement. Baseline services remain useful
without first completing a project. Vary meaningful local circumstances and
available work; do not merely reskin a required tool or reward one scripted
method. A lasting project is an investment the player may choose, not a
mandatory repeated goal or upkeep obligation. Seeded procedural roads and
movable loads can vary those circumstances within validated access, placement,
finite-stock and lore constraints; generated sites persist without reroll/refill.

Build on the existing Entity/Part/event model. Capabilities and persistent instance state belong to composable Parts; shared rules and services coordinate their interactions. New content should participate through those capabilities and contracts. Routine additions of behaviors or effects should not require editing every interacting system or scripting each entity pairing.

Players should be able to reason from the world. With suitable materials, components, objects, machines, knowledge and physical access, an interaction that makes sense should have a consistent supported path. Apply the same implemented rules throughout the world, including combinations and situations the designers did not author individually. Preserve useful alternatives, understandable costs, ownership and consequences. Avoid single-solution tool puzzles and progression padding.

Separate definitions from instances: definitions describe reusable material/process/behavior contracts; instance Parts hold particular condition, quantities, modifications, ownership and other saved state. Common events and small explicit capability interfaces let new behavior attach at the relevant instance or component. Do not grow one machine class, NPC type or scenario switch into the owner of all behavior.

Extensions must preserve causal accounting. A modifier that changes a process's cost or yield contributes before the validated preview and payment. Veto hooks can refuse before commitment; completion effects occur only after commitment. Resource quantities, physical custody, interruption and save/reload remain authoritative. No effect may silently bypass payment, create duplicate outputs, reveal protected information or erase distinct component state.

Use explicit adapters for incompatible state rather than assuming every object can be consumed or transformed safely. New behavior can supply its own validated policy without changing the transaction core. Missing support should be an honest, scoped implementation limit. Material tags alone do not invent recoverable stock; an authored finite manifest or supported constituent model supplies that evidence.

Prove the architecture through small playable increments: more than one participating instance/location, a meaningful ordinary alternative, a useful interaction outside the new apparatus, and a cross-system consequence. Check that extensions apply through Parts/contracts instead of hidden site-specific permission. Add abstractions when real behavior requires them; a universal simulation framework is not a prerequisite.

The immediate implementation priority remains reclamation and regional processing. This principle guides that work and later systems; it does not reactivate the cancelled graphics program or parked capstan/culture implementation.

## Job and mission rewards

User direction, November 26, 2026. Completing a job should noticeably advance
the player. Give enough money to fund a practical next purchase or a meaningful
share of a larger project, useful materials the player can put to work, or both.
Calibrate the scale against the goods surrendered, travel, risk and consumables
and against what nearby merchants actually charge. A short local job and a risky
expedition can make different contributions to progress.

Use existing recipes, repairs and services as destinations for material rewards.
Communicate payment before acceptance and record what was actually received.
Keep comparable jobs and alternate solutions economically coherent, and check
that delivery, stock and payment survive reload and settle once.

[Economy design and initial balance comparison](ECONOMY-SYSTEM.md#job-and-mission-rewards)

## Weather and seasonal opportunities

User direction, November23,2026: weather should primarily make selected
activities more rewarding. Skipping an opportunity should usually mean missing
a bonus while ordinary exploration stays comfortable. Preserve baseline travel,
gathering, crafting and services; do not introduce routine exposure or upkeep
chores to justify equipment. Lasting improvements should keep their benefits.

Some of the same tools should acquire different practical uses as seasonal
materials and conditions change. An observable flexible stem, ripe seed head or
fallen branch can suggest a different work choice. Support the interaction
through actual material state, finite quantities, access and explicit
capabilities; a calendar label or combat attribute is not work permission.
Learning a tool should suggest applications elsewhere, including useful town
services and portable counterparts where supported.

Player direction, November 26. Environmental opportunities should be visible
on an ordinary walk near home or farther afield. A change in the source
or surrounding terrain tells the player that something is different.
Examine can explain the material and the available work. Verify that
the visible change follows the same state as the actual opportunity.

The [weather design](WEATHER-AND-SEASONAL-TOOL-USES.md) records concrete proposed
uses, regional discovery, resource and timing policies, and staged acceptance.
Bounded W1a collection, manual watering and natural rain on exposed beds have
Editor evidence. Wider seasonal reuse remains open. Observe both taking a
bonus and comfortably ignoring it during ordinary exploration.
