# Sodden travel — current connected decking revision

Current generator: `Tools/Live16x24/build_sodden_deck.py`.
Build: `/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_sodden_deck.py -- --build`
Verify: `python3 Tools/Live16x24/build_sodden_deck.py` (or this directory's `verify.py`).
The old Tools/build_sodden_travel entry point redirects to the current generator to prevent accidental reversion.

Only connected-corner Duckboard frames change. Isolated/dead-end/straight masks0,1,2,4,5,8,10 and all non-board frames16–27 remain exact. Palette,448×24 atlas and28frame addressing stay unchanged. Native editable quads and source cells are canonical; no generated raster was sampled. Named defect: large grate-like gaps across the actual Causeway19.7 and Morrowfast crossing.

`deck-before/` freezes the preceding approved source, generator, validation and contacts. The historical generator's project-relative paths require the original checkout layout; current generator reads its pinned frozen cells directly. Root-level contact PNGs remain historical quiet-mire studies, not current deck previews. Current source receipt is `deck-verification.json`; actual gameplay/native tests live in `Docs/GRAPHICS-CAUSEWAY-DECK.md` and its evidence directory. Source validation is not full-area acceptance.
