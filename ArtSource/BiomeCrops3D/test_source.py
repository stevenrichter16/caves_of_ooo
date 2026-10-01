"""Closed, original plant source invariants; first run intentionally precedes kit."""
import hashlib,json,math,unittest
from pathlib import Path
ROSTER={
'Spread':'Claspbean Pitchpod Drawgourd Wickrush Marlroot',
'Sodden':'Sumpsieve Drowsebell Chillcress Slipsedge Peatlantern',
'Beating':'Sunbladder Shalebean Shadefan Cinderpea Spurgrass',
'Grovelands':'Choirwick Knitmoss Sourmantle Murmurpod Sealbark',
'Overwrit':'Absentmint Margincress Binderroot Greybladder Hollowchime',
'Stump':'Raingourd Prismreed ScarletSundew Cloudwick Gripfrond',
'Cave':'Lampvein Knucklecap Sootroot Veilpuff Brinebutton'}
def geometry(m):return [(b['center'],b['size']) for b in m['boxes']]
class SourceTests(unittest.TestCase):
 def setUp(self):
  self.kit=json.loads((Path(__file__).parent/'kit.json').read_text());self.models={m['id']:m for m in self.kit['models']}
 def test_closed_roster_and_280_bounded_original_forms(self):
  self.assertEqual(self.kit['schemaVersion'],1);self.assertEqual(self.kit['id'],'biome-crops-original')
  self.assertEqual(len(self.kit['species']),35);self.assertEqual(len(self.models),280);self.assertEqual(len(self.kit['models']),280)
  self.assertEqual(len(self.kit['palette']),24)
  self.assertEqual({s['name'] for s in self.kit['species']},{n for group in ROSTER.values() for n in group.split()})
  for biome,names in ROSTER.items():self.assertEqual({s['name'] for s in self.kit['species'] if s['biome']==biome},set(names.split()))
  for m in self.models.values():
   self.assertEqual(m['kind'],'entity');self.assertTrue(3<=len(m['boxes'])<=100,(m['id'],len(m['boxes'])))
   for b in m['boxes']:
    self.assertTrue(0<=b['color']<24)
    for a in 'xyz':self.assertTrue(math.isfinite(b['center'][a]) and .003<=b['size'][a]<=1.001)
    for a in 'xz':self.assertLessEqual(abs(b['center'][a])+b['size'][a]/2,.501,m['id'])
    self.assertGreaterEqual(b['center']['y']-b['size']['y']/2,-.0351,m['id']);self.assertLessEqual(b['center']['y']+b['size']['y']/2,1.8501,m['id'])
 def test_every_species_is_distinct_and_every_stage_changes_geometry(self):
  mature=set();harvest=set();seed=set()
  for s in self.kit['species']:
   prefix='biome-crop-'+s['stem'];stages=[]
   for stage in range(3):
    a=self.models[f'{prefix}-{stage}-dry'];b=self.models[f'{prefix}-{stage}-wet']
    self.assertEqual(geometry(a),geometry(b));self.assertNotEqual(a['boxes'],b['boxes']);stages.append(json.dumps(geometry(a),sort_keys=True))
   self.assertEqual(len(set(stages)),3,s['name']);mature.add(stages[2]);harvest.add(json.dumps(geometry(self.models[prefix+'-harvest']),sort_keys=True));seed.add(json.dumps(geometry(self.models[prefix+'-seed']),sort_keys=True))
  self.assertEqual(len(mature),35,'Species cannot merely recolor the same mature geometry.')
  self.assertEqual(len(harvest),35,'Actual useful harvest forms must be recognizable.')
  self.assertEqual(len(seed),35,'Seed forms must not be identical sacks.')
 def test_previous37_form_source_remains_exact(self):
  source=Path(__file__).parents[1]/'RepairCultivation3D/kit.json';self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),'14664f5ed987b81e361a2eea25734bcfdaba2c251da0a24af84b41c601ca843b');self.assertEqual(len(json.loads(source.read_text())['models']),37)
if __name__=='__main__':unittest.main()
