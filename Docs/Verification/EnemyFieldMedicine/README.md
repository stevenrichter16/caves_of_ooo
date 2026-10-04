# Enemy field medicine verification

All XML files here are native Unity6000.3.4f1 EditMode results. The headless runner was not used for this milestone. Historical failures in RED files are deliberate missing-feature/review evidence, not the final release result.

## Reproduce

1. In an idle Unity editor run **Caves of Ooo → Art → Build Patchbearer Kit**. It writes two persistent assets and checks borrowed body/rig/material/clip bytes remain unchanged.
2. Run `FieldMedicineTests`, `FieldMedicineAdversarialTests`, `FieldMedicineVisualHookTests`, `FieldMedicineSourceTests`, `PatchbearerArtTests`, `PatchbearerArtGalleryTests`. The explicit `FieldMedicineSourceTests.Seed64WholeOrdinarySpreadCensus` is an optional, separately selected full-map diagnostic; ordinary regression need not regenerate the whole world.
3. Run **Caves Of Ooo → Scenarios → World → Field Medicine Native Audit** from the saved main scene with Play stopped. The launcher isolates saves/preferences, starts native new-game input at seed1729, locates a generated actor in its nearest32 ordinary Spread columns through registered physical stairs, and makes one labelled player approach transfer and one labelled injury to8HP. An ordinary wait invokes real AI; F5/F6 verifies saved depletion. It restores the previous editor scene/save settings and writes a run folder under `Native/`.

The staged approach/injury is disclosed in both source and report. No ordinary walking route, natural injury sequence, balance ranking or bespoke drinking animation is claimed. Independent generated-source tests cover source admission/RNG/counts; core tests cover inhibition, death loot and saved inventory; actual screenshot review covers appearance.

`ArtGallery/` preserves the initial low rear bottle and final raised shoulder versions as separate runs. Its static screenshots sample native clips on a generated cave background under disclosed fixtures. They do not prove fluid animation or combat feel.

Final outcome and Q1–Q4 review are in `../../ENEMY-FIELD-MEDICINE-DESIGN.md`.
