"""Original cuboid botany: 35 authored structures, three stages, real seed/harvest forms.
No downloaded geometry. A deterministic editable source, not runtime generation.
"""
import argparse,json,math
from pathlib import Path
PALETTE=['#082C28','#103E36','#1A4B40','#26594A','#A0A77C','#CBC697','#647353','#235D25','#40872C','#65AE3D','#435A53','#62786C','#819489','#16883B','#45CB4B','#A0E772','#207838','#D2D3B4','#B77B43','#403D28','#756C40','#A39456','#C4B877','#243E39']
# Each function is a recognizable botanical structure, shared only by its own
# growing/ripe/harvest representations. No whole-plant palette-swap species.
def botanical(name,box,ripe=True):
 def b(x,y,z,w,h,d,c):box(x,y,z,w,h,d,c)
 def stem(x,z,h,c=7,w=.035):b(x,h/2+.035,z,w,h,w,c)
 def leaf(x,y,z,w=.18,d=.08,c=8):b(x,y,z,w,.033,d,c)
 def pod(x,y,z,w=.14,h=.18,c=18):b(x,y,z,w,h,w*.83,c);b(x-.012,y+h*.44,z,w*.7,h*.20,w*.64,22)
 def cap(x,y,z,w=.23,c=17):b(x,y,z,w,.08,w*.85,c);b(x,y+.05,z,w*.64,.055,w*.56,5)
 def ring(x,y,z,w=.25,h=.22,c=17):
  b(x-w/2,y,z,.04,h,.05,c);b(x+w/2,y,z,.04,h,.05,c);b(x,y+h/2,z,w,.04,.05,c);b(x,y-h/2,z,w,.04,.05,c)
 def fan(x,y,z,n=5,c=8):
  for i in range(n):
   a=(i-(n-1)/2);b(x+a*.055,y+abs(a)*.025,z+abs(a)*.018,.06,.07+.04*abs(a),.25-abs(a)*.04,c if i%2 else 9)
 def rosette(n=5,c=8,y=.095,r=.13):
  for i in range(n):
   a=i*math.tau/n;x=math.cos(a)*r;z=math.sin(a)*r
   b(x,y+i*.004,z,.11+abs(x)*.65,.04,.10+abs(z)*.65,c if i%2 else 9)
 if name=='Claspbean':
  for x in [-.10,.10]:stem(x,0,.24);pod(x,.30,0,.145,.22,8 if ripe else 7);b(x,.365,-.015,.065,.07,.09,5)
  ring(0,.24,.035,.35,.25,4);leaf(0,.12,.13,.28,.095)
 elif name=='Pitchpod':
  stem(0,0,.39,20);b(0,.37,0,.32,.035,.05,20)
  for x,y,z in [(-.14,.41,0),(.13,.48,.04),(0,.61,-.07)]:stem(x,z,y,7);pod(x,y,z,.12,.17,18);b(x,y-.08,z,.04,.07,.045,21)
 elif name=='Drawgourd':
  ring(-.12,.08,0,.27,.09,7);pod(.12,.20,.05,.22,.26,22);b(.12,.395,.05,.072,.20,.07,5);b(.12,.50,.05,.105,.025,.10,19);leaf(-.15,.105,.16,.25,.14)
 elif name=='Wickrush':
  for i in range(5):
   x=(i-2)*.055;z=(i%2)*.09;h=.35+(i%3)*.1;stem(x,z,h,7,.028);b(x,h+.08,z,.047,.18,.047,5 if ripe else 8);leaf(x+.045,h*.46,z,.12,.027)
 elif name=='Marlroot':
  for i,(x,z) in enumerate([(-.14,-.09),(.03,-.16),(.16,-.03),(.08,.13),(-.10,.11)]):pod(x,.11,z,.11,.13,18);b(x,.055,z+.025,.05,.09,.09,20)
  rosette(6,2,.23,.12);b(0,.25,0,.08,.08,.08,4)
 elif name=='Sumpsieve':
  pod(0,.10,0,.23,.15,19)
  for x,z,h in [(-.14,0,.24),(.12,.06,.28),(0,-.13,.31)]:
   stem(x,z,h,3);ring(x,h,z,.17,.025,8);b(x,h,z+.06,.17,.035,.04,9);b(x,h,z-.06,.17,.035,.04,9)
 elif name=='Drowsebell':
  for x,z,h in [(-.13,.04,.45),(.10,-.05,.57),(0,.13,.32)]:
   stem(x,z,h,3);b(x+.06,h,z,.15,.035,.035,6);pod(x+.11,h-.09,z,.115,.19,12);b(x+.11,h-.19,z,.085,.025,.082,0)
 elif name=='Chillcress':
  stem(0,0,.13,12)
  for i in range(5):
   x=(i-2)*.075;y=.10+abs(i-2)*.025;leaf(x,y,0,.065,.30-abs(i-2)*.035,12);b(x,y+.03,-.12,.035,.06,.048,17);b(x+.025,y+.06,-.11,.07,.027,.032,13)
 elif name=='Slipsedge':
  for i in range(5):
   x=(i-2)*.062;z=(i%2)*.065;h=.29+(i%3)*.07;stem(x,z,h,8,.024);b(x+.045,h,z,.13,.027,.06,9);b(x+.099,h-.029,z,.06,.068,.05,12 if ripe else 8)
 elif name=='Peatlantern':
  for x,z,h in [(-.13,-.02,.19),(.10,.07,.31),(.05,-.12,.23)]:
   stem(x,z,h,19,.045);b(x,h,z,.18,.12,.16,18);b(x,h+.075,z,.22,.045,.20,22);b(x,h+.10,z,.12,.02,.10,19);b(x-.065,h+.05,z,.025,.09,.17,5)
 elif name=='Sunbladder':
  rosette(6,6,.08,.13);pod(0,.22,0,.27,.32,12)
  for x in [-.09,0,.09]:b(x,.23,-.115,.022,.26,.04,4)
  b(0,.40,0,.08,.06,.075,17)
 elif name=='Shalebean':
  for i in range(4):
   x=(i-1.5)*.09;leaf(x,.08+abs(x)*.3,0,.11,.26,11)
  stem(.035,0,.31,20)
  for j in range(3):pod(.035,.22+j*.12,0,.15-j*.02,.08,22)
 elif name=='Shadefan':
  b(-.055,.14,0,.065,.27,.07,20);b(.015,.28,0,.18,.05,.07,21)
  for i in range(7):
   x=(i-3)*.07;h=.26-abs(i-3)*.022;b(x,.37,0,.075,h,.09,6 if i%2 else 4);b(x,.37,-.052,.025,h,.018,17)
 elif name=='Cinderpea':
  for side in [-1,1]:
   stem(side*.13,0,.28,19);b(side*.13,.32,0,.20,.055,.11,19);b(side*.205,.38,0,.055,.13,.11,19);b(side*.13,.295,-.06,.17,.024,.025,18);b(side*.215,.39,-.06,.025,.11,.025,18)
  leaf(0,.12,.06,.29,.11,20)
 elif name=='Spurgrass':
  rosette(7,6,.08,.10)
  for i,(x,z) in enumerate([(-.12,-.08),(.10,-.08),(0,.12),(-.14,.10),(.12,.09)]):
   h=.33+(i%2)*.14;stem(x,z,h,4,.025);b(x,h+.09,z,.028,.18,.029,17);b(x,h+.19,z,.015,.045,.017,5)
 elif name=='Choirwick':
  stem(0,0,.37,17,.065);b(0,.31,0,.33,.06,.065,4)
  for x,z,h in [(-.14,0,.52),(.13,.03,.45),(0,-.10,.63)]:stem(x,z,h,17,.042);b(x,h+.045,z,.075,.13,.07,12);b(x,h+.12,z,.043,.035,.041,15)
 elif name=='Knitmoss':
  for j in range(3):
   for i in range(3):
    x=(i-1)*.12;z=(j-1)*.12;y=.08+((i+j)%2)*.04;b(x,y,z,.145,.10,.13,4);b(x,y+.055,z,.14,.025,.045,17);b(x+.034,y+.02,z,.029,.10,.125,8)
 elif name=='Sourmantle':
  stem(0,0,.48,17,.05)
  for j in range(3):
   y=.17+j*.14;w=.33-j*.07;b(0,y,0,w,.046,w*.8,9);b(-w*.39,y+.025,0,.065,.08,w*.75,4);b(w*.39,y-.025,0,.065,.08,w*.75,13)
 elif name=='Murmurpod':
  for x,z,h in [(-.13,.02,.30),(.12,.05,.37),(0,-.14,.46)]:
   stem(x,z,h,17,.033);pod(x,h,z,.15,.22,12);ring(x,h,z,.20,.29,4)
 elif name=='Sealbark':
  stem(0,0,.33,20,.11)
  for i in range(4):
   x=(-.12 if i%2==0 else .12);z=(i//2-.5)*.15;y=.16+(i%2)*.1;b(x,y,z,.13,.24,.16,21);b(x,y+.13,z,.17,.035,.16,4);b(x*1.35,y+.09,z,.035,.10,.16,22)
 elif name=='Absentmint':
  stem(.025,0,.34,11,.025)
  for x,y,z in [(-.06,.12,0),(.095,.12,0),(.095,.25,0)]:leaf(x,y,z,.13,.075,12);b(x,y+.02,z,.07,.009,.02,17)
  b(.025,.35,0,.045,.045,.04,4)
 elif name=='Margincress':
  for i in range(5):
   x=(i-2)*.065;z=abs(i-2)*.035;leaf(x,.075+i*.004,z,.08,.22,12);b(x,.096+i*.004,z-.1,.08,.025,.025,4);b(x,.105+i*.004,z+.08,.03,.04,.055,17)
 elif name=='Binderroot':
  ring(0,.18,0,.26,.24,17);b(-.17,.07,0,.17,.05,.07,4);b(.17,.07,0,.17,.05,.07,4)
  for x,z in [(-.1,.10),(.08,.08)]:stem(x,z,.24,2,.025);leaf(x+.04,.25,z,.10,.065,3)
 elif name=='Greybladder':
  pod(0,.22,0,.19,.31,12);b(.03,.405,0,.075,.10,.05,17);b(.07,.455,0,.11,.025,.065,11);leaf(-.15,.10,.02,.17,.08,12);leaf(.13,.12,.04,.12,.065,4)
 elif name=='Hollowchime':
  stem(0,0,.52,11,.04);b(0,.49,0,.37,.032,.045,12)
  for i in range(3):
   x=(i-1)*.145;y=.30+(i%2)*.06;stem(x,0,y+.15,12,.018);ring(x,y,0,.105,.19,12);b(x,y,0,.036,.15,.025,22)
 elif name=='Raingourd':
  rosette(5,6,.08,.17);b(0,.14,0,.25,.08,.25,18)
  for x,z,w,d in [(0,-.13,.30,.045),(0,.13,.30,.045),(-.13,0,.045,.24),(.13,0,.045,.24)]:b(x,.245,z,w,.17,d,4)
  b(0,.19,0,.20,.012,.20,19)
 elif name=='Prismreed':
  for i in range(4):
   x=(i-1.5)*.08;z=(i%2)*.075;h=.40+i*.08;stem(x,z,h,3,.028)
   for j in range(3):b(x,h*(j+1)/4,z,.044,.025,.044,12)
   b(x,h+.065,z,.075,.16,.06,13);b(x-.012,h+.075,z-.032,.022,.15,.013,17)
 elif name=='ScarletSundew':
  for i in range(6):
   a=i*math.tau/6;x=math.cos(a)*.18;z=math.sin(a)*.18;y=.095+(i%2)*.025;leaf(x,y,z,.16,.11,18);b(x,y+.035,z,.10,.025,.055,21);b(x,y+.072,z,.04,.05,.035,17)
  b(0,.065,0,.11,.07,.11,8)
 elif name=='Cloudwick':
  for x,z,h in [(-.13,0,.40),(.13,.04,.52),(0,-.11,.63)]:stem(x,z,h,2);b(x,h+.06,z,.08,.16,.08,17);b(x,h+.15,z,.05,.045,.05,5);leaf(x+.065,.20,z,.17,.04,3)
 elif name=='Gripfrond':
  ring(0,.10,0,.24,.10,11)
  for side in [-1,1]:
   for j in range(3):
    x=side*(.10+j*.055);y=.19+j*.10;z=j*.025;b(x,y,z,.12,.09,.13,12);b(x+side*.05,y+.045,z,.035,.09,.13,4)
   b(side*.15,.46,.05,.18,.035,.12,18);b(side*.075,.425,.05,.035,.10,.12,12)
 elif name=='Lampvein':
  for i in range(6):
   x=(i-2.5)*.06;leaf(x,.07+(i%2)*.014,0,.065,.35-abs(i-2.5)*.04,3)
  for z in [-.08,.05]:b(0,.10,z,.36,.021,.025,17);b(-.15,.13,z,.045,.06,.045,15)
 elif name=='Knucklecap':
  for i,(x,z) in enumerate([(-.13,-.09),(.04,-.15),(.15,0),(.035,.13),(-.13,.08)]):
   h=.14+(i%2)*.045;stem(x,z,h,20,.065);cap(x,h,z,.16,21);b(x-.024,h+.088,z,.05,.06,.09,22)
 elif name=='Sootroot':
  pod(0,.105,0,.25,.17,19)
  for i,(x,z) in enumerate([(-.1,0),(.07,.08),(.065,-.07)]):stem(x,z,.26+i*.045,12,.025);b(x+.03,.275+i*.045,z,.085,.025,.03,17);b(x+.06,.25+i*.045,z,.023,.08,.03,4)
 elif name=='Veilpuff':
  pod(0,.24,0,.30,.34,17);b(0,.425,0,.16,.05,.15,5)
  for i in range(4):
   x=(i-1.5)*.067;b(x,.16,-.13,.038,.27,.035,3);b(x,.305,-.105,.038,.04,.085,12)
 elif name=='Brinebutton':
  b(0,.065,0,.36,.08,.30,18)
  for x,z,h in [(-.12,-.08,.14),(.09,-.10,.20),(.12,.10,.15),(-.10,.08,.18)]:
   stem(x,z,h,4,.035);cap(x,h,z,.11,17);b(x,h+.085,z,.028,.01,.03,19)
 else:raise ValueError(name)

def build():
 roster=json.loads((Path(__file__).parent/'roster.json').read_text());species=[];models=[]
 def model(mid):
  boxes=[]
  def box(x,y,z,w,h,d,c):boxes.append({'center':dict(zip('xyz',[round(x,5),round(y,5),round(z,5)])),'size':dict(zip('xyz',[round(w,5),round(h,5),round(d,5)])),'color':c})
  models.append({'id':mid,'kind':'entity','boxes':boxes});return box
 for index,s in enumerate(roster['Species']):
  name=s['Stem'];stem='scarlet-sundew' if name=='ScarletSundew' else name.lower();prefix='biome-crop-'+stem
  species.append({'name':name,'stem':stem,'biome':s['Biome'],'seedBlueprint':s['SeedBlueprint'],'cropBlueprint':s['CropBlueprint'],'harvestBlueprint':s['HarvestBlueprint']})
  for stage in range(3):
   for wet in [False,True]:
    box=model(prefix+f'-{stage}-'+('wet' if wet else 'dry'))
    # Native cultivated ground supplies the furrow overlay. A crop contributes
    # only a few damp/dry soil crumbs, so wild pockets do not become plank beds.
    dry,moist={'Spread':(20,19),'Sodden':(19,0),'Beating':(21,20),'Grovelands':(6,19),'Overwrit':(11,10),'Stump':(11,23),'Cave':(10,23)}[s['Biome']]
    for x,z,w,d in [(-.21,-.13,.16,.095),(.17,.095,.15,.08),(.025,-.19,.10,.065)]:box(x,.02,z,w,.035,d,moist if wet else dry)
    if stage==0:
     for j in range(3+index%3):
      x=-.20+j*.085;z=((index+j)%3-1)*.14;box(x,.065,z,.025+(index%5)*.007,.027+(index%3)*.005,.025+(index%7)*.008,4 if j%2 else 22)
     continue
    # Sprouts retain species architecture at half size with unopened organs.
    # Mature systems show the full distinctive authored seed/leaf/pod structure.
    scale=.54 if stage==1 else 1
    def scaled(x,y,z,w,h,d,c):box(x*scale,y*scale+.025,z*scale,w*scale,h*scale,d*scale,7 if stage==1 and c in [18,22,15] else c)
    botanical(name,scaled,stage==2)
  box=model(prefix+'-seed')
  # Dry loose seeds, their ridges and arrangement vary by physical plant form.
  for j in range(3+index%4):
   x=(j%3-1)*.08;z=(j//3-.5)*.105;y=.055+(j%2)*.019
   w=.032+(index%5)*.012;d=.037+(index%7)*.008;h=.045+(index%3)*.017
   box(x,y,z,w,h,d,22 if index%2 else 4);box(x,y+h*.55,z,w*.43,.012,d*.76,17 if index%3 else 18)
  box=model(prefix+'-harvest')
  # The actual harvested material remains recognizable from its parent plant.
  # Turn botanical growth sideways into a compact cut bundle; for containers,
  # lamps and equipped plant parts, retain the upright organ and broad mouth.
  upright=s['UtilityKind'] in ['WaterVessel','LiquidVessel','Light','Torch','HeadCover','Handwear']
  def cut(x,y,z,w,h,d,c):
   scale=.72
   if upright:box(x*scale,y*scale+.025,z*scale,w*scale,h*scale,d*scale,c)
   else:box(x*scale,.09+z*scale*.42,(y-.25)*scale,w*scale,d*scale*.42,h*scale,c)
  botanical(name,cut)
 return {'schemaVersion':1,'id':'biome-crops-original','palette':PALETTE,'species':species,'models':models}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=Path(__file__).with_name('kit.json'));a=p.parse_args();a.output.write_text(json.dumps(build(),indent=2)+'\n')
