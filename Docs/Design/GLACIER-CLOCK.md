# The Glacier Clock — localized world-consuming fronts

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-14): "One world-scale slow threat advances on a
> visible, very long timer... Consumed regions transform: new biome, new
> enemies, new loot, old towns buried and lootable as ruins... It's only
> smaller sections of the world that change biomes, not the entire world,
> and those sections need a lot of reason in this world."

## 0. The discovery that reframes this whole doc

This system does not need to be invented. **It is already canon, already
named, and already flagged by an earlier lore pass as the single best
unbuilt idea in the game's geography** — it was written down and never
turned into a system. Three citations:

1. `Lore/History/05_Spirits.md:114` — **"The Hush — regions where the
   binding is so thin that Naming barely holds... The deep Urqu-bleed
   places."** A named, existing, *localized* consuming phenomenon.
2. `Lore/History/03_History.md:168` — **"The Hush-bleed spreads. Places
   stable for centuries are developing Urqu-bleed... The Last Counter
   outpost has been pulled back twice in two generations — the Concord's
   guaranteed-delivery line is retreating."** The front is already
   advancing, already has a visible frontier landmark (an outpost in
   retreat), already ratchets rather than resets.
3. `Lore/History/02_Geography.md:101` — the design-canon rule already
   states the causal engine this doc would otherwise have to invent:
   **"The player cannot map Urqu and route around it. It is a weather of
   places and roads, correlated loosely with Strangeness Tier and
   tightly with the player's own incomplete-act ratio — completing
   things weakens Urqu locally; abandoning things feeds it. The player's
   behavior shapes where the infection spreads. This is the strongest
   mechanical hook in the geography."**

So this proposal is not new lore. It is **the implementation plan for a
mechanical hook the project already called its strongest, six phases
ago, and shipped zero code for.** Everything below is in service of
finally building it — and of honoring the two constraints just added:
localized, and load-bearing with in-world reason.

## 1. Why "one ice sheet" is wrong for this world, and what's right instead

A single uniform front (the pitch's "an ice sheet, a salt tide, a rust
front" as interchangeable skins on one mechanic) would contradict two
already-locked facts: Urqu is explicitly "a *scattered infection* of
places and routes, not a geometric region" (Geography §I, Bet 1 — "no
circle, no rings, no wedges"), and the world has **six different
inheritance-functions**, each of which fails in its own characteristic
way when its local binding gives out. A region near the Salt-Vault
doesn't go quiet the same way a region near a Concord trade-post does.

So: **not one Glacier Clock. Several small, independently-ticking
Fronts, each with a diegetic cause, each keyed to a different god's
domain, each named in the world's own vocabulary** — which is exactly
what makes "smaller sections, lots of reason" true instead of asserted.

### The three Fronts (mapped onto the pitch's three examples)

| Pitch's example | This world's Front | Domain / cause | Canon anchor |
|---|---|---|---|
| "an ice sheet" | **the Hush** *(canon name, kept as-is)* | Naming (the empty seventh) — Urqu-bleed from unclosed acts, broken oaths, swallowed refusals | `05_Spirits.md:114`; `Codex/04_TentRightOath.md:33` — *"A swallowed no is a seed of the Hush; we do not grow that crop here"* |
| "a salt tide" | **the Cure** *(new — wordplay on Salt-Cure)* | Preservation (Othren/the Salted) — arrest metastasizing outward from a Catcher cell or old catastrophe site | `09_ImminentArchive.md` (Catchers preserve the about-to-die; "throw salt at the dying"); `Lore/10_Bible.md` Preserve ending ("nothing decays, nothing heals") |
| "a rust front" | **the Arrears** *(new — Concord/Exchange vocabulary)* | Exchange (Tovreth/the Counter) — contracts left unclosed past their term literalize as corrosion and debt-made-flesh | `04_SaccharineConcord.md` (the Falling-Due, "every debt came due"; the Rising Market profiting off collapse) |

Honesty note: "ice sheet" has no literal cosmological analog (there is
no cold-god); the Hush is the functional match — a spreading blankness
and quiet rather than literal frost, but identical in shape: it
consumes, it transforms, it does not simply kill on contact. Salt and
rust map exactly.

*(A fourth is easy to add later and is not spec'd here to keep scope
tight: **the Green**, a Choir/Bloom substrate-overgrowth front from
unclosed grief, re-igniting a miniature version of the already-canon
~120-year Choir-Bloom war at local scale.)*

## 2. The causal engine — why *this* region and not that one

Every place already effectively carries the raw material for a ledger:
quests abandoned there, oaths sworn and broken there, corpses left
unread/unmourned, shrines and everyday charms neglected. Formalize this
as a **local incomplete-act tally** per place (the mechanism
`11_SecondSpine.md` C4 already designed world-wide as the
**closure-ledger** — closed / refused / abandoned — scoped down to a
place instead of the whole world).

- **The global clock is slow and visible**, matching the pitch's "very
  long timer": a Thinning-pressure value that ratchets up over the
  campaign (canon's Gen 34–36 "manifestations doubling" cadence is the
  narrative precedent). It sets a **ceiling** on how fast any place can
  advance — the ambient dread that things are worsening everywhere.
- **The local tally decides which places actually move, how fast, and
  which Front appears.** A place with mostly broken hospitality-oaths
  and un-avenged deaths trends Hush. A place near a Catcher cell or a
  mass-death site with bodies left un-arrested trends Cure. A
  Concord-heavy trade town with unclosed contracts and unpaid tariffs
  trends Arrears.
- **This makes the player the author of the map's decay pattern**,
  exactly as Geography §101 specifies. A player who always finishes
  business in their favorite three towns and never returns to the
  frontier ones is *choosing*, structurally, which regions get eaten —
  without the game ever framing it as a choice. That's the "genuine
  cost to taking your time" the pitch wants, and it's diegetic rather
  than a bolted-on doom clock.
- **Reversibility, both directions.** Closing open acts in a Restless
  or Bleeding place (finish the quest, hold the funeral, repair the
  shrine, pay the tariff, maintain the everyday charm) lowers its local
  tally and can pull it back a stage. A Consumed place is not framed as
  permanently lost content — a late-game Naming-root capstone (see
  `Docs/Design/BUILD-EXPRESSIVITY.md` §3.7, "Give a Name") is the
  natural, thematically loaded tool for *renaming a scraped place back
  into being*, giving the endgame a "repair the map" storyline for free.

## 3. Stages (buildable, four gates)

| Stage | Player-visible signal | Mechanical change |
|---|---|---|
| **0 — Stable** | none | none |
| **1 — Restless** | ambient only: *sari... sari...* audible more often here; NPCs uneasy; a Recension bulletin or Concord actuarial notice names the place | none — a pure warning tier, so the player who's paying attention gets to act before anything is lost |
| **2 — Bleeding** | Overwrit-style bleed fragments start appearing (per the amended under-text policy — hand-placed, never explained); some content degrades (a shop's stock thins, a quest-giver grows evasive); a visible frontier landmark retreats (the Last Counter pattern) | first mechanical teeth: Front-flavored hazards begin spawning; local charms/wards need active maintenance or the tally accelerates |
| **3 — Consumed** | the region transforms | zone re-keys to a new pseudo-biome per Front (see §4); old POI becomes a ruins-variant, still visitable and lootable; surviving un-evacuated NPCs become Front-flavored consumed entities (see §4) |

Stage 2→3 is the only truly destructive transition and is the one gate
that should be **slow and telegraphed for a real in-game span** (weeks
of world-time at minimum) — the pitch explicitly wants "a visible, very
long timer," not a surprise.

## 4. What "consumed" produces, per Front (new biome / enemies / loot / ruins)

This reuses, exactly, the content-keying pattern already proven this
session in `Docs/GATHER-LOOT-SYSTEM.md` and the FUN-P0
`GrimoireDistribution`/`LairTreasure` tables (biome-keyed dictionaries
with a totality/reachability test) — a Consumed cell is a **new
`BiomeType`**, and every system already keyed by `BiomeType` (scribe
stock, lair chest loot, ambassador placement) gets a hook for the new
content for free.

| Front | New pseudo-biome | New enemies (flavor) | New loot | Ruin flavor |
|---|---|---|---|---|
| The Hush | `BiomeType.Overwrit` (the region already named in canon — `Lore/Design/THE-OVERWRIT.md`) | faint, half-Unsaid things; bleed-fragment hazards | under-text relics (§9.1 J in BUILD-EXPRESSIVITY.md); First-Account-adjacent fragments | buried town, names scraped off every sign, the shore-that-isn't (bleed fragments as set-dressing) |
| The Cure | `BiomeType.SaltField` | things preserved mid-motion, arrested rather than dead — a horror register distinct from undead: no decay, no threat unless disturbed | salt-cured relics, a Catcher's abandoned tools, name-holding stones (Tepuibone/Memory-marble/Mute-Stone) | a town frozen exactly as it fell, generations of accumulated victims still standing where they died |
| The Arrears | `BiomeType.InArrears` | animated **matured contracts** (a debt so overdue it attacks whoever picks it up — ties directly to §9.1 G's Counting items) | Rising-Market scarcity-futures, Hush-insurance stubs, corroded mineral infusions | a trade-post rusted through, ledgers still legible, prices still marked, nobody left to collect |

Every row is content on the existing biome-keyed dictionaries — **no
new engine surface for the loot/enemy layer**, per the FUN-P0 "feed
before you build" rule already adopted in `BUILD-EXPRESSIVITY.md` §1.8.

## 5. Engineering feasibility (grounded in the actual code)

- `WorldMap` (`Assets/Scripts/Gameplay/World/Map/WorldMap.cs`) is a
  20×20 grid: `BiomeType[,] Tiles` + `PointOfInterest[,] POIs`.
  `BiomeType` is a 4-value enum (Cave/Desert/Jungle/Ruins) — adding
  `Overwrit`/`SaltField`/`InArrears` is a small, additive enum change.
- **Zones generate lazily and deterministically** (confirmed during the
  FUN-P0 spine survey, `Docs/FUN-P0-M3-THREAD-FORWARD.md` sweep C5).
  This is the load-bearing feasibility fact: **a cell that hasn't been
  visited yet is just a data flag.** Advancing an unvisited cell's stage
  costs nothing but changing what it generates as *the first time the
  player arrives* — no runtime zone-mutation engineering needed for
  that case.
- **The hard case is a cell the player already generated and can
  revisit** — a real village with hand-authored NPCs mid-quest. Mutating
  a live zone in place (evicting/transforming existing entities safely,
  interacting with save/load) is real, unscoped engineering.

### Scope-prune (with rationale, per CLAUDE.md §1.3)

**v1 restricts systemic Consumption to procedural/uninhabited POIs**
(Lair, MerchantCamp, wilderness cells with no hand-authored roster —
`POIType` already has exactly this distinction in
`Assets/Scripts/Gameplay/World/Map/PointOfInterest.cs`). A named,
hand-authored village falling is kept as an **authored set-piece**
(the Last Counter's twice-retreated outpost is already exactly this
pattern in canon) rather than a systemic rule, until live zone-mutation
is separately designed and built. This is the same discipline the
FUN-P0 spine used to cut its Thinning-scalar and character-creation
scope: don't build new engine surface the diagnosis doesn't need yet.

## 6. Presentation (diegetic, not a progress bar)

The pitch asks for "a visible, very long timer." This world's own
document-obsessed factions make the natural UI a diegetic one instead of
a raw meter:

- A **Recension bulletin** or **Concord actuarial report**, purchasable
  or postable at any trade-post, listing Restless/Bleeding places by
  name — in-world text the player reads, not a HUD element.
- The **Last-Counter pattern generalized**: each Front has a landmark
  outpost/marker that visibly retreats a fixed number of times before
  Stage 3 — a concrete, walkable "the frontier moved" signal, exactly as
  already established for the Hush.
- Optional stretch: a raw numeric meter still exists underneath for
  players who want it (a settings toggle), but the default experience
  is finding out a place is dying by reading about it, the same way the
  Bible teaches every other mystery in this game.

## 7. Fun-design check against the pitch's own stated goals

- **"Take your time has a genuine cost."** ✅ — local tallies advance
  only where the player leaves acts open; time spent finishing business
  elsewhere is time a neglected place spends climbing its own stages.
- **"A genuine reward, not grind-forever-nothing-changes."** ✅ — a
  Consumed region is strictly *new* content (biome, enemies, loot,
  ruins), not a punishment state; late-game exploration is regenerated
  by the world eating its own history.
- **"Solves late-game emptiness."** ✅ — the set of Consumed regions
  only grows as a campaign matures, so the late map has more to explore
  than the early map, inverted from the usual roguelike/RPG pattern
  where the world is "most alive" at hour one.
- **"Only smaller sections, not the whole world, and it needs reason."**
  ✅ — three independently-ticking, domain-specific Fronts instead of
  one global sheet; every stage transition cites the specific god,
  faction, or local failure that caused it; the causal engine (local
  incomplete-act tally) is not invented for this pitch, it's the
  project's own six-phases-old "strongest mechanical hook," finally
  wired up.

## 8. Suggested build order (agent-pace, per CLAUDE.md major-feature workflow)

1. **Data model only**: `FrontState` per WorldMap cell (Front type,
   stage, local tally) + the three new `BiomeType` values. No gameplay
   yet — a pure data/test ship, mirrors `LootTableRegistry`'s shape.
2. **The tally feed**: wire quest-abandon, oath-break, and
   un-mourned-death events (once the closure-ledger substrate from
   `BUILD-EXPRESSIVITY.md` §13 step 2 exists) to increment a cell's
   local tally. Global Thinning-pressure as a slow ceiling.
3. **Stage 1 (Restless) presentation**: the bulletin/notice content
   layer — pure writing + a storylet, no new engine.
4. **Stage 2 (Bleeding) hazards + Stage 3 content** for procedural POIs
   only (§5 scope-prune): new BiomeType-keyed loot tables and
   population tables, reusing the exact M4 pattern from
   `Docs/GATHER-LOOT-SYSTEM.md`.
5. **Defer**: live-zone mutation for hand-authored villages; the
   optional fourth Front (the Green); Naming-capstone region-repair.
