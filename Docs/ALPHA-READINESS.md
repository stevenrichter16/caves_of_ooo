# Alpha Readiness — Analysis & Implementation Plan

> Living plan + implementation log. Origin: user directive 2026-08-06 —
> "analyze the game's mechanics thoroughly so you understand what needs
> to be amended so the game is in a good alpha state. plan the fixes,
> improvements, enhances, for those issues. then implement those plans
> fully without my intervention."
>
> Produced by a 14-agent analysis workflow (8 system lenses ->
> synthesis -> adversarial verification of every P0). 67 raw gaps ->
> 13 plan items. All 5 externally-verified P0s confirmed REAL and
> honestly scoped; verifier corrections are folded into the
> sub-milestones below and marked [CORRECTED].

**Status:** 🚧 IN PROGRESS — implementing P0 set in order.

## Alpha verdict

The engine layer is well past alpha quality — combat resolution, AI goal stacks, the save format, trade pricing, and the quest/storylet engine are production-grade and heavily tested — but the game wrapped around them currently cannot be lost, cannot be learned, and cannot be resumed: a 500 HP player faces factionless 1d2-fist monsters with zero onboarding, saves are orphaned to per-boot GUID directories on every restart, and the opening minutes are a visible dev sandbox (free spell mutations, debug weapons, barrel demos, a live F8 self-dismember key). The shape of the gap is striking: it is almost entirely JSON content tuning and 1-to-30-line wiring fixes that connect already-shipped, already-tested machinery (followers, house dramas, ~190 nodes of lore dialogue, tier-3 loot, DV effects, the MP economy) to actual play — very little new system code is needed. Closing the seven P0 items restores the complete play-fail-recover-persist loop that is the alpha bar; the P1 tier then makes the persistent world worth returning to past hour three. At agent pace the P0 set is days of work, not weeks, because most sub-milestones are data edits with existing test patterns to copy.

## Top blockers (deduped)

- Player has 500 HP vs monsters that mostly punch for 1d2 — combat is unlosable, so death recovery, healing, tonics, +2 HP/level, and enemy tiers are all inert (independently flagged by 4 of 8 analyses; Objects.json:247, verified)
- Saves are written to a fresh per-boot GUID directory that nothing can rediscover after an app restart (GameBootstrap.cs:40, SetActiveGameID has zero production callers — verified; 10 orphaned save dirs on disk), and no autosave exists — a first death without a manual F5 wipes the entire session, directly violating the RPG persistence identity
- ~17 creatures including 3 of 4 lair bosses and the starting-area IceWights have no Faction tag and are permanently neutral (verified: CaveBat/CaveBear/IceWight/DesertProwler/AncientGuardian carry no Faction tag) — a third of the bestiary never attacks, boss fights never start, and bump-to-attack is gated on IsHostile so the player can barely fight them either
- Zero onboarding: no help screen, no controls text, no boot message, no goal — every core system (inventory I, talk C, skills X, quests Q, save F5) is locked behind an unguessable key and the game opens in total silence
- The new-game start is a debug showcase: 8 free attack mutations, free weapons and a stocked merchant at spawn, full tinkering unlocks, barrel-demo furniture in every village, and live P/F7/F8/F9 debug keys — F8 permanently dismembers the player's limb with no confirmation
- House Vex drama NPCs are completely mute in ~half of all villages (Drama_Vex_* conversation content was never authored and the ConversationID override has no existence guard) — four NPCs per affected village, including a broken merchant, silently do nothing
- The economy dead-ends: crafting reagents/components have no renewable source (finite one-time starter kit), traders never restock items or drams, corpses and kills yield nothing sellable, and the grimoire chest gives away the game's priciest items free in every village
- The reward loop is hollow past hour two: difficulty plateaus at tier 2 across ~60% of the map, the infinite underground is snapjaws at every depth, lair bosses drop the same items village traders sell, and tier-3 gear plus ~20 exotic weapons/schematics have no spawn path
- A large body of finished, tested content is unreachable by pure wiring omission: the entire follower system (no Persuasion.json — verified absent), ~190 nodes of the deepest dialogue on never-spawned faction NPCs, the whole MP/mutation-advancement economy (no MP stat on the Player), and the Acrobatics tree (100 SP vs +1 SP/level)
- Five status effects and the Acrobatics Dodge power write a DV stat that CombatSystem.GetDV never reads — stun/confuse/hobble/paralyze are placebos against hit chance, and the sidebar always shows DV 0 regardless of armor

## Plan

Implementation order (P0, smallest-blast-radius / dependency order):
combat-stakes -> hostile-bestiary -> save-lifeline -> strip-debug ->
onboarding -> narrative-feedback -> economy-renewables. P1/P2 follow.

| # | Item | Priority | Status |
|---|---|---|---|
| 1 | combat-stakes: Mortal player, real monster damage, coherent XP | P0 | 📋 |
| 2 | hostile-bestiary: Wildlife and lair bosses actually attack | P0 | 📋 |
| 3 | save-lifeline: Saves that survive restarts + autosave | P0 | 📋 |
| 4 | strip-debug: Designed opening instead of dev sandbox | P0 | 📋 |
| 5 | onboarding: First-five-minutes discoverability | P0 | 📋 |
| 6 | narrative-feedback: Mute-NPC guard + quest lifecycle feedback | P0 | 📋 |
| 7 | economy-renewables: Un-dead-end the economy | P0 | 📋 |
| 8 | frontier-rewards: Difficulty and loot past tier 2 | P1 | 📋 |
| 9 | dv-wiring: Make DV effects real | P1 | 📋 |
| 10 | skill-mp-economy: Skill pricing, SP discoverability, and the dead MP axis | P1 | 📋 |
| 11 | followers-live: Followers reachable and combat-capable | P1 | 📋 |
| 12 | input-correctness: Input edge-case fixes | P1 | 📋 |
| 13 | dormant-content: Spawn the authored-but-unreachable content | P1 | 📋 |
| 14 | persistence-polish: World-consistency polish | P2 | 📋 |


### 1. [P0] combat-stakes — Mortal player, real monster damage, coherent XP

**Systems:** combat-effects, progression, content-json

Retune the dev-placeholder numbers so combat has stakes and the danger/reward curve matches the tier system: 500→~40 player HP, natural weapons for the ~20 bruisers stuck on the 1d2 default fist, tier-scaled XP values, and venom for the venomous. Almost entirely JSON plus ~10 factory switch cases riding proven patterns (SnapjawClaw, the existing Poisoned on-hit spec parser).

**Sub-milestones:**
- Set Player Hitpoints 500→~40 in Objects.json:247 (keeps 500 only in test fixtures); test pins the blueprint value so it can't silently regress
- Add XPValue stats scaled by Tier tag to the ~16 creatures currently inheriting the base 10 (CaveBear, StoneGolem, SandWurm, SnapjawChieftain, etc.) — test: StoneGolem XP > SnapjawHunter XP, fixing the risk/reward inversion
- Add NaturalWeapon props + NaturalWeaponFactory entries for the bruisers (CaveBearClaw 2d4, GolemFist 2d6, WurmBite 2d6+1, ViperBite 1d3, etc., ~20 JSON props + ~10 factory cases mirroring SnapjawClaw) — one damage-die test per factory entry
- Give Viper/Scorpion/GiantSpider poison via OnHitEffectsRaw on their new natural weapons using the shipped 'Poisoned,50,1d4,6,0' spec format; counter-check: a non-venomous claw never applies Poisoned
- PlayMode lethality sanity sweep of the starting neighborhood at level 1 (honesty-bounded report); retune HP/tonic numbers only if the sweep shows unwinnable or still-trivial fights

**Verification:** CONFIRMED (isReal=True, scope_ok=True).

**[CORRECTED] Verifier corrections to honor during implementation:**
1) Evidence overstates existing fist overrides: Glowmaw (2d4), SleepingTroll (2d6), MimicChest (1d8), AmbushBandit (1d6) carry entity-level MeleeWeapon parts that are DEAD CODE — every creature inherits a Body part (Objects.json:114), so CombatSystem.MeleeAttack always takes PerformBodyPartAwareAttack (CombatSystem.cs:113-118), and GatherMeleeWeapons (CombatSystem.cs:570-614) reads only Hand._Equipped/_DefaultBehavior, never attacker.GetPart<MeleeWeaponPart>() (that fallback lives only in PerformLegacyAttack, CombatSystem.cs:183, which requires no Body). These four ALSO punch 1d2; the real strongest natural melee today is SnapjawHunterClaw 1d6, not "SleepingTroll 2d6". Milestone 3 should add these 4 creatures (convert their dead MeleeWeapon parts to NaturalWeapon props, or add an entity-level fallback in GatherMeleeWeapons) plus a liveness test pinning that their blueprint damage is actually used. 2) SnapjawChieftain is NOT stuck on the 1d2 fist — Props are inherited (BlueprintLoader.cs:206-210), so it and SnapjawScavenger already get SnapjawClaw 1d4/pen1 from Snapjaw; drop them from the fist list (an upgrade is optional flavor, not gap-closure). 3) XPValue base-10 count is ~22 combat-capable creatures when inheritance-resolved, not ~16 — add ChoirTendril (80 HP / 10 XP), Warden (40 HP / 10 XP), PalimpsestEcho, SaccharineEnvoy, PaleCurator, GlassblownDrifter to the sweep list. 4) Milestone 4 is not purely JSON+switch-cases: NaturalWeaponFactory.CreateWeapon (NaturalWeaponFactory.cs:33-53) has no OnHitEffectsRaw parameter, so venom requires extending that signature and setting the field on the created MeleeWeaponPart (trivial, but name it in the milestone). 5) Coupling risk to document: EntityFactory.InitializeAnatomy applies the NaturalWeapon prop only to "Hand"-type body parts (EntityFactory.cs:493-501); this works today because all creatures default to Humanoid anatomy, but the separate P2 "Anatomy props" fix (Quadruped/Simple/Insectoid) would make NaturalWeapon silently no-op on handless anatomies — the two fixes must land with an integration test if both ship. 6) Minor: milestone 1's fixture caveat verified accurate — no test asserts the blueprint Player's 500 HP (test 500s are hand-built entities, e.g. GasShowcaseProliferationTests.cs:72), so the blueprint change should not break the suite.

### 2. [P0] hostile-bestiary — Wildlife and lair bosses actually attack

**Systems:** ai-npc, content-json, factions

Add the missing Faction tags so the ~17 permanently-neutral creatures — including 3 of 4 lair bosses and the starting-area IceWights — become hostile. Data-only fix that restores the fight half of the core loop for a third of the bestiary; the AI engine (KillGoal, BoredGoal hostile scan, bump-attack) already works.

**Sub-milestones:**
- Add a 'Beasts' faction (InitialPlayerReputation -100, hostile to Villagers) to Content/Data/Factions.json and tag the 12 wildlife blueprints (CaveBat, CaveSlime, CaveBear, Glowmaw, Scorpion, SandWurm, GiantSpider, Viper, JungleApe, RuinScavenger, SkeletalSentry, StoneGolem) — RED test: FactionManager.IsHostile(caveBat, player) flips false→true
- Give the 3 lair bosses (DesertProwler, JungleStalker, AncientGuardian) and IceWight/CharredHusk appropriately hostile factions — one hostility test per blueprint
- Belt-and-braces: GlowmawAmbushPart calls brain.SetPersonallyHostile(player) alongside the existing Target assignment (GlowmawAmbushPart.cs:104-106) so the ambush commitment survives any faction math; counter-check: no hostility before the drop triggers
- PlayMode sweep of a tier-1 cave: verify bats/slimes/glowmaw acquire KillGoal and that bump-to-attack now works on them (it is gated on IsHostile at InputHandler.cs:669-675)

**Verification:** CONFIRMED (isReal=True, scope_ok=True).

**[CORRECTED] Verifier corrections to honor during implementation:**
Five corrections, none blocking. (1) SM3's counter-check 'no hostility before the drop triggers' must be phrased as no PERSONAL hostility (BrainPart.IsPersonallyHostileTo(player) == false pre-drop): after SM1 lands, faction-level FactionManager.IsHostile(glowmaw, player) is already true BEFORE the drop (faction hostility is not drop-gated), so a counter-check asserting IsHostile==false pre-drop fails for the wrong reason. (2) CharredHusk is NOT player-reachable in normal play — it is spawned only by dev scenarios (Scenarios/Custom/EmberSpearShowcase.cs:61, ElementalCreatureZoo.cs); including it in SM2 is harmless future-proofing but should not be counted toward the player-facing P0 claim (IceWight IS reachable). (3) Doc drift: Docs/CRYOLANCE-ICEWIGHT.md's self-review claims IceWight 'uses unknown Faction:Wights', but the shipped blueprint (Objects.json:4190) has NO Faction tag at all — SM2 should update that doc line in the same commit. (4) Directional feelings: GetFactionFeeling looks up only the source faction's dict (FactionManager.cs:162-166), so 'hostile to Villagers' needs BOTH the Beasts→Villagers entry and the reciprocal Villagers→Beasts entry (mirror the existing Snapjaws/Villagers pair) or villagers/wardens will not defend against beasts. (5) Flavor nit: InitialPlayerReputation -100 yields Attitude.Disliked (feeling -50), not Hated (needs ≤ -150 per PlayerReputation.cs:14-15,102-103) — hostile either way, but if 'wildlife is irredeemably hostile' flavor is wanted, use ≤ -150. Also worth observing in SM4: bump-attacking a still-lurking invisible Glowmaw becomes possible post-fix (walking into its solid invisible body now triggers melee instead of a plain block) — likely fine, but verify it does not reveal/kill the ambusher awkwardly.

### 3. [P0] save-lifeline — Saves that survive restarts + autosave

**Systems:** lifecycle-saveload, bootstrap

Un-orphan the per-boot GUID save directories and add an autosave so death is actually recoverable — the two P0s that silently break the RPG persistence promise in any standalone build. All machinery (atomic save, boot menu, death screen) already exists; this is discovery + trigger wiring.

**Sub-milestones:**
- Report death-screen load failure: consume QuickLoad's bool in DeathScreenController.Tick (DeathScreenController.cs:77) and log 'Load failed — save may be corrupted. [R] to restart.' — unit test with a failing loader stub
- Clear MessageLog at the top of GameBootstrap.DoStart so a restarted run doesn't show the previous life's 'You are dead' spam — test asserts empty log after reset
- Boot-time save discovery: scan Saves/*/Quick.json for the newest SaveTimestampUtc and call SaveGameService.SetActiveGameID before TryActivateBootMenu (GameBootstrap.cs:587); also persist the last gameID via PlayerPrefs on every save as a fast path — test with fabricated save dirs in a temp root picks the newest
- Autosave: one SaveGameService.QuickSave() call in InputHandler.HandleZoneTransition plus an 'Autosaved.' log line; counter-check that a failed/blocked transition does not save
- Death-screen fallback to the autosave slot when no manual save exists; PlayMode sanity: die with no F5 ever pressed, press [L], verify the session restores (honesty-bounded report)

**Verification:** CONFIRMED (isReal=True, scope_ok=True).

**[CORRECTED] Verifier corrections to honor during implementation:**
1) SM1 as scoped will NOT surface real corruption: LoadSlot (SaveSystem.cs:470-484) has no try/catch and ValidateGzipHeader (SaveSystem.cs:598-604) throws InvalidDataException, and the production SaveGameServiceAdapter (SaveLoadInputAdapters.cs:25) doesn't catch — so a genuinely corrupted Quick.sav.gz is an unhandled exception inside InputHandler.Update, not a false return. Same is true of the existing "Load failed — save may be corrupted" line at SaveLoadInputController.cs:78, which is currently unreachable for real corruption. SM1 must add a try/catch (in the adapter or LoadSlot) for its stated message to fire in production; the failing-loader-stub unit test alone would green-light a fix that never triggers in reality. 2) SM5's "fallback to the autosave slot when no manual save exists" is vacuous as written: SM4's autosave calls QuickSave(), which writes the SAME Quick slot as manual F5 (SaveSystem.cs:405-406,426), so the death screen's existing HasQuickSave check sees the autosave automatically — there is no fallback mechanism to build. Rephrase SM5 as pure PlayMode verification (which is worthwhile), or if a separate Auto slot is intended, SM4 and SM5 contradict and slot policy must be decided first. 3) BootMenuController.Tick also discards QuickLoad's bool (BootMenuController.cs, Continue branch) — same bug class as SM1; cheap to fix in the same commit for symmetry (CLAUDE.md Q1 check). 4) SM3 testability note: save paths are hardwired to Application.persistentDataPath (SaveSystem.cs:512-519), so the "fabricated save dirs in a temp root" test requires the discovery scanner to accept an injectable root dir (pure function) — fine, but plan the seam; also the scanner should tolerate an unreadable/corrupt Quick.json per-directory rather than aborting discovery. 5) Minor residual gap worth stating in the doc: the plan pruned the evidence's "every ~100 turns" autosave, so a player who dies without ever leaving the starting zone still has no autosave — acceptable alpha prune (zone transitions are frequent, roadmap sanctions one-slot), but it should be recorded as a documented divergence, especially since the companion P1 (lowering 500 HP) makes starting-zone death plausible. 6) SM4 placement detail: fire QuickSave at the END of HandleZoneTransition (after CurrentZone/ZoneManager.SetActiveZone rewiring at InputHandler.cs:777-779) so the captured GameSessionState records the destination zone as active; also note HandleZoneTransition is private on a MonoBehaviour, so the counter-check assertion will need a PlayMode test or a testable seam rather than a direct unit call.

### 4. [P0] strip-debug — Designed opening instead of dev sandbox

**Systems:** bootstrap, ux-input, world-content

Gate every debug artifact that ships in normal play — showcase spell grants, free weapons/merchant at spawn, full tinkering unlocks, barrel-demo furniture in every village, and the live P/F7/F8/F9 debug keys (F8 permanently dismembers the player) — behind a single default-off flag, and replace the start with a small designed loadout.

**Sub-milestones:**
- Gate the P/F7/F8/F9 debug key branches (InputHandler.cs:612-633) behind #if UNITY_EDITOR or a default-off EnableDebugKeys flag — test: flag off → F8 is a no-op; fix the stale 'F6 grant mutation' docstring while there
- One debug flag gating GrantShowcaseSpellMutations, SpawnDebugWeaponNearPlayer, SpawnDebugNPCNearPlayer, and the all-recipes/all-bits tinkering grant (GameBootstrap.cs:255-272, 860-864); counter-check: flag on still produces the full showcase for dev scenarios
- Define the designed starter loadout in its place (dagger, a few tonics, seeds, FlamingHands+Calm as the intended kit) — test asserts the fresh-start inventory exactly
- Remove the unconditional PlaceBarrelLayouts call (keep at most one layout, starting-village-only, as a fire tutorial) and gate PlaceDebugMaterialSandbox (VillagePopulationBuilder.cs:79-87)

**Verification:** CONFIRMED (isReal=True, scope_ok=True).

**[CORRECTED] Verifier corrections to honor during implementation:**
1) SM1 understates the F6 situation: the F6 debug branch (InputHandler.cs:596-601, TryDebugGrantRandomMutation) is dead code (always shadowed by SaveLoadInputController QuickLoad) — delete/gate the branch, not just the docstring; also fix the second stale docstring in TryDebugCycleWellState (1132-1136) which says 'F10' but the binding is P. 2) SM1's cited range 612-633 omits F7 (actually 604-609) — trivial. 3) SM3 must explicitly decide the fate of GivePlayerCraftingStarterKit (GameBootstrap.cs:267 → CraftingStarterKit.cs: 13 reagents + 6 weapon components ×2 = 19 stacks) and the 8-tonic one-of-each grant (GameBootstrap.cs:42-52) — the 'asserts fresh-start inventory exactly' test forces this decision anyway, so name it in the plan. 4) Positive scope note: SM3's proposed kit needs no new content — FlamingHandsMutation.cs, CalmMutation.cs, Dagger, tonics, and seeds all exist.

### 5. [P0] onboarding — First-five-minutes discoverability

**Systems:** ux-input, lifecycle-saveload, quests-narrative

The game opens in total silence with ~25 unguessable keybindings. Add boot text, a help popup, and an Esc-reachable menu so a new player can learn the controls and find the first quest without reading source. Pure MessageLog calls plus one popup reusing the existing centered-popup idiom.

**Sub-milestones:**
- Boot MessageLog summary at end of DoStart: 2-3 lines covering move/I/C/G/L/X/M/Q/Tab/F5 plus a call-to-adventure line ('Villagers glowing gold have work for you — press Q for your quest log.') — test pins the lines fire once per start
- F1 and '?' help popup listing all bindings, driven from a single binding table (one source of truth for a future rebind UI), reusing the AttackConfirmation centered-popup drawing idiom
- Alias Escape to open/close the pause menu in the Normal input state, and add Controls (opens help popup) + Quit entries to PauseMenuController's item list
- Render the boot Continue/New-Game prompt and the death screen as real centered popups instead of single sidebar log lines while all input is suppressed (BootMenuController.cs:44, DeathScreenController.cs:54) — the current invisible modals read as a frozen game

**Verification:** CONFIRMED (isReal=True, scope_ok=True).

**[CORRECTED] Verifier corrections to honor during implementation:**
1. DeathScreenController prompt is at line 53, not 54 (BootMenuController.cs:44 is exact). 2. 'Opens in total silence' is marginally overstated: SidebarStateBuilder.cs:232 renders MessageLog lines, so the boot-menu prompt IS visible as one sidebar log line when a save exists; with no save there is genuinely zero text. 3. SM2's 'single binding table (one source of truth for a future rebind UI)' overpromises: actual dispatch reads ~25 scattered hard-coded KeyCode checks in InputHandler plus public KeyCode fields on 4 controllers (PauseMenu/BootMenu/DeathScreen/SaveLoadInput); as scoped the table is display-only and can drift — fine for the help popup, but a rebind UI later requires a dispatch refactor not included here. 4. SM3 should additionally make Esc close the pause menu while open (currently only Tab closes) and update PauseMenuController.cs:13-18's doc-comment, which records the deliberate decision NOT to use Esc (the Esc-closes-active-modal convention); Esc-in-Normal is compatible with that convention but the doc must be revised or it becomes doc-vs-impl drift. Adding items also touches ItemCount/ClickSelect/HoverSelect/PauseMenuUI rendering + tests — still small. 5. SM4 sequencing dependency: the boot menu only activates when SaveGameService.HasQuickSave() is true (GameBootstrap.cs:587), and the sibling P0 (per-boot GUID save dirs) makes that effectively always false in standalone builds — the boot-popup half of SM4 delivers near-zero player value until the save-discovery fix lands; sequence it after. 6. Content note for SM2's binding table: F6 is double-bound (quick-load in SaveLoadInputController.cs:46 vs mutate-debug at InputHandler.cs:596, save/load controller consumes first per InputHandler.cs:343-349) — the help popup should list F6 as quick-load only, and the debug F-keys (F6-F9, P) should be listed as debug or omitted.

### 6. [P0] narrative-feedback — Mute-NPC guard + quest lifecycle feedback

**Systems:** quests-narrative, ux-input

In ~half of all villages, four House Vex drama NPCs are completely mute (their conversation JSON was never authored), and quest accept/objective/complete produce zero on-screen feedback. Guard the missing-content case in one line, add a nothing-to-say fallback, and add ~20 lines of quest lifecycle messages; author the Vex conversations last.

**Sub-milestones:**
- HouseDramaZoneBuilder.cs:94-95: only override ConversationID when ConversationLoader.Get returns non-null, so Vex NPCs instantly fall back to their blueprint's working dialogue (and the Merchant becomes tradeable again) — test: unknown drama ID keeps the blueprint conversation
- InputHandler.cs:3243: replace the silent 'if (!started) return;' with MessageLog.Add("{name} has nothing to say."); counter-check: a successful conversation start prints nothing
- Quest lifecycle MessageLog lines in StoryletPart — 'Quest accepted: X' (StartQuest), 'Objective complete: <text>' (FinishObjective), 'Quest complete/failed: X' — one test pinning each emission, display text resolved from StoryletRegistry
- Author Drama_Vex_* conversations for the 4 roles mirroring HouseThresker.json's structure (content work, largest blast radius, ships last and independently)

**Verification:** PLAUSIBLE (not externally verified — verify evidence inline before each sub-milestone).

### 7. [P0] economy-renewables — Un-dead-end the economy

**Systems:** economy-items, trade, content-json

Crafting inputs are a finite one-time kit, traders never restock, kills pay nothing, and every village chest gives away the game's priciest grimoires — a persistent character hits hard walls within hours. Close the renewability gaps using the exact SM7d seed-fix pattern already in TradeStockBuilder.

**Sub-milestones:**
- Append the 13 reagent + 6 weapon-component blueprints to TradeStockBuilder's goods arrays (TradeStockBuilder.cs:18-47, mirroring the SM7d seed fix in the same file) — test: village trader stock contains reagents; brew/forge loops become renewable
- JSON-only kill payoffs: Commerce Value 2-3 on CreatureCorpse/SnapjawCorpse (sell panel filters on CommercePart), 1-2 inventory rolls on MimicChest so the ambush drops loot — test: corpse appears in the sell panel
- Trader restock: on zone revisit after N turns, top trader drams back to blueprint value and re-roll 1-2 stock items (~30 lines); counter-check: no restock before the N-turn threshold
- Trim PlaceGrimoireChest to 2-3 starter grimoires; distribute the rest across Merchant stock and the lair rare pool (prices already authored) — closes the ~1,000-dram-per-village faucet
- Add an Ego stat (Value 16) to the Player blueprint — zero behavior change today, unlocks the shipped Qud-exact pricing lever; plus Food parts on CandyCarrot/Emberwheat and YieldCount 2-3 so farming output is edible and profitable

**Verification:** PLAUSIBLE (not externally verified — verify evidence inline before each sub-milestone).

### 8. [P1] frontier-rewards — Difficulty and loot past tier 2

**Systems:** world-content, economy-items, combat-effects

Exploration flatlines once the player handles tier-2 spawns: no Tier3 tables exist, the infinite underground is snapjaws at every depth, lair bosses drop shop-tier loot, and tier-3 gear plus ~20 exotic items/schematics have no spawn path. All table/JSON edits using creatures and items that already exist.

**Sub-milestones:**
- Boss signature drops: guaranteed Inventory item on each of the 4 lair boss blueprints (mirroring the Crossroads signature-loot pattern) — test: boss death drops its signature
- Weighted rare tier in LairPopulationBuilder's lootPool: tier-3 gear (Claymore, PlateArmor), exotic weapons, advanced tonics, and the 3 schematics — the SchematicPart study mechanic finally becomes reachable
- Tier3 population tables in PopulationTable.GetBiomeTable (currently every tier>=2 maps to Tier2) and have LairPopulationBuilder scale guard counts/loot by _poi.Tier — test: dist>8 zone spawns from the Tier3 table
- Underground depth bands in UndergroundTier: CaveBear/CaveSlime at depth 3+, SkeletalSentry/StoneGolem at 6+, extended depth-gated loot — table-only edit giving the vertical axis a reason to exist
- MerchantCamp pipeline branch in OverworldZoneManager.GetPipelineForZone: biome terrain + campfire set piece + 2 Merchant NPCs + TradeStockBuilder, so the 2-3 '$' world-map markers stop lying

**Verification:** PLAUSIBLE (not externally verified — verify evidence inline before each sub-milestone).

### 9. [P1] dv-wiring — Make DV effects real

**Systems:** combat-effects, ux-input

Five status effects (Stunned, Confused, Hobbled, Paralyzed, Berserk) and the Acrobatics Dodge power write a DV stat that CombatSystem.GetDV never reads — and on NPCs the write silently no-ops because only the Player has the stat. Meanwhile the sidebar always shows DV 0. Small, high-leverage wiring fix with clear counter-check TDD.

**Sub-milestones:**
- CombatSystem.GetDV (644-672) adds the entity's DV stat bonus-minus-penalty to the computed value — RED test: applying StunnedEffect lowers effective DV; counter-check: removal restores it
- Add a DV stat (0) to the base Creature blueprint so NPC-targeted effect writes land — per-effect counter-check pair (effect applied vs not) across the 5 effects plus Acrobatics Dodge
- Fix the sidebar/inventory DV shadowing: skip the raw DV/AV stats in InventoryScreenData.BuildPlayerStats' remaining-stats loop so FindStat returns the computed entry — test: equipping DV armor changes the displayed value

**Verification:** PLAUSIBLE (not externally verified — verify evidence inline before each sub-milestone).

### 10. [P1] skill-mp-economy — Skill pricing, SP discoverability, and the dead MP axis

**Systems:** progression, ux-input

Acrobatics is mathematically unpurchasable (100 SP vs +1 SP/level), the player is never told SP exists or that X opens the skill screen, skill 'trees' enforce no prerequisites, and the entire MP/mutation-advancement economy no-ops because no MP stat exists. Content edits plus one small spend UI complete three half-built progression axes.

**Sub-milestones:**
- Reprice Acrobatics.json (tree 100→1, powers 50→1) to match the v1.5 accessibility pricing — revives the only +DV passive and 3 actives
- Extend the level-up announcement to '+1 skill point — press [X] to spend' (LevelingSystem.cs:85) and add an SP readout to the sidebar — makes the earn-and-spend loop discoverable
- Add Requires chains to the skill JSONs (powers require their tree root; Axe_Decapitate requires Axe_Dismember) — enforcement code already ships in BuySkillAction, content-only
- Add an MP stat (0/0/999) to the Player blueprint so the existing level-up MP grant stops silently no-oping and the sidebar 'MP -' becomes a real number
- Minimal MP spend UI: a rank-up row in the existing X screen (or a GrimoirePickerUI-style popup) calling MutationsPart.SpendMPToIncreaseMutation — grimoire spells can finally grow past rank 1

**Verification:** PLAUSIBLE (not externally verified — verify evidence inline before each sub-milestone).

### 11. [P1] followers-live — Followers reachable and combat-capable

**Systems:** ai-npc, progression, content-json

The follower stack (~180 tests: party alignment, follow AI, cross-zone transit) has no player-reachable acquisition path (no Persuasion.json) and player-led followers would be combat-inert anyway (assist gates on an NPC-leader KillGoal; NPC damage never provokes). Complete the loop end-to-end with skill-file authoring plus two small AI gate extensions.

**Sub-milestones:**
- Author Content/Data/Skills/Persuasion.json (copy Acrobatics.json's shape) declaring the tree with Recruit + Dismiss — test: SkillRegistry loads it and the X screen lists it
- Groundwork: set player BrainPart.Target in InputHandler's two attack paths (bump-attack ~:673 and ExecuteAttackOnNPC :3397) — two lines, independently testable
- Extend FollowLeaderGoal's assist gate for player leaders (leader has Player tag + live in-zone Target); counter-check: follower does not attack when the player has no target
- Widen CombatSystem.cs:1039 so NPC-sourced damage also provokes the victim's personal hostility (minimal F.4 mutual defense) — followers finally defend themselves when mauled
- Route follower-kill XP to the party leader (CombatSystem.cs:1245 currently gates on killer.HasTag('Player')) so the recruit build isn't silently punished

**Verification:** PLAUSIBLE (not externally verified — verify evidence inline before each sub-milestone).

### 12. [P1] input-correctness — Input edge-case fixes

**Systems:** ux-input

Small correctness bugs that read as broken game: Shift+Comma fires pickup then transitions zones anyway (can strand a stale pickup popup), empty-tile pickup gives zero in-game feedback, look mode entry is silent, and 'l' silently shadows the advertised vi-key right-move. Each fix is minutes with a clear test.

**Sub-milestones:**
- Add !shiftHeld guard + early return to the G/Comma pickup branch (InputHandler.cs:731-735, mirroring the wait-key shift guard at 426-429) — test the '<'-on-loot-cell fall-through no longer double-fires
- Replace the Debug.Log at InputHandler.cs:1301 with MessageLog.Add('Nothing to pick up here.')
- MessageLog hint on entering look mode ('Look: move cursor, Enter=actions, Esc=exit')
- Resolve the vi-key 'l' shadowing: either move look mode off L or correct the docstring/docs to claim only wasd/arrows/numpad — pick one and align docs with impl

**Verification:** PLAUSIBLE (not externally verified — verify evidence inline before each sub-milestone).

### 13. [P1] dormant-content — Spawn the authored-but-unreachable content

**Systems:** quests-narrative, ai-npc, world-content

~190 nodes of the game's deepest dialogue, 10 lore-faction creatures, 3 finished quests, three village idle-AI behaviors, and the entire enemy ranged-ability path are all complete but unspawned/unregistered. Pure wiring: builder lines and JSON references, no new systems.

**Sub-milestones:**
- Fix Marceline_Quest.json's quest ID from IronKeyShowcaseQuest to IronKeyQuest (the orphaned quest JSON already matches her dialogue) — dead content becomes a working quest awaiting a spawn point
- Add Undertaker, PetDog, and (chance-gated) Magpie spawn lines to VillagePopulationBuilder's roster (~3 lines) — corpse burial, fetch/pet, and hoarding behaviors become visible; VillageChild's AIPetter gets a dog
- Spawn the lore-faction NPCs at low weights in their home-biome POIs/population tables (SaccharineEnvoy, PaleCurator, ChoirTendril, PalimpsestEcho, Mogu/Grib per WorldGenerator's biome-faction mapping) — the faction system stops feeling binary
- Place Marceline + an IronKey in the world, and hook EnchiridionQuest (the richest authored quest) into a cave/lair via the shipped QuestStarter.IfQuestCompleted gate — start of a mini-arc out of the starting village
- Attach existing Cryomancy/Pyromancy powers to IceWight/CharredHusk via a Skills part so KillGoal's already-working ranged-ability path fires in real play — verify with the ElementalCreatureZoo scenario

**Verification:** PLAUSIBLE (not externally verified — verify evidence inline before each sub-milestone).

### 14. [P2] persistence-polish — World-consistency polish

**Systems:** quests-narrative, combat-effects, ai-npc, world-content

Post-alpha-bar consistency work for the persistent world: drama state surviving save/load, quests failing gracefully when the giver dies, non-humanoid anatomy, timed tonics, hostility de-escalation, and world-map fog. Each sub is independently shippable; none blocks a good first week of play.

**Sub-milestones:**
- Quest-giver death → StoryletPart.FailQuest via the existing 'Died' event, with a log line — no more permanently-stranded active quests
- House drama save/load: serialize HouseDramaRuntime state (active drama IDs, pressure points, witness facts) alongside StoryletPart/NarrativeStatePart and re-activate on load
- Anatomy props for the ~15 non-humanoids (Quadruped bears, Insectoid scorpions/spiders, Simple slimes/wurms/bats) — ends 'you hit the sand wurm in the left hand'
- Convert TonicPart stat boosts to timed effects (currently permanent +4 Str stacking = infinite stat growth from vendor stock)
- 'MakePeace' conversation action (drams or rep cost) clearing the player from a speaker's PersonalEnemies — one stray hit no longer ends a merchant relationship forever
- World-map fog rendering from the already-saved Visited bitmap ('?' glyphs, hidden POIs) and 2-4 RiverChunk POIs in WorldGenerator to surface the finished river pipeline; buffer zone connections per generation attempt to fix the retry stair-leak
- Repeat pool-quest villages: at minimum an acknowledging giver line ('heard you handled the same trouble up north'); better, per-settlement quest instance IDs so each village's copy is independently completable

**Verification:** PLAUSIBLE (not externally verified — verify evidence inline before each sub-milestone).


## Per-lens analyses (reference)

The eight full system analyses (works-well lists, all 67 gaps with
file:line evidence, quick wins) are preserved in the workflow output;
the plan above is the deduped, prioritized synthesis. Key per-lens
verdicts:

**combat-effects:** The combat resolution machinery is deep and genuinely production-quality — body-part-aware multi-weapon melee, Qud-parity penetration, crits, dismemberment, idempotent death, a fully-wired thrown-weapon pipeline, and 31 status effects, all backed by heavy test/diag coverage; the July 2026 full-system audit's punch list is verifiably closed in current source. But the NUMBERS layer around that machinery is still dev-placeholder: the player has 500 HP, nearly every monster punches for 1d2, XP values and skill costs are unbalanced or stubbed, and the boot flow grants debug god-mode loadouts. The result is that a technically excellent combat system plays as a zero-stakes sandbox — a player cannot meaningfully lose, so healing, leveling, death recovery, and enemy tiers are all inert. One real wiring bug also survives: five status effects shift a "DV" stat that combat resolution never reads.

**ai-npc:** The AI substrate is genuinely strong — a Qud-mirroring goal-stack brain (BoredGoal/KillGoal/FleeGoal + 20 goal classes), faction/reputation integration, village idle behaviors, ambush archetypes, and a heavily-tested follower stack, all with observability (Think/goal inspector/diag). The alpha problems are almost entirely WIRING and CONTENT gaps, not engine bugs: roughly a third of the bestiary (including 3 of 4 lair bosses and the starting area's IceWights) has no Faction tag and is therefore permanently neutral and will never attack the player; the entire follower feature (F.1–F.3, ~180 tests) has no player-reachable acquisition path; and player-led followers would not fight even if recruited. Fixing the faction-tag gap alone transforms the combat loop from "half the monsters are scenery" to fully functional.

**progression:** The XP→level→SP→skill-purchase spine is genuinely complete and player-reachable end to end: kills and ~10 quests grant XP, leveling fires with FX and announcements, +1 SP per level buys skills from a working X-key screen with 10 trees / 52 powers, and purchased actives auto-register on the hotbar. The other advancement axes are hollow: mutation advancement (the entire MP economy) is dead code the player can never touch, attributes never grow on level-up, equipment progression plateaus at Tier 2, and a 500-HP player against 15-25 HP enemies makes every progression reward imperceptible because nothing can threaten you.

**economy-items:** The trade/currency layer is genuinely finished and well-tested (Qud-parity pricing, faction modifiers, item vetoes, diag records, full TradeUI reachable from any inventory-bearing NPC's conversation), and both crafting stations plus tinkering are player-reachable with atomic, anti-exploit command plumbing. But the economy around that machinery is not a loop: crafting inputs are a finite one-time starter kit, enemies drop nothing sellable or usable, traders never restock items or drams, and most of the authored item catalog (exotic weapons, advanced tonics, schematics) has no spawn source outside dev scenarios. A player who engages the craft/loot/trade loops hits hard resource dead-ends within the first few hours; the current experience is propped up by dev-convenience grants (all tinker recipes + bits, all 18 grimoires free in a village chest, Panacea at spawn).

**quests-narrative:** The quest/storylet/conversation engine is genuinely strong: 8 fully completable quest loops are wired into normal play (2 in the starting village, 6 distributed one-per-village), objectives are fact-based and order-independent (no softlocks), state save/loads correctly, and everything is diag-instrumented and heavily tested. The gaps are almost all content-reachability and feedback seams, not engine bugs: half of all villages spawn 4 completely mute House Vex drama NPCs (their conversation JSON was never written), ~190 nodes of the deepest authored dialogue (faction NPCs, Marceline) are attached to blueprints nothing ever spawns, quest lifecycle produces zero message-log feedback, house-drama progress evaporates on save/load, and there is no main goal or call-to-adventure — play is entirely side-quest-driven with only a subtle yellow tint marking quest-givers.

**world-content:** The worldgen substrate is genuinely solid and fully player-reachable: a 20x20 overworld with 4 biomes, villages/lairs/underground/world-map travel all wired into input, saves, and rendering, with no softlocks found (edge/stair/worldmap transitions all have passable-cell fallbacks). Content is heavily front-loaded around the starting village (quests, crafting, set pieces); the frontier under-delivers — difficulty plateaus at tier 2, the infinite underground is snapjaws-only at every depth, lair bosses drop nothing special, and merchant-camp POIs generate zones with no merchants. Demo/debug artifacts (barrel fire demos in every village, debug sandbox/weapon/NPC at spawn) leak into normal play.

**lifecycle-saveload:** The player-lifecycle skeleton is genuinely built and wired end-to-end: 0 HP triggers an idempotent death path, an input-suppressing death modal (L=load/R=restart), a manual quicksave (F5 + pause menu), a boot Continue/New-Game prompt, and a heavily-tested atomic save format with full runtime rewiring on load. However, two P0s gut it in real play: saves are written to per-boot GUID directories that nothing can ever find again after an app/editor restart (10 orphaned save dirs already exist on disk), and the roadmap-promised autosave never shipped, so a death without a manual F5 wipes the whole session. On top of that, the 500 HP debug player makes death practically unreachable, so the entire recover-from-death loop is untested by actual play and the game has no fail-state tension.

**ux-input:** The input/UI layer is architecturally mature — a 19-state modal state machine with well-isolated per-modal handlers, wired UIs for inventory (tabbed, with crafting/abilities), skills, ability manager, quest log, factions, trade, dialogue, look mode, throw targeting, pickup, and pause, all with in-modal key-hint footers. The fatal weakness is the first five minutes: there is zero onboarding — no help screen, no controls listing, no boot text — so nearly every one of those systems is locked behind an unguessable single-letter key, and several debug keys (including one that permanently dismembers the player) sit live on the shipping input path.


## Implementation log

(One section per plan item as it ships — status, tests, divergences,
commit. Appended in-commit per CLAUDE.md living-doc rules.)

### 1. combat-stakes — SHIPPED

- **SM1 mortal player:** Player Hitpoints 500→40/40 (Objects.json).
  Pin: `AlphaCombatStakesTests.Player_Hitpoints_AlphaTuned…`.
- **SM2 tier-scaled XP:** 20 creatures gain XPValue overrides
  (CaveBat 5 → ChoirTendril 70); ordering pins
  (StoneGolem > SnapjawHunter, AncientGuardian > StoneGolem, …).
- **SM3 natural weapons:** 21 new NaturalWeaponFactory entries + 21
  `NaturalWeapon` blueprint Props — including the four DEAD-CODE
  MeleeWeapon-part creatures the verifier flagged (Glowmaw 2d4,
  SleepingTroll 2d6, MimicChest 1d8, AmbushBandit 1d6, converted via
  props; their inert entity-level MeleeWeapon parts left in place
  deliberately — several content tests pin them, and they remain
  harmless dead data). End-to-end pins walk the real Body →
  RegenerateDefaultEquipment → `_DefaultBehavior` path; counter-check:
  Villager still resolves the 1d2 default fist.
- **SM4 venom:** ViperBite `Poisoned,75,1d6,8,0`, ScorpionSting
  `…50,1d4,6,0`, SpiderBite `…35,1d4,6,0` via the shipped
  OnHitEffectsRaw spec format (factory gained an onHit param);
  counter-check: CaveBearClaw carries no spec.
- **SM5 lethality sweep:** NOT live-verified (Play mode resets the
  user's scene) — honesty bound: tuning is arithmetic-verified
  (CaveBear 2d4+Str vs 40 HP + 4d4 tonics is a real fight; base
  Creature XP 10 preserved for unlisted creatures). Live retune pass
  on first playtest.
- **Ripple:** `PlayerBuilderTests.SetHp_SetsAbsoluteBaseValue` used
  100 HP — only valid under the old 500 Max (SetHp clamps by
  contract; SetHpMax raises). Re-pinned at 30 with a comment.

Tests: 9 new (7 RED→GREEN reproducing every gap + 2 counter-checks
green by design).

### 2. hostile-bestiary — SHIPPED

- **SM1+SM2:** new `Beasts` faction (Factions.json:
  InitialPlayerReputation −100, mutual −100 with Villagers); all 17
  formerly-factionless creatures tagged `Faction=Beasts` — wildlife,
  the three reachable lair bosses, and IceWight/CharredHusk. Single
  faction chosen deliberately over per-family splits (alpha bar is
  "they attack"; flavor splits are content polish).
- **SM3:** GlowmawAmbushPart drop now calls
  `brain.SetPersonallyHostile(player)` beside the Target assignment;
  counter-check pins the PERSONAL channel only (verifier-corrected
  phrasing — faction hostility rightly exists pre-drop after SM1).
- **SM4 PlayMode sweep:** honesty bound — not live-run (Play resets
  the user's scene); the KillGoal/bump-attack machinery is
  already test-covered, and hostility now feeds it via the same
  FactionManager.IsHostile path the tests pin.

Tests: 4 new (3 RED→GREEN incl. the exact neutrality repro + 1
friendly-roster counter-check).

### 3. save-lifeline — SHIPPED

- **SM3 boot discovery:** `SaveGameService.DiscoverLatestGameID(root)`
  (pure, test-driven scan of `Saves/*/Quick.json` newest
  `SaveTimestampUtc`, skipping ghost dirs and malformed metadata) +
  `ResolveActiveGameIDOnBoot()` (PlayerPrefs fast path written on
  every save, scan fallback) called in `DoStart` BEFORE the boot-menu
  `HasQuickSave` gate. The per-boot GUID orphaning is closed: old
  saves are rediscovered across app restarts.
- **SM1 corruption safety [CORRECTED per verifier]:** `LoadSlot` wraps
  deserialize+apply in try/catch → LogError + `false` (previously a
  corrupted file was an UNHANDLED exception in `InputHandler.Update`
  and every "load failed" message downstream was unreachable). Pinned
  with a real garbage `.sav.gz` on disk. Death screen consumes
  QuickLoad's bool: failure logs "Load failed — save may be
  corrupted. Press [R] to restart." and keeps the modal alive.
- **SM2:** `MessageLog.Clear()` at the top of `DoStart` — a restarted
  run no longer opens with the previous life's death spam.
- **SM4 autosave:** `QuickSave()` + "Autosaved." at the end of
  `HandleZoneTransition` (runs only for successful transitions — both
  call sites gate on the result). Death is recoverable for a player
  who never learned F5. SM5's separate autosave slot was collapsed:
  autosave writes the SAME Quick slot the death screen already loads.
- **Honesty bounds:** the restart-flow MessageLog clear and the live
  autosave cadence are play-path (MonoBehaviour boot / zone walk) —
  decision logic and failure paths are unit-pinned; a live
  die-without-F5 → [L] restore walk is the playtest check.

Tests: +7 (4 discovery incl. malformed-metadata tolerance, 1
corruption round-trip on disk, 2 death-screen failure/success pins).
Sequencing note: the two death-screen pins compiled alongside the
production edit (a fixture API mismatch blocked their isolated RED
run); the discovery/corruption tests were strict compile-RED first.

### 4. strip-debug — SHIPPED

- **DevMode gate** (`Assets/Scripts/Shared/DevMode.cs`, default OFF,
  a FIELD not a const so tests/dev tooling can toggle+restore).
- **SM1 keys:** F7/F8/F9/P branches gated + in-method
  `!DevMode.Enabled` guards (defense-in-depth); the DEAD F6
  grant-random-mutation branch and its method deleted outright
  (verifier: always shadowed by SaveLoadInputController); the stale
  "F10" docstring/log labels in TryDebugCycleWellState corrected to P.
- **SM2:** GrantShowcaseSpellMutations, InitializePlayerStartingTinkering
  (all recipes + all bits), GivePlayerStartingTonics (8 one-of-each),
  GivePlayerCraftingStarterKit (19 stacks), SpawnDebugWeapon/NPC — all
  DevMode-only. Counter-check: DevMode on restores the full sandbox.
- **SM3 designed loadout:** `NewGameLoadout` (Dagger ×1, HealingTonic
  ×2, DriedMeat ×2 — pinned exactly). Decision recorded per the
  verifier's demand: the crafting kit + tonic spread are DEV-only;
  the FARMING kit stays in both modes (designed feature loop).
- **SM4:** barrel demos + material sandbox DevMode-only; compass
  stones kept (navigation content). Divergence: the plan's "keep one
  barrel layout as a fire tutorial" was dropped — all-or-nothing is
  simpler and a tutorial belongs to a designed-content pass.

Tests: +6 (DevMode default pin, loadout-exact pin, stack-aware grant,
null-safety, F8-no-op when off + still-works counter-check when on).
