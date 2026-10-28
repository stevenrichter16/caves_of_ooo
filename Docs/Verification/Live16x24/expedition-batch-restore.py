#!/usr/bin/env python3
"""Finite expedition build/standalone restoration; default is read-only preflight.

Reuses the SHA-pinned prior restorer. Root supplies an exact reviewed delta and
idle confirmation. --apply is explicit. Owned session saves are archived, never
deleted; normal preferences, source and previous packages retain existing guards.
Only the receipt name and identical committed snapshot-helper location are rebound.
"""
from pathlib import Path
import hashlib
import importlib.util
import sys
import types

REPO = Path('/Users/steven/caves-of-ooo')
EVIDENCE = REPO/'Docs/Verification/Live16x24'
BASE_PATH = EVIDENCE/'surface-actors-restore.py'
BASE_SHA256 = '71a8105fa6b7072ec4429bcd1455b15359ae5544dcc49eff506bd197f4ea48a6'
SNAPSHOT_HELPER = EVIDENCE/'deep-caves-build-snapshot.py'
SNAPSHOT_HELPER_SHA256 = 'f92721cb75ffac0ee179f0ddcfa8bdd0bde61ad9f2023f1c85747409c206c33b'
SOURCE_COMMIT = '43a151ddbd714d1bf1a786084e88bc2cfcd5106b'
SNAPSHOT = Path('/private/tmp/coo-expedition-batch-build-protection-43a151ddb')
OWNED_METADATA = {
    'Assets/Resources/Sprites/Live16x24/bandfrog.png.meta',
    'Assets/Resources/Sprites/Live16x24/greatdew.png.meta',
    'Assets/Resources/Sprites/Live16x24/obsidian-caves.png.meta',
    'Assets/Resources/Sprites/Live16x24/quartzite-caves.png.meta',
    'Assets/Resources/Sprites/Live16x24/reedfrog.png.meta',
    'Assets/Resources/Sprites/Live16x24/shared-specialists.png.meta',
    'Assets/Resources/Sprites/Live16x24/slate-caves.png.meta',
    'Assets/Scripts/Presentation/Rendering/FlatPixelGreatdewCatalog.cs.meta',
    'Assets/Scripts/Presentation/Rendering/FlatPixelExpeditionEssentialsCatalog.cs.meta',
    'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGreatdewTests.cs.meta',
    'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelExpeditionEssentialsTests.cs.meta',
}


def configure():
    for path, expected in ((BASE_PATH,BASE_SHA256),(SNAPSHOT_HELPER,SNAPSHOT_HELPER_SHA256)):
        if hashlib.sha256(path.read_bytes()).hexdigest() != expected:
            raise RuntimeError('Pinned preservation helper changed: '+str(path))
    spec = importlib.util.spec_from_file_location('expedition_restore_base',BASE_PATH)
    m = importlib.util.module_from_spec(spec)
    exec(compile(BASE_PATH.read_bytes(),str(BASE_PATH),'exec'),m.__dict__)
    literals = {'packaged-surface-actors-restoration.json':'packaged-expedition-batch-restoration.json',
                '/tmp/coo-sill-build-snapshot.py':str(SNAPSHOT_HELPER)}
    counts = {key:0 for key in literals}
    def patch(code):
        values = []
        for value in code.co_consts:
            if isinstance(value,types.CodeType): value = patch(value)
            elif isinstance(value,str) and value in literals:
                counts[value] += 1; value = literals[value]
            values.append(value)
        changed = code.replace(co_consts=tuple(values))
        if changed.co_code != code.co_code: raise RuntimeError('Restore control flow changed.')
        return changed
    m.main.__code__ = patch(m.main.__code__)
    if counts != {key:1 for key in literals}:
        raise RuntimeError('Expected one exact occurrence of each restore literal.')
    m.OWNED_METADATA = OWNED_METADATA; m.__doc__ = __doc__
    return m


if __name__ == '__main__':
    for flag,value in (('--source-commit',SOURCE_COMMIT),('--snapshot',str(SNAPSHOT))):
        if sys.argv.count(flag) != 1 or sys.argv.index(flag)+1 >= len(sys.argv) or sys.argv[sys.argv.index(flag)+1] != value:
            raise RuntimeError('Use exact reviewed '+flag+' value for this batch.')
    # Preserve the existing base's explicit --apply gate, SHA/mode/whitespace
    # checks, logical preference comparison and wholly archived save-tree rule.
    configure().main()
