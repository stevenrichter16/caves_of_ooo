#!/usr/bin/env python3
"""Build a real reusable ring kit and snapshot scenes, writing only --output."""
import bpy,bmesh,math,random,json,argparse,sys,hashlib,gzip,shutil,time
from pathlib import Path
from mathutils import Vector,Matrix
import numpy as np
SOURCE=Path(__file__).resolve().parent
ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path,required=True);ap.add_argument('--skip-export',action='store_true');ap.add_argument('--render',action='store_true');ap.add_argument('--zones',default='all');ap.add_argument('--samples',type=int,default=32);ap.add_argument('--kit-only',action='store_true');ap.add_argument('--export-only',default='');args=ap.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
OUT=args.output.resolve();OUT.mkdir(parents=True,exist_ok=True)
for s in ('models','textures','reports','scenes','renders'): (OUT/s).mkdir(exist_ok=True)
def read_native(name):
 with gzip.open(SOURCE/'native'/(name+'.json.gz'),'rt') as f:return json.load(f)
INDEX=read_native('ring-index');FELLING=read_native('Felling-native-definition');CONTRACT=json.loads((SOURCE/'catalog-contract.json').read_text())
SEED=9092026;RNG=random.Random(SEED);MODELS={};CURRENT=None;PLACEMENTS=[];STATIC=[];SERIAL=0
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
 if c.name!='Collection':bpy.data.collections.remove(c)
SCENE=bpy.context.scene;SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1;SCENE.render.threads_mode='FIXED';SCENE.render.threads=2;SCENE.render.fps=24
exec(compile((SOURCE/'mesh_kit.py').read_text(),str(SOURCE/'mesh_kit.py'),'exec'),globals())
COLORS += [
 ('tepui0',(.505,.445,.382)),('tepui1',(.505,.445,.382)),('tepui2',(.505,.445,.382)),('tepui3',(.505,.445,.382)),
 ('forest0',(.395,.37,.225)),('forest1',(.395,.37,.225)),('forest2',(.395,.37,.225)),('forest3',(.395,.37,.225)),
 ('loam0',(.34,.285,.19)),('loam1',(.34,.285,.19)),('loam2',(.34,.285,.19)),('loam3',(.34,.285,.19)),
 ('pinkstone',(.55,.46,.435)),('pinklight',(.67,.58,.515)),('pinkdark',(.385,.355,.345)),('grain',(.71,.615,.535)),
 ('leaf_olive',(.395,.455,.14)),('leaf_sun',(.57,.585,.19)),('leaf_forest',(.315,.385,.135)),('leaf_tip',(.64,.64,.245)),
 ('bark_pale',(.52,.455,.335)),('bark_shadow',(.245,.23,.175)),('bark_ridge',(.68,.60,.455)),('bark_moss',(.37,.385,.22)),
 ('fungal_pale',(.77,.73,.54)),('fungal_light',(.89,.845,.665)),('fungal_violet',(.57,.39,.56)),('fungal_dark',(.40,.295,.41)),
 ('ochre',(.61,.405,.20)),('wine',(.40,.115,.235)),('wine_light',(.61,.225,.365)),('dew',(.70,.745,.67)),
 ('frog_green',(.425,.61,.205)),('frog_light',(.69,.735,.365)),('frog_dark',(.235,.375,.135)),('frog_stripe',(.82,.81,.515)),
 ('glass_belly',(.665,.775,.615)),('heart',(.64,.285,.245)),('tortoise_shell',(.40,.315,.18)),('tortoise_edge',(.665,.51,.215)),
 ('yellowfoot',(.78,.60,.16)),('snake_olive',(.425,.46,.23)),('snake_stone',(.335,.38,.355)),('snake_scale',(.53,.54,.435)),
 ('eagle_ash',(.81,.785,.66)),('eagle_dark',(.295,.285,.26)),('eagle_beak',(.70,.57,.28)),('wet_rock',(.355,.40,.335)),
 ('wet_light',(.53,.55,.43)),('water_glint',(.43,.63,.575)),('brine',(.30,.435,.40)),('tar',(.095,.105,.09)),
 ('ash',(.40,.385,.355)),('copper',(.58,.315,.135)),('bone',(.82,.775,.61)),('bone_shadow',(.62,.575,.42)),
 ('cloth_old',(.52,.465,.29)),('chitin',(.35,.235,.135)),('petal_violet',(.645,.47,.64)),('petal_blue',(.40,.655,.67)),
 ('petal_red',(.69,.225,.255)),('pale_cyan',(.57,.77,.70)),('creekbed',(.27,.305,.20)),('moss_glow',(.55,.63,.31))]
# Unused axis-marker swatch becomes ring-only directional bark paint. No
# exported catalog model uses axis_x; dimensions and every other swatch stay fixed.
COLORS=[('root_bark',(.55,.46,.435)) if n=='axis_x' else (n,c) for n,c in COLORS]
assert len(COLORS)==128
CINDEX={n:i for i,(n,_) in enumerate(COLORS)}
# Opaque portable paint atlas; fine surface detail is real albedo/UV, not Blender-only nodes.
W,H,T=2048,1024,128
ATLAS=bpy.data.images.new('SpawnRingPalette',width=W,height=H,alpha=False);pixels=np.ones((H,W,4),dtype=np.float32)
u,v=np.meshgrid((np.arange(T)+.5)/T,(np.arange(T)+.5)/T)
def field(rng,scale):
 grid=rng.random((scale+1,scale+1));grid[-1,:]=grid[0,:];grid[:,-1]=grid[:,0]
 xx=u*scale;yy=v*scale;ix=xx.astype(int);iy=yy.astype(int);fx=xx-ix;fy=yy-iy
 fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
 return ((grid[iy,ix]*(1-fx)+grid[iy,ix+1]*fx)*(1-fy)+(grid[iy+1,ix]*(1-fx)+grid[iy+1,ix+1]*fx)*fy)
for idx,(name,color) in enumerate(COLORS):
 rng=np.random.default_rng(SEED+idx*919);n=rng.random((T,T))-.5
 broad=field(rng,4)-.5;fine=field(rng,19)-.5
 value=1+n*.09+broad*.13+fine*.08
 tile=np.zeros((T,T,3),dtype=np.float32)+np.array(color)
 if name.startswith(('tepui','forest','loam')):
  # Ground paint replaces raised per-cell flecks. Shared edge paint and low
  # contrast keep each reusable unit surface quiet under native camera scale.
  edge=np.clip(np.minimum.reduce([u,v,1-u,1-v])/.12,0,1)
  edge=edge*edge*(3-2*edge)
  macro=field(rng,3);medium=field(rng,8)
  value=1+edge*(n*.06+(fine-.0)*.055+broad*.09)
  blend=np.clip((macro-.35)*.66,0,.25)*edge
  paint=np.array((.36,.385,.265)) if name.startswith('tepui') else np.array((.31,.36,.19))
  tile=tile*(1-blend[:,:,None])+paint*blend[:,:,None]
  # Shallow, warped stone joints avoid continuous scratch lines. Paint stays
  # restrained at cell edges; it does not manufacture a walkable rock object.
  if name.startswith('tepui'):
   wx=u+(field(rng,6)-.5)*.065;wy=v+(field(rng,7)-.5)*.065
   sites=rng.uniform(-.2,1.2,(16,2))
   distances=np.sort(np.stack([(wx-x)**2+(wy-y)**2 for x,y in sites]),axis=0)
   seam=np.exp(-((np.sqrt(distances[1])-np.sqrt(distances[0]))/.013)**2)
   value-=seam*(.035 if name.endswith('0') else .085)*edge
   value+=(field(rng,5)-.5)*.045*edge
   value+=np.clip((medium-.56)*.11,0,.025)*edge
  else:
   value+=np.clip((medium-.53)*.13,0,.035)*edge
  # Scattered flecks belong to the paint surface and share its gentle light;
  # no tiny cast-shadow geometry or repeated circular moss islands remain.
  variant=int(name[-1]);isstone=name.startswith('tepui')
  centres=rng.uniform(.16,.84,(3,2))
  for j in range(38 if isstone else 78):
   if j%3:
    centre=centres[j%3];px,py=centre+rng.normal(0,.12,2)
   else:px,py=rng.uniform(.10,.90,2)
   rx,ry=rng.uniform(.012,.035),rng.uniform(.007,.022)
   angle=rng.uniform(0,math.tau);dx=u-px;dy=v-py
   a=(dx*math.cos(angle)+dy*math.sin(angle))/rx;b=(-dx*math.sin(angle)+dy*math.cos(angle))/ry
   mask=np.clip((1-a*a-b*b)*3,0,1)*edge
   if isstone:
    pigment=np.array((.59,.535,.435)) if j%4 else np.array((.365,.39,.255));opacity=.21 if j%4 else .38
   else:
    pigment=np.array((.48,.495,.255)) if j%4 else np.array((.30,.365,.185));opacity=.54
   tile=tile*(1-mask[:,:,None]*opacity)+pigment*mask[:,:,None]*opacity
 elif name=='root_bark':
  # Long irregular mineral grain, aligned by authored ridge UVs; shadows stay
  # fully dynamic. This slot formerly held an unused diagnostic axis color.
  value=1+n*.09+broad*.12+fine*.08
  moss=np.zeros((T,T),dtype=np.float32)
  for j in range(5):
   offset=(j+.5)/5+rng.uniform(-.065,.065)
   curve=offset+.024*np.sin(u*rng.uniform(3,7)+rng.uniform(0,6))+.012*(field(rng,4)-.5)
   broken=np.clip((field(rng,3)-.27)*3,0,1)
   crease=np.exp(-((v-curve)/.010)**2)*broken
   value-=crease*.14
   value+=np.exp(-((v-curve-.018)/.016)**2)*broken*.045
   moss=np.maximum(moss,np.exp(-((v-curve)/.035)**2)*np.clip((field(rng,3)-.58)*2,0,.28))
  tile=tile*(1-moss[:,:,None])+np.array((.32,.365,.225))*moss[:,:,None]
 elif 'stone' in name or name in ('grain','wet_rock','wet_light','bone','bone_shadow'):
  value+=broad*.10+fine*.07
 elif 'bark' in name or name.startswith('wood'):
  # Fine irregular grain avoids the previous visibly sinusoidal fabric appearance.
  stripes=field(rng,27)-.5;value+=stripes*.12+fine*.045
 elif 'leaf' in name or name in ('moss','moss_light','grass'):
  value+=broad*.14+fine*.07
 elif 'fungal' in name:value+=broad*.09
 tile*=value[:,:,None];pixels[(idx//16)*T:(idx//16+1)*T,(idx%16)*T:(idx%16+1)*T,:3]=np.clip(tile,0,1)
ATLAS.pixels.foreach_set(pixels.ravel());ATLAS.filepath_raw=str(OUT/'textures/SpawnRingPalette.png');ATLAS.file_format='PNG';ATLAS.save()
MAT=bpy.data.materials.new('SpawnRingPalette');MAT.use_nodes=True;bs=MAT.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.88
tex=MAT.node_tree.nodes.new('ShaderNodeTexImage');tex.image=ATLAS;tex.interpolation='Linear';MAT.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
WATER=bpy.data.materials.new('SpawnRingWater');WATER.use_nodes=True;bs=WATER.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.018,.085,.09,1);bs.inputs['Roughness'].default_value=.35;bs.inputs['Specular IOR Level'].default_value=.2

def ground_turn(x,y):
 h=((x*0x9e3779b9)^(y*0x85ebca6b))&0xffffffff;h^=h>>16;h=(h*0x7feb352d)&0xffffffff;h^=h>>15
 return (h&3)*math.pi/2
def seed(s):return int(hashlib.sha256(str(s).encode()).hexdigest()[:8],16)
def stem(name,points,radius,color,radius_end=.012,sections=7):
 # Smooth tapered tube, robust local frames; inexpensive readable organic stems.
 vs=[];rings=len(points)
 for i,p in enumerate(points):
  p=Vector(p);t=Vector(points[min(i+1,rings-1)])-Vector(points[max(0,i-1)])
  t.normalize();ref=Vector((0,0,1)) if abs(t.z)<.9 else Vector((1,0,0));a=t.cross(ref).normalized();b=t.cross(a).normalized();r=radius+(radius_end-radius)*i/(rings-1)
  for j in range(sections):vs.append(tuple(p+r*(math.cos(j*math.tau/sections)*a+math.sin(j*math.tau/sections)*b)))
 fs=[tuple(reversed(range(sections))),tuple((rings-1)*sections+j for j in range(sections))]
 for i in range(rings-1):
  for j in range(sections):fs.append((i*sections+j,i*sections+(j+1)%sections,(i+1)*sections+(j+1)%sections,(i+1)*sections+j))
 return newmesh(name,vs,fs,color,True)
def moss_cluster(rng,center,radius=.3,count=7):
 for i in range(count):
  a=rng.random()*math.tau;r=radius*math.sqrt(rng.random());leafshape('Moss_leaf',(center[0]+math.cos(a)*r,center[1]+math.sin(a)*r,center[2]+rng.uniform(0,.045)),rng.uniform(.10,.19),rng.uniform(.075,.13),a,rng.choice(['moss','moss_light','leaf_olive']),.024)
def flower(rng,loc,color='flower_white',scale=.06):
 cylinder('Flower_centre',(loc[0],loc[1],loc[2]),scale*.42,.025,'gold',6)
 for j in range(5):
  a=j*math.tau/5;ellipsoid('Flower_petal',(loc[0]+math.cos(a)*scale*.6,loc[1]+math.sin(a)*scale*.6,loc[2]),(scale*.57,scale*.40,.018),color,1)
def rock_piece(rng,loc,scale,color='stone'):
 o=softstone('Worn_stone',loc,scale,color,rng,rng.uniform(-.25,.25));return o
def shelf_fungus(rng,loc,r=.24,color='fungal_violet'):
 # Thick nested scalloped shelf rather than generic sphere mushroom.
 n=12;outline=[(math.cos(i*math.tau/n)*r*(1+.05*math.sin(i*4)),math.sin(i*math.tau/n)*r*.72) for i in range(n)]
 ob=fittedstone('Fungal_shelf',loc,outline,.11,color);torus('Ochre_shelf_margin',(loc[0],loc[1],loc[2]+.065),r*.66,.026,'ochre',12,4).scale.y=.74
 return ob

def tiny_flower(loc,color='flower_white',radius=.045):
 vs=[];fs=[]
 for i in range(5):
  angle=i*math.tau/5;cx=loc[0]+math.cos(angle)*radius*.57;cy=loc[1]+math.sin(angle)*radius*.57
  start=len(vs);vs.extend([(cx+math.cos(angle)*radius*.6,cy+math.sin(angle)*radius*.6,loc[2]),(cx-math.sin(angle)*radius*.34,cy+math.cos(angle)*radius*.34,loc[2]+.008),(loc[0],loc[1],loc[2]),(cx+math.sin(angle)*radius*.34,cy-math.cos(angle)*radius*.34,loc[2]+.008)]);fs.extend([(start,start+1,start+2),(start,start+2,start+3)])
 return newmesh('Tiny_wildflower',vs,fs,color,False)
def ground(v,color):
 # A low solid base gives predictable imported thickness. Moss/soil grain lives
 # in each existing UV swatch, without raised flecks or circular tile decals.
 # Variant0 stays free of foliage, including the seven Felling scar positions.
 box('Ground_base',(0,0,-.04),(1,1,.08),color,0)

def canopy(v,wall=False,bush=False):
 rng=random.Random(6200+v+(50 if wall else 0)+(90 if bush else 0))
 height=.45 if bush else .95 if wall else 1.65;radius=.49 if bush else .61 if wall else .76
 if not bush:
  stem('Twisted_trunk',[(0,0,0),(.035,-.015,height*.4),(-.03,.02,height*.78)],.10,'wood',.045,7)
  for a in [0,2.1,4.2]:stem('Root',[(math.cos(a)*.35,math.sin(a)*.32,.045),(.02,0,.15),(0,0,height*.35)],.045,'wood_dark',.025,6)
 lobes=[]
 for j in range([3,4,5,4][v] if bush else [4,5,6,4][v]):
  a=j*2.4+v*.61;rr=radius*(.40 if j else .08);x=rr*math.cos(a)*(1.12 if v==1 else .94);y=rr*math.sin(a)*(.65 if v==1 else 1.04);r=radius*rng.uniform(.43,.61);z=height+rng.uniform(-.16,.13)+(j%2)*(.14 if v==2 else 0)
  lobes.append((x,y,z,r));ellipsoid('Dense_round_crown_core',(x,y,z),(r,r*.90,r*.51),'leaf_forest',2)
 count=88 if bush else 100 if wall else 145
 for i in range(count):
  x0,y0,z0,r=lobes[i%len(lobes)];a=rng.random()*math.tau;rr=r*math.sqrt(rng.random())*1.04
  x=x0+rr*math.cos(a);y=y0+rr*math.sin(a);z=z0+r*.51*math.sqrt(max(0,1-min(1,rr/r)**2))+.025
  length=rng.uniform(.115,.19) if bush else rng.uniform(.15,.23)
  col=rng.choice(['leaf_olive','leaf_olive','leaf','leaf_sun'])
  leafshape('Layered_rounded_leaf',(x,y,z),length,length*rng.uniform(.60,.82),a+rng.uniform(-.4,.4),col,length*.16)
 if wall:rock_piece(rng,(.17,-.12,.14),(.37,.31,.28),'wet_rock')
 elif bush and v%2==0:
  for j in range(5):ellipsoid('Berry',(rng.uniform(-.24,.24),rng.uniform(-.22,.22),height+.22),(.035,.035,.030),'red',1)

def natural_rock(rng,loc,size,color):
 bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=2,radius=1)
 for v in bm.verts:
  factor=rng.uniform(.87,1.10);v.co.x=math.copysign(abs(v.co.x)**.78,v.co.x)*size[0]*.5*factor;v.co.y=math.copysign(abs(v.co.y)**.82,v.co.y)*size[1]*.5*factor;v.co.z=max(-size[2]*.43,min(size[2]*.39,v.co.z*size[2]*.51*factor))
 mesh=bpy.data.meshes.new('Weathered_boulder');bm.to_mesh(mesh);bm.free();mesh.update();color_mesh(mesh,color);ob=bpy.data.objects.new('Weathered_boulder',mesh);CURRENT.objects.link(ob);ob.location=loc
 for p in mesh.polygons:p.use_smooth=True
 return ob
def stone_cluster(v,kind):
 rng=random.Random(seed(kind)+v);grain=kind in ('GrainRidge','TepuiWall','DescentLedge');low=kind=='DescentLedge';lip=kind=='SinkholeLip'
 if kind=='Rock':
  for j,(x,y,scale) in enumerate([(-.18,-.07,.58),(.18,.12,.46),(.29,-.22,.27)]):
   natural_rock(rng,(x,y,scale*.34),(scale,scale*.84,scale*.76),rng.choice(['stone','stone_light','stone_dark']))
 elif kind=='TepuiWall':
  h=.82+(v%3)*.12;natural_rock(rng,(0,0,h*.45),(.98,.97,h),'pinkstone' if v%2 else 'pinkdark')
  for j in range(2):
   y=(j-.5)*.24+rng.uniform(-.06,.06);stem('Worn_petrified_grain',[(-.29,y-.03,h*.83),(-.08,y,h*.93),(.22,y+.06,h*.84)],.011,'pinklight',.006,5)
  rock_piece(rng,(.31,-.30,.13),(.32,.29,.24),'pinklight')
 elif low:
  # Four designed, convex worn outlines. Their broad low tops remain readable
  # and inside the occupied cell; none adds a new walkable ledge or collision.
  outlines=[
   [(-.47,-.26),(-.35,-.37),(.26,-.35),(.48,-.13),(.43,.25),(.19,.34),(-.31,.29)],
   [(-.29,-.46),(.17,-.44),(.34,-.24),(.30,.35),(.04,.46),(-.32,.31),(-.37,-.12)],
   [(-.47,-.28),(-.13,-.39),(.34,-.31),(.48,-.02),(.34,.35),(-.25,.39),(-.43,.16)],
   [(-.37,-.39),(.16,-.44),(.43,-.19),(.31,.32),(-.01,.43),(-.42,.15)]]
  outline=outlines[v];n=len(outline);vs=[]
  height=[.19,.23,.17,.22][v]
  for z,scale in [(.015,.84),(.065,1),(height,.86)]:
   vs.extend((x*scale,y*scale,z+x*.025-y*.018) for x,y in outline)
  fs=[tuple(reversed(range(n))),tuple(range(2*n,3*n))]
  for level in range(2):
   for k in range(n):fs.append((level*n+k,level*n+(k+1)%n,(level+1)*n+(k+1)%n,(level+1)*n+k))
  ob=newmesh('Broad_weathered_ledge',vs,fs,['pinkstone','stone','pinklight','pinkstone'][v],True)
  ob.data.polygons[0].use_smooth=False;ob.data.polygons[1].use_smooth=False
  # One interrupted grain changes position and direction with the stone form.
  paths=[ [(-.34,-.10),(-.07,-.06),(.23,.04)], [(-.12,-.28),(-.07,-.02),(.04,.25)], [(-.24,.20),(.02,.12),(.33,.05)], [(-.25,-.23),(-.03,-.05),(.15,.18)] ]
  pts=[(x,y,height+x*.025-y*.018+.004) for x,y in paths[v]]
  stem('Broken_ledge_grain',pts,.006,'pinkdark',.003,5)
 elif lip:
  for j in range(3):
   x=(j-1)*.27;natural_rock(rng,(x,rng.uniform(-.08,.08),.23),(.47,.57,.46),rng.choice(['wet_rock','wet_light','stone_dark']))
 else:
  # East-west layers of a broad grain ridge, rather than a row of small posts.
  for j in range(3):
   h=.18+j*.14;rock_piece(rng,(rng.uniform(-.03,.03),rng.uniform(-.035,.035),h),(.99-j*.07,.87-j*.06,.25),rng.choice(['pinkstone','pinklight','pinkdark']))
   stem('East_west_grain',[(-.42,-.31+j*.035,h+.075),(-.15,-.32+j*.035,h+.12),(.20,-.29+j*.035,h+.11),(.40,-.26+j*.035,h+.07)],.017,'grain',.011,6)
 # Grounded moss/flowers occupy the native model's own cell, not neighbouring gameplay floor.
 moss_cluster(rng,(-.22 if low and v%2 else .25,-.23,.04),.14 if low else .19,3 if low else 5)
 if v%2==0:
  tiny_flower((-.29,.27,.055),'flower_yellow',.043);tiny_flower((-.20,.33,.035),'flower_white',.034)

def wall(v):
 rng=random.Random(791+v)
 for level in range(2):
  for j in range(2):rock_piece(rng,((j-.5)*.49+(.10 if level else 0),0,.17+level*.32),(.49,.52,.34),['stone','stone_warm','stone_dark'][(j+level+v)%3])
 moss_cluster(rng,(.18,0,.66),.24,5)
def mushroom_group(v,kind):
 rng=random.Random(seed(kind)+v)
 if kind=='MycelialColumn':
  stem('Living_pale_column',[(0,0,0),(.07,-.01,.36),(-.025,.03,.77),(0,0,1.15)],.18,'fungal_pale',.14,9)
  ellipsoid('Closed_column_fist',(0,0,1.16),(.235,.23,.25),'fungal_light',2)
  for q in range(3):
   a=q*math.tau/3;stem('Violet_moving_vein',[(.17*math.cos(a),.17*math.sin(a),.12),(.17*math.cos(a+.2),.17*math.sin(a+.2),.62),(.14*math.cos(a),.14*math.sin(a),1.17)],.012,'fungal_violet',.008,5)
  for a in [0,2,4]:stem('Column_root',[(.32*math.cos(a),.32*math.sin(a),.035),(.13*math.cos(a),.13*math.sin(a),.14),(0,0,.34)],.055,'fungal_pale',.022,6)
 elif kind=='FruitingBody':
  stem('Fruiting_stem',[(0,0,0),(-.03,.04,.34),(0,0,.7)],.13,'ochre',.10,8)
  for j in range(4):shelf_fungus(rng,(rng.uniform(-.13,.13),rng.uniform(-.13,.13),.20+j*.17),rng.uniform(.23,.34),'fungal_violet' if j%2 else 'fungal_pale')
 else:
  for j in range(4):
   a=j*2.4;x=math.cos(a)*.24;y=math.sin(a)*.23;h=.14+(j%3)*.12
   cylinder('Mushroom_stem',(x,y,h*.5),.04,h,'fungal_pale',7);ellipsoid('Mushroom_cap',(x,y,h),(.15,.14,.09),'petal_violet' if v%2 else 'ochre',1)
 moss_cluster(rng,(0,0,.02),.37,4)
def redgrowth(v,sundew=False):
 rng=random.Random(1390+v);count=7 if sundew else 5
 for i in range(count):
  a=i*math.tau/count+v*.4;r=.22 if sundew else .18
  ob=leafshape('Wine_leaf' if sundew else 'Red_leaf',(math.cos(a)*r,math.sin(a)*r,.10+(i%2)*.05),.47 if sundew else .28,.19,a,'wine' if sundew else 'leaf_olive',.07)
  if sundew:
   for q in [-1,0,1]:ellipsoid('Sticky_dew',(math.cos(a)*(.34+q*.05),math.sin(a)*(.34+q*.05),.18),(.023,.024,.026),'dew',1)
  else:
   stem('Red_growth_stem',[(math.cos(a)*r,math.sin(a)*r,.01),(math.cos(a)*r*.7,math.sin(a)*r*.7,.31)],.024,'wine',.014,6)
   ellipsoid('Heavy_red_fruit',(math.cos(a)*r*.7,math.sin(a)*r*.7,.32),(.071,.065,.09),'petal_red',1)
def compost(v,cache=False):
 rng=random.Random(890+v)
 for j in range(5):
  x=rng.uniform(-.39,.39);y=rng.uniform(-.23,.23)
  rock_piece(rng,(x,y,.07),(.27,.21,.13),'soil')
 # Torn cloth lies almost flat. It must not become an invented tent or usable item.
 verts=[]
 for iy in range(3):
  for ix in range(4):verts.append(((ix/3-.5)*.52,(iy/2-.5)*.24,.095+.025*math.sin(ix*1.8+iy+v)))
 faces=[]
 for iy in range(2):
  for ix in range(3):
   if (ix+iy+v)%4:faces.append((iy*4+ix,iy*4+ix+1,(iy+1)*4+ix+1,(iy+1)*4+ix))
 newmesh('Frayed_cloth',verts,faces,'cloth_old',True)
 ellipsoid('Discarded_boot',(.19,-.08,.14),(.15,.065,.075),'leather',1)
 for j in range(2):
  ob=softstone('Chitin',(-.25+j*.16,.13,.14),(.16,.15,.10),'chitin',rng,j*.4)
 if cache:
  ellipsoid('Old_pocket',(.03,.03,.18),(.16,.13,.10),'leather',1)
  for j in range(2):cylinder('Coin_glimpse',(.01+j*.07,.02,.28),.038,.018,'gold',8)
 elif v%2==0:moss_cluster(rng,(-.22,-.1,.12),.18,4)

def pool(v,kind='WaterPuddle'):
 rng=random.Random(seed(kind)+v)
 color='tar' if kind=='TarSeep' else 'brine' if kind=='BrinePool' else 'soil' if kind=='PeatBog' else 'creekbed' if kind in ('WaterPuddle','GroveSeep') else 'water'
 base=cylinder('Rounded_spray_basin',(0,0,.022),.485,.02,color,24) if kind=='SprayPool' else box('Native_bed_or_hazard_surface',(0,0,-.006 if kind in ('WaterPuddle','GroveSeep','PeatBog') else .025),(1,1,.016),color,0)
 if kind=='SprayPool':
  base.data.materials.clear();base.data.materials.append(WATER);base['keepSeparate']=True
 if kind in ('GroveSeep','SprayPool'):
  # Rim stays inside this native water cell, never occupies adjacent dry ground.
  for j in range(7):
   a=j*math.tau/7+v*.3;rock_piece(rng,(math.cos(a)*.38,math.sin(a)*.38,.08),(.23,.18,.16),'wet_rock')
 if kind in ('SprayPool','BrinePool','TarSeep'):
  for j in range(2):
   pts=[]
   for q in range(8):
    a=.3+q*.18;pts.append((math.cos(a)*(.19+j*.11)-.09,math.sin(a)*(.19+j*.11)-.10,.048))
   stem('Broken_water_curve',pts,.006 if kind!='SprayPool' else .01,'water_glint' if kind!='TarSeep' else 'iron',.004,4)
 elif kind=='PeatBog':
  for j in range(3):leafshape('Peat_leaf',(rng.uniform(-.30,.30),rng.uniform(-.3,.3),.075),.13,.07,rng.random()*6,'leaf_dark',.02)

def ore(v,bone=True):
 rng=random.Random(245+v)
 for j in range(3):
  x=(j-1)*.24;rock_piece(rng,(x,0,.20),(.37,.58,.40),'pinkstone' if bone else 'wet_rock')
  for k in range(2):
   xx=x+(k-.5)*.12
   stem('Pale_woodstone_seam' if bone else 'Ringing_dark_ore',[(xx-.16,-.19,.24),(xx,.04,.39),(xx+.17,.21,.29)],.025 if bone else .035,'bone' if bone else 'iron_light',.018,6)
 moss_cluster(rng,(.25,-.22,.10),.15,3)

def hollow_log(v):
 rng=random.Random(378+v)
 # A real hollow rim, not a black disc stuck on a solid tree.
 ob=torus('Hollow_bark_rim',(-.34,0,.22),.20,.061,'wood_dark',12,5);ob.rotation_euler[1]=math.pi/2
 for j in range(9):
  a=j*math.tau/9
  stem('Broken_log_bark',[(-.37,math.cos(a)*.19,.23+math.sin(a)*.19),(0,math.cos(a)*.21,.23+math.sin(a)*.21),(.40,math.cos(a)*.17,.23+math.sin(a)*.17)],.055,'bark_pale' if j%3==0 else 'wood',.04,6)
 moss_cluster(rng,(0,0,.43),.30,6)
 if v%2:shelf_fungus(rng,(.17,-.15,.24),.15,'ochre')

def props(bp,v):
 rng=random.Random(seed(bp)+v)
 if bp in ('TepuiStone','Floor','Grass'):ground(v,('tepui' if bp=='TepuiStone' else 'forest' if bp=='Grass' else 'loam')+str(v));return
 if bp=='Tree':canopy(v);return
 if bp=='VineWall':canopy(v,wall=True);return
 if bp=='Bush':canopy(v,bush=True);return
 if bp in ('Rock','TepuiWall','GrainRidge','DescentLedge','SinkholeLip'):stone_cluster(v,bp);return
 if bp=='Wall':wall(v);return
 if bp in ('MycelialColumn','FruitingBody','MushroomRing'):mushroom_group(v,bp);return
 if bp in ('CompostRow','CompostCache'):compost(v,bp=='CompostCache');return
 if bp in ('GroveRedGrowth','WineLeafSundew'):redgrowth(v,bp=='WineLeafSundew');return
 if bp in ('WaterPuddle','GroveSeep','SprayPool','PeatBog','TarSeep','BrinePool'):pool(v,bp);return
 if bp in ('TepuiboneVein','ChoirIronVein','OreCache'):ore(v,bp=='TepuiboneVein');return
 if bp=='GlowQuartzVein':
  rock_piece(rng,(0,0,.22),(.80,.71,.46),'wet_rock')
  for j in range(5):
   x=rng.uniform(-.24,.24);y=rng.uniform(-.22,.22);h=rng.uniform(.20,.40)
   cylinder('Light_hoarding_quartz',(x,y,.28+h*.5),.065,h,'pale_cyan',6,radius2=.05)
   cylinder('Quartz_point',(x,y,.28+h+.05),.05,.10,'dew',6,radius2=0)
  return
 if bp=='HollowLog':hollow_log(v);return
 if bp in ('Crate','Chest','StrongBox','StoneCoffer'):
  if bp=='Crate':crate(v)
  elif bp=='StoneCoffer':
   rock_piece(rng,(0,0,.29),(.73,.47,.56),'stone_dark');rock_piece(rng,(0,0,.60),(.78,.50,.14),'stone_light');box('Coffer_latch',(0,-.25,.48),(.12,.06,.16),'iron',.02)
  else:
   box('Chest_body',(0,0,.25),(.72,.49,.46),'wood',.055);box('Chest_lid',(0,0,.49),(.75,.51,.16),'wood_light',.06)
   for x in [-.24,.24]:box('Iron_band',(x,0,.585),(.055,.52,.035),'iron',.009)
   box('Chest_latch',(0,-.27,.42),(.12,.06,.18),'iron_light',.015)
  return
 if bp in ('WoodenBarrel','HaulBarrel'):barrel(v);return
 if bp in ('WovenBasket','Sack'):
  if bp=='Sack':
   ellipsoid('Cloth_sack',(0,0,.23),(.27,.24,.29),'cloth_old',2);torus('Sack_tie',(0,0,.46),.08,.018,'rope',10,4)
  else:
   cylinder('Basket_wall',(0,0,.22),.27,.42,'wood',12,radius2=.34)
   for z in [.07,.15,.23,.31,.40]:torus('Woven_rim',(0,0,z),.27+z*.16,.012,'rope',12,4)
   cylinder('Basket_shadow',(0,0,.424),.29,.013,'wood_dark',12)
   for j in range(3):ellipsoid('Basket_contents',(rng.uniform(-.15,.15),rng.uniform(-.15,.15),.45),(.1,.1,.09),'cloth_old',1)
  return
 if bp in ('StairsDown','StairsUp'):
  # Exactly one cell. No invented long runs or guessed entrance tunnels.
  for j in range(4):box('Stone_step',(0,(j-1.5)*.22,.06+j*.055),(.80,.24,.13),'stone_light',.025)
  for x in [-.43,.43]:rock_piece(rng,(x,0,.20),(.13,.94,.39),'stone_dark')
  return
 if bp=='GroveSign':
  stem('Grown_sign_stem',[(0,0,0),(.04,0,.4),(0,0,.75)],.055,'wood',.04,7);box('Grown_sign_board',(0,0,.73),(.66,.13,.30),'wood_light',.05)
  for j in range(4):box('Raised_sign_grain',(rng.uniform(-.12,.10),-.07,.79-j*.045),(.35,.01,.008),'wood_dark',0)
  leafshape('Sign_leaf',(.30,0,.66),.18,.10,.6,'leaf',.025);return
 if bp=='CopperPipe':
  beam('Copper_pipe',(-.47,0,.10),(.47,0,.10),.07,'copper',10)
  for x in [-.32,.0,.32]:
   o=torus('Pipe_joint',(x,0,.10),.077,.013,'iron',10,4);o.rotation_euler[1]=math.pi/2
  return
 if bp=='SteamVent':
  torus('Stone_vent',(0,0,.13),.20,.09,'stone_dark',12,5);cylinder('Vent_depth',(0,0,.06),.18,.07,'face_shadow',12)
  # Wisps are opaque subtle curls, not a giant preview-only alpha cloud.
  for j in range(2):stem('Steam_wisp',[(.04*j,0,.18),(-.07,.025,.43),(.04,.01,.63),(-.01,.03,.79)],.015,'dew',.004,5)
  return
 if bp=='AshBed':
  for j in range(8):rock_piece(rng,(rng.uniform(-.36,.36),rng.uniform(-.33,.33),.028),(.19,.16,.047),'ash')
  for j in range(2):beam('Charcoal',(-.25+j*.18,-.15,.07),(.22+j*.1,.18,.06),.025,'black',6)
  return
 if bp=='Campfire':
  for j in range(7):a=j*math.tau/7;rock_piece(rng,(math.cos(a)*.30,math.sin(a)*.30,.09),(.17,.15,.15),'stone_dark')
  beam('Charred_log',(-.24,-.1,.11),(.24,.1,.13),.05,'wood_dark',6);beam('Charred_log',(-.15,.2,.13),(.15,-.2,.14),.05,'wood_dark',6)
  for j in range(3):ellipsoid('Ember',(rng.uniform(-.1,.1),rng.uniform(-.1,.1),.20),(.07,.055,.13),'ember',1)
  return
 if bp=='DryBrush':
  for j in range(6):a=j*math.tau/6;stem('Dry_twig',[(0,0,0),(math.cos(a)*.12,math.sin(a)*.12,.25),(math.cos(a)*.33,math.sin(a)*.33,.39)],.02,'wood_light',.007,5)
  return
 if bp=='FallenBeam':
  box('Fallen_beam',(0,0,.13),(.89,.24,.25),'wood',.025,v*.11)
  for j in range(3):box('Beam_grain',(0,(j-1)*.06,.26),(.72,.009,.007),'wood_dark',0)
  return
 if bp=='MillStone':
  cylinder('Millstone',(0,0,.20),.42,.38,'stone',16);torus('Millstone_grain',(0,0,.40),.29,.012,'stone_dark',16,4);cylinder('Centre_hole',(0,0,.405),.08,.014,'black',10);return
 if bp in ('FellingBarePosition','SeventhPosition'):
  # Low ring of worn earth; centre unoccupied/empty. No plants or occupant.
  torus('Bare_earth_edge',(0,0,.016),.38,.022,'tile_earth',24,4)
  if bp=='SeventhPosition':torus('Quiet_inner_scar',(0,0,.013),.29,.009,'loam0',24,4)
  return
 if bp=='Tepuibone':
  softstone('Loose_tepuibone',(0,0,.065),(.26,.12,.12),'bone',rng,.15);return
 raise ValueError('No real geometry author for '+bp)

# Every nonhuman receives a real armature and nonempty in-place action tracks.
def rig_parts(mid,bone_defs,weights,family):
 coll=MODELS[mid]['collection'];arm=bpy.data.armatures.new(mid+'_Skeleton');rig=bpy.data.objects.new(mid+'__Rig',arm);SCENE.collection.objects.link(rig)
 bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for name,head,tail,parent in bone_defs:
  b=arm.edit_bones.new(name);b.head=head;b.tail=tail
  if parent:b.parent=arm.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
 for c in list(rig.users_collection):c.objects.unlink(rig)
 coll.objects.link(rig)
 for ob in list(coll.objects):
  if ob.type!='MESH':continue
  bone=weights.get(ob.name,'Body')
  if isinstance(bone,dict):
   for name,indices in bone.items():vg=ob.vertex_groups.new(name=name);vg.add(indices,1,'REPLACE')
  else:vg=ob.vertex_groups.new(name=bone);vg.add(list(range(len(ob.data.vertices))),1,'REPLACE')
  mod=ob.modifiers.new('SpawnRingRig','ARMATURE');mod.object=rig;ob.parent=rig
 rig.animation_data_create()
 for clip in ['Idle','Walk','Interact','Attack','Hit']:
  action=bpy.data.actions.new(mid+'__'+clip);rig.animation_data.action=action;length=48 if clip=='Idle' else 24
  for frame in [0,6,12,18,24] if length==24 else [0,12,24,36,48]:
   phase=frame/length*math.tau;one=math.sin(frame/length*math.pi)
   for i,p in enumerate(rig.pose.bones):
    p.rotation_mode='XYZ';p.rotation_euler=(0,0,0);p.location=(0,0,0);p.scale=(1,1,1)
    if p.name=='Root':continue
    if clip=='Idle':
     if p.name=='Body':p.scale=(1+.014*math.sin(phase),1+.01*math.sin(phase),1+.025*math.sin(phase))
     else:p.rotation_euler[1]=.02*math.sin(phase+i*.4)
    elif clip=='Walk':
     if family=='rooted':p.rotation_euler[1]=.08*math.sin(phase+i*.7)
     elif family=='serpent':p.rotation_euler[2]=.12*math.sin(phase-i*.7)
     elif family=='avian' and 'Wing' in p.name:p.rotation_euler[1]=(.38 if '.L' in p.name else -.38)*math.sin(phase)
     elif p.name.startswith('Leg'):p.rotation_euler[0]=.28*math.sin(phase+(math.pi if '.L' in p.name else 0)+(math.pi if 'Rear' in p.name else 0))
     elif p.name=='Body' and family=='frog':p.location.z=.06*max(0,math.sin(phase))
     elif p.name=='Body':p.rotation_euler[2]=.035*math.sin(phase)
    elif clip=='Interact':
     if p.name in ('Head','Body'):p.rotation_euler[0]=-.12*one
     elif family=='rooted':p.rotation_euler[1]=.12*one
    elif clip=='Attack':
     if p.name=='Head':p.rotation_euler[0]=-.32*one
     elif p.name=='Body':p.rotation_euler[0]=-.17*one
     elif family=='avian' and 'Wing' in p.name:p.rotation_euler[1]=(.5 if '.L' in p.name else -.5)*one
     elif family in ('rooted','serpent'):p.rotation_euler[2]=.25*math.sin(phase-i*.4)
     elif 'Arm' in p.name:p.rotation_euler[0]=-.75*one
    elif clip=='Hit':p.rotation_euler[0]=.16*one;p.rotation_euler[2]=.07*one
    p.keyframe_insert(data_path='rotation_euler',frame=frame,group=p.name);p.keyframe_insert(data_path='location',frame=frame,group=p.name);p.keyframe_insert(data_path='scale',frame=frame,group=p.name)
  track=rig.animation_data.nla_tracks.new();track.name=clip;strip=track.strips.new(clip,0,action);strip.name=clip;track.mute=True
 rig.animation_data.action=None
 for p in rig.pose.bones:p.rotation_euler=(0,0,0);p.location=(0,0,0);p.scale=(1,1,1)
 MODELS[mid].update(rigged=True,clips=['Idle','Walk','Interact','Attack','Hit'],sockets=[],rigFamily=family)

def creature(mid,bp,family):
 rng=random.Random(seed(bp));coll=model(mid,'actor','feet-root');weights={}
 def bind(ob,bone='Body'):weights[ob.name]=bone;return ob
 bones=[('Root',(0,0,0),(0,0,.1),None),('Body',(0,0,.1),(0,0,.6),'Root')]
 if family=='frog':
  maw=bp=='MawToad';helm=bp=='HelmwoodFrog';scale=.92 if maw else .65 if helm else .46 if bp=='GlasspaneFrog' else .41
  bodycol='frog_dark' if maw else 'frog_green';rx=scale*.48;ry=scale*.52
  bind(ellipsoid('Frog_body',(0,.03,.16),(rx,ry,.18 if maw else .14),bodycol,2))
  bind(ellipsoid('Pale_belly',(0,0,.075),(rx*.83,ry*.9,.08),'glass_belly' if bp=='GlasspaneFrog' else 'frog_light',2))
  bones.append(('Head',(0,-.13,.15),(0,-.37,.15),'Body'))
  bind(ellipsoid('Broad_frog_head',(0,-ry*.6,.17),(rx*.97,ry*.65,.13),'bark_pale' if helm else bodycol,2),'Head')
  if helm:
   bind(softstone('Bony_wooden_casque',(0,-.18,.28),(.54,.36,.12),'bark_ridge',rng,0),'Head')
   for x in [-.11,.11]:bind(beam('Casque_grain',(x,-.33,.35),(x*.6,-.03,.34),.013,'bone_shadow',5),'Head')
   bind(ellipsoid('Jaw_water_bead',(0,-.34,.09),(.035,.035,.044),'dew',1),'Head')
  if maw:
   bind(ellipsoid('Vast_dark_mouth',(0,-.27,.35),(.31,.19,.055),'face_shadow',2),'Head')
   for x in [-.21,-.12,0,.12,.21]:bind(cylinder('Maw_tooth',(x,-.34,.405),.018,.048,'bone',6,radius2=.003),'Head')
  for sx,side in [(-1,'L'),(1,'R')]:
   for rear in [False,True]:
    yy=ry*.65 if rear else -ry*.35;name='Leg'+('Rear' if rear else 'Front')+'.'+side
    bones.append((name,(sx*rx*.5,yy,.1),(sx*(rx+.16),yy-.08,.05),'Body'))
    bind(ellipsoid('Bent_frog_thigh',(sx*rx,yy,.1),(.13 if rear else .075,.12,.08),bodycol,1),name)
    for j in range(3):bind(beam('Webbed_toe',(sx*(rx+.07),yy-.06,.045),(sx*(rx+.14+j*.035),yy-.17+j*.045,.035),.018,'frog_light',5),name)
   bind(ellipsoid('Eye_bump',(sx*rx*.72,-ry*.84,.29),(.065,.065,.066),'frog_light',1),'Head')
   bind(ellipsoid('Black_eye',(sx*rx*.72,-ry*.86,.326),(.032,.034,.028),'black',2),'Head')
   bind(ellipsoid('Eye_glint',(sx*rx*.72-.009,-ry*.86-.008,.35),(.009,.01,.009),'cream',1),'Head')
  if bp=='CascadeFather':
   for sx in [-1,1]:bind(beam('Pale_back_stripe',(sx*.085,-.11,.275),(sx*.10,.19,.275),.018,'frog_stripe',6))
   for j in range(5):a=j*2.4;bind(ellipsoid('Back_borne_young',(math.cos(a)*.09,math.sin(a)*.09,.29),(.027,.037,.026),'frog_dark',1))
  if bp=='GlasspaneFrog':bind(ellipsoid('Heart_beneath_clear_belly',(0,-.03,.022),(.055,.045,.035),'heart',1))
 elif family=='tortoise':
  bind(ellipsoid('Domed_shell',(0,.03,.27),(.35,.42,.28),'tortoise_shell',2));bind(torus('Shell_rim',(0,.02,.16),.36,.038,'tortoise_edge',16,5))
  for j in range(7):
   a=j*2.399;r=.21 if j else 0;bind(softstone('Shell_scute',(math.cos(a)*r,math.sin(a)*r,.45 if j else .54),(.20,.22,.045),'tortoise_edge' if j%3==0 else 'wood',rng,a))
  bones.append(('Head',(0,-.28,.16),(0,-.54,.14),'Body'));bind(ellipsoid('Tortoise_head',(0,-.45,.17),(.11,.15,.11),'yellowfoot',2),'Head')
  for x in [-.07,.07]:bind(ellipsoid('Eye',(x,-.52,.22),(.023,.021,.019),'black',1),'Head')
  for s,side in [(-1,'L'),(1,'R')]:
   for yy,tag in [(-.25,'Front'),(.28,'Rear')]:
    n='Leg'+tag+'.'+side;bones.append((n,(s*.23,yy,.14),(s*.41,yy-.04,.05),'Body'));bind(ellipsoid('Yellow_foot',(s*.32,yy,.085),(.11,.12,.075),'yellowfoot',1),n)
 elif family=='serpent':
  heavy=bp=='SariSnake';points=[]
  for i in range(10):
   if heavy:a=i*.54;points.append((math.cos(a)*(.3-i*.007),math.sin(a)*(.3-i*.007),.105+i*.004))
   else:points.append((math.sin(i*.67)*.19,-.46+i*.102,.095))
  for i,p in enumerate(points):
   n='Coil.'+str(i);tail=Vector(p)+Vector((.001,.06,.03));bones.append((n,p,tuple(tail),'Body'))
   if heavy:bind(leafshape('Scale_diamond',(p[0],p[1],p[2]+.090),.15,.11,i*.6,'snake_scale',.013),n)
  # Continuous weighted skin, not a chain of disconnected beads.
  vs=[];fs=[];wg={};sections=10
  for i,p in enumerate(points):
   tangent=(Vector(points[min(i+1,9)])-Vector(points[max(0,i-1)])).normalized();side=tangent.cross(Vector((0,0,1))).normalized();radius=(.112 if heavy else .066)*(1-.58*(i/9)**4)
   ids=[]
   for j in range(sections):
    a=j*math.tau/sections;pos=Vector(p)+side*math.cos(a)*radius+Vector((0,0,math.sin(a)*radius*.82));ids.append(len(vs));vs.append(tuple(pos))
   wg['Coil.'+str(i)]=ids
  for i in range(9):
   for j in range(sections):fs.append((i*sections+j,i*sections+(j+1)%sections,(i+1)*sections+(j+1)%sections,(i+1)*sections+j))
  fs.extend([tuple(reversed(range(sections))),tuple(9*sections+j for j in range(sections))]);skin=newmesh('Continuous_serpent_skin',vs,fs,'snake_stone' if heavy else 'snake_olive',True);weights[skin.name]=wg
  p=points[0];bones.append(('Head',p,(p[0],p[1]-.17,p[2]),'Body'));bind(ellipsoid('Broad_viper_head' if heavy else 'Slim_colubrid_head',(p[0],p[1]-.1,p[2]+.01),(.14 if heavy else .07,.15 if heavy else .11,.08 if heavy else .05),'snake_stone' if heavy else 'snake_olive',2),'Head')
  for sx in [-1,1]:bind(ellipsoid('Snake_eye',(p[0]+sx*(.09 if heavy else .045),p[1]-.13,p[2]+.066),(.018,.023,.014),'gold',1),'Head')
 elif family=='avian':
  bind(ellipsoid('Eagle_body',(0,0,.46),(.19,.34,.19),'eagle_ash',2));bones.append(('Head',(0,-.26,.48),(0,-.46,.49),'Body'))
  bind(ellipsoid('Eagle_head',(0,-.36,.54),(.135,.16,.13),'eagle_ash',2),'Head');bind(ellipsoid('Hooked_beak',(0,-.50,.52),(.058,.115,.07),'eagle_beak',1),'Head')
  for sx in [-1,1]:bind(ellipsoid('Eagle_eye',(sx*.10,-.40,.615),(.026,.025,.022),'black',1),'Head')
  for s,side in [(-1,'L'),(1,'R')]:
   n='Wing.'+side;bones.append((n,(s*.15,0,.45),(s*.69,.1,.47),'Body'))
   bind(ellipsoid('Wing_dark_underlayer',(s*.44,.04,.45),(.42,.29,.055),'eagle_dark',2),n)
   for j in range(7):bind(leafshape('Ash_flight_feather',(s*(.29+j*.082),.12+j*.019,.53),.59-j*.021,.15,s*.33,'eagle_ash' if j%3 else 'eagle_dark',.035),n)
   for j in range(3):bind(beam('Closing_talon',(s*.10,-.04,.33),(s*(.11+(j-1)*.035),-.13,.25),.017,'eagle_beak',6))
  for j in range(4):bind(leafshape('Tail_feather',((j-1.5)*.07,.38,.46),.40,.12,math.pi/2,'eagle_dark' if j%2 else 'eagle_ash',.025))
 elif family=='fungal':
  h=1.38 if bp=='Mosshulk' else 1.05 if bp=='Shambler' else .53;r=.35 if bp=='Mosshulk' else .23 if bp=='Shambler' else .21
  bind(ellipsoid('Fungal_torso',(0,0,h*.57),(r,r*.77,h*.31),'leaf_forest' if bp=='Mosshulk' else 'fungal_dark',2))
  bones.append(('Head',(0,0,h*.82),(0,0,h*1.04),'Body'))
  bind(ellipsoid('Fungal_head',(0,-.015,h*.90),(r*.76,r*.72,h*.14),'fungal_violet' if bp=='Shambler' else 'ochre',2),'Head')
  for s,side in [(-1,'L'),(1,'R')]:
   bones += [('Arm.'+side,(s*r*.7,0,h*.75),(s*r*1.38,0,h*.35),'Body'),('Leg.'+side,(s*r*.45,0,h*.32),(s*r*.48,-.04,.06),'Body')]
   bind(beam('Fungal_arm',(s*r*.80,0,h*.70),(s*r*1.32,-.02,h*.34),r*.25,'fungal_dark',8),'Arm.'+side)
   bind(ellipsoid('Rooted_fist',(s*r*1.32,-.02,h*.32),(r*.33,r*.27,r*.35),'moss',1),'Arm.'+side)
   bind(beam('Fungal_leg',(s*r*.45,0,h*.32),(s*r*.48,-.03,.10),r*.29,'bark_shadow',8),'Leg.'+side)
   bind(ellipsoid('Fungal_foot',(s*r*.48,-.09,.075),(r*.36,r*.55,.085),'wood_dark',1),'Leg.'+side)
  for j in range(7 if bp=='Mosshulk' else 4):
   x=rng.uniform(-r,r);y=rng.uniform(-r*.5,r*.5);z=h*rng.uniform(.62,1.03)
   cap=shelf_fungus(rng,(x,y,z),r*.43,'leaf_olive' if bp=='Mosshulk' else 'fungal_pale')
   # shelf_fungus creates a second margin object; bind every unbound decoration.
   for ob in coll.objects:
    if ob.type=='MESH' and ob.name not in weights:weights[ob.name]='Head' if z>h*.82 else 'Body'
  if bp=='Mosshulk':
   for j in range(9):bind(leafshape('Moss_shag',(rng.uniform(-r,r),rng.uniform(-r*.6,r*.6),h*.84),.17,.11,rng.random()*6,'leaf_olive',.03),'Body')
 elif family=='rooted':
  bind(ellipsoid('Rooted_choir_base',(0,0,.12),(.34,.33,.14),'fungal_dark',2))
  for i in range(5):
   a=i*math.tau/5;n='Tendril.'+str(i);p=(math.cos(a)*.15,math.sin(a)*.15,.12);q=(math.cos(a)*.30,math.sin(a)*.30,.87)
   bones.append((n,p,q,'Body'));bind(stem('Listening_tendril',[p,(math.cos(a+.3)*.22,math.sin(a+.3)*.22,.45),q],.085,'fungal_pale',.036,8),n)
   bind(ellipsoid('Choir_spore_tip',(q[0],q[1],q[2]+.06),(.11,.105,.14),'fungal_violet',2),n)
  for a in [0,1.5,3,4.5]:bind(stem('Rooted_filament',[(math.cos(a)*.43,math.sin(a)*.43,.025),(.16*math.cos(a),.16*math.sin(a),.08),(0,0,.18)],.032,'fungal_pale',.018,6))
 else:raise ValueError(family)
 rig_parts(mid,bones,weights,family)
# Snapshot-derived Felling hero segments retain exact owner anchors and native solid mask.
FZ=read_native('Overworld.3.5.0');FENT={e['token']:e for e in FZ['entities']}
def public_fields(e,p):return {f['name']:f['value'].get('text') for part in e['parts'] if part['name']==p for f in part['fields']}
FSOLID=set()
for c in FZ['cells']:
 if any(e['blueprint']=='TepuiStone' and public_fields(e,'Physics').get('Solid')=='True' for e in [FENT[t] for t in c['entityTokens']]):FSOLID.add((c['x'],c['y']))
HEROS=[l for l in FELLING['layers'] if not l['mutable'] and (l['kind']=='petrified-trunk' or 'root' in l['kind'] or l['kind']=='foreground-rock-structure')]
PARTITION={l['id']:[] for l in HEROS}
for x,y in FSOLID:
 if y<5:owner=next(l for l in HEROS if l['id']=='stump-main')
 else:owner=min(HEROS,key=lambda l:(x-l['anchorX'])**2+(y-l['anchorY'])**2)
 PARTITION[owner['id']].append((x,y))
# Continuous sculpted heightfield constrained to the exact native solid union.
BOUNDARY=[]
for x,y in FSOLID:
 for dx,dy,a,b in [(-1,0,(x,y),(x,y+1)),(1,0,(x+1,y),(x+1,y+1)),(0,-1,(x,y),(x+1,y)),(0,1,(x,y+1),(x+1,y+1))]:
  if (x+dx,y+dy) not in FSOLID:BOUNDARY.append((a,b))
def edge_distance(x,y):
 best=1e9
 for (ax,ay),(bx,by) in BOUNDARY:
  px=max(min(x,max(ax,bx)),min(ax,bx));py=max(min(y,max(ay,by)),min(ay,by));best=min(best,(px-x)**2+(py-y)**2)
 return math.sqrt(best)
HEIGHT_CACHE={}
def root_height(x,y):
 key=(round(x,4),round(y,4))
 if key in HEIGHT_CACHE:return HEIGHT_CACHE[key]
 d=edge_distance(x,y)
 # Radial buttress grain broadens organically from the stump, with rounded edges.
 t=math.atan2(x-40,y+10);grain=t*17+.16*math.sin(y*.63)+.10*math.sin(x*1.32)
 broad=.83+.12*math.sin(x*.52+y*.21)+.08*math.sin(x*.91-y*.34)
 height=.025+(min(d,3.4)**.54)*(1.65*broad+.40*math.cos(grain)+.095*math.sin(grain*2.4))
 HEIGHT_CACHE[key]=height;return height
def solidheight(x,y):return root_height(x+.5,y+.5)
def felling_hero(layer):
 rng=random.Random(seed(layer['id']));cells=PARTITION[layer['id']];cx,cy=layer['anchorX']+.5,24.5-layer['anchorY']
 if not cells:
  stone_cluster(seed(layer['id'])%4,'TepuiWall');return
 verts=[];faces=[];lookup={}
 def vertex(x,y,cellx,celly):
  origx,origy=x,y;fac=1.0;radius=.42
  for sx,sy in [(-1,-1),(1,-1),(1,1),(-1,1)]:
   if (cellx+sx,celly) in FSOLID or (cellx,celly+sy) in FSOLID:continue
   ex=cellx+(1 if sx>0 else 0);ey=celly+(1 if sy>0 else 0);ccx=ex-sx*radius;ccy=ey-sy*radius
   if (x-ccx)*sx>0 and (y-ccy)*sy>0:
    dx=x-ccx;dy=y-ccy;dist=math.sqrt(dx*dx+dy*dy)
    if dist>radius:x=ccx+dx/dist*radius;y=ccy+dy/dist*radius;fac=0
    else:fac=min(fac,min(1,(radius-dist)/.15))
  key=(round(x,4),round(y,4),round(fac,3))
  if key not in lookup:lookup[key]=len(verts);verts.append((x-cx,25-y-cy,.025+(root_height(x,y)-.025)*fac))
  return lookup[key]
 # Four subdivisions per native cell remove stairlike elevation changes without
 # inventing walkable/blocked geometry outside the existing native mask.
 for x,y in cells:
  for a in range(4):
   for b in range(4):
    xx=x+a*.25;yy=y+b*.25
    faces.append(tuple(reversed([vertex(xx,yy,x,y),vertex(xx+.25,yy,x,y),vertex(xx+.25,yy+.25,x,y),vertex(xx,yy+.25,x,y)])))
 ob=newmesh('Sculpted_petrified_root_mass',verts,faces,'pinkstone',True)
 # Fewer broad, elongated ridges flow with the continuous root surface.
 # Existing triangulated topology and exact native-mask clipping are retained.
 for x,y in cells:
  for j in range(1):
   xx=x+rng.uniform(.15,.85);yy=y+rng.uniform(.15,.85);d=edge_distance(xx,yy)
   if d<.19:continue
   angle=math.atan2(-(yy+10),xx-40);length=rng.uniform(1.65,2.55);width=rng.uniform(.44,.78)
   if d<.4:width*=.75;length*=.8
   tx=math.cos(angle);ty=math.sin(angle);sx=-ty;sy=tx
   outline=[(-.50,-.04),(-.46,.19),(-.29,.35),(-.03,.47),(.23,.39),(.43,.20),(.50,.01),(.46,-.19),(.26,-.35),(-.03,-.43),(-.30,-.36),(-.46,-.21)]
   points_per_ring=len(outline)
   # Uniform shrink preserves the convex outline. Per-vertex clamping bends
   # the cap perimeter and can make its dominant projection self-intersect.
   footprint=[(tx*ox*length+sx*oy*width,-ty*ox*length-sy*oy*width) for ox,oy in outline]
   shrink=1.0
   def inside_footprint(scale):
    points=[(xx+px*scale,yy+py*scale) for px,py in footprint]
    # Exact polygon clipping checks edges AND interior against every dry/empty
    # native cell intersecting the small plate's bounding box.
    for cellx in range(math.floor(min(p[0] for p in points)),math.floor(max(p[0] for p in points))+1):
     for celly in range(math.floor(min(p[1] for p in points)),math.floor(max(p[1] for p in points))+1):
      if (cellx,celly) in FSOLID:continue
      poly=points[:]
      for axis,limit,sign in [(0,cellx,1),(0,cellx+1,-1),(1,celly,1),(1,celly+1,-1)]:
       clipped=[]
       for i,p in enumerate(poly):
        q=poly[(i+1)%len(poly)];dp=(p[axis]-limit)*sign;dq=(q[axis]-limit)*sign
        if dp>=0:clipped.append(p)
        if (dp>=0)!=(dq>=0):
         t=dp/(dp-dq);clipped.append((p[0]+(q[0]-p[0])*t,p[1]+(q[1]-p[1])*t))
       poly=clipped
       if not poly:break
      if len(poly)>2:
       area=abs(sum(poly[i][0]*poly[(i+1)%len(poly)][1]-poly[(i+1)%len(poly)][0]*poly[i][1] for i in range(len(poly))))*.5
       if area>1e-9:return False
    return True
   while not inside_footprint(shrink):shrink*=.85
   platev=[]
   for lift,scale in [(0,1),(.115,.82),(.17,.48)]:
    for px,py in footprint:
     gx=xx+px*scale*shrink;gy=yy+py*scale*shrink
     platev.append((gx-cx,25-gy-cy,root_height(gx,gy)+lift+.005))
   # The curved cap is explicitly triangulated; an importer never has to
   # project a nonplanar polygon on the root's heightfield.
   centre=len(platev);platev.append((xx-cx,25-yy-cy,root_height(xx,yy)+.205))
   platef=[(centre,2*points_per_ring+(k+1)%points_per_ring,2*points_per_ring+k) for k in range(points_per_ring)]
   for ring in range(2):
    for k in range(points_per_ring):
     a=(ring+1)*points_per_ring+k;b=(ring+1)*points_per_ring+(k+1)%points_per_ring;c=ring*points_per_ring+(k+1)%points_per_ring;d=ring*points_per_ring+k
     platef.extend([(a,b,c),(a,c,d)])
   ridge=newmesh('Flowing_petrified_bark_ridge',platev,platef,'root_bark',True)
   # Align grain with the ridge's native growth axis instead of global mesh X/Y.
   idx=CINDEX['root_bark'];uv=ridge.data.uv_layers[0]
   for loop in ridge.data.loops:
    vi=loop.vertex_index
    if vi==centre:a=b=.5
    else:
     ring=vi//points_per_ring;ox,oy=outline[vi%points_per_ring];scale=[1,.82,.48][ring]
     a=.5+ox*scale;b=.5+oy*scale
    uv.data[loop.index].uv=((idx%16+.025+.95*a)/16,(idx//16+.025+.95*b)/8)
  patch=math.sin(x*.43+y*.35)+math.sin(x*.91-y*.47)
  if patch>.50 and edge_distance(x+.5,y+.5)>.21:
   for k in range(3):
    xx=x+rng.uniform(.27,.73);yy=y+rng.uniform(.27,.73);zz=root_height(xx,yy)+.08
    leafshape('Moss_in_root_fissure',(xx-cx,25-yy-cy,zz),rng.uniform(.12,.24),rng.uniform(.10,.19),rng.random()*6,rng.choice(['moss','leaf_olive','bark_moss']),.022)
  if seed('fungus'+str(x)+':'+str(y))%17==0 and edge_distance(x+.5,y+.5)>.25:
   zz=root_height(x+.5,y+.5);shelf_fungus(rng,(x+.5-cx,24.5-y-cy,zz+.10),.20,'ochre')

def felling_component(layer):
 rng=random.Random(seed(layer['id']));kind=layer['kind'];v=seed(layer['id'])%4
 if layer['id'] in PARTITION:felling_hero(layer);return
 if 'boulder' in kind or 'rock' in kind or 'stone' in kind:stone_cluster(v,'Rock');return
 if 'red-branch' in kind:
  for i in range(4):
   a=i*math.tau/4;stem('Red_coral_stem',[(0,0,0),(.10*math.cos(a),.10*math.sin(a),.27),(.24*math.cos(a),.24*math.sin(a),.60)],.035,'petal_red',.012,7)
   for s in [-1,1]:stem('Coral_fork',[(.08*math.cos(a),.08*math.sin(a),.22),(.29*math.cos(a+s*.5),.29*math.sin(a+s*.5),.40),(.35*math.cos(a+s*.5),.35*math.sin(a+s*.5),.56)],.019,'petal_red',.005,6)
 elif 'cyan' in kind:
  for i in range(6):
   a=i*2.4;r=.22;h=rng.uniform(.28,.60);x=math.cos(a)*r;y=math.sin(a)*r
   stem('Cyan_living_stem',[(x,y,.015),(x*.9,y*.9,h)],.024,'pale_cyan',.018,7);ellipsoid('Cyan_bulb',(x*.9,y*.9,h),(.065,.062,.09),'petal_blue',2)
 elif 'pink' in kind:
  for i in range(5):
   a=i*2.4;x=math.cos(a)*.22;y=math.sin(a)*.22;h=.18+(i%3)*.12
   cylinder('Pink_stalk',(x,y,h*.5),.03,h,'fungal_pale',7);ellipsoid('Pink_cap',(x,y,h),(.13,.13,.10),'flower_pink' if i%2 else 'petal_violet',2)
 elif 'blue-leaf' in kind:
  for i in range(9):a=i*math.tau/9;leafshape('Blue_rosette',(math.cos(a)*.17,math.sin(a)*.17,.05),.48,.14,a,'petal_blue',.06)
 elif kind=='river-water':
  # Native water exists independently. This component only owns tiny current cues.
  for j in range(3):stem('Native_river_current',[(-.32+j*.18,-.32,.066),(-.25+j*.18,-.04,.066),(-.32+j*.18,.22,.066)],.008,'water_glint',.003,5)
 elif kind=='waterfall':
  for j in range(4):
   x=(j-1.5)*.15;stem('Waterfall_ribbon',[(x,.35,1.1),(x,.1,.85),(x,-.20,.27),(x,-.30,.04)],.035,'pale_cyan',.019,7)
 elif kind=='mist':
  for j in range(3):
   pts=[(math.cos(a)*(.19+j*.08),math.sin(a)*(.19+j*.08),.10+j*.13) for a in [i*.30 for i in range(9)]];stem('Low_mist_curve',pts,.011,'dew',.004,5)
 else:
  count=3 if 'spire' in kind else 4
  for i in range(count):
   a=i*2.4;x=math.cos(a)*.20;y=math.sin(a)*.20;h=rng.uniform(.45,.85) if 'spire' in kind else rng.uniform(.27,.58)
   stem('Pale_petrified_tube',[(x,y,0),(x*.88,y*.88,h)],.092,'fungal_pale',.075,8);torus('Hollow_tube_rim',(x*.88,y*.88,h),.073,.018,'bark_ridge',10,4);cylinder('Tube_hollow',(x*.88,y*.88,h-.008),.056,.010,'bark_shadow',10)
 moss_cluster(rng,(.13,-.17,.025),.23,3)

# Build exact agreed catalog IDs. Models are genuine reusable source collections.
rows_by={row['id']:row for row in CONTRACT['models']}
for mapping in CONTRACT['blueprints']:
 bp=mapping['blueprint']
 for i,mid in enumerate(mapping['models']):
  row=rows_by[mid];family=row['rigFamily']
  if family=='humanoid':
   colors={'Player':'hood_teal','CaveHermit':'hood_olive','Mogu':'hood_violet','Grib':'hood_gold','Nam':'hood_teal','Sien':'hood_olive','Sopp':'hood_violet','Snapjaw':'hood_olive','SnapjawWarlord':'hood_gold'}
   character(colors[bp],mid);CURRENT=MODELS[mid]['collection']
   # Named residents have distinct subtle silhouette details, no fake carried gear.
   rig=next(o for o in CURRENT.objects if o.type=='ARMATURE')
   decorations=[]
   if bp in ('Snapjaw','SnapjawWarlord'):
    for ob in list(CURRENT.objects):
     if any(n in ob.name for n in ('Hood','Face')):bpy.data.objects.remove(ob,do_unlink=True)
    decorations.append((ellipsoid('Scaled_canine_head',(0,-.01,1.34),(.25,.27,.31),'wood',2),'Head'))
    decorations.append((ellipsoid('Long_snapjaw_muzzle',(0,-.29,1.29),(.17,.23,.14),'skin',2),'Head'))
    for sx in [-1,1]:
     decorations.append((ellipsoid('Canine_ear',(sx*.19,.03,1.58),(.09,.10,.21),'wood_dark',1),'Head'))
     decorations.append((ellipsoid('Snapjaw_eye',(sx*.18,-.20,1.45),(.028,.032,.027),'gold',1),'Head'))
   elif bp=='CaveHermit':
    decorations.append((ellipsoid('Mossy_hood_patch',(.07,.02,1.67),(.24,.22,.075),'leaf_olive',1),'Head'))
    decorations.append((ellipsoid('Old_beard',(0,-.34,1.16),(.15,.10,.20),'cream',2),'Head'))
   elif bp!='Player':
    decorations.append((ellipsoid('Choir_cap',(0,.02,1.67),(.30,.27,.10),'fungal_pale' if bp in ('Mogu','Nam') else 'fungal_violet',2),'Head'))
   for ob,bone in decorations:
    group=ob.vertex_groups.new(name=bone);group.add(list(range(len(ob.data.vertices))),1,'REPLACE');mod=ob.modifiers.new('SpawnRingRig','ARMATURE');mod.object=rig;ob.parent=rig
   MODELS[mid]['rigFamily']='humanoid'
  elif family!='none':creature(mid,bp,family)
  else:model(mid,mapping['role']);props(bp,i)
  MODELS[mid]['sourceBlueprint']=bp
for owner in CONTRACT['fellingOwners']:
 layer=next(l for l in FELLING['layers'] if l['id']==owner['componentId']);model(owner['modelId'],'felling-component');felling_component(layer)
model('ring-water-surface','tile-overlay')
water=box('Current_native_water',(0,0,.004),(1,1,.008),'water',0);water.data.materials.clear();water.data.materials.append(WATER);water['keepSeparate']=True
assert set(MODELS)==set(rows_by),(set(MODELS)-set(rows_by),set(rows_by)-set(MODELS))
# Collections must participate in a dependency update before matrix_world metadata/export.
library=bpy.data.collections.new('ASSET_LIBRARY_EDITABLE');SCENE.collection.children.link(library)
for info in MODELS.values():library.children.link(info['collection'])
bpy.context.view_layer.update()
print('RING_KIT_AUTHORED',len(MODELS),flush=True)

# Metadata derives actual final topology and Unity-oriented bind-space bounds.
def uv3(x,y,z):return {'x':round(x,6),'y':round(z,6),'z':round(y,6)}
model_rows=[]
for mid,info in MODELS.items():
 pts=[];tris=0
 for ob in info['collection'].objects:
  if ob.type!='MESH':continue
  ob.data.calc_loop_triangles();tris+=len(ob.data.loop_triangles);pts.extend(ob.matrix_basis@v.co for v in ob.data.vertices)
 assert pts and tris>0,mid
 mins=[min(p[k] for p in pts) for k in range(3)];maxs=[max(p[k] for p in pts) for k in range(3)]
 center=[(a+b)/2 for a,b in zip(mins,maxs)];size=[b-a for a,b in zip(mins,maxs)]
 row={k:v for k,v in rows_by[mid].items() if k not in ('metadataStatus','sourceBlueprint')}
 row.update(triangles=tris,boundsCenter=uv3(*center),boundsSize=uv3(*size),pivot=info['pivot'],rigged=info.get('rigged',False),clips=info.get('clips',[]),sockets=info.get('sockets',[]))
 model_rows.append(row)
 if not args.skip_export and (not args.export_only or mid in args.export_only.split(',')):export_collection(mid)
print('RING_MODEL_METADATA_READY',len(model_rows),flush=True)
if not args.skip_export:print('RING_MODELS_EXPORTED',sum(not args.export_only or m['id'] in args.export_only.split(',') for m in model_rows),flush=True)
CATALOG={k:v for k,v in CONTRACT.items() if k not in ('status','metadataConvention')};CATALOG['models']=model_rows;CATALOG['artSeed']=SEED
CATALOG['surfacePolicies']=[{'blueprint':b,'policy':'dry-native-bed-and-rim; current TileState water uses shared overlay'} for b in ['WaterPuddle','GroveSeep','PeatBog']]+[{'blueprint':b,'policy':'native-specific hazard surface; never substituted generic water'} for b in ['BrinePool','TarSeep']]+[{'blueprint':'SprayPool','policy':'visual basin water; no claim of native LiquidPool or TileState source'},{'componentId':'blackwater-flow','policy':'owner-ripple-detail only; no river plane; native TileState survives owner removal'}]
CATALOG['sourceSnapshotIndexSha256']=hashlib.sha256(gzip.decompress((SOURCE/'native/ring-index.json.gz').read_bytes())).hexdigest()
(OUT/'catalog.json').write_text(json.dumps(CATALOG,indent=2)+'\n')
# Pack atlas into authoring blends while keeping portable relative exported path.
ATLAS.pack();ATLAS.filepath='//textures/SpawnRingPalette.png'
SCENE.view_layers[0].layer_collection.children[library.name].exclude=True
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ring_kit.blend'),compress=True)
if args.kit_only:sys.exit(0)
# Consolidated shared preview prototypes keep scene draw/object count bounded.
PREVIEW={};previewlib=bpy.data.collections.new('PREVIEW_PROTOTYPES');SCENE.collection.children.link(previewlib)
for mid,info in MODELS.items():
 coll=bpy.data.collections.new(mid+'__Preview');previewlib.children.link(coll)
 for watergroup in (False,True):
  group=[o for o in info['collection'].objects if o.type=='MESH' and bool(o.get('keepSeparate',False))==watergroup]
  if not group:continue
  verts=[];faces=[];uvs=[];smooth=[]
  for o in group:
   off=len(verts);verts.extend(tuple(o.matrix_basis@v.co) for v in o.data.vertices);faces.extend(tuple(off+j for j in p.vertices) for p in o.data.polygons);uvs.extend(tuple(t.uv) for t in o.data.uv_layers[0].data);smooth.extend(p.use_smooth for p in o.data.polygons)
  mesh=bpy.data.meshes.new(mid+'__PreviewMesh');mesh.from_pydata(verts,[],faces);mesh.update();mesh.materials.append(WATER if watergroup else MAT);uv=mesh.uv_layers.new(name='UVMap');uv.data.foreach_set('uv',[x for pair in uvs for x in pair])
  for p,smoothflag in zip(mesh.polygons,smooth):p.use_smooth=smoothflag
  ob=bpy.data.objects.new(mid+('__Water' if watergroup else '__Geometry'),mesh);coll.objects.link(ob)
 PREVIEW[mid]=coll
# Same 16 cardinal-dry masks as the runtime water helper: N1 E2 S4 W8.
WATER_PREVIEW={}
for mask in range(16):
 perimeter=[]
 for sx,sy,start,bits in [(1,1,0,3),(-1,1,90,9),(-1,-1,180,12),(1,-1,270,6)]:
  if mask&bits==bits:
   for j in range(9):
    a=math.radians(start+j*90/8);perimeter.append((sx*.4+math.cos(a)*.1,sy*.4+math.sin(a)*.1,0))
  else:perimeter.append((sx*.5,sy*.5,0))
 mesh=bpy.data.meshes.new('Current_water_mask_'+str(mask));mesh.from_pydata(perimeter,[],[tuple(range(len(perimeter)))]);mesh.update();mesh.materials.append(WATER);mesh.uv_layers.new(name='UVMap')
 wc=bpy.data.collections.new('Current_water_mask_'+str(mask));previewlib.children.link(wc);wo=bpy.data.objects.new('Water_surface',mesh);wc.objects.link(wo);WATER_PREVIEW[mask]=wc
SCENE.view_layers[0].layer_collection.children[previewlib.name].exclude=True
BLUE={m['blueprint']:m for m in CONTRACT['blueprints']};FOWN={r['componentId']:r['modelId'] for r in CONTRACT['fellingOwners']};META={m['id']:m for m in model_rows}
SCENE.world.use_nodes=True;SCENE.world.node_tree.nodes.get('Background').inputs[0].default_value=(.40,.435,.46,1);SCENE.world.node_tree.nodes.get('Background').inputs[1].default_value=.8
sun_data=bpy.data.lights.new('Warm_morning_sun','SUN');sun_data.energy=2.35;sun_data.color=(1.0,.88,.71);sun_data.angle=math.radians(24);sun=bpy.data.objects.new('Warm_morning_sun',sun_data);SCENE.collection.objects.link(sun);sun.rotation_euler=(math.radians(24),math.radians(-18),math.radians(-23))
SCENE.render.engine='CYCLES';SCENE.cycles.device='CPU';SCENE.cycles.samples=args.samples;SCENE.cycles.use_denoising=True
SCENE.render.resolution_x=2048;SCENE.render.resolution_y=640;SCENE.render.resolution_percentage=100;SCENE.render.image_settings.file_format='PNG';SCENE.render.film_transparent=False
SCENE.view_settings.view_transform='AgX';SCENE.view_settings.look='AgX - Medium High Contrast';SCENE.view_settings.exposure=.25
camd=bpy.data.cameras.new('Strict_Overhead');cam=bpy.data.objects.new('Strict_Overhead',camd);SCENE.collection.objects.link(cam);cam.location=(40,12.5,90);cam.rotation_euler=(0,0,0);camd.type='ORTHO';camd.ortho_scale=80;SCENE.camera=cam
scene_reports=[];chosen=args.zones.split(',') if args.zones!='all' else CATALOG['zones']
for zid in CATALOG['zones']:
 if zid not in chosen:continue
 z=read_native(zid);emap={e['token']:e for e in z['entities']};onground=[e for e in z['entities'] if e['x']>=0 and e['y']>=0]
 wet={(c['x'],c['y']) for c in z['cells'] if c['hasTileWater']}
 coll=bpy.data.collections.new(zid+'_NATIVE_SNAPSHOT');SCENE.collection.children.link(coll);instances=[];accounted=set();placements=[]
 def place(mid,x,y,token='',bp='',owner='',role='entity',height=0,variant=0):
  ob=bpy.data.objects.new(owner or token or mid,None);ob.instance_type='COLLECTION';ob.instance_collection=PREVIEW[mid];coll.objects.link(ob);ob.location=(x+.5,24.5-y,height);ob['modelId']=mid;ob['nativeToken']=token;ob['blueprint']=bp;ob['ownerId']=owner;ob['role']=role
  
  if META[mid]['kind']=='ground':ob.rotation_euler[2]=-ground_turn(x,y)
  if role=='current-native-water':
   mask=sum(bit for bit,dx,dy in [(1,0,-1),(2,1,0),(4,0,1),(8,-1,0)] if (x+dx,y+dy) not in wet);ob.instance_collection=WATER_PREVIEW[mask];ob['waterDryMask']=mask
  instances.append(ob);placements.append({'modelId':mid,'x':x,'y':y,'height':height,'nativeToken':token,'blueprint':bp,'ownerId':owner,'role':role});return ob
 for cell in z['cells']:
  natives=[emap[t] for t in cell['entityTokens']]
  terrain=next((e for e in natives if e['blueprint'] in ('TepuiStone','Grass','Floor','WaterPuddle')),None)
  bp=terrain['blueprint'] if terrain else ('TepuiStone' if z['biome']=='Stump' else 'Grass');ids=BLUE[bp]['models'];v=0 if zid=='Overworld.3.5.0' and 31<=cell['x']<=48 and 6<=cell['y']<=18 else seed(zid+':ground:'+str(cell['x'])+':'+str(cell['y']))%len(ids)
  place(ids[v],cell['x'],cell['y'],terrain['token'] if terrain else '',bp,role='ground')
  if terrain:accounted.add(terrain['token'])
  if cell['hasTileWater']:
   place('ring-water-surface',cell['x'],cell['y'],role='current-native-water',height=.035)
 for e in onground:
  if e['token'] in accounted:continue
  bp=e['blueprint'];owner=''
  if bp=='FellingSceneProp':owner=public_fields(e,'FellingSceneProp')['ComponentId'];mid=FOWN[owner]
  else:
   ids=BLUE[bp]['models'];mid=ids[seed((e['id'] or bp)+':'+bp)%len(ids)]
  place(mid,e['x'],e['y'],e['token'],bp,owner,BLUE[bp]['role']);accounted.add(e['token'])
 assert accounted=={e['token'] for e in onground},zid
 root=OUT/'scenes'/zid;root.mkdir(exist_ok=True)
 # Copy current reference into source scene as a non-rendering image guide.
 ref=bpy.data.images.load(str(SOURCE/'references'/(zid+'.png')),check_existing=True);ref.pack();refobj=bpy.data.objects.new('IMAGEGEN_REFERENCE_GUIDE',None);refobj.empty_display_type='IMAGE';refobj.data=ref;coll.objects.link(refobj);refobj.location=(40,12.5,-2);refobj.empty_display_size=80;refobj.hide_render=True;refobj.hide_viewport=True
 report={'zoneId':zid,'sourceCellCount':len(z['cells']),'sourceNativeJsonSha256':hashlib.sha256(gzip.decompress((SOURCE/'native'/(zid+'.json.gz')).read_bytes())).hexdigest(),'sourceGroundEntityCount':len(onground),'allGroundTokensRepresented':True,'nativeSnapshotOnly':True,'projection':'strict overhead;80x25 square metre cells','reference':'references/'+zid+'.png','referenceIsNotGeometry':True,'placements':placements,'placedTriangles':sum(sum(len(o.data.loop_triangles) if o.data.loop_triangles else sum(len(p.vertices)-2 for p in o.data.polygons) for o in inst.instance_collection.objects if o.type=='MESH') for inst in instances),'fellingOwners':sum(p['ownerId']!='' for p in placements),'tileWaterCells':sum(c['hasTileWater'] for c in z['cells']),'rigInstances':sum(META[p['modelId']]['rigged'] for p in placements),'honesty':['Captured native layout, not a runtime replacement for generated or saved state.','Preview instances show neutral pose; each rigged runtime FBX contains its five action clips.','Surface water comes from actual current TileState only; pool banks and Felling flow detail do not duplicate it.','Lighting is Blender art-direction lighting, not a claim about native Unity FOV or exposure.']}
 (root/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
 SCENE.render.filepath=str(root/'overhead.png');bpy.ops.wm.save_as_mainfile(filepath=str(root/'scene.blend'),compress=True)
 if args.render:
  bpy.ops.render.render(write_still=True)
 # Real hierarchy/mesh FBX for art inspection. Runtime uses individual catalog models instead.
 if not args.skip_export:
  temps=[];exportroot=bpy.data.objects.new(zid+'_Snapshot',None);SCENE.collection.objects.link(exportroot);exportroot.rotation_euler[2]=math.pi;temps.append(exportroot)
  for inst in instances:
   parent=bpy.data.objects.new(inst.name,None);SCENE.collection.objects.link(parent);parent.parent=exportroot;parent.location=inst.location;parent.rotation_euler=inst.rotation_euler;temps.append(parent)
   for source in inst.instance_collection.objects:
    ob=bpy.data.objects.new(source.name,source.data);SCENE.collection.objects.link(ob);ob.parent=parent;temps.append(ob)
  for o in bpy.context.selected_objects:o.select_set(False)
  for o in temps:o.select_set(True)
  bpy.context.view_layer.objects.active=exportroot
  bpy.ops.export_scene.fbx(filepath=str(root/'scene-preview.fbx'),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',global_scale=1,bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',use_custom_props=True)
  for o in temps:bpy.data.objects.remove(o,do_unlink=True)
 scene_reports.append({k:v for k,v in report.items() if k!='placements'})
 print('RING_SCENE_COMPLETE',zid,'triangles',report['placedTriangles'],flush=True)
 for ob in list(coll.objects):bpy.data.objects.remove(ob,do_unlink=True)
 bpy.data.collections.remove(coll)
(OUT/'reports/scenes.json').write_text(json.dumps(scene_reports,indent=2)+'\n')
(OUT/'reports/kit.json').write_text(json.dumps({'models':len(model_rows),'sourceTriangles':sum(m['triangles'] for m in model_rows),'riggedModels':sum(m['rigged'] for m in model_rows),'humanoidRigs':sum(m['rigFamily']=='humanoid' for m in model_rows),'nativeSourceSeed':INDEX['seed'],'sourceArtSeed':SEED,'exportAxis':'source eastX,northY,upZ; export-onlyZ180; FBX+Zforward,Yup; static bake true/rig false','materials':['SpawnRingPalette','SpawnRingWater'],'paletteSize':[2048,1024],'status':'Blender source and export only; Unity verification belongs to native integration'},indent=2)+'\n')
print('RING_BUILD_COMPLETE',len(model_rows),len(scene_reports),flush=True)
