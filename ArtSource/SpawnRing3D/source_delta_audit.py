import bpy,sys,json,hashlib,numpy as np
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:];base=Path(args[0]);new=Path(args[1]);out=Path(args[2])
def norm(root):
 bpy.ops.wm.open_mainfile(filepath=str(root/'ring_kit.blend'));cat=json.loads((root/'catalog.json').read_text());res={}
 def f(v):return round(float(v),7)
 for row in cat['models']:
  obs=[]
  for ob in bpy.data.collections[row['id']].objects:
   data={'type':ob.type,'transform':[[f(v) for v in a] for a in ob.matrix_basis],'socketName':ob.get('socketName')}
   if ob.type=='MESH':
    data.update(verts=[[f(x) for x in v.co] for v in ob.data.vertices],faces=[list(p.vertices) for p in ob.data.polygons],smooth=[p.use_smooth for p in ob.data.polygons],uv=[[f(x) for x in v.uv] for v in ob.data.uv_layers[0].data],materials=[m.name for m in ob.data.materials],colors=[[f(x) for x in v.color] for v in ob.data.color_attributes[0].data] if ob.data.color_attributes else [],weights=[[(ob.vertex_groups[g.group].name,f(g.weight)) for g in v.groups] for v in ob.data.vertices])
   elif ob.type=='ARMATURE':data.update(bones=[(b.name,[f(x) for x in b.head_local],[f(x) for x in b.tail_local],b.parent.name if b.parent else None) for b in ob.data.bones],clips=[(t.name,[(s.name,f(s.frame_start),f(s.frame_end)) for s in t.strips]) for t in ob.animation_data.nla_tracks])
   obs.append(data)
  res[row['id']]=hashlib.sha256(json.dumps(obs,sort_keys=True,separators=(',',':')).encode()).hexdigest()
 img=bpy.data.images.load(str(root/'textures/SpawnRingPalette.png'),check_existing=False);pixels=np.empty(len(img.pixels),np.float32);img.pixels.foreach_get(pixels);pixels=pixels.reshape(img.size[1],img.size[0],4)
 swatches=[hashlib.sha256(pixels[(i//16)*128:(i//16+1)*128,(i%16)*128:(i%16+1)*128].tobytes()).hexdigest() for i in range(128)]
 return res,swatches
b,bs=norm(base);n,ns=norm(new);changed=[m for m in b if b[m]!=n[m]];sw=[i for i in range(128) if bs[i]!=ns[i]]
expected={f'ring-{family}-{v}' for family in ('floor','grass','tepui-stone') for v in range(1,4)}|{'ring-tepui-stone-0'}|{f'ring-descent-ledge-{i}' for i in range(4)}|{'ring-felling-'+s for s in ('stump-main','west-root-buttress','east-root-buttress','far-east-root-buttress','southwest-root-arch','southwest-foreground-rock','southeast-foreground-root')}
checks={'exactExpectedModelsChanged':set(changed)==expected,'onlyGroundAndUnusedAxisSwatchesChanged':set(sw)==set(range(64,76))|{56}}
r={'checks':checks,'changedSourceModels':changed,'unchangedSourceModelCount':218-len(changed),'changedPaletteSwatches':sw,'baselineSourceHashes':b,'candidateSourceHashes':n,'method':'Normalized source vertices, faces, UVs, material slots, smoothing, color attributes, transforms, weights, bone rest data, socket properties and named clip-strip ranges. Incidental mesh/object datablock names intentionally ignored; runtime gameplay IDs and rig bone names retained.'}
out.write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({'checks':checks,'changed':changed,'palette':sw}));assert all(checks.values())
