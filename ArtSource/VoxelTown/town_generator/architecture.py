"""Semantic building shells as accessible, roofless voxel structures.

These rules are local to the building. Scene realization applies its world
position/quarter-turn rotation afterward; no demonstration-specific offsets,
camera-facing hand edits, or random global state enter the recipe.
"""
import math
from numbers import Real

from .mesh import VoxelMesh

ROLES = frozenset(("residence", "workshop", "trader", "meeting_house", "shrine",
                   "storehouse", "farmhouse", "bathhouse", "animal_enclosure", "guard_watch"))


def _number(value, label, minimum, maximum):
    if isinstance(value, bool) or not isinstance(value, Real) or not math.isfinite(value) \
            or not minimum <= value <= maximum:
        raise ValueError(f"{label} must be finite and between {minimum} and {maximum}")
    return float(value)


def _grid(value, q, label):
    units = value/q
    if not math.isclose(units, round(units), abs_tol=1e-7):
        raise ValueError(f"{label} must align with the voxel grid")
    return round(units)


def build_structure(building, voxel_size=.25):
    """Return a local-space ``VoxelMesh`` honoring semantic size and entrances.

    Width/depth must be even voxel multiples to center them on the common
    grid. Height must be a voxel multiple. Floor top is one voxel above the
    base; ordinary walls are .5m thick. Entrances provide at least 1.5m width
    and 2m clear height above the floor; pens remain completely open overhead.
    Entrances are local XY points on the outside perimeter, away from corners.

    The model is render geometry and an inspectable voxel occupancy recipe.
    It does not create Unity collision/destruction behavior by itself.
    """
    mesh = VoxelMesh(voxel_size)
    q = mesh.voxel_size
    role = building.role
    if role not in ROLES:
        raise ValueError(f"Unknown building role {role!r}")
    material = building.material_set
    if material not in ("sandstone", "earth"):
        raise ValueError("Architecture material_set must be sandstone or earth")
    width = _number(building.width, "width", 3, 128)
    depth = _number(building.depth, "depth", 3, 128)
    minimum_height = 1 if role == "animal_enclosure" else 2.5
    height = _number(building.wall_height, "wall_height", minimum_height, 16)
    hx = _grid(width/2, q, "half width")
    hy = _grid(depth/2, q, "half depth")
    hz = _grid(height, q, "wall height")
    thickness = max(1, math.ceil(.5/q))
    corner_clearance = max(thickness, round(.75/q)) if role in ("meeting_house", "shrine") else thickness
    opening_width = 2.5 if role == "trader" else 2. if role == "meeting_house" else 1.5
    half_opening = max(1, math.ceil(opening_width/(2*q)))
    clear_top = math.ceil(2/q)+1
    if role != "animal_enclosure" and clear_top >= hz:
        raise ValueError("Wall needs two metres of entrance clearance plus a lintel voxel")
    openings = {"front": [], "back": [], "left": [], "right": []}
    if not building.entrances:
        raise ValueError("An accessible building needs at least one entrance")
    for point in building.entrances:
        try:
            x, y = point
        except (TypeError, ValueError) as exc:
            raise ValueError("Entrances must contain local XY pairs") from exc
        x, y = _number(x, "entrance X", -64, 64), _number(y, "entrance Y", -64, 64)
        ix, iy = _grid(x, q, "entrance X"), _grid(y, q, "entrance Y")
        if iy in (-hy, hy) and abs(ix)+half_opening+corner_clearance <= hx:
            openings["front" if iy < 0 else "back"].append((ix-half_opening, ix+half_opening))
        elif ix in (-hx, hx) and abs(iy)+half_opening+corner_clearance <= hy:
            openings["left" if ix < 0 else "right"].append((iy-half_opening, iy+half_opening))
        else:
            raise ValueError("Entrance must lie on a wall with enough corner clearance")

    def box(x0, y0, z0, x1, y1, z1, mat):
        mesh.bounds((x0*q, y0*q, z0*q), (x1*q, y1*q, z1*q), mat)

    floor_material = "earth" if role == "animal_enclosure" else \
        "ancient_stone" if role == "bathhouse" else "dark_floor"
    box(-hx, -hy, 0, hx, hy, 1, floor_material)
    course = max(1, round(.5/q))
    brick = max(2, round((1.5 if role == "storehouse" else 1.25)/q))
    post_spacing = max(3, round(1/q))
    rail_levels = {max(1, round(.5/q)), max(1, round(1/q))}
    walls = (("front", -hx, hx), ("back", -hx, hx),
             ("left", -hy, hy), ("right", -hy, hy))
    for wall_index, (wall, low, high) in enumerate(walls):
        for along in range(low, high):
            at_corner = along < low+thickness or along >= high-thickness
            inside_opening = any(lo <= along < hi for lo, hi in openings[wall])
            for z in range(1, hz):
                if inside_opening and (z < clear_top or role == "animal_enclosure"):
                    continue
                if role == "animal_enclosure":
                    is_post = at_corner or (along-low) % post_spacing == 0
                    if not is_post and z not in rail_levels:
                        continue
                    mat = "wood" if is_post else "wood_light"
                else:
                    if role == "guard_watch" and z >= hz-course \
                            and (along-low)//brick % 2 == 1 and not at_corner:
                        continue
                    course_offset = brick//2 if z//course % 2 else 0
                    block_column = (along-low+course_offset)//brick
                    stone_tone = (block_column*3+z//course*5+wall_index*2) % 7
                    mat = "stone_light" if stone_tone in (4, 5) else "earth" if stone_tone == 6 else material
                    if z == hz-1:
                        mat = "stone_light" if (along-low)//brick % 2 == 0 else "sandstone"
                    if role == "workshop" and z <= course+1:
                        mat = "ancient_stone"
                    if role == "storehouse" and z in (course*2, hz-1):
                        mat = "wood"
                    if role == "farmhouse" and at_corner:
                        mat = "wood_light"
                    if role == "trader" and z == hz-1:
                        mat = "wood_light"
                    if role == "shrine" and z <= course:
                        mat = "ancient_stone"
                    if role == "bathhouse" and z <= course:
                        mat = "water_light"
                    if inside_opening and z == clear_top and role in ("trader", "meeting_house", "shrine"):
                        mat = "cloth_red" if role != "shrine" else "cloth_teal"
                # Narrow rectangular openings reveal real interior daylight.
                # They are rule-defined; no fake black window decals.
                window_role = role in ("residence", "farmhouse", "workshop")
                has_window = window_role and wall == ("back" if role == "farmhouse" else "left")
                if has_window and 0 <= along < max(1, round(1/q)) \
                        and round(1.5/q) <= z < round(2.25/q) and not at_corner:
                    continue
                wall_thickness = 1 if role == "animal_enclosure" else thickness
                # Recess only the outer skin, never the inner wall or room
                # footprint. Notches at staggered course ends reveal depth;
                # continuous deep bed joints made the first visual pass read
                # as wooden pallets, so long courses remain solid stone.
                recess = 0
                if role != "animal_enclosure" and thickness > 1 and not at_corner \
                        and not inside_opening and 1 < z < hz-1:
                    vertical_joint = (along-low+course_offset) % brick == 0 and z % course == 0
                    if vertical_joint:
                        recess = 1
                        mat = "earth" if material == "sandstone" else "dark_floor"
                if wall == "front":
                    box(along, -hy+recess, z, along+1, -hy+wall_thickness, z+1, mat)
                elif wall == "back":
                    box(along, hy-wall_thickness, z, along+1, hy-recess, z+1, mat)
                elif wall == "left":
                    box(-hx+recess, along, z, -hx+wall_thickness, along+1, z+1, mat)
                else:
                    box(hx-wall_thickness, along, z, hx-recess, along+1, z+1, mat)

    if role in ("meeting_house", "shrine"):
        # Squared corner piers read as civic/ritual construction. Piers point
        # inward so their bounds remain the same as the semantic footprint.
        pier = max(thickness, round(.75/q))
        for x in (-hx, hx-pier):
            for y in (-hy, hy-pier):
                box(x, y, 1, x+pier, y+pier, hz, "ancient_stone" if role == "shrine" else "sandstone")
                box(x, y, hz-1, x+pier, y+pier, hz, "stone_light")
    return mesh
