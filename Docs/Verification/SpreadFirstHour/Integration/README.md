# Final integrated verification

The first unfiltered native sweep completed20,404 cases:20,365 passed,39 failed,
zero skipped. The failures were34 nullable-manager save regressions, one stale
exact equipment roster and four old examination test premises. The published
save/pin fixes pass336/336 actual Unity cases; the reader fixtures and visibility/
reach counterchecks pass64/64. No production guard was weakened.

Actual unfiltered Unity EditMode **20,406/20,406 passed**, zero failures/skips, 1371.069s (job `b80eff45e56e4bdb90681af56ac80242`).
All23,789 recorded inputs are unchanged; original editor/save/input state matches
exactly. See `full-native-green.xml.gz`, `input-drift.json` and `restoration.json`.

- `run.json`: current unfiltered Unity job and discovered count.
- `input-manifest.json.gz`:23,789 input paths captured before the current run.
- `before.json`: exact editor/scene/bootstrap/save/input state.
- `FirstRun/`: preserved first-run inputs and state; `full-first-red.xml.gz` is
  its complete authoritative20,404-case failure result.
- `save-equipment-source-native-green.xml.gz`:336/336, including all92 current
  pair/viper source tests and all affected save/equipment fixtures.
- `reader-fixture-native-green.xml.gz`:64/64, including16 interaction fixtures,
  29 guarded readers,10 steam and9 reach checks.
- `cold-review-combat.md`: independent source/save review and pair callback fix.
- `PairAuthority/`: executed2RED/2controls→92GREEN private repair evidence.
- `NullManagerSave/`: reproduced existing save regression and narrow guard.
- `EquipmentPin/`: exact two deliberate original Loadout rows, preserving the
  global allowlist and nonselected controls (91before→93after).
- `ReaderFixture/`: actual first-full failures, exact fixture-only patch and
  independent peer review; unpublished local turn manager avoids test-state leaks.
- `Q1-Q4.md`: integrated claims, counterchecks, native acceptance and limits.

Raw failed runs are retained separately from fixes and final validation.
Standalone evidence does not prove Unity behavior or imported models.
No production input changes are permitted during either full native run.
