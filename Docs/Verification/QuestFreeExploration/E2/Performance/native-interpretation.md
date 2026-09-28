# Actual Unity bounded cost interpretation

The native probe completed **330 rows, no errors, restored=true** in Unity 6000.3.4f1 on 2026-09-28 (03:23:06.847–03:23:29.874 UTC). The caller timed out while returning the result, but the complete validated disk report exists; this is not a gameplay failure. Raw: `native-report.json`; its exact SHA256 and calculated distributions are in `native-summary.json`.

The allocation counter returned **0 for the retained 64 KiB positive control**, so it was rejected and all sample allocation values are null. Allocation is unavailable, not zero. Process heap snapshots include the existing editor/session and are not a retained-world memory measurement or a capacity guarantee.

## Paired measurements

World samples compare current explicit legacy-disabled and current exploration-enabled managers, not an archived old binary. Each frozen seed has one alternating arm pair and twenty cold chunks. Role samples compare 32 freshly constructed dense fixtures per branch, with one real scheduler action and corresponding no-role control. Pairing is seed/zone/iteration; ratios below are medians of paired ratios, not ratios of displayed medians. Timing includes the actual different work, not a claim that both arms produce identical worlds.

| Operation | Legacy/control median ms | Current/role median ms | Median paired ratio | Median paired delta ms | Current max ms |
|---|---:|---:|---:|---:|---:|
| Manager constructor (3 pairs) | 2.9830 | 4.4442 | 1.4432 | +1.2786 | 4.5873 |
| Cold chunk generation (60 pairs) | 24.1261 | 26.9346 | 1.0814 | +2.0520 | 61.9241 |
| In-memory save of 20 chunks (3 pairs) | 888.7133 | 918.3298 | 1.0221 | +19.6143 | 937.8912 |
| In-memory load of 20 chunks (3 pairs) | 2018.9649 | 1973.7151 | 0.9776 | −45.2498 | 1995.9038 |
| Territory warning (32 pairs) | 0.0270 | 0.1212 | 4.1366 | +0.0847 | 0.8743 |
| Grazer feeding (32 pairs) | 0.0229 | 0.0272 | 1.2555 | +0.0052 | 0.4130 |
| Grazer flight (32 pairs) | 0.0195 | 0.0304 | 1.5243 | +0.0109 | 0.2007 |

Generation totals are 1582.5481 versus 1866.9478 ms (+18.0% across the sixty chunks). Its current p95 is 47.1620 ms versus 42.6671 ms. These are cold synchronous generation costs, not frame times. Load recorded collections and varied substantially; the smaller current median does not establish a load speedup. No measured constructor/generation/save/role interval recorded a collection. Preserve the first no-role territory sample of 6.7271 ms; it is not excluded to improve the comparison.

## Investigation of at-least-twofold cases

Five cold pairs exceeded 2×. The existing same-seed native source census identifies all five as successfully committed placements, with matching owner counts:

| Seed / zone | Family | Legacy → current ms | Ratio |
|---|---|---:|---:|
| 1 / 11.9 | OccupiedBank | 28.6466 → 61.9241 | 2.1617 |
| 64 / 11.12 | RoadSpill | 20.4798 → 42.3970 | 2.0702 |
| 64 / 9.10 | WateringMargin | 20.5000 → 41.0223 | 2.0011 |
| 1729 / 11.9 | OccupiedBank | 22.8047 → 55.9363 | 2.4528 |
| 1729 / 12.8 | RoadSpill | 20.7687 → 45.7640 | 2.2035 |

Read-only source review found bounded additional work in the successful branches: `SpreadExplorationBuilder.cs` builds initial/current/final geometry and exact source validators; actor placement also snapshots owners and validates bypass/critical routes before and after its callback. Road cargo captures other receipt-owner state and reruns current geometry before final acceptance. This is a credible source of extra cold work; this probe has no nested timings, so it cannot attribute an exact fraction to any method. No repeat/unbounded retry or source growth defect was found. Do not remove the acceptance guards based on this single cohort.

The territory role has a meaningful 4.14× paired ratio but +0.0847 ms median absolute cost in this one-actor fixture. `SpreadTerritoryPart.TryTakeTurn` performs a bounded immediate-personal-threat scan, validates the actual post, then performs the bounded territory/faction scan and warning emission. Its radius is capped at 20. The ordinary control does different work. Feeding/flight also have isolated >2× ratios on very small durations; their current maxima are 0.4130/0.2007 ms. This does not establish all-NPC or whole-frame cost. If a live many-role profile later shows a problem, nested markers in these bounded scans/finalization passes are the next measurement; this result alone does not justify further optimization or reruns.

Source census labels: `E2/NativeCensus/20260928T025003Z-9a9502a9/seed-{1,64,1729}-sources.json`. They classify owners/families, not CPU causality.

## Save size and retention

| Seed | Legacy bytes | Current bytes | Increase | Owner count legacy → current | Current retained graphs |
|---|---:|---:|---:|---:|---:|
| 1 | 34,569,625 | 35,981,929 | 4.09% | 44,675 → 45,728 | 18/20 |
| 64 | 34,107,874 | 35,589,375 | 4.34% | 44,326 → 45,479 | 18/20 |
| 1729 | 34,346,812 | 35,483,626 | 3.31% | 44,471 → 45,357 | 18/20 |

The actual serialized delta is +1.14–1.48 MB in these twenty-graph cohorts. These are uncompressed in-memory serialized sizes, not compressed save files or disk IO latency. All twenty loaded graphs were distinct replacements and matched the declared tile/owner-ID/blueprint/position/pool/field projection. The projection is not every stat, gear or goal; separate save fixtures own those checks. This does not measure the worst-case all-eligible-world memory footprint.

## Review bounds

Q1: paired arms restore independent fixtures, global state and cohort identity; save/load validates replacement rather than old references. Q2: exact allocation positive control governs both arms; private .NET allocation success does not override failed native validation. Q3: branch state and exactly one actor End/energy are asserted, but these 32 samples are not a stress campaign or significance test. Q4: native status is now complete; private hash/stub measurements remain separately labelled. No gameplay, renderer or settings change follows from this interpretation. No further native run is requested.
