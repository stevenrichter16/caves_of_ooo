"""Geometry contracts; ordinary Python tests, with optional actual Blender audit.

Run: python3 -m unittest discover -s ArtSource/VoxelTown/tests -p test_assets.py
Run Blender: blender -b --python-exit-code 1 --python this_file -- --blender-audit
"""
import json
import math
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from town_generator.mesh import VoxelMesh
from town_generator.materials import PALETTE_NAMES, MATERIAL_INDEX
from town_generator.assets import build_recipes, build_assets, REQUIRED_ASSETS


class VoxelGeometryTests(unittest.TestCase):
    def test_single_block_is_six_quads(self):
        m = VoxelMesh(.25).bounds((0, 0, 0), (1, 1, 1), "sandstone")
        vertices, faces, materials = m.geometry()
        self.assertEqual(64, m.cell_count)
        self.assertEqual(6, len(faces))
        self.assertEqual(8, len(vertices))
        self.assertEqual({MATERIAL_INDEX["sandstone"]}, set(materials))

    def test_touching_identical_blocks_have_no_interior_faces(self):
        m = VoxelMesh().bounds((0, 0, 0), (1, 1, 1), "wood")
        m.bounds((1, 0, 0), (2, 1, 1), "wood")
        self.assertEqual(6, len(m.geometry()[1]))

    def test_separated_blocks_retain_inner_facing_sides(self):
        m = VoxelMesh().bounds((0, 0, 0), (1, 1, 1), "wood")
        m.bounds((2, 0, 0), (3, 1, 1), "wood")
        self.assertEqual(12, len(m.geometry()[1]))

    def test_touching_distinct_materials_keep_surface_boundary(self):
        m = VoxelMesh().bounds((0, 0, 0), (1, 1, 1), "wood")
        m.bounds((1, 0, 0), (2, 1, 1), "metal")
        self.assertEqual(10, len(m.geometry()[1]))
        self.assertEqual({MATERIAL_INDEX["wood"], MATERIAL_INDEX["metal"]},
                         set(m.geometry()[2]))

    def test_last_material_wins_overlap_without_duplicate_solid(self):
        m = VoxelMesh().bounds((0, 0, 0), (1, 1, 1), "wood")
        m.bounds((0, 0, 0), (1, 1, 1), "metal")
        self.assertEqual(64, m.cell_count)
        self.assertEqual({MATERIAL_INDEX["metal"]}, set(m.geometry()[2]))

    def test_face_winding_points_outward(self):
        m = VoxelMesh().bounds((-1, -1, -1), (1, 1, 1), "sandstone")
        vertices, faces, _ = m.geometry()
        for face in faces:
            a, b, c, _ = [vertices[i] for i in face]
            u = tuple(b[i] - a[i] for i in range(3))
            v = tuple(c[i] - a[i] for i in range(3))
            normal = (u[1]*v[2]-u[2]*v[1], u[2]*v[0]-u[0]*v[2], u[0]*v[1]-u[1]*v[0])
            self.assertGreater(sum(normal[i]*a[i] for i in range(3)), 0)

    def test_cache_is_invalidated_by_new_block(self):
        m = VoxelMesh().bounds((0, 0, 0), (.25, .25, .25), "wood")
        before = m.geometry()
        m.bounds((1, 0, 0), (1.25, .25, .25), "wood")
        self.assertEqual(6, len(before[1]))
        self.assertEqual(12, len(m.geometry()[1]))

    def test_integer_material_index_matches_name(self):
        a = VoxelMesh().bounds((0, 0, 0), (1, 1, 1), "wood")
        b = VoxelMesh().bounds((0, 0, 0), (1, 1, 1), MATERIAL_INDEX["wood"])
        self.assertEqual(a.geometry(), b.geometry())

    def test_nearest_quantization_is_symmetric_at_ties(self):
        m = VoxelMesh().box((0, 0, 0), (.25, .25, .25), "wood")
        vertices = m.geometry()[0]
        self.assertEqual({-.25, .25}, {p[0] for p in vertices})

    def test_off_grid_coordinates_are_snapped(self):
        m = VoxelMesh().bounds((.13, -.39, .1), (.88, .49, .76), "wood")
        for vertex in m.geometry()[0]:
            for x in vertex:
                self.assertEqual(round(x/.25), x/.25)

    def test_empty_geometry_raises(self):
        with self.assertRaises(ValueError):
            VoxelMesh().geometry()

    def test_bad_material_rejected_without_mutation(self):
        m = VoxelMesh()
        with self.assertRaises(ValueError):
            m.bounds((0, 0, 0), (1, 1, 1), "missing")
        self.assertEqual(0, m.cell_count)

    def test_bad_bounds_rejected_without_partial_change(self):
        m = VoxelMesh().bounds((0, 0, 0), (1, 1, 1), "wood")
        before = m.geometry()
        for bad in ((0, 1, 1), (-1, 1, 1), (math.nan, 1, 1), (math.inf, 1, 1)):
            with self.subTest(bad=bad), self.assertRaises(ValueError):
                m.bounds((0, 0, 0), bad, "wood")
        self.assertEqual(before, m.geometry())

    def test_invalid_voxel_scales_rejected(self):
        for size in (0, -.25, math.nan, math.inf, True, "x"):
            with self.subTest(size=size), self.assertRaises(ValueError):
                VoxelMesh(size)

    def test_subvoxel_collapse_rejected(self):
        with self.assertRaises(ValueError):
            VoxelMesh().bounds((.01, .01, .01), (.02, .02, .02), "wood")

    def test_bad_vectors_rejected(self):
        for vec in (None, (), (1, 2), (1, 2, 3, 4), (True, 1, 1), ("1", 1, 1)):
            with self.subTest(vec=vec), self.assertRaises(ValueError):
                VoxelMesh().box(vec, (1, 1, 1), "wood")

    def test_invalid_material_indices_rejected(self):
        for mat in (-1, len(PALETTE_NAMES), True, 1.2, None):
            with self.subTest(mat=mat), self.assertRaises(ValueError):
                VoxelMesh().bounds((0, 0, 0), (1, 1, 1), mat)

    def test_unreasonable_box_is_rejected_before_allocation(self):
        with self.assertRaises(ValueError):
            VoxelMesh().bounds((0, 0, 0), (1000000, 1000000, 1000000), "wood")


class AssetRecipeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.recipes = build_recipes(.25)

    def test_every_required_category_has_real_volume(self):
        self.assertTrue(set(REQUIRED_ASSETS).issubset(self.recipes))
        for key in REQUIRED_ASSETS:
            with self.subTest(asset=key):
                self.assertGreater(self.recipes[key].cell_count, 0)

    def test_all_recipe_vertices_are_finite_and_quantized(self):
        for key, mesh in self.recipes.items():
            with self.subTest(asset=key):
                for point in mesh.geometry()[0]:
                    self.assertTrue(all(math.isfinite(x) and x/.25 == round(x/.25) for x in point))

    def test_all_assets_ground_at_zero(self):
        for key, mesh in self.recipes.items():
            with self.subTest(asset=key):
                self.assertEqual(0, min(v[2] for v in mesh.geometry()[0]))

    def test_repeated_natural_objects_have_three_distinct_silhouettes(self):
        for key in ("reeds", "scrub", "rock", "crop", "tree", "palm", "npc", "livestock"):
            signatures = []
            for suffix in ("", "_1", "_2"):
                mesh = self.recipes[key+suffix]
                signatures.append(tuple(mesh.geometry()[0]))
            self.assertEqual(3, len(set(signatures)), key)

    def test_recipes_are_deterministic_and_do_not_use_global_rng(self):
        import random
        random.seed(789)
        state = random.getstate()
        again = build_recipes(.25)
        self.assertEqual(state, random.getstate())
        self.assertEqual(list(self.recipes), list(again))
        for key in self.recipes:
            self.assertEqual(self.recipes[key].geometry(), again[key].geometry())

    def test_voxel_scale_changes_size_but_not_topology(self):
        other = build_recipes(.5)
        for key in self.recipes:
            a, af, am = self.recipes[key].geometry()
            b, bf, bm = other[key].geometry()
            self.assertEqual(af, bf)
            self.assertEqual(am, bm)
            self.assertEqual([tuple(x*2 for x in v) for v in a], b)

    def test_npc_is_gameplay_scale(self):
        vertices = self.recipes["npc"].geometry()[0]
        self.assertGreaterEqual(max(v[2] for v in vertices), 1.75)
        self.assertLessEqual(max(v[2] for v in vertices), 2.0)

    def test_assets_are_bounded_not_extravagant_microgeometry(self):
        for key, mesh in self.recipes.items():
            with self.subTest(asset=key):
                self.assertLess(len(mesh.geometry()[1]), 1500)
                self.assertLess(mesh.cell_count, 20000)

    def test_communal_firepit_has_visible_coals_and_low_stone_ring(self):
        mesh = self.recipes["firepit"]
        vertices, _, mats = mesh.geometry()
        self.assertIn(MATERIAL_INDEX["ember"], mats)
        self.assertIn(MATERIAL_INDEX["ancient_stone"], mats)
        self.assertLessEqual(max(v[2] for v in vertices), 1)
        self.assertGreater(max(v[0] for v in vertices)-min(v[0] for v in vertices), 1)

    def test_scrub_has_separate_grounded_sprigs_not_filled_leaf_pads(self):
        for key in ("scrub", "scrub_1", "scrub_2"):
            ground = {(x, y) for x, y, z in self.recipes[key].cells if z == 0}
            area = (max(x for x, y in ground)-min(x for x, y in ground)+1) \
                * (max(y for x, y in ground)-min(y for x, y in ground)+1)
            self.assertLessEqual(len(ground), area*.55, key)


class VoxelAdversarialTests(unittest.TestCase):
    """Untrusted recipe numbers, material edits, solid union and RNG isolation.

    These target authoring mistakes rather than visual taste. Surface-area
    assertions derive expected boundaries independently from greedy meshing.
    """
    @staticmethod
    def surface_area(mesh):
        vertices, faces, _ = mesh.geometry()
        total = 0
        for face in faces:
            a, b, _, d = [vertices[i] for i in face]
            total += math.dist(a, b)*math.dist(a, d)
        return total

    def test_adversarial_budget_repaint_does_not_need_extra_capacity(self):
        m = VoxelMesh()
        m.MAX_CELLS = 64
        m.bounds((0, 0, 0), (1, 1, 1), "wood")
        m.bounds((0, 0, 0), (1, 1, 1), "metal")
        self.assertEqual(64, m.cell_count)
        self.assertEqual({MATERIAL_INDEX["metal"]}, set(m.geometry()[2]))

    def test_adversarial_partial_overlap_respects_exact_capacity(self):
        m = VoxelMesh()
        m.MAX_CELLS = 96
        m.bounds((0, 0, 0), (1, 1, 1), "wood")
        m.bounds((.5, 0, 0), (1.5, 1, 1), "metal")
        self.assertEqual(96, m.cell_count)

    def test_adversarial_capacity_failure_is_atomic(self):
        m = VoxelMesh()
        m.MAX_CELLS = 64
        m.bounds((0, 0, 0), (1, 1, 1), "wood")
        before = m.geometry()
        with self.assertRaises(ValueError):
            m.bounds((.25, 0, 0), (1.25, 1, 1), "metal")
        self.assertEqual(before, m.geometry())
        self.assertEqual(1, m.box_count)

    def test_adversarial_finite_coordinate_ratio_overflow_is_value_error(self):
        with self.assertRaises(ValueError):
            VoxelMesh().bounds((0, 0, 0), (1e308, 1, 1), "wood")

    def test_adversarial_tiny_scale_ratio_overflow_is_value_error(self):
        with self.assertRaises(ValueError):
            VoxelMesh(1e-308).bounds((0, 0, 0), (1e308, 1, 1), "wood")

    def test_adversarial_box_center_overflow_is_value_error(self):
        with self.assertRaises(ValueError):
            VoxelMesh().box((1e308, 0, 0), (1e308, 1, 1), "wood")

    def test_adversarial_negative_coordinate_translation_preserves_area(self):
        a = VoxelMesh().bounds((0, 0, 0), (1, 2, 3), "wood")
        b = VoxelMesh().bounds((-11, -12, -13), (-10, -10, -10), "wood")
        self.assertEqual(self.surface_area(a), self.surface_area(b))

    def test_adversarial_disjoint_union_area_is_additive(self):
        m = VoxelMesh().bounds((0, 0, 0), (1, 1, 1), "wood")
        m.bounds((5, 0, 0), (6, 1, 1), "metal")
        self.assertEqual(12, self.surface_area(m))

    def test_adversarial_buried_material_creates_no_surface(self):
        m = VoxelMesh().bounds((0, 0, 0), (2, 2, 2), "wood")
        m.bounds((.5, .5, .5), (1.5, 1.5, 1.5), "metal")
        self.assertEqual({MATERIAL_INDEX["wood"]}, set(m.geometry()[2]))
        self.assertEqual(24, self.surface_area(m))

    def test_adversarial_cavity_surface_remains_exposed(self):
        m = VoxelMesh(1)
        for x in range(3):
            for y in range(3):
                for z in range(3):
                    if (x, y, z) != (1, 1, 1):
                        m.bounds((x, y, z), (x+1, y+1, z+1), "wood")
        self.assertEqual(60, self.surface_area(m))

    def test_adversarial_t_junction_preserves_boundary_area(self):
        m = VoxelMesh(1)
        for p in ((0, 0, 0), (1, 0, 0), (2, 0, 0), (1, 1, 0)):
            m.bounds(p, tuple(x+1 for x in p), "wood")
        self.assertEqual(18, self.surface_area(m))

    def test_adversarial_material_islands_do_not_inflate_surface_area(self):
        m = VoxelMesh().bounds((0, 0, 0), (2, 2, 2), "wood")
        for p in ((0, 0, 0), (.75, 0, 0), (1.75, 1.75, 1.75)):
            m.bounds(p, tuple(x+.25 for x in p), "metal")
        self.assertEqual(24, self.surface_area(m))

    def test_adversarial_insertion_order_without_overlaps_is_irrelevant(self):
        positions = ((0, 0, 0), (.5, 0, 0), (.25, .25, 0))
        result = []
        for order in (positions, tuple(reversed(positions))):
            m = VoxelMesh()
            for p in order:
                m.bounds(p, tuple(x+.25 for x in p), "wood")
            result.append(m.geometry())
        self.assertEqual(*result)

    def test_adversarial_seeded_shape_surface_matches_independent_neighbors(self):
        import random
        rng = random.Random(8493)
        for seed_case in range(15):
            cells = {(rng.randrange(-3, 4), rng.randrange(-3, 4), rng.randrange(0, 4))
                     for _ in range(75)}
            m = VoxelMesh(1)
            for p in sorted(cells):
                m.bounds(p, tuple(x+1 for x in p), "wood")
            expected = 0
            for p in cells:
                for d in ((1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)):
                    expected += tuple(p[i]+d[i] for i in range(3)) not in cells
            self.assertEqual(expected, self.surface_area(m), seed_case)

    def test_adversarial_float_material_index_does_not_truncate(self):
        with self.assertRaises(ValueError):
            VoxelMesh().box((0, 0, 0), (1, 1, 1), 3.99)

    def test_adversarial_whitespace_material_does_not_silently_fallback(self):
        with self.assertRaises(ValueError):
            VoxelMesh().box((0, 0, 0), (1, 1, 1), " wood ")

    def test_adversarial_negative_size_cannot_swap_bounds(self):
        with self.assertRaises(ValueError):
            VoxelMesh().box((0, 0, 0), (-1, 1, 1), "wood")

    def test_adversarial_failed_edit_keeps_cached_geometry(self):
        m = VoxelMesh().box((0, 0, 0), (1, 1, 1), "wood")
        before = m.geometry()
        with self.assertRaises(ValueError):
            m.box((0, 0, 0), (1, 1, 1), "absent")
        self.assertIs(before, m.geometry())

    def test_adversarial_repeated_recipe_objects_are_independent(self):
        recipes = build_recipes()
        before = recipes["rock_1"].geometry()
        recipes["rock"].bounds((5, 5, 5), (6, 6, 6), "metal")
        self.assertEqual(before, recipes["rock_1"].geometry())

    def test_adversarial_light_material_is_not_used_for_whole_prop(self):
        for key in ("furnace", "lantern", "torch"):
            mats = set(build_recipes()[key].geometry()[2])
            self.assertIn(MATERIAL_INDEX["ember"], mats)
            self.assertGreater(len(mats), 1)

    def test_adversarial_livestock_horns_attach_by_faces_not_floating_edges(self):
        # The gameplay-angle contact sheet exposed an edge-only second horn.
        # Animals must be one face-connected solid, unlike grouped reeds.
        recipes = build_recipes()
        for key in ("livestock", "livestock_1", "livestock_2"):
            cells = set(recipes[key].cells)
            pending = [next(iter(cells))]
            visited = set(pending)
            while pending:
                p = pending.pop()
                for d in ((1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)):
                    neighbor = tuple(p[i]+d[i] for i in range(3))
                    if neighbor in cells and neighbor not in visited:
                        visited.add(neighbor)
                        pending.append(neighbor)
            self.assertEqual(cells, visited, key)


def blender_audit():
    import bpy
    from town_generator.materials import make_palette
    owner = "asset-library-test"
    initial_mesh_objects = sum(obj.type == "MESH" for obj in bpy.data.objects)
    sentinel = bpy.data.objects.new("Manual sentinel", None)
    bpy.context.scene.collection.objects.link(sentinel)
    material_sentinel = bpy.data.materials.new(f"VX_{owner}_wood")
    material_sentinel.diffuse_color = (.13, .27, .42, .5)
    sentinel_color = tuple(material_sentinel.diffuse_color)
    prototypes = bpy.data.collections.new("UNLINKED_ASSET_TEST")
    # The caller owns template collection lifetime, exactly as scene.py does.
    prototypes["coo_voxel_owner"] = owner
    prototypes.use_fake_user = True
    palette = make_palette(owner)
    assert tuple(material_sentinel.diffuse_color) == sentinel_color
    assert material_sentinel.get("coo_voxel_owner") is None
    assert material_sentinel not in palette
    assert make_palette(owner) == palette  # Idempotent ownership lookup.
    for key, material in zip(PALETTE_NAMES, palette):
        assert material.get("coo_voxel_key") == key
        assert material.get("coo_voxel_owner") == owner
        assert material.diffuse_color[3] == 1
    # Bad palette ordering fails before any datablock is allocated.
    before_meshes, before_objects = len(bpy.data.meshes), len(bpy.data.objects)
    try:
        build_recipes()["crate"].build("bad", prototypes, list(reversed(palette)), owner)
        raise AssertionError("Malformed palette accepted")
    except ValueError:
        pass
    assert (len(bpy.data.meshes), len(bpy.data.objects)) == (before_meshes, before_objects)
    assets = build_assets(prototypes, palette, .25, owner)
    assert len(assets) == len(build_recipes(.25))
    assert sentinel in tuple(bpy.context.scene.objects)
    assert all(obj not in tuple(bpy.context.scene.objects) for obj in assets.values())
    rows = []
    for key, obj in assets.items():
        assert obj.type == "MESH" and obj.get("coo_voxel_owner") == owner
        assert obj.data.get("coo_voxel_owner") == owner
        assert not obj.modifiers
        assert not obj.data.validate()  # Blender reports no malformed topology.
        assert len(obj.data.polygons) > 0
        assert not any(p.use_smooth for p in obj.data.polygons)
        assert all(math.isfinite(c) and abs(c/.25-round(c/.25)) < 1e-7
                   for vertex in obj.data.vertices for c in vertex.co)
        assert all(0 <= p.material_index < len(palette) for p in obj.data.polygons)
        rows.append({"key": key, "vertices": len(obj.data.vertices),
                     "quads": len(obj.data.polygons), "cells": obj.get("coo_voxel_cells")})
    # Prove linked-mesh copies are visible without unhiding/mutating source assets.
    instance = bpy.data.objects.new("instance", assets["npc"].data)
    bpy.context.scene.collection.objects.link(instance)
    assert instance.data is assets["npc"].data and not instance.hide_render
    assert sum(o.type == "MESH" for o in bpy.data.objects) == len(assets)+1+initial_mesh_objects
    output = {"status": "passed", "assets": len(rows), "materials": len(palette),
              "rows": rows, "manual_object_preserved": True, "prototypes_visible": False,
              "same_named_manual_material_preserved": True,
              "palette_order_rejection_atomic": True, "palette_regeneration_idempotent": True,
              "blender_mesh_validation": "passed"}
    bpy.ops.wm.save_as_mainfile(filepath="/tmp/voxel-assets-test.blend")
    bpy.ops.wm.open_mainfile(filepath="/tmp/voxel-assets-test.blend")
    reopened = bpy.data.collections.get("UNLINKED_ASSET_TEST")
    assert reopened is not None and len(reopened.objects) == len(rows)
    output["asset_collection_preserved_after_save_reload"] = True
    Path("/tmp/voxel-assets-audit.json").write_text(json.dumps(output, indent=2)+"\n")
    print("VOXEL_ASSET_AUDIT " + json.dumps(output))


if __name__ == "__main__":
    if "--blender-audit" in sys.argv:
        blender_audit()
    else:
        unittest.main()
