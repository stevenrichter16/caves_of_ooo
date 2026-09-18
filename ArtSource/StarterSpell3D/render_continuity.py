"""Fixed-scale Blender proof of actual per-owner cardinal/diagonal placement.

The immutable exported meshes/poses are evaluated with native anchor spacing.
Original saved Blender materials, caster pose, camera pitch and square-pixel
correction are used. This is an offline geometry proof, not a Unity capture.
"""
import argparse, bpy, hashlib, json, math, sys
from pathlib import Path
from mathutils import Vector, Quaternion
ROOT=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT))
from normalize_png import remove_density_chunk
ap=argparse.ArgumentParser();ap.add_argument('--output',required=True);args=ap.parse_args(sys.argv[sys.argv.index('--')+1:])
out=Path(args.output);out.mkdir(parents=True,exist_ok=True)
source=ROOT/'starter_spells.blend';library_path=ROOT/'runtime/starter_spell_library.json'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
data=json.loads(library_path.read_text());manifest=json.loads((ROOT/'manifest.json').read_text());assert sha(source)==data['sourceBlendSha256']
meshes={m['id']:m for m in data['meshes']};SOURCE=Vector((-2.35,0,0));PPU=33.38;rows=[]
cases=[]
for sid in ['jet_blast','ground_surge']:
 for direction in [0,45]:
  for after in [8,21]:cases.append((sid,direction,after,4,None))
cases += [('jet_blast',45,14,4,-1),('ground_surge',45,14,1,None),('ground_surge',45,14,2,None)]
for sid,direction,after,prefix,omit in cases:
 bpy.ops.wm.open_mainfile(filepath=str(source));spec=next(s for s in manifest['studies'] if s['id']==sid);entry=next(s for s in data['studies'] if s['id']==sid)
 scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene;frame=math.ceil(spec['contactFrame'])+after;scene.frame_set(frame);bpy.context.view_layer.update()
 angle=math.radians(direction);cs=math.cos(angle);sn=math.sin(angle);step=math.sqrt(2) if direction%90 else 1
 def turn(p):return Vector((p.x*cs-p.y*sn,p.x*sn+p.y*cs,p.z))
 for ob in scene.objects:
  if ob.get('transientEffect') or ob.name.startswith('Stage__'):ob.hide_render=True
 actor=scene.objects[spec['actorRoot']];actor.rotation_euler.z+=angle
 for ob in scene.objects:
  if ob.name.startswith('StudyOnly__neutral_target'):
   ob.location=SOURCE+turn(Vector((entry['authoredDistanceCells']*step,0,0)));ob.rotation_euler.z+=angle
   if sid=='ground_surge' and prefix<4:
    for child in ob.children_recursive:child.hide_render=True
 # A larger neutral cell board is only a proof backdrop. Its one-metre grid
 # makes the true diagonal centres legible without changing the pixel scale.
 for x in range(-1,6):
  for y in range(-2,6):
   bpy.ops.mesh.primitive_cube_add(size=1,location=SOURCE+Vector((x,y,-.075)));ob=bpy.context.object;ob.name='Proof__one_metre_cell';ob.scale=(.975,.975,.12);ob.data.materials.append(bpy.data.materials['SpellFolk_stone' if (x+y)%3 else 'SpellFolk_stone_light'])
 count=0
 for piece in entry['pieces']:
  f=piece['forwardCell'];l=piece['lateralCell']
  if piece['role']=='GroundCell' and f>prefix:continue
  if piece['role']=='ConeCell' and f==2 and l==omit:continue
  pose=piece['poses'][frame]
  if min(pose['scale'])<=.0001:continue
  if piece['role'] not in ('GroundCell','ConeCell'):raise ValueError('This bounded proof expects only cell-authored Jet/Surge')
  me=meshes[piece['meshId']];scale=Vector(pose['scale']);position=Vector(pose['position']);q=pose['rotation'];rot=Quaternion((q[3],q[0],q[1],q[2]))
  if piece['visualBounds']=='CellSurface' and step>1:scale.x/=step;scale.z/=step;position.x/=step;position.z/=step
  vertices=[]
  for i in range(0,len(me['vertices']),3):
   v=Vector(me['vertices'][i:i+3]);v=Vector((v.x*scale.x,v.y*scale.y,v.z*scale.z));v=rot@v+position
   author=Vector((v.z+f*step,v.x+l*step,v.y));vertices.append(tuple(SOURCE+turn(author)))
  faces=[tuple(me['triangles'][i:i+3]) for i in range(0,len(me['triangles']),3)]
  mesh=bpy.data.meshes.new('NativePoseProof__'+piece['id']);mesh.from_pydata(vertices,[],faces);mesh.update();ob=bpy.data.objects.new(mesh.name,mesh);scene.collection.objects.link(ob)
  mesh.materials.append(bpy.data.materials[data['materials'][me['materialIndex']]['id']]);colors=me.get('vertexColors')
  if colors:
   attr=mesh.color_attributes.new(name='SpellGlow',type='FLOAT_COLOR',domain='POINT')
   for i,item in enumerate(attr.data):item.color=colors[i*4:i*4+4]
  count+=1
 center=SOURCE+turn(Vector((entry['authoredDistanceCells']*step*.5,0,0)))
 scene.camera.location+=center-Vector((.15,0,0));scene.camera.data.ortho_scale=420/PPU
 scene.render.resolution_x=420;scene.render.resolution_y=340;scene.render.resolution_percentage=100;scene.cycles.samples=20
 name=f'{sid}-direction{direction}-hold{after}-prefix{prefix}'+(f'-missing{omit}' if omit is not None else '')+'.png'
 path=out/name;scene.render.filepath=str(path);bpy.ops.render.render(write_still=True);remove_density_chunk(path)
 rows.append(dict(path=name,sha256=sha(path),spell=sid,directionDegrees=direction,gridStep=step,nativeFrame=frame,nativeSeconds=frame/100,prefix=prefix,omittedSideOwner=omit,renderedPieces=count,cameraCenter=list(center)))
 print('CONTINUITY_PROOF_READY '+str(path),flush=True)
receipt=dict(sourceBlendSha256=sha(source),librarySha256=sha(library_path),pixelsPerCell=PPU,cameraPitchDegrees=56,squarePixelPng=True,images=rows,boundary='Actual immutable exported mesh/pose reconstruction with saved Blender materials and real per-owner native grid spacing. Camera centred offline for proof, pitch/pixel density unchanged. Not Unity pixels; no source save.')
(out/'render-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
