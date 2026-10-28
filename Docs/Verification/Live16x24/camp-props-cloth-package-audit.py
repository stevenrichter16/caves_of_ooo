#!/usr/bin/env python3
"""Finite camp-props/cloth wrapper, frozen source 9a2371852 with final v2 art.

Reuses the SHA-pinned durable deep-caves auditor with the prior surface-actors
wrapper's checked literal-only main adaptation. Build/archive/collision guards
and control flow remain unchanged. No Unity, Git, game or build invocation.
--dry-run writes nothing. Prior packages and normal state are preserved by the
root's separate fresh snapshot/restoration workflow, never by this packager.

One retained XML: 114 unique Passed (12 cloth + 102 related), not earlier counts
added together. XML times are real; final artwork acceptance is separate.
"""
import collections
import hashlib
import importlib.util
from pathlib import Path
import types
import xml.etree.ElementTree as ET

REPO = Path('/Users/steven/caves-of-ooo')
BASE_PATH = REPO/'Docs/Verification/Live16x24/deep-caves-package-audit.py'
BASE_SHA256 = 'fee94989ff27720ec8c3fcdd3270e7b870f11e13668f821c94f64b08f9c977b7'
SOURCE_COMMIT = '9a23718527e10e4ba7d24df203b8170bd12ba33e'
FINALIZED = True  # Final published source and v2 source/import bytes reviewed.
PINS = {
    'Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs': '34e5ddbfba4112219d2107c03c4ff67c62d27b53b99ec0f8b4a1db0dc4c8b458',
    'Assets/Scripts/Presentation/Rendering/FlatPixelSharedCampPropsCatalog.cs': '94c6f370dbcdc90f47fec323948960d64dd2322b60ae77963cf50b4f0d770ccf',
    'Assets/Scripts/Presentation/Rendering/FlatPixelSharedCampClothCatalog.cs': '349281e5b3aa82b0389834ef717f1dddb84aa7e2887e7b1f25b2ce9c83728e09',
    'Assets/Tests/EditMode/FlatPixelSharedCampPropsTests.cs': '7b8b465ad70a72c9e4579f42dfd54efefc128dae72d627f83bd42ed6df3cd5d2',
    'Assets/Tests/EditMode/FlatPixelSharedCampClothTests.cs': 'ca54c53e7b71ff2553c6df6c516261cb9c76cb1724e235ba9bbf0ea8129efdc7',
    'Assets/Resources/Sprites/Environment/cont_sack.png': 'd1a4c73c85a9aa5d115456cbb37fa106746d9a2cfcb6298fc6911d912488c9b4',
    'Assets/Resources/Sprites/Environment/cont_urn.png': '02c6e0906e1789fbd070edb6267a6303585cc045d86f4e70220befe559d15690',
    'Assets/Resources/Sprites/Environment/well.png': '4d8a33eced838078f84360ecd8d04a9d4921b06ee8ca1c4ec4a22c72406940c4',
    'Assets/Resources/Sprites/Live16x24/shared-camp-cloth.png': '3deb52ac4c4be95f6d5b738ca06176dfc5722a2ce564dfb9e11cfc00770ef6a8',
    'ArtSource/Live16x24/shared-camp-cloth/shared-camp-cloth.png': '3deb52ac4c4be95f6d5b738ca06176dfc5722a2ce564dfb9e11cfc00770ef6a8',
    'ArtSource/Live16x24/shared-camp-cloth/source-cells.json': '4a1b85848ff13396f2617bdf087850e3b0925de1b6831325464d06c97fc63ac3',
    'ArtSource/Live16x24/shared-camp-cloth/editable.blend': '14103467eeec7362b1a18ecaa096a10ed80ced75cd298802a674da6c6c2341e9',
    'ArtSource/Live16x24/shared-camp-cloth/manifest.json': '799bf4daa4ba9b1656310f2e510681ffd2af31963adf111051c4561f6d202c36',
    'Tools/Live16x24/build_shared_camp_cloth.py': '1fd7555f4cb3d7cc3b33f9ea0a32c1216ff9a8023f3adcb746dadfaaa57a54fc',
    'Docs/Verification/Live16x24/shared-camp-cloth-green.xml': '342ad77a3d8252d5d13a41db142635b4bc941e4ac2f6951e4dad53cb9874f71f',
}
NATIVE_NAME = 'shared-camp-cloth-green.xml'
NATIVE_SHA256 = '342ad77a3d8252d5d13a41db142635b4bc941e4ac2f6951e4dad53cb9874f71f'

REQUIRED_BINDINGS = ('Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedCampPropsCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedCampClothCatalog.cs',
 'Assets/Tests/EditMode/FlatPixelSharedCampPropsTests.cs',
 'Assets/Tests/EditMode/FlatPixelSharedCampClothTests.cs',
 'Assets/Resources/Sprites/Environment/cont_sack.png',
 'Assets/Resources/Sprites/Environment/cont_urn.png',
 'Assets/Resources/Sprites/Environment/well.png',
 'Assets/Resources/Sprites/Live16x24/shared-camp-cloth.png',
 'ArtSource/Live16x24/shared-camp-cloth/shared-camp-cloth.png',
 'ArtSource/Live16x24/shared-camp-cloth/source-cells.json',
 'ArtSource/Live16x24/shared-camp-cloth/editable.blend',
 'ArtSource/Live16x24/shared-camp-cloth/manifest.json',
 'Tools/Live16x24/build_shared_camp_cloth.py',
 'Docs/Verification/Live16x24/shared-camp-cloth-green.xml')

MAIN_LITERALS = {
    'deep-caves-materials-shared-stone-steam': 'shared-camp-props-cloth',
    'packaged-deep-caves-linux': 'packaged-camp-props-cloth-linux',
    '\\d{4}-\\d{2}-\\d{2}-deep-caves-': '\\d{4}-\\d{2}-\\d{2}-camp-props-cloth-',
    'Both fresh output folders must identify this deep-caves source and end in -current.': 'Both fresh output folders must identify this camp-props-cloth source and end in -current.',
    'Batched ordinary Quartzite depths 9–10 and Obsidian depths above 10 material coverage, shared neutral Pillar/BrokenColumn bodies and neutral natural SteamVent bodies. Twelve original opaque 16×24 floor/wall cells retain straight boundaries; approved stone-prop and vent bodies are borrowed unchanged. Guarded real-floor/backdrop handling, current graph/disclosure, owner/reader authority and existing damage/source mechanics remain required. Authored East SteamVent grille and independent East steam overlay remain separate; FrostVent is not covered. No generation, collision, saved-gameplay, global grading or camera change; isolated Shale study remains default-off.': 'Batched exact Sack/Urn/ordinary Well bodies, plus TentWall and GuestClothPole. Three approved square props are borrowed unchanged; a new original three-cell cloth atlas uses straight wall bodies and a passable guest pole. Current owner/disclosure/resource guards retain existing container, well, material, heat, burning, damage and collision behavior. No new service, blueprint, save-format, world geometry, global grading or camera behavior; isolated Shale study remains default-off.',
    'Scope is exact eligible ordinary Quartzite/Obsidian materials plus conservative shared Pillar/BrokenColumn/SteamVent identities. Other room props, actors, hazards, authored areas and world families remain open. Actual Obsidian depths 11/12 samples do not certify every deeper address.': 'Scope is five exact eligible bodies: Sack, Urn, ordinary Well, TentWall and GuestClothPole. Special WellSite/Repairable variants, unrelated containers, dwellings, hazards and world families remain outside this slice. Actual camp observations and normal interactions have separate evidence.',
    'Material/body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Repeated faces, dark internal facets, low-contrast fragments, sparse composition and mixed glyphs remain separate concerns. Actual journeys and persistence have separate records.': 'Body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Cloth repetition, surrounding glyphs and sparse camp composition remain later scene-review concerns. No new animation or broad world completion is claimed.',
    'Three complete retained native Results contain ': 'One complete retained native XML contains ',
    ' distinct Passed identities across 627 occurrences with 102 repeats: 419 stone plus 19/189 Steam. These are sequential checkpoints, not one final-source combined run. No earlier subset execution is added, no actual job results/times fabricated, and no performance claim follows.': ' distinct Passed identities in one 114-case cloth checkpoint: 12 cloth plus 102 related camp-prop/storage/furniture cases. Earlier subset executions are not added. Native timestamps come directly from this pinned XML; no job metadata or final-art run is invented. No performance claim follows.',
}


def native_evidence(boundary):
    a=AUDITOR
    a.require(FINALIZED and isinstance(SOURCE_COMMIT,str) and len(SOURCE_COMMIT)==40
              and set(PINS)==set(REQUIRED_BINDINGS), 'Final source/art pins are not yet reviewed; packaging disabled.')
    for name,expected in PINS.items():
        a.require(a.digest(a.canonical(REPO/name))==expected, 'Frozen selected source/resource differs: '+name)
    path=a.canonical(a.EVIDENCE/NATIVE_NAME)
    a.require(a.digest(path)==NATIVE_SHA256, 'Native XML differs from the reviewed complete receipt.')
    xml=ET.parse(path).getroot(); cases=xml.findall('.//test-case')
    a.require(xml.tag=='test-run' and xml.get('result')=='Passed'
              and all(xml.get(k)==v for k,v in {'total':'114','passed':'114','failed':'0','skipped':'0','inconclusive':'0'}.items()),
              'Expected one all-Passed 114-case native run.')
    names={row.get('fullname') for row in cases}
    a.require(len(cases)==len(names)==114 and all(isinstance(n,str) and n for n in names)
              and all(row.get('result')=='Passed' for row in cases), 'Missing/duplicate/non-Passed native case.')
    expected={'CavesOfOoo.Tests.FlatPixelSharedCampClothTests':12,
              'CavesOfOoo.Tests.FlatPixelSharedCampPropsTests':26,
              'CavesOfOoo.Tests.FlatPixelSharedStorageTests':43,
              'CavesOfOoo.Tests.FlatPixelSharedFurnitureTests':33}
    a.require(dict(collections.Counter(row.get('classname') for row in cases))==expected,
              'Native fixture scope differs from the reviewed run.')
    start=a.timestamp(xml.attrib['start-time']); end=a.timestamp(xml.attrib['end-time'])
    a.require(start<=end<=boundary, 'Native XML must precede root source/build boundary.')
    return {'receipts':[{'path':str(path),'sha256':NATIVE_SHA256,'summary':dict(xml.attrib),
                        'actual_case_rows':114,'mode':'EditMode',
                        'timestamp_source':'Actual native XML; no separate job metadata is inferred.'}],
            'current_source_binding':{'source_commit':SOURCE_COMMIT,'pins':PINS,
                'current_source_evidence_resource_bytes_match':True,
                'limits':'Final selected bytes are pinned separately from the retained runtime test checkpoint. '
                         'Root asserts source/build provenance; later art revision and gameplay acceptance need their own evidence.'},
            'execution_occurrences':114,'passed_execution_occurrences':114,
            'failed_execution_occurrences':0,'skipped_execution_occurrences':0,'inconclusive_execution_occurrences':0,
            'repeated_identity_occurrences':0,'distinct_test_identities_across_listed_receipts':114,
            'distinct_passed_test_identities':114,'distinct_skipped_test_identities':0,
            'distinct_inconclusive_test_identities':0,'all_cases_passed':True,
            'cloth_focused_passed':12,'related_passed':102,'native_job_times_verified':False,
            'native_xml_times_verified':True,'skipped_identities':[],'inconclusive_identities':[],
            'limits':'One 114-case XML only; no earlier props/cloth subset counts added. '
                     'No test rerun, final-art timing, gameplay, Linux/Deck or performance certification is invented.'}


def configure_auditor():
    if hashlib.sha256(BASE_PATH.read_bytes()).hexdigest()!=BASE_SHA256:
        raise RuntimeError('Existing audited base changed; re-review before using this wrapper.')
    spec=importlib.util.spec_from_file_location('camp_props_cloth_audited_base', BASE_PATH)
    a=importlib.util.module_from_spec(spec)
    # Compile the pinned source into its module namespace without .pyc/cache writes.
    exec(compile(BASE_PATH.read_bytes(), str(BASE_PATH), 'exec'), a.__dict__)
    counts=collections.Counter()
    def rebind_literals(code):
        values=[]
        for value in code.co_consts:
            if isinstance(value, types.CodeType):
                value=rebind_literals(value)
            elif isinstance(value,str) and value in MAIN_LITERALS:
                counts[value]+=1; value=MAIN_LITERALS[value]
            values.append(value)
        changed=code.replace(co_consts=tuple(values))
        if changed.co_code!=code.co_code:
            raise RuntimeError('Main control flow must remain unchanged.')
        return changed
    patched=rebind_literals(a.main.__code__)
    if dict(counts)!={key:1 for key in MAIN_LITERALS}:
        raise RuntimeError('Expected exactly one occurrence of each reviewed main literal.')
    a.main.__code__=patched
    a.__doc__=__doc__
    a.BOUND_SOURCE_COMMIT=SOURCE_COMMIT
    a.NATIVE_BINDINGS=PINS
    a.native_evidence=native_evidence
    return a


AUDITOR=configure_auditor()
if __name__=='__main__':
    AUDITOR.require(FINALIZED, 'Final source/art pins pending; this prepared wrapper cannot package yet.')
    AUDITOR.main()
