"""Independent destructive-regeneration hypotheses; run in isolated headless bpy."""
import sys,unittest,json
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import bpy
from town_generator.scene import clear_generated,realize,OWNER
from town_generator.layout import generate_town
from town_generator.config import Config

class PipelineReview(unittest.TestCase):
 def fresh(self):
  scene=bpy.data.scenes.new('IndependentPipelineReview')
  coll=bpy.data.collections.new('GeneratedProbe');coll['coo_voxel_owner']=OWNER
  scene.collection.children.link(coll)
  obj=bpy.data.objects.new('GeneratedParent',None);obj['coo_voxel_owner']=OWNER;coll.objects.link(obj)
  return scene,coll,obj
 def test_manual_parented_child_retains_world_transform(self):
  scene,coll,parent=self.fresh();parent.location=(4,8,3)
  manual=bpy.data.objects.new('ManualAttachedObject',None);coll.objects.link(manual)
  manual.parent=parent;manual.location=(1,2,1);scene.view_layers[0].update()
  before=manual.matrix_world.copy()
  clear_generated(scene);scene.view_layers[0].update()
  self.assertIn(manual.name,scene.objects)
  self.assertLess(max(abs(manual.matrix_world[i][j]-before[i][j]) for i in range(4) for j in range(4)),1e-6)
 def test_unparented_manual_transform_remains_unchanged_countercheck(self):
  scene,coll,parent=self.fresh();parent.location=(4,8,3)
  manual=bpy.data.objects.new('ManualSeparateObject',None);coll.objects.link(manual);manual.location=(1,2,1)
  clear_generated(scene);scene.view_layers[0].update()
  self.assertEqual(tuple(manual.location),(1,2,1))
 def test_invalid_functional_plan_preserves_previous_generation(self):
  scene,coll,parent=self.fresh();name=parent.name
  town=generate_town(Config(building_count=0,population=0,vegetation_amount=0,ruin_amount=0))
  with patch('town_generator.props.plan_props',side_effect=ValueError('No accessible interior placement')):
   with self.assertRaisesRegex(ValueError,'No accessible interior placement'):
    realize(town,scene)
  self.assertIn(name,bpy.data.objects,'A planning rejection erased the previous generated town')
 def test_manual_collection_instance_keeps_borrowed_generated_content(self):
  scene,coll,parent=self.fresh()
  other=bpy.data.scenes.new('IndependentManualInstanceScene')
  instance=bpy.data.objects.new('ManualCollectionInstance',None);instance.instance_type='COLLECTION';instance.instance_collection=coll
  other.collection.objects.link(instance)
  name=parent.name
  clear_generated(scene)
  self.assertIsNotNone(instance.instance_collection,'A manually authored instance lost its source collection')
  self.assertIn(name,bpy.data.objects,'Instanced borrowed geometry was deleted')

if __name__=='__main__':
 result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(PipelineReview))
 print('PIPELINE_REVIEW_RESULT',json.dumps(dict(tests=result.testsRun,failures=len(result.failures),errors=len(result.errors))))
 if not result.wasSuccessful():raise RuntimeError('Independent pipeline review failed')
