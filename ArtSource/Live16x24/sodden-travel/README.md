# Sodden travel — current peat face revision

Build: `/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_sodden_peat.py -- --build`
Verify: `python3 Tools/Live16x24/build_sodden_peat.py --verify` (or this directory's `verify.py`). Both earlier Tools entry points delegate to the current generator. Use `peat-before/` for the preserved prior deck source; `deck-before/` retains the earlier quiet-mire source. Historical generator paths require the original checkout layout.

Only PeatBank18–19 change from the last accepted deck revision. All26otherframes, original palette keys, opaque straight bank footprints,448×24 atlas,28frame addresses and importer remain exact. New palette keys apply only to peat faces. Native editable quads/source-cells are canonical; no generated raster was sampled. Named defect: long repeated dark hatch marks make real solid cutbanks read flat. Broad compressed faces, sparse roots and a dark moist foot give material relief without extra geometry or cast-shadow code.

Current source receipt: `peat-verification.json`; historical `deck-verification.json` belongs to the previous revision. Root-level contacts remain historical quiet-mire studies, not current previews. Actual evidence and acceptance: `Docs/GRAPHICS-PEAT-BANK-RELIEF.md`. This is a bounded correction of the approved Sodden conceptual material direction; reuse the established concept/parts rather than commission another scene concept. No full-area acceptance follows from native validation.
