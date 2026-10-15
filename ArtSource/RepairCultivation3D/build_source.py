"""Original editable cuboid models. No downloaded geometry; native glade palette."""
import argparse,json
from pathlib import Path
PALETTE=['#082C28','#103E36','#1A4B40','#26594A','#A0A77C','#CBC697','#647353','#235D25','#40872C','#65AE3D','#435A53','#62786C','#819489','#16883B','#45CB4B','#A0E772','#207838','#D2D3B4','#B77B43','#403D28','#756C40','#A39456','#C4B877','#243E39']
models=[]
def shape(name):
 b=[]
 def box(x,y,z,w,h,d,c):b.append({'center':dict(zip('xyz',[round(x,5),round(y,5),round(z,5)])),'size':dict(zip('xyz',[round(w,5),round(h,5),round(d,5)])),'color':c})
 models.append({'id':'repair-cultivation-'+name,'kind':'entity','boxes':b});return box

def crop(species,stage,wet):
 box=shape(f'{species}-{stage}-'+('wet' if wet else 'dry'));soil=19 if wet else 20
 for z in (-.24,0,.24):box(0,.035,z,.70,.07,.075,soil)
 for i,(x,z) in enumerate([(-.23,-.21),(.22,-.17),(-.18,.21),(.23,.20)]):
  if stage==0:
   w,d=(.025,.10) if species=='knotflax' else (.075,.075) if species=='hearthbulb' else (.08,.035)
   box(x,.085,z,w,.03,d,22 if species=='knotflax' else 18 if species=='hearthbulb' else 17)
   box(x+.025,.081,z+.02,.026,.025,.029,4)
  elif species=='knotflax':
   h=.28 if stage==1 else .66+(i%2)*.07
   box(x,h/2+.05,z,.03,h,.034,7);box(x+.05,h*.51,z,.13,.03,.044,8);box(x-.045,h*.72,z,.12,.029,.043,9)
   if stage==2:
    box(x,h+.075,z,.095,.10,.08,17);box(x-.035,h+.13,z,.10,.036,.045,12);box(x+.027,h+.10,z+.04,.038,.067,.047,5)
    box(x+.042,h*.48,z,.026,h*.67,.03,4)
    # Fiber tassels hang below the pale knots: the mature repair crop is legible at world scale.
    for offset,length in [(-.055,.22),(.045,.18)]:
     box(x+offset,h-.025,z-.045,.025,length,.027,5)
    box(x,h+.075,z,.12,.065,.115,12);box(x-.017,h+.12,z-.012,.075,.035,.08,17)
  elif species=='hearthbulb':
   h=.10 if stage==1 else .25
   if stage==2:
    box(x,.14,z,.245,.20,.235,18);box(x,.24,z,.19,.095,.18,22);box(x-.015,.293,z,.11,.03,.105,5)
    box(x-.067,.135,z-.025,.07,.13,.21,20);box(x+.067,.17,z+.02,.065,.13,.21,18)
    for side in (-1,1):
     box(x+side*.055,.40,z,.043,.23,.045,8 if side<0 else 9)
     box(x+side*.075,.51,z,.08,.055,.047,9)
   box(x,.11+h,z,.04,.15,.04,7)
   box(x-.065,.10+h,z,.15,.04,.065,8);box(x+.064,.14+h,z,.14,.05,.06,9)
   box(x,.18+h,z+.055,.05,.04,.14,8)
  else:
   h=.17 if stage==1 else .34
   box(x,h*.45,z,.04,h,.04,7)
   for j,side in enumerate([-1,1]):
    box(x+side*.052,h*.65+j*.063,z,.17,.04,.115,8 if j==0 else 9)
    box(x+side*.07,h*.65+j*.063+.024,z,.115,.012,.032,17)
   if stage==2:
    box(x-.014,.38,z-.027,.075,.052,.155,13);box(x-.014,.414,z-.031,.025,.017,.115,17)
    box(x+.05,.245,z+.06,.13,.033,.085,8)
    # Upright central leaf with a pale seam and real cross stitches.
    box(x,.405,z-.05,.18,.23,.045,8);box(x,.405,z-.077,.018,.215,.012,17)
    for stitch in range(3):box(x,.34+stitch*.065,z-.085,.067,.017,.018,17)
    box(x-.025,.53,z-.05,.075,.035,.055,5)
for f in ('knotflax','hearthbulb','seamleaf'):
 for stage in range(3):
  for wet in (False,True):crop(f,stage,wet)
# Rounded square masonry, open central shaft, readable distinct fault.
for f in ('lined-well','rope-well'):
 for repaired in (False,True):
  box=shape(f+'-'+('restored' if repaired else 'broken'))
  box(0,.025,0,.74,.05,.74,10);box(0,.07,0,.43,.035,.43,0)
  for side in range(4):
   for i in range(3):
    x=(i-1)*.21;z=.30;h=.43
    if f=='lined-well' and not repaired and side==2 and i==1:h=.17
    if side%2:x,z=z,x
    if side>=2:x,z=-x,-z
    for course in range(3):
     brick_h=min(.13,h-course*.145)
     if brick_h>0:
      box(x,.05+course*.145+brick_h/2,z,.18 if side%2==0 else .19,brick_h,.19 if side%2==0 else .18,10 if course==0 else 11)
    box(x,h+.065,z,.195,.03,.195,12)
    if f=='lined-well' and not (not repaired and side==2 and i==1):
     # Exposed lining remains visible before repair; the missing segment is its fault.
     ix,iz=(.1975 if x>0 else -.1975,z) if side%2 else (x,.1975 if z>0 else -.1975)
     box(ix,.26,iz,.035 if side%2 else .18,.29,.18 if side%2 else .035,18)
  # Complete the square rim; side-center stones alone left four false holes.
  for cx in (-.30,.30):
   for cz in (-.30,.30):
    for course in range(3):box(cx,.115+course*.145,cz,.19,.13,.19,10 if course==0 else 11)
    box(cx,.495,cz,.195,.03,.195,12)
  for x in (-.36,.36):box(x,.54,0,.065,1.00,.07,20);box(x+.01,1.055,0,.075,.05,.095,22)
  box(0,1.0,0,.81,.075,.075,21)
  if f=='rope-well':
   box(.04,.97,0,.10,.15,.11,19)
   for coil in range(4):
    cx=-.08+coil*.065
    for y,z,w,h,d in [(1.055,0,.03,.025,.16),(.945,0,.03,.025,.16),(1.0,-.08,.03,.11,.025),(1.0,.08,.03,.11,.025)]:
     box(cx,y,z,w,h,d,5 if coil%2==0 else 4)
   if repaired:
    box(.04,.655,0,.026,.64,.026,5);box(.04,.27,0,.18,.12,.16,20);box(.04,.34,0,.17,.032,.15,22)
   else:
    box(.04,.87,0,.028,.18,.028,5);box(.07,.777,.013,.075,.025,.035,4)
    box(.35,.07,.28,.18,.12,.16,20);box(.35,.135,.28,.17,.018,.15,22)
  else:
   box(-.05,.065,.045,.22,.02,.22,16 if repaired else 0)
   if not repaired:box(0,.062,-.43,.11,.08,.13,10)
# Posts and leaf move together only in the repaired native Door state.
for state in ('broken','closed','open'):
 box=shape('wooden-gate-'+state)
 for x in (-.435,.435):box(x,.40,0,.09,.80,.09,20);box(x,.817,0,.102,.035,.102,22);box(x,.14,.075,.12,.09,.15,10)
 for y in (.25,.66):
  box(-.372,y,0,.08,.08,.12,11)
  for x in (-.39,-.355):box(x,y,-.068,.025,.027,.018,12)
 def leaf(x,y,z,w,h,d,c):
  if state=='open':x,z=-.37+z,-.37+(x+.37);w,d=d,w
  if state=='broken':x,z=-.36+z,.08+(x+.36)*.46;y=y*.43;h=h*.43;w,d=d,w*.46
  box(x,y,z,w,h,d,c)
 for x in (-.30,.01,.31):leaf(x,.42,0,.065,.60,.065,21)
 for y in (.20,.44,.68):leaf(.0,y,0,.70,.064,.063,20)
 for i in range(5):leaf(-.26+i*.13,.23+i*.085,.038,.15,.065,.025,22)
 if state=='broken':
  box(.02,.055,.18,.19,.045,.09,18)
  box(.15,.037,-.17,.31,.05,.067,21);box(.19,.073,-.10,.23,.036,.051,18)
for f in ('knotflax','hearthbulb','seamleaf'):
 box=shape(f+'-seed');box(0,.085,0,.29,.17,.23,20);box(0,.18,0,.23,.06,.20,4);box(0,.22,0,.19,.025,.15,5)
 for i in range(3):box((i-1)*.06,.241,0,.025 if f=='knotflax' else .044,.02,.065 if f=='knotflax' else .03,22 if f=='knotflax' else 18 if f=='hearthbulb' else 17)
for f in ('knotflax-cord','cord-bundle'):
 box=shape(f);scale=1 if f=='knotflax-cord' else 1.55
 for i in range(3):
  y=.055+i*.065
  for x,z,w,d in [(0,-.15,.37,.065),(0,.15,.37,.065),(-.18,0,.065,.25),(.18,0,.065,.25)]:box(x*scale,y,z*scale,w*scale,.053,d*scale,4 if i%2 else 5)
 box(0,.11,0,.055,.22,.40*scale,19)
for f in ('hearthbulb','roasted-hearthbulb'):
 box=shape(f);roast=f.startswith('roasted');box(0,.11,0,.31,.22,.29,18 if not roast else 19);box(0,.23,0,.23,.10,.23,22 if not roast else 18);box(0,.29,0,.12,.035,.12,5 if not roast else 21)
 box(-.045,.335,0,.07,.075,.10,8 if not roast else 20);box(.065,.307,.01,.12,.035,.065,9 if not roast else 19)
box=shape('seamleaf-sprig');box(0,.075,0,.035,.05,.45,7)
for i in range(3):box((-.065 if i%2==0 else .065),.09,(i-1)*.12,.24,.045,.11,8);box(0,.12,(i-1)*.12,.22,.012,.024,17)
for f in ('salvaged-timber','timber-pile'):
 box=shape(f);count=2 if f=='salvaged-timber' else 5
 for i in range(count):
  x=(i%3-1)*.18;y=.075+(i//3)*.14;z=(i%2-.5)*.08
  box(x,y,z,.135,.14,.69,20);box(x+.025,y+.075,z,.025,.012,.57,22);box(x,y,z-.355,.12,.12,.02,18)
box=shape('clay-bank')
for i,(x,z) in enumerate([(-.25,-.17),(.01,-.20),(.24,-.08),(-.18,.15),(.12,.17)]):
 h=.10+(i%3)*.045;box(x,h/2,z,.27,h,.26,18);box(x-.02,h+.015,z-.02,.21,.03,.20,20);box(x+.065,h+.042,z,.055,.025,.065,22)
# Compact, chipped clay pieces replace FireClay's old crystal silhouette.
# One bounded native model; the large borrowed portable source pack stays intact.
box=shape('fire-clay')
for x,y,z,w,h,d in [(-.105,.07,-.07,.22,.14,.22),(.135,.06,.065,.18,.12,.20),(.005,.205,.045,.22,.23,.22)]:
 box(x,y,z,w,h,d,18)
 box(x-.025,y-h*.36,z-.012,w*.78,h*.22,d*.93,20)
 box(x-.035,y+h*.5+.006,z-.032,w*.50,.012,d*.52,22)
# Offset steps keep the clods irregular rather than manufactured bricks.
box(-.13,.151,-.08,.14,.025,.15,18);box(.052,.312,.055,.12,.016,.16,18)
box(.116,.17,.034,.028,.13,.16,18);box(-.115,.202,.055,.022,.17,.15,20)
for x,y,z in [(-.145,.085,-.183),(-.025,.223,-.068),(.15,.075,-.037),(.045,.281,.158)]:
 box(x,y,z,.025,.026,.009,17)
box(.053,.205,-.069,.014,.085,.011,19);box(-.018,.327,.06,.016,.008,.075,19)
box=shape('cultivated-soil')
# Sparse raised furrows expose the actual native ground between rows.
for z in (-.29,0,.29):
 box(0,.018,z,.81,.036,.084,20)
 for x in (-.28,.0,.28):box(x,.044,z+.004,.15,.018,.061,19)
def build():return {'schemaVersion':1,'id':'repair-cultivation-original','palette':PALETTE,'models':models}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,default=Path(__file__).with_name('kit.json'));args=p.parse_args();args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(build(),indent=2)+'\n')
