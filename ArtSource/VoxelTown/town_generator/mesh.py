"""Quantized solid boxes -> material-aware greedy surface mesh.

Logical voxel occupancy is independent of Blender. A box edits a compact
integer-cell map; its visible boundary becomes a single mesh. Adjacent filled
cells never produce internal faces, and coplanar equal-material boundaries
are combined into rectangles. There is no cube object per cell.
"""
import math
from numbers import Real

from .materials import MATERIAL_INDEX, PALETTE_NAMES


def _vector(value, label):
    try:
        result = tuple(value)
    except TypeError as exc:
        raise ValueError(f"{label} must contain three finite numbers") from exc
    if len(result) != 3 or any(isinstance(x, bool) or not isinstance(x, Real)
                               or not math.isfinite(x) for x in result):
        raise ValueError(f"{label} must contain three finite numbers")
    return result


class VoxelMesh:
    """Mutable voxel recipe with deterministic greedy mesh output.

    Coordinates are metres. Both box bounds snap to the nearest grid plane,
    with exact half-grid ties away from zero. Use explicit ``bounds`` when
    precise dimensions matter: a .25-wide feature starts on a .25 plane.
    Later boxes overwrite overlapping cells' material without adding volume.
    Invalid inputs fail before any mutation. Maximum occupied cells is a
    safety guard against accidental kilometre-sized solid boxes.
    """
    MAX_CELLS = 2_000_000

    def __init__(self, voxel_size=.25):
        if isinstance(voxel_size, bool) or not isinstance(voxel_size, Real) \
                or not math.isfinite(voxel_size) or voxel_size <= 0:
            raise ValueError("voxel_size must be a positive finite number")
        self.voxel_size = float(voxel_size)
        self.cells = {}
        self.box_count = 0
        self._geometry = None

    @property
    def cell_count(self):
        return len(self.cells)

    def _snap(self, value):
        scaled = value / self.voxel_size
        if not math.isfinite(scaled):
            raise ValueError("Coordinate-to-voxel ratio exceeds finite range")
        return math.floor(scaled + .5) if scaled >= 0 else math.ceil(scaled - .5)

    @staticmethod
    def _material(material):
        if isinstance(material, str):
            if material not in MATERIAL_INDEX:
                raise ValueError(f"Unknown voxel material {material!r}")
            return MATERIAL_INDEX[material]
        if isinstance(material, bool) or not isinstance(material, int) \
                or not 0 <= material < len(PALETTE_NAMES):
            raise ValueError("Material must be a known name or valid palette index")
        return material

    def box(self, center, size, material="sandstone"):
        center, size = _vector(center, "center"), _vector(size, "size")
        if any(x <= 0 for x in size):
            raise ValueError("Box size must be positive on every axis")
        return self.bounds(tuple(c-s/2 for c, s in zip(center, size)),
                           tuple(c+s/2 for c, s in zip(center, size)), material)

    def bounds(self, minimum, maximum, material="sandstone"):
        minimum, maximum = _vector(minimum, "minimum"), _vector(maximum, "maximum")
        material = self._material(material)
        if any(b <= a for a, b in zip(minimum, maximum)):
            raise ValueError("Maximum must exceed minimum on every axis")
        lo, hi = tuple(map(self._snap, minimum)), tuple(map(self._snap, maximum))
        if any(b <= a for a, b in zip(lo, hi)):
            raise ValueError("Box collapses below the voxel grid")
        volume = math.prod(b-a for a, b in zip(lo, hi))
        if volume > self.MAX_CELLS:
            raise ValueError("Voxel cell budget exceeded; use separate surface batches")
        if len(self.cells)+volume > self.MAX_CELLS:
            # Repainting does not allocate cells. Count exact additions only
            # near the guard; an invalid overlap edit must fail atomically.
            # Adversarial asset tests caught conservative double-counting here.
            added = sum((x, y, z) not in self.cells
                        for x in range(lo[0], hi[0])
                        for y in range(lo[1], hi[1])
                        for z in range(lo[2], hi[2]))
            if len(self.cells)+added > self.MAX_CELLS:
                raise ValueError("Voxel cell budget exceeded; use separate surface batches")
        for x in range(lo[0], hi[0]):
            for y in range(lo[1], hi[1]):
                for z in range(lo[2], hi[2]):
                    self.cells[(x, y, z)] = material
        self.box_count += 1
        self._geometry = None
        return self

    def geometry(self):
        """Return (vertices, outward-wound quad indices, material indices).

        Deterministic for a final cell map, independent of box insertion order
        except intentional last-material-wins edits. The returned lists should
        be treated as read-only by consumers.
        """
        if self._geometry is not None:
            return self._geometry
        if not self.cells:
            raise ValueError("Cannot build an empty voxel mesh")
        # Group boundary face unit squares by axis, sign, plane, material.
        planes = {}
        for cell, material in self.cells.items():
            for axis in range(3):
                u, v = (axis+1) % 3, (axis+2) % 3
                for sign in (-1, 1):
                    neighbor = list(cell)
                    neighbor[axis] += sign
                    if tuple(neighbor) in self.cells:
                        continue
                    plane = cell[axis]+(sign > 0)
                    key = (axis, sign, plane, material)
                    planes.setdefault(key, set()).add((cell[u], cell[v]))
        vertices, faces, materials, vertex_index = [], [], [], {}
        for (axis, sign, plane, material), squares in sorted(planes.items()):
            u, v = (axis+1) % 3, (axis+2) % 3
            # Sort candidate starts once. Repeated min(set) was quadratic for
            # mottled terrain; consumed rectangles simply skip later starts.
            for x, y in sorted(squares):
                if (x, y) not in squares:
                    continue
                width = 1
                while (x+width, y) in squares:
                    width += 1
                height = 1
                while all((xx, y+height) in squares for xx in range(x, x+width)):
                    height += 1
                for xx in range(x, x+width):
                    for yy in range(y, y+height):
                        squares.remove((xx, yy))
                corners = ((x, y), (x+width, y), (x+width, y+height), (x, y+height))
                if sign < 0:
                    corners = tuple(reversed(corners))
                face = []
                for cu, cv in corners:
                    point = [0, 0, 0]
                    point[axis], point[u], point[v] = plane, cu, cv
                    point = tuple(point)
                    if point not in vertex_index:
                        vertex_index[point] = len(vertices)
                        vertices.append(tuple(x*self.voxel_size for x in point))
                    face.append(vertex_index[point])
                faces.append(tuple(face))
                materials.append(material)
        self._geometry = (vertices, faces, materials)
        return self._geometry

    def build(self, name, collection, materials, owner_token):
        """Create one flat-shaded Blender mesh in the supplied collection.

        No collection linking, selection, global reset, or scene mutation occurs
        beyond this new object's link. Template collections may stay unlinked;
        visible instances can then share the returned object's ``data``.
        """
        import bpy
        if not isinstance(owner_token, str) or not owner_token.strip():
            raise ValueError("A nonempty owner token is required")
        if len(materials) != len(PALETTE_NAMES) or any(
                mat is None or mat.get("coo_voxel_key") != key
                for mat, key in zip(materials, PALETTE_NAMES)):
            raise ValueError("Materials must have the exact stable voxel palette order")
        if collection is None or not hasattr(collection, "objects"):
            raise ValueError("A Blender collection is required")
        vertices, faces, material_ids = self.geometry()
        mesh = bpy.data.meshes.new(name+"_mesh")
        mesh["coo_voxel_owner"] = owner_token
        mesh.from_pydata(vertices, [], faces)
        for material in materials:
            mesh.materials.append(material)
        for face, index in zip(mesh.polygons, material_ids):
            face.material_index = index
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        obj["coo_voxel_owner"] = owner_token
        obj["coo_voxel_cells"] = self.cell_count
        obj["coo_voxel_size"] = self.voxel_size
        obj["coo_voxel_boxes"] = self.box_count
        collection.objects.link(obj)
        return obj
