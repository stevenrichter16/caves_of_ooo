"""Independent dressing hypotheses using actual asset voxel occupancy.

These are acceptance tests, not intentional xfails. The initial receipt records
which hypotheses confirmed faults before the scene adapter/fixes were written.
"""
import dataclasses
import functools
import math
import random
import sys
import unittest
from pathlib import Path

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from town_generator.assets import build_recipes
from town_generator.config import Config,ARCHETYPES
from town_generator.layout import generate_town
from town_generator.model import Town,Path as TownPath,WaterBody,to_dict
from town_generator.buildings import building_bounds,local_to_world
from town_generator.terrain import sample_fields,ground_cells,in_water
from town_generator.spatial import Clearance
from town_generator.vegetation import plan_vegetation
from town_generator.props import plan_props
from town_generator.population import plan_population


@functools.lru_cache(None)
def recipes(q=.25):
    return build_recipes(q)


def occupied(row,q=.25):
    """Exact quarter-turn transformed solid-cell positions, independent of planners."""
    tx,ty,tz=[round(v/q) for v in row['position']]
    turn=round(row.get('rotation',0)/(math.pi/2))%4
    result=set()
    for x,y,z in recipes(q)[row['asset']].cells:
        for _ in range(turn): x,y=-y-1,x
        result.add((x+tx,y+ty,z+tz))
    return result


def collision_pairs(rows,q=.25):
    index={};collisions=set()
    for row in rows:
        for cell in occupied(row,q):
            for previous in index.get(cell,()):
                collisions.add((previous,row['id']))
            index.setdefault(cell,[]).append(row['id'])
    return sorted(collisions)


def footprint(row,q=.25):
    cells=occupied(row,q)
    return (min(x for x,y,z in cells)*q,min(y for x,y,z in cells)*q,
            (max(x for x,y,z in cells)+1)*q,(max(y for x,y,z in cells)+1)*q)


class DressingHypothesisTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.town=generate_town(Config())
        cls.props=plan_props(cls.town)
        cls.people=plan_population(cls.town)
        cls.vegetation=plan_vegetation(cls.town)

    def test_props_do_not_interpenetrate_actual_solid_voxels(self):
        """Center-only placement may permit furniture/lantern/clutter intersections."""
        found=collision_pairs(self.props)
        self.assertFalse(found,found[:12])

    def test_npcs_do_not_interpenetrate_actual_body_voxels(self):
        """Three people spaced .65m apart may overlap their 1.25m-wide arms."""
        found=collision_pairs(self.people)
        self.assertFalse(found,found[:12])

    def test_npcs_do_not_materialize_inside_furniture(self):
        """Independent furniture and population planners need shared reserved space."""
        prop_ids={p['id'] for p in self.props};person_ids={p['id'] for p in self.people}
        found=[(a,b) for a,b in collision_pairs(self.props+self.people) if a in prop_ids and b in person_ids]
        self.assertFalse(found,found[:12])

    def test_irrigation_and_crops_do_not_share_solid_voxels(self):
        """Farm centerline equipment can cut into the first crop row."""
        crops=[v for v in self.vegetation if v['role']=='cultivated']
        self.assertTrue(crops)
        self.assertTrue([p for p in self.props if p['asset']=='irrigation'])
        crop_ids={v['id'] for v in crops};prop_ids={p['id'] for p in self.props}
        found=[(a,b) for a,b in collision_pairs(self.props+crops) if a in prop_ids and b in crop_ids]
        self.assertFalse(found,found[:12])

    def test_interior_entrance_aisles_remain_clear_of_solid_props(self):
        """A readable door must retain a one-meter central approach into the room."""
        violations=[]
        for b in self.town.buildings:
            cells=set().union(*(occupied(p) for p in self.props if p['owner']==b.id and p['role']=='functional'))
            for ix in (-2,-1,0,1):
                for iy in range(math.ceil((-b.depth/2+.5)/.25),0):
                    x,y=local_to_world(b,((ix+.5)*.25,(iy+.5)*.25))
                    cell=(math.floor(x/.25),math.floor(y/.25),2)
                    if cell in cells: violations.append((b.id,cell))
        self.assertFalse(violations,violations[:12])

    def test_overflow_people_stay_on_land_and_inside_map(self):
        """Exterior crowd rows must respect rotated doors, shore, and finite terrain."""
        town=generate_town(Config(seed=41,population=180))
        violations=[]
        for p in plan_population(town):
            x,y,_=p['position'];r=footprint(p);half=town.config.town_size/2
            if min(r[:2])<-half or max(r[2:])>half or in_water(x,y,town): violations.append((p['id'],p['position']))
        self.assertFalse(violations,violations[:12])

    def test_overflow_people_do_not_enter_unrelated_buildings(self):
        """A world-negative-Y overflow formula can put rotated-house crowds through walls."""
        town=generate_town(Config(seed=41,population=90))
        violations=[]
        for p in plan_population(town):
            x,y,_=p['position']
            for b in town.buildings:
                if b.id==p['home']:continue
                a,c,d,e=building_bounds(b)
                if a<x<d and c<y<e:violations.append((p['id'],b.id,p['position']))
        self.assertFalse(violations,violations[:12])

    def test_dry_world_has_no_reeds_or_cultivated_plants(self):
        """No natural water/agriculture must suppress wetland and crop families."""
        town=generate_town(Config(seed=17,water_amount=0,agriculture_amount=0))
        plants=plan_vegetation(town)
        self.assertFalse([p for p in plants if p['role'] in ('wetland','cultivated')])
        self.assertTrue([p for p in self.vegetation if p['role']=='wetland'])

    def test_zero_vegetation_removes_natural_plants_but_keeps_farmed_crops(self):
        """Natural vegetation and deliberate agriculture have independent controls."""
        town=generate_town(Config(seed=17,vegetation_amount=0))
        plants=plan_vegetation(town)
        self.assertFalse([p for p in plants if p['role'] in ('wetland','understory','oasis-canopy')])
        self.assertTrue([p for p in plants if p['role']=='cultivated'])

    def test_zero_ruins_removes_only_ancient_remnants(self):
        town=generate_town(Config(seed=17,ruin_amount=0))
        self.assertFalse([p for p in plan_vegetation(town) if p['role']=='ancient-remnant'])
        town=generate_town(Config(seed=17,ruin_amount=1))
        self.assertTrue([p for p in plan_vegetation(town) if p['role']=='ancient-remnant'])

    def test_zero_clutter_preserves_functional_furniture(self):
        town=generate_town(Config(seed=17,clutter_amount=0))
        props=plan_props(town)
        self.assertFalse([p for p in props if p['role']=='activity-clutter'])
        self.assertTrue([p for p in props if p['asset']=='bed'])
        town=generate_town(Config(seed=17,clutter_amount=1))
        self.assertTrue([p for p in plan_props(town) if p['role']=='activity-clutter'])

    def test_stages_are_repeatable_independent_and_nonmutating(self):
        before=to_dict(self.town);rng_before=random.getstate()
        for planner in (plan_props,plan_population,plan_vegetation):
            first=planner(self.town);random.random();second=planner(self.town)
            self.assertEqual(first,second)
        random.setstate(rng_before)
        for planner in (plan_props,plan_population,plan_vegetation):planner(self.town)
        self.assertEqual(rng_before,random.getstate())
        self.assertEqual(before,to_dict(self.town))

    def test_exact_human_population_and_separate_livestock(self):
        town=generate_town(Config(seed=8,archetypes=ARCHETYPES,population=36))
        people=plan_population(town)
        humans=[p for p in people if p['role']!='livestock']
        animals=[p for p in people if p['role']=='livestock']
        self.assertEqual(36,len(humans));self.assertEqual(36,len({p['id'] for p in humans}))
        self.assertEqual(2,len(animals))

    def test_unsupported_town_voxel_scales_reject_before_invalid_furniture(self):
        """M1 explicitly supports .25m; earlier coarse-scale interpenetration was RED."""
        for size in (.125,.5,1):
            with self.assertRaisesRegex(ValueError,'voxel_size'):
                generate_town(Config(seed=19,voxel_size=size))
        town=generate_town(Config(seed=19))
        bounds={b.id:building_bounds(b) for b in town.buildings}
        for p in plan_props(town):
            if p['role']!='functional':continue
            a,b,c,d=footprint(p);x0,y0,x1,y1=bounds[p['owner']]
            self.assertGreaterEqual(a,x0);self.assertGreaterEqual(b,y0)
            self.assertLessEqual(c,x1);self.assertLessEqual(d,y1)

    def test_fractional_terrain_size_does_not_protrude_outside_map(self):
        """Ceil(tile-count) must clip the final tile rather than expanding map bounds."""
        # The lower-level terrain helper also handles arbitrary finite extents;
        # integrated towns separately require boundaries on the voxel lattice.
        town=generate_town(Config(seed=3))
        town.config=dataclasses.replace(town.config,town_size=80.25)
        half=town.config.town_size/2
        for cell in ground_cells(town):
            x,y,z=cell['center'];sx,sy,sz=cell['size']
            self.assertGreaterEqual(min(x-sx/2,y-sy/2),-half-1e-6)
            self.assertLessEqual(max(x+sx/2,y+sy/2),half+1e-6)

    def test_natural_props_use_full_shape_at_the_water_edge(self):
        """A center just outside water is insufficient for a one-meter-radius object."""
        town=Town(Config(building_count=0,population=0),[],[WaterBody('water',(0,0),(3,3))],[],[],[],{})
        clear=Clearance(town)
        self.assertFalse(clear.free(3.25,0,radius=1))
        self.assertTrue(clear.free(5,0,radius=1))

    def test_spatial_bins_honor_queries_larger_than_two_meters(self):
        """Clearance pad is a public argument; pre-indexing must not impose hidden maxima."""
        town=Town(Config(building_count=0,population=0),[],[],[],[TownPath('route',[(0,0),(0,3)],1,'test')],[],{})
        clear=Clearance(town)
        self.assertTrue(clear.near_path(5,1,pad=6))
        self.assertFalse(clear.near_path(5,1,pad=1))

    def test_field_values_are_bounded_and_gradual(self):
        near=sample_fields(-13,0,self.town);far=sample_fields(35,0,self.town)
        self.assertGreater(near['water_influence'],far['water_influence'])
        self.assertLess(near['desert_influence'],far['desert_influence'])
        for x in range(-40,41,4):
            for y in range(-40,41,4):
                values=sample_fields(x,y,self.town)
                self.assertTrue(all(0<=v<=1 and math.isfinite(v) for v in values.values()))


class DressingAdversarialTests(unittest.TestCase):
    """Boundary and cross-stage regression pins, separate from hypotheses above."""
    def test_a_third_of_residents_prefer_clear_outdoor_activity_spaces(self):
        for config in (Config(seed=41),Config(seed=8,archetypes=ARCHETYPES,population=24)):
            town=generate_town(config);people=plan_population(town);clear=Clearance(town)
            humans=[p for p in people if p['role']!='livestock']
            outside=[p for p in humans if not clear.in_building(*p['position'][:2])]
            self.assertGreaterEqual(len(outside),config.population//3)
            self.assertTrue([p for p in humans if clear.in_building(*p['position'][:2])])
            for p in outside:
                x,y=p['position'][:2]
                self.assertFalse(clear.near_path(x,y,.5),p['id'])
                self.assertIn('activity',p)
            self.assertEqual(config.population,len(humans))

    def test_vegetation_reserves_outdoor_residents_consistently(self):
        town=generate_town(Config(seed=41));people=plan_population(town);plants=plan_vegetation(town)
        outdoor=[p for p in people if p.get('activity')=='outdoor']
        self.assertEqual(6,len(outdoor))
        self.assertFalse(collision_pairs(outdoor+plants))
        again=plan_population(town)
        self.assertEqual(people,again)

    def test_outdoor_assignment_varies_outfits_and_keeps_livestock_in_pen(self):
        town=generate_town(Config());people=plan_population(town)
        self.assertEqual({'npc','npc_1','npc_2'},{p['asset'] for p in people if p['activity']=='outdoor'})
        town=generate_town(Config(seed=8,archetypes=ARCHETYPES,population=24))
        animals=[p for p in plan_population(town) if p['role']=='livestock']
        self.assertEqual(2,len(animals))
        for animal in animals:
            b=next(b for b in town.buildings if b.id==animal['home'])
            self.assertEqual('animal_enclosure',b.role)
            self.assertEqual('enclosure',animal['activity'])
            x0,y0,x1,y1=building_bounds(b);a,c,d,e=footprint(animal)
            self.assertTrue(x0<=a and y0<=c and d<=x1 and e<=y1)

    def test_outdoor_preference_falls_back_inside_without_eligible_routes(self):
        town=generate_town(Config());normal=plan_population(town)
        self.assertEqual(6,sum(p['activity']=='outdoor' for p in normal))
        town.paths=[]
        fallback=plan_population(town)
        self.assertEqual(18,len(fallback))
        self.assertTrue(all(p['activity']=='indoors' for p in fallback))

    def test_large_town_traders_can_share_the_district_market_outdoors(self):
        town=generate_town(Config(seed=2,town_size=160,building_count=50,population=200))
        props=plan_props(town)
        for b in town.buildings:
            if b.role=='trader':self.assertTrue(any(p['owner']==b.id and p['role']=='trading-yard' for p in props),b.id)

    def test_civic_corner_piers_have_their_actual_wider_clearance(self):
        from town_generator.spatial import inside_room
        from town_generator.model import Building
        for role in ('shrine','meeting_house'):
            building=Building('test',role,(0,0),0,7,7,3,[(0,-3.5)],[],'sandstone','test',[])
            lantern=dict(id='corner',asset='lantern',position=(2.75,2.75,.25),rotation=0,scale=(1,1,1))
            self.assertFalse(inside_room(lantern,building))
            self.assertTrue(inside_room(lantern,dataclasses.replace(building,role='residence')))

    def test_traders_find_merchandise_yards_when_side_wings_are_obstructed(self):
        for config in (Config(seed=3),Config(seed=9,archetypes=ARCHETYPES),Config(seed=10),Config(seed=13,clutter_amount=0)):
            town=generate_town(config);props=plan_props(town)
            for building in town.buildings:
                if building.role=='trader':
                    self.assertTrue(any(p['owner']==building.id and p['role']=='trading-yard' and p['asset']=='market_stall' for p in props),building.id)

    def test_farm_boundaries_are_fenced_with_house_facing_access(self):
        town=generate_town(Config());props=plan_props(town);plants=plan_vegetation(town)
        for farm in town.farms:
            b=next(b for b in town.buildings if b.id==farm.building_id)
            fences=[p for p in props if p['owner']==b.id and p['role']=='farm-boundary']
            self.assertGreaterEqual(len(fences),8)
            dx,dy=b.position[0]-farm.center[0],b.position[1]-farm.center[1]
            horizontal=abs(dx)>abs(dy)
            gate=(farm.center[0]+math.copysign(farm.width/2,dx),farm.center[1]) if horizontal else (farm.center[0],farm.center[1]+math.copysign(farm.depth/2,dy))
            solid=set().union(*(occupied(p) for p in props+plants))
            # A full meter of gate width has no solid voxel in the first meter inward.
            for along in (-.375,-.125,.125,.375):
                for inward in (.125,.375,.625,.875):
                    x=gate[0]-math.copysign(inward,dx) if horizontal else gate[0]+along
                    y=gate[1]+along if horizontal else gate[1]-math.copysign(inward,dy)
                    self.assertFalse(any((math.floor(x/.25),math.floor(y/.25),z) in solid for z in range(0,8)),(farm.id,x,y))

    def test_canopy_is_planned_for_inhabited_shade_not_only_random_understory(self):
        town=generate_town(Config(seed=41));trees=[v for v in plan_vegetation(town) if v['role']=='oasis-canopy']
        self.assertGreaterEqual(len(trees),15)
        near_homes=[t for t in trees if min(math.dist(t['position'][:2],b.position) for b in town.buildings)<10]
        self.assertGreaterEqual(len(near_homes),8)
        dry=generate_town(Config(seed=41,vegetation_amount=0))
        self.assertFalse([v for v in plan_vegetation(dry) if v['role']=='oasis-canopy'])

    def test_optional_clutter_cannot_take_the_primary_well_footprint(self):
        for config in (Config(seed=1),Config(seed=0,archetypes=ARCHETYPES)):
            town=generate_town(config);props=plan_props(town)
            wells=[p for p in props if p['role']=='water-source']
            self.assertEqual(1,len(wells))
            self.assertEqual(town.anchors['water'],wells[0]['position'][:2])
            self.assertFalse(collision_pairs(props))

    def test_zero_wealth_still_generates_buildable_accessible_shells(self):
        from town_generator.architecture import build_structure
        town=generate_town(Config(seed=0,wealth=0,archetypes=ARCHETYPES))
        for building in town.buildings:
            self.assertGreater(build_structure(building).cell_count,0)

    def test_prepopulated_town_lists_do_not_change_independent_planners(self):
        town=generate_town(Config(seed=71))
        expected=(plan_props(town),plan_population(town),plan_vegetation(town))
        town.props=[{'asset':'manually-authored-unknown','position':(0,0,0)}]
        town.npcs=[{'asset':'another-caller-owned-object','position':(0,0,0)}]
        actual=(plan_props(town),plan_population(town),plan_vegetation(town))
        self.assertEqual(expected,actual)
        self.assertEqual('manually-authored-unknown',town.props[0]['asset'])

    def test_furniture_and_population_do_not_intersect_real_structure_voxels(self):
        from town_generator.architecture import build_structure
        town=generate_town(Config(seed=41));q=town.config.voxel_size;walls=set()
        for building in town.buildings:
            turn=round(building.rotation/(math.pi/2))%4
            tx,ty=(round(v/q) for v in building.position)
            for x,y,z in build_structure(building,q).cells:
                for _ in range(turn):x,y=-y-1,x
                walls.add((x+tx,y+ty,z))
        failures=[]
        for row in plan_props(town)+plan_population(town):
            if walls.intersection(occupied(row,q)):failures.append((row['id'],row['asset']))
        self.assertFalse(failures,failures)

    def test_integrated_town_extent_must_align_with_centered_voxel_grid(self):
        with self.assertRaisesRegex(ValueError,'multiple'):
            generate_town(Config(town_size=80.25))
        self.assertEqual(80.5,generate_town(Config(town_size=80.5)).config.town_size)

    def test_zero_length_path_has_finite_clearance(self):
        town=Town(Config(building_count=0,population=0),[],[],[],[TownPath('route',[(0,0),(0,0)],1,'test')],[],{})
        clear=Clearance(town)
        self.assertTrue(clear.near_path(.25,.25,pad=.5))
        self.assertFalse(clear.near_path(2,2,pad=.5))

    def test_ignore_owner_does_not_ignore_its_neighbors(self):
        town=generate_town(Config(seed=2));clear=Clearance(town)
        a,b=town.buildings[:2]
        self.assertFalse(clear.in_building(*a.position,ignore=a.id))
        self.assertTrue(clear.in_building(*b.position,ignore=a.id))

    def test_allow_farm_releases_only_farm_reservation(self):
        from town_generator.model import FarmPlot
        farm=FarmPlot('farm','home',(10,10),4,4)
        town=Town(Config(building_count=0,population=0),[],[],[],[],[farm],{})
        clear=Clearance(town)
        self.assertFalse(clear.free(10,10))
        self.assertTrue(clear.free(10,10,allow_farm=True))

    def test_actual_voxel_collision_probe_has_disjoint_countercheck(self):
        a=dict(id='a',asset='crate',position=(0,0,0),rotation=0,scale=(1,1,1))
        b=dict(a,id='b')
        self.assertEqual([('a','b')],collision_pairs([a,b]))
        b['position']=(1,0,0)
        self.assertEqual([],collision_pairs([a,b]))


def semantic_case(config):
    def test(self):
        town=generate_town(config)
        before=to_dict(town)
        rows=plan_props(town)+plan_population(town)+plan_vegetation(town)
        ids=[r['id'] for r in rows]
        self.assertEqual(len(ids),len(set(ids)))
        self.assertTrue(all(r['asset'] in recipes(config.voxel_size) for r in rows))
        self.assertTrue(all(len(r['position'])==3 and all(math.isfinite(v) for v in r['position']) for r in rows))
        self.assertEqual(before,to_dict(town))
    return test


for index,config in enumerate([
    Config(seed=0),Config(seed=-4),Config(seed=2**63-1),Config(seed=19,voxel_size=.25),
    Config(seed=23,town_size=120,building_count=30,population=100),
    Config(seed=12,archetypes=ARCHETYPES),Config(seed=11,population=0),
    Config(seed=17,building_count=0,population=0,water_amount=0,vegetation_amount=0,agriculture_amount=0,ruin_amount=0),
]):
    setattr(DressingAdversarialTests,f'test_semantic_boundaries_{index:02d}',semantic_case(config))


if __name__=='__main__':unittest.main(verbosity=2)
