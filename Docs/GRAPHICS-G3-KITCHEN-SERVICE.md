# G3 — ordinary paid kitchen service

2026-10-24. **Status: source/capture audit and next acceptance plan only.** No
runtime, art, saved graph, or gameplay state changed in this packet; no Unity
tests or Play were run by its authors. Root still owns the current ordinary
actor acceptance session. Keep the accepted working court and approved sprites.

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
