"""Original quest-free Spread forms. Writes only an explicit private output."""
import json,copy,argparse
from pathlib import Path
ROOT=Path('/Users/steven/caves-of-ooo')
def model(suffix,bp,rigged=False):return {'id':'questfree-spread-'+suffix,'blueprint':bp,'rigged':rigged,'rigFamily':'quadruped' if rigged else '', 'clips':['Idle','Walk','Interact','Attack','Hit'] if rigged else [],'sockets':[],'bones':[],'boxes':[]}
def bone(m,name,head,tail,parent):m['bones'].append({'name':name,'head':list(head),'tail':list(tail),'parent':parent})
def box(m,part,center,size,color,bone='Body',material='palette'):
 d={'part':part,'center':list(center),'size':list(size),'color':color,'bone':bone,'material':material}
 if material=='water':d['semanticLiquid']='water'
 m['boxes'].append(d)
def build():
 m=model('grazer','ReedbackGrazer',True);m['feedingHeadPitch']=.68
 bone(m,'Root',(0,0,0),(0,.08,0),None);bone(m,'Body',(0,.30,.08),(0,.63,.08),'Root');bone(m,'Neck',(0,.46,-.25),(0,.50,-.38),'Body');bone(m,'Head',(0,.47,-.39),(0,.40,-.59),'Neck')
 box(m,'barrel',(0,.46,.055),(.48,.36,.66),6);box(m,'belly',(0,.285,.02),(.39,.10,.52),21);box(m,'shoulders',(0,.49,-.215),(.41,.37,.18),4)
 box(m,'sloped-neck',(0,.45,-.34),(.275,.24,.21),6,'Neck');box(m,'blunt-head',(0,.43,-.47),(.29,.22,.25),4,'Head');box(m,'broad-muzzle',(0,.345,-.625),(.26,.115,.135),17,'Head');box(m,'nose',(0,.365,-.697),(.205,.055,.024),19,'Head')
 for sign,side in [(-1,'L'),(1,'R')]:
  box(m,'side-ear-'+side,(sign*.205,.525,-.40),(.17,.075,.13),4,'Head');box(m,'inner-ear-'+side,(sign*.205,.566,-.40),(.115,.012,.085),21,'Head');box(m,'eye-'+side,(sign*.149,.456,-.532),(.018,.037,.04),0,'Head')
  for rear,z in [(False,-.17),(True,.285)]:
   n=('LegRear.' if rear else 'LegFront.')+side;bone(m,n,(sign*.172,.34,z),(sign*.18,.03,z),'Body')
   box(m,'upper-'+n,(sign*.176,.245,z),(.105,.29,.13),6,n);box(m,'ankle-'+n,(sign*.18,.094,z-.005),(.08,.13,.102),21,n)
   for split in (-1,1):box(m,'hoof-'+n,(sign*.18+split*.029,.025,z-.025),(.05,.05,.125),19,n)
 for i,(z,h) in enumerate([(-.12,.035),(.015,.065),(.15,.085),(.285,.055)]):
  box(m,'reed-ridge-'+str(i),(0,.65+h/2,z),(.075,h,.13),21 if i%2 else 4)
 bone(m,'Tail',(0,.43,.40),(0,.32,.54),'Body');box(m,'short-tail',(0,.365,.485),(.075,.16,.125),6,'Tail');box(m,'tail-tip',(0,.289,.52),(.12,.07,.105),21,'Tail')
 corpse=model('grazer-remains','ReedbackGrazerCorpse');box(corpse,'grounded-barrel',(.015,.135,.03),(.53,.25,.63),6);box(corpse,'folded-head',(-.12,.09,-.37),(.28,.16,.31),4);box(corpse,'blunt-muzzle',(-.13,.062,-.555),(.25,.09,.13),17)
 for sign in (-1,1):
  for z in (-.15,.23):box(corpse,'folded-hoof',(sign*.29,.037,z),(.14,.065,.13),19)
 for i,z in enumerate([-.08,.055,.19]):box(corpse,'reed-ridge-'+str(i),(.095,.24,z),(.085,.055,.115),21)
 empty=model('draw-empty','SpreadDrawPoint');box(empty,'buried-base',(0,.075,0),(.88,.15,.74),10);box(empty,'hollow-floor',(0,.205,0),(.67,.06,.53),0)
 box(empty,'front-lip',(0,.25,-.315),(.88,.20,.11),12);box(empty,'rear-lip',(0,.30,.315),(.88,.26,.11),12)
 for sign in (-1,1):box(empty,'side-lip',(sign*.385,.285,0),(.11,.23,.52),11)
 box(empty,'lower-front-step',(.11,.115,-.415),(.43,.13,.12),10)
 for i,(x,z) in enumerate([(-.465,.20),(-.465,.10),(.465,.20)]):box(empty,'short-bank-reed-'+str(i),(x,.24,z),(.028,.28,.04),6);box(empty,'reed-cap-'+str(i),(x,.395,z),(.04,.05,.065),5)
 full=copy.deepcopy(empty);full['id']='questfree-spread-draw-full';box(full,'finite-water-surface',(0,.315,0),(.65,.022,.51),0,material='water')
 return {'schemaVersion':1,'coordinates':'Unity X east/Y up/Z north; one unit per native cell','palette':json.loads((ROOT/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'],'models':[m,corpse,full,empty]}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.write_text(json.dumps(build(),indent=2)+'\n')
