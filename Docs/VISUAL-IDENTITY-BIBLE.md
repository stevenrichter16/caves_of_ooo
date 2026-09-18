# Caves of Ooo Visual Identity Bible

Status: binding for the animated 2D presentation pass.

## 1. Canon and compatibility

- The root `Lore/` corpus is authoritative. `Lore/10_Bible.md` and
  `Lore/11_SecondSpine.md` resolve conflicts with older material.
- `Docs/Lore/` and `Docs/Lore/v2/` are reference material from the
  superseded Palimpsest-world redesign. They are not sources for new art.
- Legacy runtime identifiers are compatibility data, not display canon.
  In particular, `Palimpsest`, `PalimpsestEcho`, and the existing file names
  remain valid internal IDs until a separately versioned content migration.
  Their canonical public art family is **the Recension**.
- Visual presentation is rebuilt from stable entity data after load. Unity
  renderers, current animation frames, tweens, particles, and flashes are
  never save data.

## 2. Format

- Logical terrain remains 16x16 pixels per cell at 16 pixels per unit.
- Moving actors use a 16x24 canvas, bottom-center pivot, and may overhang the
  cell above. A creature still occupies exactly one logical cell unless its
  gameplay blueprint says otherwise.
- Terrain has no outline. Discrete objects and actors use a one-pixel dark
  ink silhouette. This supersedes the older blanket no-outline rule in
  `Docs/STYLE-GUIDE.md` and formalizes the Pass 15 object/terrain split.
- Pixel alpha is binary. Filtering is Point, compression is disabled, mipmaps
  are disabled, and camera/viewport boundaries must land on whole pixels.
- Terrain owns the middle of the value range. Actors, hazards, usable objects,
  and projectiles own the lightest and darkest values.

## 3. Material language

| Family | Shape language | Materials and color cues |
|---|---|---|
| Surface villages / Sill | repaired, practical, slightly irregular | river stone, timber, thatch, washed cloth, reed and river-blue accents |
| Recension | layered, ruled, annotated | ink, vellum, marginalia, Memory-Marble, cool paper light |
| Pale Curation | sealed, indexed, rectilinear | salt white, labels, gloves, glass, restrained cold blue |
| Saccharine Concord | modular, portable, balanced | scales, weights, folded stalls, brass, berry and honey accents |
| Bower-Folk | staged, radial, composed | pigment, resin, Bower-Starstone, deliberate high-chroma accents |
| Rot Choir | grown, joined, enclosing | mycelium, wet fiber, Choir-Iron, warm organic shadow |
| Catacomb villages | excavated, communal, luminous | plaque walls, mycelial cloth, Glow-Quartz, cultivated bio-light |
| Tent-Right | woven, openable, shared | tents, wells, guest-cloth, salt-pale weave, repeated threshold motifs |
| Driving Bloom | interrupted, eruptive, unfinished | abandoned tools and poses, fruiting bodies breaking incomplete work |
| Urqu / Unsaying | absent rather than aggressive | erased edges, missing frames, uncertain silhouettes, no generic purple corruption |
| Root / tepui | fossil anatomy at landscape scale | pink-grey Tepuibone, growth rings, petrified fibers, bromeliads and spray |

## 4. Animation tiers

- Tier A: player, companions, bosses, and story-critical figures. Four-facing
  idle, walk, attack/cast, hurt, and authored death.
- Tier B: common combatants and important villagers. Four-facing idle, walk,
  and attack, with shared procedural hurt/death treatment.
- Tier C: minor wildlife and background figures. Directional idle plus
  transform-based movement, lunge, flash, and death.
- Motion communicates resolved turns; it never delays or changes simulation.
  Default movement is 0.10 seconds, attacks 0.16 seconds, and hurt reactions
  0.14 seconds. Accessibility controls may shorten or disable them.

## 5. Place contract

Every authored place must have all four of the following before it is called
visually complete:

1. One silhouette visible at thumbnail scale.
2. One material family that identifies its people before dialogue.
3. One restrained ambient motion family.
4. One ordinary-life detail that prevents the setting becoming continuous
   cosmic spectacle.

## 6. Readability contract

- Player, hostile, neutral, interactable, hazard, and terrain must remain
  distinguishable in a four-times-downscaled screenshot.
- FOV remains authoritative. Dynamic sprites and effects may never reveal an
  entity in unexplored or remembered-but-not-visible cells.
- Important state cannot be communicated by hue alone.
- Tall sprites sort from their ground contact point, not their image center.
- Missing or invalid visual data falls back to the existing static sprite and
  then to the canonical CP437 glyph. Missing art may not produce an empty cell.

## 7. Shipped reference slice

The first production slice establishes the implementation standard for later
art rather than creating a parallel showcase scene:

- Data-driven, four-facing 16x24 sheets for the player, Sill villagers,
  merchants, elder, warden, child, snapjaws, the Sari-Snake, Wardline,
  Cascade Father, Glasspane Frog, Yellowfoot Wayfarer, and the legacy
  `PalimpsestEcho` ID presented as Recension art.
- Idle, walk, attack, hurt, and shared death presentation driven by resolved
  simulation events. The current frame and tween are runtime-only.
- Continuous 8x8-cell grass and Tepuibone fields, plus deterministic tree
  variants. Legacy terrain tiles remain the fallback.
- Exact fog, top-object, lighting, render-layer, sprite-toggle, and save/load
  compatibility with the existing hybrid CP437 renderer.

`ArtTools/coo_vertical_slice.py` regenerates the reference assets. The output
is deterministic and Unity's sprite import postprocessor enforces point
filtering, 16 pixels per unit, no mipmaps, and no compression.

## 8. Rollout order

1. Core loop: remaining common hostiles, carried/equipped item silhouettes,
   doors, containers, stairs, and combat impact frames.
2. Place identity: one complete terrain/fixture/actor kit per authored region,
   beginning with Sill, the tepui approaches, and the catacomb settlements.
3. Narrative figures: companions, faction representatives, bosses, and their
   authored actions and deaths.
4. Environmental motion: water edges, spray, foliage, smoke, spores, cloth,
   and restrained settlement activity.
5. Interface cohesion: inventory paper-dolls, item portraits, dialogue
   portraits, and map symbols derived from the same actor/material palettes.

Each rollout unit must pass the place and readability contracts above before
the next region begins. A larger asset count is not a substitute for complete
visual identity in the places players actually visit.
