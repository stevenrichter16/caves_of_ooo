import json,math,unittest
from pathlib import Path
P=Path(__file__).parent
KIT=json.loads((P/'visitors.json').read_text())
class IndependentSourceReview(unittest.TestCase):
 def test_every_parent_chain_has_one_root_without_cycles(self):
  for m in KIT['models']:
   bones={b['name']:b for b in m['bones']};self.assertEqual(['Root'],[b['name']for b in m['bones']if b['parent']is None],m['blueprint'])
   for bone in m['bones']:
    seen=set();name=bone['name']
    while name is not None:self.assertNotIn(name,seen,m['blueprint']);seen.add(name);name=bones[name]['parent']
    self.assertIn('Root',seen)
 def test_every_socket_has_a_finite_owned_weighted_attachment(self):
  for m in KIT['models']:
   self.assertEqual(len(m['sockets']),len({s['name']for s in m['sockets']}))
   for s in m['sockets']:
    self.assertTrue(all(math.isfinite(x)for x in s['position']));self.assertIn(s['bone'],{b['bone']for b in m['boxes']});self.assertTrue(s['name'].startswith('Equipment.'))
 def test_every_nonroot_bone_has_weighted_geometry_or_weighted_descendants(self):
  for m in KIT['models']:
   bones={b['name']:b for b in m['bones']};live=set()
   for box in m['boxes']:
    name=box['bone']
    while name is not None:live.add(name);name=bones[name]['parent']
   self.assertEqual(set(bones),live,m['blueprint'])
 def test_sockets_and_unambiguous_mirrored_limbs_are_symmetric(self):
  for m in KIT['models']:
   bones={b['name']:b for b in m['bones']}
   for name,bone in bones.items():
    if name.endswith('.L') and name[:-2]+'.R'in bones:
     right=bones[name[:-2]+'.R']
     for part in ['head','tail']:
      for i in range(3):self.assertAlmostEqual(bone[part][i]*(-1 if i==0 else 1),right[part][i],places=4,msg=m['blueprint']+name)
def overlaps():
 found=[]
 for m in KIT['models']:
  boxes=m['boxes'];bounds=[[(b['center'][i]-b['size'][i]/2,b['center'][i]+b['size'][i]/2)for i in range(3)]for b in boxes]
  for a in range(len(boxes)):
   for b in range(a+1,len(boxes)):
    if boxes[a]['color']==boxes[b]['color']:continue
    for axis in range(3):
     for side in [0,1]:
      plane=bounds[a][axis][side]
      if abs(plane-bounds[b][axis][side])>1e-6:continue
      others=[i for i in range(3)if i!=axis];spans=[(max(bounds[a][i][0],bounds[b][i][0]),min(bounds[a][i][1],bounds[b][i][1]))for i in others]
      if any(hi-lo<=1e-5 for lo,hi in spans):continue
      center=[0.,0.,0.];center[axis]=plane+(1 if side else -1)*1e-5
      for i,(lo,hi)in zip(others,spans):center[i]=(lo+hi)/2
      if any(k not in [a,b]and all(lo<center[i]<hi for i,(lo,hi)in enumerate(c))for k,c in enumerate(bounds)):continue
      found.append(dict(blueprint=m['blueprint'],a=boxes[a]['part'],b=boxes[b]['part'],axis=axis,side=side,area=math.prod(hi-lo for lo,hi in spans),sameBone=boxes[a]['bone']==boxes[b]['bone']))
 return found
if __name__=='__main__':
 (P/'independent-face-diagnostic.json').write_text(json.dumps(overlaps(),indent=2)+'\n');unittest.main()
