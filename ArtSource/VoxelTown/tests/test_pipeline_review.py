"""Independent CLI counterchecks: incompatible requests must not succeed silently."""
import contextlib,io,unittest,tempfile
from pathlib import Path
from town_generator.generate import main

class CliPipelineReview(unittest.TestCase):
 def test_data_only_succeeds_and_creates_only_semantic_output(self):
  with tempfile.TemporaryDirectory() as folder, contextlib.redirect_stdout(io.StringIO()):
   main(['--data-only','--output',folder,'--building-count','0','--population','0'])
   self.assertTrue((Path(folder)/'town.json').exists())
   self.assertFalse((Path(folder)/'town.blend').exists())
 def test_data_only_and_render_are_rejected_before_output_mutation(self):
  with tempfile.TemporaryDirectory() as folder, contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
   with self.assertRaises(SystemExit) as caught:
    main(['--data-only','--render','--output',folder,'--building-count','0','--population','0'])
   self.assertEqual(caught.exception.code,2)
   self.assertEqual(list(Path(folder).iterdir()),[])
 def test_data_only_and_fbx_are_rejected_before_output_mutation(self):
  with tempfile.TemporaryDirectory() as folder, contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
   with self.assertRaises(SystemExit) as caught:
    main(['--data-only','--export-fbx','--output',folder,'--building-count','0','--population','0'])
   self.assertEqual(caught.exception.code,2)
   self.assertEqual(list(Path(folder).iterdir()),[])

# Publication hypotheses use small semantic towns and a file-writing bpy double;
# this isolates filesystem behavior from GPU/rendering availability.
import builtins,hashlib,json,os
from types import SimpleNamespace
from unittest.mock import patch
from town_generator import generate

class PublicationTests(unittest.TestCase):
 def args(self,folder,seed=41,extra=()):
  return ['--output',str(folder),'--building-count','0','--population','0','--seed',str(seed),*extra]
 def fake_blender(self,fail=None,log=''):
  scene=SimpleNamespace(objects=[],cycles=SimpleNamespace(samples=0),render=SimpleNamespace(filepath=''),name='MockTown')
  def save(**kw):
   if fail=='save':raise RuntimeError('save failed')
   Path(kw['filepath']).write_bytes(b'BLEND '+scene.render.filepath.encode());return {'FINISHED'}
  def export(**kw):
   print(log,end='')
   if fail=='export':raise RuntimeError('export failed')
   Path(kw['filepath']).write_bytes(b'FBX');return {'FINISHED'}
  def render(**kw):
   if fail=='render':raise RuntimeError('render failed')
   Path(scene.render.filepath).write_bytes(b'PNG');return {'FINISHED'}
  api=SimpleNamespace(context=SimpleNamespace(window=SimpleNamespace(scene=None)),data=SimpleNamespace(screens=[]),
      ops=SimpleNamespace(wm=SimpleNamespace(save_as_mainfile=save),export_scene=SimpleNamespace(fbx=export),render=SimpleNamespace(render=render)))
  def realize(town):
   if fail=='plan':raise ValueError('planning failed')
   return scene,{'schema':1,'seed':town.config.seed},{'buildings':0}
  return api,realize
 def run_fake(self,folder,seed=41,fail=None,extra=('--render','--export-fbx'),log=''):
  api,realize=self.fake_blender(fail,log)
  with patch.dict('sys.modules',{'bpy':api}),patch('town_generator.scene.realize',side_effect=realize),contextlib.redirect_stdout(io.StringIO()):
   main(self.args(folder,seed,extra))
 def snapshot(self,p):return {str(f.relative_to(p)):f.read_bytes() for f in p.rglob('*') if f.is_file()}
 def existing(self,root):
  folder=root/'town';folder.mkdir();(folder/'manual-notes.txt').write_text('Do not alter')
  self.run_fake(folder);return folder,self.snapshot(folder)
 def assert_failure_preserves_bundle(self,phase):
  with tempfile.TemporaryDirectory() as temp:
   root=Path(temp);folder,before=self.existing(root)
   with self.assertRaisesRegex((RuntimeError,ValueError),phase if phase!='plan' else 'planning'):
    self.run_fake(folder,42,fail=phase)
   self.assertEqual(self.snapshot(folder),before)
   self.assertEqual(set(root.iterdir()),{folder},'Owned staging directory leaked')
 def test_planning_failure_preserves_existing_bundle(self):self.assert_failure_preserves_bundle('plan')
 def test_export_failure_preserves_existing_bundle(self):self.assert_failure_preserves_bundle('export')
 def test_render_failure_preserves_existing_bundle(self):self.assert_failure_preserves_bundle('render')
 def test_save_failure_preserves_existing_bundle(self):self.assert_failure_preserves_bundle('save')
 def test_success_publishes_complete_hash_manifest_and_preserves_manual_file(self):
  with tempfile.TemporaryDirectory() as temp:
   folder,before=self.existing(Path(temp));self.run_fake(folder,42)
   bundle=json.loads((folder/'bundle.json').read_text())
   self.assertEqual(bundle['seed'],42);self.assertEqual(bundle['mode'],'blender')
   self.assertEqual(set(bundle['files']),{'town.json','town.blend','town.fbx','town.voxels.json.gz','gameplay.png','stats.json','fbx-export.log'})
   for name,record in bundle['files'].items():
    data=(folder/name).read_bytes();self.assertEqual(record['sha256'],hashlib.sha256(data).hexdigest());self.assertEqual(record['bytes'],len(data))
   self.assertEqual((folder/'manual-notes.txt').read_bytes(),before['manual-notes.txt'])
   self.assertNotEqual((folder/'town.json').read_bytes(),before['town.json'])
   self.assertIn(str(folder/'gameplay.png').encode(),(folder/'town.blend').read_bytes())
   self.assertEqual(set(Path(temp).iterdir()),{folder})
 def test_unrequested_previous_optional_outputs_are_preserved_but_not_claimed_current(self):
  with tempfile.TemporaryDirectory() as temp:
   folder,before=self.existing(Path(temp));self.run_fake(folder,42,extra=())
   bundle=json.loads((folder/'bundle.json').read_text());stats=json.loads((folder/'stats.json').read_text())
   for name in ['gameplay.png','town.fbx','fbx-export.log']:
    self.assertEqual((folder/name).read_bytes(),before[name]);self.assertNotIn(name,bundle['files']);self.assertIn(name,bundle['retainedPreviousArtifacts'])
   self.assertFalse(stats['artifacts']['render']);self.assertFalse(stats['artifacts']['fbx'])
 def test_data_only_never_imports_bpy_and_marks_prior_blend_not_current(self):
  with tempfile.TemporaryDirectory() as temp:
   folder,before=self.existing(Path(temp));original=builtins.__import__
   def guarded(name,*a,**kw):
    if name=='bpy':raise AssertionError('Data-only imported bpy')
    return original(name,*a,**kw)
   with patch('builtins.__import__',side_effect=guarded),contextlib.redirect_stdout(io.StringIO()):main(self.args(folder,42,('--data-only',)))
   bundle=json.loads((folder/'bundle.json').read_text());stats=json.loads((folder/'stats.json').read_text())
   self.assertEqual(set(bundle['files']),{'town.json','stats.json'})
   self.assertEqual(stats['mode'],'data-only');self.assertFalse(stats['artifacts']['blend'])
   self.assertEqual((folder/'town.blend').read_bytes(),before['town.blend'])
   self.assertIn('town.blend',bundle['retainedPreviousArtifacts'])
 def test_publish_failure_rolls_back_partial_replacements(self):
  with tempfile.TemporaryDirectory() as temp:
   root=Path(temp);folder,before=self.existing(root);replace=os.replace
   def reject(source,target):
    if Path(target)==folder.resolve()/'town.json' and Path(source).parent.name!='previous':raise OSError('publish failed')
    return replace(source,target)
   with patch('os.replace',side_effect=reject),self.assertRaisesRegex(OSError,'publish failed'):self.run_fake(folder,42)
   self.assertEqual(self.snapshot(folder),before);self.assertEqual(set(root.iterdir()),{folder})
 def test_unrelated_directory_named_as_artifact_rejects_before_replacing_any_file(self):
  with tempfile.TemporaryDirectory() as temp:
   folder=Path(temp)/'town';folder.mkdir();(folder/'town.json').mkdir();(folder/'town.json'/'manual').write_text('leave me')
   before=self.snapshot(folder)
   with self.assertRaises((ValueError,OSError)):self.run_fake(folder)
   self.assertEqual(self.snapshot(folder),before)
 def test_fbx_raw_log_retains_both_known_and_unrelated_warnings(self):
  text="WARNING: Cannot register a valid material index for 'shared'\nWARNING: A different exporter warning\n"
  with tempfile.TemporaryDirectory() as temp:
   folder=Path(temp)/'town';self.run_fake(folder,log=text)
   self.assertEqual((folder/'fbx-export.log').read_text(),text)

 def test_cancelled_render_preserves_bundle_even_without_python_exception(self):
  with tempfile.TemporaryDirectory() as temp:
   folder,before=self.existing(Path(temp));api,realize=self.fake_blender()
   api.ops.render.render=lambda **kw:{'CANCELLED'}
   with patch.dict('sys.modules',{'bpy':api}),patch('town_generator.scene.realize',side_effect=realize),contextlib.redirect_stdout(io.StringIO()),self.assertRaisesRegex(RuntimeError,'Render did not finish'):
    main(self.args(folder,42,('--render',)))
   self.assertEqual(self.snapshot(folder),before)
 def test_missing_export_file_rejects_success_return_without_mutating_destination(self):
  with tempfile.TemporaryDirectory() as temp:
   folder,before=self.existing(Path(temp));api,realize=self.fake_blender()
   api.ops.export_scene.fbx=lambda **kw:{'FINISHED'}
   with patch.dict('sys.modules',{'bpy':api}),patch('town_generator.scene.realize',side_effect=realize),contextlib.redirect_stdout(io.StringIO()),self.assertRaises(FileNotFoundError):
    main(self.args(folder,42,('--export-fbx',)))
   self.assertEqual(self.snapshot(folder),before)
 def test_failed_first_publication_removes_only_own_new_files(self):
  with tempfile.TemporaryDirectory() as temp:
   root=Path(temp);folder=root/'new-town';manual=root/'manual';manual.write_text('keep');replace=os.replace
   def reject(source,target):
    if Path(target).name=='town.json':raise OSError('publish failed')
    return replace(source,target)
   with patch('os.replace',side_effect=reject),self.assertRaisesRegex(OSError,'publish failed'):self.run_fake(folder)
   self.assertFalse(folder.exists());self.assertEqual(set(root.iterdir()),{manual});self.assertEqual(manual.read_text(),'keep')
 def test_replace_owned_filename_symlink_does_not_modify_external_file(self):
  with tempfile.TemporaryDirectory() as temp:
   root=Path(temp);external=root/'manual';external.write_text('external original')
   folder=root/'town';folder.mkdir();(folder/'town.json').symlink_to(external)
   self.run_fake(folder)
   self.assertEqual(external.read_text(),'external original');self.assertFalse((folder/'town.json').is_symlink())
   self.assertEqual(json.loads((folder/'town.json').read_text())['config']['seed'],41)
 def export_console(self,layouts):
  from town_generator.scene import OWNER
  class Obj:
   type='MESH'
   def __init__(self,data,materials):self.data=data;self.material_slots=[SimpleNamespace(material=m) for m in materials];self.selected=True
   def get(self,key):return OWNER
   def select_get(self):return self.selected
   def select_set(self,v):self.selected=v
  warning="WARNING: Cannot register a valid material index for 'shared'\nWARNING: Unrelated warning stays visible\n"
  mesh=object();objects=[Obj(mesh,layout) for layout in layouts]
  api,_=self.fake_blender(log=warning);out=io.StringIO()
  with tempfile.TemporaryDirectory() as temp,contextlib.redirect_stdout(out):
   generate._export_fbx(api,SimpleNamespace(objects=objects),Path(temp))
   self.assertEqual((Path(temp)/'fbx-export.log').read_text(),warning)
  self.assertTrue(all(o.selected for o in objects),'Export changed original selection')
  return out.getvalue()
 def test_duplicate_warning_aggregation_requires_matching_shared_slot_layouts(self):
  console=self.export_console([('a','b'),('a','b')])
  self.assertNotIn('WARNING: Cannot register',console);self.assertIn('1 duplicate shared-mesh',console)
  self.assertIn('WARNING: Unrelated warning stays visible',console)
 def test_different_shared_slot_layouts_leave_all_warnings_visible(self):
  console=self.export_console([('a','b'),('b','a')])
  self.assertIn('WARNING: Cannot register',console);self.assertIn('WARNING: Unrelated warning stays visible',console)
  self.assertNotIn('identical slot layouts verified',console)
 def test_without_shared_mesh_borrowers_registration_warning_is_not_assumed_safe(self):
  console=self.export_console([('a','b')])
  self.assertIn('WARNING: Cannot register',console)

 def test_staged_blend_save_is_a_copy_not_a_deleted_active_document_path(self):
  with tempfile.TemporaryDirectory() as temp:
   folder=Path(temp)/'town';api,realize=self.fake_blender();save=api.ops.wm.save_as_mainfile
   with patch.object(api.ops.wm,'save_as_mainfile',wraps=save) as observed,patch.dict('sys.modules',{'bpy':api}),patch('town_generator.scene.realize',side_effect=realize),contextlib.redirect_stdout(io.StringIO()):
    main(self.args(folder,extra=()))
   self.assertTrue(observed.call_args.kwargs.get('copy',False),'Active Blender filepath must not become the deleted staging path')

if __name__=='__main__':unittest.main()
