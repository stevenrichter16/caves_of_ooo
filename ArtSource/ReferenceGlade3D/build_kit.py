"""Original block-based glade art, offline. Writes only an explicit JSON path."""
import argparse,json,random
from pathlib import Path
FAMILIES=['ground','pale-reeds','green-grass','dark-ruin','low-wall','lit-wall','gravel','chest','barrel','mushroom-ring','cooking-fire','cooled-fire','fallen-beam']
PALETTE=['#082C28','#103E36','#1A4B40','#26594A',
         '#A0A77C','#CBC697','#647353',
         '#235D25','#40872C','#65AE3D',
         '#435A53','#62786C','#819489',
         '#16883B','#45CB4B','#A0E772',
         '#207838','#D2D3B4','#B77B43',
         '#403D28','#756C40','#A39456','#C4B877','#243E39']

def make(family,variant):
 rng=random.Random(261026+FAMILIES.index(family)*193+variant*31);boxes=[]
 def box(x,y,z,w,h,d,c):
  boxes.append({'center':dict(zip('xyz',[round(x,5),round(y,5),round(z,5)])),'size':dict(zip('xyz',[round(w,5),round(h,5),round(d,5)])),'color':c})
 if family=='ground':
  box(0,-.017,0,1,.03,1,0)
  for i in range(3+variant%3):
   x=rng.uniform(-.40,.40);z=rng.uniform(-.40,.40);h=rng.choice([.055,.062,.068]);w=rng.choice([.09,.10,.11]);box(x,h*.5,z,w,h,w,2 if i%2 else 3)
 elif family=='pale-reeds':
  # Uneven rectangular reed fingers, with flat block tops. Keep most mass
  # vertical and only a few rising offshoots, never repeated centered cones.
  for i,(px,pz) in enumerate([(-.24,-.22),(.015,-.24),(.25,-.13),(-.28,.025),(-.035,.015),(.20,.16),(-.16,.25)]):
   x=px+rng.uniform(-.025,.025);z=pz+rng.uniform(-.025,.025);h=.43+((i*3+variant)%5)*.046
   w=.124+((i+variant)%3)*.009;d=.13+((i*2+variant)%3)*.008
   box(x,h*.5,z,w,h,d,4)
   box(x,h+.024,z,w+.008,.048,d+.004,5)
   # Short asymmetric branchlets rise alongside two or three selected stems.
   if (i+variant)%3==0:
    side=-1 if (i+variant)%2 else 1
    box(x+side*.077,h*.40,z+.024,.078,.13,.083,4)
    box(x+side*.092,h*.40+.112,z+.024,.083,.104,.086,5)
 elif family=='green-grass':
  # Seven simple unequal rising fingers; no repeated side-leaf ladders.
  for i,(px,pz) in enumerate([(-.22,-.13),(-.04,-.21),(.17,-.16),(-.16,.09),(.04,.015),(.23,.09),(.00,.22)]):
   x=px+rng.uniform(-.009,.009);z=pz+rng.uniform(-.009,.009);h=.25+((i*3+variant)%5)*.032
   box(x,.032,z,.112,.064,.112,7)
   box(x,h*.5,z,.088,h,.092,8 if i%3==0 else 9)
   box(x,h+.025,z,.072,.070,.075,9)
  # Seventh image comparison: the fixed view foreshortens narrow fingers.
  # Broaden only this tuft's X silhouette; keep seven stems and its Z footprint.
  for b in boxes:
   b['center']['x']=round(b['center']['x']*1.75,5);b['size']['x']=round(b['size']['x']*1.5,5)
   b['center']['y']=round(b['center']['y']*.96,5);b['size']['y']=round(b['size']['y']*.96,5)
 elif family=='dark-ruin':
  box(0,.075,0,.92,.15,.55,1)
  for i in range(9):
   x=(i%5-2)*.19;z=(-.14 if i<5 else .13);h=.29+((i+variant)%4)*.11
   box(x,h*.5,z,.095,h,.105,1 if i%2 else 2)
   box(x+.018,h+.033,z,.074,.066,.084,3 if i%3 else 2)
   side=-1 if (i+variant)%2 else 1
   box(x+side*.053,h*.54,z,.11,.063,.085,2)
   box(x+side*.087,h*.54+.065,z,.065,.13,.075,2 if i%2 else 3)
   box(x,.055,z+.04,.14,.11,.18,1)
 elif family in ['low-wall','lit-wall']:
  for course in range(5):
   for i in range(8):
    left=-.496+i*.124+(course%2)*.053;right=min(.496,left+.12)
    if left>=.496:continue
    for column in range(4):
     z=(column-1.5)*.186+rng.uniform(-.004,.004);depth=.177+rng.uniform(-.004,.004)
     h=.121 if course<4 else .097+rng.randrange(5)*.01
     c=rng.choice([10,11,11,11,12]) if family=='low-wall' else rng.choice([1,1,2,2,3])
     box((left+right)*.5,course*.126+h*.5,z,right-left,h,depth,c)
  for i in range(16):
   x=rng.uniform(-.45,.45);y=.05+rng.randrange(5)*.119;side=-1 if i%2 else 1
   box(x,y,side*.376,.047,.048,.023,10+(i+variant)%3 if family=='low-wall' else 1+i%3)
  if family=='lit-wall':
   for side in [-1,1]:
    for i in range(7):
     x=(i-3)*.13;h=.16+((i+variant)%4)*.094
     box(x,h*.5,side*.41,.075,h,.07,13)
     box(x,h+.032,side*.411,.075,.064,.075,14 if i%2 else 15)
   for i in range(5):box((i-2)*.19,.665,-.1,.08,.042,.12,14 if i%2 else 13)
 elif family=='gravel':
  for i in range(27+variant*2):
   x=rng.uniform(-.445,.445);z=rng.uniform(-.445,.445);h=rng.choice([.055,.068,.083]);w=rng.choice([.09,.10,.11])
   box(x,h*.5,z,w,h,w,10 if i%4==0 else 3 if i%2 else 2)
 elif family=='chest':
  box(0,.19,0,.62,.34,.43,19)
  for i in range(7):
   x=(i-3)*.084
   box(x,.20,-.224,.075,.31,.026,20+(i+variant)%2)
   box(x,.20,.224,.075,.31,.026,20+(i+variant+1)%2)
   box(x,.387,0,.076,.062,.42,20+(i+variant)%2)
  for x in [-.24,.24]:
   box(x,.225,0,.045,.43,.47,21)
   box(x,.446,0,.048,.03,.45,22)
  for z in [-.245,.245]:
   box(0,.065,z,.65,.045,.033,21);box(0,.348,z,.65,.046,.033,22)
  box((variant-1.5)*.025,.25,-.261,.095,.12,.034,22);box((variant-1.5)*.025,.253,-.28,.025,.055,.01,19)
  for x in [-.318,.318]:
   for z in [-.17,.17]:box(x,.23,z,.03,.095,.035,22)
 elif family=='barrel':
  box(0,.24,0,.36,.43,.36,19)
  for i in range(5):
   x=(i-2)*.075;depth=.42 if abs(i-2)<2 else .32
   box(x,.24,0,.068,.46,depth,20+(i+variant)%2)
  for z in [-.2,.2]:
   for x in [-.12,0,.12]:box(x,.24,z,.08,.44,.035,20+(int((x+.13)*10)+variant)%2)
  for y in [.08,.37]:
   box(0,y,-.224,.35,.048,.027,22);box(0,y,.224,.35,.048,.027,22)
   box(-.195,y,0,.024,.048,.34,21);box(.195,y,0,.024,.048,.34,21)
  for i in range(5):box((i-2)*.065,.481,0,.06,.024,.31,20+(i+variant)%2)
  box((variant-1.5)*.035,.50,.035,.07,.022,.07,19)
 elif family=='mushroom-ring':
  # Five pale-gold native fungi with separate stems, stepped caps and gills.
  for i,(x,z) in enumerate([(-.23,-.19),(.18,-.21),(.29,.13),(-.07,.25),(-.28,.10)]):
   h=.19+((i+variant)%3)*.045;w=.18+((i+variant)%2)*.035
   box(x,h*.5,z,.068,h,.071,4)
   box(x,h+.016,z,w,.043,w,19)
   box(x,h+.057,z,w+.027,.065,w+.025,20 if i%2 else 21)
   box(x,h+.099,z,w-.034,.024,w-.038,22)
   box(x+.02,h+.117,z-.02,.048,.013,.053,5)
   box(x,h*.19,z,.087,.057,.086,4)
 elif family in ['cooking-fire','cooled-fire']:
  # Separate stones and crossed charred logs stay legible at the native camera.
  # The warm pieces are solid coals: no particles, light or flame simulation.
  for i,(x,z) in enumerate([(-.29,-.29),(0,-.36),(.29,-.29),(.36,0),(.29,.29),(0,.36),(-.29,.29),(-.36,0)]):
   h=.13+((i+variant)%3)*.025
   box(x,h*.5,z,.19,h,.18,10+(i+variant)%3)
   box(x-.018,h+.012,z-.014,.12,.025,.11,12)
  box(0,.14,0,.62,.12,.17,19);box(0,.205,0,.17,.12,.57,19)
  for x in [-.265,.265]:box(x+(variant-1.5)*.006,.145,0,.065,.105,.14,20)
  for z in [-.245,.245]:box(0,.21,z,.145,.10,.06,18)
  for i in range(3):box((i-1)*.058,.269,0,.027,.015,.39,20)
  if family=='cooking-fire':
   for i,(x,z,h) in enumerate([(-.115,-.075,.20),(.045,.035,.34),(.14,-.09,.16)]):
    box(x,.265+h*.5,z,.13,h,.13,18)
    box(x-.012,.27+h*.50,z-.013,.082,h*.69,.085,22)
    box(x-.019,.27+h*.77,z-.019,.043,h*.24,.047,5)
  else:
   for x,z in [(-.12,-.06),(.075,.055),(.13,-.08)]:
    box(x,.28,z,.14,.055,.12,19);box(x-.02,.315,z-.01,.07,.025,.065,10)
 elif family=='fallen-beam':
  # Long exposed faces, broad grain and a charred broken end, within one cell.
  box(0,.145,0,.97,.25,.27,20)
  box(-.433,.15,0,.104,.255,.276,19)
  box(.471,.15,0,.03,.23,.25,22)
  for i,z in enumerate([-.086,0,.086]):
   box(.005+(variant-1.5)*.012,.278,z,.78-i*.07,.018,.024,19)
   box(.03,.289,z+.016,.69-i*.07,.008,.014,21)
  for z in [-.141,.141]:
   box(.025,.11,z,.79,.027,.014,19);box(.06,.21,z,.74,.021,.014,21)
  box(.475,.205,-.055,.034,.025,.065,19)
  box(.475,.105,.051,.034,.031,.063,19)
  box(-.08+variant*.035,.284,.035,.09,.017,.055,19)
 return {'id':'reference-glade-'+family+'-'+str(variant),'family':family,'variant':variant,'boxes':boxes}

def build():return {'schemaVersion':1,'palette':PALETTE,'models':[make(f,v) for f in FAMILIES for v in range(4)]}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);args=p.parse_args();args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(build(),indent=2)+'\n')
