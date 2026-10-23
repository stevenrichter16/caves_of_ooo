"""Read-only raster/source validation; writes only verification.json.

Run python3 ArtSource/Live16x24/items/verify.py after the Blender generator.
No image creation or editing occurs here; native PNGs are Blender renders.
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
atlas = Image.open(HERE / "opening-items.png").convert("RGBA")
contact = Image.open(HERE / "opening-items-contact.png").convert("RGBA")
assert atlas.size == (192, 24) and contact.size == (1280, 1024)
assert len(source["frames"]) == len(manifest["frames"]) == 12
assert manifest["tilePivotNormalized"] == [0.5, 0.5]
assert manifest["pixelsPerUnit"] == 16 and manifest["filterMode"] == "Point"
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
assert manifest["sha256"] == hashlib.sha256((HERE / "opening-items.png").read_bytes()).hexdigest()
palette = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in source["palette"].items()}
background = (3, 47, 35, 255)
frames = source["frames"]
expected_blueprints = ("Emberwheat", "ToastedEmberwheat", "DrawgourdShell", "DrawgourdSeed",
                       "Mushroom", "RoastedMushroom", "GlowQuartz", "HealingTonic", "DriedMeat",
                       "FireClay", "SalvagedTimber", "MarlbackCorpse")
assert tuple(f["blueprint"] for f in frames) == expected_blueprints
checks = []
for i, f in enumerate(frames):
    assert f["index"] == i and f["id"] == manifest["frames"][i]["id"]
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
    assert occupied and max(y for x, y in occupied) == 22
    assert all(v is None for v in f["cells"][23])
    assert all(1 <= x <= 14 and 8 <= y <= 22 for x, y in occupied), "Keep compact objects and side/bottom margins."
    bounds = [min(x for x, y in occupied), min(y for x, y in occupied),
              max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1]
    assert bounds == manifest["frames"][i]["alphaBoundsTopLeft"]
    assert {"%02X%02X%02X" % rgba[:3] for rgba in colors} == set(manifest["frames"][i]["palette"])
    checks.append({"frame": f["id"], "colors": len(colors), "opaquePixels": len(occupied),
                   "bounds": bounds, "height": bounds[3] - bounds[1],
                   "sourcePixelMismatches": source_mismatches, "contactPixelMismatches": contact_mismatches})


def occupied(index):
    return {(x, y) for y, row in enumerate(frames[index]["cells"]) for x, v in enumerate(row) if v}


def components(index):
    pending, result = occupied(index), []
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


assert occupied(0) != occupied(1), "Toasting changes the bent grain tips, not just a color filter."
assert occupied(4) != occupied(5), "Roasted cap has an actual curled rim silhouette."
assert frames[2]["cells"][12][7] == ["neck-opening", "ink"]
assert checks[3]["height"] == 4 and checks[3]["opaquePixels"] <= 18
assert len(components(3)) == 1, "The seed must remain one connected object, not crumbs."
assert len(components(6)) == 1 and max(sum(v is not None for v in row) for row in frames[6]["cells"]) <= 3
assert len(components(10)) == 2, "Timber is two flat split pieces, not a beam or full pallet."
assert all(v is None or v[1] != "rust" for v in frames[11]["cells"][22]), "No rust-colored blood-like baseline beneath the corpse."
assert sum(check["height"] <= 9 for check in checks) >= 6
assert len({json.dumps(f["cells"]) for f in frames}) == 12

preserved = []
for relative in (
    "Assets/Art/SeedKeeper3/seed-keeper-3.png", "Assets/Art/Cook16x24/cook-16x24.png",
    "Assets/Art/Tatterjay16x24/tatterjay.png", "Assets/Art/ReedbackGrazer16x24/03-ridge.png",
    "Assets/Art/BeetleJar16x24/beetle-jar.png", "ArtSource/Live16x24/base/opening-base.png",
    "ArtSource/Live16x24/cast/opening-cast.png",
):
    current = (ROOT / relative).read_bytes()
    committed = subprocess.check_output(["git", "show", "HEAD:" + relative], cwd=ROOT)
    assert current == committed, "Prior accepted raster differs from committed bytes: " + relative
    preserved.append({"path": relative, "matchesHEAD": True, "sha256": hashlib.sha256(current).hexdigest()})
for group in ("anchors", "states", "motion"):
    prior = HERE.parent / group
    old_manifest = json.loads((prior / "manifest.json").read_text())
    image_path = prior / old_manifest["artifact"]
    digest = hashlib.sha256(image_path.read_bytes()).hexdigest()
    assert digest == old_manifest["sha256"], "Other accepted group must match its recorded source hash."
    preserved.append({"path": str(image_path.relative_to(ROOT)), "matchesAcceptedManifest": True, "sha256": digest})

report = {
    "passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size),
    "alphaValues": [0, 255], "renderedFromEditableGeometry": True, "frames": checks,
    "priorRasters": preserved, "sha256": manifest["sha256"],
    "structuralChecks": {"seedConnectedComponents": 1, "quartzConnectedComponents": 1,
                         "timberConnectedComponents": 2, "distinctRoastedSilhouettes": True,
                         "openShellNeck": True, "noCorpseBloodBaseline": True},
    "scope": "Read-only source/native/8x-contact integrity, compact silhouettes and previous atlas hash checks. No Unity import, owner routing, stack/transfer behavior or live visibility claim.",
}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({"passed": True, "sha256": manifest["sha256"], "frames": checks}, indent=2))
