#!/usr/bin/env python3
"""Compose actual Blender renders into labeled review boards; no invented effect art."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import argparse, json, subprocess, textwrap

ROOT=Path(__file__).resolve().parent
FONT=Path('/System/Library/Fonts/Supplemental')
def font(size,bold=False,mono=False):return ImageFont.truetype(str(FONT/('Andale Mono.ttf' if mono else 'Arial Bold.ttf' if bold else 'Arial.ttf')),size)
BG=(19,19,16);CARD=(28,29,24);CREAM=(235,227,201);MUTED=(164,168,145)
ACCENTS={'ember_spit':(244,138,64),'flaming_hands':(248,185,80),'jet_blast':(109,201,213),'ground_surge':(230,197,94),'rime_grip':(158,212,222),'calm':(206,167,221),'conjure_rain':(132,200,179)}
DATA=json.loads((ROOT/'manifest.json').read_text());SPECS=DATA['studies'];W=2220;PW=700;PH=541
def phase(spec,frame):
 if frame is None:return 'TRAVEL' if spec['id'] in ('ember_spit','calm') else 'CONTACT / RELEASE'
 if frame<12:return 'GATHER'
 if frame<22:return 'CHARGE'
 if frame<spec['contactFrame']:return 'TRAVEL'
 if frame<spec['impactEndFrame']:return 'CONTACT'
 if frame<spec['clearFrame']:return 'CLEAR'
 return 'READING PAUSE / RESET'
def panel(canvas,spec,x,y,index,frame=None):
 d=ImageDraw.Draw(canvas);d.rounded_rectangle((x,y,x+PW,y+PH),radius=13,fill=CARD)
 accent=ACCENTS[spec['id']]
 d.text((x+20,y+14),f'{index:02d}  {spec["school"]}',font=font(14,mono=True),fill=accent)
 d.text((x+20,y+36),spec['name'],font=font(31,bold=True),fill=CREAM)
 tag=spec['tag'];bbox=d.textbbox((0,0),tag,font=font(13,mono=True));d.text((x+PW-22-(bbox[2]-bbox[0]),y+46),tag,font=font(13,mono=True),fill=MUTED)
 imagepath=ROOT/'renders'/(spec['id']+'.png') if frame is None else ROOT/'frames'/spec['id']/('%04d.png'%(frame//3))
 im=Image.open(imagepath).convert('RGB').resize((PW,395),Image.Resampling.LANCZOS);canvas.paste(im,(x,y+78))
 d=ImageDraw.Draw(canvas);label=phase(spec,frame);d.text((x+20,y+480),label,font=font(13,mono=True),fill=accent)
 if frame is not None:d.text((x+PW-154,y+480),f'{frame/100:.2f}s NATIVE',font=font(13,mono=True),fill=MUTED)
 notes={'ember_spit':'Charcoal seed, three flame chisels, broken soot. One target.',
 'flaming_hands':'Folded flame teeth contained in one chosen adjacent cell.',
 'jet_blast':'River-blue folds, a heavy lip, three deliberate droplets.',
 'ground_surge':'Four low ground stitches. Target charge is conditional.',
 'rime_grip':'Three blunt closing clamps; the head remains uncovered.',
 'calm':'An open loop relaxes around a sentient recipient. No damage.',
 'conjure_rain':'Empty-handed moisture top-up for valid crops; no floor puddle.'}
 d.text((x+20,y+507),notes[spec['id']],font=font(16),fill=MUTED)
def board(frame=None):
 out=Image.new('RGB',(W,1290),BG);d=ImageDraw.Draw(out)
 d.text((40,29),'CAVES OF OOO',font=font(17,mono=True),fill=(159,164,135))
 d.text((40,54),'Small bindings. Strong silhouettes.',font=font(39,bold=True),fill=CREAM)
 d.text((W-559,32),'SIX STARTING SPELLS / POLISH 01',font=font(15,mono=True),fill=CREAM)
 d.text((W-559,61),'56° camera · one metre per cell · opaque carved meshes',font=font(18),fill=MUTED)
 for i,spec in enumerate(SPECS[:6]):panel(out,spec,40+(i%3)*720,126+(i//3)*561,i+1,frame)
 d.text((40,1257),'ACTUAL BLENDER RENDERS  /  '+('4.17× SLOW REVIEW LOOP' if frame is not None else 'SELECTED MOTION KEYFRAMES')+'  /  IN-PLACE CASTER ROOTS',font=font(15,mono=True),fill=MUTED)
 d.text((W-590,1257),'Polished design studies; native mesh handoff included.',font=font(17),fill=MUTED)
 return out
def rain(frame=None):
 out=Image.new('RGB',(1160,810),BG);d=ImageDraw.Draw(out)
 d.text((30,26),'CAVES OF OOO / LEARNABLE UTILITY',font=font(17,mono=True),fill=ACCENTS['conjure_rain'])
 d.text((30,56),'Conjure Rain',font=font(42,bold=True),fill=CREAM)
 d.text((30,109),'Learn from the carried Watering Grimoire or buy with skill points.',font=font(22),fill=MUTED)
 img=Image.open(ROOT/'renders/conjure_rain.png' if frame is None else ROOT/'frames/conjure_rain'/('%04d.png'%(frame//3))).convert('RGB');img=img.resize((1100,620),Image.Resampling.LANCZOS);out.paste(img,(30,146))
 d=ImageDraw.Draw(out);d.text((30,760),'EMPTY HANDS · CROP MOISTURE ONLY · NO FLOOR PUDDLES',font=font(17,mono=True),fill=CREAM)
 d.text((30,789),('SELECTED KEYFRAME' if frame is None else f'4.17× SLOW REVIEW · {phase(SPECS[6],frame)} · {frame/100:.2f}s NATIVE')+' / POLISH 01 BLENDER STUDY',font=font(13,mono=True),fill=MUTED)
 return out
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--loops',action='store_true');args=ap.parse_args()
 board().save(ROOT/'renders/starter_magic_contact_sheet.png');rain().save(ROOT/'renders/conjure_rain_design.png')
 if args.loops:
  for name,func in [('starter_magic',board),('conjure_rain',rain)]:
   folder=ROOT/'frames'/(name+'_composite');folder.mkdir(exist_ok=True)
   gif=[]
   for i,frame in enumerate(range(0,111,3)):
    image=func(frame);image.save(folder/('%04d.png'%i))
    gif.append(image.resize((1440,838) if name=='starter_magic' else (928,648),Image.Resampling.LANCZOS))
   palette=gif[10].quantize(colors=224,method=Image.Quantize.MEDIANCUT)
   frames=[im.quantize(palette=palette,dither=Image.Dither.FLOYDSTEINBERG) for im in gif]
   # GIF stores hundredths of a second. Alternate 120/130 ms instead of
   # silently truncating every 125 ms frame to 120 ms.
   durations=[120 if i%2==0 else 130 for i in range(len(frames))]
   frames[0].save(ROOT/'renders'/(name+'_loop.gif'),save_all=True,append_images=frames[1:],duration=durations,loop=0,optimize=True,disposal=2)
   subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate','8','-i',str(folder/'%04d.png'),'-vf','setsar=1','-c:v','libx264','-preset','slow','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(ROOT/'renders'/(name+'_loop.mp4'))],check=True)
  for spec in SPECS:
   subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate','8','-i',str(ROOT/'frames'/spec['id']/'%04d.png'),'-vf','setsar=1','-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(ROOT/'renders'/(spec['id']+'_motion.mp4'))],check=True)
   subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate','100/3','-i',str(ROOT/'frames'/spec['id']/'%04d.png'),'-vf','setsar=1','-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(ROOT/'renders'/(spec['id']+'_native_timing.mp4'))],check=True)
 print('Preview composition complete.')
if __name__=='__main__':main()
