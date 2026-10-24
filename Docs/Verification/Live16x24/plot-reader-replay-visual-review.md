# Plot product reader replay — independent visual/evidence review

2026-10-24. Read-only review of the seven `Docs/Verification/Live16x24/plot-products-reader-*` PNG/JSON pairs. No Unity calls, tests, gameplay changes, repository edits, or staging were performed. This review writes only this scratch report.

## Result

No actionable P1/P2 was found in this replay. Both reported UI defects now have actual ordinary-input visual evidence of correction: the Look FOCUS changes from the harvested crop to the current products, and inventory Examine opens the structured native portrait reader for all three observed seeds. The clean final world capture and receipts prove one acquired Pitchpod seed was planted without player displacement.

## What the pixels establish

- **before-harvest:** Harvest menu and FOCUS identify the actual ripe PitchPod at (18,6), with the player at (18,7).
- **focus-after-harvest:** With the cursor still at (18,6), FOCUS now identifies `PitchPod seed`; contents list both resin owners and the seed. The removed crop is absent. The duplicate resin lines truthfully represent two distinct loose owners.
- **resin-ground:** The structured reader identifies the exact lower ground subject `PitchPod resin` and shows its amber native sprite and authored use text. The separate cell FOCUS correctly remains `PitchPod seed`, the top current owner. This distinction is not the old stale-focus defect.
- **seed-carried:** Inventory now opens a complete `PitchPod seed` reader with its native olive/amber seed, description, planting/use instructions, weight, and explicit no-turn reading footer. This is actual popup evidence, unlike the historical inventory-only `plot-products-loop-seed-carried` capture.
- **emberwheat-seed:** The carried `emberwheat seed (x2)` reader shows the narrow golden grain, description and stack details.
- **gladroot-seed:** The carried `gladroot seed (x4)` reader shows the pale bent seed, description and stack details. `CandyCarrotSeed` is the underlying blueprint; the player-facing gladroot name is expected.
- **replanted:** The unobscured world shows the planting log and new planted mark at (18,6); the player remains (18,7). The reserve cord is still visible. No additional southwest step appears in these observations. This supports the reported 25 ms ordinary-key replay only, not every input device or timing.

All seven files are **1280×800** PNGs. Some JSON `editorReportedScreenSize` fields are 2321×1139; those describe the editor's reported size, not the captured PNG dimensions. The portraits have clean integer pixels, distinct product silhouettes, readable prose and no observed clipping or filtering defect.

## Exact foreground palette/geometry check

Compared every opaque pixel from each source frame in `ArtSource/Live16x24/plot-products/plot-products.png` against its screen portrait, with integer replication and no color tolerance. Each has an exact match (coordinates are the screen origin of the entire transparent 16×24 frame):

| Capture | Source frame | Integer scale | Frame origin | Matching opaque screen pixels |
|---|---:|---:|---|---:|
| resin-ground | 5 | 3× | (687,145) | 333 |
| seed-carried | 6 | 4× | (936,254) | 240 |
| emberwheat-seed | 2 | 4× | (936,272) | 192 |
| gladroot-seed | 1 | 4× | (936,272) | 208 |

This verifies displayed foreground color and shape in these four actual readers. It is not a claim about all source frames, transparent background pixels, live frame timing, or performance.

## Receipt reconciliation

All seven receipts retain seed 64, `Overworld.11.8.0`, flat presentation, player (18,7), 40 HP and 42 drams.

| Observation | Tick | Relevant state |
|---|---:|---|
| Before harvest | 990 | Original reserve PitchpodCrop stage 2/moisture 0 at (18,6); no carried Pitchpod goods |
| After harvest | 1000 | Crop removed; two loose PitchpodResin owners and one loose PitchpodSeed at (18,6) |
| Ground resin read | 1000 | Inventory and all 198 observed owner rows match the preceding receipt as multisets |
| Carried seed read | 1010 | Ground products absent; inventory has PitchpodResin ×2 and PitchpodSeed ×1 |
| Emberwheat seed read | 1010 | Same inventory and all 195 observed owner rows as carried-seed receipt |
| Gladroot seed read | 1010 | Same inventory and all 195 observed owner rows as preceding read |
| Replanted | 1020 | PitchpodSeed removed from inventory; exactly one new PitchpodCrop stage 0/moisture 0 at (18,6); resin ×2 retained |

The three carried reads preserve CandyCarrotSeed ×4 and EmberwheatSeed ×2 and spend no turn. Harvest, pickup and planting each advance the recorded tick by 10. The new crop has no original `reserve-crop` role in the observation; this is a newly planted owner, not the removed authored owner restored. The receipt comparisons establish equality of the recorded fields/multisets, not identity of objects absent from the receipt schema.

## Acceptance scope and remaining work

This replay closes the actual stale Look focus and generic carried-seed reader gaps and accepts native portraits for PitchpodSeed, EmberwheatSeed and CandyCarrotSeed. It also reaffirms ground PitchpodResin examination and the acquired-seed planting loop. The previously reviewed rain/growth, Marlroot clod preparation and save/load evidence remain separate observations, not repeated in this prefix.

Remaining ordinary product coverage is still explicit: CandyCarrot/gladroot root had an earlier world auto-yield observation but no exact product-reader/pickup/use evidence here; MarlrootClod has earlier ground-reader/preparation evidence but no carried portrait here; MarlrootSeed has earlier custody evidence but no exact portrait/planting here; InertSludge remains unobserved. These seven captures do not establish all eight products, all G2/G3 graphics, all biomes, Steam Deck, or mouse behavior.

Root reported focused native GREEN 57/57 and final regression 127/127 with zero skips. This reviewer did not execute those suites and does not add their counts because overlap has not been independently established. No cleanup receipt is among these seven pairs; the historical loop cleanup is not reused to assert this replay's current editor state.
