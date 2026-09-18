# Morrowfast first-ring renderer architecture review

Status: **proposal and source review only; no repository edits, Unity launch, test execution or ring implementation by this reviewer.** Read the current village, Felling, main zone renderer, camera, generation/save boundaries, `/tmp/codex-v3d-ring-survey.md` and `/tmp/codex-v3d-ring-art-strategy.md`; coordinated with both authors. Root owns adoption and native gates. This is CoO presentation architecture, not a Qud-parity claim.

## Recommendation

Use **one new ring presenter with two native adapters**, plus a small reusable render-surface helper extracted from the proven village path. The generated-terrain adapter serves seven chunks; the Felling adapter serves its named native owners. Keep `Village3DPresenter` and Morrowfast's strict manifest intact as the first consumer while extracting only camera/render-target/composite/fog/material ownership. Do not make eight copied presenters, relax Morrowfast validation into a generic manifest, or instantiate the eight preview FBX scenes as the player's world.

The eight editable Blender scenes and hierarchical preview FBXs remain required art deliverables. They represent identified actual native snapshots. Runtime assembly must read the **current generated or loaded graph**, which may have different plants, removed ore, opened containers, furniture, actors, equipment, water or stairs.

## Verified native scope

| Direction | Exact surface zone | Adapter and identity |
|---|---|---|
| NW | Overworld.2.5.0 | Native terrain/entity adapter; Stump Foothills, CascadeGorge |
| N | Overworld.3.5.0 | Felling adapter; actual `FellingSceneRuntime` owners/state, not generic Slopes |
| NE | Overworld.4.5.0 | Native terrain/entity adapter; Stump Slopes, ButtressRidge |
| W | Overworld.2.6.0 | Native terrain/entity adapter; Grovelands CompostingField |
| E | Overworld.4.6.0 | Native terrain/entity adapter; Grove then Olderdeep surface mouth |
| SW | Overworld.2.7.0 | Native terrain/entity adapter; Grove then Ginmere surface mouth |
| S | Overworld.3.7.0 | Native terrain/entity adapter; TendrilFen |
| SE | Overworld.4.7.0 | Native terrain/entity adapter; Grove |

These routes/formations are **source-verified**, not this reviewer's generated observations. No world road or river flags exist in these eight; local water/routes do. The native survey must precede the eight ImageGen references. Olderdeep's founding settlement and Ginmere's drowned sima are below this surface scope. No new threat balancing follows merely from changing the art; nearby tiers currently remain3–5.

## Minimal shared infrastructure and separate responsibilities

Suggested names are proposals, not existing APIs.

| Component | Owns | Must not own |
|---|---|---|
| `NativeZone3DRenderSurface` (small disposable composition helper) | One active XZ camera; owned RT and XY compositor; 80×25 point-sampled fog texture; per-source-material clones; sun; source-camera sync; detail/visibility; cleanup | Native zone generation, entity creation, save fields, collision, room logic, model selection |
| `Ring3DModelCatalog` | Validated imported model recipes, material families, pivots/extent/rig metadata, explicit supported blueprint/state mappings and fallbacks | A frozen native entity list or an idealized layout for every seed |
| `Ring3DPresenter` | Actual-zone binding, prepared native views, reference-keyed ownership/picking, dirty refresh, actor hooks, presentation eligibility/failure | Native movement, opening, harvesting, spawning, retagging, ecology, traversal or save repair |
| Generated-zone adapter | Current cells/entities/TileState → floor patch and independent entity-view requests | Re-running formation builders or reconstructing missing saved features from a seed |
| Felling adapter | Exact current named owners, their presence/current coordinates, Felling state and landmark geometry; actual fauna/dressing separately | Morrowfast rooms/roof semantics or source-pixel transforms reused as a general grid mapping |

Extract the helper with behavior-preservation tests and the current village native gate before adding ring behavior. Avoid an inheritance framework or general renderer service locator. A tiny common presentation contract becomes useful now because there are two actual 3D consumers: current zone, readiness/visibility/failure, bind, refresh, claims, rendered-body predicate and picking. It need not migrate legacy 2D presenters into a hierarchy.

Suggested bounded ring surface:

```csharp
void Bind(Zone zone, Camera sourceCamera);
void Refresh(LightMap light, IReadOnlyCollection<int> dirtyCells); // null = full native refresh
void SetPresentationVisible(bool visible);
bool ClaimsCell(int x, int y);
bool IsRepresentedEntity(Entity entity);
bool IsRenderedEntity(Entity entity);
bool TryPickWorld(Vector2 legacyXY, out Entity owner, out int x, out int y);
bool RepresentsPermanentWater(int x, int y);
```

Do not retain the caller's dirty-set reference after Refresh; `ZoneRenderer` immediately clears it. Exact current `Zone` reference, not just matching ID, gates every ownership/hook/pick decision. Only the eight z0 IDs qualify. Other chunks, world map, and sinkhole floors keep existing rendering. Unsupported pipeline/material or incomplete required ground assembly leaves the original renderer active and makes no claims.

## Geometry follows actual state

- Enumerate `zone.GetReadOnlyEntities()` / `GetEntityCell` and actual `Cell.Objects`. Key mutable views by entity reference; use durable IDs for saved identity/diagnostics, never as a substitute for alias checks after load. Not every entity has a non-null ID. `RenderPart.Visible`, native part state and current membership remain authoritative.
- Ground recipes use actual terrain blueprints, blocking/opacity, interior status and TileState. There is no native per-cell heightmap; visual ledges cannot introduce a new collision or imply a missing passage. `GenReservedCells` is expressly not serialized and cannot define loaded geometry.
- Start with small material-grouped terrain patches (approximately5×5 cells) and cache the contributing cell state. A dirty cell rebuilds its patch and needed seam neighbours only. Keep walls/trees, harvestable ore, compost caches, sundews, containers, stairs and other mutable objects separate from static ground batches. A creature-only enumeration misses WineLeafSundew.
- Stable cosmetic variants can hash zone ID + cell + recipe with an explicit stable hash. Do not consume gameplay RNG; do not use `string.GetHashCode()` as a cross-process art identity. Native generation itself currently uses `WorldSeed ^ zoneID.GetHashCode()`, so record real snapshots/runtime versions rather than promise seed-only portable scene equality.
- Seven procedural chunks can share recipe/model families while retaining their actual formation silhouettes. Missing required model/material mapping must not be silently replaced by a misleading attractive structure. Unrecognized real entities retain existing sprite/glyph fallback and a bounded diagnostic. Supporting that fallback does not fulfill the promised named creature art by itself.
- The current `Village3DPresenter.PrepareModel` maps all non-water materials to one village material. Do **not** reuse that remap for a new atlas. Map explicit imported material families to owned clones, each with the same instance fog binding. Keep unsupported materials fail-loud and source assets untouched.
- Reuse `Village3DEquipmentViews` only for actual compatible biped rigs/library recipes. Frogs, snakes, tortoise and birds do not get fake hand sockets. Unknown gear must retain diagnostic native identity; a hidden/fallback attachment must not alter equipment.

## Dirty updates and lifecycle

`ZoneRenderer` already owns native invalidation. Full refresh computes FOV/light; partial refresh receives its dirty-cell set; `ZoneTileStateSystem.BindRenderHook` connects only the active zone's tile writes. Pass those surfaces through rather than adding a second static dirty subscriber or replacing the tile-state callback.

On full refresh, reconcile native membership and state plus upload fog/light. A full FOV change does **not** imply rebuilding all geometry. On partial refresh, reconcile affected old/new cell memberships, changed props/coatings and patch neighbours. `Zone.EntityVersion` is a useful coarse membership-change signal, but increments on moves and does not cover arbitrary tag/part changes; it is neither a full visual-state version nor a reason to rebuild2,000 cells each frame. Combine existing invalidations with cached recipe-state comparison. Measure any roster scan explicitly.

`LateUpdate` should remain camera/RT resizing, already-active visual animation and cheap change checks; avoid an all-zone mesh rebuild or unbounded per-frame allocation. Existing Village full rendering currently calls `Refresh(_lightMap)` around `ZoneRenderer:992` and again through `RefreshVillagePresentation` around1024. Do not copy this duplicate fog/view work into a new path; a shared coordinator can resolve once before fallback sprite suppression, with a counter test if deduplicating the old path.

On zone switch/F6, destroy or pool only owned views, clear all reference maps, detach only own multicast handlers and bind the new graph. Clear old interpolation/actions and selection. Disable both RT camera and composite during fullscreen menus, settings-off, teardown or failure. One active 3D surface is sufficient; simultaneous village/ring roots on shared WorldLayer12 could otherwise appear in each other's camera. Disabling only the composite is insufficient.

## Exact integration traps in current source

1. **Felling exclusivity:** `ZoneRenderer.SetZone`, unpaused LateUpdate and RenderZone independently bind/show Felling scene and dressing. When ring Felling3D is ready, hide *both* legacy presenters. Failure/off must restore their former behavior. Do not leave their claimed cells/owner predicates active under the 3D result.
2. **Native light on fallback bodies:** `ResolveEntityVisualTint:1448` currently skips CPU attenuation for every authored scene except `Village3DPresenter`. Ring3D needs to join the “native-lit3D” decision; otherwise fallback creatures/items get white daytime tint in dim ring cells.
3. **Actual unknown-entity fallback already exists:** `EnvironmentSpriteRenderer.ResolveCell:1017` lifts unrepresented runtime sprites or the real glyph above order3 authored ground. Reuse this exact path and preserve its precedence. This fallback represents the native-selected top entity, not every stacked occupant simultaneously; test native stack selection/menu access rather than inventing a new all-items-visible contract. Only claim a native entity actually represented by a valid view; a broad `Terrain`/`Creature` tag claim can hide missing content. Missing all ground/library means whole-presenter fallback, not an opaque black RT covering the old map.
4. **Water duplication:** `IsAuthoredRiverWater:1257` suppresses a mark only for one permanent water coating plus Felling/Morrowfast-tagged pool terrain. Ring water needs the represented-water query above, based on current actual state. Suppress only the specific permanent water already drawn; temporary spell water, oil, ice, residues, charge, heat, cold and clouds must stay visible. The fine-water/animated-overlay paths also need a visible-frame regression, not just a source predicate assertion.
5. **Pause/mode/current-zone gates:** add the ring to disable, pause, body exclusion, claims, picking, failure and re-enable paths together. Repeated F11/Shift+F11 across village→ring→Felling→ordinary zone→F6 is the relevant player flow. Existing settings keys can continue as the view preference; do not introduce conflicting controls.

## Camera, FOV/shadows and picking

Reuse one-unit XZ projection: `(x+.5, height, 24.5-y)`. The existing XY main camera remains the input/UI framing authority. Ring wilderness should use ordinary tracked-player/Look-target framing and zone clamps; it must not be forced into Morrowfast's37.5-unit crop/letterbox. Keep the orthographic overhead camera and square cells. RT resolution follows the actual map pixelRect, including sidebar/hotbar and lower detail, without changing logical projection.

Felling requires an explicit framing decision: existing 2D art projects seven extra top rows and observes its monumental facade from a special three-cell front band. These are source-art accommodations, not generic80×25 world geometry. A new physical3D model should remain inside approved native-ground extents and below the camera, or receive a separately validated visual-only facade ownership policy. Blindly copying its overhang rows into the standard fog map, raising the camera beyond effective shadow distance, or sampling out-of-zone geometry as visible is incorrect. Keep the old Felling framing until its new adapter/camera tests replace it; no stealth village crop for the north chunk.

Use the tested per-fragment fog texture: alpha0 unseen, .5 remembered,1 visible; transients and shadows require current visibility. Memory bypasses live lights/ripple. Ground/fixed terrain can use native remembered policy, while actors/items and mutable transient visuals must not leak through a remembered cell. `Village3DVisibility.SampleCell` already uses the real LightMap and biome/band ambient; do not bake all eight chunks into identical village daylight. Decorative glow never grants LightSourcePart or discovery. Overhanging foliage must not reveal unobserved adjacent fragments or make a native open passage unreadable.

Picking resolves actual native refs and current anchor cells; view colliders are triggers/selection only. Preserve `ZoneRenderer`'s unknown item/actor precedence in the clicked cell. Filter picks by the adapter's owned collider map, current zone, membership, draw visibility and native visible target; hidden/memory/dead/removed owners do not win. The current64-hit ray buffer needs a non-vacuous dense-foliage overlap control or a bounded alternative, since an arbitrary saturated hit subset can omit the intended owned collider. Never add colliders to cosmetic clutter or terrain just to make every leaf selectable.

## Pre-art and regression gates

Native source survey first: actual nine-zone dump (centre plus ring), exact2000 cells each, resource/runtime hashes, actual entity/part frequencies, owners, TileState, connections and border arrival results. Capture seed729490642 as the reference seed and at least two additional fixed seeds (for example64 and1729). Each of the eight reference images must cite its actual snapshot; no fabricated deterministic Python reimplementation of Unity generation.

Minimum automated matrix is **8 zones ×3 seeds**, then generated→serialized→fully applied loaded graphs for each. Include these additional state controls rather than treating one pristine seed as sufficient:

- UrquActive off/on on a newly generated slope zone; EcologyDamaged off/on for relevant foothill fauna. Existing cached zones must not gain creatures merely by changing presentation.
- Native tree/ore/cache harvest and removal; real moved furniture/haulable; dropped item/corpse; added or removed actor; changed equipment; repeated refresh and off/on. Assert exact entity counts/refs/inventory/HP/turn/connection state unchanged by render calls.
- Felling all55 source components/39 mutable entries as applicable, current owner disappearance and preserved bare/seventh positions; real fauna/dressing separate. No synthetic replacement after full load.
- Sinkhole surfaces before/after belowground visit, saved stair/connection records, vanished generation reservations; no recreated lip/water/void from preview data.
- TendrilFen permanent TileState water before/after real serialization. Survey found a source persistence mismatch; obtain its RED and native fix as a separate gameplay slice. The renderer must display loaded truth, **never rebuild visual water to conceal lost state**.
- Missing model/material, missing library/unsupported renderer, unknown entity, outside-ring and z1/z2 controls; clean visible fallback and no stale claims/cameras/selection.
- FOV/shadow GPU boundaries for tree/canopy, water, tall Felling geometry and memory; actual unknown fallback sprite light, transient FX, water-mark precedence and interior/menu overlays.
- Real shared borders: the3×3 set has12 horizontal/vertical pairs,24 directions. Check generated and loaded routes using the real transition resolver; arrival may move up to10 cells. Use art continuity consistent with those actual edges, never silently carve a prettier road. Diagonal neighbours are reached via cardinal travel, not a new diagonal chunk command.

Native live acceptance should traverse the ring through ordinary input and inspect representative interaction/save/load flows, with actual GameView captures for all eight and at least one distinct layout seed. Performance needs the same75-second workload on a representative dense forest/Felling and water-heavy fen, with raw frame percentiles/max, allocations, work counts and bind/dirty rebuild counts. Keep 3D off/on runs seed/session-state matched; different loaded geometry or changed approach turns are explicit limits. No full-ring speed/60fps claim can be inferred from the village's existing profile.

## Suggested implementation sequence

1. Adopt survey/exporter; run genuine generated and loaded controls, resolve observed native water persistence before dependent visual claims; archive actual masks/borders.
2. Author eight separate references and approve their native reconciliation in the living plan; build shared model families and eight snapshot scenes/export receipts.
3. Extract the small render-surface helper under village preservation tests. Add ring catalog validation and exact eligibility/fallback REDs.
4. Ship one generated chunk end-to-end with dirty updates/native ownership/FOV/picking and real screenshots; add the remaining six procedural styles via data/model coverage, then Felling's specific owner/framing adapter.
5. Run the complete seed/save/state/border matrix, native ring travel, actual images and representative performance pair; review/fix and update living docs with all deviations.

No ring assets, native snapshots, production classes or successful runtime results are implied by this proposal.
