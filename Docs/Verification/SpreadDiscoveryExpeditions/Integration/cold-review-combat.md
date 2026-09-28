# Discovery/wayhouse cold review

Read-only review of exact source/test snapshots listed in `cold-review-combat.json`; no Unity, source writes or staging. Findings were sent to root before final integration acceptance. The frozen reviewed source snapshots remain under `/tmp/coo-discovery-wayhouse-review`.

## Q1 — symmetry

Creation → staging → final acceptance → save → restore → retention is the correct lifecycle. Normal restore binds the saved graph after entity bodies and physical indexes exist, with the nullable manager guard retained. Depletion does not cause regeneration and no source is recreated on restore.

Two important asymmetries existed in the original reviewed snapshot (resolved by the follow-up below):

1. **Final validation is stored on the replaceable plan.** `CommitGeneratedZone` asks only the current plan to finalize; a replacement plan has no pending zone and returns success. A repeated builder invocation also clears the original pending validator before refusing its consumed receipt. Pin actual staged packet → changed plan / second call → final acceptance against unchanged controls, and retain final authority outside the replaceable selection.
2. **Owner freshness is checked after stocking.** Key/reward `ObjectCreated` callbacks can move the previously captured sack/cache into a foreign graph. The later `AddItem` mutates it before `staged.Any(SpatialZone != null)` refuses. Check exact freshness, empty contents and part backlinks after callbacks, before any stock mutation; assert foreign graph unchanged.

## Q2 — cross-feature consistency

M1 is separate historical knowledge: map-only eligibility, no remote graph access, and plain saved player-property records. It does not convert Wayhouse selection into a live reward promise. Wayhouse world metadata, optional generation refusal and graph retention are separate authority. Caller RNG and ordinary receipt provenance remain explicit. The native role markers, fixed key ID and ordinary Buckler are consistent with the plan.

The packet validator treats container and inventory ownership differently. Containers check current part/child backlinks; guard inventory/equipment currently only traverses child references. A narrow foreign-inventory/equipped-backlink counter should precede any strengthening, reusing actual body slots and deduplicated equipment semantics.

## Q3 — counters and evidence

Reviewed source tests cover deterministic selection, disabled/empty metadata, actual generated seeds 1/64/1729, repeated source consumption, changed door/cache before final acceptance, malformed goods/IDs/appearance, hazard reads, and exact depleted/untouched reward save restoration with retention. The three findings above are missing boundaries in that snapshot; no executed RED is claimed by this reviewer.

M1 has actual **54/54 native GREEN**, job `675459b3bf4043c9985f8ffcaf21db4d`, `../Tests/m1-wayhouse-green-key-route-red.xml`. The overall batch's one key-route failure belongs to Wayhouse, not M1. Independent native keyboard dialogue/journal/save and Wayhouse traversal/acquisition remain separately owned gates.

The M4 composer peer read found no concrete source/rollback blocker: exact receipt owners/factory/terrain, no factory or RNG in compose, complete virtual occupancy preflight, and exact-owned position rollback. This does not prove performance or a playable route; its bounded shelter search and actual native bypass still need measurement.

## Q4 — documentation limits

Do not call selected sites committed, source/native unit passes a whole journey, or unconfirmed notes a current occupant/reward tracker. Mark Wayhouse's final callback and foreign-owner corrections pending until actual paired evidence is available. M1 logic is complete; its actual informant/UI path remains pending. Missing/invalid older selection stays disabled. Ordinary visited/saved state and explicit protected Wayhouse retention are distinct from ordinary wilderness regeneration policy.

## Follow-up: three findings resolved

Root reproduced the named boundaries, then added original staged-plan authority on the manager, preserved the pending validator through a repeated refused build, checked exact fresh empty furniture after callbacks before stocking, and validated guard inventory/equipment/body backlinks. A bounded reread found no remaining concrete blocker in those repaired seams. The original source snapshots/findings above remain historical; current reviewed hashes and named native class counts are in the JSON follow-up.

Actual native job `f0f7ee05c1034d84ab4051713c771a8f`, `../Tests/wayhouse-m4-green-launcher-red.xml`, confirms Wayhouse **42/42 GREEN** and M4 **83/83 GREEN**. Two missing-launcher failures in that batch belong to the separate native audit harness. Root has subsequently added natural composer wiring after its structural RED; matched full-pipeline conservation/census and keyboard/visual/ordinary-journey acceptance remain open. No Unity or source changes were performed by this reviewer.

### Natural pipeline follow-up

Root's new structural wiring and paired full-manager census now pass. Parsed receipts: `full-pipeline-wayhouse-focused-green.xml` is97/97 (M0 census3, Wayhouse42, structural pipeline3, composer49); `native-census-launcher-green.xml` is45/45 (census3 plus restoration42). Overlapping groups are not additional unique totals. Actual native commits/selected are3/8,4/7,7/10 in seeds1/64/1729. Bounded census source reread found no concrete blocker in the actual4299 snapshot, identical global RNG setup, equal final caller-RNG sample, current-reference/state projection and authorized-coordinate comparison. This is cold generation, not acquisition or a complete serialization proof. The main native Play route is currently running under root.
