# Recovered G2 action capture visual review — 2026-10-24

Independent read-only review of actual 1280×800 Unity EOF captures. No Unity actions, screenshot manipulation, gameplay changes or production edits. Inspected original PNGs A01/A05/A17/A23 and B01/B05/B09/B14, and all 39 indexed metadata samples plus both event records.

- A = `Docs/Verification/Live16x24/G2Action-20261024-131115-920` (24 frames)
- B = `Docs/Verification/Live16x24/G2Action-20261024-131334-237` (15 frames)

## Findings

**The two named layout defects pass this bounded live sample.**

1. A01: actual damage 6 to owner2088 and 2 to owner2071 are visibly separate at logical (39,19) and (40,17). They cannot read as the former false `62`. A05 retains 6,2,3 on separate rows. A17 similarly separates lethal6 and nonlethal1. B09 separates two actual2 events on the same recipient at (39,17)/(39,15), rather than joining as22. No literal1+2 sample is claimed here; the earlier source/test evidence covers that pair.
2. B01/B05: `Stunned` is at x36..42,row15 (later row14), damage3 at x39,row17/16, while Player is40,19. The yellow player and struck Gleaner remain intact and distinguishable. The text no longer paints over the player. Metadata across all39 samples finds no readout groups sharing touching/overlapping spans on the same row, and no readout span covering the player's logical cell.
3. Readout identity agrees with observed attack/damage events. In B01 the player's3 and Warden's6 are separate true events; only3 is displayed alongside Stunned. The additional6 is not visible as a floating label in that congested sample; the right log retains it. Do not claim every damage event is guaranteed a floating number. B09's two2s are direct hit and subsequent bleeding, both correctly attributed to owner2074.
4. Actual walk A/B art is represented for Player, Dog, Scrabbler and Gleaner; attack/hurt art is represented for Scrabbler, and Player attack plus Gleaner hurt in B. B14 has cleared readouts and returned idle sprites. Some preceding entries retain Attack/Hurt enum states during their idle-art tail: an idle-looking sprite alone is not proof the animation state already ended. These sampled stills do not establish smoothness, pacing, audio synchronization, runtime FPS, physical Deck appearance or complete animation coverage. No player hurt/guard/parry sample exists here; player HP remains40 throughout.

## Remaining visual attribution limitation

A17/A23 `Bleeding` belongs to Scrabbler2071, not Player2701. Crowding puts that text at x37..44,row15, visually closer to the yellow player at41,17 than to its recipient at40,20. The player is unobscured and the metadata/log are correct, but **the text alone can suggest that the player is bleeding**. Similarly, B's high `Stunned` uses a distant lane rather than an explicit recipient pointer. This is a remaining attribution/spacing polish concern, distinct from the now-fixed collision bug. A small recipient marker/connector or a closer safe side placement could be considered in a separately tested iteration; do not rewrite approved sprites to solve it. Do not describe the present sample as unambiguous ownership for every floating status in a crowd.

## Acceptance scope

Accept the named adjacent-digit separation and player-occlusion correction in these actual opening combat samples. Preserve this evidence alongside the prior253 native tests. This is not opening-release, six-zone-journey, all-effects, packaged-player, or physical Steam Deck acceptance. No source defect in the preserved actor art is established by this review.

## Severity and smallest follow-up after source inspection

**🟡 P2 — misleading status attribution in crowded placement; fix before claiming clear combat ownership.** This does not invalidate the two narrower overlap fixes and need not stop unrelated ordinary plot capture. It is more than a cosmetic mismatch: `Bleeding` can imply the player should treat their own condition.

At A17 the text spans x37..44,y15 with midpoint(40.5,15). Its recipient Scrabbler2071 is(40,20): five vertical cells away, Chebyshev distance5 (Euclidean≈5.02). Player2701 is(41,17): two vertical cells away, Chebyshev distance2 (Euclidean≈2.06). In native16×24 cells that is roughly120px vertical to the recipient versus48px to the player. The PNG confirms the visual mismatch; neither metadata nor log actually assigns bleeding to Player. At B01 Stunned's midpoint(39,15) is five rows above recipient(39,20), while Player(40,19) is four rows away: less acute, but still not self-evident from floating text alone.

The present code's `CanPlaceEventText` (AsciiFxRenderer.cs:410) rejects a body only **on the candidate row**, then checks preceding text reservations. It does not test for a different visible body between that row and the recipient. Existing columns stay fixed, while lane0/1/2 move the text1/3/5 rows above the owner (plus one-row rise). Shared chronological numeric reservations and body avoidance make higher lanes more common. The pre20d5c54a0 version allocated status-word lanes using only the same owner's other words; ordinary numbers did not reserve lanes and there was no visible-body exclusion. Therefore this is a newly observed consequence of the stronger collision avoidance, not the documented policy of dropping a newest readout when all lanes are occupied. Exact identical-sequence before/after causality is not separately captured; source and present samples establish the risk. Restoring the old collision behavior is not appropriate.

### Recommended minimum change: reject misleading intervening-body placements

Extend the existing **local candidate visibility/body test**, not the renderer or art system:

- For each candidate row, obtain current exact recipient cell (or the existing origin-only receipt cell).
- Examine the bounded rectangle of that text's glyph columns and rows **strictly between candidate row and recipient row**, in addition to the candidate row checked today.
- If a different currently disclosed Creature/Player occupies that rectangle, reject that candidate: the readout would sit visually above that nearer unrelated actor. Exclude exact anchor; preserve current canonical-cell validation and `CanShowEventText` checks.
- Keep the existing three lanes, chronological priority, whole-string rendering and final whole-readout omission. When every lane is misleading, omit the floating text; authoritative log and existing compact status indication remain. No new connector, icon asset, crosshair or distant relocation is needed.
- Apply the same placement invariant to numbers and words so a displaced value cannot become another actor's apparent damage. It is conservative around wide words, appropriately favoring truthful omission over a false recipient.

This costs at most the already bounded text width × at most six intervening rows × three lanes ×32 readouts, with stack locals and existing indexed loops only. No allocation or zone-wide scan. Gate each inspected cell by Explored/IsVisible **before** resolving its visible owner; hidden actors must not change placement. Re-evaluate on current layout updates so movement/rise cannot leave a formerly safe word attached visually to a new foreground actor. Near top-edge rows can be below the recipient because of BaseY's floor: scan the signed inclusive range correctly rather than assuming every candidate is above.

A separate ownership marker/connector would introduce line/body/label collisions, native glyph decisions and additional interpretation; it is not the smallest correction. A global fixed max-distance cap would hide useful uncontested lane2 texts without addressing the actual competing-recipient condition. The local intervening-body refusal fits the living doc's existing promise: “If no truthful nearby placement exists, omit that transient readout.”

Suggested RED/counter boundaries before implementation: observed Bleeding source40,20/Player41,17 with lower lanes occupied; same setup Player hidden (must preserve placement); visible actor outside text span (must preserve); only exact recipient present (all three lanes still work); movement into corridor retires without resurfacing; top-edge signed range; real multidigit still contiguous, adjacent receipts still distinct, warm100-updates zero-allocation remains. This is a proposal only; no tests or production code were changed by this reviewer.

## Narrow attribution implementation cold review — 2026-10-24

Read actual production and test diffs after the native RED receipt (`g2-attribution-red-summary.json`:61cases,50pass,11behavioral failures). The runtime diff is limited to `AsciiFxRenderer.CanPlaceEventText` and private `HasDisclosedActorAt`.

**No remaining P1/P2 source finding in this diff.** Original candidate-row protection remains semantically identical: its helper call passes no exemption, so an owner cannot be painted over near the top boundary. Intermediate rows use strict signed endpoints and exact-reference recipient exemption. Current recipient/finite lethal fallback agrees with `UpdateEventTextPosition`; visibility admission already guarantees a non-null canonical origin. Hidden/explored gates precede top-owner lookup, and canonical-cell plus existing `CanShowEventText` keep malformed/foreign/carried occupants from influencing placement. The change adds no allocation, mutation, new owner state, event schema or combat behavior. Existing chronological reservation and whole-readout retirement remain intact.

The requested foreign-render/carried competitor counters now establish the actor remains top-visible and canonically indexed before asserting unchanged layout, so they specifically protect the reused disclosure predicate rather than passing vacuously. Test geometry, maximum span and signed interval match the living document. Maximum separation6 means at most5 strictly intermediate rows×24columns per candidate, still three lanes and32groups. The source scan is bounded; runtime performance and zero-allocation acceptance depend on the actual GREEN receipt, not this review.

No claim of final GREEN or new post-attribution screenshot acceptance is made here; root is running those gates. The earlier actual-capture P2 remains open until that validation. Broader opening/Deck/whole-release boundaries remain unchanged.
