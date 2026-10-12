# Spread opening: discoveries that change decisions

Status: implementation complete; M0–M5 automated/native review completed with the benchmark and human-playtest limits below. Cursor Vault revision implemented and verified. 11 October 2026.
Source baseline: `5f0a76686` on main. Sections 1–10 record the original source/design review; sections 11 onward record implementation and new Unity evidence. The attached assessment is a hypothesis to investigate, not proof of balance or discoverability.

## 1. Design brief and planning prompt

Execute this brief: audit the existing opening, identify where an intention, reward or consequence fails to produce a useful decision, and design the smallest connected improvement. Specify actual locations, objects, encounters, alternatives, persistence, implementation owners and falsifiable acceptance criteria. Compare every starting build. Preserve free exploration and existing systemic solutions. Distinguish observed behavior from proposed changes and untested experience. Stop at an implementation-ready design.

The player-facing promise is: **recover a useful technique, use it to approach another place differently, and return with a reason to prepare for something else.** Repair and production support that promise; understanding the entire district is not an entrance requirement.

Five priorities, in order:

1. Make one attractive first expedition legible from spawn.
2. Make its guaranteed reward change an immediately available approach.
3. Compare Calm, fighting, supplies and withdrawal under ordinary rules.
4. Correct the mismatch between routine loot and permanent injury.
5. Make revisits reveal useful changes without repeated administration.

No new skill tree, quest framework, regional currency, compulsory tutorial, enemy roster or generic marker system. No global Calm/rest nerf in this tranche. No countdown invented to punish preparation. Caves of Qud is the user's experiential reference: improvisation, strange places and discoveries with practical consequences. This document makes no claim of independently verified Qud parity and copies no named creatures or lore.

## 2. Verification sweep: what actually exists

| Finding | Source checked | Design consequence |
|---|---|---|
| The cellar already has a guarded direct corridor, service passage, movable beam and permanently open detour. | [GleanersCellarBuilder](../Assets/Scripts/Gameplay/World/Generation/Builders/GleanersCellarBuilder.cs) | Improve its purpose and payoff; do not rebuild its successful topology. |
| Its finite crate contains two FireClay and an even-seed charged Shattered Rime book or odd-seed Buckler. | Same builder; [Objects](../Assets/Resources/Content/Blueprints/Objects.json) | The first new capability currently depends on seed. Duelist and Breaker already carry a Buckler. |
| An existing optional satellite teaches Vault through Ditchkeepers' Footwork. It also supplies clay or food. | [ConnectedSpreadSatellite](../Assets/Scripts/Gameplay/World/Generation/Builders/ConnectedSpreadSatellite.cs); Objects | Reposition an existing reward rather than invent another ability. |
| Vault crosses a two-cell distance, including an intervening solid tile; it requires a valid unoccupied landing and respects sealed archive barriers. Successful use gives a brief melee accuracy opportunity. | [Acrobatics_Vault](../Assets/Scripts/Gameplay/Skills/Acrobatics_Vault.cs) | A visible low barrier can demonstrate utility for every current start. Do not promise unrestricted wall traversal. |
| Calm has range 6, cooldown 20 and a fragile NoFight goal of duration 50. Goal age advances through AI processing. Damage breaks its peace. | [Spellcraft_Calm](../Assets/Scripts/Gameplay/Skills/Spellcraft_Calm.cs); [NoFightGoal](../Assets/Scripts/Gameplay/AI/Goals/NoFightGoal.cs); [BrainPart](../Assets/Scripts/Gameplay/AI/BrainPart.cs) | Do not equate its duration with 50 world-clock ticks or assume unlimited safety while attacking. |
| Rest heals HP and ordinary bleeding, advances the world clock by 60, and checks faction hostility within eight cells. | [RestSystem](../Assets/Scripts/Gameplay/Settlements/RestSystem.cs) | Calm does not itself remove the hostility gate. Clock advancement is not 60 normal NPC combat rounds. Rest does not restore limbs. |
| General melee trauma can sever appendages after sufficient actual HP damage. It does not require an axe or the Dismember skill. | [CombatSystem](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs), CheckCombatDismemberment | A blunt weapon swap alone cannot make an opening actor safe from incidental severing. |
| A recorded yard run lost feet after a 16-damage retaliation; another recovered one dried meat and two coins. These are separate runs. | [Trapper's Dispatch Yard](TRAPPERS-DISPATCH-YARD.md), native evidence | This establishes a possible risk/reward mismatch, not its frequency or average loot. |
| The meal trades ingredient healing for one-action healing plus removal of ordinary bleeding. Existing local production, permissions, cultivation and re-inking already work in recorded journeys. | [Connected exploration design](SPREAD-CONNECTED-EXPLORATION-DESIGN.md), especially sections 18 onward | Avoid reimplementing these systems or asserting every meal use was beneficial: one recorded meal was eaten at full HP. |
| Shattered Rime is a short cone consuming resonance marks; its armor effect requires consumed marks. A cold cast is deliberately weaker. | [Rites_ShatteredRime](../Assets/Scripts/Gameplay/Skills/Rites_ShatteredRime.cs); [ConsumingRiteSkillBase](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs) | It is a poor universal first lesson without the supporting setup. Vault has a simpler visible payoff. |
| Current specialist inventories differ substantially. | [StartingBuilds](../Assets/Resources/Content/Data/Builds/StartingBuilds.json) | No test-only inventory grants; no assumption that everybody has Calm, a shield or a cold setup. |

Correction to the assessment: comparing Calm's 50 to rest's 60 as though they were the same unit would be invalid. Correction to an obvious proposed fix: changing a Scrabbler's hatchet to a cudgel would not fix the general trauma rule. A regeneration method exists on Body, but this audit has not established a dependable early player-accessible limb-restoration service. The design must not promise one.

Existing successful first-hour and connected-exploration implementations remain authoritative descriptions of shipped behavior. This document proposes a subsequent revision; it does not relabel those earlier plans as unfinished.

## 3. The connected opening

### Beat A: an understandable attraction

At the reference glade (`Overworld.11.10.0`), make the cellar entrance and its short notice the strongest local expedition cue. Proposed first line:

> Ditchkeepers left a book of crossing tricks and repair clay below. Scratching carries from the broad passage; an older service passage winds around it.

This is a historical lead, not a remote inventory detector. After visiting, local inspection can describe the actual opened or depleted crate. Never rewrite the surface notice with information the player could not have acquired.

Keep the well, garden, fire and regional notice interactive immediately. Give their brief inspection one concrete use each; keep recipes, extended lore and other destinations in existing full Examine. Do not hide commands, impose an objective or require accepting dialogue. If a player walks elsewhere, nothing blocks them and no unsolicited tutorial follows them.

Visual change: reuse the current cellar, notice and clay models; give the entrance a clear silhouette and a small, static arrangement of worn crossing stones. No glowing route ribbon. At Deck resolution the stairs must remain distinguishable from crops and clutter in ordinary lighting.

### Beat B: earn a technique by choosing a route

Preserve the cellar's broad guarded passage and long covered service route, including the walkable detour around the beam. Calm remains a valid single-guard solution; a player who brought it should feel clever. Hauling, fighting, maneuvering and withdrawal remain ordinary actions.

**Proposed finite crate contents:** two FireClay, one Ditchkeepers' Footwork, and the existing seed-dependent book/Buckler. The guaranteed manual is the main expedition reward. The old equipment remains supplementary salvage, so no start loses its previous reward. Do not auto-read or auto-equip anything.

Read teaches the existing Vault skill without an SP or ink grant. Already-known Vault remains a normal already-known read, not a substitute reward roll. The manual remains a physical tradeable item. Failed generation publishes neither partial rewards nor a half-built cellar.

To avoid making the later satellite's signature reward a predictable duplicate, change only its authored manual slot to one charged Shattered Rime book. Preserve its current supplies and rolled container stock. Even-seed players may value a second physical charged book; odd-seed players receive their first. This is a known asymmetry, but the universally useful first movement capability no longer depends on it. Do not create a build-sensitive loot director to eliminate every duplicate.

### Beat C: a visible opportunity, not a test gate

The next suggested destination is the existing trapper yard one world-map step south (`Overworld.11.11.0`). It is optional and may be visited first. Its full authored arrangement remains conditional on valid source admission; do not promise the yard or its supplies when admission fails. A locally discovered notice can describe the actual admitted arrangement. The general southern lead is a historical lead, not confirmation of surviving stock.

For a seed-independent first demonstration, also place one low crossing barrier beside the glade cellar return path, with an open walk-around and visible landing. It is a static obstacle, not a trap or a special ability trigger; it must not block stairs, spawn or existing owners. Stage and validate it with the new-world glade layout. If placement cannot satisfy those constraints, reject that proposed layout during generation validation rather than publishing an obstructed spawn. This allows an immediate use even when the yard arrangement is absent.

Modify the yard’s western service apron to include a low, one-cell-thick barrier with clearly visible clear standing and landing cells two cells apart. Keep the open walking route around this demonstration segment. The segment is outside the enclosure: Vault can shorten the approach or gain position, but entering it does not teleport the player into both enemies. Reuse the yard's stone and timber palette with a lower broken silhouette and scuffed landing tile.

Within the existing yard, preserve the trap opening, movable beam, walls, cache and ranged/melee roles. Audit a legal vault across the service boundary with a visible landing, but never guarantee that an NPC will leave that landing free. If the landing is occupied, normal refusal is free and the player can walk, haul, jam or withdraw. Do not create a special Vault-only interaction.

The existing soursprayer's line of fire and the melee actor's position should make cover useful. Ordinary AI remains authoritative: a creature can move, trigger the trap, block the route or leave the cache. No hidden encounter director replaces a dead opponent or restores a spent trap.

**Proposed cache floor:** add one HealingTonic and two SalvagedTimber to the admitted original cache, preserving its rolled stock. These are finite practical salvage, not a promise of exceptional equipment. Two timber can support another obstacle choice; the tonic can support continued travel. Capacity and identity must be validated before publication. Failed admission must not delete the original roll or silently mint a second container.

### Beat D: choose what the return is for

The first two clay still cannot repair both the well and kitchen pan. Present the benefits where the player evaluates those objects:

- Well: local usable water for cultivation and preparation; no invented thirst emergency.
- Pan: a batch meal combines healing and ordinary bleed treatment into one action; separate ingredients offer more total healing.
- Keep clay: leave both repairs for later and carry onward.

No dialogue quiz or forced wound demonstrates the meal. A player who has not needed compressed treatment may rationally decline it. Likewise, making ink matters after using a charged book, not because a mandatory arrow points to the desk.

Preserve current crop growth and production while legitimately away. On return, existing objects show ripe/dry/depleted, cracked/working, preparing/ready, and stocked/collected states. Use these states instead of another region-wide progress dashboard. A ready meal should visibly differ from an empty pan; inspection must state the actual pending or ready status without repeatedly narrating the entire recipe.

## 4. Opening injury policy

**Decision:** the two authored introductory encounters should retain ordinary lethal HP combat, but their ordinary melee actors should not inflict incidental permanent limb loss through the generic high-damage trauma roll. This is a bounded authored encounter policy, not player invulnerability or a global removal of anatomy.

Implement an explicit attacker capability for incidental melee trauma, defaulting to today's behavior when absent. Proposed representation: a small serialized Part, `MeleeTraumaProfile`, with `AllowIncidentalDismemberment=false` on the actual staged opening actors only. Resolve it from the true damage source in the generic melee trauma path. It must not suppress HP damage, bleeding, armor interaction or death, nor intercept unrelated traps, spells or deliberately authored dismember abilities.

Apply to the cellar guard and both admitted yard actors on new-world generation. Their examined descriptions state that their ordinary attacks batter and wound, with no incidental limb-severing capability. Keep their existing visual identity and loadout; do not falsely claim a cudgel makes this universally true. An actor keeps its capability if it pursues the player out of the location. Avoid coordinate checks, tutorial buffs on the player or a blanket defender event veto.

Before implementation, trace all generic-trauma callers and explicit severing paths. If these actors can invoke an explicit severing skill, remove that skill from the authored profile or stop this milestone for a coherent revision; do not silently exempt it from the promise. Profile tests must use actual equipped and natural attacks and compare the same actor without the Part. Other Scrabblers retain their behavior and must not inherit this policy by blueprint accident.

This explicitly trades some simulation uniformity for a readable opening risk envelope. It is preferable here to adding a free universal regrowth fountain or disguising permanent injury with a larger loot roll. Outside this small scope, severe injury, telegraphs and accessible recovery still need a separate wider audit. No global rarity or fairness conclusion is claimed.

## 5. Build and solution comparisons

| Start | Viable initial route | Immediate new option | Question for the yard |
|---|---|---|---|
| Duelist | Covered route or normal blade combat | Vault and its brief adjacent attack opening | Does landing position support attack or escape without forcing a trade? |
| Breaker | Haul, detour or cudgel tools | Cross a barrier without moving it | When is hauling a persistent route more useful than a personal leap? |
| Stormcaller | Calm, terrain control or detour | Movement independent of elemental setup | Which opponent matters now, and can cover handle the other? |
| Bombardier | Calm, finite supplies or detour | Save a supply by changing position | When is spending a tonic or grenade worth its opportunity cost? |
| Classic | Existing broad kit or detour | A clear traversal tool | Does it add a choice rather than another redundant hotbar button? |

These are hypotheses, not prescribed solutions. Keep all ordinary approaches legal. Do not add Calm immunity, cooldown-resetting ambushes or a compulsory second enemy simply to negate a successful player tool.

Compare the same seeds and starting kits: bypass/control; melee; finite consumables; withdrawal to a legal rest site. Record paid actions, actual scheduler/world time, HP and injuries, supplies/charges spent, abilities used, loot obtained and route changes. A low action count alone does not prove dominance. Flag a strategy for review if it repeatedly has no meaningful cost or drawback across both distinct encounter questions. Prefer changing geometry, objective exposure or role separation before changing a global skill.

Rest is allowed to be excellent preparation. Do not call it an exploit merely because crops also progress. Record the travel and hostile-exclusion requirements, and distinguish HP recovery from resources that rest cannot replenish.

## 6. Persistence, source admission and exceptional play

- Freeze placement/reward revisions in the existing world-manifest/version policy. Determine the current version at implementation time; do not hard-code this document's historical version numbers.
- Existing cached zones, looted crates, saved Parts, learned skills and player location remain literal. No manual injected into an opened old cache, no enemy replacement and no retroactive injury repair.
- Preserve old-manifest generation contracts for unvisited old-world locations. A manifest bump alone is insufficient if a builder reads no version; explicitly pass or resolve that authority in each changed builder.
- Save after crate pickup, reading, hauling, trap activation, retreat and return must preserve exact ownership and depletion. No loot reroll or skill duplication on reload.
- Taking the yard first still allows walking, moving the beam, jamming when supplied, fighting or breaking ordinary walls. The manual is never a key needed to escape.
- A killed resident does not become a ghost service. A stolen, sold, burned or lost manual is a normal consequence; other existing routes and skill sources remain available.
- Already-known Vault does not award SP, extra manuals or automatic replacement loot. A full inventory uses ordinary pickup rules; the item remains where it belongs.
- A trap consumed by an NPC is a changed situation, not a broken test. Inspect actual owner state rather than demanding an intact trap later.
- No global reveal of caches, NPC status or crop readiness. Clues can record what was seen; remote services must not be promised available without evidence.
- Preserve source-receipt and transactional checks when adding rewards or actor Parts. Include callback mutation, failed factory, cache capacity, duplicated references and missing blueprints in adversarial cases.

## 7. Implementation sequence and gates

Each milestone follows CLAUDE.md: confirm a failing behavioral test, implement the minimum, run the affected suite, counter-check, update this living document in the same commit, and use the required commit template. New Assets C# files require fresh .meta GUIDs; edit Objects.json surgically and compare parsed blueprint changes.

| Milestone | Work and likely owners | Required exit evidence |
|---|---|---|
| M0: freeze contracts | Existing builders, current manifest, old-save fixtures, build/bootstrap authority | Written source correction table; baseline exact-world inventory/actor/topology records; explicit discovery of every affected generation version. |
| M1: ordinary injury envelope | CombatSystem source plumbing, proposed profile Part, authored cellar/yard staged owners | Forced trauma-positive attack without profile; identical attack with profile preserves limb but same HP/bleed/death rules. Explicit sever abilities and unrelated actors unchanged. Save/restore profile and real attack integration. |
| M2: finite capability reward | GleanersCellarBuilder, ConnectedSpreadSatellite, manifest admission | All fresh supported seeds get exactly one cellar manual; unchanged two clay and old secondary. Satellite slot changed once; old versions literal; failure publishes nothing new. Read/known/read-again/save tests. |
| M3: next encounter and salvage | SpreadExplorationWorksites, glade builder/layout authority, yard geometry/receipt validation, existing voxel presentation | Both visible standing cells and legal landing; occupied landing counter-case; walking/hauling/trap routes remain; two finite timber and tonic join original stock. No NPC or trap respawn. |
| M4: concise clues and revisits | GleanersDistrict, SpreadExplorationReadout, existing notice/examine and ConnectedSpread presentation | Short first lead, full detail still accessible, actual-state inspection, no remote omniscience; Deck readability screenshots of entrance, low barrier and ready/empty pan. |
| M5: integrated comparison and polish | Existing native scenario/input harnesses, affected EditMode cohorts | Real fresh-build journeys and save/revisit cases; strategy table; performance check; source review and explicit remaining human-playtest limits. |

M1 precedes reward marketing: do not make the yard more inviting while leaving its identified ordinary permanent-injury mismatch unresolved. M2 and M3 form the smallest playable capability loop. Do not expand the region before evaluating that loop.

## 8. Verification plan

Use seeds 1, 64 and 1729 for baseline reproducibility, covering odd/even reward branches and geometry. Begin with all five starts across those seeds (15 fresh journeys); use additional fixed seeds for rejected/admitted site-generation cases discovered by a census. Report absent optional satellites honestly, not as manually repaired worlds. The cellar and glade demonstration must not depend on a lucky optional admission. Treat a declined yard arrangement as its own tested branch: show no false live-service claim, retain ordinary exploration, and exercise the glade demonstration. Separately run the full yard journey on admitted fixed seeds without granting missing source owners.

For each fresh journey, use native controls and original inventory. Take the manual, read it, demonstrate the glade crossing, and reload a saved consequence. On admitted-yard worlds also reach the yard, demonstrate a legal new movement option there, obtain or deliberately abandon salvage, and return. Do not teleport, freeze AI, refill HP, grant abilities or edit cooldowns. Controlled diagnostic tests may manipulate state, but must be labeled separately.

Run detailed four-strategy comparisons on the Calm starts and appropriate melee/supply comparisons on other starts; unsupported actions are not test failures. Include a journey that never reads the manual and one that visits the yard before the cellar. Do not require surviving every seed as an acceptance assertion; investigate attributable unavoidable or unreadable failures rather than tuning the observer to force a pass.

Counter-checks include: missing manual blueprint; exact-cache capacity boundary; learned skill already present; occupied or sealed landing; player death during callbacks; departed source actor; depleted trap; destroyed cache; saved legacy graph; restored profile; unprofiled identical attacker; full HP meal vs injured/bleeding meal; nearby hostile preventing rest even when Calm; cleared hostile permitting rest at a valid site.

Run native Unity EditMode for changed systems and relevant persistence/generation/combat/HUD tests. Use the documented standalone runner if Unity is unavailable, with before/after failure comparison; runner success cannot certify scene imports, rendering, native controls or actual Deck performance. No count of historic passing tests proves this proposed design works.

Capture Deck-size images in ordinary lighting, and profile the same route before/after. This change should add no recurring whole-zone scans or per-frame allocations for clues. Static props and source-owned state updates should reuse current batching. Compare frame-time distributions and allocation behavior on matching conditions; a Mac run is not a Deck performance result.

**Unaided player gate, still outstanding:** ask at least three first-time players to explore without directions. Observe whether they can state a desirable expedition outcome, identify two approaches, recognize that the recovered manual changed a possibility, and explain one reason to prepare or revisit. Record confusion and hints needed; do not substitute automation for this result. A preliminary target is two of three doing those things without coaching, not statistical proof of universal usability. Time-to-first-intention is measured, not advertised in advance.

## 9. Self-review and scope decisions

- Resolved during design: Calm/rest timing conflation; injury attributed too narrowly to weapons; assumption that a charged cold rite is equally legible for all builds; accidental duplication of the satellite's main reward.
- Deliberate divergence from the earlier connected plan: the cellar gains the movement manual; the satellite's manual slot becomes a charged book; the yard gets a finite supply floor; staged opening actors gain an explicit incidental-trauma capability. All apply through a new-world version, not retroactive rewriting.
- Remaining design risk: a guaranteed Vault might trivialize too many barriers. Preserve sealed restrictions and test topology; accept clever ordinary wall crossing. Change encounter choices before silently weakening the learned ability.
- Remaining design risk: the actor-specific trauma capability needs clear examination and exact source attribution. If it becomes a general balancing patch applied everywhere, stop and design the global injury system separately.
- Remaining experiential risk: the next destination can still feel like an errand. The gate is recognizing a changed approach, not merely reaching a second address.
- Deferred: comprehensive limb recovery, global damage balance, new NPC services, new crops, more biomes, new enemy families and cosmetic expansion beyond the few necessary cues. Dog-fetching or unrelated low-impact defects do not block this loop.
- Preserve established constraints: BitLocker remains dev-only; no Shambler spore weapon; neutral GlassScorpion remains deliberate; no Curation cosmology exposition added to an opening utility notice.

## 10. Completion record for this planning task

Completed: attached assessment reviewed; current source and recorded native outcomes checked; five priorities converted into concrete content changes; alternatives, costs, save/version behavior, milestones, counter-tests and human acceptance defined.

Files changed in this planning task: this document only. No gameplay, art, balance, build or save files changed. No new tests or play sessions run; this is not implementation evidence. Readiness: existing underlying systems are available; proposed content changes and trauma capability await implementation; discovery, balance and Deck readability await observation.

## 11. Implementation log — M0

Started 11 October 2026. Current manifest is v16. New opening is v17; v16 must be added explicitly to Restore's accepted historical versions. The manager currently passes connected/fieldwork flags into the cellar but no opening revision; satellite and worksite helpers similarly need explicit opt-in arguments with legacy defaults. The glade district is installed after reference composition, so its crossing must be placed on reserved clear discovery ground without mutating old owners. Existing yard tests deliberately call the legacy helper and must retain their exact stock assertions. A one-actor yard retains its source actor rather than staging a pair: do not silently mutate that retained actor while promising transactional preservation.

Baseline remains source `5f0a76686`. New behavioral tests were authored before production changes for three fresh seeds, the glade's walkable crossing, attacker-vs-defender trauma authority and the legacy direct builder counter-case. Native RED run pending. Existing unrelated package/log modifications are preserved.

### M1–M4 implementation progress

Native RED: job `c4de4a5989d146d9910379ab0cea3184`, 9 intended failures/1 legacy pass. Core trauma gate and pre-existing anatomy suites: job `fd6c97f57079446b9ed3a92fb66563de`, 23/23. Fresh manual/crossing and legacy district admission: job `b17459e5c3c8433aa42b0792870709c4`, 25 passed with the two newly authored yard cases still RED.

Implemented v17 opt-ins; old v16 restore acceptance; saved attacker-only trauma profile and truthful Examine; finite cellar manual plus original reward; satellite book substitution; glade low wall and nearby short note; concise regional choices; southern yard count-preserving actors, optional low crossing and finite tonic/timber. The new goods use one two-unit timber stack and one tonic, occupying two available slots without resizing the container or merging original stock. One-source yards stage one Scrabbler instead of leaving an unprofiled source: this is an explicit v17-only, count-preserving divergence. All other worksite addresses keep existing rules.

Review correction: the existing yard persistence test expected GoldCoin to remain packed, although current TakeFromContainerCommand converts it to five drams per coin. Updated the test to assert real currency credit and zero packed coins, plus the new recovered-timber quantities; no currency production code was changed. KitchenBatchPart.Describe already reports Ready/collected/pending time and the existing renderer has distinct pan states; preserve these instead of duplicating them.

### Review fixes and native verification

The native build picker initially selected Stormcaller during a requested Duelist journey because stationary pointer hover undid keyboard selection. A new controller regression failed first; row-entry-only hover now preserves keyboard/controller selection, while moving to another row still selects it. The corrected native Duelist seed64 journey `e937830a3b0f4ea39455af1708e9c1d9` passed all six checks, 122 paid local inputs, with ordinary AI and the real starting kit. This prompted a further yard-crossing run rather than treating inspection alone as movement evidence.

🟡 Fixed: final worksite receipt accidentally checked every unrelated zone owner after placement, rejecting later builders. New false/true opening counter-cases failed in job `1d6e6fb661e941498c006529d59be73f`. Final validation now checks its own packet, original other stock and geometry; publication still checks the full transaction boundary.

🟡 Fixed: a replacement satellite book with a wrong skill or empty charge accepted staging. Both malformed fixtures failed in the same job; opening discovery now requires Shattered Rime and exactly 10/10 charges. Its ordinary original-stock owners remain untouched.

Fixture correction: natural weapons are created by NaturalWeaponFactory, not the blueprint factory. Corrected real-attack comparisons now exercise both an equipped-type dagger and the actual natural rake factory. These compare HP against the same seeded unprofiled attacker. The separate forced-trauma tests establish a non-vacuous dismember branch; explicit dismember tests remain unchanged.

After these fixes native Unity job `7b09ad430fc54fc789bcf217d28f897c` passed **90/90** across opening, adversarial, satellite, yard, yard persistence and starting-build UI suites. No standalone runner was needed. Updated current satellite audit expectations for the changed reward and supplied an explicit Vault learning message on the staged cellar manual.

## 12. User-directed revision: cursor Vault and committed melee

The user requested, during implementation, that Vault target a highlighted tile at distance **1–2 inclusive** and attack an NPC on that tile, showing a red target outline and yellow landing outline. This supersedes the original direction-only movement contract in this plan; the opening remains the active objective.

Design: hotbar activation enters the existing world cursor flow. Directions/D-pad move the selector; Enter/A or click confirms; Escape/B cancels without cost. Empty destinations land on the selected cell. For a living creature, land one cell toward the starting position from the selected contact, then make one committed primary melee strike using the existing combat pipeline. An adjacent target leaves the player on their current tile. Both outlines use one shared, read-only geometry resolver; blocked/occupied landings and sealed archive crossings reject without movement, attack, turn or cooldown. No invisible enemy reveal. A nonhostile target uses the existing attack confirmation before either movement or attack. Recheck after landing callbacks; death or displacement must not permit a stale remote strike.

Retain 15-turn cooldown, the existing brief poise bonus, ordinary damage/resistance/armor/on-hit hooks and faction consequences. No damage multiplier or global reach change. Append a cursor targeting enum value to preserve saved numeric values; current Vault input must also recognize older saved direction-mode ability rows. Legacy direct direction-only callers retain their old movement-only behavior, while player cursor targeting uses the new contract.

Implementation sequence: (1) failing gameplay and preview contracts, (2) shared landing/target resolver and melee execution, (3) keyboard/controller/mouse cursor and friendly confirmation, (4) red/yellow render outlines and truthful details, (5) save, callback, range and cancellation adversarial tests, (6) native empty/occupied/blocked/confirmation screenshots and revised opening journeys. Existing direction-based audit input must be updated to cursor movement plus confirmation. Avoid per-frame allocations: cached cursor state and precreated outline renderers; no whole-zone scans.

Acceptance includes cardinal/diagonal/offset targets, adjacent and two-cell targets, distance-three rejection, walls/occupants at the landing, archive barrier, free cancel/refusal, correct charge/cooldown, normal combat hooks, death/displacement on landing, friendly decline, old ability metadata and clearing both outlines on close/zone change. Native guided tests do not establish unaided usability or Deck frame rate.

### Cursor Vault verification (11 October)

Gameplay, input, renderer and legacy cursor tests passed 35/35 (`b2f51e1bacf84c41b483852c191dd0e2`). Review added two failing HUD cases: an unbound ability retained the normal hint through the snapshot cache, and old serialized Vault metadata displayed DirectionLine. Job `cd33bfc1e6f940508881a45096c07cf4` demonstrated both failures. The cache now includes the hint; Vault presentation derives its current cursor/range contract by command without rewriting unrelated saved abilities. Expanded cohort `aaa4aece3fb3425c96a4b23bd970ac18` passed 39/39 in Unity EditMode.

The manual description was edited surgically. Parsed JSON comparison found exactly one value changed: `/Objects/706/Parts/1/Params/0/Value` (DitchkeepersFootwork Examinable Description); no blueprint reserialization. The staged cellar learning message now describes selecting a creature and landing short to strike.

Broader review also found a pre-existing source-admission mismatch: the SeedKeeper stock already contains two InertSludge, but the exact validation list omitted them, preventing the resident and related reserve from appearing. Added those existing stock entries to the expected list; no loot quantities or loot-table data changed. Relevant resident/content/regression tests passed in the 96-case rerun (the sole failure there was an old cursor test selecting the newly added secondary LineRenderer instead of the named WorldCursor; corrected and included in the green 39-case cohort).


### Low crossing visibility: native-discovered correction

Broad Unity cohort `04a062c41035419a890765e6827e6537` passed **1,171/1,171** (228.08 seconds). Native run `8b2242a4c4d3430f9e5510bf596d07b3` then showed the actual cellar guard in red with a separate yellow landing, and a real melee strike (15→11 HP). Its subsequent glade jump was correctly refused because the nominally low barrier retained the opaque Wall tag. This was a content defect revealed by the new visible cursor contract, not a reason to allow unseen targets.

Five added assertions failed in `018d769725d14e7f96be8e4b8389b877`: three glade seeds could not see their landing, and both yard actor-count variants retained opaque crossings. Removed only the Wall opacity tag from the two authored crossing roles before publication; retained Solid/Physics/destructibility/visual model. Counter-check restores the Wall tag and proves the landing becomes hidden again. `5774e2026f994076967c57d52bb5cf56` passed **61/61** including opening/adversarial/yard tests after the fix.

Native second strike run `911b7a2b21e64f8a9c6a8a11e2e5f587` landed and hit again (15→14 HP), then stopped when the wounded guard blocked the harness's safe retreat. It is retained as a failed overall expedition, not presented as a completed return. First revised full cursor journey `f8a084f9f3b8461abafaed419da4320b` (Duelist, seed1) passed all six checks, including glade crossing and F5/F6; optional southern yard admission was declined honestly.


### Cold review and scope decisions

- 🟡 Fixed source admission and final-receipt boundaries (above); original rolled stock remains literal, failure rollback reclaims only unchanged owned objects.
- 🟡 Fixed stationary-pointer interference with keyboard/controller build selection, verified by native selection of every requested build rather than trusting the requested label.
- 🟡 Fixed low crossing opacity after a native refusal; retained ordinary wall/closed archive restrictions and solid collision.
- 🔵 Existing saves keep their manifest revision and original actor/reward graph. Current Vault command input recognizes old targeting metadata without upgrading old-world placement. Saved tags/parts remain literal in SaveSystem; the new source trauma profile is a saved public field.
- 🔵 No new art asset was needed for the two crossings: the existing low-wall visual is reused. No new whole-zone update loop or per-frame inventory query was added. Two cursor outlines are created once per renderer, with stable-target geometry cached.
- 🧪 Native routes are guided and combat randomness is not pinned solely by world seed. These runs can verify observable actions and persistence, not unaided discovery, universally optimal strategy or permanent safety.
- 🧪 The retained frame-time traces include scene generation, screenshots and audit I/O on a Mac. They are neither a before/after causal benchmark nor physical Steam Deck performance evidence. Those limits remain explicit; no performance improvement is claimed.

Commit cadence divergence: M0–M4 were implemented and reviewed together before committing because the v17 reward, source profile and crossings form one opt-in generation contract. The user's mid-flight Vault targeting revision then required revisiting crossing visibility. The resulting delivery is grouped by the final connected behavior rather than committing known-incomplete milestone snapshots. The original planning section remains historical; this log supersedes its old direction-only Vault contract and implementation status.


### Final cursor/controller review

Six review regressions failed first in `4d93d8d80c0c42539dffb4531380e31b`: the controller's normal adjacent direction cue overwrote the Vault cursor; stick/D-pad direction was still interpreted as a directional skill requiring A (four device/cancel cases); and an offset creature target could be rejected by an archive barrier beside the actual one-step landing. Vault now participates in the cursor direction path, retains its own renderer state, and checks intermediate archive barriers against actual movement rather than the selected creature's displacement. A barrier on the actual landing still rejects; ordinary walls and sealed barriers retain paired counter-tests.

`f3d11fa21d464b749154cb18dbdc4944` passed **119/119**: Vault gameplay, keyboard/virtual Input System gamepad input, friendly confirmation, renderer, HUD, legacy Vault and existing gamepad mapping suites. The renderer tests also cover loss of visibility, zone clearing and **zero managed allocations over 10,000 stable cursor updates**, with and without a creature. This is a narrow cache allocation invariant, not whole-game allocation or Deck timing evidence. Vault's red outline is now the same stroke width as its yellow landing for small-screen readability.

Native run `1323c535e9e646e18bc5859a3deeb30c` at **1280×800** completed all six opening checks after a real two-cell cursor strike on the original cellar guard. The strike missed through ordinary combat rules (15→15 HP); the earlier live runs demonstrate actual damage (15→11 and 15→14). The current run then crossed the glade and yard and restored the earned skill/manual graph through F5/F6. Reviewed `vault-target-1280x800.png` and `vault-after-strike-1280x800.png` in `Docs/Verification/SpreadOpening/`. This is Mac Unity at Deck resolution, not execution on Deck hardware.

### Native comparison matrix

The five starting builds × seeds 1, 64 and 1729 all completed the real ordinary opening route (**15/15**). Additional Classic seed64 runs completed withdrawal/revisit, manual-left-unread and yard-before-cellar (**3/3**). The low-HP stops from the melee-only and initial supplies-permitted scripts are preserved as failures. Both stopped at 9 HP before consuming a tonic, so they do not establish a supplies benefit. Withdrawal reached the legal cot at full HP, where rest appropriately made no HP/time change; it proves the route, not restorative effectiveness in that run.

The supplementary supply comparison explicitly uses a tonic at ≤32 HP to measure an actual preparation decision. Review also corrected an old harness assumption that tonic use was free: successful self-use is measured as a paid local action by the existing scheduler trace. Neither the game damage rules nor the harness HP safety floor was changed.

Compact run records and quantitative results are in `Docs/Verification/SpreadOpening/native-runs.json` and `native-summary.tsv`. They retain failed outcomes, actual carried stock, HP observations, turns/world time and bounded frame-time samples. Full local trace reports remain under the recorded native run paths. Bypass runs kept 40 HP and both starting tonics; that is evidence for this guided route, not evidence that the region has no risks. The southern yard's salvage was left untouched in these routes: its exact finite stock, pickup, return and replacement-save behavior are covered by the generation/persistence tests rather than claimed as native collection.


The preventive-supplies run `1c826058665a4484ba102d2286b9c393` completed at 1280×800: **two starting tonics consumed through paid actions**, lowest observed HP16, final HP29, two clay and the manual carried home and restored. It required 174 local inputs/176 completed player turns versus the Classic seed64 bypass's 121 local inputs, HP40 and zero tonics used. Combat RNG was not locked between runs, so this is an observed cost comparison, not a paired numerical balance experiment. No production damage, AI, healing quantity or HP was edited to obtain the result.

### Delivery status and reproducibility

| Milestone | Delivered | Verification / limits |
|---|---|---|
| M0 contracts | v17 opt-in; explicit v16 compatibility; legacy helper defaults retained | Fresh three-seed and legacy-save counter-tests; no old-world owner migration. |
| M1 trauma | Saved attacker-specific incidental-trauma capability, readable Examine | Same-seed HP damage comparisons, flag-swapped severing counters, explicit dismember and save tests. |
| M2 reward | Guaranteed finite cellar Vault manual; original clay/secondary retained; satellite charged-book substitution | Malformed/capacity rollback tests; real read, return and F5/F6 across 15 starts. |
| M3 routes/yard | Solid low crossings with visible landings and walk-arounds; finite yard tonic/timber; source actor count preserved | Generation/adversarial/persistence tests; actual glade and admitted-yard jumps; absent-yard branch retained. |
| M4 communication | Short notice, cellar lead, clear manual instructions, existing live-state readouts retained | Read through real native UI and inspect actual 1280×800 screenshots. Unaided discoverability remains a human-playtest question. |
| M5 integration | Five starts × three seeds; alternate order/unread/withdrawal; real cursor combat, supply comparison; cold review | 1,171 broad tests, 61 post-visibility tests and 119 final targeting/controller tests passed (overlapping cohorts, not summed). 20/22 final matrix runs completed; two initial combat safety stops retained. Earlier exploratory failures documented above. Physical Deck and causal before/after frame/allocation benchmarks remain unverified. |

Reproduce native cases from the stopped Unity editor with `CavesOfOoo.Editor.SpreadDiscoveryNativeBatch.LaunchOpening(build, seed, approach)`. Supported opening approaches here are `bypass`, `melee`, `supplies`, `withdrawal`, `unread`, `yard-first`, and `vault-strike`. The launcher isolates/restores the user's saves and scenes. The harness preserves all failures and never grants the manual/health, teleports the actor, suppresses enemies or changes cooldowns to reach its checks. Test invocation fixtures include SpreadOpeningTests, SpreadOpeningAdversarialTests, TrappersDispatchYardTests, TrappersDispatchYardPersistenceTests, VaultCursorTests, VaultCursorInputTests, VaultCursorRendererTests and NativeGamepadInputTests. The full broad fixture selection is retained in `broad-test-fixtures.json` beside the evidence.

Final review scope: new generation/content/reward contracts; damage-only gate; current and older ability metadata; hotbar and unbound activation; real gamepad state events; friendly refusal; visibility; archive and occupied landing counters; callback death/displacement; no per-frame preview allocations; literal save state and unrelated source preservation. No additional yellow-or-higher finding remains known after the corrections recorded above. The two isolated package-file edits, Unity MCP logs and unrelated generated census output are outside this delivery.

### Implementation files

- `Assets/Editor/Scenarios/SpreadDiscoveryNativeBatch.cs`
- `Assets/Resources/Content/Blueprints/Objects.json`
- `Assets/Resources/Content/Data/Skills/Acrobatics.json`
- `Assets/Scripts/Gameplay/Abilities/AbilityTargetingMode.cs`
- `Assets/Scripts/Gameplay/Combat/CombatSystem.cs`
- `Assets/Scripts/Gameplay/Entities/ExaminablePart.cs`
- `Assets/Scripts/Gameplay/Skills/Acrobatics_Vault.cs`
- `Assets/Scripts/Gameplay/World/Generation/Builders/ConnectedSpreadSatellite.cs`
- `Assets/Scripts/Gameplay/World/Generation/Builders/GleanersCellarBuilder.cs`
- `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationBuilder.cs`
- `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationResidents.cs`
- `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationWorksites.cs`
- `Assets/Scripts/Gameplay/World/Generation/SpreadExplorationPlan.cs`
- `Assets/Scripts/Gameplay/World/GleanersDistrict.cs`
- `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs`
- `Assets/Scripts/Presentation/Input/InputHandler.Controller.cs`
- `Assets/Scripts/Presentation/Input/InputHandler.cs`
- `Assets/Scripts/Presentation/Rendering/AbilityDetailsBuilder.cs`
- `Assets/Scripts/Presentation/Rendering/HotbarStateBuilder.cs`
- `Assets/Scripts/Presentation/Rendering/HudHotbarPresentation.cs`
- `Assets/Scripts/Presentation/Rendering/WorldCursorRenderer.cs`
- `Assets/Scripts/Presentation/UI/StartingBuildMenuController.cs`
- `Assets/Scripts/Presentation/UI/StartingBuildMenuUI.cs`
- `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.ConnectedVariants.cs`
- `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.District.cs`
- `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.cs`
- `Assets/Tests/EditMode/Gameplay/Skills/AcrobaticsVaultTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/ConnectedSatelliteCensusTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/ConnectedSatelliteTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/ConnectedSpreadContentTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/SpreadExplorationWorksitesTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/TrappersDispatchYardPersistenceTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/TrappersDispatchYardTests.cs`
- `Assets/Tests/EditMode/Presentation/Rendering/HotbarStateBuilderTests.cs`
- `Assets/Tests/EditMode/Presentation/Rendering/WorldCursorRendererTests.cs`
- `Assets/Tests/EditMode/Presentation/UI/StartingBuildMenuControllerTests.cs`
- `Assets/Scripts/Gameplay/Combat/MeleeTraumaProfilePart.cs`
- `Assets/Scripts/Gameplay/Skills/VaultTargeting.cs`
- `Assets/Scripts/Presentation/Input/InputHandler.Vault.cs`
- `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.Opening.cs`
- `Assets/Tests/EditMode/Gameplay/Skills/VaultCursorTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/SpreadOpeningTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/SpreadOpeningAdversarialTests.cs`
- `Assets/Tests/EditMode/Presentation/Input/VaultCursorInputTests.cs`
- `Assets/Tests/EditMode/Presentation/Rendering/VaultCursorRendererTests.cs`

Each new C# file has a Unity .meta file. Plan and compact test/native evidence accompany the source.
