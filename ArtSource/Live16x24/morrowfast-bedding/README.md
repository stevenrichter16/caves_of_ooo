# Dry Hem green bedding correction

Two existing MorrowfastSceneProp owners guest-bed-west/east use authored green covers, retaining original timber/pillow/softened-outline/silhouette. Reuses approved muted item palette g, changes64textileRGBpixels across atlasframes288/289. Allalpha and122112outside-framepixels exact; allother pixels within frames also exact. PropIcons index and importer unchanged. No new art generation, geometry, states, mechanics, population or shadow changes.

Editable source is prop-icons/make_prop_icons.py; bounded --morrowfast-beds-only asserts current index/archived frame identity, allows only original or corrected beds, refuses unreviewed atlas changes and never rewrites index. Full-export path shares the exact-owner textile correction, but full enumeration is not used here.

source-cells.json includes semantic textile/timber/pillow/outline groups with null transparent cells. editable.blend contains248opaque quads, saved and reopened to rerender bedding.png/reopened.png; all768RGBApixels match source and runtime. Reproduce via Tools/Live16x24/build_morrowfast_bedding.py documented commands. Four protected inputs and five prior approved originals retained. before-manifest.json pins historical originals; it is not repinned.

Actual before/after, native RED/GREEN, readers/Rest/travel/reload and independent reviews are in Docs/Verification/GraphicsExecution/MorrowfastRoom. This corrects coveridentity and modest material separation; small furnishing silhouettes and whole-room/area polish remain open.
