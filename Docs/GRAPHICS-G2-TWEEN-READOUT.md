# G2 — combat readouts beside moving native actors

Status: native RED, focused GREEN, wider regressions and independent source
reviews complete. Ordinary post-fix sanity is captured; exact original
moving-body/periodic-lethal live reproduction remains unobserved.
Root owns Unity and verification.

## Observed problem

Actual ordinary capture
`Verification/Live16x24/G2Action-20261025-004628-096/frame-18.png` and its JSON
show the dying MarlbackScrabbler11286 at logical `(3,20)`. Its real lethal
damage receipt is one point, correctly owned by that NPC and placed at `(3,19)`.
Player2695 has already moved logically to `(4,19)`, but its visible Walk frame
still occupies the departing `(3,19)` position. The number briefly paints
over Player even though Player remains42HP.

This is a presentation-occupancy gap. Do not change damage, event recipients,
simulation movement, finite lethal origin, fog admission, or approved art.
This is original CoO presentation work; no Qud-engine parity is claimed.

## Verification sweep

| Premise | Source-checked correction |
| --- | --- |
| Damage belongs to Player | Capture metadata identifies NPC11286; PlayerHP remains42. |
| Actors always occupy their logical cell visually | `AnimatedEntityRenderer.FlatPixel.MoveFlatView` keeps the current displayed anchor and interpolates a normal adjacent move over0.1seconds; logical movement is immediate. |
| Existing readout collision checks include animation | `AsciiFxRenderer.HasDisclosedActorAt` only checks the logical cell's top visible actor. The candidate-row and attribution-corridor rules already share this method. |
| A new screen-space label renderer is needed | Supplement that existing occupancy query with the current native walking body's bounds. Keep the existing three same-column lanes and whole-receipt retirement. |
| A stub callback proves integration | The RED fixture must use the actual ZoneRenderer-created actor renderer and FX coordinator. An omitted production binding must remain a failure. |
| Every tracked body can affect layout | Only a current, disclosed, enabled native walking view qualifies. Hidden/invalid/foreign/suppressed views and non-overlapping bounds cannot affect placement. |
| Equal entity IDs exempt an actor | The attribution exception remains exact object reference; a different actor with an equal ID still obstructs. |

## Minimum implementation after native RED

1. Add one optional, borrowed presentation occupancy query to `AsciiFxRenderer`.
   Existing standalone construction and logical-cell checks remain unchanged.
2. Implement a read-only query in `AnimatedEntityRenderer.FlatPixel.cs` for
   enabled valid native Walk bodies intersecting the requested visible cell.
   Use the actual current body bounds and world tilemap geometry, including
   partial overlap. Merely touching a cell edge is not overlap. Check the
   supplied zone against the actor renderer's current graph, the queried cell's
   disclosure, current owner/top/render/physics validity, active presentation,
   and visible body. Do not discover actors by scanning the simulation zone.
3. Bind the query once where `ZoneRenderer` constructs the native actor renderer.
   Use it through the existing candidate-row and attribution-corridor checks.
   The row excludes no owner; the corridor exempts only its exact recipient.
4. Retire a whole readout when no truthful lane exists. Never move its horizontal
   attachment, defer it until movement finishes, resurrect a retired event,
   change its logical recipient, or change tween duration to conceal the issue.

## Test and review gates

Existing `FlatPixelActorMotionTests` is the integration seam: its actual
ZoneRenderer, actual native actor presenter, ordinary MovementSystem call, real
ApplyDamage receipt, and submitted FX tilemap establish the geometric invariant.
The existing fixture borrows approved idle resources and injects a 256×24
synthetic motion texture; it does not prove motion-image pixels or ordinary play.

- Initial RED: Player leaves `(8,9)` for `(9,9)` while its actual native body
  remains over `(8,9)`; a one-HP NPC at `(8,10)` dies. Its truthful one-point
  receipt must not paint over or beyond that body. HP and logical cells remain
  unchanged by layout.
- Paired controls: finish the tween before the new damage; disable native motion;
  or reach an exact cell boundary without overlap. A newly emitted number must
  remain visible in its original column when there is no visual obstruction.
- Partial horizontal/diagonal/vertical overlap; exact-owner corridor exception;
  a different owner with the same ID; hidden/foreign/stale/current-zone changes;
  presentation and component visibility; whole words and multi-digit values;
  retired receipt does not replay after the moving actor leaves.
- Retain all `GroupedDamageReadoutTests`, rectangular actor projection, native
  motion, event/status-text and FX lifecycle controls. Add a warmed actual-query
  allocation check after production implementation, without measuring test
  reflection/collection helpers.
- Independently review both the minimal runtime diff and fixture assumptions.
  Then ordinary play must record an actual moving neighboring actor and a real
  receipt; a synthetic fixture is not live input or visual acceptance.

## Performance

Follow `Docs/PERF-FOUNDATION.md`: reuse tracked native views, bind the delegate
once, no LINQ/new per frame, no zone scan or permanent occupancy snapshot.
The existing readout cap is32 and lane count3. Query only when those bounded
candidate checks run; only moving native views need supplemental work.
Profile actual play before claiming a performance result. The new allocation
test explicitly skipped in both native GREEN runs because this runtime's
counter did not observe a known allocation. No measured zero-allocation or
performance improvement is claimed. Source review found no per-query managed
collection/delegate creation, but that is not a substitute for measurement.

## Scope and honesty bounds

The earlier action-readability document explicitly left smooth intermediate
movement outside its one-logical-cell collision claim. The new actual capture
now justifies this bounded extension. No new art, imagegen, capture framework,
gameplay changes, animation timing, arbitrary legacy-model occlusion, or broader
UI redesign is needed. Preserve all existing exact-owner and fog/cell rules.

Can verify with tests: finite receipt attribution, current native body geometry,
submitted glyphs, unchanged HP/cells, lifecycle, and bounded query behavior.
Cannot verify from those tests: ordinary visual timing, every seed/model,
capture-free performance, packaged release, or physical Steam Deck behavior.

## Implementation log

- Source audit and actual frame18 comparison complete.
- Test-first additions added19 cases to the existing native motion fixture.
  Independent source review found no concrete fixture/API blocker. Root's
  native run at01:10:58–01:11:03 UTC recorded56total,49passed,7intended
  behavioral failures,0skipped. Exact per-case failures are preserved in
  `Verification/Live16x24/tween-readout-red.xml` and
  `Verification/Live16x24/tween-readout-red-summary.json`.
- Only after that RED, added the optional read-only `MovingBodyOverlapsCell`
  query, its current native Walk-body implementation, and one-time binding in
  ZoneRenderer. Existing logical occupancy, recipient/origin, visibility,
  whole-label retirement, lanes, and actor motion remain unchanged.
- Added four later query-authority controls (hidden queried cell, foreign zone
  argument, disabled body, superseded top owner) and one warmed allocation
  check. These five are review counters, **not independently RED**. The new
  allocation check first observes a known allocation and explicitly skips when
  the runtime counter cannot measure it; a zero-reading stub is not evidence.
- Focused native GREEN, 2026-10-25 01:14:55–01:15:00 UTC: **61 total,
  60 passed, 0 failed, 1 explicit allocation-counter skip**.
  `Verification/Live16x24/tween-readout-green.xml` and its `-summary.json`
  preserve the results.
- Wider native regression run, 2026-10-25 01:16:10–01:16:55 UTC:
  **286 total, 285 passed, 0 failed, 1 explicit allocation-counter skip**
  across 11 fixtures, including the focused 61 cases. Do not add the focused
  count to this total. The suite covers native motion, grouped damage,
  ASCII/world FX, status/combat readouts and Riposte.
  `Verification/Live16x24/tween-readout-regression.xml` and its `-summary.json`
  preserve the results.
- Independent production review by `projection_cold_review`, including a
  separate delegated geometry/authority/lifecycle check, found no actionable
  P1/P2 issue. It verified current body bounds after actor Update and before FX
  LateUpdate, reference-only corridor exemption, current owner/graph/parts/top
  and fog validation, callback lifecycle, and strict edge contact. Root's
  separate runtime-diff review also found no concern. These are source reviews,
  not additional test runs or ordinary visual acceptance.
- Ordinary replay at01:18–01:24UTC used the unmodified earned prepared
  checkpoint, normal SampleScene, C Continue, actual directional keys, En garde,
  melee and F5. Twenty-four EOF captures and two actual combat events exported
  without errors; observers and profiler were disposed before stopping.
  The first strike killed Scrabbler11284 for15 at2050; subsequent walking began
  after its death. Player remained42HP. Thus ordinary combat/motion sanity
  passed, but the exact former periodic-lethal overlap did not recur.
- Independent PNG/metadata review found no new significant visual issue.
  `tween-readout-play-summary.json` indexes raw evidence and bounded results;
  `tween-readout-independent-review.md` explains why the new moving-body branch
  cannot be claimed as visually exercised by this replay.
- Raw profiler observation lasted197.7seconds; the131072-sample ring wrapped.
  Retained renderer samples averaged0.0683ms, p990.1298ms, max28.8ms; draw calls
  stayed38. This tail includes idle/tool/EOF overhead and does not establish
  overlap hot-path cost, baseline improvement, capture-free timing or Deck
  performance. Keep its full units/count/max data in `tween-readout-profile.json`.
- F5 checkpoint exported to `/tmp/coo-tween-replay-checkpoint`, byte-verified,
  before cleanup. This untempered replay branch is distinct from the accepted
  forged/crop checkpoint. Stop/cleanup at01:24:42UTC restored SampleScene,
  preferences, view and null save override. Direct console:0errors,2 existing
  Metal warnings,56logs. No release or physical device acceptance is claimed.

## In-phase self-review

- 🟡 **Observed misattribution cue addressed:** the actual lethal NPC number
  crossed Player's departing body despite correct logical ownership. Seven
  pre-production behavioral failures are now green; geometry supplements the
  existing logical checks without modifying damage or motion.
- 🔵 **Existing contracts preserved:** the query is optional and bound once;
  hidden/foreign/invalid or suppressed bodies cannot affect layout. The exact
  recipient exemption applies only to the attribution corridor. Retired events
  do not replay when an obstruction moves away. Existing grouped-readout and
  FX tests remain green.
- 🧪 **Allocation measurement unavailable:** the calibrated check skipped for
  an observed zero-reading counter, not a passing zero-allocation result.
  Existing unrelated uncalibrated tests were not changed.
- 🧪 **Exact live case remains unobserved:** ordinary approved-art combat and
  walking passed, but its different lethal strike avoided the original
  periodic-lethal overlap. Native fixture geometry is the specific fix proof;
  do not call this exact ordinary visual acceptance or repeatedly hunt RNG
  instead of completing the connected journey.
- ⚪ **Deliberate bounds:** rectangular native flat-grid Walk bodies only;
  arbitrary legacy model silhouettes, rotation and a general screen-space label
  engine remain outside this observed defect. The five later review counters
  were added after implementation and were not independently RED.

## Files

- NEW `Docs/GRAPHICS-G2-TWEEN-READOUT.md`: living plan and acceptance limits.
- MOD `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelActorMotionTests.cs`:
  real native movement/readout integration regressions and paired controls.
- MOD `AsciiFxRenderer.cs`, `AnimatedEntityRenderer.FlatPixel.cs`,
  `ZoneRenderer.cs` under `Assets/Scripts/Presentation/Rendering/`: optional
  native body-overlap supplement and one-time binding, with no new assets.
