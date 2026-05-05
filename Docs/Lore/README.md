# Lore

The world's lore lives in three layers. Pick where you want to land.

---

## Tier 1 — Five minutes

Start here. Plain English. No deep terms without a gloss.

- **[Palimpsest_Primer.md](Palimpsest_Primer.md)** — what the world is,
  in 250–500 words. The cataclysm, the two cosmic forces, the five
  cultures, and the central choice.
- **[Palimpsest_Glossary.md](Palimpsest_Glossary.md)** — every deep
  term in the corpus, with a plain-English label you can use instead.
  Reference, scanned not read.
- **[StarterNPCs.md](StarterNPCs.md)** — the seven faction-faces a
  player would meet first. Junior cataloguer, market apprentice, road-
  walking bowl-maker, smoke-born hearth-log keeper, unfired child, new
  tendril, first-time Echo-Reader.

---

## Tier 2 — Half an hour

One page per faction. ~700 words each. Image-led, with pointers into
the deep work.

- **[Factions/PaleCuration.md](Factions/PaleCuration.md)** — the
  archivists, twenty years of work remaining.
- **[Factions/SaccharineConcord.md](Factions/SaccharineConcord.md)** —
  the traders, eleven years of alloys remaining.
- **[Factions/BrineCommunion.md](Factions/BrineCommunion.md)** — the
  sea-people who carry the sea inland.
- **[Factions/Thermoclaves.md](Factions/Thermoclaves.md)** — the
  fire-keepers; the tenth fire tends itself.
- **[Factions/GlassblownRemnant.md](Factions/GlassblownRemnant.md)** —
  the silent glass-makers who refuse to make anything permanent.
- **[Factions/RotChoir.md](Factions/RotChoir.md)** — the floor under
  your feet, awake and hungry.
- **[Factions/ThePalimpsest.md](Factions/ThePalimpsest.md)** — the
  writing under the writing.

---

## Tier 3 — Hours

The deep work. For the obsessive.

- **[Palimpsest_Lore_Bible.md](Palimpsest_Lore_Bible.md)** — the
  canonical source of truth. Originally authored as a Word document,
  converted to markdown for in-repo stability. Not edited; the audits
  propose amendments to it without modifying it.
- **[Palimpsest_Lore_Compendium.md](Palimpsest_Lore_Compendium.md)** —
  the editorial monolith (~40,000 words). Every audit's strongest
  material, organised by topic into one document: cataclysm → Choir →
  Palimpsest → seam → factions → cosmology → apotheoses → comparative
  tables → cross-faction → documentary corpus → bible amendments.
- **[Palimpsest_Lore_Graph.html](Palimpsest_Lore_Graph.html)** —
  interactive force-directed atlas. 105 nodes, 186 links, across 9
  node types. D3.js, single self-contained file. Click for details,
  drag, scroll-zoom, type filters, search.
- **[Qud_Lore_Graph.html](Qud_Lore_Graph.html)** — comparison atlas:
  the same graph framework applied to Caves of Qud lore. 79 nodes, 133
  links, sourced from the Caves of Qud wiki and TV Tropes.
- **[audits/](audits/)** — 22+ timestamped lore audits run between
  2026-04-30 and 2026-05-04. Each is a depth-and-character review of
  the canonical bible plus shipped content, with concrete suggested
  additions. Driven by a Claude Routine; never edits the bible
  directly. See `audits/README.md` for the cadence.

---

## How the lore was made

The bible is the source of truth. The audits are *suggestion documents*
— a cycle of routine-driven reviews that read the bible and propose
specific additions in the bible's voice, without modifying it. The
compendium is editorial: an attempt to consolidate the audit cycle's
strongest material into one organised reference. The graphs are
read-only visualisations of the corpus's structure.

The simplification plan
([SIMPLIFICATION-PLAN.md](SIMPLIFICATION-PLAN.md)) added the Tier 1
and Tier 2 surfaces — primer, glossary, faction one-pagers, starter
NPCs, and this index — *without* modifying the deep work. The deep
work was preserved untouched; what was missing was a legible entrance.
The simplification adds the entrance.

---

## Conventions

- **Casual labels in narrative; deep terms in linguistics-deep
  sections.** First-use glossing is enough; afterward, use whichever
  fits.
- **Bible is canon. Audits propose. Compendium synthesises. Graphs
  visualise. Surface introduces.**
- **Never modify the bible or the audits in normal work.** Bible
  amendments come through a deliberate process (four are proposed in
  the audits but not yet committed). Audits are immutable —
  retroactive editing would break the audit cadence's record-keeping.

---

## File map

```
Docs/Lore/
├── README.md                       ← you are here (S5)
├── SIMPLIFICATION-PLAN.md          ← the plan that added Tier 1+2 surfaces
│
├── Palimpsest_Primer.md            ← Tier 1 surface (S2)
├── Palimpsest_Glossary.md          ← Tier 1 reference (S1)
├── StarterNPCs.md                  ← Tier 1 NPCs (S4)
│
├── Factions/                       ← Tier 2 (S3)
│   ├── PaleCuration.md
│   ├── SaccharineConcord.md
│   ├── BrineCommunion.md
│   ├── Thermoclaves.md
│   ├── GlassblownRemnant.md
│   ├── RotChoir.md
│   └── ThePalimpsest.md
│
├── Palimpsest_Lore_Bible.md        ← Tier 3 canon
├── Palimpsest_Lore_Compendium.md   ← Tier 3 monolith
├── Palimpsest_Lore_Graph.html      ← Tier 3 atlas
├── Qud_Lore_Graph.html             ← Tier 3 comparison atlas
│
└── audits/                         ← Tier 3 audit reports
    ├── README.md
    ├── ROUTINE_PROMPT.md
    └── YYYY-MM-DD-HHMM-lore-audit.md  (22+ files)
```

---

*Sub-milestone S5 of the simplification plan. The index is intentionally
short — its job is to point, not to recapitulate. For prose, see the
primer; for terms, see the glossary; for atlas, see the graph; for
canon, see the bible.*
