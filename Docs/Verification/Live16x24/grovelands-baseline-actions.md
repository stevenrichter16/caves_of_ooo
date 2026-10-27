# Grovelands playable baseline — 2026-10-27

Actual Unity Editor 6000.3.4f1, flat presentation, normal keyboard controls, 1280×800 Game view. Root owned the isolated session. This is **before-art gameplay**, not native material acceptance. Started from unchanged earned `Fixtures/packaged-harvest-products-saltpan65` (seed65, turn3300, HP38). Normal player saves and38 unrelated tracked files were hash-checked unchanged afterward.

Receiving-world map was inspected read-only first: all four addresses below were ordinary Grovelands, no current POI or saved lair column. Travel used Shift+comma, actual map arrows, Shift+period. No teleport, grants, invulnerability, forced generation or fog reveal. F5 produced each preserved fixture pair. Map UI offsets are +30,+3 from authored coordinates.

|Actual route/action|Result and capture|Saved turn|
|---|---|---:|
|SaltPan15,13 → ten left/six up → FruitingWall5,7|Arrival40,12 HP38; `grovelands-before-01-fruiting-wall.png`. 29 real FruitingBody,10 visible;202VineWall,58Tree,81Bush. Four normal Down/Down/Up/Up inputs returned40,12.|3640 arrival|
|One map step west → Grove4,7|Arrival40,12;13MycelialColumn, seep, sign,2red growth all visible; `grovelands-before-02-grove.png`.|3720|
|Nine right/one up →49,11; C/right|Actual seep interaction offers draw water and reports ground water; capture03. W draws through normal action, stock40→39, HP38; capture04.|3820|
|One map step south → CompostingField4,8|55CompostRow and4CompostCache, all visible; capture05. Right×3/Left×3 crossed the nearest passable row approach and returned to origin before leaving.|3860 arrival|
|Four map steps west → TendrilFen0,8|47FruitingBody,7red growth,2ChoirTendril; all visible; actual braided tile-state water; capture06.|4020|
|Down×3 →40,15; C/right →41,15 forage; H|Actual context offers harvest, capture07. Harvest removes GroveRedGrowth, underlying cell contains Grass, inventory gains2×GroveRed; HP38. Capture08.|4060|

Fixtures: `grovelands-fruiting-baseline65`, `grovelands-grove-baseline65`, `grovelands-seep-drawn65`, `grovelands-compost-baseline65`, `grovelands-fen-baseline65`, `grovelands-red-harvested65`. All retain the original gameID `50e670c30b6d48feb82756df64bd1cbc`; earned bytes copied without editing. Full owned session archived at `/tmp/coo-grovelands-baseline-earned-c58f1b3d` before Finish; original fresh test root retained separately. Root exited Play before source import/tests.

## Visual critique and coverage bounds

Actual normal-zoom views show designed density and quiet approach lanes, not empty generation. Grass plus-glyphs produce uniform noise; vine hash-glyphs fail to communicate connected solid growth; shelves, rows, water and landmarks depend on punctuation. Pale-column ring, quiet central seep, broken shelf beds, low compost rows and braided Fen water provide strong existing composition. Fog is unobserved space, not a bare patch to fill. Retain layouts and generation; native replacement must establish material hierarchy without multiplying objects.

The captures cover ordinary arrival views and bounded walking, one real water draw and one forage harvest. They are not full80×25 exploration, all-state acceptance, packaged verification or biome completion. No live empty-seep depletion, cache harvest, sign read, ore discovery, whole-region creature/crop coverage or performance claim is made here. Shambler/Mosshulk/ChoirTendril and optional props/hazards remain named follow-ups.

## Imagegen extraction pass

Built-in imagegen used four actual captures and approved Sill contact (five-image API maximum); exact prompt is `grovelands-concept-prompt.txt`. Original output retained and copied to `ArtSource/Live16x24/grovelands-materials/concepts/grovelands-four-formations-v1.png`. The initial seven-input request was rejected before generation; external NPS photos were then represented by observed form descriptions, not falsely counted as attached inputs.

Selected viewed external references and source semantics are recorded in the independent review. Accept concept's pale tapered rooted columns, muted violet folded shelf masses, dark wine low plants and restrained teal source contrast. Reject its dense bright grass carpet, oversized multi-cell caps, invented fog-edge architecture/skeletons, mushroom-like compost and excessive white wave lines. These are reference decisions only; no generated crop/downsample becomes production art. Fourteen original native cells are being authored with exact owner/state mappings and unchanged approved base reuse. Native gameplay acceptance remains pending.
