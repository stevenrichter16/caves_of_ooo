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
GLOWS={'fire_glow':((1,.50,.055),.68),'river_glow':((.12,.90,1),.62),'root_glow':((1,.84,.18),.68),'frost_glow':((.36,.69,1),.64),'calm_glow':((.72,.44,1),.60)}

def install_materials(a):
 for key,(color,strength) in dict(PALETTE,**GLOWS).items():
  m=bpy.data.materials.new('SpellFolk_RD_'+key);m.use_nodes=True;node=m.node_tree.nodes.get('Principled BSDF');rgba=(*a['linear_swatch'](color),1)
  m.diffuse_color=rgba;node.inputs['Base Color'].default_value=rgba;node.inputs['Roughness'].default_value=.65
  m['emission']=strength;m['glow']=key in GLOWS
  if key in GLOWS:
   nt=m.node_tree;out=nt.nodes.get('Material Output');transparent=nt.nodes.new('ShaderNodeBsdfTransparent');emit=nt.nodes.new('ShaderNodeEmission');emit.inputs['Color'].default_value=rgba
   attr=nt.nodes.new('ShaderNodeVertexColor');attr.layer_name='SpellGlow';energy=nt.nodes.new('ShaderNodeMath');energy.operation='MULTIPLY';energy.inputs[1].default_value=strength;nt.links.new(attr.outputs['Alpha'],energy.inputs[0])
   rays=nt.nodes.new('ShaderNodeLightPath');camera_only=nt.nodes.new('ShaderNodeMath');camera_only.operation='MULTIPLY';nt.links.new(energy.outputs[0],camera_only.inputs[0]);nt.links.new(rays.outputs['Is Camera Ray'],camera_only.inputs[1]);nt.links.new(camera_only.outputs[0],emit.inputs['Strength'])
   add=nt.nodes.new('ShaderNodeAddShader');nt.links.new(transparent.outputs[0],add.inputs[0]);nt.links.new(emit.outputs[0],add.inputs[1]);nt.links.new(add.outputs[0],out.inputs['Surface'])
  else:
   nt=m.node_tree;out=nt.nodes.get('Material Output');emit=nt.nodes.new('ShaderNodeEmission');emit.inputs['Color'].default_value=rgba;emit.inputs['Strength'].default_value=1.0;mix=nt.nodes.new('ShaderNodeMixShader');mix.inputs[0].default_value=strength*.93;nt.links.new(node.outputs[0],mix.inputs[1]);nt.links.new(emit.outputs[0],mix.inputs[2]);nt.links.new(mix.outputs[0],out.inputs['Surface'])
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
  elif essential and bounds=='AirborneDecoration' and sid!='ground_surge':
   # The main sculpted mass continues to unfurl during its strong scale hold.
   # This is one eased follow-through, not jitter or a perpetual particle spin.
   base=ob.location.copy();rot=ob.rotation_euler.copy();sign=-1 if sum(ord(c) for c in ob.name)%2 else 1
   rise=-.10 if sid=='rime_grip' else .13 if sid=='calm' else .16
   push=.09 if sid in ('ember_spit','flaming_hands','jet_blast') else 0
   turn=Vector((.025,-.08 if sid=='jet_blast' else .035,sign*(.045 if sid=='rime_grip' else .075)))
   for frame,mix in [(contact-5,0),(contact+3,0),(contact+18,1),(clear,.38)]:
    key(ob,frame,location=base+Vector((push,0,rise))*mix,rotation=Vector(rot)+turn*mix)
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
 def glowbed(name,center,radius,mat,parent=aftermath,planes=3,**kw):
  # The horizontal bed and two crossed open annuli are actual depth-tested
  # meshes. A transparent centre protects the face; outer alpha reaches zero.
  vs=[];faces=[];colors=[];sides=40;rings=[(.25,0),(.40,.24),(.61,.46),(.79,.25),(1.0,0)]
  for plane in range(planes):
   base=len(vs)
   for ratio,alpha in rings:
    for j in range(sides):
     angle=j*math.tau/sides;u=math.cos(angle)*radius*ratio;v=math.sin(angle)*radius*ratio
     p=Vector((u,v,.17)) if plane==0 else Vector((u,.20,v*.74+.90)) if plane==1 else Vector((.16,u,v*.74+.90))
     vs.append(tuple(Vector(center)+p));colors.append((1,1,1,alpha*(1 if plane==0 else .55)))
   for ring in range(len(rings)-1):
    for j in range(sides):
     i=base+ring*sides+j;n=base+ring*sides+(j+1)%sides;faces.append((i,n,n+sides,i+sides))
  ob=mesh(name,vs,faces,'rd_'+mat,parent);attr=ob.data.color_attributes.new(name='SpellGlow',type='FLOAT_COLOR',domain='POINT')
  for dst,c in zip(attr.data,colors):dst.color=c
  return own(ob,essential=False,**kw)
 def motes(prefix,centers,mat,parent=aftermath,shape='spark',scale=.07,**kw):
  # Each cohort is 8–16 independent, faceted, fully volumetric particles.
  vs=[];faces=[]
  for n,center in enumerate(centers):
   c=Vector(center);r=scale*(.62+rng.random()*.70);axis=Vector((rng.uniform(-.7,.7),rng.uniform(-.4,.4),1)).normalized();ref=Vector((0,1,0));u=axis.cross(ref).normalized();v=axis.cross(u).normalized();length=r*(2.25 if shape=='leaf' else 1.5 if shape=='ice' else 1.2)
   offset=len(vs)
   for p in [c+axis*length,c-axis*length,c+u*r,c-u*r,c+v*r*.65,c-v*r*.65]:vs.append(tuple(p))
   faces.extend(tuple(offset+j for j in f) for f in [(0,2,4),(0,4,3),(0,3,5),(0,5,2),(1,4,2),(1,3,4),(1,5,3),(1,2,5)])
  ob=mesh(prefix,vs,faces,'rd_'+mat,parent);return own(ob,essential=False,drift=(.06,rng.uniform(-.07,.07),.52 if shape!='ice' else -.25),**kw)
 def sample_path(ps,t):
  f=min(len(ps)-1.00001,max(0,t)*(len(ps)-1));i=int(f);return Vector(ps[i]).lerp(Vector(ps[i+1]),f-i)
 def cluster_paths(prefix,paths,colors,cohorts=12,count=12,spread=.14,parent=aftermath,shape='spark',scale=.065,**kw):
  for j in range(cohorts):
   centers=[]
   for k in range(count):
    t=(k+rng.random()*.4)/count;pt=sample_path(paths[j%len(paths)],t)
    tangent=(sample_path(paths[j%len(paths)],min(1,t+.025))-sample_path(paths[j%len(paths)],max(0,t-.025))).normalized();ref=Vector((0,1,0)) if abs(tangent.y)<.88 else Vector((0,0,1));u=tangent.cross(ref).normalized();v=tangent.cross(u).normalized();angle=j*2.399+k*.87;radial=spread*(1.28+rng.random()*.26);pt+=(u*math.cos(angle)+v*math.sin(angle))*radial;pt+=Vector((rng.uniform(-spread,spread),rng.uniform(-spread,spread),rng.uniform(-spread,spread)))*.16;centers.append(pt)
   motes(prefix+'_motes_'+str(j),centers,colors[j%len(colors)],parent,shape,scale,**kw)
 def addvec(points,offset):return [tuple(Vector(p)+Vector(offset)) for p in points]
 if sid in ('ember_spit','flaming_hands'):
  hands=sid=='flaming_hands';cell=(1,0);role='ConeCell' if hands else 'TargetImpact';anchor='Cell' if hands else 'Target';opts=dict(role=role,anchor=anchor,cell=cell);paths=[]
  # Curved tongues wrap under and behind the recipient, then roll inward.
  # Differing azimuths provide real depth; tapered C/S curves avoid a starburst
  # of long straight filaments while leaving the central face column open.
  for j in range(7):
   azimuth=math.radians(-64+j*21.3);ps=[];radius=1.74+.14*math.sin(j*1.4);height=.94+.11*math.cos(j*1.2)
   for k in range(26):
    t=k/25;angle=-2.70+t*(4.27+.14*math.sin(j));x=.10+radius*math.cos(angle);z=1.16+height*math.sin(angle)
    x+=.10*math.sin(t*math.pi*3+j*.3)*math.sin(t*math.pi);y=.15+.14*math.sin(t*math.pi)
    ps.append(tuple(target+Vector((x*math.cos(azimuth)-y*math.sin(azimuth),x*math.sin(azimuth)+y*math.cos(azimuth),z))))
   paths.append(ps);r=[.018+(.16+.025*(j%3))*math.sin(math.pi*(k+.20)/26)**.85 for k in range(26)]
   tube('RD_Fire_%s_swept_tongue_%d'%(sid,j),ps,r,'molten_gold' if j%3!=1 else 'fire_coral',flatten=.42,**opts)
   if j in (0,2,4,6):
    inner=[tuple(Vector(p)+Vector((-.017,-.025,.028))) for p in ps];tube('RD_Fire_%s_hot_ridge_%d'%(sid,j),inner,[q*.28 for q in r],'hot_ivory',flatten=.6,**opts)
  glowbed('RD_Fire_%s_soft_bed'%sid,target,2.48,'fire_glow',**opts)
  halo('RD_Fire_%s_crown_halo'%sid,paths[3],.55,'fire_glow',**opts)
  cluster_paths('RD_Fire_'+sid,paths,['hot_ivory','molten_gold','hot_ivory','fire_coral'],cohorts=20 if hands else 18,count=16,spread=.23,scale=.044,**opts)
  if not hands:
   carrier=bpy.data.objects['Ember__travel_seed'];trail=[]
   for k in range(20):
    t=k/19;trail.append((-1.45+1.65*t,.06*math.sin(t*7),.04+.08*math.sin(t*5)))
   tube('RD_Ember_tapered_flame_stream',trail,[.006+.11*(k/19)**1.4 for k in range(20)],'molten_gold',carrier,flatten=.38,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+10)
   ridge=[tuple(Vector(p)+Vector((0,-.018,.026))) for p in trail];tube('RD_Ember_fine_ivory_ridge',ridge,[.004+.026*(k/19)**1.5 for k in range(20)],'hot_ivory',carrier,flatten=.65,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+10)
   halo('RD_Ember_travel_halo',trail,.42,'fire_glow',carrier,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+10)
   cluster_paths('RD_Ember_stream',[trail],['hot_ivory','molten_gold'],cohorts=4,count=16,spread=.13,parent=carrier,scale=.038,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+10)
 elif sid=='jet_blast':
  # Four semantic owners contribute adjacent patches of ONE flowing fan. The
  # shared global width/height function makes seams overlap above the true cells;
  # only the far edge turns upward. There is no repeated upright arch per owner.
  def flow(u,f):
   width=.26+.84*(u-.40);y=f*width
   curl=max(0,(u-2.04)/.70);x=-2.35+u+.44*curl-.86*f*f*curl*curl
   z=.47+.13*math.sin(u*2.3)+.08*math.cos(f*8+u*3)+(.70+.22*math.cos(f*math.pi))*curl*curl
   return Vector((x,y,z))
  for ci,(forward,lateral) in enumerate([(1,0),(2,-1),(2,0),(2,1)]):
   opts=dict(role='ConeCell',anchor='Cell',cell=(forward,lateral));lo,hi=(.40,1.62) if forward==1 else (1.30,2.74)
   fa,fb=(-1,1) if forward==1 else (-1,-.31) if lateral<0 else (.31,1) if lateral>0 else (-.36,.36)
   rows=13;cols=6;vs=[];faces=[]
   for layer in (0,1):
    for r in range(rows):
     u=lo+(hi-lo)*r/(rows-1)
     for c in range(cols):
      f=fa+(fb-fa)*c/(cols-1);p=flow(u,f);p.z-=.095*layer;vs.append(tuple(p))
   per=rows*cols
   for r in range(rows-1):
    for c in range(cols-1):
     i=r*cols+c;faces.append((i,i+cols,i+cols+1,i+1));faces.append((per+i,per+i+1,per+i+cols+1,per+i+cols))
   for r in range(rows-1):
    for c in (0,cols-1):
     i=r*cols+c;face=(i,per+i,per+i+cols,i+cols);faces.append(face if c==0 else tuple(reversed(face)))
   for c in range(cols-1):
    for r in (0,rows-1):
     i=r*cols+c;face=(i,i+1,per+i+1,per+i);faces.append(face if r==0 else tuple(reversed(face)))
   body=mesh('RD_Jet_cell_%d_%d_connected_fold'%(forward,lateral),vs,faces,'rd_river_turquoise',aftermath);own(body,**opts)
   paths=[]
   for j,fraction in enumerate((.17,.53,.85)):
    f=fa+(fb-fa)*fraction;ps=[tuple(flow(lo+(hi-lo)*k/18,f)+Vector((0,0,.035+.025*math.sin(k*.8+j)))) for k in range(19)];paths.append(ps)
    tube('RD_Jet_cell_%d_%d_stream_crest_%d'%(forward,lateral,j),ps,[.035+.020*math.sin(math.pi*(k+.5)/19) for k in range(19)],'river_pearl' if j==1 else 'river_mint',flatten=1.5,**opts)
   if forward==2:
    lip=[tuple(flow(2.73,fa+(fb-fa)*k/15)+Vector((0,0,.025))) for k in range(16)];paths.append(lip)
    tube('RD_Jet_cell_%d_%d_front_curl'%(forward,lateral),lip,.115,'river_pearl',**opts)
    rear=[tuple(Vector(p)+Vector((-.16,0,-.13))) for p in lip];tube('RD_Jet_cell_%d_%d_rolling_curl'%(forward,lateral),rear,.18,'river_mint',flatten=.55,**opts)
   halo('RD_Jet_cell_%d_%d_mist'%(forward,lateral),paths[1],.50,'river_glow',**opts)
   glowbed('RD_Jet_cell_%d_%d_soft_bed'%(forward,lateral),Vector((-2.35+forward,lateral,0)),1.12 if forward==1 else 1.45,'river_glow',planes=1,**opts)
   cluster_paths('RD_Jet_cell_%d_%d'%(forward,lateral),paths,['river_pearl','river_mint','river_pearl'],cohorts=6,count=16,spread=.19,scale=.041,**opts)
 elif sid=='ground_surge':
  for ci in range(4):
   origin=Vector((-1.35+ci,0,0));opts=dict(role='GroundCell',anchor='Cell',cell=(ci+1,0));paths=[]
   # Native cell edges exactly meet; tiny golden forks stay low and rooted.
   for j in range(2):
    ps=addvec([(-.48,(j-.5)*.16,.13),(-.25,-.13+j*.15,.18),(-.06,.12-j*.15,.13),(.13,-.09+j*.15,.24),(.33,.08-j*.15,.18),(.48,(j-.5)*.16,.13)],origin)
    tube('RD_Surge_cell_%d_root_%d'%(ci+1,j),ps,([.022]+[.075]*4+[.022]) if j==0 else ([.022]+[.045]*4+[.022]),'root_gold' if j==0 else 'root_ivory',bounds='CellSurface',**opts);paths.append(ps)
   branch=addvec([(-.28,0,.13),(-.17,.05,.42),(.02,-.10,.62),(-.07,.06,.97),(.18,.15,1.26),(.08,.18,1.36)],origin)
   tube('RD_Surge_cell_%d_lifted_fork'%(ci+1),branch,[.065,.085,.074,.06,.045,.016],'root_ivory',**opts);paths.append(branch)
   for side in (-1,1):
    fork=addvec([(-.16,0,.15),(-.05,side*.19,.31),(.13,side*.29,.39),(.02,side*.42,.65),(.22,side*.48,.84)],origin);paths.append(fork);tube('RD_Surge_cell_%d_side_fork_%d'%(ci+1,side),fork,[.065,.075,.08,.055,.02],'root_ivory' if side<0 else 'root_gold',**opts)
   glowbed('RD_Surge_cell_%d_ground_glow'%(ci+1),origin,.83,'root_glow',planes=1,**opts)
   halo('RD_Surge_cell_%d_halo'%(ci+1),branch,.24,'root_glow',**opts)
   cluster_paths('RD_Surge_cell_%d'%(ci+1),paths,['root_ivory','root_gold','root_ivory'],cohorts=5,count=16,spread=.14,scale=.042,**opts)
 elif sid=='rime_grip':
  paths=[]
  # Layered short blunt petals form a broad frost crown; the native lower
  # clamps remain distinct. The centre and head column are unfilled.
  for j in range(12):
   angle=.10+j*math.tau/12;ps=[]
   for k in range(9):
    t=k/8;r=.82+t*1.20;phi=angle+.28*math.sin(t*math.pi);z=.20+.66*math.sin(t*math.pi*.72)+.13*(j%3)
    ps.append(tuple(target+Vector((math.cos(phi)*r,math.sin(phi)*r,z))))
   paths.append(ps);radii=[.055,.11,.16,.19,.19,.17,.14,.10,.065]
   tube('RD_Rime_frozen_blunt_petal_'+str(j),ps,radii,'frost_white' if j%3 else 'frost_blue',condition='FrozenApplied',flatten=.58)
   if j%2==0:
    seam=[tuple(Vector(p)+Vector((.02,.035,.035))) for p in ps];tube('RD_Rime_frozen_carved_seam_'+str(j),seam,.026,'frost_indigo',condition='FrozenApplied',flatten=.6)
  low=[tuple(target+Vector((1.86*math.cos(t),1.86*math.sin(t),.27))) for t in [-2.9+k*2.6/23 for k in range(24)]]
  tube('RD_Rime_neutral_contact_crest',low,.072,'frost_white')
  glowbed('RD_Rime_soft_frost_bed',target,2.58,'frost_glow')
  halo('RD_Rime_frost_halo',low,.48,'frost_glow')
  cluster_paths('RD_Rime',paths,['frost_white','frost_blue','frost_white'],cohorts=22,count=16,spread=.22,shape='ice',scale=.043)
 elif sid=='calm':
  carrier=bpy.data.objects['Calm__travel_open_loop'];paths=[]
  for j,(begin,length,radius) in enumerate([(-2.85,1.70,1.96),(-.95,1.70,1.96),(1.0,1.50,1.96)]):
   ps=[]
   for k in range(18):
    t=k/17;angle=begin+t*length;ps.append((math.cos(angle)*radius,math.sin(angle)*radius,.60*math.sin(angle*2)+.32*math.cos(angle)))
   paths.append(ps);tube('RD_Calm_open_travel_ribbon_'+str(j),ps,[.016+.14*math.sin(math.pi*(k+.5)/18)**.8 for k in range(18)],'calm_lilac' if j%2==0 else 'calm_ivory',carrier,flatten=.4,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+18)
   halo('RD_Calm_travel_halo_'+str(j),ps,.50,'calm_glow',carrier,role='ProjectileHead',anchor='ProjectileCarrier',start=14,hold=contact+18)
  # Target-bound petals carry the same open gaps; only successful pacification
  # creates the relaxing outer envelope. No cage, heart or sleep glyph.
  outpaths=[]
  for j,ps in enumerate(paths):
   offset=target+Vector((0,.06,1.03));pts=[tuple(Vector((p[0]*1.07,p[1],p[2]*1.06))+offset) for p in ps];outpaths.append(pts)
   tube('RD_Calm_pacified_unfurl_'+str(j),pts,[.018+.13*math.sin(math.pi*(k+.5)/18) for k in range(18)],'calm_ivory' if j!=1 else 'calm_lilac',flatten=.4,condition='PacifiedApplied')
   ridge=[tuple(Vector(p)+Vector((0,-.035,.055))) for p in pts];tube('RD_Calm_folded_petal_ridge_'+str(j),ridge,.034,'calm_ivory',condition='PacifiedApplied')
  glowbed('RD_Calm_pacified_soft_bed',target,2.64,'calm_glow',condition='PacifiedApplied')
  cluster_paths('RD_Calm',outpaths,['calm_ivory','calm_lilac','calm_ivory','calm_violet'],cohorts=22,count=16,spread=.22,shape='leaf',scale=.039,condition='PacifiedApplied')
 # Rain's retained utility has deliberately no new cloud or adjacent-cell volume.
 return created
