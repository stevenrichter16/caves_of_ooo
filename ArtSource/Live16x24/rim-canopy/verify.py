"""Read-only native pixel/source/footprint verification, not gameplay acceptance."""
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent
im = Image.open(root/'rim-canopy.png').convert('RGBA')
assert im.size == (48,24)
source = json.loads((root/'source-cells.json').read_text())
palette = {k:tuple(bytes.fromhex(v))+(255,) for k,v in source['palette'].items()}
for i,frame in enumerate(source['frames']):
    occupied = set()
    for y,row in enumerate(frame['cells']):
        for x,value in enumerate(row):
            pixel = im.getpixel((16*i+x,y))
            expected = palette[value[1]] if value else (0,0,0,0)
            assert pixel == expected, (i,x,y,pixel,expected)
            if value: occupied.add((x,y))
    # Preserve more body than the original179px tree, but do not demand the
    # rejected round candidate's200px floor at the expense of branch gaps.
    assert 179 < len(occupied) <= 270, (i,len(occupied))
    assert all(1 <= x <= 14 and 1 <= y <= 22 for x,y in occupied)
    seen = {next(iter(occupied))}
    while True:
        more = {(x+dx,y+dy) for x,y in seen for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]} & occupied
        if more <= seen: break
        seen |= more
    assert seen == occupied, 'Disconnected crown/body'
print('Three native cells: exact source pixels, bounded single-cell connected silhouettes; gameplay unverified.')
