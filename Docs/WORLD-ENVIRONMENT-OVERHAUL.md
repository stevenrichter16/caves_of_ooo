# World Environment Overhaul — Brainstorm + Path

**Started:** 2026-05-13
**Status:** Brainstorm — no code yet. Pre-decision document.

> **Genre framing:** Caves of Ooo is an **RPG, not a roguelike.**
> Authoritative: `Docs/PROJECT-IDENTITY.md`. Every design choice
> in this doc assumes character persistence, save/load
> recoverability, and full weight on permanent narrative
> commitments (Pacts, Brands, Marks, faction-reputation).

## Goal — "lock into cementing the environment"

The world pivots to a setting built around **tepuis, catacombs,
tepui sinkholes**, sourced from the design in
`origin/claude/ideas-gin-frogs:IDEAS.md`. This doc is the bridge
between "what CoO is today" and "what the cemented world looks
like," structured as a multi-phase path with each phase
independently testable + revertable.

Inputs:
- IDEAS.md survey (975 lines distilled — see `WORLD-IDEAS-SURVEY.md` if
  we generate one; for now, the survey lives in this session)
- Worldgen state survey (current zones, biomes, vertical structure)

---

## Premise: what "cementing the environment" actually means

Three things need to happen for the environment to feel locked:

1. **Worldgen produces tepui-shaped zones.** The dominant surface
   biome shifts from flat-cave/village to plateau-top vertical
   cliff terrain. New `BiomeType.Tepui`.
2. **Sinkholes + catacombs exist as a vertical content layer.**
   Z=1 sinkholes, Z=2 catacombs (per IDEAS.md's seven archetypes
   + chambered burial geometry). Reuses existing Z-axis multi-floor
   substrate.
3. **Faction NPCs colonize the new geography.** Pale Curation
   anchors catacombs. Rot Choir anchors Choir Cathedral sinkholes.
   Existing factions (Villagers, Concord, Palimpsest) reframe
   their occupied zones rather than vanish.

Once these three things hold, "the world is tepui-shaped" — the
environment is cemented because the content generator produces
tepui zones by default, not as an exception.

---

## Current state synthesis

### What exists today (low blast radius to keep)

- 4 surface biome types: **Cave / Desert / Jungle / Ruins**
- Faction → biome assignment: Villagers→Cave, Concord→Desert,
  RotChoir→Jungle, Palimpsest→Ruins
- 13 builders in the pipeline: `CaveBuilder` etc.
  (`OverworldZoneManager.cs:26-74`)
- Robust Z-axis multi-floor system: `ZoneTransitionSystem` + Z>0
  underground levels via `CreateUndergroundPipeline()`
- Center cell `(10,10,0)` pinned as starting Kyakukya village
  — moving this is **🔴 high risk** (breaks `VillagePopulationBuilder.cs:17`)
- Cardinal pins (N/S/E/W of center) protected from randomization
- 80×25 fixed cell grid (resizing is **🔴 high risk** — not
  on the menu)
- 3 shipped mineral items (PaleSalt, ChoirIron, GlowQuartz) +
  3 enhancement Parts + Tinker integration

### What's missing (the overhaul target)

- 🔴 No tepui biome / mesa / plateau terrain
- 🔴 No sinkholes (no procedural cliff-walled vertical descent)
- 🔴 No catacombs / burial geometry / plaques
- 🟡 No bioluminescent light economy (LightSourcePart exists; no
  zone forces darkness)
- 🟡 No `PaleCuration` NPC blueprints in production (only inline-built
  in showcase scenarios)
- 🟡 No `DrivingBloom` faction, no `BloomedEffect` / fungal infection
- 🟢 The Root of the World is unanchored (no central landmark exists
  yet; 10,10 is the village, not the Root)

---

## Strategic option space

Three strategies for getting from current → cemented:

### Option A — Pure additive ("tepui as 5th biome")
Add `BiomeType.Tepui` alongside Cave/Desert/Jungle/Ruins. Tepuis
sit on the world map as a new POI type. Existing zones unchanged.
- ✅ Lowest blast radius. Each phase reverts cleanly.
- ❌ Tepui never becomes the *dominant* biome. The world stays
  4-biome-with-tepui rather than tepui-shaped.
- ❌ Catacombs end up as one-off content under tepuis, not a
  pervasive substrate.

### Option B — Big-bang replacement
Replace all 4 existing biomes with tepui-grammar variants. Cave
becomes a tepui-foothill, Desert becomes tepui-plains, etc.
- ✅ Forces fast adoption.
- ❌ Breaks `VillagePopulationBuilder.cs:17` (starting zone is
  Cave-typed); breaks saved games; breaks tests pinning specific
  zones; breaks compass-stone narrative pointing at
  Saltglass Dunes.
- ❌ All-or-nothing — no phase boundary lets us stop and ship a
  smaller increment.

### Option C — Additive-reframing (RECOMMENDED 🟢)
Add tepui as a new biome. Then **reframe** the existing 4 biomes
as *components of the tepui ecosystem* rather than competitors to
it. The cement is gradual: each existing biome gets a tepui-
adjacent meaning, not a replacement.

| Existing biome | Reframed role |
|---|---|
| Cave | Tepui-foothill caves (the access route between flat ground and tepui-top) |
| Desert | Tepui-surrounding plains where the tepuis rise from |
| Jungle | Tepui lower-slope rainforest (this matches IDEAS.md's "jungle lower slopes" verbatim) |
| Ruins | Pre-cataclysm catacomb-adjacent surface ruins (the link to the burial substrate below) |

Then sinkholes + catacombs slot beneath via existing Z-axis.

- ✅ Every existing biome gets cosmologically meaningful, no zones
  thrown away.
- ✅ Smallest-blast-radius per phase: phase-N can ship without
  reverting prior work.
- ✅ Existing tests stay green (zones still type-check as
  Cave/Desert/etc.; just their ecological *role* changes).
- ✅ Saved games keep working — old zones are still valid; new
  zones plug into the same pipeline.
- 🟡 The cementing is gradual not sudden — the player won't notice
  a sharp "the world changed" moment, only "more content."

---

## Recommended path: Phased plan (W.1 → W.11)

Each phase = one feature branch + 5-15 commits + 30-60 tests + a
self-contained `Docs/<phase>.md` per CLAUDE.md's
major-feature workflow.

### W.1 — Verification + decision lock-in (DOCS ONLY)
**Goal:** Pin the 5 design decisions below before any code lands.
Output: this document + answers + a `Docs/WORLD-CEMENT-PLAN.md`
covering the same path with user-confirmed answers locked in.

Decisions needed (see [Open decisions](#open-decisions-for-the-user)
section below).

### W.2 — Tepui as a new BiomeType (smallest possible step)
**Smallest blast radius slice.** Add `BiomeType.Tepui` to the enum.
Write `TepuiBuilder` that produces a plateau-top zone: interior
80×25 with impassable cliff-wall borders on N/S/E/W edges,
exits via stairs-down only (no horizontal connections to
neighbors). One POI on the 20×20 worldmap.

Population: empty for now (W.6 fills it).
Content tests: zone loads, edges are impassable, stair-down spot
exists.

**Tests target:** ~15. **Blast radius:** 🟢 LOW. Mirrors
DesertBuilder pattern; no existing zone modified.

### W.3 — First sinkhole: Stranded Settlement archetype
**Smallest sinkhole archetype.** Z=1 beneath the W.2 tepui POI.
A `SinkholeBuilder` produces a cliff-walled cavity zone with a
village in the floor (reuses `VillagePopulationBuilder` with a
new "stranded" faction-tag). Stairs-up connects back to the tepui.

This is the easiest archetype because it reuses 90% of the
village population path. The novel work is just the
cliff-walled cavity layout.

**Tests target:** ~20. **Blast radius:** 🟢 LOW. Underground
pipeline already exists.

### W.4 — Catacombs sub-feature + plaques
**Z=2 beneath the sinkhole.** `CatacombBuilder` produces a
chambered burial-network layout. New `PlaquePart` for inscriptions.
New `KnowledgeUnlock` integration for dynamic-readability
(player unlocks plaque text by accumulating
`KnowledgePart` of the topic).

PaleSalt + Choir-Iron get **first real gameplay role** here:
catacomb walls keyed to mineral palette signal the faction
alignment of whoever built them.

**Tests target:** ~30 (plaques + reading + KnowledgePart unlock
+ wall-mineral signaling + persistence). **Blast radius:** 🟡
MEDIUM. New PlaquePart + readability flow is new substrate.

### W.5 — Bioluminescent light economy
**The catacomb-zone friction layer.** Per-zone `AmbientLight` field.
Catacombs default to `0.05f` (lights only, no global ambient).
Three new content items: **GlowingFungus** (stationary wall light,
slow regrow), **LanternBeetle** (portable, decays over turns),
**LuminousSlime** (painted-on consumable). Existing `LightSourcePart`
unchanged — these just create new content with it.

**Tests target:** ~25 (per-zone ambient override + 3 new item
blueprints + decay/regrow mechanics + the dead-zone over-harvest
edge case). **Blast radius:** 🟡 MEDIUM. Touches `LightMap`
ambient resolution.

### W.6 — Tepui-top population + Pale Curation debut
**Cement the new faction.** Fill the W.2 tepui-top zone with:
- A small Pale Curation outpost (3-5 NPCs: Curator, Archivist,
  Steward + ambient population). New
  `Assets/Resources/Content/Blueprints/Objects.json` entries
  for these NPC blueprints.
- New `PopulationTable` for `BiomeType.Tepui` (different roster
  from the 4 existing biomes — no Snapjaws on a tepui-top; the
  fauna is endemic).
- PaleCuration faction's hostility/disposition with each existing
  faction (allied with Palimpsest, neutral with Concord, hostile
  with RotChoir per IDEAS.md).

This is where PaleCuration "exists" in the real game world for the
first time. The mineral-trade Scribe in the
showcase scenario can now reference a real Curation outpost.

**Tests target:** ~30. **Blast radius:** 🟡 MEDIUM. New
blueprints + new population table. Existing biomes unchanged.

### W.7 — Second sinkhole: Choir Cathedral
**Unlocks DrivingBloomEffect + ChoirIron full mechanic.** New
archetype. Z=1 beneath a *different* tepui POI (not the W.2 one).
RotChoir-aligned fauna; fungal-walled chambers; new
`BloomedEffect` status (the long-deferred ChoirIron passive).
ChoirIron's "+damage vs Fungal" gets a real proving ground.

**Tests target:** ~40 (sinkhole gen + BloomedEffect ticks/save
+ ChoirIron full integration + Choir mycelium wall mechanics).
**Blast radius:** 🟡 MEDIUM. New status effect added to the
StatusEffectsPart system.

### W.8 — Third sinkhole + spreading archetype rotation
**Drowned Sima (water + amphibians)** OR **Boneyard (apex
predators)** OR **Sealed Library (lost archives)** OR **Time-Locked
Forest** OR **Pure Catacomb** OR **Door**. Pick 1-2 based on which
unlocks the most adjacent content. Recommendation: **Drowned Sima**
+ **Sealed Library** in this phase — they each ship a new mechanic
(swimming + plaque-as-text-archive) and stop the pattern from
becoming repetitive.

**Tests target:** ~50. **Blast radius:** 🟡 MEDIUM-🔴 HIGH
depending on whether swimming becomes a real movement mode.

### W.9 — Existing-biome reframing pass
**No code change to worldgen.** Lore/text reframe:
- Cave zones get a tooltip / lore-text line: "These caves
  honeycomb the lower slopes of a distant tepui."
- Desert zones: "Plain-wind ruffles the grass at the foot of a
  far tepui."
- Jungle zones: "The canopy rises toward a tepui's lower slopes."
- Ruins: "Old surface buildings, the catacombs below them now
  sealed."
- New `BiomeContextPart` (or similar) holds the flavor text shown
  when the player enters.

Optionally: change ambient tints / glyph palettes per biome to
hint at the reframed role. Pure flavor — no mechanic change.

**Tests target:** ~5 (per-biome context-text round-trip). **Blast
radius:** 🟢 LOW. Lore-only.

### W.10 — Root of the World as central landmark
**Final-act anchor.** Replace OR augment the `(10,10,0)` center pin.
Two options:
- **A.** Center cell becomes Root of the World tepui (and Kyakukya
  village relocates to an adjacent cell). 🔴 HIGH risk — breaks
  `VillagePopulationBuilder.cs:17`.
- **B.** Add Root of the World as a NEW special POI elsewhere on
  the map (e.g., dead-center of a clustered tepui range). Center
  `(10,10,0)` stays Kyakukya. 🟢 LOW risk.

Recommendation: **B** — preserve Kyakukya as the player's home base,
introduce Root as a destination. Save the relocation for never.

The Root tepui is special: three-tier ascent (lower slopes /
mid-slopes / petrified canopy summit per IDEAS.md), the First Root
sleeps in the deepest catacomb. Final-act content lives here.

**Tests target:** ~25 (landmark placement + 3-tier zone chain +
First Root entity + final-area gate). **Blast radius:** 🟡 MEDIUM.

### W.11 — Polish, balance, Urqu placeholder
**Cleanup phase.** Things that surfaced during W.2-W.10 that need
fixing. Spotty places to add a 6th-biome (tepui) entry to existing
tests, balance fungal-density numbers, etc. Also: add an Urqu
placeholder for future content (a single `UrquManifestation` event
that fires under specific faction-collapse conditions; full Urqu
arc is post-overhaul scope).

**Tests target:** ~20. **Blast radius:** 🟢 LOW.

---

## Cumulative target

| | Phases | Tests | LOC est. | Real time est. |
|---|---|---|---|---|
| **Smallest viable** | W.1 + W.2 + W.3 + W.4 | ~95 | ~800 | 4-5 days |
| **Core cement** | W.5 + W.6 + W.7 | ~95 | ~1200 | 5-7 days |
| **Polish + landmark** | W.8 + W.9 + W.10 + W.11 | ~100 | ~800 | 4-5 days |
| **Full overhaul** | All 11 phases | ~290 | ~2800 | 13-17 days |

---

## Open decisions for the user

Before W.1 commits and W.2 begins, you need to answer these 5:

### Decision 1 — Reframing or replacing?

**Confirm Option C (additive-reframing).** Tepui ships as a 5th
biome alongside the existing 4, and the existing biomes get
gradually reframed as tepui-ecology components.

Alternative: pure additive (no reframe — tepui just exists
alongside the 4). The reframe is mostly lore + tooltip text
in W.9; it's cheap, so I default to *yes, reframe*.

### Decision 2 — Center cell strategy

**Confirm Root of the World lands at a NEW cell, not (10,10).**
Kyakukya stays at (10,10) as the starting village. Root spawns
somewhere else (deep in a tepui cluster, hand-placed).

Alternative: relocate Kyakukya. I default to **no relocation** to
keep `VillagePopulationBuilder.cs:17` happy.

### Decision 3 — Multi-floor vertical = sinkhole+catacomb depth

Sinkholes at Z=1 *beneath the tepui POI*, catacombs at Z=2 *beneath
the sinkhole*. So a complete tepui descent is 3 floors:
- Z=0 surface: tepui-top (plateau)
- Z=1: sinkhole floor (one of the 7 archetypes)
- Z=2+: catacomb chambers

Each Z-level is a separate 80×25 zone connected by stairs.

Alternative: catacombs as a side-feature of a sinkhole (i.e.
chambered into the sinkhole walls, same zone). I default to
**separate Z-level** for engine consistency — multi-floor already
works; co-zoning would require new geometry primitives.

### Decision 4 — Mechanic depth for the first ship

For W.2-W.4 (smallest viable cement), how deep does each system go?

| System | Minimal viable | Full design |
|---|---|---|
| Tepui terrain | Cliff-walled rectangle | Multi-tier ascent with cliff-edge stairs |
| Sinkhole archetypes | Stranded Settlement only | All 7 archetypes |
| Catacombs | Single chamber type + plaques | Multi-chamber generative + lineage burial |
| Plaque reading | Knowledge-gated text reveal | Dynamic unlock with cross-references + Urqu names |

I default to **minimal viable** for W.2-W.4 (ship 1 archetype, 1
catacomb chamber type, knowledge-gated reading) and expand in
W.7-W.10. This keeps the first cement ship at ~5 days, not 15.

### Decision 5 — Faction redistribution timing

When does PaleCuration get a real production NPC blueprint? Two
options:

- **5a.** Now (in W.6) — tepui-top zone is empty until then, so
  it's natural to colonize tepui with the first new faction.
- **5b.** Deferred — keep PaleCuration as inline-built scenario
  content until W.7+ when catacombs make their cosmology
  legible.

I default to **5a** — the showcase scenario's inline PaleCuration
Scribe is a documented 🟡 finding; W.6 is the right place to
retire it.

---

## Methodology reminders (per CLAUDE.md)

Each phase follows the major-feature workflow:
1. **Plan to disk** — `Docs/W<N>-<phase-name>.md`
2. **Verification sweep** — read every system the plan cites; correct
   false premises BEFORE writing code
3. **Smallest-blast-radius sub-milestones** — typically 4-6 per phase
4. **RED → GREEN → adversarial → cold-eye → commit** per sub-milestone
5. **BOTH-angle audit** at phase close (taxonomy + Qud-parity-first)
6. **Hypothesis-driven deep audit** if the BOTH-angle pass declares
   "0 real bugs" on a non-trivial phase (per the lesson captured in
   CLAUDE.md `Hypothesis-driven deep audit` section)

---

## What gets shipped first

If you confirm Option C + my defaults on decisions 1-5, **W.2** ships
first as a tiny, isolated, fully-revertable PR:

- 1 new enum value: `BiomeType.Tepui`
- 1 new builder: `TepuiBuilder` (cliff-walled 80×25 plateau)
- 1 new POI placement: `PlacePOIs()` gains a `Tepui` case
- 1 zero-population biome (W.6 fills it later)
- ~15 tests pinning: zone generates, edges are impassable, stairs
  spot exists, biome enum is reachable, POI placement non-random
  in test runs

That's a one-day ship that proves the substrate works without
touching any existing biome or faction. The rest of the path
unspools from there.

---

## Next action

User reviews this brainstorm and answers the 5 decisions. I write
`Docs/W1-WORLD-CEMENT-PLAN.md` with the locked-in answers + the
phased path with concrete sub-milestone breakdowns. Then W.2
begins as a feature branch.
