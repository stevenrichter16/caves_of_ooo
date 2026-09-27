"""Record/verify the exact four-model art rewrite boundary; never changes Assets."""
import pathlib,hashlib,re,json,sys
ROOT=pathlib.Path('/Users/steven/caves-of-ooo'); HERE=pathlib.Path('/tmp/coo-spread-pools')
POOL_IDS=[f'ring-spray-pool-{i}' for i in range(4)]
def h(b):return hashlib.sha256(b).hexdigest()
def digest(p):return h((ROOT/p).read_bytes())
def sections(s,start,pattern,key):
 body=s.split(start,1)[1];starts=list(re.finditer(pattern,body,re.M));return {key(m):h(body[m.start():starts[i+1].start() if i+1<len(starts) else len(body)].encode()) for i,m in enumerate(starts)}
def snapshot():
 style='Assets/Resources/SpreadNativeStyle3D/Library.asset';voxel='Assets/Resources/VoxelWorld/Library.asset';v=(ROOT/voxel).read_text()
 guids=[re.search(r'^guid: (\w+)',(ROOT/f'Assets/Art3D/SpawnRing/Models/{i}.fbx.meta').read_text(),re.M)[1] for i in POOL_IDS]
 keys=re.findall(r'^    SourceKey: (\S+)',v,re.M);selected=[k for k in keys if any(k.startswith(g+'_') for g in guids)];assert len(selected)==8, selected
 assets=set()
 for folder in ['Assets/Resources/SpreadNativeStyle3D','Assets/Art3D/VoxelWorld','Assets/Resources/VoxelWorld','Assets/Art3D/SpawnRing/Materials','Assets/Art3D/SpawnRing/Textures']:
  p=ROOT/folder
  if p.exists():assets.update(str(a.relative_to(ROOT)) for a in p.rglob('*') if a.is_file())
 for model in POOL_IDS:
  for kind,ext in [('Models','fbx'),('Prefabs','prefab')]:
   p=f'Assets/Art3D/SpawnRing/{kind}/{model}.{ext}';assert (ROOT/p).is_file();assets.add(p);assets.add(p+'.meta')
 # Match SourceKey per row even though its header contains the raw local file ID.
 rows=v.split('  - Source: ')[1:];bindings={re.search(r'^    SourceKey: (\S+)',r,re.M)[1]:h(r.encode()) for r in rows}
 return {'files':{p:digest(p) for p in sorted(assets)},'styleEntries':sections((ROOT/style).read_text(),'  Entries:\n',r'^  - Id: (\S+)',lambda m:m[1]),'voxelBindings':bindings,'selectedSourceKeys':selected,'poolIds':POOL_IDS,'allowedChangedAssets':[style,voxel]+[f'Assets/Art3D/VoxelWorld/Meshes/{k}.asset' for k in selected]+[f'Assets/Resources/SpreadNativeStyle3D/{i}.{ext}' for i in POOL_IDS for ext in ['asset','prefab']]}
def main():
 mode=sys.argv[1];p=HERE/'preservation-before.json'
 if mode=='before':
  assert not p.exists(),'Keep existing baseline immutable; use a distinct directory for a new cohort.'
  p.write_text(json.dumps(snapshot(),indent=2)+'\n');print(p);return
 before=json.loads(p.read_text());after=snapshot();fail=[]
 if set(before['files'])!=set(after['files']):fail.append('Asset inventory changed')
 allowed=set(before['allowedChangedAssets']); changed=[]
 for k,v in before['files'].items():
  if after['files'].get(k)!=v:
   changed.append(k)
   if k not in allowed:fail.append('Unrelated asset bytes changed: '+k)
 for k,v in before['styleEntries'].items():
  if k not in POOL_IDS and after['styleEntries'].get(k)!=v:fail.append('Unrelated adopted entry changed: '+k)
 for k,v in before['voxelBindings'].items():
  if k not in before['selectedSourceKeys'] and after['voxelBindings'].get(k)!=v:fail.append('Unrelated voxel row changed: '+k)
 if set(before['styleEntries'])!=set(after['styleEntries']):fail.append('Adopted entry roster changed')
 if set(before['voxelBindings'])!=set(after['voxelBindings']):fail.append('Voxel entry roster changed')
 report={'passed':not fail,'failures':fail,'changedAssets':changed,'unselectedAdoptedEntries':len(before['styleEntries'])-4,'unselectedVoxelBindings':len(before['voxelBindings'])-8,'before':str(p),'after':after}
 (HERE/'preservation-after.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='after'},indent=2));raise SystemExit(bool(fail))
if __name__=='__main__':main()
