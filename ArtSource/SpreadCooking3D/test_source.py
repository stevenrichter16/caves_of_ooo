import json,math,unittest,hashlib
from pathlib import Path
P=Path(__file__).parent
R=Path('/Users/steven/caves-of-ooo')
class ToastedSourceTests(unittest.TestCase):
 def model(self):
  self.assertTrue((P/'kit.json').exists(),'Original prepared-grain source is not authored yet.')
  d=json.loads((P/'kit.json').read_text());self.assertEqual(len(d['models']),1);return d,d['models'][0]
 def test_one_exact_static_food_without_container_rig_or_effect(self):
  d,m=self.model();self.assertEqual(m['id'],'spread-toasted-emberwheat');self.assertEqual(m['blueprint'],'ToastedEmberwheat');self.assertEqual(m['clips'],[]);self.assertEqual(m['sockets'],[]);self.assertFalse(m['rigged']);self.assertEqual(m['kind'],'entity');self.assertNotIn('source',m)
  self.assertTrue(all(k['part'].startswith('toasted-kernel-') for k in m['kernels']))
 def test_original_low_asymmetric_cluster_fits_portable_cell(self):
  _,m=self.model();v=[v for k in m['kernels'] for v in k['vertices']];self.assertGreaterEqual(len(m['kernels']),6);self.assertLessEqual(len(m['kernels']),12)
  bounds=[(min(p[a] for p in v),max(p[a] for p in v)) for a in range(3)]
  self.assertGreater(bounds[0][1]-bounds[0][0],.4);self.assertLess(bounds[0][1]-bounds[0][0],.62);self.assertGreater(bounds[2][1]-bounds[2][0],.25);self.assertLess(bounds[2][1]-bounds[2][0],.5)
  self.assertGreaterEqual(bounds[1][0],0);self.assertGreater(bounds[1][1],.055);self.assertLess(bounds[1][1],.10)
  self.assertNotAlmostEqual(sum(p[0] for p in v),0,places=4)
 def test_palette_is_approved_and_grain_has_brown_pale_and_charred_faces(self):
  d,m=self.model();self.assertEqual(d['palette'],json.loads((R/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette']);colors={i for k in m['kernels'] for i in k['faceColors']};self.assertTrue(colors.issubset({18,19,20,21,22}));self.assertTrue({18,19,22}.issubset(colors))
 def test_each_grain_is_closed_finite_grounded_and_has_nonzero_faces(self):
  _,m=self.model()
  for k in m['kernels']:
   v=k['vertices'];self.assertTrue(all(math.isfinite(n) for p in v for n in p));self.assertAlmostEqual(min(p[1] for p in v),.01,places=6);edges={}
   for f in k['faces']:
    self.assertGreaterEqual(len(f),3);self.assertEqual(len(f),len(set(f)));self.assertTrue(all(0<=i<len(v) for i in f));a,b,c=(v[i] for i in f[:3]);u=[b[i]-a[i] for i in range(3)];w=[c[i]-a[i] for i in range(3)];cross=[u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0]];self.assertGreater(sum(n*n for n in cross),1e-12)
    for a,b in zip(f,f[1:]+f[:1]):edges[tuple(sorted((a,b)))]=edges.get(tuple(sorted((a,b))),0)+1
   self.assertTrue(all(n==2 for n in edges.values()));self.assertEqual(len(k['faces']),len(k['faceColors']))
 def test_no_borrowed_sheaf_or_new_food_parts_in_source(self):
  _,m=self.model();self.assertNotIn('parts',m);self.assertNotIn('quantity',m);self.assertNotIn('healing',m);self.assertNotIn('weight',m);self.assertNotIn('Physics',json.dumps(m));self.assertTrue(all(k['part']!='bowl' for k in m['kernels']))
 def test_source_budget_is_small_and_no_duplicate_coplanar_parts(self):
  _,m=self.model();self.assertLessEqual(sum(sum(len(f)-2 for f in k['faces']) for k in m['kernels']),240);self.assertEqual(len({json.dumps(k['vertices']) for k in m['kernels']}),len(m['kernels']))
 def test_each_kernel_is_low_and_elongated_not_an_upright_stone(self):
  _,m=self.model()
  for k in m['kernels']:
   v=k['vertices'][:6];a,b=max(((a,b) for a in v for b in v),key=lambda ab:(ab[1][0]-ab[0][0])**2+(ab[1][2]-ab[0][2])**2)
   dx,dz=b[0]-a[0],b[2]-a[2];length=math.sqrt(dx*dx+dz*dz);across=[(-dz*q[0]+dx*q[2])/length for q in v];width=max(across)-min(across)
   height=max(q[1] for q in k['vertices'])-min(q[1] for q in k['vertices'])
   self.assertGreaterEqual(length/width,2.4,k['part']);self.assertLessEqual(height/length,.30,k['part'])
 def test_existing_raw_sheaf_and_other_food_sources_unchanged(self):
  for name,want in json.loads((P/'borrowed-inputs.json').read_text()).items():self.assertEqual(hashlib.sha256((R/name).read_bytes()).hexdigest(),want,name)
if __name__=='__main__':unittest.main(verbosity=2)
