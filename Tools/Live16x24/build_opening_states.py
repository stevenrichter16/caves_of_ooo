"""Author twelve truthful opening-state frames as original Blender geometry.

Run: Blender --background --python Tools/Live16x24/build_opening_states.py
Writes only ArtSource/Live16x24/states. No raster reference is opened, sampled,
cropped, edited or downscaled. Each authored XY cell renders one native pixel.
The accepted base generator supplies render helpers through a read-only import.
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

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/states"
WIDTH, HEIGHT = 16, 24
PALETTE = {
    "ink": "342E25", "leather": "5F4830", "timber": "92703D",
    "cut": "BCA576", "cream": "E4D0A0", "linen": "B5A47E",
    "stone": "526657", "stone_edge": "7D8E71",
    "forest": "53683E", "leaf": "75834B",
    "ochre": "C99538", "ochre_dark": "9C6B32",
    "ember": "9B442B", "flame": "D57932", "gold": "E8B34B",
}


class Frame:
    def __init__(self, name, blueprint, state, description):
        self.name, self.blueprint, self.state, self.description = name, blueprint, state, description
        self.cells = [[None] * WIDTH for _ in range(HEIGHT)]

    def rect(self, part, color, x, y, width, height):
        assert color in PALETTE
        assert width > 0 and height > 0
        assert 0 <= x < x + width <= WIDTH and 0 <= y < y + height <= HEIGHT
        for row in range(y, y + height):
            for col in range(x, x + width):
                self.cells[row][col] = (part, color)
        return self

    def dot(self, part, color, x, y):
        return self.rect(part, color, x, y, 1, 1)

    def variant(self, name, state, description):
        result = Frame(name, self.blueprint, state, description)
        result.cells = copy.deepcopy(self.cells)
        return result


def wicket_posts():
    p = Frame("wicket-buckled", "GleanersBuckledWicket", "RepairablePart.BlocksFunction(owner)",
              "The same two timber posts enclose sagging broken rails; damage does not imply a passable gap.")
    for x in (1, 13):
        p.rect("post", "timber", x, 9, 2, 14)
        p.rect("post-cap", "cut", x, 9, 2, 1)
        p.rect("post-side", "leather", x + 1, 12, 1, 10)
    return p


def fire_ring():
    p = Frame("campfire-cold", "Campfire", "No BurningEffect; finite temperature below 150 degrees",
              "Low stone ring and dark charred sticks; no ember, flame or implied cooking availability.")
    p.rect("left-stone", "stone", 1, 19, 3, 2)
    p.rect("left-stone-edge", "stone_edge", 1, 19, 3, 1)
    p.rect("right-stone", "stone", 12, 19, 3, 2)
    p.rect("right-stone-edge", "stone_edge", 12, 19, 3, 1)
    p.rect("rear-left-stone", "stone", 4, 17, 3, 2)
    p.rect("rear-left-edge", "stone_edge", 4, 17, 3, 1)
    p.rect("rear-right-stone", "stone", 10, 17, 2, 2)
    p.rect("rear-right-edge", "stone_edge", 10, 17, 2, 1)
    p.rect("front-left-stone", "stone", 4, 21, 3, 2)
    p.rect("front-left-edge", "stone_edge", 4, 21, 3, 1)
    p.rect("front-right-stone", "stone", 9, 21, 3, 2)
    p.rect("front-right-edge", "stone_edge", 9, 21, 3, 1)
    p.rect("charred-stick", "ink", 5, 19, 6, 1)
    p.rect("charred-stick", "ink", 6, 20, 3, 1)
    p.dot("stick-end", "leather", 7, 18)
    p.dot("stick-end", "leather", 8, 21)
    return p


def make_frames():
    frames = []
    buckled = wicket_posts()
    buckled.rect("broken-top-rail", "timber", 3, 12, 2, 2)
    buckled.rect("broken-top-rail", "timber", 5, 13, 2, 2)
    buckled.rect("broken-top-rail", "timber", 7, 14, 1, 2)
    buckled.rect("broken-top-rail", "timber", 10, 14, 1, 2)
    buckled.rect("broken-top-rail", "timber", 11, 12, 2, 2)
    buckled.dot("exposed-break", "cut", 7, 15)
    buckled.dot("exposed-break", "cut", 10, 14)
    buckled.rect("sagging-lower-rail", "timber", 3, 18, 4, 2)
    buckled.rect("sagging-lower-rail", "timber", 7, 19, 3, 2)
    buckled.rect("broken-lower-end", "timber", 11, 18, 2, 2)
    buckled.dot("exposed-break", "cut", 9, 20)
    frames.append(buckled)

    closed = wicket_posts()
    closed.name, closed.state = "wicket-closed", "Function restored and DoorPart.IsClosed"
    closed.description = "Identical posts, continuous straight rails; repaired does not mean open."
    for y in (12, 18):
        closed.rect("straight-rail", "timber", 3, y, 10, 2)
        closed.rect("rail-edge", "cut", 3, y, 10, 1)
    closed.dot("timber-latch", "leather", 11, 15)
    frames.append(closed)

    opened = wicket_posts()
    opened.name, opened.state = "wicket-open", "Function restored and not DoorPart.IsClosed"
    opened.description = "Identical posts with rails folded narrowly beside the right hinge; unobstructed center."
    opened.rect("folded-rails", "timber", 11, 12, 1, 9)
    opened.rect("folded-rail-edge", "cut", 12, 12, 1, 9)
    opened.dot("hinge", "leather", 12, 14)
    opened.dot("hinge", "leather", 12, 19)
    frames.append(opened)

    cold = fire_ring()
    frames.append(cold)
    hot = cold.variant("campfire-hot-coals", "No BurningEffect; finite ThermalPart.Temperature >= CookingService.MinimumFiniteCookingTemperature (150f)",
                       "Identical stone ring and sticks, with three sparse embers. Stored heat does not promise available fuel or cooking readiness.")
    hot.dot("ember", "ember", 6, 19)
    hot.dot("ember", "ember", 9, 19)
    hot.dot("ember", "gold", 8, 20)
    frames.append(hot)
    fire = cold.variant("campfire-burning", "Actual BurningEffect on the current owner",
                        "Same ring and sticks beneath a distinct compact flame. No halo, smoke, projected light or animation frames.")
    fire.dot("flame-tip", "flame", 8, 9)
    fire.rect("flame-tongue", "flame", 7, 10, 2, 3)
    fire.rect("flame-tongue", "flame", 6, 13, 4, 3)
    fire.rect("flame-base", "flame", 7, 16, 3, 1)
    fire.rect("flame-base", "flame", 7, 17, 2, 3)
    fire.rect("flame-core", "gold", 7, 13, 1, 4)
    fire.rect("flame-core", "gold", 8, 16, 1, 3)
    frames.append(fire)

    seed = Frame("drawgourd-seed", "DrawgourdCrop", "Owned CropPart.GrowthStage == 0",
                 "Three planted seed marks and low soil divots. Soil moisture belongs to the independent ground layer.")
    for x, y in ((3, 21), (7, 22), (11, 21)):
        seed.rect("soil-divot", "leather", x, y, 2, 1)
        seed.dot("seed", "cut", x + 1, y - 1)
    seed.dot("soil-edge", "timber", 6, 20)
    frames.append(seed)

    sprout = Frame("drawgourd-sprout", "DrawgourdCrop", "Owned CropPart.GrowthStage == 1",
                   "Two olive leaves on a short stem, no fruit and no wilted or falsely ripe variant.")
    sprout.rect("soil-divot", "leather", 5, 22, 7, 1)
    sprout.rect("stem", "forest", 8, 17, 1, 5)
    sprout.rect("left-leaf", "leaf", 4, 16, 3, 2)
    sprout.rect("left-leaf-joint", "forest", 6, 18, 2, 1)
    sprout.rect("right-leaf", "leaf", 10, 15, 3, 2)
    sprout.rect("right-leaf-joint", "forest", 9, 17, 2, 1)
    frames.append(sprout)

    ripe = Frame("drawgourd-ripe", "DrawgourdCrop", "Owned CropPart.GrowthStage == 2 and HarvestAtMaturity",
                 "Olive vine bears one bulbous hard gourd with an unmistakable narrow vessel neck; no lantern, flower or grain head.")
    ripe.rect("soil-divot", "leather", 3, 22, 10, 1)
    ripe.rect("vine", "forest", 6, 10, 1, 12)
    ripe.rect("left-branch", "forest", 4, 14, 2, 1)
    ripe.rect("left-leaf", "leaf", 2, 13, 3, 3)
    ripe.rect("upper-leaf", "leaf", 4, 8, 3, 2)
    ripe.rect("fruit-branch", "forest", 7, 11, 4, 1)
    ripe.rect("right-leaf", "leaf", 11, 9, 3, 2)
    ripe.dot("right-leaf-joint", "forest", 11, 11)
    ripe.rect("gourd-neck", "ochre", 10, 12, 1, 4)
    ripe.dot("neck-rim", "cream", 10, 12)
    ripe.rect("gourd-shoulder", "ochre", 9, 16, 3, 1)
    ripe.rect("gourd-body", "ochre", 8, 17, 5, 4)
    ripe.rect("gourd-base", "ochre", 9, 21, 3, 1)
    ripe.rect("gourd-side", "ochre_dark", 12, 18, 1, 3)
    ripe.rect("gourd-edge", "cream", 8, 17, 1, 2)
    frames.append(ripe)

    torch = Frame("torch-unlit", "Torch", "Owned controlled-light switch is off, with no destructive BurningEffect",
                  "Loose diagonal wooden torch with a pale wrapped head; shaft and head are unchanged in its lit state.")
    for step in range(8):
        torch.rect("shaft", "leather", 3 + step, 21 - step, 2, 2)
        torch.dot("shaft-edge", "timber", 3 + step, 21 - step)
    torch.rect("wrapped-head", "linen", 9, 13, 3, 4)
    torch.rect("wrapped-head", "linen", 10, 12, 3, 3)
    torch.rect("wrapped-head", "cream", 11, 11, 2, 2)
    torch.rect("wrap-band", "cream", 9, 15, 2, 1)
    torch.rect("wrap-band", "cream", 10, 13, 2, 1)
    frames.append(torch)
    lit = torch.variant("torch-lit", "Actual owned TorchLight/LightSource.Enabled with usable heat/fuel, or actual destructive BurningEffect",
                        "Same loose shaft and wrap with a small controlled flame; not a planted post or wearer-only light.")
    lit.dot("flame-tip", "flame", 12, 5)
    lit.rect("flame-tongue", "flame", 11, 6, 2, 2)
    lit.rect("flame-body", "flame", 10, 8, 4, 2)
    lit.rect("flame-base", "flame", 11, 10, 2, 1)
    lit.rect("flame-core", "gold", 11, 8, 1, 2)
    lit.dot("flame-core", "gold", 12, 10)
    frames.append(lit)

    skin = Frame("waterskin", "Waterskin", "Actual loose current owner with WaterskinPart; neutral silhouette for any charge count",
                 "Small soft leather pouch with narrow neck and loose dark strap. No contents, water-source icon or implied full state.")
    skin.rect("neck", "leather", 6, 10, 2, 3)
    skin.rect("neck-rim", "cut", 6, 10, 2, 1)
    skin.rect("strap", "ink", 8, 11, 3, 1)
    skin.rect("strap", "ink", 11, 12, 1, 2)
    skin.rect("strap", "ink", 12, 14, 1, 2)
    skin.rect("strap", "ink", 13, 16, 1, 5)
    skin.rect("strap", "ink", 11, 21, 3, 1)
    skin.rect("pouch-shoulder", "leather", 5, 13, 4, 2)
    skin.rect("soft-pouch", "leather", 4, 15, 6, 6)
    skin.rect("soft-bulge", "leather", 10, 18, 1, 3)
    skin.rect("rounded-bottom", "leather", 5, 21, 5, 2)
    skin.rect("pouch-fold", "timber", 8, 16, 1, 5)
    skin.rect("soft-edge", "cut", 5, 13, 1, 3)
    skin.dot("soft-edge", "cut", 4, 16)
    frames.append(skin)
    assert len(frames) == 12
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
        mesh = bpy.data.meshes.new(f"{frame.name}/{part}")
        mesh.from_pydata(vertices, [], faces)
        mesh.materials.append(materials[color])
        mesh.update()
        obj = bpy.data.objects.new(part, mesh)
        obj["authoring"] = "Original flat pixel geometry; one authored unit per native pixel."
        obj["paletteHex"] = PALETTE[color]
        collection.objects.link(obj)
    return collection


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    atlas = bpy.context.scene
    atlas.name = "Native 192x24 opening state atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    materials = {key: emission_material(key + " #" + value, value) for key, value in PALETTE.items()}
    collections = [model(frame, materials, atlas, index) for index, frame in enumerate(frames)]
    camera_for(atlas, 192, HEIGHT)
    atlas.render.filepath = str(OUT / "opening-states.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("Contact sheet 8x state geometry")
    camera_for(contact, 160, 128, scale=8)
    vertices, faces = [], []
    add_quad(vertices, faces, 0, 0, 160, 128, -1)
    mesh = bpy.data.meshes.new("Contact background")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(emission_material("Contact background #032F23", "032F23"))
    contact.collection.objects.link(bpy.data.objects.new("Contact background", mesh))
    for index, (frame, collection) in enumerate(zip(frames, collections)):
        col, row = index % 4, index // 4
        instance = bpy.data.objects.new(f"{index:02d} {frame.name}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collection
        instance.location = (col * 40 + 12 - index * WIDTH, 96 - row * 40, 0)
        contact.collection.objects.link(instance)
        add_label(contact, f"{index:02d} {frame.name}", col * 40 + 20, 91 - row * 40, materials["cream"], 1.5)
    add_label(contact, "LIVE OPENING / 16 x 24 / REAL OBJECT STATES", 80, 124, materials["cream"], 2)
    add_label(contact, "8x geometry render / static state variants / no readiness icons", 80, 2, materials["linen"], 1.4)
    contact.render.filepath = str(OUT / "opening-states-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {"schema": 1, "coordinateSystem": "Top-left image origin, Y downward; original front-facing XY mesh quads.",
              "palette": PALETTE, "frames": [{"index": i, "id": f.name, "cells": f.cells} for i, f in enumerate(frames)]}
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {
        "schema": 1, "artifact": "opening-states.png", "width": 192, "height": 24,
        "frameWidth": WIDTH, "frameHeight": HEIGHT, "frameCount": 12, "pixelsPerUnit": 16,
        "tilePivotNormalized": [0.5, 0.5], "filterMode": "Point", "alpha": "Binary, verified independently.",
        "animation": "None. Twelve authoritative object-state illustrations, not animation frames.",
        "source": "opening-states.blend", "generator": "Tools/Live16x24/build_opening_states.py",
        "renderHelper": "Tools/Live16x24/build_opening_base.py (read-only import)",
        "concept": "ArtSource/Live16x24/concepts/state-parts-v1.png (visual reference only)",
        "sha256": hashlib.sha256((OUT / "opening-states.png").read_bytes()).hexdigest(), "frames": [],
    }
    for i, frame in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(frame.cells) for x, v in enumerate(row) if v]
        colors = sorted({PALETTE[v[1]] for row in frame.cells for v in row if v})
        assert 3 <= len(colors) <= 6, (frame.name, colors)
        assert max(y for x, y in occupied) == 22
        manifest["frames"].append({
            "index": i, "id": frame.name, "blueprint": frame.blueprint, "state": frame.state,
            "rect": [i * WIDTH, 0, WIDTH, HEIGHT], "footprintCells": [1, 1], "description": frame.description,
            "occupiedPixels": len(occupied), "palette": colors,
            "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                   max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1],
            "baselineTopLeft": 23,
        })
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "opening-states.blend"))
    print("Wrote native twelve-frame state atlas, editable geometry, 8x contact and manifests:", OUT)


if __name__ == "__main__":
    main()
