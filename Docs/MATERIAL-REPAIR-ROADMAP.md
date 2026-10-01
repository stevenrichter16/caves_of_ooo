# Material repairs: development tiers

Status: Tier 1 implemented and reviewed; the audit, final native evidence and accepted limits are tracked in [REPAIR-TIER-1-AND-CROPS.md](REPAIR-TIER-1-AND-CROPS.md). These tiers are development iterations, not character levels or player-facing unlocks. Each iteration must deliver a complete playable loop before expanding complexity.

## Tier 1 — Basic repair

Fire clay, timber and rope repair a cracked well lining, a damaged wooden gate and a snapped well rope. Define simple object composition, one fault per damaged object, explicit repair recipes, safe material consumption, restored function, visible damaged/repaired states and persistent results. Use real materials acquired in ordinary exploration. Start with a well, then prove reuse with the gate and alternative well fault.

## Tier 2 — Material variety

Add mortar, scrap metal, leather, resin and cloth; more repairable objects; material categories and sensible substitutes. Different environments offer different useful supplies. Composition helps select applicable repairs; it does not imply that every ingredient can fix every failure.

## Tier 3 — Tools and workmanship

Add selected tool requirements, relevant skills and a few meaningful quality levels. Basic repairs stay accessible. Temporary patches and lasting repairs should differ in understandable ways. Do not introduce universal maintenance chores.

## Tier 4 — Components and larger projects

Add multiple components/faults, staged work, saved partial progress and mixed-material requirements. Well lining, frame and lifting mechanism can be restored separately. Larger structures may require multiple visits.

## Tier 5 — Repairs within a changing world

Connect selected environmental damage, salvage, NPC repair services and consequences for travel or settlement activity. Damage comes from actual supported events. Repairing a route or service changes its usefulness. This does not promise a universal destruction or economy simulation.

## Stable foundation

- Composition: what an object is made from.
- Fault: what is broken and which function it prevents.
- Recipe: accepted materials, quantities and the resulting repair.
- State: saved on the actual owner; effects and supply consumption commit or roll back together.
- Presentation: visible damage, useful Examine text and a visible restored state.

Keep content definitions separate from action execution. Later tiers can add tools, skills, components and work stages without implementing speculative frameworks in Tier 1. Reuse existing material/damage/inventory/world-state contracts when their actual behavior fits. Preserve existing saves and unrelated content unless a documented migration is justified.

## Completion gate for every tier

Materials are obtainable in normal play; players can understand the damage and requirements; repairs restore real function; materials are consumed exactly once on success; refusal and rollback preserve supplies; save/load and leaving/returning preserve results. Verify through tests, counter-checks, dedicated adversarial review and actual in-game actions. Describe visual/feel limits honestly.
