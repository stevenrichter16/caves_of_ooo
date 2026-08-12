# Felling World Overhaul — Implementation Plan (living doc)

> **Status: W0 in progress.** Plan drafted 2026-08-11 from
> `Docs/FELLING-WORLD-DESIGN.md` §10 build order, after a six-slice
> pre-implementation verification sweep (988k tokens, 6 parallel
> verifiers over worldgen pins, enum/save, lightmap, clock, factions,
> starting-village hardcodes). Corrections from that sweep are §1 —
> read them first; they reshaped the sub-milestones.
>
> Methodology: CLAUDE.md throughout — TDD RED→GREEN per SM,
> counter-checks per invariant, §5 self-review pre-commit, living doc
> updated in the same commit, diag on every gate.

---

## 0. Goal & scope

Implement the canon-grounded world overhaul in the design doc's build
order (W0 plumbing → W1–W7 biomes → W8 gods/clock), keeping every
shipped mechanic. This document is the master plan; each W-phase gets
its own detailed section here when it starts (W0 is detailed now;
W1–W8 are milestone-level until their turn).

**Content-readiness:** 🟢 design doc complete · 🟢 engine inventory
complete · 🟢 verification sweep complete · 🟡 two user decisions
pending (§6) — neither blocks W0.

---

## 1. Verification sweep — corrections table

The sweep's job was to falsify the plan before code. It did, nine
times. **Every sub-milestone below is written against the corrected
facts, not the design doc's original assumptions.**

| # | Plan assumed | Actually true | Consequence |
|---|---|---|---|
| C1 | "Add a WorldClock" is greenfield | **The clock exists**: `TurnManager.TickCount` + `AdvanceClock` (TurnManager.cs:80, :223-227), save-persisted (SaveSystem.cs:854, restored :887 → TurnManager.cs:556). Tick economy: ~10 ticks/player action, rest +60, worldmap step +10 | WorldClock = **stateless static derivation** of TickCount. Zero new persisted state. Band widths authored in TICKS |
| C2 | "Add per-zone AmbientLevel" is a new symbol | `LightMap.AmbientLevel` already exists (public mutable field 0.4f, LightMap.cs:25), consumed at ZoneRenderer.cs:2162 + 9 test assertion sites (all **relative** — none pin 0.4f) | Work = Zone field + plumb through LightMap; LightMap's field becomes a get-only "effective" property |
| C3 | Setting ambient would just work | **LightMap cache key omits ambient inputs** (LightMap.cs:41-43: EntityVersion + EquipmentVersion only) — an ambient change renders **nothing** until an entity moves. Worse: `_lightMap` survives zone switches (ZoneRenderer.cs:499-517) — a latent cross-zone staleness bug | Cache-key fix is mandatory, not optional; it fixes the latent bug too. RED test: mutate ambient between Computes → brightness follows (fails today) |
| C4 | Save format tolerant | `FormatVersion = 4`, **strict equality**, throws on mismatch (SaveSystem.cs:25, :133-135); tripwire test pins `== 4` (StoryletSaveRoundTripTests.cs:27). Zone fields are NOT auto-persisted — every new field needs explicit SaveZone/LoadZone lines in matching order | Adding `Zone.AmbientLevel` ⇒ bump to 5 ⇒ **bricks all v4 saves** (accepted pre-1.0 policy per the test's own comment). One deliberate bump, tripwire re-pinned same commit |
| C5 | Rename Palimpsest→"the Recension" is W0 work | **Already shipped on this branch** (commit `2bd65e21`): Factions.json:56-57 has `DisplayName: "the Recension"`; conversation prose done; `GetDisplayName` exists (FactionManager.cs:259-263). Remaining: **one leak** — DialogueUI.cs:546-554 renders the raw faction ID in the portrait panel (8-char truncation) — and **zero test pins** on any display name | W0 = fix the leak + add the missing pin tests. Nothing else |
| C6 | Append enum members early, author later | `EnsureAllBiomes` iterates `Enum.GetValues` (WorldGenerator.cs:79) and **force-places one tile of every member** on new maps | Enum append MUST land in the same commit as the generator replacement |
| C7 | Tier duplicated in 2 places | **4 computations** (2 Manhattan: WorldGenerator.cs:204-210, OverworldZoneManager.cs:112-120; 2 depth: ZoneManager.cs:187-192, OverworldZoneManager.cs:164) AND tier is both stamped into `POI.Tier` at gen-time and recomputed at pipeline-time | Authored tier table becomes the single surface authority; both Manhattan copies delegate; depth formula untouched |
| C8 | Starting zone ID in 4 files | **3 constants + 2 raw literals**: SettlementSiteDefinitions.cs:10, VillagePopulationBuilder.cs:19 AND a bypassing literal at :87, RiverBuilder.cs:42, GameBootstrap.cs:269. RiverBuilder is **unregistered dead code** (zero `new RiverBuilder()` anywhere) | Consolidate to one symbol first; **delete** RiverBuilder |
| C9 | "Degraded not broken" for new biomes | Three exceptions: tier≥2/≥3 population falls to **CaveTier1** (difficulty hole, PopulationTable.cs:66-92); `StampCatalog.For` default = **empty** (no landmarks, LandmarkBuilder.cs:73); `GetBiomeTint` default = **white** (OverworldZoneManager.cs:381) | Placeholder mappings for all six new biomes land WITH the enum append |
| C10 | Live map is stable today | `GameBootstrap.cs:268` passes no seed → `Environment.TickCount` — **the biome layout is random every launch** | The authored map is a player-visible improvement, not a like-for-like swap |
| C11 | — (found, not assumed) | `TurnManager.Active` docstring claims GameBootstrap clears it on teardown; **no such code exists** — stale across sessions/tests | WorldClock gets `ResetForTests()`; tests construct their own TurnManager |
| C12 | — | Crossroads retirement is bigger than the builder: 4 pipeline registrations, compass stones (placement + 4 blueprints), **Scribe_1 region dialogue wired into EVERY village**, TemporalShard's only worldgen placement, 5 pinned tests | Own SM with its own ledger |
| C13 | — | Kyakukya→Sill is **one production line** (`WorldGenerator.cs:14`, `VillageNames[0]`) + cosmetic test/doc sweep | Rides along in housekeeping |
| C14 | — | Second ambient system exists: `LightSourceSpriteHook` Light2D global dim (0.45 dungeon / 1.0 outdoor) gated by `ZoneRenderer.ZoneIsDungeon` — a fragile ZoneID-substring heuristic whose Cave/Ruins branch is dead for all `Overworld.*` IDs | W0 leaves it; W5 (catacombs) subsumes it with `zone.AmbientLevel < threshold`. Noted so nobody double-dims |
| C15 | — | `RememberedColor` (0.2 gray) is applied unmodulated (ZoneRenderer.cs:1008, :1244): ambient < ~0.2 renders visible cells **darker than remembered ones** | W0 keeps all zones at 0.4 (plumbing only, no visual change); the perceptual-inversion guard is a W5 acceptance criterion |

Two pre-existing repo issues found in passing: untracked junk file
`1;EchoKnife;TemporalShard` at repo root (shell-redirect debris —
delete, don't commit); stale comment `WorldMapShowcase.cs:10` (cites
GameBootstrap.cs:163, now :269).

---

## 2. W0 — plumbing (detailed sub-milestones)

Order is smallest-blast-radius-first with dependencies honored:
factions (W0.4) before the authored map (W0.6) so authored POIs can
reference TentRight; crossroads retirement (W0.5) before the map swap
so no set piece lands on wrong terrain.

### W0.1 — WorldClock (stateless band derivation)

**Invariant (user-visible):** the world has a time of day derived
purely from the existing turn clock; crossing a band boundary emits a
`turn/BandChanged` diag record exactly once, including across
multi-band jumps (rest +60), and never fires spuriously on load.

- `Assets/Scripts/Gameplay/Turns/WorldClock.cs` — static, stateless
  core: `DayLengthTicks = 1200`, `BandLengthTicks = 300`;
  `enum DayBand { Dawn, Height, Dusk, Dark }`;
  `GetBand(int tick)` pure; `BandName(DayBand, int depth)` returns
  catacomb names (Bright/Half-Bright/Dim-Down/Dark-Watch) for
  depth > 0, surface names for depth ≤ 0 **including −1**
  (non-overworld IDs parse to −1 per WorldMap.cs:211-214);
  `CurrentTick => TurnManager.Active?.TickCount ?? 0` (the majority
  idiom — valid earliest during load, C11/correction #4).
- Side-effect seam: `WorldClock.NotifyPlayerTurnEnd(int tick)` —
  last-band diff, lazily primed (first call records, no emit), emits
  ONE `turn/BandChanged {fromBand, toBand, tick}` diag per boundary
  crossing even when a jump skips whole bands. `ResetForTests()`.
  Wired in `InputHandler.EndTurnAndProcess` **after**
  `ZoneTileStateSystem.OnPlayerTurnEnd` (InputHandler.cs:877) — never
  inside `TurnManager.EndTurn` (turn-divider coupling, adversarial
  null-zone test).
- **No MessageLog lines in W0** — in-world flavor text is voice-gated
  content; diag only.
- Tests (~12): band math at boundaries (0, 299, 300, 1199, 1200,
  negative-guard), depth naming incl. −1, prime-without-emit,
  single-band emit, multi-band jump emits once with correct from/to,
  no-change no-emit (counter-check), reset behavior.

### W0.2 — Per-zone ambient plumbing + LightMap cache fix

**Invariant:** a zone's authored ambient level is what the LightMap
uses — changing it (or switching zones) is reflected on the next
Compute without requiring an entity to move. No visual change ships
in W0 (all zones stay 0.4).

- `Zone.cs`: `public const float DefaultAmbientLevel = 0.4f;` +
  `public float AmbientLevel = DefaultAmbientLevel;` beside
  AmbientTint.
- `LightMap.cs`: Compute reads `zone.AmbientLevel` into
  `_effectiveAmbient`; public `AmbientLevel` becomes a get-only
  property over it (keeps ZoneRenderer.cs:2162 + all 9 test sites
  compiling with unchanged semantics); `GetBrightness` OOB returns
  `_effectiveAmbient` (C7-lightmap).
- **Cache key** (:41-43) extended: zone reference + AmbientLevel
  (approx-compare, not version-bump — no per-frame recompute) +
  AmbientTint. Fixes C3's latent cross-zone staleness too.
- `SaveSystem.cs`: write/read AmbientLevel immediately after
  AmbientTint (:946/:959); **FormatVersion 4→5**; re-pin the tripwire
  (StoryletSaveRoundTripTests.cs:27 → 5); add
  `Gap_Zone_AmbientLevel_RoundTrips` sibling of the AmbientTint gap
  test; fix its stale line-citation comment while there (C6-lightmap).
- RED-first tests: ambient-change-alone invalidates (fails today);
  cross-zone same-EntityVersion staleness (fails today — pins the
  latent bug as fixed); round-trip; OOB; property-reflects-zone;
  counter-check: unchanged ambient + unchanged versions ⇒ Compute
  early-outs (perf contract preserved).

### W0.3 — Housekeeping: constants, dead code, Sill, the faction leak

**Invariant:** one symbol owns the starting zone ID; the starting
village is named Sill; every player-visible faction name goes through
`GetDisplayName`.

- `WorldMap.StartingZoneId` const; `SettlementSiteDefinitions.cs:10`
  and `VillagePopulationBuilder.cs:19` redirect to it; fix the
  bypassing literal at VillagePopulationBuilder.cs:87 and
  GameBootstrap.cs:269.
- **Delete** `RiverBuilder.cs` (dead, unregistered; superseded by
  RiverChunkBuilder). Leave ZoneRenderer's legacy tag handling
  (harmless).
- `WorldGenerator.cs:14` `VillageNames[0]`: "Kyakukya" → **"Sill"**;
  sweep the cosmetic fixture/message references
  (StartingTownTests.cs:187/:313, WorldMapPOIRenderingTests fixtures,
  Settlement* test fixtures — none assert the generated name, so
  these are coherence edits).
- `DialogueUI.cs:546-554`: render
  `FactionManager.GetDisplayName(factionId)` with leading "the "
  stripped for the 8-char portrait panel (truncation behavior
  unchanged; a wider panel is a UI-pass decision, flagged).
- **Pin tests (currently zero exist):**
  `GetDisplayName("Palimpsest") == "the Recension"` after real-JSON
  init + unknown-ID fallback counter-check.
- Delete the `1;EchoKnife;TemporalShard` junk file; fix
  WorldMapShowcase.cs:10 stale citation.

### W0.4 — New faction entries

**Invariant:** TentRight, BowerFolk, ImminentArchive, CatacombFolk
are registered, visible, rep 0, with canon-consistent Feelings — and
no ghost factions appear in the Faction UI.

- Four `Factions.json` entries (DisplayNames: "Tent-Right",
  "the Bower-Folk", "the Imminent Archive", "the Rooted's people").
  **InitialPlayerReputation 0 for all** (the save-restore trap:
  non-zero initial rep silently reads 0 on pre-existing saves —
  faction-display hazard #4). Feelings kept minimal and symmetric:
  ImminentArchive↔PaleCuration −25 (the expulsion), TentRight→none
  (the oath is not an enmity), others 0.
- Tests: registration + visibility + rep seeding for all four;
  **ghost-faction counter-check** — every ID named in any `Feelings`
  block resolves to a registered faction (guards hazard #5, where a
  Feelings typo becomes a raw-ID row in the player-facing UI).
- Driving Bloom gets **no entry** — canon: no rep track, ever.

### W0.5 — Retire the Elemental Crossroads

**Invariant:** no set piece keys on the four cardinal zone IDs; no
village Scribe describes the four crossroads regions; compass stones
are not placed. Themed blueprints/loot/material-reactions stay in
circulation (they're engine content, reused by tier tables).

- Delete `StartingNeighborhoodBuilder.cs` + its 4 registrations
  (OverworldZoneManager.cs:128/:184/:200/:216).
- `WorldGenerator.cs:59-62` cardinal pins + `:97-101` EnsureAllBiomes
  skip-guards removed (the whole method dies in W0.6 anyway — this SM
  just removes the pins if landing separately).
- Compass stones: remove placement (VillagePopulationBuilder.cs:87-92,
  :365-404). Blueprints stay (harmless unplaced; sprite/glyph rows
  stay).
- `FriendlyNPCs.json` Scribe_1: remove the four Region* choice nodes.
- Delete `StartingNeighborhoodBuilderTests.cs` (5 tests, all pin the
  retired feature). Re-run VillagePopulationBuilder pins — the
  grimoire-chest exact-cell pin (:30-51, cell 43,11) may shift with
  cell competition; re-baseline if so.
- **Known losses, accepted + documented:** TemporalShard/EchoKnife
  lose their only guaranteed worldgen placement (still in
  MarcelineStock + skill references); crossroads mutants stay in
  tier-3 tables (BiomeJunglePassTests circulation pins stay green).
- Keep: 5 material-reaction JSONs + the `ReactionCount == 13` pin
  (files unremoved), themed weapons/loot.

### W0.6 — The authored world map (the big one)

**Invariant:** the 20×20 world is the authored map from
`FELLING-WORLD-DESIGN.md` §2.2 — fixed biomes, fixed tier table,
authored canon POIs — while lairs/merchant camps stay seed-varied.
Sill is at (10,10). Every zone still generates and is reachable.

One commit, because of C6 (enum append + generator replacement are
atomic):

- **Enum append** (after Ruins, never reorder): `Spread, Sodden,
  Beating, Grovelands, Overwrit, Stump`.
- **`WorldMapAuthoring.cs`** — authored data as stamp-style string
  rows (the established ASCII-authoring idiom): 20 rows of biome
  chars, 20 rows of tier digits, road/river overlay rows, and the
  authored POI list (name, type, faction, world cell) from the design
  doc: Sill(10,10 start), Gantry(7,8), Tine(13,7), Cinderhold(6,6),
  Posy(5,9), Marrowstye(12,12), Quillhold(14,10), Tally(10,14),
  Wellmeet(8,16), First Tent(5,17), Salt-Vault(15,15), Sumphold(15,6),
  Drowned Ledger(17,5), Slip(16,11), The Last Counter(18,18).
  (Sinkhole POIs — Olderdeep, Deepest Cathedral, Lampwell, Spivenor,
  Quiet's Door — are W5 scope; their cells are reserved in comments.
  The Felling-Site/Root zones are W6. The Unsaying is W7.)
- **WorldGenerator.Generate** consumes the tables; `EnsureAllBiomes`
  and the noise pass are deleted; lairs (3–5) + merchant camps (2–3)
  remain seed-rolled with spacing, vetoed off authored-POI cells and
  the Overwrit region.
- **Tier single authority:** `WorldMapAuthoring.TierAt(x,y)`;
  `WorldGenerator.GetTierByDistance` deleted; `OverworldZoneManager.
  GetTierForCoords` delegates (both POI stamping and pipeline-time
  agree by construction — C7).
- **Placeholder pipelines** so every new-biome zone plays TODAY
  (upgraded per phase later): Spread→open cave-params (W1 replaces),
  Sodden→jungle-params (W3), Beating→desert reuse (W2),
  Grovelands→jungle reuse (W4), Overwrit→minimal blank sheet builder
  (W7), Stump→cave reuse (W6). Plus, per C9, same-commit placeholder
  rows: population tables (map to nearest legacy at the SAME tier —
  no CaveTier1 hole), tints (§6 render table of the design doc),
  BiomePalette cases, container pools, village/lair palettes,
  world-map glyphs + legend names, hazard-terrain tables, stamp
  catalogs (nearest-legacy set, not empty).
- Roads/rivers overlay: stored + rendered on the world map zone;
  consumed by zone pipelines in W1+ (road/river formations). W0 only
  guarantees the data + world-map rendering.

**Test re-baselining ledger for W0.6** (from the sweep — deliberate,
not surprise reds):

| Test | Action |
|---|---|
| `WorldGenerator_DifferentSeeds_DifferentMaps` (WorldMapTests:711) | Rewrite: seeds must differ in **POI placement** (lairs/camps), not Tiles |
| `WorldGenerator_CenterIsCave` (:733) | Re-pin: center is **Spread** |
| `WorldGenerator_AllBiomesPresent` (:741) | Re-pin: all **authored** biomes present; legacy four absent from surface |
| `WorldGenerator_CenterHasVillage` (:1446) | Stays green (Sill is a Village POI) |
| `OverworldZoneManager_RoutesCaveBiome/_RoutesDesertBiome` (:1220/:1248) | Re-point at new-biome routing (Spread/Beating) |
| `WorldMapZoneBuilder_AllFourBiomesPresent` | Re-pin to authored roster + new glyphs |
| `BiomePaletteTests` 4-biome distinctness | Extend to the six new palettes |
| `BiomeMerchantCampTests`, `BiomePhaseBAdversarialTests` camp-per-biome | Re-baseline biome list |
| `StartingTownTests` (:176/:194) | Stays green (center still starting Village; name now Sill) |
| `WorldMapTraversalTests.Ascend_FromCenter…` (:301) | Stays green (center unmoved) |
| `PopulationTables_*`, `BiomeSpawnRepairTests` | Green until W1+ re-authors tables (placeholders map to legacy) |

### W0 exit criteria

Full EditMode suite green (with the ledger's re-baselines, each
justified in its commit body); zero compile errors via the
`unity_cycle.sh` ladder; self-review §5 per SM; cold-eye pass over
the whole W0 diff; SCOPE DIVERGENCE sections wherever the sweep
changed the plan (§1 rows referenced by ID).

---

## 3. W1–W8 milestone map (detailed at phase start)

| Phase | Ships | Key dependencies |
|---|---|---|
| **W1 — The Spread + Sill** | 6 formations (river-meadow reuses RiverChunkBuilder; hedge/lane builders new), FlowerField charm-terrain + bleed-wilt instrument, folk shrines, stamps (RiverShrine, MillStead, FestivalField), Spread population/loot tables, the Sill inciting storylet (first non-quest storylet!) | W0.6 map + roads overlay; W0.1 clock (dusk glow) |
| **W2 — The Beating** | Salt pan/ruin-field/dune/brine formations (RuinsBuilder rehomed), Parched status + Height-band glare, Tent-Right camps + **the oath** (status + sanctuary AI honor + pursuit-waits-outside scene), Wellmeet, First Tent, salt economy activation (PaleSalt veins + WantsMineralPart), the tenth fire (placed, silent, unexplained), Last Counter + 2 abandoned predecessors | W0.1 (hour bands), W0.4 (TentRight faction) |
| **W3 — The Sodden** | Mire/reed/peat-cut/causeway formations, Bog-Taken terrain bodies (readables), methane peat (BurnOffGasPart param), Sumphold + Drowned Ledger content, body-courier contracts (sealed-to-standard), boat lanes on river edges | W0.6 |
| **W4 — The Grovelands** | Grove/fen/colonnade formations, grove rules (seep, red forage, dig gate, rest-trigger encasement offer), ChoirTendril activation (37-node tree finally spawns), BloomedEffect + Bloom-front overlay, spore gas siting, the doll (one grove, no explanation) | W0.6; W0.4 |
| **W5 — Vertical** | Sinkhole POIType + Mouth/Descent/Floor pipelines (multi-zone vertical recipe), 3 floor archetypes first (Drowned Sima, Stranded Settlement, Choir Cathedral), catacomb village kit + 5 archetypes, bio-light economy + authored AmbientLevels (subsume ZoneIsDungeon heuristic — C14; RememberedColor guard — C15), plaque-walls (KnowledgePart at scale), dead zones + eyeless apex predators, Wall-Catching + Being-Preserved displacement, Lampwell, Spivenor | W0.2 (ambient), W0.1 (catacomb bands), W0.4 (CatacombFolk) |
| **W6 — The Stump** | Elevation-band formations (cascade/buttress/grainfield/summit), band-keyed population with **state-reactive spawning** (PopulationTable predicates on FactBag flags), full Sarisariñama bestiary, Felling-Site authored zone (six bare positions + the empty seventh point), Olderdeep + the Rooted's chamber + sleep-on-patch, Sealed Library vault (LockPart) | W5 (sinkholes, villages) |
| **W7 — The Overwrit** | Bleed mask (per-cell bitfield + trigger tables on hour/weather/flags), undertext on readables (can ship earlier — cheapest slice), 4 authored zones + 3 deep-bleed fragments (hand-placed, never procedural), the Unsaying, underreading teachers, edges-inward closure animation | W0.1 (thresholds); voice-gate for all readables |
| **W8 — Gods & the clock** | Five god-rooms + verbs (transcription channel, file-review, pilgrimage offering, meeting fee, behavior-mediated), Thinning escalation storylets, closure-ledger v1 (`closure/Closed\|Refused\|Abandoned` diag + decline verbs in dialogue + FactBag coupling), bio-light world-state channel, Quillhold/Salt-Vault/Tally/Posy as full places, Slip's east quarter | Everything; the capstone |

Mystery-Ledger/voice/cultural-sourcing gates (design doc §9) are
acceptance criteria on every phase from W2 onward.

---

## 4. Test strategy

- Per SM: RED → GREEN → counter-check → 1-3 mutation tests inline.
- W0.6 is the re-baselining SM — every changed pin justified in the
  commit body by ledger row.
- Dedicated adversarial sweeps due where 2+ taxonomy surfaces apply:
  W0.2 (save/load reach + boundary inputs), W2 (oath: cross-actor +
  anti-exploit + state atomicity), W5 (displacement + light economy +
  save reach). W0.1/W0.3-0.5 carry inline mutation tests only.
- PlayMode/live checks deferred to phase ends (terrain phases get the
  deterministic self-auditing bench treatment where measurable —
  glare damage matrix, ambient levels).

---

## 5. Observability

New diag emissions (all pinned by tests): `turn/BandChanged`
(W0.1); `worldgen/AuthoredMapApplied {pois, biomes}` (W0.6);
existing `worldgen/StructurePlaced`, `tile/*` untouched. W2+: oath
gates (`sanctuary/OathClaimed|OathHonored|OathBroken`), closure
ledger (W8).

---

## 6. Open decisions (flagged, not blocking)

1. **Old saves brick at W0.2** (FormatVersion 4→5, strict check).
   Accepted pre-1.0 per the existing tripwire's comment — but stated
   here so it's a decision, not an accident.
2. **AT pool quests place candy-citizen content in every world**
   (TheCandyTax, StrongestInOoo — VillagePopulationBuilder pool).
   LEGACY-CONTENT.md says re-skin/retire "in a future content pass."
   Not W0 scope; W1 (Spread content) is the natural moment. User
   call.
3. **Portrait panel width** (8 chars truncates "Recension"): W0 ships
   strip-"the "+truncate; a wider panel is a UI-pass decision.

---

## 7. Implementation log

### W0.1 — WorldClock ✅ (2026-08-11)

**Shipped.** `Assets/Scripts/Gameplay/Turns/WorldClock.cs` — stateless
band derivation over `TurnManager.TickCount` (C1: the clock already
existed; this is a *reading* of it, not a second clock). `DayBand`
enum; `DayLengthTicks = 1200`, four 300-tick bands; `GetBand(tick)`
pure with a negative-clamp; `BandName(band, depth)` switching surface
↔ catacomb vocabulary at depth > 0 and treating depth −1 (every
non-Overworld zone ID) as surface; `CurrentTick` reading
`TurnManager.Active`.

Session state is one nullable `_lastBand` for the change-diff:
primes silently on first observation (a load must not announce the
hour), emits exactly one `turn/BandChanged {fromBand, toBand, tick}`
per crossing however many bands a jump skipped.

Wiring: `InputHandler.EndTurnAndProcess` after
`ZoneTileStateSystem.OnPlayerTurnEnd` — deliberately NOT inside
`TurnManager.EndTurn` (fires per ACTOR; owns the message-log turn
divider). `GameBootstrap.DoStart` calls `WorldClock.Reset()` beside
the existing `MessageLog.Clear()`, because domain reload is off and a
restarted session would otherwise diff against the previous
session's last band.

**No MessageLog output** — in-world flavor is voice-gated content
(`Lore/Voices/VOICE-CARDS.md`); W0 ships the mechanism, later phases
ship the words.

**Tests:** 15 new (`WorldClockTests.cs`) — band boundaries incl. the
half-open 299/300 split, day wrap, negative clamp, both naming
vocabularies incl. the depth −1 case, prime-without-emit, same-band
silence (counter-check), single crossing, multi-band jump emits once,
midnight wrap, reset re-primes, read-through to the live TurnManager,
and clock-jump tracking (rest's +60 — a clock that only tracked
turn-taking would freeze through a night's sleep).

**Self-review (§5):**
- 🟡 *fixed pre-commit* — `CurrentTick_NoActiveTurnManager_IsZero`
  claimed to test the `?? 0` branch, which is unreachable: nothing
  ever clears `TurnManager.Active` (C11), so once any test constructs
  one it is non-null for the domain's life. Renamed to
  `CurrentTick_ReadsThroughToTheActiveTurnManager` with the honesty
  bound in-comment. A test whose name lies is worse than no test.
- 🔵 `Reset()` + `ResetForTests()` alias — production (bootstrap)
  needs the former, the codebase convention is the latter. Kept both;
  the alias is one line.
- 🧪 Band widths (300 ticks ≈ 30 player actions) are authored from the
  tick economy, not from play. Tunable constant; wants a live pass
  once W1 gives the bands something to *do*.
- ⚪ Deferred: hour-band predicates for storylets/conversations —
  no consumer until W1 (dusk glow) and W2 (Height glare).

**Tests: 6429 → 6444 (+15). All green.**

### W0.2 — Per-zone ambient + LightMap cache fix ✅ (2026-08-11)

**Shipped.** `Zone.AmbientLevel` (+ `Zone.DefaultAmbientLevel = 0.4f`,
shared with LightMap so the world and the renderer can't disagree
about "ordinary"). `LightMap.Compute` reads it; the public
`AmbientLevel` became a get-only property echoing the last computed
zone (keeps `ZoneRenderer.cs:2162`'s mote gate and all 9 existing
assertion sites working, now more correct — gate and brightness
finally describe the same zone). `GetBrightness` OOB returns the
zone's ambient, not a renderer constant.

**The cache-key fix — two latent bugs closed** (plan §1 C3). The key
was EntityVersion + EquipmentVersion; it is now also zone reference +
AmbientLevel + AmbientTint, compared **by value** so the day-cycle
can't force a recompute every turn:
- Ambient/tint changes rendered *nothing* until some creature moved.
  The entire catacomb light-economy and the whole day-cycle would
  have been silently inert.
- The renderer keeps ONE LightMap across zone transitions. Two zones
  with equal EntityVersion — trivially, two freshly-loaded ones —
  rendered each other's light. Harmless while every zone shared 0.4;
  a showstopper the moment ambient varies. Fixed before it could
  ever be seen.

`OverworldZoneManager.OnZoneGenerated` assigns ambient (surface and
`GetDepthAmbient(depth)`), and save/load mirrors it —
**FormatVersion 4 → 5**, which rejects every existing save. Accepted
pre-1.0 per the tripwire's own policy note, and moot in practice: an
old save carries the old noise-generated map, so the overhaul is a
new-world change regardless.

**No visual change ships.** Every zone is still 0.4; `GetDepthAmbient`
deliberately returns the historical flat value. The shape exists and
is tested so W5 replaces one method body instead of also inventing
where the number comes from.

**Tests:** 11 new — 9 in `ZoneAmbientLevelTests.cs` (field default,
LightMap reads the zone, a bright-vs-dark counter-check, OOB, the
property echo, the two cache bugs, tint invalidation, and an
early-out counter-check) + 2 save round-trips
(`Gap_Zone_AmbientLevel_RoundTrips` and an unset-defaults
counter-check guarding a pitch-black-world failure mode).

**Self-review (§5):**
- 🟡 *fixed pre-commit* — my own `Compute_NothingChanged_StillEarlyOuts`
  was **vacuous**: it built a light-source entity and never added it
  to the zone, so recompute-or-not produced the same answer. It
  certified the perf contract without testing it. Rewritten to mutate
  a light Part **in place** — a change no cache input can see — so a
  lost early-out now shows up as the cell going dark.
- 🟡 *methodology, disclosed* — the RED step was **compressed**: tests
  and implementation landed in one pass, so I never observed the two
  cache tests fail. Their RED is provable by construction (the old
  key holds EntityVersion and EquipmentVersion constant in both
  fixtures, so the early-out was unavoidable), but that is a proof,
  not an observation. Recorded rather than glossed.
- 🔵 `GetDepthAmbient` is a constant function today. Deliberate (W5
  owns the ladder) and documented in-method with the two constraints
  W5 inherits: the introspection doc's quit-trigger warning, and the
  `RememberedColor` 0.2 floor below which visible cells render darker
  than remembered ones.
- ⚪ Deferred to W5: reconciling the *second* ambient system
  (`LightSourceSpriteHook`'s Light2D global dim, gated by a ZoneID
  substring heuristic whose Cave/Ruins branch is dead for all
  `Overworld.*` IDs). Left alone here so nothing double-dims.

**Tests: 6444 → 6455 (+11). All green.**
