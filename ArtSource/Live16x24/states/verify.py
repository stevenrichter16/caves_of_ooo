"""Read-only native PNG/source/contact validation; no image creation or editing.

Run: python3 ArtSource/Live16x24/states/verify.py
Writes only verification.json after every assertion passes.
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
atlas = Image.open(HERE / "opening-states.png").convert("RGBA")
contact = Image.open(HERE / "opening-states-contact.png").convert("RGBA")
assert atlas.size == (192, 24) and contact.size == (1280, 1024)
assert len(source["frames"]) == len(manifest["frames"]) == 12
assert manifest["filterMode"] == "Point" and manifest["pixelsPerUnit"] == 16
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
assert manifest["sha256"] == hashlib.sha256((HERE / "opening-states.png").read_bytes()).hexdigest()
palette = {key: tuple(bytes.fromhex(value)) + (255,) for key, value in source["palette"].items()}
background = (3, 47, 35, 255)
frames = source["frames"]
checks = []
for i, frame in enumerate(frames):
    assert frame["index"] == i and frame["id"] == manifest["frames"][i]["id"]
    source_mismatches = contact_mismatches = 0
    colors, occupied = set(), []
    for y, row in enumerate(frame["cells"]):
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
    assert source_mismatches == contact_mismatches == 0, (frame["id"], source_mismatches, contact_mismatches)
    assert 3 <= len(colors) <= 6
    assert occupied and max(y for x, y in occupied) == 22
    assert {"%02X%02X%02X" % rgba[:3] for rgba in colors} == set(manifest["frames"][i]["palette"])
    checks.append({"frame": frame["id"], "colors": len(colors), "opaquePixels": len(occupied),
                   "sourcePixelMismatches": source_mismatches, "contactPixelMismatches": contact_mismatches})


def preserved_parts(first, others, predicate):
    coords = [(x, y) for y, row in enumerate(frames[first]["cells"]) for x, value in enumerate(row)
              if value and predicate(value[0])]
    assert coords
    for other in others:
        for x, y in coords:
            assert frames[first]["cells"][y][x] == frames[other]["cells"][y][x]
            assert atlas.getpixel((first * 16 + x, y)) == atlas.getpixel((other * 16 + x, y))
    return {"firstFrame": first, "otherFrames": others, "unchangedPartPixels": len(coords)}


shared = [
    preserved_parts(0, [1, 2], lambda part: part.startswith("post")),
    preserved_parts(3, [4, 5], lambda part: "stone" in part or part.endswith("edge")),
    preserved_parts(9, [10], lambda part: True),
]
for a, b in ((0, 1), (1, 2), (3, 4), (4, 5), (6, 7), (7, 8), (9, 10)):
    assert frames[a]["cells"] != frames[b]["cells"], "Nominal state variants must differ visibly."
assert all(frames[2]["cells"][y][x] is None for x in range(4, 11) for y in range(10, 23)), "Open wicket center must stay clear."
assert all(value is None or value[0] != "ember" for row in frames[3]["cells"] for value in row)
assert sum(value is not None and value[0] == "ember" for row in frames[4]["cells"] for value in row) == 3
assert all(value is None or not value[0].startswith("flame") for row in frames[4]["cells"] for value in row)
assert all(frames[3]["cells"][y][x] is None for y in range(17) for x in range(16))
assert any(value and value[0].startswith("flame") for row in frames[5]["cells"] for value in row)
assert all(value is None or not value[0].startswith("flame") for row in frames[9]["cells"] for value in row)

preserved = []
for relative in (
    "Assets/Art/SeedKeeper3/seed-keeper-3.png", "Assets/Art/Cook16x24/cook-16x24.png",
    "Assets/Art/Tatterjay16x24/tatterjay.png", "Assets/Art/ReedbackGrazer16x24/03-ridge.png",
    "Assets/Art/BeetleJar16x24/beetle-jar.png", "ArtSource/Live16x24/base/opening-base.png",
    "ArtSource/Live16x24/cast/opening-cast.png",
):
    current = (ROOT / relative).read_bytes()
    committed = subprocess.check_output(["git", "show", "HEAD:" + relative], cwd=ROOT)
    assert current == committed, "Earlier accepted raster differs from committed bytes: " + relative
    preserved.append({"path": relative, "matchesHEAD": True, "sha256": hashlib.sha256(current).hexdigest()})

report = {"passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size),
          "alphaValues": [0, 255], "renderedFromEditableGeometry": True,
          "frames": checks, "sharedStateParts": shared, "previousAcceptedRasters": preserved,
          "sha256": manifest["sha256"],
          "scope": "Read-only raster/source integrity, exact 8x contact, state silhouettes and prior-raster preservation. No Unity import, runtime routing or gameplay proof."}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
