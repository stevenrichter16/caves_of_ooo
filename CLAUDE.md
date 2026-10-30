**Current special-site checkpoint — 2026-10-30:** Sinkhole surfaces (Stillleaf, Ginmere, Deepest Cathedral, Lampwell, Spivenor) now use their own biome adapter's rules and art via `FlatPixelSiteAdmission` + per-adapter `AdmittingSites` instances: ~770 → ~8,900 native cells across five zones. FlatPixel 3,775/0. Merchant camps deliberately excluded (authority pins). Mire 'gap' in a census was a LiquidRegistry artifact. Next: SinkholeLip art, sinkhole inner floors, then camps/lairs. See `Docs/GRAPHICS-SPECIAL-SITES.md`.

**Current settlement-ground checkpoint — 2026-10-29:** The 15 ordinary authored settlements (all but Morrowfast/Sill) now draw Grass, StoneFloor, Sand and RoadStone with approved biome art via `FlatPixelSettlementGroundCatalog`; seed-64 native cells 1,111→17,294 of 30,000. RED10→GREEN15/15; FlatPixel regression clean apart from pre-existing terrain glyph audit; one Sill pin deliberately revised. Evidence is offline renders through the real catalog; live play not yet captured (`>` unreachable via computer-use). Slice 2: settlement pools reuse the Sill river water and SandstoneWall reuses the Beating wall frames (→20,501 native; RED10→GREEN; FlatPixel 3,730/0 failed). Slice 3: original editable town-floor packed earth (`ArtSource/Live16x24/town-floor`) for plain Floor → ~28,879/30,000 (96%); FlatPixel 3,736/0 failed. Slice 4: Bush/Tree (Spread rule), DryBrush (Beating rule), Duckboard/Reeds/PeatBank (Sodden rule+art) on real settlement ground; black boxes gone except campfire markers; FlatPixel 3,744/0 failed. Slice 5: original editable Innkeeper/Quartermaster/Scribe bodies (`settlement-keepers`) via the shared residents rule; 146/146. Live play (2026-10-30): fixture 12.11 walked south with real keys into Marrowstye — walls, town floor, road, stone floors and Innkeeper/Quartermaster/Scribe verified in actual play. Slice 6: settlement static memory (remembered ground/walls keep dimmed material, Sill policy) — found live, RED4→GREEN, FlatPixel 3,756/0 failed, live after verified. Slice 7: settlement hearths use the approved burning campfire (ordinary = permanently lit) with an original scorched-earth stain ring on real ground; FlatPixel 3,760/0; live Marrowstye verified. Slice 8: settlement Wardens reuse the approved opening Warden body inside settlement admission only (opening path untouched); FlatPixel 3,764/0; offline evidence. Next: plant memory, Quillhold/Gantry walls, then wilderness gaps. See `Docs/GRAPHICS-SETTLEMENT-GROUND.md`.

**Current checkpoint — 2026-10-29 (Claude owns Unity/Git; Codex automation paused):** (1) GroveSeep at Grove4.7,50,11 now sits on the approved Fen water floor instead of black; `~` relinquished; body/reader unchanged; RED5→111/111; actual Continue194, reader, F5 and fresh Continue200 exact (0/564000). See `Docs/GRAPHICS-GROVE-SEEP.md`. (2) Ordinary cave floors get an up-stair per third (climb-outs that open a way down where none exists); see `Docs/CAVE-CLIMB-OUTS.md`; not yet live-verified. (3) Mac framing: automatic native scale now fills large Retina views (2× editor, 3× fullscreen; Deck stays 1×); see `Docs/MAC-DISPLAY-FRAMING.md`. Packages unchanged (aa92e2746). Next: native damp-ground backing for isolated seeps or the largest remaining whole-scene glyph gaps; skeletal sentry `Z` actor glyph observed in Beating ruins.

**Current TarSeep checkpoint — 2026-10-29T02:00Z (Claude owns Unity/Git; Codex `overnight-clearing-art-pipeline` paused 00:48Z):** Beating RuinField19.12 tar seeps33,6–7 now draw an original native low tar film over their true sand, keep the independent oil mark, and show the native portrait in world and underfoot readers. Art went dome→heart→oval→film after independent and actual-play review. Tests: RED0/9→Beating111/111 (inert-effect case mutation-proven), FlatPixel*3,689 pass/0 fail/6 existing inconclusive. Actual Editor Continue196, real keys, F12 declared for the sentry-guarded walk; final art, readers, oily underfoot, F5 and fresh Continue199 verified (world crop253/564000 differ, actors only; tar/ground identical). Packages unchanged (aa92e2746). See `Docs/GRAPHICS-TAR-SEEP.md`. Next GroveSeep wet backing on the approved Fen water bank, then largest whole-scene glyph gaps.

**Current CharmFlowers checkpoint — 2026-10-29T00:15:25.008069+00:00:** Ordinary Spread19.11 now reuses approved flower sprites over truthful Grass. Native RED29pass/6fail→GREEN61/61; six actual Editor images verify Look, underfoot identity,2× walking and freshContinue198 (0/564000 changed world pixels, HP40). Independent source/visual review clear within scope. Repeated rows, sparse outer clearing, six separate FlowerField cells and other glyphs remain; no whole-area, population or shadow acceptance. Current aa92e2746 packages exclude this new fix. Normal saves/preferences,49 foreign paths, approved art and builds preserved; Unity closed, no cleanup. See `Docs/GRAPHICS-SPREAD-CHARM-FLOWERS.md`. Next compare TarSeep/oil and GroveSeep against other large scene gaps. Last actual hourly report 2026-10-28T23:57:33.692637+00:00; next 2026-10-29T00:57:33.692637+00:00.

**Current packaged regional delivery — 2026-10-28T23:43:03.024826+00:00:** Fresh Mac/Linux packages from aa92e2746 (runtime3705cb241) now include reviewed Spread environment/fixtures, Grovelands fixture continuity, Beating sandstone shading and fresh ruin brush groups/native reader. Both builds0errors/4warnings;15 actual Mac captures/4processes verify readers, vent interaction, ruin walking and exact saved/fresh-Continue world crop0/564000. Linux archive integrity verified; Linux/physical Deck execution and full-area polish remain open. Normal1005save entries/preferences,49foreignpaths, prior packages and approved sources preserved; no cleanup. Unity/player closed. See `Docs/Verification/Live16x24/packaged-regional-batch-delivery.md`.

**Handoff requested:** Read `Docs/GRAPHICS-AGENT-HANDOFF.md`. Next candidate is the large actual CharmFlowers glyph landmark at Spread19.11, immutable197, compared with live TarSeep/oil at Beating33,6–7 and GroveSeep backing. Ground cover is in scope; crops/animation deferred. Coordinate Unity/Git ownership with the existing recurring automation. Last actual hourly report22:44:16.755215UTC; next23:44:16UTC (handoff/milestone does not reset it).

**Current regional checkpoint — 2026-10-28 23:20UTC:** Grovelands peat/steam fixture continuity, restrained Beating wall shade, fresh RuinField shelter groups and the actual-driven DryBrush underfoot portrait correction are bounded Editor-verified. Read the current continuation in `Docs/GRAPHICS-COMPLETION-PLAN.md` and the regional-fixtures/Beating-shadow/Beating-ruin-composition records for tests, captures, limits and next work. Source `3705cb241f99821eca9156e7e4719b4f5576f6f7` is pushed and verified on origin/main; packages remain7f73b6a78. Next batch delivery and live TarSeep/oil baseline from196, then other observed regional glyph gaps. No full-area polish acceptance. Last actual hourly report22:44:16UTC; next23:44:16UTC. Normal saves,49foreignpaths, approved art and builds preserved; storage inspection only.

**Current fixture continuity — 2026-10-28 22:37UTC:** Ordinary Spread now reuses approved connected CopperPipe and PeatBog sprites, and preserves actual ground beneath both and existing SteamVent bodies.167/167nativechecks pass; matched actual scene, three readers, realventcrossing,2xreadability and freshContinue193 verified (0/564000worldpixel differences). Independent source/visual review0P1/P2. Actual eastward trial died toscrabbler and is retained separately; peat/pipe crossing not claimed. See [fixture record](Docs/GRAPHICS-SPREAD-FIXTURES.md). Next audit same gaps in actual Grovelands/Sodden, then bounded shadows/composition. Currentpackages unchanged; fullarea polish remainsopen. Last actualhourly report21:44:10UTC; next22:44:10UTC.

**Current ordinary Spread checkpoint — 2026-10-28 22:15UTC:** Ordinary Grass, RoadStone, Hedge, Tree and Bush now reuse approved native art beyond the opening. Native230-case run succeeded after16 initial failures plus2 actual-driven underfoot-reader failures. Actual OldRoad/Hedgerow, grass/road/hedge readers, walking,2×readability and freshContinue192 are verified. The final grass/road underfoot portraits now match the world material. Hedge bump dealt2damage and retained collision; restart preserved8HP, with252/564000 local worldpixel differences of unproven cause. Independent source and corrected actual review found no actionable P1/P2 within this scope. Road contrast, repetition, sparse parcels, other glyph owners and all full-area polish remain open. See [Spread record](Docs/GRAPHICS-SPREAD-ENVIRONMENT.md).

Next: actual owner evidence identifies PeatBog hazards, SteamVent and CopperPipe objects among the remaining black-backed gaps. Audit their existing presentation and choose one coherent environmental continuity pass; preserve their mechanics and compare other-biome needs. Broader restrained shadows and periodic meaningful population remain active. Current7f73b6a78 packages exclude Spread reuse; no physical Deck claim. Crops/animation deferred. Unity is stopped, isolation cleared; post-user-play1005save entries, settings,49foreign paths and approved art preserved. Source `1e6bbe211c4ec03eb20201f28b0406d7384b836f` is pushed and verified on origin/main. Last actual hourly report21:44:10UTC; next22:44:10UTC (5:44PM Chicago).

# Caves of Ooo — Project Rules for Claude

**Current packaged delivery — 2026-10-28 21:27 UTC:** Reviewed environmental materials, Sill contact/wall shading and SummitScrub composition are now in fresh Mac/Linux packages from source `7f73b6a784e3c8016c057ff77177e130128e13d7`. Nine actual Mac captures across four processes verify Sill/glade travel, wall/Bush readers and fresh Continues. Summit restart matches all 564,000 world pixels; Sill differs by 1,121 local pixels with cause unproven. Both builds succeeded (0 errors, 4 warnings each); Linux archive integrity is verified, Linux/physical Deck execution is not. Prior43a151ddb builds, normal saves/preferences and49 unrelated changes are preserved; no cleanup. See [delivery record](Docs/Verification/Live16x24/packaged-environment-batch-delivery.md).

Next: an earned ordinary Spread road/hedge baseline outside the six opening graphs, then one coherent environmental material pass selected from actual scene/owner evidence. This is planned, not accepted coverage. Broader shadow families, meaningful population and all full-area polish remain open; crops/nuanced animation stay deferred. **Last actual hourly report remains20:44:12.183742 UTC; next21:44:12 UTC.** Delivery commit `35d524a8c283c6ff122f06bc2ce81f9171479647` is pushed and verified on origin/main. Unity is closed; resume from the documented next baseline.

**Current SummitScrub checkpoint — 2026-10-28 21:08 UTC:** Fresh seed 64 now has 21 existing Bush owners in separated 9/7/5 drifts. Final 213 native cases passed, including 32 new cases and the actual-driven spacing RED. Three normal-zoom view pairs, real reader/walkthrough and Continue 188 (turn 1630, HP 40) verify a bounded composition improvement. Bush IDs/positions persist exactly; 173 other world pixels differ locally with cause unproven. Independent review found 0 actionable P1/P2. No PNG changes, new sprite-coverage claim or full-area acceptance. See the [composition record](Docs/GRAPHICS-SUMMIT-SCRUB-COMPOSITION.md).

Source `98ac4d64b9e01ce43375087c9b3b24beb4f6c8c8` is pushed and verified on origin/main. Next batch-package the reviewed environment, shadow and composition changes, then verify normal launch/travel. Current 43a151ddb packages remain unchanged; no Deck claim. Central emptiness, flat domes, repeated scrub stems and glyph props remain open. Crops/nuanced animation stay deferred. **Last actual hourly report 20:44:12.183742 UTC; next 21:44:12 UTC.**


**Current light/shadow pilot — 2026-10-28 19:58 UTC:** The current Sill player now has a subtle18% contact mask on disclosed dry grass/stone, reusing unchanged art and the pooled renderer. Matched actual185 images differ only16pixels beneath boots; actual court/stone/inventory/well-marker/water/reader/dry recovery/travel/freshContinue186 verified. Native281passed,0failed,1existing unreliable allocation-counter skip;18new cases. Independent review0P1/P2, deliberately faint at normal zoom. Full restart has482other local changed pixels; player crop exact, broader cause unproven. Source `e71858ddfe9d2df76f00ba6e6623e6499284b55d` is pushed and verified on origin/main. See [pilot and evidence](Docs/GRAPHICS-LIGHT-SHADOW-PILOT.md).

Next: one bounded masonry/counter shade trial with explicit disclosed receiver/rendering rules, then compare another biome. Walls, props, other actors/areas and wider lighting remain planned. Environment material polish and periodic chunk population continue; crops/optional animation deferred. Packages remain43a151ddb; no Deck claim. **Last actual hourly report19:44UTC; next20:44UTC.**


**Current continuity and lighting direction — 2026-10-28:** Sill now reuses approved native water for its actual visible river and retains subdued grass/floor/masonry/vine identity in static memory. Final203/203nativepassed; actual pool reader, wet/dry crossing, court return, glade travel and freshContinue185 (turn850,HP40) verified. Independent source/visual review0P1/P2. Restart is not pixel-exact:1892/564000 world pixels differ locally; cause unproven. Source `dfbcaa008897e0365d4d077d62af7d2e2c225b7b` is pushed and verified on origin/main. Whole-area polish and current43a151ddb packages/Deck remain open. See [continuity record](Docs/GRAPHICS-SILL-CONTINUITY.md).

**New direct user priority:** design and iterate a restrained light/shadow layer. Start with a small reversible contact-shadow pilot, then bounded wall/prop shade; use actual captures before expansion, preserve flat16×24/fog/readability and avoid a lighting-engine rewrite. See [shadow pilot](Docs/GRAPHICS-LIGHT-SHADOW-PILOT.md). Recurring prompt updated. Environmental materials and periodic chunk population remain active; crops/optional animation deferred. Last hourly report18:43:41UTC; next19:43:41UTC unchanged.


**Current Sill court checkpoint — 2026-10-28; source published:** Fresh Sill now groups its existing smithy and provisioner around a clear north-of-well court. Generation passed83/83 native cases (18 new); the exact approved MarketStall body passed152/152 (17 new). Actual fresh64/65 entered both thresholds;64 also checked forge/well menus, glade travel and freshContinue182 (turn580,HP40);65 opened both real trades without purchase and saved183 (turn390,HP40). Cached180 retains old wall7354. Independent source/actual review found0P1/P2 within this bounded scope. Keepers move normally; this is composition, not a population-count increase or static display. Counter PNG/import bytes are unchanged; Provisioner and forge glyphs remain visible debt. See [court evidence](Docs/GRAPHICS-CHUNK-POPULATION-PILOT.md).

Source `7448a7d2d26125691304e43840829acd7002395e` is pushed and verified on origin/main. Packages remain`43a151ddb`; whole-Sill/whole-area polish, wider density and physical Deck acceptance remain open. Next compare river/shoreline and fog-memory gaps against Spread environmental needs before choosing one coherent pass; no new scope is accepted yet. Crops/animation remain deferred; storage inspection only. **Actual hourly report sent18:43:41UTC; next19:43:41UTC.**


**Current environment continuation — 2026-10-28T17:46:17.553542+00:00:** Overwrit scraped-ground detail variants1–3 now have restrained flat abrasion; quiet0 and bodies4–7 remain exact. Two matched actual formations, ground/bench readers, ordinary walking and exact freshContinue179 support a bounded material pass. Native70/70 passed; independent source/actual review0P1/P2. Source `83ebfec320e6fb389b73f68cc39a22941683b480` is pushed and verified on origin/main. Repeating short marks and all full-area polish remain open; current43a151ddb packages unchanged. Next Sill well-and-shop population pilot through existing generation/owners; planned only, fresh baseline/RED/routes required. Last actual hourly report17:43:26UTC; next18:43:26UTC. Crops/animation deferred; storage inspection only. See [Overwrit record](Docs/GRAPHICS-OVERWRIT-ENVIRONMENT.md) and the chunk-population pilot.

**Recurring chunk population — direct user direction, 2026-10-28:** More chunks should approach the spawn area's richness, with purposeful groups of NPCs, environmental objects, buildings, ruins and landmarks. Consider population during every hourly planning pass and regularly implement a bounded composition improvement alongside material polish. Use biome and area character to decide where density belongs; retain deliberately empty places. Planned additions through real generation/placement systems are authorized, including actual NPC roles and building/object owners. Verify paths, entrances, sightlines, interactions, collision, encounter balance, fog, save compatibility and performance. Avoid uniform clutter or fake services/loot. Compare whole-chunk before/after gameplay and track population separately from material coverage. Do not defer this work until every sprite is polished. See [population plan](Docs/GRAPHICS-CHUNK-POPULATION.md).


**Current environment checkpoint — 2026-10-28 17:18 UTC:** Sill now has nine original native material frames: quieter lawn, distinct flat room floors and shaded straight masonry. Three matched actual pairs, all three readers, opening travel/return and exact freshContinue178 support the bounded material pass. Final native:215passed,0failed,3existingSodden allocation probes inconclusive; no allocation claim. Independent source/actual review0P1/P2. Source `c7b50b7819afedfd3ad60327d05c0c21b7b7d6fb` is pushed and verified on origin/main. Whole-settlement composition, repetition and other areas remain open; current43a151ddb packages unchanged. Crops/smallitems/animation deferred. Last actual hourly report16:42:59UTC; next17:42:59UTC. See [Sill record](Docs/GRAPHICS-SILL-ENVIRONMENT.md).


**Current environment checkpoint — 2026-10-28 16:45 UTC:** Beating’s 12 sand, paving, road and masonry frames are bounded Editor-verified. The other 22 frames and original editable sources remain intact. All 151 native cases passed; seven actual captures cover ruins, dunes, the shared shallow floor, wall interaction/reader and clean Continue176 (0/564000 world pixels differ). Independent source/visual review found 0 actionable P1/P2 issues. Source `fb0c906ae1a58a847b2bd25dc589ace5355a8402` is pushed and verified on origin/main. Broad sparse composition and subtle material repetition remain open; current43a151ddb packages exclude this pass. Next: Sill settlement lawn, masonry and interior material hierarchy after scoped consumer review. Crop work and subtle animation stay deferred. Last actual hourly report16:42:59 UTC; next17:42:59 UTC. Storage inspection only. See [Beating record](Docs/GRAPHICS-BEATING-ENVIRONMENT.md).


**Actual hourly report — 2026-10-28T16:42:59.603525+00:00:** Completed Grovelands4nativefloorframes,159nativepasses,actualGrove/Fen/CompostingField and exactContinue173; sourcea1662dc8b verifiedmain. Beating12environmentframes locallycomplete,22preserved;151nativepasses,7actualafterviews including sharedshallowfloor, wallstrike/reader and exactContinue176; independent0P1/P2. Beatingpublicationinprogress. Nextsession: publishBeating then select highest-impact settlement/regionalwall-floorpass from Sill/Overwrit/Spread comparison. Wholechunkcomposition,otherareas,packages/Deckremainopen;crop/subtleanimationdeferred. Nextreport2026-10-28T17:42:59.603525+00:00. Storageinspectiononly.


**Current environment checkpoint — 2026-10-28 16:19UTC:** Grovelands now has four original native forest-floor frames with flatter low moss/litter, replacing shared lawn repetition. V1 object-like stamps were rejected and corrected in V2.159/159native pass; actual Grove/Fen/CompostingField, Grass reader, dry/water/dry crossing and ordinary return/freshContinue173 verified (0/564000worldpixel differences). Independent final review0P1/P2. Source `a1662dc8b6b172c7b3d1e4627ab4a3dc93105d5b` is pushed and verified on origin/main; current43a151ddb packages exclude this pass. Sparse composition, repeated soft fragments and all full-area polish remain open. Next [Beating sand/paving/ruined-wall materials](Docs/GRAPHICS-BEATING-ENVIRONMENT.md). Crops/subtleanimation deferred. Last actual hourly report15:42:40UTC; next16:42:40UTC. Storage inspection only. See [Grovelands record](Docs/GRAPHICS-GROVELANDS-ENVIRONMENT.md).


**Current environment checkpoint — 2026-10-28 15:40UTC:** Stump21-frame floor/solid-dome pass is bounded Editor-verified:111/111 native tests, actual matched Summit/Gorge, unchanged blocking/passable ledge, reader and freshContinue172 with0/564000worldpixel changes. Independent source/visual review0P1/P2. Source `ab45624ed7580a60ab0090a481b3f23e56e6cc9d` verified on origin/main; packages43a151ddb unchanged. Flat platform-like caps and broad sparse composition remain open. Next Grovelands forestfloor/groundcover. Crops/animation deferred; all full-area polish remains open. Last actual hourly report15:42:40UTC; next16:42:40UTC. Storage inspection only. See [Stump record](Docs/GRAPHICS-STUMP-ENVIRONMENT.md).

**Current user focus — environmental polish (2026-10-28):** Prioritize environmental tiles, walls, flooring, grounds and ground cover across every biome/area now. Defer plants/crop-system work, extra crop states, small item packets and nuanced animation. Keep straight layered/shaded walls with quieter flatter floors. Review normal-zoom whole scenes, ground transitions, palette/material identity, repetition, clustered cover, density/quiet patches and path/hazard readability. This overrides the older coverage-before-polish queue. Sodden ground/mire material pass is now bounded Editor-verified; next Stump floor/blocker material polish, then Grovelands. Preserve separate biome resources. Read [environment plan](Docs/GRAPHICS-ENVIRONMENT-POLISH.md).


**Current Buckler checkpoint — 2026-10-28 13:12 UTC:** Original editable native16×24 Buckler replaces a shared equipment glyph. RED8→final GREEN156, four actual before/eleven after captures, ordinary drop/pickup/unequip/re-equip and fresh Continue168 preserve owner2700;0/564000restart world pixels differ. Independent runtime/actual review clear; root independently reviewed original art. Source publication is next. Current43a151ddb packages exclude Buckler and remotely published seeds(c0548d1eb). Next bounded reuse: the two planted ordinary crop stage0/1 identities in earned166, then compare broader surface/services needs. No missing standing-ripe frame: maturity releases yield. All55+5area-polish scopes remain open; no new package/Deck claim.49foreign changes, normal saves/game settings and approved art/builds preserved; only Unity session/test bookkeeping changed. Storage inspection only. Last actual hourly report12:40:49.713819UTC; next13:40:49UTC. See [Buckler](Docs/GRAPHICS-BUCKLER.md) and its bounded Editor acceptance.


**Historical graphics checkpoint (2026-10-28):** Portable gladroot/emberwheat seed frames are integrated: RED5→GREEN142, four actual before/eleven accepted after views, normal pickup/both planting actions and freshContinue166;0/564000world pixels differ. Independent source review clear; root actual-image review and source publication pending. Current packages43a151ddb exclude seeds. Next original Buckler body plan; read completion continuation. Five approved pins/49foreign paths/saves/builds preserved. Sparse mire, outside-opening crop art and all55+5polish scopes remain open. Root owns Unity/Git; storage inspection only. Last actual hourly report11:40:19.440648UTC, next12:40:19UTC unchanged.

**Whole-game credit priority (2026-10-28):** Three shared specialists are delivered from source83e1e568f: Weaponsmith plus original Tinker/Well Keeper. Native99 passes, actual Mac14 captures/three processes and exact restart comparison; Linux archive verified, no Linux/Deck execution. Finish delivery evidence publication, then select the largest observed shared equipment/prop or regional sprite gap. See the current completion continuation. All55+5 polish scopes remain open. Root owns Unity/Git; preserve49 foreign changes, saves/art/builds. Storage inspection only.

**Active user direction — 2026-10-27, cave detour ended:** Resume native sprite
coverage across every biome and area. Shared Bed/Chair delivery is verified.
Slate, Quartzite and Obsidian material delivery are verified.
Follow the current continuation at the top of this file and in
Docs/GRAPHICS-COMPLETION-PLAN.md for the active surface coverage queue.
Keep straight wall silhouettes as the current default. The isolated shale parity
prototype is parked, disabled and not accepted as reference parity; preserve its
editable candidates and evidence reversibly, with no further automatic art
iterations. Carry layered/shaded 2D wall relief and flatter quiet floors into the
later biome-by-biome, area-by-area polish phase. Coverage is not polish acceptance.
Normal saves, approved art, current builds and unrelated work remain preserved.
See [the parked study](Docs/GRAPHICS-SHALE-PARITY-TEST.md) and the shared-furniture plan.

**Chunk composition requirement (2026-10-26 local):** The user also requires
chunks not to feel too bare. Fill regions with coherent, biome-appropriate
terrain variation, flora, props and landmarks while preserving readable routes,
hazards and interactions. Judge actual whole gameplay views, not only sprite
sheets. See `Docs/GRAPHICS-WORLD-SPRITE-COVERAGE.md` for the acceptance gate.

> Auto-loaded every session. Read this first. The rules below are
> non-optional unless the user explicitly relaxes them in a given turn.

The full **Methodology Template** lives at `Docs/QUD-PARITY.md` §5162-6209.
This file is the always-on summary + workflow trigger. When in doubt about
specifics, open that section.

---

## Always-on rules

### Active graphics work — read before continuing

**User priority: graphics completion is the game's #1 priority and a release
blocker.** Incomplete/inconsistent graphics are preventing the existing deep
mechanics from delivering their value. There is no arbitrary daily/weekly
deadline. Use delegated judgment beyond individual prompts to solve this;
prioritize coherent, verified normal gameplay over unrelated feature growth
or asset counts. Continue across sessions with precise handoffs rather than
stopping after isolated packets. Preserve the user's approved art direction.

**Required later polish phase (2026-10-27):** After broad sprite coverage,
revisit every biome and area for a serious visual polish pass. Track area polish
separately from sprite integration: whole-chunk composition/fullness, palette,
silhouettes, material and gameplay readability, interiors, landmarks, travel,
visibility states and reader identity need actual before/after gameplay review.
See the completion plan and world coverage ledger; asset counts and one center
viewport cannot establish a polished area.

**Latest user priority (2026-10-27): cover missing/inconsistent sprites across
ALL biomes and areas before nuanced animation polish.** The six connected opening
scenes are an anchor, not the full scope. Include terrain, flora, actors/NPCs,
items/equipment, props/architecture and essential gameplay states across surface,
underground and special sites. Maintain a source-grounded coverage inventory;
missing and not-yet-audited coverage must remain explicit. Preserve existing
animations and accepted sprites. Defer new subtle animation work unless a named
defect blocks gameplay understanding or interaction.

For this session's visual overhaul, read `Docs/LIVE-16X24-ART-DIRECTION.md`
first, especially **Required production pipeline and agent handoff**. Read
`Docs/GRAPHICS-COMPLETION-PLAN.md` next for the current delivery order:
normal-launch 2D continuity, followed by missing sprite coverage across all
biomes/areas with coherent scene-level integration. `ArtSource/Live16x24/release-coverage.json` records bounded
coverage and pending acceptance; it is not a complete census. The actual
game target is flat 16×24 pixel art. Use a playable real-world greybox,
capture it, reinterpret it with imagegen, extract parts, produce sprites and
animations, then reintegrate and verify in-game. The user authorizes agents to
choose and use any available tools needed and to continue autonomously. This
instruction applies to the graphics effort; preserve the existing project
verification rules below.

1. **TDD is the default cadence.** Write the failing test (assertion OR
   compile error) BEFORE the production code. Run it. Confirm RED.
   Then implement the minimum to pass. See §2.1.

2. **Pre-impl verification sweep on any major feature.** Read every
   reference the plan cites. Confirm API shapes, signatures, line
   numbers, mechanic semantics. Log corrections in a table BEFORE
   writing code. The combat port caught **3 false premises** this way
   (Phases B, F, H) — saved an estimated 2-3 days. See §1.2.

3. **Living docs over chat-buffer plans.** New phase, milestone, or
   audit gets a section in `Docs/<thing>.md` with: status, Qud
   reference, implementation snippet, divergences table, in-phase
   self-review, implementation log, files changed. **Update the doc
   in the same commit as the code.**

4. **In-phase self-review BEFORE commit, not after.** Severity markers
   🔴/🟡/🔵/🧪/⚪. Fix 🟡+ pre-commit. Defer 🧪/⚪ with a note. See §5.

5. **Counter-checks for every positive assertion.** Every "X happens"
   test pairs with "Y identical-setup-but-flag-flipped does NOT
   happen." Without counter-checks, "the test passes" can mean "the
   precondition was vacuous." See §3.4. **Note:** counter-checks are
   per-invariant pairs, distinct from the dedicated adversarial test
   sweep (§Adversarial test sweep) which probes bug-class taxonomy
   per-feature. Both gates apply.

6. **Honesty bounds in every PlayMode / live report.** Explicit
   "can verify (script-observable)" and "cannot verify (visual / feel)"
   sections. Don't paraphrase raw output. See §6.3.

7. **Scope-divergence transparency.** If the plan said X and you ship
   Y, the commit body has a SCOPE DIVERGENCE section explaining why,
   with file:line citations. See §6.4.

8. **Commit message template** (§2.3):
   ```
   <type>(<scope>): <tight present-tense summary>

   <2-3 sentence problem statement in user-visible terms>

   IMPLEMENTATION NOTES (risks verified before writing code)
     1. ...

   SCOPE DIVERGENCE FROM THE PLAN (if any)
     ...

   <PHASE> SELF-REVIEW (Methodology Template §5) (if applicable)
     🟡 ...
     🔵 ...

   Files:
   - NEW/MOD <path>: <one-line purpose>

   Tests: <N> -> <M> (+D). All green.

   Co-Authored-By: ...
   ```

---

## Major-feature workflow (use when starting any non-trivial work)

For features matching two or more of:
- Touches multiple systems (AI + combat + UI, etc.)
- Requires new blueprint/JSON content
- Claims parity with a reference codebase
- Has non-obvious failure mode (state-machine, timing, RNG)
- Will be wired into player-exercisable gameplay

**Do these in order. Do not skip steps.**

1. **Plan to disk** — write `Docs/<feature>.md` with goal, scope,
   content-readiness analysis (🟢/🟡/🔴/⚪), sub-milestone breakdown.
   §1.1.

2. **Verification sweep** — read every reference the plan cites
   (Qud source, existing CoO classes, blueprints). Produce a
   corrections table for any drift. Single highest-leverage step
   in the protocol. §1.2.

3. **Scope-prune with rationale** — if the sweep reveals a sub-feature
   is redundant in current code, document the cut + cite the lines
   that make it redundant. §1.3.

4. **Sub-milestones smallest-blast-radius-first** — each commits as
   one reviewable change, independently revertable, ships one complete
   testable behavior. §1.4.

5. **Per sub-milestone, RED→GREEN→adversarial→review→commit:**
   a. Phrase invariant in user-visible terms (§2.1)
   b. Write RED test against that invariant
   c. Run, confirm RED
   d. Implement minimum to pass
   e. Run, confirm GREEN
   f. Add counter-check (§3.4)
   g. Add adversarial / mutation-resistance test (§3.9)
   h. Self-review with severity markers (§5)
   i. Fix 🟡+ findings
   j. Update living doc with phase section
   k. Commit using the §2.3 template

6. **PlayMode sanity sweep** for player-visible features (§3.5):
   `manage_editor play` → preflight check → per-scenario raw output
   → `manage_editor stop` → summary table with honesty bounds.

7. **Manual playtest scenario** for visual/feel-dependent features
   (§3.6): a `Scenarios/Custom/<Name>.cs` + menu entry. **If the
   mechanic is *measurable* (numbers, not just feel), make it a
   Deterministic Self-Auditing Scenario — see the dedicated section
   below.**

---

## Self-audit checklist (run BEFORE every commit)

For each diff hunk, ask:

- [ ] Does this commit introduce production behavior? Is there a
  test diff in the same commit covering it? (§2.1)
- [ ] Did the test fail RED before this implementation? If you can't
  recall, you compressed steps — note it in the self-review.
- [ ] For every positive assertion, is there a counter-check that
  would FAIL if the wiring were wrong? (§3.4)
- [ ] Are there magic numbers that should be named constants?
- [ ] Are there docstring claims unbacked by tests? (these become
  🧪 findings)
- [ ] Does any commit message claim "Qud parity" when the work is
  actually CoO-original? Re-classify per §4.2.
- [ ] If the diff exposes new public API (method, field, event), is
  the contract documented inline?
- [ ] Did the verification sweep reveal a false premise? Did you
  document the correction in the commit body?

Findings format (one per finding):

```
🟡 Finding N — <one-line title>
**File:** <path>:<line-range>
<1 paragraph: what's wrong, what's observable>
**Why it matters:** <concrete consequence>
**Proposed fix:** <1-3 sentences, sketch only>
```

Fix every 🟡 and 🔴 pre-commit. Defer 🧪/⚪ with a note in the doc.

---

## Post-implementation cold-eye review (MANDATORY after every multi-commit feature)

**Tests passing is necessary but not sufficient.** Green tests prove
the code does what the test asserts; they don't prove the code is
internally consistent, symmetric with neighbors, or matches the
docs. Always run the four-question pass below AFTER tests are green
and BEFORE you call the feature done. Empirically this caught four
real issues in D2 (hook-position asymmetry, redundant payload field,
test-coverage gap, doc-vs-impl drift) AFTER all 29 tests were green
and AFTER the merge had landed on main.

### Q1 — Symmetry check

When you add a feature that mirrors an existing one (OnApply mirrors
OnRemove, "Begin" mirrors "End", forward mirrors backward, etc.):
**open both source files side-by-side and read them line-by-line.**
Position of the new code relative to surrounding calls must match
the existing one. Caught D2.1: OnApply was firing before
`effect.Applied`, but D1.2's OnRemove fires after `effect.Remove` —
asymmetric chronological position relative to the user-visible
message-log entry.

The check: *if I swap a record's category from "OnApply" to "OnRemove"
in my head, would the surrounding code make equally good sense?*
If no, one side is misplaced.

### Q2 — Cross-feature consistency

When you add multiple similar features in one ship (D2 added 5
hooks + 1 tool), audit **every public-facing shape** — payload
schemas, return types, parameter names, channel names — for naming
convention. Make a mental table:

```
Hook            | category | kind          | actor | target | payload fields
OnRemove        | effect   | OnRemove      | -     | yes    | effect, duration, cause
OnApply         | effect   | OnApply       | yes   | yes    | effect, duration, justApplied, forced
DamageDealt     | damage   | DamageDealt   | yes   | yes    | amount, hpAfter, lethal, attributes
turn/Begin      | turn     | Begin         | yes   | -      | blueprintName, hp        ← had `entityId` redundant w/ ActorId
turn/End        | turn     | End           | yes   | -      | blueprintName, hp        ← same
```

Each column should follow a consistent rule. If one row deviates,
flag it. Caught D2.4: turn payload duplicated `entityId = actor.ID`
in the payload while the other hooks correctly put IDs only in the
top-level field.

### Q3 — Counter-check completeness

For every payload field with a non-trivial branch (boolean flags,
enum-ish values, success/failure paths, optional vs default args),
verify **each branch is asserted by a test**, including a counter-
check per §3.4. Caught D2.1: the `forced` field had two paths
(`ApplyEffect` → false, `ForceApplyEffect` → true), only the false
path was implicitly tested. A buggy impl that hard-coded
`forced = false` for both paths would have passed all 5 D2.1 tests.

The check: list every non-trivial field in the payload and trace
which test exercises which value. Gaps in the table become tests.

### Q4 — Doc-vs-impl drift

Open the design doc (`Docs/AI-OBSERVABILITY.md` for diag work,
`Docs/<feature>.md` generally) and read each spec'd
parameter / return shape / response field against the **actual
shipped code**. Caught D2.5: AI-OBSERVABILITY.md's generic-tools
table claimed `diag_count` returns `{ count: int }`, but the shipped
tool returns
`{ count, total_scanned, sample_first_trace_id, sample_first_kind, tool_version }`.

Doc-vs-impl drift accumulates silently and turns living docs into
useful-fiction. The fix is fast (5 minutes per ship); the cost of
not doing it compounds.

### Process

1. Tests green ✓
2. Merge to main ✓ (or about to)
3. **STOP. Run the cold-eye pass.** Read all the diffs together, not
   one-by-one.
4. Found something? File a `fix/<thing>-cold-eye-review` branch
   with a single self-contained fix-commit. Don't bury it in a
   future feature commit. Push. Merge. Now done.
5. Found nothing? Note "cold-eye review pass complete, 0 findings"
   in the merge commit body so a future reader knows it actually
   ran.

**Self-directive: I MUST run this cold-eye pass after every
multi-commit feature, even when tests are green and the merge
feels clean. Tests-green-feels-clean is exactly the state where
latent inconsistencies hide.**

### Run BOTH audit angles — taxonomy AND Qud-parity-first

The cold-eye review and the optional post-merge Explore-agent audit
should run from **two distinct angles**, not one. A single angle
has a fixed checklist and misses what the checklist doesn't ask
about.

**Angle A — Bug-class taxonomy** (the default):
- Null safety, atomicity, iterator-vs-mutation, race conditions
- Save/load reach, anti-exploit gates, diag dispatch
- Counter-check completeness, dead-code sweep
- Driven by the adversarial-test-sweep surface list

**Angle B — Qud-parity-first** (the missing half):
- Read the CoO impl and the matching Qud source side-by-side
- For each line of CoO code, ask: "what does Qud do here, and am I
  doing it the same way?"
- Drift findings: 🔴 CRITICAL (gameplay-observable Qud contract
  broken) / 🟡 NOTABLE (observable but acceptable) / 🔵 NIT
  (no gameplay impact) / ⚪ DOCUMENTED (deliberate, already noted)
- What events does Qud listen for that CoO might not?
- What guards does Qud have that CoO is missing?
- What helper methods / convenience wrappers exist in Qud's source
  that CoO would silently lack?

**Why both:** the F.3 Followers feature illustrated this empirically.
The first audit pass ran Angle A and shipped 5 findings (all real),
but deferred a 6th — "OnDestroyObjectEvent unapply hook" — as "CoO
lacks these events." A second pass running Angle B re-checked that
claim, found CoO DOES fire a `"Died"` event on the dying entity
(CombatSystem.cs:916), and surfaced the real latent bug: killing your
own follower leaked rep indefinitely. The taxonomy checklist didn't
include "what events does Qud listen for that CoO might not?" — that's
a Qud-parity question, not a bug-class taxonomy question.

**Process:**
1. Run Angle A first (it's the cheaper checklist — null safety,
   atomicity, iterator safety — all easy to enumerate).
2. **Then** run Angle B specifically (Explore-agent prompt that says
   "Qud-parity-first" and lists the Qud reference files alongside the
   CoO files). The two passes are complementary, not redundant.
3. Triage findings from both into one fix branch. Note explicitly in
   the merge commit which angle caught each finding so future audits
   can be honest about what each pattern covers.

**Self-directive: for any Qud-parity feature, I MUST run BOTH audit
angles before declaring the phase complete. Skipping Angle B because
"Angle A found nothing meaningful" is exactly how Qud-contract drift
slips through — the taxonomy angle isn't looking for those.**

### Hypothesis-driven deep audit — write RED tests BEFORE re-reading code

Both Angle A (taxonomy) and Angle B (Qud-parity-first) re-read existing
code looking for problems. That mode has a blind spot: **bugs hide
where the code looks right.** When the first audit pass declares "0
findings" but the feature is non-trivial, a third pass is warranted —
and the most effective methodology for it isn't more re-reading.
It's **hypothesis-driven RED-test authoring**.

**The methodology:** write the RED tests against hypothesized gaps
FIRST, then implement the minimum fix until each goes GREEN.

**The process:**

1. Step away from the code. Ask: *what does the player actually do
   that the per-phase tests don't simulate?* Generate hypotheses
   from player-flow, not from re-reading source. Examples that
   surface real bugs:
   - "What if the player applies a content-time mutation to an
     already-equipped item?" → Apply-to-already-equipped silent no-op
   - "What if a hook removes itself during dispatch?" → iterator skip
   - "What if a service is called with a negative reward?" → pin contract
   - "What if a stack has exactly count=1 at the boundary?" → pin boundary
2. Write **one RED test per hypothesis**. State the hypothesis in
   the test's docstring so future readers know what was being probed.
3. Run the tests. The ones that fail RED are confirmed bugs. The
   ones that go GREEN are **pinned-as-correct invariants** —
   regression infrastructure for free, since the test exists in
   case a future change breaks the contract.
4. Fix the RED bugs with the minimum change to flip each to GREEN.
   No speculative cleanup, no refactoring drift.
5. Commit with explicit per-test classification: "N confirmed bugs
   (RED→GREEN), M pinned-as-correct (already passes), K cross-system
   integration." The classification is honest about what each test
   does.

**Why this beats re-reading:**

- Re-reading suffers from confirmation bias — code that *looked*
  correct on first read tends to look correct on second read.
- Re-reading audits are structured around the code's organization
  (file by file, class by class). Bugs that span multiple files
  (cross-system) or live in the gap between two "correct" units
  (Apply attaches a Part / OnEquipped fires on equip — but Apply on
  an already-equipped item never re-fires OnEquipped) hide from
  per-file re-reads.
- RED-test authoring forces concrete scenario thinking. "What if
  the player does X then Y?" maps directly to a test setup. The
  re-read frame "is this code correct?" doesn't.
- Even when hypotheses turn out to be wrong, the pinned-correct
  test is permanent regression infrastructure — strictly more
  valuable than a re-read pass that finds nothing.

**Empirical evidence (Item Enhancements E.5.1):**

The E.4.3 audit ran BOTH angles and shipped 5 findings (1 latent
🟡 fixed, 1 🟡 hypothesis falsified, 3 minor cleanups). It
declared "0 real gameplay bugs." Honest at the time of writing.

Then the user prompted "do another run of code review." Instead of
re-reading, I generated 8 player-flow hypotheses and wrote 12 RED
tests. Result: **3 confirmed bugs (2 gameplay-visible silent
failures + 1 theoretical-but-real iterator bug), 4 pinned-as-
correct invariants, 1 cross-system integration check.** The
gameplay-visible silent failure was apply-Lacquered-to-currently-
worn-armor → AV bonus didn't land until re-equip cycle. That bug
existed for the entire shipped feature surface; both prior cold-eye
passes missed it because the per-phase mental model was "Apply is
content-time, OnEquipped is equip-time" — no overlap considered.
The hypothesis "what if the player applies during the equip-time
window?" surfaced it on the first try.

**When to run this pass:**

- After every multi-commit feature where the first audit pass
  declared "0 findings" or "no real bugs" — that's exactly the
  state where this pass is most valuable
- After every cold-eye that surfaced only `🔵` / `🧪` / `⚪`
  findings (i.e. no `🟡` or `🔴`) — the audit may have run a
  re-read pattern that the code's organization can defeat
- For any feature with cross-system flows (item ↔ inventory ↔
  combat ↔ NPC) where bugs live between systems

**Honesty bound:** this methodology is bounded by the hypotheses
the author can generate. It won't find a bug that nobody thinks to
hypothesize about. But it has a much higher ceiling than re-reading
— player-flow questions are easier to brainstorm than "what's
wrong in this 200-line file." Pair it with the fuzzing/property-
based testing pattern (out of scope of this guide) for the
truly-novel bug surface.

**Self-directive: when a cold-eye pass declares "0 real bugs" on a
non-trivial feature, I MUST run the hypothesis-driven deep audit
before declaring the feature complete. Generate 6-12 player-flow
hypotheses, write RED tests for each, classify the results
honestly. Even if all 12 turn GREEN, the regression pins are
permanent infrastructure.**

---

## Adversarial test sweep (MANDATORY for any feature with non-trivial state, parser, or cross-actor flows)

> **Full playbook: `ADVERSARIAL_TESTING.md` (project root).** That doc
> is the deep how-to: bug-class taxonomy, strategies for new vs
> existing features, code patterns, case studies, anti-patterns,
> reusability guidance for other projects. THIS section is the
> always-on trigger + checklist; open the playbook when you're
> actually designing a sweep.

**This is a separate gate from the per-sub-milestone "step g"** — that
step adds 1-3 mutation tests inline with the main suite (`Tests:` at
the bottom of `<Feature>Tests.cs`). The gate documented here adds a
**dedicated `<Feature>AdversarialTests.cs` file** with 20-60 tests
targeting bug classes the happy-path + counter-checks tests can't see.

The two are complementary, not redundant:
- **Step g (per-invariant):** "if I flip the precondition, does the
  test still fail?" — pairs with each positive assertion.
- **This gate (per-feature):** "what bug classes could exist that no
  per-invariant test would catch?" — driven by a taxonomy of surfaces,
  not by the spec.

Empirically:
- Skill-system audit: **80 adversarial tests** found 0 bugs but caught
  the **pre-WSP8.2 SkillRejected diag gap** (asymmetry between newer
  and older actives) during the symmetry sub-pass.
- Rental system: **43 adversarial tests** across two waves found 0
  bugs in main, but the suite's existence proves invariants like
  *RentalPart fields round-trip via reflection* + *CanBeTraded veto
  vs post-return clearance* + *cross-village blueprint matching* —
  contracts that were undocumented before the test pinned them.
- On-hit effects: **21 adversarial tests** — the prior-shipment
  comment in `OnHitEffectFactory.cs` explicitly credits adversarial
  tests with surfacing the Bleeding-`Magnitude`-vs-`DurationTurns`
  bug. **A real bug, found by this exact pattern.**

### When to do this gate

Run a dedicated adversarial sweep after a feature ships if **two or
more** apply:

- Feature touches **state atomicity** (ink + inventory transfer,
  rent + cooldown, multi-step transactions where partial failure
  needs rollback).
- Feature has a **parser** (string → spec, JSON-driven, comma/
  semicolon-delimited, blueprint-driven content).
- Feature has **cross-actor flows** (renter / lessor, attacker /
  defender, source / target, two-party state mutations).
- Feature has **stacking semantics** (status effects, counters,
  wallets, charge buffers — anywhere "X already has Y" matters).
- Feature has **save/load reach** (Parts that survive serialization,
  whether explicitly handled or via reflection fall-through).
- Feature has **anti-exploit gates** (sell-veto, can't-double-rent,
  faction-rep guards — anything where a clever player path could
  break the design intent).
- Feature has **probabilistic / RNG-gated behavior** (chance %,
  effect rolls — boundaries at 0% / 100% / negative).
- Feature has **diag emission contracts** (when does CommandRouted
  fire? When SkillRejected? Both? Neither? Order matters.).

### Bug-class taxonomy — surfaces to probe

Use this as a checklist when designing the file. Not every category
applies to every feature, but reading the list forces you to ask "is
this surface present?"

| Surface | Probe with |
|---|---|
| **State atomicity** | Force a partial-failure path (overweight inventory, missing currency); assert NO partial mutation |
| **Rollback paths** | Same setup but rollback target — verify the system returns to pre-attempt state |
| **Save/load reflection** | Round-trip the entity through `SaveGraphSerializer.SaveEntityBody`/`LoadEntityBody`; assert public fields preserved |
| **Mid-execution death** | Snapshot stability: target dies on swing/tick 1, verify the loop's remaining iterations behave correctly |
| **Cross-actor flows** | Two-party (renter ≠ lessor), three-party (drop-then-pickup-by-other), cross-instance (same blueprint, different ID) |
| **Parser malformed inputs** | Null, empty, whitespace-only, only-delimiters, missing required field, non-numeric where int expected, gibberish, mixed-valid-with-malformed |
| **Stacking semantics** | Re-apply already-present effect; verify NON-stacking returns true from `OnStack`; STACKING extends/accumulates correctly |
| **Boundary inputs** | Null actor / target / argument across every public API; assert no crash |
| **Conversation/dialogue actions** | `int.TryParse` failure paths, null listener, missing argument, non-existent blueprint name |
| **Anti-exploit invariants** | Try the exploit (re-rent, sell-rental, return-to-wrong-actor); assert the gate holds |
| **Probability boundaries** | chance=0 → never fires; chance=100 → always fires; chance<0 → filtered |
| **Self-referential gates** | actor == target, ArcaneSurge-style skip-self via Guid (NOT command-name), "item already in inventory" |
| **Cross-system aggregation** | Multiple modifier sources (skills × mutations) sum correctly via the dispatcher |
| **Effect-name normalization** | Case-insensitive (`BURNING` ≡ `burning`), aliases (`fire` → BurningEffect), whitespace trim |
| **Diag dispatch invariants** | Internal rejection emits BOTH `CommandRouted` AND `SkillRejected`; cooldown emits ONLY `CommandRejected`; non-matching events early-out |
| **Cooldown / re-fire** | Overwrite vs accumulate; second-cast-during-buff-window |
| **Duplicate add/remove** | AddSkill twice → false; RemoveSkill on unowned → false (idempotent) |
| **Multi-instance** | Two physical entities with same blueprint name; verify cross-instance equivalence |

### Pattern: a dedicated adversarial file

```
Assets/Tests/EditMode/.../<Feature>AdversarialTests.cs
```

Structure:
- Class `public class <Feature>AdversarialTests`
- Setup that resets relevant globals (`Diag.ResetAll()`,
  `MessageLog.Clear()`, `SkillRegistry.ResetForTests()`)
- Fixture helpers mirroring the per-feature test file
- Sections grouped by bug class, separated by `// ════════════════`
  comment banners
- Each test method prefixed `Adversarial_*` for easy
  group-name filtering
- Comments explain WHAT bug class is being probed and WHY a buggy
  impl would fail the test ("if a future change suppresses
  CommandRouted on internal bail, queries lose the dispatch trace")

Cumulative coverage measured per-system. The skill-system suite is
the canonical example: 394 baseline + 109 E2E + 37 + 10 deep
adversarial = 540+ tests across the surface.

### Process

1. Per-feature tests green ✓
2. Cold-eye review pass complete ✓
3. **STOP. Audit the bug-class taxonomy above.** Which surfaces does
   this feature touch? Two or more → run the gate.
4. Create `<Feature>AdversarialTests.cs`. Write 20-60 tests across
   the applicable categories. Group by category. Comment intent.
5. Run. Investigate every failure — failure means the test caught
   a real bug, NOT that the test is wrong.
6. Found a bug? File a `fix/<feature>-adversarial-finding` branch.
   Single self-contained fix-commit. Update the source comment to
   credit the adversarial test ("Latent bug surfaced by adversarial
   tests — see <FileName>"). Push. Merge.
7. Found nothing? Note "adversarial sweep complete, N tests, 0 bugs
   found" in the merge commit body. Document the surfaces probed
   so a future reader knows what's covered (and what isn't).

**Honesty bound: 0 bugs found in an adversarial sweep does NOT
prove the system is bug-free. Adversarial tests are bounded by the
bug classes the author imagines. To find truly-novel bug classes
you'd need fuzzing or property-based testing — out of scope of this
gate. The gate's value is (a) catching the rare bug it does find +
(b) creating regression targets so future changes break visibly.**

**Self-directive: I MUST run this gate for any feature where two or
more taxonomy surfaces apply, even when the per-feature tests are
green and the cold-eye pass found nothing. The OnHit Bleeding bug
proves the pattern catches real bugs the other gates miss; the
absence-of-bugs in skill + rental sweeps creates regression
infrastructure for future changes.**

---

## Deterministic self-auditing scenarios (use for any *measurable* mechanic)

> **Full playbook + case study: `Docs/MCP_PlayMode_Testing_Strategy.md`
> §Deterministic Self-Auditing Scenarios.** That doc is the deep
> how-to (the seven rules, the liquid-bench v1→v3.1 case study, the
> anti-patterns). THIS is the always-on trigger.

When a feature's behavior is **measurable** (damage numbers, stat/
resistance deltas, prices, durations, cooldowns — anything that fits
a table), the manual playtest scenario (workflow step 7) MUST be
*self-auditing*, not player-exercised. Manual aiming/RNG is an
unreliable measurement instrument **and it hides bugs** — the liquid
bench needed 3 failed harness designs before a deterministic matrix
caught a spawn bug the manual versions had hidden for three
iterations.

**The pattern:** the scenario's `Apply()` programmatically applies a
known stimulus to every subject (incl. a control), measures the
end-to-end delta, restores state (immortal, re-runnable rigs), and
emits one **machine-checkable diag record per cell**. One press of
Play → a complete `diag_query`-able audit. No aiming, no RNG, no
reading floating numbers.

The eight rules (detail in the playbook):

1. **Synthetic, not manual** — `Apply()` applies the stimulus
   directly, never the player.
2. **Snapshot → stimulate → measure → restore** — measure the whole
   pipeline end-to-end; restore so rigs are immortal.
3. **Always include a control row** — a baseline subject with no
   treatment.
4. **Guard preconditions LOUDLY** — verify each subject is actually
   set up; on failure emit an explicit `…Skipped`/`FAILED` record,
   **never a value that looks like a neutral pass** (the
   phantom-`×1.00` spawn bug is the cautionary tale).
5. **One machine-checkable diag record per cell** — the audit is a
   query, not log/screenshot scraping.
6. **Neutralize known confounds** before each measurement (strip
   interfering effects, freeze decay), and document each.
7. **Validate-before-merge** — the EditMode smoke only proves
   `Apply()` doesn't throw (no runtime registries there). The real
   proof is a live Play run + diag audit; keep the branch unmerged
   until the live matrix is confirmed correct. "Smoke green" ≠
   "mechanic works" (§6.3).
8. **Stamp every run; the diag buffer outlives the Play session.**
   With domain-reload-on-play off (CoO), `Diag`'s buffer persists
   across sessions — a reader deduping by (subject,case) shows STALE
   prior-run numbers. Emit a per-run `runId` + a `MatrixAuditRun`
   marker; scope the query to the newest run. When in doubt,
   cross-check the matrix against a direct `execute_code`
   measurement (bypasses the buffer). Caught for real in LX.3.

**Applies retroactively too** — elemental resistance grids, on-hit
procs, trade pricing, effect durations, etc. A self-auditing bench
turns "the unit tests probably cover this" into a one-launch
end-to-end query that also exercises bootstrap + content JSON + the
full runtime the unit tests stub out.

**Self-directive: when building a manual scenario for a measurable
mechanic, I MUST make it self-auditing per this section, not a
player-exercised demo — and I MUST hold the merge until a live diag
audit confirms the matrix, because the smoke test cannot.**

---

## Unity MCP workflow

**Standard post-edit cycle:**
1. `mcp__unity__refresh_unity` (compile request)
2. `mcp__unity__read_console types=[error]` — must be empty
3. `mcp__unity__run_tests mode=EditMode` — must pass
4. If timeout: `manage_editor stop`, retry
5. If new test count looks stale: `refresh_unity mode=force`, retry
6. Commit

**Common pitfalls (§7.2):**
- **Stale-assembly trap**: tests pass with the OLD code. Re-run after
  `refresh_unity`. The combat-port self-review hit this twice.
- **Stale-assembly trap (Play-mode variant)**: if `editor.is_focused: false`,
  Unity defers domain reloads even when `editor_state` reports
  `ready_for_tools: true`. Symptom: a Play-mode reflection probe shows
  a `CavesOfOoo.dll` build time older than the on-disk DLL, and new
  types/methods are missing. **Fix:** ask the user to focus the editor
  window or restart it; `refresh_unity {mode: force}` alone is not enough.
  Met during the Phase F/G/H methodology-debt closure (2026-04-26).
- **Active-instance routing can drop**: if multiple Unity Editor instances
  are connected to the same MCP server, `set_active_instance` may not
  persist for the whole session — a later call can route to a *different*
  project's editor. Symptom: tests "vanish" or `read_console` returns 0
  errors when there should be many, because the request hit a different
  Unity. **Fix:** re-confirm `editor_state.unity.instance_id` before
  relying on a result. Re-pin via `set_active_instance` if it drifted.
  Met during M2 of the storylet/quest layer plan (2026-04-26).
- **Test job init timeout** during recompile is normal — retry once
  after the editor settles (`sleep 3` if needed).
- `int.TryParse(arg, out duration)` writes 0 on failure AND returns
  false. Always pair with explicit sentinel guard.
- `Zone.GetReadOnlyEntities()` returns a live `Dictionary.KeyCollection`.
  Do NOT iterate while calling `ApplyEffect`/`AddEntity`/`RemoveEntity`.
  Use `zone.GetAllEntities()` instead (see Zone.cs:141).
- `execute_code` is **read-only state observation**. Firing gameplay
  events through it corrupts state — see `Docs/MCP_PlayMode_Testing_Strategy.md`.
- Entering Play mode resets the scene. **Warn the user before doing it.**

**Tool selection (§7.1):**

| Tool | Use for | Don't use for |
|---|---|---|
| `refresh_unity` | Compile after script edits | Stopping play mode |
| `read_console` | Errors after compile | Profiling |
| `run_tests` | Full EditMode/PlayMode suite | Single-test filter (often unreliable) |
| `manage_editor play/stop` | Enter/exit Play mode | Dismissing UI |
| `execute_code` | Read-only state observation | Firing events |
| `manage_input` | Keyboard playtest flows | Logic without a real key binding |

---

## When to delegate to a subagent (Task tool)

Use Task when the work is genuinely bounded and self-contained.
Don't use it for the actual feature work — delegating implementation
loses too much context.

| Task type | Subagent |
|---|---|
| Open-ended codebase questions | Explore (any thoroughness level) |
| Find symbol/file | Explore quick |
| Multi-file refactor with clear targets | general-purpose |
| Plan an implementation strategy | Plan |
| Code-review a diff for methodology compliance | general-purpose w/ explicit "review against §5 severity scale" prompt |

**Delegating self-review:** if you've been deep in implementation for
a while, spawn a general-purpose agent with the diff and the
`Methodology Template §5` checklist. Cross-check 3-4 of its claims
against source files before accepting the report (§5.4).

---

## Parity / reference-code work

When porting from `qud_decompiled_project/` (in-repo subset, 521
files, mostly visual) or `/Users/steven/qud-decompiled-project/`
(full 5368-file decompile, includes core game logic):

1. **Survey before claiming** — read each candidate file, compare
   signatures + mechanical behavior. (§4.1)
2. **Classify honestly** — Match / Extension / Divergent / CoO-original.
   Overclaimed parity is a bug. (§4.2)
3. **Document divergences** — per-phase divergence table in the
   living doc, NOT just the class docstring. (§4.3)

**False-premise detection patterns** (caught 3× in the combat port):
- "Qud has X" — verify by reading the actual file. Memory is unreliable.
- "Qud doesn't have X" — `grep -rn` to confirm. Easy to miss.
- "These helpers are missing" — check both instance and static surface.

---

## Reference docs (in this repo)

| Path | What it covers |
|---|---|
| `Docs/QUD-PARITY.md` §5162-6209 | Full Methodology Template (the canonical rules) |
| `Docs/COMBAT-QUD-PARITY-PORT.md` | 10-phase combat-system port — every phase fully documented per template |
| `Docs/COMBAT-PARITY-PORT-REVIEW.md` | Self-review findings doc + PlayMode sweep results |
| `Docs/COMBAT-AUDIT-PLAN.md` | Audit cadence pattern (Phases 0-4½ structure) |
| `Docs/COMBAT-BRANCH-MAP.md` | Branch coverage map (per-method ✅/⚠️/❌) |
| `Docs/COMBAT-TEST-BACKLOG.md` | Prioritized test entries with format `[#] (TARGET, SEVERITY, BUG_CLASS, PHASE)` |
| `ADVERSARIAL_TESTING.md` (root) | Adversarial testing methodology playbook — bug-class taxonomy, new-vs-existing strategies, code patterns, case studies. Read before designing any adversarial sweep. |
| `Docs/MCP_PlayMode_Testing_Strategy.md` | Live-bootstrap testing rules (Rule 1: never fire events via `execute_code`) + **Deterministic Self-Auditing Scenarios playbook** (the liquid-bench v1→v3.1 case study) |
| `Docs/PERF-FOUNDATION.md` | Optimization patterns, anti-patterns, audit findings (read before adding any feature touching per-frame paths) |
| `Docs/PERF-COMBAT-INVESTIGATION.md` | Original combat-perf audit (2026-04) — hypotheses + which were red herrings |
| `qud_decompiled_project/` | In-repo Qud subset (visual effects only) |
| `/Users/steven/qud-decompiled-project/` | Full Qud decompile (core game logic, 5368 files) |

---

## Project specifics (auto-memory)

- **🎮 Genre: RPG, NOT roguelike.** Character persists across save/load.
  Death is recoverable. Pacts/Brands/Marks carry full permanent narrative
  weight. World state persists. See `Docs/PROJECT-IDENTITY.md` for the
  authoritative statement. Anywhere a design appears to assume roguelike
  conventions (permadeath, run-based reset, meta-progression layer), the
  assumption is **wrong** — re-read with RPG framing.
- **🤖 Claude does ALL the coding.** The user (steven) is not hand-coding
  any of this game. Claude writes the production code, the tests, the
  docs. Time estimates that read like "5-7 days of focused work" should
  be re-read as agent-pace, not human-pace — typically 3-4x faster on
  pure coding hours, modulated by Unity reload cycles + user-review
  pauses between phases. **Do not estimate timelines as if a human is
  typing.** Estimate by agent-work-hours and surface user-review
  checkpoints separately.
- **Game scene:** `Assets/Scenes/SampleScene.unity` (NOT `Assets/game.unity`)
- **Unity 6** (6000.3.4f1) at `/Users/steven/caves-of-ooo/`
- **Architecture mirrors Qud:** Entity (bag of Parts) + GameEvent (string-keyed)
- **CP437 tilemap** at runtime (80×25), Sprite-Lit-Default + Global Light 2D
- **EditMode tests:** 2181+ NUnit tests, ~30s full suite

---

## Observability — every gate emits a diag record

Before writing the production code for a new feature, decide which
diag records it will emit. Don't punt this — the substrate
(`Diag.cs`) and the MCP tools (`diag_query` / `diag_count` /
`diag_assert` / `diag_inspect_record`) exist precisely so future
"why didn't X happen?" debugging starts with a query, not with code
grep + `execute_code` poking. If the records aren't there, the
debug session degrades to detective work — which is what the
WSP3-7 skill-system buildout did because the dispatch path was never
instrumented.

**The rule:** every action that can succeed OR fail emits a
category-appropriate diag record on each branch. Specifically:

1. **Every gate that can reject emits a record.** Cooldown checks,
   weapon-class checks, line-of-sight checks, target-type checks,
   resource-cost checks, etc. — each gets a `kind=Rejected` (or
   similar) record with a `reason` field naming which gate fired.
2. **Every successful action emits a record.** `kind=Routed` /
   `kind=Casted` / `kind=Activated` etc. The record is the proof
   the system worked end-to-end.
3. **Use the standard categories.** `event`, `effect`, `damage`,
   `turn`, `furniture`, `trade`, `quest`, `skill`, `ai` — listed in
   `Diag.DefaultOnCategories`. Add new ones only when none fit.
4. **Payload includes everything a debugger would want.** For a
   skill: command name, skill class, display name, cooldown
   remaining, reason for rejection. For damage: amount, attributes,
   resistance reduction. For movement: from/to cells, blocked
   reason if applicable. The payload is read by `diag_query`'s
   PayloadJson — make it grep-friendly.

**The verification workflow:**

When a user reports a bug ("X doesn't work"), step 1 is:
```
diag_assert category=<area> kind=<expected_success_kind>
```
- `matched=false` → the success path never fires → investigate the
  upstream pipeline (input dispatch, condition gates, etc.)
- `matched=true` → the success path fires → investigate downstream
  (effect resolution, target state, side-effect propagation)

**The pattern that surfaced this rule (skill-system bug, 2026-05):**
`SkillsPart.TryRouteSkillCommand` emitted `Added`/`Removed` records
but had ZERO instrumentation on the activation pipeline. When the
user reported "Slam doesn't hit," the right first query was
`diag_assert category=skill kind=CommandRouted` → which would have
returned `matched=false` and immediately pointed at the dispatch
gap. Instead the diag stream was unhelpful (only Add/Remove
records), so the debug session burned tool calls grepping +
execute-coding. The fix added `CommandRouted` and `CommandRejected`
emissions; future bugs in the activation pipeline now surface as
expected.

**Tests pin the emissions.** Like other contract-level invariants,
diag emissions need test coverage so a future contributor can't
silently drop them. Pattern: in the unit test for the action,
`Diag.ResetAll()` then exercise the action then `DiagQuery.Apply`
to assert the expected record fired. See
`SkillsPartTests.HandleEvent_SuccessfulRoute_EmitsCommandRoutedDiag`
for the canonical example.

---

## Performance — non-negotiables for new features

Read `Docs/PERF-FOUNDATION.md` before adding any feature that touches
the per-frame or per-turn paths. The strategies + audit history live
there; the rules below are the always-on subset.

1. **Profile before optimizing, profile after writing.** Use
   `Unity.Profiling.ProfilerRecorder` over real gameplay (60-90s
   window) — not isolated 5-call tests. Sort by **max**, not avg,
   to catch spikes. The 70,000× perf bug we shipped a fix for in
   2026-04-28 was invisible to isolated tests; only `[Perf] spike`
   logs caught it.

2. **Cache misses must be cheap.** Any `Dictionary<,>` lookup
   followed by "compute and insert" must be cheap to miss — or
   gated behind explicit invalidation, never per-call. Pre-populate
   the cache for the full input domain or just return a fallback
   on miss.

3. **No allocations in hot paths.** `LateUpdate`, `Update`, turn
   loops, per-cell render, per-NPC AI scan — none of these may
   `new List<>` / `new Dictionary<>` / use LINQ. Use the scratch-list
   pattern in `Docs/PERF-FOUNDATION.md §Pattern 1`.

4. **Per-cell dirty hooks for visible changes.** If gameplay code
   changes a cell's visible state (entity moved, color flash,
   status applied), call `ZoneRenderHooks.MarkCellDirty(x, y, source)`.
   Do not call the full-zone `MarkDirty` unless FOV / lightmap
   actually needs recompute (player moved, light source moved).

5. **Renderers gate work behind content fingerprint.** Sidebar,
   hotbar, and any new UI panel must skip work when the snapshot
   it would render is identical to the last frame's. See
   `SidebarRenderer.ComputeSnapshotFingerprint` for the pattern.

6. **`Application.runInBackground = true` for development.** This
   is set in `ProjectSettings.asset` (`runInBackground: 1`). Without
   it, the editor throttles to 10fps when the window loses focus —
   indistinguishable from "the game is laggy" in a profiler. If
   you ever see `cpu_frame_time = 100ms` with `cpu_main_thread = 0ms`,
   this setting reverted.

**Performance section required in feature plans.** When two or more
of these apply, the feature's `Docs/<feature>.md` plan needs a
**Performance** section citing which patterns it'll use:
- Plumbs `ZoneRenderHooks` from gameplay
- Allocates collections inside per-frame / per-turn methods
- Adds a new cache (`Dictionary<,>` keyed by gameplay state)
- Adds a new MonoBehaviour with `Update` / `LateUpdate`
- Adds a new event listener that fires per-frame / per-turn

---

## Final reminders

- **Don't barrel into work after a false-premise correction.** Stop,
  surface the correction, get user confirmation, then proceed.
- **The audit cadence (gap-coverage → adversarial cold-eye) is what
  catches latent bugs.** Empirical: 0% on M-style code (already
  TDD'd), 12.5% on legacy code. See §3.9.
- **GC pressure is acceptable in turn-based combat.** Don't reach
  for event-pool optimizations unless a profiler shows a problem.
- **You will be tempted to skip steps when the work feels small.**
  The combat port's three false premises were "small" features.
  Run the sweep anyway.
