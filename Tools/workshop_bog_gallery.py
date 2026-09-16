#!/usr/bin/env python3
"""Make a local gallery from WorkshopBogCompositionPreviewBatch's native receipt.

Usage: python3 Tools/workshop_bog_gallery.py Docs/Verification/VoxelWorld/<run>
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
    data = json.dumps(rows).replace("<", "\\u003c")
    page = """<!doctype html>
<html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Caves of Ooo · Cinderhold and Sumphold</title>
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
<h1>Cinderhold and Sumphold</h1><p>The Concord work town and raised boatyard, generated through the real world manager.
Choose a town and seed to inspect the rules. These are static gameplay-camera captures; they do not measure live input feel or frame rate.</p></header>
<nav id="forms" aria-label="Town"></nav>
<nav style="margin-top:14px"><label for="seed">World seed</label><select id="seed"></select></nav>
<figure><img id="scene" alt=""><figcaption><strong id="place"></strong><span id="counts"></span></figcaption></figure>
<p id="description"></p><details><summary>Native generation receipt</summary><pre id="receipt"></pre></details>
<script>
const rows=__DATA__;
const descriptions={
Cinderhold:'Four unequal stone buildings distinguish public notices, equipment work and domestic life. Forest shoulders border a packed-stone road. The factor, weaponsmith, forge, anvil and ordinary village services remain native owners.',
Sumphold:'Raised dry work fingers separate irregular water cuts. Hull trestles, peat faces, quiet records and dry board paths show the work of the town. Water contact, peat damage and board destruction use the existing native systems.'};
const forms=[...new Set(rows.map(r=>r.formation))],seeds=[...new Set(rows.map(r=>r.seed))];let selected='Sumphold';
const buttons=[];for(const form of forms){const r=rows.find(x=>x.formation===form),b=document.createElement('button');
b.textContent=r.area;b.title=form;b.onclick=()=>{selected=form;show()};document.getElementById('forms').append(b);buttons.push([form,b])}
const seed=document.getElementById('seed');for(const n of seeds){const option=document.createElement('option');option.value=n;option.textContent=n;seed.append(option)}seed.onchange=show;
function show(){const r=rows.find(x=>x.formation===selected&&x.seed===Number(seed.value));if(!r)return;
for(const [form,b] of buttons)b.setAttribute('aria-pressed',String(form===selected));
const img=document.getElementById('scene');img.src=r.image;img.alt=r.area+' — '+r.formation+' — seed '+r.seed;
document.getElementById('place').textContent=r.area+' · '+r.formation.replace(/([a-z])([A-Z])/g,'$1 $2')+' · '+r.zoneId;
document.getElementById('counts').textContent=r.entities+' native owners · '+r.creatures+' creatures · '+r.missing+' missing meshes · '+r.unmodeled+' unmodeled owners';
document.getElementById('description').textContent=descriptions[r.formation]||'';
document.getElementById('receipt').textContent=JSON.stringify(r,null,2)}show();
</script></html>""".replace("__DATA__", data)
    target = directory / "index.html"
    target.write_text(page)
    return target


if __name__ == "__main__":
    print(write_gallery(Path(sys.argv[1]).resolve()))
