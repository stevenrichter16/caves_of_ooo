from pathlib import Path
import subprocess,json
root=Path('/Users/steven/caves-of-ooo');out=root/'Docs/Verification/ModelPolish3D'
for kit,label in [('Village3D','village'),('SpawnRing3D','ring'),('MultiCellPilot3D','pilot')]:
 dest=Path('/tmp/coo-model-polish-final')/kit
 cmd=['/Applications/Blender.app/Contents/MacOS/Blender','-b','--threads','2','--python-exit-code','1','--python',str(root/'ArtSource/ModelPolish3D/polish_bundle.py'),'--','--kit',kit,'--source','/tmp/coo-model-polish-baselines/'+kit,'--output',str(dest),'--render']
 with (out/('final-'+label+'-build.log')).open('w') as f:subprocess.run(cmd,stdout=f,stderr=subprocess.STDOUT,check=True)
 with (out/('final-'+label+'-roundtrip.log')).open('w') as f:subprocess.run(cmd[:7]+[str(root/'ArtSource/ModelPolish3D/validate_bundle.py'),'--',str(dest)],stdout=f,stderr=subprocess.STDOUT,check=True)
 d=json.loads((dest/'reports/roundtrip.json').read_text());assert d['passed'];print(kit,'PASS',len(d['models']),flush=True)
