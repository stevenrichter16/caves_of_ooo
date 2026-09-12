"""Lower-density, native-import-only recipes at their actual new cell pitch.

Fine recipes describe cells in metres; this pass bins their physical centres on
an independently coarser lattice. It changes the occupancy topology, not only the
scale of unchanged source cells. The lower source bound anchors each lattice so
negative coordinates cannot add an extra coarse row on both sides. Resulting
bounds may round outward by less than one new cell; native import uniformly fits
the rebuilt mesh into the existing owner's bounds without changing gameplay.

Small container/furniture identities have explicit topology repairs. The already
minimal three-legged stool is intentionally retained at its original pitch.
"""
from collections import Counter, defaultdict
import math

from .materials import MATERIAL_INDEX
from .mesh import VoxelMesh

COARSE_KEYS = frozenset((
    'crate','crate_1','crate_2','crate_3','barrel','barrel_1','barrel_2','barrel_3',
    'bed','stool','tree','tree_1','tree_2','rock','rock_1','rock_2',
    'scrub','scrub_1','scrub_2'))


def _resample(source, pitch):
    """A source cell votes exactly once; ties use stable palette order."""
    lower=[min(p[a] for p in source.cells) for a in range(3)]
    votes=defaultdict(Counter)
    for point,material in source.cells.items():
        cell=tuple(math.floor(((point[a]-lower[a])+.5)*source.voxel_size/pitch)
                   for a in range(3))
        votes[cell][material]+=1
    result=VoxelMesh(pitch)
    result.cells={point:min(counts,key=lambda material:(-counts[material],material))
                  for point,counts in sorted(votes.items())}
    return result


def _repair_hollow_container(mesh):
    """Preserve an open column over the floor and a continuous upper rim."""
    lo=[min(p[a] for p in mesh.cells) for a in range(3)]
    hi=[max(p[a] for p in mesh.cells) for a in range(3)]
    x,y=((lo[a]+hi[a])//2 for a in range(2))
    for z in range(lo[2]+1,hi[2]+1):
        mesh.cells.pop((x,y,z),None)
    mesh.cells[(x,y,lo[2])]=MATERIAL_INDEX['wood']
    for xx in range(lo[0],hi[0]+1):
        for yy in range(lo[1],hi[1]+1):
            if xx in (lo[0],hi[0]) or yy in (lo[1],hi[1]):
                mesh.cells[(xx,yy,hi[2])]=MATERIAL_INDEX['wood_light']


def _repair_bed(mesh):
    """The frame must not outvote the blanket on the mattress's visible top."""
    hi=[max(p[a] for p in mesh.cells) for a in range(3)]
    for x in range(hi[0]+1):
        for y in range(1,hi[1]-1):
            column=[p for p in mesh.cells if p[0]==x and p[1]==y]
            if column:
                mesh.cells[max(column,key=lambda p:p[2])]=MATERIAL_INDEX['cloth_red']


def coarsen_recipes(recipes):
    """Replace only the nineteen bound native families in a fresh recipe map.

    Containers use thirds of a metre so their three-cell rims fit the original
    one-metre horizontal envelope. Nature and beds use .375m cells. Keeping
    varying pitch explicit avoids pretending all small silhouettes can survive
    a universal factor-two reduction. No global RNG or Blender is involved.
    """
    result=dict(recipes)
    for key in sorted(COARSE_KEYS):
        if key=='stool':
            continue
        pitch=1/3 if key.startswith(('crate','barrel')) else .375
        # This low rock is only .5m tall. A .375m two-layer result becomes
        # .75m tall, making the native fit shrink away the intended pitch gain.
        # One actual .5m layer preserves its low silhouette and coarse blocks.
        if key=='rock_2':
            pitch=.5
        mesh=_resample(recipes[key],pitch)
        if key in ('crate_1','barrel_2'):
            _repair_hollow_container(mesh)
        elif key=='bed':
            _repair_bed(mesh)
        result[key]=mesh
    return result
