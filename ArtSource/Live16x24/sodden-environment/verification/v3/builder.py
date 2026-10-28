"""Original Sodden firm-ground/wet-peat cells; source only, never writes Assets.
Blender --background --threads 4 --python this-file -- --build
python3 this-file --snapshot | --verify
Blender --background editable.blend --python this-file -- --inspect-blend
Pillow only decodes exports for verification; authoring uses integer unit quads.
"""
import argparse, hashlib, json, re, sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/sodden-environment'
W,H,COUNT=16,24,36
P={'ground':'3D492E','moss':'414D31','moss-dark':'39452C','sedge':'434E32','straw':'475034',
   'wet':'303B37','deep':'2D3733','film':'35403B','film-light':'39443E',
   'shore':'3D472F','shore-light':'465035','shore-dark':'28312C'}
SIDES=((1,'N'),(2,'E'),(4,'S'),(8,'W'))

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(n,v):(OUT/n).write_text(json.dumps(v,indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for p,h in pins.items():assert sha(ROOT/p)==h,p
    return len(pins)
def snapshot():
    assert not (OUT/'protected-inputs.json').exists(),'Never silently repin inputs'
    paths=[ROOT/'Assets/Resources/Sprites/Live16x24'/n for n in ('sodden-travel.png','sill-envelope.png','opening-base.png','opening-states.png','north-plot.png')]
    paths += [OUT/n for n in ('scene-reference-01.png','scene-prompt-01.txt','parts-critique.md')]
    paths += [ROOT/'Docs/Verification/Live16x24'/n for n in ('sodden-environment-before-01.png','sodden-environment-before-02-reed-maze.png')]
    paths += [ROOT/'Tools/Live16x24'/n for n in ('build_buckler.py','build_cindercaller.py','build_opening_base.py','build_north_plot.py','build_limestone_caves.py','build_sodden_travel.py')]
    save('protected-inputs.json',{str(p.relative_to(ROOT)):sha(p) for p in paths})

class Frame:
    def __init__(self,name,owner,**state):
        self.name,self.owner,self.state=name,owner,state
        self.cells=[[None]*W for _ in range(H)]
    def rect(self,part,color,x,y,w=1,h=1):
        assert color in P and 0<=x<x+w<=W and 0<=y<y+h<=H
        for yy in range(y,y+h):
            for xx in range(x,x+w):self.cells[yy][xx]=[part,color]
        return self
    def rows(self,part,color,rows):
        for y,x,w in rows:self.rect(part,color,x,y,w)
        return self

def ground(v):
    f=Frame('firm-fen-'+str(v),'Grass',variant=v,groundOnly=True)
    f.rect('quiet-firm-fen','ground',0,0,W,H)
    # Compact unequal surface clusters avoid a common diagonal stroke when
    # coarse runtime patches repeat one cell. No tall sprigs or central oval.
    dark=[[(7,3,2),(8,2,3),(9,3,1),(18,11,2),(19,12,1)],
          [(5,3,3),(6,2,4),(7,2,2),(8,3,3),(9,4,2),(17,11,2),(18,10,3),(19,11,1)],
          [(5,10,3),(6,9,2),(7,9,4),(8,11,2),(16,2,4),(17,2,3),(18,3,4),(19,2,2)],
          [(3,4,2),(4,3,4),(5,4,3),(6,3,2),(15,9,3),(16,8,4),(17,10,3),(18,9,2)]]
    light=[[(13,8,2),(14,9,1)],
           [(5,5,2),(6,6,2),(8,5,2),(16,10,3),(17,10,1),(18,12,2)],
           [(4,11,2),(6,11,2),(8,10,1),(15,3,3),(16,5,2),(18,6,2)],
           [(3,6,3),(4,7,2),(6,5,2),(14,10,3),(15,12,1),(17,9,2)]]
    f.rows('compact-moss-recess','moss-dark',dark[v])
    f.rows('low-moss-fray','moss',light[v])
    # Sparse granular edge pixels are lower contrast than the old straw line.
    grains=[[(6,4,1),(17,12,1)],[(4,4,1),(7,6,1),(19,13,1)],
            [(5,9,1),(15,2,1),(19,5,1)],[(2,5,1),(5,8,1),(18,12,1)]]
    f.rows('matted-edge-grain','sedge',grains[v])
    if v==1:f.rows('flattened-straw-fragment','straw',[(7,4,2),(17,12,1)])
    if v==3:f.rows('flattened-straw-fragment','straw',[(4,5,2),(16,10,1)])
    return f

def wet(depleted,mask):
    f=Frame(('depleted-wet-film-' if depleted else 'filled-mire-')+format(mask,'X'),'MirePool',connectedMask=mask,depleted=depleted,stillWet=True)
    f.rect('continuous-wet-peat','wet',0,0,W,H)
    # Lifted, desaturated green-gray wet peat remains visibly separate from
    # void. Broken horizontal film is only five RGB levels above its body.
    if depleted:
        f.rows('subdued-broad-wet-film','deep',[(6,0,6),(7,0,9),(8,0,12),(9,0,14),(10,0,16),
            (11,0,16),(12,2,14),(13,4,12),(14,7,9),(15,10,6)])
        f.rows('thin-level-wet-film','film',[(5,6,2),(6,9,3),(17,3,3),(18,2,1)])
    else:
        f.rows('subdued-level-wet-film','film',[(8,3,3),(8,8,2),(9,6,2),(18,11,2)])
    # The shallow shore stays INSIDE the hazard cell. Connected sides have
    # no shore, no grass and no outer stroke, including mixed stock joins.
    profileN=(1,1,2,2,1,1,1,2,2,1,1,2,1,1,1,1)
    profileS=(1,1,1,2,1,1,2,2,1,1,1,2,2,1,1,1)
    profileE=(1,1,1,2,2,1,1,1,2,1,1,2,2,1,1,1,2,2,1,1,1,2,1,1)
    profileW=(1,1,2,1,1,2,2,1,1,1,2,1,1,1,2,2,1,1,2,1,1,1,1,1)
    for bit,side in SIDES:
        if mask&bit:continue
        horizontal=side in 'NS';profile=profileN if side=='N' else profileS if side=='S' else profileE if side=='E' else profileW
        for t,depth in enumerate(profile):
            for d in range(depth):
                x,y=(t,d) if side=='N' else (t,H-1-d) if side=='S' else (W-1-d,t) if side=='E' else (d,t)
                f.rect('inside-shore-'+side,'shore',x,y)
            if t in ((4,5,10) if horizontal else (5,6,15,16)):
                x,y=(t,0) if side=='N' else (t,H-1) if side=='S' else (W-1,t) if side=='E' else (0,t)
                f.rect('broken-muted-lip-'+side,'shore-light',x,y)
            if t in ((2,3,8,9,12) if horizontal else (3,4,10,11,18,19)):
                x,y=(t,depth) if side=='N' else (t,H-1-depth) if side=='S' else (W-1-depth,t) if side=='E' else (depth,t)
                f.rect('wet-contact-'+side,'shore-dark',x,y)
    # Exact common edge pixels eliminate tile-border seams on every valid
    # connected pair. Corners remain wet when either adjoining side connects.
    for bit,side in SIDES:
        if not mask&bit:continue
        for t in range(W if side in 'NS' else H):
            x,y=(t,0) if side=='N' else (t,H-1) if side=='S' else (W-1,t) if side=='E' else (0,t)
            f.rect('connected-wet-edge','wet',x,y)
    return f

def frames():return [ground(v) for v in range(4)]+[wet(d,m) for d in (False,True) for m in range(16)]
def source():return {'coordinates':'Top-left y down; opaque named integer unit quads','palette':P,'frames':[{'index':i,'name':f.name,'owner':f.owner,**f.state,'cells':f.cells} for i,f in enumerate(frames())]}
def rgba(v):return tuple(bytes.fromhex(P[v[1]]))+(255,)
def edges(c):return {'N':c[0],'E':[row[-1] for row in c],'S':c[-1],'W':[row[0] for row in c]}
def validate(s,size,data):
    assert s==source() and size==(576,24) and len(data)==576*24*4
    for i,f in enumerate(frames()):
        assert all(v is not None for row in f.cells for v in row)
        for y,row in enumerate(f.cells):
            for x,v in enumerate(row):
                at=4*(y*576+i*16+x);assert tuple(data[at:at+4])==rgba(v),(i,x,y)
        if i<4:
            assert sum(v[1]=='ground' for row in f.cells for v in row)>=280
            assert all(v[1]=='ground' for edge in edges(f.cells).values() for v in edge)
        else:
            mask=(i-4)%16
            for bit,side in SIDES:
                if mask&bit:assert all(v[1]=='wet' for v in edges(f.cells)[side]),(i,side)
                else:assert any(v[0].endswith('-'+side) for row in f.cells for v in row),(i,side)
            assert all(not v[0].startswith(('quiet-firm','low-connected','flattened-sedge','short-matted')) for row in f.cells for v in row)
    return {'native_pixels':13824,'fully_opaque_frames':36,'connected_edges_checked':64,'ground_minimum_base_pixels':280}

def placements(kind):
    if kind=='contact':return 184,194,[{'frame':i,'topLeft':[8+(i%8)*22,18+(i//8)*34]} for i in range(36)]
    occupied=set()
    for y in range(1,7):
        for x in range(1,6 if y<4 else 5):occupied.add((x,y))
    for y in range(2,9):
        for x in range(11 if y<5 else 10,18 if y<7 else 16):occupied.add((x,y))
    for y in range(7,10):
        for x in range(5,8):occupied.add((x,y))
    result=[]
    for y in range(10):
        for x in range(20):
            i=((x*13+y*7+(x*y)%5)^((x+y)//3))%4
            if (x,y) in occupied:
                mask=sum(bit for bit,q in ((1,(x,y-1)),(2,(x+1,y)),(4,(x,y+1)),(8,(x-1,y))) if q in occupied)
                i=(20 if x>=15 or (5<=x<8 and y>=7) else 4)+mask
            result.append({'frame':i,'topLeft':[x*16,18+y*24]})
    return 320,264,result

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for,add_label,emission_material
    from build_north_plot import instance,plane
    from build_limestone_caves import canonicalize_timing
    guard();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Sodden environment native576x24';cols=[]
    for i,f in enumerate(frames()):
        col=model(native,f'{i:02d} '+f.name,f.cells,P);native.collection.children.unlink(col)
        for o in col.objects:o['frameIndex']=i
        instance(native,col,str(i),i*W,0);cols.append(col)
    camera_for(native,576,24);native.render.filepath=str(OUT/'sodden-ground.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'sodden-ground.png')
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',5,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
        width,height,ps=placements(kind);scene=bpy.data.scenes.new('Sodden source '+kind+' '+str(scale)+'x');camera_for(scene,width,height,scale)
        plane(scene,'Source surround','172018',0,0,width,height,-2)
        label=emission_material('Source label','9CA47B');add_label(scene,'SODDEN / '+('36 NATIVE CELLS' if kind=='contact' else 'SOURCE PATTERN / NOT GAMEPLAY'),width/2,height-7,label,1.6)
        for p in ps:
            i=p['frame'];x,y=p['topLeft'];instance(scene,cols[i],str(p),x,height-y-H)
            if kind=='contact':add_label(scene,str(i),x+8,height-y-H-5,label,1.5)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',source());save('palette.json',P)
    save('manifest.json',{'frames':36,'size':[576,24],'rect':'index*16,0,16,24','ground':[0,3],'filled':'4+mask','depletedWetFilm':'20+mask','connections':{'N':1,'E':2,'S':4,'W':8},'alpha':'Fully opaque','source_only':True,'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','scope':'Original ground/mire material pixels only; no new terrain owner, geometry, collision or hazard semantics.'})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def verify():
    from PIL import Image
    count=guard();s=json.loads((OUT/'source-cells.json').read_text())
    with Image.open(OUT/'sodden-ground.png') as im:assert im.mode=='RGBA';data=im.tobytes();stats=validate(s,im.size,data)
    checked=0
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',5,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
        width,height,ps=placements(kind)
        with Image.open(OUT/name) as im:
            assert im.size==(width*scale,height*scale);pix=im.load()
            for p in ps:
                x0,y0=p['topLeft'];c=s['frames'][p['frame']]['cells']
                for y,row in enumerate(c):
                    for x,v in enumerate(row):
                        expected=rgba(v)
                        for dy in range(scale):
                            for dx in range(scale):assert pix[(x0+x)*scale+dx,(y0+y)*scale+dy]==expected;checked+=1
    rejected=[]
    for name,frame,x,y,channel,value in [('wrong-ground',0,0,0,0,255),('transparent-pool',19,8,12,3,0),('invented-connected-shore',19,7,0,0,99),('wrong-empty-film',35,4,8,1,255)]:
        bad=bytearray(data);at=4*(y*576+frame*16+x)+channel;assert bad[at]!=value;bad[at]=value
        try:validate(s,(576,24),bad)
        except AssertionError:rejected.append(name)
        else:raise AssertionError(name)
    save('source-verification.json',{'passed':True,**stats,'contact_and_pattern_pixels':checked,'corruptions_rejected':rejected,'png_sha256':sha(OUT/'sodden-ground.png'),'blend_sha256':sha(OUT/'editable.blend'),'protected_inputs_unchanged':count,'runtime_or_visual_gameplay_acceptance':False})
    print(json.dumps(stats))

def inspect_blend():
    import bpy
    guard();quads=groups=0
    for i,f in enumerate(frames()):
        col=bpy.data.collections[f'{i:02d} '+f.name];found={}
        for obj in col.objects:
            assert obj.type=='MESH' and obj['frameIndex']==i and tuple(obj.location)==(0,0,0) and tuple(obj.scale)==(1,1,1) and tuple(obj.rotation_euler)==(0,0,0)
            part=re.sub(r'\.\d+$','',obj.name);color=obj['paletteHex'];assert len(obj.data.materials)==1
            nodes=[n for n in obj.data.materials[0].node_tree.nodes if n.type=='EMISSION'];assert len(nodes)==1 and nodes[0].inputs['Strength'].default_value==1
            def linear(v):s=v/255;return s/12.92 if s<=.04045 else ((s+.055)/1.055)**2.4
            assert all(abs(nodes[0].inputs['Color'].default_value[k]-linear(v))<1e-6 for k,v in enumerate(bytes.fromhex(color)))
            for face in obj.data.polygons:
                v=[obj.data.vertices[j].co for j in face.vertices];assert len(v)==4 and all(p.z==0 and p.x==int(p.x) and p.y==int(p.y) for p in v)
                x=min(p.x for p in v);y=min(p.y for p in v);assert max(p.x for p in v)-x==max(p.y for p in v)-y==1
                q=(int(x),23-int(y));assert q not in found;found[q]=(part,color);quads+=1
            groups+=1
        assert found=={(x,y):(v[0],P[v[1]]) for y,row in enumerate(f.cells) for x,v in enumerate(row)}
    assert not bpy.data.actions
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,'frames':36,'unit_quads':quads,'named_mesh_groups':groups,'exact_parts_palette_coordinates':True,'blend_sha256':sha(OUT/'editable.blend'),'runtime_or_visual_gameplay_acceptance':False})
    print('Exact reopened geometry',quads,groups)

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    p=argparse.ArgumentParser(description=__doc__)
    for flag in ('snapshot','build','verify','inspect-blend'):p.add_argument('--'+flag,action='store_true')
    o=p.parse_args(args);assert sum(vars(o).values())==1
    if o.snapshot:snapshot()
    elif o.build:build()
    elif o.verify:verify()
    else:inspect_blend()
