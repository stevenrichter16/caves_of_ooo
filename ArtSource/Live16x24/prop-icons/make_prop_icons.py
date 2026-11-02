"""Glyph -> pixel: fixed props (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md, props).

Editable source. Family templates in pixel rows; A/a/H take the prop's colour code (the muted
palette shared with item-icons); L/l are fixed leaf greens; X/x take an accent. Fixed materials
keep their own colours. Frames are 16x24, the game's cell: 16-row templates sit on the cell's
floor, and walls, doors and gates fill the full height so runs of them tile.

A prop with state gets one frame per state, so the picture never lies:
  crops   stage0 (sown), stage1 (sprout), stage2 (mature, the yield's colour; the plant's
          form follows its mature glyph: T tall, % bulb, # fibre, " leaf)
  doors   closed and open
Run `python3 make_prop_icons.py` here. Writes
  Assets/Resources/Sprites/Live16x24/prop-icons.png   (16 columns of 16x24 frames)
  Assets/Resources/Content/Data/PropIcons.json        (blueprint + state -> frame, glyph, colour, background)
Only placed, non-takeable, non-creature props that no flat catalog already names are indexed;
takeable things belong to item-icons. TheRoot is left to its own site art.
Original work; Caves of Qud furniture and terrain tiles were studied for silhouette only.
"""
import json, os, re, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "item-icons"))
import make_item_icons as items  # shared palette, blueprint helpers, already-drawn names

ROOT = items.ROOT
ATLAS = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Live16x24", "prop-icons.png")
INDEX = os.path.join(ROOT, "Assets", "Resources", "Content", "Data", "PropIcons.json")
W, H = 16, 24
LEAF = {"L": (78, 128, 56, 255), "l": (46, 84, 42, 255)}
FLAME = {"W": (250, 222, 120, 255), "Y": (255, 246, 200, 255), "R": (232, 96, 56, 255)}

T = {}
T["crop"] = [
    "................", "................", "................", "................", "................",
    "................", ".......oo.......", "......oXxo......", "...oo..oo..oo...", "..oXxo.oAo.oXxo.",
    "...oAo.oAo.oAo..", "....oAooAooAo...", ".....oAAAAAo....", "....otttttto....", "...otTttttTto...", "....oooooooo...."]
T["table"] = [
    "................", "................", "................", "................", "................",
    "................", "oooooooooooooooo", "oTTTTTTTTTTTTTTo", "otttttttttttttto", "oooooooooooooooo",
    ".oto........oto.", ".oto........oto.", ".oto........oto.", ".oto........oto.", ".ooo........ooo.", "................"]
T["desk"] = [
    "................", "................", "................", "................", "................",
    "....ooooo.......", "....oppppo......", "oooooooooooooooo", "oTTTTTTTTTTTTTTo", "otttttttttttttto",
    "oooooooooooooooo", ".otto......otto.", ".otto......otto.", ".otto......otto.", ".oooo......oooo.", "................"]
T["chest"] = [
    "................", "................", "................", "................", "................",
    "..oooooooooooo..", ".oTTTTTTTTTTTTo.", ".otAtttttttAtto.", ".oooooooooooooo.", ".otttttsSttttto.",
    ".otAtttssttAtto.", ".otttttttttttto.", ".oTttttttttttTo.", ".oooooooooooooo.", "................", "................"]
T["cabinet"] = [
    "................", "...oooooooooo...", "...oTTTTTTTTo...", "...otAAoAAAto...", "...otAAoAAAto...",
    "...otAAoAAAto...", "...otttottTto...", "...oooooooooo...", "...otAAoAAAto...", "...otAAoAAAto...",
    "...otsAoAsAto...", "...otttotttto...", "...oooooooooo...", "...oo......oo...", "................", "................"]
T["shelf"] = [
    "................", "..oooooooooooo..", "..oTttttttttTo..", "..otAXAxAAXAto..", "..otAXAxAAXAto..",
    "..oTttttttttTo..", "..otXAAxAXAAto..", "..otXAAxAXAAto..", "..oTttttttttTo..", "..otAAXAxAAXto..",
    "..otAAXAxAAXto..", "..oTttttttttTo..", "..oooooooooooo..", "..oo........oo..", "................", "................"]
T["anvil"] = [
    "................", "................", "................", "................", "................",
    "................", "..ooooooooooo...", ".oSSSSSSSSSSSoo.", "..ossssssssssSo.", "....osssssso....",
    ".....osssso.....", "....osssssso....", "...osssssssso...", "...oooooooooo...", "................", "................"]
T["gate"] = [
    "................", "................", ".oo..........oo.", ".oAo........oAo.", ".oAooooooooooAo.",
    ".oAoHoAoAoAooAo.", ".oAoAoAoAoAooAo.", ".oAoAoAoAoAooAo.", ".oAooooooooooAo.", ".oAoHoAoAoAooAo.",
    ".oAoAoAoAoAooAo.", ".oAoAoAoAoAooAo.", ".oAooooooooooAo.", ".oAo........oAo.", ".ooo........ooo.", "................"]
T["door"] = [
    "................", "...oooooooooo...", "..oAAAAAAAAAAo..", "..oAoooooooAAo..", "..oAoHAAAAoAAo..",
    "..oAoAAAAAoAAo..", "..oAoAAAAAoAAo..", "..oAoooooooAAo..", "..oAAAAAAAsAAo..", "..oAoooooooAAo..",
    "..oAoHAAAAoAAo..", "..oAoAAAAAoAAo..", "..oAoooooooAAo..", "..oAAAAAAAAAAo..", "..oooooooooooo..", "................"]
T["wall"] = [
    "oooooooooooooooo", "oHAAAoHAAAAoHAAo", "oAAAAoAAAAAoAAao", "oaaaaoaaaaaoaaao", "oooooooooooooooo",
    "oAAoHAAAAoHAAAAo", "oAAoAAAAAoAAAAao", "oaaoaaaaaoaaaaao", "oooooooooooooooo", "oHAAAoHAAAAoHAAo",
    "oAAAAoAAAAAoAAao", "oaaaaoaaaaaoaaao", "oooooooooooooooo", "oAAoHAAAAoHAAAAo", "oaaoaaaaaoaaaaao", "oooooooooooooooo"]
T["sign"] = [
    "................", "..oooooooooooo..", "..oAAAAAAAAAAo..", "..oApppppppAAo..", "..oAPPPPPPPAAo..",
    "..oApppppppAAo..", "..oAPPPPPAAAAo..", "..oAAAAAAAAAAo..", "..oooooooooooo..", "......otto......",
    "......otto......", "......otto......", "......otto......", ".....oTttto.....", ".....oooooo.....", "................"]
T["post"] = [
    "................", "................", ".......oo.......", "......oAAo......", "......oHAo......",
    "......oAAo......", ".......oto......", ".......oto......", ".......oto......", ".......oto......",
    ".......oto......", ".......oto......", "......oTtto.....", "......ooooo.....", "................", "................"]
T["trap_jaws"] = [
    "................", "................", "................", "................", "................",
    "................", "................", "...o.o.o.o.o....", "..oSoSoSoSoSo...", "..osssssssssso..",
    "..oo.......ooo..", "..osssssssssso..", "..oSoSoSoSoSo...", "...o.o.o.o.o....", "................", "................"]
T["trap_plate"] = [
    "................", "................", "................", "................", "................",
    "................", "................", "................", "..oooooooooooo..", "..oHAAAAAAAAAo..",
    "..oAAoAAAAoAao..", "..oAAAAAAAAaao..", "..oooooooooooo..", "................", "................", "................"]
T["trap_spikes"] = [
    "................", "................", "................", "................", "................",
    "................", "...o...o...o....", "..oSo.oSo.oSo...", "..oSo.oSo.oSo...", ".oSso.oSsooSso..",
    ".oSso.oSsooSso..", ".oooooooooooooo.", ".oAAAAAAAAAAAAo.", ".oooooooooooooo.", "................", "................"]
T["snare"] = [
    "................", "................", "................", "...........oo...", "..........oto...",
    ".........oto....", "....ooooooto....", "...oAo...ot.....", "..oAo....ot.....", "..oAo....ot.....",
    "...oAo..ot......", "....oAAAo.......", "....ooooo.......", "................", "................", "................"]
T["body"] = ["................"] * 8 + [
    "..ooo...........", ".oHAAoooooooooo.", "oHAAAokAAkAAkAAo", "oAAAAokAAkAAkAao", ".oaaaokaakaakao.",
    "..ooooooooooooo.", "................", "................"]
T["corpse"] = [
    "................", "................", "................", "................", "................",
    "................", "................", "................", "...oooooo.......", "..oAAAAAAoooo...",
    ".oeAAAAAAAAAAo..", "..oAAAaAAAaAAoo.", "...oaooaoaaoao..", "....o..o..o.o...", "................", "................"]
T["cup"] = [
    "................", "................", "................", "................", "................",
    "................", "....oooooooo....", "....oHAAAAAo....", "....oAAAAAAoo...", "....oAAAAAAoAo..",
    "....oAAAAAaoAo..", ".....oAAAao.o...", "......oooo......", "................", "................", "................"]
T["candle"] = [
    "................", "................", "................", ".......oo.......", "......oWWo......",
    "......oWYo......", ".......oo.......", "......oAAo......", "......oHAo......", "......oAAo......",
    "......oAAo......", ".....otAAto.....", ".....oooooo.....", "................", "................", "................"]
T["lantern"] = [
    "................", ".......oo.......", "......o..o......", ".....oooooo.....", ".....osssso.....",
    ".....oWYWWo.....", ".....oYWWWo.....", ".....oWWWWo.....", ".....osssso.....", ".....oooooo.....",
    ".......oo.......", ".......oo.......", ".......oo.......", "......oooo......", "................", "................"]
T["plant"] = [
    "................", "................", "....o......o....", "...oXo....oXo...", "...oXo.oo.oXo...",
    "....oo.oXo.oo...", ".....o.oXo.o....", "......ooooo.....", ".......oAo......", ".......oAo......",
    "......oAAo......", ".......oAo......", "......oaAo......", "......oooo......", "................", "................"]
T["rune"] = [
    "................", "................", "................", "................", "................",
    "................", ".....oooooo.....", "....oAAAAAAo....", "...oAAHAAHAAo...", "...oAAAHHAAAo...",
    "...oAAHAAHAAo...", "....oAAAAAAo....", ".....oooooo.....", "................", "................", "................"]
T["standing_stone"] = [
    "................", "................", "......oooo......", ".....oHAAAo.....", ".....oAAAAo.....",
    "....oHAAAAAo....", "....oAAeAAAo....", "....oAAeeAao....", "....oAAeAAao....", "....oAAAAAao....",
    "....oAAAAaao....", "...oAAAAaaaao...", "...oooooooooo...", "................", "................", "................"]
T["crystal"] = [
    "................", "................", "................", "................", ".......o........",
    "......oHo..o....", "......oHAooHo...", "...o..oHAooAo...", "..oHo.oAAooAo...", "..oAAooAAooAao..",
    "..oAaooAAaoAao..", ".ooooooooooooo..", "................", "................", "................", "................"]
T["device"] = [
    "................", "................", "................", "................", "....o....o......",
    "....o....o......", "...oooooooooo...", "...osssssssso...", "...osAHAAAAso...", "...osAAAeAAso...",
    "...osAAAAAAso...", "...osssssssso...", "...oooooooooo...", "................", "................", "................"]
T["paper"] = items.T["paper"]
T["barrel"] = [
    "................", "................", "................", "....oooooooo....", "...oTTTTTTTTo...",
    "...ottttttttto..", "...osssssssso...", "...otttttttto...", "...oTttttttto...", "...otttttttto...",
    "...osssssssso...", "...otttttttto...", "...oTTTTTTTTo...", "....oooooooo....", "................", "................"]
T["basket"] = [
    "................", "................", "................", "................", "................",
    "................", "..oooooooooooo..", "..oHAoAAoAAoAo..", "..oAoAAoAAoAAo..", "..oAAoAAoAAoAo..",
    "...oAoAAoAAoo...", "...oAAoAAoAAo...", "....oooooooo....", "................", "................", "................"]
T["rack"] = [
    "................", "..o.....o....o..", ".oSo...oSo..oSo.", ".oSo...oSo..oSo.", ".oSo...oSo..oSo.",
    "oooooooooooooooo", "oTTTTTTTTTTTTTTo", "oooooooooooooooo", "..oto..oto..oto.", "..oto..oto..oto.",
    "oooooooooooooooo", "oTTTTTTTTTTTTTTo", "oooooooooooooooo", ".oto.........oto", ".ooo.........ooo", "................"]
T["coals"] = [
    "................", "................", "................", "................", "................",
    "................", "................", "................", "......o.o.......", ".....oRoRo......",
    "...oooooooooo...", "..oAkAoRoAAkAo..", ".oAAAAkAAAkAAAo.", ".oooooooooooooo.", "................", "................"]
T["bundle"] = [
    "................", "................", "................", "................", "................",
    "................", "..oooooooooooo..", ".oTtttTtttTttto.", ".ooooooooooooooo", ".oTtttkttttkttto",
    ".ooooooooooooooo", ".oTtttTtttTttto.", "..oooooooooooo..", "................", "................", "................"]
T["coil"] = [
    "................", "................", "................", "................", "................",
    ".....oooooo.....", "....oAAAAAAo....", "...oAAoooooAo...", "...oAoAAAAoAo...", "...oAoAooAoAo...",
    "...oAoAAAAoAo...", "...oAAoooooAo...", "....oAAAAAAo....", ".....oooooo.....", "................", "................"]
T["frame"] = [
    "................", "................", "oooooooooooooooo", "oTTTTTTTTTTTTTTo", "oooooooooooooooo",
    "oto.oto..oto.oto", "oto.oto..oto.oto", "oto.oto..oto.oto", "oto.oto..oto.oto", "oooooooooooooooo",
    "oTTTTTTTTTTTTTTo", "oooooooooooooooo", "oto..........oto", "ooo..........ooo", "................", "................"]
T["bones"] = [
    "................", "................", "................", "................", "................",
    "................", "................", "................", "...oo....oo.....", "..oAAooooAAo.oo.",
    "...oAAAAAAoooAAo", "..oo.oAAAAAAAAo.", ".oAAoooAAoo..oo.", "..oo....oo......", "................", "................"]
T["placard"] = [
    "................", "................", "................", "..oooooooooooo..", "..oAAAAAAAAAAo..",
    "..oAppppppppAo..", "..oAPPPPPPPPAo..", "..oAppppppppAo..", "..oAPPPPPPAAAo..", "..oAAAAAAAAAAo..",
    "..oooooooooooo..", "................", "................", "................", "................", "................"]

T["graves"] = [
    "................", "................", "................", "...oooo...oooo..", "..oHAAAo.oHAAAo.",
    "..oAoAAo.oAoAAo.", "..oAAAao.oAAAao.", "..oAoAao.oAoAao.", "..oAAAao.oAAAao.", "..oooooo.oooooo.",
    "................", "......oooo......", ".....oHAAAo.....", ".....oAoAao.....", ".....oAAAao.....", ".....oooooo....."]
T["well"] = [
    "................", "..o..........o..", "..ooooooooooooo.", "..ot.........to.", "..ot....o....to.",
    "..ot....k....to.", "..ot....k....to.", "..ot...ooo...to.", "..oooooooooooo..", ".oHAAAAAAAAAAAo.",
    ".oAAeeeeeeeeAAo.", ".oAAAAAAAAAAAao.", ".oaaaaaaaaaaaao.", "..oooooooooooo..", "................", "................"]
T["grate"] = [
    "................", "................", "................", "oooooooooooooooo", "oSsoSsoSsoSsoSso",
    "osooosooosooosoo", "oooooooooooooooo", "oSsoSsoSsoSsoSso", "osooosooosooosoo", "oooooooooooooooo",
    "oSsoSsoSsoSsoSso", "osooosooosooosoo", "oooooooooooooooo", "................", "................", "................"]


T["crop0"] = ["................"] * 11 + [
    "......oAo.......", "....ootttoo.....", "...otTttttto....", "..otttttttttto..", "..oooooooooooo.."]
T["crop1"] = ["................"] * 8 + [
    ".......o.o......", "......oAoAo.....", ".....oHAoAAo....", "......oAAAo.....", ".......oAo......",
    "...ootttttttoo..", "..otTtttttttTto.", "..ooooooooooooo."]
T["crop2_tall"] = ["................"] * 2 + [
    ".....oo..oo.....", "....oHAooHAo....", "....oAaooAao....", ".oo..ooLLoo..oo.", "oHAo.oLLLLo.oHAo",
    "oAaoolLLLLlooAao", ".oolLLLoLLLlloo.", "..olLLLoLLLLlo..", "...olLLoLLLlo...", "....ollLoLllo...",
    ".....oolLloo....", "....ottttttto...", "...otTtttttTto..", "...ooooooooooo.."]
T["crop2_bulb"] = ["................"] * 5 + [
    "...o..oLoLo..o..", "...oLo.oLo..oLo.", "....oLooLooLo...", ".....oLlLlLo....", "....ooooLoooo...",
    "...oHAAooHAAo...", "..oHAAAaoAAAao..", "..oAAAaaoAAaao..", "..toaaaottaaot..", ".otTttttttttTto.", ".oooooooooooooo."]
T["crop2_fibre"] = ["................"] + [
    "...o...o...o....", "..oHo.oHo.oHo...", "..oAo.oAo.oAo...", "..oao.oao.oao...", "..oLo.oLo.oLo...",
    "..oLo.oLo.oLo...", ".oLLo.oLLooLo...", "..oLo.oLo.oLLo..", "..oLooolo.oLo...", "..oLo.oLo.oLo...",
    "..olo.olo.olo...", "..olo.olo.olo...", ".otttttttttttto.", ".oTttttttttttTo.", ".oooooooooooooo."]
T["crop2_leaf"] = ["................"] * 6 + [
    "......oooo......", "..oo.oHAAAo.oo..", ".oHAooAAaaooHAo.", ".oAAAoAaaoAAAao.", "..oAaAoaoAAaao..",
    "...oaaAoAaaoo...", "....oooLooo.....", "...ottttttto....", "..otTtttttTto...", "..ooooooooooo..."]
T["gate_open"] = ["................"] * 2 + [
    ".oo..........oo.", ".oAo........oAo.", ".oAoo.......oAo.", ".oAoHo......oAo.", ".oAoAo......oAo.",
    ".oAoAo......oAo.", ".oAoAo......oAo.", ".oAoHo......oAo.", ".oAoAo......oAo.", ".oAoAo......oAo.",
    ".oAoo.......oAo.", ".oAo........oAo.", ".ooo........ooo.", "................"]
T["door_open"] = [
    "................", "...oooooooooo...", "..oAAAAAAAAAAo..", "..oHoeeeeeeoAo..", "..oAoeeeeeeoAo..",
    "..oAoeeeeeeoAo..", "..oAoeeeeeeoAo..", "..oAoeeeeeeoAo..", "..oAoeeeeeeoAo..", "..oAoeeeeeeoAo..",
    "..oAoeeeeeeoAo..", "..oAoeeeeeeoAo..", "..oAoeeeeeeoAo..", "..oAoeeeeeeoAo..", "..oooooooooooo..", "................"]

T["boat"] = ["................"] * 6 + [
    "....oooooooo....", "..ooAAAAAAAAoo..", ".oHAAoAAoAAoAAo.", "oAAAAoAAoAAoAAAo", "oaaaaoaaoaaoaaao",
    "oooooooooooooooo", "..ot........to..", ".ot.o......o.to.", ".o...o....o...o.", "................"]
T["bay"] = ["................"] * 10 + [
    ".oooooooooooooo.", ".oHAAAAAAAAAAAo.", ".oA..........Ao.", ".oA..........Ao.", ".oAAAZZZZZoAAAo.", ".oooooooooooooo."]

def grid(h):
    return [["."] * W for _ in range(h)]

def field_gate(opened):
    """Timber rails between short posts, a brace and a plain latch below the top rail (16 rows).
    Open, the same posts stand unchanged and the leaf hangs swung toward the viewer from the left post."""
    g = grid(16)
    for x in (1, 12):
        for y in range(6, 16): g[y][x] = g[y][x + 2] = "o"; g[y][x + 1] = "A"
        g[6][x + 1] = "o"; g[7][x + 1] = "H"; g[15][x + 1] = "o"
    if opened:
        leaf = {7: (4, 6), 8: (4, 7), 9: (4, 7), 10: (5, 8), 11: (5, 8), 12: (5, 8), 13: (6, 8)}
        for y, (a, b) in leaf.items():
            for x in range(a, b + 1): g[y][x] = "o" if x in (a, b) else ("H" if y == 8 else "A")
        for x in range(5, 8): g[14][x] = "o"
    else:
        for x in range(4, 12):
            for y, ch in ((8, "o"), (9, "H"), (10, "o"), (12, "o"), (13, "A"), (14, "o")): g[y][x] = ch
        g[11][4], g[11][5] = "a", "a"
        g[11][10], g[11][11] = "S", "s"
    return ["".join(r) for r in g]

def L(rows, top=None, h=16):
    """Rows padded to 16 wide; placed `top` rows down (default: resting on the last row)."""
    rows = [r.ljust(16, ".") for r in rows]
    assert all(len(r) == 16 for r in rows), rows
    if top is None: top = h - len(rows)
    return ["." * 16] * top + rows + ["." * 16] * (h - top - len(rows))

def hashname(name):
    h = 0
    for ch in name: h = (h * 31 + ord(ch)) & 0xFFFFFFFF
    return h

def planks(variant):
    """Wide weathered boards: coherent left light, dark joints and shallow batten relief.
    Full-height opaque rows continue through adjacent cells; no per-cell top cap or shadow footprint."""
    g = grid(H); joints = (0, 6, 11)
    for y in range(H):
        for x in range(W):
            g[y][x] = "a" if x in joints else "H" if x in (1,7,12) else "B" if 7<x<11 else "A"
    # Low-contrast grain is distinct from the stronger board joints.
    for x,y in (((3,4),(9,17),(14,20)),((4,18),(9,3),(14,6)),((3,16),(8,5),(14,19)))[variant]:
        g[y][x]="g"; g[y+1][x]="g"
        if x+1 not in joints and x+1<W:g[y+1][x+1]="a"
    for x in range(W):
        g[9][x]="H";g[10][x]="A";g[11][x]="b";g[12][x]="d"
    for x in (2,8,14):g[10][x]="s";g[11][x]="d"
    return ["".join(r) for r in g]

def chalk_wall():
    """Whitewash toned below the player, courses staggered by 8, no doubled seam, pale chalk names low down."""
    g = grid(H)
    for y in range(H):
        for x in range(W): g[y][x] = "A"
    for y in (7, 15, 23):
        for x in range(W): g[y][x] = "a"
    for y in range(0, 7): g[y][0] = "a"
    for y in range(8, 15): g[y][8] = "a"
    for y in range(16, 23): g[y][0] = "a"
    for x in range(W):
        if g[0][x] == "A": g[0][x] = "H"
    names = [(2, 13, 10), (3, 14, 12), (1, 11, 17), (4, 15, 19), (2, 9, 21)]
    for x0, x1, y in names:
        for x in range(x0, x1):
            if (x * 3 + y) % 4 and g[y][x] == "A": g[y][x] = "Z"
    return ["".join(r) for r in g]

def ice_wall():
    """Irregular ice blocks with translucent glints, not tiled bathroom brick."""
    g = grid(H)
    for y in range(H):
        for x in range(W): g[y][x] = "A"
    seams = [(0, [0, 6, 11]), (6, [3, 9, 14]), (13, [1, 7, 12]), (19, [4, 10])]
    for i, (row, cuts) in enumerate(seams):
        for x in range(W): g[row][x] = "a"
        nxt = seams[i + 1][0] if i + 1 < len(seams) else H
        for y in range(row + 1, nxt):
            for c in cuts: g[y][c] = "a"
    for x, y in ((2, 2), (3, 3), (8, 8), (9, 9), (5, 15), (6, 16), (12, 21), (13, 22), (13, 3), (11, 15)): g[y][x] = "H"
    return ["".join(r) for r in g]

def barred_gate(opened):
    """Salt-white posts and 1-px bars with the ground between; one middle lock bar in the glyph's colour.
    Its top rail sits on row 2, the iron rail's cap row, so a gate in a rail line reads as one fence."""
    g = grid(H)
    for x in (0, 14):
        for y in range(2, 23): g[y][x], g[y][x + 1] = "Z", "z"
    if opened:
        for y in range(3, 22): g[y][2], g[y][3] = "Z", "z"
        return ["".join(r) for r in g]
    for x in range(2, 14):
        g[2][x], g[3][x] = "Z", "z"; g[20][x], g[21][x] = "Z", "z"
    for x in (3, 5, 7, 9, 11, 13):
        for y in range(4, 20): g[y][x] = "Z"
    for x in range(2, 14): g[11][x], g[12][x] = "A", "a"
    g[11][8] = "s"; g[12][8] = "e"
    return ["".join(r) for r in g]

def iron_rail(card):
    """Close-set iron rails under a cap on row 2, salted heavy at the base; one cell in two carries the card."""
    g = grid(H)
    for x in range(W): g[2][x] = "S"; g[3][x] = "s"
    for x in range(W):
        if x % 3 != 2:
            for y in range(4, 20): g[y][x] = "S" if x % 3 == 0 else "s"
    for y in range(18, H):
        for x in range(W):
            if y >= 20 or (x + y) % 3 == 0: g[y][x] = "Z" if (x * 7 + y) % 5 else "z"
    if card:
        g[6][9] = "k"; g[7][9] = "k"
        for y in (8, 9):
            for x in (8, 9, 10): g[y][x] = "p"
        g[9][10] = "P"
    return ["".join(r) for r in g]

LETTERS = {"North": ["e...e", "ee..e", "e.e.e", "e.e.e", "e..ee", "e...e", "e...e"],
           "East": ["eeeee", "e....", "e....", "eeee.", "e....", "e....", "eeeee"],
           "South": [".eeee", "e....", "e....", ".eee.", "....e", "....e", "eeee."],
           "West": ["e...e", "e...e", "e...e", "e.e.e", "e.e.e", "ee.ee", "e...e"]}

def compass(direction):
    """A standing menhir, not a headstone: tapered from 10 px at the foot to a peak 1 px off-centre,
    a notch cut into one shoulder, a few moss pixels at the foot, its quarter's letter cut large."""
    shape = [".......oo", "......oHHo", ".....oHHAAo", ".....oHHAAo", "....oHHHAAo", "....oHHHHAao",
             "...ooHHHHAao", "..oHHHHHHAAao", "..oHHHHHHHAao", "..oHHHHHHHAao", "..oHHHHHHHAao", "..oHHHHHHHAao",
             "..oAHHHHHHAao", "..oAAAAAAAAao", "..oAAAAAAAaao", "..oAAAAAAaaao", ".oAAAAAAAaaaao", ".oLlaaaaaaLlao", "..oooooooooooo"]
    rows = [list(r) for r in L(shape, h=H)]
    for dy, line in enumerate(LETTERS[direction]):
        for dx, ch in enumerate(line):
            if ch == "e": rows[H - len(shape) - 1 + 7 + dy][4 + dx] = "e"
    return ["".join(r) for r in rows]

def full_shelf():
    """An archive shelf as tall as a person: four boards of mixed spines, one gap where a volume is out."""
    g = grid(H)
    for y in range(1, 23):
        g[y][1] = g[y][14] = "o"; g[y][2] = "T"; g[y][13] = "t"
    for x in range(1, 15): g[1][x] = "o"; g[22][x] = "o"
    spines = "RBGWPRGBWR"
    for i, top in enumerate((2, 7, 12, 17)):
        for x in range(2, 14): g[top + 4][x] = "T"
        for x in range(3, 13):
            c = spines[(x * 3 + i * 5) % len(spines)]
            if (x + i) % 7 == 5: continue
            for y in range(top + (1 if (x + i) % 3 else 0), top + 4): g[y][x] = c
    return ["".join(r) for r in g]

SPINES = {"R": (138, 58, 48, 255), "B": (64, 82, 120, 255), "G": (74, 104, 64, 255), "W": (170, 150, 98, 255), "P": (196, 186, 160, 255)}
STONE = {"A": (126, 126, 132, 255), "a": (96, 96, 102, 255), "H": (158, 158, 162, 255)}
GLASS = {"G": (150, 156, 164, 255), "g": (184, 192, 198, 255)}
BOARD = {"A": (140, 104, 60, 255), "a": (104, 76, 44, 255), "H": (166, 128, 80, 255)}

T["toppled_shelf"] = L(["....oo", "...oTto", "..oTttooooooooo", ".oTtRBGWoRBGWto", "oTtoRBGWoPRGBto", "otooooooooooooo",
                        ".o.........oo", "........oPGo.oRo", ".........oo..oo"])
T["rack"] = L(["...o....o....o", "..oSo..oSo..oSo", "..oSo..oSo..oSo", "..oSo..oSo..oSo", "..oSo..oso..oSo",
               "oooooooooooooooo", "oTTTTTTTTTTTTTTo", "oooTooooTooooToo", "..oto..oto..oto", "..oto..oto..oto",
               "oooooooooooooooo", "oTTTTTTTTTTTTTTo", "oooooooooooooooo", ".oto........oto.", ".ooo........ooo."])
T["stone_chest"] = L(["..oooooooooooo", ".oHHHHHHHHHHHHo", ".oAAAAAAAAAAAAo", ".oaaaaaaaaaaaao", ".oAAAAAAAAAAAAo",
                      ".oAAAAAsSAAAAAo", ".oAAAAAAAAAAAAo", ".oaaaaaaaaaaaao", ".oooooooooooooo"])
T["slab"] = L(["oooooooooooooooo", "oHHHHHHHHHHHHHHo", "oAAAAAAAAAAAAAAo", "oAAAAAAAAAAAAAAo", "oaaaaaaaaaaaaaao",
               "oooooooooooooooo", ".oAAo......oAAo.", ".oAao......oAao.", ".oAao......oAao.", "oAaaao....oAaaao", "oooooo....oooooo"])
T["sign"] = L(["..oooooooooooo", "..oHAAAAAAAAAo", "..oApppAAppPAo", "..oAPPpAApPpAo", "..oAppPAAPppAo",
               "..oAAAAAAAAAAo", "..oooooooooooo", "...ot......to", "...ot......to", "...ot......to",
               "...ot......to", "..oTto....oTto", "..oooo....oooo"])
T["post"] = L(["....oooooo", "....oHAAAo", "....oApPAo", "....oAAAAo", "....oooooo", ".......oto", ".......oto",
               ".......oto", ".......oto", ".......oto", "......oTtto", "......ooooo"])
T["trap_jaws"] = L(["...o.o.o.o.o", "..oSaSaSaSaSo", "..oSSSSSSSSSo", "...oooooooo", "..oHHHHHHHHHo..o",
                    "..oaaaosoaaao.oo", "..oSSSSSSSSSo.oo", "...oSaSaSaSo.oo", "....o.o.o.o.oo", "............o"])
T["graves"] = L(["...oo.......oo", "..oHHo.....oHHo", ".oHHAAo...oHHAAo", ".oHeHAo...oHeHAo", ".oeeeAo...oeeeAo",
                 ".oHeAAo...oHeAAo", ".oHAAao...oHAAao", ".oAAaao...oAAaao", "ttttttttttttttttt"[:16],
                 "tTttTttttTttTttt"])
T["boat"] = L(["oooooooooooooo", "oHHHHHHHHHHHHHoo", "oAAAAAAAAAAAAAAo", "oaaaaaaaaaaaaaAo", "oAAAAAAAAAAAAao",
               "ooooooooooooooo", ".o.o.......o.o", "..o.........o", ".o.o.......o.o", "o...o.....o...o"])
T["basket"] = L(["..ooo......ooo", "..o.oo....oo.o", "...o.oooooo.o", "..oHAaHAaHAaHo", ".oAaHAaHAaHAaAo",
                 ".oaHAaHAaHAaHao", "..oAaHAaHAaHAo", "..oaHAaHAaHAao", "...oooooooooo"])
T["rune"] = L(["....oooooo", "...o......o", "..o..AHA...o", "..o.A...A..o", "..o..AAA...o", "..o...A....o",
               "...o......o", "....oooooo"])
T["device"] = L(["......o.o", "....ooSoSoo", "...oSSkSkSSo", "..oSsaoSoasSo", "..oSsssSsssSo", "..oSkkksskkSo",
                 "..oSssksskssSo"[:16], "..oSkkksskkSo", "..oSsssssssSo", "..osSSSSSSSso", "...ooooooooo"])
T["body"] = L([".oooo", "oHAAAoooooooo", "oAAAAoHAkAAkAoo", "oAAAAaAAAkkAAAAo", ".oaao.oAkaakaaao", "..oo...oooooooo"])
T["cabinet"] = L(["...oooooooooo", "...oTTTTTTTTo", "...otAAoAAAto", "...otAAoAAAto", "...otAAoAAAto",
                  "...otttottTto", "...oooooooooo", "...otAAoAAAto", "...otAAoAAAto", "...otsAoAsAto",
                  "...otttotttto", "...oooooooooo", "...oo......oo"])
T["glass_case"] = L(["..oooooooooooo", "..oTTTTTTTTTTo", "..otGgGGGgGGto", "..otGGgGGGgGto", "..otGGGgGGGgto",
                     "..otGGGGgGGGto", "..oTTTTTTTTTTo", "..otAAoAAoAAto", "..otAAoAAoAAto", "..oTTTTTTTTTTo",
                     "..oooooooooooo", "..oo........oo"])

# Crops: one 12x3 soil bed on the same rows at every stage; the sprout is a small version of its
# mature leaf; each mature form has its own silhouette (review: one recoloured clump read as a paw).
BED = ["..ootttttttoo", ".otTttttttTto", ".oooooooooooo"]
SPROUT = {"tiers": ["......oo.oo", ".....oLlolLo", "......oLLo", ".......oo"],
          "vine": [".......oo", "......oLo", ".......tLo", ".......to"],
          "mat": ["....oo..oo..oo", "...oLLooLLooLLo"],
          "bells": ["......oo", ".....oLLo", "......oLo.", "......ol"],
          "grass": ["......o..o", "......oLoLo", ".......LoL", ".......oo"],
          "shrub": ["......ooo", ".....oLLLo", "......olo"]}
MATURE = {
    "tiers": [".......oo", "......oXXo", ".....oLloLlo", "....oLLlolLLo", "......oLLo", "...oLLloollLLo",
              "..oLLLlooolLLLo", ".....ollllo", "...oLLLoloLLLo", "..oLLlo.o.olLLo", "......olo"],
    "vine": ["........oo", ".......oto", "..oo...oto.oo", ".oXXo.ootoolLo", ".oXxo.oLtLLo", "..oo.oLotolLo",
             "....oLo.toooXXo", "...oLLo.to.oXxo", "....oo..toLo.oo", "..oXXo.otoLLo", "..oXxooLto.oo", "...oo..oto"],
    "mat": ["...o..o...o..o", "..oXo.oXo.oXo.oXo"[:16], "oLLLoLLLoLLLoLLo", "oLLlLLLlLLLlLLLlo"[:16],
            "olLlllLllllLllLo", ".ooooooooooooooo"],
    "bells": ["......oo..oo", ".....olLooLlo", "...oo.oLoolo.oo", "..oLLooLLLloLLo", "...oolLolLolLoo",
              "...oXXooLoooXXo", "..oXHXo.lo.oXHXo", "..oXxXo.lo.oXxXo", "...ooo.oLo..ooo", "......oLlo"],
    "grass": ["...o..o...o..o", "..oXooXo.oXooXo", "..oXooXo.oXooXo", "...oLoLo..oLoLo", "...oLoLoooLoLo",
              "...oLoLoLoLoLo", "....oLoLoLoLo", "....oLlLlLlLo", ".....oLlLlLo", ".....ollllo"],
    "shrub": ["....oo..oo", "...oXXooXXo", "...oXxooXxoo", "..oLLoLLoLLLo", ".oLLlLLllLLlLo", ".olLLXXoLLlLLo",
              "..oLLXxolLXXo", "..ollLooLlXxo", "...oollttlloo", "......otto"],
}
def crop_frame(stage, form, mature_glyph):
    if stage == 0: body = ["......oXo", ".....oXxo"]
    elif stage == 1: body = SPROUT[form]
    elif mature_glyph in ('%', '#', '"'): return T[{"%": "crop2_bulb", "#": "crop2_fibre", '"': "crop2_leaf"}[mature_glyph]]
    else: body = MATURE[form]
    return L(body + BED)
def crop_form(name):
    has = lambda *ks: any(k in name for k in ks)
    if has("pea", "bean", "gourd", "pod", "Gourd"): return "vine"
    if has("cress", "moss", "lichen", "Knit"): return "mat"
    if has("bell", "chime", "puff", "cap", "button", "bladder", "lantern", "vein", "wick"): return "bells"
    if has("sedge", "rush", "grass", "reed", "flax", "Spur"): return "grass"
    if has("bark", "root", "sundew", "Sundew", "mantle", "sieve", "prism", "Prism", "Seal", "Shale"): return "shrub"
    return "tiers"

# Full-height families: walls course every 8 rows so stacked cells tile; doors and gates
# stand as tall as the wall they sit in.
COURSE_A = ["oHAAAoHAAAAoHAAo", "oAAAAoAAAAAoAAao", "oaaaaoaaaaaoaaao", "oooooooooooooooo"]
COURSE_B = ["oAAoHAAAAoHAAAAo", "oAAoAAAAAoAAAAao", "oaaoaaaaaoaaaaao", "oooooooooooooooo"]
TALL = {"wall": (COURSE_A + COURSE_B) * 3, "gate": barred_gate(False), "gate_open": barred_gate(True),
        "rail": iron_rail(True), "rail_b": iron_rail(False), "chalk_wall": chalk_wall(), "ice_wall": ice_wall(),
        "planks": planks(0), "planks_b": planks(1), "planks_c": planks(2), "shelf": full_shelf(),
        "compass_North": compass("North"), "compass_East": compass("East"), "compass_South": compass("South"), "compass_West": compass("West")}
T["field_gate"], T["field_gate_open"] = field_gate(False), field_gate(True)

def stretch(rows, at, extra):
    """Trim blank margins and repeat row `at` until the template fills the 24-row cell."""
    rows = [r for r in rows]
    while rows and set(rows[0]) == {"."}: rows.pop(0)
    while rows and set(rows[-1]) == {"."}: rows.pop()
    need = H - len(rows)
    return rows[:at] + [rows[at]] * need + rows[at:] if need > 0 else rows

def frame_rows(fam):
    if fam in TALL: return TALL[fam]
    if fam in ("door", "door_open"): return stretch(T[fam], 5, 0)
    return ["................"] * (H - 16) + T[fam]



def family(name, glyph):
    has = lambda *ks: any(k in name for k in ks)
    if has("Bedroll"): return "bedroll"
    if has("Trough"): return "trough"
    if has("HitchingPost"): return "hitching_post"
    if has("OverturnedCart"): return "cart_over"
    if has("BoatHookPost"): return "hook_post"
    if has("Cistern"): return "well"
    if has("TokenDish"): return "bowl"
    if has("Crop"): return "crop"
    if has("Corpse"): return "corpse"
    if glyph == "&": return "body"
    if has("Anvil"): return "anvil"
    if has("Graveyard"): return "graves"
    if has("Well"): return "well"
    if has("Grate"): return "grate"
    if has("Gate"): return "gate"
    if has("Rail"): return "rail"
    if has("BoatFrame"): return "boat"
    if has("NameWall"): return "chalk_wall"
    if has("Bay"): return "bay"
    if has("Door"): return "door"
    if glyph == "#" and has("Timber"): return "planks"
    if glyph == "#" and has("Ice"): return "ice_wall"
    if glyph == "#": return "wall"
    if has("Bookshelf"): return "toppled_shelf"
    if has("Shelf"): return "shelf"
    if has("Case"): return "glass_case"
    if has("Cabinet", "Locker"): return "cabinet"
    if has("Coffer"): return "stone_chest"
    if has("Chest", "Salvage", "Reliquary"): return "chest"
    if has("Desk"): return "desk"
    if has("Slab"): return "slab"
    if has("Table", "Bench", "Counter", "Rack") and glyph == "=": return "table"
    if has("WeaponRack"): return "rack"
    if has("BearTrap"): return "trap_jaws"
    if has("SpikeTrap"): return "trap_spikes"
    if has("Snare"): return "snare"
    if glyph == "^": return "trap_plate"
    if has("Post") and not has("Passage"): return "post"
    if has("Notice", "Sign", "Post", "Wayboard", "Rolls"): return "sign"
    if has("Stake"): return "post"
    if has("Placard", "Index"): return "placard"
    if glyph == "?": return "paper"
    if has("Cup"): return "cup"
    if has("Candle"): return "candle"
    if has("Lantern"): return "lantern"
    if has("Rune"): return "rune"
    if has("CompassStone"): return "compass_" + name[len("CompassStone"):]
    if has("Quartz"): return "crystal"
    if has("Capacitor"): return "device"
    if has("Barrel"): return "barrel"
    if has("Basket"): return "basket"
    if has("Coals"): return "coals"
    if has("Frame"): return "frame"
    if has("Timber", "Clay"): return "bundle"
    if has("Cord", "Rope"): return "coil"
    if has("BoneCache"): return "bones"
    if glyph == "!": return "plant"
    if glyph == "=": return "table"
    return "chest"

# ════════════════ scene owners of the painted places (rank 1, sub-milestones 2-3) ════════════════
# Morrowfast's furniture and the Felling-Site's layers are generic owners (MorrowfastSceneProp,
# FellingSceneProp) whose kind lives in the scene definition, keyed by ComponentId. Each gets the frame
# of its kind, in the painted scene's own palette (the owners' glyph colour is a uniform &y).

SCENE_PALETTES = {
    "petrified": {"A": (138, 106, 110, 255), "a": (106, 78, 82, 255), "H": (176, 140, 142, 255), "L": (78, 106, 50, 255), "l": (56, 80, 38, 255)},
    "red_plant": {"A": (168, 64, 46, 255), "a": (120, 42, 32, 255), "H": (204, 104, 76, 255)},
    "cyan_stem": {"A": (88, 184, 176, 255), "a": (56, 130, 126, 255), "H": (150, 222, 214, 255)},
    "pink_cap": {"A": (208, 122, 154, 255), "a": (156, 84, 112, 255), "H": (236, 168, 190, 255)},
    "cream_stalk": {"A": (224, 207, 160, 255), "a": (178, 158, 112, 255), "H": (240, 230, 196, 255), "X": (200, 96, 90, 255)},
    "blue_rosette": {"A": (88, 120, 184, 255), "a": (60, 86, 140, 255), "H": (134, 164, 216, 255)},
    "pale_spire": {"A": (200, 196, 184, 255), "a": (156, 152, 140, 255), "H": (222, 220, 210, 255), "L": (78, 106, 50, 255), "l": (56, 80, 38, 255)},
    "boulder": {"A": (122, 118, 114, 255), "a": (92, 88, 86, 255), "H": (156, 152, 146, 255), "L": (78, 106, 50, 255), "l": (56, 80, 38, 255)},
    "blackwater": {"A": (46, 58, 64, 255), "a": (32, 40, 46, 255), "H": (106, 138, 148, 255), "Z": (200, 220, 226, 255), "z": (150, 176, 184, 255)},
    "thatch": {"A": (150, 142, 74, 255), "a": (110, 102, 52, 255), "H": (184, 176, 104, 255)},
}

T["petrified_trunk"] = L(["...oo.oo..oo", "..oHAoHAooHAo", "..oHAaoHAoHAao", ".oHAAaoHAaoHAao", ".oHAAaoHAaoHAao", ".oHAAaoHAAoHAao",
                          ".oHAAaoHAAoHAao", ".oHAAAoHAAaHAao", "oHAAAaoHAAaoHAao", "oLHAAaoHAAaoHAao", "olLLoooLLooLLlo"])
T["root_buttress"] = L(["......oo", ".....oHAo", "....oHAAo", "...oHAAao", "..oHAAaao", ".oHAAaao", "oHAAaaoooo", "oHAAAAAAHAo",
                        "oLAAaaaaaAAo", "olLLooooLLlo"])
T["root_arch"] = L(["...oooooooo", "..oHAAAAAAHo", ".oHAoooooAAo", ".oHo.....oAo", ".oAo.....oAo", ".oAo.....oAo", "oHAo.....oAAo",
                    "oLlo.....oLlo"])
T["moss_column"] = L(["....oooo", "...oLLLLo", "...olLLlo", "....oHAo", "....oHAo", "....oHAo", "....oHAo", "....oHAao",
                      "...oHAAao", "...oLlaao", "...ooooo"])
T["red_branches"] = L(["...o...o..o", "..oHo.oHooHo", "...oAooAoAo..o", ".o..oAAoAo..oHo", "oHo..oAAo..oAo", ".oAo.oAo.oAo",
                       "..oAooAooAo", "...oAAAAAo", "....oaAao", ".....oao"])
T["cyan_stems"] = L(["..oo....oo", ".oHHo..oHHo.oo", ".oAAo..oAAooHHo", "..oo.o..oo.oAAo", "....oao..oo.oo", "..o.oao.oao.o",
                     ".oao.oaooao.oao", "..oaooaooaooao", "...oaaaaaaao"])
T["pink_caps"] = L(["...ooooo", "..oHHAAAo", ".oHAAAAAAo..ooo", ".oaaaaaaao.oHAAo", "...ozzo...oaaaao", ".ooozzo.ooo.ozo",
                    "oHAo.zo.oHAoozo", "oaao.zz.oaao.zo", ".ozo.zz..ozo.zz"])
T["stalk_fungus"] = L(["......ooo", ".....oHXAo", ".....oXXao", "......oAo", "......oAo", "......oAo...oo", "..oo..oAo..oXAo",
                       ".oXAo.oAo..oXao", ".oXao.oAo...oAo", "..oAo.oAo...oAo", "..oAo.oAao..oAo"])
T["blue_rosette"] = L(["......oo", "..oo.oHAo.oo", ".oHAooHAaooHAo", ".oAAaoAAaoAAao", "..oaaoaaooaao", ".oHAAooAoooHAAo",
                       "oHAAaaooooaaAAo", ".oaao.oaao.oaao", "..oo...oo...oo"])
T["pale_spires"] = L(["....o.....o", "...oHo...oHo", "...oAo.o.oAo", "..oHAooHooAo", "..oHAooAooAao", "..oHAaoAooAao",
                      ".oHAAaoAaoAao", ".oHAAaoAaoAAao", "oLHAAaoAaoAAaLo", "olLLLooooooLllo"])
T["pale_tubes"] = L(["...oo.oo.oo", "..oHAoHAoHAo", "..oe.oe.oe.o", "..oHAoHAoHAo", "..oHAoHAoHAo", ".ooHAoHAoHAoo", ".oLHAoHAoHALo",
                     ".olLLooooLLlo"])
T["boulder"] = L(["....oooooo", "..ooHHHAAAoo", ".oHHHAAAAAAao", ".oHAAAAAAAaao", "oHAAAAAAAAaaao", "oAAAAAAAAaaaao",
                  "oLAAAAAAaaaaLo", ".oLLloooooLLo"])
T["rock_cluster"] = L(["...ooo", "..oHAAo..ooo", "..oAAao.oHAAo", ".ooaaaoooAAao", "oHAAooHAAoaao", "oAAaooAAAaoo", ".oooo.oooo"])
T["bank_stone"] = L(["....oooo", "...oHHAAo", "...oHAAao", "..oHAAAao", "..oHAAaao", "..oHAAaao", ".oHAAAaaao", ".oLAAaaaao",
                     ".olLLooLLo"])
T["waterfall"] = ["....oZzZzZzo...."] * 2 + ["....ozZzZzZo...."] * 2 + ["....oZzHzZzo...."] * 2 + ["....ozZzHzZo...."] * 2 + \
                 ["....oZzZzZzo...."] * 6 + ["...ozZzZzZzZo..."] * 2 + ["..oZzZzZzZzZzo.."] * 2 + ["..ozZzZzZzZzZo.."] * 2 + \
                 ["..oAzAzAzAzAzo.."] + [".oAAAAAAAAAAAAo."] + [".oaaaaaaaaaaaao."] + ["................"]
T["mist"] = L(["....zz..zz", "..zzZZzzZZzz", ".zZZZZZZZZZZz", "zZZZzZZZZzZZZz", ".zzZZZZzZZZzz", "...zzzzzzzz"], 6)
T["river_water"] = L(["oooooooooooooooo", "AAAAHAAAAAAAHAAA", "AaAAAAAAHAAAAAaA", "AAAAAAaAAAAAAAAA", "AHAAAAAAAAAaAAHA",
                      "AAAAAAAHAAAAAAAA", "aAAaAAAAAAAAaAAA", "oooooooooooooooo"])
# Morrowfast furniture
T["bed"] = L(["oooooooooooooooo", "oTTTTTTTTTTTTTTo", "otppPoAAAAAAAAto", "otpPPoAHAAAHAAto", "otppPoAAAAAAAato", "oTTTToaaaaaaaaTo",
              "oooooooooooooooo", ".oto........oto.", ".ooo........ooo."])
T["stool"] = L(["....oooooooo", "....oTTTTTTo", "....oooooooo", ".....ot..to", ".....ot..to", ".....ot..to", "....oTo..oTo", "....ooo..ooo"])
T["bench"] = L(["oooooooooooooooo", "oTTTTTTTTTTTTTTo", "oooooooooooooooo", ".oto........oto.", ".oto........oto.", ".ooo........ooo."])
T["bucket"] = L(["....o.....o", "....oo...oo", ".....oooooo", "....oSsssSo", "....otTttto", "....oCcccco", "....otTttto", "....osSsSso",
                 ".....oooooo"])
T["crate"] = L(["..oooooooooooo", "..oTTTTTTTTTTo", "..otoooooooTto", "..ottTooTttoto", "..ottoTTottoto", "..ottooTTotoTo",
                "..otToottTotto", "..oTttttttttto", "..oooooooooooo"])
T["bowl"] = L(["...oooooooooo", "..oCcHCCCCcCCo", "..oAAAAAAAAAAo", "...oaAAAAAAao", ".....oooooo", "......oAo", ".....oAAao", "....ooooooo"])
T["bridge"] = L(["oooooooooooooooo", "oTtToTtToTtToTto", "otTtotTtotTtotTo", "oTtToTtToTtToTto", "oooooooooooooooo",
                 "oCcCcCcCcCcCcCco"])
T["cart"] = L(["........oo", "..oooooooTo", ".oTTTTTTTto", ".otAAkAkAtoo", ".otttttttto.oo", "..oooooooooTTo", "...ooo..ooo.oo",
               "..oSsSo.oSsSo", "..osoSo.osoSo", "...ooo...ooo"])
T["arch"] = [".oo..........oo.", ".oTooooooooooTo.", ".oTTTTTTTTTTTTo.", ".oToooooooooooo.", ".oTo.kk..kk.oTo.", ".oTo.Ak..kA.oTo.",
             ".oTo........oTo.", ".oTo........oTo."] + [".oto........oto."] * 13 + [".oTo........oTo.", "oTTto......oTTto", "oooooo....oooooo"]
T["stove"] = L(["......oo", "......oso", "......oso", "...oooosoo", "..oSSSSSSSo", "..osssssssso"[:16], "..osWRYWRso", "..osRWRWsso",
                "..osssssssso"[:16], "..oooooooooo"])
T["masonry_hearth"] = L(["...oooooooooo", "..oHHHHHHHHHHo", "..oAAoooooAAAo", "..oAoeeeeeoAAo", "..oAoeWYWeoAao", "..oAoRWRWRoAao",
                         "..oAoooooooAao", "..oaaaaaaaaaao", "..oooooooooooo"])
T["bread_oven"] = L(["....oooooo", "...oHHHAAAo", "..oHHAAAAAAo", ".oHAAAAAAAAao", ".oHAAooooAAao", ".oAAoeWYeoaao", ".oAAoRWRWoaao",
                     "oHAAAooooAAaao", "ooooooooooooooo"])
T["planter"] = L(["..o.o..o..o.o", ".oXooXoXooXoXo", "..oLooLoLooLo", ".oLloLlLloLlo", "oooooooooooooo", "oTTTTTTTTTTTTo",
                  "otttttttttttto", "oooooooooooooo"])
T["herb_pot"] = L([".....o.o", "....oLoLo", "...oLLoLLo", "....oLlLo", "...ooooooo", "...oHAAAao", "....oAAao", "....oaaao", ".....ooo"])
T["stall"] = ["oooooooooooooooo", "oCcCcCcCcCcCcCco", "occCcCcCcCcCcCco", "oooooooooooooooo", ".oto........oto.", ".oto........oto.",
              ".oto........oto.", ".oto........oto."] + ["................"] * 4 + ["oooooooooooooooo", "oTTTTTTTTTTTTTTo", "otXxAoXxAoXxAoto",
              "oTTTTTTTTTTTTTTo", "otttttttttttttto", "oTo..........oTo", "oto..........oto", "oto..........oto", "oto..........oto",
              "oTo..........oTo", "ooo..........ooo", "................"]
T["workbench"] = L(["...o......oo", "..oSo....oSso", "oooSoooooooooooo", "oTTTTTTTTTTTTTTo", "oooooooooooooooo", ".ot.oSSo.....to.",
                    ".ot.oooo.....to.", ".ooo........ooo."])
T["chopping_block"] = L([".......o", "......oSo", "......oSo", ".....ootoo", "...oTTTTTTo", "..oTtTtTtTto", "..otttttttto",
                         "..oTtTtTtTto", "...oooooooo"])

def scene_family(kind, cid):
    has = lambda *ks: any(k in cid for k in ks)
    if kind == "container":
        return "bucket" if has("bucket") else "basket" if has("basket") else "crate" if has("crate") else "barrel" if has("barrel") else "chest"
    if kind == "table": return "desk" if has("desk") else "table"
    if kind == "workstation": return "stove" if has("stove") else "chopping_block" if has("block") else "workbench"
    if kind == "hearth": return "masonry_hearth" if has("masonry") else "stove"
    if kind == "planter": return "herb_pot" if has("pot") else "planter"
    return {"bed": "bed", "stool": "stool", "bench": "bench", "bookshelf": "shelf", "bowl": "bowl", "bridge": "bridge",
            "cart": "cart", "cistern": "well", "gate": "arch", "oven": "bread_oven", "rope": "coil", "salvage": "coil",
            "shop-stall": "stall", "workbench": "workbench",
            "petrified-trunk": "petrified_trunk", "petrified-root-buttress": "root_buttress", "foreground-root-structure": "root_arch",
            "moss-capped-petrified-column": "moss_column", "red-branching-plant-cluster": "red_branches",
            "cyan-stem-cluster": "cyan_stems", "cyan-and-pale-stem-cluster": "cyan_stems", "pink-mushroom-cluster": "pink_caps",
            "pink-red-stalk-fungus": "stalk_fungus", "blue-leaf-rosette": "blue_rosette", "pale-spire-cluster": "pale_spires",
            "pale-tubular-cluster": "pale_tubes", "loose-boulder": "boulder", "loose-rock-cluster": "rock_cluster",
            "standing-bank-stone": "bank_stone", "foreground-rock-structure": "boulder", "waterfall": "waterfall", "mist": "mist",
            "river-water": "river_water"}.get(kind)

SCENE_PALETTE_OF = {"petrified_trunk": "petrified", "root_buttress": "petrified", "root_arch": "petrified", "moss_column": "petrified",
                    "red_branches": "red_plant", "cyan_stems": "cyan_stem", "pink_caps": "pink_cap", "stalk_fungus": "cream_stalk",
                    "blue_rosette": "blue_rosette", "pale_spires": "pale_spire", "pale_tubes": "pale_spire", "boulder": "boulder",
                    "rock_cluster": "boulder", "bank_stone": "boulder", "waterfall": "blackwater", "mist": "blackwater",
                    "river_water": "blackwater", "planter": None, "herb_pot": None}
SCENE_FULL = {"waterfall", "arch", "stall"}
# Morrowfast's owners all carry a uniform &y glyph; their material comes from the painted town instead.
TIMBER = {"A": (140, 104, 60, 255), "a": (104, 76, 44, 255), "H": (172, 132, 82, 255)}
ROPE = {"A": (164, 128, 82, 255), "a": (122, 92, 56, 255), "H": (196, 160, 108, 255)}
SCENE_MATERIAL = {"door": TIMBER, "door_open": TIMBER, "coil": ROPE, "barrel": TIMBER, "chest": TIMBER}

def scene_rows():
    """(blueprint, state, glyph, colour, background, [family]) for every placed scene owner of both painted places."""
    rows = []
    fell = json.load(open(os.path.join(ROOT, "Assets", "Resources", "SceneArt", "FellingSite", "definition.json")))
    for layer in fell["layers"]:
        fam = scene_family(layer["kind"], layer["id"])
        if fam: rows.append(("FellingSceneProp", layer["id"], "*" if layer["mutable"] else "#", "&y", "", ["scene:" + fam]))
    town = json.load(open(os.path.join(ROOT, "Assets", "Resources", "SceneArt", "Morrowfast", "definition.json")))
    for o in town["owners"]:
        if o["kind"] in ("npc", "creature", "building-shell", "roof"): continue
        if o["kind"] == "door":
            rows.append(("MorrowfastSceneProp", o["id"] + "|closed", "+", "&y", "", ["scene:door"]))
            rows.append(("MorrowfastSceneProp", o["id"] + "|open", "/", "&y", "", ["scene:door_open"]))
            continue
        fam = scene_family(o["kind"], o["id"])
        if fam: rows.append(("MorrowfastSceneProp", o["id"], "*", "&y", "", ["scene:" + fam]))
    return rows

# ════════════════ waysite props (Docs/CHUNK-ACTIVITY.md) ════════════════
T["bedroll"] = L(["..oooooooooooo", ".ocCooAAAAAAAAo", ".occCoAHAAAAAAo", ".ooooooaaaaaaao", "..oooooooooooo"])
T["trough"] = L(["oooooooooooooooo", "oHHHHHHHHHHHHHHo", "oAXxXXXxXXXxXXAo", "oAAAAAAAAAAAAAAo", "oaaaaaaaaaaaaaao", "oooooooooooooooo", ".oao........oao.", ".ooo........ooo."])
T["hitching_post"] = L([".....oo", "....oTto", "....oto.oo", "....oto.oSo", "....otoooSo", "....oto.oo", "....oto", "....oto", "...oTtto", "...ooooo"])
T["cart_over"] = L(["....ooo", "...oSsSo", "...osoSo", "...oSsSo", "....ooto", "oooooooooooooo", "oTTTTTTTTTTTTo", "otttttttttttto", "oTtoTtoTtoTtTo", "oooooooooooooo"])
T["hook_post"] = L(["......oo", ".....oSo", "....oSo", "....oo.oo", "......oto", "......oto", "......oto", "......oto", ".....oTto", ".....oooo"])

# Families drawn in several variants, chosen per cell so a run does not repeat (index "Variants").
VARIANT_FAMILIES = {"planks": ["planks", "planks_b", "planks_c"], "rail": ["rail", "rail_b"]}
TILE_EDGES = {"wall", "planks", "planks_b", "planks_c", "rail", "rail_b", "chalk_wall", "ice_wall"}

def darker(c, f):
    return (int(c[0] * f), int(c[1] * f), int(c[2] * f), 255)

def palette(fam, code):
    pal = items.palette_for(code)
    pal.update(LEAF); pal.update(FLAME)
    accent = items.CODES[code if code in items.CODES else "y"]
    pal["X"], pal["x"] = accent[0] + (255,), accent[2] + (255,)
    pal["Z"], pal["z"] = (200, 196, 180, 255), (160, 156, 142, 255)   # toned salt: never brighter than the player
    if fam in ("rail", "rail_b"): pal["S"], pal["s"] = (182, 178, 166, 255), (138, 138, 134, 255)
    if fam == "basket": pal["A"], pal["H"], pal["a"] = (182, 146, 92, 255), (204, 172, 116, 255), (140, 104, 60, 255)
    if fam == "bones": pal["A"], pal["a"], pal["H"] = (206, 198, 176, 255), (160, 150, 128, 255), (224, 218, 200, 255)
    if fam == "body": pal.update(items.WRAPS.get(code, items.WRAPS["y"]))
    if fam == "chalk_wall": pal["A"], pal["H"], pal["a"] = (184, 180, 164, 255), (200, 196, 180, 255), (160, 156, 142, 255)
    if fam in ("stone_chest", "slab", "graves", "trough") or fam.startswith("compass_"): pal.update(STONE)
    if fam in ("sign", "post"): pal.update(BOARD)
    if fam in ("shelf", "toppled_shelf"): pal.update(SPINES)
    if fam == "glass_case": pal.update(GLASS)
    if fam in SCENE_PALETTE_OF and SCENE_PALETTE_OF[fam]: pal.update(SCENE_PALETTES[SCENE_PALETTE_OF[fam]])
    if fam in ("planter", "herb_pot", "bowl"): pal.update(SCENE_PALETTES["thatch"]) if fam == "bowl" else None
    pal["C"], pal["c"] = (150, 128, 96, 255), (118, 98, 72, 255)          # canvas, worn canvas
    pal["d"] = darker(pal["A"], .62)
    if fam in ("planks","planks_b","planks_c"):
        # Gantry's Wood material: warmer/darker than the former bleached colour-code palette.
        pal.update({"A":(151,127,90,255),"B":(139,117,83,255),"H":(182,157,116,255),
                    "a":(100,83,56,255),"d":(75,64,47,255),"b":(121,99,68,255),
                    "s":(169,172,161,255),"g":(125,104,74,255)})
    return pal

def crop_stages(parts):
    """[(state, glyph, colour)] from the CropPart CSVs, exactly as CropPart.GlyphForStage reads them."""
    c = parts.get("Crop", {})
    glyphs = [g.strip() for g in c.get("StageGlyphsRaw", "").split(",")]
    colours = [g.strip() for g in c.get("StageColorsRaw", "").split(",")]
    out = []
    for i in range(min(len(glyphs), len(colours), 3)):
        if glyphs[i] and colours[i]: out.append(("stage%d" % i, glyphs[i][0], colours[i]))
    return out

MATURE_GLYPHS = {"T", "%", "#", '"'}

def main():
    skip = items.already_drawn()
    wild = open(os.path.join(ROOT, "Assets", "Scripts", "Presentation", "Rendering", "FlatPixelWildFaunaCatalog.cs")).read()
    skip |= set(re.findall(r'"([A-Z][A-Za-z0-9]+)"', wild))
    data = json.load(open(items.BLUEPRINTS))
    bp = {b["Name"]: b for b in data["Objects"]}
    rows = []  # (name, state, glyph, colour, background, [templates])
    for name in sorted(bp):
        if name in skip or name in ("TheRoot", "PhysicalObject"): continue
        tags, parts = items.merged(bp, name)
        if "Terrain" in tags or "Creature" in tags or items.is_item(bp, name) or "Physics" not in parts: continue
        r = items.render_of(bp, name)
        glyph, color, back = r.get("RenderString"), r.get("ColorString", ""), r.get("BackgroundColor", "")
        if not glyph or len(color) != 2 or color[0] != "&": continue
        fam = family(name, glyph)
        if "Crop" in parts:
            stages = crop_stages(parts)
            if len(stages) != 3 or stages[0][1] != glyph or stages[0][2] != color or stages[2][1] not in MATURE_GLYPHS: continue
            form = crop_form(name)
            for i, (state, g, c) in enumerate(stages):
                rows.append((name, state, g, c, back, ["crop%d:%s:%s" % (i, form, stages[2][1])]))
        elif "Door" in parts:
            if "VillageDoor" in [b["Name"] for b in items.chain(bp, name)]: fam = "field_gate"
            rows.append((name, "closed", "+", color, back, [fam]))
            rows.append((name, "open", "/", color, back, [fam + "_open"]))
        else:
            rows.append((name, "", glyph, color, back, VARIANT_FAMILIES.get(fam, [fam])))
    rows += scene_rows()
    frames = sum(len(r[5]) for r in rows)
    cols = 16
    sheet = Image.new("RGBA", (cols * W, ((frames + cols - 1) // cols) * H), (0, 0, 0, 0))
    index = []; k = 0
    for name, state, glyph, color, back, fams in rows:
        first = k
        for fam in fams:
            if fam.startswith("crop"):
                stage, form, mature = fam.split(":"); tpl = crop_frame(int(stage[4:]), form, mature); key = "crop"
            elif fam.startswith("scene:"):
                key = fam[6:]
                tpl = TALL[key] if key in TALL else stretch(T[key], 5, 0) if key in ("door", "door_open") else T[key] if key in SCENE_FULL else ["." * 16] * (H - 16) + T[key]
            else:
                tpl = frame_rows(fam); key = fam
            pal = palette(key, color[1])
            if fam.startswith("scene:") and key in SCENE_MATERIAL: pal.update(SCENE_MATERIAL[key])
            items.paint(sheet, tpl, pal, (k % cols) * W, (k // cols) * H, tile_edges=fam in TILE_EDGES)
            k += 1
        row = {"Blueprint": name, "State": state, "Frame": first, "Glyph": glyph, "Color": color, "Background": back,
               "Family": fams[0][6:] if fams[0].startswith("scene:") else "crop:" + fams[0].split(":")[1] if fams[0].startswith("crop") else fams[0]}
        if len(fams) > 1: row["Variants"] = len(fams)
        index.append(row)
    sheet.save(ATLAS)
    json.dump({"Columns": cols, "FrameHeight": H, "Icons": index}, open(INDEX, "w"), indent=1)
    print(len(index), "rows,", k, "frames for", len({r["Blueprint"] for r in index}), "props")

def update_gantry_only():
    """Re-export the three indexed timber frames without enumerating content or changing the index."""
    from pathlib import Path
    import hashlib
    out=Path(ROOT)/"ArtSource/Live16x24/gantry-timber"
    pins=json.loads((out/"protected-inputs.json").read_text())
    for rel,h in pins.items():assert hashlib.sha256((Path(ROOT)/rel).read_bytes()).hexdigest()==h,rel
    index=json.load(open(INDEX)); rows=[r for r in index["Icons"] if r["Blueprint"]=="GantryTimberWall"]
    assert len(rows)==1 and rows[0]["Frame"]==68 and rows[0]["Variants"]==3 and index["Columns"]==16
    sheet=Image.open(ATLAS).convert("RGBA");pal=palette("planks","y")
    frames=[]
    for v in range(3):
        tpl=planks(v);f=68+v
        items.paint(sheet,tpl,pal,(f%16)*W,(f//16)*H,tile_edges=True)
        frames.append({"id":"gantry-timber-"+str(v),"atlas_frame":f,"cells":[[["batten" if 9<=y<=12 else "board-edge" if c in "Ha" else "grain" if c=="g" else "board",c] for c in row] for y,row in enumerate(tpl)]})
    sheet.save(ATLAS)
    used=set(c[1] for f in frames for row in f["cells"] for c in row)
    (out/"source-cells.json").write_text(json.dumps({"palette":{k:bytes(pal[k][:3]).hex().upper() for k in sorted(used)},"frames":frames},indent=2)+"\n")
    print("Exported only GantryTimberWall frames68–70; index and other cells preserved")

if __name__ == "__main__":
    if "--gantry-only" in sys.argv:update_gantry_only()
    else:main()
