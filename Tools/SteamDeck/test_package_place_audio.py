import copy
import importlib.util
import json
import pathlib
import shutil
import tempfile
import unittest
import test_package_performance_audio as historical
SCRIPT=pathlib.Path(__file__).with_name('package-place-audio.py')
def module():
 spec=importlib.util.spec_from_file_location('pack_places',SCRIPT)
 value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value
class ReceiptGates(historical.Gates):
 @classmethod
 def setUpClass(cls):cls.pack=module()
class ExpansionGates(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.pack=module();cls.repo=SCRIPT.resolve().parents[2]
  cls.base=json.loads((cls.repo/'Docs/Verification/PlaceAudio/original22-green.json').read_text())
  cls.place=json.loads((cls.repo/'Docs/Verification/PlaceAudio/asset-contract-green.json').read_text())
 def test_real_union53_passes(self):
  r=self.pack.validate_audio(self.repo,self.base,self.place)
  self.assertEqual(53,r['asset_count']);self.assertEqual(31,r['expansion']['asset_count'])
 def test_invalid_expansion_receipts_rejected(self):
  for mutate in [lambda r:r.update(passed=False),lambda r:r.update(assetCount=22),lambda r:r.update(audioBytes=r['audioBytes']+1),lambda r:r['assets'][0].update(errors=['bad']),lambda r:r['assets'].__setitem__(1,r['assets'][0])]:
   r=copy.deepcopy(self.place);mutate(r)
   with self.assertRaises(RuntimeError):self.pack.validate_audio(self.repo,self.base,r)
 def test_legacy_failure_still_rejected(self):
  r=copy.deepcopy(self.base);r['passed']=False
  with self.assertRaises(RuntimeError):self.pack.validate_audio(self.repo,r,self.place)
 def test_actual_mutations_rejected_and_restored_positive(self):
  with tempfile.TemporaryDirectory(prefix='coo-place-package-counter-') as td:
   repo=pathlib.Path(td);world=repo/'Assets/Resources/Audio/World'
   shutil.copytree(self.repo/'Assets/Resources/Audio/World',world);(repo/'Tools/Audio').mkdir(parents=True)
   for name in ['world_audio_sources.json','place_audio_sources.json']:shutil.copyfile(self.repo/'Tools/Audio'/name,repo/'Tools/Audio'/name)
   probes=[('PLACES-CREDITS.txt',lambda b:b.replace(b'EZduzziteh',b'REMOVED')),
           ('Licenses/CC0-1.0.txt',lambda b:b[:-1]),
           ('Places/frog_01.ogg',lambda b:b[:-1]),
           ('Places/frog_01.ogg.meta',lambda b:b.replace(b'loadType: 0',b'loadType: 2')),
           ('places-catalog.json',lambda b:b.replace(b'"id": "marsh"',b'"id": "phantom"')),
           ('places-provenance.json',lambda b:b.replace(b'"startSeconds": 20',b'"startSeconds": 21')),
           ('catalog.json',lambda b:b.replace(b'Audio/World/Music/Ephemera',b'Audio/World/Music/Absent'))]
   for name,mutate in probes:
    with self.subTest(name=name):
     path=world/name;old=path.read_bytes();path.write_bytes(mutate(old))
     try:
      with self.assertRaises(RuntimeError):self.pack.validate_audio(repo,self.base,self.place)
     finally:path.write_bytes(old)
   extra=world/'Places/unknown.ogg';shutil.copyfile(world/'Places/frog_01.ogg',extra)
   with self.assertRaises(RuntimeError):self.pack.validate_audio(repo,self.base,self.place)
   extra.unlink();self.assertEqual(53,self.pack.validate_audio(repo,self.base,self.place)['asset_count'])
 def test_credits_merge_preserves_both_complete_texts(self):
  world=self.repo/'Assets/Resources/Audio/World'
  value=self.pack.combined_credits(world)
  self.assertIn((world/'CREDITS.txt').read_text(),value);self.assertIn((world/'PLACES-CREDITS.txt').read_text(),value)
if __name__=='__main__':unittest.main(verbosity=2)
