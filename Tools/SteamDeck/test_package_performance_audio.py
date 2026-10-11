import importlib.util
import json
import copy
import shutil
import pathlib
import tempfile
import unittest
import xml.etree.ElementTree as ET
SCRIPT=pathlib.Path(__file__).with_name('package-performance-audio.py')
def module():
 spec=importlib.util.spec_from_file_location('pack',SCRIPT)
 value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value
class Gates(unittest.TestCase):
 @classmethod
 def setUpClass(cls): cls.pack=module()
 def native(self):return dict(mode='world-audio',complete=True,errorsFinalized=True,failures=0,unexpectedErrors=0,runId='proof',cases=2,audit=['PASS first','PASS second'],canVerify=['observed'],cannotVerify=['not hardware'])
 def test_native_positive(self):self.pack.validate_native(self.native())
 def test_native_rejects_wrong_mode(self):
  value=self.native();value['mode']='system-depth-ii-connections'
  with self.assertRaises(RuntimeError):self.pack.validate_native(value)
 def test_native_rejects_unfinalized(self):
  value=self.native();value['errorsFinalized']=False
  with self.assertRaises(RuntimeError):self.pack.validate_native(value)
 def test_native_rejects_false_count(self):
  value=self.native();value['cases']=3
  with self.assertRaises(RuntimeError):self.pack.validate_native(value)
 def test_native_rejects_failed_individual(self):
  value=self.native();value['audit'][1]='FAIL second'
  with self.assertRaises(RuntimeError):self.pack.validate_native(value)
 def test_native_rejects_missing_honesty(self):
  value=self.native();del value['cannotVerify']
  with self.assertRaises(RuntimeError):self.pack.validate_native(value)
 def nunit_fixture(self):return ET.fromstring('<test-run result="Passed" total="2" passed="1" failed="0" skipped="1" inconclusive="0"><test-case result="Passed"/><test-case result="Skipped"/></test-run>')
 def test_nunit_positive_retains_skip(self):self.assertEqual(1,self.pack.validate_tests(self.nunit_fixture())['skipped'])
 def test_nunit_rejects_false_count(self):
  value=self.nunit_fixture();value.set('total','3')
  with self.assertRaises(RuntimeError):self.pack.validate_tests(value)
 def test_nunit_rejects_failed_individual(self):
  value=self.nunit_fixture();list(value)[0].set('result','Failed')
  with self.assertRaises(RuntimeError):self.pack.validate_tests(value)
 def test_nunit_rejects_inconclusive(self):
  value=self.nunit_fixture();value.set('inconclusive','1')
  with self.assertRaises(RuntimeError):self.pack.validate_tests(value)
class AudioGates(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.pack=module();cls.repo=SCRIPT.resolve().parents[2]
  cls.receipt=json.loads((cls.repo/'Docs/Verification/WorldAudio/asset-contract-green.json').read_text())
 def test_real_22_asset_set_passes_without_writing_assets(self):
  self.assertEqual(22,self.pack.validate_audio(self.repo,self.receipt)['asset_count'])
 def test_bad_summary_rejected(self):
  receipt=copy.deepcopy(self.receipt);receipt['passed']=False
  with self.assertRaises(RuntimeError):self.pack.validate_audio(self.repo,receipt)
 def test_duplicate_asset_receipt_rejected(self):
  receipt=copy.deepcopy(self.receipt);receipt['assets'][1]=receipt['assets'][0]
  with self.assertRaises(RuntimeError):self.pack.validate_audio(self.repo,receipt)
 def test_individual_error_rejected(self):
  receipt=copy.deepcopy(self.receipt);receipt['assets'][0]['errors']=['signal error']
  with self.assertRaises(RuntimeError):self.pack.validate_audio(self.repo,receipt)
 def test_wrong_audio_byte_count_rejected(self):
  receipt=copy.deepcopy(self.receipt);receipt['audioBytes']+=1
  with self.assertRaises(RuntimeError):self.pack.validate_audio(self.repo,receipt)
 def test_current_asset_license_catalog_and_importer_mutations_rejected(self):
  # Temporary copies only: never mutate the shared Assets tree or package a player.
  with tempfile.TemporaryDirectory(prefix='coo-audio-package-counter-') as td:
   repo=pathlib.Path(td);world=repo/'Assets/Resources/Audio/World'
   shutil.copytree(self.repo/'Assets/Resources/Audio/World',world)
   (repo/'Tools/Audio').mkdir(parents=True)
   shutil.copyfile(self.repo/'Tools/Audio/world_audio_sources.json',repo/'Tools/Audio/world_audio_sources.json')
   probes=[('CREDITS.txt',lambda b:b.replace(b"'Ephemera'",b"'Removed'")),
           ('Licenses/CC-BY-4.0.txt',lambda b:b[:-1]),
           ('Music/Ephemera.ogg',lambda b:b[:-1]),
           ('Music/Ephemera.ogg.meta',lambda b:b.replace(b'loadType: 2',b'loadType: 0')),
           ('catalog.json',lambda b:b.replace(b'Audio/World/Music/Ephemera',b'Audio/World/Music/Absent'))]
   for name,mutate in probes:
    with self.subTest(name=name):
     path=world/name;old=path.read_bytes();path.write_bytes(mutate(old))
     try:
      with self.assertRaises(RuntimeError):self.pack.validate_audio(repo,self.receipt)
     finally:path.write_bytes(old)
   self.assertEqual(22,self.pack.validate_audio(repo,self.receipt)['asset_count'])
if __name__=='__main__':unittest.main(verbosity=2)
