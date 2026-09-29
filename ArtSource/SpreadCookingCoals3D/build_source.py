"""Original paired residual-coal meshes in Unity axes; no runtime or borrowed writes."""
import json,math,copy,argparse
from pathlib import Path
R=Path('/Users/steven/caves-of-ooo')
def prism(name,footprint,bottom,top,x=0,z=0,yaw=0,taper=1,colors=None):
 v=[]
 for y,s in [(bottom,1),(top,taper)]:
  for a,b in footprint:
   a*=s;b*=s;v.append([round(x+a*math.cos(yaw)-b*math.sin(yaw),7),y,round(z+a*math.sin(yaw)+b*math.cos(yaw),7)])
 n=len(footprint);faces=[list(range(n-1,-1,-1)),list(range(n,2*n))]+[[j,(j+1)%n,(j+1)%n+n,j+n]for j in range(n)]
 return dict(part=name,vertices=v,faces=faces,faceColors=colors or [23,11]+[10]*n)
def build():
 ash=[(-.39,-.21),(-.21,-.35),(.18,-.32),(.41,-.15),(.43,.11),(.21,.34),(-.19,.32),(-.42,.12)]
 pieces=[prism('ash-bed',ash,0,.022,colors=[23,10]+[23]*8)]
 rows=[(-.19,-.14,.115,.245,.062,-.42),(-.025,-.18,.13,.22,.065,.10),(.16,-.115,.12,.235,.075,.68),(-.205,.075,.125,.215,.07,-.63),(-.02,.08,.135,.25,.085,.18),(.17,.105,.12,.21,.065,.67),(-.005,.255,.12,.19,.045,1.35)]
 for i,(x,z,w,l,h,yaw)in enumerate(rows):
  footprint=[(-w*.5,-l*.30),(0,-l*.5),(w*.5,-l*.28),(w*.48,l*.29),(0,l*.5),(-w*.46,l*.27)]
  # Most faces remain char-dark. The top and one small broken end expose restrained warm palette colors.
  colors=[23,18]+[19 if j%2 else 23 for j in range(6)];colors[2+(i%6)]=21 if i%3 else 22
  coal=prism('charcoal-'+str(i),footprint,.022,round(.022+h,7),x,z,yaw,.70,colors)
  # Two dark top banks enclose a narrow, actual exposed split. The cooled form
  # uses exactly the same vertices/topology; only the split's palette changes.
  for a,b in [(-w*.09,-l*.30),(w*.09,-l*.28),(w*.09,l*.29),(-w*.09,l*.27)]:
   a*=.70;b*=.70;coal['vertices'].append([round(x+a*math.cos(yaw)-b*math.sin(yaw),7),round(.022+h,7),round(z+a*math.sin(yaw)+b*math.cos(yaw),7)])
  coal['faces']=[coal['faces'][0],[6,7,12,15,10,11],[7,13,14,10,15,12],[7,8,9,10,14,13]]+coal['faces'][2:]
  coal['faceColors']=[colors[0],19,18,23]+colors[2:]
  pieces.append(coal)
 for i,(x,z,w,l,h,yaw)in enumerate([(-.355,-.025,.105,.23,.065,-.25),(.335,.03,.11,.225,.073,.28),(.035,-.325,.26,.10,.045,.08)]):
  pieces.append(prism('edge-stone-'+str(i),[(-w*.5,-l*.5),(w*.5,-l*.5),(w*.5,l*.5),(-w*.5,l*.5)],0,h,x,z,yaw,.76,[23,12,10,11,10,11]))
 hot=dict(id='spread-cooking-coals-hot',blueprint='SpreadCookingCoals',kind='entity',rigged=False,clips=[],bones=[],sockets=[],pieces=pieces)
 cool=copy.deepcopy(hot);cool['id']='spread-cooking-coals-cooled'
 for piece in cool['pieces']:
  if piece['part'].startswith('charcoal-'):piece['faceColors']=[{18:11,21:12,22:17}.get(c,c)for c in piece['faceColors']]
 return dict(schemaVersion=1,id='spread-residual-coals-original',coordinates='Unity X east/Y up/Z north; one unit per cell',palette=json.loads((R/'ArtSource/ReferenceGlade3D/kit.json').read_text())['palette'],models=[hot,cool])
if __name__=='__main__':
 a=argparse.ArgumentParser();a.add_argument('--output',type=Path,required=True);args=a.parse_args();args.output.write_text(json.dumps(build(),indent=2)+'\n')
