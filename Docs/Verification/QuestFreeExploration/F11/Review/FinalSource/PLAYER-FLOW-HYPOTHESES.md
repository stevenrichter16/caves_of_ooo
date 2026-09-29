# F11 final player-flow hypothesis audit

This is a read-only map of twelve player-flow hypotheses to existing executed paired tests. It adds no redundant test cases and does not claim twelve new RED tests. Earlier confirmed defects and already-correct controls remain distinguished. Exact case names, native result rows, source lines and hashes are in `player-flow-hypotheses.json`.

## 1. A player enters ordinary fallow countryside: can the encounter add bonus actors or claim a late matching substitute?

**Classification:** Cross-system coverage, already executed.

One complete original hostile roll and one exact original Magpie fund the pair; a missing source/blueprint refuses the entire replacement. The intentional loss of that Magpie’s later trader stock is documented.

- `SpreadHuntLayoutTests.WholeOrdinaryGroupAndOneExactMagpieBecomeOneBoundedPair` — 8 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntLayoutTests.cs:40`.
- `SpreadHuntLayoutTests.MissingOptionalActorBlueprintLeavesEveryOriginalOwnerAndReceiptUntouched` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntLayoutTests.cs:44`.
- `SpreadHuntAdversarialTests.MatchingReplacementMagpieCannotStandInForTheExactOriginalRoll` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntAdversarialTests.cs:42`.
- `SpreadHuntPipelineTests.HuntComposerRunsAfterOriginalPopulationAndBeforeMagpieStock` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntPipelineTests.cs:48`.
- `SpreadHuntPipelineTests.FixedThreeSeedVersionEightActualSourcesMeetBothVariantReleaseFloor` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntPipelineTests.cs:50`.

## 2. The prey disappears behind a real tree: does the hunter still know its hidden current coordinates?

**Classification:** Pinned finite/visibility contracts, already executed.

A remembered route is invariant under hidden quarry relocation and full saved replacement; real opaque cover changes the interleaved result. Both covered and open controls surviving is honest, not a promised winner.

- `SpreadPredatorStage1Tests.HiddenPositionsCannotChangeSearchEvenAfterReplacementSave` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:32`.
- `SpreadPredatorStage1Tests.NeverSeenPreyDoesNotRouteToDefaultCoordinate` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:45`.
- `SpreadPredatorStage1Tests.OneRealTreeChangesInterleavedTerminationWithoutGuaranteeingDeath` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:51`.
- `SpreadPredatorStage1Tests.HealthyPairedPreyUsesOpenSideExitWhenCardinalFlightBlocked` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:36`.

## 3. A nearby death shakes the grazer and its hunter returns: will effect-owned pacing ignore visible danger?

**Classification:** Confirmed stage3 behavior defect; retained RED→GREEN.

Only this exact nearby visible pair can substitute one real flight step. Witnessed and its duration owner remain, tick normally and remove their own goals. Far/hidden/unpaired/spent controls keep ordinary pacing.

- `SpreadHuntLiveGapTests.GenuineNearbyHunterInterruptsOnlyPacingStepAndPreservesItsOwner` — 3 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntLiveGapTests.cs:34`.
- `SpreadHuntLiveGapTests.NoCurrentVisibleNearPairLeavesOrdinaryPacingAuthoritative` — 4 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntLiveGapTests.cs:37`.
- `SpreadHuntLiveGapTests.ShakenPacingFlightSpendsOneScheduledActionAndEffectStillTicks` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntLiveGapTests.cs:52`.
- `SpreadHuntLiveGapTests.WitnessedEffectStillRemovesItsOwnPacingGoalAfterFlight` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntLiveGapTests.cs:63`.

## 4. The player interrupts, recruits, calms or gives work: can local hunting overrule combat or spend a second action?

**Classification:** Priority/accounting coverage, already executed.

Ordinary current threat, party/follow, Calm, conversation and explicit work remain authoritative; a successful local action does not push a second attacking child.

- `SpreadPredatorStage1Tests.HigherPriorityHunterStateDoesNotSpendHuntBudget` — 4 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:39`.
- `SpreadPredatorStage1Tests.ImmediateOrdinaryHostileTakesPriorityOverAdjacentPrey` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:40`.
- `SpreadPredatorStage1Tests.AdjacentQuarryReceivesOneNativeAttemptWithoutChildGoal` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:42`.
- `SpreadHuntLiveGapTests.HigherAuthorityStillOwnsPacingChildEvenWithHunterNear` — 6 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntLiveGapTests.cs:40`.
- `SpreadPredatorFeedingTests.HigherPriorityStatePreemptsMeal` — 5 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:38`.

## 5. A real hunt kills the grazer: can feeding synthesize a body, extra meat or a second ordinary loot roll?

**Classification:** Native combat/corpse/loot coverage, already executed.

The actual own-kill receipt is used only when the unchanged 70% roll creates its real body. Both sides of 69/70 and independent ordinary Beast death drops are tested; feeding creates no reward.

- `SpreadPredatorFeedingTests.OwnNativeKillBindsOnlySuccessfulSeventyPercentCorpse` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:30`.
- `SpreadPredatorFeedingTests.OrdinaryBeastLootRemainsIndependentAndIsNotConsumedOrRerolled` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:42`.
- `SpreadPredatorFeedingTests.ExternalDeathNeverClaimsMealEvenWhenKillerIDMatches` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:36`.
- `SpreadPredatorFeedingTests.TwoPaidProgressActionsThenThirdConsumesExactOwnerWithoutRewardOrExtraPose` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:31`.

## 6. The player takes or moves a feeding corpse, or a callback replaces it: can a matching decoy be consumed?

**Classification:** Provenance/transaction coverage, already executed.

Moving, carrying, transferring, changing source/killer IDs and replacement each invalidate the exact claim. A copied provenance decoy is not the fresh native creation receipt.

- `SpreadPredatorFeedingTests.InvalidMealCancelsWithoutConsumingMatchingDecoy` — 7 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:34`.
- `SpreadPredatorFeedingTests.DeathCallbackReplacementWithCopiedProvenanceIsNotOriginalReceipt` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:35`.
- `SpreadPredatorFeedingTests.ProgressCallbackRemovalPreventsAnyLaterConsumptionOrReplacementClaim` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:40`.

## 7. The player leaves, saves and returns during pursuit or feeding: can budgets refill, removed owners reappear or aliases split?

**Classification:** Cross-system saved graph coverage, already executed.

Actual generated pair state/absence survives cache and full reload; staged feeding at each progress value preserves the exact corpse and finite claim. These are fixtures, not substitutes for native keyboard save evidence.

- `SpreadHuntSaveTests.ActualGeneratedPairStateOrAbsenceSurvivesCacheReturnAndFullGraphReload` — 5 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntSaveTests.cs:11`.
- `SpreadPredatorFeedingTests.ReplacementSavePreservesFiniteProgressAndExactCorpse` — 4 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:32`.
- `SpreadPredatorStage1Tests.TerminalReciprocalStateRemainsSpentAcrossSave` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:46`.
- `SpreadHuntLiveGapTests.ReplacementSaveKeepsPacingOwnerAndExactVisiblePair` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntLiveGapTests.cs:54`.

## 8. A player examines a paused hunter or watches feeding: can the text or animation claim an action that did not happen?

**Classification:** Presentation/commit ordering coverage, already executed.

Two paid progress actions emit two valid current-owner Interact gestures; consumption emits none after the target leaves. Higher-priority pauses suppress active feeding prose. Inspection changes no state and promises no Harvest.

- `SpreadPredatorFeedingTests.TwoPaidProgressActionsThenThirdConsumesExactOwnerWithoutRewardOrExtraPose` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:31`.
- `SpreadPredatorFeedingTests.LookDoesNotClaimActiveFeedingWhenHigherBehaviorOwnsActor` — 4 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:41`.
- `SpreadPredatorFeedingTests.WorldLookReportsActualLocalPhaseWithoutMutatingOrPromisingHarvest` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorFeedingTests.cs:49`.
- `SpreadHuntLiveGapTests.DisabledDiagnosticsDoNotChangeHuntOrCreateRecords` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntLiveGapTests.cs:69`.

## 9. A visible hunter is hidden, transferred, killed, picked up or reconstructed: can it borrow a stale model?

**Classification:** Actual imported art/current-owner coverage, already executed.

Current-owner and source guards govern the imported body/remains; ongoing hunt configuration does not gate healthy body appearance. Pickup/drop removes/restores the actual ground owner. Existing animal forms remain unchanged.

- `FurrowstalkerArtTests.CurrentHunterMovementVisibilityRemovalAndFullSavedReplacementKeepOnlyTheActualOwner` — 1 native Passed case(s); `Assets/Tests/EditMode/Presentation/Rendering/FurrowstalkerArtTests.cs:79`.
- `FurrowstalkerArtTests.ExactSourceRemainsUseTheirOwnStaticBodyAndNativePickupDropRemovesThenRestoresOnlyThatView` — 1 native Passed case(s); `Assets/Tests/EditMode/Presentation/Rendering/FurrowstalkerArtTests.cs:91`.
- `FurrowstalkerArtTests.StaleSpatialBacklinksCannotBorrowTheStillRegisteredHuntersApprovedAppearance` — 1 native Passed case(s); `Assets/Tests/EditMode/Presentation/Rendering/FurrowstalkerArtTests.cs:121`.
- `FurrowstalkerArtTests.ExistingLivingAnimalModelsRemainTheirOriginalApprovedBodies` — 2 native Passed case(s); `Assets/Tests/EditMode/Presentation/Rendering/FurrowstalkerArtTests.cs:137`.

## 10. A generation callback or later stock/placement step changes an actor or route: can rollback erase that change or leave half a pair?

**Classification:** Confirmed callback hardening plus cross-system controls, already executed.

Snapshot proofs revalidate all already-created owners after each authority callback. Rollback restores only unchanged owned graphs; foreign movement/blockers survive. Later stock of other Magpies is allowed, later blockers of accepted anchors are rejected.

- `SpreadHuntAdversarialTests.SecondOwnerAuthorizationCannotChangeEarlierPlacedHunter` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntAdversarialTests.cs:30`.
- `SpreadHuntAdversarialTests.IndependentlyMovedOriginalOwnerIsNotClobberedDuringPartialRollback` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntAdversarialTests.cs:20`.
- `SpreadHuntAdversarialTests.ChangedDetachedSourceGraphIsNotReclaimedByRollback` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntAdversarialTests.cs:22`.
- `SpreadHuntAdversarialTests.LaterOrdinaryStockForOtherMagpiesDoesNotInvalidateAcceptedPair` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntAdversarialTests.cs:50`.
- `SpreadHuntAdversarialTests.LaterBlockerOnTheAcceptedPairAnchorInvalidatesColdCommitment` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntAdversarialTests.cs:52`.
- `SpreadHuntDetachedReceiptTests.RemovedActorProofStillPinsActualCarriedEquipmentState` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/World/SpreadHuntDetachedReceiptTests.cs:18`.

## 11. The exact prey is removed, recruited, dead or has a foreign rebound role: can pursuit retarget a lookalike or corrupt another actor?

**Classification:** Lifecycle/authority coverage, already executed.

Cold unbound brains may configure, foreign zones may not; live actions require exact current owners. Invalidated pairs never retarget a matching grazer, and terminal cleanup modifies only the owned reciprocal part.

- `SpreadPredatorStage1Tests.AdmissionAcceptsUnwiredColdBrainsButRefusesForeignZone` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:50`.
- `SpreadPredatorStage1Tests.InvalidatedExactPairDoesNotAttackOrRetargetMatchingDecoy` — 4 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:44`.
- `SpreadPredatorStage1Tests.AbortDoesNotMutateForeignGrazerPartBacklink` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:66`.
- `SpreadPredatorStage1Tests.InvalidSavedFlightMemoryIsSpentWithoutMotion` — 5 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadPredatorStage1Tests.cs:67`.

## 12. Two grazers flee repeatedly, including a movement callback or saved reload: can retained search memory alter decisions or create recurring turn-path garbage?

**Classification:** Confirmed stage4/5 allocation correction plus semantic controls.

Per-owner value-only buffers clear before movement callbacks. Traversal/ties, hidden-history invariance, changed blockers, two-owner isolation and save reconstruction remain equal. Actual calibrated native event counts fall to one for both measured paths; all defined phases remain accepted and malformed values rejected.

- `SpreadHuntAllocationTests.WarmedFlightStepAvoidsRecurringSearchAllocations` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntAllocationTests.cs:123`.
- `SpreadHuntAllocationTests.ReusedSearchPreservesRouteAndRespondsToChangedBlocker` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntAllocationTests.cs:141`.
- `SpreadHuntAllocationTests.HiddenHunterPositionDoesNotChangeRememberedFlightAfterWarmOrSave` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntAllocationTests.cs:149`.
- `SpreadHuntAllocationTests.TwoGrazersKeepIndependentSearchAndState` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntAllocationTests.cs:163`.
- `SpreadHuntAllocationTests.MovementCallbackReentryMatchesSequentialSearch` — 2 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntAllocationTests.cs:188`.
- `SpreadHuntPhaseValidationTests.SavedBoundsRecognizeEveryDefinedPhaseAndRejectMalformedValues` — 13 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntPhaseValidationTests.cs:31`.
- `SpreadHuntPhaseValidationTests.RepeatedBoundsValidationDoesNotAllocateOrBoxItsPhase` — 1 native Passed case(s); `Assets/Tests/EditMode/Gameplay/AI/SpreadHuntPhaseValidationTests.cs:53`.

## Evidence bounds

The core receipt remains 436 Passed / 1 allocation failure out of 437; only the unchanged semantic controls are cited from it. The failed pacing allocation is not relabelled. Stage5 independently supplies a genuine native 26 Passed / 2 Failed RED and 28/28 GREEN; the final calibrated measurements are flight 1 event, pacing 1 event and phase validation 0 events, with empty/single/five controls 0/1/5. These are current-thread allocation event counts, not bytes or whole-frame performance.

The art receipt contains 132 raw results: 130 Passed plus two temporary diagnostic Inconclusive probes. Only the actual passing art methods are mapped. This review does not turn source-model tests into pixel/feel acceptance, synthetic fixture state into naturally occurring play, or private patched-hash trajectories into Unity generation. Root-owned native input/save/return reports are separate acceptance evidence.
