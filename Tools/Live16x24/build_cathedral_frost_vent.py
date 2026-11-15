"""Cathedral-only palette correction of approved frost-vent editable pixel quads.
No new silhouette, footprint, animation or generated pixels. Ginmere stays intact.
Use Blender --background --threads 4 --python this-file -- --build / --inspect;
python3 this-file --verify checks every native pixel against the saved cells.
"""
import json, sys
from pathlib import Path
sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_frost_vent as source
source.OUT = source.ROOT / 'ArtSource/Live16x24/cathedral-frost-vent'
source.RUNTIME = source.ROOT / 'Assets/Resources/Sprites/Live16x24/cathedral-frost-vent.png'
source.P = {'mouth': '243B50', 'lip': '5D91AE', 'rime': 'A2D8ED',
            'light': 'DBF5FA', 'shade': '354B62', 'wisp': 'A3CDE6'}
if __name__ == '__main__':
    if '--build' in sys.argv:
        source.build()
        manifest = json.loads((source.OUT / 'manifest.json').read_text())
        manifest.update(scope='Exact disclosed Cathedral depth-one FrostVent and current cold-only sandstone receiver',
            derivative_of='../frost-vent/editable.blend',
            concept='../frost-vent/concept.png',
            correction='Palette only; identical frame cells, part names and alpha; Ginmere source remains unchanged')
        source.save('manifest.json', manifest)
    elif '--inspect' in sys.argv: source.inspect()
    else: source.verify()
