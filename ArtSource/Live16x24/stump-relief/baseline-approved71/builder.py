"""Scoped Stump floor/dome revision from immutable editable baseline cells.
Only writes ArtSource/Live16x24/stump-environment; never Assets or old sources.
Blender --background --threads 4 --python this-file -- --build / --inspect-blend
python3 this-file --snapshot / --verify. Pillow is read-only verification.
"""
import argparse,copy,hashlib,json,re,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/stump-environment'
BASE=ROOT/'ArtSource/Live16x24/stump-materials'
W,H,COUNT=16,24,71
TARGET=set(range(4))|{20,21}|set(range(56,71))
BASE_SOURCE=json.loads((BASE/'source-cells.json').read_text())
P=dict(BASE_SOURCE['palette'])
P.update({'env-floor':'A59BAF','env-floor-soft':'A094A9','env-floor-grain':'968B9E','env-floor-pale':'ADA2B5',
 'env-cap':'757386','env-cap-grain':'6E6D80','env-cap-worn':'7C7A8C','env-rim':'9690A3',
 'env-side':'5F6071','env-face':'535462','env-foot':'444652'})

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(n,v):(OUT/n).write_text(json.dumps(v,indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for name,h in pins.items():assert sha(ROOT/name)==h,name
    return len(pins)
def snapshot():
    import shutil
    assert not (OUT/'protected-inputs.json').exists(),'Never overwrite original input pins'
    d=OUT/'baseline-approved71';d.mkdir(exist_ok=False)
    for name in ('source-cells.json','palette.json','manifest.json','stump-materials.blend'):shutil.copy2(BASE/name,d/name)
    shutil.copy2(ROOT/'Assets/Resources/Sprites/Live16x24/stump-materials.png',d/'stump-materials.png')
    shutil.copy2(ROOT/'Tools/Live16x24/build_stump_materials.py',d/'builder.py')
    paths=list(d.iterdir())+[BASE/n for n in ('source-cells.json','palette.json','manifest.json','stump-materials.blend')]
    paths += [ROOT/'Tools/Live16x24'/n for n in ('build_stump_materials.py','build_cindercaller.py','build_opening_base.py','build_north_plot.py','build_limestone_caves.py')]
    paths += [OUT/n for n in ('scene-reference-01.png','scene-prompt-01.txt','parts-critique.md')]
    paths += [ROOT/'Docs/Verification/Live16x24'/n for n in ('stump-environment-before-01-summit.png','stump-environment-before-04-gorge.png')]
    save('protected-inputs.json',{str(p.relative_to(ROOT)):sha(p) for p in paths})
    save('baseline-provenance.json',{'original_runtime_path':'Assets/Resources/Sprites/Live16x24/stump-materials.png','sha256':sha(d/'stump-materials.png'),'original_editable_source':str(BASE.relative_to(ROOT)),'old_source_unchanged':True,'runtime_copy_managed_separately_by_root':True})

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

def floor(f,v):
    f.rect('quiet-worn-rose-plane','env-floor',0,0,W,H)
    # Dominant frame0 is truly quiet. Other three contain unequal shallow
    # erosion fields, with no isolated dash, circle, grain grid or raised prop.
    soft=[[],[(5,2,4),(6,2,7),(7,1,8),(8,2,8),(9,4,7),(10,5,5),(11,7,3)],
          [(13,8,3),(14,6,6),(15,6,7),(16,5,8),(17,7,6),(18,9,3)],
          [(4,9,3),(5,8,5),(6,8,6),(7,9,4),(16,2,4),(17,2,6),(18,3,5)]]
    f.rows('shallow-soft-fossil-field','env-floor-soft',soft[v])
    if v==1:f.rows('worn-fossil-grain','env-floor-grain',[(7,3,3),(8,3,4),(9,5,2)])
    # Variant2 has no outlined/pale center: one shallow soft field only.
    if v==3:f.rows('worn-fossil-grain','env-floor-grain',[(5,10,2),(6,10,2),(17,3,3)])
    f.meta['description']='Original flat warm rose stone; quiet shared borders and unequal shallow soft grain, no raised ground object.'
    return f

def dome(f,v,mask=0):
    f.rect('one-connected-stone-cap','env-cap',0,0,W,H)
    if v:
        f.rows('quiet-cap-weathering','env-cap-grain',[(8,4,5),(9,3,7),(10,4,7),(11,6,4)])
    # Strongest relief occurs only at actual exposed edges, fully INSIDE
    # solid cells. No repeated tiers or artificial joints across the cap.
    if mask&1:
        f.rect('exposed-north-cap-rim','env-rim',0,0,W)
        f.rect('north-rim-soft-transition','env-cap-worn',0,1,W)
    if mask&8:
        f.rect('exposed-west-side','env-side',0,0,1,H)
        f.rect('west-cap-transition','env-cap-worn',1,0,1,H)
    if mask&2:
        f.rect('exposed-east-side','env-side',14,0,1,H)
        f.rect('exposed-east-contact','env-foot',15,0,1,H)
    if mask&4:
        f.rect('near-cap-worn-edge','env-cap-worn',0,19,W)
        f.rect('near-upper-face','env-side',0,20,W)
        f.rect('near-stone-face','env-face',0,21,W,2)
        f.rect('near-ground-contact','env-foot',0,23,W)
        f.rows('subdued-face-cleavage','env-foot',[(22,2,3),(22,10,2)])
    f.meta['description']='One connected low dome cap; exposed inside-cell lip and five-pixel south face, no projected shadow or new geometry.'
    return f

def frames():
    fs=[Frame(e) for e in BASE_SOURCE['frames']]
    for v in range(4):floor(fs[v],v)
    for v in range(2):dome(fs[20+v],v)
    for mask in range(1,16):dome(fs[55+mask],0,mask)
    return fs

def source():
    result=copy.deepcopy(BASE_SOURCE);result['palette']=P;result['frames']=[]
    for f in frames():e=copy.deepcopy(f.meta);e['cells']=f.cells;result['frames'].append(e)
    return result

def rgba(c):return tuple(bytes.fromhex(P[c[1]]))+(255,) if c else (0,0,0,0)
def validate(s,size,data,baseline):
    assert s==source() and size==(1136,24) and len(data)==1136*24*4
    for i,f in enumerate(frames()):
        if i not in TARGET:assert s['frames'][i]==BASE_SOURCE['frames'][i]
        for y,row in enumerate(f.cells):
            for x,c in enumerate(row):
                at=4*(y*1136+i*16+x);assert tuple(data[at:at+4])==rgba(c),(i,x,y)
                if i not in TARGET:assert data[at:at+4]==baseline[at:at+4],('untouched',i,x,y)
                else:assert c is not None
    for i in range(4):
        c=s['frames'][i]['cells'];assert all(c[y][x][1]=='env-floor' for y in range(H) for x in range(W) if x in (0,15) or y in (0,23))
    assert all(c[1]=='env-floor' for row in s['frames'][0]['cells'] for c in row)
    for mask in range(1,16):
        f=s['frames'][55+mask];assert f['state']['exposedMask']==mask
        c=f['cells']
        if not mask&1:assert all(c[0][x][1]=='env-cap' for x in range(3,13))
        if not mask&4:assert all(c[23][x][1]=='env-cap' for x in range(3,13))
        if not mask&8:assert all(c[y][0][1]=='env-cap' for y in range(3,19))
        if not mask&2:assert all(c[y][15][1]=='env-cap' for y in range(3,19))
    return {'native_pixels':27264,'changed_frames_allowed':21,'unchanged_frames':50,'unchanged_pixels':19200}

def variant(x,y):
    n=((x*0x9e3779b1)^(y*0x85ebca77))&0xffffffff;n^=n>>16;n=n*0x7feb352d&0xffffffff;n^=n>>15
    return 0 if n&15<12 else 1+(n>>4)%3

def placements(kind):
    if kind=='contact':return 336,300,[{'frame':i,'topLeft':[8+i%10*32,18+i//10*35]} for i in range(COUNT)]
    occupied={(x,y) for y in range(2,7) for x in range(2 if y in (3,4,5) else 3,8 if y in (3,4,5) else 7)}
    occupied|={(x,y) for y in range(6,11) for x in range(13 if y in (7,8,9) else 14,19 if y in (7,8,9) else 18)}
    cells=[]
    for y in range(12):
        for x in range(20):
            fr=variant(x,y)
            if (x,y) in occupied:
                mask=sum(b for b,q in ((1,(x,y-1)),(2,(x+1,y)),(4,(x,y+1)),(8,(x-1,y))) if q not in occupied)
                fr=55+mask if mask else 20+(variant(x,y)!=0)
            cells.append({'frame':fr,'topLeft':[x*16,18+y*24]})
    return 320,306,cells

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for,add_label,emission_material
    from build_north_plot import instance,plane
    from build_limestone_caves import canonicalize_timing
    guard();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Stump environment71 native1136x24';cols=[]
    for i,f in enumerate(frames()):
        col=model(native,f'{i:02d} '+f.name,f.cells,P);native.collection.children.unlink(col)
        for o in col.objects:o['frameIndex']=i
        instance(native,col,str(i),i*W,0);cols.append(col)
    camera_for(native,1136,24);native.render.filepath=str(OUT/'stump-materials.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'stump-materials.png')
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',3,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
        width,height,ps=placements(kind);scene=bpy.data.scenes.new('Stump source '+kind+' '+str(scale));camera_for(scene,width,height,scale)
        plane(scene,'Source surround','332E35',0,0,width,height,-2)
        label=emission_material('Source label','D0C0C2');add_label(scene,'STUMP / '+('71 CELLS / 21 REVISED' if kind=='contact' else 'JOINED SOURCE STUDY / NOT GAMEPLAY'),width/2,height-7,label,1.6)
        for p in ps:
            i=p['frame'];x,y=p['topLeft'];instance(scene,cols[i],str(p),x,height-y-H)
            if kind=='contact':add_label(scene,str(i),x+8,height-y-H-4,label,1.4)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',source());save('palette.json',P)
    save('manifest.json',{'frames':71,'size':[1136,24],'target_frames':sorted(TARGET),'unchanged_frames':sorted(set(range(COUNT))-TARGET),'exposed_bits':{'N':1,'E':2,'S':4,'W':8},'source_only':True,'baseline_png_sha256':sha(OUT/'baseline-approved71/stump-materials.png'),'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','no_gameplay_geometry_change':True})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def verify():
    from PIL import Image
    count=guard();s=json.loads((OUT/'source-cells.json').read_text())
    with Image.open(OUT/'baseline-approved71/stump-materials.png') as im:baseline=im.convert('RGBA').tobytes()
    with Image.open(OUT/'stump-materials.png') as im:assert im.mode=='RGBA';data=im.tobytes();stats=validate(s,im.size,data,baseline)
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
    for name,fr,x,y,ch,v in [('changed-untouched-wall',4,4,4,0,255),('transparent-solid-dome',60,8,12,3,0),('quiet-ground-stamp',0,7,7,0,0),('wrong-dome-rim',56,7,0,0,0)]:
        bad=bytearray(data);at=4*(y*1136+fr*16+x)+ch;assert bad[at]!=v;bad[at]=v
        try:validate(s,(1136,24),bad,baseline)
        except AssertionError:rejected.append(name)
        else:raise AssertionError(name)
    save('source-verification.json',{'passed':True,**stats,'contact_and_pattern_opaque_pixels':checked,'corruptions_rejected':rejected,'png_sha256':sha(OUT/'stump-materials.png'),'blend_sha256':sha(OUT/'editable.blend'),'protected_inputs_unchanged':count,'actual_gameplay_acceptance':False});print(json.dumps(stats))

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
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,'frames':71,'unit_quads':quads,'named_mesh_groups':groups,'exact_parts_palette_coordinates':True,'blend_sha256':sha(Path(bpy.data.filepath)),'actual_gameplay_acceptance':False});print('Exact reopened geometry',quads,groups)

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    p=argparse.ArgumentParser(description=__doc__)
    for flag in ('snapshot','build','verify','inspect-blend'):p.add_argument('--'+flag,action='store_true')
    o=p.parse_args(args);assert sum(vars(o).values())==1
    if o.snapshot:snapshot()
    elif o.build:build()
    elif o.verify:verify()
    else:inspect_blend()
