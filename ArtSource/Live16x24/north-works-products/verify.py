"""Independent read-only native/source/contact checks; writes a receipt only.

Run before authoring to record the expected missing-source RED, and after the
direct Blender geometry render for GREEN. No image is created or edited here.
"""
import hashlib
import json
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
STEM = "north-works-products"
EXPECTED = ("StoneburrSeed", "FrostLichen", "MendleafSprig", "WildBerries",
            "GoldCoin", "BrewedTonic", "ConcentratedMendleaf")
required = ("source-cells.json", "manifest.json", STEM + ".png",
            STEM + "-contact.png", STEM + ".blend")
missing = [name for name in required if not (HERE / name).is_file()]
assert not missing, "Expected source RED before authoring; missing: " + ", ".join(missing)

source = json.loads((HERE / "source-cells.json").read_text())
manifest = json.loads((HERE / "manifest.json").read_text())
atlas = Image.open(HERE / (STEM + ".png")).convert("RGBA")
contact = Image.open(HERE / (STEM + "-contact.png")).convert("RGBA")
assert atlas.size == (112, 24) and contact.size == (1280, 800)
assert tuple(frame["blueprint"] for frame in source["frames"]) == EXPECTED
assert len(manifest["frames"]) == len(EXPECTED)
assert manifest["pixelsPerUnit"] == 16 and manifest["tilePivotNormalized"] == [0.5, 0.5]
assert manifest["filterMode"] == "Point" and manifest["wrapMode"] == "Clamp"
assert manifest["mipmaps"] is False and manifest["compression"] == "None"
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
digest = hashlib.sha256((HERE / (STEM + ".png")).read_bytes()).hexdigest()
assert manifest["sha256"] == digest

palette = {name: tuple(bytes.fromhex(value)) + (255,) for name, value in source["palette"].items()}
background = (3, 47, 35, 255)
checks, masks = [], []
for i, frame in enumerate(source["frames"]):
    entry = manifest["frames"][i]
    assert frame["index"] == i and entry["id"] == frame["id"]
    assert entry["rect"] == [i * 16, 0, 16, 24]
    assert len(frame["cells"]) == 24 and all(len(row) == 16 for row in frame["cells"])
    colors, occupied = set(), set()
    native_bad = contact_bad = 0
    for y, row in enumerate(frame["cells"]):
        for x, value in enumerate(row):
            actual = atlas.getpixel((i * 16 + x, y))
            if value:
                expected = palette[value[1]]
                native_bad += actual != expected
                colors.add(actual)
                occupied.add((x, y))
            else:
                native_bad += actual[3] != 0
                expected = background
            left = ((i % 4) * 40 + 12 + x) * 8
            top = (12 + (i // 4) * 40 + y) * 8
            for sy in range(8):
                for sx in range(8):
                    contact_bad += contact.getpixel((left + sx, top + sy)) != expected
    assert native_bad == contact_bad == 0, (frame["id"], native_bad, contact_bad)
    assert 3 <= len(colors) <= 6, (frame["id"], colors)
    assert occupied and all(1 <= x <= 14 and 0 <= y <= 22 for x, y in occupied)
    assert max(y for x, y in occupied) == 22 and all(value is None for value in frame["cells"][23])
    pending = [next(iter(occupied))]
    remaining = occupied - set(pending)
    while pending:
        x, y = pending.pop()
        for neighbor in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if neighbor in remaining:
                remaining.remove(neighbor)
                pending.append(neighbor)
    assert not remaining, (frame["id"], "detached pixels", sorted(remaining))
    bounds = [min(x for x, y in occupied), min(y for x, y in occupied),
              max(x for x, y in occupied) + 1, 23]
    assert entry["alphaBoundsTopLeft"] == bounds
    assert set(entry["palette"]) == {"%02X%02X%02X" % value[:3] for value in colors}
    masks.append(tuple(sorted((x - bounds[0], y - bounds[1]) for x, y in occupied)))
    checks.append({"frame": frame["id"], "blueprint": EXPECTED[i], "colors": len(colors),
                   "opaquePixels": len(occupied), "bounds": bounds,
                   "width": bounds[2] - bounds[0], "height": bounds[3] - bounds[1],
                   "sourcePixelMismatches": native_bad, "contactPixelMismatches": contact_bad})
assert len(set(masks)) == 7, "Each loose identity needs a distinct outline, not only a recolor."

def parts(index):
    return {value[0] for row in source["frames"][index]["cells"] for value in row if value}

assert 4 <= checks[0]["width"] <= 7 and 4 <= checks[0]["height"] <= 7
assert all(name.startswith("shell") for name in parts(0)), "Stoneburr is a hard reagent shell, not a planted seed."
assert checks[1]["width"] <= 9 and checks[1]["height"] <= 10
assert all(name.startswith("frond") for name in parts(1)), "Loose lichen is not its wide living source crust."
assert any(name.startswith("leaf") for name in parts(2)) and any(name.startswith("cut-stem") for name in parts(2))
assert checks[2]["height"] <= 13 and checks[2]["width"] <= 10
assert checks[3]["width"] <= 10 and checks[3]["height"] <= 9
assert any(name.startswith("berry") for name in parts(3))
assert all(name.startswith(("berry", "stem")) for name in parts(3)), "No food container or cooked meal."
assert 5 <= checks[4]["width"] <= 8 and 4 <= checks[4]["height"] <= 8
assert all(name.startswith("coin") for name in parts(4)), "One currency identity; no stack quantity or bag."
assert 5 <= checks[5]["width"] <= 9 and 9 <= checks[5]["height"] <= 14
assert any(name.startswith("flask") for name in parts(5)) and any(name.startswith("stopper") for name in parts(5))
assert not any(any(word in name for word in ("heal", "heart", "flame", "fill", "liquid", "effect", "glow", "cross"))
               for name in parts(5)), "BrewedTonic is neutral opaque packaging, not an effect promise."
assert not any(value[1] in ("rust", "rust_light", "grain", "ochre")
               for row in source["frames"][5]["cells"] for value in row if value), "No healing-red or fire-gold payload palette."
assert checks[6]["height"] < checks[2]["height"] and checks[6]["width"] <= 9
assert all(name.startswith("pressed-leaf") for name in parts(6)), "Concentrated mendleaf is a compressed reagent, not a potion."

prior = []
for group in ("anchors", "states", "motion", "items", "cellar", "north-vegetation", "north-works", "north-plot", "plot-products"):
    folder = HERE.parent / group
    accepted = json.loads((folder / "manifest.json").read_text())
    image_path = folder / accepted["artifact"]
    actual = hashlib.sha256(image_path.read_bytes()).hexdigest()
    assert actual == accepted["sha256"], "Prior accepted atlas changed: " + group
    prior.append({"path": str(image_path.relative_to(ROOT)), "matchesAcceptedManifest": True, "sha256": actual})

report = {"passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size),
          "alphaValues": [0, 255], "frames": checks, "distinctNormalizedOutlines": 7,
          "priorRasters": prior, "sha256": digest,
          "rgbaSha256": hashlib.sha256(atlas.tobytes()).hexdigest(),
          "scope": "Static source/native/contact evidence only; no gameplay, import, owner routing or Play acceptance.",
          "reproducibility": "Exact RGBA/source cells; Cycles render timing metadata may alter whole-file PNG hashes."}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
