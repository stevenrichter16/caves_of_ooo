# Drawn well supplies and spills — November 23, 2026

Status: implemented and bounded Editor-qualified. Native 252/252, actual separate
water/spill drawing and recovery, normal saves/fresh loads, and independent
source/gameplay-evidence review passed. See the [receipt](Verification/MachineSystems/WellSupplyProvenance20261123/README.md).
This resolves a documented policy conflict, not a newly introduced a9 failure.
The original full regression remains failed; [current Mac startup/artifact delivery](Verification/MachineSystems/MacCandidateFFF96F4C1/README.md) is qualified separately, with packaged controlled-input gates open.

`DENSITY-LIQUID-CARRY-POUR.md` explicitly preserves clean draws from a contained
well despite a spill around its base. Its legacy adversarial test still asserts
that rule. Commit e73e42ab3 instead applied occupied-cell purity to every Well
while unifying drinking and filling. The newer stock-well mixed-coating tests
therefore contradict that prior contract; the older test also fails alone on
ef5aecffe. Neither set of assertions alone establishes the intended rule.

Independent bounded source/design review supports restoring the existing supply
distinction, without inventing a saved protection flag or blanket contamination
immunity:

- A Well with no LiquidPool draws its abstract Stock supply. An unrelated ground
  spill does not become part of that supply, and drawing never removes, purifies
  or changes the spill. Positive Stock is finite; -1 remains the ordinary endless
  supply. This is controlled abstraction, not a claim of seals, elevation,
  acid-proof construction or complete contamination simulation.
- An attached LiquidPool is the actual exposed supply and takes precedence.
  Unlike pools/coatings in any occupied source cell still refuse every clean-water
  draw. An empty/unsafe pool cannot fall back to Stock.
- A Well combined with a TileStateSource remains subject to occupied-cell purity.
  In particular, an attached active non-water emitter cannot manufacture clean
  water through the Stock branch. Unsupported hybrid sources stay conservative.
- Direct drinking, waterskins and ordinary flasks share this admission rule.
  Repair, oath, phase/ice, custody, reach, quantity, action timing and transaction
  guards remain authoritative. Floor ice still blocks drawing under the existing
  phase abstraction; this correction grants no thermal insulation.
- A hazard under the actor is a separate contact/movement concern. It does not
  contaminate a different source cell. Drawing neither cures nor rewinds injury,
  exposure or other independently introduced state.

Smallest player outcome: an ordinary well/cistern remains drawable beside a
spill at its base, with exact existing costs and water accounting. Open seeps and
pools remain unsafe to sample when mixed. Clean alternative sources, carried
water and ordinary routes remain useful; no mandatory purification chore.

RED must retain the original failing test, cover all three consumers with finite
and endless Stock versus exposed pools, physical spill and coating, actor-cell
hazards, late contamination, exhausted pools and contradictory source capabilities.
Split superseded stock-versus-pool assertions explicitly, with exact quantity and
hazard preservation controls. Use compiling native runs and source pins. Actual
ordinary GameBootstrap actions, F5/fresh state and independent source/gameplay-
evidence review are required before qualifying the runtime correction. Native
fixtures cannot establish ordinary acquisition, difficulty, economics or hardware.

No new resource, saved field, source pool, filter recipe, water tariff, exposure
system or full-machine acceptance is proposed. Lore authority and existing water
hospitality remain unchanged; no new canonical explanation is inferred.

Completed: the original legacy success assertion is unchanged and now passes.
42 new cases distinguish supplies, preserve actual non-null acid exposure and
independent injury, and retain the last-unit metered tariff. Three superseded
stock-mixing fixtures now explicitly use exposed pools; they retain refusal and
rollback assertions. REDb40 had16 meaningful failures/24 controls; GREENb252/252
includes all42new cases. Removing exposed-supply safety is killed by9 failures
in43cases. No full-suite result is inferred from these overlapping runs.

Actual source-guided ordinary travel reaches a nonsolid TankBrocchinia17798 at
Overworld.3.4.0,47,6. A carried flask2235 pours its one mire unit beside that
source, then draws12water from the separate Stock supply. Mire19577 stays1;
direct drinking stays free. F5/fresh4580 matches measured custody/quantities.
The same C menu can recover that mire into2231(11→12), removing only its ground
owner and preserving the water flask; F5/fresh4590 matches again. No ground
purification occurs. Finite/metered/acid/oil/exposed-refusal cases are native
tests, not hand-played circumstances. Solid wells still refuse pour destinations.
