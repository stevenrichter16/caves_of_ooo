# Northern yard ground — independent source review

Reviewed the saved diff in `SpreadExplorationWorksites.cs` and
`MendleafDryingYardTests.cs` on 2026-10-26. This is a read-only source review;
the reviewer did not run Unity or alter gameplay. Root owns native execution
and ordinary visual acceptance. Native GREEN was still running at review time.

## Result

No blocking correctness finding in the reviewed change.

- Ground selection follows the already accepted functional origin/rotation.
  The optional-content comparison tests this independently of the new offsets.
- The exact northern `drying` branch alone appends ground. Non-drying stores
  retain their cache at `positions[specs.Count]`: the ground range has zero
  length, so `Skip(yardStart).Take(0)` never adopts the cache as a substrate.
- Ground entries are excluded from route obstructions and functional-owner
  approach requirements. They remain subject to dry placement, distinct-cell
  and arrival-distance checks. Existing shelves, walls and threats retain
  their old obstacle semantics.
- `DryingOwner` rechecks intended ground semantics after all factory callbacks,
  before receipt consumption, and through `DryingShape` at final validation.
  Physics solidity, the Solid tag, closure, hazards and cultivation are checked.
- The returned final closure now proves exact original substrate state and
  rechecks reservations, interior status, tile states and current cell contents.
  It excludes only the exact staged floor reference; it does not trust a role
  label to ignore unrelated owners. `finalPacket` separately pins the floor's
  identity, location and state.
- Floors use the existing staged packet, added-owner accounting and guarded
  rollback. Original Grass is preserved. At most 20 extra ground owners are
  introduced, below the packet's existing 128-owner graph limit for this yard.

## Bounded follow-up coverage

The 15 new cases cover both axes, optional content, other-zone exclusion, six
factory corruptions and four final-state changes. Two worthwhile additions
before declaring the entire delivery accepted are:

1. Add a live tile-state/coating change to the final-state test cases. The code
   explicitly rejects it, but owner snapshots alone do not cover cell hazards.
2. Extend the existing native yard persistence fixture to compare the new ground
   owners' IDs, positions and count after cached return and save replacement.
   Existing persistence assertions prove the service, shelves, medicine and
   forage outcomes; they do not explicitly assert the new floor owners.

These are verification gaps, not reproduced production failures. Actual cold
seed 64/65 gameplay captures must still judge whether the subdued footprint
improves the yard hierarchy, and whether its road-colored cells remain readable
under entities and selection. Cached older yards are intentionally unchanged.

## Actual image review — bounded footprint acceptance

Inspected `seed65-composition-resume-still-court.png` and
`seed65-composition-ground-court.png`, plus their JSON receipts. Both show the
same still at (69,17), the player at (69,16), and the actual 1280×800 game view.
The earlier journey is tick3130; the new cold-generation replay is tick1370.
Time of day, creature positions, explored memory, inventory and prior terrain
damage differ. This is a local composition comparison, not an identical-state
combat, lighting or performance experiment.

The named defect is improved: previously the still, paired shelves and broken
windbreaks read as separate objects on uniform green ground. The new muted
olive pad joins the still's working space to the shelf aisle. Its short northern
extension gives the small service group a visible approach without a waypoint
marker or another bright sprite. The gold player/still and shelf outlines remain
legible against the ground; the outer vegetation and harvested areas retain
their green setting. At this scale, the footprint is visible but subordinate to
the functional objects. No new screenshot-visible obstruction or covered owner
was identified in this bounded comparison.

The scene still contains prominent repeated hedge bands and broad quiet green
space. Those wider composition concerns are not resolved or accepted by this
small change. This assessment accepts the shared working footprint only, not
the complete northern region, enemy readability, danger balance or Deck output.

The `ground-crossed.json` and `ground-reloaded.json` receipts agree on all 18
visible yard-ground IDs and their positions (6953–6970), with player (73,17),
tick1440 and HP15 in both. Their ordering changes after reload, which is not an
identity difference. This supplies actual gameplay save/reload evidence for
the observed floor owners and supplements the earlier persistence-test gap;
it is not evidence of identical combat conditions between the before/after
composition screenshots.
