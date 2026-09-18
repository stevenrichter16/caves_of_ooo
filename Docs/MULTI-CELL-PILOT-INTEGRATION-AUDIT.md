# Multi-cell pilot integration audit

Status: transitions and the 30-case core adversarial gate verified GREEN;
40 ability contact/mutation/boundary cases and 60 selector cases verified;
16 movement-entry/callback fixes await regression (56 ability cases total), 2026-09-10.
This supports [the south-spawn pilot](MULTI-CELL-PILOT.md); it is not an
exhaustive audit of the entire game or a claim of a passing implementation.
The design is CoO-original. Existing Qud-inspired single-cell behavior is the
counter-check, not a new parity claim.

## Verified corrections and implementation seams

Source locations below refer to the pre-pilot implementation; concurrent
implementation may shift line numbers. Canonical source copies are preserved
in `/tmp/coo-multicell-before-20260910`.

| Surface | Source-verified behavior | Minimum pilot integration |
| --- | --- | --- |
| Horizontal transfer | `ZoneTransitionSystem.cs:131–150` finds an eligible single cell, removes the player, then ignores the boolean result of `AddEntity`. | Search complete candidate footprints and use a transaction that preserves the source on destination refusal. Check `ExcludeZoneArrival` on every physical cell, not an empty anchor hole. |
| Vertical transfer | `ZoneTransitionSystem.cs:378–423` searches tagged stairs or a nearby fallback, then has the same remove/add sequence. | Apply the same whole-body arrival predicate to matched stairs and fallbacks. No eligible placement means no mutation. |
| Followers | `ZoneTransitionSystem.cs:212–218` chooses an adjacent one-cell position, transfers, and updates `BrainPart.CurrentZone` without checking destination addition. | Search with each follower's footprint; commit brain-zone state only after transfer. Other-zone followers remain untouched. A blocked follower remains in its original zone. |
| Save parser | `SaveSystem.cs:1162` rebuilds anchor indexes before entity bodies have loaded. `ReadEntityBodies(false)` occurs at 408; load hooks at 421; final world rebuild at 426. | Keep early anchor reconstruction. Rebuild footprint occupancy after bodies resolve and before any hook can query it; final migrations must also preserve/update the derived index. Save only the canonical anchor reference. |
| Inventory | `PickupCommand.cs:123–127`, `DropCommand.cs:93–120` and `ThrowItemCommand.cs:332–360` already use transactional item commands and inspect normal removal/placement results. | Full removal belongs in Zone. Whole-body drop/landing validation must happen before irreversible throw damage, and failure must keep one inventory owner. Target/contact reach is still an independent gate. |
| Tumble | `Acrobatics_Tumble.cs:129–139` checks removal and actor move, but ignores the final target addition. | Before large creatures are exposed, preflight both prospective bodies ignoring both participants and reject overlapping results, or add an atomic swap. A refused swap must not lose the target or apply confusion. |
| Pathfinding | `FindPath.cs:140–167` exempts goal-cell passability, validates only the neighbor anchor, and tests two anchor-adjacent diagonal cells. | Search valid full-body anchor placements, preserve single-cell policy, and approach a reachable target contact instead of its occupied anchor. |
| AI melee | `KillGoal.cs:43–64` uses anchor adjacency and anchor pursuit. | Use body-to-body contact distance; large actors attack from any physical edge. A hollow anchor is never a contact by itself. |
| Push/pull | `SkillCombatHelpers.cs:208–295` derives push direction and pull stop from anchors; per-step displacement uses `ForceMoveTo`. | Validate full destination, ignore the displaced owner's existing body, preserve forced movement while stunned, and stop before body overlap with the puller. |
| Entry events | `MovementSystem.cs:333–372` snapshots only the destination anchor and shares a static scratch list. | Notify newly entered physical cells, avoid repeated dispatch to one contacted owner, and use reentrancy-safe snapshots. Stop after the mover is removed or displaced by an earlier listener. |
| Adjacent heat flags | `MaterialSimSystem.EmitHeatToAdjacent` dispatches its 100/8 J neighbor delivery with `Radiant=false`, despite older spell prose calling it radiant. | Count the primary 250 J material pass separately from the legitimate 12.5 J propagation pass. Do not suppress the second pass to satisfy a dedup assertion. |

## Damage, exposure and targeting

Replacing `Objects` with `Occupants` is necessary for many physical readers,
but does not by itself complete these contracts:

| Path | Existing protection or gap | Required test |
| --- | --- | --- |
| `SpellTargeting.TraceBeam` | Already uses an owner `HashSet` at lines 42–67. | Non-anchor contact is discovered and four touched cells return one owner; a separate cast returns it again. |
| `SpellTargeting.GetCreaturesInRadius` | Lines 109–117 append once per cell without an owner set. | One radius pulse damages the large creature once; two pulses damage twice. |
| `SpellTargeting.GetCreaturesInCone` | Already deduplicates owners but rejects solid cells before collecting targets at lines 288–298. Creature-only targeting is intentional. | Solid scenery still occludes correctly, large creatures are found on reachable edges, and extending object damage is an explicit ability choice. |
| `SpellTargeting.FindChainTargets` | Owner visited set prevents re-bouncing, but the next jump starts from the current anchor at line 139. | A next target near the far edge is considered; no hop to another cell of the same owner. |
| `SkillLine.Collect` | Lines 114–130 collect creatures then elemental scenery, no owner dedup. Collection precedes mutation intentionally. | A line through two cells of one non-solid target returns it once, while a creature co-located with scenery retains target priority. |
| `Pyromancy_EmberVein` | Creature damage uses beam hits; its independent per-cell heat pass at lines 76–110 has no owner set. | One beam gives one heat dose to the large object, preserves both creature damage and legitimate heat, and the next beam applies again. |
| `LineTargeting` | First-hit owner and physical impact cell are already separate outputs. Object filtering excludes ordinary terrain/walls. | A pilot destructible segment receives permitted direct attacks at an edge without making every floor tile targetable. |
| `GasSystem.OnTickEnd` | Lines 74–84 process and apply each pool individually. `IObjectGasBehaviorPart.ApplyToCell:64` currently uses anchor objects. `GasPoisonPart:84` has immediate damage on each application. | Same-family gas on several body cells gives one compatible exposure in a resolution; distinct families still act. Repeated turns remain repeated exposure. Merely migrating the per-cell query would multiply damage. |
| `TileReactionSystem` | Its action guard at lines 241–251 is reaction+cell, not owner. Occupant damage is independently applied at 349–385. | Reaction outputs occur in every affected tile, but compatible creature exposure is reduced once per owner/reaction/action. A second action is distinct. |
| Terrain source emission | `ZoneTileStateSystem.SeedTerrainSources:63–78` emits from canonical anchors. | Large finite reservoirs do not gain volume or fuel because of footprint area. Deliberately multi-cell sources require an explicit authored emission policy. |

Use local resolution contexts for owner deduplication, not an owner flag that
lasts a whole turn. Preserve separate hits, multihit weapons, separate casts,
and different hazard families. Snapshot before firing listeners; death,
destruction and displacement mutate the queried world.

## Bounded transition test inventory

`Assets/Tests/EditMode/Gameplay/World/MultiCellTransitionTests.cs` contains 22
parameterized cases authored before transition production changes:

- Complete arrival at all four horizontal edges, paired with four ordinary
  single-cell exact-edge controls.
- A solid or excluded far corner forces another valid complete placement.
- Negative footprint offsets require an inward anchor; a hole at the anchor
  does not create a false arrival contact.
- No complete horizontal arrival preserves source anchor, occupancy and both
  zones' entity versions, for solid and exclusion obstacles.
- Matched stairs skip blocked/excluded far corners; vertical fallback accepts
  a full body and refuses a lone open cell without losing the actor.
- Followers need a complete eligible footprint, preserve their original
  brain-zone relationship on refusal, cannot overlap their leader, and do not
  teleport in from an unrelated zone.

The root implementation owns the shared spatial/atomic-transfer API and the
headless test runner. No independent Unity run was launched by this audit.
The coordinated compile RED included the transition suite's missing
`SpatialFootprintPart`, `GetOccupants`, `GetOccupiedCells` and
`CanPlaceFootprint` APIs. Evidence:
`Docs/Verification/GameSystemAudit/MC02-integration-compile-red.log.gz`.
Compile RED establishes the new API dependency, not individual runtime failure
counts. Production transition edits began only after that run ended.

The transition implementation now passes the actor into every horizontal,
vertical and follower arrival search, validates candidate physical cells, and
uses `Zone.TryTransferEntityTo`. The root-provided candidate-cell view is
`Zone.GetOccupiedCells(entity, anchorX, anchorY)`. Ordinary actors without a
footprint keep the original single-cell arrival predicate, including its
existing distinction between `Solid` tags and ordinary creature Physics.
Stair-marker scans deliberately remain canonical anchor reads.

Pre-GREEN self-review:

- 🟡 Addressed: complete destination validation must precede source removal;
  all three transfer call sites now use the shared transaction.
- 🟡 Addressed: body exclusions and holes must be symmetric across horizontal,
  vertical and follower paths; all use one `CanArrive` helper.
- 🔵 Addressed: obsolete local cell lookups removed after query refactor.
- 🧪 At the pre-GREEN review, both new and legacy transition cases were pending;
  MC03 results are recorded below. The later regression sweep still owns the
  existing follower, sealed-library and hauling suites.
- 🧪 Caller sweep found no reflection references to the changed private helper
  signatures; this is a static compatibility check, not a runtime test.

`git diff --check` passed for the owned production/test paths. No scene/play or
performance observation was performed in this bounded subtask.

MC03 result: all 22 `MultiCellTransitionTests` cases passed. The combined run
compiled without C# errors and ran 73 cases: 66 passed, seven expected new
movement failures remained for the root implementation. Evidence:
`Docs/Verification/GameSystemAudit/MC03-core-targeting-transition-first.xml.gz`.
The legacy transition/follower suites still require the later broader sweep.

## Independent core cold-eye and dedicated adversarial gate

The follow-up review read the new spatial types, Entity membership hooks,
Zone reconstruction and Cell query consumers. These are source findings before
the dedicated tests run; runtime confirmation is deliberately separate.

- 🟡 Dynamic footprint Parts: adding a Part after placement did not register
  the derived body, and removing it did not clear the old body. Accepted
  contract: valid attachment updates atomically; invalid or blocked attachment
  throws `InvalidOperationException` and restores Parts/parent/index. Removal
  flattens to one anchor, or returns false if an anchor hole is now occupied.
- 🟡 Duplicate saved references: reconstruction called `Register` for every
  raw reference and overwrote the one owner entry, leaving earlier body cells
  behind. Accepted repair is deterministic first x-then-y anchor, with extra
  raw references removed and one derived body remaining.
- 🟡 Malformed saved geometry: registration skipped out-of-bounds cells and
  accepted empty invalid offsets. Accepted contract: reconstruction throws
  `InvalidDataException` before clearing the previously usable index. No
  silently clipped body or invisible canonical owner is published.
- 🟡 Synchronous render notifications: Register/Unregister notified listeners
  inside the per-cell mutation loop. A listener could observe partial body
  membership. Publish dirty notifications after the transaction is complete.
- 🟡 Ordinary-query cost: indexed `CellOccupants` access scanned canonical
  objects even in cells without spatial owners. Existing indexed Cell loops
  therefore became quadratic for a large stack of ordinary items. Preserve a
  direct-list fast path; actual frame/turn measurements remain pending.

The new `MultiCellSpatialAdversarialTests.cs` contains 30 cases across
lifecycle, rollback, malformed restoration, identity aliasing, query stability,
full-session load hooks, destruction persistence, and diagnostic gates. Tests
were authored after MC03 ended and before fixes to these findings. Root owns
core fixes and coordinates the RED run; this audit owns only transition
diagnostics after their failing tests are observed.

MC04 stopped at the expected missing `FindPath.ToContact`
compile dependency from the separately authored AI tests. It did not execute
the 29 initial adversarial assertions. After that run ended, the 30th probe
was added for render callbacks observing complete published state. The next
coordinated run must establish assertion RED before production fixes.

Diagnostic contract selected for the gate: `event/FootprintPlaced`,
`FootprintRejected`, `FootprintRemoved`, `FootprintChanged`, and
`FootprintChangeRejected`; final automatic-arrival outcomes use
`worldmap/FootprintArrivalSucceeded` and `FootprintArrivalRejected`. Candidate
search probes must not emit a record each. Removal reports once for the
canonical owner, rather than once for every body cell.

## Coordinated integration evidence and ability wave

MC05 (`Verification/GameSystemAudit/MC05-adversarial-behavior-red.xml.gz`)
ran with zero C# errors: 134 total, 112 passed, 22 failed. Seventeen failures
came from the dedicated 30-case spatial adversarial suite; thirteen pinned
already-correct behavior. Core lifecycle/index/load-hook fixes belong to the
root agent. This audit added transition outcome diagnostics after their
behavioral RED was observed.

MC07 (`Verification/GameSystemAudit/MC07-chunk-abilities-render-red.xml.gz`)
ran with zero C# errors: 203 total, 158 passed, 45 failed. All 22 transition
cases and all 30 core adversarial cases passed, including arrival diagnostics,
saved reconstruction/load hooks and atomic lifecycle notifications.

The new `MultiCellAbilityConsumerTests.cs` initially contained 35 cases. MC07
recorded 21 confirmed ability/contact/mutation failures, thirteen correct
controls, and one fixture error: BurningEffect.OnApply overwrites the requested
Duration. The 4-turn Pyroclasm test now sets the live effect's Duration after
application. That fixture correction is not counted as a gameplay bug.

Production changes installed after MC07 RED:

| Consumer | Scope and preserved rule |
| --- | --- |
| Conflagration, Rime Nova, Chill Draft | Snapshot unique physical owners for the material pulse. Keep creature damage, creature heat, general material heat and radiant propagation as separate legitimate pulses. Rime Nova still writes cold to every affected ground cell. |
| Drying Breeze | Physical owner snapshot; caster drying remains enabled. |
| Thunderclap | One scenery status attempt per owner per cast; creature damage remains its separate pass. |
| Overload | Discover and classify the conductive chain before applying damage, then strike each still-present owner once. A dry body edge still stops the chain; a body moved by an earlier hit cannot be struck again further down the same ray. |
| Pyroclasm | Find burning matter on the actor's physical perimeter and center the explosion on the actual touched cell. Snapshot the blast owners once, retaining the elemental-target filter and one consumed burning stack. |
| Whirlwind, Ground Pound | Find the first creature on each perimeter cell, deduplicate owners and strike the snapshot once per owner. Removed victims are skipped; the caster's physical perimeter also works for a large actor. The normal on-hit/push pipeline remains authoritative. |
| PoisonedByGasEffect | Matching gas on any occupied body cell suppresses the separate lingering dose; an empty anchor hole and wrong gas family do not. |
| TilePropagationSystem | Material conductivity/flammability is visible from every occupied cell; empty holes do not supply material. |

`Skills/MultiCellAbilityQueries.cs` is a small internal cast-local helper for
radius cells, owner snapshots and perimeter contacts. It does not alter
canonical anchor storage. Owner deduplication is scoped to each pulse, so
separate casts and intentionally separate spell passes remain independent.
`GasCryoPart` needs no separate query rewrite: its inherited object-gas dispatch
is migrated by the root implementation. Its existing HP-only eligibility is
not silently broadened to every destructible prop.

The probe filters primary heat doses separately from the existing adjacent
propagation dose. The emitter uses a direct-heat flag for that secondary pass;
that instrumentation correction does not change gameplay. All four new
script/test metadata GUIDs were checked across Assets and occur exactly once.

A cold-eye pass after those edits identified an additional hypothesis in the
three creature-radius damage loops: an earlier hit may remove a later victim
from the zone while the captured list still references it. Three new probes
are prepared for the next coordinated RED run before any production fix.

### Remaining consumer inventory (not blanket replacements)

The following source reads remain relevant beyond this bounded wave. Their
status must be decided by the owning implementation, not assumed complete
from the shared-query work:

- Several manual single-target skills still select through raw cell objects:
  Flaming Hands, Kindle Flame, Hearthwarm, Conjure Water, Drench Lob, Frostbind,
  Backlash Coil, and weapon actives. Physical selection and snapshot-before-
  mutation must be addressed when exposed to the pilot; deliberate creature-
  only filters must remain.
- MaterialSimSystem's canonical world tick is intentionally one tick per
  owner and must retain anchor enumeration. Its adjacent radiant emitter,
  MaterialPart's conductive chain, MaterialReactionResolver's tag chain and
  SteamEffect's wetting contacts instead need physical perimeter queries and
  owner deduplication.
- HearthAuraEffect queries the chosen physical cell. HookedEffect and
  GoFetchGoal need body reach and safe movement results. Tonic throwing needs
  a cast snapshot of physical targets before any effect mutates the cells.
- Gas merge/dispersal and generation/persistence loops can legitimately own
  canonical cell objects; changing every `.Objects` use would duplicate work
  or alter ownership and is not the migration strategy.

## Scope boundary

These are opt-in pilot safety requirements because players can bring existing
abilities, items, companions and hazards into the chosen chunk. They are not
permission to rewrite every authored town or all legacy saves. Town owner
migration, arbitrary rotating shapes, structural collapse and simultaneous
occupancy of multiple zones remain deferred as documented in the main plan.

World-map ascent/descent has separate transfer code in
`Gameplay/World/Map/WorldMapTraversal.cs`; the pilot does not make the player
large, but any generic claim of large-player world-map travel must separately
cover that path. A pet large creature still requires the follower transfer
gates above. The caller inventory should record any explicitly unsupported
handling action with a safe refusal, never silent object loss.

## Selector completion wave — verified plan before implementation

Status: 60 selector cases established RED in MC09 and all passed MC10.
The subsequent full regression remains the final compatibility gate. Scope is the nineteen remaining Skills files with raw spatial
cell reads, excluding the root-owned Tumble, projectile base and combat
helpers. HearthAuraEffect and HookedEffect are explicitly included because
fixing their activation selectors alone would leave their subsequent pulses
or pulls anchored incorrectly.

| Pattern | Source verification | Test contract and minimum change |
| --- | --- | --- |
| Automatic adjacent target | Frostbind, Rend Armor, Hook and Drag, Flurry, Shank, Disarm and Slam repeat an 8-direction anchor scan. Backstab additionally stores the found direction. | Table-driven skill tests require a non-anchor enemy edge and a large caster perimeter to work, with identical ordinary distant targets refusing. Preserve each weapon gate, target eligibility and legitimate multistrikes. |
| Explicit chosen cell | Flaming Hands snapshots only canonical anchors; Kindle Flame heats a live reverse list; Hearthwarm gates on raw thermal occupants and HearthAuraEffect later repeats a live raw query. | Visible edge works; empty anchor hole does not. Capture owners before callbacks. Hearthwarm must actually warm the selected edge on subsequent pulses, not merely apply an inert aura. |
| Line/impact selection | Conjure Water preferentially lands on burning canonical anchors; Drench Lob stops at the first canonical creature. | Burning/body edges determine the actual landing/impact cell; water affects one physical owner per pulse. Keep empty-space and blocked-flight return semantics. |
| Self-area selection | Backlash Coil snapshots all creatures in an x-then-y ring without dedup. Conjure Rain waters the first crop in each cell. | Deduplicate physical owners while retaining all-creature versus first-crop rules. Repeated casts remain separate; Rain's native CropWatered diagnostic counts owners rather than touched cells. |
| Displacement/placement | Vault, Disengage and Charging Strike inspect only an anchor and ignore MoveEntity refusal. Glacial Wall's creature-burial guard reads raw anchors. Slam's push checks one prospective cell. | Full shape must fit before movement/solid placement. Stop on a blocked far corner and do not advance a virtual position after a refused move. Charging strikes only at actual reachable contact. Vault keeps its deliberate ability to jump a normal intervening obstacle and its sealed-library barrier refusal. |
| Flanking and hook reach | Backstab steps once from the target anchor; HookedEffect checks anchor adjacency and reports a drag without inspecting movement success. | Follow the contact direction through the physical target body to the first opposite outside cell; another part of the target is not its own flanker. Hooked pull stops at physical adjacency and reports only successful movement. |
| Disarm ownership | After unequip/removal, final AddEntity is unchecked. Equipped multi-cell items are not opted into this pilot. | Equipped footprint items are refused before unequipping, preserving one item and its binding. This intentionally excludes all large equipped items from Disarm in the pilot; general large equipment/drop placement policy remains outside the pilot. |

The tests are grouped by these patterns rather than mechanically replacing all
`.Objects` calls. Inventory lists, construction identity and canonical
material/gas simulation ownership stay unchanged. Any changed normal single-
cell return, damage, range or order policy must have an explicit test and
reason rather than being hidden in the query migration.


### MC08 and MC09 implementation log

- MC08: zero C# errors; 224 total, 212 passed, 12 failed. All initial 35 ability
  cases passed; the three newly added removed-victim radius cases failed as
  hypothesized. Their subsequent fix skips absent/dead owners before dispatch
  in Conflagration, Rime Nova and Thunderclap.
- MC09: zero C# errors; 317 total, 245 passed, 72 failed. Every previously
  established case (224) passed. The new selector suite confirmed 39 failures
  and pinned 21 correct controls; the independent native-report suite owned
  the other 33 expected failures.
- After MC09 RED, this audit installed changes in nineteen selector skills,
  HearthAuraEffect, HookedEffect and the owned internal query helper. No
  selector production was installed before its RED run.
- Scope choice: Disarm refuses any equipped item carrying a spatial footprint
  before touching its binding. Large equipped items are not introduced by the
  pilot. This safe exclusion avoids pretending that equipment-drop placement
  is solved by the scene's movable pipe.
- Single-cell compatibility review caught and avoided a diagnostic regression:
  AcrobaticsVaultTests explicitly expects `landing_occupied` for creatures
  and `landing_blocked` for solid terrain. The full-body guard preserves that
  distinction and the original normal-wall jump.
- Source sweep after selector installation: no remaining `.Objects` reads
  under `Gameplay/Skills`. This result is limited to that directory; canonical
  ownership readers elsewhere deliberately remain and material propagation
  outside the assigned scope still has a separate audit entry above.

The intermediate Vault boundary hypothesis received two additional cases in
MultiCellAbilityConsumerTests (40 total): a closed archive barrier under the
far body cell refuses, while the identical ordinary wall remains vaultable.
MC10 established the archive case RED and the ordinary-wall countercheck
GREEN before the production check was extended to every intermediate body
cell. This final boundary fix awaits the full regression.


### MC10 selector and boundary evidence

MC10 ran with zero C# errors: 322 total, 320 passed, 2 failed. All sixty selector
cases passed, including the remote-body, hole, mutation and
ordinary controls. The two remaining expected failures were the newly added
Vault archive boundary case (owned here) and the root-owned removed-gas-
behavior hypothesis. The Vault fix was applied after this RED evidence.

Native acceptance cold-eye was performed read-only on the scenario, profile
and Editor batch files. Ordinary bootstrap/south-entry/F5/F6 and explicitly
labelled gameplay API stimuli are distinct in its report, failed checks are
not hidden, and cleanup validates private save roots, preferences, scenes and
view state. One actionable finding was sent to its owner: runtime input/
display cleanup must run even if initialization or profile/report file IO
throws before ordinary completion. No native files were edited by this audit.

The final physical-entry review expands the movement gate before full regression:
Vault, Disengage and Charging Strike still call raw Zone.MoveEntity, bypassing
AfterMove and physical-cell entry effects. Six new cases in
MultiCellAbilityConsumerTests (46 total) pin each skill's body-edge entry and
ordinary remote-field countercheck, actual step-event count, and the existing
BeforeMove veto bypass. These tests are ready for MC11 RED; production has
not changed for this entry hypothesis. The intended fix is the shared forced
movement event path with existing collision/target ordering preserved. Slam
and Hooked already use that path in this wave.

Ten paired callback cases were added before the same MC11 gate (56 ability
consumer cases total): Disengage/Charge must stop if an entry reaction
relocates or removes the actor, and Slam must not stun a removed victim.
The controls retain ordinary continuation and stun. This prevents enabling
entry callbacks from introducing cached-position snapback or stale effects.
All sixteen movement-entry/callback cases precede their production fix.

### MC11 entry-event RED and implementation

MC11 had zero C# errors: 355 total, 331 passed, 24 failed. Fifteen of the
sixteen new movement-entry cases failed their intended assertions before
production changes; the Slam passive-field countercheck passed. The prior
Vault intermediate archive fix passed with its ordinary-wall countercheck.

Vault, Disengage and Charging Strike now use MovementSystem.ForceMoveTo to
retain their existing BeforeMove-veto bypass while dispatching AfterMove,
physical entry effects, visual movement and slip handling. The two walking
skills end their original trajectory if a landing callback relocates, removes
or kills the actor, preventing cached-position snapback and stale charge
attacks. Slam skips trailing damage/stun when a landing reaction has removed
or killed the pushed target.

Fixture correction: the passive Charge callback control checks whether any
damage occurred rather than demanding exactly one BeforeTakeDamage event.
The native momentum bonus deliberately dispatches a second damage pulse
when its damage threshold is reached; no production pulse behavior changed.
The MC11 failures occurred earlier at absent entry, so this corrected an
unreached over-specific assertion rather than excusing a gameplay failure.

🟡 Entry callbacks bypassed by three movement skills — fixed after MC11 RED.
🟡 Callback relocation/removal resumed a stale trajectory — live position/HP
checked before another step or charge attack.
🟡 Slam stunned a removed entry victim — post-push owner/life guard.
🧪 Full regression and native visual/feel validation remain root-owned gates.

User scope update during MC11: do not spend further effort on old-save
migration. Current-format save/load, body registration and absence/damage
persistence remain acceptance gates. Earlier migration findings here are
historical audit evidence, not permission to expand legacy compatibility.

### MC13 full-suite fixture ownership corrections

The first full regression (zero C# errors, 10,885 total / 10,801 passed /
84 failed) exposed three pre-existing fixture assumptions against the new
single-zone owner contract. The archived MC13 stack traces locate natural
weapon RoundTrip at line 53 and forge departure at line 410; both directly
re-added an entity that still belonged to another zone. Shank's no-target
fixture made the same rejected placement without checking its result, then
never reached the message branch because its actor was absent from the test
zone. These were fixture errors, not changes to natural-weapon, forging or
Shank mechanics.

Forge and Shank fixtures now use the real transactional transfer and assert
source removal. Shank also asserts the intended command refusal before its
unchanged message check. The repeated natural-weapon save test resaves the
loaded GameSessionState itself, preserving actual current-format world and
turn ownership instead of extracting its actor into a second fake zone.
All original equipment, natural damage, consumption and message assertions
remain. No production files changed for these three failures; MC13 is their
RED receipt and the next full run verifies the repaired preconditions.

### MC21 gas-contact fixture correction

MC21 full regression had zero C# errors: 10,923 total / 10,890 passed /
33 failed, including the same 31 baseline failures. The intermittent
`GasOverlapChoosesStrongestFamilyDoseIndependentOfAnchor` failure came from
an uncontrolled dispersal precondition: the contact helper created unstable,
compatible adjacent clouds. `GasSystem.ProcessGasBehavior` runs before
exposure collection; a northwest spread from the remote level-3 cloud can
merge into the original level-1 anchor. `GasSystem.MergeChunk` correctly
promotes that receiver to level 3, adds its density and subtracts the donor's.
`SpatialGasExposure.Stronger` then correctly selects the now-denser anchor.
The original test incorrectly assumed that initial source identities still
represented their original strengths after native dispersal.

Contact probes now use anchored `Stable` clouds, with unchanged level,
density and placement asserted in the original strongest-family test. A new
paired stable/unstable regression uses the same explicit RNG sequence:
weak decay 1/no spread, strong decay 1/one northwest spread of 30. The
unstable case must select the promoted receiver (level 3/density 129) over
the donor (level 3/density 69), still exactly one family dose. The stable
countercheck must consume no dispersal RNG and select the original remote
level-3 pool. The exact prior GasSystem RNG object is restored in `finally`.

Only `MultiCellContactHazardTests.cs` and this log changed. No production gas
or Qud-derived dispersal/merge semantics changed. MC21 is the observed flaky
fixture receipt; the deterministic diagnostic pair and corrected contact
probes await the next root-coordinated targeted gate.
