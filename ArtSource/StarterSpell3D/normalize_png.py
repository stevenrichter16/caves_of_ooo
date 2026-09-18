"""Drop Blender's non-square pHYs metadata without touching pixel/IDAT bytes."""
import argparse, hashlib, json, struct
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def chunks(raw):
 assert raw[:8]==b'\x89PNG\r\n\x1a\n', 'not a PNG'
 position=8
 while position<len(raw):
  size=struct.unpack('>I',raw[position:position+4])[0];block=raw[position:position+12+size]
  yield raw[position+4:position+8],block
  position+=12+size
def remove_density_chunk(path):
 path=Path(path);before=path.read_bytes();parts=list(chunks(before));removed=sum(kind==b'pHYs' for kind,_ in parts)
 if removed:
  after=before[:8]+b''.join(block for kind,block in parts if kind!=b'pHYs')
  # Stronger than a re-encode: every compressed pixel-data byte is unchanged.
  assert b''.join(block for kind,block in parts if kind==b'IDAT')==b''.join(block for kind,block in chunks(after) if kind==b'IDAT')
  path.write_bytes(after)
 return removed
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--check',action='store_true');args=ap.parse_args();rows=[]
 for path in sorted(list((ROOT/'renders').glob('*.png'))+list((ROOT/'frames').rglob('*.png'))):
  before=path.read_bytes();has=sum(kind==b'pHYs' for kind,_ in chunks(before));before_idat=hashlib.sha256(b''.join(block for kind,block in chunks(before) if kind==b'IDAT')).hexdigest()
  if not args.check:remove_density_chunk(path)
  after=path.read_bytes();remaining=sum(kind==b'pHYs' for kind,_ in chunks(after));after_idat=hashlib.sha256(b''.join(block for kind,block in chunks(after) if kind==b'IDAT')).hexdigest()
  rows.append(dict(path=str(path.relative_to(ROOT)),densityChunksBefore=has,densityChunksAfter=remaining,idatUnchanged=before_idat==after_idat,idatSha256=after_idat))
 report=dict(status='GREEN' if all(r['densityChunksAfter']==0 and r['idatUnchanged'] for r in rows) else 'RED',files=len(rows),densityChunksRemoved=sum(r['densityChunksBefore']-r['densityChunksAfter'] for r in rows),filesWithDensity=sum(r['densityChunksAfter']>0 for r in rows),rows=rows)
 print(json.dumps(report,indent=2))
 if report['status']!='GREEN':raise SystemExit(1)
if __name__=='__main__':main()
