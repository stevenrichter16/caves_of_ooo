"""Export editable native integer cells; never reads or alters another image.

Run python3 Tools/Live16x24/export_cellar_floor.py. Outputs stay under ArtSource:
no Unity import, background generation, downsampling or resource copy is implicit.
"""
from pathlib import Path
import hashlib
import json
import struct
import zlib

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/cellar-floor'


def png(path, width, height, pixels):
    assert len(pixels) == width * height * 4
    def chunk(kind, body):
        return struct.pack('>I', len(body)) + kind + body + struct.pack('>I', zlib.crc32(kind + body) & 0xffffffff)
    scan = b''.join(b'\0' + pixels[y * width * 4:(y + 1) * width * 4] for y in range(height))
    data = (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>2I5B', width, height, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(scan, 9)) + chunk(b'IEND', b''))
    path.write_bytes(data)
    return hashlib.sha256(data).hexdigest()


def main():
    source = json.loads((OUT / 'source-cells.json').read_text())
    assert (source['width'], source['height'], len(source['frames'])) == (16, 24, 4)
    assert source['rowOrder'] == 'top_to_bottom'
    palette = {key: bytes.fromhex(value) + b'\xff' for key, value in source['palette'].items()}
    assert len(palette) == 3 and all(len(value) == 4 for value in palette.values())
    metrics = []
    for frame in source['frames']:
        rows = frame['rows']
        assert len(rows) == 24 and all(len(row) == 16 for row in rows)
        assert set(''.join(rows)) <= set(palette)
        marked = sum(pixel != '0' for row in rows for pixel in row)
        assert marked <= 12, 'Keep the cellar floor subordinate to objects.'
        metrics.append({'id': frame['id'], 'markedPixels': marked, 'basePixels': 384 - marked})
    pixels = b''.join(palette[key] for y in range(24) for frame in source['frames'] for key in frame['rows'][y])
    native_hash = png(OUT / 'cellar-floor.png', 64, 24, pixels)
    # Source contact only: exact integer replication, never a gameplay capture.
    contact = b''.join(pixels[((y // 8) * 64 + x // 8) * 4:((y // 8) * 64 + x // 8) * 4 + 4]
                       for y in range(192) for x in range(512))
    contact_hash = png(OUT / 'cellar-floor-contact-8x.png', 512, 192, contact)
    manifest = {'schema': 1, 'status': source['status'], 'source': 'source-cells.json',
                'generator': 'Tools/Live16x24/export_cellar_floor.py', 'artifact': 'cellar-floor.png',
                'width': 64, 'height': 24, 'frameWidth': 16, 'frameHeight': 24, 'frameCount': 4,
                'pixelsPerUnit': 16, 'tilePivotNormalized': [0.5, 0.5], 'filterMode': 'Point',
                'wrapMode': 'Clamp', 'mipmaps': False, 'compression': 'None', 'alpha': 'opaque255',
                'palette': source['palette'], 'frames': metrics, 'sha256': native_hash,
                'sourceContactSha256': contact_hash, 'resource': 'Sprites/Live16x24/cellar-floor',
                'limits': 'Not imported. No material variant routing, runtime authority, Unity tests or actual Play acceptance yet.'}
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(json.dumps({'nativeSha256': native_hash, 'frames': metrics, 'runtimeImported': False}))


if __name__ == '__main__':
    main()
