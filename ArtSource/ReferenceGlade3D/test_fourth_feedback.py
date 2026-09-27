import json,unittest
from pathlib import Path
class FourthFeedbackTests(unittest.TestCase):
 def models(self,f):return [m for m in json.loads((Path(__file__).parent/'kit.json').read_text())['models'] if m['family']==f]
 def test_reeds_remain_vertical_fingers_without_horizontal_canopies(self):
  for m in self.models('pale-reeds'):
   b=m['boxes'];vertical=sum(q['size']['x']*q['size']['y']*q['size']['z'] for q in b if q['size']['y']>=max(q['size']['x'],q['size']['z'])*1.25)
   total=sum(q['size']['x']*q['size']['y']*q['size']['z'] for q in b)
   self.assertGreaterEqual(vertical/total,.83,m['id'])
   # Fifth native review rejected centered cones: flat caps are now part of
   # the silhouette. Retain narrow independent stems and dominant vertical mass.
   self.assertTrue(all(max(q['size']['x'],q['size']['z'])<.17 for q in b))
 def test_grass_has_chunky_bright_blades_and_small_shaded_bases(self):
  for m in self.models('green-grass'):
   b=m['boxes'];stems=[q for q in b if q['size']['y']>.23]
   self.assertEqual(7,len(stems))
   for q in stems:self.assertGreaterEqual(min(q['size']['x'],q['size']['z']),.085)
   self.assertIn(7,{q['color'] for q in b});self.assertIn(9,{q['color'] for q in b})
   self.assertEqual(21,len(b))
   self.assertGreaterEqual(sum(min(q['size']['x'],q['size']['z'])>=.085 for q in b),14)
 def test_floor_pebbles_have_visible_square_sides_without_panels(self):
  for m in self.models('ground'):
   pebbles=[q for q in m['boxes'] if q['size']['x']<.5]
   self.assertIn(len(pebbles),[3,4,5])
   for q in pebbles:
    self.assertGreaterEqual(min(q['size']['x'],q['size']['z']),.09)
    self.assertLessEqual(max(q['size']['x'],q['size']['z']),.115)
    self.assertGreaterEqual(q['size']['y'],.055)
   self.assertEqual(1,sum(q['size']['x']>.5 for q in m['boxes']))
 def test_gravel_is_raised_square_fragments_instead_of_fine_dots(self):
  for m in self.models('gravel'):
   self.assertGreaterEqual(sum(min(q['size']['x'],q['size']['z'])>=.09 and q['size']['y']>=.055 for q in m['boxes']),18)
 def test_quiet_palette_and_prop_hierarchy_remain(self):
  d=json.loads((Path(__file__).parent/'kit.json').read_text());self.assertEqual('#082C28',d['palette'][0]);self.assertEqual(24,len(d['palette']))
  for f in ['chest','barrel','mushroom-ring']:
   self.assertEqual(4,len(self.models(f)))
   for m in self.models(f):self.assertLess(max(q['center']['y']+q['size']['y']/2 for q in m['boxes']),.6)
if __name__=='__main__':unittest.main()
