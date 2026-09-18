#!/usr/bin/env python3
"""Build truthful review media from a completed native Unity audit; never alter captures."""
from pathlib import Path
import argparse, hashlib, json, math, subprocess
from PIL import Image, ImageDraw, ImageFont

ap=argparse.ArgumentParser()
ap.add_argument('report',type=Path)
ap.add_argument('output',type=Path)
args=ap.parse_args()
report=args.report.resolve(); out=args.output.resolve()
assert not out.exists(), 'Use a new immutable run-specific media directory.'
report_hash=hashlib.sha256(report.read_bytes()).hexdigest()
d=json.loads(report.read_text()); run=d['runId']
assert d['failures']==0 and d['unexpectedErrors']==0 and d['workloadComplete']
assert d['shutdownObserved'] and d['shutdownRootHeld'] and d['shutdownSavingUnregistered']
assert d['displayPreferencesRestored'] and d['inputSettingsRestored']
assert (d['screenWidth'],d['screenHeight'])==(1920,1080)
labels=['Ember Spit','Flaming Hands','Jet Blast','Ground Surge','Rime Grip','Calm','Conjure Rain']
ids=['Pyromancy_EmberSpit','Pyromancy_FlamingHands','Hydromancy_JetBlast','Galvanism_GroundSurge','Cryomancy_RimeGrip','Spellcraft_Calm','Hydromancy_ConjureRain']
descriptions=['A compact charcoal seed and warm impact fragments.','Serrated flame folds inside the adjacent cell.','A broad folded stream and actual target displacement.','Low angular ground stitches along the captured path.','Blunt ice clamps only after a successful freeze.','An open loop around the pacified recipient.','Local rain over the crop that was actually watered.']
rows=[next(c for c in d['casts'] if c['spell']==sid and c['mode']=='showcase') for sid in ids]
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
source=[]
for row in rows:
 assert row['pass'] and row['sawGesture'] and row['gestureRestored'] and row['resultStable']
 assert len(row['frames'])==12 and row['zoneId']=='Overworld.3.6.0'
 assert (row['sourceX'],row['sourceY'])==(40,8), 'Review crop must be recalibrated if fixture moves.'
 previous=-1
 for f in row['frames']:
  p=Path(f['path']).resolve()
  assert p.parent==report.parent and p.name.startswith('SSN-'+run+'-')
  assert f['wallSeconds']>previous; previous=f['wallSeconds']
  assert len(f['sha256'])==64 and sha(p)==f['sha256']
  with Image.open(p) as im:assert im.size==(1920,1080)
  source.append({'path':str(p),'sha256':f['sha256'],'wallSeconds':f['wallSeconds']})
 assert row['seconds']>previous
out.mkdir(parents=True); encoded=out/'video_frames';encoded.mkdir()
fontpath='/System/Library/Fonts/Supplemental/Arial.ttf'
font=lambda n:ImageFont.truetype(fontpath,n)
bg=(24,27,25); fg=(235,235,219); mute=(170,182,167)
contact_crop=(656,234,968,426)
motion_crop=(640,200,1010,455)
contact=Image.new('RGB',(1938,1404),bg);pen=ImageDraw.Draw(contact)
pen.text((20,16),'CAVES OF OOO  /  STARTER MAGIC IN UNITY',font=font(31),fill=fg)
pen.text((20,55),'Real GameView captures • 56° camera • current game scale enlarged 2× for inspection',font=font(20),fill=mute)
selected=[]
for i,(row,label,desc) in enumerate(zip(rows,labels,descriptions)):
 frame=row['frames'][3 if i==1 else 4]
 assert frame['meshes']>0
 x=18+(i%3)*640;y=100+(i//3)*428
 pen.text((x,y),label,font=font(25),fill=fg)
 pen.text((x+330,y+5),f"{frame['wallSeconds']:.3f}s recorded",font=font(17),fill=mute)
 im=Image.open(frame['path']).convert('RGB').crop(contact_crop)
 contact.paste(im.resize((624,384),Image.Resampling.NEAREST),(x,y+33))
 # Every enlarged scene pixel is an exact repetition, with no invented detail.
 assert contact.crop((x,y+33,x+624,y+417)).resize(im.size,Image.Resampling.NEAREST).tobytes()==im.tobytes()
 selected.append({'spell':row['spell'],'label':label,'path':frame['path'],'wallSeconds':frame['wallSeconds'],'crop':contact_crop,'magnification':2})
context_frames=[('Town • complete GameView',rows[2]['frames'][4])]
south=next(c for c in d['casts'] if c['mode']=='south-showcase')
assert south['pass']
context_frames.append(('South pilot • complete GameView',south['frames'][4]))
for idx,(label,f) in enumerate(context_frames,7):
 assert sha(f['path'])==f['sha256']
 x=18+(idx%3)*640;y=100+(idx//3)*428
 pen.text((x,y),label,font=font(23),fill=fg)
 im=Image.open(f['path']).convert('RGB'); thumb=im.resize((624,351),Image.Resampling.LANCZOS)
 contact.paste(thumb,(x,y+33))
 name='town-full-gameview.png' if idx==7 else 'south-full-gameview.png'
 (out/name).write_bytes(Path(f['path']).read_bytes())
pen.text((20,1390),'Deterministic command fixtures using the live player, real outcomes and disposable recipients; images are not staged Blender renders.',font=font(16),fill=mute,anchor='ls')
contact.save(out/'starter-spells-unity-contact-sheet.png')

timeline=[]; images=[]; cursor=0.0
for i,row in enumerate(rows):
 frames=row['frames']
 for j,f in enumerate(frames):
  im=Image.new('RGB',(960,680),bg);p=ImageDraw.Draw(im)
  p.text((30,21),labels[i],font=font(35),fill=fg)
  p.text((30,67),f"REAL UNITY CAPTURE  •  1× recorded timing  •  t = {f['wallSeconds']:.3f}s",font=font(20),fill=mute)
  crop=Image.open(f['path']).convert('RGB').crop(motion_crop).resize((740,510),Image.Resampling.NEAREST)
  im.paste(crop,(110,108))
  p.text((30,633),descriptions[i],font=font(20),fill=fg)
  p.text((30,660),'2× inspection crop • ~10 captured samples/s • no interpolated motion',font=font(16),fill=mute)
  end=frames[j+1]['wallSeconds'] if j+1<len(frames) else row['seconds']
  duration=end-f['wallSeconds']
  if j==0:
   title=im.copy();tp=ImageDraw.Draw(title);tp.rectangle((0,59,960,99),fill=bg)
   tp.text((30,67),'TITLE PAUSE — gameplay playback starts next',font=font(20),fill=mute)
   timeline.append({'kind':'title_pause','spell':row['spell'],'start':cursor,'duration':.6});images.append(title);cursor+=.6
  timeline.append({'kind':'capture','spell':row['spell'],'path':f['path'],'captureWallSeconds':f['wallSeconds'],'start':cursor,'duration':duration})
  images.append(im);cursor+=duration

concat=['ffconcat version 1.0']
for i,(im,t) in enumerate(zip(images,timeline)):
 p=encoded/f'{i:04}.png';im.save(p)
 t['composedPath']=str(p);t['composedSha256']=sha(p)
 concat.extend([f"file '{p.as_posix()}'",'option framerate 1000',f"duration {t['duration']:.9f}"])
concat.extend([f"file '{(encoded/f'{len(images)-1:04}.png').as_posix()}'",'option framerate 1000'])
(out/'native-timing.ffconcat').write_text('\n'.join(concat)+'\n')
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-f','concat','-safe','0','-i',str(out/'native-timing.ffconcat'),'-fps_mode','vfr','-c:v','libx264','-threads','2','-crf','18','-pix_fmt','yuv420p','-video_track_timescale','1000000','-movflags','+faststart',str(out/'starter-spells-unity-native-timing.mp4')],check=True)
gifdur=[];rounded=0
for t in timeline:
 end=round((t['start']+t['duration'])*100)*10
 gifdur.append(end-rounded);rounded=end
assert min(gifdur)>0
images[0].save(out/'starter-spells-unity-native-timing.gif',save_all=True,append_images=images[1:],duration=gifdur,loop=0,optimize=False,disposal=2)
probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-show_frames','-show_entries','frame=best_effort_timestamp_time','-of','json',str(out/'starter-spells-unity-native-timing.mp4')]))
pts=[float(f['best_effort_timestamp_time']) for f in probe['frames']]
assert len(pts)==len(timeline)+1,(len(pts),len(timeline))
timing_error=max(abs(p-t['start']) for p,t in zip(pts,timeline));assert timing_error<.0011,timing_error
with Image.open(out/'starter-spells-unity-native-timing.gif') as gif:
 gif_t=0.;gif_error=0.
 assert gif.n_frames==len(timeline)
 for i,t in enumerate(timeline):
  gif.seek(i);gif_error=max(gif_error,abs(gif_t-t['start']));gif_t+=gif.info['duration']/1000
 assert gif_error<=.0051,gif_error
assert sha(report)==report_hash, 'Native report changed during composition.'
receipt={'status':'PASS','runId':run,'nativeReport':str(report),'nativeReportSha256':sha(report),'sources':source,'selectedContacts':selected,'timeline':timeline,'mp4MaxTimestampErrorSeconds':timing_error,'gifMaxTimestampErrorSeconds':gif_error,'nativeTimingSeconds':sum(t['duration'] for t in timeline if t['kind']=='capture'),'explicitTitlePauseSeconds':4.2,'scope':'Actual Unity command fixtures. No color changes, retouching, motion interpolation or art substitution. Exact nearest-neighbor2x local crops; full GameViews unchanged. Timeline starts at each first available capture and holds each image until the next measured timestamp. The last frame holds to the recorded case completion. GIF has10ms timing quantization; MP4 measured against recorded schedule. Capture sampling can miss detail between frames and does not establish subjective comfort.'}
receipt['outputs']=[{'path':str(p),'sha256':sha(p)} for p in sorted(out.glob('*')) if p.is_file()]
(out/'media-verification.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps({k:receipt[k] for k in ['status','runId','mp4MaxTimestampErrorSeconds','gifMaxTimestampErrorSeconds','nativeTimingSeconds']},indent=2))
