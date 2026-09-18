#!/usr/bin/env python3
"""Add four-facing casting sheets without rewriting the existing actor slice.

Imports the original palette/anatomy helpers, preserving face, costume, boot
registration, 16x24 canvas and bottom-center pivot. Four cast beats show gather,
bind/read, release, and settle. Non-humanoid definitions retain Attack fallback.

Run: python3 ArtTools/coo_casting_poses.py
Outputs only *_cast.png, missing import metadata, and a contact sheet in Docs.
EntityVisuals.json registrations are intentionally authored independently.
"""
from pathlib import Path
import uuid

from PIL import Image, ImageDraw, ImageOps

import coo_vertical_slice as base

REPO = Path(__file__).resolve().parents[1]
OUT = REPO / "Assets/Resources/Sprites/Actors"
ROLES = (
    ("player", "player", "Open book / palm"),
    ("villager", "villager", "Gather / hand binding"),
    ("merchant", "merchant", "Folded account / counting"),
    ("elder", "elder", "Staff / held gesture"),
    ("warden", "warden", "Brace / ward gesture"),
    ("child", "child", "Cupped hands / small charm"),
    ("recension_echo", "recension", "Inscription / ruled book"),
)
PAPER = base.rgba((224, 216, 174))
PAPER_SHADOW = base.rgba((141, 126, 85))


def arm(draw, shoulder, hand, palette, far=False):
    elbow = ((shoulder[0] + hand[0]) // 2, max(shoulder[1], hand[1]) + 1)
    draw.line((shoulder, elbow, hand), fill=base.INK, width=3)
    draw.line((shoulder, elbow), fill=palette.body_dark if far else palette.body_light, width=2)
    draw.line((elbow, hand), fill=palette.skin_dark if far else palette.skin, width=1)
    draw.point(hand, fill=palette.skin_light)


def book(draw, x, y, phase, palette, facing, role):
    # A closed binding opens into two registered page leaves, then closes.
    opened = phase in (1, 2)
    half = 3 if opened else 2
    if facing == 3:
        # Read from behind: show the dark cover, never reversed page lettering.
        draw.rectangle((x-half, y, x+half, y+3), fill=base.INK)
        draw.line((x-half+1, y+1, x+half-1, y+1), fill=palette.accent)
        draw.point((x, y+2), fill=palette.accent_light)
        return
    draw.rectangle((x-half, y, x+half, y+3), fill=base.INK)
    draw.rectangle((x-half+1, y, x+half-1, y+2), fill=PAPER if opened else palette.accent)
    if opened:
        draw.line((x, y, x, y+3), fill=PAPER_SHADOW)
        draw.point((x-1, y+1), fill=palette.body_dark)
        draw.point((x+1, y+2), fill=palette.body_dark)
        if role == "recension":
            draw.line((x+2, y-3, x, y+1), fill=base.INK)
            draw.point((x+2, y-3), fill=PAPER)


def casting_frame(role, facing, phase):
    palette = base.PALETTES[role]
    child = role == "child"
    top = 7 if child else 3
    side = facing in (1, 2)
    authored_facing = 2 if side else facing
    neutral = base.humanoid_frame(palette, authored_facing, 0, 0, role, child=child)
    result = base.blank()
    draw = ImageDraw.Draw(result)

    # Rebuild the upper body, reusing the neutral head and registered boots.
    # None of the attack-arm pixels are reused in these authored cast poses.
    draw.polygon(((5, top+7), (10 if side else 11, top+7), (12, 18), (4, 18)), fill=base.INK)
    draw.polygon(((6, top+8), (10, top+8), (11, 18), (5, 18)), fill=palette.body)
    draw.line((5, top+9, 5, 17), fill=palette.body_dark, width=1)
    draw.line((7, top+9, 10, top+9), fill=palette.body_light)
    result.alpha_composite(neutral.crop((0, 18, 16, 24)), (0, 18))

    reads = role in ("player", "recension", "merchant")
    if side:
        far_hand = (6, top+11) if reads else ((8, top+11), (10, top+8), (11, top+7), (9, top+10))[phase]
        near_hand = ((10, top+11), (12, top+7), (13, top+5), (11, top+9))[phase]
        arm(draw, (6, top+8), far_hand, palette, far=True)
        arm(draw, (9, top+8), near_hand, palette)
    else:
        far_hand = (6, top+11) if reads else ((6, top+11), (4, top+7), (3, top+6), (5, top+10))[phase]
        near_hand = ((10, top+11), (11, top+7), (12, top+5), (11, top+9))[phase]
        arm(draw, (5, top+8), far_hand, palette, far=True)
        arm(draw, (11, top+8), near_hand, palette)

    # Head keeps the exact original facing silhouette and facial registration.
    # Staff tips belong to the newly placed staff, not the cropped head.
    if role in ("elder", "warden"):
        result.alpha_composite(neutral.crop((3, 0, 13, top+8)), (3, 0))
    else:
        result.alpha_composite(neutral.crop((0, 0, 16, top+8)), (0, 0))
    draw = ImageDraw.Draw(result)
    if role == "player":
        draw.line((6, top+8, 10, top+10), fill=palette.accent_light)
        draw.rectangle((11, 17, 13, 20), fill=base.INK)
        draw.rectangle((12, 17, 13, 19), fill=palette.accent)
    elif role == "villager":
        draw.line((5, 18, 10, 18), fill=palette.accent)
    elif role in ("elder", "warden"):
        staff_x = 3 if side else 2
        draw.line((staff_x, top+5, staff_x, 22), fill=base.INK, width=2)
        draw.line((staff_x, top+6, staff_x, 21), fill=base.rgba((116, 87, 53)))
        draw.point((staff_x, top+5), fill=palette.accent_light)
        if role == "warden":
            draw.line((staff_x, top+3, staff_x, top+5), fill=palette.accent_light)
    if reads:
        book(draw, 9 if side else 8, min(17, top+11), phase, palette, authored_facing, role)

    # A two-pixel knot marks the held intention; school material comes from FX.
    if phase in (1, 2):
        px, py = near_hand
        draw.point((px, py-2), fill=palette.accent_light)
        if phase == 2:
            draw.point((min(14, px+1), py-3), fill=palette.accent)
    return ImageOps.mirror(result) if facing == 1 else result


def write_meta(path):
    metadata = Path(str(path) + ".meta")
    if metadata.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "caves-of-ooo:casting:" + path.name).hex
    metadata.write_text(f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spriteExtrude: 0
  spriteMeshType: 0
  spritePixelsToUnits: 16
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    overridden: 0
""")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    contact = Image.new("RGB", (896, 700), (29, 33, 30))
    caption = ImageDraw.Draw(contact)
    caption.text((12, 8), "CASTING POSES | Gather > Bind / read > Release > Settle | Rows S W E N | 16 x 24, 4 frames", fill=(224, 216, 174))
    for index, (name, role, label) in enumerate(ROLES):
        sheet = base.blank((64, 96))
        for facing in range(4):
            seen = set()
            for phase in range(4):
                frame = casting_frame(role, facing, phase)
                assert set(frame.getchannel("A").tobytes()) <= {0, 255}
                assert frame.getbbox()[3] == 23 or frame.getbbox()[3] == 24, "Foot registration drift"
                seen.add(frame.tobytes())
                sheet.alpha_composite(frame, (phase*16, facing*24))
            assert len(seen) == 4, f"{name}/{facing}: every cast beat must differ"
        path = OUT / f"{name}_cast.png"
        sheet.save(path)
        write_meta(path)
        panel_x, panel_y = (index % 4)*224 + 12, (index // 4)*320 + 38
        caption.text((panel_x, panel_y), name.replace("_", " ").upper(), fill=(237, 229, 202))
        caption.text((panel_x, panel_y+15), label, fill=(156, 181, 166))
        enlarged = sheet.resize((192, 288), Image.Resampling.NEAREST)
        contact.paste(enlarged, (panel_x, panel_y+32), enlarged)
    contact.save(REPO / "Docs/SpellFx-casting-contact.png")
    print("Generated 7 additive casting sheets: 112 registered 16x24 frames; binary alpha and distinct beats verified.")


if __name__ == "__main__":
    main()
