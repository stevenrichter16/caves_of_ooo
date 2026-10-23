"""Read-only PNG/source checks. Does not create or alter any raster image.

Run: python3 ArtSource/Live16x24/anchors/verify.py
Writes only verification.json after all assertions pass.
"""
import hashlib
import json
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
source = json.loads((HERE / "source-cells.json").read_text())
manifest = json.loads((HERE / "manifest.json").read_text())
atlas = Image.open(HERE / "opening-anchors.png").convert("RGBA")
contact = Image.open(HERE / "opening-anchors-contact.png").convert("RGBA")
assert atlas.size == (192, 24)
assert contact.size == (1280, 1024)
assert len(source["frames"]) == len(manifest["frames"]) == 12
assert manifest["filterMode"] == "Point" and manifest["pixelsPerUnit"] == 16
assert manifest["sha256"] == hashlib.sha256((HERE / "opening-anchors.png").read_bytes()).hexdigest()
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
palette = {key: tuple(bytes.fromhex(value)) + (255,) for key, value in source["palette"].items()}
background = (3, 47, 35, 255)
checks = []
for i, frame in enumerate(source["frames"]):
    assert frame["index"] == i and frame["id"] == manifest["frames"][i]["id"]
    source_mismatches = contact_mismatches = 0
    colors = set()
    occupied = []
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
            # Compare every actual 8x screen pixel, not only the block center.
            left = (i % 4 * 40 + 12 + x) * 8
            top = (8 + i // 4 * 40 + y) * 8
            for sy in range(8):
                for sx in range(8):
                    contact_mismatches += contact.getpixel((left + sx, top + sy)) != expected
    assert source_mismatches == contact_mismatches == 0, (frame["id"], source_mismatches, contact_mismatches)
    assert 3 <= len(colors) <= 7 and occupied
    assert any(y == 22 for x, y in occupied)
    assert {"%02X%02X%02X" % rgba[:3] for rgba in colors} == set(manifest["frames"][i]["palette"])
    checks.append({"frame": frame["id"], "colors": len(colors), "opaquePixels": len(occupied),
                   "sourcePixelMismatches": source_mismatches, "contactPixelMismatches": contact_mismatches})

pairs = []
for left, right in [(0, 1), (2, 3)]:
    differences = []
    for y in range(24):
        for x in range(16):
            a, b = atlas.getpixel((left * 16 + x, y)), atlas.getpixel((right * 16 + x, y))
            assert a[3] == b[3], "State variants must preserve their silhouette."
            if a != b:
                differences.append([x, y])
    assert differences, "A nominal state pair must differ visibly."
    pairs.append({"frames": [left, right], "sameAlphaSilhouette": True, "changedPixels": len(differences)})
assert all(atlas.getpixel((11 * 16 + x, y))[3] == 255 for x in [3, 12] for y in range(24))
report = {"passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size),
          "alphaValues": [0, 255], "renderedFromEditableGeometry": True,
          "frames": checks, "statePairs": pairs, "verticalWallEdgesContinuous": True,
          "sha256": manifest["sha256"],
          "scope": "Read-only raster integrity, authored geometry match, exact 8x contact and state masks. No Unity gameplay or runtime import proof."}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
