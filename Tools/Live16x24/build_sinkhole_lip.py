"""Two original native sinkhole-lip cells: undercut, spray-wet rim rock with leaning moss.
Source only; no Assets writes.
Blender --background --threads 4 --python this-file -- --build / --inspect-blend
python3 this-file --snapshot / --verify. Pillow only verifies rendered exports.
"""
import argparse,hashlib,json,re,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/sinkhole-lip'
W,H=16,24
# A straight, layered rim block like the approved walls: a moss cap leaning in, a grey-brown
# rock face with flat strata, and a dark undercut base where the rock is eaten from below.
P={'moss':'4E6A39','moss-light':'64843F','rock':'6C675B','rock-light':'827C6D','rock-dark':'504C43','undercut':'2F2C27'}

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(n,v):(OUT/n).write_text(json.dumps(v,indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for name,h in pins.items():assert sha(ROOT/name)==h,name
    return len(pins)
def snapshot():
    assert not (OUT/'protected-inputs.json').exists(),'Never replace baseline pins'
    paths=[ROOT/'Assets/Resources/Sprites/Live16x24'/n for n in ('sill-environment.png','grovelands-ground.png','beating-materials.png','opening-ground.png','cellar-floor.png')]
    paths += [ROOT/'Tools/Live16x24'/n for n in ('build_grovelands_environment.py','build_cindercaller.py','build_opening_base.py','build_north_plot.py','build_limestone_caves.py')]
    paths += [ROOT/'Docs/Verification/Live16x24'/('special-site-'+n+'-offline.png') for n in ('cathedral','stillleaf')]
    save('protected-inputs.json',{str(p.relative_to(ROOT)):sha(p) for p in paths})

class Frame:
    def __init__(self,name):self.name=name;self.cells=[[['rim-rock','rock'] for x in range(W)] for y in range(H)]
    def rows(self,part,color,rows):
        for y,x,w in rows:
            assert color in P and 0<=y<H and 0<=x<x+w<=W
            for xx in range(x,x+w):self.cells[y][xx]=[part,color]
        return self

def frames():
    out=[]
    for i,(drips,strata,cracks) in enumerate([
        ([(3,0,5),(3,7,5),(3,13,3),(4,1,2),(4,9,2)],[(8,1,6),(8,9,6),(13,3,10)],[(10,11,1),(11,11,1),(15,4,1)]),
        ([(3,0,3),(3,5,6),(3,12,4),(4,6,2),(4,13,2)],[(7,2,9),(12,0,5),(12,8,7)],[(9,4,1),(10,4,1),(15,12,1)])]):
        f=Frame('sinkhole-lip-'+'ab'[i])
        f.rows('leaning-moss','moss',[(0,0,16),(1,0,16),(2,0,16)]+drips)
        f.rows('moss-light-tips','moss-light',[(0,2,3),(0,9,4),(1,5,2)] if i==0 else [(0,1,4),(0,10,3),(1,13,2)])
        f.rows('flat-strata','rock-light',strata)
        f.rows('crack','rock-dark',cracks)
        f.rows('undercut-shade','rock-dark',[(17,0,16),(18,0,16),(19,0,16)])
        f.rows('undercut','undercut',[(20,0,16),(21,0,16),(22,0,16),(23,0,16)])
        out.append(f)
    return out

def source():return {'coordinates':'Top-left y down; original named unit quads','palette':P,'frames':[{'index':i,'name':f.name,'owner':'SinkholeLip','cells':f.cells} for i,f in enumerate(frames())]}
def rgba(c):return tuple(bytes.fromhex(P[c[1]]))+(255,)
def validate(s,size,data):
    assert s==source() and size==(32,24) and len(data)==32*24*4
    for i,f in enumerate(frames()):
        for y,row in enumerate(f.cells):
            for x,c in enumerate(row):
                at=4*(y*32+i*16+x);assert tuple(data[at:at+4])==rgba(c),(i,x,y)
        assert all(c[1]=='undercut' for c in f.cells[23]) and all(c[1]=='moss' or c[1]=='moss-light' for c in f.cells[0])
    assert frames()[0].cells!=frames()[1].cells
    return {'native_pixels':768,'fully_opaque_frames':2,'moss_cap_and_undercut_base':True}

def variant(x,y):
    n=((x*0x9e3779b1)^(y*0x85ebca77))&0xffffffff;n^=n>>16;n=n*0x7feb352d&0xffffffff;n^=n>>15
    return 0 if n&15<12 else 1+(n>>4)%3

def placements(kind):
    if kind=='contact':return 58,58,[{'frame':i,'topLeft':[8+i*25,17]} for i in range(2)]
    return 320,258,[{'frame':(x+y)&1,'topLeft':[x*16,18+y*24]} for y in range(10) for x in range(20) if (x-10)**2/49+(y-5)**2/16>=0.72 and (x-10)**2/49+(y-5)**2/16<=1.0]

def build():
    import bpy
    sys.path.insert(0,str(ROOT/'Tools/Live16x24'))
    from build_cindercaller import model
    from build_opening_base import camera_for,add_label,emission_material
    from build_north_plot import instance,plane
    from build_limestone_caves import canonicalize_timing
    guard();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name='Sinkhole lip native32x24';cols=[]
    for i,f in enumerate(frames()):
        col=model(native,f'{i:02d} '+f.name,f.cells,P);native.collection.children.unlink(col)
        for o in col.objects:o['frameIndex']=i
        instance(native,col,str(i),i*W,0);cols.append(col)
    camera_for(native,32,24);native.render.filepath=str(OUT/'sinkhole-lip.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'sinkhole-lip.png')
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',8,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
        width,height,ps=placements(kind);scene=bpy.data.scenes.new('Sinkhole lip source '+kind+' '+str(scale));camera_for(scene,width,height,scale)
        plane(scene,'Source surround','24221E',0,0,width,height,-2)
        label=emission_material('Source label','B6B797');add_label(scene,'SINKHOLE LIP / '+('NATIVE CELLS' if kind=='contact' else 'RING STUDY / SOURCE ONLY'),width/2,height-6,label,1.3)
        for p in ps:
            i=p['frame'];x,y=p['topLeft'];instance(scene,cols[i],str(p),x,height-y-H)
            if kind=='contact':add_label(scene,str(i),x+8,height-y-H-5,label,1.7)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',source());save('palette.json',P)
    save('manifest.json',{'frames':2,'size':[32,24],'resource':'Sprites/Live16x24/sinkhole-lip','owner':'SinkholeLip','source_only':True,'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','old_art_unchanged':True,'no_geometry_or_gameplay_change':True})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def verify():
    from PIL import Image
    count=guard();s=json.loads((OUT/'source-cells.json').read_text())
    with Image.open(OUT/'sinkhole-lip.png') as im:assert im.mode=='RGBA';data=im.tobytes();stats=validate(s,im.size,data)
    checked=0
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',8,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
        width,height,ps=placements(kind)
        with Image.open(OUT/name) as im:
            assert im.size==(width*scale,height*scale);px=im.load()
            for p in ps:
                x0,y0=p['topLeft'];c=s['frames'][p['frame']]['cells']
                for y,row in enumerate(c):
                    for x,v in enumerate(row):
                        for dy in range(scale):
                            for dx in range(scale):assert px[(x0+x)*scale+dx,(y0+y)*scale+dy]==rgba(v);checked+=1
    rejected=[]
    for name,fr,x,y,ch,v in [('moss-cap-removed',0,7,0,1,0),('transparent-rock',1,8,12,3,0),('undercut-lightened',0,3,23,0,255),('wrong-strata',1,4,7,1,255)]:
        bad=bytearray(data);at=4*(y*32+fr*16+x)+ch;assert bad[at]!=v;bad[at]=v
        try:validate(s,(32,24),bad)
        except AssertionError:rejected.append(name)
        else:raise AssertionError(name)
    save('source-verification.json',{'passed':True,**stats,'contact_and_pattern_pixels':checked,'corruptions_rejected':rejected,'png_sha256':sha(OUT/'sinkhole-lip.png'),'blend_sha256':sha(OUT/'editable.blend'),'protected_inputs_unchanged':count,'actual_gameplay_acceptance':False});print(json.dumps(stats))

def inspect_blend():
    import bpy
    guard();quads=groups=0
    for i,f in enumerate(frames()):
        col=bpy.data.collections[f'{i:02d} '+f.name];found={}
        for obj in col.objects:
            assert obj.type=='MESH' and obj['frameIndex']==i and tuple(obj.location)==(0,0,0) and tuple(obj.scale)==(1,1,1) and tuple(obj.rotation_euler)==(0,0,0)
            part=re.sub(r'\.\d+$','',obj.name);color=obj['paletteHex'];assert len(obj.data.materials)==1
            ns=[n for n in obj.data.materials[0].node_tree.nodes if n.type=='EMISSION'];assert len(ns)==1 and ns[0].inputs['Strength'].default_value==1
            def linear(v):a=v/255;return a/12.92 if a<=.04045 else ((a+.055)/1.055)**2.4
            assert all(abs(ns[0].inputs['Color'].default_value[k]-linear(v))<1e-6 for k,v in enumerate(bytes.fromhex(color)))
            for face in obj.data.polygons:
                vs=[obj.data.vertices[j].co for j in face.vertices];assert len(vs)==4 and all(v.z==0 and v.x==int(v.x) and v.y==int(v.y) for v in vs)
                x=min(v.x for v in vs);y=min(v.y for v in vs);assert max(v.x for v in vs)-x==max(v.y for v in vs)-y==1
                q=(int(x),23-int(y));assert q not in found;found[q]=(part,color);quads+=1
            groups+=1
        assert found=={(x,y):(v[0],P[v[1]]) for y,row in enumerate(f.cells) for x,v in enumerate(row)}
    assert not bpy.data.actions
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,'frames':2,'unit_quads':quads,'named_mesh_groups':groups,'exact_parts_palette_coordinates':True,'blend_sha256':sha(Path(bpy.data.filepath)),'actual_gameplay_acceptance':False});print('Exact reopened geometry',quads,groups)

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    p=argparse.ArgumentParser(description=__doc__)
    for flag in ('snapshot','build','verify','inspect-blend'):p.add_argument('--'+flag,action='store_true')
    o=p.parse_args(args);assert sum(vars(o).values())==1
    if o.snapshot:snapshot()
    elif o.build:build()
    elif o.verify:verify()
    else:inspect_blend()
