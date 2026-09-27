import ast,json,unittest
from pathlib import Path
from types import SimpleNamespace
ROOT=Path(__file__).parent
class SourceMothTests(unittest.TestCase):
 def source(self):return ROOT/'build_ring.py'
 def geometry(self):
  node=next(x for x in ast.parse(self.source().read_text()).body if isinstance(x,ast.FunctionDef) and x.name=='lantern_moth')
  output=[];bones=[]
  def ellipsoid(name,center,scale,color,*args):
   o=SimpleNamespace(name=name,center=center,scale=scale,color=color,rotation_euler=[0,0,0]);output.append(o);return o
  def beam(name,start,end,radius,color,*args):return SimpleNamespace(name=name)
  def rig_parts(mid,definition,weights,family):bones.extend(definition)
  env={'model':lambda *x:None,'ellipsoid':ellipsoid,'beam':beam,'rig_parts':rig_parts}
  exec(compile(ast.Module(body=[node],type_ignores=[]),str(self.source()),'exec'),env)
  env['lantern_moth']('ring-grove-lantern-moth');return output,bones
 def test_fore_and_hind_lobes_have_a_real_notch(self):
  g,_=self.geometry();front=[o for o in g if o.name=='Lantern_moth_forewing'];rear=[o for o in g if o.name=='Lantern_moth_hindwing']
  self.assertEqual(2,len(front));self.assertEqual(2,len(rear))
  for f,h in zip(front,rear):self.assertGreaterEqual((h.center[1]-h.scale[1])-(f.center[1]+f.scale[1]),.08)
 def test_all_six_bars_are_wide_enough_for_finer_voxels(self):
  bands=[o for o in self.geometry()[0] if o.name=='Lantern_moth_wing_band'];self.assertEqual(6,len(bands))
  for b in bands:self.assertGreaterEqual(b.scale[0]*2,.05)
 def test_raised_body_is_visible_above_pale_wings(self):
  g,_=self.geometry();body=next(o for o in g if o.name=='Lantern_moth_body');wing=next(o for o in g if o.name=='Lantern_moth_forewing')
  self.assertGreaterEqual(body.center[2]+body.scale[2]-(wing.center[2]+wing.scale[2]),.04)
  self.assertNotEqual(body.color,wing.color)
 def test_five_existing_bones_and_roles_are_unchanged(self):
  _,bones=self.geometry();self.assertEqual(['Root','Body','Head','Wing.L','Wing.R'],[x[0] for x in bones]);self.assertEqual(5,len(bones))
if __name__=='__main__':unittest.main()
