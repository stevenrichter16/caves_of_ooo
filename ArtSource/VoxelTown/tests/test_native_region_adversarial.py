"""Native export bug taxonomy: parser edges, grid gates and mutated ownership."""
import copy
import unittest
from town_generator.native_region import NativeConfig,generate_region,validate_region


class NativeRegionAdversarialTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):cls.region=generate_region()

    def test_hypothesis_one_blocker_can_close_the_only_building_entrance(self):
        bad=copy.deepcopy(self.region);c=bad['chunks'][0];x,y=c['buildings'][0]['entrance']
        row=copy.deepcopy(c['placements'][0]);row.update(id='bad-door',x=x,y=y,cells=[[0,0]],solid=True)
        c['placements'].append(row)
        with self.assertRaises(ValueError):validate_region(bad)

    def test_hypothesis_extra_terrain_cell_hides_duplicate_coordinates(self):
        bad=copy.deepcopy(self.region);bad['chunks'][0]['terrain'].append(copy.deepcopy(bad['chunks'][0]['terrain'][0]))
        with self.assertRaises(ValueError):validate_region(bad)

    def test_hypothesis_returned_mutation_leaks_into_future_generation(self):
        bad=generate_region();bad['chunks'][0]['placements'].clear()
        self.assertEqual(self.region,generate_region())

    def test_hypothesis_missing_native_floor_is_silently_accepted(self):
        bad=copy.deepcopy(self.region);bad['chunks'][1]['terrain'].pop()
        with self.assertRaises(ValueError):validate_region(bad)

    def test_hypothesis_an_owner_crosses_zone_boundary(self):
        bad=copy.deepcopy(self.region);bad['chunks'][0]['placements'][0]['cells']=[[0,0],[80,25]]
        with self.assertRaises(ValueError):validate_region(bad)

    def test_hypothesis_wall_cells_can_be_empty_visual_claims(self):
        bad=copy.deepcopy(self.region);c=bad['chunks'][0]
        c['placements']=[p for p in c['placements'] if p['role']!='wall']
        with self.assertRaises(ValueError):validate_region(bad)

    def test_unknown_asset_cannot_turn_into_a_missing_visual(self):
        bad=copy.deepcopy(self.region);bad['chunks'][0]['placements'][0]['asset']='voxel-unshipped'
        with self.assertRaises(ValueError):validate_region(bad)

    def test_fractional_anchor_cannot_evade_integer_cell_collision(self):
        bad=copy.deepcopy(self.region);bad['chunks'][0]['placements'][0]['x']+=.25
        with self.assertRaises(ValueError):validate_region(bad)

    def test_out_of_bounds_route_is_not_an_unchecked_manifest_claim(self):
        bad=copy.deepcopy(self.region);bad['chunks'][0]['routes'].append([2000,2000])
        with self.assertRaises(ValueError):validate_region(bad)

    def test_wrong_external_neighbor_is_rejected_even_without_a_partner_chunk(self):
        bad=copy.deepcopy(self.region);bad['chunks'][0]['portals'][0]['neighborZoneId']='Overworld.99.99.0'
        with self.assertRaises(ValueError):validate_region(bad)

    def test_native_owner_cannot_claim_a_smaller_body_than_its_recipe(self):
        bad=copy.deepcopy(self.region)
        target=next(p for p in bad['chunks'][0]['placements'] if len(p['cells'])>1)
        target['cells']=[[0,0]]
        with self.assertRaises(ValueError):validate_region(bad)

    def test_a_missing_wall_in_both_visual_and_semantic_lists_is_still_a_hole(self):
        bad=copy.deepcopy(self.region);c=bad['chunks'][0];b=c['buildings'][0];x,y=b['wallCells'].pop()
        c['placements']=[p for p in c['placements'] if (p['x'],p['y'])!=(x,y)]
        with self.assertRaises(ValueError):validate_region(bad)

    def test_optional_clutter_stays_near_actual_activity_sites(self):
        for c in self.region['chunks']:
            for p in c['placements']:
                if not p.get('optional'):continue
                close=False
                for b in c['buildings']:
                    x,y,w,h=b['bounds']
                    if max(x-p['x'],0,p['x']-(x+w-1))+max(y-p['y'],0,p['y']-(y+h-1))<=4:close=True
                self.assertTrue(close,(c['zoneId'],p['id']))

    def test_wall_physical_flags_cannot_turn_structure_into_ghost_art(self):
        for flag in ('solid','opaque'):
            bad=copy.deepcopy(self.region);wall=next(p for p in bad['chunks'][0]['placements'] if p['role']=='wall')
            wall[flag]=False
            with self.assertRaises(ValueError):validate_region(bad)

    def test_plaza_must_be_an_actual_open_in_bounds_cell(self):
        for blocked in (False,True):
            bad=copy.deepcopy(self.region);c=bad['chunks'][0]
            c['anchors']['plaza']=c['buildings'][0]['wallCells'][0] if blocked else [-1,12]
            with self.assertRaises(ValueError):validate_region(bad)


def invalid_case(name,kwargs):
    def test(self):
        with self.assertRaises(ValueError):generate_region(NativeConfig(**kwargs))
    test.__name__=name;return test

for _i,kwargs in enumerate(({'seed':True},{'seed':1.5},{'seed':None},
    {'width':True},{'width':39},{'width':257},{'width':80.5},
    {'height':19},{'height':129},{'height':float('nan')},
    {'water_amount':-1},{'water_amount':1.1},{'water_amount':float('nan')},
    {'vegetation_amount':True},{'vegetation_amount':float('inf')},
    {'clutter_amount':-0.1},{'clutter_amount':None},
    {'population_per_building':-1},{'population_per_building':True},{'population_per_building':1000})):
    setattr(NativeRegionAdversarialTests,f'test_invalid_parameter_{_i:02}',invalid_case(str(_i),kwargs))


if __name__=='__main__':unittest.main()
