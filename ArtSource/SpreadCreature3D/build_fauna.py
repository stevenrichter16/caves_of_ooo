"""Original bounded lair fauna: explicit boxes, real anatomical bones, no gameplay."""
import argparse,json,math,os
from pathlib import Path
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT','/Users/steven/caves-of-ooo'))
CLIPS=['Idle','Walk','Interact','Attack','Hit']
def model(name,bp,family):return {'id':'spread-creature-'+name,'blueprint':bp,'rigFamily':family,'clips':CLIPS[:],'bones':[],'boxes':[],'sockets':[]}
def bone(m,n,h,t,p):m['bones'].append({'name':n,'head':list(h),'tail':list(t),'parent':p})
def box(m,n,c,s,col,b='Body'):m['boxes'].append({'part':n,'center':[round(x,5)for x in c],'size':[round(x,5)for x in s],'color':col,'bone':b})
def bases(m,y=.2):bone(m,'Root',(0,0,0),(0,.05,0),None);bone(m,'Body',(0,y,0),(0,y+.15,0),'Root')
def spider():
 m=model('giant-spider','GiantSpider','arachnid');bases(m,.27);bone(m,'Head',(0,.25,-.18),(0,.31,-.35),'Body')
 box(m,'abdomen',(0,.30,.21),(.46,.34,.49),19);box(m,'abdomen-top',(0,.476,.23),(.33,.045,.32),18);box(m,'thorax',(0,.25,-.12),(.32,.25,.32),23)
 box(m,'head',(0,.25,-.33),(.29,.20,.18),19,'Head')
 for side,sign in [('L',-1),('R',1)]:
  for i in range(4):
   z=-.22+i*.14;knee=sign*(.39+(.035 if i in [1,2]else 0));foot=sign*(.52+(.025 if i in [1,2]else 0));n='Leg.'+side+str(i);bone(m,n,(sign*.13,.27,z),(foot,.05,z+.05*(i-1.5)),'Body')
   box(m,'upper-'+n,(sign*.26,.28,z),(.31,.085,.075),18,n);box(m,'bend-'+n,(knee,.215,z),(.09,.20,.095),19,n);box(m,'lower-'+n,((knee+foot)/2,.105,z+.03*(i-1.5)),(.20,.09,.075),18,n);box(m,'foot-'+n,(foot,.022,z+.05*(i-1.5)),(.10,.044,.10),23,n)
  for i in range(2):box(m,'eye-'+side+str(i),(sign*(.065+i*.06),.305,-.425),(.038,.040,.022),17,'Head')
  box(m,'mouthpart-'+side,(sign*.065,.16,-.437),(.065,.10,.10),17,'Head')
 return m
def ape():
 m=model('jungle-ape','JungleApe','ape');bases(m,.70);bone(m,'Head',(0,.91,-.13),(0,1.18,-.23),'Body')
 box(m,'barrel-torso',(0,.70,.075),(.55,.60,.43),23);box(m,'shoulder-mantle',(0,.96,.015),(.76,.29,.43),6);box(m,'lower-belly',(0,.51,-.16),(.43,.27,.12),10)
 box(m,'head',(0,1.14,-.12),(.36,.37,.33),6,'Head');box(m,'pale-face',(0,1.095,-.30),(.30,.235,.075),17,'Head');box(m,'blunt-muzzle',(0,1.055,-.365),(.235,.10,.09),11,'Head');box(m,'brow',(0,1.21,-.325),(.34,.075,.075),23,'Head')
 for side,sign in [('L',-1),('R',1)]:
  bone(m,'Arm.'+side,(sign*.29,.93,0),(sign*.45,.43,-.13),'Body');bone(m,'Hand.'+side,(sign*.45,.40,-.13),(sign*.47,.08,-.21),'Arm.'+side);bone(m,'Leg.'+side,(sign*.15,.45,.07),(sign*.20,.08,.15),'Body')
  box(m,'upper-arm-'+side,(sign*.36,.73,-.02),(.23,.46,.27),6,'Arm.'+side);box(m,'forearm-'+side,(sign*.45,.40,-.14),(.225,.35,.26),23,'Arm.'+side);box(m,'knuckle-'+side,(sign*.47,.10,-.22),(.24,.20,.25),11,'Hand.'+side)
  for i in range(3):box(m,'finger-'+side+str(i),(sign*.47+(i-1)*.065,.065,-.345),(.047,.13,.03),17,'Hand.'+side)
  box(m,'thigh-'+side,(sign*.17,.32,.13),(.22,.32,.25),6,'Leg.'+side);box(m,'foot-'+side,(sign*.20,.07,.06),(.24,.14,.34),11,'Leg.'+side);box(m,'eye-'+side,(sign*.085,1.145,-.347),(.04,.035,.025),23,'Head')
 return m
def mimic(awake):
 m=model('mimic-awake'if awake else'mimic-closed','MimicChest','mimic');bases(m,.18);bone(m,'Head',(0,.35,.20),(0,.63,.16),'Body')
 box(m,'chest-box',(0,.215,0),(.69,.36,.52),18);box(m,'front-frame',(0,.20,-.285),(.73,.43,.045),20)
 for sign in [-1,1]:
  box(m,'side-band',(sign*.24,.235,0),(.075,.39,.56),21);box(m,'foot',(sign*.27,.035,.16),(.14,.07,.14),19)
 if not awake:
  box(m,'lid',(0,.465,0),(.74,.16,.57),18,'Head');box(m,'lid-top',(0,.56,0),(.62,.055,.44),20,'Head')
  for sign in [-1,1]:box(m,'lid-band',(sign*.24,.49,0),(.075,.235,.61),22,'Head')
  box(m,'latch',(0,.30,-.322),(.11,.17,.035),22)
 else:
  box(m,'mouth-shadow',(0,.445,-.065),(.59,.14,.45),0);box(m,'raised-lid',(0,.64,.14),(.75,.22,.33),18,'Head');box(m,'lid-inner',(0,.615,-.043),(.65,.16,.043),19,'Head')
  for i in range(5):
   box(m,'lower-tooth-'+str(i),((i-2)*.105,.39,-.285),(.064,.115,.068),17)
   box(m,'upper-tooth-'+str(i),((i-2)*.11,.51,-.082),(.075,.16,.068),17,'Head')
  box(m,'tongue',(0,.38,-.40),(.155,.055,.24),18);box(m,'tongue-tip',(0,.405,-.53),(.17,.065,.08),22)
  for side,sign in [('L',-1),('R',1)]:
   bone(m,'Arm.'+side,(sign*.29,.22,.0),(sign*.43,.05,-.18),'Body');box(m,'claw-base-'+side,(sign*.40,.115,-.15),(.17,.17,.23),19,'Arm.'+side)
   for i in range(2):box(m,'claw-'+side+str(i),(sign*.41+(i-.5)*.055,.065,-.29),(.035,.10,.10),17,'Arm.'+side)
 return m
def glowmaw():
 m=model('glowmaw','Glowmaw','grasping');bases(m,.32);bone(m,'Head',(0,.34,-.12),(0,.38,-.27),'Body')
 box(m,'flattened-body',(0,.36,.10),(.47,.31,.48),19);box(m,'back-plate',(0,.535,.14),(.38,.085,.37),20)
 box(m,'mouth-shadow',(0,.32,-.22),(.41,.31,.08),0,'Head');box(m,'throat',(0,.33,-.268),(.225,.175,.035),22,'Head')
 for i in range(8):
  a=math.tau*i/8;box(m,'blunt-mouth-rim-'+str(i),(math.cos(a)*.205,.33+math.sin(a)*.145,-.26),(.11,.085,.105),18,'Head')
 for side,sign in [('L',-1),('R',1)]:
  for i in range(2):
   n='Arm.'+side+str(i);z=-.08+i*.23;bone(m,n,(sign*.17,.34,z),(sign*.39,.055,z-.10),'Body');box(m,'upper-'+n,(sign*.30,.29,z),(.27,.115,.12),20,n);box(m,'grasp-'+n,(sign*.405,.145,z-.07),(.095,.25,.13),19,n);box(m,'digit-'+n,(sign*.36,.04,z-.15),(.18,.08,.13),17,n)
 return m
def build():return {'schemaVersion':1,'coordinates':'Unity X east,Y height,Z north;1unit/nativecell','palette':json.loads((ROOT/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'],'models':[spider(),ape(),mimic(False),mimic(True),glowmaw()]}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.write_text(json.dumps(build(),indent=2)+'\n')
