import unittest,json,math,hashlib
from pathlib import Path
from build_actors import build,ROOT
class SourceAnimals(unittest.TestCase):
 def model(self,name):
  rows=[r for r in build()['models'] if r['blueprint']==name];self.assertEqual(1,len(rows),name);return rows[0]
 def test_exact_original_source_roster(self):
  self.assertEqual({'Magpie','PetDog','Viper'},{r['blueprint']for r in build()['models']})
  self.assertEqual(3,len({r['id']for r in build()['models']}))
 def test_palette_is_approved_and_borrowed_files_are_unchanged(self):
  files=[ROOT/'ArtSource/ReferenceGlade3D/kit.json',ROOT/'ArtSource/SpawnRing3D/catalog.json'];old=[p.read_bytes()for p in files]
  self.assertEqual(json.loads(old[0])['palette'],build()['palette']);build();self.assertEqual(old,[p.read_bytes()for p in files])
 def test_magpie_has_distinct_wings_beak_feet_and_long_tail(self):
  m=self.model('Magpie');parts={b['part']for b in m['boxes']}
  self.assertTrue({'wing-left','wing-right','beak','tail','foot-left','foot-right','breast'}<=parts)
  self.assertEqual('avian',m['rigFamily']);self.assertEqual(2,len([b for b in m['bones']if b['name'].startswith('Wing.')]))
 def test_dog_has_four_independent_legs_muzzle_ears_and_tail(self):
  m=self.model('PetDog');parts={b['part']for b in m['boxes']}
  self.assertTrue({'muzzle','ear-left','ear-right','tail','chest'}<=parts)
  self.assertEqual('quadruped',m['rigFamily']);self.assertEqual(4,len([b for b in m['bones']if b['name'].startswith('Leg')]))
 def test_viper_has_continuous_tapered_body_without_fictitious_limbs(self):
  m=self.model('Viper');self.assertEqual('serpent',m['rigFamily']);self.assertFalse(any(b['name'].startswith(('Leg','Wing','Arm'))for b in m['bones']))
  body=[b for b in m['boxes']if b['part'].startswith('coil-')];self.assertGreaterEqual(len(body),8)
  self.assertLess(body[-1]['size'][0],body[0]['size'][0]);self.assertIn('head',[b['part']for b in m['boxes']])
 def test_viper_neighboring_body_volumes_overlap_in_rest_pose(self):
  body=[b for b in self.model('Viper')['boxes']if b['part'].startswith('coil-')]
  for a,b in zip(body,body[1:]):
   for axis in range(3):
    overlap=min(a['center'][axis]+a['size'][axis]/2,b['center'][axis]+b['size'][axis]/2)-max(a['center'][axis]-a['size'][axis]/2,b['center'][axis]-b['size'][axis]/2)
    self.assertGreater(overlap,.005,(a['part'],b['part'],axis))
 def test_rig_graph_and_every_authored_part_have_valid_bone_and_palette(self):
  models=build()['models'];self.assertEqual(3,len(models))
  for m in models:
   seen=set()
   for b in m['bones']:
    self.assertNotIn(b['name'],seen);self.assertTrue(b['parent']is None or b['parent']in seen);seen.add(b['name']);self.assertGreater(sum((a-c)**2 for a,c in zip(b['head'],b['tail'])),.00001)
   self.assertEqual(['Idle','Walk','Interact','Attack','Hit'],m['clips']);self.assertGreater(len(m['boxes']),12)
   for b in m['boxes']:
    self.assertIn(b['bone'],seen);self.assertIn(b['color'],range(24));self.assertTrue(all(math.isfinite(v)for v in b['center']+b['size']));self.assertTrue(all(v>0 for v in b['size']))
 def test_magpie_fits_small_native_owner_without_flat_card(self):self.check_size('Magpie',.25,.9,.30,.8,.6,1.3)
 def test_dog_fits_low_quadruped_native_owner(self):self.check_size('PetDog',.25,.75,.35,.85,.65,1.3)
 def test_viper_retains_low_long_shape(self):self.check_size('Viper',.35,.95,.08,.35,.65,1.3)
 def check_size(self,name,x0,x1,y0,y1,z0,z1):
  m=self.model(name);lo=[min(b['center'][i]-b['size'][i]/2 for b in m['boxes'])for i in range(3)];hi=[max(b['center'][i]+b['size'][i]/2 for b in m['boxes'])for i in range(3)]
  self.assertGreaterEqual(lo[1],-.005)
  for i,(minimum,maximum)in enumerate([(x0,x1),(y0,y1),(z0,z1)]):self.assertGreaterEqual(hi[i]-lo[i],minimum);self.assertLessEqual(hi[i]-lo[i],maximum)
if __name__=='__main__':unittest.main(verbosity=2)
