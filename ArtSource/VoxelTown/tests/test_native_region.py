"""Rectangular gameplay candidates, independent of Unity installation policy."""
import copy
import json
import random
import unittest

from town_generator.native_region import NativeConfig, generate_region, validate_region


class NativeRegionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):cls.region=generate_region()

    def test_four_distinct_connected_roles_and_native_dimensions(self):
        data=self.region
        self.assertEqual((80,25),(data['zoneWidth'],data['zoneHeight']))
        self.assertEqual({'Overworld.3.6.0','Overworld.2.6.0','Overworld.3.7.0','Overworld.4.7.0'},
                         {c['zoneId'] for c in data['chunks']})
        self.assertEqual(4,len({c['role'] for c in data['chunks']}))
        self.assertTrue(validate_region(data))

    def test_exact_terrain_coverage_and_interior_semantics(self):
        for c in self.region['chunks']:
            terrain={(r['x'],r['y']):r for r in c['terrain']}
            self.assertEqual(2000,len(terrain))
            for b in c['buildings']:
                self.assertTrue(b['interiorCells']);self.assertTrue(b['entranceCells'])
                for xy in b['interiorCells']:self.assertTrue(terrain[tuple(xy)]['interior'])
                for xy in b['wallCells']:self.assertFalse(terrain[tuple(xy)]['interior'])

    def test_reciprocal_edges_have_exact_same_indices_and_clear_approaches(self):
        index={c['zoneId']:c for c in self.region['chunks']};shared=0
        for c in index.values():
            occupied={(p['x']+dx,p['y']+dy) for p in c['placements'] if p['solid'] for dx,dy in p['cells']}
            for p in c['portals']:
                self.assertFalse(occupied.intersection(map(tuple,p['approachCells'])))
                if p['neighborZoneId'] not in index:continue
                other=next(q for q in index[p['neighborZoneId']]['portals'] if q['neighborZoneId']==c['zoneId'])
                axis=0 if p['direction'] in ('north','south') else 1
                self.assertEqual([xy[axis] for xy in p['cells']],[xy[axis] for xy in other['cells']]);shared+=1
        self.assertEqual(6,shared)

    def test_walls_have_independent_cell_owners_and_openings_are_native_cells(self):
        for c in self.region['chunks']:
            wall_owners=[p for p in c['placements'] if p['role']=='wall']
            self.assertEqual(sum(len(b['wallCells']) for b in c['buildings']),len(wall_owners))
            self.assertTrue(all(p['cells']==[[0,0]] and p['destructible'] for p in wall_owners))
            blocked={(p['x']+dx,p['y']+dy) for p in c['placements'] if p['solid'] for dx,dy in p['cells']}
            for b in c['buildings']:
                self.assertFalse(blocked.intersection(map(tuple,b['entranceCells'])))
                self.assertFalse(blocked.intersection(map(tuple,b['aisleCells'])))

    def test_every_solid_owner_fits_without_overlap_or_crossing_chunk_boundary(self):
        for c in self.region['chunks']:
            used=set()
            for p in c['placements']:
                cells={(p['x']+dx,p['y']+dy) for dx,dy in p['cells']}
                self.assertTrue(cells);self.assertTrue(all(0<=x<c['width'] and 0<=y<c['height'] for x,y in cells))
                if p['solid']:
                    self.assertFalse(used.intersection(cells));used.update(cells)

    def test_native_rectangular_size_changes_envelope_without_squashing_assets(self):
        data=generate_region(NativeConfig(width=96,height=32,seed=73))
        self.assertEqual((96,32),(data['zoneWidth'],data['zoneHeight']))
        self.assertTrue(validate_region(data));self.assertEqual(.25,data['voxelSize'])
        self.assertTrue(all(p['uniformScale']==1 for c in data['chunks'] for p in c['placements']))

    def test_seed_variation_determinism_and_global_random_independence(self):
        state=random.getstate();a=generate_region();self.assertEqual(state,random.getstate())
        self.assertEqual(self.region,a)
        self.assertNotEqual(a,generate_region(NativeConfig(seed=73)))
        self.assertEqual(a,json.loads(json.dumps(a)))

    def test_water_vegetation_clutter_controls_have_real_counter_cases(self):
        off=generate_region(NativeConfig(water_amount=0,vegetation_amount=0,clutter_amount=0))
        self.assertFalse(any(t['water'] for c in off['chunks'] for t in c['terrain']))
        self.assertFalse(any(p['category']=='vegetation' for c in off['chunks'] for p in c['placements']))
        self.assertFalse(any(p.get('optional') for c in off['chunks'] for p in c['placements']))
        self.assertTrue(any(t['water'] for c in self.region['chunks'] for t in c['terrain']))
        self.assertTrue(any(p['category']=='vegetation' for c in self.region['chunks'] for p in c['placements']))
        self.assertTrue(any(p.get('optional') for c in self.region['chunks'] for p in c['placements']))

    def test_validation_rejects_broken_portal_and_duplicate_owner(self):
        bad=copy.deepcopy(self.region);bad['chunks'][0]['portals'][0]['cells'][0][0]+=1
        with self.assertRaises(ValueError):validate_region(bad)
        bad=copy.deepcopy(self.region);bad['chunks'][0]['placements'].append(copy.deepcopy(bad['chunks'][0]['placements'][0]))
        with self.assertRaises(ValueError):validate_region(bad)

    def test_greedy_lot_dead_end_restarts_rules_without_dropping_native_buildings(self):
        data=generate_region(NativeConfig(seed=23))
        self.assertEqual(6,len(data['chunks'][0]['buildings']))
        self.assertTrue(validate_region(data))

    def test_small_rectangle_reduces_requested_lot_count_without_scaling_assets(self):
        data=generate_region(NativeConfig(width=40,height=20))
        self.assertTrue(validate_region(data))
        self.assertLess(len(data['chunks'][0]['buildings']),len(self.region['chunks'][0]['buildings']))
        self.assertTrue(all(p['uniformScale']==1 for c in data['chunks'] for p in c['placements']))

    def test_bulk_nature_uses_the_whole_reusable_variant_family(self):
        sources={p['sourceAsset'] for c in self.region['chunks'] for p in c['placements']}
        for family in ('rock','scrub','palm'):
            self.assertTrue({family,family+'_1',family+'_2'}<=sources,family)

    def test_trunks_and_rocks_block_but_reeds_scrub_crops_do_not(self):
        for c in self.region['chunks']:
            for p in c['placements']:
                family=p['sourceAsset'].split('_')[0]
                if family in ('tree','palm','rock'):self.assertTrue(p['solid'])
                if family in ('reeds','scrub','crop'):self.assertFalse(p['solid'])

    def test_population_counter_preserves_exact_requested_initial_residents(self):
        for c in self.region['chunks']:
            self.assertEqual(2*len(c['buildings']),sum(p['category']=='npc' for p in c['placements']))
        off=generate_region(NativeConfig(population_per_building=0))
        self.assertFalse(any(p['category']=='npc' for c in off['chunks'] for p in c['placements']))

    def test_internal_routes_are_established_but_external_footpaths_are_narrow(self):
        zones={c['zoneId'] for c in self.region['chunks']}
        for c in self.region['chunks']:
            for index,p in enumerate(c['portals']):
                route=next(path for path in c['paths'] if path['id']==f'entry-{index}')
                self.assertEqual(3 if p['neighborZoneId'] in zones else 1,route['width'])

    def test_trader_merchandise_spills_into_an_accessible_exterior_market(self):
        c=self.region['chunks'][0];trader=next(b for b in c['buildings'] if b['role']=='trader')
        stall=next(p for p in c['placements'] if p['sourceAsset']=='market_stall')
        self.assertEqual(trader['id'],stall['homeId']);self.assertEqual('prop',stall['category'])
        self.assertFalse({tuple(xy) for xy in trader['interiorCells']}.intersection(
            (stall['x']+dx,stall['y']+dy) for dx,dy in stall['cells']))


if __name__=='__main__':unittest.main()
