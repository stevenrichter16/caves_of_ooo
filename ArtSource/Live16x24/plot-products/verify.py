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
atlas = Image.open(HERE / "plot-products.png").convert("RGBA")
contact = Image.open(HERE / "plot-products-contact.png").convert("RGBA")
assert atlas.size == (128, 24) and contact.size == (1280, 800)
frames = source["frames"]
expected = ("CandyCarrot", "CandyCarrotSeed", "EmberwheatSeed", "MarlrootClod",
            "MarlrootSeed", "PitchpodResin", "PitchpodSeed", "InertSludge")
assert tuple(f["blueprint"] for f in frames) == expected and len(manifest["frames"]) == 8
assert manifest["pixelsPerUnit"] == 16 and manifest["tilePivotNormalized"] == [0.5, 0.5]
assert manifest["filterMode"] == "Point" and manifest["wrapMode"] == "Clamp"
assert manifest["mipmaps"] is False and manifest["compression"] == "None"
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
assert manifest["sha256"] == hashlib.sha256((HERE / "plot-products.png").read_bytes()).hexdigest()
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
            top = (12 + (i // 4) * 40 + y) * 8
            for sy in range(8):
                for sx in range(8):
                    contact_bad += contact.getpixel((left + sx, top + sy)) != expected_color
    assert native_bad == contact_bad == 0, frame["id"]
    assert 3 <= len(colors) <= 6, (frame["id"], colors)
    assert all(1 <= x <= 14 and 0 <= y <= 22 for x, y in occupied)
    assert max(y for x, y in occupied) == 22 and all(v is None for v in frame["cells"][23])
    remaining = set(occupied)
    pending = [remaining.pop()]
    while pending:
        x, y = pending.pop()
        for neighbor in ((x-1,y), (x+1,y), (x,y-1), (x,y+1)):
            if neighbor in remaining:
                remaining.remove(neighbor)
                pending.append(neighbor)
    assert not remaining, (frame["id"], "detached pixels", remaining)
    bounds = [min(x for x, y in occupied), min(y for x, y in occupied), max(x for x, y in occupied) + 1, 23]
    assert bounds == manifest["frames"][i]["alphaBoundsTopLeft"]
    assert {"%02X%02X%02X" % rgba[:3] for rgba in colors} == set(manifest["frames"][i]["palette"])
    checks.append({"frame": frame["id"], "colors": len(colors), "opaquePixels": len(occupied), "bounds": bounds,
                   "height": bounds[3] - bounds[1], "width": bounds[2] - bounds[0],
                   "sourcePixelMismatches": native_bad, "contactPixelMismatches": contact_bad})

def part_cells(index, prefix):
    return {(x, y) for y, row in enumerate(frames[index]["cells"]) for x, v in enumerate(row)
            if v and v[0].startswith(prefix)}

assert 8 <= checks[0]["height"] <= 13 and checks[0]["width"] <= 9, "Gladroot is a detached tapered edible root."
assert part_cells(0, "root") and part_cells(0, "cut-leaf")
seed_masks = []
for i in (1, 2, 4, 6):
    assert 3 <= checks[i]["width"] <= 6 and 3 <= checks[i]["height"] <= 6
    assert 8 <= checks[i]["opaquePixels"] <= 30, "One compact kernel, not a packet or planted mark."
    assert all(v is None or v[0].startswith("seed") for row in frames[i]["cells"] for v in row)
    left, top = checks[i]["bounds"][:2]
    seed_masks.append(tuple((x - left, y - top) for y, row in enumerate(frames[i]["cells"])
                            for x, value in enumerate(row) if value))
assert len(set(seed_masks)) == 4, "Seed identities differ in silhouette as well as color."
assert part_cells(3, "root") and part_cells(3, "clay"), "Unprepared clod retains attached root strands."
assert 5 <= checks[3]["height"] <= 10 and checks[3]["width"] <= 12
assert part_cells(5, "resin") and checks[5]["height"] <= 8 and checks[5]["width"] <= 9
assert checks[7]["height"] <= 6 and checks[7]["width"] >= 8, "Stable compost is a low matte lump."
assert all(v is None or v[0].startswith("compost") for row in frames[7]["cells"] for v in row)
for index in (3, 5, 7):
    assert not any(any(word in v[0] for word in ("bottle", "vessel", "bucket", "flame", "glow", "leaf", "sprout"))
                   for row in frames[index]["cells"] for v in row if v), "Materials do not invent vessels, effects or living plants."
assert len({json.dumps(frame["cells"]) for frame in frames}) == 8

preserved = []
for relative in ("Assets/Art/SeedKeeper3/seed-keeper-3.png", "Assets/Art/Cook16x24/cook-16x24.png",
                 "Assets/Art/Tatterjay16x24/tatterjay.png", "Assets/Art/ReedbackGrazer16x24/03-ridge.png",
                 "Assets/Art/BeetleJar16x24/beetle-jar.png", "ArtSource/Live16x24/base/opening-base.png",
                 "ArtSource/Live16x24/cast/opening-cast.png"):
    current = (ROOT / relative).read_bytes()
    assert current == subprocess.check_output(["git", "show", "HEAD:" + relative], cwd=ROOT)
    preserved.append({"path": relative, "matchesHEAD": True, "sha256": hashlib.sha256(current).hexdigest()})
for group in ("anchors", "states", "motion", "items", "cellar", "north-vegetation", "north-works", "north-plot"):
    prior = HERE.parent / group
    old = json.loads((prior / "manifest.json").read_text())
    image_path = prior / old["artifact"]
    digest = hashlib.sha256(image_path.read_bytes()).hexdigest()
    assert digest == old["sha256"], group
    preserved.append({"path": str(image_path.relative_to(ROOT)), "matchesAcceptedManifest": True, "sha256": digest})

report = {"passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size), "alphaValues": [0, 255],
          "frames": checks, "distinctSeedSilhouettes": 4, "priorRasters": preserved,
          "sha256": manifest["sha256"], "rgbaSha256": hashlib.sha256(atlas.tobytes()).hexdigest(),
          "scope": "Static editable source/native/contact only; no Unity routing, drop, planting, compost or preparation claim.",
          "reproducibility": "Exact RGBA/source cells; Cycles timing metadata can change whole-file PNG hashes."}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
