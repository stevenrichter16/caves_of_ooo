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
    public sealed class LiquidSettlingPart : Part, IDescribeState
    {
        public override string Name => "LiquidSettling";

        public const string Command = "SettleLiquid|", AllCommand = "SettleAll|", Feed = "bog-mire", Water = "water", Oil = "oil";
        public const int Ratio = 4, FuelPerBatch = 5, FireReach = 2;

        /// <summary>The small text under a settle row: what it turns the mire into, at what rate, and what the fire pays.</summary>
        static string SettleHint(string output) => "Settles bog-mire into clean " + output + ", " + Ratio + " mire to 1. Each flask spends " + FuelPerBatch + " of the lit fire's fuel; the dregs are lost.";
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
                bool one = command != null && command.StartsWith(Command, StringComparison.Ordinal), all = command != null && command.StartsWith(AllCommand, StringComparison.Ordinal);
                if (!one && !all) return true;
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone; var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
                if (all ? TrySettleAll(actor, zone, command, tx) : TrySettle(actor, zone, command, tx)) { e.Handled = true; return false; }
            }
            return true;
        }

        // ── the fire coupling ──

        /// <summary>
        /// A burning fire with fuel. A finite cooking fire (the camp hearth, the coals) is lit by its own rule, hot enough to cook with fuel left (about 60 turns after a lighting),
        /// not by the flame temperature, so the hearth that is visibly burning is the one that pays; any other fire must be aflame.
        /// </summary>
        static bool Burning(Entity fire, Entity still)
        {
            var heat = fire?.GetPart<ThermalPart>(); var fuel = fire?.GetPart<FuelPart>();
            if (fire == still || heat == null || fuel == null || !WorldResourceActions.Finite(heat.Temperature) || !WorldResourceActions.Finite(fuel.FuelMass) || fuel.FuelMass <= 0) return false;
            var camp = fire.GetPart<CampfirePart>();
            bool lit = camp != null && camp.FiniteCooking ? CampfireWorkActions.Hot(camp) : heat.IsAflame;
            return lit && !(fire.GetEffect<FrozenEffect>()?.Cold > 0) && !(fire.GetEffect<WetEffect>()?.Moisture > WetLimit);
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

        /// <summary>Batches the burning fires within reach could pay for between them (each fire spares whole batches of <see cref="FuelPerBatch"/>).</summary>
        int Capacity(Zone zone)
        {
            int batches = 0; var cell = zone?.GetEntityCell(ParentEntity); if (cell == null) return 0;
            foreach (var other in zone.GetReadOnlyEntities())
            {
                if (!Burning(other, ParentEntity) || !WorldResourceActions.Ground(other, zone)) continue;
                var at = zone.GetEntityCell(other); if (at == null || Math.Max(Math.Abs(at.X - cell.X), Math.Abs(at.Y - cell.Y)) > FireReach) continue;
                batches += (int)Math.Floor(other.GetPart<FuelPart>().FuelMass / FuelPerBatch);
            }
            return batches;
        }

        Entity[] SettleableFlasks(Entity actor)
            => actor?.GetPart<InventoryPart>()?.Objects.Where(o => o?.GetPart<LiquidVesselPart>() is LiquidVesselPart v && v.LiquidId == Feed && v.Volume >= Ratio && WorldResourceActions.Carried(actor, o))
                .OrderBy(o => o.ID, StringComparer.Ordinal).ToArray() ?? new Entity[0];

        bool Standing => ParentEntity?.GetPart<PhysicsPart>()?.InInventory == null && !PackableFixturePart.IsInertKit(ParentEntity);

        // "What can I do here?": what the mire you carry would settle to at this still, or what it is missing, in both examine readers.
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 50;
        public string DescribeState(DescribeContext context)
        {
            var actor = context.Viewer; var zone = context.Zone ?? ParentEntity?.SpatialZone;
            if (actor == null || zone == null || !Standing || zone.GetEntityCell(ParentEntity) == null) return null;
            int mire = 0; var pack = actor.GetPart<InventoryPart>(); if (pack == null) return null;
            foreach (var o in pack.Objects) if (o?.GetPart<LiquidVesselPart>() is LiquidVesselPart v && v.LiquidId == Feed && WorldResourceActions.Carried(actor, o)) mire += v.Volume;
            if (mire < Ratio) return null;
            var fire = FindFire(zone, out float fullest);
            if (fire == null) return "You carry " + mire + " mire. Settling it needs a lit fire within " + FireReach + " cells that can spare " + FuelPerBatch + " fuel.";
            var settleable = SettleableFlasks(actor); int batches = Math.Min(settleable.Length, Math.Max(1, Capacity(zone))); int yield = 0;
            for (int i = 0; i < batches && i < settleable.Length; i++) yield += settleable[i].GetPart<LiquidVesselPart>().Volume / Ratio;
            return "Here you can settle your mire: " + yield + " clean water or oil from " + batches + (batches == 1 ? " flask." : " flasks.");
        }

        // ── rows ──

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !Standing || !WorldResourceActions.ActorCurrent(actor, zone) || zone.GetEntityCell(ParentEntity) == null) return;
            var pack = actor.GetPart<InventoryPart>(); if (pack == null) return;
            var flasks = pack.Objects.Where(o => o?.GetPart<LiquidVesselPart>() is LiquidVesselPart v && v.LiquidId == Feed && v.Volume > 0 && WorldResourceActions.Carried(actor, o)).ToArray();
            if (flasks.Length == 0) return;   // the zone scan for a fire is only worth doing for a player who carries mire
            var fire = FindFire(zone, out float fullest);
            for (int n = 0; n < flasks.Length; n++)
            {
                var flask = flasks[n]; var vessel = flask.GetPart<LiquidVesselPart>(); string tag = flasks.Length > 1 ? " #" + (n + 1) : "";
                string code = null, reason = null;
                if (vessel.Volume < Ratio) { code = "too_little"; reason = "Settling needs at least " + Ratio + " units of mire."; }
                else if (fire == null)
                {
                    code = fullest < 0 ? "no_fire" : "fuel_short";
                    reason = fullest < 0 ? "Settling needs a lit fire beside the still." : "No fire here can spare the fuel: a batch burns " + FuelPerBatch + " and the fullest has " + (int)Math.Floor(fullest) + ".";
                }
                if (code != null) { actions.AddDisabled("SettleLiquid", "settle " + vessel.Volume + " mire" + tag, code, reason, '\0', 12); continue; }
                int yield = vessel.Volume / Ratio;
                actions.AddActionWithHint("SettleLiquid", "settle " + vessel.Volume + " mire to " + yield + " water" + tag, CommandFor(Water, flask, vessel), SettleHint("water"), '\0', 12, false, ActionTime.OneAction);
                actions.AddActionWithHint("SettleLiquid", "settle " + vessel.Volume + " mire to " + yield + " oil" + tag, CommandFor(Oil, flask, vessel), SettleHint("oil"), '\0', 11, false, ActionTime.OneAction);
            }
            // Two or more flasks: one row for all of them, saying how many the fires could pay for and what comes out.
            var settleable = SettleableFlasks(actor);
            if (settleable.Length >= 2 && fire != null)
            {
                int batches = Math.Min(settleable.Length, Capacity(zone)); if (batches < 1) return;
                int yield = 0; for (int i = 0; i < batches; i++) yield += settleable[i].GetPart<LiquidVesselPart>().Volume / Ratio;
                string scope = batches == settleable.Length ? "all " + batches + " flasks" : batches + " of " + settleable.Length + " flasks";
                actions.AddActionWithHint("SettleAll", "settle " + scope + " to " + yield + " water", AllCommand + Water, SettleHint("water"), '\0', 10, false, ActionTime.OneAction);
                actions.AddActionWithHint("SettleAll", "settle " + scope + " to " + yield + " oil", AllCommand + Oil, SettleHint("oil"), '\0', 9, false, ActionTime.OneAction);
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

        /// <summary>Settles every carried flask of mire in turn (by flask ID), each batch paying its own fuel from a fire that can spare it, until the fires cannot pay for another. One
        /// transaction: every flask and every fire's fuel returns together on any failure. It does nothing and says why when not even one batch can be paid for.</summary>
        bool TrySettleAll(Entity actor, Zone zone, string command, InventoryTransaction transaction)
        {
            string[] f = command.Split('|');
            if (f.Length != 2 || (f[1] != Water && f[1] != Oil)) return Reject(actor, "malformed_selection", "That is not a batch you can settle.");
            if (zone == null || actor == null || !WorldResourceActions.ActorCurrent(actor, zone) || CombatSystem.IsDeathHandled(actor) || (actor.GetStat("Hitpoints") is Stat hp && hp.Value <= 0)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || !Standing || zone.GetEntityCell(ParentEntity) == null || SpatialQuery.Distance(zone, actor, ParentEntity) > 1)
                return Reject(actor, "actor_unavailable", "You cannot work the still right now.");
            var flasks = SettleableFlasks(actor);
            if (flasks.Length == 0) return Reject(actor, "stale_selection", "You carry no mire to settle.");
            bool own = transaction == null; transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(ParentEntity, actor, command)) return Reject(actor, "in_progress", "The still is already being worked.");
                string target = f[1]; int batches = 0, produced = 0; var owner = ParentEntity;
                foreach (var flask in flasks)
                {
                    var fire = FindFire(zone, out _); if (fire == null) break;
                    if (!transaction.TryClaim(flask, actor, command) || !transaction.TryClaim(fire, actor, command)) return Reject(actor, "in_progress", "The still is already being worked.");
                    var vessel = flask.GetPart<LiquidVesselPart>(); var fuel = fire.GetPart<FuelPart>(); float fuelBefore = fuel.FuelMass; int volume = vessel.Volume, yield = volume / Ratio;
                    transaction.Do(() => { vessel.LiquidId = target; vessel.Volume = yield; fuel.FuelMass = fuelBefore - FuelPerBatch; }, () => { vessel.LiquidId = Feed; vessel.Volume = volume; fuel.FuelMass = fuelBefore; });
                    transaction.AfterCommit(() => Diag.Record("liquid", "Settled", actor, owner, new { from = Feed, to = target, units = volume, yield, fuel = FuelPerBatch }));
                    batches++; produced += yield;
                }
                if (batches == 0)
                    return FindFire(zone, out float fullest) == null && fullest < 0 ? Reject(actor, "no_fire", "Settling needs a lit fire beside the still.")
                        : Reject(actor, "fuel_short", "No fire here can spare the fuel: a batch burns " + FuelPerBatch + ".");
                int done = batches, total = flasks.Length, made = produced;
                transaction.AfterCommit(() => MessageLog.Add("You settle " + done + (done == total ? " flasks" : " of " + total + " flasks") + " of mire over the fire: " + made + " " + target + ". The dregs stay in the pot."));
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        bool Reject(Entity actor, string reason, string message)
        {
            MessageLog.Add(message);
            Diag.Record("liquid", "SettleRejected", actor, ParentEntity, new { reason });
            return false;
        }
    }
}
