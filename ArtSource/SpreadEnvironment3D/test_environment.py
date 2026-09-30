import json,math,unittest
from pathlib import Path
ROOT=Path(__file__).parent
FAMILIES=['paving','road','tree','hedge','vine-wall','stubble','grain','flowers','dry-brush','rock','stoneburr','frost-lichen','candy-carrot-crop','emberwheat-crop']
def load():return json.loads((ROOT/'kit.json').read_text())
def models(f):return [m for m in load()['models']if m['family']==f]
def extents(m,axis):return (min(b['center'][axis]-b['size'][axis]/2 for b in m['boxes']),max(b['center'][axis]+b['size'][axis]/2 for b in m['boxes']))
class EnvironmentSource(unittest.TestCase):
 def test_exact_fifty_six_forms_and_four_variants(self):
  d=load();self.assertEqual(56,len(d['models']));self.assertEqual(set(FAMILIES),{m['family']for m in d['models']});self.assertEqual(56,len({m['id']for m in d['models']}))
  for f in FAMILIES:self.assertEqual({0,1,2,3},{m['variant']for m in models(f)})
 def test_approved_palette_and_finite_bounded_geometry(self):
  d=load();self.assertEqual(24,len(d['palette']));self.assertEqual('#082C28',d['palette'][0]);self.assertEqual('#D2D3B4',d['palette'][17])
  for m in d['models']:
   self.assertTrue(3<=len(m['boxes'])<=100)
   for b in m['boxes']:
    self.assertTrue(0<=b['color']<24)
    for a in 'xyz':self.assertTrue(math.isfinite(b['center'][a]));self.assertTrue(.003<=b['size'][a]<=1)
   for a in 'xz':self.assertGreaterEqual(extents(m,a)[0],-.501);self.assertLessEqual(extents(m,a)[1],.501)
 def test_tree_separate_trunk_branch_contacts_not_solid_canopy(self):
  for m in models('tree'):
   self.assertTrue(1.25<extents(m,'y')[1]<1.85)
   bases=[b for b in m['boxes']if b['center']['y']-b['size']['y']/2<=.005]
   self.assertTrue(bases);self.assertTrue(all(b['size']['x']<.3 and b['size']['z']<.3 for b in bases))
   self.assertTrue(any(abs(b['center']['x'])>.28 and b['center']['y']>.6 for b in m['boxes']))
   self.assertFalse(any(b['size']['x']>.35 and b['size']['z']>.35 for b in m['boxes']))
 def test_ground_families_flat_with_full_cell_foundation(self):
  for f in ['paving','road']:
   for m in models(f):
    self.assertEqual('ground',m['kind']);self.assertLess(extents(m,'y')[1],.055)
    self.assertTrue(any(b['size']['x']==1 and b['size']['z']==1 for b in m['boxes']))
 def test_low_hedge_and_structural_vines_are_distinct(self):
  for m in models('hedge'):self.assertTrue(.45<extents(m,'y')[1]<.75)
  for m in models('vine-wall'):self.assertTrue(.8<extents(m,'y')[1]<1.15)
  self.assertNotEqual(models('hedge')[0]['boxes'],models('vine-wall')[0]['boxes'])
 def test_harvest_states_have_distinct_height_and_grain_heads(self):
  for m in models('stubble'):self.assertLess(extents(m,'y')[1],.16)
  for m in models('grain'):self.assertGreater(extents(m,'y')[1],.4);self.assertTrue(any(b['color']==22 for b in m['boxes']))
 def test_flower_species_stays_small_pale_and_green(self):
  for m in models('flowers'):
   self.assertTrue(.2<extents(m,'y')[1]<.45);self.assertTrue(any(b['color']==17 for b in m['boxes']));self.assertTrue(any(b['color']in(7,8,9)for b in m['boxes']))
 def test_variants_change_shape_without_unbounded_palette(self):
  for f in FAMILIES:self.assertEqual(4,len({json.dumps(m['boxes'],sort_keys=True)for m in models(f)}))
if __name__=='__main__':unittest.main()
