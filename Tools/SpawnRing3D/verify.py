"""Owned headless EditMode gate. Start MCP before Unity; reject stale XML."""
import argparse, gzip, subprocess, time, sys
from pathlib import Path
import xml.etree.ElementTree as ET
p=argparse.ArgumentParser(); p.add_argument('--filter');p.add_argument('--archive',required=True);a=p.parse_args()
root=Path(__file__).resolve().parents[2];log=Path('/tmp/claude_unity_batch.log');xml=Path('/tmp/claude_testresults.xml')
if subprocess.run(['pgrep','-x','Unity'],stdout=subprocess.DEVNULL).returncode==0:raise SystemExit('Finish the current Unity run first.')
subprocess.run(['pkill','-f','[m]cp-for-unity'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);time.sleep(2)
with open('/tmp/mcp.log','w') as f:subprocess.Popen(['uv','run','mcp-for-unity','--transport','http'],cwd='/Users/steven/unity-mcp/Server',stdout=f,stderr=subprocess.STDOUT,start_new_session=True)
time.sleep(5);(root/'Temp/UnityLockfile').unlink(missing_ok=True);xml.unlink(missing_ok=True)
cmd=['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity','-batchmode','-projectPath',str(root),'-logFile',str(log),'-runTests','-testPlatform','EditMode','-testResults',str(xml)]
if a.filter:cmd+=['-testFilter',a.filter]
r=subprocess.run(cmd,cwd=root,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT)
lines=log.read_text(errors='replace').splitlines();errors=[l for l in lines if 'error CS' in l]
print('C# error lines:',len(errors),flush=True)
if errors:print('\n'.join(dict.fromkeys(errors)));sys.exit(2)
if not xml.exists():print('No fresh test XML. Editor exit:',r.returncode);sys.exit(3)
doc=ET.parse(xml).getroot();print(doc.attrib,flush=True)
for t in doc.iter('test-case'):
 if t.get('result')=='Failed':print(t.get('fullname'),t.findtext('failure/message'),flush=True)
destination=root/'Docs/Verification/GameSystemAudit'/a.archive
if destination.exists():raise SystemExit('Refusing to overwrite prior test archive: '+str(destination))
with gzip.open(destination,'wb') as f:f.write(xml.read_bytes())
with gzip.open(str(destination)+'.log.gz','wb') as f:f.write(log.read_bytes())
sys.exit(0 if doc.get('failed')=='0' else 1)
