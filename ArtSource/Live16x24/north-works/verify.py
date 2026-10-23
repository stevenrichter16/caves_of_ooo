"""Read-only native/source/contact checks. Writes verification.json only."""
import hashlib
import json
from pathlib import Path
import subprocess

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
source = json.loads((HERE / "source-cells.json").read_text())
manifest = json.loads((HERE / "manifest.json").read_text())
atlas = Image.open(HERE / "north-works.png").convert("RGBA")
contact = Image.open(HERE / "north-works-contact.png").convert("RGBA")
assert atlas.size == (144, 24) and contact.size == (960, 1184)
frames = source["frames"]
expected = ("AlchemyStill", "AlchemyShelf", "StoneburrPatch", "FrostLichenPatch",
            "MendleafPlant", "Magpie", "Viper", "MarlbackPatchbearer", "MarlbackPatchbearer")
assert len(frames) == len(manifest["frames"]) == 9
assert tuple(f["blueprint"] for f in frames) == expected
assert manifest["pixelsPerUnit"] == 16 and manifest["tilePivotNormalized"] == [0.5, 0.5]
assert manifest["filterMode"] == "Point" and manifest["wrapMode"] == "Clamp"
assert manifest["mipmaps"] is False and manifest["compression"] == "None"
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
assert manifest["sha256"] == hashlib.sha256((HERE / "north-works.png").read_bytes()).hexdigest()
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
            left = ((i % 3) * 40 + 12 + x) * 8
            top = (12 + (i // 3) * 44 + y) * 8
            for sy in range(8):
                for sx in range(8):
                    contact_bad += contact.getpixel((left + sx, top + sy)) != expected_color
    assert native_bad == contact_bad == 0, frame["id"]
    assert 3 <= len(colors) <= 6, (frame["id"], colors)
    assert all(1 <= x <= 14 and 0 <= y <= 22 for x, y in occupied)
    assert max(y for x, y in occupied) == 22 and all(v is None for v in frame["cells"][23])
    bounds = [min(x for x, y in occupied), min(y for x, y in occupied), max(x for x, y in occupied) + 1, 23]
    assert bounds == manifest["frames"][i]["alphaBoundsTopLeft"]
    assert {"%02X%02X%02X" % rgba[:3] for rgba in colors} == set(manifest["frames"][i]["palette"])
    remaining = set(occupied)
    pending = [remaining.pop()]
    while pending:
        x, y = pending.pop()
        for neighbor in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if neighbor in remaining:
                remaining.remove(neighbor)
                pending.append(neighbor)
    assert not remaining, (frame["id"], "detached pixels", remaining)
    checks.append({"frame": frame["id"], "colors": len(colors), "opaquePixels": len(occupied), "bounds": bounds,
                   "height": bounds[3] - bounds[1], "sourcePixelMismatches": native_bad, "contactPixelMismatches": contact_bad})

def part_cells(index, prefix):
    return {(x, y) for y, row in enumerate(frames[index]["cells"]) for x, v in enumerate(row) if v and v[0].startswith(prefix)}

assert part_cells(0, "condenser") and part_cells(0, "receiver"), "Still needs both functional shapes."
assert not any("flame" in v[0] or "liquid" in v[0] for row in frames[0]["cells"] for v in row if v)
assert all(frames[1]["cells"][y][x] is None for y in (8, 9, 10, 15, 16, 17) for x in range(4, 12)), "Shelf compartments must be empty."
assert part_cells(2, "burr-hook") and len(part_cells(2, "burr-hook")) >= 6
assert checks[3]["height"] <= 7 and checks[3]["bounds"][2] - checks[3]["bounds"][0] >= 12, "Lichen is a low crust."
assert len(part_cells(4, "silver-vein")) >= 6
assert checks[4]["height"] <= 13, "Mendleaf should read as low broad foliage, not a tall forked stalk."
assert len(part_cells(5, "tail")) >= 10 and checks[5]["height"] <= 15
assert checks[6]["height"] <= 12 and part_cells(6, "head"), "Viper stays a low snake."
assert not any("poison" in v[0] or "leg" in v[0] for row in frames[6]["cells"] for v in row if v)
assert part_cells(7, "patched-strap") == part_cells(8, "patched-strap")
assert part_cells(7, "healing-bottle") and not part_cells(8, "healing-bottle")
changes = [(x, y) for y in range(24) for x in range(16) if frames[7]["cells"][y][x] != frames[8]["cells"][y][x]]
assert 4 <= len(changes) <= 16 and all(10 <= x <= 13 and 8 <= y <= 12 for x, y in changes), changes
assert all(frames[7]["cells"][y][x] == frames[8]["cells"][y][x] for y in range(13, 24) for x in range(16)), "No stance/hand change between medicine states."
assert len({json.dumps(frame["cells"]) for frame in frames}) == 9

preserved = []
for relative in ("Assets/Art/SeedKeeper3/seed-keeper-3.png", "Assets/Art/Cook16x24/cook-16x24.png",
                 "Assets/Art/Tatterjay16x24/tatterjay.png", "Assets/Art/ReedbackGrazer16x24/03-ridge.png",
                 "Assets/Art/BeetleJar16x24/beetle-jar.png", "ArtSource/Live16x24/base/opening-base.png",
                 "ArtSource/Live16x24/cast/opening-cast.png"):
    current = (ROOT / relative).read_bytes()
    assert current == subprocess.check_output(["git", "show", "HEAD:" + relative], cwd=ROOT)
    preserved.append({"path": relative, "matchesHEAD": True, "sha256": hashlib.sha256(current).hexdigest()})
for group in ("anchors", "states", "motion", "items", "cellar", "north-vegetation"):
    prior = HERE.parent / group
    old = json.loads((prior / "manifest.json").read_text())
    image_path = prior / old["artifact"]
    digest = hashlib.sha256(image_path.read_bytes()).hexdigest()
    assert digest == old["sha256"], group
    preserved.append({"path": str(image_path.relative_to(ROOT)), "matchesAcceptedManifest": True, "sha256": digest})

report = {"passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size), "alphaValues": [0,255],
          "frames": checks, "medicineSlotChangedPixels": changes, "priorRasters": preserved,
          "sha256": manifest["sha256"], "rgbaSha256": hashlib.sha256(atlas.tobytes()).hexdigest(),
          "scope": "Static editable source/native/contact verification only; no Unity, owner routing, animation, gameplay or performance claim.",
          "reproducibility": "Exact RGBA/source cells; Cycles timing metadata can change whole-file PNG hashes."}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
