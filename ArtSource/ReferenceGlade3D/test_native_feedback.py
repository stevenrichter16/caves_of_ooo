import json,unittest
from pathlib import Path
class NativeFeedbackTests(unittest.TestCase):
 def kit(self):return json.loads((Path(__file__).parent/'kit.json').read_text())
 def models(self,f):return [m for m in self.kit()['models'] if m['family']==f]
 def test_ground_has_one_quiet_surface_not_nine_contrasting_panels(self):
  for m in self.models('ground'):
   surfaces=[b for b in m['boxes'] if b['size']['x']>.2 and b['size']['z']>.2]
   self.assertEqual(1,len(surfaces));self.assertGreaterEqual(len(m['boxes'])-1,3)
 def test_pale_reed_stems_are_thick_pale_branches_not_dark_twigs(self):
  for m in self.models('pale-reeds'):
   stems=[b for b in m['boxes'] if b['size']['y']>.3]
   self.assertEqual(7,len(stems))
   for b in stems:self.assertGreaterEqual(b['size']['x'],.11);self.assertIn(b['color'],[4,5])
 def test_grass_has_seven_upright_fingers_without_a_wide_mat_base(self):
  for m in self.models('green-grass'):
   stems=[b for b in m['boxes'] if b['size']['y']>.23]
   self.assertEqual(7,len(stems));self.assertTrue(all(b['color'] in [8,9] for b in stems))
   self.assertGreaterEqual(max(b['center']['y']+b['size']['y']/2 for b in m['boxes']),.40)
   self.assertFalse(any(b['size']['x']>.25 and b['size']['z']>.2 for b in m['boxes']))
 def test_stone_wall_has_a_broad_dimensional_cross_section(self):
  for m in self.models('low-wall'):
   self.assertGreaterEqual(max(b['center']['z']+b['size']['z']/2 for b in m['boxes'])-min(b['center']['z']-b['size']['z']/2 for b in m['boxes']),.72)
   self.assertGreaterEqual(max(b['center']['y']+b['size']['y']/2 for b in m['boxes']),.60)
 def test_real_mushroom_rings_have_four_small_multicap_models(self):
  self.assertEqual(4,len(self.models('mushroom-ring')))
  for m in self.models('mushroom-ring'):
   self.assertGreaterEqual(len(m['boxes']),24)
   self.assertLessEqual(max(b['center']['y']+b['size']['y']/2 for b in m['boxes']),.5)
if __name__=='__main__':unittest.main()
