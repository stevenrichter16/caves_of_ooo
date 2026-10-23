"""Concept-guided editable voxel props, rendered natively by Blender.

Run: /Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Clearing16x24/build_kit.py
No source raster is sampled, cropped, resized, or converted into a mesh.
Each colored cell below authors an extruded solid; Blender produces the PNGs.
"""
import bpy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Clearing16x24/kit"
OUT.mkdir(parents=True, exist_ok=True)

PALETTE = {
    "d": "203A31", "w": "54412A", "W": "806033", "h": "A98248",
    "c": "925035", "C": "B5673E", "o": "D28548", "f": "E9A731",
    "s": "48635E", "S": "68817A", "l": "8D9B87", "n": "102F28",
    "g": "4E6236", "G": "75834B", "e": "A7AA64", "b": "BCA576",
    "B": "E2CC90", "r": "6A4C34", "R": "947047", "t": "344B30",
    "u": "26382A", "U": "3B422E", "q": "50523A", "Q": "666247",
}

class Prop:
    def __init__(self, name, width=16, height=24, description=""):
        self.name, self.width, self.height, self.description = name, width, height, description
        self.cells = [[None] * width for _ in range(height)]
        self.parts = {}

    def rect(self, part, color, x, y, w, h, depth=2):
        assert color in PALETTE and w > 0 and h > 0
        assert x >= 0 and y >= 0 and x + w <= self.width and y + h <= self.height
        self.parts[part] = color
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.cells[yy][xx] = (part, color, depth)
        return self

    def dot(self, part, color, x, y, depth=2):
        return self.rect(part, color, x, y, 1, 1, depth)

    def erase(self, x, y, w, h):
        for yy in range(y, y+h):
            for xx in range(x, x+w):
                self.cells[yy][xx] = None
        return self


def make_props():
    props = []
    p = Prop("clay-oven", description="Dome, open dark mouth, warm coals and short chimney.")
    p.rect("chimney", "c", 6, 2, 4, 5, 3).rect("flue", "n", 7, 3, 2, 1, 3.5)
    for x,y,w,h in [(5,6,6,2),(3,8,10,3),(2,11,12,4),(1,15,14,7),(2,22,12,1)]: p.rect("clay-shell","C",x,y,w,h,3)
    p.rect("left-clay-shadow","c",1,15,2,7,3.2).rect("right-clay-shadow","c",13,15,2,7,3.2)
    p.rect("shoulder-highlight","o",5,7,4,1,3.3).rect("left-highlight","o",3,10,2,3,3.3)
    p.rect("mouth","n",5,14,6,2,3.5).rect("mouth","n",4,16,8,5,3.5)
    p.rect("hearth","c",4,21,8,2,4).rect("hearth-front","o",5,22,6,1,4.2)
    p.dot("coals","f",5,20,4).rect("coals","f",8,20,2,1,4)
    props.append(p)

    p = Prop("stone-well", description="Low stone ring with a dark open water interior; no tall roof.")
    p.rect("ring-shadow","s",2,12,12,9,3).rect("ring-shadow","s",3,11,10,11,3)
    p.rect("stone-body","S",3,13,10,8,3.2).rect("stone-body","S",4,21,8,2,3.2)
    p.rect("interior","n",4,12,8,5,4).rect("interior","n",3,13,10,3,4)
    p.rect("back-rim","l",5,10,6,2,4.3).rect("back-rim","S",3,11,2,2,4.2).rect("back-rim","S",11,11,2,2,4.2)
    p.rect("front-rim","l",5,17,5,2,4.3).rect("front-rim","S",3,16,2,3,4.2).rect("front-rim","S",10,16,3,3,4.2)
    p.rect("mortar","s",7,19,1,3,4.4).rect("mortar","s",4,20,3,1,4.4).rect("mortar","s",10,19,2,1,4.4)
    props.append(p)

    p = Prop("striped-seed-shelter",48,48,"Ochre and olive canvas canopy, two wooden posts; 48×48 pixels span 3×2 cells of 16×24 pixels.")
    p.rect("posts-shadow","w",3,7,3,40,3).rect("posts-shadow","w",42,7,3,40,3)
    p.rect("posts-light","h",3,8,1,38,3.2).rect("posts-light","h",42,8,1,38,3.2)
    p.rect("crossbar","W",3,11,42,2,2.5)
    for stripe in range(6):
        x=3+stripe*7; col="B" if stripe%2==0 else "G"
        p.rect("canvas-cream" if stripe%2==0 else "canvas-olive",col,x,11,7,13,4)
        p.rect("valance-cream" if stripe%2==0 else "valance-olive", "b" if stripe%2==0 else "g",x,24,7,4,4.3)
        p.rect("scallop-cream" if stripe%2==0 else "scallop-olive", "b" if stripe%2==0 else "g",x+1,28,5,1,4.3)
    p.rect("canvas-left","b",1,16,2,10,4).rect("canvas-right","g",45,16,2,10,4)
    p.rect("canvas-top","b",3,10,42,1,4.3)
    # Posts are visible above canvas and below the eaves. No occupied center below the roof.
    p.rect("left-cap","W",2,7,4,3,4.5).rect("right-cap","W",42,7,4,3,4.5)
    props.append(p)

    p = Prop("sorting-table", description="A worn two-leg seed sorting table with pale seed packets and a grain tray.")
    p.rect("legs","w",2,18,2,5,2).rect("legs","w",12,18,2,5,2)
    p.rect("table-top","h",1,15,14,3,3.5).rect("table-front","W",1,18,14,2,3.5)
    p.rect("left-packet","B",3,11,3,4,3).rect("left-tie","g",4,12,1,2,3.4)
    p.rect("right-packet","b",11,12,2,3,3).dot("right-tie","g",11,12,3.4)
    p.rect("grain-tray","w",7,13,3,2,3).dot("grain","f",8,13,3.4)
    props.append(p)

    p = Prop("drying-rack", description="Three tied hanging crop bundles inside a timber frame.")
    p.rect("posts","w",1,6,2,17,2).rect("posts","w",13,6,2,17,2)
    p.rect("crossbar","W",1,7,14,2,3).rect("post-light","h",1,6,1,17,3).rect("post-light","h",13,6,1,17,3)
    for x,col,shade in [(4,"f","h"),(8,"f","h"),(11,"G","g")]:
        p.rect("cords","B",x,8,1,3,3.4)
        p.rect("bundle-"+col,col,x-1,12,2,6,4).rect("bundle-neck-"+col,col,x,11,1,8,4)
        p.rect("bundle-shade-"+shade,shade,x-1,14,1,3,4.1)
    props.append(p)

    p = Prop("woven-basket", description="Open handled basket with three warm seeds.")
    p.rect("basket-base","w",3,16,10,5,2).rect("basket-base","w",4,21,8,2,2)
    p.rect("weave","W",4,17,8,5,2.2).rect("rim","h",3,16,10,2,3)
    p.rect("handle","W",3,12,2,4,2).rect("handle","W",11,12,2,4,2).rect("handle","h",5,11,6,2,2)
    p.rect("opening","n",5,14,6,2,2.7)
    for x,y in [(5,15),(7,14),(9,15)]:p.dot("seeds","f",x,y,3)
    p.rect("weave-light","h",5,19,2,1,3).rect("weave-light","h",9,20,2,1,3)
    props.append(p)

    p = Prop("seed-sack", description="Tied pale linen sack with a dark green sprout mark.")
    p.rect("sack-shadow","b",3,15,10,7,2).rect("sack-shadow","b",4,13,8,10,2)
    p.rect("sack-front","B",4,15,7,6,2.4).rect("neck","b",6,10,4,5,2)
    p.rect("top-linen","B",5,9,2,3,2.1).rect("top-linen","B",9,9,2,3,2.1)
    p.rect("tie","g",6,12,4,1,2.5).rect("sprout","g",7,17,1,4,2.5).rect("sprout","g",8,16,2,1,2.5).rect("sprout","g",5,18,2,1,2.5)
    p.rect("base-shadow","w",4,22,8,1,2.5)
    props.append(p)

    p = Prop("broken-wall", description="Broken grey masonry with moss in its missing upper edge.")
    p.rect("wall-shadow","s",1,9,14,14,3).rect("wall-shadow","s",2,7,6,2,3)
    p.rect("upper-block","S",2,8,5,7,3.2).rect("bottom-block","S",2,17,5,5,3.2)
    p.rect("right-block","S",9,14,5,8,3.2).rect("middle-block","l",3,15,9,2,3.4)
    p.erase(8,9,7,3).erase(12,12,3,2)
    p.rect("moss","g",8,11,3,2,3.4).rect("moss","G",9,12,4,2,3.4)
    p.dot("stone-pock","s",4,11,3.5).rect("stone-pock","s",11,18,2,1,3.5)
    props.append(p)

    p = Prop("fence-horizontal", description="Two rough wooden posts with paired horizontal rails; passability belongs to gameplay.")
    p.rect("posts","w",1,11,2,12,2).rect("posts","w",13,11,2,12,2)
    p.rect("rails","W",0,14,16,2,3).rect("rails","W",0,19,16,2,3)
    p.rect("rail-light","h",0,14,16,1,3.2).rect("post-light","h",1,11,1,12,3.3)
    p.dot("peg","b",13,14,3.4).dot("peg","b",1,19,3.4)
    props.append(p)

    p = Prop("fence-vertical", description="Receding fence segment in the same flat oblique object language as the horizontal fence.")
    p.rect("rails","W",7,4,2,18,2).rect("rails","W",10,4,1,18,2)
    p.rect("back-post","w",6,3,3,7,3).rect("front-post","w",6,16,3,7,4)
    p.rect("post-light","h",6,3,1,7,3.2).rect("post-light","h",6,16,1,7,4.2)
    p.rect("rail-light","h",8,10,1,6,3).dot("peg","b",7,19,4.4)
    props.append(p)

    p = Prop("tilled-bed", description="Low muted earth with broken edges and three shallow interrupted furrows; no border or plant baked into the tile.")
    # Irregular ground-level clods, rather than the former rectangular wood-like frame.
    for y,x,width in [(12,4,6),(13,2,11),(14,2,12),(15,1,14),(16,2,12),
                      (17,1,13),(18,2,13),(19,2,12),(20,1,12),(21,3,10),(22,4,6)]:
        p.rect("loose-earth","U",x,y,width,1,.15)
    for y,x,width in [(15,3,8),(18,4,9),(21,4,7)]:
        p.rect("shallow-furrow","u",x,y,width,1,.2)
    for x,y,width in [(3,14,4),(9,14,3),(2,17,5),(9,17,4),(3,20,5),(10,20,2)]:
        p.rect("crumbled-ridge","q",x,y,width,1,.3)
    for x,y in [(4,13),(10,16),(4,19),(8,22)]:p.dot("dry-clod","Q",x,y,.35)
    props.append(p)

    p = Prop("emberwheat", description="Three warm grain heads on spare stalks, clear enough to recognize at one cell.")
    p.rect("left-stem","g",4,12,1,11,1).rect("middle-stem","g",8,7,1,16,1).rect("right-stem","g",12,11,1,12,1)
    for x,y in [(4,9),(8,4),(12,8)]:
        p.rect("head","h",x-1,y,3,6,2).rect("grain","f",x,y-1,1,7,2.2)
        p.dot("grain-tip","B",x,y-1,2.4).dot("side-grain","f",x-1,y+2,2.3)
    p.rect("leaf","G",5,17,3,1,2).rect("leaf","G",9,19,3,1,2).dot("leaf","G",3,18,2)
    props.append(p)

    p = Prop("marlroot", description="Broad olive leaves above a small clay-colored exposed root shoulder.")
    p.rect("root-shadow","r",6,18,5,4,2).rect("root","b",7,18,3,4,2.2).dot("root-tail","b",8,22,2.2)
    p.rect("stem","g",8,11,1,8,1.5)
    p.rect("left-leaf","g",3,12,5,3,2).rect("left-leaf","G",4,11,3,2,2.2)
    p.rect("right-leaf","g",9,10,4,4,2).rect("right-leaf","G",10,9,2,3,2.2)
    p.rect("lower-leaf","g",5,16,3,2,2).rect("lower-leaf","G",9,15,3,2,2.2)
    p.dot("leaf-light","e",5,11,2.4).dot("leaf-light","e",10,10,2.4)
    props.append(p)

    p = Prop("grass-tuft", description="Spare forked grass with generous empty frame space.")
    p.rect("base","t",5,21,6,2,1).rect("stem","g",7,16,2,7,1.5)
    p.rect("left-leaf","g",4,17,2,3,1.5).rect("left-leaf","G",3,16,2,2,2)
    p.rect("right-leaf","g",10,18,2,3,1.5).rect("right-leaf","G",11,16,2,3,2)
    p.rect("upright","G",7,14,1,5,2).dot("tip","e",7,14,2.2)
    props.append(p)

    p = Prop("stool", description="Low four-legged rough stool with a warm worn seat.")
    p.rect("back-legs","w",5,17,1,5,1).rect("back-legs","w",10,17,1,5,1)
    p.rect("front-legs","W",4,18,2,5,2).rect("front-legs","W",10,18,2,5,2)
    p.rect("seat-shadow","w",3,16,10,3,3).rect("seat","h",4,15,8,3,3.3)
    p.rect("seat-grain","W",6,16,4,1,3.4).dot("worn-wood","b",5,15,3.5)
    props.append(p)

    p = Prop("firewood", description="Short stacked split logs, with pale cut ends.")
    p.rect("bottom-logs","w",2,19,12,4,2).rect("middle-logs","W",3,17,10,3,3)
    p.rect("top-log","w",5,15,8,3,4).rect("top-log-light","h",6,15,6,1,4.2)
    p.rect("cut-end","b",2,20,3,2,2.4).rect("cut-end","b",3,17,2,2,3.4).rect("cut-end","b",5,15,2,2,4.4)
    p.dot("end-ring","W",3,21,2.6).dot("end-ring","W",4,18,3.6).dot("end-ring","W",6,16,4.6)
    p.rect("split","d",8,20,5,1,3.5)
    props.append(p)
    return props


def linear(component):
    component /= 255
    return component / 12.92 if component <= .04045 else ((component + .055) / 1.055) ** 2.4


def material(key):
    hex_color=PALETTE[key]
    m=bpy.data.materials.new("Palette_"+hex_color)
    rgba=tuple(linear(int(hex_color[i:i+2],16)) for i in (0,2,4))+(1,)
    m.diffuse_color=rgba; m.use_nodes=True
    nodes=m.node_tree.nodes;nodes.clear()
    emission=nodes.new("ShaderNodeEmission");emission.inputs["Color"].default_value=rgba
    output=nodes.new("ShaderNodeOutputMaterial")
    m.node_tree.links.new(emission.outputs[0],output.inputs["Surface"])
    return m


def add_box(vertices,faces,x0,y0,z0,x1,y1,z1):
    start=len(vertices)
    vertices.extend([(x0,y0,z0),(x1,y0,z0),(x1,y1,z0),(x0,y1,z0),
                     (x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)])
    faces.extend(tuple(start+i for i in f) for f in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])


def model(prop, materials, offset):
    coll=bpy.data.collections.new(prop.name);bpy.context.scene.collection.children.link(coll)
    grouped={}
    # Merge identical horizontal runs into extruded solids while retaining named parts.
    for row,cells in enumerate(prop.cells):
        x=0
        while x<prop.width:
            value=cells[x]
            if value is None:x+=1;continue
            end=x+1
            while end<prop.width and cells[end]==value:end+=1
            part,color,depth=value
            vertices,faces=grouped.setdefault((part,color),([],[]))
            add_box(vertices,faces,offset+x,prop.height-row-1,0,offset+end,prop.height-row,depth)
            x=end
    count=0
    for (part,color),(vertices,faces) in sorted(grouped.items()):
        mesh=bpy.data.meshes.new(prop.name+"_"+part)
        mesh.from_pydata(vertices,[],faces);mesh.materials.append(materials[color]);mesh.update()
        obj=bpy.data.objects.new(part,mesh);coll.objects.link(obj)
        obj["authoring"]="Hand-authored modular voxel geometry; generated concept is visual guidance only."
        count+=len(vertices)
    return coll,count


def main():
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action="SELECT");bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name!="Collection":bpy.data.collections.remove(collection)
    props=make_props();materials={key:material(key) for key in PALETTE}
    built=[]
    for i,prop in enumerate(props):
        collection,vertices=model(prop,materials,i*64)
        built.append((prop,collection,vertices,i*64))
    camera_data=bpy.data.cameras.new("Native pixel camera")
    camera=bpy.data.objects.new("Native pixel camera",camera_data);bpy.context.scene.collection.objects.link(camera)
    camera_data.type="ORTHO"
    scene=bpy.context.scene;scene.camera=camera;scene.render.engine="CYCLES";scene.cycles.samples=1
    scene.cycles.use_denoising=False;scene.render.film_transparent=True
    scene.render.image_settings.file_format="PNG";scene.render.image_settings.color_mode="RGBA"
    scene.render.image_settings.color_depth="8";scene.render.resolution_percentage=100;scene.render.filter_size=.01
    # Blender writes stamp metadata even with the visible stamp disabled. Exclude
    # wall-clock/date values so an unchanged native sprite retains its PNG bytes.
    scene.render.use_stamp_date=False;scene.render.use_stamp_time=False
    scene.render.use_stamp_render_time=False
    scene.view_settings.view_transform="Standard";scene.view_settings.look="None"
    scene.view_settings.exposure=0;scene.view_settings.gamma=1;scene.render.dither_intensity=0
    manifest=[]
    for prop,collection,vertices,offset in built:
        for _,other,_,_ in built:other.hide_render=other!=collection
        camera.location=(offset+prop.width/2,prop.height/2,100)
        camera_data.ortho_scale=max(prop.width,prop.height)
        scene.render.resolution_x=prop.width;scene.render.resolution_y=prop.height
        path=OUT/(prop.name+".png");scene.render.filepath=str(path)
        bpy.ops.render.render(write_still=True)
        occupied=sum(cell is not None for row in prop.cells for cell in row)
        palette=sorted({PALETTE[cell[1]] for row in prop.cells for cell in row if cell})
        manifest.append({"id":prop.name,"file":path.name,"width":prop.width,"height":prop.height,
                         "pixelsPerUnit":16,"pivot":[.5,0],"occupiedAuthoredCells":occupied,
                         "palette":palette,"partCount":len(collection.objects),"vertices":vertices,
                         "gridFootprint":[prop.width/16,prop.height/24],"description":prop.description,
                         "sha256":hashlib.sha256(path.read_bytes()).hexdigest()})
    # A real Blender overview render, not a cropped/resampled asset contact sheet.
    # Rearrange editable source objects into a 4×4 shelf after their native renders.
    for i,(prop,collection,_,offset) in enumerate(built):
        collection.hide_render=False
        column,row=i%4,i//4
        for obj in collection.objects:
            obj.location.x=column*48+(48-prop.width)/2-offset
            obj.location.y=(3-row)*56+5
        text_curve=bpy.data.curves.new(prop.name+" label",type="FONT")
        text_curve.body=prop.name;text_curve.align_x="CENTER";text_curve.size=2.8
        text_curve.materials.append(materials["B"])
        label=bpy.data.objects.new(prop.name+" label",text_curve)
        bpy.context.scene.collection.objects.link(label)
        label.location=(column*48+24,(3-row)*56+1,6)
    background=bpy.data.materials.new("Overview dark green")
    background.use_nodes=True
    nodes=background.node_tree.nodes;nodes.clear()
    emission=nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value=tuple(linear(c) for c in (3,47,35))+(1,)
    output=nodes.new("ShaderNodeOutputMaterial");background.node_tree.links.new(emission.outputs[0],output.inputs["Surface"])
    vertices=[];faces=[];add_box(vertices,faces,0,0,-2,192,224,-1)
    mesh=bpy.data.meshes.new("Overview ground");mesh.from_pydata(vertices,[],faces);mesh.materials.append(background)
    ground=bpy.data.objects.new("Overview ground",mesh);bpy.context.scene.collection.objects.link(ground)
    camera.location=(96,112,100);camera_data.ortho_scale=224
    scene.render.resolution_x=1152;scene.render.resolution_y=1344
    scene.render.filepath=str(OUT/"kit-overview.png")
    bpy.ops.render.render(write_still=True)
    scene["source_workflow"]="Concept-guided manual voxel modeling → native Blender RGBA rendering."
    scene["reference"]="ArtSource/Clearing16x24/concepts/modular-props-v2.png"
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"clearing-modular-kit.blend"))
    document={"version":1,"source":"Tools/Clearing16x24/build_kit.py","renderer":bpy.app.version_string,
              "reference":"../concepts/modular-props-v2.png","authorship":"Concept-guided original voxel geometry, not automatic image-to-3D extraction.",
              "props":manifest}
    (OUT/"manifest.json").write_text(json.dumps(document,indent=2)+"\n")
    print("CLEARING_KIT_COMPLETE",len(props),"props",sum(p["vertices"] for p in manifest),"vertices")


if __name__=="__main__":main()
