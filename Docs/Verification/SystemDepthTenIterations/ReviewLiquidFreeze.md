# Liquid review — FrostLichen on natural water-family ground

Status: observed standalone RED→GREEN, adversarial checks, independent review and final native Unity EditMode verification complete.

Iteration 04 correctly stopped presenting authored brine/mire as pure water. The reaction resolver now matches their explicit ground water family, but `MaterialFieldActions.CanGround` still required a literal water coating at review. As a result, the existing finite FrostLichen action had disappeared on these authored pools. The affected source is FrostLichen, not the initially suspected CryogelVial.

Restore that existing ground action using the same literal-first water-family selection as the reaction resolver. Preserve pure-water drinking/collection restrictions and unrelated liquids. Bind the actual matched coating during the inventory transaction so a callback cannot swap the selected layer or its priority while retaining the old proof. Do not alter the pool's literal identity or body-coating properties.

Tests first: real factory BrinePool/MirePool, carried FrostLichen and ordinary InventorySystem action; actual ice and one-item payment; pure water, dry/oil/non-water counters; rollback and callback layer replacement/priority. Follow with existing MaterialField and natural-liquid source suites. No spell or generic reaction expansion.

## Evidence and final implementation

- `review-liquid-freeze-red.xml`: 7 cases, 2 intended missing-menu-choice failures (actual authored BrinePool/MirePool), 5 positive/negative controls passed. Production remained unchanged for this run.
- `review-liquid-freeze-green.xml`: 115/115. The 23 new cases comprise 7 core plus 16 in the dedicated adversarial fixture; 92 existing MaterialField, MaterialFieldAdversarial and NaturalLiquidSourceDepth cases also pass.
- Counterchecks cover plain water, dry/oil/acid/gel refusal, unchanged natural pool volume, one-item spending, pure-water drinking refusal, literal-water reaction precedence, same/different coating replacement, new higher-priority water, removal, hidden/blocked ground, and later inventory exception rollback.
- The command remains a request to freeze the selected wet ground. At action execution it captures the exact current coating the reaction will consume; commit validation requires that same instance, duration and reaction-selection priority. No cold/reaction occurs until inventory commit.
- Only `MaterialFieldActions.cs`, new `NaturalLiquidFreezeReviewTests.cs` plus metadata, this review log and two receipts change. The helper mirrors existing exact-water-first `TileReactionSystem.FindInputCoating` semantics for this one action. Drinking and source purity do not use the family alias.
- Self-review: 🟡 closed a concrete regression from the iteration04 literal identity correction; 🔵 finite cost and existing inventory lifecycle remain authoritative; 🧪 native input/rendering verification remains separate. No other literal-water consumer was broadened without a demonstrated regression.
- Runner: .NET SDK 10.0.105, isolated tracked EditModeRunner, one worker; native Unity remains the source of truth.
- Independent rendering/navigation-agent cold review: no actionable defect. Exact-first then insertion-order family selection matches the reaction resolver; actual-layer identity/turns/precedence checks and adverse callbacks cover the key stale-action risk.

- Final native Unity EditMode: [native-final-integration.xml](native-final-integration.xml), job `941ad3a3628542c19f5fda2b0a9e49b4`, **810/810 passed, 0 failed, 0 skipped**. All 23 new freeze cases, 66 existing MaterialField cases and 26 NaturalLiquidSourceDepth cases passed (115 relevant cases). Native input/Play rendering remains separate; these tests exercise the real inventory action and reaction systems in EditMode.
