# LOOT OVERHAUL — Gear That Drops, Containers That Exist, A World That Supplies

> Status: **COMPLETE (SM1-SM8).** Written 2026-08-09 after a
> full verification sweep (CLAUDE.md §1.2). The sweep caught one
> load-bearing false premise; the plan below is the CORRECTED one, not
> the one the request implies. Read §1 before anything else.
>
> User ask (verbatim): *"there are not tons of ways to get weapons,
> weapon parts, supplies, alchemical ingredients, armor, etc just from
> the world. enemies should drop whatever gear and weapons they have,
> as well as random other loot calculated from a loot algorithm. also,
> chests, boxes, containers, barrels, etc should be placed around the
> world following a common loot placement algorithm."*

---

## 1. Verification sweep — corrections BEFORE any code

| # | Premise (implied by the ask) | What the code actually does | Consequence for the plan |
|---|---|---|---|
| **1** | **"Enemies should drop their gear"** — needs building | **The drop mechanism ALREADY EXISTS and is 100% reliable.** `HandleDeath` calls `body.DropAllEquipment(zone)` (CombatSystem.cs:1253) then `DropInventoryOnDeath` (:1258). Every equipped item and every inventory object lands on the death cell. Pinned by 3 tests (CombatSystemTests.cs:492/520/569). | **The milestone is NOT "make enemies drop gear."** It is *"give creatures gear worth dropping."* Zero changes to the death path are needed for the core ask. |
| **2** | Creatures carry gear | **73 / 73 creatures carry nothing.** All inherit `Creature`'s Inventory part, which sets **only `MaxWeight: 150`** (Objects.json:417). `InventoryPart` has **no `Contents` / `StartingItems` / `StartingGear` field at all** (InventoryPart.cs:17-43) — blueprints have no vocabulary to pre-fill an inventory or equip an item. | Requires **new blueprint vocabulary** (§3 L1). This is the single highest-leverage change in the plan. |
| **3** | Their weapons would drop | Creature weapons are **natural-weapon behaviours** on `part._DefaultBehavior` (EntityFactory.cs:493-503), not equipped items. `UnequipSubtree` only touches `_Equipped`, so they never drop — **correctly**. A spider must not drop "fangs." | Loadouts are **additive**: beasts keep natural weapons; humanoids gain real, droppable gear. Do NOT try to make natural weapons droppable. |
| **4** | Barrels are lootable | **`WoodenBarrel` has no Container part** (Objects.json:16846) — it is Render + Physics + Material + Fuel. Pure scenery. Its only placements are DevMode-gated. | One-line blueprint fix, plus a real container family (§3 L4). |
| **5** | Wilderness has containers | **`PopulationBuilder` places ZERO containers, ever** (PopulationBuilder.cs:24-59 — it only rolls the biome table and drops bare items). The *only* container sources are LandmarkBuilder stamps and one village chest. | The placement algorithm the user asked for **does not exist in any form**. It is net-new (§3 L3). |
| **6** | Container density scales with danger | **No budget, no density, no tier scaling anywhere.** Hard cap `MaxStructuresPerZone = 2` (LandmarkBuilder.cs:672); tier only *gates* stamps via `MinTier`, never scales counts. `StampCatalog.Underground(depth)` **ignores its `depth` parameter entirely** (:152-155). | Budget formula is net-new. Also fixes a live absurdity: **a tier-1 desert zone can contain zero containers by construction** (both its chest stamps are `MinTier = 2`). |
| **7** | Loot tables are richly wired | 19 tables, all referenced, none orphaned — the system is **healthy**. But `TableRef` nesting is implemented + unit-tested and used by **zero** content. Tier lives only in table *names*; the registry has no tier concept. | Reuse the registry as-is (no engine change). Use `TableRef` for shared sub-tables — a free, already-tested lever. |

### Two bugs found in passing (fold into this work)

- **`MimicChest` cannot be opened and holds nothing.** It is a `Creature` with `Brain(Staying)` + `AIAmbush` (Objects.json:21779) but **no Container part**, and nothing ever stocks it. Killing one yields a corpse. The joke doesn't land: a mimic must *look* lootable and *be* lootable once dead.
- **Qud-parity gap in the death path.** Qud guards drops with `NoDropOnDeath` tag, `IsTemporary`, and an `Inventory.DropOnDeath` flag (Body.cs:3161-3168, Inventory.cs:2628-2660 in the decompile). CoO has **none of the three** — a summoned or temporary creature would drop its loadout. Harmless today (nothing summons armed creatures); a live bug the moment L1 ships.

### Supply-gap census (why the world feels empty)

| Category | Exists | Reachable from the world | Gap |
|---|---|---|---|
| Weapons | 33 | **3** ever spawn loose (Dagger, LongSword, ShortSword) | 30 unreachable except shops |
| Armor | 13 | 12 in tables, but tables live only in rare chests | starved by container scarcity |
| Weapon components | 6 | 5 in tables, **`WillowHaftComponent` in zero** | crafting has no supply line |
| Reagents | 13 | 8 in tables; **`EmberFruit`, `GlacierSalt`, `GlimmerBrine`, `BogSap`, `StoneburrSeed` in ZERO** | alchemy has no supply line |
| Containers | **3 blueprints** (Chest, LockedChest, Graveyard — the last never placed by worldgen) | 0–2 per zone, often 0 | the core ask |

Loose gear per wilderness zone today: **0–4 items**, from a pool of 3 blueprints. Desert T2/T3: **0–1**.

---

## 2. Goal & design principles

**Goal:** a player who never visits a shop should be able to arm, armor,
and supply themselves from the world — and feel the world getting
richer as it gets more dangerous.

1. **Drops come from real possessions.** An enemy drops gear because it
   *had* gear, resolved at spawn. No phantom "drop table" that
   contradicts what you saw it wielding. (This also means the existing
   100%-drop path needs no changes — §1 correction 1.)
2. **One placement algorithm, every zone type.** Wilderness, underground,
   lair, village, POI — all call the same service with different
   budgets. No zone type silently gets zero.
3. **Danger pays.** Tier and depth scale container *count*, container
   *kind*, and table *tier* — the three levers that make deep zones feel
   different from the doorstep.
4. **Supply the crafting systems.** Weaponcraft (6 components,
   Blade/Haft/Binding slots) and alchemy (13 reagents, tag-driven brew
   rules) are fully built and starved. Loot exists to feed them.
5. **Guard the economy.** Round 6 just made gold real (5 drams/coin) and
   tier-scaled XP. More loot = inflation risk; §6 sets explicit budgets.
6. **Every gate emits a diag record** (CLAUDE.md observability rule) —
   `loot` and `worldgen` channels already exist.

---

## 3. The five layers

### L1 — Creature loadouts (makes "drop what they have" real)

New blueprint-authorable part, resolved by the existing `name + "Part"`
convention (EntityFactory.cs:236-237), so **zero loader changes**:

```json
{ "Name": "Loadout", "Params": [
  { "Key": "Equip", "Value": "ShortSword;LeatherArmor" },
  { "Key": "Carry", "Value": "HealingTonic:35;GoldCoin:80x3-9;VenomGland:20" },
  { "Key": "Pick",  "Value": "1;Dagger;Hatchet;Cudgel" }
]}
```

- `Equip` — created and **equipped** at spawn (so it shows in combat and
  drops via the existing path).
- `Carry` — `blueprint:chance%[xMin-Max]`, into inventory.
- `Pick` — `N;a;b;c` — pick N of the listed blueprints (weapon variety
  without authoring one blueprint per bandit).
- Rolled **once at spawn**, in `EntityFactory` *after* anatomy init so
  equipping resolves body parts.

**Content:** loadouts for the ~20 humanoid hostiles only —
DesertBandit, AmbushBandit, RuinScavenger, RuneCultist, SnapjawChieftain,
SnapjawWarlord, PaleCurator, GlassblownDrifter, SaccharineEnvoy,
Quartermaster, the gnome-folk, etc. **Beasts, oozes, constructs and
tendrils get none** — they keep natural weapons (correction 3).

**Parity guards (fold in):** honor a `NoDropOnDeath` tag and a
`Temporary` check in `DropAllEquipment` / `DropInventoryOnDeath`,
mirroring Qud (Body.cs:3161-3168). Cheap now, prevents a real bug later.

**Balance note:** gear drops at 100% like Qud. Because loadouts are
*rolled per spawn* with per-entry chances, the flood is controlled at
the source (a 35% tonic on a bandit ≈ one tonic per three bandits), not
by a lossy drop roll that would contradict what the player saw equipped.

### L2 — Death loot roll (the "random other loot from an algorithm")

A `Died` subscriber (`LootDropSystem`) that rolls **one** table chosen by
(creature tier tag × creature class), scattering results on the death
cell:

| Class | T1 | T2 | T3 |
|---|---|---|---|
| Beast | `DeathBeastT1` | `DeathBeastT2` | `DeathBeastT3` |
| Humanoid | `DeathHumanoidT1` | `DeathHumanoidT2` | `DeathHumanoidT3` |
| Construct | — | `DeathConstructT2` | `DeathConstructT3` |

Contents skew toward the **supply line**: reagents, weapon components,
coins, occasional tonics — not weapons (those come from loadouts, which
is more legible). Shared sub-tables via `TableRef` (finally using the
tested-but-unused nesting).

Modest by design: expected value ≈ 0.6 items per kill at T1, ≈ 1.4 at T3.

### L3 — Container placement algorithm (the core ask)

New `ContainerPlacementService.Populate(zone, ctx)` called from
`ZoneGenerationPipeline` so **every** zone type gets it — including the
fallback pipeline and lairs, which today get zero.

**Budget:**
```
count = BaseFor(zoneKind) + TierBonus(tier) + rng.Next(0, 2)
  wilderness  base 1   underground base 2   lair base 2
  village     base 1   POI/camp    base 1
  TierBonus = tier - 1        (T1 → +0, T3 → +2, deep underground up to +7 capped at +4)
clamped to [MinFor(zoneKind), 6]
```
Wilderness T1 → 1–3 containers; underground depth 9 → 4–6. Today: 0–2,
frequently 0.

**Kind weights by biome** (so containers read as *place*):

| Biome | Kinds |
|---|---|
| Cave | Crate, Sack, OreCache, Barrel |
| Desert | Urn, BuriedCache, Sack |
| Jungle | WovenBasket, HollowLog, Urn |
| Ruins | StrongBox, Reliquary, Bookshelf, Crate |
| Village/POI | Barrel, Crate, Sack |
| Underground | OreCache, Crate, StrongBox, BoneCache |

**Placement rules** (mirroring how a real space is used):
- prefer wall-adjacent and corner cells; interiors (`Cell.IsInterior`)
  weighted up
- never on stairs, `GenReservedCells`, liquid pools, or a cell that would
  block a doorway
- minimum Chebyshev spacing 3 between containers
- deterministic from the zone seed (same seed → same layout)

**Stocking:** table = `f(containerKind, tier)`, e.g.
`OreCache × T2 → OreCacheT2`. Locked variants roll a richer table and
require the existing `Lock` part + iron key.

### L4 — Container & content blueprints

- **Fix `WoodenBarrel`** — add a Container part (one line). It is already
  placed by village layouts and is already flammable, which makes
  "burn the barrels" emergent with the Materials system.
- **Fix `MimicChest`** — give it a Container part + stock it, so it looks
  and loots like the real thing until it bites.
- **New:** Crate, Sack, Urn, StrongBox (locked), OreCache, BoneCache,
  WovenBasket, HollowLog, Reliquary (locked), Bookshelf, WeaponRack,
  AlchemyShelf.
- **Sprites** via the round-5 pipeline (`coo_items.py` fixture bodies +
  `coo_outline_pass.py` ink rings), registered in the renderer's
  `FixtureSprites` table — the blueprint-keyed pre-pass already handles
  glyph-independent claims.
- **~14 new loot tables** for the kind × tier matrix, sharing sub-tables
  via `TableRef`.

### L5 — Close the supply gaps

- The 5 orphan reagents + `WillowHaftComponent` into biome-appropriate
  tables (GlacierSalt → cold/deep, BogSap → jungle, StoneburrSeed →
  cave, GlimmerBrine → water-adjacent, EmberFruit → desert/fire).
- A `ComponentCacheT1/T2` table so Blade/Haft/Binding are findable —
  weaponcraft currently has no world supply line at all.
- Widen loose-gear pools in `PopulationTable` from 3 blueprints to the
  tier-appropriate slice of the 33-weapon / 13-armor catalog.
- Add `WeaponRack` / `AlchemyShelf` to town + camp stamps so shops have
  visible, lootable-adjacent flavor.

---

## 4. Sub-milestones (smallest blast radius first)

| SM | Scope | Blast radius | Ships alone? |
|---|---|---|---|
| **SM1** | `LoadoutPart` + EntityFactory hook + parser + tests | new part, one factory call | yes — no content yet, no behavior change |
| **SM2** | Loadout content for ~20 humanoids + Qud drop guards | Objects.json + 2 guards | yes — **the "enemies drop gear" ask lands here** |
| **SM3** | `LootDropSystem` + 8 death tables | one `Died` subscriber | yes |
| **SM4** | Container blueprints (12 new + 2 fixes) + sprites | content + art | yes — placed by nothing yet |
| **SM5** | Kind × tier loot tables (~14) | JSON only | yes |
| **SM6** | `ContainerPlacementService` + pipeline wiring | **widest** — every zone type | yes — **the "containers everywhere" ask lands here** |
| **SM7** | Supply-gap fills (orphan reagents, components, wider loose gear) | JSON only | yes |
| **SM8** | Adversarial sweep + live diag audit + balance pass | tests + tuning | closes the feature |

Each is independently revertable and ships one complete testable
behavior (CLAUDE.md §1.4).

---

## 5. Testing strategy

- **RED-first per SM** (§2.1), counter-checks per invariant (§3.4).
- **Adversarial sweep** (`LootOverhaulAdversarialTests.cs`) — the taxonomy
  says this gate applies: **parser** (malformed Loadout strings: empty,
  only-delimiters, bad chance, unknown blueprint, `xMin-Max` inverted),
  **state atomicity** (spawn fails mid-loadout → no half-equipped
  creature), **save/load reach** (a stocked container round-trips),
  **boundary** (chance 0 / 100 / negative; container `MaxItems = 10`
  overflow), **cross-actor** (two creatures, same blueprint, different
  rolls), **determinism** (same seed → same containers).
- **Live diag audit** — enter Play, `diag_query category=worldgen
  kind=ContainersPlaced` and `category=loot kind=DeathDrop`; verify
  counts per zone match the budget formula. This is the *real* proof
  (§6.3 honesty bounds); EditMode can't exercise worldgen end-to-end.
- **Fixture stubs** — every new blueprint that a population/stamp table
  can spawn needs a stub in `WorldMapTests` + `UndergroundGenerationTests`
  (this gotcha has bitten 4× now; pre-empt it in the same commit).

## 6. Economy guardrails (explicit, because round 6 just fixed gold)

- **Per-zone value budget:** target ≈ 40 drams of sellable value per
  wilderness T1 zone, scaling ≈ ×1.8 per tier. Containers hold *supply*
  (reagents, components, food) more than *treasure*.
- **Vendor spread stays wide** — loot inflation is absorbed by buy/sell
  margin, not by nerfing drops.
- **Weight is the real limiter.** `MaxCarryWeight = Str × 15`; a full
  plate + two weapons already strains a starting character. Loadout
  drops self-limit because the player must *choose*.
- **Measure before tuning:** SM8 runs a 20-zone census via `execute_code`
  (count items, sum `Commerce.Value`) and reports the actual curve rather
  than guessing.

## 7. Performance

Per CLAUDE.md, two of the triggers apply (new per-zone-gen work; more
entities per zone), so:

- Placement runs **once per zone generation**, never per frame or per
  turn — the cost sits next to existing `PopulationBuilder` work.
- Cell selection uses a **pre-filtered candidate list built in one pass**
  over the zone (no repeated full scans per container), and the existing
  `GenReservedCells` HashSet for exclusion.
- Container entities add sprite-pass claims. Round 4's incremental-claims
  work makes this ~free on NPC turns; a +4-entity/zone delta is
  negligible against the ~2000-cell full pass. **Re-baseline anyway** via
  `EnvironmentSpriteRenderer.Perf` before/after (SM8).
- No allocations in the placement inner loop (scratch lists, per
  PERF-FOUNDATION §Pattern 1).

## 8. Observability

- `loot / DeathDrop` — creature, tier, class, table, items rolled.
- `worldgen / ContainersPlaced` — zone, biome, tier, budget, actual,
  kinds, per-container table + item count.
- `loot / LoadoutRolled` — creature, equipped list, carried list.
- Existing `loot / TableRolled` (LootStocker.cs:38) is reused unchanged.

## 9. Decisions (user, 2026-08-09)

1. **Locked containers** — keep the single `iron` key for now. No
   per-biome key economy this pass.
2. **Gear glint** — YES. Dropped gear gets a subtle marker (SM4, reusing
   the round-5 item sprites).
3. **Mimic frequency** — 1-in-12 in **ruins**. Never in villages.
4. **Order** — user took the recommendation: drop half (SM1–3) first.

---

## 10. Implementation log

### SM1 — LoadoutPart ✅ SHIPPED

`Assets/Scripts/Gameplay/Entities/LoadoutPart.cs`. Blueprint-authorable
`Equip` / `Carry` / `Pick`, applied on `ObjectCreated` — which
EntityFactory fires immediately after `InitializeAnatomy`, so body parts
exist and equipping resolves. **Zero EntityFactory changes.** Static
`Factory`/`Rng` follow the CorpsePart convention (null = graceful no-op).
Re-entrancy guard (`MaxDepth = 3`) because Apply creates entities and
entity creation fires ObjectCreated — a self-referential loadout would
otherwise recurse until the stack died.

11 tests, RED→GREEN: equip lands in hand, beast-with-no-loadout owns
nothing (counter-check), carry at 100%/0% (counter-check pair), Pick
grants exactly N *and* actually randomizes across 40 spawns (a
first-entry-always impl would pass the naive test), parser hardening
(null/empty/only-delimiters/non-numeric/inverted range/missing
blueprint), chance clamping, null-factory no-op, unknown-blueprint skip.

### SM2 — Loadout content + Qud drop guards ✅ SHIPPED

16 humanoid hostiles armed, tier-scaled: T1 bandits/gnome-folk (dagger
or shortsword + a few coins + a biome reagent), T2 chieftains/curators/
envoys (armor + better weapon + a weapon component), T3 warlord and
palimpsest echo (chainmail, greatsword/claymore, multiple components).
**Beasts, oozes, constructs and tendrils deliberately got none** — they
keep natural weapons, which correctly never drop.

Every referenced blueprint validated to exist before writing (16/16,
zero missing).

**Qud-parity guards added** (the sweep's second bug): `HandleDeath` now
honors `NoDropOnDeath` and `Temporary` tags, mirroring Qud's
`Body.cs:3163` / `Inventory.cs:2634`. CoO had neither — harmless while
creatures owned nothing, an infinite gear fountain the moment SM1
shipped. Pinned by a guard test **and** a counter-check (identical setup
minus the tag still drops), so an inverted or unconditional guard can't
pass vacuously.

**Suite: 5958 → 5971 (+13). Green** (one pre-existing
FungalInfectionContagion order-dependent flake, verified passing in
isolation — 13/13).

### SM3 — Death loot roll ✅ SHIPPED

`Assets/Scripts/Gameplay/World/Generation/LootDropSystem.cs` + 11 new
tables. One table per kill, chosen by (loot class × tier), scattered on
the death cell beside the creature's own gear.

**Loot class is DERIVED, not authored** — so none of the 73 creature
blueprints needed editing: a creature with a Loadout is Humanoid, a
stone/metal/glass/crystal one is Construct, everything else is Beast.
An optional `LootClass` / `LootTable` tag overrides either.

Tables deliberately carry the **supply line**, not weapons (those come
from loadouts, where the player sees them before the kill — more
legible): reagents, weapon components, coins, occasional tonics. This
finally gives weaponcraft and alchemy a world source; before it, 5
reagents and 1 component appeared in **zero** tables.

`ReagentCommon` / `ReagentRare` / `ComponentAny` are shared sub-tables
referenced via `TableRef` — the nesting feature that shipped
implemented-and-unit-tested but with zero content using it.

10 tests: class derivation ×3, table naming ×3, explicit-tag override,
tier clamping (incl. the `int.TryParse` sentinel), roll-lands-on-cell,
zero-chance counter-check, NoDropOnDeath suppression, unknown-table /
null-factory / null-zone / not-in-zone graceful no-ops.

**Suite: 5971 → 5981 (+10). Green, flake included.**

### SM4–SM6 — containers, glint, placement ✅ SHIPPED

**SM4 — 12 container blueprints + 2 fixes + sprites + glint.**
Crate, Sack, Urn, StrongBox (locked), OreCache, BoneCache,
WovenBasket, HollowLog, Reliquary (locked), Bookshelf, WeaponRack,
AlchemyShelf. `WoodenBarrel` finally got a Container part (it was
pure scenery, audit finding 4) — and since it was already flammable,
"torch the barrels" is now emergent with the Materials system.
`MimicChest` got one too, so it looks lootable until it bites.
Sprites via the round-5 pipeline; all 12 registered in the renderer's
blueprint-keyed `FixtureSprites` table.

**Gear glint** (user call): a new `Glint` mote kind in
`AmbientMotesRenderer` — a slow warm twinkle over cells holding
takeable items. Unlike the static fixture anchors, glint anchors are
*refreshed per full redraw* (which every player move triggers), so
loot dropped this turn glints on the next frame. Capped at 24 cells;
scratch-list per PERF-FOUNDATION §Pattern 1.

**SM5 — 36 kind × tier tables**, sharing the SM3 `ReagentCommon` /
`ReagentRare` / `ComponentAny` sub-tables via `TableRef`.

**SM6 — `ContainerPlacementService` + `ContainerBuilder`.**
Budget = base(zoneKind) + (tier−1) + jitter, clamped [1,6]. Kind pools
per biome so containers read as *place* (urns in the desert, ore
caches underground, reliquaries and bookshelves in ruins). Placement
prefers wall-adjacent and interior cells, refuses stairs / reserved
cells / liquids / occupied cells, keeps Chebyshev spacing 3, and is
deterministic from the zone seed. Mimics: 1-in-12, ruins only, never
in a settlement (user call). Hooked into **all 7 pipelines** —
including lairs and the fallback cave pipeline, which got zero
containers before.

**Two defects the live sweep caught that tests had not:**
1. `ReliquaryT1` did not exist, so a tier-1 ruins zone rolling a
   reliquary would spawn it EMPTY. Added, plus an audit asserting all
   12 kind-prefixes × 3 tiers resolve.
2. ~1 container in 5 rolled up empty anyway, because every entry in
   the low-tier tables is chance-gated. An empty chest spends the
   player's walk and their expectation — added a pocket-change
   fallback and a 25-seed regression test.

**Live verification** (new game, then generated zones directly):
town = Chest + a stocked WoodenBarrel; jungle wilderness = HollowLog
+ WovenBasket, both stocked; cave = 2 OreCaches; desert = 2 Urns + a
Sack. Every zone type that previously had zero now has loot.

**Suite: 5981 → 5993 (+12). Green.** Save backed up before the live
run and restored after.

### SM7 — supply-gap fills ✅ SHIPPED

Loose-gear pools widened across all 12 tier tables, from the 3
blueprints that served the entire game (Dagger / LongSword /
LeatherArmor) to tier-appropriate slices of the 33-weapon and
13-armor catalogs — T1 shortswords/hatchets/leather caps, T2
maces/spears/bucklers/helmets, T3 greatswords/claymores/warhammers/
warded cloaks. Entries use `MinCount 0 / MaxCount 1` at low weight, so
`Roll()` rotates **variety** rather than adding **volume**.

The orphan reagents and `WillowHaftComponent` were already closed by
SM3/SM5's `ReagentCommon` / `ReagentRare` / `ComponentAny` sub-tables,
which every death and container table references.

Fixture stubs added to both generation test files (the gotcha that has
now bitten 5 times — pre-empted in the same change).

### SM8 — adversarial sweep + economy census ✅ SHIPPED

**`LootOverhaulAdversarialTests.cs` — 12 tests, 0 bugs found.** Five
taxonomy surfaces applied, so the gate was mandatory:

| Surface | Probes |
|---|---|
| Parser | 23 malformed Loadout strings (null, only-delimiters, `::50`, `Dagger:abc`, `50x-`, int-overflow counts, inverted ranges, whitespace); asserts no throw, no nameless grant, no zero-count grant, no out-of-range chance |
| State atomicity | 9×900-weight grant vs carry limit → creature still coherent, no null slots; death still runs cleanly after a partial loadout |
| Boundary | container `MaxItems` overflow (returned count matches reality), budget across tiers −100…100 × every zone kind stays in [1,6], garbage `Tier` tags fall back safely |
| Save/load reach | a stocked container round-trips through `SaveGraphSerializer` with contents intact — a reload must not empty the world |
| Determinism | same seed ⇒ same layout across **every** biome, **plus** the counter-check that different seeds actually differ (a Populate that ignored its RNG would pass the first test vacuously) |
| Cross-system | two creatures of one blueprint never share an item instance; a second placement pass never stacks containers |

One test failure during authoring was **my assertion, not the code**:
it counted the dying creature among the "floor contents" that must
survive. Corrected to measure bystanders only.

**Economy census — 20 zones, measured not guessed:**

| Metric | Measured | Plan target |
|---|---|---|
| Containers / zone | **2.1** | 1–3 wilderness |
| Items per container | **1.67** | — |
| Sellable value / zone | **47 drams** | ≈40 |

47 vs a 40-dram target across a mixed-tier sample is within ~18% — on
budget, no inflation. For scale: one zone's loot ≈ one healing tonic.
The `ComputeBudget` formula is the single knob if that ever needs to
move.

**Suite: 5993 → 6005 (+12). Green.**

---

## The overhaul is complete (SM1–SM8)

| Before | After |
|---|---|
| 73/73 creatures owned nothing | 16 humanoid hostiles spawn armed and drop it |
| No death loot roll at all | class × tier tables feed the crafting supply line |
| PopulationBuilder placed 0 containers, ever | 2.1 per zone across all 7 pipelines |
| 3 container blueprints (1 unplaceable) | 15, biome-appropriate, always stocked |
| Barrel was scenery; mimic unopenable | both are real containers |
| 3 loose-gear blueprints game-wide | tier-appropriate slices of 46 |
| 5 reagents + 1 component unobtainable | all reachable |

Tests across the arc: 5958 → 6005 (+47).

---

## 11. Original open questions (answered above)

1. **Locked containers** — currently one `iron` key opens everything.
   Want per-biome key types (a real key economy), or keep it simple?
2. **Loot sparkle** — should freshly dropped gear be visually marked
   (the round-5 item sprites make a subtle glint cheap)?
3. **Mimic frequency** — now that mimics can be made convincing, how
   often should a "chest" bite? (Suggest: 1-in-12 in ruins/underground
   T2+, never in villages.)
4. **Scope order** — ship the drop half (SM1-3) first for immediate
   payoff, or the container half (SM4-6) first for the bigger visual
   change? *(Recommendation: SM1-3 first — smaller, and it makes every
   existing fight more rewarding immediately.)*

---

## 12. SM9 — the other half of supply: NPCs who actually trade

**User report (2026-08-09):** *"merchants and saccharine envoys out in
the world do not have inventory when you trade. these are the only two
npcs i tested trading, so im sure many more npcs dont have items to
trade. ensure every npc has relavent items to trade."*

SM1–SM8 fixed supply from the world (kills, containers). This closes
supply from *people* — the third source, and the only one the player
can seek out deliberately.

### Verification sweep — corrections before writing code

| # | Assumption | Reality | Consequence |
|---|---|---|---|
| 1 | "Only shopkeepers offer trade" | `RefreshVisibleChoices` injected "[Let's trade.]" for any speaker with an `InventoryPart` — which **every** creature inherits from `Creature` | All 31 talkable NPCs offered trade; only the 5 town shops (`shop:` stamp) had stock. The user hit an empty window on the first two they tried. |
| 2 | "`Entity.SetProperty` exists" | It does not — only `GetProperty` / `GetIntProperty` / `SetIntProperty` | String props are written straight into `Properties[key]`, matching `LandmarkBuilder.cs:871` |
| 3 | "`TraderRestockSystem` will keep them stocked" | It **skips any entity whose drams < 0** | A trader without a purse never restocks *and* can never buy. `TraderPart` sets the purse before anything else, even when the factory is absent |
| 4 | "A stock table means stock" | Every entry in 24 of 31 tables was chance-gated | Same defect class as `ReliquaryT1` — a table that rolls, validates and yields nothing |

### What shipped

- **`TraderPart`** — blueprint-authorable `StockTable` + `Drams`. Hooks
  `ObjectCreated`, sets the purse, stamps the restock property, rolls
  the opening shelf. Null factory = graceful no-op (the `CorpsePart`
  convention).
- **19 themed stock tables**, one per NPC archetype, assigned across all
  31 talkable NPCs.
- **`ConversationManager.CanTrade`** — the trade option must never lie.
  True for a `TraderPart` holder, a stock-table carrier, anyone with
  goods, or anyone with coin (a sold-out merchant can still *buy*, and
  refusing there would strand the player's loot). False otherwise, so
  runtime-spawned quest-givers simply don't offer it.

### Two defects the unit tests could not see

Both were found by **live spawn checks**, not by the suite — the V-loop
pattern again.

**(a) Shops that roll empty.** A live spawn of 20 NPCs found the
Innkeeper and Scribe opening an empty window. The content audit test
then showed it was near-universal: across 12 seeds, most traders could
come up bare. *Fix:* every stock table now guarantees one **cheap,
thematic staple** at 100% — the scribe always has ink, the innkeeper
always has cooked meat, the well-keeper always has water. The good
stuff stays rare, so guaranteeing a shelf does not inflate the economy.
`EveryTraderTableGuaranteesAtLeastOneItem` pins it, and deliberately
requires a direct `Blueprint` rather than a `TableRef` — a 100%
`TableRef` can resolve into a sub-table that itself rolls empty, which
would move the bug instead of fixing it.

**(b) Town shops double-stocked (cold-eye Q1, symmetry).** `TraderPart`
guards against double-stocking with `if (inv.Objects.Count > 0) return`
— but that guard was on the wrong side of the ordering. The `shop:`
marker calls `CreateEntity` (firing `ObjectCreated`, so `TraderPart`
fills the bare shelf) and *then* rolled **the same table again**:
`shop:Weaponsmith:WeaponsmithStock` names the exact table the
Weaponsmith's own `TraderPart` had just rolled. Live evidence: the town
Weaponsmith carried 11 goods against 8 from a single roll. *Fix:* the
symmetric guard in `LandmarkBuilder` — the stamp stocks only a bare
shelf, so keepers without a `TraderPart` still work.

The regression test counts the **guaranteed staple**, not total items.
Comparing totals against a standalone roll does not work: `TraderPart.Rng`
is a shared static, so the in-zone keeper rolls at a different RNG
position and a legitimately different total. `WeaponsmithStock` grants
`Dagger` at 100%, so one roll leaves exactly one dagger and two rolls
leave two — exact, whatever the RNG does with the chance-gated rest.

### Honesty bounds

**Can verify (script-observable, live):** all 31 talkable blueprints
spawn with goods *and* a purse; all 22 talkable NPCs in the live
starting town carry stock with **zero** duplicate stacks; town shops
dropped from 11/10/8/10/10 goods to 7/5/3/7/7 after the symmetry fix.

**Cannot verify (needs a human at the keyboard):** whether each shop's
*mix* reads as thematically right, and whether prices feel fair at the
new stock depth.

### Files

- NEW `Assets/Scripts/Gameplay/Economy/TraderPart.cs`
- NEW `Assets/Tests/EditMode/Gameplay/Economy/TraderStockTests.cs` (9)
- NEW `Assets/Tests/EditMode/Gameplay/Economy/TraderStockContentTests.cs` (7)
- MOD `Assets/Scripts/Gameplay/Conversations/ConversationManager.cs` — `CanTrade` gate
- MOD `Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs` — bare-shelf guard
- MOD `Assets/Resources/Content/Data/Loot/LootTables.json` — 19 stock tables + staples
- MOD `Assets/Resources/Content/Blueprints/Objects.json` — `TraderPart` on 31 NPCs
- MOD `Assets/Scripts/GameBootstrap.cs` — `TraderPart.Factory` at both wiring sites

Tests: 6005 → 6021 (+16). All green.
