import unittest,copy
from contract import validate, validate_variant_fingerprints
class ContractTests(unittest.TestCase):
 def row(self):return {'id':'wood-post-0','family':'wood-post','variant':0,'path':'models/wood-post-0.fbx','pivot':'bottom-centre','bounds':{'min':[-.5,-.5,0],'max':[.5,.5,1.2]},'triangles':100,'zeroAreaTriangles':0,'material':'wood','cellSize':1}
 def test_valid_single_family(self):self.assertEqual(validate([dict(self.row(),id='wood-post-'+str(i),variant=i,path='models/wood-post-'+str(i)+'.fbx') for i in range(4)]),[])
 def test_invalid_cases(self):
  mutations=[('id',''),('family',''),('variant',-1),('variant',4),('variant',True),('path','../x.fbx'),('path','models/wrong.fbx'),('pivot','centre'),('triangles',0),('triangles',-1),('zeroAreaTriangles',1),('material','unknown'),('cellSize',2)]
  for key,val in mutations:
   with self.subTest(key=key,val=val):
    rows=[dict(self.row(),id='wood-post-'+str(i),variant=i,path='models/wood-post-'+str(i)+'.fbx') for i in range(4)];rows[0][key]=val;self.assertTrue(validate(rows))
 def test_bounds_failures(self):
  for axis,value,which in [(0,-.51,'min'),(1,-.51,'min'),(0,.51,'max'),(1,.51,'max'),(2,-.01,'min'),(2,float('nan'),'max'),(0,float('inf'),'max'),(0,.6,'min')]:
   with self.subTest(axis=axis,value=value):
    r=self.row();r['bounds'][which][axis]=value;self.assertTrue(validate([r]))
 def test_missing_variants(self):self.assertTrue(validate([self.row()]))
 def test_duplicate_identity(self):self.assertTrue(validate([self.row()]*4))
 def test_empty(self):self.assertTrue(validate([]))
 def test_no_mutation(self):
  r=[self.row()];old=copy.deepcopy(r);validate(r);self.assertEqual(r,old)

class ExportVariantTests(unittest.TestCase):
 def rows(self,family='wood-post'):
  return [{'id':family+'-'+str(i),'family':family,'variant':i,'visualFingerprint':format(i+1,'064x')} for i in range(4)]
 def test_four_distinct_imported_shapes_or_paints_pass(self):
  self.assertEqual(validate_variant_fingerprints(self.rows()),[])
 def test_different_filenames_do_not_hide_duplicate_imported_geometry_and_uv(self):
  rows=self.rows();rows[3]['visualFingerprint']=rows[0]['visualFingerprint']
  self.assertIn('duplicate-visual-variants:wood-post:3',validate_variant_fingerprints(rows))
 def test_unrelated_families_may_share_a_visual_fingerprint(self):
  self.assertEqual(validate_variant_fingerprints(self.rows()+self.rows('stone-post')),[])
 def test_missing_or_malformed_fingerprint_fails_closed(self):
  for value in [None,'','not-a-sha256','0'*63,'0'*65]:
   rows=self.rows();rows[0]['visualFingerprint']=value
   self.assertIn('invalid-visual-fingerprint:wood-post-0',validate_variant_fingerprints(rows))
 def test_missing_variant_does_not_count_as_a_complete_family(self):
  self.assertIn('incomplete-visual-variants:wood-post',validate_variant_fingerprints(self.rows()[:3]))
 def test_repeat_variant_with_unique_hash_fails(self):
  rows=self.rows();rows[3]['variant']=0
  self.assertIn('incomplete-visual-variants:wood-post',validate_variant_fingerprints(rows))
 def test_empty_export_fails(self):self.assertEqual(validate_variant_fingerprints([]),['empty-visual-kit'])
 def test_validation_does_not_modify_audit_rows(self):
  rows=self.rows();before=copy.deepcopy(rows);validate_variant_fingerprints(rows);self.assertEqual(rows,before)
if __name__=='__main__':unittest.main()
