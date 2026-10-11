"""Offline artifact-validator counters. Fake decoding is explicit; no listening/import evidence."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import verify_regional_music as validator

class RegionalMusicValidation(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
  self.spec=json.loads(validator.SPEC.read_text());self.rows=[r for r in self.spec['clips'] if r['iteration']<=16]
  self.source=self.root/'source-spec.json';self.source.write_text(json.dumps(self.spec));self.frames={}
  outputs=[]
  for i,row in enumerate(self.rows):
   p=self.root/row['path'];p.parent.mkdir(exist_ok=True);p.write_bytes(('explicit fake waveform '+row['id']).encode())
   Path(str(p)+'.meta').write_text('fileFormatVersion: 2\nguid: '+format(i+1,'032x')+'\nAudioImporter:\n  loadType: 2\n  preloadAudioData: 0\n  loadInBackground: 1\n  forceToMono: 0\n  normalize: 0\n')
   frames=self.spec['sources'][row['source']]['decodedFrames'];outputs.append(dict(row,outputBytes=p.stat().st_size,outputSha256=hashlib.sha256(p.read_bytes()).hexdigest(),sourceDecodedFrames=frames,preparedFrames=frames,sampleRate=44100,channels=2))
   self.frames[str(p)]=frames
  self.proof=dict(schemaVersion=1,throughIteration=16,sources={r['source']:self.spec['sources'][r['source']] for r in self.rows},clips=outputs)
  self.catalog=dict(schemaVersion=1,music=[dict(id=r['context'],resource='Audio/World/'+r['path'][:-4],gain=r['runtimeGain']) for r in self.rows],ambience=[],sounds=[],details=[])
  self.credits='\n'.join(r['credit']+'\n'+r['page']+'\n'+r['licenseUrl'] for r in self.rows)
  self.write();self.license=self.root/'Licenses/CC-BY-4.0.txt';self.license.parent.mkdir();self.license.write_text('Attribution 4.0 International\n'+'explicit synthetic license fixture\n'*110)
  self.probe_rate=44100;self.probe_channels=2;self.selected=None
 def write(self):
  (self.root/'regional-music-provenance.json').write_text(json.dumps(self.proof));(self.root/'regional-music-catalog.json').write_text(json.dumps(self.catalog));(self.root/'REGIONAL-MUSIC-CREDITS.txt').write_text(self.credits)
 def probe(self,args,**kwargs):
  self.selected=args[-1];return json.dumps({'streams':[{'sample_rate':str(self.probe_rate),'channels':self.probe_channels}]}).encode()
 def fake_decode(self,args,**kwargs):
  self.selected=args[args.index("-i")+1]
 def check(self):
  with patch.object(validator.subprocess,'check_output',side_effect=self.probe),patch.object(validator.subprocess,'run',side_effect=self.fake_decode),patch.object(validator,'raw_stats',side_effect=lambda _:dict(frames=self.frames[self.selected],peak=.4,rms=.1)):
   return validator.verify(self.root,self.source)
 def test_valid_exact_stage_has_existing_standard_receipt_schema(self):
  result=self.check();self.assertTrue(result['passed'],result);self.assertEqual(2,result['assetCount']);self.assertEqual([],result['findings']);self.assertFalse(result['unityImportVerified']);self.assertFalse(result['subjectiveListeningVerified']);self.assertTrue(all(r['errors']==[] for r in result['assets']))
 def test_missing_complete_license_fails(self):
  self.license.unlink();result=self.check();self.assertFalse(result['passed']);self.assertTrue(any('license' in s.lower() for s in result['findings']))
 def test_wrong_actual_channels_or_rate_fail_not_assumed_stereo(self):
  for channels,rate in [(1,44100),(2,48000)]:
   self.probe_channels=channels;self.probe_rate=rate;self.assertFalse(self.check()['passed'])
 def test_duplicate_provenance_rows_fail_before_dictionary_collapses_them(self):
  self.proof['clips'].append(copy.deepcopy(self.proof['clips'][0]));self.write();self.assertFalse(self.check()['passed'])
 def test_credits_metadata_cannot_disagree_with_the_independent_spec(self):
  self.proof['clips'][0]['credit']='different';self.write();self.assertFalse(self.check()['passed'])
 def test_unregistered_catalog_group_is_not_ignored(self):
  self.catalog['sounds']=[dict(id='Use',resource='Audio/World/Music/Cirrus',gain=.3)];self.write();self.assertFalse(self.check()['passed'])
 def test_missing_or_invalid_metadata_returns_failed_receipt(self):
  for text in ['{invalid','null']:
   (self.root/'regional-music-provenance.json').write_text(text);result=self.check();self.assertFalse(result['passed']);self.assertTrue(result['findings'])
 def test_bad_import_guid_is_reported(self):
  p=self.root/(self.rows[0]['path']+'.meta');p.write_text(p.read_text().replace(format(1,'032x'),'not-a-guid'));self.assertFalse(self.check()['passed'])

if __name__=='__main__':unittest.main(verbosity=2)
