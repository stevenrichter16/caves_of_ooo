# Stump runtime review and bounded performance observation

2026-10-27. **No unresolved significant code defect found in the reviewed
scope.** Actual first-trial stone contrast is a separately reported palette
issue; native source changes and matched gameplay acceptance remain root-owned.

## Review authorship and scope

This record uses the requested review filename but does **not** claim that its
writer is independent of the implementation: this reviewer authored the initial
Stump catalog, Greybox hooks and 92-case fixture. This pass is a renewed author
review of the frozen code and independently strengthened tests. The separate
`world_coverage_ledger` agent cold-reviewed the runtime without authoring it and
reported no unresolved concrete defect; its independent test review and two
root-authorized improvements are recorded in
[stump-test-contract-review.md](stump-test-contract-review.md).

Read-only review covered the full `FlatPixelStumpMaterialsCatalog`, the precise
Stump additions to `FlatPixelGreyboxCatalog`, all current Stump test bodies and
the existing liquid/well/harvest, renderer and performance-instrumentation APIs.
Only this review document was written. No Unity, tests, gameplay, Git, runtime,
art or shared living records were changed by this review pass.

Frozen source SHA-256 values re-read for this review:

| File | SHA-256 |
| --- | --- |
| `Assets/Scripts/Presentation/Rendering/FlatPixelStumpMaterialsCatalog.cs` | `3a9b604c1d32a48768cbe1700626b0654f289ec7920a88f3ecdc4e9300fc0a20` |
| `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs` | `62357e1530445eafd80af1cebc261fe21a823b107f8a8dd853d014b5e8058498` |
| `Assets/Tests/EditMode/FlatPixelStumpMaterialsTests.cs` | `548c1f8ac08d969adc337c6364136ae85efbde3e4cb4c3d3228cf5dfaecc5c6d` |

## Findings and evidence

- **Authority and disclosure:** the finite ordinary address set is checked
  against the exact current manager/cache graph, live Stump biome, null POI and
  saved-lair exclusion. Native art does not widen the opening or another region.
  Invisible, remembered, unexplored, detached, moved, carried, equipped and
  changed-footprint owners retain refusal. Readers require the current selected
  owner; the covering actor does not authorize the pool's portrait.
- **Dry owners and actual floor:** factory-derived exact material/part contracts
  distinguish passable ledges and tanks from solid ridges, domes, walls and veins.
  The floor's authored glyph variants remain valid. Cultivation, Plantable,
  unfamiliar terrain, duplicate floor and independent tile states refuse neutral
  ground. Exact neutral Terrain overlays retain their actual TepuiStone beneath
  them without inventing a floor. Hidden/foreign competitors cannot erase a
  valid disclosed substrate.
- **Water:** native ground needs one visible, current, exact SprayPool plus its
  sole permanent unowned water coating. Missing/mixed/temporary/ice coatings,
  wrong liquid, negative stock, duplicate pool part/source or foreign ownership
  refuse it. Zero stock remains wet. The source may remain below a covering
  actor, but only that source's own selected foreground is suppressed. Existing
  painter rules then omit the represented water wave while keeping independent
  heat, charge and cloud marks. No groundwater, stock or projection is changed
  by rendering. Ownerless Fen water rules remain untouched.
- **Meaningful action boundaries:** actual flask withdrawal reaches zero without
  deleting the natural source or wet projection; its ordinary dirty hook updates
  the frame. Tank drinking has no finite pool or invented empty state. Real vein
  harvest consumes the source and produces ordinary Tepuibone. Partial wall/tree/
  bush damage preserves the intact body; removal and true wall wreckage replace
  it. Unsupported current semantics use fallback rather than a misleading image.
- **Resources and lifetime:** the 26-cell main atlas and exact two vegetation
  cells remain independent optional groups. Null/malformed injection does not
  reload resources. Cached tiles/sprites are disposed, borrowed textures and
  selector are retained. The Stump path uses current-cell occupants and a cached
  finite address index, with no zone generation, world scan, game RNG or pixel
  read. Source structure alone is not an allocation or frame-time measurement.
- **Strengthened counterchecks:** the two absent/malformed-main cases now run
  submitted rendering on a real SprayPool and require its actual `~` mark,
  no native/local-water claim and unchanged coating/stock. The harvest case now
  uses the genuinely earned carried product, an authorized inventory document
  and public `TryGetFlatExaminePortrait` to require the existing harvest-product
  atlas, frame 6 UV, white tint and sprite presentation. These are functional
  counterchecks, not only non-null sprite or blueprint-count assertions.

No additional production change or test run is justified by this review.
The native-size dome/ridge/floor value separation, calm basin appearance and
SummitScrub fullness must be judged from the actual revised gameplay views.
This code review does not close those visual gates.

## Native results read from retained XML

These are existing root-run results, independently parsed here, not tests run
by this reviewer. Each file's full test names are unique.

| Receipt | Cases / results | SHA-256 |
| --- | --- | --- |
| `stump-materials-native-red.xml` | 92 failed; missing catalog/atlas contract at the recorded RED boundary | `2aed79cf2671087a311202c67383f61f8b41071fcbf662fc2a29e1050850e835` |
| `stump-materials-candidate.xml` | 92 passed, 0 failed/skipped/inconclusive | `a5dc538edbac2b6518e02a60d4738e6d8e0c18d51ec387008680666f389810fe` |
| `stump-materials-related.xml` | 644 passed, 0 failed/skipped/inconclusive | `86bbf55a73d861b86230d3e7660294f8758438034a16b6a029ed3201816f2e96` |

The related report contains **all 92 candidate identities**. Their union is
**644 distinct passing cases**, comprising 92 Stump cases and 552 additional
related cases; it is not 736. Candidate ended 11:51:09Z; related ended 11:55:10Z.
The XML is execution evidence despite the separately reported bridge status
timeout. It does not establish revised-art visual acceptance or a new package.

## Minimal read-only performance observation for root

Use the existing instrumentation rather than attaching a new sampler or
resetting counters. Capture three small serialized observations: after an
already completed render, after a short idle interval, and after a few ordinary
same-zone steps or an existing material interaction. Record what actually
happened; do not force a repaint, inject an action or change focus/settings to
manufacture a result. Keep loading, map transitions and fullscreen UI intervals
separate from same-zone observation.

At each observation read:

1. `Time.frameCount`, `Time.realtimeSinceStartupAsDouble`, `Application.isFocused`,
   `Screen.width/height`, active zone ID, actual player cell, turn tick and flat
   presentation state. These bind the sample to the current scene and prevent a
   paused/menu or changed-viewport sample being mistaken for world performance.
2. Raw `EnvironmentSpriteRenderer.Perf` fields: `Frames`, `FullPasses`,
   `IncrementalPasses`, `CellsResolved`, `ClaimsMade`, `TilemapWrites`,
   `TotalTicks` and `MaxTicks`; retain `Stopwatch.Frequency`. Read fields and
   compute deltas offline. **Do not call `Perf.Reset()`.** `Frames` means
   `PostRender` calls, not Unity frame count. Delta total ticks converted to
   milliseconds, divided by delta calls when nonzero, is average environment
   post-render CPU time for the interval—not whole-frame or GPU time. `MaxTicks`
   is a cumulative high-water mark; subtraction cannot produce an interval max.
3. `PerformanceDiagnostics.LastCompletedFrameSnapshot` once: its `UnityFrame`,
   `TurnTick`, `Paused`, `RendererDirty`, `ZoneRendererLateUpdateMs`,
   `CellsRendered`, `TilemapClearCount`, UI render counts, `MarkDirtyCount` and
   `MarkDirtyBySource`. Retain snapshot frame ID; it may precede the observer's
   frame. The first request for a completed frame materializes a detached object
   and dictionary, so this probe is not a zero-allocation measurement. The field
   named `FullZoneRedrawCount` increments for both `RenderZone` and `RenderCells`
   through `RecordZoneRedraw`; distinguish actual full/incremental passes using
   the environment counters rather than that field's name alone.

The existing **Caves Of Ooo → Diagnostics → Log Frame Snapshot** menu provides
item 3 without new code, but logs and allocates a diagnostic wrapper. A bounded
read-only execution tool may instead return the same public values alongside
raw item-2 counters. Neither route should call `GetZone`, inspect every entity,
read screenshots/pixels inside a timed loop, reset diagnostics, enable verbose
logging or toggle profiling. No source-specific zero-allocation claim follows
from a few counter snapshots. If continuous native marker timing is later
needed for a demonstrated issue, reuse the already implemented profile observer
and its availability/frame-correlation rules as a separate, labelled run.

A movement step can legitimately request a full redraw for FOV/light changes;
UI transitions and real environmental updates also have existing redraw costs.
Do not turn this observation into an unsupported rule that all ordinary steps
must be incremental. A useful result reports actual calls/cells/writes and
bounded CPU cost, plus unchanged graphics/settings and the observed action.
No comparable pre-change profile is retained by this review, so no speedup
percentage, Linux execution, physical Steam Deck or sustained-performance
certification is available.

Instrumentation sources read: `EnvironmentSpriteRenderer.cs` PostRender/Perf;
`ZoneRenderer.cs` LateUpdate, RenderZone/RenderCells and liquid-aware marks;
`Shared/Utilities/PerformanceDiagnostics.cs`; `Editor/PerformanceCaptureMenu.cs`;
`Scenarios/Custom/ReferenceGladeNativePlayer.ProfileMarkers.cs`. These are
existing facilities; this review adds no instrumentation or framework.

## Independent perimeter addendum — separate reviewer, 2026-10-27

The `/root/world_coverage_ledger` reviewer did not author the perimeter runtime
or its 19 new tests. This bounded cold review inspected the frozen
`FlatPixelStumpMaterialsCatalog.cs` SHA-256
`854659dee1c9c659fbdc78ae4080a125288bd526b951b37ee95a96ebf113a3a8`,
specifically the 71-frame contract, constructor and foreground/perimeter code at
lines 60–98, plus the new `Perimeter*` tests and the existing dirty submit path.
No unresolved significant source/API/fixture defect was found in that scope.

- Exposed-edge bits use N `(0,-1)`, E `(1,0)`, S `(0,1)`, W `(-1,0)`, consistent
  with `MovementSystem.cs:447–458`. The zone-to-tilemap Y inversion occurs only
  during submission. Mask 0 retains the original interior; masks 1–15 map to
  wall 26–40, ridge 41–55 and dome 56–70 without exceeding the 71-frame atlas.
- Each neighbor is disclosed, in the current cell/graph, selected by the existing
  fallback selector and validated by the same-family `DryFrame`/`Placed` rules.
  Hidden, remembered, covered, malformed, foreign or unsupported owners cannot
  join. Missing and undisclosed neighbors yield the same exposed edge. Diagonals
  and different solid families do not join; no broad `Cell.IsWall` shortcut was
  introduced. Independent tile heat/coatings do not incorrectly erase an
  otherwise eligible solid body's continuity.
- The authorized reader deliberately retains **exact current world-frame parity**
  using the same disclosed neighbors. A preliminary suggestion to use an isolated
  reader frame was explicitly rejected in favor of this contract; it is not a
  missing implementation requirement. Reader authority is unchanged.
- Resource failure remains independent; non-perimeter families and the original
  26 frame indices retain their roles. No per-cell collection, sprite allocation,
  recursive art resolution, generation or state mutation was added. This source
  structure is not a measured zero-allocation claim.
- The new tests pin every mask for each family, asymmetric directions, invalid
  neighbor refusal, zone corners, cross-family/diagonal behavior, reader parity,
  independent overlays and actual submitted dirty cover/uncover/destruction.
  `EnvironmentSpriteRenderer.cs:1105–1132` expands dirty cells to eight neighbors;
  `DestructionSystem.cs:167–176` emits the real removal dirty hook. The submitted
  destruction test does not substitute a forced full redraw or a selector-only
  assertion for this path.

The retained `stump-perimeter-native-red.xml` was independently parsed: 111 test
cases, 94 passed and 17 failed. The reviewer did not run Unity. New atlas import,
native GREEN and actual collision/readability comparison remain root-owned and
pending at this review boundary. The prior visual finding is not closed merely
because the perimeter code is internally consistent.
