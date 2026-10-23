"""Render nine cellar sprites from original editable flat Blender geometry.

Run Blender --background --python Tools/Live16x24/build_cellar.py.
Writes only ArtSource/Live16x24/cellar. Concepts are visual references only:
no raster is opened, sampled, traced, cropped, resized or edited. One geometry
unit is one native pixel. The accepted base supplies camera/material helpers.
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
OUT = ROOT / "ArtSource/Live16x24/cellar"
WIDTH, HEIGHT = 16, 24
PALETTE = {
    "charcoal": "342E25", "wood_dark": "5F4830", "wood": "94713D", "cut": "B5A47E",
    "mortar": "233C32", "stone_dark": "3C5148", "stone": "526657", "stone_light": "7D8E71",
    "stone_edge": "A3AE8C", "cream": "E4D0A0", "forest": "53683E", "olive": "75834B",
}


class Frame:
    def __init__(self, name, blueprint, description, use, limits, stage=None):
        self.name, self.blueprint, self.description = name, blueprint, description
        self.use, self.limits, self.stage = use, limits, stage
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
    p = Frame("stairs-up", "StairsUp",
              "Three broad pale treads rise between short stone cheeks toward a clear upper opening; no vertical doorway or descent pit.",
              "The real upward stair uses the existing registered connection back to the glade.",
              "Owned upward-stair and physics identity. No invented ladder, door, glow or additional destination.")
    p.rect("left-cheek", "stone_dark", 3, 7, 2, 15)
    p.rect("right-cheek", "stone_dark", 11, 7, 2, 15)
    p.rect("left-cheek-face", "stone", 3, 8, 1, 10)
    p.rect("right-cheek-face", "stone", 12, 8, 1, 10)
    p.rect("left-cap", "stone_light", 3, 6, 2, 1)
    p.rect("right-cap", "stone_light", 11, 6, 2, 1)
    p.rect("upper-riser", "stone", 5, 13, 6, 3)
    p.rect("middle-riser", "stone", 4, 17, 8, 3)
    p.rect("lower-riser", "stone", 2, 21, 12, 2)
    p.rect("riser-joint", "stone_dark", 8, 13, 1, 3)
    p.rect("riser-joint", "stone_dark", 6, 17, 1, 3)
    p.rect("riser-joint", "stone_dark", 10, 21, 1, 2)
    p.rect("tread", "stone_edge", 5, 12, 6, 1)
    p.rect("tread", "stone_edge", 4, 16, 8, 1)
    p.rect("tread", "stone_edge", 2, 20, 12, 1)
    p.rect("left-low-cheek", "stone_dark", 1, 20, 1, 3)
    p.rect("right-low-cheek", "stone_dark", 14, 20, 1, 3)
    frames.append(p)

    p = Frame("crate", "Crate",
              "Squat closed plank crate with broad corner braces, recessed central boards and a flat upper rail; no chest latch or arched lid.",
              "Finite existing container; the actual crate carries clay, a seed-dependent equipment reward and a manual.",
              "Contents and empty state do not change this frame. No persistent open-lid state exists; no supplies are painted onto the crate.")
    p.rect("plank-body", "wood", 2, 11, 12, 12)
    p.rect("upper-rail", "cut", 3, 10, 10, 1)
    p.rect("upper-inset", "wood_dark", 4, 12, 8, 2)
    p.rect("upper-crossbrace", "cut", 2, 14, 12, 1)
    p.rect("inset-boards", "wood_dark", 4, 15, 8, 6)
    p.rect("middle-board", "wood", 7, 16, 2, 5)
    p.rect("left-brace", "wood", 2, 11, 2, 12)
    p.rect("right-brace", "wood", 12, 11, 2, 12)
    p.rect("lower-rail", "cut", 4, 21, 8, 1)
    p.rect("lower-edge", "wood_dark", 4, 22, 8, 1)
    for x, y in ((2, 12), (13, 12), (2, 19), (13, 19)):
        p.dot("brace-fastener", "charcoal", x, y)
    frames.append(p)

    p = Frame("timber-pallet", "GleanersTimberPallet",
              "Broad low wooden slats across two visible runners, with open spaces between the three boards; larger and heavier than loose split timber.",
              "Existing solid haulable owner. Dismantling its unspent harvest removes it and yields two real SalvagedTimber units.",
              "No inventory, carried supply pile or depleted-pallet variant. Player strength and action readiness remain gameplay decisions.")
    p.rect("left-runner", "wood_dark", 3, 14, 2, 9)
    p.rect("right-runner", "wood_dark", 11, 14, 2, 9)
    for y in (14, 17, 20):
        p.rect("slat", "wood", 1, y, 14, 2)
        p.rect("slat-cut", "cut", 1, y, 14, 1)
        p.dot("left-fastener", "charcoal", 2, y)
        p.dot("right-fastener", "charcoal", 13, y)
    frames.append(p)

    p = Frame("sootroot-seed-stage", "SootrootCrop",
              "A tiny planted seed notch set in a low uneven soil mark; distinct from the pale loose seed and no premature green shoot.",
              "Current owned sootroot GrowthStage0; moisture and elapsed-world growth belong to CropPart and cultivated ground.",
              "No wooden planter, readiness icon or fixed-duration promise. The underlying prepared bed is a separate real floor owner.", 0)
    p.rect("soil-notch", "wood_dark", 5, 20, 6, 2)
    p.rect("soil-base", "wood_dark", 6, 22, 4, 1)
    p.dot("soil-shoulder", "wood_dark", 4, 21).dot("soil-shoulder", "wood_dark", 11, 21)
    p.rect("planted-seed", "charcoal", 7, 20, 3, 1)
    p.dot("seed-edge", "cut", 7, 20)
    p.dot("notch-tip", "wood_dark", 8, 19)
    frames.append(p)

    p = Frame("sootroot-sprout", "SootrootCrop",
              "Paired angular olive leaves on one short stem above a low soil footing; no harvest bulb or ripe crown.",
              "Current owned sootroot GrowthStage1. Watering modifies actual prepared-ground state rather than a separate decorative plant.",
              "No invented wilted or harvested state. The actual crop must retain its mature-harvest pulp and cultivated-seed contract.", 1)
    p.rect("stem", "olive", 7, 17, 2, 5)
    p.rect("left-leaf", "forest", 3, 12, 3, 3)
    p.rect("left-leaf", "forest", 4, 14, 3, 3)
    p.rect("left-leaf", "forest", 6, 16, 2, 2)
    p.rect("left-leaf-fold", "olive", 4, 13, 1, 2)
    p.rect("left-leaf-fold", "olive", 5, 15, 1, 2)
    p.dot("left-leaf-fold", "olive", 6, 17)
    p.rect("right-leaf", "forest", 10, 12, 3, 3)
    p.rect("right-leaf", "forest", 9, 14, 3, 3)
    p.rect("right-leaf", "forest", 8, 16, 2, 2)
    p.rect("right-leaf-fold", "olive", 11, 12, 1, 2)
    p.rect("right-leaf-fold", "olive", 10, 14, 1, 2)
    p.dot("right-leaf-fold", "olive", 9, 16)
    p.rect("soil-footing", "wood_dark", 5, 22, 6, 1)
    p.dot("soil-footing", "wood_dark", 6, 21).dot("soil-footing", "wood_dark", 9, 21)
    frames.append(p)

    p = Frame("sootroot-ripe", "SootrootCrop",
              "Three angular olive leaves rise from dark root shoulders with one restrained pale vertical seam; the root remains planted and uncut.",
              "Current owned sootroot GrowthStage2. Mature harvest removes this owner and produces actual pulp and seed.",
              "Do not render a spent crop after harvest. Positive yield counts and actual owned part state are authoritative; growth timing is not painted.", 2)
    p.rect("central-leaf", "forest", 7, 5, 2, 10)
    p.rect("central-leaf", "forest", 6, 7, 4, 5)
    p.rect("central-leaf-fold", "olive", 8, 5, 1, 9)
    p.rect("central-stem", "olive", 7, 14, 2, 4)
    p.rect("left-leaf", "forest", 2, 8, 3, 3)
    p.rect("left-leaf", "forest", 3, 10, 3, 3)
    p.rect("left-leaf", "forest", 4, 12, 3, 3)
    p.rect("left-stem", "forest", 6, 14, 1, 4)
    p.rect("left-leaf-fold", "olive", 2, 9, 1, 2)
    p.rect("left-leaf-fold", "olive", 3, 11, 1, 2)
    p.rect("left-leaf-fold", "olive", 4, 13, 1, 2)
    p.dot("left-leaf-fold", "olive", 5, 15)
    p.rect("right-leaf", "forest", 11, 8, 3, 3)
    p.rect("right-leaf", "forest", 10, 10, 3, 3)
    p.rect("right-leaf", "forest", 9, 12, 3, 3)
    p.rect("right-stem", "forest", 9, 14, 1, 4)
    p.rect("right-leaf-fold", "olive", 12, 9, 1, 2)
    p.rect("right-leaf-fold", "olive", 11, 11, 1, 2)
    p.rect("right-leaf-fold", "olive", 10, 13, 1, 2)
    p.dot("right-leaf-fold", "olive", 9, 15)
    p.rect("root-shoulder", "charcoal", 5, 18, 6, 1)
    p.rect("root-body", "charcoal", 4, 19, 8, 3)
    p.rect("root-base", "charcoal", 5, 22, 6, 1)
    p.rect("root-dry-edge", "wood_dark", 4, 19, 1, 3)
    p.dot("root-dry-edge", "wood_dark", 5, 22)
    p.rect("root-seam", "cut", 7, 19, 1, 3)
    frames.append(p)

    p = Frame("sootroot-pulp", "SootrootPulp",
              "One compact torn root portion with charcoal skin and a pale irregular interior; no bottle or whole live plant.",
              "Actual harvested, takeable, applicable burning cure. It is not a drinkable healing tonic.",
              "No flames, water pool, shine, stack-count imagery or invented processing output.")
    p.rect("pulp-skin", "charcoal", 6, 17, 4, 1)
    p.rect("pulp-skin", "charcoal", 5, 18, 6, 4)
    p.rect("pulp-skin", "charcoal", 6, 22, 5, 1)
    p.rect("dry-skin", "wood_dark", 6, 17, 2, 3)
    p.rect("torn-interior", "cream", 8, 19, 3, 3)
    p.dot("torn-interior", "cream", 9, 18)
    p.dot("torn-interior", "cream", 7, 20)
    p.dot("torn-interior", "cream", 10, 22)
    p.dot("torn-notch", "charcoal", 10, 20)
    frames.append(p)

    p = Frame("sootroot-seed", "SootrootSeed",
              "One small diagonal dark kernel with a pale crease; free of soil, packet and roots.",
              "Actual harvested takeable seed that plants SootrootCrop on cultivated soil.",
              "The ground mark and carried seed are different owners. Art does not promise multiple seeds or change valid stack quantities.")
    p.rect("seed-skin", "charcoal", 8, 18, 2, 2)
    p.rect("seed-skin", "charcoal", 7, 19, 3, 3)
    p.rect("seed-skin", "charcoal", 6, 20, 3, 2)
    p.rect("seed-base", "wood_dark", 6, 22, 2, 1)
    p.dot("seed-crease", "cream", 8, 19)
    p.rect("seed-crease", "cream", 7, 20, 1, 2)
    frames.append(p)
    p = Frame("solid-stone-wall", "StoneWall",
              "Full-cell quiet stone courses with staggered dark joints; continuous solid mass at all cell boundaries, no surface-ruin pillar cutouts.",
              "Existing ordinary StoneWall with current owned solid, non-takeable physics and its Wall tag.",
              "Terrain exception: all 16x24 pixels are opaque, including row23, so repeated walls do not imply passable gaps. No 3D cap, invented opening, glade VisualID or added collision.")
    p.rect("solid-stone", "stone_dark", 0, 0, 16, 24)
    p.rect("pale-block", "stone", 8, 0, 8, 7)
    p.rect("pale-block", "stone", 0, 16, 7, 7)
    for y in (7, 15, 23):
        p.rect("horizontal-joint", "mortar", 0, y, 16, 1)
    p.rect("staggered-joint", "mortar", 7, 0, 1, 7)
    p.rect("staggered-joint", "mortar", 15, 8, 1, 7)
    p.rect("staggered-joint", "mortar", 7, 16, 1, 7)
    frames.append(p)
    assert len(frames) == 9
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
    atlas.name = "Native 144x24 cellar atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    materials = {k: emission_material(k + " #" + v, v) for k, v in PALETTE.items()}
    collections = [model(f, materials, atlas, i) for i, f in enumerate(frames)]
    camera_for(atlas, 144, HEIGHT)
    atlas.render.filepath = str(OUT / "opening-cellar.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("Contact 8x cellar geometry")
    camera_for(contact, 160, 128, scale=8)
    vertices, faces = [], []
    add_quad(vertices, faces, 0, 0, 160, 128, -1)
    mesh = bpy.data.meshes.new("Contact background")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(emission_material("Contact background #032F23", "032F23"))
    contact.collection.objects.link(bpy.data.objects.new("Contact background", mesh))
    for i, (frame, collection) in enumerate(zip(frames, collections)):
        col, row = i % 4, i // 4
        instance = bpy.data.objects.new(f"{i:02d} {frame.name}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collection
        instance.location = (col * 40 + 12 - i * WIDTH, 96 - row * 40, 0)
        contact.collection.objects.link(instance)
        add_label(contact, f"{i:02d} {frame.name}", col * 40 + 20, 91 - row * 40, materials["cream"], 1.5)
    for repeat in range(3):
        instance = bpy.data.objects.new(f"Tiled masonry preview {repeat}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collections[8]
        instance.location = (65 + repeat * 16 - 8 * WIDTH, 16, 0)
        contact.collection.objects.link(instance)
    add_label(contact, "CONTINUOUS SOLID MASONRY / THREE CELLS", 89, 11, materials["cut"], 1.5)
    add_label(contact, "GLEANERS' CELLAR / 16 x 24 / REAL OWNERS", 80, 124, materials["cream"], 2)
    add_label(contact, "8x geometry render / native flat clusters / static state frames", 80, 2, materials["cut"], 1.4)
    contact.render.filepath = str(OUT / "opening-cellar-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {
        "schema": 1, "coordinateSystem": "Top-left image origin, Y downward; flat front-facing XY mesh quads.",
        "palette": PALETTE,
        "frames": [{"index": i, "id": f.name, "blueprint": f.blueprint, "stage": f.stage, "cells": f.cells}
                   for i, f in enumerate(frames)],
    }
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {
        "schema": 1, "artifact": "opening-cellar.png", "width": 144, "height": 24,
        "frameWidth": WIDTH, "frameHeight": HEIGHT, "frameCount": 9, "pixelsPerUnit": 16,
        "tilePivotNormalized": [0.5, 0.5], "filterMode": "Point", "wrapMode": "Clamp",
        "mipmaps": False, "compression": "None", "alpha": "Binary; verified independently.",
        "source": "opening-cellar.blend", "generator": "Tools/Live16x24/build_cellar.py",
        "concept": "ArtSource/Live16x24/concepts/cellar-parts-v2.png (reference only; native flat reconstruction)",
        "sceneConcept": "ArtSource/Live16x24/concepts/cellar-paintover-v1.png",
        "captures": ["Docs/Verification/Live16x24/cellar-greybox-" + area + ".png" for area in ("entry", "service", "store")],
        "scope": "Eight static frames for real cellar owners and existing crop stages/products, plus one continuous ordinary masonry terrain frame. No new spawns, animation or runtime integration claim.",
        "sha256": hashlib.sha256((OUT / "opening-cellar.png").read_bytes()).hexdigest(), "frames": [],
    }
    for i, f in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(f.cells) for x, v in enumerate(row) if v]
        colors = sorted({PALETTE[v[1]] for row in f.cells for v in row if v})
        assert max(y for x, y in occupied) == (23 if i == 8 else 22) and 3 <= len(colors) <= 6, (f.name, colors)
        manifest["frames"].append({
            "index": i, "id": f.name, "blueprint": f.blueprint, "stage": f.stage,
            "rect": [i * WIDTH, 0, WIDTH, HEIGHT], "description": f.description,
            "actualUse": f.use, "limits": f.limits, "occupiedPixels": len(occupied), "palette": colors,
            "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                   max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1],
            "baselineTopLeft": 24 if i == 8 else 23, "footprintCells": [1, 1],
            "fullCellTerrain": i == 8,
        })
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "opening-cellar.blend"))
    print("Wrote native cellar atlas, editable geometry, 8x contact and source:", OUT)


if __name__ == "__main__":
    main()
