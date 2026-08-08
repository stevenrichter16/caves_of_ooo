# Graphics Pass 14 — Fifteen-Sprite Expansion

> Origin: user directive 2026-08-04 — "make 15 more sprites for npcs
> or objects or terrain that already exist in game then put the
> sprites in the game." All art per `Docs/STYLE-GUIDE.md`; selection
> driven by a placement survey (what generation/settlement code
> actually spawns) crossed against the claimed-glyph set.

**Status:** ✅ shipped 2026-08-04.

## 1. The fifteen

**Actors (7, authored-color):**
| Sprite | Claims | Notes |
|---|---|---|
| villager.png | Villager, Innkeeper, WellKeeper, Scribe | robed townsfolk share one sprite |
| merchant.png | Merchant | bark robes, satchel, amber coin |
| elder.png | Elder | staff w/ amber tip, bone beard |
| warden.png | Warden | stone helm, spear |
| village_child.png | VillageChild | small silhouette |
| spore_shambler.png | SporeShambler | fungal mass, magenta caps |
| ice_wight.png | IceWight | NEW Frost family, tattered hem |

**Wrong-visual fixes (4)** — glyph collisions the blueprint tier now
resolves:
| Sprite | Was rendering as |
|---|---|
| oil_seep.png | WATER ('~') |
| acid_pond.png | WATER ('~') |
| rubble.png | BONES (',') |
| oven.png | bare WALL ('#') |

**Identity pieces (4):**
| Sprite | Notes |
|---|---|
| ice_stalactite.png | 14 placement sites; was the generic stone stalactite |
| vine_wall.png / sandstone_wall.png | themed walls — trade the 4-variant auto-tile for identity; Wall/StoneWall stay on the generic atlas (test-pinned) |
| weapon_ground.png | glyph-keyed '/' — Dagger/ShortSword/LongSword/Spear/Cudgel drops; near-gray Steel art so the COLOR-COPY tint keeps each weapon's glyph color as its identity |

## 2. Wiring

- `ActorSpriteKind` + `ResolveActorKind`: villager set (exact names),
  Merchant/Elder/Warden/VillageChild/SporeShambler/IceWight exact;
  Snapjaw prefix rule unchanged.
- `EnvFixtureKind` + `ResolveFixtureKind`: OilSeep, AcidPond, Rubble,
  Oven, IceStalactite, VineWall, SandstoneWall (exact names).
- Glyph switch: `'/'` → weapon_ground (the one glyph-keyed addition —
  deliberately color-copy, NOT authored-color).
- STYLE-GUIDE.md gained the Frost / Sandstone / Steel body families
  and the Oil / Acid liquid fills (per its own new-hue rule).

## 3. Contract change (documented supersession)

Pass 13's refusal pin `ResolveActorKind("Villager") == None` was
SUPERSEDED by this pass (villagers now have a sprite). The test was
updated to pin still-unmapped blueprints (SandWurm, IceStalactite);
the RED run caught the collision exactly as intended.

## 4. Tests

7 new resolver tests RED-first (28× CS0117 confirmed): villager
family, distinct roles, monsters, marker/corpse refusals, the four
wrong-visual fixes, themed pieces, and the generic-walls-stay-generic
counter-pin.

## 5. Bounds / deliberate scope

- SandWurm stays CP437 (no sprite yet — desert-boss art deserves its
  own pass). Warden set is exact-name; future guard variants must be
  added explicitly.
- Snapjaw color-variant identity still collapsed (Pass 13 bound).
- Manual check (Play, `\`): village → villagers/merchant/elder/warden/
  child sprites walking; ruins → rubble no longer bones, pillar set;
  desert → sandstone walls; ice caves → wight + icy stalactites;
  drop a dagger → gray blade tinted the dagger's glyph color; oil
  seep and acid pond read as oil and acid, not water.
