"""Read-only frame census. Color masks are heuristic, not scene segmentation."""
import json,hashlib,statistics
from collections import Counter
from pathlib import Path
from PIL import Image
sources={
 'sixth':'/Users/steven/caves-of-ooo/Docs/Verification/DensityCompletion/ReferenceGlade/Native/4281297b663744e0bc6de61ec5d80201/03-world-only-full-reveal.png',
 'reference':'/var/folders/rz/pncy35b16zs05n4vzkj7vmkr0000gn/T/codex-clipboard-88c2ee34-791e-4765-a3de-6b1b0aeb3805.png'}
results={}
for label,path in sources.items():
 image=Image.open(path).convert('RGB');counts=Counter(image.get_flattened_data());w,h=image.size
 regions={}
 for name,relative in [('centerfloor',(.43,.31,.74,.37)),('rightfloor',(.72,.52,.81,.64))]:
  bounds=tuple(int(relative[i]*(w if i%2==0 else h)) for i in range(4))
  pixels=list(image.crop(bounds).get_flattened_data());floor=[p for p in pixels if p[0]<30 and 25<p[1]<90 and 20<p[2]<90]
  luma=[.2126*p[0]+.7152*p[1]+.0722*p[2] for p in floor]
  regions[name]={'bounds':bounds,'maskCount':len(floor),'unique':len(set(floor)),'meanLuma8bit':statistics.mean(luma),'stdLuma8bit':statistics.pstdev(luma),'top5':Counter(floor).most_common(5)}
 results[label]={'source':path,'sha256':hashlib.sha256(Path(path).read_bytes()).hexdigest(),'width':w,'height':h,'uniqueRGB':len(counts),'top10':counts.most_common(10),'modalPixelFraction':counts.most_common(1)[0][1]/(w*h),'regions':regions}
results['limits']='Different authored content and screen scale; color count alone cannot isolate antialiasing, contact occlusion, source compression/noise or geometry. Rough color masks include some shadows/props. Actual matched GPU readbacks are required to isolate material, shadow and sampling effects.'
print(json.dumps(results,indent=2))
