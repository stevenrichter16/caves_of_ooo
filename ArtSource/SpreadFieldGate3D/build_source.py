"""Original field timber gate, two static states; explicit private output only."""
import argparse,copy,json,os
from pathlib import Path
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT','/Users/steven/caves-of-ooo'))
def box(model,part,center,size,color):model['boxes'].append(dict(part=part,center=list(center),size=list(size),color=color))
def build():
 closed=dict(id='spread-field-gate-closed',blueprint='SpreadFieldGate',rigged=False,kind='entity',rigFamily='',clips=[],bones=[],sockets=[],boxes=[])
 # Both posts sit near the field boundary side of their own one-cell footprint.
 # The leaf opens back along the left margin, leaving the centre visibly empty.
 for sign,side in [(-1,'L'),(1,'R')]:
  height=.77 if sign<0 else .73
  box(closed,'fixed-post-'+side,(sign*.405,height/2,-.25),(.13,height,.16),19)
  box(closed,'fixed-endgrain-'+side,(sign*.405,height+.015,-.25),(.132,.03,.162),21)
  box(closed,'fixed-post-face-'+side,(sign*.405,height*.52,-.337),(.073,height*.68,.014),20)
 for i,y in enumerate([.22,.56]):box(closed,'fixed-hinge-'+str(i),(-.338,y,-.25),(.06,.045,.11),10)
 for sign,side in [(-1,'L'),(1,'R')]:box(closed,'leaf-upright-'+side,(sign*.295,.40,-.25),(.06,.54,.065),20)
 for i,y in enumerate([.18,.40,.62]):box(closed,'leaf-rail-'+str(i),(0,y,-.25),(.66,.075,.08),18 if i==2 else 20)
 opened=copy.deepcopy(closed);opened['id']='spread-field-gate-open'
 hx,hz=-.325,-.25
 for p in opened['boxes']:
  if p['part'].startswith('leaf-'):
   x,y,z=p['center'];sx,sy,sz=p['size'];p['center']=[hx-(z-hz),y,hz+(x-hx)];p['size']=[sz,sy,sx]
 return dict(schemaVersion=1,id='spread-field-gate-original',coordinates='Unity X east/Y up/Z north; one unit per cell',hinge=[hx,0,hz],palette=json.loads((ROOT/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'],models=[closed,opened])
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.write_text(json.dumps(build(),indent=2)+'\n')
