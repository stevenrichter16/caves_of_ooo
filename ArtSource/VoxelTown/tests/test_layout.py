"""Semantic, clearance and dedicated adversarial gates; no Blender required."""
import dataclasses
import json
import math
import random
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from town_generator.config import Config, ARCHETYPES
from town_generator.model import to_dict
from town_generator.layout import generate_town
from town_generator.buildings import building_bounds, entrance_world
from town_generator.paths import point_in_water, route_path


def inside(point, bounds, margin=0):
    return bounds[0]+margin < point[0] < bounds[2]-margin and bounds[1]+margin < point[1] < bounds[3]-margin


def samples(points, spacing=.25):
    for a, b in zip(points, points[1:]):
        steps = max(1, math.ceil(math.dist(a, b)/spacing))
        for j in range(steps+1):
            yield (a[0]+(b[0]-a[0])*j/steps, a[1]+(b[1]-a[1])*j/steps)


class LayoutTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.town = generate_town(Config())

    def test_repeatability_and_different_seed_countercheck(self):
        original = to_dict(self.town)
        self.assertEqual(original, to_dict(generate_town(Config())))
        other = generate_town(Config(seed=42))
        self.assertNotEqual([b.position for b in self.town.buildings], [b.position for b in other.buildings])
        self.assertGreater(sum(a.position != b.position for a,b in zip(self.town.buildings, other.buildings)), 7)

    def test_default_population_roles_and_semantic_rooms(self):
        self.assertEqual(10, len(self.town.buildings))
        self.assertEqual(18, sum(len(b.occupants) for b in self.town.buildings))
        self.assertEqual(set(Config().archetypes), {b.role for b in self.town.buildings})
        for b in self.town.buildings:
            self.assertTrue(b.rooms)
            self.assertTrue(b.prop_profile)
            self.assertTrue(b.entrances)

    def test_world_is_serializable_and_does_not_borrow_random_state(self):
        before = random.getstate()
        generate_town(Config(seed=1234))
        self.assertEqual(before, random.getstate())
        self.assertEqual(41, json.loads(json.dumps(to_dict(self.town)))['config']['seed'])

    def test_building_footprints_and_water_are_disjoint(self):
        for i, a in enumerate(self.town.buildings):
            ax0,ay0,ax1,ay1 = building_bounds(a)
            for b in self.town.buildings[i+1:]:
                bx0,by0,bx1,by1 = building_bounds(b)
                self.assertTrue(ax1+2 <= bx0 or bx1+2 <= ax0 or ay1+2 <= by0 or by1+2 <= ay0)
            for w in self.town.water_bodies:
                for p in ((ax0,ay0),(ax0,ay1),(ax1,ay0),(ax1,ay1),a.position):
                    self.assertFalse(point_in_water(p,w,padding=.25))

    def test_paths_connect_to_real_entrances_and_do_not_cross_walls(self):
        endpoints = [p.points[0] for p in self.town.paths] + [p.points[-1] for p in self.town.paths]
        for b in self.town.buildings:
            self.assertTrue(any(math.dist(entrance_world(b), p) < 1e-6 for p in endpoints), b.id)
        for p in self.town.paths:
            self.assertGreater(len(p.points),1)
            for xy in samples(p.points):
                for b in self.town.buildings:
                    self.assertFalse(inside(xy,building_bounds(b),1e-5),(p.id,b.id,xy))
                for w in self.town.water_bodies:
                    self.assertFalse(point_in_water(xy,w,padding=p.width/2-.05),(p.id,xy))

    def test_major_paths_are_wider_than_footpaths(self):
        major = [p.width for p in self.town.paths if p.purpose in ('entrance-market','market-plaza')]
        secondary = [p.width for p in self.town.paths if p.purpose == 'home-water']
        self.assertTrue(major)
        self.assertTrue(secondary)
        self.assertGreater(min(major), max(secondary))

    def test_semantic_distances_have_real_counterexample(self):
        town = self.town
        traders = [b for b in town.buildings if b.role=='trader']
        homes = [b for b in town.buildings if b.role=='residence']
        self.assertLess(min(math.dist(b.position,town.anchors['market']) for b in traders),
                        min(math.dist(b.position,town.anchors['market']) for b in homes))
        self.assertTrue(town.farms)
        for f in town.farms:
            b = next(b for b in town.buildings if b.id==f.building_id)
            self.assertEqual('farmhouse',b.role)
            self.assertLess(math.dist(f.center,b.position), max(f.width,f.depth)+max(b.width,b.depth))

    def test_world_coordinates_use_voxel_grid_and_quarter_turns(self):
        for b in self.town.buildings:
            for value in (*b.position,b.width,b.depth,b.wall_height):
                self.assertAlmostEqual(value/.25,round(value/.25))
            self.assertAlmostEqual(b.rotation/(math.pi/2),round(b.rotation/(math.pi/2)))

    def test_empty_and_dry_towns_remain_valid(self):
        town=generate_town(Config(building_count=0,population=0,water_amount=0,agriculture_amount=0))
        self.assertEqual([],town.buildings)
        self.assertEqual([],town.water_bodies)
        self.assertEqual([],town.farms)
        self.assertGreater(len(self.town.water_bodies),0)

    def test_all_archetypes_are_optional_not_silent_aliases(self):
        town=generate_town(Config(seed=8,building_count=10,archetypes=ARCHETYPES))
        self.assertEqual(set(ARCHETYPES),{b.role for b in town.buildings})
        self.assertGreater(len({b.prop_profile for b in town.buildings}),7)

    def test_mutating_one_result_does_not_mutate_future_generation(self):
        a=generate_town(Config(seed=91)); b=generate_town(Config(seed=91))
        a.props.append({'kind':'manual'})
        a.buildings[0].occupants.clear()
        self.assertEqual([],b.props)
        self.assertNotEqual(to_dict(a),to_dict(b))

    def test_route_blocks_widened_clearance_not_only_centerline(self):
        from town_generator.model import Building
        def wall(id,x):
            return Building(id,'storehouse',(x,0),0,2,14,3,[(0,-7)],[], 'sandstone','storage',[])
        obstacles=[wall('left',-2),wall('right',2)]
        narrow=route_path((0,-10),(0,10),obstacles,[],30,.5,41,.1)
        wide=route_path((0,-10),(0,10),obstacles,[],30,3,41,.1)
        self.assertLess(max(abs(p[0]) for p in narrow),1)
        self.assertGreater(max(abs(p[0]) for p in wide),4)

    def test_repeated_workshops_form_staggered_lots_not_a_straight_column(self):
        """Visual M1 review found three workshops almost exactly on one X coordinate."""
        workshops=[b for b in self.town.buildings if b.role=='workshop']
        self.assertGreater(max(b.position[0] for b in workshops)-min(b.position[0] for b in workshops),3)
        orderly=generate_town(Config(building_irregularity=0))
        self.assertNotEqual([b.position for b in self.town.buildings],[b.position for b in orderly.buildings])

    def test_oasis_preset_reserves_shore_for_water_dependent_buildings(self):
        data=json.loads((Path(__file__).resolve().parents[1]/'presets/oasis.json').read_text())
        town=generate_town(Config(**data))
        self.assertEqual(data['building_count'],len(town.buildings))
        self.assertIn('bathhouse',{b.role for b in town.buildings})
        self.assertEqual(64,town.config.town_size)


class LayoutAdversarialTests(unittest.TestCase):
    """Dedicated input, isolation, geometry and scale sweep (40 cases)."""


class LayoutHypothesisTests(unittest.TestCase):
    def test_supported_voxel_grid_keeps_fields_and_doors_on_lattice(self):
        """M1 town realization supports the audited .25m physical asset scale."""
        for size in (.25,):
            town=generate_town(Config(seed=19,voxel_size=size))
            for b in town.buildings:
                for value in entrance_world(b):
                    self.assertAlmostEqual(value/size,round(value/size))
            for f in town.farms:
                for value in (*f.center,f.width,f.depth):
                    self.assertAlmostEqual(value/size,round(value/size))

    def test_hypothesis_population_and_dressing_do_not_shuffle_lots(self):
        """Changing clutter or inhabitants must not randomize the underlying architecture."""
        original=generate_town(Config(seed=16))
        changed=generate_town(Config(seed=16,population=230,clutter_amount=0,vegetation_amount=0))
        self.assertEqual([b.position for b in original.buildings],[b.position for b in changed.buildings])
        occupants=[n for b in changed.buildings for n in b.occupants]
        self.assertEqual(230,len(set(occupants)))

    def test_hypothesis_zero_agriculture_releases_attached_fields(self):
        """Zero agriculture must suppress fields despite still permitting farmhouse homes."""
        wet=generate_town(Config(seed=14))
        dry=generate_town(Config(seed=14,agriculture_amount=0))
        self.assertTrue(wet.farms)
        self.assertEqual([],dry.farms)
        self.assertIn('farmhouse',{b.role for b in dry.buildings})

    def test_hypothesis_obstructed_public_endpoint_fails_closed(self):
        """A generic routing caller must not get a route teleported out of a wall."""
        town=generate_town(Config(seed=25))
        building=town.buildings[0]
        with self.assertRaisesRegex(ValueError,'endpoint'):
            route_path(building.position,town.anchors['plaza'],town.buildings,town.water_bodies,80,1,25,.4)
        clear=route_path(town.anchors['market'],town.anchors['plaza'],town.buildings,town.water_bodies,80,1,25,.4)
        self.assertGreater(len(clear),1)

    def test_hypothesis_fields_never_cross_unrelated_buildings(self):
        """Attached fields can escape the collision check applied only to house footprints."""
        from town_generator.buildings import farm_bounds,bounds_overlap
        for seed in (3,6,9,12):
            town=generate_town(Config(seed=seed))
            for f in town.farms:
                for b in town.buildings:
                    self.assertFalse(bounds_overlap(farm_bounds(f),building_bounds(b)),(f.id,b.id))

    def test_hypothesis_ellipse_crossing_without_corner_containment_is_rejected(self):
        """A long building edge can cross a water body with all four corners dry."""
        from town_generator.model import WaterBody
        from town_generator.buildings import rect_hits_water
        water=WaterBody('test',(0,0),(3,3))
        self.assertTrue(rect_hits_water((-10,-1,10,1),water))
        self.assertFalse(rect_hits_water((-10,4,10,5),water))


def invalid_case(field,value):
    def test(self):
        with self.assertRaises((ValueError,TypeError)):
            generate_town(dataclasses.replace(Config(),**{field:value}))
    return test


for index,(field,value) in enumerate([
    ('seed',True),('seed',1.5),('town_size',float('nan')),('town_size',float('inf')),
    ('town_size',0),('town_size',-80),('building_count',-1),('building_count',1.5),
    ('population',-1),('population',2.5),('density',-.01),('density',1.01),
    ('water_amount',-.01),('water_amount',1.01),('vegetation_amount',-.01),('vegetation_amount',1.01),
    ('agriculture_amount',-.01),('agriculture_amount',1.01),('ruin_amount',-.01),('wealth',1.01),
    ('town_age',float('nan')),('clutter_amount',1.01),('building_irregularity',-.01),
    ('path_irregularity',float('inf')),('voxel_size',0),('voxel_size',.3),
    ('archetypes',()),('archetypes',('missing-role',)),('building_count',10000),('population',True),
]):
    setattr(LayoutAdversarialTests,f'test_invalid_{index:02d}_{field}',invalid_case(field,value))


def invalid_width_case(width):
    def test(self):
        with self.assertRaisesRegex(ValueError,'width'):
            route_path((-5,0),(5,0),[],[],30,width,41,.4)
    return test


for index,width in enumerate((0,-1,float('nan'),float('inf'))):
    setattr(LayoutAdversarialTests,f'test_invalid_path_width_{index}',invalid_width_case(width))


def valid_case(config):
    def test(self):
        town=generate_town(config)
        self.assertEqual(config.building_count,len(town.buildings))
        self.assertEqual(config.population,sum(len(b.occupants) for b in town.buildings))
        ids=[b.id for b in town.buildings]+[p.id for p in town.paths]
        self.assertEqual(len(ids),len(set(ids)))
        for b in town.buildings:
            bounds=building_bounds(b)
            self.assertGreaterEqual(min(bounds[:2]),-config.town_size/2+1)
            self.assertLessEqual(max(bounds[2:]),config.town_size/2-1)
        for p in town.paths:
            for xy in samples(p.points,spacing=.6):
                self.assertFalse(any(inside(xy,building_bounds(b),1e-5) for b in town.buildings),(p.id,xy))
                self.assertFalse(any(point_in_water(xy,w,padding=p.width/2-.05) for w in town.water_bodies),(p.id,xy))
    return test


for index,config in enumerate([
    Config(seed=0),Config(seed=-918),Config(seed=2**63-1),Config(seed=77,density=0),
    Config(seed=17,density=1),Config(seed=99,water_amount=1),Config(seed=6,water_amount=0),
    Config(seed=88,town_size=120,building_count=30,population=93),
    Config(seed=11,town_size=48,building_count=7,population=11),
    Config(seed=4,voxel_size=.25,building_irregularity=1,path_irregularity=1),
]):
    setattr(LayoutAdversarialTests,f'test_layout_{index:02d}_seed_{config.seed}',valid_case(config))


if __name__=='__main__':
    unittest.main(verbosity=2)
