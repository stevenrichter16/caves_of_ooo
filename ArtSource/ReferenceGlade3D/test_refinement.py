import json,unittest
from pathlib import Path
class RefinementTests(unittest.TestCase):
 def kit(self):return json.loads((Path(__file__).parent/'kit.json').read_text())
 def models(self,f):return [m for m in self.kit()['models'] if m['family']==f]
 def test_chest_and_barrel_are_four_real_variants_each(self):
  self.assertEqual(40,len(self.kit()['models']))
  for f in ['chest','barrel']:self.assertEqual(4,len(self.models(f)))
 def test_ground_has_quiet_surface_and_readable_specks(self):
  for m in self.models('ground'):
   self.assertGreaterEqual(len(m['boxes']),4)
   self.assertGreaterEqual(len(set(b['color'] for b in m['boxes'])),3)
 def test_grass_is_a_compact_tall_cluster_not_three_distant_dots(self):
  for m in self.models('green-grass'):
   self.assertGreaterEqual(len(m['boxes']),21)
   self.assertGreaterEqual(max(b['center']['y']+b['size']['y']/2 for b in m['boxes']),.30)
   self.assertLessEqual(max(b['center']['x']+b['size']['x']/2 for b in m['boxes'])-min(b['center']['x']-b['size']['x']/2 for b in m['boxes']),1.0) # Seventh: broadened after actual native visual rejection.
 def test_masonry_has_thick_staggered_small_bricks(self):
  for f in ['low-wall','lit-wall']:
   for m in self.models(f):
    self.assertGreaterEqual(len(m['boxes']),40)
    self.assertGreaterEqual(max(b['center']['z']+b['size']['z']/2 for b in m['boxes'])-min(b['center']['z']-b['size']['z']/2 for b in m['boxes']),.55)
 def test_ruins_have_fine_fractures_instead_of_six_large_pillars(self):
  for m in self.models('dark-ruin'):
   self.assertGreaterEqual(len(m['boxes']),30)
   self.assertGreaterEqual(sum(b['size']['x']<=.12 for b in m['boxes']),20)
 def test_gravel_has_readable_raised_stones(self):
  for m in self.models('gravel'):
   self.assertGreaterEqual(sum(b['size']['x']>=.06 and b['size']['y']>=.04 for b in m['boxes']),18)
if __name__=='__main__':unittest.main()
