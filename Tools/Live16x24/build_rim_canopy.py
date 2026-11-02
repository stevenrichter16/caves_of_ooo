"""Original native canopy meshes; reference raster is never read or sampled.
Run Blender --background --python Tools/Live16x24/build_rim_canopy.py.
Only writes ArtSource/Live16x24/rim-canopy; original north vegetation is preserved.
"""
import hashlib
import json
from pathlib import Path
import sys
import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_north_vegetation import Frame, PALETTE, model
from build_opening_base import camera_for, emission_material

OUT = Path(__file__).resolve().parents[2] / 'ArtSource/Live16x24/rim-canopy'


def frames():
    result = []
    # Explicit native row spans retain a single-cell silhouette. The three
    # crowns vary mass placement, never terrain footprint or branch collision.
    outlines = [
        [(5, 9), (4, 11), (4, 12), (3, 12), (1, 14), (1, 15), (1, 15), (2, 15),
         (1, 15), (1, 14), (2, 14), (2, 15), (3, 14), (4, 12), (5, 11), (7, 10)],
        [(9, 12), (7, 14), (6, 14), (5, 15), (5, 15), (3, 15), (2, 14), (1, 14),
         (1, 14), (1, 15), (1, 14), (2, 13), (2, 12), (3, 10), (4, 9), (6, 10)],
        [(5, 8), (4, 10), (4, 11), (3, 11), (2, 12), (1, 12), (1, 13), (1, 15),
         (2, 15), (3, 15), (2, 14), (2, 14), (3, 13), (4, 12), (6, 10), (7, 10)],
    ]
    for i, rows in enumerate(outlines):
        p = Frame('rim-tree-' + str(i), 'Tree', 'Dense asymmetric crown with shaded underside and exposed root forks.',
                  'Existing solid destructible Tree, ordinary Rim Forest only.',
                  'One cell, no fruit, harvest, cross-cell canopy or extra collision.')
        p.rect('trunk-shadow', 'wood_dark', 7, 13, 3, 10)
        p.rect('trunk-face', 'wood', 7, 16, 2, 6)
        p.rect('left-fork', 'wood_dark', 4, 14, 2, 5).rect('left-joint', 'wood_dark', 5, 18, 3, 2)
        p.rect('right-fork', 'wood_dark', 11, 14, 2, 5).rect('right-joint', 'wood_dark', 9, 18, 3, 2)
        p.rect('roots', 'wood_dark', 5, 22, 7, 1).dot('root-face', 'wood', 5, 22)
        for y, (left, right) in enumerate(rows, 1):
            p.rect('crown-underside', 'leaf_dark', left, y, right-left, 1)
            if y < 13 and right-left > 3:
                p.rect('crown-face', 'leaf_mid', left+1, y, right-left-2, 1)
        # Broad stepped leaf planes; no stipple, gradients or bright halo.
        shift = (0, 1, -1)[i]
        for x, y, w, h in [(6+shift, 3, 3, 2), (3, 6, 3, 2), (9, 7, 3, 2), (4, 10, 2, 2)]:
            for yy in range(y, y+h):
                for xx in range(x, x+w):
                    if p.cells[yy][xx] and p.cells[yy][xx][1] == 'leaf_mid':
                        p.dot('upper-leaf-plane', 'leaf', xx, yy)
        p.rect('left-leaf-overlap', 'leaf_dark', 6, 6, 1, 3)
        p.rect('right-leaf-overlap', 'leaf_dark', 10, 10, 2, 3)
        p.rect('lower-leaf-overlap', 'leaf_dark', 7, 13, 2, 2)
        p.dot('top-leaf-edge', 'leaf_light', 7+shift, 3)
        p.dot('left-leaf-edge', 'leaf_light', 3, 6)
        # Break the ball edge into leafy lobes without leaving the cell.
        for xx, yy in [(1,8), (2,12), (13,4), (14,10), (4,14), (10,15)]:
            p.cells[yy][xx] = None
        # A visible unequal Y-fork separates the lower masses from the root.
        p.rect('exposed-fork', 'wood_dark', 7, 14, 2, 5)
        p.rect('fork-lit-face', 'wood', 7, 15, 1, 4)
        p.rect('left-fork-tip', 'wood_dark', 5, 12, 1, 4)
        p.dot('left-fork-joint', 'wood_dark', 6, 15)
        p.rect('right-fork-tip', 'wood_dark', 10, 13, 1, 3)
        p.dot('right-fork-joint', 'wood_dark', 9, 16)
        p.dot('branch-face', 'wood', 5, 17).dot('branch-face', 'wood', 11, 17)
        result.append(p)
    return result


def main():
    OUT.mkdir(exist_ok=True, parents=True)
    bpy.context.preferences.filepaths.save_version = 0
    scene = bpy.context.scene; scene.name = 'Native 48x24 Rim tree canopy'
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    cells = frames(); mats = {k: emission_material(k+' #'+v, v) for k,v in PALETTE.items()}
    for i, frame in enumerate(cells): model(frame, mats, scene, i)
    camera_for(scene, 48, 24); scene.render.filepath = str(OUT/'rim-canopy.png')
    bpy.ops.render.render(write_still=True)
    camera_for(scene, 48, 24, scale=8); scene.render.filepath = str(OUT/'contact-8x.png')
    bpy.ops.render.render(write_still=True)
    camera_for(scene, 48, 24); scene.render.filepath = str(OUT/'rim-canopy.png')
    source = {'palette':PALETTE, 'frames':[{'name':p.name,'cells':p.cells} for p in cells]}
    (OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
    manifest = {'native':[48,24], 'frame':[16,24], 'frames':3, 'ppu':16, 'pivot':[.5,.5],
                'filter':'Point', 'wrap':'Clamp', 'mipmaps':False, 'compression':'None',
                'source':'rim-canopy.blend','generator':'Tools/Live16x24/build_rim_canopy.py',
                'concept':'concept.png (reference only; no raster extraction)',
                'baseline':'Docs/Verification/GraphicsExecution/RimTreeShadow/after.png',
                'sha256':hashlib.sha256((OUT/'rim-canopy.png').read_bytes()).hexdigest(),
                'occupied':[sum(c is not None for row in p.cells for c in row) for p in cells],
                'limits':'Authored only; integration and actual gameplay still required.'}
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'rim-canopy.blend'))
    print(json.dumps(manifest))


if __name__ == '__main__': main()
