# Density Phase 1 — reconnect and repair

> **Status:** tranches 1 and 2 ✅ implemented and verified (2026-09-26),
> including the native arrival follow-up (§10/T2.8).
> **Plan source:** `Docs/QUD-DENSITY-GAP-ANALYSIS.md` §6 Phase 1.
> **Goal:** content that already exists reaches the player walking around
> the map, and a handful of blueprints stop quietly breaking their own
> promises. The work reconnects content through existing combat, loot and generation
> systems; it does not add new creature or skill mechanics.
> **Verification:** standalone differential, real Unity EditMode tests and
> isolated Beating-lair Play-mode captures are recorded in §10/T2.7–T2.8.
> Final: 15,522 native tests pass; zero new standalone failures; 26 live assertions
> pass. Extended balance and two native graphics-console messages remain bounded below.

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
| Natural DV + DV stat on `Creature` | 🟢 | Natural/worn/stat DV compose once; obsolete pin corrected; effect apply/remove controls (T2.1) |
| Stranded weapons/tonics into loot | 🟢 | Eight weapons and four tonics gain tiered sources; special provenance preserved (T2.2) |
| Population density (Spread group, monocultures, depth table) | 🟢 | One eligible group, independent ambient rows, depth choices and roll diagnostics tested (T2.3); extended balance play remains separate |
| Orphaned stamps, traps, hazard liquids | 🟢 | Eight stamps, four existing traps, five pool blueprints and OilSeep reach live tables (T2.4); visual integration tested (T2.6) |
| BitLocker in normal play | ⚪ excluded | User explicitly keeps tinkering dev-only (2026-09-26) |
| `UrquActive` world flag | 🟢 | One-shot Sill omen activates it, endings clear it; future generation only (T2.3) |
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
**Closed in tranche 2 (T2.3).** The following records the original finding.
**File:** `LairPopulationBuilder.cs` `PlaceAmbushers`. The Cave and Desert
paths never emitted one either, so "why is there no troll in this lair?"
cannot be answered by a query. **Deferred:** add a `worldgen`-category
`AmbusherRolled` record with `{biome, blueprint, roll, threshold}` when
Phase 1's population-table work (tranche 2) instruments spawning as a whole.

🔵 **Finding 3 — Grovelands lair bosses are a tier below the region.**
JungleStalker is T2 and Grovelands lairs are T3. That is still an upgrade on
every lair seating a T2 chieftain. Tier-aware boss choice needs numbers.

🧪 **Finding 4 — No dedicated `<Feature>AdversarialTests.cs` yet.**
**Closed by `DensityPhase1AdversarialTests` (T2.5).** Original finding follows.
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

## 6. Historical tranche 1 test evidence and honesty bounds

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
**Originally owed:** an editor EditMode run and a quick Play-mode look at a
Beating lair. Both have now run; see §10/T2.7–T2.8 for results and remaining limits.

## 7. Implementation log

- 2026-09-26 — Sweep (§2). RED tests authored and compile-gated with a
  Roslyn diff against the whole tree. Data-level probe: 10/31 facts true
  pre-change, 31/31 post-change. Built `Tools/EditModeRunner` to execute
  the tests, then ran the RED run, the GREEN run and the full-suite diff
  (§6). A first full-suite diff showed 3 order-dependent failures; the cause
  was .NET's per-process string hashing feeding zone seeds. After the
  determinism patch, the diff is clean.

## 8. Tranche 2 (original plan; implementation in §10)

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
5. **Decisions:** user excludes BitLocker in normal play and delegates Urqu;
   the chosen omen/ending progression is recorded in §10/T2.3.
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

## 10. Tranche 2 — implementation plan and verification sweep

**Authorized scope:** repair dodge, reconnect existing loot and placement,
vary encounter groups and depth populations, instrument spawn rolls, choose
an Urqu activation rule, and add a dedicated adversarial suite. BitLocker
stays dev-only. The fire-scale change remains deferred for a playtest.
These are CoO content repairs, not claims of a Qud parity port.

| Sweep premise | Verified correction | Implementation consequence |
|---|---|---|
| The standalone runner can build from a clean checkout | Its `.csproj` was never tracked; `dotnet build` fails MSB1003 | Restore a portable, tracked project file before test evidence |
| Only enemies miss DV effects | `CombatSystem.GetDV` ignores the DV stat on Player too; Confused still reduces Agility indirectly | Add natural DV and the additive DV stat once, preserve worn penalties; check displayed DV |
| The existing natural-DV exclusion pin states a deliberate design | The equipment rollout documents worn penalties and natural AV, not natural DV suppression | Replace the obsolete assertion with explicit additive coverage |
| 11 weapons and 5 offensive tonics have no sources | Current source-less set is 8 weapons and 4 tonics; TemporalShard is an intentional Palimpsest gift | Preserve special provenance and add tiered sources only for the actual gaps |
| Loot find pools already exist | `LOOT-FINDS.md` is a plan; WeaponRack PickOne ignores Chance, and T3 contains crafted ForgedWeapon | Introduce scoped weapon/offense pools and remove crafted output from the rack |
| Add stamps to Overwrit/Stump aliases to reach players | Authored Overwrit omits landmarks; composed Stump replaces the Cave catalog | Preserve Overwrit emptiness; use Beating's pre-Felling ruins; see underground Ziggurat correction below |
| Five trap blueprints are available | Four are shipped: SpikeTrap, FireTrap, BearTrap, PressurePlate; TripWire is code only | Place the four existing traps, with safe entrance and occupancy checks |
| All 26 liquids can be placed through hazard tables | Only five unsourced pool blueprints exist; OilSeep is terrain, not a pool | Reconnect OilSlick, AcidPool, ConvalescencePool, MemoryBathPool, MirrorMucilagePool and OilSeep without inventing new pools |

**Milestones:** (1) runnable verification harness; (2) natural DV;
(3) tiered loot; (4) encounter groups, depth, Urqu and diagnostics;
(5) stamps/traps/liquids; (6) dedicated adversarial and full baseline diff,
Unity EditMode and Beating-lair Play-mode check where available.
Each production change gets a failing test before implementation and
counter-checks. The final review covers cross-system interactions and
documents the standalone runner's limits explicitly.

**Urqu decision (delegated by user):** the existing `SillHearsIt` inciting
omen owns activation indirectly: a one-shot storylet observes
`sill_sari_heard >= 1`, provided no ending is enacted, then sets
`UrquActive = 1`. A companion storylet clears it after an ending. This
keeps the snakes as signs of an active period, preserves the quiet-world
counter-case, and uses the shipped narrative dispatcher rather than
inventing the still-unbuilt Thinning clock or treating unfinished quests
as abandonment. Already-generated zones retain their population; new
Stump slopes/summit rolls respond to the fact. Explicit later quieting
does not retrigger the one-shot. This is a bounded progression choice,
not the full closure-ledger pressure simulation in the design §7.7.

**Population design:** named encounter groups select one eligible weighted
row and then its count, independently of ambient flora/fauna/loot rolls.
Spread T1 selects 1–2 vipers or snapjaws. Sodden and Grovelands T2/T3
replace compulsory species with one local predator/fauna group. Underground
selects one depth-appropriate group rather than guaranteeing three snapjaw
families; deeper bands include their own creatures at meaningful weight.
Optional ambient rows retain the existing independent-roll semantics.

**Placement follow-up sweep correction:** `GrovelandsFormationTests` explicitly
forbids Ziggurat as manufactured Choir culture. Preserve that authored
invariant: reconnect the eighth stamp through the live underground catalog
at tier 2 (depth 3+) instead. Beating receives the other seven. Merely
routing by retired-biome resemblance would have overridden a deliberate
worldbuilding choice.


### T2.0 Standalone runner repair

The handoff's runner omitted its project file because the repository ignores
`*.csproj`. A clean `dotnet build` reproduced MSB1003 before the fix. The
tracked project explicitly includes the core, existing stubs, four rendering
helpers, selected tests, and the optional new numeric density bench; its local
ignore exception prevents this omission recurring. It targets .NET 8, permits
major runtime roll-forward, and uses `COO_REPO` for isolated comparison copies.
This host executes under **.NET 10.0.5**, recorded with the evidence.

**Verification:** the reconstructed project builds; focused dodge 244/244,
loot 50/50, placement 128/128 and population/Urqu 28/28 have executed. Full
before/after and native verification follow below; these focused counts overlap
and must not be summed. The baseline comparison uses an isolated `git archive`
of `102ba71a` production scripts/content with the same current tests/assets,
rather than temporarily stashing a workspace in which agents are collaborating.
No user's working files are replaced during the comparison.

**Files:** `Tools/EditModeRunner/EditModeRunner.csproj`, its `.gitignore`,
`README.md`, and this living doc. **Review:** 🟡 missing-project defect fixed;
⚪ stubbed runner remains a pre-check, not a replacement for Unity.

### T2.1 Effective dodge and combat effects

`GetDV = 6 + agility modifier + natural Armor.DV + worn Armor.DV + DV stat`
for actors with a Body. Legacy actors without a Body keep their effective-armor
fallback; the additive stat works in both paths. The stat is an adjustment,
not a second base defense. `Creature` inherits a zero-valued DV stat with room
for negative shifts. Player already had it, but combat previously ignored it.
Inventory presents one computed defense entry instead of the raw modifier.
Stun, Confuse, Hobble, Paralysis, Berserk and Dodge now affect hit rolls; removal
restores defense and leaves other actors alone.

**Reference/classification:** CoO repair of the shipped ArmorPart and StatShifter
contracts, not a decompile-verified Qud port. The former equipment pin asserted
natural DV exclusion without a corresponding design rule. The change preserves
negative worn penalties and legacy equipment selection. Parsed before/after
`Objects.json` confirms **only Creature changed**, solely adding its DV stat;
no JSON reserialization occurred.

**Evidence:** `DensityPhase1DodgeTests` has 27 cases: 24 failed against old
production and 3 matched controls passed, then 27 passed. The focused combat,
equipment and display regression run passed 244/244. Two old skill fixtures
exposed by the full run were corrected: a passive-dispatch target now survives
the attacks instead of losing statuses at death; Rejoinder tests its existing
per-actor recursion guard with forced misses and 2/1/0 counters, including a
second independent attack. No Rejoinder gameplay logic changed. The three
affected skill/transfer suites pass 91/91 (overlapping, not additive evidence).
The separate transfer isolation repair is recorded in T2.5.

**Review:** 🟡 fixed ignored natural/stat DV and duplicate raw display entry.
🟡 corrected the two invalid fixture assumptions without weakening their
contracts. ⚪ old saves rebuild stats/parts from their stream: saved natural DV
is consumed by the code fix, but old NPCs are not retroactively granted a new
DV stat. New spawns inherit it; existing Player stats already support effects.
⚪ restoring intended defense changes combat difficulty; the numeric checks
prove the calculation, not balance or feel.

**Files:** `Objects.json`; `CombatSystem.cs`; `InventoryScreenData.cs`;
`AcrobaticsDodgePower.cs`; `ShortBlades_Rejoinder.cs` (comment only);
`GameAuditEquipmentContentBenchPlayer.cs`; `GameAuditMortalDeathBench.cs`;
`GameAuditEntityEquipmentAdversarialTests.cs`; `SkillActiveAbilityBehaviorTests.cs`;
`Wsp84SkillSystemAdversarialTests.cs`; new `DensityPhase1DodgeTests.cs` + `.meta`;
this living doc.

### T2.2 Tiered finds for stranded weapons and tonics

Six nested pools (`FindWeaponT1..3`, `FindOffenseT1..3`) reconnect the eight
source-less weapons and four offensive tonics verified in §10. They are
referenced by 21 container/death tables using the weapon/offense rates and tier
bands in `LOOT-FINDS.md`. The T3 weapon rack substitutes DissolutionMaul for
generic crafted ForgedWeapon. Registry validation finds no missing blueprint,
missing reference or cycle. Tests traverse nested sources and roll real loot.

**Preserved rules:** natural sacks/baskets/logs remain natural, humanoid death
loot gains consumables only, friendly loadouts stay unchanged, and rentals,
crafted output and TemporalShard's unique provenance stay outside general pools.
This implements the weapon/offense slice of the broader loot-finds plan; armor
pools, richer hostile loadouts and an economic balance pass are not claimed.

**Evidence:** 26 new loot cases, 25 failed against old production; focused
loot/regression run passes 50/50. Expected Commerce value per generated source
increased as expected: Crate T1/T2/T3 8.85/19.45/31.55 ->
15.683/34.558/56.356; StrongBox 22.3/48.45/92.85 ->
28.561/61.855/114.933; humanoid death 3.725/10.4/20.51 ->
7.698/15.289/26.260. These are table expectations, not observed game economy.

**Review:** 🟡 corrected stale stranded-item counts and crafted rack output.
⚪ Existing legacy rows remain alongside the additional scoped find rolls;
total item frequency is therefore higher than the new pool chance alone.
⚪ More finished items and sale value need play balance evaluation.
**Files:** `Assets/Resources/Content/Data/Loot/LootTables.json`, new
`Assets/Tests/EditMode/Gameplay/Biomes/DensityPhase1LootTests.cs` + `.meta`,
`Docs/LOOT-FINDS.md`, this living doc.

### T2.3 Encounter groups, Urqu signs and honest spawn diagnostics

`PopulationEntry.EncounterGroup` optionally names a weighted pick-one set.
Eligibility is filtered before normalization; one selected row rolls its
inclusive count. Independent ambient rows retain their own chance semantics,
without group weights diluting them. Invalid rows are excluded and large
weights sum in a long. Spread T1 always rolls 1–2 Vipers or Snapjaws; Sodden
T2/T3 varies Bandfrog/MawToad/Viper; Grovelands T2/T3 varies
Shambler/Rotling/Mosshulk. Independent flora is retained. This guarantees a
**table roll**, not placement when a zone has no safe room or rejects habitat.

Underground selects one depth encounter: shallow snapjaw families; then
CaveBear/Rotling, SkeletalSentry/CharredHusk/PaleStalker, and finally
StoneGolem/ObsidianBrute. Packs are capped at five; optional ambient rows remain.
The previous deeper-means-more-total-entities pin is replaced with a 200-seed
check that deeper tables vary the selected family instead of guaranteeing all
three snapjaw types. Quantity alone was the wrong density contract.

Urqu uses the exact one-shot omen/ending rule in §10. The storylets are untracked
and use the existing dispatcher; the activation is visible on the next tick
because trigger evaluation snapshots state. Tests cover quiet state, actual
omen dispatch, ending, explicit quieting, save/load and slope/summit spawning.
Ending prevents future indicator rolls; it does not despawn existing fauna.

**Diagnostics (closes tranche-1 Finding 2):** `PopulationRolled` reports each
row, including world-gated, invalid and unchosen rows. `rollKind` distinguishes
not_rolled/fixed_count/chance/weighted_choice. Only chance draws have a scalar
threshold; grouped rows report half-open selectionStart/selectionEnd intervals.
`AmbusherRolled` distinguishes count rolls (mimics, [0,3)) from probability
rolls, and records actual placement. A lair with no open cells emits
`LairPopulationRejected`. Disabled diagnostics preserve random draws and spawns.

**Full-sweep correction:** changed population draws exposed Stalagmite(74,17)
closing a 28-cell Cathedral chamber at seed 1729. Population placement now
rejects static scenery that separates formerly connected neighbors, ignores
mobile creatures in that topology, and emits `PopulationPlacementRejected`.
It consumes no replacement RNG and changes no Cathedral geometry. The original
fixed-seed reachability test remains intact. Cost is a bounded 80×25 scan/BFS
only for static blocking scenery during generation, not per-frame/per-turn.

**Evidence:** population/Urqu RED 25 failures + 3 controls -> 28/28 GREEN;
diagnostics RED 12 failures + 1 control -> 13/13 GREEN; reachability RED
4 failures + 7 controls -> 11/11 GREEN, with 93/93 related Cathedral/Grovelands
checks passing. Diagnostics compile in both Unity and the standalone runner;
the test reads scalar payload fields without adding an assembly dependency.

**Review:** 🟡 fixed ambiguous weighted-roll thresholds and missing blocked-lair
record. 🟡 fixed real late-scenery passage closure found by the full sweep.
⚪ spawn counts and difficulty remain tuning choices, not a measured density
parity claim. **Files:** `PopulationTable.cs`, `PopulationBuilder.cs`,
`LairPopulationBuilder.cs`, `UrquSigns.json` + `.meta`, new
`DensityPopulationTests`, `DensityUrquTests`, `DensityPopulationDiagnosticsTests`,
`DensityPopulationReachabilityTests` (each `.cs` + `.meta`), updated
`UndergroundGenerationTests.cs`, this living doc.

### T2.4 Live landmarks, sparse traps and liquid sources

Seven orphaned stamps join Beating's live ruin catalog: GlassblownObelisk,
BanditDugout, SealedVault, CollapsedLibrary, ClockworkWorkshop, RuneCultSite,
and PalimpsestArchive. Ziggurat joins underground tier 2 (depth 3+). Shared
stamp fields are initialized before the catalogs that reference them; no
static-init null entries are introduced. Echo and Drifter now have real
pipeline sources. Authored Overwrit emptiness and composed Stump catalogs
remain intact, and manufactured Ziggurats stay outside Choir country.

Ruin stamps use the four shipped trap blueprints. `TrapPlacementBuilder` runs
at priority 4150 after lair guards and containers, placing one or two visible
traps when safe cells exist. It excludes a three-cell edge margin, reserved
cells, stairs, liquids, state sources, creatures and other occupied cells.
Missing blueprints or no safe cells are diagnosed, not forced into unsafe
placement. Ruins use authored positions; lairs use safe available floor.

Hazard tables separate live countries: Beating adds OilSlick/OilSeep at weights
10/5 of 100, Sodden AcidPool at 10, Grovelands MirrorMucilagePool at 15.
Underground adds OilSlick, AcidPool, ConvalescencePool and MemoryBathPool at
5 each. This reconnects existing pool/terrain blueprints; it does not invent a
pool for every liquid definition or modify fire's combustibility scale.

**Full-sweep correction:** the expanded Beating stamp lottery exposed active
ConcordWaystations in the two abandoned counter zones. Those zones now exclude
only that staffed ambient stamp while keeping other ruins and their guaranteed
abandoned counter. A 24-seed test checks both abandoned sites against the live
Last Counter as a matched staffed control; the original failing pin is intact.

**Evidence:** 45 placement cases (36 failed against original production),
128/128 focused placement/formation tests pass. Abandoned-counter regression
and its new control failed 2/2 before the filter, then all 11 place-profile
cases passed. **Review:** 🟡 honored the deliberate Grovelands formation ban
instead of blindly mapping retired biomes; 🟡 kept abandoned counters unstaffed.
⚪ sparse placement and restored hazards still need sustained feel/balance play.
**Files:** `LandmarkBuilder.cs`, `HazardTerrainBuilder.cs`, new
`TrapPlacementBuilder.cs` + `.meta`, `OverworldZoneManager.cs`, new
`DensityPhase1PlacementTests.cs` + `.meta`, `PlaceProfileTests.cs`, this doc.

### T2.5 Dedicated adversarial suite and reusable native audit

`DensityPhase1AdversarialTests` contains 36 cases across nested loot replay,
cycle protection, tier clamping, capacity rejection, authored provenance,
actual on-hit effects, duplicate and stacked DV effects, removal order,
factory-instance isolation, save-graph round trips, grouped world-state gates,
null/invalid rows, extreme weight sums and diagnostic-toggle determinism.
These are **already-correct invariants after the tranche fixes**, not 36 new
RED-to-GREEN bug discoveries. The combined loot/adversarial run passes 62/62.
A first save helper omitted the body graph; fixing that test helper is not
reported as a gameplay defect.

The full sweep also exposed two pre-existing transfer-fixture failures:
merchant stock supplied by a previously wired `LoadoutPart.Factory` changed
actual weight, and ignored `AddObject` results left supposedly full inventories
empty. Capacity boundaries now include actual starting weight and assert
successful fixture insertion. A forced-factory probe failed both old tests,
then the three affected transfer/skill suites passed 91/91. Transfer gameplay
is unchanged.

The numeric `DensityPhase1Bench` measures 18 invariants on real factory entities
and detached generated zones. Its three smoke tests failed before the scenario
existed, then passed; they check fresh run IDs, preserved live position and
loud missing-content failure. The finite native launcher uses an owned temporary
save root and ordinary N / world-map / cardinal movement / descent input to a
real Beating lair. It temporarily guards player HP during travel, pauses NPC
scheduling only for the still image, and restores input, HP, scheduling, seed,
preferences and the preceding saved scene setup. It refuses dirty or untitled
scenes. `DensityPhase1NativeSummary` is emitted only after unexpected-error
accounting is finalized; incomplete teardown cannot claim success.

**Performance:** these audits are opt-in developer tools, not regular gameplay.
The native driver's waits and input steps are finite; it does not regenerate
zones or allocate inspection lists every frame. Generation topology work is
bounded as recorded in T2.3. No broad performance or frame-rate improvement is
claimed without a profiler session.

**Review:** 🟡 native compilation caught a Newtonsoft-only diagnostic test;
removed that dependency. 🟡 bench review fixed premature success reporting and
required actual boot-menu N input before claiming that step. Native keys,
rendering and cleanup have the separate editor evidence below; the numerical
scenario alone does not prove them. The first still image exposed a player/boss
overlap missed by the trap-only arrival assertion (T2.8).

**Files:** new `DensityPhase1AdversarialTests.cs`, `DensityPhase1Bench.cs`,
`DensityPhase1BenchPlayer.cs`, `DensityPhase1BenchBatch.cs`,
`DensityPhase1BenchTests.cs` (all with `.meta`); updated
`GameAuditTransferAdversarialTests.cs`; the analysis/loot/living docs and final
verification receipts. Rendering and travel-helper follow-ups are recorded
with the final integration results below.

### T2.6 Native integration follow-up — pre-implementation sweep

The first full Unity EditMode run completed 15,487 tests and reported 14
failures, all native-model coverage. The saved receipt is
`Docs/Verification/DensityPhase1/native-editmode-first.json`. Reconnected pools
exposed missing visual mappings; MarketStall was already generated by unchanged
village decoration at the baseline, but its existing mapping was restricted to
working-town sites. It is a latent presentation gap exposed by changed rolls.

| Native sweep premise | Verified correction | Bounded repair |
|---|---|---|
| Existing pool geometry has a matching color for every restored liquid | Stillleaf spring is a 24-vertex, 12-triangle flat mesh on palette swatch 123; acid/memory/mucilage lack matching palettes | Reuse that geometry in three separate asset variants with green/magenta/pale-cream swatches (the palette approximation for white mucilage); preserve the source mesh |
| A recipe-to-prefab mapping is sufficient | VoxelWorldPresentation also registers meshes for batching | Register the new and reused variants at load time; no per-frame cloning or gameplay changes |
| Every presenter's model definition needs edited data | Existing model catalogs can delegate to a small cached density library | Keep strict original catalog definitions intact; add three explicit identities |
| A zero missing-mesh count proves every owner rendered | Owners without a recipe can bypass that count | Assert owner-level authored/rendered coverage and active voxel presentation in the mixed batch test |
| All restored entities need invented voxel art | Traps and several stamp actors already retain their visible native glyph fallback | Preserve and test that fallback; do not claim new models for those entities |

Exact reused art is TarSeep for OilSlick/OilSeep, Stillleaf spring for
ConvalescencePool, and Cinderhold stall for MarketStall. New variants change only
pool palette coordinates and keep liquid identity, volume, ownership and gameplay
parts untouched. This is a scoped presentation completion discovered by real
Unity tests, not an expansion of the content or mechanics plan.

**Native rendering outcome:** 23 focused cases first exposed 10 failures; after
removing the vacuous batching assertion, RED was 13 failures with 10 valid
fallback controls. All 23 then passed, and the four previously failing rendering
suites brought the focused native sweep to **168/168 green**. No gameplay owner,
liquid volume, glyph, factory blueprint or RNG changed. Tests also hide/remove
owners and confirm no model remains. Atlas pixels are acid (32,194,54), memory
(174,127,172), mirror (231,223,172); the last is pale cream, an existing-palette
approximation of authored white. The original teal spring remains (103,169,173).
Binary mesh review confirmed only asset name/UV bytes differ from the source.

**Accessibility fixture correction:** the final standalone sweep exposed an
unseeded sundew roll in an old native-travel test. Its helper walked an entire
path without advancing turns, so a legitimate three-turn root looked like a
permanent blockage. The helper now advances ordinary turn boundaries and waits
at most ten turns for a movement-veto effect. Forced grab/miss controls exercise
a real generated sundew, leave the plant in place, restore the test RNG, and
preserve the original forest-to-Morrowfast destination assertions. RED was one
failure and one matched control; all 20 accessibility cases now pass. Native
compilation also required qualifying its RNG as `System.Random`. No gameplay
plant or movement logic changed.

**Review:** 🟡 closed missing native visual sources and the vacuous batching
probe; 🟡 corrected the travel fixture's missing turn cadence. Independent
cold-eye review found no remaining P0–P2 defects in the completed gameplay work.
BitLocker is still dev-only; GlassScorpion, plain Shambler and the exact
natural-weapon pin are unchanged. ⚪ fallback glyphs for unmapped trap/stamp art
remain deliberate and tested; this milestone does not claim voxel models for
all restored entities. ⚪ visual balance and sustained play remain separate.

**Rendering files:** `DensityPhase1VoxelLibrary.cs` + `.meta`;
`Assets/Resources/DensityPhase1Voxel3D/` library and three mesh/prefab variants
(with metadata); `SpawnRing3DCatalog.cs`, `SpawnRing3DLibrary.cs`,
`SpawnRing3DRecipes.cs`, `VoxelWorldPresentation.cs`;
`DensityPhase1RenderingTests.cs` + `.meta`. The accessibility change is confined
to `VoxelWorldAccessibilityTests.cs`.

### T2.7 Integration verification before the native arrival follow-up

The preserved pre-arrival standalone comparison is
`Docs/Verification/DensityPhase1/BeforeArrivalFix/standalone-final.json`, with both compressed
NUnit XML files, their hashes, excluded-file lists and the full outcome diff.
The isolated baseline is `102ba71a`; current tests/resources are shared, but
production scripts/content are independently archived. No working tree was
stashed or reverted during verification. Runtime is .NET 10.0.5.

| Standalone run | Selected files | Tests | Passed | Failed |
|---|---:|---:|---:|---:|
| Old production (`102ba71a`) with current tests | 666 | 9,931 | 9,478 | 453 |
| Updated production with current tests | 667 | 9,944 | 9,649 | 295 |

**Outcome diff: 0 newly failing, 158 newly passing.** The 13-case/file-selection
difference is `DensityPopulationDiagnosticsTests`, whose direct EncounterGroup
API is absent in old production; its individual RED run was recorded in T2.3.
All 295 remaining standalone failures already failed in the baseline. The runner
still uses stubs and a deterministic string hash unlike Unity; it cannot verify
native input, renderers, Mono serialization details, exact Unity-seed maps or
feel. The native rendering tests are among the 283 excluded current files.

Temporary focused XML files from the earlier session were cleared during disk
recovery. Their results were recorded contemporaneously above; the complete
regenerated final differential and native receipts are preserved in the repo.
The first full Unity run's 14 model-coverage failures are retained as RED evidence,
and the 168-case native rendering GREEN receipt is also retained.

**Native EditMode before the arrival follow-up:** Unity 6000.3.4f1 completed
**15,512/15,512 passing, zero failures or skips**, in 441.253 seconds. This is a
real editor run, including the tests that the standalone runner excludes.

**First finite Play audit:** run `c10c18672a9745c98dd84a0eb4c69b8c`, seed 1,
`Overworld.19.13.0`, 24 cardinal world-map steps. All **18 numeric and 8 native
assertions passed** in 7.198 seconds. The report, screenshot and independent
inspection/cleanup receipt are under `Docs/Verification/DensityPhase1/Native/`
in that run's directory. Pre/post checks confirmed Edit mode, a clean original
scene, original scene count, save-root override, seed, last-game preference hash
and background setting; isolation was inactive and the launcher exited zero.

**Can verify (script-observable):** real native bootstrap/input, new-game choice,
world-map travel/descent, factory DV and stun restoration, both Spread group
choices, lair boss/traps and finite cleanup. The first arrival assertion tested
absence of traps only; it did not establish absence of another creature.

**Visual inspection:** the 1920×1080 still shows rendered walls, sand, player,
actor glyphs, chest and HUD, with no blank frame or pink missing-material surface.
The HUD also explicitly lists the player and desert prowler in the same cell.
That is a real arrival defect and is addressed in T2.8; the passing numeric
report is retained rather than relabeled as proof of safe creature occupancy.

**Cannot verify (visual / feel):** a still cannot prove combat AI, animation,
difficulty, balance, every restored model or long-play pacing. NPC scheduling is
paused only for capture, and the visible high HP is a temporary travel guard.
The fire combustibility scale and tier-aware lair-boss balance remain deferred.

**Console limit:** the audit's callback counted zero unexpected errors, but the
editor console/log contained two native backend errors during capture:
`Ignoring depth surface load action as it is memoryless` and
`Ignoring depth surface store action as it is memoryless`. Those messages bypassed
the callback and are preserved in the inspection receipt. No project code sets
memoryless attachments; the new models do not create render targets or passes.
Screenshot/backend handling is a hypothesis, not an established cause. No claim
of an error-free Play console is made; a controlled capture-on/off backend probe
is deferred rather than changing shared rendering without a reproducer.

### T2.8 Native arrival overlap — verification sweep and repair

The Play screenshot exposed a pre-existing mismatch: `WorldMapTraversal.Descend`
accepts `Cell.IsPassable()`, which checks Solid tags and archive barriers, while
the lair boss has `Physics.Solid = true`. `LairBuilder` places that boss at the
same center cell used for a first world-map arrival. Normal movement blocks
that occupancy, and `KillGoal` attacks at distance one, so distance-zero overlap
is not a deliberate combat arrangement. The bounded repair must honor actual
occupancy for both the preferred cell and fallback search, preserve an empty
preferred cell, and leave the player on the world map if no arrival exists.

The repair calls `Zone.CanPlaceFootprint(player, x, y)` before leaving the map,
both for the preferred position and every fallback candidate. This existing
gate covers Solid tags, Physics solidity, closed archive barriers, authored
blocking owners and the entire body, so a multi-cell player cannot become
detached because its anchor was clear but an edge was blocked. The original
radius/order, generation reservations, nonblocking liquids and loose items
retain their existing policy. No available landing means failure with the
player still on the world map; no creature is relocated.

**Evidence:** ten native regression cases first produced **six intended failures
and four passing controls**, then **10/10 passed** after the fix. Two attempted
related-suite jobs did not initialize (zero tests executed), so their transport
receipts are retained separately and do not count as GREEN or assertion failures.
A clean native fixture dispatch completed; the complete suite and real lair
replay are repeated below with the updated code.

**Review:** 🟡 closed the observed boss/player overlap and the multi-cell
detachment case. 🟡 the old `player_arrival_is_safe` assertion was trap-only;
`player_arrival_clear_and_trap_free` now also excludes other blocking occupants.
The report explicitly bounds callback error accounting instead of implying
that zero callback errors proves an empty native console. Empty/occupied saved
locations, real boss/empty center, first-candidate blocking, saturated target,
whole-body occupancy, reservation and nonblocking-object controls all pass.
⚪ arrival does not acquire a new hazard-avoidance or generation-reservation
policy; changing those would be a separate travel design decision.

**Files:** `WorldMapTraversal.cs`, new `DensityWorldMapArrivalTests.cs` + `.meta`,
`DensityPhase1BenchPlayer.cs`, this doc and the final verification receipts.

**Final standalone differential after the arrival fix:** both sides were rerun
with the ten new arrival cases. Baseline `102ba71a`: **9,941 total, 9,482 passed,
459 failed**, 667 selected files. Updated production: **9,954 total, 9,659 passed,
295 failed**, 668 selected files. **Zero newly failing, 164 newly passing.**
The same 13 diagnostics cases explain the selection difference; all ten arrival
and all twenty voxel accessibility cases pass. The top-level
`Docs/Verification/DensityPhase1/standalone-final.json`, compressed XML, exclusion
lists and named outcome diff hold the final evidence. The same stub/hash/native
limitations described in T2.7 still apply.

**Final full Unity EditMode:** **15,522 passed, zero failed, zero skipped**, in
464.690 seconds on Unity 6000.3.4f1. Job `170ea4b1293a4be0ac83941c92499ec5` includes
the arrival repair, all new tests and all native rendering follow-ups. The raw
tool receipt is `Docs/Verification/DensityPhase1/native-editmode-final.json`.

**Final native replay:** run `4d557e31683b41cd944ded0f18c6b871` repeated seed 1,
24 world-map steps and descent to `Overworld.19.13.0`. **18 numeric + 8 native
assertions passed**, zero assertion failures, completed/finalized in 7.333 seconds.
The stronger arrival assertion passed. The inspected screenshot now shows focus
at **(39,11), Contents: sand, you**, without the desert prowler sharing the cell.
One ordinary prowler claw attack for seven damage is visible in the log before
the still-image pause; this is evidence of that one event, not a combat balance
or AI-completeness claim. The temporary high-HP guard remains explicit.

The final run's neighboring `inspection-and-cleanup.json` independently compares
pre/post state: all recorded scene, save override, seed, preference and background
fields match; the original scene is clean, isolation inactive, Edit mode restored
and launcher exit zero. The same two native memoryless-depth console messages
recurred during capture and were confirmed from newly appended log bytes plus
the console. They still bypass the callback; the T2.7 backend/visual/feel limits
remain in force. The requested real editor run and quick Beating-lair look are
now fulfilled, with their raw evidence and limits preserved.

**Final cold-eye pass (Q1–Q4):** preferred and fallback landings share one body
gate; all failure paths before relocation preserve map membership; render aliases
retain owner/gameplay identity; the counter-checks and full native/differential
runs remain green within their stated bounds. Historical census and initial
verification results are explicitly dated rather than represented as current
density measurements. No remaining confirmed yellow/red finding is left open;
long-play tuning, the fire scale, and the unisolated native graphics messages
were deferred at this milestone. The capture messages are subsequently isolated
and resolved in §11 below.

## 11. Native screenshot repair (2026-09-26)

After tranche 2 shipped at `670e3966`, the user requested continued work.
This bounded follow-up isolates and repairs the developer screenshot path.
BitLocker remains dev-only; Urqu and fire behavior are unchanged.

The original file-capture call produced two native memoryless-depth messages
in each capture-enabled run, while its matched disabled control produced none.
`DensityNativeScreenshot` now owns a color-only target and CPU readback,
restores the borrowed active target, and releases its own textures on success
or failure. Backend-aware row correction fixed the first pass's inverted PNG.
The final image matched Unity's same-frame reference at **1920×1080, all RGBA
bytes identical**; the wrong-orientation comparison differs. The temporary
reference call was removed before final acceptance because that call itself
reproduced the original messages. No ordinary camera/render pipeline changes
or log suppression were made.

Final capture ON `1d2b8c0184f8454482ef258c83e4eef5` and OFF
`748995604b6a4bcd981106560ec27bbe` each passed **18 numeric + 8 native checks**.
Neither original native error recurred; raw active-log byte ranges were read
independently of the application callback. The final image was inspected upright.
Both runs restored scene, input, save-root, preference, seed and background
settings, with zero owned capture textures left. An earlier OFF run interrupted
by delayed assembly reload is preserved as rejected; its watchdog restored
settings and recorded failure, then the control was repeated after a force
refresh. The active log file descriptor was resolved because the editor restart
had rotated the open log to `Editor-prev.log`.

**Bounds:** native fidelity/error removal is verified on this Metal editor;
the row-order unit controls cover both orientation branches but are not a
cross-platform GPU test. No balance, combat-feel or long-play claim follows
from these finite captures. See `DENSITY-RENDER-CAPTURE.md` and
`Verification/DensityPhase1/CaptureProbe/` for raw evidence, rejected attempts,
the nine-case RED/GREEN sequence and independent ownership/cleanup review.
