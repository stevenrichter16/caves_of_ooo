from pathlib import Path
import subprocess,json,sys
ROOT=Path('/Users/steven/caves-of-ooo');OUT=Path(__file__).resolve().parent;blender='/Applications/Blender.app/Contents/MacOS/Blender'
with (OUT/'semantic-MultiCellPilot3D-final.log').open('w') as f:subprocess.run([blender,'-b','--threads','2','--python-exit-code','1','--python',str(ROOT/'ArtSource/ModelPolish3D/audit_semantics.py'),'--','/Users/steven/.codex/artifacts/model-polish-2026-09-11/baselines/MultiCellPilot3D',str(ROOT/'ArtSource/MultiCellPilot3D'),str(OUT/'semantic-MultiCellPilot3D-final.json')],stdout=f,stderr=subprocess.STDOUT,check=True)
with (OUT/'pilot-roundtrip-final.log').open('w') as f:subprocess.run([blender,'-b','--threads','2','--python-exit-code','1','--python',str(ROOT/'ArtSource/ModelPolish3D/validate_bundle.py'),'--',str(ROOT/'ArtSource/MultiCellPilot3D')],stdout=f,stderr=subprocess.STDOUT,check=True)
sys.path.insert(0,str(ROOT/'Tools/Village3D'));from run_common import refuse_unity,restart_mcp
refuse_unity();target=OUT/'import-pilot-material-fix';target.mkdir();(target/'mcp-start.json').write_text(json.dumps(restart_mcp(Path('/Users/steven/unity-mcp/Server'),'pilot-material-fix'),indent=2)+'\n')
r=subprocess.run(['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity','-batchmode','-quit','-projectPath',str(ROOT),'-executeMethod','CavesOfOoo.Editor.MultiCellPilot3DAssetBuilder.BuildFromCommandLine','-multiCellPilot3dSource',str(ROOT/'ArtSource/MultiCellPilot3D'),'-multiCellPilot3dReport',str(target/'import.json'),'-logFile',str(target/'unity.log')],stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT)
errors=[l for l in (target/'unity.log').read_text(errors='replace').splitlines() if 'error CS' in l];assert not errors,errors;assert r.returncode==0
assert json.loads((target/'import.json').read_text())['status']=='passed-import-validation';print('SEMANTICS, ROUNDTRIP AND UNITY PILOT MATERIAL PASS',flush=True)
