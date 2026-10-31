"""Original native quest-cue markers for the flat 16x24 actor layer.

Editable source: the pixel rows below are the art. Run with `python3 make_quest_cue.py`
from this folder to rewrite the two PNGs under Assets/Resources/Sprites/Actors/.
Each canvas is 16x10 at 16 px per unit, drawn bottom-aligned so the marker sits one
pixel above an actor's head. Palette: dark umber outline, warm gold fill, pale
highlight (available); slate outline and muted silver (work ongoing).
"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Actors")

PALETTE = {
    ".": (0, 0, 0, 0),
    "o": (42, 29, 16, 255),     # outline, umber
    "G": (242, 193, 78, 255),   # gold
    "H": (255, 242, 179, 255),  # highlight
    "s": (36, 44, 56, 255),     # outline, slate
    "S": (176, 186, 198, 255),  # silver
}

AVAILABLE = [  # "!" — someone here has something to ask
    ".ooo.",
    "oGHGo",
    "oGGGo",
    "oGGGo",
    "oGGGo",
    ".ooo.",
    ".....",
    ".ooo.",
    "oGGGo",
    ".ooo.",
]

ACTIVE = [  # "..." — work you took on is still under way
    "sss.sss.sss",
    "sSs.sSs.sSs",
    "sss.sss.sss",
]


def draw(rows, name, width=16, height=10):
    image = Image.new("RGBA", (width, height), PALETTE["."])
    w, h = len(rows[0]), len(rows)
    left, top = (width - w) // 2, height - h
    for y, row in enumerate(rows):
        assert len(row) == w, name
        for x, ch in enumerate(row):
            image.putpixel((left + x, top + y), PALETTE[ch])
    image.save(os.path.join(OUT, name))


if __name__ == "__main__":
    draw(AVAILABLE, "quest_cue_available.png")
    draw(ACTIVE, "quest_cue_active.png")
