"""Original flat geometry for the four remaining opening cast base frames.

Run with Blender --background --python Tools/Live16x24/build_opening_cast.py.
Only ArtSource/Live16x24/cast is written. No raster is read, sampled, resized,
cropped or traced. Named pixel clusters are directly authored as mesh quads.
Render helpers are imported read-only from the accepted base generator; its
main function is never called and no earlier asset/source is changed.
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
OUT = ROOT / "ArtSource/Live16x24/cast"
WIDTH, HEIGHT = 16, 24

PALETTE = {
    "ink": "342E25",
    "leather": "5F4830",
    "cream": "E4D0A0",
    "linen": "B5A47E",
    "olive": "75834B",
    "forest": "53683E",
    "tawny": "A97542",
    "fur_dark": "7C522F",
    "fur_light": "C59156",
    "stone_dark": "3C5148",
    "stone": "526657",
    "stone_light": "7D8E71",
    "claw": "BCA576",
    "rust": "9B442B",
}


class Frame:
    def __init__(self, name, blueprint, description):
        self.name, self.blueprint, self.description = name, blueprint, description
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


def make_frames():
    frames = []

    p = Frame("villager", "Villager", "Unhelmeted civilian in a sloped olive work tunic; cream cloth, uncovered profile, lowered hands and separated feet.")
    p.rect("hair-crown", "leather", 6, 3, 4, 2)
    p.rect("hair-side", "leather", 5, 5, 3, 3)
    p.rect("face", "cream", 8, 5, 3, 4)
    p.dot("profile-nose", "cream", 11, 7)
    p.dot("eye", "leather", 10, 6)
    p.rect("neck", "linen", 8, 9, 2, 1)
    p.rect("sloped-shoulder", "olive", 6, 10, 5, 1)
    p.rect("upper-tunic", "olive", 4, 11, 8, 4)
    p.rect("tunic-shadow", "forest", 4, 15, 8, 5)
    p.rect("left-sleeve", "olive", 3, 12, 2, 5)
    p.rect("left-hand", "cream", 3, 17, 2, 2)
    p.rect("right-sleeve", "forest", 12, 13, 1, 5)
    p.dot("right-hand", "cream", 12, 18)
    p.rect("front-cloth", "cream", 8, 10, 3, 10)
    p.rect("fold", "linen", 10, 13, 1, 6)
    p.rect("waist-tie", "leather", 5, 15, 6, 1)
    p.rect("left-tunic-fold", "olive", 5, 17, 2, 3)
    p.rect("left-leg", "leather", 5, 20, 2, 3)
    p.rect("right-leg", "leather", 10, 20, 2, 3)
    p.dot("left-toe", "leather", 4, 22)
    p.dot("right-toe", "leather", 12, 22)
    frames.append(p)

    p = Frame("pet-dog", "PetDog", "Low horizontal tawny dog with a blunt muzzle, single floppy ear, raised bent tail and open belly/leg gaps; no collar prop.")
    p.rect("tail-base", "tawny", 2, 17, 2, 2)
    p.rect("raised-tail", "tawny", 1, 14, 1, 4)
    p.dot("tail-bend", "fur_light", 2, 13)
    p.rect("tail-tip", "cream", 2, 12, 2, 1)
    p.rect("body", "tawny", 3, 17, 8, 3)
    p.rect("back", "fur_light", 4, 16, 6, 1)
    p.rect("rump", "tawny", 2, 18, 3, 3)
    p.rect("chest", "tawny", 9, 17, 3, 4)
    p.rect("head", "tawny", 9, 14, 4, 4)
    p.rect("forehead", "fur_light", 10, 14, 3, 1)
    p.rect("ear", "fur_dark", 8, 15, 2, 3)
    p.dot("ear-tip", "fur_dark", 9, 18)
    p.rect("short-muzzle", "cream", 12, 16, 2, 2)
    p.dot("nose", "ink", 14, 16)
    p.dot("eye", "ink", 11, 15)
    p.rect("chest-patch", "cream", 11, 18, 1, 3)
    p.rect("far-hind-leg", "fur_dark", 5, 20, 1, 3)
    p.rect("near-hind-leg", "tawny", 3, 20, 1, 3)
    p.rect("far-front-leg", "fur_dark", 9, 20, 1, 3)
    p.rect("near-front-leg", "tawny", 11, 21, 1, 2)
    p.dot("near-hind-foot", "fur_dark", 4, 22)
    p.dot("near-front-foot", "fur_dark", 12, 22)
    frames.append(p)

    p = Frame("marlback-scrabbler", "MarlbackScrabbler", "Compact hunched shale-backed burrower, short blunt face below plates, broad hind foot and two asymmetrical grasping foreclaws.")
    p.rect("body", "tawny", 3, 16, 9, 4)
    p.rect("arched-shell-shadow", "stone_dark", 4, 10, 7, 7)
    p.rect("arched-shell-shadow", "stone_dark", 3, 12, 10, 6)
    p.rect("upper-plate", "stone", 5, 9, 5, 4)
    p.rect("upper-plate-edge", "stone_light", 6, 8, 3, 1)
    p.dot("upper-plate-edge", "stone_light", 5, 9)
    p.rect("left-plate", "stone", 2, 13, 4, 5)
    p.rect("left-plate-edge", "stone_light", 3, 12, 3, 1)
    p.rect("right-plate", "stone", 9, 12, 4, 4)
    p.rect("right-plate-edge", "stone_light", 10, 11, 2, 1)
    p.rect("lower-plate-shadow", "stone_dark", 5, 16, 6, 4)
    p.rect("lower-plate", "stone", 6, 16, 4, 3)
    p.rect("lower-plate-edge", "stone_light", 6, 16, 2, 1)
    p.rect("blunt-face", "tawny", 12, 17, 3, 2)
    p.dot("eye", "ink", 13, 17)
    p.dot("nose", "ink", 14, 18)
    p.rect("broad-hind-leg", "tawny", 2, 18, 3, 4)
    p.rect("hind-foot", "tawny", 1, 22, 4, 1)
    p.dot("hind-toe", "claw", 1, 22)
    p.rect("near-forearm", "tawny", 8, 19, 2, 2)
    p.rect("near-wrist", "tawny", 9, 20, 2, 1)
    p.rect("near-claw", "claw", 9, 21, 1, 2)
    p.dot("near-claw", "claw", 11, 21)
    p.rect("far-forearm", "tawny", 13, 19, 1, 2)
    p.rect("far-claw", "claw", 13, 21, 2, 1)
    p.dot("far-claw", "claw", 14, 22)
    frames.append(p)

    p = Frame("marlback-gleaner", "MarlbackGleaner", "Taller forward-leaning member of the shale-backed species; stepped plate ridge, rust seams and one reaching foreclaw. No permanent equipment.")
    p.rect("body-seam", "rust", 6, 7, 4, 13)
    p.rect("rear-body", "tawny", 3, 14, 4, 7)
    p.rect("upper-plate-shadow", "stone_dark", 5, 4, 4, 5)
    p.rect("upper-plate", "stone", 5, 3, 3, 5)
    p.rect("upper-edge", "stone_light", 5, 3, 2, 1)
    p.rect("middle-plate-shadow", "stone_dark", 4, 8, 6, 6)
    p.rect("middle-plate", "stone", 4, 8, 4, 5)
    p.rect("middle-edge", "stone_light", 4, 7, 3, 1)
    p.rect("front-plate-shadow", "stone_dark", 9, 10, 4, 5)
    p.rect("front-plate", "stone", 9, 10, 3, 4)
    p.rect("front-edge", "stone_light", 10, 9, 2, 1)
    p.rect("lower-plate-shadow", "stone_dark", 2, 14, 6, 5)
    p.rect("lower-plate", "stone", 2, 14, 4, 4)
    p.rect("lower-edge", "stone_light", 3, 13, 3, 1)
    p.rect("rust-joint", "rust", 8, 14, 2, 4)
    p.rect("blunt-face", "tawny", 11, 14, 3, 3)
    p.dot("eye", "ink", 12, 14)
    p.dot("nose", "ink", 13, 16)
    p.rect("bent-hind-leg", "tawny", 3, 18, 2, 3)
    p.rect("hind-foot", "tawny", 2, 21, 4, 2)
    p.dot("hind-toe", "claw", 2, 22)
    p.rect("planted-forearm", "tawny", 8, 18, 2, 4)
    p.rect("planted-claw", "claw", 8, 22, 3, 1)
    p.rect("reaching-shoulder", "rust", 10, 17, 2, 2)
    p.rect("reaching-arm", "tawny", 12, 18, 1, 2)
    p.rect("reaching-wrist", "tawny", 13, 19, 1, 2)
    p.rect("reaching-claw", "claw", 14, 20, 1, 3)
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
    atlas.name = "Native 64x24 cast atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    assert [f.blueprint for f in frames] == ["Villager", "PetDog", "MarlbackScrabbler", "MarlbackGleaner"]
    materials = {key: emission_material(key + " #" + value, value) for key, value in PALETTE.items()}
    collections = [model(frame, materials, atlas, index) for index, frame in enumerate(frames)]
    camera_for(atlas, 64, HEIGHT)
    atlas.render.filepath = str(OUT / "opening-cast.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("Contact sheet 8x cast geometry")
    camera_for(contact, 128, 44, scale=8)
    background = emission_material("Contact background #032F23", "032F23")
    vertices, faces = [], []
    add_quad(vertices, faces, 0, 0, 128, 44, -1)
    mesh = bpy.data.meshes.new("Contact background")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(background)
    backdrop = bpy.data.objects.new("Contact background", mesh)
    contact.collection.objects.link(backdrop)
    for index, (frame, collection) in enumerate(zip(frames, collections)):
        instance = bpy.data.objects.new(f"{index:02d} {frame.name}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collection
        instance.location = (index * 32 + 8 - index * WIDTH, 12, 0)
        contact.collection.objects.link(instance)
        add_label(contact, f"{index:02d} {frame.name}", index * 32 + 16, 7, materials["cream"], 1.7)
    add_label(contact, "LIVE OPENING / CAST BASE FRAMES / 16 x 24", 64, 40, materials["cream"], 2)
    add_label(contact, "8x geometry render / equipment-neutral static poses / no animation", 64, 2, materials["linen"], 1.4)
    contact.render.filepath = str(OUT / "opening-cast-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {
        "schema": 1, "coordinateSystem": "Image coordinates: top-left origin, Y downward; front-facing XY mesh quads.",
        "palette": PALETTE,
        "frames": [{"index": i, "id": frame.name, "cells": frame.cells} for i, frame in enumerate(frames)],
    }
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {
        "schema": 1, "artifact": "opening-cast.png", "width": 64, "height": 24,
        "frameWidth": WIDTH, "frameHeight": HEIGHT, "frameCount": 4,
        "pixelsPerUnit": 16, "tilePivotNormalized": [0.5, 0.5], "filterMode": "Point",
        "alpha": "Binary; native PNG validated separately.",
        "animation": "None. Four distinct living base subjects, not animation frames.",
        "source": "opening-cast.blend", "generator": "Tools/Live16x24/build_opening_cast.py",
        "renderHelper": "Tools/Live16x24/build_opening_base.py (read-only import, no earlier assets changed)",
        "concept": "ArtSource/Live16x24/concepts/cast-parts-v1.png (visual reference only)",
        "sha256": hashlib.sha256((OUT / "opening-cast.png").read_bytes()).hexdigest(), "frames": [],
    }
    for index, frame in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(frame.cells) for x, value in enumerate(row) if value]
        colors = sorted({PALETTE[value[1]] for row in frame.cells for value in row if value})
        assert 3 <= len(colors) <= 7
        assert max(y for x, y in occupied) == 22
        manifest["frames"].append({
            "index": index, "id": frame.name, "blueprint": frame.blueprint,
            "rect": [index * WIDTH, 0, WIDTH, HEIGHT], "footprintCells": [1, 1],
            "description": frame.description, "occupiedPixels": len(occupied), "palette": colors,
            "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                   max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1],
            "baselineTopLeft": 23,
        })
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "opening-cast.blend"))
    print("Wrote native four-frame cast atlas, editable geometry, 8x contact and manifests:", OUT)


if __name__ == "__main__":
    main()
