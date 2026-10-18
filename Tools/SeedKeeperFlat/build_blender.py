"""Create the requested flat Seed Keeper IN Blender, with editable colored faces.
Run: /Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/SeedKeeperFlat/build_blender.py
Coordinates below are hand-authored pixel contours, not a cropped reference image.
"""
import bpy, json, math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/SeedKeeperFlat'
OUT.mkdir(parents=True, exist_ok=True)
W, H = 64, 80
palette = [
    '#201d16', '#30281d', '#453522', '#5b422b', '#795333', '#986b42',
    '#b88750', '#d2a36a', '#e8bc80', '#f5d398', '#705638', '#906b42',
    '#aa7c49', '#bf925a', '#d6aa70', '#514a2b', '#635b34', '#776d3e',
    '#8a7e49', '#9b8d55', '#3d3924', '#2d2d20', '#ac8656', '#cfaa73',
    '#e9c990', '#695536', '#86633f', '#a17b4b', '#454632', '#646448',
    '#7e7b51', '#55563b', '#5a3629', '#754c35', '#946344', '#b17b51',
]
P={}
def poly(c, pts):
    for y in range(max(0,min(p[1] for p in pts)),min(H,max(p[1] for p in pts)+1)):
        for x in range(max(0,min(p[0] for p in pts)),min(W,max(p[0] for p in pts)+1)):
            px,py=x+.5,y+.5; inside=False
            for i,(ax,ay) in enumerate(pts):
                bx,by=pts[(i+1)%len(pts)]
                if (ay>py)!=(by>py) and px<(bx-ax)*(py-ay)/(by-ay)+ax:inside=not inside
            if inside:P[x,y]=c
def rect(c,x,y,w,h): poly(c,[(x,y),(x+w,y),(x+w,y+h),(x,y+h)])

# Boots and separated legs: dark contour, warm planes, distinct cuffs.
poly(0,[(22,62),(28,62),(28,69),(27,70),(27,75),(16,75),(15,74),(15,71),(18,70),(19,68),(22,68)])
poly(3,[(23,63),(27,63),(27,69),(26,71),(26,74),(17,74),(16,73),(17,71),(21,70),(22,68)])
rect(4,23,64,3,5);rect(2,24,67,3,2)
poly(4,[(22,69),(25,69),(25,72),(23,73),(17,73),(17,72),(20,71)])
rect(5,18,71,3,1);rect(2,16,74,10,1)
rect(1,21,67,7,3);rect(6,21,67,6,1);rect(4,22,68,5,1)
poly(0,[(35,62),(42,62),(42,68),(41,69),(41,75),(30,75),(30,72),(32,71),(33,69),(35,69)])
poly(3,[(36,63),(41,63),(40,69),(40,74),(31,74),(31,73),(35,71),(35,69)])
rect(4,36,65,4,4);rect(2,38,66,3,2)
poly(4,[(36,69),(39,69),(39,73),(36,74),(31,74),(32,73),(35,72)])
rect(5,34,72,3,1);rect(2,31,74,10,1)
rect(1,34,67,8,3);rect(6,34,67,7,1);rect(4,35,68,6,1)

# Continuous long-coat silhouette, with a hanging left edge and darker right fold.
poly(0,[(25,27),(34,25),(37,28),(38,32),(39,36),(41,40),(41,44),(43,48),(44,51),(45,54),(46,57),(47,59),(47,62),(43,63),(42,65),(37,66),(35,65),(28,66),(21,65),(20,63),(17,62),(16,60),(17,54),(18,49),(19,45),(20,39),(22,35),(23,30)])
poly(15,[(26,28),(34,27),(36,30),(37,35),(39,40),(40,45),(42,50),(43,54),(45,59),(45,61),(41,62),(40,64),(29,65),(22,64),(22,59),(22,51),(21,46),(22,39),(24,34)])
poly(16,[(26,31),(33,30),(34,35),(35,40),(35,46),(34,49),(35,55),(34,62),(38,64),(29,64),(23,63),(23,59),(23,48),(24,43)])
poly(17,[(26,32),(28,32),(28,40),(26,48),(25,57),(25,62),(23,61),(23,51),(24,45)])
poly(20,[(35,29),(37,32),(38,38),(40,43),(40,47),(43,55),(45,60),(41,61),(40,63),(38,64),(37,61),(39,57),(37,50),(35,45),(36,39)])
poly(17,[(19,47),(21,46),(21,52),(20,59),(19,62),(17,61),(18,55)])
poly(20,[(22,48),(23,47),(23,58),(22,63),(21,63),(21,55)])
rect(21,23,46,16,2);rect(3,23,45,17,1);rect(10,24,45,12,1)

# Distant shoulder/arm and a sleeve with an angular highlighted ridge.
poly(20,[(34,28),(36,29),(38,33),(39,37),(40,40),(40,43),(36,44),(33,40),(33,34)])
poly(16,[(35,30),(36,32),(37,35),(38,39),(38,41),(35,41),(34,37)])
poly(0,[(27,28),(32,28),(34,32),(33,37),(31,42),(28,46),(24,47),(21,45),(21,42),(23,38),(23,34),(25,30)])
poly(17,[(27,29),(31,29),(32,32),(31,37),(29,41),(27,44),(24,45),(22,44),(22,42),(24,38),(24,34)])
poly(18,[(27,29),(30,29),(29,31),(27,32),(26,35),(25,38),(24,40),(23,40),(24,35),(25,32)])
poly(16,[(31,31),(32,32),(31,37),(29,42),(26,45),(23,45),(25,43),(28,40),(30,36)])
rect(19,27,30,2,1);rect(15,25,43,2,1)

# Gloved near hand, strap and four asymmetrical seed pouches.
poly(0,[(21,41),(23,42),(23,45),(24,46),(22,48),(19,48),(18,47),(18,43),(19,42)])
poly(3,[(20,42),(22,43),(22,46),(21,47),(19,47),(19,44)])
rect(4,20,43,1,3);rect(2,21,45,1,2)
rect(2,29,43,1,4);rect(10,29,43,1,2)
def pouch(x,y,w,h,body,shade,light):
    poly(0,[(x+2,y),(x+w-2,y),(x+w-2,y+2),(x+w,y+4),(x+w,y+h-1),(x+w-1,y+h),(x+1,y+h),(x,y+h-1),(x,y+4),(x+2,y+2)])
    poly(body,[(x+2,y+1),(x+w-2,y+1),(x+w-2,y+3),(x+w-1,y+4),(x+w-1,y+h-1),(x+1,y+h-1),(x+1,y+4),(x+2,y+3)])
    rect(light,x+1,y+4,1,h-6);rect(light,x+2,y+3,1,2)
    rect(shade,x+w-2,y+4,1,h-5);rect(shade,x+2,y+h-2,w-3,1)
    rect(0,x+w//2,y+2,1,2);rect(0,x+w//2-1,y+3,1,1);rect(body,x+w//2,y+3,1,1)
    rect(0,x+w//2+1,y+3,1,1)
pouch(29,42,7,10,23,22,24)
pouch(36,40,6,9,26,3,27)
rect(2,32,52,1,2);rect(2,40,51,1,2)
pouch(29,53,7,9,29,28,30)
pouch(38,51,7,10,33,32,34)
rect(31,31,59,3,1);rect(35,39,55,1,2)

# Black profile under the brim; single warm eye and two scarf planes.
poly(0,[(20,19),(34,20),(35,24),(30,26),(25,26),(23,29),(21,26),(21,24),(19,22)])
rect(2,21,21,2,1);rect(24,22,21,1,2)
poly(0,[(31,22),(36,22),(37,24),(34,26),(30,28),(26,31),(22,31),(21,29),(22,27),(26,25)])
poly(17,[(31,23),(35,23),(35,25),(31,27),(28,28),(25,30),(22,30),(22,28),(26,26)])
poly(18,[(31,23),(34,23),(33,24),(29,26),(26,27),(24,29),(22,29),(24,27),(28,25)])
poly(15,[(35,24),(36,24),(34,26),(31,27),(27,30),(24,31),(23,30),(28,27),(31,26)])

# Broad, slightly asymmetric hat: dark underside, stepped straw crown, woven accents.
poly(0,[(32,4),(35,4),(35,6),(34,7),(36,8),(36,9),(39,10),(40,11),(43,12),(45,13),(47,15),(49,16),(51,18),(53,20),(55,22),(57,24),(58,25),(57,27),(54,28),(48,27),(43,26),(37,25),(34,23),(29,22),(26,21),(21,21),(17,22),(12,21),(10,20),(7,20),(6,19),(6,17),(9,15),(12,14),(15,12),(19,11),(22,10),(25,9),(28,8),(30,7),(30,5),(32,5)])
poly(3,[(7,18),(13,18),(20,20),(23,23),(19,23),(16,22),(12,22),(10,21),(7,20)])
poly(2,[(8,19),(14,20),(18,21),(20,23),(16,22),(11,21)])
poly(3,[(34,22),(40,23),(46,24),(52,25),(57,25),(56,27),(51,27),(45,26),(38,25)])
poly(4,[(40,23),(45,23),(50,25),(55,25),(54,26),(47,25)])
poly(7,[(32,5),(34,5),(34,7),(33,8),(36,9),(38,11),(41,12),(44,14),(47,16),(50,19),(53,21),(56,24),(57,25),(52,24),(48,23),(43,22),(40,21),(35,21),(31,20),(27,19),(23,18),(18,18),(14,19),(10,18),(7,18),(10,16),(13,15),(16,13),(20,12),(23,11),(26,10),(29,9),(31,8),(31,6)])
poly(6,[(33,6),(34,6),(33,8),(36,9),(38,11),(41,13),(43,14),(47,17),(48,19),(52,21),(54,23),(56,25),(51,24),(48,23),(46,21),(43,19),(41,18),(39,15),(36,14),(34,11),(31,10),(31,9)])
poly(8,[(10,16),(14,14),(18,13),(23,11),(28,10),(30,9),(32,9),(31,10),(28,11),(25,12),(21,13),(19,14),(16,14),(14,16),(11,17),(7,18),(7,17)])
poly(8,[(25,12),(30,10),(34,11),(36,12),(38,13),(34,13),(31,12),(28,13)])
poly(13,[(18,15),(24,13),(28,14),(32,14),(35,15),(40,17),(42,19),(38,18),(34,17),(29,16),(25,16),(22,17),(17,17)])
poly(14,[(28,17),(33,17),(36,18),(40,19),(43,20),(43,21),(39,20),(35,20),(31,19),(28,19)])
poly(12,[(13,17),(16,16),(20,16),(21,17),(18,18),(14,18)])
rect(11,23,12,2,1);rect(12,27,11,2,1);rect(9,17,14,2,1)
rect(11,30,14,3,1);rect(12,32,15,4,1);rect(11,37,16,2,1)
rect(13,30,8,4,1);rect(5,32,5,2,1);rect(8,31,6,1,2)
rect(6,45,19,2,1);rect(12,47,21,3,1);rect(8,40,17,2,1)
rect(9,12,16,2,1);rect(14,21,14,2,1);rect(11,25,15,2,1)

data={'schemaVersion':1,'width':W,'height':H,'palette':palette,
      'pixels':[{'x':x,'y':y,'color':c} for (x,y),c in sorted(P.items(),key=lambda p:(p[0][1],p[0][0]))],
      'authoring':'Hand-authored planar pixel geometry in Blender; no extrusion, image texture or background crop',
      'pivotPixels':[32,75]}
(ROOT/'Tools/SeedKeeperFlat/seed-keeper.json').write_text(json.dumps(data,separators=(',',':'))+'\n')

bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def linear(s): return s/12.92 if s<=0.04045 else ((s+0.055)/1.055)**2.4
mats=[]
for i,h in enumerate(palette):
    rgb=tuple(linear(int(h[j:j+2],16)/255) for j in (1,3,5))
    m=bpy.data.materials.new(f'{i:02d} {h}');m.diffuse_color=(*rgb,1);m.use_nodes=True
    m.node_tree.nodes.clear();em=m.node_tree.nodes.new('ShaderNodeEmission');em.inputs['Color'].default_value=(*rgb,1)
    out=m.node_tree.nodes.new('ShaderNodeOutputMaterial');m.node_tree.links.new(em.outputs[0],out.inputs['Surface']);mats.append(m)
verts=[];faces=[]
for (x,y),c in sorted(P.items(),key=lambda p:(p[0][1],p[0][0])):
    k=len(verts);verts += [(x,H-y-1,0),(x+1,H-y-1,0),(x+1,H-y,0),(x,H-y,0)];faces.append((k,k+1,k+2,k+3))
mesh=bpy.data.meshes.new('Seed Keeper — planar pixel faces');mesh.from_pydata(verts,[],faces);mesh.update()
obj=bpy.data.objects.new('Seed Keeper — 2D shadow and emphasis',mesh);bpy.context.collection.objects.link(obj)
for m in mats:mesh.materials.append(m)
for f,((x,y),c) in zip(mesh.polygons,sorted(P.items(),key=lambda p:(p[0][1],p[0][0]))):f.material_index=c
obj['dimensionality']='2D: every vertex lies at Z=0';obj['source']='Right-hand Seed Keeper reference; hand-authored contours and shade planes'
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
camera_data=bpy.data.cameras.new('Orthographic pixel camera');camera=bpy.data.objects.new('Orthographic pixel camera',camera_data);bpy.context.collection.objects.link(camera)
camera.location=(W/2,H/2,100);camera.rotation_euler=(0,0,0);camera_data.type='ORTHO';camera_data.ortho_scale=H
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=1
scene.render.resolution_x=W*8;scene.render.resolution_y=H*8;scene.render.resolution_percentage=100
scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.render.filter_size=.01;scene.render.filepath=str(OUT/'seed-keeper-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'seed-keeper.blend'))
bpy.ops.render.render(write_still=True)
print('SEED_KEEPER_EXPORTED',len(P),'pixels',len(verts),'coplanar vertices')
