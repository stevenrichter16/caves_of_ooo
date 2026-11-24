# Local cargo sled

November 24, 2026. One new bounded capability: fit and recover the same physical
sled beneath an ordinary local haulable load. Construction, fitting, hauling
support and recovery are one connected feature, not separate capability counts.
The portable screen and repairable crossing remain source candidates only.
This increment publishes the bounded runtime and its evidence. The next coherent
Mac delivery is pending.

## Player benefit and actual rules

One iron spike, two salvaged timber and two knotflax cord at an ordinary forge
make a reusable six-weight singleton sled. The same object is fitted beneath
one visible, nearby, unowned ordinary single-cell load for one action. It stays
physically on that ground cell, with reciprocal saved references and pickup
disabled. Its own six weight contributes to handling; normal lift and haul
capacity remain authoritative. A coherent sound sled halves the ordinary
hauling Speed penalty. It does not halve mass, increase Strength, pull through
obstacles or enable general cross-zone cargo transport.

Only a successful shared hauling follow can carry the same sled from the prior
ground cell to the actual load destination. Independent relocation leaves it
behind and removes assistance. Damage removes assistance before the next energy
accrual; only the recorded applied hauling penalty is removed and recomputed,
preserving unrelated penalties. Releasing the haul enables one-action recovery
of the original sled. A removed load leaves its sled recoverable.

Ordinary carrying, local hauling, dismantling and existing freight remain
alternatives. This adds no obligatory wear or maintenance, autonomous hauling,
NPC porter, general cargo route, capstan or complete machine. Two new Part
fields save Entity references: CargoSled.Load and SledLoad.Sled. No existing
Part field shape changes.

## Native tests and review corrections

Final `cargo-sled-current-20261124h`: **352/352**, zero compiler errors.
There are 25 sled cases, including two ordinary hauling controls, and 327
retained adjacent cases. The filter covers ordinary hauling/weight/obstacles,
lifecycle/adversarial hauling, rig relocation, turn-energy, shared material
processing and world tool rows. This does not clear the failed wider regression.

The initial a attempt used two inaccessible internal SpatialZone test assertions
and failed compilation; it proves no RED behavior. Corrected b compiled: 13
cases, two ordinary controls passed and 11 failed on the shared missing sled
blueprint. Those are missing-feature failures, not separately earned guard REDs.
c passed 17/17; d passed 350/350. Two busy-clone refusals, e and f, launched no
tests and changed no clone. The independent reviewer then identified two P2s:
late moved-sled rollback could strand an untakeable object, and recovery could
accept a different packed owner sharing the original ID. Both were reproduced
as genuine compiling g REDs, two failures out of two. Scoped takeability undo
and exact-reference membership guards fixed them; final h includes both checks.

The d batch exited normally before any signal was sent. All original XML/log
files remain retained; raw XML and lossless compressed logs are here. Twelve
runtime/test/scenario/meta SHA pins match the final idle clone3 source. No
unchanged route-tool or field-tool qualification was replayed as new progress.

## Actual Editor route

This is an explicit supplied CargoSledShowcase, seed 64, Strength 18 and ordinary
40 HP, in Overworld.10.11.0. Materials, forge and existing finite timber pallet
were supplied; no finished sled was granted. No inventory, position, clock,
stats or world state was injected after setup. Actual native InputSystem keys
operated GameBootstrap menus, readers, movement and F5/Continue.

- Forge20 consumes the exact spike, two timber and two cord, producing sled2143.
- Fit30 places that same sled beneath pallet2142 at41,12. Mass90→96; ordinary
  hauling penalty36 becomes19. The existing haul command is free; Speed becomes81.
- Native Left moves actor to39,12 at43 and both ground owners to40,12. Native Up
  reaches39,11 at55 and both owners follow to39,12. The forge is passable; Up is
  not a blocked-movement demonstration. Down into the moved solid pallet then
  refuses with tick55, position, energy1025 and grip unchanged. The load remains
  an actual obstacle after being moved, an interaction outside the sled menu.
- F5/fresh55 retains the active grip, exact reciprocal references, both physical
  positions, Speed81, energy1025, HP40 and selected pack state.
- Native release restores Speed100 at55. Recovery65 returns the original2143,
  removing the support without moving or rebuilding the pallet.
- Existing dismantle75 consumes that pallet once for two timber. Existing
  shape_haft85 spends one timber for FieldHaftComponent2707, retaining one timber
  and sled2143. This is the existing preparation output, not OakHaftComponent.
- F5/fresh85 retains the selected final state: Speed100, no grip or pallet,
  the original unlinked carried sled, one timber and the field haft.

The two-owner cell intentionally opens the existing pile summary. Choose
"everything here" and the actual sled or pallet to see its own actions. This is
not a missing-action defect. Recovery is absent while the load is actively
hauled. Both full reader pages were inspected; reading and cancellation were
free. The initial I input had no effect during startup; separately observed I
opened Inventory normally. A background read timed out before Unity was brought
forward; it changed no world state and was not retried as an action.

Three F5 archives retain **all 12 files**, verified against their receipts before
Finish. Both selected fresh pairs match exactly. These observations do not prove
whole-world or compressed-save equality, ordinary acquisition, optimal hauling
before dismantling, travel, sustained NPC use, packaged input or comparable FPS.
Three real PNGs show construction, the fitted reader and the actual hauling HUD.
Root and the bounded independent reviewer inspected all three; no graphics work
or art acceptance is inferred.

All three private isolation roots are preserved and already finished:
9e5e49c6219045689c667754caed09a4, d0eeba163f0541a1bd3bb259d9f924c0 and
84f916e8b43c4d6695180af5b73bdb7c. Never Finish these tokens again. Main is stopped,
root null and scene clean, profiler off, original seven views/index zero, input
settings/devices restored, reload locks released and RequestedSeed restored to0.
Current delivered Mac a02338565 predates the uncommitted sled. See review.md and
preservation.json for final bounded review and fresh preservation evidence.
