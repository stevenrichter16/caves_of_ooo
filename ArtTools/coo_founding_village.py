#!/usr/bin/env python3
"""Original W6.4 pixel art; only new founding assets and their contact sheet."""
from pathlib import Path
import re
import uuid
from PIL import Image, ImageDraw
from coo_stump_bestiary import outlined, INK

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Sprites/Environment'


def rooted():
    im = Image.new('RGBA', (16, 16)); d = ImageDraw.Draw(im)
    dark, skin, light = (100, 101, 86), (168, 164, 132), (218, 211, 172)
    # Knees bent and shins grounded; lifted torso curves back into the floor.
    d.line([(3, 13), (7, 13), (8, 11), (5, 11)], fill=dark, width=2)
    d.line([(2, 12), (5, 12), (5, 10), (6, 7), (8, 6), (9, 7)], fill=skin, width=2)
    d.line([(3, 13), (6, 13)], fill=light)
    d.polygon([(8, 4), (9, 2), (11, 3), (11, 5), (9, 6)], fill=skin)
    d.point((11, 3), fill=light); d.point((11, 4), fill=INK)
    # Open arms reach to the eastern wall at two heights, never touching it.
    d.line([(8, 6), (11, 6), (13, 5), (14, 5)], fill=light)
    d.line([(8, 8), (10, 9), (13, 10), (14, 9)], fill=skin)
    d.point([(14, 4), (14, 10)], fill=light)
    # Torso plume rather than a hat: stalk roots remain in chest/abdomen.
    d.line([(7, 8), (6, 5), (6, 3)], fill=(184, 188, 139))
    d.line([(7, 8), (8, 5), (8, 3)], fill=(211, 220, 166))
    d.line([(6, 8), (4, 6), (4, 4)], fill=(166, 182, 136))
    d.line([(3, 4), (5, 4)], fill=(227, 231, 183))
    d.line([(5, 2), (7, 2)], fill=(228, 228, 180))
    d.point((8, 2), fill=(240, 237, 197))
    return outlined(im)


def plume(variant):
    im = Image.new('RGBA', (16, 16)); d = ImageDraw.Draw(im)
    d.ellipse((2, 10, 13, 14), fill=(86, 113, 79))
    heights = [(5, 2, 5, 7), (7, 4, 2, 5), (4, 6, 3, 7), (6, 3, 5, 2)][variant]
    for i, x in enumerate((3, 6, 9, 12)):
        y = heights[i]
        d.line([(x, 12), (x-1 if i%2 else x+1, y+2), (x, y+1)], fill=(175, 193, 134))
        d.line([(x-1, y), (x+1, y)], fill=(227, 231, 182))
        d.point((x, y-1), fill=(242, 239, 205))
        d.point((x, 11), fill=(128, 153, 96))
    return outlined(im)


def villager(tender):
    im = Image.new('RGBA', (16, 16)); d = ImageDraw.Draw(im)
    robe = (86, 107, 113) if tender else (119, 108, 78)
    trim = (164, 184, 179) if tender else (183, 170, 116)
    d.polygon([(5, 7), (10, 7), (12, 13), (4, 13)], fill=robe)
    d.line([(5, 8), (4, 11), (3, 10)], fill=trim)
    d.line([(10, 8), (11, 10), (12, 10)], fill=trim)
    d.rectangle((6, 3, 9, 6), fill=(209, 207, 181))
    d.line([(6, 2), (9, 2)], fill=(168, 175, 162))
    d.point([(6, 4), (9, 4)], fill=INK)
    d.line([(7, 7), (7, 11)], fill=trim)
    d.point([(5, 14), (10, 14)], fill=(98, 91, 69))
    if tender:
        d.line([(12, 9), (13, 5)], fill=(152, 132, 96))
        d.line([(12, 5), (14, 5)], fill=(205, 206, 192))
        d.rectangle((2, 10, 4, 12), fill=(148, 143, 127))
    else:
        d.line([(12, 7), (12, 14)], fill=(116, 97, 68))
        d.rectangle((11, 5, 13, 7), fill=(205, 177, 93))
        d.point((12, 6), fill=(248, 227, 150))
    return outlined(im)


def niche(variant):
    """New wall-carved alcoves: distinct lintels, bedding and lamp positions."""
    im = Image.new('RGBA', (16, 16)); d = ImageDraw.Draw(im)
    stone, edge, recess = (103, 95, 73), (153, 141, 105), (47, 49, 39)
    outline = [(2, 4), (4, 2), (11, 2), (13, 4), (13, 13), (2, 13)]
    if variant == 2: outline = [(2, 3), (12, 3), (13, 5), (13, 13), (2, 13)]
    if variant == 3: outline = [(3, 2), (11, 2), (13, 5), (13, 13), (2, 13), (2, 5)]
    d.polygon(outline, fill=stone); d.line([(4, 3), (10, 3)], fill=edge)
    d.rectangle((4, 5, 11, 11), fill=recess)
    d.line([(3, 6), (3, 11)], fill=edge)
    d.line([(3, 12), (12, 12)], fill=(117, 107, 79))
    cloth = [(96, 107, 73), (110, 91, 66), (84, 102, 108)][variant-1]
    d.rectangle((5, 9, 10, 11), fill=cloth)
    pillow = 5 if variant != 2 else 9
    d.line([(pillow, 8), (pillow+1, 8)], fill=(174, 158, 115))
    d.point((7+variant%2, 10), fill=(158, 145, 106))
    lamp = 11 if variant == 1 else 5
    d.point((lamp, 6), fill=(189, 201, 125)); d.point((lamp, 7), fill=(105, 134, 87))
    d.point((12, 3+variant), fill=recess)
    return outlined(im)


def main():
    art = {'the_rooted': rooted(), 'founding_plume': plume(0),
           'founding_plume_v1': plume(1), 'founding_plume_v2': plume(2),
           'founding_plume_v3': plume(3), 'founding_listener': villager(False),
           'founding_plaque_tender': villager(True),
           'niche_home_v1': niche(1), 'niche_home_v2': niche(2), 'niche_home_v3': niche(3)}
    template = (OUT / 'cascade_father.png.meta').read_text()
    sheet = Image.new('RGB', (160*len(art), 195), (46, 49, 42)); draw = ImageDraw.Draw(sheet)
    for i, (name, sprite) in enumerate(art.items()):
        assert set(sprite.getchannel('A').tobytes()) <= {0, 255}
        path = OUT / (name+'.png'); sprite.save(path)
        meta = Path(str(path)+'.meta')
        if not meta.exists():meta.write_text(re.sub(r'(?m)^guid: [0-9a-f]+$', 'guid: '+uuid.uuid4().hex, template))
        enlarged = sprite.resize((160, 160), Image.Resampling.NEAREST)
        sheet.paste(enlarged, (i*160, 0), enlarged)
        draw.text((i*160+3, 170), name.replace('_',' '), fill=(225,219,190))
    sheet.save(ROOT / 'Docs/Verification/FellingW6/W64-creatures-plume.png')

if __name__ == '__main__': main()
