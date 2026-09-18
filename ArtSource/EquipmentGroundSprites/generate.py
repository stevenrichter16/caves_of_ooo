"""Seven exact ground-equipment silhouettes. Pillow; no resampling/antialiasing.
Run from any directory; writes only this kit's PNGs and new GUID-only meta copies.
Optional --preview PATH writes a nearest-neighbor contact sheet outside Assets.
"""
from pathlib import Path
import argparse
import re
import uuid
from PIL import Image, ImageDraw
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Resources/Sprites/Environment'
INK = (30, 32, 28, 255)
BODY = (159, 166, 151, 255)
LIGHT = (219, 224, 207, 255)

def sprite(kind):
    im = Image.new('RGBA', (16, 16))
    d = ImageDraw.Draw(im)
    def poly(points, fill=BODY): d.polygon(points, fill=fill, outline=INK)
    def rect(box, fill=BODY): d.rectangle(box, fill=fill, outline=INK)
    if kind == 'dagger':
        # Short leaf blade, wide quillon, wrapped grip and square pommel.
        poly([(12,2),(13,3),(12,6),(8,10),(6,8),(9,4)])
        d.line([(11,4),(8,7)], fill=LIGHT)
        poly([(5,7),(9,11),(8,12),(4,8)], LIGHT)
        poly([(5,10),(6,11),(4,13),(3,12)])
        rect((2,12,4,14), LIGHT)
    elif kind == 'sword':
        # Long parallel blade occupies most of the diagonal; pointed tip.
        poly([(13,1),(14,2),(13,5),(6,12),(3,9),(10,2)])
        d.line([(12,3),(5,10)], fill=LIGHT)
        poly([(2,8),(7,13),(6,14),(1,9)], LIGHT)
        poly([(3,11),(4,12),(2,14),(1,13)])
        d.point((1,14), fill=LIGHT)
    elif kind == 'spear':
        # Narrow continuous haft and visibly separate broad spearhead.
        poly([(9,5),(10,6),(3,14),(1,14),(1,13)])
        d.line([(8,7),(3,12)], fill=LIGHT)
        poly([(13,1),(14,1),(14,4),(11,7),(8,7),(8,4)], LIGHT)
        d.line([(12,3),(10,5)], fill=BODY)
        rect((7,7,9,8))
    elif kind == 'boots':
        # Two distinct L-shaped boots, clear ankle openings and flat soles.
        poly([(2,3),(6,3),(6,9),(8,10),(8,13),(1,13),(1,10),(2,9)])
        poly([(9,2),(13,2),(13,8),(15,9),(15,12),(8,12),(8,9),(9,8)])
        d.line([(3,4),(5,4)],fill=LIGHT); d.line([(10,3),(12,3)],fill=LIGHT)
        d.line([(2,11),(6,11)],fill=LIGHT);d.line([(9,10),(13,10)],fill=LIGHT)
    elif kind == 'gloves':
        # Paired mitten gauntlets: broad cuffs, round fingers and inward thumbs.
        poly([(2,2),(4,1),(6,2),(6,6),(8,5),(8,8),(6,10),(6,13),(2,13),(2,9),(1,7),(1,3)])
        poly([(11,3),(13,2),(14,3),(15,5),(14,9),(13,10),(13,14),(9,14),(9,10),(8,8),(9,6),(10,7),(10,4)])
        d.line([(2,11),(5,11)],fill=LIGHT);d.line([(10,12),(12,12)],fill=LIGHT)
        d.line([(3,3),(3,6)],fill=LIGHT);d.line([(12,4),(12,7)],fill=LIGHT)
    elif kind == 'helmet':
        # Domed cap with side protection, open face and a central nose guard.
        poly([(5,2),(10,2),(12,4),(13,7),(13,12),(11,13),(10,9),(5,9),(4,13),(2,12),(2,7),(3,4)])
        d.line([(5,3),(9,3)],fill=LIGHT);d.line([(4,4),(3,7)],fill=LIGHT)
        rect((2,7,13,9), LIGHT)
        rect((7,8,8,13), BODY)
    elif kind == 'mace':
        # Heavy flanged head, long haft and a small contrasting pommel.
        poly([(8,6),(10,8),(4,14),(2,12)])
        d.line([(8,8),(4,12)],fill=LIGHT)
        poly([(9,1),(12,1),(14,3),(14,6),(12,8),(9,8),(7,6),(7,3)])
        d.line([(10,2),(10,6)],fill=LIGHT);d.line([(12,3),(12,5)],fill=LIGHT)
        rect((1,12,3,14), LIGHT)
    else: raise ValueError(kind)
    return im

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--preview',type=Path);args=parser.parse_args()
    kinds=['dagger','sword','spear','boots','gloves','helmet','mace']
    template=(OUT/'weapon_ground.png.meta').read_text()
    rows=[]
    for kind in kinds:
        im=sprite(kind);path=OUT/f'item_{kind}.png';im.save(path)
        meta=path.with_suffix('.png.meta')
        if not meta.exists(): meta.write_text(re.sub(r'(?m)^guid: [0-9a-f]{32}$','guid: '+uuid.uuid4().hex,template,count=1))
        assert im.size==(16,16) and {p[3] for p in im.getdata()}=={0,255}
        rows.append(im)
    assert len({im.getchannel('A').tobytes() for im in rows})==7
    if args.preview:
        sheet=Image.new('RGB',(7*176,216),(62,65,58));draw=ImageDraw.Draw(sheet)
        for i,(kind,im) in enumerate(zip(kinds,rows)):
            sheet.paste(im.resize((160,160),Image.Resampling.NEAREST),(i*176+8,12),im.resize((160,160),Image.Resampling.NEAREST))
            draw.text((i*176+12,185),kind,fill=(232,234,222))
        args.preview.parent.mkdir(parents=True,exist_ok=True);sheet.save(args.preview)
    print('Authored seven16x16 binary-alpha sprites; existing GUIDs preserved.')
if __name__=='__main__':main()
