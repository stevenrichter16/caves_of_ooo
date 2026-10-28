#!/usr/bin/env python3
"""Finite village-role and sidearm wrapper, published source 0b888da80.

Reuses the SHA-pinned durable deep-caves auditor with the prior surface-actors
wrapper's checked literal-only main adaptation. Build/archive/collision guards
and control flow remain unchanged. No Unity, Git, game or build invocation.
--dry-run writes nothing. Prior packages and normal state are preserved by the
root's separate fresh snapshot/restoration workflow, never by this packager.

One retained complete JSON job: 146 unique Passed, with actual job start/finish
timestamps. Earlier subsets are not added; actual gameplay and package review
have separate evidence. No tests, Unity, builds or restoration are run here.
"""
import collections
import hashlib
import importlib.util
from pathlib import Path
import types
import json

REPO = Path('/Users/steven/caves-of-ooo')
BASE_PATH = REPO/'Docs/Verification/Live16x24/deep-caves-package-audit.py'
BASE_SHA256 = 'fee94989ff27720ec8c3fcdd3270e7b870f11e13668f821c94f64b08f9c977b7'
SOURCE_COMMIT = '0b888da80571bf35ab4f5348ff6e600d289340e7'
FINALIZED = True  # Published source and selected source/import bytes reviewed.
PINS = {'Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs': '4f24f8736cda86dd0d9d94f698dc416b1235fd765faf28e41db645076e173d50',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedResidentsCatalog.cs': 'e499b78d96994e16972dc90a574cc2428aacc5b340a474bbd329e3878f5729d2',
 'Assets/Scripts/Presentation/Rendering/FlatPixelIncidentalActorCatalog.cs': 'e8b34461e80f5fbdabe3aca7827e00c0b79d3f3635679b94fdd55311ab6ee071',
 'Assets/Scripts/Presentation/Rendering/FlatPixelIncidentalEquipmentCatalog.cs': '0e6d9c4994ee2260a02b20414dc0dc7c2ea0f8c0d4e1816fecfb83b5028891c5',
 'Assets/Tests/EditMode/FlatPixelSharedVillageRolesTests.cs': 'e430a8b00b4acddb896e6b8372d06d44917ab86b832f0220d6d7021eb733c7f6',
 'Assets/Tests/EditMode/FlatPixelSharedSidearmsTests.cs': '66b429744e8a6030d432fd91486587b1eb470d3e41a4d428443a1bea1e77b6ad',
 'Assets/Tests/EditMode/FlatPixelSharedResidentsTests.cs': 'ac3dd19686223cd93a9a2f949b23c22630029732bd1006744c427ead6e9acc53',
 'Assets/Tests/EditMode/FlatPixelSharedWeaponsmithTests.cs': '07749008e2f2a1dc76da594b645e310a9f7b851c577ea1a05ade5246d21ed448',
 'Assets/Tests/EditMode/FlatPixelSharedSpecialistsTests.cs': '459052551b2236a8c9843874b7bcc3ab7956aa4977c459cadaad2491742ad842',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelIncidentalActorTests.cs': 'd6e6cfc758e4f8bf01768ea25d12930c15109b495d981565500ced1947f03929',
 'Assets/Resources/Sprites/Live16x24/farmer.png': '4981b3cb861d30a3c8db4b426237f2a751dd48eca584034dac43d78acb63bfc0',
 'Assets/Resources/Sprites/Environment/elder.png': '5d4403f377001ad77e0dfb3f8cd0943c7382d9e366094df7a9f6fcd367ede61e',
 'Assets/Resources/Sprites/Environment/village_child.png': '836bdc9f82e46d120ed7a7a7d41e0e4c74738caef7db201fe884ba2d382188db',
 'Assets/Resources/Sprites/Environment/item_dagger.png': '065019eb5627ff8ac6cc2b7dc6b2ce65d3bb44c0737568de97950e2c6c61ba61',
 'Assets/Resources/Sprites/Environment/item_sword.png': '08860df5fbf7a8f395890756b7fab96f38326fdfed9a3c70238ae5ad73f4c8ee',
 'Assets/Resources/Sprites/Environment/item_mace.png': 'fc6c68c6d4895bb9cf6a4385e6b410447058125cd537138d82d6973a4adca215',
 'ArtSource/Live16x24/incidental-actors/farmer.png': '4981b3cb861d30a3c8db4b426237f2a751dd48eca584034dac43d78acb63bfc0',
 'ArtSource/Live16x24/incidental-actors/source-cells.json': '2451a596b8fb0bc59e287db81c40d83e2fd7bf1d82a840cc384a376d37d83636',
 'ArtSource/Live16x24/incidental-actors/incidental-actors.blend': 'd893df02c96c512061e424f70a4b999e91ebca09bb208ed2faf1359d4ae3034b',
 'ArtSource/Live16x24/incidental-actors/manifest.json': '3e9e8b5b11d570542e7860bcc2c85d3116a24e9155de49ba4852812f711b01b5',
 'Docs/Verification/Live16x24/shared-village-roles-native-green.json': 'a2fc42ff0ef686e52a5ac87facd8a7261c22c46de6b5a1cfe3e8624ecd0afc43'}
NATIVE_NAME = 'shared-village-roles-native-green.json'
NATIVE_SHA256 = 'a2fc42ff0ef686e52a5ac87facd8a7261c22c46de6b5a1cfe3e8624ecd0afc43'

REQUIRED_BINDINGS = ('Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedResidentsCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelIncidentalActorCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelIncidentalEquipmentCatalog.cs',
 'Assets/Tests/EditMode/FlatPixelSharedVillageRolesTests.cs',
 'Assets/Tests/EditMode/FlatPixelSharedSidearmsTests.cs',
 'Assets/Tests/EditMode/FlatPixelSharedResidentsTests.cs',
 'Assets/Tests/EditMode/FlatPixelSharedWeaponsmithTests.cs',
 'Assets/Tests/EditMode/FlatPixelSharedSpecialistsTests.cs',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelIncidentalActorTests.cs',
 'Assets/Resources/Sprites/Live16x24/farmer.png',
 'Assets/Resources/Sprites/Environment/elder.png',
 'Assets/Resources/Sprites/Environment/village_child.png',
 'Assets/Resources/Sprites/Environment/item_dagger.png',
 'Assets/Resources/Sprites/Environment/item_sword.png',
 'Assets/Resources/Sprites/Environment/item_mace.png',
 'ArtSource/Live16x24/incidental-actors/farmer.png',
 'ArtSource/Live16x24/incidental-actors/source-cells.json',
 'ArtSource/Live16x24/incidental-actors/incidental-actors.blend',
 'ArtSource/Live16x24/incidental-actors/manifest.json',
 'Docs/Verification/Live16x24/shared-village-roles-native-green.json')

MAIN_LITERALS = {
    'deep-caves-materials-shared-stone-steam': 'shared-village-roles-and-sidearms',
    'packaged-deep-caves-linux': 'packaged-village-roles-linux',
    '\\d{4}-\\d{2}-\\d{2}-deep-caves-': '\\d{4}-\\d{2}-\\d{2}-village-roles-',
    'Both fresh output folders must identify this deep-caves source and end in -current.': 'Both fresh output folders must identify this village-roles source and end in -current.',
    'Batched ordinary Quartzite depths 9–10 and Obsidian depths above 10 material coverage, shared neutral Pillar/BrokenColumn bodies and neutral natural SteamVent bodies. Twelve original opaque 16×24 floor/wall cells retain straight boundaries; approved stone-prop and vent bodies are borrowed unchanged. Guarded real-floor/backdrop handling, current graph/disclosure, owner/reader authority and existing damage/source mechanics remain required. Authored East SteamVent grille and independent East steam overlay remain separate; FrostVent is not covered. No generation, collision, saved-gameplay, global grading or camera change; isolated Shale study remains default-off.': 'Batched shared Farmer, Elder and VillageChild bodies and exact Dagger, ShortSword and Mace item bodies. Six approved PNGs are reused unchanged: Farmer remains a native 16-by-24 body; Elder, Child and the three sidearms use their complete 16-by-16 square cells. Exact owner, current graph, disclosure, custody and independent resource guards preserve health, equipment, AI, conversation, trade, damage and item mechanics. Farmer damage and wet cues use the existing single Render event. Child has no invented conversation or trader service. No new art, blueprint, save format, world geometry, grading or camera behavior; isolated Shale study remains default-off.',
    'Scope is exact eligible ordinary Quartzite/Obsidian materials plus conservative shared Pillar/BrokenColumn/SteamVent identities. Other room props, actors, hazards, authored areas and world families remain open. Actual Obsidian depths 11/12 samples do not certify every deeper address.': 'Scope is three living village roles and three sidearm identities. Farmer portability extends only its modern world and reader routes; Grazer and Cindercaller remain local. Other actors, equipment, service props, areas and world families remain outside this slice. Actual Sill movement, selected-pile readers, available services, item custody and persistence have separate evidence.',
    'Material/body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Repeated faces, dark internal facets, low-contrast fragments, sparse composition and mixed glyphs remain separate concerns. Actual journeys and persistence have separate records.': 'Body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. The static Elder body does not encode Sitting; existing status text does. Mixed cast scale, remaining glyphs and settlement composition remain separate polish work. No new animation or broad world completion is claimed.',
    'Three complete retained native Results contain ': 'One complete retained native JSON job contains ',
    ' distinct Passed identities across 627 occurrences with 102 repeats: 419 stone plus 19/189 Steam. These are sequential checkpoints, not one final-source combined run. No earlier subset execution is added, no actual job results/times fabricated, and no performance claim follows.': ' distinct Passed identities in one 146-case checkpoint: 24 village roles, 23 sidearms, 24 residents, 17 Weaponsmith, 19 specialists and 39 incidental-actor cases. Earlier subset runs are not added. Actual start/finish timestamps are read from the complete successful job envelope; no XML, test rerun, gameplay or performance certification is invented.',
}


def native_evidence(boundary):
    a=AUDITOR
    a.require(FINALIZED and isinstance(SOURCE_COMMIT,str) and len(SOURCE_COMMIT)==40
              and set(PINS)==set(REQUIRED_BINDINGS), 'Final source/art pins are not yet reviewed; packaging disabled.')
    for name,expected in PINS.items():
        a.require(a.digest(a.canonical(REPO/name))==expected, 'Frozen selected source/resource differs: '+name)
    path=a.canonical(a.EVIDENCE/NATIVE_NAME)
    a.require(a.digest(path)==NATIVE_SHA256, 'Native JSON differs from the reviewed complete receipt.')
    job=json.loads(path.read_text())
    a.require(isinstance(job,dict) and job.get('job_id')=='c5ab6ba713ec4d97a5d9cd835ae4ad8f'
              and job.get('status')=='succeeded' and job.get('mode')=='EditMode'
              and job.get('error') is None, 'Expected the reviewed successful native job.')
    progress=job.get('progress',{}); result=job.get('result',{})
    a.require(isinstance(progress,dict) and progress.get('completed')==146 and progress.get('total')==146
              and progress.get('failures_so_far')==[] and progress.get('failures_capped') is False,
              'Native job progress is incomplete or reports failures.')
    a.require(isinstance(result,dict) and result.get('mode')=='EditMode', 'Missing complete EditMode Results.')
    summary=result.get('summary',{}); cases=result.get('results',[])
    a.require(isinstance(summary,dict) and summary.get('resultState')=='Passed'
              and all(summary.get(k)==v for k,v in {'total':146,'passed':146,'failed':0,'skipped':0}.items())
              and summary.get('inconclusive',0)==0, 'Expected one all-Passed 146-case native run.')
    a.require(isinstance(cases,list) and len(cases)==146
              and all(isinstance(row,dict) and isinstance(row.get('fullName'),str)
                      and row['fullName'] and row.get('state')=='Passed' for row in cases),
              'Missing or non-Passed native case.')
    names=[row['fullName'] for row in cases]
    a.require(len(set(names))==146, 'Duplicate native identity.')
    expected={'CavesOfOoo.Tests.FlatPixelSharedVillageRolesTests':24,
              'CavesOfOoo.Tests.FlatPixelSharedSidearmsTests':23,
              'CavesOfOoo.Tests.FlatPixelSharedResidentsTests':24,
              'CavesOfOoo.Tests.FlatPixelSharedWeaponsmithTests':17,
              'CavesOfOoo.Tests.FlatPixelSharedSpecialistsTests':19,
              'CavesOfOoo.Tests.FlatPixelIncidentalActorTests':39}
    a.require(dict(collections.Counter(n.rsplit('.',1)[0] for n in names))==expected,
              'Native fixture scope differs from the reviewed run.')
    start_ms=job.get('started_unix_ms'); end_ms=job.get('finished_unix_ms')
    a.require(type(start_ms) is int and type(end_ms) is int and 0<start_ms<=end_ms,
              'Native job must contain actual positive start and finish timestamps.')
    start=a.dt.datetime.fromtimestamp(start_ms/1000,a.dt.timezone.utc)
    end=a.dt.datetime.fromtimestamp(end_ms/1000,a.dt.timezone.utc)
    a.require(start<=end<=boundary, 'Native job must precede root source/build boundary.')
    return {'receipts':[{'path':str(path),'sha256':NATIVE_SHA256,'summary':summary,
                        'actual_case_rows':146,'mode':'EditMode','job_id':job['job_id'],
                        'started_unix_ms':start_ms,'finished_unix_ms':end_ms,
                        'started_at_utc':start.isoformat(),'finished_at_utc':end.isoformat(),
                        'timestamp_source':'Actual complete native JSON job envelope; no inferred XML or times.'}],
            'current_source_binding':{'source_commit':SOURCE_COMMIT,'pins':PINS,
                'current_source_evidence_resource_bytes_match':True,
                'limits':'Selected current source, resource and fixture bytes are pinned separately. '
                         'Root asserts source/build provenance; actual gameplay and package acceptance have separate evidence.'},
            'execution_occurrences':146,'passed_execution_occurrences':146,
            'failed_execution_occurrences':0,'skipped_execution_occurrences':0,'inconclusive_execution_occurrences':0,
            'repeated_identity_occurrences':0,'distinct_test_identities_across_listed_receipts':146,
            'distinct_passed_test_identities':146,'distinct_skipped_test_identities':0,
            'distinct_inconclusive_test_identities':0,'all_cases_passed':True,
            'village_roles_focused_passed':24,'sidearms_passed':23,'other_related_passed':99,
            'native_job_times_verified':True,'native_xml_times_verified':False,
            'skipped_identities':[],'inconclusive_identities':[],
            'limits':'One complete 146-case JSON job only; earlier subset executions are not added. '
                     'No test rerun, gameplay, Linux/Deck execution or performance certification is invented.'}


def configure_auditor():
    if hashlib.sha256(BASE_PATH.read_bytes()).hexdigest()!=BASE_SHA256:
        raise RuntimeError('Existing audited base changed; re-review before using this wrapper.')
    spec=importlib.util.spec_from_file_location('village_roles_audited_base', BASE_PATH)
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
