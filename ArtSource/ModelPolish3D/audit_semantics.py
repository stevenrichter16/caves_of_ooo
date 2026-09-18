"""Compare shipped and polished FBX semantics, excluding intentional surface normals."""
import ast,bpy,json,hashlib,sys,math
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:];before=Path(args[0]);after=Path(args[1]);out=Path(args[2]);out.parent.mkdir(parents=True,exist_ok=True)
quant=lambda v:round(float(v),6)
def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':'),allow_nan=False).encode()).hexdigest()
source=Path(__file__).resolve().parents[1]/'Village3D/audit_rebuild.py'
s=source.read_text();tree=ast.parse(s)
nodes=[n for n in tree.body if isinstance(n,ast.FunctionDef)]
class Capture(ast.NodeTransformer):
 def visit_Expr(self,n):
  if isinstance(n.value,ast.Call) and ast.unparse(n.value.func)=='meshes.append':return ast.copy_location(ast.parse('meshes.append(payload)').body[0],n)
  return self.generic_visit(n)
 def visit_Return(self,n):
  if isinstance(n.value,ast.Name) and n.value.id=='result':return [ast.copy_location(ast.parse("result['actions']=actions").body[0],n),n]
  return n
module=ast.fix_missing_locations(Capture().visit(ast.Module(body=nodes,type_ignores=[])));exec(compile(module,str(source),'exec'),globals())
def prune(d):
 for m in d['meshes']:
  m.pop('normals');m.pop('smooth')
 d.pop('animationContentSha256')
 # Imported material suffixes reflect temporary Blender IDs, not runtime slots.
 for m in d['meshes']:m['materials']=[n.split('.')[0] for n in m['materials']]
 return d

def compare(a,b,path='',errors=None):
 if errors is None:errors=[]
 if isinstance(a,dict) and isinstance(b,dict):
  if set(a)!=set(b):errors.append(path+': keys');return errors
  for k in a:compare(a[k],b[k],path+'/'+k,errors)
 elif isinstance(a,list) and isinstance(b,list):
  if len(a)!=len(b):errors.append(path+': count');return errors
  for i,(x,y) in enumerate(zip(a,b)):compare(x,y,path+'/'+str(i),errors)
 elif isinstance(a,(int,float)) and isinstance(b,(int,float)):
  if abs(a-b)>2e-5:errors.append(path+': value')
 elif a!=b:errors.append(path+': value')
 return errors
file='manifest.json' if (after/'manifest.json').exists() else 'catalog.json';models=json.loads((after/file).read_text())['models'];rows=[];failures=[]
for m in models:
 a=prune(read_model(before/m['path'],m['rigged']));b=prune(read_model(after/m['path'],m['rigged']));errors=compare(a,b)
 allowed=m['id']=='barrel-0' and all('/vertices/' in e for e in errors)
 if errors and not allowed:failures.append(m['id'])
 rows.append({'id':m['id'],'sameExceptNormals':not errors,'barrelTransformCorrection':bool(errors and allowed),'differences':errors[:12],'differenceCount':len(errors)})
 print('SEMANTICS',m['id'],'CORRECTED' if allowed and errors else 'PASS' if not errors else 'FAIL',len(errors),flush=True)
report={'passed':not failures,'failures':failures,'models':rows,'tolerance':.00002,'fields':'world positions, topology, UV/colors/material assignments, skin weights, skeleton/rest/socket matrices, sampled animation curve data; normals/smooth flags intentionally excluded'};out.write_text(json.dumps(report,indent=2)+'\n');print('SEMANTIC_COMPLETE',not failures,len(rows));sys.exit(bool(failures))
