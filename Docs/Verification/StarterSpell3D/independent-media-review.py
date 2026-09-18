"""Read-only independent inspection of encoded Blender study deliverables."""
from pathlib import Path
from PIL import Image, ImageChops
from fractions import Fraction
import json, subprocess, hashlib, math, sys

ROOT=Path('/Users/steven/caves-of-ooo')
ART=ROOT/'ArtSource/StarterSpell3D'
OUT=ROOT/'Docs/Verification/StarterSpell3D/independent-media-review.json'
manifest=json.loads((ART/'manifest.json').read_text())
source_hash_before=hashlib.sha256((ART/'starter_spells.blend').read_bytes()).hexdigest()
checks=[];videos=[];gifs=[];studies=[]
def check(name,passed,**evidence):
 checks.append(dict(name=name,passed=bool(passed),**evidence))
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def probe(path):
 return json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-count_frames','-show_streams','-of','json',str(path)]))['streams'][0]
def decode_selected(path,indices,width,height):
 expression='+'.join('eq(n\\,%d)'%n for n in indices)
 raw=subprocess.check_output(['ffmpeg','-v','error','-threads','1','-i',str(path),'-vf','select='+expression,'-vsync','0','-f','rawvideo','-pix_fmt','rgb24','-threads','1','-'])
 size=width*height*3
 assert len(raw)==len(indices)*size,(path,len(raw),len(indices)*size)
 return [Image.frombytes('RGB',(width,height),raw[i*size:(i+1)*size]) for i in range(len(indices))]
def difference(a,b):
 d=ImageChops.difference(a.convert('RGB'),b.convert('RGB'));hist=d.histogram()
 mse=sum((i%256)**2*n for i,n in enumerate(hist))/(a.width*a.height*3)
 bands=d.split();highest=ImageChops.lighter(ImageChops.lighter(bands[0],bands[1]),bands[2])
 significant=sum(highest.histogram()[16:])/(a.width*a.height)
 return dict(rgbMSE=mse,psnrDB=99.0 if mse==0 else 10*math.log10(255*255/mse),fractionPixelsChangedAtLeast16=significant)
def inspect_video(path,expected_size,expected_rate,indices,source_frames):
 stream=probe(path);width,height=stream['width'],stream['height']
 count=int(stream['nb_read_frames']);rate=float(Fraction(stream['avg_frame_rate']));duration=float(stream['duration'])
 check(path.name+' frame count',count==37,actual=count,expected=37)
 check(path.name+' dimensions and square pixels',(width,height)==expected_size and stream.get('sample_aspect_ratio')=='1:1',actual=[width,height],expected=list(expected_size),sampleAspectRatio=stream.get('sample_aspect_ratio'))
 check(path.name+' playback rate and duration',abs(rate-expected_rate)<1e-7 and abs(duration-37/expected_rate)<1e-5,actualRate=rate,actualDuration=duration,expectedDuration=37/expected_rate)
 decoded=decode_selected(path,indices,width,height)
 quality=[]
 for index,image,original in zip(indices,decoded,source_frames):
  with Image.open(original) as im:quality.append(dict(frame=index,source=str(original.relative_to(ROOT)),**difference(image,im)))
 check(path.name+' encoded frames match rendered sources',all(q['psnrDB']>=30 for q in quality),quality=quality)
 motion=difference(decoded[0],decoded[1]);check(path.name+' contains substantial visible motion',motion['fractionPixelsChangedAtLeast16']>=.0002,motion=motion)
 videos.append(dict(path=str(path.relative_to(ROOT)),sha256=sha(path),stream={k:stream.get(k) for k in ['codec_name','width','height','sample_aspect_ratio','avg_frame_rate','duration','nb_frames','nb_read_frames']},sampledFrames=indices,sourceFrameComparisons=quality,motion=motion))

check('Manifest identifies six initial actives and separate learnable Rain',sum(s['isStartingActive'] for s in manifest['studies'])==6 and next(s for s in manifest['studies'] if s['id']=='conjure_rain')['isStartingActive'] is False)
check('Native timing convention is 100 fps',manifest['nativeFps']==100,nativeFps=manifest['nativeFps'])
for s in manifest['studies']:
 sid=s['id'];folder=ART/'frames'/sid;files=sorted(folder.glob('*.png'))
 with Image.open(ART/'renders'/(sid+'.png')) as hero_image:
  density=hero_image.info.get('dpi')
  check(sid+' hero PNG has expected raster and square pixel metadata',hero_image.size==(1100,620) and (density is None or abs(density[0]-density[1])<1e-6),dimensions=list(hero_image.size),recordedDensity=density)
 check(sid+' source sequence is contiguous 37 frames',len(files)==37 and [p.name for p in files]==['%04d.png'%i for i in range(37)],count=len(files))
 sizes=set();densities=[]
 for path in files:
  with Image.open(path) as image:
   sizes.add(image.size)
   if image.info.get('dpi'):densities.append(image.info['dpi'])
 check(sid+' source frame raster is square pixel',sizes=={(840,474)} and all(abs(d[0]-d[1])<1e-6 for d in densities),sizes=[list(z) for z in sizes],recordedDensities=sorted(set(densities)))
 hero=s['heroFrame'];contact=s['contactFrame'];impact=s['impactEndFrame'];clear=s['clearFrame']
 phase='travel' if 22<=hero<contact else 'impact' if contact<=hero<impact else 'other'
 check(sid+' hero phase matches manifest',phase==s['heroPhase'],actualPhase=phase,declaredPhase=s['heroPhase'],heroNativeFrame=hero,contactNativeFrame=contact)
 check(sid+' sampled loop spans all declared phases and clears',0<12<22<=contact<impact<clear<108,contact=contact,impactEnd=impact,clear=clear,firstFrame=0,lastFrame=108,samplingStep=3)
 # First full 30ms sample at/after contact; exact subframe timings remain in Blender.
 contact_index=math.ceil(contact/3);after_index=math.ceil((clear+9)/3);indices=[0,contact_index,after_index]
 sources=[folder/('%04d.png'%i) for i in indices]
 for suffix,rate in [('motion',8),('native_timing',100/3)]:inspect_video(ART/'renders'/(sid+'_'+suffix+'.mp4'),(840,474),rate,indices,sources)
 studies.append(dict(id=sid,sourceNativeFrames=list(range(0,111,3)),contactNativeFrame=contact,firstSampleAtOrAfterContact=contact_index*3,contactSamplingDelaySeconds=(contact_index*3-contact)/100,heroNativeFrame=hero,heroPhase=phase,clearNativeFrame=clear))

for name,size,gifsize in [('starter_magic',(2220,1290),(1440,838)),('conjure_rain',(1160,810),(928,648))]:
 folder=ART/'frames'/(name+'_composite');indices=[0,10,26]
 inspect_video(ART/'renders'/(name+'_loop.mp4'),size,8,indices,[folder/('%04d.png'%i) for i in indices])
 path=ART/'renders'/(name+'_loop.gif');im=Image.open(path);frames=[];durations=[]
 loop=im.info.get('loop')
 for i in range(im.n_frames):
  im.seek(i);durations.append(im.info.get('duration',0));frames.append(im.convert('RGB').copy())
 check(name+' GIF dimensions frame count and infinite loop',im.size==gifsize and im.n_frames==37 and loop==0,size=list(im.size),count=im.n_frames,loop=loop)
 check(name+' GIF timing preserves requested slow cadence',durations==[120 if i%2==0 else 130 for i in range(37)],durationsMilliseconds=durations,totalMilliseconds=sum(durations),mp4TotalMilliseconds=4625)
 motion=difference(frames[0],frames[10]);hashes={hashlib.sha256(frame.tobytes()).hexdigest() for frame in frames}
 check(name+' GIF is genuinely animated',len(hashes)>10 and motion['fractionPixelsChangedAtLeast16']>=.0002,distinctDecodedFrames=len(hashes),motion=motion)
 gifs.append(dict(path=str(path.relative_to(ROOT)),sha256=sha(path),dimensions=list(im.size),frameCount=im.n_frames,loop=loop,totalDurationMilliseconds=sum(durations),distinctDecodedFrames=len(hashes),motion=motion))

check('Frozen Blender source unchanged during media inspection',source_hash_before==sha(ART/'starter_spells.blend'),before=source_hash_before,after=sha(ART/'starter_spells.blend'))
result=dict(status='FINAL encoded Blender preview inspection' if '--final' in sys.argv else 'DRAFT encoded preview observations; await explicit source/media freeze',method='Independent ffprobe frame/timing/aspect metadata, selected ffmpeg-decoded frames compared to original Blender PNGs, Pillow GIF decoding and frame differences. No renders or Unity operations.',manifestSha256=sha(ART/'manifest.json'),blenderFileSha256=sha(ART/'starter_spells.blend'),samplingHonesty='37 source samples, native frames 0..108 by3. Native MP4 samples at100/3fps and lasts1.11s including final frame hold; slow MP4 uses8fps and lasts4.625s. GIF hundredths require alternating120/130ms, summing4.620s (5ms shorter). Sampled contact can lag exact Blender subframe by0..20ms. This verifies encoded Blender previews, not gameplay integration or native render performance.',passed=sum(c['passed'] for c in checks),failed=sum(not c['passed'] for c in checks),checks=checks,videos=videos,gifs=gifs,studies=studies)
OUT.write_text(json.dumps(result,indent=2)+'\n')
print('INDEPENDENT_MEDIA_REVIEW',result['passed'],'PASS',result['failed'],'FAIL',str(OUT))
for c in checks:
 if not c['passed']:print('FAIL',json.dumps(c))
raise SystemExit(1 if result['failed'] else 0)
