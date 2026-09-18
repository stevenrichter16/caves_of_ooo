# Morrowfast merchant renewal and native quest cues

Status: implemented and verified. All 60 owned cases pass in CG17/CG18.
CGN04/CGN06 captures verify the corrected cue at the gameplay camera, and
CGN06 records 60 seconds of aggregate native editor performance.

This is the merchant/presentation slice of
[the chunk gameplay implementation plan](CHUNK-GAMEPLAY-IMPLEMENTATION.md).
Qud reference: none; CoO-original integration. No camera, reveal, native quest
rewards, save migration, or general trader eligibility changes belong to this slice.

## Verified contracts and corrections

| Source observation | Implementation decision |
|---|---|
| Morrowfast's Orrit and Sella are Stillcord traders with empty `StockTable`, followed by manual initial stocking. | Declare two exact themed stock tables; preserve existing initial quantities and the factory-less construction fallback. |
| `TraderRestockSystem` already supports genuine non-Villager `TraderPart` owners with nonempty tables. | Leave its eligibility, 300-turn interval, purse floor, and low-shelf threshold unchanged. |
| `TraderPart` rolls initial goods when its static factory exists. | Manual fallback returns when the shelf is already nonempty; do not double the initial economy. |
| The shipped ordinary `Villager` now has `TraderPart`. | The legacy-faction counterfixture explicitly removes this Part to isolate the retained faction/purse path. This was a test premise correction, not a runtime bug. |
| Legacy `QuestBeaconPart` only changes a render-event color. The 3D bodies do not consume that color. | Render a separate owner-bound marker using actual `QuestBeacon.Quest` and journal state. Preserve body color/material identity. |
| Morrowfast uses `Village3DPresenter`; regional voxel chunks use `SpawnRing3DPresenter`. | Integrate the same helper into both presenters. |
| Actual Morrowfast conversation checks require proximity. | Discovery cues do not call the adjacency predicate, and never claim a turn-in is ready. |
| A nonempty unknown quest ID originally produced an available marker. | Require an actual registered quest definition, confirmed by an independent RED countercase. |
| Child transforms inherit actor scale. | Compensate actor scale so small/large bodies do not shrink/enlarge markers; independent RED cases use 0.35× and 2.5× actors. |
| Native captures made the available sign hard to identify. The helper assumed the regional 16×8 palette for the town's actual 8×8 atlas. | Pass each presenter's actual column count; use its existing pale face and dark backing swatches. Do not recolor either palette or add a material/light. |

## Resulting behavior

Orrit's `MorrowfastMenderStock` contains the existing four torches, two daggers,
one spear, one leather armor, three tepuibone, and four fire clay. Sella's
`MorrowfastProvisionerStock` retains eight mushrooms, four dried meat, two healing
tonics, two burn salves, and three water tonics. Buying them out and revisiting
after the native restock interval replenishes the same living merchants. Loading
or registering content does not refill a shelf.

`QuestCueStateQuery` classifies an actual living, visible, nonhostile offering
owner in the player's native graph. A real unstarted quest shows a coarse pale
exclamation with a dark silhouette; an active quest shows a subdued hollow diamond; completed, unknown,
or absent quest context shows nothing. The diamond means ongoing journal work,
not eligibility for a reward. Morrowfast's beacons are Nemm's bell, Hesta's return
notch, and Farra's new dry-goods expedition. Her existing supper dialogue remains
available independently; this slice does not introduce a multi-quest selector.

The marker is a child of the existing actor view, has no collider or rigidbody,
and is created after the actor's native selection bounds are captured. Its
geometry stays separate from ground batching and equipment. It uses the existing
presenter-owned palette/fog material and does not add a simulation light source.
Available/active meshes are built once per presenter and reused across actors.
State changes are checked in the existing actor/view frame loops and normal
refresh, including empty-dirty refresh; there is no world scan or event subscription.
Zone release destroys the helper's meshes while preserving borrowed source assets.

## Verification and self-review

- **CG05 actual RED:** 92 selected cases, 56 passed, 36 failed, zero C# errors.
  All 25 cue cases failed before implementation; nine merchant cases failed,
  including the subsequently corrected Villager fixture premise. Two unrelated
  field-art failures belonged to the parallel slice.
- **CG07:** all 25 initial cue cases passed. The merchant slice passed 17/18;
  its sole failure was the false assertion that `Villager` lacked `TraderPart`.
- **CG09:** the merchant slice and original cues passed. The dedicated cue
  adversarial sweep confirmed four REDs: unknown/non-quest definitions and two
  inherited-scale cases. Those fixes passed CG10 and subsequent combined runs.
- Two additional tests now check that moving/copying a Morrowfast-only giver to
  another scene cannot advertise its local conversation there. **CG10** captured
  both actual REDs (combined run: 221 cases, 218 passed, three failed, zero C#
  errors; the third was the parallel Vennit guidance case). The query now checks
  the exact authored Morrowfast scene/owner; generic quest givers retain their
  existing portable behavior. **CG11** verified both transplant corrections
  GREEN; the exact-ID comparison also avoids per-frame string concatenation.
- **CG10** also verified the four CG09 registry/scale corrections and all 18
  merchant cases. Five new metadata GUIDs are unique across `Assets`; the loot
  file parses, has unique table names, and defines 15/19 fixed starting units.
- The owned suite initially contained 58 cases: 18 merchant, 25 initial quest-cue, and
  15 independent cue adversarial cases. Root controls combined run receipts;
  **CG13** passed this slice while exposing nine other integration failures.
  **CG14** passes all 354 selected cases after those fixes; the repeat full
  comparison and rendered acceptance are recorded by the root.
- 🟢 Native purchase, stock ownership, exact initial amounts, timing boundaries,
  same-owner state changes, explicit visibility, hostility, removal, and cleanup
  have focused coverage. Legacy 2D beacon color behavior remains intact.
- 🧪 Repeat verification of the camera-driven refinement remains required.
  Mesh/lifecycle assertions do not prove screen-scale
  readability, appearance in motion, or real profiler timing. Reuse and absence
  of a world scan are source-verified; no zero-allocation runtime claim is made.

## Files in this slice

- Merchant-only and beacon lines in `MorrowfastContent.cs` and two surgical
  additions in `Content/Data/Loot/LootTables.json`.
- New `Gameplay/Storylets/QuestCueStateQuery.cs` and
  `Presentation/Rendering/NativeQuestCueViews.cs`.
- Small hooks in `Village3DPresenter.cs` / `SpawnRing3DPresenter.cs` and a corrected
  explanatory comment in `QuestBeaconPart.cs`.
- `MorrowfastMerchantRestockTests.cs`, `NativeQuestCueTests.cs`, and independent
  `NativeQuestCueAdversarialTests.cs`, with copied metadata and fresh GUIDs.

Root owns combined runs, shared expedition/guidance integration, final review,
and commits. This subtask does not launch Unity or alter the player's save.

### Final full-suite comparison

CG14 passed all 354 targeted cases. CG15 completed with 14,726 passed and
32 failed, zero C# errors. Its exact 32 failure names match the pre-change
CG01 baseline; no new failures remain. See the central implementation report
for rendered acceptance and its limits.

### Native camera review and palette correction

CGN02's actual 1920×1080 screenshots showed an identifiable active diamond,
correct disappearance after completion/loading, and unclipped journal and travel
notes. The available exclamation was difficult to distinguish against Morrowfast's
olive ground. Source and texture inspection found a specific atlas mismatch:
`ArtSource/Village3D/build_scene.py` exports an 8×8 atlas, while the helper used
the regional 16×8 layout for both presenters. Its supposed town amber sampled
RGB `(24,56,62)` instead of a light sign.

Two new cases in `NativeQuestCueAdversarialTests` load the actual borrowed
material's palette, measure a pale face/dark backing, require visible silhouette
margins and a bounded footprint, and retain the active diamond/material controls.
**CG16 actual RED: 42 total, 40 passed, exactly these two failed, zero C# errors.**
The correction now passes atlas columns explicitly (town 8; regional 16; both
have 8 rows) and uses existing swatches 26/59. Their sampled pale/dark RGB values
are `(226,218,169)` / `(15,18,13)` for the town and `(230,222,172)` / `(14,16,12)`
for the regional palette. Four coarse boxes make a 0.755-high, 0.28-wide outlined
exclamation, still below one cell and shared across owners. The active diamond's
geometry and subdued swatch 13 remain; its UV now also respects the correct atlas.

This is a source/asset-backed visual defect, not a quest-state error. No camera,
reveal, light, material, palette asset, native owner, or quest semantics changed.
The original native run's input/camera assertion corrections are recorded by the
root harness; this slice does not claim those interrupted runs as completed
acceptance. The revised available sign still needs a new native look-pass, and
static images cannot establish every biome's contrast or animation feel.

Final acceptance: CG17 356/356 focused, CG18 14,728 passed with the same 32
pre-existing failures, zero C# errors. CGN06 passed all 28 live checks and
60-second aggregate editor profiling; see the central implementation/native
reports for exact scope, visual evidence and performance bounds.
