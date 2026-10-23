"""Build twelve native loose-item sprites from original Blender pixel geometry.

Run Blender --background --python Tools/Live16x24/build_opening_items.py.
Writes only ArtSource/Live16x24/items. Concept images are visual references;
this script never opens, samples, crops, resizes or edits a raster. Every flat
colored cell is editable mesh geometry rendered directly at native resolution.
The accepted base generator supplies camera/material helpers read-only.
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
OUT = ROOT / "ArtSource/Live16x24/items"
WIDTH, HEIGHT = 16, 24
PALETTE = {
    "ink": "342E25", "leather": "5F4830", "cream": "E4D0A0", "linen": "B5A47E",
    "grain_stem": "806033", "grain": "D8A446", "ochre": "C99538", "ochre_dark": "9C6B32",
    "cap": "C9C39A", "cap_dark": "ABA986", "olive": "75834B", "wood": "94713D", "cut": "BCA576",
    "quartz_dark": "397568", "quartz": "70A68A", "quartz_tip": "B4CCAB",
    "rust": "9B442B", "rust_light": "B85834", "tawny": "A97542",
    "stone_dark": "3C5148", "stone": "526657", "stone_light": "7D8E71",
}
BLUEPRINTS = ("Emberwheat", "ToastedEmberwheat", "DrawgourdShell", "DrawgourdSeed",
              "Mushroom", "RoastedMushroom", "GlowQuartz", "HealingTonic", "DriedMeat",
              "FireClay", "SalvagedTimber", "MarlbackCorpse")


class Frame:
    def __init__(self, name, blueprint, description, source, use, limits):
        self.name, self.blueprint, self.description = name, blueprint, description
        self.source, self.use, self.limits = source, use, limits
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
    p = Frame("emberwheat", "Emberwheat", "A short cut sheaf with notched grain heads and stems gathered into one tie; no standing soil or growing leaves.",
              "Three opening ripe field rows yield one unit each; packed first, overflow left on the cut row.",
              "Eat for 2d4 healing or cook into toasted emberwheat.",
              "No LightSourcePart. No planted-row identity, light halo, or promise of more than the real stack count.")
    p.rect("left-cut-stem", "grain_stem", 5, 13, 1, 5)
    p.rect("left-cut-stem", "grain_stem", 6, 17, 1, 3)
    p.rect("middle-cut-stem", "grain_stem", 8, 11, 1, 12)
    p.rect("right-cut-stem", "grain_stem", 11, 14, 1, 3)
    p.rect("right-cut-stem", "grain_stem", 10, 16, 1, 3)
    p.rect("right-cut-stem", "grain_stem", 9, 18, 1, 4)
    for x, y in ((5, 10), (8, 8), (11, 12)):
        p.rect("grain-spine", "grain", x, y, 1, 5)
        p.dot("grain-tip", "cream", x, y)
        p.dot("grain-left-kernel", "grain", x - 1, y + 1)
        p.dot("grain-right-kernel", "grain", x + 1, y + 2)
        p.dot("grain-left-kernel", "grain", x - 1, y + 3)
    p.rect("sheaf-tie", "linen", 6, 19, 4, 1)
    p.dot("tie-knot", "linen", 7, 20).dot("cut-end", "grain", 9, 22)
    frames.append(p)

    p = Frame("toasted-grain", "ToastedEmberwheat", "Dry cut grain with bent browned tips and a shorter gathered stem; distinct outline as well as color from raw grain.",
              "Cooking carried Emberwheat at a valid hot station replaces it with the same quantity of carried cooked food.",
              "Eat for 3d4 healing and +20 heat resistance for 100 actor turns, replacing the previous prepared meal.",
              "No flame, smoke, glowing coals, bread loaf or decorative duplicate beside the station.")
    p.rect("left-cut-stem", "ochre_dark", 5, 15, 1, 3).rect("left-cut-stem", "ochre_dark", 6, 17, 1, 3)
    p.rect("middle-cut-stem", "ochre_dark", 8, 12, 1, 11)
    p.rect("right-cut-stem", "ochre_dark", 10, 16, 1, 3).rect("right-cut-stem", "ochre_dark", 9, 18, 1, 4)
    p.rect("middle-head", "grain", 8, 10, 1, 6)
    p.rect("bent-middle-tip", "leather", 7, 9, 2, 2)
    p.dot("middle-kernel", "ochre_dark", 9, 12).dot("middle-kernel", "grain", 7, 13)
    p.dot("middle-kernel", "grain", 9, 14)
    p.rect("left-head", "grain", 5, 13, 1, 4).rect("bent-left-tip", "leather", 3, 12, 3, 1)
    p.dot("left-kernel", "ochre_dark", 4, 14).dot("left-kernel", "grain", 6, 15)
    p.rect("right-head", "grain", 11, 13, 1, 4).rect("bent-right-tip", "leather", 11, 12, 2, 1)
    p.dot("right-kernel", "ochre_dark", 12, 14).dot("right-kernel", "grain", 10, 15)
    p.rect("sheaf-tie", "linen", 6, 19, 4, 1).dot("tie-knot", "linen", 7, 20)
    p.dot("cut-end", "grain", 9, 22)
    frames.append(p)

    p = Frame("drawgourd-shell", "DrawgourdShell", "A detached golden hard-shell vessel, open dark neck above a rounded base; no vine, water surface or painted fill level.",
              "The opening ripe DrawgourdCrop releases one shell and one seed; Harvest leaves both on the bed, Gather tries to pack them.",
              "Reusable water-only vessel: capacity three; fresh shells are empty. Fill, drink, water crops, transfer or douse through real actions.",
              "Not stackable. One neutral silhouette for any valid charge count; no spring, automatic refill or implied water supply.")
    p.rect("neck", "ochre", 6, 12, 3, 5)
    p.rect("open-rim", "cream", 6, 11, 3, 1)
    p.dot("neck-opening", "ink", 7, 12)
    p.rect("shoulders", "ochre", 5, 16, 5, 2)
    p.rect("shell-body", "ochre", 4, 18, 7, 3)
    p.rect("shell-base", "ochre", 5, 21, 5, 2)
    p.rect("shell-edge", "cream", 4, 18, 1, 2)
    p.rect("shell-side", "ochre_dark", 10, 18, 1, 3)
    p.dot("shell-bottom-cut", "ochre_dark", 9, 22)
    frames.append(p)

    p = Frame("drawgourd-seed", "DrawgourdSeed", "One small pointed cream seed with a flat dry crease; deliberately much smaller than the vessel and no tilled soil behind it.",
              "One seed accompanies the ripe opening gourd's harvested shell.",
              "Consume a carried seed to plant an empty prepared bed, underfoot or adjacent.",
              "A loose seed is not an already planted crop or a packet promising several seeds.")
    p.rect("seed-crown", "cream", 7, 19, 3, 1)
    p.rect("seed-body", "cream", 6, 20, 5, 2)
    p.rect("seed-underside", "linen", 7, 22, 3, 1)
    p.dot("seed-point", "linen", 6, 22)
    p.dot("seed-crease", "leather", 9, 21)
    frames.append(p)

    p = Frame("mushroom", "Mushroom", "One broad pale cap above its short attached stem, separate from the three-cap living mushroom ring.",
              "Opening rings at (34,19) and (50,21) each yield two to four; source ring is removed, produce packs first or overflows.",
              "Eat for 1d4 healing or cook into RoastedMushroom.",
              "No poison, light, colony regrowth or multiple-unit promise is painted into this loose cap.")
    p.rect("cap-crown", "cap", 6, 15, 5, 1)
    p.rect("cap", "cap", 5, 16, 7, 2)
    p.rect("cap-brim", "cap", 4, 18, 9, 1)
    p.rect("cap-gill", "cap_dark", 5, 19, 7, 1)
    p.rect("stem", "olive", 8, 20, 2, 3)
    p.dot("stem-edge", "cream", 8, 20)
    frames.append(p)

    p = Frame("roasted-mushroom", "RoastedMushroom", "Browned cap with up-curled ends and shortened cream stem; preserves the mushroom identity but changes its rim silhouette.",
              "Cooking carried Mushroom creates carried cooked food atomically, never an extra food owner beside the fire.",
              "Eat for 3d4 healing and +20 acid resistance for 100 actor turns, replacing the previous prepared meal.",
              "No poison, live fire, steam or stacked-meal promise.")
    p.rect("browned-crown", "leather", 6, 15, 5, 2)
    p.rect("toasted-cap", "ochre_dark", 5, 17, 7, 2)
    p.rect("left-curl", "cap", 4, 17, 1, 2)
    p.rect("right-curl", "cap", 12, 16, 1, 3)
    p.rect("cap-rim", "cap", 5, 19, 7, 1)
    p.rect("stem", "linen", 8, 20, 2, 3)
    p.dot("stem-root", "linen", 7, 22)
    frames.append(p)

    p = Frame("glow-quartz", "GlowQuartz", "One oblique detached mineral shard with a short broken base; no three-pronged rock source or surrounding glow.",
              "The two opening quartz veins yield one or two units each and disappear when harvested; items pack first or overflow.",
              "Forge infusion or consume one via CrackQuartz to create a separate temporary ground light.",
              "Raw GlowQuartz has no LightSourcePart. CrackedGlowQuartz is a different non-takeable thirty-turn owner and is outside this atlas.")
    p.dot("shard-tip", "quartz_tip", 9, 14)
    p.rect("upper-shard", "quartz", 8, 15, 2, 2)
    p.rect("middle-shard", "quartz", 7, 17, 3, 2)
    p.rect("lower-shard", "quartz", 6, 19, 3, 3)
    p.rect("broken-base", "quartz_dark", 7, 22, 2, 1)
    p.rect("single-face", "quartz_dark", 9, 17, 1, 2)
    p.rect("single-face", "quartz_dark", 8, 19, 1, 3)
    p.dot("broken-cut", "quartz_tip", 6, 21)
    frames.append(p)

    p = Frame("healing-tonic", "HealingTonic", "Small cream medicine vessel with a brown stopper and worn rust paper band; opaque contents and no animated magic glow.",
              "One in the opening chest at (51,8); two in authored starting builds. Loose only after a real drop or transfer.",
              "Consume by applying the 4d6+4 healing dose, or throw its real tonic payload.",
              "No reusable empty-bottle owner, cure-all icon or transparent fill-level promise.")
    p.rect("stopper", "leather", 7, 13, 2, 2)
    p.rect("bottle-neck", "linen", 7, 15, 2, 2)
    p.rect("bottle-shoulders", "cream", 6, 17, 4, 1)
    p.rect("bottle", "cream", 5, 18, 6, 4)
    p.rect("bottle-base", "linen", 6, 22, 4, 1)
    p.rect("paper-band", "rust", 5, 19, 6, 2)
    p.dot("worn-paper", "cream", 5, 20)
    frames.append(p)

    p = Frame("dried-meat", "DriedMeat", "Two irregular dry red-brown strips tied at one end; separated lower ends, no bones or fresh blood pool.",
              "One in the opening chest; two in starting builds; later visible through an actual drop or throw.",
              "Eat for 3d4 healing; eligible hunting furrowstalkers can consume a ground unit as a finite diversion.",
              "No promise that opening PetDog or Marlbacks obey bait. Throwing at a creature remains an attack.")
    p.rect("tie", "linen", 6, 12, 4, 1)
    p.dot("tie-loop", "linen", 5, 11).dot("tie-loop", "linen", 10, 11)
    p.rect("tie-knot", "linen", 7, 13, 2, 2)
    p.rect("left-strip", "rust", 6, 15, 2, 3).rect("left-strip", "rust", 5, 18, 2, 3)
    p.rect("left-strip", "rust", 4, 21, 3, 2)
    p.rect("left-dry-edge", "leather", 6, 18, 1, 3).dot("left-cut", "rust_light", 4, 22)
    p.rect("right-strip", "rust", 9, 15, 2, 3).rect("right-strip", "rust", 10, 18, 2, 5)
    p.rect("right-dry-edge", "leather", 11, 19, 1, 3).dot("right-cut", "rust_light", 10, 22)
    frames.append(p)

    p = Frame("fire-clay", "FireClay", "Compact uneven pressed red-clay lump with one matte clay-color ridge and flat dark underside; no flame or light.",
              "Two finite measures in the connected cellar supply crate; visible in the opening only after bringing and dropping them.",
              "Two seal the opening well; one can smother existing flames without adding wetness. Other authored repairs use their own quantities.",
              "Not a burning coal, ore vein, explosive or lasting fire-protection effect. Clay provenance remains gameplay data.")
    p.rect("clay-top", "rust_light", 6, 17, 3, 1)
    p.rect("clay-body", "rust", 4, 18, 7, 4)
    p.rect("left-lump", "rust", 3, 20, 2, 2)
    p.rect("right-lump", "rust", 10, 20, 3, 2)
    p.rect("pressed-crease", "leather", 8, 20, 2, 1)
    p.rect("clay-bottom", "leather", 5, 22, 6, 1)
    p.cells[21][3] = p.cells[21][12] = None
    frames.append(p)

    p = Frame("salvaged-timber", "SalvagedTimber", "Two short offset split wooden pieces with blunt cut ends and a clear horizontal gap; flat planks, not cylindrical logs or a full pallet.",
              "Dismantling the finite cellar timber pallet yields two units, packed first with genuine overflow if needed.",
              "Repair supported wooden structures/gear, jam eligible traps, shape a haft, or add ten fuel to a finite cooking fire.",
              "Art is an identity marker, not a count display. Feeding fuel does not ignite the fire. No new wood resource is created by drawing it.")
    p.rect("upper-plank", "wood", 2, 17, 10, 2)
    p.rect("upper-cut", "cut", 2, 17, 1, 2)
    p.rect("upper-splinter", "wood", 9, 16, 3, 1).dot("upper-end", "cut", 11, 16)
    p.rect("upper-grain", "leather", 6, 18, 3, 1)
    p.rect("lower-plank", "wood", 5, 21, 9, 2)
    p.rect("lower-split", "wood", 7, 20, 3, 1).dot("lower-splinter", "wood", 4, 22)
    p.dot("lower-cut", "cut", 13, 21).rect("lower-grain", "leather", 7, 22, 3, 1)
    frames.append(p)

    p = Frame("marlback-corpse", "MarlbackCorpse", "Low collapsed shale plates, folded tawny limbs and short pale claws, with an off-center blunt head; no upright living pose or embedded equipment.",
              "Actual opening Scrabbler/Gleaner deaths have a seventy-percent corpse chance, subject to corpse suppression; corpse records SourceBlueprint and SourceID.",
              "Carryable remains, weight ten. The unchanged native entity owns its persistence and interactions.",
              "No default HarvestablePart, FoodPart, decay timer or stack. No promised meat, minerals, resurrection or fresh gore decal.")
    p.rect("collapsed-back", "stone_dark", 4, 16, 7, 5)
    p.rect("broad-plate", "stone", 5, 16, 6, 3)
    p.rect("broad-plate-edge", "stone_light", 5, 15, 5, 1)
    p.rect("left-plate", "stone", 3, 18, 3, 3).rect("left-edge", "stone_light", 3, 17, 3, 1)
    p.rect("right-plate", "stone", 10, 19, 3, 2).rect("right-edge", "stone_light", 11, 18, 2, 1)
    p.rect("folded-rear-limb", "tawny", 2, 21, 4, 1).rect("folded-rear-foot", "tawny", 3, 22, 2, 1)
    p.dot("rear-claw", "cut", 1, 21)
    p.rect("lower-joint", "rust", 6, 20, 2, 2)
    p.rect("folded-near-limb", "tawny", 8, 21, 2, 1).rect("near-claw", "cut", 8, 22, 2, 1)
    p.rect("blunt-head", "tawny", 12, 21, 3, 1).dot("jaw", "tawny", 13, 22)
    p.rect("folded-far-limb", "tawny", 10, 22, 3, 1).dot("far-claw", "cut", 14, 22)
    frames.append(p)
    assert tuple(f.blueprint for f in frames) == BLUEPRINTS
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
    atlas.name = "Native 192x24 loose-item atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    materials = {k: emission_material(k + " #" + v, v) for k, v in PALETTE.items()}
    collections = [model(f, materials, atlas, i) for i, f in enumerate(frames)]
    camera_for(atlas, 192, HEIGHT)
    atlas.render.filepath = str(OUT / "opening-items.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("Contact 8x loose-item geometry")
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
        add_label(contact, f"{i:02d} {frame.name}", col * 40 + 20, 91 - row * 40, materials["cream"], 1.55)
    add_label(contact, "LIVE OPENING / 16 x 24 / LOOSE ITEMS", 80, 124, materials["cream"], 2)
    add_label(contact, "8x geometry render / actual owners only / no extra spawns or quantity promises", 80, 2, materials["linen"], 1.35)
    contact.render.filepath = str(OUT / "opening-items-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {"schema": 1, "coordinateSystem": "Top-left image origin, Y downward; flat front-facing XY mesh quads.",
              "palette": PALETTE, "frames": [{"index": i, "id": f.name, "blueprint": f.blueprint, "cells": f.cells} for i, f in enumerate(frames)]}
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {
        "schema": 1, "artifact": "opening-items.png", "width": 192, "height": 24,
        "frameWidth": WIDTH, "frameHeight": HEIGHT, "frameCount": 12, "pixelsPerUnit": 16,
        "tilePivotNormalized": [0.5, 0.5], "filterMode": "Point", "alpha": "Binary; verified independently.",
        "source": "opening-items.blend", "generator": "Tools/Live16x24/build_opening_items.py",
        "concept": "ArtSource/Live16x24/concepts/loose-items-v1.png (reference only; native flat reconstruction)",
        "capture": "Docs/Verification/Live16x24/native-states-gourd-harvest.png",
        "scope": "Static identity frames for twelve real loose items. Actual visible top owner only. No new spawns, animation, stack-count artwork or runtime integration claim.",
        "sha256": hashlib.sha256((OUT / "opening-items.png").read_bytes()).hexdigest(), "frames": [],
    }
    for i, f in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(f.cells) for x, v in enumerate(row) if v]
        colors = sorted({PALETTE[v[1]] for row in f.cells for v in row if v})
        assert max(y for x, y in occupied) == 22 and 3 <= len(colors) <= 6, (f.name, colors)
        manifest["frames"].append({
            "index": i, "id": f.name, "blueprint": f.blueprint, "rect": [i * WIDTH, 0, WIDTH, HEIGHT],
            "description": f.description, "realSource": f.source, "actualUse": f.use, "limits": f.limits,
            "occupiedPixels": len(occupied), "palette": colors,
            "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                   max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1],
            "baselineTopLeft": 23, "footprintCells": [1, 1],
        })
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "opening-items.blend"))
    print("Wrote native item atlas, editable geometry, 8x contact and source:", OUT)


if __name__ == "__main__":
    main()
