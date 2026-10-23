"""Original native geometry for seven portable north-works goods.

Run Blender --background --python Tools/Live16x24/build_north_works_products.py.
Writes only ArtSource/Live16x24/north-works-products. No raster is opened,
sampled or edited. Existing helpers supply integer meshes and flat materials.
"""
import hashlib
import json
from pathlib import Path
import sys

import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_opening_base import add_label, camera_for, emission_material
from build_opening_items import Frame, model, PALETTE as ITEM_PALETTE
from build_north_plot import instance, plane

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/north-works-products"
PALETTE = {key: ITEM_PALETTE[key] for key in (
    "leather", "linen", "ochre", "ochre_dark", "olive", "rust", "rust_light",
    "stone_dark", "stone", "stone_light", "quartz")}


def make_frames():
    frames = []
    p = Frame("stoneburr-seed", "StoneburrSeed", "One dry knuckled ochre shell with a few short bent hooks and an irregular dry edge.",
              "Actual StoneburrPatch harvest produces this portable reagent; source disappears when spent.",
              "Reagent binding:2 participates in existing brewing, including stoneskin tonic.",
              "No SeedPart: the name does not make this plantable. No seed packet, plant stem, face or quantity promise.")
    p.dot("shell-top-hook", "ochre_dark", 8, 16)
    p.rect("shell-top-neck", "ochre", 7, 17, 2, 1)
    p.rect("shell-body", "ochre", 6, 18, 4, 4)
    p.rect("shell-left-hook", "ochre_dark", 5, 17, 1, 2)
    p.rect("shell-right-hook", "ochre", 10, 18, 2, 1)
    p.dot("shell-right-tip", "ochre_dark", 11, 17)
    p.dot("shell-low-hook", "ochre_dark", 10, 20)
    p.rect("shell-left-foot", "ochre_dark", 5, 21, 2, 1)
    p.rect("shell-bottom", "ochre_dark", 7, 22, 2, 1)
    p.rect("shell-dry-face", "linen", 6, 18, 2, 1)
    p.dot("shell-edge", "ochre_dark", 9, 21)
    frames.append(p)

    p = Frame("frost-lichen", "FrostLichen", "A small low detached crust with blunt sage frond edges and muted cold flecks.",
              "Harvested from a real FrostLichenPatch; detached portion is smaller than the living broad crust.",
              "Reagent cold:2 participates in the existing brewing rules.",
              "No crystal points, snowflake, stars, light, snow aura, rooted plant or wider source-patch silhouette.")
    p.rect("frond-upper", "stone_light", 6, 18, 2, 1)
    p.rect("frond-left", "stone_light", 5, 19, 4, 3)
    p.rect("frond-right", "stone_light", 9, 20, 3, 2)
    p.dot("frond-right-tip", "stone_light", 10, 19)
    p.rect("frond-low-edge", "stone", 6, 22, 4, 1)
    p.rect("frond-cold-face", "quartz", 6, 19, 2, 1)
    p.dot("frond-cold-right", "quartz", 10, 20)
    p.dot("frond-low-fleck", "stone", 5, 21)
    frames.append(p)

    p = Frame("mendleaf-sprig", "MendleafSprig", "A detached short forked stem with two broad olive leaves and one quiet vein on each.",
              "Real MendleafPlant harvest yields the raw sprig.",
              "Reagent vital:1; two carried sprigs can be prepared near a still into one ConcentratedMendleaf.",
              "Not a rooted three-leaf plant, ready healing potion, bandage, full branch or extra medicinal effect.")
    p.rect("cut-stem-shaft", "leather", 8, 17, 1, 6)
    p.dot("cut-stem-left-join", "leather", 7, 17)
    p.rect("cut-stem-base", "leather", 7, 22, 2, 1)
    p.dot("leaf-left-tip", "olive", 4, 12)
    p.rect("leaf-left-upper", "olive", 4, 13, 3, 2)
    p.rect("leaf-left-lower", "olive", 5, 15, 3, 2)
    p.dot("leaf-left-tail", "olive", 7, 17)
    p.dot("leaf-left-vein", "linen", 5, 14)
    p.dot("leaf-left-vein", "linen", 6, 15)
    p.dot("leaf-right-tip", "olive", 11, 15)
    p.rect("leaf-right-upper", "olive", 9, 16, 3, 2)
    p.rect("leaf-right-lower", "olive", 9, 18, 2, 1)
    p.dot("leaf-right-vein", "linen", 10, 17)
    p.dot("leaf-left-edge", "stone_dark", 7, 16)
    frames.append(p)

    p = Frame("wild-berries", "WildBerries", "A compact rusty-red double-lobed fruit cluster with a tiny attached dry stem.",
              "Finite BerryBush harvest yields one to three units; the art does not encode the stack quantity.",
              "Raw FoodPart heals1d3 when a real carried unit is eaten.",
              "Cooking=Meal is flavor, not a Cookable part. No basket, planted bush, growing leaf canopy or cooked food.")
    p.rect("stem-cut", "leather", 8, 16, 1, 3)
    p.dot("stem-branch", "leather", 9, 17)
    p.rect("berry-left-top", "rust", 6, 18, 2, 1)
    p.rect("berry-left-body", "rust", 5, 19, 4, 3)
    p.rect("berry-left-base", "rust", 6, 22, 2, 1)
    p.rect("berry-right-top", "rust", 9, 19, 2, 1)
    p.rect("berry-right-body", "rust", 9, 20, 3, 2)
    p.rect("berry-right-base", "rust", 9, 22, 2, 1)
    p.dot("berry-left-face", "rust_light", 6, 19)
    p.dot("berry-right-face", "rust_light", 9, 20)
    p.dot("berry-join", "leather", 8, 21)
    frames.append(p)

    p = Frame("gold-coin", "GoldCoin", "One small rounded muted-gold disk with a single square dark inset and a worn pale edge.",
              "Existing loose currency, including actual hollow-stump output; no new money spawn.",
              "Existing Commerce currency; value and quantity remain owned game state.",
              "No coin stack, sack, amount text, cross emblem, glow, gem facet or repeated sparkles.")
    p.rect("coin-top", "ochre", 7, 17, 4, 1)
    p.rect("coin-body", "ochre", 6, 18, 6, 4)
    p.rect("coin-bottom", "ochre_dark", 7, 22, 4, 1)
    p.rect("coin-worn-rim", "linen", 6, 18, 1, 2)
    p.dot("coin-worn-top", "linen", 7, 17)
    p.dot("coin-inset", "ochre_dark", 9, 19)
    p.rect("coin-side", "ochre_dark", 11, 20, 1, 2)
    frames.append(p)

    p = Frame("brewed-tonic", "BrewedTonic", "An opaque broad-based grey-green flask with a short neck and a plain brown stopper.",
              "The one existing brew blueprint carries varied resolved payloads; actual north-worksite stoneskin batch was observed.",
              "Consumed through TonicPart; healing-only uses Healing dice, status/coating/throwable outcomes may own BrewItemPart.",
              "One neutral container for supported forms, never a promise of healing or safe drinking. Explicit Food, foreign or unknown form stays runtime fallback; no liquid line, label, reusable empty vessel or payload symbol.")
    p.rect("stopper", "leather", 7, 10, 3, 2)
    p.rect("flask-neck", "stone", 7, 12, 3, 4)
    p.rect("flask-neck-rim", "stone_light", 6, 12, 5, 1)
    p.rect("flask-shoulder", "stone", 6, 16, 5, 1)
    p.rect("flask-body", "stone", 5, 17, 7, 5)
    p.rect("flask-base", "stone_dark", 6, 22, 5, 1)
    p.rect("flask-quiet-side", "stone_light", 5, 17, 1, 3)
    p.dot("flask-shoulder-edge", "stone_light", 6, 16)
    p.rect("flask-dark-side", "stone_dark", 11, 19, 1, 3)
    frames.append(p)

    p = Frame("concentrated-mendleaf", "ConcentratedMendleaf", "A compact dry pressed olive leaf wad, with one folded seam and a pale cut edge.",
              "Prepare consumes two actual MendleafSprig units near a still to produce one portable measure.",
              "Reagent vital:2; brewing can make one2d4 mending tonic instead of separate1d4 sprig brews.",
              "Still raw reagent, not a ready potion, bandage, medicine bottle, tied package or loose full-size sprig.")
    p.rect("pressed-leaf-top", "olive", 7, 17, 3, 1)
    p.rect("pressed-leaf-upper", "olive", 6, 18, 5, 2)
    p.rect("pressed-leaf-body", "olive", 5, 20, 7, 2)
    p.rect("pressed-leaf-base", "stone_dark", 6, 22, 5, 1)
    p.rect("pressed-leaf-cut", "linen", 5, 20, 2, 1)
    p.dot("pressed-leaf-cut-edge", "linen", 5, 21)
    p.rect("pressed-leaf-fold", "stone_dark", 8, 18, 1, 2)
    p.dot("pressed-leaf-fold-end", "stone_dark", 9, 19)
    frames.append(p)
    return frames


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    atlas = bpy.context.scene
    atlas.name = "Native 112x24 north works products"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    assert len(frames) == 7
    materials = {key: emission_material(key + " #" + value, value) for key, value in PALETTE.items()}
    collections = [model(frame, materials, atlas, i) for i, frame in enumerate(frames)]
    camera_for(atlas, 112, 24)
    atlas.render.filepath = str(OUT / "north-works-products.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("North works product contact 8x geometry")
    camera_for(contact, 160, 100, scale=8)
    plane(contact, "Contact background", "032F23", 0, 0, 160, 100, -1)
    for i, (frame, collection) in enumerate(zip(frames, collections)):
        instance(contact, collection, f"{i:02d} {frame.name}", (i % 4) * 40 + 12 - i * 16, 64 - (i // 4) * 40)
        add_label(contact, f"{i:02d} {frame.name}", (i % 4) * 40 + 20, 59 - (i // 4) * 40, materials["linen"], 1.6)
    add_label(contact, "NORTH WORKS / 16 x 24 / PORTABLE GOODS", 80, 95, materials["linen"], 2)
    add_label(contact, "Direct 8x geometry / no painted quantity, payload promise or reusable empty vessel", 80, 6, materials["linen"], 1.4)
    contact.render.filepath = str(OUT / "north-works-products-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {"schema": 1, "coordinateSystem": "Top-left origin, Y downward; flat front-facing XY quads.",
              "palette": PALETTE, "frames": [{"index": i, "id": f.name, "blueprint": f.blueprint, "cells": f.cells}
                                             for i, f in enumerate(frames)]}
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {"schema": 1, "artifact": "north-works-products.png", "width": 112, "height": 24,
                "frameWidth": 16, "frameHeight": 24, "frameCount": 7, "pixelsPerUnit": 16,
                "tilePivotNormalized": [0.5, 0.5], "filterMode": "Point", "wrapMode": "Clamp", "mipmaps": False,
                "compression": "None", "alpha": "Binary; independently verified.", "source": "north-works-products.blend",
                "generator": "Tools/Live16x24/build_north_works_products.py", "resource": "Sprites/Live16x24/north-works-products",
                "reference": "ArtSource/Live16x24/concepts/works-products-v1.png",
                "prompt": "ArtSource/Live16x24/concepts/works-products-prompt.txt",
                "captures": ["Docs/Verification/Live16x24/north-works-stoneburr-harvest.png",
                             "Docs/Verification/Live16x24/north-works-batch-brew.png"],
                "scope": "Seven static portable identities. No new gameplay, spawns, animation, import or live acceptance claim.",
                "sha256": hashlib.sha256((OUT / "north-works-products.png").read_bytes()).hexdigest(), "frames": []}
    for i, f in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(f.cells) for x, value in enumerate(row) if value]
        colors = sorted({PALETTE[value[1]] for row in f.cells for value in row if value})
        assert max(y for x, y in occupied) == 22 and 3 <= len(colors) <= 6, (f.name, colors)
        manifest["frames"].append({"index": i, "id": f.name, "blueprint": f.blueprint,
                                  "rect": [i * 16, 0, 16, 24], "description": f.description,
                                  "realSource": f.source, "actualUse": f.use, "limits": f.limits,
                                  "occupiedPixels": len(occupied), "palette": colors, "footprintCells": [1, 1],
                                  "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                                         max(x for x, y in occupied) + 1, 23], "baselineTopLeft": 23})
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "north-works-products.blend"))
    print("Wrote native north works products and direct 8x contact:", OUT)


if __name__ == "__main__":
    main()
