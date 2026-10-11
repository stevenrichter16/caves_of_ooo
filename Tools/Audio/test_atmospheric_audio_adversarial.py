"""Temporary-copy mutation counters; never changes shared Assets."""
import json
from pathlib import Path
import shutil
import tempfile
import unittest
import numpy as np
from verify_atmospheric_audio import verify, signal_errors
from test_atmospheric_audio import WORLD
class AtmosphereCounterchecks(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory(prefix='coo-atmos-counter-');self.root=Path(self.temp.name)/'World';shutil.copytree(WORLD,self.root)
  self.assertTrue(verify(self.root,decode=False)['passed'])
 def tearDown(self):self.temp.cleanup()
 def rejected(self,needle):
  r=verify(self.root,decode=False);self.assertFalse(r['passed']);self.assertTrue(any(needle in s for s in r['findings']),r['findings'])
 def editjson(self,name,change):
  p=self.root/name;r=json.loads(p.read_text());change(r);p.write_text(json.dumps(r))
 def test_missing_asset(self):
  (self.root/'Atmospheric/water_drop_01.ogg').unlink();self.rejected('missing asset')
 def test_changed_audio_bytes(self):
  (self.root/'Atmospheric/work_forge_01.ogg').write_bytes(b'not audio');self.rejected('output hash')
 def test_wrong_source_member(self):
  self.editjson('atmospheric-provenance.json',lambda r:r['clips'][0].update(member='renamed-old-take.ogg'));self.rejected('provenance member')
 def test_wrong_source_hash(self):
  self.editjson('atmospheric-provenance.json',lambda r:next(iter(r['sources'].values())).update(sha256='0'*64));self.rejected('source provenance')
 def test_unapproved_catalog_id(self):
  self.editjson('atmospheric-catalog.json',lambda r:r['details'][0].update(id='fake-animals'));self.rejected('catalog')
 def test_bad_gain(self):
  self.editjson('atmospheric-catalog.json',lambda r:r['sounds'][0].update(gain=2));self.rejected('catalog')
 def test_wrong_streaming_importer(self):
  p=self.root/'Atmospheric/reed_rustle_01.ogg.meta';p.write_text(p.read_text().replace('loadType: 0','loadType: 2'));self.rejected('loadType')
 def test_duplicate_guid(self):
  shutil.copyfile(self.root/'Atmospheric/water_drop_01.ogg.meta',self.root/'Atmospheric/water_drop_02.ogg.meta');self.rejected('duplicate GUID')
 def test_missing_credit(self):
  (self.root/'ATMOSPHERIC-CREDITS.txt').write_text('');self.rejected('missing credit/source')
 def test_missing_license(self):
  (self.root/'Licenses/CC0-1.0.txt').unlink();self.rejected('missing complete CC0')
 def test_undeclared_third_palette_clip(self):
  shutil.copyfile(self.root/'Atmospheric/water_drop_01.ogg',self.root/'Atmospheric/extra.ogg');self.rejected('unreferenced')
 def test_missing_provenance_row(self):
  self.editjson('atmospheric-provenance.json',lambda r:r['clips'].pop());self.rejected('provenance clip count')
class AtmosphereSignalCounterchecks(unittest.TestCase):
 def test_detail_under_shared_four_second_gap(self):
  safe=np.full(round(3.5*44100),.1,np.float32)
  self.assertEqual([],signal_errors(safe,44100,1,'detail')[0])
  too_long=np.full(round(3.6*44100),.1,np.float32)
  self.assertIn('duration',signal_errors(too_long,44100,1,'detail')[0])

 def test_good_phrase_and_signal_corruption(self):
  a=(.1*np.sin(np.arange(44100)*.02)).astype(np.float32)
  self.assertEqual([],signal_errors(a,44100,1,'detail')[0])
  for bad,rate,ch in [(a*0,44100,1),(a*10,44100,1),(a,22050,1),(a,44100,2),(np.tile(a,5),44100,1)]:
   self.assertTrue(signal_errors(bad,rate,ch,'detail')[0])
  a[20]=np.nan;self.assertTrue(signal_errors(a,44100,1,'detail')[0])
if __name__=='__main__':unittest.main(verbosity=2)
