using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>A reusable, water-only vessel. Units are individual drinks.
    /// Public fields persist through the existing part save stream.</summary>
    public sealed class WaterskinPart : Part, IToolMenuState, IDescribeUse, IDescribeUseRows
    {
        public override string Name => "Waterskin";
        public int Capacity = 3;
        public int Charges;

        /// <summary>What a menu says to tell two skins apart: "2/3".</summary>
        public string ToolMenuState() => Capacity <= 0 || Charges < 0 || Charges > Capacity ? "water state unavailable" : Charges + "/" + Capacity;

        public int Order => 10;

        /// <summary>The pack menu always lists drink and fill; describing states whether each would work (a skin that is empty cannot be drunk from, a full one or one with no fresh water beside you cannot be filled).</summary>
        public void AddUseRows(Entity actor, Zone zone, InventoryActionList rows)
        {
            if (rows == null || actor == null || actor.GetPart<InventoryPart>()?.Objects.Contains(ParentEntity) != true) return;
            zone = zone ?? actor.SpatialZone;
            WaterTransferActions.AddActions(actor, ParentEntity, zone, rows); DouseWorldActions.AddActions(actor, ParentEntity, zone, rows);
            string drink = "drink water (" + Charges + "/" + Capacity + ")";
            if (Charges > 0) rows.AddAction("DrinkWater", drink, "DrinkWaterskin", 'd', 20); else rows.AddDisabled("DrinkWater", drink, "empty", "The waterskin is empty.", 'd', 20);
            if (Charges >= Capacity) rows.AddDisabled("FillWater", "fill from nearby fresh water", "full", "The waterskin is full.", 'f', 19);
            else if (WaterVesselService.FreshSourceBeside(actor, zone) == null) rows.AddDisabled("FillWater", "fill from nearby fresh water", "no-water", "There is no fresh water beside you.", 'f', 19);
            else rows.AddAction("FillWater", "fill from nearby fresh water", "FillWaterskin", 'f', 19);
        }

        /// <summary>How to use it (the owner, 2026-11-21): drink, fill, douse. The "right now" line is built from the rows the pack menu shows for it.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Choose drink water from the pack (a drink relieves one layer of parch). To fill it, stand beside a well, cistern or standing fresh water, press C on the water and choose fill the waterskin.");
            guide.How.Add("To put a fire out, press C on the burning thing and choose douse.");
            guide.Needs.Add("fresh water within reach to fill it: a well, a cistern or a pool of fresh water");
            guide.Needs.Add("a charge in it to drink or douse");
            guide.Limits.Add("It holds " + Capacity + " drinks and holds " + Charges + " now. It carries water only.");
            if (context.Viewer != null && !UseGuides.CarriedBy(context.Viewer, ParentEntity)) guide.WhyNot = UseGuides.PickUpFirst;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == WorldInteractionSystem.WorldCellActionsEvent)
            {
                WaterVesselService.AddCellActions(e.GetParameter<Entity>("Actor"), ParentEntity, e.GetParameter<Zone>("Zone"), e.GetParameter<Cell>("Cell"), e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                if (actor?.GetPart<InventoryPart>()?.Objects.Contains(ParentEntity) != true) return true;
                AddUseRows(actor, e.GetParameter<Zone>("Zone") ?? actor.SpatialZone,
                    e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            if (WaterTransferActions.IsCommand(command) || DouseWorldActions.IsCommand(command))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone"); var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
                bool success = WaterTransferActions.IsCommand(command) ? WaterTransferActions.TryAct(actor, ParentEntity, zone, command, tx)
                    : DouseWorldActions.TryAct(actor, ParentEntity, zone, command, tx);
                if (!success) return true; e.Handled = true; return false;
            }
            if (!WaterVesselService.IsWaterCommand(command)) return true;
            if (!WaterVesselService.TryAct(e.GetParameter<Entity>("Actor"), ParentEntity,
                e.GetParameter<Zone>("Zone"), command, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true;
            return false;
        }
    }
}
