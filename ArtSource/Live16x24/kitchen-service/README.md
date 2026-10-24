# Kitchen service: two native state cues

2026-10-24. Two static 16×24 derivatives clarify an actual working batch and a ready commissioned meal. The original east/kitchen atlas remains unchanged. These images do not create service state, supplies, heat, worker activity or pickup permissions; the runtime must prove those current state/contents relationships before selecting either derivative.

## Viewed concept and extraction

Root's [concept.png](concept.png), with [exact imagegen prompt](imagegen-prompt.txt), uses actual gameplay and the approved contact as references. Retained ideas: a quiet linen/olive cloth covers the formerly empty dark pan cavity; one recognizable wrapped FieldMeal sits on the existing serving board. Rejected: the generated image's soft shading, inaccurate reconstructed rims/feet, excessive pixel density and any implication that its study panels are exact native assets or gameplay evidence. No generated raster pixels were sampled, cropped, resized, traced or composited.

Authoring reads `../east-kitchen/source-cells.json`. The repaired pan at frame5 and neutral pickup board at frame7 are copied as immutable base-cell layers. The approved FieldMeal at frame13 is copied as an overlay with a single integer translation `(0,-3)`; all52 occupied meal pixels retain their exact colors and part names. It is not resized, redrawn or replaced with a new meal icon. The new cloth is a58-pixel native layer with a small stepped fold and two short draped edges. It uses existing linen and olive palette entries, with no cord/cross that could confuse it with the completed parcel.

## Exact atlas mapping

Resource: `Sprites/Live16x24/kitchen-service`, PNG size32×24, two adjacent transparent native16×24 frames.

| Index | Rectangle x,y,w,h | Actual subject/state | Base and overlay |
| --- | --- | --- | --- |
| 0 | 0,0,16,24 | `ConnectedBatchPan`, valid current `Working` batch | Approved repaired pan5 + cloth over cavity; handles, lower clay body, base and repaired seam remain unchanged |
| 1 | 16,0,16,24 | `ConnectedKitchenPickup`, valid current `Ready` commissioned output | Approved neutral board7 + one exact approved FieldMeal13 shifted up3 rows; board handles and lower edge remain visible |

The original damaged pan4, repaired pan5 and neutral board7 remain the old source assets and ordinary fallback choices. No new idle frame is shipped. A missing state resource, invalid job, absent paid input or missing/foreign commissioned output must not imply a working/ready state. This is a presentation contract for root's state/custody tests; the art generator does not inspect or change the game.

The cloth is a physical working-state cue, not a flame, smoke plume, progress bar, service-availability badge, healing effect or proof the Cook is currently present. The single parcel represents a ready commissioned output and does not declare an arbitrary stock quantity or permission to take it. The original bodies are still present beneath the overlays in the editable Blender scene; their covered pixels have not been destructively replaced in the source layers.

## Source files and recreation

- `kitchen-service.blend`: two native layered collections, plus direct8× comparison scene. Each collection has editable approved-base and state-overlay integer-pixel quads.
- `source-cells.json`: full approved base cells, separate overlay cells and final composited cells, with source path/hash and existing palette.
- `kitchen-service.png`:32×24 native direct geometry render.
- `kitchen-service-contact.png`:1024×320 direct8× geometry render, comparing approved repaired pan / Working cloth / approved neutral board / Ready parcel. It is a source comparison, not gameplay evidence.
- `manifest.json`: exact state mapping, source indices, palette, dimensions, translation and asset hash.
- Generator: `Tools/Live16x24/build_kitchen_service.py`.
- Runtime files: `Assets/Resources/Sprites/Live16x24/kitchen-service.png` and fresh `.meta`.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_kitchen_service.py
python3 ArtSource/Live16x24/kitchen-service/verify.py
```

Blender5.2.1LTS renders flat emission quads directly at native resolution. Existing render/import helpers are read-only. The generator writes only this source directory and its new owned resource/metafile, retaining any Unity-normalized metadata on rebuild. Pillow is used by the verifier only for read-only pixel comparison; it does not generate or edit the image.

## Palette and bounded visual review

Working pan has five visible colors: clay `#A97542`, clay edge `#C39565`, clay base `#765337`, linen `#B5A47E` and olive fold `#96A462`. Ready board has five: wood `#94713D`, edge `#BCA576`, cord/dark wood `#5F4830`, linen `#B5A47E` and cream `#E4D0A0`. These are all approved east/kitchen palette colors. Alpha is0/255; both silhouettes finish at the original baseline row22 with one empty row below. Neither extends outside one16×24 cell.

The native atlas and direct8× comparison were inspected. The working vessel's pale uncorded cover is clearly different from its dark open idle cavity; its clay handles and repaired lower body remain identifiable. The ready board shows the existing dark tied parcel motif within its handles and wooden lower edge. Both remain quiet low objects, with no added outline, glow, smoke, fire, gradients or animation. This source review does not establish native-scale readability in the ordinary scene; actual working/ready/collected transitions and reader consistency remain root's next gate.

## Verification

`source-red.txt` records failure before the new outputs existed. `source-green.txt` and `verification.json` record:

- Exact32×24 native atlas, two16×24 frames and hard alpha.
- Exact approved base-cell layers; every output pixel outside its overlay is unchanged.
- All52 approved meal pixels translated exactly by `(0,-3)` with no recoloring/resizing.
- Pan handles, baseline and repaired clay seam preserved; cloth covers the dark cavity.
- Zero native/source and direct8× comparison pixel mismatches, including the unchanged comparison frames.
- Byte-identical source/resource PNG and the authored Point/Clamp/16PPU/no-mipmap/no-compression import contract.
- SHA256 preservation of24 original source/art files listed in `preserved-source-hashes.json`.

Final PNG SHA256: `0d307429eb432ebe9828af9cff2c048d818bb2856b82a56b891dcd3ac4b324e7`.

No runtime C#, gameplay test fixture, world generation, ledger, Unity scene or git commit was changed by the art-authoring subagent. Source evidence does not prove state-query purity, paid-input/output custody, graph/fog admission, reader lifetime, save/load, interaction access, animation, performance or release quality.
