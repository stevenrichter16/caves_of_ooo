"""Source-derived static poses for Morrowfast's two additional native residents.

Only the two extracted body PNGs are inputs. No contact collar, ground, new pose,
resampling, source scene change or synthesized animation is part of this export.
"""
from __future__ import annotations
import colorsys
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Morrowfast'
DESTINATION = ROOT / 'Assets/Resources/SceneArt/Morrowfast/Art/actors'
REVIEW = SOURCE / 'Integration/actors'
FRAME_SIZE = (48, 64)
ACTORS = [
    {'id':'farra', 'name':'Farra Sprig', 'blueprint':'MorrowfastFarra', 'source':'build/sprites/western-shopkeeper-sprite.png', 'offset':[5,3], 'sourceFoot':[19,55], 'hatHue':.27, 'hatName':'weathered olive'},
    {'id':'edden', 'name':'Edden Brack', 'blueprint':'MorrowfastEdden', 'source':'build/sprites/east-robed-resident-sprite.png', 'offset':[4,3], 'sourceFoot':[20,56], 'hatHue':.55, 'hatName':'muted blue-grey'},
]

def derive_body(body: Image.Image, hue: float) -> Image.Image:
    result = body.copy()
    for y in range(min(14, body.height)):
        for x in range(body.width):
            r,g,b,a = body.getpixel((x,y))
            if not a: continue
            _, saturation, value = colorsys.rgb_to_hsv(r/255, g/255, b/255)
            if saturation < .08 or value < .13: continue
            rgb = colorsys.hsv_to_rgb(hue, min(.35,saturation*.85), value)
            result.putpixel((x,y),tuple(round(c*255) for c in rgb)+(a,))
    return result

def export(source: Path = SOURCE, destination: Path = DESTINATION) -> dict:
    destination.mkdir(parents=True, exist_ok=True)
    report = {'schemaVersion':1,'sourceScene':'morrowfast','frameWidth':48,'frameHeight':64,'pixelsPerUnit':41,
        'sourceOwnersChanged':0,'actors':[]}
    for actor in ACTORS:
        source_path = source / actor['source']
        with Image.open(source_path) as image: body = image.convert('RGBA')
        derived = derive_body(body, actor['hatHue'])
        frame = Image.new('RGBA', FRAME_SIZE)
        frame.paste(derived, tuple(actor['offset']))
        sheet = Image.new('RGBA',(FRAME_SIZE[0]*4,FRAME_SIZE[1]*16))
        for row in range(16): sheet.paste(frame,(0,row*FRAME_SIZE[1]))
        filename = f"morrowfast_{actor['id']}_sheet.png"
        sheet.save(destination/filename, optimize=True)
        foot_x,foot_y = actor['sourceFoot']; dx,dy = actor['offset']
        report['actors'].append({'id':actor['id'],'name':actor['name'],'blueprint':actor['blueprint'],
            'visualId':'actor.morrowfast_'+actor['id'],'file':filename,'source':actor['source'],
            'sourceSha256':hashlib.sha256(source_path.read_bytes()).hexdigest(),
            'sheetSha256':hashlib.sha256((destination/filename).read_bytes()).hexdigest(),
            'sourceBodySize':list(body.size),'bodyOffset':actor['offset'],
            'pivot':[ (dx+foot_x)/48, (64-dy-foot_y)/64 ],
            'paletteChange':{'region':'upper 14 source rows, alpha > 0, saturation >= 0.08, value >= 0.13',
                'hat':actor['hatName'],'hue':actor['hatHue'],'saturation':'min(0.35, original * 0.85)','value':'unchanged'},
            'motion':'static source pose repeated for four facings and four states',
            'unusedColumns':'three fully transparent columns; frame count is one for every state',
            'contactsIncluded':False})
    (destination/'actors-provenance.json').write_text(json.dumps(report,indent=2)+'\n')
    return report

def review_montage(report: dict, source: Path = SOURCE, destination: Path = DESTINATION, review: Path = REVIEW):
    review.mkdir(parents=True,exist_ok=True)
    canvas = Image.new('RGB',(576,408),(28,32,30)); draw=ImageDraw.Draw(canvas)
    try:
        font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',16)
        small=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',12)
    except OSError: font=small=ImageFont.load_default()
    draw.text((20,15),'Morrowfast · additional residents',font=font,fill=(228,225,205))
    draw.text((20,41),'One original pose; body alpha preserved; hat palette only.',font=small,fill=(168,182,165))
    for i,record in enumerate(report['actors']):
        left=20+i*286
        original=Image.open(source/record['source']).convert('RGBA')
        derived=Image.open(destination/record['file']).convert('RGBA').crop((0,0,48,64))
        canvas.paste(original.resize((original.width*2,original.height*2),Image.Resampling.NEAREST),(left,123),original.resize((original.width*2,original.height*2),Image.Resampling.NEAREST))
        scaled=derived.resize((144,192),Image.Resampling.NEAREST)
        canvas.paste(scaled,(left+98,94),scaled)
        draw.text((left,76),record['name'],font=font,fill=(230,217,176))
        draw.text((left,297),'source body ×2',font=small,fill=(180,183,171))
        draw.text((left+116,297),'derived ×3',font=small,fill=(180,183,171))
        draw.text((left,329),record['paletteChange']['hat']+' cap',font=small,fill=(200,206,186))
        draw.text((left,350),'48×64 frame · 41 PPU',font=small,fill=(165,180,163))
    draw.text((20,382),'No collars or backdrop pixels. No invented directional or action frames.',font=small,fill=(166,179,163))
    canvas.save(review/'comparison.png')
    (review/'provenance.json').write_text(json.dumps(report,indent=2)+'\n')

if __name__=='__main__':
    result=export();review_montage(result)
    print(json.dumps({'actors':len(result['actors']),'resources':str(DESTINATION),'review':str(REVIEW/'comparison.png')}))
