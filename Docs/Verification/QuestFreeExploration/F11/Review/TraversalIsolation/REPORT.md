# Traversal fixture isolation follow-up

The final affected native run contains654 cases:647 pass and seven fail. All ten arrival cases and all hunt cases pass. All seven failures are the pre-existing WorldMapTraversalTests methods that request a ground zone, and each fails on an unhandled unknown `Crate` blueprint log rather than a traversal assertion. The four methods that do not generate ground pass. No hunt production change is warranted by these failures.

## Cause and controls

The fixture creates an EntityFactory from MinimalBlueprintsJson, which deliberately omits settlement container blueprints. ContainerBuilder uses the ambient static ContainerPlacementService.Factory; if null, it stores the pipeline's minimal factory there. Ground generation then asks that factory for Crate, WoodenBarrel, Sack, WeaponRack and AlchemyShelf. The fixture neither supplies that dependency nor restores it. Its `new OverworldZoneManager` also publishes SettlementManager.Current.

The old blanket LogAssert.ignoreFailingMessages assignment is in SetUp. The installed Unity Test Framework runs setup actions in their own temporary LogScope and the test body in another scope; the current-scope property does not carry that suppression into the body. Native logs saying IgnoreFailingMessages:true are therefore not proof that the body ignores errors.

The previous F9 full result has all11 traversal cases passing, with only missing CrateT1 warnings on its ground-generating cases. This is consistent with a previous fixture having left a sufficiently complete ambient factory. The F11 pre-guard2442 result contains zero traversal cases, so it is not a passing baseline for this fixture.

A private paired source-control probe invokes the unchanged original Ascend_FromGroundZone_TransitionsToWorldMap method with only the ambient factory changed: null versus a factory loaded from the actual Objects.json. The null case logs missing container blueprints and leaves its incomplete factory installed; the full-factory case logs no unknown-blueprint errors and retains the supplied factory. Both expected-control cases pass. This proves the dependency in the private runner; it does not reproduce Unity's logging scopes or claim a native baseline rerun. The root's native seven failures remain the executed RED.

## Narrow fixture correction

The candidate changes only WorldMapTraversalTests.cs. It removes blanket log suppression and uses CreateDetached(factory,seed,true), preserving current exploration metadata while avoiding publication of the settlement registry. The single ground destination is cached as explicit open Floor cells before access. Actual WorldMap generation and the real Ascend/Descend methods remain. This isolates traversal from incidental population/loot generation without weakening its movement code.

All eleven original test method bodies, including assertions and coordinate expectations, are byte-identical; original-assertions.json records the check. Three cases are added: null and deliberately incomplete foreign ambient factories must remain untouched after an exact saved-coordinate round trip, and a real solid Wall at the default center must produce a neighboring passable descent. Thus the original expected coordinates are retained, and the collision fallback remains exercised rather than assumed away.

The private candidate passes14/14 with55 assertions and zero error logs. Both production and full EditModeTests assemblies compile against the current native reference boundary with zero errors; this is a compiler check, not native test execution. Root owns the native14-case rerun and any broader confirmation. No shared file, Unity state, Git state, production source or model was changed by this review.

## Evidence limitations

The private runner uses its established stable string hashing and a runner-only LogAssert shim which collects error messages; it does not simulate Unity LogScope lifetimes. The first private compile failed because TestTools was absent from its stubs, before test execution; build.log retains it. A collector shim was added only to the private runner, followed by successful compilation and all fourteen tests. Native-source compile logs do not use this shim. Causal results and native result slices remain separate from the candidate GREEN.
