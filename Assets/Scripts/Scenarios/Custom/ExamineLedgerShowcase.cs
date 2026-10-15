using System;
using CavesOfOoo.Core;
namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario(name:"Examine Field Ledger",category:"World",description:"Nearby wells, crops, materials and actors for C-reader and same-world-art inspection. Use an isolated save for automated captures.")]
    public sealed class ExamineLedgerShowcase : IScenario
    {
        public static readonly string[] Subjects = {"RepairLinedWell","RepairRopeWell","RepairWoodenGate","KnotflaxCrop","HearthbulbCrop","SeamleafCrop","FireClay","SaltCuredBody","MarlbackScrabbler","Warden","Chest","LoanerLongsword"};
        public void Apply(ScenarioContext ctx)
        {
            ctx.World.RemoveEntitiesWithTag("Creature");
            for(int y=7;y<=17;y++)for(int x=30;x<=53;x++)
            {
                foreach(var entity in ctx.Zone.GetCell(x,y).Objects.ToArray())if(entity!=ctx.PlayerEntity)ctx.Zone.RemoveEntity(entity);
                ctx.Zone.AddEntity(ctx.Factory.CreateEntity("Grass"),x,y);
            }
            ctx.Player.Teleport(40,12).SetHpMax();
            for(int i=0;i<Subjects.Length;i++)
            {
                var entity=ctx.Spawn(Subjects[i]).At(34+(i%6)*3,9+(i/6)*6);
                if(entity==null)throw new InvalidOperationException("Examine showcase could not place "+Subjects[i]);
                var brain=entity.GetPart<BrainPart>();if(brain!=null)brain.Passive=true;
                var crop=entity.GetPart<CropPart>();if(crop!=null)
                {
                    crop.GrowthStage=2;crop.Water(50);
                    var render=entity.GetPart<RenderPart>();
                    render.RenderString=crop.GlyphForStage(crop.GrowthStage).ToString();
                    render.ColorString=crop.ColorForStage(crop.GrowthStage);
                }
            }
            ctx.Log("Examine ledger: two rows of wells, crops, materials, people and equipment. Examine from the world action menu; reader actions use no turn.");
        }
    }
}
