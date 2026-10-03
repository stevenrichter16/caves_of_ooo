# Equipment discoveries — executed working prompts

## 1. Brainstorm

Audit the current component-to-weapon-to-skill path and the actual equipment damage pipeline. Find the smallest set of original, geographically grounded discoveries that changes a learned build's choices. Prefer existing combat mechanics and destinations. For each candidate, name its use, cost, source, ordinary acquisition route, visual identity, and counterexample where another item is better. Do not grant skills through equipment or add a new quest framework. Preserve natural-only wilderness loot and literal saved inventories.

**Executed:** three independent source, forging and defense/presentation audits. Selected three heads and two defenses. Rejected extra stun stacking, acid protection marketed as mire immunity, and geographically generic loot.

## 2. Design

Turn that audit into a living implementation design with verified source references, a corrections table, exact authored values and geography, save behavior, UI and model requirements, and measurable acceptance criteria. Trace each promised benefit into existing gameplay. Plan failing tests, counter-checks, adversarial checks and an ordinary native crafting/equipment demonstration. Explicitly distinguish original design from any reference-game comparison.

**Executed:** `EQUIPMENT-DISCOVERIES-DESIGN.md` records the contracts and sweep before production changes.

## 3. Implement and review

Implement the approved design from the smallest contract outward. Observe failing tests before production changes. Repair the existing steel family connection without recomputing saved damage. Supply each new component in a real, geographically specific inventory with usable companion parts. Make defenses work for actual actors lacking resistance stats, while preserving unrelated bonus behavior. Create and wire original ground, assembled and worn forms; show families and tradeoffs in ordinary UI. Run native Unity tests, source acquisition and crafting/equipment checks, inspect rendered evidence, review symmetry/cross-feature consistency/counterexamples/doc drift, and fix significant findings. Record exact evidence and limits in the living design. Stage only this work, fetch/rebase, and push authorized main.

**Executed:** all three heads and both defenses, their finite sources and 18 model forms, steel-family save correction, UI and equipment lifecycle fixes. Native verification: 1575 unique selected passing tests (164 new cases); ordinary Sodden acquisition/craft/equip/save journey 14/14, zero errors. See the living design for raw evidence, review findings and limits.
