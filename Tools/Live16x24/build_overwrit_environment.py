"""Scoped Overwrit ground revision from immutable editable baseline cells.
Only writes ArtSource/Live16x24/overwrit-environment; never Assets or old sources.
Blender --background --threads 4 --python this-file -- --build / --inspect-blend
python3 this-file --snapshot / --verify. Pillow is read-only verification.
"""
import argparse,copy,hashlib,json,re,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/overwrit-environment'
BASE=ROOT/'ArtSource/Live16x24/overwrit-materials'
W,H,COUNT=16,24,8
TARGET=set(range(4))
BASE_SOURCE=json.loads((BASE/'source-cells.json').read_text())
P=dict(BASE_SOURCE['palette'])
P.update({'env-wear':'A29D8E','env-polish':'AAA595'})

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(n,v):(OUT/n).write_text(json.dumps(v,indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for name,h in pins.items():assert sha(ROOT/name)==h,name
    return len(pins)
def snapshot():
    assert not (OUT/'protected-inputs.json').exists(),'Never overwrite original input pins'
    approved=BASE/'overwrit-materials.png'
    runtime=ROOT/'Assets/Resources/Sprites/Live16x24/overwrit-materials.png'
    assert sha(approved)==sha(runtime),'Current runtime must match approved baseline before starting'
    paths=[BASE/n for n in ('source-cells.json','manifest.json','overwrit-materials.blend','overwrit-materials.png')]
    paths += [ROOT/'Tools/Live16x24'/n for n in ('build_overwrit_materials.py','build_beating_environment.py','build_cindercaller.py','build_opening_base.py','build_north_plot.py','build_limestone_caves.py')]
    paths += [OUT/n for n in ('scene-reference-01.png','prompt.txt','critique.md')]
    paths += [ROOT/'Docs/Verification/Live16x24'/n for n in ('overwrit-environment-before-01-rim.png','overwrit-environment-before-02-blank.png')]
    paths += [ROOT/'Assets/Scripts/Presentation/Rendering/FlatPixelOverwritMaterialsCatalog.cs']
    save('protected-inputs.json',{str(p.relative_to(ROOT)):sha(p) for p in paths})
    save('baseline-provenance.json',{'original_runtime_png_sha256':sha(runtime),
      'runtime_png_not_an_active_guard':True,'reason':'Root owns authorized replacement/import; immutable approved source PNG remains pinned.',
      'old_source_preserved_in_place':True,'no_duplicate_old_atlas_packet':True})

class Frame:
    def __init__(self,entry):
        self.meta=copy.deepcopy(entry);self.name=entry['id'];self.cells=copy.deepcopy(entry['cells'])
    def rect(self,part,color,x,y,w=1,h=1):
        assert color in P and 0<=x<x+w<=W and 0<=y<y+h<=H
        for yy in range(y,y+h):
            for xx in range(x,x+w):self.cells[yy][xx]=[part,color]
        return self
    def rows(self,part,color,rows):
        for y,x,w in rows:self.rect(part,color,x,y,w)
        return self

def ground(f,v):
    f.rect('smooth-ground','ground',0,0,W,H)
    # Broken, shallow rubbed traces are distributed toward different margins.
    # No closed central island, highlighted rim or raised object is constructed.
    wear=[[],[(4,1,6),(5,2,9),(6,1,4),(6,8,6),(8,4,5),(10,10,4)],
      [(12,8,6),(13,2,8),(14,1,5),(14,9,5),(16,4,7),(17,8,4)],
      [(3,3,8),(4,1,5),(6,9,5),(16,1,8),(17,4,7),(19,10,4)]]
    scrape=[[],[(5,3,3),(6,11,2),(8,5,2)],
      [(13,4,3),(14,10,2),(16,5,2)],[(3,6,2),(16,2,3),(17,8,2)]]
    polish=[[],[(15,2,6),(16,5,3)],[(5,9,4)],[(10,3,5)]]
    f.rows('flat-rubbed-ground','env-wear',wear[v])
    f.rows('shallow-broken-abrasion','scuff',scrape[v])
    f.rows('subdued-erased-trace','env-polish',polish[v])
    return f

def frames():
    fs=[Frame(e) for e in BASE_SOURCE['frames']]
    for v in range(4):ground(fs[v],v)
    return fs

def source():
    result=copy.deepcopy(BASE_SOURCE);result['palette']=P;result['frames']=[]
    for f in frames():e=copy.deepcopy(f.meta);e['cells']=f.cells;result['frames'].append(e)
    return result

def rgba(c):return tuple(bytes.fromhex(P[c[1]]))+(255,) if c else (0,0,0,0)
def validate(s,size,data,baseline):
    assert s==source() and size==(128,24) and len(data)==128*24*4
    for key,value in BASE_SOURCE['palette'].items():assert P[key]==value
    for i,f in enumerate(frames()):
        if i not in TARGET:assert s['frames'][i]==BASE_SOURCE['frames'][i]
        for y,row in enumerate(f.cells):
            for x,c in enumerate(row):
                at=4*(y*128+i*16+x);assert tuple(data[at:at+4])==rgba(c),(i,x,y)
                if i not in TARGET:assert data[at:at+4]==baseline[at:at+4],('untouched',i,x,y)
                else:assert c is not None
        if i in TARGET:
            assert all(f.cells[y][x][1]=='ground' for y in range(H) for x in range(W) if x in (0,15) or y in (0,23))
            assert sum(c[1]=='ground' for row in f.cells for c in row)>=320
    assert s['frames'][0]==BASE_SOURCE['frames'][0]
    assert all(c[1]=='ground' for row in s['frames'][0]['cells'] for c in row)
    assert P['ground']=='A7A292'
    return {'native_pixels':3072,'changed_frames_allowed':4,'unchanged_body_frames':4,'unchanged_body_pixels':1536,'quiet0_exact':True,'base_A7A292_exact':True}

def variant(x,y):
    n=((x*0x9e3779b1)^(y*0x85ebca77))&0xffffffff;n^=n>>16;n=n*0x7feb352d&0xffffffff;n^=n>>15
    return 0 if n&15<12 else 1+(n>>4)%3

def placements(kind):
    if kind=='contact':return 272,62,[{'frame':i,'topLeft':[8+i*32,18]} for i in range(COUNT)]
    return 320,306,[{'frame':variant(x,y),'topLeft':[x*16,18+y*24]} for y in range(12) for x in range(20)]

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for,add_label,emission_material
    from build_north_plot import instance,plane
    from build_limestone_caves import canonicalize_timing
    guard();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Overwrit environment8 native128x24';cols=[]
    for i,f in enumerate(frames()):
        col=model(native,f'{i:02d} '+f.name,f.cells,P);native.collection.children.unlink(col)
        for o in col.objects:o['frameIndex']=i
        instance(native,col,str(i),i*W,0);cols.append(col)
    camera_for(native,128,24);native.render.filepath=str(OUT/'overwrit-materials.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'overwrit-materials.png')
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',3,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
        width,height,ps=placements(kind);scene=bpy.data.scenes.new('Overwrit source '+kind+' '+str(scale));camera_for(scene,width,height,scale)
        plane(scene,'Source surround','4D4D46',0,0,width,height,-2)
        label=emission_material('Source label','D0CEC2');add_label(scene,'OVERWRIT / '+('8 CELLS / GROUND ONLY' if kind=='contact' else 'JOINED SOURCE STUDY / NOT GAMEPLAY'),width/2,height-7,label,1.6)
        for p in ps:
            i=p['frame'];x,y=p['topLeft'];instance(scene,cols[i],str(p),x,height-y-H)
            if kind=='contact':add_label(scene,str(i),x+8,height-y-H-4,label,1.4)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',source());save('palette.json',P)
    save('manifest.json',{'frames':8,'size':[128,24],'target_frames':sorted(TARGET),'unchanged_frames':sorted(set(range(COUNT))-TARGET),'source_only':True,'baseline_png_sha256':sha(BASE/'overwrit-materials.png'),'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','no_gameplay_geometry_change':True})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def verify():
    from PIL import Image
    count=guard();s=json.loads((OUT/'source-cells.json').read_text())
    with Image.open(BASE/'overwrit-materials.png') as im:baseline=im.convert('RGBA').tobytes()
    with Image.open(OUT/'overwrit-materials.png') as im:assert im.mode=='RGBA';data=im.tobytes();stats=validate(s,im.size,data,baseline)
    checked=0
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',3,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
        width,height,ps=placements(kind)
        with Image.open(OUT/name) as im:
            assert im.size==(width*scale,height*scale);pix=im.load()
            for p in ps:
                x0,y0=p['topLeft'];c=s['frames'][p['frame']]['cells']
                for y,row in enumerate(c):
                    for x,v in enumerate(row):
                        if v is None:continue
                        for dy in range(scale):
                            for dx in range(scale):assert pix[(x0+x)*scale+dx,(y0+y)*scale+dy]==rgba(v);checked+=1
    rejected=[]
    for name,fr,x,y,ch,v in [('changed-untouched-bench',6,11,12,0,255),('transparent-ground',1,8,12,3,0),('quiet-ground-stamp',0,7,7,0,0),('ground-edge-seam',2,0,8,0,0)]:
        bad=bytearray(data);at=4*(y*128+fr*16+x)+ch;assert bad[at]!=v;bad[at]=v
        try:validate(s,(128,24),bad,baseline)
        except AssertionError:rejected.append(name)
        else:raise AssertionError(name)
    save('source-verification.json',{'passed':True,**stats,'contact_and_pattern_opaque_pixels':checked,'corruptions_rejected':rejected,'png_sha256':sha(OUT/'overwrit-materials.png'),'blend_sha256':sha(OUT/'editable.blend'),'protected_inputs_unchanged':count,'actual_gameplay_acceptance':False});print(json.dumps(stats))

def inspect_blend():
    import bpy
    guard();quads=groups=0
    for i,f in enumerate(frames()):
        col=bpy.data.collections[f'{i:02d} '+f.name];found={}
        for obj in col.objects:
            assert obj.type=='MESH' and obj['frameIndex']==i and tuple(obj.location)==(0,0,0) and tuple(obj.scale)==(1,1,1) and tuple(obj.rotation_euler)==(0,0,0)
            part=re.sub(r'\.\d+$','',obj.name);color=obj['paletteHex'];assert len(obj.data.materials)==1
            nodes=[n for n in obj.data.materials[0].node_tree.nodes if n.type=='EMISSION'];assert len(nodes)==1 and nodes[0].inputs['Strength'].default_value==1
            def linear(v):a=v/255;return a/12.92 if a<=.04045 else ((a+.055)/1.055)**2.4
            assert all(abs(nodes[0].inputs['Color'].default_value[k]-linear(v))<1e-6 for k,v in enumerate(bytes.fromhex(color)))
            for face in obj.data.polygons:
                vs=[obj.data.vertices[j].co for j in face.vertices];assert len(vs)==4 and all(v.z==0 and v.x==int(v.x) and v.y==int(v.y) for v in vs)
                x=min(v.x for v in vs);y=min(v.y for v in vs);assert max(v.x for v in vs)-x==max(v.y for v in vs)-y==1
                q=(int(x),23-int(y));assert q not in found;found[q]=(part,color);quads+=1
            groups+=1
        assert found=={(x,y):(v[0],P[v[1]]) for y,row in enumerate(f.cells) for x,v in enumerate(row) if v is not None}
    assert not bpy.data.actions
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,'frames':8,'unit_quads':quads,'named_mesh_groups':groups,'exact_parts_palette_coordinates':True,'blend_sha256':sha(Path(bpy.data.filepath)),'actual_gameplay_acceptance':False});print('Exact reopened geometry',quads,groups)

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    p=argparse.ArgumentParser(description=__doc__)
    for flag in ('snapshot','build','verify','inspect-blend'):p.add_argument('--'+flag,action='store_true')
    o=p.parse_args(args);assert sum(vars(o).values())==1
    if o.snapshot:snapshot()
    elif o.build:build()
    elif o.verify:verify()
    else:inspect_blend()
