# Multi-cell entities and destructible 3D environments

Status: source-verified feasibility audit and proposed implementation plan,
2026-09-10. No gameplay implementation, Unity state change, or new test run in
this audit. Read against the current working tree, including its pre-existing
changes. This is CoO-original architecture; no Qud multi-cell parity is claimed.

## Decision

Yes: a single environmental object or creature can occupy several cells while
keeping one identity, health pool, inventory, faction, effect list and turn
schedule. It needs an authoritative spatial footprint used by every relevant
gameplay query. Scaling a mesh or adding a Unity collider does not supply that
contract. Existing Morrowfast footprints provide a useful partial precedent,
not a general implementation.

Recommend one canonical entity plus a derived cell-to-owner occupancy index.
Do not create a full duplicate entity in every covered cell, and do not merely
insert the same entity reference into several existing Cell.Objects lists.
Neither shortcut preserves the current save, movement and targeting contracts.

Use this capability selectively. Individual large trees, boulders, cisterns,
stalls and large creatures may be whole objects. Long walls, banks and ridges
should retain independently breakable sections wherever local breaches matter.
Those sections can have continuous art and each section can itself span cells.
The grid does not require visible tile seams in the final model.

## Verification sweep: existing behavior and corrections

| Premise | Source-verified finding | Consequence |
| --- | --- | --- |
| Every cell contains one entity | A cell has a stack, `List<Entity> Objects`. Each placed entity has one indexed cell. | Preserve stacking: items, creatures and surfaces can coexist. |
| There is no multi-cell precedent | Morrowfast has 71 authored owners; 12 have footprint arrays longer than one cell, including the 16-cell cistern. Bridges also have a separate support mask. | Reuse the ownership idea; remove zone-specific exceptions through migration. |
| Existing footprints solve everything | `BlockingOwner` supplies selected collision queries; `WithinOwnerReach` supplies selected interactions. Raw `Cell.Objects` still lists anchors. NPC/creature entries are excluded from the footprint lookup. | No general multi-cell creature or consistent spell/contact support exists here. |
| Moved furniture carries its footprint | Morrowfast collision accepts authored furniture at its original anchor; moved stools become a single-cell Physics body. Reach translates authored offsets. | This is specialized behavior, not a general moving footprint contract. |
| Enlarging the renderer makes the object hitable everywhere | 3D picking resolves an owner, often returning its anchor. Combat/contact queries are separate native code. | Preserve the actual contacted cell as well as the owner. |
| A visually combined mesh is necessarily one gameplay object | Ring rendering batches neighboring entities in 10x5-cell patches, while recipes retain individual owners and patch rebuilds use current objects. | Drawing optimization can coexist with independent destruction. |
| Nearly everything is already destructible | Ordinary structural damage requires `DestructiblePart`. Tree, FruitingBody and TepuiWall have/inherit it. Rock, GrainRidge and CopperPipe do not in the inspected blueprint inheritance chains. | Treat broad destructibility as a separate content/mechanics deliverable. These are source findings, not new runtime reproductions. |
| Clearable scene props use ordinary damage | Morrowfast/Felling owner constructors do not add structural HP, Material or Thermal parts. Their specialized Clear actions remove owners directly; some kinds are fixed. | Clearability is not normal breaking, burning or corrosion. Conversion must supply appropriate Parts and lifecycle wiring. |
| Damage applies to everything a spell finds | Several target helpers intentionally select only Creature; line object targeting excludes Terrain/Wall, and tile occupant reactions currently filter Creature. | Occupancy migration must preserve targeting categories. Extending a spell to scenery needs its own explicit policy/tests. |
| Zone.MoveEntity fully validates a destination | It delegates to AddEntity after bounds/closed-archive checks; voluntary collision lives in BeforeMove/Physics, forced callers make their own checks. | Audit every placement/movement mode. Do not silently change all forced movement to voluntary movement. |
| Save/load can store a reference in all covered cells | Cells serialize references, but rebuilding the entity-to-cell map repeatedly overwrites one entry per entity. Removal only clears the indexed cell. | Duplicate cell membership would leave ambiguous anchors and stale occupancy. |

Evidence locations, inspected in this audit:

- [Cell.cs](../Assets/Scripts/Gameplay/World/Map/Cell.cs):55, 94–149, 167–223.
- [Zone.cs](../Assets/Scripts/Gameplay/World/Map/Zone.cs):93, 177–232,
  309–365, 443–466.
- [MorrowfastSceneRuntime.cs](../Assets/Scripts/Gameplay/World/MorrowfastSceneRuntime.cs):37–75,
  77–103, 212–229; [definition](../Assets/Resources/SceneArt/Morrowfast/definition.json).
- [MorrowfastPropPart.cs](../Assets/Scripts/Gameplay/World/MorrowfastPropPart.cs):9–37;
  [FellingSceneRuntime.cs](../Assets/Scripts/Gameplay/World/FellingSceneRuntime.cs):114–120;
  [FellingScenePropPart.cs](../Assets/Scripts/Gameplay/World/FellingScenePropPart.cs):TryClear.
- [DestructionSystem.cs](../Assets/Scripts/Gameplay/World/DestructionSystem.cs):58–79,
  115–179, 198–235, 278–289; [Objects.json](../Assets/Resources/Content/Blueprints/Objects.json).
- [EntityFactory.cs](../Assets/Scripts/Data/Factories/EntityFactory.cs):170–226.
- [SpawnRing3DRecipes.cs](../Assets/Scripts/Presentation/Rendering/SpawnRing3DRecipes.cs):24–59;
  [SpawnRing3DGroundPatches.cs](../Assets/Scripts/Presentation/Rendering/SpawnRing3DGroundPatches.cs):119–181;
  [SpawnRing3DPresenter.cs](../Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs):252–276.
- [SpawnRing3DIntegrationTests.cs](../Assets/Tests/EditMode/Presentation/Rendering/SpawnRing3DIntegrationTests.cs):185–207;
  [MorrowfastBridgeTests.cs](../Assets/Tests/EditMode/Gameplay/World/MorrowfastBridgeTests.cs):9–20.
  Tests were read, not rerun.

## Proposed spatial contract

Names below are proposed interfaces, not shipped APIs:

```csharp
// One entity; one saved anchor; one shape in local grid coordinates.
SpatialFootprintPart // shape/version, orientation, state-dependent masks
ZoneSpatialIndex    // derived cell -> owner/occupied-point entries
GetOccupants(cell, queryKind, results)
CanPlaceFootprint(owner, anchor, orientation, movementMode)
TryRelocateFootprint(owner, destination, movementMode)
FindReachableContact(actor, target, actionRules)
// Contact carries Owner + actual Cell + optional structural section.
```

Keep the existing entity registry unique. Existing `GetEntityCell` remains the
anchor accessor for persistence and other intentional anchor operations; it
must not masquerade as a nearest contact cell. Introduce explicit occupancy
queries and migrate physical-cell consumers. Separate anchored membership from
physical occupancy: a model pivot/anchor can be inside the hole of a ring and
must not create a phantom blocker or target there.

An entity without a footprint retains its current one-cell behavior. A
footprint is a set of offsets, not just a width/height rectangle: a hollow
cistern, bent root or L-shaped counter needs real empty cells. Basic objects
share one mask. Where needed, distinguish:

- physical body/contact cells;
- movement-blocking and sight/projectile-blocking cells;
- interaction surfaces and walkable supports, such as a bridge deck;
- purely visual overhang, such as leaves above an open walking cell.

Use existing tag/Part rules to determine each query's behavior; do not equate
solid, opaque, creature, terrain and targetable. Support is distinct from an
obstruction. Decorative canopy geometry alone never blocks, receives a ground
spell or grants reach. Highlight physical cells on selection and fade overhang
when it obscures the player, so the distinction is readable.

The occupancy index is derived, never a second authoritative entity store.
Add/remove/relocate/state changes update it through one transaction. Readers
must never observe half a footprint. Removing or picking up an owner clears all
its entries. Reusable query buffers must be safe against reentrant damage,
death and callbacks; snapshot before invoking mutating listeners.

Audit raw cell-list access by intent, not by global text replacement. A static
scan found 130 non-comment lines with GetEntityCell across 63 Gameplay files,
156 with GetEntityPosition across 92, and 370 `.Objects` lines across 127.
These are search results, not a count of required edits: inventory lists and
intentional anchor access must remain distinct. Before opt-in ships, keep a
reviewed caller inventory and reject unclassified physical-cell reads.

## Mechanics that must use the footprint

| System | Required behavior and failure prevented |
| --- | --- |
| Placement, movement, rotation | Validate the entire candidate mask before modifying anything; ignore the mover's own old cells, respect other occupants and movement-mode rules. No half-move when a far corner is blocked. |
| Pathfinding and AI | Search valid anchor placements for the actor's shape. Validate diagonal sweeps/rotation, not only the endpoint. Approach a reachable edge of the target rather than its anchor. A large creature cannot squeeze down a one-cell corridor. |
| Melee and interaction | Use nearest legal contact within action reach, respecting intervening walls and the existing diagonal policy. Mouse, keyboard, bump and AI must agree. A hole is not a contact point. |
| Projectiles and beams | Intersect actual occupied cells and return owner plus impact cell. Empty gaps remain shootable. Keep damage categories and pierce rules explicit. |
| AoE and chain attacks | Enumerate touched cells, then deduplicate owners per damage pulse/hit. Beam already has a seen set; radius helper does not. Chain jumps cannot hop between cells of the same owner. |
| Gas, liquids, fire and terrain reactions | Discover contacts anywhere the body actually touches. Aggregate repeated exposure from one field/effect family deterministically; avoid automatic footprint-area multipliers. Different effects and deliberately separate hits remain distinct. |
| Turn/effect processing | Schedule one brain and tick one effect list per owner. Cell contact notifications do not become extra turns or duplicate fuel consumption. |
| Destruction/death | Keep object and creature paths separate. Fire the owner's veto and terminal events once, spill contents/rewards once, remove all occupancy and visual fragments. Preserve killer/source/faction/quest attribution. |
| Loot and wreckage | Default to one spill/drop batch at a valid contact/anchor fallback; never clone loot per covered cell. Debris distribution may be visual. Larger yields require content balance, not implicit area multiplication. |
| Rendering, FOV and light | One owner view or owner-tagged fragments; clip by visible cells so an unseen anchor does not hide a visible edge, and one visible edge does not reveal the hidden body. Dirty old/new footprints and visual extents; refresh visibility when opacity changes. |
| Inventory and handling | An allowed pickup stores the owner once and clears the whole world footprint. Dropping restores its shape only if the full placement is legal. Hauling, pushing, throwing and teleporting need mode-specific whole-footprint validation. |
| Save/load and transitions | Save canonical owner, anchor, shape/version/orientation and actual mutable state once. Rebuild occupancy after entity bodies and relevant scene state resolve. Destroyed objects must stay absent after zone revisit or regeneration/migration. |

Supporting source read:

- [PhysicsPart.cs](../Assets/Scripts/Gameplay/Combat/PhysicsPart.cs):95–151;
  [MovementSystem.cs](../Assets/Scripts/Gameplay/Turns/MovementSystem.cs):57–164, 199–227;
  [FindPath.cs](../Assets/Scripts/Gameplay/AI/FindPath.cs):130–169;
  [KillGoal.cs](../Assets/Scripts/Gameplay/AI/Goals/KillGoal.cs):43–64.
- [SpellTargeting.cs](../Assets/Scripts/Gameplay/Combat/SpellTargeting.cs):42–73,
  80–120, 134–149; [LineTargeting.cs](../Assets/Scripts/Gameplay/Combat/LineTargeting.cs):145–198.
- [IObjectGasBehaviorPart.cs](../Assets/Scripts/Gameplay/Materials/IObjectGasBehaviorPart.cs):43–69;
  [TileReactionSystem.cs](../Assets/Scripts/Gameplay/World/Map/TileReactionSystem.cs):349–385;
  [ZoneTileStateSystem.cs](../Assets/Scripts/Gameplay/World/Map/ZoneTileStateSystem.cs):63–78;
  [BurningEffect.cs](../Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs):91–146.
- [FieldOfView.cs](../Assets/Scripts/Gameplay/World/FieldOfView.cs):95;
  [DragSystem.cs](../Assets/Scripts/Gameplay/World/DragSystem.cs):215–218, 382–386.
- [SaveSystem.cs](../Assets/Scripts/Gameplay/Save/SaveSystem.cs):408–428,
  952–975, 1127–1184, 1874–1949.

### Damage and exposure policy

For a 3x2 object, any of its six physical cells resolves to the same owner.
An ordinary 10-damage blast covering all six deals one 10-damage application,
before the existing resistance/hardness rules. A second legitimate strike is
a second hit. Scope deduplication to resolution ID plus pulse/hit identity,
never a global once-per-turn flag that would suppress dual wielding or DoT.

For persistent fields, choose an explicit per-effect reducer (normally the
strongest compatible contact per owner per field-resolution step). Preserve
different gas types, independent attacks and non-damage material reactions.
Do not silently apply this new aggregation to existing one-cell actors.
Environmental source emission is a separate question: authored contact cells
may seed local terrain, but emitting from six cells must not multiply a finite
fuel/liquid reservoir sixfold. Pin both reception and emission in tests.

Default large prop damage is whole-object damage: cracks/char progress on the
model and final destruction removes it together. If chopping a branch or
breaking a section must leave the rest, use explicit structural sections with
their own durability and dependencies. They need not each be exactly one cell.
Do not collapse an entire ridge or house because one cell was struck unless
that is an authored structural-collapse rule.

## Destructibility readiness

| Deliverable | Readiness | Action |
| --- | --- | --- |
| Existing cell-based ring art ownership/batching | 🟢 Foundation present | Preserve it while introducing larger owners. Continuous art does not require merging native entities. |
| Static multi-cell owner/collision/reach | 🟡 Specialized Morrowfast precedent | Generalize to the shared index and migrate special cases. |
| Damage to ordinary authored scenery | 🔴 Incomplete for the user's intended contract | Inventory concrete content; add material-appropriate Destructible/Material/Thermal/Fuel/handling behavior, and route ordinary attacks that should affect it. |
| Whole multi-cell creatures | 🔴 No general spatial contract | Keep current creatures single-cell until movement, AI, hazards and lifecycle coverage lands. |
| Local structural breakup | ⚪ Additional feature | Preserve current section semantics first; add structural dependencies only where content needs them. |
| Entities simultaneously spanning zones | ⚪ Later, explicit scope | First implementation requires a whole footprint inside one zone. Cross-zone handoff is atomic at a valid full destination; otherwise refuse safely. Persistent straddling needs a later multi-zone occupancy design. |

Respect intentional exceptions such as critical traversal sites; enumerate and
explain them in content instead of treating every missing Part as an intentional
protection. Do not globally attach destructibility to the PhysicalObject base:
this would also affect items, terrain and progression objects without reviewing
their aftermath. Keep world substrate and walkable underlayers separate from
props, so destruction reveals the correct ground rather than an art void.

Bridge destruction is an explicit exception requiring an aftermath policy.
Current manual removal refuses an occupied crossing; arbitrary fire/combat
cannot simply bypass that and strand actors in invalid state. Either preserve
the refusal under a documented structural rule or implement supported
fall/water/evacuation consequences as a separately verified feature.

## Implementation sequence

1. **M0 — contracts and reproducible baseline.** Inventory physical-cell reads,
   destructible content, all mutation APIs and scene owner lifecycle hooks.
   Re-establish the current test baseline rather than using historical counts.
   Write failing edge-hit/duplicate-AoE/whole-removal tests first.
2. **M1 — spatial substrate, no content opt-in.** Add validated immutable shapes,
   per-zone index, explicit occupancy/contact queries and transactional
   placement/removal. Old one-cell behavior is the paired control. Malformed
   shapes fail with diagnostics; no silent single-cell fallback for a large mesh.
3. **M2 — complete stationary pilot.** Wire collision, sight, keyboard/mouse
   interaction, projectile/AoE targeting, applicable material exposure,
   destruction and persistence before enabling one large prop. Use an existing
   simple tree/boulder identity with one model; do not invent lore species.
   Add structural/material Parts as content requires. Include renderer removal
   and occupied-edge spell effects. Only then migrate authored scene owners
   and their removal ledgers; preserve their existing state and stable IDs.
4. **M3 — destructible environment coverage.** Classify ring/town fixtures by
   whole object versus local section; add missing damage behavior and aftermath.
   Preserve existing independently breakable cells when grouping their art.
   Four visual variants per repeated family; no glyph-only content. Content
   JSON changes use surgical splices and actual factory parameter tests.
5. **M4 — movable props and large creatures.** Migrate pathfinding, nearest-edge
   combat, swept moves, displacement, status/contact events, hauling/pickup/drop,
   equipment/corpse placement and zone handoff. Fixed footprints initially;
   facing may animate without rotating collision. Physical rotation is a
   separate validated transaction. Use actual existing creature identity only
   where lore and passage dimensions justify the larger body.
6. **M5 — close-out.** Dedicated adversarial sweep; full regression against
   baseline; cold-eye symmetry and source/doc review; deterministic scenario
   with read-only diagnostic evidence; real play look-pass and profiling.
   Keep later partial breakup and cross-zone straddling explicitly deferred.

Each sub-milestone follows RED → GREEN → counter-check → adversarial tests →
self-review → living doc update. Production opt-in stays gated until the pilot
is complete, so partially migrated mechanics cannot be encountered in play.
No global rewriting of existing saves, authored layouts or IDs as a shortcut.

### Save compatibility

The current serializer writes field names but reads values using the current
reflected type; arbitrary future field changes are not automatically safe.
Use supported primitive/array data or an explicit serializer and versioned
migration. Do not assume arbitrary offset structs will serialize: the current
type whitelist does not accept every value type. Missing footprint means legacy
single-cell. Cache/index fields are nonserialized. Rebuild once valid bodies
and scene migrations are available, and ensure load hooks do not query a stale
half-built index. Save-before-destruction, save-after-destruction and loading an
older scene revision all need tests. Preserve unknown saved occupants.

## Required verification and adversarial gate

All items below are **planned, not executed**. Per-invariant pairs must cover
the identical scene with the relevant flag/occupied cell changed. A dedicated
MultiCellAdversarialTests suite should cover roughly 20–60 scenarios selected
from these surfaces, in addition to targeted RED/GREEN tests.

- Any edge of a 3x2 owner can be hit; an adjacent empty/hole cell cannot.
- A blocked far corner leaves anchor, occupancy, turn cost and listeners
  unchanged. Self-overlap during a valid move is allowed; another owner blocks.
- A large body cannot pass a narrow doorway or cut a blocked diagonal; a
  one-cell actor still follows its prior rules. AI attacks a reachable edge.
- Blast touching six cells damages once; a second pulse/strike damages again.
  Chain attacks never bounce to the same owner's other cells.
- Fire/gas/water touching a non-anchor body cell is detected; visual leaves over
  it do not grant physical contact. Independent hazard types remain effective.
- Destruction veto leaves every occupied cell intact. Reentrant destruction
  produces one terminal event and one loot batch; neighbor owners survive.
- Removal, corpse conversion, harvest, pickup, teleport and zone unload leave
  no stale references or ghost barriers. Inventory stores exactly one owner.
- Save/load preserves shape, damage, effects, inventory and missing owners;
  old one-cell saves remain valid. Invalid shape/unknown version has a defined
  refusal/migration path, never an invisible blocker.
- A visible far edge with hidden anchor can be selected; unseen fragments stay
  hidden. Mouse, keyboard and native sprite fallback agree on owner/contact.
- Deleting a batched object rebuilds affected geometry only, preserves nearby
  models and reveals the actual underlayer. Opacity changes update FOV/light.
- Roof/support/door changes, overlapping nonblocking contents and a bridge
  occupied at destruction time follow explicit policies.

## Performance and diagnostics

Follow [PERF-FOUNDATION.md](PERF-FOUNDATION.md): index by the zone's 2000 cells,
update only old/new footprint entries, reuse buffers, keep miss paths cheap,
and invalidate affected patches/visual extents. No full-world or all-owner scan
per cell. Account for potentially expensive shape-aware A* and dense occupancy;
measure before choosing further caches. Do not globally share scratch buffers
across callbacks that may reenter. Ordinary one-cell queries need a fast path.

Emit stable diagnostic records for placement/move refusal and success,
footprint rebuild/removal, contact resolution and damage deduplication where
needed. Include owner ID, anchor, shape/version, actual contact and reason.
Pin emissions in tests. Profile actual dense fen, ridge and village play for
60–90 seconds, reporting p95/p99/max, allocation and geometry rebuild counts.
No performance guarantee is established by this audit.

## Cold-eye review of this plan

- 🟡 Addressed: existing multi-cell collision is real, but specialized. Do not
  present this as either a wholly absent feature or a solved generic one.
- 🟡 Addressed: repeat damage is not uniformly absent/present; beam deduplicates,
  radius does not. Proposed operation-scoped dedup preserves legitimate hits.
- 🟡 Addressed: one index cannot conflate support, opacity, contact and canopy.
  Anchors outside physical masks require filtered occupancy queries.
- 🟡 Addressed: shape support does not automatically add object destruction or
  change Creature-only spell semantics. Content coverage is a separate stage.
- 🟡 Addressed: save graph bodies, scene caches, removal ledgers and load hooks
  must all agree before publishing occupancy. No serializer compatibility claim.
- ⚪ Deferred: partial structural collapse, simultaneous cross-zone occupancy,
  live feel/performance and full caller migration await implementation.

Audit limits: static source/data inspection and existing-test reading establish
these code contracts. No failing tests were authored/run, so projected failures
of a hypothetical implementation are risks to pin, not claimed reproduced bugs.
This feasibility pass does not claim an exhaustive audit of every game system.

## Work log and files changed

2026-09-10: read project methodology, spatial/lifecycle/targeting/material/save
and presentation seams; inspected native owner data and blueprint inheritance;
documented corrections, design, staged rollout and acceptance gates. Added this
document, linked the art-direction contract and appended today's work log.
No runtime code, sprites, meshes, blueprints or saved game state changed.
