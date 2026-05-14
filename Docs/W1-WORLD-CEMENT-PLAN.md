# W.1 — World Cement Plan (Decisions Locked)

**Date:** 2026-05-13
**Status:** Plan locked. W.2 next.

> **Genre framing:** CoO is an **RPG, not a roguelike**
> (`Docs/PROJECT-IDENTITY.md`). All phase plans assume character
> persistence and save/load. Cosmic-actor mechanics (Urqu Pacts,
> Spirit Pacts, Memory-Eating) carry full permanent weight.

This is the action-oriented sibling of `WORLD-ENVIRONMENT-OVERHAUL.md`.
That doc was the brainstorm; this doc captures the locked-in answers
+ concrete sub-milestone breakdowns for the first 3 phases (W.2–W.4).

---

## Locked decisions

| # | Decision | Answer |
|---|---|---|
| 1 | Reframing or replacing? | **Option C — additive-reframing.** Tepui as 5th biome, existing 4 reframed as tepui-ecology components |
| 2 | Center-cell strategy | **Kyakukya stays at (10,10,0).** Root of the World ships at a NEW cell as a hand-placed POI |
| 3 | Multi-floor vertical depth | **Z=0 tepui → Z=1 sinkhole → Z=2+ catacomb.** Each a separate 80×25 zone connected by stairs |
| 4 | First-ship mechanic depth | **Minimal viable for W.2–W.4.** 1 sinkhole archetype (Stranded Settlement), 1 catacomb chamber type, knowledge-gated plaque reading. Expand in W.7–W.10. |
| 5 | Faction redistribution timing | **PaleCuration debuts in W.6.** Tepui-top zone is empty W.2–W.5, gets PaleCuration outpost in W.6. Retires the inline-built showcase Scribe at that point. |

These five answers govern all sub-milestone decisions below. If a
decision becomes a regret mid-implementation, revisit this doc + the
brainstorm doc together; don't ship the override silently.

---

## Phase-by-phase concrete sub-milestone breakdowns

### W.2 — Tepui as a new BiomeType

**Branch:** `feat/world-cement-w2-tepui-biome`
**Estimate:** 1 day, ~15 tests
**Acceptance:** A tepui zone generates with impassable cliff borders
  + a stairs-down spot; ships zero population (W.6 fills); plays
  identically to existing biomes from outside the new POI.

| Sub | Work | Tests |
|---|---|---|
| W.2.1 | `BiomeType.Tepui` enum + 4-biome switch-statement audit | 3 (all 4 biomes still resolve; new Tepui resolves) |
| W.2.2 | `TepuiBuilder` — interior plateau + N/S/E/W cliff borders impassable | 5 (border cells unreachable, interior reachable, glyph palette correct) |
| W.2.3 | `OverworldZoneManager` routing + `CreateTepuiPipeline()` | 3 (zone ID → Tepui pipeline; pipeline executes) |
| W.2.4 | `PlacePOIs()` adds 1 Tepui POI on the worldmap | 2 (POI placed deterministically per seed; not on the cardinal pin) |
| W.2.5 | `StairsDownBuilder` adds a single descent spot (target zone is empty until W.3) | 2 (stair exists; navigating it lands in an empty Z=1 zone) |

**Verification sweep checklist before W.2.1 starts:**
- Read `BiomeType` enum + every switch statement on it (audit
  what breaks if a new value is added)
- Read `OverworldZoneManager.CreateUndergroundPipeline` (the Z>0
  pattern W.3 will mirror)
- Read `PlacePOIs` placement logic (how do cardinals get pinned?
  Will Tepui slot into rotation cleanly?)
- Read `ZoneRenderer` glyph/color mapping (what cliff glyph reads
  visually as impassable? Probably `#` matching wall convention)

### W.3 — First sinkhole: Stranded Settlement archetype

**Branch:** `feat/world-cement-w3-stranded-sinkhole`
**Estimate:** 1.5 days, ~20 tests
**Acceptance:** Descending from the W.2 tepui's stairs-down lands
  the player in a Stranded-Settlement sinkhole — a cliff-walled
  cavity with a 4-5 NPC village in the floor. Stairs-up returns.

| Sub | Work | Tests |
|---|---|---|
| W.3.1 | `SinkholeBuilder` — cliff cavity layout (interior ring, sloped descent visualization) | 5 |
| W.3.2 | `StrandedSettlementBuilder` — village-shape population in the cavity floor | 5 |
| W.3.3 | `OverworldZoneManager` routes Z=1-beneath-Tepui → Sinkhole pipeline | 3 |
| W.3.4 | `StairsUpBuilder` connects back to the tepui above | 2 |
| W.3.5 | NPC dialect tag — Stranded villagers have a `Dialect: stranded` tag (IDEAS.md design hook); reused for W.6 dialogue gating | 3 (tag set, persists, queryable) |
| W.3.6 | Adversarial: descend + ascend doesn't corrupt zone state | 2 |

**Verification sweep checklist:**
- Read `VillagePopulationBuilder` end-to-end (the pattern W.3.2
  reuses)
- Check what happens if a creature spawn-table is empty (does the
  pipeline tolerate it for the W.2 empty case?)
- Confirm `StairsUpBuilder` doesn't assume the target is on Z=0

### W.4 — Catacombs + plaques

**Branch:** `feat/world-cement-w4-catacombs-plaques`
**Estimate:** 2 days, ~30 tests
**Acceptance:** A second stairs-down in the W.3 sinkhole descends
  into a Z=2 catacomb chamber with plaques. Plaques reveal text
  when the player has accumulated relevant `KnowledgePart`
  entries; otherwise the text is partial/illegible.

| Sub | Work | Tests |
|---|---|---|
| W.4.1 | `CatacombBuilder` — chambered burial layout (corridors + niches) | 5 |
| W.4.2 | New `PlaquePart` — inscription text + `RequiredKnowledge` list | 5 (text round-trip; required-knowledge gating) |
| W.4.3 | `ExaminablePart` integration — examining a plaque triggers reading | 4 (reading flow + readability gating + counter-check) |
| W.4.4 | `KnowledgePart.HasKnowledge(topic)` → plaque text resolution layer | 4 (full-knowledge → full text; partial → partial; no knowledge → silhouette text) |
| W.4.5 | Wall-mineral signaling — chamber walls tagged with the mineral palette (PaleSalt/ChoirIron/GlowQuartz) signal the faction-of-builders. New `MineralVeinPart` on wall cells. | 5 (vein placement deterministic per faction; visible glyph differs by mineral) |
| W.4.6 | Adversarial: round-trip a catacomb zone with plaques + verify text gating still works post-load | 5 |
| W.4.7 | Cold-eye + BOTH-angle audit | 2 (regression pins) |

**Verification sweep checklist:**
- Read `ExaminablePart.BuildExamineLine` (already extended in
  E.5.3 — now it'll also append plaque text); confirm
  composition with multiple Parts emitting examine text works
  cleanly
- Read `KnowledgePart` + `FactBag` (current knowledge model;
  W.4.4 builds on this)
- Verify `MaterialPart` round-trip is robust (catacomb walls
  with MaterialTags need to survive save/load — pinned in the
  E.5.1 audit's MaterialPartRoundTripTests)
- Audit existing item-described systems for the "partial-knowledge
  partial-text" pattern (does CoO already have a 'understood
  vs. unidentified' precedent? If not, we're introducing it)

---

## Phases W.5–W.11 — concrete enough to start, leaving wiggle room

The brainstorm doc has prose-level outlines for these. They'll get
their own `Docs/W<N>-*-PLAN.md` when their turn comes. **Do NOT
write the W.5+ plans yet** — verification sweeps for those depend
on what we learn shipping W.2–W.4. Premature locking is a known
methodology mistake.

That said, the cross-phase dependencies are:
- **W.5 (light economy)** depends on **W.4** (catacombs are the
  first dark-by-design zone)
- **W.6 (Pale Curation debut)** depends on **W.2** (tepui-top zone
  needs to exist to be populated)
- **W.7 (Choir Cathedral + DrivingBloomEffect)** depends on **W.3
  pattern (sinkhole pipeline)** — not on W.6
- **W.8 (Drowned Sima + Sealed Library)** depends on **W.7**
  (proves the multi-archetype pattern scales)
- **W.9 (lore reframe)** depends on **NONE** — could ship any
  time, but parks beside W.10
- **W.10 (Root of the World landmark)** depends on **W.2** (need
  the tepui biome registered)
- **W.11 (Urqu placeholder + polish)** depends on **everything
  else** — last

A possible mid-stream reshuffle: if shipping W.5 (light economy)
turns out to be much harder than estimated, defer it to after W.6
since W.6 doesn't depend on it.

---

## What the path explicitly does NOT include (yet)

These were in the brainstorm IDEAS.md survey but are explicitly
out-of-scope for W.2–W.11:

| Deferred system | Reason | Likely future placement |
|---|---|---|
| **Bloc-system** (faction coalitions) | Adds a diplomatic layer that's not load-bearing for environment cement | Post-W.11 |
| **Sect-system** (splinter mini-factions) | Same | Post-W.11 |
| **House-Drama internal voting** | Affects narrative/social, not environment | Post-W.11 |
| **Spirit affinity (Inquiry/Bloodlust)** | Player-behavior-driven, orthogonal to environment | Separate parallel track |
| **Urqu Pacts + Recanting** | Late-game cosmology, needs Spirit foundation | Post-Spirit |
| **Preservation methods** (Ink-Bath, Honey-Sealed, Resin-Cast, Bog-Taken) | Body-disposition mechanics; orthogonal to terrain | Post-W.11 |
| **Tinker bit-refining tier system** (raw → cut → infusable → architectural-grade) | Mineral-economy depth | Post-W.11; revisits E.5+ polish queue |
| **Memory-Marble + Sari-Pyrite + 3 new minerals** | Stones-with-Stake content expansion | Post-W.4 (catacombs give the existing 3 minerals their first real role) |
| **Lineage / descendant-readable burial** | Long-horizon player-history mechanic | Post-Root |

This list isn't apologetic — it's discipline. The environment cement
ships when tepui+sinkhole+catacomb+light-economy+PaleCuration are
working. Everything above is on the IDEAS.md table for after that.

---

## Action: cut W.2

Once this plan + the introspection doc are reviewed, the next
concrete step is:

```
git checkout main
git pull origin main
git checkout -b feat/world-cement-w2-tepui-biome
```

Then begin sub-milestone W.2.1 (BiomeType enum + audit).
