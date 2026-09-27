"""Original biome source cuboids, no Unity or borrowed-asset writes."""
import argparse,json,random
from pathlib import Path
FAMILIES=['paving','road','tree','hedge','vine-wall','stubble','grain','flowers','dry-brush','rock']
PALETTE=['#082C28','#103E36','#1A4B40','#26594A','#A0A77C','#CBC697','#647353','#235D25','#40872C','#65AE3D','#435A53','#62786C','#819489','#16883B','#45CB4B','#A0E772','#207838','#D2D3B4','#B77B43','#403D28','#756C40','#A39456','#C4B877','#243E39']
def make(f,v):
 rng=random.Random(262701+FAMILIES.index(f)*197+v*41);boxes=[]
 def box(x,y,z,w,h,d,c):boxes.append({'center':dict(zip('xyz',[round(x,5),round(y,5),round(z,5)])),'size':dict(zip('xyz',[round(w,5),round(h,5),round(d,5)])),'color':c})
 if f in ('paving','road'):
  box(0,-.018,0,1,.032,1,1 if f=='road' else 10)
  if f=='paving':
   for row in range(3):
    for col in range(3):
     x=(col-1)*.331;z=(row-1)*.331;h=.023+((row+col+v)%3)*.006
     box(x,h/2,z,.312,h,.311,10+(col+row+v)%3)
     if (row+col+v)%2==0:box(x+.057,h+.004,z-.052,.084,.008,.07,11)
  else:
   for i in range(19):
    x=rng.uniform(-.43,.43);z=rng.uniform(-.43,.43);w=rng.uniform(.056,.105);h=rng.uniform(.014,.038)
    box(x,h/2,z,w,h,w*.78,10 if i%4==0 else 2+(i+v)%2)
 elif f=='tree':
  # A forked pale trunk and separated angular leaf fans, with daylight gaps.
  trunk=.155+.008*v;box(0,.42,0,trunk,.84,.17,4);box(.014,.80,-.007,.128,.37,.134,5)
  for i,(x,z) in enumerate([(-.31,-.12),(.29,-.17),(-.20,.27),(.25,.24)]):
   side=-1 if x<0 else 1;h=.77+((i+v)%4)*.105
   box(x*.43,h-.1,z*.4,abs(x)*.85,.105,.095,4)
   box(x*.75,h,z*.75,.105,.22,.10,4)
   box(x,h+.21,z,.10,.24,.105,5)
   for leaf in range(3):
    lx=x+side*(leaf-1)*.053;lz=z+(leaf-1)*.035;lh=h+.39-abs(leaf-1)*.055
    box(lx,lh,lz,.145,.105,.155,6 if leaf==1 else 4)
    box(lx,lh+.065,lz,.119,.04,.12,5)
  box(-.025,1.34+.035*v,.023,.12,.27,.12,4);box(-.025,1.51+.035*v,.023,.19,.071,.19,5)
  for x,z in [(-.105,.02),(.085,.07),(0,-.105)]:box(x,.058,z,.15,.116,.125,4)
 elif f=='hedge':
  for i in range(8):
   x=(i-3.5)*.122;z=(-1 if i%2 else 1)*(.10+.02*(v%2));h=.42+((i+v)%4)*.055
   box(x,.17,z,.09,.34,.105,6)
   for j in range(3):box(x,h*(.45+j*.21),z+(j-1)*.044,.112,.15,.19,7+(i+j+v)%3)
   box(x,h+.033,z,.10,.075,.145,9)
 elif f=='vine-wall':
  for i in range(7):
   x=(i-3)*.14;h=.77+((i+v)%4)*.062
   box(x,h/2,0,.075,h,.14,6);box(x+.02,h+.023,0,.09,.046,.15,4)
   for j in range(4):
    side=-1 if (i+j+v)%2 else 1
    box(x,j*.19+.12,side*.10,.15,.063,.12,7)
    box(max(-.434,min(.434,x+side*.033)),j*.19+.15,side*.17,.13,.08,.14,8 if j%2 else 9)
  for j in range(3):box(0,.19+j*.24,0,.95,.068,.1,6)
 elif f in ('stubble','grain'):
  for row in range(3):
   for col in range(4):
    x=(col-1.5)*.21+rng.uniform(-.014,.014);z=(row-1)*.28+rng.uniform(-.015,.015)
    h=.069+((row+col+v)%4)*.015 if f=='stubble' else .28+((col+row+v)%4)*.04
    box(x,h/2,z,.05,h,.05,6 if f=='stubble' else 7)
    if f=='grain':
     box(x-.035,h*.49,z,.105,.031,.063,8);box(x+.032,h*.72,z,.099,.033,.06,9)
     box(x,h+.053,z,.075,.106,.065,21);box(x,h+.119,z,.061,.035,.054,22)
    else:box(x+.023,h*.45,z,.076,.031,.045,4)
 elif f=='flowers':
  for i,(x,z) in enumerate([(-.31,-.22),(-.04,-.29),(.25,-.2),(-.23,.09),(.08,.01),(.31,.24),(-.09,.28)]):
   h=.16+((i+v)%4)*.037
   box(x,h/2,z,.038,h,.04,7);box(x-.031,h*.4,z,.109,.029,.055,8);box(x+.032,h*.64,z,.1,.026,.061,9)
   box(x,h+.034,z,.102,.053,.105,17);box(x,h+.065,z,.061,.023,.06,5)
 elif f=='dry-brush':
  for i in range(7):
   x=(i%3-1)*.20+rng.uniform(-.035,.035);z=(i//3-1)*.2;h=.22+((i+v)%4)*.062
   box(x,h/2,z,.047,h,.049,20);box(x+.045,h*.59,z,.12,.045,.055,21);box(x+.075,h*.59+.075,z,.045,.15,.049,22)
 elif f=='rock':
  for i,(x,z) in enumerate([(-.23,-.16),(.14,-.22),(-.15,.18),(.22,.17),(0,0)]):
   h=.14+((i+v)%4)*.055;w=.24+((i+v)%2)*.04
   box(x,h/2,z,w,h,w*.9,10);box(x-.014,h+.024,z-.013,w-.043,.048,w*.9-.04,11);box(x-.03,h+.054,z-.028,w*.37,.012,w*.35,12)
 return {'id':'spread-environment-'+f+'-'+str(v),'family':f,'variant':v,'kind':'ground'if f in ('paving','road')else'entity','boxes':boxes}
def build():return {'schemaVersion':1,'id':'spread-environment-original','palette':PALETTE,'models':[make(f,v)for f in FAMILIES for v in range(4)]}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(build(),indent=2)+'\n')
