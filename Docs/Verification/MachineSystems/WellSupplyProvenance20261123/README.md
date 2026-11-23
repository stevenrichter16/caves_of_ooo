# Separate well supplies and ground spills — November 23, 2026

Bounded runtime correction, native checks and actual GameBootstrap play.
[Contract and policy history](../../../WELL-SUPPLY-PROVENANCE.md).
Independent source/gameplay-evidence review: zero actionable P1/P2 findings;
this is evidence review, not a second native operator.

The old test was preserving an explicit contract in
DENSITY-LIQUID-CARRY-POUR.md. The later uniform well admission accidentally
conflated abstract drawn Stock with exposed LiquidPool water. The fix restores
that distinction in the shared admission used by drinking, skins and flasks.
Attached pools and tile-state emitters retain occupied-cell safety, finite
pool precedence and no fallback. Phase/ice, repair, oath, custody, quantity,
callback and action-time guards remain. No saved fields, new source, tariff,
water benefit, exposure immunity or physical contamination simulation.

## Native evidence

- Corrected REDb:40cases,16meaningful failures,24controls,zero compiler errors.
  The original legacy spill test remains unchanged.
- REDa:40cases,28failures;12 failures were invalid assertions that adding an
  acid pool must create an acid tile coating. Corrected fixtures assert the
  actual pool identity, amount and cell instead. These12 are not bug evidence.
- GREENa:248/249; one remaining newer waterskin callback fixture still applied
  mixed-ground refusal to abstract Stock. It now explicitly supplies a real
  exposed pool and verifies rollback of its3units as well as untouched Stock3.
- Final GREENb:252/252/zeroCS.42new cases are discovered by full names in
  new-test-discovery.json. Adjacent density-liquid, well consistency, well/flask,
  waterskin transactions and natural source-depth classes are included.
- Exposed-safety mutant:43cases,9failures,zeroCS; it is killed. Source in the
  idle clone was restored to all7 exact main pins afterward. No mutant reached
  main or the actual game. The next clone run must import its restored source.

New cases cover finite/endless Stock versus exposed pools, physical acid versus
oil coating, all three consumers, actual non-null acid exposure, independent
late injury, late spills, dry-pool fallback refusal, contradictory emitters and
partial last-unit metered water. Three prior fixtures now distinguish exposed
supplies explicitly; their refusal assertions remain. Native runs overlap and
are not a new full regression or an inferred corrected full-suite failure count.
The retained full a9 run stays40813total/104fail; the91 baseline failure names
and other whole-program gates remain open.

## Actual ordinary actions

Resume the existing isolated earned game8ab6a6384c2a44deb8b3a68303809d8f at4200,
HP360, camp40,9. Normal ascent4210, seven map steps west/seven north4490, descent
4500 reach Overworld.3.4.0. Five northeast moves and one east reach46,7 at4560.
The generated, visible TankBrocchinia17798 at47,6 has Stock−1/no attached pool.
Its ordinary reader is captured. This is a source-guided route, not an
exploration/difficulty qualification.

Inventory selects existing flask2235 with1/12bog-mire. Its northeast-pour preview
is cancelled freely with the complete measured snapshot unchanged. Reopening
and accepting spends that1unit at4570, making PouredLiquidPool19577/mire1 on the
plant's cell. The abstract supply remains Stock−1; occupied-cell purity is false.
The ordinary C menu still offers direct water and a grouped fill menu. Preview
and cancel again preserve the complete measured snapshot at4570.

The fill submenu separately identifies well-water draws and ground-mire fills.
Native arrows select the exact empty2235:4570→4580 fills12water, keeps source
Stock−1 and mire19577=1, and leaves every other flask unchanged. Native direct
drinking stays4580/energy1000 with the complete measured snapshot unchanged.
F5/archive-before-Stop/fresh Continue4580 exactly preserve measured source,
ground owner, flask contents, all inventory owners/quantities, slots, selected
stats and player position/HP/energy/weight.

After fresh Continue, native C/FillLiquid selects2231 containing11mire.4580→4590
recovers exactly1, bringing2231to12 and retiring only19577. The12water in2235
and sourceStock−1 remain. This is finite material recovery, not purification.
F5 and a second fresh Continue4590 again exactly match the measured snapshot.
Both separately archived four-file checkpoints match checkpoint-hashes.json
and recovery-checkpoint-hashes.json; tar archives preserve both states here.

## Evidence boundaries and preservation

Eight PNGs use real end-of-frame native captures. Synthetic production InputSystem
keyboard events drove Continue, movement, Inventory, C menus, readers, actions,
cancel and F5; no direct gameplay calls, grants, teleports or clock changes.
Inherited HPmax400/Strength70/carry1050, stocked items and modifications remain
fixtures. Actual water source is endless and spill is bog-mire. Finite/metered,
acid/oil, exposed-source refusals and callback scenarios remain native-test
evidence. Unlike pouring into exposed water and pouring onto solid wells remain
refused; neither rule was weakened to stage a demonstration.

An initial import/startup branch had null nonserialized bootstrap fields; it
was excluded without gameplay input or saving. Stop, completed import and clean
scene reload preceded the successful branch. The next start was interrupted;
on retry its actual isolation token/root and initialized player were recovered
before native Continue. One read-only observer failed compilation by calling an
internal query; it supplied no gameplay evidence. Corrected read-only observers
record the actual states. No second native operator, package-input, controller,
hardware-performance, NPC survival or full-machine acceptance is claimed.

Native keyboard and original input settings were restored; main stopped, root
null, scene clean, profiler off, original7built-in views/custom0/selected0.
Fresh checks preserve all84 foreign baseline files and861 selected normal files.
No whole-directory or byte-exact preference-plist restoration claim. Every app,
project, export, normal save and foreign handoff is retained. Current Mac delivery
is recorded separately in the continuation. All12 complete-machine gates stay
open; graphics cancelled, capstan/cultures parked, Deck deferred, hourly active.
