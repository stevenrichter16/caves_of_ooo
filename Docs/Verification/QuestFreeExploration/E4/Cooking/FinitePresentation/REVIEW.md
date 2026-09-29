# Finite cooking rest and focused readout — bounded review

Private two-file candidate; no station/data/version/placement or Unity execution. Core cooking39 remains unchanged.

## Evidence and order

The pre-implementation plan and source corrections are in PLAN.md. Initial35 executed against unchanged shared source:10 PASS/25 expected RED. The implementation then passed35. Eleven ownership/state controls were added without additional production changes. The exact final46 against unchanged shared source gives10 PASS/36 expected RED; candidate plus frozen core/everyday/rest neighbors gives182/182 PASS. Both current Unity runtime and full EditMode reference compilations completed with0 errors. These are private runner/reference results, not native Unity test results.

The old-wire tests construct actual current-runtime zero-field and prior FiniteCooking-only wire and invoke the real LoadPart path. They prove missing AllowRest initialization through Part's Activator construction, not compatibility with an archived historical Unity binary. Full session round trips separately retain AllowRest false/true, finite state, exact replaced owners, heat/fuel and turn state.

## Q1 — symmetry

AllowRest independently gates gathered Rest and both direct rest commands before clock/HP changes. Legacy true remains the default. Both finite true and false are exercised; heat is not rest authorization.

## Q2 — consistency

Readout shares CookingService.MinimumFiniteCookingTemperature. A hot source with exhausted fuel is described as still hot but unusable. The finite glyph and proximity branches preserve authored color and omit the legacy crackle. No new world Cook command, heat/fuel debit, material cadence or station selection change is introduced.

## Q3 — counter-checks and authority

Actual world-reader checks exact active zone/cell/current owner, installed Campfire/Render/Physics parts and visible state; malformed Fuel/Thermal cannot advertise readiness. Paired hidden/stale/moved/foreign/carried/equipped controls start with a positive read and assert actual mutation where needed. Reads preserve source values, graph version, messages, HP, energy and clock. Direct refusal and legacy success are tested through InventorySystem; snapshots restore borrowed state, and reference compilers validate native APIs.

## Q4 — scope and remaining gates

This slice suppresses only Campfire glyph flicker and warm proximity text. ZoneRenderer ember registration, particles, light/model state and new SpreadCookingCoals source activation remain separate work. No fully quiet finite presentation or player approach timing claim is made. Current owned core39 and toasted art are independent. Native tests and any later visual/readability gate remain root-owned.

Combat and renderer independently read the current two production files and current-visible/rest/readout seams; both found no concrete blocker. No additional code change resulted from review.

## Final factual wording correction

Root requested the ready hint qualify the ingredient: carried raw food, because prepared meals have no Cook action. The narrowed real reader assertion ran6 cases:1 intended wording RED/5 controls; the one-string correction then passed all46 presentation cases. Earlier182/182 and matched46 baseline remain explicitly pre-wording receipts; core behavior is unchanged. The final source/test hashes are in the refreshed manifests.
