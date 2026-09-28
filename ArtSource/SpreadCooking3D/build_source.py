"""Original prepared-grain source, Unity coordinates; writes explicit private output only."""
import argparse,json,math,os
from pathlib import Path
ROOT=Path(os.environ.get('COO_SPREAD_SOURCE_ROOT','/Users/steven/caves-of-ooo'))
def kernel(i,x,z,width,length,height,yaw):
 # Six-sided kernels taper at both ends; the narrow top exposes pale interiors.
 footprint=[(-.5,-.25),(0,-.5),(.5,-.25),(.5,.25),(0,.5),(-.5,.25)]
 v=[]
 for y,scale in [(0.01,1),(.01+height,.72)]:
  for a,b in footprint:
   a*=width*scale;b*=length*scale
   v.append([round(x+a*math.cos(yaw)-b*math.sin(yaw),7),round(y,7),round(z+a*math.sin(yaw)+b*math.cos(yaw),7)])
 faces=[list(range(5,-1,-1)),list(range(6,12))]+[[j,(j+1)%6,(j+1)%6+6,j+6] for j in range(6)]
 colors=[19,22 if i in (1,4,7) else 18]+[20 if (j+i)%3==0 else (19 if j==0 else 18) for j in range(6)]
 return dict(part='toasted-kernel-'+str(i),vertices=v,faces=faces,faceColors=colors)
def build():
 rows=[(-.19,-.08,.065,.20,.040,-.55),(-.095,-.115,.070,.22,.052,-.10),(.015,-.13,.070,.225,.045,.25),(.14,-.09,.075,.21,.047,.70),(-.17,.09,.065,.20,.040,-.75),(-.04,.09,.075,.245,.060,-.10),(.085,.095,.070,.22,.050,.15),(.20,.075,.065,.195,.040,.60)]
 m=dict(id='spread-toasted-emberwheat',blueprint='ToastedEmberwheat',rigged=False,kind='entity',clips=[],bones=[],sockets=[],kernels=[kernel(i,*r) for i,r in enumerate(rows)])
 return dict(schemaVersion=1,id='spread-toasted-emberwheat-original',coordinates='Unity X east/Y up/Z north; one unit per cell',palette=json.loads((ROOT/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'],models=[m])
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);args=p.parse_args();args.output.write_text(json.dumps(build(),indent=2)+'\n')
