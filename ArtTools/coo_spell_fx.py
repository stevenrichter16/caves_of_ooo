#!/usr/bin/env python3
"""Regenerate original spell flipbooks and the complete runtime spell visual catalog.

Format: detail=6x9 16px cells (Cast, Charge, Head, Trail, Beam, Wave,
Overlay, Sigil, Mark); impact=6x2 32px cells (resolved, resisted).
Only opaque palette pixels or transparent pixels; no resampling/antialiasing.
"""
from pathlib import Path
import json
import math
import re
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Assets/Resources/Sprites/SpellFx'
INK = '#172028'
PALETTES = {
    'fire': ('#823b32','#db6537','#f3ad47','#ffdf8c'),
    'ice': ('#425876','#678fba','#a5d4df','#e2eee4'),
    'lightning': ('#666247','#afa051','#e8cf70','#fff1b2'),
    'water': ('#285e77','#388caa','#74c5ce','#c1e6dc'),
    'acid': ('#43553a','#719b47','#b3cb65','#e0e8a2'),
    'binding': ('#5e625c','#939b83','#ccd1af','#ede6c8'),
    'ink': ('#37364b','#796873','#bc8d84','#e4d7b5'),
}

def line(d, points, c, width=1): d.line([(round(x),round(y)) for x,y in points], fill=c, width=width)
def poly(d, points,c): d.polygon([(round(x),round(y)) for x,y in points],fill=c)
def ring(d,cx,cy,r,c,squash=1,step=1):
    for i in range(0,32,step):
        a=i*math.tau/32
        d.point((round(cx+math.cos(a)*r),round(cy+math.sin(a)*r*squash)),fill=c)
def star(d,cx,cy,r,c):
    line(d,[(cx-r,cy),(cx+r,cy)],c); line(d,[(cx,cy-r),(cx,cy+r)],c)
def shard(d,x,y,r,c): poly(d,[(x-r,y),(x,y-r*1.7),(x+r,y),(x,y+r)],c)
def outline(image):
    # One-pixel dark silhouette for discrete spell material, never interpolated alpha.
    mask=image.getchannel('A'); pixels=mask.load(); edge=Image.new('RGBA',image.size)
    ed=ImageDraw.Draw(edge)
    for y in range(image.height):
        for x in range(image.width):
            if pixels[x,y]:
                for dx,dy in [(-1,0),(1,0),(0,-1),(0,1)]:
                    if 0<=x+dx<image.width and 0<=y+dy<image.height and not pixels[x+dx,y+dy]: ed.point((x+dx,y+dy),INK)
    edge.alpha_composite(image)
    return edge

def detail(s,row,f):
    im=Image.new('RGBA',(16,16)); d=ImageDraw.Draw(im)
    dark,mid,light,hot=PALETTES[s]; pulse=[0,1,2,2,1,0][f]
    if row==0: # gesture/inscription: a small material gathering above a restrained binding
        line(d,[(3,11),(5,12),(9,12),(12,10)],dark)
        if s=='ink':
            poly(d,[(3,5),(7,6),(8,5),(12,4),(12,11),(8,12),(3,11)],light)
            line(d,[(8,6),(8,11)],dark)
            for y in (7,9): line(d,[(4,y),(6,y)],mid); line(d,[(9,y-1),(11,y-1)],dark)
        elif s=='binding':
            line(d,[(3,8),(6,5),(9,9),(6,10),(4,7),(9,5),(12,8)],light)
            d.point((8+f%3,3),hot)
        else:
            for i in range(4):
                a=(i*math.pi/2+f*.3); x=8+round(math.cos(a)*(5-pulse)); y=7+round(math.sin(a)*(4-pulse))
                if s=='fire': line(d,[(x,y),(x,y-1-f%2)],light)
                elif s=='ice': shard(d,x,y,1,light)
                elif s=='lightning': line(d,[(x-1,y-1),(x+1,y),(x,y+1)],hot)
                elif s=='water': d.ellipse((x,y,x+1,y+1),fill=light)
                else: d.rectangle((x,y,x+1,y+1),fill=light)
    elif row==1: # contraction/material charge
        if s in ('binding','ink'):
            r=5-pulse
            poly(d,[(8-r,8),(8,8-r),(8+r,8),(8,8+r)],dark)
            line(d,[(8-r,8),(8,8-r),(8+r,8),(8,8+r),(8-r,8)],light)
            line(d,[(5,7),(9,10),(10,6),(5,9)],mid)
        elif s=='lightning':
            line(d,[(2,5),(6,7),(4,10),(9,9),(8,3),(12,5)],light); star(d,8,8,1+pulse//2,hot)
        else:
            for i in range(5):
                a=i*math.tau/5+(f*.4 if s in ('water','fire','acid') else 0); r=5-pulse
                x=8+round(math.cos(a)*r); y=8+round(math.sin(a)*r)
                if s=='ice': shard(d,x,y,1,light)
                elif s=='fire': poly(d,[(x-1,y+1),(x,y-2),(x+1,y+1)],light)
                else: d.ellipse((x-1,y-1,x+1,y+1),outline=light)
            d.rectangle((7,7,8,8),fill=hot)
    elif row==2: # right-facing head, school-specific silhouette
        if s=='fire':
            poly(d,[(2,4+f%2),(7,5),(6,3),(11,6),(14,8),(10,11),(3,12),(5,9),(1,8),(6,7)],mid)
            poly(d,[(6,6),(10,7),(12,8),(8,10),(5,9),(8,8)],light); d.point((10,8),hot)
        elif s=='ice':
            poly(d,[(2,7),(10,4),(14,8),(10,11),(2,9),(6,8)],mid)
            line(d,[(3,8),(13,8),(10,5)],hot); line(d,[(7,6),(9,10)],light)
        elif s=='lightning':
            poly(d,[(2,5),(10,6),(8,8),(14,8),(6,12),(8,9),(2,9),(5,7)],light)
            line(d,[(4,6),(8,7),(6,9),(11,8)],hot); d.point((2,f%2+11),mid)
        elif s=='water':
            poly(d,[(3,5),(8,5),(14,8),(8,11),(3,11),(1,8)],mid)
            d.ellipse((3,6,8,10),fill=light); line(d,[(5,6),(8,6),(11,8)],hot)
            d.point((1,4+f%3),light)
        elif s=='acid':
            for x,y,r in [(11,8,2),(6,6+f%2,2),(5,10,1),(1,8,1)]:
                d.ellipse((x-r,y-r,x+r,y+r),fill=mid,outline=light); d.point((x,y-1),hot)
        elif s=='binding':
            line(d,[(1,5),(6,6),(10,10),(12,8),(9,5),(6,9),(1,10)],light)
            line(d,[(6,6),(10,10),(12,8)],hot); d.point((13,7),mid)
        else:
            poly(d,[(3,4),(11,6),(14,8),(11,10),(3,12),(6,8)],light)
            line(d,[(5,10),(11,7),(13,8)],dark); d.point((2,6+f%4),mid)
    elif row==3: # trail with discontinuity to retain terrain contrast
        if s=='ice':
            for x in (3,7,11): shard(d,x,7+(x+f)%3,1,light if x>5 else dark)
        elif s=='lightning': line(d,[(1,8),(4,7+f%2),(7,9),(10,7),(14,8)],light)
        elif s in ('binding','ink'): line(d,[(2,9),(5,8),(7,10),(11,7),(13,8)],mid); d.point((10,6),light)
        else:
            for x in (3,7,11):
                y=8+((x+f)%3-1)
                if s=='fire': line(d,[(x,y),(x,y-1-f%2)],light if x>5 else mid)
                else: d.ellipse((x,y,x+1,y+1),fill=light if x>5 else mid)
    elif row==4: # connecting stream/beam with branch or scalloped edges
        if s=='lightning':
            line(d,[(0,8),(3,5+f%2),(6,10),(9,7),(12,9),(15,8)],light)
            line(d,[(6,10),(7,13),(10,14)],mid); line(d,[(9,7),(11,3)],hot)
        elif s=='ice':
            line(d,[(0,8),(15,8)],light)
            for x in (3,8,13): line(d,[(x-2,5),(x,8),(x-1,11)],mid)
        elif s in ('binding','ink'):
            line(d,[(0,8),(4,6),(8,10),(12,6),(15,8)],light); line(d,[(0,8),(4,10),(8,6),(12,10),(15,8)],mid)
        else:
            line(d,[(0,8),(15,8)],mid,3)
            line(d,[(0,7),(4,7+f%2),(8,7),(12,6+f%2),(15,7)],light)
            for x in (3,9,13):
                if s=='fire': poly(d,[(x-1,7),(x,3+(f+x)%3),(x+2,8)],mid)
                elif s=='acid': d.ellipse((x-1,10,x+1,12),outline=light)
                else: d.point((x,10+f%2),light)
    elif row==5: # cell-aligned wave
        r=2+f
        if s=='ice': line(d,[(8-r,8),(8,8-r),(8+r,8),(8,8+r),(8-r,8)],light)
        elif s=='lightning':
            line(d,[(8-r,8),(6,7),(8,8-r),(9,6),(8+r,8),(10,9),(8,8+r),(7,10),(8-r,8)],light)
        elif s in ('binding','ink'):
            d.rectangle((8-r,8-r,8+r,8+r),outline=mid); line(d,[(8-r,8),(8+r,8)],light)
        else:
            ring(d,8,8,r,light,.55 if s=='water' else .8,2 if s=='fire' else 1)
            if s=='acid': d.point((8+r,6),hot)
    elif row==6: # brief material residue; no alpha fading
        if s=='fire':
            for x,y in [(3,10),(5,12),(8,11),(11,12),(12,9)]: d.point((x,y),dark)
            d.point((5+f,9-f//2),light if f<3 else mid)
        elif s=='ice':
            line(d,[(2,11),(5,9),(8,12),(11,9),(14,11)],mid)
            for x in (4,8,12): line(d,[(x,12),(x,9-f%2)],light)
        elif s=='water':
            ring(d,8,11,5,mid,.35); line(d,[(4+f%3,10),(8+f%3,10)],light)
        elif s=='acid':
            for x,y in [(4,10),(9,11),(12,9)]: d.ellipse((x,y,x+1,y+1),outline=mid)
            line(d,[(3,12),(6,13),(8,12),(11,13),(13,11)],dark)
        elif s=='lightning': line(d,[(2,12),(6,10),(8,12),(11,9),(13,11)],mid); d.point((7+f%3,11),light)
        else:
            line(d,[(3,11),(12,11)],mid)
            for x in (4,7,10): line(d,[(x,9),(x+1,12)],dark)
    elif row==7: # ward / ledger inscription
        if s=='ink':
            d.rectangle((3,3,12,12),outline=mid)
            line(d,[(6,4),(6,11)],light)
            for y in (5,8,10): line(d,[(8,y),(11-(f+y)%2,y)],light)
            if f>1: line(d,[(4,10),(11,4)],dark)
        elif s=='binding':
            line(d,[(8,2),(13,5),(11,11),(8,14),(4,11),(3,5),(8,2)],light)
            line(d,[(5,6),(10,10),(11,6),(6,10),(5,6)],mid); d.point((8,7),hot)
        elif s=='ice':
            for i in range(6):
                a=i*math.tau/6; line(d,[(8,8),(8+math.cos(a)*6,8+math.sin(a)*6)],light)
            shard(d,8,8,2,mid)
        elif s=='lightning':
            line(d,[(5,3),(11,3),(7,7),(11,7),(5,13),(7,9),(4,9),(5,3)],light)
        elif s=='fire':
            poly(d,[(4,11),(4,8),(6,9),(7,3),(10,8),(11,6),(12,11),(9,13),(6,13)],mid)
            poly(d,[(6,11),(8,7),(10,12),(8,13)],light)
        elif s=='water': ring(d,8,9,5,mid,.7); line(d,[(4,8),(6,6),(9,7),(11,5)],light)
        else:
            d.ellipse((4,4,11,11),outline=light)
            for x,y in [(6,6),(10,10),(3,11)]: d.point((x,y),hot)
    else: # consumed mark / expenditure tick; only drawn for actual consumption
        line(d,[(5,3),(10,3),(10,11),(5,11),(5,3)],mid)
        if f<3: line(d,[(6,5),(9,5),(7,8),(9,9)],light)
        else:
            line(d,[(4,10),(11,4)],light)
            d.point((3,4+f%2),light); d.point((12,10),mid)
    return outline(im)

def impact(s,f,resisted):
    im=Image.new('RGBA',(32,32)); d=ImageDraw.Draw(im); dark,mid,light,hot=PALETTES[s]
    r=[4,7,10,12,11,10][f]
    if resisted:
        # A split boundary and detached marks conveys a check even in monochrome.
        line(d,[(13,5),(8,8),(7,17),(12,25)],light)
        line(d,[(19,5),(24,8),(25,17),(20,25)],mid)
        for x,y in [(12,12),(19,17),(14,21)]: line(d,[(x-1,y-1),(x+1,y+1)],hot)
        if f>2: d.point((4+f,4),mid)
    elif s=='fire':
        for i in range(7):
            a=i*math.tau/7+f*.13; x=16+math.cos(a)*r*.8; y=17+math.sin(a)*r*.6
            poly(d,[(x-2,y+2),(x-1,y-3-f%2),(x+1,y),(x+3,y-2),(x+2,y+3)],mid)
            line(d,[(x,y),(x,y-2)],light)
        if f<3: poly(d,[(12,18),(14,12),(16,14),(18,10),(20,19),(16,21)],light); d.point((16,16),hot)
        for i in range(4): d.point((5+i*6,5+(i+f)%4),hot if f<4 else dark)
    elif s=='ice':
        for i in range(6):
            a=i*math.tau/6; x=16+math.cos(a)*r; y=16+math.sin(a)*r
            shard(d,x,y,2 if f<4 else 1,light); line(d,[(16,16),(x,y)],mid)
        if f<4: shard(d,16,16,3,hot)
        ring(d,16,16,r+2,dark,1,4)
    elif s=='lightning':
        for i in range(5):
            a=i*math.tau/5+.2; end=(16+math.cos(a)*r,16+math.sin(a)*r)
            midp=(16+math.cos(a+.25)*r*.55,16+math.sin(a+.25)*r*.55)
            line(d,[(16,16),midp,(midp[0]-2,midp[1]+1),end],light)
            line(d,[midp,(midp[0]+3,midp[1]-3)],mid)
        if f<3: star(d,16,16,2,hot)
        ring(d,16,20,r,mid,.35,3)
    elif s=='water':
        ring(d,16,20,r,light,.45); ring(d,16,20,max(2,r-3),mid,.45)
        for i in range(6):
            a=i*math.pi/5+math.pi; x=16+math.cos(a)*r; y=19+math.sin(a)*r-f//2
            d.ellipse((round(x)-1,round(y)-2,round(x)+1,round(y)+1),fill=mid)
            d.point((round(x),round(y)-1),hot)
        if f<3: line(d,[(11,18),(14,13),(17,17),(20,12),(22,19)],light)
    elif s=='acid':
        ring(d,16,20,r,mid,.45,2)
        for i in range(6):
            a=i*math.tau/6+.4; x=round(16+math.cos(a)*r*.8); y=round(16+math.sin(a)*r*.7-f//2)
            rr=2 if (i+f)%3 else 1
            d.ellipse((x-rr,y-rr,x+rr,y+rr),outline=light); d.point((x,y-rr),hot)
        line(d,[(8,24),(12,23),(16,25),(20,23),(24,24)],dark)
    elif s=='binding':
        for i in range(4):
            inset=f//2+i*2
            if i%2==0: line(d,[(16,5+inset),(27-inset,16),(16,27-inset),(5+inset,16),(16,5+inset)],light if i==0 else mid)
        line(d,[(10,13),(20,20),(22,11),(12,21),(10,13)],hot if f<2 else mid)
    else:
        # Ruled marginalia closes around the debt; torn corners make the late frames.
        x0=16-r; x1=16+r
        line(d,[(x0,8),(x0,25),(x1,25),(x1,8)],mid)
        line(d,[(x0+2,7),(x1-2,7)],light)
        line(d,[(12,9),(12,23)],dark)
        for j,y in enumerate((11,15,19,22)):
            line(d,[(14,y),(22-(j+f)%3,y)],light if j==f%4 else mid)
        if f>1: line(d,[(10,23),(23,10)],hot)
        for i in range(4): d.point((5+i*7,5+(f+i)%3),mid)
    return outline(im)

FAMILIES={
 'Projectile':'Kindle IceLance ArcBolt Quench AcidSpray Calm EmberSpit',
 'Beam':'EmberVein RailSpike', 'Chain':'Overload', 'GroundLine':'GroundSurge Oilmark Undertow',
 'Cone':'FlameJet Backdraft JetBlast ShatteredRime', 'Lob':'DrenchLob ConjureWater',
 'Ring':'Conflagration RimeNova Thunderclap ColdSnap BacklashCoil StormAnvil RenderedSteam VerdigrisBloom SunderingWord',
 'Field':'GlacialWall Hearthwarm ConjureRain ScaldingVeil',
 'Inscription':'Pyroclasm Frostbind RimeGrip HangingBolt StillHeart HollowCoin BloodletterLedger Fulmination KindleFlame FlamingHands',
 'Ward':'HeartFlame Hibernate ArcaneSurge LeyTap WardGleam ChillDraft DryingBreeze',
}
SCHOOLS=dict(zip(('Pyromancy','Cryomancy','Galvanism','Hydromancy','Corrosion','Spellcraft','Rites'),PALETTES))
RITE_IMPACT={'StormAnvil':'lightning','HangingBolt':'lightning','RenderedSteam':'water','ScaldingVeil':'water','Fulmination':'lightning','ShatteredRime':'ice','StillHeart':'ice','VerdigrisBloom':'acid'}

def metadata(path):
    target=Path(str(path)+'.meta')
    if not target.exists():
        text='fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,str(path.relative_to(ROOT))).hex+'\n'
        if path.suffix == '.png':
            text += 'TextureImporter:\n  textureType: 8\n  textureShape: 1\n  spriteMode: 1\n  nPOTScale: 0\n'
        target.write_text(text)

def main():
    OUT.mkdir(parents=True,exist_ok=True); metadata(OUT)
    contact=Image.new('RGBA',(6*32+112,7*64),(24,30,31,255)); cd=ImageDraw.Draw(contact)
    for y,s in enumerate(PALETTES):
        sheet=Image.new('RGBA',(96,144))
        for row in range(9):
            for f in range(6): sheet.paste(detail(s,row,f),(f*16,row*16))
        dest=OUT/f'{s}_detail.png'; sheet.save(dest); metadata(dest)
        sheet=Image.new('RGBA',(192,64))
        for row in range(2):
            for f in range(6): sheet.paste(impact(s,f,row==1),(f*32,row*32))
        dest=OUT/f'{s}_impact.png'; sheet.save(dest); metadata(dest)
        cd.text((6,y*64+4),s,fill=PALETTES[s][2]); contact.alpha_composite(sheet,(112,y*64))
        for f in range(6): contact.alpha_composite(detail(s,2,f),(6+f*16,y*64+24))
    definitions=[]
    for prefix,school in SCHOOLS.items():
        data=json.loads((ROOT/f'Assets/Resources/Content/Data/Skills/{prefix}.json').read_text())
        for tree in data['Skills']:
            for power in tree.get('Powers',[]):
                cls=power['Class']; source=(ROOT/f'Assets/Scripts/Gameplay/Skills/{cls}.cs').read_text()
                if not ('DeclareActivatedAbility(' in source or re.search(r': (ProjectileSpellSkillBase|ConsumingRiteSkillBase)',source)): continue
                spell=cls.split('_',1)[1]; family=next((k for k,v in FAMILIES.items() if spell in v.split()),None)
                if family is None: raise ValueError('Explicit family required: '+cls)
                domestic=spell in ('KindleFlame','Hearthwarm','ChillDraft','ConjureWater','DryingBreeze','WardGleam')
                rite=prefix=='Rites'; payoff=RITE_IMPACT.get(spell,school) if rite else school
                definitions.append(dict(ID=cls,School=school,Family=family,DetailSheet=f'Sprites/SpellFx/{school}_detail',ImpactSheet=f'Sprites/SpellFx/{payoff}_impact',CastDuration=.08 if domestic else .12,ChargeDuration=.05 if domestic else .18 if rite else .1,StepDuration=.025,ImpactDuration=.14 if domestic else .25 if rite else .2,AftermathDuration=.1 if domestic else .18,Domestic=domestic,IsRite=rite,ReverseTravel=spell=='Undertow',Variant=len(definitions)%6))
    for cls,school in [('Pyromancy_ScorchRetort','fire'),('Cryomancy_FrostRetort','ice'),('Galvanism_ShockRetort','lightning'),('Corrosion_AcidRetort','acid')]:
        definitions.append(dict(ID=cls,School=school,Family='Inscription',DetailSheet=f'Sprites/SpellFx/{school}_detail',ImpactSheet=f'Sprites/SpellFx/{school}_impact',CastDuration=0,ChargeDuration=0,StepDuration=.025,ImpactDuration=.12,AftermathDuration=.06,Domestic=True))
    dest=ROOT/'Assets/Resources/Content/Data/SpellVisuals.json'; dest.write_text(json.dumps({'Definitions':definitions},indent=2)+'\n'); metadata(dest)
    preview=ROOT/'Docs/SpellFx-atlas-preview.png'; contact.resize((contact.width*3,contact.height*3),Image.Resampling.NEAREST).save(preview)
    print(f'Generated {len(definitions)} spell definitions and 14 registered pixel sheets.')

if __name__=='__main__': main()
