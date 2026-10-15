"""Readability constraints derived from the individually generated object targets.
These are source-geometry tests; Unity import and screenshot review remain separate.
"""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class WorldRefinementTests(unittest.TestCase):
    def setUp(self):
        self.repair = {m['id']: m['boxes'] for m in json.loads((ROOT / 'ArtSource/RepairCultivation3D/kit.json').read_text())['models']}
        self.yard = {m['id']: m['boxes'] for m in json.loads((ROOT / 'ArtSource/CurationYard3D/kit.json').read_text())['models']}

    def test_broken_lined_well_exposes_vertical_clay_to_explain_the_material(self):
        blocks = self.repair['repair-cultivation-lined-well-broken']
        self.assertGreaterEqual(sum(b['color'] == 18 and b['size']['y'] >= .12 for b in blocks), 8)
        # The rope well must remain recognizably a different fault.
        self.assertEqual(0, sum(b['color'] == 18 and b['size']['y'] >= .12 for b in self.repair['repair-cultivation-rope-well-broken']))

    def test_well_rim_has_four_solid_corners(self):
        for family in ('lined-well', 'rope-well'):
            for state in ('broken', 'restored'):
                blocks = self.repair[f'repair-cultivation-{family}-{state}']
                for x in (-.30, .30):
                    for z in (-.30, .30):
                        self.assertTrue(any(abs(b['center']['x']-x) < .001 and abs(b['center']['z']-z) < .001
                                            and b['size']['y'] >= .12 for b in blocks), (family, state, x, z))

    def test_clay_lining_is_on_the_exposed_inner_shaft_face(self):
        blocks = self.repair['repair-cultivation-lined-well-broken']
        lining = [b for b in blocks if b['color'] == 18 and b['size']['y'] >= .12]
        self.assertGreaterEqual(len(lining), 8)
        for b in lining:
            depth_axis = 'x' if b['size']['x'] < b['size']['z'] else 'z'
            self.assertLessEqual(abs(b['center'][depth_axis]), .2, 'lining must project inside the masonry interior edge at .205')

    def test_rope_well_has_multiple_winding_loops_with_a_broken_short_drop(self):
        for state in ('broken', 'restored'):
            blocks = self.repair['repair-cultivation-rope-well-' + state]
            self.assertGreaterEqual(sum(b['color'] in (4, 5) and b['center']['y'] > .94 for b in blocks), 10)
        broken = self.repair['repair-cultivation-rope-well-broken']
        restored = self.repair['repair-cultivation-rope-well-restored']
        self.assertFalse(any(b['color'] == 5 and b['size']['y'] > .5 for b in broken))
        self.assertTrue(any(b['color'] == 5 and b['size']['y'] > .5 for b in restored))

    def test_gate_has_readable_metal_hinge_studs_in_each_native_state(self):
        for state in ('broken', 'open', 'closed'):
            blocks = self.repair['repair-cultivation-wooden-gate-' + state]
            self.assertGreaterEqual(sum(b['color'] == 12 and b['size']['x'] < .055 and b['size']['y'] < .055 for b in blocks), 4)

    def test_only_mature_knotflax_has_hanging_harvestable_fiber_tassels(self):
        for wet in ('wet', 'dry'):
            def tassels(stage):
                return sum(b['color'] == 5 and b['size']['y'] >= .16 and b['center']['y'] > .45
                           for b in self.repair[f'repair-cultivation-knotflax-{stage}-{wet}'])
            self.assertGreaterEqual(tassels(2), 8)
            self.assertEqual(0, tassels(0)); self.assertEqual(0, tassels(1))

    def test_hearthbulb_has_broad_exposed_shoulders_and_upright_split_leaves(self):
        for wet in ('wet', 'dry'):
            blocks = self.repair[f'repair-cultivation-hearthbulb-2-{wet}']
            self.assertGreaterEqual(sum(b['color'] == 18 and b['size']['x'] >= .23 for b in blocks), 4)
            self.assertGreaterEqual(sum(b['color'] in (7, 8, 9) and b['size']['y'] >= .2 for b in blocks), 8)

    def test_mature_seamleaf_pale_cross_stitches_are_geometry_not_ui_marks(self):
        for wet in ('wet', 'dry'):
            blocks = self.repair[f'repair-cultivation-seamleaf-2-{wet}']
            self.assertGreaterEqual(sum(b['color'] == 17 and .04 <= b['size']['x'] <= .08 and b['size']['z'] <= .025 for b in blocks), 12)

    def test_cured_body_salt_crystals_and_wrist_tally_preserve_case_identity(self):
        for case in (1, 2):
            blocks = self.yard[f'curation-yard-salt-cured-body-{case}']
            self.assertGreaterEqual(sum(b['name'].startswith('salt-crystal-') for b in blocks), 6)
            self.assertTrue(any(b['name'] == 'wrist-tally-cord' for b in blocks))
            self.assertEqual(case, sum(b['name'].startswith('case-notch-') for b in blocks))

    def test_fire_clay_is_a_compact_chipped_clay_material_not_upright_crystals(self):
        blocks = self.repair.get('repair-cultivation-fire-clay')
        self.assertIsNotNone(blocks, 'Fire clay needs an individual clay-chunk model in the bounded repair kit.')
        clay = [b for b in blocks if b['color'] == 18]
        self.assertGreaterEqual(len(clay), 3)
        self.assertGreaterEqual(sum(b['size']['x'] >= .14 and b['size']['z'] >= .14 for b in clay), 3)
        self.assertGreaterEqual(sum(b['color'] in (4, 5, 17) and max(b['size'].values()) <= .045 for b in blocks), 3)
        self.assertLessEqual(max(b['center']['y'] + b['size']['y'] / 2 for b in blocks), .34)

    def test_preserved_body_has_no_new_hands_hidden_inside_the_existing_forearm(self):
        for case in (1, 2):
            blocks = self.yard[f'curation-yard-salt-cured-body-{case}']
            arm = next((b for b in blocks if b['name'] == 'folded-forearm'), None)
            if arm is None:
                continue
            top = arm['center']['y'] + arm['size']['y'] / 2
            for hand in (b for b in blocks if b['name'].startswith('folded-hand-')):
                self.assertGreater(hand['center']['y'] + hand['size']['y'] / 2, top + .01,
                                   'An added hand must contribute visible geometry rather than sit inside the existing arm.')


if __name__ == '__main__':
    unittest.main()
