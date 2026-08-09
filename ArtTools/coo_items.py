#!/usr/bin/env python3
"""GRAPHICS PASS 15 round 5 — ground-item bodies.

A few NEAR-GRAY bodies cover dozens of blueprints: the renderer
claims them with the glyph's COLOR COPIED (not authoredColor), so a
FireTonic's red '!' becomes a red vial, GlowQuartz's cyan '*' a cyan
gem — one sprite per FAMILY, identity carried by tint (the same trick
weapon_ground shipped in Pass 14). Fixed-color bodies (torch, meat,
bone) opt out of tint via near-final palettes anyway.

Run:  python3 ArtTools/coo_items.py   (then the outline pass)
"""

from PIL import Image
import os

ROOT = os.path.join(os.path.dirname(__file__), "..",
                    "Assets", "Resources", "Sprites", "Environment")

# near-gray tint bases (multiply-tinted by glyph color at claim time)
G_DARK, G_MID, G_LIGHT = (96, 96, 100, 255), (150, 150, 156, 255), (200, 200, 206, 255)


def blank():
    return Image.new("RGBA", (16, 16), (0, 0, 0, 0))


def px_rows(px, rows, color):
    for y, xs in rows.items():
        for x in xs:
            if 0 <= x < 16 and 0 <= y < 16:
                px[x, y] = color


def save(im, name):
    im.save(os.path.join(ROOT, name + ".png"))


def vial():
    im = blank(); px = im.load()
    px_rows(px, {4: range(6, 10)}, G_DARK)                    # cork
    px_rows(px, {5: [7, 8]}, G_DARK)                          # neck
    px_rows(px, {6: range(6, 10), 7: range(5, 11), 8: range(5, 11),
                 9: range(5, 11), 10: range(5, 11), 11: range(6, 10)}, G_MID)
    px_rows(px, {8: range(6, 10), 9: range(6, 10), 10: range(6, 9)}, G_LIGHT)  # liquid
    px_rows(px, {7: [5], 8: [5], 9: [5]}, G_DARK)
    save(im, "item_vial")


def book():
    im = blank(); px = im.load()
    px_rows(px, {5: range(4, 12), 6: range(3, 13), 7: range(3, 13),
                 8: range(3, 13), 9: range(3, 13), 10: range(3, 13),
                 11: range(4, 12)}, G_MID)
    px_rows(px, {6: [3, 4], 7: [3], 8: [3], 9: [3], 10: [3, 4]}, G_DARK)  # spine
    px_rows(px, {6: range(6, 11), 7: range(6, 11)}, G_LIGHT)   # cover emblem
    px_rows(px, {8: range(7, 10)}, G_DARK)
    save(im, "item_book")


def gem():
    im = blank(); px = im.load()
    px_rows(px, {6: range(7, 9), 7: range(6, 10), 8: range(5, 11),
                 9: range(5, 11), 10: range(6, 10), 11: range(7, 9)}, G_MID)
    px_rows(px, {7: [6], 8: [5, 6], 9: [5]}, G_DARK)
    px_rows(px, {6: [7], 7: [7, 8], 8: [7]}, G_LIGHT)          # facet glint
    save(im, "item_gem")


def key():
    im = blank(); px = im.load()
    px_rows(px, {5: range(5, 9), 6: [5, 8], 7: range(5, 9)}, G_MID)   # bow
    px_rows(px, {8: [6, 7], 9: [6, 7], 10: [6, 7], 11: [6, 7]}, G_MID)
    px_rows(px, {10: [8, 9], 11: [8]}, G_MID)                  # teeth
    px_rows(px, {5: range(5, 7)}, G_LIGHT)
    save(im, "item_key")


def torch():
    im = blank(); px = im.load()
    wood_d, wood = (86, 62, 40, 255), (122, 92, 58, 255)
    fl_d, fl, fl_l = (176, 92, 32, 255), (222, 148, 48, 255), (244, 208, 110, 255)
    px_rows(px, {8: [7, 8], 9: [7, 8], 10: [7, 8], 11: [7, 8], 12: [7, 8]}, wood)
    px_rows(px, {9: [7], 11: [7]}, wood_d)
    px_rows(px, {6: range(6, 10), 7: range(6, 10)}, fl)
    px_rows(px, {4: [7, 8], 5: range(6, 10)}, fl_d)
    px_rows(px, {5: [7, 8], 6: [7, 8]}, fl_l)
    save(im, "item_torch")


def meat():
    im = blank(); px = im.load()
    m_d, m, m_l = (110, 48, 42, 255), (158, 70, 58, 255), (196, 110, 92, 255)
    bone = (222, 214, 196, 255)
    px_rows(px, {6: range(5, 11), 7: range(4, 12), 8: range(4, 12),
                 9: range(4, 12), 10: range(5, 11)}, m)
    px_rows(px, {7: [4, 5], 8: [4], 9: [4, 5]}, m_d)
    px_rows(px, {6: range(6, 9), 7: range(6, 9)}, m_l)
    px_rows(px, {10: [10], 11: [10, 11], 12: [11]}, bone)      # bone stub
    save(im, "item_meat")


def fruit():
    im = blank(); px = im.load()
    px_rows(px, {6: range(6, 10), 7: range(5, 11), 8: range(5, 11),
                 9: range(5, 11), 10: range(6, 10)}, G_MID)
    px_rows(px, {7: [5], 8: [5], 9: [5, 6]}, G_DARK)
    px_rows(px, {6: [6, 7], 7: [6]}, G_LIGHT)
    px_rows(px, {5: [8], 4: [8, 9]}, (86, 110, 74, 255))       # leaf/stem (fixed green)
    save(im, "item_fruit")


def seed_pouch():
    im = blank(); px = im.load()
    px_rows(px, {7: range(6, 10), 8: range(5, 11), 9: range(5, 11),
                 10: range(5, 11), 11: range(6, 10)}, G_MID)
    px_rows(px, {8: [5], 9: [5], 10: [5, 6]}, G_DARK)
    px_rows(px, {6: [7, 8], 5: [7]}, G_DARK)                   # tied neck
    px_rows(px, {8: [7, 9], 9: [6, 8]}, G_LIGHT)               # seeds peeking
    save(im, "item_seed")


def armor_piece():
    im = blank(); px = im.load()
    px_rows(px, {5: [5, 6, 9, 10], 6: range(5, 11), 7: range(4, 12),
                 8: range(4, 12), 9: range(5, 11), 10: range(5, 11),
                 11: range(6, 10)}, G_MID)
    px_rows(px, {7: [4], 8: [4, 5], 9: [5]}, G_DARK)
    px_rows(px, {6: range(6, 8), 7: [6]}, G_LIGHT)
    px_rows(px, {8: [7, 8], 9: [7, 8]}, G_DARK)                # chest seam
    save(im, "item_armor")


def bone_item():
    im = blank(); px = im.load()
    b_d, b, b_l = (168, 160, 142, 255), (214, 206, 188, 255), (238, 232, 218, 255)
    px_rows(px, {6: [4, 5], 7: [4, 5, 6]}, b)                  # knob
    px_rows(px, {8: [6, 7], 9: [7, 8], 10: [8, 9]}, b)         # shaft diagonal
    px_rows(px, {11: [9, 10, 11], 12: [10, 11]}, b)            # knob
    px_rows(px, {7: [4], 9: [7], 11: [9]}, b_d)
    px_rows(px, {6: [5], 8: [7]}, b_l)
    save(im, "item_bone")


def vein():
    im = blank(); px = im.load()
    r_d, r, r_l = (70, 70, 76, 255), (104, 104, 112, 255), (134, 134, 142, 255)
    px_rows(px, {4: range(4, 12), 5: range(3, 13), 6: range(2, 14),
                 7: range(2, 14), 8: range(2, 14), 9: range(2, 14),
                 10: range(3, 13), 11: range(4, 12)}, r)        # rock mass
    px_rows(px, {5: [3, 4], 6: [2, 3], 7: [2], 8: [2, 3], 9: range(2, 5)}, r_d)
    px_rows(px, {4: range(5, 8), 5: range(5, 7)}, r_l)
    # crystal flecks: NEAR-GRAY LIGHT so the glyph tint colors them
    px_rows(px, {6: [6, 7], 7: [9, 10], 8: [5, 6], 9: [8, 9],
                 7: [9, 10], 10: [6, 7]}, G_LIGHT)
    px_rows(px, {6: [8], 8: [7], 9: [10]}, (255, 255, 255, 255))
    save(im, "item_vein")


def grenade():
    """Round bomb + fuse, near-gray body — the gas color rides the
    glyph tint (poison green, sleep blue, stun gold)."""
    im = blank(); px = im.load()
    px_rows(px, {7: range(6, 10), 8: range(5, 11), 9: range(5, 11),
                 10: range(5, 11), 11: range(6, 10)}, G_MID)
    px_rows(px, {8: [5], 9: [5], 10: [5, 6]}, G_DARK)
    px_rows(px, {7: [6, 7]}, G_LIGHT)
    px_rows(px, {6: [7, 8]}, G_DARK)                           # cap
    px_rows(px, {5: [9], 4: [10], 3: [10]}, (122, 92, 58, 255))  # fuse
    px[3, 2] = (244, 208, 110, 255)                            # spark
    save(im, "item_grenade")


def scroll():
    im = blank(); px = im.load()
    px_rows(px, {5: range(5, 11), 6: range(4, 12), 7: range(4, 12),
                 8: range(4, 12), 9: range(4, 12), 10: range(5, 11)}, G_LIGHT)
    px_rows(px, {5: [4, 5], 10: [10, 11]}, G_MID)              # rolled ends
    px_rows(px, {7: range(6, 10), 8: range(6, 9)}, G_DARK)     # script lines
    save(im, "item_scroll")


# ── Round 5 interactable fixtures (authored colors) ──────────────

def berry_bush():
    im = blank(); px = im.load()
    dark, mid, light = (44, 66, 42, 255), (64, 92, 58, 255), (84, 114, 74, 255)
    berry = (176, 60, 66, 255)
    px_rows(px, {4: range(5, 11), 5: range(4, 12), 6: range(3, 13),
                 7: range(3, 13), 8: range(3, 13), 9: range(4, 12),
                 10: range(4, 12), 11: range(5, 11)}, dark)
    px_rows(px, {5: range(5, 9), 6: range(4, 9), 7: range(5, 8)}, mid)
    px_rows(px, {5: [5, 6]}, light)
    for x, y in [(5, 8), (9, 6), (7, 10), (11, 8), (4, 6), (8, 8)]:
        px[x, y] = berry
    px_rows(px, {12: [7, 8]}, (70, 62, 48, 255))
    save(im, "berry_bush")


def beehive():
    im = blank(); px = im.load()
    h_d, h, h_l = (140, 104, 44, 255), (178, 138, 58, 255), (208, 172, 92, 255)
    px_rows(px, {2: range(6, 10)}, (70, 62, 48, 255))          # branch stub
    px_rows(px, {3: range(6, 10), 4: range(5, 11), 5: range(4, 12),
                 6: range(4, 12), 7: range(4, 12), 8: range(5, 11),
                 9: range(6, 10)}, h)
    px_rows(px, {4: range(5, 11)}, h_l)
    px_rows(px, {6: range(4, 12)}, h_d)                        # band lines
    px_rows(px, {8: range(5, 11)}, h_d)
    px_rows(px, {7: [7, 8]}, (40, 30, 20, 255))                # entrance
    px[3, 11] = (40, 36, 20, 255); px[10, 10] = (40, 36, 20, 255)  # bees
    save(im, "beehive")


def hollow_stump():
    im = blank(); px = im.load()
    w_d, w, w_l = (86, 62, 40, 255), (122, 92, 58, 255), (152, 120, 78, 255)
    px_rows(px, {6: range(4, 12), 7: range(3, 13), 8: range(3, 13),
                 9: range(3, 13), 10: range(3, 13), 11: range(4, 12)}, w)
    px_rows(px, {7: [3, 4], 8: [3], 9: [3], 10: [3, 4]}, w_d)
    px_rows(px, {6: range(5, 11)}, w_l)                        # rim
    px_rows(px, {7: range(6, 10), 8: range(6, 10), 9: range(6, 10)}, (40, 30, 20, 255))  # hollow
    px_rows(px, {12: [4, 5, 10, 11]}, w_d)                     # roots
    px[7, 8] = (208, 172, 92, 255)                             # glint of loot
    save(im, "hollow_stump")


def mushroom_ring():
    im = blank(); px = im.load()
    c_d, c, c_l = (140, 104, 44, 255), (196, 166, 80, 255), (224, 202, 130, 255)
    stem = (214, 206, 188, 255)
    for cx, cy in [(4, 5), (10, 4), (13, 8), (9, 11), (3, 10), (7, 7)]:
        px_rows(px, {cy: range(cx - 1, cx + 2)}, c)
        px[cx, cy - 1] = c_l
        px[cx, cy + 1] = stem
    save(im, "mushroom_ring")


def signpost():
    im = blank(); px = im.load()
    w_d, w, w_l = (86, 62, 40, 255), (122, 92, 58, 255), (152, 120, 78, 255)
    px_rows(px, {4: range(3, 13), 5: range(3, 13), 6: range(3, 13),
                 7: range(3, 13)}, w)                          # board
    px_rows(px, {4: range(3, 13)}, w_l)
    px_rows(px, {7: range(3, 13)}, w_d)
    px_rows(px, {5: range(5, 11), 6: range(5, 9)}, w_d)        # carved text
    px_rows(px, {8: [7, 8], 9: [7, 8], 10: [7, 8], 11: [7, 8], 12: [7, 8]}, w)
    px_rows(px, {9: [7], 11: [7]}, w_d)
    save(im, "signpost")


# ── Round 7 (loot overhaul SM4) container family ─────────────────

def _lid_box(body, dark, light, lid=None):
    """Shared chest-ish body: slab + lid band + shadow."""
    im = blank(); px = im.load()
    px_rows(px, {5: range(3, 13), 6: range(2, 14), 7: range(2, 14),
                 8: range(2, 14), 9: range(2, 14), 10: range(2, 14),
                 11: range(3, 13)}, body)
    px_rows(px, {6: [2, 3], 7: [2], 8: [2], 9: [2], 10: [2, 3], 11: range(3, 6)}, dark)
    px_rows(px, {5: range(4, 10)}, light)
    px_rows(px, {7: range(2, 14)}, lid or dark)          # lid seam
    return im, px


def crate():
    body, dark, light = (150, 116, 62, 255), (104, 78, 40, 255), (184, 150, 92, 255)
    im, px = _lid_box(body, dark, light)
    px_rows(px, {8: [7, 8], 9: [7, 8]}, dark)            # plank cross
    px_rows(px, {6: range(6, 10), 10: range(6, 10)}, dark)
    save(im, "cont_crate")


def sack():
    body, dark, light = (168, 152, 116, 255), (118, 104, 78, 255), (198, 184, 150, 255)
    im = blank(); px = im.load()
    px_rows(px, {6: [7, 8], 7: range(6, 10), 8: range(5, 11), 9: range(4, 12),
                 10: range(4, 12), 11: range(4, 12), 12: range(5, 11)}, body)
    px_rows(px, {9: [4, 5], 10: [4], 11: [4, 5], 12: range(5, 8)}, dark)
    px_rows(px, {8: range(6, 9), 9: range(6, 9)}, light)
    px_rows(px, {5: [7, 8]}, dark)                        # tied neck
    save(im, "cont_sack")


def urn():
    body, dark, light = (176, 128, 84, 255), (124, 88, 56, 255), (206, 166, 118, 255)
    im = blank(); px = im.load()
    px_rows(px, {4: range(6, 10), 5: range(5, 11)}, dark)  # rim
    px_rows(px, {6: range(5, 11), 7: range(4, 12), 8: range(4, 12),
                 9: range(4, 12), 10: range(5, 11), 11: range(6, 10)}, body)
    px_rows(px, {7: [4, 5], 8: [4], 9: [4, 5], 10: [5]}, dark)
    px_rows(px, {6: range(6, 9), 7: range(6, 8)}, light)
    px_rows(px, {8: range(6, 10)}, dark)                   # painted band
    save(im, "cont_urn")


def strongbox():
    body, dark, light = (128, 132, 140, 255), (84, 88, 96, 255), (170, 176, 186, 255)
    im, px = _lid_box(body, dark, light)
    px_rows(px, {8: [7, 8], 9: [7, 8]}, (224, 196, 96, 255))   # brass lock
    px_rows(px, {6: [4, 11], 10: [4, 11]}, dark)               # corner bands
    save(im, "cont_strongbox")


def ore_cache():
    rock, dark, light = (112, 108, 104, 255), (74, 72, 70, 255), (146, 142, 138, 255)
    im = blank(); px = im.load()
    px_rows(px, {6: range(5, 11), 7: range(3, 13), 8: range(3, 13),
                 9: range(3, 13), 10: range(3, 13), 11: range(4, 12)}, rock)
    px_rows(px, {7: [3, 4], 8: [3], 9: [3], 10: range(3, 6)}, dark)
    px_rows(px, {6: range(6, 9)}, light)
    for x, y in [(6, 8), (9, 7), (7, 10), (11, 9)]:
        px[x, y] = (150, 220, 235, 255)                    # crystal glints
    save(im, "cont_ore_cache")


def bone_cache():
    b, bd, bl = (206, 198, 180, 255), (160, 152, 136, 255), (232, 226, 212, 255)
    im = blank(); px = im.load()
    px_rows(px, {9: range(3, 13), 10: range(3, 13), 11: range(4, 12)}, bd)
    px_rows(px, {7: range(5, 11), 8: range(4, 12)}, b)
    px_rows(px, {7: range(6, 9)}, bl)
    px_rows(px, {6: [5, 6, 10, 11]}, b)                    # rib tips
    px_rows(px, {5: [5, 11]}, bd)
    save(im, "cont_bone_cache")


def woven_basket():
    body, dark, light = (152, 132, 78, 255), (108, 92, 52, 255), (186, 168, 112, 255)
    im = blank(); px = im.load()
    px_rows(px, {6: range(4, 12), 7: range(3, 13), 8: range(3, 13),
                 9: range(3, 13), 10: range(4, 12), 11: range(5, 11)}, body)
    px_rows(px, {7: [3, 4], 8: [3], 9: [3, 4], 10: [4, 5]}, dark)
    px_rows(px, {6: range(5, 9)}, light)
    px_rows(px, {8: range(3, 13), 10: range(4, 12)}, dark)  # weave rows
    save(im, "cont_woven_basket")


def hollow_log():
    w, wd, wl = (128, 96, 60, 255), (88, 66, 40, 255), (158, 126, 84, 255)
    im = blank(); px = im.load()
    px_rows(px, {7: range(2, 14), 8: range(2, 14), 9: range(2, 14),
                 10: range(2, 14)}, w)
    px_rows(px, {10: range(2, 14)}, wd)
    px_rows(px, {7: range(3, 13)}, wl)
    px_rows(px, {8: range(3, 7), 9: range(3, 7)}, (40, 30, 20, 255))  # hollow end
    px_rows(px, {8: [10, 11], 9: [10, 11]}, wd)             # bark knot
    save(im, "cont_hollow_log")


def reliquary():
    body, dark, light = (128, 96, 152, 255), (86, 62, 104, 255), (168, 138, 190, 255)
    im, px = _lid_box(body, dark, light)
    px_rows(px, {8: [7, 8], 9: [7, 8]}, (232, 214, 250, 255))  # ward sigil
    px_rows(px, {6: [5, 10], 10: [5, 10]}, light)
    save(im, "cont_reliquary")


def bookshelf():
    w, wd = (122, 92, 58, 255), (86, 62, 40, 255)
    im = blank(); px = im.load()
    px_rows(px, {4: range(3, 13), 5: range(3, 13), 6: range(3, 13),
                 7: range(3, 13), 8: range(3, 13), 9: range(3, 13),
                 10: range(3, 13), 11: range(3, 13)}, wd)
    for row, cols in {5: [(4, (176, 72, 60)), (6, (72, 108, 168)), (8, (188, 160, 80))],
                      8: [(4, (96, 148, 96)), (7, (150, 92, 168)), (10, (176, 72, 60))]}.items():
        for x, c in cols:
            px_rows(px, {row: [x, x + 1], row + 1: [x, x + 1]}, c + (255,))
    px_rows(px, {7: range(3, 13), 10: range(3, 13)}, w)     # shelf boards
    save(im, "cont_bookshelf")


def weapon_rack():
    w, wd = (122, 92, 58, 255), (86, 62, 40, 255)
    steel, steel_l = (140, 146, 156, 255), (192, 198, 208, 255)
    im = blank(); px = im.load()
    px_rows(px, {11: range(2, 14), 12: range(2, 14)}, wd)   # base
    px_rows(px, {3: range(2, 14)}, w)                        # top bar
    for x in (4, 7, 10):                                     # hung blades
        px_rows(px, {4: [x], 5: [x], 6: [x], 7: [x], 8: [x], 9: [x]}, steel)
        px[x, 4] = steel_l
        px_rows(px, {10: [x - 1, x, x + 1]}, wd)             # crossguard
    save(im, "cont_weapon_rack")


def alchemy_shelf():
    w, wd = (100, 116, 88, 255), (70, 84, 62, 255)
    im = blank(); px = im.load()
    px_rows(px, {4: range(3, 13), 5: range(3, 13), 6: range(3, 13),
                 7: range(3, 13), 8: range(3, 13), 9: range(3, 13),
                 10: range(3, 13), 11: range(3, 13)}, wd)
    px_rows(px, {7: range(3, 13), 10: range(3, 13)}, w)      # boards
    for x, c in [(4, (176, 72, 60)), (7, (96, 180, 200)), (10, (188, 160, 80))]:
        px_rows(px, {5: [x], 6: [x, x + 1]}, c + (255,))     # vials
    for x, c in [(5, (150, 200, 120)), (9, (170, 110, 190))]:
        px_rows(px, {8: [x], 9: [x, x + 1]}, c + (255,))
    save(im, "cont_alchemy_shelf")


def main():
    for f in (vial, book, gem, key, torch, meat, fruit, seed_pouch,
              armor_piece, bone_item, vein, scroll, grenade,
              berry_bush, beehive, hollow_stump, mushroom_ring, signpost,
              crate, sack, urn, strongbox, ore_cache, bone_cache,
              woven_basket, hollow_log, reliquary, bookshelf,
              weapon_rack, alchemy_shelf):
        f()
    print("items+fixtures+containers: 30 bodies generated")


if __name__ == "__main__":
    main()
