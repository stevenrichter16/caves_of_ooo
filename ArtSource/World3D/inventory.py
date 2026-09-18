"""Read-only world-art inventory; never equates local art with global coverage."""
from pathlib import Path
import argparse,copy,hashlib,json,re
ROOT=Path(__file__).resolve().parents[2]

def resolve_blueprints(rows):
 raw={r['Name']:r for r in rows}
 if len(raw)!=len(rows):raise ValueError('Duplicate blueprint')
 done={};active=set()
 def visit(name):
  if name in done:return done[name]
  if name in active:raise ValueError('Inheritance cycle: '+name)
  if name not in raw:raise ValueError('Missing parent: '+name)
  active.add(name);r=raw[name];parent=r.get('Inherits')
  d=copy.deepcopy(visit(parent)) if parent else {'parts':{},'tags':{},'props':{},'ancestors':[]}
  d['ancestors']=([parent]+d['ancestors']) if parent else []
  for p in r.get('Parts',[]):d['parts'].setdefault(p['Name'],{}).update({v['Key']:v['Value'] for v in p.get('Params',[])})
  for source,target in [('Tags','tags'),('Props','props')]:d[target].update({v['Key']:v['Value'] for v in r.get(source,[])})
  done[name]=d;active.remove(name);return d
 for name in raw:visit(name)
 return done

def coverage_status(local,candidates):
 return ('local-only+candidate' if candidates else 'local-only') if local else ('candidate-only' if candidates else 'uncovered')

def sprite_tables(source):
 source=re.sub(r'/\*.*?\*/|//[^\n]*','',source,flags=re.S);result={}
 for table in ('NamedActorSprites','CreatureSprites','FixtureSprites'):
  match=re.search(r'\b'+table+r'\s*=\s*\{(.*?)\};',source,re.S)
  if not match:continue
  for bp,sprite in re.findall(r'\(\s*"([^"]+)"\s*,\s*"([^"]+)"',match[1]):result.setdefault(bp,[]).append('Sprites/Environment/'+sprite)
 return result

def build(root=ROOT):
 def read(p):return json.loads((root/p).read_text())
 source='Assets/Resources/Content/Blueprints/Objects.json';raw=read(source)['Objects'];resolved=resolve_blueprints(raw)
 sprites=sprite_tables((root/'Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs').read_text())
 actor=read('Assets/Resources/Content/Data/EntityVisuals.json')['Definitions']
 for row in actor:
  for bp in row['Blueprints']:
   sprites.setdefault(bp,[]).extend(row[k] for k in ('Sheet','CastSheet') if row.get(k))
 ring=read('ArtSource/SpawnRing3D/catalog.json');bindings={r['blueprint']:r for r in ring['blueprints']}
 pilot=read('ArtSource/MultiCellPilot3D/catalog.json');pilotbindings={}
 for m in pilot['models']:pilotbindings.setdefault(m['blueprint'],[]).append(m['id'])
 rows=[]
 for r in raw:
  name=r['Name'];d=resolved[name];parts=d['parts'];render=parts.get('Render',{});anc=d['ancestors'];physics=parts.get('Physics',{})
  category='actor' if 'Creature' in anc or name=='Creature' else 'item' if 'Item' in anc or name=='Item' else 'terrain' if 'Terrain' in anc or 'Wall' in anc or name in ('Terrain','Wall') else 'fixture'
  local=[]
  if name in bindings:local.append({'library':'SpawnRing3D','models':bindings[name]['models'],'zones':ring['zones'],'resolver':bindings[name].get('resolver','')})
  if name in pilotbindings:local.append({'library':'MultiCellPilot3D','models':pilotbindings[name],'condition':'MultiCellPilotPropPart exact family and footprint; blueprint match alone is insufficient'})
  rows.append({'blueprint':name,'category':category,'ancestors':anc,'displayName':render.get('DisplayName',''),'glyph':render.get('RenderString',''),
   'color':render.get('ColorString',''),'description':parts.get('Examinable',{}).get('Description',''),
   'parts':parts,'tags':d['tags'],'spriteReferences':sorted(set(sprites.get(name,[]))),
   'localArt':local,'status':coverage_status(local,[]),'globalIntegrated':False,
   'designStatus':'needs-authored-design','reviewNotes': ['No explicit display name; inspect base/dynamic usage before exclusion'] if not render.get('DisplayName') else []})
 pngs=[]
 for path in sorted((root/'Assets/Resources/Sprites').rglob('*.png')):
  resource=str(path.relative_to(root/'Assets/Resources').with_suffix(''))
  refs=[r['blueprint'] for r in rows if resource in r['spriteReferences']]
  pngs.append({'path':str(path.relative_to(root)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'directBlueprintReferences':refs,
   'mappingStatus':'direct-table-or-actor-sheet' if refs else 'requires-state-or-glyph-mapping-review'})
 return {'schemaVersion':1,'scope':'World identities and source sprites; local coverage is not global integration.',
  'blueprintSource':source,'blueprintSha256':hashlib.sha256((root/source).read_bytes()).hexdigest(),
  'summary':{'blueprints':len(rows),'spritePNGs':len(pngs),'localBlueprintCandidates':sum(bool(r['localArt']) for r in rows),'globalIntegrated':0,
   'spriteFilesRequiringRoutingReview':sum(not r['directBlueprintReferences'] for r in pngs)},'blueprints':rows,'spriteFiles':pngs,
  'unresolvedSurfaces':['Native dynamically constructed/reskinned entities','Glyph routing and fixture state variants','Town owner/component identities','Terrain atlas cells','Non-resource art and UI/effect classification']}

def main():
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=Path(__file__).parent/'coverage.json');a=p.parse_args();d=build();a.output.write_text(json.dumps(d,indent=2,ensure_ascii=False)+'\n');print(json.dumps(d['summary']))
if __name__=='__main__':main()
