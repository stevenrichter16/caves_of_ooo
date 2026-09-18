from pathlib import Path
import subprocess,sys,json
ROOT=Path('/Users/steven/caves-of-ooo');OUT=Path(__file__).resolve().parent/'U03-import';OUT.mkdir();sys.path.insert(0,str(ROOT/'Tools/Village3D'));from run_common import refuse_unity,restart_mcp
refuse_unity();(OUT/'mcp.json').write_text(json.dumps(restart_mcp(Path('/Users/steven/unity-mcp/Server'),'building-block-import'),indent=2)+'\n')
r=subprocess.run(['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity','-batchmode','-quit','-projectPath',str(ROOT),'-executeMethod','CavesOfOoo.Editor.BuildingBlock3DAssetBuilder.Build','-logFile',str(OUT/'unity.log')],stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT,cwd=ROOT)
log=(OUT/'unity.log').read_text(errors='replace');errors=[x for x in log.splitlines() if 'error CS' in x];print('C# errors',len(errors));print('Unity exit',r.returncode);assert not errors and r.returncode==0
