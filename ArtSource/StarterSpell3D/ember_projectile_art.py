"""Ember's flowing projectile identity; deliberately independent of Hands art.

Short Source-anchored sections remap along the actual route in the existing
renderer. No long carrier-child tail, simulation, new material or native role.
"""
import math, random
import bpy
from mathutils import Vector


def smooth(t):
    t=max(0,min(1,t));return t*t*(3-2*t)


def author(a,root,aftermath,target,cast,contact,clear):
    mesh=a['mesh'];key=a['key'];rng=random.Random(18203)
    carrier=bpy.data.objects['Ember__travel_seed'];scene=bpy.context.scene
    # Read the exact retained carrier curves before any new geometry is keyed.
    carriers=[]
    for frame in range(111):
        scene.frame_set(frame);bpy.context.view_layer.update()
        carriers.append(carrier.matrix_world.translation.copy())

    def register(ob,role,anchor,essential=True,start=22,hold=None,end=None):
        ob['transientEffect']=True;ob['noCollider']=True
        ob['nativeRole']=role;ob['nativeCondition']='Always';ob['nativeAnchor']=anchor
        ob['nativeForwardCell']=0;ob['nativeLateralCell']=0;ob['nativeVariant']=0
        ob['visualBounds']='AirborneDecoration';ob['reducedEssential']=essential
        ob['readabilityRevision']=2;ob['startFrame']=start;ob['peakFrame']=contact
        ob['holdFrame']=contact if hold is None else hold;ob['clearFrame']=clear if end is None else end
        a['EFFECTS'].append(ob)
        return ob

    def animate(ob,fn):
        for f in range(111):
            loc,scale,rotation=fn(f)
            key(ob,f,location=loc,scale=scale,rotation=rotation)
        # Analytic smooth functions are sampled at native 100 Hz. Linear keys
        # avoid Bezier overshoot while retaining every real editable pose.
        for layer in ob.animation_data.action.layers:
            for strip in layer.strips:
                bag=strip.channelbag(ob.animation_data.action_slot)
                if bag:
                    for curve in bag.fcurves:
                        for point in curve.keyframe_points:point.interpolation='LINEAR'

    def add_tube(vs,fs,points,radii,flatten=1,sides=6):
        offset=len(vs);ps=[Vector(p) for p in points]
        for i,p in enumerate(ps):
            tangent=(ps[min(i+1,len(ps)-1)]-ps[max(0,i-1)]).normalized()
            u=tangent.cross(Vector((0,1,0))).normalized();v=tangent.cross(u)
            for j in range(sides):
                angle=j*math.tau/sides
                vs.append(tuple(p+u*math.cos(angle)*radii[i]+v*math.sin(angle)*radii[i]*flatten))
            if i:
                for j in range(sides):
                    fs.append((offset+(i-1)*sides+j,offset+(i-1)*sides+(j+1)%sides,offset+i*sides+(j+1)%sides,offset+i*sides+j))
        fs.extend([tuple(offset+j for j in range(sides-1,-1,-1)),tuple(offset+(len(ps)-1)*sides+j for j in range(sides))])

    def cone_radius(q):return .055+.61*q**.88
    def tail_pose(q,f,particle=False):
        carrierpos=carriers[f];forward=carrierpos.x+2.35
        settle=smooth((f-contact)/18)
        fraction=q+(1-q)*settle
        x=-2.35+.36+(forward-.36)*fraction
        y=.045*math.sin(q*math.tau*1.4+f*.095)*math.sin(math.pi*q)
        z=carrierpos.z+.07*math.sin(q*math.pi*2.5-f*.08)*math.sin(math.pi*q)
        envelope=smooth((f-22)/5)*(1-smooth((f-contact)/20))
        # The stream contracts in both length and girth; it does not become
        # a second static contact ornament after reaching its target.
        radial=envelope*(1-.40*settle)
        axial=envelope*(1-.62*settle)
        return ((x,y,z),(axial,radial,radial),(.16*math.sin(q*5+f*.08),0,0))

    # Small luminous nose. These are the only carrier children; their complete
    # forward length is bounded independently of travel distance.
    for name,offset,size,mat in [
        ('charcoal_nose',(-.025,0,0),(.16,.17,.16),'charcoal'),
        ('molten_nose',(.065,-.012,.012),(.18,.205,.185),'rd_molten_gold'),
        ('ivory_nose',(.15,-.07,.055),(.12,.115,.105),'rd_hot_ivory')]:
        ob=a['ico']('RD_Ember_'+name,(0,0,0),size,mat,carrier,sub=2)
        # Apply the intended local proportions to mesh data so the keyed
        # envelope is uniform and independently inspectable.
        for v in ob.data.vertices:
            v.co=Vector((v.co.x*size[0],v.co.y*size[1],v.co.z*size[2]))
        ob.scale=(1,1,1);register(ob,'ProjectileHead','ProjectileCarrier',start=13,end=contact+12)
        def fn(f,offset=offset):
            env=smooth((f-13)/8)*(1-smooth((f-contact)/12))
            return offset,(env,env,env),(0,.04*math.sin(f*.12),0)
        animate(ob,fn)

    # Sixteen short sections, each with a central volume and swept outer
    # ribbons. Their generously lapped caps join even at sqrt(2) cell spacing.
    # Coherent gold across sections avoids a striped, bead-like train.
    for j in range(16):
        q=(j+.5)/16;r=cone_radius(q);vs=[];fs=[]
        xs=[-.18+.36*k/5 for k in range(6)]
        core=[(x,.01*math.sin(k*.8+j*.2),.012*math.cos(k*.7+j*.3)) for k,x in enumerate(xs)]
        add_tube(vs,fs,core,[r*(.37+.035*math.sin(k*.8+j)) for k in range(6)],sides=6)
        for arm in range(3):
            ps=[]
            for k,x in enumerate(xs):
                angle=arm*math.tau/3+q*5.4+x*1.8
                ps.append((x,r*.70*math.cos(angle),r*.70*math.sin(angle)))
            add_tube(vs,fs,ps,[r*.29]*6,flatten=.38,sides=4)
        ob=mesh('RD_Ember_tail_section_%02d'%j,vs,fs,'rd_molten_gold',root)
        register(ob,'ProjectileTrail','Source');animate(ob,lambda f,q=q:tail_pose(q,f))

        # Sixteen distinct faceted ember islands per section. Closely packed
        # within/around the ribbons so the visible tail reads as one full cone.
        vs=[];fs=[]
        for k in range(16):
            angle=k*2.399+j*.42;rad=r*(.57+rng.random()*.38)
            c=Vector((rng.uniform(-.14,.14),math.cos(angle)*rad,math.sin(angle)*rad))
            size=(.022+.018*rng.random())*(.50+.50*q);base=len(vs)
            for p in [c+Vector((size*1.7,0,0)),c-Vector((size*1.7,0,0)),c+Vector((0,size,0)),c-Vector((0,size,0)),c+Vector((0,0,size*.8)),c-Vector((0,0,size*.8))]:vs.append(tuple(p))
            fs.extend(tuple(base+n for n in tri) for tri in [(0,2,4),(0,4,3),(0,3,5),(0,5,2),(1,4,2),(1,3,4),(1,5,3),(1,2,5)])
        ob=mesh('RD_Ember_travel_motes_%02d'%j,vs,fs,['rd_hot_ivory','rd_fire_coral','rd_hot_ivory','rd_molten_gold'][j%4],root)
        register(ob,'ProjectileTrail','Source',essential=False);animate(ob,lambda f,q=q:tail_pose(q,f,True))

    # Eight short crossed glow sleeves follow the same route. Unlike one long
    # sprite, a hidden centre cannot discard the entire visible trailing path.
    for j in range(8):
        q=(j+.5)/8;r=cone_radius(q)*1.10;vs=[];fs=[];colors=[]
        for plane in range(2):
            base=len(vs)
            for x in (-.20,0,.20):
                for f,alpha in [(-1,0),(-.45,.17),(0,.60),(.45,.17),(1,0)]:
                    vs.append((x,r*f if plane==0 else 0,r*f if plane else 0));colors.append((1,1,1,alpha))
            for row in range(2):
                for col in range(4):
                    n=base+row*5+col;fs.append((n,n+1,n+6,n+5))
        ob=mesh('RD_Ember_tail_glow_%02d'%j,vs,fs,'rd_fire_glow',root)
        attr=ob.data.color_attributes.new(name='SpellGlow',type='FLOAT_COLOR',domain='POINT')
        for dst,color in zip(attr.data,colors):dst.color=color
        register(ob,'ProjectileTrail','Source',essential=False);animate(ob,lambda f,q=q:tail_pose(q,f))

    # Contact is deliberately modest: a warm three-lobed puff, two low splash
    # curls and sixteen fading flecks. No radial ring, tongues or fire crown.
    for j in range(3):
        size=(.18+.02*j,.18,.18-.01*j)
        ob=a['ico']('RD_Ember_contact_puff_%d'%j,(0,0,0),size,['rd_molten_gold','rd_fire_coral','rd_hot_ivory'][j],aftermath,sub=1)
        for v in ob.data.vertices:v.co=Vector((v.co.x*size[0],v.co.y*size[1],v.co.z*size[2]))
        ob.scale=(1,1,1);register(ob,'TargetImpact','Target',start=contact-4,end=contact+20)
        def fn(f,j=j):
            t=smooth((f-contact)/20);env=smooth((f-(contact-4))/6)*(1-t)
            p=target+Vector(((j-1)*(.10+.04*t),.06*math.sin(j*2.2),.73-.19*t+.06*(j%2)))
            return p,(env,env,env),(.10*t,.16*j*t,.12*t)
        animate(ob,fn)
    for j in range(2):
        ps=[]
        for k in range(10):
            t=k/9;ps.append((-.20+.40*t,(j-.5)*.26+.035*math.sin(t*5),.60+.09*math.sin(t*math.pi)))
        vs=[];fs=[];add_tube(vs,fs,ps,[.018+.025*math.sin(math.pi*k/9) for k in range(10)],flatten=.45)
        ob=mesh('RD_Ember_contact_splash_%d'%j,vs,fs,'rd_molten_gold',aftermath)
        register(ob,'TargetImpact','Target',start=contact-3,end=contact+23)
        def fn(f):
            t=smooth((f-contact)/23);env=smooth((f-contact+3)/6)*(1-t)
            return target+Vector((0,0,-.16*t)),(env,env,env),(0,.09*t,0)
        animate(ob,fn)
    vs=[];fs=[]
    for k in range(16):
        angle=k*2.399;r=.13+rng.random()*.20;c=Vector((math.cos(angle)*r,math.sin(angle)*r,.62+rng.random()*.16));base=len(vs);size=.027+rng.random()*.010
        for p in [c+Vector((size,0,0)),c-Vector((size,0,0)),c+Vector((0,size,0)),c-Vector((0,size,0)),c+Vector((0,0,size*1.6)),c-Vector((0,0,size*1.6))]:vs.append(tuple(p))
        fs.extend(tuple(base+n for n in tri) for tri in [(0,2,4),(0,4,3),(0,3,5),(0,5,2),(1,4,2),(1,3,4),(1,5,3),(1,2,5)])
    ob=mesh('RD_Ember_contact_motes_00',vs,fs,'rd_molten_gold',aftermath);register(ob,'TargetImpact','Target',essential=False,start=contact-3)
    def fn(f):
        t=smooth((f-contact)/(clear-contact));env=smooth((f-contact+3)/7)*(1-t)
        return target+Vector((0,0,-.34*t)),(env,env,env),(0,0,.16*t)
    animate(ob,fn)

    vs=[];fs=[];colors=[]
    for plane in range(2):
        base=len(vs)
        for r,alpha in [(0,.40),(.20,.60),(.40,.22),(.59,0)]:
            for k in range(24):
                t=k*math.tau/24
                vs.append((r*math.cos(t),r*math.sin(t) if plane==0 else 0,.67+(0 if plane==0 else r*.6*math.sin(t))))
                colors.append((1,1,1,alpha))
        for ring in range(3):
            for k in range(24):
                n=base+ring*24+k;nxt=base+ring*24+(k+1)%24;fs.append((n,nxt,nxt+24,n+24))
    ob=mesh('RD_Ember_contact_soft_puff',vs,fs,'rd_fire_glow',aftermath)
    attr=ob.data.color_attributes.new(name='SpellGlow',type='FLOAT_COLOR',domain='POINT')
    for dst,color in zip(attr.data,colors):dst.color=color
    register(ob,'TargetImpact','Target',essential=False,start=contact-4,end=contact+25)
    animate(ob,lambda f:(target,(smooth((f-contact+4)/7)*(1-smooth((f-contact)/25)),)*3,(0,0,0)))
