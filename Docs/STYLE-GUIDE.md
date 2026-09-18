# Caves of Ooo — Sprite & Tile Style Guide ("Muted Overgrowth")

> Current production correction (2026-09-06): the user requires actual16×16
> sprites with binary alpha and shared object outline RGB(30,32,28). Assets live
> under `Assets/Resources/Sprites/Environment/`; copy a valid shipped metadata
> template and change only its GUID. The historical no-outline and old-path
> instructions below are superseded for current object art. Existing editable
> item/outline helpers and EQUIPMENT-GROUND-SPRITES-PLAN document the applied pipeline.

> The binding art ruleset for every 16×16 sprite and tile in sprite
> mode. Chosen 2026-07-27 from a five-style comparison board (same
> scene, five rulesets — see `Docs/StyleExploration/`); the user
> selected **Muted Overgrowth**. Every future sprite must pass §5's
> checklist before it ships.

---

## 1. Identity

A quiet, overgrown ruin-world. The world is desaturated moss, stone,
and bone; **saturation is reserved for what is alive or arcane**. No
outlines — readability comes from value contrast against dark ground,
not from ink. Texture comes from sparse single-pixel speckle, never
from gradients or anti-aliasing.

Format facts: **16×16 px** (the grid cell size — `CP437TilesetGenerator.
GlyphSize = 16`), PPU 16, point filtering, transparent background for
entities, full-fill for terrain. No partial alpha — every pixel is
opaque or empty.

## 2. The Four Rules

1. **No outlines.** Shapes hold together by value steps (base / +1
   light / −1 dark) against the dark ground. If a sprite disappears
   into the ground, darken its lowest value — do not add an outline.
2. **Muted bodies.** Every body/structure color comes from the §3
   family ramps (moss, stone, bark, bone, rust-fur). New hues need a
   new family added HERE first, built at the same low-mid chroma.
3. **Saturation is life.** High-chroma pixels (§3 accents) are capped
   at **≤ 10% of a sprite's opaque pixels (≈ ≤ 8 px typical)** and
   are ONLY spent on living/arcane features: eyes, fungal caps,
   blooms, embers, enchantment. An accent pixel on dead matter is a
   style bug.
4. **Speckle, not shading.** Depth on any fill ≥ ~20 px comes from
   seeded 1px speckle in the family's ±1 values (§4 densities). No
   dithering patterns, no hue-shifted shadows, no gradients.

## 3. Palette

### Terrain fills (full 16×16 tiles)

| Swatch | Hex | Use |
|---|---|---|
| ground base | `#2C3226` | open mossy ground |
| ground ±    | `#343C2C` `#262C21` `#3A422E` `#21261D` | ground speckle set |
| floor base  | `#383C34` | worked interior floor |
| floor ±     | `#40443A` `#30342D` `#464A3E` | floor speckle set |
| wall top    | `#565E4E` | wall cap band (rows 0–4) |
| wall front  | `#393F34` / `#31372E`, seam `#282D26` | wall face checker |
| water base  | `#1A4242` | pool fill |
| water bands | `#204E4C` `#16383A`, sparkle `#3C6E68` | 3px-gap stripes |
| oil (Pass 14) | base `#201A12`, bands `#2E2418`/`#16120C`, sheen `#6B5A2E`, glint `#C89A50` | OilSeep — glints are the only warm pixels |
| acid (Pass 14) | base `#2E4A1C`, bands `#3E661F`/`#243A16`, glint `#86C22E` | AcidPond — the glint is arcane, hence bright |

### Body family ramps (dark → base → light)

| Family | Dark | Base | Light | Extra |
|---|---|---|---|---|
| Moss cloth | `#4A5E48` | `#60765C` | — | worn by people |
| Leaf       | `#34482E` | `#46603A` | `#5D7C44` | hi `#8CA054` |
| Stone      | `#3E4638` | `#6A7260` | `#848C76` | mid `#545C4C` |
| Bark       | `#463828` | `#5C4A34` | — | roots/handles |
| Bone       | `#1E201C` (dark) | `#AA9E7E` | `#C4B896` | skin, teeth, stems |
| Rust fur   | `#583A22` | `#A06A3A` | — | mid `#7A4E2C` |
| Boot/leather | — | `#463E30` | — | |
| Frost (Pass 14) | `#3A4A58` | `#7A93A8` | `#AECBD8` | pale `#E2F2F8` — undead cold, ice |
| Sandstone (Pass 14) | `#605032` | `#8A7448` | `#A89060` | mid `#786440` — desert masonry |
| Steel (Pass 14) | — | `#A8B0AC` | `#D8DCD8` | ground weapons; near-gray ON PURPOSE so the glyph-color tint supplies identity |

### Accents (the saturated ≤10%)

| Swatch | Hex | Meaning |
|---|---|---|
| Amber      | `#E8B45C` | machine heat, treasure glint |
| Eye-amber  | `#FFD06E` | creature eyes (pair with a dark socket px) |
| Magenta    | `#A4589E` / `#C478BA` | fungal life |
| Pale bloom | `#E6BEE0` | fungal highlight |
| Teal crystal | `#78B4AA` / hi `#BEEBDE` | arcane crystal |

## 4. Speckle densities (fraction of the material's pixels repainted)

Seeded RNG (stable per sprite) — never hand-place "random" noise.

| Material | Density | Mix |
|---|---|---|
| Open ground tile | ~18% (46 px / 256) | 4-color ± set |
| Interior floor   | ~12% | ± set + faint seam rows |
| Tree canopy      | ~45% | ~50% dark/light ±1, ~7% highlight |
| Weathered stone  | ~55% | ±1 values + 10–12% moss-green intrusion |
| Fur / hide       | ~17% | mid + dark only |
| Cloth            | ~8%  | dark only |
| Fungal caps      | ~5%  | pale bloom only |

Moss intrusion on stone is the signature weathering move: 6–10% of a
stone surface's pixels replaced with `#4A5E48`/`#5D7C44`.

## 5. Pre-ship checklist (every new sprite)

- [ ] 16×16, opaque-or-empty pixels only, no partial alpha
- [ ] Zero outline pixels
- [ ] All body colors from §3 families (or a new family added there)
- [ ] Accent pixels ≤ 10% of opaque pixels, only on living/arcane
- [ ] Fills ≥ 20 px carry §4 speckle at the material's density
- [ ] Reads at 1× on `#2C3226` ground (test against a ground tile,
      not a white canvas)
- [ ] Entities that sit on bg-tilemap feedback (crops on wet soil)
      keep transparent margins so the bg block stays visible

## 6. Renderer integration contract

- Environment sprites are **tinted by the cell's foreground color**
  (`EnvironmentSpriteRenderer.PostRender` copies the main tilemap
  color) — terrain/fixture art must tolerate its glyph-color tint.
- **Actor sprites are authored-color**: the Pass 13 actor tier sets
  `authoredColor`, and the renderer applies ONLY the lighting value
  (max channel of the copied color as a gray tint), not the glyph
  hue — a `&Y` player does not render yellow.
- Blueprint-keyed resolution (Pass 12/13) beats glyph-keyed; exact
  names for fixtures, prefix+exclusion for actor families
  (`Snapjaw*` minus `*Corpse`). Contracts pinned in
  `EnvironmentSpriteRendererBlueprintTests`.

## 7. Production pipeline

- Author in piskel (MCP projects) or the PIL framework
  (`mo_sprites.py` pattern: string pixel-maps + palette dict +
  seeded speckle). Either way the §5 checklist gates shipping.
- Files: `Assets/Sprites/Environment/<name>.png` + hand-written
  `.meta` (textureType 8, spriteMode 1, PPU 16, filterMode 0 — a
  default Unity import is PPU 100 bilinear and WRONG).
- Exploration art stays in `Docs/StyleExploration/` (outside
  `Assets/` — Unity must not import it).

## 8. Reference set (shipped exemplars)

`player.png` (hooded wanderer), `snapjaw.png` (rust jackal, amber
eye), `tree.png` (mossy canopy), `pillar.png` (weathered column,
moss intrusion), `mushroom.png` (glowcap cluster) — all five follow
every rule above and are the tie-breakers when this doc is ambiguous.
