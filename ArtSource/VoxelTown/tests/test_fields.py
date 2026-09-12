import unittest
from types import SimpleNamespace as NS
from town_generator.terrain import sample_fields
class FieldsTests(unittest.TestCase):
 def town(self,water=.5):
  return NS(config=NS(town_size=80,water_amount=water,vegetation_amount=.6,agriculture_amount=.5,ruin_amount=.3,seed=41),water_bodies=[NS(center=(-20,0),radii=(10,20))] if water else [],buildings=[],farms=[],paths=[],anchors={'plaza':(0,0),'ruin':(24,18),'farming':(-5,18)})
 def test_shore_wetter_than_desert(self):
  t=self.town();self.assertGreater(sample_fields(-9,0,t)['water_influence'],sample_fields(30,0,t)['water_influence'])
 def test_no_water_counter(self):self.assertEqual(sample_fields(-9,0,self.town(0))['water_influence'],0)
 def test_finite_bounded_and_gradual(self):
  t=self.town()
  for x in range(-40,40):
   a=sample_fields(x,0,t);b=sample_fields(x+.01,0,t)
   for k,v in a.items():self.assertTrue(0<=v<=1);self.assertLess(abs(v-b[k]),.02)
 def test_repeat_does_not_mutate(self):
  t=self.town();a=sample_fields(3,4,t);self.assertEqual(a,sample_fields(3,4,t))
if __name__=='__main__':unittest.main()
