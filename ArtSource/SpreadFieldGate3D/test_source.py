import json,math,unittest
from pathlib import Path
HERE=Path(__file__).parent
REPO=Path('/Users/steven/caves-of-ooo')
IDS={'spread-field-gate-closed','spread-field-gate-open'}
class SourceTests(unittest.TestCase):
 def setUp(self):
  self.kit=json.loads((HERE/'kit.json').read_text());self.models={m['id']:m for m in self.kit['models']}
 def pair(self):
  self.assertEqual(set(self.models),IDS,'Two original states must exist; absence is not a fallback gate.')
  return self.models['spread-field-gate-closed'],self.models['spread-field-gate-open']
 def test_exact_two_static_states_not_animation_or_new_gameplay(self):
  for m in self.pair():
   self.assertEqual(m['blueprint'],'SpreadFieldGate');self.assertFalse(m['rigged']);self.assertEqual(m['clips'],[]);self.assertEqual(m['bones'],[]);self.assertEqual(m['sockets'],[])
 def test_palette_is_exact_existing_approved_source(self):
  self.assertEqual(self.kit['palette'],json.loads((REPO/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'])
 def test_posts_are_identical_grounded_timber_in_both_states(self):
  a,b=self.pair();pa=[p for p in a['boxes'] if p['part'].startswith('fixed-')];pb=[p for p in b['boxes'] if p['part'].startswith('fixed-')]
  self.assertEqual(pa,pb)
  posts=[p for p in pa if p['part'] in ('fixed-post-L','fixed-post-R')];self.assertEqual(len(posts),2)
  for p in posts:self.assertAlmostEqual(p['center'][1]-p['size'][1]/2,0);self.assertLessEqual(p['size'][1],.85)
 def test_open_leaf_is_same_leaf_rotated_about_its_original_hinge(self):
  a,b=self.pair();aa={p['part']:p for p in a['boxes'] if p['part'].startswith('leaf-')};bb={p['part']:p for p in b['boxes'] if p['part'].startswith('leaf-')}
  self.assertEqual(set(aa),set(bb));self.assertGreaterEqual(len(aa),5)
  hx,hz=self.kit['hinge'][0],self.kit['hinge'][2]
  for k,p in aa.items():
   q=bb[k];x,y,z=p['center'];expected=[hx-(z-hz),y,hz+(x-hx)]
   for actual,wanted in zip(q['center'],expected):self.assertAlmostEqual(actual,wanted,places=8)
   self.assertEqual(q['size'],[p['size'][2],p['size'][1],p['size'][0]]);self.assertEqual(q['color'],p['color'])
 def test_closed_barrier_and_open_walk_gap_are_visibly_different(self):
  a,b=self.pair()
  closed=[p for p in a['boxes'] if p['part'].startswith('leaf-rail-')];self.assertEqual(len(closed),3)
  self.assertTrue(all(p['center'][0]-p['size'][0]/2<-.3 and p['center'][0]+p['size'][0]/2>.3 for p in closed))
  # A .54-wide central route extends through the whole cell in the open state.
  # This is a visual corridor; actual DoorPart still owns logical collision.
  for p in b['boxes']:
   self.assertTrue(p['center'][0]+p['size'][0]/2 <= -.27 or p['center'][0]-p['size'][0]/2 >= .27,p['part'])
 def test_finite_small_geometry_inside_cell_for_all_quarter_turns(self):
  for m in self.pair():
   self.assertLessEqual(len(m['boxes']),20)
   for p in m['boxes']:
    self.assertTrue(all(math.isfinite(v) for v in p['center']+p['size']));self.assertTrue(all(v>0 for v in p['size']));self.assertIn(p['color'],range(24))
    self.assertGreaterEqual(p['center'][1]-p['size'][1]/2,-1e-8);self.assertLessEqual(p['center'][1]+p['size'][1]/2,.85)
    for i in [0,2]:self.assertLessEqual(abs(p['center'][i])+p['size'][i]/2,.49)
 def test_every_piece_is_connected_to_a_grounded_post(self):
  for m in self.pair():
   boxes=m['boxes'];reached={i for i,p in enumerate(boxes) if p['part'] in ('fixed-post-L','fixed-post-R')}
   while True:
    nxt={i for i,p in enumerate(boxes) if any(all(abs(p['center'][k]-boxes[j]['center'][k]) <=(p['size'][k]+boxes[j]['size'][k])/2+1e-8 for k in range(3)) for j in reached)}
    if nxt<=reached:break
    reached|=nxt
   self.assertEqual(len(reached),len(boxes),[p['part'] for i,p in enumerate(boxes) if i not in reached])
 def test_different_palette_faces_do_not_overlap_in_the_same_plane(self):
  for m in self.pair():
   for i,a in enumerate(m['boxes']):
    for b in m['boxes'][i+1:]:
     if a['color']==b['color']:continue
     for axis in range(3):
      other=[k for k in range(3) if k!=axis]
      if not all(min(a['center'][k]+a['size'][k]/2,b['center'][k]+b['size'][k]/2)-max(a['center'][k]-a['size'][k]/2,b['center'][k]-b['size'][k]/2)>1e-6 for k in other):continue
      for sign in [-1,1]:
       self.assertGreater(abs(a['center'][axis]+sign*a['size'][axis]/2-b['center'][axis]-sign*b['size'][axis]/2),1e-6,(m['id'],a['part'],b['part'],axis,sign))
if __name__=='__main__':unittest.main(verbosity=2)
