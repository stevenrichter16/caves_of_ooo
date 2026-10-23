"""Original native geometry for Nella's crop stages, neutral tray and ground cord.

Blender --background --python Tools/Live16x24/build_north_plot.py
Writes only ArtSource/Live16x24/north-plot. No raster is read or transformed.
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
OUT = ROOT / "ArtSource/Live16x24/north-plot"
WIDTH, HEIGHT = 16, 24
PALETTE = {
    "wood_dark": "5F4830", "wood": "94713D", "linen": "B5A47E",
    "leaf_dark": "435A35", "forest": "53683E", "leaf": "75834B",
    "rust": "9B442B", "ochre": "C99538",
}


class Frame:
    def __init__(self, name, blueprint, stage, description, use, limits):
        self.name, self.blueprint, self.stage = name, blueprint, stage
        self.description, self.use, self.limits = description, use, limits
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
    p = Frame("candycarrot-sprout", "CandyCarrotCrop", 1,
              "Low spray of fine branching leaves gathered at one quiet ground crown.",
              "Gladroot's actual second and final standing stage; yield stays below ground.",
              "No exposed root, ripe carrot, harvest flag or new third stage. Completion releases yield and removes the crop.")
    p.rect("crown", "leaf_dark", 6, 20, 4, 3)
    p.rect("leaf-center", "forest", 7, 15, 2, 6)
    p.dot("leaf-center-tip", "leaf", 7, 14)
    p.rect("leaf-left", "forest", 4, 17, 2, 3)
    p.rect("leaf-left-join", "forest", 5, 19, 3, 2)
    p.rect("leaf-left-fork", "leaf", 2, 16, 3, 1)
    p.dot("leaf-left-tip", "leaf", 2, 15)
    p.rect("leaf-left-vein", "leaf", 4, 17, 1, 2)
    p.rect("leaf-right", "forest", 10, 16, 2, 4)
    p.rect("leaf-right-join", "forest", 8, 19, 3, 2)
    p.rect("leaf-right-fork", "leaf", 11, 15, 2, 1)
    p.dot("leaf-right-tip", "leaf", 12, 14)
    p.dot("leaf-right-vein", "leaf", 10, 18)
    frames.append(p)

    p = Frame("emberwheat-sprout", "EmberwheatCrop", 1,
              "A narrow unequal cluster of upright stalks and long green leaf blades without filled grain heads.",
              "Actual stage1 before automatic produce release; unfilled immature grain silhouette.",
              "No ripe kernel head or standing ripe frame. Existing RipeCropRow remains a different owner.")
    p.rect("crown", "leaf_dark", 5, 21, 6, 2)
    p.rect("stalk-middle", "forest", 7, 10, 1, 12)
    p.rect("stalk-left", "forest", 4, 13, 1, 8)
    p.rect("stalk-left-join", "forest", 5, 20, 3, 1)
    p.rect("stalk-right", "forest", 10, 12, 1, 10)
    p.rect("blade-middle", "leaf", 7, 10, 1, 5)
    p.rect("blade-left", "leaf", 3, 16, 1, 3)
    p.rect("blade-left-tip", "leaf", 2, 15, 2, 1)
    p.dot("blade-left-join", "forest", 3, 19)
    p.rect("blade-right", "leaf", 11, 16, 1, 3)
    p.rect("blade-right-tip", "leaf", 11, 15, 2, 1)
    p.dot("blade-right-join", "forest", 11, 19)
    p.rect("blade-inner", "leaf", 8, 18, 1, 3)
    frames.append(p)

    p = Frame("marlroot-sprout", "MarlrootCrop", 1,
              "Two low broad angular leaves rising from a dark knuckled crown.",
              "The middle growing stage of the real harvest-at-maturity root crop.",
              "No ripe clay shoulder or loose harvested clod is visible yet. Crop ownership does not require Vegetation tag.")
    p.rect("crown", "leaf_dark", 6, 20, 4, 3)
    p.rect("leaf-left", "forest", 3, 17, 5, 3)
    p.rect("leaf-left-tip", "forest", 2, 16, 3, 2)
    p.rect("leaf-left-face", "leaf", 3, 17, 3, 1)
    p.dot("leaf-left-joint", "forest", 5, 20)
    p.rect("leaf-right", "forest", 8, 16, 5, 3)
    p.rect("leaf-right-tip", "forest", 11, 15, 3, 2)
    p.rect("leaf-right-face", "leaf", 10, 16, 3, 1)
    p.rect("leaf-right-joint", "forest", 9, 19, 2, 2)
    frames.append(p)

    p = Frame("marlroot-ripe", "MarlrootCrop", 2,
              "Broad leaves above attached knuckled red-clay shoulders with root ridges.",
              "Actual mature unharvested MarlrootCrop; harvest releases clods and a seed through simulation.",
              "One growing plant, not a loose FireClay pile, loose clod item or fixed yield-count pictogram.")
    p.rect("root-crown", "leaf_dark", 6, 15, 4, 8)
    p.rect("leaf-left", "forest", 2, 12, 5, 3)
    p.rect("leaf-left-tip", "forest", 1, 11, 3, 2)
    p.rect("leaf-left-face", "leaf", 2, 12, 3, 1)
    p.rect("leaf-left-joint", "forest", 5, 14, 3, 3)
    p.rect("leaf-right", "forest", 8, 11, 5, 4)
    p.rect("leaf-right-tip", "forest", 11, 10, 3, 2)
    p.rect("leaf-right-face", "leaf", 10, 11, 3, 1)
    p.rect("leaf-right-joint", "forest", 8, 14, 2, 3)
    p.rect("clay-left-knuckle", "rust", 3, 18, 4, 4)
    p.rect("clay-left-shoulder", "rust", 4, 17, 3, 1)
    p.rect("clay-right-knuckle", "rust", 9, 17, 4, 4)
    p.rect("clay-right-shoulder", "rust", 10, 16, 2, 1)
    p.rect("clay-low", "rust", 5, 21, 6, 2)
    p.rect("clay-left-face", "wood", 4, 18, 1, 3)
    p.rect("clay-right-face", "wood", 10, 17, 2, 1)
    p.rect("root-ridge", "leaf_dark", 7, 17, 2, 5)
    p.dot("root-ridge-tip", "leaf_dark", 6, 22)
    frames.append(p)

    p = Frame("pitchpod-sprout", "PitchpodCrop", 1,
              "A slim stem with two opposed pointed leaves and one small central growing tip.",
              "The middle stage of the actual harvest-at-maturity resin crop.",
              "No amber resin or paper pods before maturity; no flowering interpretation.")
    p.rect("stem", "leaf_dark", 7, 16, 2, 7)
    p.rect("leaf-left", "forest", 3, 16, 4, 2)
    p.dot("leaf-left-tip", "leaf", 2, 15)
    p.rect("leaf-left-edge", "leaf", 3, 15, 2, 1)
    p.rect("leaf-left-joint", "forest", 6, 17, 2, 2)
    p.rect("leaf-right", "forest", 9, 14, 3, 3)
    p.dot("leaf-right-tip", "leaf", 12, 13)
    p.dot("leaf-right-tip-join", "forest", 12, 14)
    p.rect("leaf-right-edge", "leaf", 10, 14, 2, 1)
    p.rect("leaf-right-joint", "forest", 8, 16, 2, 2)
    p.rect("tip", "forest", 7, 12, 1, 4)
    p.dot("tip-light", "leaf", 7, 12)
    frames.append(p)

    p = Frame("pitchpod-ripe", "PitchpodCrop", 2,
              "Paired olive leaves with two angular hanging paper pods and a small amber resin bead.",
              "Actual mature unharvested resin crop; products and seed appear only through real harvest.",
              "Pods are not round flowers, lamps, flame, a brewing effect or a promise of a fixed visible yield.")
    p.rect("stem", "leaf_dark", 7, 9, 2, 14)
    p.rect("leaf-left", "forest", 3, 17, 5, 3)
    p.rect("leaf-left-tip", "forest", 2, 16, 2, 1)
    p.rect("leaf-left-face", "leaf", 3, 17, 3, 1)
    p.rect("leaf-right", "forest", 8, 18, 5, 3)
    p.rect("leaf-right-tip", "forest", 12, 17, 2, 1)
    p.rect("leaf-right-face", "leaf", 10, 18, 3, 1)
    p.rect("pod-left-stalk", "leaf_dark", 4, 8, 4, 1)
    p.rect("pod-left-shoulder", "wood", 3, 9, 3, 1)
    p.rect("pod-left-body", "wood", 2, 10, 5, 4)
    p.rect("pod-left-taper", "wood", 3, 14, 3, 1)
    p.dot("pod-left-tip", "wood", 4, 15)
    p.rect("pod-left-fold", "linen", 3, 10, 1, 3)
    p.dot("pod-left-fold-tip", "linen", 4, 13)
    p.rect("pod-right-stalk", "leaf_dark", 8, 6, 4, 3)
    p.rect("pod-right-shoulder", "wood", 10, 8, 3, 1)
    p.rect("pod-right-body", "wood", 9, 9, 5, 4)
    p.rect("pod-right-taper", "wood", 10, 13, 3, 1)
    p.dot("pod-right-tip", "wood", 11, 14)
    p.rect("pod-right-fold", "linen", 10, 9, 1, 3)
    p.dot("pod-right-fold-tip", "linen", 11, 12)
    p.dot("amber-bead", "ochre", 4, 16)
    p.dot("amber-bead-edge", "ochre", 5, 16)
    frames.append(p)

    p = Frame("reserve-tray", "ConnectedReserveTray", None,
              "A shallow worn wooden sorting tray with two small cord loops and an empty open center.",
              "Existing non-solid ContainerPart owner; neutral contents-independent identity.",
              "No painted seed, stock, loot promise, chest lock, permission color or new open/closed state.")
    p.rect("tray-front", "wood_dark", 2, 20, 12, 2)
    p.rect("tray-front-edge", "wood", 3, 20, 10, 1)
    p.rect("tray-foot", "wood_dark", 3, 22, 10, 1)
    p.rect("tray-back", "wood_dark", 3, 16, 10, 1)
    p.rect("tray-left", "wood_dark", 2, 17, 2, 4)
    p.rect("tray-right", "wood_dark", 12, 17, 2, 4)
    p.rect("tray-left-face", "wood", 2, 18, 1, 2)
    p.rect("tray-right-face", "wood", 13, 18, 1, 2)
    p.rect("tray-floor", "wood_dark", 4, 19, 8, 1)
    p.rect("cord-loop-left-top", "linen", 3, 15, 2, 1)
    p.dot("cord-loop-left", "linen", 2, 16)
    p.dot("cord-loop-left", "wood", 2, 17)
    p.dot("cord-loop-left", "linen", 5, 16)
    p.rect("cord-loop-right-top", "linen", 11, 15, 2, 1)
    p.dot("cord-loop-right", "linen", 10, 16)
    p.dot("cord-loop-right", "linen", 13, 16)
    p.dot("cord-loop-right", "wood", 13, 17)
    frames.append(p)

    p = Frame("reserve-cord", None, None,
              "Asymmetric short twine runs with small tie knots laid flat near a bed's edges; fully transparent center.",
              "Decoration only on the two actual bound claimed ground owners; survives crop removal.",
              "No fence, collision, seed/contents count, permission color or full-cell border. Not a standalone owner.")
    # Unequal lengths, slight bends and inset tie tails prevent this physical
    # material from reading as four identical UI selection brackets.
    for x, y in ((3, 3), (2, 3), (2, 4), (2, 5), (1, 5), (1, 6), (1, 7), (1, 8),
                 (2, 8), (2, 9), (2, 10), (10, 4), (11, 4), (12, 4), (12, 5), (13, 5),
                 (2, 20), (3, 20), (3, 21), (4, 21), (5, 21), (6, 21),
                 (13, 15), (13, 16), (13, 17), (14, 17), (14, 18), (14, 19), (14, 20)):
        p.dot("cord-short-run", "wood", x, y)
    p.dot("cord-knot-left", "linen", 1, 7)
    p.dot("cord-tie-tail-left", "wood_dark", 2, 7)
    p.dot("cord-knot-low", "linen", 4, 21)
    p.dot("cord-tie-tail-low", "wood_dark", 4, 20)
    p.dot("cord-knot-right", "linen", 14, 18)
    p.dot("cord-tie-tail-right", "wood_dark", 13, 18)
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
            add_quad(vertices, faces, index * WIDTH + x, HEIGHT - row - 1, index * WIDTH + end, HEIGHT - row)
            x = end
    for (part, color), (vertices, faces) in sorted(grouped.items()):
        mesh = bpy.data.meshes.new(frame.name + "/" + part)
        mesh.from_pydata(vertices, [], faces)
        mesh.materials.append(materials[color])
        mesh.update()
        obj = bpy.data.objects.new(part, mesh)
        obj["authoring"] = "Original flat native geometry; one grid unit per rendered pixel."
        obj["paletteHex"] = PALETTE[color]
        collection.objects.link(obj)
    return collection


def instance(scene, collection, name, x, y, z=0):
    obj = bpy.data.objects.new(name, None)
    obj.instance_type = "COLLECTION"
    obj.instance_collection = collection
    obj.location = (x, y, z)
    scene.collection.objects.link(obj)


def plane(scene, name, color, x, y, width, height, z):
    vertices, faces = [], []
    add_quad(vertices, faces, x, y, x + width, y + height, z)
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(emission_material(name + " #" + color, color))
    scene.collection.objects.link(bpy.data.objects.new(name, mesh))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    atlas = bpy.context.scene
    atlas.name = "Native 128x24 north plot atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    assert len(frames) == 8
    materials = {key: emission_material(key + " #" + value, value) for key, value in PALETTE.items()}
    collections = [model(frame, materials, atlas, i) for i, frame in enumerate(frames)]
    camera_for(atlas, 128, 24)
    atlas.render.filepath = str(OUT / "north-plot.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("Contact and ground overlay 8x geometry")
    camera_for(contact, 160, 144, scale=8)
    plane(contact, "Contact background", "032F23", 0, 0, 160, 144, -1)
    labels = ("00 gladroot sprout", "01 wheat sprout", "02 marlroot sprout", "03 marlroot ripe",
              "04 pitchpod sprout", "05 pitchpod ripe", "06 neutral tray", "07 ground cord")
    for i, (frame, collection) in enumerate(zip(frames, collections)):
        instance(contact, collection, f"{i:02d} {frame.name}", (i % 4) * 40 + 12 - i * 16, 108 - (i // 4) * 44)
        add_label(contact, labels[i], (i % 4) * 40 + 20, 103 - (i // 4) * 44, materials["linen"], 1.55)
    add_label(contact, "NELLA PLOT / NATIVE 16 x 24 / FLAT GEOMETRY", 80, 139, materials["linen"], 2)
    add_label(contact, "GROUND CORD / EMPTY AND RIPE / EXACT DRY + WET SOIL", 80, 45, materials["linen"], 1.8)
    for i, (soil, crop, label) in enumerate((("45341F", None, "dry / empty"), ("252319", None, "wet / empty"),
                                            ("45341F", 3, "dry / marlroot"), ("252319", 5, "wet / pitchpod"))):
        x, y = i * 40 + 12, 12
        plane(contact, "Soil " + label, soil, x, y, 16, 24, -0.5)
        instance(contact, collections[7], "Ground cord " + label, x - 7 * 16, y)
        if crop is not None:
            instance(contact, collections[crop], "Foreground crop " + label, x - crop * 16, y, 0.2)
        add_label(contact, label, i * 40 + 20, 7, materials["linen"], 1.6)
    contact.render.filepath = str(OUT / "north-plot-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {"schema": 1, "coordinateSystem": "Top-left origin, Y downward; flat front-facing XY quads.",
              "palette": PALETTE, "frames": [{"index": i, "id": f.name, "blueprint": f.blueprint,
                                             "stage": f.stage, "cells": f.cells} for i, f in enumerate(frames)]}
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {"schema": 1, "artifact": "north-plot.png", "width": 128, "height": 24,
                "frameWidth": 16, "frameHeight": 24, "frameCount": 8, "pixelsPerUnit": 16,
                "tilePivotNormalized": [0.5, 0.5], "filterMode": "Point", "wrapMode": "Clamp", "mipmaps": False,
                "compression": "None", "alpha": "Binary; independently verified.", "source": "north-plot.blend",
                "generator": "Tools/Live16x24/build_north_plot.py", "resource": "Sprites/Live16x24/north-plot",
                "reference": "ArtSource/Live16x24/concepts/north-parts-reference.png",
                "concept": "ArtSource/Live16x24/concepts/north-plot-concept.png",
                "capture": "Docs/Verification/Live16x24/north-greybox-plot.png",
                "scope": "Six real standing crop states, neutral tray and separate ground decoration. No runtime integration claim.",
                "sha256": hashlib.sha256((OUT / "north-plot.png").read_bytes()).hexdigest(), "frames": []}
    for i, f in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(f.cells) for x, value in enumerate(row) if value]
        colors = sorted({PALETTE[value[1]] for row in f.cells for value in row if value})
        assert all(y < 23 for x, y in occupied) and 3 <= len(colors) <= 6, (f.name, colors)
        manifest["frames"].append({"index": i, "id": f.name, "blueprint": f.blueprint, "stage": f.stage,
                                  "rect": [i*16, 0, 16, 24], "description": f.description, "actualUse": f.use,
                                  "limits": f.limits, "occupiedPixels": len(occupied), "palette": colors,
                                  "footprintCells": [1, 1], "layer": "ground-decoration" if i == 7 else "foreground",
                                  "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                                         max(x for x, y in occupied)+1, max(y for x, y in occupied)+1],
                                  "baselineTopLeft": 22 if i == 7 else 23})
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "north-plot.blend"))
    print("Wrote original native plot geometry and direct8x contact:", OUT)


if __name__ == "__main__":
    main()
