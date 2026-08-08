#!/usr/bin/env python3
"""GRAPHICS PASS 15 round 2 — the automated squint test.

Usage:  python3 ArtTools/coo_squint_test.py <screenshot.png> [x0 y0 x1 y1]

Two checks on a gameplay screenshot (optionally cropped to the
tilemap region):

1. GRID-PATTERN DETECTOR — the whole point of the macro fields is
   that variation lives LARGER than the tile. If the 16px tiling
   ever reasserts itself (a regression to per-tile hashing), the
   luminance autocorrelation shows a spike at exactly the tile
   period vs its neighbor lags. FAIL if the periodic spike exceeds
   the neighborhood by more than GRID_SPIKE_TOLERANCE.

2. VALUE-ZONING HISTOGRAM — terrain is supposed to live in the
   30-60% brightness band (MAX_TERRAIN_LUMA gate in the generator),
   leaving the extremes for actors/UI. If more than
   BRIGHT_FRACTION_MAX of pixels exceed the bright threshold, the
   scene has lost its value zoning (e.g. fog dimming broke, or a
   terrain tile regenerated too bright).

Exit code 0 = both pass; 1 = any check fails. Prints a table either
way — the V-loop records the numbers per round.
"""

import sys
from PIL import Image

TILE = 16                       # on-screen tile period AFTER downscale-aware sampling
GRID_SPIKE_TOLERANCE = 0.06     # autocorr spike allowed above neighbor lags
BRIGHT_LUMA = 190               # "bright" threshold (0-255)
BRIGHT_FRACTION_MAX = 0.18      # more than this fraction bright = zoning broken


def luma_rows(im):
    g = im.convert("L")
    w, h = g.size
    px = list(g.get_flattened_data() if hasattr(g, "get_flattened_data") else g.getdata())
    return [px[y * w:(y + 1) * w] for y in range(h)], w, h


def autocorr_spike(rows, w, h, axis, period):
    """Mean normalized autocorrelation at `period` minus the mean of
    the two neighboring lags (period-3, period+3). Positive spike =
    periodic structure at exactly the tile size."""
    def corr(lag):
        num = cnt = 0
        if axis == 'x':
            for y in range(0, h, 4):
                row = rows[y]
                for x in range(0, w - lag, 4):
                    num += abs(row[x] - row[x + lag])
                    cnt += 1
        else:
            for y in range(0, h - lag, 4):
                r0, r1 = rows[y], rows[y + lag]
                for x in range(0, w, 4):
                    num += abs(r0[x] - r1[x])
                    cnt += 1
        return num / max(cnt, 1) / 255.0
    at = corr(period)
    near = (corr(period - 3) + corr(period + 3)) / 2
    # LOW difference at the period vs neighbors = self-similar at
    # that lag = grid. Spike is how much MORE similar (lower diff).
    return near - at


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    im = Image.open(sys.argv[1]).convert("RGB")
    if len(sys.argv) >= 6:
        x0, y0, x1, y1 = map(int, sys.argv[2:6])
        im = im.crop((x0, y0, x1, y1))

    rows, w, h = luma_rows(im)

    sx = autocorr_spike(rows, w, h, 'x', TILE)
    sy = autocorr_spike(rows, w, h, 'y', TILE)
    grid_ok = sx < GRID_SPIKE_TOLERANCE and sy < GRID_SPIKE_TOLERANCE

    flat = [v for r in rows for v in r]
    bright = sum(1 for v in flat if v > BRIGHT_LUMA) / len(flat)
    zoning_ok = bright <= BRIGHT_FRACTION_MAX

    print(f"squint test on {sys.argv[1]} ({w}x{h})")
    print(f"  grid spike x={sx:+.4f} y={sy:+.4f} (tol {GRID_SPIKE_TOLERANCE})"
          f"  -> {'PASS' if grid_ok else 'FAIL: 16px tiling visible'}")
    print(f"  bright fraction={bright:.3f} (max {BRIGHT_FRACTION_MAX})"
          f"  -> {'PASS' if zoning_ok else 'FAIL: value zoning broken'}")
    return 0 if (grid_ok and zoning_ok) else 1


if __name__ == "__main__":
    sys.exit(main())
