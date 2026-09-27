import json,math,unittest,os
from pathlib import Path
EXPECTED='CaveBat CaveSlime CaveBear Scorpion SandWurm SkeletalSentry StoneGolem PaleStalker ObsidianBrute DesertProwler DuneLurker BrittleHound JungleStalker CanopyStrangler AncientGuardian VaultSentinel BrassHusk GlassScorpion SporeShambler IceWight CharredHusk SleepingTroll SunStriker Reedfrog Bandfrog GinFrog SummitSinger BrocchiniaSentinel PrickleBrowGecko'.split()
class VisitorSource(unittest.TestCase):
 def setUp(self):
  p=Path(__file__).with_name('visitors.json');self.k=json.loads(p.read_text()) if p.exists() else {'models':[]};self.m={m['blueprint']:m for m in self.k['models']}
 def get(self,bp):self.assertIn(bp,self.m);return self.m[bp]
 def parts(self,bp,key):return [b for b in self.get(bp)['boxes'] if key in b['part']]
 def test_exact29_no_proxy_alias(self):self.assertEqual(set(EXPECTED),set(self.m));self.assertEqual(29,len({m['id'] for m in self.m.values()}))
 def test_palette_retains24_and_explicit_semantic_colors(self):
  base=json.loads((Path(os.environ.get('COO_SPREAD_SOURCE_ROOT',str(Path(__file__).resolve().parents[2])))/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'];self.assertEqual(base,self.k.get('palette',[])[:24]);self.assertEqual(30,len(self.k['palette']))
 def test_all_geometry_has_real_bone_ownership_and_bounded_extent(self):
  self.assertEqual(29,len(self.m))
  for m in self.m.values():
   names=[b['name'] for b in m['bones']];self.assertEqual(len(names),len(set(names)));self.assertIn('Root',names)
   self.assertEqual(['Idle','Walk','Interact','Attack','Hit'],m['clips']);self.assertGreaterEqual(len(m['boxes']),5)
   for b in m['bones']:
    self.assertTrue(b['parent'] is None or b['parent'] in names);self.assertNotEqual(b['head'],b['tail'])
   for b in m['boxes']:
    self.assertIn(b['bone'],names);self.assertIn(b['color'],range(30));self.assertTrue(all(math.isfinite(x) for x in b['center']+b['size']));self.assertTrue(all(0<x<2 for x in b['size']))
    self.assertTrue(all(abs(b['center'][i])+b['size'][i]/2<2 for i in range(3)));self.assertGreaterEqual(b['center'][1]-b['size'][1]/2,-.015)
 def test_every_species_has_distinct_geometry_beyond_color(self):
  self.assertEqual(29,len(self.m));keys=[]
  for m in self.m.values():keys.append(tuple((tuple(b['center']),tuple(b['size']),b['bone']) for b in m['boxes']))
  self.assertEqual(29,len(set(keys)))
 def test_frogs_have_four_limbs_no_tail_and_folded_hindquarters(self):
  for bp in ['Reedfrog','Bandfrog','GinFrog','SummitSinger']:
   m=self.get(bp);self.assertEqual(4,len([b for b in m['bones'] if b['name'].startswith('Leg')]));self.assertFalse(any(b['name'].startswith('Tail') for b in m['bones']));self.assertEqual(2,len(self.parts(bp,'thigh')))
 def test_banded_frog_is_scarlet_on_black_and_summit_has_W(self):
  bands=self.parts('Bandfrog','scarlet-band');self.assertGreaterEqual(len(bands),3);self.assertTrue(all(b['color']==24 for b in bands));self.assertGreaterEqual(len(self.parts('SummitSinger','dorsal-W')),4)
 def test_lizards_have_tail_chain_and_specific_eye_spines(self):
  for bp in ['SunStriker','BrocchiniaSentinel','PrickleBrowGecko']:self.assertGreaterEqual(len([b for b in self.get(bp)['bones'] if b['name'].startswith('Tail')]),4)
  self.assertEqual(2,len(self.parts('PrickleBrowGecko','brow-spine')));self.assertTrue(any(b['color']==27 for b in self.parts('PrickleBrowGecko','throat')))
 def test_bat_has_independent_wings_and_not_ground_arms(self):
  m=self.get('CaveBat');self.assertEqual(2,len([b for b in m['bones'] if b['name'].startswith('Wing')]));self.assertGreaterEqual(len(self.parts('CaveBat','wing-panel')),6)
 def test_scorpions_have8legs2claws_and_segmented_stings(self):
  for bp in ['Scorpion','GlassScorpion']:
   names=[b['name'] for b in self.get(bp)['bones']];self.assertEqual(8,len([n for n in names if n.startswith('Leg')]));self.assertEqual(2,len([n for n in names if n.startswith('Arm')]));self.assertGreaterEqual(len([n for n in names if n.startswith('Tail')]),4);self.assertTrue(self.parts(bp,'sting'))
 def test_quadrupeds_have_four_paws_and_different_physical_builds(self):
  for bp in ['CaveBear','PaleStalker','DesertProwler','BrittleHound','JungleStalker']:self.assertEqual(4,len(self.parts(bp,'paw')))
  self.assertGreaterEqual(len(self.parts('BrittleHound','glass-shard')),3)
 def test_wurm_and_ambush_maw_are_not_humanoid(self):
  self.assertGreaterEqual(len([b for b in self.get('SandWurm')['bones'] if b['name'].startswith('Segment')]),6);self.assertGreaterEqual(len(self.parts('SandWurm','mouth-rim')),6)
  self.assertEqual(4,len([b for b in self.get('DuneLurker')['bones'] if b['name'].startswith('Arm')]));self.assertLess(max(b['center'][1]+b['size'][1]/2 for b in self.get('DuneLurker')['boxes']),.7)
 def test_fungal_and_plant_anatomies_are_distinct(self):
  self.assertGreaterEqual(len([b for b in self.get('CanopyStrangler')['bones'] if b['name'].startswith('Tendril')]),4);self.assertGreaterEqual(len(self.parts('SporeShambler','spore-sac')),4)
 def test_real_equipped_sentry_has_nine_bones_and_head_socket(self):
  m=self.get('SkeletalSentry');self.assertEqual({'Root','Spine','Head','Arm.L','Arm.R','Hand.L','Hand.R','Leg.L','Leg.R'},{b['name'] for b in m['bones']});self.assertEqual({'Equipment.Head','Equipment.Hand.L','Equipment.Hand.R','Equipment.Back'},{s['name'] for s in m['sockets']});self.assertGreaterEqual(len(self.parts('SkeletalSentry','rib')),6)
 def test_construct_and_undead_distinguishing_structures(self):
  for bp,part in [('StoneGolem','stone-block'),('ObsidianBrute','obsidian-slab'),('AncientGuardian','shoulder-tower'),('VaultSentinel','inset-face'),('BrassHusk','brass-plate'),('IceWight','ice-ridge'),('CharredHusk','ember-seam'),('SleepingTroll','belly')]:self.assertTrue(self.parts(bp,part))
 def test_exposed_different_color_faces_do_not_share_a_raster_plane(self):
  from independent_source_review import overlaps
  self.assertEqual([],overlaps(),"Coplanar colored surfaces reproduce the earlier native spider shadow seam.")
if __name__=='__main__':unittest.main()
