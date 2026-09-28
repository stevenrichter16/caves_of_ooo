import hashlib,json,math,unittest
from pathlib import Path
ROOT=Path(__file__).parent
REPO=Path('/Users/steven/caves-of-ooo')
CLIPS=['Idle','Walk','Interact','Attack','Hit','CarryIdle','CarryWalk','Pickup','Deposit']
class SourceTests(unittest.TestCase):
 def setUp(self):self.kit=json.loads((ROOT/'kit.json').read_text());self.models={m['id']:m for m in self.kit['models']}
 def model(self,live=True):
  k='spread-tatterjay' if live else 'spread-tatterjay-remains';self.assertIn(k,self.models);return self.models[k]
 def test_exact_two_original_sources(self):self.assertEqual(set(self.models),{'spread-tatterjay','spread-tatterjay-remains'})
 def test_borrowed_exact_palette_and_magpie_untouched(self):
  self.assertEqual(self.kit['palette'],json.loads((REPO/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'])
  self.assertEqual(hashlib.sha256((REPO/'ArtSource/SpreadBiome3D/actors.json').read_bytes()).hexdigest(),'6896c37644fea4116678a9005e210afdc567c9b799fa5c302deb4acb528e7e64')
 def test_actual_avian_rig_and_head_owned_bill_socket(self):
  m=self.model();self.assertEqual(m['blueprint'],'Tatterjay');self.assertEqual(m['rigFamily'],'avian')
  self.assertEqual({b['name'] for b in m['bones']},{'Root','Body','Head','Wing.L','Wing.R','Leg.L','Leg.R','Tail'})
  self.assertEqual(m['sockets'],[{'name':'Collector.Bill','bone':'Head','position':[0,.515,-.425]}])
  bill=next(b for b in m['boxes'] if b['part']=='broad-pale-bill');self.assertEqual(bill['bone'],'Head')
  self.assertTrue(all(abs(c-p)<=s/2+.001 for p,c,s in zip(m['sockets'][0]['position'],bill['center'],bill['size'])))
 def test_original_broad_chest_bill_ragged_tail_not_magpie_clone(self):
  m=self.model();b={b['part']:b for b in m['boxes']};self.assertGreaterEqual(b['broad-chest']['size'][0],.30);self.assertGreaterEqual(b['broad-pale-bill']['size'][0],.10)
  tails=[b for b in m['boxes'] if b['part'].startswith('ragged-tail-')];self.assertEqual(len(tails),3)
  self.assertEqual(len({b['size'][2] for b in tails}),3);self.assertEqual(len({b['center'][2] for b in tails}),3)
  self.assertTrue(all(b['bone']=='Tail' for b in tails));self.assertFalse(any('scrap' in b['part'] for b in m['boxes']))
 def test_nine_authored_clips_have_motion_and_carry_differs_from_empty(self):
  m=self.model();self.assertEqual(m['clips'],CLIPS);self.assertEqual(set(m['motion']),set(CLIPS))
  for n in CLIPS:self.assertTrue(any(abs(x)>0 for x in m['motion'][n].values()),n)
  self.assertNotEqual(m['motion']['CarryIdle'],m['motion']['Idle']);self.assertNotEqual(m['motion']['CarryWalk'],m['motion']['Walk']);self.assertNotEqual(m['motion']['Pickup'],m['motion']['Deposit'])
 def test_bounded_finite_palette_geometry_and_connected_parts(self):
  for m in [self.model(),self.model(False)]:
   bones={b['name'] for b in m['bones']};self.assertLessEqual(len(m['boxes']),48)
   for b in m['boxes']:
    self.assertTrue(all(math.isfinite(v) for v in b['center']+b['size']));self.assertTrue(all(v>0 for v in b['size']));self.assertTrue(all(abs(c)+s/2<1 for c,s in zip(b['center'],b['size'])));self.assertIn(b['color'],range(24))
    if m['rigged']:self.assertIn(b['bone'],bones)
   reached={0};pending=True
   while pending:
    pending=False
    for i,a in enumerate(m['boxes']):
     if i in reached:continue
     if any(all(abs(a['center'][k]-m['boxes'][j]['center'][k]) <= (a['size'][k]+m['boxes'][j]['size'][k])/2+.000001 for k in range(3)) for j in reached):reached.add(i);pending=True
   self.assertEqual(len(reached),len(m['boxes']),[b['part'] for i,b in enumerate(m['boxes']) if i not in reached])
 def test_grounded_actual_avian_remains_not_human_or_loot(self):
  m=self.model(False);self.assertEqual(m['blueprint'],'TatterjayCorpse');self.assertFalse(m['rigged']);self.assertEqual(m['bones'],[]);self.assertEqual(m['sockets'],[]);self.assertEqual(m['clips'],[])
  self.assertLess(max(b['center'][1]+b['size'][1]/2 for b in m['boxes']),.25)
  names=[b['part'] for b in m['boxes']];self.assertIn('folded-pale-bill',names);self.assertTrue(any(n.startswith('fallen-tail') for n in names));self.assertFalse(any('loot' in n or 'scrap' in n for n in names))
 def test_different_palette_parts_have_no_overlapping_coplanar_faces(self):
  overlaps=[]
  for m in [self.model(),self.model(False)]:
   for i,a in enumerate(m['boxes']):
    for b in m['boxes'][i+1:]:
     if a['color']==b['color']:continue
     for axis in range(3):
      other=[k for k in range(3) if k!=axis]
      if not all(min(a['center'][k]+a['size'][k]/2,b['center'][k]+b['size'][k]/2)-max(a['center'][k]-a['size'][k]/2,b['center'][k]-b['size'][k]/2)>.00001 for k in other):continue
      for sign in (-1,1):
       if abs(a['center'][axis]+sign*a['size'][axis]/2-b['center'][axis]-sign*b['size'][axis]/2)<.000001:overlaps.append((m['id'],a['part'],b['part'],axis,sign))
  self.assertEqual(overlaps,[])
if __name__=='__main__':unittest.main(verbosity=2)
