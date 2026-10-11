using System;
using System.Linq;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core
{
    public static partial class CompanionManagementActions
    {
        const string Equip = "CompanionEquip", Unequip = "CompanionUnequip", Compare = "CompanionCompare";
        static readonly EquipPlanner GearPlanner = new EquipPlanner();
        static bool GearCommand(string command) => Prefix(command, Equip) || Prefix(command, Unequip) || Prefix(command, Compare);
        static bool Unbound(Entity item) => !SecondExplorationActions.Special(item) && !item.HasPart<RentalPart>() && !item.HasTag("NoDrop");
        static string PlanStamp(EquipPlan plan) => string.Join(",", plan.ClaimedParts.Select(p => p.ID)) + ":"
            + string.Join(",", plan.Displacements.Select(d => d.BodyPart.ID + "=" + Uri.EscapeDataString(d.Item.ID)));
        static string WornStamp(Body body, Entity item) => string.Join(",", body.GetParts().Where(p => p._Equipped == item).Select(p => p.ID));
        static void OfferGear(Entity actor, Entity follower, InventoryActionList actions)
        {
            var body = follower.GetPart<Body>();
            foreach (var item in SecondExplorationActions.Owned(follower).ToArray())
            {
                if (item.GetPart<EquippablePart>() == null) continue;
                Offer(actions, "compare companion gear: " + item.GetDisplayName(), Choice(Compare, item));
                if (body?.ParentEntity != follower || !Unbound(item)) continue;
                if (Supply(follower, item))
                {
                    foreach (var part in body.GetParts())
                    {
                        var plan = GearPlanner.Build(follower, item, part);
                        if (!plan.IsValid || plan.Displacements.Any(d => !Unbound(d.Item))) continue;
                        string old = string.Join(", ", plan.Displacements.Select(d => d.Item).Distinct().Select(e => e.GetDisplayName()));
                        Offer(actions, "equip " + item.GetDisplayName() + " on " + part.GetDisplayName() + (old.Length > 0 ? " (replace " + old + ")" : ""),
                            Choice(Equip, item) + "|" + part.ID + "|" + PlanStamp(plan));
                    }
                }
                else
                    Offer(actions, "unequip to companion pack: " + item.GetDisplayName(), Choice(Unequip, item) + "|" + WornStamp(body, item));
            }
        }
        static Entity SelectedGear(Entity follower, string[] fields)
        {
            if (fields.Length < 3 || !int.TryParse(fields[2], out int quantity) || quantity < 1) return null;
            string id = WorldResourceActions.Decode(fields[1]);
            var matches = SecondExplorationActions.Owned(follower).Where(e => e.ID == id).ToArray();
            return matches.Length == 1 && Units(matches[0]) == quantity ? matches[0] : null;
        }
        static bool ReadGear(OwnerScope scope, string command, InventoryTransaction tx)
        {
            var fields = command.Split('|'); if (fields.Length != 3) return false;
            var item = SelectedGear(scope.Follower, fields);
            var pack = scope.Follower.GetPart<InventoryPart>(); var carried = pack.Objects.ToArray(); var worn = pack.EquippedItems.ToArray();
            var body = scope.Follower.GetPart<Body>(); var slots = body?.GetParts().Select(p => new { Part=p, Item=p._Equipped }).ToArray();
            if (item == null || !EquipmentComparisonService.TryDescribe(scope.Follower, item, out string text, out _)) return false;
            tx.BeforeCommit(() => scope.Current() && SelectedGear(scope.Follower, fields) == item
                && carried.SequenceEqual(pack.Objects) && worn.SequenceEqual(pack.EquippedItems) && scope.Follower.GetPart<Body>() == body
                && (body == null || slots.SequenceEqual(body.GetParts().Select(p => new { Part=p, Item=p._Equipped }))));
            tx.AfterCommit(() => MessageLog.AddAnnouncement(text)); return true;
        }
        static bool ChangeGear(OwnerScope scope, string command, InventoryTransaction tx)
        {
            var fields = command.Split('|'); var follower = scope.Follower; var body = follower.GetPart<Body>();
            var item = SelectedGear(follower, fields);
            if (item == null || !Unbound(item) || body?.ParentEntity != follower || !tx.TryClaim(item, scope.Actor, command)) return false;
            var context = new InventoryContext(follower, scope.Zone);
            if (fields[0] == Equip)
            {
                if (fields.Length != 5 || !Supply(follower, item) || !int.TryParse(fields[3], out int slot)) return false;
                var parts = body.GetParts().Where(p => p.ID == slot).ToArray(); if (parts.Length != 1) return false;
                var part = parts[0]; var plan = GearPlanner.Build(follower, item, part);
                if (!plan.IsValid || PlanStamp(plan) != fields[4] || plan.Displacements.Any(d => !Unbound(d.Item))) return false;
                foreach (var old in plan.Displacements.Select(d => d.Item).Distinct()) if (!tx.TryClaim(old, scope.Actor, command)) return false;
                Entity expectedPlacement = null;
                int expectedRemaining = Units(item) > 1 ? Units(item) - 1 : 1;
                bool Prepared(Entity prepared)
                {
                    expectedPlacement = prepared;
                    var fresh = GearPlanner.Build(follower, prepared, part);
                    return scope.Current() && follower.GetPart<Body>() == body && body.GetParts().Contains(part)
                        && Supply(follower, item) && Units(item) == expectedRemaining
                        && prepared != null && Units(prepared) == 1 && Unbound(prepared)
                        && prepared.SpatialZone == null && prepared.GetPart<PhysicsPart>()?.Equipped == null
                        && (prepared == item || prepared.GetPart<PhysicsPart>()?.InInventory == null)
                        && fresh.IsValid && PlanStamp(fresh) == fields[4] && Unbound(item)
                        && fresh.Displacements.All(d => Unbound(d.Item));
                }
                if (!EquipCommand.ExecuteInternal(context, tx, item, part, true, true, true, Prepared).Success) return false;
                // Stack equip may create one separate object. Verify the actual
                // selected slot and its native owner after all outer callbacks.
                var placed = expectedPlacement;
                tx.BeforeCommit(() => scope.Current() && follower.GetPart<Body>() == body && body.GetParts().Contains(part)
                    && placed != null && part._Equipped == placed && SecondExplorationActions.OwnedExact(follower, placed)
                    && placed.GetPart<PhysicsPart>()?.Equipped == follower && WithinCapacity(context.Inventory, null));
                return placed != null && part._Equipped == placed;
            }
            if (fields[0] != Unequip || fields.Length != 4 || Supply(follower, item) || WornStamp(body, item) != fields[3]) return false;
            if (!new UnequipCommand(item).Execute(context, tx).Success) return false;
            tx.BeforeCommit(() => scope.Current() && follower.GetPart<Body>() == body && Supply(follower, item) && WithinCapacity(context.Inventory, null));
            return true;
        }
    }
}
