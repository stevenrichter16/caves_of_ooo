#!/usr/bin/env python3
"""GRAPHICS PASS 15 round 2 (S1/S3/S4) — the ink-outline pass.

Farming law #3: objects get a 1px dark ink outline; terrain NEVER
does. This script:
  1. Adds the ink ring to every OBJECT/ACTOR sprite (idempotent —
     re-running never double-outlines because the ring only fills
     TRANSPARENT pixels adjacent to opaque ones).
  2. The PLAYER gets the strongest ring: 8-connected, near-black.
  3. Restyles the bush (the old one read as popcorn: light speckle
     on dark — exactly the per-tile noise law #2 bans).
  4. Generates the 9 role NPCs (5 shopkeepers + 4 hermits) as
     palette-kin of the villager base: the three robe greens remap
     to a role triad; skin/hair/ink stay.

Terrain (macro slices, water family, walls, atlases) is explicitly
NOT listed — outlining terrain is the one thing law #3 forbids.

Run:  python3 ArtTools/coo_outline_pass.py
"""

from PIL import Image
import os

ROOT = os.path.join(os.path.dirname(__file__), "..",
                    "Assets", "Resources", "Sprites", "Environment")

INK = (30, 32, 28, 255)          # matches the villager sprite's ink details
PLAYER_INK = (14, 16, 14, 255)   # S4: strongest ring on the player

# Objects + actors that get the ring. Pools (oil_seep, acid_pond) are
# deliberately excluded — they are ground features; an outline would
# promote them to "object standing on ground".
OUTLINE_4 = [
    "alchemy_still", "barrel", "bed", "bones", "boulder", "bush",
    "cactus", "campfire", "candycarrot_crop", "chair", "chest",
    "corpse", "crop_seed", "door_closed", "door_open", "elder",
    "emberwheat_crop", "forge", "gold_pile", "ice_stalactite",
    "ice_wight", "lantern", "market_stall", "merchant", "mushroom",
    "oven", "pillar", "rubble", "shrine", "snapjaw",
    "spore_shambler", "stairs_down", "stairs_up", "stalactite",
    "stalagmite", "tree", "village_child", "villager", "warden",
    "weapon_ground", "well",
]

# blueprint-named role NPCs -> (mid, dark, light) robe triads
ROLE_TRIADS = {
    "weaponsmith":   ((104, 104, 112), (72, 72, 80),   (132, 132, 140)),  # iron
    "armorer":       ((140, 96, 58),   (100, 66, 40),  (170, 124, 80)),   # bronze
    "apothecary":    ((70, 124, 116),  (46, 88, 82),   (96, 152, 142)),   # teal
    "arcanist":      ((110, 82, 140),  (76, 54, 100),  (140, 110, 170)),  # violet
    "provisioner":   ((160, 132, 76),  (116, 92, 52),  (188, 162, 104)),  # wheat
    "cave_hermit":   ((84, 96, 120),   (56, 66, 88),   (110, 124, 150)),  # slate
    "desert_hermit": ((168, 138, 92),  (124, 98, 62),  (196, 168, 120)),  # sand
    "jungle_hermit": ((58, 104, 66),   (38, 72, 46),   (80, 132, 88)),    # jungle
    "ruins_hermit":  ((150, 144, 132), (108, 102, 92), (180, 174, 162)),  # ash
}

# The villager base's robe triad (mid, dark, light) — measured.
VILLAGER_ROBE = ((74, 94, 72), (52, 72, 46), (96, 118, 92))


def outline(im, ink, diag=False):
    """Fill transparent pixels adjacent to opaque ones with ink.
    Idempotent: ink pixels are opaque, so re-running changes nothing."""
    w, h = im.size
    px = im.load()
    ring = []
    neigh = [(-1, 0), (1, 0), (0, -1), (0, 1)]
    if diag:
        neigh += [(-1, -1), (-1, 1), (1, -1), (1, 1)]
    for y in range(h):
        for x in range(w):
            if px[x, y][3] != 0:
                continue
            for dx, dy in neigh:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] > 0 \
                        and px[nx, ny][:3] != ink[:3]:
                    ring.append((x, y))
                    break
    for x, y in ring:
        px[x, y] = ink
    return len(ring)


def restyle_bush():
    """Law #2-compliant bush: flat dark mass, mid-green lobes as FLAT
    fills, 1px lip, no speckle. Ink ring added by the outline pass."""
    dark = (44, 66, 42, 255)
    mid = (64, 92, 58, 255)
    light = (84, 114, 74, 255)
    trunk = (70, 62, 48, 255)
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()
    # canopy: a fat rounded blob rows 3..12
    body = {
        3: range(6, 10), 4: range(4, 12), 5: range(3, 13), 6: range(2, 14),
        7: range(2, 14), 8: range(2, 14), 9: range(3, 13), 10: range(3, 13),
        11: range(4, 12), 12: range(5, 11),
    }
    for y, xs in body.items():
        for x in xs:
            px[x, y] = dark
    # two flat mid-tone lobes (variation by SHAPE, not noise)
    for y, xs in {4: range(5, 9), 5: range(4, 10), 6: range(4, 9),
                  7: range(5, 8)}.items():
        for x in xs:
            px[x, y] = mid
    for y, xs in {7: range(9, 13), 8: range(8, 13), 9: range(9, 12)}.items():
        for x in xs:
            px[x, y] = mid
    # single light glint on the upper-left lobe (flat, 3px)
    for x, y in [(5, 4), (6, 4), (5, 5)]:
        px[x, y] = light
    # trunk peek
    for x, y in [(7, 13), (8, 13)]:
        px[x, y] = trunk
    im.save(os.path.join(ROOT, "bush.png"))
    return im


def make_role_npc(name, triad):
    base = Image.open(os.path.join(ROOT, "villager.png")).convert("RGBA")
    px = base.load()
    mapping = {VILLAGER_ROBE[i]: triad[i] for i in range(3)}
    for y in range(base.size[1]):
        for x in range(base.size[0]):
            p = px[x, y]
            if p[3] > 0 and p[:3] in mapping:
                px[x, y] = mapping[p[:3]] + (255,)
    out = os.path.join(ROOT, name + ".png")
    base.save(out)
    return base


def main():
    total = 0
    # 3. bush restyle FIRST so the ring lands on the new body
    restyle_bush()
    print("bush restyled (flat lobes, no speckle)")

    # 4. role NPCs from the villager base (before outlining: the base
    #    villager.png is still un-ringed on first run; ring them all
    #    below by including the new files in the pass)
    npc_names = []
    for name, triad in ROLE_TRIADS.items():
        make_role_npc(name, triad)
        npc_names.append(name)
    print(f"{len(npc_names)} role NPCs generated: {', '.join(npc_names)}")

    # 1. ink ring on all object/actor sprites (+ freshly made NPCs)
    for name in OUTLINE_4 + npc_names:
        path = os.path.join(ROOT, name + ".png")
        if not os.path.exists(path):
            print(f"  SKIP (missing): {name}")
            continue
        im = Image.open(path).convert("RGBA")
        n = outline(im, INK, diag=False)
        im.save(path)
        total += n

    # 2. player gets the strongest ring (8-connected, near-black)
    ppath = os.path.join(ROOT, "player.png")
    pim = Image.open(ppath).convert("RGBA")
    n = outline(pim, PLAYER_INK, diag=True)
    pim.save(ppath)
    print(f"outline pass: {total} ring px on objects, {n} on player")


if __name__ == "__main__":
    main()
