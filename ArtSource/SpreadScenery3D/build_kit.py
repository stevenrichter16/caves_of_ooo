"""Original, deterministic single-cell scenery. No borrowed gameplay or RNG state.
Regenerates only this directory's kit.json; Unity importer owns persistent assets.
"""
from pathlib import Path
import json, math
PALETTE=['#082C28','#103E36','#1A4B40','#26594A','#A0A77C','#CBC697','#647353','#235D25','#40872C','#65AE3D','#435A53','#62786C','#819489','#16883B','#45CB4B','#A0E772','#207838','#D2D3B4','#B77B43','#403D28','#756C40','#A39456','#C4B877','#243E39']
BLUEPRINTS=['BerryBush','Signpost','HollowStump','Beehive','RiverShrine','FlowerField','StoneFloor','StoneWall','Chair','Bed','Well','Oven','WatchLantern','CampfireGroundMarker','WellGroundMarker','OvenGroundMarker','LanternGroundMarker','Shrine','AlchemyShelf','AlchemyStill','TinkersForge','OldStump','PressurePlate','BearTrap','FireTrap','SpikeTrap','WeaponRack','KnotflaxSnare','CrackedGlowQuartz']
def model(bp,v,unlit=False,jammed=False):
    boxes=[]
    def b(x,y,z,sx,sy,sz,c):
        boxes.append(dict(center=[round(x,5),round(y,5),round(z,5)],size=[round(sx,5),round(sy,5),round(sz,5)],color=c))
    def legs(w,d,h,c=19):
        for x in [-w,w]:
            for z in [-d,d]: b(x,h/2,z,.075,h,.075,c)
    def ring(r,y,sy,width,c,n=8):
        for i in range(n):
            a=(i+.5*v)*math.tau/n
            b(math.cos(a)*r,y,math.sin(a)*r,width,sy,width,c+(i%2) if c in (10,19,21) else c)
    def stump(quest=False):
        ring(.23,.2,.4,.16,19)
        # Open center deliberately remains empty and dark; no fake container lid.
        ring(.22,.415,.045,.16,21)
        for x,z in [(-.32,.04),(.28,.23),(.13,-.31)]: b(x,.055,z,.17,.11,.13,19)
        b(-.22,.49,.10,.09,.16,.11,20)
        if quest: b(.21,.465,-.07,.13,.10,.14,21)
    if bp=='BerryBush':
        b(0,.18,0,.10,.36,.11,19)
        for i,(x,z,s) in enumerate([(-.18,-.12,.34),(.18,-.12,.36),(0,.16,.43)]):
            b(x,.28+i*.03,z,s,.34,s,7+i%3)
            b(x-.03,.48+i*.02,z-.04,s*.6,.09,s*.58,9)
        for i in range(9):
            a=i*2.4+v*.6; b(math.cos(a)*.29,.35+(i%3)*.075,math.sin(a)*.28,.055,.055,.055,18)
    elif bp=='Signpost':
        b(-.03,.47,0,.10,.94,.12,19);b(0,.78,-.02,.78,.24,.11,20)
        b(.03,.58,.0,.60,.14,.1,21);b(-.31,.78,-.08,.07,.04,.025,19)
        b(.24,.59,-.06,.025,.11,.025,18)
    elif bp in ['HollowStump','OldStump']: stump(bp=='OldStump')
    elif bp=='Beehive':
        b(-.21,.38,.10,.08,.76,.09,19);b(-.02,.72,.10,.42,.07,.09,20)
        for y,w in [(.25,.25),(.33,.39),(.42,.43),(.51,.35),(.59,.21)]: b(.10,y,.05,w,.085,w*.78,21 if y<.5 else 22)
        b(.11,.35,-.126,.12,.05,.025,19)
    elif bp in ['RiverShrine','Shrine']:
        ring(.29,.085,.17,.17,10)
        b(0,.19,.06,.42,.20,.32,11);b(-.03,.40,.09,.28,.25,.24,10)
        b(-.04,.57,.09,.36,.09,.3,12)
        if bp=='RiverShrine':
            b(.16,.09,-.24,.20,.065,.15,5);b(-.17,.10,-.23,.09,.11,.12,4)
        else:b(.12,.32,-.1,.12,.09,.13,17)
    elif bp=='FlowerField':
        for i in range(13):
            a=i*2.399+v; r=.11+.25*(i%5)/4;x,z=math.cos(a)*r,math.sin(a)*r;h=.14+(i%3)*.04
            b(x,h/2,z,.024,h,.026,7)
            b(x,h,z,.10,.027,.04,5 if i%3 else 17);b(x,h+.001,z,.04,.027,.10,5 if i%3 else 17)
            b(x,h+.02,z,.032,.025,.033,18)
    elif bp=='StoneFloor':
        for iz in range(3):
            for ix in range(3): b((ix-1)*.321,.016+(ix+iz)%2*.002,(iz-1)*.321,.31,.031,.31,10+(ix+iz+v)%3)
    elif bp=='StoneWall':
        for iy in range(4):
            for ix in range(3): b((ix-1)*.31,.145+iy*.275,0,.303,.26,.31+.03*((ix+iy+v)%2),10+(ix+iy)%3)
        for ix in range(3):b((ix-1)*.31,1.125,.015,.31,.05,.36,12)
    elif bp=='Chair':
        legs(.23,.22,.38);b(0,.40,0,.57,.10,.57,20)
        for x in [-.23,.23]:b(x,.66,.23,.08,.52,.08,19)
        for y in [.64,.85]:b(0,y,.23,.48,.08,.07,21)
    elif bp=='Bed':
        legs(.30,.39,.24);b(0,.25,0,.73,.11,.91,19);b(0,.35,0,.65,.12,.82,5)
        b(0,.428,-.14,.65,.035,.47,6);b(0,.437,.27,.50,.08,.22,17)
        b(0,.41,.44,.73,.33,.07,20)
    elif bp=='Well':
        ring(.28,.18,.36,.20,10);ring(.30,.38,.07,.21,12)
        for x in [-.32,.32]:b(x,.74,0,.07,.77,.08,19)
        b(0,1.14,0,.77,.08,.12,20);b(0,.84,0,.018,.55,.02,21)
        b(.08,.34,-.08,.10,.11,.1,19)
    elif bp=='Oven':
        b(0,.13,0,.74,.26,.68,10);b(0,.43,.13,.72,.36,.43,11)
        for x in [-.27,.27]:b(x,.38,-.2,.18,.34,.23,11)
        b(0,.59,-.14,.72,.13,.38,12);b(0,.66,.04,.50,.08,.39,10)
        b(.17,.83,.17,.17,.32,.16,10);b(.17,1.0,.17,.22,.05,.21,12)
    elif bp=='WatchLantern':
        b(0,.06,0,.24,.12,.24,10);b(0,.53,0,.07,.96,.08,19)
        b(.05,1.06,0,.27,.045,.08,20)
        b(.13,.90,0,.17,.23,.16,19);b(.13,.90,-.086,.11,.17,.016,20 if unlit else 22)
        b(.13,1.035,0,.23,.055,.22,10);b(.13,.77,0,.21,.035,.20,10)
    elif bp.endswith('GroundMarker'):
        # Flat stains/stonework only: this marker is not a usable fixture.
        if bp=='CampfireGroundMarker':
            ring(.25,.022,.034,.09,10);b(0,.012,0,.37,.02,.31,19)
            b(.07,.036,0,.35,.025,.06,20);b(-.1,.034,.08,.07,.025,.30,19)
        elif bp=='WellGroundMarker':ring(.28,.023,.035,.14,10);b(.22,.042,-.17,.07,.016,.20,12)
        elif bp=='OvenGroundMarker':
            for x in [-.25,0,.25]:b(x,.02,.07,.23,.035,.53,10)
            b(0,.042,-.17,.31,.015,.15,19)
        else:
            b(0,.022,0,.28,.035,.25,10);b(.03,.045,0,.10,.018,.10,20)
            b(-.18,.017,.12,.14,.025,.07,6)
    elif bp=='AlchemyShelf':
        for x in [-.34,.34]:b(x,.51,.20,.075,1.02,.15,19)
        for y in [.10,.47,.83]:b(0,y,.06,.76,.065,.40,20)
        for i in range(7):
            x=-.26+(i%4)*.17;y=.20+(i//4)*.37;z=.02
            b(x,y,z,.09,.15,.10,4+i%3);b(x,y+.095,z,.05,.04,.05,19)
    elif bp=='AlchemyStill':
        legs(.22,.20,.27);b(0,.29,0,.60,.07,.53,20)
        b(-.13,.48,0,.26,.31,.29,21);b(-.13,.665,0,.13,.08,.14,22)
        b(.08,.65,0,.31,.04,.04,18);b(.235,.50,0,.05,.30,.05,18)
        b(.23,.355,.03,.14,.09,.16,11)
    elif bp=='TinkersForge':
        b(0,.16,0,.65,.32,.52,10);b(0,.36,0,.55,.09,.43,12)
        b(0,.46,0,.22,.15,.23,10);b(.02,.58,0,.54,.10,.29,11)
        b(.31,.58,0,.16,.06,.15,12);b(-.27,.085,-.30,.18,.17,.16,19)
    elif bp=='PressurePlate':
        b(0,.035,0,.71,.055,.71,10);b(0,.075,0,.51,.028,.49,12)
        for x,z in [(-.27,-.27),(.27,.27)]:b(x,.07,z,.05,.025,.05,18)
    elif bp=='BearTrap':
        b(0,.035,0,.51,.05,.36,10)
        for x in [-.23,.23]:
            b(x,.09,0,.06,.07,.46,11)
            for z in [-.18,-.06,.06,.18]:b(x*.83,.145,z,.045,.07,.055,12)
        b(0,.075,0,.18,.02,.15,20);b(.33,.04,.13,.16,.025,.05,10)
    elif bp=='FireTrap':
        b(0,.06,0,.61,.11,.47,10)
        for x in [-.18,0,.18]:b(x,.13,0,.065,.06,.19,19)
        b(-.29,.075,.08,.055,.04,.15,18)
    elif bp=='SpikeTrap':
        b(0,.035,0,.64,.06,.61,10)
        for x in [-.20,0,.20]:
            for z in [-.19,.01,.21]:
                b(x,.095,z,.07,.085,.07,11);b(x,.158,z,.033,.055,.033,12)
    elif bp=='WeaponRack':
        for x in [-.33,.33]:b(x,.53,.12,.08,1.06,.10,19)
        for y in [.24,.88]:b(0,y,.12,.75,.08,.10,20)
        # Empty pegs. Loot remains real separate native stock, never fake weapons.
        for x in [-.22,0,.22]:b(x,.79,.025,.055,.06,.14,21)
        for x in [-.30,.30]:b(x,.06,0,.13,.12,.56,19)
    elif bp=='CrackedGlowQuartz':
        # Two separated fractured crystals with pale cut faces. Their dark
        # bases leave a clear crack; illumination belongs to the real Part.
        b(-.13,.08,-.03,.19,.16,.23,10)
        b(-.14,.22,-.04,.14,.18,.17,12)
        b(-.14,.33,-.05,.09,.06,.11,17)
        b(-.17,.23,-.131,.06,.14,.021,17)
        b(.115,.065,.05,.18,.13,.21,10)
        b(.115,.17,.05,.13,.14,.15,12)
        b(.12,.26,.048,.08,.05,.09,17)
        b(.15,.17,-.029,.055,.11,.019,5)
        b(.025,.03,-.18,.06,.06,.08,12)
        b(-.05,.025,.18,.09,.05,.06,17)
    elif bp=='KnotflaxSnare':
        # A broad pale, open noose at ankle height. Its two small stakes and
        # folded tail distinguish cord from the adjacent metal trap families.
        # The renderer adds no collider; only the actual CordSnarePart catches.
        for i in range(20):
            a=i*math.tau/20
            b(math.cos(a)*.25,.055,math.sin(a)*.25,.091,.047,.091,5 if i%4==0 else 22)
        for x,z in [(-.34,-.20),(.34,.20)]:
            b(x,.12,z,.072,.24,.079,19)
            b(x,.25,z,.080,.035,.085,21)
            b(x,.14,z,.096,.035,.101,5)
        b(-.294,.085,-.20,.12,.047,.052,22)
        b(.29,.085,.20,.14,.047,.052,22)
        b(-.345,.055,-.32,.046,.039,.20,5)
        b(-.277,.055,-.397,.18,.039,.046,22)
        b(-.20,.055,-.355,.046,.039,.13,22)
    else:raise ValueError(bp)
    if jammed:
        # The same physical mechanism, visibly arrested by a spent length of
        # timber. Broad tan boards stay legible at the ordinary camera scale.
        if bp=='SpikeTrap':
            for box in boxes[1:]:
                box['center'][1]=round(box['center'][1]*.45,5)
                box['size'][1]=round(box['size'][1]*.45,5)
            b(0,.12,.005,.79,.085,.21,21)
            b(0,.169,-.057,.76,.013,.025,22)
            b(.365,.12,.005,.033,.088,.215,18)
        elif bp=='BearTrap':
            b(0,.17,-.045,.77,.10,.19,21)
            b(-.065,.228,-.10,.58,.018,.025,22)
            b(-.37,.17,-.045,.035,.105,.195,18)
            b(.075,.23,.037,.065,.035,.09,19)
        elif bp=='FireTrap':
            # Block the pressure actuator outside the three vents. This is
            # not a wooden plug placed in an emitted flame.
            b(0,.145,.20,.77,.085,.16,21)
            b(.01,.195,.164,.69,.015,.028,22)
            b(.37,.145,.20,.03,.09,.165,18)
            b(-.22,.109,.105,.065,.10,.15,11)
        elif bp=='PressurePlate':
            boxes[1]['center'][1]=.118
            b(0,.077,-.24,.79,.09,.18,21)
            b(-.02,.129,-.28,.73,.016,.04,22)
            b(.025,.06,-.125,.71,.065,.065,20)
            b(-.375,.077,-.24,.032,.095,.185,18)
        else:raise ValueError('No jammed state for '+bp)
    # Distinct tiny surface trim per variant without simulation random draws.
    if v:
        for box in boxes:
            box['center'][0]=-box['center'][0]
        q=boxes[0]; q['center'][1]=round(q['center'][1]+.002,5)
    return {'id':'spread-scenery-'+bp.lower()+('-jammed' if jammed else '-unlit' if unlit else '')+'-'+str(v),'boxes':boxes}
models=[model(bp,v) for bp in BLUEPRINTS for v in range(2)]
models += [model('WatchLantern',v,True) for v in range(2)]
models += [model(bp,v,jammed=True) for bp in ['SpikeTrap','BearTrap','FireTrap','PressurePlate'] for v in range(2)]
out={'schemaVersion':1,'palette':PALETTE,'models':models}
Path(__file__).with_name('kit.json').write_text(json.dumps(out,indent=2)+'\n')
print(f'{len(models)} models, {sum(len(m["boxes"]) for m in models)} boxes')
