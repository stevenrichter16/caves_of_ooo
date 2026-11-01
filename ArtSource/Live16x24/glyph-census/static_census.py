"""Static glyph census (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md, Prompt 1's static half).

Counts the blueprints of each kind that no flat catalog names, where "named" means a quoted
blueprint name in any FlatPixel*.cs, or a row in the generated item or prop index. The
runtime bench (GlyphCensusBenchTests) confirms these against generated zones.
Run `python3 static_census.py [--list]` here.
"""
import glob, json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "item-icons"))
import make_item_icons as items

ROOT = items.ROOT
RENDERING = os.path.join(ROOT, "Assets", "Scripts", "Presentation", "Rendering")
DATA = os.path.join(ROOT, "Assets", "Resources", "Content", "Data")

def named():
    out = set()
    for f in glob.glob(os.path.join(RENDERING, "FlatPixel*.cs")): out |= set(re.findall(r'"([A-Z][A-Za-z0-9]+)"', open(f).read()))
    for index in ("ItemIcons.json", "PropIcons.json"):
        path = os.path.join(DATA, index)
        if os.path.exists(path): out |= {r["Blueprint"] for r in json.load(open(path))["Icons"]}
    return out

def kind(bp, name):
    tags, parts = items.merged(bp, name)
    if "Terrain" in tags or "Physics" not in parts and "Render" not in parts: return None
    if "Creature" in tags: return "npc" if "Conversation" in parts else "creature"
    if items.is_item(bp, name): return "item"
    return "prop"

def main():
    bp = {b["Name"]: b for b in json.load(open(items.BLUEPRINTS))["Objects"]}
    have = named()
    rows = {}
    for name in sorted(bp):
        if items.ABSTRACT.search(name) or name == "PhysicalObject": continue
        r = items.render_of(bp, name)
        if not r.get("RenderString") or len(r.get("ColorString", "")) != 2: continue
        k = kind(bp, name)
        if k: rows.setdefault(k, []).append((name, name in have, r["RenderString"] + r["ColorString"]))
    for k in ("creature", "npc", "item", "prop"):
        group = rows.get(k, [])
        missing = [n for n in group if not n[1]]
        print("%-8s %4d blueprints, %4d not named by any flat catalog" % (k, len(group), len(missing)))
        if "--list" in sys.argv:
            for n, _, g in missing: print("   ", n, g)

if __name__ == "__main__":
    main()
