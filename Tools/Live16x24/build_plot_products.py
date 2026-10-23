"""Original native geometry for eight portable plot goods.

Run Blender --background --python Tools/Live16x24/build_plot_products.py.
Writes only ArtSource/Live16x24/plot-products. No raster is opened or edited.
Accepted item helpers supply unchanged integer meshes/materials, not pixels.
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
OUT = ROOT / "ArtSource/Live16x24/plot-products"
PALETTE = {key: ITEM_PALETTE[key] for key in (
    "leather", "cream", "linen", "grain", "ochre", "ochre_dark", "olive", "cut",
    "rust", "rust_light", "stone_dark", "stone")}


def make_frames():
    frames = []
    p = Frame("gladroot", "CandyCarrot", "One detached squat orange root with short cut sage leaves and a tapered edible end.",
              "The public CandyCarrotCrop automatically releases two real food owners on completion.",
              "Consume a carried unit for1d4 healing through FoodPart.",
              "Food.Cooking=Meal is flavor, not a Cookable part. No prepared meal, planted crown, soil or new growth state.")
    p.rect("cut-leaf-center", "stone", 7, 12, 2, 4)
    p.rect("cut-leaf-left", "olive", 5, 12, 2, 2)
    p.dot("cut-leaf-left-join", "stone", 6, 14)
    p.rect("cut-leaf-right", "olive", 9, 13, 2, 2)
    p.rect("root-shoulder", "ochre", 6, 15, 4, 1)
    p.rect("root-body", "ochre", 5, 16, 6, 3)
    p.rect("root-taper", "ochre", 6, 19, 4, 2)
    p.rect("root-tip", "ochre_dark", 7, 21, 2, 2)
    p.rect("root-cut-face", "linen", 5, 16, 2, 1)
    p.dot("root-face", "linen", 5, 17)
    p.rect("root-side", "ochre_dark", 10, 17, 1, 2)
    p.dot("root-low-side", "ochre_dark", 9, 20)
    frames.append(p)

    p = Frame("gladroot-seed", "CandyCarrotSeed", "One small pale oblique seed with a dry crease; no sack or planted mound.",
              "Starting supplies and finite SeedKeeperStock; not a saved-seed yield of this automatic crop.",
              "Consume a carried seed to plant CandyCarrotCrop on eligible Plantable ground, or an adjacent prepared bed.",
              "A loose seed is not already planted and does not promise water, growth time or multiple units.")
    p.rect("seed-top", "linen", 8, 18, 2, 1)
    p.rect("seed-upper", "cream", 7, 19, 3, 2)
    p.rect("seed-lower", "cream", 6, 21, 3, 1)
    p.rect("seed-bottom", "linen", 6, 22, 2, 1)
    p.dot("seed-crease", "leather", 9, 20)
    frames.append(p)

    p = Frame("emberwheat-seed", "EmberwheatSeed", "One long narrow golden grain with a quiet dry seam.",
              "Starting supplies and finite SeedKeeperStock; distinct from the harvested Emberwheat sheaf.",
              "Consume a carried seed to plant EmberwheatCrop on eligible ground.",
              "Warm lore does not grant LightSource or combustion. No flame, glow, cut stalk or seed packet.")
    p.dot("seed-point", "ochre_dark", 8, 17)
    p.rect("seed-upper", "grain", 7, 18, 2, 3)
    p.rect("seed-broad", "grain", 6, 19, 3, 2)
    p.rect("seed-lower", "grain", 7, 21, 2, 1)
    p.dot("seed-tip", "ochre_dark", 8, 22)
    p.rect("seed-crease", "ochre_dark", 8, 19, 1, 3)
    p.dot("seed-face", "linen", 7, 19)
    frames.append(p)

    p = Frame("marlroot-clod", "MarlrootClod", "Detached knuckled red-clay clod with short pale root strands wrapped around it.",
              "Actual mature MarlrootCrop yields two clods and one separate saved seed.",
              "BotanicalProcessing Prepare consumes one carried clod and creates one FireClay.",
              "Unprepared clod, not already refined repair clay, edible root, stone ore, leafy crop or decorative extra yield.")
    p.rect("clay-upper", "rust", 6, 15, 4, 2)
    p.rect("clay-middle", "rust", 4, 17, 8, 3)
    p.rect("clay-low", "rust", 5, 20, 6, 2)
    p.rect("clay-bottom", "leather", 6, 22, 4, 1)
    p.rect("clay-left-knuckle", "rust_light", 4, 18, 2, 1)
    p.dot("clay-top-face", "rust_light", 7, 15)
    p.rect("root-left-cut", "linen", 3, 16, 3, 1)
    p.rect("root-left-wrap", "linen", 5, 17, 1, 2)
    p.rect("root-middle-wrap", "linen", 6, 18, 2, 1)
    p.rect("root-central-knot", "linen", 7, 19, 2, 1)
    p.rect("root-low-wrap", "linen", 7, 20, 1, 3)
    p.rect("root-right-wrap", "linen", 10, 19, 2, 1)
    p.dot("root-right-cut", "linen", 12, 20)
    p.dot("root-right-join", "leather", 11, 20)
    frames.append(p)

    p = Frame("marlroot-seed", "MarlrootSeed", "One squat angular red-brown seed with a pale nick and dark dry lower edge.",
              "One saved seed accompanies real mature Marlroot harvest.",
              "Consume a carried seed to plant MarlrootCrop in actual cultivated soil.",
              "No root strands, soil clod, resin, prepared clay or already claimed bed.")
    p.rect("seed-top", "rust", 7, 18, 2, 1)
    p.rect("seed-body", "rust", 6, 19, 4, 3)
    p.dot("seed-left", "rust", 5, 20)
    p.rect("seed-base", "leather", 7, 22, 2, 1)
    p.dot("seed-notch", "linen", 7, 18)
    p.dot("seed-notch-low", "linen", 6, 19)
    p.dot("seed-side", "leather", 9, 21)
    frames.append(p)

    p = Frame("pitchpod-resin", "PitchpodResin", "One irregular low amber solid with a restrained matte cut face and thick dark edge.",
              "Actual mature PitchpodCrop yields two resin units and one separate saved seed.",
              "Raw Reagent heat:1,combustible:1, usable in existing brewing.",
              "Not a finished bottle, burning coating, glowing jewel, detached paper pod or planted crop.")
    p.rect("resin-upper", "ochre", 6, 16, 3, 1)
    p.rect("resin-body", "ochre", 5, 17, 6, 3)
    p.rect("resin-right-lobe", "ochre", 10, 18, 2, 3)
    p.rect("resin-lower", "ochre", 6, 20, 5, 2)
    p.rect("resin-base", "ochre_dark", 7, 22, 3, 1)
    p.rect("resin-cut-face", "cut", 5, 17, 2, 1)
    p.dot("resin-cut-face", "cut", 5, 18)
    p.rect("resin-side", "ochre_dark", 11, 19, 1, 2)
    p.dot("resin-dry-edge", "ochre_dark", 10, 21)
    frames.append(p)

    p = Frame("pitchpod-seed", "PitchpodSeed", "One tapered dry olive seed with a warm amber seam, smaller and darker than the resin.",
              "One saved seed accompanies real mature Pitchpod harvest.",
              "Consume a carried seed to plant PitchpodCrop in actual cultivated soil.",
              "No growing stem, pod shell, flower, flame, resin supply or seed packet.")
    p.dot("seed-tip", "stone_dark", 7, 17)
    p.rect("seed-upper", "olive", 7, 18, 2, 2)
    p.rect("seed-body", "olive", 6, 20, 4, 2)
    p.rect("seed-base", "stone_dark", 7, 22, 2, 1)
    p.rect("seed-seam", "ochre", 7, 19, 1, 3)
    p.dot("seed-side", "stone_dark", 9, 21)
    frames.append(p)

    p = Frame("inert-sludge", "InertSludge", "A single low irregular grey-olive compost lump with one muted dry fold; no separate chips.",
              "Finite keeper/provisioner stock and an actual fallback BrewingService output.",
              "Consume one carried unit to compost a freshly planted crop once, reducing growth-stage time25 percent without water.",
              "Neutral stable material, not a container, spilled liquid, water source, sprouting plant or ready potion.")
    p.rect("compost-top", "stone", 6, 18, 4, 1)
    p.rect("compost-body", "stone", 4, 19, 8, 3)
    p.rect("compost-left", "stone_dark", 3, 21, 2, 1)
    p.rect("compost-right", "stone_dark", 11, 20, 2, 2)
    p.rect("compost-base", "stone_dark", 5, 22, 7, 1)
    p.rect("compost-dry-fold", "olive", 5, 19, 3, 1)
    p.dot("compost-fold-low", "olive", 5, 20)
    p.dot("compost-side", "stone_dark", 10, 21)
    frames.append(p)
    return frames


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    atlas = bpy.context.scene
    atlas.name = "Native 128x24 plot products"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    assert len(frames) == 8
    materials = {key: emission_material(key + " #" + value, value) for key, value in PALETTE.items()}
    collections = [model(frame, materials, atlas, i) for i, frame in enumerate(frames)]
    camera_for(atlas, 128, 24)
    atlas.render.filepath = str(OUT / "plot-products.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("Plot product contact 8x geometry")
    camera_for(contact, 160, 100, scale=8)
    plane(contact, "Contact background", "032F23", 0, 0, 160, 100, -1)
    for i, (frame, collection) in enumerate(zip(frames, collections)):
        instance(contact, collection, f"{i:02d} {frame.name}", (i % 4) * 40 + 12 - i * 16, 64 - (i // 4) * 40)
        add_label(contact, f"{i:02d} {frame.name}", (i % 4) * 40 + 20, 59 - (i // 4) * 40, materials["linen"], 1.6)
    add_label(contact, "PLOT PRODUCTS / NATIVE 16 x 24 / ORIGINAL LOOSE GOODS", 80, 95, materials["linen"], 2)
    add_label(contact, "Direct8x geometry / one identity per cell / quantities and effects stay in the simulation", 80, 6, materials["linen"], 1.4)
    contact.render.filepath = str(OUT / "plot-products-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {"schema": 1, "coordinateSystem": "Top-left origin, Y downward; flat front-facing XY quads.",
              "palette": PALETTE, "frames": [{"index": i, "id": f.name, "blueprint": f.blueprint,
                                             "cells": f.cells} for i, f in enumerate(frames)]}
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {"schema": 1, "artifact": "plot-products.png", "width": 128, "height": 24,
                "frameWidth": 16, "frameHeight": 24, "frameCount": 8, "pixelsPerUnit": 16,
                "tilePivotNormalized": [0.5, 0.5], "filterMode": "Point", "wrapMode": "Clamp", "mipmaps": False,
                "compression": "None", "alpha": "Binary; independently verified.", "source": "plot-products.blend",
                "generator": "Tools/Live16x24/build_plot_products.py", "resource": "Sprites/Live16x24/plot-products",
                "reference": "ArtSource/Live16x24/concepts/plot-products-v1.png",
                "prompt": "ArtSource/Live16x24/concepts/plot-products-prompt.txt",
                "capture": "Docs/Verification/Live16x24/north-plot-harvest.png",
                "scope": "Eight static portable-item identities. No new gameplay, spawns, animation or runtime integration claim.",
                "sha256": hashlib.sha256((OUT / "plot-products.png").read_bytes()).hexdigest(), "frames": []}
    for i, f in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(f.cells) for x, value in enumerate(row) if value]
        colors = sorted({PALETTE[value[1]] for row in f.cells for value in row if value})
        assert max(y for x, y in occupied) == 22 and 3 <= len(colors) <= 6, (f.name, colors)
        manifest["frames"].append({"index": i, "id": f.name, "blueprint": f.blueprint,
                                  "rect": [i*16, 0, 16, 24], "description": f.description, "realSource": f.source,
                                  "actualUse": f.use, "limits": f.limits, "occupiedPixels": len(occupied),
                                  "palette": colors, "footprintCells": [1, 1],
                                  "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                                         max(x for x, y in occupied)+1, 23], "baselineTopLeft": 23})
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "plot-products.blend"))
    print("Wrote original native plot products and direct8x contact:", OUT)


if __name__ == "__main__":
    main()
