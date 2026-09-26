# Walking-around density: Caves of Ooo (main) vs Caves of Qud

> **Status:** analysis, 2026-09-26. Phase 1 is being implemented; progress,
> the verification sweep's corrections to this document, and test results
> live in `Docs/DENSITY-PHASE-1.md`.
> **Question (user):** "I want Caves of Qud–level density of content and
> combat… mainly the content you interact with walking around the map.
> What's missing for full gameplay symmetry and parity?"
> **Code audited:** `main` at `8259eb67` (2026-09-19), via a detached
> worktree (the analysis was written on a lore branch whose `Assets/` was
> stale relative to main).
> **Method:** eight parallel audits (melee, ranged, interaction verbs,
> creatures/AI, items, per-zone census, simulation, social/lore), each
> reading main's code and content; then a measured pass over main's own
> native world census (`Docs/Verification/FactionPOI/FP01-native-census/
> native-census.json`, seed 141343545: 400 surface zones + 12 underground).
> Findings marked ✔ were re-verified by hand against source.
> **Limits:** nothing was run in Unity. The census is one seed and predates
> some runtime-spawned NPCs (Morrowfast's residents). The Qud decompile is
> not available here (the repo's `qud_decompiled_project` symlink points to
> a path on the author's machine), so Qud figures come from the repo's
> parity docs **[DOC]** or general Qud knowledge **[KNOWN]**, and are
> approximate unless tagged DOC.

---

## 1. Bottom line

The density gap is **mostly disconnected content and a handful of systemic
bugs, not missing content.** A large share of what already exists never
reaches a player: 18 of 41 hostile creature types never appeared in a full
generated world, 11 of 29 weapons can't be obtained, 17 of 26 liquids have
no source in the world, about 8 finished landmark set pieces sit in retired
biome tables, and the largest conversation trees are placed only in biomes
the authored map doesn't use. Reconnecting that is small, mostly data work,
and would roughly double the variety a player meets.

Beyond reconnection, three structural gaps separate a walk through Ooo from
a walk through Qud:

1. **Enemies ask one question.** No creature has a ranged attack, a spell,
   or any active ability, and the AI would skip adjacent abilities even if
   given them. Every fight is "it walks up and trades blows."
2. **The world barely responds to the player or to itself.** Tile fire and
   creature fire are separate models that never meet, a scale mismatch
   stops fire spreading through vegetation, 91 of 200 fixed features offer
   nothing but Examine, and there is no way to carry, pour, butcher or dig.
3. **The people and places are unreadable.** 176 wilderness NPCs share 12
   conversation trees, 77 villagers share one, signposts and shrines have no
   text, no NPC ever speaks unprompted, and nothing travels between zones.

Raw content *volume* (Qud has thousands of blueprints against Ooo's 477) is
real, but it is the last gap to close, not the first.

---

## 2. What a zone actually contains (measured)

From the native census, classifying every placed blueprint by the verbs it
supports. *Interactables* = talking NPCs + items on the ground +
harvestables + containers + stations (wells, fires, shrines, forges,
stairs). Hazard pools are counted separately because the census records
them per cell.

| Wilderness biome | Zones | Hostiles / zone | Zones with no hostile | Interactables / zone | Zones with a pool |
|---|---:|---:|---:|---:|---:|
| Spread | 132 | 0.2 | **86%** | 4.4 | 80% |
| Beating | 99 | 2.0 | 0% | 6.2 | 44% |
| Sodden | 59 | 1.5 | 0% | 4.6 | 97% |
| Grovelands | 36 | 2.8 | 0% | 9.8 | 89% |
| Overwrit | 23 | 0.2 | 96% | 0.7 | 4% |
| Stump | 20 | 0.8 | 65% | 9.7 | 85% |
| **All wilderness** | **369** | **≈1.2** | **≈40%** | **≈5.5** | — |

The Spread figure is exact, not sampling noise: its tier-1 table gives the
viper and snapjaw rows an 8% chance each (weight 2 of 25, minimum 0), so
0.92² ≈ 85% of zones get neither ✔ (`PopulationTable.cs:274-296`, roll
semantics `:68-95`). The Spread is a third of all wilderness.

**Content that ever appears in that 412-zone world:**

| Category | Defined | Appears anywhere | Never appears |
|---|---:|---:|---:|
| Hostile creature types | 41 | 23 | **18** |
| Talking NPC types | 46 | 38 | 8 |
| Liquid pool types | 13 | 7 | 6 |
| Harvestable types | 13 | 12 | 1 |
| Container types | 19 | 15 | 4 |

(Items are excluded from this table because they live mostly inside
containers and shops, which the census doesn't open; see §4.)

**Qud, for comparison [KNOWN, approximate]:** a surface zone usually holds
several creature groups (roughly 5–15 creatures) drawn from tier tables
with ranged and mutant members, several harvestable plant species, common
fresh/brackish/salt pools, scattered chests, statues and ruin remnants, and
a chance of a named legendary. Crossing the world map rolls a 10% travel
encounter per entry **[DOC `WORLD-MAP-UI-PLAN.md:66-74`]**. We don't have a
measured Qud census, so treat these as order-of-magnitude.

---

## 3. Content that exists but never reaches a player

| What | Evidence | Fix size |
|---|---|---|
| **14 hostile creatures that can never spawn** (census: 18 absent). Legacy-biome tables are never selected; lair bosses and ambushers only have legacy-biome cases; two snakes require a world flag nothing sets. | Every live biome falls through to `default: "SnapjawChieftain"` ✔ (`WorldGenerator.cs:184-194`); `LairPopulationBuilder.cs:97-110`; `RequiresWorldFlag = "UrquActive"` read in 4 rows and set nowhere ✔ (`PopulationTable.cs:157-189`) | S |
| **11 of 29 weapons unobtainable**, including 7 of the 8 with on-hit effects; 5 of 6 offensive throwing tonics orphaned | Appear only in `Scenarios/Custom/*Showcase.cs` or no loot/trade list | S |
| **17 of 26 liquids** have no in-world source; OilSlick, AcidPool, OilSeep, FireTrap never placed; 3 gases have no emitter | `HazardTerrainBuilder.cs` tables; `LiquidDefinitions/` | S |
| **~8 finished landmark stamps** (SealedVault, CollapsedLibrary, BanditDugout, GlassblownObelisk, Ziggurat, ClockworkWorkshop, RuneCultSite, PalimpsestArchive) | Stamp tables keyed to retired biomes (`LandmarkBuilder.cs:65-83`) | S |
| **5 trap blueprints** never placed by world generation | Only `Scenarios/Custom/*` | S |
| **Largest talkers unreachable**: PalimpsestEcho (46 nodes, ~3k words, one of the four ending voices), GlassblownDrifter (25 nodes) | Placed only by Ruins/Desert stamps (`LandmarkBuilder.cs:428-440`, `PopulationTable.cs:857`) | S |
| **Lore Codex (13 in-world texts, ~5k words)** almost entirely unplaced; 0 readable documents in the world | `Lore/Codex/` vs content search | S–M |
| **Tinkering** (bits, schematics, disassembly, mods) unusable in normal play | BitLocker granted only inside `if (DevMode.Enabled)` ✔ (`GameBootstrap.cs:318-322`) | S |
| **Gas grenades** found in 7 loot tables and 2 shops but can't be thrown | All three inherit `Item`, which has no `Handling` part, so `IsThrowable` returns false ✔ (`HandlingService.cs:39-46`) | S |
| Underground depth variety | 95%+ of spawns are snapjaws at every depth; deep creatures are weight 1–2 of ~30 | S |

---

## 4. Gaps by dimension (condensed from the eight audits)

Severity: 🔴 critical · 🟡 notable · 🔵 minor · ⚪ deliberate divergence.

### Encounters and combat

| Gap | Qud | Ooo main | Sev | Effort |
|---|---|---|---|---|
| Enemies with ranged attacks or active abilities | pervasive [KNOWN] | **0 of 104**; only the Player has `ActivatedAbilities` | 🔴 | M |
| AI uses adjacent abilities | yes [KNOWN] | `TryUseRangedAbility` skips `AdjacentCell` abilities and only runs when not adjacent (`AIHelpers.cs:446-485`, `KillGoal.cs:54-72`) | 🔴 | S |
| Evasive enemies | stat-driven DV | natural DV ignored for anything with a Body ✔ (`CombatSystem.cs:683-711`: Viper authored 5, SunStriker 6, never applied); the DV stat exists only on the Player, so Stun/Confuse/Hobble/Dodge don't change hit chance | 🔴 bug | S |
| Assist aggro, varied flee | faction-mates join [KNOWN] | only the damaged creature turns hostile; everyone flees at 25% | 🟡 | S |
| Distinct lair bosses | per-lair bosses, multi-level lairs [KNOWN] | one boss everywhere, one level | 🟡 | S–M |
| Enemies using terrain or items | throw grenades, use gear [KNOWN] | none | 🟡 | M |
| Shields that block | shield skill tree [KNOWN] | a buckler is hand armor only | 🟡 | M |
| Resolution core (penetration, crits, resistances) | — | matches Qud **[DOC `COMBAT-QUD-PARITY-PORT.md`]** | ✅ | — |

### Ranged and thrown

| Gap | Qud | Ooo main | Sev | Effort |
|---|---|---|---|---|
| Missile weapons, ammo, reload | ~50+ weapons, 3 loader types **[DOC]** | none | 🔴 or ⚪ | L |
| Throwing as a loop | thrown slot, stackable throwing weapons [DOC] | 5 inputs per throw, then walk to recover it | 🟡 | M |
| Line of fire | cover, path preview [DOC] | corpses and dropped items block throws (audit F12, open) | 🟡 | S |
| Spell aiming | — | 8 directions only (`AbilityTargetingMode.cs`) | 🔵 | M |
| Explosions | HE, EMP, chain detonation [DOC] | none | 🟡 | M |

Whether guns and bows belong in this setting is a design call; the doc
record treats them as deferred, not rejected. Spells are the working
ranged build today.

### Things to do with things (interaction verbs)

| Gap | Qud | Ooo main | Sev | Effort |
|---|---|---|---|---|
| Butcher / eat corpses | hundreds of corpse types [KNOWN] | 2 generic corpse types for 103 creatures; no butcher verb | 🔴 | M |
| Carry, pour, fill, drink liquids | core tool; water is currency [KNOWN] | wells and tonics only | 🔴 | M |
| Look-only scenery | rare | 91 of 200 fixed features | 🟡 | S (data) |
| Doors | everywhere | 5 scene doors in Morrowfast; locked doors only in an orphaned stamp | 🟡 | S |
| Sit, sleep, light a torch | yes | chairs/beds NPC-only; Torch can't be equipped or lit | 🟡 | S |
| Terrain powers | dig, phase, disintegrate, identify [KNOWN] | burn, freeze, shock, soak, build ice walls (~26 of 68 actives) | 🟡 | M–L |

### Simulation

| Gap | Qud | Ooo main | Sev | Effort |
|---|---|---|---|---|
| One fire model | one physics temperature [KNOWN] | tile fire and creature/object fire never exchange heat | 🔴 | M |
| Fire spreads through vegetation | yes | tile fire needs combustibility ≥ 50 ✔ (`TilePropagationSystem.cs:50`) but ~85 blueprints use a 0–1 scale; only 4 use 0–100 | 🔴 bug | S |
| Liquids met in the world | ~30, pools everywhere [KNOWN] | 26 defined, ~5–8 met | 🔴 | S |
| Clouds, light, wind | smoke blocks sight; darkness limits vision [KNOWN] | smoke and steam are inert; light is visual only; wind is plumbed but never set | 🟡 | S–M |
| AI avoids hazards | pathing cost for gas/liquids **[DOC]** | none | 🟡 | M |
| Status effects | ~150+ [KNOWN] | 37 | 🟡 | L |
| Farming | none | yes | ⚪ Ooo exceeds Qud | — |

### People, places and text

| Gap | Qud | Ooo main | Sev | Effort |
|---|---|---|---|---|
| Individual NPCs | generated names everywhere [KNOWN] | 21 identical "Saltwalker"s, 9 copies of one named Choir keeper; no name generator | 🔴 | M |
| Villager dialogue | per-village history and gossip [KNOWN] | 77 villagers share one 11-node tree that still mentions snapjaw raids and "old Mara" | 🔴 | S–M |
| Roaming encounters | travel encounters, caravans, pilgrims [KNOWN/DOC] | none; no goal moves an NPC between zones | 🟡 | M–L |
| Unprompted speech | ambient remarks [UNSURE on Qud] | none | 🟡 | S |
| Readable signs and books | signs, authored and generated books [KNOWN] | 64 signposts with no text ✔; shrines, bookshelves, graveyards with no text ✔; 0 readable documents | 🔴 | S |
| Examine text | ~universal [KNOWN] | 34% of placeable blueprints; items 10% | 🟡 | S (content) |
| Named legendaries | HeroMaker legendaries [KNOWN] | none | 🟡 | M |
| Generated history | sultan shrines, murals [KNOWN] | none | ⚪ likely deliberate (Mystery Ledger forbids confirming protected answers) | — |

### Items and loot

| Gap | Qud | Ooo main | Sev | Effort |
|---|---|---|---|---|
| Finds that change a build | roughly every few zones [UNSURE] | roughly every 15–25 zones; Grovelands, the region around the starting town, yields **no** weapons, armor or spell books from exploration | 🔴 | S–M |
| Loot scales with region | tiers 0–8 [DOC] | item tiers stop at 3; the tier-4 Stump produced zero weapons in 4,000 simulated zones | 🔴 | S–M |
| Random item mods on spawn | 3% per slot **[DOC]** | none; 3 enhancements have no source | 🟡 | M |
| Coins as filler | — | 70–85% of all pickups | 🟡 | S |
| Armor slots | ~9 [UNSURE] | 6 slots, 12 armor pieces | 🟡 | M |

---

## 5. Where Qud's density actually comes from

Counting blueprints undersells the real difference. A Qud zone is dense
because three multipliers stack:

1. **Enemies set the question.** The esper, the rifleman, the qudzu and the
   turret each demand a different answer, so the player's whole toolkit
   gets used. In Ooo the player has ~56 actives, tonics and terrain
   reactions, but nearly every enemy asks "walk up and trade blows," so
   most of that toolkit is optional.
2. **Verbs × objects.** Qud's objects mostly support several verbs (take,
   eat, butcher, pour, fill, read, open, light), and the world supplies the
   setups (oil pools, brush, water) without the player casting anything.
   In Ooo the combinations work, but almost always only when the player's
   own spell starts them.
3. **Every person is someone.** Generated names, per-village histories and
   travel encounters mean nothing repeats exactly. In Ooo the same few
   dialogue trees follow you everywhere.

Ooo already has the machinery for all three (an ability AI hook, a
material-reaction and tile-state engine, a conversation predicate system).
What's missing is mostly wiring, placement and content on top of it.

---

## 6. Recommended sequence

Ordered by player-felt density per unit of effort. Phases 1–2 are almost
entirely data and small code changes on existing systems.

### Phase 1 — Reconnect and repair (all S)
1. **Live-biome cases** for lair bosses and ambushers
   (`WorldGenerator.GetBossForBiome`, `LairPopulationBuilder.PlaceAmbushers`);
   fold the 14 legacy-biome hostiles into live tables; decide who sets
   `UrquActive` or stop gating on it.
2. **Population tables:** a guaranteed encounter group in `SpreadTier1`;
   replace `MinCount` monocultures (Bandfrog, Shambler, Rotling) with
   pick-a-group rows; move underground depth additions into a pick-one
   sub-table so depth brings new creatures.
3. **Place what's built:** the 11 weapons and 5 tonics into loot tables;
   the 8 stamps into live biomes' stamp lists; traps into lairs and ruins;
   OilSlick/AcidPool and the unsourced liquids into hazard tables; the
   Echo and Drifter into reachable stamps.
4. **Bug fixes:** `Handling` on the 3 grenades; natural DV plus a DV stat on
   `Creature` (and update the test that currently locks in the wrong
   behavior, `GameAuditEntityEquipmentAdversarialTests.cs:104-124`); one
   combustibility scale and TarSeep's volatility; natural weapons for
   MawToad, Bandfrog and Shambler; the Cudgel item's `Cudgel` tag; a
   BitLocker in normal play. *(Correction, Phase 1 sweep: GlassScorpion's
   neutral faction is deliberate — `Docs/BIOME-OVERHAUL-LOG.md:219` — and is
   not a bug.)*
5. **Give silent objects words:** signpost text using the existing
   regional-guidance destinations, shrine/bookshelf/graveyard descriptions,
   and a first pass of examine text for weapons, armor and tonics.

### Phase 2 — Enemies that ask questions (M)
- Let `KillGoal` use abilities (including adjacent ones) with a weighted
  pass before melee.
- Give 8–10 hostiles kits from the 17 shipped melee actives and the 31
  existing spell abilities, plus one ranged verb (a spit or thrown spear)
  for 3–4 creatures.
- Assist aggro for same-faction creatures in sight; per-creature flee
  thresholds.

### Phase 3 — A world that responds (M)
- Join the two fire models; make smoke block sight and steam scald.
- A liquid container with fill/pour/drink. This extends main's own
  `Docs/DAY-TO-DAY.md` D.3 waterskin; D.4 cooking fits here too.
- Butchering and per-family corpses, turning 103 creature types into loot
  decisions.
- Harvest or read verbs on the 91 look-only features; world-wide doors;
  sit/sleep and a light verb.

### Phase 4 — People and places (S–M, then M–L)
- The Codex as found documents seeded through library and tomb stamps.
- Per-culture name pools; one-per-world limits on fixed-name characters;
  villager dialogue that varies by place.
- A small ambient-remark system (it can carry main's planned *sari*
  ambience); a roaming-traveller roll on zone entry; epithets and
  descriptions for lair bosses.

### Phase 5 — Breadth toward Qud's volume (L)
- More creatures per biome and depth, a second cave layout and a depth
  grammar for the underground, multi-level lairs.
- Random item mods on spawn; tiered loot above tier 3; more armor slots.
- A legendary-creature generator; more status effects.
- Missile weapons, if the setting wants them.

---

## 7. Deliberate divergences (not gaps)

- **RPG, not roguelike:** no identification of unknown items, no
  permadeath economy, no weapon drops from kills (weapons come only from
  what enemies visibly carry).
- **Generated sultan-style history:** conflicts with the Mystery Ledger's
  rule that no text may confirm protected answers. Generated rumor within
  that rule is the safer analogue.
- **Overwrit's emptiness and the Stump's "sequence, not garrison":**
  authored intent. Overwrit is still worth one small reason to visit.
- **Farming and grimoires:** areas where Ooo already exceeds Qud.

---

## 8. Relationship to other plans on main

- `Docs/DAY-TO-DAY.md` (planned, empty implementation log): D.3 waterskin
  and D.4 cooking are Phase 3 items; D.2 (showing the day band) is
  independent and still the cheapest visible win.
- `Docs/LOOT-FINDS.md` (planned, not implemented): its tiered find tables
  are the natural home for Phase 1 item placement.
- `Docs/SYSTEMS-AUDIT-2026-08.md`: finding F12 (line of fire) is still open.
