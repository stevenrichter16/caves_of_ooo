# Throwing staging inventory

Snapshot UTC: 2026-10-16T06:41:06+00:00. HEAD: `4fc2c0bbd55193bc68958518b72beefe856370be`.

This is a file-list receipt, not a staging operation or acceptance claim. Final comparison reruns and reviews can update evidence after the snapshot. Historical unrelated untracked artifacts are excluded. No Assets files or Git index entries were changed while preparing it.

`staging-paths.txt` contains the exact repo-relative paths, one per line, without wildcard expansion. Review the final diffs before staging this list. It includes this inventory and itself.

## Modified tracked files (42)

```text
Assets/Scripts/Gameplay/AI/ItemLandedEvent.cs
Assets/Scripts/Gameplay/AI/TerrainNavigationWeight.cs
Assets/Scripts/Gameplay/Abilities/ActivatedAbilitiesPart.cs
Assets/Scripts/Gameplay/Combat/PhysicsPart.cs
Assets/Scripts/Gameplay/Effects/AsciiFxBus.cs
Assets/Scripts/Gameplay/Entities/ExaminablePart.cs
Assets/Scripts/Gameplay/Inventory/Commands/Acquisition/PickupCommand.cs
Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs
Assets/Scripts/Gameplay/Inventory/Commands/Equipment/UnequipCommand.cs
Assets/Scripts/Gameplay/Inventory/InventoryCommandExecutor.cs
Assets/Scripts/Gameplay/Inventory/InventoryCommandResult.cs
Assets/Scripts/Gameplay/Inventory/InventoryPart.cs
Assets/Scripts/Gameplay/Inventory/InventoryTransaction.cs
Assets/Scripts/Gameplay/Items/LiquidSourcePhase.cs
Assets/Scripts/Gameplay/Items/LiquidVesselService.cs
Assets/Scripts/Gameplay/Items/StackerPart.cs
Assets/Scripts/Gameplay/Items/TonicExamineService.cs
Assets/Scripts/Gameplay/Look/CellStatusReadout.cs
Assets/Scripts/Gameplay/Look/WorldCursorState.cs
Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs
Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs
Assets/Scripts/Gameplay/Save/SaveSystem.cs
Assets/Scripts/Gameplay/Save/SessionTileStateSerializer.cs
Assets/Scripts/Gameplay/Skills/SkillsPart.cs
Assets/Scripts/Gameplay/Turns/MovementSystem.cs
Assets/Scripts/Gameplay/Turns/TurnManager.cs
Assets/Scripts/Gameplay/World/Map/TileReactionSystem.cs
Assets/Scripts/Gameplay/World/Map/Zone.cs
Assets/Scripts/Gameplay/World/Map/ZoneTileState.cs
Assets/Scripts/Gameplay/World/Map/ZoneTileStateSystem.cs
Assets/Scripts/Presentation/Input/InputHandler.ControllerHints.cs
Assets/Scripts/Presentation/Input/InputHandler.cs
Assets/Scripts/Presentation/Input/NativeGamepadInput.cs
Assets/Scripts/Presentation/Rendering/AsciiFxRenderer.cs
Assets/Scripts/Presentation/Rendering/SpreadParticleFrame.cs
Assets/Scripts/Presentation/Rendering/WorldCursorRenderer.cs
Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.cs
Assets/Tests/EditMode/Gameplay/AI/DensityPetRetrievalAdversarialTests.cs
Assets/Tests/EditMode/Gameplay/Items/ThrowableTonicTests.cs
Assets/Tests/EditMode/Gameplay/World/ConnectedReserveGroundThrowBoundaryReviewTests.cs
Assets/Tests/EditMode/Presentation/Rendering/SpreadParticleRenderingTests.cs
Assets/Tests/EditMode/Presentation/Rendering/WorldCursorRendererTests.cs
```

## New Unity assets (28 sources plus 28 metadata files)

All listed metadata exists, has `fileFormatVersion: 2` and a lowercase 32-hex GUID, and has no GUID collision anywhere under Assets. Every listed meta has its source; Throwing.json parses successfully.

| Source/content/test | Metadata | GUID |
| --- | --- | --- |
| `Assets/Editor/Scenarios/ThrowingNativeBatch.cs` | `Assets/Editor/Scenarios/ThrowingNativeBatch.cs.meta` | `974c9b0f98d64527a4c7f8b594ffc583` |
| `Assets/Editor/Scenarios/ThrowingNaturalNativeBatch.cs` | `Assets/Editor/Scenarios/ThrowingNaturalNativeBatch.cs.meta` | `6aadaecca27c47bcaf18695dbf474e50` |
| `Assets/Resources/Content/Data/Skills/Throwing.json` | `Assets/Resources/Content/Data/Skills/Throwing.json.meta` | `0f9f319ba7df406ca90dda0d0b69b314` |
| `Assets/Scripts/Gameplay/Combat/ThrowTrajectory.cs` | `Assets/Scripts/Gameplay/Combat/ThrowTrajectory.cs.meta` | `0a61dec8ac9c4c88bc27b164d6869834` |
| `Assets/Scripts/Gameplay/Inventory/ActionResolutionGuard.cs` | `Assets/Scripts/Gameplay/Inventory/ActionResolutionGuard.cs.meta` | `db7013d09d9c46a29323c0227a89683e` |
| `Assets/Scripts/Gameplay/Items/ThrowPayloadResolver.cs` | `Assets/Scripts/Gameplay/Items/ThrowPayloadResolver.cs.meta` | `6081d81f4bcc44b99cdf6c2d6ea679ac` |
| `Assets/Scripts/Gameplay/Look/ThrowPreview.cs` | `Assets/Scripts/Gameplay/Look/ThrowPreview.cs.meta` | `6892309bfdec4fa69cb13894706af0ef` |
| `Assets/Scripts/Gameplay/Materials/FiniteLiquidFilmPart.cs` | `Assets/Scripts/Gameplay/Materials/FiniteLiquidFilmPart.cs.meta` | `ebdfd32088244e42b79a1927dc768b56` |
| `Assets/Scripts/Gameplay/Skills/ThrowingRules.cs` | `Assets/Scripts/Gameplay/Skills/ThrowingRules.cs.meta` | `77799e6a29c04ddbb1b2969e90050735` |
| `Assets/Scripts/Gameplay/Skills/ThrowingSkills.cs` | `Assets/Scripts/Gameplay/Skills/ThrowingSkills.cs.meta` | `d805e8b2f54c42748e26a49b0f1550e1` |
| `Assets/Scripts/Gameplay/Skills/ThrowingStatePart.cs` | `Assets/Scripts/Gameplay/Skills/ThrowingStatePart.cs.meta` | `f316d9663ac04e4a8719fb1529fad8ac` |
| `Assets/Scripts/Presentation/Input/InputHandler.Throwing.cs` | `Assets/Scripts/Presentation/Input/InputHandler.Throwing.cs.meta` | `78ac6f5d01c64479bb4d492acfa56604` |
| `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.Throwing.cs` | `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.Throwing.cs.meta` | `ba8110ad68884d0e97b89ab33d2bee3d` |
| `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.ThrowingContinuation.cs` | `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.ThrowingContinuation.cs.meta` | `ad4306d842ef442cadd68e9b0a148bb0` |
| `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.ThrowingVendor.cs` | `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.ThrowingVendor.cs.meta` | `88c7bf5841b14682b9adbb94239b9fb4` |
| `Assets/Scripts/Scenarios/Custom/ThrowingNativePlayer.cs` | `Assets/Scripts/Scenarios/Custom/ThrowingNativePlayer.cs.meta` | `d01922af7c45491f8756dec57ad8a145` |
| `Assets/Tests/EditMode/Gameplay/Combat/ThrowLifecycleTests.cs` | `Assets/Tests/EditMode/Gameplay/Combat/ThrowLifecycleTests.cs.meta` | `71f7e7691362450b80003b9be66ee891` |
| `Assets/Tests/EditMode/Gameplay/Combat/ThrowTrajectoryTests.cs` | `Assets/Tests/EditMode/Gameplay/Combat/ThrowTrajectoryTests.cs.meta` | `d3189980e102494eb7cdb747318cd9fb` |
| `Assets/Tests/EditMode/Gameplay/Combat/ThrowingAuditBaselineTests.cs` | `Assets/Tests/EditMode/Gameplay/Combat/ThrowingAuditBaselineTests.cs.meta` | `ae5c9bbf341048a08f3781fa1af49547` |
| `Assets/Tests/EditMode/Gameplay/Items/ThrowPayloadPersistenceTests.cs` | `Assets/Tests/EditMode/Gameplay/Items/ThrowPayloadPersistenceTests.cs.meta` | `a93b0f267ce44744b8d248b54e2d91a3` |
| `Assets/Tests/EditMode/Gameplay/Skills/ThrowingComparisonWorld.cs` | `Assets/Tests/EditMode/Gameplay/Skills/ThrowingComparisonWorld.cs.meta` | `8935863acc344389b80fcfbffcf5ee18` |
| `Assets/Tests/EditMode/Gameplay/Skills/ThrowingExpeditionComparisonTests.cs` | `Assets/Tests/EditMode/Gameplay/Skills/ThrowingExpeditionComparisonTests.cs.meta` | `ec9921c312fd43da9e4d39151027b858` |
| `Assets/Tests/EditMode/Gameplay/Skills/ThrowingOpportunityComparisonTests.cs` | `Assets/Tests/EditMode/Gameplay/Skills/ThrowingOpportunityComparisonTests.cs.meta` | `8edbbd53b26040aeb0dce91ec15bc5d9` |
| `Assets/Tests/EditMode/Gameplay/Skills/ThrowingPayloadComparisonTests.cs` | `Assets/Tests/EditMode/Gameplay/Skills/ThrowingPayloadComparisonTests.cs.meta` | `3124493f860a4548b62e95cc7ea153a3` |
| `Assets/Tests/EditMode/Gameplay/Skills/ThrowingSkillTests.cs` | `Assets/Tests/EditMode/Gameplay/Skills/ThrowingSkillTests.cs.meta` | `f1cab9ff95ed44528336cd778db14268` |
| `Assets/Tests/EditMode/Presentation/Input/ThrowCursorInputTests.cs` | `Assets/Tests/EditMode/Presentation/Input/ThrowCursorInputTests.cs.meta` | `3e9bead051e246cf953cd438c770212a` |
| `Assets/Tests/EditMode/Presentation/Input/ThrowPaymentBoundaryTests.cs` | `Assets/Tests/EditMode/Presentation/Input/ThrowPaymentBoundaryTests.cs.meta` | `e61691c3f93c42aaa2fd47b54234a95b` |
| `Assets/Tests/EditMode/Presentation/Input/ThrowSourcePopupVisibilityTests.cs` | `Assets/Tests/EditMode/Presentation/Input/ThrowSourcePopupVisibilityTests.cs.meta` | `8fb77af9ce6c414e9b42e99b1fe0b6f4` |

## Throwing design and living documents (6)

```text
Docs/THROWING-AUDIT-1.md
Docs/THROWING-AUDIT-2.md
Docs/THROWING-AUDIT-3-DESIGN.md
Docs/THROWING-EXECUTION-SOURCE-INDEX.md
Docs/THROWING-IMPLEMENTATION-PLAN.md
Docs/THROWING-PAYLOAD-IMPLEMENTATION.md
```

## Throwing evidence (272 exact files)

Keep RED receipts, partial/failed native reports and explicitly superseded comparison archives as historical evidence; their presence is not a pass claim. Includes both corrected controlled-native runs at 1280×800 (`7ef6804d…`) and 1280×720 (`d1e9a12e…`), source-popup RED/GREEN and selected precision receipts.

```text
Docs/Verification/Throwing/Native/1bac8a0157e442ac95010228cdaa8ba0/report.json
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/01-equipped-body-aim.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/02-ground-aim.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/03-measured-sources.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/04-measured-preparation.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/05-controller-focused-splash.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/06-focused-film-result.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/07-driving-preview.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/08-recovery-source.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/09-recovered-equipment.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/10-ground-source-preview.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/11-ground-source-landed.png
Docs/Verification/Throwing/Native/2bd74b3e3b3a41ee9e910de64b1d0aac/report.json
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/01-equipped-body-aim.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/02-ground-aim.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/03-measured-sources.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/04-measured-preparation.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/05-controller-focused-splash.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/06-focused-film-result.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/07-driving-preview.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/08-recovery-source.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/09-recovered-equipment.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/10-ground-source-preview.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/11-ground-source-landed.png
Docs/Verification/Throwing/Native/3f46a1702fa2477c94a8e5a38ff28644/report.json
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/01-equipped-body-aim.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/02-ground-aim.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/03-measured-sources.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/04-measured-preparation.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/05-controller-focused-splash.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/06-focused-film-result.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/07-driving-preview.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/08-recovery-source.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/09-recovered-equipment.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/10-ground-source-preview.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/11-ground-source-landed.png
Docs/Verification/Throwing/Native/7ef6804d5a204abe9bffbd592a900f43/report.json
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/01-equipped-body-aim.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/02-ground-aim.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/03-measured-sources.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/04-measured-preparation.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/05-controller-focused-splash.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/06-focused-film-result.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/07-driving-preview.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/08-recovery-source.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/09-recovered-equipment.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/10-ground-source-preview.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/11-ground-source-landed.png
Docs/Verification/Throwing/Native/c5e51c0b6e4d41a48ba9072ff76faaf4/report.json
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/01-equipped-body-aim.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/02-ground-aim.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/03-measured-sources.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/04-measured-preparation.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/05-controller-focused-splash.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/06-focused-film-result.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/07-driving-preview.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/08-recovery-source.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/09-recovered-equipment.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/10-ground-source-preview.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/11-ground-source-landed.png
Docs/Verification/Throwing/Native/d1e9a12ed2484c33ac5629b9d8d97299/report.json
Docs/Verification/Throwing/Recovery/editor-scene-before-restart.backup.gz
Docs/Verification/Throwing/Recovery/manifest.json
Docs/Verification/Throwing/aborted-reimport-cache-cleanup.json
Docs/Verification/Throwing/affected-comparison-test-names.json
Docs/Verification/Throwing/analyze_measured_revision.py
Docs/Verification/Throwing/analyze_payload_comparisons.py
Docs/Verification/Throwing/analyze_payload_precision.py
Docs/Verification/Throwing/analyze_physical_comparisons.py
Docs/Verification/Throwing/analyze_precision_gate.py
Docs/Verification/Throwing/audit1-baseline-fixtures.json
Docs/Verification/Throwing/audit1-dagger-red-with-dialogue-margin.xml
Docs/Verification/Throwing/baseline-core.xml
Docs/Verification/Throwing/baseline-fixtures.json
Docs/Verification/Throwing/baseline-terrain-save.xml
Docs/Verification/Throwing/baseline-tonic-corrected.xml
Docs/Verification/Throwing/batch-filter-attempt.json
Docs/Verification/Throwing/comparison-archive-manifest.json
Docs/Verification/Throwing/comparison-explicit-stun-correction-archive.json
Docs/Verification/Throwing/comparison-stun-correction-archive.json
Docs/Verification/Throwing/comparison-stun-seeding-red.txt
Docs/Verification/Throwing/comparisons-plus-preview-red.xml
Docs/Verification/Throwing/comparisons/W1-0-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/W1-0.json.gz
Docs/Verification/Throwing/comparisons/W1-2-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/W1-2-precision100-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/W1-2-precision100.json.gz
Docs/Verification/Throwing/comparisons/W1-2.json.gz
Docs/Verification/Throwing/comparisons/W1-4-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/W1-4.json.gz
Docs/Verification/Throwing/comparisons/W1-6-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/W1-6.json.gz
Docs/Verification/Throwing/comparisons/W2-0.json.gz
Docs/Verification/Throwing/comparisons/W2-2.json.gz
Docs/Verification/Throwing/comparisons/W2-4.json.gz
Docs/Verification/Throwing/comparisons/W2-6.json.gz
Docs/Verification/Throwing/comparisons/W3-budget-0.json.gz
Docs/Verification/Throwing/comparisons/W3-budget-2.json.gz
Docs/Verification/Throwing/comparisons/W3-budget-4.json.gz
Docs/Verification/Throwing/comparisons/W3-budget-6.json.gz
Docs/Verification/Throwing/comparisons/W4-0.json.gz
Docs/Verification/Throwing/comparisons/W4-2-precision100.json.gz
Docs/Verification/Throwing/comparisons/W4-2.json.gz
Docs/Verification/Throwing/comparisons/W4-4.json.gz
Docs/Verification/Throwing/comparisons/W4-6.json.gz
Docs/Verification/Throwing/comparisons/W4-reversal-2-precision100.json.gz
Docs/Verification/Throwing/comparisons/W4-reversal-2.json.gz
Docs/Verification/Throwing/comparisons/W4-reversal-4.json.gz
Docs/Verification/Throwing/comparisons/W4-reversal-6.json.gz
Docs/Verification/Throwing/comparisons/W5-0.json.gz
Docs/Verification/Throwing/comparisons/W5-2-precision100.json.gz
Docs/Verification/Throwing/comparisons/W5-2.json.gz
Docs/Verification/Throwing/comparisons/W5-4.json.gz
Docs/Verification/Throwing/comparisons/W5-6.json.gz
Docs/Verification/Throwing/comparisons/W6-0.json.gz
Docs/Verification/Throwing/comparisons/W6-2.json.gz
Docs/Verification/Throwing/comparisons/W6-4.json.gz
Docs/Verification/Throwing/comparisons/W6-6-precision100.json.gz
Docs/Verification/Throwing/comparisons/W6-6.json.gz
Docs/Verification/Throwing/comparisons/W7-budget-0.json.gz
Docs/Verification/Throwing/comparisons/W7-budget-2.json.gz
Docs/Verification/Throwing/comparisons/W7-budget-4.json.gz
Docs/Verification/Throwing/comparisons/W7-budget-6.json.gz
Docs/Verification/Throwing/comparisons/W7-order-budget-0.json.gz
Docs/Verification/Throwing/comparisons/W7-order-budget-2.json.gz
Docs/Verification/Throwing/comparisons/W7-order-budget-4.json.gz
Docs/Verification/Throwing/comparisons/W7-order-budget-6.json.gz
Docs/Verification/Throwing/comparisons/W7-precision100-0-bank-open-broad-ground.json.gz
Docs/Verification/Throwing/comparisons/W7-precision100-0-bank-open-direct.json.gz
Docs/Verification/Throwing/comparisons/W7-precision100-2-focused-acid-immune-broad-ground.json.gz
Docs/Verification/Throwing/comparisons/W7-precision100-2-focused-acid-immune-focused-if-owned.json.gz
Docs/Verification/Throwing/comparisons/W7-precision100-2-focused-fast-broad-ground.json.gz
Docs/Verification/Throwing/comparisons/W7-precision100-2-focused-fast-focused-if-owned.json.gz
Docs/Verification/Throwing/comparisons/W7-precision100-2-focused-open-broad-ground.json.gz
Docs/Verification/Throwing/comparisons/W7-precision100-2-focused-open-focused-if-owned.json.gz
Docs/Verification/Throwing/comparisons/W7-precision100-2-jet-open-available-control.json.gz
Docs/Verification/Throwing/comparisons/baseline-measured-plus2/W1-0.json.gz
Docs/Verification/Throwing/comparisons/baseline-measured-plus2/W1-2-precision100.json.gz
Docs/Verification/Throwing/comparisons/baseline-measured-plus2/W1-2.json.gz
Docs/Verification/Throwing/comparisons/baseline-measured-plus2/W1-4.json.gz
Docs/Verification/Throwing/comparisons/baseline-measured-plus2/W1-6.json.gz
Docs/Verification/Throwing/comparisons/packages-0.json.gz
Docs/Verification/Throwing/comparisons/packages-2.json.gz
Docs/Verification/Throwing/comparisons/packages-4.json.gz
Docs/Verification/Throwing/comparisons/packages-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W6-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W6-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W6-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W6-6-precision100.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W6-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W7-budget-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W7-budget-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W7-budget-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W7-budget-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W7-order-budget-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W7-order-budget-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W7-order-budget-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W7-order-budget-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-before-stun-seeding/W7-precision100-2-jet-open-available-control.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W6-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W6-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W6-6-precision100.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W6-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W7-budget-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W7-budget-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W7-budget-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W7-budget-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W7-order-budget-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W7-order-budget-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W7-order-budget-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-canonical-before-final-explicit-stream/W7-order-budget-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-0-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-2-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-2-precision100-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-2-precision100.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-4-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-6-measured-r1.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W1-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W2-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W2-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W2-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W2-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W4-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W4-2-precision100.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W4-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W4-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W4-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W4-reversal-2-precision100.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W4-reversal-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W4-reversal-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W4-reversal-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W5-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W5-2-precision100.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W5-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W5-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W5-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W6-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W6-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W6-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W6-6-precision100.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W6-6.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W7-precision100-0-bank-open-broad-ground.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W7-precision100-0-bank-open-direct.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W7-precision100-2-focused-acid-immune-broad-ground.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W7-precision100-2-focused-acid-immune-focused-if-owned.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W7-precision100-2-focused-fast-broad-ground.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W7-precision100-2-focused-fast-focused-if-owned.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W7-precision100-2-focused-open-broad-ground.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W7-precision100-2-focused-open-focused-if-owned.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/W7-precision100-2-jet-open-available-control.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/packages-0.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/packages-2.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/packages-4.json.gz
Docs/Verification/Throwing/comparisons/superseded-overwritten-explicit-stun-stream/packages-6.json.gz
Docs/Verification/Throwing/death-visibility-review-red.xml
Docs/Verification/Throwing/driving-reversal-green.xml
Docs/Verification/Throwing/editor-view-restoration.json
Docs/Verification/Throwing/explicit-stream-comparisons-green.xml
Docs/Verification/Throwing/explicit-stun-and-skills-green.xml
Docs/Verification/Throwing/explicit-stun-stream-red.xml
Docs/Verification/Throwing/final-comparison-test-names.json
Docs/Verification/Throwing/final-death-visibility-regression-green.xml
Docs/Verification/Throwing/final-regression-green.xml
Docs/Verification/Throwing/final-verification-summary.json
Docs/Verification/Throwing/flight-payload-smoke-green.xml
Docs/Verification/Throwing/flight-render-red.json
Docs/Verification/Throwing/geometry-red.json
Docs/Verification/Throwing/geometry-red.txt
Docs/Verification/Throwing/idle-unity-cache-cleanup.json
Docs/Verification/Throwing/incomplete-final-sweep.json
Docs/Verification/Throwing/integration-first.xml
Docs/Verification/Throwing/integration-review-red.xml
Docs/Verification/Throwing/lifecycle-red-geometry-initial.xml
Docs/Verification/Throwing/measured-and-seeded-stun-green.xml
Docs/Verification/Throwing/measured-revision-1.md
Docs/Verification/Throwing/measured-revision-summary.md
Docs/Verification/Throwing/measured-tuning-red.xml
Docs/Verification/Throwing/measured-tuning-stale-assembly-rejected.xml
Docs/Verification/Throwing/native-baseline-after.png
Docs/Verification/Throwing/native-baseline-target.png
Docs/Verification/Throwing/native-controlled-route.md
Docs/Verification/Throwing/natural-archive-manifest.json
Docs/Verification/Throwing/natural-evidence.md
Docs/Verification/Throwing/package-cache-compression.json
Docs/Verification/Throwing/payload-comparison-summary.csv
Docs/Verification/Throwing/payload-comparison-summary.md
Docs/Verification/Throwing/payload-precision-gate.md
Docs/Verification/Throwing/payload-red.json
Docs/Verification/Throwing/payment-actor-lease-red.xml
Docs/Verification/Throwing/physical-comparison-summary.csv
Docs/Verification/Throwing/physical-comparison-summary.md
Docs/Verification/Throwing/precision-gate.md
Docs/Verification/Throwing/precision-overlap.md
Docs/Verification/Throwing/precision100-payload-green.xml
Docs/Verification/Throwing/precision100-physical-green.xml
Docs/Verification/Throwing/redundant-extracted-build-verification.json
Docs/Verification/Throwing/review-geometry-green-comparison-smoke.xml
Docs/Verification/Throwing/review-geometry-preview-red.xml
Docs/Verification/Throwing/review-payload-description-red.xml
Docs/Verification/Throwing/seeded-comparisons-green.xml
Docs/Verification/Throwing/skills-implementation.md
Docs/Verification/Throwing/skills-red.json
Docs/Verification/Throwing/source-popup-visibility-green.xml
Docs/Verification/Throwing/source-popup-visibility-red.xml
Docs/Verification/Throwing/staging-inventory.md
Docs/Verification/Throwing/staging-paths.txt
Docs/Verification/Throwing/sweep-first.xml
Docs/Verification/Throwing/transparent-diagnostic-compression.json
Docs/Verification/Throwing/ui-red.txt
```

## Throwing natural reports outside this directory (61 exact files)

Only the five terminal campaigns explicitly recorded by natural-archive-manifest.json are admitted. Each archive hash was checked and each report identifies a throwing mode. These source-guided reports remain partial where their checks failed. Do not stage the entire shared SpreadDiscoveryExpeditions directory.

```text
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-000-action-0-opening-00-build-picker.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-001-action-0-throwing-natural-01-actual-start.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-002-action-60-district-09-native-cache-contents.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-003-action-64-connected-announcement-1-page-0.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-004-action-201-connected-duelist-defense-1.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-005-action-291-connected-duelist-defense-2.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-006-action-381-connected-announcement-2-page-0.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-007-action-467-opening-skill-ThrowingSkill.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-008-action-467-opening-earned-purchase-review.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-009-action-467-opening-earned-deflect-owned.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-010-action-467-throwing-natural-02-earned-sure-grip.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-011-action-480-throwing-natural-no-opportunity.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/capture-012-action-480-throwing-natural-05-retained-earned-checkpoint.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/026cda75ca4c47bda4781255878963b7/report.json.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/campaign-expedition-0-earned-1.sav.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/campaign-expedition-1-earned-1.sav.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/campaign-first-point.sav.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-000-action-0-opening-00-build-picker.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-001-action-0-throwing-natural-01-actual-start.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-002-action-60-district-09-native-cache-contents.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-003-action-64-connected-announcement-1-page-0.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-004-action-373-connected-announcement-2-page-0.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-005-action-465-opening-skill-ThrowingSkill.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-006-action-465-opening-earned-purchase-review.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-007-action-465-opening-earned-deflect-owned.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-008-action-465-throwing-natural-02-earned-sure-grip.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-009-action-476-throwing-natural-no-opportunity.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-010-action-476-throwing-natural-05-retained-earned-checkpoint.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-011-action-482-throwing-natural-05-retained-earned-checkpoint.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/capture-012-action-488-throwing-natural-05-retained-earned-checkpoint.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/67c0be707d2941d9aae53474927dd036/report.json.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/7e313c0c8ba543ac888fded6fe8a1412/capture-000-action-0-opening-00-build-picker.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/7e313c0c8ba543ac888fded6fe8a1412/capture-001-action-0-throwing-natural-01-actual-start.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/7e313c0c8ba543ac888fded6fe8a1412/capture-002-action-60-district-09-native-cache-contents.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/7e313c0c8ba543ac888fded6fe8a1412/capture-003-action-64-connected-announcement-1-page-0.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/7e313c0c8ba543ac888fded6fe8a1412/report.json.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/campaign-expedition-0-earned-1.sav.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/campaign-expedition-1-earned-1.sav.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/campaign-expedition-2-earned-1.sav.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/campaign-first-point.sav.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-000-action-0-opening-00-build-picker.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-001-action-0-throwing-natural-01-actual-start.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-002-action-62-district-09-native-cache-contents.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-003-action-67-connected-announcement-1-page-0.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-004-action-382-connected-announcement-2-page-0.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-005-action-427-opening-skill-ThrowingSkill.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-006-action-427-opening-earned-purchase-review.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-007-action-427-opening-earned-deflect-owned.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-008-action-427-throwing-natural-02-earned-sure-grip.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-009-action-511-throwing-natural-no-opportunity.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-010-action-511-throwing-natural-05-retained-earned-checkpoint.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-011-action-531-throwing-vendor-actual-stock.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-012-action-582-throwing-natural-05-retained-earned-checkpoint.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-013-action-588-throwing-natural-05-retained-earned-checkpoint.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/capture-014-action-628-throwing-natural-05-retained-earned-checkpoint.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/c782c0f98a2548da8de1b11f549e5df6/report.json.gz
Docs/Verification/SpreadDiscoveryExpeditions/Native/e408bf98e48f445781e4219f92e29c55/capture-000-action-0-opening-00-build-picker.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/e408bf98e48f445781e4219f92e29c55/capture-001-action-0-throwing-natural-01-actual-start.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/e408bf98e48f445781e4219f92e29c55/capture-002-action-60-district-09-native-cache-contents.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/e408bf98e48f445781e4219f92e29c55/capture-003-action-64-connected-announcement-1-page-0.png
Docs/Verification/SpreadDiscoveryExpeditions/Native/e408bf98e48f445781e4219f92e29c55/report.json.gz
```

## Local disk compaction receipt

The cleanup manifest is included; it records58 old untracked diagnostic archives and verified SHA-256 values. Source, tracked artifacts, active logs and authoring exports were excluded. Saved59,840,996 bytes. Those historical archive payloads remain local and outside this throwing task list.

```text
Docs/Verification/LOCAL-DIAGNOSTIC-ARCHIVE-2026-10-16.json
```

## Exclusions and temporary-helper check

No reconnect-named source or metadata remains under Assets; the temporary reconnect helper has been removed. No other reconnect source-content match exists under Assets. The untracked Assets/Scripts/Editor.meta remains excluded. No helper deletion or other cleanup was performed while preparing this snapshot.

The unrelated/generated untracked Assets exclusions are unchanged:

```text
Assets/InitTestScene3702ca5b-f92b-48fa-92a0-976aef8efa7f.unity
Assets/InitTestScene3702ca5b-f92b-48fa-92a0-976aef8efa7f.unity.meta
Assets/InitTestScenedcab54b0-66de-4e6d-8cf6-c72ca524bef6.unity
Assets/InitTestScenedcab54b0-66de-4e6d-8cf6-c72ca524bef6.unity.meta
Assets/Scripts/Editor.meta
```

Historical ArtSource, build, and verification artifacts outside the named task paths are excluded. Local __pycache__ files are excluded. Do not use `git add .` for this task.

Machine-readable list: 438 existing paths; total bytes at snapshot: 40,625,149.
