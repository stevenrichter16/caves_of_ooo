using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Settling at a still (Docs/MACHINE-INVENTED-USES.md, "fractionating still"). A flask of bog-mire is the difficult local feed; at a standing still, beside a lit fire that spends a
    /// fixed measure of its own fuel, it settles to either clean water or oil, four units to one, chosen at the still. The two outputs compete for the same feed, the dregs are spent,
    /// and nothing is created: output times four never exceeds the feed. Brine is deliberately not settled (a free salt source would inflate the Beating's money), and every other
    /// liquid is left to the shipped transfers. The still is the same station whether communal or a set-down camp kit; a carried kit offers nothing.
    /// <para>Fire is coupled, not assumed: the batch burns <see cref="FuelPerBatch"/> from a burning fire within <see cref="FireReach"/> of the still that can spare it, and refuses up front
    /// with the numbers otherwise.</para>
    /// </summary>
    public sealed class LiquidSettlingPart : Part
    {
        public override string Name => "LiquidSettling";

        public const string Command = "SettleLiquid|", Feed = "bog-mire", Water = "water", Oil = "oil";
        public const int Ratio = 4, FuelPerBatch = 5, FireReach = 2;
        const float WetLimit = 0.35f;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                AddRows(actor, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone, e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command == null || !command.StartsWith(Command, StringComparison.Ordinal)) return true;
                var actor = e.GetParameter<Entity>("Actor");
                if (TrySettle(actor, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone, command, e.GetParameter<InventoryTransaction>("InventoryTransaction")))
                { e.Handled = true; return false; }
            }
            return true;
        }

        // ── the fire coupling ──

        static bool Burning(Entity fire, Entity still)
        {
            var heat = fire?.GetPart<ThermalPart>(); var fuel = fire?.GetPart<FuelPart>();
            return fire != still && heat != null && fuel != null && WorldResourceActions.Finite(heat.Temperature) && heat.IsAflame
                && WorldResourceActions.Finite(fuel.FuelMass) && fuel.FuelMass > 0
                && !(fire.GetEffect<FrozenEffect>()?.Cold > 0) && !(fire.GetEffect<WetEffect>()?.Moisture > WetLimit);
        }

        /// <summary>The nearest burning fire that can spare a batch (ties by ID), or null with the fullest burning fire's fuel in <paramref name="fullest"/> (-1 when none burns).</summary>
        Entity FindFire(Zone zone, out float fullest)
        {
            fullest = -1f; Entity best = null; int bestDistance = int.MaxValue;
            var cell = zone?.GetEntityCell(ParentEntity); if (cell == null) return null;
            foreach (var other in zone.GetReadOnlyEntities())
            {
                if (!Burning(other, ParentEntity) || !WorldResourceActions.Ground(other, zone)) continue;
                var at = zone.GetEntityCell(other); if (at == null) continue;
                int distance = Math.Max(Math.Abs(at.X - cell.X), Math.Abs(at.Y - cell.Y)); if (distance > FireReach) continue;
                float fuel = other.GetPart<FuelPart>().FuelMass; if (fuel > fullest) fullest = fuel;
                if (fuel < FuelPerBatch) continue;
                if (distance < bestDistance || (distance == bestDistance && best != null && string.CompareOrdinal(other.ID, best.ID) < 0)) { best = other; bestDistance = distance; }
            }
            return best;
        }

        bool Standing => ParentEntity?.GetPart<PhysicsPart>()?.InInventory == null && !PackableFixturePart.IsInertKit(ParentEntity);

        // ── rows ──

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !Standing || !WorldResourceActions.ActorCurrent(actor, zone) || zone.GetEntityCell(ParentEntity) == null) return;
            var pack = actor.GetPart<InventoryPart>(); if (pack == null) return;
            var fire = FindFire(zone, out float fullest);
            foreach (var flask in pack.Objects.ToArray())
            {
                var vessel = flask?.GetPart<LiquidVesselPart>();
                if (vessel == null || vessel.LiquidId != Feed || vessel.Volume <= 0 || !WorldResourceActions.Carried(actor, flask)) continue;
                string code = null, reason = null;
                if (vessel.Volume < Ratio) { code = "too_little"; reason = "Settling needs at least " + Ratio + " units of mire."; }
                else if (fire == null)
                {
                    code = fullest < 0 ? "no_fire" : "fuel_short";
                    reason = fullest < 0 ? "Settling needs a lit fire beside the still." : "No fire here can spare the fuel: a batch burns " + FuelPerBatch + " and the fullest has " + (int)Math.Floor(fullest) + ".";
                }
                if (code != null) { actions.AddDisabled("SettleLiquid", "settle " + vessel.Volume + " mire", code, reason, '\0', 12); continue; }
                int yield = vessel.Volume / Ratio;
                actions.AddAction("SettleLiquid", "settle " + vessel.Volume + " mire to " + yield + " water", CommandFor(Water, flask, vessel), '\0', 12, false, ActionTime.OneAction);
                actions.AddAction("SettleLiquid", "settle " + vessel.Volume + " mire to " + yield + " oil", CommandFor(Oil, flask, vessel), '\0', 11, false, ActionTime.OneAction);
            }
        }

        static string CommandFor(string target, Entity flask, LiquidVesselPart vessel)
            => Command + target + "|" + flask.ID + "|" + vessel.Volume.ToString(CultureInfo.InvariantCulture);

        // ── the settling ──

        bool TrySettle(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (f.Length != 4 || (f[1] != Water && f[1] != Oil) || !int.TryParse(f[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int volume))
                return Reject(actor, "malformed_selection", "That is not a batch you can settle.");
            if (zone == null || actor == null || !WorldResourceActions.ActorCurrent(actor, zone) || CombatSystem.IsDeathHandled(actor) || (actor.GetStat("Hitpoints") is Stat hp && hp.Value <= 0)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || !Standing || zone.GetEntityCell(ParentEntity) == null || SpatialQuery.Distance(zone, actor, ParentEntity) > 1)
                return Reject(actor, "actor_unavailable", "You cannot work the still right now.");
            var pack = actor.GetPart<InventoryPart>();
            var flask = pack?.Objects.FirstOrDefault(o => o != null && o.ID == f[2] && WorldResourceActions.Carried(actor, o));
            var vessel = flask?.GetPart<LiquidVesselPart>();
            if (vessel == null || vessel.LiquidId != Feed || vessel.Volume != volume) return Reject(actor, "stale_selection", "The flask is not as it was.");
            if (volume < Ratio) return Reject(actor, "too_little", "Settling needs at least " + Ratio + " units of mire.");
            var fire = FindFire(zone, out float fullest);
            if (fire == null)
                return fullest < 0 ? Reject(actor, "no_fire", "Settling needs a lit fire beside the still.")
                    : Reject(actor, "fuel_short", "No fire here can spare the fuel: a batch burns " + FuelPerBatch + " and the fullest has " + (int)Math.Floor(fullest) + ".");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(flask, actor, command) || !tx.TryClaim(ParentEntity, actor, command) || !tx.TryClaim(fire, actor, command))
                    return Reject(actor, "in_progress", "The still is already being worked.");
                var fuel = fire.GetPart<FuelPart>(); float fuelBefore = fuel.FuelMass; int yield = volume / Ratio; string target = f[1];
                tx.Do(() => { vessel.LiquidId = target; vessel.Volume = yield; fuel.FuelMass = fuelBefore - FuelPerBatch; }, () => { vessel.LiquidId = Feed; vessel.Volume = volume; fuel.FuelMass = fuelBefore; });
                var owner = ParentEntity;
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You settle " + volume + " mire over the fire: " + yield + " " + target + " runs into the flask. The dregs stay in the pot.");
                    Diag.Record("liquid", "Settled", actor, owner, new { from = Feed, to = target, units = volume, yield, fuel = FuelPerBatch });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Reject(Entity actor, string reason, string message)
        {
            MessageLog.Add(message);
            Diag.Record("liquid", "SettleRejected", actor, ParentEntity, new { reason });
            return false;
        }
    }
}
