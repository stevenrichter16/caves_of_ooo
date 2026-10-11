"""Actual pinned-cache offline pipeline checks; never write the baseline world."""
import copy, hashlib, importlib.util, json, os, pathlib, shutil, tempfile, unittest
HERE=pathlib.Path(__file__).resolve().parent
def load(name):
 s=importlib.util.spec_from_file_location(name,HERE/(name+'.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
class UtilityPipeline(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.prep=load('prepare_utility_audio');cls.verify=load('verify_utility_audio');cls.spec=HERE/'utility_audio_sources.json'
  cls.cache=pathlib.Path(os.environ['COO_AUDIO_SOURCE_CACHE']);cls.baseline=pathlib.Path(os.environ['COO_AUDIO_BASELINE_WORLD'])
  cls.tmp=tempfile.TemporaryDirectory(prefix='coo-portable-utility-tests-');cls.addClassCleanup(cls.tmp.cleanup);cls.root=pathlib.Path(cls.tmp.name)
  cls.stage10=cls.root/'10';cls.stage14=cls.root/'14'
  cls.prep.prepare_assets(cls.spec,cls.cache,cls.stage10,10,cls.baseline)
  cls.prep.prepare_assets(cls.spec,cls.cache,cls.stage14,14,cls.baseline,cls.stage10)
 def test_every_phase_selects_exactly_its_cumulative_palette(self):
  for n,count in [(10,2),(11,3),(12,4),(13,7),(14,8)]:
   s=self.prep.selected_spec(self.spec,n);self.assertEqual(count,len(s['clips']));self.assertTrue(all(r['iteration']<=n for r in s['clips']))
 def test_early_stage_contains_no_future_audio_or_credit(self):
  self.assertEqual(2,len(list((self.stage10/'Utility').glob('*.ogg'))));s=json.loads((self.stage10/'utility-catalog.json').read_text());self.assertEqual({'WaterFill','WaterPour','WaterDrink'},{r['id'] for r in s['sounds']});self.assertNotIn('CookPrepare',(self.stage10/'utility-provenance.json').read_text())
 def test_actual_decode_full_receipt_and_baseline_preservation(self):
  r=self.verify.verify(self.stage14,self.spec,14,self.baseline);self.assertTrue(r['passed'],r);self.assertEqual(8,r['assetCount']);self.assertEqual([],r['findings']);self.assertFalse(r['unityImportVerified']);self.assertFalse(r['subjectiveListeningVerified']);self.assertEqual(73,r['priorAssetCount'])
 def test_previous_clip_bytes_and_unity_identities_are_preserved(self):
  for p in (self.stage10/'Utility').glob('*'):
   self.assertEqual(p.read_bytes(),(self.stage14/'Utility'/p.name).read_bytes())
  for name in ('Utility.meta','utility-catalog.json.meta','utility-provenance.json.meta','UTILITY-CREDITS.txt.meta'):
   self.assertEqual((self.stage10/name).read_bytes(),(self.stage14/name).read_bytes())
 def test_wrong_phase_and_future_clip_are_refused(self):
  self.assertFalse(self.verify.verify(self.stage14,self.spec,10,self.baseline,decode=False)['passed'])
  with tempfile.TemporaryDirectory(prefix='utility-extra-') as d:
   root=pathlib.Path(d);shutil.copytree(self.stage10,root,dirs_exist_ok=True);shutil.copyfile(self.stage14/'Utility/cook_prepare_01.ogg',root/'Utility/future.ogg');self.assertFalse(self.verify.verify(root,self.spec,10,self.baseline,decode=False)['passed'])
 def test_modified_clip_meta_catalog_and_credits_are_refused_with_positive_restore(self):
  with tempfile.TemporaryDirectory(prefix='utility-mutations-') as d:
   root=pathlib.Path(d);shutil.copytree(self.stage10,root,dirs_exist_ok=True)
   for name,change in [('Utility/water_fill_01.ogg',lambda b:b[:-1]),('Utility/water_fill_01.ogg.meta',lambda b:b.replace(b'loadType: 0',b'loadType: 2')),('utility-catalog.json',lambda b:b.replace(b'WaterDrink',b'FakeDrink')),('UTILITY-CREDITS.txt',lambda b:b.replace(b'Vehicle',b'Removed'))]:
    p=root/name;old=p.read_bytes();new=change(old);self.assertNotEqual(old,new);p.write_bytes(new);self.assertFalse(self.verify.verify(root,self.spec,10,self.baseline,decode=False)['passed'],name);p.write_bytes(old)
   self.assertTrue(self.verify.verify(root,self.spec,10,self.baseline,decode=False)['passed'])
 def test_invalid_phase_refused_before_output(self):
  for n in [9,15,True,10.5]:
   with self.assertRaises(ValueError):self.prep.selected_spec(self.spec,n)
 def test_output_cannot_be_the_live_baseline_world(self):
  with self.assertRaises(ValueError):self.prep.prepare_assets(self.spec,self.cache,self.baseline,10,self.baseline)
 def test_previous_tampered_source_is_not_silently_regenerated(self):
  with tempfile.TemporaryDirectory(prefix='utility-bad-previous-') as d:
   root=pathlib.Path(d);shutil.copytree(self.stage10,root/'previous');p=root/'previous/Utility/water_fill_01.ogg';p.write_bytes(p.read_bytes()[:-1])
   with self.assertRaises(ValueError):self.prep.prepare_assets(self.spec,self.cache,root/'next',11,self.baseline,root/'previous')
 def test_copy_map_omits_shared_old_folders_and_future_clips(self):
  rows=json.loads((self.stage10/'copy-map.json').read_text())['resources'];targets=[r['destination'] for r in rows]
  self.assertTrue(all(s.startswith('Assets/Resources/Audio/World/') for s in targets));self.assertFalse(any('/Music/' in s or '/Licenses/' in s for s in targets));self.assertEqual(11,len(targets))
 def test_changed_cached_archive_is_rejected_before_output(self):
  with tempfile.TemporaryDirectory(prefix='utility-bad-cache-') as d:
   root=pathlib.Path(d);cache=root/'cache';cache.mkdir();(cache/'tinysized.zip').write_bytes(b'changed archive')
   with self.assertRaises(ValueError):self.prep.prepare_assets(self.spec,cache,root/'next',10,self.baseline)
   self.assertFalse((root/'next').exists())
 def test_selected_member_hash_change_is_rejected(self):
  with tempfile.TemporaryDirectory(prefix='utility-bad-member-') as d:
   root=pathlib.Path(d);spec=json.loads(self.spec.read_text());spec['clips'][0]['memberSha256']='0'*64;bad=root/'spec.json';bad.write_text(json.dumps(spec))
   with self.assertRaises(ValueError):self.prep.prepare_assets(bad,self.cache,root/'next',10,self.baseline)
   self.assertFalse(list((root/'next').rglob('*.ogg')))
 def test_symlink_output_cannot_write_shared_utility_target(self):
  with tempfile.TemporaryDirectory(prefix='utility-alias-') as d:
   root=pathlib.Path(d);stage=root/'next';stage.mkdir();original=root/'shared';original.mkdir();(stage/'Utility').symlink_to(original,target_is_directory=True)
   with self.assertRaises(ValueError):self.prep.prepare_assets(self.spec,self.cache,stage,10,self.baseline)
   self.assertEqual([],list(original.iterdir()))
 def test_duplicate_unity_identity_is_rejected_with_positive_restore(self):
  with tempfile.TemporaryDirectory(prefix='utility-guid-') as d:
   root=pathlib.Path(d);shutil.copytree(self.stage10,root,dirs_exist_ok=True);first=root/'Utility/water_fill_01.ogg.meta';second=root/'Utility/water_pour_01.ogg.meta';old=second.read_bytes();second.write_bytes(first.read_bytes())
   self.assertFalse(self.verify.verify(root,self.spec,10,self.baseline,decode=False)['passed']);second.write_bytes(old);self.assertTrue(self.verify.verify(root,self.spec,10,self.baseline,decode=False)['passed'])
if __name__=='__main__':unittest.main(verbosity=2)
