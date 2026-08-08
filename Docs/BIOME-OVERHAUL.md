# BIOME OVERHAUL — Regions, Structures, Loot & Reprieve

> **Status: PROPOSED — awaiting user sign-off. No implementation started.**
> Branch: `claude/game-lore-analysis-jqa7ur` · Drafted 2026-08-08
> Analysis basis: five parallel deep-explorations of worldgen, bestiary,
> structures, economy, and narrative systems (file:line citations below
> come from those sweeps; each load-bearing one gets re-verified in the
> pre-implementation sweep, §8).

---

## 0. Goal & vision

Turn the four biomes from *reskinned monster fields* into *regions* — each
with its own identity, landmarks worth walking toward, buildings with loot
inside, a resident faction you can talk to, dangers that feel native, and
places of reprieve where the world helps you back up.

The five-lens audit found the engine is dramatically ahead of the content:
locked chests, keys, traps, containers, ambush AI, gas/liquid hazards,
material reactions, faction reputation, follower AI, and ~150 nodes of
faction dialogue **all exist and all sit unreachable**. The overhaul is
therefore roughly ⅓ new systems (loot tables, structure stamps, rest,
harvest), ⅓ activation of dormant content, and ⅓ new themed content per
biome.

**RPG framing reminder** (`Docs/PROJECT-IDENTITY.md`): persistent world,
recoverable death, permanent commitments. Zones are fully serialized in
the save graph (`SaveSystem.cs:758-786` — all cached zones round-trip), so
looted-stays-looted holds and a container economy is safe.

---

## 1. Current-state audit (condensed)

### 1.1 What exists today

| Surface | State |
|---|---|
| Overworld | 20×20, noise-banded into Cave/Desert/Jungle/Ruins quartiles (`WorldGenerator.cs:40-49`); center pinned Cave + 4 cardinal set pieces |
| POIs | 5-7 Villages, 3-5 Lairs, 2-3 MerchantCamps (`WorldGenerator.cs:117-192`); `RiverChunk` never assigned |
| Tiers | Manhattan distance bands ≤4/≤8/>8 → 1/2/3, but **tier 3 tables == tier 2** (`PopulationTable.cs:61`) |
| Underground | Unbounded z; material strata sandstone→obsidian (`SolidEarthBuilder.cs:42-50`); only Snapjaw-family + Glowmaw spawn at ANY depth (`PopulationTable.cs:306-342`) |
| Biome zones | Terrain builder + uniform-random creature scatter. Zero structures, zero containers, zero loot besides 1-2 ground weapons |
| Villages | 3-5 procedural buildings, square, well/oven/lantern repair ladder (starting village only), grimoire chest (3 utility rites), quest pool (6), HouseDrama |
| Lairs | Boss chamber + 2-3 side rooms, biome boss, guards, 1-2 items from a 5-item pool, mimic/troll/bandit ambushers |
| Reprieve | **None.** No rest, no natural regen, no inn (Innkeeper is mute — `Innkeeper_1` conversation was never authored), shrine explicitly doesn't heal (`SanctuaryPart.cs:9-14`) |

### 1.2 The dormant-content goldmine (activation targets)

- **14 unreachable creatures**, incl. five faction envoys with full dialogue
  trees (PalimpsestEcho 42 nodes, ChoirTendril 37, GlassblownDrifter 25,
  PaleCurator 22, SaccharineEnvoy 21), five named RotChoir NPCs, CharredHusk,
  RuneCultist (+ its complete `AILayRune` trap-laying AI).
- **5 factions with zero world presence**: RotChoir, Palimpsest,
  SaccharineConcord, PaleCuration, GlassblownRemnant. `Factions.json` entries,
  reputation plumbing, price modifiers — all live; no spawner uses them.
- **13 grimoires with no source** (incl. the whole attack tier: Conflagration,
  RimeNova, Thunderclap, EmberVein, ArcBolt, IceLance, AcidSpray) — 1,275
  drams of authored content.
- **All 3 schematics** dead → 4 tinker mod recipes unlearnable.
- **3 minerals** (PaleSalt/GlowQuartz/ChoirIron) with recipes, enhancement
  classes, and a `WantsMineralPart` turn-in service — zero spawn sources.
- **LockedChest / LockedDoor / IronKey / Graveyard / 5 trap types** — fully
  implemented parts + blueprints, never placed by any builder.
- **Followers** — complete recruit/dismiss/follow/assist stack, gated to zero
  because no Persuasion skill tree JSON exists.
- **Weapons/armor with no source**: PlateArmor (only T3 armor!), VenomDagger,
  SeveranceEdge, PalimpsestBlade.
- **MerchantCamp POIs generate empty** — no merchant, indistinguishable from
  wilderness (`OverworldZoneManager.cs:57-59` + villager-only stock filter).

### 1.3 Systemic gaps the overhaul must fix

1. **No loot-table system** — every item placement is a hardcoded array.
2. **No structure system** outside villages/lairs — wilderness is furniture-free.
3. **No reprieve loop** — recovery is consumables + level-up full-heal only.
4. **Economy dead-ends** — Ink non-renewable; named NPCs can't buy (no
   wallets); corpses worthless (butchery absent); reagents purchase-only;
   drams have no sink beyond shopping.
5. **Difficulty curve flatlines** — tier 3 = tier 2, boss XP inversions
   (SnapjawChieftain: 40 HP for 15 XP), five hostiles punch with the default
   1d2 fist, underground scales count but never variety.

---

## 2. Cross-cutting foundations

These ship first; every biome pass consumes them.

### A1 — Loot-table system 🔴 (new system) — ✅ SHIPPED (see LOG §6)

`Assets/Resources/Content/Data/LootTables.json` + `LootTableRegistry` /
`LootTable.Roll(rng)` in `CavesOfOoo.Data`.

- Entry: `{ Blueprint | TableRef, Weight, MinCount, MaxCount, Chance }`.
  One level of `TableRef` nesting (e.g. `RuinsChestT2` → `GrimoireRare`).
  Load-time validation: unknown blueprint / unknown table-ref / cyclic ref
  → hard error (mirror `StoryletRegistry`'s posture, not the conversation
  system's fail-open).
- `ChestStockBuilder` helper: create `Chest`/`LockedChest`, fill from a named
  table, place at a cell. Used by lairs, structures, vaults.
- Retrofit: lair loot pool (`LairPopulationBuilder.cs:39`) and grimoire chest
  move to tables. This **absorbs ALPHA-READINESS P1 "frontier-rewards"**
  (the promised lair rare pool).
- Diag: `category=loot, kind=TableRolled` payload {table, results, zone}.

### A2 — Structure-stamp system 🔴 (new system)

Small hand-authored ASCII stamps placed into wilderness zones — the missing
middle between "empty biome" and "full village".

- `StructureStamp`: string rows + legend (char → blueprint or marker:
  `chest:TableName`, `spawn:Blueprint`, `npc:Blueprint:ConversationID`,
  `door-gap`, `campfire`). C# catalog first (JSON later if it earns it).
- `LandmarkBuilder : IZoneBuilder` (priority 3800, before population):
  per-biome catalog, 0-2 structures per non-POI wilderness zone
  (tier-gated), anchor search = the proven 80-attempt all-cells-passable
  scan from `StartingNeighborhoodBuilder.cs:201-256`.
- Stamps mark claimed cells so `PopulationBuilder` doesn't dump a CaveBear
  in the hermit's bedroom (pass a reserved-cell set through the pipeline).
- Diag: `category=worldgen, kind=StructurePlaced` {stamp, zone, anchor}.

### A3 — Harvest & butchery 🟡 (small system, big economy unlock) — ✅ SHIPPED (see LOG §6)

- `HarvestablePart { YieldBlueprint, YieldCount, YieldChance }` +
  "harvest" world action (via `GetInventoryActions`, same pattern as
  `SeedPart.cs:43`). One-shot: part removes itself after harvest.
- Per-creature corpse yields (new corpse blueprints where needed):

| Corpse of | Yields |
|---|---|
| Viper / GiantSpider / Scorpion | `VenomGland` ×1 (75%) |
| CaveBear / SandWurm / JungleApe | `RawMeat` ×2-3 |
| CaveSlime | `BogSap` ×1 |
| Glowmaw | `EmberFruit` ×1 (its lure-light, reflavored) |
| StoneGolem / ObsidianBrute | `GlowQuartz` ×1 (35%) |
| SkeletalSentry / CharredHusk | `Bone` ×1-2 |

- `MineralVein` solid entity (harvest → PaleSalt/GlowQuartz/ChoirIron by
  strata band) — placed underground + in Cave landmark stamps. Unlocks the
  three dead tinker infusion recipes and the `WantsMineralPart` service.
- `Bone` and `GoldCoin` get `CommercePart` (1 each) + `Stacker`; GoldCoin
  stacks become chest treasure.

### A4 — Rest & reprieve mechanics 🔴 (new system — the "points of reprieve" core)

- **Campfire rest**: "rest" world action on `CampfirePart`. Preconditions:
  no visible hostile (FOV check), not starving/burning. Effect: heal to
  full, cure Bleeding, advance the world clock `+60` turns (feeds relapse
  timers, crop growth, trader restock — the world moves while you rest),
  message + diag (`category=rest, kind=Rested` / `kind=RestBlocked` with
  reason). Counter-check: hostile in FOV → blocked, zero heal, zero clock.
- **Inn room** (drams sink #1): author `Innkeeper_1` — 10 drams → same
  rest effect + `WellRested` (+10 Speed, 100 turns). Innkeeper finally
  speaks; exists in every village already.
- **Shrine donation** (drams sink #2, small): donate 5 drams at a Shrine →
  minor blessing (+1 AV, 150 turns). Extends `SanctuaryPart` without
  breaking its flee-waypoint contract.
- **Hermits** (wilderness reprieve NPCs, placed by A2 stamps): heal/cure
  services via dialogue for drams, small trade stock, one rumor line
  pointing at the nearest lair/enclave. One hermit archetype per biome
  (§3), all with wallets so they can also buy.

### A5 — Spawn & difficulty repairs 🟢 (data + small code) — ✅ SHIPPED (see LOG §6)

- Real **tier-3 tables** for all four biomes (`GetBiomeTable` gains a
  `tier >= 3` branch) — tier-3 entries in §3.
- **Boss XP overrides**: SnapjawChieftain 15→75; audit the other three.
- **Natural weapons** for the five 1d2-fist hostiles: DesertBandit
  (`BanditBlade` 1d6), BrassHusk (`HuskFist` 1d6 + `Electrified,20,,3,1.0`),
  GlassScorpion (`GlassSting` 1d4 pen2), SporeShambler (`SporeTouch` 1d4 +
  spore gas via `EmitGasOnHitRaw`), RuneCultist (`CultistKnife` 1d4).
- **Underground band tables** (§3.5) — variety by strata, not just count.
- Optional guard: first-spawn placement ≥ 10 cells from the player's
  arrival edge in tier-1 zones (cheap fairness fix; verify feasibility).

### A6 — Economy plumbing 🟢 (data + wiring) — ✅ SHIPPED (see LOG §6)

- **Wallets** for Quartermaster (150), Scribe (100), Elder (120),
  WellKeeper/Farmer/Warden (80) — they already carry sellable stock; now
  they can buy, and `TraderRestockSystem` covers them.
- **InkVial** item (Commerce 10, use → +25 Ink): Scribe stock + loot
  tables. Rental economy becomes renewable.
- **Inheritance fixes**: CandyCarrot/Emberwheat → inherit `FoodItem`
  (regression pins on Food tag + category).
- **PlateArmor + dead uniques** enter circulation via §4.5 redistribution.
- Armor-slot ladder fill (4 new items, §4.3) so Head/Feet/Back/Hand have
  more than one item each.

---

## 3. Per-biome plans

Format per biome: identity → bestiary (tiers) → structures & loot →
reprieve → hazards → faction presence → quest hooks.

### 3.1 CAVE — "The Mossveil Reach" (start region)

**Identity:** damp karst meadows over an endless underworld; snapjaw
tribes, glowing fungus, old mine works. The tutorial biome — gentlest
tier 1, and the front door to the Strata.

**Bestiary:**
| Tier | Roster |
|---|---|
| 1 | Snapjaw, Scavenger, CaveBat, CaveSlime, Glowmaw (existing) |
| 2 | + CaveBear, SnapjawHunter (existing) |
| 3 (new table) | + **SnapjawWarlord** (new), **Mosshulk** (new), Glowmaw ×2 packs |

New creatures:
- **SnapjawWarlord** — HP 45, AV 4/DV 3, Spd 100, XP 90, `WarlordCleaver`
  2d5 pen2. Leads warband camps; drops its cleaver (real item, 1d10 pen2).
- **Mosshulk** — HP 55, AV 6/DV 0, Spd 70, XP 80, `MosshulkSlam` 2d5 pen2.
  Slow tank; harvest: `MendleafSprig` ×2.

**Structures (LandmarkBuilder catalog):**
- **Abandoned mine head** — boarded shaft: guaranteed `StairsDown`, 2
  supply crates (`Chest` ← `CaveSupplyT1`: torches, tonics, LampOil,
  GoldCoins), 1 MineralVein. The "dungeon entrance with a porch" stamp.
- **Snapjaw warband camp** (tier 2+) — 3 tents (wall stamps), totem,
  campfire, Warlord + 2-4 snapjaws, `LockedChest` ← `WarbandLootT2`
  (weapons table + IronKey carried by the Warlord).
- **Old watchtower** — 2-story footprint, SkeletalSentry ×1, chest ←
  `RuinsChestT1`.

**Reprieve:** **Mosskeeper's Hut** — hermit (Villagers faction, wallet 120):
heal-to-full for 8 drams, Antidote/BurnSalve stock, rumor line. Campfire
outside (restable).

**Hazards:** glowcap spore patches (existing gas system, low-magnitude
Poison gas puffs when stepped on), rubble fields.

**Faction presence:** Villagers (existing villages) — no new enclave;
this biome's "civilization" is the starting village + hermits.

**Quests:** *Warband Bounty* (kill the Warlord — village Warden posts it),
*Deep Delivery* (bring a crate to the depth-3 Pale Curation gallery —
teaches descent).

### 3.2 DESERT — "The Saccharine Barrens"

**Identity:** crystallized sugar-sand wastes; the Saccharine Concord's
caravans, the Glassblown Remnant's obelisks, bandits, buried worms and
tombs. Adventure-Time-candy meets glass-desert.

**Bestiary:**
| Tier | Roster |
|---|---|
| 1 | Snapjaw, Scorpion, DesertBandit (now armed — A5), **GlassScorpion** (activated) |
| 2 | + SandWurm, DesertProwler roams (existing) |
| 3 (new) | + **DuneLurker** (new), **BrittleHound** (new) packs, SandWurm ×2 |

New creatures:
- **DuneLurker** — HP 45, AV 5/DV 1, Spd 90, XP 85, `LurkerMaw` 2d6 pen2.
  Reuses `AIAmbush` (buried; wake-on-sight) — the desert's sleeping-troll.
- **BrittleHound** — HP 20, AV 1/DV 4, Spd 120, XP 35, `BrittleFangs` 1d6
  pen1 + `Bleeding,25,1d2,10,0`. Fast pack skirmisher.

**Structures:**
- **Caravanserai** (tier 1-2) — walled court, campfire, water trough
  (WaterPuddle), 2 Concord traders (wallets 200) + 1 guard; `Chest` ←
  `CaravanGoodsT1`. The desert's rest hub.
- **Sandstone tomb** (tier 2+) — 3-room stamp, SkeletalSentry ×2,
  `LockedChest` ← `TombVaultT2` (PlateArmor lives here at low weight,
  GoldCoin stacks, VenomDagger), key on a sentry or hidden in an urn
  (`Chest` reflavored, `Preposition:"in"`).
- **Glassblown obelisk** — GlassblownDrifter (activated, 25-node dialogue,
  wallet 80) + glass-glyph decor; sells GlassblownStiletto line; rep hooks
  already authored in `Factions.json` dialogue.
- **Bandit dugout** (tier 2+) — AmbushBandit ×2-3 + chest ← `BanditCacheT2`.

**Reprieve:** the **Caravanserai** (rest + trade + rumor) and the roaming
**Concord Envoy** (SaccharineEnvoy activated near desert villages).

**Hazards:** OilSeep patches (existing blueprint, first natural placement —
fire chains), collapsing sand pits (SpikeTrap reflavored "sinkhole", low
density, tier 2+).

**Faction presence:** **SaccharineConcord enclave** — desert villages
already get `Faction=SaccharineConcord` on the POI
(`WorldGenerator.cs:212-222`) but spawn generic Villagers; the enclave
pass makes one desert village per world an actual Concord settlement
(Envoy + candy-citizen reskins + Concord trade table). GlassblownRemnant
gets obelisk hermit-sites, not a village.

**Quests:** *The Sugar Toll* (Concord: clear the bandit dugout),
*Glass for the Glassblown* (bring 3 GlassScorpion husks → harvest yield —
to the Drifter; teaches butchery).

### 3.3 JUNGLE — "The Rotwood"

**Identity:** mutated overgrowth in the Muted Overgrowth palette; the Rot
Choir's fungal congregation, venom and spores, strangler canopy. Highest
attrition biome — poison pressure everywhere.

**Bestiary:**
| Tier | Roster |
|---|---|
| 1 | Snapjaw, SnapjawHunter, GiantSpider, Viper, **Rotling** (new swarm) |
| 2 | + JungleApe, Glowmaw, **SporeShambler** (activated, now armed) |
| 3 (new) | + **CanopyStrangler** (new), **ChoirTendril** (activated elite, 80 HP/70 XP) |

New creatures:
- **Rotling** — HP 8, AV 0/DV 2, Spd 110, XP 8, `RotlingClaw` 1d3 +
  `Poisoned,10,1d2,4,0`. Spawns 2-4; teaches poison economy early.
- **CanopyStrangler** — HP 40, AV 2/DV 4, Spd 105, XP 80, `StranglerLash`
  2d4 pen2. Glowmaw-pattern ceiling ambusher (reuse `GlowmawAmbushPart`
  generalized or `AIAmbush` + invisible-until-triggered).

**Structures:**
- **Overgrown ziggurat** (tier 2+) — the jungle's tomb: 3 stacked-room
  stamp, vine-choked, ChoirTendril or JungleApe guards, `LockedChest` ←
  `ZigguratVaultT2` (Sporeblade, FirstRootGlaive weight, ChoirIron,
  grimoire rare).
- **Rot Choir grove-shrine** — fungal clearing: Mogu/Grib/Nam/Sien/Sopp
  (activated, each with authored dialogue) around a spore-mother stamp;
  RotChoir trade table (toxin reagents, Antidote, CharredTonic).
- **Hunter's blind** — stilt hut: chest ← `HunterCacheT1` (DriedMeat,
  VenomDagger weight, arrows-someday), good early loot.
- **Mendleaf garden** — herbalist hermit + 4-6 harvestable Mendleaf plants.

**Reprieve:** **Rotwood Herbalist** (hermit: cures Poisoned free — the
biome's pressure valve — heals for drams) and the **grove-shrine** (rest
allowed if RotChoir rep ≥ 0 — first rep-gated reprieve).

**Hazards:** AcidPond clusters (existing), spore-cloud vents (gas emitter
prop, low-magnitude Poison gas on proximity).

**Faction presence:** **RotChoir enclave** — jungle villages' POI faction
is already RotChoir; one becomes a real Choir congregation (ChoirTendril
anchor + the five named NPCs + `ChoirTendril_1`'s 37-node tree goes live).

**Quests:** *Choir of Spores* (Choir: recover the SennaLedger-style relic
from a ziggurat), *The Strangler* (village: kill the CanopyStrangler that
took a villager — corpse prop at the site).

### 3.4 RUINS — "The Palimpsest Fields"

**Identity:** the pre-fall city, over-written and re-read; the Recension
(Palimpsest) archives what the ruins remember, the Rune Cult defaces it,
brass and char husks patrol dead plazas. The loot-dense, trap-dense biome.

**Bestiary:**
| Tier | Roster |
|---|---|
| 1 | RuinScavenger, SnapjawScavenger/Hunter, **BrassHusk** (activated, now armed) |
| 2 | + SkeletalSentry, **CharredHusk** (activated — the fire twin finds its home), **RuneCultist** (activated, lays real runes via `AILayRune`) |
| 3 (new) | + StoneGolem, **VaultSentinel** (new), **PalimpsestEcho** (activated elite) |

New creature:
- **VaultSentinel** — HP 60, AV 8/DV 0, Spd 60, XP 110, `SentinelHalberd`
  2d6 pen3. Stands before sealed vaults; doesn't wander (`Staying`).

**Structures:**
- **Collapsed library** (tier 1+) — book-room stamp: Palimpsest lectern,
  chest ← `LibraryShelfT1` (utility grimoires: KindleFlame, ChillDraft,
  Hearthwarm, DryingBreeze, ConjureWater, WardGleam circulate HERE),
  PalimpsestEcho chance at tier 2+.
- **Sealed vault** (tier 2+) — the game's treasure room: VaultSentinel +
  `LockedDoor` (first natural placement) + `LockedChest` ←
  `SealedVaultT3` (attack grimoires ArcBolt/IceLance/AcidSpray at low
  weight, Conflagration/RimeNova/Thunderclap/EmberVein at rare weight,
  PalimpsestBlade, SeveranceEdge, PlateArmor, GoldCoin stacks). Key held
  by the Sentinel. **This is where the 13 dead grimoires live.**
- **Clockwork workshop** (tier 2+) — BrassHusk ×2, chest ←
  `WorkshopCacheT2` (all 3 schematics circulate here, components, bits
  via a `BitPouch` mini-item if cheap, else components).
- **Rune-cult site** (tier 2+) — RuneCultist ×2-3 actively laying
  RuneOfFlame/Frost/Poison (their complete AI finally runs), defaced
  shrine, chest ← `CultCacheT2`.

**Reprieve:** **The Palimpsest Archive** — one per world (world-map
enclave): PaleCurator-style archivists (PalimpsestEcho anchor + authored
42-node tree), rest allowed, buys grimoires at premium, **sells
GrimoireCopy service context**, InkVial stock. The Recension is the
scholar-reprieve.

**Hazards:** live rune traps (from cult sites — first natural trap
placement), BrokenCapacitor arc clusters near water (reuse the Sparkwright
vocabulary), corridor SpikeTrap/TripWire at 5-8% in tier-2+ ruins zones
(`RuinsBuilder` addition, faction-filtered to hostiles-of-ruins so
RuinScavengers don't insta-die).

**Faction presence:** **Palimpsest enclave** (the Archive) + **Cultists**
finally exist as an enemy faction in the world (rune-cult sites).

**Quests:** *Marginalia* (Archive: recover a specific grimoire from a
sealed vault), *Unwriting the Cult* (destroy 3 rune-cult sites —
`AddFactWhenSlain` pattern).

### 3.5 UNDERGROUND — "The Strata"

**Identity:** depth becomes a real dimension instead of a snapjaw
treadmill. Each material band gets its own roster, veins, and one
landmark. The Pale Curation lives down here — curators of what the
surface lost.

**Band tables** (replaces flat `UndergroundTier`):
| Band | Depth (material) | Roster | Veins |
|---|---|---|---|
| A | 1-2 (Sandstone) | Snapjaw pack, CaveBat, CaveSlime | GlowQuartz (rare) |
| B | 3-5 (Limestone) | + CaveBear, Glowmaw, Rotling | PaleSalt |
| C | 6-8 (Shale/Slate) | SkeletalSentry, CharredHusk, **PaleStalker** (new) | PaleSalt + ChoirIron |
| D | 9-11 (Quartzite) | StoneGolem, PaleStalker packs, **ObsidianBrute** (new, rare) | ChoirIron + GlowQuartz |
| E | 12+ (Obsidian) | ObsidianBrute, ChoirTendril, VaultSentinel | all three, rich |

New creatures:
- **PaleStalker** — HP 50, AV 3/DV 5, Spd 115, XP 120, `StalkerTalon`
  2d5 pen2. The deep's fast hunter.
- **ObsidianBrute** — HP 80, AV 10/DV 0, Spd 55, XP 160, `ObsidianFist`
  3d6 pen3. The deep's wall.

**Structures (underground LandmarkBuilder, low density):**
- **Mine gallery** (band A-B) — timbered room, crates ← `CaveSupplyT1`,
  2-3 veins.
- **Pale Curation gallery** (band C, one per world at a deterministic
  depth-7 zone near the start column) — the underground reprieve:
  PaleCurator (activated, 22-node tree, wallet 150), rest allowed,
  buys minerals at premium (wires `WantsMineralPart`/`MineralTradeService`
  to a live NPC), sells Torch/LampOil/tonics.
- **Sealed reliquary** (band D+) — LockedChest ← `SealedVaultT3` +
  VaultSentinel. The deep-dive payoff.

**Soft cap:** content plateaus at band E; document that depth >15 repeats
band E (explicit, not a bug).

### 3.6 World map & rivers (stretch, Phase H) ⚪

- Render the existing fog-of-war bitmap (written+saved, never read).
- Paint the reserved legend rows on the world-map zone.
- Assign 2-3 `RiverChunk` POIs along a noise path; riverside stamp
  (fishing dock, ferry hermit). Only if earlier phases land clean.

---

## 4. New content inventory

### 4.1 New creature blueprints (8)
SnapjawWarlord · Mosshulk · DuneLurker · BrittleHound · Rotling ·
CanopyStrangler · VaultSentinel · PaleStalker · ObsidianBrute (9 with
the brute). All get XPValue, faction, natural weapon (factory case),
corpse config; ambushers reuse `AIAmbush`/`GlowmawAmbushPart`.

### 4.2 Activated existing creatures (14)
GlassScorpion, SporeShambler, BrassHusk, CharredHusk (tables) ·
ChoirTendril, PalimpsestEcho (tier-3 elites + enclave anchors) ·
RuneCultist (cult sites) · SaccharineEnvoy, PaleCurator,
GlassblownDrifter, Mogu, Grib, Nam, Sien, Sopp (enclaves/hermit-sites).

### 4.3 New item blueprints (~10)
- `WarlordCleaver` 1d10 pen2 (35) — Warlord drop
- `InkVial` (10) — Ink economy fix
- Armor ladder: `LeatherCap` Head AV1 (8) · `IronshodBoots` Feet AV2
  SP-5 (28) · `WardedCloak` Back DV2 (40) · `IronBuckler` Hand AV2/DV1 (45)
- `MineralVein` (harvest node), `SporeVent` (hazard prop), `Urn`
  (small container, MaxItems 3), `Tent`/`Totem` decor solids as needed
  by stamps (cheap RenderPart-only solids).

### 4.4 New structures (stamp catalog, ~15 stamps)
Cave: mine head, warband camp, watchtower, Mosskeeper's hut ·
Desert: caravanserai, tomb, obelisk, dugout · Jungle: ziggurat,
grove-shrine, hunter's blind, Mendleaf garden · Ruins: library, sealed
vault, workshop, cult site · Underground: mine gallery, Curation
gallery, reliquary.

### 4.5 Dead-content redistribution map

| Dead content | New source |
|---|---|
| 6 utility grimoires | `LibraryShelfT1` table + Archive stock |
| 7 attack grimoires | `SealedVaultT3` (weighted rare) + enclave premium stock |
| 3 schematics | `WorkshopCacheT2` + Tinker trade |
| PlateArmor | `TombVaultT2` (rare) / `SealedVaultT3` |
| VenomDagger / SeveranceEdge / PalimpsestBlade | tomb / vault tables |
| PaleSalt / GlowQuartz / ChoirIron | MineralVeins + golem harvest + Curation trade |
| IronKey / LockedChest / LockedDoor | tombs, vaults, warband camps (key on a guard or in an urn — always obtainable) |
| Traps ×5 | cult sites (runes), ruins corridors, desert sinkholes |
| Graveyard | village decor near Shrine (undertaker flavor, optional) |
| OilSlick / AcidPool | desert seeps / jungle ponds |
| Followers | **Persuasion skill tree JSON** (Recruit + Dismiss, cheap) — absorbs P1 followers-live |
| `PushNoFightGoal` | hermit/enclave dialogue de-escalation options |

---

## 5. Faction enclaves & quests

One enclave per stranded faction, each = existing dialogue + activation:

| Faction | Enclave | Where | Reprieve? |
|---|---|---|---|
| SaccharineConcord | Concord settlement (village variant) | Desert | inn-equivalent |
| GlassblownRemnant | Obelisk sites (hermit-scale) | Desert | rest only |
| RotChoir | Grove congregation | Jungle | rep-gated rest |
| Palimpsest | The Archive | Ruins | full (rest/trade/copy) |
| PaleCuration | Curation gallery | Underground band C | full |
| Cultists | Rune-cult sites (hostile — anti-enclave) | Ruins/Desert | no |

New quests (6, all 2-stage, existing storylet vocab): Warband Bounty ·
Deep Delivery · The Sugar Toll · Glass for the Glassblown · Choir of
Spores / The Strangler (jungle pair — pick per village) · Marginalia /
Unwriting the Cult (ruins pair). Rep rewards route through the now-live
factions; at least two quests use `IfReputationAtLeast` (currently
0 uses in content).

---

## 6. Phasing & sub-milestones

Each SM = one TDD commit (RED→GREEN→counter-check→self-review→living-doc
update). Adversarial sweeps close each phase per `ADVERSARIAL_TESTING.md`.

| Phase | SMs | Contents |
|---|---|---|
| **A — Foundations** | A1 loot tables · A2 structure stamps + LandmarkBuilder · A3 harvest/butchery + veins · A4 rest (campfire/inn/shrine) · A5 spawn repairs · A6 economy plumbing | the systems |
| **B — Reprieve network** | B1 merchant camps become real (per-biome camp stamps + traders with wallets) · B2 hermits ×4 + dialogue · B3 Persuasion tree + follower reachability | the "help" layer |
| **C — Cave pass** | C1 bestiary+t3 · C2 structures+loot · C3 quests | |
| **D — Desert pass** | D1-D3 same shape + Concord/Glassblown activation | |
| **E — Jungle pass** | E1-E3 + RotChoir activation | |
| **F — Ruins pass** | F1-F3 + Palimpsest/Cultists activation, traps live | |
| **G — Strata pass** | G1 band tables · G2 veins+galleries+Curation · G3 reliquaries | |
| **H — Stretch** ⚪ | fog-of-war render, map legend, river POIs | only on green backlog |

Estimate (agent-pace per project memory): A ≈ 6-8h, B ≈ 3-4h, C-G ≈ 3-4h
each, H ≈ 2-3h → ~25-35 agent-hours total; realistically 3-5 calendar
days with per-phase review checkpoints. Phases C-G are independent after
A+B and can be reordered or trimmed.

## 7. Performance section (required by CLAUDE.md)

- All new builders run at zone-generation time, not per-frame. Stamp
  placement is O(attempts × stamp-area) — bounded, one-shot.
- LootTableRegistry loads once at boot (same pattern as ConversationLoader);
  `Roll` allocates only result lists at gen time.
- Rest advances turns via the existing TurnManager loop — verify the +60
  advance batches without per-turn render (sweep item; if the turn loop
  renders per tick, add a batch-advance path).
- SporeVent/hazard props use the existing gas system; no new per-frame
  listeners. No new MonoBehaviours, no new Update paths.

## 8. Risks & pre-implementation verification sweep

Per methodology §1.2 — read before writing code; log corrections:

1. `ContainerPart` contents through `SaveGraphSerializer` — confirm
   items inside containers round-trip (zones do; verify nested entities).
2. Settlement-stage change evicts + regenerates the starting-village zone
   (`OverworldZoneManager.PrepareZoneForAccess:314-331`) — **would refill
   that zone's containers**. Mitigation: starting village keeps its
   hardcoded chest only, or loot stamps check a persistent
   `ZoneLootRolled` flag.
3. `TriggerOnStepPart` faction filter semantics — confirm traps can be
   made player-hostile without friendly-fire on ruins natives.
4. `GlowmawAmbushPart` generalizability for CanopyStrangler (or clone).
5. Turn-advance batching for rest (see §7).
6. `PopulationBuilder` reserved-cell handoff — pipeline currently has no
   claimed-cells channel; confirm cheapest seam (zone-level set vs
   builder-ordering convention like StartingNeighborhood uses today).
7. Village POI faction stamp (`WireNPC`) vs enclave rosters — enclave
   villages must not double-spawn the standard roster.
8. `WantsMineralPart` + `MineralTradeService` UI surface — service layer
   is complete but "surface wiring lands in E.4/E.5+"; confirm dialogue
   action is the cheapest surface.
9. All agent-cited file:lines spot-checked at implementation time.

## 9. Decisions requested (user sign-off)

1. **Rest model** — recommended: campfire rest = full heal + 60-turn
   world-clock advance, blocked when hostiles visible; inn adds a buff.
   Alternative: heal-over-turns (interruptible) — more sim, more code.
2. **Scope** — recommended: full plan (activate 14 + add 9 creatures,
   ~15 stamps, 6 quests). Trim option: skip Phase H and one biome pass.
3. **Enclave visibility** — recommended: enclaves are landmark structures
   inside existing biome/village zones (no new world-map POI types).
   Alternative: new world-map glyphs per enclave (more code, more legible).
4. **Phase order** — recommended A→B→C→D→E→F→G, review checkpoint after
   each phase.
