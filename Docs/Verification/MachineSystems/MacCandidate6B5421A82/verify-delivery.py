from pathlib import Path
import datetime, hashlib, json, plistlib, subprocess, tarfile

bundle = Path('/private/tmp/coo-mac-6b5421a82-20261124-export')
repo = Path('/Users/steven/caves-of-ooo')
native = Path('/private/tmp/coo-mac-6b5421a82-20261124-native')
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
read = lambda p: json.loads(p.read_text())
write = lambda p, x: p.write_text(json.dumps(x, indent=2) + '\n')

deltas = read(bundle / 'input-deltas.json')
effective = read(bundle / 'post-build-input-pins.json')
expected = set(deltas['changed']) | set(deltas['added'])
seen = set()
with tarfile.open(bundle / 'effective-input-deltas.tar.gz') as archive:
    for member in archive:
        if not member.isfile():
            continue
        assert member.name not in seen
        data = archive.extractfile(member).read()
        assert hashlib.sha256(data).hexdigest() == effective[member.name]
        seen.add(member.name)
assert seen == expected and len(seen) == 86
write(bundle / 'overlay-proof.json', {'members': len(seen), 'exact': True,
      'scope': 'Every changed or added effective-input body matches the post-build pin.'})

log = (native / 'player.log').read_text()
required = ['[Bootstrap] Loaded 7 starting build(s); new-game picker ON.',
            'GameBootstrap: Registered 7 creatures for turns',
            '[Bootstrap] ZoneRenderer wired successfully']
assert all(line in log for line in required)
import re
generated = int(re.search(r'\[Bootstrap\] Zone generated: (\d+) entities', log)[1])
ready = int(re.search(r'\[Bootstrap\] DONE\. Zone has (\d+) entities', log)[1])
assert ready == generated + 1
failures = [line for line in log.splitlines() if any(s in line for s in
            ['Exception:', 'NullReferenceException', 'Failed to', 'ERROR', 'Error:'])]
assert not failures, failures
process = read(native/'process.json'); observation = read(bundle/'window-observation.json')
assert observation['pid'] == process['pid']
windows = [w for w in observation['windows'] if w['bounds'].get('Height',0)>100 and w['bounds'].get('Width',0)>100]
assert len(windows)==1
write(bundle / 'startup-result.json', {'ownedPID': process['pid'], 'bootstrapLogComplete': True,
      'generatedEntities': generated, 'readyEntities': ready, 'registeredCreatures': 7,
      'knownFailureLines': failures, 'ownedWindow': windows[0],
      'imageCaptured': False, 'nativeInputAccess': observation['nativeInputAccess'],
      'accessibilityTrusted': observation['accessibilityTrusted'],
      'scope': 'Owned read-only startup log plus native window observation only. No additional screenshot, rendered-image judgment, input, Continue, packaged action or save trial.'})

apps = read(bundle / 'prior-apps-before.json')
prior_count = len(apps)
prior_files = sum(map(len, apps.values()))
candidate = read(bundle / 'build-report.json')['output']
assert candidate not in apps
apps[candidate] = read(bundle / 'player-pins.json')
for app, pins in apps.items():
    root = Path(app)
    current = {str(p.relative_to(root)): sha(p) for p in sorted(root.rglob('*')) if p.is_file()}
    assert current == pins, app
write(bundle / 'all-apps-post-boot.json', apps)

foreign = read(Path('/private/tmp/coo-shared-source-foreign-baseline.json'))
assert all(sha(repo / p) == pin for p, pin in foreign.items())
normal = read(Path('/private/tmp/coo-machines-normal-state-baseline.json'))
selected = {p: pin for p, pin in normal.items() if '/Unity/' not in p and not p.endswith('/TestResults.xml')}
assert len(selected) == 861 and all(sha(Path(p)) == pin for p, pin in selected.items())
other = {'Docs/MACHINE-PROGRAM-HANDOFF.md': 'aa2c8cb7eb478c2486c220dd1cea091f4eadc19094122e7799ef55b9ff57405c',
         'ProjectSettings/PackageManagerSettings.asset': 'fe29386343e3809224bda909982c5807a92f3222fd1db8dec96b8b3f61fffeae'}
assert all(sha(repo / p) == pin for p, pin in other.items())
saves = Path.home() / 'Library/Application Support/com.DefaultCompany.2D-URP/Saves'
save_pins = read(native / 'normal-saves-before.json')
assert {str(p.relative_to(saves)): sha(p) for p in saves.rglob('*') if p.is_file()} == save_pins
before = plistlib.loads((native / 'prefs-before.plist').read_bytes())
restored = plistlib.loads(subprocess.check_output(['defaults', 'export', 'com.DefaultCompany.2D-URP', '-']))
assert restored == before
assert subprocess.run(['pgrep', '-x', 'caves-of-ooo'], capture_output=True).returncode == 1
result = {'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
          'priorApps': prior_count, 'priorAppFiles': prior_files, 'newAppFiles': len(apps[candidate]),
          'allAppMaps': len(apps), 'allAppFiles': sum(map(len, apps.values())), 'allAppFilesExact': True,
          'foreignTrackedPins': len(foreign), 'selectedNormalPins': len(selected),
          'otherForeignTracked': other, 'otherForeignExact': True,
          'macNormalSaveFiles': len(save_pins), 'macNormalSavesExact': True,
          'macLogicalPreferences': len(before), 'macPreferencesLogicallyRestored': True,
          'playersStopped': True,
          'scope': 'Fresh selected apps, normal and foreign maps exact; Unity operational paths/TestResults excluded. No full-directory or byte-identical preferences claim.'}
write(bundle / 'preservation.json', result)
print(json.dumps(result))
