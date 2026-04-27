# Content Roadmap — Tiers 1-5

> **Living document.** Update on every content ship. Refer to before
> picking the next task. Status emoji + commit/PR hash = source of
> truth for what's done.

## How to use

- **When picking work:** scan tier in order; pick the first ✅-eligible
  item that fits your time budget.
- **When shipping:** flip status to ✅ and add commit hash. Add a one-line
  blurb under "Recently shipped."
- **When discovering scope creep:** split into a separate item rather
  than letting one ship grow.
- **When finding a new idea:** add to the appropriate tier with `📋`.
  Don't auto-elevate — let it earn promotion through prior wins.

## Status legend

| Emoji | Meaning |
|---|---|
| ✅ | Shipped (commit/branch linked) |
| 🚧 | In progress |
| 📋 | Planned (next-up candidate) |
| 💡 | Idea (not yet committed-to) |
| ❌ | Considered + rejected (with rationale) |

---

## Currently working on

(none — pick from Tier 1 next)

---

## Recently shipped

| Commit | What |
|---|---|
| `d00299b` | ElementalSwordsShowcase scenario + smoke tests for 4 combat showcases |
| `9e2358a` | IceSword (1d8 Cutting/Ice/LongBlades) + 6 tests |
| `2bbe31b` | Combat hit-log fix: log + FX + dismemberment use post-resistance damage |
| `6de301e` | FlamingSwordShowcase scenario + FlameDemo probe |
| `3261f1f` | FlamingSword (1d8 Cutting/Fire/LongBlades) + 6 tests |
| `5803d3e` | StoneskinTonic (Phase F + T2.4 first content) |

---

## Tier 1 — Quick Wins (≤1 hour, content-only)

> Pure JSON + tests. Reuses existing infrastructure. Each ship adds
> visible gameplay depth without new code paths. The FlamingSword /
> IceSword pattern is the template.

### Elemental weapons (Phase C × E)

- ✅ **FlamingSword** — 1d8 Cutting/Fire/LongBlades — `3261f1f`
- ✅ **IceSword** — 1d8 Cutting/Ice/LongBlades — `9e2358a`
- 📋 **ThunderHammer** — Bludgeoning/Electric, 1d8+1, slow but heavy. Gates on something Electric-resistant (need to add).
- 📋 **AcidicDagger** — Piercing/Acid, 1d4+1, fast. Gates on something Acid-resistant (need to add).
- 💡 **CryoLance** — Piercing/Ice longblade, 1d6+2 with high crit
- 💡 **EmberSpear** — Piercing/Fire, mid-tier polearm

### Backfill weapon Attributes (cheap)

> Most existing weapons declare no `Attributes` string and so don't
> participate in physical-class attribution. Backfilling each is ~1
> blueprint line + 1 test.

- 📋 **LongSword** — `Cutting LongBlades`
- 📋 **Battleaxe** — `Cutting Axe`
- 📋 **Greatsword** — `Cutting LongBlades` (2H)
- 📋 **Mace** — `Bludgeoning Cudgel`
- 📋 **Spear** — `Piercing`
- 📋 **Hatchet** — `Cutting Axe`
- 📋 **Claymore** — `Cutting LongBlades`
- 💡 **Sporeblade** — `Cutting LongBlades` + maybe a `Fungal` flag (already has `RotChoir` tag)
- 💡 **ChoirSpine, OldWorldPipe, etc.** — case-by-case, may have lore-driven attributes

### Resistant creatures (matching elemental weapons)

> Each creature spawns the resistance interaction for one weapon class.

- ✅ **Glowmaw** — HeatResistance 50 — exists
- ✅ **Snapjaw** — ColdResistance 25 — exists
- ✅ **SnapjawHunter** — ColdResistance 50 — exists
- 📋 **BrassHusk** — declare ElectricResistance 50 (thematic; pairs with ThunderHammer)
- 📋 **CaveSlime** — declare AcidResistance 50 (thematic; pairs with AcidicDagger). Could also add vulnerability somewhere.
- 💡 **IceWight** — full ColdResistance 100 (immune to Ice, vulnerable to Fire)
- 💡 **CharredHusk variant** — HeatResistance 100 + ColdVulnerability (-50)

### Status tonics (use existing StatusTonicPart dispatch)

> The StatusTonicPart already dispatches to BurningEffect, AcidicEffect,
> WetEffect, ElectrifiedEffect, FrozenEffect, StoneskinEffect via name
> lookup. Each new tonic = 1 blueprint + 1 test.

- ✅ **PoisonTonic, FireTonic, StoneskinTonic** — exist
- 📋 **AcidTonic** — applies AcidicEffect on shatter
- 📋 **LightningTonic** — applies ElectrifiedEffect (5 turns conductive)
- 📋 **FrostTonic** — applies FrozenEffect (2 turns can't move)
- 📋 **WaterTonic** — applies WetEffect (damps fire, conducts electricity)
- 💡 **BleedTonic** — applies BleedingEffect (DOT)
- 💡 **CharredTonic** — applies CharredEffect (post-burn vulnerable state)

### Lightweight scenarios

- ✅ **FlamingSwordShowcase, ElementalSwordsShowcase** — exist
- 📋 **ElementalCreatureZoo** — one of every resistance creature in a small zone, label them. Useful for content QA.
- 💡 **TonicTestBench** — vials of every tonic on a shelf, bottles labeled. Drink-and-observe.

---

## Tier 2 — Cohesive Bundles (½–1 day)

> Pair a small system enhancement with content that exercises it. The
> StoneskinTonic ship is the template (Phase F event hook + Effect class
> + tonic blueprint + tests).

### OnHitEffects abstraction

- 💡 **`MeleeWeaponPart.OnHitEffects` list** of `(Probability, EffectName, Magnitude, Duration)` tuples. On successful hit, roll each probability and apply the effect to the defender.
- Unblocks: FlamingSword applies BurningEffect on hit, IceSword applies FrozenEffect, AcidicDagger applies AcidicEffect, Sporeblade applies a Fungal status.
- Risk: tons of existing effects already exist; just need the wiring + a hook in `PerformSingleAttack` after `ApplyDamage`.

### Light-emitting weapons / equipment

- 💡 **`LightSourcePart` propagation through equipment** — held FlamingSword glows red, held IceSword glows cyan. Currently LightSource lives on entities directly; need a "if equipped, project light from wielder's cell" pass.
- Unblocks: torches, lanterns held in hand, glowing weapons.

### Throwable consumables

- 💡 **Tonics shatter on impact when thrown.** ThrowItemCommand already exists. Need: on-impact event → check StatusTonicPart → apply to creatures in target cell. Pairs with the elemental tonics above.

### Trap furniture

- 💡 **PressurePlate, FireTrap, SpikeTrap.** Cell-stepped events fire damage / status. Mechanically similar to AmbushPart but cell-bound.
- 💡 **TripWire** — line of cells that all fire when stepped on.

### Lock & key

- 💡 **LockPart on doors/chests.** KeyPart on items. UnlockEvent on bump. Foundation for keyed dungeon progression.

### Hunger / Food

- 💡 **HungerStat + FoodPart on edibles.** Existing food items (Snapjaw meat, jerky) can declare nutrition. Buff/debuff on full vs starving.

---

## Tier 3 — System Extensions (1-2 days)

> A real new system + content that exercises it. Each one is a
> commitable feature with its own `Docs/<feature>.md` plan.

### Crafting / Forge

- 💡 **AnvilSitePart + RecipeSystem.** Combine ingredients (ore + fuel + Forge interaction) → output items. Unblocks player-created elemental weapons (IceSword from steel + ice essence).

### Quest System

- 💡 **NPC dialog → objective → reward loop.** Existing conversation system has the dialog half. Need: quest state on NPCs, completion checks, reward grants.
- Pairs with: faction reputation effects, settlement plot.

### Trading / Shopkeepers

- 💡 **MerchantInventoryPart + Currency + price formula.** Existing Merchant creature blueprint exists; needs the actual trade UI + offer system.

### Spellbook / Mana / Scrolls

- 💡 **ManaStat + ScrollItemPart that grants one-time-use mutations.** Existing GrimoirePart is similar but for permanent spells. Scrolls = consumables.

### Day/Night cycle

- 💡 **Time-of-day stat + light propagation + monster behavior shifts.** Currently no cycle. Could thread through TurnManager.

### Weather

- 💡 **WeatherSystem rolls per-zone.** Rain damps fire (extinguishes BurningEffect). Snow chills (applies WetEffect → Cold synergy). Storm spawns Lightning damage.

---

## Tier 4 — Long Arcs (3-5 days)

> Multi-system features. Each requires a planning phase, sub-milestone
> breakdown, and probably a saga or two of fixes. Methodology Template
> §1.1 fully applies.

### Procedural quests

- 💡 **Quest templates → procedurally generated objectives.** Hunt N creatures, fetch N items, kill named target, escort NPC. Plugs into Quest System (Tier 3).

### Companion system

- 💡 **Tame / hire NPC followers.** They walk with the player, share inventory, take orders ("attack", "wait", "fetch X"). PetDog already exists — extend its goal stack.

### Skill trees

- 💡 **XP-gated skill unlocks.** LevelingSystem already grants XP. Need: skill graph, point allocation, stat/ability effects.

### Themed dungeon generator

- 💡 **Dungeon templates with theme = monster pool + tile palette + loot table.**
  - **IcyCaves**: SnapjawHunters + IceWights, snow tiles, frostshield loot
  - **FungalMires**: SporeShamblers + Sporeblades, mushroom tiles
  - **RuinedTowers**: SkeletalSentry + Brass items, stone tiles
- Builders/ already has the worldgen scaffolding; need theme injection.

### Boss creatures

- 💡 **Unique encounters with scripted multi-phase behaviors.** A boss has 3 hp thresholds, summons adds at each, has signature attacks. Tied to dungeon endings.

---

## Tier 5 — Aspirational / Big Bets

> Months of work. Require design decisions. Listed for orientation —
> not roadmapped.

- 💡 **Procedural lore generator** — historical events, legendary figures, named items with backstory
- 💡 **Item adjective system** — random magical prefixes/suffixes ("flaming sword of cleaving"), procedural stat rolls
- 💡 **Roguelike permadeath structure** — meta-progression, unlocks across runs
- 💡 **Multiple endings / NewGame+** — branching late-game content
- 💡 **Modding API** — JSON-loaded user content beyond Objects.json
- 💡 **Multiplayer support** — co-op or asynchronous
- 💡 **Steam release polish** — settings menu, save migration, Steam achievements

---

## Cross-cutting concerns

> Things that aren't a single feature but affect many.

- ⚪ **Determinism audit** — ensure all RNG goes through seeded `System.Random` instances; no `UnityEngine.Random` in gameplay code. Important for quest seeds, save reproducibility.
- ⚪ **Save graph completeness** — already 56+ tests; the deep-dive audit (commit `0b00968`) found 0 bugs. Re-run periodically as new fields ship.
- ⚪ **Performance budget** — turn-based combat is forgiving; no profiling done yet. Likely safe until 100+ entities/zone.
- ⚪ **Accessibility** — colorblind mode (currently use `&R`/`&C` color codes for swords; ensure shape/glyph differentiates too).

---

## How to add to this doc

When you have an idea:
1. Pick the smallest tier it could ship in. If it requires new code paths, it's not Tier 1.
2. Add a `💡` bullet under that tier's relevant subsection.
3. Note the system(s) it builds on, and what it unblocks.
4. Don't promote to `📋` unless it's the next thing in line.

When you ship:
1. Flip status to `✅` with the merge commit hash.
2. Add a one-line entry to "Recently shipped" with the same hash.
3. If the ship revealed new ideas, drop them in as `💡`.

When you reject an idea:
1. Mark it `❌` with a one-line rationale (e.g., "❌ Friction-based stamina — duplicates Speed, no clear gameplay payoff").
2. Don't delete — record the decision so we don't re-litigate.
