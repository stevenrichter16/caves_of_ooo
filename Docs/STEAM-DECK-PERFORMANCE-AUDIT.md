# Steam Deck performance audit — 2026-10-10

Status: code/configuration audit complete against `5657211c5` (the Vulkan/controller
build). No gameplay or rendering changes in this audit. Qud reference: none; these
are implementation costs in Caves of Ooo, not changes to its gameplay design.

## Decision

Optimize redundant presentation work first. The strongest immediate targets are
the always-redrawn hotbar and the sidebar's full inventory reconstruction. Then
make native 3D refresh incremental, stop hidden legacy rendering/contact work, and
measure native GPU submission/shadows. Address synchronous whole-world autosaving
as a separate high-priority source of travel freezes. Preserve content density,
simulation rules, native 3D style, visibility and recovery behavior.

This ordering reflects confidence, execution frequency and fix risk. It is **not
a measured ranking of Steam Deck milliseconds**. Idle slowness, action hitches and
transition pauses have different likely causes and must be measured separately.

## Method and limits

- Read the current input, UI, native/legacy rendering, material/tile simulation,
  AI, diagnostics, save and quality-setting paths. Three bounded independent code
  reviews covered rendering, UI and simulation; principal findings were checked
  directly against source.
- Consulted existing performance reports and verified whether their proposed
  fixes already shipped. Historical timings below retain their original scope.
- Unity inspection confirmed an idle editor at the ordinary SampleScene. No new
  PlayMode or Steam Deck performance capture was made. Mac editor results and the
  earlier software-rendered Linux diagnostic do not establish Deck CPU/GPU speed.
- Verified Unity's documented SRP Batcher/MaterialPropertyBlock restriction.
  No claimed FPS gain, GPU saturation or current hardware bottleneck is inferred
  merely from a settings file or code count.

## Verification sweep corrections

| Suspected issue / old statement | Current evidence |
| --- | --- |
| Old 190 ms dirty-frame report describes today's build | It predates lookup/contact fixes. The later report is still an older editor workload, not Deck timing. |
| All terrain meshes rebuild every frame | Ground has forty fingerprinted patches. Equal fingerprints skip rebuilding. |
| Creature-tag lookup scans all terrain | `Zone.GetEntitiesWithTagNonAlloc` now uses an index. |
| All explored chunks run NPC AI continuously | Transition registration limits ordinary turn actors to the active zone. Cached chunks still cost memory and saving. |
| Sidebar fingerprinting eliminates sidebar work | It skips final painting; full inventory/snapshot construction happens first. |
| Low detail removes contact computation | It only zeros shader strength; refresh/raster/upload can still execute. |
| Standalone Ultra means four cascades and expensive MSAA | All quality levels point at one URP asset: one 2048 shadow atlas, shadow distance 50. Native camera explicitly disables HDR, MSAA and post-processing. |
| Unity game-message logging is development-only | `GameBootstrap.cs:196` unconditionally mirrors messages to `Debug.Log`. |
| The new controller path is an obvious performance culprit | It samples once per Input System update and reuses its collections; no comparable large workload found there. |

## Ranked findings

### 1. Stop rebuilding unchanged HUD content — highest-confidence first work

**Frequency:** every unpaused gameplay frame, including standing still.

`ZoneRenderer.cs:975-978` calls both sidebar and hotbar rendering every frame.
`HotbarRenderer.cs:40-64` clears two tilemaps and repaints them without a content
gate. Its background alone is 80×6 cells, each receiving SetTile, SetTileFlags and
SetColor (`:74-81`; dimensions in `GameplayHotbarLayout.cs:11-14`): **1,440 tilemap
API calls/frame**, or 86,400/second at a hypothetical 60fps, before borders/text.
These are code-derived operation counts, not measured draw calls or CPU time.
`HotbarStateBuilder.cs:14-57,88-100` also creates slot data and repeatedly derives
names/glyphs from strings.

`SidebarStateBuilder.cs:42` calls `InventoryScreenData.Build(player)` merely to
obtain vitals, weight and currency. `Gameplay/Inventory/InventoryScreenData.cs:87-153`
allocates dictionaries/sets/lists, sorts every carried/equipped item, builds item
display data including every item's available actions (`:458-471`), then equipment
and paperdoll layout. The sidebar renderer's later
fingerprint does not undo that work. Larger inventories increase this idle cost.

**Change:** give the sidebar a lightweight vitals source; invalidate/cache its
snapshot on relevant state changes. Cache hotbar state and paint only changed
slots/text; paint its static background once per layout/lifecycle change.
**Check:** unchanged frames perform no tilemap clear/repaint or inventory-screen
build. HP/MP, equipment-derived AV/DV, cooldowns, selection, logs, inventory weight,
load/new game, resize and pause/resume still update immediately. Measure allocation
and CPU with small/large inventories at the same world position.

### 2. Make native world refresh proportional to changes

**Frequency:** dirty render frames, including player movement and NPC/state changes.

`SpawnRing3DPresenter.cs:267-312` receives dirty cells but re-resolves every entity,
reconciles all views, then refreshes contact, all fog pixels, transient volumes and
equipment. `SpawnRing3DRecipes.cs:25` runs a long refinement chain for owners.
Dirty cells currently narrow ground patch checking, not this whole reconciliation.
`ZoneRenderer.cs:994-1068` additionally clears and walks all 2,000 native tile cells
on full redraws. Some fallback work remains necessary; scope by actual ownership.

**Change:** separate geometry/ownership, visibility/light, equipment and transient
invalidations. Resolve geometry only for added/removed/changed owners; update
visibility without reconstructing unchanged recipes. Use frame-local region-policy
results where safe. Keep full rebuild for binding, recovery and genuine bulk edits.
**Check:** moving one NPC only reconciles affected owners; player FOV changes do
not cause unchanged geometry resolution. Add/remove/transform, hidden owners,
multi-cell bodies, equipment, region authority and fallback failures remain exact.

### 3. Stop rendering work whose result is hidden

**Frequency:** contact work on dirty refreshes; legacy water animation every frame.

`SpawnRing3DPresenter.cs:309` always calls contact Refresh. At `:625`, low detail
calls SetEnabled(false), but `ReferenceGladeGroundContact.cs:95-96` only changes
shader strength. Refresh still scans contributors and, when visibility/signature
changes, rasterizes and uploads its 640×200 field (`:66-104`).

`ZoneRenderer.RefreshWaterCache` (`:2325`) and `UpdateAmbientAnimations` (`:2429`)
also lack a native-owner exclusion. Background/fine/animated environment layers
sit below the opaque native composite (surface `:214-222`; shader
`Assets/Art3D/Village/Shaders/Village3DComposite.shader:10-14,59`). They can spend CPU
updating water which the native view overwrites. Stationary shimmer already skips
unchanged bands; flowing water still performs recurring writes.

**Change:** suspend contact generation while disabled, with proper rebuild on
reenable. Suspend covered legacy water/reflection/bank painters when the current
zone has a valid visible native surface. Retain meaningful effects/readouts and
restore legacy state correctly if 3D fails or is switched off.
**Check:** zero contact raster/upload while low detail is active; no covered water
writes during native rendering. Full-detail restoration, fog/privacy, mode toggles,
zone switches, pause and presenter failure must retain the correct picture.

### 4. Investigate native render submission and add a usable handheld profile

**Frequency:** every rendered frame. Potential CPU submission and GPU costs.

`NativeZone3DRenderSurface.cs:151-181` gives every prepared renderer a nonempty
MaterialPropertyBlock, including combined ground patches. This makes those
GameObjects incompatible with the enabled SRP Batcher. Unity documents the
[compatibility requirement](https://docs.unity3d.com/6000.0/Documentation/Manual/SRPBatcher-Materials.html).
Instancing switches alone do not prove instanced draws: `_Transient` remains an
ordinary material-buffer field in `Village3DCommon.hlsl:13-25`. Frame Debugger must
verify actual batch reasons. SRP batching reduces CPU setup; it does not imply all
meshes become a single draw.

Full detail renders a separate viewport-sized target and soft-shadowed world, then
composites it (`NativeZone3DRenderSurface.cs:241-276`). All prepared renderers cast
and receive shadows. Actual URP settings are 2048 main-light shadow resolution,
one cascade and distance 50 (`Assets/Settings/UniversalRP.asset:45-70`).

`Village3DSettings.cs:39-42` defaults to full detail. Low detail's target is 75% in
each dimension: 56.25% of full-detail pixels, plus sunlight shadows disabled. This
is a pixel-work reduction, **not an expected 43.75% frame-time improvement**.
The only runtime detail toggle is Shift+F11 (`Village3DControls.cs:15-20`), which
isn't in native controller mappings. Pause offers save/load/help/quit, no graphics
settings (`PauseMenuController.cs:35-40`). All Unity quality levels share the same
URP asset. No ordinary gameplay code sets a dedicated handheld frame budget.

**Change:** first inspect batch reasons and GPU time. Preserve per-owner tint/fog
and ambient semantics using a small material-family design or correctly supported
instancing where beneficial. Add controller-accessible presentation settings and a
handheld preset with independent world resolution/shadow/effect choices. Keep HUD
resolution and art silhouettes. Select a consistent frame-pacing policy from real
measurements; merely imposing a cap does not remove action stalls.
**Check:** native Vulkan captures at the same resolution/save, full versus low,
shadows versus no shadows. Record actual render-target sizes, render-thread time,
GPU time, batches, SetPass and shadow submissions. Inspect visuals for fog leaks,
lighting/tint loss, and readable action cues. Do not turn off MSAA/HDR again as an
optimization: they are already disabled on the native camera.

### 5. Remove whole-world serialization from the chunk-transition frame

**Frequency:** every successful chunk transition; cost grows with cached world.

`InputHandler.cs:1078-1083` synchronously QuickSaves after arrival. `SaveSystem.cs:653-660`
serializes the live session directly into gzip/file output. It visits every cached
zone (`:907-911`), every cell (`:1162-1165`) and entity graph, copies the previous
compressed save to backup (`:758`), and saves metadata/preferences before returning.
Normal travel has no general cache eviction; stale village regeneration is an
explicit exception. This is separate from ordinary idle FPS.

**Change:** stage safe immutable save data, cache serialized unchanged chunks, and
move compression/disk commit off the input/render frame. Cache serializable field
metadata (`SaveSystem.cs:1897-1943`). Later, bound retained inactive world graphs.
**Do not simply run state.Save on a worker:** it traverses live mutable state and
invokes binding/mutation helpers. Preserve atomic backup and recovery semantics.
**Check:** snapshot/serialization/compression/write timed separately at 1/20/100
visited chunks; save/load round trips, repeated travel, simultaneous save requests,
load/quit during pending work, disk-full and interrupted-write recovery.

### 6. Index active environmental work instead of scanning inert objects

**Frequency:** once per player action, after NPC processing.

`InputHandler.cs:1113-1120` runs environment updates. `MaterialSimSystem.cs:27-61`
allocates two lists and a closure, scans all 2,000 cells and checks effects/parts on
every noncreature to find the few requiring work. `ZoneTileStateSystem.cs:67-75`
copies all zone entities to find terrain state sources. `CropTime.cs:26-40` already
uses an indexed tag and pooled snapshot; it is a useful existing pattern.

**Change:** maintain active-material and terrain-source memberships; take reusable
snapshots of relevant owners. Preserve processing order, mutation safety and exactly
once-per-turn semantics, including ignition during a pass and multi-cell owners.
**Check:** dry versus burning/wet zones with the same actor count and progressively
more inert decoration; reaction, hazard, crop and save/load invariants unchanged.

### 7. Reduce production diagnostics and other avoidable frame work

`Diag.cs:119-120` enables 27 categories in release too. `:215-227` builds GUID strings
and eagerly serializes JSON. Crop reconciliation emits even when no growth unit is
produced (`CropTime.cs:85-87`). `GameBootstrap.cs:196` mirrors every gameplay log
message to Unity logging. `PerformanceDiagnostics.cs:126-144` creates a snapshot
and dictionary every frame even when verbose logging is disabled.

**Change:** retain essential failures and opt-in diagnostic capture; use bounded
compact values with serialization at export, preserve record-time value semantics,
and gate the Unity-log mirror. Reuse performance snapshots when no consumer needs
immutable history. Measure before allocating a large refactor budget.

Also eliminate the all-entity idle scan in `CombatIntentRenderer.cs:54-77`: an
unchanged-draw check happens only after scanning all owners for committed melee.
The sidebar's fallback focus (`SidebarStateBuilder.cs:233`) reaches
`LookQueryService.cs:54` and `CombatIntentReadout.ThreatLine` (`:52-62`), performing
a second whole-zone threat scan in ordinary no-look-focus frames. Share indexed
actors or dirty intent/visibility state across both consumers, retaining all danger
cues. Empty or unchanged intent should be cheap to query.
`WorldFxCoordinator.cs:120-122,154-156` advances then calls all three backends again
with zero delta; separate advance/admission/paint to avoid a duplicate paint when
no new request arrived. Preserve first-frame visibility, contact recovery and
damage-resolution timing. These are follow-ups after the first HUD findings.

### 8. Pathfinding and asset-bind costs — measure affected scenarios first

Obstacle/cost-aware pursuit recomputes a full path to consume one step
(`AIHelpers.cs:435`; multi-cell `KillGoal.cs:144`). `FindPath.cs:64-101` allocates a
result and resets 2,000 pooled nodes. Profile congested combat before choosing
validated path reuse, stamped nodes and reusable results. Greedy movement and
hostile-target caching already exist. Negative-target caching can delay engagement.

Some art libraries copy mesh buffers while validating during zone binding. This
can contribute to transition spikes but is **not a per-frame import**. Measure bind
separately from generation/save before caching validation or changing asset loading.

## Existing measurements: useful evidence, not a Deck baseline

- `Docs/DENSITY-SPREAD-RENDER-PERFORMANCE.md:72`: later editor route reports dirty
  renderer mean 40.227 ms / p95 43.007; native refresh 21.795 / 23.694; contact
  8.791 / 10.074. Markers nest; do not sum them. This is after earlier lookup/contact
  improvements, with workload/confound caveats retained in that document.
- `Docs/Verification/QuestFreeExploration/E2/Performance/native-interpretation.md:15`:
  September 28 native Unity probe found approximately 918 ms median in-memory save
  of 20 chunks, before gzip/disk, across only three current-arm samples. Serialized
  cohorts were about 35.5–36 MB / 45k entities. This supports the save architecture
  concern; it does not predict this player's transition time.
- `Docs/Verification/SpawnRing3D/PERFORMANCE.md`: older Mac/Metal paced walking had
  low typical frame percentiles but unassigned 390/674 ms whole-editor spikes.
  Neither a sustained 60fps guarantee nor a current Linux comparison follows.

## Recommended implementation and acceptance order

1. Capture one release-like Vulkan baseline: fixed save/resolution/zoom, warm-up,
   quiet 60-second idle, same paced walk, crowded combat, wetland/spells, and chunk
   crossings after short/long exploration. No screenshots/report IO inside samples.
   Record main/render/GPU time, allocations/collections, refresh counts and actual
   draw statistics. Group idle, dirty, combat and transition frames separately.
2. Land HUD vitals and hotbar invalidation with failing work-count tests and
   behavioral counter-checks. Re-run the matched idle/walk sample.
3. Stop hidden contact/legacy work, then narrow native reconciliation. Land these
   independently so each benefit and any visual regression can be attributed.
4. Add accessible handheld settings; fix demonstrated submission/shadow bottlenecks.
   Compare at identical settings before presenting quality changes as speedups.
5. Implement safe staged/incremental saving as a separate milestone; then active
   environment indices, production diagnostics and measured combat hotspots.

Track median/p95/p99/**maximum** and the number of over-budget frames in each
population. At a proposed 60fps target the budget is 16.7ms; at 40fps it is 25ms.
Those are targets to select, not promises. A low overall p95 can hide every movement
hitch when changed frames are under 5% of samples. Missing GPU/allocation counters
must be reported unavailable, never zero. Success also requires visual parity,
unchanged deterministic gameplay, compatible saves and safe failure recovery.

## Self-review and follow-through

- 🟡 Confirmed recurring work: hotbar repaint, sidebar inventory construction,
  broad native reconcile, hidden contacts/water, main-thread full-world save.
- 🟡 Existing documentation drift corrected above; do not optimize already-fixed
  tag lookups, Unicode atlas misses or contact opaque pixels again.
- 🧪 Hardware attribution remains open: no current Deck frame capture, render-thread
  profile or GPU saturation proof. No numerical speedup claimed.
- ⚪ This deliverable is an audit and prioritized work plan. No performance change
  is shipped here, so no new regression test execution is claimed.

Files changed: this audit only. Read-only inspection did not alter editor play state,
game settings, source, save data or the distributed archive.
