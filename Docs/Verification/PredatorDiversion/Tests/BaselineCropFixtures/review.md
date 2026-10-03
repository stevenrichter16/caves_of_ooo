# Broad-sweep crop fixture triage

HEAD:49dc8768f4952a0187abbbf46bc7fa2beade8937. No Assets files were written for this task.

## Stale crop readout phrase

CropPart.cs, ExaminablePart.cs, and SpreadCropReadoutTests.cs are each byte-identical between HEAD and current. Exact SHA256 evidence: head-comparison.txt. HEAD already has the truthful automatic maturity sentence in CropPart:224; the old test expects the obsolete phrase `this area`.

`correction.patch` changes only that assertion to `At maturity, produce falls here to pick up.` It continues to check dry/paused vs moist/growing state and verifies reading does not consume moisture/time. Supplemental reference baseline6/7 (same single native failure), scratch-corrected7/7. No crop production prose changes.

## Stale elapsed-time driver

CropSystem.cs, CropSystemPart.cs, CropTime.cs and SpreadEverydayResidentTests.cs are byte-identical to HEAD; the two crop and two seed parsed blueprints are also identical. Exact comparison: resident-head-comparison.txt. The elapsed-world-time policy shipped in3afb07f20; stamped player TickEnd reconciles WorldClock.CurrentTick, which the old resident helper never advances. NPC event counts and repeated player notifications intentionally cannot manufacture growth.

`resident-correction.patch` installs a fixture-owned TurnManager after DensityLootTestScope creates its native HotbarSaveFixture, restores prior Active in finally, and advances10 actual world ticks before each player reconciliation boundary. NPC events retain the same clock. Rename the test to describe elapsed-world-time behavior, preserving all dry/wet/cooldown/seed debit/finite produce assertions. The source fixture remains a core clock-driving fixture; this does not claim native scheduler/input validation.

Supplemental reference baseline8/10, exact CandyCarrot/Emberwheat failures; scratch-corrected10/10. Native's HotbarSaveFixture installs its own clock (unlike the inert reference shim), so construction order was explicitly inspected and corrected before staging publication. Root owns native recheck after the ongoing wide sweep.
