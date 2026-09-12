"""Neutral meshes from the real toolkit recipes, with explicit native sizing.

No Blender dependency. Vertices use a horizontally centred floor pivot. Voxel
runs retain the source integer grid and declare ``voxelOrigin`` in metres; this
also represents half-voxel offsets needed to centre odd-width recipes honestly.
The optional single-cell fit is ONE uniform scale on all three axes. It changes
the effective voxel size and does not preserve the full-size object's stature.
"""
import json
import math
import os
import tempfile
from pathlib import Path

from .assets import build_recipes
from .materials import PALETTE_HEX


def native_recipes(profile='fine'):
    """Fresh recipes; opt-in ``native_coarse`` changes topology and cell pitch.

    The coarse profile is for uniformly fitted native imports, not replacement
    of the fine toolkit's authored full-size buildings or candidate ownership.
    """
    if profile not in ('fine','native_coarse'):
        raise ValueError('Unknown native recipe profile')
    recipes=build_recipes(.25)
    if profile=='native_coarse':
        from .native_coarse import coarsen_recipes
        recipes=coarsen_recipes(recipes)
    return recipes


def _runs(mesh):
    rows=[]
    for (x,y,z),material in sorted(mesh.cells.items(),key=lambda p:(p[0][2],p[0][1],p[0][0])):
        if rows and rows[-1][1:3]==[y,z] and rows[-1][0]+rows[-1][3]==x and rows[-1][4]==material:
            rows[-1][3]+=1
        else:rows.append([x,y,z,1,material])
    return rows


def asset_manifest(profile='fine'):
    """Return independent JSON-ready mesh/voxel records in stable asset order.

    ``nativePlacementOffset`` is applied after the centred pivot when placing
    a full-size asset at a native cell centre. Even-width/depth footprints need
    a half-metre offset to avoid spuriously touching an extra row/column. The
    footprint includes the complete horizontal geometry (including canopy),
    not an inferred trunk-only collision shape or an automatic gameplay Part.
    ``singleCellScale`` instead fits the centred mesh inside one cell with no
    placement offset. Unity decides which physical ownership policy to use.
    """
    result={'schemaVersion':1,'axes':'Blender: X east, Y north, Z up',
            'pivot':'centred horizontal bounds; floor at Z=0',
            'units':'metres','voxelSize':.25,'cellSize':1,
            'nativeAxes':'X east, Y south; Unity X east, Y height, Z north',
            'palette':[{'name':name,'hex':color} for name,color in PALETTE_HEX.items()],
            'assets':[]}
    if profile=='native_coarse':
        result.update(profile=profile,voxelSize=None,voxelSizePolicy='per-asset',
                      sourceVoxelSize=.25,usage='uniformly-fitted-native-import-only')
    for key,mesh in sorted(native_recipes(profile).items()):
        vertices,quads,materials=mesh.geometry()
        lo=[min(v[axis] for v in vertices) for axis in range(3)]
        hi=[max(v[axis] for v in vertices) for axis in range(3)]
        origin=[-(lo[0]+hi[0])/2,-(lo[1]+hi[1])/2,-lo[2]]
        size=[hi[i]-lo[i] for i in range(3)]
        low=[lo[i]+origin[i] for i in range(3)];high=[hi[i]+origin[i] for i in range(3)]
        fit=min(1.,1/max(size[:2]));width,depth=(math.ceil(v-1e-9) for v in size[:2])
        offset=[.5 if width%2==0 else 0.,.5 if depth%2==0 else 0.,0.]
        # Half-open bounds: the boundary plane itself does not occupy a cell.
        minx=math.floor(.5+low[0]+offset[0]+1e-9)
        maxx=math.ceil(.5+high[0]+offset[0]-1e-9)-1
        miny=math.floor(.5-high[1]-offset[1]+1e-9)
        maxy=math.ceil(.5-low[1]-offset[1]-1e-9)-1
        triangles=[];triangle_materials=[]
        for face,material in zip(quads,materials):
            a,b,c,d=face;triangles.extend(([a,b,c],[a,c,d]));triangle_materials.extend((material,material))
        result['assets'].append({'id':'voxel-'+key.replace('_','-'),'sourceAsset':key,
            'voxelSize':mesh.voxel_size,'voxelOrigin':origin,'runs':_runs(mesh),
            'vertices':[[v[i]+origin[i] for i in range(3)] for v in vertices],
            'quads':[list(face) for face in quads],'quadMaterials':list(materials),
            'triangles':triangles,'triangleMaterials':triangle_materials,
            'bounds':{'minimum':low,'maximum':high,'size':size},
            'nativePlacementOffset':offset,
            'nativeFootprintBounds':{'minX':minx,'minY':miny,'maxX':maxx,'maxY':maxy,
                'width':maxx-minx+1,'depth':maxy-miny+1},
            'nativeFootprintCells':[[x,y] for y in range(miny,maxy+1) for x in range(minx,maxx+1)],
            'fitsOneCellAtNativeScale':max(size[:2])<=1,
            'singleCellScale':fit,'singleCellUniformScale':[fit]*3,
            'singleCellEffectiveVoxelSize':mesh.voxel_size*fit,
            'ownership':'single-cell' if max(size[:2])<=1 else 'multi-cell-at-native-scale'})
    if profile=='native_coarse':
        from .native_palette import cap_export_materials
        for row in result['assets']:
            cap_export_materials(row)
    return result


def export_assets(path,profile='fine'):
    """Atomically replace one generated JSON file, preserving unrelated files."""
    path=Path(path);data=asset_manifest(profile)
    encoded=(json.dumps(data,sort_keys=True,separators=(',',':'))+'\n').encode()
    path.parent.mkdir(parents=True,exist_ok=True)
    with tempfile.NamedTemporaryFile(prefix='.native-assets-',dir=path.parent,delete=False) as stream:
        temporary=Path(stream.name)
        try:
            stream.write(encoded);stream.flush();os.fsync(stream.fileno())
        except BaseException:
            temporary.unlink(missing_ok=True);raise
    try:os.replace(temporary,path)
    finally:temporary.unlink(missing_ok=True)
    return data
