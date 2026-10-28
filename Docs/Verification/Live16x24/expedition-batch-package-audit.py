#!/usr/bin/env python3
"""Finite Greatdew/portable-supplies delivery wrapper; no Unity/Git invocation.

Reuses the pinned package auditor's archive/license/checksum/collision workflow.
Root must fill the explicit build/copy receipt markers and set FINALIZED after
review. --dry-run writes nothing. Raw Linux build bytes/receipt are preserved;
a separate identical canonical-name executable is required by the launcher.
"""
import collections
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys
import types

REPO = Path('/Users/steven/caves-of-ooo')
EVIDENCE = REPO/'Docs/Verification/Live16x24'
BASE_PATH = EVIDENCE/'deep-caves-package-audit.py'
BASE_SHA256 = 'fee94989ff27720ec8c3fcdd3270e7b870f11e13668f821c94f64b08f9c977b7'
SOURCE_COMMIT = '43a151ddbd714d1bf1a786084e88bc2cfcd5106b'
FOLDER = '2026-10-28-expedition-batch-43a151ddb-current'
FINALIZED = True
BUILT_AFTER = '2026-10-28T11:55:27.183502+00:00'
LINUX_REPORT_SHA256 = '1b8377b86094a469a51b949d6a49d77756df4e74a17ad1de3ec96d955095a45f'
LINUX_COPY_REPORT_SHA256 = '9ecbaadb6612d95b03cf9952b1d8bc462dbd788defd4af87d2fe9e2a629f7aa1'
LINUX_EXECUTABLE_SHA256 = '8027f7d1f9ae7dacfc826fb218adcc1ae7464af098a8a59397e7531c0f7ec0bc'
LINUX_EXECUTABLE_BYTES = 4472
LINUX_JOB_ID = 'build-1b42eb4cdd'
LINUX_ROOT = REPO/'Builds/SteamDeck'/FOLDER/'CavesOfOoo-current'
MAC_APP = REPO/'Builds/Verification'/FOLDER/'CavesOfOoo.app'
LINUX_REPORT = EVIDENCE/'packaged-expedition-batch-linux-build.json'
MAC_REPORT = EVIDENCE/'packaged-expedition-batch-mac-build.json'
LINUX_COPY_REPORT = EVIDENCE/'packaged-expedition-batch-linux-executable-copy.json'
NATIVE_NAME = 'expedition-essentials-native-green.json'
NATIVE_SHA256 = '8197074b67f3758d7fd264feee148ed47da6daa9694af2b52c191f4c1de262ff'
NATIVE_JOB_ID = '5dcaf42cc8e74b929a7bf0007b67658a'
PINS = {'Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs': 'ee39e4e07c9e7a2b6162246ef49b815acaebe5cdff2fd3f6e9daf56a7ba94721',
 'Assets/Scripts/Presentation/Rendering/FlatPixelExpeditionEssentialsCatalog.cs': 'b5fd381ef94b44a8e6c4589f25e0bc7a1edbe1e721674bcd547c1ac71bd7ccc8',
 'Assets/Scripts/Presentation/Rendering/FlatPixelGreatdewCatalog.cs': '84babaf20c591fc0995082a12dcebc4578883c42eeefcd10f7f8cbac4ae70e00',
 'Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs': '18d3f179d9341bc1458b534eaf93dd6d434cb7bee60b582e8c6bc48b2705eca5',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelExpeditionEssentialsTests.cs': '19e3b8f1436cf3ac0795deda7fb9a765d91d8275347818e2acfab4b0611c224d',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGreatdewTests.cs': '2f654be9b0a1a6c4485cd1b91361f57920c0be199900c9d7d503613f36b6e2cb',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGlobalPresentationTests.cs': '2e52b955f08d9d3596edebcd42c626ccb798e80aa09f8cf5d33fa699af668f71',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelOpeningItemsTests.cs': '66834469907190618fea16ba1f3f54796571fbc54777a9db07af4b0042227eac',
 'Assets/Resources/Sprites/Live16x24/greatdew.png': '3dab2ae235b74ef5ea648348705ff9aa73f576c2e09a2cfb372879bfece7ec33',
 'Assets/Resources/Sprites/Live16x24/opening-items.png': '1ead2618c2d82d2ca7cae8765ab09f690255998ad0bb5b36027d7835fc292394',
 'Assets/Resources/Sprites/Environment/item_book.png': '5a15f96a509ada25d3400f023ce63fb7072f4671d1091bd5e5a17474d260cbec',
 'ArtSource/Live16x24/greatdew/source-cells.json': 'b57800e458f0c00113dcd4422a33fd731a0c857291d44773233a200dac852444',
 'ArtSource/Live16x24/greatdew/editable.blend': '8f86abdcff21d67aa348c77daa60c3dd5008de6d2c9d5d2f4fc8d6cd1ac7f33f',
 'Docs/Verification/Live16x24/expedition-essentials-native-green.json': '8197074b67f3758d7fd264feee148ed47da6daa9694af2b52c191f4c1de262ff'}
MAIN_LITERALS = {
    'deep-caves-materials-shared-stone-steam': 'expedition-greatdew-and-portable-supplies',
    'packaged-deep-caves-linux': 'packaged-expedition-batch-linux',
    '\\d{4}-\\d{2}-\\d{2}-deep-caves-': '\\d{4}-\\d{2}-\\d{2}-expedition-batch-',
    'Both fresh output folders must identify this deep-caves source and end in -current.': 'Both fresh output folders must identify this expedition-batch source and end in -current.',
    'Batched ordinary Quartzite depths 9–10 and Obsidian depths above 10 material coverage, shared neutral Pillar/BrokenColumn bodies and neutral natural SteamVent bodies. Twelve original opaque 16×24 floor/wall cells retain straight boundaries; approved stone-prop and vent bodies are borrowed unchanged. Guarded real-floor/backdrop handling, current graph/disclosure, owner/reader authority and existing damage/source mechanics remain required. Authored East SteamVent grille and independent East steam overlay remain separate; FrostVent is not covered. No generation, collision, saved-gameplay, global grading or camera change; isolated Shale study remains default-off.': 'Original native 16-by-24 Greatdew plant body plus existing approved HealingTonic and DriedMeat complete opening-items frames and the complete square WateringGrimoire book body. Exact owner, current graph, disclosure, custody, semantic-part, optional-resource and existing Render-cue guards preserve the real repeatable step snare, consumable uses and skill-teaching Read. No item stock, mechanics, generation, collision, save-format, camera or global grading change.',
    'Scope is exact eligible ordinary Quartzite/Obsidian materials plus conservative shared Pillar/BrokenColumn/SteamVent identities. Other room props, actors, hazards, authored areas and world families remain open. Actual Obsidian depths 11/12 samples do not certify every deeper address.': 'Scope is exact Greatdew, HealingTonic, DriedMeat and WateringGrimoire identities. Other flora, hazards, supplies, equipment and world families remain open. Greatdew has no invented harvesting, aura or disabled state. The approved book remains square, not stretched or animated. Actual Editor and standalone observations have separate receipts.',
    'Material/body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Repeated faces, dark internal facets, low-contrast fragments, sparse composition and mixed glyphs remain separate concerns. Actual journeys and persistence have separate records.': 'Body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Broad sparse mire terrain, remaining glyph props and restrained hazard salience remain separate work. Existing approved sprites and other presentation remain preserved.',
    'Three complete retained native Results contain ': 'One complete retained native JSON job contains ',
    ' distinct Passed identities across 627 occurrences with 102 repeats: 419 stone plus 19/189 Steam. These are sequential checkpoints, not one final-source combined run. No earlier subset execution is added, no actual job results/times fabricated, and no performance claim follows.': ' distinct Passed identities in one 139-case checkpoint: 20 expedition essentials, 21 Greatdew, 46 opening items and 52 global presentation. Earlier subset runs and the earlier 243-Passed/3-Inconclusive Greatdew checkpoint are not added. Actual timestamps come from this complete job; no XML, gameplay or performance certification is invented.',
}


def native_evidence(boundary):
    a = AUDITOR
    a.require(FINALIZED, 'Actual build/copy bindings still pending root review.')
    for name, expected in PINS.items():
        a.require(a.digest(a.canonical(REPO/name)) == expected, 'Frozen source/resource differs: '+name)
    path = a.canonical(EVIDENCE/NATIVE_NAME)
    a.require(a.digest(path) == NATIVE_SHA256, 'Complete native receipt differs.')
    job = json.loads(path.read_text())
    a.require(job.get('job_id') == NATIVE_JOB_ID and job.get('status') == 'succeeded'
              and job.get('mode') == 'EditMode' and job.get('error') is None, 'Wrong native job.')
    progress = job.get('progress') or {}; result = job.get('result') or {}
    cases = result.get('results') or []; summary = result.get('summary') or {}
    a.require(progress.get('total') == 139 and progress.get('completed') == 139
              and progress.get('failures_so_far') == [] and progress.get('failures_capped') is False,
              'Incomplete or failing native progress.')
    a.require(result.get('mode') == 'EditMode' and summary.get('resultState') == 'Passed'
              and all(summary.get(k) == v for k,v in {'total':139,'passed':139,'failed':0,'skipped':0}.items())
              and summary.get('inconclusive',0) == 0, 'Not the complete139-Passed result.')
    a.require(len(cases) == 139 and all(isinstance(row,dict) and row.get('state') == 'Passed'
              and isinstance(row.get('fullName'),str) and row['fullName'] for row in cases), 'Invalid native cases.')
    names = [row['fullName'] for row in cases]
    expected = {'CavesOfOoo.Tests.FlatPixelExpeditionEssentialsTests':20,
                'CavesOfOoo.Tests.FlatPixelGreatdewTests':21,
                'CavesOfOoo.Tests.FlatPixelOpeningItemsTests':46,
                'CavesOfOoo.Tests.FlatPixelGlobalPresentationTests':52}
    a.require(len(set(names)) == 139 and dict(collections.Counter(n.split('(',1)[0].rsplit('.',1)[0]
              for n in names)) == expected, 'Native identities/fixture scope differ.')
    start_ms,end_ms = job.get('started_unix_ms'),job.get('finished_unix_ms')
    a.require(type(start_ms) is int and type(end_ms) is int and 0 < start_ms <= end_ms,
              'Missing actual native job timestamps.')
    start = a.dt.datetime.fromtimestamp(start_ms/1000,a.dt.timezone.utc)
    end = a.dt.datetime.fromtimestamp(end_ms/1000,a.dt.timezone.utc)
    a.require(start <= end <= boundary, 'Native job must precede build source boundary.')
    return {'receipts':[{'path':str(path),'sha256':NATIVE_SHA256,'summary':summary,
                        'actual_case_rows':139,'mode':'EditMode','job_id':NATIVE_JOB_ID,
                        'started_unix_ms':start_ms,'finished_unix_ms':end_ms,
                        'started_at_utc':start.isoformat(),'finished_at_utc':end.isoformat()}],
            'current_source_binding':{'source_commit':SOURCE_COMMIT,'pins':PINS,
                'current_source_evidence_resource_bytes_match':True,
                'limits':'Root asserts source/build provenance; selected bytes are independently pinned, not binary reproducibility.'},
            'execution_occurrences':139,'passed_execution_occurrences':139,
            'failed_execution_occurrences':0,'skipped_execution_occurrences':0,'inconclusive_execution_occurrences':0,
            'repeated_identity_occurrences':0,'distinct_test_identities_across_listed_receipts':139,
            'distinct_passed_test_identities':139,'distinct_skipped_test_identities':0,
            'distinct_inconclusive_test_identities':0,'all_cases_passed':True,
            'fixture_passed_counts':expected,'native_job_times_verified':True,'native_xml_times_verified':False,
            'skipped_identities':[],'inconclusive_identities':[],
            'limits':'One actual final139-case checkpoint only. Earlier243-Passed/3-Inconclusive Greatdew run is not added. No gameplay, Linux/Deck execution or performance claim.'}


def configure_auditor():
    if hashlib.sha256(BASE_PATH.read_bytes()).hexdigest() != BASE_SHA256:
        raise RuntimeError('Pinned package auditor changed; review before reuse.')
    spec = importlib.util.spec_from_file_location('expedition_audited_base',BASE_PATH)
    a = importlib.util.module_from_spec(spec)
    exec(compile(BASE_PATH.read_bytes(),str(BASE_PATH),'exec'),a.__dict__)
    counts = collections.Counter()
    def patch(code):
        values = []
        for value in code.co_consts:
            if isinstance(value,types.CodeType): value = patch(value)
            elif isinstance(value,str) and value in MAIN_LITERALS:
                counts[value] += 1; value = MAIN_LITERALS[value]
            values.append(value)
        changed = code.replace(co_consts=tuple(values))
        if changed.co_code != code.co_code: raise RuntimeError('Auditor control flow changed.')
        return changed
    a.main.__code__ = patch(a.main.__code__)
    if dict(counts) != {key:1 for key in MAIN_LITERALS}:
        raise RuntimeError('Expected one exact occurrence of each reviewed main literal.')
    a.__doc__ = __doc__; a.BOUND_SOURCE_COMMIT = SOURCE_COMMIT; a.NATIVE_BINDINGS = PINS
    a.native_evidence = native_evidence
    original_receipt = a.build_receipt
    def build_receipt(path,platform,expected_output,source,boundary,now):
        if platform != 'StandaloneLinux64':
            return original_receipt(path,platform,expected_output,source,boundary,now)
        a.require(path == LINUX_REPORT and a.digest(a.canonical(path)) == LINUX_REPORT_SHA256
                  and expected_output == LINUX_ROOT/'CavesOfOoo.x86_64', 'Unexpected raw Linux receipt/output.')
        raw = a.canonical(LINUX_ROOT/'CavesOfOoo'); canonical = a.canonical(expected_output)
        copy_path = a.canonical(LINUX_COPY_REPORT)
        a.require(a.digest(copy_path) == LINUX_COPY_REPORT_SHA256, 'Copy receipt bytes differ.')
        copy = json.loads(copy_path.read_text())
        a.require(copy.get('raw_build_executable') == str(raw.relative_to(REPO))
                  and copy.get('canonical_archive_executable') == str(canonical.relative_to(REPO))
                  and copy.get('action') == 'copy2; raw build executable preserved'
                  and copy.get('bytes') == LINUX_EXECUTABLE_BYTES and LINUX_EXECUTABLE_BYTES > 0
                  and copy.get('both_sha256') == LINUX_EXECUTABLE_SHA256
                  and copy.get('mode') == '0o755', 'Copy provenance differs.')
        for executable in (raw,canonical):
            a.require(executable.stat().st_size == LINUX_EXECUTABLE_BYTES
                      and executable.stat().st_mode & 0o7777 == 0o755
                      and a.digest(executable) == LINUX_EXECUTABLE_SHA256, 'Raw/canonical bytes or mode differ.')
        receipt = original_receipt(path,platform,raw,source,boundary,now)
        a.require(receipt['summary']['job_id'] == LINUX_JOB_ID, 'Unexpected Linux build job.')
        copied_at = a.timestamp(copy['at'])
        a.require(a.timestamp(receipt['summary']['completed_at']) <= copied_at <= now+a.dt.timedelta(minutes=2),
                  'Copy must follow actual build.')
        receipt['canonical_executable_copy'] = {'path':str(copy_path),'sha256':LINUX_COPY_REPORT_SHA256,
            'provenance':copy,'both_current_files_hash_size_mode_verified':True,
            'limits':'Raw build receipt/executable unchanged; launcher uses identical canonical copy. Linux not executed.'}
        return receipt
    a.build_receipt = build_receipt
    return a


AUDITOR = configure_auditor()
if __name__ == '__main__':
    AUDITOR.require(FINALIZED, 'Root must fill/review actual build and copy markers before use.')
    for pin in (LINUX_REPORT_SHA256,LINUX_COPY_REPORT_SHA256,LINUX_EXECUTABLE_SHA256):
        AUDITOR.require(re.fullmatch(r'[0-9a-f]{64}',pin), 'Root build/copy SHA marker remains.')
    required = {'--source-commit':SOURCE_COMMIT,'--built-after':BUILT_AFTER,
        '--linux-output-dir':str(LINUX_ROOT),'--mac-app':str(MAC_APP),
        '--linux-build-report':str(LINUX_REPORT),'--mac-build-report':str(MAC_REPORT),
        '--receipt-dir':str(EVIDENCE)}
    for flag,value in required.items():
        if sys.argv.count(flag) != 1 or sys.argv.index(flag)+1 >= len(sys.argv) or sys.argv[sys.argv.index(flag)+1] != value:
            raise RuntimeError('Use exact reviewed '+flag+' value for this delivery.')
    if '--receipt-prefix' in sys.argv:
        index = sys.argv.index('--receipt-prefix')
        if sys.argv.count('--receipt-prefix') != 1 or index+1 >= len(sys.argv) or sys.argv[index+1] != 'packaged-expedition-batch-linux':
            raise RuntimeError('Use fixed packaged-expedition-batch-linux receipt prefix.')
    AUDITOR.main()
