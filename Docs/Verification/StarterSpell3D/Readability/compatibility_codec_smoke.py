#!/usr/bin/env python3
"""Synthetic-only codec timing/order proof. No native game media is composed."""
import argparse
import importlib.util
import json
import subprocess
from pathlib import Path
from PIL import Image

parser=argparse.ArgumentParser();parser.add_argument('output',type=Path);args=parser.parse_args()
out=args.output.resolve();out.mkdir(parents=True,exist_ok=False)
source=Path(__file__).resolve().with_name('compose_final_native_media.py')
spec=importlib.util.spec_from_file_location('composer',source);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
checks=[]
def check(name,value):
 checks.append({'name':name,'pass':bool(value)})
 if not value: raise AssertionError(name)
try:
 check('explicit compatibility encoder exists',callable(getattr(module,'encode_compatibility',None)))
 timeline=[];cursor=0.;images=[]
 for index,duration in enumerate((.123456,.567890,.088800)):
  im=Image.new('RGB',(96,64));im.putdata([((x*17+index*73)%256,(y*23+index*83)%256,((x+y)*13+index*97)%256) for y in range(64) for x in range(96)])
  path=out/(str(index)+'.png');im.save(path);images.append(im.tobytes())
  timeline.append({'encodedPath':str(path),'start':cursor,'duration':duration});cursor+=duration
 evidence=module.encode_compatibility(timeline,out/'synthetic-standard-h264.mp4',(96,64))
 stream=evidence['ffprobe']['streams'][0]
 check('ordinary H264 with yuv420p',stream['codec_name']=='h264' and stream['pix_fmt']=='yuv420p')
 check('dimensions and square pixels preserved',(stream['width'],stream['height'],stream.get('sample_aspect_ratio'))==(96,64,'1:1'))
 pts=[float(x['best_effort_timestamp_time']) for x in evidence['ffprobe']['frames']]
 check('three actual samples and final hold sentinel only',len(pts)==4)
 check('native timestamp holds including long pause preserved',evidence['maximumPtsErrorSeconds']<=.0011 and abs((pts[2]-pts[1])-.567890)<=.0011)
 raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(out/'synthetic-standard-h264.mp4'),'-map','0:v:0','-fps_mode','passthrough','-pix_fmt','rgb24','-f','rawvideo','-'])
 step=96*64*3;frames=[raw[i:i+step] for i in range(0,len(raw),step)]
 check('decoded count matches actual sample count',len(frames)==4)
 nearest=[]
 for frame in frames:
  distances=[sum(abs(a-b) for a,b in zip(frame,target)) for target in images]
  nearest.append(min(range(len(distances)),key=distances.__getitem__))
 check('decoded sample identity/order preserved without invented inbetweens',nearest==[0,1,2,2])
 check('compression explicitly disclosed; no exact RGB claim',evidence['chromaCompressed'] is True and evidence['decodedRgbMatchesEveryInput'] is False and frames[0]!=images[0])
 result={'status':'PASS','syntheticOnly':True,'assertions':len(checks),'checks':checks,'evidence':evidence}
except Exception as exc:
 result={'status':'FAIL','syntheticOnly':True,'assertions':len(checks),'checks':checks,'error':type(exc).__name__+': '+str(exc)}
(out/'receipt.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='evidence'},indent=2))
raise SystemExit(0 if result['status']=='PASS' else 1)
