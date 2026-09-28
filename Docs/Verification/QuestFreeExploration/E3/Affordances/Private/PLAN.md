# Restrained current-action affordances

Status: source sweep; private test-first slice. Root owns Unity, native RED, publication and visual acceptance. F8 paused at its source audit.

## User-facing contract
One muted pair of corner brackets on one currently visible actionable nearby tile; one short line at the top of FOCUS: `C, D: harvest` (normal) or `Enter: harvest` (Look). These keys open the existing action menu; they never execute the advertised verb directly. The hint says `menu` explicitly. No pulsing, icon fields, quest compass, remote content search or markers on remembered fog.

Normal selection scans only underfoot + 8 neighbours. Look inspects only its selected cell. Stable nearest/cell/order choice; no priority based on contents/value/rarity. A pile can require the existing everything-here picker. No cue while another UI/modal, FX or scheduler owns input. An unavailable supported action has no actionable bracket. Existing menu remains authoritative and can reject a later changed state or callback veto.

## Sweep corrections / concrete references
- WorldInteractionSystem.cs:73–110 GatherActions fires GetInventoryActions callbacks. Its class summary incorrectly calls all methods pure. Query must not call it, execution, factory CreateEntity, arbitrary effect AllowAction, RNG or lazy conversation loading.
- InputHandler.cs:3653 direction keys are W/A/S/D plus Y/U/B/N diagonals; C then . underfoot is native at3744. Normal Enter uses hotbar. Look Enter opens menu at1957ff. Cue text names menu, not direct execution.
- InputHandler.cs:2599 requires actual adjacency for acting rows; Examine/picker exempt. Core query pins exact current zone/physical cell/Render/Physics backlinks, actual IsVisible and IsExplored, positive living actor and source units.
- FieldHarvestPart.cs:15–53 ready row+valid factory yield, overflow is legal; no guaranteed packed count. HarvestablePart.cs:60–91 finite source/range/product controls; zero-chance harvest is legal and spends source, no yield promise.
- ContainerPart.cs:27 live lock combines old flag and LockPart. Opening even an empty container is legal; query must not reveal contents or use hidden contents to prioritize it.
- DoorPart.cs:33–67 CanOperate calls arbitrary effect callbacks; reproduce only read admission and actual occupancy for close in this query. Never call TrySetOpen. Locked door remains ordinary menu inspect/unlock; no false Open cue.
- WorldCursorRenderer.cs existing XY cell registration/sorting7 overlays NativeZone3DRenderSurface composite sorting3 at its unchanged ground registration. New owned brackets use that layer, no native presenter/material/camera changes.
- ZoneRenderer.cs:1514 sidebar renders every frame but fingerprint controls actual redraw. Add bounded cue field to snapshot/fingerprint; one reserved top-of-focus line, reducing existing focus budget by its wrapped lines.

## Scope and API
`WorldAffordanceQuery.Find(Entity actor, Zone zone, bool focused, int x, int y)` returns immutable nullable `WorldAffordance` (exact Target, Cell, Command, Verb, Hint). `Current(actor, zone, cue)` rechecks current admission before drawing. No saved state or event subscriptions.
Supported initial typed verbs: ready field and ordinary harvest; unlocked container open; permitted ordinary door open/close; sign read; take and chat only if source sweep establishes pure native prerequisites. Unsupported/custom actions remain discoverable in normal menus. Full menu extension parity is not claimed.

Presentation owns only two small line segments/corners and one cloned unlit material; clear on source/visibility/input change, SetZone, pause, disable and dispose. Cue has no collider/picking participant. Native visual gate must view both 2D and voxel composite, camera edges, modal return and spent-source removal; pure tests cannot prove pixels/readability.

## Test-first gates
1. Compile-compatible reflection fixture against absent query: meaningful missing API RED, exact ownership/FOV/reach/depleted/lock/close occupancy/callback-free/RNG/priority controls. Pair current mutation with unchanged controls.
2. Core minimum + isolated .NET GREEN; actual-reference compile. No shared runner mutation.
3. Reflection UI/renderer fixture before production: key/state gates, single marker/current source, modal/zone/disabled/disposal, sidebar hint and fingerprint, unchanged camera/selection. Root executes native RED before shared production.
4. Peer Q1–Q4 and exact preimage manifest. Root native focused GREEN + bounded actual-key screenshot proof. No claim all verbs, field-wide event markers or action success after arbitrary later vetoes.
