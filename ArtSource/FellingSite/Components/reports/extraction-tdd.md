# Extraction test-first record

Eighteen pixel-ownership/removal tests were written before `ArtTools/felling_components.py` existed. The initial run failed at import: `ImportError: cannot import name felling_components from ArtTools`. After implementing the pipeline, all 18 tests passed. This was a missing-module RED gate, not 18 separate executed assertion failures. Contract RED/GREEN raw logs are preserved separately.

Subsequent regressions cover a crop whose foot anchor lies beyond its visible silhouette, self-intersecting polygons, exclusion of pale stone from red plants, and exclusion barriers protecting the same stone during backing repair.

Native-scale visual review then found that the brighter red-tip selection lost dim brown connecting branches. The added `test_red_mask_retains_dim_brown_connecting_stem` failed with `AssertionError: visible brown stem must survive saturation filtering`. The producer now permits lower saturation for connected dim branches while retaining the bright-stone rejection and authored stone exclusions. The 23-test pixel suite passed after this correction. Final raw output is refreshed by `verify_felling_components.py`.
