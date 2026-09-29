# Post-Play regression-state diagnosis

Status: private matched diagnosis only; no runtime or fixture repair. Existing isolation debt is deferred at the user's priority boundary. The first native full result remains **21,734 passed / 9 failed / 21,743 total**; these experiments do not replace it. Root separately reported 63/63 affected native tests passing on the identical source after the full run, and is running a fresh-domain full sweep.

## Torch: exact retained-reaction reproduction

| Private thermal source | Reaction registry | Original assertion | Fuel after one tick | Burning intensity | HP |
|---|---|---|---:|---:|---:|
| Current notification | Empty | PASS | 49.7 | 1 | 999 |
| Current notification | Shipped organic only | FAIL | 49.475002 | 1.5 | 999 |
| Current notification | All 13 shipped reactions | FAIL | 49.475002 | 1.5 | 999 |
| Before notification | Empty | PASS | 49.7 | 1 | 999 |
| Before notification | Shipped organic only | FAIL | 49.475002 | 1.5 | 999 |
| Before notification | All 13 shipped reactions | FAIL | 49.475002 | 1.5 | 999 |

`DensityTorchAdversarialTests.Setup` (Assets/Tests/EditMode/Gameplay/Items/DensityTorchAdversarialTests.cs:13–14) does not capture, reset, or restore `MaterialReactionResolver`. Its test at22–23 assumes just0.3 fuel loss. The actual organic torch blueprint has BurnRate0.3 and Organic material (Objects.json:20425–20476). `BurningEffect.OnTurnStart` consumes base fuel at99–101 then invokes the resolver at185. The shipped `fire_plus_organic.json:13–14` first adds0.5 intensity, then applies a1.5 fuel modifier. Resolver:232 calculates the extra0.3×0.5×1.5=0.225. Therefore50−0.3−0.225=49.475, exactly matching the native failure. No weather multiplier is involved.

`GameBootstrap.cs:122` normally installs the real reactions. This demonstrates a concrete post-Play/static-state route, but these private checks did not capture the precise prior writer in the failed native domain. The matched previous ThermalPart proves the arithmetic is independent of the new finite-only notification.

## Merchant: independent loadout-state countercheck

Using the actual Merchant factory with only `LoadoutPart.Factory` toggled reproduces the capacity premise. Factory null produces no equipment; 99+51 Starapples fit the150 capacity. Factory wired produces actual ShortSword+LeatherBoots,8 weight; removing only `InventoryPart.Objects` leaves that equipment.99 apples bring weight to107 and the second51 stack correctly refuses. Both paired assertions pass.

`GameBootstrap.cs:220` sets the loadout factory. `LoadoutPart.cs:86–104` applies authored gear on ObjectCreated when the factory exists. `InventoryPart.cs:437–450` correctly counts carried and equipped weight. `GameAuditTransferBench.cs:55–61` removes carried stock only before trying150 apples; `GameAuditTransferTests.cs:143–145` assumes empty Objects implies capacity for up to150 torch weight. The acquisition bench's100 expected versus108 actual follows the same8-weight gear difference. This confirms the claimed setup dependence, not an inventory gameplay regression.

## Disposition and limits

No notification, thermal arithmetic, stock, equipment, tests, or shared files were changed. The exact native nine failures remain preserved. Future fixture maintenance should explicitly snapshot/isolate/restore material reactions and loadout factory/RNG, or deliberately assert the fully bootstrapped content behavior with paired controls. This low-priority isolation maintenance is not included in the finite cooking release. No claim that every post-Play global is isolated, or that the private runner replaces native evidence.

Thermal current SHA256: `de6fe2bc2f0ba0921fd8dcb73720d2c8b4b7f94b0f7d011faaaadb9c246ef381`.
Thermal prior SHA256: `d671e3debc50185249cd3088cba576a3ff5cc8f745b93d8809afb08141772f38`.
