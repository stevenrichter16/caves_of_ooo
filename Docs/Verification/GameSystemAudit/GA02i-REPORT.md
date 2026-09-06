# GA02i — planting truth and positive mineral payments

Status: COMPLETE. Baseline d219936a,8395/8395GREEN.

Planting used to return success even when ground, placement or seed payment refused.
An empty first mineral stack could pay reputation or hide a later valid unit, and
cached SaltMaster dialogue could advance after its payment disappeared. This slice
makes planting's event result truthful, requires a positive carried payment, and
lets required dialogue actions stop the choice pipeline on refusal.

## Verified scope and corrections

Read the complete five runtime targets, inventory command/event contract, actual
SaltMaster and Founding content/dialogues, CropPlanting/Founding/Handover tests,
CROPS-WATERING §2.2 and FellingSiteRules counterfixtures. This follows existing CoO
planting and mineral contracts, not a Qud parity claim.

| Verified fact/correction | Result |
|---|---|
| Inventory action succeeds on Handled or false event propagation | Failed planting returns true/unhandled; only paid planting handles the event |
| SaltMaster actual reward is TentRight+5 | Native and actual-content tests use that reward |
| Founding offers CatacombFolk+50 once | Keep permission, standing, reach and any-nonzero fact gates |
| IfHaveItem already skips nonpositive entries | No redundant predicate repair |
| Explicit and automatic selections differ | Exact selected seed cannot borrow another; automatic mineral search skips empties |
| SelectChoice bool means dialogue still active | Assert node/payment/required diagnostics, not bool as action success |
| These actions currently cost zero ticks/energy | Preserve and test that; no turn-saving claim |
| Crop factory initialization can change seed ownership/quantity | Remove exact newly placed crop if subsequent payment refuses |
| Felling barren + Plantable tag is a synthetic counterfixture | Do not claim that terrain combination is authored |
| Internal selector is inaccessible to test assembly | Pure-query test uses reflection; no public API expansion |
| Missing blueprint logs an error | Exact expected-log assertion, not ignored logs |
| ConversationLoader.Reset reloads Resources on Get | Explicitly load an empty set for missing-conversation fixture |

InventoryPart now has one pure internal positive-blueprint selector. MineralTrade
uses existing checked one-unit payment before reputation; Founding permission uses
the same selection while retaining distinct empty-stack reasons. SeedPart validates
actual carriage, returns the real result, cleans up only the newly placed crop on
late payment refusal, and describes one planted seed in success prose. Required
SellMineral and OfferFoundingStone adapters preserve existing spoken outcomes and
legacy ExecuteAll/registry override/reset behavior while stopping required pipelines.
Duplicated mineral and seed payment loops were removed in favor of the checked
inventory operation. No removed mechanic or speculative dead-code claim.

No new content/art, public saved field, format migration or ordinary hot-path work.
A47 brew/tinker selection and tinker output receipts remain another slice. General
callback/effect rollback A41, global clone IDs A48 and other audit work remain open.
The native driver is temporary; no performance speedup is claimed for this wave.

## TDD and independent review

Initial26RED:20fail/6controls; one-unit message assertion26RED:21fail/5controls;
late payment29RED:23fail/6controls. Minimum250GREEN. Dedicated first compile failed
on internal-method access; retained log. Corrected67run:66pass/1missing-blueprint
expected-log fixture failure. Corrected gameplay67passed in staging75RED with only
8missing-bench failures. Native compile then needed Data import; retained log.
First expanded296run:295pass/1missing-conversation fixture failure, corrected above.
All XML evidence is retained alongside this report; compile failures never used XML.

Final additions:29 regression,38 dedicated adversarial,8 native staging cases.
Root and independent taxonomy/reference cold-eye found no remaining production
must-fix. Ten attempted-break hypotheses were exercised after minimum implementation:

1. Exact empty seed borrowing a later valid stack: zero/negative refusal and later positive success.
2. Stale owner backreference bypass: actual owner/missing backreference/actorless controls versus wrong actor, ground, stale and equipped metadata.
3. Refusal firing after-action or success: terrain/occupied/placement/veto controls pin event truth and callbacks.
4. Crop retained after initialization invalidates payment: remove/empty/keep controls pin exact cell, zone and Crop tag-index cleanup.
5. Missing factory/blueprint or wrong explicit zone accepted: failures versus absent-context fallback.
6. Automatic selector chooses empty or later positive: four distinct entries, exact ordering, singleton/multiple quantities.
7. Pure queries mutate inventory/diagnostics:100repeat case/null queries, identical lists, penalties and quantities.
8. Checked payment changes supported configurations: no-Stacker singleton, no faction, zero/negative reward controls.
9. Cached choice advances unpaid or cannot retry: removed/empty/kept payment, reach/standing changes, fresh-salt retry.
10. Required registry/latch compatibility breaks: both action names, legacy continuation, override/reset, exact rejection payloads and nonzero once facts.

These are pinned invariants, not ten newly discovered bugs. Malformed quantities,
Physics-only equipped metadata and crop initializer callbacks are explicit extension
fixtures. No narrative save round-trip or generic action exception rollback claim.

## Native and honesty bounds

Disposable arena uses actual CandyCarrotSeed2, PaleSalt1, Tepuibone2; hard floor then
adjacent Grass; actual SaltMaster/FoundingPlaqueTender with unmodified conversations.
NPCs deliberately receive no turns so they remain adjacent. Native keyboard enters
inventory, chooses Plant, moves to Grass, plants and retries occupied ground, then
sells salt and offers one stone through actual dialogue. Fresh tick/energy snapshots
exclude the real movement. Once-only offering stays hidden despite a spare stone.

Can verify script-observable native keyboard dispatch, quantities, crop identity,
service diagnostics, rewards, row changes, cancellation and zero action-time cost.
Command boolean/AfterInventoryAction suppression, adapter diagnostic attribution,
malformed stacks, cached stale choices and extension callbacks are EditMode evidence.
Cannot verify visual quality, readability or buttery feel headlessly. Known A31 camera
teardown debt will be retained in raw output; no clean-shutdown assertion by omission.

## Completion

Focused296/296GREEN,02:56:39–48UTC. Native44/44PASS,
runbc503b68197b4bd3b37d1a04c8673b3f,9.192578375seconds,exit0,
zeroC#errors and no logged exceptions in retained native output.
Full8470/8470GREEN,02:59:15–03:01:02UTC,107.1199362seconds,
zeroC#errors. Baseline8395→8470,+75tests (29regression,38adversarial,8staging).
All evidence retained as GA02i-focused.xml.gz,GA02i-native.json/log.gz and
GA02i-full.xml.gz.2367assetGUIDsunique,zero collisions. Owned paths have no
protected-manifest overlap. Independent reviews cleared; test-fixture cleanup now
resets FactionManager even in diagnostic-only cases. A47's remaining brew/tinker
slice remains queued while the accepted crafting smoothing wave starts next.
