"""Render twelve original flat 16x24 opening anchors from editable geometry.

Run with /Applications/Blender.app/Contents/MacOS/Blender --background --python
Tools/Live16x24/build_opening_anchors.py. The saved imagegen concept is a visual
reference only. This authoring script never reads, samples or resizes an image.
It renders native PNG and an integer 8x contact directly from the same quads.
Geometry/export setup follows build_opening_base.py; approved art is unchanged.
"""

import hashlib
import json
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/anchors"
WIDTH, HEIGHT, PPU = 16, 24, 16
PALETTE = {
    "hollow": "162E25", "mortar": "233C32", "stone_dark": "3C5148",
    "stone": "526657", "stone_light": "7D8E71", "stone_edge": "A3AE8C",
    "clay": "9C6B32", "clay_patch": "BCA576", "water": "196463",
    "water_edge": "458C7A", "charcoal": "342E25", "wood_dark": "5F4830",
    "wood": "94713D", "wood_edge": "B5A47E", "wood_cut": "C99538",
    "cream": "E4D0A0", "linen": "B5A47E", "olive": "75834B",
    "cap": "C9C39A", "cap_dark": "ABA986", "quartz_dark": "397568",
    "quartz": "70A68A", "quartz_tip": "B4CCAB",
}


class Frame:
    """Integer image coordinates, top-left origin. Each cell is a flat quad."""

    def __init__(self, name, description, owners):
        self.name, self.description, self.owners = name, description, owners
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


def well(repaired):
    state = "sealed" if repaired else "cracked"
    p = Frame(state + "-well", "Broad masonry catch well with fired-clay lining; "
              + ("warm sealed seam, no service-ready glow." if repaired else "one open dark lining split."),
              ["RepairLinedWell:Repairable.Repaired=" + str(repaired).lower()])
    p.rect("round-body", "stone_dark", 2, 13, 12, 8)
    p.rect("round-body", "stone_dark", 4, 11, 8, 12)
    p.rect("side-rim", "stone", 1, 14, 14, 5)
    p.rect("front-masonry", "stone", 3, 18, 10, 4)
    p.rect("front-low-course", "stone", 5, 21, 6, 2)
    p.rect("interior", "hollow", 4, 14, 8, 4)
    p.rect("back-lining", "clay", 4, 13, 8, 2)
    p.rect("left-lining", "clay", 3, 14, 2, 3)
    p.rect("right-lining", "clay", 11, 14, 2, 3)
    p.rect("rear-rim", "stone_light", 5, 11, 6, 1)
    p.rect("left-rim", "stone_light", 2, 13, 3, 1)
    p.rect("right-rim", "stone_light", 11, 13, 3, 1)
    p.rect("left-lip", "stone_light", 2, 17, 3, 1)
    p.rect("front-lip", "stone_light", 4, 18, 8, 1)
    p.rect("right-lip", "stone_light", 12, 17, 2, 1)
    p.rect("left-course-joint", "stone_dark", 4, 20, 1, 2)
    p.rect("right-course-joint", "stone_dark", 11, 19, 1, 2)
    seam = "clay_patch" if repaired else "hollow"
    p.rect(state + "-lining-seam", seam, 7, 17, 2, 3)
    p.rect(state + "-lining-seam", seam, 8, 20, 2, 2)
    p.dot(state + "-lining-seam", seam, 7, 22)
    return p


def basin(filled):
    state = "filled" if filled else "empty"
    p = Frame(state + "-basin", "Shallow stone bank pocket; "
              + ("visible finite water, not a new spring." if filled else "dark depleted bowl; surrounding wet ground is independent."),
              ["SpreadDrawPoint:LiquidPool.LiquidId=water:Volume" + (">0" if filled else "<=0")])
    p.rect("rim-foundation", "stone_dark", 2, 17, 12, 5)
    p.rect("rim-foundation", "stone_dark", 4, 16, 8, 7)
    p.rect("wide-shallow-rim", "stone", 1, 18, 14, 3)
    p.rect("back-rim", "stone_light", 5, 16, 6, 1)
    p.rect("left-rim", "stone_light", 2, 17, 3, 1)
    p.rect("right-rim", "stone_light", 11, 17, 3, 1)
    p.rect("bowl", "water" if filled else "hollow", 4, 18, 8, 3)
    p.rect("bowl", "water" if filled else "hollow", 3, 19, 10, 1)
    p.rect("front-lip", "stone_light", 4, 21, 8, 1)
    if filled:
        p.rect("water-surface-cut", "water_edge", 5, 18, 3, 1)
    return p


def make_frames():
    frames = [well(False), well(True), basin(True), basin(False)]

    p = Frame("stairs", "Ground opening with descending tread cuts; passable, not a standing cabinet or hatch.", ["StairsDown"])
    p.rect("ground-cut", "hollow", 2, 14, 12, 9)
    p.rect("left-worn-lip", "stone", 1, 15, 2, 8)
    p.rect("right-worn-lip", "stone", 13, 15, 2, 8)
    p.rect("back-lip", "stone_light", 3, 14, 10, 1)
    p.rect("far-tread", "mortar", 5, 16, 6, 1)
    p.rect("middle-tread", "stone_dark", 4, 19, 8, 1)
    p.rect("near-tread", "stone", 3, 22, 10, 1)
    p.rect("left-near-wear", "stone_light", 1, 21, 2, 2)
    p.rect("right-near-wear", "stone_light", 13, 21, 2, 2)
    frames.append(p)

    p = Frame("fallen-beam", "One-cell low roof timber with charred left end; moves with the actual haulable owner.", ["FallenBeam"])
    p.rect("beam", "wood_dark", 1, 18, 13, 5)
    p.rect("beam-top", "wood", 2, 17, 11, 2)
    p.rect("beam-face", "wood", 5, 19, 8, 3)
    p.rect("charred-end", "charcoal", 1, 18, 4, 5)
    p.rect("burnt-grain", "charcoal", 5, 21, 3, 1)
    p.rect("broken-end", "wood_cut", 13, 18, 2, 2)
    p.rect("broken-end", "wood_cut", 12, 20, 2, 2)
    p.dot("splinter", "wood_edge", 14, 17)
    p.dot("splinter", "wood_edge", 14, 22)
    frames.append(p)

    p = Frame("chest", "Closed reinforced wood box; no invented open state or visible contents.", ["Chest"])
    p.rect("box", "wood_dark", 2, 14, 12, 9)
    p.rect("lid", "wood", 3, 12, 10, 5)
    p.rect("lid-worn-edge", "wood_edge", 4, 12, 8, 1)
    p.rect("front", "wood", 3, 18, 10, 4)
    p.rect("lid-seam", "charcoal", 3, 16, 10, 1)
    p.rect("left-reinforcement", "stone_dark", 4, 14, 1, 9)
    p.rect("right-reinforcement", "stone_dark", 11, 14, 1, 9)
    p.rect("latch", "stone_dark", 7, 16, 2, 3)
    p.dot("latch-edge", "stone_light", 7, 16)
    frames.append(p)

    p = Frame("barrel", "Rounded staved wood container with two plain hoops; no assumed liquid or open state.", ["WoodenBarrel"])
    p.rect("barrel-outline", "wood_dark", 5, 9, 6, 14)
    p.rect("barrel-outline", "wood_dark", 4, 10, 8, 12)
    p.rect("barrel-bulge", "wood_dark", 3, 13, 10, 7)
    p.rect("staves", "wood", 4, 13, 8, 7)
    p.rect("top", "wood", 5, 10, 6, 2)
    p.rect("stave-left", "wood_dark", 6, 14, 1, 5)
    p.rect("stave-right", "wood_dark", 10, 14, 1, 5)
    p.rect("top-lip", "wood_edge", 5, 9, 6, 1)
    p.rect("upper-hoop", "stone", 4, 12, 8, 2)
    p.rect("upper-hoop-edge", "stone_light", 4, 12, 8, 1)
    p.rect("lower-hoop", "stone", 4, 20, 8, 2)
    p.rect("lower-hoop-edge", "stone_light", 4, 20, 8, 1)
    frames.append(p)

    p = Frame("sign", "Plain weathered board on a narrow post; no fixed arrow or fabricated writing.", ["Signpost"])
    p.rect("post", "wood_dark", 7, 7, 2, 16)
    p.rect("post-wear", "wood", 7, 16, 1, 6)
    p.rect("board", "wood_dark", 2, 9, 12, 7)
    p.rect("board-face", "wood", 2, 9, 12, 6)
    p.rect("board-wear", "wood_edge", 3, 9, 9, 1)
    p.rect("board-grain", "wood_dark", 3, 13, 3, 1)
    p.rect("board-grain", "wood_dark", 10, 12, 3, 1)
    frames.append(p)

    p = Frame("mushrooms", "Three low flat caps with separated stems and open ground; finite passable harvest source.", ["MushroomRing:Harvestable.Harvested=false"])
    p.rect("back-stem", "cap_dark", 7, 17, 1, 3)
    p.rect("left-stem", "cap_dark", 3, 20, 1, 3)
    p.rect("right-stem", "cap_dark", 12, 19, 1, 4)
    for name, x, y in [("back", 7, 15), ("left", 3, 18), ("right", 12, 17)]:
        p.rect(name + "-cap", "cap", x - 2, y + 1, 5, 2)
        p.rect(name + "-cap-top", "cream", x - 1, y, 3, 1)
        p.dot(name + "-cap-side", "cap_dark", x + 2, y + 2)
    p.dot("low-mycelium", "olive", 6, 22).dot("low-mycelium", "olive", 9, 22)
    frames.append(p)

    p = Frame("quartz-vein", "Solid low irregular rock with three angular mineral points; source disappears when harvested.", ["GlowQuartzVein:reference-glade-lit-wall:Harvestable.Harvested=false"])
    p.rect("rock-base", "stone_dark", 2, 19, 12, 4)
    p.rect("rock-back", "stone_dark", 4, 16, 8, 7)
    p.rect("rock-face", "stone", 3, 20, 10, 3)
    p.rect("rock-left", "stone", 2, 19, 4, 3)
    p.rect("rock-mid", "stone", 6, 17, 5, 5)
    p.rect("rock-right", "stone", 11, 19, 3, 3)
    p.rect("left-crystal", "quartz_dark", 3, 15, 2, 5)
    p.rect("left-crystal", "quartz", 3, 15, 1, 4)
    p.dot("left-crystal-tip", "quartz_tip", 3, 14)
    p.rect("high-crystal", "quartz_dark", 7, 11, 3, 8)
    p.rect("high-crystal", "quartz", 7, 10, 2, 7)
    p.dot("high-crystal-tip", "quartz_tip", 7, 9)
    p.rect("right-crystal", "quartz_dark", 11, 16, 2, 5)
    p.rect("right-crystal", "quartz", 12, 14, 1, 5)
    p.dot("right-crystal-tip", "quartz_tip", 12, 13)
    frames.append(p)

    p = Frame("lit-wall", "Full-height continuous masonry course with a narrow muted mineral seam; no crystal source or halo.", ["Wall:reference-glade-lit-wall"])
    p.rect("continuous-course", "stone_dark", 3, 0, 10, 24)
    p.rect("upper-left-block", "stone", 3, 0, 5, 6)
    p.rect("upper-right-block", "stone", 9, 0, 4, 6)
    p.rect("middle-left-block", "stone", 3, 7, 3, 7)
    p.rect("middle-right-block", "stone", 7, 7, 6, 7)
    p.rect("lower-block", "stone", 3, 15, 10, 8)
    p.rect("upper-worn-edge", "stone_light", 3, 0, 5, 1)
    p.rect("middle-worn-edge", "stone_light", 8, 7, 5, 1)
    p.rect("lower-worn-edge", "stone_light", 3, 15, 4, 1)
    p.rect("seam", "quartz_dark", 8, 4, 1, 6)
    p.rect("seam", "quartz", 7, 10, 1, 7)
    p.rect("seam", "quartz_dark", 6, 16, 1, 5)
    p.dot("seam-turn", "quartz", 8, 9)
    frames.append(p)
    assert len(frames) == 12
    for frame in frames:
        colors = {value[1] for row in frame.cells for value in row if value}
        assert 3 <= len(colors) <= 7, (frame.name, colors)
        assert any(frame.cells[22]), frame.name
    for a, b in [(0, 1), (2, 3)]:
        assert [[bool(v) for v in row] for row in frames[a].cells] == [[bool(v) for v in row] for row in frames[b].cells]
        assert frames[a].cells != frames[b].cells
    assert all(frames[11].cells[y][3] and frames[11].cells[y][12] for y in range(24))
    return frames


def linear(byte):
    value = byte / 255
    return value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4


def emission_material(name, hex_color):
    material = bpy.data.materials.new(name)
    rgba = tuple(linear(int(hex_color[i:i + 2], 16)) for i in (0, 2, 4)) + (1,)
    material.diffuse_color = rgba
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = rgba
    output = nodes.new("ShaderNodeOutputMaterial")
    material.node_tree.links.new(emission.outputs[0], output.inputs["Surface"])
    return material


def add_quad(vertices, faces, x0, y0, x1, y1, z=0):
    base = len(vertices)
    vertices.extend(((x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)))
    faces.append((base, base + 1, base + 2, base + 3))


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
        obj["authoring"] = "Original flat pixel geometry, one authored grid unit per native pixel."
        obj["paletteHex"] = PALETTE[color]
        collection.objects.link(obj)
    return collection


def camera_for(scene, width, height, scale=1):
    data = bpy.data.cameras.new(scene.name + " camera")
    obj = bpy.data.objects.new(data.name, data)
    scene.collection.objects.link(obj)
    data.type = "ORTHO"
    data.ortho_scale = max(width, height)
    obj.location = (width / 2, height / 2, 100)
    scene.camera = obj
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 1
    scene.cycles.use_denoising = False
    scene.render.resolution_x = width * scale
    scene.render.resolution_y = height * scale
    scene.render.resolution_percentage = 100
    scene.render.pixel_aspect_x = scene.render.pixel_aspect_y = 1
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.film_transparent = True
    scene.render.filter_size = 0.01
    scene.render.dither_intensity = 0
    scene.render.use_stamp = False
    scene.render.use_stamp_date = False
    scene.render.use_stamp_time = False
    scene.render.use_stamp_render_time = False
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0
    scene.view_settings.gamma = 1


def add_label(scene, text, x, y, material, size=2.0):
    curve = bpy.data.curves.new(text, "FONT")
    curve.body = text
    curve.align_x = "CENTER"
    curve.size = size
    curve.materials.append(material)
    label = bpy.data.objects.new(text, curve)
    label.location = (x, y, 1)
    scene.collection.objects.link(label)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    atlas_scene = bpy.context.scene
    atlas_scene.name = "Native 192x24 anchors atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    materials = {key: emission_material(key + " #" + value, value) for key, value in PALETTE.items()}
    collections = [model(frame, materials, atlas_scene, index) for index, frame in enumerate(frames)]
    camera_for(atlas_scene, WIDTH * len(frames), HEIGHT)
    atlas_scene.render.filepath = str(OUT / "opening-anchors.png")
    bpy.ops.render.render(write_still=True, scene=atlas_scene.name)

    # Same meshes at exactly eight screen pixels per authored native pixel.
    # This is a direct Blender render, not raster cropping or image resampling.
    contact = bpy.data.scenes.new("Contact sheet 8x native geometry")
    camera_for(contact, 160, 128, scale=8)
    background = emission_material("Contact background #032F23", "032F23")
    vertices, faces = [], []
    add_quad(vertices, faces, 0, 0, 160, 128, -1)
    mesh = bpy.data.meshes.new("Contact background")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(background)
    backdrop = bpy.data.objects.new("Contact background", mesh)
    contact.collection.objects.link(backdrop)
    for index, (frame, collection) in enumerate(zip(frames, collections)):
        column, row = index % 4, index // 4
        instance = bpy.data.objects.new(f"{index:02d} {frame.name}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collection
        instance.location = (column * 40 + 12 - index * WIDTH, 96 - row * 40, 0)
        contact.collection.objects.link(instance)
        add_label(contact, f"{index:02d} {frame.name}", column * 40 + 20, 91 - row * 40, materials["cream"])
    add_label(contact, "LIVE OPENING / 16 x 24 / FUNCTIONAL ANCHORS", 80, 124, materials["cream"], 2.2)
    add_label(contact, "8x geometry render / no animation / original approved PNGs unchanged", 80, 3, materials["stone_light"], 1.5)
    contact.render.filepath = str(OUT / "opening-anchors-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {
        "schema": 1,
        "coordinateSystem": "Top-left image coordinates, y downward; cells become front-facing XY quads.",
        "palette": PALETTE,
        "frames": [{"index": i, "id": frame.name, "cells": frame.cells} for i, frame in enumerate(frames)],
    }
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {
        "schema": 1,
        "artifact": "opening-anchors.png",
        "width": 192,
        "height": 24,
        "frameWidth": WIDTH,
        "frameHeight": HEIGHT,
        "frameCount": len(frames),
        "pixelsPerUnit": PPU,
        "tilePivotNormalized": [0.5, 0.5],
        "filterMode": "Point",
        "alpha": "Binary; validated from the Blender PNG separately.",
        "animation": "None. Twelve static owner/state frames. Repair and supply pairs are state variants, not animation.",
        "source": "opening-anchors.blend",
        "generator": "Tools/Live16x24/build_opening_anchors.py",
        "concept": "ArtSource/Live16x24/concepts/anchor-parts-v1.png (visual reference only)",
        "sha256": hashlib.sha256((OUT / "opening-anchors.png").read_bytes()).hexdigest(),
        "frames": [],
    }
    for index, frame in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(frame.cells) for x, value in enumerate(row) if value]
        colors = sorted({PALETTE[value[1]] for row in frame.cells for value in row if value})
        manifest["frames"].append({
            "index": index, "id": frame.name, "rect": [index * WIDTH, 0, WIDTH, HEIGHT],
            "owners": frame.owners, "footprintCells": [1, 1],
            "description": frame.description, "occupiedPixels": len(occupied), "palette": colors,
            "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                   max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1],
        })
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "opening-anchors.blend"))
    print("Wrote native twelve-frame atlas, 8x geometry contact sheet, editable blend and source manifests:", OUT)


if __name__ == "__main__":
    main()
