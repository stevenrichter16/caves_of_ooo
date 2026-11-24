from pathlib import Path
import json,gzip,hashlib,datetime,shutil,plistlib
r=Path('/Users/steven/caves-of-ooo');b=Path('/private/tmp/coo-mac-152b2cdcf-20261124-export');d=r/'Docs/Verification/MachineSystems/MacCandidate152B2CDCF';native=Path('/private/tmp/coo-mac-152b2cdcf-20261124-native');sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
for p in native.iterdir():
 if p.is_file():
  if p.suffix=='.log':(d/'Native'/(p.name+'.gz')).write_bytes(gzip.compress(p.read_bytes(),mtime=0))
  else:shutil.copy2(p,d/'Native'/p.name)
log=(native/'player.log').read_text();assert 'DONE. Zone has 2669 entities' in log and 'Registered 7 creatures' in log
bad=[s for s in log.splitlines() if any(x in s for x in ['NullReferenceException','Unhandled Exception','FATAL','error CS','MissingMethodException'])];assert not bad,bad
before=plistlib.loads((native/'prefs-before.plist').read_bytes());rest=json.loads((native/'restoration.json').read_text());assert rest['player_stopped'] and rest['preferences_logically_restored']
prior=json.loads((b/'prior-apps-before.json').read_text());apps={}
for name,old in prior.items():
 p=Path(name);current={str(f.relative_to(p)):sha(f) for f in p.rglob('*') if f.is_file()};assert current==old;apps[name]=current
app=Path(json.loads((b/'preparation.json').read_text())['output'])/'CavesOfOoo.app';current={str(f.relative_to(app)):sha(f) for f in app.rglob('*') if f.is_file()};assert current==json.loads((b/'player-pins.json').read_text());apps[str(app)]=current;assert len(apps)==26
(d/'all-apps-post-boot.json.gz').write_bytes(gzip.compress((json.dumps(apps,indent=2)+'\n').encode(),mtime=0))
foreign=json.loads(Path('/private/tmp/coo-shared-source-foreign-baseline.json').read_text());assert all(sha(r/p)==s for p,s in foreign.items())
normal=json.loads(Path('/private/tmp/coo-machines-normal-state-baseline.json').read_text());normal={p:s for p,s in normal.items() if '/Unity/' not in p and not p.endswith('/TestResults.xml')};assert len(normal)==861;assert all(sha(Path(p))==s for p,s in normal.items())
other={'Docs/MACHINE-PROGRAM-HANDOFF.md':'aa2c8cb7eb478c2486c220dd1cea091f4eadc19094122e7799ef55b9ff57405c','ProjectSettings/PackageManagerSettings.asset':'fe29386343e3809224bda909982c5807a92f3222fd1db8dec96b8b3f61fffeae'};assert all(sha(r/p)==s for p,s in other.items())
sroot=Path.home()/'Library/Application Support/com.DefaultCompany.2D-URP/Saves';sp=json.loads((native/'normal-saves-before.json').read_text());assert {str(f.relative_to(sroot)):sha(f) for f in sroot.rglob('*') if f.is_file()}==sp
out={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'allApps':len(apps),'allAppFiles':sum(map(len,apps.values())),'priorAppsExact':len(prior),'priorAppFilesExact':sum(map(len,prior.values())),'newPlayerFilesExact':len(current),'foreignTrackedExact':len(foreign),'normalSelectedExact':len(normal),'normalSelection':'excludes Unity service bookkeeping/TestResults.xml; no wholefilesystem or plist byte equality claim','otherForeignExact':2,'normalMacSaveFilesExact':len(sp),'logicalPreferencesRestored':len(before),'knownFatalLogLines':bad,'ownedPlayerClosed':True,'allScopedChecksExact':True}
(d/'preservation.json').write_text(json.dumps(out,indent=2)+'\n');shutil.copy2(__file__,d/'verify-preservation.py');print(json.dumps(out))
