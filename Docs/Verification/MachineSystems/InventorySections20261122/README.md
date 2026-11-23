# Readable inventory equipment descriptions — November22,2026

Bounded current-source Editor qualification: weapons/armor now retain typed
Combat, Protection, Equip slots, On-hit/Conditional effects, Current condition,
Equipment comparison, Handling, Disassembly and About this item sections where
supported. Overview headings use distinct gold uppercase labels and rules;
full-reader headings retain its green uppercase style and spacing. Damage,
Hit bonus and Penetration bonus each occupy a separate row. Armor value (AV)
and Dodge value (DV) each occupy a separate row, including zero/negative values.
Hybrid equipment displays both Combat and Protection.

[All27 actual screenshots](GALLERY.md) · [Browser gallery](GALLERY.html).
These are native GameBootstrap frames, not mockups. The screenshot coroutine
waits for the real end of frame, then uses existing DensityNativeScreenshot.
No image retouching. Earlier direct GameView.RenderView reflection captures
could omit retained Canvas text; those exploratory frames are excluded here.

## Source and native checks

All19 owned source/test pins match main and clone3. Nine runtime files change
presentation/capture only; no saved fields, balance, equip mechanics, stacking
policy, new recipe or authored item is introduced. The generic StateReader
keeps its default behavior; its optional inclusion predicate permits the torch
section to own its validated fuel reading without duplicating generic fuel.
Other state/UseGuides providers remain composable and are captured inside the
same equipment ownership/slot/Part guard. Metadata does not run them again.
The retained overview reuses this capture during paint/search/resize/modal work.
An explicit F1 read refreshes; closing it rebuilds through the established path.

- Compiling native REDa:20 cases,18 failures/2 passes/zeroCS. Sixteen failures
  demonstrate missing sections/provider duplication/stale publication/page
  breaks. Two failures were off-page fixture selection mistakes at narrow/large
  layouts, corrected without a production-navigation change; exclude them as
  defect proof. The2 passes are pagination controls.
- Intermediate GREENb405/412 and GREENc410/412 exposed a dropped UseGuide pass,
  changed legacy label assertions and duplicate torch fuel; corrected. GREENd
  passed412/412 on the earlier joined-row wording. GREENe411/412 had one obsolete
  DV wording assertion after the user's separate-row refinement; updated to
  retain the actual-value check. These runs are retained, not additive.
- Final GREENf:471/471,zeroCS. This includes all20 new UI cases plus existing
  equipment, examine, ownership/callback, dense/retained inventory, free reader,
  natural/displaced/per-location comparison and three corrected regression
  fixture classes. new-test-discovery.json lists all20 newly discovered cases.
  Source/test pin equality excludes a stale source snapshot.

Forty trailing-space diagnostics in retained generated failure XML are recorded
in generated-whitespace-exceptions.json. Raw evidence bytes are preserved;
source/document checks are clean.

The raw broad a9 regression is separate and remains failed: see
[its assessment](../FullRegressionA9DE3D3D9/ASSESSMENT.md). GREENf is bounded
integration evidence, not a new whole-suite run or a claim that all old failures
are fixed.

## Actual game route and save/reload

Native production InputSystem keyboard events drove Continue, I, Tab, item
selection, F1, every page, Escape and text-size controls in isolated SampleScene
GameBootstrap. No direct UI action/equip/reader method substituted for those
inputs. Readback of exact owners/rows/Parts and measured gameplay is recorded
in observations.json. Earlier failed/transient startup probes are excluded;
valid evidence starts only with live bootstrap/handler/player/zone identities.
MCP calls explicitly pin main and guard its dataPath/non-batch mode.

The existing earned game8ab6a6384c2a44deb8b3a68303809d8f resumes at4200, camp10.11.
Dagger2127, ironshod boots2351, modified cloak3c6a92d5471b4e47add2a686e7fca289
and actually worn cloak2342 are the exact subjects. At2315×1189, the pack shows
61 counted rows/66 stacks/210 items, capacity76; the dense list's geometry is
unchanged. Larger-text overview and full details remain reachable. Carried
armor shows planner choices; worn cloak names its actual back slot and states
that this is not a normal hit location. Conditional coatings/attempts retain
qualifications; numerical enhancements are not added a second time.

All ordinary selection/F1/paging/return/resize observations stay at4200,
energy1000,HP360,weight536. Complete measured inventory owner/count/liquid/fuel,
selected stats, slots and position snapshots match before/after. Native F5 is
free and preserves the same measurements. The isolated checkpoint was archived
before Stop; fresh Continue reproduces the exact measured snapshot and reader.
This is selected measured state, not a byte-exact whole-world claim.
The raw fresh snapshot is retained in observations.json. Separately measured
checkpoint-before.json/checkpoint-after.json and the matching ordinary checkpoint
archive make the four-file preservation comparison reviewable after cleanup.

After the ordinary checkpoint/reload proof, a separate disposable test item is
created from VenomDagger plus ArmorPart(AV3,DV1), named **hybrid test dagger**.
It is explicitly a composability/UI countercase, not authored content or earned
acquisition. Native popup Equip(auto) and real equipment selection show both
stat families; inspection/equip remain4200 under existing rules. Its4 reader
pages and compact panel are included. All4 owned checkpoint files remain
byte-identical to the ordinary archived checkpoint: this derivative was never
saved and isolation cleanup removed it.

## Review, preservation and limits

Independent source and gameplay-evidence review is recorded separately. It is
not a second native operator. All27 PNGs are2315×1189; no FPS/fixed-resolution,
physical keyboard/controller, Deck or hardware-performance qualification.
Inherited HPmax400, Strength70/carry1050, supplies and prior modified gear are
fixtures, not ordinary acquisition/economic/difficulty evidence.

Main finished stopped/rootnull/clean scene/profiler off, original7 built-in
Game-view entries/custom0/selected0/no owned view. Fresh84 foreign baseline
files and861 selected normal-state hashes match (Unity analytics/TestResults
excluded). This is not a fresh whole-directory or all-preferences byte claim.
Every existing app/project/export and art source is retained. The current
[Mac fbe candidate](../MacCandidateFBE0742F2/README.md) packages this UI;
packaged UI interaction acceptance remains open.
All12 complete-machine/full-delivery gates remain open; M3c remains last full
delivery. Graphics cancelled, capstan/cultures parked, Deck deferred.
