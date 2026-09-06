# FLOW1 — crafting selection and mixed-input continuity

Status: IMPLEMENTED AND REGRESSION-VERIFIED; LIVE POINTER DELIVERY UNVERIFIED. Baseline1ef093a1,8470GREEN. Accepted smoothing steps1–2;
step3 retry feedback is next. Existing CoO command/UI contracts, not Qud parity.

## Implemented behavior

Crafting now uses the shared ownership-checked radio command: choosing another blade,
haft, binding or quench replaces that slot; reagents retain multiple selection.
Legacy duplicate exclusive marks normalize at inventory UI rebuild, keeping the last
marked carried item and its exact marker. This matches existing component selection
order; last-quench selection is an explicit repair policy (old duplicates suppressed
quenching). Token-graph tests prove decode itself remains unchanged; no save migration.

One37-line map drives actual rendered row placement and pointer hit testing, including
section spacers. Headers, empty notes, overflow arrows and result area stay inert.
The selected logical row remains visible through long lists; exact-fit lists have no
false down arrow. Crafting owns clicks/hover instead of reaching hidden equipment.
All inventory panels/popups now ignore an unchanged pointer grid so keyboard selection
is not immediately undone. Click handlers independently resolve their current hit.

## Verification sweep and corrections

Read both runtime UI files, ToggleCraftMarkCommand, ForgePart selection, screen-data
geometry, PERF-FOUNDATION and existing inventory/crafting tests. The living smoothing
plan records the source correction table and timestamped implementation log.

- Header spacers invalidate the old logical-row-only scroll calculation.
- Preview selected first while station selected last; reuse radio behavior.
- Existing duplicate marks require normalization as well as future command routing.
- Main-panel and popup hover had the same parked-pointer defect; added3actual REDs
  before extending the guard globally, within the same UI file.
- Native keyboard accepts either input backend. Pointer uses legacy only; queued
  MouseState is not proof that legacy pointer events arrive.
- Batch-quench tests must count units, not entities; strengthened during review.
- Test helper namespace is CavesOfOoo.Tests, not its TestSupport folder name.

Runtime scope is two UI files only. New scenario/driver/launcher and three test files
support the checks. No blueprint/art changes, public save fields or removed mechanic.
The duplicate drawing/scroll geometry and direct mark toggle were replaced, not claimed
as deletion of an unrelated dead system. Two unreachable pointer branches in the new
headless test driver were removed during review.

## TDD, adversarial and cold-eye evidence

Retained initial22RED(20fail), staging30RED(27fail), legacy32RED(21fail), minimum172GREEN,
207GREEN, hover72RED(3actual failures) and final focused218GREEN. First RED equipment
countercheck used a nonexistent Label; corrected to ShortLabel. The pointer compile
attempt's3CS0234loglines are retained; no stale XML was used. New total72 tests:
24regression,40dedicated adversarial,8native staging.

Independent taxonomy/reference review and root cold-eye covered these attempted breaks:

1. Last marked winner versus first/unmarked entries, with exact survivor marker identity.
2. Rebuild/reopen/mode changes and token-graph saved duplicates; multi-reagent controls.
3. Foreign/stale rows and previews refusing without payment.
4. Batch one-quench versus unquenched unit counts.
5. Long lists, section spacers, exact-fit overflow, empty and shrinking contents.
6. Actual rendered selection tile matching the shared hit map.
7. Invalid/inert pointer exit and reentry, headers and result-area boundaries.
8. Parked pointer versus keyboard navigation in both main panel directions and popups.
9. Popup precedence and independent click dispatch after keyboard changes.
10. Warmed unchanged hover:1000calls allocate0bytes and redraw0times; moved selection
    produces one render. This isolated check does not claim total frame allocation0.

No remaining production must-fix found. Observer review fixed premature inner complete
status and inconsistent desktop/global timeout allowances. GUI mouse acceptance remains
unverified, as explained below; it is not hidden behind the passing logical tests.

## Native and honesty bounds

Headless Play uses a real rendered arena, actual six content items and adjacent Forge/
Still. A virtual keyboard drives the production InputHandler/UI.75seconds include25idle,
25navigation and25selection, with observed versus attempted action counts. Further
checks forge the selected Iron, pay exactly its inputs, leave Steel untouched and retain
multi-reagent selection. The final post-global-hover result is recorded below.

Desktop CUA run d97db4f4b22541a4ad956243ffebad2b uses actual N,I,three separately observed
Tabs, SpaceSteel→Down→SpaceIron; no clear or selection mutation intervenes. Screenshot
and state show the replacement and preview. The raw report's first state check passes.
The subsequent pointer phase fails: CUA scroll/right/left-click/drag visibly move the
cursor while legacy Input.mousePosition stays1881,894 (grid79,7), never Oak13,9. Retained
report2observations/1failure,252.649266458s,0C#errors; raw last state and log are preserved.
Cause beyond legacy input delivery remains unproven. The preceding real-input run timed
out during root context recovery before Crafting; two earlier simulated desktop runs
failed tab preflight. All raw failures remain distinct from successful native results.

Can verify: keyboard selection and actual command execution, payment/recipient identity,
script-observed cursor continuity and rendered tile geometry; measured marker workload.
Cannot verify: successful OS mouse ingestion/click in this desktop setup, overall visual
quality or subjective buttery feel. Logical pointer tests adjust a fixture camera around
the existing legacy pointer/directly invoke the click handler; they are NOT OS input
proof. The retained menu observer supports a future working desktop/manual pass. Known
A31Camera teardown is separate; do not infer clean shutdown from state assertions.

## Performance bounds and final results

Pre-change valid run70ad07028b6442f1bda2b43dee8ae350:75.00008825s,360885frames,
70/70navigation,68/68toggles. First invalid capture had120000capacity and omitted detailed
metrics before failure; retained separately. New bounded500000samples record timestamp,
approximate phase and prior completed-frame profiler values. Whole-frame GC includes
editor/harness setup and input. No UI-only allocation or speedup claim.

A craft-only after run8e570fba6cea479e87595c70af5417fa passed20native observations but
predates the global hover extension; preserved under craft-only names. The final capture,
full suite, GUID/ownership audit and comparison follow before commit.

Final headless native24/24PASS,run775c6f2fbb674d21a6700a924b584b5c,
80.540280167s total,75.000108s measured,366913frames,70/70navigation and69/69
toggles; exit0,0C#errors, no logged native exceptions. Input max2.391458ms,
p99.002292ms; inventory render max1.481416ms. Baseline corresponding maxima
1.078208ms/.948875ms: no speedup claimed from this noisy one-run comparison.
Frame/phase means,p99,max are in FLOW1-perf-comparison.json; raw CSVs retained
compressed. Input correctness and bounded idle work are the supported outcomes.

Full8542/8542GREEN,03:53:43–03:55:28UTC,105.0671965seconds,zeroC#errors.
Baseline8470→8542,+72tests. Focused218GREEN and40dedicated adversarial cases.
2374assetGUIDsunique,0collisions; owned paths have0protected-manifest overlap.
Native mouse failure remains explicit above, with no waived or claimed passed pointer
gate. The accepted next wave proceeds while that live verification limit remains
documented. Today's suite7302→8542,+1240tests, not a count of discovered bugs.
