"""Import the validated art bundles using the existing strict Unity builders."""
from pathlib import Path
import subprocess,json,hashlib,sys,gzip
ROOT=Path('/Users/steven/caves-of-ooo');OUT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'Tools/Village3D'))
from run_common import refuse_unity,restart_mcp
refuse_unity()
def snapshot():return {str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for top in ['Assets','ProjectSettings'] for p in (ROOT/top).rglob('*') if p.is_file() and p.name!='.DS_Store'}
(OUT/'import-baseline.json').write_text(json.dumps(snapshot(),indent=2)+'\n')
for kit,arg in [('Village3D','village3d'),('SpawnRing3D','spawnRing3d'),('MultiCellPilot3D','multiCellPilot3d')]:
 source=ROOT/'ArtSource'/kit
 receipt=json.loads((source/'reports/polish-roundtrip.json').read_text());assert receipt['passed']
 target=OUT/('import-'+kit);target.mkdir()
 (target/'mcp-start.json').write_text(json.dumps(restart_mcp(Path('/Users/steven/unity-mcp/Server'),kit),indent=2)+'\n')
 cmd=['/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity','-batchmode','-quit','-projectPath',str(ROOT),'-executeMethod','CavesOfOoo.Editor.'+kit+'AssetBuilder.BuildFromCommandLine','-'+arg+'Source',str(source),'-'+arg+'Report',str(target/'import.json'),'-logFile',str(target/'unity.log')]
 r=subprocess.run(cmd,cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT)
 log=(target/'unity.log').read_text(errors='replace');errors=[line for line in log.splitlines() if 'error CS' in line];print(kit,'C# errors',len(errors),flush=True)
 with gzip.open(target/'unity.log.gz','wb') as f:f.write(log.encode())
 if errors or r.returncode:raise RuntimeError((kit,r.returncode,errors))
 d=json.loads((target/'import.json').read_text());assert d['status']=='passed-import-validation',d
 print(kit,'IMPORTED',d.get('modelCount',len(d.get('models',[]))),flush=True)
(OUT/'import-after.json').write_text(json.dumps(snapshot(),indent=2)+'\n')
