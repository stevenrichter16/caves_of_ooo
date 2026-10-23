"""Build four native vegetation cells from original editable Blender geometry.

Run Blender --background --python Tools/Live16x24/build_north_vegetation.py.
Writes only ArtSource/Live16x24/north-vegetation. Concept images are reference
only; no raster is read, sampled, resized or edited. The accepted base generator
supplies camera/material helpers, while every new cell is original integer mesh.
"""
import hashlib
import json
from pathlib import Path
import sys

import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import add_label, add_quad, camera_for, emission_material

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/north-vegetation"
WIDTH, HEIGHT = 16, 24
PALETTE = {
    "wood_dark": "5F4830", "wood": "94713D", "cut": "B5A47E",
    "leaf_dark": "435A35", "leaf_mid": "53683E", "leaf": "75834B", "leaf_light": "96A462",
    "berry": "9B442B", "berry_edge": "B85834", "cream": "E4D0A0",
}
HEDGE_PREVIEWS = [[12 + i * 16, 76] for i in range(4)] + [[92, y] for y in (12, 36, 60)] + [[128, 60], [128, 36], [112, 36]]


class Frame:
    def __init__(self, name, blueprint, description, use, limits, boundary=False):
        self.name, self.blueprint, self.description = name, blueprint, description
        self.use, self.limits, self.boundary = use, limits, boundary
        self.cells = [[None] * WIDTH for _ in range(HEIGHT)]

    def rect(self, part, color, x, y, width, height):
        assert color in PALETTE and width > 0 and height > 0
        assert 0 <= x < x + width <= WIDTH and 0 <= y < y + height <= HEIGHT
        for row in range(y, y + height):
            for col in range(x, x + width):
                self.cells[row][col] = (part, color)
        return self

    def dot(self, part, color, x, y):
        return self.rect(part, color, x, y, 1, 1)


def make_frames():
    frames = []
    p = Frame("tree", "Tree",
              "Tall branching trunk beneath three broad angular leaf masses; visible roots and open space beside the trunk distinguish solid tree from low passable vegetation.",
              "Existing ordinary Tree owns solid physics, destructibility and plant/wood thermal material.",
              "No tree harvest, hidden fruit, new shadow, oversized canopy or cross-cell body. Damage, fire and removal remain simulation state.")
    p.rect("trunk", "wood_dark", 7, 9, 3, 13)
    p.rect("trunk", "wood", 7, 13, 2, 9)
    p.rect("left-root", "wood_dark", 5, 22, 4, 1)
    p.rect("right-root", "wood_dark", 9, 22, 3, 1)
    p.dot("root-cut", "wood", 5, 22)
    p.rect("left-fork", "wood_dark", 4, 10, 2, 7)
    p.rect("left-fork", "wood_dark", 5, 16, 3, 2)
    p.rect("left-fork-face", "wood", 5, 12, 1, 4)
    p.rect("right-fork", "wood_dark", 11, 11, 2, 5)
    p.rect("right-fork", "wood_dark", 9, 15, 3, 2)
    p.rect("left-crown", "leaf_dark", 2, 5, 5, 8)
    p.rect("left-crown", "leaf_dark", 1, 7, 2, 4)
    p.rect("left-crown-face", "leaf_mid", 2, 6, 4, 5)
    p.rect("left-crown-fold", "leaf", 2, 6, 3, 2)
    p.rect("right-crown", "leaf_dark", 9, 5, 5, 8)
    p.rect("right-crown", "leaf_dark", 13, 7, 2, 4)
    p.rect("right-crown-face", "leaf_mid", 10, 6, 4, 5)
    p.rect("right-crown-fold", "leaf", 10, 6, 3, 2)
    p.rect("upper-crown", "leaf_dark", 5, 2, 6, 8)
    p.rect("upper-crown", "leaf_dark", 6, 1, 3, 1)
    p.rect("upper-crown-face", "leaf_mid", 5, 3, 5, 5)
    p.rect("upper-crown-fold", "leaf", 6, 2, 3, 3)
    frames.append(p)

    p = Frame("hedge", "Hedge",
              "Dense woven bramble with broad matte leaf clusters and short visible brown stems. Both axes meet at leafy boundaries so a repeated row, column or elbow reads solid.",
              "Existing ordinary solid Hedge; one cached frame does not depend on neighbor discovery or allocate adjacency variants.",
              "Boundary exception: leaf contacts touch all four cell edges including row23. Corner notches are one-pixel-scale leaf breaks, never walkable-looking lanes. No berries or invented fence posts.", True)
    p.rect("bramble-mass", "leaf_dark", 1, 1, 14, 22)
    p.rect("top-contact", "leaf_dark", 3, 0, 10, 1)
    p.rect("bottom-contact", "leaf_dark", 3, 23, 10, 1)
    p.rect("left-contact", "leaf_dark", 0, 3, 1, 18)
    p.rect("right-contact", "leaf_dark", 15, 3, 1, 18)
    # Deliberate large flat leaves, not per-pixel texture or noisy checkerboard.
    for x, y, w, h in ((1, 2, 5, 5), (8, 1, 5, 5), (5, 8, 5, 5),
                       (0, 12, 4, 5), (11, 10, 5, 6), (2, 18, 6, 5), (10, 19, 5, 4)):
        p.rect("leaf-mass", "leaf_mid", x, y, w, h)
    for x, y, w, h in ((2, 2, 3, 2), (9, 2, 3, 2), (6, 8, 3, 2),
                       (0, 13, 2, 2), (12, 11, 3, 2), (3, 19, 3, 2), (11, 20, 3, 2)):
        p.rect("leaf-fold", "leaf", x, y, w, h)
    p.rect("lower-weave", "wood_dark", 1, 17, 4, 1)
    p.rect("lower-weave", "wood_dark", 4, 15, 1, 3)
    p.rect("upper-weave", "wood_dark", 6, 4, 1, 4)
    p.rect("upper-weave", "wood_dark", 7, 7, 5, 1)
    p.rect("right-weave", "wood_dark", 10, 15, 1, 5)
    p.rect("right-weave", "wood_dark", 11, 17, 4, 1)
    frames.append(p)

    p = Frame("berry-bush", "BerryBush",
              "Low broad leafy bush with three small rust-red fruit clusters and a short woody footing; no upright trunk or solid wall silhouette.",
              "Actual unspent HarvestablePart yields one to three WildBerries and removes the source on successful gathering.",
              "Fruit marks identify the plant, not an item count. No perpetual spent bush, light halo, extra produce or new obstruction.")
    p.rect("woody-foot", "wood_dark", 7, 20, 2, 3)
    p.rect("left-leaves", "leaf_dark", 3, 15, 5, 7)
    p.rect("left-low-leaves", "leaf_dark", 2, 18, 2, 3)
    p.rect("middle-leaves", "leaf_dark", 6, 13, 5, 9)
    p.rect("right-leaves", "leaf_dark", 10, 16, 4, 6)
    p.dot("right-low-leaves", "leaf_dark", 14, 20)
    p.rect("left-leaf-face", "leaf_mid", 3, 16, 4, 4)
    p.rect("middle-leaf-face", "leaf_mid", 7, 14, 3, 6)
    p.rect("right-leaf-face", "leaf_mid", 10, 17, 3, 4)
    p.rect("left-leaf-fold", "leaf", 3, 17, 2, 2)
    p.rect("middle-leaf-fold", "leaf", 7, 14, 2, 2)
    p.rect("right-leaf-fold", "leaf", 11, 20, 2, 1)
    for x, y in ((5, 16), (10, 16), (9, 20)):
        p.rect("berry", "berry", x, y, 2, 2)
        p.dot("berry-cut", "berry_edge", x, y)
    frames.append(p)

    p = Frame("dry-brush", "DryBrush",
              "Sparse dry forked stems and pale snapped tips, with ample open space between branches; warm brown rather than live green leaves.",
              "Existing non-solid DryBrush terrain, readily combustible and destructible. Actual thermal/effect state owns ignition and removal.",
              "No permanent flame, green foliage, harvest fruit, new solid collision or out-of-cell twigs.")
    p.rect("central-stem", "wood_dark", 7, 13, 1, 10)
    p.rect("central-stem-face", "wood", 7, 13, 1, 6)
    p.rect("left-fork", "wood", 4, 11, 1, 6)
    p.rect("left-fork", "wood", 5, 16, 1, 3)
    p.rect("left-fork", "wood", 6, 18, 2, 1)
    p.dot("left-tip", "cut", 4, 10)
    p.rect("right-fork", "wood_dark", 10, 12, 1, 5)
    p.rect("right-fork", "wood_dark", 9, 16, 1, 4)
    p.rect("right-fork", "wood_dark", 7, 19, 3, 1)
    p.rect("right-fork-face", "wood", 10, 12, 1, 3)
    p.rect("upper-tip", "wood", 11, 9, 1, 4)
    p.dot("upper-tip-cut", "cut", 11, 9)
    p.rect("low-left-fork", "wood_dark", 2, 17, 1, 3)
    p.rect("low-left-fork", "wood_dark", 3, 19, 3, 1)
    p.rect("low-left-fork", "wood_dark", 5, 20, 2, 2)
    p.rect("low-root", "wood_dark", 5, 22, 5, 1)
    p.dot("low-left-tip", "cut", 2, 17)
    p.dot("central-tip", "cut", 7, 13)
    frames.append(p)
    return frames


def model(frame, materials, scene, index):
    collection = bpy.data.collections.new(f"{index:02d}-{frame.name}")
    scene.collection.children.link(collection)
    grouped = {}
    for row, cells in enumerate(frame.cells):
        x = 0
        while x < WIDTH:
            value = cells[x]
            if value is None:
                x += 1
                continue
            end = x + 1
            while end < WIDTH and cells[end] == value:
                end += 1
            vertices, faces = grouped.setdefault(value, ([], []))
            add_quad(vertices, faces, index * WIDTH + x, HEIGHT - row - 1,
                     index * WIDTH + end, HEIGHT - row)
            x = end
    for (part, color), (vertices, faces) in sorted(grouped.items()):
        mesh = bpy.data.meshes.new(frame.name + "/" + part)
        mesh.from_pydata(vertices, [], faces)
        mesh.materials.append(materials[color])
        mesh.update()
        obj = bpy.data.objects.new(part, mesh)
        obj["authoring"] = "Original flat native pixel geometry; one grid unit per rendered pixel."
        obj["paletteHex"] = PALETTE[color]
        collection.objects.link(obj)
    return collection


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    atlas = bpy.context.scene
    atlas.name = "Native 64x24 north vegetation atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    materials = {k: emission_material(k + " #" + v, v) for k, v in PALETTE.items()}
    collections = [model(f, materials, atlas, i) for i, f in enumerate(frames)]
    camera_for(atlas, 64, HEIGHT)
    atlas.render.filepath = str(OUT / "north-vegetation.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("Contact 8x north vegetation geometry")
    camera_for(contact, 160, 144, scale=8)
    vertices, faces = [], []
    add_quad(vertices, faces, 0, 0, 160, 144, -1)
    mesh = bpy.data.meshes.new("Contact background")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(emission_material("Contact background #032F23", "032F23"))
    contact.collection.objects.link(bpy.data.objects.new("Contact background", mesh))
    for i, (frame, collection) in enumerate(zip(frames, collections)):
        instance = bpy.data.objects.new(f"{i:02d} {frame.name}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collection
        instance.location = (i * 40 + 12 - i * WIDTH, 112, 0)
        contact.collection.objects.link(instance)
        add_label(contact, f"{i:02d} {frame.name}", i * 40 + 20, 107, materials["cream"], 1.65)
    for i, (x, y) in enumerate(HEDGE_PREVIEWS):
        instance = bpy.data.objects.new(f"Hedge continuity preview {i}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collections[1]
        instance.location = (x - WIDTH, y, 0)
        contact.collection.objects.link(instance)
    add_label(contact, "NORTH ROUTE / 16 x 24 / VEGETATION", 80, 140, materials["cream"], 2)
    add_label(contact, "HEDGE / FOUR ADJACENT CELLS", 44, 70, materials["cut"], 1.5)
    add_label(contact, "VERTICAL", 100, 6, materials["cut"], 1.5)
    add_label(contact, "CORNER / SAME TILE", 128, 30, materials["cut"], 1.5)
    add_label(contact, "8x geometry / no raster edits", 41, 44, materials["cut"], 1.5)
    add_label(contact, "Solid boundaries stay in their cells", 41, 39, materials["cut"], 1.4)
    add_label(contact, "No animation or new collision", 41, 34, materials["cut"], 1.4)
    contact.render.filepath = str(OUT / "north-vegetation-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {"schema": 1, "coordinateSystem": "Top-left image origin, Y downward; flat front-facing XY mesh quads.",
              "palette": PALETTE, "frames": [{"index": i, "id": f.name, "blueprint": f.blueprint, "cells": f.cells}
                                            for i, f in enumerate(frames)]}
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {
        "schema": 1, "artifact": "north-vegetation.png", "width": 64, "height": 24,
        "frameWidth": WIDTH, "frameHeight": HEIGHT, "frameCount": 4, "pixelsPerUnit": 16,
        "tilePivotNormalized": [0.5, 0.5], "filterMode": "Point", "wrapMode": "Clamp", "mipmaps": False,
        "compression": "None", "alpha": "Binary; verified independently.",
        "source": "north-vegetation.blend", "generator": "Tools/Live16x24/build_north_vegetation.py",
        "concept": "ArtSource/Live16x24/concepts/north-parts-reference.png (reference only; original native reconstruction)",
        "sceneConcept": "ArtSource/Live16x24/concepts/north-works-concept.png",
        "capture": "Docs/Verification/Live16x24/north-greybox-works.png",
        "scope": "Four static ordinary vegetation identities. No new spawns, animation, collision or runtime integration claim.",
        "hedgePreviewOrigins": HEDGE_PREVIEWS,
        "sha256": hashlib.sha256((OUT / "north-vegetation.png").read_bytes()).hexdigest(), "frames": [],
    }
    for i, f in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(f.cells) for x, v in enumerate(row) if v]
        colors = sorted({PALETTE[v[1]] for row in f.cells for v in row if v})
        assert max(y for x, y in occupied) == (23 if f.boundary else 22) and 3 <= len(colors) <= 6, (f.name, colors)
        manifest["frames"].append({
            "index": i, "id": f.name, "blueprint": f.blueprint, "rect": [i * WIDTH, 0, WIDTH, HEIGHT],
            "description": f.description, "actualUse": f.use, "limits": f.limits,
            "occupiedPixels": len(occupied), "palette": colors,
            "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                   max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1],
            "baselineTopLeft": 24 if f.boundary else 23, "footprintCells": [1, 1], "continuousBoundary": f.boundary,
        })
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "north-vegetation.blend"))
    print("Wrote native vegetation, editable source and repeated hedge contact:", OUT)


if __name__ == "__main__":
    main()
