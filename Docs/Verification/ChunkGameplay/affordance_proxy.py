"""Join native blueprint occurrence counts to inherited authored capabilities.
This is not a simulation of interactions, loaded inventories or dynamic Parts.
"""
from pathlib import Path
from functools import lru_cache
from collections import Counter
import hashlib,json
root=Path(__file__).resolve().parents[3]
census=root/'Docs/Verification/FactionPOI/FP01-native-census/native-census.json'
blueprints=root/'Assets/Resources/Content/Blueprints/Objects.json'
freeze=root/'Docs/Verification/FactionPOI/FP01-native-census/source-freeze.json'
frozen=json.loads(freeze.read_text())['paths']
current={str(p.relative_to(root)) for base in ['Assets/Scripts','Assets/Editor'] for p in (root/base).rglob('*.cs')}
current|={str(p.relative_to(root)) for p in (root/'Assets/Resources/Content').rglob('*.json')}
added=sorted(current-set(frozen));removed=sorted(set(frozen)-current)
changed=[p for p,h in frozen.items() if not (root/p).is_file() or hashlib.sha256((root/p).read_bytes()).hexdigest()!=h]
assert not (added or removed or changed),(added,removed,changed)
verification={
 'date':'2026-09-16','method':'SHA-256 and source-path set comparison against prior native census source freeze; no new Unity execution',
 'baseline':str(freeze.relative_to(root)),'baseline_sha256':hashlib.sha256(freeze.read_bytes()).hexdigest(),
 'source_count':len(frozen),'changed_paths':changed,'added_paths':added,'removed_paths':removed,
 'native_census':str(census.relative_to(root)),'native_census_sha256':hashlib.sha256(census.read_bytes()).hexdigest(),
}
Path(__file__).with_name('source-verification.json').write_text(json.dumps(verification,indent=2)+'\n')
raw={o['Name']:o for o in json.loads(blueprints.read_text())['Objects']}
@lru_cache(None)
def resolve(name):
 b=raw[name]
 p,t=resolve(b['Inherits']) if b.get('Inherits') else ({},{})
 p={k:dict(v) for k,v in p.items()};t=dict(t)
 for part in b.get('Parts',[]):p.setdefault(part['Name'],{}).update({kv['Key']:kv['Value'] for kv in part.get('Params',[])})
 t.update({kv['Key']:kv['Value'] for kv in b.get('Tags',[])})
 return p,t
for name in raw:resolve(name)
# Counterchecks against actual authored content: not every object is an activity.
assert 'Sanctuary' in resolve('RiverShrine')[0]
assert 'Conversation' in resolve('EncasedElder')[0]
assert 'Conversation' not in resolve('ChoirNode')[0]
assert 'Harvestable' not in resolve('OverwritNewGrowth')[0]
assert 'Creature' in resolve('GinFrog')[1] and resolve('GinFrog')[0]['Brain']['Passive']=='true'
features=['Conversation','Trader','Container','Harvestable','Campfire','Sanctuary','Forge','AlchemyStill']
d=json.loads(census.read_text());counts={};unknown=Counter();rows=[]
for r in d['rows']:
 if not r['id'].endswith('.0'):continue
 parts=set();tags=set();unresolved=[]
 for name,n in r['census'].items():
  if name not in raw:unknown[name]+=n;unresolved.append(name);continue
  p,t=resolve(name);parts.update(p);tags.update(t)
 present={k:k in parts for k in features};present['Creature']='Creature' in tags
 row={'id':r['id'],'biome':r['biome'],'map_type':r['poiType'],'features':present,'unresolved_blueprints':unresolved};rows.append(row)
 summary=counts.setdefault(r['biome'],Counter());summary['chunks']+=1
 for k,v in present.items():summary[k]+=int(v)
assert len(rows)==400
spread=[r for r in d['rows'] if r['biome']=='Spread' and r['id'].endswith('.0') and not r['poi']]
spread_crops={'poi_free_chunks':len(spread),'chunks_with_crop_rows':sum('CropRow' in r['census'] for r in spread),'crop_row_owners':sum(r['census'].get('CropRow',0) for r in spread)}
assert spread_crops=={'poi_free_chunks':132,'chunks_with_crop_rows':30,'crop_row_owners':3277}
assert not ({'Crop','Harvestable'} & set(resolve('CropRow')[0]))
over=[r for r in d['rows'] if r['biome']=='Overwrit' and r['id'].endswith('.0') and r['id']!='Overworld.2.11.0']
assert len(over)==22
assert all(set(r['census']) <= {'OverwritGround','OverwritNewGrowth','StairsDown'} for r in over)
h=2166136261
for c in 'HelmwoodDoor|141343545|2|7':h=((h^ord(c))*16777619)&0xffffffff
report={
 'seed':d['seed'],'native_census_sha256':hashlib.sha256(census.read_bytes()).hexdigest(),
 'blueprints_sha256':hashlib.sha256(blueprints.read_bytes()).hexdigest(),
 'method':'Actual fresh native entity occurrences joined to static inherited blueprint Parts/Tags. All400 surface cells, including settlements. Counts are chunks with at least one matching object.',
 'limitations':['Creature includes passive fauna, not just enemies.','Container does not mean stocked or rewarding.','Conversation/Trader are static capability presence, not service availability or distinct story count.','Runtime-injected Parts/blueprints are not inferred; unresolved owners are listed.','No new native execution or playthrough in this analysis.'],
 'by_biome':counts,'unknown_blueprint_counts':unknown,'rows':rows,
 'spread_authored_crop_rows':spread_crops,
 'overwrit_composed_chunks_with_only_ground_growth_and_possible_stair':len(over),
 'overwrit_composed_chunks_with_stairs':sum('StairsDown' in r['census'] for r in over),
 'helmwood_selection_index_for_seed':h%8,
 'helmwood_eligible_for_seed':h%8==0,
}
out=Path(__file__).with_name('affordance-proxy.json');out.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k not in ['rows']},indent=2))
