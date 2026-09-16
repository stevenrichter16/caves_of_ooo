# Stillleaf — the threshold and the seal

Status: complete and installed. Stillleaf native composition passes final targeted and
full-suite checks. SC14 has 537/537 focused passes; SC17 has 12,825 passes and
exactly the 32 unchanged baseline failures, with zero C# errors and no new
failures. All 287 new tests pass. Eighteen native camera previews have complete
model coverage; live input feel and sustained FPS remain outside this evidence.
The aggregate implementation log and review are in
`CATHEDRAL-STILLLEAF-COMPOSITION-PLAN.md`.

## Finite scope and intent

Only `Overworld.2.4.0`, `.1`, and `.2` belong to this composition. The manager
must additionally recognize the actual Stillleaf sinkhole/profile, including a
renamed place retaining that profile. Depth 3 and every other site keep their
existing pipelines. The builder is for fresh empty zones; it rejects an already
populated zone rather than erasing its owners.

The surface is a wind-cut pink-stone threshold, with broad outcrop shoulders
and open paths around the native mouth. Three small, contiguous native Bush
colonies sit outside the lip in sheltered pockets, leaving the stone landing
and protected approaches clear. Its centre remains real walkable stone:
the visual does not imply a new falling or climbing mechanic. The descent uses
three unequal cliff-attached ledges: an upper left shelf three cells deep,
a narrower right shelf two cells deep and a broad lower left cache landing
five cells deep. Three native rope anchors and one sack containing a
Torch, DriedMeat and HealingTonic accompany the previous expedition's bones. The bottom is an eroded outer sandstone chamber around an intact,
austere archive. Two tapered sandstone projections grow inward from opposing
chamber sides, forming unequal outer bays without closing the central route.
The native late stamp can trim their ends when selecting an alternative
enclosure anchor. Negative space and the closed approach are the main features;
this is not a new treasure room or a random ruin scatter.

## Native authority

`StillleafCompositionPlan` holds only ground, object and approach cell data.
`StillleafCompositionBuilder` stages and validates detached entities before
placing anything. Missing or malformed required native content must reject
without partial terrain or newly published reservations. Seed and exact zone
address determine the layout independently of the caller's random stream.

The native mouth builder owns its lip and downward connection. Existing stair
builders and the connection registry own travel; parent-first preparation already
supports loading a lower level directly. `StillleafArrivalReservationBuilder`
reserves the actual stair cells at priority 3660, before ordinary hazards,
population and containers. It does not move stairs or invent new connections.

The native `SealedLibraryBuilder` remains authoritative at priority 3650, after
stairs and their connector. It chooses a stair-free enclosure among its native
anchors, carves exterior approaches, stamps the seal and reserves its contents.
There must be no later whole-map connectivity repair through the archive.

The three fixed wall materials remain Tepuibone, MemoryMarble and ChoirIron.
The door remains a native `LockPart` with key ID `coo.sealed-library.stillleaf`.
Walls, door and shelves are intentionally indestructible. The barrier derives
closure from the real lock; there is no parallel visual gameplay state. The
interior floor's `ExcludeZoneArrival` remains present even after unlocking.
Shelves are fixed examinable archives, with no newly invented container or
reading function. No quest key, readable archive text or new story resolution
ships in this composition.

## Verification and limits

The owned core fixture covers exact scope, seed repeatability, coherent cliff
mass, native ground ownership, connected reserved approaches before the seal,
and preservation of stairs when the late archive stamp lands. The separate
adversarial fixture covers missing/malformed dependency atomicity, rebuilding
rejection, lower-first reciprocal stairs, closed-room exclusion, exterior travel
and real synthetic-key unlocking. Synthetic keys exist only inside tests.

SC10 produced nine Stillleaf native manager previews: the three depths at seeds
64, 1729 and 729490642. Every image was inspected. The receipt reports zero
missing meshes and zero unmodeled owners in all nine scenes. The mouth has
three visible green colonies and quieter broad stone shoulders; the descent
reads as three unequal landings, with its broad cache shelf visibly distinct;
the archive's two bedrock projections form outer bays while the intact seal
remains the dominant constructed object. Its exterior is intentionally sparse.

A remaining art limitation is that the reused Bush mesh repeats as individually
separated square tufts, giving contiguous colonies a somewhat planted appearance.
The grouped ecology and native ownership are correct; the image review does not
establish that this is the final vegetation silhouette. Static renders and the
receipt establish composition and model coverage, not sustained gameplay FPS or
live input feel. SC11's 527/527 targeted result includes the refined native and
sealed-access gates; the isolated full-suite comparison is still pending.
Existing saved zones are not regenerated and no migrations are added.

## Final verification

SC17 confirms the exact baseline failure names and messages are unchanged.
SC13 rebuilt all 294 combined artifact/metadata files byte for byte. The final
GUID audit covers 5,735 Unity metadata files and finds no task collision.
`Verification/VoxelWorld/SC16-closeout/regression.json` records the comparison;
`SC10-refined-preview/index.html` contains all eighteen actual native renders.
Earlier pending statements in the implementation log describe those earlier
runs; this final verification supersedes them. New layout rules apply on fresh
native generation; existing cached/saved graphs are not rewritten.
