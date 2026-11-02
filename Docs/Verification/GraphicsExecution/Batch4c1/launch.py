#!/usr/bin/env python3
"""Root-only smoke launcher for this built source. Preserves each owned test-save tree.
Normal standalone saves were absent in the guarded prebuild snapshot. Never use
against a different source or while a player is running. Native UI owns all play.
"""
from pathlib import Path
import subprocess,shutil,json,datetime,hashlib,sys
root=Path('/Users/steven/caves-of-ooo');e=root/'Docs/Verification/GraphicsExecution/Batch4c1';saves=Path.home()/'Library/Application Support/com.DefaultCompany.2D-URP/Saves';n=int(sys.argv[1]);fixture=sys.argv[2]
assert subprocess.run(['pgrep','-x','caves-of-ooo'],capture_output=True).returncode==1
source='4c1da7fcd73a47a1d01006a2e1200423dabfac1e';assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==source
before=json.loads(Path('/private/tmp/coo-graphics-build-protection-4c1da7fcd/before.json').read_text());assert before['normal_saves']['normal-standalone']['before']['root']['type']=='absent'
archived=None
if fixture!='continue':
 src=root/fixture;assert src.is_dir() and src.is_relative_to(root/'Docs/Verification/GraphicsExecution');info=json.loads((src/'Quick.json').read_text())
 if saves.exists():
  archived=Path('/private/tmp')/f'coo-4c1-owned-session-before{n}';assert not archived.exists();saves.rename(archived)
 target=saves/info['GameID'];target.mkdir(parents=True)
 for name in ['Quick.json','Quick.sav.gz']:shutil.copy2(src/name,target/name)
else:
 assert saves.is_dir();src=next(saves.glob('*/Quick.json')).parent;info=json.loads((src/'Quick.json').read_text())
exe=root/'Builds/Verification/2026-11-02-graphics-4c1da7fcd-current/CavesOfOoo.app/Contents/MacOS/caves-of-ooo';log=e/f'mac-run{n}.log'
p=subprocess.Popen([str(exe),'-logFile',str(log)],stdout=open(f'/private/tmp/coo-4c1-standalone{n}.log','wb'),stderr=subprocess.STDOUT,start_new_session=True)
r={'pid':p.pid,'at':datetime.datetime.now(datetime.timezone.utc).isoformat(),'source':source,'input_fixture':str(src),'fixture_sha256':{name:hashlib.sha256((src/name).read_bytes()).hexdigest() for name in ['Quick.json','Quick.sav.gz']},'exe':str(exe),'log':str(log),'previous_owned_session_archive':str(archived) if archived else None,'normal_save_root_preexisting':False,'resolution':'ordinary default; no forced screen arguments'}
(e/f'mac-process{n}.json').write_text(json.dumps(r,indent=2)+'\n');print('Player',p.pid,'zone',info['ActiveZoneID'],info['HPSummary'])
