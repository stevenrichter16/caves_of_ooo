# Caves of Ooo: a release vision

*Design proposal, 17 September 2026. This describes the intended release experience and separates existing foundations from proposed work. Implementation status and verification of the current regional wave are recorded in [Morrowfast style and regional situations](MORROWFAST-STYLE-AND-REGIONAL-SITUATIONS.md).*

## The game worth finishing

Caves of Ooo should become a turn-based, systemic expedition RPG about an ordinary person learning that the small acts holding a community together are also the acts holding reality together. Players should be able to pursue powerful combat builds, ingenious material interactions, dangerous discoveries and complicated loyalties. They should also be able to help, bargain, withdraw, refuse and leave something undisturbed—and have those decisions mean something.

The aspiration associated with Caves of Qud is breadth of possibility, surprising combinations and a world that feels larger than the player. That is a design direction, not a claim of feature parity. CoO's identity should come from its own world: fungal care that can become incorporation, preservation that can deny change, exchange that seeks closure, hospitality that recognizes a spoken no, and gods whose histories are uncertain testimony.

This is a persistent campaign RPG. Its release vision does not assume permadeath, mandatory restarts, run-based resets or metaprogression between disposable characters. Procedural seeds vary the world a campaign inhabits; they do not require players to abandon that campaign. Death and difficulty policies should be designed explicitly around continuity, challenge and player choice.

The finished game needs a coherent journey through these possibilities. More chunks, assets and examinable objects will not create that journey by themselves. Each major addition should improve a decision the player can actually make, an outcome they can recognize, or a reason to return.

Canon sets firm boundaries. Urqu is pressure, not a villain with a master plan. The closure-ledger recognizes **closed, refused and abandoned** acts; a deliberate refusal is closure, not moral failure. Protected questions in the Mystery Ledger must remain unanswered. The player can stake their future on a reading of Naro without discovering a developer-certified correct interpretation.

## What we already have—and what remains a promise

The current foundation includes turn-based combat, six starter spell/skill options, character advancement, equipment, crafting, alchemy, tinkering, finite harvesting, trade, rentals, repairs, farming, environmental reactions and constrained hauling. Destructible native owners and persistent zone graphs make physical changes meaningful. The world has distinct voxel compositions, named settlements, vertical sites and real routes between them.

Recent verified work gives the opening a concrete purpose: travel from the western field to Morrowfast, recover Farra's cloth, return it once, receive useful supplies, and learn directions from Vennit. Travel notes persist. Six previously repeated global stories now have canonical homes. Morrowfast's local choices, Cinderhold's faction tradeoff, guest-right and Olderdeep's pilgrimage demonstrate that useful actions can already change relationships and places.

These foundations are not a finished campaign. Boat frames do not imply boating; archival scenery does not imply usable knowledge; generated walls do not imply player house construction. Farming is not an offscreen agricultural economy. The lore's ending designs are not proof of a playable ending chain. The first Supply/Recovery wave now adds five verified finite requests, native merchant stock, persistent notes and a local habitat-preservation outcome. It is not a dynamic world simulation. The 32 inherited regression failures were repaired in R1; the latest R2 full run passed 14,931/14,931 tests. R2 also records actual delivery outcomes in persistent receipts. These bounded acceptances are not release certification; campaign, balance and distribution gates remain open.

## The player's three loops

**Thirty seconds: read a situation and commit an action.** The player notices a threat, opening, useful object or social opportunity; understands enough to choose; acts; sees the result. In combat this might be repositioning before casting into conductive terrain. Outside combat it might be inspecting a marked load, harvesting a finite row or deciding whether to accept a request. Essential danger and ownership information must survive the zoomed-out voxel camera.

**Ten minutes: prepare, travel, solve and reassess.** A lead names a place and a need. The player chooses equipment, supplies and a route, encounters a problem with alternatives, and returns or commits to continuing. Success changes more than a checklist: an ingredient becomes equipment, a delivery enters a shop, a repaired service becomes available, or someone remembers a decision. A failed expedition can produce a different plan rather than a mandatory reload.

**A campaign: acquire power, form commitments and decide what deserves continuation.** Early practical choices introduce the world's values. Middle-game journeys expose incompatible faction projects. The final acts ask players to enact a position with the knowledge, relationships and capabilities they built. The outcome should recognize important decisions without pretending every incidental item pickup deserves an epilogue.

## A release systems contract

The following matrix proposes release targets. “Current” summarizes bounded evidence, not complete certification.

| System | Current foundation | Desired release behavior | Acceptance gate |
|---|---|---|---|
| Combat | Turn-based attacks, spells, effects and reactive materials | Readable threats, viable tactical alternatives, recoverable errors and distinct encounter purposes | Complete representative encounters with several builds; explain every death through visible/logged causes |
| Character builds | XP, skill purchases, equipment and crafting | Combat, mobility, utility and preparation investments change solutions; avoid compulsory universal picks | Build-diversity playtests show different successful approaches and comprehensible costs |
| Regional situations | Two finite templates, five bindings, verified native delivery | Finite instances with actual owners, alternatives, lasting outcomes and explicit release | Independent instances; no double payment; destroyed cargo stays lost; full save during transit |
| Exploration | Biomes, named sites, vertical routes, map guidance | Leads connect curiosity to attainable goals while preserving optional mysteries | Ordinary players find destinations without source knowledge; no fabricated routes or services |
| Factions and obligations | Reputation, hospitality and several authored consequences | Useful, costly commitments; refusal and renegotiation have truthful aftermath | Each release-critical arc supports its advertised choices and handles death, refusal and changed allegiance |
| Quest and knowledge state | Global storylets, canonical hosts, persistent notes | Unique stories plus instance-safe local work; evidence enables action without resolving forbidden mysteries | No cross-instance completion; ending prerequisites obtained through actual play |
| Destructible voxel world | Native owners, multi-cell support, damage and hauling | Appearance, collision, damage and interaction agree; destruction produces understandable consequences | Footprint/owner checks, duplicate-damage prevention, pathfinding, displacement and persistence tests |
| Ecology and hazards | Water/material reactions, biome hazards, native flora/fauna | Observable local causes and consequences; preservation and exploitation both actionable | Real damage/contact paths change native state; tests distinguish untouched, damaged, removed and restored owners |
| Settlement services | Trade, restocking, rentals, copying, repair and local work | Settlements provide different preparations and opportunities without requiring a universal economy simulation | Correct inventories, affordable intended routes, loss/return rules and no reward/restock exploits |
| Crafting and resources | Weapon parts, alchemy, tinkering, harvest | Ingredients have discoverable uses and competing demands; finished goods justify expedition effort | Normal starting supplies—not developer grants—support learning and several useful progression paths |
| Farming and revisits | Active-zone growth, persistent looting/destruction | Explicit time rules and selected evolving opportunities; no invisible world-wide promise | Leaving/returning produces exactly documented changes; no unannounced respawn or resurrection |
| UI, discovery and accessibility | Journal, notes, cues, controls and voxel presentation | Consistent verbs, readable consequences, remapping and accessible visual/audio cues | New-player task completion, configurable text/contrast/audio and no information conveyed only by color |
| Saves and lifecycle | Native entity/zone serialization and regression coverage | Reliable mid-action, mid-expedition and post-choice continuation, with clear recovery from failure | Interrupted-save recovery; graph identity retained; no lost cargo, duplicated rewards or stale presentation |
| Campaign and endings | Detailed lore and partial playable arcs | A complete release-scoped path from ordinary life to meaningful ending enactments | Each advertised ending has an ordinary-play entry-to-credits playthrough and an independently reviewed consequence record |
| Performance and delivery | Headless suites, native harnesses and profiling | Stable target-hardware performance, reproducible installation and understandable known issues | Worst-case play sessions, clean packaging, crash/save recovery and explicit disposition of baseline failures |

## Chunks as places with decisions

A useful chunk grammar needs more than a terrain mask and a density slider. Start with an environmental condition, useful connections and a reason the place has its current shape. Then decide whether it is quiet transit, a resource opportunity, a social place, a hazard problem, a discovery, or a meaningful combination. Not every chunk needs a quest or a fight.

For a situation-bearing chunk, define the actual owners, standing space, approaches, visible evidence, costs and outcomes before dressing it. A wetland recovery can offer a dry detour versus a shorter hazardous approach. Protected mineral extraction can compete with bringing traded material. An occupied site can offer a conversation, a withdrawal or a destructive solution whose consequences remain visible.

Seed variation should change a decision: where the safe bank lies, which approach exposes the load, how cover interacts with a threat, or which nearby settlement offers relevant preparation. Moving identical crates a few cells is visual variation, not another problem. Conversely, preserve enough consistent visual language that players can learn what a bank, door, source or warning means.

Use a restrained voxel vocabulary: broad silhouettes, few colors per object, clear interiors, readable paths and empty space. Every interactive mass needs honest native occupancy and ownership. Decorative scenery must not suggest a bridge, harvest or passage that the simulation denies.

Revisits should follow causes. A delivered consignment may become stock; a repair may restore a service; a faction decision may change access or an encounter. A destroyed person should not return because a generator ran again. Limited scheduled or authored changes can provide renewed interest without claiming to simulate every household offscreen.

## Three proposed situations that show the intended depth

These are proposed extensions, not claims about existing content:

- **Grovelands: repair versus extraction.** A settlement needs iron to keep a useful service working. Traded iron preserves the grove; direct extraction is cheaper in currency but carries the existing Choir consequence. A release-quality version makes the repaired service visibly useful, lets the player decline, and has the people affected acknowledge the choice. The commodity delivery is implemented and verified in the first wave; the broader service aftermath still needs implementation.
- **Sodden: bring back light without poisoning the bank.** Recovering lamp oil is a physical journey. A prepared traveller can follow dry approaches; aggressive heat changes the local gas hazard. Preservation should be readable in the habitat left behind, not only a hidden bonus. The native hazard is an existing mechanic; the optional intact-bank payment is implemented with damage/removal/save counterchecks in the first wave. A later regional response must be scoped and tested before it is promised.
- **An archival journey: knowledge with a use and a cost.** A record should open a concrete course of action—an introduction, route or negotiation—while its custody creates a choice about who benefits. Several accounts may remain incompatible. Completing the journey should make the player better able to act while leaving the Mystery Ledger intact. This is campaign work still to build, using the release-scoped Recension/Curation arcs.

## Power, preparation and alternatives

Combat should remain satisfying in its own right: anticipation, positioning, clear impacts, material interactions and builds that feel different. Large effects must communicate reach and consequence without obscuring the next decision. Encounter design should include escape, protection, recovery and control—not only elimination.

Noncombat play needs actual support rather than promises that everything can be negotiated. Some problems should reward preparation, knowledge, hospitality, exchange or a careful route; others may legitimately remain dangerous. Advertise alternatives only when they exist. Release-critical progress needs redundancy or an explicit consequence path when a necessary actor dies or an object is destroyed.

Crafting and trade should turn expeditions into new options. Give important materials competing uses, but explain those uses early enough for an informed choice. Keep economic scope bounded: delivered stock, selected shortages and authored requests are achievable without pretending caravans, production and prices form an autonomous economy.

Vein Pressure is an optional, risky prototype, not a prerequisite for this release vision. The deferred concept could make wounds, stimulants and surges more strategic, but it touches damage, toxins, heat, action speed and body types simultaneously. Preserve the current injury/HP model during a small experiment; require readable low/steady/surging/overpressure states and a complete encounter test before considering replacement. Do not postpone an otherwise complete game for an unproven combat rewrite.

## The journey and its scope

**Early game: being useful without being chosen.** Morrowfast should teach the game's grammar through ordinary needs, hospitality, material choices and optional commitments. Establish reliable controls, preparation, one regional journey, a social consequence and the possibility of an enacted no. Explain how this actual opening relates to the lore's Sill origin framing rather than silently treating two different places as the same tutorial.

**Middle game: competing ways to keep a world.** Connect the existing geography through release-scoped faction arcs. The Choir's care, Curation's preservation, the Recension's records, Tent-Right's practice and the catacomb villages' continuity should become decisions with practical consequences. Trade and preparation matter throughout. Evidence arrives through action, testimony and places; it should not become a sequence of lore lectures between generic fights.

**Late game: commitments become enactments.** Follow the canonical dramatic triage: five deep factions, two mid, three background; full or braided arcs where load-bearing, shorter agreements elsewhere. Selen's name is not merely a delivery token. Giving, bargaining with or withholding it should produce distinct consequences. Naro's evidence supports a stake, not a final answer key.

Advertise only endings that can actually be reached. Consume, Preserve and Renewal should preserve their different costs rather than collapse into a morality ladder. Practice-path Renewal involves ongoing civic work and loss, not a universally perfect ending. A clean closure-ledger can belong to a principled refuser as well as someone who completes many tasks. Unaccepted optional content must not become an invisible universal obligation; define exactly what counts as undertaking an act and how the player can end it. Repeating easy requests must not erase unrelated abandoned commitments or turn cosmic closure into a farmable reputation score.

## A dependency-led roadmap

**1. Stabilize the verified slice.** The coarse Morrowfast restyle and first regional templates now have focused, full-regression and native acceptance evidence. The 32 inherited failures are repaired, and the native opening has passed without debug protection. Continue targeted readability, input and transaction repairs while preserving that opening. Keep the implementation record explicit about its limits. Establish production visibility/debug defaults explicitly; preserve full-reveal as a chosen mode rather than silently overriding user preference.

**2. Make the regional loop repeatable before multiplying it.** Verify instance identity, quantities, inventory transactions, refusal/release, cargo loss, habitat outcomes and save/load. Playtest whether each binding creates a meaningful choice. Then expand a small number of situations into better variants; do not add dozens of nearly identical errands.

**3. Build one complete middle-game chain.** Connect real settlements, one vertical discovery, preparation and a faction tradeoff. Ensure every prerequisite is normally obtainable. Test multiple builds and action orders, including refusal and losing a participant. This establishes the campaign architecture before spreading writing across every faction.

**4. Implement the closure and ending spine.** Agree on transparent undertaken/refused/abandoned semantics, then build the smallest complete route to an ending and its consequences. Add the other advertised ending routes through the same robust state architecture. Keep mythological uncertainty separate from mechanical ambiguity: players can understand what they are committing without knowing the cosmic truth.

**5. Complete release-scoped depth.** Expand the five deep/two mid/three background plan only as far as the chosen release promises. Make ordinary revisits, preparation and build progression sustain the journey. Defer systems whose principal contribution is scope rather than a stronger decision.

**6. Run a release gate, not just a test suite.** Recruit players unfamiliar with the code. Observe comprehension, boredom, avoidable confusion, build viability and recovery from mistakes. Test target hardware, worst-case effects, controls, accessibility, audio, installation and save interruption. Classify every known baseline failure; “unchanged from before” is useful regression evidence but not release approval. Publish only claims the shipped build can demonstrate.

The goal is a world where a player can say what they wanted, what they tried, what happened and why it mattered. CoO already contains pieces of that game. Finishing it means connecting those pieces into a legible, consequential journey—not filling every empty cell.

## Evidence and planning references

Current executable scope: `Docs/CHUNK-GAMEPLAY-IMPLEMENTATION.md`, `Docs/CHUNK-GAMEPLAY-NATIVE-AUDIT.md`, `Docs/CANONICAL-VILLAGE-QUESTS.md`, `Docs/REGIONAL-GUIDANCE.md`. Historical diagnosis: `Docs/CHUNK-GAMEPLAY-AUDIT.md` (read as a dated snapshot). Presence/accessibility: `Docs/VOXEL-WORLD-ACCESSIBILITY.md`, `Docs/FACTION-AND-SACRED-POI-AUDIT.md`. Canon: `Lore/10_Bible.md`, `Lore/11_SecondSpine.md`, `Lore/MYSTERY-LEDGER.md`, `Lore/Design/V1-DramaticCore.md`. Deferred combat proposal: `Docs/VEIN-PRESSURE-DESIGN.md`. Supporting source review: [release-vision evidence](RELEASE-VISION-EVIDENCE.md).
