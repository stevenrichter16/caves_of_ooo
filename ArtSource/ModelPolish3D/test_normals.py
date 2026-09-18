"""Offline art contracts: each rejects an incorrect smoothing/weighting policy."""
import math, unittest
from sculpt_normals import blend_corner
class NormalsTests(unittest.TestCase):
 def test_nearby_plane_blends(self):
  n=blend_corner((0,0,1),[((0,.5,.8660254),1)],40,.7)
  self.assertGreater(n[1],.2);self.assertAlmostEqual(sum(x*x for x in n),1,places=7)
 def test_sharp_chip_retains_plane(self):
  self.assertEqual(blend_corner((0,0,1),[((1,0,0),100)],40,.7),(0,0,1))
 def test_area_weight_dominates(self):
  a=blend_corner((0,0,1),[((0,.5,.8660254),100),((0,-.5,.8660254),1)],40,1)
  self.assertGreater(a[1],.45)
 def test_zero_blend_retains_plane(self):
  self.assertEqual(blend_corner((0,0,1),[((0,.5,.8660254),1)],40,0),(0,0,1))
 def test_opposite_winding_never_blends(self):
  self.assertEqual(blend_corner((0,0,1),[((0,0,-1),100)],80,1),(0,0,1))
 def test_empty_keeps_plane(self):self.assertEqual(blend_corner((0,0,1),[],40,1),(0,0,1))
 def test_zero_area_ignored(self):self.assertEqual(blend_corner((0,0,1),[((0,.5,.8660254),0)],40,1),(0,0,1))
 def test_zero_normal_ignored(self):self.assertEqual(blend_corner((0,0,1),[((0,0,0),10)],40,1),(0,0,1))
 def test_repeatable(self):
  args=((0,0,1),[((0,.5,.866),1)],40,.7)
  self.assertEqual(blend_corner(*args),blend_corner(*args))
 def test_input_unchanged(self):
  rows=[((0,.5,.866),1)];before=repr(rows);blend_corner((0,0,1),rows,40,.7);self.assertEqual(repr(rows),before)
 def test_uniform_weight_scale(self):
  n=[((0,.5,.866),2),((0,-.5,.866),1)]
  a=blend_corner((0,0,1),n,40,1);b=blend_corner((0,0,1),[(x,w*1000) for x,w in n],40,1)
  for x,y in zip(a,b):self.assertAlmostEqual(x,y)
 def test_normal_scale(self):
  self.assertEqual(blend_corner((0,0,10),[],40,1),(0,0,1))
 def test_rotation_symmetry(self):
  a=blend_corner((0,0,1),[((0,.5,.866),1)],40,.7);b=blend_corner((1,0,0),[((.866,0,.5),1)],40,.7)
  self.assertEqual(a,(b[1],b[2],b[0]))
 def test_full_blend(self):
  n=blend_corner((0,0,1),[((0,.6,.8),1)],45,1);self.assertAlmostEqual(n[1],.6)
 def test_equal_opposed_slopes_cancel(self):self.assertEqual(blend_corner((0,0,1),[((0,.6,.8),1),((0,-.6,.8),1)],45,1),(0,0,1))
 def test_zero_angle_no_rounding(self):self.assertEqual(blend_corner((0,0,1),[((0,.01,.99995),1)],0,1),(0,0,1))
 def test_nonfinite_base_rejected(self):
  with self.assertRaises(ValueError):blend_corner((0,0,float('nan')),[],40,1)
 def test_zero_base_rejected(self):
  with self.assertRaises(ValueError):blend_corner((0,0,0),[],40,1)
 def test_negative_weight_rejected(self):
  with self.assertRaises(ValueError):blend_corner((0,0,1),[((0,0,1),-1)],40,1)
 def test_nonfinite_weight_rejected(self):
  with self.assertRaises(ValueError):blend_corner((0,0,1),[((0,0,1),float('inf'))],40,1)
 def test_nonfinite_neighbor_rejected(self):
  with self.assertRaises(ValueError):blend_corner((0,0,1),[((0,float('nan'),1),1)],40,1)
 def test_invalid_angle_rejected(self):
  for angle in [-1,91,float('nan')]:
   with self.assertRaises(ValueError):blend_corner((0,0,1),[],angle,1)
 def test_invalid_blend_rejected(self):
  for amount in [-.1,1.1,float('inf')]:
   with self.assertRaises(ValueError):blend_corner((0,0,1),[],40,amount)
 def test_tiny_weight_scale(self):
  a=blend_corner((0,0,1),[((0,.6,.8),1e-12)],45,1);self.assertAlmostEqual(a[1],.6)
if __name__=='__main__':unittest.main()
