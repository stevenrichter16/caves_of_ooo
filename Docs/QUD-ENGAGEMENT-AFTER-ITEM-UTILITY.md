# Engagement after the 35 item passes

Status: source investigation complete, 2026-10-08, against CoO `f239fb81b` and the available local full Qud decompile. Recommendations below are proposed work, not implemented changes. The 35 item passes and native evidence are recorded in [ITEM-UTILITY-35-PASSES.md](ITEM-UTILITY-35-PASSES.md).

## Research prompt used

Read both actual codebases. Identify what makes obtainable objects, ordinary encounters and preparation produce choices. Credit CoO features already implemented. For each remaining gap, give a concrete player situation, exact source evidence, bounded change, drawback, and counter-check. Do not equate more commands, copied identities, or a universal optimizer with engagement. Prefer composing existing mechanics. Distinguish source facts, design inferences and proposed implementation.

## Findings and corrections

The most useful next investment is making existing capabilities encounter one another in legible situations. CoO already has 19 nonempty Spread exploration families, persistent local outcomes, historical travel reports, six Sodden formations and three connected Sumphold destinations. Enemies already use finite medicine, recover weapons and select supported skills. Players now have 35 more situational item uses. None of these should be rebuilt or counted as a fresh gap.

| Initial possibility | Verified correction | Resulting decision |
|---|---|---|
| Missing ecological/composite encounters | Grazing, hunting, territory and a connected wet-crossing satellite exist. | Pilot compatibility between two existing roles at a few ordinary sites. Preserve quiet chunks and finite budgets. |
| Missing danger-aware pathfinding | A* already consumes actor-specific terrain costs; direct approach, retreat and firing-position shortcuts do not consistently use them. | Connect those bounded local decisions to the existing cost model. |
| Missing useful travel notes | Notes already exist and explicitly distinguish reports from current truth; family keys overwrite a second same-family site. | Add instance-specific evidence only when a concrete expedition needs it. |
| Missing liquid use/collection | Vessels, coatings, surface films and physical material verbs exist. | Favor bounded cleanup and delivery tools, not another payload catalogue. |
| Qud's sprayer proves ranged spraying | The cited reference applies liquid to carried/equipped objects. | A short-range world applicator would be CoO-original design. |
| Qud AI is an optimal planner | Its command lists weight entries and make bounded random attempts. | Learn contextual eligibility and real payment, not perfect counterplay. |
| Qud retreat proves hidden-threat fairness | The cited flee path reads a live target position. | Extend CoO's stronger observed-pursuit discipline deliberately, rather than claiming parity. |

Detailed evidence, scenarios, risks and acceptance criteria:

- [Item composition](Verification/QudEngagementAfterItemUtility/ItemComposition.md): 41 checked source links; cleanup, delivery and conditional preparation.
- [Combat decisions](Verification/QudEngagementAfterItemUtility/CombatDecisions.md): 42 checked source links; actual capability selection, movement costs and escape information.
- [Exploration](Verification/QudEngagementAfterItemUtility/Exploration.md): 56 checked source links; role combinations, regional identity, expedition clues and social stakes.

## Recommended delivery order after the requested Sodden art pass

### 1. Make placing a hazard influence a real opponent

Use existing actor-specific terrain costs in local movement choices. Add one opt-in enemy carrying one real defensive supply, with actor-aware feedback and NPC perception independent of player FOV. A guard spending its water to extinguish itself loses an attack opportunity and becomes electrically vulnerable. Leftover supplies remain real loot. Couple this to one readable encounter, not an upgrade for every enemy.

Acceptance: native whole-goal red/green tests for ordinary/immune actors, alternative/forced routes, paid/empty supplies and hidden targets. Show a real encounter in Play, including the player laying an obstacle and the opponent's resulting choice. No perfect avoidance or free counters.

### 2. Compose two ordinary exploration problems and one useful clue chain

Pilot water + hunt and claimed passage + salvage with a shared population/reward budget and a traversable bypass. Use the existing composite satellite as the control. Tie one actual equipment/material lead to a particular persisted site, retain multiple same-family notes, and keep historical report versus local observation explicit. No remote stock inspection or fabricated reward.

Acceptance: seed comparisons with secondary pressure disabled, protected arrivals, finite owners/resources, at least two materially different effective solutions, and revisits preserving literal outcomes. Keep quiet places quiet. Native checks establish outcomes; human playtesting is needed for surprise and appeal.

### 3. Complete a small item interaction loop

Start with capped, paid removal of eligible body coatings using existing absorbent pith; preserve Wet, internal poison and unrelated burning. Then vet one repairable liquid-delivery tool with limited compatible contents and a discoverable works source. Finally pilot one conditional meal, one saved charge, and the existing one-preparation slot. Do not attempt all three as an inseparable framework.

Acceptance: exact real costs, partial/full cleanup, veto/rollback, visibility/hostility, conservation, expiry and save/load. Every new affordance must have an authored ordinary-play use. General liquid mixtures remain deferred until a reclamation destination justifies their model and migration cost.

## Later expansion and stopping criteria

Regional material/hazard/reward profiles can link two nearby sites once the composite pilot is perceptibly different. Exceptional loot owners can acquire a small number of visible personal faction relationships and one peaceful transfer route later. Neither needs a new universal simulation or quest framework. If a pilot merely adds enemies to the same fight, revise its geometry, incentives and information before adding more variants.

## Review and honesty bounds

- Root spot-checks confirmed direct movement bypasses weighted pathfinding, the family-keyed note ledger, Qud command weighting and the sprayer target restriction.
- ⚪ This is reference-informed CoO design. No Qud code, art, enemies, lore or dialogue has been copied.
- 🧪 Source reading cannot establish actual Qud feel or current retail behavior. No Qud runtime test was performed, and no new CoO runtime behavior ships with this report.
- The user's later visual request takes precedence in delivery order: finish an original Sodden 3D sprite presentation before these future gameplay pilots. This research does not authorize claiming those pilots completed.

## Implementation log / files

2026-10-08: compared three bounded source surfaces in parallel, checked conflicting premises against current CoO, synthesized this ordered roadmap. Files added: this document and the three linked research reports. No production/test changes in this milestone; existing item verification is unchanged.
