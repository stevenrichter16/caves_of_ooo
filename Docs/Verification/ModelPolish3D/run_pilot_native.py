"""Native private-save pilot acceptance; exits through existing audit cleanup."""
from pathlib import Path
import subprocess,sys,json,time
ROOT=Path('/Users/steven/caves-of-ooo');OUT=Path(__file__).resolve().parent/'native-pilot';OUT.mkdir()
sys.path.insert(0,str(ROOT/'Tools/Village3D'));from run_common import refuse_unity,restart_mcp
refuse_unity();(OUT/'mcp-start.json').write_text(json.dumps(restart_mcp(Path('/Users/steven/unity-mcp/Server'),'model-polish-pilot'),indent=2)+'\n')
start=time.time();cmd=['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity','-projectPath',str(ROOT),'-executeMethod','CavesOfOoo.Editor.MultiCellPilotNativeAuditBatch.RunFromCommandLine','-logFile',str(OUT/'unity.log')]
r=subprocess.run(cmd,cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT)
log=(OUT/'unity.log').read_text(errors='replace');errors=[line for line in log.splitlines() if 'error CS' in line];print('C# errors',len(errors),flush=True)
reports=[p for p in (ROOT/'Docs/Verification/MultiCellPilot').glob('MCN-*-cleanup.json') if p.stat().st_mtime>=start];assert len(reports)==1,reports
cleanup=json.loads(reports[0].read_text());native=reports[0].with_name(reports[0].name.replace('-cleanup','-native'));d=json.loads(native.read_text());receipt={'exit':r.returncode,'compileErrors':errors,'cleanupReport':str(reports[0]),'nativeReport':str(native),'cleanup':cleanup,'nativeChecks':d.get('checks')};(OUT/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'exit':r.returncode,'compileErrors':errors,'cleanup':cleanup}),flush=True)
assert not errors and r.returncode==0;assert cleanup['finalNativeVerified']
