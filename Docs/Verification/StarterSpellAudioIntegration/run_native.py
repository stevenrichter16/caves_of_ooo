"""Native (audio-enabled) isolated acceptance launcher; never close an open editor."""
from pathlib import Path
import subprocess,sys,json
ROOT=Path('/Users/steven/caves-of-ooo')
sys.path.insert(0,str(ROOT/'Tools/Village3D'))
from run_common import refuse_unity,restart_mcp
name=sys.argv[1]
out=Path(__file__).resolve().parent/name
out.mkdir()
refuse_unity()
(out/'mcp-start.json').write_text(json.dumps(restart_mcp(Path('/Users/steven/unity-mcp/Server'),name),indent=2)+'\n')
cmd=['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity','-projectPath',str(ROOT),'-executeMethod','CavesOfOoo.Editor.StarterSpellAudioNativeAudit.RunFromCommandLine','-logFile',str(out/'unity.log')]
print('Starting native audio acceptance '+name,flush=True)
result=subprocess.run(cmd,cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT)
log=(out/'unity.log').read_text(errors='replace')
errors=[line for line in log.splitlines() if 'error CS' in line]
receipt={'exit':result.returncode,'compileErrors':errors,'cleanupCompleted':('[NativeSaveIsolation] Cleanup complete, exit='+str(result.returncode)) in log, 'cleanupExitCode':result.returncode,'native':True}
(out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps(receipt,indent=2),flush=True)
sys.exit(result.returncode)
