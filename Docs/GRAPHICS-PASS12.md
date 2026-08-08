# Graphics Pass 12 — Blueprint-Keyed Terrain Identity, Farming & Village Fixture Tiles

> 10 new 16×16 sprites + the first blueprint-keyed (rather than
> glyph-keyed) resolution tier in `EnvironmentSpriteRenderer.ChooseTile`.
> Origin: user directive 2026-07-27 — "create 10 16x16 tiles for the
> environment: look at what glyphs are used the most and what they're
> used for, then plan the tiles, create them with piskel, add them to
> the game so they render."

**Status:** ✅ shipped 2026-07-27 (art + wiring + resolver tests).
Verification state at commit: offline compile green; targeted suites
36/36 green on the live editor (all 9 new resolver pins +
EnvironmentSpriteRendererTests + SpriteEnvToggleControllerTests). The
full-EditMode regression sweep did NOT re-run post-change at commit
time — the editor session dropped mid-gate (see §6). CLOSED in Pass
13 (2026-08-01): full suite 5723/5723 green with all Pass 12 code
included. Live sprite-mode eyeball (backslash toggle) is the
outstanding manual check — see Honesty bounds.

---

## 1. Glyph survey — how the 10 were chosen

Method: parse every blueprint's `RenderString` from Objects.json,
cross-reference all blueprint names referenced in
`Assets/Scripts/Gameplay/World/Generation/` (builders place by palette
variables, so string-literal call-site grep alone under-counts), and
mark each against the glyph set Passes 7–11 already claim
(`# . ~ = - + ' ^ o | ; t T * _ > < , 0 % $ h [ !`).

Finding: the RAW environment glyphs are nearly all claimed — the gaps
are **identity collisions**, where one glyph is shared by entities that
deserve different tiles:

| Problem class | Cases | What sprite mode showed before this pass |
|---|---|---|
| Terrain identity | `Grass`, `Sand`, `Bank` all paint `.` | All three rendered as the generic stone-floor atlas — a jungle meadow, a desert, and a riverbank looked identical |
| Farming (SM1-7 feature) | `*Crop` stage 0 paints `.`, CandyCarrot sprout paints `t`, Emberwheat sprout paints `i` | Planted seeds VANISHED into the floor sprite; a candy-carrot sprout rendered as a **cactus**; emberwheat kept its bare glyph |
| Village fixtures | `Well` `O`, `TinkersForge` `n`, `AlchemyStill` `&` uncovered; `MarketStall` `=` | Well/forge/still stayed CP437 glyphs in sprite mode; the market stall rendered as a **bed** |

## 2. The 10 tiles

All 16×16 (the game's true cell size — note GRAPHICS-PASS7.md's
"16×24" line is doc drift; `CP437TilesetGenerator.GlyphSize = 16` and
every shipped sprite is 16×16), PPU 16, point-filtered, in
`Assets/Sprites/Environment/`:

| File | Claims | Design |
|---|---|---|
| grass.png | `Grass` | Full-fill dark-green turf, speckle + blade marks |
| sand.png | `Sand` | Full-fill muted tan, ripple dashes + bright grains |
| bank.png | `Bank` | Full-fill muddy shore, tufts on one edge, pebbles |
| crop_seed.png | any `*Crop` at stage 0 | Transparent bg; tilled mound + 3 seeds + tiny shoot |
| candycarrot_crop.png | `CandyCarrotCrop` stage 1+ | Transparent bg; leafy sprout, orange shoulder in the soil |
| emberwheat_crop.png | `EmberwheatCrop` stage 1+ | Transparent bg; 3 golden stalks, ember tips |
| well.png | `Well` | Roof + posts + rope/bucket + stone ring with dark mouth |
| market_stall.png | `MarketStall` | Red/cream striped awning, counter with goods |
| forge.png | `TinkersForge` | Stone hearth, glowing mouth, anvil on top |
| alchemy_still.png | `AlchemyStill` | Outlined copper alembic, tube into a green flask, flame |

**Crop tiles are transparent around the plant on purpose:** the
wet-soil feedback (`CropPart.WET_SOIL_BG` block on the bg tilemap,
below the overlay) must stay visible around a watered crop. A full-fill
crop tile would have hidden the farming feature's core visual signal.

## 3. Renderer wiring — the blueprint-keyed tier

`ChooseTile` now runs a blueprint-keyed block BEFORE all glyph
branches, built on two PUBLIC pure-static resolvers (directly
test-pinned, no reflection):

- `ResolveFixtureKind(blueprintName)` → `EnvFixtureKind` — EXACT-name
  matches only. Near-misses stay unclaimed by contract: SandstoneFloor
  and StoneFloor are floors, SilverSand is a carried item,
  WellGroundMarker is a placement helper, WellKeeper is a creature.
- `ResolveCropKind(blueprintName, growthStage)` → `CropSpriteKind` —
  `*Crop` suffix + stage. Stage 0 → generic seed mound (safe for any
  future crop); stage ≥1 → only KNOWN crops map (an unknown sprout
  keeps its CP437 glyph rather than borrowing a look); stage −1
  (no CropPart) → None; stage ≥2 (corrupt save) → treated as sprout.

Null tiles (missing PNG) fall through to the pre-Pass-12 behavior —
the glyph pipeline is the graceful fallback at every step.

**Perf note:** the block adds one `GetTopVisibleObject` per painted
cell per `PostRender` — the same lookup the Pass 10 entity pre-pass
already performs for EVERY cell each PostRender, so the complexity
class is unchanged. No allocations (enum switches on cached Tile
fields).

## 4. Tests

`EnvironmentSpriteRendererBlueprintTests.cs` Pass 12 section — 9 tests
written RED-first (CS0117 against the missing resolvers, then GREEN):
terrain identity + fixtures match; near-miss names (5) refuse;
null/empty refuse; seed stage generic for unknown crops; known sprouts
map; unknown sprout keeps glyph; produce (`CandyCarrot` — no `Crop`
suffix) refuses; no-CropPart sentinel refuses; corrupt high stage
clamps to sprout.

## 5. Honesty bounds

- EditMode pins the resolver contracts and compile-verifies the
  wiring; it CANNOT drive the AssetDatabase sprite loads or the
  overlay tilemap end-to-end (Init + LoadSprites are editor-play
  paths).
- NOT machine-verified: how the 10 tiles read in a live zone at game
  zoom, sprite-mode toggle on (`\` key). Manual check: new game →
  village (well/stall/grass everywhere) → plant a seed → water it →
  watch seed-mound → sprout tiles with the wet-soil block visible
  around the plant.
- Known accepted quirk: produce items (`%`) still render via the
  existing mushroom-sprite overload (pre-Pass-12 behavior, unchanged).

## 6. Process notes

- Passes 10 and 11 (chest/lantern/bed/corpse disambiguation) shipped
  without a GRAPHICS doc section — recorded here for the trail; their
  contracts live in EnvironmentSpriteRendererBlueprintTests.
- Art authored via the piskel MCP (one 16×16 project per tile),
  exported at scale 1 directly into Assets/Sprites/Environment/ with
  hand-written .meta files (Sprite/single, PPU 16, point filter —
  Unity's default import would have been PPU 100 + bilinear).
- Session note: verification fought infrastructure — the caves-of-ooo
  editor had been closed and the MCP server was down (a different
  project's editor was running). Recovery: restart server, relaunch
  editor, force a domain reload (the project's CodexMcpKickstart only
  retries its bridge 5×2s at load, and the server was briefly down in
  exactly that window). The targeted suites ran green; the editor
  session then dropped again before the full sweep, which is why the
  full-suite gate is deferred to the next editor open.
