#!/usr/bin/env python3
"""GRAPHICS PASS 15 round 5 — the bestiary.

44 hostile/wild creature sprites, 16x16, built from ~12 body
ARCHETYPES so families share silhouettes while palettes carry each
creature's ASCII color identity (&G jungle ape stays green, &C pale
stalker stays cyan...). Style laws hold: flat fills, 3-tone value
shading, 1px ink ring (the shared outline pass), accent eyes.

Run:  python3 ArtTools/coo_bestiary.py
Then: python3 ArtTools/coo_outline_pass.py   (adds the ink rings)
"""

from PIL import Image
import os

ROOT = os.path.join(os.path.dirname(__file__), "..",
                    "Assets", "Resources", "Sprites", "Environment")
INK = (30, 32, 28, 255)

# Qud-ish console colors -> sprite base RGB
COL = {
    'g': (86, 110, 74),   'G': (96, 160, 88),
    'y': (150, 120, 78),  'Y': (196, 166, 80),
    'w': (128, 128, 132), 'W': (200, 200, 204),
    'r': (140, 60, 50),   'R': (190, 70, 56),
    'K': (78, 78, 86),    'C': (110, 180, 190),
    'c': (90, 130, 140),  'M': (170, 90, 170),
    'm': (110, 60, 110),  'B': (90, 110, 180),
}


def tones(base):
    d = tuple(max(0, int(c * 0.60)) for c in base)
    l = tuple(min(255, int(c * 1.35)) for c in base)
    return d + (255,), base + (255,), l + (255,)


def blank():
    return Image.new("RGBA", (16, 16), (0, 0, 0, 0))


def px_rows(px, rows, color):
    for y, xs in rows.items():
        for x in xs:
            if 0 <= x < 16 and 0 <= y < 16:
                px[x, y] = color


# ── archetypes ───────────────────────────────────────────────────

def quadruped(base, eye, bulk=0, ears=True, tail=True):
    """Side-profile beast: bears, hounds, cats, dogs."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {7: range(4, 12 + bulk), 8: range(3, 13 + bulk),
                 9: range(3, 13 + bulk), 10: range(3, 13 + bulk),
                 11: range(4, 12 + bulk)}, mid)
    px_rows(px, {5: range(10, 15), 6: range(10, 15),
                 7: range(11, 15), 8: range(11, 14)}, mid)          # head right
    px_rows(px, {7: range(4, 8), 8: range(3, 7)}, dark)              # haunch shade
    px_rows(px, {5: range(10, 13)}, light)                           # head light
    px_rows(px, {12: [4, 5, 9, 10], 13: [4, 5, 9, 10]}, dark)        # legs
    if ears:
        px_rows(px, {4: [11, 13]}, dark)
    if tail:
        px_rows(px, {6: [2, 3], 7: [2]}, dark)
    px[13, 6] = eye
    return im


def biped_brute(base, eye, crest=None, broad=0):
    """Front-facing heavy: apes, golems, trolls, hulks, husks."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {2: range(6, 10), 3: range(5, 11), 4: range(5, 11)}, mid)   # head
    px_rows(px, {5: range(4 - broad, 12 + broad), 6: range(3 - broad, 13 + broad),
                 7: range(3 - broad, 13 + broad), 8: range(3 - broad, 13 + broad),
                 9: range(4 - broad, 12 + broad), 10: range(4 - broad, 12 + broad)}, mid)
    px_rows(px, {6: [3 - broad, 4 - broad], 7: [3 - broad], 8: [3 - broad, 4 - broad],
                 9: range(4 - broad, 7)}, dark)                     # left shade
    px_rows(px, {5: range(6, 10), 2: range(6, 8)}, light)            # chest/brow light
    px_rows(px, {6: [2 - broad, 13 + broad], 7: [2 - broad, 13 + broad],
                 8: [2 - broad, 13 + broad]}, dark)                  # arms
    px_rows(px, {11: [5, 6, 9, 10], 12: [5, 6, 9, 10], 13: [5, 6, 9, 10]}, dark)
    px[6, 3] = eye; px[9, 3] = eye
    if crest:
        px_rows(px, {1: range(7, 9), 0: [7]}, crest)
    return im


def humanoid_rogue(base, eye, hood=True):
    """Cloaked hostile humanoid: bandits, cultists, drifters."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {3: range(6, 10), 4: range(5, 11), 5: range(5, 11)}, dark if hood else mid)
    px_rows(px, {6: range(5, 11), 7: range(4, 12), 8: range(4, 12),
                 9: range(4, 12), 10: range(5, 11), 11: range(5, 11),
                 12: range(5, 11)}, mid)
    px_rows(px, {7: [4], 8: [4], 9: [4], 10: [5], 11: [5]}, dark)     # cloak shade
    px_rows(px, {6: range(6, 8)}, light)
    px_rows(px, {13: [6, 9]}, dark)                                   # feet
    px[6, 4] = eye; px[9, 4] = eye
    return im


def serpent(base, eye, big=False):
    """S-coil: viper, sand wurm."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    if not big:
        path = {4: range(9, 13), 5: range(8, 14), 6: range(7, 10),
                7: range(5, 9), 8: range(4, 8), 9: range(4, 8),
                10: range(5, 10), 11: range(7, 12), 12: range(9, 13),
                13: range(10, 13)}
        px_rows(px, path, mid)
        px_rows(px, {y: list(xs)[:1] for y, xs in path.items()}, dark)
        px_rows(px, {3: range(9, 13), 4: range(8, 14)}, mid)          # head
        px_rows(px, {3: range(9, 11)}, light)
        px[12, 3] = eye
        return im
    # BIG: a wurm ERUPTING — thick vertical body arcing over, maw up.
    px_rows(px, {3: range(6, 11), 4: range(5, 12), 5: range(5, 12),
                 6: range(5, 11), 7: range(5, 10), 8: range(5, 10),
                 9: range(5, 10), 10: range(4, 10), 11: range(4, 11),
                 12: range(3, 12), 13: range(3, 13)}, mid)
    px_rows(px, {6: [5], 7: [5], 8: [5], 9: [5], 10: [4, 5],
                 11: [4], 12: [3, 4], 13: range(3, 6)}, dark)          # left shade
    px_rows(px, {3: range(7, 10), 4: range(6, 9)}, light)              # head light
    px_rows(px, {2: [6, 8, 10]}, dark)                                 # maw teeth
    px_rows(px, {5: [7, 8], 8: [6, 7], 11: [7, 8]}, dark)              # segment rings
    px[6, 4] = eye; px[10, 4] = eye
    return im


def arachnid(base, eye, big=False, claws=False):
    """Dome + splayed legs: spiders, scorpions."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    body = {6: range(6, 10), 7: range(5, 11), 8: range(5, 11), 9: range(6, 10)}
    if big:
        body = {5: range(6, 10), 6: range(5, 11), 7: range(4, 12),
                8: range(4, 12), 9: range(5, 11), 10: range(6, 10)}
    px_rows(px, body, mid)
    px_rows(px, {min(body): list(body[min(body)])[:2]}, light)
    legs = {7: [2, 3, 12, 13], 8: [2, 13], 9: [3, 4, 11, 12],
            10: [2, 3, 12, 13], 11: [2, 13]}
    px_rows(px, legs, dark)
    if claws:
        px_rows(px, {5: [2, 3, 12, 13], 6: [2, 13], 4: [2, 13]}, dark)
        px_rows(px, {3: [1, 14], 12: [7, 8], 13: [8]}, dark)          # tail sting
    px[6, 7] = eye; px[9, 7] = eye
    return im


def flyer(base, eye, wingspan=5):
    """Spread wings, small body: bats, birds."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {6: range(7 - wingspan, 7), 7: range(8 - wingspan, 7),
                 5: range(7 - wingspan + 1, 6)}, dark)                # left wing
    px_rows(px, {6: range(9, 9 + wingspan), 7: range(9, 8 + wingspan),
                 5: range(10, 9 + wingspan)}, dark)                   # right wing
    px_rows(px, {5: range(7, 9), 6: range(7, 9), 7: range(7, 9),
                 8: range(7, 9), 9: [7, 8]}, mid)                     # body
    px_rows(px, {5: [7]}, light)
    px[7, 4] = eye
    return im


def blob(base, eye, maw=None):
    """Ooze dome + drips: slimes, rotlings, glowmaw."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {7: range(5, 11), 8: range(4, 12), 9: range(3, 13),
                 10: range(3, 13), 11: range(3, 13), 12: range(4, 12)}, mid)
    px_rows(px, {9: [3, 4], 10: [3], 11: [3, 4], 12: range(4, 7)}, dark)
    px_rows(px, {7: range(6, 9), 8: range(5, 8)}, light)
    px_rows(px, {13: [5, 9, 11], 14: [5]}, mid)                       # drips
    px[6, 9] = eye; px[9, 9] = eye
    if maw:
        px_rows(px, {10: range(6, 10), 11: range(6, 10)}, maw)
    return im


def skeletal(base, eye):
    """Thin undead biped with rib lines."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {2: range(6, 10), 3: range(6, 10), 4: range(7, 9)}, light)  # skull
    px_rows(px, {5: range(6, 10), 6: range(5, 11), 7: range(5, 11),
                 8: range(6, 10), 9: range(6, 10)}, mid)
    px_rows(px, {6: [6, 8, 10 - 1], 7: [6, 8]}, dark)                 # ribs
    px_rows(px, {5: [4, 11], 6: [4, 11], 7: [4, 11]}, mid)            # arms
    px_rows(px, {10: [6, 9], 11: [6, 9], 12: [6, 9], 13: [6, 9]}, mid)
    px[6, 2] = eye; px[9 - 1, 2] = eye
    return im


def sentinel(base, eye, visor=True):
    """Blocky construct: vault sentinel, guardians."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {2: range(5, 11), 3: range(5, 11), 4: range(5, 11)}, mid)   # head block
    px_rows(px, {5: range(4, 12), 6: range(4, 12), 7: range(4, 12),
                 8: range(4, 12), 9: range(4, 12), 10: range(5, 11)}, mid)
    px_rows(px, {5: [4], 6: [4], 7: [4], 8: [4], 9: range(4, 7)}, dark)
    px_rows(px, {2: range(5, 8), 5: range(5, 8)}, light)
    px_rows(px, {6: [2, 3, 12, 13], 7: [2, 13], 8: [2, 13]}, dark)    # shoulder plates
    px_rows(px, {11: [5, 6, 9, 10], 12: [5, 6, 9, 10], 13: range(5, 11)}, dark)
    if visor:
        px_rows(px, {3: range(6, 10)}, (20, 22, 20, 255))
        px[7, 3] = eye; px[8, 3] = eye
    return im


def tendril(base, eye):
    """Writhing vertical vine mass: stranglers, choir tendrils."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {3: [7, 8], 4: [6, 7, 8], 5: [6, 7], 6: [7, 8, 9],
                 7: [8, 9], 8: [7, 8], 9: [6, 7, 8], 10: [6, 7],
                 11: [7, 8, 9], 12: [7, 8, 9], 13: range(5, 11)}, mid)
    px_rows(px, {4: [4, 5], 5: [3, 4], 6: [3], 8: [11, 12], 9: [12],
                 7: [4, 5], 10: [10, 11]}, dark)                      # side tendrils
    px_rows(px, {3: [7], 6: [7]}, light)
    px[7, 5] = eye
    return im


def lurker(base, eye):
    """Half-buried mound with watching eyes."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {9: range(5, 11), 10: range(4, 12), 11: range(3, 13),
                 12: range(3, 13)}, mid)
    px_rows(px, {11: [3, 4], 12: range(3, 7)}, dark)
    px_rows(px, {9: range(6, 9)}, light)
    px_rows(px, {8: [5, 6, 9, 10]}, mid)                              # eye stalks
    px[5, 7] = eye; px[10, 7] = eye
    px_rows(px, {7: [5, 10]}, mid)
    return im


def imp(base, eye):
    """Small mischief biped: the gnome-folk."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {5: range(6, 10), 6: range(5, 11), 7: range(5, 11)}, mid)   # big head
    px_rows(px, {4: [5, 10]}, dark)                                   # horn nubs
    px_rows(px, {8: range(6, 10), 9: range(6, 10), 10: range(6, 10)}, mid)
    px_rows(px, {8: [5, 10], 9: [5, 10]}, dark)                       # arms
    px_rows(px, {9: [6]}, dark)
    px_rows(px, {11: [6, 9], 12: [6, 9]}, dark)                       # legs
    px_rows(px, {5: range(6, 8)}, light)
    px[6, 6] = eye; px[9, 6] = eye
    return im


def snapjaw_boss(base, eye, crest):
    """Bulked snapjaw with a crest — chieftain/warlord."""
    dark, mid, light = tones(base)
    im = blank(); px = im.load()
    px_rows(px, {2: range(5, 10), 3: range(4, 11), 4: range(4, 12)}, mid)   # big head
    px_rows(px, {4: range(9, 13), 5: range(10, 13)}, dark)            # JAW
    px_rows(px, {5: range(4, 10), 6: range(3, 12), 7: range(3, 12),
                 8: range(3, 12), 9: range(4, 11), 10: range(4, 11)}, mid)
    px_rows(px, {6: [3, 4], 7: [3], 8: [3, 4]}, dark)
    px_rows(px, {2: range(5, 7), 5: range(5, 7)}, light)
    px_rows(px, {6: [2, 12], 7: [2, 12], 8: [2, 12]}, dark)           # arms
    px_rows(px, {11: [5, 6, 9, 10], 12: [5, 6, 9, 10], 13: [5, 6, 9, 10]}, dark)
    px_rows(px, {1: range(5, 10), 0: [6, 8]}, crest)                  # war crest
    px[5, 3] = eye; px[8, 3] = eye
    return im


# ── the roster: (file, archetype-call) ───────────────────────────

EYE_R = (200, 60, 50, 255)
EYE_Y = (230, 200, 90, 255)
EYE_W = (230, 230, 235, 255)
EYE_G = (120, 220, 100, 255)
EYE_C = (130, 220, 230, 255)
EYE_K = (25, 25, 30, 255)

ROSTER = {
    # quadrupeds
    "cave_bear":       lambda: quadruped(COL['y'], EYE_R, bulk=1),
    "brittle_hound":   lambda: quadruped(COL['W'], EYE_C),
    "desert_prowler":  lambda: quadruped(COL['W'], EYE_Y, tail=True),
    "pet_dog":         lambda: quadruped(COL['y'], EYE_K),
    "jungle_stalker":  lambda: quadruped(COL['G'], EYE_Y),
    "pale_stalker":    lambda: quadruped(COL['C'], EYE_W),
    # brutes
    "jungle_ape":      lambda: biped_brute(COL['G'], EYE_Y, broad=1),
    "stone_golem":     lambda: biped_brute(COL['y'], EYE_C, broad=1),
    "obsidian_brute":  lambda: biped_brute(COL['m'], EYE_R, broad=1),
    "mosshulk":        lambda: biped_brute(COL['g'], EYE_G, broad=1),
    "sleeping_troll":  lambda: biped_brute(COL['g'], EYE_K, broad=1),
    "ancient_guardian": lambda: sentinel(COL['w'], EYE_C),
    "charred_husk":    lambda: biped_brute(COL['r'], EYE_R),
    "brass_husk":      lambda: sentinel(COL['y'], EYE_R, visor=True),
    # rogues
    "desert_bandit":   lambda: humanoid_rogue(COL['y'], EYE_K),
    "ambush_bandit":   lambda: humanoid_rogue(COL['r'], EYE_K),
    "rune_cultist":    lambda: humanoid_rogue(COL['M'], EYE_W),
    "ruin_scavenger":  lambda: humanoid_rogue(COL['K'], EYE_Y),
    "pale_curator":    lambda: humanoid_rogue(COL['w'], EYE_C),
    "saccharine_envoy": lambda: humanoid_rogue(COL['Y'], EYE_K, hood=False),
    "glassblown_drifter": lambda: humanoid_rogue(COL['R'], EYE_W, hood=False),
    "palimpsest_echo": lambda: humanoid_rogue(COL['B'], EYE_C, hood=False),
    # serpents
    "viper":           lambda: serpent(COL['g'], EYE_R),
    "sand_wurm":       lambda: serpent(COL['W'], EYE_R, big=True),
    # arachnids
    "giant_spider":    lambda: arachnid(COL['G'], EYE_R, big=True),
    "scorpion":        lambda: arachnid(COL['W'], EYE_K, claws=True),
    "glass_scorpion":  lambda: arachnid(COL['C'], EYE_W, claws=True),
    # flyers
    "cave_bat":        lambda: flyer(COL['K'], EYE_R),
    "magpie":          lambda: flyer(COL['K'], EYE_W, wingspan=4),
    # blobs
    "cave_slime":      lambda: blob(COL['g'], EYE_K),
    "rotling":         lambda: blob(COL['g'], EYE_R),
    "glowmaw":         lambda: blob(COL['Y'], EYE_K, maw=(40, 20, 24, 255)),
    # undead / constructs
    "skeletal_sentry": lambda: skeletal(COL['w'], EYE_C),
    "vault_sentinel":  lambda: sentinel(COL['W'], EYE_R),
    # tendrils
    "choir_tendril":   lambda: tendril(COL['G'], EYE_W),
    "canopy_strangler": lambda: tendril(COL['G'], EYE_R),
    # lurkers
    "dune_lurker":     lambda: lurker(COL['y'], EYE_Y),
    # gnome-folk
    "mogu":            lambda: imp(COL['g'], EYE_W),
    "grib":            lambda: imp(COL['G'], EYE_K),
    "nam":             lambda: imp(COL['y'], EYE_W),
    "sien":            lambda: imp(COL['W'], EYE_K),
    "sopp":            lambda: imp(COL['K'], EYE_Y),
    # snapjaw bosses
    "snapjaw_chieftain": lambda: snapjaw_boss(COL['R'], EYE_Y, (196, 166, 80, 255)),
    "snapjaw_warlord":   lambda: snapjaw_boss(COL['M'], EYE_R, (110, 60, 110, 255)),
}

def main():
    n = 0
    for name, make in ROSTER.items():
        im = make()
        im.save(os.path.join(ROOT, name + ".png"))
        n += 1
    print(f"bestiary: {n} sprites generated")


if __name__ == "__main__":
    main()
