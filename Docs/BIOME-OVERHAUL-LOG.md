# BIOME OVERHAUL — Implementation Log & Handoff

> **Purpose:** running log of the biome-overhaul implementation so ANY
> model/agent (not just the one that started it) can pick up mid-stream.
> Read this file top-to-bottom, then the plan (`Docs/BIOME-OVERHAUL.md`),
> then continue from the first unchecked item in §3.
>
> **Update discipline:** every sub-milestone commit updates this file in
> the SAME commit (CLAUDE.md rule 3 — living docs over chat-buffer plans).

---

## 1. How to work in this repo (read once)

- **The plan** is `Docs/BIOME-OVERHAUL.md` — user signed off on ALL of it
  2026-08-08, including: rest = instant full-heal + 60-turn clock advance;
  full scope (9 new creatures, 14 activated, ~15 stamps, 6 quests);
  enclaves as in-zone landmarks (no new world-map POI types); phase order
  A→B→C→D→E→F→G(→H) with a user review checkpoint after each phase.
- **Methodology is non-optional** — `CLAUDE.md` at repo root. Short form:
  1. TDD: write the failing test FIRST, run it, confirm RED, then
     implement. Compile errors (CS0117/CS0103) count as RED.
  2. Counter-check every positive assertion (flag-flipped twin test).
  3. Self-review with 🔴/🟡/🔵/🧪/⚪ before each commit; fix 🟡+.
  4. Commit message template in CLAUDE.md §"Commit message template".
  5. Adversarial test sweep (`<Feature>AdversarialTests.cs`) closes each
     phase — see `ADVERSARIAL_TESTING.md` at repo root.
  6. Update THIS file + `Docs/BIOME-OVERHAUL.md` status in-commit.
- **Compile/test cycle** (Unity must be running; MCP server at
  `http://127.0.0.1:8080/mcp`):
  1. `mcp__UnityMCP__refresh_unity {compile: "request"}`
  2. `mcp__UnityMCP__read_console {types:["error"]}` must be empty —
     **also** grep Editor.log for "error CS" (read_console has missed
     compile errors before; `~/bin/unity_cycle.sh` exists for this)
  3. `mcp__UnityMCP__run_tests {mode:"EditMode"}` → returns job_id →
     poll `mcp__UnityMCP__get_test_job {job_id, wait_timeout:60}`
  4. Offline fallback (no Unity): `Scripts/verify_compile.sh` proves
     compile only, NOT behavior.
  - If MCP is dead: server relaunch = `cd /Users/steven/unity-mcp/Server
    && uv run mcp-for-unity --transport http` via Bash run_in_background;
    resilient driver at `~/bin/mcp_r.sh` (see project memory MEMORY.md).
- **Test suite baseline:** 5778 EditMode tests green as of commit
  `0acd6413`. Known flake (pre-existing, NOT ours):
  `FungalInfectionContagionTests.Contagion_HostInOwnSporeCloud_…` —
  order-dependent, passes in isolation. Do not chase it.
- **Editing `Objects.json` (22k lines): NEVER json.dump / reformat.**
  Use surgical edits (Edit tool with exact-match strings, or the
  regex-chunk python pattern from the alpha arc). Validate with
  `python3 -c "import json; json.load(open(...))"` after every edit.
- **Branch:** work on `claude/game-lore-analysis-jqa7ur` (current).
  Commit per sub-milestone with the CLAUDE.md template, co-author line:
  the model doing the work.

## 2. Architecture crib sheet (for models without prior context)

- Entity = bag of Parts (`Assets/Scripts/Core/Entity.cs`); blueprints in
  `Assets/Resources/Content/Blueprints/Objects.json` under top-level key
  `"Objects"`; `EntityFactory` sets public fields by reflection. `Props`
  array with `NaturalWeapon` key → `NaturalWeaponFactory.Create(name)`
  (a C# switch in `Assets/Scripts/Gameplay/Anatomy/NaturalWeaponFactory.cs`).
- Zone gen: `OverworldZoneManager.GetPipelineForZone` picks builders by
  POI/biome/depth (`Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:26-80`).
  Builders sort by Priority ascending; population at 4000, trade stock 4100.
- Spawn tables: hardcoded C# in `Assets/Scripts/Data/Tables/PopulationTable.cs`.
  `Roll` semantics: every entry spawns MinCount guaranteed; ONE extra roll
  at chance Weight/totalWeight adds rng.Next(1, Max-Min+1) more.
- Containers: `ContainerPart` (`Assets/Scripts/Gameplay/Items/ContainerPart.cs`)
  — the only container abstraction; open/unlock via world actions.
- World actions: parts answer `GetInventoryActions` →
  `WorldInteractionSystem.GatherActions` (`Assets/Scripts/Gameplay/Interaction/`).
- Diag observability: every gate emits a record (`Diag.cs`); tests pin
  emissions. Standard categories listed in `Diag.DefaultOnCategories`.
- Saves: all cached zones round-trip fully (`SaveSystem.cs:758-786`) —
  looted-stays-looted. CAVEAT: settlement-stage change EVICTS the starting
  village zone for regeneration (`OverworldZoneManager.PrepareZoneForAccess`)
  — anything randomly stocked there would re-roll (plan §8.2).
- Conversations: JSON in `Resources/Content/Conversations/`, actions/
  predicates registered in `ConversationActions.cs`/`ConversationPredicates.cs`.
  WARNING: unknown predicate names PASS silently (fail-open) — double-check
  spelling against the registry when authoring dialogue.
- Quests: storylets in `Resources/Content/Data/Storylets/`, validated at
  load, tracked by `StoryletPart`.

## 3. Status board

Legend: ☐ not started · ◐ in progress · ☑ done (commit hash) · ✖ dropped (reason)

### Phase A — Foundations
- ☑ **A5** spawn repairs: tier-3 tables ×4 biomes, boss XP fixes,
  natural weapons for the five 1d2-fist hostiles (see §6 entry)
- ☑ **A6** economy plumbing: NPC wallets, InkVial, food inheritance
  fixes, GoldCoin/Bone commerce (see §6 entry)
- ☐ **A1** loot-table system: registry + JSON + ChestStockBuilder +
  lair retrofit
- ☐ **A3** harvest/butchery: HarvestablePart, corpse yields, MineralVein
- ☐ **A4** rest: campfire rest action, Innkeeper_1 + inn room, shrine
  donation
- ☐ **A2** structure stamps: StructureStamp + catalog + LandmarkBuilder
  + reserved-cell handoff
- ☐ **A-close** adversarial sweep + cold-eye review + phase commit,
  then STOP for user checkpoint

(Order within phase = smallest blast radius first: A5→A6→A1→A3→A4→A2.)

### Phase B — Reprieve network (not started)
B1 merchant camps real · B2 hermits ×4 · B3 Persuasion tree/followers

### Phases C–G (not started)
C Cave · D Desert · E Jungle · F Ruins · G Strata — per-biome passes per
plan §3. Phase H (fog-of-war render, map legend, rivers) is stretch ⚪.

## 4. Decisions made along the way

| # | Decision | Why |
|---|---|---|
| 1 | (from sign-off) rest = full heal + 60-turn advance | user picked recommended option |
| 2 | (from sign-off) enclaves are in-zone landmarks | user picked recommended option |

## 5. Verification-sweep corrections

(Per-SM sweep findings vs the plan's assumptions get logged here — the
plan cites agent-report line numbers that MUST be re-verified before use.)

| SM | Claim checked | Verdict | Correction |
|---|---|---|---|
| A5 | `GetBiomeTable` only branches `tier >= 2` | ✅ confirmed | — |
| A5 | SnapjawChieftain inherits 15 XP, no override | ✅ confirmed | — |
| A5 | DesertBandit/BrassHusk/GlassScorpion/SporeShambler/RuneCultist have no NaturalWeapon prop | ✅ confirmed | `BanditBlade` factory case ALREADY existed — DesertBandit only needed the Props wiring |
| A5 | On-hit `Electrified` spec is valid | ✅ confirmed | ThunderHammer precedent: `Electrified,30,,3,1.0` |
| A5 | A spore gas exists for EmitGasOnHitRaw | ✅ confirmed | gas id `fungal-spores` (Content/Data/GasDefinitions); spec format `GasId,Chance[,CellDensity,AdjDensity,Level]`, defaults fill missing fields |
| A5 | GlassScorpion/SporeShambler/BrassHusk aggro like monsters | ❌ **false** | their factions (SaccharineConcord/RotChoir/Palimpsest) start at rep 0 → neutral-until-provoked. Tier-3 tables treat them as ECOLOGY; the hostile backbone is Beasts/Snapjaws-faction. Same behavior as the existing crossroads set pieces. |
| A6 | "WellKeeper/Farmer/… have no Drams wallet" | ❌ **partly false** | IntProps INHERIT through `BlueprintLoader.Bake` — WellKeeper/Farmer already carry Villager's 100. Only the four Creature-lineage NPCs (Quartermaster/Scribe/Elder/Warden) needed wallets. Pinned in `VillagerLineage_InheritsWallet_Pin`. |
| A6 | "GoldCoin/Bone need Stacker added" | ❌ **false** | the `Item` BASE blueprint already carries `Stacker` — every item inherits it. Only `Commerce` was missing. |

## 6. Per-SM implementation log

(One subsection per completed SM: what shipped, files, tests before→after,
divergences, self-review findings. Newest at top.)

### A6 — economy plumbing (2026-08-08)

**Shipped:**
1. Wallets: Quartermaster 150 / Scribe 100 / Elder 120 / Warden 80
   drams (IntProps). They can now BUY from the player and are covered
   by `TraderRestockSystem` (its is-a-trader test = "carries Drams").
2. `InkVial` blueprint (Commerce 10, stacks via inherited Item Stacker)
   + `InkVialPart` ("use" → `RentalSystem.AddInk(actor, 25)`, message,
   `trade/InkRefilled` diag, TonicPart-style stack consume). Sources:
   Scribe guaranteed ×2 per village (`ScribeStock`/`StockScribe`) +
   the general `TradeStockBuilder` pool.
3. GoldCoin/Bone gain `Commerce` 1 → tradeable (chest-treasure /
   butchery-yield ready for A1/A3).
4. CandyCarrot/Emberwheat now inherit `FoodItem` → Food tag + Food
   inventory category; own Food params survive the bake merge (pinned).

**Files:** NEW `Assets/Scripts/Gameplay/Items/InkVialPart.cs`,
`Assets/Tests/EditMode/Gameplay/Biomes/BiomeEconomyPlumbingTests.cs` (9);
MOD `Objects.json` (InkVial chunk + 4 IntProps blocks + 2 Commerce +
2 Inherits), `VillagePopulationBuilder.cs` (ScribeStock + StockScribe),
`TradeStockBuilder.cs` (pool + InkVial).

**Tests:** 5790 → 5799 (+9). All 5799 green (flake included this run).
RED evidence: CS0246 `InkVialPart`. ⚠ compressed step: the data-only
assertions (wallets/commerce/inheritance) never ran individually RED —
the compile error blocked the suite and the data edits landed in the
same compile window. Noted per §2.1.

**Self-review:** 🔵 RED compression noted above. 🔵 InkVial's 'u'
hotkey shares priority 20 with tonic actions — the action list handles
key assignment; no observed collision. No 🟡/🔴.

### A5 — spawn & difficulty repairs (2026-08-08)

**Shipped:**
1. Tier-3 tables (`CaveTier3/DesertTier3/JungleTier3/RuinsTier3`) +
   `tier >= 3` branch in `GetBiomeTable`. Backbone = hostile bruisers
   with `MinCount >= 1`; activated species (GlassScorpion, SporeShambler,
   CharredHusk, BrassHusk, ChoirTendril, PalimpsestEcho) enter the world
   as neutral-until-provoked ecology (see §5 correction).
2. Boss XP ladder: SnapjawChieftain 15→65 (new explicit stat),
   DesertProwler 45→65, JungleStalker 45→65, AncientGuardian 65→100.
3. Natural weapons: DesertBandit→BanditBlade (existing case, new Props);
   new factory cases `HuskFist` (1d6 pen1 + `Electrified,20,,3,1.0`),
   `GlassSting` (1d4 pen2), `SporeTouch` (1d4 + `fungal-spores,25` gas
   emit — `CreateWeapon` gained an `emitGasOnHitRaw` param),
   `CultistKnife` (1d4 pen1).

**Files:** MOD `PopulationTable.cs`, `NaturalWeaponFactory.cs`,
`Objects.json` (14-line surgical diff), `WorldMapTests.cs` (fixture);
NEW `Assets/Tests/EditMode/Gameplay/Biomes/BiomeSpawnRepairTests.cs` (12).

**Tests:** 5778 → 5790 (+12). RED evidence: 12× CS0117 (tier-3 methods
missing), then behavioral (`chieftain Expected 65 But was 15`, five ×
`Expected 1d6/1d4 But was 1d2`). Full suite green except the documented
pre-existing FungalInfectionContagion flake.

**Divergences:** boss XP ladder is 65/65/65/100, not the plan's literal
"Chieftain 15→75" — cross-boss consistency (three comparable tier-2
bosses at one value, tier-3 boss above non-boss ChoirTendril's 70).

**Self-review:** 🔵 SporeTouch gas spec relies on parser defaults for
density/level — deliberate, defaults verified in `EmitGasOnHitSpec.Parse`.
🔵 WorldMapTests' inline fixture must know every guaranteed-MinCount
species (see §7 gotcha). No 🟡/🔴 findings.

## 7. Gotchas discovered during implementation

- MCP server port 8080 answers 406 to bare GET when alive (that's healthy).
- **WorldMapTests has an inline minimal blueprint fixture** (`TestBlueprints`
  const, ~line 13). Any population-table entry with `MinCount >= 1` WILL
  spawn during its zone-gen tests; an unknown blueprint logs an
  EntityFactory error and NUnit fails the test as "Unhandled log message".
  When adding guaranteed table entries, add matching one-line stubs to
  that fixture (done for the 7 activated species in A5).
- Editor.log grep for "error CS" can show STALE errors from the previous
  compile cycle — the reliable green signal is the test job actually
  starting (a broken assembly refuses to run).
*(add as found)*
