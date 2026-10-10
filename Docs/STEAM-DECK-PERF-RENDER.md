# Steam Deck bounded native rendering optimization

Status: new native regression/adversarial cases 27/27 GREEN; independent surface-setting cases 4/4 GREEN after RED; compatibility correction rerun 58/58 selected existing/new adversarial cases GREEN, 2026-10-10. Live/profile validation remains owned by the root task. Qud reference: none; presentation-only Caves of Ooo work. No Deck timing or draw-call improvement claimed.

## Goal and scope

Implement audit findings 2–4 in small independent milestones: suspend disabled contact computation with exact resume; reconcile native owners from known geometry dirt while keeping explicit full recovery and visibility-only refresh; remove unnecessary renderer property blocks only when all native values are uniform and no authored per-instance data or ambient probe requires one. Root task owns ZoneRenderer invalidation and settings integration. Preserve all art, content, gameplay, native visibility and fallback behavior.

Content readiness: 🟢 existing imported native libraries and integration fixtures; 🟢 known ownership/material contracts; 🟡 invalidation distinction needs caller integration; 🧪 native Vulkan Frame Debugger / Deck GPU attribution unavailable in this implementation environment.

## Verification sweep before production

| Reference inspected | Finding / correction |
| --- | --- |
| `SpawnRing3DPresenter.RefreshCurrent`, `SyncCamera`, `Release` | Every refresh resolves every native owner; dirty cells currently narrow only ground. Explicit null dirt must remain full refresh. |
| `ReferenceGladeGroundContact.Refresh`, `SetEnabled` | Disabled state only changes shader strength. Retain latest zone/recipe/reveal references during suspension, rebuild on reenable before restoring strength. |
| `NativeZone3DRenderSurface.PrepareModel`, `ConfigureAmbientProbe` | All renderers receive an MPB; custom SH probe coefficients require MPB under current shader contract. Preserve those blocks and all tint/headwear/indexed data. |
| `Village3DCommon.hlsl` | `_Transient` is a material uniform; static material-family clones can carry zero without a renderer block. Transient/custom ambient renderers retain their blocks in this bounded milestone. |
| `SpawnRing3DRecipes` neighbor helpers and all refinement libraries | Height, tent wall, timber wall and niche recipes read cardinal neighbors. Expand geometry dirt by one cardinal cell; direct-cell-only reconciliation would preserve obsolete adjacent geometry. Refinement methods have no additional direct cell scans. |
| `SpawnRing3DGroundPatches.Refresh` | Null dirt fingerprints all patches; known dirt and marked changed recipes scope patches. Existing geometry already caches meshes. |
| `Zone.AddEntity`, `RemoveEntity`, `GetOccupiedCells`, `Cell.Occupants` | EntityVersion changes for movement/membership; multi-cell owners must deduplicate. Previous anchor indexing is needed to remove owners absent from dirty cells. |
| `SpawnRing3DIntegrationFixture`, existing contact/surface/integration tests | Real-resource fixture and reflection support assertion-based RED before counters/APIs exist. Existing empty-dirt tests require visibility/equipment refresh without unnecessary ground rebuilding; broader compatibility review below corrected the assumption that this also permits skipping all owner reconciliation. |
| `SpreadTransientVolumes.Refresh` | Full scan remains a separate conservative subsystem; skipping its reconciliation without a proven source revision would risk missing effects. |
| `CLAUDE.md`, `PERF-FOUNDATION.md` | Test first, record actual RED/GREEN centrally; reusable scratch collections; operation counts are evidence of work removed, not hardware milliseconds. |

## Milestones and implementation snippets

1. Contact suspension: retained inputs + enabled flag; disabled `Refresh` only records inputs, reenable runs one fresh `Refresh` before setting strength. Counters independently track contributor visits, raster and upload.
2. Incremental owner refresh: explicit full null/empty fallback; nonempty dirty-cell owner candidates from previous anchor index and current occupants; reconcile each unique owner once. Explicit `RefreshVisibility` loops committed views and updates shader fog/contact/equipment/transients without recipe resolution when membership is unchanged. Counters expose recipe resolutions.
3. Uniform static materials: set `_Transient=0` on surface-owned clone; `PrepareModel` leaves an empty renderer block empty when static/no custom SH. Preserve existing blocks, indexed values, transient policy and custom SH. Do not clear unknown MPB data.

## Divergences and rationale

| Planned possibility | Bounded implementation decision |
| --- | --- |
| Migrate all ambient/transient values into new shader/material families | Preserve custom SH and transient blocks: proof of image parity and variant identity requires a separate measured shader change. Optimize safe existing static family now. |
| Narrow transient effects and equipment by new revisions | Preserve existing refresh contracts; current source mutation paths lack one comprehensive invalidation revision. |

## Validation / implementation log

- Plan and sweep recorded before production changes.
- Root agent owns Unity refresh, RED/GREEN execution and PlayMode validation; this agent performs no Unity tool calls.
- Root recorded native RED in the combined run: missing recipe/contact diagnostics, stale neighbor output, and unconditional static MPBs. Actual receipt: `combined-red.json` in root verification output. MCP returned only its first 25 failures, so this document does not invent per-case counts.
- After RED release, implemented the three production milestones. Contact counters count actual scan/raster/upload execution. Recipe counter counts only reconciliation work; style probes remain independent.
- Added seven work/regression tests and twenty dedicated adversarial cases before implementation. Boundaries, cardinal dependencies, reveal/hidden state, remove/move/unknown membership, tint/headwear/indexed/SH data are covered.
- `git diff --check` passes for owned production files. Focused rendering and graphics suites are GREEN as recorded below; broader existing rendering suites and live/profile parity checks remain with the root task.

## In-phase self-review

- 🧪 CPU/GPU/frame-time gains require matched native release captures; no such gains claimed.
- 🔵 Resolved sweep finding: direct-cell-only candidates would miss adjoining walls/niches/height variants; candidates expand one cardinal cell and deduplicate owners.
- 🔵 Resolved visibility finding: cached views explicitly check native Render.Visible and current membership every refresh, independent of recipe reconciliation.
- 🔵 Symmetry check: add/reconcile indexes committed owners; hide/remove/unrecognized recipe unindexes them; Release clears all buckets/counters. Unknown bulk input, invalid cell keys and missing membership hints use full recovery.
- ⚪ Scope deliberately excludes gameplay mutation, save format, content reduction, shaders and global render settings.
- ⚪ Existing custom SH, transient, tint and headwear blocks remain; this restores batching eligibility only for uniform static renderers without these overrides. Frame Debugger must verify actual submission grouping.
- ⚪ Contact scans, transient source reconciliation, equipment and fog continue on active dirty refreshes. Owner recipe work is incremental; no claim that the entire refresh is O(changes).

Files planned: presenter, contact helper, render surface, new regression tests with Unity metadata, this document; root owns caller integration.

### First GREEN correction

Central run: 25/27 rendering cases passed. Both failing neighbor cases used `Overworld.10.14.0` (Tally), whose TentWall recipe is a fixed family (`SpawnRing3DRecipes` civic-quartet branch). The asserted connection-dependent geometry did not exist there. Corrected the test fixture to `FirstTentCompositionPlan.ZoneID` (`Overworld.5.17.0`), whose `TentNeighbors` branch actually switches corner/straight geometry, and added an explicit old-vs-new recipe precondition. Production was unchanged for this test-premise correction. Central rerun passed both corrected cases (job `6579a486ab3847f4bdd049c273825e6e`).

### Independent graphics setting integration

After four missing-API REDs, added `SyncConfigured(Camera,bool,float,bool)` with independent finite `.5–1` world resolution and shadows. Existing `Sync(Camera,bool,bool)` delegates its exact old low-detail mapping, preserving reflection callers and legacy tests. Both village and spawn-ring presenters consume effective `Village3DSettings.WorldResolutionScale` / `ShadowsEnabled`. Settings/prefs/menu ownership belongs to the HUD agent. Contact and decoration still use LowDetail; the borrowed HUD target stays full resolution. Central surface-setting run passed 4/4 (`dcd7bb69e60e4e2186ba72ec5e2c4dcf`).

Independent read-only cross-feature review found no actionable regression in nullable setting overrides and saved-key reset, effective resolution/shadow consumers, pause submenu input and bounds clearing, hotbar retained mutable-input handling, or sidebar direct-vitals parity against InventoryScreenData. This source review supplements the central tests; it does not establish visual or GPU performance parity.

### Broad-suite compatibility correction

The existing adversarial suite exposed four real REDs in job `e2b7486c6ca147138ec56075d2a974eb`: live actor glyph reskin, two takeability ownership switches, and render-hidden batched removal all use public `Refresh(light, emptySet)` after mutable owner fields change without membership revision. Treating that historical input as a visibility guarantee broke its contract. The complete recipe pipeline spans many independently mutable parts; a partial property fingerprint would only hide the immediate examples.

Correction plan: public `Refresh` with null or empty dirt performs conservative owner reconciliation. The explicit `RefreshVisibility` entry point conveys the caller's positive knowledge that only visibility/light changed; it retains zero recipe resolution with unchanged membership. Nonempty known dirty sets remain incremental. Caller integration uses the explicit entry point only for positively classified visibility refreshes. Existing adversarial tests remain unchanged; their failures supply the RED gate for this fix.

Implemented that split. The new reveal work-count case now invokes `RefreshVisibility` by name and still asserts both exact reveal state and zero recipe resolutions. Its former public-empty-input premise was incompatible with the pre-existing public API. No assertions were weakened; the four original regression cases are unchanged. Central rerun `4546aed9331140e6bdcd01783f170b35` passed all 58 selected existing `SpawnRing3DAdversarialTests` and new `SteamDeckNativeRenderAdversarialTests` cases.

## Next-slate investigation, read-only

Status: candidates only; wait for the root-owned post-first-slate profile before selecting or implementing. The entries below are source-observable repeated work, not measured bottlenecks or estimated savings. No further production or test edits have been made for them. The HUD agent separately owns the hidden glyph/overlay paint design in `STEAM-DECK-PERF-NATIVE-PAINT.md`.

| Candidate / source evidence | Smallest plausible change after measurement | Correctness / test boundary |
| --- | --- | --- |
| `NativeZone3DRenderSurface.UpdateFog` samples 2,000 cells and uploads/applies the same texture on every refresh, including identical light/fog. | Keep exact color sampling; compare the packed `Color32` pixels and upload only if any byte changes. This avoids inventing a fog or light revision and can be independent of owner reconciliation. | Initial texture, null zone, direct visibility/explored edits, FullReveal, light-only edits and zone-reference replacement must publish exact pixels. Count real uploads, not samples; retain disposed/missing-resource behavior. A render refresh is not automatically a fog change. |
| `ReferenceGladeGroundContact.Refresh` constructs every placed footprint before comparing its existing signature. Active contact still scans every committed recipe. | If contact refresh is material but unchanged rasters dominate, collect compact contributor descriptors/signature first and expand footprints only on signature change. Keep disabled suspension and full current visibility checks. | No scan-skip based only on EntityVersion: direct visibility/render/glyph mutations exist. Changed orientation/position/model, fog and resume must match the current raster byte-for-byte; contributor and footprint-expansion counters should distinguish saved work. If raster/upload dominates instead, this candidate is probably too small. |
| `SpreadTransientVolumes.Refresh/Update` performs source reconciliation and rewrites meshes, transforms, layer, material/block and enabled state for every accepted current sample. | If native refresh time remains material in gas/hazard routes, retain source scanning but skip exact unchanged submissions only after validating the owned view's current state. Cache a compact shape key rather than repeated shape-string construction if allocations support it. | Existing hostile hierarchy/material/indexed-block recovery is intentional. A sample-only equality test would miss external corruption. Removal/capacity reuse, native water suppression, hidden fog, density bands, color and moved sources must retain exact fallback and repair. Full reconciliation remains until a complete source revision exists. |
| `RefreshEquipment(true)` and `RefreshHeadwearCover` run for every native refresh; the equipment subsystem already has a version-aware nonforce mode. | Investigate only if equipment time is visible in profiles. A trusted visibility-only route could potentially use nonforce synchronization; geometry/public refresh remains conservative. | Raw mutable inventory/body/equipment edits are explicitly covered by force refresh. EquipmentChangeBus alone does not cover this compatibility contract. Headwear blocks must preserve transient/SH/tint/indexed data. Do not broadly substitute false for true. |
| `SyncConfigured` reapplies camera/light/composite transforms/projection and active state every LateUpdate, even when borrowed camera/configuration is unchanged. | Investigate native property-set cost only if frame markers implicate it; retain validation and target-loss recovery. | Owned camera/composite hierarchy can be changed/destroyed outside the surface. Source-parameter caching alone would stop restoring required state. Resizes, perspective/null cameras, target loss and pause/quality transitions remain immediate. |
| `AsciiFxRenderer.Render` clears and repaints every frame with live output; coordinator now calls it once unless fresh work was admitted. | Consider per-cell paint diff only if FX rendering remains a measured cost after the first-slate duplicate-pass removal. | Render order, overlaps, numeric readouts, delayed particles, native frame Begin/End cleanup, aura Off state, visual expiry and shader tint all need exact comparisons. No broad retained-frame cache or backend rewrite without a separate plan and RED gate. |

Prioritization should use matched profile attribution: native refresh with fog/contact markers, gas/hazard versus idle routes, equipment counts, and FX active versus empty queues. Hidden glyph/overlay paint already has a separate source plan and earlier measured evidence; avoid claiming any of these secondary candidates is larger until the post-profile says so.

Read-only cross-plan finding: `IsRenderedEntity` is not a fresh recipe validator. Spawn-ring uses cached recipe membership and committed view state (batched output also checks explored); village uses its cached owner view. A Render-event glyph/physics/owner mutation requires explicit current membership and fresh compatibility evidence or conservative fallback. Merely invoking that cached predicate a second time does not meet a promise of post-event revalidation.
