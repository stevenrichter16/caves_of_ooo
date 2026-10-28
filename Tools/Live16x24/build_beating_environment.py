"""Scoped Beating sand/paving/wall/road revision from immutable editable baseline cells.
Only writes ArtSource/Live16x24/beating-environment; never Assets or old sources.
Blender --background --threads 4 --python this-file -- --build / --inspect-blend
python3 this-file --snapshot / --verify. Pillow is read-only verification.
"""
import argparse,copy,hashlib,json,re,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/beating-environment'
BASE=ROOT/'ArtSource/Live16x24/beating-materials'
W,H,COUNT=16,24,34
TARGET=set(range(4))|set(range(12,18))|{32,33}
BASE_SOURCE=json.loads((BASE/'source-cells.json').read_text())
P=dict(BASE_SOURCE['palette'])
P.update({'env-sand-soft':'785B38','env-sand-shade':'705533',
 'env-floor-soft':'856F50','env-floor-recess':'7A6549',
 'env-road-soft':'6D6858','env-road-shade':'646050',
 'env-wall-face':'9A7B55','env-wall-mid':'8A6C49','env-wall-shade':'745B3D',
 'env-wall-mortar':'584730','env-wall-light':'B29B73'})

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(n,v):(OUT/n).write_text(json.dumps(v,indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for name,h in pins.items():assert sha(ROOT/name)==h,name
    return len(pins)
def snapshot():
    assert not (OUT/'protected-inputs.json').exists(),'Never overwrite original input pins'
    evidence=ROOT/'Docs/Verification/Live16x24/beating-environment-baseline-pins.json'
    captured=json.loads(evidence.read_text())
    for name,h in captured.items():assert sha(ROOT/name)==h,name
    paths=[BASE/n for n in ('source-cells.json','manifest.json','beating-materials.blend','beating-materials.png')]
    paths += [ROOT/'Tools/Live16x24'/n for n in ('build_beating_materials.py','build_stump_environment.py','build_cindercaller.py','build_opening_base.py','build_north_plot.py','build_limestone_caves.py')]
    paths += [OUT/n for n in ('scene-reference-01.png','scene-prompt-01.txt','parts-critique.md')]
    paths += [ROOT/'Docs/Verification/Live16x24'/n for n in ('beating-environment-baseline-pins.json','beating-environment-before-01-dune.png','beating-environment-before-02-ruin.png')]
    paths += [ROOT/'Assets/Scripts/Presentation/Rendering'/n for n in ('FlatPixelBeatingMaterialsCatalog.cs','FlatPixelShallowCavesCatalog.cs')]
    pins={str(p.relative_to(ROOT)):sha(p) for p in paths}
    for name,h in pins.items():
        if name in captured:assert h==captured[name]
    save('protected-inputs.json',pins)
    save('baseline-provenance.json',{'root_baseline_receipt':str(evidence.relative_to(ROOT)),
      'original_runtime_png_sha256':captured['Assets/Resources/Sprites/Live16x24/beating-materials.png'],
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

def sand(f,v):
    f.rect('quiet-walking-sand','sand',0,0,W,H)
    fields=[[],[(4,3,4),(5,2,7),(6,2,8),(7,4,7),(8,7,3)],
      [(13,8,3),(14,5,7),(15,4,9),(16,6,7),(17,8,4)],
      [(4,9,3),(5,7,5),(6,8,5),(16,2,4),(17,2,7),(18,4,5)]]
    f.rows('low-soft-sand-wear','env-sand-soft',fields[v])
    f.meta['description']='Quiet warm sand with broad low-contrast surface wear; no repeated wind slashes, piles or pebble motifs.'
    return f

def floor(f,v):
    f.rect('warm-flat-paving','floor',0,0,W,H)
    fields=[[],[(6,3,4),(7,2,7),(8,3,8),(9,5,7),(10,8,3)],
      [(14,7,5),(15,5,8),(16,4,9),(17,7,5)],
      [(4,8,4),(5,7,6),(6,8,5),(15,2,4),(16,2,6),(17,3,3)]]
    f.rows('shallow-paving-wear','env-floor-soft',fields[v])
    joins=[[],[(17,4,3),(18,9,2)],[(6,3,2),(8,8,3)],[(11,3,2),(12,10,2)]]
    f.rows('subdued-broken-floor-join','env-floor-recess',joins[v])
    f.meta['description']='Flat worn sandstone; unchanged 806A4C base matches shallow ambient backing. No raised slabs or tile rim.'
    return f

def wall(f,v):
    f.rect('recessed-masonry-core','env-wall-mortar',0,0,W,H)
    blocks=[[(0,0,10,7),(11,0,5,7),(0,8,5,8),(6,8,10,8),(0,17,11,7),(12,17,4,7)],
      [(0,0,6,7),(7,0,9,7),(0,8,11,8),(12,8,4,8),(0,17,5,7),(6,17,10,7)]][v]
    for k,(x,y,w,h) in enumerate(blocks):
        f.rect('broad-warm-stone-face','env-wall-face',x,y,w,h)
        f.rect('lower-stone-plane','env-wall-mid',x,y+h-3,w,2)
        f.rect('lower-mortar-contact','env-wall-shade',x,y+h-1,w)
        # Worn short highlights sit on stone faces, never on a claimed exposed cell edge.
        if w>5:f.rect('short-worn-stone-light','env-wall-light',x+1,y+1,w-3)
        if w>8:f.rect('subdued-side-plane','env-wall-mid',x+w-1,y+2,1,h-3)
    f.meta['description']='Solid warm masonry with broad faces, lower planes and recessed mortar. Fully opaque straight footprint; internal relief only, no exposure mask or new geometry.'
    return f

def road(f,v):
    f.rect('compact-gravel-road','road',0,0,W,H)
    fields=[[(4,3,5),(5,2,7),(6,4,5),(15,9,3),(16,7,6),(17,9,4)],
      [(6,9,3),(7,7,6),(8,9,4),(16,2,5),(17,2,7),(18,5,4)]]
    f.rows('flat-packed-dust-wear','env-road-soft',fields[v])
    f.meta['description']='Flat packed warm grey-brown road with soft low-contrast dust fields; no recurring pebble dots, pits or raised cobbles.'
    return f

def frames():
    fs=[Frame(e) for e in BASE_SOURCE['frames']]
    for v in range(4):sand(fs[v],v);floor(fs[12+v],v)
    for v in range(2):wall(fs[16+v],v);road(fs[32+v],v)
    return fs

def source():
    result=copy.deepcopy(BASE_SOURCE);result['palette']=P;result['frames']=[]
    for f in frames():e=copy.deepcopy(f.meta);e['cells']=f.cells;result['frames'].append(e)
    return result

def rgba(c):return tuple(bytes.fromhex(P[c[1]]))+(255,) if c else (0,0,0,0)
def validate(s,size,data,baseline):
    assert s==source() and size==(544,24) and len(data)==544*24*4
    for key,value in BASE_SOURCE['palette'].items():assert P[key]==value
    for i,f in enumerate(frames()):
        if i not in TARGET:assert s['frames'][i]==BASE_SOURCE['frames'][i]
        for y,row in enumerate(f.cells):
            for x,c in enumerate(row):
                at=4*(y*544+i*16+x);assert tuple(data[at:at+4])==rgba(c),(i,x,y)
                if i not in TARGET:assert data[at:at+4]==baseline[at:at+4],('untouched',i,x,y)
                else:assert c is not None
    for i,key in [(i,'sand') for i in range(4)]+[(i,'floor') for i in range(12,16)]+[(32,'road'),(33,'road')]:
        c=s['frames'][i]['cells'];assert all(c[y][x][1]==key for y in range(H) for x in range(W) if x in (0,15) or y in (0,23))
    assert all(c[1]=='sand' for row in s['frames'][0]['cells'] for c in row)
    assert all(c[1]=='floor' for row in s['frames'][12]['cells'] for c in row)
    assert P['floor']=='806A4C'
    return {'native_pixels':13056,'changed_frames_allowed':12,'unchanged_frames':22,'unchanged_pixels':8448,'floor_base_matches_shallow_ambient':True}

def variant(x,y):
    n=((x*0x9e3779b1)^(y*0x85ebca77))&0xffffffff;n^=n>>16;n=n*0x7feb352d&0xffffffff;n^=n>>15
    return 0 if n&15<12 else 1+(n>>4)%3

def placements(kind):
    if kind=='contact':return 272,198,[{'frame':i,'topLeft':[8+i%8*32,18+i//8*35]} for i in range(COUNT)]
    cells=[]
    for y in range(12):
        for x in range(20):
            fr=variant(x,y)
            if 5<=x<=14 and 3<=y<=9:fr=12+variant(x,y)
            if y in (3,9) and 5<=x<=14 or x in (5,14) and 3<=y<=9:fr=16+((x+y)&1)
            if y in (6,7):fr=32+((x+y)&1)
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
    native=bpy.context.scene;native.name='Beating environment34 native544x24';cols=[]
    for i,f in enumerate(frames()):
        col=model(native,f'{i:02d} '+f.name,f.cells,P);native.collection.children.unlink(col)
        for o in col.objects:o['frameIndex']=i
        instance(native,col,str(i),i*W,0);cols.append(col)
    camera_for(native,544,24);native.render.filepath=str(OUT/'beating-materials.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'beating-materials.png')
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',3,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
        width,height,ps=placements(kind);scene=bpy.data.scenes.new('Beating source '+kind+' '+str(scale));camera_for(scene,width,height,scale)
        plane(scene,'Source surround','302D26',0,0,width,height,-2)
        label=emission_material('Source label','D2C7A6');add_label(scene,'BEATING / '+('34 CELLS / 12 REVISED' if kind=='contact' else 'JOINED SOURCE STUDY / NOT GAMEPLAY'),width/2,height-7,label,1.6)
        for p in ps:
            i=p['frame'];x,y=p['topLeft'];instance(scene,cols[i],str(p),x,height-y-H)
            if kind=='contact':add_label(scene,str(i),x+8,height-y-H-4,label,1.4)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',source());save('palette.json',P)
    save('manifest.json',{'frames':34,'size':[544,24],'target_frames':sorted(TARGET),'unchanged_frames':sorted(set(range(COUNT))-TARGET),'source_only':True,'baseline_png_sha256':sha(BASE/'beating-materials.png'),'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','no_gameplay_geometry_change':True})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def verify():
    from PIL import Image
    count=guard();s=json.loads((OUT/'source-cells.json').read_text())
    with Image.open(BASE/'beating-materials.png') as im:baseline=im.convert('RGBA').tobytes()
    with Image.open(OUT/'beating-materials.png') as im:assert im.mode=='RGBA';data=im.tobytes();stats=validate(s,im.size,data,baseline)
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
    for name,fr,x,y,ch,v in [('changed-untouched-salt',4,4,4,0,255),('transparent-solid-wall',16,8,12,3,0),('quiet-sand-stamp',0,7,7,0,0),('shallow-floor-base-drift',12,7,0,0,0)]:
        bad=bytearray(data);at=4*(y*544+fr*16+x)+ch;assert bad[at]!=v;bad[at]=v
        try:validate(s,(544,24),bad,baseline)
        except AssertionError:rejected.append(name)
        else:raise AssertionError(name)
    save('source-verification.json',{'passed':True,**stats,'contact_and_pattern_opaque_pixels':checked,'corruptions_rejected':rejected,'png_sha256':sha(OUT/'beating-materials.png'),'blend_sha256':sha(OUT/'editable.blend'),'protected_inputs_unchanged':count,'actual_gameplay_acceptance':False});print(json.dumps(stats))

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
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,'frames':34,'unit_quads':quads,'named_mesh_groups':groups,'exact_parts_palette_coordinates':True,'blend_sha256':sha(Path(bpy.data.filepath)),'actual_gameplay_acceptance':False});print('Exact reopened geometry',quads,groups)

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    p=argparse.ArgumentParser(description=__doc__)
    for flag in ('snapshot','build','verify','inspect-blend'):p.add_argument('--'+flag,action='store_true')
    o=p.parse_args(args);assert sum(vars(o).values())==1
    if o.snapshot:snapshot()
    elif o.build:build()
    elif o.verify:verify()
    else:inspect_blend()
