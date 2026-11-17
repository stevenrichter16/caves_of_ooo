using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Optional fresh-town salvage, never a cached-zone retrofit.</summary>
    internal static class CinderholdOilRecovery
    {
        internal const string Blueprint = "CinderholdOilRecoveryTray";
        internal static bool Payload(Entity owner)
        {
            if(owner?.BlueprintName != Blueprint || string.IsNullOrEmpty(owner.ID) || owner.Parts.Count != 6
                || owner.Parts.Any(p=>p==null || p.ParentEntity!=owner)
                || new[]{"Render","Physics","Examinable","Composition","Container","ContainedLiquidSource"}.Any(n=>owner.Parts.Count(p=>p.Name==n && p.ParentEntity==owner)!=1)) return false;
            var physics=owner.GetPart<PhysicsPart>();var liquid=owner.GetPart<ContainedLiquidSourcePart>();var box=owner.GetPart<ContainerPart>();
            var render=owner.GetPart<RenderPart>();
            if(physics==null || liquid==null || box==null || render==null || owner.GetPart<CompositionPart>()==null)return false;
            return physics.Solid && !physics.Takeable && physics.Weight==40 && physics.InInventory==null && physics.Equipped==null
                && liquid.LiquidId=="oil" && liquid.Capacity==12 && liquid.Volume>=0 && liquid.Volume<=12
                && !box.IsLocked && box.MaxItems==1 && owner.GetPart<CompositionPart>().MaterialsRaw=="Clay"
                && render.Visible && render.RenderString=="u" && render.ColorString=="&y" && render.RenderLayer==4
                && string.IsNullOrEmpty(render.VisualID) && string.IsNullOrEmpty(render.VisualVariant)
                && string.IsNullOrEmpty(render.BackgroundColor) && string.IsNullOrEmpty(render.GlyphVariants)
                && owner.HasTag("Furniture") && !owner.HasTag("Creature") && !owner.HasTag("Item") && !owner.HasTag("Terrain");
        }
        internal static Entity CreateFresh(EntityFactory factory)
        {
            var tray=CinderholdCompositionBuilder.Create(factory,Blueprint);
            if(!Payload(tray) || tray.GetPart<ContainedLiquidSourcePart>().Volume!=12 || tray.GetPart<ContainerPart>().Contents.Count!=0) return null;
            var container=tray.GetPart<ContainerPart>();var parts=tray.Parts.ToArray();
            var head=CinderholdCompositionBuilder.Create(factory,"CinderhookAxeHeadComponent");
            if(!Payload(tray) || tray.SpatialZone!=null || !tray.Parts.SequenceEqual(parts) || tray.GetPart<ContainerPart>()!=container
                || container.Contents.Count!=0 || tray.GetPart<ContainedLiquidSourcePart>().Volume!=12
                || !CinderholdCompositionBuilder.RegionalPortable(head,"CinderhookAxeHeadComponent") || head.SpatialZone!=null
                || head.GetPart<PhysicsPart>().InInventory!=null || head.GetPart<PhysicsPart>().Equipped!=null
                || (head.GetPart<StackerPart>()?.StackCount??1)!=1 || !tray.GetPart<ContainerPart>().AddItem(head)) return null;
            if(!Payload(tray) || tray.SpatialZone!=null || tray.GetPart<ContainedLiquidSourcePart>().Volume!=12
                || tray.GetPart<ContainerPart>().Contents.Count!=1 || tray.GetPart<ContainerPart>().Contents[0]!=head
                || head.GetPart<PhysicsPart>().InInventory!=tray || head.SpatialZone!=null) return null;
            return tray;
        }
    }
}
