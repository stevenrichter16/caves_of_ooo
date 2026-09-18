# Morrowfast 3D source kit

Authoring source is `build_scene.py`; inputs are exact native/art definition snapshots beside it. The builder writes only its output directory. It uses Blender's existing `bpy`/`bmesh`, metre units, a fixed independent art seed, and a shared palette atlas. No game RNG, native entity definition, scene or save is changed by generating art.

Run:

```
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 2 --python build_scene.py -- --output build --render
```

`--probe-only` exports only the labelled axis/cube fixture. `--skip-export` builds the source scene and manifest without bulk FBX export. `--render` also renders the overhead composition and roof cutaway. Source rendering is limited to two CPU threads.

The standalone axis probe retains its named markers and is excluded from the runtime manifest.

Outputs: editable `village_master.blend`, `models/*.fbx`, `textures/VillagePalette.png`, complete `manifest.json`, `reports/validation.json`, and optional `renders/village_overhead.png` / `village_cutaway.png`.

The master preserves individual stone, timber, cloth and foliage pieces within reusable collections. The assembled village instances those collections. The `ASSET_LIBRARY_EDITABLE` collection is excluded from the render view layer to avoid drawing every prototype at the world origin; enable it only to inspect/edit kit assets. Runtime FBX export assembles static source vertices, UVs and colors into one geometry mesh per model and a separate Water group where required; the small skinned character pieces are joined on temporary rig copies. This avoids repeated expensive scene reevaluation for dense ground patches. Original source collections remain editable.

Coordinate contract: Blender X=east, Y=north, Z=up. Desired Unity X=east, Y=up, Z=north. Exports use `axis_forward='Z', axis_up='Y'` plus one export-only parent rotation of π about Blender Z. Both ±Z presets alone reversed the horizontal axes in actual Unity import. Probe-v3's explicit correction passed all four Unity axis, scale and floor tests, as reported by the integrating root agent on 2026-09-09. The authoring scene remains east/north/up and manifest coordinates remain Unity east/up/north. Static models bake space transforms; rig models preserve bone transforms and require a separate animated import check.

Manifest positions use expected Unity axes. Native owner position is `(anchorX+.5,0,25-anchorY-.5)+visualOffset`. Preserve visual offsets when matching current native owner positions. House shells and roofs use the native owner anchor as their local origin, so their mesh bounds may be asymmetric around their roots. Doors pivot at their left hinge; their visualOffset is −.45 in X relative to the door cell centre. Rotate the prefab root about its up axis to open it. Physics/navigation remains native; these meshes are views.

All 71 native owners are mapped, with unchanged IDs and five roof/door/room relationships. Interiors follow the existing `room-open` visibility classification. The showcase player is preview-only and deliberately excluded from the persistent owner manifest. Static decorations do not create new interactive or blocking entities.

Rig: nine bones, feet-root origin, four hood palettes. Clips: Idle, Walk, Interact, Attack, Hit; 24fps, 48-frame Idle and 24-frame remaining clips; no Root translation keys. Equipment.Head, Equipment.Hand.L, Equipment.Hand.R and Equipment.Back are separate socket empties parented to bones. Meshes use simple rigid component weights appropriate to the small overhead character. Unity must validate skinned import, clip lengths/looping and bind poses before acceptance.

Palette materials: opaque VillagePalette samples 64 stable paint-grain atlas swatches through per-face UVs (1024×1024 PNG, 128×128 tiles, inset UVs); named VillageWater is opaque dark teal water with roughness .43 and metallic .05. Runtime materials must be recreated in the project's URP shader with native visibility/light masks. Blender's studio lighting does not establish Unity appearance.

Known intentional differences: native room/door footprints outrank ambiguous screenshot pixels; the existing creek is retained; unseen interior furnishings are authored. This is a three-dimensional recreation, not a textured plane or baked screenshot. The palette-and-geometry approach needs no complex Blender shader baking, but does not claim photoreal texture/AO bakes. Actual Unity screenshots, native state binding and performance remain root integration gates.

Equipment exports are equipment-blade, equipment-club, equipment-staff, equipment-shield, equipment-pack and equipment-helmet. They are reusable attachment models only, not owner/static placements. Blade/club/staff extend along local authoring +Z from a grip at zero; shield face lies in XY with normal +Z. Pack extends down from its back attachment; helmet covers the top of the hood relative to its head attachment. Runtime may apply an attachment-local rotation appropriate to the rig bone/silhouette. Native equipment remains authoritative.

Run `python3 reconcile_layout.py <build-output>` after generation to write the exact owner/footprint/room/water/decorations ledger under `reports/`. The JSON distinguishes reference pixel feet from model centres and documents the 67-cell native creek.

Validation helpers: `validate_fbx.py` imports a representative door, well, shell, rig and two attachments back into Blender and checks mesh/material groups, UVs, exact socket names and animation take names. Blender imports each FBX take as several per-object actions; the five distinct takes are the clip contract. `budget_report.py` computes conservative source AABB counts for the central crop and whole zone; it does not replace Unity profiling.

Reference refinement (2026-09-09): paving uses one deterministic variable-spacing world-coordinate point set with fitted rounded cell outlines, so detail-patch borders cannot restart rows. Roofs use low rounded masonry slabs with varied joints over a raised moss bed, including the inn's low roof subdivisions. Crowns have hundreds of rounded closed leaflets above overlapping cores, and color accents use deliberate flower groups. Canopies have sag, edge hems/ties and directional folds; wellwater has visible broken curve highlights. Ground paint uses granular multiscale value noise instead of repeating sine-wave shards. These are authored material/geometry changes; no vertex-AO or extra Unity shader contract was introduced.

Authorized starter garden: bare prepared earth marks six actual native cells, X 41 and 42, Y 21 through 23, adjacent to the new-game village start at (40,23). The main X40 road stays clear. The right cosmetic road verge is recut around these cells; native footprints, room extents and the 71-owner mappings remain unchanged. The terrain-detail mesh contains soil/furrows only and does not fake planted crops. Native Plantable properties and real planting/growth remain gameplay-owned. The informational manifest starterGardenCells list records the exact mask.


Surface polish follow-up: the accepted editable master and model FBXs now include the reviewed sculpt-normal pass. See `../ModelPolish3D/README.md` for the required fresh-build → polish → validate pipeline, original archive and preservation limits. `renders/polish-before.png` / `polish-after.png` are matched Blender galleries, not native screenshots. Per-model decisions are in `reports/polish.json`.
