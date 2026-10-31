# Place Work and Progression — design and plan

Status: **design and plan, 2026-10-30. P1 (progression bench), P2 (creature levels), P2b (player growth), P3 (scaled and companion kill XP), P4 (tier-correct spawning), C1 (concern substrate) and C2 (concern conversation) implemented;**
each later milestone records its own status as it lands. This is CoO-original design
(one piece, tier-scaled XP, is a Qud-parity port and is labelled as such).

User direction (2026-10-30): start on (1) giving settlements real work and
(2) matching progression and danger to the world's tiers. "The fill of the
gameplay and content needs to go all the way through. No adding surface level
stuff. Design first, plan, then implement."

"All the way through" is read as two requirements:

- **Coverage:** every place on the map, every tier from Sill to the Root,
  surface and underground, early game to the ending.
- **Depth:** each piece of work runs through every layer: generation →
  conversation → journal → real world objects and places → persistent state →
  visible consequences → save/load → progression and rewards. No template
  errands whose only consequence is gold.

---

## 1. Evidence (seed 64 census, 2026-10-30)

A whole-world probe generated all 400 surface zones plus underground floors 1–3
under every site and every fourth zone (556 zones), with the game's
registries loaded as the bootstrap loads them. Raw output is in the session
scratchpad; the findings that drive this design are:

| Finding | Number |
|---|---|
| Talking NPCs / of which traders | ~400 / ~360 |
| Reachable quests in the whole world | ~16 (most single-step fetch, reach or kill) |
| Places sharing the same Elder/Merchant/Quartermaster/Tinker/Scribe/Innkeeper kit | 15 of 17 villages |
| Places with a settlement-repair system (well/oven/lantern) | 2 (Sill, Wellmeet) |
| Overwrit (23 zones): hostiles / NPCs / harvestables | 4 / 0 / 0 |
| Underground sampled (156 zones): NPCs | 14, all at sinkhole floors |
| Total kill XP, whole Spread surface (129 zones) | 2,739 (≈ level 5) |
| Total kill XP, Beating surface (101 zones) | 34,178 (12× the Spread) |
| Stump (designed tier 3–5): kill XP / most common enemies | 1,942 / tier-1 frogs and singers |
| Creatures with a `Level` stat | 1 of 124 (XPValue is hand-set per blueprint) |
| Highest creature tier actually spawned | 3 (tier-4/5 map cells reuse tier-3 tables) |
| Authored creatures never spawned | 30, incl. Sari-Snakes, Sky-Sari, Pale Stalker, Obsidian Brute |

### Verification sweep — corrections and confirmed premises

| Premise | Evidence | Result |
|---|---|---|
| The nine legacy "Candy" quests are unreachable | `VillagePopulationBuilder.PickVillageQuest` places six of them at fixed canonical hosts (Tine, Quillhold, Wellmeet, Gantry, Posy, Sumphold) with code-assigned givers; Ellun/Hallun are placed at Sill | **False.** ~16 quests are reachable. The gap is depth and coverage, not reachability. |
| World chests hold only coins | First probe ran without `LootTableRegistry`; with it, ~1,800 containers hold varied, tier-gated loot | **False.** Loot quantity is not a gap. |
| Spawn tables stop at tier 3 | `PopulationTable.GetBiomeTable`: `tier >= 3` → `*Tier3()` | Confirmed. |
| Underground danger follows depth, not place | `OverworldZoneManager` cave floors: `depth / 3 + 1`; sinkhole floors `min(surfaceTier + 1, 8)` | Confirmed (mixed rule). |
| Map tiers match canon place tiers | `WorldMapAuthoring.TierRows` vs `Lore/History/02_Geography.md` §II | **Mismatch:** Tally, Quillhold, Posy, Marrowstye, Sumphold are 1 (canon 2); Lampwell 1 (3); Spivenor and the Quiet's Door 2 (4); Slip 1 (3); Last Counter 3 (4); Olderdeep 4 (3). |
| Settlement repair is data-driven | `SettlementManager.ApplyRepairMethod` is a hard-coded switch; `SettlementSiteDefinitions.IsTrackedVillage` admits Sill and Wellmeet only | Confirmed hard-coded; generalising it is part of this plan. |
| CoO awards XP with Qud's tier scaling | Qud `XRL.World.Parts/Experience.cs:33-50` halves/tenths/zeroes XP by level band difference; CoO `LevelingSystem.AwardKillXP` awards XPValue flat | Confirmed missing. |
| Quests feed the closure ledger | `StoryletPart.StartQuest` opens a ledger entry; `RefuseQuest` ends it Refused | Confirmed; concerns compiled to quests inherit it. |
| Quests can be registered at runtime | `StoryletRegistry.Register(StoryletData)` | Confirmed. |
| Conversations accept dynamic choices | `ConversationManager.RefreshVisibleChoices` → `RegionalGuidance.AppendChoices`, `SpreadDiscoveryReports.AppendChoices` | Confirmed provider pattern. |
| A hook exists to add things to already-generated zones | `OnZoneGenerated` is fresh-only; `OnZoneAttached` must never rebuild contents; `WorldTravellers.OnZoneEntered(player, zone)` runs with the player present | Confirmed. Pending sites will apply on player entry, with a saved ledger part on the World entity (the `LocalPeopleLedgerPart` pattern). |

---

## 2. Design — Place Work ("concerns")

### Pillars

1. **Every place has its own troubles, in its own voice.** Work is authored per
   place from canon: its economy, its factions, its geography. Mechanics
   underneath are shared; every instance has a named owner, a specific reason
   and a specific location. No "kill five X" templates.
2. **More than one way through, each leaving a different place behind.** A
   concern offers two or three approaches that use different systems (fight,
   supply, restore, carry, read, negotiate). Each produces a different
   outcome: temporary or lasting, taught or dependent, and which faction gains.
3. **The world remembers.** Outcomes are persistent place state that visibly
   changes the place: services, stock and prices, defenders, repaired
   structures, residents' lines, and what neighbouring places say about it.
4. **Saying no is a real verb.** Every concern can be refused aloud (closure
   ledger `Refused`); some are refused by taking a rival's side. Dropping one
   silently leaves it open, as canon requires (`Lore/11_SecondSpine.md` C4).
5. **Work leads somewhere.** Each place's arc ends pointing at another place,
   usually one tier deeper, so the travel graph becomes the progression graph.
   Tier-1 arcs teach systems; tier-3+ arcs touch the factions' central dramas
   and the main spine.
6. **Rewards are progression.** Concern XP is a planned share of each tier's
   XP budget (§3). Arcs can teach a power without spending SP, give
   place-unique gear, and open trade or services.

### What a concern is

A **concern** is one authored trouble of one place:

```
Concern
  Id, Place, Tier, Faction stake
  Owner          the resident who raises it (role or named blueprint)
  Title, Pitch   voice-carded text (Lore voice gates apply)
  Requires       predicates: earlier concerns, reputation, world facts, ending state
  Approaches[]   2–3 per concern
    Kind         Confront | Supply | Restore | Carry | Read | Settle
    Objective    compiled to quest objectives with real triggers
    WorldHooks   what is placed or marked in the world, and where
    Outcome      effects applied on success (see Consequences)
  Refusal        spoken line and its cost
  Reward         XP (from the tier budget), drams/ink, items, taught power
  Next           concerns or places this opens
```

Concerns live in `Content/Data/Concerns/<Place>.json`, are validated by a lint
test (every blueprint, place, quest, faction and power exists; every approach
has a reachable objective), and are compiled at load into ordinary
`StoryletData` quests (`concern.<id>`). That means the journal, objectives,
closure ledger, quest events, diag `quest`/`closure` records and save/load all
reuse the shipped quest engine.

### Approach kinds (each a real system, not a counter)

| Kind | What the player does | Built on |
|---|---|---|
| **Confront** | Deal with a specific threat at a specific place: a den, a raider camp, a creature that came up a sinkhole. The site is resolved at offer time to a real zone of the right tier and direction and written in the journal ("the burrow two fields west of the weir"). Its encounter is placed when the player arrives. | Pending-site ledger + `WorldTravellers.OnZoneEntered`; `FinishObjectiveWhenSlain`; tier-correct encounter groups (§3) |
| **Supply** | Bring real materials obtainable in the region (salt from the pans, peat, glow-quartz, fire clay). Sources are validated by a census test, so every request has a real nearby source. | Harvestables/veins; counted hand-over (new `IfHaveItemCount`/`TakeItemCount`) |
| **Restore** | Repair a working structure of the place (well, oven, lantern, and new place-specific sites: weir, mine pump, boat slip, salt pan, ink vat, lantern hatchery). A temporary fix (a rite), a lasting fix (materials and know-how), or teach a caretaker. Relapse while unmaintained. | `SettlementManager` repair model, generalised from a hard-coded switch to data |
| **Carry** | Take a sealed item or person to a named resident of another place. | `BogBodyCourier` pattern generalised; arrival marker |
| **Read** | Go to a place, read a plaque, ledger or wall, and report what it says. Brings the lore into play. | `ReadableDocumentPart`/`KnowledgePart` (one placard in the world today); markers |
| **Settle** | Resolve socially: pay, meet a reputation threshold, use Persuasion, or side with one faction against another (reputation moves both ways). | Reputation, conversation predicates and actions, skills |

### Consequences ("the place changes")

Outcome effects are data, applied on success and stored in settlement state:

- **Conditions** (`SettlementState.SetCondition`), read by a new predicate
  `IfSettlementCondition`, by trader stock (condition-gated stock rows), by
  price modifiers, by residents' lines and by site visuals.
- **Reputation** with one or more factions, signed.
- **Services and stock** opened or improved (a repaired forge sells better
  work; a restored hatchery sells lanterns).
- **Defenders or residents** added on the player's next arrival (applied at
  entry, never by regenerating a cached zone).
- **Relapse timers** for temporary fixes, as the well already does.
- **Taught powers**: a caretaker or master teaches one specific power free.
- **World facts** read by other places' residents ("You're the one who
  cleared the Sumphold slip").
- **Next concerns** unlocked, here or in another place.

### Discoverability

Each place's Elder (or its equivalent: Registrar, Host, Factor, Warden) answers
"What troubles this place?" with the place's current open concerns and who
raises them. Owners carry a quest beacon. Residents of neighbouring places
mention unresolved troubles they would know about. No floating markers beyond
the existing beacon.

### Coverage plan (canon-grounded arcs)

Each place gets an arc of 2–4 chained concerns. The seeds below come from canon
(`Lore/History/02_Geography.md`, `Lore/Factions/*`) and the world design; final
text passes the voice gates.

| Tier | Place | Arc seed |
|---|---|---|
| 1 | Sill | The well, oven and lantern (existing, rebuilt on the new model); "Sill hears it" opens the arc toward Gantry and Tally. |
| 1 | Gantry | Paperwork politics: a Concord exchange and a Curation filer dispute a caravan manifest; Tent-Right guest rights at the rest stop. |
| 1 | Tine | Fishing weir and lake; the retired Recension scribe (the first deep-lore resident) points to Quillhold. |
| 1–2 | Cinderhold | Mine pump and flooding; Black-Gall extraction; mineral infusions (activates the dormant `WantsMineralPart`); the existing Pruning contract. |
| 2 | Tally | Debts as concerns: collect, escort a caravan, or formally default (a closure that costs reputation and is clean in cosmology). |
| 2 | Quillhold | Long transcription jobs paid in ink (activates the dormant ink economy); the Hidden Shrine. |
| 2 | Posy | Bower-Folk commissions: compose an installation, source resin; reputation economy. |
| 2 | Wellmeet | Salt trade and pans; the three-day oath; the warren. |
| 2 | Marrowstye | Intake of the region's dead; the Catcher cell in the disused wing. |
| 2 | Sumphold | Boat slip; peat cutters; bog bodies sold to the Concord, a real moral choice. |
| 3 | Drowned Ledger, Salt-Vault, Lampwell, Slip, Olderdeep, the First Tent | Faction-central work: the excavation, the Salted's file, the light trade, the east quarter going Hush, the Rooted. |
| 4 | Spivenor, the Deepest Cathedral, the Quiet's Door, the Last Counter | Choir integration, the Wedded, the Quiet, the threshold before the deep places. |
| 5 | Felling-Site, the Root | The main spine's endings. |
| — | Merchant camps | Caravan work: escort, repair, cargo. |

The six existing pool quests and the Morrowfast/Stillleaf/Bog/Pruning quests
are kept and folded into their places' arcs rather than duplicated.

---

## 3. Design — Progression matched to tiers

### Target curve

| Tier | Player level while there | Creature levels spawned |
|---|---|---|
| 1 | 1–5 | 1–4 |
| 2 | 5–9 | 4–8 |
| 3 | 9–13 | 8–12 |
| 4 | 13–17 | 12–16 |
| 5 | 17–20 | 16–20 |

The player's XP curve stays as it is (`15L³ + 100` to next level, Qud's).

### Rules

1. **Every creature has a `Level`; XP follows from it.** Levels are authored
   per blueprint from its current `Tier` tag and threat. `XPValue` becomes a
   function of `Level` (tunable constants), pinned by a test so the two cannot
   drift. Starting point `XPValue ≈ 0.6·L³ + 4`, which puts today's tier-3
   creatures (175–550 XP at levels 8–10) in range.
2. **Trivial kills stop paying (Qud parity).** Award `XPValue` halved, divided
   by ten, or zero when the player's level band (`Level / 5`) exceeds the
   victim's by 1, 2, or more than 2 (`Experience.cs:33-50`).
3. **Spawns follow tier.** A zone of tier T draws creatures of tier T and T−1;
   tier T+1 only as a rare, signposted "out of its depth" encounter; never
   T+2. Tier-4/5 tables exist, so those cells stop reusing tier-3 tables.
   Underground floors use their place's tier plus depth.
4. **Place tier is canon's.** A `PlaceTier` per authored place (from canon)
   governs place content: concerns, residents, stock, and sinkhole floors.
   Wilderness keeps the cell table, corrected where it contradicts canon.
5. **Work is part of the budget.** Concern XP is planned at about 40% of each
   tier's XP budget, so a player who engages with places progresses without
   grinding, and a pure fighter still progresses more slowly.
6. **Upper-tier creatures exist.** The 30 dormant creatures and new tier-4/5
   creatures (with native art through the pipeline) fill tiers 4–5.

### Measurement: the progression bench

A deterministic, self-auditing EditMode bench (CLAUDE.md "Deterministic
self-auditing scenarios"):

- **XP budget per tier**: kill XP available in each tier's zones at the
  expected player level (tier-scaled), plus concern XP; reports the level
  reached on exit from each tier.
- **Spawn tier violations**: every zone's creatures against its tier rule.
- **Duel table**: each starting build at level L with tier-appropriate gear
  against each creature at tier T, using the real combat code with seeded RNG.
  Reports turns to kill, damage taken and win rate. Includes a control row.

Each run is stamped and emits one record per cell; invariants (spawn rule, XP
bands, duel win-rate bands) are asserted as tests.

---

## 4. Plan — milestones, smallest blast radius first

Each milestone: RED → GREEN → counter-checks → adversarial pass where two or
more taxonomy surfaces apply → self-review → living-doc update → commit.

| # | Milestone | Ships | Depends on |
|---|---|---|---|
| **P1** | Progression bench | Census + duel bench; tests asserting the target invariants, RED today. | — |
| **P2** | Creature levels | `Level` on every creature, `XPValue = f(Level)` migration, pin test. | P1 |
| **P2b** | Player growth per level | Qud-parity level-up: HP gain rolled with the Toughness modifier, an attribute point at levels 3, 9, 15…, +1 to every attribute at 6, 12, 18 (`XRL.World.Parts/Leveler.cs:262-275`); the attribute-point spend path must reach the character screen. Added after the P1 duel baseline (below). | P1 |
| **P3** | Tier-scaled XP | Qud-parity award scaling in `LevelingSystem.AwardKillXP`, with diag. | P2 |
| **P4** | Tier-correct spawning | `PlaceTier`; spawn rule; tier-4/5 tables from existing upper-tier creatures; underground by place tier + depth; tier-table corrections. | P1–P3 |
| **C1** | Concern substrate | Data model, registry, lint, compile to quests, ledger part, save/load; Sill's well/oven/lantern rebuilt on it end to end. | — |
| **C2** | Concern conversation | Owner provider, accept/refuse aloud, status and turn-in, "What troubles this place?" | C1 |
| **C3** | Approach kinds | Supply (counted), Carry, Read, Settle, Restore (data-driven sites), Confront (pending sites at real zones). | C1–C2, P4 for encounter tiers |
| **C4** | Consequences | Conditions → stock, prices, lines, defenders, relapse, taught powers, world facts, next concerns. | C3 |
| **W1–W5** | Content waves | Tier 1 places → tier 2 → tier 3 → tier 4/5 → merchant camps and catacomb sites. Each wave: arcs authored, lint green, census of reachability, live play of at least one arc. | C1–C4 |
| **P5** | Upper-tier bestiary | Dormant creatures placed; new tier-4/5 creatures with native art. | P4 |
| **P6** | Final balance | Bench re-run with concern XP included; tune constants to the target curve. | all |

Order of work: P1 first (measure before changing), then C1 in parallel with
P2–P4, because the substrate does not depend on the balance numbers.

### Save compatibility

- Population and level changes apply to newly generated zones. Cached zones in
  existing saves keep their creatures; the census and bench measure fresh
  worlds. Documented, not migrated.
- Concern state is a new World-entity ledger part with an EOF-defensive load;
  older saves load with no concerns started.
- The existing Sill/Wellmeet site state is migrated into the generalised site
  model, preserving stage, relapse and conditions.

### Performance

- Pending-site application runs once per player zone entry (not per turn), over
  a short list.
- The conversation provider runs only while a conversation is open.
- No per-frame work; no new caches keyed on gameplay state beyond the ledger.

### Observability

Diag records: `concern/Offered|Accepted|Refused|Resolved|Relapsed|SitePlaced|SiteSkipped`
and `progression/XPAwarded` (amount, raw, scaling reason). Every rejection
names its gate.

### Honesty bounds

- The census is one seed at generation time; it does not include respawns,
  events, trader stock or death drops. The bench measures deterministic
  matchups, not play feel.
- Content waves are large writing work; each wave is verified in actual play
  for at least one arc, not every concern.
- New creature art follows the existing 16×24 pipeline and its review gates.

---

## P1 — Progression bench (implemented 2026-10-30)

**Ships:** `ProgressionCurve` (tier level bands, `XPValueForLevel`,
`CumulativeXPToReach`, Qud-parity `ScaleKillXP`) with 13 curve tests, and the
explicit `ProgressionBenchTests` (category `ProgressionBench`): a tier audit and
a duel table, each writing a stamped report to `Library/ProgressionBench/` and
one `progression` diag record per cell. Both bench tests fail today by design;
they are the RED that P2–P6 must turn green.

**Baseline, seed 64** (expected zone tier = cell tier on the surface, +1 for
floors 1–3):

| Tier | Zones | Creature tiers spawned | Out-of-band | Kill XP (scaled at band entry) | Planned kill share |
|---|---|---|---|---|---|
| 1 | 142 | t1 194, t2 2 | 2 | 2,855 | 1,140 ✓ |
| 2 | 170 | t1 375, t2 91, t3 16 | 16 | 9,862 | 11,004 |
| 3 | 138 | t1 93, t2 82, t3 109 | 93 | 32,397 | 43,332 |
| 4 | 80 | t1 96, t2 43, t3 5 | 139 | 1,251 | 111,948 |
| 5 | 26 | t1 46, t2 15 | 61 | 0 | 158,616 |

If tier-4/5 zones spawned creatures at those tiers' levels, their existing
headcounts (144 and 61) would cover the plan at `XPValueForLevel`; the fix is
placement and levels, not a new XP curve.

**Duel baseline (melee, starting kit plus one roll from the tier's find tables):**
melee builds win 93–100% at tier 1 and 62–68% at tier 2, then 18–43% at tier 3,
12–62% at tier 4 and 0% at tier 5. Levels add only +2 HP (plus 1 MP and 1 SP),
so player power barely grows while creature power climbs. **Correction to the
plan:** progression needs a player-growth step (P2b), not only creature levels.

**Bench limits:** melee only; one roll per find table (sparse kits); no skills,
spells, consumables or companions; non-melee builds are reported, not asserted.

**Self-review:** 🔵 the duel's gear is one roll per table, so kits vary by seed;
deliberate (find tables are what a player meets) and noted. 🧪 the
attribute-spend path for P2b is not yet audited.

## P2 — Creature levels (implemented 2026-10-30)

**Ships:** every one of the 122 concrete creature blueprints now carries a
`Level` inside its tier's band, and its `XPValue` equals
`ProgressionCurve.XPValueForLevel(Level)`. Creatures that deliberately give no
XP (filers, scribes, hosts, grazers…) carry Qud's `NoXP` tag and pay 0.
`CreatureLevelTests` pins both rules for every creature.

**Level choice:** each creature's old hand-set XP was inverted through the curve
and clamped into its tier's band, which keeps relative threat within a tier
(e.g. Glowmaw L3, Mosshulk L9, Vault Sentinel L10). One authored exception:
the Sand Wurm is L7, not L6, so it still outpays the Cave Bear
(`AlphaCombatStakesTests.Xp_RiskMatchesReward_AcrossTiers`).

**Bug found on the way:** stat entries default to `Max = 999`, so the three
upper-tier creatures (Pale Stalker, Sky-Sari, Obsidian Brute) silently paid 999
instead of 1,041–2,462. Their XP entries now declare `Max: 999999`; the
drift test would catch a recurrence.

**Revised pins (deliberate):** nine tests pinned the old hand-set XP numbers
(round-6 audit). Exact values became "XP follows Level", and the relationships
they protected are now asserted directly: bosses outpay their minions, a
warband leader outpays its chieftain, the tier-3 guardian outpays the Choir
tendril. `BossXP_MinionValuesUntouched` now pins that bosses get no special rule.

**Also fixed:** `DensityTravellerIntegrationContractTests.EntryHookPrecedes…`
anchored on `SaveGameService.QuickSave()`, renamed to `RequestQuickSave()`; the
order contract was still true but the test was failing on a missing anchor.
It now anchors on the current call and asserts its anchors exist.

**Bench after P2 (seed 64):** kill XP tier 1 3,213 (plan 1,140), tier 2 15,273
(11,004), tier 3 45,806 (43,332). Tiers 4–5 stay at 2,500 and 153 because they
still spawn tier-1/2 creatures; that is P4.

**Self-review:** 🟡 Brittle Hound (20 HP) now pays 311 and Sky-Sari (58 HP) 1,041:
XP follows tier while some stat blocks lag their tier. Fix in P4/P5 (stats to
band), noted rather than hidden. 🔵 Peaceful residents keep a small XP value if
killed (L2, 9 XP), as before.

## P3 — Scaled and companion kill XP (implemented 2026-10-30)

**Ships (Qud parity, `XRL.World.Parts/Experience.cs`):**

- `LevelingSystem.AwardKillXP` scales the award by the level-band gap between
  the recipient and the victim (`ProgressionCurve.ScaleKillXP`): one band above
  halves it, two a tenth, three or more nothing. A victim without a Level
  counts as its tier's lowest level; a `NoXP` victim pays nothing.
- **A companion's kill pays its leader.** `CombatSystem.HandleDeath` no longer
  gates the award on the killer being the player; `AwardKillXP` walks the
  killer's party-leader chain (at most 8 hops) to the player and scales at the
  player's level (Qud `partyLeader.AwardXP`, Experience.cs:80). Before this,
  companions' kills were simply lost XP.
- Diag: `progression/XPAwarded` and `progression/XPWithheld` with raw,
  awarded, both levels, `via` (kill/companion) and `reason`
  (full/outclassed/noxp). The `progression` and `concern` channels are now on
  by default (the P1 bench's records were silently dropped before).

**Tests:** `KillXPAwardTests` 12 (9 RED → GREEN; 3 counter-checks already held:
a stranger's kill and someone else's follower's kill pay nobody, a stronger
victim pays exactly its value). One of my own counter-checks was wrong (L14 vs
L1 is a two-band gap, a tenth) and was corrected to L15.

**Also fixed (pre-existing, found on the way):** four
`WorldMapObservabilityTests` failed on `HEAD`: `ContainerBuilder` installs the
generating factory into `ContainerPlacementService.Factory`, and a world with
no `Crate` blueprint logged an error per placement attempt. Placement now skips
blueprints the factory lacks, as `LootStocker` already does.

**Divergence from Qud:** Qud awards companions' leaders through events with
`PassedUpFrom`; CoO walks the chain directly. Same outcome, no new event type.

## P2b — Player growth per level (implemented 2026-10-30)

**Ships (Qud parity, `XRL.World.Parts/Leveler.cs`):**

- Health per level is `max(2 + Toughness modifier, 1)` (Qud `RollHP`:
  `max(roll 1-4 + modifier, 1)`). **Divergence:** CoO keeps its deterministic
  base of 2 instead of rolling, so no level-up can be re-rolled by reloading;
  at average Toughness the result is the old +2. The starting player
  (Toughness 18) now gains +3.
- An attribute point (`AP`, new Player stat) at levels 3, 9, 15…; +1 to every
  attribute at 6, 12, 18… (Leveler.cs:268-269).
- Raising Toughness, by point or by the level-6 bonus, adds or removes the
  modifier change once per level already taken, to maximum and current health
  (Qud's Toughness stat-change handler).
- `AttributeAdvancement.TrySpend` spends points; diag
  `progression/AttributeRaised|AttributeRefused`.
- **Where points are spent:** the skills screen gains an **Attributes** group
  (first, when the actor has an AP stat) with one row per attribute, its
  current value, modifier and purpose. Review/confirm uses the same flow as
  skills; the screen's unit label reads AP in this group and SP elsewhere.

**Tests:** `PlayerGrowthTests` 14 (RED by compile → GREEN). Counter-checks:
modifier 0 keeps +2, very low Toughness still gains 1, no AP at 2 or 6, no
bonus before 6, no health from a Toughness raise within a modifier step,
nothing spent without a point or on a non-attribute.

**Revised pins (deliberate):** `GameAuditMortalDeathTests.LevelUpKeeps…`
pinned +2 HP for the real player; it now asserts 2 + Toughness modifier.

**Also fixed (pre-existing, stale pins found in the regression):**
`SpreadRareEncounterLifecycleTests.UsedLunge…` pinned a 25-turn Lunge cooldown
that `bfa625f44` deliberately cut to 10 (the save restores it correctly; the
test now reads `LongBlades_Lunge.COOLDOWN`), and `ClaimedSuppliesTests.RealTaken…`
pinned exploration plan version 16 (now `SpreadExplorationPlan.CurrentVersion`, 17).
Still failing on `HEAD` and flagged separately:
`RealDeathSpillsSameGearAndPickedCapSurvives…` and two
`WorldLogHistoryInputTests` pointer tests (batch-mode hit-testing suspected).

**Self-review:** 🔵 Intelligence does not yet add skill points (Qud adds
(Int−10)×4 of 50 per level); CoO's 1-SP-per-power economy has no clean scale
for it. Deferred, noted.

## P4 — Tier-correct spawning (implemented 2026-10-30)

**Ships:**

- `ZoneTierPolicy.TierFor(zoneId)`: one tier per zone. Surface = authored cell
  tier; below ground +1 for floors 1–3 and +1 per three floors after; capped at
  5; 0 outside the world map.
- `ZoneTierFloor`: in a zone of tier T ≥ 3, a creature hostile to the player of
  tier < T−1 does not spawn. Applied as an admission predicate inside
  `PopulationTable.Roll` (refusal recorded as `tier_floor`, like world-flag
  refusals) and to structure-stamp occupants (`spawn:` markers): an outclassed
  bandit dugout stands empty. Wildlife, non-creatures and unknown blueprints
  always pass; tier 1–2 zones and non-world zones are never filtered.
- **Wildlife faction** (neutral, hidden): canon's ecology indicator species —
  cascade-father, glasspane frog, summit singer, yellowfoot wayfarer, wardline,
  grove lantern-moth, gin frog, brocchinia-sentinel — were hostile Beasts. Canon
  writes them as gentle ("where the water has gone wrong, they are the first
  thing missing"; "the Bower-Folk pay for these alive"; "some villages have kept
  one for six generations"). They no longer hunt the player, and are no longer
  counted as threats.
- Underground tables take the zone tier (`UndergroundTier(depth, …, zoneTier)`):
  shallow floors under deep places now draw their tier-3/4 bestiary instead of
  tier-1 marlbacks. All five underground call sites pass it.
- Tier-4/5 surface tables (`WithUpperTierThreats`) keep the tier-3 table and add
  canopy stranglers (forest biomes), ancient guardians, pale stalkers and, at
  tier 5, obsidian brutes. The Stump bands keep their ecology and gain the same
  threats by band and tier.

**Bench, seed 64, Urqu active (the state after Sill hears the sari):**

| Tier | Out-of-band hostiles before → after | Kill XP (scaled) before → after | Plan |
|---|---|---|---|
| 1 | 2 → 0 | 3,213 | 1,140 ✓ |
| 2 | 16 → 0 | 15,184 | 11,004 ✓ |
| 3 | 93 → 2 | 45,806 → 56,222 | 43,332 ✓ |
| 4 | 139 → 2 | 2,500 → 49,760 | 111,948 |
| 5 | 61 → 0 | 153 → 34,744 | 158,616 |

(Run f82a8cf6, after the stamp floor. The four remaining out-of-band hostiles —
a bandfrog and a mimic chest at tier 3, a desert prowler and an ambush bandit at
tier 4 — come from builders outside the population roll and the stamps; P5
audits them.) Tiers 4–5 now spawn the right creatures but too few: 57 and 22
hostiles across 80 and 26 zones. Density and a deeper upper bestiary are P5.

**Divergences and scope decisions:**

| Decision | Why |
|---|---|
| The Overwrit keeps `RuinsTier3` at tiers 4–5 (no upper threats) | Its sparse Unsaying circulation is authored and pinned (`OverwritCompositionTests`); its dangers are designed with the dead-regions work, not bolted on here. Left out of the tier-threat test on purpose. |
| `UndergroundTier(int, bool)` kept as its own overload beside `(int, bool, int zoneTier)` | `FieldMedicineSourceTests` reflects on the two-argument form as the explicit field-medicine opt-in. |
| The previous (3D) presentation's coverage pins excuse the P4 owners (`PreviousPresentationTierGap`) | Tier 4–5 chunks in Stump, Sanctum, the Founding Camp and floors under Ginmere now hold upper threats and the deeper underground bestiary, which have no 3D models. The 3D path draws them as glyphs; the flat 16×24 target is where their art is owed (P5). The list is exact, so any other unmodeled owner still fails. |

**Regression:** the full EditMode suite (34,496 cases) was compared case by case
with an export of HEAD on the same clone. P4-only failures were the
field-medicine overload, the Overwrit table name and the 3D coverage pins above;
all are fixed (234/234 on the affected classes). The remaining failures in that
run either fail identically at HEAD, read `ArtSource/` (not present in the
verification clone), or pass when their class runs alone (order-dependent, not
P4).

**Self-review:** 🟡 the upper creatures now placed in the Stump and Grovelands
(canopy strangler, ancient guardian, pale stalker, obsidian brute) lack native
16×24 art in those biomes and have no 3D models; graphics coverage follows in P5
(tracked by `PreviousPresentationTierGap`). 🔵 Sari-snakes
(tier 2) are filtered from tier-4+ Stump zones; P5 decides whether canon's
slope indicator should be re-tiered with stats to match.

## C1 — Concern substrate (implemented 2026-10-31)

**Ships:**

- `ConcernData` (Content/Data/Concerns/*.json): id, place, owner blueprint,
  faction, tier, title, pitch, thanks, requires, two or more approaches (kind,
  label, objective, triggers, outcome, optional offer), a spoken refusal and its
  reply, reward (XP, drams, effects) and next concerns.
- `ConcernRegistry.Compile`: a concern becomes an ordinary quest `concern.<id>`
  with a `work` stage of Optional objectives (the first approach finished
  advances it; its outcome runs and `concern.<id>.approach` records which,
  1-based) and a `report` stage waiting on `concern.<id>.reported`. The journal,
  objective ticks, closure ledger, quest events and save/load are the shipped
  quest engine's.
- `ConcernRegistry.Lint`: every shipped concern must name a real place, owner and
  tier; have two or more approaches with known kinds, labels, objectives and at
  least one trigger; use only registered predicates and actions; give an offer its
  text; and carry a spoken refusal with a reply. `EveryShippedConcernLintsClean`
  gates content.
- `PlaceSites` + predicates `IfPlaceSiteStage "Place:Site:Stage"` (at least that
  stage) and `IfPlaceSiteMethod "Place:Site:Method"`, readable on tick with no
  speaker (the conversation-only `IfSettlementSiteStage` needs one). Only exact
  enum names count.
- `ContractPlaceRepair "Place:Site:Drams"` and `RepairMethodId.Contracted`: the
  Concord does a stable repair for coin, leaves the condition
  `ConcordContract.<site>` and gains 5 standing. Checks the site and its state
  before charging; refunds if the settlement refuses the repair.
- Sill's well, oven and lantern are concerns on their real repair sites, each with
  a by-hand rebuild (Restore) and a Concord contract (Settle). A site already
  repaired before anyone asks is not raised (`Requires IfNotPlaceSiteStage`).

**Divergences from the plan:**

| Plan | Shipped | Why |
|---|---|---|
| A concern ledger part with its own save/load | None | All concern state is quest state plus facts, which `StoryletPart` and `NarrativeStatePart` already save; a second store could disagree with them. |
| Data-driven Restore sites for every place | Sill's three hard-coded sites only | C3 makes sites data-driven; C1 proves the substrate end to end on the sites that exist. |

## C2 — Concern conversation (implemented 2026-10-31)

**Ships:** `ConcernConversation.AppendChoices`, a dynamic choice provider called
from `ConversationManager.RefreshVisibleChoices`, on the authored start node only,
for a speaker standing in the concern's own place:

- The owner offers "[Is something wrong here?]" → the pitch, with each approach
  named → "[I'll see to it.]" (quest started, giver recorded) or the spoken
  refusal (quest started and ended Refused at once, so the ledger records an
  enacted no; the reply is spoken; refusal effects run).
- While open: "[About <title>...]" → a checklist of approaches, the owner's offers
  (e.g. pay the Concord), and "[I'm giving this up. I'm telling you so.]".
- Once an approach is finished: "[It's done: <title>.]" → the owner's thanks →
  quest completed, then XP, drams and effects paid.
- The place's Elder answers "[What troubles this place?]" with every open concern
  and whom to ask. No authored dialogue tree is edited; synthetic nodes are
  prefixed `concern:` and never stack more concern choices.
- Diag `concern/Offered|Accepted|Refused|Reported|OfferTaken`; required actions
  emit `event/ConversationActionApplied|Rejected` with the reason.
  `ConversationActions.ExecuteRequired` returns that reason to callers.

**Tests:** `ConcernSubstrateTests` (8), `ConcernConversationTests` (10),
`ConcernAdversarialTests` (40: hollow and duplicate files, predicate argument
shapes, place and speaker gates, double and wrong-state actions, contract funds
at the boundary, refusal and refund, approach exclusivity, diag, refusal
surviving a content reload). Regression: Concern, Settlement, Conversation,
Storylet, Quest and Repair classes, 1,525/1,528; the three failures are an
MCP-socket log in two setups and a pre-existing Posy appearance pin that fails
identically at HEAD.

**Adversarial findings (RED → GREEN):**

1. A well already repaired before anyone asked was still pitched as broken.
   Fixed with `Requires IfNotPlaceSiteStage` on all three Sill concerns.
2. `Enum.TryParse` accepted "3", "-1" and lowercase names as repair stages, so a
   content typo could pass a gate. Fixed: exact enum names only.
3. Contracting a well that was already sound answered "you can't afford it" when
   the player was short. Fixed: the site and its state are checked before price.

**Self-review:** 🟡 (fixed) the report paid its reward before completing the
quest; it now completes first and pays only on success. 🧪 The contract's refund
branch is defensive since the site pre-check: no current path reaches it, and no
test forces it. ⚪ TDD order: the C1 data model and registry were written before
their tests; the tests were then run RED by compile error and by the three
findings above.

## C3a — Supply, Carry and Read approaches (implemented 2026-10-31)

**Ships:**

- **Supply** (`Supply {Blueprint, Count}`): the owner's status node shows
  "(you carry k of N)" and, once the player carries enough, "[Hand over N …]".
  The hand-over takes exactly N carried units or nothing (stacks count each unit;
  equipped and floor items do not), then sets `concern.<id>.<approach>.done`.
  General `IfHaveItemCount "Blueprint:N"` and `TakeItemCount "Blueprint:N"` are
  registered for any dialogue.
- **Carry** (`Carry {Item, ItemName, Place, Recipient, Handover, Reply}`): the
  owner hands over one named sealed item (`SealedLetter` / `SealedParcel`, no
  trade value, `NoTrade`, `QuestItem`) tagged `ConcernCargo = "concern|approach"`,
  and writes another if it is lost. Only the named recipient blueprint standing
  in the named place offers the handover line; delivering takes the item, sets
  the done fact and speaks the reply. Another concern's cargo never counts.
- **Read** (`Read {Document, Place}`): while the concern is worked, the document's
  catalog blueprint stands in the place (`ConcernWorld.OnZoneEntered`, called on
  zone transition beside the travellers and at once on accepting in that place),
  with a stable id so re-entry and save/load keep one copy. The player's reading
  sets `document_read.<id>` (`ReadableDocumentPart.ReadFact`); a non-player's
  reading sets nothing.
- These kinds need no authored trigger: the compiler adds the done or read fact.
  Lint checks every supply blueprint and count, carry item, place, recipient and
  lines, and that each document and its blueprint exist.
- Diag `concern/SupplyHanded|CargoIssued|CargoDelivered|DocumentPlaced|DocumentNotPlaced`.

**Tests:** `ConcernApproachKindsTests` 22 (compile, lint, the three kinds through
real conversations, document placement and reading, and adversarial: double
hand-over, wrong recipients, settled-another-way, non-player reading, malformed
counts). Regression: concern, readable, inventory and trade classes 1,226/1,227
(MCP-socket log); content and coverage pins show only failures already present at
HEAD.

**Self-review:** 🟡 `SealedLetter` and `SealedParcel` have glyphs only; their
native 16×24 art joins the deferred small-item packet. 🔵 Read documents are
placed on the first open, unreserved cell ringing out from the zone centre, not
at an authored spot; places with a natural wall or board for them get authored
anchors in the content waves.

## C3b — Confront approaches (implemented 2026-10-31)

**Ships:** `Confront {Offset "dx,dy", Site, Foes [{Blueprint, Count}]}`.

- Undertaking the concern fixes the site (`ConcernWorld.ResolveSites`): the place
  plus the authored offset, or, if a place or point of interest stands there, the
  first open zone in the rings around it. Stored as a fact, so it survives
  save/load and content reloads; never re-resolved.
- The status node names the site ("the den two fields west of Sill").
- Foes are placed on the player's first real arrival at the site (the zone
  transition, before new creatures are registered with the scheduler), never on
  accepting, and never twice. They stand away from where the player came in.
  Each carries `ConcernFoe` and `AddFactWhenSlain` for the approach's slain count;
  the compiled trigger is "slain ≥ total". A foe that cannot be placed counts as
  gone, so the approach can never lock. No killer gate (the CoO/Qud
  `FinishObjectiveWhenSlain` convention): a neighbour's kill counts.
- Lint: the offset must be "dx,dy", non-zero and on the world map; the site needs
  a name; every foe must exist, have a count, and sit within the concern's tier
  ±1 (P4's tier rule, so a tier-1 concern cannot send the player at an obsidian
  brute).
- Diag `concern/SiteResolved|SiteNotResolved|FoesPlaced`.

**Tests:** `ConcernConfrontTests` 10 (site resolution and stepping aside from a
place, placement once and only on arrival, finishing on the last death, status
text, lint; adversarial: refused concerns place nothing, an unplaceable foe cannot
lock the approach, the site survives a content reload). Concern suite 90/90.

**Self-review:** ⚪ TDD order: the tests were written first but not run RED
before the implementation (a compile RED was certain). 🔵 The site's zone tier is
not checked at runtime when a site steps aside; lint checks the foes' tier, and a
one-ring step changes the zone tier by at most one band in the authored map.

## C3c — Data-driven Restore sites (implemented 2026-10-31)

**Ships:**

- `PlaceSiteCatalog` (Content/Data/PlaceSites.json): every place's working
  structures as data: place, stable id, spoken name, broken description, a
  lasting fix (materials, optional tool, text), an optional patch (materials,
  relapse turns, text) and a relapse line.
- The settlement manager tracks them as `RepairableSiteType.Structure` sites of
  the place's settlement, at any point of interest, beside Sill's hand-built
  well, oven and lantern (unchanged). A settlement saved before its structures
  were authored gains them on its next access, so content can grow under
  existing saves. Enum values are appended (saves store them as ints).
- `SettlementManager.RepairPlaceSite` and the action `RepairPlaceSite
  "Place:Site:Lasting|Patch"`: a lasting repair takes every material, needs the
  tool (kept) and is stable; a patch holds for its relapse turns
  (`RepairMethodId.PatchedRepair`) and relapses with its own line on the
  player's return (`AdvanceSettlement`). All or nothing; refusal reasons
  `missing_materials`, `missing_tool`, `already_repaired`, `already_patched`,
  `no_patch`, `unknown_site`. The Concord's `ContractPlaceRepair` works on these
  sites too.
- Concern approach `Restore {Site}`: the owner's status shows what the repair
  needs and offers "[Repair the …]" (and "[Patch the … for now.]") when the
  player carries it; the compiled trigger is the site at a stable repair not made
  by contract. A patch does not finish it.
- Lint: catalog sites need a real place, name, broken and relapse text, lasting
  materials, known materials and tools, and relapse turns for a patch; a Restore
  approach must name a site of its concern's place.

**Tests:** `PlaceSiteTests` 13 (tracking, reconcile under an old save, lasting
repair atomicity and tool, patch and relapse, Concord contract, five malformed
arguments, the Restore approach offering and finishing, a patch not finishing
it, lint). With Concern, Settlement and Repair classes: 769/769.

**Self-review:** 🔵 the catalog ships no sites yet; W1 authors them. 🔵 Data
sites are repaired in conversation with their owner rather than at a world
object: places have no generated object for a weir or a pump yet, and adding one
per place (with art) belongs with each place's content wave.

## C4 — Consequences (implemented 2026-10-31)

**Ships:** `PlaceConditionCatalog` (Content/Data/PlaceConditions.json) says what a
settlement condition does to its place while it holds, applied by
`PlaceConsequences`:

| Effect | How |
|---|---|
| Prices | `Price` multiplies what the place's traders charge and divides what they pay (`TradeSystem.GetBuyPrice/GetSellPrice`), for traders standing in the place only. Lint keeps it within 0.5–2. |
| Stock | `Stock [{Owner, Blueprint, Count}]`: on arrival the resident is topped up to the count, at most once per restock interval (int-property stamp, save-safe). |
| Residents' lines | `Remarks`: an ambient remark by a visible, non-hostile resident near the player, after `WorldRemarks` declines the turn (own chance, shared emission budget). |
| News elsewhere | `HeardIn` + `Heard`: residents of the listed places say it while the condition holds in its own place. |
| Arrivals | `Arrivals [{Blueprint, Count}]`: placed once on a real arrival, stable ids, before the scheduler registers the zone. |

Concern outcomes set conditions with `SetPlaceCondition "Place:Condition"`;
`ContractPlaceRepair` already sets `ConcordContract.<site>`. `TeachSkill
"SkillClass"` teaches a power free (refuses unknown or known skills). A concern
listed in another's `Next` is raised only once that one is completed (reported,
not merely worked).

**Sill content:** by hand, the well lowers Sill's prices (×0.97), keeps the well
keeper selling waterskins, and is talked about in Gantry and Tine. The oven keeps
the farmer selling toasted emberwheat, and the lantern keeps the warden selling
torches and is talked about in Tally. By contract, prices rise
(×1.04/×1.03/×1.02, the Concord's tithe), residents grumble about the bill or the
inspector, Gantry hears Sill now pays for its own water, and a Concord factor
comes to live in Sill after the well contract.

**Tests:** `PlaceConsequencesTests` 9 (setting conditions, prices with
counter-checks, stock top-up and its interval, arrivals once and only on
arrival, own and heard lines, a resident speaking, teaching once, the next
concern waiting for a report, shipped conditions lint clean). The Sill hand
rebuild sets its condition and not the contract's. Concern, place, settlement,
remark, ambience, Sari and conversation classes: 588/588.

**Self-review:** ⚪ TDD order: tests were written first but not run RED before
the implementation. 🔵 `PlaceAt` keeps its last answer because trade prices ask
it per row. 🔵 Trader-capacity bench tests (GameAuditTransfer*) fail in some
suite orderings and pass alone; they also failed in the P4 full run before C4
existed, so the order-dependence predates this work.

## W1a — Tier-1 content: Sill's arc, Gantry, Tine (implemented 2026-10-31)

**Ships (5 new concerns, 8 in all):**

| Concern | Owner | Approaches | Each leaves behind |
|---|---|---|---|
| `sill.tithe` The Bridge Money (after `sill.well`) | Elder | Carry the elder's letter to the Gantry registrar · Read the road-tithe roll at Gantry · Pay 30 drams | a new hand on the roll (prices ×0.98, Gantry hears) · Sill stops paying · Sill paid, still no bridge |
| `gantry.manifest` The Sixth Crate | Gantry registrar | Read the posted manifest · Countersign the exchange's count | caravans trust Gantry (Tent-Right +5, Concord −3, Wellmeet hears) · the exchange is grateful (prices ×0.95, Concord +5, Tent-Right −5, Wellmeet warns of short counts) |
| `gantry.cloth` The Torn Guest-Cloth | Tent-Right host | Restore the guest pole (cloak + oak stake) · Supply four starapples | shade under the cloth again (Tent-Right +5, Wellmeet hears) · the guest bowl filled, the cloth still down |
| `tine.weir` The Gapped Weir | Elder | Restore the weir (2 salvaged timber, 2 oak stakes, a hatchet; patch possible) · Concord contract 45 | perch sold again (merchant keeps 4), prices ×0.97, Sumphold and Sill hear · Concord catch-tax, prices ×1.04 |
| `tine.scribe` The Scribe's Last Copy | Scribe (retired Palimpsest) | Supply three ink vials · Read the Felling account at Quillhold | she finishes her copy, gap and all · a new margin, Quillhold hears; she gives her old plaque-wall copy (Codex05) |

New content: two place sites (Tine weir, Gantry guest pole), two place documents
(the posted manifest, the road-tithe roll), `LakePerch`, eleven place conditions,
and `PayDrams "N"` (all or nothing).

**Evidence:**

- `W1ContentTests` (8) plays each concern end to end through the real
  conversation, site, document, reputation and consequence systems: two different
  Gantrys from the manifest, the weir feeding Tine, the scribe helped from
  Quillhold, the tithe arc after the well, paying short.
- `PlaceCensusBenchTests` (explicit) on seed 64: every owner and Carry recipient
  lives in its place, and every Supply/Restore material is sold or lying in a
  place of the concern's tier or below. Salvaged timber is the thinnest source:
  one place on seed 64, and which place varies between runs.
- Voice gate: a blind fresh-context reviewer attributed 36 passages against the
  voice cards. 20 passed, 15 were weak and one failed: the registrar "striking"
  a roll entry broke the Recension taboo, since they do not correct old hands
  but enter theirs beside them. All 16 have been rewritten, the fail as "a fifth
  hand beside the four". Per the gate, rewrites are not re-graded; they go to
  the next wave's blind run.

**Self-review:** 🟡 fixed: heard news only ever told the first item when two
places' news reached the same place; it now rotates. 🔵 The census is one
seed; trader stock varies. 🔵 Two catalog pins (`DensityReadableDocument*`) were
revised to name the W1 place documents.

## W1b — Tier-1 content: Posy, Quillhold, Tally, Marrowstye, Sumphold, Slip (implemented 2026-10-31)

**Ships (6 new concerns, 14 in all, every tier-1 place but Morrowfast has work):**

| Concern | Owner | Approaches | Each leaves behind |
|---|---|---|---|
| `posy.composition` The Unfinished Composition | Elder (Bower-Folk) | Supply 2 pitchpod resin · Settle: be held in the composition for an evening | the piece sealed, Gantry hears (Bower-Folk +4) · people stand where you stood (Bower-Folk +8) |
| `quillhold.lamps` The Dark Copy-Desks | Scribe | Supply 3 lamp oil · Read the First Account's condition report | desks lit, the scribe sells ink, prices ×0.98 · the report's last line, Tine hears; paid in 3 ink |
| `tally.account` An Outstanding Account | Elder (Concord) | Carry the notice to the farmer at Sill · Settle 25 drams for her | Sill's farmer dunned · her debt cleared (Sill ×0.98, Concord +3); a spoken refusal is a formal default (Concord −3) |
| `marrowstye.wing` The Disused Wing | Curation intake filer | Read what the Catchers left (codex-08) · Carry the cards to the Salt-Vault indexer | the wing filed under Catchers, the Salt-Vault hears · the cards voided |
| `sumphold.cut` The East Cut | Peat cutter | Confront three bandfrogs in the cut east of town · Settle 30 drams for the Concord's pest man | the cut worked, prices ×0.97, Tine hears · the pest man's share, prices ×1.03 |
| `slip.east` East of the Well | Warden | Supply 3 lamp oil · Read the names on the east wall aloud | the east lamps lit · the names said, Sumphold hears |

Also: `SlipNameWall` and its document; twelve conditions. **Discoverability:**
the owner of an offerable concern carries the Available quest cue, and the owner
of one being worked carries the Active cue (`QuestCueStateQuery`, via
`ConcernConversation.CueFor`); a spoken no clears it.

**Evidence:** `W1ContentTests` 15 (seven new end-to-end plays: being held at
Posy, ink pay at Quillhold, Tally's formal default as a clean closure and its
notice reaching Sill, Marrowstye's cards voided at the Salt-Vault, Sumphold's
bandfrogs at their resolved site, Slip's wall read aloud). The cue test pairs
owner and non-owner, and accept and refuse. The census shows every owner and
recipient lives in its place, and every material has a source (pitchpod resin,
like salvaged timber, is sold in one place on seed 64). Concern, place, document
and quest-cue classes are all green.

**Voice gate, second blind run (57 passages: W1b and W1a's rewrites):** 38 pass,
15 weak, 2 fail. No card taboos were broken, and the reviewer judged the comedy
lines to land. The finding that matters: every pitch closed on an "Or…"
alternative and a "bring N of X" ask, so it read as a template. The two fails
were the peat cutter's "clear them out… east of town" (directions and a kill
template) and Tally's three-option menu. All 17 have been rewritten, with
alternatives woven into speech ("Failing oil…", "Lacking resin, there is still
the gap…", "Otherwise the Concord sends its pest man…"). W1a's rewrites passed
or improved. These rewrites wait for the next wave's blind run.

**Self-review:** 🔵 The flat 16×24 renderer drew no quest cue for any quest
(only the 3D presenters did). Fixed below (Flat quest cue). 🔵 Morrowfast's
residents already carry four authored quests; its concerns come with tier 3.

## Flat quest cue (implemented 2026-10-31)

Closes W1b's 🔵: the flat 16×24 renderer had no quest cue for any quest. A new
`FlatQuestCueLayer` (owned by `ZoneRenderer`, flat mode only, hidden while the
renderer is paused) evaluates the same `QuestCueStateQuery` as the 3D presenters
every 15 frames. Every visible creature that has something to ask gets a small
original "!" one pixel above its head (Available). One whose work is under way
gets a muted "..." (Active). Static resident sprites and animated actors are
marked alike. Sprites are pooled and sort above actor bodies. The art and its
editable source are in `ArtSource/Live16x24/quest-cue/make_quest_cue.py`, which
writes `Resources/Sprites/Actors/quest_cue_{available,active}.png`.

**Evidence:** `FlatPixelActorMotionTests.QuestCue_AConcernOwnerIsMarkedAvailableThenActiveAndANeighbourIsNot`.
It checks: no cue before the concern exists; the owner is marked Available and
the Villager next to them is not; the cue sorts above the body; accepting the
concern turns it Active; an unseen cell, a paused renderer and a spoken refusal
each clear it. The flat rendering classes are green.
**Cannot verify:** how it reads at normal zoom in real play. There is no live
capture yet, and the palette and placement are judged from the offline preview.

## Implementation log

- 2026-10-30 — design and plan written; census evidence gathered.
- 2026-10-30 — P1: `ProgressionCurve` + curve tests (13 green); explicit tier
  audit and duel bench with baseline above. Per-creature level tests move to P2.
- 2026-10-30 — P2: levels on 122 creatures, XP from level, NoXP tag, 999 clamp
  fixed, nine XP pins revised to relationships, stale traveller-hook anchor fixed.
- 2026-10-30 — P3: band-scaled kill XP, companion kills pay the leader,
  progression diag on by default; container placement skips unknown blueprints.
- 2026-10-30 — P2b: HP with Toughness, attribute points and level-6 bonuses,
  Attributes group on the skills screen; three stale pins fixed.
- 2026-10-30 — P4: zone tier policy, tier floor in rolls and stamps, Wildlife
  faction for indicator species, underground and tier-4/5 tables by zone tier.
- 2026-10-31 — P4 regression against a HEAD export: field-medicine overload
  restored, Overwrit kept sparse, 3D coverage pins excuse exactly the P4 owners.
- 2026-10-31 — C1/C2: concern substrate (compile to quests, lint, place-site
  predicates, Concord contract) and conversation (offer, spoken refusal, status,
  report, Elder); Sill's well, oven and lantern; 40-case adversarial sweep with
  three findings fixed.
- 2026-10-31 — C3a: Supply, Carry and Read approach kinds; counted item
  predicate/action; sealed cargo; placed documents; read facts.
- 2026-10-31 — C3b: Confront approaches (sites fixed on undertaking, foes placed
  on arrival, slain counts, tier-checked lint).
- 2026-10-31 — C3c: data-driven place sites (catalog, settlement tracking and
  reconcile, lasting repair and patch with relapse, Restore approach, lint).
- 2026-10-31 — C4: place conditions (prices, stock, remarks, news, arrivals),
  SetPlaceCondition, TeachSkill, next-concern gating; Sill's six outcomes.
- 2026-10-31 — W1a: Sill's bridge money, Gantry's manifest and guest-cloth,
  Tine's weir and scribe; place census bench; blind voice review and rewrites.
- 2026-10-31 — W1b: Posy, Quillhold, Tally, Marrowstye, Sumphold and Slip;
  concern quest cues; second blind voice run and rewrites.
- 2026-10-31 — flat quest cue layer: the 16×24 view marks quest givers and
  concern owners (Available "!", Active "...").
