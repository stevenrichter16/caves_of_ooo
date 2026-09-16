# Wellmeet — the cloth, the scales and the well

Status: complete and installed; all 55 owned native tests pass. The final exported art and complete owner coverage pass FC17/FC18/FC20b. FC22 full regression has 13,169 passing tests, exactly 32 unchanged baseline failures, zero C# errors and no new failures. All 344 additional tests pass.

The aggregate review and implementation record are in [OLDERDEEP-WELLMEET-COMPOSITION-PLAN.md](OLDERDEEP-WELLMEET-COMPOSITION-PLAN.md). Static native previews do not establish live input feel or sustained frame rate.

## Exact place and scope

Only `Overworld.8.16.0` is composed. The manager must additionally retain the
current native Village/TentCamp profile authority. Renaming a profiled place is
supported; substituting a different place/profile is not permission to replay
this composition. The First Tent at `5.17.0`, other villages and depth 1+ retain
their existing generation. Saved/current zones are not regenerated.

Wellmeet is a semi-permanent water and salt crossroads. Five unequal tents
represent guest reception, service shelter, salt exchange, a household and a
resting house. They share an open well commons and receiving/salt courts.
Native RoadStone paths connect actual doorways and zone edges through four
bending trunks and short branches to the nearest shared route. Sand fills outdoor
negative space. Every individual wall, floor, furnishing and service remains a
native owner; broad voxel meshes must not introduce unrelated collision.

The well commons stays at `(40,12)`, where VillagePopulation now creates a
repairable main well for this exact profile. Its four cardinal service markers remain physically clear
and outdoors. A separate native profile well belongs to the receiving court,
so a completed settlement has two actual well owners. Neither is a painted
water feature.

## Native shade and services

The base owns five TentWall outlines and open three-cell entrances. Guest and
household shelters contain native beds/chairs; service and salt shelters use
Crates in place of beds. Four sparse outer shoulders contain native DryBrush
and Rock, outside protected paths and interiors. Tent interiors use exact
StoneFloor and `Cell.IsInterior=true`. Outdoor RoadStone keeps
`IsInterior=false`: paving is not shade. The
actual Beating glare system consumes the interior flag, while generic village
NPC interior selection uses StoneFloor. Their meanings are tested separately.

`WellmeetProfileBuilder` runs at 3860 after cave placement. It creates exactly
one TentRightHost, SaltMaster, GuestClothPole and additional Well in the base
plan's receiving/salt courts. It requires the same realized zone instance and
refuses replay or obstructed profile cells. All four detached owners are
validated before any is placed. Their native Parts, faction/conversations,
mineral demand and owner identity remain authoritative. The profile helper adds
SettlementId linkage; it does not create a second set of generic village roles.

The native host offers UnderTheCloth guest-right. The oath lasts three native
clock days and protects against hostile people, not beasts. The salt-master
accepts a real carried PaleSalt unit for native TentRight reputation; empty
inventory must not receive an unpaid reward. No caravan scheduler, deity,
mount, boat or new economy is implied by this settlement's models.

Ordinary VillagePopulation, shrine,
merchant/rental roles, trade stock, containers and house-drama integration remain
in the manager pipeline. Source verification corrected a premise: ordinary
villages did not previously activate repairable site tracking. This feature
explicitly opts exact Wellmeet with Village/TentCamp authority into the existing
well/oven/lantern record and Part systems; other sites remain unchanged.
The new reservation option applies only to this composition: protected paths
and profile courts cannot become generic NPC interiors or service positions.
Other villages retain their existing selection behavior. Actual repair methods,
costs, consequences, record representation and native conversations remain the
existing systems; repair-site enablement itself is new to Wellmeet.

Each actual well/oven/lantern site Part exposes its last applied visual stage.
Recipes select fouled, temporary, repaired or caretaker-improved geometry from
that owner-local stage. The additional profile well has no repair-site Part and
uses the ordinary stable form. Low native ground-marker pads remain separate
owners; the main fixture geometry conveys its repair stage.

## Dry ground and lifetime

The ordinary village pipeline adds a flowing bottom river regardless of the
world map. Wellmeet's authored map cell has no river. This exact composition
explicitly omits that generic river and relies on its actual wells. Tests compare
its absence of native WaterPuddle with the unchanged First Tent river. This is a
native generation decision, not a shader hiding real water.

The base runs at priority 1000 and accepts fresh empty zones or a cleared
retry of its same zone instance. Successful retries reset the late-profile
lifetime only after the new base owner batch commits. It validates
all required native ground, walls, furniture and eventual profile dependencies
before mutation, stages owners, and publishes shade/reservations after placement
succeeds. The late profile also stages its complete batch. Failed placement
removes only its newly placed owners. Diagnostic records identify scope,
content, occupied-service or placement rejection. No per-turn generation or
parallel saved state is introduced.

Detached preview generation must not create effects in the player's active
zone. FC16 confirmed this existing site-aura behavior for WellSitePart,
OvenSitePart and LanternSitePart: applying a detached owner's visual stage
started an aura in the current active graph. Native owner-membership checks
now guard all three entry points. FC18/FC20b and full FC22 pass the detached-owner
refusals and the real attached-owner aura countercontrols.

## Voxel coverage and presentation

The final source kit contains 76 models across 19 four-variant families. Tent
runs and corner pieces use actual current neighboring walls, so destruction
changes the remaining cloth shape. Packed-earth roads, dark indoor flooring,
furniture, native repair stages, adult role silhouettes and shorter children
remain visually distinct within the limited palette.

The last coverage pass adds quiet low pads for WellGroundMarker,
OvenGroundMarker, LanternGroundMarker and CampfireGroundMarker; these replace
the misleading tall ash-pile treatment. Farmer and WellKeeper use exact native
adult aliases. AlchemyShelf retains its real container owner. Legitimate quest
villager and shrine-marker appearances require their actual native quest Parts.

The shared GinFrog/gecko branch now preserves native movable-actor behavior
when those owners enter Wellmeet. A cold-eye test caught their earlier static
batch classification; movement-stable bodies and canonical glyph guards were
corrected after actual RED. No procedural content is rebuilt by presentation.

The combined source inventory is 116 models: 40 Olderdeep and 76 Wellmeet.
Every source mesh stays within one horizontal native cell and at most two
palette swatches / 240 vertices. FC17/FC18/FC20b verify actual assets and native
owner coverage; FC21b verifies byte-identical rebuilding.

## Verification and limits

FC07 captured actual missing cave-arrival reservation and retry failures.
The fix reserves only native stairs that actually exist (priority3870) and
passable cardinal arrivals before village population. FC09 verified retry and
all three repairable native service records/Parts; it captured the deliberate
road/furnishing refinement failures before their generator rules changed.

FC12 completed with all 55 owned Wellmeet core/adversarial cases passing, including
packed-earth bent routes, differentiated furnishings, sparse native fringes,
stair reservations, staged missing-dependency rejection and repair site records.
At that point the combined batch retained separate art/renderer adversarial
failures; this was not a full-feature or full-suite green claim.

FC13 static review inspected all three seed previews. Packed-earth forks and
clear negative courts now read separately from dark shaded interiors; native
beds distinguish resting shelters from crate-equipped service/salt shelters.
All five entrances remain visibly open. The combined renderer still needed
coverage for newly activated repair markers and service roles at this point.
The named site deliberately shares four trunk-route relationships across seeds;
its tent dimensions/positions, population and fringes vary. This is a bounded
Wellmeet composition, not a claim of arbitrary town topology generation.

FC14 again passed all 55 native cases. Its shared rendering coverage gate
exposed Farmer, WellKeeper and newly enabled repair markers. FC15 captured the
new marker/alias asset failures before production additions. FC16 then captured
the three detached-aura failures described above, with zero C# errors.

Complete: final owner/model coverage, the 116-model rebuild and gallery,
aura-guard verification, full-suite baseline comparison and metadata checks. Static native previews establish composition, silhouette and observable
owner coverage. They cannot establish live input feel, native light animation
quality or sustained frame rate. Fresh-generation plans allocate only during
creation; presentation borrows meshes and reconciles current native owners.
