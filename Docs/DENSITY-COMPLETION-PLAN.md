# Content density completion — implementation plan

**Status:** whole-biome C15 native acceptance complete; remaining content milestones active, 27 September 2026. This plan was recorded before implementation. Execution is
authorized by the user's instruction to analyze the gaps, write a thorough plan,
write an execution prompt, and implement it. Baseline: `50ef23d2` on `main`.
The companion [execution prompt](DENSITY-EXECUTION-PROMPT.md) drives this work.
Every milestone below remains open until its own acceptance evidence is recorded.

**Latest user extension (26 September):** expand the approved reference-glade
look to an entire biome and start new players there. C15 below is required work,
including all chunks, environmental objects, NPCs, enemies, player and items.
The existing content completion milestones remain active.

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

### C13 — Original enemy roster (user addition)

Audit every active creature against recognizable Caves of Qud imports, starting
with the Snapjaw family. Replace confirmed imports with original Ooo creatures,
including distinct ecology, appearance, descriptions and role-appropriate combat
kits. Preserve encounter difficulty and regional intent while removing imported
names from current blueprints, generated encounters, quests, dialogue and UI.
Check inherited families, bosses, factions, corpses, equipment and sprite bindings.
Historical design references and test evidence remain truthful archival records.

Acceptance: a recorded roster decision for each suspect; failing tests before
replacement; no retired enemy in fresh generated content; actual factory/spawn,
equipment/death and quest paths work with replacements; deliberate, tested old-save
compatibility without rebuilding unrelated saved parts. Do not confuse common
fantasy creature names with confirmed game-specific imports. Pair each new design
with a distinct encounter role, and repeat affected density and combat checks.

### C14 — Reference glade, models and playable scene (user addition)

Build the supplied image as an authored playable wilderness scene, with original
Ooo enemies. The detailed plan and source corrections live in
`Docs/DENSITY-REFERENCE-GLADE.md`. Implement the composition with native entities,
scoped voxel models/materials and the existing orthographic renderer, then save a
launchable Unity scene. Require test-first world placement, persistence and
interaction checks, native keyboard play and inspected screenshots. Iterate the
art against the actual image; passing logic tests alone cannot close this milestone.

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
| C0 | Baseline recorded | Five seeds, 150 zones, 447 stocked container observations; historical open counter overcounted locked-container attempts; exact-key C12 replay now separates lock refusal and observed opens |
| C1 | Implemented, native integration green | 51 RED (41 missing-content, 10 controls); 138 focused GREEN; native new content/adversarial GREEN; armor 19→59 in scoped census |
| C2 | Implemented, play gate pending | Ten kits, local assistance/flee, 98 new checks native GREEN; ordinary-stat scheduler audit under verification |
| C3 | Water/cooking and general liquids verified in native Play | Water/cooking75 core/adversarial +16UI GREEN and13/13 acquisition. General liquids94 native core/UI and26 renderer GREEN; actual purchased water/oil/acid carry/pour/save route21/21, zero errors, exact restoration. Three native puddle frames visually reviewed |
| C4 | Beds and generated doors verified in native Play | Generated-bed route15/15, zero errors. Door110 gameplay/menu and28render checks native GREEN. C15 route791acdc226ba4c1da03727736d14618d now reaches an actual supported Spread village: exact generated VillageDoor body, paid close/open, stable owner/position and native state-dependent models pass. Its later lair-source refusal remains an honest separate failure |
| C5 | Readables and examine coverage implemented | 13 canonical copies, native pagination and acquisition proof;39 useful descriptions added after39 actual-factory RED cases,116 focused GREEN |
| C6 | Implemented | 27 roles/seven cultures; five unique Choir identities;59 native GREEN, additional saved-graph and dead-listener controls verified standalone |
| C7 | Implemented and bounded native route verified | Sari45 and126 focused native checks; actual traveller/dialogue route13/13,37.845s,0errors, exact restoration. Real zone-entry merchant, purchase/revisit, F5/sell/F6 and natural Scribe remark after83 paid routine actions; shared19-action silence. Two labelled player approach transfers, no actor/stock/context grants; finite route does not establish encounter frequency or every voice |
| C8 | Measured material and steam fixes verified; broader environment work open | Hazard navigation108, smoke181 focused GREEN. Steam22/22 native route and34 native unit checks. Five blueprint unit corrections pass28 new plus137 neighboring native tests; unchanged thermal replay8/8, zero errors. Tar still does not ignite from one FlamingHands; broad combustion/contact/scald remain open |
| C9 | Implemented; native follow-up pending | Corpse/scenery census,4 content yields and transactional finite harvest;90 focused GREEN plus independent malformed-product guards |
| C10 | Placement and second layout implemented | Boss/guard overlap repaired; depth-aware alternate room layout and reserved travel routes pass 188 focused standalone checks. Native layout/integration GREEN in the580-case selection; saved multi-level lair graph is published and native focused GREEN; actual ordinary-stat Beating keyboard route14/14 GREEN, real starting Calm/Rime Grip, reward/save/ascent/revisit and exact cleanup proven; failed dagger-only attempt retained |
| C11 | T4, modified finds and one legendary keeper source verified in native Play | Actual T4 sources/locks and modified/legendary42-case native selections GREEN. Complete native route19/19, zero errors, exact restoration: earned modified maul and real keeper armor acquired/equipped/inspected, both actual enemies defeated, F5/mutate/F6 preserves reward identities and source depletion. Two labelled content-entry transfers; ordinary initial stats, earned levels, no grants. Broader legendary families remain open |
| C12 | Foundation integration green; campaign gate open | Exact standalone differential has zero new failures. Final unfiltered native Unity sweep passes 16,996/16,996 with no skips. Exact-key census: 445 containers, 39 locked, 406 observed opens. Final standalone follow-up repeats10,075 cases with zero new failures against both preserved baselines; supplemental1,888/1,888 pass. Final native follow-up now19709/19709 GREEN; representative campaign play remains open |
| C13 | Runtime and scoped models implemented | Original Marlbacks, Grove lantern moth and two quest creatures; exact saved-identity compatibility, 163+7 native art checks. Separated native roster75/75 passes with zero errors; all six bodies inspected, moth wing silhouette repaired and134 nearby native art checks pass; follow-up commit pending |
| C14 | User-approved scene, contact rendering and native combat verified | Latest walkthrough12/12 and strict real-player combat12/12, zero errors, exact restoration. Combat finishes31/40HP with original dagger and one original Rime, no tonic; earlier dagger-only failure retained.60.016-second editor movement sample mean3.869ms/p954.030ms. Native glade/contact regressions GREEN; reeds partly obscure the upper-edge fight, so pose observation is not an animation-quality claim |
| C15 | Whole-biome implementation and native acceptance complete | Normal N/F5/scene-reload/C startup12GREEN. Imported52 profession rigs,396 portable forms,56 scenery variants,40 environment forms,5 fauna forms,12 fitted gear forms and common terrain9. Headwear16+fit6+seven native gear galleries pass; all52 original body shape/UV0/rig buffers remain unchanged. Existing visitor rigs13 adopted with36nativeGREEN; full107 roster now106 approved/1 intentionally hidden after29 new species bodies pass76 native checks and29 three-pose capture cases. Static868 import completes (462verified reused/406written); new29body pack imports before hooks. Actual biome route23/23, zero errors, exact restoration verifies chunk travel/harvest/save, village doors, paired lair stairs and foreign profile. User-reported2D fallback is resolved in viewed native frames. Pool33 native gates and13-image comparison now pass after a scoped8-mesh rebuild; all864 unrelated adopted entries and458 unsaved editor objects remain unchanged. Gas/ground/particle source, lifecycle and actual512-capacity checks are native GREEN, with complete reviewed native galleries. Controlled final editor movement sample averages4.252ms/p954.014ms; redraw p9543.007ms and one506.677ms editor/GC/UI spike remain honestly recorded. Exact-role Posy repair passes24/24 and combined72/72, including all142 Spread surfaces perseed plus1/1/2 affiliated floors across seeds64/1/1729. Normal-bootstrap story registries are now explicitly isolated/loaded/restored by the census. Final unfiltered native19709/19709 GREEN, zero failures/skips, exact editor restoration and zero drift in2513 inputs. The fourteen first-sweep obsolete expectations are preserved and corrected with157/157 focused GREEN. Exact9249-path ownership draft has complete GUID/resource dependencies and compiles1055 runtime sources against clean HEAD with only declared overlays |

## 7. Implementation log and self-review

- 2026-09-26: verified shipped baseline, current gap corrections and preserved
  constraints; authored this plan and execution prompt before production edits.
- ⚪ Content completion is bounded by the explicit milestones above, not the
  historical Qud comparison's speculative volume or an unsupported percentage.
- ⚪ Optional guns, new anatomy slots and hundreds of effects are separate rules
  expansions. They are named exclusions, not silently marked implemented.

- First integrated native run: 392 cases, 391 passed, one catalog-null-array validation failure. Unity normalized null to an empty array; implementation now rejects either. MCP timed out before test initialization, but NUnit subsequently completed: raw XML is the authoritative receipt.
- C1 scope-only census: the same 447 stocked container observations contain 19→59 armor units. Commerce value 11,706→14,013 (+19.7%); neutral sale value 3,809→4,615 (+21.2%). These measure source/economy changes, not difficulty or enjoyable pacing. The historical helper counted handled open attempts, so they are not evidence that every locked container was successfully opened.
- Native C3/C5 driver uses real generated stock and harvested ingredients with keyboard interactions and ordinary starting money/stats. Travel positioning is explicitly accelerated and cannot establish ordinary walking-route balance.

- Native integration follow-up:141/141 passed (chair/torch inventory timing, sari entry/ending, local people, ordinary-stat combat scheduler). The preceding33-case RED had7 intended missing-hook failures. This is native EditMode integration, not a keyboard combat/balance playtest.
- Isolated baseline full standalone corpus:10,060 cases,9,765 passed,295 environment failures. The exact same 672-file selection after C10/C11 yields 10,075 cases, 9,780 passed, and the same 295 failures: zero newly failing. Renamed/added test cases explain the count change; new fixtures outside this selection have separate focused coverage.

### C15 — Complete Spread biome in the approved voxel style

**Status: implementation and native acceptance complete for the bounded source corpus and recorded routes.** The user likes
C14's current native appearance and requests its use across an entire biome.
Choose the existing **Spread**, whose palette and native glade already fit.

1. Inventory actual Spread generation, including every authored and generated
   POI, travelling populations, merchant stock, drops, dynamic liquids, doors,
   harvest products and player equipment. Record provenance and explicit model
   coverage, including rare sources; sampled occurrence alone is insufficient.
2. Add map-derived presentation authority for every Spread surface chunk and
   restored graph. Separate visual eligibility from the fixed reference scene's
   authored geometry. Preserve each chunk's own terrain, collision, placement,
   contents and gameplay identity. Foreign biomes and ordinary underground are
   negative controls; biome-affiliated lair floors need an explicit source rule.
3. Extend the original modular voxel kit with distinct silhouettes for every
   uncovered family and species. Apply the approved palette, geometry scale,
   actor treatment, lighting and ground detail to all models used in eligible
   chunks, including objects carried in from elsewhere. No ASCII or invisible
   object may silently stand in for missing coverage. Preserve functional rigs,
   attack animation, equipment sockets and interaction/picking footprints.
4. Make the normal new-game entry use a verified Spread start with safe legal
   placement, ordinary stats and equipment, connected travel and reachable
   services. Keep Continue at its saved location. Review seed and scene defaults
   and test failed/malformed spawn candidates rather than assuming a clear cell.
5. Verify cross-chunk travel, POI visits, biome exits/re-entry, live entities,
   item drops, light/door/liquid changes, death cleanup and save/load. Capture
   actual native frames across varied chunks and compare performance to C14.
   Run source coverage checks plus paired native rendering controls and finish
   with the full integration suite. Update this plan with exact results.

Initial sweep corrections: `SpreadCompositionPlan` uses a fixed authored
wilderness list, not current map biome authority; `AreaCompositionScope` does
not yet cover Spread; the reference-glade visual profile currently belongs to
one exact zone. The saved SampleScene overrides the code's nominal Morrowfast
start. Widening the glade address predicate alone would neither cover the biome
nor safely establish an ordinary new-game start. Specialist inventories and
concrete source/asset manifests will accompany the implementation.


C15 receiving-creature follow-up is now source and native evidence based:
`DENSITY-SPREAD-VISITOR-MODELS.md` records the107-definition native diagnostic
and its test-first execution plan. Its initial107-roster fixture recorded64
approved bodies and42 visible gaps, split into13 existing animated forms needing
style adoption and29 missing species bodies. The13 existing forms now pass36
native checks and a39-frame pose gallery, bringing approved coverage to77; the29
new bodies now pass76 native art checks and29 three-pose capture cases; the complete107 roster passes with106 approved bodies and one intentionally hidden definition. One undropped Glowmaw is legitimately
hidden. The initial missing42 assertion and all owner/removal controls remain
preserved as RED evidence. Party transfer on surface/stair transitions makes receiving-biome
coverage relevant, while the diagnostic does not claim ordinary local spawning
or recruitment of every species. The original roster gaps are closed; the final unfiltered native checkpoint and its explicit evidence bounds determine C15 completion.


### Follow-on completeness audit (user extension,27 September)

After C15 actual biome presentation is verified, continue the remaining ledger
and inspect coded systems for incomplete implementation or inaccessible content.
For each candidate, trace definition → actual generation/source → ordinary player
action → outcome/feedback → persistence/revisit. Prioritize verified broken or
unreachable behavior over speculative new systems. Record exact source premises,
a failing ordinary-route assertion, bounded implementation, matched negative
controls and native acceptance where visible interaction is involved. The user
explicitly requested continuing this audit if the original content-filling list
runs out; it does not waive the existing lore, BitLocker or verification rules.


### C15 final native checkpoint — 27 September 2026

Actual unfiltered Unity EditMode job `fd718936b9304ca3b0441e84e92ad5d4` completed **19,709/19,709 passing, zero failures and zero skips**, in 979.5685913 seconds. The authoritative XML and summary are preserved at `Docs/Verification/DensityCompletion/Integration/native-spread-full-second.json` and its neighboring `.xml.gz`. All2,513 recorded compilation/content inputs remained unchanged during the run. Scene/start-scene, seed override, save root, last-game preference, background execution and input settings restored exactly.

The first full follow-up retained14 failures from obsolete rendering expectations; all14 were addressed by narrowly strengthened fixture/classification corrections, then157/157 affected cases passed before this second unfiltered run. No production behavior was weakened to satisfy those pins. The three normal-bootstrap story-aware census seeds each cover142 surface chunks plus1/1/2 affiliated lair floors with zero unmodeled supported owners. Actual biome keyboard acceptance remains23/23, ordinary N/F5/scene-reload/C is12/12, and Beating lair acceptance is14/14. Their screenshots, explicit travel shortcuts and exact cleanup receipts remain separate from EditMode evidence.

C15 is complete for the stated shipped-source corpus and verified routes. This is not every possible world seed, every animation transition, an unlimited particle budget or a standalone player-build benchmark. The measured editor redraw p95 is43.007ms with one506.677ms editor/GC/UI spike. Broader encounter balance, ambient dialogue acceptance, ordinary corpse-harvest acceptance, combustion/contact, additional legendary families and campaign-level content review remain on the active plan; legacy2D liquid shimmer is a separate narrow follow-up.
