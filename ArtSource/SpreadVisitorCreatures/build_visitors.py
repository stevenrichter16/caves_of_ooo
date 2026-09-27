"""Original receiving-biome bodies. Pure authored data; no gameplay or old art writes."""
import json,math,re,os
from pathlib import Path
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT',str(Path(__file__).resolve().parents[2])))
BASE=json.loads((ROOT/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette']
PALETTE=BASE+['#C84332','#151C1C','#37476B','#D99B43','#95CBCB','#866C98']
CLIPS=['Idle','Walk','Interact','Attack','Hit']
def model(bp,family):return dict(id='spread-visitor-'+re.sub(r'(?<!^)(?=[A-Z])','-',bp).lower(),blueprint=bp,rigFamily=family,clips=CLIPS[:],bones=[],boxes=[],sockets=[])
def bone(m,n,h,t,p):m['bones'].append(dict(name=n,head=list(h),tail=list(t),parent=p))
def box(m,n,c,s,col,b='Body'):
 m['boxes'].append(dict(part=n,center=[round(v,5) for v in c],size=[round(v,5) for v in s],color=col,bone=b))
def base(m,y=.25):bone(m,'Root',(0,0,0),(0,.05,0),None);bone(m,'Body',(0,y,0),(0,y+.12,0),'Root')
def eyes(m,x,y,z,scale=.035,iris=27):
 for side,sign in [('L',-1),('R',1)]:
  box(m,'eye-iris-'+side,(sign*x,y,z),(scale*1.5,scale*1.1,scale*.55),iris,'Head');box(m,'eye-pupil-'+side,(sign*x,y,z-scale*.3),(scale*.45,scale*.95,scale*.18),25,'Head')
def frog(bp):
 m=model(bp,'frog');base(m,.18);bone(m,'Head',(0,.20,-.15),(0,.28,-.30),'Body')
 color={'Reedfrog':8,'Bandfrog':25,'GinFrog':22,'SummitSinger':20}[bp];light={'Reedfrog':15,'Bandfrog':24,'GinFrog':17,'SummitSinger':18}[bp];scale={'Reedfrog':1.12,'Bandfrog':.90,'GinFrog':1,'SummitSinger':.76}[bp]
 box(m,'squat-trunk',(0,.23,.05),(.44,.32,.48),color);box(m,'broad-head',(0,.24,-.23),(.44,.235,.24),color,'Head');box(m,'lifted-throat',(0,.12,-.25),(.28,.15,.18),light,'Head')
 for side,sign in [('L',-1),('R',1)]:
  box(m,'brow-'+side,(sign*.165,.375,-.26),(.13,.105,.12),color,'Head')
  for rear,z in [(False,-.12),(True,.19)]:
   name=('LegRear.' if rear else 'LegFront.')+side;bone(m,name,(sign*.15,.20,z),(sign*.28,.05,z-.10),'Body')
   if rear:box(m,'folded-thigh-'+side,(sign*.25,.18,z+.025),(.23,.23,.28),color,name)
   box(m,'elbow-'+name,(sign*.30,.085,z-.055),(.12,.14,.11),light,name);box(m,'webbed-foot-'+name,(sign*.28,.025,z-.16),(.19,.05,.15),light,name)
   for j in range(3):box(m,'toe-'+name+str(j),(sign*.28+(j-1)*.060,.022,z-.25),(.039,.044,.08),light,name)
 eyes(m,.165,.382,-.327,.033,27 if bp!='GinFrog' else 20)
 if bp=='Bandfrog':
  for i,z in enumerate([-.09,.045,.18]):box(m,'scarlet-band-'+str(i),(0,.397,z),(.432,.024,.067),24)
 elif bp=='SummitSinger':
  for i,(x,z) in enumerate([(-.14,.14),(-.105,.075),(-.07,.02),(-.035,.075),(0,.14),(.035,.075),(.07,.02),(.105,.075),(.14,.14)]):box(m,'dorsal-W-'+str(i),(x,.397,z),(.048,.020,.068),19)
 elif bp=='Reedfrog':
  for x,z in [(-.13,.10),(.13,.02),(-.09,-.08),(.10,.19)]:box(m,'wet-back-patch',(x,.401,z),(.085,.025,.085),13)
 else:
  box(m,'quiet-pale-cheeks',(0,.272,-.358),(.31,.09,.023),17,'Head')
 for b in m['bones']:
  for key in ['head','tail']:b[key]=[round(v*scale,5)for v in b[key]]
 for b in m['boxes']:
  for key in ['center','size']:b[key]=[round(v*scale,5)for v in b[key]]
 return m
def lizard(bp):
 m=model(bp,'lizard');base(m,.17);bone(m,'Head',(0,.19,-.20),(0,.22,-.38),'Body');color={'SunStriker':21,'BrocchiniaSentinel':6,'PrickleBrowGecko':26}[bp];light=27 if bp!='BrocchiniaSentinel' else 20
 width=.36 if bp=='SunStriker' else .29 if bp=='BrocchiniaSentinel' else .23
 box(m,'flattened-trunk',(0,.20,.02),(width,.19,.51),color);box(m,'wedge-head',(0,.19,-.32),(width*.88,.17,.25),color,'Head');box(m,'throat',(0,.128,-.35),(width*.66,.085,.17),light,'Head')
 for side,sign in [('L',-1),('R',1)]:
  for rear,z in [(False,-.14),(True,.22)]:
   name=('LegRear.'if rear else'LegFront.')+side;bone(m,name,(sign*width*.42,.18,z),(sign*.32,.04,z-.07),'Body')
   box(m,'upper-limb-'+name,(sign*.235,.15,z+.04),(.23,.09,.09),color,name);box(m,'sharp-elbow-'+name,(sign*.33,.085,z),(.08,.14,.10),color,name);box(m,'long-foot-'+name,(sign*.32,.028,z-.10),(.08,.056,.19),light,name)
   for j in range(3 if bp=='SunStriker' else 5):box(m,'toe-'+name+str(j),(sign*.32+(j-(1 if bp=='SunStriker' else 2))*.025,.022,z-.21),(.018,.044,.075),light,name)
  if bp=='PrickleBrowGecko':box(m,'brow-spine-'+side,(sign*.105,.36,-.30),(.032,.26,.042),17,'Head')
 eyes(m,width*.31,.255,-.45,.025,27)
 for i in range(5):
  name='Tail.'+str(i);prev='Body'if i==0 else'Tail.'+str(i-1);z=.28+i*.11;y=.18-i*.026;w=.15-i*.024;bone(m,name,(0,y,z),(0,y-.015,z+.13),prev);box(m,'tapered-tail-'+str(i),(math.sin(i*.6)*.025,y,z+.045),(w,.10-i*.016,.145),color,name)
 if bp=='SunStriker':
  for i,z in enumerate([-.08,.065,.205]):box(m,'sun-band-'+str(i),(0,.303,z),(width*.98,.026,.053),19)
 elif bp=='BrocchiniaSentinel':
  for i,(x,z) in enumerate([(-.085,.13),(.08,.09),(-.07,-.08),(.08,-.10)]):box(m,'lichen-mottle-'+str(i),(x,.304,z),(.085,.025,.085),4 if i%2 else 10)
 else:
  for side,sign in [('L',-1),('R',1)]:
   for i in range(4):box(m,'indigo-lace-'+side+str(i),(sign*.075,.305,-.10+i*.10),(.034,.025,.055),17)
 return m
def bat():
 m=model('CaveBat','avian');base(m,.38);bone(m,'Head',(0,.43,-.10),(0,.53,-.21),'Body');box(m,'fur-body',(0,.38,.0),(.22,.28,.25),19);box(m,'short-face',(0,.48,-.15),(.23,.20,.17),10,'Head')
 for side,s in [('L',-1),('R',1)]:
  bone(m,'Wing.'+side,(s*.10,.43,0),(s*.72,.39,.08),'Body');box(m,'long-ear-'+side,(s*.085,.64,-.14),(.075,.24,.095),10,'Head')
  for i in range(4):box(m,'wing-panel-'+side+str(i),(s*(.22+i*.135),.41-i*.018,.045+i*.033),(.19,.045,.36-i*.065),26 if i%2 else 23,'Wing.'+side)
  box(m,'wing-thumb-'+side,(s*.18,.45,-.17),(.075,.07,.12),17,'Wing.'+side);bone(m,'Leg.'+side,(s*.065,.27,.07),(s*.10,.16,.10),'Body');box(m,'hanging-foot-'+side,(s*.09,.18,.08),(.075,.16,.11),17,'Leg.'+side)
 eyes(m,.07,.51,-.245,.022,27);return m
def slime():
 m=model('CaveSlime','gel');base(m,.15)
 for n,c,s,col in [('broad-gel-base',(0,.055,0),(.64,.11,.61),13),('gel-middle',(.025,.17,.025),(.52,.20,.47),8),('slumped-crown',(-.045,.31,.04),(.33,.13,.29),14),('gel-highlight',(-.09,.382,-.02),(.17,.025,.15),15),('trapped-mineral',(.12,.276,-.13),(.09,.025,.10),22),('front-gel-edge',(0,.14,-.275),(.35,.09,.07),9)]:box(m,n,c,s,col)
 return m
def scorpion(bp):
 m=model(bp,'arachnid');base(m,.19);bone(m,'Head',(0,.19,-.23),(0,.23,-.35),'Body');glass=bp=='GlassScorpion';col=28 if glass else 21;dark=12 if glass else 19;light=17 if glass else 22
 box(m,'segmented-carapace',(0,.21,.045),(.30,.24,.48),col);box(m,'low-face',(0,.19,-.245),(.26,.17,.15),dark,'Head')
 for j in range(3):box(m,'back-facet-'+str(j),(0,.341,-.08+j*.13),(.255,.024,.070),light)
 for side,s in [('L',-1),('R',1)]:
  for i in range(4):
   n='Leg.'+side+str(i);z=-.17+i*.12;bone(m,n,(s*.11,.20,z),(s*.42,.035,z+.04),'Body');box(m,'leg-upper-'+n,(s*.26,.17,z),(.29,.07,.065),col,n);box(m,'leg-foot-'+n,(s*.415,.065,z+.03),(.07,.13,.10),dark,n)
  n='Arm.'+side;bone(m,n,(s*.13,.20,-.22),(s*.27,.12,-.48),'Body');box(m,'pincer-arm-'+side,(s*.235,.15,-.38),(.125,.12,.30),col,n);box(m,'pincer-palm-'+side,(s*.29,.12,-.54),(.19,.16,.18),dark,n)
  for j in [-1,1]:box(m,'pincer-tip-'+side+str(j),(s*.29+j*.069,.12,-.675),(.055,.115,.18),light,n)
 for i,(y,z) in enumerate([(.24,.32),(.40,.40),(.57,.41),(.70,.32),(.72,.17)]):
  n='Tail.'+str(i);bone(m,n,(0,y,z),(0,y+.055,z-.04),'Body'if i==0 else'Tail.'+str(i-1));box(m,'tail-segment-'+str(i),(0,y,z),(.14-i*.012,.19,.17),col,n)
 box(m,'curved-sting',(0,.64,.075),(.06,.19,.09),light,'Tail.4');eyes(m,.073,.23,-.334,.018,25)
 if glass:box(m,'glass-heart',(0,.35,.12),(.065,.08,.075),27)
 return m
def quadruped(bp):
 profiles={'CaveBear':(.53,.56,.68,19,10,.34,.23),'PaleStalker':(.26,.63,.58,17,12,.23,.15),'DesertProwler':(.36,.38,.65,21,20,.25,.19),'BrittleHound':(.28,.46,.57,28,12,.28,.16),'JungleStalker':(.32,.40,.75,23,7,.23,.18)}
 w,y,length,col,accent,headw,headl=profiles[bp];m=model(bp,'quadruped');base(m,y);bone(m,'Head',(0,y+.06,-length*.46),(0,y+.075,-length*.72),'Body')
 box(m,'ribcage',(0,y,0),(w,.32 if bp=='CaveBear' else .25,length),col);box(m,'shoulder',(0,y+.10,-length*.24),(w*1.13,.26,.25),accent);box(m,'head',(0,y+.07,-length*.62),(headw,.23,headl),col,'Head');box(m,'short-muzzle'if bp=='CaveBear' else'long-muzzle',(0,y-.005,-length*.62-headl*.6),(headw*.66,.105,headl*.64),accent,'Head')
 for side,s in [('L',-1),('R',1)]:
  for rear,z in [(False,-length*.30),(True,length*.30)]:
   n=('LegRear.'if rear else'LegFront.')+side;bone(m,n,(s*w*.34,y-.10,z),(s*w*.41,.065,z-.035),'Body');box(m,'limb-'+n,(s*w*.40,(y-.1)/2+.04,z),(.13 if bp=='CaveBear'else .085,y-.10,.13),col,n);box(m,'paw-'+n,(s*w*.41,.05,z-.065),(.18 if bp=='CaveBear'else .12,.10,.22),accent,n)
  box(m,'ear-'+side,(s*headw*.35,y+.24,-length*.61),(.09 if bp=='CaveBear'else .065,.10 if bp=='CaveBear'else .17,.08),accent,'Head')
 eyes(m,headw*.32,y+.105,-length*.62-headl*.52,.026,27)
 if bp!='CaveBear':
  for i in range(3):
   n='Tail.'+str(i);z=length*.48+i*.14;bone(m,n,(0,y,z),(0,y-.025,z+.15),'Body'if i==0 else'Tail.'+str(i-1));box(m,'tail-'+str(i),(0,y-i*.04,z+.07),(.09-i*.017,.085-i*.014,.19),col,n)
 else:box(m,'short-tail',(0,y,.40),(.14,.15,.14),col)
 if bp=='BrittleHound':
  for i in range(4):box(m,'glass-shard-'+str(i),((-.06 if i%2 else .06),y+.24,-.23+i*.14),(.065,.22+.035*(i%2),.10),17)
 elif bp=='PaleStalker':
  for s in [-1,1]:box(m,'brow-shelf',(s*.11,y+.20,-.40),(.10,.055,.23),12,'Head')
 elif bp=='JungleStalker':
  for i in range(4):box(m,'dark-dorsal-band-'+str(i),(0,y+.144,-.22+i*.14),(w*.95,.027,.06),7)
 return m
def worm():
 m=model('SandWurm','serpent');base(m,.24)
 for i in range(7):
  z=-.58+i*.185;y=.24+math.sin(i*.48)*.065;n='Segment.'+str(i);bone(m,n,(0,y,z),(0,y,z+.20),'Body'if i==0 else'Segment.'+str(i-1));box(m,'thick-segment-'+str(i),(0,y,z),(.42-i*.024,.37-i*.016,.235),21 if i%2 else 20,n);box(m,'ridge-'+str(i),(0,y+.205-i*.008,z),(.29-i*.016,.055,.11),22,n)
 bone(m,'Head',(0,.24,-.61),(0,.24,-.80),'Body');box(m,'mouth-dark',(0,.24,-.746),(.32,.30,.055),25,'Head')
 for i in range(8):
  a=i*math.tau/8;box(m,'mouth-rim-'+str(i),(math.cos(a)*.175,.24+math.sin(a)*.175,-.795),(.105,.085,.11),17,'Head')
 return m
def dune():
 m=model('DuneLurker','grasping');base(m,.18);bone(m,'Head',(0,.21,-.19),(0,.22,-.38),'Body');box(m,'flattened-sand-shell',(0,.22,.10),(.58,.30,.53),20);box(m,'buried-back-ridge',(0,.395,.16),(.47,.085,.33),21);box(m,'horizontal-maw',(0,.22,-.215),(.47,.20,.085),25,'Head')
 for side,s in [('L',-1),('R',1)]:
  for i in range(2):
   n='Arm.'+side+str(i);z=-.12+i*.26;bone(m,n,(s*.20,.22,z),(s*.45,.055,z-.13),'Body');box(m,'grasping-limb-'+n,(s*.37,.15,z-.06),(.25,.15,.15),21,n);box(m,'burrow-claw-'+n,(s*.455,.06,z-.19),(.15,.10,.18),17,n)
  box(m,'maw-side-'+side,(s*.235,.22,-.29),(.07,.28,.12),22,'Head')
 for i in range(4):box(m,'maw-tooth-'+str(i),((i-1.5)*.105,.30,-.29),(.055,.12,.06),17,'Head')
 return m
def plant():
 m=model('CanopyStrangler','rooted');base(m,.25);box(m,'root-knuckle',(0,.16,0),(.48,.30,.44),19);box(m,'bowed-trunk',(0,.58,.04),(.20,.76,.23),6);box(m,'leaf-crown',(0,.99,.03),(.47,.17,.41),7)
 for i in range(4):
  a=i*math.tau/4;x=math.cos(a)*.36;z=math.sin(a)*.36;n='Tendril.'+str(i);bone(m,n,(x*.32,.72,z*.32),(x,.24,z),'Body');box(m,'vine-shoulder-'+str(i),(x*.62,.72,z*.62),(.17,.17,.17),8,n);box(m,'hanging-vine-'+str(i),(x,.46,z),(.085,.57,.09),6,n);box(m,'hooked-root-'+str(i),(x*.87,.18,z*.87),(.17,.10,.18),17,n);box(m,'leaf-'+str(i),(x*.55,.96,z*.55),(.22,.075,.28),9,n)
 return m
def humanoid(bp):
 m=model(bp,'humanoid');bone(m,'Root',(0,0,0),(0,.12,0),None);bone(m,'Spine',(0,.64,0),(0,1.27,0),'Root');bone(m,'Head',(0,1.30,0),(0,1.61,0),'Spine')
 for side,s in [('L',-1),('R',1)]:
  bone(m,'Arm.'+side,(s*.27,1.20,0),(s*.39,.70,0),'Spine');bone(m,'Hand.'+side,(s*.39,.70,0),(s*.40,.54,-.03),'Arm.'+side);bone(m,'Leg.'+side,(s*.17,.66,0),(s*.17,.10,-.03),'Spine')
 for n,b,p in [('Equipment.Head','Head',(0,1.68,0)),('Equipment.Hand.L','Hand.L',(-.40,.57,0)),('Equipment.Hand.R','Hand.R',(.40,.57,0)),('Equipment.Back','Spine',(0,1.09,.23))]:m['sockets'].append(dict(name=n,bone=b,position=list(p)))
 profiles={'SkeletalSentry':(17,19,.37),'StoneGolem':(11,12,.53),'ObsidianBrute':(25,26,.67),'AncientGuardian':(17,21,.58),'VaultSentinel':(12,10,.38),'BrassHusk':(21,27,.40),'IceWight':(28,17,.36),'CharredHusk':(25,24,.35),'SleepingTroll':(6,8,.65),'SporeShambler':(19,29,.43)}
 col,accent,width=profiles[bp]
 if bp=='SkeletalSentry':
  box(m,'spinal-column',(0,1.02,.035),(.10,.64,.10),17,'Spine');box(m,'bone-pelvis',(0,.67,0),(.34,.13,.20),17,'Spine')
  for side,s in [('L',-1),('R',1)]:
   for i in range(4):box(m,'separated-rib-'+side+str(i),(s*.11,1.18-i*.12,-.015),(.18,.052,.17),17,'Spine')
 else:
  box(m,'torso',(0,1.02,0),(width,.58,.30),col,'Spine');box(m,'pelvis',(0,.70,0),(width*.82,.20,.28),accent,'Spine')
 box(m,'head',(0,1.47,-.012),(.29,.29,.265),col,'Head');box(m,'jaw',(0,1.335,-.055),(.23,.09,.205),accent,'Head')
 for side,s in [('L',-1),('R',1)]:
  box(m,'upper-arm-'+side,(s*.30,1.10,0),(.12 if bp=='SkeletalSentry' else .19,.34,.19),col,'Arm.'+side);box(m,'forearm-'+side,(s*.39,.81,0),(.09 if bp=='SkeletalSentry' else .15,.29,.155),accent,'Arm.'+side);box(m,'hand-'+side,(s*.40,.59,-.025),(.145,.18,.17),col,'Hand.'+side);box(m,'leg-'+side,(s*.17,.37,0),(.11 if bp=='SkeletalSentry' else .19,.48,.21),col,'Leg.'+side);box(m,'foot-'+side,(s*.17,.085,-.065),(.20,.17,.32),accent,'Leg.'+side)
 eyes(m,.082,1.505,-.152,.030,27 if bp not in ['IceWight','CharredHusk']else 28 if bp=='IceWight'else 24)
 if bp=='SkeletalSentry':
  for s in [-1,1]:box(m,'deep-eye-socket',(s*.082,1.505,-.150),(.083,.088,.026),25,'Head')
 elif bp=='StoneGolem':
  for i,(x,y,z) in enumerate([(-.22,1.19,0),(.22,1.17,0),(-.16,.88,-.17),(.14,.95,-.17)]):box(m,'stone-block-'+str(i),(x,y,z),(.23,.22,.22),12 if i%2 else 10,'Spine')
 elif bp=='ObsidianBrute':
  for s in [-1,1]:box(m,'obsidian-slab',(s*.36,1.30,.03),(.29,.37,.35),26,'Spine');box(m,'obsidian-edge',(s*.36,1.50,.03),(.16,.065,.27),12,'Spine')
 elif bp=='AncientGuardian':
  for s in [-1,1]:
   for i in range(3):box(m,'shoulder-tower-'+str(s)+str(i),(s*.31,1.25+i*.13,.04),(.22-i*.026,.10,.27-i*.026),21 if i%2 else 17,'Spine')
  box(m,'stone-inset',(0,1.02,-.169),(.18,.28,.030),11,'Spine')
 elif bp=='VaultSentinel':
  box(m,'tall-shell',(0,1.23,.115),(.39,.58,.22),11,'Spine');box(m,'inset-face',(0,1.46,-.155),(.17,.19,.025),25,'Head');box(m,'face-slit',(0,1.48,-.174),(.14,.035,.02),22,'Head')
 elif bp=='BrassHusk':
  for i in range(4):box(m,'brass-plate-'+str(i),(0,.83+i*.115,-.176),(.35,.086,.05),27 if i%2 else 22,'Spine')
  for s in [-1,1]:box(m,'brass-cheek',(s*.11,1.45,-.16),(.065,.15,.04),27,'Head')
 elif bp=='IceWight':
  for i in range(4):box(m,'ice-ridge-'+str(i),((-.11 if i%2 else .13),1.24+i*.10,.085),(.085,.24,.10),17,'Spine'if i<2 else'Head')
 elif bp=='CharredHusk':
  for i in range(5):box(m,'ember-seam-'+str(i),((i%2-.5)*.17,.82+i*.10,-.170),(.055,.16,.025),24 if i%2 else 27,'Spine')
 elif bp=='SleepingTroll':
  box(m,'slumped-belly',(0,.89,-.14),(.62,.52,.33),8,'Spine');box(m,'broad-nose',(0,1.43,-.185),(.19,.13,.12),6,'Head')
  for s in [-1,1]:box(m,'troll-ear',(s*.20,1.48,0),(.14,.10,.16),6,'Head');box(m,'heavy-forearm',(s*.40,.79,0),(.24,.38,.24),6,'Arm.'+('L'if s<0 else'R'))
 elif bp=='SporeShambler':
  box(m,'hunched-back',(0,1.17,.17),(.45,.42,.24),10,'Spine');box(m,'uneven-cap',(0,1.66,0),(.47,.13,.39),29,'Head')
  for i,(x,y,z) in enumerate([(-.20,1.23,.17),(.23,1.10,.16),(-.17,.92,.18),(.175,1.38,.11),(.10,1.69,-.03)]):box(m,'spore-sac-'+str(i),(x,y,z),(.13,.14,.13),17 if i%2 else 29,'Head'if y>1.30 else'Spine')
 return m
def build():
 models=[bat(),slime()]+[quadruped(x)for x in ['CaveBear','PaleStalker','DesertProwler','BrittleHound','JungleStalker']]+[scorpion('Scorpion'),scorpion('GlassScorpion'),worm(),dune(),plant()]+[humanoid(x)for x in ['SkeletalSentry','StoneGolem','ObsidianBrute','AncientGuardian','VaultSentinel','BrassHusk','IceWight','CharredHusk','SleepingTroll','SporeShambler']]+[frog(x)for x in ['Reedfrog','Bandfrog','GinFrog','SummitSinger']]+[lizard(x)for x in ['SunStriker','BrocchiniaSentinel','PrickleBrowGecko']]
 return dict(schemaVersion=1,coordinates='Unity X east,Y height,Z north; one unit per native cell',palette=PALETTE,models=models)
if __name__=='__main__':Path(__file__).with_name('visitors.json').write_text(json.dumps(build(),indent=2)+'\n')
