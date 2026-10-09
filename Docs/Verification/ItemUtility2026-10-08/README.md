# Item utility audit evidence

Source snapshot: main `c7e154dbcdafcbef882ce3dac5a7fe9cec21773a`, 8 October 2026.

This directory supports the [assessment](../../ITEM-UTILITY-AUDIT-2026-10-08.md) and [complete catalog](../../ITEM-UTILITY-CATALOG-2026-10-08.md). No gameplay changes or new Unity run were made for this audit.

## Inventory coverage

`catalog.json` includes source, actual use, limitations and file/line evidence for every row. Blueprint inheritance was resolved by merging parent parts and overriding only authored child parameters. All 298 takeable candidates from 709 object definitions are accounted for:

- 286 source-traced obtainable static item definitions.
- Eight inheritance templates excluded from player content.
- Four concrete items excluded because no current normal acquisition path was found: EchoKnife, InertSludge, CharredTonic and LanternOil.
- Seven additional runtime-created or recovered portable item families make 293 obtainable portable families in total.

Eight movable world-object families are supplemental, not portable-item count padding. All 26 liquid registry IDs are reviewed separately: six collectible, two with live pools whose collection fails the purity gate, and 18 without a current physical collection source. World coatings can use a liquid definition without making it an obtainable bottled resource.

Runtime variants of forged weapons, brews, copied books, corpse harvest payloads and modified equipment are grouped by generating system. The catalog is not a count of unique combinations. Random table membership alone was not accepted as reachability: current generation, stock owners, harvest/craft inputs and action consumers were traced. A source path is not a promise of availability in every seed or old save.

## Reagent calculation

Run from the repository root:

```sh
python3 Docs/Verification/ItemUtility2026-10-08/reagent_reachability_probe.py
```

This updates `reagent_reachability.json`, including source hashes. The current result is 24 reagent profiles, 1,471 distinct property sets obtainable from nonempty selections, 1,468 Brew outcomes, three Mishap outcomes and zero InertSludge outcomes.

The calculation reproduces the current property parser, positive MAX merge, required/forbidden presence rules and outcome branch in Python. It handles bare properties, signed Int32 potencies, parser separators, empty profiles and duplicate rule IDs. Because potency merges by maximum and outcome matching uses presence, repeating an ingredient cannot reach an extra no-effect branch. The report records witnesses for every no-effect profile.

Counter-reading also checked for another live sludge source or normal-play mutation of reagent properties. None was found. This bounded proof does not apply to modded, saved or development-injected reagents/rules. It does not execute C# or verify native UI behavior, input consumption, mishap damage, item spawning or subjective balance. The player command does apply mishap damage; the resolver's no-output classification must not be confused with a harmless attempt.

## Review and validation

Three category reviews were reconciled against an independent census. Source-routing corrections and disputed negative findings received a second reading. The saved `audit_validation.json` records candidate reconciliation, unique IDs, citation path/line checks, file hashes and the snapshot. Markdown links were checked in all three documents. A valid line number only establishes an address, not that the cited claim is true; the semantic checks were source review.

TDD, Unity EditMode/PlayMode and the standalone gameplay runner were not run because production behavior was not changed. No historical test total is reused as evidence for this analysis. Existing unrelated editor logs and other workspace artifacts were left alone.
