# Prepared-bed readout: independent source review

2026-10-25 UTC. Read-only independent review of the prepared-bed plan, `CellStatusReadout.cs` diff, new `PreparedBedReadoutTests`, existing cell-status characterization tests and the authority/crop/fixture helpers. No Unity control, production edits, staging or deletion was performed. Following the initial read-only review, the coordinator requested the three-case test addition described below.

**Verdict: no significant production defect found in this narrow change.** Ordinary input/capture acceptance and the affected regression completion remain the coordinator's gates; this source review does not substitute for them.

## Authority, visibility and current ownership

`CellStatusReadout.cs:83` rejects fogged, foreign and replaced cells before any state read. Lines87–90 reject attached managers whose active or cached current zone is no longer the queried instance. Detached standalone zones remain supported, matching existing hazard fixtures; no static global world fallback is introduced.

`HasDisclosedPreparedBed` first checks the actual gameplay cultivation rule, then independently requires owned visible Render, CultivatedSoil and Physics on the **same** canonical terrain owner. Tag, anchor, takeable/solid and carried/equipped exclusions are preserved. An unrelated visible CropRow cannot lend visibility to hidden cultivated ground.

The crop suppression path requires owned visible Render and Crop plus `CropTime.IsCurrentOwner` and the exact current anchor. That helper checks the real crop tag, current part, physical custody, anchor membership and non-takeable/non-solid Physics without reconciling growth. Hidden, foreign and moved crop references cannot suppress the neutral cue. Wording does not promise access, moisture, emptiness or immediate planting rights.

## Hazard compatibility and query cost

Existing coating deduplication, longest duration, residue/energy/cloud order and formatting are unchanged. The new cue appends after hazards; an absent sparse TileState can now produce the legitimate cultivation-only description. The broader canonical/current-zone guard deliberately removes stale/inactive hazard disclosure as well as the new cue, rather than changing live hazard text. This is a source-visible behavior narrowing and should remain explicit in the living record.

One small countercheck was recommended and, at the coordinator's request, added as three parameterized cases: an attached active zone containing a real coating should first expose its exact hazard line; deactivation or canonical cell/zone replacement should hide both query forms; restoring authority should restore the identical line. Existing new authority-negative cases contain no TileState, so they demonstrate cue privacy rather than directly pinning this hazard narrowing. This is a test-strengthening addition, not an observed runtime failure. Its native rerun is recorded separately by the coordinator.

New helper work is bounded to the selected cell: cultivation eligibility and one owner pass, with reference/dictionary checks. No global scan, allocation of helper collections, polling, cache or event publication was added. `CropTime.IsCurrentOwner`, `WorldLocationContext.For` and `Zone.GetEntityCell` are read-only lookups. The existing output list/string allocation remains; no performance improvement or device benchmark is claimed.

## Test assessment and evidence

The new fixture exercises real factory Grass/CropRow, authorized reserve payment, actual harvest and pickup, then confirms the original owners/prose/permission and yielded items remain intact. The cue is verified through summary, Look, examine document and action-menu status surfaces. Authority negatives begin with a successful cue to avoid vacuous rejection; the two-owner visibility/cultivation countercase is particularly relevant. Visible crop suppression has an invisible-crop counterpart, and repeated queries check growth stamps, money/rights, dirty callbacks and sparse-state writes.

Fixture cleanup uses the existing `DensityLootTestScope` / `HotbarSaveFixture` chain, which snapshots the changed world/render/factory globals and restores them. Native tests remain query/transaction evidence, not proof of human controller input.

Independently read saved NUnit roots:

- `prepared-bed-red.xml`: **25 total, 2 passed, 23 failed, 0 skipped** before production; matches the missing-cue expectation.
- `prepared-bed-green.xml`: **72 total, 72 passed, 0 failed, 0 skipped** after production, including the selected companion cell-status coverage. This reviewer did not run Unity and does not count those as 72 new tests.

Current published Linux package uses runtime commit `9ffd7c973a82d1b0fa351300f2b981d703694885` and predates this follow-up. This review does not imply that the new cue is already in that archive, that every graphics milestone is accepted, or that physical Steam Deck behavior was tested.

Coordinator follow-up: the three added cases passed in `prepared-bed-counter-green.xml` (28/28 final fixture,0fail/skip);75 distinct native cases across both GREEN runs. Actual menu/reader/FOCUS and seed-replant evidence is separately indexed in `prepared-bed-play-summary.json`.
