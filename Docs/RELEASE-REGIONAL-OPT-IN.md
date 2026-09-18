# Recommended next release milestone: explicit opt-in regional work

Active regional UX slice, 18 September 2026. R2 and fullscreen repair are on local main at 816ed752; R118 confirmed 21/21 expected RED cases without compiler errors. Accepted: R121110/110 focused, R12270/70 native with screenshot review, R12314963/14963 full. Publishing the verified candidate to local main. This starts **after R1 acceptance and R2's truthful outcome receipts have passed their gates**. The live documents still contain earlier handoff checkpoints, so this recommendation does not independently declare either phase integrated.

## Why this is the next smallest complete improvement

**Let the player read a regional offer without accepting it, then explicitly choose to accept.** This is already the next UX option in [RELEASE-REGIONAL-OUTCOMES.md:16](/Users/steven/caves-of-ooo/Docs/RELEASE-REGIONAL-OUTCOMES.md:16), rather than a new feature invented for the roadmap. It serves the optional-commitment and informed-action rules in [RELEASE-VISION.md:27](/Users/steven/caves-of-ooo/Docs/RELEASE-VISION.md:27), the regional-loop phase at line 99, and the game-state recommendation to clarify sources, refusal and aftermath before expanding errands ([GAME-STATE-2026-09-17.md:366](/Users/steven/caves-of-ooo/Docs/GAME-STATE-2026-09-17.md:366)).

The present discrepancy is verified in executable code: the menu says “read the regional request,” but that command generates the source zone and sets `Accepted=true`. Merely asking what someone needs therefore marks the request active. Reading is not currently a neutral preview. Changing only the label to “accept” would make that verb more truthful but would still omit an informed choice before commitment.

This slice improves all five existing bindings without new requests, scenery, rewards, faction systems, or a world director. It should precede material-use guidance or a middle-game chain because those are larger content/readiness tasks. It must not claim that these local booleans already implement the lore's cosmic undertaken/refused/abandoned ledger.

## Verified implementation seams

| Shipped API | Why the slice is bounded |
|---|---|
| [RegionalRequestPart.cs:16–17](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/RegionalRequestPart.cs:16) | Existing instance/recipient identity and persisted `Accepted`/`Completed` fields suffice; a second undertaking-state system is unnecessary. |
| [CanAct and GetInventoryActions, lines 66–93](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/RegionalRequestPart.cs:66) | One owner already supplies read/deliver/release rows. Add an explicit `RegionalRequest:accept` action and keep delivery/release conditional on acceptance. |
| [Current read branch, lines 113–123](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/RegionalRequestPart.cs:113) | Move the current source-resolution, recovery-cargo validation and acceptance transition to `accept`. Make `read` display terms without taking on the work. |
| [InputHandler.cs:2840](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Input/InputHandler.cs:2840) | The native C dispatcher already routes every `RegionalRequest:*` command through `PerformInventoryActionCommand`; no new UI input system is needed. |
| [Transaction snapshots, lines 104–112](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/RegionalRequestPart.cs:104) and [PerformInventoryActionCommand.cs:57–95](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/Inventory/Commands/Actions/PerformInventoryActionCommand.cs:57) | Keep acceptance/notes inside the existing native transaction and publish messages only after commit. Before/after callbacks and rollback already exist. |
| [GetCueState, lines 53–61](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/World/RegionalRequestPart.cs:53) | Existing Available/Active/None states directly express unread/read-but-unaccepted, accepted, and completed/unavailable work. |

These line references describe the current live code inspected for this recommendation. Recheck exact locations after R2 merges; preserve R2's historical receipt formatting rather than restoring the earlier single-format note writer.

## Minimal behavior

- **Read:** show the goods, destination, payment, real alternatives and danger warning, explicitly saying the offer is not accepted when inactive, or already accepted when active. Do not change acceptance, completion, inventory, currency, reputation, or an existing active/released/completed note. For the smallest patch, keep this preview in the existing interaction message presentation; a new remembered-preview state or modal is unnecessary.
- **Do not generate a remote zone to preview it.** Use definitions and already cached observations. An unvisited source is “not yet checked,” not “exhausted.” Reading a known lost recovery offer may explain the loss, but must not advertise a deliverable or regenerate cargo.
- **Accept:** perform the existing authoritative source/cargo check, then set `Accepted`, record the active note and change the cue. Acceptance need not require a prior read. Read remains non-mutating on active work. Repeat accept must not create another instance or reward.
- **Release/completion:** retain R2's durable outcomes and the current finite-instance rules. Release remains reversible only while the actual request is still viable. Completed work cannot be restarted or paid twice. These are local request semantics, not new global moral or cosmic state.

Expected production scope: `RegionalRequestPart`, a small preview formatter beside the R2 note/receipt formatter, and only necessary native harness changes. Retain existing owner, cargo, habitat and payment safeguards. No new per-frame scans, assets, public runtime services, save migrations or request bindings.

## Five acceptance groups, RED before production

1. **Reading does not undertake work.** Parameterize the five actual bindings. Invoke the real gathered C action: terms are visible, `Accepted`/`Completed` stay false, cue remains Available where viable, source-cache count stays unchanged, and goods/currency/reputation/notes are unchanged. Pair with an already accepted request whose reread preserves its active note, and a never-generated source whose preview says availability is unknown rather than exhausted.
2. **Only explicit acceptance enables delivery/release.** Before acceptance, gathered deliver/release actions are absent and direct attempts fail. The native accept action changes the exact instance to Active and creates its valid active note; the same actor's other binding is unchanged. Repeating acceptance cannot duplicate state, source owners, payment or rewards. Ordinary Examine/Chat and zero-turn C policy remain unchanged controls.
3. **Preview cannot bypass physical authority or finite loss.** Known destroyed recovery cargo produces an honest unavailable preview and rejected acceptance, without new cargo. Carrying the exact surviving cargo is the positive control. Retain stale-menu distance, dead/hostile recipient, copied/transplanted Part and wrong-cargo controls through the real command path; the new accept verb must not weaken `Context`.
4. **Transactions and full saves preserve the distinction.** Before/AfterInventoryAction rejection during acceptance restores acceptance, note, currency and emitted receipts. An actual GameSession roundtrip preserves read-only/unaccepted, accepted, released and completed instances independently; no-load path reaccepts a destroyed recovery. R2 completed receipts remain unchanged after reread or rejected repeat delivery, and payout/stock counts remain exact.
5. **Native player acceptance route.** Reuse the ordinary Morrowfast route: C-read Orrit's terms, walk away with no Active cue, return and explicitly accept, inspect the active Q/Tab note, then release/reaccept and deliver through native actions. Capture the R2 settled receipt, save/reload, and prove no second delivery. Assert no synthetic inventory transfer or direct quest mutation; report any later F12 use separately from the already vulnerable opening. Inspect message wrapping at the actual camera/UI size.

Use the existing [RegionalSituationWorldActionTests.cs:62](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Input/RegionalSituationWorldActionTests.cs:62) dispatcher fixture as a seam, not as a reason to weaken coverage: its old assertion that *read accepts* becomes the demonstrated behavior change, while its transaction, reach, UI-return and zero-turn controls remain. Keep existing graph/cargo/adversarial suites and add the new command to them.

Done means a player can learn the terms, deliberately take on the work, release it and later read its truthful outcome. It does not mean the requests are balanced across builds, the middle game is connected, or a closure/ending system has shipped.

## R3 verification sweep and implementation-impact inventory

Extended read-only on 18 September UTC. Runtime/test references below were verified in `/Users/steven/caves-of-ooo`; the same existing read callers also occur in `/tmp/coo-regional-verification-20260917`. The pending R2 fixture is `/tmp/coo-r2-tests/RegionalSituationOutcomeTests.cs`, and its production draft is `/tmp/coo-r2-production.patch`. R2 has not been silently applied by this review. All new R3 material is staged under `/tmp`, outside both projects' Assets.

### Corrections that matter before implementation

| Tempting premise | Verified behavior / required correction |
|---|---|
| Reading already means neutral inspection. | False: `RegionalRequestPart:113–123` hydrates the source, sets `Accepted`, and writes an active note. R3 deliberately changes the meaning of the existing `read` command; this is not only a label edit. |
| Replacing every `"read"` with `"accept"` is safe. | False: several calls test pure reach, gathering a readable row, and lost-cargo viability. Move commitment/viability to accept; keep actual read coverage and pair lost-cargo tests with a truthful non-mutating read. |
| Existing `RecordNote` can format a cold preview unchanged. | False: `RegionalRequestPart:231–254` initializes supply availability to false and proves it only from a cached owner. Calling it for an ungenerated source would falsely say exhausted/unavailable and would persist a note. Preview needs separate formatting with explicit **unknown** versus **observed unavailable**; no new saved field is needed. |
| Any cached source owner proves supply remains available. | False: a harvested `FieldHarvestPart` can remain as stubble, and a damaged/destroyed owner can persist. Reuse the current quantity/yield/HP rules when the cache exists. Cache membership itself is not stock availability. |
| Recovery eligibility can be simplified to a source-cell check. | False: exact cargo can be carried, nested, dropped in another cached chunk, saved and recovered. Preserve the current owner/index and container-lifecycle contracts. R3 must not rescan/generate arbitrary chunks or invent replacements. |
| A native UI route needs a new input branch. | False: `InputHandler:2840–2847` already handles any `RegionalRequest:` command. Add the action through the Part; retain zero turns, outer inventory transaction and menu-return behavior. |
| Completed-note formatting can be reintroduced from the old source. | False: R2 changes completion to a durable actual-outcome receipt and narrows source querying to active notes. Implement R3 on the accepted R2 result, preserving that behavior and all thirteen R2 cases. |
| No migrations means resetting old accepted work is harmless. | False: no migration is needed, but existing scalar state remains authoritative. A saved `Accepted=true` stays active; do not undo it, infer state from text, or synthesize acceptance on load. Old prose need not be rewritten. |

### Exact compatibility contract

| Current native state | Read | Accept | Deliver / release | Cue |
|---|---|---|---|---|
| Unaccepted, viable supply/recovery | Local terms only; no persisted note or source generation | Current authoritative validation, then existing accepted note | Absent/refused | Existing Available |
| Unaccepted recovery known missing, inaccessible, or source replaced | Explain unavailable work without creating cargo | Absent/refused | Absent/refused | Existing None |
| Accepted | Inspect terms without changing the active note | Absent/refused, avoiding a second undertaking | Preserve existing checks and unpaid release | Preserve existing Active/context rules |
| Released, still viable | Preserve the released receipt | Reopen the same instance explicitly; overwrite that instance's released note with its active note | Refused until accepted | Existing Available when viable |
| Completed | No new request action; inspect historical Q/Tab receipt | Refused | Refused, no second payment | None |
| Dead/hostile/detached recipient, wrong player/instance/graph or out of reach | Refused | Refused | Existing refusal | Existing context rule |

The unavailable preview row is intentional: the player can ask a living nearby recipient what happened even when there is no available-work cue. This does not expose actions through a dead or invalid recipient. If the source cache does not exist, preview says **not checked/unknown**; it must not promise a local vein, ripe row, or available parcel. A known invalid source address can instead be described as unavailable using current map authority, without hydration. A live exact carried consignment can remain acceptable under the existing recovery rules; it does not prove local source stock exists.

No read means refusal, abandonment or undertaking. No new obligation ledger, cooldown, reward, source replacement or migration is included. Reading may emit its explicit terms through MessageLog and a diagnostic only after the native outer transaction commits; those are observations, not saved work state. Acceptance continues to be zero-turn like the existing request verbs. Source cache hydration on a failed acceptance is not claimed to be reversible; acceptance/note/payment mutation and success receipts **are** transactional.

### Existing fixtures requiring intentional updates

Paths in this table are relative to the live project root. Line numbers are the verified pre-R3 versions. This is a semantic inventory, not permission for broad string replacement.

| Fixture | Exact methods / call locations | R3 change and preserved evidence |
|---|---|---|
| `Assets/Tests/EditMode/Gameplay/Storylets/RegionalSituationTests.cs` | `BroughtSupplyEntersRealStockAndPaysOnlyOnce` 162; `IncompleteSupplyPreservesEveryUnitAndDoesNotReward` 186; `FullRecipientInventoryLeavesPaymentRequestAndRewardUnchanged` 205; `ExactRecoveredCargoDeliversRealStockWhereOrdinaryGoodsDoNot` 227; `SeparateIronBindingsRemainIndependentlyPlayable` 255,257 | Change only setup commitment to `accept`. Keep all exact ownership, quantities, stock, reward, currency and independent-instance assertions. |
| Same fixture | `NativeReadActionRecordsUsefulNotesAndReleaseDoesNotPayOrDeleteSource` 302–308 | Rename around explicit acceptance; still gather the read row, additionally gather/execute accept to create the note. Replace repeated commitment setup with a non-mutating reread or rejected repeat accept. Keep release/source-preservation assertions. |
| Same fixture | `ReplacedSourcePoiCannotAdvertiseOrInstallRecovery` 325 | The forbidden verb becomes **accept**; retain absent cue and no installed source. Add/retain a readable unavailable explanation while the recipient context remains legitimate. |
| `Assets/Tests/EditMode/Gameplay/Storylets/RegionalSituationAdversarialTests.cs` | `AuthorityRefusalCannotConsumeSupplyAndRestoredAuthorityWorks` 64; `InvalidSupplyOwnershipAndQuantityCannotComplete` 92; `CurrencyOverflowRollsBackExactSplitAndDestinationMerge` 106; `ReceiverCapacityRefusalRestoresSupplyAndAllowsRetry` 118; `SplitCloneFailureRestoresSourceAndReleasesRequestClaim` 128; `NativeActionPostCallbackFailureIsAtomic` 143; `WalletObserverCannotReenterWithASecondEligibleStack` 154; `CompletedNotesSurviveEntitySerializationWithoutDuplicatingEntries` 174; `RecoveryRequiresExactCargoWithoutErasingUnrelatedContents` 187; `RemovingEveryHabitatOwnerCannotVacuouslyEarnPreservation` 230; `FullWorldReloadRetainsCarriedCargoAndCompletionWithoutRegeneration` 245; `RecoverySourceReplacedAfterAcceptanceStillAllowsUnpaidRelease` 320 | Setup becomes accept, with delivery/rollback/authority/save controls retained exactly. Do not delete callbacks, owner faults, habitat counterexamples or repeat-delivery controls because new preview is pure. |
| Same fixture | `KnownDestroyedRecoveryCannotKeepAdvertisingAvailableWork` 316 | Test viable/dead cargo against accept, not read; explicitly keep None/Available cue and source identity controls. |
| `Assets/Tests/EditMode/Gameplay/Storylets/RegionalSituationUsabilityTests.cs` | `OneGrainExplainsTwoUnitRequirementAndSecondUnitThenWorks` 41; `WrongCargoExplainsSealedConsignmentAndActualSource` 58; `RemovedSupplySourceGetsTruthfulNoteAndBroughtGoodsStillComplete` 77; `NotesNameNativeGoodsActualRecipientAndDestinationTown` 94; `PostActionFailureCannotPublishSuccessfulDeliveryReceipt` 113 | Use accept to establish active work. Preserve missing-unit, exact sealed-cargo directions, truthful exhausted supply, human-readable labels and deferred receipt assertions. Read-only preview gets separate new tests rather than weakening these active-note requirements. |
| `Assets/Tests/EditMode/Gameplay/Storylets/RegionalSituationPresentationTests.cs` | `DistantAvailableCueDoesNotGenerateSourceAndBecomesActiveAfterReading` 36; `RealWorldActionMenuOffersRequestWithoutSyntheticZoneParameter` 60,62; `JournalContainsPersistentRequestAndExistingDirectionsWithoutOverwritingEither` 69,82 | Rename first test for explicit acceptance and assert reading leaves Available. Gather both read/accept before commitment. Only accept creates the persistent request note; retain the distant read refusal and travel-note coexistence. |
| `Assets/Tests/EditMode/Gameplay/Storylets/RegionalSituationCargoLifecycleTests.cs` | `RestoredDroppedAwayCargoCanBeRecoveredWithoutStaleCueOrReplacement` 74,81,83 | Change viability and Active-transition calls to accept. Retain actual pickup/drop, third-zone save, index recovery and removed-owner countercheck. Read can explain the unavailable situation without restoring an Available cue or creating cargo. |
| `Assets/Tests/EditMode/Gameplay/Storylets/RegionalSituationContainerLifecycleTests.cs` | `RestoredContainerCargoRecoversItsCueAfterNativeRetrieval` 73,79,80,92,94 | Same for nested cargo: `CanAct/TryAct(...,"accept")` proves eligibility, native Take proves recovery; pure read must not retrieve or recreate the nested owner. |
| `Assets/Tests/EditMode/Gameplay/Storylets/RegionalSituationCargoRollbackTests.cs` | `FailedNativeTakeAfterTakenKeepsCueMissingAndRetryRecoversSameOwner` 74,85,93,95 | Preserve precommit ownership/index rollback; test accept viability. Do not relax the failing native Take's None cue. |
| `Assets/Tests/EditMode/Presentation/Input/RegionalSituationWorldActionTests.cs` | `NativeWorldMenuReadsAndReleasesRequestWithoutChargingGenericActionTurn` 67; `NativeWorldMenuDeliveryCommitsOnlyAfterSuccessfulNativePostAction` 83; `StaleRequestMenuCannotBypassTheWorldReachGate` 114 | Exercise real menu **read → accept → release**, with read unaccepted/no note, then accept Active. Callback expectation becomes three successful actions instead of two; tick count stays unchanged. Delivery setup uses accept. Parameterize stale-menu coverage for read **and** accept, leaving ordinary Examine unchanged. |
| Pending R2 `RegionalSituationOutcomeTests.cs` (currently `/tmp/coo-r2-tests/`) | `CompletedSupplyReplacesInstructionsWithActualOutcome` 50; `RecoveryReceiptRecordsActualBankOutcomeAndPaidAmount` 62; `ReleaseReplacesActiveInstructionsWithoutPretendingPermanentRefusal` 82,89; `NativePostActionRollbackRestoresPreviousNoteWhileSuccessReplacesIt` 94; `FullSessionReloadKeepsReceiptAfterStockLeavesAndForbidsSecondPayment` 106; `CurrencyCommitFailureCannotPublishAnUnpaidReceipt` 120; `ReloadedJournalRendersDistinctOutcomesWithoutRecipientsOrGeneratingMissingSources` 130,133 | All setup/reaccept reads become accept. Especially line 89 must explicitly accept before expecting the active note to return; an intervening read must preserve the released receipt. All thirteen parameterized R2 outcome cases and real rendered journal assertions remain required. |

There are **eight current fixtures plus the pending R2 fixture** with this dependency. Searches covered all current `Assets` source callers and literal/action-label references across the project, excluding generated caches and historical verification archives. Existing fixtures with no read-as-accept assumption need no wording change; still run the full regional filter. This is not a claim that all these tests have been executed under R3.

### Native scenario, runners, and documentation

- `Assets/Scripts/Scenarios/Custom/ChunkGameplayNativeAudit.cs`, `InspectRegionalIron`, lines 235–238: currently C-read directly precedes `regional_native_read` asserting Accepted and one note, then `regional-request` capture. Split into successful read with unaccepted/no note/no source cache and explicit C-accept with Active/one note. Add an actual preview capture and retain the active capture. A short walk away/back should prove inspection did not commit; release/reaccept is useful only if done through actual C actions. Preserve root's R2 trade-and-reopened-receipt steps, ordinary vulnerable opening, all five interiors, protected extraction and explicitly labeled later F12 use.
- `Assets/Editor/Scenarios/ChunkGameplayNativeAuditBatch.cs:206`: replace the old misleading acceptance meaning of `regional_native_read` with separate required `regional_native_preview` and `regional_native_accept` gates (or retain read only if it now asserts preview and add accept). Require both explicitly; do not merely increase a total-check threshold.
- `Tools/ChunkGameplay/run_native.py:37`: no read/accept dispatch or required-gate list exists here; update its run-scope/capture/step description after the actual new route is known. Do not claim the old seventeen-frame count for a newly expanded route. It still must reject exceptions and require the batch result.
- `Docs/MORROWFAST-STYLE-AND-REGIONAL-SITUATIONS.md:51,119`: current mechanics/how-to text must say C-read terms, **accept**, deliver/release. Add an R3 implementation/gate entry; preserve chronological RS receipts and historical check counts.
- `Docs/GAME-STATE-2026-09-17.md:183` and `Docs/RELEASE-AGENT-PROMPT.md:37`: append a current release checkpoint or update clearly current handoff language to distinguish read from acceptance. Keep the dated snapshot's historical claims identifiable.
- `Docs/RELEASE-REGIONAL-OUTCOMES.md:9,16,46`: lines 9/16 are the verified R2 premise/considered-option history and should remain dated; append R3 adoption/status rather than erasing what was observed. Its still-current native route at line 46 needs explicit acceptance before active notes, or a clear link to its superseding R3 route.
- `Docs/CHUNK-GAMEPLAY-NATIVE-AUDIT.md` currently describes the earlier discovery/cloth/directions route, not a direct read-as-accept instruction. Link the expanded regional route and limits when updating native acceptance, without presenting Vennit's different “remember directions” verb as request acceptance.
- `Docs/RELEASE-R1-ACCEPTANCE.md:19–21`, `Docs/RELEASE-STABILIZATION.md`, `Docs/RELEASE-VISION-EVIDENCE.md:24`, saved screenshots, JSON/XML receipts and archived native review reports are **historical evidence**, not active callers. Preserve those results unchanged and append/link new R3 evidence. No retroactive relabeling of the R104/RS27 capture is warranted.
- `Docs/RELEASE-VISION.md:27,87–99` supplies the design rationale and does not prescribe the old command semantics; no behavioral correction is required there. The R3 living doc should link it and disclose this as CoO-original UX, not claimed Qud parity.

### RED fixture: nine flows, twenty-one cases (expanded during pre-implementation review)

`/tmp/coo-r3-tests/RegionalSituationOptInTests.cs` and its copied unique `.meta` are ready for a future root-controlled import into `Assets/Tests/EditMode/Gameplay/Storylets/`. The fixture references only existing public APIs and string commands; the new `accept` behavior should fail as assertions rather than require a missing production type. **No Unity run or actual RED is claimed yet.**

1. Five bindings: reading a cold offer leaves state, inventory/stock, currency, reputation, player properties, notes and source-cache count unchanged; shows terms and explicitly unknown availability.
2. Original-town and recovery recipient: real gathered rows expose read/accept, not deliver/release; acceptance creates Active/one note; repeated accept rejects and active reread does not rewrite history.
3. Present/missing actual cargo: both offers can be read; only surviving work can be accepted; no replacement owner appears.
4. Read/accept stale command: moving away prevents the previously gathered native command; returning restores use.
5. Supply/recovery release: read preserves the released receipt; explicit reaccept returns the same instance to Active without money/goods changing.
6. Supply in the player's real inventory: reading cannot deliver or pay; explicit acceptance enables one exact transfer/reward and no second payment.
7. Before-veto/after-exception/success: native callback failure rolls back commitment/note and cannot announce success; retry proves the claim was released. Source cache is deliberately prewarmed because cache hydration is not a transactional promise.
8. Full `GameSessionState` graph: four distinct instances remain preview-only, active, released and completed after actual serialization; the preview's source remains cold and completed work cannot repay.
9. Two supply types: cold availability is unknown, physically removed finite source is disclosed as unavailable, and legitimate bought/carried supply can still be accepted without replenishing it.

No reflection, direct `Accepted` assignments, synthetic quest-event shortcuts or manually installed request Parts are used. Test setup creates a normal detached native world and supplies test inventory where explicitly stated. The new fixture does not replace native C/zero-turn UI coverage or the existing copied-owner/hostility/death/cargo transaction suites.

### Verification sequence and scope stop

After R2 is accepted: import the staged fixture in the independent clone, capture actual assertion RED, implement only the agreed preview/accept behavior, update the exact existing dependencies above, then run the combined regional/outcome/world-action tests. Follow with the full suite, native C-read/C-accept/Q/F5/F6 route, visual review of term and receipt wrapping, and four-question cold-eye review. Keep the live editor open throughout isolated work. No new models or fresh imported art are necessary.

The staged tests are source-reviewed only. The copied `.meta` changes only its GUID and was checked against all current live Assets metadata without collisions. Root must recheck metadata after installation and record actual RED/GREEN counts. The recommendation is ready; production and documentation publication await root's R2/R3 sequence.

## Root verification sweep before implementation (18 September)

| Assumption | Verified correction / decision |
|---|---|
| Reading is already neutral | `RegionalRequestPart` sets Accepted and generates source in the read branch; move that branch to explicit accept. |
| Every missing source is exhausted | `CachedZones` distinguishes a cold source from known absence. Use nullable availability in preview; never call GetZone when reading. |
| Recovery with lost cargo should hide every verb | Keep a local explanatory read action, but gate accept and the available cue on the real consignment. |
| Active reread should rewrite a note | Reading becomes nonmutating. Preserve the acceptance snapshot and historical released/completed receipt; preview gives current observed availability. |
| Supply can only be completed from the marked source | Bought/carried goods already work; accept remains possible when the finite local source is gone. |
| Accept needs a new input route | Native C dispatcher handles the RegionalRequest prefix already. Add one owner-bound action, no new input state or save field. |

Source reading confirmed all five definitions, native transaction snapshots, source-owner authority, active/released/completed note formatting, the actual C input dispatcher and current world-action tests. The original note-writing fixture should become explicit acceptance plus an unchanged-note reread. Existing lost-cargo tests should test refusal of acceptance while retaining honest explanation. The completed R2 outcome and rollback assertions remain authoritative.

## R118 RED and initial implementation

R118 compiled without errors and all 21 new opt-in cases failed against the prior behavior: implicit read acceptance, missing explicit accept command, hidden known-loss explanation and stale source availability. One stale-accept case failed while gathering the absent new action; the others failed their behavioral preconditions/assertions. R119 tests the minimal implementation before existing commitment fixtures are updated.

The two production files now separate read from accept, reuse the original authoritative acceptance branch and transaction, add the explicit menu row, and build ephemeral terms from nullable cached observations. Shared terms name actual goods/count, recipient, return place, reward and existing hazards. Existing completion receipts are untouched. Active and released notes remain saved snapshots on reread. No new field, migration, quest, reward or ongoing query.

## R119 and review-driven regression

Initial new fixture GREEN: 21/21, zero compiler errors. Independent cold-eye found a real semantic mismatch: the known-lost recovery preview correctly described unavailable cargo but still advertised the absent Accept action. Added a negative preview-instruction assertion with cached-live and unvisited positive controls before fixing it. R120 runs that expected RED plus the existing regional suite.

Existing fixtures were classified by purpose before edits: commitment setup and recovery-viability checks now use accept; neutral rereads, gathered read actions and distant-read refusal remain. The old native world-action fixture now exercises read, accept and release through the real dispatcher (three callback pairs, zero turns), with a second stale-accept reach case. All R2 outcomes, habitat, currency-overflow, exact-owner, callback and full-save assertions remain.

## R121 GREEN, R122 native and four-question cold-eye review

R120 reproduced only the new missing-cargo wording regression: 109 pass, 1 expected fail. The suffix now offers inquiry when the original cargo becomes available, rather than an absent action. R121: **110/110**, zero compiler errors. This includes all 35 existing regional adversarial cases, unchanged R2 transaction/outcome assertions, the 21-case new opt-in matrix and native C dispatcher cases.

R122: **70/70 native checks**, 925 queued movement/input steps, 23 real 1920x1080 captures, zero compiler errors/unhandled exceptions, frozen source and successful private-save/settings cleanup. The native route reads without acceptance or source generation, walks away, explicitly accepts, releases, reads without erasing the released note, reaccepts and renders the active journal. It then retains the same protected-extraction law, exact delivery/payment/trade stock, clean fullscreen menus and F5/F6 historical receipt proof. Root inspected the actual preview and active-note screenshots: terms, unknown source, explicit opt-in and accepted state are visible. The preview remains the existing gameplay log; no new modal was introduced.

- Symmetry: read never toggles acceptance or rewrites notes; explicit accept owns initial/reaccepted transition, release owns the reverse, completion owns its historical receipt.
- Cross-feature consistency: all five real bindings use the same verbs and real item labels; recovery still requires exact cargo, supply still permits outside goods.
- Counterchecks: cold/live/removed/harvested source, inactive/active/released/completed instances, valid/distant actor, before-veto/after-throw/commit, original/counterfeit cargo and repeat delivery covered through new and retained fixtures.
- Docs versus code: no claim of permanent lore refusal/closure, new save fields or transactional rollback of first-time source cache hydration. Existing notes remain interaction snapshots; new preview is current cached observation only.

🟡 Fixed: reading silently accepted work; known unavailable recovery advertised nonexistent Accept.
🔵 Improved: physical source status is unknown until cached, not falsely exhausted; all recovery previews now name quantity and actual contents as well as the sealed-cargo requirement.
🧪 Bounded: native preview screenshot covers Orrit's iron offer; all five text/identity branches are exercised in EditMode. Native combat balance, all accessibility settings and every reward use remain unclaimed.
⚪ Deferred: longer-term faction/campaign/closure arcs remain the release roadmap.

Independent final source review found no remaining material issue. Full R123 passed **14,963/14,963**, zero compiler errors/failures/skips/inconclusive; 348.71 seconds. Candidate manifest lists 16 source/test/tool files. GUID audit: 6,774 unique clone metadata GUIDs, zero collisions.

The R122 restored historical-receipt screenshot was also inspected after F5/F6: actual payment, trade-stock aftermath and no-repeat wording remain visible. The source hash manifest and the archived native report identify the exact accepted candidate; no stale prior-run screenshot is used as proof.

## Publication

The exact 16-file candidate and its evidence are committed with this living document and promoted to local main. No remote push, legacy migration or release certification. User Unity stayed open; execution used only the independent clone. Next bounded slice: material-use descriptions, Docs/RELEASE-MATERIAL-GUIDANCE-PLAN.md.
