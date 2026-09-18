"""Production spell-art checks; run with python3 -m unittest ArtTools.test_spell_fx_assets."""
import json
from pathlib import Path
import re
import unittest
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]

class SpellFxAssetsTests(unittest.TestCase):
    def setUp(self):
        self.defs = json.loads((ROOT/'Assets/Resources/Content/Data/SpellVisuals.json').read_text())['Definitions']

    def test_registered_casts_are_covered(self):
        registered = set()
        for school in ('Pyromancy','Cryomancy','Galvanism','Hydromancy','Corrosion','Spellcraft','Rites'):
            data = json.loads((ROOT/f'Assets/Resources/Content/Data/Skills/{school}.json').read_text())
            for skill in data['Skills']:
                for power in skill.get('Powers', []):
                    cls = power['Class']
                    source = (ROOT/f'Assets/Scripts/Gameplay/Skills/{cls}.cs').read_text()
                    if 'DeclareActivatedAbility(' in source or re.search(r': (ProjectileSpellSkillBase|ConsumingRiteSkillBase)', source):
                        registered.add(cls)
        ids = [d['ID'] for d in self.defs]
        self.assertEqual(len(ids), len(set(ids)))
        self.assertGreaterEqual(len(registered), 49)
        self.assertEqual(set(), registered - set(ids))
        self.assertNotIn('Cryomancy_FrostRetort', registered)

    def test_binary_alpha_registered_dimensions_and_school_shapes(self):
        silhouettes = set()
        for school in ('fire','ice','lightning','water','acid','binding','ink'):
            for suffix, size, rows in [('detail',16,9), ('impact',32,2)]:
                file = ROOT/f'Assets/Resources/Sprites/SpellFx/{school}_{suffix}.png'
                image = Image.open(file).convert('RGBA')
                self.assertEqual((size*6,size*rows), image.size)
                self.assertEqual({0,255}, set(image.getchannel('A').tobytes()))
                for row in range(rows):
                    for frame in range(6):
                        tile=image.crop((frame*size,row*size,(frame+1)*size,(row+1)*size))
                        self.assertIsNotNone(tile.getbbox(), (school, suffix,row,frame))
                if suffix == 'detail':
                    silhouettes.add(image.crop((0,32,16,48)).getchannel('A').tobytes())
        self.assertEqual(7,len(silhouettes),'Schools must differ in silhouette, not only hue')

if __name__ == '__main__': unittest.main()
