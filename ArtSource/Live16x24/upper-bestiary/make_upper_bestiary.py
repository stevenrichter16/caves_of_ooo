"""Original 16x16 bodies for the upper-tier bestiary (P5, Docs/UPPER-TIER-BESTIARY.md).

Editable source: the pixel rows below are the art. Run `python3 make_upper_bestiary.py`
from this folder to rewrite the PNGs under Assets/Resources/Sprites/Environment/.
They follow the shared 16x16 environment format (the accepted Merchant/Elder format):
a near-black outline, flat fills with one highlight and one shade, bottom-aligned.

- riverwarden: a black dwarf caiman, low and broad, armoured back, yellow eye.
- sima_steward: an oilbird, wings half open, hooked bill, pale spots on rust feathers.
- bloomed_warden: a catacomb warden in grey chitin plates, pale and tall, fruiting caps
  broken through at the neck and shoulders.
- bloomed_hound: a brittle hound gone pale, a double row of pink caps along its spine.
"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Environment")

PAL = {
    ".": (0, 0, 0, 0),
    "o": (30, 32, 28, 255),      # outline
    # riverwarden
    "g": (52, 70, 50, 255),      # hide
    "G": (82, 104, 70, 255),     # hide highlight
    "k": (36, 46, 36, 255),      # hide shade / scutes
    "b": (150, 150, 110, 255),   # belly
    "y": (226, 196, 70, 255),    # eye
    "w": (220, 220, 210, 255),   # teeth
    # sima-steward
    "r": (128, 74, 44, 255),     # rust feather
    "R": (168, 104, 62, 255),    # feather highlight
    "q": (88, 50, 32, 255),      # feather shade
    "s": (236, 226, 200, 255),   # pale spots
    "h": (210, 180, 120, 255),   # bill
    # bloomed warden / hound
    "p": (196, 200, 206, 255),   # pale skin
    "c": (120, 130, 146, 255),   # chitin
    "C": (160, 170, 186, 255),   # chitin highlight
    "d": (84, 92, 106, 255),     # chitin shade
    "m": (214, 120, 176, 255),   # fruiting cap
    "M": (244, 196, 226, 255),   # cap highlight
    "e": (40, 40, 44, 255),      # eye-dark
}

RIVERWARDEN = [
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "........o.o.o...",
    "..oo...okokokoo.",
    ".oyGo.okGkGkGggo",
    "ogggggggggggggko",
    "owwwbbbbbbbbbgko",
    ".oooobbbbbbbbbo.",
    "....ogo.ogo.ogo.",
    "....oo..oo..oo..",
]

SIMA_STEWARD = [
    "................",
    "................",
    ".....oo.........",
    "....orRo........",
    "...ohhro.....oo.",
    "...oohro....orRo",
    "....orrro..orrqo",
    "..ooRrsrroorsrqo",
    ".oRrrrrsrrrrrqo.",
    "orrsrqqrrrsrqo..",
    "oqqqoorrrrqqo...",
    ".oo..orrqqro....",
    ".....oqrqro.....",
    "......oqqo......",
    ".......hh.......",
    "......o..o......",
]

BLOOMED_WARDEN = [
    ".....oooo.......",
    "....oppppo......",
    "...opeppepo.....",
    "...oppppppo.....",
    "..oMmoppomMo....",
    ".omMmCccCmmMo...",
    ".ocCcCccCcCco...",
    "ocdCcccccccdco..",
    "ocdoCcccccodco..",
    "opooCcdcdcoopo..",
    "oo..cccdccc.oo..",
    "....odcocdo.....",
    "....occoocco....",
    "....occo.occo...",
    "....odo...odo...",
    "....ooo...ooo...",
]

BLOOMED_HOUND = [
    "................",
    "................",
    "................",
    "................",
    "................",
    "..........oo....",
    "...m.m.m.oppo...",
    "..oMoMoMooppeo..",
    ".oppppppppppppo.",
    "oppppppppppppo..",
    "odpppppppppppo..",
    ".odppppppppdo...",
    "..opdo..opdo....",
    "..opo...opo.....",
    "..opo...opo.....",
    "..oo....oo......",
]


def draw(rows, name):
    assert len(rows) == 16, name
    img = Image.new("RGBA", (16, 16), PAL["."])
    for y, row in enumerate(rows):
        assert len(row) == 16, (name, y, row)
        for x, ch in enumerate(row):
            img.putpixel((x, y), PAL[ch])
    img.save(os.path.join(OUT, name + ".png"))


if __name__ == "__main__":
    draw(RIVERWARDEN, "riverwarden")
    draw(SIMA_STEWARD, "sima_steward")
    draw(BLOOMED_WARDEN, "bloomed_warden")
    draw(BLOOMED_HOUND, "bloomed_hound")
