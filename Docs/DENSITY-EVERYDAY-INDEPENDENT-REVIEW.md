# Independent C3 water/cooking and next-band rest review

Status: all 16 independent probes pass after the production owner fixed the findings. The expanded RED was 16 total, three living controls passing and 13 failures, including both ordinary-rest dead controls and the maximum Parched stack case. Combined everyday owner verification is 93/93; receipt `Docs/Verification/DensityCompletion/Everyday/review-green.xml`. No production changes were made by this reviewer.

## Findings with counter-checks

### 🟡 Dead actors can use the new actions

`WaterVesselService.TryAct` and `CookingService.TryCook` validate inventory ownership but do not reject an actor with zero HP or `_DeathHandled`. The real `InventorySystem.PerformAction` route accepts FillWaterskin, DrinkWaterskin and Cook for both states. Six tests fail; the matching three living-actor tests pass. A stale action must refuse before consuming, transferring or transforming resources. Actors without an HP stat can retain their existing test/tool contract; actors with HP or committed death have explicit validity signals.

### 🟡 Next-band rest accepts dead actors

`RestSystem.TryRestUntilNextBand` checks placement, clock and overflow, then calls the existing rest primitive without checking living state. A zero-HP actor is healed and the clock advances; a death-handled actor with positive HP also rests. Both dedicated tests fail. This report concerns the newly added route; the existing ordinary-rest behavior predates this milestone.

### 🟡 Drink rollback duplicates or miscounts a new dehydration exposure

The undo callback restores the original absolute `ParchedEffect.Stacks`, but adds a relative penalty delta. If a new Parched exposure occurs while an outer transaction is open, those policies disagree:

- Initial one stack: drink removes the effect, another exposure creates a new instance, rollback reinserts the old instance. There are now two Parched objects.
- Initial two stacks: drink reduces to one, another exposure raises it to two, rollback restores two stacks but adds one penalty. The actor has two represented stacks and three Strength/Agility penalty points.

Both tests fail. Preserve the independent exposure by restoring the drink's relieved stack as a bounded delta to the current active effect, or restoring the original identity if none exists. Keep stack count and penalties consistent, including the MaxStacks cap. Retain existing rollback identity and unrelated Weakness controls.

## Review scope and limits

Inspected WaterskinPart, WaterVesselService, CookablePart, CookingService, RestSystem, CampfirePart, InventorySystem command dispatch, ParchedEffect, the rollback restore hook, and the existing everyday core/adversarial fixtures. Ownership, finite-water conservation, per-unit product creation, real destination stacking, outer transaction receipts and physical footprint reach already have useful coverage. A separate reviewer owns the failed-cooking receipt double-restore issue; this report does not duplicate it.

Evidence: `Docs/Verification/DensityCompletion/EverydayIndependent/red.xml.gz`, `red.log.gz`, and the exact temporary probe source `red-tests.cs.txt`. These are core .NET proofs and counter-checks, not evidence of native UI/input or natural acquisitions. No Unity tools, commits or pushes were used by this reviewer.

Owner fix confirmed: living-actor guards now precede water/cooking and shared rest mutation, with cooking revalidation after product initialization. Drink undo restores one relieved stack to the current Parched object, capped at three, with matching actual penalty delta; when absent, it restores the original object. The new cap counter-check prevents four penalty points representing three stacks. Source fixture is `Assets/Tests/EditMode/Gameplay/Items/DensityEverydayIndependentReviewTests.cs`; expanded RED receipts are preserved alongside the initial ones.
