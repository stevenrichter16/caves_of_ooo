from pathlib import Path
import json,re,hashlib
import argparse
args=argparse.ArgumentParser();args.add_argument('--repo',type=Path,default=Path(__file__).resolve().parents[2]);config=args.parse_args()
r=config.repo.resolve();b=Path(__file__).resolve().parent
semantic_families={'water','spring','tar','acid','mirror-mucilage','memory-bath','brine','spray-pool','steam-vent','ash-bed','campfire','peat-bog','oil','seep','pool','quartz','choir-iron-vein','tepui-bone-vein','ore-vein'}
def semantic(id):
 return any('-'+f+'-' in id+'-' for f in semantic_families)
rows=[]
c=json.loads((r/'ArtSource/SpawnRing3D/catalog.json').read_text())
for m in c['models']:
 if m['rigged'] or m['kind']=='actor':continue
 rows.append({'id':m['id'],'sourceLibrary':'SpawnRing3D','kind':m['kind'],'semanticColors':semantic(m['id']) or m['kind']=='tile-overlay'})
approved_libraries={'SoddenVoxel3D','TallyVoxel3D','StillleafVoxel3D','WellmeetVoxel3D','DensityPhase1Voxel3D','GinmereVoxel3D','TineVoxel3D','CathedralVoxel3D','GantryVoxel3D','FirstTentVoxel3D','CinderholdVoxel3D','MarrowstyeVoxel3D','BeatingVoxel3D','LastCounterVoxel3D','DrownedLedgerVoxel3D','OlderdeepVoxel3D','QuillholdVoxel3D','SumpholdVoxel3D','SpreadVoxel3D','StumpVoxel3D','OverwritVoxel3D'}
for name in sorted(approved_libraries):
 p=r/'Assets/Resources'/name/'Library.asset'
 for block in re.split(r'^  - Id: ',p.read_text(),flags=re.M)[1:]:
  id=block.splitlines()[0];kind=re.search(r'^      kind: (.+)$',block,re.M);rig=re.search(r'^      rigged: (.+)$',block,re.M)
  assert kind and rig,p
  if kind[1]=='actor' or rig[1]!='0':continue
  rows.append({'id':id,'sourceLibrary':p.parent.name,'kind':kind[1],'semanticColors':semantic(id)})
rows.sort(key=lambda x:x['id']);assert len({x['id']for x in rows})==len(rows)
out={'schemaVersion':1,'id':'spread-native-style-original-geometry','scope':'Explicit existing static native source graph; native owner/state and receiving Spread authority remain required. Static historical actor shapes cannot enter the owner gate. Not all source definitions are naturally reachable.','palettePolicy':'Ordinary props remap to approved24 colors. Explicit semantic entries preserve exact native palette cells and water shader; this is no geometry redesign.','models':rows}
(b/'sources.json').write_text(json.dumps(out,indent=2)+'\n')
print(len(rows),'sources;',sum(x['semanticColors']for x in rows),'semantic')
