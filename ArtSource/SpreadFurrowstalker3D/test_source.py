"""Independent original-source contracts; no Unity/runtime acceptance implied."""
import copy,json,math,unittest
from pathlib import Path
ROOT=Path(__file__).parent
REPO=Path('/Users/steven/caves-of-ooo')
IDS=['spread-furrowstalker','spread-furrowstalker-remains']
CLIPS=['Idle','Walk','Interact','Attack','Hit']
def validate_palette(k):
 assert k['palette']==json.loads((REPO/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette']
def validate_geometry(m):
 names={b['name'] for b in m['bones']}
 assert len({p['part'] for p in m['boxes']})==len(m['boxes'])
 assert 8<=len(m['boxes'])<=70
 for p in m['boxes']:
  assert len(p['center'])==len(p['size'])==3
  assert all(math.isfinite(n) for n in p['center']+p['size'])
  assert all(s>0 for s in p['size'])
  assert all(abs(c)+s/2<=.95 for c,s in zip(p['center'],p['size']))
  assert isinstance(p['color'],int) and 0<=p['color']<24 and p['material']=='palette'
  assert not m['rigged'] or p['bone'] in names
 assert not m.get('sockets')
def validate_rig(m):
 assert m['rigged'] and m['rigFamily']=='quadruped' and m['clips']==CLIPS
 names=[b['name'] for b in m['bones']];assert 9<=len(names)<=12 and len(set(names))==len(names)
 assert {'Root','Body','Neck','Head','Jaw','Tail','LegFront.L','LegFront.R','LegRear.L','LegRear.R'}<=set(names)
 seen=set()
 for b in m['bones']:
  assert b['parent'] in seen or b['name']=='Root' and b['parent'] is None
  assert all(math.isfinite(v) for v in b['head']+b['tail'])
  assert sum((x-y)**2 for x,y in zip(b['head'],b['tail']))>.0001
  seen.add(b['name'])
def validate_motion(m):
 motion=m['motion']
 assert set(motion)==set(CLIPS)
 for clip in CLIPS:
  d=motion[clip];assert d['frames']>=4 and d['frames']<=48 and d['loop']==(clip in ('Idle','Walk'))
  assert d['rootMotion'] is False
 assert .45<=motion['Interact']['headPitch']<=.75
 assert .12<=motion['Interact']['jawPitch']<=.35
 assert motion['Attack']['jawPitch']>=.32 and motion['Attack']['headPitch']<0
 assert .15<=motion['Walk']['legPitch']<=.35
 assert 0<motion['Idle']['breathScale']<=.02
 assert motion['Hit']['bodyPitch']>0
class SourceTests(unittest.TestCase):
 def setUp(self):
  self.k=json.loads((ROOT/'kit.json').read_text());self.ms={m['id']:m for m in self.k['models']};self.body=self.ms[IDS[0]];self.remains=self.ms[IDS[1]]
 def test_exact_original_two_forms_not_borrowed_grazer(self):
  self.assertEqual(list(self.ms),IDS);self.assertEqual(self.body['blueprint'],'Furrowstalker');self.assertEqual(self.remains['blueprint'],'FurrowstalkerCorpse')
  self.assertEqual(self.k['id'],'spread-furrowstalker-original');self.assertNotIn('animations',self.remains)
 def test_approved_palette_and_foreign_swatch_counter(self):
  validate_palette(self.k);k=copy.deepcopy(self.k);k['palette'][0]='#FF00FF'
  with self.assertRaises(AssertionError):validate_palette(k)
 def test_finite_owned_geometry_and_invalid_buffer_counters(self):
  for m in self.ms.values():validate_geometry(m)
  for field,value in [('size',[0,.1,.1]),('center',[float('nan'),0,0]),('color',24),('bone','foreign')]:
   m=copy.deepcopy(self.body);m['boxes'][0][field]=value
   with self.assertRaises(AssertionError,msg=field):validate_geometry(m)
 def test_low_long_hunter_has_distinct_narrow_waist_and_real_jaw(self):
  pieces={b['part']:b for b in self.body['boxes']};torso=pieces['long-torso'];waist=pieces['drawn-waist'];head=pieces['wedge-head']
  self.assertGreater(torso['size'][2],torso['size'][0]*1.7);self.assertLess(waist['size'][0],torso['size'][0]);self.assertLess(head['size'][1],.19)
  self.assertEqual(pieces['lower-jaw']['bone'],'Jaw');self.assertFalse(any('ridge' in p or 'hoof' in p or 'saddle' in p for p in pieces))
 def test_rig_is_connected_with_four_legs_and_foreign_parent_counter(self):
  validate_rig(self.body);m=copy.deepcopy(self.body);m['bones'][-1]['parent']='foreign'
  with self.assertRaises(AssertionError):validate_rig(m)
 def test_five_live_states_and_absent_clip_counter(self):
  validate_rig(self.body);m=copy.deepcopy(self.body);m['clips'].remove('Attack')
  with self.assertRaises(AssertionError):validate_rig(m)
 def test_five_explicit_bounded_motions_and_motionless_counter(self):
  validate_motion(self.body)
  for key,field in [('Interact','headPitch'),('Attack','jawPitch'),('Walk','legPitch'),('Idle','breathScale'),('Hit','bodyPitch')]:
   m=copy.deepcopy(self.body);m['motion'][key][field]=0
   with self.assertRaises(AssertionError,msg=key):validate_motion(m)
 def test_no_root_motion_or_unrequested_effects(self):
  self.assertEqual(self.k['effects'],[]);self.assertEqual(self.k['physics'],[])
  m=copy.deepcopy(self.body);m['motion']['Attack']['rootMotion']=True
  with self.assertRaises(AssertionError):validate_motion(m)
 def test_exact_static_low_remains_keeps_body_marks_without_reward(self):
  m=self.remains;self.assertFalse(m['rigged']);self.assertEqual(m['bones'],[]);self.assertEqual(m['clips'],[])
  self.assertLess(max(p['center'][1]+p['size'][1]/2 for p in m['boxes']),.26)
  self.assertIn('folded-tapered-tail',{p['part'] for p in m['boxes']});self.assertIn('pale-throat',{p['part'] for p in m['boxes']})
  self.assertNotIn('reward',m);self.assertNotIn('meat',json.dumps(m))
 def test_geometry_budget_and_grounded_feet(self):
  self.assertGreaterEqual(len(self.body['boxes'])*12,400);self.assertLessEqual(len(self.body['boxes'])*12,700)
  self.assertLessEqual(len(self.remains['boxes'])*12,240)
  feet=[p for p in self.body['boxes'] if p['part'].startswith('foot-')];self.assertEqual(len(feet),4)
  self.assertTrue(all(abs(p['center'][1]-p['size'][1]/2)<.001 for p in feet))
if __name__=='__main__':unittest.main(verbosity=2)
