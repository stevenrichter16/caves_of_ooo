"""Render the square inventory atlas without writing any world art.

Run with Python 3 and Pillow. Use --check to verify the committed PNG and index.
World frames are cropped and centered. Larger poses use nearest-neighbor fit.
The remaining portable tools and supplies share editable pixel templates below.
"""

import argparse
import io
import json
from pathlib import Path

from PIL import Image
import make_item_icons as world


ROOT = Path(world.ROOT)
RESOURCES = ROOT / "Assets/Resources"
SPRITES = RESOURCES / "Sprites"
ATLAS = SPRITES / "Live16x24/inventory-item-icons.png"
INDEX = RESOURCES / "Content/Data/InventoryItemIcons.json"
SIZE, COLUMNS = 16, 16
UNKNOWN = "unknown"


def strip(names, sheet, height=24):
    """Name the frames in an existing one-row atlas, left to right."""
    return {name: (sheet, index, height) for index, name in enumerate(names.split()) if name != "_"}


# These are the same neutral frames the existing specialty readers use. The
# original resources stay as inputs and are never replaced by this generator.
BORROW = {
    **strip("Emberwheat ToastedEmberwheat DrawgourdShell DrawgourdSeed Mushroom RoastedMushroom "
            "GlowQuartz HealingTonic DriedMeat FireClay SalvagedTimber MarlbackCorpse", "Live16x24/opening-items"),
    **strip("PaleSalt Bone SaltbriarSprig ChoirIron Honeycomb GroveRed Tepuibone KnotflaxCord", "Live16x24/world-harvest-products"),
    **strip("StoneburrSeed FrostLichen MendleafSprig WildBerries GoldCoin ConcentratedMendleaf BrewedTonic", "Live16x24/north-works-products"),
    **strip("CandyCarrot CandyCarrotSeed EmberwheatSeed", "Live16x24/plot-products"),
    **strip("_ _ _ _ _ _ _ _ _ Torch _ Waterskin", "Live16x24/opening-states"),
    "Dagger": ("Environment/item_dagger", 0, 16),
    "ShortSword": ("Environment/item_sword", 0, 16),
    "Mace": ("Environment/item_mace", 0, 16),
    "WateringGrimoire": ("Environment/item_book", 0, 16),
    "LeatherArmor": ("Live16x24/leather-armor", 0, 24),
    "Hatchet": ("Live16x24/hatchet", 0, 24),
    "Cudgel": ("Live16x24/cudgel", 0, 24),
    "Buckler": ("Live16x24/buckler", 0, 24),
    "LongSword": ("Live16x24/long-sword", 0, 24),
}

# Optional icons for the already visible storage selection. A locked chest,
# ordinary chest and mimic deliberately share the neutral closed body.
WORLD_STORAGE = {
    "Chest": ("Live16x24/opening-anchors", 6, 24),
    "LockedChest": ("Live16x24/opening-anchors", 6, 24),
    "MimicChest": ("Live16x24/opening-anchors", 6, 24),
    "Crate": ("Live16x24/opening-cellar", 1, 24),
    "StrongBox": ("Environment/cont_strongbox", 0, 16),
    "OreCache": ("Environment/cont_ore_cache", 0, 16),
    "HollowLog": ("Environment/cont_hollow_log", 0, 16),
    "Sack": ("Environment/cont_sack", 0, 16),
    "Urn": ("Environment/cont_urn", 0, 16),
}


def pixels(*rows):
    assert len(rows) <= SIZE and all(len(row) <= SIZE for row in rows), rows
    return world.L(list(rows))


TEMPLATES = {
    # The same warm outline and materials as the existing item families.
    "box": pixels("...oooooooooo", "..oTTTTTTTToo", ".ooTtttTttooTo", ".oTTTTTTTToTto",
                  ".otttosottoTto", ".oTttosottoTto", ".otTttttttooTo", ".oTtttttTtooo",
                  ".ooooooooooo"),
    "satchel": pixels("....okkkko", "...oko..oko", "..oko....oko", "..ooooooooooo",
                      ".oTHAAAAAAAAo", ".oTTAAHAAAaao", ".oTTTTokkoaaaao", ".otAAAkSkaaaao",
                      ".otAAAokooooo", ".otAAAaaaaaaao", "..oaaaaaaaoo", "...oooooooo"),
    "sieve": pixels("..ooooooooooo", ".oTTTTTTTTTTTo", "oToeoSeoSeoSoTo", "oToSeoSeoSeooTo",
                    "oToeoSeoSeoSoTo", "oToooooooooooTo", ".ottttttttttto", "..ooooooooooo"),
    "pump": pixels("..oooooooooooo", "..oSTTTTSSSSSo", "..oooooooooooo", ".......oso", "......osso",
                   ".....osoSo", "....oooSSoooo", "...oTTsSSsttto", "...otTSsSsooTo", "...otTsSSoooo",
                   "...otTTSTto", "..ooTTTTTTtoo", "..oTTttttttto", "..ooooooooooo"),
    "condenser": pixels("....oooooo", "....oTTTTo", "...osAAAAso", "..osHHAAAaso", "..osAAAAaaso",
                        "...osaaaaaso", "....ooooTTosoo", ".....oSSo..ooso", "...ooSo..oSSoo",
                        "..oSo..ooSoo", "..oSoooSo", "...ooooo"),
    "bellows": pixels(".oSSoooTTTTTo", "..oooTTTTTo", "...otHAAAAo", "..otAAAAAAAo",
                      "..otAAAAaaaao", "...otaaaaaaaao", "...ooTTTTTTTTTo", ".....oooooooooo"),
    "sled": pixels("..............o", "...........okko", "........okko", ".....okko",
                   "..oooooo....oo", ".oTTTTTTTTTToTo", "oTttTttTttttoTo", "ooooooooooooTo",
                   "..oto....otoTo", "..oto....otooo", ".oooooooooooo", "oTTTTTTTTTTTTo",
                   ".ooooooooooo"),
    "fish_trap": pixels("......okkko", ".....ok...ko", "....oTToooTTo", "...oTAaAaAaTTo",
                          "..oTAaAaAaAaTTo", "..otooTTTTootto", "..oTAaAaAaAaTTo", "...oTTaAaAaTTo",
                          "....ooooooooo"),
    "tray": pixels("...ooooooooooo", "..oTTTTTTTTTTTo", ".oTeeeeeeeeeeeTo", ".oTeAaaAAAaAaeTo",
                    ".oTeeeeeeeeeeeTo", "..otTTTTTTTTtto", "...ottttttttto", "....ooooooooo"),
    "awning": pixels(".......oo", ".....ooAAoo", "...ooHAAAAaoo", ".ooHHAAAAAAaaoo",
                      "oHHAAAAAAAAAaao", "oooTTToooTToooo", "..oTo....oTo", "..oTo....oTo",
                      "..oTo....oTo", "..oTo....oTo", ".oTTo...oTTo", ".oooo...oooo"),
    "bench": pixels("..oooooooooo", ".oTTTTTTTTTToo", "oTTtTtTTtTtTtTo", "ooooooooooooooo",
                     "..oto......oto", "..oto......oto", "..oto......oto", ".oTTo.....oTTo"),
    "hearth": pixels(".......o", "......oYo", "....ooYWo", "...oWoWWoo", "...oWYYWWo",
                      "...oWWWWo", "..oTToooTTo", ".osSTTTTTSso", "oSSssTTssSSso", ".ossssssssso"),
    "still": pixels(".......ooo", "....oooosoo", "...osAAAAsoso", "..osHAAAAasoSo", "..osAAAAaasoSo",
                    "..osAAAaaso.so", "...osaaaaso..so", "....ooooo..osso", "...oSSSSSo.oSSo",
                    "...ooooooo.oooo"),
    "trough": pixels(".ooooooooooooo", "oTsssssssssssTo", "oTsHAAAAAAAasTo", "oTsAAAaAAAAasTo",
                      "oTsssssssssssTo", ".otTTtTtTtTtto", "..oTtttttttTo", "...ooooooooo"),
    "scraper": pixels("........oooo", ".......osSSo", "......osSSSo", ".....osSSSo", ".....osSoo",
                       "....otToo", "...otToo", "..otToo", ".otToo", ".oooo"),
    "probe": pixels("..........ooo", ".........osAo", "........ossso", ".......osSo", "......osSo",
                     ".....osSo", "....osSo", "...osSo", "..osSo", ".osSo", ".ooo"),
    "striker": pixels(".......oooooo", "......oSSSSSoo", ".....oSSooSoo", "....oSoo..oSo",
                       "...oSo....oSo", "...oSo...oSoo", "...oSoooSSo", "....oooooo"),
    "spring": pixels(".oooooooooooo", ".oTtTtTtTtTTo", ".otoSSooSSoso", ".otooeoeoeoto",
                      ".otoSSooSSoto", ".oTTtTttTtTTo", ".oooooooooooo"),
    "clay": pixels("...oooooooo", "..oHHAAAAAao", ".oHAAAAAAaao", ".oAAAAAaaooo", ".oAAAAaaoao",
                    ".oaaaaooaoao", "..ooooooooo"),
    "grains": pixels("........o", "....oo.oHo", "...oHooAao..oo", "...oAaaooo.oHAo",
                      "....ooo....oAao", ".oo......oo.oo", "oHAo....oHAo", ".ooo....oAao", ".........oo"),
    "chaff": pixels("...oo......oo", "..oTto....oTto", "...oTto..oTto", ".oo.oTtooTto",
                     ".oTtooTToTo.o", "..oTTokkoTToo", "...oTToookTo", "....oTtotTo", ".....ooooo"),
    "seed_heads": pixels("..oo.....ooo", ".oHAo...oHAAo", ".oHAao.ooAAao", "..oAAo.oAaaao",
                          "...oAaooAaoo", "....oAHAaao", "....oAAAAo", ".....oTTo", ".....okko",
                          "....oTtto", "....ooooo"),
    "meal": pixels(".....ooooooo", "...ooTTHAATToo", "..oTHAAAAAAaTTo", ".oTTAAaAAAAaaTTo",
                    ".osSTAaaaAAaSso", "..osssTTTTssso", "...oooooooooo"),
    "strip": pixels("....ooooooooo", "...oHAHAAAAAao", "..oAAAooooAAo", "..oAAoAAAoAo",
                     "..oAAoAAoAo", "..oAAAAAAaao", "...oooooooo"),
    "unknown": pixels("......ooo", "....ooHHHoo", "...oHAAAHAAo", "..oHAAAAAAaao", "..oAAHHAAAaao",
                       "..oAAAAAAaao", "..oAAAAAaaao", "...oaaaaaao", "....oooooo"),
}


FAMILIES = {
    "CampAwning": "awning", "CampBench": "bench", "CampBox": "box", "CampFishTrap": "fish_trap",
    "CampHearth": "hearth", "CampSettlingTray": "tray", "CampStill": "still", "CampTrough": "trough",
    "CargoSled": "sled", "FieldSatchel": "satchel", "HandBellows": "bellows", "HandPump": "pump",
    "HandSieve": "sieve", "SteamCondenser": "condenser", "FlintStriker": "striker", "SpringTrap": "spring",
    "Scraper": "scraper", "DischargeRod": "probe", "SoundingRod": "probe", "HearthNozzle": "pipe",
    "ReleasedExtractionTool": "blade_part", "RecoveredIronBlank": "blade_part", "LiningClayParcel": "parcel",
    "PreparedLiningClay": "clay", "DryClaySpoil": "pile", "QuarrySweepings": "grains", "CoarseGrit": "grains",
    "DryKnotflaxChaff": "chaff", "KnotflaxSeedHeads": "seed_heads", "FieldMeal": "meal", "RimeFlake": "crystals",
    "HushStrip": "strip", "CuredLeatherOffcut": "strip", "InertSludge": "pot", "MarlrootClod": "bulb",
    "PitchpodResin": "jar", "VenomGland": "sac", "AcidCrust": "crystals", "BrineCrust": "crystals",
    "VentScale": "crystals",
}

ACCENTS = {
    "CampAwning": "G", "CampFishTrap": "y", "CampStill": "c", "CampTrough": "C",
    "FieldSatchel": "g", "FieldMeal": "O", "PreparedLiningClay": "o", "CoarseGrit": "W",
    "KnotflaxSeedHeads": "G", "unknown": "C",
}


def eligible_blueprints(blueprints):
    # The content's Item-suffixed schema rows are templates. Takeable props
    # such as keys, cups and bodies can also appear as exact inventory rows.
    return {name for name in blueprints if not world.ABSTRACT.search(name) and world.is_item(blueprints, name)
            and "Creature" not in world.merged(blueprints, name)[0]}


def centered(frame):
    bounds = frame.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("an inventory frame cannot be empty")
    icon = frame.crop(bounds)
    if max(icon.size) > SIZE:
        scale = SIZE / max(icon.size)
        icon = icon.resize(tuple(max(1, round(length * scale)) for length in icon.size), Image.Resampling.NEAREST)
    frame = Image.new("RGBA", (SIZE, SIZE))
    frame.paste(icon, ((SIZE - icon.width) // 2, (SIZE - icon.height) // 2))
    return frame


def render():
    blueprints = {row["Name"]: row for row in json.loads(Path(world.BLUEPRINTS).read_text())["Objects"]}
    item_names = eligible_blueprints(blueprints)
    existing = json.loads(Path(world.INDEX).read_text())
    world_frames = {row["Blueprint"]: row["Frame"] for row in existing["Icons"]}
    named = sorted(item_names | (WORLD_STORAGE.keys() & blueprints.keys()))
    names = [UNKNOWN, *named]
    images = {}

    def asset(path):
        if path not in images:
            with Image.open(path) as source:
                images[path] = source.convert("RGBA")
        return images[path]

    def borrowed(path, index, height, columns):
        x, y = index % columns * SIZE, index // columns * height
        image = asset(path)
        if x + SIZE > image.width or y + height > image.height:
            raise ValueError(f"{path.name} does not have frame {index}")
        return image.crop((x, y, x + SIZE, y + height))

    def draw(name):
        source = BORROW.get(name) or WORLD_STORAGE.get(name)
        if source is not None:
            sheet, index, height = source
            path = SPRITES / f"{sheet}.png"
            return borrowed(path, index, height, asset(path).width // SIZE)
        if name in world_frames:
            return borrowed(Path(world.ATLAS), world_frames[name], existing["FrameHeight"], existing["Columns"])
        render_fields = world.render_of(blueprints, name)
        glyph = (render_fields.get("RenderString") or "?")[0]
        code = ACCENTS.get(name, (render_fields.get("ColorString") or "&y")[-1])
        family = FAMILIES.get(name, world.family(name, glyph)) if name != UNKNOWN else UNKNOWN
        rows = TEMPLATES[family] if family in TEMPLATES else world.T[family]
        frame = Image.new("RGBA", (SIZE, SIZE))
        world.paint(frame, rows, world.palette_for(code), 0, 0, h=SIZE)
        return frame

    rows = (len(names) + COLUMNS - 1) // COLUMNS
    atlas = Image.new("RGBA", (COLUMNS * SIZE, rows * SIZE))
    icons = []
    for index, name in enumerate(names):
        frame = centered(draw(name))
        atlas.paste(frame, (index % COLUMNS * SIZE, index // COLUMNS * SIZE))
        icons.append({"Blueprint": name, "Frame": index})
    png = io.BytesIO()
    atlas.save(png, format="PNG")
    data = {"Columns": COLUMNS, "FrameHeight": SIZE, "Icons": icons}
    return png.getvalue(), (json.dumps(data, indent=1) + "\n").encode(), len(item_names), len(icons)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="check outputs without writing them")
    args = parser.parse_args()
    png, index, items, icons = render()
    for path, contents in ((ATLAS, png), (INDEX, index)):
        if args.check:
            if path.read_bytes() != contents:
                raise SystemExit(f"{path.name} differs from the generated result")
        else:
            path.write_bytes(contents)
    print(f"{items} portable item blueprints, {icons - items - 1} storage owners, and one fallback")


if __name__ == "__main__":
    main()
