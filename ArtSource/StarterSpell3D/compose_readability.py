"""Labeled boards/video from actual Readability Blender pixels and sample times.

Uses only rendered frames. It does not reconstruct or retouch effect imagery.
Run after render_readability.py --loops with the SAME frozen source hash.
"""
import argparse,hashlib,json,subprocess
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parent
FONTS=Path('/System/Library/Fonts/Supplemental')
BG=(20,20,17);FG=(240,234,211);MUTED=(165,170,149)
def font(n,bold=False):return ImageFont.truetype(str(FONTS/('Arial Bold.ttf' if bold else 'Arial.ttf')),n)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def phase(spec,frame):
 if frame<12:return 'Gather'
 if frame<22:return 'Charge'
 if frame<spec['contactFrame']:return 'Travel'
 if frame<spec['impactEndFrame']:return 'Contact'
 if frame<spec['clearFrame']:return 'Settle'
 return 'Reading pause'
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--input',required=True);ap.add_argument('--output',required=True);args=ap.parse_args();src=Path(args.input);out=Path(args.output);out.mkdir(parents=True,exist_ok=True)
 receipt=json.loads((src/'render-receipt.json').read_text());source=sha(ROOT/'starter_spells.blend')
 if source!=receipt['sourceBlendSha256']:raise SystemExit('Render source hash differs from current Blender source; do not mix revisions')
 specs=json.loads((ROOT/'manifest.json').read_text())['studies'];rows=receipt['images'];outputs=[]
 def still(spec,view='detail'):
  row=next(r for r in rows if r['spell']==spec['id'] and r['phase']=='peak' and r['view']==view);p=src/row['path']
  if sha(p)!=row['sha256']:raise ValueError('Changed render '+str(p))
  return p,row['nativeFrame']
 def board(frame=None,small=False):
  width,height,pw,ph=(1320,740,420,260) if small else (2160,1188,690,388)
  im=Image.new('RGB',(width,height),BG);d=ImageDraw.Draw(im);d.text((25,22),'CAVES OF OOO · STARTER MAGIC · READABILITY 01',font=font(23,True),fill=FG)
  subtitle='Blender at retained gameplay pixel density · 33.38 px/cell · 56°' if small else 'Actual Blender renders · 56° · '+('4.17× slower review' if frame is not None else 'Selected contact peaks')
  d.text((25,57),subtitle,font=font(18),fill=MUTED)
  for i,spec in enumerate(specs[:6]):
   x=25+(i%3)*(pw+20);y=104+(i//3)*(ph+(48 if small else 155));d.text((x,y),spec['name'],font=font(20 if small else 27,True),fill=FG)
   if frame is None:p,native=still(spec,'native_scale' if small else 'detail')
   else:p=src/'frames'/spec['id']/('%04d.png'%(frame//3));native=frame
   image=Image.open(p).convert('RGB');image=image if small else image.resize((pw,ph),Image.Resampling.LANCZOS);im.paste(image,(x,y+35))
   if not small:d.text((x,y+ph+46),f'{phase(spec,native)} · native t={native/100:.3f}s',font=font(18),fill=MUTED)
  d.text((25,height-31),'Editable source preview; native game appearance is reviewed separately.',font=font(17),fill=MUTED)
  return im
 for name,im in [('contact-sheet.png',board()),('gameplay-scale-proof.png',board(small=True))]:p=out/name;im.save(p);outputs.append(p)
 # Composed frames retain the source sample interval; GIF cannot store .125sec,
 # so its slow-review durations alternate .12/.13sec without changing pixels.
 composite=out/'composite-frames';composite.mkdir(exist_ok=True);gif=[]
 for index,frame in enumerate(range(0,111,3)):
  im=board(frame);p=composite/('%04d.png'%index);im.save(p);gif.append(im.resize((1440,792),Image.Resampling.LANCZOS))
 palette=gif[12].quantize(colors=240,method=Image.Quantize.MEDIANCUT);gs=[im.quantize(palette=palette,dither=Image.Dither.FLOYDSTEINBERG) for im in gif];p=out/'starter-spells-review.gif';gs[0].save(p,save_all=True,append_images=gs[1:],duration=[120 if i%2==0 else 130 for i in range(37)],loop=0,disposal=2,optimize=True);outputs.append(p)
 def encode(folder,p,rate):
  subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate',str(rate),'-i',str(folder/'%04d.png'),'-vf','setsar=1','-c:v','libx264','-threads','2','-preset','medium','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(p)],check=True);outputs.append(p)
 encode(composite,out/'starter-spells-review.mp4',8)
 for spec in specs:
  folder=src/'frames'/spec['id'];encode(folder,out/(spec['id']+'-native-timing.mp4'),'100/3');encode(folder,out/(spec['id']+'-slow-review.mp4'),8)
 rain,native=still(specs[6]);p=out/'conjure-rain-peak.png';Image.open(rain).save(p);outputs.append(p)
 report=dict(sourceBlendSha256=source,renderReceiptSha256=sha(src/'render-receipt.json'),nativeFrameSamples=list(range(0,111,3)),nativeSampleRate=100,reviewPlaybackFps=8,reviewSlowFactor=100/3/8,outputs={str(p.relative_to(out)):dict(sha256=sha(p),bytes=p.stat().st_size) for p in outputs},boundary='Actual Blender pixels, not Unity screenshots. Fixed-scale proof pixels are pasted without resizing.')
 (out/'media-provenance.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(source=source,outputs=len(outputs),output=str(out)),indent=2))
if __name__=='__main__':main()
