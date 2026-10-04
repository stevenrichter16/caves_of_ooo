# Gallery iteration and learnable combat — working prompts

Baseline: main `59a28aa07`, 2026-10-03. User authorizes autonomous brainstorming, design, implementation, review and publication.

## Brainstorming prompt

Read the current inspection-gallery implementation, actual combat/AI/skill code and canonical lore. Identify what the player can notice, predict and change through ordinary play. Compare extensions against the existing systems before proposing new infrastructure. Prefer choices with distinct costs, safe observation, partial success and persistent physical aftermath. Find combat inconsistencies that prevent knowledgeable players from executing their intended plan. Brainstorm other reusable event archetypes with stakes, alternate verbs, consequences and real mechanical prerequisites. Choose one coherent gallery/combat milestone; distinguish selected work from future encounter ideas. Do not equate more items, health or switches with depth.

Executed: reviewed the gallery's prescribed route, shared door/repair/stock state, ordinary pursuit, authored NPC powers, adjacent weapon skills, status effects and renderer/readout seams. Selected a directional attack commitment with counterplay, precise adjacent-skill targeting, and a gallery layout/stock/clue revision. The design records rejected premises and seven future event archetypes.

## Design prompt

Turn the selected ideas into a complete, bounded player loop. Specify actors, locations, physical geometry, observable tells, action timing, resource costs, useful outcomes, failure states and saved state. For combat, state exactly when direction becomes fixed, what blocks or interrupts an attack, when the opponent is vulnerable, and how normal armor, weapon, status and turn rules remain authoritative. Verify selected-target ownership and refusal semantics. Make knowledge transferable to an existing wilderness enemy. Describe restrained in-world/readout cues, required art and ordinary input acceptance. Define observed failing tests, positive/counter pairs, adversarial checks and the final native verification scope before production.

Executed: `GALLERY-AND-TACTICAL-COMBAT.md` is the implementation contract. Root coordinates Unity and shared content; disjoint skill-targeting and committed-melee workstreams prepare tests first. Exact gameplay and source corrections are recorded there.

## Implementation prompt

Implement the approved contract through failing tests, narrow production changes and passing affected tests. Keep user-selected targets authoritative; do not silently substitute bystanders. Make committed attacks use the existing combat pipeline, real scheduled turns and saved Part fields. Exercise sidestepping, solid interposition, successful versus refused control, recovery, displacement, hostility changes and save/load. Wire the behavior and cues into generated content, not a showcase-only scene. Edit Objects.json surgically, prove the parsed blueprint diff and add fresh metadata for new assets. Verify real native input, compare screenshots and record evidence limitations. Review lifecycle symmetry, cross-feature consistency, counterchecks and documentation drift; fix significant issues. Preserve unrelated work, update living docs, fetch/rebase and push the completed milestone to main. Record lower-importance pre-existing issues rather than expanding scope indefinitely.

Executed: implemented and reviewed the chosen contract. The living design records 770/770 native EditMode cases, 22/22 generated-gallery checks, 8/8 controlled directional-input checks, screenshot review and explicit evidence limits.
