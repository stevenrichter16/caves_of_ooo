"""Architectural access and semantic footprint tests, independent of Blender."""
import math
from pathlib import Path
import sys
from types import SimpleNamespace
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from town_generator.architecture import build_structure
from town_generator.materials import MATERIAL_INDEX


def building(role="residence", **changes):
    result = dict(id="test-house", role=role, width=8., depth=8., wall_height=3.,
                  entrances=[(0., -4.)], material_set="sandstone", rotation=0,
                  position=(0., 0.), rooms=[], prop_profile="domestic", occupants=[])
    result.update(changes)
    return SimpleNamespace(**result)


class ArchitectureTests(unittest.TestCase):
    def test_geometry_bounds_match_semantic_footprint(self):
        m = build_structure(building())
        vertices = m.geometry()[0]
        self.assertEqual((-4, -4, 0), tuple(min(v[i] for v in vertices) for i in range(3)))
        self.assertEqual((4, 4, 3), tuple(max(v[i] for v in vertices) for i in range(3)))

    def test_floor_fills_footprint_at_one_voxel_high(self):
        m = build_structure(building())
        self.assertTrue(all((x, y, 0) in m.cells for x in range(-16, 16) for y in range(-16, 16)))
        self.assertNotIn((0, 0, 1), m.cells)

    def test_front_entrance_has_full_walking_clearance(self):
        m = build_structure(building())
        for x in range(-3, 3):
            for y in range(-16, -13):
                for z in range(1, 9):
                    self.assertNotIn((x, y, z), m.cells)

    def test_countercheck_wall_beside_door_is_solid(self):
        m = build_structure(building())
        self.assertIn((-8, -16, 5), m.cells)
        self.assertNotIn((0, -16, 5), m.cells)

    def test_roof_does_not_cover_interior(self):
        m = build_structure(building())
        self.assertTrue(all((x, y, z) not in m.cells
                            for x in range(-5, 5) for y in range(-5, 5) for z in range(1, 12)))

    def test_side_and_back_entrances_are_physically_open(self):
        m = build_structure(building(entrances=[(4, 0), (-1, 4)]))
        for x, y in ((15, 0), (14, -1), (-4, 15), (-3, 14)):
            self.assertTrue(all((x, y, z) not in m.cells for z in range(1, 9)))
        self.assertIn((0, -16, 5), m.cells)

    def test_semantic_world_transform_does_not_change_local_mesh(self):
        a = build_structure(building())
        b = build_structure(building(rotation=math.pi/2, position=(10, -20)))
        self.assertEqual(a.geometry(), b.geometry())

    def test_primary_archetypes_have_distinct_structures(self):
        fingerprints = [repr(build_structure(building(role)).geometry())
                        for role in ("residence", "workshop", "trader", "farmhouse")]
        self.assertEqual(4, len(set(fingerprints)))

    def test_all_archetypes_preserve_center_aisle(self):
        roles = ("residence", "workshop", "trader", "meeting_house", "shrine", "storehouse",
                 "farmhouse", "bathhouse", "animal_enclosure", "guard_watch")
        for role in roles:
            m = build_structure(building(role))
            for x in range(-2, 2):
                for y in range(-16, 0):
                    for z in range(1, 8):
                        self.assertNotIn((x, y, z), m.cells, role)

    def test_pen_has_see_through_rails_but_standing_posts(self):
        m = build_structure(building("animal_enclosure", wall_height=1.5))
        self.assertIn((-16, -16, 3), m.cells)
        self.assertNotIn((-13, -16, 3), m.cells)
        self.assertIn((-13, -16, 2), m.cells)

    def test_watch_structure_has_gaps_between_battlements(self):
        m = build_structure(building("guard_watch", wall_height=3.75))
        top = [(x, 15, 14) in m.cells for x in range(-16, 16)]
        self.assertIn(True, top)
        self.assertIn(False, top)

    def test_material_set_changes_walls_without_changing_occupancy(self):
        a = build_structure(building(material_set="sandstone"))
        b = build_structure(building(material_set="earth"))
        self.assertEqual(set(a.cells), set(b.cells))
        self.assertNotEqual(a.cells, b.cells)

    def test_width_and_depth_variations_keep_both_entrance_and_wall(self):
        for width, depth in ((5., 5.5), (6.5, 7.), (9., 9.)):
            m = build_structure(building(width=width, depth=depth, entrances=[(0, -depth/2)]))
            self.assertNotIn((0, -round(depth/.5), 4), m.cells)
            # Exterior relief may recess one voxel; the continuous inner skin
            # still blocks passage through this wall across every lot size.
            self.assertIn((-round(width/.5)+1, 0, 4), m.cells)

    def test_alternate_voxel_scale_quantizes_all_vertices(self):
        for q in (.125, .5):
            m = build_structure(building(), q)
            self.assertTrue(all(abs(v/q-round(v/q)) < 1e-7 for p in m.geometry()[0] for v in p))

    def test_invalid_dimension_rejected(self):
        for width in (-1, 0, 1, math.nan, math.inf, True):
            with self.subTest(width=width), self.assertRaises(ValueError):
                build_structure(building(width=width))

    def test_unknown_role_and_material_rejected(self):
        for change in ({"role": "typo"}, {"material_set": "water"}):
            with self.subTest(change=change), self.assertRaises(ValueError):
                build_structure(building(**change))

    def test_missing_or_nonperimeter_entrances_rejected(self):
        for entrances in ([], [(0, 0)], [(20, 0)], [(4, 4)]):
            with self.subTest(entrances=entrances), self.assertRaises(ValueError):
                build_structure(building(entrances=entrances))

    def test_civic_piers_cannot_intrude_into_an_accepted_offset_entrance(self):
        # A .75m civic pier needs more clearance than a .5m plain wall corner.
        for role in ("shrine", "meeting_house"):
            opening = .75 if role == "shrine" else 1.
            near_corner = 4.-opening-.5
            with self.subTest(role=role), self.assertRaises(ValueError):
                build_structure(building(role, entrances=[(near_corner, -4.)]))

    def test_civic_piers_leave_valid_offset_entrance_clear(self):
        m = build_structure(building("shrine", entrances=[(1., -4.)]))
        for x in range(1, 7):
            self.assertTrue(all((x, -16, z) not in m.cells for z in range(1, 9)))

    def test_masonry_relief_recesses_outer_faces_without_weakening_inner_wall(self):
        m = build_structure(building())
        # Away from the door, courses contain both proud and recessed stones.
        outer = [(x, -16, z) in m.cells for x in range(-13, -5) for z in range(2, 8)]
        self.assertIn(True, outer)
        self.assertIn(False, outer)
        self.assertTrue(all((x, -15, z) in m.cells for x in range(-13, -5) for z in range(2, 8)))

    def test_trader_lintel_cloth_is_above_walkable_opening(self):
        m = build_structure(building("trader"))
        self.assertIn(MATERIAL_INDEX["cloth_red"], m.cells.values())
        self.assertTrue(all((0, -16, z) not in m.cells for z in range(1, 9)))


if __name__ == "__main__":
    unittest.main()
