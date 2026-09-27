# Current rare-equipment allowlist correction

Readiness before changes: the failing global allowlist derives from GameAuditEntityEquipmentContentTests.Kits. Both approved rare children now intentionally declare exact Loadout overrides; all other existing Objects declarations are unchanged. The shared Kits manifest is missing only those two additions.

Proposed minimum: add literal Cutter ShortSword;LeatherCap and Mate Cudgel rows to Kits. This automatically exercises existing ActualBlueprintCreatesItsAuthoredKitWithRealOwnership for both; keep global exact set equality, Humanoid-derived class checks and Villager/SummitSinger/Armorer no-Loadout controls unchanged. No production or data change. Existing latchcoil source fixture separately pins natural bite/no equipment and unchanged ordinary Viper.

First run unchanged current equipment fixtures privately to retain actual allowlist RED; then change two data rows in the single test source and rerun the same equipment cohort.

Outcome: current91 =90PASS/1FAIL. The actual failure reports exactly two extras: SpreadDitchMate and SpreadHurdleCutter. Three-line candidate (comment +2 rows) yields93/93GREEN, including both new actual kit/ownership cases; actual-reference compile0. Original adversarial source is untouched. Root native publication remains the final gate.
