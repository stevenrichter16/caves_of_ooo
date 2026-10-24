# Independent kitchen service state-art visual review

2026-10-24. Reviewed actual `kitchen-service-states-*.png` frames, paired job/tile observations, the native source mapping and current native-test XMLs. No Unity actions, runtime mutation, code edits, cleanup or git operations were performed by this reviewer. This is a bounded acceptance of the two static kitchen state cues in the recorded seed-65 Editor route.

## Decision

**Accept the demonstrated Working-cloth / Ready-parcel / collected-neutral sequence. No actionable P2-or-higher defect remains in the reviewed evidence.** The sequence occurs in the normal flat world and its shared examine reader. It does not accept the entire opening, every regional asset, animation, packaged launch or physical Steam Deck performance.

All eleven state-replay PNGs independently report 1280×800. Each was visually inspected. The `working-2x` image is an actual in-game zoomed capture, not an enlarged contact-sheet substitute; ordinary-scale images remain the acceptance basis.

## Visible state and actual ownership

- **Idle, tick 5110:** repaired pan 7208 has the approved open dark cavity; board 7210 is neutral. Actual tile receipts select original `east-kitchen` rectangles `(80,0,16,24)` and `(112,0,16,24)` respectively.
- **Working, tick 5120:** the pan cavity is replaced by a quiet light cloth while its clay edges remain identifiable. The adjacent escrow and empty pickup board remain visually distinct. The two-times gameplay view makes the new cover particularly legible; the ordinary view also shows the light covered cavity. Tile receipts select `kitchen-service` frame `(0,0,16,24)` only for the paid Working pan. The job started at 5110 and is due at 5230, with actual grain 7233 ×2 and split pulp `6a992cba1f634140a60fda096e27a26c` ×1 held by escrow 7209, paid purses 48/102 and player commissioner 2696. No finished-meal cue is present yet.
- **Ready, tick 5230:** the pan returns to its original open frame and the pickup board gains one recognizable tied parcel. Actual output is FieldMeal 9249 ×1 in pickup 7210, with station/customer/ordinal provenance 7208/2696/1 and emptied escrow. Tile receipts select `kitchen-service` frame `(16,0,16,24)` only for the pickup board. The background court, actors and approved art retain their established form.
- **Readers:** the Ready pan reader correctly uses the open pan portrait and says the meal is in the pickup tray. The pickup-board reader shows the board with the same tied parcel and explains that an empty board cannot make another. The normal pickup menu lists one wrapped meal. These are actual game UI captures, not source-art views.
- **Collected, tick 5260:** parcel overlay disappears from the world board; the actual submitted board returns to `east-kitchen` rectangle `(112,0,16,24)`. Pan is Idle ordinal 1, both trays empty, same meal 9249 now carried by player 2696 with provenance intact. The visible collection log agrees. The neutral board still has a small pale wooden highlight; it no longer has the larger tied parcel silhouette.

The exact native source atlas is 32×24: two independent 16×24 static frames. The source README describes retained approved bodies and the original 52-pixel meal translated by three rows. Gameplay evidence verifies the resulting state choices; it does not itself prove every editable-source construction claim.

## Reload checks

Working before/after F5/F6 job receipts are identical in every recorded field except label and observation time. Collected before/after F5/F6 receipts are likewise identical except those two fields. This covers recorded actor identity/custody, recipe/input/output/provenance, purses, repair flag and job state; it is not byte equality of complete save internals.

Ready after reload is observed later at tick 5250, after two movement steps and readers. Its job differs only in raw remaining-ticks arithmetic; all station, Cook, escrow, pickup and inventory rows are equal to Ready at tick 5230. The later world screenshot and tile receipt retain the ready parcel.

Single-file reopening confirmed both Working and Collected reload frames are fully populated with wall, player, Cook, pan, board, trees and hedges at the restored ordinary zoom. There is no supported blank-world reload defect in these final bytes:

| Exact PNG | SHA256 |
| --- | --- |
| `kitchen-service-states-working-reloaded.png` | `b3041f617151e3c06d87aa75d55487d146021d7f7a8ce298b52aa53a6d509f1f` |
| `kitchen-service-states-collected-reloaded.png` | `8fac9579bd3a45a3180f97cdabcad876d5df41ad29fc523e78febfbc4f91733c` |

An initial concern from a multi-image preview was withdrawn after reopening these exact originals and matching root’s hashes. It is not a runtime finding or an omitted acceptance failure.

## Tests, profile and limits

Independent XML set comparison gives final focused 38 passed / 5 skipped / 0 failed, regression 479 passed / 5 skipped / 0 failed, **36 overlapping passed cases**, hence **481 distinct passed cases**. The five shared skips explicitly report that this native Unity runtime returns zero even for a retained managed allocation. They do not establish zero allocations. Earlier RED and intermediate failures remain separate historical evidence, not the final result.

The profile covers 85.121804 seconds; all five recorder streams have 64,360 samples, capacity 131,072 and `wrapped:false` (frame-delta metadata is 64,359). `COO.ZoneRenderer.LateUpdate` mean is 0.095124 ms, p99 0.175458 ms, maximum 29.699417 ms. Main Thread maximum is 959.0285 ms during a sequence containing reload/tool/capture work. This is one Editor observation, with no controlled prior-build comparison or hardware Deck measurement; it cannot establish a speedup, hitch-free play or console performance. Static images cannot establish animation cadence.

The first gameplay acceptance review separately proves real material acquisition, repair, cooking, post-collection consumption and reopening an empty board. This replay began from that ordinary repaired checkpoint; it does not claim a second fresh acquisition route. Native tests cover counterconditions beyond this play route, but the screenshots do not demonstrate every theft, worker loss, missing resource, hidden owner or blocked-job variant. No whole-release acceptance follows from this service slice.
