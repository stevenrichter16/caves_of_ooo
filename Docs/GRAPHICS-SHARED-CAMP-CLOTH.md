# Shared camp cloth: next visible family

Status: bounded two-owner native and actual Editor coverage verified, with independent review. Source publication in this commit; batch packaging pending. No whole-area polish acceptance.

## Why next

BrineLens current disclosed camp has10 TentWall glyphs and1 GuestClothPole alongside the ordinary well and sack. That envelope is a larger visible gap than another isolated small prop. The same owners occur at FirstTent/Wellmeet/Gantry and DrownedLedger. Source patterns need no new spawns or density machinery; no cave work.

## Source corrections before implementation

| Owner | Actual source | Required boundaries |
|---|---|---|
| TentWall | Objects.json33942, Terrain, #/&y,layer2,solid,Cloth,8HP,thermal260/.8 | Preserve straight cell boundaries and actual walls. No roof across entrances, stone substitution or topology change. Damage/destruction and thermal remain mechanic-owned; use existing cues. |
| GuestClothPole | Objects.json33992,PhysicalObject, vertical-bar/&Y,layer2,passable,Furniture | Slender ochre post and small dark cloth. Description mentions three days but the owner does not implement a timer/service; no fake shop/interaction. |

The vertical bar in the source row means literal glyph `|`. No exact flat raster was found; FirstTentVoxelKitLibrary/WellmeetVoxelLibrary/SoddenNativeArtLibrary entries are3D, not usable provenance for a flat PNG. Re-read cited sources when implementing inherited/custom variants.

## Art extraction and scope

Actual baseline: `Docs/Verification/Live16x24/shared-camp-props-before-01-brine.png`, earned drifter136 seed65, normal GameBootstrap. Built-in imagegen result and exact prompt/hash/critique: `ArtSource/Live16x24/shared-camp-cloth/`. The concept changes footprints and some HUD text; neither is accepted. Carry only broad cloth folds, restrained charcoal/ochre contrast, tied posts and a small hanging cloth into original editable16×24 cells. Proposed minimum is two wall variants and one pole, no animation or connectivity framework unless actual comparison identifies a need. Keep floors quiet and existing mechanics/geometry untouched.

Root acquired exact current readers through ordinary Look at49,3 TentWall35204 (receipt capture shows wall8/8 Cloth) and54,6 GuestClothPole35213; retained `shared-camp-cloth-before-02-wall-reader.png` and `before-03-pole-reader.png`. The exact wall owner is35204 from current-paint receipt, not a spawned fixture. Native runtime RED/GREEN and integration remain pending. Preserve source-cells/palette and editable native project, immutable before PNG, current props, all normal saves. Follow actual after world/reader/whole-camp composition, damage/blocking/passable-pole and visibility checks. Coverage will still not equal full settlement polish. No source/drive cleanup.

## Root pre-implementation check — 03:38UTC

Read exact Objects TentWall/GuestClothPole/Terrain/PhysicalObject, DestructiblePart and ThermalPart ApplyHeat, plus actual Cloth8/8 and pole readers. No actor Hitpoints stat: structural HP is DestructiblePart. Partial positive HP keeps the wall body; Gone/HP0 rejects it. Cloth has no authored wreckage blueprint, so removal must show actual underlying floor, not fabricated rubble. Thermal changes remain live; existing Burning/Wet Render cues must not be overwritten by native white reset. Guest pole has no structural/thermal/service part. FrontierClothScreen inherits TentWall but adds ReclaimCloth: excluded from the first exact-role slice, not automatically admitted by similar name or glyph.

Minimum scope: 48×24 atlas with2original16×24 straight-boundary clothwall variants and1transparentpole. Broad folds/tiedwood, no roof/topology edits. Original editable source and palette/builder remain separate from the AI raster. No optional animation, new service or genericconnectivity work. Relevant regression and actual world/readers/break/passablepole/visibility checks precede acceptance. Current props source6c08899a012a04db329a77fdf92ba3768ec8e147 verified on origin/main; current packages still8c0c72e11.

Runtime RED jobe660c74a476242a5ac187d2da6391467:12/12fail on missingadapter setup, retained shared-camp-cloth-red.xml before production. Root implementation now adds narrowatlas/body adapter and four role-authoritative world/reader seams; nativeGREEN/actualintegration pending. Two staticwallframes chosen by(x+y)&1; poleframe2; survivingdamagedwall retainsbody with realRendercue.

## Iteration1 — real comparison, not acceptance

114/114 native tests passed (12cloth+102camp/storage/furniture), jobf86ba58217054bb7984051b49d940449,26.87s, retained XML. Source review by portable_equipment_final_review found noP1/P2; this reviewer authored the tests, but independently reviewed root runtime. Atlas48×24Point/mipcount1 loaded inUnity. Unchanged actual drifter136→after01 shows10 currentvisiblewalls and1pole all native; exact wall reader02 showsCloth8/8. Gameplay exposes **support dominance and rung-like folds**: golden posts/ties overpower cloth atnormalzoom. Not accepted from source/contact/tests alone.

Root+artistagree minimalv2: narrow/darken wall supports with existing palette, broaden cloth faces, short staggered sag shadows; preserve polepixels, straight opaque boundary, geometry/mechanics and3cellcontract. Save allv1sources reversibly. Newatlas only; runtimeunchanged, so114native evidence remains relevant. Re-runsourcepixel/reopen proof and actualsamecheckpoint comparison; no repeated broadnative suite withoutnewruntimechange. v1Editorprofile122.77s,58662samples,rendermean.0955ms,p99.2372ms,drawmean39.89max44; tool/UI/idle included, no speedup/Deckclaim. Ownedv1savebranchcopied+SHAverified beforeFinish.

## V2 actual verification — 03:58UTC

Original atlas SHA256 `3deb52ac4c4be95f6d5b738ca06176dfc5722a2ce564dfb9e11cfc00770ef6a8`; source/runtime bytes match. Root reran `python3 Tools/Live16x24/build_shared_camp_cloth.py --verify`:1,152 native pixels,324,480 contact pixels, six corruptions rejected, unchanged pole/palette. All v1 sources remain in `versions/v1/`. No runtime edit followed the114-case GREEN.

Actual GameBootstrap seed65 starts from earned drifter136. Captures03–11 and matching JSON receipts retain the following ordinary input sequence:

- Initial matched camp view03 and exact wall reader04, Cloth8/8. Two dark wall variants and small guest cloth replace glyphs; straight cell geometry is unchanged.
- From player48,3, interact east and choose the visible `a) break` action: wall35204 at49,3 changes8→1, remains solid/native (05). Repeat the actual action: wall removed, underlying Sand33076 visible and passable (06). Ordinary east input moves player into49,3 (07). No fabricated rubble or scripted damage.
- Walk50,3→50,5→54,5; exact guest-pole35213 reader08. Step onto54,6 (09): player2695 correctly owns the rendered cell, so that receipt's native=true describes the player body, not the covered pole. Step off54,7 (10) restores the pole body.
- F5 creates fixture `Fixtures/shared-camp-cloth139`, turn3500, player54,7HP38. Fresh isolated Editor Continue restores that state (11); all564,000 world-crop pixels match the saved view. This is Editor restart evidence, not a full standalone process test.

V2 profile lasted399.22s,173,802 frames; the131,072-sample ring wrapped. Retained ZoneRenderer samples mean0.098ms,p990.227ms; draw calls mean37.86,max43. Includes UI/tool/idle time and does not establish a controlled speedup, sustained FPS or Deck performance. JSON preserves outliers and wrap status.

Root preserved and SHA-checked all owned v1/v2/restart save branches before Finish; final Editor idle, override null, scene clean. Normal saves were never used for this test.49foreign tracked paths remain unchanged; no storage cleanup.

Independent review: `Verification/Live16x24/shared-camp-cloth-independent-review.md`. Bounded coverage accepted for these two exact owners. V2 reduces the ladder effect but repeated panel rhythm remains; the disclosed camp still has broad sparse patches. These are named later composition/material polish debts, not resolved by three sprite cells. FirstTent/Wellmeet/Gantry/DrownedLedger have registered uses but no new site-by-site actual acceptance here. Thermal and burning propagation are native-tested only; actual play did not ignite cloth. FrontierClothScreen and reclaim/service variants deliberately remain outside the adapter. Inventory is inapplicable to these non-takeable owners.

Next: publish exact owned source/evidence, verify origin/main, then one batched props+cloth Mac/Linux delivery. Preserve current8c0c72e11 packages and normal preferences/saves. Broader specialist residents and regional gaps remain open; no optional motion work.

Next-family evidence (finite audit): shared Farmer portability, approved `Sprites/Live16x24/farmer.png` plus editable `ArtSource/Live16x24/incidental-actors/`. VillagePopulationBuilder places Farmers at main-well settlements, while current body routes remain local-only. Prior actual control is incidental-actors-65-farmer-1x.png, earned kitchen224401440. No current disclosed Farmer outside the opening is established: acquire that baseline before extending or claiming global coverage. This is the candidate after batched delivery, not work completed.

## Packaged delivery — 2026-10-28 04:10UTC

Source9a2371852 is verified on origin/main and included in fresh Mac/Linux builds. See [batched delivery](Verification/Live16x24/packaged-camp-props-cloth-delivery.md):10actual Mac captures across2processes, world/readers, pole traversal, well drinking, Sack Open2items and exact564000-pixel save/restart comparison. Real Scorpion damage is preserved in fixture141. Linux archive verified, Linux/physicalDeck not executed. Earlier pending-package statements are superseded within these bounded observations; no full-area polish claim. Normal state, prior builds and49foreign paths preserved.
