# GRAPHICS PASS 15 — Sprite Reintegration: Flowing Ground, Honest Objects, Verified by Eye

> Status: APPROVED + IN PROGRESS (user sign-off 2026-08-08; monsters
> stay ASCII). Brainstorm additions folded in: **wall top-face**
> (lighter 3-4px top band where no wall above — walls read as solid
> blocks), **overlap skirts + priority stacking** for ground fringes
> (soft material overhangs hard: grass laps sand laps stone),
> **tall 16×20 actors** (bottom pivot, overhang the cell above —
> actors break the tile plane dimensionally), **value zoning**
> (terrain 30-60% brightness, actors get the extremes; enforced by
> generator gate), and an **automated squint test** in the V-loop
> (downscale 4×, assert actor-vs-neighborhood contrast). Deferred to
> Pass 16: dual-grid rendering, day/night light curve, wall-edge AO,
> drop-shadow bg pass, monster token plates, zone fade + POI banners,
> per-biome ambient particles.
> Sources: (a) full audit of the dormant Pass 7-14 sprite system,
> (b) deep study of the farming project
> (`/Users/steven/farming/.claude/worktrees/farming-sim-sprite-setup-d2ab60`),
> whose rendering the user called "very clear and seamless."
> The prior attempt failed the eyeball test because it was never
> eyeballed: the gate went off 2026-05-10 and Passes 12-14 were all
> authored AFTER that — compile-verified, never once seen running.

## 0. What the farming game taught us (the five laws)

1. **Macro-field slicing.** Ground variation must live LARGER than the
   tile: author one seamless toroidal 64×64 image, slice 4×4, index by
   `(x mod 4, y mod 4)`. The grid disappears at the source. One modulo.
2. **Flat fills; boundaries are a 1-2px darker LIP, never a blend,
   never noise.** Per-tile speckle prints the same mark every 16px and
   reads as damage — our Muted Overgrowth tiles are speckled; that
   speckle must go (style guide gets amended, sprites regenerated).
3. **Objects get a 1px dark ink outline; terrain NEVER does.** This
   single contrast separates "thing on ground" from "ground" better
   than any saturation trick.
4. **Edges live on a transparent overlay, generated as a complete
   family** (SDF/scallop sets), over an untouched base — N materials
   need N fringe families, not N×N transition tiles.
5. **Pixel-perfect discipline enforced in code**: import postprocessor
   (Point/uncompressed/no-mips/FullRect/extrude-0), atlas padding,
   integer sorting bands, snap-to-pixel camera.

## 1. Phase R — repair the substrate (blockers found by audit) — ✅ SHIPPED

R1-R5 all landed (2026-08-08): sprites moved to
`Assets/Resources/Sprites/Environment/` (git mv — GUIDs intact) with a
runtime `Resources.Load` path; the vertical-mirror fix (`zoneY =
Height-1-y` feeds every zone lookup, plus the player-torch fix in
LightSourceSpriteHook); release-restores-glyph claims (struct Claim
remembers displaced tile+color; `NotifyMainTilemapCleared` handles the
full-repaint path so stale glyphs never resurrect); the
SpriteImportPostprocessor (enforced pixel-perfect settings, kills the
24 stale per-platform compression overrides, preserves atlas Multiple
mode); single top-entity fetch per cell + crop scan only on `*Crop` +
`COO.EnvSprites.PostRender` ProfilerMarker + claim capacity 2048.
Verified by 5 NEW harness tests driving the real component end-to-end
(real Resources sprites, real tilemaps, real zone) — the class of test
whose absence hid all three defects. Suite 5915 → 5920.

## V-loop round 1 (2026-08-08) — three live-screenshot fixes

The eyeball loop immediately earned its existence. Three defects no
test saw, each found in a screenshot, diagnosed with read-only
execute_code queries against the RUNNING game, fixed, and re-verified:

1. **The river was still ASCII** — the animated-water renderer strips
   water glyphs off the main tilemap before our pass, so the
   glyph-keyed water branch never fired. → Water claims by BLUEPRINT
   in the pre-pass.
2. **"Specks" — scattered dark holes across the ground** — same root
   cause generalized: the animated env also claims grass/floor
   glyphs; overlay-tile census (execute_code) showed the holes were
   exactly its claimed cells. → ALL ground claims are now
   blueprint-driven and glyph-independent; our overlay (order 3)
   covers its layers (order 2).
3. **Objects floating in dark boxes** — sprites' transparent margins
   revealed the bg contrast box. → Every claim also paints the cell's
   ground material into the BG tilemap; ASCII actor letters likewise
   stand on terrain now.

Live result (zoomed screenshots, starting town): continuous flowing
ground, river with scalloped shoreline lips, wall runs reading as
solid capped blocks, objects/actors sitting directly ON the ground.

**Punch list for round 2 (Phase S):** — ✅ ALL SHIPPED (2026-08-08,
V-loop round 2). Item-by-item record:

1. **FOV dimming — ROOT CAUSE was deeper than the punch list knew.**
   The tint plumbing was added (per-cell `tint` = white when visible,
   `RememberedTint` gray-blue when remembered, threaded through every
   blueprint-tier claim + `PaintGroundUnderAscii`), but the real
   reason dimming never reached sprite terrain: **`MakeTile` never
   cleared `Tile.flags`, and a fresh Tile defaults to
   `TileFlags.LockColor` — every `SetColor` on a claimed cell was a
   silent no-op.** The glyph-tier claims had been copying the dim
   glyph color correctly all along; LockColor threw it away. Fixed in
   MakeTile (+ defensive `SetTileFlags(None)` at every claim, paint,
   and release-restore site).
2. **Fog gating.** Unexplored cell → no claim of any kind (the
   unexplored block stays). Remembered-not-visible → TERRAIN-ONLY
   claims (`TerrainEntityOf` mirrors `RenderRememberedCell`'s
   layer≤1 predicate exactly), dimmed; actors/items never resolve in
   fog. Live census: claims=41, onUnexplored=0, actorInFog=0.
3. **Ink outlines + player highlight.** `ArtTools/coo_outline_pass.py`
   — idempotent 1px ink ring on 41 object/actor sprites (2247 ring
   px), 8-connected near-black ring on the player (62 px). Player
   cell's bg ground patch warms to `PlayerHighlightTint` (S4).
4. **False-identity guards.** `GlyphClaimAllowed(glyph, blueprint)` —
   census-built allow-lists over every colliding glyph (traps '^',
   troll/warhammer 'T', bandit 'h', veins/runes '*', grimoires '+',
   seeds ',', foods '%', torch '/'); the water-glyph branch now
   requires a real water blueprint (Viper '~' stays a snake); doors
   guard on "Door". The '*' guard also kills the phantom fire-lights
   on ore veins (the light hook keys on the overlay tile literally
   named "Campfire").
5. **9 role NPCs.** Generated as palette-kin of the villager base
   (robe triad swap): Weaponsmith/Armorer/Apothecary/Arcanist/
   Provisioner + Cave/Desert/Jungle/Ruins hermits. Renderer side: a
   blueprint-keyed `_namedActorTiles` dictionary — future named NPC =
   drop a PNG + one table row, no enum/field/switch triple.
6. **Bush restyle.** Flat dark mass + two flat mid-tone lobes + 3px
   glint (variation by SHAPE, not speckle) + trunk peek + ink ring.
7. **Automated squint test.** `ArtTools/coo_squint_test.py` — (a)
   16px-periodicity autocorrelation spike detector (grid regression
   alarm), (b) value-zoning bright-fraction gate. Live jungle frame:
   grid spike x=-0.0003 y=-0.0012 (tol 0.06) PASS; bright
   fraction=0.014 (max 0.18) PASS.

**Two REAL bugs found by the round-2 live sweep, both in
`GlyphGhostRenderer` (Pass 6 ghost trails — latent for the feature's
entire life because the gate shipped OFF):**
- **Iterator invalidation:** the decay loop wrote `_ghosts[pos] =
  ghost` inside its foreach → `InvalidOperationException` EVERY
  frame once any ghost decayed (spammed 10× within seconds of
  loading a save into a jungle full of movers; aborts LateUpdate).
  Fixed with the scratch-list snapshot pattern; pinned by
  `GlyphGhostRendererDecayTests` (single + many-ghost shapes).
- **Vertical mirror (the R2 bug class again):** `SpawnGhost` wrote
  ghost tiles at raw zone coords — every ghost spawned on the
  MIRRORED row and sampled the wrong cell's glyph. Same fix shape as
  R2 (`Height-1-y`); pinned by the mirror assert in the decay test.

Tests 5924 → 5935 (+11: 4 fog, 1 guard bank, viper, spike-trap,
weaponsmith, player-highlight, 2 ghost pins). Suite green.

**Deferred to round 3:** town live-check of the 9 NPC sprites +
player highlight in situ (the test save was mid-jungle; both are
unit-pinned), river shoreline re-verify under fog, '/' torch/key
sub-cases, Marceline/Farmer/Undertaker generic-villager mapping.
GraphicsPolish gate: ON in working tree for the loop; ships committed
only after the full checklist passes.

## V-loop round 3 (2026-08-08) — town live sweep

Setup: save directory backed up
(`…/DefaultCompany/caves-of-ooo.backup-round3-*`), then NEW GAME into
the Starting Town. A background multi-lens adversarial audit
(fog state machine / claim lifecycle / guard census / perf hot path,
each finding adversarially refuted) ran in parallel; findings folded
in below when confirmed.

**Live-verified working:** river with the full shoreline family
(water_m* + e_n/e_s/e_e edges + ic_se inner corner resolving along
the real bank), wall_v* top-faces, Apothecary teal role sprite in
situ, player sprite + warm ground highlight visible, bushes/chest
with ink rings, compass stones as tinted boulders, fog gating intact
in town.

**Three defects found by the claims-census (execute_code), all
invisible to the 5935 tests:**
1. **The red plaza cross** — the campfire's four CampfireGroundMarker
   cells claimed the stone-floor macro TINTED RED (glyph-color copy
   from the markers' warm '.'), and the campfire itself devolved to a
   red ASCII flicker frame ('z') because its GlyphVariants defeat the
   '*'-keyed glyph tier. → Campfire joined the blueprint-keyed entity
   pre-pass (sprite on every flicker frame + fire light preserved);
   ground/water families in the glyph tier now claim authoredColor
   (lighting VALUE only — a macro carries its own palette, no hue
   leak).
2. **MarketStall blanked to nothing** — the animated-env renderer
   claims '=' glyphs off the main tilemap, and the sprite pass's
   null-glyph branch skipped ALL resolution. → The null-glyph branch
   now runs the blueprint tiers (ChooseTile with '\0'), so fixtures
   and actors survive glyph-stripping.
3. **Tinker rendered as a bare letter** — blueprint missing from the
   actor table. → villager-kin.

Also shipped: Farmer/Undertaker/Marceline role sprites (Marceline
gets pale vampire skin + night-violet robe via the new SKIN_OVERRIDES
hook in coo_outline_pass.py); roster pin 9 → 12.

Process note: the frozen-compile-pump editor deadlock struck TWICE
this session (documented in auto-memory `unity-compile-verification`);
the second occurrence was caught by the frozen-test-count check
(5935 after adding 4 tests = stale assembly) — the suite had
"passed" against the pre-fix binary. Editor restart cures it.

### The background audit's verdict (26 agents, 17 confirmed / 5 refuted)

Four lenses (fog state machine / claim lifecycle / guard census /
perf hot path), every finding adversarially re-verified against
source. Fixed this round:

- 🔴 **Release-clobber** (found by TWO lenses independently): on the
  incremental path RenderDirtyCells runs BEFORE PostRender, and the
  release loop restored LAST frame's snapshots over its fresh paints
  — a spriteless monster (Viper, DesertBandit…) walking toward a
  stationary player was INVISIBLE until the player moved; dropped
  loot never appeared. Fix: restore only when the main cell is still
  null (our claim's own marker); bg claims carry a `Written` token
  and restore only if unchanged. Pinned by
  `Release_NeverClobbersAFreshRepaint`.
- 🟡 **Overlay over fullscreen UIs**: the sprite overlay (order 3) and
  ghost overlay kept the last gameplay frame's tiles above the main
  tilemap the inventory/quest UIs paint on (order 0).
  `ReleaseAllClaims()` + `ClearGhosts()` now run on ZoneRenderer's
  Paused transition. (AnimatedEnvironmentRenderer has the same
  pre-existing issue — deferred, noted below.)
- 🟡 **Shoreline actor-tracking**: IsLandAt keyed off the top entity,
  so a viper swimming the river dragged shore scallops with it —
  through fog, that tracked an unseen enemy. Terrain-scan now.
- 🟡 **Reskin guard**: quest builders reskin base blueprints via
  RenderString (BMO = Villager as cyan 'b'; dirt gnomes = Snapjaws as
  'g'). Actor tiers now require the CANONICAL glyph
  (NamedActorSprites carries it; KindCanonicalGlyph for enum kinds) —
  live-verified: BMO renders his honest 'b' in town.
- 🟡 **Stage markers**: Well/Oven/Lantern GroundMarkers' repair-stage
  color signal (ash → gold) was being replaced by gray macro stone —
  they keep the colored dot now; CampfireGroundMarker still claims.
- 🟡 **Ghost fixes ×3**: ghosts now capture the MOVER's tile+color
  BEFORE the move (they duplicated the repainted terrain '.');
  the ghost tilemap clears TileFlags so the fade actually fades
  (the MakeTile LockColor root cause, third sighting); _lastKnown
  prunes entities that left the zone (unbounded growth + pinned
  object graphs).
- 🔵 Fog-dim unified (glyph-tier fog claims used the ~0.2 remembered
  gray while blueprint-tier used 0.4 RememberedTint — one tint now);
  '/' guard comment corrected (26 painters) + polarity documented.

**Deferred to a dedicated round-4 PERF pass** (profile-first per
PERF-FOUNDATION; all confirmed by the audit's perf lens):
release/reclaim tilemap churn (~13-15k writes/repaint — the big one),
bg ping-pong on ASCII cells, LightSourceSpriteHook `t.name` string
allocs, ExtractGlyph Substring allocs (pre-existing), shoreline
re-classification of static water, AnimatedEnvironmentRenderer
pause behavior, ghost decay per-redraw-vs-per-time normalization.

Round-3 close: tests 5935 → 5948 (+13). Suite green (one documented
pre-existing FungalInfectionContagion order-dependent flake, passes
in isolation). Live re-verified in town: campfire sprite + light at
the river bank (red cross gone), plaza NPCs with rings, BMO honest,
shoreline lips under fog. Jungle save restored from backup after the
new-game test runs.

## V-loop round 4 (2026-08-08) — the perf pass (profile-first)

**Instrumentation first** (per PERF-FOUNDATION): permanent live
counters `EnvironmentSpriteRenderer.Perf` (frames by path, cells
resolved, claims, tilemap writes, avg/max ms) — readable via
execute_code, reset per measurement window. They stay as the sprite
pass's observability surface.

**Baseline measured live** (jungle save, walking): the full pass
costs **~2000 cells, ~1300-1600 claims, 5300-6400 tilemap writes,
avg 2.7-3.0 ms, max 3.4 ms per repaint** — and the pre-round-4 code
paid exactly that on EVERY repaint, including NPC-only turns while
the player stood still. (The audit's 13-15k-writes estimate was ~2×
pessimistic; the real number is still the dominant per-step cost.)

**The fix — incremental claims:**
- Claims moved from per-frame lists to persistent
  `Dictionary<Vector3Int, Claim>`; the per-cell resolution body
  extracted into `ResolveCell`.
- `PostRender(zone, w, h, dirtyKeys)`: FULL path (null) releases +
  rescans everything (player moves, zone changes — unchanged
  semantics); INCREMENTAL path releases + re-resolves ONLY the dirty
  cells **plus their 8-neighborhoods** (wall top-face variants and
  shoreline masks are functions of neighbors).
- ZoneRenderer's dirty path passes `_dirtyCells` (cleared AFTER the
  sprite pass now).
- Round-3's clobber guard + snapshot-preservation carried into the
  targeted release helpers; re-claim without release keeps the
  ORIGINAL displaced snapshot (never captures our own macro).
- Also: LightSourceSpriteHook classifies tiles ONCE per instance
  (was 3 `t.name` string allocs per lit-scan cell) + reuses its
  scratch set; ExtractGlyph caches per-tile parses (was
  name+Substring per ASCII cell per rescan).

**Measured after:** stationary turns (70 waits, no visible movement)
→ **0 passes, 0 writes, 0 ms** — the pass simply doesn't run.
Player-move repaints unchanged (~3 ms full pass — same work as
before, now only when actually needed).

**Honesty bounds:** a live NPC-move incremental frame was NOT
captured (the cave zone the walk reached has stationary camp
merchants; no wanderer crossed the FOV during the windows). The
NPC-turn claim rests on (a) branch equivalence — the incremental
path rides the exact dirty-set branch RenderDirtyCells uses, the
branch the round-3 invisible-monster bug proved NPC moves take —
and (b) three unit pins: ≤9 cells resolved per dirty cell (vs 2000),
honest re-resolution under the clobber guard, and neighbor shoreline
updates. Expected NPC-turn cost: ~9-45 cells ≈ tens of writes ≈
~0.05 ms — roughly two orders of magnitude below a full pass.

Bonus: the measurement walk crossed into a cave zone
(Overworld.9.10.0) — the checklist's "cave wilderness" screenshot
item, verified visually in passing (flowing dark stone, capped wall
runs, outlined rubble/boulders, fog correct). Checklist still open:
a deep strata zone.

Tests 5948 → 5951 (+3 incremental pins). Save restored from backup
after the measurement session (autosaves had moved the player).

**Still deferred:** bg ping-pong fine-tuning (largely mooted — bg
writes now happen only on resolved cells), AnimatedEnvironmentRenderer
pause behavior (pre-existing), ghost decay per-time normalization,
strata-zone checklist screenshot, committing the GraphicsPolish gate
ON (awaiting user's call that the pass looks done).

## V-loop round 5 (2026-08-09) — THE BESTIARY + items + interactables + ambient + buried mechanics

User direction: "still many sprites to go" (the monsters-stay-ASCII
carve-out ends), plus mid-round additions: "environment additions
with gameplay interactability", "more ambient motion (non-fire)",
"continue surfacing mechanics that live deep in the code but aren't
used".

**A — The bestiary (44 creature sprites).** `ArtTools/coo_bestiary.py`
— 12 body ARCHETYPES (quadruped, biped brute, cloaked rogue, serpent,
arachnid, flyer, blob, skeletal, sentinel construct, tendril, buried
lurker, imp, snapjaw-boss) so families share silhouettes while each
creature keeps its ASCII color identity (census-driven palette per
ColorString). Wired through the SAME blueprint-keyed actor mechanism
as the role NPCs — `CreatureSprites` table with canonical glyphs, so
the reskin guard still protects quest reskins (dirt gnomes stay
honest 'g'). Live-verified: a GiantSpider claimed its sprite in the
jungle save (claims census), and the player killed it.

**B — Item bodies (13) + tint-carried identity.**
`ArtTools/coo_items.py` — near-gray family bodies (vial, book, gem,
key, torch, meat, fruit, seed, armor, bone, vein, scroll, grenade)
claimed with the glyph's COLOR COPIED: one vial serves all 14 tonics,
one gem all ores, one book all 21 grimoires. Ore veins ('*') get the
crystal-flecked rock face (replacing round-2's honest-ASCII stance —
a tinted mineable node beats a letter). Glyph fallbacks: '[' armor,
'!' vial.

**C — Interactables (5 new, zero new C#).** BerryBush→WildBerries,
Beehive→Honeycomb (both new FoodItems), HollowStump→gold cache,
MushroomRing→mushrooms (all HarvestablePart), Signpost (flavor solid).
Placed in Cave/Jungle/Ruins tier-1 population tables; fixture-tier
sprites (blueprint-keyed pre-pass); generation-test fixture stubs
added (the 3×-bitten gotcha, pre-empted this time).

**D — Ambient motion: AmbientMotesRenderer.** One component, four
mote kinds — Drip (stalactites, fast cyan fall), Spore (mushroom
rings, slow green rise), Leaf (jungle trees, sampled 1-in-6 capped
24, wide sway fall), Bee (hives, tight gold orbit). Registered in
ZoneRenderer's SetZone scan alongside the campfire embers; paused
with them. Live: 24 leaf anchors registered in the jungle save.

**E — Buried mechanics surfaced (27-agent audit: 21 confirmed).**
Shipped this round (content-only): 3 GAS GRENADES
(poison/sleep/stun — GasGrenadePart + the already-wired
ThrowItemCommand detonation; loot rows in BanditCacheT2/WarbandLootT2/
CultCacheT2 + Provisioner/Weaponsmith shop stock), BurnOffGas on
SporeShambler (torch it → spore cloud) + GasImmunity (shambler
immune to its own spores, Rotling to poison — AI gas-pathing changes
too), ConvalescencePool (the heal-over-time liquid finally has a
world source: rare Desert-T2/Cave-T3 spring), GiveInk on the Warren
quest reward. **Round-6 content backlog from the audit:** knowledge
tiers (Reveal/IfSpeakerKnows), HouseVex drama conversations, quest
failure lifecycle (FailQuest/IfQuestFailed), PushNoFightGoal
talk-down pacification, ParalyzedEffect inflictor, SteamEffect
reaction row, exotic liquid pools (~20 more incl. memory-bath
resurrection), GasMask equip-routing (needs a small code layer).

Tests 5951 → 5957 (+6: ape claim + roster-44 pin, creature reskin
guard, tonic tint, vein tint, berry-bush fixture, item-body family
pins). Viper tests updated: the viper now legitimately claims its
OWN snake sprite (never water); clobber/incremental tests use
SteamCloud as the spriteless '~' stand-in. Save restored after the
live session.

| # | Defect | Fix |
|---|---|---|
| R1 | `LoadSprites` is `#if UNITY_EDITOR` + `AssetDatabase` — null in any build, assets strippable | Move PNGs to `Assets/Resources/Sprites/Environment/`, load via `Resources.Load`; keep paths in one manifest |
| R2 | Blueprint tier reads the zone VERTICALLY MIRRORED (`PostRender` passes tilemap-y into `zone.GetCell`; ZoneRenderer paints at `Height-1-y`) — same bug in `LightSourceSpriteHook` player torch | One `zoneY = Height-1-y` feeding all zone lookups; harness test that paints a marked zone and asserts overlay/zone correspondence |
| R3 | Dirty-path repaints blank every non-dirty claimed cell (overlay released, main glyph was nulled last frame) | Stop nulling the main tilemap; paint the overlay ABOVE the glyph (it fully covers 16×16), release = just clear overlay cell |
| R4 | 24 stale `.meta`s carry per-platform DXT compression — banding on the exact tiles covering most screen | Normalize all metas uncompressed; add our own `ArtImportPostprocessor` (farming's, adapted to PPU 16) so it can never regress |
| R5 | Perf: 2-3 redundant `GetTopVisibleObject`/`GetPart` scans per cell ×2000, zero ProfilerMarkers | Single top-entity fetch per cell threaded through tiers; `ProfilerMarker` around `PostRender`; re-baseline PERF-FOUNDATION table |

## 2. Phase G — ground that flows

- **G1 Macro fields** replace the hash-picked floor atlas: seamless
  4×4 toroidal fields (adapted `macro_field()` from the farming
  ArtTools, PIL) for: grass, sand, generic stone floor, and the five
  strata floors as value-shifted recolors of one stone macro
  (sandstone/limestone/shale/slate/quartzite/obsidian finally look
  different — today five strata collapse to one ochre atlas and
  SlateFloor literally renders as the BONES sprite).
- **G2 Edge fringes on a new overlay tilemap** (order between floor
  and objects): generated scalloped fringe families — water shoreline
  first (the river!), then grass-over-sand and grass-over-stone.
  Grass is the universal base, priority model, no N×N matrix.
- **G3 Walls restyled flat+lip** using the existing 4-variant
  classifier; full 16-variant autotile only if screenshots demand it.

## 3. Phase S — honest, readable objects

- **S1 Outline pass**: every actor/fixture/item sprite gets the 1px
  ink ring (regenerated via `outline()`; bodies keep their Pass 13-14
  designs); all terrain speckle removed per law #2. STYLE-GUIDE.md
  amended accordingly.
- **S2 Kill the false-identity table**: the glyph tier gets blueprint
  guards so Viper≠water, traps≠stalagmite, DesertBandit≠chair,
  SleepingTroll≠tree, ore veins≠campfire (they currently even spawn
  fire lights), grimoires≠door, seeds≠bones. Mismatch → honest CP437
  glyph (an honest letter beats a false sprite).
- **S3 Coverage for the town**: sprite the 9 sprite-less friendly
  NPCs the player meets most (5 shopkeepers + 4 hermits — cheap
  palette-kin of the villager base). **Monsters stay glyphs for now**
  — outlined-sprite NPCs + flat ground makes letters MORE readable
  than before; the ~40-creature bestiary is its own future art pass.
- **S4 Player findability**: player sprite gets the strongest ink
  outline + a subtle 1-cell ground highlight underneath (reuses the
  proven bg-tint pipeline).

## 4. Phase V — the eyeball loop (the step that never happened)

Computer-use screen access to Unity is granted. Loop per change-batch:
enter Play (⚠ resets the scene — flagged each time) → screenshot the
Game view → `zoom` into seams/actors → judge against a fixed checklist
(no grid pattern? player found <1s? actor/terrain separation? strata
distinct? no false identities? water edge clean?) → adjust generators →
repeat. Before/after screenshots saved to `Assets/Screenshots/`.
Verification is BLOCKING: the gate ships ON only after the checklist
passes on live screenshots of: starting town, cave wilderness, a
strata zone, the river, and one combat scene.

## 5. Order & estimates (agent-pace)

R1-R5 (~3h) → G1-G2 (~4h art+code) → S1-S2 (~3h) → V loop interleaved
from first R-complete build → S3-S4 (~2h) → G3 + polish (~2h).
Tests: harness tests for R2/R3 (the class of bug 44 resolver pins
couldn't see), resolver-guard tests for S2, generator self-validation
(palette/size/opacity gates like the farming ArtTools).
