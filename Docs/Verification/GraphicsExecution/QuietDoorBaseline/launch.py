from pathlib import Path
import subprocess,shutil,json,datetime,hashlib,plistlib
root=Path('/Users/steven/caves-of-ooo');e=root/'Docs/Verification/GraphicsExecution/QuietDoorBaseline';e.mkdir(exist_ok=True)
control=Path('/private/tmp/coo-quiet-door-baseline-protection');assert not control.exists();control.mkdir()
saves=Path.home()/'Library/Application Support/com.DefaultCompany.2D-URP/Saves';assert not saves.exists()
assert subprocess.run(['pgrep','-x','caves-of-ooo'],capture_output=True).returncode==1
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()=='1b286ec324b72cebe4c33aaa141b8982ded37261'
for d in ['com.DefaultCompany.2D-URP','unity.DefaultCompany.caves-of-ooo']:
 (control/(d+'.plist')).write_bytes(subprocess.check_output(['defaults','export',d,'-']))
source='59a4af34b077f1f987f62cbee38db81479014322'
exe=root/'Builds/Verification/2026-11-15-graphics-59a4af34b-current/CavesOfOoo.app/Contents/MacOS/caves-of-ooo';assert exe.is_file()
src=root/'Docs/Verification/GraphicsExecution/DrownedLedgerCourts/AfterCheckpoint';info=json.loads((src/'Quick.json').read_text());assert info['Turn']==5540 and info['ActiveZoneID']=='Overworld.17.5.0'
r={'recordedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'buildSource':source,'checkoutSource':'1b286ec324b72cebe4c33aaa141b8982ded37261','limits':'Existing verified Mac build excludes only later Salt-Vault grade. Actual baseline survey, not current-source acceptance. No injected state or native Editor test claim.','normalStandaloneSavesBefore':'absent','fixture':str(src),'fixtureHashes':{n:hashlib.sha256((src/n).read_bytes()).hexdigest() for n in ['Quick.json','Quick.sav.gz']},'executableSHA256':hashlib.sha256(exe.read_bytes()).hexdigest(),'control':str(control),'requestedResolution':'1280x800 windowed'}
(control/'before.json').write_text(json.dumps(r,indent=2)+'\n')
target=saves/info['GameID'];target.mkdir(parents=True)
for n in ['Quick.json','Quick.sav.gz']:shutil.copy2(src/n,target/n)
log=e/'mac-baseline.log';assert not log.exists()
with (control/'stdout.log').open('xb') as out:
 p=subprocess.Popen([str(exe),'-screen-width','1280','-screen-height','800','-screen-fullscreen','0','-logFile',str(log)],stdout=out,stderr=subprocess.STDOUT,start_new_session=True)
r['pid']=p.pid;(e/'launch.json').write_text(json.dumps(r,indent=2)+'\n')
print({'pid':p.pid,'build':source[:9],'fixtureTurn':5540,'normalSaves':'absent before; isolated earned copy only'})
