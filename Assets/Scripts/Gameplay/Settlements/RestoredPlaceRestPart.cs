using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Free sheltered rest at a physical structure whose saved repair is complete.</summary>
    public sealed class RestoredPlaceRestPart : Part, IDescribeState, IDescribeUse, IInventoryActionReceiptOwner
    {
        public override string Name => "RestoredPlaceRest";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 21;
        private const string Command = "RestAtRestoredPlaceSite";
        private const string Label = "rest under the guest-cloth";
        private static readonly string Hint = $"Free. {RestSystem.RestClockTurns} ticks pass. Restores health and stops bleeding.";

        internal static Entity GantryPole(Zone zone)
        {
            if (zone?.ZoneID != GantryCompositionPlan.ZoneID) return null;
            var poles = zone.GetReadOnlyEntities().Where(entity => entity.BlueprintName == "GuestClothPole"
                && entity.GetProperty("SettlementId") == zone.ZoneID).Take(2).ToArray();
            if (poles.Length != 1) return null;
            var pole = poles[0];
            return pole.HasTag("Furniture") && pole.GetPart<PhysicsPart>() is { Takeable: false, Solid: false }
                && WorldResourceActions.Ground(pole, zone) && zone.GetReadOnlyEntities().Count(entity => entity.ID == pole.ID) == 1
                ? pole : null;
        }

        internal static void BindGantry(Zone zone)
        {
            var pole = GantryPole(zone);
            if (pole == null) return;
            if (pole.GetPart<PlaceSiteObjectPart>() == null)
                pole.AddPart(new PlaceSiteObjectPart
                {
                    Place = "Gantry", SiteId = "GuestPole", BrokenWord = "torn", MendedWord = "hung"
                });
            if (pole.GetPart<RestoredPlaceRestPart>() == null)
                pole.AddPart(new RestoredPlaceRestPart());
        }

        private RepairableSiteState WorkingSite(Zone zone)
        {
            var owner = ParentEntity;
            if (owner == null || owner.GetPart<RestoredPlaceRestPart>() != this || GantryPole(zone) != owner
                || owner.GetPart<PlaceSiteObjectPart>() is not { Place: "Gantry", SiteId: "GuestPole" } place
                || PlaceSiteCatalog.Get(place.Place, place.SiteId)?.OffersShelteredRest != true) return null;
            var world = WorldLocationContext.For(zone);
            return world != null && world.CachedZones.TryGetValue(zone.ZoneID, out var active) && active == zone
                && world.SettlementManager.TryGetExistingSite(zone.ZoneID, place.SiteId, out var site)
                && site.Stage == RepairStage.StableRepair ? site : null;
        }

        public string DescribeState(DescribeContext context) => WorkingSite(ParentEntity?.SpatialZone) == null
            ? null : "You can rest under the hung cloth when the area is safe.";

        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            if (WorkingSite(ParentEntity?.SpatialZone) == null) return;
            guide.How.Add("Stand beside the hung cloth and choose rest when hurt or bleeding.");
            guide.Limits.Add("Rest is free and time passes. Nearby danger interrupts it.");
        }

        public bool OwnsInventoryReceipt(string command) => command == Command;

        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            var zone = PreparationActions.ZoneFor(e, actor);
            if (e.ID == "GetInventoryActions" && WorkingSite(zone) != null && WorldResourceActions.Nearby(actor, ParentEntity, zone))
            {
                var rows = e.GetParameter<InventoryActionList>("Actions");
                string refusal = RestRefusal(actor, zone, TurnManager.Active);
                if (refusal == null) rows?.AddActionWithHint("ShelteredRest", Label, Command, Hint, 'r', 20, time: ActionTime.Free);
                else rows?.AddDisabled("ShelteredRest", Label, "rest_unavailable", refusal, 'r', 20);
            }
            if (e.ID == "InventoryAction" && OwnsInventoryReceipt(e.GetStringParameter("Command"))
                && TryRest(actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction")))
            {
                e.Handled = true;
                return false;
            }
            return true;
        }

        private string RestRefusal(Entity actor, Zone zone, TurnManager clock)
        {
            if (WorldLocationContext.For(zone)?.ActiveZone != zone
                || !WorldResourceActions.Nearby(actor, ParentEntity, zone)
                || zone.GetEntityCell(ParentEntity)?.IsVisible != true
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || clock == null || clock.TickCount < 0 || clock.TickCount > int.MaxValue - RestSystem.RestClockTurns)
                return "You cannot settle down here right now.";
            if (RestSystem.HostileNear(actor, zone) != null) return "It is too dangerous to rest here.";
            if ((actor.GetStat("Hitpoints") is not Stat hp || hp.Value >= hp.Max) && actor.GetEffect<BleedingEffect>() == null)
                return "You are already in good shape.";
            return null;
        }

        private bool TryRest(Entity actor, Zone zone, InventoryTransaction transaction)
        {
            var site = WorkingSite(zone);
            var clock = TurnManager.Active;
            if (site == null || transaction == null) return false;
            string refusal = RestRefusal(actor, zone, clock);
            if (refusal != null) { MessageLog.Add(refusal); return false; }
            var owner = ParentEntity;
            var standing = zone.GetEntityCell(actor);
            var shelter = zone.GetEntityCell(owner);
            int tick = clock.TickCount;
            if (!transaction.TryClaim(owner, actor, Command)) return false;
            transaction.BeforeCommit(() => TurnManager.Active == clock && clock.TickCount == tick
                && ParentEntity == owner && WorkingSite(zone) == site && zone.GetEntityCell(owner) == shelter
                && zone.GetEntityCell(actor) == standing && RestRefusal(actor, zone, clock) == null);
            return RestSystem.TryRestWithTransaction(actor, zone, "guest-cloth", out _, transaction);
        }
    }
}
