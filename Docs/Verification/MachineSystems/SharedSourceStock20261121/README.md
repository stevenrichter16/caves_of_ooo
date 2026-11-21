# Shared proofing-source stock correction — November 21, 2026

The previous search stopped after 400 source cells. Different entry points to a
larger connected source could elect different holders and create separate stock
budgets. Search now covers the bounded native zone (80 × 25 cells).

Existing saved stock holders take precedence, ordered by (y, x, entity ID). This
preserves depleted budgets created on noncanonical cells by the old search.
Lookup examines every eligible source owner on each cell, validates stock
parentage, never mutates or combines budgets, and adds no saved fields. Multiple
legacy budgets remain dormant while connected except for the elected holder.
Changing topology/removing the elected owner is not a migration solved here.

## Verification

- Native initial RED: four geometry cases; 400 cells passed, 420/1,000/2,000
  cells failed. No compiler errors.
- Independent review identified a legacy-save refill countercase. Native RED
  for the two save-upgrade cases failed before the existing-stock correction.
- Final native GREEN: 184/184 tray, scraper and proofing cases, 12.18 seconds
  reported test duration. No compiler errors. Receipts overlap earlier runs.
- Final bounded independent source review: zero remaining P1/P2 findings in
  source/test diff. Reviewer did not run tests or gameplay.
- Both mutation checks were killed: restoring the partial search failed five cases;
  removing legacy-stock retention failed two cases.
- Actual GameBootstrap play used a scraper and two trays on frost vent 2389.
  All three drew five total catches from its five-unit stock, then refused more.
  Fresh Play/Continue preserved the depleted 0/5 budget and LastTick 190 at tick
  910. See [the native receipt](../CampPlay20261121/README.md). Large geometry
  and legacy-upgrade coverage remain native tests, not an authored giant live pool.
- Subsequent action-time corrections pass a combined 303-case native regression
  including this source family. Independent source/evidence reviews are bounded;
  the inherited full-suite assessment remains open. This document does not accept
  the whole machine or camp program.

The final full-suite run inherited from Claude (`fullrun_h`, runtime e89a81af6)
was left running in clone4. The obsolete clone2 `fullg` run was terminated by
exact PID/command guard to remove contention, preserving its logs. Targeted
checks ran in clone3, never the user's open Editor.

All 84 preexisting tracked dirty-file hashes matched the prechange snapshot
after native GREEN. Normal persistent-state files (1,226) were snapshotted
before preparing isolated gameplay. No normal save is a playtest target.
