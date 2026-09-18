"""Run in Blender; compare rebuilt FBX semantics instead of container timestamps."""
import bpy,json,hashlib,sys,math
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:];original=Path(args[0]);rebuilt=Path(args[1]);out=Path(args[2]);out.mkdir(parents=True,exist_ok=True)
quant=lambda value: round(float(value),6)
def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':'),allow_nan=False).encode()).hexdigest()
def filehash(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def material_id(material):
 for name in ['VillagePalette','VillageWater']:
  if material.name.startswith(name):return name
 return material.name

def read_model(path,rigged):
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 bpy.context.scene.frame_set(0)
 bpy.ops.import_scene.fbx(filepath=str(path),use_anim=rigged)
 actions=[]
 for action in sorted(bpy.data.actions,key=lambda a:a.name):
  curves=[]
  for layer in action.layers:
   for strip in layer.strips:
    for bag in strip.channelbags:
     for curve in bag.fcurves:
      curves.append({'path':curve.data_path,'index':curve.array_index,'keys':[[quant(k.co[0]),quant(k.co[1])] for k in curve.keyframe_points]})
  actions.append({'name':action.name,'range':[quant(f) for f in action.frame_range],'curves':sorted(curves,key=lambda c:(c['path'],c['index']))})
 for ob in bpy.context.scene.objects:
  if ob.animation_data:ob.animation_data_clear()
  if ob.type=='ARMATURE':
   for pose in ob.pose.bones:pose.matrix_basis.identity()
 bpy.context.view_layer.update()
 meshes=[];sockets=[];skeletons=[]
 for ob in sorted(bpy.context.scene.objects,key=lambda o:o.name):
  if ob.name.startswith('Equipment.'):
   sockets.append({'name':ob.name,'parentBone':ob.parent_bone,'matrixWorld':[[quant(f) for f in row] for row in ob.matrix_world]})
  if ob.type=='ARMATURE':
   skeletons.append({'name':ob.name,'bones':[{'name':b.name,'parent':b.parent.name if b.parent else '', 'matrixLocal':[[quant(f) for f in row] for row in b.matrix_local]} for b in ob.data.bones]})
  if ob.type!='MESH':continue
  mesh=ob.data;mesh.calc_loop_triangles()
  vertices=[[quant(f) for f in ob.matrix_world@v.co] for v in mesh.vertices]
  faces=[list(p.vertices) for p in mesh.polygons]
  normals=[[quant(f) for f in p.normal] for p in mesh.polygons]
  uvs=[[[quant(f) for f in item.uv] for item in layer.data] for layer in mesh.uv_layers]
  colors=[[[quant(f) for f in item.color] for item in layer.data] for layer in mesh.color_attributes]
  groups=[g.name for g in ob.vertex_groups]
  weights=[[[groups[g.group],quant(g.weight)] for g in v.groups] for v in mesh.vertices]
  payload={'name':ob.name,'vertices':vertices,'faces':faces,'normals':normals,'smooth':[p.use_smooth for p in mesh.polygons],'uvs':uvs,'colors':colors,'materials':[material_id(a) for a in mesh.materials],'materialIndices':[p.material_index for p in mesh.polygons],'weights':weights}
  meshes.append({'name':ob.name,'vertices':len(vertices),'triangles':len(mesh.loop_triangles),'materialSlots':payload['materials'],'uvLayers':len(uvs),'contentSha256':digest(payload)})
 # Blender splits one FBX take into an action on each animated object.
 takes=sorted({a['name'].split('|')[-1] for a in actions})
 result={'meshes':meshes,'sockets':sockets,'skeletons':skeletons,'takes':takes,'animationContentSha256':digest(actions),'actionCount':len(actions)}
 return result

before=json.loads((original/'manifest.json').read_text());after=json.loads((rebuilt/'manifest.json').read_text())
assert before==after,'Manifest semantic mismatch'
assert filehash(original/'textures/VillagePalette.png')==filehash(rebuilt/'textures/VillagePalette.png'),'Palette PNG byte mismatch'
rows=[];failed=[]
for model in after['models']:
 mid=model['id'];a=read_model(original/model['path'],model['rigged']);b=read_model(rebuilt/model['path'],model['rigged'])
 ok=a==b
 if not ok:failed.append(mid)
 row={'id':mid,'semanticEqual':ok,'originalSemanticSha256':digest(a),'rebuiltSemanticSha256':digest(b),'fbxBytesEqual':filehash(original/model['path'])==filehash(rebuilt/model['path']),'rebuilt':b}
 if not ok:row['original']=a
 rows.append(row)
 print('REBUILD_MODEL',mid,'PASS' if ok else 'FAIL',flush=True)
report={'status':'passed' if not failed else 'failed','source':'Rebuilt completed refined Village3D build_scene.py from its self-contained final bundle without repository writes','artSeed':after['artSeed'],'modelCount':len(rows),'ownerCount':len(after['owners']),'manifestSemanticallyEqual':before==after,'normalizedManifestSha256':digest(after),'palettePngBytesEqual':True,'palettePngSha256':filehash(rebuilt/'textures/VillagePalette.png'),'semanticFailures':failed,'fbxByteEqualCount':sum(r['fbxBytesEqual'] for r in rows),'quantizationDecimals':6,'comparisonFields':['world-space mesh vertices','polygon topology and normals','smooth flags','UV and color layers','material slot identity and polygon assignment','vertex group names and weights','bone rest matrices and hierarchy','equipment socket names, parent bones and world matrices','animation take names, frame ranges and sampled curve key values'],'limits':['This verifies exported content, not binary FBX determinism; creation timestamps, source paths and FBX container metadata may differ.','Mesh/transform values are normalized to six decimal places for stable numeric comparison.','Does not establish Unity shader quality, skin playback, input/state binding, or measured frame performance.'],'models':rows}
(out/'rebuild-verification.json').write_text(json.dumps(report,indent=2)+'\n')
print('VILLAGE_REBUILD_AUDIT',json.dumps({k:v for k,v in report.items() if k!='models'}),flush=True)
assert not failed,'Rebuilt model semantics differ: '+str(failed)
