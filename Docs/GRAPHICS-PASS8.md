# Graphics Pass 8 — 15 sprite sets + enhanced lighting

> **Living plan + progress doc.** Pass 7 shipped 5 environment sprite
> sets (walls, floors, water, doors). Pass 8 adds **15 more** to cover
> the rest of the game's environmental glyph vocabulary, then layers
> in URP 2D dynamic lighting on top of the new sprites.

---

## Status banner

| Field | Value |
|---|---|
| **Pass** | 8 of N |
| **Last updated** | 2026-05-09 |
| **Branch** | `feat/graphics-pass8-sprite-expansion` |
| **Sub-milestones complete** | 0 / 16 |

---

## Strategic decisions

### 15 distinct sprite sets, glyph-keyed

The existing `EnvironmentSpriteRenderer` (Pass 7) is glyph-keyed: it
scans rendered cells, extracts the CP437 glyph, looks up a Tile,
paints overlay + clears main. Pass 8 extends the same pattern — each
new sprite set adds:
- A 16×16 PNG in `Assets/Sprites/Environment/`
- A `_xxxSprite` field + `_xxxTile` field
- A `LoadSingle` call in `LoadSprites()`
- A `ScriptableObject.CreateInstance<Tile>()` in `BuildTiles()`
- A glyph→tile mapping arm in `ChooseTile()`

### 15 chosen sets (from glyph inventory)

Picked for: (a) glyph is unambiguous (no clash with Pass 7 claims —
`#./~=-+'` already taken), (b) representative across biomes /
contexts, (c) sets up the lighting follow-up (campfire, shrine, etc.).

| # | Glyph | Sprite | Category | Why |
|---|---|---|---|---|
| 1 | `^` | Stalagmite | Topology | Core dungeon decor |
| 2 | `o` | Boulder / rock | Topology | Cave anchors |
| 3 | `|` | Stalactite / reed | Topology | Vertical variety |
| 4 | `;` | Bush | Vegetation | Outdoor zones |
| 5 | `t` | Cactus | Vegetation | Desert biome |
| 6 | `T` | Tree | Vegetation | Forest biome |
| 7 | `*` | Campfire | **Light source** | Lighting hook |
| 8 | `_` | Shrine / altar | **Light source** | Settlement focal |
| 9 | `>` | Stairs down | Navigation | Zone transitions |
| 10 | `<` | Stairs up | Navigation | Zone transitions |
| 11 | `,` | Bones / rubble | Decoration | Atmospheric scatter |
| 12 | `0` | Wooden barrel | Furniture | Breakable obstacle |
| 13 | `%` | Mushroom | Vegetation / food | Cave biome flavor |
| 14 | `$` | Gold pile | Loot | Reward signal |
| 15 | `h` | Chair / stool | Furniture | Settlement interior |

### Iterate, then light

1. **8A** — Generate all 15 sprites (Piskel MCP, in batches)
2. **8B** — Wire into `EnvironmentSpriteRenderer` (single edit pass)
3. **8C** — Compile + tests + play-mode visual verify
4. **8D** — Iterate on sprites that read poorly or sit oddly in cells
5. **8E** — Enhanced 2D lighting: add Light2D components for `*`,
   `_`, `!` (lit), and a soft global ambient drop in dungeons so the
   lights actually pop. Per-light flicker. Per-light color (warm
   orange for `*`, blue-white for `_` shrine, soft yellow for `!`
   lantern).

### Toggle behavior

`SpriteEnvToggleController` (Pass 7) already toggles the whole
renderer on/off — Pass 8's new sprites inherit that toggle. No new
controller needed.

---

## Sub-milestones

### 8A.1 — Generate sprites (batch 1, 5 sprites)
Stalagmite, Boulder, Stalactite, Bush, Cactus.

### 8A.2 — Generate sprites (batch 2, 5 sprites)
Tree, Campfire, Shrine, Stairs-down, Stairs-up.

### 8A.3 — Generate sprites (batch 3, 5 sprites)
Bones, Barrel, Mushroom, Gold pile, Chair.

### 8B.1 — Wire all 15 into `EnvironmentSpriteRenderer`
Single MOD diff: extend `LoadSprites`/`BuildTiles`/`ChooseTile`.

### 8B.2 — Pass8SpriteImporter Editor menu
Configures import settings (PPU=16, Point, Uncompressed) for the
15 new PNGs in one click.

### 8C — Tests + Play-mode verify
Run EditMode, enter Play mode, screenshot a populated zone, eyeball
each sprite for readability + cell alignment.

### 8D — Iterative polish
Per problem-sprite, edit pixels via Piskel MCP, re-export, refresh.
Recheck via screenshot. Repeat until each reads cleanly.

### 8E.1 — `LightSourceSpriteHook` MonoBehaviour
Scans cells; for each `*` (campfire) and `_` (shrine), spawns a
Light2D child with biome-appropriate color + flicker.

### 8E.2 — Ambient dim in dungeons
Reduce Global Light 2D intensity in cave/dungeon zones so the
point lights actually create contrast. Outdoor zones keep current
ambient. Toggle via biome metadata.

### 8E.3 — Light flicker controller
Per-Light2D component: sine-wave + perlin noise on intensity, so
campfires breathe, shrines pulse subtly.

### 8E.4 — Tests + verify
EditMode tests for the LightSource attach logic; Play-mode
screenshot of a campfire-lit dungeon.

---

## Sub-milestone progress

| Sub-milestone | Status | Tests | Commit |
|---|---|---|---|
| 8A.1 Sprites batch 1 (5) | ⏳ | n/a | — |
| 8A.2 Sprites batch 2 (5) | ⏳ | n/a | — |
| 8A.3 Sprites batch 3 (5) | ⏳ | n/a | — |
| 8B.1 Renderer wiring | ⏳ | — | — |
| 8B.2 Pass8SpriteImporter | ⏳ | n/a | — |
| 8C Play-mode verify | ⏳ | — | — |
| 8D Iterative polish | ⏳ | — | — |
| 8E.1 LightSourceSpriteHook | ⏳ | — | — |
| 8E.2 Biome ambient dim | ⏳ | — | — |
| 8E.3 Flicker controller | ⏳ | — | — |
| 8E.4 Tests + verify | ⏳ | — | — |
| **TOTAL** | **0 / 11** | — | — |

---

*Updated as each sub-milestone ships.*
