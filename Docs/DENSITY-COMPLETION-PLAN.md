# Content density completion — implementation plan

**Status:** planned before implementation, 26 September 2026. Execution is
authorized by the user's instruction to analyze the gaps, write a thorough plan,
write an execution prompt, and implement it. Baseline: `50ef23d2` on `main`.
The companion [execution prompt](DENSITY-EXECUTION-PROMPT.md) drives this work.
Every milestone below remains open until its own acceptance evidence is recorded.

## 1. Outcome and completion contract

Walking through a new campaign should offer distinct tactical encounters,
worthwhile equipment decisions, readable discoveries, useful preparation and
recognizable local people. Existing systems should supply these experiences in
ordinary play. An implemented class, passing factory test or developer grant is
not evidence that the player can find and use the content.

The reconnect-and-repair tranche is already shipped. This plan completes the
remaining density work in independently reviewable milestones; it does not
restart Phase 1 or claim parity with Qud's total content volume. Larger expansion
milestones are explicit work, not silently included in an early completion claim.

A milestone is complete only when it has: verified source premises, recorded
RED before production changes, positive and negative controls, relevant save and
adversarial coverage, native checks for its player route, reviewed living docs,
and a commit. Integration requires zero new standalone differential failures,
an unfiltered Unity EditMode pass, real input-driven Play-mode evidence and a
fresh world/loot census. Balance judgments require representative ordinary-stat
play; invincible or staged demos cannot establish balance or natural acquisition.

## 2. Constraints and decisions

- BitLocker remains dev-only. Tinkering activation is excluded by user direction.
- Preserve the Sill-omen/ending Urqu rule. Urqu is pressure, never a spawned villain.
- Preserve neutral GlassScorpion, plain Shambler without spores, silent Gin Frogs,
  the Mystery Ledger, cultural voices and authored regional restrictions.
- Choir containers remain natural; no manufactured gear in sacks/baskets/logs.
  Friendly/service NPCs are not upgraded into reward targets. Weapon and armor
  rewards from fighting must come from visibly carried/equipped items.
- Rentals, generic crafted outputs and uniquely sourced artifacts stay outside
  generic find pools. More loot must preserve meaningful shops and crafting.
- Existing saves retain serialized parts/content unless a specific, tested
  migration is justified. New content is accepted for fresh campaigns; code fixes
  may improve old entities without silently rebuilding them.
- Do not resolve protected mysteries in generated names, rumours, books or bosses.
  Reuse canonical Codex prose rather than inventing authoritative explanations.
- Fire is not changed from a numeric test alone. Its milestone includes bounded
  native play and a counter-scene before integration, honoring the prior deferral.
- New ranged creature actions use existing spell/natural attack systems. Guns,
  ammunition economies, identification and permadeath are not required to close
  this content-filling pass; they remain separate design proposals.
- Preserve all unrelated tracked and untracked work. Root coordinates surgical
  `Objects.json` edits, validates parsed differences, and supplies fresh metadata
  for new Assets sources. One owner controls Unity at a time.

## 3. Pre-implementation verification sweep

The September census is historical. Its percentages are not current acceptance
targets. These corrections are verified against tracked baseline source.

| Old premise | Current evidence | Consequence |
|---|---|---|
| Existing content is still broadly disconnected | `DENSITY-PHASE-1.md` T2 and §11 record live loot, encounters, stamps, traps, liquids, directions and examination | Preserve those repairs; start from `50ef23d2` |
| Butchering is absent | `CorpsePart` passes family harvest configuration into `HarvestablePart`; corpse and mineral Harvest already work | Expand coverage/yields only where missing; do not invent a second harvest system |
| No cooking behavior exists | Material reactions already transform raw meat/starapple under fire | Add an explicit safe cooking action and missing authored products; share existing products |
| No hazard-aware pathing exists | `GasNavigationWeight` already contributes to A* | Extend existing costs for actual uncovered liquid/tile hazards; retain gas behavior |
| Steam is inert | `SteamEffect` cools and wets neighbors | Preserve those reactions; scope added scalding/visibility to real gaps |
| No rest, light or doors exist | `RestSystem`, equipped LightSource aggregation and Morrowfast doors work | Add player-facing gaps, not duplicate mechanics |
| All Codex text is absent | Six plaque-wall entries are already authored/examinable | Add discoverable documents and reading without deleting existing plaques |
| Conversation guidance is missing | Scribes, innkeepers and Morrowfast clerk already supply real leads; signs now do too | Add local identity and dialogue, preserve actual destination/work checks |
| Enemy skills can simply be added to blueprints | `KillGoal` melees when adjacent; ranged helper skips adjacent abilities; some skill executors choose the first neighbor, and weapon skills require equipped classes | Fix target/eligibility/energy semantics before granting kits; reject unsafe ally-intercepted casts |
| All same-faction actors should assist | Beasts includes unrelated ecology | Assistance is authored opt-in for social combatants, with sight/radius and hostility gates |
| Armor find pools exist | Six weapon/offense pools ship; no `FindArmorT1..3`; 12 concrete armor types exist | Reconnect current armor first; preserve legacy loot rows |
| High-tier loot is only a table wiring job | Container/death selection clamps to T3; no current Item-tagged Tier4+ blueprint | Add actual higher-tier authored variants and measured sourcing as a separate milestone |

Before each milestone, append exact API/content corrections discovered during
its own sweep. Each specialist doc must identify code paths and authored sources.

## 4. Milestones and acceptance

### C0 — Baseline and measurement

Preserve baseline commits/test receipts and add a reusable multi-seed census.
Use at least five explicit seeds, all surface biomes, depth bands 1/3/5 and
lairs. Count encounter groups/species/roles, empty zones, interactable verbs,
readables, containers actually opened, item categories/tier/value, visible
equipment and dropped loot. Separate eligible natural/quiet regions from
manufactured regions; never demand a hostile or manufactured item everywhere.

Output distributions and source provenance, not only totals. Record excluded
content and failed placements. Capture before changing loot/population and after
all milestones; never relabel the old single-seed census as current.

### C1 — Armor finds and legible hostile gear

Complete the existing `LOOT-FINDS.md` armor rates with three nested pools and
15 eligible container references. Retain existing rows and all natural-container
and friendly-NPC rules. Upgrade a named roster of hostile humanoid loadouts using
existing armor/weapon tiers; coordinate with C2 so weapon-family skills have the
correct visible equipment and shields cannot displace the intended weapon.

Acceptance: registry validation, real rolls with tier/category floors, zero-rate
and hostile/friendly controls, factory equipment and actual death spill, seeded
per-source/per-zone counts and sale-value changes. Native proof opens a naturally
generated container and observes an equipped hostile's drop. No balance claim
from expected-value math alone.

### C2 — Tactical enemy encounters

Use an opt-in tactics component and a deliberately bounded ability allowlist.
Select useful abilities before melee/movement with authored chances and a melee
fallback. Support actual chosen targets, cooldown/resources, range, alignment,
line of fire and friendly-intercept safety. Avoid granting all player spells to
all NPCs. Author ten kits across existing melee/ranged enemies, with at least
three real ranged threats and multiple adjacent actions. Candidate roster:
SnapjawScavenger, SnapjawHunter, DesertBandit, AmbushBandit, SnapjawChieftain,
SnapjawWarlord, CaveSlime, IceWight, CharredHusk and RuneCultist. Final mappings
must match equipped classes and authored creature identity.

Add opt-in same-faction assistance with radius/visibility/hostility checks and
per-creature flee thresholds. Do not rally every Beast. Preserve neutral,
passive, friendly, party and mind-control semantics. Emit chosen/rejected action
diagnostics with reasons; avoid per-frame work.

Acceptance: actual factory actors execute attacks through ordinary turns;
adjacent/ranged success has cooldown, blocked, off-ray, ally and chance-zero
controls; failures do not double-spend energy or freeze melee. Dedicated
adversarial tests cover targets dying/moving, cross-actor ownership, save/load,
RNG determinism and summon/effect side effects. Native representative fights use
ordinary stats and at least melee, ranged and control builds.

### C3 — Water and explicit cooking

Provide a purchasable/found waterskin and usable fill/drink actions using existing
water sources and Parched semantics. Initial water capacity is three drinks as
specified by `DAY-TO-DAY.md`. Validate ownership, adjacency, finite volume,
stack identity, empty/full state, unsafe/non-water sources and save/load.
General liquid carry/pour follows as a separate substep with liquid identity,
volume conservation and existing material/contact reactions; mixing or unsafe
drinking must never silently become clean water.

Add explicit cooking beside a real usable fire/oven/hearth, sharing existing
raw-meat/starapple products and adding the planned mushroom recipe. Revalidate
the source and ingredient at execution; output capacity and failed transactions
must not delete ingredients or create duplicates. Define one turn per successful
batch; refusals are free. Stock sources and ingredient descriptions make the
loop discoverable. Native proof buys/fills/drinks a skin and cooks a real stack.

### C4 — Clock, rest and ordinary interaction clarity

Show current surface/depth day band. Add rest-until-next-band to actual rest
sites under existing payment, hostility and healing rules. Preserve ordinary
rest; bound turn advancement and avoid repeated healing/reward exploits.
Add player sit/sleep/light actions only where the world actually supports them:
respect chair/bed ownership, working lights, extinguishing and equipped-light
aggregation. Add missing door interactions to applicable generated buildings,
without breaking reserved approaches or authored native footprints.

Review player-subject log grammar using a narrow tested grammar layer; never
rewrite quoted lore/NPC dialogue accidentally. Native checks confirm band text,
time advance, refusals, furniture ownership and readable action results.

### C5 — Found texts and authored examination

Expose the 13 canonical Codex artifacts as readable, non-consumable documents
with stable IDs, provenance, complete paginated text and ordinary inventory/world
Read actions. Preserve original prose and formatting meaning. Match placement
to libraries, tombs, appropriate merchants and authored sites; protect unique
story provenance and do not repeat guarded revelations as universal facts.
Keep existing plaques. Reading does not spend turns, grant invented powers or
resolve mysteries; remembered reading state, if supplied, must be saved and
must not prevent rereading.

Write concrete examine prose for remaining frequently encountered equipment,
plants and scenery, prioritizing census frequency and useful clues. Mechanics
continue to come from live descriptions, so flavor cannot contradict forged or
enhanced instances. Respect culture voice cards and blind-review newly authored
substantial dialogue rather than grading one's own prose.

Acceptance: all documents have real sources; action ownership, stale selection,
repeated read, save/load and missing-text controls; all pages/characters remain
reachable at native display bounds; actual found book opened with keyboard.

### C6 — Recognizable local people

Add deterministic culture-aware names for generic residents while retaining
their visible role and any authored proper name. Avoid renaming quest owners,
blueprint identity or save keys. Enforce one-per-world placement only for truly
unique fixed-name characters, with persistent ownership and death/revisit
controls; generic roles remain repeatable.

Replace generic place-blind villager lines with bounded local dialogue driven
by actual settlement/biome and existing state. Preserve repair/quest predicates,
canonical hosts and mystery boundaries. At least one local observation, practical
lead and changed-state response per supported settlement culture. Tests cover
determinism, collisions, old saves, dead/absent owners and no fabricated services.

### C7 — Ambient life and travelling encounters

Implement the existing authored sari ambience plan with tier/spacing/ending
rules, preserving Sill's unique tier-1 inciting beat. Add restrained contextual
NPC remarks with visibility/hostility/silence gates and shared throttling.
Use no gameplay RNG for purely cosmetic variation and never reveal unseen foes.

Add a bounded zone-entry traveller encounter system with persistent IDs,
one-time entry rolls, legal standing space, real origin/destination leads and
ordinary talk/trade/withdrawal options. Explicitly distinguish entry encounters
from a fully simulated offscreen caravan economy. If actual cross-zone movement
is added, conserve entity/inventory identity and test revisit/death/save behavior.
Do not respawn unique travellers or create infinite trade stock by crossing an edge.

### C8 — Connected environmental behavior

Unify authored combustibility scale and verify TarSeep volatility against actual
materials. Connect object/creature and tile heat without recursive double damage
or multiplying reactions. Extend smoke visibility and temperature-dependent
steam scalding while preserving existing wetting/cooling. Extend existing gas
navigation costs to uncovered liquid/tile hazards with creature immunity and
escape controls. Add meaningful sources for still-unsourced authored liquids
and gases only after verifying their interaction and biome fit.

Acceptance: matching wet/dry, volatile/inert, smoke/clear, hot/cool and immune/
susceptible controls; finite propagation and save/load; no unbounded per-turn
zone scans. Native ordinary-stat fire/water/oil encounter and control scene must
show readable damage, extinguishing and escape. This is the required playtest
gate before changing the previously deferred global fire behavior.

### C9 — More useful scenery and corpse coverage

Use the new census to inventory examine-only features. Assign Read, Harvest,
Open, Rest, Light or another existing meaningful action where authored identity
supports it; intentional decoration remains decoration. Expand existing corpse
harvest configuration across suitable biological families, conserving the
single-use/overflow/ownership rules; constructs and protected entities have
explicit counter-cases. No fabricated useful verb solely to increase counts.

Acceptance: an explicit disposition for every census-listed look-only feature
and corpse family, real sources/products, intact footprints and native examples.

### C10 — Underground and lair progression

Add a second cave layout and depth grammar with observable choices: roomed ruins
versus natural chambers, depth-appropriate terrain/resources and distinct enemy
roles. Add multi-level lairs with reciprocal stairs, persistent graph identity,
safe arrivals and boss placement tied to a recorded final level. Choose bosses
by biome and tier without forcing manufactured content into Choir formations.

Acceptance: multi-seed connectivity, exits, no body/trap overlap, unique boss and
loot ownership, descent/ascent/revisit/save round trips. Native ordinary travel
reaches and leaves at least one new layout and multi-level lair. Keep authored
Overwrit/Stump routes and existing fixed settlements intact.

### C11 — Higher-tier, modified and exceptional finds

Extend progression above T3 with real authored item variants and tier-aware
source rules; then add bounded random enhancements through existing enhancement
contracts. Preserve RNG replay, inspection truth, sale value and equipment
refresh semantics. Add a small legendary template system with saved name,
epithet, allowed kit and visibly carried reward; no unique lore actor cloning.
Expand creature variants by biome/depth only where they introduce a distinct
encounter role, with actual model/glyph coverage and authored natural weapons.

Acceptance: no generic unique/rental/crafted leakage, compatible enhancements,
zero/100% controls, save/revisit identity, meaningful tier differences and actual
natural sources. Extra armor slots or large new status-effect systems require a
separate rules justification; varied existing equipment/effects satisfy the
content goal without claiming those optional Qud-volume features are complete.

### C12 — Final density, economy and campaign verification

Repeat C0 with identical seeds and definitions. Report deltas and distributions,
including quiet regions, actual loot and unique content—not just authored counts.
Play representative early/mid/deep routes with ordinary starting supplies and
different builds. Evaluate threat readability, recovery, item frequency/value,
preparation, return visits and protected lore. Fix observed defects with RED
tests; do not weaken tests to hit a density target.

Run exact-corpus standalone before/after comparison, native full EditMode and
finite input-driven scenarios. Preserve raw results and rejected runs. Review
Q1 symmetry, Q2 public contracts, Q3 branch/counter completeness and Q4 docs;
perform independent adversarial and player-flow hypothesis reviews. Record
remaining uncertainties honestly, fetch/rebase and push verified commits to main.

## 5. Execution order and ownership

First wave: C0 baseline plus C1 loot, C2 tactics and C3/C4 preparation can be
developed in parallel with disjoint source ownership. Root owns blueprint merge,
shared UI integration, C5 documents and Unity scheduling. Then C6/C7 people and
ambience, C8/C9 environment and interaction coverage, C10/C11 progression, and C12.
Dependencies are C2→C10/C11 kits; C3→C8 liquid conservation; C5→C6 knowledge;
C0/C1→C11 economy. Source edits stop during native runs.

For each milestone: sweep → failing test → minimal implementation → matched
controls → dedicated adversarial suite → focused/native acceptance → self-review
→ living doc → commit. Private runner copies replace stashing shared production
files; both sides must use the same named test corpus. Full sweeps are integration
gates, not a substitute for focused RED evidence.

## 6. Status ledger

| Milestone | Status | Evidence / next gate |
|---|---|---|
| C0 | Planned | New baseline census before loot/population edits |
| C1 | Planned | Armor/loadout sweep; baseline and RED |
| C2 | Planned | Target-safe ability design; baseline and RED |
| C3 | Planned | Water/cooking transactions; baseline and RED |
| C4 | Planned | Clock/rest/furniture/grammar sweep |
| C5 | Planned | Canonical document registry and paginated Read RED |
| C6 | Planned | Generic/unique identity census and voice review |
| C7 | Planned | Sari/remarks/traveller persistence sweep |
| C8 | Planned | Thermal/liquid/gas contracts and native fire gate |
| C9 | Planned | Action and corpse coverage census |
| C10 | Planned | Cave/stair/lair ownership and progression sweep |
| C11 | Planned | Authored tiers/mods/legendary contracts |
| C12 | Planned | Final census, ordinary-stat play, full suites |

## 7. Implementation log and self-review

- 2026-09-26: verified shipped baseline, current gap corrections and preserved
  constraints; authored this plan and execution prompt before production edits.
- ⚪ Content completion is bounded by the explicit milestones above, not the
  historical Qud comparison's speculative volume or an unsupported percentage.
- ⚪ Optional guns, new anatomy slots and hundreds of effects are separate rules
  expansions. They are named exclusions, not silently marked implemented.
