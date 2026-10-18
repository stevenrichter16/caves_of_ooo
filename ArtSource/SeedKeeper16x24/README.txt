SEED KEEPER — exact native pixel sprite

Native canvas: 16 × 24 pixels, transparent RGBA PNG
Opaque palette: 8 colors
Visible pixels: 151
Transparent pixels: 233
Alpha values: 0 and 255 only

seed-keeper-16x24.png — native production asset
seed-keeper-16x24-preview-8x.png — exact 8× nearest-neighbor enlargement on flat dark teal, 128 × 192
seed-keeper-16x24-preview.png — exact 32× nearest-neighbor enlargement on flat dark teal, 512 × 768
seed-keeper-16x24.svg — editable 16 × 24 SVG, each rect exactly one native pixel
sprite-grid.txt — editable literal character grid
generate.py — Python/Pillow source, authoritative palette and grid
sprite-info.json — palette and pixel counts

Created directly on a 16 × 24 grid. No downsampling, anti-aliasing, smoothing, or large-image source.
To edit, change GRID or PALETTE in generate.py and run: python generate.py
Use nearest-neighbor / point filtering when displaying the native PNG at larger sizes.
