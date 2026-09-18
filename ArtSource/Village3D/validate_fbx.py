"""Independent Blender FBX roundtrip checks; Unity acceptance remains separate."""
import bpy,json,sys,math
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1])
results=[]
for mid in ['oak-door','central-well','keeper-gatehouse-shell','character-teal','equipment-blade','equipment-pack']:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 bpy.ops.import_scene.fbx(filepath=str(root/'models'/f'{mid}.fbx'),use_anim=True)
 obs=list(bpy.context.scene.objects);meshes=[o for o in obs if o.type=='MESH'];rigs=[o for o in obs if o.type=='ARMATURE']
 row={'id':mid,'meshCount':len(meshes),'materialSlots':sorted(set(m.name.split('.')[0] for o in meshes for m in o.data.materials)),'rigCount':len(rigs),'sockets':sorted(o.name for o in obs if o.name.startswith('Equipment.')),'actions':[{'name':a.name,'range':list(a.frame_range)} for a in bpy.data.actions],'meshes':[{'name':o.name,'vertices':len(o.data.vertices),'faces':len(o.data.polygons),'uvLayers':len(o.data.uv_layers),'armatureModifiers':sum(1 for q in o.modifiers if q.type=='ARMATURE'),'vertexGroups':len(o.vertex_groups)} for o in meshes]}
 assert all(x['uvLayers']>=1 and x['vertices']>0 for x in row['meshes'])
 if mid=='character-teal':
  assert len(rigs)==1 and len(meshes)==1
  assert set(row['sockets'])=={'Equipment.Head','Equipment.Hand.L','Equipment.Hand.R','Equipment.Back'},row['sockets']
  row['animationTakes']=sorted({a['name'].split('|')[-1] for a in row['actions']})
  row['rigActions']=[a for a in row['actions'] if a['name'].startswith('character-teal__Rig|')]
  assert set(row['animationTakes'])=={'Idle','Walk','Interact','Attack','Hit'},row['actions']
  assert len(row['rigActions'])==5,row['actions']
  assert row['meshes'][0]['armatureModifiers']==1
 else:assert len(meshes)==(2 if mid=='central-well' else 1)
 results.append(row)
(root/'reports/fbx-blender-roundtrip.json').write_text(json.dumps({'status':'Blender import passed; Unity rig/material behavior still requires separate verification','models':results},indent=2)+'\n')
print('FBX_ROUNDTRIP_PASS',json.dumps(results))
