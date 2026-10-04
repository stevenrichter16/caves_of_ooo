# Marrowstye: the misplaced last words

The disused wing is expanded by [The continuing inspection](CURATION-ANNEX.md). That document supersedes the original six-cell quarantine layout below; the receiving/courier work remains intact. New generation includes a gallery, holding room, screened service passage, repairable escape gate and finite recovery stock.

Status: implemented and reviewed on the published Spread residents base `decd297dd`. Final affected native tests passed200/200; final Play visit passed all gameplay checks and20/21 total checks, with one unresolved attack-pose capture limitation recorded below. The user requested a lore-grounded Curation area near spawn, with preserved bodies, distinctive models, tools, people and danger. This expands an existing canonical place, with original local cases and dialogue.

**Visit:** from the starting glade, travel one chunk east and two south to **Marrowstye, 12.12**. Speak to Doreth or Ivrin in the receiving hall, examine the subjects and quoted bays, haul each subject to its matching bay, release it, then interact with the receiving index. The counterfoil opens the supply-wing cabinet. Its separate inspection key allows an optional visit inside quarantine. Requires a new game or an unvisited Marrowstye chunk; already-cached saved chunks remain unchanged.

## Implementation directive

Make **Marrowstye, Overworld.12.12.0 (one east, two south from the glade)** a place an ordinary explorer can understand and use without its courier quest. Preserve its original courier clerk and delivery contract. Develop its receiving hall, supply wing and separate disused wing instead of creating a second generic Curation outpost beside it.

The public scene is a sincere procedural problem, “the misplaced last words.” Two salt-cured bodies have been placed under each other's receiving labels. Their labels preserve ordinary final remarks, not a lecture. Examine the body and the bay, haul the real ninety-weight cargo to the matching location, and ask the local index stand to certify the arrangement. Correct filing grants a physical stamped counterfoil for one real supply cabinet, once. The key, cabinet stock, body positions and local state all persist. No quest acceptance, repeatable opinion reward, spontaneous curing or copied courier payout.

The side wing contains a visibly incomplete, Bloom-driven intake behind a real locked quarantine barrier. Its salt crust is broken by growth; fully cured subjects remain inert. The player can read the warning and leave, or deliberately unlock/open the barrier and fight. This must remain optional and physically contained until opened. It must not kill the public staff during normal filing.

## Lore authority and local fiction

Read: `Lore/README.md`, `Lore/10_Bible.md` §IV, `Lore/11_SecondSpine.md` opening amendments, `Lore/Factions/03_PaleCuration.md` founding/doctrine/roles/filing/geography/relationships, `Lore/Factions/09_ImminentArchive.md` distinction, `Lore/Factions/08_DrivingBloom.md` and `CanonFactionRosterTests` no-society pin, `Lore/Voices/VOICE-CARDS.md` §3, and relevant material-culture/geography references. Bible/SecondSpine win over phase-history facts. `Docs/Lore/Factions/PaleCuration.md` is an older incompatible conception and is not this area's authority.

- Bodies hold memory in Curation doctrine; they are indexed under last words. Mis-positioning is nearly as serious as decay. Canon roles include Filer, Indexer and Aide. Mainline mundane regional intake and Concord transport justify this small operation (`03`:65–107,135–137).
- The Salted is consciously self-preserved; that does not make ordinary preserved bodies conscious or undead. Her file's missing last words and “Status: continuing” may be alluded to without revealing gated private history (`10_Bible`:118,128).
- Unwilling living preservation belongs to the expelled Catchers. The new mainline staff do not kidnap or salt the player. Marrowstye's existing disused-wing/Catcher association remains context, not a claim that this batch implements the entire Catcher system.
- Bloom attacks incompletely cured bodies; complete preservation resists it (`03`:153). The local quarantine case is an authored application of this premise. Bloom does not give villain speeches; Urqu is not given intent.
- Comedy comes from exact procedure encountering an unfileable world, never sarcastic villains. Condition–method–status dialogue, evasive feelings and material care follow the voice card. New names, final remarks, intake discrepancy and transfer paperwork are original local fiction.

## Source sweep and readiness corrections

| Surface | Verified current behavior | Decision |
|---|---|---|
| Existing place | Marrowstye already occupies 12.12 with IntakeHall, SupplyWing, DomesticWing, DisusedWing and dedicated 3D kit. | Expand that meaningful chunk; do not relocate the Salt-Vault or duplicate the regional chamber. |
| Ordinary encounter gap | FilerClerk's substantive existing branch needs BogBodyCourier; otherwise it mostly dismisses the player. | Add public context without replacing the exact courier branch. Dedicated new local workers avoid remote quest inheritance. |
| Cured bodies | Two real SaltCuredBody owners are solid, weight90, Handling, noncarryable/nonthrowable, no Destructible/corpse/curing parts. Strength12 can haul;11 cannot. | Give these exact owners local identity/labels and meaningful haul destinations. Preserve actual handling; no invented damage/curing simulation. |
| Cargo identity | StoneCoffers are solid110-weight props, not containers; courier SealedBogTakenBody is a separate carried30-weight quest parcel. | Keep both coffers and the original25-dram/+10 courier reward entirely independent of local filing. |
| Generation | Composition1000; profile3860; arrival reservations3870; generic population4000. Hall aisles and disused wing are already reserved. | Add a narrowly scoped enrichment builder using room-relative slots and exact terrain identity. Stage/validate before publication, preserve public routes, well40,12, stairs and existing graph identities. No restamping cached zones. |
| Locks | KeyPart.KeyId matches LockPart.KeyId; keys are reusable. Container.Locked is an independent authority. Door actors require Player/CanOpenDoors. | Physical counterfoil opens exactly one matching cabinet; Container.Locked=false. Separate inspection key opens quarantine. Use actual door/lock behavior and test enemy cannot escape a closed barrier. |
| Preservation tools | No general corpse-curing or raw-PaleSalt action exists. PaleSalt's enhancement requires dev-only tinkering. | Tools may be actual usable items or workbench equipment; do not promise working chemistry. BitLocker stays dev-only. |
| Existing faction services | PaleCurator has repeatable rep/opinion choices; CurationSorter starts a distant quest; Stillleaf file logic is exact-quest-bound. | Dedicated local conversations/state. No copied service hooks or repeatable reputation award. |
| Art | Marrowstye already resolves regional models; missing-only Spread refinement cannot override them. | Deliberate exact-site/current-owner mapping for changed bodies and new props; existing region kit and selected humanoid imports. Preserve unrelated palettes/models. |
| Threat | No general DrivingBloom actor/faction content was found in the checked blueprint/faction files. | A single original half-set intake, basic tendril melee, modest tier1 stats and existing Beasts affiliation. Local saved personal hostility targets the actual three staff. The first proposed DrivingBloom faction was rejected by the canonical no-society/no-reputation pin before implementation. No Factions.json changes. Add the natural-weapon list pin. Physical containment is an acceptance gate; no disease system is implied. |

## Concrete content and boundaries

1. **Receiving subjects and bays.** Retain the existing two bodies at hall-relative (7,5)/(17,5). Add two nonblocking labelled positions at (7,4)/(17,4), assigned to the opposite starting subject. Case1 Meret Pell: “No, the other one.” (crooked thimble at cuff, east bay); Case2 Osren Vale: “Leave the door open.” (blue wrist thread, west bay). Last words are filing labels, not instructions for the quarantine door. Give names, material signs of salt care, and a matching personal detail. Verify complete body identity, not blueprint/count alone.
2. **Index desk and real access.** A readable index near the clerk's spare frontage offers an explicit check/certify action. One saved local authority binds both exact bodies, both bays and the physical counterfoil owner. Wrong, foreign, missing, distant, still-being-hauled or already-certified arrangements cannot duplicate a key or reward. Completion does not prevent later movement; the record remains a historical certification. No scans on idle turns.
3. **Staff and tools.** Add a broad pale-apron Filer and a narrow high-collared junior Indexer with dedicated conversations. Keep the existing clerk/courier role. Add visible salt-working bench/sieve and an actual indexing stamp/counterfoil and a blunt salt rake as appropriate to implemented item actions. Never imply a held mesh is an actual inventory tool when it is only costume. Workbench tooling is acceptable scenery when labelled honestly.
4. **Supply cabinet and documents.** A real matching lock/cabinet with a finite small kit and discrepancy/carriage records. Connect Tent-Right salt, Concord carriage and regional transfer to the wider world through practical receipts. Do not hand the player gated god revelations or a generic lore dump. Separate optional inspection key, plain hazard warning and a quiet choice to leave the side wing alone.
5. **Optional quarantine.** Build a bounded cage inside the existing disused wing without blocking its entry/public routes. Locked saved door, real walls and one original incomplete Bloom-driven body. No dialogue, no spore disease. New body has a different silhouette from inert salt-cured subjects. Closed/locked tests must run real turns; open/release tests must show actual combat. If containment proves unsafe, fix the significant issue before enabling the threat; do not silently ship an uncontained town killer.
6. **Visible coherent kit.** New labelled-body forms, desk/index rack, salt workbench/sieve, cabinet, quarantine barrier, original workers and half-set foe, plus tool/key appearances through existing native paths. Low pale architecture and dark green Spread surroundings should make the intake legible. Reuse the established rig/clips where appropriate; inspect actual camera images and action animation.

## Order, ownership and verification

Write failing tests before production and capture native RED. Parallel work may prepare scratch candidates; root alone imports/refreshes/runs Unity. Root owns lore/content JSON, integration and native acceptance. Generation/local filing, art and focused content/action review are separate bounded workstreams. Every new Assets C# source gets a handwritten fresh GUID meta. Objects.json is surgically edited and parsed-object differences recorded.

Use existing save Parts and physical keys/locks, not a new quest framework. Generation and explicit action gates emit diagnostic success/refusal reasons. Preserve factory/RNG/static state, validate current world/actor/source ownership, and refuse partial/foreign/malformed authority. A local grant must use the existing item-delivery transaction semantics or a staged transfer with rollback, not set-complete-then-hope inventory accepts.

Tests cover real generated native occurrence at seeds64/1729; exact body/bay identity; incomplete/swapped/foreign/removed/distant/dead/hostile/repeated action refusal; physical key and finite cabinet; save/load after movement/certification/depletion; Strength11/12 hauling; untouched courier quest/payment; doors/containment/combat; exact-owner art across movement and state. Review Q1–Q4 and independent lore voice. Run the affected native regressions once, and rerun affected corrections where needed.

Native route: approach the actual site, speak to staff, read labels/index, haul real bodies, certify with the actual menu, take the physical access item, unlock/open/take cabinet contents, save/load and prove no duplicate issue/refill. Inspect generated and ordinary-fog frames. Separately prove closed quarantine safety and voluntary release/real combat with the starter actor; disclose any travel/setup assistance and distinguish measured logic from visual/balance limits.

## Performance and accepted limits

Enrichment is cold generation only and bounded to one exact named chunk. Index verification occurs only on explicit nearby interaction; no per-frame/per-turn graph scans or new cache. Reuse current cell-dirty and animation hooks. New art follows existing fingerprints/libraries and shared materials. No offscreen work simulation, generalized preservation chemistry, living-player preservation, God encounter, mandatory quest, full Catcher arc or revised mainline ending. Existing cached Marrowstye stays literal; fresh generation gains the new scene. Evaluate any unrelated broken feature by importance and record low-value issues rather than stalling this scene.

## Evidence and in-phase review

Native RED 398153b63ac646a38e5c20c013223ef6: 70 cases, 2 controls passed, 68 failed before new runtime/content/art. Initial setup-only compiler errors (missing namespace and internal test field access) were repaired first; the earlier zero-case run is not feature evidence. Production begins after this RED. The later native checks below cover generated content, player input, saving, art and optional combat; publication follows final regression review. Significant findings block their affected content; cosmetic/legacy limits are recorded explicitly. This document is updated in the same commit as the scene/content changes, using CLAUDE.md §2.3.


### Content production

Append 14 original blueprints surgically; preserve all existing parsed blueprints. Two new public conversations and two original local readable records; the old clerk gains public context while the courier choice, predicates, all actions and Delivered node remain exact. `ReadableDocumentCatalog` is reused; local Source fields explicitly identify authored receiving records instead of claiming Codex transcription. Existing faction data is unchanged. Three new rigged people and fifteen scoped static/item forms are imported through existing rendering paths.

### Placement counter-check correction

The first cage rectangle (disused wing X+10..14) would have cut off an unrelated pocket of public floor. The builder's route-preservation check rejected it before publication. Shift the cage to X+13..17 against the existing east wall. Its six interior cells remain walkable terrain; one retains the original passable rubble owner (the half-set currently occupies another); the original rubble and breach stay literal. Native containment and route tests use the actual bounds.

### Persistence correction during verification

🟡 Fixed before publication: OverworldZoneManager initially allowed explicit Marrowstye unload to discard the new finite receiving graph. A save/reentry witness caught the resulting refill risk after the restored exact references themselves passed. Retain a cached native Marrowstye carrying its configured CurationIntake, including a certified/depleted yard, using the existing CanUnloadZone retention boundary. Unconfigured or unrelated zones keep their previous unload policy. This is one bounded named chunk, not a global cache expansion.

### Player-menu integration correction

🟡 Independent source review found that the generic world-action menu fires a bare InventoryAction event, while safe filing requires an InventoryTransaction. A direct TryCertify/command test could pass while the actual player menu refused. Add one explicit certification route beside the existing transactional regional requests, with a real UI dispatch regression and native keyboard proof. No generic action semantics are changed.

### Native menu RED and payment policy

Native job `88ba60a358a64ab69ce34bb63c122a24`: all four UI cases failed before the dispatch fix, each observing zero BeforeInventoryAction notifications from the actual gathered menu selection. The exact new branch now routes certification through PerformInventoryActionCommand, charges one ordinary action only for committed success, and redraws. Wrong placement, capacity rejection, callback rollback and repeat certification charge no time. The rest of world actions are unchanged.

### Imported art

Unity imported fifteen original static forms (145 cuboids, 1,740 triangles total) and three selected humanoid roles on the existing rig and five clips. The old environment, portable, regional and palette assets are preserved; full borrowed-file proof is recorded separately. Source preview approval is distinct from the ordinary-camera and native animation inspection recorded below.

### Native route, first pass and real gate-access correction

Run `20b91200139d4df384b3224fae479160` passed ordinary start and eleven receiving checks: generated owners, public conversation, refusal of the transposed arrangement, both actual hauls/releases, physical certification, repeat refusal, finite cabinet, real document reader, equipped rake and native F5/F6 graph replacement. One labelled player-travel shortcut reached actual Marrowstye; no subject, stock, stats or outcome was placed by the scenario.

🟡 The gate on the cage's north edge faced the existing room wall. The cage was genuinely closed, but no public standing tile could reach the door. Native movement caught this; abstract containment and public-floor flood tests were insufficient. Move the gate to the cage's west edge, disused-relative `(13,2)`, quarter-turn1, and add an actual public approach test. Cage footprint, thirteen rails and all old terrain remain unchanged. This failed route is retained honestly; it does not prove quarantine acceptance. Its original top-level report zone still named the glade despite the explicit Marrowstye travel entry; the observer was corrected to read the actual current zone in the later successful runs.


### Native public visit and optional quarantine acceptance

Run `9bbd0681c4b24b9a820250140014c267` passed **21/21 checks**, zero unexpected errors, in68.723s. The fourteen public checks finish independently before the seven optional-combat checks. The west-facing gate is reached using ordinary movement. Twelve normal wait inputs produced nine exact half-set NPC turn-end records, with containment and all three staff HP unchanged. After voluntary unlock/open, two normal waits produced an actual hostile attack before any player attack; it missed. The starter player then used its existing Rime Grip once and the earned salt rake to defeat the half-set; no healing tonics were used. The recorded attack frame shows changed Spine/Arm.R bones during the actual native attack state.

**Can verify (script-observable):** real generated owners; local dialogue and Examine/read menus; actual ninety-weight hauling and release; player-menu transaction and one-time physical counterfoil; finite cabinet stock; equipped original rake; native F5/F6 graph replacement; no repeat/refill; closed quarantine over scheduled turns; real unlocking, opening, hostile attack attempt, player damage and attributed death; normal actor stats, currency and reputation; BitLocker disabled. Exactly one disclosed travel shortcut takes the ordinary player from the glade to generated Marrowstye. Every local movement and action uses keyboard input; the observer sets no cargo, stock, stats, combat result or success state.

**Visual inspection:** actual camera frames show the two pale human subject forms, low receiving frames, separate desk/cabinet/bench silhouettes, public staff, the west-facing barred cage, open gate and real attack pose. The record reader fits the authored report. The scene retains ordinary fog; these are not omniscient showcase renders. Independent art review found no material defect. Fine case details rely on Examine text at the ordinary camera distance.

**Cannot verify (visual/feel and broader scope):** the route does not prove natural discovery, every seed/save configuration, universal combat balance or whether the tone and fine silhouettes work for every player. Existing cached chunks are deliberately not rebuilt. NPC work tools are visible equipment/scenery, not a new autonomous curing process. No general curing chemistry or full Catcher story is implemented.

### Shipped implementation shape

The generation pipeline adds the receiving enrichment immediately after Marrowstye's existing profile:

```csharp
pipeline.AddBuilder(new MarrowstyeProfileBuilder(intake));
pipeline.AddBuilder(new CurationReceivingBuilder(intake)); // priority3865
```

`CurationIntakePart` binds the original subjects, anchored bays, staff and one stored counterfoil by exact references and saved IDs. The gathered player action uses `PerformInventoryActionCommand`; snapshots and outer transaction rollback cover transfer and certification together. Successful certification costs one ordinary turn, refusals cost none. The configured named chunk is retained against explicit unload so finite contents cannot regenerate.

The local receiving records connect Tent-Right salt, Concord carriage, Curation filing and incomplete-cure Bloom contamination without claiming new cosmology. Doreth and Ivrin use the canonical procedural voice. The half-set uses existing Beasts affiliation plus personal hostility to the actual local staff; Bloom is not added as a social/reputation faction.

### Files and verification records

- New gameplay: `CurationIntakePart.cs`, `CurationReceivingBuilder.cs`; existing pipeline, unload boundary and one exact input-command branch.
- Content: fourteen new blueprint rows, two local conversations, a public branch on the old clerk, two readable receiving records. Existing blueprint objects and the exact courier delivery/reward branch are preserved.
- Art source/import: `ArtSource/CurationYard3D/`, three new entries in the existing humanoid source, scoped static importer/library and selected humanoid import. Runtime changes reuse existing world, portable and equipment rendering.
- Tests: dedicated receiving, content, UI, art and people fixtures, plus narrow natural-weapon and readable-catalog pins. All new Assets C# files carry unique handwritten metas.
- Native route: `ReferenceGladeNativeCuration.cs`, existing native player/batch/combat reporting integration. Isolated save/preferences cleanup restores the prior editor scene.
- Evidence: `Verification/CurationReceiving/Tests/`, `Art/` and both historical/final `Native/` runs. The failed gate-access route remains labelled as a failure.

The art preservation receipt covers2,150 borrowed files:2,149 byte-identical; only the expected humanoid library changes. Exactly75 new generated files are present, with no unexpected outputs. The old56 humanoid source definitions, environment/portable/regional kits and palettes are preserved.


### Cold-eye review (Q1–Q4)

- **Q1, symmetry:** certification transfer/completion share rollback and success-only time payment. Refusal/repeat preserve the physical item. New body/bay views follow current exact owners after movement/load, and the gate follows actual Door state rather than a stale Physics flag. The menu/transaction mismatch and gate-access flaw were fixed before publication.
- **Q2, cross-feature consistency:** two keys use the existing lock authority and remain separate from one another; cabinet stock uses native inventory; the courier quest still uses its original parcel/reward; the half-set uses native melee/AI. Canon faction and natural-weapon/readable pins are audited separately from artistic model counts. The local papers identify themselves as original records.
- **Q3, counter-checks:** positive certification is paired with absent/replaced/wrong-position/still-hauled subjects, distant/dead/hostile actors or workers, rejected capacity, repeated action and outer-command rollback. Invalid source/generation and exact art selection have dedicated counterchecks. A separate adversarial suite tests malformed IDs/backlinks, anchors and a live contained NPC's player-facing log.
- **Q4, doc-versus-implementation:** the new lore and dialogue were checked against current authoritative faction material; old incompatible lore was excluded. The receipt is historical, tools do not imply unimplemented curing, ordinary bodies stay inert, and quarantine remains voluntary. This is CoO-original content, not a claim of a copied Qud mechanic or character.

🟡 Fixed findings: unsafe explicit zone unload/refill; actual menu missing the inventory transaction; cage blocking a public pocket; north gate having no public approach. Final adversarial findings and regression counts are recorded below.

🔵 Deferred polish: worker explanation text remains a static description of the filing problem after certification; the index itself correctly reports that certification already happened. Conditional staff chatter is optional polish, not a prerequisite for using the site. No autonomous NPC tool-working/curing schedule is claimed.


### Final adversarial review

The separate `CurationReceivingAdversarialTests` fixture adds25 cases beyond the original receiving/UI checks. Reference production run:22 pass,3 fail. These exposed one ordinary player-visible defect (the contained creature's failed door attempts printing player-directed refusal messages) and two malformed-state contract gaps (the index's ID was not captured; the counterfoil's KeyPart/PhysicsPart parent backlinks were not checked). These last two are not described as normal-player exploits.

The prepared corrections capture the index ID alongside the other seven owner IDs, validate the counterfoil part backlinks, and send DoorPart refusal prose only to an actual player. Door failure return values, diagnostics, permissions, collision and AI are unchanged. The paired player locked-door attempt still reports refusal. Reference candidate run:53/53, comprising25 adversarial and28 core receiving cases. Native verification of these final corrections is recorded separately below; reference success does not prove their pixels or scheduler timing.


### Broad native regressions and expectation corrections

Native job `fa18c4d151e44216b091ca9af34c882c` completed1,690 cases:1,677 pass,13 fail,0 skipped,1,353.088s. All13 failures were inspected before changing tests. Five expected the old13-record catalogue; they now pin the exact13 canonical plus2 original local records, retain attribution checks and require malformed replacement to clear both kinds. Three disused-wing cases prohibited any initial creature; they now require exactly the authored contained half-set, with no ordinary residents. The combined topology check retains the original `IsOpenGround` domain, all service frontages, room entries and all public reachability; only the six physically enclosed quarantine cells are excepted.

The other five failures were older expectations exposed by this wider selection. Two beam cases used active glade11.10 but expected its former ring model family. The test and existing reference-glade override were byte-identical at the previous commit; only the expected model/source library was corrected, retaining owner, drag, mesh, material, transform and save checks. Three manager cases counted every Merchant as the generic service: previously authored HouseDrama can add a named antagonist Merchant and diminished-head Elder. Six baseline-source and six current-source generation probes confirm this with the drama registered/absent across three seeds. The revised check requires exactly one generic service and validates each extra against its saved marker and living authored drama role. No merchant, beam or terrain production code changes were needed.

The provenance receipts identify their limits: source and reference-run evidence, not a separately launched native baseline editor. Final native reruns of the changed expectations and final production corrections follow below. The broad raw failing result remains available; no historical failure is relabelled as a green run.


### Final native test acceptance

Native job `c3be676f97344294900a06f8827000cf` passed **200/200**, zero failures/skips, in808.065s. It reruns all105 new Curation cases, the changed legacy fixtures, ordinary door behavior, player door input and bump unlocking after the final ownership/logging corrections. Earlier native RED receipts remain separate:68 of70 initial feature checks failed before implementation; all4 real menu tests failed before transactional dispatch; the public-gate reference test failed before the west-facing correction; the dedicated reference audit had3 confirmed defects before its fixes.

The broader run plus corrected/affected reruns cover **1,715 latest cases with no remaining failures**, including105 new Curation cases. This is composite evidence:175 old broad-run cases were superseded by the200-case rerun; it is not a claim of one unfiltered final-state run. The later production delta is the two exact-ownership guards and player-only door-refusal prose, plus the observer's quiet-wait check. Source checks separately pass4 static-kit and6 humanoid cases. The native results, parsed-content preservation, fresh metadata checks and baseline provenance are retained under `Verification/CurationReceiving/Tests/`.

### Final native visit and accepted capture limitation

Final run `683fb8361836400096a5fc034683fd43` reports **20/21 checks**, zero unexpected errors, in67.008s. Its raw report correctly retains `complete:false`, `publicRouteComplete:true` and `errorsFinalized:true`. All public interactions, hauling, certification, finite supplies, native save/load, containment and voluntary combat passed. Twelve actual waits produced eight half-set turn-end records, unchanged staff health and **zero unsolicited door-refusal messages**. After the player opened quarantine, one ordinary wait elicited a hostile attack attempt. The player used Rime Grip once and killed the half-set with one actual rake attack for14damage; no healing was used.

🧪 **Unresolved visual capture limitation:** `curation_actual_attack_pose` alone failed. The observer samples for0.28s around a0.22s attack with a0.065s crossfade, requiring the actual Attack state, positive normalized time and changed bones. It does not require a living target. The earlier21/21 run captured the first attack and observed the second, lethal attack with the same art/animation path; its actual attack screenshot remains separate evidence. The final run's sole attack did not produce a qualifying captured pose. This proves missing final-run visual verification, not a demonstrated gameplay failure or an established lethal-target animation bug. Source review did not justify a speculative production change. Following the user's direction to move past low-importance issues, retain the failed check rather than retry until green or weaken its assertion.

**Can verify:** final generated content, ordinary local input and all gameplay checks listed above; the closed-quarantine screenshot confirms a clean log after real NPC turns. **Cannot verify:** a sampled attack pose on the final run, universal animation timing or natural discovery/balance beyond the disclosed route. Previous art/pose evidence is not relabelled as final-run evidence. Worker chatter and this capture limitation remain the two documented follow-up polish/measurement items.
