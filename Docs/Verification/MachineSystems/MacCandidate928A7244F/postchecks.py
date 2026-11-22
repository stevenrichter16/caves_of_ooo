from pathlib import Path
import json,hashlib,datetime,subprocess,plistlib
b=Path('/private/tmp/coo-mac-928a7244f-20261122-export');n=Path('/private/tmp/coo-mac-928a7244f-20261122-native')
app=Path('/Users/steven/coo-verification/builds/mac-928a7244f-20261122/CavesOfOoo.app')
old=Path('/Users/steven/coo-verification/builds/mac-45170b1b6-20261122/CavesOfOoo.app')
rel='Contents/Resources/Data/Managed/CavesOfOoo.dll';raw=(app/rel).read_bytes();prior=(old/rel).read_bytes()
sha=lambda raw:hashlib.sha256(raw).hexdigest()
assert raw!=prior and b'OwnsBakeReceipt' in raw and b'InventoryUnitOutputReceipt' in raw
assert b'OwnsBakeReceipt' not in prior and b'InventoryUnitOutputReceipt' not in prior
(b/'assembly-proof.json').write_text(json.dumps({'assembly':rel,'sha256':sha(raw),'previous451_sha256':sha(prior),'different':True,'bakeReceiptMetadata':True,'unitOutputReceiptMetadata':True,'previousHasNewMetadata':False},indent=2)+'\n')
new=json.loads((b/'build-report.json').read_text());prev=json.loads(Path('/private/tmp/coo-mac-45170b1b6-20261122-export/build-report.json').read_text())
warnings=lambda x:[m['content'] for m in x['messages'] if m['type']=='Warning']
assert warnings(new)==warnings(prev);(b/'warnings-comparison.json').write_text(json.dumps({'sameWarningsAs451':True,'count':len(warnings(new))},indent=2)+'\n')
apps=json.loads(Path('/private/tmp/coo-mac-45170b1b6-20261122-export/prior-apps-preserved.json').read_text())
apps['45170b1b6']={'app':str(old),'pins':'/private/tmp/coo-mac-45170b1b6-20261122-export/player-pins.json'}
for k,v in apps.items():
 root=Path(v['app']);want=json.loads(Path(v['pins']).read_text());got={str(p.relative_to(root)):sha(p.read_bytes()) for p in root.rglob('*') if p.is_file()}
 assert want==got,k
 v.update(files=len(got),fullInventoryAndHashesEqual=True)
(b/'prior-apps-preserved.json').write_text(json.dumps(apps,indent=2)+'\n')
repo=Path('/Users/steven/caves-of-ooo');foreign=json.loads(Path('/private/tmp/coo-shared-source-foreign-baseline.json').read_text());normal=json.loads(Path('/private/tmp/coo-machines-normal-state-baseline.json').read_text());normal={k:v for k,v in normal.items() if '/Unity/' not in k and not k.endswith('/TestResults.xml')}
diff=lambda entries,root:[k for k,v in entries.items() if not(root/k).is_file() or sha((root/k).read_bytes())!=v]
fd=diff(foreign,repo);nd=diff(normal,Path('/'));assert not fd and not nd
(b/'preservation.json').write_text(json.dumps({'checkedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'foreignCount':len(foreign),'foreignDifferences':fd,'normalSelectedCount':len(normal),'normalDifferences':nd,'selectedOnly':True,'analyticsAndTestResultsExcluded':True},indent=2)+'\n')
if (n/'restoration.json').exists():
 before=plistlib.loads((n/'prefs-before.plist').read_bytes());now=plistlib.loads(subprocess.check_output(['defaults','export','com.DefaultCompany.2D-URP','-']))
 saves=Path.home()/'Library/Application Support/com.DefaultCompany.2D-URP/Saves'
 assert before==now and not saves.exists() and not saves.is_symlink()
 (n/'fresh-preferences-saves-check.json').write_text(json.dumps({'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'logicalPreferenceKeys':len(now),'allValuesEqual':True,'normalSavesAbsent':True,'normalSavesNonSymlink':True},indent=2)+'\n')
print(json.dumps({'priorApps':len(apps),'assembly':sha(raw),'warnings':len(warnings(new)),'foreign':len(foreign),'normal':len(normal)}))
