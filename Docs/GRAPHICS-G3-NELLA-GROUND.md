# G3 — Nella's working soil

2026-10-25 UTC. **Native GREEN:272/272 passed, zero failed or skipped. Source and actual-image reviews are clear; bounded ordinary gameplay accepted below.**

## Problem and bounded plan

The actual `prepared-bed-restored-focus.png` capture shows the selected empty
reserve bed at18,6 amid broad brown old-field fragments. The text correctly
identifies prepared soil, but its ground material does not distinguish it from
the surrounding cut rows. Preserve the approved Keeper, crop stages, tray,
cords, owners, coordinates, permissions and finite harvests. This is original
Caves of Ooo presentation work, not a Qud implementation parity claim.

Reuse the existing native ground family: old field earth (frame18), prepared dry
furrows (19), prepared wet furrows (20). Extend only the exact admitted current
Nella graph `Overworld.11.8.0`; retain the existing glade behavior. Do not expand
pond banks or road art as a side effect. Do not mark the four unprepared public
crops as cultivated. A prepared bed is a soil condition, not permission to take
its crop; the existing neutral cord and interaction menu retain that distinction.

## Pre-implementation verification sweep

| Premise | Verified correction |
| --- | --- |
| New soil sprites are needed | `FlatPixelOpeningGroundCatalog` already owns cached frames18/19/20; editable source is `ArtSource/Live16x24/Ground/build_ground.py` and `opening-ground.blend`. Reuse accepted bytes first. |
| Native ground already applies here | Before this change, both the catalog and `FlatPixelGreyboxCatalog.TryResolvePresentation` restricted it to `ReferenceGladePlan.IsActive`. Both routes need the same exact glade/Nella material scope. |
| All Nella crops occupy cultivated soil | `SpreadExplorationResidents` attaches `CultivatedSoilPart` only to the two reserve beds. Public crops must retain their current generic moisture treatment. |
| Soil wetness persists after harvesting | Present implementation derives wetness from the current owned crop's `MoistureTicks`. A removed crop cannot keep an empty bed wet. |
| Broad brown strips are disposable decoration | Some `RipeCropRow` owners have real finite `FieldHarvestPart` yield. Change presentation, not owners or harvest behavior. |
| A current screenshot requires replaying all geometry | The prepared-bed screenshot and receipt are from the latest accepted readout pass. The intervening runtime change only alters cold northern works hedges at11,9, not Nella's saved graph at11,8. Reuse this verified composition for design; require new actual input for acceptance. |
| Unity is free for this audit | Initial preflight found normal Play active, so the fixture stayed outside Assets. This historical permission hold is superseded: the user authorized save/stop, inventory was accepted and pushed as `f25554e6f`, and root reported stopped/cleaned Unity with no active tests at06:16:08 UTC. Root retains exclusive Unity ownership. |
| `CropTime.IsCurrentOwner` proves the enumerated crop owns that part | It validates `crop.ParentEntity` and that owner's current graph membership. The initial detached-foreign-part case did not prove a shared part whose parent is another current crop. The added glade counter reproduced that exact defect in native RED: expected dry frame19 but received wet20. Both the native reader and generic `GroundOf` now compare the part's parent with the enumerated owner. The same test also pins generic `DrySoil`; that later assertion was not reached in RED, so it is not a separate observed failure. |
| Restoring message text restores the borrowed reader queue | `MessageLog.Restore` deliberately clears transient announcement origins and changes `Revision`/`HistoryGeneration`. The imported fixture snapshots and restores the exact origin entries and both counters after nested fixture cleanup; test assertions are unchanged. |

## Production pipeline

1. **Playable baseline:** actual1280×800 `prepared-bed-restored-focus.png` and
   its adjacent receipt. Seed64, Nella,18,7, empty prepared bed18,6 after actual
   harvest/pickup/F6. Approved native material reference is
   `ArtSource/Live16x24/Ground/opening-ground-context.png`, a labeled source study.
2. **Imagegen reinterpretation:** exact prompt in
   `ArtSource/Live16x24/concepts/nella-ground/prompt.txt`; built-in imagegen output
   recorded alongside it. The output makes old earth too bumpy/prominent,
   softens pixels and alters UI. Reject generated pixels and that material
   emphasis. Retain only the dry-furrow versus irregular-earth distinction
   already present in native source. No generated image is gameplay evidence.
3. **Parts/state mapping:** old canonical CropRow/RipeCropRow→18; real prepared
   plain Grass or supported field owner→19; one current visible owned wet crop
   above prepared soil→20. Crops without cultivated soil, unsupported custom
   visuals, hidden/stale owners, ambiguous moisture and hazards keep truthful
   existing fallback. Actor/drop cover must not erase valid lower soil.
4. **Native production:** reuse existing exact16×24 frames and borrowed atlas;
   no approved pixel changes, new texture download, per-frame allocation or
   gameplay mutation planned. Change only bounded selection/integration.
5. **RED/GREEN and review:** first run authored tests against current code,
   then minimal runtime change. Pair every positive with ownership/visibility/
   scope or state counter-check; include default world integration and relevant
   existing ground/claim suites. Independent review before commit.
6. **Actual acceptance:** resume the earned fresh-origin after-North checkpoint;
   reach Nella through ordinary input, pay8drams, inspect the same public versus
   reserve beds, harvest/pickup, plant an earned seed, Rain, leave/return and
   save/load. Capture native arrival and close views. Compare ground hierarchy,
   exact owner state and selection. Reject the change if texture adds noise
   without making the genuine prepared beds clearer.

## Pre-acceptance review and limitations (historical)

- 🔵 Source/visual audit identifies one bounded material-hierarchy issue.
- ⚪ Reuse existing pixels before creating another asset packet; approved cast
  and neutral ownership cords remain unchanged.
- 🧪 Native initial and exact-owner RED runs are recorded below. Root's combined
  GREEN run passed all272 distinct cases, including the31 new cases. Sources
  remain frozen. Ordinary gameplay acceptance and allocation measurement are
  not established by this native result.
- 🧪 No new package, frame-time, draw-call, physical Deck or full-scene acceptance.
  Root's post-inventory-push check recorded848,601,088bytes free. No worthwhile
  safe cache target was found and0bytes were deleted. Nella adds no packages;
  reuse the existing atlas and do not schedule another build from this slice.

## Pre-acceptance continuation (historical)

Current pass: native verification is GREEN and root is conducting the isolated
ordinary Nella harvest/replant replay. Root owns Unity and the final gameplay
record; this agent has not invoked Unity or a test runner. Sources and tests
remain frozen. After this material pass, continue the fresh-origin
glade→east forge→kitchen route and seed65 visual counter-pass. Passing native
tests does not itself close these ordinary input or visual gates.

## Test import log — 2026-10-25 UTC

- Added `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelNellaGroundCatalogTests.cs`
  with a fresh two-line `.meta`:30 cases, using the existing resource and an
  injected borrowed atlas. The source-reviewed scratch assertions are unchanged.
- Coverage includes both direct selection and the actual world route, prepared
  versus public soil, lower terrain under actors/products, exact graph/cell and
  ownership refusals, recovery, coating/hazard priority, and unchanged glade
  water/path frames. Existing injected constructors remain resource-free.
- Real harvest/pickup/adjacent-plant commands exercise existing dirty hooks.
  `CropPart.Water(1)` and the explicit one-unit `CropSystem.OnTickEnd` fixture
  driver test submitted wet/dry repaint without manual dirtiness; they do not
  establish ordinary Rain input or elapsed-world-clock acceptance.
- Message cleanup restores exact entries, queued text, reader/audio origins,
  flash/serial, revision/generation and callbacks after `globals.Dispose()`.
  At import, source review found no additional API/setup blocker; compilation
  and native results were still pending. That historical checkpoint changed no
  runtime or approved art files. Later native results are recorded below.

## Native RED and implementation — 2026-10-25 UTC

- Initial native RED at06:39:54–06:39:56 UTC:30 cases,4 passed,26 failed,
  zero skipped. Failures exposed absent Nella companion/world binding.
  Evidence: [XML](Verification/Live16x24/nella-ground-red.xml) and
  [summary](Verification/Live16x24/nella-ground-red-summary.json).
- Added one exact shared-part/current-foreign-owner case in the already-admitted
  glade. Public `Entity.AddPart` assigns a new parent without removing the old
  owner's list reference. Both crops retain real current anchors; the new
  parent remains wet, the old bed must be dry, and detaching/reassigning restores
  each original owner. This isolates ownership from the missing Nella gate.
- Owner RED at06:41:15–06:41:17 UTC:31 cases,4 passed,27 failed, zero skipped.
  The added case expected source rect x304 (dry19) but got x320 (wet20).
  Evidence: [XML](Verification/Live16x24/nella-ground-owner-red.xml) and
  [summary](Verification/Live16x24/nella-ground-owner-red-summary.json).
- `FlatPixelOpeningGroundCatalog.IsSupportedZone` now shares the exact material
  gate with `FlatPixelGreyboxCatalog.TryResolvePresentation`. Nella still needs
  existing admitted/current Spread/no-POI authority. The underlying strict
  selector continues to validate current cell/owner and visible disclosure.
- Nella reuses only old-field18 and prepared dry/wet19/20. Its ordinary Grass
  requires empty VisualID/variant and the authored Grass glyph variants; glade
  keeps its existing `reference-glade-ground` identity. Active Nella coatings
  refuse native ground, and road16/17 plus water0–15 remain glade-only.
- Added exact crop-parent checks in `CurrentCrop` and the matching generic
  `GroundOf` lookup. The latter avoids truthful dry artwork carrying a stale
  wet semantic classification from the same shared part. No gameplay ownership
  reconciliation, time advancement, resource additions or new observer added.
- Production files changed: `FlatPixelOpeningGroundCatalog.cs` and
  `FlatPixelGreyboxCatalog.cs` only. All31 test assertions are frozen. Native
  GREEN and affected regression now passed as recorded below; ordinary material
  readability acceptance remains pending.

Independent source review by `projection_cold_review/look_focus_draft_review`
on2026-10-25 found no actionable P1/P2 in the settled two-file implementation
and focused fixture: exact scope, disclosure order, coating/path refusal,
shared-part repair and borrowed lifetime are consistent. The reviewer did not
invoke Unity or run tests. Root subsequently supplied the native GREEN receipt
below; ordinary visual acceptance remains a separate gate.

## Native GREEN — 2026-10-25 UTC

Root's native run at06:44:01–06:44:15 UTC passed272/272 distinct test cases,
zero failed and zero skipped. The XML contains31 `FlatPixelNellaGroundCatalogTests`
cases plus241 affected existing controls; the31 are included in272, not an
additional run to add again. Controls cover opening ground, north plot soil,
prepared readout, cultivated farming and greybox behavior.

Evidence: [native XML](Verification/Live16x24/nella-ground-green.xml),
[summary](Verification/Live16x24/nella-ground-green-summary.json), and
[job receipt](Verification/Live16x24/nella-ground-green-job.json). A read-only
XML check confirmed272 unique full test names, every result `Passed`, and31
cases in the new fixture. Independent source-review provenance is the named
child review immediately above; it is source evidence, not an additional test
run or ordinary gameplay capture.

The result confirms native selection, refusal/recovery, submitted repaint and
existing controls. It does not establish the material's actual scene hierarchy,
ordinary Rain/harvest/plant controls, travel/save continuity, physical Deck
readability or performance. Root's in-progress isolated Play replay will supply
its own acceptance record; none is inferred here.


## Ordinary gameplay and visual acceptance — 2026-10-25 07:01 UTC

Root resumed the earned fresh-origin after-North checkpoint in isolated ordinary
Play with normal flat presentation. No teleport, grants, health reset, crop-stage
mutation or time injection was used. The preserved checkpoint and actual captures
are enumerated in `Verification/Live16x24/nella-ground-play-summary.json`.

- Talk/payment changed 50 to 42 drams and the real reserve claim to Granted.
- Real harvest removed Pitchpod9187 at18,6. Picking up gave two resin and one seed.
  The empty bed retained cultivated Grass and the real cut-row owner. Its dry
  furrows are visible without a target outline (`nella-ground-empty-dry.png`).
- Plant-here on unprepared18,7 correctly refused. Choosing the adjacent prepared
  bed consumed the saved seed and created Pitchpod9193 at18,6. The successful
  action's held `b` key also moved the player southwest after the inventory
  closed; a separate input-release audit is queued, not attributed to soil art.
- Ordinary hotbar4 Rain gave both reserve crops moisture39; actual background
  lookup confirms atlas frame20. Forty normal waits exhausted moisture and
  restored frame19; the planted Pitchpod grew to stage1/16 units without creating
  an extra harvest. No gameplay crop behavior changed in this slice.
- F5, south-border departure, F6 latest autosave and ordinary north-border return
  preserved crop9193, Granted rights, two resin, 42 drams and32HP. New checkpoint:
  `/tmp/coo-fresh-opening-after-nella/20cb263ebed24a8380168954e23981ca/`, tick3510,
  Nella18,7. Quick.sav.gz SHA256
  `9cde4c780f6e855623e02e7e9dcaebc2382b82773b15ab5a1f4098efeb0c8390`.

Independent actual-image review accepts the restrained material distinction and
clearer wet/dry contrast with unchanged cords/foregrounds. It explicitly leaves
broad repetitive field fragments and weak service grouping open. Source/review
receipts are beside the actual PNGs. The earlier before capture has different
owner/build/tick and a target outline, so it is a material comparison, not an
identical-simulation before/after image pair.

### Self-review and measurement limits

- 🟡 Corrected a capture-helper error: first four raw receipts queried background
  `(x,-y)` and returned null. Actual rendering uses `(x,Zone.Height-1-y)`.
  Those raw files are preserved. `wet-focused`, `dried-after-wait` and `save-return`
  use corrected coordinates and confirm the actual painted20→19→19 frames.
- 🔵 Native GREEN272/272 with zero skips, source and independent image reviews
  support this exact material slice. No approved pixel or gameplay owner changed.
- 🧪 Profiler ran279.36 seconds /241299 frames through input and tool/idle time;
  its131072-sample circular buffers wrapped. Retained-tail p99 is40 draw calls/
  batches and about0.143ms for the ZoneRenderer marker. This is not a baseline
  comparison, complete-run distribution, standalone or physical Deck result.
- 🧪 Whole-scene quality, fresh-origin east forge/kitchen, seed65 and regional
  coverage remain open. Current packages predate inventory and this change.
- ⚪ Cleanup07:01:10 UTC restored normal scene, preferences, view and save root;
  owned Play/isolation are false. Normal user save and backup hashes match.
  Storage inspection found612225024 bytes free and no worthwhile safe cache
  target; zero bytes deleted. Preserve useful caches and all current builds.

**Can verify:** native ownership/state/dirty-hook regressions and the named
ordinary inputs, real material appearance, growth, payment, save/load and travel.
**Cannot verify:** every seed/owner, complete scene/world graphics, new package,
physical Deck controls or performance from this pass.

Next: preserve this exact slice on main, then address the observed input-release
risk and continue the earned east forge/kitchen journey. Design a more coherent
Nella service grouping using the existing real owners; do not add disconnected
props to disguise the weak field layout. Reuse this actual capture for that
playable greybox/concept cycle. Keep earlier checkpoints and approved pixels.
