#!/usr/bin/env python3
"""W6.3b original cell art: five animals, nest, stone seams and water passage.

Uses the shipped bestiary's flat three-tone style and shared (30,32,28) outline.
Metadata is copied verbatim from cascade_father.png.meta, changing only its GUID.
"""
from pathlib import Path
import re
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Resources/Sprites/Environment"
INK = (30, 32, 28, 255)


def outlined(image):
    result = Image.new("RGBA", (16, 16))
    for y in range(16):
        for x in range(16):
            if image.getpixel((x, y))[3]:
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    xx, yy = x + dx, y + dy
                    if 0 <= xx < 16 and 0 <= yy < 16:
                        result.putpixel((xx, yy), INK)
    result.alpha_composite(image)
    return result


def frog(helmet=False):
    im = Image.new("RGBA", (16, 16)); d = ImageDraw.Draw(im)
    dark, mid, light = ((74, 61, 40), (121, 109, 67), (170, 153, 97)) if helmet else ((66, 72, 44), (115, 119, 65), (169, 167, 99))
    d.polygon([(4, 5), (5, 3), (10, 3), (12, 5), (11, 10), (9, 12), (5, 11)], fill=mid)
    d.polygon([(4, 8), (2, 10), (2, 12), (5, 12), (5, 11), (3, 11), (5, 9)], fill=dark)
    d.polygon([(11, 8), (13, 10), (13, 12), (10, 12), (10, 11), (12, 11), (10, 9)], fill=dark)
    d.line([(4, 6), (2, 7), (3, 8)], fill=mid)
    d.line([(11, 6), (13, 7), (12, 8)], fill=mid)
    d.line([(5, 4), (10, 4)], fill=light)
    d.point([(4, 4), (11, 4)], fill=(220, 191, 76))
    d.point([(5, 5), (10, 5)], fill=INK)
    if helmet:
        d.polygon([(4, 3), (6, 1), (9, 1), (11, 3), (10, 4), (5, 4)], fill=light)
        d.line([(6, 2), (9, 2)], fill=(203, 181, 132))
        d.line([(6, 7), (7, 10), (9, 10)], fill=dark)
        d.point([(5, 8), (9, 7), (10, 9)], fill=light)
    else:
        # The W is the ecological reading mark, distinct from the broad casque.
        d.line([(5, 7), (6, 9), (7, 8), (8, 9), (9, 7)], fill=INK)
        d.point([(6, 6), (9, 6), (7, 11)], fill=light)
    return outlined(im)


def lizard(pricklebrow=False):
    im = Image.new("RGBA", (16, 16)); d = ImageDraw.Draw(im)
    dark, mid, light = ((37, 51, 77), (58, 80, 113), (105, 126, 146)) if pricklebrow else ((64, 78, 54), (104, 119, 77), (161, 164, 111))
    d.line([(2, 3), (2, 6), (4, 9), (7, 10)], fill=dark, width=2)
    d.polygon([(5, 8), (7, 6), (10, 6), (11, 9), (9, 11), (6, 11)], fill=mid)
    d.polygon([(9, 4), (12, 3), (13, 5), (12, 7), (9, 7)], fill=mid)
    d.line([(6, 8), (5, 6), (4, 6)], fill=mid)
    d.line([(7, 10), (6, 13), (4, 13)], fill=mid)
    d.line([(10, 9), (12, 10), (12, 12), (13, 12)], fill=mid)
    d.line([(7, 7), (8, 8), (8, 10)], fill=light)
    d.point([(11, 4)], fill=(225, 195, 83))
    if pricklebrow:
        d.line([(10, 3), (10, 1)], fill=light)
        d.point([(12, 2)], fill=light)
        d.line([(10, 6), (12, 6)], fill=(218, 141, 63))
        d.point([(7, 9), (9, 10), (5, 8)], fill=(196, 166, 78))
    else:
        d.point([(6, 8), (8, 7), (10, 9), (9, 5), (4, 8)], fill=light)
        d.point([(7, 9), (9, 8), (11, 5)], fill=dark)
    return outlined(im)


def eagle():
    im = Image.new("RGBA", (16, 16)); d = ImageDraw.Draw(im)
    wing, mid, light = (65, 65, 71), (127, 126, 123), (208, 201, 180)
    d.polygon([(7, 5), (5, 4), (2, 2), (1, 2), (1, 5), (3, 8), (6, 9)], fill=wing)
    d.polygon([(8, 5), (10, 4), (13, 2), (14, 2), (14, 5), (12, 8), (9, 9)], fill=wing)
    d.line([(2, 3), (4, 6), (6, 7)], fill=mid)
    d.line([(13, 3), (11, 6), (9, 7)], fill=mid)
    d.point([(1, 5), (3, 7), (14, 5), (12, 7)], fill=light)
    d.polygon([(6, 5), (9, 5), (9, 10), (8, 12), (7, 12), (6, 10)], fill=light)
    d.rectangle((6, 3, 9, 5), fill=light)
    d.point([(6, 2), (9, 2)], fill=mid)
    d.point([(6, 4), (9, 4)], fill=(164, 75, 45))
    d.polygon([(7, 5), (8, 5), (8, 7), (7, 6)], fill=(161, 138, 81))
    d.line([(7, 9), (8, 9)], fill=mid)
    d.point([(6, 11), (9, 11), (6, 12), (9, 12)], fill=(175, 141, 69))
    d.line([(7, 12), (7, 14), (8, 14), (8, 12)], fill=wing)
    return outlined(im)


def nest():
    im = Image.new("RGBA", (16, 16)); d = ImageDraw.Draw(im)
    d.ellipse((1, 4, 14, 14), fill=(87, 66, 40))
    d.ellipse((3, 5, 12, 12), fill=(51, 47, 33))
    d.line([(2, 11), (5, 14), (11, 14), (14, 10)], fill=(134, 103, 59))
    for row in range(4):
        for col in range(4):
            x, y = 2 + col * 3, 5 + row * 2
            d.point((x, y), fill=(224, 208, 150))
            d.point((x + 1, y), fill=(173, 158, 105))
    d.line([(1, 6), (3, 3), (6, 4)], fill=(134, 103, 59))
    d.line([(12, 3), (14, 5), (13, 7)], fill=(117, 118, 66))
    return outlined(im)


def tepuibone(variant=0, chip=False):
    im = Image.new("RGBA", (16, 16)); d = ImageDraw.Draw(im)
    shapes = [
        [(2, 5), (5, 2), (11, 2), (14, 7), (12, 13), (6, 14), (1, 10)],
        [(1, 6), (4, 2), (10, 3), (13, 5), (14, 12), (5, 13), (2, 11)],
        [(2, 4), (7, 1), (12, 4), (14, 10), (11, 14), (3, 12), (1, 8)],
        [(1, 5), (5, 3), (12, 2), (14, 8), (12, 12), (7, 14), (2, 12)]]
    shape = [(4, 6), (8, 3), (12, 6), (11, 11), (6, 12), (3, 10)] if chip else shapes[variant]
    d.polygon(shape, fill=(139, 126, 121))
    d.line([(4, 10), (8, 12), (11, 10)], fill=(83, 81, 75))
    if chip:
        d.line([(5, 6), (8, 5), (10, 6)], fill=(224, 214, 180))
        d.line([(5, 8), (8, 7), (11, 8)], fill=(191, 181, 151))
    else:
        y = 5 + variant
        d.line([(2, y), (5, y + 1), (9, y), (13, y + 1)], fill=(228, 217, 181), width=2)
        d.line([(3, y + 3), (7, y + 2), (12, y + 3)], fill=(189, 176, 145))
        d.point([(6, 3), (10, 11)], fill=(176, 151, 143))
    return outlined(im)


def water_passage():
    im = Image.new("RGBA", (16, 16)); d = ImageDraw.Draw(im)
    d.polygon([(1, 8), (3, 3), (6, 1), (10, 2), (13, 5), (14, 11), (2, 12)], fill=(121, 125, 109))
    d.polygon([(4, 8), (5, 5), (8, 4), (11, 6), (12, 11), (3, 11)], fill=INK)
    d.line([(3, 4), (6, 2), (10, 3), (12, 5)], fill=(180, 177, 143))
    d.polygon([(4, 10), (11, 9), (13, 12), (11, 14), (3, 14), (1, 12)], fill=(58, 100, 110))
    d.line([(4, 11), (8, 10), (10, 11)], fill=(125, 171, 166))
    d.line([(3, 13), (7, 12), (11, 13)], fill=(163, 190, 173))
    return outlined(im)


def main():
    art = {"summit_singer": frog(), "brocchinia_sentinel": lizard(),
           "sky_sari": eagle(), "pricklebrow_gecko": lizard(True), "helmwood_frog": frog(True), "pricklebrow_nest": nest(),
           "tepuibone": tepuibone(chip=True), "tepuibone_vein": tepuibone(),
           "tepuibone_vein_v1": tepuibone(1), "tepuibone_vein_v2": tepuibone(2),
           "tepuibone_vein_v3": tepuibone(3), "helmwood_water_passage": water_passage()}
    template = (OUT / "cascade_father.png.meta").read_text()
    contact = Image.new("RGB", (960, 190 * ((len(art) + 5) // 6)), (46, 49, 42)); draw = ImageDraw.Draw(contact)
    for index, (name, sprite) in enumerate(art.items()):
        assert set(sprite.getchannel("A").tobytes()) <= {0, 255}
        path = OUT / (name + ".png"); sprite.save(path)
        meta = Path(str(path) + ".meta")
        if not meta.exists():
            meta.write_text(re.sub(r"(?m)^guid: [0-9a-f]+$", "guid: " + uuid.uuid4().hex, template))
        contact.paste(sprite.resize((160, 160), Image.Resampling.NEAREST), (160 * (index % 6), 190 * (index // 6)),
                      sprite.resize((160, 160), Image.Resampling.NEAREST))
        draw.text((160 * (index % 6) + 5, 190 * (index // 6) + 170), name.replace("_", " "), fill=(225, 219, 190))
    dest = ROOT / "Docs/Verification/FellingW6/W63b-creatures.png"
    dest.parent.mkdir(parents=True, exist_ok=True); contact.save(dest)


if __name__ == "__main__":
    main()
