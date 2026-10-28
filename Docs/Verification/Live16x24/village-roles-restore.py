#!/usr/bin/env python3
"""Finite village-role and sidearm restoration wrapper. Defaults to read-only preflight.

Reuses the exact pinned surface-actors restorer; only the receipt-name literal
and six specifically authorized metadata paths are rebound. The four existing
Slate/Quartzite/Obsidian/specialist PNG metas require the existing exact whitespace-only check;
their current bytes must match the new snapshot after that exact normalization. All preservation, prior-build,
reviewed-delta, metadata-whitespace, preference and save guards remain intact.
The underlying helper preserves the explicitly reviewed current Editor state;
for this fresh idle snapshot, bind those fields to the matching normal baseline.
Do not use a historical Editor-state exception without a separately reviewed fact.
--apply is explicit. No build/Unity/game or cleanup operation is introduced.
"""
from pathlib import Path
import hashlib
import importlib.util
import sys
import types

BASE_PATH=Path('/Users/steven/caves-of-ooo/Docs/Verification/Live16x24/surface-actors-restore.py')
BASE_SHA256='71a8105fa6b7072ec4429bcd1455b15359ae5544dcc49eff506bd197f4ea48a6'
SOURCE_COMMIT='0b888da80571bf35ab4f5348ff6e600d289340e7'
SNAPSHOT=Path('/private/tmp/coo-village-roles-build-protection-0b888da80')
OWNED_METADATA={
    'Assets/Resources/Sprites/Live16x24/slate-caves.png.meta',
    'Assets/Resources/Sprites/Live16x24/quartzite-caves.png.meta',
    'Assets/Resources/Sprites/Live16x24/obsidian-caves.png.meta',
    'Assets/Tests/EditMode/FlatPixelSharedVillageRolesTests.cs.meta',
    'Assets/Tests/EditMode/FlatPixelSharedSidearmsTests.cs.meta',
    'Assets/Resources/Sprites/Live16x24/shared-specialists.png.meta',
}


def configure():
    if hashlib.sha256(BASE_PATH.read_bytes()).hexdigest()!=BASE_SHA256:
        raise RuntimeError('Existing reviewed restorer changed; review before use.')
    spec=importlib.util.spec_from_file_location('village_roles_restore_base',BASE_PATH)
    m=importlib.util.module_from_spec(spec)
    exec(compile(BASE_PATH.read_bytes(),str(BASE_PATH),'exec'),m.__dict__)
    old='packaged-surface-actors-restoration.json'
    new='packaged-village-roles-restoration.json'
    count=0
    def patch(code):
        nonlocal count
        values=[]
        for value in code.co_consts:
            if isinstance(value,types.CodeType): value=patch(value)
            elif isinstance(value,str) and value==old: count+=1; value=new
            values.append(value)
        changed=code.replace(co_consts=tuple(values))
        if changed.co_code!=code.co_code: raise RuntimeError('Restore control flow changed.')
        return changed
    patched=patch(m.main.__code__)
    if count!=1: raise RuntimeError('Expected one exact receipt literal.')
    m.main.__code__=patched; m.OWNED_METADATA=OWNED_METADATA; m.__doc__=__doc__
    return m


if __name__=='__main__':
    # Enforce this final source/snapshot without weakening the existing parser.
    for flag,value in (('--source-commit',SOURCE_COMMIT),('--snapshot',str(SNAPSHOT))):
        if sys.argv.count(flag)!=1 or sys.argv.index(flag)+1>=len(sys.argv) or sys.argv[sys.argv.index(flag)+1]!=value:
            raise RuntimeError('Use the exact reviewed '+flag+' value for this build batch.')
    configure().main()
