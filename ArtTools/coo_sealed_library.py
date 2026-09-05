#!/usr/bin/env python3
"""Original 16x16 archive architecture, precise shared palette and binary alpha."""
from pathlib import Path
import re,uuid
from PIL import Image,ImageDraw
from coo_stump_bestiary import outlined,INK
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Resources/Sprites/Environment'

def wall(kind,v):
 im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
 palettes=[((123,110,94),(205,193,160),(76,76,67)),((126,140,136),(213,224,201),(78,101,103)),((75,92,89),(150,166,133),(47,58,57))]
 base,hi,shade=palettes[kind]
 d.rectangle((1,1,14,14),fill=base);d.line((1,1,14,1),fill=hi);d.line((1,14,14,14),fill=shade)
 if kind==0:
  d.line([(2,5),(6+v,5),(8+v,6),(13,6)],fill=shade)
  d.line([(2,10),(5,10),(7,9),(13,9)],fill=hi)
  for x in (3+v,10-v):d.line((x,2,x-1,4),fill=hi)
  d.line((4+v,11,4+v,13),fill=shade)
 elif kind==1:
  for off in (0,6):
   d.line([(2,3+off),(5+v,4+off),(7,3+off),(11,4+off),(13,2+off)],fill=hi)
   d.line([(4,5+off),(7,6+off),(11-v,5+off)],fill=shade)
  d.point((12-v,12),fill=(237,235,202))
 else:
  d.rectangle((3,2,5,13),fill=shade);d.line((4,2,4,13),fill=hi)
  d.rectangle((10,2,12,13),fill=shade);d.line((11,2,11,13),fill=hi)
  d.line((2,6+v,13,6+v),fill=shade)
  for p in [(2,2),(13,2),(2,13),(13,13)]:d.point(p,fill=(197,190,145))
 return outlined(im)

def floor(v):
 im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
 d.polygon([(1,2),(7,1),(14,2),(14,13),(9,14),(1,13)],fill=(82,91,87))
 d.line([(2,3),(7,2),(13,3)],fill=(117,126,111));d.line((8+v%2,4,8+v%2,12),fill=(56,67,66))
 d.line((2,8+v%3,7,8+v%3),fill=(57,67,62));d.point((11,6+v),fill=(142,143,117))
 return outlined(im)

def shelf(v):
 im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
 d.rectangle((1,2,14,14),fill=(71,83,79));d.line((2,2,13,2),fill=(175,181,147))
 for row in (4,9):
  for n in range(3):
   x=2+n*4;col=[(193,182,143),(154,167,146),(176,143,116),(204,199,161)][(n+row+v)%4]
   d.rectangle((x,row,x+2,row+3),fill=col);d.line((x+1,row,x+1,row+3),fill=(107,94,72))
  d.line((2,row+4,13,row+4),fill=(131,146,128))
 d.point((3+v*3,7),fill=(201,102,81));return outlined(im)

def door():
 im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
 d.rectangle((2,1,13,14),fill=(170,173,143));d.rectangle((4,2,11,14),fill=(66,85,82))
 d.line((4,3,11,3),fill=(145,170,146));d.line((4,11,11,11),fill=(119,142,123))
 d.line((7,4,7,13),fill=(41,52,50));d.rectangle((8,7,10,9),fill=(201,178,125));d.point((9,8),fill=INK)
 for p in [(3,3),(12,3),(3,12),(12,12)]:d.point(p,fill=(220,211,171))
 return outlined(im)

def main():
 art={}
 families=[('library_tepuibone_wall',lambda v:wall(0,v)),('library_memory_marble_wall',lambda v:wall(1,v)),('library_choir_iron_wall',lambda v:wall(2,v)),('sealed_library_floor',floor),('sealed_archive_shelf',shelf)]
 for name,fn in families:
  for v in range(4):art[name+('' if v==0 else '_v'+str(v))]=fn(v)
 art['sealed_library_door']=door();template=(OUT/'cascade_father.png.meta').read_text()
 sheet=Image.new('RGB',(640,6*190),(46,49,42));d=ImageDraw.Draw(sheet)
 for i,(name,im) in enumerate(art.items()):
  assert set(im.getchannel('A').tobytes())<={0,255}
  p=OUT/(name+'.png');im.save(p);meta=Path(str(p)+'.meta')
  if not meta.exists():meta.write_text(re.sub(r'(?m)^guid: [0-9a-f]+$','guid: '+uuid.uuid4().hex,template))
  x=(i%4)*160;y=(i//4)*190;s=im.resize((160,160),Image.Resampling.NEAREST);sheet.paste(s,(x,y),s)
  d.text((x+2,y+163),name.replace('library_','').replace('_',' '),fill=(226,214,191))
 sheet.save(ROOT/'Docs/Verification/FellingW6/W66-architecture-art.png')
if __name__=='__main__':main()
