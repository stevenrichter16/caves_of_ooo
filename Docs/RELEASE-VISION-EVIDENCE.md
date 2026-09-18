# Release vision evidence — independent bounded review, 2026-09-17

This is planning evidence, not a release certification. Read-only source inspection; no Unity, playthrough or web comparison. The independent source review used `/tmp/coo-regional-verification-20260917`. Subsequent RS26/RS27/RS28 evidence accepted the first situation wave for scoped publication to `/Users/steven/caves-of-ooo`; see its living document and publication manifest. This review itself did not run Unity. The target is a persistent campaign RPG; this review does not propose roguelike resets, mandatory permadeath or between-run metaprogression. Paths below are relative to that root unless explicitly marked clone.

## Authority and the strongest design constraints

- `Lore/10_Bible.md` is the canon lock. Its initial cosmological paragraph alone is insufficient: read `Lore/11_SecondSpine.md` C1–C4 and `Lore/Design/V1-DramaticCore.md` alongside it.
- Urqu is pressure, never a scheming villain. Intent belongs to mouthpieces (`11_SecondSpine.md:16–56`). A conventional final boss who explains Urqu would contradict this.
- The **closure-ledger**, not completion percentage, is canonical (`11_SecondSpine.md:164–190`). Carried-through and deliberately refused acts both drain pressure; silent abandonment feeds it. Do not design a mandatory completionist checklist or punish declining quests. Exact engineering/time thresholds remain design work, not existing simulation.
- `Lore/MYSTERY-LEDGER.md` protects questions that neither NPCs nor ending text may conclusively answer. Naro's motives remain readings, not a collectible “true answer.”
- `Lore/Design/V1-DramaticCore.md:104–142` already scopes v1: five deep factions, two mid, three background; three full/braided god arcs and shorter agreements. It explicitly defers some faction wars, origins, Spirit-killing and forced-Renewal disaster. A full release need not implement every lore paragraph.
- That document's named climax is an **enactment**: acquiring Selen's name is setup; giving it freely, using it as leverage or withholding it is the consequential decision. Evidence should support decisions, not turn mystery into exposition rewards.

## Verified foundations and limits

**Immediate loop.** Current native systems support turn-based exploration/combat, six starting spells/skills, XP/HP/MP/SP advancement, skill purchases, ordinary equipment, weapon component crafting, alchemy, tinkering, harvesting, inventory trade, rentals, repairs and constrained hauling. Relevant code: `Gameplay/Skills/StartingSpellKit.cs`, `Gameplay/Items/CraftingStarterKit.cs`, `Presentation/UI/InventoryUI.Crafting.cs`, `Gameplay/Inventory/`, `Gameplay/Economy/TraderRestockSystem.cs`. Prefix code paths with `Assets/Scripts/`. The broad starter material grant is developer-only; audit was corrected accordingly. Do not claim normal play begins with unlimited crafting resources.

**Recently verified onboarding.** `Docs/CHUNK-GAMEPLAY-IMPLEMENTATION.md` and `Docs/CHUNK-GAMEPLAY-NATIVE-AUDIT.md` document the actual spawn→Morrowfast→field→return journey, one-shot reward, native container pickup, saved travel notes, F5/F6 and cleanup. Farra's dry cloth errand leads toward Nemm's bell materials; Vennit gives actual map-derived destinations. `Gameplay/World/MorrowfastExpedition.cs`, `Gameplay/Conversations/RegionalGuidance.cs`, `Presentation/UI/QuestLogUI.cs` are executable entry points. This is a demonstrated introduction, not a full first-act campaign.

**World presence.** Native map/accessibility audits prove authored places occur and are reachable; counts are not engagement scores. `Docs/VOXEL-WORLD-ACCESSIBILITY.md`, `Docs/FACTION-AND-SACRED-POI-AUDIT.md`, `Docs/CHUNK-GAMEPLAY-AUDIT.md`. Exploration already includes surface biome hazards, named sinkholes, stairs, persistent destruction, lore encounters and resource opportunities. Revisit usually returns the same looted/damaged graph; it is not a new encounter roll. Farming advances in the active zone, not as an offscreen simulated economy.

**Distinct working faction decisions.** Cinderhold's pruner contract exchanges Choir standing for Concord reward. Wellmeet and First Tent have genuine guest-right/hospitality; the scoped repair system is verified at Wellmeet, not asserted for every tent settlement. Drowned Ledger→Marrowstye has an actual marked body-parcel delivery. Olderdeep connects Tepuibone, trust, resting on the plume, Rooted recognition and temporary patch-bloom. Native context and exact owners matter; examining boat frames, toll rolls, archive shelves or signs is not equivalent to operating their described industries. Audit settlement table links source and limits.

**Global stories versus reusable situations.** Six generic quests now each have one canonical host; existing global state is retained. `Docs/CANONICAL-VILLAGE-QUESTS.md`. The first wave's two new templates/five bindings are finite Supply/Recovery requests with saved instance IDs, native cargo/stock, release, notes and optional habitat preservation. The final evidence is 100/100 focused regional tests, 56/56 native checks and no added failures in the full suite; they do not constitute a dynamic regional economy or a general event director. `Gameplay/World/RegionalSituations.cs`, `RegionalRequestPart.cs`, `RegionalSituationNotes.cs`, `RegionalHabitatPart.cs` and `RegionalCargoPart.cs`.

**Endgame evidence bound.** Lore supplies ending designs; a bounded code search did not identify a complete closure-ledger→god agreements→finale→epilogue runtime. Existing `Docs/CHUNK-GAMEPLAY-AUDIT.md` likewise records central missing acquisition/action chains, such as ordinary access to Stillleaf's sealed knowledge. Treat complete campaign/endings as unverified/unfinished until actual entry-to-credits tests exist. `Docs/CONTENT-ROADMAP.md` contains historical combat status and should not be presented as proof of current release completeness.

## Proposed player-facing pillars

1. **An ordinary person whose practical acts matter.** Start with food, shelter, tools and welcomed/refused obligations. Later choices should reuse that grammar at larger stakes. Preserve comedy and beauty; kindness is not merely a reward vending machine.
2. **A world altered through native verbs.** Route, harvest, bargain, fight, burn, carry, repair, release. Environmental and political outcomes should follow the actual owners touched, not an invisible quest flag disconnected from the scene.
3. **Factions offering incompatible forms of care.** Make preservation, incorporation, hospitality, exchange and remembering materially useful and costly. Avoid faction reputation as the only consequence; show changed access, people, goods, practices and places.
4. **Knowledge as a choice about action.** Directions and discoverable clues must be usable. Keep protected mysteries uncertain while allowing concrete commitments with consequences.
5. **Build expression that changes solutions.** Combat, mobility, environmental mitigation, utility magic and social preparation should create genuinely different approaches. Do not require every build to solve every problem identically, or lock critical progress behind one irreplaceable randomly destroyed item.

## Highest missing choices to develop next

- A middle-game objective chain that gives players reasons to choose between already-built towns and descend, with alternate routes and clearly recoverable setbacks.
- More outcomes than “deliver requested item for drams”: lawful alternative supply versus damaging extraction is a start; expand recipient/site responses and refusal consequences before multiplying template count.
- Practical faction decisions with permanent local effects and surviving noncombat routes. Refusal and changed allegiance need authored aftermath, not only integer reputation.
- An explicit knowledge/ending spine whose prerequisites can be acquired through ordinary play. Stillleaf key/reading, god contact and consent cannot remain synthetic tests or prose promises.
- Deliberate revisit opportunities. Prefer named changes driven by time/previous decisions over refilling every chest or silently resurrecting recipients. Define whether offscreen processes advance before promising them.
- A resolved onboarding choice: lore defaults to Sill while the actual start is west of Morrowfast. Explain the relationship in canon/gameplay or deliberately amend the origin framing; do not describe Sill's tutorial as the current spawn.

## Staged release gates (proposed, not completed)

1. **Trustworthy vertical slice:** actual new player, no debug invincibility, intended visibility defaults, readable voxel/camera/UI, complete first regional journey, explicit decline/release, native saves. Preserve full-reveal as a chosen mode rather than silently reverting user preference.
2. **Repeatable regional loop:** two instances independent, no duplicate rewards, destroyed owners stay destroyed, source/recipient order independent, save during transit, alternative resource source, meaningful ecology counterchecks. Add templates only after these gates.
3. **First-act-to-middle-game campaign:** grounded leads from Morrowfast into at least the release-scoped faction network; more than one viable build/route, no synthetic keys, no mandatory missing NPC after a legal action, clear failure/abandonment recovery.
4. **Playable ending spine:** all shipped ending prerequisites obtainable; actual consequential enactments and honest epilogues; complete/refuse/abandon semantics visible, tested and not grindable through cheap repeat requests. Protect Mystery Ledger questions. Each advertised ending must have an ordinary-player entry-to-credits playthrough.
5. **Release quality:** independent playtests across builds/seeds, balance and pacing, tutorial comprehension, keyboard remapping/readability/accessibility, sound consistency, worst-case performance, recovery from interrupted saves and asset installation, known-issue triage. A very large green test count alone is insufficient; existing baseline failures require explicit disposition before claiming release-ready.

Do not sell construction, fishing, boat travel, autonomous caravan economy, all god arcs, dynamic Thinning or systemic endgame as shipped simply because corresponding art or lore exists. Those are separate production commitments.
