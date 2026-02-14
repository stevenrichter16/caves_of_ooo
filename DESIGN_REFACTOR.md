**Proposed Changes (no edits yet)**

1. Build an inventory transaction pipeline.  
I’d add a command layer around pickup/equip/unequip/drop so each action runs as `Validate -> Execute -> Rollback on failure` instead of mutating state in many places.  
Primary target: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/InventorySystem.cs`  
Why: prevents half-applied states between inventory, body parts, stacks, and zone.

2. Add an equip planner + rule specifications.  
I’d move slot legality into composable rules (`SlotType`, `Laterality`, `SlotsRequired`, `BodyDependency`, `Size/Gigantic`, `ConflictPolicy`) and have a planner return an equip plan before applying anything.  
Primary targets: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/Body.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Core/Anatomy/BodyPart.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Core/EquippablePart.cs`  
Why: this is the biggest gap with Qud-like equipment robustness.

3. Replace string-event hot paths with typed domain events (while keeping compatibility).  
I’d keep your event-driven style, but add typed wrappers for critical inventory flows and gradually migrate string IDs.  
Primary targets: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/InventorySystem.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Core/InventoryAction.cs`  
Why: fewer typo bugs, easier refactors, better tooling support.

4. Introduce a centralized stat modifier resolver.  
Instead of directly editing stat fields from equipment/mutations/consumables, I’d aggregate modifiers from sources and compute final values in one pass.  
Primary targets: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/InventorySystem.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Core/MutationsPart.cs`  
Why: avoids stacking/removal bugs and makes balancing easier.

5. Refactor inventory UI/input into a formal state machine.  
I’d model states explicitly (`Closed`, `ListFocus`, `EquipPopup`, `ItemActionPopup`, `BodyPartPicker`) and route input by state.  
Primary targets: `/Users/steven/caves-of-ooo/Assets/Scripts/Rendering/InventoryUI.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Rendering/InputHandler.cs`  
Why: current behavior already has state transitions; formalizing them reduces edge-case input bugs.

6. Rework trading into an offer aggregate with commit.  
I’d split trading into `DraftOffer`, `Validate`, `Preview delta`, and atomic `Commit` (items + currency + ownership consequences).  
Primary target: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/TradeSystem.cs`  
Why: needed for Qud-like bartering depth and safe complex transfers.

7. Add regression tests before refactor rollout.  
I’d add tests for stacked equip/unequip, multi-slot conflicts, rollback correctness, and overburden edge cases.  
Primary targets: inventory/core test project paths in your Unity test assemblies.  
Why: these systems are interconnected and easy to break during upgrades.

If you want, I can next draft the exact class/interface skeletons and migration order (still no file edits).

I’ll re-scan the current inventory-related code first, then I’ll give you an updated class/interface skeleton and migration order that reflects the newest implementation state.

Explored 3 lists
I found several recently updated inventory files (InventorySystem, InventoryUI, InputHandler, PickupUI). I’m now mapping the new architecture before proposing the refactor skeleton so it aligns with current code, not the older snapshot.

Explored 5 files, 5 searches
I’ve confirmed new additions like PickupUI and displacement previews. I’m now checking bootstrap wiring and then I’ll give you an updated, concrete refactor blueprint that builds on these new pieces instead of replacing them.

Explored 4 files, 1 search
Worked for 1m 14s
Perfect. I rechecked the current code and updated this plan to match the new inventory flow you added.

Current additions I’m explicitly accounting for:

PreviewDisplacements already exists in InventorySystem.cs (line 611).
Displacement confirmation popup exists in InventoryUI.cs (line 55).
Pickup now has its own UI/state (PickupOpen) in InputHandler.cs (line 64) and PickupUI.cs (line 14).
Updated skeleton (no edits yet)

// Core context shared by commands.
public sealed class InventoryContext
{
    public Entity Actor { get; }
    public Zone Zone { get; }
    public InventoryPart Inventory { get; }
    public Body Body { get; }
}

// Generic command pipeline.
public interface IInventoryCommand
{
    ValidationResult Validate(InventoryContext ctx);
    CommandResult Execute(InventoryContext ctx, InventoryTransaction tx);
}

// Undo log for rollback.
public sealed class InventoryTransaction
{
    public void Do(Action apply, Action undo);
    public void Commit();
    public void Rollback();
}

// Existing behavior wrappers first, then real internals move in.
public sealed class PickupCommand : IInventoryCommand { /* uses current Pickup flow */ }
public sealed class DropCommand : IInventoryCommand { /* uses Drop/DropPartial */ }
public sealed class EquipCommand : IInventoryCommand
{
    public EquipPlan Plan { get; }
}
public sealed class UnequipCommand : IInventoryCommand { }

// Single source of truth for equip preview + execution.
public sealed class EquipPlan
{
    public Entity Item { get; init; }
    public BodyPart TargetPart { get; init; } // optional
    public List<BodyPart> OccupiedParts { get; init; }
    public List<InventorySystem.Displacement> Displacements { get; init; }
    public bool IsValid { get; init; }
    public string FailureReason { get; init; }
}

public interface IEquipRule
{
    RuleResult Check(EquipPlanBuilder builder);
}

public sealed class EquipPlanner
{
    public EquipPlan Build(Entity actor, Entity item, BodyPart targetPart = null);
}
Key design change based on your new code

Keep PreviewDisplacements, but make it call EquipPlanner.Build(...) and return plan.Displacements.
In InventoryUI, store the full EquipPlan in the confirmation state, not just ItemToEquip + TargetPart.
On confirm, execute InventorySystem.Equip(plan) so preview and actual equip cannot diverge.
Migration order (safe and incremental)

Add InventoryContext, InventoryTransaction, and IInventoryCommand with zero behavior change adapters around existing methods in InventorySystem.cs.
Add EquipPlanner and EquipPlan; have current PreviewDisplacements delegate to it.
Add InventorySystem.Equip(EquipPlan plan) overload; keep old Equip(actor,item,target) and internally build/execute a plan.
Update InventoryUI displacement popup state in InventoryUI.cs to carry EquipPlan.
Convert pickup/drop/equip entry points in InputHandler and PickupUI to use command pipeline wrappers.
Add typed domain events as wrappers (optional next phase), while preserving existing string events for compatibility.
Then move container/trade flows onto same command+transaction pattern.
What I would not replace

Your current PickupUI state split.
Your displacement confirmation UX.
Your body-part-aware equip logic shape.
Your inventory screen data builder/paperdoll layout pipeline.
If you want, next I can draft the exact file-by-file class placement map (which new types go in which file/folder) before you start implementing.


draft the exact file-by-file class placement map (which new types go in which file/folder) before I start implementing. New code was just added a minute ago so make sure you take that new code into account

I’ll re-scan the repository state right now (including most recently modified files) and then produce an exact file-by-file placement map that matches the current code, not the earlier snapshot.

Explored 2 lists
Ran git status --short


Explored 1 list
I can see fresh changes in InputHandler and PickupUI; I’m reading those current versions now so the placement map includes the newest UI/input architecture and not stale assumptions.

Explored 2 files
I’ve got the latest InputHandler/PickupUI state. I’ll quickly verify the current bootstrap wiring and test layout so the file map includes exact folder boundaries for new command/planner types.

Explored 2 files, 1 list
Worked for 1m 17s
Pulled from latest repo state just now (including fresh PickupUI + InputHandler updates).
This is the exact placement map I’d use before implementation.

Core New Files

InventoryContext.cs
InventoryContext

InventoryTransaction.cs
InventoryTransaction, TransactionStep (internal/private helper type)

InventoryValidationResult.cs
InventoryValidationResult, InventoryValidationErrorCode

InventoryCommandResult.cs
InventoryCommandResult, InventoryCommandErrorCode

IInventoryCommand.cs
IInventoryCommand

InventoryCommandExecutor.cs
InventoryCommandExecutor

Command Files
7. PickupCommand.cs
PickupCommand

DropCommand.cs
DropCommand

DropPartialCommand.cs
DropPartialCommand

EquipCommand.cs
EquipCommand

UnequipCommand.cs
UnequipCommand

AutoEquipCommand.cs
AutoEquipCommand

TakeFromContainerCommand.cs
TakeFromContainerCommand

PutInContainerCommand.cs
PutInContainerCommand

PerformInventoryActionCommand.cs
PerformInventoryActionCommand

Equip Planning Files
16. EquipPlan.cs
EquipPlan, EquipPlanSlot

InventoryDisplacement.cs
InventoryDisplacement (move/replace current nested InventorySystem.Displacement)

EquipPlanner.cs
EquipPlanner

EquipPlanBuilder.cs
EquipPlanBuilder

Equip Rule Files
20. IEquipRule.cs
IEquipRule

EquipRuleResult.cs
EquipRuleResult

TargetPartCompatibilityRule.cs
TargetPartCompatibilityRule

SlotAvailabilityRule.cs
SlotAvailabilityRule

SlotCountRule.cs
SlotCountRule

DisplacementRule.cs
DisplacementRule

StackSplitRule.cs
StackSplitRule

Existing Files To Wire (no new file needed)

InventorySystem.cs
Keep as public facade. Route current static methods to InventoryCommandExecutor and EquipPlanner.

InventoryUI.cs
Change popup state to store EquipPlan directly (instead of item+target only), then confirm executes that plan.

InputHandler.cs
No new state needed; keep InputState (Normal, AwaitingDirection, InventoryOpen, PickupOpen) and swap internals to command facade calls.

PickupUI.cs
Keep current overlay popup architecture; just call command facade methods.

InventoryScreenData.cs
Use planner-backed displacement/equip metadata only if needed for richer previews.

Test File Placement

InventoryCommandExecutorTests.cs
InventoryTransactionTests.cs
EquipPlannerTests.cs
EquipCommandTests.cs
PickupFlowTests.cs
If you want, I can next give the exact order to create these files so it compiles at every step without broken references.


yes

Use this exact order so you can keep the project compiling at every step.

Create folders only.
/Users/steven/caves-of-ooo/Assets/Scripts/Core/Inventory/
/Users/steven/caves-of-ooo/Assets/Scripts/Core/Inventory/Commands/
/Users/steven/caves-of-ooo/Assets/Scripts/Core/Inventory/Planning/
/Users/steven/caves-of-ooo/Assets/Scripts/Core/Inventory/Rules/

Add pure DTO/result files (no dependencies on existing systems).
InventoryValidationResult.cs
InventoryCommandResult.cs

Add context + interfaces.
InventoryContext.cs
IInventoryCommand.cs

Add transaction + executor shell.
InventoryTransaction.cs
InventoryCommandExecutor.cs

Add planning core models.
InventoryDisplacement.cs
EquipPlan.cs
EquipPlanBuilder.cs

Add rule contracts and result model.
IEquipRule.cs
EquipRuleResult.cs

Add concrete rules (still not wired).
TargetPartCompatibilityRule.cs
SlotAvailabilityRule.cs
SlotCountRule.cs
DisplacementRule.cs
StackSplitRule.cs

Add planner.
EquipPlanner.cs

Add command classes, but initially let them call current InventorySystem methods (no integration yet).
PickupCommand.cs
DropCommand.cs
DropPartialCommand.cs
EquipCommand.cs
UnequipCommand.cs
AutoEquipCommand.cs
TakeFromContainerCommand.cs
PutInContainerCommand.cs
PerformInventoryActionCommand.cs

In InventorySystem.cs, add new overloads only (don’t replace old methods yet):
BuildEquipPlan(...), Equip(EquipPlan plan), PreviewEquipPlan(...).

In InventorySystem.cs, make PreviewDisplacements(...) delegate to EquipPlanner but preserve current return shape so InventoryUI.cs keeps compiling.

Update InventoryUI.cs displacement confirm state to hold EquipPlan directly; keep existing popup flow intact (important because this file just changed and already has displace confirmation logic).

After planner path is stable, switch InventorySystem public methods one-by-one to executor-backed commands in this order: Pickup, Drop, DropPartial, Equip, UnequipItem, container transfer, PerformAction.
Do not switch all at once.

Add tests in parallel with each switch.
InventoryCommandExecutorTests.cs
InventoryTransactionTests.cs
EquipPlannerTests.cs
EquipCommandTests.cs
PickupFlowTests.cs

Final cleanup pass: remove duplicated slot-search/displacement code from InventorySystem.cs once planner is the single source of truth.


Here are the first 5 skeleton files, ready for your current codebase state.

InventoryValidationResult.cs
namespace CavesOfOoo.Core.Inventory
{
    public enum InventoryValidationErrorCode
    {
        None = 0,
        InvalidActor,
        InvalidItem,
        InvalidZone,
        MissingInventoryPart,
        MissingBody,
        MissingEquippablePart,
        NotTakeable,
        NotOwned,
        NoCompatibleSlot,
        OverWeightLimit,
        BlockedByRule,
        Unknown
    }

    public sealed class InventoryValidationResult
    {
        public bool IsValid { get; }
        public InventoryValidationErrorCode ErrorCode { get; }
        public string Message { get; }

        private InventoryValidationResult(bool isValid, InventoryValidationErrorCode errorCode, string message)
        {
            IsValid = isValid;
            ErrorCode = errorCode;
            Message = message ?? string.Empty;
        }

        public static InventoryValidationResult Valid()
        {
            return new InventoryValidationResult(true, InventoryValidationErrorCode.None, string.Empty);
        }

        public static InventoryValidationResult Invalid(InventoryValidationErrorCode code, string message)
        {
            return new InventoryValidationResult(false, code, message);
        }
    }
}
InventoryCommandResult.cs
namespace CavesOfOoo.Core.Inventory
{
    public enum InventoryCommandErrorCode
    {
        None = 0,
        ValidationFailed,
        ExecutionFailed,
        RollbackFailed,
        Unknown
    }

    public sealed class InventoryCommandResult
    {
        public bool Succeeded { get; }
        public InventoryCommandErrorCode ErrorCode { get; }
        public string Message { get; }
        public InventoryValidationResult Validation { get; }

        private InventoryCommandResult(
            bool succeeded,
            InventoryCommandErrorCode errorCode,
            string message,
            InventoryValidationResult validation = null)
        {
            Succeeded = succeeded;
            ErrorCode = errorCode;
            Message = message ?? string.Empty;
            Validation = validation;
        }

        public static InventoryCommandResult Success(string message = "")
        {
            return new InventoryCommandResult(true, InventoryCommandErrorCode.None, message);
        }

        public static InventoryCommandResult ValidationFailure(InventoryValidationResult validation)
        {
            return new InventoryCommandResult(
                false,
                InventoryCommandErrorCode.ValidationFailed,
                validation?.Message ?? "Validation failed.",
                validation);
        }

        public static InventoryCommandResult Failure(InventoryCommandErrorCode code, string message)
        {
            return new InventoryCommandResult(false, code, message);
        }
    }
}
InventoryContext.cs
namespace CavesOfOoo.Core.Inventory
{
    public sealed class InventoryContext
    {
        public Entity Actor { get; }
        public Zone Zone { get; }
        public InventoryPart Inventory { get; }
        public Body Body { get; }

        public InventoryContext(Entity actor, Zone zone = null)
        {
            Actor = actor;
            Zone = zone;
            Inventory = actor?.GetPart<InventoryPart>();
            Body = actor?.GetPart<Body>();
        }
    }
}
IInventoryCommand.cs
namespace CavesOfOoo.Core.Inventory
{
    public interface IInventoryCommand
    {
        string Name { get; }

        InventoryValidationResult Validate(InventoryContext context);

        InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction);
    }
}
InventoryTransaction.cs
using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core.Inventory
{
    public sealed class InventoryTransaction
    {
        private readonly List<TransactionStep> _steps = new List<TransactionStep>();
        private bool _closed;

        public bool IsClosed => _closed;

        public void Do(Action apply, Action undo)
        {
            if (_closed)
                throw new InvalidOperationException("Cannot add steps to a closed transaction.");

            apply?.Invoke();
            _steps.Add(new TransactionStep(undo));
        }

        public void Commit()
        {
            _steps.Clear();
            _closed = true;
        }

        public void Rollback()
        {
            if (_closed)
                return;

            for (int i = _steps.Count - 1; i >= 0; i--)
            {
                _steps[i].Undo?.Invoke();
            }

            _steps.Clear();
            _closed = true;
        }

        private sealed class TransactionStep
        {
            public Action Undo { get; }

            public TransactionStep(Action undo)
            {
                Undo = undo;
            }
        }
    }
}




Here are the next 5 skeletons, designed to compile with the first 5 files I gave you.

InventoryCommandExecutor.cs
using System;

namespace CavesOfOoo.Core.Inventory
{
    public sealed class InventoryCommandExecutor
    {
        public InventoryCommandResult Execute(IInventoryCommand command, InventoryContext context)
        {
            if (command == null)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ValidationFailed, "Command is null.");

            if (context == null)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ValidationFailed, "Context is null.");

            var validation = command.Validate(context);
            if (validation == null || !validation.IsValid)
                return InventoryCommandResult.ValidationFailure(validation);

            var transaction = new InventoryTransaction();

            try
            {
                var result = command.Execute(context, transaction)
                    ?? InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Command returned null result.");

                if (result.Succeeded)
                {
                    transaction.Commit();
                    return result;
                }

                transaction.Rollback();
                return result;
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                    return InventoryCommandResult.Failure(
                        InventoryCommandErrorCode.ExecutionFailed,
                        $"Command '{command.Name}' threw: {ex.Message}");
                }
                catch (Exception rollbackEx)
                {
                    return InventoryCommandResult.Failure(
                        InventoryCommandErrorCode.RollbackFailed,
                        $"Command '{command.Name}' failed and rollback also failed: {rollbackEx.Message}");
                }
            }
        }
    }
}
InventoryDisplacement.cs
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Planning
{
    public sealed class InventoryDisplacement
    {
        public Entity Item { get; set; }
        public BodyPart BodyPart { get; set; }

        public InventoryDisplacement() { }

        public InventoryDisplacement(Entity item, BodyPart bodyPart)
        {
            Item = item;
            BodyPart = bodyPart;
        }
    }
}
EquipPlan.cs
using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Planning
{
    public sealed class EquipPlan
    {
        public Entity Actor { get; internal set; }
        public Entity Item { get; internal set; }
        public BodyPart TargetPart { get; internal set; }

        public List<BodyPart> OccupiedParts { get; } = new List<BodyPart>();
        public List<InventoryDisplacement> Displacements { get; } = new List<InventoryDisplacement>();

        public bool IsValid { get; internal set; } = true;
        public string FailureReason { get; internal set; } = string.Empty;

        public static EquipPlan Invalid(Entity actor, Entity item, BodyPart targetPart, string reason)
        {
            return new EquipPlan
            {
                Actor = actor,
                Item = item,
                TargetPart = targetPart,
                IsValid = false,
                FailureReason = reason ?? "Invalid equip plan."
            };
        }
    }
}
EquipPlanBuilder.cs
using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Planning
{
    public sealed class EquipPlanBuilder
    {
        private readonly Entity _actor;
        private readonly Entity _item;
        private readonly BodyPart _targetPart;

        private readonly List<BodyPart> _occupiedParts = new List<BodyPart>();
        private readonly List<InventoryDisplacement> _displacements = new List<InventoryDisplacement>();

        private bool _valid = true;
        private string _failureReason = string.Empty;

        public Entity Actor => _actor;
        public Entity Item => _item;
        public BodyPart TargetPart => _targetPart;

        public IReadOnlyList<BodyPart> OccupiedParts => _occupiedParts;
        public IReadOnlyList<InventoryDisplacement> Displacements => _displacements;

        public bool IsValid => _valid;
        public string FailureReason => _failureReason;

        public EquipPlanBuilder(Entity actor, Entity item, BodyPart targetPart = null)
        {
            _actor = actor;
            _item = item;
            _targetPart = targetPart;
        }

        public void Claim(BodyPart part)
        {
            if (part == null) return;
            if (!_occupiedParts.Contains(part))
                _occupiedParts.Add(part);
        }

        public void AddDisplacement(Entity item, BodyPart bodyPart)
        {
            if (item == null || bodyPart == null) return;
            _displacements.Add(new InventoryDisplacement(item, bodyPart));
        }

        public void Fail(string reason)
        {
            _valid = false;
            _failureReason = reason ?? "Equip plan failed.";
        }

        public EquipPlan Build()
        {
            var plan = new EquipPlan
            {
                Actor = _actor,
                Item = _item,
                TargetPart = _targetPart,
                IsValid = _valid,
                FailureReason = _failureReason
            };

            plan.OccupiedParts.AddRange(_occupiedParts);
            plan.Displacements.AddRange(_displacements);
            return plan;
        }
    }
}
IEquipRule.cs
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    public interface IEquipRule
    {
        string Name { get; }

        InventoryValidationResult Validate(EquipPlanBuilder builder, InventoryContext context);
    }
}



Here are the next 5 skeletons.

Note: EquipRuleResult is optional utility right now because your IEquipRule currently returns InventoryValidationResult.

EquipRuleResult.cs
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core.Inventory.Rules
{
    public sealed class EquipRuleResult
    {
        public bool Passed { get; }
        public InventoryValidationErrorCode ErrorCode { get; }
        public string Message { get; }

        private EquipRuleResult(bool passed, InventoryValidationErrorCode errorCode, string message)
        {
            Passed = passed;
            ErrorCode = errorCode;
            Message = message ?? string.Empty;
        }

        public static EquipRuleResult Pass()
        {
            return new EquipRuleResult(true, InventoryValidationErrorCode.None, string.Empty);
        }

        public static EquipRuleResult Fail(InventoryValidationErrorCode errorCode, string message)
        {
            return new EquipRuleResult(false, errorCode, message);
        }

        public InventoryValidationResult ToValidationResult()
        {
            return Passed
                ? InventoryValidationResult.Valid()
                : InventoryValidationResult.Invalid(ErrorCode, Message);
        }
    }
}
EquipPlanner.cs
using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Rules;

namespace CavesOfOoo.Core.Inventory.Planning
{
    public sealed class EquipPlanner
    {
        private readonly List<IEquipRule> _rules = new List<IEquipRule>();

        public EquipPlanner(IEnumerable<IEquipRule> rules = null)
        {
            if (rules == null) return;
            foreach (var rule in rules)
            {
                if (rule != null)
                    _rules.Add(rule);
            }
        }

        public EquipPlan Build(Entity actor, Entity item, BodyPart targetPart = null)
        {
            var builder = new EquipPlanBuilder(actor, item, targetPart);

            if (actor == null)
            {
                builder.Fail("No actor provided.");
                return builder.Build();
            }

            if (item == null)
            {
                builder.Fail("No item provided.");
                return builder.Build();
            }

            var equippable = item.GetPart<EquippablePart>();
            if (equippable == null
Reconnecting... 1/5
Worked for 1m 5s
Perfect. Here are the next 5 compile-safe skeletons.

EquipRuleResult.cs
namespace CavesOfOoo.Core.Inventory.Rules
{
    public sealed class EquipRuleResult
    {
        public bool Passed { get; }
        public InventoryValidationErrorCode ErrorCode { get; }
        public string Message { get; }

        private EquipRuleResult(bool passed, InventoryValidationErrorCode errorCode, string message)
        {
            Passed = passed;
            ErrorCode = errorCode;
            Message = message ?? string.Empty;
        }

        public static EquipRuleResult Pass()
        {
            return new EquipRuleResult(true, InventoryValidationErrorCode.None, string.Empty);
        }

        public static EquipRuleResult Fail(InventoryValidationErrorCode code, string message)
        {
            return new EquipRuleResult(false, code, message);
        }

        public InventoryValidationResult ToValidation()
        {
            return Passed
                ? InventoryValidationResult.Valid()
                : InventoryValidationResult.Invalid(ErrorCode, Message);
        }
    }
}
EquipPlanner.cs
using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Rules;

namespace CavesOfOoo.Core.Inventory.Planning
{
    public sealed class EquipPlanner
    {
        private readonly List<IEquipRule> _rules = new List<IEquipRule>();

        public EquipPlanner() { }

        public EquipPlanner(IEnumerable<IEquipRule> rules)
        {
            if (rules == null) return;
            foreach (var rule in rules)
                if (rule != null)
                    _rules.Add(rule);
        }

        public EquipPlan Build(InventoryContext context, Entity item, BodyPart targetPart = null)
        {
            if (context?.Actor == null)
                return EquipPlan.Invalid(context?.Actor, item, targetPart, "Actor is missing.");

            if (item == null)
                return EquipPlan.Invalid(context.Actor, item, targetPart, "Item is missing.");

            if (context.Inventory == null)
                return EquipPlan.Invalid(context.Actor, item, targetPart, "InventoryPart is missing.");

            var equippable = item.GetPart<EquippablePart>();
            if (equippable == null)
                return EquipPlan.Invalid(context.Actor, item, targetPart, "Item is not equippable.");

            var builder = new EquipPlanBuilder(context.Actor, item, targetPart);

            // Base rule pass
            for (int i = 0; i < _rules.Count; i++)
            {
                var validation = _rules[i].Validate(builder, context);
                if (validation != null && !validation.IsValid)
                {
                    builder.Fail(validation.Message);
                    return builder.Build();
                }
            }

            // Temporary bridge: reuse existing displacement logic until rules fully own planning.
            var displacements = InventorySystem.PreviewDisplacements(context.Actor, item, targetPart);
            for (int i = 0; i < displacements.Count; i++)
            {
                builder.AddDisplacement(displacements[i].Item, displacements[i].BodyPart);
            }

            // Ensure explicitly targeted slot is claimed in the plan.
            if (targetPart != null)
                builder.Claim(targetPart);

            return builder.Build();
        }
    }
}
EquipCommand.cs
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class EquipCommand : IInventoryCommand
    {
        private readonly Entity _item;
        private readonly BodyPart _targetPart;
        private readonly EquipPlan _prebuiltPlan;

        public string Name => "Equip";

        public EquipCommand(Entity item, BodyPart targetPart = null)
        {
            _item = item;
            _targetPart = targetPart;
        }

        public EquipCommand(EquipPlan plan)
        {
            _prebuiltPlan = plan;
            _item = plan?.Item;
            _targetPart = plan?.TargetPart;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");
            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");
            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");
            if (_item.GetPart<EquippablePart>() == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingEquippablePart, "Item is not equippable.");

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var plan = _prebuiltPlan;
            if (plan != null && !plan.IsValid)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ValidationFailed, plan.FailureReason);

            bool success = InventorySystem.Equip(context.Actor, _item, _targetPart);
            if (!success)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Equip failed.");

            return InventoryCommandResult.Success("Equipped.");
        }
    }
}
PickupCommand.cs
namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class PickupCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "Pickup";

        public PickupCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");
            if (context.Zone == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidZone, "Zone is null.");
            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");
            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");

            var physics = _item.GetPart<PhysicsPart>();
            if (physics == null || !physics.Takeable)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.NotTakeable, "Item is not takeable.");

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.Pickup(context.Actor, _item, context.Zone);
            if (!success)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Pickup failed.");

            return InventoryCommandResult.Success("Picked up.");
        }
    }
}
DropCommand.cs
namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class DropCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "Drop";

        public DropCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");
            if (context.Zone == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidZone, "Zone is null.");
            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");
            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");
            if (!context.Inventory.Contains(_item))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.NotOwned, "Item is not in actor inventory/equipment.");

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.Drop(context.Actor, _item, context.Zone);
            if (!success)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Drop failed.");

            return InventoryCommandResult.Success("Dropped.");
        }
    }
}




I’ll give the next set (DropPartialCommand, UnequipCommand, AutoEquipCommand, TakeFromContainerCommand, PutInContainerCommand) next.

DropPartialCommand.cs
namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class DropPartialCommand : IInventoryCommand
    {
        private readonly Entity _item;
        private readonly int _count;

        public string Name => "DropPartial";

        public DropPartialCommand(Entity item, int count)
        {
            _item = item;
            _count = count;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");
            if (context.Zone == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidZone, "Zone is null.");
            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");
            if (_count <= 0)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, "Drop count must be > 0.");
            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");
            if (!context.Inventory.Contains(_item))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.NotOwned, "Item is not in actor inventory/equipment.");

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.DropPartial(context.Actor, _item, _count, context.Zone);
            if (!success)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Drop partial failed.");

            return InventoryCommandResult.Success("Dropped partial stack.");
        }
    }
}
UnequipCommand.cs
namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class UnequipCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "Unequip";

        public UnequipCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");
            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");
            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");
            if (!InventorySystem.IsEquipped(context.Actor, _item))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, "Item is not equipped.");

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.UnequipItem(context.Actor, _item);
            if (!success)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Unequip failed.");

            return InventoryCommandResult.Success("Unequipped.");
        }
    }
}
AutoEquipCommand.cs
namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class AutoEquipCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "AutoEquip";

        public AutoEquipCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");
            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");
            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");
            if (_item.GetPart<EquippablePart>() == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingEquippablePart, "Item is not equippable.");
            if (!context.Inventory.Contains(_item))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.NotOwned, "Item not in inventory.");

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.AutoEquip(context.Actor, _item);
            if (!success)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Auto-equip failed.");

            return InventoryCommandResult.Success("Auto-equipped.");
        }
    }
}
TakeFromContainerCommand.cs
namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class TakeFromContainerCommand : IInventoryCommand
    {
        private readonly Entity _container;
        private readonly Entity _item;

        public string Name => "TakeFromContainer";

        public TakeFromContainerCommand(Entity container, Entity item)
        {
            _container = container;
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");
            if (_container == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Container is null.");
            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");
            if (_container.GetPart<ContainerPart>() == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, "Target is not a container.");
            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.TakeFromContainer(context.Actor, _container, _item);
            if (!success)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Take from container failed.");

            return InventoryCommandResult.Success("Took item from container.");
        }
    }
}
PutInContainerCommand.cs
namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class PutInContainerCommand : IInventoryCommand
    {
        private readonly Entity _container;
        private readonly Entity _item;

        public string Name => "PutInContainer";

        public PutInContainerCommand(Entity container, Entity item)
        {
            _container = container;
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");
            if (_container == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Container is null.");
            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");
            if (_container.GetPart<ContainerPart>() == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, "Target is not a container.");
            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");
            if (!context.Inventory.Contains(_item))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.NotOwned, "Item is not in actor inventory/equipment.");

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.PutInContainer(context.Actor, _container, _item);
            if (!success)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Put in container failed.");

            return InventoryCommandResult.Success("Put item in container.");
        }
    }
}



 I’ll give PerformInventoryActionCommand plus the first two concrete rules (TargetPartCompatibilityRule, SlotAvailabilityRule) so you can start wiring EquipPlanner immediately.


yes

Add these 3 files next.

PerformInventoryActionCommand.cs
namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class PerformInventoryActionCommand : IInventoryCommand
    {
        private readonly Entity _item;
        private readonly string _command;

        public string Name => "PerformInventoryAction";

        public PerformInventoryActionCommand(Entity item, string command)
        {
            _item = item;
            _command = command;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");
            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");
            if (string.IsNullOrEmpty(_command))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, "Action command is empty.");
            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");
            if (!context.Inventory.Contains(_item))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.NotOwned, "Item is not in actor inventory/equipment.");

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool handled = InventorySystem.PerformAction(context.Actor, _item, _command, context.Zone);
            if (!handled)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Inventory action was not handled.");

            return InventoryCommandResult.Success("Inventory action handled.");
        }
    }
}
TargetPartCompatibilityRule.cs
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    public sealed class TargetPartCompatibilityRule : IEquipRule
    {
        public string Name => "TargetPartCompatibility";

        public InventoryValidationResult Validate(EquipPlanBuilder builder, InventoryContext context)
        {
            if (builder == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.Unknown, "EquipPlanBuilder is null.");

            BodyPart target = builder.TargetPart;
            if (target == null)
                return InventoryValidationResult.Valid();

            if (context?.Body == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingBody, "Target body part provided, but actor has no body.");

            if (target.Abstract)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.NoCompatibleSlot, "Cannot equip to an abstract body part.");

            var equippable = builder.Item?.GetPart<EquippablePart>();
            if (equippable == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingEquippablePart, "Item is not equippable.");

            string[] slots = equippable.GetSlotArray();
            bool compatible = false;

            for (int i = 0; i < slots.Length; i++)
            {
                string slotType = slots[i]?.Trim();
                if (string.IsNullOrEmpty(slotType))
                    continue;

                if (slotType == target.Type)
                {
                    compatible = true;
                    break;
                }
            }

            if (!compatible)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NoCompatibleSlot,
                    $"Item cannot be equipped on {target.GetDisplayName()} ({target.Type}).");
            }

            return InventoryValidationResult.Valid();
        }
    }
}
SlotAvailabilityRule.cs
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    public sealed class SlotAvailabilityRule : IEquipRule
    {
        public string Name => "SlotAvailability";

        public InventoryValidationResult Validate(EquipPlanBuilder builder, InventoryContext context)
        {
            if (builder == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.Unknown, "EquipPlanBuilder is null.");

            var equippable = builder.Item?.GetPart<EquippablePart>();
            if (equippable == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingEquippablePart, "Item is not equippable.");

            // Legacy actors (no Body part) still use slot-string equip path.
            if (context?.Body == null)
                return InventoryValidationResult.Valid();

            string[] slots = equippable.GetSlotArray();
            var neededByType = new Dictionary<string, int>();

            for (int i = 0; i < slots.Length; i++)
            {
                string slotType = slots[i]?.Trim();
                if (string.IsNullOrEmpty(slotType))
                    continue;

                if (!neededByType.ContainsKey(slotType))
                    neededByType[slotType] = 0;

                neededByType[slotType]++;
            }

            // If caller specified a target part, treat it as satisfying one required slot of that type.
            if (builder.TargetPart != null)
            {
                string targetType = builder.TargetPart.Type;
                if (neededByType.TryGetValue(targetType, out int needed))
                {
                    needed--;
                    if (needed <= 0)
                        neededByType.Remove(targetType);
                    else
                        neededByType[targetType] = needed;
                }
            }

            foreach (var kvp in neededByType)
            {
                string slotType = kvp.Key;
                int required = kvp.Value;

                var candidates = context.Body.GetEquippableSlots(slotType);
                int available = candidates?.Count ?? 0;

                if (available < required)
                {
                    return InventoryValidationResult.Invalid(
                        InventoryValidationErrorCode.NoCompatibleSlot,
                        $"Need {required} {slotType} slot(s), but only {available} exist.");
                }
            }

            return InventoryValidationResult.Valid();
        }
    }
}


I’ll give SlotCountRule, DisplacementRule, and a first practical EquipCommand update that consumes EquipPlan directly.


yes

Perfect. Add these next.

SlotCountRule.cs
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    public sealed class SlotCountRule : IEquipRule
    {
        public string Name => "SlotCount";

        public InventoryValidationResult Validate(EquipPlanBuilder builder, InventoryContext context)
        {
            if (builder == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.Unknown, "EquipPlanBuilder is null.");

            var equippable = builder.Item?.GetPart<EquippablePart>();
            if (equippable == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingEquippablePart, "Item is not equippable.");

            if (context?.Body == null)
                return InventoryValidationResult.Valid(); // legacy path

            string[] slots = equippable.GetSlotArray();
            var requiredByType = new Dictionary<string, int>();

            for (int i = 0; i < slots.Length; i++)
            {
                string slotType = slots[i]?.Trim();
                if (string.IsNullOrEmpty(slotType))
                    continue;

                if (!requiredByType.ContainsKey(slotType))
                    requiredByType[slotType] = 0;

                requiredByType[slotType]++;
            }

            if (requiredByType.Count == 0)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.NoCompatibleSlot, "No valid slot requirements.");

            // If target part is selected, it consumes one slot of that type.
            if (builder.TargetPart != null)
            {
                string targetType = builder.TargetPart.Type;
                if (!requiredByType.ContainsKey(targetType))
                {
                    return InventoryValidationResult.Invalid(
                        InventoryValidationErrorCode.NoCompatibleSlot,
                        $"Target part type '{targetType}' is not required by item.");
                }

                requiredByType[targetType]--;
                builder.Claim(builder.TargetPart);
            }

            // Reduce requirements by already-claimed parts (if any).
            for (int i = 0; i < builder.OccupiedParts.Count; i++)
            {
                var claimed = builder.OccupiedParts[i];
                if (claimed == null) continue;
                if (!requiredByType.ContainsKey(claimed.Type)) continue;

                if (requiredByType[claimed.Type] > 0)
                    requiredByType[claimed.Type]--;
            }

            // Ensure body has enough slots by type for remaining requirements.
            foreach (var kvp in requiredByType)
            {
                string type = kvp.Key;
                int remaining = kvp.Value;
                if (remaining <= 0) continue;

                int available = context.Body.GetEquippableSlots(type)?.Count ?? 0;
                if (available < remaining)
                {
                    return InventoryValidationResult.Invalid(
                        InventoryValidationErrorCode.NoCompatibleSlot,
                        $"Need {remaining} more '{type}' slot(s), only {available} available.");
                }
            }

            return InventoryValidationResult.Valid();
        }
    }
}
DisplacementRule.cs
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    public sealed class DisplacementRule : IEquipRule
    {
        public string Name => "Displacement";

        public InventoryValidationResult Validate(EquipPlanBuilder builder, InventoryContext context)
        {
            if (builder == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.Unknown, "EquipPlanBuilder is null.");

            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");

            if (builder.Item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");

            // Bridge to existing proven logic until planner fully owns slot search.
            var displacements = InventorySystem.PreviewDisplacements(
                context.Actor,
                builder.Item,
                builder.TargetPart);

            for (int i = 0; i < displacements.Count; i++)
            {
                builder.AddDisplacement(displacements[i].Item, displacements[i].BodyPart);
            }

            return InventoryValidationResult.Valid();
        }
    }
}
Replace EquipCommand.cs with this updated version:
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class EquipCommand : IInventoryCommand
    {
        private readonly Entity _item;
        private readonly BodyPart _targetPart;
        private readonly EquipPlan _prebuiltPlan;
        private readonly EquipPlanner _planner;

        public string Name => "Equip";

        public EquipCommand(Entity item, BodyPart targetPart = null, EquipPlanner planner = null)
        {
            _item = item;
            _targetPart = targetPart;
            _planner = planner ?? new EquipPlanner();
        }

        public EquipCommand(EquipPlan plan, EquipPlanner planner = null)
        {
            _prebuiltPlan = plan;
            _item = plan?.Item;
            _targetPart = plan?.TargetPart;
            _planner = planner ?? new EquipPlanner();
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Actor is null.");

            if (_item == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "Item is null.");

            if (context.Inventory == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "InventoryPart missing.");

            if (_item.GetPart<EquippablePart>() == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingEquippablePart, "Item is not equippable.");

            // If caller gave a plan, validate plan state too.
            if (_prebuiltPlan != null && !_prebuiltPlan.IsValid)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    _prebuiltPlan.FailureReason);
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var plan = _prebuiltPlan ?? _planner.Build(context, _item, _targetPart);
            if (plan == null)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Planner returned null plan.");

            if (!plan.IsValid)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ValidationFailed, plan.FailureReason);

            // Bridge execution to existing equip flow for now.
            bool success = InventorySystem.Equip(context.Actor, plan.Item, plan.TargetPart);
            if (!success)
                return InventoryCommandResult.Failure(InventoryCommandErrorCode.ExecutionFailed, "Equip failed.");

            return InventoryCommandResult.Success("Equipped.");
        }
    }
}
