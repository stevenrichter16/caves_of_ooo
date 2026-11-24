using System;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    internal static class SpringTrapRecovery
    {
        const string Prefix = "RecoverSpringTrap|";
        sealed class Token { internal readonly string Value = Guid.NewGuid().ToString("N"); }
        static readonly ConditionalWeakTable<SpringTrapPart, Token> Tokens = new ConditionalWeakTable<SpringTrapPart, Token>();
        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        internal static bool Supported(Entity owner)
        {
            var trap = owner?.GetPart<SpringTrapPart>(); var laid = owner?.GetPart<LaidDevicePart>();
            return owner?.BlueprintName == "SpringTrapLaid" && trap?.ParentEntity == owner && trap.Armed
                && SpringTrapKitPart.CartridgeFor(trap.Kind) != "" && trap.ConsumeOnTrigger && string.IsNullOrEmpty(trap.TriggerFaction)
                && laid?.ParentEntity == owner && laid.ItemBlueprint == "" && laid.Label == "spring trap"
                && owner.Properties.Count == 0 && owner.IntProperties.Count == 0 && owner.Statistics.Count == 0
                && owner.HasTag("Trap") && owner.Tags.Keys.All(k => k == "Trap")
                && owner.Parts.Count(p => p is SpringTrapPart) == 1 && owner.Parts.Count(p => p is PhysicsPart) == 1
                && owner.Parts.Count(p => p is LaidDevicePart) == 1
                && owner.Parts.All(p => p.ParentEntity == owner && (p is SpringTrapPart || p is RenderPart || p is PhysicsPart
                    || p is ExaminablePart || p is LaidDevicePart || p is StatusEffectsPart status && status.EffectCount == 0));
        }
        static bool Eligible(Entity actor, SpringTrapPart trap, Zone zone)
        {
            var owner = trap?.ParentEntity; var cell = owner == null ? null : zone?.GetEntityCell(owner);
            return actor != null && zone != null && Supported(owner) && owner.GetPart<SpringTrapPart>() == trap
                && WorldResourceActions.ActorCurrent(actor, zone) && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && actor.GetPart<InventoryPart>()?.ParentEntity == actor
                && WorldResourceActions.Ground(owner, zone) && WorldResourceActions.Nearby(actor, owner, zone)
                && cell?.IsVisible == true && owner.GetPart<RenderPart>()?.Visible == true;
        }
        static string Quote(Entity actor, SpringTrapPart trap, Zone zone)
        {
            var cell = zone.GetEntityCell(trap.ParentEntity); var origin = zone.GetEntityCell(actor);
            return Prefix + Uri.EscapeDataString(trap.ParentEntity.ID) + "|" + trap.Kind + "|" + Uri.EscapeDataString(zone.ZoneID)
                + "|" + cell.X + "|" + cell.Y + "|" + origin.X + "|" + origin.Y + "|" + Tokens.GetValue(trap, _ => new Token()).Value;
        }
        internal static void AddRows(Entity actor, SpringTrapPart trap, Zone zone, InventoryActionList rows)
        {
            if (rows == null || !Eligible(actor, trap, zone)) return;
            rows.AddActionWithHint("RecoverSpringTrap", "recover the loaded spring trap", Quote(actor, trap, zone),
                "Takes one action and returns one loaded frame. Keep off its cell: if it springs first, the trap is spent.", 'r', 20, false, ActionTime.OneAction);
        }
        internal static bool TryAct(Entity actor, SpringTrapPart trap, Zone zone, string command, InventoryTransaction tx)
        {
            if (tx == null || !IsCommand(command) || !Eligible(actor, trap, zone) || command != Quote(actor, trap, zone)) return Reject(actor, trap?.ParentEntity);
            var owner = trap.ParentEntity; var pack = actor.GetPart<InventoryPart>(); var factory = HarvestablePart.Factory;
            if (factory == null || !tx.TryClaimAction(actor) || !tx.TryClaim(actor, actor, command) || !tx.TryClaim(owner, actor, command)) return Reject(actor, owner);
            var origin = zone.GetEntityCell(actor); var at = zone.GetEntityCell(owner); var physical = owner.GetPart<PhysicsPart>();
            var parts = owner.Parts.ToArray(); string kind = trap.Kind, id = owner.ID;
            bool Original() => Eligible(actor, trap, zone) && owner.ID == id && trap.Kind == kind && owner.Parts.SequenceEqual(parts)
                && owner.GetPart<PhysicsPart>() == physical && zone.GetEntityCell(owner) == at && zone.GetEntityCell(actor) == origin
                && actor.GetPart<InventoryPart>() == pack && HarvestablePart.Factory == factory;
            var kit = factory.CreateEntity("SpringTrap"); var load = kit?.GetPart<SpringTrapKitPart>(); var kp = kit?.GetPart<PhysicsPart>();
            // A factory observer may spring or remove the device independently.
            // Nothing has been removed by this recovery yet, so never restore it.
            if (!Original() || kit?.BlueprintName != "SpringTrap" || load?.ParentEntity != kit || load.Loaded != "" || load.Kind != ""
                || kit.SpatialZone != null || kp?.ParentEntity != kit || !kp.Takeable || kp.InInventory != null || kp.Equipped != null
                || (kit.GetPart<StackerPart>()?.StackCount ?? 1) != 1 || kit.HasTag("Creature") || string.IsNullOrEmpty(kit.ID)
                || pack.Objects.Any(e => e == kit || e?.ID == kit.ID) || zone.GetReadOnlyEntities().Any(e => e.ID == kit.ID)) return Reject(actor, owner);
            load.Loaded = SpringTrapKitPart.CartridgeFor(kind); load.Kind = kind;
            string kitID = kit.ID; var kitParts = kit.Parts.ToArray();
            bool RetractCreatedKit()
            {
                if (kit.ID != kitID || kit.BlueprintName != "SpringTrap" || kit.GetPart<PhysicsPart>() != kp || kp.ParentEntity != kit
                    || kit.GetPart<SpringTrapKitPart>() != load || load.ParentEntity != kit || load.Kind != kind
                    || load.Loaded != SpringTrapKitPart.CartridgeFor(kind) || !kit.Parts.SequenceEqual(kitParts))
                { Diag.Record("furniture", "SpringTrapRecoveryUndoIdentityChanged", actor, kit); return false; }
                if (kit.SpatialZone is Zone ground)
                {
                    if (kp.InInventory != null || kp.Equipped != null || ground.GetEntityCell(kit)?.Objects.Contains(kit) != true
                        || !ground.RemoveEntity(kit)) return false;
                }
                else if (kp.InInventory is Entity carrier)
                {
                    if (kp.Equipped != null) return false;
                    var inventory = carrier.GetPart<InventoryPart>(); var container = carrier.GetPart<ContainerPart>();
                    if (inventory?.Objects.Count(e => e == kit) == 1) { if (!inventory.RemoveObject(kit)) return false; }
                    else if (container?.Contents.Count(e => e == kit) == 1) { if (!container.RemoveItem(kit)) return false; }
                    else return false;
                }
                return kit.SpatialZone == null && kp.InInventory == null && kp.Equipped == null && !pack.Objects.Contains(kit);
            }
            var receipt = InventoryTransferSnapshot.Capture(pack, kit); bool attempted = false;
            tx.Do(null, () => { if (!attempted || RetractCreatedKit()) receipt.Restore(); });
            if (!tx.TryClaim(kit, actor, command)) return Reject(actor, owner);
            attempted = true;
            bool placed = receipt.Apply(() => pack.AddCraftedUnitWithinCapacity(kit, out _));
            if (!placed || !receipt.ClaimChanges(tx, actor, command) || !Original()) return Reject(actor, owner);
            bool removed = false;
            tx.Do(() => { if (!zone.RemoveEntity(owner)) throw new InvalidOperationException("Recovery target left the field."); removed = true; }, () =>
            {
                if (removed && owner.SpatialZone == null && physical.InInventory == null && physical.Equipped == null
                    && owner.ID == id && owner.GetPart<SpringTrapPart>() == trap && owner.GetPart<PhysicsPart>() == physical
                    && trap.Armed && trap.Kind == kind && RetractCreatedKit())
                    zone.AddEntity(owner, at.X, at.Y);
            });
            bool Recovered() => WorldResourceActions.ActorCurrent(actor, zone) && zone.GetEntityCell(actor) == origin
                && actor.GetPart<InventoryPart>() == pack && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && owner.ID == id && owner.SpatialZone == null
                && physical.InInventory == null && physical.Equipped == null && owner.Parts.SequenceEqual(parts)
                && trap.Armed && trap.Kind == kind && WorldResourceActions.Carried(actor, kit, true)
                && kit.GetPart<SpringTrapKitPart>() == load && load.Kind == kind && load.Loaded == SpringTrapKitPart.CartridgeFor(kind)
                && HarvestablePart.Factory == factory;
            tx.BeforeCommit(Recovered);
            tx.AfterCommit(() => ZoneRenderHooks.MarkCellDirty(at.X, at.Y, "SpringTrapRecovered"));
            tx.AfterCommit(() => MessageLog.Add("You recover the loaded spring trap. Its " + SpringTrapKitPart.WordFor(kind) + " stays in the frame."));
            tx.AfterCommit(() => Diag.Record("furniture", "SpringTrapRecovered", actor, kit, new { kind }));
            return true;
        }
        static bool Reject(Entity actor, Entity owner)
        { MessageLog.Add("That spring trap cannot be recovered now. Check its load, position and room in your pack."); Diag.Record("furniture", "SpringTrapRecoveryRefused", actor, owner); return false; }
    }
}
