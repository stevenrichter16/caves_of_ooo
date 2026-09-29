import json,math,hashlib,unittest
from pathlib import Path
P=Path(__file__).parent; R=Path('/Users/steven/caves-of-ooo')
class CoalsSourceTests(unittest.TestCase):
 def models(self):
  self.assertTrue((P/'kit.json').is_file(),'Original residual-coal pair has not been authored.')
  d=json.loads((P/'kit.json').read_text());self.assertEqual(len(d['models']),2);return d,d['models']
 def test_exact_original_static_pair_no_effect_or_service(self):
  _,ms=self.models();self.assertEqual([m['id'] for m in ms],['spread-cooking-coals-hot','spread-cooking-coals-cooled'])
  for m in ms:
   self.assertEqual(m['blueprint'],'SpreadCookingCoals');self.assertFalse(m['rigged']);self.assertEqual(m['clips'],[]);self.assertEqual(m['sockets'],[])
   self.assertFalse(any(x in m for x in ['emission','light','particles','fuel','quantity','recipe','collision','source']))
 def test_both_states_retain_exact_geometry_and_piece_identity(self):
  _,ms=self.models();self.assertEqual([p['part'] for p in ms[0]['pieces']],[p['part'] for p in ms[1]['pieces']])
  for a,b in zip(ms[0]['pieces'],ms[1]['pieces']):self.assertEqual(a['vertices'],b['vertices']);self.assertEqual(a['faces'],b['faces'])
 def test_low_grounded_one_cell_station_envelope(self):
  _,ms=self.models()
  for m in ms:
   vs=[v for p in m['pieces'] for v in p['vertices']];size=[max(v[i]for v in vs)-min(v[i]for v in vs)for i in range(3)]
   self.assertTrue(all(abs(v[0])<=.46 and abs(v[2])<=.46 and 0<=v[1]<=.16 for v in vs));self.assertGreater(size[0],.7);self.assertGreater(size[2],.6);self.assertLess(size[1],.16);self.assertGreater(size[1],.065);self.assertEqual(min(v[1]for v in vs),0)
 def test_same_approved_palette_restrained_heat_only_faces(self):
  d,ms=self.models();self.assertEqual(d['palette'],json.loads((R/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'])
  hot=[c for p in ms[0]['pieces']for c in p['faceColors']];cool=[c for p in ms[1]['pieces']for c in p['faceColors']]
  self.assertEqual(len(hot),len(cool));changed=[(a,b)for a,b in zip(hot,cool)if a!=b];self.assertGreaterEqual(len(changed),7);self.assertLessEqual(len(changed),21);self.assertTrue(all(a in [18,21,22] and b in [10,11,12,17]for a,b in changed));self.assertTrue(all(c in range(24)for c in hot+cool))
 def test_charcoal_is_low_elongated_and_edge_stones_are_incomplete(self):
  _,ms=self.models()
  for m in ms:
   coal=[p for p in m['pieces']if p['part'].startswith('charcoal-')];stones=[p for p in m['pieces']if p['part'].startswith('edge-stone-')];self.assertEqual(len(coal),7);self.assertEqual(len(stones),3)
   for p in coal:
    vs=p['vertices'];height=max(v[1]for v in vs)-min(v[1]for v in vs);span=max(math.hypot(a[0]-b[0],a[2]-b[2])for a in vs for b in vs);self.assertLess(height/span,.45);self.assertAlmostEqual(min(v[1]for v in vs),.022,places=6)
 def test_closed_finite_nonzero_geometry_small_budget(self):
  _,ms=self.models()
  for m in ms:
   tris=0
   for p in m['pieces']:
    vs=p['vertices'];self.assertTrue(all(math.isfinite(n)for v in vs for n in v));self.assertEqual(len(p['faces']),len(p['faceColors']));edges={}
    for f in p['faces']:
     self.assertEqual(len(f),len(set(f)));self.assertTrue(all(0<=i<len(vs)for i in f));tris+=len(f)-2
     a,b,c=(vs[i]for i in f[:3]);u=[b[i]-a[i]for i in range(3)];v=[c[i]-a[i]for i in range(3)];cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];self.assertGreater(sum(x*x for x in cross),1e-12)
     for a,b in zip(f,f[1:]+f[:1]):edges[tuple(sorted((a,b)))]=edges.get(tuple(sorted((a,b))),0)+1
    self.assertTrue(all(n==2 for n in edges.values()))
   self.assertGreaterEqual(tris,160);self.assertLessEqual(tris,280)
 def test_neither_source_copies_old_campfire_or_food(self):
  _,ms=self.models()
  for m in ms:self.assertFalse(any(word in p['part'] for p in m['pieces']for word in ['flame','bowl','pot','grate','grain','torch']))
 def test_hot_charcoal_is_mainly_dark_with_restrained_exposed_splits(self):
  _,ms=self.models()
  for p in ms[0]['pieces']:
   if not p['part'].startswith('charcoal-'):continue
   v=p['vertices'];top=max(q[1]for q in v);total=warm=0
   for face,color in zip(p['faces'],p['faceColors']):
    if not all(abs(v[i][1]-top)<1e-7 for i in face):continue
    area=abs(sum(v[a][0]*v[b][2]-v[b][0]*v[a][2]for a,b in zip(face,face[1:]+face[:1])))/2;total+=area
    if color in [18,21,22]:warm+=area
   self.assertGreater(total,0);self.assertGreater(warm/total,.10);self.assertLess(warm/total,.36,p['part'])
 def test_all_borrowed_toast_and_campfire_bytes_unchanged(self):
  for name,want in json.loads((P/'borrowed-inputs.json').read_text()).items():self.assertEqual(hashlib.sha256((R/name).read_bytes()).hexdigest(),want,name)
if __name__=='__main__':unittest.main(verbosity=2)
