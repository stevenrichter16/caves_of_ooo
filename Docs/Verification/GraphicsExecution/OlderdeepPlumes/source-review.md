# Independent source review

Reviewer: `sumphold_grade_source_review`, read-only; 2026-11-15.

Final: **0 P1/P2 findings** in implementation and corrected tests. Exception requires the Olderdeep catalog instance, exact `Overworld.4.6.2`, current map/cache/profile/biome/no-lair authority, one canonical disclosed plume and a real StoneFloor. Bare, rubble and sandstone substrates remain refused. Finite guard preserves ownership, positive partial HP, thermal/light/war semantics and rejects added meanings. Ambient, foreground, memory and mechanics routes unchanged.

Test corrections before final GREEN: initial enum assignment failed compilation, corrected to explicit `EntityVisualFacing` cast before RED. After RED, reputation oracle corrected from unrelated `ListeningTradition` key to actual `CatacombFolk`; catalog-isolation fixture supplied valid optional Sill art. Neither is evidence of a production regression. RED retained separately: 61 cases, 55 missing-positive-premise failures, 6 controls. Native GREEN and actual play are independent gates.

Initial GREEN 558: 556 passed, two failed. Covered-cell assertion wrongly treated the player's collider as a floor change; now checked only when uncovered. Existing bloom-expiry fixture lacked PhysicsPart and never slept. The unchanged published production source reproduced its 16799-tick failure in a three-case isolated run (two expired cases passed vacuously). Added owned PhysicsPart to canonical Actor helper and an immediate HasBloom assertion before elapsed-clock advancement. Existing missing/foreign-physics refusals remain in FoundingRestTests. Independent reviewer confirmed cause and minimal test-only correction; final GREEN rerun required. Raw initial and control XML retained.
