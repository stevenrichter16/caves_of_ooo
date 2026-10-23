"""Author and render the first live-opening base frames with Blender.

Run from any directory:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python \
      Tools/Live16x24/build_opening_base.py

These are original flat pixel geometries, designed while looking at the saved
imagegen parts reference and approved project sprites. No raster is read,
sampled, cropped, resized, or traced by this script. Every authored cell is an
editable front-facing mesh quad. Blender renders the native atlas directly.
The enlarged contact sheet renders the same geometry on an integer 8x grid.

The emission/linear-color setup follows Tools/Clearing16x24/build_kit.py;
the earlier kit and approved PNGs are never modified or imported.
"""

import hashlib
import json
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/base"
WIDTH, HEIGHT, PPU = 16, 24, 16

PALETTE = {
    "ink": "342E25",
    "leather": "5F4830",
    "cream": "E4D0A0",
    "linen": "B5A47E",
    "ochre": "C99538",
    "gold": "E8B34B",
    "ochre_dark": "9C6B32",
    "forest": "53683E",
    "forest_dark": "3E5035",
    "rust": "9B442B",
    "rust_light": "B85834",
    "mortar": "233C32",
    "stone_dark": "3C5148",
    "stone": "526657",
    "stone_light": "7D8E71",
    "low_stone": "788D73",
    "low_edge": "A3AE8C",
    "moss": "586638",
    "reed_stem": "757D49",
    "reed_dark": "647244",
    "reed": "ABA986",
    "reed_light": "C9C39A",
    "leaf_dark": "435A35",
    "leaf": "75834B",
    "leaf_light": "96A462",
    "grain_stem": "806033",
    "grain": "D8A446",
    "grain_dark": "A58242",
    "cut": "BCA576",
}


class Frame:
    """Named geometry cells in image coordinates (Y increases downward)."""

    def __init__(self, name, description, owners):
        self.name = name
        self.description = description
        self.owners = owners
        self.cells = [[None] * WIDTH for _ in range(HEIGHT)]

    def rect(self, part, color, x, y, width, height):
        assert color in PALETTE, color
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

    p = Frame("player", "Equipment-neutral ochre-hooded traveler; separated boots, cream front and open face.", ["Player"])
    p.rect("hood-outline", "ochre_dark", 5, 2, 6, 7)
    p.rect("hood-crown", "ochre", 6, 1, 4, 2)
    p.rect("hood-crown", "gold", 6, 2, 3, 2)
    p.rect("hood-brow", "ochre", 4, 4, 8, 2)
    p.rect("hood-brow", "gold", 5, 3, 6, 2)
    p.rect("face-opening", "ink", 6, 5, 4, 3)
    p.rect("face", "cream", 7, 6, 2, 2)
    p.rect("left-hood", "ochre", 4, 6, 2, 3)
    p.rect("right-hood", "gold", 10, 6, 1, 2)
    p.rect("collar", "leather", 6, 9, 5, 1)
    p.rect("coat", "ochre", 5, 10, 7, 8)
    p.rect("coat-hem", "ochre", 4, 17, 8, 3)
    p.rect("left-sleeve", "gold", 3, 11, 2, 6)
    p.rect("left-cuff", "leather", 3, 17, 2, 2)
    p.rect("right-sleeve", "gold", 12, 12, 1, 7)
    p.rect("right-hand", "cream", 12, 17, 1, 2)
    p.rect("front-linen", "cream", 7, 10, 3, 10)
    p.rect("left-coat-fold", "ochre_dark", 5, 13, 1, 5)
    p.rect("left-leg", "leather", 5, 20, 2, 2)
    p.rect("right-leg", "leather", 10, 20, 2, 2)
    p.rect("left-boot", "leather", 4, 22, 3, 1)
    p.rect("right-boot", "leather", 10, 22, 3, 1)
    frames.append(p)

    p = Frame("dark-wall", "Three tall flat masonry masses; dark broken ruin, no bevelled cubes or baked light.", ["Wall:reference-glade-dark-ruin"])
    p.rect("back-mortar", "mortar", 5, 6, 5, 15)
    p.rect("upper-stone", "stone_dark", 2, 2, 8, 6)
    p.rect("upper-face", "stone", 3, 3, 6, 5)
    p.rect("upper-worn-edge", "stone_light", 3, 2, 5, 1)
    p.rect("middle-stone", "stone_dark", 7, 9, 7, 6)
    p.rect("middle-face", "stone", 8, 10, 5, 4)
    p.rect("middle-worn-edge", "stone_light", 8, 9, 4, 1)
    p.rect("lower-stone", "stone_dark", 2, 16, 11, 7)
    p.rect("lower-left-face", "stone", 3, 17, 4, 5)
    p.rect("lower-right-face", "stone", 9, 17, 3, 6)
    p.rect("lower-worn-edge", "stone_light", 3, 16, 4, 1)
    p.dot("moss", "moss", 9, 7).rect("moss", "moss", 12, 14, 2, 2)
    p.rect("moss", "moss", 1, 22, 2, 1)
    frames.append(p)

    p = Frame("low-wall", "Half-height stone course with a visible top break; one cell, solid only when its owner is solid.", ["Wall:reference-glade-low-wall"])
    p.rect("left-stone", "stone", 1, 14, 7, 9)
    p.rect("left-face", "low_stone", 2, 15, 5, 7)
    p.rect("left-worn-edge", "low_edge", 2, 13, 5, 2)
    p.rect("right-stone", "stone", 9, 13, 5, 10)
    p.rect("right-face", "low_stone", 10, 14, 3, 8)
    p.rect("right-worn-edge", "low_edge", 10, 12, 3, 2)
    p.rect("small-break", "stone_dark", 7, 19, 2, 4)
    p.dot("moss", "moss", 1, 22).rect("moss", "moss", 13, 21, 2, 2)
    frames.append(p)

    p = Frame("tall-reeds", "Three narrow straw-grey reed blades, irregular heights and open stem gaps; not ripe grain.", ["Reeds:reference-glade-pale-reeds:variant0"])
    p.rect("left-stem", "reed_stem", 3, 11, 1, 12)
    p.rect("middle-stem", "reed_stem", 7, 5, 1, 18)
    p.rect("right-stem", "reed_dark", 12, 10, 1, 13)
    p.rect("left-blade", "reed", 2, 9, 2, 6).dot("left-tip", "reed_light", 3, 8)
    p.rect("middle-blade", "reed", 7, 3, 2, 7).rect("middle-edge", "reed_light", 7, 3, 1, 5)
    p.dot("middle-tip", "reed_light", 7, 2)
    p.rect("right-blade", "reed", 12, 8, 2, 6).rect("right-edge", "reed_light", 13, 8, 1, 3)
    p.dot("left-leaf", "reed_dark", 2, 18).dot("left-leaf", "reed_stem", 1, 17)
    p.rect("middle-bent-leaf", "reed_dark", 5, 15, 2, 1).dot("middle-bent-leaf", "reed_stem", 4, 14)
    p.rect("right-leaf", "reed_stem", 13, 18, 2, 1).dot("right-leaf", "reed_stem", 14, 17)
    frames.append(p)

    p = Frame("short-reeds", "Two bent straw blades with a cut remnant; lower sparse companion to tall reeds.", ["Reeds:reference-glade-pale-reeds:variant1"])
    p.rect("left-stem", "reed_dark", 4, 14, 1, 9)
    p.rect("middle-stem", "reed_stem", 8, 10, 1, 13)
    p.rect("cut-stem", "reed_dark", 12, 19, 1, 4)
    p.rect("left-blade", "reed", 3, 12, 2, 5).dot("left-tip", "reed_light", 4, 11)
    p.rect("middle-blade", "reed", 8, 9, 2, 5).rect("middle-edge", "reed_light", 9, 8, 1, 4)
    p.rect("cut-edge", "reed", 12, 18, 2, 1)
    p.rect("low-leaf", "reed_stem", 5, 19, 3, 1).dot("low-leaf", "reed_stem", 6, 18)
    p.rect("bent-leaf", "reed_dark", 9, 16, 2, 1).dot("bent-leaf", "reed_stem", 10, 15)
    p.dot("fallen-base", "reed_dark", 2, 22).dot("fallen-base", "reed_dark", 14, 22)
    frames.append(p)

    p = Frame("low-bush", "Low forked olive sprig with a dark center and open edges; no flowers or opaque shrub mass.", ["Bush:reference-glade-green-grass"])
    p.rect("main-stem", "leaf_dark", 7, 17, 2, 6)
    p.rect("upright-leaf", "leaf", 7, 14, 1, 6).dot("upright-tip", "leaf_light", 6, 13)
    p.rect("left-leaf", "leaf_dark", 4, 17, 2, 3).rect("left-tip", "leaf", 3, 16, 2, 2)
    p.dot("left-point", "leaf_light", 2, 15)
    p.rect("right-leaf", "leaf", 10, 17, 2, 4).rect("right-tip", "leaf_light", 11, 15, 2, 2)
    p.rect("left-foot", "leaf", 5, 21, 2, 2).dot("left-foot", "leaf_dark", 4, 21)
    p.rect("right-foot", "leaf_dark", 9, 21, 3, 2)
    frames.append(p)

    p = Frame("rubble", "Three low flat stone fragments with generous gaps; no flower-colored dots and no standing column.", ["Rubble:reference-glade-gravel", "BrokenColumn:reference-glade-dark-ruin"])
    p.rect("left-stone", "stone_dark", 2, 19, 4, 4)
    p.rect("left-face", "stone", 3, 19, 3, 3).rect("left-edge", "stone_light", 3, 18, 2, 1)
    p.rect("middle-stone", "stone_dark", 8, 16, 4, 4)
    p.rect("middle-face", "stone", 8, 16, 3, 3).rect("middle-edge", "stone_light", 8, 15, 2, 1)
    p.rect("right-stone", "stone_dark", 11, 21, 4, 2)
    p.rect("right-face", "stone", 12, 20, 2, 2).dot("right-edge", "stone_light", 12, 19)
    frames.append(p)

    p = Frame("ripe-grain", "Three distinct golden emberwheat heads on narrow stems; only unharvested native field rows.", ["RipeCropRow:FieldHarvest.Harvested=false"])
    p.rect("left-stem", "grain_stem", 4, 13, 1, 10)
    p.rect("middle-stem", "grain_stem", 8, 9, 1, 14)
    p.rect("right-stem", "grain_stem", 12, 14, 1, 9)
    for x, y in [(4, 9), (8, 4), (12, 10)]:
        p.rect("grain-spine", "grain", x, y, 1, 7)
        p.dot("grain-tip", "cream", x, y)
        # Alternating kernels leave silhouette notches. Reeds have a narrow
        # unbroken blade instead; these must differ in shape, not only hue.
        p.dot("left-kernel", "grain", x - 1, y + 1)
        p.dot("right-kernel", "grain", x + 1, y + 2)
        p.dot("left-kernel", "grain", x - 1, y + 3)
        p.dot("right-kernel", "grain", x + 1, y + 4)
        p.dot("left-kernel", "grain_dark", x - 1, y + 5)
    p.rect("left-leaf", "leaf", 5, 18, 2, 1).dot("left-leaf", "leaf", 6, 17)
    p.rect("right-leaf", "leaf", 9, 20, 3, 1).dot("right-leaf", "leaf", 10, 19)
    frames.append(p)

    p = Frame("stubble", "Three blunt cut stalks, substantially shorter and duller than ripe heads; never a growth stage promise.", ["CropRow", "RipeCropRow:FieldHarvest.Harvested=true"])
    p.rect("left-cut-stem", "grain_stem", 4, 18, 1, 5)
    p.rect("middle-cut-stem", "grain_stem", 8, 16, 1, 7)
    p.rect("right-cut-stem", "grain_stem", 12, 19, 1, 4)
    p.dot("cut-end", "cut", 4, 17).dot("cut-end", "cut", 8, 15).dot("cut-end", "cut", 12, 18)
    p.dot("broken-leaf", "grain_dark", 5, 21).dot("broken-leaf", "grain_dark", 7, 22)
    p.dot("broken-leaf", "grain_dark", 11, 21)
    frames.append(p)

    p = Frame("warden", "Equipment-neutral forest-coated warden with flat cap, cream front and rust shoulder cloth; no baked weapon.", ["Warden"])
    p.rect("cap-crown", "forest", 5, 2, 6, 3)
    p.rect("cap-light", "leaf", 5, 2, 3, 1)
    p.rect("cap-brim", "forest_dark", 3, 5, 9, 1)
    p.rect("face-outline", "ink", 5, 6, 5, 3)
    p.rect("face", "cream", 6, 6, 3, 3).dot("brow", "ink", 8, 6)
    p.rect("collar", "linen", 5, 9, 6, 1)
    p.rect("coat-back", "forest_dark", 4, 11, 9, 10)
    p.rect("left-coat", "forest", 4, 11, 3, 9)
    p.rect("right-coat", "forest", 10, 13, 2, 8)
    p.rect("front-linen", "cream", 7, 10, 3, 11)
    p.rect("front-fold", "linen", 9, 15, 1, 5)
    p.rect("left-sleeve", "forest", 3, 12, 1, 7)
    p.dot("left-hand", "cream", 3, 19)
    p.rect("rust-shoulder", "rust", 10, 10, 3, 3)
    p.rect("rust-drape", "rust", 12, 12, 2, 4)
    p.rect("rust-edge", "rust_light", 11, 10, 2, 1)
    p.rect("coat-hem", "forest_dark", 4, 20, 3, 1)
    p.rect("left-leg", "leather", 5, 21, 2, 2)
    p.rect("right-leg", "leather", 10, 21, 2, 2)
    p.rect("left-boot", "leather", 4, 22, 3, 1)
    p.rect("right-boot", "leather", 10, 22, 3, 1)
    frames.append(p)
    assert len(frames) == 10
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
    atlas_scene.name = "Native 160x24 atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    materials = {key: emission_material(key + " #" + value, value) for key, value in PALETTE.items()}
    collections = [model(frame, materials, atlas_scene, index) for index, frame in enumerate(frames)]
    camera_for(atlas_scene, WIDTH * len(frames), HEIGHT)
    atlas_scene.render.filepath = str(OUT / "opening-base.png")
    bpy.ops.render.render(write_still=True, scene=atlas_scene.name)

    # Same meshes at exactly eight screen pixels per authored native pixel.
    # This is a direct Blender render, not raster cropping or image resampling.
    contact = bpy.data.scenes.new("Contact sheet 8x native geometry")
    camera_for(contact, 160, 88, scale=8)
    background = emission_material("Contact background #032F23", "032F23")
    vertices, faces = [], []
    add_quad(vertices, faces, 0, 0, 160, 88, -1)
    mesh = bpy.data.meshes.new("Contact background")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(background)
    backdrop = bpy.data.objects.new("Contact background", mesh)
    contact.collection.objects.link(backdrop)
    for index, (frame, collection) in enumerate(zip(frames, collections)):
        column, row = index % 5, index // 5
        instance = bpy.data.objects.new(f"{index:02d} {frame.name}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collection
        instance.location = (column * 32 + 8 - index * WIDTH, 56 - row * 40, 0)
        contact.collection.objects.link(instance)
        add_label(contact, f"{index:02d} {frame.name}", column * 32 + 16, 51 - row * 40, materials["cream"])
    add_label(contact, "LIVE OPENING / 16 x 24 / BASE FRAMES", 80, 84, materials["cream"], 2.2)
    add_label(contact, "8x geometry render / no animation / original approved PNGs unchanged", 80, 3, materials["reed"], 1.5)
    contact.render.filepath = str(OUT / "opening-base-contact.png")
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
        "artifact": "opening-base.png",
        "width": 160,
        "height": 24,
        "frameWidth": WIDTH,
        "frameHeight": HEIGHT,
        "frameCount": len(frames),
        "pixelsPerUnit": PPU,
        "tilePivotNormalized": [0.5, 0.5],
        "filterMode": "Point",
        "alpha": "Binary; validated from the Blender PNG separately.",
        "animation": "None. Ten distinct static base subjects, not animation frames.",
        "source": "opening-base.blend",
        "generator": "Tools/Live16x24/build_opening_base.py",
        "concept": "ArtSource/Live16x24/concepts/base-parts-v1.png (visual reference only)",
        "sha256": hashlib.sha256((OUT / "opening-base.png").read_bytes()).hexdigest(),
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
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "opening-base.blend"))
    print("Wrote native ten-frame atlas, 8x geometry contact sheet, editable blend and source manifests:", OUT)


if __name__ == "__main__":
    main()
