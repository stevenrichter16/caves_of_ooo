# G3 — Prepared planting-bed readout

2026-10-25 UTC. **Implemented; native and bounded ordinary-play accepted.**

## Problem and scope

An actual ordinary harvest at Nella's second reserve bed removes the Pitchpod
and yields produce/seed correctly, but the surviving decorative `CropRow` wins
target selection. Its historical name is “cut emberwheat row”; cultivation lives
on the Grass below it. Planting still works. Make that current ground context
readable without renaming the row, changing targeting, altering permissions or
adding new art. This is a CoO-original disclosure correction, not Qud source parity.
The accepted native art remains stable; this small text correction reuses the
actual six-site captures rather than generating unrelated concepts.

## Source verification sweep

| Assumption | Verified correction |
| --- | --- |
| Harvest failed or changed the crop species | `sixsite-pitchpod-harvested.json` and exact-cell receipt show the real crop gone and2resin+seed acquired. `CropRow` is pre-existing decorative terrain. |
| The selected row owns cultivation | `SpreadExplorationResidents` adds the marker to actual ground; `WorldInteractionSystem` resolves equal-layer terrain to the later row. |
| Ground readout already handles prepared soil | `CellStatusReadout.Collect` returns when sparse TileState is absent. Cultivation is a terrain Part, not TileState. |
| `CultivatedSoilPart.IsCultivated` proves public disclosure | It checks usable physical soil, not Render ownership/visibility or exact current graph/cell authority. Check the same qualifying soil owner, not two different entities. |
| Existing crops can be detected by an unguarded HasPart | Hidden, stale or foreign owners must not alter public readout. Suppress the extra cue only for a current disclosed physical crop at that exact cell. |
| Paid test access can inherit any editor reputation | The fixture now neutralizes the keeper faction inside the existing restored test scope before payment; this is test setup, not gameplay grant evidence. |
| Reading should refresh growth/rights | No. Shared readouts must not reconcile, grant rights, create TileState, spend time or mark dirty. |

Read: `CellStatusReadout.cs`, `CultivatedSoilPart.cs`, `CropPart.cs`/`CropTime.cs`,
`LookQueryService.cs`, `WorldInteractionSystem.cs`, `ExamineDocumentBuilder.cs`,
existing CellStatusReadout/SpreadCropReadout/ConnectedReserveInput/LookFocusRefresh
fixtures, plus `GRAPHICS-G3-SIXSITE-JOURNEY.md` and its observed harvest receipts.

## Smallest implementation plan

1. Add native tests first: real authorized harvest+pickup on factory Grass and
   CropRow, absent sparse TileState, shared C-description/Look/structured reader.
   Counter-check unprepared, live crop, hidden/foreign/stale graph/cell/parts,
   carried soil, two-owner eligibility mismatch, hazard preservation and read-only
   repeated queries. Root runs native RED before production.
2. Change only `CellStatusReadout`: append **prepared planting bed** after the
   existing hazard descriptions for exact current disclosed usable cultivation,
   when no current disclosed crop occupies it. Preserve all hazard wording/order.
   Do not infer permission, emptiness, moisture or “plant now”. Reuse existing
   dirty notifications; do not add polling, cache or owner mutation.
3. Native GREEN plus affected Look/crop/interaction regression. Independent
   code/counter review; fix significant findings before commit.
4. Reuse an earned legitimate near-bed checkpoint. Through ordinary input,
   harvest, pick up, select the actual row, read it, plant the supplied seed and
   confirm the live crop replaces the bed cue. Capture real1280×800 output and
   read-only state. Keep original checkpoints/saves/preferences intact.
5. Update this living record and release ledger, fetch/rebase and push exact
   owned changes. Inspect storage after remote verification. The separately
   packaged9ffd7c973 build predates this small follow-up; do not silently relabel it.

## Initial review and acceptance bounds

- 🟡 Fix planned: current prepared soil is obscured by legitimate historical row text.
- 🔵 Scope: no new mechanic, asset, crop lifecycle or save migration is needed.
- 🧪 Pending: native RED/GREEN, ordinary input/capture and independent review.
- ⚪ Prior package and physical Deck gates remain separate.

Can verify: exact readout strings, state authority, unchanged yielded owners,
hazard order and no query mutation, plus actual captured ordinary UI.
Cannot infer: all-world art completion, every seed/crop/layout, hardware comfort
or performance from a text cue. No new benchmark or imagegen production is claimed.

## Implementation and native evidence — 02:54 UTC

**Implemented and bounded ordinary-play accepted.** `CellStatusReadout` is the
only changed production file. It checks canonical cell/current attached-zone
authority, then appends the neutral phrase for the same eligible cultivated
terrain owner with owned visible Render/Physics. A current disclosed physical
crop suppresses the extra bed cue. No TileState is created; owner identities,
row name/prose, crop behavior and access rules are unchanged.

Implementation seam:

```csharp
bool preparedBed = HasDisclosedPreparedBed(zone, cell);
if (state == null)
    return preparedBed ? new List<string> { "prepared planting bed" } : null;
// Existing hazard collection remains in its original order.
if (preparedBed) parts.Add("prepared planting bed");
```

| Native receipt | Total / pass / fail / skip | Meaning |
| --- | --- | --- |
| `prepared-bed-red.xml` |25 /2 /23 /0|Before production, missing-cue assertions fail. Real authorized harvest/pickup preconditions succeed. |
| `prepared-bed-green.xml` |72 /72 /0 /0|New25 plus47 existing CellStatusReadout, SpreadCropReadout, ConnectedReserveInput and LookFocusRefresh cases. |
| `prepared-bed-counter-green.xml` |28 /28 /0 /0|Final new fixture, including three additional independent-review hazard-authority counters. |

These are **75 distinct passing cases, 28 new cases**, not100 unique tests.
Results are actual Unity EditMode, not the standalone Roslyn runner. Explicit
AssetDatabase refresh and reflected loaded fixture checks preceded both runs.
A transient MCP503 during assembly reload resolved after reload; no failed
compiler result or stale assembly was accepted.

### Scope details and review

- 🟡 **Fixed:** genuine prepared soil was absent from the UI after harvest
  because the selected decoration did not own cultivation and sparse TileState
  was absent. Shared current-state text now discloses it.
- 🔵 **Deliberate authority tightening:** the canonical/current-zone guard also
  suppresses stale or inactive hazard queries. Live hazard text/order is unchanged.
  Three added tests prove active water+charge text, null on deactivation or
  replaced cell/zone, and identical text after authority restoration. This is
  recorded explicitly rather than claiming every possible query is unchanged.
- 🔵 **Independent review:** `prepared-bed-independent-review.md` found no
  significant production defect. Its requested three-case counter was added
  and passed natively. No helper collection, polling, cache, global scan,
  growth reconciliation or permission mutation was introduced.
- ⚪ **Presentation limits:** no art was changed. This is a measured affordance
  correction on previously integrated art; no new concept/imagegen pipeline
  output or all-world visual acceptance is claimed.

## Actual ordinary gameplay and comparison

Two owned isolated launches use normal `SampleScene.unity`, seed64 and ordinary
C Continue. Both receipts state `forcedPresentation=false`. They load a previously
earned character, not a fresh New Game. Root used actual CUA keyboard actions;
observation scripts only read state and capture Unity's1280×800 output.

1. Continue the original plot checkpoint at18,7, tick1550,40HP,36drams.
   At18,6, ripe Pitchpod7199 overlays cultivated Grass5524 and CropRow5525.
   C→Up→H harvests; C→Up→G→Tab collects2resin plus seed7202. Resin7197 grows
   from1 to3. Tick becomes1570; rights remain Granted.
2. Before production, C→Up shows only “cut emberwheat row”; Examine shows
   historical cut-stubble prose. Export the actual F5 checkpoint, then stop.
3. After production, Continue the exact exported checkpoint. The captured
   zone/player/HP/tick/drams/equipment/inventory/observed objects all match;
   the same5524/5525 owners remain. C-menu now adds the cyan bed line, and
   Examine adds it under CURRENT STATE without rewriting history.
4. Open inventory, select actual PitchpodSeed7202 and choose **plant prepared
   bed (18,6)**. Tick1570→1580; seed is consumed and new crop7203 occupies
   the same soil/row. C-menu loses the extra cue; Examine supplies the seed-stage,
   dry/paused-growth text. The real Look cursor selects Pitchpod at18,6.
5. Ordinary F6 restores the deliberately saved pre-plant checkpoint. Look
   L→period→Up now visibly shows the bed cue in FOCUS at18,6, tick1570.
   This restores the empty saved state; it is not persistence proof for7203.

`prepared-bed-play-summary.json` indexes10 captures with hashes. Nine are
accepted observations. `prepared-bed-unexpected-pause.png` retains a rejected
Look attempt: an extra Escape opened Pause, so it is not Focus evidence.
A mouse inventory-tab attempt also opened an empty equipment picker; root
cancelled and used keyboard Tab. Fast text entry yielded only the `c` filter;
visible row navigation completed the transaction. No perfect-input claim.

Representative actual images:

- [Before menu](Verification/Live16x24/prepared-bed-before-menu.png)
- [After menu](Verification/Live16x24/prepared-bed-after-menu.png)
- [After Examine](Verification/Live16x24/prepared-bed-after-reader.png)
- [Replanted crop](Verification/Live16x24/prepared-bed-replanted-reader.png)
- [Restored bed in FOCUS](Verification/Live16x24/prepared-bed-restored-focus.png)

Original and exported checkpoints remain untouched outside the repo. The new
empty checkpoint is `/tmp/coo-prepared-bed-empty-checkpoint/33db4c719b3b4a5baba1d70a23632cd1/`;
Quick166466bytes SHA256`bc1a22e8f6ddb924ca8f8e125275f7756063725b6c07ffc2c4271caaa1ba3dc0`.
All four file hashes are in `prepared-bed-empty-checkpoint.json`. Final cleanup
restores editor view/scene/preferences, no Play or isolation, and null save-root
override. Before stop the console had0errors/2existing Metal warnings.

**Can verify:** real harvest/pickup/seed consumption, matching owned-state
before/after, correct menu/reader/FOCUS output, crop suppression, normal load,
native ownership/fog/hazard/read-only counters. **Cannot verify:** physical Deck
input/comfort/performance, every crop/seed/region, fresh-character full journey
or this cue in the already-built9ffd7c973 packages. No new performance measurement.

Files: MOD `CellStatusReadout.cs`; NEW `PreparedBedReadoutTests.cs` + fresh meta;
this record, release ledger/continuations, native receipts and actual captures.
Next is fresh-New-Game whole-scene acceptance and measured composition refinement,
not another unrelated mechanics or asset packet.
