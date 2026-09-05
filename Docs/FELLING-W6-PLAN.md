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


---

### W6.3a — What lives at each height (SHIPPED)

**§7.6, shipped with two live wirings.** `PopulationEntry` gains
`RequiresWorldFlag` / `ForbidsWorldFlag`, read from
`NarrativeStatePart.Current`'s FactBag — the world-flag store the plan
named. Both fail SAFE in opposite directions: a *required* flag with
no world state fails CLOSED (never spawn state-gated content into a
world that has no such state), a *forbidden* one fails OPEN (nothing
has gone wrong yet). A gated-out entry contributes **no weight**, so
suppressing one indicator increases the optional-roll chances of remaining
rows; their guaranteed minimum counts are unchanged. (W6.3b corrects the
original prose claim; the shipped roll behavior is unchanged.)

The wirings are canon, not invented:
- **Sari-Snake** `Requires UrquActive` — "Urqu's signs in flesh…
  increased spawning frequency during Urqu's manifest periods"
  (`sarisarinama_bestiary_design.md`:64-70).
- **Cascade-Father** `Forbids EcologyDamaged` — the indicator whose
  ABSENCE is the alarm: "a village whose nearby cascade no longer
  hosts Cascade-Fathers is a village in ecological trouble" (:270).

**The Stump stops borrowing the cave.** `GetStumpTable(band, tier)`
replaces `GetBiomeTable(Stump, *)`'s CaveTier1/2/3 — the god-tree's
stump was populated by snapjaws. The band reaches the pipeline from
the router (which has the coordinates) and from the sinkhole-mouth
base as well, so W6.6's Sealed Library mouth on the SLOPES will not
populate with foothill fauna. `StumpBand.None` falls back to the
foothills, not to the cave.

**Five creatures, all with art** (the standing rule): SariSnake,
Wardline, CascadeFather, GlasspaneFrog, YellowfootWayfarer. The
Sari-Snake and the Wardline deliberately share the `'s'` glyph and are
told apart by their sprites — a coil versus a drape on a branch —
which is what the art tier is for. Canon's structural opposition is
mechanical: the Wardline is Passive (it wards, it does not hunt you),
the Sari-Snake is not.

**Two param formats corrected against shipped content before they
could fail soft:** the on-hit spec key is `OnHitEffectsRaw` with a
`Name,Chance,Dice,Duration,Magnitude` payload (not the `OnHitEffect`
/ colon form I first wrote), and natural armour is an `Armor` Part
with an `AV` param, not an `ArmorValue` stat — the Yellowfoot's shell
would have been silently absent.

**Deferred to W6.3b:** the summit/sima wave (Summit Singer,
Brocchinia-Sentinel, Sky-Sari, Sima Pricklebrow, Helmwood Frog) and
the tepuibone veins that W6.2's review moved here.

**Verification:** 7200 → 7212 (+12). Two false alarms chased to ground
rather than assumed: the roster-count pin fired correctly (45→50, the
guard doing its job), and a nanosecond-scale Diag perf test failed at
264ns against a 200ns ceiling — it PASSED on a rerun without the MCP
server starting concurrently, so it was load-induced, not a
regression. Recipe that runs clean: restart the MCP server, let it
settle ~5s, then launch Unity; the plugin's WebSocket error otherwise
poisons ~19 unrelated tests through LogAssert in either direction
(server absent OR server started mid-run).

---

### W6.3b — summit/sima bestiary (SHIPPED, 2026-09-05)

**Untouched baseline:** HEAD `02ef64df`, branch
`claude/game-lore-analysis-jqa7ur`. This workspace also contains the
previously completed, uncommitted spell-presentation work, so the actual
baseline is **7,302/7,302 passed**, zero compile errors, rather than the
handoff's 7,212. Headless Unity ran after the MCP server settled;
[native XML](Verification/FellingW6/W63b-baseline-7302.xml.gz),
2026-09-05 15:00:01–15:00:37 UTC. Existing changes remain in place; a
hash/contents snapshot is at `/tmp/codex-w63b-preexisting/`. Only this
phase's changes will be staged.

#### Pre-implementation verification sweep

| Premise | Verified reality | Consequence |
|---|---|---|
| Five creatures can all enter the summit table | Bestiary §§III–IV: Pricklebrow is sima-floor fauna; Helmwood is forest fauna with rare sima arrivals. PopulationBuilder samples generic passable cells, without habitat restrictions. | Keep surface bands and sima fauna distinct; pin negative habitats as well as presence. |
| Passive + FleeThreshold implements the sentinel | BrainPart / GoalHandler.ShouldFlee require injury. AIFleeToShrine demonstrates seeking a destination, but also requires injury. | Approach-triggered bromeliad seeking needs bounded explicit behavior. |
| Singer silence is part of this wave | This plan's W6.3 scope assigns the silence/alarm clock to W8. | Ship the passive W-marked animal; do not delete singers under UrquActive to impersonate silence. |
| Sky-Sari is an ordinary rare spawn | Bestiary §VI: active during manifest periods. W6.3a's RequiresWorldFlag fails closed without state. | Require UrquActive; test set/clear/missing state and the paired Sari-Snake. |
| Anatomy is an arbitrary Body parameter | EntityFactory.InitializeAnatomy reads Props[Anatomy]; supported choices are Humanoid/Quadruped/Insectoid/Simple. No flight/stoop/nest framework exists. | Use supported anatomy; disclose any boundary between art and mechanics. |
| Helmwood can simply be sprinkled underground | Its presence implies a real water-passage connection. DrownedSimaBuilder currently establishes no such connection. | An underground clue must be anchored to actual passage truth, never random filler. |
| Tepuibone already exists | Only tepuibone-slurry exists, as a liquid ID. MineralVein is a dig-law tag; shipped Harvestable uses YieldBlueprint/YieldMin/YieldMax. | Author real vein and resource art, with a bounded connectivity-safe stamp. Slurry interactions remain deferred. |
| Content parameters validate themselves | Unknown parameters and missing builder blueprints fail soft. | Assert instantiated values, resource loading, exact sprite dimensions/alpha, and unique GUIDs. JSON edits are surgical string splices. |
| W6.3a's comment says flag gates never reweight | Roll excludes rejected weight, so later weighted bonus choices favor remaining entries. | Preserve existing behavior; correct the documentation claim without changing the population contract. |

**Readiness / order:** 🟢 blueprint/art/table and reachability seams;
🟡 targeted habitat behavior; ⚪ Singer alarm/audio clock, advanced
aerial locomotion, specimen economy, and slurry verbs remain outside
this content wave. Work through roster/placement, observable habitat
behavior, and vein content/stamping with recorded RED→GREEN cycles.
Then counter-checks, dedicated adversarial tests, cold-eye review and
the required player-flow hypothesis pass. This is CoO-original content,
not a claim of Qud creature parity.

**Performance / observability:** tables and stamps run only during
generation. New behavior uses bounded scans without hot-path collection
allocations, existing movement/goals, and success/refusal diagnostics.
Persist state through the shipped serializer. Headless tests can verify
placement, routing, state and imported assets; they cannot judge live
visual feel or the deferred summit cloud look-pass.


### Authorized follow-on — whole-game system audit (2026-09-05)

After completing the entire Felling continuation above, the user requests a
methodical scan of every game system for bugs, inconsistencies, and dead
mechanics; evidence-backed repair plans; then implementation without further
intervention. Preserve this order. Inventory systems from the shipped code and
living docs, separate verified defects from recorded debt, and apply the same
RED → GREEN, review → fix, adversarial-gate methodology to each bounded wave.
This is authorized follow-on work, not a reason to interrupt W6 close-out.


W6.3b progress: real factory/art RED 5 → GREEN 44; cold-eye found unused
natural melee dice and incorrect frog/eagle limbs. RED 5 real failures plus
an equipped-control fixture error (missing FirstSlotForEquipped, corrected),
then GREEN 79 including existing combat controls. Added opt-in
BodyNaturalAttack fallback only when no hand weapon exists; Frog/Avian layouts.
Ecology RED 12 → GREEN 87: Urqu-gated eagles, summit habitat filtering,
healthy Sentinel approach/quiet behavior, communal nest actual movement,
16 distinct scheduled defenders, and Ginmere floor placement.

Helmwood verification correction: a wet ordinary staircase does NOT satisfy
the canon Door shortcut. W6.3b now includes a minimal paired WaterPassage
marker/connection using the existing registry and save graph, plus exact-cell
travel. No StairsUp/Down marker (would reveal secrets through auto-walking).
Only place the rare underground frog when both real endpoints exist. Test
round trip, blocked/orphan exits, ordinary stairs, save/load, and unloading.


### W6.4 readiness addendum (verified before implementation)

- Olderdeep is in SinkholeSites, not WorldMapAuthoring.Places. The existing
  village Profile switch is bypassed by sinkhole routing. Pass an authored
  FoundingVillage profile through fresh stamping, rehydration and the floor
  builder; preserve already visited cached floors rather than erase player state.
- HearthPatch has BOTH Solid tag and Physics.Solid. Author a separately
  walkable plume with HearthPatchPart, not an inherited solid patch with only
  one override. WarRep is -150, shared CatacombFolk ledger, player damage only.
- BedPart is NPC sitting, not player sleeping. RestSystem.TryRest supplies
  healing/bleeding cure and exactly 60 clock ticks, but no dream event. Add
  the explicit same-cell, trusted-player action; emit meeting only on success.
  Normal underfoot terrain is obscured by player targeting, so test the actual
  interaction picker and command dispatch rather than only a direct part call.
- Founding villagers need their own ConversationIDs. Predicates/actions are
  Key/Value arrays; IfReputationAtLeast=CatacombFolk:Liked starts at 50.
- Rooted canon: arched torso, grounded shins, eastward open arms, torso plume,
  oval chamber, untouched gap. Emotional key is contentment. Tooltip remains
  the Rooted; mortal name is a deepest-rite reveal. Listening (burial, plaque
  deepening, mutter-prayer) differs from aggressive Tending cultivation.
- Olderdeep geography Tier 3 vs design Tier 5 approach is a documented source
  mismatch; do not silently retier. Clock-long scent/recognition is beyond the
  narrowly planned meeting and needs an explicit scope decision, not an
  ordinary EndTurn duration pretending to measure weeks.


### W6.5 / W6.6 readiness addendum (verified before implementation)

- Felling (3,5) needs its own appended POI type/authored stamp, surface route
  and world-map render branch. WorldMapAuthoring.Places would make a village.
  Use a sparse bespoke pipeline, not the full random Stump pipeline.
- The six bare positions require actual clearing: stamp dots preserve terrain,
  and ClearsVegetation misses non-solid plants. Mandatory placement must verify
  success. The empty seventh is one POINT, not a zone or summoned actor.
- No tile-level Urqu consumer exists. Zone.UrquBleedLevel is zone-wide and read
  by flower lifespan only. Static terrain receives neither actor EndTurn nor
  generic material ticks; standing/waiting needs a real occupancy seam.
  Persist an entity/part: transient TileState and GenReservedCells do not save.
- Append SealedLibrary=3, explicitly map its coined mouth, and KEEP the unnamed
  archetype pool modulo 3. Enum + mouth + floor route ship in one commit.
- Library vault must land after Stairs(3500)/StairConnector(3600), before
  hazards(3900)/population(4000)/containers(4100); otherwise repair can breach
  walls or stairs can appear inside. Reserve its interior and verify no entry.
- Available slopes are tier 4, routed to floor tier 5, while canon first Library
  says tier 3. A POI-tier edit alone does not affect the router; resolve the
  mismatch explicitly rather than claim both. Existing discovery means hidden
  until the surface chunk is entered, not permanently absent from the map.
- Stock LockedDoor is wooden, HP22/hardness2 and breakable even with no key.
  KeyId must be exact/nonempty and IsLocked=true; missing KeyId auto-unlocks.
  For a seal awaiting a future key use Destructible.Indestructible=true.
  Do not inherit a Solid-tagged wall for the door: unlock clears Physics.Solid
  only. The closed stock door also fails tag-only gas barriers.
- Author Tepuibone/Memory-Marble/Choir-Iron walls with real art; their names do
  not create an immunity aura. No wall fungal/Urqu protection mechanic exists.
  W6 ships sealed architecture with no key, quest arc, Staking or ending verb.

### W6.3b current verification checkpoint — 2026-09-05

Still in implementation/review; W6.4–6.7 readiness notes above are not shipped.
Baseline was 7302 (90 earlier spell tests above the 7212 handoff), all green.
Current changes remain uncommitted while the live scenario/performance gate runs.

| Review finding / false premise | Resolution / evidence |
| --- | --- |
| A surviving water marker lost its route when the owning surface unloaded | Lazily load only an uncached authored peer, then re-resolve its real connection; do not repair cached orphans. Direct ascent and whole-save controls RED → GREEN. |
| Frog placement could choose its own entrance | Exact outer rings exclude the endpoint; reserved inner ring control RED → GREEN. |
| SprayPool looked wet but has no LiquidPool | Passage child explicitly declares LiquidId=water, Volume=60. |
| Heavy harvest yield silently disappeared when the pack filled | Overflow drops at the harvest cell (actor cell for carried sources); carried/dropped diagnostic counts; capacities 0/12/24 preserve both units. Stacker merges two units into one object, so tests count units. |
| Bare Stump bands had ordinary Helmwood rows but no trees | Remove unreachable rows; add rare Helmwood to shipped Grovelands forest tables and apply the real Tree-nearness predicate to the surface pipelines. Actual-map seed sweep RED → GREEN. |
| Habitat rejection and cryptic AI decisions were silent | Cold worldgen refusal record; opt-in `ai` decisions distinguish quiet/covered/no-cover/move/veto. The AI channel intentionally stays off by default to avoid ring churn. |
| Newly spawned defenders appeared inert after one wait in a test | Existing energy scheduler starts new actors at zero, player wins the first tie. Two real waits prove action; this was a fixture timing correction, not a scheduler change. |
| Population-row splice removed only half a two-line statement | Native compile check caught syntax failure before XML parsing; corrected the surgical splice. No stale results accepted. |

Evidence: passage/harvest 61/61 GREEN; forest/observability 45/50 RED (four real missing wires plus the scheduler fixture), then 50/54 with only the four newly introduced AI-diagnostic REDs remaining. Earlier full checkpoint 7353/7353. Final full run and native Play workload still required.

Native verification plan: reusable self-auditing summit/sima scenario, real bootstrap,
registered energy scheduler, six treatment/control rows stamped by run ID. Follow
with a 75-second native frame capture under ordinary input waits; record AI,
retreat, input and renderer CPU counters plus GC. This establishes script-observable
behavior and measured CPU cost on this host, not visual quality, flight feel, or
Singer audio (W8). Use the actual scene `Assets/Scenes/Main/SampleScene.unity`.

#### W6.3b final-gate progress

- Full EditMode suite: **7394/7394 GREEN**, zero C# errors, 2026-09-05
  17:22:35–17:23:29 UTC (`W63b-full-green-7394.xml.gz`).
- Six-row scenario smoke: all controls pass, including repeat run stamps and
  loud missing-blueprint refusal. The native bootstrap audit also passed all
  six rows; its first 75-second capture had **zero player ticks**, correctly
  rejected by the activity guards. Raw `W63b-live-idle-rejected-profile.json`
  is evidence of a failed capture, not performance evidence for AI.
- Native Test Framework PlayMode launch stalled before running its test body.
  Replaced the temporary PlayMode assembly with an Editor batch launcher using
  the real OnAfterBootstrap event and an independent 180-second deadline.
  One older editor process was also closed before repeating the complete
  server-settle / single-editor sequence. EditMode XML remained independently
  timestamped; the final run is repeated with only one editor.
- Batch input needs an unfocused-Game-view override. The scenario now clones
  InputSettings, routes native queued keys to the Game view, then restores the
  original settings/device state on teardown. No production input workaround.
- Pre-existing spell-renderer teardown issue observed: ZoneRenderer's camera
  accent callback uses `?.` on a destroyed Unity Camera, producing a
  MissingReferenceException during editor exit. This is in the protected
  earlier spell work, outside W6.3b changes; record for the authorized
  whole-game audit, do not silently rewrite the user's pre-existing diff.

### W6.3b exit — 2026-09-05

**SHIPPED.** Final single-editor native EditMode suite **7394/7394 GREEN**,
zero C# errors, 17:38:40–17:39:40 UTC. Worktree baseline 7302 → 7394 (+92);
90 protected earlier spell tests remain outside this commit. Dedicated gate:
43 adversarial cases. Twelve authored sprites pass the complete metadata/GUID
and pixel audit. Cold-eye findings in this wave are resolved.

Native Play audit run `868449a126e4415fb6b5d2cef38adc63`: all six rows PASS;
75.0006 seconds, 70,659 frames, 498 actual input-driven player actions, zero
capture failures, batch launcher exit 0. Sentinel behavior across the three
subjects: active-frame mean 0.0502 ms, p99 0.0866 ms, max 0.1038 ms. The full
AI stress workload's first-action maximum and editor GC are retained, not
hidden by averages. See `Docs/Verification/FellingW6/REPORT.md` for raw data,
failed-capture controls, scope boundaries and protected-work preservation.

**Next implementation: W6.4, Olderdeep the Founding.** W6.5 (Felling-Site),
W6.6 (SealedLibrary enum + archetype + mouth in one commit), and W6.7 close-out
remain. Then perform the user's authorized whole-game system audit/repair cycle.

### W6.4 — Olderdeep the Founding (IN PROGRESS, 2026-09-05)

Starts after W6.3b commit `bdd79d3f`; suite baseline 7394. Protected work
snapshot refreshed at `/tmp/codex-w64-preexisting/`. Earlier readiness sweeps
above remain authoritative; read the exit footer for actual shipped status.

Implementation sequence:
1. RED actual authored profile, loaded-map rehydration, Olderdeep floor content,
   generic-village controls and physical/art contracts. Wire FoundingVillage
   through the existing StrandedSettlement profile seam; preserve cached floors.
2. Author the larger chamber and oval annex with the Rooted in the western
   focus, arms reaching east, a clear untouched gap, and a walkable torso plume
   carrying HearthPatch war rules. Real 16×16 body and plume/NPC art, with
   3–4 variants for repeated plume cells. Keep normal arrival/stair connectivity.
3. RED actual underfoot menu and trusted same-cell sleep. Existing inventory
   command dispatch already supplies Actor+Zone and does not charge an extra
   action; RestSystem charges exactly 60 ticks. Guard trust/reach/hostiles in
   the part itself so direct commands cannot bypass the menu.
4. Listening Tradition conversations with their own IDs. The initial meeting
   conveys contentment and remembered dream fragments; no god/ending dialogue,
   and no public mortal-name leak. Trust permission uses actual faction standing.
5. Dedicated adversarial gate, cold-eye review, actual scenario/audit, full
   suite and living docs in the W6.4 commit.

Further verified corrections: RestSystem's nearby-hostile check uses faction
hostility and Chebyshev radius8 without LOS; generic underfoot targeting picks
the player before terrain. A narrowly marked underfoot interaction must remain
reachable without hiding ordinary loot/pile choices. The shared HearthPatch
ledger is CatacombFolk-wide, despite its older village-scoped comment. The
plume's damage path must exercise real RouteDamage before assuming the war
rule fires (indestructible direct Damage is an early return).

W6.4 sweep correction before implementation: final founding decoration belongs
at priority3650, AFTER both stairs3500 and StairConnector3600; reservations do
not protect a body from their Solid/Wall clearing. The authored profile still
routes through the StrandedSettlement family. Preserve actual stair cells and
connect them before placing the body/plume. Also, `c` direction input currently
cannot choose self: recognize Period/Keypad5 locally in the talk-direction
state, rather than changing the shared parser used by throws/targeting.

W6.4 interaction sweep corrections: CatacombFolk begins at0 and has no
shipped positive reputation path. Author a one-time Tepuibone delivery to the
founding-wall tender: one actual stack unit for +50, with a saved world fact
latching only a successful exchange. This small service is a CoO-original
inference from plaque tending + name-holding stone, not a new quest arc.
The existing MineralTradeService provides stack-aware consume/reward; JSON
SellMineral + SetFact is not atomic because failed SellMineral returns void.
Use a guarded wrapper which rechecks current speaker/reach/standing/item and
sets the fact only after true. Cached dialogue choices do not recheck predicates.
Authoring9/9 GREEN; rest/trust24 actual RED has10 failures (missing successful
rest, underfoot navigation, conversation and exchange),14 passing controls.
First test compile caught an incorrect fixture RemoveTag API; fixed to the
actual Tags.Remove before accepting the assertion RED. No stale XML accepted.

W6.4 cold-eye review (before fixes): 🟡 the annex relied on random cave walls,
so an open base left the Rooted reaching into space; its plume intruded east of
his body. Author a compact oval (center61/radii7,6), whose rounded foci match
body58 and east wall65, and a deterministic wall face. Reserve and leave its
six-cell gap empty. 🟡 niches must sit against real chamber walls rather than
in rows across open floor. Forced stair fixtures and open/solid bases pin both.
Reservations govern generated content, not runtime walking; villagers stay still
by their existing Wanders=false contract, and the player can choose to cross
sacred ground. This is cultural space, not an invisible collision barrier.
The canon patch-bloom recognition can use a saved player expiry property read
only when talking: 14 * WorldClock.DayLengthTicks =16800 ticks (two weeks),
without another per-turn hook or scan. Add boundary tests before authoring.

W6.4 checkpoints: 33/33 interaction/content GREEN; dedicated31 adversarial
RED17 exposed the deterministic wall/plume geometry, dead-player rest loophole
and unimplemented scent expiry. After fixes, 68 tests passed66 with precisely
the two new consent regression REDs: raw -1 reputation showed an offer that
execution refused; UnderTheCloth masked a tender's personal grudge. Share the
permission predicate and use GetFeelingUnfloored (the existing recruit-consent
precedent). All70 focused checks then GREEN, zero C# errors. Scenario2 tests
first failed to compile on the deliberately absent FoundingVillageBench type;
implemented the native keyboard scenario and both now pass. Art7/7 passes
16x16, binary alpha, shared outline, metadata-only-GUID-diff and global GUID audit.
Final router review found that a FoundingVillage profile still passed through
the display-name archetype switch before selecting its builder. Add two
opposite-archetype-name RED controls, then let the profile win on the floor.

Last geometry review: the original seven-row collision rectangle rejected two
legal stairs at(60,9)/(60,15) for every preferred body center. Replace it with
actual plume/gap/root-face collision cells and deterministic fallback center
rows7..17. Preserve exact stair entities and coordinates; their approaches
remain above/below the three-row sacred gap. The new two-stair assertion ran
RED before this correction. Native stale-selection staging stays one tile
from the selected plume, so it tests strict same-cell sleep, not just generic
interaction reach. The native once-only row audits CanOffer; repeated actual
exchange and stale cached-choice execution are independently covered in EditMode.

Native RED (exit1, zero C# errors): real held S selected Sleep, then the same
held shortcut became normal movement after the menu closed. This spent an
extra action and changed the underfoot target before the next case. The
new input release latch consumes a selected world-action letter until key-up
before allowing Normal-state input. Native exact-clock/energy/position checks
exercise the counter-condition after a held shortcut; no shorter test pulse
is used to hide the defect. Filtered raw gameplay lines are archived in
W64-native-held-key-red.txt (no process arguments or credentials).

W6.4 native GREEN: run30e595e630c149b59348e356239a6c3d,8/8 actual keyboard
rows PASS, no C# errors, launcher exit0. Held S no longer moves the player;
rest remains exactly60ticks with unchanged energy and position. The headless
ScreenCapture call produced no file; this is not visual-playtest evidence.
The first full suite was7466/7467, with only the user's pre-recorded fungal
self-cloud flaky failure. Final repeat follows the last art fix: repeated
NicheHome fixtures now use four variants (existing base plus three new original
16x16 sprites), alongside four plume variants. Three missing-import assertions
ran RED before this art/renderer entry. Ten authored sprites, both distinct
four-variant families and all global GUIDs pass the audit. The native scenario
ran before the final cosmetic niche variants; no new gameplay followed it.


### W6.4 exit — 2026-09-05

**SHIPPED.** Final single-editor EditMode suite **7470/7470 GREEN**, zero C#
errors,18:35:30–18:36:28 UTC. Baseline7394 →7470 (+76), including34 dedicated
adversarial cases. Ten original sprites, four plume/four niche variants,
metadata and global GUID audit pass. Native eight-row keyboard encounter
run30e595e630c149b59348e356239a6c3d passed with exit0; exact rest timing,
unchanged energy/position, earning trust, first-meeting latch and refusal flows.
The earlier held-key native failure and known fungal full-suite flake are
retained as failures in the raw evidence, not omitted or counted as passes.

See `Docs/Verification/FellingW6/W64-REPORT.md` for the final implementation,
resolved cold-eye findings, scope inferences and honesty bounds. The native
run does not establish visual feel; headless screenshot capture was absent.
No new per-turn scan was introduced. Prior spell work stays unstaged; only
our incremental hunks enter the three shared presentation/test files.

**Next: W6.5, the Felling-Site**, then W6.6 (Sealed Library enum + archetype +
mouth in one commit) and W6.7 close-out. After W6 completion, continue the
user's authorized whole-game system audit, repair plans and implementations.


### W6.5 — the Felling-Site (IN PROGRESS, 2026-09-05)

Baseline: W6.4 commit `7e8f31c7`, 7470/7470 GREEN. Protected work snapshot:
`/tmp/codex-w65-preexisting/manifest.json` (403 paths); preserve their bytes
and stage only incremental hunks in any shared file. Qud reference: none;
this site and exposure are CoO-original Felling content.

Readiness: 🟢 authored POI/router, existing stone, status effects, ordinary
save graph; 🟡 persistent barrenness and standing exposure need runtime
consumers; ⚪ exact ring geometry and two-action confusion are authored
interpretations, not quantitative canon. W6.5 is not pure authoring.

Preimplementation verification corrections (source read before code):

| Assumption | Verified correction / implementation consequence |
|---|---|
| Put the site in Places | WorldGenerator stamps every Place as Village. Append POIType.FellingSite=5; reserve (3,5) before opportunistic rolls. Surface route only; display name is not a behavior gate. |
| Reuse the Stump pipeline | Surface pipeline adds caves, hazards, loot, trade and population. A dedicated open-stone circle builder preserves the empty scene and clear approaches. |
| Center the seventh | WorldMapTraversal descends at (40,12). Keep ordinary arrival ground there; place six bare cells + seventh on the surrounding ring. Exact geometry is authored staging. |
| Zone bleed field represents the point | Zone.UrquBleedLevel is zone-wide and consumed by FlowerCharm. Do not change it; highest pressure is one persistent SeventhPosition cell. |
| Terrain EndTurn or entry trigger suffices | Static terrain receives no actor EndTurn; EntityEnteredCell excludes waits. Check the ending player's actual cell after actor effect cleanup in TurnManager.EndTurn. No NPC or zone scan. |
| Existing confusion changes input directions | ConfusedEffect actually subtracts2 DV/Agility and does not scramble player directions. Use brief nonstacking two-action disorientation, with a normal aftereffect on leaving; respect preexisting effects/immunity. Clear JustApplied only for this new post-cleanup application. |
| Generation reservation means permanently bare | GenReservedCells is transient and generation-only. Audit runtime flora placement before adding a durable barrenness gate. Do not ship an unused Barren flag. |
| Rehydrate POI fixes old maps visually | Cached WorldMapCell render fields also persist. Refresh derived rendering after load without rebuilding the map or removing occupants. Preserve cached player-modified ground zones. |
| Label the empty position with Naro's history | Mystery Ledger§1 overrides design history: neither absence nor attendance is proven in-world. No identity labels, authoritative refusal, intentional Urqu, quest or ending action. |

References read: Docs/FELLING-WORLD-DESIGN.md§3.6, this plan§3/W6.5,
Lore/History/02_Geography.md§Stump + bleed distribution, History03 Felling,
Lore/10_Bible.md cosmology + Seventh, Lore/11_SecondSpine.md C1/C4,
Lore/MYSTERY-LEDGER.md§1; WorldGenerator, WorldMap, PointOfInterest,
OverworldZoneManager, WorldMapZoneBuilder, Cell/Zone, TurnManager,
StatusEffectsPart/ConfusedEffect and existing W6.4 save/builder tests.

Implementation slices: (1) actual POI, deterministic accessible 6+1 circle,
real art/variants and cached-map repair; (2) standing exposure + durable flora
exclusion, with RED gameplay controls before code; (3) dedicated20–60-case
adversarial gate, cold-eye fixes, deterministic native input/performance
scenario75seconds, full suite and living docs in one complete W6.5 commit.

Scope boundaries: no new ending interaction, no quest facts, no UrquActive
gate, no zone-wide maximum and no explanatory NPC. Old cached ground is
preserved rather than regenerated. Exposure duration and geometry are
CoO-original; visual/feel judgment remains separate from native observables.

W6.5 runtime sweep follow-up (before implementation): rooted flora has no
complete shared tag; Plant/Wood materials also describe food/furniture. Add
Vegetation to the31 verified rooted/standing-dead blueprints only, with Crop
and FlowerCharm part controls. Zone.AddEntity is the shared bool-returning
placement/movement gate: reject before changing source membership. Incoming
barren terrain removes only vegetation; loaded-cell repair runs after bodies
resolve, with only this new classification rehydrated from current blueprint
tags. SeedPart and BuilderSpawn currently ignore AddEntity failure; honor it
before consuming a seed or reporting a placed entity. FarmPlotSeeder must
check barren before replacing floors. No player flower-conjuring command or
fungal-root propagation currently ships; existing FlowerCharm is a lifespan
reader. This prevents material heuristics from deleting food, furniture or
mobile creatures. The31-entry census includes living substrate architecture
and standing dead plants as explicit content classification choices.

Authoring RED confirmed14/16 failures (missing site/art) with2 passing nearby
controls, zero C# errors. A fixture parenthesis typo was corrected before
accepting that assertion RED. Save RED1/4 confirms the missing old-map POI
repair;3 saved-POI/uncached-map controls pass.

W6.5 runtime RED10/19 (9 controls pass) →39/39 focusedGREEN with authoring
and save tests. Dedicated hypothesis/adversarial run32/33; its only failure
was a wrong test expectation that Olderdeep(4,6) lay in Stump, when actual
map authoring places its mouth in Grovelands. Correct the control to derive
the real underlying biome, preserving the hidden-name assertion. No production
fix implied. Earlier direct test access to internal BuilderSpawn failed to
compile; the test now invokes its public static method via test reflection,
without expanding production API. No stale results were used.
Cold-eye taxonomy and canon passes found0 confirmed 🟡+ defects. Hypotheses
include all31 plant classes, preserved source/index/version on refusal,
active-effect save/expiry, prior penalties, veto, absent/dead/removed targets,
old missing-tag flora, cached ground preservation, renamed routes, real
lateral arrivals, map descent and marker-order-independent loaded cleanup.
New ordinary player-end lookup reads only the current cell with no allocation;
actual confusion application/log lifecycle allocates. Profiling follows below.


W6.5 full sweep RED7544/7547, zero C# errors. It caught one missed project
contract (🟡): production must apply effects through Entity.ApplyEffect,
never StatusEffectsPart directly. Replace the call, preserving normal veto and
JustApplied semantics; the structural facade test remains strict. Two existing
world tests assumed every Stump cell was wilderness: grain orientation now
samples actual wilderness, and the no-lair/camp pin explicitly permits only
coordinate-validated authored Felling/sinkhole POIs. Random claims still fail.
These failures are preserved as W65-full-integration-red.xml.gz. Repeat native
capture and full suite after the facade correction; no pre-fix performance
numbers stand in for the final implementation.

Full repeat7546/7547: all77 new tests and the repaired structural contracts
pass; sole failure is the user's pre-recorded fungal self-cloud flaky test.
Keep raw W65-full-known-fungal-flake.xml.gz and repeat after the final native
capture. Final scene/art appearance is unchanged by the facade correction.

Final native capture657ea017c8ae4f87a77638a8ad210d89:9/9PASS,75.001255583sec,
74360frames,7410ticks,741active exposure frames, zero failures/C# errors,
launcher exit0. Exposure active-frame mean0.085080ms,p990.220875ms,
max0.381000ms. Full-frame samples and whole-editor GC are retained; the
pre-facade capture is separately named rather than overwritten.


### W6.5 exit — 2026-09-05

**SHIPPED.** Final full EditMode suite **7547/7547 GREEN**, zero C# errors,
19:18:46–19:19:45 UTC. W6.4 baseline7470 →7547 (+77), including36 dedicated
adversarial cases and2 deterministic scenario checks. Nine real sprites,
four variants each for bare positions/creases, binary alpha, template metadata
and global GUID audit pass. Final native run657ea017c8ae4f87a77638a8ad210d89
passes9/9,75seconds,zero capture failures,launcher exit0. First full integration
failures and the known fungal flake remain archived with the successful repeat.

Cold-eye and final full-suite review resolved the facade violation; existing
Stump tests now distinguish the authored site from wilderness without admitting
random camps/lairs. The true exposure hook uses Entity.ApplyEffect and the
final native measurement follows that fix. All403 protected baseline files
remain; only incremental hunks enter SaveSystem and EnvironmentSpriteRenderer.
Other original files are byte-identical except the editor's runtime MCP log.
Concurrent component-art outputs are also left alone.

See `Docs/Verification/FellingW6/W65-REPORT.md` for behavior, raw gates,
performance, canon inferences and visual/feel limits. **Next: W6.6, Sealed
Library (enum + archetype + slope mouth in ONE commit), then W6.7 close-out.**
After W6 completion, continue the authorized whole-game audit, repair plans
and implementations without pausing for approval.
