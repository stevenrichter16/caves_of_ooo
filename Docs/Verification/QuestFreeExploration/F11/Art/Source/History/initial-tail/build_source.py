"""Original furrowstalker source; explicit private output, no borrowed writes."""
import json,argparse
from pathlib import Path
REPO=Path('/Users/steven/caves-of-ooo')
def form(id,bp,rigged=False):
 return dict(id=id,blueprint=bp,rigged=rigged,rigFamily='quadruped' if rigged else '',clips=['Idle','Walk','Interact','Attack','Hit'] if rigged else [],sockets=[],bones=[],boxes=[])
def bone(m,name,head,tail,parent):m['bones'].append(dict(name=name,head=head,tail=tail,parent=parent))
def box(m,part,center,size,color,bone='Body'):
 m['boxes'].append(dict(part=part,center=center,size=size,color=color,bone=bone,material='palette'))
def build():
 m=form('spread-furrowstalker','Furrowstalker',True)
 bone(m,'Root',[0,0,0],[0,.08,0],None);bone(m,'Body',[0,.20,.06],[0,.43,.06],'Root')
 bone(m,'Neck',[0,.32,-.23],[0,.31,-.38],'Body');bone(m,'Head',[0,.30,-.39],[0,.26,-.58],'Neck')
 bone(m,'Jaw',[0,.235,-.43],[0,.215,-.64],'Head')
 box(m,'long-torso',[0,.295,.045],[.32,.25,.65],19)
 box(m,'drawn-waist',[0,.27,.245],[.245,.20,.24],20)
 box(m,'forward-shoulders',[0,.34,-.18],[.375,.28,.23],20)
 box(m,'high-shoulder-fur',[0,.47,-.21],[.255,.045,.175],21)
 box(m,'tucked-hips',[0,.31,.335],[.28,.235,.22],19)
 box(m,'breast-keel',[0,.205,-.20],[.20,.12,.21],18)
 box(m,'low-neck',[0,.305,-.335],[.235,.21,.22],20,'Neck')
 box(m,'wedge-head',[0,.285,-.48],[.245,.16,.24],19,'Head')
 box(m,'flat-tapered-muzzle',[0,.246,-.622],[.175,.115,.16],20,'Head')
 box(m,'black-nose',[0,.262,-.71],[.15,.045,.026],0,'Head')
 box(m,'lower-jaw',[0,.196,-.572],[.165,.045,.265],19,'Jaw')
 box(m,'pale-throat',[0,.168,-.518],[.115,.018,.15],5,'Jaw')
 for sign,side in [(-1,'L'),(1,'R')]:
  box(m,'flat-ear-'+side,[sign*.124,.38,-.398],[.075,.10,.09],19,'Head')
  box(m,'ear-face-'+side,[sign*.13,.40,-.432],[.048,.05,.018],18,'Head')
  box(m,'recessed-eye-'+side,[sign*.125,.314,-.52],[.016,.027,.036],0,'Head')
  box(m,'pale-cheek-'+side,[sign*.125,.245,-.486],[.014,.055,.095],21,'Head')
  for rear,z in [(False,-.18),(True,.335)]:
   n=('LegRear.' if rear else 'LegFront.')+side;x=sign*(.137 if rear else .173)
   bone(m,n,[x,.265,z],[x,.06,z-.04],'Body')
   box(m,'upper-'+n,[x,.195,z],[.105,.21,.145],19,n)
   box(m,'ankle-'+n,[x,.082,z-.035],[.073,.12,.092],20,n)
   box(m,'foot-'+n,[x,.025,z-.074],[.115,.05,.17],0,n)
  for i,z in enumerate([-.065,.095]):
   box(m,'broken-flank-'+side+str(i),[sign*.163,.325-i*.025,z],[.012,.06,.055],18)
 bone(m,'Tail',[0,.31,.415],[.08,.255,.60],'Body');bone(m,'TailTip',[.08,.255,.60],[.18,.22,.77],'Tail')
 box(m,'tail-root',[.022,.285,.475],[.11,.095,.21],19,'Tail')
 box(m,'tail-bend',[.075,.247,.62],[.085,.07,.17],20,'Tail')
 box(m,'tail-tip',[.14,.215,.758],[.065,.055,.16],19,'TailTip')
 box(m,'tail-pale-underside',[.14,.182,.74],[.046,.015,.085],21,'TailTip')
 m['motion']={
  'Idle':dict(frames=48,loop=True,rootMotion=False,breathScale=.012),
  'Walk':dict(frames=16,loop=True,rootMotion=False,legPitch=.24),
  'Interact':dict(frames=14,loop=False,rootMotion=False,headPitch=.48,jawPitch=.19),
  'Attack':dict(frames=6,loop=False,rootMotion=False,headPitch=-.19,jawPitch=.48),
  'Hit':dict(frames=6,loop=False,rootMotion=False,bodyPitch=.11)}
 c=form('spread-furrowstalker-remains','FurrowstalkerCorpse')
 box(c,'collapsed-long-body',[0,.115,.04],[.37,.22,.66],19)
 box(c,'collapsed-shoulder',[-.025,.16,-.21],[.38,.16,.22],20)
 box(c,'flat-head',[-.06,.11,-.465],[.28,.16,.29],19)
 box(c,'folded-muzzle',[-.075,.064,-.64],[.20,.075,.15],20)
 box(c,'pale-throat',[-.072,.022,-.545],[.14,.025,.16],5)
 box(c,'closed-nose',[-.075,.082,-.723],[.16,.035,.025],0)
 for sign in [-1,1]:
  for z in [-.16,.27]:box(c,'folded-foot-'+str(sign)+str(z),[sign*.21,.029,z],[.16,.055,.115],0)
  box(c,'laid-ear-'+str(sign),[-.06+sign*.16,.14,-.355],[.105,.05,.09],19)
  box(c,'visible-flank-'+str(sign),[sign*.15,.22,.10],[.016,.026,.09],18)
 box(c,'folded-tail-root',[.06,.08,.44],[.14,.08,.27],19)
 box(c,'folded-tapered-tail',[.145,.055,.648],[.065,.055,.205],20)
 return dict(schemaVersion=1,id='spread-furrowstalker-original',coordinates='Unity X east/Y up/Z north; source forward -Z; one unit per native cell',palette=json.loads((REPO/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'],effects=[],physics=[],models=[m,c])
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.write_text(json.dumps(build(),indent=2)+'\n')
