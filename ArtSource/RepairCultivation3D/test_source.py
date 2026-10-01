import json, unittest
from pathlib import Path
class SourceTests(unittest.TestCase):
 def setUp(self):
  self.kit=json.loads((Path(__file__).parent/'kit.json').read_text());self.models={m['id']:m for m in self.kit['models']}
 def test_exact_finite_original_pack(self):
  self.assertEqual(len(self.models),37);self.assertEqual(len(self.kit['palette']),24)
  for m in self.models.values():
   self.assertTrue(3<=len(m['boxes'])<=100)
   for b in m['boxes']:
    self.assertTrue(0<=b['color']<24)
    for a in 'xyz':self.assertTrue(.003<=b['size'][a]<=1.001)
    for a in 'xz':self.assertLessEqual(abs(b['center'][a])+b['size'][a]/2,.501)
    self.assertGreaterEqual(b['center']['y']-b['size']['y']/2,-.0351)
    self.assertLessEqual(b['center']['y']+b['size']['y']/2,1.8501)
 def test_species_growth_and_wet_soil(self):
  for f in ('knotflax','hearthbulb','seamleaf'):
   heights=[]
   for stage in range(3):
    a=self.models[f'repair-cultivation-{f}-{stage}-dry']['boxes'];b=self.models[f'repair-cultivation-{f}-{stage}-wet']['boxes']
    self.assertEqual([(x['center'],x['size']) for x in a],[(x['center'],x['size']) for x in b]);self.assertNotEqual(a,b)
    heights.append(max(x['center']['y']+x['size']['y']/2 for x in a))
   self.assertTrue(heights[0]<heights[1]<heights[2],heights)
 def test_repairs_change_geometry_and_gate_leaf(self):
  for f,end in [('lined-well','restored'),('rope-well','restored'),('wooden-gate','closed')]:
   a=self.models[f'repair-cultivation-{f}-broken']['boxes'];b=self.models[f'repair-cultivation-{f}-{end}']['boxes'];self.assertNotEqual([(x['center'],x['size']) for x in a],[(x['center'],x['size']) for x in b])
  self.assertNotEqual(self.models['repair-cultivation-wooden-gate-closed']['boxes'],self.models['repair-cultivation-wooden-gate-open']['boxes'])
if __name__=='__main__':unittest.main()
