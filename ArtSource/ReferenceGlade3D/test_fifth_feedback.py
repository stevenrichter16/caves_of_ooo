import json,unittest,hashlib
from pathlib import Path
ROOT=Path(__file__).parent
class FifthFeedbackTests(unittest.TestCase):
 def kit(self):return json.loads((ROOT/'kit.json').read_text())
 def test_reeds_have_seven_flat_block_finger_tops(self):
  for m in self.kit()['models']:
   if m['family']!='pale-reeds':continue
   tops=[b for b in m['boxes'] if b['center']['y']>.40 and b['size']['y']<=.08 and min(b['size']['x'],b['size']['z'])>=.11]
   self.assertGreaterEqual(len(tops),7,m['id'])
 def test_reeds_no_longer_repeat_three_tier_cones_on_every_stalk(self):
  for m in self.kit()['models']:
   if m['family']=='pale-reeds':self.assertLessEqual(len(m['boxes']),24,m['id'])
 def test_other_thirtytwo_models_and_palette_stay_identical(self):
  def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()
  b=self.kit();self.assertEqual('4e6a23e3be626fb4508e0f70cfbfffc69b992cf6c5c205792a8a9ecdff2260c6',digest(b['palette']))
  self.assertEqual('be75e6bd7be3a49d564cb972bff2be6a3a092e347c5c3a66d5a64365202a053a',digest([m for m in b['models'] if m['family'] not in ['pale-reeds','green-grass','cooking-fire','cooled-fire','fallen-beam']]))
if __name__=='__main__':unittest.main()
