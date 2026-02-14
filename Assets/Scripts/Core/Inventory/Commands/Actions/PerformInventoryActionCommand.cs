using System.Collections.Generic;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class PerformInventoryActionCommand : IInventoryCommand
    {
        private readonly Entity _item;
        private readonly string _actionCommand;

        public string Name => "PerformInventoryAction";

        public PerformInventoryActionCommand(Entity item, string actionCommand)
        {
            _item = item;
            _actionCommand = actionCommand;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Perform action requires a valid actor.");
            }

            if (_item == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidItem,
                    "Perform action requires a valid item.");
            }

            if (string.IsNullOrEmpty(_actionCommand))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Action command is empty.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var actor = context.Actor;
            var zone = context.Zone;

            var snapshot = CaptureSnapshot(context, _item);
            if (snapshot != null)
            {
                transaction.Do(
                    apply: null,
                    undo: () => RestoreSnapshot(context, snapshot));
            }

            // Fire BeforeInventoryAction on actor (can veto).
            var before = GameEvent.New("BeforeInventoryAction");
            before.SetParameter("Actor", (object)actor);
            before.SetParameter("Item", (object)_item);
            before.SetParameter("Command", _actionCommand);
            if (!actor.FireEvent(before))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    $"Inventory action '{_actionCommand}' was cancelled.");
            }

            // Fire InventoryAction on the item.
            var actionEvent = GameEvent.New("InventoryAction");
            actionEvent.SetParameter("Actor", (object)actor);
            actionEvent.SetParameter("Item", (object)_item);
            actionEvent.SetParameter("Command", _actionCommand);
            if (zone != null)
                actionEvent.SetParameter("Zone", (object)zone);

            bool handled = !_item.FireEvent(actionEvent) || actionEvent.Handled;
            if (!handled)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    $"Inventory action '{_actionCommand}' failed.");
            }

            // Fire AfterInventoryAction on actor.
            var after = GameEvent.New("AfterInventoryAction");
            after.SetParameter("Actor", (object)actor);
            after.SetParameter("Item", (object)_item);
            after.SetParameter("Command", _actionCommand);
            actor.FireEvent(after);

            return InventoryCommandResult.Ok();
        }

        private static ActionSnapshot CaptureSnapshot(InventoryContext context, Entity item)
        {
            if (context?.Actor == null || item == null)
                return null;

            return new ActionSnapshot
            {
                ActorStats = CaptureActorStats(context.Actor),
                ItemState = CaptureItemState(context, item)
            };
        }

        private static Dictionary<string, Stat> CaptureActorStats(Entity actor)
        {
            var snapshot = new Dictionary<string, Stat>();
            if (actor?.Statistics == null)
                return snapshot;

            foreach (var kvp in actor.Statistics)
                snapshot[kvp.Key] = new Stat(kvp.Value);

            return snapshot;
        }

        private static ItemStateSnapshot CaptureItemState(InventoryContext context, Entity item)
        {
            var stacker = item.GetPart<StackerPart>();
            var physics = item.GetPart<PhysicsPart>();
            var inventory = context.Inventory;
            bool carried = inventory != null && inventory.Objects.Contains(item);

            return new ItemStateSnapshot
            {
                Item = item,
                StackCount = stacker?.StackCount,
                WasCarriedByActor = carried,
                CarriedIndex = carried ? inventory.Objects.IndexOf(item) : -1,
                EquippedState = UnequipCommand.CaptureEquippedState(context, item),
                InInventoryOwner = physics?.InInventory,
                EquippedOwner = physics?.Equipped
            };
        }

        private static void RestoreSnapshot(InventoryContext context, ActionSnapshot snapshot)
        {
            if (context?.Actor == null || snapshot == null)
                return;

            RestoreActorStats(context.Actor, snapshot.ActorStats);
            RestoreItemState(context, snapshot.ItemState);
        }

        private static void RestoreActorStats(Entity actor, Dictionary<string, Stat> snapshot)
        {
            if (actor?.Statistics == null || snapshot == null)
                return;

            var removeKeys = new List<string>();
            foreach (var key in actor.Statistics.Keys)
            {
                if (!snapshot.ContainsKey(key))
                    removeKeys.Add(key);
            }

            for (int i = 0; i < removeKeys.Count; i++)
                actor.Statistics.Remove(removeKeys[i]);

            foreach (var kvp in snapshot)
            {
                if (!actor.Statistics.TryGetValue(kvp.Key, out var stat) || stat == null)
                {
                    actor.Statistics[kvp.Key] = new Stat(kvp.Value) { Owner = actor };
                    continue;
                }

                stat.BaseValue = kvp.Value.BaseValue;
                stat.Bonus = kvp.Value.Bonus;
                stat.Penalty = kvp.Value.Penalty;
                stat.Boost = kvp.Value.Boost;
                stat.Min = kvp.Value.Min;
                stat.Max = kvp.Value.Max;
            }
        }

        private static void RestoreItemState(InventoryContext context, ItemStateSnapshot snapshot)
        {
            if (context?.Actor == null || context.Inventory == null || snapshot?.Item == null)
                return;

            var inventory = context.Inventory;
            var item = snapshot.Item;
            var stacker = item.GetPart<StackerPart>();
            var physics = item.GetPart<PhysicsPart>();

            if (stacker != null && snapshot.StackCount.HasValue)
                stacker.StackCount = snapshot.StackCount.Value;

            if (snapshot.WasCarriedByActor)
            {
                var equippedNow = UnequipCommand.CaptureEquippedState(context, item);
                if (equippedNow.HasLocation)
                    UnequipCommand.TryForceUnequip(context, item, equippedNow);

                if (!inventory.Objects.Contains(item))
                {
                    int insertAt = snapshot.CarriedIndex;
                    if (insertAt < 0 || insertAt > inventory.Objects.Count)
                        insertAt = inventory.Objects.Count;
                    inventory.Objects.Insert(insertAt, item);
                }
                else if (snapshot.CarriedIndex >= 0)
                {
                    int currentIndex = inventory.Objects.IndexOf(item);
                    if (currentIndex != snapshot.CarriedIndex)
                    {
                        inventory.Objects.RemoveAt(currentIndex);
                        int insertAt = snapshot.CarriedIndex;
                        if (insertAt < 0 || insertAt > inventory.Objects.Count)
                            insertAt = inventory.Objects.Count;
                        inventory.Objects.Insert(insertAt, item);
                    }
                }

                if (physics != null)
                {
                    physics.InInventory = context.Actor;
                    physics.Equipped = null;
                }

                return;
            }

            if (snapshot.EquippedState != null && snapshot.EquippedState.HasLocation)
            {
                UnequipCommand.TryForceRestore(context, item, snapshot.EquippedState);
                return;
            }

            if (inventory.Objects.Contains(item))
                inventory.RemoveObject(item);

            var equippedNowFinal = UnequipCommand.CaptureEquippedState(context, item);
            if (equippedNowFinal.HasLocation)
                UnequipCommand.TryForceUnequip(context, item, equippedNowFinal);

            if (physics != null)
            {
                physics.InInventory = snapshot.InInventoryOwner;
                physics.Equipped = snapshot.EquippedOwner;
            }
        }

        private sealed class ActionSnapshot
        {
            public Dictionary<string, Stat> ActorStats;
            public ItemStateSnapshot ItemState;
        }

        private sealed class ItemStateSnapshot
        {
            public Entity Item;
            public int? StackCount;
            public bool WasCarriedByActor;
            public int CarriedIndex;
            public UnequipCommand.EquippedStateSnapshot EquippedState;
            public Entity InInventoryOwner;
            public Entity EquippedOwner;
        }
    }
}
