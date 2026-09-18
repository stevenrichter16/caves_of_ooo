"""Dense carved spell art. Imported deliberately by build_studies; no startup work.

All volumes are real meshes with keyed transforms. Surface cells and airborne
presentation are separately owned; no collider or simulation semantics live here.
"""
import math, random
import bpy
from mathutils import Vector

PALETTE={
 'hot_ivory':((1.0,.94,.66),1.0),'molten_gold':((1.0,.68,.10),.88),'fire_coral':((1.0,.28,.06),.67),'fire_plum':((.43,.10,.11),.3),
 'river_turquoise':((.045,.67,.77),.67),'river_mint':((.44,.98,.94),.92),'river_pearl':((.86,1.0,.96),1.0),'river_deep':((.045,.27,.42),.30),
 'root_ivory':((1.0,.97,.65),1.0),'root_gold':((1.0,.73,.10),.85),'root_ochre':((.67,.32,.065),.5),
 'frost_white':((.88,.98,1.0),1.0),'frost_blue':((.30,.66,1.0),.82),'frost_indigo':((.14,.25,.58),.42),
 'calm_ivory':((1.0,.91,1.0),.97),'calm_lilac':((.76,.52,1.0),.82),'calm_violet':((.40,.23,.72),.50),
}
GLOWS={'fire_glow':((1,.43,.035),.23),'river_glow':((.07,.86,1),.20),'root_glow':((1,.80,.13),.23),'frost_glow':((.27,.59,1),.23),'calm_glow':((.67,.36,1),.22)}

def install_materials(a):
 for key,(color,strength) in dict(PALETTE,**GLOWS).items():
  m=bpy.data.materials.new('SpellFolk_RD_'+key);m.use_nodes=True;node=m.node_tree.nodes.get('Principled BSDF');rgba=(*a['linear_swatch'](color),1)
  m.diffuse_color=rgba;node.inputs['Base Color'].default_value=rgba;node.inputs['Roughness'].default_value=.65
  m['emission']=strength;m['glow']=key in GLOWS
  if key in GLOWS:
   nt=m.node_tree;out=nt.nodes.get('Material Output');transparent=nt.nodes.new('ShaderNodeBsdfTransparent');emit=nt.nodes.new('ShaderNodeEmission');emit.inputs['Color'].default_value=rgba;emit.inputs['Strength'].default_value=1.4
   attr=nt.nodes.new('ShaderNodeVertexColor');attr.layer_name='SpellGlow';mix=nt.nodes.new('ShaderNodeMixShader');nt.links.new(attr.outputs['Alpha'],mix.inputs[0]);nt.links.new(transparent.outputs[0],mix.inputs[1]);nt.links.new(emit.outputs[0],mix.inputs[2]);nt.links.new(mix.outputs[0],out.inputs['Surface'])
  else:
   node.inputs['Emission Color'].default_value=rgba;node.inputs['Emission Strength'].default_value=strength*.70
  a['MATS']['rd_'+key]=m
 # The retained utility and inner carving use the same explicit material contract.
 for key,m in a['MATS'].items():
  if 'emission' not in m:m['emission']=.42 if key in ('ivory','foam','water_light','ice_rim','ember_core','violet_core') else 0.0
  if 'glow' not in m:m['glow']=False


def author(a,spec,root,aftermath,target,cast,contact,clear):
 sid=spec['id'];rng=random.Random(73129+sum(ord(c) for c in sid));mesh=a['mesh'];pulse=a['pulse'];key=a['key'];M=a['MATS'];created=[]
 def own(ob,role='TargetImpact',condition='Always',anchor='Target',cell=(0,0),essential=True,bounds='AirborneDecoration',start=None,hold=None,drift=None):
  ob['nativeRole']=role;ob['nativeCondition']=condition;ob['nativeAnchor']=anchor;ob['nativeForwardCell']=cell[0];ob['nativeLateralCell']=cell[1];ob['nativeVariant']=max(0,cell[0]-1) if role=='GroundCell' else 0;ob['visualBounds']=bounds;ob['reducedEssential']=essential;ob['readabilityRevision']=1
  pulse(ob,contact-5 if start is None else start,contact+21 if hold is None else hold,clear)
  if drift:
   base=ob.location.copy();key(ob,contact-5,location=base-Vector(drift)*.20);key(ob,contact+20,location=base+Vector(drift)*.35);key(ob,clear,location=base+Vector(drift))
  created.append(ob);return ob
 def tube(name,points,radii,mat,parent=aftermath,flatten=1,**kw):
  vs=[];faces=[];sides=6;ps=[Vector(p) for p in points]
  for i,p in enumerate(ps):
   tangent=(ps[min(i+1,len(ps)-1)]-ps[max(0,i-1)]).normalized();ref=Vector((0,1,0)) if abs(tangent.y)<.88 else Vector((0,0,1));u=tangent.cross(ref).normalized();v=tangent.cross(u).normalized();r=radii[i] if isinstance(radii,list) else radii
   for j in range(sides):vs.append(tuple(p+u*(math.cos(j*math.tau/sides)*r)+v*(math.sin(j*math.tau/sides)*r*flatten)))
   if i:
    for j in range(sides):faces.append(((i-1)*sides+j,(i-1)*sides+(j+1)%sides,i*sides+(j+1)%sides,i*sides+j))
  faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(ps)-1)*sides+j for j in range(sides))]);ob=mesh(name,vs,faces,'rd_'+mat,parent);return own(ob,**kw)
 def halo(name,points,width,mat,parent=aftermath,**kw):
  ps=[Vector(p) for p in points];vs=[];faces=[];colors=[]
  # Two crossing planes are a volumetric glow sleeve; transparent outer bands
  # soften edges without animated material parameters or post-processing.
  for plane in range(2):
   base=len(vs)
   for i,p in enumerate(ps):
    tangent=(ps[min(i+1,len(ps)-1)]-ps[max(0,i-1)]).normalized();ref=Vector((0,1,0)) if abs(tangent.y)<.88 else Vector((0,0,1));u=tangent.cross(ref).normalized();v=tangent.cross(u).normalized();side=u if plane==0 else v
    w=width*(.25+.75*math.sin(math.pi*(i+.5)/len(ps)))
    for f,alpha in [(-1,0),(-.42,.18),(0,.60),(.42,.18),(1,0)]:vs.append(tuple(p+side*w*f));colors.append((1,1,1,alpha))
    if i:
     for j in range(4):faces.append((base+(i-1)*5+j,base+(i-1)*5+j+1,base+i*5+j+1,base+i*5+j))
  ob=mesh(name,vs,faces,'rd_'+mat,parent);attribute=ob.data.color_attributes.new(name='SpellGlow',type='FLOAT_COLOR',domain='POINT')
  for dst,c in zip(attribute.data,colors):dst.color=c
  return own(ob,essential=False,**kw)
 def motes(prefix,centers,mat,parent=aftermath,shape='spark',scale=.07,**kw):
  # Each cohort is 8–16 independent, faceted, fully volumetric particles.
  vs=[];faces=[]
  for n,center in enumerate(centers):
   c=Vector(center);r=scale*(.62+rng.random()*.70);axis=Vector((rng.uniform(-.7,.7),rng.uniform(-.4,.4),1)).normalized();ref=Vector((0,1,0));u=axis.cross(ref).normalized();v=axis.cross(u).normalized();length=r*(2.25 if shape=='leaf' else 1.5 if shape=='ice' else 1.2)
   offset=len(vs)
   for p in [c+axis*length,c-axis*length,c+u*r,c-u*r,c+v*r*.65,c-v*r*.65]:vs.append(tuple(p))
   faces.extend(tuple(offset+j for j in f) for f in [(0,2,4),(0,4,3),(0,3,5),(0,5,2),(1,4,2),(1,3,4),(1,5,3),(1,2,5)])
  ob=mesh(prefix,vs,faces,'rd_'+mat,parent);return own(ob,essential=False,drift=(.06,rng.uniform(-.07,.07),.22 if shape!='ice' else -.18),**kw)
 def sample_path(ps,t):
  f=min(len(ps)-1.00001,max(0,t)*(len(ps)-1));i=int(f);return Vector(ps[i]).lerp(Vector(ps[i+1]),f-i)
 def cluster_paths(prefix,paths,colors,cohorts=12,count=12,spread=.14,parent=aftermath,shape='spark',scale=.065,**kw):
  for j in range(cohorts):
   centers=[]
   for k in range(count):
    t=(k+rng.random()*.4)/count;pt=sample_path(paths[j%len(paths)],t)
    pt+=Vector((rng.uniform(-spread,spread),rng.uniform(-spread,spread),rng.uniform(-spread,spread)));centers.append(pt)
   motes(prefix+'_motes_'+str(j),centers,colors[j%len(colors)],parent,shape,scale,**kw)
 def addvec(points,offset):return [tuple(Vector(p)+Vector(offset)) for p in points]
 if sid in ('ember_spit','flaming_hands'):
  hands=sid=='flaming_hands';cell=(1,0);role='ConeCell' if hands else 'TargetImpact';anchor='Cell' if hands else 'Target';opts=dict(role=role,anchor=anchor,cell=cell)
  paths=[]
  # Swept asymmetric flame crescents: broad lower skirt, rising carved fingers,
  # unfilled centre above the shoulder. These are volumes, not a billboard flare.
  for j in range(5):
   radius=.86+j*.12;ps=[]
   for k in range(17):
    t=k/16;angle=-2.75+t*(3.82-.15*j);x=.20+radius*math.cos(angle);z=.90+radius*math.sin(angle)*.85;z=max(.22,z);y=.28+.09*math.sin(t*math.pi)+j*.025
    ps.append(tuple(target+Vector((x,y,z))))
   paths.append(ps);r=[(.09+.032*j)*math.sin(math.pi*(k+.8)/18)**.7 for k in range(17)]
   tube('RD_Fire_%s_carved_crown_%d'%(sid,j),ps,r,'molten_gold' if j%2==0 else 'fire_coral',**opts)
   if j in (0,2,4):
    inner=[tuple(Vector(p)+Vector((-.035,-.075,.015))) for p in ps];tube('RD_Fire_%s_ivory_curl_%d'%(sid,j),inner,[q*.42 for q in r],'hot_ivory',**opts)
  halo('RD_Fire_%s_crown_halo'%sid,paths[2],.48,'fire_glow',**opts)
  cluster_paths('RD_Fire_'+sid,paths,['hot_ivory','molten_gold','fire_coral'],cohorts=10 if hands else 12,count=12,spread=.21,scale=.060,**opts)
  if not hands:
   carrier=bpy.data.objects['Ember__travel_seed'];paths=[]
   for j in range(3):
    ps=[(-1.9+t*2.12,(j-1)*(.10+.16*(1-t))+.08*math.sin(t*7+j),.035+.11*math.sin(t*5+j)) for t in [k/15 for k in range(16)]];paths.append(ps)
    tube('RD_Ember_connected_wake_'+str(j),ps,[.035+.13*(k/15)**.7 for k in range(16)],'hot_ivory' if j==1 else 'molten_gold' if j==0 else 'fire_coral',carrier,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+10)
   halo('RD_Ember_travel_halo',paths[1],.32,'fire_glow',carrier,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+10)
 elif sid=='jet_blast':
  for ci,(forward,lateral) in enumerate([(1,0),(2,-1),(2,0),(2,1)]):
   origin=Vector((-2.35+forward,lateral,0));opts=dict(role='ConeCell',anchor='Cell',cell=(forward,lateral));paths=[]
   # Forward rolling lips retain the four cone owners. Broad side curls may
   # overlap visually above the floor; the ground stencil remains unchanged.
   for j in range(3):
    ps=[]
    for k in range(16):
     t=k/15;x=-.62+1.14*t;y=(j-1)*.24+(.24 if lateral>0 else -.24 if lateral<0 else 0)*math.sin(t*math.pi);z=.27+(1.04 if forward==2 else .70)*math.sin(t*math.pi*.84)
     ps.append(tuple(origin+Vector((x,y,z))))
    paths.append(ps);tube('RD_Jet_cell_%d_%d_fold_%d'%(forward,lateral,j),ps,[.06+.14*math.sin(math.pi*(k+.5)/16) for k in range(16)],'river_turquoise' if j!=1 else 'river_mint',flatten=1.45,**opts)
   lip=[tuple(origin+Vector((.11+.15*math.cos(t),.75*math.sin(t),.71+.41*math.cos(t)))) for t in [-1.45+k*2.9/17 for k in range(18)]]
   paths.append(lip);tube('RD_Jet_cell_%d_%d_pearl_crest'%(forward,lateral),lip,.095,'river_pearl',**opts)
   halo('RD_Jet_cell_%d_%d_mist'%(forward,lateral),paths[1],.37,'river_glow',**opts)
   cluster_paths('RD_Jet_cell_%d_%d'%(forward,lateral),paths,['river_pearl','river_mint','river_turquoise'],cohorts=3,count=12,spread=.16,scale=.060,**opts)
 elif sid=='ground_surge':
  for ci in range(4):
   origin=Vector((-1.35+ci,0,0));opts=dict(role='GroundCell',anchor='Cell',cell=(ci+1,0));paths=[]
   # Native cell edges exactly meet; tiny golden forks stay low and rooted.
   for j in range(2):
    ps=addvec([(-.47,(j-.5)*.16,.13),(-.25,-.13+j*.15,.18),(-.06,.12-j*.15,.13),(.13,-.09+j*.15,.24),(.33,.08-j*.15,.18),(.47,(j-.5)*.16,.13)],origin)
    tube('RD_Surge_cell_%d_root_%d'%(ci+1,j),ps,.075 if j==0 else .045,'root_gold' if j==0 else 'root_ivory',bounds='CellSurface',**opts);paths.append(ps)
   branch=addvec([(-.28,0,.13),(-.17,.04,.35),(.02,-.04,.48),(-.07,.02,.76),(.18,.03,.99),(.08,.06,1.07)],origin)
   tube('RD_Surge_cell_%d_lifted_fork'%(ci+1),branch,[.065,.085,.074,.06,.045,.016],'root_ivory',**opts);paths.append(branch)
   halo('RD_Surge_cell_%d_halo'%(ci+1),branch,.24,'root_glow',**opts)
   cluster_paths('RD_Surge_cell_%d'%(ci+1),paths,['root_ivory','root_gold','root_ochre'],cohorts=3,count=12,spread=.105,scale=.047,**opts)
 elif sid=='rime_grip':
  paths=[]
  # Broad exterior chiseled rays with a protected central face column.
  for j in range(7):
   angle=-2.8+j*.55;ps=[]
   for k in range(7):
    t=k/6;r=.72+t*.73;x=math.cos(angle)*r;z=.89+math.sin(angle)*r*.82;y=.29+.10*math.sin(t*math.pi)
    ps.append(tuple(target+Vector((x,y,max(.17,z)))))
   paths.append(ps);r=[.13,.20,.23,.20,.15,.11,.03]
   tube('RD_Rime_frozen_outer_crown_'+str(j),ps,r,'frost_blue' if j%2 else 'frost_white',condition='FrozenApplied',flatten=.68)
   tube('RD_Rime_frozen_indigo_facet_'+str(j),[tuple(Vector(p)+Vector((.025,.08,-.04))) for p in ps], [v*.6 for v in r],'frost_indigo',condition='FrozenApplied',flatten=.6)
  # A low neutral crescent communicates cold impact without claiming a freeze.
  low=[tuple(target+Vector((1.35*math.cos(t),.30+.08*math.sin(t),.35+.12*math.sin(t)))) for t in [-2.9+k*2.6/17 for k in range(18)]]
  tube('RD_Rime_neutral_contact_crest',low,.09,'frost_white')
  halo('RD_Rime_frost_halo',low,.38,'frost_glow')
  cluster_paths('RD_Rime',paths,['frost_white','frost_blue','frost_indigo'],cohorts=12,count=12,spread=.15,shape='ice',scale=.067)
 elif sid=='calm':
  carrier=bpy.data.objects['Calm__travel_open_loop'];paths=[]
  for j,(begin,length,radius) in enumerate([(-2.85,1.70,1.32),(-.95,1.70,1.32),(1.0,1.50,1.32)]):
   ps=[]
   for k in range(18):
    t=k/17;angle=begin+t*length;ps.append((math.cos(angle)*radius,.25+.10*math.sin(t*math.pi),math.sin(angle)*radius*.82+.03))
   paths.append(ps);tube('RD_Calm_open_travel_ribbon_'+str(j),ps,[.035+.065*math.sin(math.pi*(k+.5)/18) for k in range(18)],'calm_ivory' if j%2==0 else 'calm_lilac',carrier,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+18)
   halo('RD_Calm_travel_halo_'+str(j),ps,.29,'calm_glow',carrier,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+18)
  # Target-bound petals carry the same open gaps; only successful pacification
  # creates the relaxing outer envelope. No cage, heart or sleep glyph.
  outpaths=[]
  for j,ps in enumerate(paths):
   offset=target+Vector((0,.06,1.03));pts=[tuple(Vector((p[0]*1.07,p[1],p[2]*1.06))+offset) for p in ps];outpaths.append(pts)
   tube('RD_Calm_pacified_unfurl_'+str(j),pts,.09,'calm_ivory' if j!=1 else 'calm_lilac',condition='PacifiedApplied')
  cluster_paths('RD_Calm',outpaths,['calm_ivory','calm_lilac','calm_violet'],cohorts=12,count=12,spread=.14,shape='leaf',scale=.062,condition='PacifiedApplied')
 # Rain's retained utility has deliberately no new cloud or adjacent-cell volume.
 return created
