# North surface-water material continuity

Status: bounded native and ordinary-play acceptance complete, 2026-10-26.
Graphics remains the primary release blocker. This is a bounded material
continuity correction selected by independent whole-route review.

## Plan and verified boundaries

Reuse the existing bank atlas for sole live water on exact admitted
`Overworld.11.9.0`. Keep dry North court/field/prepared soil, fog, unknown/mixed
liquids, ice, other graphs, foreground ownership and finite resources unchanged.
No geometry/generation/AI/stock/art-source changes or new atlas are needed.

| Assumption | Source verification and correction |
| --- | --- |
| North already borrows glade banks | FlatPixelOpeningGroundCatalog admits glade,Nella soil,East water; managedNorth is excluded. |
| Current patch is a pond entity | Actual58,17 is PeatBog6905 under a sole water coating. LiquidPool and TileStateSource are separate. |
| Permanent coating implies infinite contents | False. Blueprint pool starts70; source water lease4. Capture actual pool and coating separately; do not infer permanent-coating origin. |
| All North ground should be admitted | False. Only sole livewater; retain early refusal for every dry material. |
| Existing negative tests all change | Only managedNorth EastWater exclusion changes afterRED. Existing unmanagedNorth rejection must remain. |
| Need new terrain texture | Existing editable opening-ground frames0–15 already define the accepted bank language. Reuse exact bytes. |

Qud reference is the established legible16×24 material vocabulary; no new Qud
code/art parity claim. This is original CoO presentation.

## Playable baseline

Root uses normal GameBootstrap with isolated unmodified earned
`nella-earned-north-border65` checkpoint. CContinue at20,0; Right38/Down18
reaches58,16. Two Down actions strike/destroy a hedgerow rather than move;
this actual change is retained. F5 saves the approach. No teleport, health or
inventory grant. `north-water-before-bank-detail.png/.json` record world and
pool/source details. The unchanged earned result is retained at
`Verification/Live16x24/Fixtures/north-water-earned-bank65` before leavingPlay.
Original sourcef5e7c3d79,HP40,42drams. Root stopped/restored ownedPlay beforetests.

## Acceptance sequence

1. Actual capture → imagegen reinterpretation of only the water edge.
2. Extract material mask and existing frame mapping; no concept raster import.
3. Native RED before minimal shared exact-zone/material change.
4. GREEN plus hostile graph/state/visibility/custody counterchecks and review.
5. Ordinary samecheckpoint Continue,matchedcapture,wetcrossing,F5/F6 and return.
6. Preserve source/saves, update bounded ledger, push exactownedchanges.

## Honesty and self-review

Scripts verify selected tile, live owner, liquid quantity, coating values and
state conservation. Actual captures are required for readability, not tests
alone. Freeze/melt and unseen materials may have native coverage only. Physical
Deck/Linux execution, whole-opening completeness and remaining regions are not
accepted by this narrow pass. Packages currently2a8e71044 precede aftermath
and this change. No storage reclamation is authorized.

## Imagegen extraction and original source reuse

[Concept](../ArtSource/Live16x24/concepts/north-water/concept.png) was generated
from the actual current bank capture; [prompt](../ArtSource/Live16x24/concepts/north-water/prompt.txt)
constrains the scene and individual water footprint. Extracted target is simply
quiet olive/silt cardinal margins and teal interior. Reject the concept's
dense speckling, altered HUD and stronger edge contrast. Existing native
`opening-ground` frames0–15 already meet this material target and remain byte-
identical. No generated pixels, new decorative entities or replacement sprite
are imported. Owner mapping is lower sole-water coating→cardinal mask; PeatBog
foreground and finite pool remain distinct. Shared21frame atlas stays cached
with existing point-sampled16×24 sprites, no new atlas/draw pass/animation.

Native RED22/22completed,22expectedfailures before production; retained full
failure list is below the25-entry limit. Ordinary route and all mask assertions
fail on the missing North permission. No production changed until that receipt.

## Implementation and independent review

The production diff adds only exactNorth admission and names the shared
East/North water-only predicate. Both are still subject to managed currentgraph
and biome/POI gates. Existing early dry refusal remains above every field/soil/
road branch. Existing East negative moves from newlysupportedNorth to11.7;
unmanagedNorth rejection stays. No generation, overlay, dirty-hook or source
art change. Independent review of production and22new tests found no material
issue. The existing East renderer expiry/freeze/melt tests exercise the shared
paint hook; North's direct-query expiry case is not labeled full repaint proof.


## Native and ordinary-play results

- Native gate: **22 expected RED failures → 299/299 GREEN, zero skipped**
  across seven fixtures. All 16 shoreline masks, live/mixed/frozen/expired
  coatings, hidden neighbors, graph replacement, biome/POI rejection, dry
  restoration, owner identity and zero warm-query allocation are covered.
- Actual matching Continue starts at North (58,16), HP40 and42drams.
  Before/after preserve every recorded field and all205owners by ID. Down
  crosses the real bog; F5/F6 preserves the full205owner and player record.
- Up18 reaches garden (58,24); Down17 returns to North (58,16), HP40.
  The bank persists. The actual PeatBog6905 still holds70units of water and
  a four-turn source lease; the separately observed surface coating is
  Permanent. Its origin was not established and is not an infinite-stock claim.
- Actual world inputs and captures use normal GameBootstrap, default flat
  presentation and an isolated earned checkpoint. No gameplay grants,
  teleportation, forced health or visibility were used.
- A26.41second editor recorder window includes actual bog crossing and
  walking, plus tool/idle time. 10,183 retained samples did not wrap; draw
  calls ranged to40 (mean39.04), renderer marker mean0.189ms/p99 0.292ms.
  Main-thread max1.126s includes editor/tool stalls. This is neither a
  controlled before/after benchmark nor standalone/SteamDeck performance.

Evidence: [before](Verification/Live16x24/north-water-before-bank-detail.png),
[after](Verification/Live16x24/north-water-after-bank.png),
[crossing](Verification/Live16x24/north-water-after-crossing.png),
[reload](Verification/Live16x24/north-water-after-reload.png),
[garden boundary](Verification/Live16x24/north-water-garden-boundary.png),
[return](Verification/Live16x24/north-water-after-return.png).
Each capture has a paired JSON observation. Independent source, test,
image and parity review found no material issue.

## Final self-review and handoff

- 🔵 The existing foreground teal tilde remains above the bank. This pass
  changes the underlying material only; no new owner silhouette is claimed.
- 🧪 North freeze/melt is not ordinary spell-input proof. The shared renderer
  repaint hooks have existing native East coverage in the299-test run.
- ⚪ This is a small continuity fix, not whole North or opening completion.
  Bounded graph tests use three seeds; ordinary play uses seed65.
- ⚪ No new art or animation was warranted: the approved native bank frames
  and editable `ArtSource/Live16x24/Ground/opening-ground.blend` are reused.

Root restored normal SampleScene, preferences, view and save root. All776
normal save files,374approved art/source files and two unrelated screenshots
retain their protected hashes.30Unity-generated whitespace-only PNG metadata
changes were backed up and restored to HEAD. No storage cleanup was performed;
approximately426.5GiB remains free. The disposable test session was closed
normally; the pre-change earned bank fixture remains available in the repo.

Next: refresh dated Mac/Linux current packages from the reviewed/pushed
source including aftermath and North water. Preserve earlier outputs. Verify
actual Mac Continue, corpse inventory/reader and North crossing/save/return,
then resume remaining cellar growth, secondary crops and regional coverage.
Physical Deck and Linux execution remain unverified.

Files: minimal FlatPixelOpeningGroundCatalog and East exclusion-test edits;
new FlatPixelNorthSurfaceWaterTests and metadata; captured concept/prompt,
earned fixture, paired gameplay captures, native/profile/review/preservation
receipts; this record and the same-commit living coverage documents.
