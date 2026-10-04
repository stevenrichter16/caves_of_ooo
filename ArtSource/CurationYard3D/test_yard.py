import json, unittest
from pathlib import Path
ROOT=Path(__file__).parent
class YardSourceTests(unittest.TestCase):
 def setUp(self):self.pack=json.loads((ROOT/'kit.json').read_text()) if (ROOT/'kit.json').exists() else {'palette':[],'models':[]}
 def test_exact_original_forms(self):
  ids=['salt-cured-body-1','salt-cured-body-2','receiving-bay-1','receiving-bay-2','intake-index','tool-cabinet','salt-bench','quarantine-rail','quarantine-gate-closed','quarantine-gate-open','salt-rake','counterfoil','inspection-key','transfer-docket','discrepancy-report','service-gate-broken','recovery-cabinet','maintenance-rack','inspection-slab','annex-placard']
  self.assertEqual({'curation-yard-'+x for x in ids},{m['id'] for m in self.pack['models']})
 def test_all_forms_have_native_bounded_geometry_and_palette(self):
  self.assertEqual(24,len(self.pack['palette']));self.assertEqual(20,len(self.pack['models']))
  for model in self.pack['models']:
   self.assertGreaterEqual(len(model['boxes']),3);self.assertLess(len(model['boxes']),100)
   self.assertGreaterEqual(len({b['color'] for b in model['boxes']}),2)
   for b in model['boxes']:
    self.assertIn(b['color'],range(24));self.assertTrue(all(s>0 for s in b['size'].values()))
    for axis in ['x','z']:self.assertLessEqual(abs(b['center'][axis])+b['size'][axis]/2,.50001)
    self.assertGreaterEqual(b['center']['y']-b['size']['y']/2,-.001);self.assertLessEqual(b['center']['y']+b['size']['y']/2,1.65)
 def test_cases_have_real_matching_tags_and_different_human_geometry(self):
  rows={m['id']:m for m in self.pack['models']};self.assertIn('curation-yard-salt-cured-body-1',rows)
  shapes=[]
  for case in [1,2]:
   body=rows[f'curation-yard-salt-cured-body-{case}'];bay=rows[f'curation-yard-receiving-bay-{case}']
   for model in [body,bay]:self.assertEqual(case,len([b for b in model['boxes'] if b['name'].startswith('case-notch')]))
   self.assertTrue(all(any(b['name']==part for b in body['boxes']) for part in ['head','torso','left-leg','right-leg']))
   shapes.append([(b['center'],b['size']) for b in body['boxes']])
  self.assertNotEqual(shapes[0],shapes[1])
 def test_open_gate_moves_leaf_aside_with_posts_unchanged(self):
  rows={m['id']:m for m in self.pack['models']};self.assertIn('curation-yard-quarantine-gate-open',rows)
  closed=rows['curation-yard-quarantine-gate-closed']['boxes'];opened=rows['curation-yard-quarantine-gate-open']['boxes']
  self.assertEqual([b for b in closed if b['name'].startswith('post')],[b for b in opened if b['name'].startswith('post')])
  self.assertTrue(any(b['name'].startswith('leaf') and abs(b['center']['x'])<.1 for b in closed))
  self.assertTrue(all(abs(b['center']['x'])-b['size']['x']/2>.25 for b in opened if b['name'].startswith('leaf')))
 def test_annex_forms_explain_their_actual_functions(self):
  rows={m['id']:m for m in self.pack['models']}
  required={
   'service-gate-broken':['broken-brace','fallen-leaf','split-post'],
   'recovery-cabinet':['latched-door','sealed-bundle','recovery-mark'],
   'maintenance-rack':['timber-rest','tools-hook','materials-card'],
   'inspection-slab':['head-rest','wrist-restraint','drain-channel'],
   'annex-placard':['board','gallery-diagram','holding-diagram','service-diagram']}
  for suffix,names in required.items():
   self.assertIn('curation-yard-'+suffix,rows)
   model=rows['curation-yard-'+suffix]
   self.assertTrue(set(names).issubset({b['name'] for b in model['boxes']}))
   self.assertGreaterEqual(len({b['color'] for b in model['boxes']}),3)
if __name__=='__main__':unittest.main()
