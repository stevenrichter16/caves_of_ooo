"""Original fitted clothing: cuboid forms in Unity socket-local coordinates.
No borrowed meshes, global palette modifications or runtime geometry creation.
Humanoid baseline: Body socket .99h, Feet .10h, Head socket1.68h; root scale
follows the actual native actor. Skinning uses one actual equipment bone.
"""
import json
from pathlib import Path
HERE=Path(__file__).parent
models=[]
def form(blueprint,slot):
 r={'id':'spread-worn-'+blueprint.lower(),'blueprint':blueprint,'slot':slot,'boxes':[]};models.append(r);return r['boxes']
def B(boxes,center,size,paint):boxes.append({'center':center,'size':size,'paint':paint})
# Armor is an open-neck vest around the approved .41-.46w/.55h/.27d
# torso. Side strips leave arm openings; no folded ground clothing reuse.
for name,base,edge in [('LeatherArmor',19,20),('ChainMail',10,11),('PlateArmor',11,12),('FineRingMail',11,12),('RivetedPlate',10,11)]:
 b=form(name,'Body')
 for z in [-.166,.166]:B(b,[0,0,z],[.49,.57,.035],base)
 for x in [-.23,.23]:B(b,[x,-.11,0],[.035,.35,.30],base)
 for x in [-.15,.15]:B(b,[x,.27,0],[.15,.035,.35],edge)
 B(b,[0,-.21,-.19],[.50,.055,.025],19)
 B(b,[0,-.21,-.21],[.065,.067,.025],29)
 if name in ('ChainMail','FineRingMail'):
  # Authored sparse contrasting link rows read at game scale, no microscopic mesh rings.
  pitch=.075 if name=='ChainMail' else .06
  for row in range(6):
   for col in range(6):
    B(b,[(col-2.5)*pitch,(row-2.5)*.073,-.188],[.027,.032,.013],edge)
 elif name in ('PlateArmor','RivetedPlate'):
  for y in [-.12,.02,.16]:B(b,[0,y,-.199],[.445,.105,.025],edge)
  for x in [-.185,.185]:
   for y in [-.15,0,.15]:B(b,[x,y,-.221],[.025,.025,.014],12)
 else:
  for y in [-.08,.035,.15]:B(b,[.10,y,-.19],[.09,.025,.016],22)
for name,color,edge in [('LeatherCap',19,20),('IronHelmet',10,12)]:
 b=form(name,'Head');B(b,[0,-.027,0],[.345,.08,.315],color)
 # Open bottom, open face; top and side rim cover the block head without a solid box over the face.
 for x in [-.161,.161]:B(b,[x,-.125,.025],[.025,.19,.30],color)
 B(b,[0,-.12,.162],[.345,.19,.025],color)
 B(b,[0,-.09,-.158],[.345,.035,.032],edge)
 if name=='IronHelmet':B(b,[0,.03,0],[.035,.06,.32],edge)
 else:B(b,[0,-.015,-.19],[.36,.035,.095],edge)
for name,color in [('LeatherBoots',19),('IronshodBoots',10)]:
 b=form(name,'Feet');B(b,[0,-.04,-.025],[.225,.125,.335],color);B(b,[0,-.095,-.025],[.235,.035,.35],23)
 B(b,[0,.055,.025],[.22,.115,.225],color);B(b,[0,.115,.025],[.24,.03,.245],20 if name=='LeatherBoots' else 11)
 for z in [-.02,.04,.1]:B(b,[0,.085,z],[.13,.018,.02],22)
 if name=='IronshodBoots':B(b,[0,-.018,-.19],[.23,.065,.035],12)
b=form('LeatherGloves','Handwear');B(b,[0,.045,0],[.17,.18,.195],19);B(b,[0,.145,0],[.20,.04,.21],20)
B(b,[.105,.015,-.015],[.065,.11,.11],19);B(b,[0,.055,-.11],[.11,.02,.018],22)
for name,color,trim in [('Cloak',6,20),('WardedCloak',3,29)]:
 b=form(name,'Back');B(b,[0,-.19,.065],[.49,.63,.04],color)
 for x in [-.24,.24]:B(b,[x,-.22,.066],[.025,.59,.055],trim)
 B(b,[0,.105,.025],[.42,.055,.07],trim)
 if name=='WardedCloak':
  B(b,[0,-.17,.093],[.025,.24,.017],29);B(b,[0,-.17,.093],[.12,.026,.017],29)
(HERE/'worn-source.json').write_text(json.dumps({'schemaVersion':1,'paletteCells':42,'models':models},indent=2)+'\n')
print('Authored fitted forms:',len(models),'cuboids:',sum(len(m['boxes']) for m in models))
