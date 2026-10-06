using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Exact-owner predicates and transaction steps for the second exploration packet.
    /// Commands are paid by the normal input caller only after successful dispatch.</summary>
    public static class SecondExplorationActions
    {
        public static EntityFactory Factory;
        static readonly string[] Commands = { "CopyVolume", "ArtisanRepair", "ReturnRental", "BuyRental", "ClaimGuestLocker", "LocksmithOpen", "StepAside", "TreatPatient", "DonateEquipment", "InterCorpse", "RecoverLamp", "RigRopeShortcut", "SalvageJammedTrap", "StripClothScreen", "PermitPassage", "BarterCollector", "BorrowVolume" };
        public static bool IsCommand(string command) => command != null && Commands.Any(c => command == c || command.StartsWith(c + "|", StringComparison.Ordinal));
        internal static EntityFactory Content(Zone zone) => WorldLocationContext.For(zone)?.Factory ?? Factory;
        internal static bool Live(Entity e) => e != null && !CombatSystem.IsDeathHandled(e) && e.GetStatValue("Hitpoints", 1) > 0;
        internal static bool Near(Entity actor, Entity owner, Zone zone) => WorldResourceActions.Nearby(actor, owner, zone)
            && Live(owner) && !(owner.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP <= 0));
        internal static bool Willing(Entity actor, Entity other, Zone zone) => Near(actor, other, zone) && actor != other
            && other.HasTag("Creature") && !other.HasPart<SpatialFootprintPart>()
            && !FactionManager.IsHostile(other, actor) && !FactionManager.IsHostile(actor, other)
            && other.GetPart<BrainPart>() is BrainPart b && b.Target == null && b.PartyLeader == null
            && other.GetEffect<FrozenEffect>()?.Cold > 0 != true;
        internal static bool Special(Entity item) => item == null || item.HasTag("QuestItem") || item.HasTag("Quest") || item.HasTag("Unique")
            || item.HasTag("Essential") || item.HasTag("NoTrade") || item.HasTag("Owned") || item.HasTag("NoTake")
            || item.Properties.ContainsKey("QuestID") || item.Properties.ContainsKey("OwnerID") || item.Properties.ContainsKey("Owner");
        internal static bool Carried(Entity actor, Entity item, bool one = false) => WorldResourceActions.Carried(actor, item, one);
        internal static int Units(Entity e) => e?.GetPart<StackerPart>()?.StackCount ?? 1;
        internal static IEnumerable<Entity> Owned(Entity actor)
        {
            var inv = actor?.GetPart<InventoryPart>(); if (inv == null) return Array.Empty<Entity>();
            return inv.Objects.Concat(inv.EquippedItems.Values).Distinct().Where(e => OwnedExact(actor, e));
        }
        internal static bool OwnedExact(Entity actor, Entity item)
        {
            if (Carried(actor, item)) return true;
            var inv = actor?.GetPart<InventoryPart>(); var p = item?.GetPart<PhysicsPart>();
            return inv != null && item != null && item.SpatialZone == null && p?.ParentEntity == item && p.Equipped == actor
                && p.InInventory == null && !inv.Objects.Contains(item) && inv.EquippedItems.ContainsValue(item)
                && (actor.GetPart<Body>() == null || inv.FindEquippedBodyPart(item) != null);
        }
        internal static Entity Selected(Entity actor, string command)
        {
            int i = command?.IndexOf('|') ?? -1; if (i < 0 || command.IndexOf('|', i + 1) >= 0) return null;
            string id = WorldResourceActions.Decode(command.Substring(i + 1));
            var found = Owned(actor).Where(e => e.ID == id).ToArray(); return found.Length == 1 ? found[0] : null;
        }
        internal static Entity GroundSelected(Zone zone, string command)
        { int i = command?.IndexOf('|') ?? -1; return i < 0 || command.IndexOf('|', i + 1) >= 0 ? null : WorldResourceActions.ExactGround(zone, command.Substring(i + 1)); }
        internal static string BookTitle(Entity book)
        {
            string title = book.GetDisplayName(); const string prefix = "Grimoire of ";
            return title.StartsWith(prefix, StringComparison.Ordinal) ? title.Substring(prefix.Length) : title;
        }
        internal static string Choice(string command, Entity e) => command + "|" + Uri.EscapeDataString(e.ID);
        internal static bool Claim(InventoryTransaction tx, Entity actor, string command, params Entity[] owners)
        { if (tx == null || !tx.TryClaim(actor, actor, command)) return false; foreach (var e in owners) if (!tx.TryClaim(e, actor, command)) return false; return true; }
        internal static bool Pay(InventoryTransaction tx, Entity actor, Entity provider, int amount)
        {
            if (amount <= 0 || TradeSystem.GetDrams(actor) < amount || TradeSystem.GetDrams(provider) > int.MaxValue - amount) return false;
            tx.DeferCurrencyTransfer(actor, provider, amount); return true;
        }
        internal static bool Consume(InventoryTransaction tx, Entity actor, Entity item, int quantity, string command)
        {
            if (quantity < 1 || !Carried(actor, item) || Units(item) < quantity || !tx.TryClaim(item, actor, command)) return false;
            var inv = actor.GetPart<InventoryPart>(); var snap = InventoryTransferSnapshot.Capture(inv, item); tx.Do(null, snap.Restore);
            return snap.Apply(() => { for (int i = 0; i < quantity; i++) if (!Carried(actor, item) || !inv.TryConsumeOne(item)) return false; return true; })
                && snap.ClaimChanges(tx, actor, command);
        }
        internal static bool Spend(InventoryTransaction tx, Entity owner, string blueprint, int quantity, string command)
        {
            var items = owner.GetPart<InventoryPart>()?.Objects.Where(e => e.BlueprintName == blueprint && Carried(owner, e)).ToArray();
            if (items == null || items.Sum(e => (long)Units(e)) < quantity) return false;
            foreach (var item in items) { int use = Math.Min(quantity, Units(item)); if (!Consume(tx, owner, item, use, command)) return false; quantity -= use; if (quantity == 0) return true; } return false;
        }
        internal static bool Give(InventoryTransaction tx, Entity actor, Entity item, string command)
        {
            var inv = actor.GetPart<InventoryPart>(); if (inv == null || item == null || item.SpatialZone != null
                || item.GetPart<PhysicsPart>()?.InInventory != null || item.GetPart<PhysicsPart>()?.Equipped != null || !tx.TryClaim(item, actor, command)) return false;
            var snap = InventoryTransferSnapshot.Capture(inv, item); tx.Do(null, snap.Restore);
            return snap.Apply(() => inv.AddObject(item)) && snap.ClaimChanges(tx, actor, command);
        }
        internal static bool Transfer(InventoryTransaction tx, Entity actor, Entity from, Entity to, Entity item, string command)
        {
            if (!Carried(from, item) || !Claim(tx, actor, command, from, to, item)) return false;
            var a = from.GetPart<InventoryPart>(); var b = to.GetPart<InventoryPart>(); if (b == null) return false;
            var sa = InventoryTransferSnapshot.Capture(a, item); var sb = InventoryTransferSnapshot.Capture(b, item);
            tx.Do(null, sa.Restore); tx.Do(null, sb.Restore);
            return sa.Apply(() => sb.Apply(() => a.RemoveObject(item) && b.AddObject(item)))
                && sa.ClaimChanges(tx, actor, command) && sb.ClaimChanges(tx, actor, command);
        }
        internal static bool Fresh(Entity e, string blueprint) => e != null && e.BlueprintName == blueprint && !string.IsNullOrEmpty(e.ID)
            && e.SpatialZone == null && e.GetPart<PhysicsPart>() is PhysicsPart p && p.ParentEntity == e && p.InInventory == null && p.Equipped == null
            && e.Parts.All(part => part.ParentEntity == e) && !CombatSystem.IsDeathHandled(e);
        internal static bool Reject(Entity actor, Entity owner, string command, string reason)
        { Diag.Record("event", "ExplorationActionRejected", actor, owner, new { command, reason }); return false; }
        internal static void Report(InventoryTransaction tx, Entity actor, Entity owner, string command)
            => tx.AfterCommit(() => { Diag.Record("event", "ExplorationActionCompleted", actor, owner, new { command }); });
    }

    /// <summary>Small common dispatch boundary; each concrete Part owns its eligibility,
    /// choices and saved consequences. Menu reads never execute gameplay callbacks.</summary>
    public abstract class SecondExplorationServicePart : Part
    {
        protected abstract string[] Verbs { get; }
        protected virtual bool Civilian => true;
        protected virtual bool Current(Entity actor, Zone zone) => ParentEntity != null
            && ParentEntity.Parts.Count(p => p.GetType() == GetType()) == 1
            && (Civilian ? SecondExplorationActions.Willing(actor, ParentEntity, zone) : SecondExplorationActions.Near(actor, ParentEntity, zone));
        protected abstract void Choices(Entity actor, Zone zone, InventoryActionList actions);
        protected abstract bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx);
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
            if (e.ID == "GetInventoryActions") { if (Current(actor, zone)) Choices(actor, zone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            if (!Verbs.Any(v => command == v || command?.StartsWith(v + "|", StringComparison.Ordinal) == true)) return true;
            var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (tx == null || !Current(actor, zone)) { SecondExplorationActions.Reject(actor, ParentEntity, command, "unavailable-owner"); return true; }
            // Action-block callbacks belong to execution, never the pure menu reader.
            if (actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || !Current(actor, zone))
            { SecondExplorationActions.Reject(actor, ParentEntity, command, "actor-blocked"); return true; }
            if (!SecondExplorationActions.Claim(tx, actor, command, ParentEntity) || !Apply(actor, zone, command, tx))
            { SecondExplorationActions.Reject(actor, ParentEntity, command, "requirements-or-state-changed"); return true; }
            SecondExplorationActions.Report(tx, actor, ParentEntity, command); e.Handled = true; return false;
        }
        protected static void Offer(InventoryActionList a, string text, string command) => a?.AddAction(command, text, command, '\0', 24);
    }
}
