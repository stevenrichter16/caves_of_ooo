using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>A reusable single-liquid vessel. Volume uses the same units as
    /// LiquidPoolPart; its identity and capacity persist through the normal save
    /// stream. Unlike the clean-water waterskin it has no drinking action.</summary>
    public sealed class LiquidVesselPart : Part, IToolMenuState, IDescribeUse, IInventoryActionReceiptOwner
    {
        public override string Name => "LiquidVessel";
        public int Capacity = 12;
        public int Volume;
        public string LiquidId = "";
        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<LiquidVesselPart>() == this && AcidRinseActions.IsCommand(command);

        internal static bool HasCarriedOil(Entity actor, Entity flask, bool allowEmpty = false)
        {
            var liquid = flask?.GetPart<LiquidVesselPart>();
            return PreparationActions.Carried(actor, flask) && WorldResourceActions.Carried(actor, flask)
                && liquid?.ParentEntity == flask && flask.Parts.Count(part => part is LiquidVesselPart) == 1
                && flask.Parts.Count(part => part is PhysicsPart) == 1 && flask.Parts.Count(part => part is StackerPart) <= 1
                && !flask.HasPart<WaterskinPart>() && liquid.Capacity > 0
                && liquid.Volume >= (allowEmpty ? 0 : 1) && liquid.Volume <= liquid.Capacity
                && liquid.LiquidId == (liquid.Volume == 0 ? "" : "oil")
                && LiquidRegistry.IsInitialized && LiquidRegistry.Get("oil") != null;
        }

        /// <summary>What a menu says to tell two flasks apart: "empty" or "11/12 water".</summary>
        public string ToolMenuState()
        {
            if (Capacity <= 0 || Volume < 0 || Volume > Capacity) return "contents unavailable";
            if (Volume == 0) return string.IsNullOrEmpty(LiquidId) ? "empty" : "contents unavailable";
            var liquid = LiquidRegistry.IsInitialized ? LiquidRegistry.Get(LiquidId) : null;
            return liquid == null || string.IsNullOrWhiteSpace(liquid.DisplayName) ? "contents unavailable"
                : Volume + "/" + Capacity + " " + liquid.DisplayName;
        }

        public int Order => 10;

        /// <summary>How to use a flask (the owner, 2026-11-21). The "right now" line is built from the rows the pack menu shows for it (fill, pour, douse, coat).</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("To fill it, stand beside a pool, well or other liquid, press C on it and choose fill (or choose fill from the pack). To pour, press C on your own cell or a cell beside you and choose pour or pour one.");
            guide.How.Add(AcidRinseActions.UseText);
            guide.Needs.Add("an unmixed liquid within reach to fill it: it takes one kind at a time");
            guide.How.Add("Oil can refill an unlit torch in your pack: use this flask and choose the torch. One oil adds up to 25 fuel; lamp-oil measures remain another option.");
            if (LiquidId == "oil" && Volume > 0)
                guide.How.Add("To feed finite coals, press C beside the hearth. Up to three whole oil units add one fuel each. Cold coals stay cold.");
            guide.Limits.Add("It holds up to " + Capacity + " units" + (Volume > 0 ? " and holds " + Volume + " now" : "") + ". What you pour acts on the ground it lands on: oil burns and slicks, water carries a charge and damps fire.");
            if (context.Viewer != null && !UseGuides.CarriedBy(context.Viewer, ParentEntity)) guide.WhyNot = UseGuides.PickUpFirst;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                VesselTorchRefueling.AddActions(actor, ParentEntity, actor?.SpatialZone, e.GetParameter<InventoryActionList>("Actions"));
                WaterTransferActions.AddActions(actor, ParentEntity, actor?.SpatialZone, e.GetParameter<InventoryActionList>("Actions"));
                LiquidTransferActions.AddActions(actor, ParentEntity, actor?.SpatialZone, e.GetParameter<InventoryActionList>("Actions"));
                DouseWorldActions.AddActions(actor, ParentEntity, actor?.SpatialZone, e.GetParameter<InventoryActionList>("Actions"));
                ProofingCoatActions.AddActions(actor, ParentEntity, actor?.SpatialZone, e.GetParameter<InventoryActionList>("Actions"));
                LiquidVesselService.AddActions(actor, ParentEntity, actor?.SpatialZone,
                    e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            if (VesselTorchRefueling.IsCommand(command))
            {
                if (!VesselTorchRefueling.TryAct(e.GetParameter<Entity>("Actor"), ParentEntity, e.GetParameter<Zone>("Zone"),
                    command, e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
                e.Handled = true; return false;
            }
            if (LiquidTransferActions.IsCommand(command))
            {
                if (!LiquidTransferActions.TryAct(e.GetParameter<Entity>("Actor"), ParentEntity, e.GetParameter<Zone>("Zone"),
                    command, e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
                e.Handled = true; return false;
            }
            if (WaterTransferActions.IsCommand(command) || DouseWorldActions.IsCommand(command))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone"); var tx = e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction");
                bool success = WaterTransferActions.IsCommand(command) ? WaterTransferActions.TryAct(actor, ParentEntity, zone, command, tx)
                    : DouseWorldActions.TryAct(actor, ParentEntity, zone, command, tx);
                if (!success) return true; e.Handled = true; return false;
            }
            if (ProofingCoatActions.IsCommand(command))
            {
                if (!ProofingCoatActions.TryAct(e.GetParameter<Entity>("Actor"), ParentEntity, e.GetParameter<Zone>("Zone"), command,
                    e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
                e.Handled = true; return false;
            }
            if (!LiquidVesselService.IsLiquidCommand(command)) return true;
            if (!LiquidVesselService.TryAct(e.GetParameter<Entity>("Actor"), ParentEntity,
                e.GetParameter<Zone>("Zone"), command,
                e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true;
            return false;
        }
    }
}
