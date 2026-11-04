#!/usr/bin/env python3
"""Refresh vendored gameplay/content from one exact Caves of Ooo commit; retain browser adapters."""
from pathlib import Path
import argparse,io,json,shutil,subprocess,tarfile
root=Path(__file__).resolve().parent
cli=argparse.ArgumentParser(description=__doc__)
cli.add_argument('repository',type=Path,help='Local Caves of Ooo Git checkout')
cli.add_argument('--commit',default=json.loads((root/'source-provenance.json').read_text())['sourceCommit'])
args=cli.parse_args();repo=args.repository.resolve()
commit=subprocess.check_output(['git','rev-parse',args.commit+'^{commit}'],cwd=repo,text=True).strip()
rendering=('QudColorParser','AsciiWorldRenderPolicy','ZoneRenderHooks','EntityVisualHooks','QuestLogSnapshot','QuestLogStateBuilder','SkillBrowserModel','SkillsScreenStateBuilder','SkillsScreenSnapshot','AbilityDetailsBuilder','AbilityManagerStateBuilder','AbilityManagerSnapshot')
sprites=('opening-base','opening-cast','opening-anchors','opening-states','opening-items','opening-cellar','opening-ground','opening-motion','sill-environment')
paths=['Assets/Scripts/'+g for g in ('Gameplay','Data','Shared')]+['Assets/Resources/Content']+['Assets/Scripts/Presentation/Rendering/'+n+'.cs' for n in rendering]+['Assets/Resources/Sprites/Live16x24/'+n+'.png' for n in sprites]
tree=subprocess.check_output(['git','ls-tree','-rz',commit,*paths],cwd=repo)
entries=[]
for raw in tree.split(b'\0'):
 if not raw:continue
 header,name=raw.split(b'\t',1);mode,kind,sha=header.split();name=name.decode()
 if kind==b'blob' and name.endswith(('.cs','.json','.png')):entries.append((sha,name))
# cat-file requests only the selected blobs; git archive can hydrate unrelated assets in a partial clone.
content=subprocess.check_output(['git','cat-file','--batch'],cwd=repo,input=b'\n'.join(sha for sha,_ in entries)+b'\n')
for group in ('Source','Content'):
 d=root/'Core'/group
 if d.exists():shutil.rmtree(d)
offset=0
for sha,name in entries:
 end=content.index(b'\n',offset);header=content[offset:end].split();assert header[1]==b'blob'
 size=int(header[2]);data=content[end+1:end+1+size];offset=end+size+2;dest=None
 if name.startswith('Assets/Scripts/'):
  rel=name.removeprefix('Assets/Scripts/')
  if rel.startswith('Presentation/Rendering/'):rel=rel.removeprefix('Presentation/')
  dest=root/'Core/Source'/rel
 elif name.startswith('Assets/Resources/Content/'):dest=root/'Core/Content'/name.removeprefix('Assets/Resources/Content/')
 elif name.endswith('.png'):dest=root/'Client/wwwroot/art'/Path(name).name
 if dest:
  dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
subs={'Gameplay/World/Map/OverworldZoneManager.cs':('zoneID.GetHashCode()','CooRun.StableHash.Of(zoneID)'), 'Gameplay/World/Map/ZoneManager.cs':('zoneID.GetHashCode()','CooRun.StableHash.Of(zoneID)'), 'Gameplay/Economy/TraderRestockSystem.cs':('e.ID.GetHashCode()','CooRun.StableHash.Of(e.ID)'), 'Gameplay/Materials/GasPoisonPart.cs':('gasType?.GetHashCode() ?? 0','CooRun.StableHash.Of(gasType)'), 'Gameplay/Settlements/SettlementSiteVisuals.cs':('(entity.ID ?? "").GetHashCode()','CooRun.StableHash.Of(entity.ID ?? "")')}
for file,(old,new) in subs.items():
 p=root/'Core/Source'/file;text=p.read_text();assert old in text,f'Adaptation target changed: {file}';p.write_text(text.replace(old,new))
p=root/'Core/Source/Gameplay/Save/SaveSystem.cs';text=p.read_text();needle='            state.TurnManager?.Activate();';assert text.count(needle)==1
p.write_text(text.replace(needle,(root/'scripts/load-validation.cs.txt').read_text()+needle))
manifest={'repository':'stevenrichter16/caves_of_ooo','sourceCommit':commit,'sourceFiles':len(list((root/'Core/Source').rglob('*.cs'))),'contentFiles':len(list((root/'Core/Content').rglob('*.json'))),'changes':['Standalone .NET compatibility layer and embedded Resources; no Unity runtime','Stable string hashes and approximate Perlin noise from the existing headless adaptation; no desktop seed parity claim','Validate complete imported sessions before publishing live singleton state','Browser facade and canvas/DOM presentation kept outside the imported core']}
(root/'source-provenance.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(manifest))
