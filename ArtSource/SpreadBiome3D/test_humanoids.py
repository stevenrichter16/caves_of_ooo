import json, unittest, os
from pathlib import Path
ROOT=Path(__file__).parent
PROJECT=Path(os.environ.get("COO_SPREAD_SOURCE_ROOT",str(ROOT.parents[1])))
class HumanSourceTests(unittest.TestCase):
 def setUp(self):
  self.data=json.loads((ROOT/'humanoids.json').read_text()) if (ROOT/'humanoids.json').exists() else {'roles':[]}
 def test_all_nonplayer_humanoid_identities_are_explicit(self):
  anatomy=json.loads((PROJECT/'Docs/Verification/DensityCompletion/SpreadBiome/Art/actor-anatomy-map.json').read_text())
  expected={x['blueprint'] for x in anatomy if x['anatomy']=='humanoid' and x['blueprint']!='Player'}
  expected.update(('MarlbackCindercaller','MarlbackSoursprayer'))
  self.assertEqual(expected,{r['blueprint'] for r in self.data['roles']})
 def test_canonical_glyphs_follow_current_blueprints(self):
  refs={r['blueprint']:r['glyph'] for r in json.loads((PROJECT/'Docs/Verification/DensityCompletion/SpreadBiome/Authority/all-creature-source-requirements.json').read_text())}
  refs.update(MarlbackCindercaller='g',MarlbackSoursprayer='g')
  self.assertGreater(len(self.data['roles']),40)
  for r in self.data['roles']: self.assertEqual(refs[r['blueprint']],r['glyph'])
 def test_only_approved_palette_indices_and_safe_ids(self):
  self.assertGreater(len(self.data['roles']),40)
  for r in self.data['roles']:
   self.assertRegex(r['id'],r'^spread-person-[a-z0-9-]+$')
   for key in ('body','skin','accent','hair'): self.assertIn(r[key],range(24))
 def test_child_and_small_species_do_not_become_full_adults(self):
  rows={r['blueprint']:r for r in self.data['roles']}
  self.assertIn('VillageChild',rows)
  for bp in ('VillageChild','DirtGnome','SootGremlin'):self.assertLess(rows[bp]['stature'],.85)
  self.assertEqual(1,rows['Farmer']['stature'])
 def test_roles_have_distinct_observable_clothing_not_only_ids(self):
  rows={r['blueprint']:r for r in self.data['roles']}
  self.assertIn('Scribe',rows)
  signatures={tuple(rows[bp][k] for k in ('body','accent','headwear','garment')) for bp in ('Farmer','Scribe','Armorer','Merchant','Warden','Arcanist','Innkeeper')}
  self.assertEqual(7,len(signatures))
 def test_existing_three_approved_body_sources_not_replaced(self):
  self.assertEqual(['ring-player','ring-sien','ring-nam'],self.data.get('preserveNativeModelIds'))
if __name__=='__main__':unittest.main()
