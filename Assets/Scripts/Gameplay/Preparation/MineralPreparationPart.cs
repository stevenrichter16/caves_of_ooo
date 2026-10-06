using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    public sealed class MineralPreparationPart : Part
    {
        public override string Name => "MineralPreparation";
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var mineral = ParentEntity; var zone = PreparationActions.ZoneFor(e, actor);
            var modification = Modification(mineral.BlueprintName); string enhancement = "Enhancement" + mineral.BlueprintName;
            if (e.ID == "GetInventoryActions")
            {
                if (modification != null && PreparationActions.Carried(actor, mineral))
                    foreach (var target in actor.GetPart<InventoryPart>().Objects)
                        if (Compatible(actor, target, modification, enhancement))
                            e.GetParameter<InventoryActionList>("Actions")?.AddAction("InfuseMineral" + target.ID,
                                "infuse " + InventoryPart.GetUnitDisplayName(target) + " at forge (1 " + InventoryPart.GetUnitDisplayName(mineral) + "; one enhancement slot)",
                                "InfuseMineral|" + PreparationActions.Escaped(target), '\0', 17);
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
        static bool Compatible(Entity actor, Entity item, MineralInfusionTinkerModification modification, string enhancement) =>
            PreparationActions.Carried(actor, item) && (item.GetPart<StackerPart>()?.StackCount ?? 1) == 1
            && item.GetPart(enhancement) == null && modification.CanApply(item, out _);
        static MineralInfusionTinkerModification Modification(string name) => name switch
        { "PaleSalt" => new PaleSaltTinkerModification(), "ChoirIron" => new ChoirIronTinkerModification(), "GlowQuartz" => new GlowQuartzTinkerModification(), _ => null };
    }
}
