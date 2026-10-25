"""Export native HUD cells and a review contact; no image input or Unity import.

The world uses16x24 cells. This HUD preserves its established16x16 icon bands.
After native RED, copy the generated rows into HudSkillIconAtlas's appended slots.
"""
from pathlib import Path
import json
from export_cellar_floor import png
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/warden-hotbar'
def main():
    source=json.loads((OUT/'source-cells.json').read_text())
    assert (source['width'],source['height'],len(source['frames']))==(16,16,3)
    palette={k:bytes.fromhex(v) for k,v in source['palette'].items()}
    for frame in source['frames']:
        assert len(frame['rows'])==16 and all(len(r)==16 for r in frame['rows'])
        assert set(''.join(frame['rows']))<=set(palette)
    native=b''.join(palette[c] for y in range(16) for f in source['frames'] for c in f['rows'][y])
    digest=png(OUT/'warden-hotbar.png',48,16,native)
    preview=b''.join(native[((y//8)*48+x//8)*4:((y//8)*48+x//8)*4+4] for y in range(128) for x in range(384))
    png(OUT/'warden-hotbar-contact-8x.png',384,128,preview)
    snippet='\n'.join('            // '+f['id']+'\n            '+ ' +\n            '.join('"'+r+'"' for r in f['rows'])+',' for f in source['frames'])
    (OUT/'runtime-rows.txt').write_text(snippet+'\n')
    (OUT/'manifest.json').write_text(json.dumps({'schema':1,'source':'source-cells.json','sha256':digest,'frameWidth':16,'frameHeight':16,'frameCount':3,'alpha':'binary','pixelsPerUnit':16,'paletteUnchanged':True,'limits':'Authored source, not gameplay proof; code atlas import is explicit.'},indent=2)+'\n')
    print(digest)
if __name__=='__main__':main()
