"""Deterministic sculpt-normal math; no geometry, UV, rig or runtime mutations."""
import math

def unit(value, allow_zero=False):
 if len(value)!=3 or not all(math.isfinite(x) for x in value):raise ValueError('finite three-vector required')
 length=math.hypot(*value)
 if length==0:
  if allow_zero:return None
  raise ValueError('nonzero reference normal required')
 return tuple(x/length for x in value)

def blend_corner(reference, neighbors, angle_degrees, amount):
 """Area-weight incident planes within the chip angle; retain carved faceting.

 Adjacency must be supplied by vertex identity, never spatial proximity: touching
 independent stones, concave body corners and separate owners must not be welded.
 """
 if not math.isfinite(angle_degrees) or not 0<=angle_degrees<=90:raise ValueError('angle must be 0..90')
 if not math.isfinite(amount) or not 0<=amount<=1:raise ValueError('amount must be 0..1')
 base=unit(reference);threshold=math.cos(math.radians(angle_degrees));chosen=[]
 for vector,weight in neighbors:
  if not math.isfinite(weight) or weight<0:raise ValueError('finite nonnegative area required')
  n=unit(vector,True)
  if n is not None and weight>0 and sum(x*y for x,y in zip(base,n))>=threshold-1e-10:chosen.append((n,weight))
 if not chosen or amount==0:return base
 scale=max(w for _,w in chosen)
 average=unit(tuple(math.fsum(n[i]*(w/scale) for n,w in chosen) for i in range(3)),True)
 if average is None:return base
 return unit(tuple((1-amount)*base[i]+amount*average[i] for i in range(3)))
