import json,unittest,hashlib
from pathlib import Path
ROOT=Path(__file__).parent
class SixthFeedbackTests(unittest.TestCase):
 def kit(self):return json.loads((ROOT/'kit.json').read_text())
 def test_grass_has_seven_separate_upright_fingers(self):
  for m in self.kit()['models']:
   if m['family']=='green-grass':self.assertEqual(7,sum(b['size']['y']>.23 for b in m['boxes']),m['id'])
 def test_grass_has_no_repeated_leaf_ladders(self):
  for m in self.kit()['models']:
   if m['family']=='green-grass':self.assertLessEqual(len(m['boxes']),21,m['id'])
 def test_other_thirtysix_models_and_palette_unchanged(self):
  def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()
  b=self.kit();self.assertEqual('4e6a23e3be626fb4508e0f70cfbfffc69b992cf6c5c205792a8a9ecdff2260c6',digest(b['palette']))
  self.assertEqual('0ead394977572a238acbff44d1a4b27bfd239c9481d65a526440271a1da828f3',digest([m for m in b['models'] if m['family']!='green-grass']))
if __name__=='__main__':unittest.main()
