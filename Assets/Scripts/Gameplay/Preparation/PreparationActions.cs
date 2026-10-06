using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Ordinary finite preparation actions. No skill grants, bits, or hidden stock.</summary>
    public static class PreparationActions
    {
        public static bool IsCommand(string command) => command == "SalvageForgedWeapon" || command == "HarvestCropSeeds"
            || command != null && (command.StartsWith("PrepareRecipe|", StringComparison.Ordinal)
                || command.StartsWith("RefuelTorch|", StringComparison.Ordinal) || command.StartsWith("InfuseMineral|", StringComparison.Ordinal)
                || command.StartsWith("CompostCrop|", StringComparison.Ordinal) || command.StartsWith("TransplantCrop|", StringComparison.Ordinal));
        internal static Zone ZoneFor(GameEvent e, Entity actor) => e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone;
        internal static bool CurrentActor(Entity actor, Zone zone)
        {
            if (!WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<InventoryPart>()?.ParentEntity != actor) return false;
            var manager = WorldLocationContext.For(zone);
            return (manager == null || manager.CachedZones.TryGetValue(zone.ZoneID, out var current) && current == zone)
                && !actor.HasEffect<StunnedEffect>() && !actor.HasEffect<AsleepByGasEffect>() && !actor.HasEffect<ParalyzedEffect>();
        }
        internal static bool Carried(Entity actor, Entity item) => WorldResourceActions.Carried(actor, item, false)
            && item.HasTag("Item") && !item.HasTag("Creature") && !item.HasTag("Terrain") && !item.HasPart<CropPart>()
            && !item.HasPart<SpatialFootprintPart>() && !item.GetPart<PhysicsPart>().Solid
            && (item.GetPart<StackerPart>() is not StackerPart stack || stack.StackCount <= stack.MaxStack);
        internal static bool Begin(Entity actor, Entity item, Zone zone, InventoryTransaction tx, string command)
        {
            if (tx == null || !CurrentActor(actor, zone) || !Carried(actor, item)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true)
                return Reject(actor, item, command, "unavailable");
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(item, actor, command)) return Reject(actor, item, command, "in_progress");
            return true;
        }
        internal static bool Reject(Entity actor, Entity item, string command, string reason)
        {
            Diag.Record("event", "PreparationRejected", actor: actor, target: item, payload: new { command, reason });
            if (actor?.HasTag("Player") == true) MessageLog.Add("You cannot prepare that here now; nothing is spent.");
            return false;
        }
        internal static void Complete(InventoryTransaction tx, Entity actor, Entity item, string command, string message)
        {
            tx.AfterCommit(() => { Diag.Record("event", "PreparationCompleted", actor: actor, target: item, payload: new { command }); MessageLog.Add(message); });
        }
        internal static string Escaped(Entity item) => Uri.EscapeDataString(item.ID);
        internal static bool Fresh(Entity item, string blueprint) => CropYieldService.Fresh(item, blueprint)
            && item.HasTag("Item") && !item.HasTag("Terrain");
        static bool HasIdentity(IEnumerable<Entity> owners, Entity item)
        { foreach (var owner in owners) if (ReferenceEquals(owner, item) || owner?.ID == item.ID) return true; return false; }

        // Create every output before touching payment. The receipt records actual merge recipients,
        // and the outer action transaction also owns rollback after a downstream observer throws.
        internal static bool Transform(Entity actor, Entity input, Zone zone, EntityFactory factory, InventoryTransaction tx,
            string command, int units, string[] outputs, Func<bool> stillValid, Func<Entity, int, bool> validOutput, string message)
        {
            if (factory == null || units < 1 || outputs == null || outputs.Length < 1 || outputs.Length > 8
                || !stillValid() || !Begin(actor, input, zone, tx, command)) return Reject(actor, input, command, "invalid_recipe_or_source");
            var pack = actor.GetPart<InventoryPart>(); var stack = input.GetPart<StackerPart>(); var physics = input.GetPart<PhysicsPart>();
            int count = stack?.StackCount ?? 1; string id = input.ID, blueprint = input.BlueprintName;
            var anchor = zone.GetEntityCell(actor);
            if (count < units) return Reject(actor, input, command, "not_enough_material");
            var products = new Entity[outputs.Length]; var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < outputs.Length; i++)
            {
                if (!factory.Blueprints.ContainsKey(outputs[i])) return Reject(actor, input, command, "unknown_output");
                products[i] = factory.CreateEntity(outputs[i]);
            }
            if (!CurrentActor(actor, zone) || zone.GetEntityCell(actor) != anchor || !Carried(actor, input)
                || input.ID != id || input.BlueprintName != blueprint || input.GetPart<StackerPart>() != stack
                || input.GetPart<PhysicsPart>() != physics || (stack?.StackCount ?? 1) != count || !stillValid())
                return Reject(actor, input, command, "source_changed");
            for (int i = 0; i < products.Length; i++)
            {
                var product = products[i];
                if (!Fresh(product, outputs[i]) || !validOutput(product, i) || !ids.Add(product.ID) || product.ID == actor.ID
                    || HasIdentity(pack.Objects, product) || HasIdentity(zone.GetReadOnlyEntities(), product)
                    || !tx.TryClaim(product, actor, command)) return Reject(actor, input, command, "invalid_output");
                foreach (var resident in pack.Objects)
                    if (resident?.BlueprintName == outputs[i] && !Carried(actor, resident)) return Reject(actor, input, command, "invalid_destination");
            }
            int ceiling = pack.MaxWeight < 0 ? int.MaxValue : Math.Max(pack.MaxWeight, pack.GetCarriedWeight());
            var receipt = InventoryTransferSnapshot.Capture(pack, products); bool restored = false;
            Action restore = () => { if (!restored) { restored = true; receipt.Restore(); } }; tx.Do(null, restore);
            if (!receipt.Apply(() =>
            {
                for (int i = 0; i < units; i++) if (!pack.TryConsumeOne(input)) return false;
                foreach (var product in products) if (!pack.AddCraftedUnit(product, out _)) return false;
                return pack.GetCarriedWeight() <= ceiling;
            }) || !receipt.ClaimChanges(tx, actor, command))
            { restore(); return Reject(actor, input, command, "capacity_or_transfer"); }
            Complete(tx, actor, input, command, message); return true;
        }
    }
}
