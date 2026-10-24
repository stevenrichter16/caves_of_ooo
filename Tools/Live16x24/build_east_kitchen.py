"""Original native east/kitchen cell geometry. Blender --background --python this-file.
No concept raster is opened, sampled, cropped or resized. All new pixels are
named editable integer-cell mesh quads rendered directly at native resolution.
Writes only the east-kitchen source folder and two owned resource PNG/meta pairs.
Approved Cook is copied as bytes, never transformed. Existing helpers read-only.
"""
import hashlib,json,shutil,sys,uuid
from pathlib import Path
import bpy
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_opening_base import add_quad,add_label,camera_for,emission_material
from build_north_plot import instance,plane
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Live16x24/east-kitchen'
RESOURCE=ROOT/'Assets/Resources/Sprites/Live16x24'
P={'ink':'342E25','wood_dark':'5F4830','wood':'94713D','cut':'BCA576','cream':'E4D0A0','linen':'B5A47E','stone_dark':'3C5148','stone':'526657','stone_light':'7D8E71','leaf_dark':'435A35','leaf':'75834B','leaf_light':'96A462','clay_dark':'765337','clay':'A97542','clay_light':'C39565','copper_dark':'8E5B36','copper':'B67A49','copper_edge':'C99565','patina':'758972','petal':'AC6C88','petal_light':'CA91A3'}

class Frame:
 def __init__(self,name,blueprint,description,state='identity',limits='No new behavior or decoration outside the real owner.'):
  self.name,self.blueprint,self.description,self.state,self.limits=name,blueprint,description,state,limits
  self.cells=[[None]*16 for _ in range(24)]
 def rect(self,part,color,x,y,w,h):
  assert color in P and 0<=x<x+w<=16 and 0<=y<y+h<=24,(part,color,x,y,w,h)
  for yy in range(y,y+h):
   for xx in range(x,x+w):self.cells[yy][xx]=(part,color)
  return self
 def dot(self,part,color,x,y):return self.rect(part,color,x,y,1,1)
 def clear(self,x,y,w=1,h=1):
  for yy in range(y,y+h):
   for xx in range(x,x+w):self.cells[yy][xx]=None
  return self

def frames():
 fs=[]
 p=Frame('forge','TinkersForge','Tall squared masonry hearth with an unlit recessed opening and broad work lip.',limits='No flame, coal glow, tool table, stock, heat, craft-ready or active-work cue.')
 p.rect('masonry-body','stone_dark',2,7,12,16).rect('upper-cap','stone',4,5,8,3)
 p.rect('upper-cap-edge','stone_light',4,5,7,1).rect('top-course','stone',3,8,10,3)
 p.rect('left-upright','stone',2,11,3,9).rect('right-upright','stone',11,11,3,9)
 p.rect('recess','ink',5,11,6,8).rect('recess-neck','ink',6,10,4,2)
 p.rect('left-course','stone_light',2,11,2,1).rect('right-course','stone_light',11,14,2,1)
 p.rect('left-joint','stone_dark',2,16,3,1).rect('right-joint','stone_dark',11,19,3,1)
 p.rect('work-lip','stone_light',3,19,10,1).rect('base-face','stone',3,20,10,3)
 p.rect('base-joint','stone_dark',7,20,1,3).dot('top-joint','stone_dark',8,7);fs.append(p)
 p=Frame('oven','Oven','Squat rounded pale masonry oven with a clay arch and short capped neck.',limits='Neutral station: no invented flame, smoke, finite fuel, food or readiness state.')
 p.rect('oven-base','stone',3,19,10,4).rect('oven-body','stone_light',2,15,12,5)
 p.rect('oven-shoulder','stone_light',3,12,10,4).rect('oven-crown','stone_light',5,11,6,2)
 p.rect('neck','stone',6,9,4,2).rect('neck-cap','linen',5,8,6,1)
 p.rect('arch-upper','clay',5,14,6,2).rect('arch-left','clay',4,16,2,3).rect('arch-right','clay',10,16,2,3)
 p.rect('mouth','ink',6,16,4,4).rect('mouth-wide','ink',5,18,6,2)
 p.rect('left-stone-toe','stone_light',3,20,3,3).rect('right-stone-toe','stone_light',10,20,3,3)
 p.rect('worn-crown','linen',4,12,4,1).dot('right-wear','linen',12,16);fs.append(p)
 p=Frame('vertical-cot','Bed','Narrow north-south wooden cot with a flat woven linen bed and two end rails.',limits='One real bed cell; no sleeping actor, bedding occupancy or claimed-use cue.')
 p.rect('left-rail','wood_dark',3,4,2,19).rect('right-rail','wood_dark',11,4,2,19)
 p.rect('woven-bed','linen',5,6,6,14).rect('head-rail','wood',3,4,10,2).rect('foot-rail','wood',3,20,10,2)
 p.rect('left-rail-face','wood',3,6,1,14).rect('right-rail-face','wood',12,6,1,14)
 p.rect('weave-length','cream',6,7,1,12).rect('weave-length','cream',9,7,1,12)
 for y in (9,13,17):p.rect('weave-cross','wood',5,y,6,1)
 p.dot('rail-end','linen',3,4).dot('rail-end','linen',12,20);fs.append(p)
 p=Frame('chair','Chair','Upright spare wooden chair with a broad back, open back gap and separated legs.')
 p.rect('left-back-post','wood_dark',4,5,2,12).rect('right-back-post','wood_dark',10,5,2,12)
 p.rect('back-rest','wood',4,5,8,4).rect('back-top','cut',4,5,8,1)
 p.dot('back-peg','wood_dark',5,7).dot('back-peg','wood_dark',10,7)
 p.rect('seat','wood',3,15,9,4).rect('seat-front','cut',3,15,9,1)
 p.rect('left-leg','wood_dark',3,18,2,5).rect('right-leg','wood_dark',10,18,2,5)
 p.rect('lower-brace','wood',5,20,5,1);fs.append(p)
 def pan(sealed):
  q=Frame('sealed-clay-pan' if sealed else 'cracked-clay-pan','ConnectedBatchPan','Low broad earthenware batch pan with '+('a quiet pale clay repair seam.' if sealed else 'a split in its front clay wall.'),'repaired' if sealed else 'damaged','Clay/masonry only; no iron, fire, food, ingredients, active job or ready cue.')
  q.rect('clay-upper-rim','clay_light',4,14,8,1).rect('clay-shoulder','clay',2,15,12,5)
  q.rect('clay-left-handle','clay_light',1,16,2,2).rect('clay-right-handle','clay_light',13,16,2,2)
  q.rect('clay-body','clay',3,20,10,2).rect('clay-base','clay_dark',5,22,6,1)
  q.rect('pan-inner','clay_dark',3,16,10,3).rect('inner-recess','ink',4,16,8,2)
  q.rect('front-rim','clay_light',3,19,10,1)
  col='linen' if sealed else 'ink'
  q.dot('repair' if sealed else 'crack',col,8,19).rect('repair' if sealed else 'crack',col,7,20,1,2).dot('repair' if sealed else 'crack',col,8,22)
  return q
 fs.extend((pan(False),pan(True)))
 p=Frame('neutral-input-tray','ConnectedKitchenEscrow','Shallow tied woven tray with an empty dark inset, quiet crossing fibers and no depicted contents.',limits='Neutral container at every unproved service state; no paid ingredients or working cue.')
 p.rect('basket-top','cut',4,15,8,1).rect('basket-rim','wood',2,16,12,4)
 p.rect('basket-inset','wood_dark',3,17,10,2).rect('basket-wall','wood',3,20,10,3)
 p.rect('basket-front-edge','cut',3,20,10,1)
 for x in (4,7,10):p.rect('basket-weave','wood_dark',x,21,1,2)
 p.rect('binding','cut',7,16,1,5);fs.append(p)
 p=Frame('neutral-pickup-board','ConnectedKitchenPickup','Flat rectangular serving board with two short handles and a plain empty face.',limits='No meal parcel or readiness cue painted onto the container.')
 p.rect('board-edge','wood_dark',2,15,12,7).rect('board-face','wood',3,16,10,5)
 p.rect('board-lip','cut',3,15,10,1).rect('board-base','wood_dark',3,22,10,1)
 p.rect('left-handle','cut',1,17,2,3).rect('right-handle','cut',13,17,2,3)
 p.rect('face-grain','cut',4,19,6,1);fs.append(p)
 p=Frame('claspbean-planted','ClaspbeanCrop','Low newly planted crown: two tiny cotyledons and a short ground line.',state='stage0',limits='No ripe pod, visible yield count or damp-soil promise.')
 p.rect('soil','wood_dark',5,22,7,1).rect('soil-break','wood',6,21,4,1)
 p.rect('crown','leaf_dark',8,19,1,3).rect('left-cotyledon','leaf',6,18,2,2).rect('right-cotyledon','leaf',9,18,2,1)
 p.dot('leaf-join','leaf_dark',8,19).dot('leaf-join','leaf',9,19);fs.append(p)
 p=Frame('claspbean-growing','ClaspbeanCrop','Two broad climbing leaves on a young forked stem above a quiet crown.',state='stage1',limits='No pods, harvest-ready mark or moisture indicator.')
 p.rect('soil','wood_dark',4,22,8,1).rect('soil-break','wood',6,21,4,1).rect('stem','leaf_dark',7,15,2,7)
 p.rect('left-leaf','leaf',3,13,4,3).rect('left-leaf-join','leaf',5,16,3,1).dot('left-leaf-tip','leaf',3,12)
 p.rect('right-leaf','leaf',9,12,4,3).rect('right-leaf-join','leaf',8,15,3,1).dot('right-leaf-tip','leaf',12,11)
 p.rect('left-vein','leaf_light',3,13,2,1).rect('right-vein','leaf_light',11,12,2,1);fs.append(p)
 p=Frame('claspbean-ripe','ClaspbeanCrop','Upright claspbean with two pale hanging pods, small upper leaves and rooted crown.',state='stage2',limits='Only actual mature crop selects this; drawn pods do not specify yielded stack quantity or moisture.')
 p.rect('soil','wood_dark',4,22,9,1).rect('stem','leaf_dark',7,8,2,14)
 p.rect('left-upper-leaf','leaf',4,5,3,3).dot('left-tip','leaf_light',3,5).rect('left-join','leaf',6,8,2,1)
 p.rect('right-upper-leaf','leaf',9,5,3,3).dot('right-tip','leaf_light',11,4).rect('right-join','leaf',8,8,2,1)
 p.rect('left-side-leaf','leaf',3,11,4,2).rect('right-side-leaf','leaf',9,10,4,2)
 p.rect('left-pod-neck','leaf_dark',4,13,1,2).rect('left-pod','linen',3,15,3,5).rect('left-pod-face','cream',3,15,1,3).dot('left-pod-end','linen',4,20)
 p.rect('right-pod-neck','leaf_dark',11,12,1,3).rect('right-pod','linen',10,15,3,4).rect('right-pod-face','cream',10,15,1,2).dot('right-pod-end','linen',11,19);fs.append(p)
 p=Frame('claspbean-pulp','ClaspbeanPulp','Small loose pale pulp with two soft lobes and a quiet olive underside.',limits='Raw portable pulp; no bowl, blood, bandage or prepared meal.')
 p.rect('pulp-upper','cream',7,17,3,2).rect('pulp-left','cream',4,19,5,3).rect('pulp-right','linen',9,19,3,3)
 p.rect('pulp-low','linen',5,22,6,1).rect('pulp-fold','linen',6,20,2,1).dot('pulp-underside','leaf',8,22).dot('pulp-right-face','cream',10,20);fs.append(p)
 p=Frame('claspbean-seed','ClaspbeanSeed','One small kidney-shaped pale seed with a recessed quiet seam.',limits='Illustrates one identity, not a stack count or planted state.')
 p.rect('seed-top','linen',7,16,3,1).rect('seed-body','cream',6,17,5,4).rect('seed-base','linen',7,21,3,2)
 p.dot('seed-left-turn','linen',5,19).rect('seed-notch','wood_dark',9,18,2,1).dot('seed-seam','linen',8,19);fs.append(p)
 p=Frame('wrapped-field-meal','FieldMeal','Compact folded linen meal parcel tied with a single dark cord.',limits='Portable completed FieldMeal only; no extra food, serving tray, effect icon or invented quantity.')
 p.rect('parcel-top','linen',5,16,6,1).rect('parcel-body','cream',4,17,8,5).rect('parcel-base','linen',5,22,6,1)
 p.rect('fold-left','linen',4,19,2,2).rect('fold-right','linen',10,17,2,2)
 p.rect('cord','wood_dark',7,16,1,7).rect('cord-cross','wood_dark',4,19,8,1).dot('cord-knot','wood_dark',8,20);fs.append(p)
 p=Frame('charm-flowers','CharmFlowers','Sparse pair of small muted pink flowers on uneven stems, with open ground between.',limits='No glow, bloom, dense carpet or implied pickable output.')
 p.rect('left-stem','leaf_dark',5,15,1,8).rect('left-leaf','leaf',3,19,2,1)
 p.rect('left-petal','petal',4,13,3,3).dot('left-petal-tip','petal',5,12).dot('left-petal-light','petal_light',4,13).dot('left-center','linen',5,14)
 p.rect('right-stem','leaf_dark',11,19,1,4).rect('right-leaf','leaf',12,21,2,1)
 p.rect('right-petal','petal',10,17,3,3).dot('right-petal-tip','petal',11,16).dot('right-petal-light','petal_light',12,18).dot('right-center','linen',11,18);fs.append(p)
 p=Frame('copper-pipe','CopperPipe','Small elbow of worn copper with a neutral greenish union collar.',limits='No flow, heat, leak, connection or active machinery cue.')
 p.rect('vertical-pipe','copper',4,13,4,10).rect('bend','copper',5,10,5,5).rect('horizontal-pipe','copper',9,10,4,4)
 p.rect('vertical-edge','copper_edge',4,15,1,7).rect('horizontal-edge','copper_edge',9,10,4,1)
 p.rect('pipe-end','copper_dark',12,11,1,3).rect('base-end','copper_dark',5,22,3,1)
 p.rect('union','patina',6,11,3,5).dot('union-face','copper_edge',6,11);fs.append(p)
 p=Frame('neutral-vent','SteamVent','Low inset metal grille with two dark slots inside a dull stone-green surround.',limits='Neutral opening only; no drawn steam, plume, heat, safety or pressure cue.')
 p.rect('grille-body','stone_dark',2,15,12,8).rect('upper-edge','stone_light',3,15,10,1)
 p.rect('left-edge','stone',2,16,2,7).rect('right-edge','stone',12,16,2,7).rect('lower-edge','stone',4,22,8,1)
 p.rect('upper-slot','ink',4,17,8,1).rect('lower-slot','ink',4,20,8,1).rect('crossbar','stone',4,19,8,1);fs.append(p)
 p=Frame('millstone','MillStone','Broad pale circular grinding stone with a square axle opening and one plain radial groove.',limits='No stoneburr, seed, active rotation, grinding contents or cast shadow.')
 p.rect('ring-top','linen',5,11,6,1).rect('ring-shoulder','stone_light',3,12,10,2).rect('ring-body','stone_light',2,14,12,6)
 p.rect('ring-low','stone',3,20,10,2).rect('ring-base','stone',5,22,6,1)
 p.rect('ring-upper-face','linen',4,13,8,1).rect('axle-inset','stone',6,15,4,4).clear(7,16,2,2)
 p.rect('radial-groove','stone',10,15,4,1).rect('worn-left','linen',2,15,1,3);fs.append(p)
 assert len(fs)==18
 return fs

def model(frame,materials,scene,index):
 collection=bpy.data.collections.new(f'{index:02d}-{frame.name}');scene.collection.children.link(collection);grouped={}
 for y,row in enumerate(frame.cells):
  for x,v in enumerate(row):
   if v:
    vertices,faces=grouped.setdefault(v,([],[]));add_quad(vertices,faces,index*16+x,23-y,index*16+x+1,24-y)
 for (part,color),(verts,faces) in sorted(grouped.items()):
  mesh=bpy.data.meshes.new(frame.name+'/'+part);mesh.from_pydata(verts,[],faces);mesh.materials.append(materials[color]);mesh.update()
  obj=bpy.data.objects.new(part,mesh);obj['authoring']='Original integer-cell geometry; direct native render.';obj['paletteHex']=P[color];collection.objects.link(obj)
 return collection

def write_meta(path):
 if path.exists():return
 path.write_text('''fileFormatVersion: 2
guid: GUID
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  streamingMipmaps: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 0
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spriteExtrude: 0
  spriteMeshType: 0
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 16
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: SPRITEID
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  userData:
  assetBundleName:
  assetBundleVariant:
'''.replace('GUID',uuid.uuid4().hex).replace('SPRITEID',uuid.uuid4().hex))

def main():
 OUT.mkdir(parents=True,exist_ok=True);bpy.context.preferences.filepaths.save_version=0
 atlas=bpy.context.scene;atlas.name='Native east kitchen 288x24';bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 fs=frames();materials={k:emission_material(k+' #'+v,v) for k,v in P.items()};collections=[model(f,materials,atlas,i) for i,f in enumerate(fs)]
 camera_for(atlas,288,24);atlas.render.filepath=str(OUT/'east-kitchen.png');bpy.ops.render.render(write_still=True,scene=atlas.name)
 contact=bpy.data.scenes.new('East kitchen contact direct 8x geometry');camera_for(contact,192,128,scale=8);plane(contact,'Contact background','032F23',0,0,192,128,-1)
 for i,(f,col) in enumerate(zip(fs,collections)):
  instance(contact,col,f'{i:02d} {f.name}',i%6*32+8-i*16,94-i//6*36)
  add_label(contact,f'{i:02d} {f.name}',i%6*32+16,90-i//6*36,materials['linen'],1.35)
 add_label(contact,'EAST / KITCHEN - ORIGINAL NATIVE 16 x 24',96,123,materials['cream'],2.1)
 add_label(contact,'18 exact owner/state cells | neutral services | no generated pixels | approved Cook unchanged',96,4,materials['linen'],1.3)
 contact.render.filepath=str(OUT/'east-kitchen-contact.png');bpy.ops.render.render(write_still=True,scene=contact.name)
 source={'schema':1,'coordinateSystem':'Top-left origin; y downward. Every non-null cell is one original flat mesh quad.','palette':P,'frames':[{'index':i,'id':f.name,'blueprint':f.blueprint,'state':f.state,'cells':f.cells} for i,f in enumerate(fs)]};(OUT/'source-cells.json').write_text(json.dumps(source,indent=2)+'\n')
 m={'schema':1,'artifact':'east-kitchen.png','width':288,'height':24,'frameWidth':16,'frameHeight':24,'frameCount':18,'pixelsPerUnit':16,'tilePivotNormalized':[.5,.5],'filterMode':'Point','wrapMode':'Clamp','mipmaps':False,'compression':'None','alpha':'Binary; independently checked.','source':'east-kitchen.blend','editableCells':'source-cells.json','generator':'Tools/Live16x24/build_east_kitchen.py','resource':'Sprites/Live16x24/east-kitchen','references':['ArtSource/Live16x24/concepts/east-kitchen-v1.png','ArtSource/Live16x24/concepts/east-kitchen-v2.png','ArtSource/Live16x24/anchors/opening-anchors-contact.png'],'scope':'Original static source kit only. No Unity import, runtime selectors, new behavior, state-loop or ordinary gameplay acceptance.','sha256':hashlib.sha256((OUT/'east-kitchen.png').read_bytes()).hexdigest(),'frames':[]}
 m.update({'baselineCaptures': ['Docs/Verification/Live16x24/east-kitchen-baseline-forge-reader.png', 'Docs/Verification/Live16x24/east-kitchen-baseline-kitchen-layout.png', 'Docs/Verification/Live16x24/east-kitchen-baseline-pan-actions.png'], 'conceptPrompts': ['ArtSource/Live16x24/concepts/east-kitchen-v1-prompt.md', 'ArtSource/Live16x24/concepts/east-kitchen-v2-prompt.md'], 'sourceEvidence': {'red': 'source-red.txt', 'firstPassFindings': 'source-first-pass-findings.txt', 'green': 'source-green.txt', 'verification': 'verification.json'}, 'nativeReview': 'Independent final source/contact review found no actionable art or semantic defect. This is not runtime or gameplay acceptance.'})
 for i,f in enumerate(fs):
  pts=[(x,y) for y,row in enumerate(f.cells) for x,v in enumerate(row) if v];colors=sorted({P[v[1]] for row in f.cells for v in row if v});assert 3<=len(colors)<=6 and max(y for x,y in pts)==22,(f.name,colors)
  m['frames'].append({'index':i,'id':f.name,'blueprint':f.blueprint,'state':f.state,'rect':[i*16,0,16,24],'description':f.description,'limits':f.limits,'occupiedPixels':len(pts),'palette':colors,'alphaBoundsTopLeft':[min(x for x,y in pts),min(y for x,y in pts),max(x for x,y in pts)+1,23],'baselineTopLeft':23,'footprintCells':[1,1]})
 for name in ('east-kitchen','east-kitchen-cook'):write_meta(RESOURCE/(name+'.png.meta'))
 shutil.copyfile(OUT/'east-kitchen.png',RESOURCE/'east-kitchen.png')
 cook=ROOT/'Assets/Art/Cook16x24/cook-16x24.png';shutil.copyfile(cook,RESOURCE/'east-kitchen-cook.png')
 m['approvedCook']={'source':str(cook.relative_to(ROOT)),'resource':'Sprites/Live16x24/east-kitchen-cook','byteCopy':True,'sha256':hashlib.sha256(cook.read_bytes()).hexdigest()}
 (OUT/'manifest.json').write_text(json.dumps(m,indent=2)+'\n');bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'east-kitchen.blend'));print('Wrote',OUT)
if __name__=='__main__':main()
