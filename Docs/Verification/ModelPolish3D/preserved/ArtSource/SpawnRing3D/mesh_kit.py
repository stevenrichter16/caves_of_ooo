# Shared import-safe source functions, extracted from the accepted village kit.
# No source scene is executed. Build script supplies bpy/material/output context.
COLORS=[
 ('earth',(0.34,0.37,0.17)),('moss',(0.385,0.415,0.18)),('moss_light',(0.48,0.50,0.21)),('moss_dark',(0.22,0.29,0.10)),
 ('stone',(0.56,0.555,0.44)),('stone_light',(0.68,0.655,0.51)),('stone_dark',(0.42,0.45,0.38)),('stone_warm',(0.62,0.58,0.44)),
 ('wood',(0.37,0.25,0.15)),('wood_light',(0.54,0.38,0.23)),('wood_dark',(0.23,0.16,0.10)),('wood_end',(0.62,0.47,0.29)),
 ('iron',(0.26,0.29,0.27)),('iron_light',(0.45,0.48,0.43)),('rope',(0.68,0.56,0.32)),('soil',(0.24,0.20,0.11)),
 ('leaf',(0.29,0.40,0.09)),('leaf_light',(0.44,0.52,0.13)),('leaf_dark',(0.205,0.29,0.065)),('cream',(0.84,0.80,0.64)),
 ('red',(0.64,0.20,0.13)),('gold',(0.78,0.57,0.20)),('teal',(0.25,0.47,0.43)),('violet',(0.48,0.32,0.48)),
 ('water',(0.09,0.21,0.23)),('water_light',(0.20,0.38,0.37)),('flower_white',(0.87,0.84,0.65)),('flower_pink',(0.76,0.31,0.46)),
 ('hood_teal',(0.14,0.44,0.44)),('hood_gold',(0.68,0.48,0.17)),('hood_violet',(0.43,0.32,0.49)),('hood_olive',(0.39,0.43,0.24)),
 ('skin',(0.69,0.57,0.40)),('face_shadow',(0.12,0.14,0.12)),('leather',(0.30,0.22,0.14)),('steel',(0.61,0.64,0.57)),
 ('cobble',(0.57,0.535,0.40)),('cobble_light',(0.69,0.635,0.49)),('cobble_dark',(0.43,0.435,0.33)),('tile_earth',(0.43,0.37,0.24)),
 ('roof_moss',(0.43,0.44,0.185)),('roof_moss_light',(0.51,0.51,0.235)),('roof_moss_dark',(0.405,0.425,0.185)),('roof_stone',(0.50,0.50,0.31)),
 ('flower_yellow',(0.79,0.67,0.28)),('cabbage',(0.36,0.54,0.17)),('cabbage_light',(0.54,0.66,0.27)),('bread',(0.74,0.55,0.28)),
 ('cloth_shadow',(0.26,0.36,0.31)),('cloth_light',(0.79,0.66,0.33)),('paper',(0.79,0.74,0.57)),('terracotta',(0.53,0.32,0.20)),
 ('book_blue',(0.28,0.40,0.43)),('book_red',(0.48,0.23,0.21)),('book_green',(0.33,0.39,0.20)),('ember',(0.72,0.28,0.08)),
 ('axis_x',(0.85,0.12,0.08)),('axis_y',(0.12,0.72,0.20)),('axis_z',(0.12,0.29,0.85)),('black',(0.06,0.07,0.05)),
 ('grass',(0.43,0.47,0.20)),('grass_light',(0.59,0.59,0.28)),('pebble',(0.55,0.54,0.40)),('rim',(0.75,0.71,0.55))]
BONE_DEFS=[('Root',(0,0,0),(0,0,.25),None),('Spine',(0,0,.42),(0,0,1.1),'Root'),
 ('Head',(0,0,1.1),(0,0,1.65),'Spine'),('Arm.L',(-.31,0,.98),(-.45,0,.55),'Spine'),
 ('Arm.R',(.31,0,.98),(.45,0,.55),'Spine'),('Leg.L',(-.16,0,.50),(-.16,0,.08),'Root'),
 ('Leg.R',(.16,0,.50),(.16,0,.08),'Root'),('Hand.L',(-.45,0,.55),(-.45,0,.42),'Arm.L'),
 ('Hand.R',(.45,0,.55),(.45,0,.42),'Arm.R')]

def color_mesh(mesh,color):
 mesh.update()
 idx=CINDEX[color];uv=mesh.uv_layers.new(name='UVMap') if not mesh.uv_layers else mesh.uv_layers[0]
 mins=[min(v.co[i] for v in mesh.vertices) for i in range(3)];sizes=[max(v.co[i] for v in mesh.vertices)-mins[i] for i in range(3)]
 for poly in mesh.polygons:
  dominant=max(range(3),key=lambda i:abs(poly.normal[i]));axes=[i for i in range(3) if i!=dominant]
  for li in poly.loop_indices:
   co=mesh.vertices[mesh.loops[li].vertex_index].co
   a=(co[axes[0]]-mins[axes[0]])/max(sizes[axes[0]],.0001);b=(co[axes[1]]-mins[axes[1]])/max(sizes[axes[1]],.0001)
   uv.data[li].uv=((idx%16+.025+.95*a)/16,(idx//16+.025+.95*b)/8)
 attr=mesh.color_attributes.new(name='SpawnRingColor',type='BYTE_COLOR',domain='CORNER') if not mesh.color_attributes else mesh.color_attributes[0]
 for q in attr.data:q.color=(*COLORS[idx][1],1)
 mesh.materials.append(MAT)

def newmesh(name,verts,faces,color,smooth=False):
 mesh=bpy.data.meshes.new(name+'Mesh');mesh.from_pydata(verts,[],faces);mesh.update();color_mesh(mesh,color)
 ob=bpy.data.objects.new(name,mesh);CURRENT.objects.link(ob)
 for p in mesh.polygons:p.use_smooth=smooth
 return ob

def box(name,loc,size,color,bevel=.07,rot=0):
 bm=bmesh.new();bmesh.ops.create_cube(bm,size=1)
 for v in bm.verts:v.co.x*=size[0];v.co.y*=size[1];v.co.z*=size[2]
 if bevel>0:bmesh.ops.bevel(bm,geom=list(bm.edges),offset=min(bevel,min(size)*.3),segments=2,affect='EDGES')
 mesh=bpy.data.meshes.new(name+'Mesh');bm.to_mesh(mesh);bm.free();color_mesh(mesh,color)
 ob=bpy.data.objects.new(name,mesh);CURRENT.objects.link(ob);ob.location=loc;ob.rotation_euler[2]=rot
 # Broad faces remain flat, rounded bevel faces smooth.
 for p in mesh.polygons:p.use_smooth=len(p.vertices)==4 and p.area < min(size)*max(size)*.25
 return ob

def ellipsoid(name,loc,scale,color,sub=1):
 bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=sub,radius=1)
 for v in bm.verts:v.co.x*=scale[0];v.co.y*=scale[1];v.co.z*=scale[2]
 mesh=bpy.data.meshes.new(name+'Mesh');bm.to_mesh(mesh);bm.free();color_mesh(mesh,color)
 ob=bpy.data.objects.new(name,mesh);CURRENT.objects.link(ob);ob.location=loc
 for p in mesh.polygons:p.use_smooth=True
 return ob

def softstone(name,loc,size,color,rng,rot=0):
 # Low, softly bevelled irregular masonry: flattened tops catch light while
 # unequal rounded corners keep the overhead silhouette from looking tiled.
 n=8;outline=[]
 for i in range(n):
  a=(i+.5)*math.tau/n
  outline.append((math.copysign(abs(math.cos(a))**.6,math.cos(a))*rng.uniform(.92,1.04),math.copysign(abs(math.sin(a))**.6,math.sin(a))*rng.uniform(.92,1.04)))
 verts=[]
 for z,r in [(-.5,.72),(-.19,1),(.19,1),(.5,.76)]:
  verts.extend((x*size[0]*.5*r,y*size[1]*.5*r,z*size[2]) for x,y in outline)
 faces=[tuple(reversed(range(n))),tuple(range(3*n,4*n))]
 for j in range(3):
  for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 ob=newmesh(name,verts,faces,color,True);ob.location=loc;ob.rotation_euler[2]=rot
 ob.data.polygons[0].use_smooth=False;ob.data.polygons[1].use_smooth=False
 return ob

def fittedstone(name,loc,outline,height,color,flat_top=False):
 n=len(outline);vs=[]
 for z,r in [(-.25,.79),(.07,1),(.56,.69)]:
  vs.extend((x*r,y*r,z*height) for x,y in outline)
 fs=[tuple(reversed(range(n))),tuple(range(n*2,n*3))]
 for ring in range(2):
  for i in range(n):fs.append((ring*n+i,ring*n+(i+1)%n,(ring+1)*n+(i+1)%n,(ring+1)*n+i))
 ob=newmesh(name,vs,fs,color,True);ob.location=loc
 ob.data.polygons[0].use_smooth=False
 if flat_top:ob.data.polygons[1].use_smooth=False
 return ob

def leafshape(name,loc,length,width,angle,color,tilt=.045):
 # Ten smooth facets around a gently rounded leaf; closed volume catches light
 # and avoids black razor-thin card interiors from the overhead camera.
 n=5;vs=[]
 for i in range(n):
  a=i*math.tau/n;vs.append((math.cos(a)*length*.53,math.sin(a)*width*.52,0))
 vs.extend([(0,0,tilt),(0,0,-tilt*.48)])
 fs=[(n,i,(i+1)%n) for i in range(n)]+[(n+1,(i+1)%n,i) for i in range(n)]
 ob=newmesh(name,vs,fs,color,True);ob.location=loc;ob.rotation_euler[2]=angle
 return ob

def cylinder(name,loc,radius,depth,color,vertices=12,radius2=None):
 r2=radius if radius2 is None else radius2
 if radius==0 and r2==0:raise ValueError('A cylinder needs a nonzero endpoint radius')
 if radius==0 or r2==0:
  # A true cone has one apex. A collapsed ring produces duplicate vertices,
  # a zero-area cap and degenerate quads that real Unity imports discard.
  top_apex=r2==0;r=radius if top_apex else r2;ringz=-depth/2 if top_apex else depth/2;apexz=-ringz
  vs=[(r*math.cos(i*math.tau/vertices),r*math.sin(i*math.tau/vertices),ringz) for i in range(vertices)]+[(0,0,apexz)]
  fs=[tuple(reversed(range(vertices))) if top_apex else tuple(range(vertices))]
  for i in range(vertices):
   j=(i+1)%vertices;fs.append((i,j,vertices) if top_apex else (j,i,vertices))
  capcount=1
 else:
  vs=[]
  for z,r in [(-depth/2,radius),(depth/2,r2)]:
   for i in range(vertices):a=i*math.tau/vertices;vs.append((r*math.cos(a),r*math.sin(a),z))
  fs=[tuple(reversed(range(vertices))),tuple(range(vertices,vertices*2))]
  for i in range(vertices):j=(i+1)%vertices;fs.append((i,j,j+vertices,i+vertices))
  capcount=2
 ob=newmesh(name,vs,fs,color);ob.location=loc
 for p in ob.data.polygons:p.use_smooth=p.index>=capcount
 return ob

def beam(name,a,b,r,color,vertices=8):
 a=Vector(a);b=Vector(b);ob=cylinder(name,(a+b)*.5,r,(b-a).length,color,vertices)
 ob.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return ob

def torus(name,loc,r,minor,color,major_segments=16,minor_segments=5):
 vs=[];fs=[]
 for i in range(major_segments):
  a=math.tau*i/major_segments
  for j in range(minor_segments):
   b=math.tau*j/minor_segments;rr=r+minor*math.cos(b);vs.append((rr*math.cos(a),rr*math.sin(a),minor*math.sin(b)))
 for i in range(major_segments):
  for j in range(minor_segments):fs.append((i*minor_segments+j,((i+1)%major_segments)*minor_segments+j,((i+1)%major_segments)*minor_segments+(j+1)%minor_segments,i*minor_segments+(j+1)%minor_segments))
 ob=newmesh(name,vs,fs,color,True);ob.location=loc;return ob

def model(mid,kind='prop',pivot='ground-centre'):
 global CURRENT
 CURRENT=bpy.data.collections.new(mid);MODELS[mid]={'id':mid,'collection':CURRENT,'kind':kind,'pivot':pivot};return CURRENT

def instance(mid,loc=(0,0,0),angle=0,scale=(1,1,1),owner=None,role='static'):
 ob=bpy.data.objects.new(owner or mid+'_instance',None);ob.instance_type='COLLECTION';ob.instance_collection=MODELS[mid]['collection'];SCENE.collection.objects.link(ob)
 ob.location=loc;ob.rotation_euler[2]=angle;ob.scale=scale
 ob['modelId']=mid;ob['ownerId']=owner or '';ob['role']=role
 return ob

def export_collection(mid):
 info=MODELS[mid];coll=info['collection'];temps=[];remap={}
 root=bpy.data.objects.new(mid,None);SCENE.collection.objects.link(root);temps.append(root)
 # Unity FBX importer reverses both horizontal source axes. The export-only
 # parent rotates them once; authoring geometry and manifest stay east/north.
 root.rotation_euler[2]=math.pi
 if not info.get('rigged'):
  # Direct assembly avoids Blender's O(piece-count) context/depsgraph work for
  # each join operation. This preserves every source vertex, face, UV and tint.
  for water in [False,True]:
   group=[o for o in coll.objects if o.type=='MESH' and bool(o.get('keepSeparate',False))==water]
   if not group:continue
   verts=[];faces=[];uvs=[];colors=[];smooth=[]
   for ob in group:
    mesh=ob.data;offset=len(verts);mat=ob.matrix_basis
    verts.extend(tuple(mat@v.co) for v in mesh.vertices)
    faces.extend(tuple(offset+i for i in f.vertices) for f in mesh.polygons)
    smooth.extend(f.use_smooth for f in mesh.polygons)
    uvs.extend(tuple(t.uv) for t in mesh.uv_layers[0].data)
    colors.extend(tuple(t.color) for t in mesh.color_attributes[0].data)
   name=mid+('__Water' if water else '__Geometry');mesh=bpy.data.meshes.new(name+'Mesh');mesh.from_pydata(verts,[],faces);mesh.update()
   mesh.materials.append(WATER if water else MAT);layer=mesh.uv_layers.new(name='UVMap');layer.data.foreach_set('uv',[v for uv in uvs for v in uv])
   attr=mesh.color_attributes.new(name='SpawnRingColor',type='BYTE_COLOR',domain='CORNER');attr.data.foreach_set('color',[v for color in colors for v in color])
   for f,value in zip(mesh.polygons,smooth):f.use_smooth=value
   ob=bpy.data.objects.new(name,mesh);SCENE.collection.objects.link(ob);ob.parent=root;temps.append(ob)
  for ob in bpy.context.selected_objects:ob.select_set(False)
  for ob in temps:ob.select_set(True)
  bpy.context.view_layer.objects.active=root
  bpy.ops.export_scene.fbx(filepath=str(OUT/'models'/f'{mid}.fbx'),use_selection=True,object_types={'EMPTY','MESH'},
   axis_forward='Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',global_scale=1.0,
   bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=False,add_leaf_bones=False,
   bake_anim=False,path_mode='RELATIVE',use_custom_props=True)
  for ob in temps:bpy.data.objects.remove(ob,do_unlink=True)
  return
 for source in coll.objects:
  ob=source.copy()
  if source.data:ob.data=source.data.copy() if source.type in ('MESH','ARMATURE') else source.data
  SCENE.collection.objects.link(ob);temps.append(ob);remap[source]=ob
  if ob.type=='ARMATURE':ob.name=mid+'__Rig'
  if ob.get('socketName'):ob.name=ob['socketName']
 for source,ob in remap.items():
  ob.parent=remap.get(source.parent,root)
  ob.matrix_parent_inverse=source.matrix_parent_inverse.copy() if source.parent else Matrix.Identity(4)
  for mod in ob.modifiers:
   if mod.type=='ARMATURE':mod.object=remap.get(mod.object,mod.object)
 rigged=bool(info.get('rigged'))
 if rigged:
  rig=next(o for o in temps if o.type=='ARMATURE')
  for track in rig.animation_data.nla_tracks:track.mute=False
 for water in [False,True]:
  group=[o for o in temps if o.type=='MESH' and bool(o.get('keepSeparate',False))==water]
  if not group:continue
  for ob in bpy.context.selected_objects:ob.select_set(False)
  for ob in group:ob.select_set(True)
  bpy.context.view_layer.objects.active=group[0]
  if len(group)>1:bpy.ops.object.join()
  joined=group[0];joined.name=mid+('__Water' if water else '__Geometry')
  # Joined duplicate material slots are canonicalized by Blender, preserving the
  # palette atlas; only one palette slot is needed per joined geometry.
  if len(joined.data.materials)>1 and all(m==joined.data.materials[0] for m in joined.data.materials):
   mat=joined.data.materials[0];joined.data.materials.clear();joined.data.materials.append(mat)
   for p in joined.data.polygons:p.material_index=0
  temps=[o for o in temps if o==joined or o not in group]
 for ob in bpy.context.selected_objects:ob.select_set(False)
 for ob in temps:ob.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(OUT/'models'/f'{mid}.fbx'),use_selection=True,object_types={'EMPTY','MESH','ARMATURE'},
  axis_forward='Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',global_scale=1.0,
  bake_space_transform=not rigged,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=False,add_leaf_bones=False,
  bake_anim=rigged,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=rigged,bake_anim_use_all_actions=False,
  bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,bake_anim_step=1,path_mode='RELATIVE',use_custom_props=True)
 # Object joining invalidates references to removed children; the filtered list
 # above contains only living objects after each join.
 for ob in temps:
  if ob.name in bpy.data.objects:bpy.data.objects.remove(ob,do_unlink=True)

def barrel(variant=0,sideways=False):
 rng=random.Random(810+variant);count=12;r=.42;h=.92
 for i in range(count):
  a=math.tau*i/count;vs=[]
  for z,rr in [(0,r*.82),(.15,r),(.46,r*1.06),(.78,r),(.92,r*.82)]:
   for da in [-.47,.47]: t=a+da*math.tau/count;vs.append((rr*math.cos(t),rr*math.sin(t),z))
  fs=[(j*2,j*2+1,j*2+3,j*2+2) for j in range(4)]
  ob=newmesh('Oak_stave_%02d'%i,vs,fs,['wood','wood_light','wood_dark'][(i+variant)%3],True)
 for z,rr in [(.13,r),(.72,r*1.01)]:torus('Iron_hoop',(0,0,z),rr,.037,'iron',12,4)
 cylinder('Barrel_lid',(0,0,h-.025),r*.8,.04,'wood_end',12)
 for x in [-.17,0,.17]:box('Lid_seam',(x,0,h),(.013,.59,.006),'wood_dark',0)
 if variant==1:torus('Rope_coil',(0,0,h+.045),.22,.04,'rope',12,4)
 if variant==2:box('Lid_brace',(0,0,h+.035),(.08,.58,.04),'wood_light',.012,rot=.55)
 if variant==3:cylinder('Iron_bung',(.15,0,h+.02),.065,.035,'iron',8)
 if sideways:
  for ob in CURRENT.objects:ob.matrix_world=Matrix.Translation((0,0,.48))@Matrix.Rotation(math.pi/2,4,'X')@Matrix.Translation((0,0,-h/2))@ob.matrix_world

def crate(variant=0):
 for z in [.1,.32,.54]:
  for y in [-.37,.37]:box('Side_plank',(0,y,z),(.82,.09,.19),'wood_light' if variant%2 else 'wood',.025)
  for x in [-.37,.37]:box('End_plank',(x,0,z),(.09,.66,.19),'wood',.025)
 for x in [-.37,.37]:
  for y in [-.37,.37]:box('Corner',(x,y,.35),(.105,.105,.75),'wood_dark',.026)
 box('Crate_floor',(0,0,.06),(.7,.7,.09),'wood_dark',.02)
 if variant%2==0:
  for i in range(4):box('Lid_plank',(-.3+i*.2,0,.69),(.185,.82,.09),'wood_light',.014)
  beam('Cross_brace',(-.32,-.32,.76),(.32,.32,.76),.04,'wood_dark',5)
 else:
  for i in range(6):beam('Stored_kindling',((i%3)*.15-.15,(i//3)*.2-.1,.1),((i%3)*.13-.1,(i//3)*.19-.1,.78+(i%2)*.14),.045,'wood_end',5)

def bucket(variant=0):
 cylinder('Bucket_body',(0,0,.27),.24,.5,'wood_light',12,radius2=.32)
 cylinder('Bucket_inside',(0,0,.528),.265,.012,'water' if variant%2 else 'wood_dark',16)
 torus('Upper_band',(0,0,.49),.315,.025,'iron',16,4);torus('Lower_band',(0,0,.07),.25,.024,'iron',12,4)
 for s in [-1,1]:beam('Handle_post',(s*.28,0,.43),(s*.27,0,.83),.025,'iron',6)
 beam('Handle_grip',(-.27,0,.83),(.27,0,.83),.035,'wood_end',8)

def roundpot(variant=0):
 cylinder('Pot',(0,0,.23),.26,.43,'terracotta',12,radius2=.35);torus('Pot_rim',(0,0,.45),.33,.045,'wood_end',16,5)
 cylinder('Pot_soil',(0,0,.45),.285,.014,'soil',12)
 for i in range(5):
  a=i*math.tau/5;ellipsoid('Herb_leaf',(.15*math.cos(a),.15*math.sin(a),.61),(.13,.11,.18),'leaf_light' if i%2 else 'leaf',1)
  if i%2==0:ellipsoid('Bloom',(.16*math.cos(a),.16*math.sin(a),.78),(.08,.08,.07),'flower_pink' if variant%2 else 'flower_white',1)

def rope_coil():
 for r in [.14,.22,.30]:torus('Woven_rope',(0,0,.055),r,.035,'rope',18,5)
 beam('Loose_end',(.32,0,.05),(.49,.22,.05),.03,'rope',6)

def stool():
 cylinder('Stool_seat',(0,0,.51),.27,.1,'wood_light',12)
 for a in [0,math.tau/3,2*math.tau/3]:beam('Stool_leg',(.22*math.cos(a),.22*math.sin(a),.04),(.18*math.cos(a),.18*math.sin(a),.5),.05,'wood',7)

def bench():
 for x in [-.58,.58]:box('Bench_support',(x,0,.26),(.16,.4,.52),'wood',.025)
 for y in [-.10,.10]:box('Bench_seat',(0,y,.56),(1.5,.18,.12),'wood_light',.025)
 box('Bench_back',(0,.23,.85),(1.5,.10,.32),'wood',.04)

def hearth(variant=0):
 for x in [-.48,.48]:box('Hearth_cheek',(x,0,.42),(.28,.70,.8),'stone_dark',.1)
 box('Hearth_back',(0,.28,.45),(.85,.25,.85),'stone',.1)
 box('Hearth_arch',(0,-.03,.90),(1.23,.82,.27),'stone_light',.14)
 box('Fire_bed',(0,-.13,.1),(.72,.63,.14),'face_shadow',.04)
 for i in range(3):beam('Charred_log',(-.28,-.22+i*.16,.2),(.27,-.10+i*.11,.2),.065,'wood_dark',7)
 for x in [-.17,.04,.21]:ellipsoid('Glowing_coal',(x,-.14,.22),(.10,.12,.045),'ember',1)
 if variant: cylinder('Cooking_pot',(0,0,1.09),.32,.27,'iron',16,radius2=.38);torus('Pot_lip',(0,0,1.23),.38,.035,'iron_light')

def rockcluster(variant=0):
 rng=random.Random(1500+variant)
 for i in range(3+(variant%2)):
  a=i*2.4;r=.33*math.sqrt(i);sz=rng.uniform(.22,.46)
  ob=ellipsoid('River_rock_%d'%i,(r*math.cos(a),r*math.sin(a),sz*.42),(sz,sz*.84,sz*.69),['stone','stone_dark','stone_light'][(i+variant)%3],1);ob.rotation_euler[2]=a
  if i%2==0:ellipsoid('Rock_moss',(.08+r*math.cos(a),r*math.sin(a),sz*.91),(.19,.15,.037),'moss',1)

def character(color,mid):
 coll=model(mid,'character','feet-root');parts={}
 def part(ob,bone):parts[ob.name]=bone;return ob
 part(cylinder('Coat',(0,0,.69),.39,.83,color,16,radius2=.28),'Spine')
 part(torus('Coat_hem',(0,0,.285),.39,.035,'leather',16,4),'Spine')
 part(torus('Leather_belt',(0,0,.69),.35,.035,'leather',16,4),'Spine')
 part(box('Belt_buckle',(0,-.35,.70),(.10,.05,.10),'gold',.014),'Spine')
 part(ellipsoid('Hood',(0,0,1.32),(.38,.34,.39),color,3),'Head')
 part(ellipsoid('Hood_point',(0,.21,1.55),(.17,.24,.14),color,1),'Head')
 part(ellipsoid('Hood_dark_opening',(0,-.291,1.31),(.225,.045,.23),'face_shadow',2),'Head')
 part(ellipsoid('Face',(0,-.32,1.25),(.135,.035,.12),'skin',1),'Head')
 for s,label in [(-1,'L'),(1,'R')]:
  part(beam('Sleeve.'+label,(s*.25,0,1.02),(s*.44,0,.58),.14,color,10),'Arm.'+label)
  part(ellipsoid('Hand.'+label,(s*.46,0,.51),(.115,.12,.13),'skin',1),'Hand.'+label)
  part(cylinder('Trouser.'+label,(s*.17,0,.27),.13,.30,'leather',10),'Leg.'+label)
  part(ellipsoid('Boot.'+label,(s*.17,-.075,.09),(.14,.22,.11),'wood_dark',1),'Leg.'+label)
 arm=bpy.data.armatures.new(mid+'_Skeleton');rig=bpy.data.objects.new('SpawnRingRig',arm);SCENE.collection.objects.link(rig)
 bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for name,head,tail,parent in BONE_DEFS:
  b=arm.edit_bones.new(name);b.head=head;b.tail=tail
  if parent:b.parent=arm.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
 for collection in list(rig.users_collection):collection.objects.unlink(rig)
 coll.objects.link(rig)
 for ob in list(coll.objects):
  if ob.type!='MESH':continue
  group=ob.vertex_groups.new(name=parts[ob.name]);group.add(list(range(len(ob.data.vertices))),1,'REPLACE')
  mod=ob.modifiers.new('SpawnRingRig','ARMATURE');mod.object=rig;ob.parent=rig
 for name,bone,loc in [('Equipment.Head','Head',(0,0,1.68)),('Equipment.Hand.L','Hand.L',(-.46,-.02,.46)),('Equipment.Hand.R','Hand.R',(.46,-.02,.46)),('Equipment.Back','Spine',(0,.27,.95))]:
  sock=bpy.data.objects.new(mid+'__'+name,None);sock['socketName']=name;coll.objects.link(sock);sock.parent=rig;sock.parent_type='BONE';sock.parent_bone=bone
  # Bone parenting translates from bone tail, so world bind-space matrix is explicit.
  sock.matrix_parent_inverse=(rig.matrix_world @ rig.pose.bones[bone].matrix @ Matrix.Translation((0,rig.data.bones[bone].length,0))).inverted();sock.matrix_basis=Matrix.Translation(loc);sock['socketBone']=bone
 rig.animation_data_create()
 for clip in ['Idle','Walk','Interact','Attack','Hit']:
  for p in rig.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0);p.location=(0,0,0)
  action=bpy.data.actions.new(mid+'__'+clip);rig.animation_data.action=action
  length=48 if clip=='Idle' else 24
  for frame in [0,length//4,length//2,3*length//4,length]:
   phase=frame/length*math.tau
   for p in rig.pose.bones:p.rotation_euler=(0,0,0);p.location=(0,0,0)
   if clip=='Idle':rig.pose.bones['Head'].rotation_euler[1]=.025*math.sin(phase);rig.pose.bones['Spine'].rotation_euler[0]=.025*math.sin(phase)
   elif clip=='Walk':
    for side,sign in [('L',1),('R',-1)]:rig.pose.bones['Leg.'+side].rotation_euler[0]=.36*sign*math.sin(phase);rig.pose.bones['Arm.'+side].rotation_euler[0]=-.28*sign*math.sin(phase)
   elif clip=='Interact':rig.pose.bones['Spine'].rotation_euler[0]=-.16*math.sin(frame/length*math.pi);rig.pose.bones['Arm.R'].rotation_euler[0]=-.65*math.sin(frame/length*math.pi)
   elif clip=='Attack':rig.pose.bones['Arm.R'].rotation_euler[0]=-1.15*math.sin(frame/length*math.pi);rig.pose.bones['Spine'].rotation_euler[2]=.23*math.sin(phase)
   elif clip=='Hit':rig.pose.bones['Spine'].rotation_euler[0]=.24*math.sin(frame/length*math.pi);rig.pose.bones['Head'].rotation_euler[0]=-.14*math.sin(frame/length*math.pi)
   for p in rig.pose.bones:p.keyframe_insert(data_path='rotation_euler',frame=frame,group=p.name)
  track=rig.animation_data.nla_tracks.new();track.name=clip;strip=track.strips.new(clip,0,action);strip.name=clip;track.mute=True
 rig.animation_data.action=None
 for p in rig.pose.bones:p.rotation_euler=(0,0,0);p.location=(0,0,0)
 MODELS[mid]['rigged']=True;MODELS[mid]['clips']=['Idle','Walk','Interact','Attack','Hit'];MODELS[mid]['sockets']=['Equipment.Head','Equipment.Hand.L','Equipment.Hand.R','Equipment.Back']
