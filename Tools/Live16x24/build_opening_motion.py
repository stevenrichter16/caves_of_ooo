"""Author compact live-opening motion derivatives as native Blender geometry.

Run Blender --background --python Tools/Live16x24/build_opening_motion.py.
Only ArtSource/Live16x24/motion is written. The accepted base/cast generators
are imported read-only for their original named cell geometry, not PNG data.
No raster is sampled, resized, cropped, traced or edited. Original idle poses
stay in their existing atlases; these are sixteen genuinely changed poses.
"""

import copy
import hashlib
import json
from pathlib import Path
import sys

import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_opening_base as base
import build_opening_cast as cast
from build_opening_base import add_label, add_quad, camera_for, emission_material

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/motion"
WIDTH, HEIGHT = 16, 24
PALETTE = dict(base.PALETTE)
for key, value in cast.PALETTE.items():
    assert key not in PALETTE or PALETTE[key] == value
    PALETTE[key] = value
POSES = ("walk-a", "walk-b", "melee", "hurt")
ACTORS = ("Player", "PetDog", "MarlbackScrabbler", "MarlbackGleaner")


class Frame:
    def __init__(self, idle, actor, pose, description):
        self.name = actor + "-" + pose
        self.actor, self.pose, self.description = actor, pose, description
        self.cells = copy.deepcopy(idle.cells)
        self.idle = idle

    def rect(self, part, color, x, y, width, height):
        assert color in PALETTE
        assert width > 0 and height > 0
        assert 0 <= x < x + width <= WIDTH and 0 <= y < y + height <= HEIGHT
        for row in range(y, y + height):
            for col in range(x, x + width):
                self.cells[row][col] = (part, color)
        return self

    def dot(self, part, color, x, y):
        return self.rect(part, color, x, y, 1, 1)

    def remove(self, predicate):
        for row in self.cells:
            for x, value in enumerate(row):
                if value and predicate(value[0]):
                    row[x] = None
        return self

    def move_parts(self, predicate, dx, dy):
        """Move selected original mesh cells, never an image or whole pose."""
        selected = [(x, y, value) for y, row in enumerate(self.cells)
                    for x, value in enumerate(row) if value and predicate(value[0])]
        self.remove(predicate)
        for x, y, value in selected:
            assert 0 <= x + dx < WIDTH and 0 <= y + dy < HEIGHT
            self.cells[y + dy][x + dx] = value
        return self


def player_frames(idle):
    frames = []
    limbs = lambda part: any(word in part for word in ("sleeve", "cuff", "hand", "leg", "boot"))
    head = lambda part: "hood" in part or part.startswith("face")
    p = Frame(idle, "Player", "walk-a", "Left arm bends forward as the left boot reaches out; opposite arm trails. Hood and coat identity stay fixed.")
    p.remove(limbs)
    p.rect("left-upper-sleeve", "gold", 3, 11, 2, 4)
    p.rect("left-bent-sleeve", "gold", 2, 14, 2, 2)
    p.rect("left-cuff", "leather", 1, 14, 1, 2)
    p.rect("left-hand", "cream", 1, 13, 1, 1)
    p.rect("right-sleeve", "gold", 12, 13, 1, 5)
    p.rect("right-cuff", "leather", 13, 17, 1, 2)
    p.dot("right-hand", "cream", 13, 19)
    p.rect("left-leg", "leather", 5, 20, 2, 1).rect("left-shin", "leather", 4, 21, 2, 1)
    p.rect("left-boot", "leather", 3, 22, 3, 1)
    p.rect("right-leg", "leather", 10, 20, 1, 2).rect("right-boot", "leather", 10, 22, 2, 1)
    frames.append(p)

    p = Frame(idle, "Player", "walk-b", "Right arm curls forward while the left arm extends behind; narrower left step and wide right step reverse the stride.")
    p.remove(limbs)
    p.rect("left-sleeve", "gold", 3, 12, 1, 5)
    p.rect("left-cuff", "leather", 2, 16, 1, 3).dot("left-hand", "cream", 2, 19)
    p.rect("right-upper-sleeve", "gold", 12, 11, 1, 4)
    p.rect("right-bent-sleeve", "gold", 12, 14, 2, 2)
    p.rect("right-cuff", "leather", 14, 13, 1, 2).dot("right-hand", "cream", 14, 12)
    p.rect("left-leg", "leather", 6, 20, 1, 2).rect("left-boot", "leather", 6, 22, 2, 1)
    p.rect("right-leg", "leather", 10, 20, 2, 1).rect("right-shin", "leather", 11, 21, 2, 1)
    p.rect("right-boot", "leather", 12, 22, 3, 1)
    frames.append(p)

    p = Frame(idle, "Player", "melee", "Committed short rightward reach, forward hood and braced split legs; no permanent weapon or trail.")
    p.remove(limbs)
    p.move_parts(head, 1, 1)
    p.move_parts(lambda part: part in ("collar", "coat", "front-linen", "left-coat-fold"), 1, 0)
    # Moving the visible front cloth would otherwise reveal holes where it
    # previously occluded the hem. Restore the underlying coat, not a new item.
    p.rect("coat-hem", "ochre", 4, 17, 9, 3)
    p.rect("left-coat-fold", "ochre_dark", 6, 17, 1, 1)
    p.rect("front-linen", "cream", 8, 17, 3, 3)
    p.rect("left-upper-sleeve", "gold", 4, 11, 2, 3)
    p.rect("left-rear-sleeve", "gold", 3, 14, 2, 2).rect("left-cuff", "leather", 2, 16, 2, 1)
    p.dot("left-hand", "cream", 2, 17)
    p.rect("right-reach", "gold", 12, 11, 2, 2)
    p.rect("right-cuff", "leather", 14, 11, 1, 2).rect("right-hand", "cream", 15, 11, 1, 2)
    p.rect("left-leg", "leather", 5, 20, 2, 1).rect("left-shin", "leather", 4, 21, 2, 1)
    p.rect("left-boot", "leather", 3, 22, 3, 1)
    p.rect("right-leg", "leather", 11, 20, 2, 2).rect("right-boot", "leather", 11, 22, 3, 1)
    frames.append(p)

    p = Frame(idle, "Player", "hurt", "Hood pulls back and down, shoulders compress, one hand rises and the other folds inward; boots still meet the same baseline.")
    p.remove(limbs)
    p.move_parts(head, -1, 1)
    p.move_parts(lambda part: part in ("collar", "coat", "front-linen", "left-coat-fold"), -1, 0)
    p.rect("coat-hem", "ochre", 4, 17, 8, 3)
    p.rect("left-coat-fold", "ochre_dark", 4, 17, 1, 1)
    p.rect("front-linen", "cream", 6, 17, 3, 3)
    p.rect("left-upper-sleeve", "gold", 2, 12, 2, 3)
    p.rect("left-bent-sleeve", "gold", 3, 15, 3, 2).dot("left-cuff", "leather", 6, 15)
    p.dot("left-hand", "cream", 7, 15)
    p.rect("right-sleeve", "gold", 11, 12, 1, 4)
    p.rect("right-bent-sleeve", "gold", 12, 11, 1, 3)
    p.rect("right-cuff", "leather", 12, 9, 1, 2).dot("right-hand", "cream", 12, 8)
    p.rect("left-leg", "leather", 5, 20, 2, 1).rect("left-shin", "leather", 6, 21, 2, 1)
    p.rect("left-boot", "leather", 6, 22, 3, 1)
    p.rect("right-leg", "leather", 10, 20, 2, 1).rect("right-shin", "leather", 11, 21, 2, 1)
    p.rect("right-boot", "leather", 12, 22, 3, 1)
    frames.append(p)
    return frames


def dog_frames(idle):
    frames = []
    limbs = lambda part: "leg" in part or "foot" in part
    head = lambda part: part in ("head", "forehead", "ear", "ear-tip", "short-muzzle", "nose", "eye")
    p = Frame(idle, "PetDog", "walk-a", "Diagonal trot: near hind leg reaches backward, near front leg reaches forward; far pair bends underneath the belly.")
    p.remove(limbs)
    p.rect("near-hind-leg", "tawny", 2, 20, 1, 2).rect("near-hind-foot", "fur_dark", 1, 22, 2, 1)
    p.dot("far-hind-leg", "fur_dark", 5, 20).dot("far-hind-knee", "fur_dark", 6, 21)
    p.rect("far-hind-foot", "fur_dark", 6, 22, 2, 1)
    p.dot("far-front-leg", "fur_dark", 9, 20).dot("far-front-foot", "fur_dark", 8, 21)
    p.dot("near-front-leg", "tawny", 11, 21).rect("near-front-foot", "fur_dark", 12, 22, 2, 1)
    frames.append(p)

    p = Frame(idle, "PetDog", "walk-b", "Opposite trot phase: near hind leg folds forward, near foreleg folds back, and the far foreleg plants out ahead.")
    p.remove(limbs)
    p.dot("near-hind-leg", "tawny", 3, 20).dot("near-hind-knee", "tawny", 4, 21)
    p.rect("near-hind-foot", "fur_dark", 4, 22, 2, 1)
    p.dot("far-hind-leg", "fur_dark", 2, 20).rect("far-hind-foot", "fur_dark", 1, 21, 2, 1)
    p.dot("near-front-leg", "tawny", 10, 21).rect("near-front-foot", "fur_dark", 9, 22, 2, 1)
    p.dot("far-front-leg", "fur_dark", 12, 20).dot("far-front-knee", "fur_dark", 13, 21)
    p.rect("far-front-foot", "fur_dark", 13, 22, 2, 1)
    frames.append(p)

    p = Frame(idle, "PetDog", "melee", "Head and neck stretch forward into a small open-jaw bite; rear leg pushes, foreleg braces, low body and floppy ear remain recognizably canine.")
    p.remove(limbs).remove(lambda part: part.startswith("tail") or part == "raised-tail")
    p.move_parts(head, 1, 1)
    p.remove(lambda part: part == "short-muzzle")
    p.rect("neck", "tawny", 11, 18, 2, 2)
    p.rect("upper-muzzle", "cream", 13, 17, 2, 1)
    p.rect("lower-muzzle", "cream", 13, 19, 2, 1)
    p.rect("tail-base", "tawny", 1, 18, 3, 1).dot("tail-tip", "cream", 0, 17)
    p.rect("rear-push-leg", "tawny", 2, 20, 1, 2).rect("rear-push-foot", "fur_dark", 1, 22, 2, 1)
    p.rect("far-rear-leg", "fur_dark", 5, 20, 1, 3)
    p.rect("front-braced-leg", "tawny", 11, 20, 1, 2).rect("front-braced-foot", "fur_dark", 12, 22, 3, 1)
    p.dot("far-front-leg", "fur_dark", 9, 20).rect("far-front-foot", "fur_dark", 8, 21, 2, 1)
    frames.append(p)

    p = Frame(idle, "PetDog", "hurt", "Head recoils up and back, tail drops, one forepaw curls under the chest; no hit icon, blood or palette flash.")
    p.remove(limbs).remove(lambda part: part.startswith("tail") or part == "raised-tail")
    p.move_parts(head, -1, -1)
    p.rect("tail-base", "tawny", 1, 18, 2, 1).rect("lowered-tail", "tawny", 1, 19, 1, 2)
    p.dot("tail-tip", "cream", 2, 21)
    p.rect("near-hind-leg", "tawny", 3, 20, 1, 2).rect("near-hind-foot", "fur_dark", 3, 22, 2, 1)
    p.rect("far-hind-leg", "fur_dark", 6, 20, 1, 3)
    p.dot("raised-front-leg", "tawny", 12, 20).dot("raised-front-foot", "fur_dark", 13, 20)
    p.rect("planted-front-leg", "tawny", 10, 20, 1, 2).rect("planted-front-foot", "fur_dark", 10, 22, 2, 1)
    frames.append(p)
    return frames


def scrabbler_frames(idle):
    frames = []
    limbs = lambda part: any(word in part for word in ("hind", "forearm", "wrist", "claw"))
    p = Frame(idle, "MarlbackScrabbler", "walk-a", "Wide rear plant and near claw stepping backward, while the farther claw reaches ahead; shale dome stays in place.")
    p.remove(limbs)
    p.rect("hind-hip", "tawny", 2, 18, 3, 2).rect("hind-shin", "tawny", 2, 20, 2, 2)
    p.rect("hind-foot", "tawny", 1, 22, 3, 1).dot("hind-toe", "claw", 1, 22)
    p.rect("near-forearm", "tawny", 8, 19, 2, 1).rect("near-wrist", "tawny", 7, 20, 2, 1)
    p.rect("near-claw", "claw", 6, 21, 2, 1).dot("near-claw", "claw", 5, 22)
    p.dot("far-forearm", "tawny", 13, 19).rect("far-wrist", "tawny", 14, 20, 1, 2)
    p.rect("far-claw", "claw", 13, 22, 2, 1)
    frames.append(p)

    p = Frame(idle, "MarlbackScrabbler", "walk-b", "Rear foot comes beneath the dome; near claw plants forward while the farther wrist folds back into a visible gap.")
    p.remove(limbs)
    p.rect("hind-hip", "tawny", 3, 18, 2, 2).rect("hind-shin", "tawny", 4, 20, 2, 2)
    p.rect("hind-foot", "tawny", 5, 22, 3, 1).dot("hind-toe", "claw", 5, 22)
    p.rect("near-forearm", "tawny", 8, 19, 2, 1).rect("near-wrist", "tawny", 10, 20, 1, 2)
    p.rect("near-claw", "claw", 11, 22, 2, 1)
    p.dot("far-forearm", "tawny", 13, 19).dot("far-wrist", "tawny", 12, 20)
    p.dot("far-claw", "claw", 12, 21)
    frames.append(p)

    p = Frame(idle, "MarlbackScrabbler", "melee", "Low lateral grasp with one long leading foreclaw and rear foot spread for purchase; short blunt head stays below the same shale dome.")
    p.remove(limbs)
    p.rect("hind-hip", "tawny", 2, 18, 3, 2).rect("hind-shin", "tawny", 1, 20, 3, 2)
    p.rect("hind-foot", "tawny", 0, 22, 4, 1).dot("hind-toe", "claw", 0, 22)
    p.rect("near-forearm", "tawny", 8, 19, 3, 2).rect("near-wrist", "tawny", 11, 19, 3, 1)
    p.rect("near-claw-upper", "claw", 14, 18, 2, 1)
    p.rect("near-claw-lower", "claw", 14, 20, 2, 1)
    p.rect("far-forearm", "tawny", 10, 21, 2, 1).rect("far-claw", "claw", 11, 22, 2, 1)
    frames.append(p)

    p = Frame(idle, "MarlbackScrabbler", "hurt", "Both wrists retract beneath the face and the dome lowers one pixel over a close braced stance; plate shape and sparse face stay unchanged.")
    p.remove(limbs)
    p.move_parts(lambda part: "plate" in part or "shell" in part, 0, 1)
    p.rect("hind-hip", "tawny", 3, 19, 2, 2).rect("hind-shin", "tawny", 4, 21, 1, 1)
    p.rect("hind-foot", "tawny", 3, 22, 3, 1).dot("hind-toe", "claw", 3, 22)
    p.rect("near-forearm", "tawny", 8, 20, 2, 1).rect("near-wrist", "tawny", 9, 19, 2, 1)
    p.dot("near-claw", "claw", 10, 18).dot("near-claw", "claw", 11, 19)
    p.rect("far-forearm", "tawny", 12, 20, 1, 2).rect("far-claw", "claw", 11, 22, 3, 1)
    frames.append(p)
    return frames


def gleaner_frames(idle):
    frames = []
    limbs = lambda part: any(word in part for word in ("hind", "planted", "reaching"))
    p = Frame(idle, "MarlbackGleaner", "walk-a", "Long rearward hind stride, near claw drawn under the body and far arm stretched ahead; stepped ridge remains tall.")
    p.remove(limbs)
    p.rect("hind-thigh", "tawny", 3, 18, 2, 2).rect("hind-shin", "tawny", 2, 20, 2, 2)
    p.rect("hind-foot", "tawny", 1, 22, 3, 1).dot("hind-toe", "claw", 1, 22)
    p.rect("near-forearm", "tawny", 8, 18, 2, 2).rect("near-wrist", "tawny", 7, 20, 2, 2)
    p.rect("near-claw", "claw", 6, 22, 3, 1)
    p.rect("reaching-shoulder", "rust", 10, 17, 2, 2).rect("reaching-arm", "tawny", 12, 18, 2, 1)
    p.rect("reaching-wrist", "tawny", 14, 19, 1, 2).rect("reaching-claw", "claw", 14, 21, 2, 1)
    p.dot("reaching-claw", "claw", 15, 22)
    frames.append(p)

    p = Frame(idle, "MarlbackGleaner", "walk-b", "Rear foot steps inward and the near foreclaw plants outward; elevated far wrist swings back, reversing the stride without shifting the shell.")
    p.remove(limbs)
    p.rect("hind-thigh", "tawny", 4, 18, 2, 2).rect("hind-shin", "tawny", 5, 20, 2, 2)
    p.dot("lower-plate-shadow", "stone_dark", 5, 18)
    p.rect("hind-foot", "tawny", 5, 22, 3, 1).dot("hind-toe", "claw", 5, 22)
    p.rect("near-forearm", "tawny", 8, 18, 2, 2).rect("near-wrist", "tawny", 10, 20, 2, 2)
    p.rect("near-claw", "claw", 11, 22, 3, 1)
    p.rect("reaching-shoulder", "rust", 10, 17, 2, 2).rect("reaching-arm", "tawny", 11, 18, 1, 2)
    p.rect("reaching-wrist", "tawny", 12, 19, 2, 1).dot("reaching-claw", "claw", 13, 18)
    frames.append(p)

    p = Frame(idle, "MarlbackGleaner", "melee", "Tall leading claw rises beside the blunt face, open pincer reaches forward, and the other foreleg braces; no weapon or extra limb.")
    p.remove(limbs)
    p.rect("hind-thigh", "tawny", 3, 18, 2, 2).rect("hind-shin", "tawny", 2, 20, 2, 2)
    p.rect("hind-foot", "tawny", 1, 22, 3, 1).dot("hind-toe", "claw", 1, 22)
    p.rect("planted-forearm", "tawny", 8, 18, 2, 3).rect("planted-wrist", "tawny", 9, 21, 2, 1)
    p.rect("planted-claw", "claw", 9, 22, 3, 1)
    p.rect("reaching-shoulder", "rust", 10, 17, 2, 2).rect("reaching-arm", "tawny", 12, 17, 2, 1)
    p.rect("reaching-wrist", "tawny", 14, 15, 1, 3)
    p.rect("reaching-claw-upper", "claw", 14, 14, 2, 1)
    p.rect("reaching-claw-lower", "claw", 14, 18, 2, 1)
    frames.append(p)

    p = Frame(idle, "MarlbackGleaner", "hurt", "Tall ridge leans back a pixel, one claw curls high near the body, and the supporting leg bends outward into a defensive recoil.")
    p.remove(limbs)
    p.move_parts(lambda part: part in ("upper-plate-shadow", "upper-plate", "upper-edge",
                                     "middle-plate-shadow", "middle-plate", "middle-edge"), -1, 1)
    # The original concealed seam must follow the plates; leaving its exposed
    # upper pixel behind would create a floating rust speck above the joint.
    p.cells[7][9] = None
    p.rect("hind-thigh", "tawny", 3, 18, 2, 2).rect("hind-shin", "tawny", 4, 20, 2, 2)
    p.rect("hind-foot", "tawny", 4, 22, 3, 1).dot("hind-toe", "claw", 4, 22)
    p.rect("planted-forearm", "tawny", 8, 18, 2, 2).rect("planted-wrist", "tawny", 10, 20, 2, 2)
    p.rect("planted-claw", "claw", 11, 22, 3, 1)
    p.rect("reaching-shoulder", "rust", 10, 17, 2, 2).rect("reaching-arm", "tawny", 12, 17, 1, 2)
    p.rect("reaching-wrist", "tawny", 11, 18, 1, 2).dot("reaching-claw", "claw", 10, 19)
    p.dot("reaching-claw", "claw", 11, 20)
    frames.append(p)
    return frames


def make_frames():
    player = base.make_frames()[0]
    cast_frames = cast.make_frames()
    frames = (player_frames(player) + dog_frames(cast_frames[1]) +
              scrabbler_frames(cast_frames[2]) + gleaner_frames(cast_frames[3]))
    assert [(p.actor, p.pose) for p in frames] == [(a, p) for a in ACTORS for p in POSES]
    return frames


def model(frame, materials, scene, index):
    collection = bpy.data.collections.new(f"{index:02d}-{frame.name}")
    scene.collection.children.link(collection)
    grouped = {}
    for row, cells in enumerate(frame.cells):
        x = 0
        while x < WIDTH:
            value = cells[x]
            if value is None:
                x += 1
                continue
            end = x + 1
            while end < WIDTH and cells[end] == value:
                end += 1
            vertices, faces = grouped.setdefault(value, ([], []))
            add_quad(vertices, faces, index * WIDTH + x, HEIGHT - row - 1,
                     index * WIDTH + end, HEIGHT - row)
            x = end
    for (part, color), (vertices, faces) in sorted(grouped.items()):
        mesh = bpy.data.meshes.new(frame.name + "/" + part)
        mesh.from_pydata(vertices, [], faces)
        mesh.materials.append(materials[color])
        mesh.update()
        obj = bpy.data.objects.new(part, mesh)
        obj["authoring"] = "Flat native pixel geometry derived from accepted named original cell geometry."
        obj["paletteHex"] = PALETTE[color]
        collection.objects.link(obj)
    return collection


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    atlas = bpy.context.scene
    atlas.name = "Native 256x24 compact motion atlas"
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    frames = make_frames()
    materials = {k: emission_material(k + " #" + v, v) for k, v in PALETTE.items()}
    collections = [model(f, materials, atlas, i) for i, f in enumerate(frames)]
    camera_for(atlas, 256, HEIGHT)
    atlas.render.filepath = str(OUT / "opening-motion.png")
    bpy.ops.render.render(write_still=True, scene=atlas.name)

    contact = bpy.data.scenes.new("Contact 8x motion geometry")
    camera_for(contact, 160, 168, scale=8)
    vertices, faces = [], []
    add_quad(vertices, faces, 0, 0, 160, 168, -1)
    mesh = bpy.data.meshes.new("Contact background")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(emission_material("Contact background #032F23", "032F23"))
    contact.collection.objects.link(bpy.data.objects.new("Contact background", mesh))
    for i, (frame, collection) in enumerate(zip(frames, collections)):
        col, row = i % 4, i // 4
        instance = bpy.data.objects.new(f"{i:02d} {frame.name}", None)
        instance.instance_type = "COLLECTION"
        instance.instance_collection = collection
        instance.location = (col * 40 + 12 - i * WIDTH, 136 - row * 40, 0)
        contact.collection.objects.link(instance)
        add_label(contact, f"{i:02d} {frame.actor}", col * 40 + 20, 131 - row * 40, materials["cream"], 1.5)
        add_label(contact, frame.pose, col * 40 + 20, 128 - row * 40, materials["linen"], 1.6)
    add_label(contact, "LIVE OPENING / 16 x 24 / COMPACT MOTION", 80, 164, materials["cream"], 2)
    add_label(contact, "8x geometry render / canonical poses / original idle reused / not runtime evidence", 80, 2, materials["linen"], 1.35)
    contact.render.filepath = str(OUT / "opening-motion-contact.png")
    bpy.ops.render.render(write_still=True, scene=contact.name)

    source = {
        "schema": 1, "coordinateSystem": "Top-left image origin, Y downward; front-facing XY mesh quads.",
        "palette": PALETTE,
        "frames": [{"index": i, "id": f.name, "actor": f.actor, "pose": f.pose,
                    "cells": f.cells, "idleCells": f.idle.cells} for i, f in enumerate(frames)],
    }
    (OUT / "source-cells.json").write_text(json.dumps(source, indent=2) + "\n")
    manifest = {
        "schema": 1, "artifact": "opening-motion.png", "width": 256, "height": 24,
        "frameWidth": 16, "frameHeight": 24, "frameCount": 16, "pixelsPerUnit": 16,
        "actorPivotNormalized": [0.5, 0], "filterMode": "Point", "alpha": "Binary, verified independently.",
        "frameOrder": "Four poses per actor: walk-a, walk-b, melee, hurt; Player, PetDog, MarlbackScrabbler, MarlbackGleaner.",
        "scope": "Canonical one-facing pose artwork only, not direction sheets or evidence of runtime integration. Idle reused from accepted base/cast atlases. No shadows, weapons, collision changes or baked hit markers.",
        "source": "opening-motion.blend", "generator": "Tools/Live16x24/build_opening_motion.py",
        "concept": "ArtSource/Live16x24/concepts/motion-parts-v1.png (pose reference, duplicate walks and added shading rejected)",
        "originalGeometry": ["Tools/Live16x24/build_opening_base.py", "Tools/Live16x24/build_opening_cast.py"],
        "sha256": hashlib.sha256((OUT / "opening-motion.png").read_bytes()).hexdigest(), "frames": [],
    }
    for i, f in enumerate(frames):
        occupied = [(x, y) for y, row in enumerate(f.cells) for x, value in enumerate(row) if value]
        colors = sorted({PALETTE[v[1]] for row in f.cells for v in row if v})
        assert max(y for x, y in occupied) == 22
        assert 3 <= len(colors) <= 7
        original = "base/opening-base.png" if f.actor == "Player" else "cast/opening-cast.png"
        idle_index = 0 if f.actor == "Player" else ACTORS.index(f.actor)
        manifest["frames"].append({
            "index": i, "id": f.name, "blueprint": f.actor, "pose": f.pose,
            "rect": [i * 16, 0, 16, 24], "footprintCells": [1, 1], "description": f.description,
            "idleSource": {"path": "ArtSource/Live16x24/" + original, "frame": idle_index},
            "occupiedPixels": len(occupied), "palette": colors,
            "alphaBoundsTopLeft": [min(x for x, y in occupied), min(y for x, y in occupied),
                                   max(x for x, y in occupied) + 1, max(y for x, y in occupied) + 1],
            "baselineTopLeft": 23,
        })
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "opening-motion.blend"))
    print("Wrote sixteen native motion poses, editable mesh geometry, 8x contact and source:", OUT)


if __name__ == "__main__":
    main()
