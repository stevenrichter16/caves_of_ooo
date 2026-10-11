using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Local, explicit management of an exact current recruit. Mutating
    /// commands join the caller's inventory receipt; input pays only on commit.
    /// Whole selected stacks retain their identity rather than merging invisibly.</summary>
    public static partial class CompanionManagementActions
    {
        public const string PackCommand = "CompanionPack";
        public const string StepAsideCommand = "CompanionStepAside";
        const string Give = "CompanionGive", Take = "CompanionTake";
        /// <summary>Recognizes only this service's commands, for native paid dispatch.</summary>
        public static bool IsCommand(string command) => IsReadOnlyCommand(command) || Prefix(command, Give) || Prefix(command, Take) || GearCommand(command) || command == StepAsideCommand;
        /// <summary>Pack inspection is a free, current-owner read.</summary>
        public static bool IsReadOnlyCommand(string command) => command == PackCommand || Prefix(command, Compare);
        static bool Prefix(string command, string verb) => command?.StartsWith(verb + "|", StringComparison.Ordinal) == true;
        static int Units(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        static bool Supply(Entity owner, Entity item) => WorldResourceActions.Carried(owner, item, false)
            && !SecondExplorationActions.Special(item) && !item.HasPart<RentalPart>() && !item.HasTag("NoDrop");
        static string Choice(string verb, Entity item) => verb + "|" + Uri.EscapeDataString(item.ID) + "|" + Units(item);
        static void Offer(InventoryActionList actions, string display, string command) => actions?.AddAction(command, display, command, '\0', 23);

        internal static bool HandleEvent(Entity follower, GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
            if (e.ID == "GetInventoryActions")
            {
                if (!Eligible(actor, follower, zone)) return true;
                var actions = e.GetParameter<InventoryActionList>("Actions");
                Offer(actions, "inspect companion pack and equipment", PackCommand);
                foreach (var item in actor.GetPart<InventoryPart>().Objects.ToArray())
                    if (Supply(actor, item)) Offer(actions, "give whole stack: " + item.GetDisplayName(), Choice(Give, item));
                foreach (var item in follower.GetPart<InventoryPart>().Objects.ToArray())
                    if (Supply(follower, item)) Offer(actions, "retrieve whole stack: " + item.GetDisplayName(), Choice(Take, item));
                OfferGear(actor, follower, actions);
                if (AsideCell(actor, follower, zone) != null) Offer(actions, "ask companion to step aside", StepAsideCommand);
                return true;
            }
            string command = e.GetStringParameter("Command");
            if (e.ID != "InventoryAction" || !IsCommand(command)) return true;
            var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (tx == null || !Eligible(actor, follower, zone)) return true;
            var scope = new OwnerScope(actor, follower, zone);
            if (!SecondExplorationActions.Claim(tx, actor, command, follower)) return true;
            if (command == PackCommand)
            {
                if (!ReadPack(scope, tx)) return true;
            }
            else if (Prefix(command, Compare))
            {
                if (!ReadGear(scope, command, tx)) return true;
            }
            else
            {
                if (actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                    || follower.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || !scope.Current()) return true;
                if (!(command == StepAsideCommand ? StepAside(scope, tx)
                    : GearCommand(command) ? ChangeGear(scope, command, tx) : Transfer(scope, command, tx))) return true;
            }
            e.Handled = true; return false;
        }

        static bool Transfer(OwnerScope scope, string command, InventoryTransaction tx)
        {
            var fields = command.Split('|');
            if (fields.Length != 3 || !int.TryParse(fields[2], out int quantity) || quantity < 1) return false;
            bool giving = fields[0] == Give;
            var from = giving ? scope.Actor : scope.Follower; var to = giving ? scope.Follower : scope.Actor;
            var item = WorldResourceActions.ExactCarried(from, fields[1]);
            if (!Supply(from, item) || Units(item) != quantity || !tx.TryClaim(item, scope.Actor, command)) return false;
            var source = from.GetPart<InventoryPart>(); var destination = to.GetPart<InventoryPart>();
            if (destination.Objects.Any(x => x == item || x?.ID == item.ID) || !WithinCapacity(destination, item)) return false;
            var beforeSource = InventoryTransferSnapshot.Capture(source, item);
            var beforeDestination = InventoryTransferSnapshot.Capture(destination, item);
            tx.Do(null, beforeSource.Restore); tx.Do(null, beforeDestination.Restore);
            bool moved = beforeSource.Apply(() => beforeDestination.Apply(() => source.RemoveObject(item) && destination.AddRetrievedObject(item)));
            if (!moved || !beforeSource.ClaimChanges(tx, scope.Actor, command) || !beforeDestination.ClaimChanges(tx, scope.Actor, command)) return false;
            tx.BeforeCommit(() => scope.Current() && Supply(to, item) && Units(item) == quantity
                && !source.Objects.Contains(item) && WithinCapacity(destination, null));
            tx.AfterCommit(() => MessageLog.Add((giving ? "You give " : "You retrieve ") + item.GetDisplayName()
                + (giving ? " to " : " from ") + scope.Follower.GetDisplayName() + "."));
            return true;
        }
        static bool WithinCapacity(InventoryPart pack, Entity incoming)
        {
            long weight = 0;
            foreach (var item in pack.Objects.Concat(pack.EquippedItems.Values).Distinct())
            { int amount = InventoryPart.GetItemWeight(item); if (amount < 0) return false; weight += amount; }
            if (incoming != null) { int amount = InventoryPart.GetItemWeight(incoming); if (amount < 0) return false; weight += amount; }
            return weight <= int.MaxValue && (pack.MaxWeight < 0 || weight <= pack.MaxWeight);
        }
        static bool ReadPack(OwnerScope scope, InventoryTransaction tx)
        {
            var pack = scope.Follower.GetPart<InventoryPart>();
            var carried = pack.Objects.ToArray(); var worn = pack.EquippedItems.ToArray();
            var quantities = carried.Select(Units).ToArray();
            var lines = new List<string> { scope.Follower.GetDisplayName() + " — companion pack", "",
                "Carried weight (including equipment): " + pack.GetCarriedWeight() + ". Strength allowance (soft limit): " + pack.GetMaxCarryWeight() + ".",
                "Hard pack limit: " + (pack.MaxWeight < 0 ? "none" : pack.MaxWeight.ToString()) + ".",
                "Transfer choices move the complete selected stack. Equipped and bound items cannot be retrieved.", "", "Carried:" };
            if (carried.Length == 0) lines.Add("(empty)");
            foreach (var item in carried) lines.Add("- " + item.GetDisplayName() + " [" + Units(item) + "]" + (Supply(scope.Follower, item) ? "" : " (not transferable)"));
            lines.Add(""); lines.Add("Equipment:");
            if (worn.Length == 0) lines.Add("(none)");
            var body = scope.Follower.GetPart<Body>();
            foreach (var entry in worn)
            {
                string slot = body?.GetParts().FirstOrDefault(p => p.ID.ToString() == entry.Key)?.GetDisplayName() ?? entry.Key;
                lines.Add("- " + slot + ": " + entry.Value.GetDisplayName());
            }
            bool Current() => scope.Current() && carried.SequenceEqual(pack.Objects) && worn.SequenceEqual(pack.EquippedItems)
                && quantities.SequenceEqual(carried.Select(Units));
            if (!Current()) return false;
            tx.BeforeCommit(() => Current()); string text = string.Join("\n", lines); tx.AfterCommit(() => MessageLog.AddAnnouncement(text)); return true;
        }
        static bool Eligible(Entity actor, Entity follower, Zone zone, bool adjacent = true)
        {
            var effect = follower?.GetEffect<RecruitedEffect>(); var brain = follower?.GetPart<BrainPart>();
            return actor != null && follower != null && actor != follower && zone != null
                && actor.HasTag("Creature") && follower.HasTag("Creature") && actor.GetStatValue("Hitpoints") > 0 && follower.GetStatValue("Hitpoints") > 0
                && WorldResourceActions.ActorCurrent(actor, zone) && WorldResourceActions.ActorCurrent(follower, zone)
                && zone.GetEntityCell(follower)?.IsVisible == true && follower.GetPart<RenderPart>()?.Visible != false
                && (!adjacent || SpatialQuery.Distance(zone, actor, follower) <= 1)
                && effect?.Owner == follower && effect.Duration != 0 && effect.Recruiter == actor
                && brain?.ParentEntity == follower && brain.CurrentZone == zone && brain.PartyLeader == actor && brain.Target == null
                && actor.GetPart<BrainPart>()?.PartyMembers.Contains(follower) == true
                && actor.GetPart<InventoryPart>()?.ParentEntity == actor && follower.GetPart<InventoryPart>()?.ParentEntity == follower
                && !FactionManager.IsHostile(actor, follower) && !FactionManager.IsHostile(follower, actor);
        }
        sealed class OwnerScope
        {
            internal readonly Entity Actor, Follower; internal readonly Zone Zone;
            readonly BrainPart brain; readonly RecruitedEffect effect; readonly InventoryPart source, destination;
            readonly Cell actorCell, followerCell;
            internal OwnerScope(Entity actor, Entity follower, Zone zone)
            { Actor=actor; Follower=follower; Zone=zone; brain=follower.GetPart<BrainPart>(); effect=follower.GetEffect<RecruitedEffect>(); source=actor.GetPart<InventoryPart>(); destination=follower.GetPart<InventoryPart>(); actorCell=zone.GetEntityCell(actor); followerCell=zone.GetEntityCell(follower); }
            internal bool Current(Cell expectedFollowerCell = null) => Eligible(Actor,Follower,Zone,expectedFollowerCell == null) && Follower.GetPart<BrainPart>()==brain && Follower.GetEffect<RecruitedEffect>()==effect
                && Actor.GetPart<InventoryPart>()==source && Follower.GetPart<InventoryPart>()==destination
                && Zone.GetEntityCell(Actor)==actorCell && Zone.GetEntityCell(Follower)==(expectedFollowerCell ?? followerCell);
        }
    }
}
