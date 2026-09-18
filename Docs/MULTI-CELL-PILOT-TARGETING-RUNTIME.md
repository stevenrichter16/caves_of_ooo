# Multi-cell targeting and authored runtime

Status, 2026-09-10: targeting/runtime implementation and the four camera-relative
native picking helper cases are verified. Final 56° native acceptance passed all
26 live gates and 315 independent artifact checks. MC26 passed all 416 net added
tests; the only 31 failures match the exact pre-existing baseline. The earlier
successful 81° live run is explicitly historical evidence.
CoO-original. Parent scope is [MULTI-CELL-PILOT.md](MULTI-CELL-PILOT.md); current
[native acceptance status](MULTI-CELL-PILOT-NATIVE-ACCEPTANCE.md) is recorded separately.

## Verification corrections

| Initial assumption | Verified source / decision |
| --- | --- |
| Cell entity lists represent physical presence | `Cell.Objects` is canonical anchor membership; use `Occupants` for physical hits/selection. Owner lifetime/save iteration stays canonical. |
| Occupants preserve render sorting | The merged view is stable and unsorted. `WorldInteractionSystem` explicitly chooses/sorts render layers. |
| All beam consumers already deduplicate | EmberVein has a separate heat pass beyond its beam hit list. Snapshot unique owners before heat callbacks; removed owners are skipped and nested casts get independent snapshots. |
| Every pilot native identity has 16×16 art | MawToad, CopperPipe, TarSeep and SteamVent lacked explicit icons/mappings. Four real Blender-derived 16×16 icons now imported, binary alpha and shared outline validated by the art report. |
| Native floor named Mud exists | It does not; corrected the migration fixture to verified Grass with nonnull preconditions. |
| A fresh ridge has no written tiles | Native LiquidPool writes the two tar pools on AddEntity. Preserve that mechanic; ordinary stone remains free of ambient fen water. |
| Adding heat7 stores7 | ZoneTileState clamps at MaxEnergy2; the upgrade test now pins actual before/after capped state. |
| (38,12) is free for a three-cell pipe | Authored rocks block that assumed lane. Tests and native audit preflight every body cell instead of manufacturing empty coordinates. |
| A saved single-cell hermit can always gain a footprint in place | Old single-cell placement permitted solid stacks. Preflight adoption and retain an overlapping resident unchanged; persist the skipped authored identity. |

## Implementation

Targeting, lines, rites, skills, interaction and destruction read physical
occupancy. Radius and line hit lists deduplicate one owner per invocation;
beam/cone preserve their existing sets. Chain hops use the last owner's body
for contact distance. Creature-only ability gates remain deliberate, while
physical destructible footprints can intercept a projectile on any covered
cell. Empty footprint holes never select the surrounding owner.

The target `Overworld.3.7.0` loads the byte-identical Blender layout contract:
149 native owners, 421 solid cells and 2000 TepuiStone floors. Every scenery
owner has destructibility/material/thermal behavior; native actors retain
combat/AI parts. New durability/handling is attached only to pilot instances,
not global blueprints. The pipe is movable with native hauling but cannot be
taken into inventory or thrown. Spatial rotation stays fixed.

A revision marker on protected native ground at (0,0) makes installation
idempotent even after every authored owner is gone. Legacy upgrades retain
actors, inventory, containers, conversation/landmark identities and tile
writing. Saved contents win footprint conflicts and skipped identities stay
absent. Safe existing CaveHermit residents are adopted without changing ID,
HP, inventory, conversation or position. The world map marks only this cell
as Stump/Foothills, retaining tier3, neighbors, visitation and saved POIs.

## Tests and review

Entries below retain their original gate state. Historical statements that native
execution was pending are superseded by the explicitly dated camera/run evidence
at the end of this document, not by an assumed current-angle result.

- MC02 compile RED preceded targeting implementation; MC03 established green.
- MC05: all25 targeting and22 dedicated targeting adversarial cases passed.
  The adversarial set probes callback movement/removal/addition, nested casts,
  independent identities/invocations, holes, loaded damage and underfoot UI.
- MC06 missing runtime/types compile RED preceded runtime implementation.
- MC07 runtime:9 pass7 fail. Six expected map/sprite gaps; one invalid Mud
  fixture. Map/band repair and four icons implemented only after these REDs.
- MC08 runtime initial16 cases all pass; dedicated runtime adversarial16
  cases11 pass5 fail. Two confirmed bugs: legacy inventory-bearing terrain
  deletion and partial stacked-hermit adoption. Both fixed after RED.
  Three failed hypotheses were fixture/premise corrections above.
- MC08 also exposed missing cached-zone upgrade integration in root-owned
  save integration tests; PrepareZoneForAccess now upgrades a cached pilot
  before returning. Root owns the active-loaded-world save hook.
- MC09: all16 runtime and16 dedicated runtime adversarial cases GREEN;
  the paired root save integration tests also passed.
- MC10: all33 native receipt metadata cases GREEN after MC09 missing-type
  assertion RED.
- MC11 exposed the native output-failure cleanup bug and two missing forced
  creature-movement receipt gates; implemented after their RED. MC12 all115
  owned cases GREEN:47 targeting,32 runtime,36 native metadata/cleanup.
- MC13 full regression:10,885 total,10,801 passed,84 failed, zero C# errors.
  Our confirmed failure was the existing sprite roster pin55 after the new
  MawToad made56. Surgically updated the pin and its explanatory comment.
  Parent/other agents own the remaining baseline, expected and regression
  failures. Native Play execution and final visual review remain pending.

Cold-eye findings (in phase):

- 🟡 Saved Rock with InventoryPart could be removed as terrain, losing owned
  contents. The classifier now preserves InventoryPart; empty Rock remains
  the negative control.
- 🟡 Dynamic footprint attachment to an overlapping saved hermit threw after
  marker installation and left partial adoption. Preflight now skips the
  authored identity without changing the resident or conflicting container.
- 🟡 Cached legacy access repaired its map label but did not install native
  pilot content. Added the scoped upgrade call; neighboring access remains
  unchanged and saved POIs do not invoke settlement regeneration here.
- 🔵 Initial immutable heat snapshot and layer ordering were explicit design
  fixes, tested with reentrant heat callbacks and out-of-order visible parts.
- 🧪 Native captures, subjective look/feel and sustained profiling are pending.
  EditMode results alone make none of those claims.

All new sprite metas are guid-only clones of an existing Environment sprite
meta. Source/destination hashes match, JSON resource equals the Blender source,
and full Assets GUID audit found no collisions after import. No blueprint
JSON was reformatted. Unrelated pre-existing working-tree edits are retained.

## Owned files

Targeting: SpellTargeting, LineTargeting, SkillLine, RiteTargeting,
WorldInteractionSystem, DestructionSystem and Pyromancy_EmberVein.
Runtime: MultiCellPilotRuntime/PropPart/StatePart/Builder, OverworldZoneManager,
WorldMapAuthoring, WorldMap, StumpBands and the resource layout.
Art wiring: four Environment PNGs/metas and EnvironmentSpriteRenderer entries.
Tests: MultiCellTargetingTests, MultiCellTargetingAdversarialTests,
MultiCellPilotRuntimeTests, MultiCellPilotRuntimeAdversarialTests.

## Pool lifetime review follow-up

Native stimulus preflight exposed a real footprint mismatch: TarSeep renewed
oil across its whole body each turn, while initial LiquidPool projection and
removal still used only the anchor. Existing AddEntity relocation also left
the old projection behind. MC15 ran16 dedicated pool/native-selector cases:
14 RED and2 controls passed, zero C# errors. The full run was10,907 total,
10,861 passed,46 failed:31 exact baseline failures,14 new pool REDs and one
camera-pose test owned by the art agent.

Fixed after that RED: project all committed body cells, clear departing cells
on movement/removal/shape changes/transfer/swap, leave holes dry, preserve
another same-liquid pool's shared projection, and use the true old single
cell when a footprint is first attached to a placed pool. Other liquid and
residue layers survive removal. The runtime content assertion now verifies
the exact twelve-cell union of both native tar bodies. Native exact-position
movement chooses a currently dry complete body and path without altering
hazards or forcing RNG. MC16 passed all16 pool/native-selector cases and all390 added tests. Full
result:10,876/10,907 passed, zero C# errors, with only31 exact baseline
failures remaining. Actual native execution is pending.

Pool cold-eye pass during the MC16 freeze: confirmed all mutation preflights
occur before unprojection; dynamic first attachment distinguishes the old
canonical single cell from the newly added part; changing/removing an indexed
part reads committed old offsets. Shared same-liquid owners retain their
coating, while a swap excludes both departing owners and still retains a
third source. Successful overlap moves clear old cells before projecting the
new body; failed moves/transfers do not alter writing. Removal only targets
the source liquid, leaving other liquids, residue, energy and clouds intact.
The shipped tile-change callback only dirties rendering, so no new gameplay
callback mutation path was found. No additional blocker was identified by
this read-only pass; independent peer review subsequently found no blocker in these delegated
hunks or the native dry-body/path selector. MC16 passed all16 pool/native
selector cases and all other added tests.

Historical native Play acceptance with the 81° camera completed successfully in
run `f8a9acbea29d442e9e75c6f04cc71aa6`:26 live gates, zero unexpected errors,
cleanup exit0, exact current-version save/load ledgers, three1080 captures,
and80.005s/25,171 recorded frames. Independent evidence parsing passed315
checks, including all metric recomputations and raw idle/walk observations.
Full details and evidence bounds are in `MULTI-CELL-PILOT-NATIVE-ACCEPTANCE.md`.
No claim of artistic completeness is made by those gameplay measurements.

Final metadata sweep after native completion found3,773 Assets GUIDs and
zero collisions; git diff --check was clean.


## Current camera-relative native picking follow-up

The old native audit assumed a flat cursor cell and a raised physical mesh hit
must share coordinates. That premise does not hold for the requested tilt. MC23
confirmed four missing-selector RED cases, then MC24/MC25 passed the minimum
actual-camera helper and its imported-edge, hole, hidden-cell and disabled-mesh
controls. The helper independently measures a real imported visible nonanchor
edge contact before invoking the game picker exactly once. The original
owner-specific occupancy-hole gate remains unchanged. No world geometry, FOV,
fog or camera state is changed to make the stimulus pass, and there is no fixed
angle constant in the selector.

An independent cold-eye review found no blocker. Production now targets 56°
downward. Final native run `da0e5e856474438e96c7b6436b7cbd42` and its retained
artifacts have passed independent verification, as recorded below. Historical
81° frames and the intervening 66° diagnostic renders are not evidence of the
final 56° appearance. No speculative lighting change was made from the controlled
GPU diagnostic observations.


MC26 final full EditMode gate: 10,933 tests, 10,902 passed, zero skipped, zero C#
compiler errors. The 31 failures match the exact pre-existing baseline; all 416
net additional tests passed. The four camera-relative native picking helper
cases remain GREEN. Final 56° native execution followed this gate and passed
independent artifact verification.


Final current-camera run `da0e5e856474438e96c7b6436b7cbd42`: 26/26 live gates,
zero unexpected/fatal errors, cleanup exit 0 with private save-root removal and
all restoration receipts true. Three actual 1920×1080 GameView captures and
28,601 frames across 80.005094708 seconds were retained. Each walk phase completed
25 ordinary keyboard movements; idle position and tick stayed unchanged. Exact
save/load owner and tile ledgers matched with 148 remaining unique owners.
Independent raw-artifact parsing passed all 315 checks again after the launcher.
The measured raised ridge contact belonged to physical (8,2), while its projected
cursor lay over flat (8,1), so the updated native picking gate was nonvacuous.

Frame means were 2.62–2.98 ms and p99 values 3.59–4.19 ms in this uncapped Apple
M5/Metal Editor run. Real maxima reached 616.37 ms and 397.60 ms; restored idle
also had a 61.29 MB peak GC allocation sample. These include Editor/UI/observer
work and do not establish uniformly smooth production performance, a causal
camera improvement, whole-world coverage or artistic completeness. Detailed
phase values, raw SHA256 and reproduction instructions are in the native
acceptance document. This final review changed documentation only.
