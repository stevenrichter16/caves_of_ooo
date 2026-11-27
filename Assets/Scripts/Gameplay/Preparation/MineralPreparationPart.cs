using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    public sealed class MineralPreparationPart : Part, IDescribeUse, IDescribeUseRows
    {
        public override string Name => "MineralPreparation";

        public int Order => 0;

        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            var modification = Modification(ParentEntity?.BlueprintName);
            if (modification == null) return;

            guide.How.Add("At a usable forge, open this mineral's inventory actions and choose an item to infuse.");
            guide.Needs.Add("a compatible unequipped item with a free enhancement slot");
            guide.Limits.Add("Infusion spends one unit of the mineral and fills one enhancement slot.");
            var effect = modification.DescribeEnhancement();
            if (!string.IsNullOrWhiteSpace(effect)) guide.Limits.Add(effect + ".");

            if (context.Viewer == null) return;
            if (!PreparationActions.Carried(context.Viewer, ParentEntity))
                guide.WhyNot = UseGuides.PickUpFirst;
            else if (!ForgePart.IsNearForge(context.Viewer, context.Zone))
                guide.WhyNot = "stand within reach of a usable forge";
            else
                guide.WhyNot = "carry a compatible unequipped item with a free enhancement slot";
        }

        public void AddUseRows(Entity actor, Zone zone, InventoryActionList rows)
        {
            if (ForgePart.IsNearForge(actor, zone))
                AddInfusionRows(actor, ParentEntity, Modification(ParentEntity?.BlueprintName), rows);
        }

        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var mineral = ParentEntity; var zone = PreparationActions.ZoneFor(e, actor);
            var modification = Modification(mineral.BlueprintName); string enhancement = "Enhancement" + mineral.BlueprintName;
            if (e.ID == "GetInventoryActions")
            {
                AddInfusionRows(actor, mineral, modification, e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            string command = e.GetStringParameter("Command");
            if (e.ID != "InventoryAction" || command == null || !command.StartsWith("InfuseMineral|", StringComparison.Ordinal)) return true;
            if (e.GetParameter<bool>("PreparationAttempted")) return true; e.SetParameter("PreparationAttempted", true);
            var item = WorldResourceActions.ExactCarried(actor, command.Substring(14)); var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (mineral.GetPart<MineralPreparationPart>() != this || modification == null || !Compatible(actor, item, modification, enhancement)
                || !ForgePart.IsNearForge(actor, zone) || !PreparationActions.Begin(actor, mineral, zone, tx, command) || !tx.TryClaim(item, actor, command))
            { PreparationActions.Reject(actor, mineral, command, "mineral_target_or_forge"); return true; }
            var pack = actor.GetPart<InventoryPart>(); var receipt = InventoryTransferSnapshot.Capture(pack);
            string display = item.GetPart<RenderPart>()?.DisplayName; int modifications = item.GetIntProperty("ModificationCount");
            bool hadCount = item.IntProperties.ContainsKey("ModificationCount"); var original = new HashSet<Part>(item.Parts); var introduced = new List<IItemEnhancement>(); bool restored = false;
            Action restore = () =>
            {
                if (restored) return; restored = true;
                foreach (var added in introduced)
                    if (item.Parts.Contains(added)) { added.Remove(item); item.RemovePart(added); }
                if (item.GetPart<RenderPart>() is RenderPart render) render.DisplayName = display;
                if (hadCount) item.SetIntProperty("ModificationCount", modifications); else item.IntProperties.Remove("ModificationCount");
                receipt.Restore();
            };
            tx.Do(null, restore);
            // The target is carried; never inherit a legacy tinker caller's equipped owner.
            var oldCrafter = MineralInfusionTinkerModification.CurrentCrafter; bool applied;
            try { MineralInfusionTinkerModification.CurrentCrafter = null; applied = modification.Apply(item, out _); }
            finally
            {
                // Freeze this call's actual additions before the outer command publishes callbacks.
                foreach (var part in item.Parts) if (part is IItemEnhancement added && !original.Contains(added)) introduced.Add(added);
                MineralInfusionTinkerModification.CurrentCrafter = oldCrafter;
            }
            if (!applied || !PreparationActions.Carried(actor, item) || !receipt.Apply(() => pack.TryConsumeOne(mineral))
                || !receipt.ClaimChanges(tx, actor, command)) { restore(); PreparationActions.Reject(actor, mineral, command, "infusion_or_transfer"); return true; }
            PreparationActions.Complete(tx, actor, item, command, "You work one " + InventoryPart.GetUnitDisplayName(mineral) + " into " + InventoryPart.GetUnitDisplayName(item) + ".");
            e.Handled = true; return false;
        }

        static void AddInfusionRows(Entity actor, Entity mineral, MineralInfusionTinkerModification modification,
            InventoryActionList rows)
        {
            if (rows == null || modification == null || !PreparationActions.Carried(actor, mineral)) return;

            string enhancement = "Enhancement" + mineral.BlueprintName;
            foreach (var target in actor.GetPart<InventoryPart>().Objects)
                if (Compatible(actor, target, modification, enhancement))
                    rows.AddAction("InfuseMineral" + target.ID,
                        "infuse " + InventoryPart.GetUnitDisplayName(target) + " at forge (1 " + InventoryPart.GetUnitDisplayName(mineral) + "; one enhancement slot)",
                        "InfuseMineral|" + PreparationActions.Escaped(target), '\0', 17);
        }

        static bool Compatible(Entity actor, Entity item, MineralInfusionTinkerModification modification, string enhancement) =>
            PreparationActions.Carried(actor, item) && (item.GetPart<StackerPart>()?.StackCount ?? 1) == 1
            && item.GetPart(enhancement) == null && modification.CanApply(item, out _);
        static MineralInfusionTinkerModification Modification(string name) => name switch
        { "PaleSalt" => new PaleSaltTinkerModification(), "ChoirIron" => new ChoirIronTinkerModification(), "GlowQuartz" => new GlowQuartzTinkerModification(), _ => null };
    }
}
