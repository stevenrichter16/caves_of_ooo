# What W.2 – W.4 Entail (Concrete)

**Companion to** `W1-WORLD-CEMENT-PLAN.md`. That doc has the
sub-milestone breakdowns + test budgets. This doc answers the
plainer question: **what will the player actually experience, and
what code lands?**

---

## Combined player experience after all three phases

Starting from Kyakukya village (10,10,0), the player can:

1. **See a new POI** on the worldmap — a tepui mesa, visually
   distinct from cave/desert/jungle/ruins.
2. **Walk to the tepui POI**, enter it, find themselves on a
   plateau (80×25 zone) with **impassable cliff borders on all
   four edges**. No way off except a marked **stairs-down spot**
   somewhere along the cliff edge.
3. **Descend the stairs** into a **Stranded Settlement sinkhole**
   one floor below (Z=1) — a cliff-walled cavity with a **small
   isolated village** of 4-5 NPCs at the floor. NPCs are passive
   but suspicious; they speak in a `Dialect: stranded` tag (gates
   later content). Stairs-up returns to the tepui.
4. **Find a second stairs-down** in the sinkhole leading to a
   **catacomb chamber** at Z=2 — chambered burial-network corridors
   with niches. **Plaques** are inscribed on the walls.
5. **Examine the plaques.** If the player has accumulated
   relevant `KnowledgePart` entries (faction lore, mineral
   history, language fragments), the plaque text reads fully.
   Without the right knowledge, the text is partial / fragmentary
   / illegible.
6. **See mineral veins in the catacomb walls** — Pale-Salt,
   Choir-Iron, Glow-Quartz vein patterns colorize the walls by
   the **faction-of-builders** who carved the chambers. Pale
   Curation chambers vein with Pale-Salt; Rot Choir with Fungal
   mycelium; Saccharine Concord with Glow-Quartz. **The minerals
   the player has been Tinker-applying since E.3 now have a
   diegetic home.**

This is the smallest viable cement of the new world. Tepui as
a visible surface biome → sinkhole as a hidden underlayer →
catacombs as a buried archive where the existing item system
suddenly has cultural meaning.

---

## W.2 — Tepui as a new BiomeType

**Goal:** Make a tepui zone exist and be walkable.

### What ships

- **1 new enum value:** `BiomeType.Tepui` (`WorldMap.cs:3-8`).
- **1 new builder:** `TepuiBuilder` — generates an 80×25 zone with
  `WallPart` on all border cells (impassable), interior open
  floor, distinctive glyph/color palette (visible plateau-top
  feel: maybe pale stone color, sparse vegetation glyphs).
- **1 new biome pipeline:** `CreateTepuiPipeline()` in
  `OverworldZoneManager.cs` — mirrors the existing
  `CreateCavePipeline()` / `CreateDesertPipeline()` shape.
- **1 POI placement update:** `PlacePOIs()` in `WorldGenerator.cs`
  rolls 1 tepui POI per worldgen seed, placed deterministically
  on the 20×20 worldmap (not on the cardinals, not at (10,10)).
- **1 new stairs-down spot** on the tepui's cliff edge — the
  cliff has one "passable" cell that's actually a staircase.
  Target zone: empty placeholder (W.3 fills it).
- **Zero population at this stage.** Tepui-top has no NPCs, no
  loot, no creatures. Just terrain. (W.6 fills it.)

### Files touched (estimated)

| File | Change |
|---|---|
| `WorldMap.cs` | +1 enum value |
| `WorldGenerator.cs` | +1 POI placement case |
| `OverworldZoneManager.cs` | +1 pipeline routing case + factory method |
| NEW `TepuiBuilder.cs` | New builder class |
| NEW test fixture | ~15 tests pinning the contract |
| `PopulationTable.cs` | +1 entry for Tepui (empty roster for now) |

### Player-visible delta

Before W.2: 4 biomes (Cave/Desert/Jungle/Ruins).
After W.2: 5 biomes. Walking onto a tepui visibly looks different
from the others, and there's an obvious "go down here" cliff-edge
spot.

### Agent-pace effort

~2-3 hours of agent coding + 1-2 Unity reload cycles per
sub-milestone test verification. **Single focused session.**

---

## W.3 — First sinkhole: Stranded Settlement

**Goal:** When the player descends from the tepui, they land in a
populated sinkhole.

### What ships

- **1 new builder:** `SinkholeBuilder` — produces a cliff-walled
  cavity layout. Cliff cells around the edges, interior open
  floor representing the sinkhole bottom.
- **1 new builder:** `StrandedSettlementBuilder` — populates the
  sinkhole floor with a village blueprint (4-5 NPCs: a leader,
  a craftsperson, ambient villagers). Reuses
  `VillagePopulationBuilder`'s NPC blueprint patterns.
- **1 new NPC tag system entry:** `Dialect: stranded` — every
  Stranded Settlement NPC carries this tag. Future content
  (W.6+ dialogue, lore reading) keys off it.
- **`OverworldZoneManager` routes** Z=1 beneath a Tepui POI →
  Sinkhole pipeline.
- **`StairsUpBuilder` connects** the sinkhole's exit back to the
  tepui above.

### Files touched (estimated)

| File | Change |
|---|---|
| NEW `SinkholeBuilder.cs` | New builder |
| NEW `StrandedSettlementBuilder.cs` | New builder |
| `OverworldZoneManager.cs` | +1 routing case for Z=1-beneath-Tepui |
| NEW test fixture | ~20 tests |
| `PopulationTable.cs` | +1 entry for Stranded village |
| Possibly new NPC blueprints in `Objects.json` (3-5 NPCs) | New content |

### Player-visible delta

Before W.3: descending the tepui stairs lands you in an empty Z=1
zone.
After W.3: descending lands you in a hidden village with 4-5
suspicious villagers. They speak (passive dialogue), they're
clearly different from surface Villagers — the dialect tag is the
first hint that this place has been isolated for generations.

### Agent-pace effort

~3-4 hours of agent coding (more builder logic + content
blueprints + dialect tag wiring). **Roughly a session-and-a-half.**

---

## W.4 — Catacombs + plaques

**Goal:** The deepest layer where the minerals get their
cosmological meaning + readable lore.

### What ships

- **1 new builder:** `CatacombBuilder` — produces a chambered
  burial-network layout. Corridors connecting niches; each niche
  is a small cell or cluster of cells with a burial marker
  glyph. Layout is generative (different per seed).
- **1 new Part:** `PlaquePart` — an inscription attached to an
  entity (placed on walls / niches). Has `InscriptionText` +
  `RequiredKnowledge` list (string topics that must be in the
  player's `KnowledgePart` for the text to fully read).
- **`ExaminablePart` integration:** when player examines a plaque,
  if `KnowledgePart.HasKnowledge(topic)` for all required topics
  → full text. Partial → partial. Otherwise → fragmentary
  glyph-noise. (Reuses the `GetEffectDescription`-style append
  pattern from E.5.3.)
- **1 new Part:** `MineralVeinPart` — wall cells get this tagged
  with `MineralType: PaleSalt | ChoirIron | GlowQuartz`. Renders
  as a colorized vein pattern on the wall glyph. Catacomb chambers
  are vein-themed by their builder-faction (each chamber has a
  dominant mineral palette signaling who carved it).
- **`StairsUpBuilder`** connects the catacomb back to the sinkhole
  above.

### Files touched (estimated)

| File | Change |
|---|---|
| NEW `CatacombBuilder.cs` | New builder |
| NEW `PlaquePart.cs` | New Part |
| NEW `MineralVeinPart.cs` | New Part (or extend `MaterialPart`) |
| `ExaminablePart.cs` | +plaque text resolution path |
| `KnowledgePart.cs` (likely existing) | confirmed query API |
| NEW test fixture (likely 2 files) | ~30 tests |

### Player-visible delta

Before W.4: descending from the sinkhole lands you in another
empty zone.
After W.4: you land in a multi-chamber catacomb. Walls visibly
colorized by mineral vein palette (so you can see "this chamber
was Pale Curation's"). Plaques you can examine — text quality
depends on what you know. **The first time the player thinks:
"oh, this is where the minerals come from."**

### Agent-pace effort

~4-5 hours of agent coding (most novel substrate: PlaquePart +
MineralVeinPart + KnowledgePart-gated text + cold-eye/adversarial
sweep at phase close). **Roughly 1 full session.**

---

## Total revised estimate (agent-pace, RPG-framed)

| Phase | Sub-milestones | Tests | Agent-hours | Calendar |
|---|---|---|---|---|
| W.2 | 5 | ~15 | 2-3 | half-day |
| W.3 | 6 | ~20 | 3-4 | half-day |
| W.4 | 7 | ~30 | 4-5 | full day |
| **Total W.2-W.4** | **18** | **~65** | **9-12 hrs** | **1-2 days** |

This is the **smallest viable cement** of the tepui/sinkhole/
catacomb world. After it ships and you play through, we
re-evaluate before committing to W.5+.

---

## What needs your sign-off before W.2 cuts

From the brainstorm doc's outstanding questions:

1. **Root cell strategy** — keep Root at NEW cell (current
   default), OR pivot to "Root at (10,10), Kyakukya at the foot of
   the world-tree"? The second is engine-safe AND thematically
   stronger. **Recommendation: pivot.** (Doesn't affect W.2-W.4 —
   matters at W.10.)
2. **W.2.5 density bump** — add a sub-phase between W.2 and W.3
   that bumps tepui-POI count from 1 to 4-6 across the worldmap?
   Without it, "there's a tepui out there somewhere" is the
   feeling. With it, "tepuis are *the* defining surface feature"
   is the feeling. ~1 extra hour. **Recommendation: yes, add it.**

Confirm both (or override) and I cut `feat/world-cement-w2-tepui-biome`.
