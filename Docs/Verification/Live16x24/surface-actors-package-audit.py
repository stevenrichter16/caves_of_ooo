#!/usr/bin/env python3
"""Small, finite wrapper around the SHA-pinned existing deep-caves auditor.

Use its normal CLI with surface-actors output folders, source 8c0c72e11 and
--receipt-prefix packaged-surface-actors-linux. --dry-run writes nothing.
The wrapper changes only nine checked main string literals, source/evidence
bindings and the native evidence provider. Build/archive/collision checks and
all main bytecode/control flow are retained. No build, game or Git invocation.
Native evidence is 154 + 139 retained occurrences, 24 repeated identities,
269 distinct Passed, NOT a combined final-source run. No job times are claimed.
"""
import collections
import hashlib
import importlib.util
import json
from pathlib import Path
import types

REPO = Path('/Users/steven/caves-of-ooo')
BASE_PATH = REPO/'Docs/Verification/Live16x24/deep-caves-package-audit.py'
BASE_SHA256 = 'fee94989ff27720ec8c3fcdd3270e7b870f11e13668f821c94f64b08f9c977b7'
SOURCE_COMMIT = '8c0c72e11530179fcca73d8fe429b71cd9ccb25b'

PINS = {
    'Assets/Resources/Sprites/Environment/brittle_hound.png': '6b277f949cf55edad26579ebfc732d93af95f285650b6dfaabe94ad60d94fea8',
    'Assets/Resources/Sprites/Environment/glassblown_drifter.png': '2dbfbc75816a83e7bda5243cc87ef11834684624cadeea2428d70c8b3ed5bcd0',
    'Assets/Resources/Sprites/Environment/merchant.png': 'd79c98d33a03b156c400e9f7120c99f1a133a67468c3dfee609e77af53ea6f2e',
    'Assets/Resources/Sprites/Environment/scorpion.png': '32650e55a2a1284984fb11129b243242f4f613551e175272f0535a55c3205091',
    'Assets/Resources/Sprites/Live16x24/opening-cast.png': '35a2de34d9bf6d6dfe66c7dd47439f7ac03ec6b7de9e09f026d6cb623e24d73a',
    'Assets/Scripts/Presentation/Rendering/FlatPixelBeatingActorsCatalog.cs': '6969393677d17fa5efb58667504e5df5867083ba9763cf6bead192ad324322e0',
    'Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs': 'a8eefa30a32daea0f601bbf826063e872cb9a029b16954381b156f6041d12762',
    'Assets/Scripts/Presentation/Rendering/FlatPixelSharedResidentsCatalog.cs': 'efec19a6fd5f99f47a64e4b3d8c8d43d0c7d15152535aadcac06996eb0a54b4d',
    'Assets/Tests/EditMode/FlatPixelBeatingActorsTests.cs': '05c1a605a7d7e05b2853706154db56708eac9f13f1c2735133db7d9d441d5846',
    'Assets/Tests/EditMode/FlatPixelSharedResidentsTests.cs': 'ac3dd19686223cd93a9a2f949b23c22630029732bd1006744c427ead6e9acc53',
    'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelOpeningArtCatalogTests.cs': '8a88bf008572c621c56802b747a73ad1caa253a94a4bba77da61b07577e34305',
    'Docs/Verification/Live16x24/beating-actors-final-139.json': 'd6a6931e8170becaf30a801c51dcea1306ae105616fe16959fd391b26d104b15',
    'Docs/Verification/Live16x24/shared-residents-final154.json': 'c02b642bd7a3144cd13355bf6902a23e47d6abdc9c5d56ad947ed9f008fb9dea',
}

MAIN_LITERALS = {
    'deep-caves-materials-shared-stone-steam': 'shared-residents-beating-actors',
    'packaged-deep-caves-linux': 'packaged-surface-actors-linux',
    '\\d{4}-\\d{2}-\\d{2}-deep-caves-': '\\d{4}-\\d{2}-\\d{2}-surface-actors-',
    'Both fresh output folders must identify this deep-caves source and end in -current.': 'Both fresh output folders must identify this surface-actors source and end in -current.',
    'Batched ordinary Quartzite depths 9–10 and Obsidian depths above 10 material coverage, shared neutral Pillar/BrokenColumn bodies and neutral natural SteamVent bodies. Twelve original opaque 16×24 floor/wall cells retain straight boundaries; approved stone-prop and vent bodies are borrowed unchanged. Guarded real-floor/backdrop handling, current graph/disclosure, owner/reader authority and existing damage/source mechanics remain required. Authored East SteamVent grille and independent East steam overlay remain separate; FrostVent is not covered. No generation, collision, saved-gameplay, global grading or camera change; isolated Shale study remains default-off.': 'Batched exact neutral Villager/Merchant and Scorpion/BrittleHound/GlassblownDrifter native bodies. Five approved source sprites are borrowed unchanged; no new pixels or animation. Current owner, disclosure, live-body, reskin and resource guards preserve separate corpses, legitimate services, damage/status cues and existing local identities. Farmer, Ellun and other specialist or reskinned residents remain outside this slice. No blueprint, AI, economy, save-format, geometry, global grading or camera change; isolated Shale study remains default-off.',
    'Scope is exact eligible ordinary Quartzite/Obsidian materials plus conservative shared Pillar/BrokenColumn/SteamVent identities. Other room props, actors, hazards, authored areas and world families remain open. Actual Obsidian depths 11/12 samples do not certify every deeper address.': 'Scope is the five exact eligible living identities: Villager, Merchant, Scorpion, BrittleHound and GlassblownDrifter. Services and contents remain simulation-owned. Actual Sill and Beating observations are bounded samples; Farmer, specialist residents, other actors, hazards and regional families remain open.',
    'Material/body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Repeated faces, dark internal facets, low-contrast fragments, sparse composition and mixed glyphs remain separate concerns. Actual journeys and persistence have separate records.': 'Body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Mixed native cell sizes, sparse composition and remaining glyphs need later cohesive scene review. Actual journeys, purchases, combat and persistence have separate records.',
    'Three complete retained native Results contain ': 'Two complete retained native Results contain ',
    ' distinct Passed identities across 627 occurrences with 102 repeats: 419 stone plus 19/189 Steam. These are sequential checkpoints, not one final-source combined run. No earlier subset execution is added, no actual job results/times fabricated, and no performance claim follows.': ' distinct Passed identities across 293 occurrences with 24 repeated SharedResidents identities: 154 residents checkpoint plus 139 Beating checkpoint. These are sequential checkpoints, not one final-source combined run. No earlier subset execution is added. Exported Results provide no job timestamps; none are invented. No performance claim follows.',
}

RESULTS = (
    ('shared-residents-final154.json', 154,
     {'FlatPixelGrovelandsActorsTests':44, 'FlatPixelIncidentalActorTests':39,
      'FlatPixelOpeningArtCatalogTests':26, 'FlatPixelOpeningCastTests':21,
      'FlatPixelSharedResidentsTests':24}),
    ('beating-actors-final-139.json', 139,
     {'FlatPixelBeatingActorsTests':20, 'FlatPixelBeatingMaterialsTests':95,
      'FlatPixelSharedResidentsTests':24}),
)


def native_evidence(boundary):
    """Read only real exported Results; boundary is not a fabricated test time."""
    a = AUDITOR
    for name, expected in PINS.items():
        a.require(a.digest(a.canonical(REPO/name)) == expected,
                  'Frozen selected source/evidence/resource differs: ' + name)
    receipts, groups = [], []
    for name, expected_total, expected_classes in RESULTS:
        path = a.canonical(a.EVIDENCE/name)
        raw = json.loads(path.read_text())
        a.require(isinstance(raw, dict) and raw.get('mode') == 'EditMode', 'Expected native EditMode Result.')
        summary, cases = raw.get('summary'), raw.get('results')
        a.require(isinstance(summary, dict) and isinstance(cases, list) and len(cases)==expected_total,
                  'Native case count/summary differs.')
        a.require(all(isinstance(row, dict) and isinstance(row.get('fullName'), str)
                      and row['fullName'] and row.get('state')=='Passed' for row in cases),
                  'Missing identity or non-Passed native case.')
        names={row['fullName'] for row in cases}
        a.require(len(names)==expected_total, 'Duplicate native identities.')
        a.require(summary.get('total')==summary.get('passed')==expected_total
                  and summary.get('failed')==summary.get('skipped')==0
                  and summary.get('inconclusive',0)==0 and summary.get('resultState')=='Passed',
                  'Only complete all-Passed native Results are accepted.')
        a.require(sum(expected_classes.values())==len(names) and all(
            sum(n.startswith('CavesOfOoo.Tests.'+cls+'.') for n in names)==count
            for cls,count in expected_classes.items()), 'Native fixture identities differ.')
        receipts.append({'path':str(path), 'sha256':PINS[str(path.relative_to(REPO))],
                         'mode':'EditMode', 'summary':summary, 'actual_case_rows':len(cases),
                         'job_metadata':'Not supplied by these exported Results; no job times inferred.',
                         'scope':'Retained sequential native checkpoint, not executed by this wrapper.'})
        groups.append(names)
    overlap=groups[0]&groups[1]
    a.require(len(overlap)==24 and all(n.startswith('CavesOfOoo.Tests.FlatPixelSharedResidentsTests.')
                                    for n in overlap), 'Expected exact 24 SharedResidents overlap.')
    a.require(len(groups[0]|groups[1])==269, 'Expected 269 distinct retained Passed identities.')
    return {'receipts':receipts,
            'current_source_binding':{'source_commit':SOURCE_COMMIT, 'pins':PINS,
                'current_source_evidence_resource_bytes_match':True,
                'limits':'Selected current files match reviewed byte pins. Earlier Results remain sequential checkpoints. '
                         'Root asserts source freeze/build provenance; no binary reproducibility or native job timing proof.'},
            'execution_occurrences':293, 'passed_execution_occurrences':293,
            'failed_execution_occurrences':0, 'skipped_execution_occurrences':0,
            'inconclusive_execution_occurrences':0, 'repeated_identity_occurrences':24,
            'distinct_test_identities_across_listed_receipts':269, 'distinct_passed_test_identities':269,
            'distinct_skipped_test_identities':0, 'distinct_inconclusive_test_identities':0,
            'all_cases_passed':True, 'combined_final_source_execution':False,
            'native_job_times_verified':False, 'residents_checkpoint_passed':154, 'beating_checkpoint_passed':139,
            'skipped_identities':[], 'inconclusive_identities':[],
            'limits':'154 residents plus 139 Beating occurrences, deduplicated by exact fullName, give 269 unique Passed. '
                     'No execution dates or job metadata are invented. No test rerun, gameplay, Linux/Deck or performance certification.'}


def configure_auditor():
    if hashlib.sha256(BASE_PATH.read_bytes()).hexdigest()!=BASE_SHA256:
        raise RuntimeError('Existing audited base changed; re-review before using this wrapper.')
    spec=importlib.util.spec_from_file_location('surface_actors_audited_base', BASE_PATH)
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
    AUDITOR.main()
