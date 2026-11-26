using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A finite source inside a solid container. This is deliberately
    /// not a ground pool: no floor projection, automatic contact or regeneration.</summary>
    public sealed class ContainedLiquidSourcePart : Part, IDescribeState
    {
        // ── reader contract (Docs/MACHINE-READERS.md) ──
        public ReaderScope Scope => ReaderScope.Quick;
        public int Order => 50;
        public string DescribeState(DescribeContext context) => Describe();

        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Entity, object> RecoveryCustody = new System.Runtime.CompilerServices.ConditionalWeakTable<Entity, object>();
        internal static bool IsRecoveryCustodyProtected(Entity item) => item != null && RecoveryCustody.TryGetValue(item, out _);
        // Normal destination merging completes before callbacks. Until its outer
        // receipt closes, those exact units cannot be merged into a second stack.
        internal static void GuardRecoveryCustody(Entity item, InventoryTransaction tx)
        {
            if(item==null || IsRecoveryCustodyProtected(item))return;
            RecoveryCustody.Add(item,new object());
            tx.Do(null,()=>RecoveryCustody.Remove(item));
            tx.AfterCommit(()=>RecoveryCustody.Remove(item));
        }
        public override string Name => "ContainedLiquidSource";
        public string LiquidId = "oil";
        public int Volume = 12;
        public int Capacity = 12;
        public const string RecoveryPrefix = "RecoverThroughLiquid|";
        public static bool IsRecovery(string command) => command?.StartsWith(RecoveryPrefix, StringComparison.Ordinal) == true;
        public bool AllowsCleanAccess => ValidPayload() && Volume == 0;
        bool ValidPayload() => ParentEntity != null && ParentEntity.GetPart<ContainedLiquidSourcePart>() == this
            && ParentEntity.Parts.Count(p => p is ContainedLiquidSourcePart && p.ParentEntity == ParentEntity) == 1
            && Capacity > 0 && Volume >= 0 && Volume <= Capacity && LiquidRegistry.IsInitialized && LiquidRegistry.Get(LiquidId) != null
            && !ParentEntity.HasPart<LiquidPoolPart>() && !ParentEntity.HasPart<LiquidVesselPart>();
        internal bool Context(Entity actor, Zone zone)
        {
            var source = ParentEntity; var physics = source?.GetPart<PhysicsPart>(); var container = source?.GetPart<ContainerPart>();
            return ValidPayload() && PreparationActions.CurrentActor(actor, zone)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && WorldResourceActions.Ground(source, zone) && physics?.ParentEntity == source && !physics.Takeable && physics.Solid
                && container?.ParentEntity == source && !container.IsLocked && source.Parts.Count(p => p is ContainerPart) == 1
                && source.Parts.Count(p => p is PhysicsPart) == 1 && !source.HasTag("Creature")
                && source.GetPart<RenderPart>()?.Visible == true && source.ID != actor.ID
                && zone.GetEntityCell(source) is Cell cell && cell.IsVisible && cell.Explored
                && zone.GetReadOnlyEntities().Count(e => e == source || e.ID == source.ID) == 1
                && SpatialQuery.Distance(zone, actor, source) <= 1;
        }
        internal bool CanFill(Entity actor, Zone zone, LiquidVesselPart receiver) => Context(actor, zone) && Volume > 0
            && receiver != null && receiver.Volume < receiver.Capacity && (receiver.Volume == 0 || receiver.LiquidId == LiquidId);

        public string Describe()
        {
            string liquid = LiquidRegistry.Get(LiquidId)?.DisplayName ?? LiquidId;
            string state = "Contained " + liquid + ": " + Volume + "/" + Capacity + " units. Draw with a compatible carried flask; no refilling or deposits.";
            if (Volume > 0 && CinderholdOilRecovery.Payload(ParentEntity) && !SecondExplorationActions.Special(ParentEntity))
                state += " A hand pump can move the oil to clear floor within hose reach.";
            return state + (Volume == 0 ? " The tray is drained; its remaining equipment can be taken cleanly."
                : " Drain it for clean recovery, or explicitly reach through the liquid in one action. Oil exposure is brief and can worsen heat injury while present; it does not ignite you by itself. Recovery leaves the liquid here.");
        }
        // This unsaved permission belongs to one explicit selection and one outer
        // receipt. Normal Take/TakeAll cannot mint it or keep it after a transfer.
        internal sealed class RecoveryPermission
        {
            internal Entity Actor, Source, Item;
            internal InventoryTransaction Transaction;
            internal int Volume;
            internal string LiquidId;
        }
        internal bool CanTake(Entity actor, Entity item, Zone zone, RecoveryPermission permission = null, InventoryTransaction tx = null)
        {
            var container = ParentEntity?.GetPart<ContainerPart>(); var p = item?.GetPart<PhysicsPart>();
            if (!Context(actor, zone) || item == null || string.IsNullOrEmpty(item.ID) || item.ID == actor.ID || item.ID == ParentEntity.ID
                || p?.ParentEntity != item || !p.Takeable || p.InInventory != ParentEntity || p.Equipped != null || item.SpatialZone != null
                || (item.GetPart<StackerPart>()?.StackCount ?? 1) != 1 || container.Contents.Count(e => e == item || e?.ID == item.ID) != 1
                || !container.Contents.Contains(item) || actor.GetPart<InventoryPart>().Objects.Any(e => e == item || e.ID == item.ID)) return false;
            return Volume == 0 || permission != null && permission.Actor == actor && permission.Source == ParentEntity
                && permission.Item == item && permission.Transaction == tx && tx != null && !tx.IsCommitted && !tx.IsRolledBack
                && permission.Volume == Volume && permission.LiquidId == LiquidId;
        }
        internal bool PrepareTake(Entity actor, Entity item, Zone zone, InventoryTransaction tx, RecoveryPermission permission, out Action cleanup, out Action guard)
        {
            cleanup = null; guard = null;
            if (!CanTake(actor, item, zone, permission, tx) || !tx.TryClaim(actor, actor, "ContainedRecovery")
                || !tx.TryClaim(ParentEntity, actor, "ContainedRecovery") || !tx.TryClaim(item, actor, "ContainedRecovery")) return false;
            var source = ParentEntity; var pack = actor.GetPart<InventoryPart>(); var container = source.GetPart<ContainerPart>();
            var sourceParts = source.Parts.ToArray(); var itemParts = item.Parts.ToArray(); var physics = item.GetPart<PhysicsPart>();
            var itemStack = item.GetPart<StackerPart>();
            var recipient = itemStack == null ? item : pack.Objects.FirstOrDefault(o => o.GetPart<StackerPart>() is StackerPart stack
                && stack.StackCount < stack.MaxStack && stack.CanStackWith(item)) ?? item;
            var recipientStack = recipient.GetPart<StackerPart>(); int recipientBefore = recipient == item ? 0 : recipientStack.StackCount;
            var recipientParts = recipient.Parts.ToArray(); string recipientId = recipient.ID;
            if (recipient != item && !WorldResourceActions.Carried(actor, recipient, false) || !tx.TryClaim(recipient, actor, "ContainedRecovery")) return false;
            cleanup = () => { DetachUnexpectedCustody(item, actor); if(recipient != item) DetachUnexpectedCustody(recipient, actor); };
            // Guard installation is deferred until the ordinary transfer has
            // merged its one unit. The command calls it before Taken callbacks.
            guard = () => { GuardRecoveryCustody(item, tx); GuardRecoveryCustody(recipient, tx); };
            var actorCell = zone.GetEntityCell(actor); var sourceCell = zone.GetEntityCell(source);
            string sourceId = source.ID, actorId = actor.ID, itemId = item.ID, liquid = LiquidId; int amount = Volume, capacity = Capacity;
            tx.BeforeCommit(() => ParentEntity == source && Context(actor, zone) && source.ID == sourceId && actor.ID == actorId && item.ID == itemId
                && source.Parts.SequenceEqual(sourceParts) && item.Parts.SequenceEqual(itemParts)
                && item.Parts.All(p => p.ParentEntity == item) && actor.GetPart<InventoryPart>() == pack
                && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(source) == sourceCell
                && LiquidId == liquid && Volume == amount && Capacity == capacity && !container.Contents.Contains(item)
                && container.Contents.All(e => e?.ID != itemId) && recipient.ID == recipientId && recipient.Parts.SequenceEqual(recipientParts)
                && pack.Objects.Count(e => e == recipient || e.ID == recipientId) == 1 && WorldResourceActions.Carried(actor, recipient, false)
                && (recipientStack?.StackCount ?? 1) == recipientBefore + 1
                && physics.Equipped == null && item.SpatialZone == null
                && (recipient == item ? physics.InInventory == actor : physics.InInventory == null && itemStack.StackCount == 0 && !pack.Objects.Contains(item))
                && !pack.EquippedItems.ContainsValue(item) && pack.FindEquippedBodyPart(item) == null);
            if (amount > 0)
                tx.AfterCommit(() =>
                {
                    if (PreparationActions.CurrentActor(actor, zone))
                        actor.ApplyEffect(new LiquidCoveredEffect(liquid, Math.Min(12, amount)), source, zone);
                    Diag.Record("liquid", "ContainedWetRecovery", actor, item, new { source = sourceId, liquid, exposure = Math.Min(12, amount), remaining = amount });
                });
            return true;
        }
        // Scalar/list receipts do not own foreign custody introduced by a
        // callback. Detach only this participating owner before those receipts
        // restore its original location; never clear unrelated inventory items.
        internal static void DetachUnexpectedCustody(Entity item, Entity expectedCarrier)
        {
            item.SpatialZone?.RemoveEntity(item);
            var physics = item.GetPart<PhysicsPart>();
            var carrier = physics?.InInventory;
            if (carrier != null && carrier != expectedCarrier)
            {
                carrier.GetPart<InventoryPart>()?.RemoveObject(item);
                carrier.GetPart<ContainerPart>()?.RemoveItem(item);
            }
            var wearer = physics?.Equipped;
            if (wearer != null)
            {
                var inventory = wearer.GetPart<InventoryPart>();
                if (inventory != null)
                    foreach (var slot in inventory.EquippedItems.Where(pair => pair.Value == item).Select(pair => pair.Key).ToArray())
                        inventory.EquippedItems.Remove(slot);
                var body = wearer.GetPart<Body>();
                if (body != null) foreach (var limb in body.GetParts()) if (limb._Equipped == item) limb.ClearEquipped();
                physics.Equipped = null;
            }
        }
        string RecoveryQuote(Entity item) => RecoveryPrefix + Uri.EscapeDataString(item.ID) + "|" + Uri.EscapeDataString(ParentEntity.ID) + "|" + Uri.EscapeDataString(LiquidId) + "|" + Volume.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var zone = PreparationActions.ZoneFor(e, actor);
            if (e.ID == "GetInventoryActions")
            {
                if (Volume > 0 && Context(actor, zone))
                    foreach (var item in ParentEntity.GetPart<ContainerPart>().Contents)
                    {
                        // Selection is explicit and the execution path rechecks
                        // exact identity and current container custody.
                        if (item?.GetPart<PhysicsPart>()?.Takeable == true)
                            e.GetParameter<InventoryActionList>("Actions")?.AddAction("RecoverThroughLiquid",
                                "recover " + item.GetDisplayName() + " through " + (LiquidRegistry.Get(LiquidId)?.DisplayName ?? LiquidId) + " (exposure " + Math.Min(12,Volume) + ")", RecoveryQuote(item), 'r', 20);
                    }
                return true;
            }
            string command = e.GetStringParameter("Command");
            if (e.ID != "InventoryAction" || !IsRecovery(command)) return true;
            var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            bool Reject() => PreparationActions.Reject(actor, ParentEntity, command, "contained_recovery_unavailable");
            if (tx == null || Volume <= 0 || !Context(actor, zone)) { Reject(); return true; }
            var items = ParentEntity.GetPart<ContainerPart>().Contents.Where(item => command == RecoveryQuote(item)).ToArray();
            if (items.Length != 1) { Reject(); return true; }
            var permission = new RecoveryPermission { Actor = actor, Source = ParentEntity, Item = items[0], Transaction = tx, Volume = Volume, LiquidId = LiquidId };
            var take = new TakeFromContainerCommand(ParentEntity, items[0], permission); var context = new InventoryContext(actor, zone);
            if (!take.Validate(context).IsValid || !take.Execute(context, tx).Success) { Reject(); return true; }
            e.Handled = true; return false;
        }
    }
}
