# Readable inventory density — November 22, 2026

Baseline a040c7809/runtime7c3bb8a57. First functional layout increment: actual
44 item entries at1280×800 and66 at1920×1080, versus10 at both before, with
measured final10×20px body glyphs at both targets. Optional125% pack text gives30 entries at800p.
The original800p20px text-height reference is retained. The former fitted1080
UI enlarged text with the whole panel; capacity gain partly comes from fixing
physical size on larger windows. Old pinned-aspect geometry also changed horizontal
projection, so no identical before/after physical glyph-width claim is made.
Existing art, item commands, custody and time rules remain. Category buttons,
additional liquid/treatment state badges and sorting are deferred; the whole
redesign and all twelve complete-machine/delivery gates remain open.

## Source and native tests

Two columns use an item-only ordinal adapter over existing carried/equipped rows,
preserving exact owners and the other tabs. Up/down crosses pages; left/right
stays within the page. Page movement preserves column then nearest valid row.
Home/End reach first/last; Tab/T still switches modes. Existing search remembers
selection, F1 keeps full names/readers, and removal chooses the nearest owner.
Resize refreshes geometry before pointer dispatch without moving selection under
a parked pointer. Modal paint/hits share one transform and own their wheel.

The header reads current hard capacity (negative means unlimited) separately
from the Strength soft limit. Only generated final stack suffixes are removed;
striker charges and genuine numeric qualifiers remain. Rounded huge QTY/WT cells
are marked~; exact values and combat comparison remain in selected/F1 details.
Pack text enlargement via-/=, A-/A+ and command menu does not enlarge other tabs
or fitted action popups. No new save fields/preferences or per-frame lists.

Corrected primary REDb27:26fail/1control; review REDc3 found4 failures/1control
for ragged pages and too-short details panes. Projection REDf3:2fail/1control.
Final focused GREENi160/160, expanded projection GREENj4/4: **161 distinct cases,
41 new and120 existing**. Three j cases strengthen/replace the same i cases;
j adds a fourth large-text control. Their added default scale parameter changes
NUnit names; do not count these as seven different projection cases. No full
suite rerun or hardware qualification. Relevant retained tests still check real
ownership, quantities, comparison, fit and free browsing, rather than old fixed
lower-panel geometry. Early invalid fixture/helper runs are excluded.

The first native1080 PNG exposed clipping despite66 model rows: gameplay camera
aspect was pinned independently of target size. Root-rect compensation also
lagged on same-size aspect changes. Final layout derives horizontal compensation
from current camera aspect and pixel bounds; it does not change the camera.
Only actual layout/scaler invalidation forces canvas settlement. Expanded tests
and final native800/1080 establish four content corners exactly at target bounds
and physical10×20 glyphs. Custom projection matrices are outside this claim.

## Actual normal-input qualification

The earned workshop archive at1580, Overworld.4.9.0,40,12 has69 combined owners,
75 rows, HP400, hard5000/soft1050. These are disclosed controlled-fixture values,
not ordinary acquisition/capacity or survival balance. Native GameBootstrap
Continue, keyboard handlers and real Inventory actions were used; no runtime
grant, teleport, direct command invocation or tick jump.

Baseline before800/before1080 and corrected after800/after1080 show10→44/66.
Same69 IDs, target corners and physical glyph size were observed. Resize retained
backing state/document and owner; native tests also pin no provider requery.
Ordinary arrows/page/end, search, two-page striker F1/return and filter clear
retained the intended owners.100 alternating free arrows left tick unchanged.

Warded cloak2342 was found among identically named owners. Popup cancel preserved
actor, twelve flasks and treatments exactly. Unequip/re-equip are existing FREE
actions: Heat35→0→35 and item ArmorPart.AV1→0→1, same two treatments/owner.
An incorrect assumption that Unequip closed Inventory caused follow-up keys to
close it and make one ordinary south move to40,13 at1590; a pending direction
was cancelled. This input error is retained, not reset or counted as paid equip.
Native Throw/Escape on dried meat2129 preserved stock2/tick1590. Native eat spent
2→1 and advanced1590→1600, with HP400 already full; no healing benefit claimed.
F5 saved1600 with exact bottles/coat/Heat35/regard8. Reader and125% text paths
were observed and enlargement returned to default before close/save.

The original1600 owned root was removed by Stop before archival; it has no fresh
reload claim. An interrupted fresh setup contained only its empty boot marker;
no Continue was posted. A separate minimal earned1580 branch repeated native
search/eat/F5 to1590 at40,12, stock1. Its graph was copied BEFORE Stop, then loaded
through a fully fresh GameBootstrap Continue. observations.json records exact
saved→fresh actor/food equality, all12flasks, coat/treatments, Heat35/Armor1,
regard8,69 IDs and44 visible entries. fresh-1590.png is real GameView readback
with only vertical row orientation corrected, no resampling.

## Review, preservation and limits

Independent bounded source review found no remaining P1/P2 after fixes. Final
durable source/artifact/gameplay-evidence verdict is recorded in review.md.
No second operator independently played this candidate. Pointer/controller and
large-population cases have native tests, not physical hardware evidence.
No new actual drop/liquid/camp action qualification is claimed by this UI slice;
their existing command paths are retained and previous machine receipts remain
separate. No universal UI, complete-machine or delivery acceptance.

Three ProfilerRecorder windows were valid/running but yielded zero marker events:
109.449418s idle,142.208918s/100arrows preliminary,107.084265s/100arrows with
non-deep profiler enabled. These are unusable for timing, not0ms results. No
FPS/performance improvement claim. Cache/invalidation behavior is test evidence.
Original profilerfalse was restored; recorders were disposed.

Owned keyboard/settings/callbacks were removed. Initial GameView relative-index
cleanup was a no-op; corrected absolute removal verified only owned7+8, leaving
seven built-ins. Final fresh episode removed only owned7: total7/custom0/selected0.
Main stopped, not compiling, save rootnull, profilerfalse, viewOwnedfalse.
Fresh preservation.json verifies84foreign tracked and861normal selected paths
unchanged. This is not whole-directory/plist byte-exact restoration. All builds,
foreign changes, saves/preferences and parked art remain; no cleanup/staging of
other-agent files. source-pins.json pins14 owned source/test/meta paths.

excluded-clipped-1080.png preserves the preliminary failed viewport; it is not
acceptance evidence. Final images/observations and valid RED/GREEN XML are stored
alongside this receipt. Runtime fd4a638b4 is published; new ../MacCandidateFD4A638B4/README.md
records its universal build/boot. Packaged inventory gameplay remains open. Next: disclosed contents/charges/treatments in rows/search, then
connected gathering/preparation/transport trips per MACHINE-GAMEPLAY-LOOPS.md.
