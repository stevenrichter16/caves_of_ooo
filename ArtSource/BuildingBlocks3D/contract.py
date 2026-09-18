"""Portable acceptance of cell-aligned, independently owned architecture assets."""
import math,re

def validate(rows):
 errors=[];seen=set();families={}
 if not rows:return ['empty-kit']
 for row in rows:
  mid=row.get('id','');family=row.get('family','');v=row.get('variant')
  if not re.fullmatch(r'[a-z][a-z0-9-]*',mid) or mid in seen:errors.append('invalid-or-duplicate-id:'+mid)
  seen.add(mid)
  if not re.fullmatch(r'[a-z][a-z0-9-]*',family):errors.append('invalid-family:'+mid)
  if type(v) is not int or v not in range(4):errors.append('invalid-variant:'+mid)
  elif v in families.setdefault(family,set()):errors.append('duplicate-family-variant:'+mid)
  else:families[family].add(v)
  if row.get('path')!='models/'+mid+'.fbx':errors.append('invalid-path:'+mid)
  if row.get('pivot')!='bottom-centre' or row.get('cellSize')!=1:errors.append('invalid-snap-contract:'+mid)
  if row.get('material') not in ('wood','stone'):errors.append('invalid-material:'+mid)
  if type(row.get('triangles')) is not int or row['triangles']<=0 or row.get('zeroAreaTriangles')!=0:errors.append('invalid-topology:'+mid)
  b=row.get('bounds',{});lo=b.get('min',[]);hi=b.get('max',[])
  if len(lo)!=3 or len(hi)!=3 or not all(isinstance(x,(int,float)) and math.isfinite(x) for x in lo+hi):errors.append('invalid-bounds:'+mid);continue
  if any(lo[i]>=hi[i] for i in range(3)) or lo[2]<-1e-5:errors.append('inverted-or-buried:'+mid)
  if any(lo[i]<-.50001 or hi[i]>.50001 for i in (0,1)):errors.append('outside-cell:'+mid)
 for family,variants in families.items():
  if variants!=set(range(4)):errors.append('incomplete-variants:'+family)
 return errors
