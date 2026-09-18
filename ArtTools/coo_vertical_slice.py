#!/usr/bin/env python3
"""Generate the first animated Caves of Ooo visual slice.

Outputs standard 4-column x 16-row actor sheets. Rows are grouped as
Idle, Walk, Attack, Hurt, with South, West, East, North inside each group.
Each frame is 16x24 and uses opaque-or-empty pixels only.

Also outputs an actor contact shadow, four tree variants, and seamless
8x8-cell macro atlases for grass and Tepuibone.
"""

from __future__ import annotations

import math
import os
import random
from dataclasses import dataclass

from PIL import Image, ImageDraw, ImageOps


ROOT = os.path.normpath(os.path.join(
    os.path.dirname(__file__), "..", "Assets", "Resources", "Sprites"))
ACTOR_ROOT = os.path.join(ROOT, "Actors")
ENV_ROOT = os.path.join(ROOT, "Environment")

FRAME_W = 16
FRAME_H = 24
SHEET_COLS = 4
SHEET_ROWS = 16
INK = (22, 27, 24, 255)
DEEP = (31, 38, 33, 255)


@dataclass(frozen=True)
class Palette:
    body_dark: tuple[int, int, int, int]
    body: tuple[int, int, int, int]
    body_light: tuple[int, int, int, int]
    skin_dark: tuple[int, int, int, int]
    skin: tuple[int, int, int, int]
    skin_light: tuple[int, int, int, int]
    accent: tuple[int, int, int, int]
    accent_light: tuple[int, int, int, int]


def rgba(rgb):
    return tuple(rgb) + (255,)


PALETTES = {
    "player": Palette(
        rgba((48, 70, 58)), rgba((72, 103, 78)), rgba((111, 137, 91)),
        rgba((120, 96, 70)), rgba((174, 139, 95)), rgba((215, 183, 127)),
        rgba((53, 139, 153)), rgba((111, 200, 191))),
    "villager": Palette(
        rgba((71, 66, 48)), rgba((108, 98, 65)), rgba((149, 130, 83)),
        rgba((115, 82, 57)), rgba((168, 121, 77)), rgba((213, 165, 108)),
        rgba((61, 115, 130)), rgba((98, 166, 173))),
    "merchant": Palette(
        rgba((66, 42, 50)), rgba((112, 57, 70)), rgba((157, 82, 88)),
        rgba((105, 72, 54)), rgba((163, 112, 75)), rgba((213, 164, 105)),
        rgba((190, 143, 55)), rgba((236, 198, 84))),
    "elder": Palette(
        rgba((49, 57, 52)), rgba((72, 83, 72)), rgba((112, 124, 100)),
        rgba((105, 78, 58)), rgba((157, 115, 80)), rgba((205, 161, 110)),
        rgba((165, 178, 169)), rgba((224, 226, 205))),
    "warden": Palette(
        rgba((38, 58, 64)), rgba((55, 88, 91)), rgba((88, 125, 118)),
        rgba((102, 75, 55)), rgba((158, 112, 74)), rgba((207, 160, 104)),
        rgba((168, 126, 49)), rgba((224, 184, 73))),
    "child": Palette(
        rgba((64, 62, 47)), rgba((102, 96, 61)), rgba((151, 130, 74)),
        rgba((110, 77, 53)), rgba((171, 119, 75)), rgba((219, 167, 109)),
        rgba((63, 122, 128)), rgba((107, 177, 168))),
    "recension": Palette(
        rgba((39, 47, 68)), rgba((61, 75, 107)), rgba((92, 111, 150)),
        rgba((104, 78, 62)), rgba((162, 119, 82)), rgba((210, 169, 116)),
        rgba((184, 178, 143)), rgba((231, 222, 180))),
}


def blank(size=(FRAME_W, FRAME_H)):
    return Image.new("RGBA", size, (0, 0, 0, 0))


def add_outline(image: Image.Image, color=INK) -> Image.Image:
    source = image.load()
    result = blank(image.size)
    target = result.load()
    width, height = image.size
    for y in range(height):
        for x in range(width):
            if source[x, y][3] != 0:
                continue
            found = False
            for ox, oy in ((-1, -1), (0, -1), (1, -1), (-1, 0),
                           (1, 0), (-1, 1), (0, 1), (1, 1)):
                nx, ny = x + ox, y + oy
                if 0 <= nx < width and 0 <= ny < height and source[nx, ny][3] != 0:
                    found = True
                    break
            if found:
                target[x, y] = color
    result.alpha_composite(image)
    return result


def shift_opaque(image: Image.Image, dx: int, dy: int) -> Image.Image:
    moved = blank(image.size)
    moved.alpha_composite(image, (dx, dy))
    return moved


def humanoid_frame(
        palette: Palette,
        facing: int,
        state: int,
        frame: int,
        role: str,
        child: bool = False) -> Image.Image:
    image = blank()
    draw = ImageDraw.Draw(image)

    phase = frame % 4
    bob = -1 if state == 1 and phase in (1, 3) else 0
    if state == 0 and phase == 2:
        bob = -1
    hurt_dx = (-1, 1, -1, 0)[phase] if state == 3 else 0
    top = (7 if child else 3) + bob
    foot = 22
    cx = 8 + hurt_dx
    side = facing in (1, 2)
    west = facing == 1

    if side:
        # Author east, then mirror west after details and outline.
        leg_front = 9 + (1 if state == 1 and phase in (0, 3) else 0)
        leg_back = 6 - (1 if state == 1 and phase in (1, 2) else 0)
        draw.rectangle((leg_back, foot - 5, leg_back + 2, foot - 1), fill=palette.body_dark)
        draw.rectangle((leg_front, foot - 5, leg_front + 2, foot - 1), fill=palette.body)
        draw.rectangle((leg_back - 1, foot - 1, leg_back + 2, foot), fill=DEEP)
        draw.rectangle((leg_front, foot - 1, leg_front + 3, foot), fill=DEEP)

        draw.polygon([
            (cx - 3, top + 7), (cx + 2, top + 7), (cx + 3, foot - 5),
            (cx - 3, foot - 5)], fill=palette.body)
        draw.rectangle((cx - 3, top + 8, cx - 2, foot - 6), fill=palette.body_dark)
        draw.line((cx - 1, top + 9, cx + 2, top + 10), fill=palette.body_light, width=1)

        attack_reach = (0, 1, 3, 1)[phase] if state == 2 else 0
        arm_y = top + 10
        draw.line((cx, arm_y, cx + 3 + attack_reach, arm_y + (phase & 1)),
                  fill=palette.skin_dark, width=2)
        draw.line((cx - 2, arm_y + 1, cx - 3, arm_y + 4),
                  fill=palette.body_dark, width=2)

        draw.rectangle((cx - 2, top + 1, cx + 2, top + 6), fill=palette.skin)
        draw.rectangle((cx - 2, top + 1, cx + 1, top + 2), fill=palette.skin_light)
        draw.point((cx + 2, top + 3), fill=INK)
        draw.point((cx + 3, top + 4), fill=palette.skin_dark)
        draw.rectangle((cx - 3, top, cx + 1, top + 1), fill=palette.body_dark)
        draw.point((cx - 3, top + 2), fill=palette.body_dark)

        draw_role_prop(draw, role, facing, state, phase, top, foot, cx, palette)
        if state == 3 and phase in (1, 2):
            draw.line((cx - 2, top + 8, cx + 2, top + 8), fill=palette.accent_light)

        image = add_outline(image)
        return ImageOps.mirror(image) if west else image

    # Front/back view.
    left_step = -1 if state == 1 and phase in (0, 3) else 0
    right_step = 1 if state == 1 and phase in (1, 2) else 0
    draw.rectangle((cx - 3 + left_step, foot - 5, cx - 1 + left_step, foot - 1),
                   fill=palette.body_dark)
    draw.rectangle((cx + 1 + right_step, foot - 5, cx + 3 + right_step, foot - 1),
                   fill=palette.body)
    draw.rectangle((cx - 4 + left_step, foot - 1, cx - 1 + left_step, foot), fill=DEEP)
    draw.rectangle((cx + 1 + right_step, foot - 1, cx + 4 + right_step, foot), fill=DEEP)

    draw.polygon([
        (cx - 3, top + 7), (cx + 3, top + 7), (cx + 4, foot - 5),
        (cx - 4, foot - 5)], fill=palette.body)
    draw.rectangle((cx - 4, top + 9, cx - 3, foot - 6), fill=palette.body_dark)
    draw.line((cx - 1, top + 8, cx + 2, top + 8), fill=palette.body_light)
    draw.rectangle((cx - 2, top + 11, cx + 2, top + 12), fill=palette.accent)

    attack = state == 2
    left_arm_x = cx - 5 - ((0, 1, 2, 1)[phase] if attack else 0)
    right_arm_x = cx + 5 + ((0, 1, 2, 1)[phase] if attack else 0)
    arm_walk = 1 if state == 1 and phase in (0, 3) else 0
    draw.line((cx - 3, top + 9, left_arm_x, top + 13 + arm_walk),
              fill=palette.body_dark, width=2)
    draw.line((cx + 3, top + 9, right_arm_x, top + 13 - arm_walk),
              fill=palette.body, width=2)
    draw.point((left_arm_x, top + 14 + arm_walk), fill=palette.skin)
    draw.point((right_arm_x, top + 14 - arm_walk), fill=palette.skin)

    if facing == 0:
        draw.rectangle((cx - 3, top + 1, cx + 3, top + 6), fill=palette.skin)
        draw.rectangle((cx - 3, top + 1, cx + 2, top + 2), fill=palette.skin_light)
        draw.point((cx - 2, top + 4), fill=INK)
        draw.point((cx + 2, top + 4), fill=INK)
        draw.point((cx, top + 6), fill=palette.skin_dark)
        draw.rectangle((cx - 4, top, cx + 3, top + 1), fill=palette.body_dark)
        draw.point((cx - 4, top + 2), fill=palette.body_dark)
        draw.point((cx + 3, top + 2), fill=palette.body_dark)
    else:
        draw.rectangle((cx - 3, top + 1, cx + 3, top + 6), fill=palette.body_dark)
        draw.rectangle((cx - 2, top + 2, cx + 2, top + 5), fill=palette.body)
        draw.line((cx - 1, top + 2, cx + 2, top + 2), fill=palette.body_light)

    draw_role_prop(draw, role, facing, state, phase, top, foot, cx, palette)
    if state == 3 and phase in (1, 2):
        draw.line((cx - 3, top + 9, cx + 3, top + 9), fill=palette.accent_light)
    return add_outline(image)


def draw_role_prop(draw, role, facing, state, phase, top, foot, cx, palette):
    attack = state == 2
    extension = (0, 1, 3, 1)[phase] if attack else 0
    if role == "player":
        # Sill-blue scarf and asymmetrical field satchel.
        draw.line((cx - 2, top + 8, cx + 2, top + 10), fill=palette.accent_light, width=1)
        draw.rectangle((cx + 2, top + 13, cx + 4, top + 16), fill=palette.accent)
        draw.point((cx + 3, top + 13), fill=palette.accent_light)
    elif role == "merchant":
        draw.rectangle((cx - 3, top + 14, cx + 3, top + 15), fill=palette.accent)
        draw.rectangle((cx + 2, top + 15, cx + 4, top + 18), fill=palette.accent_light)
        draw.point((cx - 1, top + 11), fill=palette.accent_light)
        draw.point((cx + 1, top + 11), fill=palette.accent_light)
    elif role == "elder":
        staff_x = cx + 5 + extension
        draw.line((staff_x, top + 8, staff_x, foot), fill=rgba((94, 69, 43)), width=1)
        draw.point((staff_x, top + 7), fill=palette.accent_light)
        draw.line((cx - 3, top, cx + 3, top), fill=palette.accent_light, width=1)
    elif role == "warden":
        spear_x = cx + 5 + extension
        draw.line((spear_x, top + 5, spear_x, foot), fill=rgba((111, 82, 47)), width=1)
        draw.polygon([(spear_x, top + 2), (spear_x - 1, top + 5),
                      (spear_x + 1, top + 5)], fill=palette.accent_light)
    elif role == "villager":
        draw.rectangle((cx - 3, top + 16, cx + 3, top + 16), fill=palette.accent)
    elif role == "child":
        draw.point((cx + 3, top + 10), fill=palette.accent_light)
    elif role == "recension":
        draw.rectangle((cx + 2, top + 12, cx + 5, top + 17), fill=palette.accent_light)
        draw.line((cx + 3, top + 13, cx + 4, top + 16), fill=palette.body_dark)
        draw.line((cx - 4, top + 5, cx - 6 - extension, top + 1), fill=palette.accent_light)


def snapjaw_frame(facing, state, frame):
    image = blank()
    draw = ImageDraw.Draw(image)
    body_dark = rgba((91, 50, 32))
    body = rgba((151, 80, 42))
    body_light = rgba((207, 126, 62))
    muzzle = rgba((191, 151, 86))
    accent = rgba((224, 194, 87))
    bob = -1 if state == 1 and frame in (1, 3) else 0
    attack = (0, 1, 3, 1)[frame] if state == 2 else 0

    if facing in (1, 2):
        # East profile, mirrored for west.
        draw.line((4, 13 + bob, 2, 10 + bob), fill=body_dark, width=2)
        draw.polygon([(4, 10 + bob), (10, 9 + bob), (12, 14 + bob),
                      (10, 19 + bob), (4, 18 + bob)], fill=body)
        draw.rectangle((4, 14 + bob, 6, 18 + bob), fill=body_dark)
        draw.rectangle((5, 18, 7, 22), fill=body_dark)
        draw.rectangle((10, 18, 12, 22), fill=body)
        draw.rectangle((4, 22, 7, 22), fill=DEEP)
        draw.rectangle((10, 22, 13, 22), fill=DEEP)
        draw.polygon([(9, 5 + bob), (13, 7 + bob), (14, 11 + bob),
                      (10, 13 + bob), (8, 9 + bob)], fill=body)
        draw.polygon([(9, 5 + bob), (10, 2 + bob), (12, 6 + bob)], fill=body_dark)
        draw.polygon([(12, 6 + bob), (14, 3 + bob), (14, 8 + bob)], fill=body_dark)
        draw.rectangle((12, 9 + bob, 14 + attack, 11 + bob), fill=muzzle)
        draw.point((12, 7 + bob), fill=accent)
        draw.line((8, 12 + bob, 12 + attack, 15 + bob), fill=body_light, width=2)
        result = add_outline(image)
        return ImageOps.mirror(result) if facing == 1 else result

    draw.line((4, 12 + bob, 2, 9 + bob), fill=body_dark, width=2)
    draw.line((12, 12 + bob, 14, 9 + bob), fill=body_dark, width=2)
    draw.polygon([(5, 9 + bob), (11, 9 + bob), (13, 17 + bob),
                  (10, 19 + bob), (6, 19 + bob), (3, 17 + bob)], fill=body)
    draw.rectangle((4, 12 + bob, 5, 18 + bob), fill=body_dark)
    draw.rectangle((5, 18, 7, 22), fill=body_dark)
    draw.rectangle((9, 18, 11, 22), fill=body)
    draw.rectangle((4, 22, 7, 22), fill=DEEP)
    draw.rectangle((9, 22, 12, 22), fill=DEEP)
    if facing == 0:
        draw.polygon([(5, 5 + bob), (8, 3 + bob), (11, 5 + bob),
                      (12, 10 + bob), (8, 12 + bob), (4, 10 + bob)], fill=body)
        draw.polygon([(5, 6 + bob), (4, 2 + bob), (7, 5 + bob)], fill=body_dark)
        draw.polygon([(10, 5 + bob), (12, 2 + bob), (11, 7 + bob)], fill=body_dark)
        draw.rectangle((6, 8 + bob, 10, 11 + bob + min(attack, 1)), fill=muzzle)
        draw.point((6, 7 + bob), fill=accent)
        draw.point((10, 7 + bob), fill=accent)
    else:
        draw.polygon([(5, 5 + bob), (8, 3 + bob), (11, 5 + bob),
                      (12, 10 + bob), (8, 12 + bob), (4, 10 + bob)], fill=body_dark)
        draw.line((6, 6 + bob, 10, 6 + bob), fill=body_light)
    if state == 3 and frame in (1, 2):
        draw.line((5, 13, 11, 13), fill=rgba((235, 151, 103)))
    return add_outline(image)


def oriented_square(image, facing):
    if facing == 0:
        return image
    if facing == 1:
        return image.transpose(Image.Transpose.ROTATE_270)
    if facing == 2:
        return image.transpose(Image.Transpose.ROTATE_90)
    return image.transpose(Image.Transpose.ROTATE_180)


def snake_frame(facing, state, frame, wardline=False):
    square = blank((16, 16))
    draw = ImageDraw.Draw(square)
    if wardline:
        dark, body, light = rgba((48, 62, 34)), rgba((86, 108, 53)), rgba((142, 151, 76))
        eye = rgba((225, 196, 84))
    else:
        dark, body, light = rgba((61, 53, 51)), rgba((112, 88, 79)), rgba((167, 132, 112))
        eye = rgba((207, 70, 58))
    wave = (0, 1, 0, -1)[frame]
    points = [(5 + wave, 2), (9, 4), (7 - wave, 7), (10, 10), (8, 13)]
    draw.line(points, fill=dark, width=5)
    draw.line(points, fill=body, width=3)
    draw.line([(6 + wave, 2), (9, 4), (8, 6)], fill=light, width=1)
    head_y = 13 + (1 if state == 2 and frame == 2 else 0)
    draw.ellipse((5, head_y - 2, 11, head_y + 2), fill=body)
    draw.line((6, head_y - 1, 9, head_y - 1), fill=light)
    draw.point((6, head_y), fill=eye)
    draw.point((10, head_y), fill=eye)
    if wardline:
        draw.point((8, 1), fill=light)
        draw.point((5, 8), fill=light)
    if state == 3 and frame in (1, 2):
        draw.line((5, 8, 11, 8), fill=eye)
    square = add_outline(square)
    square = oriented_square(square, facing)
    image = blank()
    image.alpha_composite(square, (0, 8))
    return image


def frog_frame(facing, state, frame, glasspane=False):
    image = blank()
    draw = ImageDraw.Draw(image)
    if glasspane:
        dark, body, light = rgba((42, 78, 63)), rgba((70, 126, 91)), rgba((128, 185, 132))
        belly, eye = rgba((125, 194, 174)), rgba((233, 210, 99))
    else:
        dark, body, light = rgba((52, 74, 78)), rgba((82, 123, 126)), rgba((147, 191, 179))
        belly, eye = rgba((192, 210, 188)), rgba((47, 42, 38))
    jump = -2 if state == 1 and frame in (1, 2) else 0
    attack = -2 if state == 2 and frame == 2 else 0
    y = 17 + jump + attack
    draw.rectangle((4, y, 11, y + 4), fill=body)
    draw.rectangle((5, y - 2, 10, y + 1), fill=body)
    draw.point((5, y - 3), fill=light)
    draw.point((10, y - 3), fill=light)
    draw.point((5, y - 2), fill=eye)
    draw.point((10, y - 2), fill=eye)
    draw.rectangle((6, y + 1, 9, y + 3), fill=belly)
    draw.line((4, y + 3, 1, y + 5), fill=dark, width=2)
    draw.line((11, y + 3, 14, y + 5), fill=dark, width=2)
    draw.line((5, y + 4, 3, y + 6), fill=dark, width=2)
    draw.line((10, y + 4, 12, y + 6), fill=dark, width=2)
    if not glasspane:
        # Young riding on the adult's back.
        for x, oy in ((6, -1), (8, -2), (10, -1)):
            draw.point((x, y + oy), fill=belly)
            draw.point((x, y + oy - 1), fill=dark)
    if state == 3 and frame in (1, 2):
        draw.line((5, y, 10, y), fill=rgba((232, 130, 107)))
    return add_outline(image)


def tortoise_frame(facing, state, frame):
    square = blank((16, 16))
    draw = ImageDraw.Draw(square)
    shell_dark = rgba((76, 59, 37))
    shell = rgba((126, 96, 52))
    shell_light = rgba((172, 132, 68))
    skin = rgba((116, 112, 63))
    yellow = rgba((224, 190, 55))
    step = 1 if state == 1 and frame in (1, 3) else 0
    draw.ellipse((3, 4, 12, 12), fill=shell)
    draw.line((5, 5, 10, 5), fill=shell_light)
    draw.line((5, 8, 10, 8), fill=shell_dark)
    draw.line((7, 5, 7, 11), fill=shell_dark)
    draw.rectangle((6, 11, 10, 14 + (1 if state == 2 and frame == 2 else 0)), fill=skin)
    draw.point((7, 13), fill=INK)
    draw.point((9, 13), fill=INK)
    draw.point((2 - step, 6), fill=yellow)
    draw.point((13 + step, 6), fill=yellow)
    draw.point((3, 12 + step), fill=yellow)
    draw.point((12, 12 - step), fill=yellow)
    if state == 3 and frame in (1, 2):
        draw.line((4, 7, 11, 7), fill=rgba((230, 142, 91)))
    square = add_outline(square)
    square = oriented_square(square, facing)
    image = blank()
    image.alpha_composite(square, (0, 8))
    return image


def make_sheet(frame_function) -> Image.Image:
    sheet = Image.new(
        "RGBA",
        (FRAME_W * SHEET_COLS, FRAME_H * SHEET_ROWS),
        (0, 0, 0, 0))
    for state in range(4):
        for facing in range(4):
            row = state * 4 + facing
            for frame in range(4):
                sheet.alpha_composite(frame_function(facing, state, frame),
                                      (frame * FRAME_W, row * FRAME_H))
    return sheet


def save_actor(name, frame_function):
    path = os.path.join(ACTOR_ROOT, name + "_sheet.png")
    make_sheet(frame_function).save(path)
    print(path)


def make_shadow():
    image = blank((16, 8))
    draw = ImageDraw.Draw(image)
    color = (17, 24, 20, 255)
    draw.rectangle((4, 4, 11, 5), fill=color)
    draw.rectangle((6, 3, 9, 6), fill=color)
    draw.point((3, 4), fill=color)
    draw.point((12, 4), fill=color)
    image.save(os.path.join(ACTOR_ROOT, "actor_shadow.png"))


def periodic_blob(draw, cx, cy, radii, fill, size):
    rng = random.Random(cx * 9176 + cy * 131 + radii * 17)
    points = []
    for i in range(12):
        angle = math.tau * i / 12
        radius = radii * rng.uniform(0.72, 1.18)
        points.append((cx + math.cos(angle) * radius,
                       cy + math.sin(angle) * radius))
    for ox in (-size, 0, size):
        for oy in (-size, 0, size):
            draw.polygon([(int(x + ox), int(y + oy)) for x, y in points], fill=fill)


def make_grass_macro(size=128):
    rng = random.Random(1701)
    base = rgba((59, 98, 61))
    image = Image.new("RGBA", (size, size), base)
    draw = ImageDraw.Draw(image)
    for _ in range(18):
        cx, cy = rng.randrange(size), rng.randrange(size)
        radius = rng.randrange(8, 21)
        color = rng.choice([
            rgba((53, 89, 55)), rgba((67, 108, 64)), rgba((73, 112, 67)),
            rgba((63, 100, 54))])
        periodic_blob(draw, cx, cy, radius, color, size)
    # Long-form field accents rather than one repeated mark per tile.
    for _ in range(95):
        x, y = rng.randrange(size), rng.randrange(size)
        color = rng.choice([rgba((82, 124, 71)), rgba((48, 84, 51)), rgba((97, 126, 69))])
        length = rng.choice((1, 2, 3))
        draw.line((x, y, x + rng.choice((-1, 0, 1)), y - length), fill=color)
    image.save(os.path.join(ENV_ROOT, "grass_macro8.png"))


def make_tepui_macro(size=128):
    rng = random.Random(1080)
    base = rgba((130, 101, 100))
    image = Image.new("RGBA", (size, size), base)
    draw = ImageDraw.Draw(image)
    for _ in range(14):
        cx, cy = rng.randrange(size), rng.randrange(size)
        radius = rng.randrange(8, 24)
        color = rng.choice([
            rgba((116, 90, 91)), rgba((145, 111, 107)), rgba((157, 121, 112)),
            rgba((122, 96, 91))])
        periodic_blob(draw, cx, cy, radius, color, size)
    # Fossil grain bands cross cell boundaries and repeat only at macro scale.
    for band in range(8):
        color = rgba((102, 81, 83)) if band % 2 == 0 else rgba((174, 137, 125))
        base_y = 8 + band * 16
        points = []
        for x in range(size):
            y = int((base_y + 2.2 * math.sin(math.tau * x / 64 + band)) % size)
            points.append((x, y))
        draw.line(points, fill=color)
    for _ in range(38):
        x, y = rng.randrange(size), rng.randrange(size)
        draw.point((x, y), fill=rgba((189, 151, 133)))
    image.save(os.path.join(ENV_ROOT, "tepui_macro8.png"))


def make_tree(seed):
    rng = random.Random(seed)
    image = blank((16, 16))
    draw = ImageDraw.Draw(image)
    trunk_dark = rgba((65, 49, 34))
    trunk = rgba((100, 72, 43))
    leaf_dark = rgba((38, 67, 39))
    leaf = rgba((67, 105, 52))
    leaf_light = rgba((105, 137, 66))

    lean = rng.choice((-1, 0, 0, 1))
    draw.polygon([(7, 8), (9, 8), (10 + lean, 14), (6 + lean, 14)], fill=trunk)
    draw.rectangle((6, 11, 7, 14), fill=trunk_dark)
    draw.point((9 + lean, 10), fill=rgba((137, 94, 50)))

    blobs = [(5, 5, 4), (10, 5, 4), (7, 3, 4), (7, 7, 5)]
    for i, (cx, cy, radius) in enumerate(blobs):
        cx += rng.choice((-1, 0, 1))
        cy += rng.choice((-1, 0, 0, 1))
        color = leaf_dark if i == 0 else leaf
        draw.ellipse((cx - radius, cy - radius // 2,
                      cx + radius, cy + radius // 2 + 1), fill=color)
    for _ in range(9):
        x, y = rng.randrange(3, 13), rng.randrange(1, 9)
        if image.getpixel((x, y))[3] != 0:
            draw.point((x, y), fill=leaf_light)
    return add_outline(image)


def make_trees():
    for index in range(4):
        name = "tree.png" if index == 0 else f"tree_v{index}.png"
        make_tree(401 + index).save(os.path.join(ENV_ROOT, name))


def main():
    os.makedirs(ACTOR_ROOT, exist_ok=True)
    os.makedirs(ENV_ROOT, exist_ok=True)

    save_actor("player", lambda f, s, i: humanoid_frame(PALETTES["player"], f, s, i, "player"))
    save_actor("villager", lambda f, s, i: humanoid_frame(PALETTES["villager"], f, s, i, "villager"))
    save_actor("merchant", lambda f, s, i: humanoid_frame(PALETTES["merchant"], f, s, i, "merchant"))
    save_actor("elder", lambda f, s, i: humanoid_frame(PALETTES["elder"], f, s, i, "elder"))
    save_actor("warden", lambda f, s, i: humanoid_frame(PALETTES["warden"], f, s, i, "warden"))
    save_actor("child", lambda f, s, i: humanoid_frame(PALETTES["child"], f, s, i, "child", child=True))
    save_actor("snapjaw", snapjaw_frame)
    save_actor("sari_snake", lambda f, s, i: snake_frame(f, s, i, False))
    save_actor("wardline", lambda f, s, i: snake_frame(f, s, i, True))
    save_actor("cascade_father", lambda f, s, i: frog_frame(f, s, i, False))
    save_actor("glasspane_frog", lambda f, s, i: frog_frame(f, s, i, True))
    save_actor("yellowfoot_wayfarer", tortoise_frame)
    save_actor("recension_echo", lambda f, s, i: humanoid_frame(PALETTES["recension"], f, s, i, "recension"))

    make_shadow()
    make_grass_macro()
    make_tepui_macro()
    make_trees()


if __name__ == "__main__":
    main()
