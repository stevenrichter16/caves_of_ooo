"""Original Tatterjay and remains; explicit output only, no borrowed art mutations."""
import argparse,json,os
from pathlib import Path
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT','/Users/steven/caves-of-ooo'))
CLIPS=['Idle','Walk','Interact','Attack','Hit','CarryIdle','CarryWalk','Pickup','Deposit']
def model(mid,bp,live):return dict(id=mid,blueprint=bp,rigged=live,rigFamily='avian' if live else '',clips=list(CLIPS)if live else[],sockets=[],bones=[],boxes=[])
def bone(m,n,h,t,p):m['bones'].append(dict(name=n,head=list(h),tail=list(t),parent=p))
def box(m,n,c,s,col,b='Body'):m['boxes'].append(dict(part=n,center=list(c),size=list(s),color=col,bone=b))
def build():
 m=model('spread-tatterjay','Tatterjay',True)
 bone(m,'Root',(0,0,0),(0,.07,0),None);bone(m,'Body',(0,.14,.035),(0,.38,.035),'Root');bone(m,'Head',(0,.40,-.15),(0,.57,-.23),'Body')
 box(m,'broad-chest',(0,.31,.035),(.34,.30,.32),3);box(m,'pale-breast',(0,.335,-.145),(.275,.245,.095),17)
 box(m,'dark-shoulder',(0,.43,.035),(.30,.085,.265),23);box(m,'thick-neck',(0,.45,-.155),(.17,.19,.18),3,'Head')
 box(m,'broad-head',(0,.5775,-.23),(.225,.205,.22),3,'Head');box(m,'stepped-crown',(-.025,.6925,-.185),(.16,.05,.16),23,'Head')
 box(m,'broad-pale-bill',(0,.515,-.395),(.115,.075,.16),17,'Head');box(m,'bill-cut',(0,.487,-.399),(.105,.026,.15),19,'Head')
 for sign,side in [(-1,'L'),(1,'R')]:
  bone(m,'Wing.'+side,(sign*.15,.39,.015),(sign*.31,.31,.12),'Body');bone(m,'Leg.'+side,(sign*.085,.21,-.005),(sign*.085,.027,-.035),'Body')
  box(m,'folded-wing-'+side,(sign*.205,.33,.085),(.10,.20,.34),23,'Wing.'+side)
  box(m,'slate-wing-bar-'+side,(sign*.205,.434,.09),(.11,.032,.215),12,'Wing.'+side)
  box(m,'wing-tip-'+side,(sign*.205,.29,.27),(.09,.07,.11),3,'Wing.'+side)
  box(m,'shin-'+side,(sign*.085,.112,-.025),(.035,.19,.042),19,'Leg.'+side)
  box(m,'spread-foot-'+side,(sign*.085,.021,-.065),(.105,.042,.15),19,'Leg.'+side)
  box(m,'pale-eye-'+side,(sign*.117,.587,-.285),(.018,.035,.044),17,'Head')
  box(m,'eye-cut-'+side,(sign*.127,.587,-.297),(.009,.026,.023),0,'Head')
 bone(m,'Tail',(0,.26,.17),(0,.20,.48),'Body')
 for i,(x,y,z,length,col) in enumerate([(-.085,.26,.31,.29,3),(0,.23,.35,.38,23),(.085,.245,.335,.33,12)]):box(m,'ragged-tail-'+str(i),(x,y,z),(.08,.055,length),col,'Tail')
 m['sockets']=[dict(name='Collector.Bill',bone='Head',position=[0,.515,-.425])]
 # Parameters are consumed by the isolated exporter; native tests must also
 # verify imported clip deformation and socket ownership, not these labels alone.
 m['motion']={'Idle':{'bodyBreath':.018,'headSway':.035},'Walk':{'legSwing':.34,'wingSwing':.08,'bodyBob':.014},'Interact':{'headPitch':.40,'bodyPitch':.09},'Attack':{'headPitch':.45,'wingSwing':.20},'Hit':{'bodyPitch':-.15,'headPitch':-.12},'CarryIdle':{'headPitch':-.11,'bodyBreath':.012},'CarryWalk':{'headPitch':-.11,'legSwing':.30,'wingSwing':.06,'bodyBob':.010},'Pickup':{'headPitch':.70,'bodyPitch':.10},'Deposit':{'headPitch':.50,'bodyPitch':.06,'headSway':.09}}
 c=model('spread-tatterjay-remains','TatterjayCorpse',False)
 box(c,'folded-body',(0,.105,.055),(.38,.20,.38),3);box(c,'folded-pale-chest',(0,.13,-.11),(.28,.17,.10),17)
 box(c,'folded-head',(-.055,.085,-.205),(.245,.16,.20),3);box(c,'folded-pale-bill',(-.06,.058,-.34),(.13,.07,.17),17)
 for sign,side in [(-1,'L'),(1,'R')]:
  box(c,'fallen-wing-'+side,(sign*.235,.053,.05),(.15,.09,.31),23);box(c,'fallen-wing-mark-'+side,(sign*.235,.101,.05),(.115,.018,.22),12)
  box(c,'folded-foot-'+side,(sign*.105,.029,-.01),(.05,.035,.20),19)
 for i,(x,z,n,col) in enumerate([(-.07,.31,.30,3),(0,.34,.36,23),(.07,.325,.33,12)]):box(c,'fallen-tail-'+str(i),(x,.075,z),(.067,.06,n),col)
 return dict(schemaVersion=1,id='spread-collector-original-art',coordinates='Unity X east/Y up/Z north; one unit per cell',palette=json.loads((ROOT/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'],models=[m,c])
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.write_text(json.dumps(build(),indent=2)+'\n')
