from pathlib import Path
import subprocess, plistlib, json, hashlib, os, signal, time
base=Path('/private/tmp/coo-mac-203023f31-20261123-native')
saves=Path.home()/'Library/Application Support/com.DefaultCompany.2D-URP/Saves'
domain='com.DefaultCompany.2D-URP'
exe=Path('/Users/steven/coo-verification/builds/mac-203023f31-20261123/CavesOfOoo.app/Contents/MacOS/caves-of-ooo')
def export():
 r=subprocess.run(['defaults','export',domain,'-'],capture_output=True,check=True)
 plistlib.loads(r.stdout);return r.stdout
if __name__=='__main__':
 mode=__import__('sys').argv[1]
 if mode=='launch':
  assert subprocess.run(['pgrep','-x','caves-of-ooo'],capture_output=True).returncode==1,'Another player is running'
  assert not saves.exists() and not saves.is_symlink(),'Normal saves changed; refuse'
  assert not base.exists();base.mkdir()
  before=export();(base/'prefs-before.plist').write_bytes(before)
  assert exe.is_file()
  with (base/'stdout.log').open('xb') as stream:
   p=subprocess.Popen([str(exe),'-screen-width','1280','-screen-height','800','-screen-fullscreen','0','-logFile',str(base/'player.log')],stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
  receipt={'pid':p.pid,'exe':str(exe),'exe_sha256':hashlib.sha256(exe.read_bytes()).hexdigest(),'saves_before':'absent','purpose':'Boot/render verification; fresh native input preflight required before any input'}
  (base/'process.json').write_text(json.dumps(receipt,indent=2)+'\n');print('Owned Mac player PID',p.pid)
 elif mode=='restore':
  r=json.loads((base/'process.json').read_text());pid=r['pid']
  command=subprocess.run(['ps','-p',str(pid),'-o','command='],capture_output=True,text=True).stdout.strip()
  if command:
   assert command.startswith(str(exe)+' '),'PID no longer belongs to owned player'
   os.kill(pid,signal.SIGTERM)
   for i in range(40):
    if subprocess.run(['ps','-p',str(pid),'-o','pid='],capture_output=True,text=True).returncode!=0:break
    time.sleep(.25)
   else:raise RuntimeError('Owned player did not exit; preserve state and stop')
  assert subprocess.run(['pgrep','-x','caves-of-ooo'],capture_output=True).returncode==1,'Another player appeared; do not restore over it'
  after=export()
  if not (base/'prefs-after-player.plist').exists():(base/'prefs-after-player.plist').write_bytes(after)
  a=plistlib.loads((base/'prefs-before.plist').read_bytes());b=plistlib.loads(after)
  changed=sorted(k for k in set(a)|set(b) if a.get(k)!=b.get(k))
  if saves.exists():
   assert not saves.is_symlink();saves.rename(base/'owned-saves-after')
  subprocess.run(['defaults','import',domain,str(base/'prefs-before.plist')],capture_output=True,check=True)
  restored=plistlib.loads(export())
  added=sorted(set(restored)-set(a))
  # defaults import may retain keys introduced by this owned player. Remove
  # only absent-before, still-equal-to-owned-after screen/LastGameID keys.
  allowed=lambda k:k.startswith('Screenmanager ') or k=='CavesOfOoo.LastGameID'
  assert all(allowed(k) and k not in a and k in b and restored[k]==b[k] for k in added),'Unexpected or independently changed added keys; preserve for review'
  for key in added:subprocess.run(['defaults','delete',domain,key],capture_output=True,check=True)
  restored=plistlib.loads(export());assert restored==a,'Logical preference restoration mismatch'
  assert not saves.exists() and not saves.is_symlink()
  out={'player_stopped':True,'normal_saves_restored':'absent','preferences_logically_restored':True,'changed_preference_keys':changed,'no_byte_exact_plist_claim':True}
  (base/'restoration.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out))
 else:raise ValueError(mode)
