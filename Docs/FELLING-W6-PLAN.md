# FELLING W6 — The Stump (the Root-slope)

> Phase plan per CLAUDE.md's major-feature workflow. Canon authority:
> `Lore/` (10_Bible.md); design: `Docs/FELLING-WORLD-DESIGN.md` §3.6
> (the tepui), §7.6 (state-reactive spawning), roadmap §10 (W6 line).
> W6 is one of "the two big ones" (the other was W5).

## 0. Scope

The central tepui — the petrified stump of the god-tree — as a
playable region: elevation bands as the content key, the terrain
builder family that makes the mountain read as ONE OBJECT (the
Grainfield), the band bestiary with state-reactive spawns, Olderdeep's
founding-village identity + the Rooted's chamber, the Felling-Site
(authored, tile-level), and the Sealed Library vault.

**Explicitly OUT (scope-prune, with owners):**
- The Staking ritual and every ending verb — W8 (the god-clock phase).
- Dohren's god-dialogue — W8. W6 ships the CHAMBER and the body in its
  embrace-pose; sleeping on the patch produces the *meeting* as
  authored text, not the finale conversation.
- The Root of the World zone at (3,3) — W8's god-room. Stays reserved.
- Recension survey stations + Tepui Bronze specimen economy — W7/W8
  (Recension scrip, §7.8).
- The Thinning clock (§7.7) — W8. The FactBag flags W6.3 reads are
  written by EXISTING systems (Bloom front) or authored constants.
- Waterfall shrines — fold what fits into W6.2's landmark stamps;
  anything needing new interaction verbs defers.
- Tepuibone veins/slurry economy — a mineral-vein stamp ships in W6.2
  (reuses MineralVein tag + GroveLaw-style dig hooks); the slurry
  liquid's uses defer.

## 1. Verification sweep — corrections table

| # | Claim (from design/plan) | Verified reality | Correction |
|---|---|---|---|
| 1 | "T cells are the tepui" | BiomeRows: T region ≈ cols 1-5 × rows 1-5; `BiomeType.Stump` exists; tint (0.98,0.93,0.92) already pink-grey (OverworldZoneManager.cs:869) | none — W0 prepared this |
| 2 | Stump generates as placeholder | `CreateStumpPipeline` = CaveBuilder(SeedChance 50, NoiseThreshold .44) + generic surface spine (:552-555) | W6.2 replaces the terrain builder, KEEPS the spine (connectivity/landmarks/hazards/containers/population slots) |
| 3 | Stump population | `PopulationTable.GetBiomeTable(Stump,*)` returns CaveTier1/2/3 — borrowed caves | 🔴 band tables are new content (W6.3) |
| 4 | Band bestiary "exists in the bestiary" | Blueprint census: ONLY MawToad exists. SariSnake, GlasspaneFrog, SummitSinger, SkySari, CascadeFather, Wardline, YellowfootWayfarer, BrocchiniaSentinel, PrickleBrowGecko, HelmwoodFrog: absent | 🔴 ~10 creatures to author, WITH sprites (standing rule) |
| 5 | "state-reactive spawn hooks" | `PopulationTable` has no predicate support; `FactBag` ships (Get/Set/Has, int-valued) on NarrativeStatePart | W6.3 extends PopulationEntry with an optional world-flag predicate |
| 6 | The Root (3,3) / Felling-Site (3,5) reserved | WorldMapAuthoring docstring lists them as reserved-not-placed; TierRows carry the two '5's; no POI stamped | W6.5 places Felling-Site POI; Root stays reserved (W8) |
| 7 | Olderdeep already a village? | W5: Olderdeep (4,6) routes to a STANDARD StrandedSettlement floor. Founding identity = canon 02_Geography.md:73 (the Rooted IN the patch-chamber, Listening Tradition, theological center) | W6.4 layers a PROFILE on the existing floor (the W4.6 profile mechanism — switch on Profile, never on Name) |
| 8 | Sealed Library "on the slopes, never marked" | The found-not-shown machinery (Visited-gated map render) shipped in W5.7; SinkholeArchetype is append-only; the structural pin will DEMAND a mouth the moment the enum member lands | W6.6 adds the archetype + a slope mouth in the same commit or the pin goes red — the pin is the sequencing contract |
| 9 | Cloud overlay "first legitimate use" | cloud tile channel is render-only today | W6.2 summit fog rides it; PlayMode look-pass item (headless bound) |
| 10 | Formation families are per-phase | Formation.cs enum: Spread/Beating/Sodden/Grovelands families exist; selection via FormationSelector.StableIndex (pure) | W6.2 adds the Stump family the same way |

## 2. Content-readiness

- 🟢 Band geometry (pure function over authored rows) — data only.
- 🟢 Olderdeep floor exists end-to-end (W5); profile mechanism proven (W4.6/W4.7).
- 🟢 Sinkhole machinery for the Library (W5, incl. rehydration + hidden mouths).
- 🟡 Terrain builders — new family, but every primitive (ridge masks ≈ Braid, terraces ≈ W5 descent, domes ≈ Radial) has a shipped cousin.
- 🔴 Bestiary: 10 creatures + sprites + tables. The single biggest content lift.
- 🔴 The Rooted's chamber: new authored content (body entity, plume-patch, sleep hook).
- ⚪ Felling-Site: pure authoring, tile-level, no new systems.

## 3. Sub-milestones (smallest blast radius first)

**W6.1 — The bands as data.** `StumpBands.BandAt(wx,wy)` — pure
function over the T region: Summit (inner), Slopes (mid ring),
Foothills (outer ring + G-adjacent T edge). Band-keyed ambient tint
shift (warm base → cool bright summit) on Stump zones. Tests: total
coverage of T cells, counter (non-T = None), determinism, the tint
ladder's direction.

**W6.2 — The mountain reads as one object.** StumpTerrainBuilder
family + Formation Stump members: Grainfield (parallel `═` ridges,
SAME compass direction in every slope chunk — the signature),
CascadeGorge (terrace + spray pools, foothills), SummitScrub (stone
domes + Tank-Brocchinia), RimForest (green crack). Tepuibone vein
stamp. Sprites: grain ridge, spray pool, brocchinia tank, stone dome,
tepuibone vein (+ variants where stamped in bulk — the W5 wallpaper
lesson). Reachability via the shared FormationReachability contract.

**W6.3 — What lives at each height.** Band-keyed population tables
(replace the borrowed cave tables); the ~10 creatures in two waves —
(a) foothill/lowland: SariSnake, GlasspaneFrog, CascadeFather,
Wardline, YellowfootWayfarer; (b) summit/sima: SummitSinger,
BrocchiniaSentinel, SkySari, PrickleBrowGecko, HelmwoodFrog — each
with blueprint + 16×16 sprite + CreatureSprites entry (the loads-audit
pins the census). §7.6: `PopulationEntry.RequiresWorldFlag` /
`ForbidsWorldFlag` read from a world-flag store backed by
NarrativeStatePart.FactBag; indicator species wired (SariSnake ~
UrquActive; Summit Singer silence = the alarm is W8's clock — W6 ships
the predicate machinery + one live wiring).

**W6.4 — Olderdeep, the Founding.** Profile "FoundingVillage" on the
(4,6) floor: larger chamber, the Rooted's chamber annex — Dohren's
body in the embrace-pose (fungal plume erupting from the torso, arms
toward the east wall), the plume IS the founding patch (HearthPatchPart
on the plume tiles — war rules apply to the god's own body). Sleeping
on the patch = the meeting (authored dream text via the existing
rest/sleep seam). Listening Tradition dialogue for its villagers.
Sprites: the Rooted (multi-tile? no — one 16×16 body + plume tiles),
plume-patch variant.

**W6.5 — The Felling-Site.** Authored POI + zone at (3,5), tile-level:
the circle where the bark went soft, six bare positions where nothing
grows (1,080 years), the empty seventh — a single POINT where the air
is wrong (highest Urqu-bleed; a standing effect cell, not a zone).
Never a quest hook yet; the place precedes the plot.

**W6.6 — The Sealed Library.** `SinkholeArchetype.SealedLibrary` + a
slope mouth (coined name, the W5.6 pattern — canon names no site) in
the SAME commit (the structural pin enforces this). The floor: a vault
walled in Tepuibone/Memory-Marble/Choir-Iron, one LockPart door, NO
key (the key is a future quest-arc — the vault ships locked and
canon says that is the point). Never marked (found-not-shown already
gates it).

**W6.7 — Close-out.** Both audit angles + hypothesis-driven RED pass +
adversarial gate + design-gate sweep + honesty bounds + exit statement.

## 4. Performance / observability

- Band lookup is per-zone-generation, not per-frame — pure function, no cache needed.
- Population predicates: per-spawn-roll dictionary lookups on FactBag — cold path (worldgen), fine.
- The summit cloud overlay is per-frame render — reuse the EXISTING cloud channel (render-only today); no new per-frame allocation permitted (PERF-FOUNDATION rules); look-pass to verify.
- Diag: `worldgen/FloorArchetype` extended with band; new `worldgen/StumpFormation` record; population predicate rejections emit `population/FlagRejected` (observability rule: every gate that can reject emits).

## 5. Risks

- **R1 — The band map is authored data over authored rows;** drift
  between BiomeRows' T region and StumpBands is a silent hole. Pin:
  every T cell has a band and every banded cell is T.
- **R2 — 10 creatures is sprite-heavy;** budget per creature is one
  16×16 + blueprint + table rows. No creature ships glyph-only (the
  standing rule); the loads-audit already enforces file existence.
- **R3 — Olderdeep profile must not disturb the three OTHER
  settlements** (Lampwell/Spivenor + the W5 tests). Profile switch, not
  name switch (W4.6 lesson), counter-pinned.
- **R4 — The Rooted's plume-as-hearth:** HearthPatchPart declares war
  against CatacombFolk; Olderdeep is the theological CENTER, so war at
  the founding patch should be at least as heavy. Same part, same
  ledger — verify the faction is CatacombFolk there, not a new one.
- **R5 — SealedLibrary enum member turns the structural pin red until
  its mouth lands.** Deliberate: enum + mouth + archetype case ship in
  one commit.
- **R6 — Formation determinism:** StableIndex(saltedKey), never
  builder rng (the R3/W5 rule), for the Grainfield's compass direction
  (which must ALSO be the same across chunks — a world-level constant,
  not per-chunk roll).

## 6. Implementation log

(appended per sub-milestone)

---

### W6.1 — The bands as data (SHIPPED)

`StumpBands`: authored band rows in the BiomeRows house style (NOT
derived geometry — the T region is irregular; a distance formula
misclassifies the east shoulder). 21 T cells → 11 Foothills / 5
Slopes / 5 Summit, the summit a plus-shape over the Root. Band tint
wired into OnZoneGenerated: warm base → cool bright summit
(TintFor pure, pinned; integration pinned on generated (3,3) vs
(2,1) zones). R1's two coverage fences pinned in both directions.

Tests: 7170 → 7178 (+8). All green, flaky included.

---

### W6.2a — The Grainfield + the Cascade gorge (SHIPPED)

Formation family opened: `ForStump(band, zoneID)` — band-keyed pools,
pure in the address; W6.2a pools carry only shipped formations
(Slopes=Grainfield, Foothills=CascadeGorge, Summit empty until
W6.2b). `StumpFormationBuilder` (3100, W5.7-corrected delta contract
with targeted repair). GrainRidge blueprint + 4-face sprite set
(bulk-stamped → FixtureVariantCounts); SprayPool rides the water
tileset + '~' animation family.

**Two RED→findings en route, both real:**
1. The W0 base terrain (CaveBuilder 50/0.44, "rock and fissure")
   measured ~86% WALL and often uncrossable (probe: open=245/1794,
   crossed=false) — not a playfield, and canon's bands are all
   walkable country. Swapped to open stone with outcrops
   (DesertBuilder 0.90/0.05); the W0 routing pin re-baselined from
   "uses the rock palette" to "is rock-country you can WALK".
2. Opportunistic LAIRS could claim tepui cells — one took (2,2) at
   seed 42 and the slope generated as a snapjaw den (the probe's
   SnapjawHunter=4 census was the tell). Canon: the mountain is
   "designed sequence, not garrison" — the Stump joins the Overwrit
   in both opportunistic exclusions.

Tests: 7178 → 7188 (+10). All green, flaky included.

---

### W6.2b — The summit family (SHIPPED)

Pools widened (Slopes: grain ×2 + buttress; Summit: scrub ×2 + rim);
three builders: ButtressRidge (ridge spokes fanning downhill, reusing
GrainRidge — a buttress IS grain, bent), SummitScrub (radial dome
blobs + tank-brocchinia), RimForest (a winding two-line dwarf forest,
crossable through the scrub — Tree/Bush reuse, no new art needed).
StoneDome ships 3 faces (bulk-stamped); TankBrocchinia one. The
formation tests were made pool-widening-proof: grainfield chunks are
found by ASKING the selector, not hard-coded (the W6.2a addresses
would have silently rolled ButtressRidge).

Proper assertion-RED this time (the enum existed, so the 4 new tests
failed on assertions against the empty summit pool, not on compile).

Tests: 7188 → 7193 (+5, one W6.2a test restructured). All green.

---

### W6.2 cold-eye fix wave — 2026-08-30

A 62-agent review of W6.1–W6.2 (taxonomy / canon-parity / worldgen-
integration / player-flow lanes, 2-vote adversarial verify). 30 verify
agents were killed mid-run by a spend limit, so the list is PARTIAL —
findings past ~13 never got a second vote and were dropped rather than
shipped unverified. 12 confirmed; all 12 are dispositioned below.

| # | Sev | Finding | Disposition |
|---|-----|---------|-------------|
| 1,2,6 | 🔴 | **The tepui rendered as a cactus desert.** The W6.2a base swap took `DesertBuilder`'s STOCK content: `Sand` floor — which `ResolveGroundMaterial` maps to the Beating's own `GroundMaterial.Sand`, blueprint-keyed with no biome awareness — plus a hard-coded, non-configurable 3% cactus scatter (~48 per chunk, summit included). `GrainRidge`'s examine text said "pink-grey stone" while the ground under it examined as sand. My W6.2a commit called the swap "open stone with outcrops", which was wrong. | FIXED — `TepuiStone`/`TepuiWall` blueprints, a `GroundMaterial.Tepui` with its own 16-tile macro set + 4 wall variants (recoloured from sandstone toward rose-grey: the ground tier paints at AUTHORED colour, unlike the fixture tier, so a recoloured *blueprint* could not have worked); `CactusChance`/`CactusBlueprint` fielded on DesertBuilder (default preserves the desert exactly, pinned) and set to 0 for the Stump |
| 3,12 | 🟡🔵 | The re-baselined W0 routing pin asserted only crossability — which another test already covers — so deleting the whole `BiomeType.Stump` route would have stayed green. Its comment also promised an outcrop assertion it did not make. | FIXED — asserts `TepuiStone` again (pipeline-discriminating, like every sibling pin); comment matches |
| 4 | 🟡 | The Stump lair/camp exclusion shipped with no test pin, unlike the Overwrit exclusion it mirrors — deleting either `continue` stayed green. | FIXED — 12-seed twin pin |
| 5 | 🔵 | The band-tint integration pin asserted the BLUE channel only (so drift in the warm half passed silently) and duplicated the base-tint literal. | FIXED — full colour, one shared `StumpBands.BaseTint` seam that `GetBiomeTint` now reads |
| 7 | 🟡 | Spray zones never got Drip motes — canon pairs them with the water animation, which came free with the ground material while the motes did not. | FIXED — `SprayPool` registers a Drip anchor, capped like the trees |
| 8 | 🟡 | Summit cloud overlay: the plan assigned it to W6.2, it shipped nowhere, and no deferral was recorded. | DEFERRED, recorded here — it is a per-frame render overlay riding the existing cloud channel, so it belongs with the look-pass that can actually judge it, not with a headless terrain milestone |
| 9 | 🟡 | Tepuibone vein stamp: promised in the W6.2 plan line, not shipped, not recorded as moved. | DEFERRED to **W6.3** — the veins are `MineralVein`-tagged content whose only current consumer is the dig-law hook; they land with the band content pass, not the terrain pass |
| 10 | 🟡 | Tank-Brocchinia's examine text narrates drinking, but it ships Examinable-only — canon's affordances (drinkable rainwater, studyable, glow-fungus inoculation for portable light) are unrecorded. | DEFERRED, recorded — drink/study/inoculate are three new interaction verbs; that is an affordance milestone, and the examine text is written so it describes what people DO here rather than promising the player a button |
| 11 | 🔵 | GrainRidge ships `'='` where canon says "`═`-class glyphs mapped to CP437". | **DOCUMENTED DIVERGENCE.** `'='` is the project-wide ASCII-tier convention for this shape (DescentLedge, PlaqueWall, roads all use it), GrainRidge has real sprite art so the glyph is a fallback only, and U+2550 renders as `?` (CP437TilesetGenerator returns the fallback for non-CP437 codepoints). The correct form is raw byte 205, which would be the FIRST non-ASCII RenderString in the entire content pack — real encoding risk across the JSON loader for a 🔵. Not worth it standalone; revisit if a CP437-glyph pass ever happens. |

**Fixture-divergence caught while fixing:** `WorldMapTests`' minimal
inline blueprint fixture had `Sand`/`SandstoneWall` but not the new
tepui pair, and `BuilderSpawn.TryPlace` fails SOFT on unknown
blueprints — so the Stump zone in that test generated with no ground
at all and the routing pin failed for the wrong reason. Fixture
extended. This is the same trap W4.4 hit twice (§ "test fixtures
diverging from production").

