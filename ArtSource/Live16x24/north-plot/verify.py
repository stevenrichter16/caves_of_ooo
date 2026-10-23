"""Read-only source/native/contact checks; writes verification.json only."""
import hashlib
import json
from pathlib import Path
import subprocess

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
source = json.loads((HERE / "source-cells.json").read_text())
manifest = json.loads((HERE / "manifest.json").read_text())
atlas = Image.open(HERE / "north-plot.png").convert("RGBA")
contact = Image.open(HERE / "north-plot-contact.png").convert("RGBA")
assert atlas.size == (128, 24) and contact.size == (1280, 1152)
frames = source["frames"]
expected = ("candycarrot-sprout", "emberwheat-sprout", "marlroot-sprout", "marlroot-ripe",
            "pitchpod-sprout", "pitchpod-ripe", "reserve-tray", "reserve-cord")
assert tuple(f["id"] for f in frames) == expected and len(manifest["frames"]) == 8
assert manifest["pixelsPerUnit"] == 16 and manifest["tilePivotNormalized"] == [0.5, 0.5]
assert manifest["filterMode"] == "Point" and manifest["wrapMode"] == "Clamp"
assert manifest["mipmaps"] is False and manifest["compression"] == "None"
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
assert manifest["sha256"] == hashlib.sha256((HERE / "north-plot.png").read_bytes()).hexdigest()
palette = {name: tuple(bytes.fromhex(value)) + (255,) for name, value in source["palette"].items()}
background = (3, 47, 35, 255)
checks = []
for i, frame in enumerate(frames):
    assert frame["index"] == i and frame["id"] == manifest["frames"][i]["id"]
    assert manifest["frames"][i]["rect"] == [i * 16, 0, 16, 24]
    assert len(frame["cells"]) == 24 and all(len(row) == 16 for row in frame["cells"])
    colors, occupied = set(), set()
    native_bad = contact_bad = 0
    for y, row in enumerate(frame["cells"]):
        for x, value in enumerate(row):
            actual = atlas.getpixel((i * 16 + x, y))
            if value:
                expected_color = palette[value[1]]
                native_bad += actual != expected_color
                colors.add(actual)
                occupied.add((x, y))
            else:
                native_bad += actual[3] != 0
                expected_color = background
            left = ((i % 4) * 40 + 12 + x) * 8
            top = (12 + (i // 4) * 44 + y) * 8
            for sy in range(8):
                for sx in range(8):
                    contact_bad += contact.getpixel((left + sx, top + sy)) != expected_color
    assert native_bad == contact_bad == 0, frame["id"]
    assert 3 <= len(colors) <= 6, (frame["id"], colors)
    assert all(1 <= x <= 14 and 0 <= y <= 22 for x, y in occupied)
    assert all(v is None for v in frame["cells"][23])
    if i != 7:
        assert max(y for x, y in occupied) == 22
        remaining = set(occupied)
        pending = [remaining.pop()]
        while pending:
            x, y = pending.pop()
            for neighbor in ((x-1,y), (x+1,y), (x,y-1), (x,y+1)):
                if neighbor in remaining:
                    remaining.remove(neighbor)
                    pending.append(neighbor)
        assert not remaining, (frame["id"], "detached pixels", remaining)
    bounds = [min(x for x, y in occupied), min(y for x, y in occupied),
              max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1]
    assert bounds == manifest["frames"][i]["alphaBoundsTopLeft"]
    assert {"%02X%02X%02X" % rgba[:3] for rgba in colors} == set(manifest["frames"][i]["palette"])
    checks.append({"frame": frame["id"], "colors": len(colors), "opaquePixels": len(occupied), "bounds": bounds,
                   "height": bounds[3] - bounds[1], "sourcePixelMismatches": native_bad, "contactPixelMismatches": contact_bad})

def parts(index, prefix):
    return {(x, y) for y, row in enumerate(frames[index]["cells"]) for x, v in enumerate(row)
            if v and v[0].startswith(prefix)}

assert checks[0]["height"] <= 9 and len(parts(0, "leaf")) >= 15
assert not any("root" in v[0] or "fruit" in v[0] for row in frames[0]["cells"] for v in row if v)
assert checks[1]["height"] >= 11 and len(parts(1, "stalk")) >= 8
assert not any("kernel" in v[0] or "ripe" in v[0] for row in frames[1]["cells"] for v in row if v)
assert not parts(2, "clay") and len(parts(3, "clay")) >= 12
assert checks[2]["height"] < checks[3]["height"]
assert not parts(4, "pod") and not parts(4, "amber")
assert len(parts(5, "pod")) >= 12 and 1 <= len(parts(5, "amber")) <= 4
assert checks[4]["height"] < checks[5]["height"]
assert checks[6]["height"] <= 9 and len(parts(6, "cord-loop")) >= 6
assert all(not v or v[0].startswith(("tray", "cord-loop")) for row in frames[6]["cells"] for v in row)
assert all(frames[6]["cells"][y][x] is None for y in (17, 18) for x in range(5, 11)), "No painted tray stock."
assert 16 <= checks[7]["opaquePixels"] <= 40, "Cord must stay sparse, not a fence."
assert all(frames[7]["cells"][y][x] is None for y in range(6, 19) for x in range(4, 12)), "Transparent cord center."
assert all(v is None for v in frames[7]["cells"][22]), "Cord is ground decoration, not an upright foot."
cord_quadrant_counts = [sum(1 for y in ys for x in xs if frames[7]["cells"][y][x])
                        for ys in (range(12), range(12, 24)) for xs in (range(8), range(8, 16))]
assert len(set(cord_quadrant_counts)) >= 3, "Physical twine runs must not read as four identical target brackets."
iteration = json.loads((HERE / "cord-pass2.json").read_text())
current_frame_hashes = [hashlib.sha256(json.dumps(f["cells"], sort_keys=True).encode()).hexdigest() for f in frames]
assert current_frame_hashes[:7] == iteration["beforeFrameCellSha256"][:7], "Cord refinement must leave seven approved object frames unchanged."
assert len({json.dumps(frame["cells"]) for frame in frames}) == 8

# Four independently rendered compositing examples: bare dry/wet ground, then
# the two mature crops. Geometry is rendered again; no preview uses a raster.
preview_bad = 0
previews = (("45341F", None), ("252319", None), ("45341F", 3), ("252319", 5))
for i, (soil, crop) in enumerate(previews):
    ground = tuple(bytes.fromhex(soil)) + (255,)
    for y in range(24):
        for x in range(16):
            value = frames[7]["cells"][y][x]
            expected_color = palette[value[1]] if value else ground
            if crop is not None and frames[crop]["cells"][y][x]:
                expected_color = palette[frames[crop]["cells"][y][x][1]]
            left, top = (i * 40 + 12 + x) * 8, (108 + y) * 8
            for sy in range(8):
                for sx in range(8):
                    preview_bad += contact.getpixel((left + sx, top + sy)) != expected_color
assert preview_bad == 0, preview_bad

preserved = []
for relative in ("Assets/Art/SeedKeeper3/seed-keeper-3.png", "Assets/Art/Cook16x24/cook-16x24.png",
                 "Assets/Art/Tatterjay16x24/tatterjay.png", "Assets/Art/ReedbackGrazer16x24/03-ridge.png",
                 "Assets/Art/BeetleJar16x24/beetle-jar.png", "ArtSource/Live16x24/base/opening-base.png",
                 "ArtSource/Live16x24/cast/opening-cast.png"):
    current = (ROOT / relative).read_bytes()
    assert current == subprocess.check_output(["git", "show", "HEAD:" + relative], cwd=ROOT)
    preserved.append({"path": relative, "matchesHEAD": True, "sha256": hashlib.sha256(current).hexdigest()})
for group in ("anchors", "states", "motion", "items", "cellar", "north-vegetation", "north-works"):
    prior = HERE.parent / group
    old = json.loads((prior / "manifest.json").read_text())
    image_path = prior / old["artifact"]
    digest = hashlib.sha256(image_path.read_bytes()).hexdigest()
    assert digest == old["sha256"], group
    preserved.append({"path": str(image_path.relative_to(ROOT)), "matchesAcceptedManifest": True, "sha256": digest})

report = {"passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size), "alphaValues": [0, 255],
          "frames": checks, "soilCropCompositeMismatches": preview_bad, "priorRasters": preserved,
          "cordQuadrantPixelCounts": cord_quadrant_counts, "cordRefinementPreservedObjectFrames": 7,
          "sha256": manifest["sha256"], "rgbaSha256": hashlib.sha256(atlas.tobytes()).hexdigest(),
          "scope": "Static editable source/native/contact only; no runtime routing, ownership, fog, growth or interaction claim.",
          "reproducibility": "Exact RGBA/source cells; Cycles timing metadata can change whole-file PNG hashes."}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
