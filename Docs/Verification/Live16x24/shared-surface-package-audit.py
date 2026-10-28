#!/usr/bin/env python3
"""Finite shared-surface delivery wrapper for source fa553ca36.

Reuses the SHA-pinned deep-caves package auditor and literal-only adaptation
from village-roles. Its main control flow, archive checks, collision guards and
read-only dry run are unchanged. One exact Linux receipt adapter validates the
unaltered raw build executable and its byte-identical canonical-name copy.
No Unity, Git, build, test or player invocation.
One complete actual JSON job contributes 336 unique Passed cases; prior runs are
not added. Source/resource pins and gameplay/package acceptance remain separate.
"""
import collections
import hashlib
import importlib.util
from pathlib import Path
import types
import json
import sys

REPO = Path('/Users/steven/caves-of-ooo')
BASE_PATH = REPO/'Docs/Verification/Live16x24/deep-caves-package-audit.py'
BASE_SHA256 = 'fee94989ff27720ec8c3fcdd3270e7b870f11e13668f821c94f64b08f9c977b7'
SOURCE_COMMIT = 'fa553ca364a092a515e4837aca7859066c7a426d'
FINALIZED = True  # Published source and selected source/import bytes reviewed.
PINS = {'Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs': '00d00b43599ef7b521fd00648fec9506cba8ff85b88fee35e3ed13b6e72d3f9d',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedVineWallCatalog.cs': '46c5ec5c2aa7846fb34710c1c130a38dc1a8ba101d9f227cc96a505355436fa7',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedStorageCatalog.cs': 'c36b5f901509f9e6aedbcfbc30c265df273a6f354ba8f2733b25b3bd01cc6428',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSoddenFrogsCatalog.cs': 'e7b5ad266b103d6d16eecd411570391966d5e5f037449326c93799655e16bcf9',
 'Assets/Scripts/Presentation/Rendering/ZoneRenderer.FlatPixelPaint.cs': '4b987371355c345dba277386ccf76ebf98607cea01c057ca9cea3cfd0ae32583',
 'Assets/Scripts/Gameplay/Entities/DamageFlashPart.cs': 'b446503e976d8c39fe16e45afbae0a2a9d5c1d23e5a2e007019d855e2e804d69',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSillEnvelopeCatalog.cs': 'e39d3b4bdbb44d604ebe708ae52108369f6368180680fd04d09c3e39d20220fa',
 'Assets/Scripts/Presentation/Rendering/FlatPixelGrovelandsMaterialsCatalog.cs': '3e7e92852bf7ea8e5cbd3b215830f0d53e7ee27778730ea311e9bf7f97fa07a1',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGlobalPresentationTests.cs': 'f3ea58230878bf91bf93819b53a605032331dc6b5a8b752aa56f8c36a28e6d8e',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGrovelandsMaterialsTests.cs': 'b8575bfb9eafd9220b51bcddb2b1e2b66dc3d1d871a431927ca75c5f0f17bbc0',
 'Assets/Tests/EditMode/FlatPixelHollowLogTests.cs': '0458f73967520b9dc0903bb31d4b15118b32b9a804fabc9aef4c23e5446fc261',
 'Assets/Tests/EditMode/FlatPixelSharedCampPropsTests.cs': '7b8b465ad70a72c9e4579f42dfd54efefc128dae72d627f83bd42ed6df3cd5d2',
 'Assets/Tests/EditMode/FlatPixelSharedStorageTests.cs': 'f4c4ab5ecf881e4ac45a1232dadaec9540eb7f4dad7aacbb184f5a400e5ca499',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSharedVineWallTests.cs': 'c880265d51a16a1d1380efa23dee3c117c72248476fa0f173f1132cefa549d91',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSillEnvelopeTests.cs': 'b013f15cc121d5373a64162d2f3005d3448d946b3e2d216d60b3488e72ba4ba5',
 'Assets/Tests/EditMode/FlatPixelSoddenFrogsTests.cs': '356db5470d3722b2c68dcc686f1cfe765c97adaa2f98ba5c5b97a27c29c071cd',
 'Assets/Resources/Sprites/Live16x24/reedfrog.png': '6ad06ba141ad458f6c4beda1f1ea15a547e5f9e8d3e5fa730f1589b66a392355',
 'Assets/Resources/Sprites/Live16x24/bandfrog.png': '492a0628a74c1291100f6b807ee851396a61d23ab8c3f09e4793f2a72e6568e9',
 'Assets/Resources/Sprites/Environment/gin_frog.png': 'ffee061c71076206872aee9a4bea0e55bc8252fde63afce1857b7dec8cfe99eb',
 'Assets/Resources/Sprites/Live16x24/sill-envelope.png': 'bde729b96032b623093f430f8929bbb5e61a9f2649f85311aa3d6c36cfbe253f',
 'Assets/Resources/Sprites/Environment/cont_hollow_log.png': '0cb5fbd52a7fd91c0f5266254073f2a31b6ab5184a8201129558acf26a4e5783',
 'ArtSource/Live16x24/sodden-frogs/source-cells.json': 'def7ad45a123e7e3345252cc6d7d58aa5db93fa4668c756ac99cbd9519f74fd5',
 'ArtSource/Live16x24/sodden-frogs/palette.json': 'd6c2566d1da20c1583727d788df4b27fe70848ef879a2725b851520e9fb079ad',
 'ArtSource/Live16x24/sodden-frogs/editable.blend': '366661b90430b201762ae541f04b79e27ca05bf2ea45d9c8fdd74fc68873507f',
 'ArtSource/Live16x24/sodden-frogs/manifest.json': '17b2542a7e2880d7f7138e07ccdd8862aaec6755157b6c3e0cbf43ee40a73ea9',
 'Tools/Live16x24/build_sodden_frogs.py': 'c6f6d90b3ad72b0b8635603e433f181de6114748f3c6795cb1631afc3ec21eef',
 'Docs/Verification/Live16x24/shared-surface-native-green.json': '28d11add7160001c0a28d56b3e95472792a60abc70ac256d858e0a29842e9adf'}
NATIVE_NAME = 'shared-surface-native-green.json'
NATIVE_SHA256 = '28d11add7160001c0a28d56b3e95472792a60abc70ac256d858e0a29842e9adf'

REQUIRED_BINDINGS = ('Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedVineWallCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSharedStorageCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSoddenFrogsCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/ZoneRenderer.FlatPixelPaint.cs',
 'Assets/Scripts/Gameplay/Entities/DamageFlashPart.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelSillEnvelopeCatalog.cs',
 'Assets/Scripts/Presentation/Rendering/FlatPixelGrovelandsMaterialsCatalog.cs',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGlobalPresentationTests.cs',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGrovelandsMaterialsTests.cs',
 'Assets/Tests/EditMode/FlatPixelHollowLogTests.cs',
 'Assets/Tests/EditMode/FlatPixelSharedCampPropsTests.cs',
 'Assets/Tests/EditMode/FlatPixelSharedStorageTests.cs',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSharedVineWallTests.cs',
 'Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSillEnvelopeTests.cs',
 'Assets/Tests/EditMode/FlatPixelSoddenFrogsTests.cs',
 'Assets/Resources/Sprites/Live16x24/reedfrog.png',
 'Assets/Resources/Sprites/Live16x24/bandfrog.png',
 'Assets/Resources/Sprites/Environment/gin_frog.png',
 'Assets/Resources/Sprites/Live16x24/sill-envelope.png',
 'Assets/Resources/Sprites/Environment/cont_hollow_log.png',
 'ArtSource/Live16x24/sodden-frogs/source-cells.json',
 'ArtSource/Live16x24/sodden-frogs/palette.json',
 'ArtSource/Live16x24/sodden-frogs/editable.blend',
 'ArtSource/Live16x24/sodden-frogs/manifest.json',
 'Tools/Live16x24/build_sodden_frogs.py',
 'Docs/Verification/Live16x24/shared-surface-native-green.json')

MAIN_LITERALS = {
    'deep-caves-materials-shared-stone-steam':
        'shared-surface-frogs-vines-and-hollow-log',
    'packaged-deep-caves-linux':
        'packaged-shared-surface-linux',
    '\\d{4}-\\d{2}-\\d{2}-deep-caves-':
        '\\d{4}-\\d{2}-\\d{2}-shared-surface-',
    'Both fresh output folders must identify this deep-caves source and end in -current.':
        'Both fresh output folders must identify this shared-surface source and end in -current.',
    'Batched ordinary Quartzite depths 9–10 and Obsidian depths above 10 material coverage, shared neutral Pillar/BrokenColumn bodies and neutral natural SteamVent bodies. Twelve original opaque 16×24 floor/wall cells retain straight boundaries; approved stone-prop and vent bodies are borrowed unchanged. Guarded real-floor/backdrop handling, current graph/disclosure, owner/reader authority and existing damage/source mechanics remain required. Authored East SteamVent grille and independent East steam overlay remain separate; FrostVent is not covered. No generation, collision, saved-gameplay, global grading or camera change; isolated Shale study remains default-off.':
        'Two original neutral 16-by-24 Reedfrog and Bandfrog bodies plus unchanged approved 16-by-16 GinFrog reuse. Canonical surface VineWalls borrow the approved Sill connected atlas while Sill and authored Grovelands addresses retain their existing authority, including refusals. HollowLog borrows its approved full 16-by-16 container image. Exact owner, disclosure, custody, thermal, graph and optional-resource guards retain simulation and existing service meanings. A same-event native color cue preserves the real Bandfrog damage flash when its original glyph color already equals the flash color. No world generation, collision, saved-gameplay, camera or global grading change.',
    'Scope is exact eligible ordinary Quartzite/Obsidian materials plus conservative shared Pillar/BrokenColumn/SteamVent identities. Other room props, actors, hazards, authored areas and world families remain open. Actual Obsidian depths 11/12 samples do not certify every deeper address.':
        'Scope is three exact frog species, canonical eligible surface VineWalls outside already reserved Sill/Grovelands policies, and exact HollowLog storage identity. Underground/special VineWall coverage is not added. HollowLog has no invented structural health or heat state. Other flora, hazards, actors, equipment and world families remain open. Actual ordinary Editor and standalone observations have separate receipts.',
    'Material/body coverage and reader identity do not establish whole-scene fullness, reference parity or area polish. Repeated faces, dark internal facets, low-contrast fragments, sparse composition and mixed glyphs remain separate concerns. Actual journeys and persistence have separate records.':
        'Body coverage, connected wall selection and reader identity do not establish whole-scene fullness, reference parity or area polish. Mixed frog baselines, remaining Greatdew glyphs, broad sparse terrain and settlement composition remain separate work. Existing sprites and optional animation remain preserved.',
    'Three complete retained native Results contain ':
        'One complete retained native JSON job contains ',
    ' distinct Passed identities across 627 occurrences with 102 repeats: 419 stone plus 19/189 Steam. These are sequential checkpoints, not one final-source combined run. No earlier subset execution is added, no actual job results/times fabricated, and no performance claim follows.':
        ' distinct Passed identities in one 336-case checkpoint: 26 shared-vine, 14 HollowLog, 21 frog and 275 related cases. Earlier subset runs are not added. Actual start/finish timestamps come from the successful complete job envelope; no XML, test rerun, gameplay or performance certification is invented.',
}

LINUX_ROOT = REPO/'Builds/SteamDeck/2026-10-28-shared-surface-fa553ca36-current/CavesOfOoo-current'
LINUX_REPORT = REPO/'Docs/Verification/Live16x24/packaged-shared-surface-linux-build.json'
LINUX_REPORT_SHA256 = '880dff9060826cf2ee47a819f8eef2e39511ef083a3712b3b7c5857073c3f343'
LINUX_COPY_REPORT = REPO/'Docs/Verification/Live16x24/packaged-shared-surface-linux-executable-copy.json'
LINUX_COPY_REPORT_SHA256 = '7d5b98d38f1dd28208f02e1568dbe941a9d994d7f242f6cf2f3c727d4dfc0c57'
LINUX_EXECUTABLE_SHA256 = '8027f7d1f9ae7dacfc826fb218adcc1ae7464af098a8a59397e7531c0f7ec0bc'


def native_evidence(boundary):
    a=AUDITOR
    a.require(FINALIZED and isinstance(SOURCE_COMMIT,str) and len(SOURCE_COMMIT)==40
              and set(PINS)==set(REQUIRED_BINDINGS), 'Final source/art pins are not yet reviewed; packaging disabled.')
    for name,expected in PINS.items():
        a.require(a.digest(a.canonical(REPO/name))==expected, 'Frozen selected source/resource differs: '+name)
    path=a.canonical(a.EVIDENCE/NATIVE_NAME)
    a.require(a.digest(path)==NATIVE_SHA256, 'Native JSON differs from the reviewed complete receipt.')
    job=json.loads(path.read_text())
    a.require(isinstance(job,dict) and job.get('job_id')=='93f3a4257a444aa7b4a69fab0a7e48b0'
              and job.get('status')=='succeeded' and job.get('mode')=='EditMode'
              and job.get('error') is None, 'Expected the reviewed successful native job.')
    progress=job.get('progress',{}); result=job.get('result',{})
    a.require(isinstance(progress,dict) and progress.get('completed')==336 and progress.get('total')==336
              and progress.get('failures_so_far')==[] and progress.get('failures_capped') is False,
              'Native job progress is incomplete or reports failures.')
    a.require(isinstance(result,dict) and result.get('mode')=='EditMode', 'Missing complete EditMode Results.')
    summary=result.get('summary',{}); cases=result.get('results',[])
    a.require(isinstance(summary,dict) and summary.get('resultState')=='Passed'
              and all(summary.get(k)==v for k,v in {'total':336,'passed':336,'failed':0,'skipped':0}.items())
              and summary.get('inconclusive',0)==0, 'Expected one all-Passed 336-case native run.')
    a.require(isinstance(cases,list) and len(cases)==336
              and all(isinstance(row,dict) and isinstance(row.get('fullName'),str)
                      and row['fullName'] and row.get('state')=='Passed' for row in cases),
              'Missing or non-Passed native case.')
    names=[row['fullName'] for row in cases]
    a.require(len(set(names))==336, 'Duplicate native identity.')
    expected={'CavesOfOoo.Tests.FlatPixelGlobalPresentationTests': 52,
 'CavesOfOoo.Tests.FlatPixelGrovelandsMaterialsTests': 103,
 'CavesOfOoo.Tests.FlatPixelHollowLogTests': 14,
 'CavesOfOoo.Tests.FlatPixelSharedCampPropsTests': 26,
 'CavesOfOoo.Tests.FlatPixelSharedStorageTests': 43,
 'CavesOfOoo.Tests.FlatPixelSharedVineWallTests': 26,
 'CavesOfOoo.Tests.FlatPixelSillEnvelopeTests': 51,
 'CavesOfOoo.Tests.FlatPixelSoddenFrogsTests': 21}
    a.require(dict(collections.Counter(n.split('(',1)[0].rsplit('.',1)[0] for n in names))==expected,
              'Native fixture scope differs from the reviewed run.')
    start_ms=job.get('started_unix_ms'); end_ms=job.get('finished_unix_ms')
    a.require(type(start_ms) is int and type(end_ms) is int and 0<start_ms<=end_ms,
              'Native job must contain actual positive start and finish timestamps.')
    start=a.dt.datetime.fromtimestamp(start_ms/1000,a.dt.timezone.utc)
    end=a.dt.datetime.fromtimestamp(end_ms/1000,a.dt.timezone.utc)
    a.require(start<=end<=boundary, 'Native job must precede root source/build boundary.')
    return {'receipts':[{'path':str(path),'sha256':NATIVE_SHA256,'summary':summary,
                        'actual_case_rows':336,'mode':'EditMode','job_id':job['job_id'],
                        'started_unix_ms':start_ms,'finished_unix_ms':end_ms,
                        'started_at_utc':start.isoformat(),'finished_at_utc':end.isoformat(),
                        'timestamp_source':'Actual complete native JSON job envelope; no inferred XML or times.'}],
            'current_source_binding':{'source_commit':SOURCE_COMMIT,'pins':PINS,
                'current_source_evidence_resource_bytes_match':True,
                'limits':'Selected current source, resource and fixture bytes are pinned separately. '
                         'Root asserts source/build provenance; actual gameplay and package acceptance have separate evidence.'},
            'execution_occurrences':336,'passed_execution_occurrences':336,
            'failed_execution_occurrences':0,'skipped_execution_occurrences':0,'inconclusive_execution_occurrences':0,
            'repeated_identity_occurrences':0,'distinct_test_identities_across_listed_receipts':336,
            'distinct_passed_test_identities':336,'distinct_skipped_test_identities':0,
            'distinct_inconclusive_test_identities':0,'all_cases_passed':True,
            'shared_vines_passed':26,'hollow_log_passed':14,'sodden_frogs_passed':21,'other_related_passed':275,
            'native_job_times_verified':True,'native_xml_times_verified':False,
            'skipped_identities':[],'inconclusive_identities':[],
            'limits':'One complete 336-case JSON job only; earlier subset executions are not added. '
                     'No test rerun, gameplay, Linux/Deck execution or performance certification is invented.'}


def configure_auditor():
    if hashlib.sha256(BASE_PATH.read_bytes()).hexdigest()!=BASE_SHA256:
        raise RuntimeError('Existing audited base changed; re-review before using this wrapper.')
    spec=importlib.util.spec_from_file_location('shared_surface_audited_base', BASE_PATH)
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
    original_build_receipt=a.build_receipt
    def build_receipt(path, platform, expected_output, source, boundary, now):
        if platform!='StandaloneLinux64':
            return original_build_receipt(path, platform, expected_output, source, boundary, now)
        # Unity actually built the unextended executable. Preserve that receipt;
        # require the explicitly recorded identical copy used by the launcher.
        a.require(path==LINUX_REPORT and a.digest(a.canonical(path))==LINUX_REPORT_SHA256
                  and expected_output==LINUX_ROOT/'CavesOfOoo.x86_64',
                  'Use this exact raw Linux receipt and canonical copied executable.')
        raw=a.canonical(LINUX_ROOT/'CavesOfOoo')
        canonical_exe=a.canonical(expected_output)
        report_path=a.canonical(LINUX_COPY_REPORT)
        a.require(a.digest(report_path)==LINUX_COPY_REPORT_SHA256,
                  'Linux executable-copy provenance differs from reviewed bytes.')
        copy=json.loads(report_path.read_text())
        a.require(copy.get('raw_build_executable')==str(raw.relative_to(REPO))
                  and copy.get('canonical_archive_executable')==str(canonical_exe.relative_to(REPO))
                  and copy.get('action')=='copy2; raw build executable preserved'
                  and copy.get('bytes')==4472 and copy.get('both_sha256')==LINUX_EXECUTABLE_SHA256
                  and copy.get('mode')=='0o755', 'Unexpected executable-copy provenance.')
        for executable in (raw,canonical_exe):
            a.require(executable.stat().st_size==4472
                      and (executable.stat().st_mode & 0o7777)==0o755
                      and a.digest(executable)==LINUX_EXECUTABLE_SHA256,
                      'Original and canonical Linux executables must retain reviewed bytes and modes.')
        receipt=original_build_receipt(path, platform, raw, source, boundary, now)
        a.require(receipt['summary']['job_id']=='build-ada86ac4fb', 'Unexpected Linux build job.')
        copied_at=a.timestamp(copy['at'])
        a.require(a.timestamp(receipt['summary']['completed_at'])<=copied_at<=now+a.dt.timedelta(minutes=2),
                  'Executable copy must follow the actual build.')
        receipt['canonical_executable_copy']={
            'path':str(report_path),'sha256':LINUX_COPY_REPORT_SHA256,'provenance':copy,
            'both_current_files_hash_size_mode_verified':True,
            'limits':'Raw build receipt and unextended executable remain unchanged. '
                     'The launcher uses the identical canonical-name copy. No Linux execution is claimed.'}
        return receipt
    a.build_receipt=build_receipt
    return a


AUDITOR=configure_auditor()
if __name__=='__main__':
    AUDITOR.require(FINALIZED, 'Final source/art pins pending; this prepared wrapper cannot package yet.')
    required = {
        '--source-commit': SOURCE_COMMIT,
        '--built-after': '2026-10-28T10:29:01.737362+00:00',
        '--linux-output-dir': str(REPO/'Builds/SteamDeck/2026-10-28-shared-surface-fa553ca36-current/CavesOfOoo-current'),
        '--mac-app': str(REPO/'Builds/Verification/2026-10-28-shared-surface-fa553ca36-current/CavesOfOoo.app'),
        '--linux-build-report': str(REPO/'Docs/Verification/Live16x24/packaged-shared-surface-linux-build.json'),
        '--mac-build-report': str(REPO/'Docs/Verification/Live16x24/packaged-shared-surface-mac-build.json'),
        '--receipt-dir': str(REPO/'Docs/Verification/Live16x24'),
    }
    for flag, value in required.items():
        if sys.argv.count(flag)!=1 or sys.argv.index(flag)+1>=len(sys.argv) or sys.argv[sys.argv.index(flag)+1]!=value:
            raise RuntimeError('Use the exact reviewed '+flag+' value for this delivery.')
    if '--receipt-prefix' in sys.argv:
        if sys.argv.count('--receipt-prefix')!=1 or sys.argv.index('--receipt-prefix')+1>=len(sys.argv) or sys.argv[sys.argv.index('--receipt-prefix')+1]!='packaged-shared-surface-linux':
            raise RuntimeError('Use the fixed packaged-shared-surface-linux receipt prefix.')
    AUDITOR.main()
