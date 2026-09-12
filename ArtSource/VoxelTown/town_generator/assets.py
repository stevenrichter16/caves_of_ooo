"""Reusable chunky voxel recipes, not geometry stolen from a demonstration.

Each recipe is authored in integer voxel units. ``build_recipes`` is pure
Python and returns the same catalogue at every seed. Layout chooses variants;
the asset library never consumes either global or gameplay randomness.

Base origins sit at z=0. X is width, Y depth, local -Y is the front. Templates
are made in the supplied collection; leave it UNLINKED from the scene and
create visible objects sharing template.data. There is no hidden-render flag
on the mesh or prototype that can accidentally hide those instances.
"""
from .mesh import VoxelMesh


REQUIRED_ASSETS = (
    "wall", "floor", "door", "gate", "stairs", "window", "awning", "fence",
    "bridge", "well", "crate", "barrel", "bed", "table", "stool", "shelf",
    "workbench", "furnace", "lantern", "torch", "market_stall", "sign", "banner",
    "irrigation", "crop", "reeds", "scrub", "rock", "palm", "tree", "ruin",
    "npc", "livestock", "trough", "pot", "hay", "firepit",
)


class _Recipe:
    def __init__(self, voxel_size):
        self.mesh = VoxelMesh(voxel_size)

    def b(self, x0, y0, z0, x1, y1, z1, material):
        """Solid cuboid in integer voxel coordinates; later material wins."""
        s = self.mesh.voxel_size
        self.mesh.bounds((x0*s, y0*s, z0*s), (x1*s, y1*s, z1*s), material)
        return self

    def frame_legs(self, width, depth, height, material="wood"):
        for x in (-width//2, width//2-1):
            for y in (-depth//2, depth//2-1):
                self.b(x, y, 0, x+1, y+1, height, material)
        return self


def _architecture(key, s):
    a = _Recipe(s)
    b = a.b
    if key == "floor":
        b(-2, -2, 0, 2, 2, 1, "dark_floor")
    elif key == "wall":
        b(-2, -1, 0, 2, 1, 10, "sandstone")
        for z in range(0, 10, 2):
            x = -2 if z % 4 == 0 else 0
            b(x, -1, z, x+2, 0, z+2, "stone_light")
        b(-2, -1, 9, 2, 1, 10, "stone_light")
    elif key in ("door", "gate"):
        width = 4 if key == "door" else 8
        for x in range(-width//2, width//2):
            b(x, -1, 0, x+1, 0, 8, "wood_light" if x % 3 == 0 else "wood")
        for z in (1, 5):
            b(-width//2, -1, z, width//2, 0, z+1, "metal")
        b(width//2-1, -1, 3, width//2, 0, 4, "rust")
    elif key == "stairs":
        for step in range(4):
            b(-4, -4+step*2, 0, 4, -2+step*2, step+1,
              "sandstone" if step % 2 else "stone_light")
    elif key == "window":
        b(-3, -1, 0, 3, 0, 1, "stone_light")
        b(-3, -1, 4, 3, 0, 5, "stone_light")
        for x in (-3, 2):
            b(x, -1, 0, x+1, 0, 5, "sandstone")
        b(0, -1, 1, 1, 0, 4, "wood")
    elif key == "awning":
        for x in (-6, 5):
            b(x, -4, 0, x+1, -3, 8, "wood")
        for y in range(-4, 4):
            z = 7 + (y+4)//3
            b(-6, y, z, 6, y+1, z+1, "cloth_cream")
        b(-6, 3, 0, -5, 4, 10, "wood")
        b(5, 3, 0, 6, 4, 10, "wood")
    elif key == "fence":
        for x in (-2, 1):
            b(x, -1, 0, x+1, 0, 5, "wood")
        for z in (1, 3):
            b(-2, -1, z, 2, 0, z+1, "wood_light")
    elif key == "bridge":
        for y in range(-6, 6):
            b(-4, y, 0, 4, y+1, 1, "wood" if y % 3 else "wood_light")
        for x in (-4, 3):
            for y in (-6, 0, 5):
                b(x, y, 0, x+1, y+1, 5, "wood")
            b(x, -6, 4, x+1, 6, 5, "wood_light")
    elif key == "well":
        # Readable open ring, not a filled cylinder or cube.
        for x in range(-6, 6):
            for y in range(-6, 6):
                r = (x+.5)**2 + (y+.5)**2
                if 16 <= r < 35:
                    b(x, y, 0, x+1, y+1, 3, "sandstone")
                    b(x, y, 3, x+1, y+1, 4,
                      "stone_light" if (x+y) % 4 else "ancient_stone")
                elif r < 16:
                    b(x, y, 0, x+1, y+1, 1, "water")
        for x in (-6, 5):
            b(x, -1, 0, x+1, 1, 12, "wood")
        b(-6, -1, 11, 6, 1, 12, "wood_light")
        b(0, 0, 4, 1, 1, 11, "metal")
    elif key == "ruin":
        b(-5, -2, 0, 5, 2, 1, "ancient_stone")
        for x, height in ((-4, 12), (-2, 8), (0, 5), (2, 10)):
            b(x, -1, 1, x+2, 1, height, "ancient_stone")
            if x in (-4, 2):
                b(x, -1, height-1, x+2, 1, height,
                  "sandstone" if x == 2 else "stone_light")
        b(-5, 1, 0, -3, 3, 2, "leaf")
        b(2, -3, 0, 4, -1, 1, "ancient_stone")
    return a.mesh


def _furniture(key, s):
    a = _Recipe(s)
    b = a.b
    if key == "crate":
        b(-2, -2, 0, 2, 2, 4, "wood")
        for x in (-2, 1):
            b(x, -2, 0, x+1, 2, 4, "wood_light")
        for z in (0, 3):
            b(-2, -2, z, 2, 2, z+1, "wood_light")
        b(-1, -1, 3, 1, 1, 4, "wood")
    elif key == "barrel":
        for z in range(5):
            for x in range(-2, 2):
                for y in range(-2, 2):
                    if abs(x+.5)+abs(y+.5) > (2 if z in (0, 4) else 3):
                        continue
                    mat = "metal" if z in (1, 3) else "wood_light" if x == -2 else "wood"
                    b(x, y, z, x+1, y+1, z+1, mat)
    elif key == "bed":
        a.frame_legs(4, 8, 2)
        b(-2, -4, 1, 2, 4, 2, "wood")
        b(-2, -3, 2, 2, 3, 3, "cloth_red")
        b(-2, 2, 2, 2, 4, 4, "cloth_cream")
        b(-2, 3, 0, 2, 4, 5, "wood_light")
    elif key in ("table", "workbench"):
        width = 6 if key == "table" else 8
        a.frame_legs(width, 4, 3)
        b(-width//2, -2, 3, width//2, 2, 4, "wood_light")
        if key == "workbench":
            b(-4, 1, 4, 4, 2, 5, "wood")
            b(2, -2, 4, 4, 0, 5, "metal")
            b(2, -2, 5, 3, 0, 6, "metal")
            b(-3, 0, 4, 0, 1, 5, "rust")
            b(-1, -1, 4, 0, 1, 5, "wood")
    elif key == "stool":
        for x, y in ((-1, -1), (1, -1), (0, 1)):
            b(x, y, 0, x+1, y+1, 2, "wood")
        b(-1, -1, 2, 2, 2, 3, "wood_light")
    elif key == "shelf":
        for x in (-3, 2):
            b(x, 0, 0, x+1, 2, 8, "wood")
        for z in (0, 3, 6):
            b(-3, 0, z, 3, 2, z+1, "wood_light")
            for x in (-2, 0, 1):
                b(x, 0, z+1, x+1, 1, z+3,
                  "cloth_red" if x == -2 else "sandstone" if x == 0 else "metal")
    elif key == "furnace":
        b(-3, -3, 0, 3, 3, 1, "ancient_stone")
        b(-3, -3, 1, -1, 3, 6, "sandstone")
        b(1, -3, 1, 3, 3, 6, "sandstone")
        b(-1, 0, 1, 1, 3, 6, "sandstone")
        b(-1, -3, 4, 1, 0, 6, "sandstone")
        b(-1, -2, 1, 1, 0, 2, "coal")
        b(-1, -1, 2, 1, 0, 3, "ember")
        b(-2, 0, 6, 1, 3, 12, "earth")
        b(-2, 0, 12, 1, 3, 13, "rust")
        b(-3, -3, 5, 3, 3, 6, "ancient_stone")
    elif key == "firepit":
        for x in range(-3, 3):
            for y in range(-3, 3):
                radius = (x+.5)**2+(y+.5)**2
                if 4 <= radius < 10:
                    b(x, y, 0, x+1, y+1, 2, "ancient_stone")
                elif radius < 4:
                    b(x, y, 0, x+1, y+1, 1, "coal")
        b(-2, -1, 1, 2, 0, 2, "wood")
        b(-1, -2, 1, 0, 2, 2, "wood")
        b(0, 0, 1, 1, 1, 3, "ember")
        b(-1, -1, 2, 0, 0, 3, "ember")
    elif key == "lantern":
        b(-1, -1, 0, 1, 1, 1, "metal")
        b(-1, -1, 1, 1, 1, 3, "ember")
        b(-1, -1, 3, 1, 1, 4, "metal")
        for x, y in ((-1, -1), (0, 0)):
            b(x, y, 1, x+1, y+1, 3, "metal")
    elif key == "torch":
        b(-1, -1, 0, 1, 1, 1, "ancient_stone")
        b(0, 0, 1, 1, 1, 6, "wood")
        b(-1, -1, 5, 1, 1, 6, "metal")
        b(-1, -1, 6, 1, 1, 7, "ember")
        b(0, 0, 7, 1, 1, 8, "ember")
    elif key == "market_stall":
        for x in (-6, 5):
            for y in (-4, 3):
                b(x, y, 0, x+1, y+1, 10, "wood")
        b(-6, -4, 3, 6, -1, 4, "wood_light")
        for x in range(-6, 6):
            for y in range(-4, 4):
                z = 8 if y < -1 else 9
                b(x, y, z, x+1, y+1, z+1,
                  "cloth_red" if (x+6)//3 % 2 else "cloth_cream")
        for x in (-4, 0, 3):
            b(x, -3, 4, x+2, -1, 5, "wood")
            b(x, -3, 5, x+2, -2, 6, "dry_leaf" if x == 0 else "leaf_light")
    elif key in ("sign", "banner"):
        b(0, 0, 0, 1, 1, 8, "wood")
        if key == "sign":
            b(-2, -1, 5, 3, 0, 8, "wood_light")
            b(-1, -1, 6, 2, 0, 7, "coal")
        else:
            b(-3, 0, 7, 4, 1, 8, "wood_light")
            b(-2, -1, 2, 3, 0, 7, "cloth_red")
            b(-1, -1, 1, 2, 0, 2, "cloth_red")
            b(0, -1, 3, 1, 0, 6, "cloth_cream")
    elif key == "irrigation":
        b(-4, -2, 0, 4, 2, 1, "earth")
        b(-4, -1, 0, 4, 1, 1, "water")
        for y in (-2, 1):
            b(-4, y, 1, 4, y+1, 2, "sandstone")
        b(-3, -2, 2, -2, 2, 3, "wood")
    elif key == "trough":
        b(-4, -2, 0, 4, 2, 1, "wood")
        for y in (-2, 1):
            b(-4, y, 1, 4, y+1, 2, "wood_light")
        for x in (-4, 3):
            b(x, -1, 1, x+1, 1, 2, "wood_light")
        b(-3, -1, 0, 3, 1, 1, "water")
    elif key == "pot":
        b(-1, -1, 0, 1, 1, 1, "rust")
        for x in range(-2, 2):
            for y in range(-2, 2):
                if abs(x+.5)+abs(y+.5) <= 3:
                    if abs(x+.5) > .5 or abs(y+.5) > .5:
                        b(x, y, 1, x+1, y+1, 3, "rust")
        for x, y in ((-1, -1), (-1, 1), (1, -1), (1, 1)):
            b(x, y, 3, x+1, y+1, 4, "sandstone")
    elif key == "hay":
        b(-2, -2, 0, 2, 2, 2, "dry_leaf")
        b(-1, -2, 2, 1, 2, 3, "dry_leaf")
        b(-2, 0, 0, 2, 1, 2, "wood_light")
        b(-1, 0, 2, 1, 1, 3, "wood_light")
    return a.mesh


def _nature(key, variant, s):
    a = _Recipe(s)
    b = a.b
    if key == "crop":
        if variant == 2:
            # Low leafy produce distinct from the two grain stages.
            b(-2, -2, 0, 2, 2, 1, "earth")
            for x, y in ((-1, -1), (1, 0), (-1, 1)):
                b(x-1, y-1, 1, x+1, y+1, 2, "leaf")
                b(x, y, 2, x+1, y+1, 3, "leaf_light")
        else:
            for i, (x, y) in enumerate(((-2, -1), (0, -2), (1, 1), (-1, 1))):
                h = 3 + (i+variant) % 3
                b(x, y, 0, x+1, y+1, h, "leaf" if variant == 0 else "dry_leaf")
                b(x, y, h-1, x+1, y+1, h+1, "dry_leaf")
                if i % 2:
                    b(x-1, y, 1, x, y+1, 2, "leaf_light")
    elif key == "reeds":
        layouts = (((-2, -1, 7), (0, 0, 9), (1, -2, 6), (-1, 2, 8)),
                   ((-2, 1, 5), (0, -2, 8), (2, 0, 10), (1, 2, 6), (-1, 0, 7)),
                   ((-2, -2, 6), (-1, 1, 5), (1, -1, 8), (2, 1, 7)))
        for i, (x, y, height) in enumerate(layouts[variant]):
            b(x, y, 0, x+1, y+1, height, "leaf")
            b(x, y, height-2, x+1, y+1, height, "dry_leaf")
            side = 1 if i % 2 else -1
            b(x+side, y, 1, x+side+1, y+1, height-3, "leaf_light")
    elif key == "scrub":
        arrangements = (((-2, -1, 3), (1, 0, 4), (-1, 2, 2)),
                        ((-2, 1, 4), (1, -1, 3), (2, 2, 2)),
                        ((-2, -2, 2), (1, 0, 3), (-1, 2, 2)))
        for i, (x, y, height) in enumerate(arrangements[variant]):
            leaf = "dry_leaf" if variant == 2 else "leaf"
            tip = "wood_light" if variant == 2 else "leaf_light"
            b(x, y, 0, x+1, y+1, height, leaf)
            b(x-1, y, 1, x+1, y+1, min(height, 3), leaf)
            b(x, y-1, 1, x+1, y+1, min(height, 3), leaf)
            b(x, y, height-1, x+1, y+1, height, tip)
    elif key == "rock":
        profiles = (((-3, -2, 0, 3, 2, 1), (-2, -2, 1, 2, 2, 3), (-1, -1, 3, 1, 1, 4)),
                    ((-2, -2, 0, 2, 3, 1), (-2, -1, 1, 1, 2, 4), (-1, 0, 4, 1, 1, 5)),
                    ((-3, -2, 0, 2, 2, 1), (-2, -1, 1, 1, 2, 2), (1, 0, 0, 3, 2, 2)))
        for i, bounds in enumerate(profiles[variant]):
            b(*bounds, "ancient_stone" if i != 1 else "sandstone")
    elif key == "palm":
        trunk_height = (14, 16, 12)[variant]
        for z in range(0, trunk_height, 2):
            lean = 1 if variant == 1 and z >= 8 else -1 if variant == 2 and z >= 6 else 0
            b(-1+lean, -1, z, 1+lean, 1, min(z+2, trunk_height),
              "wood" if z % 4 else "wood_light")
        crownx = 1 if variant == 1 else -1 if variant == 2 else 0
        b(crownx-2, -2, trunk_height-1, crownx+2, 2, trunk_height+2, "leaf")
        directions = ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1))
        for i, (dx, dy) in enumerate(directions):
            length = 4 + (i+variant) % 3
            for step in range(1, length):
                x, y = crownx+dx*step, dy*step
                z = trunk_height+1 - max(0, step-2)//2
                b(x-1, y-1, z, x+1, y+1, z+1,
                  "leaf_light" if i % 3 == 0 else "leaf")
        b(crownx-1, -1, trunk_height-2, crownx+1, 1, trunk_height-1, "dry_leaf")
    elif key == "tree":
        h = (9, 10, 8)[variant]
        b(-1, -1, 0, 1, 1, h+2, "wood")
        b(-2, -1, 0, 2, 1, 1, "wood")
        for i, (cx, cy, dz, width) in enumerate(((-2, 0, 0, 6), (2, 1, 1, 6), (0, -2, 2, 5))):
            cx += 1 if variant == 1 and i == 0 else -1 if variant == 2 and i == 2 else 0
            top = h + dz
            b(cx-width//2, cy-2, top-2, cx+width//2, cy+2, top+1, "leaf")
            b(cx-width//2+1, cy-1, top+1, cx+width//2-1, cy+1, top+2, "leaf_light")
            b(cx-1, cy-3, top-1, cx+1, cy+3, top+1, "leaf")
    return a.mesh


def _character(key, variant, s):
    a = _Recipe(s)
    b = a.b
    if key == "npc":
        cloth = ("cloth_teal", "cloth_red", "cloth_cream")[variant]
        skin = "skin_dark" if variant == 1 else "skin"
        for x in (-1, 1):
            b(x, -1, 0, x+1, 1, 1, "wood")
            b(x, 0, 1, x+1, 1, 3, "earth")
        b(-1, -1, 3, 2, 1, 5, cloth)
        b(-1, -1, 3, 2, 1, 4, "wood")
        b(-1, -1, 5, 2, 1, 7, skin)
        b(-1, 0, 5, 2, 1, 7, "wood")
        for x in (-2, 2):
            b(x, -1, 3, x+1, 1, 5, cloth)
            b(x, -1, 2, x+1, 0, 3, skin)
        if variant == 0:
            b(-1, -1, 7, 2, 1, 8, "cloth_teal")
        elif variant == 1:
            b(-2, -2, 7, 3, 2, 8, "dry_leaf")
            b(-1, -1, 8, 2, 1, 9, "dry_leaf")
            b(2, -1, 1, 3, 0, 2, "wood")
        else:
            b(-1, 0, 7, 2, 1, 8, "wood")
            b(-2, 1, 3, 2, 2, 5, "wood_light")
            b(2, -1, 0, 3, 0, 5, "wood")
    elif key == "livestock":
        coat = ("sandstone", "cloth_cream", "earth")[variant]
        length = (7, 6, 8)[variant]
        for x in (-3, length-5):
            for y in (-2, 1):
                b(x, y, 0, x+1, y+1, 3, "wood")
        b(-3, -2, 2, length-3, 2, 5, coat)
        b(-4, -1, 3, -3, 0, 5, "wood")
        b(length-4, -1, 3, length-2, 1, 7, coat)
        b(length-3, -2, 5, length-1, 1, 7, coat)
        b(length-1, -1, 5, length, 1, 6, "wood")
        if variant == 0:
            b(-2, -1, 5, 0, 1, 6, coat)
            b(1, -1, 5, 3, 1, 6, coat)
        elif variant == 1:
            b(length-3, -2, 7, length-2, -1, 8, "wood")
            b(length-3, 0, 7, length-2, 1, 8, "wood")
        else:
            b(-1, -2, 5, 2, 2, 6, "cloth_red")
            b(-1, -3, 3, 2, -2, 5, "wood_light")
            b(-1, 2, 3, 2, 3, 5, "wood_light")
    return a.mesh


def build_recipes(voxel_size=.25):
    """Return all category meshes and three variants for repeatable families."""
    recipes = {}
    architecture = {"wall", "floor", "door", "gate", "stairs", "window", "awning",
                    "fence", "bridge", "well", "ruin"}
    nature = {"crop", "reeds", "scrub", "rock", "palm", "tree"}
    for key in REQUIRED_ASSETS:
        if key in architecture:
            recipes[key] = _architecture(key, voxel_size)
        elif key in nature:
            for variant in range(3):
                suffix = "" if variant == 0 else f"_{variant}"
                recipes[key+suffix] = _nature(key, variant, voxel_size)
        elif key in ("npc", "livestock"):
            for variant in range(3):
                suffix = "" if variant == 0 else f"_{variant}"
                recipes[key+suffix] = _character(key, variant, voxel_size)
        else:
            recipes[key] = _furniture(key, voxel_size)
    return recipes


def build_assets(collection, materials, voxel_size=.25, owner_token="voxel-town"):
    """Build one prototype mesh per recipe, never one Object per voxel.

    Keep the supplied collection unlinked to hide templates. They carry the
    catalogue key, common voxel scale, cell count, and owner on each object.
    Data-level ownership enables safe regeneration in an existing .blend.
    """
    result = {}
    for key, recipe in build_recipes(voxel_size).items():
        obj = recipe.build(f"VX_{owner_token}_asset_{key}", collection, materials, owner_token)
        obj["coo_voxel_asset"] = key
        obj["coo_voxel_prototype"] = True
        result[key] = obj
    return result
