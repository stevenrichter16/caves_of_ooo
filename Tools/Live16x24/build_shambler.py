"""One original Shambler idle cell, authored as editable integer-cell Blender quads.

Build: /Applications/Blender.app/Contents/MacOS/Blender --background --python
       Tools/Live16x24/build_shambler.py -- --build
Verify: python3 Tools/Live16x24/build_shambler.py --verify

Only this builder and its named new ArtSource outputs are owned. Build never
reads a raster as drawing input: every colored cell below is original geometry.
The verifier uses Pillow strictly to read/check the finished renders; it never
creates, edits, crops, resizes, composites or saves an image. Existing concept,
prompt, sprite, metadata and editable sources remain untouched.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/Live16x24/grovelands-actors"
WIDTH, HEIGHT, SCALE = 16, 24, 8
PALETTE = {
    "shadow": "3F3343",
    "deep": "665164",
    "mauve": "92768A",
    "dust": "B39AA9",
    "moss": "6B775F",
    "sage": "92A283",
    "eye": "282731",
}
OWNED = {"shambler.png", "shambler-contact.png", "shambler.blend",
         "source-cells.json", "manifest.json", "protected-inputs.json",
         "source-verification.json", "blend-geometry-verification.json"}
BACKGROUNDS = ("99887D", "1E4433", "10120E")
PLACEMENTS = ((8, 14), (40, 14), (72, 14))  # Contact top-left image coordinates.


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(name, value):
    assert name in OWNED
    (OUT / name).write_text(json.dumps(value, indent=2) + "\n")


def source_cells():
    cells = [[None] * WIDTH for _ in range(HEIGHT)]

    def rect(part, color, x, y, width=1, height=1):
        assert color in PALETTE
        assert 0 <= x < x + width <= WIDTH and 0 <= y < y + height <= HEIGHT
        for row in range(y, y + height):
            for col in range(x, x + width):
                cells[row][col] = [part, color]

    # A modest forward-drooping head; no cap, horn, luminous eyes or spore cloud.
    rect("small-drooped-head", "deep", 7, 5, 3, 1)
    rect("small-drooped-head", "deep", 6, 6, 5, 2)
    rect("hanging-jaw", "deep", 8, 8, 3, 1)
    rect("hanging-jaw", "deep", 9, 9, 1, 1)
    rect("moss-on-crown", "moss", 7, 5, 2, 1)
    rect("head-plane", "mauve", 8, 6, 3, 1)
    rect("head-plane", "mauve", 7, 7, 4, 1)
    rect("jaw-plane", "dust", 9, 8, 1, 1)
    rect("small-eye", "eye", 10, 7, 1, 1)
    rect("head-moss", "moss", 6, 6, 2, 1)
    # Crooked narrow flesh trunk, off-center shoulder and recessed belly.
    rect("slack-neck", "shadow", 6, 8, 2, 2)
    rect("crooked-trunk", "deep", 5, 10, 4, 3)
    rect("crooked-trunk", "deep", 5, 13, 3, 4)
    rect("crooked-trunk", "deep", 6, 16, 3, 1)
    rect("slack-shoulder", "mauve", 5, 10, 3, 1)
    rect("slack-shoulder", "mauve", 6, 11, 3, 1)
    rect("breast-plane", "mauve", 7, 12, 2, 2)
    rect("breast-plane", "dust", 8, 12, 1, 1)
    rect("slack-belly", "shadow", 5, 13, 1, 3)
    rect("hip-plane", "mauve", 6, 15, 2, 2)
    rect("shoulder-moss", "moss", 4, 10, 2, 1)
    rect("shoulder-moss", "sage", 5, 9, 1, 1)
    # Short hanging arm ends high, visibly distinct from the two planted legs.
    rect("short-arm", "deep", 4, 11, 2, 3)
    rect("short-arm", "mauve", 3, 13, 2, 3)
    rect("short-forearm", "deep", 3, 16, 1, 1)
    rect("short-hand", "mauve", 2, 17, 2, 1)
    rect("short-hand-light", "dust", 2, 17, 1, 1)
    rect("short-arm-moss", "moss", 3, 14, 1, 1)
    # One clearly attached tendril arm. It narrows from two to one cell,
    # hangs above the feet, and has no hand, hook, cane knob or pale terminal.
    rect("attached-tendril-root", "mauve", 9, 11, 1, 2)
    rect("attached-tendril-root", "deep", 9, 13, 2, 1)
    rect("tapering-tendril", "mauve", 10, 14, 1, 2)
    rect("tapering-tendril", "deep", 11, 15, 1, 3)
    rect("unlit-tendril-tip", "deep", 12, 17, 1, 3)
    # Unequal stance and separated legs; no skirt, third leg, shadow or plinth.
    rect("back-thigh", "deep", 4, 17, 2, 2)
    rect("back-shin", "deep", 3, 19, 2, 3)
    rect("back-leg-plane", "mauve", 4, 18, 1, 3)
    rect("back-leg-moss", "moss", 3, 20, 1, 1)
    rect("back-foot", "mauve", 3, 22, 3, 1)
    rect("back-toe", "dust", 3, 22, 1, 1)
    rect("front-thigh", "mauve", 7, 17, 2, 2)
    rect("front-knee", "deep", 8, 19, 1, 1)
    rect("front-shin", "mauve", 8, 20, 1, 2)
    rect("front-foot", "mauve", 7, 22, 3, 1)
    rect("front-toe", "dust", 9, 22, 1, 1)
    return cells


def guard():
    path = OUT / "protected-inputs.json"
    if path.exists():
        pins = json.loads(path.read_text())["sha256"]
    else:
        files = []
        for folder in (ROOT / "Assets/Resources/Sprites", ROOT / "ArtSource/Live16x24"):
            files.extend(p for p in folder.rglob("*") if p.is_file()
                         and (p.suffix in (".png", ".blend", ".json") or p.name.endswith(".png.meta"))
                         and not (p.parent == OUT and p.name in OWNED)
                         and p.name not in ("release-coverage.json", "world-sprite-coverage.json"))
        files.extend(p for p in OUT.iterdir() if p.is_file() and p.name not in OWNED)
        files.extend(ROOT / "Tools/Live16x24" / n for n in
                     ("build_opening_base.py", "build_opening_cast.py", "build_cindercaller.py", "build_north_plot.py", "build_maw_toad.py"))
        pins = {str(p.relative_to(ROOT)): sha(p) for p in sorted(set(files))}
        save("protected-inputs.json", {"scope": "Existing sprite PNG/meta, editable Live16x24 blends/JSON, concepts/prompts and imported helpers. Read-only protection, not gameplay acceptance.", "sha256": pins})
    for name, expected in pins.items():
        assert sha(ROOT / name) == expected, "Protected input changed: " + name
    return pins


def validate(cells, size, rgba):
    assert size == (WIDTH, HEIGHT), "Wrong native dimensions"
    assert len(cells) == HEIGHT and all(len(row) == WIDTH for row in cells)
    assert len(rgba) == WIDTH * HEIGHT * 4
    colors, occupied, tendril = set(), [], []
    for y, row in enumerate(cells):
        for x, value in enumerate(row):
            actual = tuple(rgba[4*(y*WIDTH+x):4*(y*WIDTH+x)+4])
            expected = (0, 0, 0, 0) if value is None else tuple(bytes.fromhex(PALETTE[value[1]])) + (255,)
            assert actual == expected, ("Native pixel differs", x, y, actual, expected)
            if value:
                colors.add(value[1]); occupied.append((x,y))
                if "tendril" in value[0]: tendril.append((x,y))
    assert len(colors) <= 8 and len(colors) == len(PALETTE)
    bounds=(min(x for x,y in occupied), min(y for x,y in occupied), max(x for x,y in occupied)+1, max(y for x,y in occupied)+1)
    assert bounds == (2,5,13,23), bounds
    assert max(y for x,y in occupied) == 22 and len(tendril) >= 8
    assert max(y for x,y in tendril) == 19, "Tendril must end above feet; no cane"
    assert cells[22][6] is None and cells[21][6] is None, "Leg separation stays open"
    assert all(value is None for value in cells[23]), "No shadow/baseline row below feet"
    # The head/body/arms/legs form one attached silhouette, not floating parts.
    remaining=set(occupied);pending=[remaining.pop()]
    while pending:
        x,y=pending.pop()
        for key in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if key in remaining:remaining.remove(key);pending.append(key)
    assert not remaining, "Detached pixel clusters"
    return {"occupied_pixels":len(occupied),"palette_colors":len(colors),"tendril_pixels":len(tendril),"bounds_top_left":list(bounds),"baseline_row_top_left":22,"single_connected_silhouette":True,"tendril_lowest_row":19}


def verify():
    from PIL import Image  # Read-only decode; no image creation/edit/save.
    pins = guard(); source = json.loads((OUT / "source-cells.json").read_text())
    assert source["palette"] == PALETTE and source["cells"] == source_cells()
    with Image.open(OUT / "shambler.png") as im:
        assert im.mode == "RGBA"; rgba = im.tobytes(); stats = validate(source["cells"], im.size, rgba)
    with Image.open(OUT / "shambler-contact.png") as im:
        assert im.mode == "RGBA" and im.size == (96*SCALE,48*SCALE)
        for index,(left,top) in enumerate(PLACEMENTS):
            bg = tuple(bytes.fromhex(BACKGROUNDS[index]))+(255,)
            for y,row in enumerate(source["cells"]):
                for x,value in enumerate(row):
                    expected = bg if value is None else tuple(bytes.fromhex(PALETTE[value[1]]))+(255,)
                    for dy in range(SCALE):
                        for dx in range(SCALE):
                            assert im.getpixel(((left+x)*SCALE+dx,(top+y)*SCALE+dy)) == expected, ("8x geometry mismatch",index,x,y)
    assert (OUT / "shambler.blend").stat().st_size > 10000
    counterchecks = []
    def reject(name, pixels=rgba, size=(16,24), cells=source["cells"]):
        try: validate(cells,size,pixels)
        except (AssertionError,KeyError,ValueError): counterchecks.append({"name":name,"rejected":True}); return
        raise AssertionError("Verifier accepted corruption: " + name)
    altered=bytearray(rgba); altered[3]=127; reject("partial alpha",bytes(altered))
    altered=bytearray(rgba); altered[4*(14*16+10)]=255; reject("wrong attached-tendril color",bytes(altered))
    reject("wrong canvas dimensions",size=(16,16))
    altered=bytearray(rgba); altered[4*(22*16+3):4*(22*16+3)+4]=b"\0\0\0\0"; reject("missing baseline foot",bytes(altered))
    wrong=copy.deepcopy(source["cells"]); wrong[14][10]=["tapering-tendril","dust"]; reject("source-cell color drift",cells=wrong)
    result={"passed":True,"native_dimensions":[16,24],"exact_native_rgba_matches_authored_cells":True,"native_pixels_checked":384,"direct_integer8x_contact_pixels_checked":3*384*64,"binary_alpha":True,"maximum_palette_colors":8,"stats":stats,"source_png_sha256":sha(OUT/"shambler.png"),"decoded_rgba_sha256":hashlib.sha256(rgba).hexdigest(),"source_cells_sha256":sha(OUT/"source-cells.json"),"editable_blend_sha256":sha(OUT/"shambler.blend"),"contact_sha256":sha(OUT/"shambler-contact.png"),"protected_inputs_unchanged":len(pins),"corruption_counterchecks":counterchecks,"image_mutations_by_verifier":False,"limits":"Source/render verification only. No Unity import, runtime native tests, actual after gameplay, package, Linux/Deck execution or biome/polish acceptance."}
    save("source-verification.json",result);print(json.dumps(result,indent=2))



def inspect_blend():
    import bpy
    source=json.loads((OUT/"source-cells.json").read_text())
    collection=bpy.data.collections.get("Shambler / original named pixel clusters")
    assert collection is not None
    recovered={};faces=0
    for obj in collection.objects:
        assert obj.type=="MESH" and obj.location.length==0
        color=obj["paletteHex"];assert color in PALETTE.values()
        for face in obj.data.polygons:
            assert len(face.vertices)==4
            coords=[obj.data.vertices[i].co for i in face.vertices]
            assert all(v.z==0 and v.x==int(v.x) and v.y==int(v.y) for v in coords)
            left=min(int(v.x) for v in coords);right=max(int(v.x) for v in coords)
            bottom=min(int(v.y) for v in coords);top=max(int(v.y) for v in coords)
            assert right-left==1 and top-bottom==1
            key=(left,23-bottom);assert key not in recovered
            part=obj.name.rsplit(".",1)[0] if obj.name.rsplit(".",1)[-1].isdigit() else obj.name
            recovered[key]=(part,color);faces+=1
    expected={(x,y):(v[0],PALETTE[v[1]]) for y,row in enumerate(source["cells"]) for x,v in enumerate(row) if v}
    assert recovered==expected
    native=bpy.data.scenes.get("Shambler exact native 16x24")
    assert native.render.resolution_x==16 and native.render.resolution_y==24
    assert native.render.resolution_percentage==100 and native.render.film_transparent
    result={"passed":True,"read_only_scene_inspection":True,"blend_saved_again":False,
            "editable_named_mesh_clusters":len(collection.objects),"integer_unit_quad_faces":faces,
            "every_mesh_cell_part_palette_matches_source_json":True,
            "native_scene_resolution":[16,24],"native_blend_sha256":sha(OUT/"shambler.blend"),
            "name_disambiguation":"Blender numeric object suffixes identify same named part with different palette groups; source part and palette both compared at every cell.",
            "scope":"Reopened editable Blender source geometry only; no gameplay acceptance."}
    save("blend-geometry-verification.json",result);print(json.dumps(result,indent=2))


def build():
    import bpy
    sys.path.insert(0,str(ROOT/"Tools/Live16x24"))
    from build_opening_base import camera_for, emission_material, add_label
    from build_cindercaller import model
    from build_north_plot import instance, plane
    OUT.mkdir(parents=True,exist_ok=True); pins=guard(); cells=source_cells()
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.object.select_all(action="SELECT");bpy.ops.object.delete(use_global=False)
    native=bpy.context.scene;native.name="Shambler exact native 16x24"
    col=model(native,"Shambler / original named pixel clusters",cells,PALETTE)
    camera_for(native,16,24);native.render.filepath=str(OUT/"shambler.png")
    bpy.ops.render.render(write_still=True,scene=native.name)
    contact=bpy.data.scenes.new("Shambler / three backing studies / direct 8x geometry")
    camera_for(contact,96,48,scale=SCALE)
    labels=emission_material("Contact label #D0C29F","D0C29F")
    for index,(bg,(x,top)) in enumerate(zip(BACKGROUNDS,PLACEMENTS)):
        plane(contact,"Backing study "+str(index),bg,index*32,0,32,48,-1)
        instance(contact,col,"Same native body "+str(index),x,48-top-24)
        add_label(contact,("STONE STUDY","GREEN STUDY","READER STUDY")[index],index*32+16,6,labels,1.2)
    add_label(contact,"SHAMBLER / ORIGINAL IDLE / 16 x 24",48,44,labels,1.7)
    add_label(contact,"SAME INTEGER GEOMETRY / 8x / NO ARTIFACT ACCEPTANCE",48,2,labels,1.0)
    contact.render.filepath=str(OUT/"shambler-contact.png")
    bpy.ops.render.render(write_still=True,scene=contact.name)
    save("source-cells.json",{"schema":1,"blueprint":"Shambler","id":"shambler-idle","coordinate_system":"Top-left,y downward. Named cells are front-facing integer mesh quads; null is transparent.","palette":PALETTE,"cells":cells,"state":"neutral living idle only; existing commitment/damage/status cues remain runtime-owned; no new attack pose, corpse or nuanced motion","authoring":"Original manual cluster reconstruction in code/Blender. Concept pixels are not sampled, traced, rescaled or used as render inputs."})
    save("manifest.json",{"schema":1,"artifact":"shambler.png","width":16,"height":24,"frame_count":1,"blueprint":"Shambler","state":"idle","sprite_rect":[0,0,16,24],"pivot_normalized":[0.5,0.5],"pixels_per_unit":16,"mesh_type":"FullRect","expected_import":"Point,Clamp,no mipmaps,uncompressed,binary alpha; root owns integration","source":"shambler.blend","source_cells":"source-cells.json","generator":"Tools/Live16x24/build_shambler.py","native_png_sha256":sha(OUT/"shambler.png"),"contact":"shambler-contact.png","contact_scale":8,"contact_backings":list(BACKGROUNDS),"contact_placements_top_left":list(PLACEMENTS),"palette":PALETTE,"bounds_top_left":[2,5,13,23],"baseline_row_top_left":22,"protected_input_count":len(pins),"concept_inputs":["shambler-scene-concept-v1.png","shambler-parts-reference-v1.png"],"critique":"Keep small drooped head, crooked narrow torso, unequal grounded feet and ONE attached tapering tendril. Reject cane-like hook, oversized shoulders, high-resolution shading, soft alpha, glow and changed scene geometry. Seven original flat mauve/gray-moss colors; no hat, held tool, spore cloud or shadow.","self_review":"Native shape/material study only; actual green/stone body and reader comparison required after real integration. Tendril attachment, leg separation and small tilted head must survive normal zoom and actual status/damage/commitment cues. Existing Mosshulk, ChoirTendril, SporeShambler and all prior art remain unchanged.","runtime_acceptance":False,"polish_acceptance":False})
    bpy.context.window.scene=native
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"shambler.blend"))
    guard();print("Wrote owned Shambler native source/render outputs only.")


if __name__ == "__main__":
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument("--build",action="store_true");parser.add_argument("--verify",action="store_true");parser.add_argument("--inspect-blend",action="store_true");args=parser.parse_args(argv)
    assert sum((args.build,args.verify,args.inspect_blend))==1, "Choose exactly one mode"
    if args.build:build()
    elif args.inspect_blend:inspect_blend()
    else:verify()
