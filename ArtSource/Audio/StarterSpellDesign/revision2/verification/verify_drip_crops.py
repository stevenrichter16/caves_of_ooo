"""Catch accidental amplification of background noise instead of selected drops."""
import json,subprocess,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
m=json.loads((ROOT/'manifest.json').read_text());rain=next(s for s in m['spells'] if s['id']=='conjure_rain');rows=[]
for variant in rain['variants']:
 layer=next(l for l in variant['layers'] if l['name']=='individual_drips')
 for e in layer['events']:
  p=ROOT/e['source'];x=np.frombuffer(subprocess.check_output(['ffmpeg','-v','error','-i',str(p),'-ac','1','-ar','48000','-f','f32le','pipe:1']),np.float32)
  a=round(e['sourceStartSeconds']*48000);b=round((e['sourceStartSeconds']+e['sourceLengthSeconds'])*48000)
  selected=float(np.sqrt(np.mean(x[a:b]**2)));reference=float(np.sqrt(np.mean(x*x)))
  rows.append({'variant':variant['variant'],'sourceStartSeconds':e['sourceStartSeconds'],'selectedRms':selected,'sourceRms':reference,'relativeActivity':selected/reference,'pass':selected>=reference*.2})
r={'pass':all(x['pass'] for x in rows),'scope':'Discrete-drop crop activity relative to this same recording; catches its quiet gaps, not a universal sound-quality threshold.','rows':rows}
print(json.dumps(r,indent=2));sys.exit(0 if r['pass'] else 1)
