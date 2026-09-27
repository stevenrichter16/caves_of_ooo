import json,unittest
from pathlib import Path
class Fauna(unittest.TestCase):
 def setUp(self):
  p=Path(__file__).with_name('fauna.json');self.k=json.loads(p.read_text()) if p.exists() else {'models':[]};self.m={r['id']:r for r in self.k['models']}
 def test_exact_current_fauna_models(self):self.assertEqual({'spread-creature-giant-spider','spread-creature-jungle-ape','spread-creature-mimic-closed','spread-creature-mimic-awake','spread-creature-glowmaw'},set(self.m))
 def model(self,id):self.assertIn('spread-creature-'+id,self.m);return self.m['spread-creature-'+id]
 def test_spider_has_eight_independent_legs_and_low_abdomen(self):
  m=self.model('giant-spider');self.assertEqual(8,len([b for b in m['bones'] if b['name'].startswith('Leg.')]))
  feet=[b for b in m['boxes'] if b['part'].startswith('foot-')];self.assertEqual(8,len(feet));self.assertTrue(all(b['center'][1]-b['size'][1]/2<=.02 for b in feet))
  self.assertLess(max(b['center'][1]+b['size'][1]/2 for b in m['boxes']),.65)
 def test_ape_long_arms_no_tail_and_distinct_pale_face(self):
  m=self.model('jungle-ape');names={b['name'] for b in m['bones']};self.assertTrue({'Arm.L','Arm.R','Hand.L','Hand.R','Leg.L','Leg.R'}<=names);self.assertNotIn('Tail',names)
  self.assertTrue(any(b['part']=='pale-face' and b['color']==17 for b in m['boxes']))
 def test_mimic_disguise_has_no_visible_teeth_but_awake_form_does(self):
  closed=self.model('mimic-closed');awake=self.model('mimic-awake')
  self.assertFalse(any('tooth' in b['part'] or 'tongue' in b['part'] or 'eye' in b['part'] for b in closed['boxes']))
  self.assertGreaterEqual(len([b for b in awake['boxes'] if 'tooth' in b['part']]),8)
  self.assertTrue(any('tongue' in b['part'] for b in awake['boxes']))
 def test_glowmaw_has_grasping_limbs_and_lit_throat_not_a_humanoid(self):
  m=self.model('glowmaw');self.assertGreaterEqual(len([b for b in m['bones'] if b['name'].startswith('Arm.')]),4);self.assertTrue(any(b['part']=='throat' and b['color']==22 for b in m['boxes']))
 def test_spider_colored_leg_joints_have_no_coplanar_overlapping_faces(self):
  m=self.model('giant-spider');conflicts=[]
  for i,a in enumerate(m['boxes']):
   for b in m['boxes'][i+1:]:
    if a['bone']!=b['bone'] or not a['bone'].startswith('Leg.') or a['color']==b['color']:continue
    for axis in range(3):
     others=[v for v in range(3) if v!=axis]
     if not all(min(a['center'][v]+a['size'][v]/2,b['center'][v]+b['size'][v]/2)-max(a['center'][v]-a['size'][v]/2,b['center'][v]-b['size'][v]/2)>1e-6 for v in others):continue
     for sign in [-1,1]:
      if abs((a['center'][axis]+sign*a['size'][axis]/2)-(b['center'][axis]+sign*b['size'][axis]/2))<1e-6:conflicts.append((a['part'],b['part'],axis,sign))
  self.assertEqual([],conflicts,'Different colored overlapping coplanar faces flicker under native rasterization.')
 def test_all_parts_have_real_bones_positive_sizes_and_exact_palette(self):
  self.assertEqual(5,len(self.m))
  for m in self.m.values():
   bones={b['name'] for b in m['bones']};self.assertIn('Root',bones);self.assertEqual(['Idle','Walk','Interact','Attack','Hit'],m['clips'])
   for b in m['boxes']:
    self.assertIn(b['bone'],bones);self.assertTrue(all(v>0 for v in b['size']));self.assertIn(b['color'],range(24))
if __name__=='__main__':unittest.main()
