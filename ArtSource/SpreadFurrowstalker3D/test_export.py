"""Checks actual Blender source buffers/posed mesh observations, not native import."""
import copy,hashlib,json,math,unittest
from pathlib import Path
ROOT=Path(__file__).parent
IDS=['spread-furrowstalker','spread-furrowstalker-remains']
def buffers_ok(form):
 assert form['meshCount']>0
 for mesh in form['meshes']:
  assert mesh['vertices'] and mesh['triangles']>0
  assert all(all(math.isfinite(v) for v in xyz) for xyz in mesh['vertices'])
  assert all(len(set(face))==len(face) and all(0<=i<len(mesh['vertices']) for i in face) for face in mesh['faces'])
  assert all(math.isfinite(x) and math.isfinite(y) and abs(y-.5)<.00001 and any(abs(x-(i+.5)/24)<.00001 for i in range(24)) for x,y in mesh['uv'])
  assert all(.99<sum(v*v for v in n)<1.01 for n in mesh['normals'])
  if form['rigged']:assert all(len(w)==1 and w[0][0] in form['bones'] and w[0][1]==1 for w in mesh['weights'])
  else:assert not mesh['weights']
def tail_attachment_ok(frame):
 # The child inboard face must enter the parent volume, rather than meeting
 # only at an off-centre corner. This is source continuity, not pixel proof.
 bend=frame['tailContactVertices']['tail-bend'];tip=frame['tailContactVertices']['tail-tip']
 p=[sum(tip[i][axis]for i in (0,1,4,5))/4 for axis in range(3)]
 delta=[p[i]-bend[0][i]for i in range(3)]
 for j in (1,2,4):
  edge=[bend[j][i]-bend[0][i]for i in range(3)];t=sum(x*y for x,y in zip(delta,edge))/sum(x*x for x in edge)
  assert -.00001<=t<=1.00001
class ExportTests(unittest.TestCase):
 def setUp(self):
  self.e=json.loads((ROOT/'Export/source-observations.json').read_text());self.ms={m['id']:m for m in self.e['forms']}
 def test_exact_export_files_and_hashes(self):
  c=json.loads((ROOT/'Export/catalog.json').read_text());self.assertEqual([m['id'] for m in c['models']],IDS)
  for m in c['models']:
   p=ROOT/'Export'/m['path'];self.assertGreater(p.stat().st_size,1000);self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),m['sha256'])
 def test_actual_source_geometry_buffers_and_bad_uv_counter(self):
  for m in self.ms.values():buffers_ok(m)
  m=copy.deepcopy(self.ms[IDS[0]]);m['meshes'][0]['uv'][0]=[.999,.4]
  with self.assertRaises(AssertionError):buffers_ok(m)
 def test_actual_rigid_bone_ownership_and_foreign_weight_counter(self):
  m=self.ms[IDS[0]];buffers_ok(m);self.assertEqual(len(m['bones']),11)
  m=copy.deepcopy(m);m['meshes'][0]['weights'][0]=[['foreign',1]]
  with self.assertRaises(AssertionError):buffers_ok(m)
 def test_actual_five_clips_root_fixed_and_each_deforms_geometry(self):
  m=self.ms[IDS[0]];self.assertEqual(list(m['poses']),['Idle','Walk','Interact','Attack','Hit'])
  for name,frames in m['poses'].items():
   self.assertGreater(max(f['maxVertexDeltaFromFirst'] for f in frames),.001,name)
   self.assertTrue(all(f['rootTranslation']==[0,0,0] for f in frames),name)
 def test_actual_feeding_lowers_world_muzzle_without_penetrating_floor(self):
  f=self.ms[IDS[0]]['poses']['Interact'];self.assertLess(min(x['muzzleWorldUp'] for x in f),f[0]['muzzleWorldUp']-.06)
  self.assertGreater(min(x['minWorldUp'] for x in f),-.035)
 def test_actual_attack_opens_jaw_and_returns(self):
  f=self.ms[IDS[0]]['poses']['Attack'];self.assertGreater(max(x['jawGap'] for x in f),f[0]['jawGap']+.02)
  self.assertAlmostEqual(f[0]['muzzleWorldUp'],f[-1]['muzzleWorldUp'],places=5)
 def test_tail_tip_enters_parent_section_through_every_sample_and_offset_counter(self):
  for clip,frames in self.ms[IDS[0]]['poses'].items():
   for f in frames:
    with self.subTest(clip=clip,frame=f['frame']):tail_attachment_ok(f)
  f=copy.deepcopy(self.ms[IDS[0]]['poses']['Idle'][0])
  for v in f['tailContactVertices']['tail-tip']:v[0]+=1
  with self.assertRaises(AssertionError):tail_attachment_ok(f)
 def test_actual_remains_static_and_lower_than_live_body(self):
  m=self.ms[IDS[1]];self.assertFalse(m['rigged']);self.assertEqual(m['bones'],[]);self.assertEqual(m['poses'],{})
  self.assertLess(m['bounds']['max'][2],.26);self.assertGreater(self.ms[IDS[0]]['bounds']['max'][2],m['bounds']['max'][2]+.15)
 def test_review_outputs_are_real_source_files(self):
  for name in ['furrowstalker-review.blend','renders/furrowstalker-top.png','renders/furrowstalker-oblique.png','renders/furrowstalker-poses.png']:
   self.assertGreater((ROOT/'Export'/name).stat().st_size,1000,name)
if __name__=='__main__':unittest.main(verbosity=2)
