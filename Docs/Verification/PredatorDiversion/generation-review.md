# Predator diversion generation boundary — source sweep / review

Scope: current enabled manifest13 admits the existing hunt pair to the new capability. No hunt family, address selection, variant hash, population, source receipt, terrain, placement, loot or caller-RNG change.

Verified source contracts before implementation:
- `SpreadExplorationPlan.Legacy` uses the numeric current version despite Enabled=false; callers must guard Enabled as well as Version.
- Restore's whitelist currently uses CurrentVersion for12, so bumping to13 also requires explicitly retaining12. Version11/12 both require the saved WorldKey and exact accepted graph bindings.
- `SpreadExplorationBuilder.Hunt` already owns the exact cold-attempt authority. Only this caller should opt in automatically.
- `SpreadExplorationHunt.TryPlace` and `SpreadPredatorPart.Configure` are accessed by existing tests with non-overload-aware GetMethod. Add trailing optional parameters rather than overloads; reflection argument lists must append explicit false.
- Save serialization covers public Part fields, so missing historical diversion fields default false. Cached graph attachment does not run a composer or Configure.
- Final generation proof snapshots the configured role after pair admission; no separate mutable generation registry needed.

RED fixture: `PredatorDiversionGenerationTests` 9 cases. Supplemental reference compile succeeds; /tmp/predator-generation-red.xml: 8 failures exactly absent saved field/version13, one missing-manifest counter passes. Native RED pending at authoring. Source-pair full saves use actual source corpus without manufactured placement; the controlled cached-pair case is explicitly labeled and only checks that attachment does not upgrade it.

Planned review checks: symmetric true/false admission and save replacement; explicit12/11 restore and missing/cached counters; identical generated owner blueprints/locations/component shape/contents and random tail between current and literal12 runs. Current-version fixture pins advance, historical literal manifests and family enum pins remain unchanged.

## Published implementation / review

Root observed native RED job `3e76270c95a24382b04b13ed63371e67`: all59 completed, including the eight expected generation failures. Then the scoped generation gate and intentional current-version pin corrections were published.

- `SpreadExplorationPlan`: CurrentVersion13, explicitly restore12 in addition to11 and all prior supported versions. Allocation, family enum, seeds, addresses and wire columns unchanged.
- `SpreadExplorationHunt.TryPlace`: optional trailing flag defaults false and passes to the role's Configure. Existing source callers remain disabled.
- `SpreadExplorationBuilder.Hunt`: only the exact cold generation context with Enabled and Version>=13 passes true.
- `HuntFixture` reflection supplies false. Existing current-version tests pin13; historical wire/enum values remain literal. Two old fixture wire readers recognize13's WorldKey.

Supplemental verification: `reference-green.xml` 9/9; `reference-affected.xml` 323/323, including the new9 and existing manifest, hunt source/layout/pipeline/save, passage/cooking/hauling and fieldwork adversarial coverage. No failures/skips. Native follow-up remains root-owned.

Q1–Q4 generation review: the true/false states are symmetric and saved; literal12/11 and missing/cached countercases verify distinct authority; no duplicate opt-in registry/migration; public TryPlace inline contract describes default false. Actual-source comparison preserves blueprints, positions, owner/component shape, carried stock and caller random tail. Full graph reload replaces owners, preserves aliases and literal version/flag. No significant generation finding remains.

Independent role source review: no significant ownership/save/version finding. Revalidation pins the actual source reference/ID/anchor at each paid action; terminal diversion clears the reference before callbacks; the per-role reentrancy guard prevents a gesture callback from advancing another bite; ordinary drop/throw commits before subsequent native NPC idle scheduling. Original prey history and pursuit/search budgets are not changed by new approach/feeding steps. Role behavior, broader malformed-state tests, native turns and visual input evidence are separately owned by root/services agents; this review does not substitute for those results.

Final root-owned native evidence is recorded in the living design: the4240-case affected run included generation regressions; the104-case focused final GREEN included all9 new generation cases. No generation production changed after those native runs.
