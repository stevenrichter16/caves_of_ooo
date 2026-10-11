"""Offline production-pipeline counters; synthetic audio does not prove listening quality."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import numpy as np

HERE=Path(__file__).resolve().parent

class RegionalMusicPipeline(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  spec=importlib.util.spec_from_file_location('regional_prepare',HERE/'prepare_regional_music.py')
  cls.module=importlib.util.module_from_spec(spec);spec.loader.exec_module(cls.module)
  cls.spec=json.loads((HERE/'regional_music_sources.json').read_text())
 def test_stages_are_cumulative_without_early_music_shipping(self):
  m=self.module
  self.assertEqual(['sodden','grovelands'],[r['context'] for r in m.selected_rows(self.spec,16)])
  self.assertEqual(['sodden','grovelands','organic'],[r['context'] for r in m.selected_rows(self.spec,17)])
  self.assertEqual(['sodden','grovelands','organic','village'],[r['context'] for r in m.selected_rows(self.spec,19)])
  with self.assertRaises(ValueError):m.selected_rows(self.spec,18)
 def test_all_supplied_mixes_keep_exact_license_source_credit(self):
  self.module.validate_spec(self.spec)
  for field in ['page','credit','changes','licenseUrl']:
   bad=copy.deepcopy(self.spec);bad['clips'][0][field]=''
   with self.assertRaises(ValueError,msg=field):self.module.validate_spec(bad)
  bad=copy.deepcopy(self.spec);bad['clips'][0]['license']='unknown'
  with self.assertRaises(ValueError):self.module.validate_spec(bad)
 def test_output_paths_and_duplicate_clip_identity_are_rejected(self):
  for path in ['/outside/escape.ogg','Music/../../escape.ogg','Music/nested/escape.ogg','Music/a.mp3']:
   bad=copy.deepcopy(self.spec);bad['clips'][0]['path']=path
   with self.assertRaises(ValueError,msg=path):self.module.validate_spec(bad)
  bad=copy.deepcopy(self.spec);bad['clips'][1]['path']=bad['clips'][0]['path']
  with self.assertRaises(ValueError):self.module.validate_spec(bad)
 def test_source_bytes_and_hash_are_both_required(self):
  with tempfile.TemporaryDirectory() as folder:
   path=Path(folder)/'source';path.write_bytes(b'known')
   row={'bytes':5,'sha256':self.module.sha(path)};self.module.verify_source(path,row)
   with self.assertRaises(ValueError):self.module.verify_source(path,dict(row,bytes=4))
   with self.assertRaises(ValueError):self.module.verify_source(path,dict(row,sha256='0'*64))
 def test_attenuation_never_boosts_the_composers_dynamics(self):
  self.assertEqual(1,self.module.music_gain(.1));self.assertEqual(1,self.module.music_gain(.75))
  self.assertAlmostEqual(.75,self.module.music_gain(1));self.assertAlmostEqual(.375,self.module.music_gain(2))
  for peak in [0,-1,float('nan'),float('inf')]:
   with self.assertRaises(ValueError):self.module.music_gain(peak)
 def test_signal_analysis_rejects_silence_nonfinite_and_partial_frames(self):
  with tempfile.TemporaryDirectory() as folder:
   path=Path(folder)/'audio.f32';signal=np.tile(np.array([.1,-.1],dtype='<f4'),100)
   signal.tofile(path);stats=self.module.raw_stats(path);self.assertEqual(100,stats['frames']);self.assertAlmostEqual(.1,stats['peak'])
   for data in [signal*0,np.array([float('nan'),.1],dtype='<f4'),signal[:-1]]:
    data.tofile(path)
    with self.assertRaises(ValueError):self.module.raw_stats(path)
 def test_meta_preserves_identity_and_streaming_contract(self):
  with tempfile.TemporaryDirectory() as folder:
   path=Path(folder)/'track.ogg';path.write_bytes(b'placeholder');self.module.write_meta(path,audio=True)
   meta=Path(str(path)+'.meta');original=meta.read_text();self.module.write_meta(path,audio=True);self.assertEqual(original,meta.read_text())
   for value in ['loadType: 2','preloadAudioData: 0','loadInBackground: 1','normalize: 0','forceToMono: 0']:self.assertIn(value,original)

if __name__=='__main__':unittest.main(verbosity=2)
