# Cindercaller runtime source review

2026-10-24. Implementer/self-review: `/root/projection_cold_review`.
Independent reviewer: `/root/projection_cold_review/look_focus_draft_review`.
Root owns native Unity execution and ordinary gameplay acceptance.

Reviewed scope: `FlatPixelIncidentalActorCatalog.cs`, new
`FlatPixelCindercallerTests.cs` and the bounded G2 Cindercaller plan. Runtime
changes add one independent optional texture/cache and exact static role; no
Greybox, Player/motion, admission, gameplay, AI or generation change.

## Correction and native evidence

- Initial source review missed that `AnnouncementDocument.Subject` is internal
  to the runtime assembly. Native import caught the test compile failure. The
  fixture now passes the same known `owner` used to build and validate the
  document, through the existing public semantic API. The failed/zero-case
  invocation is **not** behavioral RED.
- [Corrected behavioral RED](cindercaller-red.xml),
  [summary](cindercaller-red-summary.json),22:30:52Z: **2 failed,0 passed,0
  skipped**. World NativeForeground was false; exact-subject lookup returned
  the same cached glyph. Both assertions preceded resource checks.
- [Focused GREEN](cindercaller-green.xml),
  [summary](cindercaller-green-summary.json),22:35:31–22:35:34Z: **45 passed,0
  failed,0 skipped**. The additional43 counters were added after the initial
  behavioral RED, not independently observed to fail.

## Independent findings

No actionable P1/P2 remained in the final source review. Member accessibility
was checked explicitly: Entity/Zone operations, inventory and ability fields,
portrait public properties and the reflected internal portrait constructor.

The optional third frame preserves Farmer/Grazer behavior and the earlier
resource-free two-texture constructor. Default loading is bounded once per
catalog; generated Sprite/Tile ownership is separate from borrowed textures.
Selection requires the exact Cindercaller and current structural appearance
contract, while neutral body art does not depend on remaining FireMoss, held
Cudgel, mood, HP threshold or cooldown readiness.

The fixture's real movement, reagent removal and unequip calls are consistent
with current APIs. Query repetition preserves the real ability cooldown. World
and authorized-reader rejection use current graph/cell/custody/disclosure;
fixture callbacks, content/global state and owned objects are restored using
the established rendering fixture pattern.

## Bounds

The independent reviewer did not compile, run tests, call Unity or inspect
ordinary gameplay. Native results above are root's raw receipts. Actual visual
appearance, ordinary movement/combat/casting, persistence and broader regression
acceptance remain root's subsequent gates. Cached bounded source selection is
not a measured zero-allocation claim; this runtime's earlier allocation API
calibration was unsupported. No Player defense/Riposte change is included.
