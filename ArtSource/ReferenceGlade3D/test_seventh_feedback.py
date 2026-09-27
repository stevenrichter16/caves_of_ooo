import hashlib,json,unittest
from pathlib import Path
ROOT=Path(__file__).parent
class SeventhFeedbackTests(unittest.TestCase):
 def kit(self):return json.loads((ROOT/'kit.json').read_text())
 def grasses(self):return [m for m in self.kit()['models'] if m['family']=='green-grass']
 def test_grass_is_a_broad_low_tuft_in_the_current_fixed_view(self):
  for m in self.grasses():
   boxes=m['boxes'];width=max(b['center']['x']+b['size']['x']/2 for b in boxes)-min(b['center']['x']-b['size']['x']/2 for b in boxes)
   self.assertGreaterEqual(width,.89,m['id']);self.assertLessEqual(width,1.0,m['id'])
 def test_grass_keeps_clear_low_fingers_below_the_reed_hierarchy(self):
  for m in self.grasses():
   self.assertEqual(21,len(m['boxes']));top=max(b['center']['y']+b['size']['y']/2 for b in m['boxes'])
   self.assertGreaterEqual(top,.40);self.assertLessEqual(top,.425,m['id'])
 def test_other_thirtysix_models_and_palette_are_exact(self):
  def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()
  b=self.kit();self.assertEqual('4e6a23e3be626fb4508e0f70cfbfffc69b992cf6c5c205792a8a9ecdff2260c6',digest(b['palette']))
  self.assertEqual('0ead394977572a238acbff44d1a4b27bfd239c9481d65a526440271a1da828f3',digest([m for m in b['models'] if m['family']!='green-grass']))
if __name__=='__main__':unittest.main()
