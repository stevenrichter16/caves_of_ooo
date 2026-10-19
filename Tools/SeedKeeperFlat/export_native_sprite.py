"""Export opaque native sprite pixels for SeedKeeperFlatBuilder; never resize art."""
import argparse
import json
from pathlib import Path

from PIL import Image


def export(source_path, destination):
    with Image.open(source_path) as source:
        image = source.convert("RGBA")
    if image.size != (16, 24):
        raise ValueError("The native Seed Keeper canvas must be exactly 16 by 24.")
    palette, pixels = [], []
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, alpha = image.getpixel((x, y))
            if alpha not in (0, 255):
                raise ValueError("Native art must have binary alpha; no antialiasing.")
            if alpha == 0:
                continue
            swatch = f"#{r:02x}{g:02x}{b:02x}"
            if swatch not in palette:
                palette.append(swatch)
            pixels.append({"x": x, "y": y, "color": palette.index(swatch)})
    if len(pixels) < 100 or len(palette) > 256:
        raise ValueError("Sprite does not satisfy the flat keeper source bounds.")
    data = {"schemaVersion": 1, "width": 16, "height": 24,
            "palette": palette, "pixels": pixels}
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(data, indent=2) + "\n")
    print(f"{destination}: {len(pixels)} opaque native pixels, {len(palette)} colors")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    export(args.source, args.destination)
