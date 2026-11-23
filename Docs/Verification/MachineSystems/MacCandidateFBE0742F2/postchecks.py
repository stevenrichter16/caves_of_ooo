from pathlib import Path
import json,hashlib,datetime,subprocess,plistlib
b=Path('/private/tmp/coo-mac-fbe0742f2-20261122-export');n=Path('/private/tmp/coo-mac-fbe0742f2-20261122-native')
app=Path('/Users/steven/coo-verification/builds/mac-fbe0742f2-20261122/CavesOfOoo.app');old=Path('/Users/steven/coo-verification/builds/mac-a9de3d3d9-20261122/CavesOfOoo.app');sha=lambda raw:hashlib.sha256(raw).hexdigest()
rel='Contents/Resources/Data/Managed/CavesOfOoo.dll';raw=(app/rel).read_bytes();prior=(old/rel).read_bytes();assert raw!=prior
api=['get_OverviewText','TryDescribeEquipmentSections','BuildInventorySnapshot'];labels=['Armor value (AV): ','Dodge value (DV): '];assert all(x.encode() in raw and x.encode() not in prior for x in api);assert all(x.encode('utf-16le') in raw and x.encode('utf-16le') not in prior for x in labels)
(b/'assembly-proof.json').write_text(json.dumps({'assembly':rel,'sha256':sha(raw),'previousA9SHA256':sha(prior),'different':True,'newAPIs':api,'newLabels':labels,'previousHasAPIsOrLabels':False},indent=2)+'\n')
current=json.loads((b/'build-report.json').read_text());previous=json.loads(Path('/Users/steven/caves-of-ooo/Docs/Verification/MachineSystems/MacCandidateA9DE3D3D9/build-report.json').read_text());warn=lambda r:[m['content'] for m in r['messages'] if m['type']=='Warning'];assert warn(current)==warn(previous)
(b/'warnings-comparison.json').write_text(json.dumps({'sameWarningsAsA9':True,'count':len(warn(current))},indent=2)+'\n')
apps=json.loads(Path('/private/tmp/coo-mac-a9de3d3d9-20261122-export/prior-apps-preserved.json').read_text());apps['a9de3d3d9']={'app':str(old),'pins':'/private/tmp/coo-mac-a9de3d3d9-20261122-export/player-pins.json'}
for name,entry in apps.items():
 root=Path(entry['app']);expected=json.loads(Path(entry['pins']).read_text());actual={str(p.relative_to(root)):sha(p.read_bytes()) for p in root.rglob('*') if p.is_file()};assert actual==expected,name;entry.update(files=len(actual),fullInventoryAndHashesEqual=True)
(b/'prior-apps-preserved.json').write_text(json.dumps(apps,indent=2)+'\n')
repo=Path('/Users/steven/caves-of-ooo');foreign=json.loads(Path('/private/tmp/coo-shared-source-foreign-baseline.json').read_text());normal=json.loads(Path('/private/tmp/coo-machines-normal-state-baseline.json').read_text());normal={f:h for f,h in normal.items() if '/Unity/' not in f and not f.endswith('/TestResults.xml')}
fd=[f for f,h in foreign.items() if not(repo/f).is_file() or sha((repo/f).read_bytes())!=h];nd=[f for f,h in normal.items() if not Path(f).is_file() or sha(Path(f).read_bytes())!=h];assert not fd and not nd
(b/'preservation.json').write_text(json.dumps({'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'foreignCount':len(foreign),'foreignDifferences':fd,'normalSelectedCount':len(normal),'normalDifferences':nd,'selectedOnly':True,'analyticsAndTestResultsExcluded':True},indent=2)+'\n')
before=plistlib.loads((n/'prefs-before.plist').read_bytes());now=plistlib.loads(subprocess.check_output(['defaults','export','com.DefaultCompany.2D-URP','-']));saves=Path.home()/'Library/Application Support/com.DefaultCompany.2D-URP/Saves';assert before==now and not saves.exists() and not saves.is_symlink()
(n/'fresh-preferences-saves-check.json').write_text(json.dumps({'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'logicalPreferenceKeys':len(now),'allValuesEqual':True,'normalSavesAbsent':True,'normalSavesNonSymlink':True},indent=2)+'\n')
print(json.dumps({'priorApps':len(apps),'assembly':sha(raw),'warnings':len(warn(current)),'foreign':len(foreign),'normalSelected':len(normal),'logicalPreferences':len(now)}))
