"""Third palette contract: real new takes, preserved old53 and explicit provenance."""
import hashlib
import json
from pathlib import Path
import unittest
REPO=Path(__file__).resolve().parents[2]
WORLD=REPO/'Assets/Resources/Audio/World'
DETAILS={'leaf-rustle','reed-rustle','wood-settle','stone-shift','water-drop'}
SOUNDS={'WorkForge','WorkRepairWood','WorkRepairStone','WorkRepairMetal','HarvestPlant'}
class AtmosphereAssets(unittest.TestCase):
 def data(self):
  p=WORLD/'atmospheric-catalog.json';self.assertTrue(p.is_file(),'Third atmosphere catalog missing')
  return json.loads(p.read_text())
 def test_ids_are_bounded_and_have_two_distinct_takes(self):
  c=self.data();self.assertFalse(c['music']);self.assertFalse(c['ambience'])
  self.assertEqual(DETAILS,{r['id'] for r in c['details']});self.assertEqual(SOUNDS,{r['id'] for r in c['sounds']})
  for group in ['details','sounds']:
   for cue in {r['id'] for r in c[group]}:
    paths=[REPO/'Assets/Resources'/(r['resource']+'.ogg') for r in c[group] if r['id']==cue]
    self.assertEqual(2,len(paths));self.assertTrue(all(p.is_file() for p in paths));self.assertNotEqual(*[hashlib.sha256(p.read_bytes()).hexdigest() for p in paths])
 def test_new_bytes_and_source_members_are_not_old_renames(self):
  self.data();spec=json.loads((REPO/'Tools/Audio/atmospheric_audio_sources.json').read_text())
  prior=[]
  for name in ['provenance.json','places-provenance.json']:prior+=json.loads((WORLD/name).read_text())['clips']
  oldmembers={(r['source'],r.get('member')) for r in prior};oldbytes={r['outputSha256'] for r in prior}
  members=[]
  for r in spec['clips']:
   pair=(r['source'],r.get('member'));self.assertNotIn(pair,oldmembers);members.append(pair)
   self.assertNotIn(hashlib.sha256((WORLD/r['path']).read_bytes()).hexdigest(),oldbytes)
  self.assertEqual(len(members),len(set(members)))
 def test_expansion_provenance_credits_and_importers(self):
  self.data();spec=json.loads((REPO/'Tools/Audio/atmospheric_audio_sources.json').read_text());proof=json.loads((WORLD/'atmospheric-provenance.json').read_text());credits=(WORLD/'ATMOSPHERIC-CREDITS.txt').read_text()
  self.assertEqual(spec['sources'],proof['sources']);self.assertEqual(len(spec['clips']),len(proof['clips']))
  for r in spec['clips']:
   self.assertEqual('CC0-1.0',r['license']);self.assertIn(r['credit'],credits);self.assertIn(r['page'],credits)
   meta=Path(str(WORLD/r['path'])+'.meta').read_text();self.assertIn('loadType: 0',meta);self.assertIn('preloadAudioData: 1',meta)
 def test_old53_unchanged(self):
  prior=[]
  for name in ['provenance.json','places-provenance.json']:prior+=json.loads((WORLD/name).read_text())['clips']
  self.assertEqual(53,len(prior))
  for r in prior:self.assertEqual(r['outputSha256'],hashlib.sha256((WORLD/r['path']).read_bytes()).hexdigest(),r['path'])
if __name__=='__main__':unittest.main(verbosity=2)
