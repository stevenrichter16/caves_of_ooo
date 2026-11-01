# Ordinary Spread field-row presentation

Status: bounded native and actual Editor verification complete, 2026-11-01;
source publication, sustained performance and packaged delivery remain pending.
Root owns Unity, capture, tests and publication. This is V1 of the
[whole-game execution plan](GRAPHICS-EXECUTION-PLAN.md): reuse approved native art
for existing ordinary field owners. No crop mechanics, new planting stages, geometry,
loot, population or approved PNG changes are in scope. The focused/regression run
passed196/196; actual body/soil, reader, ordinary harvest and fresh Continue were
observed. Independent source and visual review reported no new P1/P2 findings.
This does not accept the whole area, broader shadows or packaged/hardware execution.

## Defect and baseline

The actual Editor capture [field-before](Verification/GraphicsExecution/field-before.png)
shows repeated black-backed glyph strips crossing otherwise native grass. This makes
real worked fields look unfinished and obscures their material identity. Fix their
existing presentation before adding more scenery to solve apparent emptiness.

Root reports the captured game as seed65, `Overworld.10.15.0`, player40,12, HP40,
with240 field-row owners. The image independently shows40,12 and40/40HP; seed, zone
and owner count require the root's game-state record, not inference from pixels.
The screenshot is actual gameplay, not a concept or detached catalog render.

**Saved-baseline discrepancy:** the retained
`Verification/GraphicsExecution/field-before-save/Quick.json` describes
`Overworld.10.11.0`, turn1480, saved2026-10-28T21:36:59.8238940Z, HP40.
Its companion `Quick.sav.gz` is a binary save. Root confirmed this is the10.11 entry
checkpoint; the10.15 screenshot followed real ordinary travel, but the attempted
freeze/copy did not capture that later state. Retain the pair as the entry fixture,
not an exact reload of10.15. Root replayed the route and verified the later harvest
save timestamp/zone before fresh Continue. The original metadata remains unchanged.
The cause was identified: a save loaded through `FlatPixelGreyboxMarker` writes after
F5 to its embedded GameID directory, `a2e4390711a54b869eced598fbfbbac0`, rather than
the assumed copied entry path. Resolve the actual save destination before preserving
a later checkpoint; do not modify metadata to manufacture agreement.

## Art and rendering contract

Reuse existing immutable16×24 frames:

| Current real owner/state | Foreground | Underlay |
|---|---|---|
| `CropRow` ordinary cut row | `opening-base` frame8 | `opening-ground` frame18, old-field soil |
| `RipeCropRow`, unharvested | `opening-base` frame7 | same field soil |
| Same `RipeCropRow`, harvested | `opening-base` frame8 | same field soil |

The row owns this soil presentation, including legitimate row-only cells. Do not
invent Grass owners. Removing the row restores the actual underlying owner/material.
Harvest preserves the same row and its real layer2; it changes the body through the
existing `FieldHarvestPart.Harvested` state. World and authorized Examine/underfoot
portraits must resolve the same approved body, including when the player covers it.

Implementation lives in
`Assets/Scripts/Presentation/Rendering/FlatPixelSpreadEnvironmentCatalog.cs`:
`IsEligibleFieldRow`, `DryFieldCell`, ground/foreground/reader routing and disposal
of borrowed-art views. Existing current-zone admission remains authoritative; this
does not replace opening, settlement, lair or special-site policies. An adapter
instance may already admit its biome's ordinary sites; that inherited path requires
its own actual evidence and receives no new site-acceptance claim here.

## Source corrections and refusal controls

- Ordinary ripe rows use exactly one owned `FieldHarvestPart`, yield one Emberwheat,
  and retain their blueprint/layer after harvest. A harvested row is not recreated.
- Prepared beds (`CultivatedSoilPart`), `CropPart`, foreign yields, malformed/duplicate
  ownership, unsupported glyph/layer/colour, carried or solid forms keep their policy
  or fallback. No permissive blueprint alias hides altered gameplay meaning.
- Water, oil, mixed coatings or other live tile state do not receive dry field soil.
  Competing visible road/terrain/flowers and duplicate rows refuse ambiguous backing.
- Hidden cells, unexplored cells and hidden owners do not gain native live art.
  A hidden competing owner must not disclose itself by changing a visible field.
- A ground claim can suppress the owner's fallback glyph. Therefore available soil
  with a missing optional body atlas **must not claim the row's ground**. The guard
  now checks both body and soil availability. Missing/malformed soil may still leave
  an available truthful body and portrait; it must not fabricate an underlay.
- Preserve immutable art and existing camera, fog/memory, collision, yields, RNG,
  save format and legacy presentation. No new per-cell allocation or simulation loop.

## Native verification

`Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSpreadFieldRowsTests.cs`
contains the focused positive, reader, harvest, ownership/state, disclosure,
resource and removal controls.

| Receipt | Recorded result | Meaning |
|---|---|---|
| [field-rows-red.xml](Verification/GraphicsExecution/field-rows-red.xml) |16 cases:11 pass,5 expected failures;20:04:24–20:04:26Z |Three ordinary body/soil states, same-owner harvest transition and underfoot reader fail before support. |
| [field-optional-red.xml](Verification/GraphicsExecution/field-optional-red.xml) |1 case,1 expected failure;20:18:58–20:18:59Z |Valid soil plus missing body incorrectly claimed backing and could erase the fallback row. |

The final [field-regression-green.xml](Verification/GraphicsExecution/field-regression-green.xml)
records **196 passed,0 failed,0 skipped/inconclusive**,20:21:31–20:22:03Z.
It covers focused row/reader/resource/state controls plus portable-crop, opening,
berry/hive and signpost regressions. Added counter-cases include missing/malformed
soil, hidden competing owners and negative reader admission. A real harvest also
checks the incremental renderer repaint against a full repaint; the same row changes
body without recreating the catalog. These are native checks, distinct from the
actual play evidence below. Independent source review found no additional P1/P2
issue after the missing-body ground-claim guard.

## Actual Editor result and reproducibility

Root replayed ordinary travel from the retained entry to seed65,
`Overworld.10.15.0`. [After at40,12](Verification/GraphicsExecution/field-after.png)
shows the same240 row owners as native field strips over old-field soil, and
[explicit2× view](Verification/GraphicsExecution/field-after-zoom2.png) checks their
readability. This is a replay comparison, not an exact saved-state pixel match:
player wetness changed from48% in the baseline to46% after travel.

The [ripe-row Examine](Verification/GraphicsExecution/field-ripe-examine.png)
shows the actual native portrait. Ordinary interaction (`C`, `b`, Return) from38,18
harvested real row15995 at37,19; `Harvested` became true and the log reported one
grain. See [harvest capture](Verification/GraphicsExecution/field-harvest.png).
No new harvest or yield mechanic was added. Repeated-harvest refusal and additional
underfoot/unsupported-state cases are native-tested; they are not separately claimed
as new live observations here.

The retained [harvest save metadata](Verification/GraphicsExecution/field-harvest-save/Quick.json)
is `Overworld.10.15.0`, turn1830, HP40, timestamp
`2026-11-01T20:29:09.5080400Z`, GameID
`a2e4390711a54b869eced598fbfbbac0`, with its companion binary save.
Fresh isolated Play and Continue preserved player38,18, HP40 and row15995's harvested
state; see [fresh Continue](Verification/GraphicsExecution/field-fresh-continue.png).
Continue returned to default1× while the preceding view was2×, so no pixel-identical
image or whole-scene restart claim is made.

Reproduction corrections:

- Read `manager.ActiveZone` after travel; `GameBootstrap._zone` can retain a stale
  earlier graph. Use `InputHandler.CurrentZone`/`PlayerEntity` for current interaction
  state; bootstrap player references can also become stale.
- Batched arrow inputs can be dropped. Observe each world-map move before issuing
  the next; do not assume a queued sequence reached its destination.
- Verify actual GameID save directory, saved zone and timestamp before copying the
  checkpoint or claiming a fresh Continue test.
- Keep camera/zoom and status differences in comparison receipts. Do not synthesize
  a matched result by editing the save or image.

Independent source and visual review reported no new P1/P2 findings within this
bounded presentation slice. Grid repetition, the broad sparse center and other glyph
owners remain visible debt. One center view and2× inspection are not whole-chunk
composition acceptance. No population, shadow or full-area acceptance advances.

## Performance, remaining gates and delivery

One observed Editor render-counter snapshot was39 batches,39 draw calls and41 SetPass
calls. It is **not** the required60–90second gameplay profile, has no controlled
before/after baseline and establishes no performance improvement. Execution-plan R11
remains pending; no physical Deck measurement is claimed.

Remaining work:

1. Root publishes exact reviewed owned source/tests/living records and evidence, then
   updates only the appropriate execution-matrix coverage/observation axes.
2. Record a scoped sustained gameplay profile before completing the performance gate.
3. Audit the full chunk through multiple normal-zoom views and traversed routes;
   revisit repetition, dense/quiet patches and sparse family composition separately.
   Broader memory/wet/foreign-state live checks remain bounded by native evidence.
4. Batch the accepted presentation slice into a current package and verify normal
   launch, travel, readers and save/restart. Existing packages do not gain this change
   merely because source and Editor evidence exist.

Verified now: retained RED/GREEN, guarded immutable-frame reuse, actual body/soil and
ripe reader, ordinary one-grain harvest, preserved harvested owner on fresh Continue,
and bounded independent source/visual review. Still unverified: whole-area polish,
new shadow/population work, sustained performance, current packaged execution and
physical Deck. Main publication is pending root at this checkpoint.
