"""Read-only native/source/contact checks; writes only verification.json.

Run after Blender --background --python Tools/Live16x24/build_cellar.py.
This validator never creates, edits, crops, resizes or samples art for authoring.
"""
import hashlib
import json
from pathlib import Path
import subprocess

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
source = json.loads((HERE / "source-cells.json").read_text())
manifest = json.loads((HERE / "manifest.json").read_text())
atlas = Image.open(HERE / "opening-cellar.png").convert("RGBA")
contact = Image.open(HERE / "opening-cellar-contact.png").convert("RGBA")
assert atlas.size == (144, 24) and contact.size == (1280, 1024)
assert len(source["frames"]) == len(manifest["frames"]) == 9
assert manifest["tilePivotNormalized"] == [0.5, 0.5]
assert manifest["pixelsPerUnit"] == 16 and manifest["filterMode"] == "Point"
assert manifest["wrapMode"] == "Clamp" and not manifest["mipmaps"]
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
assert manifest["sha256"] == hashlib.sha256((HERE / "opening-cellar.png").read_bytes()).hexdigest()
frames = source["frames"]
assert tuple(f["id"] for f in frames) == (
    "stairs-up", "crate", "timber-pallet", "sootroot-seed-stage",
    "sootroot-sprout", "sootroot-ripe", "sootroot-pulp", "sootroot-seed", "solid-stone-wall")
assert tuple(f["blueprint"] for f in frames) == (
    "StairsUp", "Crate", "GleanersTimberPallet", "SootrootCrop",
    "SootrootCrop", "SootrootCrop", "SootrootPulp", "SootrootSeed", "StoneWall")
palette = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in source["palette"].items()}
background = (3, 47, 35, 255)
checks = []
for i, f in enumerate(frames):
    assert f["index"] == i and f["id"] == manifest["frames"][i]["id"]
    assert manifest["frames"][i]["rect"] == [i * 16, 0, 16, 24]
    assert len(f["cells"]) == 24 and all(len(row) == 16 for row in f["cells"])
    colors, occupied = set(), []
    source_mismatches = contact_mismatches = 0
    for y, row in enumerate(f["cells"]):
        for x, value in enumerate(row):
            actual = atlas.getpixel((i * 16 + x, y))
            if value:
                expected = palette[value[1]]
                source_mismatches += actual != expected
                colors.add(actual)
                occupied.append((x, y))
            else:
                source_mismatches += actual[3] != 0
                expected = background
            left = (i % 4 * 40 + 12 + x) * 8
            top = (8 + i // 4 * 40 + y) * 8
            for sy in range(8):
                for sx in range(8):
                    contact_mismatches += contact.getpixel((left + sx, top + sy)) != expected
    assert source_mismatches == contact_mismatches == 0, (f["id"], source_mismatches, contact_mismatches)
    assert 3 <= len(colors) <= 6
    if i < 8:
        assert occupied and max(y for x, y in occupied) == 22
        assert all(v is None for v in f["cells"][23])
        assert all(1 <= x <= 14 for x, y in occupied), "Object side margins prevent adjacent silhouettes merging."
    else:
        assert len(occupied) == 16 * 24, "Terrain fills every pixel without false walking gaps."
    bounds = [min(x for x, y in occupied), min(y for x, y in occupied),
              max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1]
    assert bounds == manifest["frames"][i]["alphaBoundsTopLeft"]
    assert {"%02X%02X%02X" % rgba[:3] for rgba in colors} == set(manifest["frames"][i]["palette"])
    checks.append({"frame": f["id"], "colors": len(colors), "opaquePixels": len(occupied),
                   "bounds": bounds, "height": bounds[3] - bounds[1],
                   "sourcePixelMismatches": source_mismatches, "contactPixelMismatches": contact_mismatches})


def components(index):
    pending = {(x, y) for y, row in enumerate(frames[index]["cells"]) for x, v in enumerate(row) if v}
    result = []
    while pending:
        cell = pending.pop()
        queue, group = [cell], {cell}
        while queue:
            x, y = queue.pop()
            for neighbor in ((x-1, y), (x+1, y), (x, y-1), (x, y+1)):
                if neighbor in pending:
                    pending.remove(neighbor)
                    queue.append(neighbor)
                    group.add(neighbor)
        result.append(group)
    return result


assert len({json.dumps(f["cells"]) for f in frames}) == 9
assert checks[0]["height"] > checks[1]["height"] > checks[2]["height"]
assert all(frames[0]["cells"][y][7][0] == "tread" for y in (12, 16, 20)), "Three visible rising treads."
assert all(frames[2]["cells"][y][7] is None for y in (16, 19, 22)), "Pallet gaps and two separate runners."
assert checks[3]["height"] < checks[4]["height"] < checks[5]["height"]
assert not any(v and v[0].startswith("root-") for row in frames[4]["cells"] for v in row)
assert checks[6]["height"] <= 7 and checks[6]["opaquePixels"] <= 42
assert checks[7]["height"] <= 5 and checks[7]["opaquePixels"] <= 18
assert len(components(6)) == len(components(7)) == 1, "Loose products remain one object each."
assert all(len(components(i)) == 1 for i in range(9)), "No isolated decorative pixels."
assert all(frames[i]["stage"] == i - 3 for i in (3, 4, 5))
assert all(frames[i]["stage"] is None for i in (0, 1, 2, 6, 7, 8))
assert all(frames[8]["cells"][y][x][1] == "mortar" for y in (7, 15, 23) for x in range(16))
wall_preview_mismatches = 0
for repeat in range(3):
    for y, row in enumerate(frames[8]["cells"]):
        for x, value in enumerate(row):
            expected = palette[value[1]]
            for sy in range(8):
                for sx in range(8):
                    actual = contact.getpixel(((65 + repeat * 16 + x) * 8 + sx, (88 + y) * 8 + sy))
                    wall_preview_mismatches += actual != expected
assert wall_preview_mismatches == 0, "Repeated terrain preview is the same original geometry."

preserved = []
for relative in (
    "Assets/Art/SeedKeeper3/seed-keeper-3.png", "Assets/Art/Cook16x24/cook-16x24.png",
    "Assets/Art/Tatterjay16x24/tatterjay.png", "Assets/Art/ReedbackGrazer16x24/03-ridge.png",
    "Assets/Art/BeetleJar16x24/beetle-jar.png", "ArtSource/Live16x24/base/opening-base.png",
    "ArtSource/Live16x24/cast/opening-cast.png",
):
    current = (ROOT / relative).read_bytes()
    committed = subprocess.check_output(["git", "show", "HEAD:" + relative], cwd=ROOT)
    assert current == committed, "Prior accepted raster differs from HEAD: " + relative
    preserved.append({"path": relative, "matchesHEAD": True, "sha256": hashlib.sha256(current).hexdigest()})
for group in ("anchors", "states", "motion", "items"):
    prior = HERE.parent / group
    old_manifest = json.loads((prior / "manifest.json").read_text())
    image_path = prior / old_manifest["artifact"]
    digest = hashlib.sha256(image_path.read_bytes()).hexdigest()
    assert digest == old_manifest["sha256"], "Accepted group changed: " + group
    preserved.append({"path": str(image_path.relative_to(ROOT)), "matchesAcceptedManifest": True, "sha256": digest})
report = {
    "passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size),
    "alphaValues": [0, 255], "renderedFromEditableGeometry": True,
    "frames": checks, "priorRasters": preserved, "sha256": manifest["sha256"],
    "rgbaSha256": hashlib.sha256(atlas.tobytes()).hexdigest(),
    "reproducibility": "Exact native RGBA and source geometry are stable. Cycles embeds variable render-duration PNG text metadata, so whole-file SHA256 may change on rebuild; manifest always records the current file.",
    "structuralChecks": {"distinctFrames": 9, "connectedSilhouettes": 9,
                         "threeRisingTreads": True, "openPalletGaps": True,
                         "orderedGrowthHeights": True, "compactLooseProducts": True,
                         "opaqueMasonryPixels": 384, "repeatedWallPixelMismatches": wall_preview_mismatches},
    "scope": "Read-only source/native/8x-contact integrity, native silhouette checks and prior raster preservation. No Unity import, state routing, gameplay or live visibility claim.",
}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({"passed": True, "sha256": manifest["sha256"], "frames": checks}, indent=2))
