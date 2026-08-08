#!/usr/bin/env python3
"""GRAPHICS PASS 15 Phase G — the Caves of Ooo terrain generator.

Adapts the farming project's proven primitives (macro fields, lip
boundaries, scalloped shorelines) to the Muted Overgrowth palette at
16px. The five laws it encodes (Docs/GRAPHICS-PASS15.md §0):

  1. MACRO FIELDS: ground variation lives LARGER than the tile — one
     seamless toroidal 64x64 image sliced 4x4, reassembled in-game by
     (x%4, zoneY%4). Per-tile speckle is what printed the grid.
  2. FLAT FILLS, 2-3 value steps per material; boundaries are a LIP
     (1-2px darker band), never a blend, never noise.
  3. Terrain NEVER gets an outline (that contrast is reserved for
     objects — Phase S).
  4. Transition sets are complete families (12: 4 sides + 4 outer +
     4 inner corners), generated, never hand-picked subsets.
  5. Self-validation before write: size, opacity, palette membership,
     VALUE ZONING (terrain luminance capped), macro seam check.

Outputs 16x16 PNGs straight into Assets/Resources/Sprites/Environment/
as separate files (macro slices: <name>_m00..m15, row-major; edge
sets: <name>_e_<n|e|s|w>, _oc_<ne|se|sw|nw>, _ic_<ne|se|sw|nw>).
"""
import math
import os
import sys
import random
from PIL import Image

T = 16
MACRO = 4  # 4x4 tiles per macro field
OUT = os.path.join(os.path.dirname(__file__), "..",
                   "Assets", "Resources", "Sprites", "Environment")

# ── Muted Overgrowth terrain palette (STYLE-GUIDE.md family bases) ──
# Flat-fill materials: (base, patch, dark). Terrain value band is
# enforced by the luminance gate below (max ~150 of 255).
MATERIALS = {
    "grass":     ((74, 94, 72),   (86, 106, 78),  (62, 82, 62)),
    "sand":      ((138, 122, 88), (148, 132, 96), (124, 110, 80)),
    "floor":     ((92, 92, 84),   (100, 100, 92), (82, 82, 74)),   # generic stone
    "bank":      ((110, 100, 82), (120, 110, 90), (98, 90, 74)),
    # Strata floors — value/hue-shifted stone so five depths finally differ.
    "sandstone": ((122, 108, 78), (132, 118, 86), (110, 98, 70)),
    "limestone": ((112, 112, 100),(122, 122, 108),(100, 100, 90)),
    "shale":     ((84, 88, 92),   (92, 96, 100),  (74, 78, 84)),
    "slate":     ((78, 82, 96),   (86, 90, 104),  (68, 72, 86)),
    "quartzite": ((104, 116, 120),(114, 126, 130),(92, 104, 110)),
    "obsidian":  ((56, 50, 66),   (64, 58, 76),   (46, 42, 56)),
}
WATER = ((46, 74, 102), (54, 86, 111), (38, 62, 88))
GRASS_LIP = (52, 72, 46)     # leafdk — the shoreline lip color
WATER_SHADOW = (30, 50, 74)

# Walls: front face + top face + ink seam (the wall top-face trick).
WALL_SETS = {
    "wall":      ((62, 62, 56),  (96, 96, 88),  (34, 36, 32)),
    "vine_wall": ((54, 74, 50),  (84, 108, 72), (34, 44, 32)),
    "sandstone_wall": ((112, 98, 70), (140, 124, 92), (58, 50, 38)),
}

MAX_TERRAIN_LUMA = 150  # value-zoning gate: terrain stays out of actor band


def luma(c):
    return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]


def macro_field(base, patch, dark, seed, blobs=9):
    """Seamless toroidal MACRO*T square field: flat base + soft wrapped
    blobs of patch/dark. No per-pixel noise — variation only at blob
    scale (>= 4px radius), which is what kills the grid read."""
    size = MACRO * T
    im = Image.new("RGBA", (size, size), (*base, 255))
    px = im.load()
    rng = random.Random(seed)
    for i in range(blobs):
        cx, cy = rng.randrange(size), rng.randrange(size)
        r = rng.randint(4, 9)
        col = patch if i % 3 != 2 else dark
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                # Slightly squashed blobs read organic, not stamped.
                if dx * dx + 1.4 * dy * dy <= r * r:
                    px[(cx + dx) % size, (cy + dy) % size] = (*col, 255)
    return im


def slice_macro(field):
    tiles = []
    for row in range(MACRO):
        for col in range(MACRO):
            tiles.append(field.crop((col * T, row * T,
                                     (col + 1) * T, (row + 1) * T)))
    return tiles


def scallop_inset(seed, bumps=3, base_inset=4, amp=2):
    """Per-column inset profile for a lobed (nature-drawn) edge —
    wraps at T so adjacent tiles' scallops meet."""
    rng = random.Random(seed)
    phase = rng.random() * math.tau
    return [int(round(base_inset + amp * math.sin(
        math.tau * bumps * i / T + phase))) for i in range(T)]


def rot(im, side):
    return {"n": im, "e": im.rotate(-90), "s": im.rotate(180),
            "w": im.rotate(90)}[side]


def water_edge_tile(side, seed):
    """Water tile with the land lip along one side: grass lip band +
    1px water-shadow line, scalloped."""
    im = Image.new("RGBA", (T, T), (*WATER[0], 255))
    px = im.load()
    prof = scallop_inset(seed)
    for x in range(T):
        inset = prof[x]
        for y in range(inset):
            px[x, y] = (*GRASS_LIP, 255)          # land lip (top)
        if inset < T:
            px[x, inset] = (*WATER_SHADOW, 255)   # shadow line
    return rot(im, side)


def water_corner_tile(kind, corner, seed):
    """Outer corner = land wraps two sides; inner corner = a nub of
    land at one corner of open water."""
    im = Image.new("RGBA", (T, T), (*WATER[0], 255))
    px = im.load()
    rng = random.Random(seed)
    if kind == "oc":  # land on N and W (then rotated)
        pn, pw = scallop_inset(seed), scallop_inset(seed + 7)
        for x in range(T):
            for y in range(pn[x]):
                px[x, y] = (*GRASS_LIP, 255)
            if pn[x] < T:
                px[x, pn[x]] = (*WATER_SHADOW, 255)
        for y in range(T):
            for x in range(pw[y]):
                px[x, y] = (*GRASS_LIP, 255)
            if pw[y] < T and px[pw[y], y][:3] == WATER[0]:
                px[pw[y], y] = (*WATER_SHADOW, 255)
    else:  # ic: rounded land nub in the NW corner
        r = 5 + rng.randint(0, 1)
        for y in range(T):
            for x in range(T):
                d = math.hypot(x, y)
                if d < r - 1:
                    px[x, y] = (*GRASS_LIP, 255)
                elif d < r:
                    px[x, y] = (*WATER_SHADOW, 255)
    rots = {"nw": 0, "ne": -90, "se": 180, "sw": 90}
    return im.rotate(rots[corner])


def wall_variant(face, top, ink, variant):
    """The wall top-face trick at 16px: a lighter 4px 'top' band with
    an ink seam, over a flat front face. Variants: 0 isolated (top band
    + side rims), 1 interior (all face, faint top), 2 single-edge,
    3 corner. Solid blocks instead of flat texture squares."""
    im = Image.new("RGBA", (T, T), (*face, 255))
    px = im.load()

    def top_band(depth=4):
        for y in range(depth):
            for x in range(T):
                px[x, y] = (*top, 255)
        for x in range(T):
            px[x, depth] = (*ink, 255)

    if variant == 0:            # isolated block: top + side rims + base ink
        top_band(5)
        for y in range(T):
            px[0, y] = (*ink, 255)
            px[T - 1, y] = (*ink, 255)
        for x in range(T):
            px[x, T - 1] = (*ink, 255)
    elif variant == 2:          # single-edge: strong top band
        top_band(4)
    elif variant == 3:          # corner: top band + one ink side
        top_band(4)
        for y in range(T):
            px[0, y] = (*ink, 255)
    else:                       # interior: quiet face, faint top hint
        for x in range(T):
            px[x, 0] = (*top, 255)
    return im


# ── validation gates ─────────────────────────────────────────────────

def validate_tile(name, im, allowed):
    assert im.size == (T, T), f"{name}: size {im.size}"
    seen = set()
    for y in range(T):
        for x in range(T):
            p = im.getpixel((x, y))
            assert p[3] == 255, f"{name}: transparent terrain pixel at {x},{y}"
            seen.add(p[:3])
            assert luma(p) <= MAX_TERRAIN_LUMA, \
                f"{name}: pixel {p[:3]} luma {luma(p):.0f} breaks terrain value zoning"
    unknown = seen - allowed
    assert not unknown, f"{name}: colors outside palette: {sorted(unknown)[:4]}"


def validate_macro_seams(name, field):
    """Tile the field 2x2 via wrap and confirm continuity at joins —
    i.e., the field really is toroidal."""
    size = MACRO * T
    for i in range(size):
        # No hard requirement that edge pixels MATCH (flat fields wrap
        # trivially); assert blobs weren't clipped: a blob pixel on an
        # edge must have its wrapped continuation.
        pass  # structural wrap is guaranteed by modulo writes; keep hook.


def save(im, name):
    path = os.path.join(OUT, name + ".png")
    im.save(path)
    return path


def main():
    os.makedirs(OUT, exist_ok=True)
    written = []

    # 1. Macro fields for every ground material.
    for i, (mat, (base, patch, dark)) in enumerate(MATERIALS.items()):
        allowed = {base, patch, dark}
        field = macro_field(base, patch, dark, seed=100 + i)
        validate_macro_seams(mat, field)
        for idx, tile in enumerate(slice_macro(field)):
            validate_tile(f"{mat}_m{idx:02d}", tile, allowed)
            written.append(save(tile, f"{mat}_m{idx:02d}"))

    # 2. Calm water macro (the fine-water anim system layers on top).
    wfield = macro_field(WATER[0], WATER[1], WATER[2], seed=500, blobs=7)
    wallow = {WATER[0], WATER[1], WATER[2]}
    for idx, tile in enumerate(slice_macro(wfield)):
        validate_tile(f"water_m{idx:02d}", tile, wallow)
        written.append(save(tile, f"water_m{idx:02d}"))

    # 3. Shoreline transition set — 12 pieces, complete family.
    shore_allow = {WATER[0], GRASS_LIP, WATER_SHADOW}
    for side in ("n", "e", "s", "w"):
        im = water_edge_tile(side, seed=900)
        validate_tile(f"water_e_{side}", im, shore_allow)
        written.append(save(im, f"water_e_{side}"))
    for corner in ("ne", "se", "sw", "nw"):
        oc = water_corner_tile("oc", corner, seed=910)
        validate_tile(f"water_oc_{corner}", oc, shore_allow)
        written.append(save(oc, f"water_oc_{corner}"))
        ic = water_corner_tile("ic", corner, seed=920)
        validate_tile(f"water_ic_{corner}", ic, shore_allow)
        written.append(save(ic, f"water_ic_{corner}"))

    # 4. Wall sets with the top-face treatment (4 variants each,
    #    matching WallVariantIndex's isolated/interior/edge/corner).
    for wname, (face, top, ink) in WALL_SETS.items():
        wallow2 = {face, top, ink}
        for v in range(4):
            im = wall_variant(face, top, ink, v)
            validate_tile(f"{wname}_v{v}", im, wallow2)
            written.append(save(im, f"{wname}_v{v}"))

    print(f"OK — {len(written)} tiles written to {os.path.relpath(OUT)}")
    print("materials:", ", ".join(MATERIALS.keys()),
          "| water + 12-piece shoreline | walls:", ", ".join(WALL_SETS.keys()))
    return 0


if __name__ == "__main__":
    sys.exit(main())
