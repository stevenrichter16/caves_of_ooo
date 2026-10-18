from pathlib import Path
from collections import Counter
from PIL import Image
import json

OUT = Path(__file__).resolve().parent
# Flat representative colors from the higher-resolution photographic reference.
# Do not reproduce its compression noise or illumination variation.
PALETTE = {
    '.': (0, 0, 0, 0),
    'G': (227, 161, 64, 255),   # straw hat
    'D': (10, 44, 47, 255),    # face and pouch separation
    'C': (252, 235, 210, 255), # eye and cream pouch
    'O': (100, 109, 66, 255),  # olive cloak
    'F': (82, 94, 58, 255),    # cloak folds
    'R': (170, 64, 40, 255),   # red pouch
    'B': (84, 127, 170, 255),  # blue pouch
    'K': (222, 158, 61, 255),  # brighter ochre boots
}
BACKGROUND = (0, 49, 55, 255)
# Every character below is exactly one pixel. '.' is fully transparent.
GRID = [
    '................',
    '......GG........',
    '.....GGGG.......',
    '...GGGGGGGG.....',
    '.GGGGGGGGGGG....',
    '...DDDDDGGGGG...',
    '....DCDO....GG..',
    '......DOO.......',
    '......OOOO......',
    '.....OOFOO......',
    '.....OOFOO......',
    '.....OFOFOO.....',
    '.....OFOOD.R....',
    '.....OFOODRR....',
    '.....OFOODRR....',
    '.....OFOCDOB....',
    '.....OFDCCOBB...',
    '....OOFDCCOBB...',
    '....OFOODDO.....',
    '....OFOOFOO.....',
    '......K.OOO.....',
    '......KK..K.....',
    '.....KKK.KK.....',
    '................',
]

def render(grid, stem):
    assert len(grid) == 24 and all(len(row) == 16 for row in grid), [(i, len(r)) for i,r in enumerate(grid)]
    assert set(''.join(grid)) <= PALETTE.keys()
    im = Image.new('RGBA', (16,24))
    im.putdata([PALETTE[c] for row in grid for c in row])
    im.save(OUT / f'{stem}.png')
    preview = Image.new('RGBA', im.size, BACKGROUND)
    preview.alpha_composite(im)
    preview.convert('RGB').resize((512,768), Image.Resampling.NEAREST).save(OUT / f'{stem}-preview.png')
    preview.convert('RGB').resize((128,192), Image.Resampling.NEAREST).save(OUT / f'{stem}-preview-8x.png')
    svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="16" height="24" viewBox="0 0 16 24" shape-rendering="crispEdges">', '<title>Seed Keeper, reference-corrected 16 by 24 pixel sprite</title>']
    for y,row in enumerate(grid):
        for x,c in enumerate(row):
            if c != '.':
                rgba = PALETTE[c]
                fill = '#%02x%02x%02x' % rgba[:3]
                svg.append(f'  <rect x="{x}" y="{y}" width="1" height="1" fill="{fill}"/>')
    svg.append('</svg>')
    (OUT / f'{stem}.svg').write_text('\n'.join(svg) + '\n')
    return im

if __name__ == '__main__':
    im = render(GRID, 'seed-keeper-16x24')
    counts = Counter(''.join(GRID))
    info = {
        'width': 16, 'height': 24,
        'opaque_color_count': len([c for c in counts if c != '.']),
        'visible_pixel_count': 384 - counts['.'],
        'transparent_pixel_count': counts['.'],
        'alpha_values': sorted(set(im.getchannel('A').getdata())),
        'palette': {c: '#%02x%02x%02x' % PALETTE[c][:3] for c in counts if c != '.'},
        'pixel_counts': dict(counts),
        'preview_scale': 32,
    }
    (OUT/'sprite-grid.txt').write_text('\n'.join(GRID)+'\n')
    (OUT/'sprite-info.json').write_text(json.dumps(info, indent=2)+'\n')
    (OUT/'README.txt').write_text(f'''SEED KEEPER — reference-corrected native pixel sprite\n\nNative canvas: 16 × 24 pixels, transparent RGBA PNG\nOpaque palette: {info['opaque_color_count']} colors\nVisible pixels: {info['visible_pixel_count']}\nTransparent pixels: {info['transparent_pixel_count']}\nAlpha values: 0 and 255 only\n\nseed-keeper-16x24.png — native production asset\nseed-keeper-16x24-preview-8x.png — exact 8× nearest-neighbor enlargement on flat dark teal, 128 × 192\nseed-keeper-16x24-preview.png — exact 32× nearest-neighbor enlargement on flat dark teal, 512 × 768\nseed-keeper-16x24.svg — editable 16 × 24 SVG, each rect exactly one native pixel\nsprite-grid.txt — editable literal character grid\ngenerate.py — Python/Pillow source, authoritative palette and grid\nsprite-info.json — palette and pixel counts\nreference-higher-resolution.jpg — user-supplied reference for the corrected proportions\n\nEdited directly on a 16 × 24 grid to match the higher-resolution reference. No downsampling, anti-aliasing or smoothing. Original supplied version is preserved in Git commit 121c780f2.\nTo edit, change GRID or PALETTE in generate.py and run: python generate.py\nUse nearest-neighbor / point filtering when displaying the native PNG at larger sizes.\n''')
    print(json.dumps(info, indent=2))
