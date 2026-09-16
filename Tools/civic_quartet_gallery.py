#!/usr/bin/env python3
"""Make a local gallery from CivicQuartetCompositionPreviewBatch's native receipt.

Usage: python3 Tools/civic_quartet_gallery.py Docs/Verification/VoxelWorld/<run>
Only writes index.html; never changes generated geometry or images.
"""
import json
from pathlib import Path
import sys


def write_gallery(directory: Path) -> Path:
    rows = json.loads((directory / "preview-receipt.json").read_text())["rows"]
    for row in rows:
        image = Path(row["image"])
        if image.name != str(image) or not (directory / image).is_file():
            raise ValueError("Receipt image is missing or outside the gallery")
    if len(rows) != 12 or any(r["seed"] != r["actualWorldSeed"] or r["seed"] == 0 for r in rows):
        raise ValueError("Twelve reproducible native previews required")
    for area in {r["area"] for r in rows}:
        if len({r["formation"] for r in rows if r["area"] == area}) != 3:
            raise ValueError("Three distinct formations required per area")
    data = json.dumps(rows).replace("<", "\\u003c")
    page = """<!doctype html>
<html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Caves of Ooo · Gantry, Tine, Quillhold and Tally</title>
<style>
:root{color-scheme:dark;font:16px/1.5 system-ui;background:#171b18;color:#e8e6d7}
body{max-width:1650px;margin:auto;padding:28px}h1{font-size:28px;margin:8px 0}
p{max-width:900px;color:#bac0b3}header{margin-bottom:20px}nav{display:flex;flex-wrap:wrap;gap:10px;align-items:center}
button,select{font:inherit;background:#2b342d;color:inherit;border:1px solid #58634e;padding:8px 12px;border-radius:6px}
button[aria-pressed=true]{background:#526344;border-color:#b2c492}figure{margin:20px 0 0}
img{display:block;width:100%;background:#000;border:1px solid #41493f;border-radius:8px;image-rendering:auto}
figcaption{display:flex;justify-content:space-between;gap:15px;margin:12px 0;color:#bcc5b5;flex-wrap:wrap}
details{padding:10px 0}pre{white-space:pre-wrap;font:13px/1.6 ui-monospace,monospace;color:#b7c4a8}
.eyebrow{letter-spacing:.15em;text-transform:uppercase;font-size:12px;color:#b4c78d}
</style>
<header><div class="eyebrow">Caves of Ooo · Native voxel composition</div>
<h1>Gantry, Tine, Quillhold and Tally</h1><p>Four lore-based towns, each with three distinct formations generated through the real world manager.
Choose a town and seed to inspect the rules. These are static gameplay-camera captures; they do not measure live input feel or frame rate.</p></header>
<nav id="forms" aria-label="Town"></nav>
<nav style="margin-top:14px"><label for="seed">World seed</label><select id="seed"></select></nav>
<figure><img id="scene" alt=""><figcaption><strong id="place"></strong><span id="counts"></span></figcaption></figure>
<p id="description"></p><details><summary>Native generation receipt</summary><pre id="receipt"></pre></details>
<script>
const rows=__DATA__;
const descriptions={
Gantry:'A working crossroads with an open exchange, registry office, caravan rest and guest-cloth hospitality.',
Tine:'A sheltered lake edge with dry piers, boat-building yards, reeds, a scribe retreat and village services.',
Quillhold:'A public archive with connected shelf runs, copying rooms, communal dining and courier rest.',
Tally:'A central exchange with loading courts, grouped storage, stocked traders and rentable shop frontages.'};
const forms=[...new Set(rows.map(r=>r.area))];let selected='Gantry';
const readable=name=>name.replace(/([a-z])([A-Z])/g,'$1 $2');
const buttons=[];for(const form of forms){const r=rows.find(x=>x.area===form),b=document.createElement('button');
b.textContent=readable(r.area);b.title=form;b.onclick=()=>{selected=form;populateSeeds();show()};document.getElementById('forms').append(b);buttons.push([form,b])}
const seed=document.getElementById('seed');
function populateSeeds(){seed.replaceChildren();for(const r of rows.filter(x=>x.area===selected)){
const option=document.createElement('option');option.value=r.seed;option.textContent=readable(r.formation)+' · '+r.seed;seed.append(option)}}
seed.onchange=show;populateSeeds();
function show(){const r=rows.find(x=>x.area===selected&&x.seed===Number(seed.value));if(!r)return;
for(const [form,b] of buttons)b.setAttribute('aria-pressed',String(form===selected));
const img=document.getElementById('scene');img.src=r.image;img.alt=readable(r.area)+' — seed '+r.seed;
document.getElementById('place').textContent=readable(r.area)+' · '+readable(r.formation)+' · '+r.zoneId;
document.getElementById('counts').textContent=r.entities+' native owners · '+r.creatures+' creatures · '+r.missing+' missing meshes · '+r.unmodeled+' unmodeled owners';
document.getElementById('description').textContent=descriptions[r.area]||'';
document.getElementById('receipt').textContent=JSON.stringify(r,null,2)}show();
</script></html>""".replace("__DATA__", data)
    target = directory / "index.html"
    target.write_text(page)
    return target


if __name__ == "__main__":
    print(write_gallery(Path(sys.argv[1]).resolve()))
