"""Original native pixel geometry for north works, forage and creatures.

Run Blender --background --python Tools/Live16x24/build_north_works.py.
Writes only ArtSource/Live16x24/north-works. No raster is read or transformed.
Patchbearer derives accepted Gleaner geometry, never a sampled sprite.
"""
import copy
import hashlib
import json
from pathlib import Path
import sys

import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import add_label, add_quad, camera_for, emission_material
from build_opening_cast import make_frames as accepted_cast_frames

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/north-works"
WIDTH, HEIGHT = 16, 24
PALETTE = {
    "wood_dark": "5F4830", "wood": "94713D", "copper": "A97542", "ochre": "C99538",
    "linen": "B5A47E", "cream": "E4D0A0", "leaf_dark": "435A35", "forest": "53683E",
    "leaf": "75834B", "stone_dark": "3C5148", "stone": "526657", "stone_light": "7D8E71",
    "cold": "70A68A", "water": "196463", "ink": "233C32", "skin": "A97542",
}


class Frame:
    def __init__(self, name, blueprint, description, use, limits):
        self.name, self.blueprint, self.description = name, blueprint, description
        self.use, self.limits = use, limits
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
    p = Frame("alchemy-still", "AlchemyStill", "Compact warm receiving body and bent condenser with a small attached receiving vessel.",
              "Owned AlchemyStillPart provides immediate real brewing interaction; existing non-solid furniture.",
              "No flame, bubbles, liquid level, output item or invented ready/running state.")
    p.rect("body", "wood_dark", 2, 15, 7, 7)
    p.rect("body-face", "copper", 3, 16, 5, 5)
    p.rect("body-foot", "wood_dark", 3, 22, 5, 1)
    p.rect("shoulder", "copper", 2, 14, 7, 2)
    p.rect("shoulder", "copper", 3, 13, 5, 1)
    p.rect("neck", "copper", 5, 10, 2, 4)
    p.rect("body-band", "wood", 2, 17, 7, 1)
    p.rect("body-band-light", "linen", 3, 17, 2, 1)
    p.rect("condenser-rise", "copper", 5, 7, 1, 4)
    p.rect("condenser-bend", "copper", 6, 6, 2, 2)
    p.rect("condenser-top", "copper", 7, 5, 4, 1)
    p.rect("condenser-fall", "copper", 10, 6, 2, 1)
    p.rect("condenser-fall", "copper", 11, 7, 2, 2)
    p.rect("condenser-down", "wood", 12, 9, 1, 7)
    p.dot("condenser-joint", "linen", 12, 9)
    p.rect("receiver-neck", "wood_dark", 12, 16, 1, 2)
    p.rect("receiver-body", "copper", 11, 18, 3, 4)
    p.rect("receiver-foot", "wood_dark", 11, 22, 3, 1)
    p.rect("receiver-rim", "wood", 11, 17, 3, 1)
    frames.append(p)

    p = Frame("alchemy-shelf", "AlchemyShelf", "Two broad empty compartments between upright wooden posts and low feet.",
              "Existing solid wooden Container owner. Empty native compartments do not manufacture stock.",
              "No bottle silhouettes, herbs, painted supplies, loot count or new open/closed state.")
    p.rect("left-post", "wood_dark", 2, 5, 2, 18)
    p.rect("right-post", "wood_dark", 12, 5, 2, 18)
    p.rect("left-post-face", "wood", 2, 5, 1, 16)
    p.rect("right-post-face", "wood", 12, 5, 1, 16)
    for y in (5, 12, 19):
        p.rect("shelf-board", "wood_dark", 3, y, 10, 3)
        p.rect("shelf-edge", "wood", 3, y, 10, 1)
    p.rect("worn-top", "linen", 4, 5, 4, 1)
    p.dot("left-post-peg", "linen", 2, 12)
    p.dot("right-post-peg", "linen", 12, 19)
    frames.append(p)

    p = Frame("stoneburr", "StoneburrPatch", "Three ochre hooked burr heads over dark leathery leaves on irregular stems.",
              "Unspent HarvestablePart yields StoneburrSeed and removes the source after successful gathering.",
              "Burrs identify the source, not yield count. No corn-like kernels, ripe crop flag or permanent spent plant.")
    p.rect("middle-stem", "wood_dark", 7, 10, 1, 13)
    p.rect("left-stem", "wood_dark", 3, 13, 1, 8)
    p.rect("left-join", "wood_dark", 4, 20, 4, 1)
    p.rect("right-stem", "wood_dark", 11, 15, 1, 7)
    p.rect("right-join", "wood_dark", 8, 21, 4, 1)
    p.rect("left-leaf", "forest", 4, 17, 3, 2)
    p.dot("left-leaf-tip", "leaf", 4, 17)
    p.rect("right-leaf", "forest", 8, 18, 3, 2)
    p.dot("right-leaf-tip", "leaf", 10, 18)
    for x, y in ((2, 10), (6, 6), (10, 12)):
        p.rect("burr-core", "ochre", x + 1, y + 1, 2, 3)
        p.rect("burr-hook-top", "ochre", x, y, 3, 1)
        p.dot("burr-hook-left", "ochre", x, y + 1)
        p.rect("burr-hook-bottom", "ochre", x + 1, y + 4, 3, 1)
        p.dot("burr-hook-right", "ochre", x + 3, y + 3)
        p.dot("burr-face", "linen", x + 1, y + 1)
    frames.append(p)

    p = Frame("frost-lichen", "FrostLichenPatch", "Low broken cold mineral crust with broad, quiet lobes close to the ground.",
              "Unspent HarvestablePart yields FrostLichen and removes this source on success.",
              "Not a flowering plant, pool, emitted frost cloud, ready coating or safe drink.")
    p.rect("crust-foot", "stone_dark", 2, 21, 12, 2)
    p.rect("left-crust", "stone", 1, 20, 4, 2)
    p.rect("middle-crust", "stone", 5, 18, 6, 4)
    p.rect("right-crust", "stone", 10, 20, 5, 2)
    p.rect("left-cold-lobe", "cold", 2, 19, 3, 2)
    p.rect("middle-cold-lobe", "cold", 6, 17, 3, 2)
    p.rect("right-cold-lobe", "cold", 11, 19, 3, 2)
    p.rect("low-cold-seam", "cold", 6, 21, 4, 1)
    frames.append(p)

    p = Frame("mendleaf", "MendleafPlant", "Three broad pointed soft leaves, each with a simple pale central vein.",
              "Unspent HarvestablePart yields MendleafSprig and removes this source on success.",
              "No new flower, glow, crop growth state, harvest count or baked effect.")
    p.rect("stem", "wood_dark", 7, 16, 1, 7)
    p.rect("upper-leaf", "forest", 6, 12, 3, 6)
    p.dot("upper-leaf-tip", "forest", 7, 11)
    p.rect("upper-leaf-side", "leaf", 5, 13, 1, 3)
    p.rect("silver-vein-upper", "linen", 7, 13, 1, 4)
    p.rect("left-leaf", "forest", 3, 18, 5, 3)
    p.dot("left-leaf-tip", "forest", 2, 19)
    p.rect("left-leaf-top", "leaf", 4, 17, 2, 1)
    p.rect("silver-vein-left", "linen", 4, 19, 3, 1)
    p.rect("right-leaf", "forest", 8, 16, 5, 3)
    p.dot("right-leaf-tip", "forest", 13, 17)
    p.rect("right-leaf-top", "leaf", 9, 15, 3, 1)
    p.rect("silver-vein-right", "linen", 9, 17, 3, 1)
    p.rect("lower-leaf", "forest", 8, 20, 4, 2)
    p.dot("lower-leaf-tip", "leaf", 11, 21)
    p.rect("silver-vein-lower", "linen", 8, 20, 2, 1)
    frames.append(p)

    p = Frame("magpie", "Magpie", "Compact cream-breasted dark bird with a low long tail, angular head, short beak and two separated feet.",
              "Actual passive hoarder creature; inventory and pickup events remain simulation-owned.",
              "No permanent stolen item, sack, toy eyes, wing animation or directional sheet claim.")
    p.rect("body", "ink", 6, 15, 6, 5)
    p.rect("neck", "ink", 10, 12, 2, 5)
    p.rect("head", "ink", 10, 10, 3, 3)
    p.dot("forehead", "stone", 10, 10)
    p.dot("eye", "cream", 12, 11)
    p.rect("beak", "wood_dark", 13, 12, 2, 1)
    p.rect("breast", "cream", 10, 15, 2, 4)
    p.rect("belly", "cream", 8, 19, 3, 1)
    p.rect("wing", "stone_dark", 6, 16, 4, 3)
    p.rect("wing-edge", "stone", 6, 16, 3, 1)
    p.rect("tail-root", "ink", 4, 18, 3, 2)
    p.rect("tail-middle", "ink", 2, 19, 3, 2)
    p.rect("tail-tip", "stone_dark", 1, 21, 3, 1)
    p.rect("tail-edge", "stone", 3, 19, 3, 1)
    p.rect("left-foot", "wood_dark", 8, 20, 1, 3)
    p.dot("left-toe", "wood_dark", 7, 22)
    p.rect("right-foot", "wood_dark", 11, 19, 1, 4)
    p.dot("right-toe", "wood_dark", 12, 22)
    frames.append(p)

    p = Frame("viper", "Viper", "Low angular S-shaped snake with a broad raised head, narrow neck and open lower coil.",
              "Actual hostile creature with ViperBite; poison and corpse/gland outcomes remain simulation state.",
              "No permanent poison, tongue flick, invented limbs or new directional motion.")
    p.rect("lower-coil", "leaf_dark", 3, 20, 9, 2)
    p.rect("lower-coil-base", "forest", 4, 22, 7, 1)
    # A two-pixel bend with open negative space, not a filled snake-shaped mound.
    p.rect("left-turn", "forest", 2, 18, 2, 3)
    p.rect("upper-bend", "forest", 3, 17, 6, 2)
    p.rect("lower-back", "forest", 10, 19, 3, 3)
    p.rect("lower-top", "leaf", 4, 20, 6, 1)
    p.rect("neck", "forest", 8, 14, 2, 4)
    p.rect("neck-edge", "leaf", 8, 14, 1, 3)
    p.rect("head", "forest", 9, 12, 5, 3)
    p.rect("head-crown", "leaf", 10, 11, 3, 1)
    p.rect("head-edge", "leaf", 9, 12, 3, 1)
    p.dot("head-eye", "ochre", 12, 13)
    p.dot("head-nose", "leaf_dark", 13, 14)
    # Remove the inner loop before mesh generation, leaving actual transparent cells.
    for y in (20, 21):
        for x in range(5, 10):
            p.cells[y][x] = None
    frames.append(p)

    # Preserve accepted shale anatomy/stance; remap flat material clusters to
    # six colors including the state-only bottle, rather than imitate the
    # generated humanoid in the reference sheet.
    p = Frame("patchbearer-healing-slot-present", "MarlbackPatchbearer",
              "Forward-leaning shale-backed Gleaner anatomy, repaired pale straps and one small accessible healing bottle in its cradle.",
              "Actual owned FieldMedicinePart.FindCarriedMedicine() returns a positive carried healing tonic.",
              "Healing slot only, not total medicine count; hands remain equipment-neutral. Static pose.")
    remap = {"rust": "skin", "tawny": "skin", "stone_dark": "stone_dark", "stone": "stone",
             "stone_light": "linen", "ink": "stone_dark", "claw": "linen"}
    p.cells = [[(value[0], remap[value[1]]) if value else None for value in row]
               for row in accepted_cast_frames()[3].cells]
    for x, y, w, h in ((5, 7, 2, 1), (6, 8, 2, 1), (7, 9, 2, 1), (8, 10, 3, 1),
                       (9, 11, 1, 2), (7, 13, 2, 1), (6, 14, 2, 1), (4, 15, 2, 1)):
        p.rect("patched-strap", "linen", x, y, w, h)
    p.rect("healing-cradle-back", "stone_dark", 10, 9, 4, 5)
    p.rect("healing-cradle-rim", "linen", 10, 13, 4, 1)
    p.dot("healing-cradle-left", "linen", 10, 12)
    p.dot("healing-cradle-right", "linen", 13, 12)
    absent = copy.deepcopy(p)
    absent.name = "patchbearer-healing-slot-absent"
    absent.description = "The identical shale body and repaired straps, with the small healing cradle exposed."
    absent.use = "Actual owned medicine query finds no carried healing tonic. Separate Antidote may remain."
    absent.limits = "Not an all-medicine-empty claim; no changed stance, missing equipment, injury or weakened behavior implied."
    p.dot("healing-bottle-stopper", "cream", 11, 8)
    p.rect("healing-bottle-neck", "cold", 11, 9, 1, 1)
    p.rect("healing-bottle-body", "cold", 11, 10, 2, 3)
    p.dot("healing-bottle-label", "cream", 11, 11)
    frames.extend((p, absent))
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
        obj["authoring"] = "Original flat native pixel geometry; one grid unit per rendered pixel."
        obj["paletteHex"] = PALETTE[color]
        collection.objects.link(obj)
    return collection


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    atlas = bpy.context.scene
    atlas.name = "Native 144x24 north works atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    assert len(frames) == 9
    materials = {key: emission_material(key + " #" + value, value) for key, value in PALETTE.items()}
    collections = [model(frame, materials, atlas, i) for i, frame in enumerate(frames)]
    camera_for(atlas, 144, 24)
    atlas.render.filepath = str(OUT / "north-works.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("Contact 8x north works geometry")
    camera_for(contact, 120, 148, scale=8)
    vertices, faces = [], []
    add_quad(vertices, faces, 0, 0, 120, 148, -1)
    mesh = bpy.data.meshes.new("Contact background")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(emission_material("Contact background #032F23", "032F23"))
    contact.collection.objects.link(bpy.data.objects.new("Contact background", mesh))
    labels = ("00 still / no flame", "01 empty shelf", "02 stoneburr", "03 frost lichen", "04 mendleaf",
              "05 magpie", "06 viper", "07 healing slot present", "08 healing slot absent")
    for i, (frame, collection) in enumerate(zip(frames, collections)):
        instance = bpy.data.objects.new(f"{i:02d} {frame.name}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collection
        instance.location = ((i % 3) * 40 + 12 - i * 16, 112 - (i // 3) * 44, 0)
        contact.collection.objects.link(instance)
        add_label(contact, labels[i], (i % 3) * 40 + 20, 107 - (i // 3) * 44, materials["cream"], 1.55)
    add_label(contact, "NORTH WORKS / 16 x 24 / NATIVE GEOMETRY", 60, 143, materials["cream"], 1.9)
    add_label(contact, "Static actors / unchanged gameplay / 8x geometry, no raster edits", 60, 5, materials["linen"], 1.4)
    contact.render.filepath = str(OUT / "north-works-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {"schema": 1, "coordinateSystem": "Top-left origin, Y downward; flat front-facing XY mesh quads.",
              "palette": PALETTE, "frames": [{"index": i, "id": f.name, "blueprint": f.blueprint, "cells": f.cells}
                                             for i, f in enumerate(frames)]}
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {"schema": 1, "artifact": "north-works.png", "width": 144, "height": 24,
                "frameWidth": 16, "frameHeight": 24, "frameCount": 9, "pixelsPerUnit": 16,
                "tilePivotNormalized": [0.5,0.5], "filterMode": "Point", "wrapMode": "Clamp", "mipmaps": False,
                "compression": "None", "alpha": "Binary; independently verified.", "source": "north-works.blend",
                "generator": "Tools/Live16x24/build_north_works.py", "resource": "Sprites/Live16x24/north-works",
                "reference": "ArtSource/Live16x24/concepts/north-parts-reference.png",
                "concept": "ArtSource/Live16x24/concepts/north-works-concept.png",
                "capture": "Docs/Verification/Live16x24/north-greybox-works.png",
                "scope": "Nine static functional/creature identity states. No animation or runtime integration claim.",
                "sha256": hashlib.sha256((OUT / "north-works.png").read_bytes()).hexdigest(), "frames": []}
    for i, f in enumerate(frames):
        occupied = [(x,y) for y,row in enumerate(f.cells) for x,value in enumerate(row) if value]
        colors = sorted({PALETTE[value[1]] for row in f.cells for value in row if value})
        assert max(y for x,y in occupied) == 22 and 3 <= len(colors) <= 6, (f.name, colors)
        manifest["frames"].append({"index": i, "id": f.name, "blueprint": f.blueprint, "rect": [i*16,0,16,24],
                                  "description": f.description, "actualUse": f.use, "limits": f.limits,
                                  "occupiedPixels": len(occupied), "palette": colors, "footprintCells": [1,1],
                                  "alphaBoundsTopLeft": [min(x for x,y in occupied), min(y for x,y in occupied),
                                                         max(x for x,y in occupied)+1,23], "baselineTopLeft": 23})
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "north-works.blend"))
    print("Wrote native north works, editable source and geometry contact:", OUT)


if __name__ == "__main__":
    main()
