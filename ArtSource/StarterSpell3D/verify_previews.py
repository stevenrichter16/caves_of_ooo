"""Check actual rendered sequences and encoded preview metadata."""
from pathlib import Path
from PIL import Image
import hashlib,json,subprocess
ROOT=Path(__file__).resolve().parent;manifest=json.loads((ROOT/'manifest.json').read_text());checks=[]
def need(name,ok,actual):checks.append(dict(name=name,passed=bool(ok),actual=actual))
for spec in manifest['studies']:
 files=sorted((ROOT/'frames'/spec['id']).glob('*.png'));need(spec['id']+' has complete actual render sequence',len(files)==37,len(files))
 sizes={Image.open(p).size for p in files};need(spec['id']+' source frame dimensions',sizes=={(840,474)},list(sizes))
 unique=len({hashlib.sha256(p.read_bytes()).hexdigest() for p in files});need(spec['id']+' is not one repeated still',unique>=15,unique)
 for mode,fps,duration in [('motion',8,4.625),('native_timing',100/3,1.11)]:
  path=ROOT/'renders'/(spec['id']+'_'+mode+'.mp4')
  probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-show_entries','stream=width,height,r_frame_rate,nb_frames,sample_aspect_ratio:format=duration','-of','json',str(path)]));s=probe['streams'][0];a,b=map(int,s['r_frame_rate'].split('/'))
  need(path.name+' has 37 real frames at expected clock',int(s['nb_frames'])==37 and abs(a/b-fps)<1e-6 and abs(float(probe['format']['duration'])-duration)<.01,probe)
  need(path.name+' has square output pixels',s['sample_aspect_ratio']=='1:1',s['sample_aspect_ratio'])
for name in ['starter_magic','conjure_rain']:
 image=Image.open(ROOT/'renders'/(name+'_loop.gif'));total=0
 for index in range(image.n_frames):image.seek(index);total+=image.info['duration']
 need(name+' GIF loops with measured slow-review timing',image.n_frames==37 and abs(total-4625)<=10 and image.info.get('loop')==0,dict(frames=image.n_frames,durationMs=total,loop=image.info.get('loop')))
report=dict(status='GREEN' if all(c['passed'] for c in checks) else 'RED',assertions=len(checks),passed=sum(c['passed'] for c in checks),checks=checks)
(ROOT/'preview-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(status=report['status'],assertions=report['assertions'],passed=report['passed']),indent=2))
if report['status']!='GREEN':raise SystemExit(1)
