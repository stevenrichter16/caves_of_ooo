# Finite poured fuel — bounded combustion correction

## Problem and smallest player-facing change

The native camp still→oil flask→fire-trap chain in
Verification/MachineSystems/ColdOilPlay20261121 exposed recoverable fuel after
its ignition film burned. Both heat- and ember-ignition should consume the local
ordinary poured oil, so a player cannot recover and reuse that same fuel.

## Policy frozen before native RED

- Add opt-in reaction data `ConsumePhysicalLiquid`; enable only the two shipped
  oil ignition rows. It requires a consumed input coating. Default false keeps
  custom/other reactions, physical water freezing/charge/steam unchanged.
- One ignition consumes **all represented ordinary poured fuel in that cell**
  as one bounded burst. Damage, smoke/embers and action time do not scale with
  volume. This is controlled abstraction, not ongoing fluid/fire simulation.
- Consume every distinct matching eligible owner, not whichever is enumerated
  first. Eligibility follows ordinary vessel-pool lifetime: ground
  PouredLiquidPool, positive matching LiquidPoolPart, unique attached part,
  nontakeable/unheld/unequipped/noncreature, one occupied cell, no Well,
  TileStateSource or FiniteLiquidFilm Part. The existing lifetime distinction
  between recoverable poured pools and authored sources is intentionally reused.
- Remove the coating, then zero/retire eligible pools through ordinary Zone
  removal **before damage callbacks**. Existing thin-film callback retains its
  ownership. Unrelated liquids/entities and storage vessels remain untouched.
- Existing saved poured pools qualify without a new saved flag or save-version
  change. Full exhaustion/normal removal prevents reload projection resurrection.
- No automatic re-projection, per-turn job, wall-clock processing or extra tick.
  Finite input/output: all eligible local fuel disappears; existing residue/cloud
  expires on authoritative game turns. Before ignition, normal collection remains
  available. An initiated ignition has no reversible fuel refund.

## Explicit exclusions

Natural OilSlick/TarSeep and multi-cell reservoirs retain legacy behavior. They
have independent coating/fuel/source contracts; zeroing a natural pool is not
sufficient to stop projection. Do not claim global combustion conservation.
Do not consume physical water whenever a tile reaction removes water, nor attach
the noncollectible finite thin-film Part to recoverable ordinary pools.

## Verification gates

Independent bounded source/design critique selected this whole-local-pool policy
over unit1 debit, which would need new remainder/reprojection/fire-cadence rules.
Native RED: both ignition paths,1/3/int.MaxValue units, co-located matching and
unrelated owners, legacy roundtrip, collection before ignition and failed recovery
after, nonignition opt-out, tile-only/finite-film compatibility, natural/multi-cell
and invalid ownership exclusions, plus pre-damage callback ordering.
Then minimal implementation, focused regression and adversarial checks, actual
GameBootstrap flask/trap and ordinary-fire interaction, fresh save/Continue,
independent final source/evidence review. Tests do not replace actual play.
Lore boundary: consumes existing poured oil only; no new setting/lore/mystery claim.

## Implemented and verified bounded correction

LiquidPoolPart owns the eligibility/debit; TileReactionSystem applies the data
opt-in after coating consumption and before occupant damage. The reaction
diagnostic records total physical units. No new saved state or action cost.
Native RED compiled23cases,11failed; GREEN229/229 focused regressions; five
behavioral mutants killed with no compile errors. Root actual play repeated the
earned camp checkpoint: trap burns1unit with unchanged12damage, ordinary pouring
beside hot ground burns2units through the ember reaction, both owners disappear,
and fresh Continue300/HP388 has no oil/owner resurrection. See
[receipt](Verification/MachineSystems/PouredFuelFix20261121/README.md).
Independent source review found zero P1/P2 blockers; final artifact review is
recorded there. No independent operator played Unity. Natural-source and
multi-cell conservation, all twelve machine gates and Mac delivery remain open.
