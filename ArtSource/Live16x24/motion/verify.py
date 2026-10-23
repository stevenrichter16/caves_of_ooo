"""Read-only PNG/source/contact checks. Writes verification.json, never images.

Run python3 ArtSource/Live16x24/motion/verify.py after the Blender generator.
This is artwork integrity evidence, not runtime or gameplay validation.
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
atlas = Image.open(HERE / "opening-motion.png").convert("RGBA")
contact = Image.open(HERE / "opening-motion-contact.png").convert("RGBA")
assert atlas.size == (256, 24) and contact.size == (1280, 1344)
assert len(source["frames"]) == len(manifest["frames"]) == 16
assert manifest["actorPivotNormalized"] == [0.5, 0]
assert manifest["pixelsPerUnit"] == 16 and manifest["filterMode"] == "Point"
assert set(atlas.getchannel("A").tobytes()) == {0, 255}
assert hashlib.sha256((HERE / "opening-motion.png").read_bytes()).hexdigest() == manifest["sha256"]
palette = {k: tuple(bytes.fromhex(v)) + (255,) for k, v in source["palette"].items()}
background = (3, 47, 35, 255)
frames = source["frames"]
actors = ("Player", "PetDog", "MarlbackScrabbler", "MarlbackGleaner")
poses = ("walk-a", "walk-b", "melee", "hurt")
assert [(f["actor"], f["pose"]) for f in frames] == [(a, p) for a in actors for p in poses]
base = json.loads((HERE.parent / "base/source-cells.json").read_text())
cast = json.loads((HERE.parent / "cast/source-cells.json").read_text())
idles = [base["frames"][0]["cells"]] + [cast["frames"][i]["cells"] for i in (1, 2, 3)]


def colored(cells):
    return {(x, y): palette[v[1]] for y, row in enumerate(cells) for x, v in enumerate(row) if v}


def normalized_pixels(cells):
    pixels = colored(cells)
    min_x, min_y = min(x for x, y in pixels), min(y for x, y in pixels)
    return {(x - min_x, y - min_y, rgba) for (x, y), rgba in pixels.items()}


def difference(a, b):
    a, b = colored(a), colored(b)
    return {
        "changedPixels": sum(a.get(p) != b.get(p) for p in set(a) | set(b)),
        "changedSilhouettePixels": len(set(a) ^ set(b)),
    }


checks = []
for i, frame in enumerate(frames):
    assert frame["index"] == i and frame["id"] == manifest["frames"][i]["id"]
    idle = idles[i // 4]
    assert frame["idleCells"] == idle, "Idle geometry must match the accepted source exactly."
    cells = frame["cells"]
    assert len(cells) == 24 and all(len(r) == 16 for r in cells)
    colors, occupied = set(), []
    source_mismatches = contact_mismatches = 0
    for y, row in enumerate(cells):
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
    assert 3 <= len(colors) <= 7
    assert colors <= set(colored(idle).values()), "No new palette colors are allowed in derivative poses."
    assert occupied and max(y for x, y in occupied) == 22
    assert all(v is None for v in cells[23]), "Keep the accepted one-pixel bottom margin."
    assert normalized_pixels(cells) != normalized_pixels(idle), "A rigidly translated idle is not an animation pose."
    diff = difference(cells, idle)
    assert diff["changedPixels"] >= 15 and diff["changedSilhouettePixels"] >= 8, (frame["id"], diff)
    checks.append({"frame": frame["id"], "colors": len(colors), "opaquePixels": len(occupied),
                   "sourcePixelMismatches": source_mismatches, "contactPixelMismatches": contact_mismatches,
                   "idleDifference": diff, "rigidIdleTranslation": False})

walk_checks = []
for actor_index, actor in enumerate(actors):
    group = frames[actor_index * 4:actor_index * 4 + 4]
    for a in range(4):
        for b in range(a + 1, 4):
            assert normalized_pixels(group[a]["cells"]) != normalized_pixels(group[b]["cells"]), "Each pose must be visibly unique."
    pair = difference(group[0]["cells"], group[1]["cells"])
    assert pair["changedSilhouettePixels"] >= 12, (actor, pair)
    # Actual alternation must reach the leg region; arm-only differences do
    # not qualify as two walking poses.
    feet_a, feet_b = colored(group[0]["cells"]), colored(group[1]["cells"])
    foot_change = len({p for p in set(feet_a) ^ set(feet_b) if p[1] >= 20})
    assert foot_change >= 6, (actor, foot_change)
    # Preserve each actor's identifying head/plate crown during both walks.
    idle = idles[actor_index]
    def is_identity(part):
        if actor == "Player":
            return "hood" in part or part.startswith("face")
        if actor == "PetDog":
            return part in ("head", "forehead", "ear", "ear-tip", "short-muzzle", "nose", "eye")
        return "plate" in part or "edge" in part or "shell" in part
    identity = [(x, y, palette[v[1]]) for y, row in enumerate(idle) for x, v in enumerate(row)
                if v and is_identity(v[0])]
    for f in group[:2]:
        pixels = colored(f["cells"])
        assert all(pixels.get((x, y)) == rgba for x, y, rgba in identity)
    walk_checks.append({"actor": actor, **pair, "changedLegSilhouettePixels": foot_change,
                        "preservedIdentityPixels": len(identity)})

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

report = {
    "passed": True, "nativeSize": list(atlas.size), "contactSize": list(contact.size),
    "alphaValues": [0, 255], "renderedFromEditableGeometry": True, "frames": checks,
    "walkPairChecks": walk_checks, "previousAcceptedRasters": preserved, "sha256": manifest["sha256"],
    "scope": "Read-only raster/source integrity, exact 8x contact, real limb/silhouette changes, original idle identity/palette preservation and baseline. No Unity import, animation timing, hook behavior, fog or gameplay proof.",
}
(HERE / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({"passed": True, "sha256": manifest["sha256"], "walkPairChecks": walk_checks}, indent=2))
