"""Private deterministic source bake: exact borrowed FBX -> scoped voxel data.
Uses no Unity and never rewrites borrowed source. Explicit output only."""
import bpy,bmesh,json,math,hashlib,sys,argparse,ast,importlib.util
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--repo',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--only-new',action='store_true');p.add_argument('--variants-only',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
ROOT=args.repo.resolve();HERE=Path(__file__).parent;OUT=args.output.resolve();OUT.mkdir(parents=True,exist_ok=True);(OUT/'models').mkdir(exist_ok=True)
rows=json.loads((HERE/('variant-designs.json' if args.variants_only else 'designs.json')).read_text())['items']
if args.only_new:rows=[r for r in rows if not r.get('source')]
SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1
MODELS={};CURRENT=None;MAT=bpy.data.materials.new('PortableSourcePalette');WATER=MAT
exec(compile((ROOT/'ArtSource/SpawnRing3D/mesh_kit.py').read_text(),'borrowed_mesh_primitives','exec'),globals());CINDEX={n:i for i,(n,c) in enumerate(COLORS)}
spec=importlib.util.spec_from_file_location('forms',HERE/('dynamic_forms.py' if args.variants_only else 'new_forms.py'));forms=importlib.util.module_from_spec(spec);spec.loader.exec_module(forms)
# Exact accepted24 cells; six explicit semantic accents add red, cold-cyan,
# mind-violet, document-blue, ember-orange and gold without global palette edits.
tree=ast.parse((ROOT/'ArtSource/ReferenceGlade3D/build_kit.py').read_text());PAL=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='PALETTE' for t in n.targets))
assert len(PAL)==24
PAL+=['#AF4C3D','#61AFB0','#8B75A8','#4D8197','#CD753B','#D4B659']
MAP={'earth':20,'moss':6,'moss_light':4,'moss_dark':3,'stone':11,'stone_light':12,'stone_dark':10,'stone_warm':12,'wood':20,'wood_light':21,'wood_dark':19,'wood_end':18,'iron':10,'iron_light':11,'rope':22,'soil':19,'leaf':7,'leaf_light':9,'leaf_dark':2,'cream':17,'red':24,'gold':29,'teal':3,'violet':26,'water':0,'water_light':25,'flower_white':17,'flower_pink':24,'hood_teal':3,'hood_gold':29,'hood_violet':26,'hood_olive':6,'skin':18,'face_shadow':23,'leather':19,'steel':12,'cobble':11,'cobble_light':12,'cobble_dark':10,'tile_earth':20,'roof_moss':6,'roof_moss_light':4,'roof_moss_dark':3,'roof_stone':10,'flower_yellow':29,'cabbage':8,'cabbage_light':9,'bread':21,'cloth_shadow':10,'cloth_light':22,'paper':17,'terracotta':18,'book_blue':27,'book_red':24,'book_green':6,'ember':28,'axis_x':24,'axis_y':9,'axis_z':27,'black':23,'grass':6,'grass_light':4,'pebble':11,'rim':17}
assert set(MAP)==set(CINDEX)
if args.variants_only:
    PAL += ['#5454FF','#54FFFF','#54FF54','#545454','#FF5454','#FFFF54','#FFFFFF','#00ABAB','#00AB00','#AB00AB','#ABAB00','#ABABAB']
PITCH=.035

def wipe():
    for ob in list(bpy.data.objects):bpy.data.objects.remove(ob,do_unlink=True)
    # Data blocks are owned only by this private Blender process.
    for mesh in list(bpy.data.meshes):
        if mesh.users==0:bpy.data.meshes.remove(mesh)

def collect(objects,imported):
    vertices=[];triangles=[];colors=[]
    for ob in objects:
        if ob.type!='MESH':continue
        mesh=ob.data;mesh.calc_loop_triangles();mat=ob.matrix_world if imported else ob.matrix_basis
        offset=len(vertices);vertices.extend(mat@v.co for v in mesh.vertices)
        if not mesh.uv_layers:raise ValueError('missing palette UV')
        for tri in mesh.loop_triangles:
            ids=tuple(offset+i for i in tri.vertices);a,b,c=[vertices[i] for i in ids]
            if (b-a).cross(c-a).length<1e-10:raise ValueError('degenerate source triangle')
            triangles.append(ids)
            uv=sum((mesh.uv_layers[0].data[i].uv for i in tri.loops),Vector((0,0)))/3
            index=int(math.floor(uv.x*16))+16*int(math.floor(uv.y*8))
            if not 0<=index<len(COLORS):raise ValueError(('bad source palette',index,tuple(uv)))
            colors.append(MAP[COLORS[index][0]])
    if not vertices or any(not math.isfinite(v) for p in vertices for v in p):raise ValueError('empty/nonfinite source')
    return vertices,triangles,colors

def bake(vertices,triangles,colors):
    lo=Vector([min(v[i] for v in vertices) for i in range(3)]);hi=Vector([max(v[i] for v in vertices) for i in range(3)])
    if max(abs(lo.x),abs(hi.x),abs(lo.y),abs(hi.y))>.501 or lo.z<-.025:raise ValueError(('source outside ground cell',list(lo),list(hi)))
    counts=[max(1,math.ceil((hi[i]-lo[i])/PITCH)) for i in range(3)];steps=[(hi[i]-lo[i])/counts[i] for i in range(3)]
    if any(s<=0 for s in steps) or math.prod(counts)>100000:raise ValueError('invalid voxel work budget')
    tree=BVHTree.FromPolygons(vertices,triangles,all_triangles=True);voxels={};radius=math.sqrt(sum(s*s for s in steps))*.51
    for x in range(counts[0]):
      for y in range(counts[1]):
       for z in range(counts[2]):
        point=lo+Vector(((x+.5)*steps[0],(y+.5)*steps[1],(z+.5)*steps[2]));near,normal,index,distance=tree.find_nearest(point)
        if index is not None and (distance<=radius or (point-near).dot(normal)<0):voxels[(x,y,z)]=colors[index]
    positions=[];indices=[];paint=[]
    # Outward Blender faces; swapping Y/Z reverses handedness, so reverse native triangles.
    faces=[((-1,0,0),[(0,0,0),(0,0,1),(0,1,1),(0,1,0)]),((1,0,0),[(1,0,0),(1,1,0),(1,1,1),(1,0,1)]),
           ((0,-1,0),[(0,0,0),(1,0,0),(1,0,1),(0,0,1)]),((0,1,0),[(0,1,0),(0,1,1),(1,1,1),(1,1,0)]),
           ((0,0,-1),[(0,0,0),(0,1,0),(1,1,0),(1,0,0)]),((0,0,1),[(0,0,1),(1,0,1),(1,1,1),(0,1,1)])]
    for (x,y,z),color in sorted(voxels.items()):
      for direction,corners in faces:
       if (x+direction[0],y+direction[1],z+direction[2]) in voxels:continue
       start=len(paint)
       for ox,oy,oz in corners:
        px=lo.x+(x+ox)*steps[0];py=lo.y+(y+oy)*steps[1];pz=lo.z+(z+oz)*steps[2]
        positions.extend([round(px,6),round(pz,6),round(py,6)]);paint.append(color)
       indices.extend([start,start+2,start+1,start,start+3,start+2])
    if not positions or len(paint)>65535:raise ValueError(('mesh budget',len(paint)))
    return {'positions':positions,'triangles':indices,'paletteIndices':paint,'voxels':len(voxels),'sourceBoundsMin':list(lo),'sourceBoundsMax':list(hi),'pitch':PITCH}

output=[]
for i,row in enumerate(rows):
    wipe();imported=bool(row.get('source'));mid=row.get('id') or 'spread-portable-'+row['blueprint'].lower()
    MAP['water']=PAL.index(row['color']) if row.get('color') else 0
    if imported:
        path=ROOT/row['source'];digest=hashlib.sha256(path.read_bytes()).hexdigest()
        if digest!=row['sourceSha256']:raise ValueError('borrowed source changed')
        bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False);bpy.context.view_layer.update();objects=list(bpy.context.selected_objects)
    else:
        model(mid,'item');forms.build(row,globals());objects=list(CURRENT.objects);export_collection(mid)
        path=OUT/'models'/f'{mid}.fbx';digest=hashlib.sha256(path.read_bytes()).hexdigest()
    vertices,triangles,colors=collect(objects,imported)
    result=bake(vertices,triangles,colors);result.update({'id':mid,'blueprint':row['blueprint'],'form':row['form'],'source':row.get('source') or 'models/'+mid+'.fbx','sourceSha256':digest,'borrowed':imported,'sourceTriangles':len(triangles)})
    output.append(result);print('BAKED',i+1,len(rows),row['blueprint'],len(result['paletteIndices']),flush=True)
(OUT/('variant-source.json' if args.variants_only else 'portable-source.json')).write_text(json.dumps({'schemaVersion':1,'coordinateContract':'Unity X east,Y height,Z north;1unit per native cell','palette':PAL,'paletteSource':'ArtSource/ReferenceGlade3D/build_kit.py PALETTE exact first24','models':output},separators=(',',':'))+'\n')
(OUT/('variant-summary.json' if args.variants_only else 'source-summary.json')).write_text(json.dumps({'models':len(output),'borrowedModels':sum(x['borrowed'] for x in output),'vertices':sum(len(x['paletteIndices']) for x in output),'triangles':sum(len(x['triangles'])//3 for x in output),'maxVertices':max(len(x['paletteIndices']) for x in output),'paletteCells':len(PAL),'pitch':PITCH},indent=2)+'\n')
print('DONE',len(output),flush=True)
