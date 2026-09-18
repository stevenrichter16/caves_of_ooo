#!/usr/bin/env python3
"""Editable Living Woodcut multi-cell meshes, FBX bundle and overhead scene.
Uses verified SpawnRing3D construction/export helpers; never executes its builder.
"""
import bpy,bmesh,math,random,json,argparse,sys,hashlib,ast,shutil
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.geometry import delaunay_2d_cdt
import numpy as np
ROOT=Path(__file__).resolve().parent;SHARED=ROOT.parent/'SpawnRing3D'
ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path,default=ROOT);ap.add_argument('--render',action='store_true');ap.add_argument('--skip-export',action='store_true');ap.add_argument('--samples',type=int,default=24);ap.add_argument('--ground-albedo',type=Path,default=ROOT/'textures/PilotGroundAlbedo-imagegen-v1.png');args=ap.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
OUT=args.output.resolve();OUT.mkdir(parents=True,exist_ok=True)
for d in ('models','textures','reports','renders'):(OUT/d).mkdir(exist_ok=True)
CONTRACT=json.loads((ROOT/'model-contract.json').read_text());LAYOUT=json.loads((ROOT/'layout.json').read_text());MODELS={};CURRENT=None
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
 if c.name!='Collection':bpy.data.collections.remove(c)
SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1;SCENE.render.threads_mode='FIXED';SCENE.render.threads=2;SCENE.render.fps=24
exec(compile((SHARED/'mesh_kit.py').read_text(),str(SHARED/'mesh_kit.py'),'exec'),globals())
source=ast.parse((SHARED/'build_ring.py').read_text())
for node in source.body:
 if isinstance(node,ast.AugAssign) and isinstance(node.target,ast.Name) and node.target.id=='COLORS':COLORS+=ast.literal_eval(node.value)
selected=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name in ('stem','rig_parts','creature')]
exec(compile(ast.Module(body=selected,type_ignores=[]),str(SHARED/'build_ring.py'),'exec'),globals())
# Pilot palette is isolated: existing town/ring colors never change.
recolors={'tepui0':(.56,.365,.285),'tepui1':(.56,.365,.285),'tepui2':(.56,.365,.285),'tepui3':(.56,.365,.285),'pinkstone':(.72,.50,.435),'pinklight':(.81,.61,.48),'pinkdark':(.385,.28,.29),'grain':(.815,.63,.435),'root_bark':(.645,.415,.355),'stone':(.47,.39,.315),'stone_light':(.655,.57,.43),'stone_dark':(.335,.31,.25),'ochre':(.55,.385,.105),'leaf_olive':(.37,.32,.095),'leaf_sun':(.68,.51,.135),'bone':(.80,.73,.535),'bone_shadow':(.535,.48,.335),'tar':(.033,.045,.046),'water':(.034,.055,.064),'water_light':(.085,.135,.15),'copper':(.54,.265,.095),'frog_dark':(.43,.33,.12),'frog_light':(.67,.54,.21)}
COLORS=[('root_bark',recolors['root_bark']) if n=='axis_x' else (n,recolors.get(n,c)) for n,c in COLORS];CINDEX={n:i for i,(n,_) in enumerate(COLORS)}
W,H,T=2048,1024,128;pixels=np.ones((H,W,4),np.float32);u,v=np.meshgrid((np.arange(T)+.5)/T,(np.arange(T)+.5)/T)
for idx,(name,col) in enumerate(COLORS):
 rng=np.random.default_rng(91337+idx);noise=rng.random((T,T))-.5
 broad=np.sin(u*math.tau*1.3+np.sin(v*math.tau*.7))*np.sin(v*math.tau*.9+.4)
 value=1+noise*.025+broad*.035
 if name.startswith('tepui') or name in ('pinkstone','pinklight','pinkdark','root_bark','grain'):
  phase=int(name[-1])*.71 if name.startswith('tepui') else idx*.31
  warped=v+.055*np.sin(u*math.tau+phase)+.02*np.sin(u*math.tau*3+phase)
  ridge=np.sin(warped*math.tau*(4.0 if name.startswith('tepui') else 5.0)+phase)
  cuts=np.exp(-((ridge+.89)/.10)**2)
  value-=cuts*(.115 if name.startswith('tepui') else .085)
  value+=np.exp(-((ridge+.49)/.13)**2)*.055
  value+=np.sin(warped*math.tau*1.4+phase)*.055
  # Fine grain stays matte and portable; no baked cast shadows.
 tile=np.clip(np.asarray(col)*value[:,:,None],0,1)
 pixels[(idx//16)*T:(idx//16+1)*T,(idx%16)*T:(idx%16+1)*T,:3]=tile
ATLAS=bpy.data.images.new('PilotPalette',width=W,height=H,alpha=False);ATLAS.pixels.foreach_set(pixels.ravel());ATLAS.filepath_raw=str(OUT/'textures/PilotPalette.png');ATLAS.file_format='PNG';ATLAS.save()
MAT=bpy.data.materials.new('PilotPalette');MAT.use_nodes=True;bs=MAT.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.9
tex=MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.image=ATLAS;MAT.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
WATER=bpy.data.materials.new('PilotTar');WATER.use_nodes=True;bs=WATER.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.012,.024,.029,1);bs.inputs['Roughness'].default_value=.24;bs.inputs['Metallic'].default_value=.08

# Continuous procedural base albedo, authored from mathematical mineral grain.
# No obstacles, silhouettes, lighting, baked cast shadows or reference pixels.
GW,GH=2560,800;gx,gy=np.meshgrid((np.arange(GW)+.5)/GW,(np.arange(GH)+.5)/GH)
def smooth_noise(nx,ny,seedval):
 r=np.random.default_rng(seedval);grid=r.random((ny+1,nx+1));xx=gx*nx;yy=gy*ny;ix=xx.astype(int);iy=yy.astype(int);fx=xx-ix;fy=yy-iy;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
 return ((1-fx)*grid[iy,ix]+fx*grid[iy,ix+1])*(1-fy)+((1-fx)*grid[iy+1,ix]+fx*grid[iy+1,ix+1])*fy
macro=smooth_noise(16,6,9061);broad=smooth_noise(37,11,9062);fine=smooth_noise(180,55,9063)
flow=gy+.038*np.sin(gx*math.tau*2.4+np.sin(gy*5.3))+.018*np.sin(gx*math.tau*5.1+gy*8)+.046*(macro-.5)+.007*(broad-.5)
# Local fibre deflections create forks and elongated knots while preserving
# the shared east-west structural direction. Their pigment is not a new object.
for kx,ky,rx,ry,amp in [(.14,.64,.075,.13,.42),(.38,.21,.09,.14,.44),(.57,.62,.11,.12,.40),(.75,.35,.08,.12,.44),(.9,.8,.10,.14,.40)]:
 dy=gy-ky;envelope=np.exp(-((gx-kx)/rx)**2-(dy/ry)**2);flow+=ry*amp*np.tanh(dy/ry*3.5)*envelope
# Coherent E-W direction with broad mineral forks and finer broken fibres.
wave=flow*math.tau*43;ridge=np.sin(wave+.22*np.sin(flow*math.tau*19+gx*12));crease=np.exp(-((ridge+.965)/.070)**2)*np.clip((broad-.14)*2.2,0,1)
highlight=np.exp(-((ridge+.70)/.13)**2);bands=np.sin(flow*math.tau*7+macro*.7)
base=np.array((.625,.432,.378));value=1+(macro-.5)*.14+(broad-.5)*.18+(fine-.5)*.13+bands*.065-crease*.16+highlight*.055
rgb=np.clip((base+(broad-.5)[:,:,None]*np.array((.06,.045,.008)))*value[:,:,None],0,1)
# Subtle ochre mineral dust, not painted foliage or invented native entities.
dust=np.clip((smooth_noise(75,23,9065)-.72)*.30,0,.035);rgb=rgb*(1-dust[:,:,None])+np.array((.59,.475,.265))*dust[:,:,None]
img=bpy.data.images.new('PilotGroundAlbedo',width=GW,height=GH,alpha=False);arr=np.ones((GH,GW,4),np.float32);arr[:,:,:3]=rgb;img.pixels.foreach_set(arr.ravel());img.filepath_raw=str(OUT/'textures/PilotGroundAlbedo.png');img.file_format='PNG';img.save();img.pack();img.filepath='//textures/PilotGroundAlbedo.png'
if args.ground_albedo.exists():
 # Consume the ImageGen material unchanged as a normal 3D base-color texture.
 # Source remains versioned; the stable import filename is a byte-exact copy.
 shutil.copyfile(args.ground_albedo,OUT/'textures/PilotGroundAlbedo.png')
 img=bpy.data.images.load(str(OUT/'textures/PilotGroundAlbedo.png'),check_existing=False);img.name='PilotGroundAlbedo';img.pack();img.filepath='//textures/PilotGroundAlbedo.png';GW,GH=img.size[:]
GROUND_MAT=bpy.data.materials.new('PilotGroundAlbedo');GROUND_MAT.use_nodes=True;gbs=GROUND_MAT.node_tree.nodes.get('Principled BSDF');gbs.inputs['Roughness'].default_value=.93;gt=GROUND_MAT.node_tree.nodes.new('ShaderNodeTexImage');gt.image=img;GROUND_MAT.node_tree.links.new(gt.outputs['Color'],gbs.inputs['Base Color'])
(OUT/'textures/ground-contract.json').write_text(json.dumps({'texture':'textures/PilotGroundAlbedo.png','size':[GW,GH],'sRGB':True,'alpha':False,'wrap':'Clamp','filter':'Bilinear','coordinates':'u = UnityWorldX / 80; v = UnityWorldZ / 25; world origin is southwest corner; texture is only mineral albedo','nativeCellUV':'u=(cellX+localX+0.5)/80; v=(24.5-cellY+localNorth)/25','source':'ImageGen flat mineral albedo, byte-exact input; see imagegen-ground-provenance.json' if args.ground_albedo.exists() else 'Deterministic procedural mineral albedo'},indent=2)+'\n')

def seed(s):return int(hashlib.sha256(s.encode()).hexdigest()[:8],16)
def inside(cells,x,y,eps=.0001):return any(abs(x-cx)<=.5+eps and abs(y+cy)<=.5+eps for cx,cy in cells)
def edges(cells):
 cells=set(cells);result=[]
 for x,dy in cells:
  y=-dy
  for dx,ddy,a,b in [(1,0,(x+.5,y-.5),(x+.5,y+.5)),(-1,0,(x-.5,y+.5),(x-.5,y-.5)),(0,-1,(x+.5,y+.5),(x-.5,y+.5)),(0,1,(x-.5,y-.5),(x+.5,y-.5))]:
   if (x+dx,dy+ddy) not in cells:result.append((a,b))
 return result
def edge_dist(x,y,a,b):
 p=Vector((x,y));a=Vector(a);b=Vector(b);ab=b-a;t=max(0,min(1,(p-a).dot(ab)/ab.dot(ab)));return (p-a-ab*t).length

def clip_to_cells(ob,cells):
 """Clip every offending triangle to the exact occupied-cell union.
 Vertex-only clamps are insufficient at concave corners: triangles can bridge
 a hole even when all three endpoints are legal. UV/color interpolation keeps
 the cut surface consistent, with one native owner and one exported mesh.
 """
 mesh=ob.data;mesh.calc_loop_triangles();mat=ob.matrix_basis;inv=mat.inverted();occupied=set(cells);vertices=[];faces=[];uvs=[];colors=[];smooth=[];vindex={}
 def clipped(poly,axis,bound,keep_greater):
  result=[]
  for a,b in zip(poly,poly[1:]+poly[:1]):
   av=a[0][axis]-bound;bv=b[0][axis]-bound;ain=av>=-1e-9 if keep_greater else av<=1e-9;bin=bv>=-1e-9 if keep_greater else bv<=1e-9
   if ain:result.append(a)
   if ain!=bin:
    t=av/(av-bv);point=a[0].lerp(b[0],t);point[axis]=bound;result.append((point,a[1].lerp(b[1],t),a[2].lerp(b[2],t)))
  clean=[]
  for p in result:
   if not clean or (p[0]-clean[-1][0]).length>1e-8:clean.append(p)
  if len(clean)>1 and (clean[0][0]-clean[-1][0]).length<1e-8:clean.pop()
  return clean
 def emit(poly,shade):
  for k in range(1,len(poly)-1):
   tri=[poly[0],poly[k],poly[k+1]]
   if (tri[1][0]-tri[0][0]).cross(tri[2][0]-tri[0][0]).length<1e-10:continue
   ids=[]
   for pos,uv,col in tri:
    local=inv@pos;key=tuple(round(x,7) for x in local)
    if key not in vindex:vindex[key]=len(vertices);vertices.append(tuple(local))
    ids.append(vindex[key]);uvs.append(tuple(uv));colors.append(tuple(col))
   faces.append(tuple(ids));smooth.append(shade)
 for tri in mesh.loop_triangles:
  poly=[(mat@mesh.vertices[mesh.loops[i].vertex_index].co,Vector(mesh.uv_layers[0].data[i].uv),Vector(mesh.color_attributes[0].data[i].color)) for i in tri.loops]
  minx=min(p[0].x for p in poly);maxx=max(p[0].x for p in poly);miny=min(p[0].y for p in poly);maxy=max(p[0].y for p in poly)
  covered={(x,y) for x in range(math.floor(minx+.5+1e-8),math.floor(maxx+.5-1e-8)+1) for y in range(math.floor(-maxy+.5+1e-8),math.floor(-miny+.5-1e-8)+1)}
  shade=mesh.polygons[tri.polygon_index].use_smooth
  if covered and covered<=occupied:emit(poly,shade);continue
  for x,y in cells:
   if maxx<x-.5 or minx>x+.5 or maxy<-y-.5 or miny>-y+.5:continue
   cut=poly
   for axis,bound,greater in [(0,x-.5,True),(0,x+.5,False),(1,-y-.5,True),(1,-y+.5,False)]:
    cut=clipped(cut,axis,bound,greater)
    if len(cut)<3:break
   if len(cut)>=3:emit(cut,shade)
 if not vertices:
  bpy.data.objects.remove(ob,do_unlink=True);return
 new=bpy.data.meshes.new(mesh.name+'_ExactFootprint');new.from_pydata(vertices,[],faces);new.update()
 for material in mesh.materials:new.materials.append(material)
 layer=new.uv_layers.new(name='UVMap');layer.data.foreach_set('uv',[q for p in uvs for q in p]);attr=new.color_attributes.new(name='SpawnRingColor',type='BYTE_COLOR',domain='CORNER');attr.data.foreach_set('color',[q for p in colors for q in p])
 for face,shade in zip(new.polygons,smooth):face.use_smooth=shade
 ob.data=new

def ridge(row):
 cells=[(c['x'],c['y']) for c in row['footprint']];rng=random.Random(seed(row['id']));variant=row['variant'];family=row['family'];massTriangles=[]
 # Four designed masses, not just four seeds applied to the same bead chain.
 designs=[
  [(.45,-.08,1.76,1.45,1.05,.10),(.60,-1.16,1.76,1.86,1.45,-.14),(.38,-2.57,1.84,1.91,1.12,.14)],
  [(.53,-1.47,1.60,3.63,1.58,.06),(.03,-.35,.85,1.17,.94,-.14),(1.08,-2.62,.82,1.51,1.12,.11)],
  [(.62,-.57,1.65,2.16,1.36,.11),(.40,-2.30,1.80,2.40,1.55,-.14),(.08,-1.31,.78,1.10,.88,.23)],
  [(.48,-1.63,1.67,3.76,1.42,-.055),(.94,-.37,.90,1.35,1.10,.15),(.03,-2.79,.92,1.20,1.03,-.18)]]
 if family=='PilotRidgeN':forms=designs[variant]
 elif family=='PilotRidgeE':forms=[(-y,-x,sy,sx,h,a+math.pi/2) for x,y,sx,sy,h,a in designs[variant]]
 else:
  diagonal=[
   [(2.18,-.58,1.70,2.49,1.38,-.61),(.78,-2.37,1.76,2.45,1.53,-.61)],
   [(1.53,-1.52,1.64,4.32,1.66,-.61),(2.90,-.37,.82,1.20,1.06,-.14),(.04,-2.77,.9,1.16,1.09,.1)],
   [(1.93,-.73,1.80,2.95,1.49,-.63),(.73,-2.37,1.72,2.48,1.38,-.64),(2.80,-1.08,.82,.95,1.02,.11)],
   [(1.47,-1.48,1.72,4.38,1.55,-.63),(2.8,-.17,.88,1.19,1.04,-.1),(.19,-2.82,1.0,1.15,.99,.13)]]
  forms=diagonal[variant]
  if family=='PilotRidgeSE':forms=[(3-x,y,sx,sy,h,-a) for x,y,sx,sy,h,a in forms]
 def surface(x,y):
  height=0
  for a,b,c in massTriangles:
   denominator=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
   if abs(denominator)<1e-9:continue
   u=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/denominator;v=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/denominator
   if u>=-.0001 and v>=-.0001 and u+v<=1.0001:height=max(height,u*a[2]+v*b[2]+(1-u-v)*c[2])
  return height
 for crag,(cx,cy,sx,sy,height,angle) in enumerate(forms):
  rx=sx*.5;ry=sy*.5;ca=math.cos(angle);sa=math.sin(angle)
  n=12;vs=[];fs=[];outline=[]
  for i in range(n):
   a=(i+.18)*math.tau/n;xx=math.copysign(abs(math.cos(a))**.59,math.cos(a));yy=math.copysign(abs(math.sin(a))**.61,math.sin(a));jitter=rng.uniform(.79,1.09);outline.append((xx*jitter,yy*jitter))
  # Unequal broad chipped faces and narrow erosion shoulders. Irregular
  # angular polygons deliberately replace the earlier circular capsule caps.
  profile=[(.01,.86),(.11,1),(.35,.97),(.57,.90),(.74,.70)]
  shearx=rng.uniform(-.14,.14);sheary=rng.uniform(-.09,.09)
  for level,(z,r) in enumerate(profile):
   for i,(xx,yy) in enumerate(outline):
    px=xx*rx*r+shearx*z+.035*math.sin(i*2.2+level)*z;py=yy*ry*r+sheary*z+.045*math.sin(i*1.8-level)*z;zz=z*height+(xx*.07+yy*.08)*z+(.052*math.sin(i*2.7+level+variant) if level else 0)
    vs.append((cx+px*ca-py*sa,cy+px*sa+py*ca,zz))
  # Chipped crowns have unequal internal peaks and saddles. Constrained
  # triangulation preserves the exact shoulder while avoiding manufactured
  # broad flat caps or a repeated radial fan at the centre of every stone.
  crown=[Vector(v[:2]) for v in vs[4*n:5*n]];heights=[v[2] for v in vs[4*n:5*n]]
  for j,t in enumerate((-.43,.04,.43)):
   px=rx*rng.uniform(-.19,.19)+shearx*.85;py=ry*t+sheary*.85
   crown.append(Vector((cx+px*ca-py*sa,cy+px*sa+py*ca)))
   heights.append(height*(1.08 if j==(variant+crag)%3 else rng.uniform(.83,1.01)))
  cv,ce,cf,orig,_,_=delaunay_2d_cdt(crown,[(i,(i+1)%n) for i in range(n)],[tuple(range(n))],1,1e-7,True)
  mapping=[]
  for v,sourceids in zip(cv,orig):
   sourceid=sourceids[0]
   if sourceid<n:mapping.append(4*n+sourceid)
   else:mapping.append(len(vs));vs.append((v.x,v.y,heights[sourceid]))
  for face in cf:
   tri=tuple(mapping[i] for i in face)
   a,b,c=(Vector(vs[i]) for i in tri)
   fs.append(tri if (b-a).cross(c-a).z>0 else tri[::-1])
  for k in range(4):
   for i in range(n):
    a=k*n+i;b=k*n+(i+1)%n;c=(k+1)*n+(i+1)%n;d=(k+1)*n+i;fs.extend([(a,b,c),(a,c,d)])
  for i in range(1,n-1):fs.append((0,i+1,i))
  ob=newmesh('Jagged_laminar_woodstone',vs,fs,'pinkstone' if crag%4 else 'root_bark',False)
  # Split a few broad crown polygons into pigment planes. Bevels retain real
  # directional light; this is not a painted fake shadow or object texture.
  massTriangles.extend(tuple(vs[i] for i in f) for f in fs)
 # Transverse grain travels across adjoining pieces but skips actual fractures.
 xmin=min(c[0] for c in cells)-.5;xmax=max(c[0] for c in cells)+.5;ymin=-max(c[1] for c in cells)-.5;ymax=-min(c[1] for c in cells)+.5
 for line in range(int((ymax-ymin)*6)):
  base=ymin+.08+line*.166+rng.uniform(-.035,.035);run=[];previous=None
  for k in range(int((xmax-xmin)*18)+1):
   x=xmin+k/18;y=base+.043*math.sin(x*4.2+line*.7+variant);z=surface(x,y)
   if z>.14 and (previous is None or abs(previous-z)<.20):run.append((x,y,z+.009))
   else:
    if len(run)>2:stem('Petrified_fibre_seam',run,.0085,'pinklight',.0045,5)
    run=[]
   previous=z
  if len(run)>2:stem('Petrified_fibre_seam',run,.0085,'pinklight',.0045,5)
 # Strong ochre scrub and broken talus are spatially owned by this ridge. Each
 # clump is lifted to the true top surface so it cannot disappear inside stone.
 boundary=edges(cells)
 for i in range(35):
  a,b=boundary[i%len(boundary)];t=rng.uniform(.15,.85);x=a[0]+(b[0]-a[0])*t;y=a[1]+(b[1]-a[1])*t
  nearest=min(cells,key=lambda c:(c[0]-x)**2+(-c[1]-y)**2);x=x*.70+nearest[0]*.30;y=y*.70-nearest[1]*.30;z=surface(x,y)+.035
  if i%3==0:
   chip=ellipsoid('Chipped_scree',(x,y,z+.12),(.22,.17,.22),'pinklight' if i%2 else 'pinkstone',1)
   for face in chip.data.polygons:face.use_smooth=False
  for j in range(6):
   aa=j*2.39+i;length=rng.uniform(.18,.29)
   leafshape('Ochre_scrub_clump',(x+math.cos(aa)*.055,y+math.sin(aa)*.055,z+.035+j*.010),length,length*.53,aa,'leaf_sun' if j%3 else 'ochre',.035)
 for ob in list(CURRENT.objects):
  if ob.type=='MESH':clip_to_cells(ob,cells)

def rock(row,large=False):
 rng=random.Random(seed(row['id']));size=1.83 if large else .86;center=(.5,-.5) if large else (0,0)
 for j,(dx,dy,s) in enumerate([(-.12,.04,.69),(.24,-.13,.40),(-.25,-.25,.28)]):
  x=center[0]+dx*size;y=center[1]+dy*size;h=s*size*.6
  ob=ellipsoid('Fractured_woodstone',(x,y,h*.44),(s*size*.51,s*size*.44,h*.52),'pinkstone' if j==0 else 'pinkdark',2)
  for vert in ob.data.vertices:
   vert.co*=rng.uniform(.88,1.08)
   if vert.co.z<-.38*h:vert.co.z=-.38*h
  for face in ob.data.polygons:face.use_smooth=False
  for k in range(3):
   yy=y+(k-1)*s*size*.17
   stem('Mineral_grain',[(x-s*size*.25,yy,h*.88),(x,yy+.015,h*.945),(x+s*size*.24,yy-.008,h*.87)],.010,'pinklight',.006,5)
 for i in range(4):leafshape('Gold_lichen',(center[0]+rng.uniform(-.30,.30)*size,center[1]+rng.uniform(-.30,.30)*size,.08),.11,.065,rng.random()*6,'ochre',.017)

def tar(row):
 rng=random.Random(seed(row['id']))
 # Three connected lobes inside one six-cell seep owner; surface is a separate
 # exported material section so gloss survives Unity import.
 for j in range(3):
  x=.5+[-.06,.15,-.12][j];y=-.13-j*.87;rx=.70 if j==1 else .68;ry=.55
  ob=ellipsoid('Glossy_tar_lobe',(x,y,.042),(rx,ry,.040),'tar',2);ob.data.materials.clear();ob.data.materials.append(WATER);ob['keepSeparate']=True
  rim=torus('Thick_tar_lip',(x,y,.054),rx,.059,'pinkdark',24,6);rim.scale.y=ry/rx
  glint=stem('Tar_meniscus_glint',[(x+math.cos(a)*rx*.76,y+math.sin(a)*ry*.76,.078) for a in [i*.16+1 for i in range(10)]],.011,'water_light',.006,5)
  for k in range(3):
   a=k*2.1+j*.7;xx=x+math.cos(a)*rx*.94;yy=y+math.sin(a)*ry*.95
   if -.39<xx<1.39 and -2.39<yy<.39:softstone('Tar_bank',(xx,yy,.11),(.17,.19,.20),'pinkdark',rng,a)

def pipe(row):
 v=row['variant'];rng=random.Random(seed(row['id']))
 beam('Long_copper_body',(-.30,0,.20),(2.29,0,.20),.165,'copper',12)
 for x in [-.23,.48,1.30,2.22]:
  o=torus('Pipe_coupling',(x,0,.20),.172,.036,'wood_end',12,5);o.rotation_euler[1]=math.pi/2
 for x in [-.315,2.31]:
  o=cylinder('Hollow_pipe_end',(x,0,.20),.137,.012,'black',12);o.rotation_euler[1]=math.pi/2
 for x in [.1,1.9]:box('Mineral_support',(x,0,.07),(.27,.36,.12),'pinkdark',.05)
 for i in range(3):ellipsoid('Verdigris_patch',(rng.uniform(-.1,2.1),-.02,.35),(.055,.065,.009),'leaf_olive',1)

def vent(row):
 rng=random.Random(seed(row['id']));v=row['variant']
 for i in range(3):a=i*2.1;softstone('Vent_stone',(math.cos(a)*.23,math.sin(a)*.22,.10),(.27,.25,.23),'pinkstone',rng,a)
 cylinder('Hollow_vent_body',(0,0,.26),.23,.45,'copper',12,radius2=.19)
 torus('Weathered_vent_rim',(0,0,.49),.20,.056,'grain',16,5)
 cylinder('Dark_vent_throat',(0,0,.485),.17,.016,'black',12)

def ruin(row):
 rng=random.Random(seed(row['id']))
 for level in range(2):
  for j in range(2):softstone('Old_enclosure_masonry',((j-.5)*.45,0,.16+level*.29),(.46,.69,.30),'stone_light' if (level+j+row['variant'])%3==0 else 'stone',rng,rng.uniform(-.035,.035))
 for i in range(3):leafshape('Masonry_lichen',(rng.uniform(-.3,.3),rng.uniform(-.3,.3),.62),.13,.07,rng.random()*6,'ochre',.012)

def bone(row):
 rng=random.Random(seed(row['id']))
 softstone('Mineral_seam_host',(0,0,.035),(.90,.57,.085),'pinkstone',rng,.04)
 for i in range(4):
  x=-.31+i*.20;softstone('Flush_tepuibone_inclusion',(x,rng.uniform(-.10,.10),.072),(.13,.31,.054),'bone' if i%2 else 'bone_shadow',rng,rng.uniform(-.3,.3))

def normalize_actor(mid,cells):
 # Fit actual authored bind-space geometry to the fixed gameplay rectangle.
 coll=MODELS[mid]['collection'];meshes=[o for o in coll.objects if o.type=='MESH'];pts=[o.matrix_basis@v.co for o in meshes for v in o.data.vertices]
 xmin=min(p.x for p in pts);xmax=max(p.x for p in pts);ymin=min(p.y for p in pts);ymax=max(p.y for p in pts)
 w=max(x for x,y in cells)+1;h=max(y for x,y in cells)+1;factor=min((w-.12)/(xmax-xmin),(h-.12)/(ymax-ymin));offset=Vector(((w-1)/2-(xmin+xmax)*factor/2,-(h-1)/2-(ymin+ymax)*factor/2,0))
 for ob in meshes:
  # Bake object-local transform before rescaling so rest skin and armature agree.
  old=ob.matrix_basis.copy()
  for p in ob.data.vertices:p.co=old@p.co*factor+offset
  ob.matrix_basis=Matrix.Identity(4)
 rig=next(o for o in coll.objects if o.type=='ARMATURE');SCENE.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for b in rig.data.edit_bones:b.head=b.head*factor+offset;b.tail=b.tail*factor+offset
 bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False);SCENE.collection.objects.unlink(rig)
 MODELS[mid]['normalizationScale']=factor

for row in CONTRACT['models']:
 mid=row['id'];family=row['family'];CURRENT=model(mid,row['role'],'anchor-cell-centre')
 if family.startswith('PilotRidge'):ridge(row)
 elif family in ('PilotBoulder','PilotRock'):rock(row,family=='PilotBoulder')
 elif family=='PilotTar':tar(row)
 elif family=='PilotPipe':pipe(row)
 elif family=='PilotSteamVent':vent(row)
 elif family=='PilotRuinWall':ruin(row)
 elif family=='PilotTepuibone':bone(row)
 elif family=='PilotGround':box('Ground_slab',(0,0,-.04),(1,1,.08),'tepui'+str(row['variant']),0)
 else:
  bpy.data.collections.remove(CURRENT);MODELS.pop(mid)
  if family=='PilotHermit':character('hood_olive',mid);MODELS[mid]['rigFamily']='humanoid'
  else:creature(mid,row['blueprint'],'frog' if family=='PilotMawToad' else 'serpent')
  normalize_actor(mid,[(c['x'],c['y']) for c in row['footprint']])
 MODELS[mid]['contract']=row
 print('AUTHORED',mid,flush=True)
library=bpy.data.collections.new('ASSET_LIBRARY_EDITABLE');SCENE.collection.children.link(library)
for info in MODELS.values():library.children.link(info['collection'])
bpy.context.view_layer.update();rows=[];geometry=[]
for mid,info in MODELS.items():
 pts=[];triangles=0;zero=0;outside=0;cells=[(c['x'],c['y']) for c in info['contract']['footprint']]
 for ob in info['collection'].objects:
  if ob.type!='MESH':continue
  mesh=ob.data;mesh.calc_loop_triangles();triangles+=len(mesh.loop_triangles)
  for tri in mesh.loop_triangles:
   a,b,c=(mesh.vertices[i].co for i in tri.vertices)
   if (b-a).cross(c-a).length<1e-10:zero+=1
  for vtx in mesh.vertices:
   p=ob.matrix_basis@vtx.co;pts.append(p)
   if not inside(cells,p.x,p.y,.002):outside+=1
 mins=[min(p[k] for p in pts) for k in range(3)];maxs=[max(p[k] for p in pts) for k in range(3)]
 unity=lambda xs:dict(x=round(xs[0],6),y=round(xs[2],6),z=round(xs[1],6))
 row=dict(info['contract']);row.update(path='models/'+mid+'.fbx',triangles=triangles,boundsCenter=unity([(a+b)/2 for a,b in zip(mins,maxs)]),boundsSize=unity([b-a for a,b in zip(mins,maxs)]),outsideFootprintVertices=outside,zeroAreaTriangles=zero,rigged=info.get('rigged',False),rigFamily=info.get('rigFamily','none'),clips=info.get('clips',[]),sockets=info.get('sockets',[]),pivot='anchor-cell-centre')
 rows.append(row);geometry.append(dict(modelId=mid,triangles=triangles,zeroAreaTriangles=zero,outsideFootprintVertices=outside))
 if not args.skip_export:export_collection(mid)
 print('EXPORTED',mid,triangles,'outside',outside,'zero',zero,flush=True)
(OUT/'catalog.json').write_text(json.dumps(dict(schemaVersion=1,id='multicell-pilot-3d',zoneId=LAYOUT['zoneId'],zoneWidth=80,zoneHeight=25,coordinates='Unity X east,Y height,Z north; one unit per cell; anchor-cell-centre',paletteTexture='textures/PilotPalette.png',models=rows),indent=2)+'\n')
(OUT/'reports/geometry.json').write_text(json.dumps(dict(modelCount=len(rows),uniqueTriangles=sum(r['triangles'] for r in rows),models=geometry),indent=2)+'\n')
ATLAS.pack();ATLAS.filepath='//textures/PilotPalette.png'
SCENE.view_layers[0].layer_collection.children[library.name].exclude=True
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'pilot_kit.blend'),compress=True)
# Preview assembly uses a per-owner prototype; geometry stays independently
# editable in the library and independently removable at runtime.
preview=bpy.data.collections.new('PREVIEW_PROTOTYPES');SCENE.collection.children.link(preview);PROTOS={}
for mid,info in MODELS.items():
 coll=bpy.data.collections.new(mid+'__Preview');preview.children.link(coll)
 for water in (False,True):
  group=[o for o in info['collection'].objects if o.type=='MESH' and bool(o.get('keepSeparate'))==water]
  if not group:continue
  vs=[];fs=[];uvs=[];smooth=[]
  for o in group:
   offset=len(vs);vs.extend(tuple(o.matrix_basis@v.co) for v in o.data.vertices);fs.extend(tuple(offset+i for i in p.vertices) for p in o.data.polygons);uvs.extend(tuple(t.uv) for t in o.data.uv_layers[0].data);smooth.extend(p.use_smooth for p in o.data.polygons)
  mesh=bpy.data.meshes.new(mid+'PreviewMesh');mesh.from_pydata(vs,[],fs);mesh.update();mesh.materials.append(WATER if water else MAT);layer=mesh.uv_layers.new();layer.data.foreach_set('uv',[x for pair in uvs for x in pair])
  for p,s in zip(mesh.polygons,smooth):p.use_smooth=s
  ob=bpy.data.objects.new(mid+('__Water' if water else '__Geometry'),mesh);coll.objects.link(ob)
 PROTOS[mid]=coll
SCENE.view_layers[0].layer_collection.children[preview.name].exclude=True
placed=bpy.data.collections.new('Overworld.3.7.0_AUTHORED_OWNERS');SCENE.collection.children.link(placed)
def place(mid,x,y,name):
 ob=bpy.data.objects.new(name,None);ob.instance_type='COLLECTION';ob.instance_collection=PROTOS[mid];placed.objects.link(ob);ob.location=(x+.5,24.5-y,0);ob['modelId']=mid;ob['ownerId']=name;return ob
# Two triangles per native ground cell with continuous world UVs. Runtime may
# use exactly these UVs in its existing dirty per-cell ground patch pipeline.
gvs=[];gfs=[]
for y in range(25):
 for x in range(80):
  q=len(gvs);gvs.extend([(x,24-y,0),(x+1,24-y,0),(x+1,25-y,0),(x,25-y,0)]);gfs.append((q,q+1,q+2,q+3))
gmesh=bpy.data.meshes.new('Native_cell_ground_world_uv');gmesh.from_pydata(gvs,[],gfs);gmesh.update();gmesh.materials.append(GROUND_MAT);guv=gmesh.uv_layers.new()
for loop in gmesh.loops:
 co=gmesh.vertices[loop.vertex_index].co;guv.data[loop.index].uv=(co.x/80,co.y/25)
go=bpy.data.objects.new('Ground_cell_patches_preview',gmesh);placed.objects.link(go)
for p in LAYOUT['placements']:place(p['modelId'],p['x'],p['y'],p['id'])
SCENE.world.use_nodes=True;SCENE.world.node_tree.nodes.get('Background').inputs[0].default_value=(.42,.44,.49,1);SCENE.world.node_tree.nodes.get('Background').inputs[1].default_value=.65
sun_data=bpy.data.lights.new('Warm_grazing_sun','SUN');sun_data.energy=2.15;sun_data.color=(1,.94,.89);sun_data.angle=math.radians(18);sun=bpy.data.objects.new('Warm_grazing_sun',sun_data);SCENE.collection.objects.link(sun);sun.rotation_euler=(math.radians(27),math.radians(-22),math.radians(-28))
SCENE.render.engine='CYCLES';SCENE.cycles.device='CPU';SCENE.cycles.samples=args.samples;SCENE.cycles.use_denoising=True;SCENE.render.resolution_x=2400;SCENE.render.resolution_y=750;SCENE.render.resolution_percentage=100;SCENE.render.image_settings.file_format='PNG';SCENE.view_settings.view_transform='AgX';SCENE.view_settings.look='AgX - Medium High Contrast';SCENE.view_settings.exposure=.15
camd=bpy.data.cameras.new('Strict_Overhead');cam=bpy.data.objects.new('Strict_Overhead',camd);SCENE.collection.objects.link(cam);cam.location=(40,12.5,90);cam.rotation_euler=(0,0,0);camd.type='ORTHO';camd.ortho_scale=80;SCENE.camera=cam
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'scene.blend'),compress=True)
if args.render:
 SCENE.render.filepath=str(OUT/'renders/overhead.png');bpy.ops.render.render(write_still=True)
 cam.location=(37,14,60);camd.ortho_scale=25;SCENE.render.resolution_x=1600;SCENE.render.resolution_y=1000;SCENE.render.filepath=str(OUT/'renders/ridge-detail.png');bpy.ops.render.render(write_still=True)
print('PILOT_BUILD_COMPLETE',len(rows),len(LAYOUT['placements']),flush=True)
