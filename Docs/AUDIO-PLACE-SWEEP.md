# Place audio: source and identity sweep

Status: read-only source audit and proposed bounded implementation, 2026-10-11.
This document records what the existing world can truthfully support. It is not
evidence that the proposed audio has shipped or has been heard in Play mode.
The parent implementation owns its tests, audio assets, listening review, and
native performance evidence. No gameplay or rendering sources were changed by
this sweep.

## 1. Small set with distinct purposes

Keep a quiet regional background, then let actual nearby sources distinguish
one chunk from the next. A field, a riverbank, an occupied room, and an abandoned
oven should not have the same mix. Intermittent animal calls need actual animals;
working sounds need actual work. Large empty spaces need silence between sounds.

| Region | Regional identity | Current sources that can enrich a chunk | Boundaries |
| --- | --- | --- | --- |
| Spread | Open river country: soft air, vegetation, restrained daytime life; quieter at Dark. | Flow-tagged `WaterPuddle`; `Reeds`; `Tree`; standing crop rows; actual `Magpie` and `PetDog`; occupied village rooms. | No `sari` call: the world design explicitly reserves its arrival in the Spread for an inciting event. Do not invent geese entities merely because the aspirational sound paragraph mentions geese. |
| Sodden | Heavy, sparse wetland texture; quiet gaps and low standing-water movement. | Positive-volume `PeatBog` and `MirePool`; actual `Reedfrog`, `GinFrog`, `Bandfrog`, `MawToad`; Sumphold's visible residents and water. | Bog wilderness deliberately lacks ordinary human trouble. Village work/chatter must not blanket every Sodden chunk. Standing mire is not a rushing river. |
| Beating | Dry broad air and faint crust/grit texture; much sparser at Dark. | `DryBrush`, `BrinePool`; actual `BrittleHound` can have a restrained glass-edged canine voice. | No market or caravan bells without a corresponding source/event. `SunStriker` is a basking lizard, not a bird. Do not infer active storms from a biome name. |
| Grovelands | Restrained distant chordal organic tone, close vegetation and water-vein detail. | `Tree`, `GroveSeep`, actual `HelmwoodFrog`; a very close `GroveLanternMoth` could have soft wing movement. | The Choir's material culture is grown, not manufactured. Do not add forge/clanging textures as generic regional activity. A moth is not a generic insect chorus. |
| Overwrit | Thinned, nearly empty air with intentional gaps. | Actual visible inhabitants/props, if present. | Do not fill its scarcity with a busy procedural sound bed or invented populated-town loop. Bleed/manifestation sound needs the real event; this sweep does not supply that event. |
| Stump | Exposed high stone alternating with close humid tree/spray pockets. | `SprayPool`, `HelmwoodWaterPassage`, `Tree`; actual `SummitSinger` gives widely spaced frog notes at Dusk; `GlasspaneFrog` is a possible later distinct voice. | `CascadeFather` explicitly has **no call**. `SummitSinger` is a frog, not a bird. Its lore alarm/silence clock is documented implementation debt; do not claim an Urqu alarm from an ordinary animal call. |
| Underground | Enclosed quiet tone; sparse drip only where an actual water source supports it. | Actual `CaveBat`, water/seep sources, occupied interiors or campfires. | Depth must take precedence over the surface biome and POI above. No river, crowd, or surface bird chorus by location alone. |

Identity evidence:
[biome definitions](../Assets/Scripts/Gameplay/World/Map/WorldMap.cs#L15),
[Spread population](../Assets/Scripts/Data/Tables/PopulationTable.cs#L377),
[Sodden population](../Assets/Scripts/Data/Tables/PopulationTable.cs#L555),
[Grovelands population](../Assets/Scripts/Data/Tables/PopulationTable.cs#L615),
[Stump population](../Assets/Scripts/Data/Tables/PopulationTable.cs#L208),
[sound and culture intent](FELLING-WORLD-DESIGN.md#L259),
[Stump implementation corrections](STUMP-COMPOSITION.md#L51).
Exact species descriptions were checked in `Assets/Resources/Content/Blueprints/Objects.json`.

## 2. Time, weather, room, and town APIs

- Use `WorldClock.GetBand(input.TurnManager.TickCount)` with the receiving
  session's turn manager. Bands are Dawn=0, Height=1, Dusk=2, Dark=3, 300 ticks
  apiece, a 1,200-tick day. Negative ticks return Dawn. Do not use wall time or
  advance simulation to schedule a day change.
- `Cell.IsInterior` describes the actual player cell. `VillageBuilder` marks
  floor interiors, leaving wall/door edge cells exterior. Sumphold, Tally and
  Marrowstye also author the flag. Read it to soften exterior layers and change
  room texture. Do not generate a new room plan from the audio update.
- `WorldMap.GetPOI(x,y)` returns `Type`, `Name`, `Faction`, and `Profile`.
  Use the active map and validated current surface address only. Village
  profiles can select a tonal family, but a village POI alone does not prove
  that people are nearby or that work is underway.
- `Zone.CurrentWindSpeed` and `CurrentWindDirection` exist for gas transport,
  default to zero/empty, and this sweep found no production assignments beyond
  those defaults. No ambient rain/weather state was found. `Conjure Rain` is
  an action with effects, not a persistent storm. Do not label a generic air
  bed as a dynamically simulated weather system.

Sources: [clock](../Assets/Scripts/Gameplay/Turns/WorldClock.cs#L42),
[cell interior](../Assets/Scripts/Gameplay/World/Map/Cell.cs#L33),
[village interior assignment](../Assets/Scripts/Gameplay/World/Generation/Builders/VillageBuilder.cs#L212),
[POI fields](../Assets/Scripts/Gameplay/World/Map/PointOfInterest.cs#L37),
[authored settlements](../Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs#L211),
[wind fields](../Assets/Scripts/Gameplay/World/Map/Zone.cs#L70).

Town vocabulary can vary without pretending that work occurs: Sill's occupied
rooms can sound warm and open; Sumphold's Boatyard spaces damp and wooden;
Marrowstye's Intake sparse and institutional; Quillhold's PrimaryArchive close
and hushed; Tally's CentralExchange busier only around actual visible residents.
A low, indistinct occupied-room texture should require at least one suitable
living, visible, nonhostile conversational resident nearby. A larger crowd
texture should require multiple distinct current owners. Tools carried by a
resident do not prove that a tool is being used.

## 3. Source predicates

Apply a common gate first: the original owner is still in the receiving Zone;
`SpatialZone`, `zone.GetEntityCell(owner)`, the cell's zone and membership agree;
the owned `PhysicsPart` is neither in inventory nor equipped; the owned
`RenderPart.Visible` is true; the cell is explored and currently visible; the
world presenter/input is actually visible; the source remains within the
chosen small radius. Reject destroyed structure state (`DestructiblePart.Gone`
or HP <= 0). Exact part ownership matters. An actor additionally needs its
actual `Hitpoints > 0`, no handled death, and an owned `BrainPart`.

| Source | Positive evidence | Refusal/countercheck |
| --- | --- | --- |
| Flowing water | Owned `LiquidPoolPart`, `Volume > 0`, `LiquidId == "water"`, plus actual `FlowsEast`/`FlowsSouth` tag; the river builder stamps those tags on `WaterPuddle`. Exact `SprayPool`/`HelmwoodWaterPassage` may instead use a smaller spray texture. | An ordinary puddle, consumed draw point, oil, acid, or stationary brine must not become a river. Recheck volume and exact source while its loop plays. |
| Standing wetland | `MirePool` with `LiquidId == "bog-mire"`; `PeatBog` with `LiquidId == "water"`; `BrinePool` with `LiquidId == "brine"`; each with positive actual volume. | These are separate authored identities; a generic liquid-tag test would include dangerous unrelated liquids. Keep wetland bubbling very sparse. |
| Woods/reeds | Actual intact, visible `Tree` or `Reeds`. | A dropped wooden item, destroyed stump, or mere Wood material is not a woodland. This is foliage/creak texture, not chopping. |
| Crops | Owned `CropPart.GrowthStage >= 1`, or exact `RipeCropRow` with owned `FieldHarvestPart.Harvested == false`. | Seed stage and cut stubble are not standing leaves/grain. No call to growth reconciliation from audio. |
| Actual flame | Owned `BurningEffect` with `Owner == owner`, `Duration == -1 || Duration > 0`, finite positive `Intensity`. If a FuelPart exists, it must be owned and have finite `FuelMass > 0`. | Duration 0 already means exhausted/expired even before removal. Residual heat and smoldering are not flame. A finite cooking source starting hot without BurningEffect remains silent in this flame layer. |
| Legacy authored campfire | Explicit compatibility policy only: owned `CampfirePart` with `FiniteCooking == false`, owned finite Thermal state at/above its finite flame threshold, and owned finite positive fuel. | Campfire presence alone would keep sounding after cooling/dousing. Do not apply the legacy shortcut to `SpreadCookingCoals`. An actual newly reignited BurningEffect can still sound even if an old CharredEffect remains. |
| Animal call | Exact approved BlueprintName on a currently living visible actor. Prefer calls at spaced wall-clock intervals, but recheck current ownership, life, range and visibility at emission. | No calls from corpse blueprints, harvested remains, aliases, inventory contents, hidden/dead actors, or all frogs indiscriminately. In particular CascadeFather is silent. |

Sources: [liquid state](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L35),
[river tagging](../Assets/Scripts/Gameplay/World/Generation/Builders/RiverChunkBuilder.cs#L151),
[burning duration/fuel lifecycle](../Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs#L112),
[finite ember refusal](../Assets/Scripts/Presentation/Rendering/CampfireEmberRenderer.cs#L64),
[crop stage](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L39),
[finite field harvest](../Assets/Scripts/Gameplay/Farming/FieldHarvestPart.cs#L10).

Initial animal families worth implementing: Magpie chatter, PetDog short voice,
reedfrog-family croaks with separate small/large variants, HelmwoodFrog soft
forest note, SummitSinger's very sparse Dusk note, CaveBat flutter/click. Add
BrittleHound only with a materially distinct glass-edged cue. This is enough
to make several real regions audible without filling every second.

## 4. Actual work versus scenery

Generic resident data is not a work scheduler. `MorrowfastResidentPart` stores
identity and last bell awareness; `StillleafResidentPart` stores identity.
The generated seed keeper and wayside cook are ordinary passive traders with
conversation/stock roles. Neither a role string, a forge nearby, a carried
hammer, nor `BoredGoal` proves hammering, chopping, or cooking.

One concrete work system exists: `KitchenBatchPart` on `ConnectedBatchPan` in
`Overworld.12.11.0`. A paid commission records actual Worker/WorkerID, escrow,
pickup, commissioner, inputs, State, StartTick, DueTick and JobOrdinal. It
finishes through existing reconciliation after turns, zone arrival, bootstrap
and load. Wall-clock audio must never call `Reconcile` or create a meal.

Recommended safe read seam for a kitchen texture: expose a side-effect-free
predicate in KitchenBatchPart that reuses `Bound`, `ValidJob`, `InputsCurrent`,
`LiveStructure` and `StaffAvailable` against the receiving zone and existing
commissioner; require `currentTick < DueTick` and a visible, living exact worker
within two tiles of the visible pan. Recheck it when emitting/continuing the
sound. `State == "Working"` by itself can outlive malformed or changed public
owners and is insufficient. Reusing current validators is preferable to
copying receipt logic into rendering. If this seam is outside the slice, defer
the kitchen texture. Work sounds for one-shot forging should observe its real
committed transaction in a separate change, not run on a timer beside a forge.

Sources: [resident identity](../Assets/Scripts/Gameplay/World/MorrowfastResidentPart.cs#L4),
[generated residents](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationResidents.cs#L222),
[kitchen binding and staff](../Assets/Scripts/Gameplay/World/KitchenBatchPart.cs#L13),
[kitchen job and input validation](../Assets/Scripts/Gameplay/World/KitchenBatchPart.cs#L201),
[kitchen completion](../Assets/Scripts/Gameplay/World/KitchenBatchPart.cs#L264),
[forge command](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L98).

## 5. Performance and disclosure contract

1. Reuse the context refresh on zone/actor/turn change and hidden-to-shown
   transition. A radius-six search is at most 169 cells; inspect only current
   visible cells. Never enumerate cached world zones or rebuild generation
   plans from the audio path. Do not use per-frame LINQ or allocate a collection
   per source/cell.
2. Retain at most a small fixed set of source witnesses (for example six types)
   and at most two simultaneous local continuous textures. Bound both candidate
   occupants inspected and retained voices; many copies of one species must
   not become a chorus. Deduplicate multi-cell owners and choose nearest
   sources deterministically.
3. Per-frame work may revalidate retained witnesses and advance fades/cooldowns;
   it must not rescan the world while the player idles. A new source becomes
   eligible on the next context refresh. Direct mutation of a retained source
   must silence it immediately. Removing/killing/draining/hiding it must not
   leave a stale sound playing through the fade tail; distinguish an ordinary
   place crossfade from loss of source authority.
4. Hidden menus may retain a nondisclosing regional bed, but never discover
   fresh sources from stale FOV. On returning, resample visible source context.
   World-map and invalid-address contexts must not inherit the last town's
   localized voices. A surface village POI cannot add residents to its cave.
5. Add tests for visible positives and hidden, depleted, spent, carried,
   mismatched part/zone/owner, replaced source, dead actor, CascadeFather,
   finite hot coals without flame, expired burning with fuel, cooled legacy
   fire, and kitchen changed bindings. Verify exact capped scan/source counts
   and zero idle rescan growth. Tests establish routing/ownership; a native
   listening pass establishes whether the mix is pleasant and recognizable.

## 6. Scope decisions

The rich first slice is regional day/night identity plus water, foliage,
interior/occupied-place character, fire, and a handful of actual animal voices.
It can make exploration and returning to towns perceptibly different without
new simulation. Weather, automatic smith schedules, invented animal populations,
Urqu's alarm clock, and unobserved offscreen directional clues are separate
features requiring their own gameplay designs and authority rules.

## 7. Independent implementation review and native evidence

The first parent-owned implementation was inspected after the design sweep.
`WorldAudioPlaceAdversarialTests` adds 31 synthetic-clip cases without changing
production. It probes active animal tails across idle and turn refresh, destroyed
water/fire/wood sources, mismatched raw owner/zone/part bindings, missing animal
HP/Brain, standing versus flowing water, drained-source ambience tails, room
and day-band transitions, hidden discovery, menu resumption, independent buses,
the 16-source voice cap and 512-owner scan bound.

The coordinator's [initial native receipt](Verification/PlaceAudio/native-red.xml)
records **118 total, 98 passed, 20 failed** on 2026-10-11. This independent
fixture contributed **31 total, 15 passed, 16 failed**. Its observed failures
covered animal tails lost at turn refresh, missing animal HP/Brain, destroyed
water/fire/wood sources, raw spatial/cell ownership, water-bed tails after
draining, room changes and day-band changes. The other four failures belonged
to the credits reader and combat fixtures; they are not counted as independent
place-test failures.

The coordinator fixed source retirement before replacing the discovery
snapshot, retained separate audible-source witnesses, moved destruction checks
before classification, required actual living animal brains and exact spatial
ownership, handled room/day policy changes, and immediately invalidated only
the source-dependent water bed. The menu path also stops inheriting that water
bed while retaining eligible regional ambience and music. The credits reader
now combines both complete attribution texts.

The [first post-fix native receipt](Verification/PlaceAudio/native-fix-fixture-red.xml)
records **118 total, 116 passed, 2 failed**, including **31/31 independent place
cases** and **52/52 combat cases**. The two remaining basic place cases created
frog fixtures without the newly required BrainPart; the coordinator corrected
those fixture premises. The [final broad native receipt](Verification/PlaceAudio/broad-green.xml)
records **1478/1478 passed, zero failed or skipped**, including all **119** new
cases: 36 basic place, 31 adversarial place, 29 basic combat and 23 adversarial
combat. The additional basic place case checks source-dependent water retirement
through the runtime menu path.

Read-only re-review found no further material issue in the shipped place
routing, audio-bus separation, targeted water invalidation or credits changes.
The real detail recordings last at most 3.02 seconds, below the ten-second
minimum ordinary detail cadence; this bound matters when changing recordings
or introducing overlapping continuous source voices later. These receipts
prove native routing and ownership assertions, not subjective sound quality,
physical Steam Deck performance or frame-time improvement. Kitchen work,
SummitSinger's single-note call and bat recordings remain explicitly deferred.

Final read-only combat review checked that each selected weapon creates its own
attempt after offhand admission, that the impact receipt measures only the HP
decrement after callbacks and resistance, and that generic damage supplies no
weapon attempt. The router still validates both visible current owners and the
captured contact before accepting the cue. The public damage entry point,
gameplay snapshots and death handling are unchanged. No material regression
was found. The coordinator's live playback audit and delivery checks are
separate from this native EditMode evidence.
