"""Original native threat cues for the flat 16x24 ground layer.

Editable source: the pixel rows below are the art. Run with `python3 make_threat_cue.py`
from this folder to rewrite the PNG under Assets/Resources/Sprites/Actors/.

stoop_shadow: the Sky-Sari's shadow on the ground two turns before it drops (P5 B3,
Docs/UPPER-TIER-BESTIARY.md). A raptor seen from below, wings spread, drawn as a soft
translucent shade over whatever ground is there: a core of near-black at 60% and a
fringe at 30%, so grass and sand still read through it. 16x24 canvas, centred on the
cell's lower two thirds, where the actor's feet stand.
"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Actors")

PALETTE = {
    ".": (0, 0, 0, 0),
    "f": (14, 12, 18, 77),    # fringe, ~30%
    "c": (10, 8, 14, 153),    # core, ~60%
}

STOOP_SHADOW = [
    "................",
    ".......ff.......",
    "......fccf......",
    "f.....fccf.....f",
    "ff...fccccf...ff",
    "fcf.fccccccf.fcf",
    "fcccccccccccccff",
    ".fcccccccccccccf",
    "..ffccccccccff..",
    "....ffccccff....",
    "......fccf......",
    ".....fcffcf.....",
    "....ff....ff....",
    "................",
]


def draw(rows, name, width=16, height=24):
    image = Image.new("RGBA", (width, height), PALETTE["."])
    w, h = len(rows[0]), len(rows)
    left, top = (width - w) // 2, height - h - 3
    for y, row in enumerate(rows):
        assert len(row) == w, name
        for x, ch in enumerate(row):
            image.putpixel((left + x, top + y), PALETTE[ch])
    image.save(os.path.join(OUT, name))


if __name__ == "__main__":
    draw(STOOP_SHADOW, "threat_stoop_shadow.png")
