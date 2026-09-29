"""Isolated original furrowstalker source export; no shared or Unity writes."""
import bpy,bmesh,math,json,argparse,sys,ast,os,zlib,struct,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT','/Users/steven/caves-of-ooo'))
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--render',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=args.output.resolve()
for name in ('models','textures','renders'):(OUT/name).mkdir(parents=True,exist_ok=True)
kit=json.loads(Path(__file__).with_name('kit.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1;SCENE.render.fps=24
MODELS={};CURRENT=None
exec(compile((ROOT/'ArtSource/SpawnRing3D/mesh_kit.py').read_text(),'borrowed_mesh_kit','exec'),globals())
# Source swatches are exact sRGB. PNG writes explicit bytes, independent of any
# Blender scene/color-management setting. The native builder borrows the glade palette.
def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
rgb=[tuple(int(h[i:i+2],16)for i in (1,3,5))for h in kit['palette']]
png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',24,1,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b'\0'+bytes(v for c in rgb for v in c)))+chunk(b'IEND',b'')
(OUT/'textures/SpreadBiomePalette.png').write_bytes(png)
img=bpy.data.images.load(str(OUT/'textures/SpreadBiomePalette.png'));img.colorspace_settings.name='sRGB'
MAT=bpy.data.materials.new('SpreadBiomePalette');MAT.use_nodes=True;tex=MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;tex.interpolation='Closest';shader=MAT.node_tree.nodes.get('Principled BSDF');MAT.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color']);shader.inputs['Roughness'].default_value=1;WATER=bpy.data.materials.new('SemanticWater');WATER.use_nodes=True;WATER.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(0,.667,.667,1);WATER.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.85
COLORS=[(str(i),tuple(v/255 for v in c))for i,c in enumerate(rgb)];CINDEX={str(i):i for i in range(24)}
def color_mesh(mesh,color):
 idx=int(color);mesh.update();uv=mesh.uv_layers.new(name='UVMap')
 for q in uv.data:q.uv=((idx+.5)/24,.5)
 attr=mesh.color_attributes.new(name='SpreadBiomeColor',type='BYTE_COLOR',domain='CORNER')
 for q in attr.data:q.color=(*COLORS[idx][1],1)
 mesh.materials.append(MAT)
tree=ast.parse((ROOT/'ArtSource/SpawnRing3D/build_ring.py').read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef)and n.name=='rig_parts');exec(compile(ast.Module(body=[fn],type_ignores=[]),'borrowed_rig_parts','exec'),globals())
def xyz(p):return(p[0],p[2],p[1])

def author_clips(rig,desc):
 for tr in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(tr)
 for clip,cfg in desc['motion'].items():
  action=bpy.data.actions.new(desc['id']+'__'+clip);rig.animation_data.action=action;length=cfg['frames']
  samples=sorted(set([0,length/4,length/2,3*length/4,length]))
  for frame in samples:
   phase=frame/length*math.tau;one=math.sin(frame/length*math.pi)
   for i,pb in enumerate(rig.pose.bones):
    pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
    if pb.name!='Root':
     if clip=='Idle':
      if pb.name=='Body':pb.scale=(1+cfg['breathScale']*math.sin(phase),1,1+cfg['breathScale']*.8*math.sin(phase))
      elif pb.name=='Head':pb.rotation_euler[1]=.018*math.sin(phase)
      elif pb.name.startswith('Tail'):pb.rotation_euler[1]=.025*math.sin(phase)
     elif clip=='Walk':
      if pb.name.startswith('Leg'):pb.rotation_euler[0]=cfg['legPitch']*math.sin(phase+(math.pi if '.L' in pb.name else 0)+(math.pi if 'Rear' in pb.name else 0))
      elif pb.name=='Body':pb.rotation_euler[2]=.018*math.sin(phase)
      elif pb.name.startswith('Tail'):pb.rotation_euler[1]=.06*math.sin(phase)
     elif clip=='Interact':
      if pb.name=='Head':pb.rotation_euler[0]=cfg['headPitch']*one
      elif pb.name=='Neck':pb.rotation_euler[0]=.12*one
      elif pb.name=='Jaw':pb.rotation_euler[0]=cfg['jawPitch']*one
     elif clip=='Attack':
      if pb.name=='Head':pb.rotation_euler[0]=cfg['headPitch']*one
      elif pb.name=='Jaw':pb.rotation_euler[0]=cfg['jawPitch']*one
      elif pb.name=='Neck':pb.rotation_euler[0]=-.08*one
      elif pb.name=='Body':pb.rotation_euler[0]=-.045*one
     elif clip=='Hit':
      if pb.name=='Body':pb.rotation_euler[0]=cfg['bodyPitch']*one;pb.rotation_euler[2]=.065*one
      elif pb.name=='Head':pb.rotation_euler[0]=.11*one
    for prop in ('rotation_euler','location','scale'):pb.keyframe_insert(data_path=prop,frame=frame,group=pb.name)
  # Modern action channels may live in a layered action; Blender's default
  # interpolation is smooth. Root is explicitly keyed to identity throughout.
  tr=rig.animation_data.nla_tracks.new();tr.name=clip;strip=tr.strips.new(clip,0,action);strip.name=clip;tr.mute=True
 rig.animation_data.action=None
 for pb in rig.pose.bones:pb.rotation_euler=(0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)

def posed_points(objects):
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();out={}
 for part,ob in objects.items():
  e=ob.evaluated_get(dg);mesh=e.to_mesh()
  try:out[part]=[e.matrix_world@v.co for v in mesh.vertices]
  finally:e.to_mesh_clear()
 return out

def centroid(points):return sum(points,Vector())/len(points)
def observe_pose(rig,action,frame,objects,base):
 rig.animation_data.action=action;SCENE.frame_set(int(frame),subframe=frame-int(frame));pts=posed_points(objects)
 muzzle=centroid(pts['flat-tapered-muzzle']);jaw=centroid(pts['lower-jaw'])
 return dict(frame=frame,muzzleWorldUp=muzzle.z,jawGap=(muzzle-jaw).length,minWorldUp=min(v.z for vs in pts.values() for v in vs),
  maxVertexDeltaFromFirst=max((v-base[k][i]).length for k,vs in pts.items() for i,v in enumerate(vs)),rootTranslation=list(rig.pose.bones['Root'].matrix.translation),
  tailContactVertices={name:[list(v)for v in pts[name]]for name in ('tail-root','tail-bend','tail-tip')}),pts

def baked_preview(mid,clip,pts,objects):
 coll=bpy.data.collections.new('Preview_'+clip)
 for name,vs in pts.items():
  source=objects[name].data;mesh=source.copy()
  for v,pt in zip(mesh.vertices,vs):v.co=pt
  ob=bpy.data.objects.new('Preview_'+clip+'__'+name,mesh);coll.objects.link(ob)
 return coll

rows=[];observations=[];pose_collections={}
for desc in kit['models']:
 mid=desc['id'];coll=model(mid,'actor' if desc['rigged'] else 'entity','feet-root');SCENE.collection.children.link(coll);weights={};objects={}
 for piece in desc['boxes']:
  ob=box(piece['part'],xyz(piece['center']),xyz(piece['size']),str(piece['color']),bevel=0);weights[ob.name]=piece['bone'];objects[piece['part']]=ob
 rig=None
 if desc['rigged']:
  rig_parts(mid,[(b['name'],xyz(b['head']),xyz(b['tail']),b['parent'])for b in desc['bones']],weights,'quadruped')
  rig=next(o for o in coll.objects if o.type=='ARMATURE');author_clips(rig,desc)
 bpy.context.view_layer.update()
 verts=[ob.matrix_world@v.co for ob in objects.values()for v in ob.data.vertices]
 lo=[min(v[i] for v in verts)for i in range(3)];hi=[max(v[i] for v in verts)for i in range(3)]
 meshes=[]
 for name,ob in objects.items():
  mesh=ob.data
  meshes.append(dict(name=name,vertices=[list(v.co)for v in mesh.vertices],faces=[list(f.vertices)for f in mesh.polygons],triangles=sum(len(f.vertices)-2 for f in mesh.polygons),
   normals=[list(f.normal)for f in mesh.polygons],uv=[list(v.uv)for v in mesh.uv_layers[0].data],weights=[[[ob.vertex_groups[g.group].name,g.weight]for g in v.groups]for v in mesh.vertices]if rig else []))
 poses={}
 if rig:
  for clip,cfg in desc['motion'].items():
   action=next(t.strips[0].action for t in rig.animation_data.nla_tracks if t.name==clip);rig.animation_data.action=action;SCENE.frame_set(0);base=posed_points(objects)
   frames=[]
   for frame in [0,cfg['frames']/4,cfg['frames']/2,3*cfg['frames']/4,cfg['frames']]:
    measured,points=observe_pose(rig,action,frame,objects,base);frames.append(measured)
    if frame==cfg['frames']/2 and clip!='Walk' or frame==cfg['frames']/4 and clip=='Walk':pose_collections[clip]=baked_preview(mid,clip,points,objects)
   poses[clip]=frames
  rig.animation_data.action=None
  for pb in rig.pose.bones:pb.rotation_euler=(0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
  bpy.context.view_layer.update()
 export_collection(mid)
 rows.append(dict(id=mid,sourceBlueprint=desc['blueprint'],path='models/'+mid+'.fbx',rigFamily=desc['rigFamily'],rigged=desc['rigged'],clips=desc['clips'],sockets=[],bones=[b['name']for b in desc['bones']],triangles=sum(m['triangles']for m in meshes),boundsBlender=dict(min=lo,max=hi),sha256=hashlib.sha256((OUT/'models'/f'{mid}.fbx').read_bytes()).hexdigest()))
 observations.append(dict(id=mid,rigged=desc['rigged'],bones=[b['name']for b in desc['bones']],meshCount=len(meshes),meshes=meshes,bounds=dict(min=lo,max=hi),poses=poses))
 SCENE.collection.children.unlink(coll)
(OUT/'catalog.json').write_text(json.dumps(dict(schemaVersion=1,id=kit['id'],palette=kit['palette'],models=rows),indent=2)+'\n')
(OUT/'source-observations.json').write_text(json.dumps(dict(boundary='Blender source geometry and evaluated animation only. Not Unity imported-buffer, played motion, source behavior, camera or gameplay acceptance.',forms=observations),indent=2)+'\n')
# Readable original source review. Borrowed palette/camera conventions, no effects.
SCENE.world.use_nodes=True;SCENE.world.node_tree.nodes['Background'].inputs[0].default_value=(.28,.32,.30,1);SCENE.world.node_tree.nodes['Background'].inputs[1].default_value=.8
floor=bpy.data.materials.new('ReviewGround');floor.use_nodes=True;floor.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.003,.025,.021,1);floor.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=1
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));bpy.context.object.data.materials.append(floor)
li=bpy.data.lights.new('Key','AREA');li.energy=600;li.size=4;ob=bpy.data.objects.new('Key',li);SCENE.collection.objects.link(ob);ob.location=(-2,-3,6);ob.rotation_euler=(Vector((0,0,0))-ob.location).to_track_quat('-Z','Y').to_euler()
ca=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',ca);SCENE.collection.objects.link(cam);SCENE.camera=cam;ca.type='ORTHO'
SCENE.render.engine='CYCLES';SCENE.cycles.samples=24;SCENE.render.threads_mode='FIXED';SCENE.render.threads=2;SCENE.render.resolution_percentage=100;SCENE.view_settings.view_transform='Standard';SCENE.view_settings.look='Medium High Contrast'
main=[instance(rows[0]['id'],loc=(-.65,0,0)),instance(rows[1]['id'],loc=(.65,0,0))]
SCENE.render.resolution_x=1600;SCENE.render.resolution_y=1100;ca.ortho_scale=3.25
cam.location=(0,-.01,7);cam.rotation_euler=(Vector((0,0,.1))-cam.location).to_track_quat('-Z','Y').to_euler()
if args.render:SCENE.render.filepath=str(OUT/'renders/furrowstalker-top.png');bpy.ops.render.render(write_still=True)
cam.location=(3,-4,3);cam.rotation_euler=(Vector((0,0,.15))-cam.location).to_track_quat('-Z','Y').to_euler()
if args.render:SCENE.render.filepath=str(OUT/'renders/furrowstalker-oblique.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'furrowstalker-review.blend'),compress=True)
for ob in main:ob.hide_render=True
pose_instances=[]
for i,(clip,coll)in enumerate(pose_collections.items()):
 ob=bpy.data.objects.new('Pose_'+clip,None);ob.instance_type='COLLECTION';ob.instance_collection=coll;SCENE.collection.objects.link(ob);ob.location=((i-2)*1.25,0,0);pose_instances.append(ob)
 font=bpy.data.curves.new('Label_'+clip,'FONT');font.body=clip;font.align_x='CENTER';font.size=.18;label=bpy.data.objects.new('Label_'+clip,font);SCENE.collection.objects.link(label);label.location=((i-2)*1.25,-1.0,.02);label.rotation_euler=(0,0,0)
SCENE.render.resolution_x=2000;SCENE.render.resolution_y=850;ca.ortho_scale=7.1;cam.location=(0,-5,4.8);cam.rotation_euler=(Vector((0,-.15,.1))-cam.location).to_track_quat('-Z','Y').to_euler()
if args.render:SCENE.render.filepath=str(OUT/'renders/furrowstalker-poses.png');bpy.ops.render.render(write_still=True)
print('FURROWSTALKER_SOURCE',json.dumps(rows))
