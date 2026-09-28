# Final bounded acceptance and persistence review

Status: read-only production audit; a two-case test-only save bridge is private. No Unity invocation, shared Assets edit, production change, test-runner mutation in the repository, or staging. Root owns the running full native suite. Review baseline: `6015ba8b9c31bfe52658f8ec8b0bcf6f98218a14`.

## Findings and limits

No additional concrete production blocker was found. The precise missing automated bridge was partial looting of an actually relocated ordinary cache, keeping other entries and guard equipment, then leaving it cached and saving/loading before returning. M4's 83 receipt/composition/adversarial cases and three paired full-manager census cases establish current-reference relocation and content/RNG preservation, but do not themselves exercise that lifecycle. `SpreadWildernessPipelineCensusTests.cs:64` only checks repeat cached `GetZone`; it is not a save or a paid player journey. The former Wayhouse42 shorthand was also too broad: the current50 source cases include untouched/fully depleted singleton reward saves, not partial multi-entry caches.

The existing persistence route is appropriate: `ZoneManager.cs:44-60` returns the cached graph; `SaveSystem.cs:905-909` saves each cached zone; `SaveSystem.cs:1146-1192` saves actual cells/entity references and rebuilds ownership on load. M4 introduces no save hook and no serialized receipt. Explicit ordinary `UnloadZone` remains regeneration, unlike the new bounded accepted-Wayhouse retention. No new global depletion or reconstruction promise follows.

Existing inventory positive/refusal controls are `InventorySystemTests.TakeFromContainer_TransfersItem`, `TakeFromContainer_TooHeavy_PutsBack`, `TakeFromContainerCommand_TooHeavy_RollsBackToContainer` (lines3322,3349,3550), plus merge/rollback controls. The new bridge also pairs an actual full-capacity refusal with success on the same source item, checking exact remaining IDs/counts/backlinks. It makes no new action-cost claim.

## Two-case bridge

`SpreadWildernessPersistenceTests.RelocatedPartialCacheAndGuardGearSurviveCachedReturnAndFullSave("cargo"/"shelter")` uses current terrain/population/container/haulable producers and composer. A bounded fixture-only RNG selection seeks real rolled multi-entry stock; no entry is inserted, replaced or refilled. Both current isolated cases use producer RNG16. This is a controlled command/save compatibility witness, not an ordinary acquired-source run or full-manager population census.

Each case proves actual cache displacement; genuine remaining stock after one native Take command; same-owner capacity refusal; exact taken/remaining item IDs/counts; guard carried/equipped signatures and anchors; player/zone/owners replaced by full save/load; cached away/return without recomposition/refill. Player transfers and an empty away-zone are explicit fixture setup. No `UnloadZone` is called. Two cases passed in an isolated runner; actual current Unity test-assembly reference compilation passed with zero errors. Unity execution remains pending and must be reported separately from the already running full suite.

No production RED/GREEN is fabricated: the existing implementation was expected to pass. The first reference compile correctly rejected two direct internal `SpatialZone` field accesses; assertions now read that internal field through reflection. The original diagnostic is retained as `reference-compile-first.log`; final `reference-compile.log` is clean. Peer review also found that the original generic-dictionary Cast snapshot would throw when the registry was populated. The fixture now snapshots via direct IDictionary enumeration, with null-safe cleanup. A controlled standalone host seeded the actual 98-entry initialized registry before both cases and checked the exact original dictionary/entry references and initialized flag after:2/2 GREEN and exact restoration true (`nonempty-green.xml/.log`, `nonempty-host.cs.txt`). This is a fixture repair only. The isolated fixture shim does not reproduce Unity scene/static-isolation work; actual native execution remains required.

## Acceptance wording to retain

- Final goods source gate: corrected meaningful native39=31PASS/8RED, then current Wayhouse50+economy3=53/53 GREEN. Preserve earlier six genuine/two false-premise count probes and the51/53 intermediate result.
- Main19 validates generated Wayhouse front/rear, exact key/reward/equip and saved depletion with disclosed original-player transfers. It does not establish the ordinary continuous Wayhouse journey requested at plan line222.
- Ordinary14 establishes actual N/report/grain acquisition/Eat/return/save; it is the real no-transfer surface loop.
- Cards9 establishes actual cargo/shelter model/layout/readers, physical pair clue and a paid shelter bypass. It does not establish cargo partial Take by keyboard or separate shelter combat requested at plan line304. New bridge2 establishes partial-cache lifecycle at core level only. These are documentation/acceptance boundaries, not reproduced gameplay defects and not a reason for an open-ended new route bot.
- Full current native suite is still running. Performance's measured component CPU scope must not become a whole-game/GPU/allocation neutrality claim.

## Inventory

Current published additions:15 new fixture files,225 distinct case names present in native XML; two restoration rows make227 new cases versus the source baseline. Private bridge adds2 later. M4's83 is receipt34+composition26+adversarial23; structural pipeline3 and native census3 are separate, overlapping runs must not be summed. All current new source files have matching metas; no new GUID collision was found; no `Objects.json` delta exists. No NUnit/assert/test-only helper markers appeared in the new runtime/scenario source scan. Exact current file hashes and fixture counts are in `inventory-audit.json`.

Exclude active Unity `Assets/InitTestScene*.unity(.meta)` artifacts and the two tracked `Assets/UnityMCP/Log` logs. Those temporary scenes can change during the running suite; they are not task-owned content.

## Q1–Q4

Q1 symmetry: cached away/return and pre/post-save ownership follow the same existing graph path; no receipt replay or asymmetric refill path added. Q2 consistency: source and restored item/guard snapshots retain actual counts/IDs/links rather than display names; quantities and native-vs-private scopes are explicit. Q3 counter-checks: same actual item is first refused by genuine capacity then accepted; the test proves partial rather than empty cache, nonempty guard gear, actual displacement and graph replacement. Q4 doc-vs-implementation drift: test-only bounded producer seeds and transfers are disclosed; no ordinary journey, global persistence, UI, balance or pixel proof is inferred.

## Later native result

Native job `79b6620947d042e5b89a39f37c32761c` passed50/50, including both new persistence cases and all seven corrected lighting-class cases. These are separate from the original full20633 run, whose one original fixture failure is retained. See `Docs/Verification/SpreadDiscoveryExpeditions/Integration/README.md` for exact counts, scopes and limits. Earlier pending statements above describe the private pre-publication checkpoint.
