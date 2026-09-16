"""Classify this audit's native census; distinct landmarks are not distinct chunks."""
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parent
source=root/'FP01-native-census/native-census.json'
d=json.loads(source.read_text())
required={
 'GroveShrine':['Mogu','Grib','Nam','Sien','Sopp'],
 'RiverShrine':['RiverShrine'],
 'FestivalField':['FlowerField','Signpost'],
 'ConcordWaystation':['SaccharineEnvoy'],
 'TentRightCamp':['TentRightHost','GuestClothPole'],
 'RuneCultSite':['RuneCultist'],
 'PalimpsestArchive':['PalimpsestEcho'],
 'GlassblownObelisk':['GlassblownDrifter'],
 'WarbandCamp':['SnapjawWarlord'],
 'CurationGallery':['PaleCurator'],
}
fixed={}
for name,x,y in [('Deepest Cathedral',5,4),('Stillleaf',2,4)]:
 for z in range(3):fixed[f'Overworld.{x}.{y}.{z}']=name
for name,x,y in [('Felling-Site',3,5),('Woven Doll',1,6),('Tenth Fire',2,19),('Abandoned Counter A',19,18),('Abandoned Counter B',19,19)]:
 fixed[f'Overworld.{x}.{y}.0']=name

def excluded(r):
 return r['poiType'] in ('Village','MerchantCamp') or r['poi'] in ('Olderdeep','Lampwell','Spivenor')

assert d['complete'] and len(d['rows'])==412
assert len({r['id'] for r in d['rows']})==412
assert sum(r['id'].endswith('.0') for r in d['rows'])==400
assert not d['errors'] and not any(r['error'] or r['diagDropped'] for r in d['rows'])
# Scope counterchecks: the map's Sinkhole type does not imply non-settlement.
assert excluded({'poiType':'Village','poi':'Sill'})
assert excluded({'poiType':'MerchantCamp','poi':'Caravanserai'})
assert excluded({'poiType':'Sinkhole','poi':'Olderdeep'})
assert not excluded({'poiType':'Sinkhole','poi':'Stillleaf'})
assert not excluded({'poiType':None,'poi':None})
by_type={k:set() for k in required};by_type['BloomFront']=set()
formations={'Grove':['GroveSeep','GroveSign'],
 'TendrilFen':['ChoirTendril'], 'CompostingField':['CompostRow','CompostCache']}
by_formation={k:set() for k in formations}
qualifying={};ignored=[];discrete=set()
for r in d['rows']:
 stamps={e['payload']['stamp'] for e in r['events'] if e['kind']=='StructurePlaced'}
 matches=stamps & required.keys()
 # A diagnostic by itself is insufficient: actual defining owners must survive.
 for stamp in matches:
  assert all(r['census'].get(bp,0)>0 for bp in required[stamp]),(r['id'],stamp)
 reasons=set(matches)
 if any(e['kind']=='BloomFront' for e in r['events']):
  assert r['census'].get('BloomingFruitingBody',0)>0
  reasons.add('BloomFront')
 if r['id'] in fixed:reasons.add('Fixed: '+fixed[r['id']])
 discrete_match=bool(reasons)
 for e in r['events']:
  if e['kind']!='FormationApplied' or e['payload'].get('biome')!='Grovelands':continue
  form=e['payload'].get('formation')
  if form in formations:
   assert all(r['census'].get(bp,0)>0 for bp in formations[form]),(r['id'],form)
   reasons.add('Cultural formation: '+form)
 if not reasons:continue
 if excluded(r):ignored.append({'id':r['id'],'reasons':sorted(reasons)});continue
 qualifying[r['id']]=sorted(reasons)
 if discrete_match:discrete.add(r['id'])
 for reason in reasons:
  if reason in by_type:by_type[reason].add(r['id'])
  if reason.startswith('Cultural formation: '):by_formation[reason.split(': ',1)[1]].add(r['id'])
rows={r['id']:r for r in d['rows']}
assert set(fixed)<=qualifying.keys()
for id,bps in {
 'Overworld.3.5.0':['FellingBarePosition','SeventhPosition'],
 'Overworld.5.4.2':['ChoirNode','EncasedElder','ChoirTendril'],
 'Overworld.2.4.2':['SealedLibraryDoor','SealedArchiveShelf'],
 'Overworld.2.7.2':['MirePool','GinFrog'],
 'Overworld.1.6.0':['WovenDoll'],
 'Overworld.2.19.0':['UntendedFire'],
}.items():assert all(rows[id]['census'].get(bp,0)>0 for bp in bps),id
for id in ('Overworld.19.18.0','Overworld.19.19.0'):
 assert any(e['kind']=='StructurePlaced' and e['payload']['stamp']=='AbandonedCounter' for e in rows[id]['events'])
 assert rows[id]['census'].get('Bones',0)>0
procedural=set().union(*by_type.values())
ecological=[f'Overworld.2.7.{z}' for z in range(3)]
assert not set(ecological)&qualifying.keys()
associations={
 'Saccharine Concord':{'ConcordWaystation','Fixed: Abandoned Counter A','Fixed: Abandoned Counter B'},
 'Rot Choir':{'GroveShrine','Fixed: Deepest Cathedral','Fixed: Woven Doll',
  'Cultural formation: Grove','Cultural formation: TendrilFen','Cultural formation: CompostingField'},
 'Tent-Right':{'TentRightCamp'},'Recension':{'Fixed: Stillleaf','PalimpsestArchive'},
 'Rune Cult':{'RuneCultSite'},'Snapjaws':{'WarbandCamp'},
 'Folk river reverence':{'RiverShrine'},'Festival grounds':{'FestivalField'},
 'Driving Bloom':{'BloomFront'},'Felling / Urqu':{'Fixed: Felling-Site'},
 'Tenth Fire':{'Fixed: Tenth Fire'},
}
by_association={name:sorted(id for id,rs in qualifying.items() if kinds.intersection(rs))
 for name,kinds in associations.items()}
assert set().union(*(set(ids) for ids in by_association.values()))==qualifying.keys()
summary={
 'seed':d['seed'],'scope':d['scope'],'census_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
 'unique_qualifying_chunks':len(qualifying),
 'qualifying_surface_chunks':sum(id.endswith('.0') for id in qualifying),
 'qualifying_below_ground_chunks':sum(not id.endswith('.0') for id in qualifying),
 'fixed_sites':len(set(fixed.values())),'fixed_site_chunks':len(fixed),
 'procedural_chunks':len(procedural),'fixed_procedural_overlap':len(procedural & fixed.keys()),
 'discrete_site_or_stamp_chunks':len(discrete),
 'cultural_formation_chunks':len(set().union(*by_formation.values())),
 'additional_cultural_chunks':len(qualifying.keys()-discrete),
 'ecological_optional_chunks':ecological,'total_including_ecological':len(qualifying)+len(ecological),
 'by_procedural_type':{k:{'count':len(v),'zones':sorted(v)} for k,v in by_type.items()},
 'by_cultural_formation':{k:{'count':len(v),'zones':sorted(v)} for k,v in by_formation.items()},
 'by_association':{k:{'count':len(v),'zones':v} for k,v in by_association.items()},
 'fixed':fixed,'qualifying':qualifying,'excluded_matches':ignored,
 'overlapping_chunks':{k:v for k,v in qualifying.items() if len(v)>1},
 'validation':'412 unique rows, all owners checked, zero generation/runtime errors or diagnostic drops; scope counterchecks passed',
}
(root/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({k:v for k,v in summary.items() if k not in ('by_procedural_type','fixed','qualifying','overlapping_chunks')},indent=2))
