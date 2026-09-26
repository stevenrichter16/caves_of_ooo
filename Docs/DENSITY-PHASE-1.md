# Density Phase 1 — reconnect and repair

> **Status:** tranche 1 ✅ shipped (2026-09-26). Tranche 2 planned (§8).
> **Plan source:** `Docs/QUD-DENSITY-GAP-ANALYSIS.md` §6 Phase 1.
> **Goal:** content that already exists reaches the player walking around
> the map, and a handful of blueprints stop quietly breaking their own
> promises. Everything here is data or a few switch cases on existing
> systems. There are no new mechanics.
> **Verification:** tests were run outside the editor with
> `Tools/EditModeRunner` (see §6 for what that does and does not prove).
> The Unity editor run is still owed before this is called verified.

---

## 0. User-visible invariants (tranche 1)

| # | Invariant | Tests |
|---|---|---|
| T1.1 | A gas grenade you find or buy can be thrown, and it bursts into gas where it lands. | `ShippedGrenadeThrowTests` |
| T1.2 | A lair's boss and its dormant ambusher belong to the country the lair is dug into. | `LairBossBiomeTests`, `LairPopulationBuilderAmbushTests` (CanonBiome*) |
| T1.3 | The maw-toad and bandfrog bite; the shambler lashes with a tendril. None of them punches with a 1d2 fist. | `DensityPhase1ContentTests` §T1.3, `GameAuditNaturalWeaponActivationAdversarialTests` |
| T1.4 | The item called "cudgel" works with Cudgel skills; the loaner longsword is a cutting long blade. | `DensityPhase1ContentTests` §T1.4, `WeaponAttributesContentTests` |
| T1.5 | Signposts, shrines, bookshelves and graveyards say something when examined. | `DensityPhase1ContentTests` §T1.5 |

## 1. Content readiness (Phase 1 as a whole)

| Item | Readiness | Note |
|---|---|---|
| Grenade `Handling` | 🟢 | Pure data. The throw pipeline (`ThrowItemCommand` → `GasGrenadePart.Detonate`) is already built and tested |
| Lair bosses / ambushers for live biomes | 🟢 | Bosses and ambushers are finished blueprints. Only the switch cases were missing |
| Natural weapons (3 creatures) | 🟢 | `DefaultBite` / `DefaultTendril` recipes already exist in `NaturalWeaponFactory` |
| Weapon-family attributes | 🟢 | Pure data |
| Examine prose for 4 fixtures | 🟢 | Pure data (`ExaminablePart.Description` alias) |
| Natural DV + DV stat on `Creature` | 🟡 | Real bug, but a test pins the current behavior. It needs its own sweep (tranche 2) |
| Stranded weapons/tonics into loot | 🟡 | Content placement. Must respect `Docs/LOOT-FINDS.md` tiers |
| Population density (Spread group, monocultures, depth table) | 🟡 | Tunes spawn rates, which changes the feel. Tranche 2 plus a numbers pass |
| Orphaned stamps, traps, hazard liquids | 🟡 | Placement into live-biome tables |
| BitLocker in normal play | 🔴 decision | A design call (tinkering availability), not a bug. Needs the user |
| `UrquActive` world flag | 🔴 decision | Who sets it is a lore/progression decision. Needs the user |
| One combustibility scale | ⚪ deferred | Changes fire spread everywhere. Needs a playtest, not just tests |

## 2. Verification sweep — corrections

| Premise (from the analysis or the plan) | Actual | Impact |
|---|---|---|
| "Natural weapons for MawToad and Shambler" | **Bandfrog** is also unarmed (fist 1d2) and sits in every Sodden tier and the Sodden lair guards | Added Bandfrog (`DefaultBite`) |
| "GlassScorpion's faction" is a bug | **Deliberate.** Its faction starts neutral by design: "Tier-3 tables treat them as ECOLOGY" (`Docs/BIOME-OVERHAUL-LOG.md:219`) | Dropped from Phase 1. Annotated in the analysis doc |
| Shambler should get `SporeTouch` (the spore shambler's weapon) | `SporeTouch` carries a `fungal-spores` gas emitter, which would give the plain Shambler a disease vector: a design change | Used `DefaultTendril` (1d3, Bludgeoning). `SporeShambler` untouched and pinned |
| Adding creature props is test-neutral | `GameAuditNaturalWeaponActivationAdversarialTests` pins the **exact** list of 40 armed blueprints | Updated to 43 plus `DefaultTendril` dice, in the same commit |
| The `Cudgel` item's attribute is an oversight nobody pinned | `WeaponAttributesContentTests.Cudgel_HasBludgeoningAttribute` pins `"Bludgeoning"`. It predates the weapon-attribute backfill, which only touched weapons with no attributes (`Docs/WEAPON-ATTRIBUTE-BACKFILL.md` §Verification) | Test renamed and updated to `"Bludgeoning Cudgel"` (Mace and Warhammer already use that string) |
| `"Slashing"` might be a recognized alias | Read by nothing: no resistance, skill, flag helper or effect (`grep -i slashing Assets/Scripts` → one prose comment) | LoanerLongsword → `"Cutting LongBlades"`. A content-wide test forbids the tag |
| Lairs could land in the Overwrit or the Stump | `WorldGenerator.PlacePOIs` skips both (`WorldGenerator.cs:122,128`) | Only four live cases needed: Spread, Sodden, Beating, Grovelands |
| Region tiers | From `WorldMapAuthoring.TierRows`: Spread T1 (all 142 cells), Sodden T2, Beating T2–3, Grovelands T3 (41 of 43) | Ambushers chosen tier-matched: AmbushBandit T1, SleepingTroll T2, CanopyStrangler T3 |
| A Snapjaws-faction troll among Beasts-faction Sodden guards will fight them | `Factions.json`: Snapjaws↔Beasts has no feeling, so they are neutral. Cave lairs already mix the two factions | No change needed |
| Grenades inherit a `Handling` part from `Item` | `Item` has only Physics + Stacker. Every tonic gets `Handling` from `TonicItem`; the grenades inherit `Item` directly | Handling added on each grenade (not on `Item`, which would make coins throwable; a counter-check pins this) |
| Adding `Handling` could change grenade weight | `HandlingService.GetWeight` prefers `Handling.Weight` only when > 0 | Weight stays 2 (Physics). Pinned |

## 3. Scope prune (tranche 1)

- **Signposts do not point anywhere yet.** The analysis suggested signpost
  text built from `RegionalGuidance` destinations. That is code (nearest
  place, direction), so tranche 1 ships static examine prose ("the arrows
  still point, but not at any names"), which is also a hook for the later
  version.
- **Lair bosses are per-biome, not per-tier.** Grovelands lairs (T3) get
  JungleStalker (T2). Tier-aware selection (e.g. AncientGuardian for T3
  lairs) needs a balance pass and is deferred (§5, 🔵 Finding 3).
- **No save migration.** See §5, 🔵 Finding 1.

## 4. Tranche 1 implementation

### T1.1 Grenades are throwable
`PoisonGasGrenade`, `SleepGasGrenade`, `StunGasGrenade` each gain
`{ "Name": "Handling", "Params": [{ "Key": "GripType", "Value": "OneHand" }] }`,
the same shape as `TonicItem`'s. `HandlingPart.Throwable` defaults to true.
**Why the existing suite missed it:** `GasGrenadePartTests` calls
`Detonate` directly, and `ThrowableTonicTests` hand-adds a `HandlingPart`
before throwing. Nothing threw a factory-built grenade.
**Qud reference:** grenades are thrown items in Qud [KNOWN]. This is a
CoO content repair, not a parity port.

### T1.2 Lair bosses and ambushers for the live biomes
`WorldGenerator.GetBossForBiome`: Spread/Sodden → SnapjawChieftain
(explicit), Beating → DesertProwler, Grovelands → JungleStalker.
`LairPopulationBuilder.PlaceAmbushers`: Spread/Beating → AmbushBandit 30%,
Sodden → SleepingTroll 25%, Grovelands → CanopyStrangler 25%. The rates
mirror the retired Desert/Cave cases. The new rolls draw from the same
shrinking cell pool (the M1.R-2 contract), which a new test pins in all
four live biomes. **Classification:** CoO-original.

### T1.3 Natural weapons
`MawToad`, `Bandfrog` → `DefaultBite` (1d3+1, Piercing Cutting Animal).
`Shambler` → `DefaultTendril` (1d3, Bludgeoning Animal). Authored as
`Props.NaturalWeapon`, the pattern used by Viper, Rotling and Scorpion,
materialized by `EntityFactory` onto both Hands (`EntityFactory.cs:498-512`).

### T1.4 Weapon-family attributes
`Cudgel`: `"Bludgeoning"` → `"Bludgeoning Cudgel"`. Slam, Ground Pound,
Backswing and Cudgel Expertise gate on the `Cudgel` tag via
`SkillCombatHelpers.FindEquippedWeaponOfClass` / `Attributes.Contains`.
`LoanerLongsword`: `"Slashing"` → `"Cutting LongBlades"`. Qud expresses
weapon family as the MeleeWeapon's skill [KNOWN]; CoO uses the attribute
string. Semantic match.

### T1.5 Examine prose
`Examinable.Description` added to Signpost, Shrine, Bookshelf and
Graveyard, in the voice of existing prose (MillStone, PaleSaltVein). Each
line was checked against `Lore/MYSTERY-LEDGER.md`: the shrine's god is
left unnamed ("depends on who you ask"). The gin frog stays silent (canon
gate 7), and a counter-check pins that `PhysicalObject` did not gain a
default description.

## 5. In-phase self-review (Methodology §5)

🔵 **Finding 1 — Existing saves keep the old blueprints.**
**File:** `Assets/Scripts/Gameplay/Save/SaveSystem.cs:1480` (parts are
rebuilt from the save stream). Grenades already in a pack stay
unthrowable, maw-toads already generated keep fists, and existing worlds'
lair POIs keep their chieftains (`PointOfInterest.BossBlueprint` is
saved). New spawns, shop restocks, unbuilt lair zones and new games all
get the fixes.
**Why 🔵:** pre-release, with no player saves in the wild. A migration per
content fix would accumulate. **If needed:** the precedent is
`Body.RestoreMissingDefaultEquipment` (`Body.cs:83-101`), a load-time
top-up.

🔵 **Finding 2 — Ambusher placement emits no diag record.**
**File:** `LairPopulationBuilder.cs` `PlaceAmbushers`. The Cave and Desert
paths never emitted one either, so "why is there no troll in this lair?"
cannot be answered by a query. **Deferred:** add a `worldgen`-category
`AmbusherRolled` record with `{biome, blueprint, roll, threshold}` when
Phase 1's population-table work (tranche 2) instruments spawning as a whole.

🔵 **Finding 3 — Grovelands lair bosses are a tier below the region.**
JungleStalker is T2 and Grovelands lairs are T3. That is still an upgrade on
every lair seating a T2 chieftain. Tier-aware boss choice needs numbers.

🧪 **Finding 4 — No dedicated `<Feature>AdversarialTests.cs` yet.**
Tranche 1 touches two taxonomy surfaces (probability boundaries: ambusher
rates; stacking: grenade stacks). The adversarial probes live inline:
10 foreign-ambusher exclusions, cell non-stacking in 4 biomes, stack-of-3,
weight pin, a content-wide "no inert Slashing" sweep, and a Boss-tag
sweep. The dedicated file is scheduled after tranche 2, when the
population-table work adds the larger surfaces (row parsing, `MinCount`
semantics).

### Cold-eye pass (Q1–Q4)
- **Q1 symmetry:** `GetBossForBiome` and `PlaceAmbushers` now cover the same
  four live biomes. `LairGuards` already did (Overwrit/Stump route to legacy
  rosters, but lairs never generate there).
- **Q2 consistency:** ambusher rates follow the retired cases (bandit 30%,
  others 25%). Every new Props entry follows the Viper shape. The JSON
  follows each block's local formatting (compact where the block is compact).
- **Q3 counter-checks:** every positive assertion has a counter-check:
  coin not throwable, dagger throw releases no gas, villager keeps fists,
  SporeShambler keeps SporeTouch, dagger is not a cudgel, gin frog silent,
  10 foreign-ambusher exclusions, bosses not all identical.
- **Q4 doc drift:** the analysis doc was corrected in place (Bandfrog,
  GlassScorpion) and points here.

## 6. Test evidence and honesty bounds

Run with `Tools/EditModeRunner` (Unity-free core plus UnityEngine stub,
NUnitLite, single-threaded, deterministic string-hash patch):

| Run | Result |
|---|---|
| RED: new tests vs. **old** production code | 23 fail, exactly the positive assertions listed in the commit. Every counter-check passes on the old code |
| GREEN: all 658 Unity-free test files, new code | 9,749 tests: 9,452 pass, 297 fail |
| Diff, old vs. new code, whole suite (`diff_results.py`) | **0 newly failing**; 23 newly passing (the intended set) |
| Determinism: two identical runs | 0 differing outcomes |

The 297 failures are environmental and identical before and after:
native scenario launchers and `CavesOfOoo.Editor.*` tools are not compiled,
art and voxel paths are missing, and Newtonsoft rejects a reference loop
that `JsonUtility` tolerates.

**Can verify (script-observable):** blueprint loading and inheritance,
factory materialization of natural weapons, throw pipeline and gas
spawning, lair population rates over 200 seeds, world-map lair bosses over
200 seeds, examine messages.
**Cannot verify here:** the 282 Unity-dependent test files; exact
Unity RNG per seed (zone seeds use a stable hash, not Mono's); how the new
bosses and ambushers *feel* in play (difficulty, pacing); Play-mode
rendering of the newly reachable bosses and ambushers (all five already
have sprite mappings, `EnvironmentSpriteRenderer.cs:151-170`).
**Owed:** `mcp__unity__run_tests mode=EditMode` in the editor, and a quick
Play-mode look at a Beating lair.

## 7. Implementation log

- 2026-09-26 — Sweep (§2). RED tests authored and compile-gated with a
  Roslyn diff against the whole tree. Data-level probe: 10/31 facts true
  pre-change, 31/31 post-change. Built `Tools/EditModeRunner` to execute
  the tests, then ran the RED run, the GREEN run and the full-suite diff
  (§6). A first full-suite diff showed 3 order-dependent failures; the cause
  was .NET's per-process string hashing feeding zone seeds. After the
  determinism patch, the diff is clean.

## 8. Tranche 2 (planned)

1. **Natural DV + DV stat** (`CombatSystem.GetDV` ignores natural armor DV
   when a Body exists). First read the pinning test
   `GameAuditEntityEquipmentAdversarialTests.cs:104-124` and decide whether
   the pin is deliberate.
2. **Stranded weapons and tonics into loot** via `LootTables.json`, validated
   by `LootTableRegistry.Validate`, respecting `Docs/LOOT-FINDS.md` tiers.
3. **Population:** a guaranteed encounter group in `SpreadTier1`; pick-a-group
   rows instead of `MinCount` monocultures; a pick-one depth table
   underground. Instrument spawning with diag records (closes Finding 2).
4. **Placement:** orphaned stamps (`LandmarkBuilder.cs:65-83`) into live
   biomes; traps into lairs and ruins; OilSlick/AcidPool into hazard tables.
5. **Decisions for the user:** BitLocker in normal play; `UrquActive`.
6. Dedicated `DensityPhase1AdversarialTests.cs` (closes Finding 4).

## 9. Files changed (tranche 1)

- MOD `Assets/Resources/Content/Blueprints/Objects.json`: 12 blueprints (surgical text edits; parsed-object diff verified to touch only these)
- MOD `Assets/Scripts/Gameplay/World/Generation/WorldGenerator.cs`: live-biome boss cases
- MOD `Assets/Scripts/Gameplay/World/Generation/Builders/LairPopulationBuilder.cs`: live-biome ambusher cases
- NEW `Assets/Tests/EditMode/Gameplay/Items/ShippedGrenadeThrowTests.cs` (+ .meta)
- NEW `Assets/Tests/EditMode/Gameplay/World/Generation/LairBossBiomeTests.cs` (+ .meta)
- NEW `Assets/Tests/EditMode/Gameplay/Biomes/DensityPhase1ContentTests.cs` (+ .meta)
- MOD `Assets/Tests/EditMode/Gameplay/World/Generation/LairPopulationBuilderAmbushTests.cs`: canon-biome section
- MOD `Assets/Tests/EditMode/Gameplay/Anatomy/GameAuditNaturalWeaponActivationAdversarialTests.cs`: 40 → 43 pinned recipes
- MOD `Assets/Tests/EditMode/Gameplay/Combat/WeaponAttributesContentTests.cs`: Cudgel pin updated
- NEW `Docs/DENSITY-PHASE-1.md` (this file)
