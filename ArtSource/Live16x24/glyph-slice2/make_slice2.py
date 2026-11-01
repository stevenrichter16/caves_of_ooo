"""Glyph -> pixel slice 2: twelve creatures that had no art (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md).

Editable source: the pixel rows are the art. Run `python3 make_slice2.py` from this folder to
rewrite the PNGs under Assets/Resources/Sprites/Environment/ (the shared 16x16 format).
Original work; Caves of Qud tiles and the project's marlback, viper, magpie and rotling bodies
were studied for silhouette only.
"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Environment")

BASE = {
    ".": (0, 0, 0, 0),
    "o": (22, 27, 24, 255),     # outline
    # marlback shale and face
    "P": (156, 154, 128, 255),  # plate highlight
    "p": (109, 120, 100, 255),  # plate
    "S": (67, 77, 74, 255),     # plate seam / shade
    "F": (74, 66, 49, 255),     # face / underside
    "e": (230, 200, 90, 255),   # eye
    "W": (214, 210, 186, 255),  # pale mark / notch / chalk
}

def marlback(extra):
    pal = dict(BASE); pal.update(extra); return pal

MARL = [
    "................",
    "................",
    "................",
    ".....oooooo.....",
    "....oPPPPPPo....",
    "...oSSSSSSSSo...",
    "..oPpPPpPPPpPo..",
    "..oSSSSSSSSSSo..",
    ".oPpPPPPPpPPPPo.",
    ".oSSSSSSSSSSSSo.",
    ".oFFFFFFFFFFFFo.",
    ".oFeFFFFFFFFeFo.",
    "..oFFFFFFFFFFo..",
    "..ooFFoooooFFoo.",
    "...oFo.....oFo..",
    "...oo.......oo..",
]

def edit(rows, changes):
    rows = [list(r) for r in rows]
    for (x, y), ch in changes.items(): rows[y][x] = ch
    return ["".join(r) for r in rows]

SOURSPRAYER = edit(MARL, {(4,10):"g",(5,10):"g",(6,10):"g",(7,10):"g",(8,10):"g",(9,10):"g",(10,10):"g",(11,10):"g",
                          (5,12):"G",(6,12):"G",(13,8):"G",(14,9):"G"})
STORMBINDER = edit(MARL, {(5,4):"b",(6,4):"b",(9,4):"b",(10,4):"b",(4,6):"b",(7,6):"b",(10,6):"b",(3,8):"b",(8,8):"b",(12,8):"b",(7,2):"y",(8,1):"y"})
DITCHMATE = edit(MARL, {(5,4):"W",(6,6):"W",(7,8):"W",(9,4):"W",(10,6):"W",(11,8):"W",(14,10):"c",(15,9):"c",(15,8):"c",(15,7):"c",(14,11):"o"})
HURDLECUTTER = edit(MARL, {(7,3):"W",(8,3):"W",(7,4):"o",(8,4):"o",(14,4):"k",(14,5):"k",(14,6):"k",(14,7):"k",(14,8):"k",(14,9):"o",(13,9):"o",(15,9):"o"})

HALFSET = [
    "................",
    "......oooo......",
    ".....oWWyyo.....",
    ".....oWeyeo.....",
    "......oWyo......",
    "...ooWWWggggo...",
    "..oWWWWWgGgggo..",
    "..oWoWWtggoggo..",
    "..oWoWWggGgogo..",
    "..oWoWgggggoGo..",
    "...ooggGgggoog..",
    ".....ogggggo..gg",
    ".....oggoggo...g",
    ".....ogo.ogo....",
    ".....ogo.ogo....",
    ".....oo...oo....",
]
HALFSET_PAL = dict(BASE, W=(222,222,210,255), y=(196,176,150,255), g=(70,120,60,255), G=(120,170,90,255), t=(200,160,90,255))

DIRTGNOME = [
    "................",
    "................",
    "................",
    "................",
    "......oooo......",
    ".....obbbbo.....",
    ".....bebbebo....",
    "....obbbnbbo....",
    "...ooobbbbooo...",
    "..obbdbbbbdbbo..",
    ".obbobbbbbbobbo.",
    "occoobbdbbboocco",
    "oc.co.obbbo.oc.c",
    ".....obbobbo....",
    ".....odo.odo....",
    ".....ooo.ooo....",
]
DIRTGNOME_PAL = dict(BASE, b=(142,104,66,255), d=(98,70,44,255), n=(178,138,96,255), c=(210,196,160,255), e=(240,220,120,255))

FURROWSTALKER = [
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "........oo......",
    "oooo...oddoo....",
    "ottto.oddddeo...",
    ".oooootttttdddo.",
    "....otttttttjjjo",
    "....ottttttttooo",
    "....oTTttttTTo..",
    "....oto.oo.oto..",
    "....oto....oto..",
    "....oo.....oo...",
]
FURROW_PAL = dict(BASE, t=(150,120,70,255), T=(112,88,52,255), d=(70,52,36,255), j=(222,210,180,255), e=(240,200,80,255))

WATCHER = [
    "......oooo..o...",
    ".....offffo.o...",
    ".....ofeefo.o...",
    ".....offffo.o...",
    "....ocffffco.o..",
    "...occccccccoo..",
    "...ocCccccCcho..",
    "...occccccccho..",
    "...ocCccccCcco..",
    "...occccccccco..",
    "....occccccco...",
    "....occccccco...",
    "....occo.occo...",
    "....odo...odo...",
    "....odo...odo...",
    "....ooo...ooo...",
]
WATCHER_PAL = dict(BASE, f=(212,184,150,255), e=(60,50,40,255), c=(130,134,128,255), C=(170,174,166,255), h=(150,112,70,255), d=(70,60,48,255))

SOOTGREMLIN = [
    "................",
    "................",
    "................",
    "................",
    "................",
    "......oooo......",
    ".....okkkko.....",
    "....okekkeko....",
    "....okkggkko....",
    "...ookkggkkoo...",
    "..okkkkkkkkkko..",
    "..okKkkkkkkKko..",
    "..oco.okkko.oco.",
    "......okoko.....",
    "......oo.oo.....",
    "................",
]
SOOT_PAL = dict(BASE, k=(48,46,50,255), K=(78,76,82,255), g=(150,150,150,255), e=(230,140,60,255), c=(110,110,112,255))

LATCHCOIL = [
    "................",
    "................",
    "................",
    "................",
    "................",
    "........oooo....",
    ".......oggggoo..",
    "......ogeggggro.",
    "......oggoooo...",
    "..oWWo.oggo.....",
    ".oggggWoggo.....",
    "oggooogggggoo...",
    "ogWo..ooggggWgo.",
    ".oggoooooogggggo",
    "..oggggggWgggoo.",
    "...oooooooooo...",
]
LATCH_PAL = dict(BASE, g=(84,128,70,255), W=(220,220,206,255), e=(220,60,40,255), r=(200,80,80,255))

SUNSTRIKER = [  # top-down: a broad flat lizard, legs splayed, tail curling away
    "................",
    ".......oo.......",
    "......oyyo......",
    "......eyye......",
    ".o....oyyo....o.",
    ".oo..oDYYDo..oo.",
    "..oooyYyyYyooo..",
    ".....oDYYDo.....",
    ".....oyYYyo.....",
    "..oooDYyyYDooo..",
    ".oo..oyDDyo..oo.",
    ".o....oyyo....o.",
    ".......oyo......",
    "........oyo.....",
    ".........oyo....",
    "..........oo....",
]
SUN_PAL = dict(BASE, y=(150,124,64,255), Y=(226,186,80,255), D=(96,80,48,255), e=(30,30,30,255))

TATTERJAY = [
    "................",
    "................",
    "................",
    "......ooo.......",
    ".....obbbo......",
    "....obebbbo.....",
    "..ooobbbbbo.....",
    ".ohhhobbbbbo....",
    "..ooobBBbbbbo...",
    ".....obwwbBbbo..",
    ".....obwwwbBbbo.",
    "......obwwbbobbo",
    ".......obbbo.obo",
    "........oyyo..oo",
    "........oy.yo...",
    "................",
]
TATTER_PAL = dict(BASE, b=(52,128,128,255), B=(84,170,160,255), w=(220,226,214,255), h=(208,180,110,255), e=(20,20,20,255), y=(150,120,70,255))

SPRITES = {
    "marlback_soursprayer": (SOURSPRAYER, marlback({"g": (110, 140, 50, 255), "G": (170, 196, 70, 255)})),
    "marlback_stormbinder": (STORMBINDER, marlback({"b": (70, 110, 150, 255), "y": (250, 240, 140, 255)})),
    "spread_ditch_mate": (DITCHMATE, marlback({"c": (120, 86, 52, 255)})),
    "spread_hurdle_cutter": (HURDLECUTTER, marlback({"k": (190, 196, 204, 255)})),
    "curation_half_set": (HALFSET, HALFSET_PAL),
    "dirt_gnome": (DIRTGNOME, DIRTGNOME_PAL),
    "furrowstalker": (FURROWSTALKER, FURROW_PAL),
    "sodden_passage_guard": (WATCHER, WATCHER_PAL),
    "soot_gremlin": (SOOTGREMLIN, SOOT_PAL),
    "spread_latchcoil": (LATCHCOIL, LATCH_PAL),
    "sun_striker": (SUNSTRIKER, SUN_PAL),
    "tatterjay": (TATTERJAY, TATTER_PAL),
}


def draw(rows, pal, name):
    assert len(rows) == 16, name
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        assert len(row) == 16, (name, y, row)
        for x, ch in enumerate(row):
            img.putpixel((x, y), pal[ch])
    img.save(os.path.join(OUT, name + ".png"))


if __name__ == "__main__":
    for name, (rows, pal) in SPRITES.items():
        draw(rows, pal, name)
