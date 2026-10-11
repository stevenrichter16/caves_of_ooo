using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One carried pith removes up to twenty units of an existing oil,
    /// pitch or honey body coat. Selection binds the exact effect, not merely
    /// its name; weak transient tokens neither change saves nor retain owners.</summary>
    public static class BodyCoatingCleanupActions
    {
        public const int MaximumAmount = 20;
        const string Prefix = "WickBody|";
        sealed class Token { internal readonly string Value = Guid.NewGuid().ToString("N"); }
        static readonly ConditionalWeakTable<LiquidCoveredEffect, Token> Tokens = new ConditionalWeakTable<LiquidCoveredEffect, Token>();
        static string Identity(LiquidCoveredEffect coat) => Tokens.GetValue(coat, _ => new Token()).Value;
        public static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;

        public static void AddActions(Entity actor, Entity item, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Source(actor, item, zone)) return;
            var origin = zone.GetEntityCell(actor);
            foreach (var target in zone.GetReadOnlyEntities())
            {
                if (!Recipient(actor, target, zone, out var coat)) continue;
                var at = zone.GetEntityCell(target);
                string command = Prefix + Escape(zone.ZoneID) + "|" + origin.X + "|" + origin.Y + "|" + Escape(target.ID)
                    + "|" + at.X + "|" + at.Y + "|" + Quantity(item) + "|" + coat.LiquidId + "|" + coat.Amount + "|" + Identity(coat);
                actions.AddAction("WickBody", "wick " + coat.LiquidId + " from " + (target == actor ? "yourself" : target.GetDisplayName())
                    + " (1 pith; remove " + Math.Min(MaximumAmount, coat.Amount) + ")", command, '\0', 18);
            }
        }

        internal static bool TryAct(Entity actor, Entity item, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command) || tx == null || !Source(actor, item, zone)) return Reject(actor, item, "source-unavailable");
            string[] f = command.Split('|');
            if (f.Length != 11 || !Number(f[2], out int ox) || !Number(f[3], out int oy)
                || !Number(f[5], out int x) || !Number(f[6], out int y) || !Number(f[7], out int count)
                || !Number(f[9], out int amount)) return Reject(actor, item, "malformed-selection");
            var target = WorldResourceActions.ExactGround(zone, f[4]);
            var origin = zone.GetEntityCell(actor); var at = target == null ? null : zone.GetEntityCell(target);
            if (WorldResourceActions.Decode(f[1]) != zone.ZoneID || origin.X != ox || origin.Y != oy
                || at == null || at.X != x || at.Y != y || count != Quantity(item)
                || !Recipient(actor, target, zone, out var coat) || coat.LiquidId != f[8] || coat.Amount != amount || Identity(coat) != f[10])
                return Reject(actor, item, "stale-selection");
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(item, actor, command) || !tx.TryClaim(target, actor, command))
                return Reject(actor, item, "in-progress");
            var inventory = actor.GetPart<InventoryPart>(); var physics = item.GetPart<PhysicsPart>(); var stack = item.GetPart<StackerPart>();
            var status = target.GetPart<StatusEffectsPart>(); string modifiers = coat.AppliedModsRaw; int duration = coat.Duration;
            bool Current() => WorldResourceActions.ActorCurrent(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && zone.GetEntityCell(actor) == origin && actor.GetPart<InventoryPart>() == inventory && item.BlueprintName == "PrismreedPith"
                && item.GetPart<PhysicsPart>() == physics && item.GetPart<StackerPart>() == stack
                && (count > 1 ? WorldResourceActions.Carried(actor, item, false) && Quantity(item) == count - 1
                    : !inventory.Objects.Contains(item) && item.SpatialZone == null && physics.InInventory == null && physics.Equipped == null)
                && WorldResourceActions.ExactGround(zone, f[4]) == target && zone.GetEntityCell(target) == at
                && Recipient(actor, target, zone, out var current) && current == coat && target.GetPart<StatusEffectsPart>() == status
                && coat.Amount == amount && coat.LiquidId == f[8] && coat.AppliedModsRaw == modifiers && coat.Duration == duration;
            var payment = InventoryTransferSnapshot.Capture(inventory, item); tx.Do(null, payment.Restore);
            if (!payment.Apply(() => inventory.TryConsumeOne(item)) || !payment.ClaimChanges(tx, actor, command) || !Current())
                return Reject(actor, item, "changed-during-payment");
            tx.BeforeCommit(Current);
            // The physical consequence follows the committed payment. External
            // EffectRemoved callbacks cannot turn completed cleaning into free use.
            tx.AfterCommit(() => {
                coat.Amount = Math.Max(0, amount - MaximumAmount);
                if (coat.Amount == 0) status.RemoveEffect(coat);
            });
            tx.AfterCommit(() => ZoneRenderHooks.MarkCellDirty(at.X, at.Y, "BodyCoatingCleanup"));
            tx.AfterCommit(() => Diag.Record("event", "BodyCoatingCleaned", actor, target,
                new { liquid = f[8], removed = Math.Min(MaximumAmount, amount), remaining = Math.Max(0, amount - MaximumAmount), spent = 1 }));
            tx.AfterCommit(() => MessageLog.Add("You wick " + Math.Min(MaximumAmount, amount) + " " + f[8] + " from "
                + (target == actor ? "yourself" : target.GetDisplayName()) + ". Other conditions remain."));
            return true;
        }

        static bool Source(Entity actor, Entity item, Zone zone) => item?.BlueprintName == "PrismreedPith"
            && WorldResourceActions.ActorCurrent(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
            && WorldResourceActions.Carried(actor, item, false) && actor.GetPart<InventoryPart>().CanConsumeOne(item);
        static bool Recipient(Entity actor, Entity target, Zone zone, out LiquidCoveredEffect coat)
        {
            coat = null;
            if (!WorldResourceActions.Nearby(actor, target, zone) || !target.HasTag("Creature") || CombatSystem.IsDeathHandled(target)
                || target.GetStatValue("Hitpoints", 0) <= 0 || target.GetPart<RenderPart>()?.Visible != true
                || !zone.GetOccupiedCells(target).Any(cell => cell.IsVisible && SpatialQuery.DistanceToCell(zone, actor, cell.X, cell.Y) <= 1)) return false;
            if (target != actor && (!BrainPart.ArePartyAligned(actor, target)
                || target.GetPart<BrainPart>()?.IsPersonallyHostileTo(actor) == true || actor.GetPart<BrainPart>()?.IsPersonallyHostileTo(target) == true)) return false;
            var status = target.GetPart<StatusEffectsPart>();
            if (status?.ParentEntity != target) return false;
            var coats = status.GetAllEffects().OfType<LiquidCoveredEffect>().ToArray();
            if (coats.Length != 1) return false;
            coat = coats[0];
            return coat.GetType() == typeof(LiquidCoveredEffect) && coat.Owner == target && coat.Amount > 0 && coat.Duration != 0
                && (coat.LiquidId == "oil" || coat.LiquidId == "pitch" || coat.LiquidId == "honey");
        }
        static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        static string Escape(string value) => Uri.EscapeDataString(value ?? "");
        static bool Number(string value, out int result) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result >= 0;
        static bool Reject(Entity actor, Entity item, string reason)
        { Diag.Record("event", "BodyCoatingCleanupRejected", actor, item, new { reason }); return false; }
    }
}
