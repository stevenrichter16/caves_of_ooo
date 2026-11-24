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

Build on the existing Entity/Part/event model. Capabilities and persistent instance state belong to composable Parts; shared rules and services coordinate their interactions. New content should participate through those capabilities and contracts. Routine additions of behaviors or effects should not require editing every interacting system or scripting each entity pairing.

Players should be able to reason from the world. With suitable materials, components, objects, machines, knowledge and physical access, an interaction that makes sense should have a consistent supported path. Apply the same implemented rules throughout the world, including combinations and situations the designers did not author individually. Preserve useful alternatives, understandable costs, ownership and consequences. Avoid single-solution tool puzzles and progression padding.

Separate definitions from instances: definitions describe reusable material/process/behavior contracts; instance Parts hold particular condition, quantities, modifications, ownership and other saved state. Common events and small explicit capability interfaces let new behavior attach at the relevant instance or component. Do not grow one machine class, NPC type or scenario switch into the owner of all behavior.

Extensions must preserve causal accounting. A modifier that changes a process's cost or yield contributes before the validated preview and payment. Veto hooks can refuse before commitment; completion effects occur only after commitment. Resource quantities, physical custody, interruption and save/reload remain authoritative. No effect may silently bypass payment, create duplicate outputs, reveal protected information or erase distinct component state.

Use explicit adapters for incompatible state rather than assuming every object can be consumed or transformed safely. New behavior can supply its own validated policy without changing the transaction core. Missing support should be an honest, scoped implementation limit. Material tags alone do not invent recoverable stock; an authored finite manifest or supported constituent model supplies that evidence.

Prove the architecture through small playable increments: more than one participating instance/location, a meaningful ordinary alternative, a useful interaction outside the new apparatus, and a cross-system consequence. Check that extensions apply through Parts/contracts instead of hidden site-specific permission. Add abstractions when real behavior requires them; a universal simulation framework is not a prerequisite.

The immediate implementation priority remains reclamation and regional processing. This principle guides that work and later systems; it does not reactivate the cancelled graphics program or parked capstan/culture implementation.

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

The [weather design](WEATHER-AND-SEASONAL-TOOL-USES.md) records concrete proposed
uses, regional discovery, resource/timing policies and staged acceptance. These
are design extensions, not claims that natural weather or seasonal tool uses
already exist. Qualify both taking a bonus and comfortably ignoring it, and
observe ordinary exploratory routes rather than only directed recipe tests.
