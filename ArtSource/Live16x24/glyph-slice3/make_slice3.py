"""Glyph -> pixel slice 3: ten people who had no body (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md).

Editable source. Run `python3 make_slice3.py` here to rewrite the PNGs under
Assets/Resources/Sprites/Environment/ (shared 16x16 people format: round head, simple robe,
arms at the sides, bottom-aligned; the Pale Curation wear hoods with lit eyes, as the
approved pale_curator and stillleaf_indexer do). Original work.
"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Environment")

OPEN_FACE = [
    "................",
    "................",
    ".....oooooo.....",
    "....oHHHHHHo....",
    "....oFFFFFFo....",
    "....oFeFFeFo....",
    "....oFFFFFFo....",
    "...ooBBBBBBoo...",
    "..oBBBTBBBBBBo..",
    "..oBoBTBBBBoBo..",
    "..oBoBBTBBBoBo..",
    "..oFoBBBTBBoFo..",
    "....oBBBBBBo....",
    "....oBBoBBBo....",
    "....oddoodd.....",
    "....ooo.ooo.....",
]

HOODED = [
    "................",
    "................",
    ".....oooooo.....",
    "....oHHHHHHo....",
    "...oHHkkkkHHo...",
    "...oHkLkkLkHo...",
    "...oHHkkkkHHo...",
    "...ooBBBBBBoo...",
    "..oBBBTBBBBBBo..",
    "..oBoBTBBBBoBo..",
    "..oBoBBTBBBoBo..",
    "..oFoBBBTBBoFo..",
    "....oBBBBBBo....",
    "....oBBBBBBo....",
    ".....oBBBBo.....",
    "......oooo......",
]

BASE = {".": (0, 0, 0, 0), "o": (22, 27, 24, 255), "F": (214, 182, 146, 255), "e": (52, 40, 32, 255),
        "k": (40, 42, 50, 255), "L": (130, 220, 230, 255), "d": (70, 60, 48, 255)}

def person(template, item=None, **colors):
    pal = dict(BASE); pal.update(colors)
    rows = [list(r) for r in template]
    if item:
        for (x, y), ch in item.items(): rows[y][x] = ch
    return ["".join(r) for r in rows], pal

C = lambda r, g, b: (r, g, b, 255)

PEOPLE = {
    # the Concord's factor: a cream coat, a gold sash, a ledger under the arm
    "concord_factor": person(OPEN_FACE, {(13, 10): "g", (13, 11): "g", (14, 10): "o", (14, 11): "o"},
                             H=C(90, 66, 40), B=C(222, 214, 190), T=C(214, 170, 60), g=C(150, 100, 50)),
    # Pale Curation: hooded, salt-pale robes, lit eyes; each carries what its work is
    "curation_intake_filer": person(HOODED, {(13, 11): "t", (13, 12): "t"}, H=C(196, 198, 200), B=C(214, 216, 216), T=C(150, 156, 162), t=C(200, 160, 90)),
    "curation_junior_indexer": person(HOODED, None, H=C(170, 174, 178), B=C(190, 194, 196), T=C(130, 136, 142)),
    "curation_sorter": person(HOODED, {(13, 11): "c", (13, 12): "c"}, H=C(150, 170, 176), B=C(176, 196, 200), T=C(90, 150, 160), c=C(130, 200, 210)),
    "filer_clerk": person(HOODED, {(1, 10): "p", (1, 11): "p", (2, 10): "p"}, H=C(160, 176, 184), B=C(186, 200, 206), T=C(110, 140, 150), p=C(236, 232, 216)),
    "gantry_registrar": person(HOODED, {(13, 11): "s", (14, 11): "s", (13, 12): "o"}, H=C(180, 180, 176), B=C(200, 198, 192), T=C(120, 110, 90), s=C(150, 70, 60)),
    # a peat cutter: brown apron over a grey shirt, a spade over the shoulder
    "peat_cutter": person(OPEN_FACE, {(13, 3): "w", (13, 4): "w", (13, 5): "w", (13, 6): "w", (12, 2): "w", (14, 2): "w"},
                          H=C(110, 84, 54), B=C(120, 92, 62), T=C(150, 150, 140), w=C(150, 154, 160)),
    # the Recension's scribe: a dark teal robe, ink on the cuff, a quill
    "recension_scribe": person(OPEN_FACE, {(13, 9): "q", (13, 8): "q", (14, 7): "q"}, H=C(70, 70, 76), B=C(48, 98, 104), T=C(30, 30, 40), q=C(236, 232, 214)),
    # Tent-Right: head-wraps and sand-pale robes; the salt-master carries a small scale
    "salt_master": person(OPEN_FACE, {(13, 11): "s", (14, 12): "s", (12, 12): "s", (13, 12): "o"}, H=C(226, 222, 206), B=C(214, 206, 180), T=C(160, 130, 90), s=C(176, 176, 170)),
    "tent_right_host": person(OPEN_FACE, None, H=C(200, 170, 110), B=C(196, 160, 104), T=C(150, 64, 54)),
}


def draw(rows, pal, name):
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        assert len(row) == 16, (name, y)
        for x, ch in enumerate(row):
            img.putpixel((x, y), pal[ch])
    img.save(os.path.join(OUT, name + ".png"))


if __name__ == "__main__":
    for name, (rows, pal) in PEOPLE.items():
        draw(rows, pal, name)
