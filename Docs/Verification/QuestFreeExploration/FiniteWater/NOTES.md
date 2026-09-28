# Finite-pool committed draw invalidation

Status: private minimum candidate; **100/100** isolated core checks pass (25 new, 75 existing liquid neighbors). Actual Unity runtime and full EditMode-reference compilation pass with zero errors. Native execution remains root-owned and pending.

## Verified source boundary

Both water-vessel services decremented the public pool volume, then only called the exact PouredLiquidPool retirement helper. A persistent finite pool therefore emitted no service-level render invalidation. The ordinary inventory path already closes inventory and calls ZoneRenderer.MarkDirty (InputHandler.CloseInventory), so this is **not a demonstrated keyboard stale-image failure**. The new callback supplies the missing service contract for committed finite draws.

The shared observer captures the original Cell before mutation, then requires the same current owner, pool/backlink, anchor, and nonportable physical ownership. It retains the existing PouredLiquidPool retirement body; an exhausted poured source is still removed normally. A persistent source stays in its exact cell at Volume0 and receives one cell dirty notification. Outer rollback/refusal never calls the observer; stale/moved/replaced source or pool cannot publish another cell.

## Test-first evidence

- red.xml: initial23, 6 real missing-notification failures /17 controls.
- green.xml: first98/98 (23 +75 neighbors).
- stale-poured-red.xml: preserved baseline25, 8 real failures /17 controls. Two supplemental cases prove the old observer wrongly removed an exhausted poured owner moved by an outer transaction. The new identity gate precedes the unchanged retirement body.
- final-green.xml: final100/100, including25 new controls and75 unchanged neighbors.
- candidate-tests-first-compile-failure.log: native test assembly rejected one internal Entity.SpatialZone assertion that the single-assembly runner admitted. Replaced only that assertion with exact public cell membership; latest runtime/tests compile logs both0 errors.

Fixtures use explicitly constructed finite source/vessel graphs and actual services/transactions, not native player input or a generated source claim. They snapshot/restore LiquidRegistry dictionaries/initialized state, MessageLog message/tick/serial lists and callbacks, and renderer dirty callbacks. Stale callbacks are distinguished from ordinary removal/move invalidations by clearing prior observations only after the explicit stimulus.

## Q1–Q4

Q1: both successful fill paths share the same after-commit observer; neither rollback path invokes it. Poured retirement remains its existing normal Zone removal path.
Q2: owner/part/cell authority follows current finite pool semantics; no new quantity, source, contact, save, or UI cost changes.
Q3: partial/depleted positive pairs cover both services, outer commit idempotence, rollback, full refusal, removal/replacement/part replacement/move/foreign backlink, existing poured retirement and infinite well. Two actual baseline negatives specifically pin moved poured source protection.
Q4: direct service invalidation is the claim. No native art acceptance, keyboard break, renderer-pixel result or general LiquidPool field setter is claimed. Existing full inventory redraw remains an independent path.

Private runner copies the accepted stable-hash stubs and runtime-only Unity shims. No shared runner/Unity/editor controls were used; private compile source selected current shared gameplay with only the two candidate service replacements.
