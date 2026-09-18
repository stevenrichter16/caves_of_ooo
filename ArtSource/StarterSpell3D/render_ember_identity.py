"""Saved-export Ember proof at native scale and true 1/4-cell route anchors.

Offline Blender pixels with original materials. Camera recentred for inspection
only; unchanged 56-degree pitch and 33.38 px/cell. No canonical source save.
"""
import argparse,bpy,hashlib,json,math,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT))
from audit_ember_identity import pose_at,mapped_forward
from audit_readability import points
from normalize_png import remove_density_chunk
ap=argparse.ArgumentParser();ap.add_argument('--output',required=True);ap.add_argument('--detail',action='store_true');args=ap.parse_args(sys.argv[sys.argv.index('--')+1:])
out=Path(args.output);out.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
source=ROOT/'starter_spells.blend';path=ROOT/'runtime/starter_spell_library.json';data=json.loads(path.read_text());manifest=json.loads((ROOT/'manifest.json').read_text())
assert data['sourceBlendSha256']==sha(source)
study=next(s for s in data['studies'] if s['id']=='ember_spit');spec=manifest['studies'][0];meshes={m['id']:m for m in data['meshes']};origin=Vector((-2.35,0,0));rows=[]
cases=[(4,0,27),(4,0,29),(4,0,29.5),(4,0,37.5),(4,0,45.5),(4,45,29),(1,0,29.5),(1,45,29.5)]
for cells,degrees,frame in cases:
    bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.data.scenes[spec['scene']];bpy.context.window.scene=scene
    scene.frame_set(int(frame),subframe=frame%1);bpy.context.view_layer.update()
    angle=math.radians(degrees);step=math.sqrt(2) if degrees%90 else 1;length=cells*step
    def turn(p):return Vector((p.x*math.cos(angle)-p.y*math.sin(angle),p.x*math.sin(angle)+p.y*math.cos(angle),p.z))
    for ob in scene.objects:
        if ob.get('transientEffect') or ob.name.startswith('Stage__'):ob.hide_render=True
    scene.objects[spec['actorRoot']].rotation_euler.z+=angle
    for ob in scene.objects:
        if ob.name.startswith('StudyOnly__neutral_target'):
            ob.location=origin+turn(Vector((length,0,0)));ob.rotation_euler.z+=angle
    for x in range(-1,7):
        for y in range(-2,6):
            bpy.ops.mesh.primitive_cube_add(size=1,location=origin+Vector((x,y,-.075)));ob=bpy.context.object;ob.name='Proof__one_metre_cell';ob.scale=(.975,.975,.12);ob.data.materials.append(bpy.data.materials['SpellFolk_stone' if (x+y)%3 else 'SpellFolk_stone_light'])
    rendered=0
    for piece in study['pieces']:
        if piece['role']=='TargetImpact' and frame<study['contactFrame']:continue
        pose=pose_at(piece,frame)
        if min(pose['scale'])<=.0001:continue
        me=meshes[piece['meshId']];values=points(me,pose)
        if piece['role']=='ProjectileTrail':shift=mapped_forward(study,pose['position'][2],length)-pose['position'][2]
        elif piece['role']=='ProjectileHead':
            lo=int(frame);t=frame-lo;forward=(study['projectileProgress'][lo]*(1-t)+study['projectileProgress'][min(110,lo+1)]*t)*3
            shift=mapped_forward(study,forward,length)-forward
        else:shift=length
        vertices=[tuple(origin+turn(Vector((z+shift,x,y)))) for x,y,z in values]
        faces=[tuple(me['triangles'][i:i+3]) for i in range(0,len(me['triangles']),3)]
        geo=bpy.data.meshes.new('EmberNativeProof__'+piece['id']);geo.from_pydata(vertices,[],faces);geo.update();ob=bpy.data.objects.new(geo.name,geo);scene.collection.objects.link(ob)
        geo.materials.append(bpy.data.materials[data['materials'][me['materialIndex']]['id']]);colors=me.get('vertexColors')
        if colors:
            attr=geo.color_attributes.new(name='SpellGlow',type='FLOAT_COLOR',domain='POINT')
            for i,item in enumerate(attr.data):item.color=colors[i*4:i*4+4]
        rendered+=1
    center=origin+turn(Vector((length*.5,0,0)));scene.camera.location+=center-Vector((.15,0,0))
    width=800 if args.detail else 460;scene.camera.data.ortho_scale=10 if args.detail else width/33.38
    scene.render.resolution_x=width;scene.render.resolution_y=500 if args.detail else 340;scene.render.resolution_percentage=100;scene.cycles.samples=20
    name=f'ember-{cells}cells-{degrees}deg-frame{frame:g}'+('-detail' if args.detail else '-native-scale')+'.png';dest=out/name;scene.render.filepath=str(dest)
    bpy.ops.render.render(write_still=True);remove_density_chunk(dest)
    actualtime=.22+max(0,min(frame,study['contactFrame'])-22)/(study['contactFrame']-22)*cells*.025+max(0,frame-study['contactFrame'])/100
    rows.append(dict(path=name,sha256=sha(dest),cells=cells,directionDegrees=degrees,routeMetres=length,authorFrame=frame,mappedRuntimeSeconds=actualtime,renderedPieces=rendered,cameraCenter=list(center)))
    print('EMBER_PROOF_READY '+str(dest),flush=True)
receipt=dict(sourceBlendSha256=sha(source),librarySha256=sha(path),pixelsPerCell=80 if args.detail else 33.38,cameraPitchDegrees=56,images=rows,boundary='Offline reconstruction of actual exported meshes/TRS under native route mapping. Saved Blender materials; centred inspection camera. Not Unity pixels or gameplay performance. No source save.')
(out/'render-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
