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
P={'ground':'3D492E','quiet-mat':'414C30','moss-soft':'454F33','moss':'4D593A','moss-dark':'39452C','sedge':'515D3D','straw':'545D3D',
   'wet':'303B37','deep':'2D3733','film':'3A453D','interior-film':'34403B','film-light':'3D493F',
   'shore':'52543B','shore-light':'626345','shore-dark':'28312C'}
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
    # Soft filled mats have neither a dark center nor a light perimeter:
    # those arrangements previously read as repeated loops/slashes in Play.
    # Frame0 contains no high-contrast mark. Placements and proportions vary.
    soft=[[(10,7,5),(11,4,9),(12,2,11),(13,3,11),(14,2,12),(15,4,9),(16,6,6),(17,9,3)],
          [(4,3,3),(5,2,6),(6,2,5),(7,1,7),(8,2,6),(9,3,4),(10,4,2),
           (17,10,3),(18,10,4),(19,11,2)],
          [(3,2,2),(4,2,4),(5,3,3),(13,9,3),(14,7,6),(15,8,6),
           (16,7,7),(17,8,5),(18,9,4),(19,10,2)],
          [(5,9,3),(6,8,5),(7,8,6),(8,10,4),(9,11,2),
           (16,3,3),(17,2,6),(18,2,5),(19,3,4),(20,4,2)]]
    core=[[],[(5,4,2),(6,3,4),(7,3,3),(8,4,2)],
          [(14,9,2),(15,9,4),(16,8,4),(17,9,2)],
          [(6,10,2),(7,9,3),(8,11,1),(17,3,3),(18,3,2)]]
    f.rows('soft-filled-moss-mat','quiet-mat' if v==0 else 'moss-soft',soft[v])
    f.rows('unequal-filled-moss-core','moss',core[v])
    recess=[[],[(13,11,2),(14,10,3)],
            [(8,6,3),(9,7,2)],[(12,3,2),(13,2,3),(14,3,1)]]
    f.rows('subdued-matted-recess','moss-dark',recess[v])
    if v==1:f.rows('soft-sedge-core','sedge',[(6,4,2),(7,4,1)])
    if v==2:f.rows('soft-sedge-core','sedge',[(15,10,2),(16,9,2)])
    if v==3:f.rows('soft-sedge-core','sedge',[(7,10,1),(17,4,1)])
    return f

def wet(depleted,mask):
    f=Frame(('depleted-wet-film-' if depleted else 'filled-mire-')+format(mask,'X'),'MirePool',connectedMask=mask,depleted=depleted,stillWet=True)
    f.rect('continuous-wet-peat','wet',0,0,W,H)
    # Lifted, desaturated green-gray wet peat remains visibly separate from
    # void. Wider interrupted level film has restrained material contrast.
    if depleted:
        f.rows('subdued-broad-wet-film','deep',[(6,0,6),(7,0,9),(8,0,12),(9,0,14),(10,0,16),
            (11,0,16),(12,2,14),(13,4,12),(14,7,9),(15,10,6)])
        f.rows('thin-level-wet-film','interior-film',[(5,5,3),(6,4,6),(7,6,3),(17,2,2),(18,3,1)])
    else:
        f.rows('subdued-level-wet-film','interior-film',[(8,3,3),(9,2,6),(10,4,3),(18,11,1)])
    # The shallow shore stays INSIDE the hazard cell. Connected sides have
    # no shore, no grass and no outer stroke, including mixed stock joins.
    profileN=(2,2,3,3,2,2,2,3,3,2,2,3,2,2,2,2)
    profileS=(2,2,2,3,2,2,3,3,2,2,2,3,3,2,2,2)
    profileE=(2,2,2,3,3,2,2,2,3,2,2,3,3,2,2,2,3,3,2,2,2,3,2,2)
    profileW=(2,2,3,2,2,3,3,2,2,2,3,2,2,2,3,3,2,2,3,2,2,2,2,2)
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
            # One dark contact row seats the peat lip; the next restrained
            # film is entirely within this same existing wet cell.
            x,y=(t,depth) if side=='N' else (t,H-1-depth) if side=='S' else (W-1-depth,t) if side=='E' else (depth,t)
            f.rect('wet-contact-'+side,'shore-dark',x,y)
            film_width=2 if t in ((3,4,5,11,12) if horizontal else (5,6,7,15,16)) else 1
            for d in range(depth+1,depth+1+film_width):
                x,y=(t,d) if side=='N' else (t,H-1-d) if side=='S' else (W-1-d,t) if side=='E' else (d,t)
                f.rect('inside-wet-film-'+side,'film-light',x,y)
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
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,'frames':36,'unit_quads':quads,'named_mesh_groups':groups,'exact_parts_palette_coordinates':True,'blend_sha256':sha(Path(bpy.data.filepath)),'runtime_or_visual_gameplay_acceptance':False})
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
