from pathlib import Path
import plistlib,subprocess,json,datetime,hashlib
c=Path('/private/tmp/coo-quiet-door-baseline-protection');e=Path('/Users/steven/caves-of-ooo/Docs/Verification/GraphicsExecution/QuietDoorBaseline');d='com.DefaultCompany.2D-URP'
assert subprocess.run(['pgrep','-x','caves-of-ooo'],capture_output=True).returncode==1
old=plistlib.loads((c/(d+'.plist')).read_bytes());new=plistlib.loads(subprocess.check_output(['defaults','export',d,'-']));changes={k for k in set(old)|set(new) if old.get(k)!=new.get(k)}
allowed={'CavesOfOoo.LastGameID','Screenmanager Fullscreen mode','Screenmanager Resolution Height','Screenmanager Resolution Use Native','Screenmanager Resolution Width','unity.player_session_count','unity.player_sessionid','unity_connect.mega_session_id','unity_connect.session_id'};assert changes<=allowed,changes-allowed
editor='unity.DefaultCompany.caves-of-ooo';assert plistlib.loads((c/(editor+'.plist')).read_bytes())==plistlib.loads(subprocess.check_output(['defaults','export',editor,'-']))
s=Path.home()/'Library/Application Support/com.DefaultCompany.2D-URP/Saves';assert sorted(p.name for p in s.iterdir())==['b343f331bf5d40a189c86e4aaba9aa47'];assert not (c/'owned-session-Saves').exists()
(c/'player-after.plist').write_bytes(plistlib.dumps(new,sort_keys=True));s.rename(c/'owned-session-Saves')
subprocess.run(['defaults','import',d,str(c/(d+'.plist'))],check=True,capture_output=True)
now=plistlib.loads(subprocess.check_output(['defaults','export',d,'-']))
for k in set(now)-set(old):
 assert k in changes;subprocess.run(['defaults','delete',d,k],check=True,capture_output=True)
assert plistlib.loads(subprocess.check_output(['defaults','export',d,'-']))==old;assert not s.exists()
b=json.load(open('/private/tmp/coo-sumphold-composition-preservation.json'));sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest();assert all(sha(p)==h for p,h in b['dirty'].items());normal=[p for p,h in b['normal'].items() if not Path(p).exists() or sha(p)!=h];assert all(p.endswith(('TestResults.xml','session_end_event')) for p in normal)
r={'recordedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'foreignTrackedExact':80,'normalGameplayFilesExact':True,'expectedDiagnosticChanges':normal,'standalonePreferencesRestoredExactLogical':True,'standaloneSavesRestoredAbsent':True,'editorPreferencesUnchangedDuringSurvey':True,'testSavesArchived':str(c/'owned-session-Saves'),'changedPlayerPreferenceKeysRestored':sorted(changes),'playersClosed':True,'cleanup':'none','artChanges':'none','additionalStartupChangesPreserved':{p:sha(p) for p in ['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectSettings.asset']},'startupChangesNote':'Three previously clean files changed when Unity restarted around13:31–13:32Z; versions1.1.0→1.1.1 and Standalone batching default entry. Preserved/unstaged; no attribution beyond timing, no graphics acceptance.'}
(e/'restoration.json').write_text(json.dumps(r,indent=2)+'\n');print({'normalRestored':True,'foreignExact':80,'additionalStartupChangesPreserved':3,'playersClosed':True})
