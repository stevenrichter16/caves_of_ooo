# Atomic clay baking and a useful repair endpoint — November22

Status: qualified correction; 127/127 targeted native GREEN, three killed mutants,
and actual processing/repair/fresh Continue. See
[the durable receipt](Verification/MachineSystems/RegionalClay20261122/README.md).
The original qualification below is retained as history; its future-tense
statements are superseded by the completed results at the end.
Existing stills bake8bog-mire plus10lit-fire fuel into1existing FireClay, retaining
mire remainder. No new recipe, hazard, item, upkeep or machine is proposed.

Source review finds creation before resource claims in LiquidSettlingPart.TryBake.
An ObjectCreated callback can re-enter baking while the outer action has no claims:
a nested12->4 then outer stale12->4 can yield2clay for only8mire. This is not yet
an executed reproduction. Independent review confirms the source path and requires
native separate/shared-transaction RED before treating it as a runtime defect.

Smallest proposed correction: join/create the transaction and reserve the actor's
single action, flask, station and selected fire before any creation callback.
Capture exact Parts, identities/custody/anchors/amounts and enforce them after
creation/output placement and before commit. Admit exactly one fresh portable
FireClay owner, not an owned, malformed or multi-unit substitute. Charge8mire and
10fuel exactly once. Native separate-transaction and same-transaction reentry
must both refuse. Interrupted source/output changes refund only owned deltas and
preserve independent injury, death, forced custody changes or other stock where
representable; diagnose unsupported raw mutation rather than overflow, heal,
restore into another liquid or erase replacement Parts. Output may merge into an
existing compatible clay stack through the existing inventory path. No blanket
actor/station snapshot may rewind independent callback consequences on this path.
Completion text/diagnostics follow commitment. Existing water/oil settling is a
separate contract and the closed oil/water cooling defects remain closed.

An actual useful target has been inspected through ordinary input: cached
Gleaners Overworld11.10 well9998 at41,8 is unrepaired/unusable, Stock=-1legacy
endless policy. Reader and failed zero-clay Repair2850/HP385 show the existing
2FireClay bill; refusal costs no turn. Source-guided approach from earned2760
used existing campbox2150 to store all20fixture clay2243, freely, preserving the
same owner. Ascend/Up/Right/descend plus local movement reached40,9. World-map
coordinates have30/3offset; the fourth requested localUp was not observed, so
actual cost is three moves to2850, with no collision diagnosis. Native C/Numpad9
reached the actual well. Original source13151 remains48 and actor poison/combat
consequences are retained. Archive-before-Stop:
/private/tmp/coo-regional-clay-before-earned-20261122, game8ab6a6384c2a44deb8b3a68303809d8f,
metadata2850/HP385. No processed-clay output or repair has occurred yet.

After the bounded defect is corrected: collect two12-unit flasks from actual
mire48->24, bake2clay leaving4+4mire with20process fuel plus measured ordinary
burn, and use only produced clay to restore the real well. Keep fixture20clay
stored and excluded. Feed/striker/capacities/HP are inherited fixture equipment;
ordinary acquisition, price/difficulty and balance are not established. Nearby
cellar clay and Morrowfast mender stock are source-supported alternatives until
actually observed. Lasting repair need not require a manufactured hydration debt
or repeated upkeep. Direct drinking restores the ordinary legacy service;
Waterskin filling is supported, while this pool-less well currently cannot fill
a generic LiquidFlask. Do not promise that missing common-sense interaction or
finite well tariff without its own increment. Native before/after reader,
function/action/refusal, actual movement, interruption and fresh persistence must
keep source, leftovers, stored fixture stock and repair cause coherent.

Lore: mundane functional pottery and practical well repair follow
Lore/History/08_MaterialCulture.md117–123; no new cosmology or protected fact.
All12complete-machine, general thermal/fractionation and NPC exposure gates remain open.

## Completed correction and player outcome

Native RED reproduced two clay for eight mire with both separate and shared
transactions. TryBake now reserves one actor action and exact flask/station/fire
before creation callbacks. Exact Parts, identities, amounts, custody, capacity,
heat and anchors are revalidated after creation, placement and before commitment.
Exactly eight mire and ten fuel are paid before publishing one fresh portable
FireClay. Spending the last ten fuel works at communal and installed camp stills.
Completion effects follow commitment; no saved fields or recipe changes.

InventoryUnitOutputReceipt admits one crafted stackable unit through existing
capacity and compatible-merge rules. Immediate failed placement uses the existing
transfer snapshot. Later undo retracts only that unit from the same Stacker,
preserving independent stock and forced custody. Replaced Parts/unrepresentable
raw mutations receive bounded diagnostics, not invented restoration guarantees.
Source refunds use identity-aware deltas. The command bypasses its blanket
snapshot only for this attached native baking contract, preserving independent
injury, fuel and movement. Ordinary water/oil settling is unchanged.

Actual native GameBootstrap play gathered24mire from source48->24, baked two clay
at the camp still, physically merged them, carried them to well9998 and repaired
it using only those units. Fixture20clay stayed boxed. Two timber, one striker
use,20process fuel plus0.75ordinary burn were measured;8mire remained. Normal
F5/archive-before-Stop/freshContinue3540 preserved produced stock; another fresh
Continue3640 preserved the repair/cause, menu and working direct drinking service.
Combat and poison damage were retained; no grants/teleports/stat resets/action
method calls. The existing restoration reward added30XP once,9->39.

This is a useful regional-processing alternative to source-supported cellar clay
and mender stock, not a price comparison or ordinary-equipment acquisition proof.
The repaired well remains legacy Stock=-1 endless, with direct drinking played.
Generic LiquidFlask filling at this pool-less well remains unsupported despite
broad reader wording; address that common-sense gap separately. Unloaded-fire
chronology and all twelve complete-machine gates remain open. Source/artifact
review is separate from actual play and is not a second native operator.
