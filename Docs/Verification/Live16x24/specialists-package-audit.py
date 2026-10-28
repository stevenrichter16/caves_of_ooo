#!/usr/bin/env python3
"""Finite settlement-specialist wrapper, frozen published source 83e1e568f.

Reuses the SHA-pinned durable deep-caves auditor with the prior surface-actors
wrapper's checked literal-only main adaptation. Build/archive/collision guards
and control flow remain unchanged. No Unity, Git, game or build invocation.
--dry-run writes nothing. Prior packages and normal state are preserved by the
root's separate fresh snapshot/restoration workflow, never by this packager.

One retained XML: 99 unique Passed (19 specialists + 17 Weaponsmith + 24
residents + 39 incidental actors). Earlier subsets are not added. Actual play
and package acceptance have separate receipts; XML times are not invented.
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
SOURCE_COMMIT = '83e1e568fa4e3e2c9d4883fdcaf88be85fea9049'
FINALIZED = True  # Published source and selected source/import bytes reviewed.
PINS = {'Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs': '44dbccfb32c3e4b4c0854b71c0a0c161f17cb4e0e9d3175503f83574a0416e9b',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedResidentsCatalog.cs': '06fd622aaa5c3b686699ff9124ac959bbff6fdd9ec8dbb6ad27d40b6c025eb6e',
 'Assets/Tests/EditMode/FlatPixelSharedSpecialistsTests.cs': '459052551b2236a8c9843874b7bcc3ab7956aa4977c459cadaad2491742ad842',
 'Assets/Tests/EditMode/FlatPixelSharedWeaponsmithTests.cs': '07749008e2f2a1dc76da594b645e310a9f7b851c577ea1a05ade5246d21ed448',
 'Assets/Tests/EditMode/FlatPixelSharedResidentsTests.cs': 'ac3dd19686223cd93a9a2f949b23c22630029732bd1006744c427ead6e9acc53',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelIncidentalActorTests.cs': '1d3f95ea1949d822b3f3e53d0f97134fd627d2346af5d6d2fb27b01d75368e9e',
 'Assets/Resources/Sprites/Live16x24/opening-cast.png': '35a2de34d9bf6d6dfe66c7dd47439f7ac03ec6b7de9e09f026d6cb623e24d73a',
 'Assets/Resources/Sprites/Environment/merchant.png': 'd79c98d33a03b156c400e9f7120c99f1a133a67468c3dfee609e77af53ea6f2e',
 'Assets/Resources/Sprites/Environment/weaponsmith.png': 'd07f8e09765eb0846cdeab7e1ec686145e8ee799e0b30b1c44d35836820ccc66',
 'Assets/Resources/Sprites/Live16x24/shared-specialists.png': '02f38a58943bdb09eb078802056bc34a379e07a30a1220089e8800177938d402',
 'ArtSource/Live16x24/shared-specialists/shared-specialists.png': '02f38a58943bdb09eb078802056bc34a379e07a30a1220089e8800177938d402',
 'ArtSource/Live16x24/shared-specialists/source-cells.json': '47c113f5958f90be20e46860075d36707a89a05eafc7a8173a09419a614b8da8',
 'ArtSource/Live16x24/shared-specialists/editable.blend': '002e4874231d7dec2f08b7ef7a8e5b3f25e5c3d1a832c1f08268f3043a8e24dd',
 'ArtSource/Live16x24/shared-specialists/manifest.json': '78c727b90cbff60e03bb17cdf0d10d61fe19219442a5bc2b28118193ac9f62ba',
 'ArtSource/Live16x24/shared-specialists/import-spec.json': '1598bf18052aa23f074a048ff769dde45f8b47d8c1aa6e28bd0b72260be139f0',
 'ArtSource/Live16x24/shared-specialists/runtime-publication.json': '757618191ad2232adbdcf3aef4ce97ea544478cc78ce6afaa97cc36d332bfcf0',
 'Tools/Live16x24/build_shared_specialists.py': '7649c0fff978c5b4a733143bb3a568adbc94ce7923461e1d8346f2f9b8b741d9',
 'Docs/Verification/Live16x24/shared-specialists-native-green.xml': '80b266260287b19ca7b8939bcb5300539b5518c696008e86f0cf365d46f2d5d8'}
NATIVE_NAME = 'shared-specialists-native-green.xml'
NATIVE_SHA256 = '80b266260287b19ca7b8939bcb5300539b5518c696008e86f0cf365d46f2d5d8'

REQUIRED_BINDINGS = ('Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedResidentsCatalog.cs',
 'Assets/Tests/EditMode/FlatPixelSharedSpecialistsTests.cs',
 'Assets/Tests/EditMode/FlatPixelSharedWeaponsmithTests.cs',
 'Assets/Tests/EditMode/FlatPixelSharedResidentsTests.cs',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelIncidentalActorTests.cs',
 'Assets/Resources/Sprites/Live16x24/opening-cast.png',
 'Assets/Resources/Sprites/Environment/merchant.png',
 'Assets/Resources/Sprites/Environment/weaponsmith.png',
 'Assets/Resources/Sprites/Live16x24/shared-specialists.png',
 'ArtSource/Live16x24/shared-specialists/shared-specialists.png',
 'ArtSource/Live16x24/shared-specialists/source-cells.json',
 'ArtSource/Live16x24/shared-specialists/editable.blend',
 'ArtSource/Live16x24/shared-specialists/manifest.json',
 'ArtSource/Live16x24/shared-specialists/import-spec.json',
 'ArtSource/Live16x24/shared-specialists/runtime-publication.json',
 'Tools/Live16x24/build_shared_specialists.py',
 'Docs/Verification/Live16x24/shared-specialists-native-green.xml')

MAIN_LITERALS = {
    'deep-caves-materials-shared-stone-steam': 'shared-settlement-specialists',
    'packaged-deep-caves-linux': 'packaged-specialists-linux',
    '\\d{4}-\\d{2}-\\d{2}-deep-caves-': '\\d{4}-\\d{2}-\\d{2}-specialists-',
    'Both fresh output folders must identify this deep-caves source and end in -current.': 'Both fresh output folders must identify this specialists source and end in -current.',
    'Batched ordinary Quartzite depths 9–10 and Obsidian depths above 10 material coverage, shared neutral Pillar/BrokenColumn bodies and neutral natural SteamVent bodies. Twelve original opaque 16×24 floor/wall cells retain straight boundaries; approved stone-prop and vent bodies are borrowed unchanged. Guarded real-floor/backdrop handling, current graph/disclosure, owner/reader authority and existing damage/source mechanics remain required. Authored East SteamVent grille and independent East steam overlay remain separate; FrostVent is not covered. No generation, collision, saved-gameplay, global grading or camera change; isolated Shale study remains default-off.': 'Batched exact living Weaponsmith, Tinker and WellKeeper native bodies. Weaponsmith borrows its approved 16-by-16 square texture unchanged; Tinker and WellKeeper use two original editable 16-by-24 cells. Current owner, disclosure and independent resource guards preserve mutable health, equipment, names, relationships, conversation, trade and purification teaching. Existing Villager and Merchant bodies remain unchanged. No new service, blueprint, save format, world geometry, global grading or camera behavior; isolated Shale study remains default-off.',
    'Scope is exact eligible ordinary Quartzite/Obsidian materials plus conservative shared Pillar/BrokenColumn/SteamVent identities. Other room props, actors, hazards, authored areas and world families remain open. Actual Obsidian depths 11/12 samples do not certify every deeper address.': 'Scope is three exact eligible settlement resident roles: Weaponsmith, Tinker and WellKeeper. Other professions, service props, actors, areas and world families remain outside this slice. Actual Sill observations, purchases, ordinary movement and persistence have separate evidence.',
    'Material/body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Repeated faces, dark internal facets, low-contrast fragments, sparse composition and mixed glyphs remain separate concerns. Actual journeys and persistence have separate records.': 'Body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Mixed cast scale, remaining glyphs, small distant role recognition and surrounding settlement composition remain separate polish work. No new animation or broad world completion is claimed.',
    'Three complete retained native Results contain ': 'One complete retained native XML contains ',
    ' distinct Passed identities across 627 occurrences with 102 repeats: 419 stone plus 19/189 Steam. These are sequential checkpoints, not one final-source combined run. No earlier subset execution is added, no actual job results/times fabricated, and no performance claim follows.': ' distinct Passed identities in one 99-case resident checkpoint: 19 specialists, 17 Weaponsmith, 24 residents and 39 incidental-actor cases. Earlier subset executions are not added. Native timestamps come directly from this pinned XML; no job metadata, gameplay or performance certification is invented.',
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
              and all(xml.get(k)==v for k,v in {'total':'99','passed':'99','failed':'0','skipped':'0','inconclusive':'0'}.items()),
              'Expected one all-Passed 99-case native run.')
    names={row.get('fullname') for row in cases}
    a.require(len(cases)==len(names)==99 and all(isinstance(n,str) and n for n in names)
              and all(row.get('result')=='Passed' for row in cases), 'Missing/duplicate/non-Passed native case.')
    expected={'CavesOfOoo.Tests.FlatPixelSharedSpecialistsTests':19,
              'CavesOfOoo.Tests.FlatPixelSharedWeaponsmithTests':17,
              'CavesOfOoo.Tests.FlatPixelSharedResidentsTests':24,
              'CavesOfOoo.Tests.FlatPixelIncidentalActorTests':39}
    a.require(dict(collections.Counter(row.get('classname') for row in cases))==expected,
              'Native fixture scope differs from the reviewed run.')
    start=a.timestamp(xml.attrib['start-time']); end=a.timestamp(xml.attrib['end-time'])
    a.require(start<=end<=boundary, 'Native XML must precede root source/build boundary.')
    return {'receipts':[{'path':str(path),'sha256':NATIVE_SHA256,'summary':dict(xml.attrib),
                        'actual_case_rows':99,'mode':'EditMode',
                        'timestamp_source':'Actual native XML; no separate job metadata is inferred.'}],
            'current_source_binding':{'source_commit':SOURCE_COMMIT,'pins':PINS,
                'current_source_evidence_resource_bytes_match':True,
                'limits':'Final selected bytes are pinned separately from the retained runtime test checkpoint. '
                         'Root asserts source/build provenance; later art revision and gameplay acceptance need their own evidence.'},
            'execution_occurrences':99,'passed_execution_occurrences':99,
            'failed_execution_occurrences':0,'skipped_execution_occurrences':0,'inconclusive_execution_occurrences':0,
            'repeated_identity_occurrences':0,'distinct_test_identities_across_listed_receipts':99,
            'distinct_passed_test_identities':99,'distinct_skipped_test_identities':0,
            'distinct_inconclusive_test_identities':0,'all_cases_passed':True,
            'specialists_focused_passed':19,'weaponsmith_passed':17,'other_related_passed':63,'native_job_times_verified':False,
            'native_xml_times_verified':True,'skipped_identities':[],'inconclusive_identities':[],
            'limits':'One 99-case XML only; no earlier Weaponsmith or specialist subset counts added. '
                     'No test rerun, final-art timing, gameplay, Linux/Deck or performance certification is invented.'}


def configure_auditor():
    if hashlib.sha256(BASE_PATH.read_bytes()).hexdigest()!=BASE_SHA256:
        raise RuntimeError('Existing audited base changed; re-review before using this wrapper.')
    spec=importlib.util.spec_from_file_location('specialists_audited_base', BASE_PATH)
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
