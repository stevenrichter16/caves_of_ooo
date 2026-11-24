using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Fixed public plot authority. Collectors never create or replace
    /// this owner; exact live ground references retain spatial authority.</summary>
    public sealed class RainCatchmentPart : Part
    {
        public override string Name => "RainCatchment";
        public Entity Ground;
        public string WorldKey = "", PlotKey = "";
        public bool Public;
        public int PolicyVersion, ActivatedAt = -1, LastWindow = -1, Spent;
        public const int Allowance = 3, Reach = 2;

        internal bool Bound(Zone zone, int now)
        {
            var owner = ParentEntity; var manager = WorldLocationContext.For(zone);
            if (!Public || PolicyVersion != RegionalWeather.PolicyVersion || ActivatedAt < 0 || now < ActivatedAt
                || LastWindow < -1 || LastWindow > now / RegionalWeather.WindowTicks || Spent < 0 || Spent > Allowance
                || LastWindow == -1 && Spent != 0 || string.IsNullOrEmpty(PlotKey) || string.IsNullOrEmpty(WorldKey)
                || manager?.Exploration?.WorldKey != WorldKey || !FixedOn(owner, Ground, zone)
                || owner.Parts.Count(p => p is RainCatchmentPart) != 1 || owner.GetPart<RainCatchmentPart>() != this) return false;
            var at = zone.GetEntityCell(owner);
            foreach (var other in zone.GetReadOnlyEntitiesWithPart<RainCatchmentPart>())
            {
                if (other == owner) continue;
                var claim = other.GetPart<RainCatchmentPart>(); var cell = zone.GetEntityCell(other);
                if (claim.PlotKey == PlotKey || cell != null
                    && Math.Abs(cell.X - at.X) <= Reach * 2 && Math.Abs(cell.Y - at.Y) <= Reach * 2) return false;
            }
            return true;
        }
        internal int Remaining(int window) => LastWindow == window ? Allowance - Spent : Allowance;
        internal static bool FixedOn(Entity owner, Entity ground, Zone zone)
        {
            if (owner == null || ground == null || owner == ground || zone == null || !ground.HasTag("Terrain")
                || !WorldResourceActions.Ground(owner, zone) || !WorldResourceActions.Ground(ground, zone)
                || owner.GetPart<PhysicsPart>().Takeable || ground.GetPart<PhysicsPart>().Takeable
                || owner.HasTag("Creature") || owner.HasPart<PackableFixturePart>() || owner.HasPart<InstallablePart>()
                || owner.Parts.Count(p => p is PhysicsPart) != 1 || ground.Parts.Count(p => p is PhysicsPart) != 1
                || owner.GetPart<RenderPart>()?.Visible != true) return false;
            var cell = zone.GetEntityCell(owner);
            return cell == zone.GetEntityCell(ground) && !cell.IsInterior
                && zone.GetReadOnlyEntities().Count(e => e == owner || e.ID == owner.ID) == 1
                && zone.GetReadOnlyEntities().Count(e => e == ground || e.ID == ground.ID) == 1;
        }
    }

    public sealed class RainCollectorPart : Part, IDescribeState, IDescribeUse
    {
        public override string Name => "RainCollector";
        public Entity Anchor, Ground;
        public bool Clean;
        public const string Prefix = "CollectRain|";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 45;
        static T Unique<T>(Entity e) where T : Part
        { var parts = e?.Parts.OfType<T>().ToArray(); return parts?.Length == 1 && parts[0].ParentEntity == e ? parts[0] : null; }
        internal static bool OwnsReceipt(Entity owner, string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true
            && Unique<RainCollectorPart>(owner) != null;

        bool Context(Entity actor, Zone zone, out RainCatchmentPart claim, out RegionalWeather.Observation weather)
        {
            claim = Unique<RainCatchmentPart>(Anchor); weather = default;
            int now = WorldClock.CurrentTick;
            if (!Clean || Unique<RainCollectorPart>(ParentEntity) != this || claim == null
                || !RainCatchmentPart.FixedOn(ParentEntity, Ground, zone) || !claim.Bound(zone, now)) return false;
            var at = zone.GetEntityCell(ParentEntity); var root = zone.GetEntityCell(Anchor);
            if (Math.Max(Math.Abs(at.X - root.X), Math.Abs(at.Y - root.Y)) > RainCatchmentPart.Reach) return false;
            if (actor != null && (!PreparationActions.CurrentActor(actor, zone)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || SpatialQuery.Distance(zone, actor, ParentEntity) > 1)) return false;
            weather = RegionalWeather.Current(zone, now, claim.PolicyVersion); return weather.Valid;
        }
        static bool Receiver(Entity actor, Entity owner, out int amount, out int capacity, bool needsRoom = true)
        {
            amount = capacity = 0;
            if (!WaterTransferActions.Vessel(actor, owner, out amount, out capacity)
                || Unique<PhysicsPart>(owner) == null || owner.HasTag("Creature") || owner.HasTag("Terrain")
                || owner.HasPart<SpatialFootprintPart>() || owner.Parts.Count(p => p is WaterskinPart || p is LiquidVesselPart) != 1
                || owner.HasPart<StackerPart>() && (Unique<StackerPart>(owner) == null || owner.GetPart<StackerPart>().StackCount != 1)) return false;
            return (!needsRoom || amount < capacity) && LiquidRegistry.IsInitialized && LiquidRegistry.Get("water") != null;
        }
        public string DescribeState(DescribeContext context)
        {
            var zone = context.Zone ?? ParentEntity?.SpatialZone;
            if (!Context(null, zone, out var claim, out var weather)) return "This collecting edge is unavailable; its fixed public catchment must remain intact.";
            return (weather.Raining ? "Mild showers." : "Fair weather; the edge collects during mild showers.")
                + " Shared catchment: " + claim.Remaining(weather.Window) + "/3 water available this interval. The frames share one allowance; missed rain is not stored.";
        }
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("During mild showers, stand beside the frame, press C and choose catch rain, then the exact carried flask or waterskin.");
            guide.Needs.Add("a compatible carried water vessel with room, and the intact public collecting edge");
            guide.Limits.Add("Both frames share three water per shower interval. This is optional water for existing uses; fair weather costs nothing.");
        }
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
            if (e.ID == "GetInventoryActions") { AddRows(actor, zone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            string command = e.GetStringParameter("Command");
            if (e.ID != "InventoryAction" || command?.StartsWith(Prefix, StringComparison.Ordinal) != true) return true;
            if (!TryCollect(actor, zone, command, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true; return false;
        }
        static string Encode(string text) => Uri.EscapeDataString(text ?? "");
        static string Num(int n) => n.ToString(CultureInfo.InvariantCulture);
        string Quote(Entity vessel, RainCatchmentPart claim, int window, int amount, int capacity)
            => Prefix + Encode(Anchor.ID) + "|" + Encode(ParentEntity.ID) + "|" + Encode(vessel.ID) + "|" + Num(window)
                + "|" + Num(claim.LastWindow) + "|" + Num(claim.Spent) + "|" + Encode(vessel.GetPart<LiquidVesselPart>()?.LiquidId ?? "skin")
                + "|" + Num(amount) + "|" + Num(capacity);
        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || !Context(actor, zone, out var claim, out var weather)) return;
            if (!weather.Raining || claim.Remaining(weather.Window) <= 0)
            { actions.AddDisabled("CollectRain", "catch rain", weather.Raining ? "spent" : "fair", weather.Raining ? "This catchment's allowance is used for now." : "There are no mild showers now. Ordinary water sources still work.", '\0', 19); return; }
            bool found = false;
            foreach (var item in actor.GetPart<InventoryPart>().Objects)
            {
                if (!Receiver(actor, item, out int units, out int cap)) continue;
                int take = Math.Min(cap - units, claim.Remaining(weather.Window)); found = true;
                actions.AddActionWithHint("CollectRain", "catch " + take + " water in " + item.GetDisplayName() + " (" + units + "→" + (units + take) + "/" + cap + ")",
                    Quote(item, claim, weather.Window, units, cap), "The selected vessel receives ordinary clean water. Both frames share three water this interval.", '\0', 19, false, ActionTime.OneAction);
            }
            if (!found) actions.AddDisabled("CollectRain", "catch rain", "receiver", "Carry a compatible flask or waterskin with room for water.", '\0', 19);
        }
        bool TryCollect(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            if (tx == null || !Context(actor, zone, out var claim, out var weather) || !weather.Raining) return false;
            var bits = command.Split('|');
            if (bits.Length != 10) return false;
            var vessel = WorldResourceActions.ExactCarried(actor, bits[3]);
            if (!Receiver(actor, vessel, out int before, out int capacity)
                || command != Quote(vessel, claim, weather.Window, before, capacity)) return false;
            int take = Math.Min(capacity - before, claim.Remaining(weather.Window)); if (take <= 0) return false;
            var owner = ParentEntity; var anchor = Anchor; var ground = Ground; var rootGround = claim.Ground;
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(owner, actor, command)
                || !tx.TryClaim(anchor, actor, command) || !tx.TryClaim(vessel, actor, command)) return false;
            var skin = vessel.GetPart<WaterskinPart>(); var liquid = vessel.GetPart<LiquidVesselPart>();
            var physics = vessel.GetPart<PhysicsPart>();
            var actorCell = zone.GetEntityCell(actor); var ownerCell = zone.GetEntityCell(owner); var anchorCell = zone.GetEntityCell(anchor);
            string aid = actor.ID, oid = owner.ID, rid = anchor.ID, vid = vessel.ID, world = claim.WorldKey, plot = claim.PlotKey;
            int activation = claim.ActivatedAt, policy = claim.PolicyVersion, last = claim.LastWindow, spent = claim.Spent;
            int expectedSpent = last == weather.Window ? spent + take : take;
            var water = LiquidRegistry.Get("water");
            tx.Do(() => { claim.LastWindow = weather.Window; claim.Spent = expectedSpent; WaterTransferActions.SetUnits(vessel, before + take); }, () =>
            {
                // Undo only this action's still-present quantity. Do not rewind
                // independent injury, custody, replacement Parts or added water.
                bool receiverOwned = skin != null ? skin.ParentEntity == vessel && Unique<WaterskinPart>(vessel) == skin
                    : liquid.ParentEntity == vessel && Unique<LiquidVesselPart>(vessel) == liquid && liquid.LiquidId == "water";
                if (receiverOwned)
                {
                    if (skin != null) skin.Charges = WaterQuantityUndo.Restore(skin.Charges, take, Math.Min(capacity, Math.Max(0, skin.Capacity)));
                    else { liquid.Volume = WaterQuantityUndo.Restore(liquid.Volume, take, Math.Min(capacity, Math.Max(0, liquid.Capacity))); liquid.LiquidId = liquid.Volume == 0 ? "" : "water"; }
                }
                else CavesOfOoo.Diagnostics.Diag.Record("weather", "RainRollbackLimited", actor, vessel,
                    new { reason = "receiver-part-or-liquid-independently-replaced" });
                if (claim.ParentEntity == anchor && Unique<RainCatchmentPart>(anchor) == claim
                    && claim.Ground == rootGround && claim.WorldKey == world && claim.PlotKey == plot
                    && claim.PolicyVersion == policy && claim.ActivatedAt == activation
                    && claim.LastWindow == weather.Window && claim.Spent >= 0 && claim.Spent <= RainCatchmentPart.Allowance)
                {
                    int independent = claim.Spent - expectedSpent;
                    if (independent == 0) { claim.LastWindow = last; claim.Spent = spent; }
                    else claim.Spent = Math.Max(0, Math.Min(RainCatchmentPart.Allowance, claim.Spent - take));
                }
            });
            tx.BeforeCommit(() => Context(actor, zone, out var current, out var currentWeather) && current == claim
                && currentWeather.Window == weather.Window && currentWeather.Raining && ParentEntity == owner
                && Anchor == anchor && Ground == ground && claim.Ground == rootGround && actor.ID == aid && owner.ID == oid && anchor.ID == rid && vessel.ID == vid
                && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(owner) == ownerCell && zone.GetEntityCell(anchor) == anchorCell
                && claim.WorldKey == world && claim.PlotKey == plot && claim.ActivatedAt == activation && claim.PolicyVersion == policy
                && claim.LastWindow == weather.Window && claim.Spent == expectedSpent
                && vessel.GetPart<PhysicsPart>() == physics && vessel.GetPart<WaterskinPart>() == skin && vessel.GetPart<LiquidVesselPart>() == liquid
                && Receiver(actor, vessel, out int currentUnits, out int currentCap, false) && currentUnits == before + take && currentCap == capacity
                && LiquidRegistry.Get("water") == water);
            tx.AfterCommit(() => { MessageLog.Add("You catch " + take + " water in the " + vessel.GetDisplayName() + ". " + (RainCatchmentPart.Allowance - expectedSpent) + " remain in this shared catchment interval.");
                CavesOfOoo.Diagnostics.Diag.Record("weather", "RainCollected", actor, owner, new { anchor = rid, recipient = vid, window = weather.Window, amount = take, spent = expectedSpent }); });
            return true;
        }
    }
}
