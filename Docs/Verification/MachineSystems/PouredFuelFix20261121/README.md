# Poured fuel combustion correction — November 21, 2026

**Result:** eligible ordinary poured oil now burns away when either heat or
embers consumes its film. The recoverable physical pool is exhausted/removed
before occupant damage. This fixes the demonstrated reuse loophole without
changing trap damage/time or water reactions.

Design/source correction: [FINITE-POURED-FUEL](../../../FINITE-POURED-FUEL.md).
Natural authored sources, wells/replenishing sources, multi-cell bodies and
invalid custody are excluded. Thin films retain their existing retirement.
All represented local poured fuel burns as one nonproportional burst, not one
unit per turn. No new save fields or clocks.

## Native tests and bounded independent source review

- `poured_fuel_red_20261121_b`:23total,12pass/11fail,0compile errors.
  Actual pour/recovery, both ignition paths, int.MaxValue, legacy/full-session
  resurrection, co-located owners and pre-damage callback expose the bug.
- `poured_fuel_green_20261121_a`:229/229,0compile errors. Includes new23,
  TileReaction, SpringTrap, DensityLiquidVessel families, LiquidPoolPart and
  ThrowPayloadPersistence. These targeted results are not a new full-suite run.
- Five [mutants](mutants.json) killed: no debit11fail, ember opt-out3fail,
  retained volume11fail, source consumption2fail, steam consumption1fail.
  All compiled. Exact XMLs and [summary](test-summary.json) retained here;
  logs remain `/Users/steven/coo-verification/results/poured_fuel_*`.
- First test-authoring attempt `_red_..._a` did not compile (fixture/internal
  API mistakes), corrected before `_b`; it is **not** RED evidence.
- Independent read-only source/design reviewer found zero P1/P2 blockers.
  Root cross-checked callback order, eligibility and reaction data against source.
  Final independent evidence review does not constitute another game operator.
  Final source/artifact review found zero P1/P2 publication blockers; resource
  wording was corrected to distinguish liquid oil from the discrete LampOil item.

## Actual GameBootstrap after-play

Used the earned220 archive from ColdOilPlay20261121, game
`d3c56928ecec4cd0a4532b3402867974`; fixture400HP/Strength70. Native keyboard
menus/movement/F5/Continue; bridge used only for launch/configuration/readbacks.
[Exact native observations](native-observations.json).

1. Fresh Continue220: flask2231 oil3, loaded fire kit2146, camp intact.
2. C west pour-one220→230: pool2716 at35,10 oil1; carried flask2. C west lay
   trap2717 at230→240. Step240→250: HP400→388, trap and pool gone,
   embers3/smoke2, no Burning. Underfoot C menu has no collection row.
   [Visible aftermath](native-after-trigger.png) shows embers/cloud and no oil
   puddle; the brief preexisting Oily contact effect expires normally.
3. Ordinary C west pour-all2 at250→260 onto34,10 beside hot ground ignited
   immediately. No new trap/strike/ability was used. Both units and their owner
   disappeared; HP388. Diagnostics distinguish `ignite_oil_heat` at35,10 costing1
   from `ignite_oil_embers` at34,10 costing2. This is the useful ordinary-world
   interaction outside a machine; still output→trap/embers is the cross-system
   chain. Its prior still processing was earned in the earlier receipt, not
   replayed here. No claim of a flint-striker or Ember Spit playtest.
4. Four ordinary waits reach300; both cells' residue/cloud cleared, no remaining
   carried oil. F5 then separate stopped/fresh Play/Continue retains300/HP388,
   no pool/trap/oil and no coating on either cell. Saved matter does not return.
   No comparison of world-clock phase or pixels is claimed.

Archive: `/private/tmp/coo-poured-fuel-fixed-earned-20261121/` plus game directory.
One launch/readback attempt occurred before Play started; its null observer
results are not gameplay evidence. `manage_editor` entered Play, then native
Continue and readbacks succeeded. No gameplay exception was found in that attempt.

## Preservation and limits

Main stopped clean/nullroot, original input restored/temporary devices0. Fresh
hashes:84foreign source and861normal game files unchanged against retained
baselines, Unity analytics/TestResults excluded. No whole-prefs/whole-directory
byte-exact claim. All prior builds retained. Clone3 ends with the final steam
mutation: **resync owned production source before any future test**.

No standalone Mac acceptance, independent game operation, NPC/Grove trial or
new performance qualification. Tests used the existing32GiB machine; no new
benchmark or broad full-suite run. This bounded bug correction does not accept
any of the twelve complete machines or solve natural-reservoir combustion.
