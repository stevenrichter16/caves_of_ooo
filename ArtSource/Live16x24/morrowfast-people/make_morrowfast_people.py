"""Glyph -> pixel, rank 1 sub-milestone 4: Morrowfast's eight residents (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md).

Editable source, in the shared 16x16 people format of slice 3 (round head, simple garment, arms at the
sides, bottom-aligned). Each carries what its description gives it. Glyph colours: the watchkeepers
&y (grey leathers), Vennit &C (the Recension's teal-cyan), the others &g (grove greens). Original work.
Run `python3 make_morrowfast_people.py` here to rewrite the PNGs under Assets/Resources/Sprites/Environment.
"""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "glyph-slice3"))
from make_slice3 import OPEN_FACE, HOODED, person, draw, C

def spear(x=13):
    """A spear held upright at the right hand, its point above the head."""
    item = {(x, y): "w" for y in range(3, 14)}
    item[(x, 2)] = "s"; item[(x, 1)] = "s"
    return item

PEOPLE = {
    # Nemm Hushbell, the west watchkeeper: grey leathers, a spear, his bell cord wound in cloth at the hip
    "morrowfast_nemm": person(OPEN_FACE, {**spear(), (2, 12): "c", (2, 13): "b", (3, 13): "c"},
                              H=C(96, 78, 56), B=C(134, 128, 116), T=C(92, 84, 72), w=C(120, 92, 60), s=C(176, 180, 186),
                              c=C(196, 186, 160), b=C(170, 150, 80)),
    # Hesta Brack, the east watchkeeper: grey leathers, a spear, a return slate at her side
    "morrowfast_hesta": person(OPEN_FACE, {**spear(), (1, 10): "l", (1, 11): "l", (2, 10): "l", (2, 11): "p"},
                               H=C(70, 50, 34), B=C(128, 124, 116), T=C(90, 82, 70), w=C(120, 92, 60), s=C(176, 180, 186),
                               l=C(60, 64, 70), p=C(214, 214, 206)),
    # Vennit Rusk, careful clerk of the Recension: a teal-cyan robe, a ledger held open
    "morrowfast_vennit": person(OPEN_FACE, {(13, 10): "g", (13, 11): "g", (14, 10): "p", (14, 11): "o"},
                                H=C(60, 60, 66), B=C(70, 150, 156), T=C(40, 96, 104), g=C(120, 84, 50), p=C(230, 226, 210)),
    # Pell Drowse, keeper of the Dry Hem: a moss-green jerkin, a pair of damp boots carried toe-down
    "morrowfast_pell": person(OPEN_FACE, {(13, 11): "d", (13, 12): "d", (14, 12): "d", (14, 11): "o"},
                              H=C(120, 96, 64), B=C(96, 124, 74), T=C(150, 132, 92), d=C(80, 64, 48)),
    # Orrit Coilstitch, rope mender: a green work smock, a coil of rope at the side
    "morrowfast_orrit": person(OPEN_FACE, {(13, 10): "r", (14, 10): "r", (13, 11): "r", (14, 12): "r", (13, 12): "r", (14, 11): "o"},
                               H=C(84, 70, 52), B=C(88, 118, 70), T=C(170, 140, 96), r=C(176, 140, 90)),
    # Sella Kettle of the Second Bowl: a pale apron over green, a kettle in hand
    "morrowfast_sella": person(OPEN_FACE, {(13, 11): "k", (14, 11): "k", (13, 12): "k", (14, 12): "k", (14, 10): "o", (6, 9): "a", (7, 9): "a",
                                           (6, 10): "a", (7, 10): "a", (8, 10): "a", (6, 11): "a", (7, 11): "a", (8, 11): "a"},
                               H=C(150, 92, 54), B=C(92, 126, 78), T=C(110, 140, 92), k=C(70, 72, 78), a=C(222, 214, 190)),
    # Farra Sprig, Bower-taught gardener: a leaf-green smock, a sprig held up
    "morrowfast_farra": person(OPEN_FACE, {(13, 8): "L", (12, 7): "L", (14, 7): "L", (13, 9): "t", (13, 10): "t"},
                               H=C(170, 120, 70), B=C(104, 146, 82), T=C(150, 120, 70), L=C(120, 176, 84), t=C(110, 80, 50)),
    # Edden Brack, a tired traveller home from the north: a dusty cloak, a pack, a walking stick
    "morrowfast_edden": person(OPEN_FACE, {**{(13, y): "w" for y in range(5, 15)}, (2, 8): "q", (2, 9): "q", (1, 9): "q", (2, 10): "q"},
                               H=C(90, 70, 50), B=C(110, 132, 84), T=C(140, 120, 84), w=C(120, 92, 60), q=C(150, 118, 76)),
}

if __name__ == "__main__":
    for name, (rows, pal) in PEOPLE.items():
        draw(rows, pal, name)
    print(len(PEOPLE), "residents")
