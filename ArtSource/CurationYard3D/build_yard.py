"""Reproducible original intake objects. Unity consumes exact cuboids; no borrowed mesh is edited."""
import json
from pathlib import Path
ROOT=Path(__file__).parent
PALETTE=['#082C28','#103E36','#1A4B40','#26594A','#A0A77C','#CBC697','#647353','#235D25','#40872C','#65AE3D','#435A53','#62786C','#819489','#16883B','#45CB4B','#A0E772','#207838','#D2D3B4','#B77B43','#403D28','#756C40','#A39456','#C4B877','#243E39']
def box(name,c,s,color):return dict(name=name,center=dict(zip('xyz',c)),size=dict(zip('xyz',s)),color=color)
models=[]
def model(name,boxes):models.append(dict(id='curation-yard-'+name,boxes=boxes))
def tag(case,x,y,z):
 out=[box('case-label',(x,y,z),(.22,.07,.18),17)]
 for i in range(case):out.append(box('case-notch-'+str(i),(x-.05+i*.09,y+.043,z),(.035,.02,.12),19))
 return out
for case in [1,2]:
 width=.34 if case==1 else .41
 b=[box('resting-salt', (0,.025,0),(.58,.05,.96),11),box('torso',(0,.18,-.01),(width,.26,.40),17),box('head',(-.025 if case==1 else .035,.21,-.34),(.24,.23,.23),17),box('neck',(0,.17,-.215),(.14,.12,.09),5),box('left-leg',(-.10,.12,.33),(.15,.16,.31),17),box('right-leg',(.10,.12,.33),(.15,.16,.31),17)]
 if case==1:
  b += [box('left-arm',(-.22,.17,-.02),(.11,.18,.40),17),box('right-arm',(.22,.17,-.02),(.11,.18,.40),17),box('closed-eyes',(0,.327,-.37),(.17,.012,.02),11),box('salt-fold',(-.08,.329,-.06),(.14,.04,.25),5)]
 else:
  b += [box('left-arm',(-.20,.20,.02),(.11,.18,.34),17),box('folded-forearm',(.06,.343,.025),(.31,.09,.11),5),box('right-elbow',(.24,.225,-.08),(.10,.16,.23),17),box('closed-eyes',(.035,.327,-.37),(.16,.012,.02),11),box('salt-fold',(.02,.33,-.09),(.24,.03,.09),5)]
 b+=tag(case,0,.255,.29);model('salt-cured-body-'+str(case),b)
 b=[box('left-mark',(-.44,.022,0),(.06,.044,.94),17),box('right-mark',(.44,.022,0),(.06,.044,.94),17),box('end-mark',(0,.022,.44),(.86,.044,.06),17),box('label-foot',(.32,.08,-.36),(.22,.16,.19),11),box('label-stem',(.32,.21,-.36),(.05,.20,.045),19)]+tag(case,.32,.34,-.36)
 model('receiving-bay-'+str(case),b)
model('intake-index',[
 box('foot',(0,.075,0),(.67,.15,.54),11),box('stand',(0,.41,.08),(.15,.62,.17),19),box('desk',(0,.73,0),(.83,.10,.60),5),box('rack',(0,.95,.23),(.81,.43,.065),11),
 *[box('file'+str(i),(-.29+i*.19,1.00,.16),(.14,.32,.09),17) for i in range(4)],
 *[box('heading'+str(i),(-.29+i*.19,1.17,.105),(.075,.025,.02),19) for i in range(4)],box('open-record',(-.14,.796,-.12),(.35,.03,.27),17),box('record-lines',(-.14,.815,-.12),(.24,.01,.025),19),box('stamp-base',(.26,.81,-.13),(.17,.09,.13),11),box('stamp-knob',(.26,.90,-.13),(.065,.10,.065),19)])
model('tool-cabinet',[
 box('base',(0,.06,0),(.80,.12,.58),11),box('case',(0,.62,.045),(.75,1.05,.52),5),box('top',(0,1.19,.015),(.84,.09,.61),17),
 *[box('drawer'+str(i),(0,.27+i*.27,-.239),(.64,.215,.06),17) for i in range(4)],
 *[box('handle'+str(i),(0,.27+i*.27,-.289),(.17,.035,.06),19) for i in range(4)],box('lock',(.24,.66,-.30),(.09,.14,.04),11),box('key-slot',(.24,.66,-.324),(.019,.06,.012),19)])
model('salt-bench',[
 *[box('leg'+str(i),(x,.32,z),(.105,.64,.105),11) for i,(x,z) in enumerate([(-.34,-.30),(.34,-.30),(-.34,.30),(.34,.30)])],box('slab',(0,.66,0),(.86,.10,.79),17),box('tray',(0,.739,.04),(.54,.075,.45),11),box('salt',(0,.787,.04),(.40,.035,.32),5),
 *[box('sieve-rail'+str(i),(-.17+i*.085,.818,.03),(.025,.025,.28),17) for i in range(5)],box('tool-shaft',(.33,.741,-.03),(.035,.05,.50),18),box('tool-head',(.33,.77,-.26),(.18,.035,.075),11)])
posts=[box('post-left',(-.42,.63,0),(.12,1.26,.14),11),box('post-right',(.42,.63,0),(.12,1.26,.14),11),box('post-cap-left',(-.42,1.29,0),(.16,.07,.18),17),box('post-cap-right',(.42,1.29,0),(.16,.07,.18),17)]
for opened in [False,True]:
 leaf=[box('leaf-top',(0,1.04,0),(.70,.085,.07),17),box('leaf-bottom',(0,.22,0),(.70,.085,.07),17)]
 for i in range(5):leaf.append(box('leaf-bar'+str(i),(-.28+i*.14,.63,0),(.045,.77,.055),17))
 leaf+=[box('leaf-lock',(.24,.62,-.052),(.11,.16,.05),19)]
 if opened:
  for b in leaf:
   x,z=b['center']['x'],b['center']['z'];b['center']['x']=-.35-z;b['center']['z']=x+.10;b['size']['x'],b['size']['z']=b['size']['z'],b['size']['x']
 model('quarantine-gate-'+('open' if opened else 'closed'),posts+leaf)
model('quarantine-rail',[
 box('foot',(0,.045,0),(.95,.09,.24),11),box('top',(0,1.00,0),(.97,.08,.11),17),box('bottom',(0,.22,0),(.97,.07,.10),17),
 *[box('upright'+str(i),(-.42+i*.21,.58,0),(.045,.85,.075),17) for i in range(5)]])
model('salt-rake',[
 box('shaft',(0,.425,0),(.045,.79,.045),18),box('grip',(0,.39,0),(.06,.14,.06),19),box('head',(0,.83,0),(.39,.075,.075),11),
 *[box('tooth'+str(i),(-.15+i*.10,.90,-.015),(.035,.12,.045),17) for i in range(4)]])
model('counterfoil',[box('card',(0,.025,0),(.45,.05,.35),17),box('fold',(-.17,.056,0),(.02,.016,.35),11),box('stamp',(.10,.060,.055),(.14,.025,.11),19),box('notation',(-.015,.056,-.075),(.27,.012,.025),11),box('notch',(0,.057,.13),(.028,.017,.08),19)])
model('inspection-key',[box('shaft',(0,.039,0),(.055,.078,.39),11),box('tooth',(.07,.04,-.15),(.17,.08,.05),17),box('bow-left',(-.10,.04,.18),(.05,.08,.18),18),box('bow-right',(.10,.04,.18),(.05,.08,.18),18),box('bow-top',(0,.04,.27),(.20,.08,.04),18),box('bow-bottom',(0,.04,.09),(.20,.08,.04),18)])
for suffix,palette,n in [('transfer-docket',5,2),('discrepancy-report',17,3)]:
 b=[box('sheet',(0,.023,0),(.43,.046,.51),palette),box('fold',(-.17,.051,0),(.025,.016,.51),11),box('seal',(.11,.053,-.17),(.10,.025,.075),19)]
 b += [box('entry'+str(i),(.015,.052,-.045+i*.083),(.26,.013,.018),11) for i in range(n)]
 model(suffix,b)
(ROOT/'kit.json').write_text(json.dumps(dict(schemaVersion=1,id='curation-yard-original',palette=PALETTE,models=models),indent=2)+'\n')
