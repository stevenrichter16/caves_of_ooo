"""Original Sodden cuboid recipes. Deterministic offline source, no scene edits."""
import argparse
import hashlib
import json
import random
from pathlib import Path

PALETTE = [
    "#092D29", "#103A33", "#19463B", "#275447",  # quiet land
    "#071C20", "#102C2E",                       # black mire / reflection
    "#273D3A", "#41534E", "#617269",            # cut peat
    "#596B5C", "#A1AF91", "#D0D5B6",            # soaked wood / pale snags
    "#184A31", "#347345", "#589459", "#A9BA7A",# rooted jade / seed heads
    "#3E392B", "#786347", "#AA9064",            # worked wood
    "#344846", "#81948A",                       # dull metal
    "#485651", "#74B6B5", "#D8E1CB",            # cloth / clear bead / pale face
]
FAMILIES = ["ground", "mire", "peat", "snag", "boards", "body", "reeds", "greatdew"]
BLUEPRINTS = {"ground": "Grass", "mire": "MirePool", "peat": "PeatBank", "snag": "DeadTree",
              "boards": "Duckboard", "body": "BogTakenBody", "reeds": "Reeds", "greatdew": "Greatdew"}
BUDGETS = dict(ground=120, mire=96, peat=576, snag=720, boards=384, body=576, reeds=600, greatdew=720)
BUDGETS.update(vines=480, brine=192, acid=192, steam=336)
REGIONAL = [
    ("spread-environment-paving", "paving", "StoneFloor"),
    ("wellmeet-floor", "floor", "StoneFloor"),
    ("sumphold-wall", "wall", "StoneWall"),
    ("sumphold-water", "water", "WaterPuddle"),
    ("drownedledger-boards", "regional-boards", "Duckboard"),
    ("wellmeet-tent", "tent", "TentWall"),
    ("wellmeet-corner", "corner", "TentWall"),
    ("drownedledger-preserved", "preserved", "PreFellingBody"),
    ("drownedledger-stake", "stake", "SurveyStake"),
    ("drownedledger-table", "table", "ReadingTable"),
    ("ring-vine-wall", "vines", "VineWall"),
    ("ring-brine-pool", "brine", "BrinePool"),
]
SINGLES = [
    ("sodden-district-bench-broken", "bench-broken", "SoddenDressingBench"),
    ("sodden-district-bench-working", "bench-working", "SoddenDressingBench"),
    ("sodden-district-works-locker", "locker", "SoddenWorksLocker"),
    ("sodden-district-works-salvage", "salvage", "SoddenWorksSalvage"),
    ("sodden-district-route-notice", "notice", "SoddenRouteNotice"),
    ("density-pool-acid", "acid", "AcidPool"),
    ("ring-steam-vent", "steam", "SteamVent"),
]


def model_id(family, variant):
    prefix = "spread-" if family == "reeds" else "sodden-native-" if family == "greatdew" else "sodden-"
    return f"{prefix}{family}-{variant}"


def make(family, variant):
    rng = random.Random(20261008 + FAMILIES.index(family) * 941 + variant * 137)
    boxes = []

    def box(x, y, z, width, height, depth, color):
        boxes.append({"center": dict(zip("xyz", (round(v, 6) for v in (x, y, z)))),
                      "size": dict(zip("xyz", (round(v, 6) for v in (width, height, depth)))), "color": color})

    if family == "ground":
        box(0, -.02, 0, 1, .04, 1, 0)
        for i in range(4 + variant % 3):
            x, z = rng.uniform(-.40, .40), rng.uniform(-.40, .40)
            h, w = rng.choice((.038, .048, .058)), rng.choice((.045, .062, .078))
            box(x, h / 2, z, w, h, w * .85, 2 if i % 3 else 3)
    elif family == "mire":
        # The native saved water overlay is at .035; the actual mire top remains .048.
        box(0, .038, 0, 1, .02, 1, 4)
        for i in range(2 + variant % 2):
            box(rng.uniform(-.29, .29), .049, rng.uniform(-.36, .36), .14 + i * .03, .002, .025, 5)
    elif family == "peat":
        h = .88 + variant * .045
        box(0, h / 2, 0, .96, h, .94, 6)
        # Both cut faces show horizontal layers; the native camera sees -Z.
        for face in (-1, 1):
            for row in range(4):
                split = -.07 + ((row + variant + face) % 3) * .058
                for left, right in ((-.467, split - .012), (split + .012, .467)):
                    y = .11 + row * .205 + ((row + variant) % 2) * .011
                    box((left + right) / 2, y, face * .474, right - left, .125 + (row % 2) * .019,
                        .033, 7 if (row + variant) % 3 else 8)
        for side in (-1, 1):
            for row in range(3):
                box(side * .482, .16 + row * .255, (row % 2 - .5) * .10, .032, .13, .72, 7)
        # Broad overlapping cuts interrupt the outline, rather than three narrow
        # full-depth cap columns with a repeating six-patch green grid.
        flip = -1 if variant % 2 else 1
        terraces = [(-.05, -.25, .82, .44, .12), (.10, .04, .73, .39, .18), (-.08, .31, .71, .29, .085)]
        for i, (x, z, w, d, cap) in enumerate(terraces):
            rise = cap + ((i + variant) % 3) * .017
            box(x * flip, h + rise / 2, z, w - (variant % 2) * .035, rise, d, 7 if i != 1 else 8)
            box((x + (-.13 if i == 0 else .12 if i == 1 else -.075)) * flip,
                h + rise + .018, z + rng.uniform(-.038, .038),
                .23 + ((i + variant) % 3) * .026, .036, .19 + ((i + variant) % 2) * .036, 12 if i % 2 else 13)
    elif family == "snag":
        h = 2.79 + variant * .105
        sway = (variant - 1.5) * .022
        for i in range(5):
            y0 = i * h / 5
            box(sway * i / 4, y0 + h / 10, (i % 2) * .028, .26 - i * .026, h / 5 + .01, .25 - i * .022, 9 if i < 2 else 10)
            if i > 0:
                box(sway * i / 4 - .027, y0 + h / 10 + .024, .139 - i * .008, .10, h / 5 - .10, .021, 11)
        for side in (-1, 1):
            for i in range(4):
                x = side * (.12 + i * .094)
                # Alternate the genuinely broken high side, not just height jitter.
                high_side = -1 if variant % 2 else 1
                y = h * (.61 if side == high_side else .46) + i * .105 + (variant % 2) * .025
                box(x, y, -.02 + side * .036, .18, .13, .17, 10)
                if i > 1:
                    rise = .22 + ((i + variant + (side > 0)) % 3) * .075
                    box(x + side * .012, y + rise / 2, -.02 + side * .036, .115, rise, .12, 10)
                    box(x + side * .012, y + rise + .019, -.02 + side * .036, .117, .038, .124, 11)
        for i, (x, z) in enumerate(((-.20, -.08), (.17, -.11), (-.11, .21), (.17, .17))):
            box(x, .065 + (i % 2) * .015, z, .25, .13, .14, 9)
        box(sway, h + .015, .028, .13, .06, .13, 11)
    elif family == "boards":
        for z in (-.32, .32):
            box(0, .044, z, .98, .088, .115, 16)
        for plank in range(3):
            x = (plank - 1) * .31
            zshift = ((plank + variant) % 3 - 1) * .012
            length = .96 - ((plank + variant) % 3) * .021
            # Long crosswise boards retain their identity across the whole cell.
            box(x, .128 + ((plank + variant) % 2) * .008, zshift,
                .287, .072, length, 17 if (plank + variant) % 3 else 18)
            for z in (-.31, .31):
                box(x + .07, .176 + ((plank + variant) % 2) * .008, z, .037, .012, .034, 19)
            box(x - .098, .17, zshift + .05, .025, .01, .46, 16)
            box(x - .055, .173, zshift + (1 if (plank + variant) % 2 else -1) * length * .39,
                .069, .012, .052, 16)
    elif family == "body":
        shift = (variant - 1.5) * .008
        side = -1 if variant % 2 else 1
        box(0, .126, -.015, .33, .20, .40, 21)
        box(-.08, .20, -.02, .16, .072, .37, 9)
        box(.08, .21, -.06, .15, .07, .28, 21)
        box(shift, .147, .292, .20, .20, .215, 10)
        box(shift, .26, .296, .19, .04, .18, 23)
        box(shift - .066, .27, .337, .035, .024, .040, 9)
        box(shift + .052, .27, .337, .035, .024, .040, 9)
        box(shift, .196, .396, .096, .062, .025, 10)
        for sign in (-1, 1):
            x = sign * .095 + (shift if sign == side else 0)
            box(x, .10, -.291, .122, .145, .18, 21)
            box(x + sign * .024, .087, -.414, .116, .13, .13, 16)
            box(x, .182, -.282, .082, .027, .081, 9)
            armx = sign * (.204 if sign == side else .217)
            box(armx, .121, .017 + sign * shift, .099, .123, .27, 21)
            box(armx - sign * .046, .205, .02 + sign * .065, .174, .08, .088, 9)
            box(armx - sign * .109, .257, .02 + sign * .065, .079, .045, .080, 23)
        for i in range(4):
            box(-.062 + i * .041, .255, -.113, .025, .015, .20, 16 if i % 2 else 17)
        box(.128, .222, -.141, .085, .081, .095, 17)
        box(.125, .269, -.136, .066, .014, .079, 18)
    elif family == "reeds":
        positions = [(-.29, -.21), (-.06, -.25), (.23, -.15), (-.23, .04), (.01, .01), (.27, .20), (-.09, .25)]
        for i, (px, pz) in enumerate(positions):
            x, z = px + rng.uniform(-.012, .012), pz + rng.uniform(-.012, .012)
            h = .50 + ((i * 3 + variant) % 5) * .047
            box(x, .035, z, .13, .07, .12, 12)
            box(x, h * .28, z, .099, h * .56, .107, 13)
            box(x + .014, h * .71, z, .102, h * .45, .098, 14)
            box(x + .014, h + .025, z, .135, .13, .119, 15 if (i + variant) % 3 else 11)
            if (i + variant) % 2 == 0:
                direction = -1 if i % 2 else 1
                box(x + direction * .068, h * .47, z + .018, .15, .082, .071, 13)
                box(x + direction * .103, h * .47 + .07, z + .018, .069, .18, .071, 14)
    elif family == "greatdew":
        side = 1 if variant == 0 else -1
        box(0, .046, 0, .50, .092, .43, 6)
        box(-.10, .103, .05, .31, .114, .28, 12)
        box(.12, .082, -.08, .24, .10, .24, 13)
        # A large angular hook, not a tree, insect, or impassable wall.
        path = [(0, .18), (-.055, .30), (-.085, .42), (-.065, .55), (.005, .67), (.105, .745), (.22, .735), (.29, .655)]
        for i, (x, y) in enumerate(path):
            box(x * side, y, -.025 + (variant * .055), .12, .16 if i < 5 else .113, .13, 13 if i < 4 else 14 if i < 6 else 11)
        for i, (x, z) in enumerate(((-.25, -.16), (.20, -.23), (-.22, .19), (.20, .16))):
            box(x, .094, z, .19, .10, .092, 12)
            box(x * .86, .161 + i * .008, z, .12, .12, .096, 13)
        for i, (x, y, z) in enumerate(((-.13, .45, .075), (.045, .66, .085), (.24, .66, .085), (.31, .52, -.05))):
            box(x * side, y, z + variant * .026, .073, .11, .071, 22)
            box(x * side - .012, y + .032, z + .029 + variant * .026, .034, .035, .021, 23)
        box(.295 * side, .583, .029 + variant * .02, .08, .086, .075, 11)
    else:
        raise ValueError(family)

    return {"id": model_id(family, variant), "family": family, "variant": variant,
            "sourceBlueprint": BLUEPRINTS[family], "boxes": boxes}


def regional(model_id, family, blueprint, variant):
    rng = random.Random(20261009 + sum(ord(c) for c in family) * 73 + variant * 139)
    boxes = []

    def box(x, y, z, w, h, d, color):
        boxes.append({"center": dict(zip("xyz", (round(v, 6) for v in (x, y, z)))),
                      "size": dict(zip("xyz", (round(v, 6) for v in (w, h, d)))), "color": color})

    if family in ("paving", "floor"):
        box(0, -.018, 0, 1, .036, 1, 6)
        # Broad quiet slabs: subtle mortar, not a pale miniature checkerboard.
        split = -.09 + variant * .055
        for a, b in ((-.491, split - .01), (split + .01, .491)):
            box((a + b) / 2, .011, 0, b - a, .022, .982, 7 if family == "paving" else 6)
        box(-.34 + variant * .19, .025, -.34, .085, .006, .042, 8 if family == "paving" else 7)
        box(.30 - variant * .16, .025, .37, .058, .006, .055, 7)
    elif family == "water":
        # Ordinary water remains lighter than bog-mire and below the native .035 overlay.
        box(0, .002, 0, 1, .02, 1, 5)
        for i in range(2):
            box(rng.uniform(-.30, .30), .013, rng.uniform(-.37, .37), .16 + .03 * i, .002, .017, 6)
    elif family == "wall":
        box(0, .49, 0, .98, .98, .98, 6)
        for side in (-1, 1):
            for course in range(4):
                split = -.08 + ((course + variant) % 3) * .075
                for a, b in ((-.495, split - .011), (split + .011, .495)):
                    box((a + b) / 2, .13 + course * .235, side * .482,
                        b - a, .209, .034, 7 if (course + variant) % 3 else 8)
                box(side * .482, .13 + course * .235, -.025 + ((course + variant) % 2) * .05,
                    .034, .21, .946, 7)
        for i in range(3):
            box((i - 1) * .332, 1.013 + ((i + variant) % 2) * .013, 0, .328, .052, .982, 8)
        for i in range(3):
            box(-.30 + i * .30, .047, -.492, .11 + (i % 2) * .035, .078, .014, 12)
    elif family == "regional-boards":
        boxes = make("boards", variant)["boxes"]
        # Excavation access is rain-dark timber, distinct from the warmer main causeway.
        for b in boxes:
            if b["color"] == 18: b["color"] = 17
            elif b["color"] == 17: b["color"] = 9
    elif family in ("tent", "corner"):
        def panel(axis, start, end):
            middle = (start + end) / 2
            w = end - start
            if axis == "x":
                box(middle, .48, 0, w, .94, .13, 9)
                box(middle, 1.018, 0, w, .071, .145, 16)
            else:
                box(0, .48, middle, .13, .94, w, 9)
                box(0, 1.018, middle, .145, .071, w, 16)
            for j in range(3):
                at = start + (j + .5) * w / 3
                height = .66 + ((j + variant) % 3) * .055
                if axis == "x":
                    box(at, .49, -.076, w / 3 - .032, height, .022, 10 if j % 2 else 9)
                    box(at, .49, .076, w / 3 - .032, height, .022, 10 if j % 2 else 9)
                else:
                    box(-.076, .49, at, .022, height, w / 3 - .032, 10 if j % 2 else 9)
                    box(.076, .49, at, .022, height, w / 3 - .032, 10 if j % 2 else 9)
        if family == "tent":
            panel("x", -.5, .5)
            post = -.43 + variant * .028
            box(post, .55, 0, .10, 1.10, .24, 16)
            box(post, .90, -.139, .13, .072, .037, 17)
        else:
            # Original corner joins +X/+Z; native recipe owns its quarter-turn.
            panel("x", 0, .5); panel("z", 0, .5)
            box(0, .55, 0, .13, 1.10, .13, 16)
            box(0, .905, 0, .165, .07, .165, 17)
    elif family == "preserved":
        boxes = make("body", variant)["boxes"]
        for b in boxes:
            # Preserve the existing witness's head-at-negative-Z orientation.
            b["center"]["x"] *= -1; b["center"]["z"] *= -1
            b["color"] = {21: 10, 16: 9, 17: 9, 18: 10, 23: 11}.get(b["color"], b["color"])
        box(.185, .269, -.07, .093, .018, .09, 20)
    elif family == "stake":
        h = 1.04 + variant * .06
        box(0, h / 2, 0, .115, h, .115, 16)
        box(-.024, h / 2, .062, .031, h - .16, .022, 17)
        box(0, h - .14, .01, .24, .26, .16, 7)
        for side in (-1, 1):
            box(0, h - .13, side * .096, .179, .177, .015, 10)
            box(-.025 + variant * .014, h - .12, side * .108, .088, .022, .012, 19)
        box(0, .038, 0, .22, .075, .19, 6)
    elif family in ("table", "bench-working", "bench-broken"):
        broken = family == "bench-broken"
        for x in (-.34, .34):
            for z in (-.27, .27):
                height = .39 if broken and x > 0 else .70
                box(x, height / 2, z, .115, height, .115, 16)
                box(x, height - .09, z, .139, .047, .14, 19)
        for i in range(5):
            x = (i - 2) * .176
            y = .725 if not broken or i < 2 else .55 - (i - 2) * .08
            box(x, y, 0, .166, .10, .76 if family != "table" else .84, 17 if i % 3 else 18)
        for z in (-.26, .26): box(0, .30, z, .74, .065, .067, 16)
        if family == "table":
            for side in (-1, 1):
                box(side * .30, .81, side * .10, .13, .08, .24, 10)
                box(side * .30, .861, side * .10, .15, .022, .064, 16)
            box(.245, .89 + variant * .01, -.29, .25, .20 + variant * .02, .089, 16)
            box(.245, .914 + variant * .01, -.237, .20, .15, .017, 11)
        else:
            for side in (-1, 1):
                post = .84 if not broken else (.65 if side < 0 else .32)
                bottom = .73 if not broken else (.73 if side < 0 else .39)
                box(side * .35, bottom + post / 2, -.27, .12, post, .12, 16)
            if not broken:
                box(0, 1.60, -.27, .91, .10, .135, 17)
                box(-.09, 1.32, -.205, .42, .46, .045, 10)
                for i in range(3): box(-.22 + i * .13, 1.32, -.174, .065, .35, .014, 11)
            else:
                box(-.03, .11, .26, .85, .13, .14, 17)
            box(-.16, .81 if not broken else .83, .09, .32, .07, .32, 11)
            box(-.16, .858 if not broken else .878, .09, .055, .022, .34, 12)
            box(.17, .812 if not broken else .59, .12, .29, .064, .25, 10)
            box(.17, .855 if not broken else .633, .12, .058, .02, .27, 13)
    elif family == "locker":
        box(0, .12, 0, .84, .24, .71, 16)
        box(0, .68, 0, .81, .90, .67, 17)
        for face in (-1, 1):
            for i in range(5):
                box((i - 2) * .15, .68, face * .345, .141, .87, .032, 17 if i % 2 else 18)
            for x in (-.285, .285): box(x, .68, face * .37, .076, .97, .035, 19)
            box(0, .67, face * .378, .112, .255, .036, 19)
            box(0, .69, face * .404, .16, .066, .029, 20)
        box(0, 1.17, 0, .94, .10, .79, 16)
        for i in range(4): box((i - 1.5) * .224, 1.226, 0, .21, .025, .75, 17)
    elif family == "salvage":
        for i in range(4):
            box(-.025 + (i % 2) * .05, .10 + i * .073, -.30 + i * .20, .88 - i * .025, .115, .14, 16 if i % 2 else 17)
            for side in (-1, 1): box(side * (.43 - i * .012), .102 + i * .073, -.30 + i * .20, .023, .083, .10, 18)
        box(-.31, .56, -.25, .17, 1.02, .18, 16)
        box(-.31, 1.08, -.25, .25, .07, .18, 18)
        box(.21, .37, .08, .15, .16, .80, 17)
        box(.20, .48, .24, .12, .41, .13, 16)
        box(-.11, .34, -.25, .19, .036, .052, 19)
    elif family == "notice":
        box(0, .95, 0, .16, 1.90, .16, 16)
        box(-.025, .95, -.087, .038, 1.73, .022, 17)
        for i, (x, y) in enumerate(((-.055, 1.69), (.055, 1.30))):
            box(x, y, .11, .85, .27, .12, 17)
            for face in (-1, 1):
                for line in range(2):
                    box(x + (-.16 if i == 0 else .16), y + (line - .5) * .065,
                        .11 + face * .068, .29 if line == 0 else .20, .025, .016, 10)
                box(x, y, .11 + face * .072, .035, .035, .018, 19)
        box(0, 1.94, 0, .245, .09, .23, 17)
        box(0, .044, 0, .36, .088, .34, 6)
    elif family == "vines":
        # A dense rooted wall, not an enlarged harmless reed clump.
        box(0, .045, 0, .93, .09, .91, 12)
        box(-.12, .087, .16, .59, .11, .50, 9)
        box(.16, .071, -.14, .51, .09, .56, 12)
        positions = [(-.26, -.24), (.255, -.19), (-.19, .25), (.25, .25)]
        for i, (px, pz) in enumerate(positions):
            x, z = px + rng.uniform(-.012, .012), pz + rng.uniform(-.012, .012)
            height = 1.10 + ((i + variant) % 3) * .045
            sway = -.028 if (i + variant) % 2 else .028
            for step in range(3):
                box(x + sway * step, (step + .5) * height / 3, z, .145, height / 3 + .035, .14, 9 if step == 0 else 12)
            box(x, .53 + ((i + variant) % 3) * .09, z, .36, .11, .16, 13)
            for tier in range(3):
                width = .34 + ((i + tier + variant) % 3) * .025
                box(x + sway, .46 + tier * .32 + ((i + variant) % 2) * .055,
                    z + (tier % 2 - .5) * .045, width, .20 + (tier % 2) * .04, .30 + (i % 2) * .04,
                    12 if tier == 0 else 13 if tier == 1 else 14)
        box(-.035, .63 + variant * .015, -.22, .79, .075, .084, 12)
        box(.015, .84 - variant * .012, .22, .72, .082, .079, 13)
    elif family == "brine":
        box(0, .037, 0, 1, .024, 1, 22)
        for i in range(3):
            box(rng.uniform(-.33, .33), .050, rng.uniform(-.34, .34), .17 + i * .025, .002, .019, 20)
        for i in range(5):
            x, z = rng.uniform(-.40, .40), rng.uniform(-.40, .40)
            height = .039 + ((i + variant) % 3) * .012
            box(x, .050 + height / 2, z, .066 + (i % 2) * .018, height, .060, 23)
        box(-.23 + variant * .13, .054, -.21, .19, .006, .039, 11)
    elif family == "acid":
        # Keep green/yellow acid warning instead of assimilating it to teal water.
        box(0, .037, 0, 1, .024, 1, 14)
        for i in range(3):
            box(-.26 + i * .25, .050, -.28 + i * .24, .23, .002, .039, 13)
        for i, (x, z) in enumerate(((-.33, -.13), (-.10, .30), (.23, -.30), (.32, .15), (.03, -.04))):
            height = .038 + (i % 3) * .012
            box(x, .051 + height / 2, z, .104 + (i % 2) * .025, height, .09, 15)
            if i % 2 == 0: box(x - .018, .052 + height, z + .008, .036, .012, .039, 11)
    elif family == "steam":
        # Mineral vent only. Live gas/heat are still owned by native systems.
        box(0, .045, 0, .72, .09, .69, 6)
        box(0, .145, 0, .31, .018, .30, 4)
        for i, (x, z, w, d) in enumerate(((-.26, 0, .17, .52), (.26, .01, .17, .52),
                                         (0, -.255, .39, .17), (.01, .25, .39, .17))):
            height = .31 + (i % 3) * .055
            box(x, height / 2, z, w, height, d, 7 if i % 2 else 8)
            box(x - .018, height + .039, z + .012, w * .74, .078, d * .73, 11)
            box(x + .011, height + .091, z - .019, w * .44, .026, d * .43, 23)
        box(-.19, .148, -.22, .13, .14, .10, 8)
        box(.19, .20, .21, .14, .22, .12, 8)
    else:
        raise ValueError(family)
    return {"id": model_id, "family": family, "variant": variant, "sourceBlueprint": blueprint, "boxes": boxes}


def build():
    models = [make(f, v) for f in FAMILIES for v in range(2 if f == "greatdew" else 4)]
    models.extend(regional(f"{prefix}-{v}", f, bp, v) for prefix, f, bp in REGIONAL for v in range(4))
    models.extend(regional(identifier, f, bp, 0) for identifier, f, bp in SINGLES)
    return {"schemaVersion": 1, "palette": PALETTE, "models": models}


def validate(kit):
    assert len(kit["models"]) == 85
    assert len({m["id"] for m in kit["models"]}) == 85
    for model in kit["models"]:
        assert len(model["boxes"]) * 12 <= BUDGETS.get(model["family"], 720), model["id"]
        for b in model["boxes"]:
            assert 0 <= b["color"] < len(PALETTE)
            assert all(v > 0 for v in b["size"].values())
            for axis in "xz":
                assert abs(b["center"][axis]) + b["size"][axis] / 2 <= .50001, (model["id"], b)
    return kit


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=Path(__file__).with_name("kit.json"))
    args = parser.parse_args()
    payload = json.dumps(validate(build()), indent=2) + "\n"
    args.output.write_text(payload)
    print(json.dumps({"models": len(build()["models"]), "boxes": sum(len(m["boxes"]) for m in build()["models"]),
                      "sha256": hashlib.sha256(payload.encode()).hexdigest()}))
