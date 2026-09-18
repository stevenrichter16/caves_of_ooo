import unittest,copy
from contract import validate
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
if __name__=='__main__':unittest.main()
