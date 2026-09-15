"""Reproducible 16px fallback art for the native Overwrit identities.

The primary art is the voxel kit. These preserve identity in sprite mode.
The blank floor deliberately has no ink border or visual variants.
"""
from pathlib import Path
import re
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Resources/Sprites/Environment"
INK = (30, 32, 28, 255)


def save(image, name):
    path = OUT / (name + ".png")
    image.save(path)
    meta = path.with_suffix(".png.meta")
    if not meta.exists():
        source = (OUT / "rope_anchor.png.meta").read_text()
        meta.write_text(re.sub(r"(?m)^guid: .*$", "guid: " + uuid.uuid4().hex, source))


save(Image.new("RGBA", (16, 16), (180, 179, 159, 255)), "overwrit_ground")
for family in ("new_growth", "waymarker", "pilgrim_bench"):
    for variant in range(4):
        image = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        if family == "new_growth":
            fill = (131, 147, 100, 255)
            for x, width in ((3 + variant % 2, 2), (7, 3 - variant % 2), (11 - variant // 2, 2)):
                draw.rectangle((x - 1, 5, x + width, 12), fill=INK)
                draw.rectangle((x, 6, x + width - 1, 11 - variant % 2), fill=fill)
            draw.rectangle((3, 12, 12, 13), fill=INK)
            draw.rectangle((4, 12, 11 - variant, 12), fill=fill)
        elif family == "waymarker":
            fill = (173, 169, 150, 255)
            left, right = 5 - variant % 2, 10 + variant // 2
            draw.rectangle((left, 2, right, 13), fill=INK)
            draw.rectangle((left + 1, 3, right - 1, 12), fill=fill)
            draw.rectangle((left - 1, 12, right + 1, 14), fill=INK)
            draw.rectangle((left, 12, right, 13), fill=fill)
        else:
            fill = (149, 124, 88, 255)
            draw.rectangle((2, 3, 13, 7), fill=INK)
            draw.rectangle((3, 4, 12, 6), fill=fill)
            draw.rectangle((1, 7, 14, 11), fill=INK)
            draw.rectangle((2, 8, 13, 10), fill=fill)
            for x in (3 + variant % 2, 11 - variant // 2):
                draw.rectangle((x, 11, x + 1, 13), fill=INK)
            draw.point((2 + variant, 7), fill=fill)
        save(image, "overwrit_" + family + ("" if variant == 0 else "_v" + str(variant)))

print("Generated 13 quiet native fallback sprites; existing GUIDs preserved.")
