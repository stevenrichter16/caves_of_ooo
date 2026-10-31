# Place Work and Progression — design and plan

Status: **design and plan, 2026-10-30. P1 (progression bench), P2 (creature levels) and P3 (scaled and companion kill XP) implemented;**
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

## Implementation log

- 2026-10-30 — design and plan written; census evidence gathered.
- 2026-10-30 — P1: `ProgressionCurve` + curve tests (13 green); explicit tier
  audit and duel bench with baseline above. Per-creature level tests move to P2.
- 2026-10-30 — P2: levels on 122 creatures, XP from level, NoXP tag, 999 clamp
  fixed, nine XP pins revised to relationships, stale traveller-hook anchor fixed.
- 2026-10-30 — P3: band-scaled kill XP, companion kills pay the leader,
  progression diag on by default; container placement skips unknown blueprints.
