"""Nine original native Sill turf, floor and wall cells. Source only; no Assets writes.
Blender --background --threads 4 --python this-file -- --build / --inspect-blend
python3 this-file --snapshot / --verify. Pillow only verifies rendered exports.
"""
import argparse,hashlib,json,re,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/sill-environment'
W,H=16,24
P={'turf':'344639','turf-soft':'3B4C3F','turf-worn':'3A4636',
 'floor':'605E53','floor-soft':'676559','floor-recess':'59584F',
 'mortar':'35443D','stone':'6C7569','stone-lower':'59665C','stone-contact':'45554C','stone-light':'8A917F'}

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(n,v):(OUT/n).write_text(json.dumps(v,indent=2)+'\n')
def guard():
    pins=json.loads((OUT/'protected-inputs.json').read_text())
    for name,h in pins.items():assert sha(ROOT/name)==h,name
    return len(pins)
def snapshot():
    assert not (OUT/'protected-inputs.json').exists(),'Never replace baseline pins'
    approved=json.loads((OUT/'approved-pins.json').read_text())
    for name,h in approved.items():assert sha(ROOT/name)==h,name
    paths=[ROOT/name for name in approved]
    paths += [ROOT/'Tools/Live16x24'/n for n in ('build_grovelands_environment.py','build_cindercaller.py','build_opening_base.py','build_north_plot.py','build_limestone_caves.py')]
    paths += [OUT/n for n in ('scene-reference-01.png','prompt.txt','critique.md','approved-pins.json')]
    paths += [ROOT/'Docs/Verification/Live16x24'/n for n in ('sill-environment-before-01-exterior.png','sill-environment-before-03-interior.png')]
    pins={str(p.relative_to(ROOT)):sha(p) for p in paths}
    for name,h in approved.items():assert pins[name]==h
    save('protected-inputs.json',pins)

class Frame:
    def __init__(self,name,owner,base):self.name=name;self.owner=owner;self.cells=[[['quiet-'+owner,base] for x in range(W)] for y in range(H)]
    def rect(self,part,color,x,y,w=1,h=1):
        assert color in P and 0<=y<y+h<=H and 0<=x<x+w<=W
        for yy in range(y,y+h):
            for xx in range(x,x+w):self.cells[yy][xx]=[part,color]
        return self
    def rows(self,part,color,rows):
        for y,x,w in rows:self.rect(part,color,x,y,w)
        return self

def frames():
    fs=[]
    fields=[[],[(4,3,4),(5,2,7),(6,2,8),(7,4,7),(8,6,5),(9,8,2)],
      [(12,9,3),(13,6,6),(14,4,9),(15,5,8),(16,8,4)],
      [(4,9,3),(5,7,6),(6,8,5),(15,2,4),(16,2,7),(17,4,5)]]
    for v in range(4):
        f=Frame('quiet-matte-turf-'+str(v),'Grass','turf')
        f.rows('flat-low-turf-mottle','turf-soft',fields[v])
        if v==1:f.rows('subdued-turf-wear','turf-worn',[(17,10,3),(18,8,5),(19,9,3)])
        if v==2:f.rows('subdued-turf-wear','turf-worn',[(4,3,3),(5,2,5),(6,4,2)])
        fs.append(f)
    wear=[[],[(5,3,5),(6,2,8),(7,4,8),(8,6,4)],
      [(13,8,4),(14,5,8),(15,4,9),(16,7,5)],
      [(4,9,3),(5,7,6),(6,9,3),(16,3,4),(17,2,7),(18,4,4)]]
    seams=[[],[(17,4,3),(18,10,2)],[(5,3,3),(7,8,2)],[(11,4,2),(12,10,2)]]
    for v in range(4):
        f=Frame('flat-worn-stone-floor-'+str(v),'StoneFloor','floor')
        f.rows('soft-flat-floor-wear','floor-soft',wear[v]).rows('faint-broken-floor-join','floor-recess',seams[v]);fs.append(f)
    wall=Frame('intact-layered-stone-wall','StoneWall','mortar')
    for x,y,w,h in [(0,0,9,7),(10,0,6,7),(0,8,5,8),(6,8,10,8),(0,17,11,7),(12,17,4,7)]:
        wall.rect('broad-stone-face','stone',x,y,w,h)
        wall.rect('lower-stone-plane','stone-lower',x,y+h-3,w,2)
        wall.rect('stone-mortar-contact','stone-contact',x,y+h-1,w)
        if w>5:wall.rect('restrained-worn-stone-light','stone-light',x+1,y+1,w-3)
    fs.append(wall)
    return fs

def source():return {'coordinates':'Top-left y down; original named unit quads','palette':P,'frames':[{'index':i,'name':f.name,'owner':f.owner,'cells':f.cells} for i,f in enumerate(frames())]}

def rgba(c):return tuple(bytes.fromhex(P[c[1]]))+(255,)
def validate(s,size,data):
    assert s==source() and size==(144,24) and len(data)==144*24*4
    for i,f in enumerate(frames()):
        for y,row in enumerate(f.cells):
            for x,c in enumerate(row):
                at=4*(y*144+i*16+x);assert tuple(data[at:at+4])==rgba(c),(i,x,y)
                if i<8 and (x in (0,15) or y in (0,23)):assert c[1]==('turf' if i<4 else 'floor')
        if i<8:assert sum(c[1]==('turf' if i<4 else 'floor') for row in f.cells for c in row)>=280
    for i,key in [(0,'turf'),(4,'floor')]:assert all(c[1]==key for row in s['frames'][i]['cells'] for c in row)
    return {'native_pixels':3456,'fully_opaque_frames':9,'quiet0_and4_exact_base':True,'all_ground_boundary_pixels_common_base':True}

def variant(x,y):
    n=((x*0x9e3779b1)^(y*0x85ebca77))&0xffffffff;n^=n>>16;n=n*0x7feb352d&0xffffffff;n^=n>>15
    return 0 if n&15<12 else 1+(n>>4)%3

def placements(kind):
    if kind=='contact':return 296,62,[{'frame':i,'topLeft':[8+i*32,18]} for i in range(9)]
    cells=[]
    for y in range(12):
        for x in range(20):
            fr=variant(x,y)
            if 5<=x<=14 and 3<=y<=9:fr=4+variant(x,y)
            if y in (3,9) and 5<=x<=14 or x in (5,14) and 3<=y<=9:fr=8
            if x in (9,10) and y==9:fr=4+variant(x,y)
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
    native=bpy.context.scene;native.name='Sill environment native144x24';cols=[]
    for i,f in enumerate(frames()):
        col=model(native,f'{i:02d} '+f.name,f.cells,P);native.collection.children.unlink(col)
        for o in col.objects:o['frameIndex']=i
        instance(native,col,str(i),i*W,0);cols.append(col)
    camera_for(native,144,24);native.render.filepath=str(OUT/'sill-environment.png');bpy.ops.render.render(write_still=True,scene=native.name);canonicalize_timing(OUT/'sill-environment.png')
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',3,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
        width,height,ps=placements(kind);scene=bpy.data.scenes.new('Sill source '+kind+' '+str(scale));camera_for(scene,width,height,scale)
        plane(scene,'Source surround','252F29',0,0,width,height,-2)
        label=emission_material('Source label','B8BBA8');add_label(scene,'SILL / '+('NINE MATERIAL CELLS' if kind=='contact' else 'SOURCE STUDY / NOT GAMEPLAY'),width/2,height-6,label,1.3)
        for p in ps:
            i=p['frame'];x,y=p['topLeft'];instance(scene,cols[i],str(p),x,height-y-H)
            if kind=='contact':add_label(scene,str(i),x+8,height-y-H-5,label,1.7)
        scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True,scene=scene.name);canonicalize_timing(OUT/name)
    save('source-cells.json',source());save('palette.json',P)
    save('manifest.json',{'frames':9,'size':[144,24],'resource':'Sprites/Live16x24/sill-environment','owners':['Grass','StoneFloor','StoneWall'],'source_only':True,'pixelsPerUnit':16,'pivot':[.5,.5],'filter':'Point','wrap':'Clamp','mipmaps':False,'compression':'None','old_art_unchanged':True,'no_geometry_or_gameplay_change':True})
    bpy.context.window.scene=native;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'editable.blend'));guard()

def verify():
    from PIL import Image
    count=guard();s=json.loads((OUT/'source-cells.json').read_text())
    with Image.open(OUT/'sill-environment.png') as im:assert im.mode=='RGBA';data=im.tobytes();stats=validate(s,im.size,data)
    checked=0
    for kind,scale,name in [('contact',1,'contact-native.png'),('contact',3,'contact.png'),('pattern',1,'scene-pattern-native.png'),('pattern',3,'scene-pattern.png')]:
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
    for name,fr,x,y,ch,v in [('quiet-turf-stamp',0,7,7,0,0),('transparent-wall',8,8,12,3,0),('floor-border-seam',6,0,8,0,255),('wrong-turf',1,4,6,1,255)]:
        bad=bytearray(data);at=4*(y*144+fr*16+x)+ch;assert bad[at]!=v;bad[at]=v
        try:validate(s,(144,24),bad)
        except AssertionError:rejected.append(name)
        else:raise AssertionError(name)
    save('source-verification.json',{'passed':True,**stats,'contact_and_pattern_pixels':checked,'corruptions_rejected':rejected,'png_sha256':sha(OUT/'sill-environment.png'),'blend_sha256':sha(OUT/'editable.blend'),'protected_inputs_unchanged':count,'actual_gameplay_acceptance':False});print(json.dumps(stats))

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
    save('blend-geometry-verification.json',{'passed':True,'saved_blend_reopened':True,'frames':9,'unit_quads':quads,'named_mesh_groups':groups,'exact_parts_palette_coordinates':True,'blend_sha256':sha(Path(bpy.data.filepath)),'actual_gameplay_acceptance':False});print('Exact reopened geometry',quads,groups)

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    p=argparse.ArgumentParser(description=__doc__)
    for flag in ('snapshot','build','verify','inspect-blend'):p.add_argument('--'+flag,action='store_true')
    o=p.parse_args(args);assert sum(vars(o).values())==1
    if o.snapshot:snapshot()
    elif o.build:build()
    elif o.verify:verify()
    else:inspect_blend()
