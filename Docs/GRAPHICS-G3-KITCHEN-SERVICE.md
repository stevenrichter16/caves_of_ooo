# G3 — ordinary paid kitchen service

2026-10-24. **Current status: bounded ordinary service and native state-art
acceptance complete.** The historical audit below is retained. Root completed
the real ingredient→repair→paid batch→pickup→reload→consumption route, then
replayed the new cloth/parcel art through ordinary controls. Final selected
Unity evidence:481 distinct passes,0 failures,5 unsupported-allocation skips.
This closes one G3 service slice; the full graphics release remains open.

## Historical starting plan

The next useful delivery is a real paid batch from ingredients to a collected,
readable meal. Existing neutral tray art is sufficient to exercise this gate.
Capture that loop before deciding whether additional service-state art improves
it. This is a bounded G3 service gate, not completion of G2, G3, or the release.
The visual reference remains the established Qud-like flat 16×24 direction; no
new Qud mechanic or parity claim is introduced.

## Verified corrections and current checkpoint

| Initial assumption | Current source/evidence |
| --- | --- |
| Seed65 can commission immediately after the court visit | It has2 ClaspbeanPulp and50 drams, but its pan is unrepaired and it carries neither FireClay nor Emberwheat grain. `working-court-65-saved.json` and later `incidental-actors-65-continued.json` agree on these facts. |
| The previously repaired pan applies to this save | The earlier east/kitchen repaired-pan evidence is another checkpoint. Do not transfer that repair to seed65. |
| EmberwheatSeed or toasted grain satisfies the batch | `KitchenBatchPart.SelectInputs` requires exactly2 **Emberwheat** and1 **ClaspbeanPulp** from real carried custody. The2 carried seeds are not grain. |
| Buying from Orven alone supplies the recipe | Initial `WaysideCookStock` contains only1 raw Emberwheat. Existing trader restocking is a separate mutable system; do not force or rely on rerolling it for this finite route. |
| The glade surface chest contains repair clay | `ReferenceGladeBuilder` supplies only HealingTonic, Torch and DriedMeat there. The guaranteed original2 FireClay are in the **cellar** supply crate. |
| Neutral tray artwork means empty or ready | Frames6/7 deliberately make neither claim; existing tests keep them unchanged after arbitrary FieldMeal insertion and locking. Pan frames4/5 mean broken/repaired, not idle/working/ready. |
| Existing 3D state selectors can be copied directly | `ConnectedSpread3DLibrary` uses looser content/job predicates. They are reference behavior, not sufficient authority for new native commissioned-job cues. |

The preserved checkpoint tree is
`/tmp/coo-working-court-65-checkpoint/c757c38ed9c4452583230acac209cb85`.
Restore it only through the existing owned save-isolation flow. The older
`working-court-65-saved.json` records tick1310/player50,8; the later ordinary
`incidental-actors-65-continued.json` records tick1440/player54,0. The actual
checkpoint `Quick.json` independently records Turn1440, kitchen12.11 and40/40HP
(saved19:56:46Z). The earlier screenshot receipt is not interchangeable with
that save metadata. Re-read the resumed position and inventory before controls.

Known seed65 service identities from both receipts:

- Cook7190 at51,6; pan7208 at51,4; escrow7209 at52,4; pickup7210 at53,4.
- Pan `Repaired=false`, batch `State="Idle"`, both tray contents0.
- Inventory contains ClaspbeanPulp7227×2 and ClaspbeanSeed7229×1 from the
  already harvested kitchen crop. It also contains2 healing tonics,2 dried
  meat, a watering grimoire,4 gladroot seeds and2 emberwheat seeds.
- The real solid Farmer on the cot is an actor, not a blocked-court defect.
  The paid batch does not require the cot; use ordinary waits nearby.

Source: `KitchenBatchPart.cs:135–180`; `LootTables.json:3170–3201`;
`ReferenceGladeBuilder.cs:16,78–83`. The actual
[pan menu](Verification/Live16x24/working-court-65-pan-menu.png) visibly offers
`patch the batch pan (2 fire clay)` and no paid batch while broken.

## Finite ordinary route from seed65

1. **Get grain and clay in one return trip.** Walk north from kitchen12.11 to
   east12.10, then west to the glade11.10 through actual open terrain. Existing
   seed65 east evidence records ripe rows at53,19 and53,21, but a live grazer
   can deplete them; do not promise their current state. The glade has three
   authored finite rows at44,10;46,12;50,14. Examine/harvest two still-ripe rows
   using `C`, direction, `harvest` (`h`). Each gives1 Emberwheat, packs it if
   capacity permits, and leaves actual cut stubble. Check the pack or any
   overflow on the row. Preserve these raw grains instead of Cooking them.
2. **Recover the original2 clay.** Reach glade stairs28,19 and descend with
   `Shift+Period` (`>`). Cellar arrival is40,12. For seed65, the generated
   supply crate is66,17 and its original contents include2 FireClay. The
   longer southern service passage runs via40,19; the beam at48,19 has a
   permanent detour via46,22→50,22→50,19. Follow the current visible passage
   to the store, interact with the crate from an adjacent cell, choose
   `open` (`o`), and take the clay using the actual item row/Enter. Return
   to40,12 and ascend with `Shift+Comma` (`<`). This route avoids the direct
   corridor sightline; it is **not a safety guarantee** against the real
   moving guard. Do not consume this clay on the glade well first.
3. **Return and repair.** Return east then south to the kitchen. Use the
   court's rear aisle to reach51,3, immediately north of the pan, adapting
   to current actors. `C`, Down, `patch the batch pan` (`r`) consumes2
   FireClay. Capture the repaired title/portrait and ordinary `Current
   State`; do not rename, repair, or reposition anything through tools.
4. **Commission.** Reopen the pan from the adjacent cell. The action is
   `prepare field meal (2 grain, 1 claspbean, 2 drams)` (`b`). Select the
   displayed action and release the key. Success physically escrows2 grain
   and1 pulp, transfers2 drams to Orven, and sets `Working` with
   `DueTick=StartTick+120`. If no purchase occurred, player drams should
   change50→48 and carried pulp2→1. Verify exact actual diffs rather than
   assuming that aggregate display implies success.
5. **Observe work and collect.** Examine the pan while Working. Return to
   normal gameplay and advance ordinary turns with short `.` taps;120
   world ticks is about12 normal-speed turns, **not120 seconds**. The start
   action already advances time. Use the live countdown, not a blind long
   held key or forced clock. At completion the pan describes one wrapped
   meal ready. Approach pickup53,4 from53,3 or54,4, `C` toward it, `open`,
   then Enter on the actual FieldMeal. A one-item pickup closes the popup,
   pays a normal turn and reconciles collection. Reopen to verify empty;
   repeated pickup must not mint another parcel.
6. **Read, retain, then optionally eat.** Inventory→select the wrapped
   field meal→Examine/F1 exercises its existing native item portrait.
   Save/reload before consumption to preserve output identity/custody and
   Idle/empty trays. `eat (3d4 healing; stop bleeding)` consumes one actual
   meal even at full HP. Full-HP eating proves consumption, not a healing
   amount or bleed removal; do not invent injury for the screenshot.

These are source-backed destinations and controls, not an already-executed
route. No old seed64 coordinates are substituted. Cached depleted owners stay
depleted; no regeneration, stock grants, copied repair state or debug movement.
If the cellar clay or glade grain was actually consumed in the resumed graph,
use the real alternative chain rather than a fresh save: inspect Orven's
current raw-grain stock; or plant the carried EmberwheatSeed and water through
its real70 moist-round growth. Nella's reserve Marlroot can yield2 clods to
prepare into2 FireClay after the actual rights/growth gate. Those are longer
ordinary routes and must be recorded as such; neither is silently pre-satisfied.

Relevant source: `ReferenceGladePlan.cs:96–98`; `FieldHarvestPart.cs:19–33`;
`FieldHarvestService.cs:9–55`; `GleanersDistrict.cs:15–20`;
`GleanersCellarBuilder.cs:60–78,99–119` (seed65 crate66,17 also appears in the
two-seed census); `OverworldZoneManager.cs:160–164` passes the actual WorldSeed;
`InputHandler.cs:688–708,954–960,990–1005,3347–3366,3419–3455`;
`PickupUI.cs:165–191`; `FieldMealPart.cs:12–57`.

## State authority and presentation design

`KitchenBatchPart` already owns the real job. Keep it the authority; do not
infer a job from generic tray stock or add a parallel presentation timer.

| Visual/readout claim | Required truth and limits |
| --- | --- |
| Broken/repaired pan | Existing owned unique structural RepairablePart with `clay-batch-pan` and actual `Repaired`; unchanged native frames already cover this. |
| Working/covered batch | Exact current configured station and owner refs; valid Working ordinal/start/due/commissioner; exact input references/IDs/counts and actual escrow custody. Due time alone cannot complete it. |
| Waiting/blocked | Actual Working `BlockedReason` such as pickup-full or meal-unavailable. Contents, fuel, oven heat and an invented cook animation are not substitutes. |
| Ready commissioned parcel on board | Exact current board/station relationship, Ready, actual matching Output/OutputID, FieldMealPart, single-unit custody in Pickup, and commissioned provenance. An unrelated FieldMeal placed there is not this completed job. |
| Collected | The actual Output left the board; ordinary reconciliation resets the job. `OwnsCommissionedMeal` is explicitly historical and can remain true after transfer, so it cannot alone authorize an on-board image. |

The pan's `Describe()` already reports the recipe, countdown, blocked state
and ready parcel in both examine routes. The tray descriptions explain escrow
interruption and actual pickup; Open shows empty/count and the loot popup lists
the real parcel. `CanStart` hides the unavailable batch action rather than
showing a disabled missing-ingredient reason. The recipe remains discoverable
through the pan reader. These observations are enough for first acceptance.

If the actual loop demonstrates poor visibility, the smallest subsequent art
slice is a **working-pan cue and a commissioned-parcel-on-pickup cue**, retaining
all current neutral frames as fallback. Capture the genuine states first,
then follow the existing imagegen/native-source pipeline. Do not generically
decorate full containers or label a neutral tray “empty.” No art is authorized
or produced by this audit.

There is no public pure tray→job visual query today. A later implementation
should expose a narrow read-only semantic snapshot on the existing batch and
reuse bounded renderer observations; it must not call `Reconcile`, generate
owners, or scan the whole zone per sprite. Resolve current graph/cell/visibility
before live stock. Hidden state remains hidden. A completed commissioned parcel
can survive the Cook's death: do not impose a living-worker requirement on its
ready cue merely because starting/finishing work needs a worker. The exact
remaining station/pickup/output relationship still matters.

Lifecycle source: `KitchenBatchPart.cs:92–124,224–281,293–355,378–405`.
Committed start/ready/cancel/collected call `Changed`, which dirties the pan,
escrow and pickup. Generic `ContainerPart.AddItem/RemoveItem` and failed
completion's BlockedReason assignments do not independently dirty those
render cells. Any new contents cue therefore needs actual transfer/partial
popup retirement coverage, not just direct selector calls. Preserve pure
readers and the existing boundary reconciliation (`InputHandler.cs:1242–1243`).

## Acceptance receipts and any later TDD slice

Capture real normal UI plus paired observational receipts at: repaired+eligible;
Working after payment; ready before pickup; pickup row; carried meal reader;
empty board/Idle after collection; saved/reloaded; optional consumption. Record
current owner IDs, State, ordinal, start/due/current tick, exact input/output
IDs/custody/counts, player/Cook drams, and repaired flag. Keep camera/mode/seed
with screenshots. A neutral world frame must not be presented as visual proof
of the internal job transition; pair it with its actual reader/contents.

Existing source fixtures to reuse only if runtime changes become necessary:
`ConnectedKitchenTests`, `ConnectedKitchenIntegrationTests`,
`ConnectedKitchenAdversarialTests`, `ConnectedKitchenCallbackExceptionReviewTests`,
`ConnectedKitchenCallbackLifecycleReviewTests`, `ConnectedServiceQueryTests`,
`ConnectedChoiceReadoutTests`, `ConnectedSpreadInputTests`,
`KitchenPanReadoutTests`, `FlatPixelEastKitchenTests`.

For new state cues, write native RED first:119 ticks remains Working; full
pickup delays completion without eating inputs; actual completion makes one
owned parcel; partial/last take retires the submitted cue without a full redraw;
arbitrary FieldMeal/foreign refs/stale graph/reskin/hidden owners stay neutral;
save/load preserves actual job; Cook removal after completion does not erase
the real parcel; repeated Examine/menu/portrait calls change no ticks, money,
stock, job ordinal or identities. Missing optional art keeps the neutral peer.

## Review and handoff

Source sweep by `/root/projection_cold_review`; independent read-only state/UI
audit by `/root/projection_cold_review/look_focus_draft_review` on2026-10-24.
No actionable defect was established in the existing paid-service path by this
audit. The review supports exercising it before expanding presentation.

- 🔵 Corrected checkpoint/material/seed-versus-grain assumptions above.
- 🧪 Real paid service, visual readiness/readability, save/reload and meal use
  remain pending. Existing implementation tests are source references here;
  this audit did not rerun or count them as new acceptance.
- 🧪 No hardware, frame-cost or whole-journey claim. Future state-cue invalidation
  and post-worker-death authority require their own tests if implemented.
- ⚪ Other forge, crop-family, motion/defence and release gates remain separate.

Files changed in this audit: this new plan only. Root chooses the next ordinary
session after the current incidental-actor milestone is closed.

## 2026-10-24 continuation — bounded service cue TDD plan

**Status: tests authored; native RED pending.** This append records a new
presentation slice without rewriting the historical source-only route above.
Root has now reported an ordinary repaired pan and paid Working commission:
player50→48 drams, Cook100→102, actual escrow2 Emberwheat+1 ClaspbeanPulp,
due5230. Ready/pickup/use proof remains the parent's separate live acceptance
until its receipts are recorded. This author has not operated Unity.

The new optional resource is `Sprites/Live16x24/kitchen-service.png`,32×24:
frame0 covered paid pan, frame1 pickup board with this job's actual parcel.
Both preserve their approved existing bases. All18 existing furniture/product
frames and the approved Cook remain unchanged. No flame, staff activity,
stock fullness or access-permission cue is implied. Existing pan5/board7 remain
neutral fallback for absent optional art or absent valid job state.

The source sweep is `/tmp/coo-kitchen-visual-projection-audit.md`, independently
reviewed for predicates/index semantics by
`/root/projection_cold_review/look_focus_draft_review`. Corrections before code:

| Premise | Required correction |
| --- | --- |
| `State` or any tray meal establishes the visual claim | Working needs exact configured owners, valid paid-job records and physical recipe escrow; Ready needs actual unique Output custody plus station/customer/ordinal provenance. |
| Existing InputsCurrent alone proves an exact recorded recipe | Duplicate recorded references/IDs can repeat one real row while omitting another. The pure visual query must prove a distinct2–3-row bijection and exact2grain+1pulp totals. |
| Bound can be shared by every state cue | Bound requires the Cook still grounded; a completed commissioned parcel survives Cook removal. Ready uses the remaining current station/pickup relationship, output custody and historical provenance without requiring current staff. |
| Calling existing helpers on every frame is allocation-free | RecipeContents creates a HashSet, LiveStructure an array, and OutputStored a capturing LINQ predicate. Use bounded loops for the pure presentation query; leave transaction behavior unchanged. |
| Adding a TryResolve overload is automatically compatible | Existing tests reflect the original name without parameter selection. Keep `TryResolve(Entity,out Tile)` neutral and use a distinct `TryResolvePresentation(Zone,Entity,out Tile)` entry point. |
| A take always dirties the board immediately | A partial TakeFromContainer can leave its centered popup open before turn reconciliation. Actual parcel removal must retire the submitted cue even while State still says Ready. |

Planned minimum runtime changes, only after native RED:

- `KitchenBatchPart`: one pure documented presentation query with None,
  CoveredWorking and ReadyParcel. No reconciliation, ticking, factory calls,
  currency or save mutations. Find the unique authority through the existing
  zone part index, with current reference/cell/owned-part rechecks. Its first
  query lazily seeds the index with one scan; warmed queries do not world-scan.
- `FlatPixelEastKitchenCatalog`: third independently optional borrowed texture,
  cached2-frame tiles/sprites, distinct zone-aware entry point. Old two-texture
  construction deliberately passes null and never loads the new resource.
  Preserve render/physical/foreign-reskin guards and independent disposal.
- `FlatPixelGreyboxCatalog`: use the new entry point for current visible world
  owners and authorized portraits. Memory takes the old neutral semantic path,
  never live job contents. Extend existing observed-owner reconciliation only
  for painted exact pan/pickup owners: current cell/FOV/top-owner before stock,
  suppressed coverage without stock reads, bounded old/new cell invalidation,
  quiet no-op stability, lifecycle clear and no portrait registration. Reuse
  the existing pre-dirty-decision renderer call; no new renderer, world scan,
  inventory event framework or gameplay timing change.

New fixture: `CavesOfOoo.Tests.FlatPixelKitchenServiceTests`. Its positives use
actual factory owners, real Configure/TryStart/completion/transfer and submitted
Tilemap frames, with no manually assigned Working/Ready positive. Tests cover
119-versus120 and blocked capacity; exact payment/escrow; partial transfer before
reconciliation; Cook removal after Ready; false State/unrelated meal; malformed
input/output provenance and custody; actual portrait purity; current graph/cell,
fog/coverage/removal; cached borrowed optional resources and old-constructor
compatibility; warmed observation allocation. The first RED must be the fresh
nonzero native run, not source inspection or stale test discovery.

Use existing FlatPixelEastKitchenTests and ConnectedKitchen family fixtures as
regression controls, plus relevant observer and examine groups. Actual resource
binding and later world screenshots are separate from synthetic/lifecycle proof.
No gameplay, native test, full-G3, GPU-performance or hardware acceptance is
claimed by this tests-first appendix.

### Service cue implementation checkpoint (before native GREEN)

Root's fresh `kitchen-service-red.xml` recorded25 cases:2 passed,23 failed,
zero skips, establishing neutral old artwork versus the required real job cues
and the missing independent third-texture constructor. The frozen first
implementation touches only KitchenBatchPart, FlatPixelEastKitchenCatalog and
FlatPixelGreyboxCatalog. It leaves transactions, timing, inputs, currency,
reconciliation, generation and the existing renderer call order unchanged.

The new pure query checks disclosed current subject/canonical cells before job
records, uses the maintained batch-part index, validates a distinct escrow
bijection, and separates Ready output custody from Cook availability. The
catalog keeps the old semantic API and old injection resource-free. The shared
world/examine route uses optional state art; the existing observed-owner pass
refreshes painted pan/pickup signatures with covered/hidden retirement and
bounded old/new-cell dirtiness. This is not a new simulation or renderer.

The original25 tests remain intact apart from an additional two parameterized
missing-pickup/missing-escrow recovery counters added during source review.
Those two are post-implementation counters, not separate behavioral RED evidence.
The27-case native GREEN and independent settled-source review are pending.
The initial RED reaches state-art assertions before the deeper transfer cadence
assertions; do not describe it as an independently isolated stale-transfer RED.

### Native diagnosis and material-matching correction

Root's first27-case run passed26. The one failed assertion incorrectly expected
the pan as top artwork after cancellation: gameplay correctly returns the
remaining grain onto that cell. The corrected fixture asserts actual grain
foreground and the exact lower pan's neutral portrait; rendering priority was
not changed. Independent source review also found CompositionPart.Contains
uses Split, now repeatedly reached by the observed pan.

The strengthened31-case native run passed30 and failed its retained4096-byte
allocation calibration: GC.GetAllocatedBytesForCurrentThread returned0 for that
real allocation. Earlier reported zero-byte measurements therefore provide
**no allocation evidence on this Unity runtime**. The counter and four dependent
Working/Ready selector/observer measurements now explicitly Ignore on unsupported
runtimes rather than silently passing. Other service behavior tests remain active.

The scoped source correction walks comma-delimited material tokens without
Split/Trim allocation and preserves exact ordinal matching after whitespace
trim. It is shared only by the two presentation call sites; the gameplay
CompositionPart.Contains implementation is unchanged. Ten controls were saved
first for ASCII/Unicode whitespace, comma gaps, substring/case/null rejection
and actual state/world images. They characterize unchanged semantics, not an
independently failing behavioral RED. The final41-case native run is pending;
this runtime is expected to report five explicit unsupported-counter skips.
A source-level allocation finding can be closed by this bounded implementation
review; measured zero-GC/frame or broader performance still cannot be claimed.

Independent final source review by
`/root/projection_cold_review/look_focus_draft_review` closed the Split finding:
the matcher preserves trimmed, case-sensitive comma-token semantics without
per-call arrays, substrings, delegates or collections; both presentation paths
use it, and gameplay matching is unchanged. No additional material source
finding remained. This is source review, not measured zero allocation or live
cue acceptance. Supplemental review: `/tmp/coo-kitchen-service-runtime-review.md`.

### Final native source checks and independent quiet-frame coverage

Root's41-case service run passed36, failed0 and explicitly skipped5 unsupported
allocation measurements. Its21-group regression then passed479 of484, failed0,
with those same5 skips (22:00:43–22:01:11Z). These counts overlap; do not add them
as disjoint tests. Neither run proves zero allocations.

A review found the skipped observer measurement also bundled its no-repaint
assertion. Two separate Working/Ready cases now retain that behavior check
independently: both actual painted owners remain observed through20 real renderer
LateUpdate calls, dirty sets stay empty, FullPasses and CellsResolved do not
increase, and clock/state/submitted frames remain unchanged. They do not call
or depend on the allocation API. This is a coverage strengthening, not a newly
observed production defect; no runtime was changed. Root's focused43-case rerun
and ordinary visual replay remain pending.


## Final integration and acceptance — 2026-10-24 22:10 UTC

The pre-art route is [recorded with actual controls and receipts](Verification/Live16x24/kitchen-service-play-record.md):
glade grain, cellar clay, two-clay repair, two-dram commission,120ticks,
actual parcel pickup, F5/F6 and full-HP consumption. The first grain attempt
ended in a recoverable death; it is retained, not hidden. The second route
used no gameplay grants or debug transport. [Nine derived checks](Verification/Live16x24/kitchen-service-play-summary.json)
and [independent evidence review](Verification/Live16x24/kitchen-service-acceptance-review.md)
accept those bounded mechanics. Full-HP eating proves consumption, not healing.

### Production pipeline and extracted parts

Greybox/composition/controls reuse the accepted real working court. The actual
pre-art Working capture and approved kitchen contact were sent to built-in
imagegen; [exact prompt/concept](../ArtSource/Live16x24/kitchen-service/README.md)
are preserved. Generated soft pixels are reference only. Two parts were
reconstructed as exact native editable pixels: cloth on the approved repaired
pan (58 changed pixels), and the approved wrapped meal translated3pixels up on
the neutral pickup board (52 overlay pixels). No heat, activity glow, inventory
fullness or access-permission icon is invented. The32×24 Point atlas uses two
16×24 frames and one optional borrowed resource. Existing24 source/asset files
are hash-preserved; editable Blender, cells, generator, manifest and source
verification accompany the production PNG/meta.

World and exact-subject Examine share the pure current-owner/custody query.
Observed visible pan/board cells repaint only on signature changes. Arbitrary
job strings/food, forged records, current-graph replacement and hidden owners
do not authorize cues. Completed output survives Cook death; partial take
removes the cue before the next transaction reconciliation. Gameplay timing,
recipes, payment, save and world generation implementations are unchanged.

### Tests and review

Behavioral RED25:2pass/23fail. First GREEN27:26pass/1fixture-assumption failure;
actual returned grain legitimately covers a cancelled pan, and the test now
checks foreground ownership plus exact lower-pan portrait. Follow-up31:
30pass/1counter calibration failure. Unity reports0 even for a retained4096byte
allocation, so five dependent measurements now explicitly skip. Independent
source review found Split/Trim in the new repeated material path; a shared
presentation-only token matcher preserves original Contains semantics without
changing gameplay. Ten token characterizations pass. Two independent quiet
frame checks exercise actual submission without the unavailable GC API.

Final focused43:38pass/0fail/5skip. Regression484:479pass/0fail/5skip,
36 passed cases overlap, yielding **481 distinct passes and5 skips**. See
[kitchen-service-quiet-final-summary](Verification/Live16x24/kitchen-service-quiet-final-summary.json),
[regression](Verification/Live16x24/kitchen-service-regression-summary.json),
[source review](Verification/Live16x24/kitchen-service-runtime-review.md) and
[derived acceptance](Verification/Live16x24/kitchen-service-states-summary.json).
No raw aggregate count is presented as a full-suite pass.

### Actual new-art replay

[Replay record](Verification/Live16x24/kitchen-service-states-play-record.md)
contains the exact route/actions and bounds. Eleven actual1280×800 captures
include [Working2×](Verification/Live16x24/kitchen-service-states-working-2x.png),
[Ready](Verification/Live16x24/kitchen-service-states-ready.png),
[board portrait](Verification/Live16x24/kitchen-service-states-board-reader.png),
[collected](Verification/Live16x24/kitchen-service-states-collected.png), and
[settled collected reload](Verification/Live16x24/kitchen-service-states-collected-reloaded.png).
Working and collected same-tick recorded-field equality survives F5/F6; Ready
retains its exact output across reload and two subsequent movement turns.
[Independent visual review](Verification/Live16x24/kitchen-service-states-visual-review.md)
accepts the bounded cloth/parcel/portrait/removal sequence. Root cross-checked
its source and exact individual capture claims before acceptance.

### Performance and honesty bounds

[85.12s native profile](Verification/Live16x24/kitchen-service-states-profile.json)
records64360 samples, none wrapped. Renderer mean0.095ms,p990.175ms,max29.699ms;
MainThread mean1.319ms,p992.217ms,max959.029ms. Window includes commission,
save/reload,11waits and UI/tool capture overhead; no isolated speedup or Deck
FPS claim. DrawCalls max43,Batches max42. No zero-allocation measurement is
claimed; corrected matcher is source-reviewed, and stable-frame behavior is
native-tested independently.

Can verify: real selected controls, named owner/recipe/currency/custody fields,
submitted16×24 textures, sampled screenshots/portraits, native negative gates,
and restored isolation. Cannot verify here: every serialization field, all
seeds/regions, complete motion/audio feel, hardware controller ergonomics,
packaged launch/performance or full six-zone release.

### Self-review and next action

- 🟡 Closed: false Ready/Working authority, stale partial-transfer visuals and
  repeated Split allocation are covered by implementation and explicit counters.
- 🔵 Corrected: cancellation foreground test, unsupported-GC calibration and
  historical source-only status. The five skips remain visible.
- 🧪 Existing FieldMeal prose says throwing remains available while Handling
  says it cannot be thrown. Recorded in the pre-art review; separate item-text
  repair, not a graphics-release blocker and not silently changed here.
- ⚪ This is CoO-original presentation, not a Qud implementation parity claim.
  Old3D and approved base assets remain reversible. No current build replaced.

Next: prioritize remaining visible Player/actor identity and action feedback,
then close crop/forge and the literal six-site journey before packaged/hardware
release checks. Do not keep adding disconnected prop sheets. Keep this workflow
active across sessions and update the bounded coverage ledger from real evidence.

Files: three runtime classes above; new FlatPixelKitchenServiceTests+meta;
optional kitchen-service.png+meta; editable ArtSource/Live16x24/kitchen-service;
Tools/Live16x24/build_kitchen_service.py; this living doc/completion plan/coverage;
pre-art and final real-Play/source/native/profile evidence under Verification.
