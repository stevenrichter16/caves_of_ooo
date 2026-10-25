"""Export original editable 16x24 item cells with only the Python standard library.

Run python3 Tools/Live16x24/export_incidental_equipment.py. The exporter reads only
source-cells.json. Its fixed outputs stay under ArtSource/Live16x24/incidental-equipment;
it never reads/resizes a reference image, imports Unity resources, or edits other art.
"""
from pathlib import Path
import hashlib
import json
import struct
import zlib

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtSource/Live16x24/incidental-equipment'
FRAME_OUTPUTS = (('LeatherArmor', 'leather-armor.png'), ('Hatchet', 'hatchet.png'))


def png(path, width, height, pixels):
    if len(pixels) != width * height * 4:
        raise ValueError('Pixel buffer does not match the declared dimensions.')

    def chunk(kind, body):
        return (struct.pack('>I', len(body)) + kind + body
                + struct.pack('>I', zlib.crc32(kind + body) & 0xffffffff))

    scan = b''.join(b'\0' + pixels[y * width * 4:(y + 1) * width * 4]
                    for y in range(height))
    data = (b'\x89PNG\r\n\x1a\n'
            + chunk(b'IHDR', struct.pack('>2I5B', width, height, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(scan, 9)) + chunk(b'IEND', b''))
    path.write_bytes(data)
    return hashlib.sha256(data).hexdigest()


def main():
    source_bytes = (OUT / 'source-cells.json').read_bytes()
    source = json.loads(source_bytes)
    if (source['width'], source['height'], source['rowOrder']) != (16, 24, 'top_to_bottom'):
        raise ValueError('Source must declare exact native 16x24 top-to-bottom rows.')
    if [(f['id'], f['artifact']) for f in source['frames']] != list(FRAME_OUTPUTS):
        raise ValueError('Only the two explicitly owned item identities/output filenames are allowed.')
    palette = {key: bytes.fromhex(value) for key, value in source['palette'].items()}
    if palette.get('.') != b'\0\0\0\0' or any(
            len(key) != 1 or len(value) != 4 or (key != '.' and value[3] != 255)
            for key, value in palette.items()):
        raise ValueError('Use one-character keys, transparent dot and opaque RGBA colors.')

    # Validate both sources before writing any of the fixed owned outputs.
    frames = []
    for frame in source['frames']:
        rows = frame['rows']
        if len(rows) != 24 or any(len(row) != 16 for row in rows):
            raise ValueError('Every frame must contain 24 rows of exactly 16 cells.')
        if not set(''.join(rows)) <= set(palette):
            raise ValueError('Every cell must have an explicit palette entry.')
        occupied = [(x, y) for y, row in enumerate(rows) for x, key in enumerate(row) if key != '.']
        if not occupied:
            raise ValueError('An equipment frame cannot be empty.')
        pixels = b''.join(palette[key] for row in rows for key in row)
        xs, ys = zip(*occupied)
        frames.append((frame, pixels, {
            'id': frame['id'], 'artifact': frame['artifact'],
            'width': 16, 'height': 24, 'opaquePixels': len(occupied),
            'inkBoundsTopLeft': [min(xs), min(ys), max(xs) - min(xs) + 1, max(ys) - min(ys) + 1],
            'colorsUsed': sorted(set(''.join(rows)) - {'.'})}))

    metrics = []
    for frame, pixels, metric in frames:
        metric['sha256'] = png(OUT / frame['artifact'], 16, 24, pixels)
        metrics.append(metric)
    # Side-by-side source previews; these are not gameplay captures or runtime routing.
    native = b''.join(palette[key] for y in range(24)
                      for frame in source['frames'] for key in frame['rows'][y])
    native_hash = png(OUT / 'incidental-equipment.png', 32, 24, native)
    contact = b''.join(native[((y // 8) * 32 + x // 8) * 4:((y // 8) * 32 + x // 8) * 4 + 4]
                       for y in range(192) for x in range(256))
    contact_hash = png(OUT / 'incidental-equipment-contact-8x.png', 256, 192, contact)
    manifest = {
        'schema': 1, 'status': source['status'], 'source': 'source-cells.json',
        'sourceSha256': hashlib.sha256(source_bytes).hexdigest(),
        'generator': 'Tools/Live16x24/export_incidental_equipment.py',
        'frameWidth': 16, 'frameHeight': 24, 'frameCount': 2,
        'pixelsPerUnit': 16, 'pivotNormalized': [0.5, 0.5],
        'filterMode': 'Point', 'wrapMode': 'Clamp', 'mipmaps': False,
        'compression': 'None', 'alpha': 'binary0or255', 'palette': source['palette'],
        'frames': metrics, 'nativeContact': 'incidental-equipment.png',
        'nativeContactSha256': native_hash, 'sourceContact': 'incidental-equipment-contact-8x.png',
        'sourceContactSha256': contact_hash,
        'limits': 'Authoring manifest only. Runtime routing, native tests and bounded actual gameplay evidence are recorded in Docs/GRAPHICS-G3-INCIDENTAL-EQUIPMENT.md.'}
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'frames': metrics, 'runtimeModifiedByExporter': False}))


if __name__ == '__main__':
    main()
