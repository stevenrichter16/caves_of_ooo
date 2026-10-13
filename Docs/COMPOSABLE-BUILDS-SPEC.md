# CavesOfOoo Composable Builds Implementation and In Game Test Specification

## Contents

- [Purpose and status](#purpose-and-status)
- [Design goals and boundaries](#design-goals-and-boundaries)
- [Existing mechanics that this design must respect](#existing-mechanics-that-this-design-must-respect)
- [Prototype decision register](#prototype-decision-register)
- [Counterfighter skill specification](#counterfighter-skill-specification)
- [Progression and probability](#progression-and-probability)
- [Displacer design and candidate gates](#displacer-design-and-candidate-gates)
- [Encounter specifications](#encounter-specifications)
- [Readability and player feedback](#readability-and-player-feedback)
- [Opening expeditions and acquisition](#opening-expeditions-and-acquisition)
- [Ordered implementation milestones](#ordered-implementation-milestones)
- [Technical implementation contract](#technical-implementation-contract)
- [Validation and playable acceptance plan](#validation-and-playable-acceptance-plan)
- [Open decisions and implementation handoff](#open-decisions-and-implementation-handoff)
- [Additional source references](#additional-source-references)
- [Design rationale from the Qud comparison](#design-rationale-from-the-qud-comparison)

Version 1.0 • 12 October 2026 • Design and implementation handoff

## Purpose and status

This document consolidates the Counterfighter, Displacer, hybrid-build, encounter, readability, progression and opening-expedition design discussed for **CavesOfOoo, the Unity/C# game in `stevenrichter16/caves_of_ooo`**. It is not for the older JavaScript repository `stevenrichter16/CavesOfOoo`.

The goal is to build skills that are useful individually, create recognizable fighting styles together, and combine with other disciplines into worthwhile hybrid or chimera builds. Their main tradeoff is foregoing other genuinely useful purchases. The world, enemies, NPCs, geometry, resources and opportunities must make those alternatives valuable.

**This deliverable is a specification, not an implementation or test report.** No game code was changed and no Unity tests or playtests were run while preparing it. Source observations are distinguished from proposed behavior. Creating this document does not authorize publishing, merging or deploying game changes.

### How to interpret requirements

- **Existing baseline:** behavior found in the inspected repository or its implementation records. Recheck on the checkout before editing.
- **Prototype requirement:** recommended behavior for the first implementation, subject to the acceptance gates below.
- **Provisional balance value:** a reproducible starting value for experiments, not a claim of measured balance.
- **Candidate extension:** implement only after its comparison gate shows a worthwhile distinction from existing skills.
- **Open decision:** resolve and record before the dependent implementation; do not silently choose an incompatible behavior.

The broad design direction is user-endorsed. Exact percentages, the reaction cap, starter loadout and candidate skills are proposals, not individually finalized balance decisions. Where this spec supplies an engineering default beyond the earlier discussion, it is explicitly a recommendation to make the prototype testable.

### Source baseline

The latest reviewed main revision is [`244177619cffae76fb452cce40e3c49cc5dfcb2f`](https://github.com/stevenrichter16/caves_of_ooo/commit/244177619cffae76fb452cce40e3c49cc5dfcb2f), including the full-screen Skills Field Manual and preceding opening revision [`bf47a5651a0e9ecaae9dbdab3fb8817f1d34e80f`](https://github.com/stevenrichter16/caves_of_ooo/commit/bf47a5651a0e9ecaae9dbdab3fb8817f1d34e80f). Pin test evidence to the actual implementation revision, not merely to `main`.

Before implementation, inspect checkout instructions, current changed files and the actual Unity project. Preserve unrelated work. If main has moved, produce a small baseline-difference note before applying this design. Source links in this document are navigation aids, not permission to overwrite newer behavior.

## Design goals and boundaries

### The player decisions we want

A player should repeatedly decide:

1. Whether to hold a valuable position, leave it, or change the enemy's position.
2. Whether to spend an action preparing a defense or use that action offensively.
3. Whether to spend a finite item, wait for an opportunity, use terrain, or retreat.
4. Whether the next skill point should increase reliability, introduce a new action, or connect two disciplines.
5. Whether an encounter serves their current purpose at all.

A build has an identity when these decisions differ, not merely when its damage number differs. A hybrid must preserve real choices rather than automatically execute several complete builds' responses at once.

### Principles to preserve

- Skills interact through precise shared events such as a full parry or successful forced movement. Do not equate different events because their animations look similar.
- No special penalty for being a hybrid. Existing skill costs, equipment used by the actual ability, action economy, range, legal destinations and foregone skills supply tradeoffs.
- A strong combination is welcome. A cheap combination that answers almost every situation must be investigated.
- Ordinary walking, cover, items and learned Vault belong in every comparison. Do not make baseline actions worse to manufacture value for a new skill.
- A nonviolent bypass, partial harvest, safe passage or successful withdrawal can be a good outcome.
- NPCs and creatures retain ordinary risks and rules. Avoid invulnerability, perfect hazard knowledge, invisible coordination bonuses or a required solution disguised as freedom.
- Readability exposes trustworthy evidence, not omniscient enemy plans.
- This first milestone is bounded. It is not a full new class system, a global AI rewrite or a complete rebalance of CavesOfOoo.

### Not included in the first implementation

Do not add a universal next-turn prediction system, every-attack windups, blanket mass-based movement resistance, respec, new global hunger pressure, generic faction reputation, compulsory quests, a new economy, automatic old-world content replacement, or many additional ranks simply to fill a skill tree. The earlier creative association with “Rootbound” remains an independent, unadopted theme idea; this spec does not establish it as lore or a mechanic.

## Existing mechanics that this design must respect

| Existing element | Relevant contract | Design consequence |
|---|---|---|
| En Garde | Active long-blade guard; two owner turns; halves one positive adjacent melee blow, rounding up; cooldown 12 | Deepen its defense outcome without accidentally retaining the old halving hook twice |
| Short Blades Rejoinder | Chance of a counter after an incoming natural miss, using eligible Piercing equipment | A new parry is not a miss and must not accidentally activate Rejoinder |
| Long Blades Follow Through | Existing active sword strike; on kill attempts ordinary movement into the victim's former cell | Preserve its name and behavior. The proposed displacement-following passive is **Claim Ground** |
| Expertise and Lunge | Accuracy and a range-two sword attack without movement | Real alternatives to buying more passive reliability or branching into water control |
| Jet Blast and Undertow | Existing pushes/pulls with Wet and associated cooldowns | Reuse them; do not introduce duplicate generic shove/pull skills |
| Cudgel Slam | Weapon-specific force and collision consequences | Preserve as the heavy-impact option, not a universal skill granted to sword users |
| Acrobatics Dodge and Tumble | Dodge gives +2 DV; Tumble swaps with an adjacent creature and can briefly confuse a hostile target | New precision maneuvers must justify replacing these existing purchases |
| Committed melee | Stored origin and fixed attack direction; real windup/recovery; displacement cancels commitment into recovery | Interruption is already a consequence of successful movement; do not add a second stun as an automatic bonus |
| Forced movement | Legal body placement, bracing checks, entry reactions and slip processing | A move can trigger a real trap or liquid contact; “free movement” never means hazard immunity |
| Current skills | Roots/powers generally cost one SP; duplicate skill types are not a ranked progression system | Ranking needs a deliberate schema, purchase, load and UI integration |
| Skill books | Can teach an ability outside ordinary purchased-root requirements | A learned Vault does not grant the Acrobatics root |
| Current opening | Fresh cellar supplies Vault manual, two clay and seed-dependent book/buckler; older saved content retained | Keep common earned mobility equal in tests and do not retrofit old worlds |

See the source map later in this file for code integration points. The existing current En Garde description sometimes uses “parry” for partial mitigation; the new UI must distinguish **full Deflect** from **Guarded partial damage**.

## Prototype decision register

| Decision | Recommended first treatment | Status |
|---|---|---|
| Deflect ranks | 15%, 22%, 28%; one SP each | Provisional values |
| Riposte ranks | 45%, 65%, 80% after a full parry; one SP each | Provisional values |
| En Garde | Adds 30 percentage points to the same parry roll; has 30% without Deflect; failed parry retains half damage | Prototype rule; numerical bonus provisional |
| Duration and cooldown | Preserve two owner turns and cooldown 12 initially | Starting baseline, not an uptime guarantee |
| Guard consumption | First eligible would-hit strike consumes it whether parried or reduced; natural miss does not | Prototype rule requiring event-order changes |
| Counter limit | Initially one Riposte attack attempt between defender turns; compare against uncapped | Experiment, not final rule |
| Reaction overlap | No two counter systems responding to the same incoming strike; no counter chains | Correctness requirement |
| New Reposition | Sideways one-cell adjacent enemy movement; no damage or extra stun | Candidate; compare with Tumble first |
| New Claim Ground | Optional one ordinary free step into original vacated cell after own active displacement | Candidate; compare with Dodge/Tumble and preserve existing Follow Through |
| Starter preset | Optional ordinary long blade plus root and En Garde, priced against current starter packages | Candidate, exact whole kit unresolved |
| XP rate | Preserve existing progression initially | Scope boundary |
| New-world encounter roles | Opt-in authored yard/forge role behavior, preserving population/source ownership | Proposed content slice; old cached graphs remain literal |

## Counterfighter skill specification

### Identity

“I want to be here when you attack.” The character accepts exposure in exchange for defending a valuable position and potentially retaliating. It is not a universal answer to ranged attacks, environmental damage or every attack in a crowd.

### Deflect

**Kind:** passive, three prototype ranks. **Equipment:** eligible equipped long blade. **Trigger:** an eligible adjacent melee strike has passed its normal accuracy resolution and would otherwise enter the hit-resolution path.

Requirements for a prototype parry include a living defender able to react, a valid usable weapon and legal body adjacency. Recommended rule: the defender must be able to perceive the attacker through the game's combat perception rules; do not use the human player's camera/fog state as a substitute for an NPC's perception. Resolve blind/unseen-attacker behavior explicitly and test it before shipping.

Use exactly one defense roll per eligible strike. Do not roll Deflect against an ordinary miss. Success prevents that strike's damage and hit-triggered consequences, including effects that normally require successful weapon contact. It does not erase previously existing burning, bleeding, poison or an independent environmental event.

Deflect does not require En Garde or Riposte. It continues to have defensive value while the player attacks, uses an item or performs another action. Additional ranks improve reliability, not range, area coverage or the number of reaction attacks.

### En Garde

**Kind:** existing active skill, deepened. Preserve existing equipped-long-blade requirements, paid activation, two owner-turn duration and cooldown 12 for the first prototype.

Let the passive parry probability be `p` (zero if Deflect is not learned). While a qualifying En Garde is active, use `p + 0.30`, clamped defensively to a valid probability. This is a single roll, not independent guard and Deflect rolls. The prototype maximum is 58% with Deflect III.

A failed parry applies the existing half-damage rule, rounding the remaining positive damage up. A successful parry fully prevents this strike. Consume the guard on the first qualifying would-hit strike, before reaction execution; a natural miss, irrelevant ranged hit or independent status tick must not spend it.

For armor that would reduce a connecting blow to zero, this spec recommends that the qualifying would-hit defense check still occur and consume the guard. This follows the newly proposed would-hit semantics rather than the old positive-damage-only rule. It is a documented prototype decision to test, not an assertion about existing behavior. Ensure the implementation does not consume twice when downstream damage processing later observes zero.

Without Deflect, En Garde still supplies its own base parry chance and fallback protection. Without Riposte, it remains a useful defensive action. One guard protects one qualifying strike, not an entire multiweapon sequence.

**Timing:** retain the current owner-turn lifecycle until tests establish the exact activation/expiration points. Two owner turns is not two enemy actions or a fixed world-time duration. A speed change, stun or save/load must not silently extend/restart the effect. Never advertise a guaranteed defense of “the enemy's next attack.”

### Riposte

**Kind:** passive, three prototype ranks. **Trigger:** a full parry caused by an eligible parry source. Initially Deflect and En Garde are qualifying sources. A generic natural miss, reduced damage, armor absorption, an enemy missing after displacement, or successful interruption is not a parry.

After a parry, if the reaction is legal, roll the learned Riposte probability. A successful roll creates one ordinary attack with a selected eligible long blade against the actual attacker. Apply normal accuracy, penetration, damage and appropriate on-hit rules; this attack may miss. Do not invoke an action that sweeps every weapon or grants a free lunge. Revalidate the attacker, defender, range, body positions, weapon ownership and action eligibility before execution.

Prototype limit: one issued Riposte attack attempt between the defender's turns. Missing with the counter uses the allowance. A failed Riposte chance roll does not issue an attack and does not spend the attack allowance; subsequent eligible parries may roll until one attack is issued. This is a recommended precise interpretation for testing. Therefore the simple per-strike probability examples below apply only while the allowance is available. Preserve the allowance across saves and do not reset it through menu queries, equipment changes or failed commands.

Riposte depends on a usable parry source, not specifically on purchasing Deflect first. The UI should explain the dependency and warn when the current character cannot trigger it. Do not silently add a mandatory Deflect prerequisite that invalidates a guard-first or future equipment-granted build. If purchase remains allowed without a source, require a clear warning and preserve ordinary cancellation without cost.

### Reaction safety and overlap

Use an explicit attack/reaction origin so a counter cannot generate another counter or turn an existing extra-attack hook into recursion. An incoming strike must be claimed by at most one reactive counter response. The exact weapon/response selection policy must be deterministic and documented; preserve legacy Rejoinder behavior when the new skills are absent.

The design does not authorize accidentally nerfing every existing reaction by installing an undocumented global cap. The proposed per-turn cap applies to the new Riposte prototype; any broader policy must be an explicit separately tested change. Same-strike exclusion and recursion protection apply wherever needed to prevent duplicated responses to the new event.

## Progression and probability

### Prototype rank data

| Skill | Rank I | Rank II | Rank III | Purchase cost |
|---|---:|---:|---:|---|
| Deflect | 15% | 22% | 28% | One SP per rank |
| Riposte after a full parry | 45% | 65% | 80% | One SP per rank |
| En Garde bonus | +30 percentage points | Not ranked initially | Not ranked initially | One SP, plus existing foundation requirements |

The complete package costs eight purchased points when nothing is already granted: one Long Blades root, three Deflect ranks, one En Garde and three Riposte ranks. The introductory root plus three skills costs four. These totals are not character-level promises because starter grants and learned abilities vary.

### Probability examples

For an otherwise-successful eligible enemy strike, while a counter allowance is available:

`P(counter attempt) = P(full parry) × P(Riposte succeeds | full parry)`

- Rank I passive package: `0.15 × 0.45 = 6.75%`.
- Rank I under En Garde: `0.45 × 0.45 = 20.25%`.
- Rank II passive package: `0.22 × 0.65 = 14.3%`.
- Rank III passive package: `0.28 × 0.80 = 22.4%`.
- Rank III under En Garde: `0.58 × 0.80 = 46.4%`.

Across all incoming attacks, multiply by the enemy's probability of connecting. For counters that deal damage, account for the counter's own accuracy, penetration and damage resolution. Guarded probabilities are not sustained uptime. Do not add percentages or present a counter attempt as guaranteed damage.

Dodge may reduce parry opportunities because it prevents the strike earlier. This can still be stronger survival. Counter count alone is an invalid measure of build quality.

### Routes and equal-budget comparisons

Guard-first sequence: root → En Garde → Riposte I → Deflect I. Passive-first: root → Deflect I → En Garde → Riposte I. These are suggestions, not forced prerequisite chains.

Use six spent SP, identical gear, attributes, world stage, consumables and earned manuals for paper/controlled comparison:

| Loadout | Six purchased entries | What it forgoes |
|---|---|---|
| Counterfighter | Long Blades, Deflect I, En Garde, Riposte I, Expertise, Lunge | Water control and further passive ranks |
| Sword and water hybrid | Long Blades, Deflect I, En Garde, Riposte I, Hydromancy, Jet Blast | Expertise accuracy and stationary Lunge reach; not the base defensive package |
| Rank-heavy defender | Long Blades, Deflect I and II, En Garde, Riposte I and II | Two different capabilities in exchange for reliability |
| Existing control benchmark | Hydromancy, Jet Blast, Undertow, Acrobatics, Dodge, Tumble | Sword reaction skills |
| Candidate precision variant | Hydromancy, Jet Blast, Undertow, Acrobatics, Reposition, Claim Ground | Dodge and Tumble; assumes new powers belong under Acrobatics at one SP each |

The candidate precision placement/cost is a test assumption, not a final tree decision. Give all comparison characters sufficient attributes for the real prerequisites; the existing Acrobatics powers require Agility 15. A free learned Vault does not grant a free Acrobatics root. Hydromancy's root supplies an actual moisture benefit, so do not model it as a meaningless entry tax.

## Displacer design and candidate gates

### Identity and reuse

“I want you somewhere else before you attack.” The result is only useful if the new position is better. A shove that exposes a firing lane, places the player beside a threat or protects a target from a planned fire attack can be a poor move.

Existing options:

- **Jet Blast:** weapon-independent short cone, one-cell push, Wet and temporary water coating; currently cooldown 20. It does not grant a lightning attack.
- **Undertow:** existing short-line pull toward the caster with Wet; currently cooldown 25. Do not pull an unwanted melee threat into adjacency merely because the skill is available.
- **Slam:** cudgel-specific longer knockback/collision/stun option, currently cooldown 50. Preserve the specialized weapon role.
- **Tumble:** changes both player and target position. Distinguish this from moving only an enemy.
- **Follow Through:** existing long-blade kill-and-advance active, not the new displacement passive.

Current forced movement does not use a universal mass or attacker-versus-defender Strength contest. Respect existing bracing and body-placement rules. Treat any new weight/strength/resistance model as separate scope, not as if it already exists.

### Candidate Reposition

Spend one normal action to move one adjacent enemy one cell laterally while the actor stays put. No direct damage, bonus stun or free strike. A selected destination must be physically legal for the target's entire footprint and follow existing force/bracing rules.

Before implementation, define “lateral” precisely for eight-direction movement and multi-cell creatures. Recommended first prototype: expose only target destinations one step from its origin that remain adjacent to the player and are sideways rather than directly toward/away from the player's closest interacting body cell. Provide a tested deterministic rule for diagonals and large bodies; do not silently use anchor-cell shortcuts. If an unambiguous low-risk rule cannot be agreed, restrict the first fixture to one-cell creatures and clearly label that temporary prototype limitation instead of claiming general support.

Use a meaningful configurable cooldown; 15 turns is a candidate test value, not finalized balance. Successful physical displacement already cancels appropriate committed windups; no second interruption reward or refreshed stun is added. Failure/no-op must not cancel a windup.

**Retention gate:** compare it directly with Tumble and ordinary movement. Its distinct benefit must be preserving the player's own tile while changing the enemy's. If that rarely matters, omit the new skill rather than add unrelated damage to justify it.

### Candidate Claim Ground

After the actor's own active displacement successfully changes a target's position, allow one optional ordinary step into that target's original vacated cell. It is not an automatic teleport. The cell must still be adjacent, unoccupied and legal after the displacement and all entry, slip, damage and death reactions finish.

The optional step costs no additional action in this prototype, but obeys the player's movement restrictions, footprint, root/stun vetoes, collision and entry hazards. It happens at most once per original active use, not per pushed cell or affected enemy. If several targets moved, offer only legal candidates in one bounded selection; never execute multiple advances.

No triggers from incoming attacks, counters, slides, collision chains or autonomous movement. Declining or cancelling the optional step must not undo the already paid/committed parent action. Preserve chronology: the enemy move and its consequences are real before the follow-up is offered. Revalidate after UI delay and refuse stale positions safely. Do not allow saving/reloading to duplicate the follow-up; either serialize the pending action transaction faithfully or prohibit the save only during this bounded unresolved selection according to existing action-UI rules, with an explicit test.

**Retention gate:** compare against Dodge/Tumble and against taking an ordinary step next turn. Its value is gaining meaningful ground in the same action, not immunity to consequences.

## Encounter specifications

These are optional world situations and test fixtures, not mandatory quests or perfectly choreographed puzzles. Preserve actual rolled owners, resources and old-world content. The two-caster/melee pair compositions occur only when the existing two-hostile source allowance permits them.

### Trapper yard

Existing site: southern yard near the glade, conditional Soursprayer plus Scrabbler, trap, beam-blocked service entrance, destructible walls, shallow lichen and deeper cache. Current opening changes provide a tonic and two timber while retaining ordinary rolled stock; do not describe this reward floor as the earlier historical tiny-cache-only baseline.

Proposed roles:

- Scrabbler defends the cache-side entrance with bounded pursuit.
- Soursprayer spends ordinary actions seeking a legitimate firing lane behind it.
- Moving one can separate them. Neither instantly teleports back, ignores cover or receives perfect hazard knowledge.
- Losing the defended arrangement is allowed. Return behavior must not erase legitimate displacement, cure injuries or refill abilities/resources.

Tell: visible return toward the opening, then an actual caster sidestep. Do not claim an ordinary Scrabbler has a committed attack windup.

Player routes: isolate melee behind real cover; separate the pair; use an armed trap when legal; jam an entrance with actual timber; haul the beam; breach destructible walls; take exterior lichen only; withdraw. Leave world-border travel available.

Useful outcomes: lichen for later frost tempering, real retained cache stock, a persistent opened entrance, preserved supplies. A sprung trap remains sprung. Partial success is legitimate.

### Tempering shelter

Existing site: conditional Cindercaller/Scrabbler, broken walls, forge and finite stoneburr.

Proposed difference from the yard: the Scrabbler advances toward a visible threat, while the caster values a clear lane. Paid repositioning and obstacles can separate them. Reuse current combat perception and navigation; do not add telepathic synchronized attacks.

Counterfighter can receive the advancing melee actor behind opaque cover. Displacer can make separation worse or move the caster out of position. A resource-heavy offense may remove the ranged threat earlier. Wet is helpful for some follow-up plans and unhelpful for ignition; avoid universal “elemental setup” bonuses.

Useful outcomes: seeds for Stoneskin preparation, access to frost tempering using yard lichen, or bypass and return. Current station commands can complete without advancing combat time, so do not construct a fictional timed crafting-defense test. Any station-time redesign is separate scope.

### Furrowstalker and Reedback hunt

Existing hunt mechanics include stalking/pursuit/search/feeding and a finite meat diversion. The proposed addition is clearer cover/crossing composition, not a new predator framework.

Use the actual Furrowstalker and ReedbackGrazer identities. Let visibility, cover and existing attention priorities determine whether the player can cross or divert the hunter. A piece of RawMeat/DriedMeat committed to a diversion is an actual resource, not an endless lure button. Throwing at the creature is an attack, not guaranteed feeding.

Possible outcomes include safe crossing, helping the grazer escape, retreat with supplies, or intentional combat. No new rescue XP, tame flag or hidden morality score is required. Do not guarantee rescue or safe escape because a diversion was attempted.

### Shared control encounter

Use an existing committed-melee creature such as Shambler in an explicitly controlled scenario. Do not silently add it to normal Spread population or pretend it is the default Scrabbler behavior.

First test the open-space case: ordinary walking or common Vault may solve it. Then test a real cost to abandoning the tile, such as a narrow entrance, another threat's lane or hazardous alternative cells. Do not invent damage for a decorative floor. A full build is valuable only if it changes an actual decision compared with those shared tools.

## Readability and player feedback

Reuse `CombatIntentReadout`, the live Look/Examine path, current combat-intent rendering, effect/ability UI and the new Skills Field Manual. Do not add a parallel combat model that predicts outcomes differently from the simulation.

### Four distinct results

- **Miss:** normal accuracy resolution did not connect. No Deflect/Riposte event.
- **Deflected:** full parry prevented the strike and its contact effects.
- **Guarded:** failed parry received En Garde's partial damage mitigation.
- **Riposte:** a counterattack was attempted; its own hit or miss must be visible.

Use short event text and appropriate existing action feedback. Keep essential meaning readable with muted audio and without color alone. Bespoke windup sounds are not assumed to be implemented.

### Present posture versus commitment

A visible paid caster sidestep indicates an attempt to improve a lane; it is not a promise to fire next turn. Exact ground chevrons are reserved for real committed attacks. A full parry chance must never be presented as “blocks the next attack.”

On successful displacement, remove the old committed ray and present actual recovery at the new visible position. A refused push leaves the warning intact. The warning does not rotate to chase the player. Real interposing creatures/barriers constrain it.

FOCUS has a small line budget: prioritize immediate committed danger, then one useful current-state line. Put fuller tendencies in Examine. Do not stack several labels, display debug goal stacks, reveal hidden target coordinates, expose private search budgets, or announce unseen cancellations/deaths.

Losing sight removes live cues. Reacquiring reads current authoritative state. Death, zone transitions, replacement graphs and save/load cannot leave stale arrows or duplicate events.

### Skill browser additions

Show owned/max rank, current effect, next-rank change, real SP cost and foundation requirements, including any source-based Riposte warning. Make upgrading distinct from hotbar assignment. A rank upgrade updates the same skill rather than adding another active button or duplicated passive instance. Preserve the Field Manual/Discipline Board's current controller navigation and live eligibility checks.

## Opening expeditions and acquisition

### Pace facts

The first level threshold is 115 XP, and leveling awards one SP. The four existing finite connected accomplishments each grant 30 XP: recover both original clay units, restore the first district service, harvest the recovered dry cellar bed, and collect the first commissioned meal. Together they grant 120 XP, not several skill purchases. Repairing both well and pan must not duplicate the restoration award.

### Starter candidate

A defensive swordsman preset may supply an ordinary long blade, Long Blades foundation and En Garde as part of a budget comparable to existing presets. Do not assign a final complete kit without auditing current starter grants, attributes and equipment value. Keep it an optional preset, not a class restriction or universal extra skill gift.

The current Duelist's dagger/Short Blades setup is not this kit. A guaranteed nearby ordinary longsword acquisition was not established in the narrow source pass. Generic village loaner stock does not prove a guaranteed affordable glade weapon. Resolve the equipment-access gate before advertising the build as naturally available to every fresh character.

### Suggested optional arc

1. **Glade/cellar:** select confrontation or bypass, recover meaningful supplies, deliberately learn common Vault. Do not script a lucky parry to teach the skill.
2. **First return:** choose well/pan/retaining clay; notice growing crops and useful work. These do not depend on a combat class.
3. **Mixed outing:** use yard or forge to expose a limitation without invalidating the character. Ordinary tools, cover, retreat and alternative entrances remain valid.
4. **First purchase:** answer an experienced need, such as passive defense, counter payoff, accuracy or reach. Show the two-point foundation-plus-power commitment before a planned cross-discipline purchase.
5. **Return with a new possibility:** use an unresolved entrance, resources, crops or changed approach. Never refill a depleted cache or resurrect a cleared enemy merely to demonstrate progression.

Current fresh cellar rewards include a free uncharged Vault-teaching manual, two clay and the seed-dependent charged Shattered Rime/Buckler. Current authored opening attackers have a narrow incidental-severing exemption, not global invulnerability or the removal of ordinary HP danger. Retain those newer changes rather than implementing older critique literally.

## Ordered implementation milestones

Each milestone ends with source review, relevant automated checks and recorded evidence. Do not label a checked-in test or test count as proof of ordinary discovery or enjoyment.

1. **Baseline and contract fixtures:** pin checkout; enumerate existing attack/miss/hit/reaction hooks and skill/persistence behavior; add failing tests for new semantics without changing encounter populations.
2. **Ranked skill infrastructure:** add backward-compatible rank data, atomic purchase/upgrade and existing browser integration; old unranked skills behave identically. Validate saves before adding new skills.
3. **Defense resolution:** implement the single would-hit parry decision and En Garde consumption/fallback; pass isolated damage/contact-effect tests and neighboring regression suites.
4. **Riposte reactions:** implement source-tagged single-weapon counter, current-owner revalidation, one-strike response ownership and the configurable prototype budget. Test existing Rejoinder/extra attacks before accepting.
5. **Player presentation:** update descriptions, outcome messages, rank controls and actual guard state in the existing UI. Verify ordinary keyboard/controller operations at supported viewport sizes.
6. **Controlled build comparisons:** implement isolated loadout/encounter fixtures and run paired experiments before changing general content or balance. Record all grants and setup shortcuts as fixture-only.
7. **Displacer candidate gate:** test existing control baseline; retain Reposition/Claim Ground only if each has a distinct value. Do not rename or replace existing Follow Through.
8. **Scoped encounter roles:** apply proposed behaviors only to admitted authored yard/forge roles with correct ownership and source budgets. Hunt changes remain composition/readability. Test refusal, absent/one-hostile and old-save cases.
9. **Natural opening:** resolve eligible weapon/starter availability, then run actual new-game expeditions without grants/teleports and perform blind-player discovery checks.
10. **Balance and release decision:** compare the recorded variants, resolve open decisions, run final regression on the exact revision and document remaining limits. Publishing/build distribution requires its own requested scope.

The following technical and QA sections expand this sequence into integration contracts and reproducible acceptance cases.

## Technical implementation contract

### Verified code baseline and source map

This section was checked against `main` at `244177619cffae76fb452cce40e3c49cc5dfcb2f` on 12 October 2026. Paths below exist at that revision unless explicitly marked **NEW candidate**. Proposed types, fields and event names are design suggestions, not APIs already present in the repository.

| Area | Existing source and integration point |
|---|---|
| Strike resolution | [`Assets/Scripts/Gameplay/Combat/CombatSystem.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Combat/CombatSystem.cs): `PerformMeleeAttack`, `PerformCommittedMeleeAttack`, `PerformSingleAttack`, typed damage, weapon enumeration and death checks |
| Skill ownership and dispatch | [`SkillsPart.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/SkillsPart.cs), [`BaseSkillPart.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/BaseSkillPart.cs), [`SkillEventContext.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/SkillEventContext.cs), [`SkillEventDispatcher.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/SkillEventDispatcher.cs), all under `Assets/Scripts/Gameplay/Skills/` |
| Purchase authority | [`SkillPurchaseEligibility.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/SkillPurchaseEligibility.cs) and [`BuySkillAction.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/BuySkillAction.cs): side-effect-free evaluation followed by live revalidation and SP mutation |
| Current guard | [`LongBlades_EnGarde.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/LongBlades_EnGarde.cs) and [`Effects/Concrete/EnGardeEffect.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Effects/Concrete/EnGardeEffect.cs), beneath `Assets/Scripts/Gameplay/` |
| Existing reactions | [`ShortBlades_Rejoinder.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/ShortBlades_Rejoinder.cs) and [`Cudgel_Backswing.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/Cudgel_Backswing.cs) |
| Physical eligibility | [`SkillCombatHelpers.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/SkillCombatHelpers.cs), [`SpatialQuery.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/World/Spatial/SpatialQuery.cs), [`StatusEffectsPart.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs) |
| Persistence and owner turns | [`SaveSystem.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Save/SaveSystem.cs), [`TurnManager.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Turns/TurnManager.cs) |
| Content and grants | [`LongBlades.json`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Resources/Content/Data/Skills/LongBlades.json), [`GrimoirePart.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Items/GrimoirePart.cs) |
| Current browser | [`Docs/FULLSCREEN-SKILLS.md`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/FULLSCREEN-SKILLS.md), [`SkillBrowserModel.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Presentation/Rendering/SkillBrowserModel.cs), [`SkillsScreenStateBuilder.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Presentation/Rendering/SkillsScreenStateBuilder.cs) |

The inspected [`CLAUDE.md`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/CLAUDE.md) calls for a verification sweep, red-before-green tests, paired counter-checks, in-phase review, living documentation and a deterministic self-auditing live scenario for measurable mechanics. Apply those gates during implementation. This document does not claim they have already run.

### Ranked ownership and purchases

Current `SkillsPart.AddSkill` rejects a second instance of the same runtime type. `SkillPurchaseEligibility.Evaluate` rejects every already-owned class. `BaseSkillPart` and current power metadata have no rank model. Three repeated calls to `AddSkill` therefore cannot implement three ranks.

**Recommended minimal model:** retain one attached part per owned class. Add a serialized owned rank and a read-only rank lookup that returns zero when the part is absent. Add explicit maximum-rank metadata, defaulting to one for every existing root/power, plus a next-rank cost lookup. Suggested names such as `Rank`, `MaxRank` and `RankCosts` are new schema choices to validate against the serializer, not existing members.

- Deflect and Riposte have maximum rank three and costs `[1, 1, 1]`. Long Blades root and En Garde remain maximum one and cost one. Preserve all other current costs and gates.
- Probability curves have one authoritative definition consumed by mechanics and description builders. An upgrade replaces the effective probability; it does not install another listener or sum all purchased ranks.
- A first acquisition runs the existing add/ability-registration lifecycle once. An upgrade changes the existing part through a distinct controlled rank transition. It must not replay `AddSkill`, duplicate stat shifts, change ability identity, clear cooldowns or reset reaction allowance.
- Keep `AddSkill`'s duplicate behavior for books, starter grants and legacy callers. Introduce an explicit purchase/upgrade route rather than making every duplicate acquisition secretly buy or grant a rank.
- Extend `Evaluate` to distinguish unowned, owned-and-upgradeable and maxed. Evaluate next-rank cost, parent ownership, prerequisites, exclusions and attributes from live state. Preserve the existing exemption for already granted ownership; do not delete a book-owned power because its parent is absent.
- `BuySkillAction.Execute` remains the commit authority. Re-evaluate, spend exactly the next cost, perform one transition, and roll back both SP and rank if it fails. Include old/new rank in its result and diagnostics. UI review should carry the expected current/next rank so stale confirmation cannot silently buy a different upgrade.
- Purchases require `LongBladesSkill`; do not add a mandatory Deflect prerequisite to Riposte. En Garde is independently useful and can be its only parry source. Warn about no usable source without inventing a second purchase engine or blocking an otherwise allowed purchase.

The full package remains eight SP: root 1 + Deflect 3 + En Garde 1 + Riposte 3. No SP-rate change, automatic refund or respec is part of this migration.

**Book/grant decision:** preserve current `GrimoirePart` behavior by default. It is reusable, teaches an unowned class and rejects an already-owned class. For any new ranked skill book, recommend a one-time effective rank-I grant; rereading cannot increase rank. A future rank-specific manual would need explicit target-rank/provenance semantics and a separate content decision. Do not create Deflect/Riposte manuals merely to bypass the natural-acquisition test.

### The strike resolution seam

Existing `PerformSingleAttack` is public, returns `void`, and accepts attacker, defender, weapon, primary flag, zone, RNG and optional display-source text. It currently proceeds:

1. Accuracy roll, bonuses and DV; natural 20 enters the hit path.
2. On natural miss: attacker `AttackerMeleeMiss`, then defender `DefenderAfterAttackMissed`, then return.
3. Select body hit location; calculate penetration; return on zero penetrations.
4. Sum one damage roll per penetration into one typed `Damage`; return on nonpositive total.
5. `BeforeTakeDamage` mutations, resistances, `TakeDamage`, HP/death handling.
6. Weapon/class/gas/enhancement on-hit dispatch; survivor-gated skill/critical hooks and dismemberment.

Insert the **would-hit defense decision between steps 2 and 3**. A later damage hook cannot reliably distinguish a natural miss from armor absorption, spend guard on armor-zero contact, or prevent all hit-associated consequences. A parry is its own outcome; never manufacture a miss event to reuse Rejoinder.

Recommended new outcome representation distinguishes at least invalid/cancelled, natural miss, full parry, hit without penetration and resolved hit. Preserve actual damage separately. Provide this outcome to callers that append strike-associated effects. An implementation may keep the current public wrapper and add a result-returning internal path, or migrate call sites; decide after enumerating them. Do not claim a particular proposed signature already compiles.

**Eligibility protocol:** require live, distinct participants still registered in the same zone, valid body contact at `SpatialQuery.Distance(zone, attacker, defender) == 1`, and an equipped usable long blade. `SpatialQuery` uses nearest occupied body cells and Chebyshev distance; origin/anchor distance is insufficient for large bodies. One blade permits one defense roll, even if several are equipped or one two-handed object appears on several body slots.

Current weapon lookup walks `Body.ForeachEquippedObject` and checks an attribute substring. Verify usable grip and exact LongBlades classification for the new path. Do not count a sword carried in inventory, a disarmed/destroyed item, a detached hand or a natural attack just because some unrelated damage carries a tag. Resolve legacy body-less defenders explicitly; current En Garde helper requires a Body.

A disabled defender cannot parry or counter. Query current effect action eligibility without firing `BeginTakeAction`, which also processes turn-start damage. Frozen, stunned and paralyzed are required negative cases; Rooted only blocks movement and should not by itself prevent a stationary sword defense. Perception requirements need an explicit combat-perception predicate and blind/unseen-attacker tests; do not substitute player-camera visibility.

### Guard resolution and mitigation

If neither learned Deflect nor a usable En Garde supplies a parry chance, continue ordinary resolution without a defense RNG draw. On a qualifying would-hit strike, snapshot the current Deflect rank and whether an unspent, unexpired guard is usable. Consume/remove that guard **before** rolling or dispatching reactions. Use one `rng.Next(100)` roll against `15/22/28 + 30 when guarded`; guard alone uses 30. Do not make a second independent guard roll.

A successful roll ends the incoming strike before penetration, damage, weapon contact procs, critical effects or dismemberment. A natural 20 is still an otherwise-hit strike and can be parried under this recommended prototype rule; retain critical behavior if defense fails. Log the parry outcome rather than a zero-damage hit or ordinary miss.

On failed defense with a consumed guard, carry a **strike-local fallback flag** forward. It must affect only this strike, never a nested retaliation, poison tick or unrelated damage event. Remove or retire the old `EnGardeEffect.OnBeforeTakeDamage` halving path so both paths cannot reduce the same blow.

**Recommended fixed mitigation phase:** halve the aggregate positive damage rolled across all penetrations once, using integer `n / 2 + n % 2`, before ordinary damage-event mutations and resistances. Do not halve every penetration separately. Zero remains zero. Existing guard mutates in effect-list order during `BeforeTakeDamage`; fixing this phase may change stacking with other flat/reductive effects. Record and test that ordering decision before shipping, rather than treating identical isolated half-damage arithmetic as proof of complete stacking parity.

Armor-zero contact still consumed the qualifying guard upstream. Environmental damage, spells, ranged hits, independent status ticks and nonadjacent reach do not enter this new defense path or spend guard. If the wearer becomes unable to react, the recommended behavior is that defense is temporarily ineligible while its original duration continues; no paused timer or free reactivation.

Retain `EnGardeEffect.Duration`, `Spent`, and its class identity for old saves. Keep the existing owner-turn effect lifecycle, including `JustApplied`, so the activation's own closing turn does not accidentally shorten or extend the two-owner-turn window. Successful activation still pays the normal action and starts cooldown 12; no-op/rejected activation follows existing command semantics.

### Riposte ownership and reaction safety

A full-parry outcome offers Riposte only after confirming learned rank, a usable source, a legal counter weapon/target and the available prototype allowance. Roll 45/65/80 using the same supplied RNG. Failure leaves the allowance available. Success reserves the allowance **before** entering counter resolution. A launched counter's miss, zero penetration or zero damage still spends it.

Use a new persistent defender-owned budget state, rather than a `[NonSerialized]` recursion flag or per-weapon counter. A **NEW candidate** is `Assets/Scripts/Gameplay/Combat/MeleeReactionStatePart.cs`. Reset the Riposte allowance at exactly one defined owner boundary: inside `TurnManager.EndTurn`, after the owner's `EndTurn` event/effect processing and immediately before `SpendEnergy(actor)`. Only a genuine scheduled current-owner turn may reset it, including a skipped disabled turn; guard duplicate/reentrant calls so the same completed turn cannot reset it twice. Listening to arbitrary `EndTurn` events alone is insufficient. Never reset on each enemy action, global tick, menu opening, skill refresh, equipment change, failed command or load. Record this end-to-end convention in the UI/test harness; do not describe it loosely as a global round.

The budget belongs to the actor across skill removal/reacquisition within an interval. Ensure it exists before the first reaction can launch, and preserve it through zone changes and saves. Other counter systems do not automatically inherit this new frequency limit.

Use explicit strike provenance, carried through both damage and skill-event contexts: ordinary/active/bonus strike versus reactive counter, original action/strike identity, parent strike and counter depth or equivalent. These are **new protocol fields**. Existing `attackSourceDesc` is display text; `SkillEventContext.Properties` exists, but ordinary combat contexts do not establish a comprehensive provenance contract. Never parse “(Riposte)” from a message as a security or recursion gate.

Issue exactly one selected long-blade `PerformSingleAttack`-style resolution, with normal hit, penetration, damage and applicable non-attack-generating on-hit effects. Do not call `PerformMeleeAttack` and accidentally iterate all hands. Choose deterministically, preferring a valid primary long blade then stable eligible body order; never roll once per weapon.

Reactive counters cannot generate further counterattacks or extra-attack chains. This must also block an opposing Rejoinder when a Riposte misses, and a Riposte responding to a parried reactive counter. Defensive parry may still protect against a counter; its reaction-origin flag prevents another response. Preserve legacy natural-miss Rejoinder behavior outside this new interaction unless a broader reaction change is explicitly adopted and tested.

Revalidate immediately before issuing a counter and before each resumed multi-strike iteration: both actors alive/present, same zone, body adjacency, action eligibility, and selected weapon still owned/equipped/usable. Use a transient reservation while revalidating to block reentrant issuance. If eligibility disappears before the counter is issued, release that reservation and diagnose the cancellation; commit the spent allowance atomically only when the selected-weapon counter is issued. A later miss, armor-zero hit or defensive parry still spends it. Never reroll the same original strike, teleport to a target or loop through replacement weapons. At most one counter response can claim a particular incoming strike. Full parry must not dispatch the natural-miss pathway, so ordinary Deflect plus Rejoinder never doubles up.

**Pseudocode only; conceptual operations below are not drop-in repository APIs:**

```text
resolve one strike with explicit provenance
    validate current strike participants
    roll existing accuracy
    if natural miss:
        dispatch ordinary miss hooks allowed by provenance
        return NaturalMiss

    inspect would-hit defense eligibility
    if eligible:
        snapshot rank and guard; consume qualifying guard once
        roll one full-parry chance
        if full parry:
            finalize FullParry and suppress contact consequences
            if counter origin or no legal Riposte or budget spent: stop
            if Riposte chance fails: stop, allowance stays free
            reserve allowance transiently against reentry
            revalidate counter; if invalid release reservation and return FullParry
            commit spent allowance; issue one weapon with counter provenance
            return FullParry

    resolve existing location and penetration
    aggregate damage; apply this strike's guarded fallback if present
    continue ordinary damage and permitted on-hit pipeline
    return structured result
```

### Compatibility audit for existing weapons and skills

- **Conk requires a caller fix.** [`Cudgel_Conk.cs`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/Cudgel_Conk.cs) calls `PerformSingleAttack`, then unconditionally applies Stunned. A parry return inside CombatSystem alone still permits this stun. Gate that appended strike effect on the structured full-parry outcome. Preserve its current natural-miss stun unless separately changing that ability. Revalidate life/presence after a parry-triggered counter kills its caster.
- **Multi-strike actives:** Flurry, Whirlwind and ordinary offhands get distinct strike identities and defense checks. Guard covers the first qualifying would-hit strike only. If Riposte kills, disables or disarms the attacker, later strikes must not use stale loop state. Follow Through remains its existing strike-and-on-kill movement; a parry cannot create an advance.
- **Critical and status procs:** full parry suppresses Long Blades critical bleeding, Lacerate, cutting/piercing/bludgeoning class effects, weapon on-hit overrides, gas emission, item enhancements and Axe/Cudgel on-hit effects for that incoming strike. It does not remove effects already on either actor. A landed Riposte may apply its own legitimate ordinary on-hit effects.
- **Backswing/Rejoinder:** natural miss remains distinct from full parry. A Backswing bonus strike is its own otherwise-hit opportunity when valid; a reaction-origin attack cannot use Backswing or another attack-spawning passive to evade the no-chain rule. Inspect hybrid-attribute weapons as well as normal sword/axe/cudgel/short-blade cases.
- **Explicit exclusions:** [`SkillCombatHelpers.DealGuaranteedHitDamage`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/SkillCombatHelpers.cs) handles guaranteed custom damage for Slam/Ground Pound, outside the ordinary accuracy/strike path. Their displacement, collision damage and control are not automatically parryable under this prototype. Rend Armor and Disarm directly change status/equipment without an ordinary strike roll and remain outside it. Changing any of these requires an authored contact/provenance rule and separate tests, not a blanket `Damage.HasAttribute("Melee")` hook.
- **Reentrancy:** CombatSystem's weapon-gathering scratch list explicitly assumes no nested full multiweapon resolution. A selected-weapon counter avoids that route, but audit every indirect proc and callback before relying on the shared buffers. Use reentrancy-safe storage where needed; do not clear the outer attack's weapon list from a nested action.

### Save compatibility and reproducibility gate

Current `SaveWriter.FormatVersion` is 7, and `ReadHeader` accepts strict equality. Parts are normally instantiated and populated by named public fields. `SkillsPart.OnAfterLoad` reconstructs its cache from entity parts. Effects are created without constructors, then receive saved duration/public fields, including `JustApplied`; constructor initializers alone are not an effect migration.

Adding fields is not a reason to claim universal compatibility. The existing format cannot safely skip an unknown removed field; an old binary also does not know the new skill types. **Recommended release strategy:** version-8 writes with an explicitly tested new-reader path for versions 7 and 8, retaining old field names/types and framing. This is a compatibility design gate, not an instruction to bump blindly before inspecting the actual implementation changes.

Migration acceptance:

1. Keep each old skill instance and its ability IDs/cooldowns. Missing legacy rank becomes one on an owned part; absent skill remains rank zero. Deflect/Riposte are not granted retroactively.
2. New saves persist ranks, unspent/spent reaction allowance, guard duration/Spent/JustApplied and current cooldowns. `OnAfterLoad` rebuilds caches without rerunning acquisition or resetting these fields.
3. Restore old active En Garde with remaining lifetime and spent state, under the documented new combat semantics. Do not reset duration or charge a new action. Reject or explicitly repair invalid rank ranges; never silently grant maximum rank.
4. A new writer must not leave header/version metadata inconsistent. Preserve the existing section framing and prove v7 fixtures load without stream misalignment. Preserve the original save during migration. Old binaries should reject new saves cleanly rather than half-read them.
5. Save only at an atomic resolved-strike boundary, or explicitly serialize any pending reaction transaction. Do not load a state where incoming damage was prevented but a counter can be issued twice.

Forward the caller's RNG through defense, Riposte and counter resolution. Do not instantiate a wall-clock RNG inside the new skills. No defense draw occurs on a natural miss/ineligible strike; no Riposte draw occurs without a full parry and legal available reaction. Deterministic tests must assert draw ordering as well as thresholds. Full parries intentionally skip downstream penetration/damage/proc draws.

**Honesty bound:** seeded scenario reproducibility is not identical random continuation across real save/load. Current `LoadBrainPart` reconstructs `brain.Rng` with `new System.Random()`. The new budget must survive load regardless. If exact combat replay after reload is required, add a separately specified persisted RNG-state protocol; do not claim the existing save system already guarantees it.

### Field Manual and phased file changes

Extend existing `PowerData.cs`, `SkillData.cs` and `SkillRegistry.cs` for validated rank metadata; `BaseSkillPart.cs`, `SkillsPart.cs`, `SkillPurchaseEligibility.cs` and `BuySkillAction.cs` for runtime ownership and atomic transitions. Missing rank metadata must preserve all old one-rank content. Invalid cost arrays/rank maxima must fail content validation visibly.

**NEW candidates:** `Assets/Scripts/Gameplay/Skills/LongBlades_Deflect.cs`, `LongBlades_Riposte.cs`, and a narrowly scoped combat defense/outcome helper under `Assets/Scripts/Gameplay/Combat/`. Add Unity `.meta` files through the normal project workflow. Avoid a global replacement of every skill/event framework.

Modify `CombatSystem.cs`, `EnGardeEffect.cs`, necessary reaction/active callers and `SkillEventContext`/dispatcher together behind failing contract tests. Only then author new entries/descriptions in `Assets/Resources/Content/Data/Skills/LongBlades.json`.

Update the existing presentation files `Assets/Scripts/Presentation/Rendering/SkillsScreenStateBuilder.cs`, `SkillBrowserModel.cs`, `AbilityDetailsBuilder.cs`, and `Assets/Scripts/Presentation/UI/SkillsScreenUI.cs`/`.View.cs`. Current row state treats ownership and buyability as alternatives; an upgradeable owned skill requires distinct ownership/rank and next-purchase eligibility. Do not make a rank-I passive appear unowned merely because rank II is purchasable. Preserve hidden-book ownership, stable class selection, cancellation-first review, controller navigation, hotbar identity and zero turn/energy cost for browsing/purchases. Show exact current/next values, one-SP next cost and “MAX 3/3” when complete.

### Diagnostics and low-level acceptance

Extend existing `skill`/`damage` records and correlation IDs. Proposed records should expose: original/parent strike, origin, rank, eligibility rejection, guard snapshot/consumption, defense roll/chance/outcome, fallback input/output, Riposte roll, budget reservation, counter cancellation/result, selected weapon and resulting HP delta. Every rejectable gate needs an observable reason. Diagnostics are developer evidence, not permission to reveal hidden enemies in player UI.

The mechanical gate is green only with paired positive/negative assertions for guard-alone and each rank; exact roll boundaries; natural miss versus armor-zero; odd/even and multi-penetration damage; critical and every contact-effect path; no-source/raw-Melee-tag exclusions; multiweapon and two-handed deduplication; death/removal/disarm between stages; large-body adjacency; disabled/rooted distinction; failed proc versus counter miss budget behavior; save/load on both sides of the owner boundary; and legacy purchase/book/reaction regressions. Include stale UI confirmation and duplicate-input tests.

Follow [`Docs/PERF-FOUNDATION.md`](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/PERF-FOUNDATION.md): no per-frame registry rebuilding, no LINQ/new collection churn in added hot paths, state-driven UI invalidation, and real crowded-combat profiling. Do not optimize away correctness or reuse a non-reentrant scratch buffer through recursive combat. Record isolated tests, full regression, live deterministic matrix, visual/controller checks and unverified physical-hardware behavior separately.

## Validation and playable acceptance plan

This is a proposed acceptance plan, not a report of tests run for this document. Source inspection is pinned to `244177619cffae76fb452cce40e3c49cc5dfcb2f`. Historical green receipts in the repository establish their original scope only; they do not verify the proposed Deflect/Riposte revision or new movement powers.

### 1. Keep three kinds of evidence separate

1. **Deterministic rule tests:** isolated actors, controlled rolls, exact state assertions and production combat/save entry points. These prove mechanics, including unlikely failure branches.
2. **Native Unity ordinary-input journeys:** the real bootstrap, current full-screen skills UI, actual inventory, AI, turn scheduler, rendered cues, travel and F5/F6. These prove that a player can reach and use the feature. Controlled native fixtures are useful but must carry a fixture label.
3. **Unaided player sessions:** fresh players, no route coaching, observed decisions and a short interview. These evaluate discoverability, identity and enjoyment. Neither damage simulations nor scripted successful journeys substitute for them.

Every case records baseline/candidate commit, Unity version, seed, actual starting build, manifest version, save identity, fixture/native classification, selected test count and unexpected console errors. A timeout, compile failure, stale assembly, zero-test discovery or incomplete report is not a pass. Preserve failures before retrying.

### 2. Test setup and executable entry points

The checked project specifies Unity **6000.3.4f1**. Use an isolated save location through the existing native launchers and retain their scene/preferences restoration checks. Do not overwrite a player's ongoing world. Verify the actual loaded scene and address; old overview documentation names different scene locations, while the checked Spread launcher explicitly opens `Assets/Scenes/Main/SampleScene.unity`.

Source-verified standalone pre-check, from a checkout:

```sh
cd Tools/EditModeRunner
python3 select_tests.py --all
./run.sh
```

Record the .NET SDK/runtime. The runner selects only files that compile without Unity and uses stubs and different string-seed hashing. Compare baseline/candidate failures, by test name and failure message, with its documented `diff_results.py`; do not turn the existing environmental failures into a new universal allowed-failure count. The native editor remains authoritative. Do not run the README's optional stash-based example against an unreviewed dirty working tree; separate baseline/candidate checkouts are safer.

Run native EditMode through the installed Unity test tool, following its current schema. The repository documents `run_tests mode=EditMode`; any new fixture name below is a **proposed suite**, not an existing callable command. Expand it to the actual compiled class before invoking a filtered run. Native PlayMode and ordinary input are separate gates.

Verified existing editor entry points, called only with Play stopped and saved scenes:

```csharp
CavesOfOoo.Editor.SpreadDiscoveryNativeBatch.LaunchOpening("duelist", 64, "bypass");
CavesOfOoo.Editor.ReferenceGladeNativeBatch.LaunchSkillsAt(800);
CavesOfOoo.Editor.ReferenceGladeNativeBatch.LaunchSkillsAt(720);
```

The opening launcher also supports the documented `melee`, `supplies`, `withdrawal`, `unread`, `yard-first` and `vault-strike` approaches. Its existing driver does not magically test new candidate purchases or encounter behavior: extend the driver and its assertions before treating it as candidate acceptance. The skills launcher stages **10 SP** and proves UI transactions, not earned progression.

### 3. Equal-budget build laboratory

Create three controlled six-SP profiles with identical level, HP, attributes, Agility at least 15, weapon/armor quality, healing inventory, starting cells, world state and known Vault status. Log the six actual purchases, including roots. No profile receives a free discipline root. Disable unrelated starting powers consistently in this fixture only; separately test intact live starters.

- **Specialist:** Long Blades root, Deflect I, En Garde, Riposte I, Expertise, Lunge.
- **Hybrid:** the same first four, then Hydromancy root and Jet Blast instead of Expertise/Lunge.
- **Existing movement control:** Hydromancy root, Jet Blast, Undertow, Acrobatics root, Dodge, Tumble.
- **Optional new-movement variant:** replace only Dodge/Tumble with proposed Reposition/Claim Ground, provided their final prerequisite graph actually permits this six-SP allocation. It is an experiment, not a final tree decision.

In the shared Vault arm, all profiles learn it from the same physical manual at zero SP, and none gains Acrobatics root from that reading. Also run all profiles without Vault to measure how much the shared discovery masks their differences. Equipment can enable a profile's skill, but cannot secretly improve its damage, armor or speed. These laboratories do not establish that six SP are reachable in the opening.

Compare the proposed defensive starter separately against the actual existing starters. Price its blade, root and En Garde against the whole existing package, including supplies and forgone powers. Never label the proposed starter as currently available.

### 4. Deterministic matrix: defensive blade rules

Use a programmable RNG adapter or deterministic sequence fixture at the production RNG seam; assert the number and order of consumed rolls. If that seam must be added, mark it as test infrastructure. Never repeatedly reload until a desired random outcome appears and call that a probability test. The cases below use a normalized 0–99 roll with success when `roll < chance`; adapt boundary values explicitly if the production domain differs.

| ID | Setup and action | Required assertion and counter-control |
|---|---|---|
| DB01 | For Deflect I/II/III, force an otherwise-hit eligible melee attack with rolls immediately below and at 15/22/28. | Below succeeds; threshold fails. No passive, wrong weapon and unlearned rank never inherit the chance. Purchasing a higher rank replaces the prior chance; ranks do not sum. |
| DB02 | Force attack roll below, at and above its ordinary hit threshold before any parry decision. | An ordinary miss stays a miss, consumes no guard and opens no Riposte window. A would-hit enters exactly one parry gate. Preserve the project's ordinary natural-roll and attack-flag semantics. |
| DB03 | Arm En Garde and repeat DB01. | Effective parry thresholds are 45/52/58, using one parry roll, not a second independent roll. With only Long Blades root and En Garde, require its standalone 30-point case; guard-first is a supported route. Counter: identical unguarded state retains 15/22/28. |
| DB04 | Guard a would-hit with a failed parry and deterministic aggregate pre-mutation/pre-resistance weapon damage 1, 2, 3 and 9. | Remaining damage is respectively 1, 1, 2 and 5 at the specified reduction stage. Consume once. A second eligible blow receives no fallback. A successful parry deals zero and does not also halve another blow. Add a fixed flat damage modifier plus resistance control: the half applies to the aggregate first, then the existing mutation/resistance order runs once; assert the exact result and compare an unguarded control. |
| DB05 | While guarding, cause an ordinary miss, spell, ranged hit, environmental tick and separately a qualifying would-hit whose armor result is zero. | Nonqualifying events preserve guard. The qualifying would-hit consumes it even when armor later yields zero; log would-hit/parry/armor separately. Zero HP loss is not evidence of a parry. |
| DB06 | Activate guard, spend owner actions and interleave several enemy actions at different speeds. | Guard lasts the specified two owner-turn intervals, not two global events or two attacker turns; cooldown is 12 under the existing owner cooldown convention. Pin the exact activation/decrement boundary in a test before implementation. Repeated UI open, wait in a modal or reactivation cannot refresh it. |
| DB07 | After a successful qualifying parry, test Riposte I/II/III immediately below/at 45/65/80. | Conditional counter attempt follows 45/65/80. Failed parry, ordinary miss and absent Riposte cause none. Report counter probability given a parry, not an unconditional 45–80% claim. |
| DB08 | Force Riposte success, then force its ordinary melee attack to miss and separately hit. | Exactly one normal counter attempt; it can miss. Armor, resistance, weapon/body, hit/kill attribution and eligible on-hit rules use the normal single-attack path. No guaranteed hit or full multiattack sequence. |
| DB09 | Two enemies attack in one owner interval; include dual weapons and a fast attacker. | At most one counter **attempt**, including a missed attempt, until the next specified owner-interval reset. A failed Riposte probability check leaves the allowance available; a launched counter spends it even when its attack misses. If validation cancels before issuance, release the transient reservation; a later separate eligible parry can still counter. After issuance, miss/armor-zero/parry spends the allowance. Never retry the original strike. Pin reset timing independently of UI input. |
| DB10 | Both actors can parry/counter; also give the defender Short Blades Rejoinder and change weapon ownership mid-resolution. | No reaction chain, counter-to-counter recursion or double Rejoinder/Riposte payout. One causal attack cannot be both an ordinary dodge reward and a new parry reward. |
| DB11 | Unequip, disarm, destroy or invalidate the sword/body slot between activation and contact; move/remove/kill either actor or change zones. | Revalidate live equipment, body, owner and spatial identity. No stale weapon, dead target or foreign-zone counter. Refused branches are safe and observable; no charges disappear merely because a menu was opened. |
| DB12 | Reach-two attack, diagonal contact, intervening wall/creature, behind-cover source and attack flag variants. | A nonadjacent range-two Lunge bypasses the new parry gate, preserves guard and produces no Riposte; pair it with a genuinely adjacent qualifying strike. Preserve explicit LOS/contact rules at both defense and counter stages. A parry must not manufacture counter reach; a blocked counter never strikes through a wall. |
| DB13 | Qualifying Conk strike: force full parry, ordinary hit and natural miss. Contrast Slam/Ground Pound, Rend Armor and Disarm. | Full parry suppresses Conk's strike-associated stun despite its current unconditional post-attack application. Preserve existing natural-miss behavior unless separately changed. Guaranteed direct damage/control and non-hit-roll maneuvers do not become parryable merely through a `Melee` tag; they remain outside the new strike gate unless explicitly reauthored. |
| DB14 | Defender cannot react or perceive its attacker; repeat with an NPC defender outside the human player's visible camera area. | Use the chosen combat-perception/reaction contract, not player rendering visibility. Positive control perceives and reacts; disallowed status/perception fails safely. These eligibility rules must be decided and documented before assertions become release gates. |

For DB06/DB09, write an ordinal trace such as activation → enemy action A → enemy action B → next owner action → enemy action C. Annotate remaining guard turns, cooldown and counter budget at every boundary. The prototype counter reset occurs inside `TurnManager.EndTurn`, after the scheduled owner's EndTurn event/effects and immediately before `SpendEnergy(actor)`, once per genuine scheduled owner turn. Test disabled/skipped owner turns and duplicate/reentrant calls; arbitrary EndTurn events, global ticks and menu actions must not reset it. Preserve and explicitly pin En Garde's existing activation/decrement convention separately; do not infer guard duration from the counter reset or a screenshot.

### 5. Deterministic matrix: composition, progression and persistence

| ID | Setup and action | Required assertion and negative control |
|---|---|---|
| CP01 | Cast existing Jet Blast and Undertow against a legal target, then wall/edge/anchored/invalid targets. | Jet Blast retains one-cell push, Wet and cooldown 20; Undertow retains one-cell pull and cooldown 25. Distinguish successful force from attempted force. Ordinary damage/effect behavior is unchanged. |
| CP02 | Exercise existing Slam, Dodge and Tumble independently before combining them with defenses. | Slam remains Cudgel-specific; Dodge retains +2 DV; Tumble retains its legal swap/confusion behavior. An existing named power is not evidence for proposed Reposition. |
| CP03 | If implemented, use Reposition to move the adjacent enemy sideways one cell with a free cell, occupied cell, wall, edge and movement-blocking status. | Enemy moves; player stays put. Test all eight approach directions and full body footprints against the final lateral rule, cost and cooldown; no bonus damage or stun. Cancellation/refusal preserves state. Treat cooldown as provisional until selected and tested. |
| CP04 | If implemented, actively force an adjacent enemy away and accept/decline Claim Ground. Repeat after failed force, reaction force, environmental slip and another follow step. | At most one optional step into the exact vacated legal cell, only after the owner's eligible active force. No remote move, reaction/slip chain or recursively earned follow step. Other actors occupying it veto movement. |
| CP05 | Kill with existing Long Blades Follow Through; combine with the candidate move system. | Existing on-kill Follow Through remains its own mechanic. Claim Ground does not replace, rename or silently duplicate it. Define and test simultaneous-trigger ordering explicitly. |
| PR01 | Fresh level-one actor, XP 114 then +1; repeat starting XP 0 with four distinct connected accomplishments. | First level at 115 XP; +1 SP per level. Four source-bound 30-XP awards total 120, producing one level/SP from zero, not four points. |
| PR02 | Recover the two original clay units; restore either eligible service; bring the bound dry bed to harvest; collect the first commissioned meal. Replay and vary source IDs/world IDs. | Exact committed awards once. Restoring both services still shares the restoration award. Looking, purchase attempts, rollback, unrelated crops/clay/meals and repeat collection award nothing. |
| PR03 | Read one fresh v17 cellar manual; save/load; read again; inspect skills. Load an older cached world afterward. | Vault learned without SP and without Acrobatics root; normal duplicate-read behavior. Old cache/world remains literal, with no retroactive manual or enemy replacement. |
| PR04 | Buy ranks I–III, try rank IV, double-confirm, cancel, lose SP/prerequisites while review is open, then repeatedly read a reusable skill grimoire. | Each valid rank costs exactly one SP; rejection/cancel changes nothing. One owned skill Part/ability identity remains, maximum rank is enforced, and `AddSkill`/book re-reads never silently purchase or increment ranks. Book ownership and purchased rank progression are distinct routes. |
| SV01 | Save before guard, during unspent guard, after fallback/parry, after counter attempt, and one boundary before expiry/reset. Reload each. | Skills/ranks, guard remaining/spent state, cooldowns, budget interval and exact equipped owner references match. Reload cannot refresh a counter or resurrect guard. |
| SV02 | Save defeated enemies, moved objects, looted cache, spent trap, harvested bed, working/ready/collected meal and learned Vault. Leave/return and fully reload. | Same source ownership, depletion, physical outputs and finite awards; no duplicate skill, corpse, loot, food or XP. Old manifest generation is unchanged. |
| SV03 | Load checked-in real version-7 fixtures with unranked skills and active old guard; save/load under the proposed version-8 reader/writer. Try truncated, malformed and unsupported-version files. | Explicit migration initializes ranks and new guard/reaction state by declared policy, preserves old graph/content and fails atomically on invalid data. The strict current format cannot safely skip arbitrary inserted fields; test v7 compatibility rather than claiming reflection automatically supplies it. Keep the original checkpoint intact on refusal. |

Add independent adversarial suites covering invalid IDs, duplicated Parts, callback-induced death/movement, target mutation between preview and commit, save replacement, overflow, cancellation and rollback. Include positive controls proving each fixture really reaches the relevant branch. Proposed suite families are `ComposableBladeTests`, `ComposableBladeAdversarialTests`, `DisplacementCompositionTests` and `ComposableBuildSaveTests`; these names are planning placeholders.

### 6. Diagnostic and visual evidence contract

Existing useful categories include `skill/CommandRouted`, `skill/CommandRejected`, skill-local `SkillRejected`, combat hit rolls, `damage/DamageDealt`, death attribution and `event/ConnectedExplorationAccomplished`. Verify exact emission names at implementation time. Proposed new defensive records should carry actor/attacker/weapon IDs, source attack cause, would-hit result, roll/threshold, passive rank, guard contribution, guard consumption/reduction, counter eligibility/probability, interval budget and rejection reason. Correlate the originating hit, reduction, counter and any death by cause; unrelated nearby combat cannot satisfy the test.

For intent, use the existing `CombatIntentReadout` and `CombatIntentRenderer` contract: only current visible physical owners, visible unblocked ray segments, fixed committed origin/direction, and correct removal after movement, death, cancellation or zone change. Capture cue, Look/Examine text and actual resolution together. Hidden owner, hidden intermediate cell and visible far tile must not leak a warning. Do not assume an ordinary Scrabbler has a windup; first assert the tested blueprint actually owns the opt-in commitment Part. Ranged lane behavior needs its actual targeting evidence, not a relabelled melee chevron.

Capture full UI at 1280×800 and 1280×720: ranks/costs, live requirements, owned manual-only Vault, En Garde conditions, real range/cooldown, purchase cancel/confirm, assignment and locked prerequisites. Inspect pixels, not just screenshot existence. Verify no world action/energy/tick change while browsing. Sample a real 60–90-second combat/UI/intent session for frame-time spikes and allocations; scoped cache tests do not certify whole-game or physical Deck performance.

### 7. Ordinary-input end-to-end recipes

Run the five existing starts on fresh seeds **1, 64 and 1729** first. Verify the selected build after bootstrap, not just the requested label. Record actual admission of each optional site. A coordinate/manifest assignment is not proof the source transaction succeeded. Maintain at least one admitted and one refused-site case from a source census; do not repair refusals by spawning missing actors.

**E2E01: glade → cellar → technique → yard.** Start at `Overworld.11.10.0`, use the local clue and actual stairs into its cellar. Choose an ordinary direct/service/detour approach. Take the actual cache, confirming two original clay units and the fresh-world manual. Open inventory with I, select the book's Read action, close inventory and dismiss each announcement. Open Skills with X; confirm Vault ownership and absent unpurchased Acrobatics root. Assign through I within the skills screen to a chosen slot, close, activate that slot, move the Vault cursor and confirm a legal visible destination. Confirm the current cursor distance 1–2 contract, rather than obsolete direction-only instructions. Walk south to `Overworld.11.11.0`; use the demonstration crossing only when realized and legal, then choose to retrieve or abandon real salvage and return. Record paid inputs, minimum HP, supplies spent and acquisition provenance.

**E2E02: paid defense purchase and actual fight.** Start with naturally available SP or first earn it; if it is insufficient for root plus power, retain that result. X opens the full-screen browser; Q/E changes discipline, arrows/J/K select, Enter opens review, Right selects learning, Enter commits. Cancel is initially selected. Test cancellation first, then one valid purchase, then repeated confirmation. Assign the actual active and use it in a live qualifying fight. Capture miss-preserves-guard, guard resolution and counter evidence when naturally observed; use DB fixtures for exact RNG branches not reached here. Do not grant SP or reroll for a favorable screenshot.

**E2E03: yard and forge decision comparison.** On admitted candidate worlds, test the yard's cache-guard Scrabbler plus paid-budget Soursprayer lane and the forge at `Overworld.12.10.0` with an advancing Scrabbler/Cindercaller lane. Preserve the conditional two-hostile budget; one-hostile/refused sources must not grow into free extra groups. Record waiting, covering, advancing, pushing/pulling, retreat and ordinary attacks. Compare same profile decisions before/after the candidate encounter change. An observed safe lane bypass is a valid outcome, not grounds to secretly strengthen enemies.

**E2E04: noncombat progression and return.** Take clay through its real source; choose well or kitchen pan repair. Reach kitchen `Overworld.12.11.0` and seed keeper `Overworld.11.8.0` through ordinary travel. Gather/pay actual ingredients, commission, wait/travel under ordinary time, collect the physical meal, and cultivate/harvest the designated dry cellar bed. Record the four finite award causes and resulting XP/SP. Inputs from hostile kills add separate XP; do not attribute their levels to the 120-XP circuit. Actual stock/admission may change the route; document the missing source rather than inventing guaranteed supplies.

**E2E05: hunting and persistence.** Use actual Furrowstalker/Reedback situations and finite meat under current AI. Test diversion, withdrawal and attack without new stalking behavior. Record consumed meat and changed target state; save, leave, return and reload. Repeat an unread-manual journey and a yard-before-cellar journey to prove there is no mandatory learning order.

In all final journeys: no teleports, stat/inventory/ability grants, healed-to-full resets, frozen enemies or cooldown edits. Read-only observations are allowed; do not dispatch events or end turns through evaluator code. The established input guide recommends short key pulses, state checks and dismissal of queued announcements. Re-query after transitions and stop on unexpected modal state rather than pressing blind sequences.

### 8. Save/console closure and blind player gate

For every native acceptance journey, F5 must emit a new success message and write a changed isolated checkpoint. Make a real subsequent move or paid action that changes a recorded value; F6 must replace the player graph and restore the checkpoint's identity, position, HP, energy/tick, skill ownership and depleted sources. A pre-existing file and an unmoved character prove nothing. Restart the editor/player for one fresh and one legacy save, then verify the same state. Preserve console errors/warnings with phase and stack; compare existing warnings explicitly, require zero unexplained new errors, and confirm save root/preferences/scenes restored after success, refusal and timeout.

Recruit at least three first-time players for an exploratory gate; give only “Explore, make your character stronger, and return when you choose.” Do not reveal the cellar route, optimal build or proposed mechanics. Record time to first stated goal, whether they notice the manual, whether they can explain two approaches, their first purchase and its understood cost, guard/counter comprehension, avoidable versus unreadable damage, chosen retreat and reason to revisit. Ask afterward: “What changed your plan?”, “What did that defensive move do?”, and “What would you buy next, and why?” A preliminary gate is two of three recognizing a changed possibility and a meaningful next choice without hints. Report every hint and failure; three people are qualitative evidence, not population-level proof.

For build preference comparisons, use a separate matched, counterbalanced session with the controlled six-SP profiles; rotate order and hide descriptive labels that prime “specialist” or “hybrid.” Ask for interesting choices and frustrations before revealing numbers. Do not conflate a strong profile with an enjoyable one.

### 9. Balance hypotheses, stop conditions and release checklist

Pre-register risks: guarding may dominate ordinary attacks; stacked reactions may explode multi-enemy damage; mobility may erase lane pressure; the shared Vault may obscure build identity; root taxes may make a promised opening combination inaccessible; finite rewards may feel trivial compared with injury risk. Use paired encounter seeds and explicit action policies in a separate simulation. A shared initial seed alone does not preserve identical combat draws after builds consume different RNG calls; report that limitation or use a declared event-keyed simulation stream. Report trial count, deaths/withdrawals, HP-loss distribution, turns, resource cost, parry opportunities and conditional counter rates with 95% confidence intervals. Do not treat overlapping intervals as proof of equality or retry until a desired ranking appears. Increase trials for precision under a declared stopping rule; gameplay enjoyment remains a human judgment.

Release only when:

- Candidate rule and adversarial suites pass in native Unity; new standalone regressions are absent and exclusions are disclosed.
- Exact guard/counter turn semantics, side-effect order, rank costs/prerequisites and any new movement cooldown have no unresolved design ambiguity.
- All three equal-budget profiles are legal and identity differences appear without unequal stats, gear or shared-discovery benefits.
- Fresh/legacy save tests pass; no reaction loops, duplicate awards, respawns, hidden-owner cues or stale-world rewrites remain.
- Ordinary-input routes, admitted/refused-site controls, actual purchases, console and restoration checks have final receipts; each screenshot was inspected.
- Blind-player findings are reported separately, with confusing or mandatory-looking steps addressed or explicitly left open for user review.
- Evidence index distinguishes passed, failed, blocked and never-run cases. No “balanced,” “fun,” “Deck verified” or “complete first hour” claim exceeds its evidence.

### Source anchors for this plan

- [Test runner and its honesty bounds](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Tools/EditModeRunner/README.md), [Unity version](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/ProjectSettings/ProjectVersion.txt), [native input strategy](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/MCP_PlayMode_Testing_Strategy.md), [adversarial methodology](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/ADVERSARIAL_TESTING.md).
- [Full-screen skill input](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Presentation/UI/SkillsScreenUI.cs), [staged UI audit](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Scenarios/Custom/ReferenceGladeNativePlayer.Skills.cs), [opening launcher](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Editor/Scenarios/SpreadDiscoveryNativeBatch.cs), [skills launcher](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Editor/Scenarios/ReferenceGladeNativeBatch.cs).
- [Finite accomplishment implementation](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/World/ConnectedSpreadProgress.cs), [opening/Vault revision and native limits](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/SPREAD-OPENING-DECISION-DESIGN.md), [connected region and finite sources](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/SPREAD-CONNECTED-EXPLORATION-DESIGN.md), [glade checkpoint proof requirements](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/DENSITY-REFERENCE-GLADE.md).
- [Visible intent authority](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Look/CombatIntentReadout.cs), [intent rendering](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Presentation/Rendering/CombatIntentRenderer.cs), [existing En Garde entry point](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Scripts/Gameplay/Skills/LongBlades_EnGarde.cs).

## Open decisions and implementation handoff

Resolve these questions explicitly in the implementation record. The recommended values above allow a controlled prototype, but none should be hidden in an incidental code change.

| Decision | Prototype default or evaluation | Required evidence before keeping it |
|---|---|---|
| Riposte allowance | One issued attack between owner intervals, failed proc leaves allowance | Compare fast/slow actors, crowds and uncapped arm; no save-reset exploit |
| Reaction weapon choice | Deterministic eligible long blade; one response owner per strike | Mixed sword/dagger and weapon-removal tests; existing reactions unchanged when new skills absent |
| Counter attack flags | Normal accuracy, never guaranteed hit; suppress recursive reaction origins | Counter miss/parry tests and audit of every post-strike attack caller |
| Perception | Defender can perceive adjacent attacker through actual simulation rules | Player/NPC, darkness, blindness, hidden-attacker and full-body adjacency controls |
| Zero-damage connecting strikes | Qualifying would-hit defense resolves and consumes guard before armor | Separate zero-damage, natural miss and full-parry records; no double consumption |
| Other direct attacks | Explicitly classify special attacks that bypass normal accuracy | Conk side-effect test and direct-damage/control exclusions; no silent universal immunity |
| Rank persistence | One source of truth; legacy rank one; repeat books cannot rank up | Actual old save fixture, new save round trip, repeated grant/purchase and damaged-data tests |
| Reposition geometry | One legal lateral displacement; simple tested diagonal/body policy | Compare with Tumble; reject or defer ambiguous footprint behavior |
| Claim Ground choice | At most one optional legal ordinary step after all primary effects | Multi-target, slippery/trapped destination, interruption, save and cancellation controls |
| Candidate tree placement | Acrobatics placement/cost is a comparison assumption | Real eligibility and same-SP comparison with Dodge/Tumble; no hidden new root |
| Starter kit | Optional blade/root/guard at a comparable overall starter budget | Current starter-value audit, natural acquisition, no undocumented extra benefits |
| Encounter role tuning | Source-bound role behavior with paid movement and bounded pursuit | Same world state baseline/candidate, refusal and legacy-save cases, no required solution |
| Final rank percentages | Keep prototype values until measurement | Conditional rates, survival/resources, decisions and blind feedback rather than counter count alone |

### Suggested implementer task

> Work in the current Unity CavesOfOoo repository, beginning with the baseline and instructions in this specification. Reconcile any newer source before editing and preserve unrelated work and saves. Implement the ranked defensive-sword core first with failing behavioral tests, then integrate its real player UI and save compatibility. Treat the listed probabilities as configurable prototype values. Preserve natural-miss behavior and existing named skills, especially Rejoinder and Long Blades Follow Through. Do not grant passive ranks through repeated books. Record exact attack outcomes and reaction provenance so full parries prevent their strike's contact effects without suppressing unrelated world events. Validate the core before adding candidate movement powers or encounter behaviors. Run controlled mechanics tests, native ordinary-input acceptance and blind-player evaluation as separate evidence classes. Preserve old worlds and inspect actual visuals. Report implemented, tested, blocked and deferred work separately. Do not publish, merge, deploy or overwrite user saves merely because the document contains a release checklist.

This prompt is a ready handoff, not a claim that an implementation task has been started.

### Deliverable checklist for an implementation

- [ ] Source baseline, changed-file ownership and design-difference record.
- [ ] Rank schema/purchase/save decision and legacy fixtures.
- [ ] Single defense outcome and special-attack classification.
- [ ] En Garde consumption, fallback and exact scheduler trace.
- [ ] Riposte source ownership, weapon selection, allowance and recursion protections.
- [ ] Actual Field Manual rank/purchase/assignment presentation.
- [ ] Existing-skill regression evidence and deterministic boundary cases.
- [ ] Controlled equal-budget profiles, common Vault and logged setup grants.
- [ ] Candidate movement acceptance or explicit decision to defer it.
- [ ] Source-preserving authored encounter integration or explicit deferral.
- [ ] Natural new-game acquisition, real SP costs and end-to-end checkpoint restoration.
- [ ] Blind-player observations and unresolved balance/clarity hypotheses.
- [ ] Final exact-commit test evidence, inspected screenshots and known limitations.

## Additional source references

These records provide context and earlier native evidence. Their historical successes do not establish that this specification has been implemented.

- [Starting builds](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Resources/Content/Data/Builds/StartingBuilds.json)
- [Long Blades data](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Resources/Content/Data/Skills/LongBlades.json)
- [Hydromancy data](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Resources/Content/Data/Skills/Hydromancy.json)
- [Acrobatics data](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Assets/Resources/Content/Data/Skills/Acrobatics.json)
- [Full-screen skills design and implementation](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/FULLSCREEN-SKILLS.md)
- [Connected Spread](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/SPREAD-CONNECTED-EXPLORATION-DESIGN.md)
- [Working water and timber](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/EXPLORATION-DEPTH-NEXT-DESIGN.md)
- [Yard geometry and historical evidence](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/TRAPPERS-DISPATCH-YARD.md)
- [Specialist worksites](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/SPREAD-CONTENT-EXPANSION.md)
- [Predator diversion](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/PREDATOR-DIVERSION-DESIGN.md)
- [Current opening changes](https://github.com/stevenrichter16/caves_of_ooo/blob/244177619cffae76fb452cce40e3c49cc5dfcb2f/Docs/SPREAD-OPENING-DECISION-DESIGN.md)

## Design rationale from the Qud comparison

The earlier comparison used traditional Joppa as a reference for connections between intention, route choice, threat assessment, loot interpretation and new possibilities. It was not a requirement to copy Qud's enemies, lore, assets, exact mechanics or punishment. The Spread's distinctive strength is learning and improving a worked landscape through material choices and consequential returns.

The motivating risks were reading/interaction burden, a dominant response to repeated encounter shapes, weak value for preparation when retreat/rest solves the same problem, and disproportionate punishment for modest rewards. Some opening risk/reward concerns were subsequently addressed by the reviewed source revision. Do not implement obsolete critique in place of inspecting the current build.

Judge this milestone by whether new purchases produce different worthwhile decisions in that landscape. More skills, more tests and more creatures are not substitutes for that result.
