# Sodden native art source

`build_kit.py` is the authoritative original cuboid recipe. It writes deterministic
`kit.json` with 85 combined-mesh models and a private 24-color palette. Run it from
the repository root with `python3 ArtSource/SoddenNativeArt3D/build_kit.py`.

The native import entry is
`CavesOfOoo.Editor.SoddenNativeArtKitBuilder.Run(reportPath)` in clean Unity Edit
mode. It validates the complete reviewed source SHA before writing only
`Assets/Resources/SoddenNativeArt3D`. Existing generated assets are updated in place
to retain their GUIDs. The receipt records source hash, model, cuboid and triangle
counts. This importer does not save a scene or modify the shared ring/glade atlas.

The kit overrides 24 existing Sodden model IDs, four existing reed IDs, and 45
census-verified named-place IDs only after the presentation layer validates their
actual native owners and biome scope. The named-place set covers Sumphold paving,
walls and ordinary water; district floors, dressing-bench states, locker, salvage
and notice; and Drowned Ledger boards, canvas, preserved bodies, stakes and tables.
Ten additional exact-ID replacements cover four vine walls, four brine pools,
acid and a mineral steam vent. Brine remains pale cyan and acid remains green
with yellow-green foam; regional art does not remove liquid warning identities.
The two new Greatdew models require exact native snare-owner admission.
Greatdew is static non-solid vegetation, not a creature. No art prefab adds
collision, gameplay components, animation, light or native state.

The final source contains 1,529 cuboids / 18,348 triangles across all 85 models;
no model exceeds 36 cuboids. Source SHA-256 is
`e5eb1e3fa457642eebdf8d3d6e3e269964f714101ebbab2404b5545f6159c432`.
Native import/test receipts and same-location images are recorded in
`Docs/Verification/SoddenArtDirection/`; source preflight alone is not native
acceptance. These are asset measurements, not frame-rate measurements. Repeated
terrain is still handled by native batching.

Mire's top is .048 cells high, above the existing saved-water layer at .035;
the opaque dark pool still belongs to a real native mire owner. Water-only saved
coatings remain their own physical/visual feature. Ground and mire have level
shared boundaries; small accents do not form a raised rim around each tile.
The ordinary named-place water mesh stays at .012, below the saved-water film;
it deliberately remains lighter than black mire. Floor/paving IDs are ground
models; walls, canvas and props remain native entity models. Straight canvas
spans X and corner canvas joins +X/+Z, preserving existing quarter-turn recipes.

See `Docs/Verification/SoddenArtDirection/ModelDesign.md` for full-biome coverage,
source constraints, review gates and deliberate reuse of existing original actor
rigs/crop stages. Unity-native images and actual gameplay are the visual acceptance
gate. Source bounds, tests and the import receipt do not prove visual similarity,
readability, interaction feel, animation quality or performance.
