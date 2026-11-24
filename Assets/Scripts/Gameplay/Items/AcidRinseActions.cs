using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Finite clean water removes one exact acid coat. Wetness is staged
    /// with selective undo; coat removal waits for committed payment.</summary>
    public static class AcidRinseActions
    {
        public const int WaterCost = 2, MaximumAmount = 20;
        const string Prefix = "RinseAcid|";
        sealed class Token { internal readonly string Value = Guid.NewGuid().ToString("N"); }
        static readonly ConditionalWeakTable<object, Token> Tokens = new ConditionalWeakTable<object, Token>();
        static string Identity(object value) => Tokens.GetValue(value, _ => new Token()).Value;
        public static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        public static string UseText => "Rinse acid from yourself or a visible companion beside you: " + WaterCost
            + " clean water removes up to " + MaximumAmount + " acid coating and leaves the recipient wet. Other conditions remain; wetness conducts electricity.";

        public static void AddActions(Entity actor, Entity item, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Source(actor, item, zone, out int units, out int cap, out Part vessel)) return;
            var origin = zone.GetEntityCell(actor);
            foreach (var target in zone.GetReadOnlyEntities())
            {
                if (!Recipient(actor, target, zone, out var coat)) continue;
                var cell = zone.GetEntityCell(target);
                string command = Prefix + Escape(zone.ZoneID) + "|" + N(origin.X) + "|" + N(origin.Y) + "|" + Escape(item.ID)
                    + "|" + N(units) + "|" + N(cap) + "|" + (vessel is WaterskinPart ? "skin" : "flask")
                    + "|" + Escape(target.ID) + "|" + N(cell.X) + "|" + N(cell.Y) + "|" + N(coat.Amount)
                    + "|" + Identity(coat) + "|" + Context(actor, item, vessel, target, zone)
                    + "|" + Escape(item.BlueprintName);
                actions.AddActionWithHint("RinseAcid", "rinse acid from " + (target == actor ? "yourself" : target.GetDisplayName())
                    + " (" + WaterCost + " water; remove " + Math.Min(MaximumAmount, coat.Amount) + ")", command,
                    UseText, '\0', 18, false, ActionTime.OneAction);
            }
        }

        internal static bool TryAct(Entity actor, Entity item, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command) || tx == null || !Source(actor, item, zone, out int units, out int cap, out Part vessel))
                return Reject(actor, item, "source-unavailable");
            var f = command.Split('|');
            if (f.Length != 15 || !Number(f[2], out int ox) || !Number(f[3], out int oy)
                || !Number(f[5], out int quotedUnits) || !Number(f[6], out int quotedCap)
                || !Number(f[9], out int x) || !Number(f[10], out int y) || !Number(f[11], out int amount))
                return Reject(actor, item, "malformed-selection");
            var target = WorldResourceActions.ExactGround(zone, f[8]); var origin = zone.GetEntityCell(actor);
            var at = target == null ? null : zone.GetEntityCell(target);
            if (WorldResourceActions.Decode(f[1]) != zone.ZoneID || origin.X != ox || origin.Y != oy
                || WorldResourceActions.Decode(f[4]) != item.ID || units != quotedUnits || cap != quotedCap
                || f[7] != (vessel is WaterskinPart ? "skin" : "flask") || at == null || at.X != x || at.Y != y
                || !Recipient(actor, target, zone, out var coat) || coat.Amount != amount || Identity(coat) != f[12]
                || Context(actor, item, vessel, target, zone) != f[13] || WorldResourceActions.Decode(f[14]) != item.BlueprintName)
                return Reject(actor, item, "stale-selection");
            if (!tx.TryClaimAction(actor) || !tx.TryClaim(actor, actor, command) || !tx.TryClaim(item, actor, command)
                || !tx.TryClaim(target, actor, command)) return Reject(actor, item, "in-progress");

            var pack = actor.GetPart<InventoryPart>(); var physics = item.GetPart<PhysicsPart>(); var parts = item.Parts.ToArray();
            var status = target.GetPart<StatusEffectsPart>(); string modifiers = coat.AppliedModsRaw, sourceActor = coat.SourceActorId;
            int duration = coat.Duration; var oldWet = target.GetEffect<WetEffect>(); WetEffect appliedWet = oldWet;
            float oldMoisture = oldWet?.Moisture ?? 0; int oldWetDuration = oldWet?.Duration ?? 0, appliedDuration = 0;
            float addedMoisture = 0; bool stagedWet = false; var skin = vessel as WaterskinPart; var flask = vessel as LiquidVesselPart;
            bool OwnsEffect(Effect effect) => effect?.Owner == target && status.ParentEntity == target
                && target.GetPart<StatusEffectsPart>() == status && target.Parts.Count(p => p is StatusEffectsPart) == 1
                && status.GetAllEffects().Count(e => ReferenceEquals(e, effect)) == 1;
            bool Current(bool requireWet)
            {
                return WorldResourceActions.ActorCurrent(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                    && zone.GetEntityCell(actor) == origin && actor.GetPart<InventoryPart>() == pack
                    && WorldResourceActions.ExactCarried(actor, f[4]) == item && item.GetPart<PhysicsPart>() == physics
                    && item.Parts.SequenceEqual(parts) && parts.All(p => p.ParentEntity == item)
                    && WaterTransferActions.Vessel(actor, item, out int now, out int capacity) && now == units - WaterCost && capacity == cap
                    && ((Part)item.GetPart<WaterskinPart>() ?? item.GetPart<LiquidVesselPart>()) == vessel
                    && WorldResourceActions.Decode(f[14]) == item.BlueprintName
                    && WorldResourceActions.ExactGround(zone, f[8]) == target && zone.GetEntityCell(target) == at
                    && Recipient(actor, target, zone, out var current) && current == coat && target.GetPart<StatusEffectsPart>() == status
                    && coat.Amount == amount && coat.Duration == duration && coat.AppliedModsRaw == modifiers && coat.SourceActorId == sourceActor
                    && (!requireWet || target.GetEffect<WetEffect>() == appliedWet && appliedWet.Owner == target
                        && appliedWet.Moisture == 1f && appliedWet.Duration == appliedDuration && appliedDuration != 0);
            }
            tx.Do(() => WaterTransferActions.SetUnits(item, units - WaterCost), () => {
                // Invert only our debit. Independent spills/injury/forced drops are not ours to undo.
                if (vessel.ParentEntity != item || !item.Parts.Contains(vessel)
                    || item.Parts.Count(p => p is WaterskinPart || p is LiquidVesselPart) != 1
                    || ((Part)item.GetPart<WaterskinPart>() ?? item.GetPart<LiquidVesselPart>()) != vessel)
                { Diag.Record("event", "AcidRinseUndoVesselOwnerChanged", actor, item); return; }
                if (skin != null) skin.Charges = WaterQuantityUndo.Restore(skin.Charges, -WaterCost, Math.Min(cap, Math.Max(0, skin.Capacity)));
                else if (flask.LiquidId == "water" || flask.Volume == 0 && string.IsNullOrEmpty(flask.LiquidId))
                { flask.Volume = WaterQuantityUndo.Restore(flask.Volume, -WaterCost, Math.Min(cap, Math.Max(0, flask.Capacity))); if (flask.Volume > 0) flask.LiquidId = "water"; }
                else Diag.Record("event", "AcidRinseUndoLiquidChanged", actor, item);
            });
            if (!Current(false)) return Reject(actor, item, "payment-changed");
            var incoming = new WetEffect(1);
            tx.Do(null, () => {
                if (!stagedWet) return;
                if (!OwnsEffect(oldWet ?? incoming))
                { Diag.Record("event", "AcidRinseUndoWetOwnerChanged", actor, target); return; }
                if (oldWet == null) status.RemoveAddedEffectForInventoryUndo(incoming);
                else
                {
                    // Keep independent evaporation/additions; remove only our added moisture.
                    if (WorldResourceActions.Finite(oldWet.Moisture)) oldWet.Moisture = Math.Max(0, oldWet.Moisture - addedMoisture);
                    if (oldWet.Duration == appliedDuration) oldWet.Duration = oldWetDuration;
                }
            });
            // An already saturated active Wet needs no redundant effect application.
            if (oldWet == null || oldMoisture < 1f || oldWetDuration == 0)
            {
                if (!status.ApplyEffectWithReceipt(incoming, actor, zone,
                    () => {
                        if (!Current(false) || target.GetEffect<WetEffect>() != oldWet
                            || oldWet != null && (oldWet.Moisture != oldMoisture || oldWet.Duration != oldWetDuration))
                            throw new InvalidOperationException("The rinse recipient changed before wetness was applied.");
                    },
                    () => { stagedWet = true; if (oldWet != null) addedMoisture = Math.Max(0, oldWet.Moisture - oldMoisture); }))
                    return Reject(actor, item, "wetness-refused");
            }
            appliedWet = target.GetEffect<WetEffect>();
            if (appliedWet != (oldWet ?? incoming) || appliedWet?.Owner != target || !WorldResourceActions.Finite(appliedWet.Moisture)
                || appliedWet.Moisture != 1f) return Reject(actor, item, "wetness-changed");
            if (appliedWet.Duration == 0) appliedWet.Duration = Effect.DURATION_INDEFINITE;
            appliedDuration = appliedWet.Duration;
            if (!Current(true)) return Reject(actor, item, "changed-during-rinse");
            tx.BeforeCommit(() => Current(true));
            bool cleaned = false;
            tx.AfterCommit(() => {
                // Earlier committed observers can move or replace the coating. Payment
                // is final, but it grants no authority over the effect's new owner.
                if (!OwnsEffect(coat) || status.GetAllEffects().OfType<LiquidCoveredEffect>().Count() != 1
                    || coat.LiquidId != "acid" || coat.Amount != amount || coat.Duration != duration
                    || coat.AppliedModsRaw != modifiers || coat.SourceActorId != sourceActor)
                {
                    Diag.Record("event", "AcidRinseCommittedCoatChanged", actor, target, new { water = WaterCost });
                    MessageLog.Add("The rinse water was spent, but the acid coating changed before it could be removed.");
                    return;
                }
                coat.Amount = Math.Max(0, amount - MaximumAmount);
                cleaned = true;
                if (coat.Amount == 0) status.RemoveEffect(coat);
            });
            tx.AfterCommit(() => { if (cleaned) ZoneRenderHooks.MarkCellDirty(at.X, at.Y, "AcidRinsed"); });
            tx.AfterCommit(() => { if (cleaned) Diag.Record("event", "AcidRinsed", actor, target,
                new { removed = Math.Min(MaximumAmount, amount), remaining = Math.Max(0, amount - MaximumAmount), water = WaterCost }); });
            tx.AfterCommit(() => { if (cleaned) MessageLog.Add("You rinse " + Math.Min(MaximumAmount, amount) + " acid from "
                + (target == actor ? "yourself" : target.GetDisplayName()) + ". The recipient is wet; other conditions remain."); });
            return true;
        }

        static bool Source(Entity actor, Entity item, Zone zone, out int units, out int cap, out Part vessel)
        {
            units = cap = 0; vessel = null;
            if (!WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || !WaterTransferActions.Vessel(actor, item, out units, out cap) || units < WaterCost
                || string.IsNullOrEmpty(item.ID) || WorldResourceActions.ExactCarried(actor, Escape(item.ID)) != item) return false;
            vessel = (Part)item.GetPart<WaterskinPart>() ?? item.GetPart<LiquidVesselPart>(); return vessel != null;
        }
        static bool Recipient(Entity actor, Entity target, Zone zone, out LiquidCoveredEffect coat)
        {
            coat = null;
            if (!WorldResourceActions.Nearby(actor, target, zone) || !target.HasTag("Creature") || CombatSystem.IsDeathHandled(target)
                || target.GetStatValue("Hitpoints", 0) <= 0
                || string.IsNullOrEmpty(target.ID) || WorldResourceActions.ExactGround(zone, Escape(target.ID)) != target
                || target != actor && (target.GetPart<RenderPart>()?.Visible != true
                    || !zone.GetOccupiedCells(target).Any(c => c.IsVisible && SpatialQuery.DistanceToCell(zone, actor, c.X, c.Y) <= 1))) return false;
            if (target != actor && (!BrainPart.ArePartyAligned(actor, target)
                || target.GetPart<BrainPart>()?.IsPersonallyHostileTo(actor) == true || actor.GetPart<BrainPart>()?.IsPersonallyHostileTo(target) == true)) return false;
            var status = target.GetPart<StatusEffectsPart>(); if (status?.ParentEntity != target) return false;
            var coats = status.GetAllEffects().OfType<LiquidCoveredEffect>().ToArray(); if (coats.Length != 1) return false;
            var wets = status.GetAllEffects().OfType<WetEffect>().ToArray();
            if (wets.Length > 1 || wets.Length == 1 && (wets[0].GetType() != typeof(WetEffect) || wets[0].Owner != target
                || !WorldResourceActions.Finite(wets[0].Moisture) || wets[0].Moisture < 0 || wets[0].Moisture > 1)) return false;
            coat = coats[0]; return coat.GetType() == typeof(LiquidCoveredEffect) && coat.Owner == target
                && coat.LiquidId == "acid" && coat.Amount > 0 && coat.Duration != 0;
        }
        static string Context(Entity actor, Entity item, Part vessel, Entity target, Zone zone)
            => Identity(zone) + ":" + Identity(actor) + ":" + Identity(item) + ":" + Identity(vessel) + ":" + Identity(target);
        static string Escape(string s) => Uri.EscapeDataString(s ?? "");
        static string N(int n) => n.ToString(CultureInfo.InvariantCulture);
        static bool Number(string s, out int n) => int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out n) && n >= 0;
        static bool Reject(Entity actor, Entity item, string reason)
        { Diag.Record("event", "AcidRinseRejected", actor, item, new { reason }); return false; }
    }
}
