"""Read-only full-frame verification of the actual Unity screenshot.

No source or capture images are modified or produced. Compare their RGBA buffers
against exact native sprite placement at 4x, including every background pixel.
"""
import hashlib, json, sys
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
EVIDENCE=ROOT/'Docs/Verification/Clearing16x24'
name=sys.argv[1] if len(sys.argv)>1 else 'clearing-game-final.png'
report_name=sys.argv[2] if len(sys.argv)>2 else 'capture-verification.json'
capture=Image.open(EVIDENCE/name).convert('RGBA')
assert capture.size==(1280,800),capture.size
placement=json.loads((EVIDENCE/'unity-assembly.json').read_text())['result']['data']['result']['placements']
expected=bytearray(bytes((3,47,35,255))*(1280*800))
for p in sorted(placement,key=lambda p:p['layer']):
    sprite=Image.open(ROOT/p['asset']).convert('RGBA');w,h=sprite.size
    left=round(64*p['x']+32-2*w);top=round(784-96*p['y']-4*h)
    raw=sprite.tobytes()
    for y in range(h):
        for x in range(w):
            rgba=raw[(y*w+x)*4:(y*w+x+1)*4]
            assert rgba[3] in (0,255),(p['id'],rgba)
            if rgba[3]==0:continue
            for oy in range(4):
                yy=top+y*4+oy
                if not 0<=yy<800:continue
                for ox in range(4):
                    xx=left+x*4+ox
                    if 0<=xx<1280:
                        pos=(yy*1280+xx)*4;expected[pos:pos+4]=rgba
actual=capture.tobytes()
different=sum(actual[i:i+4]!=expected[i:i+4] for i in range(0,len(actual),4))
report={'capture':name,'size':list(capture.size),'sourcePixelScale':4,'placements':len(placement),
        'differentPixels':different,'captureSha256':hashlib.sha256((EVIDENCE/name).read_bytes()).hexdigest(),
        'contract':'Every actual captured RGBA pixel matches the exact unmodified native source sprite composite or flat #032F23 background.'}
(EVIDENCE/report_name).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
assert different==0,report
