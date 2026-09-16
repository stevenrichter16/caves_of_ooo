# Marrowstye — the window and the weight

Status: complete and installed. All 48 owned native tests pass within WI13’s311/311 focused run. WI14 full regression: 13,762 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 311 additional tests pass.

CoO-original named-place composition. See [the aggregate plan](DROWNED-LEDGER-MARROWSTYE-COMPOSITION-PLAN.md) for source corrections, independent review, visual refinement and exact integration records.

## Exact place and authority

Only fresh Overworld.12.12.0 under current Village/Intake authority is eligible.
The mapped site is Spread tier1, road=true and river=false. The scoped layout
replaces the generic village bottom river with dry intake ground. Other sites,
current graphs, camera/reveal and spawn remain unchanged.

Four unequal semantic wings form an intake complex: a broad receiving hall
with two short masonry partition fingers defining three work bays,
supplies, domestic shelter and a disused wing. Public aisles remain clear and
wide enough for actual hauling; the clerk's offset window faces a reserved
standing frontage. The main well remains at40,12 with open cardinal neighbors.
Floor is native outdoor substrate; RoadStone marks real public approaches
and the receiving court, StoneFloor actual shaded interior, and low
SandstoneWall art remains native masonry. Furnishings use existing owners, including two clerk-side Crates. Three small
border colonies contain six trees and fifteen bushes. The disused wing has a
two-cell real rubble breach and starts without seeded residents; its reservation
does not prevent later native movement into it.

## Native content policy

The late profile preserves two StoneCoffers and one FilerClerk. Two existing
SaltCuredBody owners are deliberately introduced as intake cargo. Coffers weigh
110; ordinary cured bodies90. Both are solid, Handling-enabled, noncarryable
and nonthrowable, without Destructible Parts. Coffers have no ContainerPart.
A model must not invent storage, curing, corpse, toll or refrigerated mechanics.
No PaleCurator is introduced.

Drag capacity is Strength times8: coffer14 succeeds/13 fails, cured body12
succeeds/11 fails. Actual grabbing and movement must preserve both owner
identities, refuse blocked movement atomically, and remove their native speed
penalty on release. A broad aisle does not waive these rules.

The courier parcel is the separate SealedBogTakenBody: weight30, takeable and
NoTrade. The clerk requires an active BogBodyCourier quest and an actual
carried parcel. Delivery consumes one unit, sets the existing delivery fact,
pays25 drams/+10 PaleCuration and completes once. Entering the town or pointing
at ordinary cured cargo does not deliver anything.

## Staging, reservations and lifetime

Base1000 stages and validate real native dependencies before mutation,
then publish shade/reservations. Late profile3860 places exactly its three
owners once under same-zone identity. Arrival3870 protects real cave stairs
before population/drama. The cave placement filter must reject protected cells
and unsafe adjacent arrival space. Shared generic residents/services/stock and
house drama remain authoritative, with scoped reservation exclusion.

A populated or foreign graph rejects without damaging current entities or
publishing an unusable replacement plan. An empty same-instance native retry
stages fresh owners and resets the late lifetime only after successful commit.
Worldgen success/refusal diagnostics explain every gate.

## Verification and limits

Initial tests pin scope, deterministic and seed-varying architecture, final
native frontages, exact owner counts, native haul/courier semantics, malformed
content atomicity, retries and actual stair rolls. Root owns shared routing,
art, isolated Unity validation and final integration. No per-frame or per-turn
generation scans are added. Static previews verify shape/readability and live
owner coverage, not input feel or sustained gameplay performance.

## Implementation log

- Before production: read CLAUDE and aggregate plan; verified physical owner
  Parts, exact map settings, DragSystem and actual clerk conversation. Corrected
  coffer-container and ordinary-body/courier-item assumptions. Authored initial
  native and separate adversarial fixtures; waiting for root RED capture.

- WI04: actual missing-plan/builder compiler RED captured; implemented four
  unequal wings, reserved intake aisles, staged native base, three-owner late
  profile and reference-safe arrival helper. Added exact public cave filter
  with neighbor clearance; a separate manager-wiring RED now checks a blocked
  adjacent arrival while the center remains open. Both owned fixtures initialize
  real loot tables and reset them to avoid inherited partial registry state.

- WI07: native layout/profile/count/frontage and clerk dialog gates passed.
  Two haul cases exposed a harness error: synthetic Speed100 used Stat's
  default Max30. Corrected the fixture to the native Speed range and added
  a pre-grab speed assertion; the documented native44/36 penalties were
  correct and no hauling balance changed. The dedicated cave-neighbor
  manager-wiring test produced its intended RED for the shared filter fix.

- WI09: actual refinement RED captured before changes. Added two masonry
  partition fingers, real clerk-side supplies, three continuous clear hauling
  rows, mapped-road paving, a rubble breach, quiet initial disused wing and
  three bounded vegetation colonies. The hall grew northward so its southern
  wall does not occupy the native main well's standing frontage. New content
  remains staged and validated; no fixture-policy or hauling balance changes.

- WI10/WI11: all three refined Marrowstye previews inspected. The partitioned
  intake bays, paved receiving commons, bounded border growth and initially
  empty disused wing are visible; cargo remains separate from the furniture.
  WI11 reports309 combined cases,308 passed, one unrelated shared art-name
  fixture mismatch, zero C# errors. All46 owned native cases passed.
- Independent cross-review identified a narrow fail-soft preflight omission:
  a visible FilerClerk with a changed glyph could be accepted although its
  exact native renderer rejects that reskin. Added two RED cases for early
  and late corruption, owner/reservation atomicity, and canonical@ recovery
  before changing the native validation. Final result is pending.

- WI12: actual narrow RED captured25 adversarial cases,23 passed and both
  new glyph corruption cases failed, zero C# errors. The shared native
  FilerClerk preflight now requires its canonical@ render identity before
  either terrain or late profile mutation. No conversation, hauling, art or
  generic actor policy changed. Combined targeted/full verification pending.
