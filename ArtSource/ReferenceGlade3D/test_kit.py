import json,unittest
from pathlib import Path
ROOT=Path(__file__).parent
FAMILIES={'ground','pale-reeds','green-grass','dark-ruin','low-wall','lit-wall','gravel','chest','barrel','mushroom-ring'}
class GladeKitTests(unittest.TestCase):
 def read(self):return json.loads((ROOT/'kit.json').read_text())
 def test_exact_ten_families_four_variants(self):
  d=self.read();self.assertEqual(40,len(d['models']));self.assertEqual({'reference-glade-'+f+'-'+str(v) for f in FAMILIES for v in range(4)},{m['id'] for m in d['models']})
 def test_boxes_stay_inside_native_cell_and_have_volume(self):
  for m in self.read()['models']:
   self.assertTrue(m['boxes'],m['id'])
   for b in m['boxes']:
    self.assertTrue(all(b['size'][axis]>0 for axis in 'xyz'))
    for axis in 'xz':self.assertLessEqual(abs(b['center'][axis])+b['size'][axis]*.5,.501,m['id'])
 def test_palette_is_bounded_and_role_colors_are_distinct(self):
  d=self.read();self.assertEqual(24,len(d['palette']));self.assertEqual(24,len(set(d['palette'])))
  for m in d['models']:
   for b in m['boxes']:self.assertIn(b['color'],range(24))
 def test_low_plant_and_wall_height_hierarchy(self):
  d=self.read();ranges={'ground':(.005,.1),'pale-reeds':(.4,.95),'green-grass':(.12,.5),'dark-ruin':(.35,.9),'low-wall':(.35,.75),'lit-wall':(.35,.75),'gravel':(.02,.16),'chest':(.3,.7),'barrel':(.3,.7),'mushroom-ring':(.25,.5)}
  for m in d['models']:
   y=[b['center']['y']+s*b['size']['y']*.5 for b in m['boxes'] for s in [-1,1]];height=max(y)-min(y);a,b=ranges[m['family']];self.assertGreaterEqual(height,a);self.assertLessEqual(height,b)
 def test_variants_have_real_geometric_changes(self):
  d=self.read()
  for f in FAMILIES:self.assertEqual(4,len({json.dumps(m['boxes'],sort_keys=True) for m in d['models'] if m['family']==f}))
if __name__=='__main__':unittest.main()
