#!/usr/bin/env python3
"""Root-only smoke launcher for this built source. Preserves each owned test-save tree.
Normal standalone saves were absent in the guarded prebuild snapshot. Never use
against a different source or while a player is running. Native UI owns all play.
"""
from pathlib import Path
import subprocess,shutil,json,datetime,hashlib,sys
root=Path('/Users/steven/caves-of-ooo');e=root/'Docs/Verification/GraphicsExecution/Batch88c7';saves=Path.home()/'Library/Application Support/com.DefaultCompany.2D-URP/Saves';n=int(sys.argv[1]);fixture=sys.argv[2]
assert subprocess.run(['pgrep','-x','caves-of-ooo'],capture_output=True).returncode==1
source='88c7fd521d12098ac8558928bc7656f71fde0a85';assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==source
before=json.loads(Path('/private/tmp/coo-graphics-build-protection-88c7fd52/before.json').read_text());assert before['normal_saves']['normal-standalone']['before']['root']['type']=='absent'
log=e/f'mac-run{n}.log';receipt=e/f'mac-process{n}.json';stdout=Path(f'/private/tmp/coo-88c7-standalone{n}.log')
for output in (log,receipt,stdout):
 assert not output.exists() and not output.is_symlink(), 'Refusing to overwrite prior launch evidence: '+str(output)
archived=None
if fixture!='continue':
 src=root/fixture;assert src.is_dir() and src.is_relative_to(root/'Docs/Verification/GraphicsExecution');info=json.loads((src/'Quick.json').read_text())
 if saves.exists():
  archived=Path('/private/tmp')/f'coo-88c7-owned-session-before{n}';assert not archived.exists();saves.rename(archived)
 target=saves/info['GameID'];target.mkdir(parents=True)
 for name in ['Quick.json','Quick.sav.gz']:shutil.copy2(src/name,target/name)
else:
 assert saves.is_dir();src=next(saves.glob('*/Quick.json')).parent;info=json.loads((src/'Quick.json').read_text())
exe=root/'Builds/Verification/2026-11-02-graphics-88c7fd521-current/CavesOfOoo.app/Contents/MacOS/caves-of-ooo'
with stdout.open('xb') as stream:
 p=subprocess.Popen([str(exe),'-logFile',str(log)],stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
r={'pid':p.pid,'at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'source':source,'input_fixture':str(src),'fixture_sha256':{name:hashlib.sha256((src/name).read_bytes()).hexdigest() for name in ['Quick.json','Quick.sav.gz']},'exe':str(exe),'log':str(log),'previous_owned_session_archive':str(archived) if archived else None,'normal_save_root_preexisting':False,'resolution':'ordinary default; no forced screen arguments'}
with receipt.open('x') as stream: stream.write(json.dumps(r,indent=2)+'\n')
print('Player',p.pid,'zone',info['ActiveZoneID'],info['HPSummary'])
