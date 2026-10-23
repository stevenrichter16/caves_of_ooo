"""Read-only native/geometry/contact verification; writes verification.json only."""
import hashlib
import json
from pathlib import Path
import subprocess

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
source = json.loads((HERE / "source-cells.json").read_text())
manifest = json.loads((HERE / "manifest.json").read_text())
atlas = Image.open(HERE / "north-vegetation.png").convert("RGBA")
contact = Image.open(HERE / "north-vegetation-contact.png").convert("RGBA")
assert atlas.size == (64, 24) and contact.size == (1280, 1152)
frames = source["frames"]
assert len(frames) == len(manifest["frames"]) == 4
assert tuple(f["blueprint"] for f in frames) == ("Tree", "Hedge", "BerryBush", "DryBrush")
assert tuple(f["id"] for f in frames) == ("tree", "hedge", "berry-bush", "dry-brush")
assert manifest["pixelsPerUnit"] == 16 and manifest["tilePivotNormalized"] == [0.5, 0.5]
assert manifest["filterMode"] == "Point" and manifest["wrapMode"] == "Clamp"
assert not manifest["mipmaps"] and manifest["compression"] == "None"
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
assert manifest["sha256"] == hashlib.sha256((HERE / "north-vegetation.png").read_bytes()).hexdigest()
palette = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in source["palette"].items()}
background = (3, 47, 35, 255)
checks = []
for i, f in enumerate(frames):
    assert f["index"] == i and f["id"] == manifest["frames"][i]["id"]
    assert manifest["frames"][i]["rect"] == [i * 16, 0, 16, 24]
    assert len(f["cells"]) == 24 and all(len(row) == 16 for row in f["cells"])
    colors, occupied = set(), []
    native_mismatches = contact_mismatches = 0
    for y, row in enumerate(f["cells"]):
        for x, value in enumerate(row):
            actual = atlas.getpixel((i * 16 + x, y))
            if value:
                expected = palette[value[1]]
                native_mismatches += actual != expected
                colors.add(actual)
                occupied.append((x, y))
            else:
                native_mismatches += actual[3] != 0
                expected = background
            left = (i * 40 + 12 + x) * 8
            top = (8 + y) * 8
            for sy in range(8):
                for sx in range(8):
                    contact_mismatches += contact.getpixel((left + sx, top + sy)) != expected
    assert native_mismatches == contact_mismatches == 0, f["id"]
    assert 3 <= len(colors) <= 6
    assert occupied
    if i != 1:
        assert max(y for x, y in occupied) == 22
        assert all(v is None for v in f["cells"][23])
        assert all(1 <= x <= 14 for x, y in occupied)
    bounds = [min(x for x, y in occupied), min(y for x, y in occupied),
              max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1]
    assert bounds == manifest["frames"][i]["alphaBoundsTopLeft"]
    assert {"%02X%02X%02X" % rgba[:3] for rgba in colors} == set(manifest["frames"][i]["palette"])
    checks.append({"frame": f["id"], "colors": len(colors), "opaquePixels": len(occupied),
                   "bounds": bounds, "height": bounds[3] - bounds[1],
                   "sourcePixelMismatches": native_mismatches, "contactPixelMismatches": contact_mismatches})


def occupied(index):
    return {(x, y) for y, row in enumerate(frames[index]["cells"]) for x, v in enumerate(row) if v}


def connected(points):
    remaining = set(points)
    queue = [remaining.pop()]
    while queue:
        x, y = queue.pop()
        for neighbor in ((x-1, y), (x+1, y), (x, y-1), (x, y+1)):
            if neighbor in remaining:
                remaining.remove(neighbor)
                queue.append(neighbor)
    return not remaining


assert all(connected(occupied(i)) for i in range(4)), "No detached decoration pixels."
assert len({json.dumps(f["cells"]) for f in frames}) == 4
assert checks[0]["height"] >= 21 and checks[2]["height"] <= 11
assert checks[3]["opaquePixels"] < checks[2]["opaquePixels"] < checks[1]["opaquePixels"]
assert all(v is None or not v[1].startswith("leaf") for row in frames[3]["cells"] for v in row), "Dry brush has no live leaves."
assert all(v is None or not v[1].startswith("berry") for row in frames[1]["cells"] for v in row), "Solid hedge does not promise berries."
assert sum(1 for row in frames[0]["cells"][15:22] for v in row if v and v[0] == "trunk") >= 12
hedge = occupied(1)
assert len(hedge) >= 300
horizontal_contacts = [y for y in range(24) if (0, y) in hedge and (15, y) in hedge]
vertical_contacts = [x for x in range(16) if (x, 0) in hedge and (x, 23) in hedge]
assert len(horizontal_contacts) >= 12 and len(vertical_contacts) >= 8
assert connected(hedge | {(x + 16, y) for x, y in hedge} | {(x, y + 24) for x, y in hedge})

# The exact geometry instances form the horizontal row, vertical column and
# elbow in the lower contact panel. Verify the pixels in each occupied cell.
preview_mismatches = 0
for px, py in manifest["hedgePreviewOrigins"]:
    for y, row in enumerate(frames[1]["cells"]):
        for x, value in enumerate(row):
            expected = palette[value[1]] if value else background
            left, top = (px + x) * 8, (144 - py - 24 + y) * 8
            for sy in range(8):
                for sx in range(8):
                    preview_mismatches += contact.getpixel((left + sx, top + sy)) != expected
assert preview_mismatches == 0

preserved = []
for relative in (
    "Assets/Art/SeedKeeper3/seed-keeper-3.png", "Assets/Art/Cook16x24/cook-16x24.png",
    "Assets/Art/Tatterjay16x24/tatterjay.png", "Assets/Art/ReedbackGrazer16x24/03-ridge.png",
    "Assets/Art/BeetleJar16x24/beetle-jar.png", "ArtSource/Live16x24/base/opening-base.png",
    "ArtSource/Live16x24/cast/opening-cast.png",
):
    current = (ROOT / relative).read_bytes()
    committed = subprocess.check_output(["git", "show", "HEAD:" + relative], cwd=ROOT)
    assert current == committed, "Prior accepted raster changed: " + relative
    preserved.append({"path": relative, "matchesHEAD": True, "sha256": hashlib.sha256(current).hexdigest()})
for group in ("anchors", "states", "motion", "items", "cellar"):
    prior = HERE.parent / group
    old_manifest = json.loads((prior / "manifest.json").read_text())
    image_path = prior / old_manifest["artifact"]
    digest = hashlib.sha256(image_path.read_bytes()).hexdigest()
    assert digest == old_manifest["sha256"], "Accepted group changed: " + group
    preserved.append({"path": str(image_path.relative_to(ROOT)), "matchesAcceptedManifest": True, "sha256": digest})

report = {
    "passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size),
    "alphaValues": [0, 255], "renderedFromEditableGeometry": True, "frames": checks,
    "hedgeHorizontalContacts": horizontal_contacts, "hedgeVerticalContacts": vertical_contacts,
    "hedgePreviewPixelMismatches": preview_mismatches,
    "priorRasters": preserved, "sha256": manifest["sha256"],
    "rgbaSha256": hashlib.sha256(atlas.tobytes()).hexdigest(),
    "reproducibility": "Native RGBA/source pixels are exact; Cycles PNG timing metadata may change whole-file SHA256 on rebuild.",
    "scope": "Read-only native/source/contact integrity and static connected-hedge checks. No Unity import, collision, owner routing, fog, animation or live acceptance claim.",
}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
