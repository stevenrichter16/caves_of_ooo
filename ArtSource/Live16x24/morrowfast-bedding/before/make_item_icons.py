"""Glyph -> pixel: item icons (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md, items).

Editable source. Each family has a hand-drawn 16x16 template in pixel rows. The letters
A (base), a (shade) and H (highlight) take the item's own colour code, mapped to a muted
palette that sits beside the project's sprites (not the raw terminal colours). Fixed
materials (wood, metal, glass, paper, cork, cloth) keep their own colours.

Run `python3 make_item_icons.py` here. It reads Objects.json and writes:
  Assets/Resources/Sprites/Live16x24/item-icons.png      (16 columns of 16x16 frames)
  Assets/Resources/Content/Data/ItemIcons.json          (blueprint -> frame, glyph, colour)
Original work; Caves of Qud item tiles were studied for silhouette economy only.
"""
import json, os, re
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
BLUEPRINTS = os.path.join(ROOT, "Assets", "Resources", "Content", "Blueprints", "Objects.json")
ATLAS = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Live16x24", "item-icons.png")
INDEX = os.path.join(ROOT, "Assets", "Resources", "Content", "Data", "ItemIcons.json")

# Muted art palette per colour code: (base, shade, highlight).
CODES = {
    "r": ((150, 52, 44), (104, 34, 30), (196, 92, 74)), "R": ((206, 84, 64), (150, 52, 44), (240, 140, 110)),
    "g": ((70, 120, 60), (46, 84, 42), (112, 160, 90)), "G": ((120, 176, 84), (78, 128, 56), (170, 214, 120)),
    "w": ((140, 104, 60), (98, 72, 42), (182, 146, 92)), "W": ((204, 186, 118), (156, 140, 84), (226, 214, 160)),
    "b": ((56, 76, 140), (38, 52, 100), (96, 120, 184)), "B": ((92, 128, 200), (60, 90, 150), (150, 182, 236)),
    "m": ((120, 60, 120), (84, 40, 84), (164, 100, 164)), "M": ((180, 96, 180), (130, 62, 130), (220, 150, 220)),
    "c": ((48, 120, 128), (32, 84, 90), (90, 164, 170)), "C": ((110, 190, 196), (70, 140, 148), (172, 228, 230)),
    "y": ((170, 170, 164), (120, 120, 116), (214, 214, 206)), "Y": ((206, 202, 188), (162, 158, 146), (224, 220, 206)),
    "K": ((84, 84, 90), (56, 56, 62), (126, 126, 132)), "k": ((44, 44, 48), (28, 28, 30), (80, 80, 86)),
    "o": ((170, 96, 40), (122, 66, 28), (214, 142, 72)), "O": ((220, 140, 60), (168, 98, 36), (246, 186, 110)),
}
FIXED = {
    ".": (0, 0, 0, 0), "o": (22, 27, 24, 255),
    "t": (120, 84, 50, 255), "T": (160, 118, 72, 255),          # wood, wood highlight
    "s": (150, 156, 164, 255), "S": (206, 212, 218, 255),        # metal, metal highlight
    "q": (196, 214, 214, 255),                                     # glass glint
    "p": (226, 216, 190, 255), "P": (176, 164, 136, 255),        # paper, paper shade
    "k": (150, 112, 70, 255),                                      # cork / cord
    "e": (40, 40, 44, 255),                                        # dark detail
    "W": (236, 204, 112, 255), "Y": (246, 232, 184, 255),        # flame, flame core
    "r": (150, 52, 44, 255),                                       # wax seal
}

T = {}
T["flask"] = [
    "................", "................", "................", "......okko......", "......okko......",
    ".....oqoooo.....", ".....oqAAAo.....", "....oqAAAAAo....", "...oqHAAAAAAo...", "...oqAAAAAaao...",
    "...oqAAAAAaao...", "...oqAAAAaaao...", "....oAAAaaao....", ".....ooooooo....", "................", "................"]
T["jar"] = [
    "................", "................", "................", "................", ".....oooooo.....",
    "....okkkkkko....", ".....oooooo.....", "....oqAAAAAo....", "....oqHAAAao....", "....oqAAAAao....",
    "....oqAAAaao....", "....oqAAaaao....", ".....oooooo.....", "................", "................", "................"]
T["pot"] = [
    "................", "................", "................", "................", "................",
    "................", "....oooooooo....", "...oHAAAAAAao...", "...oAAAAAAAao...", "...oAAAAAAaao...",
    "....oAAAAaao....", ".....oooooo.....", "................", "................", "................", "................"]
T["sac"] = [
    "................", "................", "................", ".......oo.......", "......oAAo......",
    ".....oHAAAo.....", "....oHAAAAAo....", "...oHAAAAAAAo...", "...oAAAAAAAao...", "...oAAAAAAaao...",
    "....oAAAAaao....", ".....oaaaao.....", "......oooo......", "................", "................", "................"]
T["roll"] = [
    "................", "................", "................", "................", "................",
    "....ooooooooo...", "...oHAAAAAAAAo..", "..oHAoAAAAAAAo..", "..oAoAoAAAAAAo..", "..oAAoAAAAAAao..",
    "...oaaaaaaaao...", "....ooooooooo...", "................", "................", "................", "................"]
T["book"] = [
    "................", "................", "................", "................", "...ooooooooo....",
    "...oHAAAAAAAo...", "...oAAAHHAAAoo..", "...oAAHAAHAAopo.", "...oAAAHHAAAopo.", "...oAAAAAAAAopo.",
    "...oAAAAAAAaopo.", "...oaaaaaaaaoo..", "....ooooooooo...", "................", "................", "................"]
T["paper"] = [
    "................", "................", "................", "................", "....ooooooo.....",
    "....oppppppo....", "....opPPPPpo....", "....oppppppo....", "....opPPPPpo....", "....oppppppo....",
    "....opPPPppo....", "....oAppppPo....", "....ooooooo.....", "................", "................", "................"]
T["codex"] = [
    "................", "................", "................", "................", "....oooooooo....",
    "....otttttTo....", "....otppppto....", "....otPPPPto....", "....otppppto....", "....otPPPpto....",
    "....otppppto....", "....oTttttto....", "....oooooooo....", "................", "................", "................"]
T["scroll"] = [
    "................", "................", "................", "................", "...oo.......oo..",
    "..oAAo.....oAAo.", "..oApoooooooApo.", "...opppppppppo..", "...opPPPPPPPPo..", "...opppppppppo..",
    "..oApoooooooApo.", "..oAAo.....oAAo.", "...oo.......oo..", "................", "................", "................"]
T["letter"] = [
    "................", "................", "................", "................", "................",
    "..oooooooooooo..", "..oPppppppppPo..", "..opPppppppPpo..", "..oppPppppPppo..", "..opppPAAPpppo..",
    "..oppppAApppppo.", "..oooooooooooo..", "................", "................", "................", "................"]
T["parcel"] = [
    "................", "................", "................", "................", "....ooooooooo...",
    "...oPpppkpppPo..", "...opppkpppppo..", "...okkkkkkkkko..", "...opppkpppppo..", "...opppkpppppo..",
    "...oPpppkpppPo..", "....ooooooooo...", "................", "................", "................", "................"]
T["seeds"] = [
    "................", "................", "................", "................", "................",
    "................", "......oo........", ".....oHAo..oo...", ".....oAao.oHAo..", "..oo..oo..oAao..",
    ".oHAo.oo...oo...", ".oAao.oHAo......", "..oo..oAao......", "......oo........", "................", "................"]
T["charm"] = [
    "................", "................", "....ok....ko....", "....ok....ko....", ".....ok..ko.....",
    "......okko......", ".......oo.......", "......oAAo......", ".....oHAAAo.....", ".....oAAAao.....",
    "......oAao......", ".......oo.......", "................", "................", "................", "................"]
T["stone"] = [
    "................", "................", "................", "................", "................",
    "................", ".....oooooo.....", "....oHHAAAAo....", "...oHAAAAAAao...", "...oAAAAAAaao...",
    "...oAAAAaaaao...", "....oaaaaaao....", ".....oooooo.....", "................", "................", "................"]
T["pile"] = [
    "................", "................", "................", "................", "................",
    "................", "................", "................", "......oooo......", "....ooHAAAoo....",
    "...oHAAAAAAAo...", "..oAAAAAAAAaao..", "..oaaaaaaaaaao..", "...ooooooooooo..", "................", "................"]
T["crystals"] = [
    "................", "................", "................", "................", "......o.........",
    ".....oHo...o....", ".....oHAo.oHo...", "..o..oHAo.oAo...", ".oHo.oAAo.oAao..", ".oAAooAAaooAao..",
    ".oAaoAAAaoAAao..", "..oooooooooooo..", "................", "................", "................", "................"]
T["fruit"] = [
    "................", "................", "................", "................", ".......oto......",
    "......otoo......", ".....oooHHoo....", "....oAAAHAAAo...", "...oAAAAAAAAao..", "...oAAAAAAAaao..",
    "...oAAAAAAaaao..", "....oAAAaaaao...", ".....ooooooo....", "................", "................", "................"]
T["bulb"] = [
    "................", "................", "................", ".....o.o.o......", "......ooo.......",
    "......oAo.......", ".....oHAAo......", "....oHAAAAo.....", "....oAAAAAo.....", "....oAAAAao.....",
    ".....oAAao......", "......oto.......", "......ooo.......", "................", "................", "................"]
T["meat"] = [
    "................", "................", "................", "................", "................",
    "....ooooo.......", "...oHAAAAoo.....", "..oAAAAAAAAoo...", "..oAAAaAAAAAAo..", "..oAAAAAAaAAoSo.",
    "...oAAAAAAAoSSo.", "....ooooooo.oo..", "................", "................", "................", "................"]
T["fish"] = [
    "................", "................", "................", "................", "................",
    "................", "...ooooooo...o..", "..oHAAAAAAo.oAo.", ".oeAAAAAAAAoAAo.", ".oAAAAAAAAAAAAo.",
    "..oaaaaaaao.oao.", "...ooooooo...o..", "................", "................", "................", "................"]
T["sprig"] = [
    "................", "................", "................", "..........oo....", ".........oHAo...",
    ".....oo.oAAo....", "....oHAooAao....", "....oAAoo.o.....", ".....ooo.o......", ".......oto......",
    "......oto.oo....", ".....oto.oHAo...", "....oto..oAao...", "....oo....oo....", "................", "................"]
T["twig"] = [
    "................", "................", "................", "................", "............oo..",
    "...........oAo..", "..........oto...", ".........oto....", "......o.oto.....", ".....oAoto......",
    "......oto.......", ".....oto........", "....oto.........", "....oo..........", "................", "................"]
T["dagger"] = [
    "................", "................", "...........oo...", "..........oSso..", ".........oSso...",
    "........oSso....", ".......oSso.....", "......oSso......", "....oooso.......", ".....oAoo.......",
    "....oAo.o.......", "...ooto.........", "...oto..........", "....o...........", "................", "................"]
T["sword"] = [
    "..............o.", ".............oSo", "............oSso", "...........oSso.", "..........oSso..",
    ".........oSso...", "........oSso....", ".......oSso.....", "......oSso......", "...o.oSso.......",
    "....oAso........", "...oAAo.........", "..ootoA.........", ".oto.o..........", ".oo.............", "................"]
T["greatsword"] = [
    ".............oo.", "............oSSo", "...........oSSso", "..........oSSso.", ".........oSSso..",
    "........oSSso...", ".......oSSso....", "......oSSso.....", "..o..oSSso......", "..oAoSSso.......",
    "...oAsso........", "..oAAAo.........", ".ootoAAo........", "oto.o.o.........", "oo..............", "................"]
T["spear"] = [
    "..............o.", ".............oSo", "............oSSo", "...........oSSo.", "..........oAoo..",
    ".........oto....", "........oto.....", ".......oto......", "......oto.......", ".....oto........",
    "....oto.........", "...oto..........", "..oto...........", ".oto............", ".oo.............", "................"]
T["axe"] = [
    "................", "........ooo.....", ".......oSSSo....", "......oSSSSso...", "......oSSsooto..",
    ".......ooo.oto..", "..........oto...", ".........oto....", "........oto.....", ".......oto......",
    "......oto.......", ".....oto........", "....oto.........", "....oo..........", "................", "................"]
T["hammer"] = [
    "................", "................", ".....ooooo......", "....oAHAAAo.....", "....oAAAAao.....",
    "....oAAAaao.....", ".....ooooooo....", ".......oto......", ".......oto......", ".......oto......",
    ".......oto......", ".......oto......", ".......oto......", ".......ooo......", "................", "................"]
T["head"] = [
    "................", "................", "................", "................", "......oooo......",
    ".....oSSSso.....", "....oSSSSsso....", "....oSSSssso....", ".....oSsso......", "......oAo.......",
    "......oAo.......", "......ooo.......", "................", "................", "................", "................"]
T["haft"] = [
    "................", "................", "..............o.", ".............oTo", "............oTto",
    "...........oTto.", "..........oTto..", ".........oTto...", "........oTto....", ".......oTto.....",
    "......oTto......", ".....oTto.......", "....oTto........", "....ooo.........", "................", "................"]
T["binding"] = [
    "................", "................", "................", "................", ".....oooooo.....",
    "....oHAAAAAo....", "...oAAooooAAo...", "...oAo....oAo...", "...oAo....oAo...", "...oAAooooAAo...",
    "....oAAAAAao....", ".....oooooo.....", "................", "................", "................", "................"]
T["spike"] = [
    "................", "................", "................", "................", "...........o....",
    "..........oSo...", ".........oSso...", "........oSso....", ".......oSso.....", "......oSso......",
    ".....oSso.......", "....oooo........", "....oo..........", "................", "................", "................"]
T["blade_part"] = [
    "................", "................", "................", "................", "................",
    "..............o.", ".............oS.", "...oooooooooooS.", "..oSSSSSSSSSSSso", "..oSSSSSSSSSsso.",
    "...ooooooooooo..", "................", "................", "................", "................", "................"]
T["helm"] = [
    "................", "................", "................", "................", ".....oooooo.....",
    "....oHAAAAAo....", "...oHAAAAAAAo...", "...oAAAAAAAao...", "...oAAoooooAo...", "...oAo.....oo...",
    "...oo...........", "................", "................", "................", "................", "................"]
T["boots"] = [
    "................", "................", "................", "....oooo..oooo..", "....oHAo..oHAo..",
    "....oAAo..oAAo..", "....oAAo..oAAo..", "....oAAo..oAAo..", "....oAAoo.oAAoo.", "....oAAAAooAAAAo",
    "....oaaaaooaaaao", "....ooooo.ooooo.", "................", "................", "................", "................"]
T["gloves"] = [
    "................", "................", "................", "................", "...o.o.o........",
    "..oAoAoAo.o.o.o.", "..oAoAoAooAoAoAo", "..oHAAAAooAAAAAo", "..oAAAAAooAAAAAo", "..oAAAAao.oAAAo.",
    "...oaaao...oaao.", "....ooo.....oo..", "................", "................", "................", "................"]
T["body"] = [
    "................", "................", "....oo....oo....", "...oHAoooooAo...", "..oHAAAAAAAAAo..",
    "..oAAoAAAAAoAo..", "..ooAAAAAAAAoo..", "....oAAAAAAAo...", "....oAAHAAAAo...", "....oAAAAAAao...",
    "....oAAAAAaao...", "....ooooooooo...", "................", "................", "................", "................"]
T["cloak"] = [
    "................", "................", ".....oooooo.....", "....oHAAAAAo....", "....oAoooooAo...",
    "...oAAAAAAAAo...", "...oAAAAAAAAo...", "..oHAAAAAAAAAo..", "..oAAAAAAAAAao..", "..oAAAAAAAAaao..",
    "..oAAAAAAAaaao..", "...ooooooooooo..", "................", "................", "................", "................"]
T["shield"] = [
    "................", "................", "................", ".....oooooo.....", "....oSSSSSso....",
    "...oSAAAAAAso...", "...oSAAHAAAso...", "...oSAAAAAAso...", "...oSAAAAAaso...", "....oSAAAaso....",
    ".....oSaaso.....", "......oSso......", ".......oo.......", "................", "................", "................"]
T["grenade"] = [
    "................", "................", "................", "........ok......", ".......okk......",
    "......ooso......", ".....oHAAAo.....", "....oHAAAAAo....", "....oAAoAAao....", "....oAAAoAao....",
    ".....oAAAao.....", "......oooo......", "................", "................", "................", "................"]
T["doll"] = [
    "................", "................", "......oooo......", ".....oHAAAo.....", ".....oAeAeo.....",
    "......oAAo......", "....ooAAAAoo....", "...oAoAAAAoAo...", "......oAAo......", ".....oAooAo.....",
    ".....oAo.oAo....", ".....oo...oo....", "................", "................", "................", "................"]
T["shard"] = [
    "................", "................", "........o.......", ".......oHo......", "......oHAo......",
    "......oHAAo.....", ".....oHAAAo.....", ".....oAAAao.....", "....oAAAaao.....", "....oAAaao......",
    "....oAaao.......", ".....ooo........", "................", "................", "................", "................"]
T["pipe"] = [
    "................", "................", "................", "................", "...........ooo..",
    "..........oAAAo.", ".........oAAAAo.", "........oAAAo...", ".......oAAo.....", "......oAAo......",
    ".....oAAo.......", "....oAAo........", "...ooo..........", "................", "................", "................"]

T["key"] = [
    "................", "................", "................", "................", "....oooo........",
    "...oSssSo.......", "...os..so.......", "...oSssSo.......", "....osso........", ".....oso........",
    ".....osoo.......", ".....ossso......", ".....oso........", ".....ossso......", "......ooo.......", "................"]
T["shroud"] = [
    "................", "................", "................", "................", "................",
    "................", "................", "..ooo...........", ".oHAAoooooooooo.", "oHAAAokAAkAAkAAo",
    "oAAAAokAAkAAkAao", ".oaaaokaakaakao.", "..ooooooooooooo.", "................", "................", "................"]
T["corpse"] = [
    "................", "................", "................", "................", "................",
    "................", "...........oo...", "..ooo......oAo..", ".oHAAooooooooAo.", "oAeAAAAAAAAAAAo.",
    "oAAAAAAAAAAAAAo.", ".oaAaaaaaaaaaao.", "..oo.oo..oo.oo..", "..oo.oo..oo.oo..", "................", "................"]
T["cup"] = [
    "................", "................", "................", "................", "................",
    "................", "....oooooooo....", "....oHAAAAAo....", "....oAAAAAAoo...", "....oAAAAAAoAo..",
    "....oAAAAAaoAo..", ".....oAAAao.o...", "......oooo......", "................", "................", "................"]
T["candle"] = [
    "................", "................", "................", ".......oo.......", "......oWWo......",
    "......oWYo......", ".......oo.......", "......oAAo......", "......oHAo......", "......oAAo......",
    "......oAAo......", ".....otAAto.....", ".....oooooo.....", "................", "................", "................"]

def L(rows, top=0):
    """Rows padded to 16 with transparency; `top` blank rows above, the rest below, 16 in all."""
    out = ["." * 16] * top + [r.ljust(16, ".") for r in rows]
    assert all(len(r) == 16 for r in out), rows
    return out + ["." * 16] * (16 - len(out))

T["paper_b"] = L(["...oooooooooooo", "...oPppppppppPo", "...opPppppppPpo", "...oppPppppPppo", "...opppPrrPpppo",
                  "...opppprrppppo", "...oppppppppppo", "...oPPPPPPPPPPo", "...oooooooooooo"], 4)
T["key_b"] = L(["...ooooo", "...oSSSo", "...oS.so", "...oSsso", "....oso", "....oso", "....osoo", "....osSSo",
                "....oso", "....osSo", "....ooo"], 3)
T["key_c"] = L(["....ooo", "...oSsSo", "...os.so", "...oSsSo", "....oso", "....oso", "....oso", "....osooo",
                "....osSSo", "....osooo", "....oso", "....ooo"], 2)
T["boots"] = L(["..oooo...oooo", "..oHAo...oHAo", "..oAko...oAko", "..oAAo...oAAo", "..oAAoo..oAAoo",
                "..oAAAAo.oAAAAo", "..oAAAAAooAAAAAo", "..oaaaaaooaaaaao", "..oooooooooooooo"], 4)
T["seeds"] = L([".....oo", "....okko", "....oppo", "...oPpppo", "..oPppppo", "..opppppo..o", "..oPpppppooAo.o",
                "...oooooo.oAooAo", "...........o..o"], 5)
T["corpse"] = L(["....aa......aa", "...aao.....aao", "...oao.....oao", "..oooaooooooaooo", ".oHAAAAAAAAAAAAo",
                 "oeAAAHHHHHHHAAAo", "oAAAAAAAAAAAAAo", ".oaaaaaaaaaaaoo", "..ooooooooooo"], 5)
T["corpse_bird"] = L(["......a.a", "......a.a", "...oooooooo", "..oHAAAAAAoo", "kkoeAAAAaaAAoo",
                      "..oAAAaaaaAAAAo", "...oooooooooAAo", "............ooo"], 6)
T["sheaf"] = L(["...oooooooo", "...oppppppoo", "..oopPPPPpoPo", "..opppkkkpppo", "..oPPPkPkPPPo", "..oppppkppppo",
                "..oPPPPPPPPpo", "...oooooooooo"], 5)
T["codex_b"] = L(["....oooooooo", "....otttrtTo", "....otpprpto", "....otPPrPto", "....otpppptoo", "....otPPPpto",
                  "....otppppto", "....oTttttto", "....oooooooo"], 4)
T["codex_c"] = L(["....oooooooo", "....otttttTo", "....otppppto", "....otPPPPsSo", "....otppppsso", "....otPPPpto",
                  "....otppppto", "....oTttttto", "....oooooooo"], 4)
T["shroud"] = L([".oooo", "oHAAAoooooooo", "oAAAAoHAkAAkAoo", "oAAAAaAAAkkAAAAo", ".oaao.oAkaakaaao", "..oo...oooooooo"], 8)

def family(name, glyph):
    n = name
    has = lambda *ks: any(k in n for k in ks)
    if has("Key") and glyph == "/": return "key"
    if has("Corpse"): return "corpse_bird" if has("jay", "Magpie", "Sari", "Bird") else "corpse"
    if glyph == "&" and has("Body"): return "shroud"
    if has("Cup"): return "cup"
    if has("Candle"): return "candle"
    if has("Branch"): return "twig"
    if has("Tuft", "Fan"): return "sprig"
    if has("Shell", "skin"): return "sac"
    if glyph == "+": return "book"
    if glyph == "g": return "grenade"
    if glyph == "=": return "letter"
    if glyph == "&": return "parcel"
    if glyph == "i": return "doll"
    if glyph == ".": return "pile"
    if has("Seed"): return "seeds"
    if has("Codex", "Register"): return "codex"
    if has("Schematic"): return "scroll"
    if glyph == "?": return "paper"
    if has("Hammer", "Maul"): return "hammer"
    if has("Buckler"): return "shield"
    if has("Helmet", "Cap", "Hood"): return "helm"
    if has("Boots"): return "boots"
    if has("Gloves"): return "gloves"
    if has("Cloak", "Wrap"): return "cloak"
    if has("Mail", "Plate", "Armor", "Apron", "Screen"): return "body"
    if has("Haft"): return "haft"
    if has("Binding", "Lashing"): return "binding"
    if has("Spike"): return "spike"
    if has("HeadComponent"): return "head" if not has("AxeHead") else "axe"
    if has("BladeComponent", "EdgeComponent", "LongBladeComponent"): return "blade_part"
    if has("Dagger", "Knife", "Stiletto"): return "dagger"
    if has("Greatsword", "Claymore"): return "greatsword"
    if has("Spear", "Glaive", "Lance"): return "spear"
    if has("Cleaver", "Axe"): return "axe"
    if has("Shard"): return "shard"
    if has("Pipe"): return "pipe"
    if has("Spine"): return "spike"
    if has("Sword", "Blade", "Edge", "Longsword"): return "sword"
    if has("Trinket"): return "charm"
    if has("Marble", "Stone"): return "stone"
    if has("Salt", "Sand"): return "crystals" if has("Glacier") else "pile"
    if has("Meat"): return "meat"
    if has("Perch"): return "fish"
    if has("Fruit", "Starapple", "Ember"): return "fruit"
    if has("Hearthbulb", "Root", "Bulb"): return "bulb"
    if has("Cutting"): return "twig"
    if has("Bladder", "Pod", "SporeSac", "Spore"): return "sac"
    if has("Pad", "Bandage", "Dressing", "Ribbon"): return "roll"
    if has("Wax", "Paste", "Gel", "Sap", "Pulp", "Moss", "Brine"): return "pot"
    if has("Oil"): return "jar"
    if glyph == ";": return "sprig"
    if glyph == "%": return "pile"
    if glyph == "!": return "flask"
    if glyph == "/": return "sword"
    if glyph == "]": return "blade_part"
    if glyph == "[": return "body"
    if glyph == "*": return "charm"
    return "pile"

ABSTRACT = re.compile(r"Item$")

def chain(bp, name):
    out = []
    b = bp.get(name)
    while b:
        out.append(b); b = bp.get(b.get("Inherits"))
    return out

def render_of(bp, name):
    r = {}
    for b in reversed(chain(bp, name)):
        for p in b.get("Parts", []):
            if p["Name"] == "Render":
                for q in p.get("Params", []): r[q["Key"]] = q["Value"]
    return r

def merged(bp, name):
    """The blueprint's parts and tags after inheritance."""
    tags, parts = {}, {}
    for b in reversed(chain(bp, name)):
        for t in b.get("Tags", []): tags[t["Key"]] = t.get("Value")
        for p in b.get("Parts", []):
            d = parts.setdefault(p["Name"], {})
            for q in p.get("Params", []): d[q["Key"]] = q["Value"]
    return tags, parts

def is_item(bp, name):
    """Derived from Item, or a takeable non-creature object (keys, corpses, cups, writs)."""
    names = [b["Name"] for b in chain(bp, name)]
    if "Item" in names[1:] or any(b.get("Inherits") == "Item" for b in chain(bp, name)): return True
    tags, parts = merged(bp, name)
    return "Creature" not in tags and str(parts.get("Physics", {}).get("Takeable", "false")).lower() == "true"

def palette_for(code):
    base, shade, hi = CODES.get(code, CODES["y"])
    pal = dict(FIXED)
    pal["A"], pal["a"], pal["H"] = base + (255,), shade + (255,), hi + (255,)
    return pal

def already_drawn():
    """Items some flat catalog already names keep their approved art: never indexed here."""
    import glob
    src = "".join(open(f).read() for f in glob.glob(os.path.join(ROOT, "Assets", "Scripts", "Presentation", "Rendering", "FlatPixel*.cs"))
                  if not f.endswith("FlatPixelWildFaunaCatalog.cs"))
    return set(re.findall(r'"([A-Z][A-Za-z0-9]+)"', src))

WARM_OUTLINE = (52, 46, 37, 255)   # the approved opening chest and barrel outline
FRAME_H, FLOOR_ROW = 24, 22         # 16x24 cells; small things rest on row 22, where the player stands
VARIANTS = {"codex": ["codex", "sheaf", "scroll", "codex_c"], "paper": ["paper", "paper_b", "scroll"], "letter": ["letter", "paper_b"], "key": ["key", "key_b", "key_c"]}

_SEEN = {}
def variant(fam, name):
    """Same-family items that would otherwise share one frame take variants in turn (names sorted), so a
    family of n items spreads evenly over its silhouettes. Stable while the blueprint list is."""
    options = VARIANTS.get(fam)
    if not options: return fam
    k = _SEEN.get(fam, 0); _SEEN[fam] = k + 1
    return options[k % len(options)]

def soften(px, w, h, x0, y0, tile_edges=False):
    """One outer outline in the warm approved brown; interior lines take the material's own dark.
    `px` maps (x, y) -> RGBA inside the frame at (x0, y0). With tile_edges, the frame's border is
    interior (walls continue into the next cell), so only transparency makes an edge."""
    black = FIXED["o"]
    out = dict(px)
    for (x, y), c in px.items():
        if c != black: continue
        edge = False; near = []
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if not (x0 <= nx < x0 + w and y0 <= ny < y0 + h):
                if not tile_edges: edge = True
                continue
            n = px.get((nx, ny))
            if n is None or n[3] == 0: edge = True
            elif n != black: near.append(n)
        if edge or not near: out[(x, y)] = WARM_OUTLINE
        else:
            r = sum(n[0] for n in near) / len(near); g = sum(n[1] for n in near) / len(near); b = sum(n[2] for n in near) / len(near)
            out[(x, y)] = (int(r * .55), int(g * .55), int(b * .55), 255)
    return out

def paint(sheet, rows, pal, fx, fy, w=16, h=FRAME_H, bottom=FLOOR_ROW, tile_edges=False):
    """Draw template rows into the frame at (fx, fy): bottom-aligned so the lowest drawn row lands on
    `bottom` (full-height templates fill the frame), then soften the outline."""
    drawn = [i for i, line in enumerate(rows) if any(ch != "." for ch in line)]
    if not drawn: return
    top = fy + (0 if len(rows) == h else bottom - drawn[-1])
    px = {}
    for y, line in enumerate(rows):
        assert len(line) == w, (len(line), line)
        for x, ch in enumerate(line):
            if ch != ".": px[(fx + x, top + y)] = pal[ch]
    for (x, y), c in soften(px, w, h, fx, fy, tile_edges).items():
        if fy <= y < fy + h: sheet.putpixel((x, y), c)

def main():
    skip = already_drawn()
    data = json.load(open(BLUEPRINTS))
    bp = {b["Name"]: b for b in data["Objects"]}
    rows = []
    for name in sorted(bp):
        if ABSTRACT.search(name) or not is_item(bp, name) or name in skip: continue
        r = render_of(bp, name)
        glyph, color = r.get("RenderString"), r.get("ColorString", "")
        if not glyph or len(color) != 2 or color[0] != "&": continue
        rows.append((name, glyph, color, variant(family(name, glyph), name)))
    cols = 16
    sheet = Image.new("RGBA", (cols * 16, ((len(rows) + cols - 1) // cols) * FRAME_H), (0, 0, 0, 0))
    index = []
    for i, (name, glyph, color, fam) in enumerate(rows):
        pal = palette_for(color[1])
        if fam == "shroud": pal.update(WRAPS.get(color[1], WRAPS["y"]))
        paint(sheet, T[fam], pal, (i % cols) * 16, (i // cols) * FRAME_H)
        index.append({"Blueprint": name, "Frame": i, "Glyph": glyph, "Color": color, "Family": fam})
    sheet.save(ATLAS)
    json.dump({"Columns": cols, "FrameHeight": FRAME_H, "Icons": index}, open(INDEX, "w"), indent=1)
    print(len(index), "icons")

# Wrapped bodies are cloth, never the glyph's bright colour: linen, peat, salt-grey, dark peat.
WRAPS = {"y": {"A": (176, 168, 148, 255), "a": (134, 126, 108, 255), "H": (200, 194, 176, 255)},
         "w": {"A": (122, 98, 70, 255), "a": (90, 72, 52, 255), "H": (150, 124, 92, 255)},
         "W": {"A": (182, 180, 170, 255), "a": (140, 138, 128, 255), "H": (204, 202, 192, 255)},
         "K": {"A": (122, 112, 96, 255), "a": (96, 88, 74, 255), "H": (140, 132, 116, 255)}}

if __name__ == "__main__":
    main()
