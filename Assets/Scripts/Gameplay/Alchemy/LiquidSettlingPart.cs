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
    /// and nothing is created: output times four never exceeds the feed. Brine uses the separate heat-paid distillation batch. The still is the same
    /// station whether communal or a set-down camp kit; a carried kit offers nothing.
    /// <para>Fire is coupled, not assumed: the batch burns <see cref="FuelPerBatch"/> from a burning fire within <see cref="FireReach"/> of the still that can spare it, and refuses up front
    /// with the numbers otherwise.</para>
    /// </summary>
    public sealed class LiquidSettlingPart : Part, IDescribeState, IDescribeUse, IInventoryActionReceiptOwner
    {
        public override string Name => "LiquidSettling";
        public bool OwnsInventoryReceipt(string command) => BrineDistillation.IsCommand(command);
        internal bool DistillationStanding => Standing;
        internal Entity DistillationFire(Zone zone) => FindFire(zone, out _, BrineDistillation.Fuel);
        internal bool DistillationFireReady(Zone zone, Entity fire, int need)
        {
            var at = zone?.GetEntityCell(ParentEntity); var there = fire == null ? null : zone?.GetEntityCell(fire);
            return at != null && there != null && WorldResourceActions.Ground(fire, zone)
                && (need == 0 ? fire.GetPart<ThermalPart>() != null && WorldResourceActions.Finite(fire.GetPart<ThermalPart>().Temperature)
                    && (fire.GetPart<CampfirePart>()?.FiniteCooking == true ? fire.GetPart<ThermalPart>().Temperature >= CookingService.MinimumFiniteCookingTemperature : fire.GetPart<ThermalPart>().IsAflame)
                    && !(fire.GetEffect<FrozenEffect>()?.Cold > 0) && !(fire.GetEffect<WetEffect>()?.Moisture > WetLimit) : Burning(fire, ParentEntity))
                && fire.GetPart<FuelPart>()?.FuelMass >= need
                && Math.Max(Math.Abs(at.X - there.X), Math.Abs(at.Y - there.Y)) <= FireReach;
        }

        public const string Command = "SettleLiquid|", AllCommand = "SettleAll|", Feed = "bog-mire", Water = "water", Oil = "oil";
        public const int Ratio = 4, FuelPerBatch = 5, FireReach = 2;

        /// <summary>Firing: baking mire into fire clay (Docs/MACHINE-INVENTED-USES.md 3j). Eight mire to one clay, ten of the lit fire's fuel; the rest of the flask stays as mire.</summary>
        public const string BakeCommand = "BakeClay|", ClayBlueprint = "FireClay";
        public const int ClayRatio = 8, ClayFuel = 10;
        const string InvalidBakeVolume = "That flask cannot hold the selected mire.";

        /// <summary>The small text under a settle row: what it turns the mire into, at what rate, and what the fire pays.</summary>
        static string SettleHint(string output, string heat) => "Settles bog-mire into clean " + output + ", " + Ratio + " mire to 1. " + heat;
        const float WetLimit = 0.35f;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                BrineDistillation.AddRows(actor, this, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone, e.GetParameter<InventoryActionList>("Actions"));
                AddRows(actor, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone, e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (BrineDistillation.IsCommand(command))
                {
                    var who = e.GetParameter<Entity>("Actor");
                    if (!BrineDistillation.TryAct(who, this, e.GetParameter<Zone>("Zone") ?? who?.SpatialZone ?? SettlementRuntime.ActiveZone, command, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
                    e.Handled = true; return false;
                }
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

        /// <summary>The nearest burning fire that can spare a batch (ties by ID), and the fullest nearby burning fire.</summary>
        Entity FindFire(Zone zone, out Entity fullest, float need = FuelPerBatch)
        {
            fullest = null; Entity best = null; int bestDistance = int.MaxValue;
            var cell = zone?.GetEntityCell(ParentEntity); if (cell == null) return null;
            foreach (var other in zone.GetReadOnlyEntities())
            {
                if (!Burning(other, ParentEntity) || !WorldResourceActions.Ground(other, zone)) continue;
                var at = zone.GetEntityCell(other); if (at == null) continue;
                int distance = Math.Max(Math.Abs(at.X - cell.X), Math.Abs(at.Y - cell.Y)); if (distance > FireReach) continue;
                float fuel = other.GetPart<FuelPart>().FuelMass;
                if (fullest == null || fuel > fullest.GetPart<FuelPart>().FuelMass) fullest = other;
                if (fuel < need) continue;
                if (distance < bestDistance || (distance == bestDistance && best != null && string.CompareOrdinal(other.ID, best.ID) < 0)) { best = other; bestDistance = distance; }
            }
            return best;
        }

        /// <summary>The steady natural heat (a steam vent, an ash bed) within reach of the still, which pays a settling batch with no fuel but cannot bake clay; null when there is none.</summary>
        Entity FindVent(Zone zone) => NaturalHeat.Find(zone, zone?.GetEntityCell(ParentEntity), FireReach);

        /// <summary>Why baking has no fire: a vent beside the still settles mire but is not a kiln, and the text says so rather than just "no fire".</summary>
        static string NoFireToBake(bool hasVent) => hasVent
            ? "The vent is not hot enough to bake clay. Baking needs a lit fire beside the still."
            : "Baking clay needs a lit fire beside the still.";

        static bool VisibleHeat(Zone zone, Entity source)
            => source != null && zone.GetEntityCell(source)?.IsVisible == true && source.GetPart<RenderPart>() is { Visible: true };

        static string VisibleFuelShortfall(Zone zone, Entity fullest)
            => VisibleHeat(zone, fullest) ? " The fullest nearby fire has " + Math.Floor(fullest.GetPart<FuelPart>().FuelMass) + "." : "";

        (Entity Vent, Entity Fire, string Code, string Reason) SettlingHeat(Zone zone)
        {
            var vent = FindVent(zone);
            if (vent != null) return (vent, null, null, null);
            var fire = FindFire(zone, out var fullest);
            if (fire != null) return (null, fire, null, null);
            return fullest == null
                ? (null, null, "no_fire", "Settling needs a lit fire beside the still.")
                : (null, null, "fuel_short", "Settling burns " + FuelPerBatch + " fuel per flask." + VisibleFuelShortfall(zone, fullest));
        }

        (Entity Fire, string Code, string Reason) BakingHeat(Zone zone, bool hasVent, InventoryPart pack)
        {
            var fire = FindFire(zone, out var fullest, ClayFuel);
            if (fire == null)
                return fullest == null ? (null, "no_fire", NoFireToBake(hasVent))
                    : (null, "fuel_short", "Baking burns " + ClayFuel + " fuel." + VisibleFuelShortfall(zone, fullest));
            var fuel = fire.GetPart<FuelPart>();
            if (!WorldResourceActions.Finite(fuel.MaxFuel) || fuel.MaxFuel < fuel.FuelMass || fuel.FuelMass - ClayFuel >= fuel.FuelMass)
                return (null, "invalid_inputs", "The nearby fire cannot supply a measured clay batch.");
            int weight = SiftableStockPart.Weight(AlchemyStillPart.Factory, ClayBlueprint);
            if (weight >= 0 && pack.MaxWeight >= 0 && (long)pack.GetCarriedWeight() + weight > pack.MaxWeight)
                return (null, "no_room", "Leave room for " + weight + " lb of fire clay.");
            return (fire, null, null);
        }

        static bool BakeVolume(LiquidVesselPart vessel, int volume)
            => vessel.Capacity > 0 && volume >= ClayRatio && volume <= vessel.Capacity;

        static string FuelQuote(Zone zone, Entity fire, int cost)
            => "Uses " + cost + " fuel from a nearby fire." + (VisibleHeat(zone, fire)
                ? " Ready now: " + fire.GetDisplayName() + " ("
                    + fire.GetPart<FuelPart>().FuelMass.ToString("0.##", CultureInfo.InvariantCulture) + " fuel)." : "");

        static string SettlingQuote(Zone zone, Entity vent, Entity fire, int batches)
        {
            if (vent != null) return "The still can use steady heat from " + (VisibleHeat(zone, vent) ? vent.GetDisplayName() : "a nearby vent or ash bed") + " right now.";
            return batches == 1 ? FuelQuote(zone, fire, FuelPerBatch)
                : "Uses " + (batches * (long)FuelPerBatch) + " fuel across nearby fires.";
        }

        static string BakeQuote(Zone zone, Entity fire, int volume)
            => FuelQuote(zone, fire, ClayFuel) + (volume == ClayRatio ? " The flask empties."
                : " " + (volume - ClayRatio) + " mire stays in the flask.");

        /// <summary>Batches the burning fires within reach could pay for between them (each fire spares whole batches of <see cref="FuelPerBatch"/>).</summary>
        int Capacity(Zone zone, int requested)
        {
            int batches = 0; var cell = zone?.GetEntityCell(ParentEntity); if (cell == null) return 0;
            foreach (var other in zone.GetReadOnlyEntities())
            {
                if (!Burning(other, ParentEntity) || !WorldResourceActions.Ground(other, zone)) continue;
                var at = zone.GetEntityCell(other); if (at == null || Math.Max(Math.Abs(at.X - cell.X), Math.Abs(at.Y - cell.Y)) > FireReach) continue;
                batches += (int)Math.Min(requested - batches, Math.Floor(other.GetPart<FuelPart>().FuelMass / FuelPerBatch));
                if (batches == requested) break;
            }
            return batches;
        }

        Entity[] CarriedMire(Entity actor)
            => actor?.GetPart<InventoryPart>()?.Objects.Where(o => o?.GetPart<LiquidVesselPart>() is LiquidVesselPart v
                && v.LiquidId == Feed && v.Volume > 0 && WorldResourceActions.Carried(actor, o)).ToArray() ?? Array.Empty<Entity>();

        static Entity[] SettleableFlasks(Entity[] flasks)
            => flasks.Where(o => o.GetPart<LiquidVesselPart>().Volume >= Ratio).OrderBy(o => o.ID, StringComparer.Ordinal).ToArray();

        bool Standing => ParentEntity?.GetPart<PhysicsPart>()?.InInventory == null && !PackableFixturePart.IsInertKit(ParentEntity);

        // "What can I do here?": what the mire you carry would settle to at this still, or what it is missing, in both examine readers.
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 50;
        public string DescribeState(DescribeContext context)
        {
            var actor = context.Viewer; var zone = context.Zone ?? ParentEntity?.SpatialZone;
            if (actor == null || zone == null || !Standing || zone.GetEntityCell(ParentEntity) == null) return null;
            var flasks = CarriedMire(actor);
            if (flasks.Length == 0) return null;
            var settleable = SettleableFlasks(flasks);
            if (settleable.Length == 0) return "Settling needs at least " + Ratio + " mire in one flask.";
            var heat = SettlingHeat(zone);
            string state = heat.Reason;
            if (state == null)
            {
                int batches = heat.Vent != null ? settleable.Length : Capacity(zone, settleable.Length);
                int yield = settleable.Take(batches).Sum(o => o.GetPart<LiquidVesselPart>().Volume / Ratio);
                string heatText = SettlingQuote(zone, heat.Vent, heat.Fire, batches);
                state = "You can settle your mire into " + yield + " clean water or oil from " + batches
                    + (batches == 1 ? " flask. " : " flasks. ") + heatText;
            }
            if (AlchemyStillPart.Factory == null) return state;
            var bakingFlasks = settleable.Select(o => o.GetPart<LiquidVesselPart>()).Where(v => v.Volume >= ClayRatio).ToArray();
            if (bakingFlasks.Length == 0) return state;
            var bakeable = bakingFlasks.Where(v => BakeVolume(v, v.Volume)).ToArray();
            if (bakeable.Length == 0) return state + "\n" + InvalidBakeVolume;
            var bake = BakingHeat(zone, heat.Vent != null, actor.GetPart<InventoryPart>());
            if (bake.Reason != null) return state + "\n" + bake.Reason;
            return state + "\nYou can bake " + ClayRatio + " of it into fire clay. " + (bakeable.Length == 1
                ? BakeQuote(zone, bake.Fire, bakeable[0].Volume)
                : FuelQuote(zone, bake.Fire, ClayFuel) + " The remaining mire stays in the selected flask.");
        }

        /// <summary>How to settle and bake mire at the still (the owner, 2026-11-21), from the live constants. The "right now" line is built from <see cref="AddRows"/>'s rows (disabled with the reason when the fire is short).</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            if (!Standing) return;
            guide.How.Add("Distil 4 brine with 5 fuel into a separate empty flask. Choose 2 water or 1 water and a pound of brine crust at your feet.");
            guide.Needs.Add("to keep brine crust: a clear, dry cell beside the still");
            guide.How.Add("With a flask of bog-mire in your pack, press C beside it and choose settle (to clean water or to oil), or bake to boil mire down to fire clay.");
            guide.Needs.Add("to settle: a flask with at least " + Ratio + " units of bog-mire, and either a burning fire within " + FireReach + " cells that can spare " + FuelPerBatch + " fuel a flask or a steam vent or ash bed within " + FireReach + " cells");
            guide.Needs.Add("to bake clay: a flask with at least " + ClayRatio + " units, and a burning fire within " + FireReach + " cells that can spare " + ClayFuel + " fuel (a vent is not hot enough)");
            guide.Limits.Add("Settling uses the whole flask of mire and gives 1 water or oil for every " + Ratio + " mire. Baking spends " + ClayRatio + " mire for 1 fire clay and leaves any extra mire in the flask.");
        }

        // ── rows ──

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !Standing || !WorldResourceActions.ActorCurrent(actor, zone) || zone.GetEntityCell(ParentEntity) == null) return;
            var flasks = CarriedMire(actor);
            if (flasks.Length == 0) return;   // the zone scan for a fire is only worth doing for a player who carries mire
            var heat = SettlingHeat(zone);
            string quote = heat.Reason == null ? SettlingQuote(zone, heat.Vent, heat.Fire, 1) : null;
            for (int n = 0; n < flasks.Length; n++)
            {
                var flask = flasks[n]; var vessel = flask.GetPart<LiquidVesselPart>(); string tag = flasks.Length > 1 ? " #" + (n + 1) : "";
                string code = vessel.Volume < Ratio ? "too_little" : heat.Code;
                string reason = vessel.Volume < Ratio ? "Settling needs at least " + Ratio + " mire in one flask." : heat.Reason;
                if (code != null) { actions.AddDisabled("SettleLiquid", "settle " + vessel.Volume + " mire" + tag, code, reason, '\0', 12); continue; }
                int yield = vessel.Volume / Ratio;
                actions.AddActionWithHint("SettleLiquid", "settle " + vessel.Volume + " mire to " + yield + " water" + tag, CommandFor(Water, flask, vessel), SettleHint(Water, quote), '\0', 12, false, ActionTime.OneAction);
                actions.AddActionWithHint("SettleLiquid", "settle " + vessel.Volume + " mire to " + yield + " oil" + tag, CommandFor(Oil, flask, vessel), SettleHint(Oil, quote), '\0', 11, false, ActionTime.OneAction);
            }
            // Two or more flasks: one row for all of them, saying how many the fires could pay for and what comes out.
            var settleable = SettleableFlasks(flasks);
            if (settleable.Length >= 2 && heat.Reason == null)
            {
                int batches = heat.Vent != null ? settleable.Length : Capacity(zone, settleable.Length);
                int yield = 0; for (int i = 0; i < batches; i++) yield += settleable[i].GetPart<LiquidVesselPart>().Volume / Ratio;
                string scope = batches == settleable.Length ? "all " + batches + " flasks" : batches + " of " + settleable.Length + " flasks";
                string batchQuote = SettlingQuote(zone, heat.Vent, heat.Fire, batches);
                actions.AddActionWithHint("SettleAll", "settle " + scope + " to " + yield + " water", AllCommand + Water, SettleHint(Water, batchQuote), '\0', 10, false, ActionTime.OneAction);
                actions.AddActionWithHint("SettleAll", "settle " + scope + " to " + yield + " oil", AllCommand + Oil, SettleHint(Oil, batchQuote), '\0', 9, false, ActionTime.OneAction);
            }
            AddBakeRows(actor, zone, actions, flasks, heat.Vent != null);
        }

        /// <summary>One "bake" row per flask holding enough mire: boils the water off and bakes what is left into one fire clay. Needs the camp's hands (the content factory), a lit fire that can spare ten fuel, and room in the pack.</summary>
        void AddBakeRows(Entity actor, Zone zone, InventoryActionList actions, Entity[] flasks, bool hasVent)
        {
            if (AlchemyStillPart.Factory == null) return;
            (Entity Fire, string Code, string Reason)? heat = null;
            for (int n = 0; n < flasks.Length; n++)
            {
                var flask = flasks[n]; var vessel = flask.GetPart<LiquidVesselPart>(); if (vessel.Volume < ClayRatio) continue;
                string tag = flasks.Length > 1 ? " #" + (n + 1) : "", label = "bake " + ClayRatio + (vessel.Volume > ClayRatio ? " of the " + vessel.Volume : "") + " mire to 1 fire clay" + tag;
                if (!BakeVolume(vessel, vessel.Volume))
                {
                    actions.AddDisabled("BakeClay", label, "invalid_volume", InvalidBakeVolume, '\0', 8);
                    continue;
                }
                heat ??= BakingHeat(zone, hasVent, actor.GetPart<InventoryPart>());
                var offer = heat.Value;
                if (offer.Reason != null)
                {
                    actions.AddDisabled("BakeClay", label, offer.Code, offer.Reason, '\0', 8);
                    continue;
                }
                actions.AddActionWithHint("BakeClay", label, BakeCommand + flask.ID + "|" + vessel.Volume.ToString(CultureInfo.InvariantCulture),
                    "Makes 1 fire clay. " + BakeQuote(zone, offer.Fire, vessel.Volume), '\0', 8, false, ActionTime.OneAction);
            }
        }

        // ── baking clay ──

        internal static bool OwnsBakeReceipt(Entity owner, string command) => owner!=null
            && owner.GetPart<LiquidSettlingPart>()?.ParentEntity==owner && command!=null
            && command.StartsWith(BakeCommand,StringComparison.Ordinal);

        bool TryBake(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f=command.Split('|');
            if(f.Length!=3 || !int.TryParse(f[2],NumberStyles.None,CultureInfo.InvariantCulture,out int volume))
                return Reject(actor,"malformed_selection","That is not a batch you can bake.");
            var owner=ParentEntity;var pack=actor?.GetPart<InventoryPart>();
            if(zone==null || actor==null || pack==null || AlchemyStillPart.Factory==null
                || !WorldResourceActions.Nearby(actor,owner,zone) || owner.GetPart<LiquidSettlingPart>()!=this || !Standing)
                return Reject(actor,"actor_unavailable","You cannot work the still right now.");
            var matches=pack.Objects.Where(o=>o?.ID==f[1]).ToArray();
            var flask=matches.Length==1?matches[0]:null;var vessel=flask?.GetPart<LiquidVesselPart>();
            if(!WorldResourceActions.Carried(actor,flask) || vessel?.ParentEntity!=flask || vessel.LiquidId!=Feed
                || vessel.Volume!=volume || !BakeVolume(vessel, volume))
                return Reject(actor,"stale_selection","The flask is not as it was, or has too little mire.");
            var offer = BakingHeat(zone, FindVent(zone) != null, pack);
            if (offer.Reason != null) return Reject(actor, offer.Code, offer.Reason);
            var fire = offer.Fire;
            var factory=AlchemyStillPart.Factory;var fuel=fire.GetPart<FuelPart>();var heat=fire.GetPart<ThermalPart>();
            float fuelBefore=fuel.FuelMass,temperature=heat.Temperature,maxFuel=fuel.MaxFuel;
            float paidFuel=fuelBefore-ClayFuel;
            int capacity=vessel.Capacity,left=volume-ClayRatio;
            var actorCell=zone.GetEntityCell(actor);var ownerCell=zone.GetEntityCell(owner);var fireCell=zone.GetEntityCell(fire);
            var actorPhysics=actor.GetPart<PhysicsPart>();var flaskPhysics=flask.GetPart<PhysicsPart>();
            var ownerPhysics=owner.GetPart<PhysicsPart>();var firePhysics=fire.GetPart<PhysicsPart>();
            var flaskParts=flask.Parts.ToArray();var ownerParts=owner.Parts.ToArray();var fireParts=fire.Parts.ToArray();
            string actorId=actor.ID,ownerId=owner.ID,flaskId=flask.ID,fireId=fire.ID;
            bool Current(int units,float mass)
            {
                var camp=fire.GetPart<CampfirePart>();
                return AlchemyStillPart.Factory==factory && ParentEntity==owner && owner.GetPart<LiquidSettlingPart>()==this
                    && WorldResourceActions.ActorCurrent(actor,zone) && actor.GetStatValue("Hitpoints")>0
                    && !CombatSystem.IsDeathHandled(actor) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true
                    && actor.GetPart<InventoryPart>()==pack && pack.ParentEntity==actor && actor.ID==actorId
                    && actor.GetPart<PhysicsPart>()==actorPhysics && zone.GetEntityCell(actor)==actorCell
                    && owner.ID==ownerId && Standing && WorldResourceActions.Ground(owner,zone) && zone.GetEntityCell(owner)==ownerCell
                    && SpatialQuery.Distance(zone,actor,owner)<=1 && owner.GetPart<PhysicsPart>()==ownerPhysics
                    && owner.Parts.SequenceEqual(ownerParts) && ownerParts.All(p=>p.ParentEntity==owner)
                    && flask.ID==flaskId && WorldResourceActions.Carried(actor,flask) && flask.GetPart<PhysicsPart>()==flaskPhysics
                    && flask.GetPart<LiquidVesselPart>()==vessel && vessel.ParentEntity==flask && vessel.Capacity==capacity
                    && vessel.Volume==units && vessel.LiquidId==(units==0?"":Feed)
                    && flask.Parts.SequenceEqual(flaskParts) && flaskParts.All(p=>p.ParentEntity==flask)
                    && fire.ID==fireId && WorldResourceActions.Ground(fire,zone) && zone.GetEntityCell(fire)==fireCell
                    && fire.GetPart<PhysicsPart>()==firePhysics && fire.GetPart<FuelPart>()==fuel && fuel.ParentEntity==fire
                    && fire.GetPart<ThermalPart>()==heat && heat.ParentEntity==fire && heat.Temperature==temperature
                    && fire.Parts.SequenceEqual(fireParts) && fireParts.All(p=>p.ParentEntity==fire)
                    && fuel.FuelMass==mass && fuel.MaxFuel==maxFuel && WorldResourceActions.Finite(maxFuel) && maxFuel>=fuelBefore
                    && WorldResourceActions.Finite(mass) && mass>=0 && paidFuel<fuelBefore
                    && (camp?.FiniteCooking==true?heat.Temperature>=CookingService.MinimumFiniteCookingTemperature:heat.IsAflame)
                    && !(fire.GetEffect<FrozenEffect>()?.Cold>0) && !(fire.GetEffect<WetEffect>()?.Moisture>WetLimit)
                    && Math.Max(Math.Abs(fireCell.X-ownerCell.X),Math.Abs(fireCell.Y-ownerCell.Y))<=FireReach;
            }
            if(!Current(volume,fuelBefore))return Reject(actor,"invalid_inputs","The still's inputs are not ready.");
            bool own=tx==null;tx??=new InventoryTransaction();
            try
            {
                if(!tx.TryClaimAction(actor) || !tx.TryClaim(actor,actor,command) || !tx.TryClaim(flask,actor,command)
                    || !tx.TryClaim(owner,actor,command) || !tx.TryClaim(fire,actor,command))
                    return Reject(actor,"in_progress","The still is already being worked.");
                // Factory events can perform arbitrary work. No callback may spend
                // this same action or source again, including through the same receipt.
                var clay=factory.CreateEntity(ClayBlueprint);
                var productPhysics=clay?.GetPart<PhysicsPart>();var productStack=clay?.GetPart<StackerPart>();
                if(!Current(volume,fuelBefore) || clay?.BlueprintName!=ClayBlueprint || string.IsNullOrEmpty(clay.ID)
                    || clay.SpatialZone!=null || clay.HasTag("Creature") || productPhysics?.ParentEntity!=clay || !productPhysics.Takeable
                    || productPhysics.InInventory!=null || productPhysics.Equipped!=null || productStack?.ParentEntity!=clay || productStack.StackCount!=1
                    || pack.Objects.Any(e=>e==clay||e?.ID==clay.ID) || zone.GetReadOnlyEntities().Any(e=>e.ID==clay.ID))
                    return Reject(actor,"state_changed","The still's inputs or prepared clay changed before payment.");
                tx.Do(()=>{vessel.Volume=left;vessel.LiquidId=left==0?"":Feed;fuel.FuelMass=paidFuel;},()=>
                {
                    if(flask.GetPart<LiquidVesselPart>()==vessel && vessel.ParentEntity==flask
                        && (vessel.LiquidId==Feed || vessel.Volume==0 && string.IsNullOrEmpty(vessel.LiquidId)))
                    {vessel.Volume=WaterQuantityUndo.Restore(vessel.Volume,-ClayRatio,vessel.Capacity);if(vessel.Volume>0)vessel.LiquidId=Feed;}
                    else Diag.Record("liquid","BakeFeedUndoIdentityChanged",actor,flask);
                    if(fire.GetPart<FuelPart>()==fuel && fuel.ParentEntity==fire && WorldResourceActions.Finite(fuel.FuelMass)
                        && WorldResourceActions.Finite(fuel.MaxFuel) && fuel.MaxFuel>=0)
                    {
                        double restored=(double)fuel.FuelMass+ClayFuel;
                        if(restored<0 || restored>fuel.MaxFuel)Diag.Record("liquid","BakeFuelUndoBoundary",actor,fire);
                        fuel.FuelMass=(float)Math.Max(0,Math.Min(fuel.MaxFuel,restored));
                    }
                    else Diag.Record("liquid","BakeFuelUndoIdentityChanged",actor,fire);
                });
                if(!InventoryUnitOutputReceipt.TryPlace(pack,actor,clay,tx,command,out var output)
                    || !Current(left,paidFuel) || !output.Current())return Reject(actor,"output_refused","You cannot carry that clay, or the selected inputs changed.");
                tx.BeforeCommit(()=>Current(left,paidFuel)&&output.Current());
                tx.AfterCommit(()=>
                {
                    MessageLog.Add("You boil the water off "+ClayRatio+" mire at the fire and bake what is left: one fire clay."+(left>0?" "+left+" mire stays in the flask.":""));
                    Diag.Record("liquid","Baked",actor,owner,new{from=Feed,units=ClayRatio,yield=1,fuel=ClayFuel,left});
                });
                if(own)tx.Commit();return true;
            }
            finally {if(own)tx.Rollback();}
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
            if (volume < Ratio) return Reject(actor, "too_little", "Settling needs at least " + Ratio + " mire in one flask.");
            var offer = SettlingHeat(zone);
            if (offer.Reason != null) return Reject(actor, offer.Code, offer.Reason);
            bool vent = offer.Vent != null; Entity fire = offer.Fire;
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
            var flasks = SettleableFlasks(CarriedMire(actor));
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
                {
                    var offer = SettlingHeat(zone);
                    return Reject(actor, offer.Code ?? "fuel_short", offer.Reason ?? "No fire here can spare fuel for another batch.");
                }
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
