import json,unittest,math,hashlib
from pathlib import Path
ROOT=Path(__file__).parent
class SourceTests(unittest.TestCase):
 def setUp(self):self.kit=json.loads((ROOT/'kit.json').read_text());self.models={m['id']:m for m in self.kit['models']}
 def model(self,suffix):
  self.assertIn('questfree-spread-'+suffix,self.models);return self.models['questfree-spread-'+suffix]
 def test_palette_is_accepted_first24(self):self.assertEqual(self.kit['palette'],json.loads(Path('/Users/steven/caves-of-ooo/ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'])
 def test_exact_four_owned_forms(self):self.assertEqual(set(self.models),{'questfree-spread-grazer','questfree-spread-grazer-remains','questfree-spread-draw-full','questfree-spread-draw-empty'})
 def test_original_low_broad_herbivore_four_leg_rig(self):
  m=self.model('grazer');self.assertEqual(m['blueprint'],'ReedbackGrazer');self.assertEqual(m['rigFamily'],'quadruped');self.assertEqual(sum(b['name'].startswith('Leg') for b in m['bones']),4)
  names=[b['part'] for b in m['boxes']];self.assertTrue(any(x.startswith('reed-ridge') for x in names));self.assertIn('broad-muzzle',names);self.assertFalse(any(x in names for x in ('fang','horn','antler','back-saddle')))
  body=next(b for b in m['boxes'] if b['part']=='barrel');self.assertGreater(body['size'][0],.40);self.assertGreater(body['size'][2],.60)
 def test_five_clips_and_explicit_head_down_feeding(self):
  m=self.model('grazer');self.assertEqual(m['clips'],['Idle','Walk','Interact','Attack','Hit']);self.assertGreaterEqual(m['feedingHeadPitch'],.5);self.assertLessEqual(m['feedingHeadPitch'],.8)
 def test_all_finite_bone_owned_nonzero_bounded_cuboids(self):
  self.assertEqual(len(self.models),4)
  for m in self.models.values():
   names={b['name'] for b in m.get('bones',[])};self.assertGreater(len(m['boxes']),0);self.assertLessEqual(len(m['boxes']),64)
   for b in m['boxes']:
    self.assertTrue(all(math.isfinite(v) for v in b['center']+b['size']));self.assertTrue(all(v>0 for v in b['size']));self.assertTrue(all(abs(c)+s/2<=1 for c,s in zip(b['center'],b['size'])));self.assertIn(b['material'],['palette','water'])
    if m.get('rigged'):self.assertIn(b['bone'],names)
    if b['material']=='palette':self.assertIn(b['color'],range(24))
 def test_remains_original_low_silhouette_no_fake_reward(self):
  m=self.model('grazer-remains');self.assertEqual(m['blueprint'],'ReedbackGrazerCorpse');self.assertFalse(m['rigged']);self.assertLess(max(b['center'][1]+b['size'][1]/2 for b in m['boxes']),.3);self.assertTrue(any(b['part'].startswith('reed-ridge') for b in m['boxes']))
 def test_draw_empty_keeps_real_trough_and_no_water(self):
  empty=self.model('draw-empty');full=self.model('draw-full');self.assertEqual(empty['blueprint'],'SpreadDrawPoint');self.assertEqual(empty['boxes'],[b for b in full['boxes'] if b['material']!='water']);self.assertGreaterEqual(len(empty['boxes']),6);self.assertFalse(any(b['material']=='water' for b in empty['boxes']))
 def test_full_surface_visible_inside_rim_not_solid_block(self):
  m=self.model('draw-full');water=[b for b in m['boxes'] if b['material']=='water'];self.assertEqual(len(water),1);w=water[0];self.assertLess(w['size'][1],.04);self.assertGreater(w['size'][0]*w['size'][2],.1);rim=next(b for b in m['boxes'] if b['part']=='front-lip');self.assertGreater(rim['center'][1]+rim['size'][1]/2,w['center'][1]+w['size'][1]/2);self.assertEqual(w['semanticLiquid'],'water')
 def test_draw_rim_raises_readable_stone_without_expanding_cell_footprint(self):
  for suffix in ('draw-full','draw-empty'):
   m=self.model(suffix);rim=[b for b in m['boxes'] if b['part'].endswith('-lip')]
   self.assertEqual(len(rim),4)
   self.assertGreaterEqual(max(b['center'][1]+b['size'][1]/2 for b in rim),.42)
   self.assertTrue(all(abs(b['center'][axis])+b['size'][axis]/2<=.49 for b in m['boxes'] for axis in (0,2)))
 def test_draw_water_presents_broad_raised_surface_below_real_open_rim(self):
  m=self.model('draw-full');w=next(b for b in m['boxes'] if b['material']=='water')
  self.assertGreaterEqual(w['center'][1],.30);self.assertGreaterEqual(w['size'][0]*w['size'][2],.30)
  front=next(b for b in m['boxes'] if b['part']=='front-lip')
  self.assertLessEqual(front['center'][1]+front['size'][1]/2-(w['center'][1]+w['size'][1]/2),.045)
 def test_polish_preserves_animal_sources_and_exact_empty_structure(self):
  before={'questfree-spread-grazer': 'e4443d3edfde0fdf80de040816dffa2d66c6c75eb4e554fd5aa8434f4e9412b5', 'questfree-spread-grazer-remains': '5e65f1971cce56a7f773e56935852503f4fafd48eb3445d39cc9ddc1da136818'}
  for suffix in ('grazer','grazer-remains'):
   m=self.model(suffix);self.assertEqual(hashlib.sha256(json.dumps(m,sort_keys=True).encode()).hexdigest(),before[m['id']])
  self.assertEqual(self.model('draw-empty')['boxes'],[b for b in self.model('draw-full')['boxes'] if b['material']!='water'])
if __name__=='__main__':unittest.main(verbosity=2)
