"""Original Spread animal source descriptions; writes only an explicit output."""
import argparse,json,math,os
from pathlib import Path
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT',str(Path(__file__).resolve().parents[2])))
CLIPS=['Idle','Walk','Interact','Attack','Hit']
def animal(bp,family):
 return {'id':'spread-biome-'+{'Magpie':'magpie','PetDog':'pet-dog','Viper':'viper'}[bp],'blueprint':bp,'rigFamily':family,'clips':list(CLIPS),'sockets':[],'bones':[],'boxes':[]}
def bone(m,name,head,tail,parent):m['bones'].append({'name':name,'head':list(head),'tail':list(tail),'parent':parent})
def box(m,part,center,size,color,bone_name='Body'):
 m['boxes'].append({'part':part,'center':[round(v,5)for v in center],'size':[round(v,5)for v in size],'color':color,'bone':bone_name})
def make_magpie():
 m=animal('Magpie','avian');bone(m,'Root',(0,0,0),(0,.07,0),None);bone(m,'Body',(0,.13,0),(0,.37,0),'Root');bone(m,'Head',(0,.38,-.13),(0,.55,-.23),'Body')
 box(m,'body',(0,.33,.055),(.24,.27,.28),23);box(m,'breast',(0,.34,-.105),(.20,.22,.065),17);box(m,'neck',(0,.435,-.135),(.11,.15,.12),23,'Head')
 box(m,'head',(0,.54,-.24),(.18,.20,.20),23,'Head');box(m,'crown',(0,.645,-.22),(.145,.055,.15),23,'Head');box(m,'beak',(0,.505,-.395),(.055,.05,.14),19,'Head')
 for sign,side in [(-1,'L'),(1,'R')]:
  tag='left'if sign<0 else'right';bone(m,'Wing.'+side,(sign*.11,.37,0),(sign*.32,.30,.09),'Body')
  bone(m,'Leg.'+side,(sign*.06,.20,0),(sign*.07,.025,-.02),'Body')
  box(m,'wing-'+tag,(sign*.16,.33,.10),(.11,.18,.32),23,'Wing.'+side)
  box(m,'wing-bar-'+tag,(sign*.165,.432,.105),(.115,.032,.17),17,'Wing.'+side)
  box(m,'wing-tip-'+tag,(sign*.16,.30,.285),(.085,.075,.17),0,'Wing.'+side)
  box(m,'shin-'+tag,(sign*.065,.11,-.025),(.035,.18,.035),19,'Leg.'+side)
  box(m,'foot-'+tag,(sign*.075,.02,-.055),(.075,.04,.12),19,'Leg.'+side)
  box(m,'eye-'+tag,(sign*.091,.565,-.27),(.016,.032,.035),17,'Head')
 bone(m,'Tail',(0,.25,.18),(0,.16,.48),'Body')
 for i in range(3):box(m,'tail',(0,.245-i*.026,.29+i*.115),(.115-i*.014,.06,.18),23 if i%2 else 0,'Tail')
 return m
def make_dog():
 m=animal('PetDog','quadruped');bone(m,'Root',(0,0,0),(0,.08,0),None);bone(m,'Body',(0,.19,.03),(0,.45,.03),'Root');bone(m,'Head',(0,.40,-.20),(0,.57,-.36),'Body')
 box(m,'torso',(0,.33,.055),(.33,.27,.47),18);box(m,'chest',(0,.365,-.16),(.35,.31,.17),17)
 box(m,'back-saddle',(0,.455,.10),(.30,.06,.30),19);box(m,'head',(0,.51,-.285),(.27,.24,.25),18,'Head')
 box(m,'muzzle',(0,.46,-.445),(.18,.11,.16),17,'Head');box(m,'nose',(0,.47,-.53),(.115,.065,.035),0,'Head')
 for sign,side in [(-1,'L'),(1,'R')]:
  tag='left'if sign<0 else'right';box(m,'ear-'+tag,(sign*.09,.65,-.25),(.075,.17,.11),18,'Head');box(m,'inner-ear-'+tag,(sign*.09,.655,-.311),(.043,.10,.017),19,'Head');box(m,'eye-'+tag,(sign*.13,.55,-.37),(.027,.033,.030),0,'Head')
  for rear,z in [(False,-.125),(True,.215)]:
   name=('LegRear.'if rear else'LegFront.')+side;bone(m,name,(sign*.12,.31,z),(sign*.12,.04,z),'Body')
   box(m,'leg-'+name,(sign*.12,.16,z),(.075,.27,.095),18,name);box(m,'paw-'+name,(sign*.12,.035,z-.025),(.10,.07,.145),17,name)
 bone(m,'Tail',(0,.365,.28),(.10,.57,.42),'Body')
 for i in range(3):box(m,'tail',(i*.036,.40+i*.067,.30+i*.06),(.073,.11,.10),18 if i<2 else 17,'Tail')
 return m
def make_viper():
 m=animal('Viper','serpent');bone(m,'Root',(0,0,0),(0,.045,0),None);bone(m,'Body',(0,.075,-.25),(0,.13,-.35),'Root');bone(m,'Head',(0,.11,-.35),(0,.11,-.46),'Body')
 box(m,'head',(0,.12,-.41),(.19,.12,.21),7,'Head');box(m,'jaw',(0,.072,-.455),(.155,.035,.15),17,'Head')
 for sign in [-1,1]:box(m,'eye',(sign*.092,.145,-.46),(.018,.026,.032),17,'Head');box(m,'pupil',(sign*.101,.145,-.463),(.007,.02,.012),0,'Head')
 for i in range(20):
  x=math.sin(i*.34)*.26;z=-.29+i*.042;y=.09
  nx=math.sin((i+1)*.34)*.26;nz=z+.042;thick=.13*(1-i/24);name='Coil.'+str(i)
  bone(m,name,(x,y,z),(nx,y,nz),'Body'if i==0 else'Coil.'+str(i-1))
  # Each stepped volume spans successive curve points with a positive cuff.
  # Adjacent cuboids overlap even on the tight tail bend; no disconnected beads.
  box(m,'coil-'+str(i),((x+nx)*.5,y,(z+nz)*.5),(abs(nx-x)+thick,.085*(1-i/28),.042+thick*.65),7 if i%2 else 8,name)
  box(m,'dorsal-scale-'+str(i),((x+nx)*.5,y+.045*(1-i/28),(z+nz)*.5),(.043*(1-i/24),.015,.050),9,name)
 return m
def build():
 palette=json.loads((ROOT/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette']
 return {'schemaVersion':1,'coordinates':'Unity X east,Y height,Z north;1unit/nativecell','palette':palette,'models':[make_magpie(),make_dog(),make_viper()]}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.write_text(json.dumps(build(),indent=2)+'\n')
