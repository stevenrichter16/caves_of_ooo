# GA02h — actual crafting recipients and one-weapon transformations

Status: COMPLETE. Baseline db30c145,8274/8274GREEN.

A46: AddObject could merge a new weapon away while forging returned its spent
orphan. Batch/composite consumers then targeted the wrong entity. Temper and
reforge also changed every weapon represented by a selected stack for one payment.
The repaired contract transforms one weapon unit, returns actual owned recipients,
and preserves all remaining units. Batch entries represent produced units and may
repeat one recipient reference. Optional quenching still occurs once after forging;
a refused quench leaves the successful forge and unspent medium.

## Verification sweep and implementation

Read actual inventory insertion/weight/receipt/transaction and command executor,
CloneForStack, all forge/temper/recompute services, permanent upgrades, crafting mark
and native station/UI consumers. This is CoO's component/tempering contract from
Docs/CRAFTING-ALCHEMY-SYSTEM.md §7.1, not a Qud parity claim.

| Verified premise or correction | Implementation consequence |
|---|---|
| A first compatible stack may already be full | Capture recipient at actual successful merge/append, never search afterward |
| Batch output references need not be distinct | madeCount counts produced units; repeated recipient references are valid |
| Both composite consumers quench once | Explicit one-weapon hint; separate forge/quench command transactions |
| Selected carried stack can contain many weapons | Clone one before debit and payload mutation; preserve equipped singleton identity |
| CloneForStack has no ID assignment | Give only new A46 clones fresh IDs; global A48 split-ID repair remains queued |
| Stacking ignores configured weight fields | Validate actual final carried weight after receipt-covered interim insertions |
| Actors may already exceed MaxWeight | Immutable ceiling max(startWeight,MaxWeight); allow non-increasing transforms |
| Factory/clone initialization may perform independent or same-TX work | Prepare first; enroll own undo just before own mutation, preserving true reverse order |
| Message publication is synchronous | Finish capacity/claims/marks first; keep undo armed through publication exceptions |
| Sharp rejects stacks | Pay both singleton upgrades separately, then actually merge compatible weapons in fixture |
| Brew batching requires a still | Native arena contains adjacent AlchemyStill and TinkersForge |

InventoryPart keeps its existing public AddObject capacity behavior. Internal
single-unit crafting insertion skips malformed nonpositive destinations. Transfer
receipts now accept multiple deduplicated incoming entities. A narrowly scoped
operation groups exact quantity/list/owner receipts and only the payload fields
written by temper/reforge. No whole Part, Stat, equipment or enhancement replacement.
New temper removal captures the exact newly added Part instance before attachment.
Marks move to the actual transformed recipient and restore their original instances
on ordinary rollback. One failing extension undo cannot prevent remaining payment
and payload restoration; failures are diagnosed.

Public service wrappers own a transaction; command overloads join their parent's.
Ordinary later batch refusal keeps earlier successful units. Exceptions roll back
the enclosing transaction, including earlier batch iterations. Preparation runs
before receipts so independent completed work is preserved. Same-transaction
preparation registers its own earlier mutation first. Final changed recipients are
claimed before callbacks, as are input weapon and payment before preparation.

A47 prerequisites covered here: positive carried forge components/quench/replacement,
positive owned target, abnormal equipped-stack refusal, truthful forge batch amount,
and exact reforge displaced-output restoration/prevalidation. Other A47 selectors,
brewing, planting, conversation refusals and tinkering output receipts remain queued.
Duplicated forge/quench payment and rollback helpers were consolidated into checked
inventory payment plus exact receipts; this is maintenance cleanup, not a claim that
an entire abandoned game mechanic was removed.

No new content/sprite, public saved field or save-format change. Public StackCount
remains the v7 field. No ordinary per-frame/per-turn work or speedup claim; the new
native driver is temporary and ends after its audit. Consequently no hot-path
performance benchmark is claimed or required for this one-shot inventory wave.

## TDD and review evidence

All successful XML runs below had zero compiler errors. The initial adversarial
fixture compile mistake (GatherActions already returns List) is retained separately;
no stale XML was treated as a run. Two older refusal-message assertions required
retaining the word “own”; implementation wording was made compatible.

| Artifact | Result |
|---|---|
| GA02h-adversarial-red-corrected.xml.gz | 97: 82 pass / 15 fail; 2026-09-06 02:05:33Z–2026-09-06 02:05:37Z |
| GA02h-expanded.xml.gz | 442: 440 pass / 2 fail; 2026-09-06 02:08:48Z–2026-09-06 02:09:00Z |
| GA02h-full-first.xml.gz | 8393: 8392 pass / 1 fail; 2026-09-06 02:12:11Z–2026-09-06 02:13:59Z |
| GA02h-minimum.xml.gz | 115: 115 pass / 0 fail; 2026-09-06 02:01:17Z–2026-09-06 02:01:23Z |
| GA02h-red-transaction.xml.gz | 52: 20 pass / 32 fail; 2026-09-06 01:55:28Z–2026-09-06 01:55:31Z |
| GA02h-red.xml.gz | 40: 16 pass / 24 fail; 2026-09-06 01:45:16Z–2026-09-06 01:45:18Z |
| GA02h-review-red.xml.gz | 117: 116 pass / 1 fail; 2026-09-06 02:10:55Z–2026-09-06 02:11:01Z |

Final additions:52 regression,60 dedicated adversarial,9 native staging
cases. Dedicated coverage includes actual paid payload splitting, same-part double
remerge, equipped Glow/wounded HP/slots, saved IDs and station rows, full/later/empty
destinations, explicit empty selection, finite/heavy-recipient/overweight capacity,
partial batch commit/rollback, independent participant claim rejection, throwing
publication/marker hooks and independent versus shared-transaction clone preparation.

🔴/🟡 fixed before completion: original orphan/whole-stack errors; nonpositive
recipient merge; grouped undo exception stopping later receipts; constructor-time
undo enrollment reversing nested preparation order. Default/flag-flipped controls
remain alongside each change. False-premise fixture corrections did not change game
rules. Independent runtime and native reviews cleared with no remaining must-fix findings. Two additional capacity-refusal controls and diagnostic assertions strengthened the gate.

## Native and honesty bounds

Verified actual keyboard route: brew3 from actual raw inputs; forge1 plain; forge2
plus one quench; station quench a remaining plain unit into the existing tempered
recipient; station reforge one tempered unit with IronSpike; inspect displacedSteel,
remaining quantities, marks and addressable IDs. Reflection only reads rows/state.

Can verify script-observable keyboard dispatch, recipient identity, paid quantities,
assembly/temper differences, repeated selection and diagnostic outcomes. The native
pack is finite but is not a capacity-boundary, overweight or refusal test. Those,
rollback, callbacks, equipment and saves remain EditMode evidence. Cannot verify
visual quality, readability, timing feel or rendering performance headlessly.
Known A31 camera teardown debt is tracked separately; native raw output will retain
any post-audit teardown errors without claiming clean shutdown.

Final focused136/136GREEN,02:17:03–08UTC,zeroC#errors. The first full8393
run had one compatibility-message failure; the separate non-melee rejection is
restored. Two later-capacity-refusal controls bring the new total to121tests.
2360assetGUIDsunique, no collisions. Current owned paths have zero intersection
with the protected pre-existing work manifest. Native/full final outcomes are recorded below.

Native completed **40/40PASS**, run **ce5eede85da24cbcbe85a8b2c00ddcbc**,
16.518107959seconds, launcherexit0,zeroC#errors. All40observations are retained
in GA02h-native.json and GA02h-native.log.gz. Actual controls brewed3, forged1 then
batch2+onequench (plain2/tempered1/media2), station-tempered one plain unit into
existing tempered2, then reforged one of those with IronSpike. Final actual weapons
were plainSteel1/temperedSteel1/plainIron1, returnedSteel1, brew1; the new selected
weapon had its own addressable ID. Two known A31 Camera MissingReferenceExceptions
occurred after native capture exit; no clean-shutdown claim. Full final suite completed below.

## Completion

Full **8395/8395GREEN**,02:18:42–02:20:24UTC,101.8574372seconds,
zeroC#errors (GA02h-full.xml.gz). Baseline8274→8395,**+121tests**:
52regression,60dedicatedadversarial,9native staging. Final focused136/136GREEN
and native40/40PASS. Independent root/agent taxonomy and reference cold-eye fixed
all identified must-fix findings before commit.2360GUIDsunique; owned files have
zero protected-manifest overlap. No outstanding A46 blocker. Recorded A31 teardown,
A47 remaining payment/receipt work, and A48 global clone identity repair remain
explicitly outside this completed wave. Next Wave2i covers planting/mineral dialogue.
