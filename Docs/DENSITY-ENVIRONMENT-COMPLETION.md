# Connected environmental behavior — density C8

Status: navigation and smoke visibility implemented with focused core/adversarial
verification. Native environment presentation remains pending.
Global combustibility/thermal changes still require the separately planned native
fire and counter-scene gate. No fire balance claim is made here.

## Scope and source corrections

| Premise | Verified source | Correction |
|---|---|---|
| AI ignores every hazard | `GasNavigationWeight.ForCell`, `FindPath.Search` | Preserve existing gas costs and extend their step seam. |
| Tile coatings always coat a walker | `MovementSystem.FireCellEnteredEvents`, `LiquidPoolPart.HandleEvent` | Only actual nonempty pool entities coat creatures. Tile-only acid is not damaging by itself. |
| All tile energies damage a walker | `ZoneTileState`, `TileReactionSystem`, `TilePropagationSystem` | Heat/cold/charge are reaction state, not a generic contact damage layer. Do not price hypothetical contact damage. |
| Pool and coating are two hazards | `Zone.ProjectPool` | Pools project permanent floor coatings; combine by maximum, not sum, to avoid charging twice. |
| Flying actors ignore slick floors | `LiquidSlipSystem.FindSlipperyLiquid` | Current slip contract has no flight exemption. Do not invent navigation-only immunity. |
| A body's anchor is its contact surface | `Zone.GetOccupiedCells(entity,x,y)` | Query every prospective physical cell with the existing allocation-free view, taking the maximum hazard. |
| Full resistance always makes lava safe | `LiquidCoveredEffect.ApplyStatModifiers`, lava JSON | Lava lowers heat resistance by25; damage immunity must account for incoming negative modifiers. |
| Cold/heat damage use arbitrary strings | `Damage`, `CombatSystem.ApplyResistances` | Follow actual type aliases and resistance rules; untyped damage is not heat damage. |

## Navigation substep

Add a pure bounded terrain-cost helper at the existing A* step seam. Read live
pool damage, harmful modifiers/action blocking, and actual slippery floor state;
respect relevant full resistance and preserve harmless/healing water paths.
Use finite penalties so a sole hazardous route and escape from a hazardous start
remain possible. Keep null-actor legacy searches unchanged. Do not allocate game
events, consume RNG, apply effects, mutate cells or cache actor-specific results.
Gas and terrain costs combine by maximum across the prospective body.

RED and matched controls will cover safe detours, forced passages, immunity and
vulnerability, empty/unknown sources, real pool versus tile-only state, footprints,
registry/actor absence, repeated searches and state/RNG purity. A dedicated
adversarial fixture probes malformed authored state, body boundaries and combined
hazards. Native visual/environment proof is a later C8 gate, not inferred from A*.

## Remaining C8 substeps

- Authored combustibility audit and native fire/counter-scene before scale changes.
- Connect actual thermal contact without double damage/reactions.
- Smoke visibility and temperature-dependent steam scalding.
- Sources for unsourced authored liquid/gas interactions after regional review.

## Review and evidence

The bounded navigation and smoke receipts below track RED through GREEN. This
is a CoO implementation using existing
mechanics; it does not claim full Qud navigation or thermal parity.

## Navigation implementation and review

- Core28 cases passed after missing-helper compile RED. Dedicated28-case
  adversarial sweep found3 real contract errors: two casing variants of incoming
  liquid immunity, and a modifier targeting a stat absent from the actor. Fixed
  only after the recorded56-case RED (53pass/3fail); final56/56 and52 nearby gas,
  path and slipping regressions pass (108/108 total).
- Q1: direct pool exposure and floor slip remain distinct; no second cost for the
  permanent coating projection. Q2: finite max90 and null-actor legacy route are
  explicit. Q3: immunity, unknown definitions, same-body offsets, missing stats,
  sole passages and changing registries have matched controls. Q4: navigation
  is conservative about incoming negative resistance modifiers and does not
  simulate arbitrary callback-based immunity. Ordinary combat balance and native
  path presentation are still pending.
- Files: new TerrainNavigationWeight and two test fixtures/metas; FindPath step
  seam uses the maximum across the complete prospective body.

## Smoke visibility sweep before implementation

`TileReactionSystem` already creates three-turn `smoke` clouds from oil/heat or
embers, and two-turn `steam` clouds from water/heat. Smoke currently changes only
readout/graphics. `FieldOfView` and `LightMap` test walls; `AIHelpers` tests solid
cells. Preserve each existing obstacle policy and add a shared cloud-only query;
do not make smoke physically solid or block projectiles by pretending it is a wall.
Steam remains transparent in this substep.

Changes to opacity must invalidate FOV and the cached lightmap when smoke appears,
is replaced, expires or is cleared/loaded. Ordinary wet/heat writes still repaint
only their cell. A new opacity version/hook should be driven by the sparse state,
not an extra whole-zone scan every turn. Old inactive-zone bindings must not dirty
the newly active zone. Add matched actual FOV/AI/light, expiry, replacement,
clear/load and active-binding controls before implementation. This does not change
the deferred combustibility scale or steam damage.

## Smoke implementation, independent review and evidence

Root authored the candidate after an 18-case core RED (13 intended failures and
five controls). The dedicated 21-case adversarial pass found two exception-path
failures: an observer could abort smoke mutation or successful load. The bounded
SightChanged observer guard fixes both. The final private candidate was 39/39.
The independent reviewer read all five changed production files and both new
fixtures before publishing the candidate, preserving the current torch/C13
changes; only documentation/indentation changed from that final candidate.
`Environment/smoke-publication-manifest.json` records before/candidate/published
hashes and the previous source is retained as evidence.

Actual authored `smoke` with positive lifetime now obscures FOV, AI intermediate
line of sight and direct light propagation. It remains visible at the ray's
endpoint, walkable and non-solid. Steam and unknown IDs stay transparent. A
sparse opacity epoch invalidates cached lighting, while full render dirtiness
occurs only on opacity topology changes. Extending unchanged smoke and ordinary
coating/energy writes preserve cell-only updates. Expiry, clear, replacement and
load all invalidate; inactive zones are detached when the active binding changes.

Shared current-source result: **181/181 GREEN**. That includes 39 smoke cases and
142 neighboring tile-state, tile-source, equipment/ambient lighting, faction-AI
and torch checks. Receipt: `Environment/smoke-nearby-green.json` / compressed XML.
No native FOV/lighting claim follows from this standalone group.

- Q1: each consumer keeps its previous wall/solid policy and adds the same
  cloud-only opacity query. Physical collision and projectile behavior are unchanged.
- Q2: version and full-dirty notifications both follow actual opacity changes;
  save data remains authored tile state, not callbacks or cached versions.
- Q3: clear/smoke/steam/unknown, expiry/extension, valid/malformed load, bounds,
  independent zones, rebound zones, throwing/reentrant observers and unchanged
  furniture/wall behavior have explicit controls.
- Q4: smoke is a bounded visibility feature. It does not implement steam scalding,
  thermal contact damage or a new combustibility scale. No blocking review finding
  remains in the inspected candidate.

Files: ZoneTileState, ZoneTileStateSystem, FieldOfView, AIHelpers, LightMap; new
DensitySmokeVisibilityTests and DensitySmokeVisibilityAdversarialTests (+metas).
FindPath's stale gas-only comments now describe the already-shipped terrain/gas
cost, with no navigation formula change in this publication.

**Can verify:** real core FOV/AI/light calculations, cached recomputation without
entity movement, sparse lifecycle, save/load and observer contracts.
**Cannot verify yet:** final native smoke readability, light/shadow appearance,
ordinary keyboard tactical feel or fire balance. Root owns the live acceptance.
