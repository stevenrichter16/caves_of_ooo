#!/usr/bin/env python3
"""Two clearer recorded moments; exact4x inspection pixels, no art changes."""
from pathlib import Path
import hashlib,json
from PIL import Image,ImageDraw,ImageFont

root=Path(__file__).resolve().parent
receipt=json.loads((root/'media-verification.json').read_text())
report=Path(receipt['nativeReport']);sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
assert sha(report)==receipt['nativeReportSha256']
data=json.loads(report.read_text())
source=root/'starter-spells-unity-contact-sheet.png'
output=root/'starter-spells-unity-contact-sheet-refined.png'
assert not output.exists(),'Keep prior review attempts immutable.'
before={p.name:sha(p) for p in root.glob('starter-spells-unity-native-timing.*')}
im=Image.open(source).convert('RGB');pen=ImageDraw.Draw(im)
font=lambda n:ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',n)
bg=(24,27,25);fg=(235,235,219);mute=(170,182,167)
pen.rectangle((0,50,1938,88),fill=bg)
pen.text((20,55),'Real GameView captures • 56° camera • 2× local views; Calm and Rain enlarged 4× for detail',font=font(20),fill=mute)
changes=[]
for index,sid,label,crop,reason in [
 (5,'Spellcraft_Calm','Calm',(731,265,887,361),'Earlier recorded open loop is clearer around the recipient; later0.403s primarily shows the lower settling arcs.'),
 (6,'Hydromancy_ConjureRain','Conjure Rain',(664,274,820,370),'Earlier recorded falling strokes remain higher above the crop; later0.404s is closer to the ground. The4x crop makes the small original pixels inspectable without recoloring.')]:
 row=next(r for r in data['casts'] if r['spell']==sid and r['mode']=='showcase');f=row['frames'][3]
 assert f['meshes']>0 and sha(f['path'])==f['sha256']
 patch=Image.open(f['path']).convert('RGB').crop(crop);assert patch.size==(156,96)
 x=18+(index%3)*640;y=100+(index//3)*428
 pen.rectangle((x,y,x+624,y+417),fill=bg)
 pen.text((x,y),label,font=font(25),fill=fg)
 pen.text((x+200,y+6),'4× detail',font=font(17),fill=mute)
 pen.text((x+330,y+5),f"{f['wallSeconds']:.3f}s recorded",font=font(17),fill=mute)
 enlarged=patch.resize((624,384),Image.Resampling.NEAREST)
 im.paste(enlarged,(x,y+33))
 assert im.crop((x,y+33,x+624,y+417)).resize(patch.size,Image.Resampling.NEAREST).tobytes()==patch.tobytes()
 changes.append(dict(spell=sid,path=f['path'],sha256=f['sha256'],wallSeconds=f['wallSeconds'],crop=crop,magnification=4,reason=reason))
im.save(output)
assert {p.name:sha(p) for p in root.glob('starter-spells-unity-native-timing.*')}==before
result=dict(status='PASS',nativeRunId=data['runId'],originalSheet=dict(path=str(source),sha256=sha(source)),refinedSheet=dict(path=str(output),sha256=sha(output)),changes=changes,videoHashesUnchanged=before,checks=['Both selections use the same successful native run.','Both original screenshot hashes verified.','Every4x scene pixel repeats an original pixel exactly.','No color change, retouching or interpolation.','All other cards and GIF/MP4 remain unchanged.'])
(root/'contact-refinement-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({'status':result['status'],'output':str(output),'videoHashesUnchanged':True},indent=2))
