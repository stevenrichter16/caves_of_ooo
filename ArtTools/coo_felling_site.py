#!/usr/bin/env python3
"""Original precise-palette 16x16 ground art for W6.5."""
from pathlib import Path
import re, uuid
from PIL import Image, ImageDraw
from coo_stump_bestiary import outlined, INK
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Resources/Sprites/Environment'

def bare(v):
    im=Image.new('RGBA',(16,16)); d=ImageDraw.Draw(im)
    edges=[[(2,5),(5,3),(11,4),(13,8),(11,12),(4,12),(2,10)],
           [(2,6),(6,3),(11,4),(13,9),(10,13),(3,11)],
           [(3,4),(9,3),(13,6),(12,11),(7,13),(2,10)],
           [(2,7),(4,4),(10,3),(13,7),(11,12),(5,13),(2,10)]][v]
    d.polygon(edges,fill=(145,113,98)); d.line([(4,6),(7,5),(11,6)], fill=(200,169,142))
    d.line([(4,10),(8,11),(11,10)],fill=(95,79,69))
    d.line([(5,8),(10,8)], fill=(168,135,114))
    d.point((6+v,7),fill=(219,187,155)); return outlined(im)

def scar(v):
    im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
    paths=[[(2,10),(4,8),(8,7),(13,7)],[(2,8),(5,7),(9,7),(13,5)],
           [(2,6),(6,7),(10,8),(13,10)],[(3,4),(4,7),(7,10),(12,11)]]
    d.line(paths[v],fill=(102,80,72),width=2)
    d.line([(x,y-2) for x,y in paths[v]],fill=(191,158,130))
    d.line([(x,y+2) for x,y in paths[v]][1:3],fill=(139,112,96))
    return outlined(im)

def seventh():
    im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
    # Stone edges fail to meet. The transparent center remains unoccupied.
    d.line([(2,5),(5,4),(7,5)],fill=(194,163,140))
    d.line([(9,3),(12,4),(13,6)],fill=(141,123,128))
    d.line([(2,10),(4,12),(7,11)],fill=(123,101,95))
    d.line([(10,13),(12,11),(13,9)],fill=(215,207,180))
    d.point((8,13),fill=(170,163,170));return outlined(im)

def main():
    art={}
    for name,fn in [('felling_bare_position',bare),('felling_scar',scar)]:
        for v in range(4):art[name+('' if v==0 else '_v'+str(v))]=fn(v)
    art['seventh_position']=seventh()
    template=(OUT/'cascade_father.png.meta').read_text()
    sheet=Image.new('RGB',(160*len(art),195),(46,49,42));d=ImageDraw.Draw(sheet)
    for i,(name,im) in enumerate(art.items()):
        assert set(im.getchannel('A').tobytes()) <= {0,255}
        p=OUT/(name+'.png');im.save(p);meta=Path(str(p)+'.meta')
        if not meta.exists():meta.write_text(re.sub(r'(?m)^guid: [0-9a-f]+$','guid: '+uuid.uuid4().hex,template))
        scaled=im.resize((160,160),Image.Resampling.NEAREST);sheet.paste(scaled,(i*160,0),scaled)
        d.text((i*160+2,169),name.replace('_',' '),fill=(226,214,191))
    sheet.save(ROOT/'Docs/Verification/FellingW6/W65-ground-art.png')
if __name__=='__main__':main()
