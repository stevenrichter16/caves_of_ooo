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

        /// <summary>Firing: baking mire into fire clay (Docs/MACHINE-INVENTED-USES.md 3j). Eight mire to one clay, ten of the lit fire's fuel; the rest of the flask stays as mire.</summary>
        public const string BakeCommand = "BakeClay|", ClayBlueprint = "FireClay";
        public const int ClayRatio = 8, ClayFuel = 10;

        /// <summary>The small text under a settle row: what it turns the mire into, at what rate, and what the fire pays.</summary>
        static string SettleHint(string output, bool vent = false) => "Settles bog-mire into clean " + output + ", " + Ratio + " mire to 1. " + (vent ? "The vent's steady heat pays; no fuel." : "The lit fire spends " + FuelPerBatch + " fuel per flask.");
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
                bool one = command != null && command.StartsWith(Command, StringComparison.Ordinal), all = command != null && command.StartsWith(AllCommand, StringComparison.Ordinal), bake = command != null && command.StartsWith(BakeCommand, StringComparison.Ordinal);
                if (!one && !all && !bake) return true;
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone; var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
                if (bake ? TryBake(actor, zone, command, tx) : all ? TrySettleAll(actor, zone, command, tx) : TrySettle(actor, zone, command, tx)) { e.Handled = true; return false; }
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
        Entity FindFire(Zone zone, out float fullest, float need = FuelPerBatch)
        {
            fullest = -1f; Entity best = null; int bestDistance = int.MaxValue;
            var cell = zone?.GetEntityCell(ParentEntity); if (cell == null) return null;
            foreach (var other in zone.GetReadOnlyEntities())
            {
                if (!Burning(other, ParentEntity) || !WorldResourceActions.Ground(other, zone)) continue;
                var at = zone.GetEntityCell(other); if (at == null) continue;
                int distance = Math.Max(Math.Abs(at.X - cell.X), Math.Abs(at.Y - cell.Y)); if (distance > FireReach) continue;
                float fuel = other.GetPart<FuelPart>().FuelMass; if (fuel > fullest) fullest = fuel;
                if (fuel < need) continue;
                if (distance < bestDistance || (distance == bestDistance && best != null && string.CompareOrdinal(other.ID, best.ID) < 0)) { best = other; bestDistance = distance; }
            }
            return best;
        }

        /// <summary>The steady natural heat (a steam vent, an ash bed) within reach of the still, which pays a settling batch with no fuel but cannot bake clay; null when there is none.</summary>
        Entity FindVent(Zone zone) => NaturalHeat.Find(zone, zone?.GetEntityCell(ParentEntity), FireReach);

        /// <summary>Why baking has no fire: a vent beside the still settles mire but is not a kiln, and the text says so rather than just "no fire".</summary>
        string NoFireToBake(Zone zone) => FindVent(zone) != null ? "The vent is not hot enough to bake clay; baking needs a lit fire beside the still." : "Baking clay needs a lit fire beside the still.";

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
            bool vent = FindVent(zone) != null; Entity fire = vent ? null : FindFire(zone, out _);
            if (!vent && fire == null) return "You carry " + mire + " mire. Settling it needs a lit fire within " + FireReach + " cells that can spare " + FuelPerBatch + " fuel.";
            var settleable = SettleableFlasks(actor); int batches = vent ? settleable.Length : Math.Min(settleable.Length, Math.Max(1, Capacity(zone))); int yield = 0;
            for (int i = 0; i < batches && i < settleable.Length; i++) yield += settleable[i].GetPart<LiquidVesselPart>().Volume / Ratio;
            return "Here you can settle your mire: " + yield + " clean water or oil from " + batches + (batches == 1 ? " flask" : " flasks") + (vent ? ", on the vent's heat." : ".") + (AlchemyStillPart.Factory != null && settleable.Any(o => o.GetPart<LiquidVesselPart>().Volume >= ClayRatio) ? " Or bake " + ClayRatio + " of it into fire clay." : "");
        }

        // ── rows ──

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !Standing || !WorldResourceActions.ActorCurrent(actor, zone) || zone.GetEntityCell(ParentEntity) == null) return;
            var pack = actor.GetPart<InventoryPart>(); if (pack == null) return;
            var flasks = pack.Objects.Where(o => o?.GetPart<LiquidVesselPart>() is LiquidVesselPart v && v.LiquidId == Feed && v.Volume > 0 && WorldResourceActions.Carried(actor, o)).ToArray();
            if (flasks.Length == 0) return;   // the zone scan for a fire is only worth doing for a player who carries mire
            bool vent = FindVent(zone) != null; float fullest = -1f; Entity fire = vent ? null : FindFire(zone, out fullest);
            for (int n = 0; n < flasks.Length; n++)
            {
                var flask = flasks[n]; var vessel = flask.GetPart<LiquidVesselPart>(); string tag = flasks.Length > 1 ? " #" + (n + 1) : "";
                string code = null, reason = null;
                if (vessel.Volume < Ratio) { code = "too_little"; reason = "Settling needs at least " + Ratio + " units of mire."; }
                else if (!vent && fire == null)
                {
                    code = fullest < 0 ? "no_fire" : "fuel_short";
                    reason = fullest < 0 ? "Settling needs a lit fire beside the still." : "No fire here can spare the fuel: a batch burns " + FuelPerBatch + " and the fullest has " + (int)Math.Floor(fullest) + ".";
                }
                if (code != null) { actions.AddDisabled("SettleLiquid", "settle " + vessel.Volume + " mire" + tag, code, reason, '\0', 12); continue; }
                int yield = vessel.Volume / Ratio;
                actions.AddActionWithHint("SettleLiquid", "settle " + vessel.Volume + " mire to " + yield + " water" + tag, CommandFor(Water, flask, vessel), SettleHint("water", vent), '\0', 12, false, ActionTime.OneAction);
                actions.AddActionWithHint("SettleLiquid", "settle " + vessel.Volume + " mire to " + yield + " oil" + tag, CommandFor(Oil, flask, vessel), SettleHint("oil", vent), '\0', 11, false, ActionTime.OneAction);
            }
            // Two or more flasks: one row for all of them, saying how many the fires could pay for and what comes out.
            var settleable = SettleableFlasks(actor);
            if (settleable.Length >= 2 && (vent || fire != null))
            {
                int batches = vent ? settleable.Length : Math.Min(settleable.Length, Capacity(zone)); if (batches < 1) return;
                int yield = 0; for (int i = 0; i < batches; i++) yield += settleable[i].GetPart<LiquidVesselPart>().Volume / Ratio;
                string scope = batches == settleable.Length ? "all " + batches + " flasks" : batches + " of " + settleable.Length + " flasks";
                actions.AddActionWithHint("SettleAll", "settle " + scope + " to " + yield + " water", AllCommand + Water, SettleHint("water", vent), '\0', 10, false, ActionTime.OneAction);
                actions.AddActionWithHint("SettleAll", "settle " + scope + " to " + yield + " oil", AllCommand + Oil, SettleHint("oil", vent), '\0', 9, false, ActionTime.OneAction);
            }
            AddBakeRows(actor, zone, actions, flasks);
        }

        /// <summary>One "bake" row per flask holding enough mire: boils the water off and bakes what is left into one fire clay. Needs the camp's hands (the content factory), a lit fire that can spare ten fuel, and room in the pack.</summary>
        void AddBakeRows(Entity actor, Zone zone, InventoryActionList actions, Entity[] flasks)
        {
            if (AlchemyStillPart.Factory == null) return;
            Entity fire = null; float fullest = -1f; bool looked = false;
            for (int n = 0; n < flasks.Length; n++)
            {
                var flask = flasks[n]; var vessel = flask.GetPart<LiquidVesselPart>(); if (vessel.Volume < ClayRatio) continue;
                if (!looked) { fire = FindFire(zone, out fullest, ClayFuel); looked = true; }
                string tag = flasks.Length > 1 ? " #" + (n + 1) : "", label = "bake " + ClayRatio + (vessel.Volume > ClayRatio ? " of the " + vessel.Volume : "") + " mire to 1 fire clay" + tag;
                if (fire == null)
                {
                    actions.AddDisabled("BakeClay", label, fullest < 0 ? "no_fire" : "fuel_short", fullest < 0 ? NoFireToBake(zone) : "No fire here can spare the fuel: baking burns " + ClayFuel + " and the fullest has " + (int)Math.Floor(fullest) + ".", '\0', 8);
                    continue;
                }
                actions.AddActionWithHint("BakeClay", label, BakeCommand + flask.ID + "|" + vessel.Volume.ToString(CultureInfo.InvariantCulture),
                    "Boils the water off bog-mire and bakes what is left into fire clay: " + ClayRatio + " mire to 1, and the lit fire spends " + ClayFuel + " fuel.", '\0', 8, false, ActionTime.OneAction);
            }
        }

        // ── baking clay ──

        bool TryBake(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (f.Length != 3 || !int.TryParse(f[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int volume)) return Reject(actor, "malformed_selection", "That is not a batch you can bake.");
            if (zone == null || actor == null || !WorldResourceActions.ActorCurrent(actor, zone) || CombatSystem.IsDeathHandled(actor) || (actor.GetStat("Hitpoints") is Stat hp && hp.Value <= 0)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || !Standing || zone.GetEntityCell(ParentEntity) == null || SpatialQuery.Distance(zone, actor, ParentEntity) > 1 || AlchemyStillPart.Factory == null)
                return Reject(actor, "actor_unavailable", "You cannot work the still right now.");
            var pack = actor.GetPart<InventoryPart>();
            var flask = pack?.Objects.FirstOrDefault(o => o != null && o.ID == f[1] && WorldResourceActions.Carried(actor, o));
            var vessel = flask?.GetPart<LiquidVesselPart>();
            if (vessel == null || vessel.LiquidId != Feed || vessel.Volume != volume) return Reject(actor, "stale_selection", "The flask is not as it was.");
            if (volume < ClayRatio) return Reject(actor, "too_little", "Baking clay needs at least " + ClayRatio + " units of mire.");
            var fire = FindFire(zone, out float fullest, ClayFuel);
            if (fire == null)
                return fullest < 0 ? Reject(actor, "no_fire", NoFireToBake(zone))
                    : Reject(actor, "fuel_short", "No fire here can spare the fuel: baking burns " + ClayFuel + " and the fullest has " + (int)Math.Floor(fullest) + ".");
            var clay = AlchemyStillPart.Factory.CreateEntity(ClayBlueprint); if (clay == null) return Reject(actor, "no_clay", "The clay would not take.");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(flask, actor, command) || !tx.TryClaim(ParentEntity, actor, command) || !tx.TryClaim(fire, actor, command))
                    return Reject(actor, "in_progress", "The still is already being worked.");
                var fuel = fire.GetPart<FuelPart>(); float fuelBefore = fuel.FuelMass; string idBefore = vessel.LiquidId; int left = volume - ClayRatio;
                var receipt = InventoryTransferSnapshot.Capture(pack, clay);
                tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.AddObject(clay)) || !receipt.ClaimChanges(tx, actor, command))   // a second clay merges into the first stack, so the new entity itself need not be in the pack
                    return Reject(actor, "no_room", "You cannot carry the clay.");
                tx.Do(() => { vessel.Volume = left; if (left == 0) vessel.LiquidId = ""; fuel.FuelMass = fuelBefore - ClayFuel; }, () => { vessel.Volume = volume; vessel.LiquidId = idBefore; fuel.FuelMass = fuelBefore; });
                var owner = ParentEntity;
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You boil the water off " + ClayRatio + " mire at the fire and bake what is left: one fire clay." + (left > 0 ? " " + left + " mire stays in the flask." : ""));
                    Diag.Record("liquid", "Baked", actor, owner, new { from = Feed, units = ClayRatio, yield = 1, fuel = ClayFuel, left });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
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
            bool vent = FindVent(zone) != null; float fullest = -1f; Entity fire = vent ? null : FindFire(zone, out fullest);
            if (!vent && fire == null)
                return fullest < 0 ? Reject(actor, "no_fire", "Settling needs a lit fire beside the still.")
                    : Reject(actor, "fuel_short", "No fire here can spare the fuel: a batch burns " + FuelPerBatch + " and the fullest has " + (int)Math.Floor(fullest) + ".");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(flask, actor, command) || !tx.TryClaim(ParentEntity, actor, command) || (fire != null && !tx.TryClaim(fire, actor, command)))
                    return Reject(actor, "in_progress", "The still is already being worked.");
                var fuel = fire?.GetPart<FuelPart>(); float fuelBefore = fuel != null ? fuel.FuelMass : 0f; int yield = volume / Ratio; string target = f[1]; float spend = fuel != null ? FuelPerBatch : 0f;
                tx.Do(() => { vessel.LiquidId = target; vessel.Volume = yield; if (fuel != null) fuel.FuelMass = fuelBefore - FuelPerBatch; }, () => { vessel.LiquidId = Feed; vessel.Volume = volume; if (fuel != null) fuel.FuelMass = fuelBefore; });
                var owner = ParentEntity;
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You settle " + volume + " mire " + (vent ? "on the vent's heat" : "over the fire") + ": " + yield + " " + target + " runs into the flask. The dregs stay in the pot.");
                    Diag.Record("liquid", "Settled", actor, owner, new { from = Feed, to = target, units = volume, yield, fuel = spend, heat = vent ? "vent" : "fire" });
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
                string target = f[1]; int batches = 0, produced = 0; var owner = ParentEntity; bool vent = FindVent(zone) != null;
                foreach (var flask in flasks)
                {
                    var fire = vent ? null : FindFire(zone, out _); if (!vent && fire == null) break;
                    if (!transaction.TryClaim(flask, actor, command) || (fire != null && !transaction.TryClaim(fire, actor, command))) return Reject(actor, "in_progress", "The still is already being worked.");
                    var vessel = flask.GetPart<LiquidVesselPart>(); var fuel = fire?.GetPart<FuelPart>(); float fuelBefore = fuel != null ? fuel.FuelMass : 0f; int volume = vessel.Volume, yield = volume / Ratio; float spend = fuel != null ? FuelPerBatch : 0f;
                    transaction.Do(() => { vessel.LiquidId = target; vessel.Volume = yield; if (fuel != null) fuel.FuelMass = fuelBefore - FuelPerBatch; }, () => { vessel.LiquidId = Feed; vessel.Volume = volume; if (fuel != null) fuel.FuelMass = fuelBefore; });
                    transaction.AfterCommit(() => Diag.Record("liquid", "Settled", actor, owner, new { from = Feed, to = target, units = volume, yield, fuel = spend, heat = vent ? "vent" : "fire" }));
                    batches++; produced += yield;
                }
                if (batches == 0)
                    return FindFire(zone, out float fullest) == null && fullest < 0 ? Reject(actor, "no_fire", "Settling needs a lit fire beside the still.")
                        : Reject(actor, "fuel_short", "No fire here can spare the fuel: a batch burns " + FuelPerBatch + ".");
                int done = batches, total = flasks.Length, made = produced;
                transaction.AfterCommit(() => MessageLog.Add("You settle " + done + (done == total ? " flasks" : " of " + total + " flasks") + " of mire " + (vent ? "on the vent's heat" : "over the fire") + ": " + made + " " + target + ". The dregs stay in the pot."));
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
