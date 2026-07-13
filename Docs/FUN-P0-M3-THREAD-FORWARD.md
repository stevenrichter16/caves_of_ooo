# FUN-P0 · M3 — Thread Forward + The Game Speaks

> **Status:** Pending (after M2) · Parent: `FUN-P0-SPINE.md`
> **Goal:** There is always a named next want, and the game acknowledges
> deeds: quest lifecycle + zone arrivals reach the MessageLog, mute drama
> NPCs speak, quests point across the map and chain into each other.

---

## Verification sweep corrections (§1.2)

| # | Analysis claim | Sweep finding | Plan impact |
|---|---|---|---|
| C1 | "Four quest events, only dev listener" | **Five** events (QuestFailed too), fired on `StoryletPart.LocalPlayer` via `FireQuestEvent` (StoryletPart.cs:244-258) with params QuestId/ObjectiveId/FromIndex/ToIndex; QuestEventTests pins the contract (consume, don't alter). LocalPlayer is set at GameBootstrap :272 (fresh) and :743 (load) — announcer part must be ensured on BOTH paths. | Announcer = new `QuestAnnouncerPart` on the player (idempotent add both paths), resolving quest/stage/objective display text from StoryletRegistry. |
| C2 | Announcement channel | `MessageLog.Add` is plain (renderer colors by recency); `AddAnnouncement` = modal popup + flash. **No inline color markup in log lines** — '&Y' would render literally. StoryletReactorTests has ~15 exact GetLast/Count assertions: announcements must NOT fire from storylet lifecycle internals. | Objectives/stages → `Add`; quest complete → `AddAnnouncement`. Emission only from the listener part (tests that don't attach it see zero new lines). |
| C3 | Zone arrival choke point | All three transition paths converge on `InputHandler.HandleZoneTransition` (:764-825, private Mono — EditMode-untestable). Naming data available: `WorldMap.FromZoneID` → `GetPOI(wx,wy).Name`, `GetBiome`, `GetDepth`. WorldMapObservabilityTests assert EXACT record counts on Category="worldmap" with **no Kind filter** — a new worldmap-category diag record would break 4 tests. | Pure static composer `ZoneArrival.ComposeArrivalMessage(zoneID, worldMap)` (testable), called from HandleZoneTransition. Diag: **new category "arrival"** (default-off) instead of "worldmap" (C3 exact-count trap). |
| C4 | Vex NPCs mute | VERIFIED mechanism: HouseDramaZoneBuilder:93-95 unconditionally overwrites working ConversationIDs (Elder_1, Villager_1…) with `Drama_Vex_{Role}` which has no data; C-key path bypasses the world-action "Hi." fallback. HouseDramaZoneBuilderNpcTests:86 pins the unconditional stamping format. Removing HouseVex from rotation would re-pick dramas in every village (modulo shift). | Fix: **conditional stamping** — only overwrite ConversationID when `ConversationLoader.Get(dramaConvId) != null`. Update the stamping-format pin test to the new contract. Vex stays in rotation (its NPCs keep stock conversations until Vex dialogue is authored in P1). |
| C5 | Cross-zone mechanism | Objectives poll global facts; parts attached at target-zone generation time; zones generate lazily+deterministically; NarrativeState is world-entity-persisted. ClearTheWarren is actually same-zone (all gnomes in the giver's village) and uses **AddFactWhenSlain** (increment), not SetFactWhenSlain. Objective-level Triggers are NOT load-validated (typo'd predicate evaluates TRUE). | Cross-zone quests attach kill-fact parts in the **lair pipeline** (boss slain → fact). Every new objective predicate name double-checked against the registered vocabulary (validation gap). |
| C6 | Pool growth | `PickVillageQuest = hash % pool.Length` — **adding pool entries remaps every village's quest** (save-visible); QuestVillagePoolTests distribution test samples 400 zone IDs. CinnamonBunFavor is fully authored; EnchiridionQuest/IronKeyQuest have placeholder external triggers + bench pins (QuestSystemBench expects EnchiridionQuest stage-0 objective count == 3; MarcelineQuestDialogueTests pins Marceline → IronKeyShowcaseQuest). | Pool grows in ONE commit (single remap). EnchiridionQuest/IronKey promotion constraints: keep stage-0 objective count; keep Marceline's quest ID. |
| C7 | Reward vocabulary | Full action/predicate palette enumerated (ConversationActions.cs) — incl. StartQuest (validates + runs stage-0 OnEnter + diag), GiveItem, SetFact/AddFact, Reveal. AwardXP is a conversation action (bypasses XPValue). | New quest JSON authored strictly from the registered vocabulary. |

## Scope

**In:** M3.a QuestAnnouncerPart + arrival lines · M3.b Vex conditional stamping · M3.c two cross-zone quests (lair-pointer + delivery) · M3.d completion-node chaining + CinnamonBunFavor pool promotion.
**Out (pruned):** EnchiridionQuest/IronKeyQuest world placement (placeholder triggers make them P1 authoring work — pool remap risk taken once now with CinnamonBun, again in P1 with the rest); journal UI (P1); Village Book main quest (P1 headline); periodic ambient sensory narrator (P1).

## Content readiness

- 🟢 Event contract, MessageLog, POI lookups, fact plumbing, conversation vocabulary all verified.
- 🟢 CinnamonBunFavor fully authored (conversation + storylet + content tests).
- 🟡 New conversation/storylet JSON for 2 cross-zone quests (authoring work, existing vocabulary only).
- 🟡 Pool remap is save-visible (documented divergence; pre-P0 saves are dev saves).

## Sub-milestones

### M3.a — The game speaks
- `QuestAnnouncerPart` (player part): handles the 5 quest events → lines like
  `Quest started: The Missing Cartridge.` / `Objective complete: Find BMO's
  cartridge.` / quest complete via `AddAnnouncement`. Text resolved from
  StoryletRegistry (quest name, objective Text). Diag: category "quest",
  kind "Announced" (default-on category, new kind — no exact-count pins on
  "quest" category+no-kind filters per sweep).
- `ZoneArrival` static composer + HandleZoneTransition call: first-visit
  village → `You arrive at Kyakukya.`; biome line otherwise; depth line for
  z>0 (`You descend into the deep earth (level 2).`); world map → `You survey
  the world from above.` First-visit tracked via `ZoneManager.CachedZones`
  pre-transition check (sweep C-answer) — composer stays pure.
- Ensured on fresh + load paths (C1). Tests: event→line mapping incl. exact
  strings, no-listener counter-check (StoryletReactorTests isolation, C2),
  composer table-driven tests (village/biome/depth/worldmap/invalid),
  announcer save-reload idempotency (no double-attach).

### M3.b — Drama NPCs speak
- HouseDramaZoneBuilder: stamp `Drama_{ID}_{Role}` only when the conversation
  exists (C4); otherwise keep blueprint conversation. Update
  HouseDramaZoneBuilderNpcTests stamping pin: Thresker roles stamped,
  Vex roles keep Elder_1/Villager_1/… (RED first: C-press-on-Vex-NPC
  currently yields no dialogue data — pin via ConversationLoader lookup).

### M3.c — Cross-zone quests (the pull vector)
- **"The Chieftain's Trophy"**: village Elder variant points at the nearest
  Cave-biome lair by POI name + world coordinates; boss-slain fact
  (`lair_boss_slain_<zoneID>` via SetFactWhenSlain attached in lair pipeline
  on the boss); reward: drams + XP + ChoirIron mineral. New conversation
  nodes on Warden (thematic: the watch wants the raiders gone).
- **"Letter for the Next Village"**: Innkeeper hands a letter addressed to a
  neighboring village's Innkeeper (nearest other village POI, named in quest
  text); delivery via existing TakeItem/IfHaveItem + SetProperty pattern
  (MessageForHermit shape, cross-zone recipient); reward: drams + first
  GiveInk in shipped content (10 ink — makes the rental loop's currency
  earnable for the first time, P0-scoped teaser of the P1 faucet).
- Both quests name their destination POI in dialogue + quest-log text.
- Tests: storylet content pins; predicate names validated against registry
  (C5 gap); lair pipeline attaches boss fact part (counter-check: non-boss
  lairs don't); delivery works when target village generates after accept.

### M3.d — Chaining + pool growth
- Completion nodes gain pointer choices (existing actions only): RootBeerGuy
  → BMO (`Root Beer Guy mentions BMO fussing about a lost cartridge.`), BMO →
  Chieftain's Trophy, pool quests → Letter chain. Implemented as extra
  conversation choices gated on IfQuestCompleted + not-already-started.
- Pool: += CinnamonBunFavor, ChieftainsTrophy, LetterForTheNextVillage
  (one commit — single remap, C6); rerun distribution tests.
- Tests: chaining choices appear only post-completion (counter-check:
  absent before); pool reachability across 400 zone IDs still passes.

## Performance note
Announcer fires per quest event (rare) and per zone transition (rare);
composer allocates only on transition. No per-turn polling added — the
part is event-driven via the existing HandleEvent dispatch.

## Divergences
- Arrival lines are CoO-original (Qud shows location in the HUD instead —
  sidebar location line deferred to P1 with fingerprint-hash care).
- Pool remap changes which quest existing villages offer (save-visible,
  accepted for P0; documented in commit body).

## Implementation log
(filled per sub-milestone)
