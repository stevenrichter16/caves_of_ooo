using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One carried plant becomes an authored, bounded batch of useful
    /// supplies. The outer inventory command owns consumption and rollback.</summary>
    public sealed class BotanicalProcessingPart : Part
    {
        public override string Name => "BotanicalProcessing";
        public const string Command = "ProcessBotanical";
        public string OutputBlueprint = "";
        public int OutputCount = 1;
        public string ActionText = "prepare";

        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            if (e.ID == "GetInventoryActions")
            {
                if (CurrentInput(actor, actor?.GetPart<InventoryPart>()))
                    e.GetParameter<InventoryActionList>("Actions")?.AddAction(Command, ActionText, Command, 'p', 18);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != Command) return true;
            // Entity dispatch observes Part-list changes. A replacement recipe
            // added by an initializer must not retry the same refused action.
            if (e.GetParameter<bool>("BotanicalProcessingAttempted")) return true;
            e.SetParameter("BotanicalProcessingAttempted", true);
            if (!Process(actor, e.GetParameter<Zone>("Zone"), e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true;
            return false;
        }

        bool Process(Entity actor, Zone zone, InventoryTransaction transaction)
        {
            var raw = ParentEntity;
            var inventory = actor?.GetPart<InventoryPart>();
            var factory = SeedPart.Factory;
            var anchor = zone?.GetEntityCell(actor);
            if (transaction == null || !CurrentInput(actor, inventory) || !CurrentActor(actor, zone, anchor)
                || OutputCount < 1 || OutputCount > 8 || string.IsNullOrEmpty(OutputBlueprint)
                || OutputBlueprint == raw.BlueprintName || factory == null || !factory.Blueprints.ContainsKey(OutputBlueprint))
                return Reject(actor, "unavailable", "You cannot prepare that plant right now.");
            if (!transaction.TryClaim(actor, actor, Command) || !transaction.TryClaim(raw, actor, Command))
                return Reject(actor, "in_progress", "That plant is already being prepared.");

            var stack = raw.GetPart<StackerPart>();
            var physics = raw.GetPart<PhysicsPart>();
            int before = stack?.StackCount ?? 1;
            string into = OutputBlueprint, sourceId = raw.ID;
            int count = OutputCount;
            var products = new Entity[count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++) products[i] = factory.CreateEntity(into);
            // Initializers can call gameplay. Validate the complete batch after the
            // final initializer so a later callback cannot invalidate an earlier unit.
            if (!CurrentInput(actor, inventory) || !CurrentActor(actor, zone, anchor) || SeedPart.Factory != factory
                || stack != raw.GetPart<StackerPart>() || physics != raw.GetPart<PhysicsPart>()
                || (stack?.StackCount ?? 1) != before || raw.ID != sourceId || OutputBlueprint != into || OutputCount != count)
                return Reject(actor, "source_changed", "The plant is no longer available to prepare.");
            foreach (var output in products)
            {
                if (!FreshOutput(output, into) || !ids.Add(output.ID) || output.ID == actor.ID || output.ID == sourceId
                    || HasIdentity(inventory.Objects, output) || HasIdentity(zone.GetReadOnlyEntities(), output)
                    || !transaction.TryClaim(output, actor, Command))
                    return Reject(actor, "invalid_output", "That plant has no usable prepared result.");
            }
            // A corrupt carried alias must not receive a stack merge.
            foreach (var resident in inventory.Objects)
                if (resident?.BlueprintName == into && !Carried(actor, inventory, resident))
                    return Reject(actor, "invalid_destination", "Your supplies cannot receive that result.");

            var receipt = InventoryTransferSnapshot.Capture(inventory, products);
            bool restored = false;
            Action restore = () => { if (!restored) { restored = true; receipt.Restore(); } };
            transaction.Do(null, restore);
            bool applied = receipt.Apply(() =>
            {
                if (!inventory.TryConsumeOne(raw)) return false;
                foreach (var output in products) if (!inventory.AddCraftedUnit(output, out _)) return false;
                return inventory.MaxWeight < 0 || inventory.GetCarriedWeight() <= inventory.MaxWeight;
            });
            if (!applied || !receipt.ClaimChanges(transaction, actor, Command))
            {
                restore();
                return Reject(actor, "capacity_or_transfer", "You cannot carry the prepared supplies; the plant is unchanged.");
            }
            transaction.AfterCommit(() => Diag.Record("event", "BotanicalProcessed", actor, raw,
                new { from = raw.BlueprintName, into, count }));
            transaction.AfterCommit(() => MessageLog.Add("You prepare " + InventoryPart.GetUnitDisplayName(raw) + " into useful supplies."));
            return true;
        }

        bool CurrentInput(Entity actor, InventoryPart inventory) => ParentEntity != null
            && ReferenceEquals(ParentEntity.GetPart<BotanicalProcessingPart>(), this)
            && Carried(actor, inventory, ParentEntity) && Portable(ParentEntity);

        static bool Carried(Entity actor, InventoryPart inventory, Entity item)
        {
            var physics = item?.GetPart<PhysicsPart>(); var stack = item?.GetPart<StackerPart>();
            if (actor == null || inventory == null || inventory.ParentEntity != actor || actor.GetPart<InventoryPart>() != inventory
                || item == null || !inventory.CanConsumeOne(item) || physics == null || physics.ParentEntity != item
                || physics.InInventory != actor || physics.Equipped != null || item.SpatialZone != null
                || (stack != null && (stack.ParentEntity != item || stack.StackCount < 1 || stack.StackCount > stack.MaxStack))
                || inventory.FindEquippedBodyPart(item) != null || inventory.EquippedItems.ContainsValue(item)) return false;
            int matches = 0;
            foreach (var carried in inventory.Objects) if (ReferenceEquals(carried, item) || carried?.ID == item.ID) matches++;
            return !string.IsNullOrEmpty(item.ID) && matches == 1;
        }

        static bool Portable(Entity item) => item.HasTag("Item") && !item.HasTag("Creature") && !item.HasTag("Terrain")
            && !item.HasPart<CropPart>() && item.GetPart<PhysicsPart>() is PhysicsPart physics && physics.Takeable && !physics.Solid;

        static bool FreshOutput(Entity item, string blueprint)
        {
            if (item == null || item.BlueprintName != blueprint || string.IsNullOrEmpty(item.ID) || !Portable(item) || item.SpatialZone != null) return false;
            var physics = item.GetPart<PhysicsPart>(); var stack = item.GetPart<StackerPart>();
            return physics.ParentEntity == item && physics.InInventory == null && physics.Equipped == null
                && (stack == null || (stack.ParentEntity == item && stack.StackCount == 1 && stack.MaxStack >= 1));
        }
        static bool HasIdentity(IEnumerable<Entity> entities, Entity item)
        { foreach (var other in entities) if (ReferenceEquals(other, item) || other?.ID == item.ID) return true; return false; }

        static bool CurrentActor(Entity actor, Zone zone, Cell anchor)
        {
            var effects = actor?.GetPart<StatusEffectsPart>();
            return actor != null && zone != null && anchor != null && actor.SpatialZone == zone
                && ReferenceEquals(anchor, zone.GetEntityCell(actor)) && anchor.Objects.Contains(actor)
                && !CombatSystem.IsDeathHandled(actor) && !(actor.GetStat("Hitpoints") is Stat hp && hp.Value <= 0)
                && effects?.HasEffect<StunnedEffect>() != true && effects?.HasEffect<FrozenEffect>() != true
                && effects?.HasEffect<AsleepByGasEffect>() != true;
        }
        bool Reject(Entity actor, string reason, string message)
        { MessageLog.Add(message); Diag.Record("event", "BotanicalProcessingRejected", actor, ParentEntity, new { reason }); return false; }
    }
}
